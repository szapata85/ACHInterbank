#!/usr/bin/env bash
# Reproducible F07 runtime evidence; exits nonzero if SDK or tests are unavailable.
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
PROJECT="$ROOT/tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj"
RESULTS="${F07_RESULTS_DIR:-$ROOT/artifacts/f07-dotnet-results}"
if ! command -v dotnet >/dev/null 2>&1; then
  echo 'F07 NOT EXECUTED: .NET SDK absent; no runtime test result may be claimed.' >&2
  exit 2
fi
mkdir -p "$RESULTS"
printf 'dotnet version: '; dotnet --version
# First isolate cross-phase tests, then run ALL xUnit tests to protect ACH Colombia.
dotnet test "$PROJECT" --configuration Release \
  --filter 'FullyQualifiedName~CenitF07CrossPhaseRegressionTests|FullyQualifiedName~CenitChamberResponseLifecycleTests' \
  --results-directory "$RESULTS" --logger 'trx;LogFileName=F07-cross-phase.trx' --verbosity normal

dotnet test "$PROJECT" --configuration Release \
  --results-directory "$RESULTS" --logger 'trx;LogFileName=F07-full-regression.trx' --verbosity normal

echo 'F07 xUnit results are in:' "$RESULTS"
