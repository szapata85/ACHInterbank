#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../../.." && pwd)"
cd "$ROOT"
if ! command -v dotnet >/dev/null 2>&1; then
  echo 'NO_EJECUTADO: SDK .NET 10 no instalado. No se certifican pruebas C# ni consultas EF Core.' >&2
  exit 127
fi
mkdir -p "docs/auditoria-prenotificaciones/evidencias"
# Small, targeted regression suite; avoid running unrelated large tests.
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj \
  --filter 'FullyQualifiedName~PrenotificationCursorI13RelationalTests|FullyQualifiedName~PrenotificationCandidateScanI12Tests|FullyQualifiedName~PrenotificationTacitAcceptanceI09SafetyTests|FullyQualifiedName~PrenotificationI10TerminalStateTests' \
  --logger 'trx;LogFileName=APN-I13.trx' \
  --results-directory docs/auditoria-prenotificaciones/evidencias
