# Playwright Proc_Contrapartidas con SOAP local

## Objetivo

Validar en entorno local/UAT que una transaccion debito creada desde la SPA en `/transactions` queda originada por CFA (`IsDefaultSource=true`) y, al procesarse, invoca `Proc_Contrapartidas` contra el SOAP legacy local `WSCFAACH.svc`.

La prueba es opt-in porque `Proc_Contrapartidas` es candidato monetario. Debe usarse solo con datos sinteticos y SOAP local controlado.

## Configuracion requerida

Modo live del backend:

```powershell
$env:ProcContrapartidas__Mode = "Live"
```

Clave equivalente en `appsettings`:

```json
{
  "ProcContrapartidas": {
    "Mode": "Live"
  }
}
```

En `appsettings.Development.json` el default local queda en `Live` para esta validacion. `DryRun` sigue disponible como rollback/fallback:

```powershell
$env:ProcContrapartidas__Mode = "DryRun"
```

No cambiar `ProcTransacciones__Mode`; este escenario no debe ejecutar `Proc_Transacciones`.

En este runtime los endpoints SOAP se toman de la configuracion persistida expuesta por `api/users/soap-integrations`. El spec la ajusta temporalmente con:

- `SOAP_LOCAL_WSCFAACH_URL`
- `SOAP_LOCAL_AXON_RESPONSE_URL`

Si la API corre en Docker y el SOAP corre en el host:

```powershell
$env:SOAP_LOCAL_WSCFAACH_URL = "http://host.docker.internal:7083/WSCFAACH.svc"
$env:SOAP_LOCAL_AXON_RESPONSE_URL = "http://host.docker.internal:7083/WSAxonRespuestaTransacciones.svc"
$env:WSCFAACH__HostHeader = "localhost:7083"
```

`WSCFAACH__HostHeader` es necesario cuando IIS/WCF rechaza `Host: host.docker.internal:7083` con `HTTP 400 Invalid Hostname`. Si la API corre en host, usar `http://localhost:7083/...` y no se requiere ese override.

## Variables Playwright

```powershell
$env:RUN_LOCAL_SOAP_PROC_CONTRAPARTIDAS_E2E = "true"
$env:ALLOW_LOCAL_MONETARY_SOAP_E2E = "true"
$env:ProcContrapartidas__Mode = "Live"
$env:ACH_E2E_DB_PROVIDER = "SqlServer"
$env:ACH_UI_URL = "http://localhost:743"
$env:ACH_API_URL = "http://localhost:843"
$env:ACH_USER = "<usuario-local-uat>"
$env:ACH_PASS = "<password-local-uat>"
$env:SOAP_LOCAL_WSCFAACH_URL = "http://host.docker.internal:7083/WSCFAACH.svc"
$env:SOAP_LOCAL_AXON_RESPONSE_URL = "http://host.docker.internal:7083/WSAxonRespuestaTransacciones.svc"
$env:SOAP_LOCAL_LOG_DIR = "C:\WebServices\WSCFAACH\Log"
```

Tambien puede usarse `SOAP_LOCAL_WSCFAACH_LOG` para apuntar a un archivo especifico. El SOAP local no expone endpoints auxiliares tipo `/__requests`; la evidencia se valida por archivo plano.

Si la API corre en host en lugar de Docker:

```powershell
$env:SOAP_LOCAL_WSCFAACH_URL = "http://localhost:7083/WSCFAACH.svc"
$env:SOAP_LOCAL_AXON_RESPONSE_URL = "http://localhost:7083/WSAxonRespuestaTransacciones.svc"
Remove-Item Env:\WSCFAACH__HostHeader -ErrorAction SilentlyContinue
```

## Ejecucion multi motor

ACHInterbank soporta SQL Server y PostgreSQL. El spec `e2e/transactions-proc-contrapartidas.spec.ts` selecciona el motor con `ACH_E2E_DB_PROVIDER` y no debe quedar acoplado a un unico proveedor.

