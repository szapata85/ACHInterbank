#!/bin/sh
set -e

cert_path="${ASPNETCORE_Kestrel__Certificates__Default__Path:-/https/aspnetapp.pfx}"
cert_password="${ASPNETCORE_Kestrel__Certificates__Default__Password:-changeit}"

if [ -z "${Database__Provider:-}" ] && [ -n "${ConnectionStrings__PostgresConnection:-}" ]; then
  export Database__Provider="Postgres"
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
