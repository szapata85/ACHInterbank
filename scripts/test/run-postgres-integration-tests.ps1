param(
    [switch]$Full,
    [switch]$Clean
)

$ErrorActionPreference = 'Stop'
$RootDir = (Resolve-Path (Join-Path $PSScriptRoot "../..")).Path
$ComposeFile = Join-Path $RootDir 'docker-compose.test.yml'
$EnvFile = Join-Path $RootDir '.env.test.example'
$Solution = Join-Path $RootDir 'ACHInterbank.sln'
$TestProject = Join-Path $RootDir 'tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj'
$PersistenceProject = Join-Path $RootDir 'src/Cfa.ACHInterbank.Persistence/Cfa.ACHInterbank.Persistence.csproj'
$ApiProject = Join-Path $RootDir 'src/Cfa.ACHInterbank.Api/Cfa.ACHInterbank.Api.csproj'

function Log([string]$Message) {
    Write-Host "[postgres-harness] $Message"
}

function Require-Command([string]$Name) {
    if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
        throw "$Name is not available."
    }
}

Require-Command dotnet
Require-Command docker

try {
    Log "Starting PostgreSQL integration stack..."
    docker compose -f $ComposeFile --env-file $EnvFile up -d postgres-ach-test | Out-Null

    Log "Waiting for PostgreSQL healthcheck..."
    $healthy = $false
    for ($i = 0; $i -lt 60; $i++) {
        $status = docker inspect --format='{{json .State.Health.Status}}' achinterbank-postgres-test 2>$null
        if ($status -eq '"healthy"') {
            $healthy = $true
            break
        }
        Start-Sleep -Seconds 2
    }

    if (-not $healthy) {
        docker compose -f $ComposeFile --env-file $EnvFile ps
        docker logs achinterbank-postgres-test
        throw 'PostgreSQL container is not healthy.'
    }

    $env:ASPNETCORE_ENVIRONMENT = 'Test'
    $env:DOTNET_ENVIRONMENT = 'Test'
    $env:REQUIRE_POSTGRES_TESTS = 'true'
    if (-not $env:Database__Provider) { $env:Database__Provider = 'PostgreSQL' }

    $postgresPort = if ($env:POSTGRES_PORT) { $env:POSTGRES_PORT } else { '5433' }
    $postgresDb = if ($env:POSTGRES_DB) { $env:POSTGRES_DB } else { 'achinterbank_test' }
    $postgresUser = if ($env:POSTGRES_USER) { $env:POSTGRES_USER } else { 'ach_test' }
    $postgresPassword = if ($env:POSTGRES_PASSWORD) { $env:POSTGRES_PASSWORD } else { 'ach_test_password' }

    if (-not $env:POSTGRES_TEST_CONNECTION_STRING) {
        $env:POSTGRES_TEST_CONNECTION_STRING = "Host=localhost;Port=$postgresPort;Database=$postgresDb;Username=$postgresUser;Password=$postgresPassword"
    }

    if (-not $env:ConnectionStrings__PostgresConnection) {
        $env:ConnectionStrings__PostgresConnection = $env:POSTGRES_TEST_CONNECTION_STRING
    }

    Log "Applying EF migrations on PostgreSQL..."
    dotnet ef database update --project $PersistenceProject --startup-project $ApiProject --context AchDbContext

    Log "Running PostgreSQL integration tests (Category=Postgres)..."
    dotnet test $TestProject -c Release --no-build --filter "Category=Postgres" -v minimal

    Log "Running ExternalFileName tests..."
    dotnet test $TestProject -c Release --no-build --filter "FullyQualifiedName~ExternalFileName" -v minimal

    Log "Running NACHA core non-regression (60/60 baseline)..."
    dotnet test $TestProject -c Release --no-build --filter "FullyQualifiedName~BatchNumber|FullyQualifiedName~NachaFileBuilder|FullyQualifiedName~Mapping" -v minimal

    Log "Running NACHA broad non-regression (154/154 baseline)..."
    dotnet test $TestProject -c Release --no-build --filter "FullyQualifiedName~Nacha|FullyQualifiedName~Mapping|FullyQualifiedName~BatchNumber" -v minimal

    if ($Full) {
        Log "Running full test suite..."
        dotnet test $Solution -c Release --no-build -v minimal
    }

    Log "PostgreSQL integration harness finished successfully."
}
finally {
    if ($Clean) {
        Log "Cleaning postgres test stack (docker compose down -v)..."
        docker compose -f $ComposeFile --env-file $EnvFile down -v
    }
    else {
        Log "Leaving postgres test stack running (use -Clean to tear down)."
    }
}
