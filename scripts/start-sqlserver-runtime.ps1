param(
    [switch]$ResetData
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
            throw 'MSSQL_SA_PASSWORD is required and was not found in the environment or .env file.'
        }

        $plainPassword = ConvertFrom-SecureStringPlainText -SecureString $securePassword
        Set-Item -Path Env:MSSQL_SA_PASSWORD -Value $plainPassword
    }

    Set-DefaultEnv -Name 'ACH_E2E_DB_PROVIDER' -DefaultValue 'SqlServer'
    Set-DefaultEnv -Name 'ACH_E2E_SQLSERVER_HOST' -DefaultValue '127.0.0.1'
    Set-DefaultEnv -Name 'ACH_E2E_SQLSERVER_PORT' -DefaultValue $env:SQLSERVER_HOST_PORT
    Set-DefaultEnv -Name 'ACH_E2E_SQLSERVER_DATABASE' -DefaultValue 'ACHInterbank'
    Set-DefaultEnv -Name 'ACH_E2E_SQLSERVER_USER' -DefaultValue 'sa'
    if ([string]::IsNullOrWhiteSpace($env:ACH_E2E_SQLSERVER_PASSWORD)) {
        Set-Item -Path Env:ACH_E2E_SQLSERVER_PASSWORD -Value $env:MSSQL_SA_PASSWORD
    }
}

function Redact-Text {
    param([string]$Text)

    if ([string]::IsNullOrWhiteSpace($Text)) {
        return $Text
    }

    $redacted = $Text
    $patterns = @(
        '(?i)(MSSQL_SA_PASSWORD\s*[:=]\s*)([^\s;]+)',
        '(?i)(ACH_E2E_SQLSERVER_PASSWORD\s*[:=]\s*)([^\s;]+)',
        '(?i)(ACH_E2E_SQLSERVER_CONNECTION_STRING\s*[:=]\s*)([^\r\n]+)',
        '(?i)(POSTGRES_PASSWORD\s*[:=]\s*)([^\s;]+)',
        '(?i)(ConnectionStrings__SqlConnection\s*[:=]\s*)([^\r\n]+)',
        '(?i)(ConnectionStrings__PostgresConnection\s*[:=]\s*)([^\r\n]+)',
        '(?i)(Password=)([^;]+)',
        '(?i)(secretKetJwt\s*[:=]\s*)([^\s;]+)',
        '(?i)(clientSecret\s*[:=]\s*)([^\s;]+)',
        '(?i)(x_api_key\s*[:=]\s*)([^\s;]+)'
    )

    foreach ($pattern in $patterns) {
        $redacted = [regex]::Replace($redacted, $pattern, '$1[REDACTED]')
    }

    return $redacted
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

    $previousErrorActionPreference = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        $output = & docker compose @fullArgs 2>&1
        $exitCode = $LASTEXITCODE
    } finally {
        $ErrorActionPreference = $previousErrorActionPreference
    }

    $renderedOutput = Redact-Text (($output | Out-String).TrimEnd())

    if ($exitCode -ne 0) {
        throw $renderedOutput
    }

    if ($CaptureOutput) {
        return $renderedOutput
    }

    if (-not [string]::IsNullOrWhiteSpace($renderedOutput)) {
        Write-Host $renderedOutput
    }
}

function Get-SqlCmdPath {
    $candidates = @(
        '/opt/mssql-tools18/bin/sqlcmd',
        '/opt/mssql-tools/bin/sqlcmd'
    )

    foreach ($candidate in $candidates) {
        try {
            Invoke-Compose -Arguments @('exec', '-T', 'sqlserver', 'sh', '-lc', "test -x $candidate")
            return $candidate
        } catch {
            continue
        }
    }

    throw 'Unable to locate sqlcmd in the SQL Server container.'
}

function Show-StatusAndLogs {
    Write-Section 'Compose ps --all'
    try {
        Invoke-Compose -Arguments @('ps', '--all')
    } catch {
        Write-Host (Redact-Text ($_.Exception.Message))
    }

    Write-Section 'Compose logs --no-color --tail 500'
    try {
        Invoke-Compose -Arguments @('logs', '--no-color', '--tail', '500')
    } catch {
        Write-Host (Redact-Text ($_.Exception.Message))
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

function Invoke-SqlCmdInContainer {
    param(
        [Parameter(Mandatory = $true)][string]$Query,
        [Parameter(Mandatory = $true)][string]$Database
    )

    $shellDatabase = $Database.Trim() -replace '"', '\"'
    $sqlCmd = Get-SqlCmdPath
    $hostTempFile = [System.IO.Path]::GetTempFileName()
    $containerQueryFile = '/tmp/achinterbank-sqlcmd-query.sql'

    try {
        Set-Content -LiteralPath $hostTempFile -Value $Query -NoNewline -Encoding utf8
        & docker cp $hostTempFile "achinterbank-sqlserver:$containerQueryFile" | Out-Null
        if ($LASTEXITCODE -ne 0) {
            throw "docker cp $hostTempFile achinterbank-sqlserver:$containerQueryFile failed with exit code $LASTEXITCODE."
        }

        $shellScript = 'SQLCMDPASSWORD="$MSSQL_SA_PASSWORD" ' + $sqlCmd + ' -S localhost -U sa -d ' + $shellDatabase + ' -C -i ' + $containerQueryFile
        return Invoke-Compose -Arguments @('exec', '-T', 'sqlserver', 'sh', '-lc', $shellScript) -CaptureOutput
    } finally {
        Remove-Item -LiteralPath $hostTempFile -ErrorAction SilentlyContinue
    }
}

function Get-SqlServerVersion {
    return Invoke-SqlCmdInContainer -Database 'master' -Query 'SET NOCOUNT ON; SELECT @@VERSION;'
}

function Get-EfMigrationsTableState {
    $query = @"
SET NOCOUNT ON;
SELECT s.name AS SchemaName, t.name AS TableName
FROM sys.tables AS t
INNER JOIN sys.schemas AS s ON s.schema_id = t.schema_id
WHERE t.name = '__EFMigrationsHistory';
"@

    return Invoke-SqlCmdInContainer -Database 'ACHInterbank' -Query $query
}

Push-Location $root
try {
    Import-DotEnv -Path (Join-Path $root '.env')
    Ensure-Environment

    Write-Section 'Compose config validation'
    Invoke-Compose -Arguments @('config', '--quiet')

    if ($ResetData) {
        Write-Section 'Clean project stack with data reset'
        Invoke-Compose -Arguments @('down', '--volumes', '--remove-orphans')
    } else {
        Write-Section 'Clean project stack'
        Invoke-Compose -Arguments @('down', '--remove-orphans')
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
    $migrationOutput = Get-EfMigrationsTableState
    if ([string]::IsNullOrWhiteSpace($migrationOutput)) {
        Write-Host 'No __EFMigrationsHistory table found.'
    } else {
        Write-Host $migrationOutput
    }

    Write-Section 'Final status'
    Invoke-Compose -Arguments @('ps', '--all')
} catch {
    Write-Host (Redact-Text ($_.Exception.Message))
    Show-StatusAndLogs
    throw
} finally {
    Pop-Location
}
