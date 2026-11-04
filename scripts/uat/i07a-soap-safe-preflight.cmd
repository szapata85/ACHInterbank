@echo off
setlocal
rem I07-A: preflight exclusivamente estatico; NO API, SOAP, DB, UAT ni Live.
cd /d "%~dp0\..\.."
if not exist "ACHInterbank.sln" (
  echo ERROR: no se encontro la raiz ACHInterbank.sln
  exit /b 2
)
where python >nul 2>&1
if errorlevel 1 (
  echo NOT_RUN_ENVIRONMENT: Python 3 no disponible.
  exit /b 3
)
echo [I07-A] Validacion de solucion
python scripts/validate-solution-projects.py
if errorlevel 1 exit /b 1
if not exist artifacts mkdir artifacts
echo [I07-A] Ratchet P8.1 (solo estatico)
python scripts/validate-cfa-p8-1-static.py
if errorlevel 1 exit /b 1
echo [I07-A] VALIDACION ESTATICA TERMINADA
 echo .NET, xUnit, P8.3, P8.4, SQL y UAT oficial siguen pendientes de ejecucion separada.
echo NO se invocaron servicios SOAP ni se autorizo Live.
exit /b 0
