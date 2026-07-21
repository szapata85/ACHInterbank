#!/usr/bin/env bash
set -euo pipefail
compose_file="$1"; api01="$2"; api02="$3"; evidence="$4"
mkdir -p "$evidence"
cp -R web/ach-interbank-ui/playwright-report "$evidence/" 2>/dev/null || true
cp -R web/ach-interbank-ui/test-results "$evidence/" 2>/dev/null || true
find web/ach-interbank-ui/test-results -name 'scheduler-desktop.png' -exec cp {} "$evidence/scheduler-desktop.png" \; 2>/dev/null || true
find web/ach-interbank-ui/test-results -name 'scheduler-mobile-view-only.png' -exec cp {} "$evidence/scheduler-mobile-view-only.png" \; 2>/dev/null || true
[[ -s "$evidence/scheduler-junit.xml" ]] || { echo 'No se generó scheduler-junit.xml'; exit 1; }
[[ -s "$evidence/scheduler-desktop.png" && -s "$evidence/scheduler-mobile-view-only.png" ]] || { echo 'Faltan capturas requeridas'; exit 1; }
grep -Eq 'tests="2"' "$evidence/scheduler-junit.xml" || { echo 'El E2E no ejecutó exactamente dos escenarios'; exit 1; }
grep -Eq 'skipped="0"|skipped="0\.0"' "$evidence/scheduler-junit.xml" || { echo 'El E2E contiene pruebas omitidas'; exit 1; }
docker compose -f "$compose_file" logs --no-color achinterbank-api-01 > "$evidence/api-01-scheduler.log" || true
docker compose -f "$compose_file" logs --no-color achinterbank-api-02 > "$evidence/api-02-scheduler.log" || true
