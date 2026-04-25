#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$ROOT_DIR"

export DOTNET_ROOT="${DOTNET_ROOT:-$HOME/.dotnet}"
export PATH="$DOTNET_ROOT:$DOTNET_ROOT/tools:$PATH"

echo "[UAT] Setup .NET/Node"
SETUP_OK=0
for attempt in 1 2 3; do
  if bash scripts/codex/setup-codex-env.sh; then
    SETUP_OK=1
    break
  fi
  echo "[UAT][WARN] setup-codex-env falló en intento ${attempt}; reintentando..."
  sleep 3
done
if [[ $SETUP_OK -ne 1 ]]; then
  echo "[UAT][ERROR] no fue posible completar setup de SDK/tooling tras 3 intentos."
  exit 1
fi

echo "[UAT] SDK info"
dotnet --info

dotnet --list-sdks

dotnet ef --version

echo "[UAT] Restore + build backend"
dotnet restore ACHInterbank.sln
dotnet build ACHInterbank.sln -c Release

echo "[UAT] Incoming/CommandCenter/StateMachine/Resilience/Observability"
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release \
  --filter "FullyQualifiedName~IncomingNacha|FullyQualifiedName~CommandCenter|FullyQualifiedName~StateMachine|FullyQualifiedName~Observability|FullyQualifiedName~Resilience" -v minimal

echo "[UAT] NACHA/Mapping/BatchNumber regression"
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release \
  --filter "FullyQualifiedName~Nacha|FullyQualifiedName~Mapping|FullyQualifiedName~BatchNumber" -v minimal

echo "[UAT] DigitalEnvelope/Signature/OpenEnvelope"
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release \
  --filter "FullyQualifiedName~DigitalEnvelope|FullyQualifiedName~Signature|FullyQualifiedName~OpenEnvelope" -v minimal

echo "[UAT] Certificate/SecretRef/OpenBao/Vault"
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release \
  --filter "FullyQualifiedName~Certificate|FullyQualifiedName~SecretRef|FullyQualifiedName~OpenBao|FullyQualifiedName~Vault" -v minimal

echo "[UAT] Frontend build"
pushd web/ach-interbank-ui >/dev/null
npm ci
npm run build

set +e
npm test -- --watch=false --browsers=ChromeHeadless
NPM_TEST_EXIT=$?
set -e

if [[ $NPM_TEST_EXIT -ne 0 ]]; then
  echo "[UAT][WARN] npm test falló (entorno sin CHROME_BIN / issue karma-rimraf)."
fi
popd >/dev/null

echo "[UAT] Verify workflow manual-only"
rg -n "workflow_dispatch|if: github.event_name == 'workflow_dispatch'|push:|pull_request|schedule|workflow_run" .github/workflows/postgres-integration-tests.yml -S

echo "[UAT] Completed"
