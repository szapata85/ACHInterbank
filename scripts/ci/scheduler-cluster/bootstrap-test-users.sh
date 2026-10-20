#!/usr/bin/env bash
set -euo pipefail

compose_file="$1"
database="$2"
api_url="$3"

api_failure_diagnostics() {
  echo "::error::API SQL Server did not become ready"
  docker compose -f "$compose_file" ps || true
  docker compose -f "$compose_file" logs --no-color --tail=250 \
    scheduler-sqlserver quartz-schema achinterbank-api-01 || true
}

api_login_failure_diagnostics() {
  docker compose -f "$compose_file" logs --no-color --tail=250 \
    achinterbank-api-01 || true
}

sanitize_login_response() {
  local response_file="$1"
  local response

  if command -v jq >/dev/null 2>&1; then
    response="$(jq -r '
      if type == "object" then
        (.message // .title // .error // (.errors | keys | join(",")) // "No sanitized error message")
      else
        "Non-JSON error response"
      end
    ' "$response_file" 2>/dev/null || printf '%s' 'Non-JSON error response')"
  else
    response="$(node -e '
      const fs = require("fs");
      try {
        const body = JSON.parse(fs.readFileSync(process.argv[1], "utf8"));
        const value = body?.message ?? body?.title ?? body?.error ??
          (body?.errors && typeof body.errors === "object" ? Object.keys(body.errors).join(",") : null) ??
          "No sanitized error message";
        process.stdout.write(String(value));
      } catch {
        process.stdout.write("Non-JSON error response");
      }
    ' "$response_file" 2>/dev/null || printf '%s' 'Non-JSON error response')"
  fi
  printf '%s' "$response" | tr '\r\n' ' ' | cut -c1-1000
}

extract_login_token() {
  local response_file="$1"

  if command -v jq >/dev/null 2>&1; then
    jq -er '.data.token // .data.data.token' "$response_file"
  else
    node -e '
      const fs = require("fs");
      const body = JSON.parse(fs.readFileSync(process.argv[1], "utf8"));
      const token = body?.data?.token ?? body?.data?.data?.token;
      if (!token) process.exit(1);
      process.stdout.write(token);
    ' "$response_file"
  fi
}

for _ in $(seq 1 90); do
  if curl -fsS "$api_url/health/ready" >/dev/null; then break; fi
  sleep 2
done
if ! curl -fsS "$api_url/health/ready" >/dev/null; then
  api_failure_diagnostics
  exit 1
fi

admin_user="scheduler-admin-${GITHUB_RUN_ID:-local}-${RANDOM}"
view_user="scheduler-view-${GITHUB_RUN_ID:-local}-${RANDOM}"
view_role_name="Scheduler CI View ${GITHUB_RUN_ID:-local}-${RANDOM}"
admin_password="$(openssl rand -hex 16)!aA1"
view_password="$(openssl rand -hex 16)!aA1"
admin_hash="$(printf %s "$admin_password" | sha256sum | awk '{print $1}')"
view_hash="$(printf %s "$view_password" | sha256sum | awk '{print $1}')"
new_uuid() {
  if [[ -r /proc/sys/kernel/random/uuid ]]; then
    cat /proc/sys/kernel/random/uuid
  else
    openssl rand -hex 16 | sed -E 's/(.{8})(.{4})(.{4})(.{4})(.{12})/\1-\2-\3-\4-\5/'
  fi
}
admin_id="$(new_uuid)"
view_id="$(new_uuid)"
view_role_id="$(new_uuid)"

if [[ "$database" == postgres ]]; then
  sql="INSERT INTO \"Users\" (\"Id\",\"Username\",\"FullName\",\"Email\",\"PasswordHash\",\"IsActive\",\"FailedLoginAttempts\",\"CreatedAt\",\"UpdatedAt\") VALUES ('$admin_id','$admin_user','Scheduler CI admin','$admin_user@ci.local','$admin_hash',true,0,timezone('utc',now()),timezone('utc',now())),('$view_id','$view_user','Scheduler CI view','$view_user@ci.local','$view_hash',true,0,timezone('utc',now()),timezone('utc',now())); INSERT INTO \"UserRoles\" (\"UserId\",\"RoleId\",\"CreatedAt\",\"UpdatedAt\") VALUES ('$admin_id','1f8602da-6415-43f8-b61d-cb396f8577f1',timezone('utc',now()),timezone('utc',now())); INSERT INTO \"Roles\" (\"Id\",\"Name\",\"Description\") VALUES ('$view_role_id','Scheduler CI View','Ephemeral CI role'); INSERT INTO \"UserRoles\" (\"UserId\",\"RoleId\",\"CreatedAt\",\"UpdatedAt\") VALUES ('$view_id','$view_role_id',timezone('utc',now()),timezone('utc',now())); INSERT INTO \"RolePermissions\" (\"RoleId\",\"PermissionId\") SELECT '$view_role_id',\"Id\" FROM \"Permissions\" WHERE \"Name\" IN ('Scheduler.View','Scheduler.History.View','Scheduler.ViewInstances'); INSERT INTO \"ClearingHouseConfigs\" (\"Id\",\"ClearingHouseId\",\"HolidayStrategy\",\"NachaProfileId\",\"PaymentRailCode\",\"RequiresNachaProfile\",\"TimeZoneId\") SELECT 1,1,'CalendarDays',NULL,NULL,false,'America/Bogota' WHERE NOT EXISTS (SELECT 1 FROM \"ClearingHouseConfigs\" WHERE \"Id\"=1); INSERT INTO \"ClearingHouses\" (\"Id\",\"Name\",\"Code\",\"OriginCode\",\"ClearingHouseId\",\"CreatedAt\",\"IsActive\",\"UpdatedAt\") SELECT 1,'ACH Colombia CI','ACHCOL','ACHCOL',1,timezone('utc',now()),true,timezone('utc',now()) WHERE NOT EXISTS (SELECT 1 FROM \"ClearingHouses\" WHERE \"Code\"='ACHCOL');"
  docker compose -f "$compose_file" exec -T scheduler-postgres psql -v ON_ERROR_STOP=1 -U scheduler_test -d achinterbank_scheduler -c "$sql" >/dev/null
else
  sql="SET XACT_ABORT ON; BEGIN TRANSACTION; INSERT INTO [Users] ([Id],[Username],[FullName],[Email],[PasswordHash],[IsActive],[FailedLoginAttempts],[CreatedAt],[UpdatedAt]) VALUES ('$admin_id','$admin_user','Scheduler CI admin','$admin_user@ci.local','$admin_hash',1,0,SYSUTCDATETIME(),SYSUTCDATETIME()),('$view_id','$view_user','Scheduler CI view','$view_user@ci.local','$view_hash',1,0,SYSUTCDATETIME(),SYSUTCDATETIME()); INSERT INTO [UserRoles] ([UserId],[RoleId],[CreatedAt],[UpdatedAt]) VALUES ('$admin_id','1f8602da-6415-43f8-b61d-cb396f8577f1',SYSUTCDATETIME(),SYSUTCDATETIME()); INSERT INTO [Roles] ([Id],[Name],[Description]) VALUES ('$view_role_id','$view_role_name','Ephemeral CI role'); INSERT INTO [UserRoles] ([UserId],[RoleId],[CreatedAt],[UpdatedAt]) VALUES ('$view_id','$view_role_id',SYSUTCDATETIME(),SYSUTCDATETIME()); INSERT INTO [RolePermissions] ([RoleId],[PermissionId]) SELECT '$view_role_id',[Id] FROM [Permissions] WHERE [Name] IN ('Scheduler.View','Scheduler.History.View','Scheduler.ViewInstances'); IF NOT EXISTS (SELECT 1 FROM [ClearingHouses] WITH (UPDLOCK,HOLDLOCK) WHERE [Code]='ACHCOL') BEGIN INSERT INTO [ClearingHouseConfigs] ([ClearingHouseId],[HolidayStrategy],[NachaProfileId],[PaymentRailCode],[RequiresNachaProfile],[TimeZoneId]) VALUES (0,'CalendarDays',NULL,NULL,0,'America/Bogota'); DECLARE @clearingHouseConfigId int = CONVERT(int,SCOPE_IDENTITY()); INSERT INTO [ClearingHouses] ([Name],[Code],[OriginCode],[ClearingHouseId],[CreatedAt],[IsActive],[UpdatedAt]) VALUES ('ACH Colombia CI','ACHCOL','ACHCOL',@clearingHouseConfigId,SYSUTCDATETIME(),1,SYSUTCDATETIME()); DECLARE @clearingHouseId int = CONVERT(int,SCOPE_IDENTITY()); UPDATE [ClearingHouseConfigs] SET [ClearingHouseId]=@clearingHouseId WHERE [Id]=@clearingHouseConfigId; END; COMMIT TRANSACTION;"
  if ! sql_output="$(
    MSYS_NO_PATHCONV=1 docker compose -f "$compose_file" exec -T scheduler-sqlserver \
      /opt/mssql-tools18/bin/sqlcmd \
      -S localhost \
      -U sa \
      -P "${SCHEDULER_SQL_PASSWORD:-Scheduler_Local_123!}" \
      -C \
      -I \
      -d ACHInterbankSchedulerCluster \
      -b \
      -r1 \
      -Q "$sql" 2>&1
  )"; then
    echo "::error::SQL Server bootstrap failed"
    printf '%s\n' "$sql_output"
    docker compose -f "$compose_file" logs --no-color --tail=250 \
      scheduler-sqlserver achinterbank-api-01 || true
    exit 1
  fi
