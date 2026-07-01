# Playwright Proc_Contrapartidas con SOAP local

## Objetivo

Validar en entorno local/UAT que una transaccion debito creada desde la SPA en `/transactions` queda originada por CFA (`IsDefaultSource=true`) y, al procesarse, dispara `Proc_Contrapartidas` contra el SOAP legacy local `WSCFAACH.svc`.

La prueba no se ejecuta por defecto porque `Proc_Contrapartidas` es un candidato monetario. Requiere habilitacion explicita por variables de entorno y debe usarse solo con datos sinteticos y servicio SOAP local controlado.

## Endpoints SOAP locales

- `WSCFAACH`: `http://localhost:7083/WSCFAACH.svc`
  - `Proc_Contrapartidas`
  - `Proc_Transacciones`
- `WSAxonRespuestaTransacciones`: `http://localhost:7083/WSAxonRespuestaTransacciones.svc`
  - `RegistrarRespuestaTransaccion`

Si la API corre dentro de Docker y el SOAP corre en el host, usar `host.docker.internal:7083` en las variables `SOAP_LOCAL_WSCFAACH_URL` y `SOAP_LOCAL_AXON_RESPONSE_URL`.

## Variables requeridas

```powershell
$env:RUN_LOCAL_SOAP_PROC_CONTRAPARTIDAS_E2E = "true"
$env:ALLOW_LOCAL_MONETARY_SOAP_E2E = "true"
$env:ACH_UI_URL = "http://localhost:743"
$env:ACH_API_URL = "http://localhost:843"
$env:ACH_USER = "<usuario-local-uat>"
$env:ACH_PASS = "<password-local-uat>"
$env:SOAP_LOCAL_WSCFAACH_URL = "http://localhost:7083/WSCFAACH.svc"
$env:SOAP_LOCAL_AXON_RESPONSE_URL = "http://localhost:7083/WSAxonRespuestaTransacciones.svc"
$env:SOAP_LOCAL_WSCFAACH_LOG = "<ruta-al-log-plano-wscfaach>"
```

Alternativamente puede usarse `SOAP_LOCAL_LOG_DIR` si el servicio SOAP escribe varios archivos `.log`, `.txt` o `.xml`.

Para que exista transmision real al SOAP local, arrancar la API local/UAT con:

```powershell
$env:ProcContrapartidas__Mode = "Live"
```

Sin ese valor, el backend seguira en `DryRun` y el spec fallara con evidencia `PROC_DRY_RUN`, lo cual es correcto para ambientes no autorizados.

## Configuracion aplicada por el spec

El spec lee `api/users/soap-integrations`, cambia temporalmente los endpoints de:

- `Proc_Contrapartidas`
- `Proc_Transacciones`
- `RegistrarRespuestaTransaccion`

y restaura la configuracion original al finalizar. No agrega `PLValidarUsuarioBV`, no crea campos `METODO` y no modifica DTOs, servicios ni rutas.

Se puede desactivar este ajuste temporal con:

```powershell
$env:PROC_CONTRA_CONFIGURE_SOAP_SETTINGS = "false"
```

## Comandos

Desde `web/ach-interbank-ui`:

```powershell
npx playwright test e2e/transactions-proc-contrapartidas.spec.ts --project=chromium --trace on
```

Builds recomendados:

```powershell
cd C:\Users\CHECHO\Documents\proyectos\Interbank\ACHInterbank_SPA2
dotnet build ACHInterbank.sln -c Release
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --filter "FullyQualifiedName~ContrapartidaDispatchJobServiceTests|FullyQualifiedName~TransactionIntegrationReadinessGuaranteeTests"

cd web/ach-interbank-ui
npm run build
```

## Evidencia esperada

El spec adjunta en `web/ach-interbank-ui/test-results`:

- captura del formulario `/transactions/create` lleno,
- captura del listado `/transactions` despues de crear,
- request `Proc_Contrapartidas` sanitizado,
- fragmento sanitizado del log SOAP local.

La evidencia debe confirmar:

- operacion `Proc_Contrapartidas`,
- campos funcionales `OFNIT`, `OFCTA`, `OFMONDEB`, `OFIDCAMCOMPE`, `OFFECHEFEC`,
- ausencia de tag SOAP `<METODO>`,
- ausencia de `Proc_Transacciones` para este escenario,
- respuesta real del SOAP local o error controlado del servicio local.

## Limitaciones conocidas

- La prueba queda saltada si no se habilitan `RUN_LOCAL_SOAP_PROC_CONTRAPARTIDAS_E2E=true` y `ALLOW_LOCAL_MONETARY_SOAP_E2E=true`.
- La prueba exige ruta de log local mediante `SOAP_LOCAL_WSCFAACH_LOG` o `SOAP_LOCAL_LOG_DIR`; no asume endpoints auxiliares como `/__requests`.
- La SPA requiere una cuenta destino activa para debitos; el spec crea primero una prenotificacion sintetica por API como prerequisito y luego crea la transaccion monetaria desde la UI.
- El contrato backend actual revisado no declara `ILR` ni `cantTrans`. El spec los valida porque forman parte del requisito observado; si no aparecen en el request live, el resultado debe tratarse como hallazgo funcional antes de tocar codigo productivo.
