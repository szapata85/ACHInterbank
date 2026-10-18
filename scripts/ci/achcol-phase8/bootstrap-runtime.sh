#!/usr/bin/env bash
set -euo pipefail

compose_file="${1:-docker-compose.achcol-phase8.yml}"
api_url="${2:-http://127.0.0.1:${PHASE8_API_01_PORT:-8461}}"
evidence_dir="${3:-artifacts/achcol-phase8-evidence}"
mkdir -p "$evidence_dir"

require() { command -v "$1" >/dev/null 2>&1 || { echo "Missing required command: $1" >&2; exit 2; }; }
require docker
require curl
require jq
require openssl

for _ in $(seq 1 90); do
  if curl -fsS "$api_url/health/ready" >/dev/null 2>&1; then break; fi
  sleep 2
done
curl -fsS "$api_url/health/ready" > "$evidence_dir/health-api-01.json"

admin_user="achcol-phase8-admin-$(date +%s)-$RANDOM"
admin_password="$(openssl rand -hex 18)!aA1"
admin_hash="$(printf %s "$admin_password" | sha256sum | awk '{print $1}')"
admin_id="$(cat /proc/sys/kernel/random/uuid 2>/dev/null || openssl rand -hex 16 | sed -E 's/(.{8})(.{4})(.{4})(.{4})(.{12})/\1-\2-\3-\4-\5/')"

sql="INSERT INTO \"Users\" (\"Id\",\"Username\",\"FullName\",\"Email\",\"PasswordHash\",\"IsActive\",\"FailedLoginAttempts\",\"CreatedAt\",\"UpdatedAt\") VALUES ('$admin_id','$admin_user','ACHCOL Phase 8 admin','$admin_user@phase8.local','$admin_hash',true,0,timezone('utc',now()),timezone('utc',now())); INSERT INTO \"UserRoles\" (\"UserId\",\"RoleId\",\"CreatedAt\",\"UpdatedAt\") VALUES ('$admin_id','1f8602da-6415-43f8-b61d-cb396f8577f1',timezone('utc',now()),timezone('utc',now()));"
docker compose -f "$compose_file" exec -T phase8-postgres \
  psql -v ON_ERROR_STOP=1 -U phase8_test -d achinterbank_phase8 -c "$sql" >/dev/null

login_response="$(mktemp)"
trap 'rm -f "$login_response"' EXIT
http_code="$(curl -sS -o "$login_response" -w '%{http_code}' \
  -H 'Content-Type: application/json' \
  -d "{\"username\":\"$admin_user\",\"password\":\"$admin_password\"}" \
  "$api_url/auth/login")"
[[ "$http_code" =~ ^2 ]] || { echo "Synthetic Phase 8 login failed with HTTP $http_code" >&2; exit 1; }
token="$(jq -er '.data.token // .data.data.token' "$login_response")"

fingerprint="$(docker compose -f "$compose_file" exec -T phase8-sftp \
  ssh-keygen -lf /etc/ssh/ssh_host_ed25519_key.pub -E sha256 | awk '{print $2}' | tr -d '\r')"
[[ "$fingerprint" == SHA256:* ]] || { echo "Could not resolve controlled SFTP host fingerprint" >&2; exit 1; }

get_admin() {
  curl -fsS -H "Authorization: Bearer $token" "$api_url/api/ach-colombia/file-exchange/administration"
}

admin_json="$(get_admin)"
printf '%s' "$admin_json" | jq '{profileName,provider,protocol,profileEnabled,endpoint,port,principal,automaticOutboundEnabled,automaticInboundEnabled,manualOutboundAllowed,manualInboundAllowed,maximumRetries,retryDelaySeconds,retentionDays,outboundLocation,inboundLocation,archiveLocation,credentialConfigured,credentialType,credentialUpdatedAtUtc,sftpHostKeyFingerprint,sftpTimeoutSeconds,concurrencyToken}' > "$evidence_dir/mft-administration-before.json"

# Protect the synthetic password first while the current ManagedFolder profile is still active.
# Switching to SftpEndpoint is fail-closed and therefore requires an already protected password.
credential_payload="$(printf '%s' "$admin_json" | jq \
  --arg secret "$PHASE8_SFTP_PASSWORD" '
  {credentialType:"Password", secret:$secret, concurrencyToken:.concurrencyToken}')"
with_credential="$(curl -fsS -X PUT \
  -H "Authorization: Bearer $token" \
  -H 'Content-Type: application/json' \
  --data "$credential_payload" \
  "$api_url/api/ach-colombia/file-exchange/administration/credential")"

update_payload="$(printf '%s' "$with_credential" | jq \
  --arg host 'phase8-sftp' \
  --arg user "${PHASE8_SFTP_USERNAME:-achcol_phase8}" \
  --arg fp "$fingerprint" '
  {
    profileName: "ACH Colombia Phase 8 Controlled",
    provider: "SftpEndpoint",
    protocol: "Sftp",
    profileEnabled: true,
    endpoint: $host,
    port: 22,
    principal: $user,
    automaticOutboundEnabled: true,
    automaticInboundEnabled: true,
    manualOutboundAllowed: true,
    manualInboundAllowed: true,
    maximumRetries: 3,
    retryDelaySeconds: 5,
    retentionDays: 7,
    outboundLocation: "/input",
    inboundLocation: "/output",
    archiveLocation: "/archive",
    concurrencyToken: .concurrencyToken,
    sftpHostKeyFingerprint: $fp,
    sftpTimeoutSeconds: 10
  }')"
configured="$(curl -fsS -X PUT \
  -H "Authorization: Bearer $token" \
  -H 'Content-Type: application/json' \
  --data "$update_payload" \
  "$api_url/api/ach-colombia/file-exchange/administration")"
printf '%s' "$configured" | jq '{profileName,provider,protocol,profileEnabled,endpoint,port,principal,automaticOutboundEnabled,automaticInboundEnabled,manualOutboundAllowed,manualInboundAllowed,maximumRetries,retryDelaySeconds,retentionDays,outboundLocation,inboundLocation,archiveLocation,credentialConfigured,credentialType,credentialUpdatedAtUtc,sftpHostKeyFingerprint,sftpTimeoutSeconds,concurrencyToken}' > "$evidence_dir/mft-administration-configured.json"

curl -fsS -X POST \
  -H "Authorization: Bearer $token" \
  "$api_url/api/ach-colombia/file-exchange/administration/test-connection" \
  | jq '{succeeded,code,message}' > "$evidence_dir/mft-connection-test.json"

# Persist only a disposable bearer token outside certification evidence for the current run.
runtime_env="$(mktemp -p "${TMPDIR:-/tmp}" achcol-phase8-runtime.XXXXXX)"
chmod 600 "$runtime_env"
cat > "$runtime_env" <<RUNTIME
PHASE8_API_TOKEN=$token
PHASE8_API_URL=$api_url
RUNTIME
printf '%s\n' "$runtime_env"
