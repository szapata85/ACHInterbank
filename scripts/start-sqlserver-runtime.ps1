param(
    [switch]$SkipClean
)

$ErrorActionPreference = 'Stop'

$projectName = 'achinterbank-spa2'
$root = Resolve-Path (Join-Path $PSScriptRoot '..')
$composeFiles = @(
    'docker-compose.yml',
    'docker-compose.sqlserver.yml'
)

function Write-Section {
    param([Parameter(Mandatory = $true)][string]$Title)
    Write-Host ""
    Write-Host "=== $Title ==="
}

function ConvertFrom-SecureStringPlainText {
    param([Parameter(Mandatory = $true)][Security.SecureString]$SecureString)

    $bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($SecureString)
    try {
        return [Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr)
    } finally {
        if ($bstr -ne [IntPtr]::Zero) {
            [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr)
        }
    }
}

function Import-DotEnv {
    param([Parameter(Mandatory = $true)][string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        return
    }

    foreach ($line in Get-Content -LiteralPath $Path) {
        $trimmed = $line.Trim()
        if ([string]::IsNullOrWhiteSpace($trimmed) -or $trimmed.StartsWith('#')) {
            continue
        }

        $equalsIndex = $trimmed.IndexOf('=')
        if ($equalsIndex -lt 1) {
            continue
        }

        $name = $trimmed.Substring(0, $equalsIndex).Trim()
        $value = $trimmed.Substring($equalsIndex + 1).Trim()
        if (($value.StartsWith('"') -and $value.EndsWith('"')) -or ($value.StartsWith("'") -and $value.EndsWith("'"))) {
            $value = $value.Substring(1, $value.Length - 2)
        }

        if (-not [string]::IsNullOrWhiteSpace($name) -and -not (Test-Path "Env:$name")) {
            Set-Item -Path "Env:$name" -Value $value
        }
    }
}

function Set-DefaultEnv {
    param(
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][string]$DefaultValue
    )

    $currentValue = (Get-Item -Path "Env:$Name" -ErrorAction SilentlyContinue).Value
    if ([string]::IsNullOrWhiteSpace($currentValue)) {
        Set-Item -Path "Env:$Name" -Value $DefaultValue
    }
}

function Ensure-Environment {
    Set-DefaultEnv -Name 'DATABASE_PROVIDER' -DefaultValue 'SqlServer'
    Set-DefaultEnv -Name 'DATABASE_APPLY_MIGRATIONS' -DefaultValue 'true'
    Set-DefaultEnv -Name 'ASPNETCORE_ENVIRONMENT' -DefaultValue 'Development'
    Set-DefaultEnv -Name 'ASPNETCORE_URLS' -DefaultValue 'http://+:8080'
    Set-DefaultEnv -Name 'ENABLE_HTTPS_REDIRECTION' -DefaultValue 'false'
    Set-DefaultEnv -Name 'NPM_VERSION' -DefaultValue '12.0.1'
    Set-DefaultEnv -Name 'MSSQL_PID' -DefaultValue 'Developer'
    Set-DefaultEnv -Name 'SQLSERVER_HOST_PORT' -DefaultValue '1433'

    if ([string]::IsNullOrWhiteSpace($env:MSSQL_SA_PASSWORD)) {
        $securePassword = $null
        try {
            $securePassword = Read-Host -Prompt 'Enter MSSQL_SA_PASSWORD' -AsSecureString
        } catch {
            throw 'MSSQL_SA_PASSWORD is not available in the environment or .env, and the current host did not allow interactive entry.'
        }

        $plainPassword = ConvertFrom-SecureStringPlainText -SecureString $securePassword
        Set-Item -Path Env:MSSQL_SA_PASSWORD -Value $plainPassword
    }
}

function Invoke-Compose {
    param(
        [Parameter(Mandatory = $true)][string[]]$Arguments,
        [switch]$CaptureOutput
    )

    $fullArgs = @(
        '--project-name', $projectName
    )

    foreach ($composeFile in $composeFiles) {
        $fullArgs += @('-f', $composeFile)
    }

    $fullArgs += $Arguments

    if ($CaptureOutput) {
        $output = & docker compose @fullArgs 2>&1
        if ($LASTEXITCODE -ne 0) {
            throw "docker compose $($Arguments -join ' ') failed with exit code $LASTEXITCODE."
        }

        return $output
    }

    & docker compose @fullArgs
    if ($LASTEXITCODE -ne 0) {
        throw "docker compose $($Arguments -join ' ') failed with exit code $LASTEXITCODE."
    }
}

function Show-StatusAndLogs {
    Write-Section 'Compose ps --all'
    try {
        Invoke-Compose -Arguments @('ps', '--all')
    } catch {
        Write-Host $_
    }

    Write-Section 'Compose logs --no-color --tail 500'
    try {
        Invoke-Compose -Arguments @('logs', '--no-color', '--tail', '500')
    } catch {
        Write-Host $_
    }
}