Seleccion SQL Server:

```powershell
$env:ACH_E2E_DB_PROVIDER = "SqlServer"
```

Conexion SQL Server mediante connection string:

```powershell
$env:ACH_E2E_SQLSERVER_CONNECTION_STRING = "Server=127.0.0.1,1433;Database=<db-local>;User Id=<usuario>;Password=<password>;TrustServerCertificate=True"
```

Conexion SQL Server mediante variables separadas:

```powershell
$env:ACH_E2E_SQLSERVER_HOST = "127.0.0.1"
$env:ACH_E2E_SQLSERVER_PORT = "1433"
$env:ACH_E2E_SQLSERVER_DATABASE = "<db-local>"
$env:ACH_E2E_SQLSERVER_USER = "<usuario>"
$env:ACH_E2E_SQLSERVER_PASSWORD = "<password>"
```

El contenedor local de `docker-compose.sqlserver.yml` publica `127.0.0.1:${SQLSERVER_HOST_PORT:-1433}`. Reutilizar los valores cargados localmente para `MSSQL_DB` y `MSSQL_SA_PASSWORD`, exportandolos a `ACH_E2E_SQLSERVER_*`; no copiar secretos al repositorio.

Validacion de conectividad SQL Server:

```powershell
sqlcmd -S "$env:ACH_E2E_SQLSERVER_HOST,$env:ACH_E2E_SQLSERVER_PORT" -U "$env:ACH_E2E_SQLSERVER_USER" -P "$env:ACH_E2E_SQLSERVER_PASSWORD" -d "$env:ACH_E2E_SQLSERVER_DATABASE" -C -Q "SELECT DB_NAME() AS DatabaseName"
```

Comando Playwright final para SQL Server:

```powershell
$env:ACH_E2E_DB_PROVIDER = "SqlServer"
npx playwright test e2e/transactions-proc-contrapartidas.spec.ts --project=chromium --trace on
```

Seleccion PostgreSQL:

```powershell
$env:ACH_E2E_DB_PROVIDER = "Postgres"
```

Conexion PostgreSQL mediante connection string:

```powershell
$env:ACH_E2E_POSTGRES_CONNECTION_STRING = "Host=127.0.0.1;Port=5432;Database=<db-local>;Username=<usuario>;Password=<password>"
```

Conexion PostgreSQL mediante variables separadas:

```powershell
$env:ACH_E2E_POSTGRES_HOST = "127.0.0.1"
$env:ACH_E2E_POSTGRES_PORT = "5432"
$env:ACH_E2E_POSTGRES_DATABASE = "<db-local>"
$env:ACH_E2E_POSTGRES_USER = "<usuario>"
$env:ACH_E2E_POSTGRES_PASSWORD = "<password>"
```

Tambien pueden reutilizarse valores locales de `.env`, `.env.example`, `docker-compose.postgres.yml` o `ConnectionStrings__PostgresConnection`, cargandolos en variables `ACH_E2E_POSTGRES_*` para Playwright. No subir `.env` con secretos ni logs reales.

Validacion de conectividad PostgreSQL:

```powershell
psql "host=$env:ACH_E2E_POSTGRES_HOST port=$env:ACH_E2E_POSTGRES_PORT dbname=$env:ACH_E2E_POSTGRES_DATABASE user=$env:ACH_E2E_POSTGRES_USER password=$env:ACH_E2E_POSTGRES_PASSWORD" -c "SELECT current_database();"
```

Comando Playwright final para PostgreSQL:

```powershell
$env:ACH_E2E_DB_PROVIDER = "Postgres"
npx playwright test e2e/transactions-proc-contrapartidas.spec.ts --project=chromium --trace on
```

## Comandos usados

Desde `web/ach-interbank-ui`:

```powershell
npx playwright test e2e/transactions-proc-contrapartidas.spec.ts --project=chromium --trace on
```

Verificacion completa recomendada:

