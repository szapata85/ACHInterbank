#!/usr/bin/env bash
set -euo pipefail
compose_file="$1"; api01="$2"; api02="$3"; spa="$4"; evidence="$5"
mkdir -p "$evidence"
for url in "$api01/health/live" "$api01/health/ready" "$api02/health/live" "$api02/health/ready" "$spa"; do
  for _ in $(seq 1 90); do curl -fsS "$url" >/dev/null && break; sleep 2; done
  curl -fsS "$url" >/dev/null
done
curl -fsS "$api01/health/ready" > "$evidence/health-api-01.json"
curl -fsS "$api02/health/ready" > "$evidence/health-api-02.json"
docker compose -f "$compose_file" ps > "$evidence/docker-compose-ps.txt"
