#!/bin/sh
set -eu

# Bootstrap OpenBao para laboratorio/UAT.
# IMPORTANTE: key-shares=1 y key-threshold=1 simplifican operación,
# pero NO son aceptables para producción.

STATE_DIR="/bootstrap/state"
INIT_FILE="$STATE_DIR/openbao-init.json"
API_TOKEN_FILE="$STATE_DIR/api-token"
POLICY_FILE="/bootstrap/policy-ach-api.hcl"
KV_MOUNT="${OPENBAO_KV_MOUNT:-secret}"
POLICY_NAME="${OPENBAO_POLICY_NAME:-ach-api}"

umask 077
mkdir -p "$STATE_DIR"

wait_for_openbao() {
  i=0
  until bao status >/dev/null 2>&1; do
    i=$((i + 1))
    if [ "$i" -gt 90 ]; then
      echo "[bootstrap] OpenBao no respondió en tiempo esperado"
      exit 1
    fi
    sleep 2
  done
}

status_json() {
  bao status -format=json 2>/dev/null || true
}

extract_json_value() {
  key="$1"
  file="$2"
  sed -n "s/.*\"$key\"[[:space:]]*:[[:space:]]*\"\([^\"]*\)\".*/\1/p" "$file" | head -n 1
}

extract_status_bool() {
  key="$1"
  echo "$2" | sed -n "s/.*\"$key\"[[:space:]]*:[[:space:]]*\(true\|false\).*/\1/p" | head -n 1
}

wait_for_openbao

CURRENT_STATUS="$(status_json)"
INITIALIZED="$(extract_status_bool initialized "$CURRENT_STATUS")"

if [ "$INITIALIZED" != "true" ]; then
  echo "[bootstrap] Inicializando OpenBao UAT (key-shares=1 threshold=1)"
  TMP_INIT_FILE="$STATE_DIR/openbao-init.tmp.json"
  bao operator init -key-shares=1 -key-threshold=1 -format=json > "$TMP_INIT_FILE"
  mv "$TMP_INIT_FILE" "$INIT_FILE"
fi

if [ ! -s "$INIT_FILE" ]; then
  echo "[bootstrap] Archivo de estado $INIT_FILE no existe o está vacío"
  exit 1
fi

UNSEAL_KEY="$(sed -n 's/.*"unseal_keys_b64"[[:space:]]*:[[:space:]]*\["\([^"]*\)"\].*/\1/p' "$INIT_FILE" | head -n 1)"
ROOT_TOKEN="$(extract_json_value root_token "$INIT_FILE")"

if [ -z "$UNSEAL_KEY" ] || [ -z "$ROOT_TOKEN" ]; then
  echo "[bootstrap] No se pudo recuperar unseal key o root token desde el estado local"
  exit 1
fi

CURRENT_STATUS="$(status_json)"
SEALED="$(extract_status_bool sealed "$CURRENT_STATUS")"
if [ "$SEALED" = "true" ]; then
  echo "[bootstrap] Ejecutando unseal"
  bao operator unseal "$UNSEAL_KEY" >/dev/null
fi

if ! BAO_TOKEN="$ROOT_TOKEN" bao token lookup >/dev/null 2>&1; then
  echo "[bootstrap] Root token no válido para este nodo"
  exit 1
fi

if ! BAO_TOKEN="$ROOT_TOKEN" bao secrets list -format=json | grep -q "\"$KV_MOUNT/\""; then
  echo "[bootstrap] Habilitando KV v2 en $KV_MOUNT"
  BAO_TOKEN="$ROOT_TOKEN" bao secrets enable -path="$KV_MOUNT" kv-v2 >/dev/null
fi

BAO_TOKEN="$ROOT_TOKEN" bao policy write "$POLICY_NAME" "$POLICY_FILE" >/dev/null

if [ ! -s "$API_TOKEN_FILE" ] || ! BAO_TOKEN="$(cat "$API_TOKEN_FILE" 2>/dev/null)" bao token lookup >/dev/null 2>&1; then
  echo "[bootstrap] Creando token de API para UAT"
  BAO_TOKEN="$ROOT_TOKEN" bao token create -orphan -policy="$POLICY_NAME" -period=24h -format=json \
    | sed -n 's/.*"client_token"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p' | head -n 1 > "$API_TOKEN_FILE"
fi

chmod 600 "$API_TOKEN_FILE"

FINAL_STATUS="$(status_json)"
FINAL_INITIALIZED="$(extract_status_bool initialized "$FINAL_STATUS")"
FINAL_SEALED="$(extract_status_bool sealed "$FINAL_STATUS")"

if [ "$FINAL_INITIALIZED" != "true" ] || [ "$FINAL_SEALED" != "false" ]; then
  echo "[bootstrap] Estado final inválido: initialized=$FINAL_INITIALIZED sealed=$FINAL_SEALED"
  exit 1
fi

echo "[bootstrap] OpenBao UAT inicializado, unsealed y con token API disponible"
