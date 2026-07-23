#!/usr/bin/env bash
set -euo pipefail

compose_file="$1"
database="$2"
api_url="$3"

for _ in $(seq 1 90); do
  if curl -fsS "$api_url/health/ready" >/dev/null; then break; fi
  sleep 2
done
curl -fsS "$api_url/health/ready" >/dev/null

admin_user="scheduler-admin-${GITHUB_RUN_ID:-local}-${RANDOM}"
view_user="scheduler-view-${GITHUB_RUN_ID:-local}-${RANDOM}"
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
  sql="INSERT INTO [Users] ([Id],[Username],[FullName],[Email],[PasswordHash],[IsActive],[FailedLoginAttempts],[CreatedAt],[UpdatedAt]) VALUES ('$admin_id','$admin_user','Scheduler CI admin','$admin_user@ci.local','$admin_hash',1,0,SYSUTCDATETIME(),SYSUTCDATETIME()),('$view_id','$view_user','Scheduler CI view','$view_user@ci.local','$view_hash',1,0,SYSUTCDATETIME(),SYSUTCDATETIME()); INSERT INTO [UserRoles] ([UserId],[RoleId],[CreatedAt],[UpdatedAt]) VALUES ('$admin_id','1f8602da-6415-43f8-b61d-cb396f8577f1',SYSUTCDATETIME(),SYSUTCDATETIME()); INSERT INTO [Roles] ([Id],[Name],[Description]) VALUES ('$view_role_id','Scheduler CI View','Ephemeral CI role'); INSERT INTO [UserRoles] ([UserId],[RoleId],[CreatedAt],[UpdatedAt]) VALUES ('$view_id','$view_role_id',SYSUTCDATETIME(),SYSUTCDATETIME()); INSERT INTO [RolePermissions] ([RoleId],[PermissionId]) SELECT '$view_role_id',[Id] FROM [Permissions] WHERE [Name] IN ('Scheduler.View','Scheduler.History.View','Scheduler.ViewInstances'); IF NOT EXISTS (SELECT 1 FROM [ClearingHouseConfigs] WHERE [Id]=1) INSERT INTO [ClearingHouseConfigs] ([Id],[ClearingHouseId],[HolidayStrategy],[NachaProfileId],[PaymentRailCode],[RequiresNachaProfile],[TimeZoneId]) VALUES (1,1,'CalendarDays',NULL,NULL,0,'America/Bogota'); IF NOT EXISTS (SELECT 1 FROM [ClearingHouses] WHERE [Code]='ACHCOL') INSERT INTO [ClearingHouses] ([Id],[Name],[Code],[OriginCode],[ClearingHouseId],[CreatedAt],[IsActive],[UpdatedAt]) VALUES (1,'ACH Colombia CI','ACHCOL','ACHCOL',1,SYSUTCDATETIME(),1,SYSUTCDATETIME());"
  docker compose -f "$compose_file" exec -T scheduler-sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "${SCHEDULER_SQL_PASSWORD:-Scheduler_Local_123!}" -C -d ACHInterbankSchedulerCluster -b -Q "$sql" >/dev/null
fi

login() { curl -fsS -H 'Content-Type: application/json' -d "{\"username\":\"$1\",\"password\":\"$2\"}" "$api_url/auth/login" | jq -er '.data.token // .data.data.token'; }
view_token="$(login "$view_user" "$view_password")"
for secret in "$admin_user" "$admin_password" "$view_token"; do echo "::add-mask::$secret"; done
{
  echo "ACH_USER=$admin_user"
  echo "ACH_PASS=$admin_password"
  echo "E2E_SCHEDULER_VIEW_TOKEN=$view_token"
} >> "$GITHUB_ENV"
