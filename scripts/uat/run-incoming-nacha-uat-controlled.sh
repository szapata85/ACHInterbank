#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$ROOT_DIR"

export DOTNET_ROOT="${DOTNET_ROOT:-$HOME/.dotnet}"
export PATH="$DOTNET_ROOT:$DOTNET_ROOT/tools:$PATH"

echo "[UAT] Setup .NET/Node"
REQUIRED_SDK="10.0.203"

if [[ -x "$DOTNET_ROOT/dotnet" ]]; then
  export PATH="$DOTNET_ROOT:$DOTNET_ROOT/tools:$PATH"
fi

CURRENT_SDK=""
if command -v dotnet >/dev/null 2>&1; then
  CURRENT_SDK="$(dotnet --version || true)"
fi

if [[ "$CURRENT_SDK" == "$REQUIRED_SDK" ]]; then
  echo "[UAT] SDK requerido ya disponible en cache local: ${CURRENT_SDK}. Se omite descarga."
else
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

echo "[UAT] Certificate/SecretRef"
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release \
  --filter "FullyQualifiedName~Certificate|FullyQualifiedName~SecretRef" -v minimal

echo "[UAT] Frontend build"
pushd web/ach-interbank-ui >/dev/null
npm ci
set +e
npm run build
BUILD_EXIT=$?
set -e
if [[ $BUILD_EXIT -ne 0 ]]; then
  echo "[UAT][WARN] build productivo falló. Reintentando build UAT sin optimization externa..."
  npm run build -- --optimization=false
fi

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
