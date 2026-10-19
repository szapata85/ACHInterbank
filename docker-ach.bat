@echo off
setlocal EnableExtensions DisableDelayedExpansion
rem ACHInterbank NAV6 - Docker SQL Server (sin PowerShell)
rem Requiere Docker Desktop, Docker Compose y .NET SDK 10.
rem Ejemplos:
rem   docker-ach.cmd config
rem   docker-ach.cmd migrate
rem   docker-ach.cmd up --seed --no-auto-migrations --no-build
rem   docker-ach.cmd status
rem   docker-ach.cmd logs
rem   docker-ach.cmd down
rem   docker-ach.cmd reset --confirm-data-loss (BORRA datos de SQL Server)

set "ROOT=%~dp0"
set "CLI=%ROOT%tools\AchInterbank.DockerCli\AchInterbank.DockerCli.csproj"

if not exist "%CLI%" (
    echo ERROR: No se encuentra el ejecutor .NET: "%CLI%"
    exit /b 1
)

where dotnet >nul 2>&1
if errorlevel 1 (
    echo ERROR: No se encuentra dotnet. Instale .NET SDK 10.
    exit /b 1
)

pushd "%ROOT%" >nul 2>&1
if errorlevel 1 (
    echo ERROR: No se puede acceder a la carpeta del proyecto: "%ROOT%"
    exit /b 1
)

dotnet run --project "%CLI%" -- %*
set "RESULTADO=%ERRORLEVEL%"

popd
exit /b %RESULTADO%
