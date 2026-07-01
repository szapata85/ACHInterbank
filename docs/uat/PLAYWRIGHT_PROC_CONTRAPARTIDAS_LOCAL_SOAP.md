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

En este runtime los endpoints SOAP no se leen directamente de `appsettings`: se toman de la configuracion persistida expuesta por `api/users/soap-integrations`. El spec la ajusta temporalmente con:

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

## Comandos usados

Desde `web/ach-interbank-ui`:

```powershell
npx playwright test e2e/transactions-proc-contrapartidas.spec.ts --project=chromium --trace on
```

Verificacion backend:

```powershell
dotnet build ACHInterbank.sln -c Release
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --no-build --filter "FullyQualifiedName~TransactionIntegrationReadinessGuaranteeTests"
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
- La respuesta del SOAP local puede ser rechazo funcional; el criterio de esta validacion es invocacion real local, evidencia en log plano y request outbound correcto.
- Si la API corre en Docker, IIS/WCF puede requerir `WSCFAACH__HostHeader=localhost:7083`.
- `ILR` y `cantTrans` se ignoran en esta iteracion: no son requeridos en DTOs, contratos, builders, mappings ni validaciones Playwright.
- No se guardan logs originales en el repo; solo se adjunta evidencia sanitizada en `test-results`.
