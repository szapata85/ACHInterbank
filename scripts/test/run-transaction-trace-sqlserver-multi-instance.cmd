@echo off
setlocal EnableExtensions
for /f "delims=" %%G in ('git --exec-path') do set "git_exec_path=%%G"
for %%G in ("%git_exec_path%\..\..\..\bin\bash.exe") do set "git_bash=%%~fG"
if not exist "%git_bash%" exit /b 2
"%git_bash%" scripts/test/run-transaction-trace-sqlserver-multi-instance.sh
exit /b %ERRORLEVEL%
