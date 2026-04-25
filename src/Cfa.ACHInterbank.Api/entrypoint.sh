#!/bin/sh
set -e

cert_path="${ASPNETCORE_Kestrel__Certificates__Default__Path:-/https/aspnetapp.pfx}"
cert_password="${ASPNETCORE_Kestrel__Certificates__Default__Password:-changeit}"

if [ -z "${Database__Provider:-}" ] && [ -n "${ConnectionStrings__PostgresConnection:-}" ]; then
  export Database__Provider="Postgres"
fi

openbao_token_file="${DigitalEnvelope__OpenBao__ApiTokenFilePath:-/openbao-bootstrap/api-token}"
wait_openbao_token="${WAIT_FOR_OPENBAO_TOKEN_FILE:-true}"
wait_openbao_timeout="${WAIT_FOR_OPENBAO_TIMEOUT_SECONDS:-90}"

if [ "$wait_openbao_token" = "true" ] && [ -z "${DigitalEnvelope__OpenBao__ApiToken:-}" ]; then
  elapsed=0
  while [ ! -s "$openbao_token_file" ] && [ "$elapsed" -lt "$wait_openbao_timeout" ]; do
    echo "Waiting for OpenBao API token file at $openbao_token_file (${elapsed}s/${wait_openbao_timeout}s)"
    sleep 2
    elapsed=$((elapsed + 2))
  done
fi

if [ "${ASPNETCORE_URLS:-}" != "" ] && [ ! -f "$cert_path" ] && command -v openssl >/dev/null 2>&1; then
  cert_dir="$(dirname "$cert_path")"
  mkdir -p "$cert_dir"

  echo "Generating development HTTPS certificate at $cert_path"
  openssl req -x509 -nodes -newkey rsa:2048 \
    -keyout "$cert_dir/aspnetapp.key" \
    -out "$cert_dir/aspnetapp.crt" \
    -days 365 \
    -subj "/CN=localhost"

  openssl pkcs12 -export \
    -out "$cert_path" \
    -inkey "$cert_dir/aspnetapp.key" \
    -in "$cert_dir/aspnetapp.crt" \
    -passout "pass:$cert_password"
fi

exec dotnet Cfa.ACHInterbank.Api.dll
