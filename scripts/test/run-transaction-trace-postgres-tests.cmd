@echo off
setlocal EnableExtensions DisableDelayedExpansion

for /f "usebackq tokens=*" %%C in (`docker compose -f docker-compose.scheduler-cluster.yml ps -q scheduler-postgres`) do set "trace_pg_container=%%C"
if not defined trace_pg_container exit /b 2

for /f "tokens=1,* delims==" %%A in ('docker inspect --format "{{range .Config.Env}}{{println .}}{{end}}" %trace_pg_container% ^| findstr /b POSTGRES_PASSWORD=') do set "trace_pg_password=%%B"
if not defined trace_pg_password exit /b 3

set "TRANSACTION_TRACE_POSTGRES_CONNECTION_STRING=Host=127.0.0.1;Port=5434;Database=postgres;Username=scheduler_test;Password=%trace_pg_password%"
set "trace_pg_password="

dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --no-build --filter FullyQualifiedName=Cfa.ACHInterbank.Tests.TransactionTraceAllocationMultiDbTests.ConcurrentIndependentContexts_AllocateAndPersistDistinctDailyTraces
exit /b %ERRORLEVEL%
