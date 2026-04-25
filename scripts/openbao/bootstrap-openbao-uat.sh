#!/bin/sh
set -eu

STATE_DIR="/bootstrap/state"
INIT_FILE="$STATE_DIR/openbao-init.json"
API_TOKEN_FILE="$STATE_DIR/api-token"
POLICY_FILE="/bootstrap/policy-ach-api.hcl"

mkdir -p "$STATE_DIR"

wait_for_openbao() {
  i=0
  until bao status >/dev/null 2>&1; do
    i=$((i + 1))
    if [ "$i" -gt 60 ]; then
      echo "OpenBao no respondió en tiempo esperado"
      exit 1
    fi
    sleep 2
  done
}

extract_json_value() {
  key="$1"
  file="$2"
  sed -n "s/.*\"$key\"[[:space:]]*:[[:space:]]*\"\([^\"]*\)\".*/\1/p" "$file" | head -n 1
}

wait_for_openbao

if [ ! -s "$INIT_FILE" ]; then
  echo "[bootstrap] Inicializando OpenBao UAT (key-shares=1 threshold=1)"
  bao operator init -key-shares=1 -key-threshold=1 -format=json > "$INIT_FILE"
fi

UNSEAL_KEY="$(sed -n 's/.*"unseal_keys_b64"[[:space:]]*:[[:space:]]*\["\([^"]*\)"\].*/\1/p' "$INIT_FILE" | head -n 1)"
ROOT_TOKEN="$(extract_json_value "root_token" "$INIT_FILE")"

if [ -z "$UNSEAL_KEY" ]; then
  echo "No se pudo extraer unseal key desde $INIT_FILE"
  exit 1
fi

if [ -z "$ROOT_TOKEN" ]; then
  ROOT_TOKEN="${OPENBAO_UAT_ROOT_TOKEN:-}"
fi

if bao status 2>/dev/null | grep -q 'Sealed[[:space:]]*true'; then
  echo "[bootstrap] Unsealing OpenBao"
  bao operator unseal "$UNSEAL_KEY" >/dev/null
fi

if ! bao login "$ROOT_TOKEN" >/dev/null 2>&1; then
  echo "[bootstrap] root token no válido para login"
  exit 1
fi

if ! bao secrets list -format=json | grep -q '"'"${OPENBAO_KV_MOUNT:-secret}"'/"'; then
  echo "[bootstrap] Habilitando KV v2 en ${OPENBAO_KV_MOUNT:-secret}"
  bao secrets enable -path="${OPENBAO_KV_MOUNT:-secret}" kv-v2 >/dev/null
fi

bao policy write "${OPENBAO_POLICY_NAME:-ach-api}" "$POLICY_FILE" >/dev/null

if [ ! -s "$API_TOKEN_FILE" ] || ! BAO_TOKEN="$(cat "$API_TOKEN_FILE" 2>/dev/null)" bao token lookup >/dev/null 2>&1; then
  echo "[bootstrap] Creando token UAT para API"
  bao token create -orphan -policy="${OPENBAO_POLICY_NAME:-ach-api}" -period=24h -format=json \
    | sed -n 's/.*"client_token"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p' | head -n 1 > "$API_TOKEN_FILE"
fi

chmod 600 "$API_TOKEN_FILE"
echo "[bootstrap] OpenBao UAT listo. Token API en $API_TOKEN_FILE"
