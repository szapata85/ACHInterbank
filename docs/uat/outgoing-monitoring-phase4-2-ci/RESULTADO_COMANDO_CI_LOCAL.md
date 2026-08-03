# Resultado del comando exacto de CI local

## Build previo

`dotnet build ACHInterbank.sln -c Release --no-restore`

- 0 advertencias.
- 0 errores.

## Comando equivalente al workflow

```powershell
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj `
  -c Release `
  --no-build `
  --filter "Category!=ClearingHouseMultiDb&Category!=OutgoingMonitorMultiDb" `
  --logger "trx;LogFileName=dotnet-tests.trx" `
  --results-directory TestResults `
  -- RunConfiguration.MaxCpuCount=1
```

Resultado:

- aprobadas: 2.113;
- fallidas: 0;
- omitidas: 7;
- total: 2.120;
- duracion informada: 21m08s;
- TRX: `TestResults/dotnet-tests.trx`.

Las siete omisiones corresponden a las pruebas opt-in preexistentes mostradas por el runner; no se agregaron categorias, `Skip`, exclusiones ni `continue-on-error`.

## Frontend y Docker

- Angular build: aprobado.
- ChromeHeadless: 685 aprobadas, 0 fallidas.
- API SQL Server live/ready: HTTP 200/200.
- SPA SQL Server: HTTP 200.
- API PostgreSQL live/ready: HTTP 200/200.
- SPA PostgreSQL: HTTP 200.
- SQL Server y PostgreSQL: contenedores saludables.
- Coincidencias de error/duplicado de bootstrap en logs finales: 0 por motor.

## GitHub Actions

`gh run view 30823343497` no pudo ejecutarse porque GitHub CLI no estaba autenticado. No se declara CI remoto verde. Un nuevo workflow queda pendiente de publicar el commit local.
