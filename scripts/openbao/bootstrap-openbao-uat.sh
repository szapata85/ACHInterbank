#!/bin/sh
set -eu

# Bootstrap OpenBao para laboratorio/UAT.
# IMPORTANTE:
# key-shares=1 y key-threshold=1 simplifican operación,
# pero NO son aceptables para producción.

STATE_DIR="/bootstrap/state"
INIT_FILE="$STATE_DIR/openbao-init.json"
API_TOKEN_FILE="$STATE_DIR/api-token"
POLICY_FILE="/bootstrap/policy-ach-api.hcl"

KV_MOUNT="${OPENBAO_KV_MOUNT:-secret}"
POLICY_NAME="${OPENBAO_POLICY_NAME:-ach-api}"
WAIT_SECONDS="${WAIT_FOR_OPENBAO_SECONDS:-180}"
SLEEP_SECONDS=2

umask 077
mkdir -p "$STATE_DIR"

log() {
  echo "[bootstrap] $*"
}

status_json() {
  # No validar por exit code.
  # OpenBao puede responder pero devolver código != 0 si está sealed o sin inicializar.
  bao status -format=json 2>&1 || true
}

extract_json_value_from_file() {
  key="$1"
  file="$2"

  tr -d '\r\n' < "$file" \
    | sed -n "s/.*\"$key\"[[:space:]]*:[[:space:]]*\"\([^\"]*\)\".*/\1/p" \
    | head -n 1
}

extract_status_bool() {
  key="$1"
  content="$2"

  printf "%s" "$content" \
    | tr -d '\r\n' \
    | sed -n "s/.*\"$key\"[[:space:]]*:[[:space:]]*\(true\|false\).*/\1/p" \
    | head -n 1
}

extract_unseal_key() {
  file="$1"

  tr -d '\r\n' < "$file" \
    | sed -n 's/.*"unseal_keys_b64"[[:space:]]*:[[:space:]]*\[[[:space:]]*"\([^"]*\)".*/\1/p' \
    | head -n 1
}

wait_for_openbao() {
  elapsed=0
  last_status=""

  log "Esperando OpenBao en BAO_ADDR=${BAO_ADDR:-no-definido}"

  while [ "$elapsed" -lt "$WAIT_SECONDS" ]; do
    last_status="$(status_json)"

    # Si aparece "initialized", OpenBao ya respondió.
    # Puede estar initialized=false, pero eso ya es una respuesta válida.
    if printf "%s" "$last_status" | grep -q '"initialized"'; then
      log "OpenBao respondió correctamente."
      return 0
    fi

    log "OpenBao aún no responde. Reintentando en ${SLEEP_SECONDS}s..."
    sleep "$SLEEP_SECONDS"
    elapsed=$((elapsed + SLEEP_SECONDS))
  done

  log "ERROR: OpenBao no respondió en tiempo esperado."
  log "Última salida recibida de bao status:"
  echo "$last_status"
  exit 1
}

wait_for_openbao

CURRENT_STATUS="$(status_json)"
INITIALIZED="$(extract_status_bool initialized "$CURRENT_STATUS")"

if [ "$INITIALIZED" != "true" ]; then
  log "Inicializando OpenBao UAT con key-shares=1 y threshold=1"

  TMP_INIT_FILE="$STATE_DIR/openbao-init.tmp.json"

  bao operator init \
    -key-shares=1 \
    -key-threshold=1 \
    -format=json > "$TMP_INIT_FILE"

  mv "$TMP_INIT_FILE" "$INIT_FILE"
  chmod 600 "$INIT_FILE"

  log "OpenBao inicializado. Archivo de estado creado en $INIT_FILE"
else
  log "OpenBao ya estaba inicializado."
fi

if [ ! -s "$INIT_FILE" ]; then
  log "ERROR: Archivo de estado $INIT_FILE no existe o está vacío."
  log "Si OpenBao ya fue inicializado pero se perdió este archivo, no se puede hacer unseal automáticamente."
  log "En ambiente DEV/UAT puedes reiniciar volúmenes de OpenBao."
  exit 1
fi

UNSEAL_KEY="$(extract_unseal_key "$INIT_FILE")"
ROOT_TOKEN="$(extract_json_value_from_file root_token "$INIT_FILE")"

if [ -z "$UNSEAL_KEY" ] || [ -z "$ROOT_TOKEN" ]; then
  log "ERROR: No se pudo recuperar unseal key o root token desde $INIT_FILE"
  exit 1
fi

CURRENT_STATUS="$(status_json)"
SEALED="$(extract_status_bool sealed "$CURRENT_STATUS")"

if [ "$SEALED" = "true" ]; then
  log "OpenBao está sealed. Ejecutando unseal..."
  bao operator unseal "$UNSEAL_KEY" >/dev/null
  log "Unseal ejecutado."
else
  log "OpenBao ya estaba unsealed."
fi

if ! BAO_TOKEN="$ROOT_TOKEN" bao token lookup >/dev/null 2>&1; then
  log "ERROR: Root token no válido para este nodo."
  log "Esto puede pasar si el volumen de datos de OpenBao y el volumen bootstrap/state quedaron desincronizados."
  exit 1
fi

if ! BAO_TOKEN="$ROOT_TOKEN" bao secrets list -format=json | grep -q "\"$KV_MOUNT/\""; then
  log "Habilitando KV v2 en $KV_MOUNT"
  BAO_TOKEN="$ROOT_TOKEN" bao secrets enable -path="$KV_MOUNT" -version=2 kv >/dev/null
else
  log "KV mount $KV_MOUNT ya existe."
fi

if [ ! -f "$POLICY_FILE" ]; then
  log "ERROR: No existe el archivo de policy $POLICY_FILE"
  exit 1
fi

log "Registrando policy $POLICY_NAME"
BAO_TOKEN="$ROOT_TOKEN" bao policy write "$POLICY_NAME" "$POLICY_FILE" >/dev/null

if [ ! -s "$API_TOKEN_FILE" ] || ! BAO_TOKEN="$(cat "$API_TOKEN_FILE" 2>/dev/null || true)" bao token lookup >/dev/null 2>&1; then
  log "Creando token de API para UAT"

  TMP_TOKEN_FILE="$STATE_DIR/api-token.tmp.json"

  BAO_TOKEN="$ROOT_TOKEN" bao token create \
    -orphan \
    -policy="$POLICY_NAME" \
    -period=24h \
    -format=json > "$TMP_TOKEN_FILE"

  NEW_API_TOKEN="$(extract_json_value_from_file client_token "$TMP_TOKEN_FILE")"

  rm -f "$TMP_TOKEN_FILE"

  if [ -z "$NEW_API_TOKEN" ]; then
    log "ERROR: No se pudo extraer client_token al crear el token de API."
    exit 1
  fi

  printf "%s" "$NEW_API_TOKEN" > "$API_TOKEN_FILE"
  chmod 600 "$API_TOKEN_FILE"
else
  log "Token de API existente válido."
fi

FINAL_STATUS="$(status_json)"
FINAL_INITIALIZED="$(extract_status_bool initialized "$FINAL_STATUS")"
FINAL_SEALED="$(extract_status_bool sealed "$FINAL_STATUS")"

if [ "$FINAL_INITIALIZED" != "true" ] || [ "$FINAL_SEALED" != "false" ]; then
  log "ERROR: Estado final inválido: initialized=$FINAL_INITIALIZED sealed=$FINAL_SEALED"
  echo "$FINAL_STATUS"
  exit 1
fi

log "OpenBao UAT inicializado, unsealed y con token API disponible."