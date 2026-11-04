#!/usr/bin/env bash
# APN-I12: truthful executable suite, including I08-I11 regression.
set -euo pipefail
cd "$(dirname "${BASH_SOURCE[0]}")/../../.."
if ! command -v dotnet >/dev/null 2>&1; then
  echo 'NO_EJECUTADA: SDK dotnet 10.0.403 ausente; no certificar compilación ni xUnit.' >&2
  exit 127
fi
echo "SDK: $(dotnet --version)"
project='tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj'
filter='FullyQualifiedName~PrenotificationCandidateScanI12Tests|FullyQualifiedName~PrenotificationReceiptWindowI11Tests|FullyQualifiedName~ThirdPartyAccountValidationLifecycleTests|FullyQualifiedName~PrenotificationTacitAcceptanceI09SafetyTests|FullyQualifiedName~PrenotificationI10TerminalStateTests|FullyQualifiedName~CenitChamberResponseLifecycleTests'
dotnet restore "$project"
dotnet test "$project" --no-restore --configuration Release \
  --filter "$filter" --logger 'trx;LogFileName=APN-I12.trx' \
  --results-directory ./TestResults/APN-I12