```powershell
dotnet build ACHInterbank.sln -c Release
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --no-build
cd web/ach-interbank-ui
npm run build
npm test -- --watch=false --browsers=ChromeHeadless
npx playwright test e2e/transactions-proc-contrapartidas.spec.ts --project=chromium --trace on
```

## Comportamiento del spec

El spec:

- exige `RUN_LOCAL_SOAP_PROC_CONTRAPARTIDAS_E2E=true` y `ALLOW_LOCAL_MONETARY_SOAP_E2E=true`;
- crea datos sinteticos desde la SPA en `/transactions/create`;
- crea/activa un tercero sintetico como prerequisito local;
- usa CFA como origen (`IsDefaultSource=true`) y una entidad destino externa;
- ajusta temporalmente `api/users/soap-integrations` para endpoints locales y restaura al final;
- ajusta temporalmente el mapping publicado de `Proc_Contrapartidas` a los valores esperados y restaura al final;
- no agrega `PLValidarUsuarioBV`;
- no agrega ni envia `<METODO>` en el request outbound de ACHInterbank;
- no exige `ILR` ni `cantTrans`.
- exige que `ContrapartidaDispatchAttempts.ResponsePayloadXml` no este vacio;
- exige que se persistan `SoapMethodName`, `SoapEndpoint`, `ExecutionMode=Live`, `DurationMs`, `SoapResponseCode`, `SoapResponseDescription`, `SoapTechnicalStatus`, `IsSuccessful`, `IsFunctionalRejection` e `IsTechnicalFailure`.

## Verificacion de persistencia

Tabla principal:

- `ContrapartidaDispatchAttempts`.
- Union a transaccion: `ContrapartidaDispatchAttempts.DispatchItemId -> ContrapartidaDispatchItems.Id -> AchTransactions.Id`.
- Union a ciclo/lote: `ContrapartidaDispatchItems.AchCycleId`, `ContrapartidaDispatchItems.AchBatchId` y `ContrapartidaDispatchBatches.AchCycleId`.

Consulta SqlServer sanitizada por referencia sintetica:

```sql
SELECT TOP (1)
       t.TransactionExternalId,
       i.AchCycleId,
       i.AchBatchId,
       a.SoapMethodName,
       a.SoapEndpoint,
       a.ExecutionMode,
       a.StartedAtUtc,
       a.FinishedAtUtc,
       a.DurationMs,
       a.SoapResponseCode,
       a.SoapResponseDescription,
       a.SoapTechnicalStatus,
       a.IsSuccessful,
       a.IsFunctionalRejection,
       a.IsTechnicalFailure,
       LEN(a.RequestPayloadXml) AS RequestLength,
       LEN(a.ResponsePayloadXml) AS ResponseLength,
       a.CorrelationId
FROM ContrapartidaDispatchAttempts a
JOIN ContrapartidaDispatchItems i ON i.Id = a.DispatchItemId
JOIN AchTransactions t ON t.Id = i.AchTransactionId
WHERE t.TransactionExternalId = N'<referencia-sintetica>'
ORDER BY a.FinishedAtUtc DESC;
```

Consulta PostgreSQL sanitizada equivalente:

```sql
SELECT
       t."TransactionExternalId",
       i."AchCycleId",
       i."AchBatchId",
       a."SoapMethodName",
       a."SoapEndpoint",
       a."ExecutionMode",
       a."StartedAtUtc",
       a."FinishedAtUtc",
       a."DurationMs",
       a."SoapResponseCode",
       a."SoapResponseDescription",
       a."SoapTechnicalStatus",
       a."IsSuccessful",
       a."IsFunctionalRejection",
       a."IsTechnicalFailure",
       LENGTH(a."RequestPayloadXml") AS "RequestLength",
       LENGTH(a."ResponsePayloadXml") AS "ResponseLength",
       a."CorrelationId"
FROM "ContrapartidaDispatchAttempts" a
JOIN "ContrapartidaDispatchItems" i ON i."Id" = a."DispatchItemId"
JOIN "AchTransactions" t ON t."Id" = i."AchTransactionId"
WHERE t."TransactionExternalId" = '<referencia-sintetica>'
ORDER BY a."FinishedAtUtc" DESC
LIMIT 1;
```

