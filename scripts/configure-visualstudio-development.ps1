[CmdletBinding()]
param(
    [ValidateSet('SqlServer', 'Postgres')]
    [string]$Provider = 'SqlServer',

    [string]$HostName = 'localhost',
    [int]$Port = 0,
    [string]$Database = 'ACHInterbank',
    [string]$UserName = '',
    [Security.SecureString]$Password,

    [switch]$IntegratedSecurity,
    [switch]$StartDocker,
    [switch]$SkipMigrations,
    [switch]$ApplySeed
)

$ErrorActionPreference = 'Stop'
$root = Resolve-Path (Join-Path $PSScriptRoot '..')
$appSettingsPath = Join-Path $root 'src/Cfa.ACHInterbank.Api/appsettings.Development.json'

function ConvertFrom-SecureStringPlainText {
    param([Parameter(Mandatory = $true)][Security.SecureString]$Value)

    $ptr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($Value)
    try {
        return [Runtime.InteropServices.Marshal]::PtrToStringBSTR($ptr)
    }
    finally {
        if ($ptr -ne [IntPtr]::Zero) {
            [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($ptr)
        }
    }
}

function Import-DotEnv {
    param([Parameter(Mandatory = $true)][string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) { return }

    foreach ($line in Get-Content -LiteralPath $Path) {
        $trimmed = $line.Trim()
        if ([string]::IsNullOrWhiteSpace($trimmed) -or $trimmed.StartsWith('#')) { continue }
        $equalsIndex = $trimmed.IndexOf('=')
        if ($equalsIndex -lt 1) { continue }

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

function Save-DevelopmentSettings {
    param(
        [Parameter(Mandatory = $true)][string]$ProviderValue,
        [Parameter(Mandatory = $true)][string]$ConnectionKey,
        [Parameter(Mandatory = $true)][string]$ConnectionValue
    )

    if (-not (Test-Path -LiteralPath $appSettingsPath)) {
        throw "Development appsettings not found: $appSettingsPath"
    }

    $config = Get-Content -LiteralPath $appSettingsPath -Raw | ConvertFrom-Json

    if ($null -eq $config.Database) {
        $config | Add-Member -NotePropertyName Database -NotePropertyValue ([pscustomobject]@{})
    }
    if ($null -eq $config.ConnectionStrings) {
        $config | Add-Member -NotePropertyName ConnectionStrings -NotePropertyValue ([pscustomobject]@{})
    }

    $config.Database.Provider = $ProviderValue
    if ($config.Database.PSObject.Properties.Name -contains 'ApplyMigrations') {
        $config.Database.ApplyMigrations = -not $SkipMigrations
    }
    else {
        $config.Database | Add-Member -NotePropertyName ApplyMigrations -NotePropertyValue (-not $SkipMigrations)
    }
    if ($config.Database.PSObject.Properties.Name -contains 'ApplySeed') {
        $config.Database.ApplySeed = $ApplySeed.IsPresent
    }
    else {
        $config.Database | Add-Member -NotePropertyName ApplySeed -NotePropertyValue $ApplySeed.IsPresent
    }

    if (-not ($config.ConnectionStrings.PSObject.Properties.Name -contains 'PostgresConnection')) {
        $config.ConnectionStrings | Add-Member -NotePropertyName PostgresConnection -NotePropertyValue ''
    }
    if (-not ($config.ConnectionStrings.PSObject.Properties.Name -contains 'SqlConnection')) {
        $config.ConnectionStrings | Add-Member -NotePropertyName SqlConnection -NotePropertyValue ''
    }

    $config.ConnectionStrings.$ConnectionKey = $ConnectionValue

    # Keep the other provider value untouched so a developer may switch providers deliberately.
    $json = $config | ConvertTo-Json -Depth 100
    [System.IO.File]::WriteAllText($appSettingsPath, $json + [Environment]::NewLine, [System.Text.UTF8Encoding]::new($false))
}

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw 'dotnet was not found in PATH. Install/repair the .NET 10 SDK workload in Visual Studio 2026.'
}

Import-DotEnv -Path (Join-Path $root '.env')

$providerNormalized = $Provider.ToLowerInvariant()
if ($providerNormalized -eq 'sqlserver') {
    if ($Port -le 0) { $Port = 1433 }
    if ([string]::IsNullOrWhiteSpace($UserName)) { $UserName = 'sa' }

    if ($StartDocker) {
        if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
            throw 'Docker was requested but the docker command was not found.'
        }

        if (-not $Password -and -not [string]::IsNullOrWhiteSpace($env:MSSQL_SA_PASSWORD)) {
            $Password = ConvertTo-SecureString $env:MSSQL_SA_PASSWORD -AsPlainText -Force
        }
        if (-not $Password) {
            $Password = Read-Host -Prompt 'MSSQL_SA_PASSWORD for the local ACHInterbank SQL Server container' -AsSecureString
        }

        $plainPasswordForDocker = ConvertFrom-SecureStringPlainText $Password
        try {
            $env:MSSQL_SA_PASSWORD = $plainPasswordForDocker
            if ([string]::IsNullOrWhiteSpace($env:MSSQL_DB)) { $env:MSSQL_DB = $Database }
            if ([string]::IsNullOrWhiteSpace($env:SQLSERVER_HOST_PORT)) { $env:SQLSERVER_HOST_PORT = [string]$Port }

            Push-Location $root
            try {
                & docker compose -f docker-compose.yml -f docker-compose.sqlserver.yml up -d sqlserver quartz-schema
                if ($LASTEXITCODE -ne 0) { throw 'Unable to start the SQL Server development services.' }
            }
            finally { Pop-Location }
        }
        finally { $plainPasswordForDocker = $null }
    }

    if ($IntegratedSecurity) {
        $connectionString = "Server=$HostName,$Port;Database=$Database;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
    }
    else {
        if (-not $Password -and -not [string]::IsNullOrWhiteSpace($env:MSSQL_SA_PASSWORD)) {
            $Password = ConvertTo-SecureString $env:MSSQL_SA_PASSWORD -AsPlainText -Force
        }
        if (-not $Password) {
            $Password = Read-Host -Prompt "Password for SQL Server user '$UserName'" -AsSecureString
        }

        $plainPassword = ConvertFrom-SecureStringPlainText $Password
        try {
            $connectionString = "Server=$HostName,$Port;Database=$Database;User Id=$UserName;Password=$plainPassword;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
        }
        finally { $plainPassword = $null }
    }

    Save-DevelopmentSettings -ProviderValue 'SqlServer' -ConnectionKey 'SqlConnection' -ConnectionValue $connectionString
}
else {
    if ($IntegratedSecurity) { throw '-IntegratedSecurity is only supported by this helper for SQL Server.' }
    if ($Port -le 0) { $Port = 5432 }
    if ([string]::IsNullOrWhiteSpace($UserName)) { $UserName = 'postgres' }

    if (-not $Password -and -not [string]::IsNullOrWhiteSpace($env:POSTGRES_PASSWORD)) {
        $Password = ConvertTo-SecureString $env:POSTGRES_PASSWORD -AsPlainText -Force
    }
    if (-not $Password) {
        $Password = Read-Host -Prompt "Password for PostgreSQL user '$UserName'" -AsSecureString
    }

    $plainPassword = ConvertFrom-SecureStringPlainText $Password
    try {
        $connectionString = "Host=$HostName;Port=$Port;Database=$Database;Username=$UserName;Password=$plainPassword"
    }
    finally { $plainPassword = $null }

    Save-DevelopmentSettings -ProviderValue 'Postgres' -ConnectionKey 'PostgresConnection' -ConnectionValue $connectionString
}

Write-Host ''
Write-Host 'Visual Studio Development database configuration is ready.' -ForegroundColor Green
Write-Host "Provider: $Provider"
Write-Host "Server:   $HostName`:$Port"
Write-Host "Database: $Database"
Write-Host "Migrate:  $(-not $SkipMigrations)"
Write-Host "Seed:     $($ApplySeed.IsPresent)"
Write-Host "File:     $appSettingsPath"
Write-Host ''
Write-Host 'ConnectionStrings are read from appsettings.Development.json as requested.' -ForegroundColor Yellow
Write-Host 'Do not commit real database passwords to Git.' -ForegroundColor Yellow
Write-Host 'Restart the API profile in Visual Studio (https/http/IIS Express).'
