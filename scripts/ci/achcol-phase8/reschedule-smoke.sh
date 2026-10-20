#!/usr/bin/env bash
set -euo pipefail

: "${PHASE8_API_TOKEN:?PHASE8_API_TOKEN is required}"
PHASE8_API_URL="${PHASE8_API_URL:-http://127.0.0.1:${PHASE8_API_01_PORT:-8461}}"
evidence_dir="${1:-artifacts/achcol-phase8-evidence}"
compose_file="${2:-docker-compose.achcol-phase8.yml}"
mkdir -p "$evidence_dir"

admin_rows="$(curl -fsS -H "Authorization: Bearer $PHASE8_API_TOKEN" \
  "$PHASE8_API_URL/api/ach-colombia/operational-schedules/administration")"
printf '%s' "$admin_rows" | jq . > "$evidence_dir/operational-schedule-administration.json"

# Resolve an already-active non-candidate regulatory cycle directly from the controlled DB.
# This avoids making the latest inactive candidate executable and does not write regulatory data.
scope="$(docker compose -f "$compose_file" exec -T phase8-postgres \
  psql -U phase8_test -d achinterbank_phase8 -At -F '|' -v ON_ERROR_STOP=1 -c \
  'SELECT h."Id", c."Id", c."PolicyVersion", c."CycleName" FROM "ClearingHouses" h JOIN "ClearingHouseCycleConfigs" c ON c."ClearingHouseId"=h."Id" WHERE h."Code"='"'"'ACHCOL'"'"' AND c."IsActive"=true AND c."PolicyVersion" <> '"'"'ACHCOL-7C-CANDIDATE'"'"' ORDER BY c."EffectiveFrom" DESC, c."StartTime" ASC LIMIT 1;' | tr -d '\r')"
[[ -n "$scope" ]] || { echo "No active non-candidate ACH Colombia cycle available for controlled reschedule smoke" >&2; exit 3; }
IFS='|' read -r house_id cycle_id policy_version cycle_name <<< "$scope"
printf 'policy=%s\ncycle=%s\n' "$policy_version" "$cycle_name" > "$evidence_dir/reschedule-scope.txt"

# Test-only operational times. They are not written to regulation and are deliberately unrelated to C2-C6 authority.
start_1="12:34:00"
start_2="12:35:00"

http_code="$(curl -sS -o "$evidence_dir/reschedule-existing.json" -w '%{http_code}' \
  -H "Authorization: Bearer $PHASE8_API_TOKEN" \
  "$PHASE8_API_URL/api/ach-colombia/operational-schedules/by-cycle/$cycle_id")"
if [[ "$http_code" == "404" ]]; then
  create="$(jq -n \
    --argjson house "$house_id" --argjson cycle "$cycle_id" --arg start "$start_1" '
    {clearingHouseId:$house,clearingHouseCycleConfigId:$cycle,automaticOutboundEnabled:true,
     automaticOutboundStartTime:$start,automaticOutboundLatestStartTime:null,outboundExecutionStrategy:1,
     effectiveTimeZoneId:"America/Bogota",isActive:true}')"
  current="$(curl -fsS -X POST -H "Authorization: Bearer $PHASE8_API_TOKEN" -H 'Content-Type: application/json' \
    --data "$create" "$PHASE8_API_URL/api/ach-colombia/operational-schedules")"
elif [[ "$http_code" =~ ^2 ]]; then
  current="$(cat "$evidence_dir/reschedule-existing.json")"
else
  echo "Could not query controlled operational schedule (HTTP $http_code)" >&2
  exit 1
fi
printf '%s' "$current" | jq . > "$evidence_dir/reschedule-before.json"

schedule_id="$(printf '%s' "$current" | jq -r '.id')"
row_version="$(printf '%s' "$current" | jq -r '.rowVersion')"
update="$(jq -n \
  --argjson house "$house_id" --argjson cycle "$cycle_id" --arg start "$start_2" --arg rv "$row_version" '
  {clearingHouseId:$house,clearingHouseCycleConfigId:$cycle,automaticOutboundEnabled:true,
   automaticOutboundStartTime:$start,automaticOutboundLatestStartTime:null,outboundExecutionStrategy:1,
   effectiveTimeZoneId:"America/Bogota",isActive:true,expectedRowVersion:$rv}')"
after="$(curl -fsS -X PUT -H "Authorization: Bearer $PHASE8_API_TOKEN" -H 'Content-Type: application/json' \
  --data "$update" "$PHASE8_API_URL/api/ach-colombia/operational-schedules/$schedule_id")"
printf '%s' "$after" | jq . > "$evidence_dir/reschedule-after.json"

# The smoke is made non-executable after reschedule verification, so it cannot fire later unexpectedly.
rv2="$(printf '%s' "$after" | jq -r '.rowVersion')"
curl -fsS -X PATCH -H "Authorization: Bearer $PHASE8_API_TOKEN" -H 'Content-Type: application/json' \
  --data "$(jq -n --arg rv "$rv2" '{expectedRowVersion:$rv}')" \
  "$PHASE8_API_URL/api/ach-colombia/operational-schedules/$schedule_id/deactivate" \
  | jq . > "$evidence_dir/reschedule-deactivated.json"
