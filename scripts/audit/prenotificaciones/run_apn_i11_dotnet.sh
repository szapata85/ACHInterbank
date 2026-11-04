#!/usr/bin/env bash
# APN-I11. Requires .NET SDK 10.0.403 (or allowed roll-forward per global.json).
# Fails nonzero when .NET is unavailable; never claims an unexecuted test passed.
set -euo pipefail
cd "$(dirname "${BASH_SOURCE[0]}")/../../.."
if ! command -v dotnet >/dev/null 2>&1; then
  echo 'NO_EJECUTADA: dotnet SDK no está instalado en este ambiente.' >&2
  exit 127
fi
printf 'SDK: '; dotnet --version
project='tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj'
filter='FullyQualifiedName~PrenotificationReceiptWindowI11Tests|FullyQualifiedName~ThirdPartyAccountValidationLifecycleTests|FullyQualifiedName~PrenotificationTacitAcceptanceI09SafetyTests|FullyQualifiedName~PrenotificationI10TerminalStateTests|FullyQualifiedName~CenitChamberResponseLifecycleTests'
dotnet restore "$project"
dotnet test "$project" --no-restore --configuration Release --filter "$filter" --logger 'trx;LogFileName=APN-I11.trx' --results-directory ./TestResults/APN-I11
