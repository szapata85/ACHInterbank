#!/usr/bin/env bash
set -euo pipefail

compose_file="docker-compose.achcol-phase8.yml"
evidence_dir="${PHASE8_EVIDENCE_DIR:-artifacts/achcol-phase8-evidence}"
mkdir -p "$evidence_dir" artifacts/achcol-phase8-fixtures

require() { command -v "$1" >/dev/null 2>&1 || { echo "Missing required command: $1" >&2; exit 2; }; }
for cmd in python dotnet docker curl jq openssl sha256sum; do require "$cmd"; done
docker compose version >/dev/null

python scripts/validate-solution-projects.py | tee "$evidence_dir/validate-solution-projects.txt"
dotnet --info > "$evidence_dir/dotnet-info.txt"
dotnet build ACHInterbank.sln -c Release | tee "$evidence_dir/build-release.txt"

test_filter='FullyQualifiedName~AchColombiaCycleOperationalScheduleAdminServiceTests|FullyQualifiedName~AchColombiaOperationalQuartzScheduleTests|FullyQualifiedName~AchColombiaManagedMftOperationalTriggerTests|FullyQualifiedName~AchColombiaManagedFileExchangeTests|FullyQualifiedName~AchColombiaSftpMftAdapterTests|FullyQualifiedName~AchColombiaManagedMftInboundWindowTests|FullyQualifiedName~AchColombiaManagedMftConnectionTesterTests'
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --no-build \
  --filter "$test_filter" --logger "trx;LogFileName=achcol-phase8-focal.trx" \
  | tee "$evidence_dir/focal-tests.txt"

export PHASE8_POSTGRES_PASSWORD="${PHASE8_POSTGRES_PASSWORD:-$(openssl rand -hex 20)}"
export PHASE8_SFTP_PASSWORD="${PHASE8_SFTP_PASSWORD:-$(openssl rand -hex 20)}"
export PHASE8_SFTP_USERNAME="${PHASE8_SFTP_USERNAME:-achcol_phase8}"

cleanup() {
  scripts/ci/achcol-phase8/collect-evidence.sh "$compose_file" "$evidence_dir" || true
  docker compose -f "$compose_file" down --remove-orphans >/dev/null 2>&1 || true
}
trap cleanup EXIT

docker compose -f "$compose_file" config --quiet
docker compose -f "$compose_file" up -d --build

for url in \
  "http://127.0.0.1:${PHASE8_API_01_PORT:-8461}/health/ready" \
  "http://127.0.0.1:${PHASE8_API_02_PORT:-8462}/health/ready" \
  "http://127.0.0.1:${PHASE8_SPA_PORT:-746}/health/live"; do
  for _ in $(seq 1 120); do curl -fsS "$url" >/dev/null 2>&1 && break; sleep 2; done
  curl -fsS "$url" >/dev/null
done

runtime_env="$(scripts/ci/achcol-phase8/bootstrap-runtime.sh "$compose_file" "http://127.0.0.1:${PHASE8_API_01_PORT:-8461}" "$evidence_dir")"
# shellcheck disable=SC1090
source "$runtime_env"
rm -f "$runtime_env"

scripts/ci/achcol-phase8/reschedule-smoke.sh "$evidence_dir" "$compose_file"

# Credential rotation is exercised without persisting any secret in evidence.
admin="$(curl -fsS -H "Authorization: Bearer $PHASE8_API_TOKEN" "$PHASE8_API_URL/api/ach-colombia/file-exchange/administration")"
wrong_secret="$(openssl rand -hex 20)"
wrong_payload="$(printf '%s' "$admin" | jq --arg s "$wrong_secret" '{credentialType:"Password",secret:$s,concurrencyToken:.concurrencyToken}')"
rotated="$(curl -fsS -X PUT -H "Authorization: Bearer $PHASE8_API_TOKEN" -H 'Content-Type: application/json' --data "$wrong_payload" "$PHASE8_API_URL/api/ach-colombia/file-exchange/administration/credential")"
curl -sS -X POST -H "Authorization: Bearer $PHASE8_API_TOKEN" "$PHASE8_API_URL/api/ach-colombia/file-exchange/administration/test-connection" | jq '{succeeded,code,message}' > "$evidence_dir/credential-rotation-negative.json"
restore_payload="$(printf '%s' "$rotated" | jq --arg s "$PHASE8_SFTP_PASSWORD" '{credentialType:"Password",secret:$s,concurrencyToken:.concurrencyToken}')"
curl -fsS -X PUT -H "Authorization: Bearer $PHASE8_API_TOKEN" -H 'Content-Type: application/json' --data "$restore_payload" "$PHASE8_API_URL/api/ach-colombia/file-exchange/administration/credential" >/dev/null
curl -fsS -X POST -H "Authorization: Bearer $PHASE8_API_TOKEN" "$PHASE8_API_URL/api/ach-colombia/file-exchange/administration/test-connection" | jq '{succeeded,code,message}' > "$evidence_dir/credential-rotation-restored.json"

scripts/ci/achcol-phase8/collect-evidence.sh "$compose_file" "$evidence_dir"
trap - EXIT
docker compose -f "$compose_file" down --remove-orphans

echo "Phase 8 controlled harness finished. Review $evidence_dir and the certification matrix before claiming E2E PASS."
