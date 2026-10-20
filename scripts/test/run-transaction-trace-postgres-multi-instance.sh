#!/usr/bin/env bash
set -euo pipefail

export GITHUB_ENV=/dev/stdout
docker compose -f docker-compose.scheduler-cluster.yml exec -T scheduler-postgres \
  psql -v ON_ERROR_STOP=1 -U scheduler_test -d achinterbank_scheduler >/dev/null <<'SQL'
DELETE FROM "RolePermissions" WHERE "RoleId" IN (SELECT "Id" FROM "Roles" WHERE "Name" = 'Scheduler CI View');
DELETE FROM "UserRoles" WHERE "RoleId" IN (SELECT "Id" FROM "Roles" WHERE "Name" = 'Scheduler CI View') OR "UserId" IN (SELECT "Id" FROM "Users" WHERE "Username" LIKE 'scheduler_ci_%');
DELETE FROM "Users" WHERE "Username" LIKE 'scheduler_ci_%';
DELETE FROM "Roles" WHERE "Name" = 'Scheduler CI View';
SQL
echo "[OPS-GAP-005] bootstrap-start"
bootstrap_output="$(scripts/ci/scheduler-cluster/bootstrap-test-users.sh docker-compose.scheduler-cluster.yml postgres http://127.0.0.1:8441)"
echo "[OPS-GAP-005] bootstrap-complete"
while IFS='=' read -r key value; do
  case "$key" in
    ACH_USER) export TRANSACTION_TRACE_API_USERNAME="$value" ;;
    ACH_PASS) export TRANSACTION_TRACE_API_PASSWORD="$value" ;;
  esac
done <<< "$bootstrap_output"
unset bootstrap_output

if test -z "${TRANSACTION_TRACE_API_USERNAME:-}"; then echo "[OPS-GAP-005] bootstrap-user-missing"; exit 2; fi
if test -z "${TRANSACTION_TRACE_API_PASSWORD:-}"; then echo "[OPS-GAP-005] bootstrap-password-missing"; exit 3; fi
echo "[OPS-GAP-005] bootstrap-credentials-captured"

trace_pg_container="$(docker compose -f docker-compose.scheduler-cluster.yml ps -q scheduler-postgres)"
trace_pg_password="$(docker inspect --format '{{range .Config.Env}}{{println .}}{{end}}' "$trace_pg_container" | sed -n 's/^POSTGRES_PASSWORD=//p')"
if test -z "$trace_pg_password"; then echo "[OPS-GAP-005] database-password-missing"; exit 5; fi
echo "[OPS-GAP-005] runtime-configuration-ready"

export TRANSACTION_TRACE_API_1=http://127.0.0.1:8441
export TRANSACTION_TRACE_API_2=http://127.0.0.1:8442
export TRANSACTION_TRACE_RUNTIME_PROVIDER=PostgreSql
export TRANSACTION_TRACE_RUNTIME_CONNECTION_STRING="Host=127.0.0.1;Port=5434;Database=achinterbank_scheduler;Username=scheduler_test;Password=$trace_pg_password"
unset trace_pg_password
export REQUIRE_TRANSACTION_TRACE_MULTI_INSTANCE=true

dotnet.exe test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --no-build --filter FullyQualifiedName=Cfa.ACHInterbank.Tests.TransactionTraceAllocationMultiDbTests.TwoApiProcesses_SharingOneDatabase_PersistOneHundredDistinctTraces
