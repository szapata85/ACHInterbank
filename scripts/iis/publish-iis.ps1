# Publica la API + Angular en un unico sitio IIS (sin nginx/ARR/Docker).
# Requiere .NET SDK 10, Node.js compatible y npm en Windows; no modifica la BD.
[CmdletBinding()]
param(
    [string]$OutputDirectory = 'artifacts\publish\iis',
    [switch]$SkipNpmCi
)

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$apiProject = Join-Path $repoRoot 'src\Cfa.ACHInterbank.Api\Cfa.ACHInterbank.Api.csproj'
$angularRoot = Join-Path $repoRoot 'web\ach-interbank-ui'
$angularDist = Join-Path $angularRoot 'dist\ach-interbank-ui'
if ([System.IO.Path]::IsPathRooted($OutputDirectory)) {
    $outputPath = [System.IO.Path]::GetFullPath($OutputDirectory)
} else {
    $outputPath = [System.IO.Path]::GetFullPath((Join-Path $repoRoot $OutputDirectory))
}

if ((Test-Path -LiteralPath $outputPath) -and @(Get-ChildItem -LiteralPath $outputPath -Force).Count -gt 0) {
    throw "La salida ya contiene archivos: $outputPath. Use una carpeta VACIA para no mezclar versiones de la SPA."
}

foreach ($command in @('dotnet', 'npm.cmd')) {
    if (-not (Get-Command $command -ErrorAction SilentlyContinue)) {
        throw "No se encontro '$command' en PATH. Instale el SDK/herramienta antes de publicar."
    }
}

New-Item -ItemType Directory -Path $outputPath -Force | Out-Null
Write-Host '[1/4] Publicando ASP.NET Core 10 (IIS InProcess)...'
& dotnet publish $apiProject --configuration Release --output $outputPath --self-contained false '-p:AspNetCoreHostingModel=InProcess'
if ($LASTEXITCODE -ne 0) { throw 'Fallo dotnet publish. No despliegue esta salida.' }

Write-Host '[2/4] Compilando Angular para produccion...'
Push-Location $angularRoot
try {
    if (-not $SkipNpmCi) {
        & npm.cmd ci
        if ($LASTEXITCODE -ne 0) { throw 'Fallo npm ci.' }
    }
    & npm.cmd run build -- --configuration production
    if ($LASTEXITCODE -ne 0) { throw 'Fallo npm run build.' }
} finally {
    Pop-Location
}
if (-not (Test-Path -LiteralPath (Join-Path $angularDist 'index.html'))) {
    throw "Angular no genero index.html en $angularDist. No despliegue esta salida."
}

Write-Host '[3/4] Copiando Angular a wwwroot...'
$webRoot = Join-Path $outputPath 'wwwroot'
New-Item -ItemType Directory -Path $webRoot -Force | Out-Null
Copy-Item -Path (Join-Path $angularDist '*') -Destination $webRoot -Recurse -Force

Write-Host '[4/4] Preparando appsettings.json y web.config sin variables de entorno...'
# SPA habilitada unicamente en la carpeta publicada para IIS; el codigo fuente
# conserva SpaHosting.Enabled=false para los escenarios Docker.
$publishedSettingsPath = Join-Path $outputPath 'appsettings.json'
if (-not (Test-Path -LiteralPath $publishedSettingsPath)) {
    throw 'Falta appsettings.json en la salida de dotnet publish.'
}
# Modificar solo esta propiedad, preservando todas las anotaciones del JSON.
. (Join-Path $repoRoot 'scripts/jsonc-settings.ps1')
$publishedSettings = Read-JsoncSettings -Path $publishedSettingsPath
if ($null -eq $publishedSettings.SpaHosting) { throw 'No existe la seccion SpaHosting en appsettings.json.' }
Set-JsoncSetting -Path $publishedSettingsPath -Section 'SpaHosting' -Name 'Enabled' -Value $true

$webConfigPath = Join-Path $outputPath 'web.config'
if (-not (Test-Path -LiteralPath $webConfigPath)) {
    throw 'dotnet publish no genero web.config de ASP.NET Core Module (ANCM). Verifique el Hosting Bundle.'
}
[xml]$doc = Get-Content -LiteralPath $webConfigPath -Raw -Encoding UTF8
$aspNetCore = $doc.SelectSingleNode('/configuration/system.webServer/aspNetCore')
if ($null -eq $aspNetCore) { throw 'No existe aspNetCore en web.config.' }
$aspNetCore.SetAttribute('hostingModel', 'inprocess')
# No publicar configuracion de la aplicacion mediante environmentVariables:
# Database.ApplyMigrations y Database.ApplySeed se leen de appsettings.json.
$environmentVariables = $aspNetCore.SelectSingleNode('environmentVariables')
if ($null -ne $environmentVariables) {
    [void]$aspNetCore.RemoveChild($environmentVariables)
}
$doc.Save($webConfigPath)

Write-Host "Publicacion IIS lista: $outputPath"
Write-Host 'Configure ConnectionStrings:SqlConnection y otros valores en appsettings.json publicado (proteja el archivo con ACL NTFS).'
Write-Host 'Database:ApplyMigrations=true en appsettings.json: la API aplicara migraciones al iniciar. Tras validar, cambie a false y reinicie el sitio.'
Write-Host 'No se instalaron servicios, no se ejecutaron operaciones ACH y no se modifico ninguna base de datos.'
