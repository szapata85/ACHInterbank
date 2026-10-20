#!/usr/bin/env bash
set -euo pipefail
compose_file="$1"; api01="$2"; api02="$3"; evidence="$4"; playwright_outcome="${5:-unknown}"
mkdir -p "$evidence"
cp -R web/ach-interbank-ui/playwright-report "$evidence/" 2>/dev/null || true
cp -R web/ach-interbank-ui/test-results "$evidence/" 2>/dev/null || true
find web/ach-interbank-ui/test-results -name 'scheduler-desktop.png' -exec cp {} "$evidence/scheduler-desktop.png" \; 2>/dev/null || true
find web/ach-interbank-ui/test-results -name 'scheduler-mobile-tasks.png' -exec cp {} "$evidence/scheduler-mobile-tasks.png" \; 2>/dev/null || true
docker compose -f "$compose_file" logs --no-color achinterbank-api-01 > "$evidence/api-01-scheduler.log" || true
docker compose -f "$compose_file" logs --no-color achinterbank-api-02 > "$evidence/api-02-scheduler.log" || true

{
  echo "Playwright outcome: $playwright_outcome"
  if [[ -s "$evidence/scheduler-desktop.png" ]]; then
    echo "scheduler-desktop.png: present"
  else
    echo "scheduler-desktop.png: absent because the scenario did not reach the final success capture"
  fi
  if [[ -s "$evidence/scheduler-mobile-tasks.png" ]]; then
    echo "scheduler-mobile-tasks.png: present"
  else
    echo "scheduler-mobile-tasks.png: absent because the scenario did not reach the final success capture"
  fi
} > "$evidence/playwright-outcome.txt"

if [[ "$playwright_outcome" != "success" ]]; then
  echo "::notice::Playwright failed; automatic screenshot, video, trace, error-context, JUnit and report artifacts were preserved when generated."
  exit 0
fi

[[ -s "$evidence/scheduler-junit.xml" ]] || {
  echo 'No se generó scheduler-junit.xml para una ejecución exitosa'
  exit 1
}
[[ -s "$evidence/scheduler-desktop.png" && -s "$evidence/scheduler-mobile-tasks.png" ]] || {
  echo 'Una ejecución exitosa no produjo las capturas finales requeridas'
  exit 1
}

node - "$evidence/scheduler-junit.xml" <<'NODE'
const fs = require('fs');
const file = process.argv[2];
const xml = fs.readFileSync(file, 'utf8');
const root = xml.match(/<testsuites\b([^>]*)>/) ?? xml.match(/<testsuite\b([^>]*)>/);
if (!root) {
  console.error('JUnit no contiene un elemento testsuites/testsuite válido');
  process.exit(1);
}
const attributes = Object.fromEntries(
  [...root[1].matchAll(/(\w+)="([^"]*)"/g)].map((match) => [match[1], match[2]])
);
const tests = Number(attributes.tests ?? -1);
const failures = Number(attributes.failures ?? 0);
const errors = Number(attributes.errors ?? 0);
const skipped = Number(attributes.skipped ?? 0);
if (tests !== 5 || failures !== 0 || errors !== 0 || skipped !== 0) {
  console.error(`Resultado JUnit inesperado: tests=${tests}, failures=${failures}, errors=${errors}, skipped=${skipped}`);
  process.exit(1);
}
NODE
