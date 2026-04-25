#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$ROOT_DIR"

if ! command -v docker >/dev/null 2>&1; then
  echo "Docker no disponible en este entorno."
  echo "Ejecute este script en una máquina con Docker/Compose para el E2E real."
  exit 2
fi

cp -n .env.example .env || true

echo "[1/7] Levantando stack UAT..."
docker compose up -d --build

echo "[2/7] Verificando servicios y bootstrap..."
docker compose ps
docker compose logs openbao-bootstrap --tail=100
docker compose exec achinterbank-api sh -c 'test -s /openbao-bootstrap/api-token && echo token-ok'

echo "[3/7] Ejecutando suite policy/historical decrypt..."
export DOTNET_ROOT=${DOTNET_ROOT:-$HOME/.dotnet}
export PATH="$DOTNET_ROOT:$DOTNET_ROOT/tools:$PATH"
$DOTNET_ROOT/dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj \
  -c Release \
  --filter "FullyQualifiedName~HistoricalDecryptPolicyTests" \
  -v minimal

echo "[4/7] Placeholder de carga real .pfx (requiere bearer JWT válido):"
cat <<'CMDS'
curl -k -X POST "https://localhost:843/nacha-security/certificates/management/private" \
  -H "Authorization: Bearer <JWT_ADMIN>" \
  -F "code=CERT-HIST-DEC" \
  -F "displayName=Cert Historical Decrypt" \
  -F "clearingHouseId=1" \
  -F "environment=1" \
  -F "purpose=2" \
  -F "holderType=1" \
  -F "storageMode=6" \
  -F "password=<PFX_PASSWORD>" \
  -F "file=@./tests/fixtures/certs/historical-decrypt.pfx"
CMDS

echo "[5/7] Verificar en OpenBao (ajustar ruta según SecretRef persistido):"
cat <<'CMDS'
docker compose exec openbao sh -lc 'export BAO_ADDR=http://127.0.0.1:8200; bao kv get secret/certificates/test/ch-1/inbounddecryption/v1'
CMDS

echo "[6/7] Verificar en BD metadata + SecretRef (sin private material):"
cat <<'CMDS'
docker compose exec postgres psql -U sa -d ACHInterbank -c "select id, purpose, status, secretref, rawpubliccertificate is not null as has_public_blob from \"DigitalCertificateVersions\" order by id desc limit 5;"
CMDS

echo "[7/7] Para validar decrypt histórico real, ejecutar operación de decrypt con sobre cuyo issuer/serial apunte a versión vencida retenida y revisar CertificateUsageLogs (OperationType=HistoricalDecrypt)."
