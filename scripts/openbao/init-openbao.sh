#!/usr/bin/env bash
set -euo pipefail

if [[ "${1:-}" == "" ]]; then
  echo "Uso: $0 <openbao-container-name>"
  exit 1
fi

CONTAINER="$1"

if ! docker exec "$CONTAINER" bao status >/dev/null 2>&1; then
  echo "OpenBao aún no responde en el contenedor $CONTAINER"
  exit 1
fi

if docker exec "$CONTAINER" bao status 2>/dev/null | grep -q "Initialized.*true"; then
  echo "OpenBao ya inicializado."
  exit 0
fi

echo "Inicializando OpenBao (1 unseal key, threshold 1)..."
docker exec "$CONTAINER" bao operator init -key-shares=1 -key-threshold=1 | tee ./ops/openbao/.openbao-init.local.txt

echo "Guardado en ./ops/openbao/.openbao-init.local.txt (NO commitear)."