fi

login() {
  local username="$1"
  local password="$2"
  local response_file
  local http_code
  local token

  response_file="$(mktemp)"
  if ! http_code="$(curl -sS -o "$response_file" -w '%{http_code}' \
    -H 'Content-Type: application/json' \
    -d "{\"username\":\"$username\",\"password\":\"$password\"}" \
    "$api_url/auth/login")"; then
    echo "::error::Synthetic-user login request failed"
    api_login_failure_diagnostics
    rm -f "$response_file"
    return 1
  fi

  if [[ ! "$http_code" =~ ^2 ]]; then
    echo "::error::Synthetic-user login returned HTTP $http_code"
    printf 'Sanitized response: %s\n' "$(sanitize_login_response "$response_file")"
    api_login_failure_diagnostics
    rm -f "$response_file"
    return 1
  fi

  if ! token="$(extract_login_token "$response_file")"; then
    echo "::error::Synthetic-user login response did not contain a token"
    printf 'HTTP status: %s; sanitized response: %s\n' "$http_code" "$(sanitize_login_response "$response_file")"
    api_login_failure_diagnostics
    rm -f "$response_file"
    return 1
  fi

  rm -f "$response_file"
  printf '%s\n' "$token"
}
view_token="$(login "$view_user" "$view_password")"
if [[ "${GITHUB_ACTIONS:-}" == "true" ]]; then
  for secret in "$admin_user" "$admin_password" "$view_token"; do echo "::add-mask::$secret"; done
fi
{
  echo "ACH_USER=$admin_user"
  echo "ACH_PASS=$admin_password"
  echo "E2E_SCHEDULER_VIEW_TOKEN=$view_token"
} >> "$GITHUB_ENV"
