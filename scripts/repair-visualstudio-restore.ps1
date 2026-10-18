param(
    [switch]$SkipTests
)

$ErrorActionPreference = 'Stop'
$Root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
Set-Location $Root

Write-Host '== ACHInterbank: Visual Studio / NuGet restore repair ==' -ForegroundColor Cyan

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw 'No se encontró dotnet en PATH. Instale/active el SDK .NET 10.0.402 o parche compatible.'
}

$requiredSdk = '10.0.402'
$sdks = & dotnet --list-sdks
if (-not ($sdks -match "^$([regex]::Escape($requiredSdk))\s")) {
    throw "No se encontró el SDK .NET $requiredSdk. Revise Visual Studio Installer / .NET SDK."
}

$activeSdk = (& dotnet --version).Trim()
if ($activeSdk -ne $requiredSdk) {
    throw "El SDK activo es $activeSdk; se esperaba $requiredSdk según global.json."
}

if (Get-Command python -ErrorAction SilentlyContinue) {
    & python scripts/validate-solution-projects.py
} elseif (Get-Command py -ErrorAction SilentlyContinue) {
    & py -3 scripts/validate-solution-projects.py
} else {
    throw 'Se requiere Python 3 para ejecutar scripts/validate-solution-projects.py.'
}
if ($LASTEXITCODE -ne 0) { throw 'El grafo de solución no es válido.' }

Write-Host 'Eliminando únicamente cachés locales regenerables (.vs, bin, obj)...' -ForegroundColor Yellow
if (Test-Path '.vs') { Remove-Item '.vs' -Recurse -Force }
Get-ChildItem -Path . -Directory -Recurse -Force -ErrorAction SilentlyContinue |
    Where-Object { $_.Name -in @('bin', 'obj') } |
    Sort-Object FullName -Descending |
    ForEach-Object { Remove-Item $_.FullName -Recurse -Force -ErrorAction SilentlyContinue }

& dotnet nuget locals all --clear
if ($LASTEXITCODE -ne 0) { throw 'Falló dotnet nuget locals all --clear.' }

& dotnet restore ACHInterbank.sln
if ($LASTEXITCODE -ne 0) { throw 'Falló dotnet restore ACHInterbank.sln.' }

Write-Host 'Auditando paquetes NuGet directos y transitivos...' -ForegroundColor Cyan
& dotnet list ACHInterbank.sln package --vulnerable --include-transitive
if ($LASTEXITCODE -ne 0) { throw 'Falló la auditoría de vulnerabilidades NuGet.' }

& dotnet build ACHInterbank.sln -c Release --no-restore
if ($LASTEXITCODE -ne 0) { throw 'Falló dotnet build ACHInterbank.sln -c Release.' }

if (-not $SkipTests) {
    & dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --no-build
    if ($LASTEXITCODE -ne 0) { throw 'Fallaron las pruebas backend.' }
}

Write-Host 'Restore/build finalizados. Abra ACHInterbank.sln en Visual Studio 2026.' -ForegroundColor Green
