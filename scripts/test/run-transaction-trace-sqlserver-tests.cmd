@echo off
setlocal EnableExtensions DisableDelayedExpansion

for /f "usebackq tokens=*" %%C in (`docker compose -f docker-compose.scheduler-cluster.sqlserver.yml ps -q scheduler-sqlserver`) do set "trace_sql_container=%%C"
if not defined trace_sql_container exit /b 2

for /f "tokens=1,* delims==" %%A in ('docker inspect --format "{{range .Config.Env}}{{println .}}{{end}}" %trace_sql_container% ^| findstr /b MSSQL_SA_PASSWORD=') do set "trace_sql_password=%%B"
if not defined trace_sql_password exit /b 3

set "TRANSACTION_TRACE_SQLSERVER_CONNECTION_STRING=Server=127.0.0.1,1434;Database=master;User Id=sa;Password=%trace_sql_password%;Encrypt=True;TrustServerCertificate=True"
set "trace_sql_password="

dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --no-build --filter FullyQualifiedName=Cfa.ACHInterbank.Tests.TransactionTraceAllocationMultiDbTests.ConcurrentIndependentContexts_AllocateAndPersistDistinctDailyTraces
exit /b %ERRORLEVEL%
