#!/usr/bin/env bash
set -euo pipefail

export GITHUB_ENV=/dev/stdout
trace_sql_container="$(docker compose -f docker-compose.scheduler-cluster.sqlserver.yml ps -q scheduler-sqlserver)"
trace_sql_password="$(docker inspect --format '{{range .Config.Env}}{{println .}}{{end}}' "$trace_sql_container" | sed -n 's/^MSSQL_SA_PASSWORD=//p')"
if test -z "$trace_sql_password"; then echo "[OPS-GAP-005] database password missing" >&2; exit 1; fi
echo "[OPS-GAP-005] bootstrap-cleanup-start"
MSYS_NO_PATHCONV=1 docker compose -f docker-compose.scheduler-cluster.sqlserver.yml exec -T scheduler-sqlserver \
  /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$trace_sql_password" -d ACHInterbankSchedulerCluster -C -b -Q \
  "SET QUOTED_IDENTIFIER ON; SET ANSI_NULLS ON; DELETE FROM [RolePermissions] WHERE [RoleId] IN (SELECT [Id] FROM [Roles] WHERE [Name] = 'Scheduler CI View'); DELETE FROM [UserRoles] WHERE [RoleId] IN (SELECT [Id] FROM [Roles] WHERE [Name] = 'Scheduler CI View') OR [UserId] IN (SELECT [Id] FROM [Users] WHERE [Username] LIKE 'scheduler_ci_%'); DELETE FROM [Users] WHERE [Username] LIKE 'scheduler_ci_%'; DELETE FROM [Roles] WHERE [Name] = 'Scheduler CI View';" >/dev/null
echo "[OPS-GAP-005] bootstrap-cleanup-complete"
echo "[OPS-GAP-005] bootstrap-start"
bootstrap_output="$(scripts/ci/scheduler-cluster/bootstrap-test-users.sh docker-compose.scheduler-cluster.sqlserver.yml sqlserver http://127.0.0.1:8451)"
echo "[OPS-GAP-005] bootstrap-complete"
while IFS='=' read -r key value; do
  case "$key" in
    ACH_USER) export TRANSACTION_TRACE_API_USERNAME="$value" ;;
    ACH_PASS) export TRANSACTION_TRACE_API_PASSWORD="$value" ;;
  esac
done <<< "$bootstrap_output"
unset bootstrap_output

if test -z "${TRANSACTION_TRACE_API_USERNAME:-}"; then echo "[OPS-GAP-005] bootstrap username missing" >&2; exit 1; fi
if test -z "${TRANSACTION_TRACE_API_PASSWORD:-}"; then echo "[OPS-GAP-005] bootstrap password missing" >&2; exit 1; fi
echo "[OPS-GAP-005] bootstrap-credentials-captured"

echo "[OPS-GAP-005] runtime-configuration-ready"

export TRANSACTION_TRACE_API_1=http://127.0.0.1:8451
export TRANSACTION_TRACE_API_2=http://127.0.0.1:8452
export TRANSACTION_TRACE_RUNTIME_PROVIDER=SqlServer
export TRANSACTION_TRACE_RUNTIME_CONNECTION_STRING="Server=127.0.0.1,1434;Database=ACHInterbankSchedulerCluster;User Id=sa;Password=$trace_sql_password;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
unset trace_sql_password

dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --no-build --filter 'FullyQualifiedName~TwoApiProcesses_SharingOneDatabase_PersistOneHundredDistinctTraces'