El request persistido no debe contener `<METODO>`, `Proc_Transacciones` ni `RegistrarRespuestaTransaccion`. La respuesta persistida debe tener contenido y el `SoapResponseCode` debe corresponder al codigo entregado por el SOAP cuando el legacy lo informa explicitamente.

## Interpretacion de codigos observados

| Codigo | Interpretacion aplicada |
| --- | --- |
| `R96` | Exito operativo observado para contrapartida. Se persiste como `IsSuccessful=true`. |
| `R01` | Rechazo funcional observado por fondos insuficientes. Se persiste como `IsFunctionalRejection=true` y `IsTechnicalFailure=false`. |
| `RE` | Respuesta tecnica/anomala. No se asume rechazo funcional sin confirmacion del legado. |
| `0` | Respuesta tecnica/anomala o cierre no funcional segun contexto. No se asume significado funcional no confirmado. |
| Otros | Documentar codigo y respuesta cruda sanitizada; no inferir significado sin validacion funcional. |

## Rollback a DryRun

Para desactivar transmision local sin cambiar codigo:

```powershell
$env:ProcContrapartidas__Mode = "DryRun"
```

Reiniciar la API despues de cambiar la variable. La prueba Playwright live debe seguir saltandose si no estan ambos flags:

```powershell
$env:RUN_LOCAL_SOAP_PROC_CONTRAPARTIDAS_E2E = "false"
$env:ALLOW_LOCAL_MONETARY_SOAP_E2E = "false"
```

## Resultado 2026-07-01

Resultado: GO local.

La corrida Playwright paso contra:

- API Docker: `http://localhost:843`
- SPA Docker: `http://localhost:743`
- SOAP local host: `http://host.docker.internal:7083/WSCFAACH.svc`
- Host header WCF: `localhost:7083`
- Log plano: `C:\WebServices\WSCFAACH\Log\Trama_ACH_20260701.log`

Evidencia observada:

- request persistido contiene `Proc_Contrapartidas`;
- no contiene `Proc_Transacciones`;
- no contiene `RegistrarRespuestaTransaccion`;
- no contiene tag `<METODO>` en el outbound request de ACHInterbank;
- el log plano local registra `INICIO Proc_Contrapartidas`;
- para la validacion actual, ademas debe existir `ResponsePayloadXml` persistido y campos `SoapResponse*`/`SoapTechnicalStatus` consultables;
- campos funcionales esperados presentes, excluyendo `ILR` y `cantTrans`;
- `OFDD=TRANSFER  `;
- `OFFECHEFEC` en formato `yyyyMMdd`;
- `OFMONCRE=0`;
- `OFST=OO`;
- `OFIDTX=0`;
- `OFIDREVER=0`;
- `OFIDEBAPLI=1`;
- `OFIDCAMCOMPE=1` para ACH Colombia.

Nota: el SOAP legacy puede registrar internamente `<METODO>` en su `strEnvelope` de trazabilidad. Esa etiqueta es metadato legacy del componente SOAP y no forma parte del envelope outbound enviado por ACHInterbank.

## Limitaciones

- Productivo permanece NO-GO.
- La respuesta del SOAP local puede ser exito, rechazo funcional o respuesta tecnica/anomala; el criterio actual exige invocacion real local, evidencia en log plano, request outbound correcto y response persistida.
- Si la API corre en Docker, IIS/WCF puede requerir `WSCFAACH__HostHeader=localhost:7083`.
- `ILR` y `cantTrans` se ignoran en esta iteracion: no son requeridos en DTOs, contratos, builders, mappings ni validaciones Playwright.
- No se guardan logs originales en el repo; solo se adjunta evidencia sanitizada en `test-results`.
