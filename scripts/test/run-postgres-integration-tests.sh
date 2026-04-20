#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
COMPOSE_FILE="${ROOT_DIR}/docker-compose.test.yml"
ENV_FILE="${ROOT_DIR}/.env.test.example"
SOLUTION="${ROOT_DIR}/ACHInterbank.sln"
TEST_PROJECT="${ROOT_DIR}/tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj"
PERSISTENCE_PROJECT="${ROOT_DIR}/src/Cfa.ACHInterbank.Persistence/Cfa.ACHInterbank.Persistence.csproj"
API_PROJECT="${ROOT_DIR}/src/Cfa.ACHInterbank.Api/Cfa.ACHInterbank.Api.csproj"

RUN_FULL=false
CLEAN=false

for arg in "$@"; do
  case "$arg" in
    --full) RUN_FULL=true ;;
    --clean) CLEAN=true ;;
    *) echo "Unknown argument: $arg"; exit 1 ;;
  esac
done

log() { printf '[postgres-harness] %s\n' "$*"; }

need_cmd() { command -v "$1" >/dev/null 2>&1; }

if ! need_cmd dotnet; then
  log "dotnet is not available. Run scripts/codex/setup-codex-env.sh or install .NET SDK 10."
  exit 1
fi

if ! need_cmd docker; then
  log "docker is not available. Install Docker Desktop/Engine to run PostgreSQL integration harness."
  exit 1
fi

cleanup() {
  if [[ "$CLEAN" == "true" ]]; then
    log "Cleaning postgres test stack (docker compose down -v)..."
    docker compose -f "$COMPOSE_FILE" --env-file "$ENV_FILE" down -v
  else
    log "Leaving postgres test stack running (use --clean to tear down)."
  fi
}
trap cleanup EXIT

log "Starting PostgreSQL integration stack..."
docker compose -f "$COMPOSE_FILE" --env-file "$ENV_FILE" up -d postgres-ach-test

log "Waiting for PostgreSQL healthcheck..."
for _ in {1..60}; do
  health="$(docker inspect --format='{{json .State.Health.Status}}' achinterbank-postgres-test 2>/dev/null || true)"
  if [[ "$health" == '"healthy"' ]]; then
    break
  fi
  sleep 2
done

if [[ "${health:-}" != '"healthy"' ]]; then
  log "PostgreSQL container is not healthy."
  docker compose -f "$COMPOSE_FILE" --env-file "$ENV_FILE" ps
  docker logs achinterbank-postgres-test || true
  exit 1
fi

export ASPNETCORE_ENVIRONMENT=Test
export DOTNET_ENVIRONMENT=Test
export REQUIRE_POSTGRES_TESTS=true
export Database__Provider="${Database__Provider:-PostgreSQL}"
export POSTGRES_TEST_CONNECTION_STRING="${POSTGRES_TEST_CONNECTION_STRING:-Host=localhost;Port=${POSTGRES_PORT:-5433};Database=${POSTGRES_DB:-achinterbank_test};Username=${POSTGRES_USER:-ach_test};Password=${POSTGRES_PASSWORD:-ach_test_password}}"
export ConnectionStrings__PostgresConnection="${ConnectionStrings__PostgresConnection:-$POSTGRES_TEST_CONNECTION_STRING}"

log "Applying EF migrations on PostgreSQL..."
dotnet ef database update \
  --project "$PERSISTENCE_PROJECT" \
  --startup-project "$API_PROJECT" \
  --context AchDbContext

log "Running PostgreSQL integration tests (Category=Postgres)..."
dotnet test "$TEST_PROJECT" -c Release --no-build --filter "Category=Postgres" -v minimal

log "Running ExternalFileName tests..."
dotnet test "$TEST_PROJECT" -c Release --no-build --filter "FullyQualifiedName~ExternalFileName" -v minimal

log "Running NACHA core non-regression (60/60 baseline)..."
dotnet test "$TEST_PROJECT" -c Release --no-build --filter "FullyQualifiedName~BatchNumber|FullyQualifiedName~NachaFileBuilder|FullyQualifiedName~Mapping" -v minimal

log "Running NACHA broad non-regression (154/154 baseline)..."
dotnet test "$TEST_PROJECT" -c Release --no-build --filter "FullyQualifiedName~Nacha|FullyQualifiedName~Mapping|FullyQualifiedName~BatchNumber" -v minimal

if [[ "$RUN_FULL" == "true" ]]; then
  log "Running full test suite..."
  dotnet test "$SOLUTION" -c Release --no-build -v minimal
fi

log "PostgreSQL integration harness finished successfully."
