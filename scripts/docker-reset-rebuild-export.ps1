[CmdletBinding()]
param(
    [string]$Password = $env:MSSQL_SA_PASSWORD,
    [int]$TimeoutSeconds = 900
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repoRoot = Split-Path -Parent $PSScriptRoot
Set-Location $repoRoot
$composeFile = Join-Path $repoRoot 'docker-compose.yml'
$portainerFile = Join-Path $repoRoot 'dist/portainer/docker-compose.yml'
$envExample = Join-Path $repoRoot 'dist/portainer/stack.env.example'
$exportTar = Join-Path $repoRoot 'dist/portainer/ach_interbank.tar'
$hashFile = Join-Path $repoRoot 'dist/portainer/ach_interbank.tar.sha256'
$manifestFile = Join-Path $repoRoot 'dist/portainer/images-manifest.txt'
$timestamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$inventoryDir = Join-Path $repoRoot 'dist/docker-evidence'
New-Item -ItemType Directory -Force -Path $inventoryDir | Out-Null

function Invoke-DockerChecked {
    param([Parameter(Mandatory)][string[]]$Arguments)
    & docker @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Docker command failed ($LASTEXITCODE): docker $($Arguments -join ' ')"
    }
}

function Get-DockerIds {
    param([Parameter(Mandatory)][string[]]$Arguments)
    $result = @(& docker @Arguments)
    if ($LASTEXITCODE -ne 0) { throw "Docker inventory command failed: docker $($Arguments -join ' ')" }
    return @($result | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
}

if (-not (Get-Command docker -ErrorAction SilentlyContinue)) { throw 'Docker CLI is not available.' }
Invoke-DockerChecked @('version')

docker ps -a | Out-File (Join-Path $inventoryDir "before-$timestamp-containers.txt")
docker image ls --digests | Out-File (Join-Path $inventoryDir "before-$timestamp-images.txt")
docker volume ls | Out-File (Join-Path $inventoryDir "before-$timestamp-volumes.txt")
docker network ls | Out-File (Join-Path $inventoryDir "before-$timestamp-networks.txt")
docker system df | Out-File (Join-Path $inventoryDir "before-$timestamp-df.txt")

$containers = @(Get-DockerIds @('ps', '-aq'))
if ($containers.Count -gt 0) { Invoke-DockerChecked (@('rm', '-f') + $containers) }
$volumes = @(Get-DockerIds @('volume', 'ls', '-q'))
if ($volumes.Count -gt 0) { Invoke-DockerChecked (@('volume', 'rm', '-f') + $volumes) }
$images = @(Get-DockerIds @('image', 'ls', '-aq'))
if ($images.Count -gt 0) { Invoke-DockerChecked (@('image', 'rm', '-f') + $images) }
Invoke-DockerChecked @('network', 'prune', '-f')
Invoke-DockerChecked @('builder', 'prune', '-af')
Invoke-DockerChecked @('system', 'prune', '-af', '--volumes')

if ((@(docker ps -aq)).Count -gt 0) { throw 'Containers remain after cleanup.' }
if ((@(docker image ls -aq)).Count -gt 0) { throw 'Images remain after cleanup.' }
if ((@(docker volume ls -q)).Count -gt 0) { throw 'Volumes remain after cleanup.' }
docker ps -a | Out-File (Join-Path $inventoryDir "after-$timestamp-containers.txt")
docker image ls | Out-File (Join-Path $inventoryDir "after-$timestamp-images.txt")
docker volume ls | Out-File (Join-Path $inventoryDir "after-$timestamp-volumes.txt")
docker network ls | Out-File (Join-Path $inventoryDir "after-$timestamp-networks.txt")
docker system df | Out-File (Join-Path $inventoryDir "after-$timestamp-df.txt")

if ([string]::IsNullOrWhiteSpace($Password) -or $Password -eq 'REEMPLAZAR_CON_CLAVE_SEGURA') {
    throw 'Set MSSQL_SA_PASSWORD in the process environment or pass -Password. It is never written to the repository.'
}
$env:MSSQL_SA_PASSWORD = $Password

Invoke-DockerChecked @('pull', 'mcr.microsoft.com/mssql/server:2025-latest')
Invoke-DockerChecked @('compose', '-f', $composeFile, 'config', '--quiet')
Invoke-DockerChecked @('compose', '-f', $composeFile, 'build', '--pull', '--no-cache')
Invoke-DockerChecked @('compose', '-f', $composeFile, 'up', '-d', '--force-recreate', '--remove-orphans')

$deadline = (Get-Date).AddSeconds($TimeoutSeconds)
do {
    $serviceContainers = @('sqlserver', 'achinterbank-api', 'achinterbank-spa') | ForEach-Object {
        $id = docker compose -f $composeFile ps -q $_
        if (-not [string]::IsNullOrWhiteSpace($id)) {
            $state = docker inspect --format '{{.State.Status}}|{{if .State.Health}}{{.State.Health.Status}}{{else}}none{{end}}' $id
            [pscustomobject]@{ State = ($state -split '\|')[0]; Health = ($state -split '\|')[1] }
        }
    }
    $healthy = @($serviceContainers | Where-Object { $_.Health -eq 'healthy' }).Count
    $running = @($serviceContainers | Where-Object { $_.State -eq 'running' }).Count
    if ($running -ge 3 -and $healthy -ge 3) { break }
    if ((Get-Date) -ge $deadline) { docker compose -f $composeFile ps; docker compose -f $composeFile logs --tail 300; throw 'Timed out waiting for the Stack.' }
    Start-Sleep -Seconds 5
} while ($true)

Invoke-DockerChecked @('compose', '-f', $composeFile, 'exec', '-T', 'sqlserver', '/opt/mssql-tools18/bin/sqlcmd', '-S', 'localhost', '-U', 'sa', '-P', $Password, '-C', '-Q', "SELECT @@VERSION; SELECT SERVERPROPERTY('ProductVersion'); SELECT SERVERPROPERTY('ProductMajorVersion'); SELECT SERVERPROPERTY('Edition');")
Invoke-DockerChecked @('compose', '-f', $composeFile, 'restart')
Start-Sleep -Seconds 10
Invoke-DockerChecked @('compose', '-f', $composeFile, 'ps')
Invoke-DockerChecked @('compose', '-f', $composeFile, 'logs', '--tail', '300')
Invoke-DockerChecked @('stats', '--no-stream')

$imagesToSave = @(& docker compose --env-file $envExample -f $portainerFile config --images | Sort-Object -Unique)
if ($LASTEXITCODE -ne 0 -or $imagesToSave.Count -eq 0) { throw 'Could not resolve images from the Portainer Compose.' }
foreach ($image in $imagesToSave) {
    Invoke-DockerChecked @('image', 'inspect', $image)
}
Invoke-DockerChecked (@('image', 'save', '--output', $exportTar) + $imagesToSave)
Get-FileHash -Path $exportTar -Algorithm SHA256 | ForEach-Object { "$($_.Hash)  ach_interbank.tar" } | Set-Content $hashFile

"GeneratedUtc=$((Get-Date).ToUniversalTime().ToString('o'))" | Set-Content $manifestFile
foreach ($image in $imagesToSave) {
    $inspect = docker image inspect $image | ConvertFrom-Json
    $item = $inspect[0]
    $repoDigest = @($item.RepoDigests) -join ','
    Add-Content $manifestFile ("image=$image; id=$($item.Id); digest=$repoDigest; size=$($item.Size); created=$($item.Created); services=resolved-from-dist/portainer/docker-compose.yml")
}

Invoke-DockerChecked @('image', 'rm', '-f', 'achinterbank-api:2026.07.24', 'achinterbank-spa:2026.07.24', 'achinterbank-quartz-schema:2026.07.24')
Invoke-DockerChecked @('load', '--input', $exportTar)
Invoke-DockerChecked @('compose', '--env-file', $envExample, '-f', $portainerFile, 'up', '-d', '--no-build', '--force-recreate')
Invoke-DockerChecked @('compose', '--env-file', $envExample, '-f', $portainerFile, 'ps')

Write-Output 'RESULTADO: RECONSTRUCCION Y EXPORTACION COMPLETADAS'
Write-Output "TAR: $exportTar"
Write-Output "SHA256: $hashFile"
Write-Output "MANIFEST: $manifestFile"
