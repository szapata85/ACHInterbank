#!/usr/bin/env bash
set -euo pipefail
compose_file="${1:-docker-compose.achcol-phase8.yml}"
evidence_dir="${2:-artifacts/achcol-phase8-evidence}"
api01="${3:-http://127.0.0.1:${PHASE8_API_01_PORT:-8461}}"
api02="${4:-http://127.0.0.1:${PHASE8_API_02_PORT:-8462}}"
mkdir -p "$evidence_dir"

curl -fsS "$api01/health/ready" > "$evidence_dir/health-api-01-final.json" || true
curl -fsS "$api02/health/ready" > "$evidence_dir/health-api-02-final.json" || true
docker compose -f "$compose_file" ps > "$evidence_dir/docker-compose-ps.txt" || true
docker compose -f "$compose_file" logs --no-color phase8-goanywhere > "$evidence_dir/goanywhere-simulator.log" 2>/dev/null || true

psql_cmd=(docker compose -f "$compose_file" exec -T phase8-postgres psql -U phase8_test -d achinterbank_phase8 -v ON_ERROR_STOP=1 -P pager=off)
"${psql_cmd[@]}" -c 'SELECT "SchedulerName","InstanceId","InstanceName","HostName","LastHeartbeatUtc","Status","CurrentlyExecutingJobs","Version" FROM "SchedulerInstanceStates" ORDER BY "InstanceName";' > "$evidence_dir/scheduler-instances.txt" || true
"${psql_cmd[@]}" -c 'SELECT "TRIGGER_NAME","TRIGGER_GROUP","JOB_NAME","JOB_GROUP","NEXT_FIRE_TIME","PREV_FIRE_TIME","TRIGGER_STATE","TRIGGER_TYPE" FROM "QRTZ_TRIGGERS" ORDER BY "TRIGGER_GROUP","TRIGGER_NAME";' > "$evidence_dir/quartz-triggers.txt" || true
"${psql_cmd[@]}" -c 'SELECT "ExecutionId","TaskCode","JobName","TriggerName","SchedulerInstanceId","ScheduledAt","StartedAt","FinishedAt","Status","IsRecovery","MisfireDetected","CorrelationId" FROM "TaskExecutionLog" WHERE "TaskCode" IN ('"'"'AchColombiaManagedMftOutbound'"'"','"'"'AchColombiaManagedMftInbound'"'"') ORDER BY "StartedAt";' > "$evidence_dir/task-executions.txt" || true
"${psql_cmd[@]}" -c 'SELECT "Id","Direction","PhysicalFileName","ContentSha256","FileSize","OperationalDate","AchCycleId","ExecutionOrigin","Status","AttemptCount","CreatedAtUtc","TransferredAtUtc","ProcessedAtUtc","CorrelationId","IdempotencyKey" FROM "AchManagedFileTransfers" ORDER BY "CreatedAtUtc";' > "$evidence_dir/transfers.txt" || true
"${psql_cmd[@]}" -c 'SELECT e."TransferId",e."OccurredAtUtc",e."EventType",e."Result",e."ExecutionOrigin",e."Actor" FROM "AchManagedFileTransferEvents" e ORDER BY e."OccurredAtUtc",e."Id";' > "$evidence_dir/transfer-events.txt" || true

cat > "$evidence_dir/EXECUTION_STATUS.txt" <<STATUS
Controlled runtime evidence collected.
This file is not an external ACH Colombia homologation artifact.
No real ACH Colombia endpoint or production credential was used by this harness.
STATUS