function Wait-For-Health {
    $deadline = (Get-Date).AddMinutes(4)
    $readyUrl = 'http://localhost:843/health/ready'
    $liveUrl = 'http://localhost:843/health/live'
    $spaUrl = 'http://localhost:743'
    $spaHealthUrl = 'http://localhost:743/health/live'

    while ((Get-Date) -lt $deadline) {
        $liveOk = $false
        $readyOk = $false
        $spaOk = $false
        $spaHealthOk = $false

        try {
            $liveResponse = Invoke-WebRequest -UseBasicParsing -Uri $liveUrl -TimeoutSec 10
            $livePayload = $liveResponse.Content | ConvertFrom-Json
            $liveOk = $liveResponse.StatusCode -eq 200 -and $livePayload.status -eq 'Healthy' -and $livePayload.check -eq 'live' -and $livePayload.service -eq 'ACHInterbank'
        } catch {
            $liveOk = $false
        }

        try {
            $readyResponse = Invoke-WebRequest -UseBasicParsing -Uri $readyUrl -TimeoutSec 10
            $readyPayload = $readyResponse.Content | ConvertFrom-Json
            $readyOk = $readyResponse.StatusCode -eq 200 -and $readyPayload.status -eq 'Healthy' -and $readyPayload.check -eq 'ready' -and $readyPayload.database -eq 'Healthy'
        } catch {
            $readyOk = $false
        }

        try {
            $spaResponse = Invoke-WebRequest -UseBasicParsing -Uri $spaUrl -TimeoutSec 10
            $spaOk = $spaResponse.StatusCode -eq 200
        } catch {
            $spaOk = $false
        }

        try {
            $spaHealthResponse = Invoke-WebRequest -UseBasicParsing -Uri $spaHealthUrl -TimeoutSec 10
            $spaHealthPayload = $spaHealthResponse.Content | ConvertFrom-Json
            $spaHealthOk = $spaHealthResponse.StatusCode -eq 200 -and $spaHealthPayload.status -eq 'Healthy' -and $spaHealthPayload.check -eq 'live'
        } catch {
            $spaHealthOk = $false
        }

        if ($liveOk -and $readyOk -and $spaOk -and $spaHealthOk) {
            return
        }

        Start-Sleep -Seconds 5
    }

    throw 'Health checks did not become ready within the allotted time.'
}

function Get-SqlServerVersion {
    $sqlCmdCandidates = @(
        '/opt/mssql-tools18/bin/sqlcmd',
        '/opt/mssql-tools/bin/sqlcmd'
    )

    foreach ($sqlCmd in $sqlCmdCandidates) {
        try {
            $output = Invoke-Compose -Arguments @('exec', '-T', 'sqlserver', $sqlCmd, '-S', 'localhost', '-U', 'sa', '-P', $env:MSSQL_SA_PASSWORD, '-C', '-Q', 'SET NOCOUNT ON; SELECT @@VERSION;') -CaptureOutput
            return $output
        } catch {
            continue
        }
    }

    throw 'Unable to query SQL Server version from the container.'
}

function Get-MigrationHistory {
    $sqlCmdCandidates = @(
        '/opt/mssql-tools18/bin/sqlcmd',
        '/opt/mssql-tools/bin/sqlcmd'
    )

    foreach ($sqlCmd in $sqlCmdCandidates) {
        try {
            $output = Invoke-Compose -Arguments @('exec', '-T', 'sqlserver', $sqlCmd, '-S', 'localhost', '-U', 'sa', '-P', $env:MSSQL_SA_PASSWORD, '-C', '-Q', 'SELECT COUNT(*) AS MigrationCount FROM dbo.__EFMigrationsHistory;') -CaptureOutput
            return $output
        } catch {
            continue
        }
    }

    throw 'Unable to query EF migration history from the container.'
}

Push-Location $root
try {
    Import-DotEnv -Path (Join-Path $root '.env')
    Ensure-Environment

    Write-Section 'Compose config'
    Invoke-Compose -Arguments @('config', '--quiet')
    Invoke-Compose -Arguments @('config')

    if (-not $SkipClean) {
        Write-Section 'Clean project stack'
        Invoke-Compose -Arguments @('down', '--volumes', '--remove-orphans')
    }

    Write-Section 'Build'
    Invoke-Compose -Arguments @('build', '--pull', '--no-cache', '--build-arg', "NPM_VERSION=$env:NPM_VERSION")

    Write-Section 'Up'
    Invoke-Compose -Arguments @('up', '--detach', '--force-recreate', '--remove-orphans')

    Write-Section 'Status'
    Invoke-Compose -Arguments @('ps', '--all')

    Write-Section 'Health'
    Wait-For-Health

    Write-Section 'SQL Server version'
    $versionOutput = Get-SqlServerVersion
    Write-Host $versionOutput

    Write-Section 'EF migration history'
    $migrationOutput = Get-MigrationHistory
    Write-Host $migrationOutput

    Write-Section 'Final status'
    Invoke-Compose -Arguments @('ps', '--all')
} catch {
    Write-Host $_
    Show-StatusAndLogs
    throw
} finally {
    Pop-Location
}
