# Ejecutar en PowerShell desde ACHInterbank_final; no modifica BD.
$ErrorActionPreference = 'Continue'
# Keep native tool warnings (including NuGet NU1510) in logs, never treat them as
# PowerShell terminating errors. The exit code remains authoritative.
if (Get-Variable PSNativeCommandUseErrorActionPreference -ErrorAction SilentlyContinue) {
    $PSNativeCommandUseErrorActionPreference = $false
}
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$dir = Join-Path (Get-Location) "logs\seed-audit-$stamp"
New-Item -ItemType Directory -Path $dir -Force | Out-Null

function Exec-Logged([string]$title, [scriptblock]$action) {
    $file = Join-Path $dir "$title.log"
    Write-Host "==== $title ===="
    & $action 2>&1 | Tee-Object -FilePath $file
    if ($LASTEXITCODE -ne 0) { throw "Falló $title (exit $LASTEXITCODE). Revisar $file" }
}
Exec-Logged '01-build' { dotnet build .\ACHInterbank.sln -c Debug }
Exec-Logged '02-unit-tests-seed' { dotnet test .\tests\Cfa.ACHInterbank.Tests\Cfa.ACHInterbank.Tests.csproj -c Debug --no-build --filter 'FullyQualifiedName~IntegrationBootstrapperTests|FullyQualifiedName~SoapIntegrationSettingsBootstrapperTests|FullyQualifiedName~NavigationMenuNav22ParametersInformationArchitectureTests|FullyQualifiedName~NavigationMenuNav21SeedInfrastructureTests' }
Exec-Logged '03-ef-pending' { dotnet ef migrations has-pending-model-changes --project .\src\Cfa.ACHInterbank.Persistence.Migrations.SqlServer --startup-project .\src\Cfa.ACHInterbank.Api --context AchDbContext }
Write-Host "Evidencia en: $dir"
