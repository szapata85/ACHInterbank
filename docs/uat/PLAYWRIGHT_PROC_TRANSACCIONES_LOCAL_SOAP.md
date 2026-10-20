# Validacion E2E LIVE controlada de Proc_Transacciones

## Identificacion

- Fecha local: 2026-07-12 16:32:53 -05:00 (America/Bogota).
- Fecha UTC: 2026-07-12T21:32:53Z.
- Commit probado: `d531716beeedc4757fc873160cb84bbe80eab06b`.
- Motor previsto y validado: SQL Server local, contenedor `achinterbank-sqlserver`, publicado solo en `127.0.0.1:1433`.
- Flujo previsto: upload NACHA-M entrante, persistencia desagregada, `CreditoEntrante`, cola, post-procesamiento, orquestador, cliente SOAP y `Proc_Transacciones` LIVE.
- Resultado de la ronda: **NO-GO preventivo antes del upload**.

## Variables requeridas

La ejecucion LIVE requiere, sin registrar sus valores sensibles:

- `RUN_LOCAL_SOAP_PROC_TRANSACCIONES_E2E`
- `ALLOW_LOCAL_MONETARY_SOAP_E2E`
- `ACH_E2E_DB_PROVIDER`
- `ProcTransacciones__Mode`
- `ACH_API_URL`
- `ACH_UI_URL`
- `ACH_USER`
- `ACH_PASS`
- `SOAP_LOCAL_LOG_DIR`
- `ACH_E2E_PROC_TRANSACCIONES_RECEIVER_ACCOUNT`
- `ACH_E2E_PROC_TRANSACCIONES_EXPECTED_AMOUNT`
- `ACH_E2E_PROC_TRANSACCIONES_EXPECTED_ENDPOINT`
- Variables de conexion SQL Server E2E admitidas por el soporte Playwright.

En esta ronda, las variables monetarias, URLs y credenciales Playwright no estaban inyectadas en el proceso. No se inventaron valores ni se imprimieron secretos.

## Preflight ejecutado

1. Repositorio limpio y `HEAD` igual al commit esperado.
2. `dotnet build ACHInterbank.sln -c Release`: OK, 0 warnings, 0 errores.
3. Pruebas backend focalizadas: 130 passed, 0 failed, 0 skipped.
4. `npm run build`: OK.
5. `npx playwright test e2e/transactions-proc-transacciones-preflight.spec.ts --project=chromium`: 5 passed, 0 failed.
6. SQL Server local: migracion `20260711003700_AddIncomingNachaProcTransaccionesSoapAudit` aplicada; 13 tablas y 17 columnas de auditoria requeridas confirmadas.
7. SOAP local: `http://localhost:7083/WSCFAACH.svc` no acepto conexion; `TCP 7083 = false`.
8. Directorio `C:\WebServices\WSCFAACH\Log`: existe y es legible. Snapshot inicial: 2 archivos preexistentes; no se modificaron.

## Configuracion efectiva

- Endpoint SOAP esperado: `http://localhost:7083/WSCFAACH.svc`.
- Endpoint SOAP alcanzable: no.
- Modo efectivo de API: no consultado; la compuerta SOAP fallo antes de configurar o verificar `Live`.
- `mappingReady`: no consultado.
- API/UI: no configuradas para LIVE.
- Productivo: NO-GO, sin cambios.

## Ejecucion Playwright LIVE

Comando autorizado, no ejecutado por fallo de preflight:

```powershell
npx playwright test e2e/transactions-proc-transacciones.spec.ts `
  --project=chromium `
  --workers=1 `
  --retries=0 `
  --repeat-each=1 `
  --trace=on
```

Resultado exacto LIVE: 0 passed, 0 failed, 0 skipped, 0 retries, 0 transmisiones. La ausencia de ejecucion no se contabiliza como `skipped`; el spec no fue iniciado.

## Evidencia de correlacion

- `IDTRAN`: no generado para LIVE.
- `IDLOTE`: no enviado.
- `DispatchQueueId`: no creado.
- `CorrelationId`: no creado.
- Codigo SOAP: no disponible.
- Estado tecnico: preflight bloqueado por SOAP local no disponible.
- Clasificacion funcional: no evaluada.
- Auditoria `IncomingNachaIntegrationExecution`: no creada.
- Estado de cola de la prueba: no creado.

## Evidencia SOAP local

- Directorio: `C:\WebServices\WSCFAACH\Log`.
- Ventana del intento de disponibilidad: 2026-07-12 16:32:53 -05:00.
- Snapshot: 2 archivos preexistentes, con ultima modificacion anterior a la ronda.
- Archivo nuevo o modificado correlacionado: ninguno.
- Tokens de correlacion: no existen porque no hubo request outbound.

## Validaciones negativas

No hubo request outbound. Por tanto, durante esta ronda:

- no se envio `<METODO>`;
- no se invoco `Proc_Contrapartidas`;
- no se invoco `RegistrarRespuestaTransaccion`;
- no se uso `PLValidarUsuarioBV`.

Estas observaciones prueban ausencia de transmision, no conformidad funcional del request LIVE, que quedo sin ejecutar.

## Limpieza y restauracion

- No se acelero ni modifico Quartz.
- `IncomingNachaPostProcessing` no fue modificado.
- No se creo `IngestionId` ni se insertaron datos de prueba; no hubo registros que limpiar.
- No se borraron transacciones, bancos, ciclos, mappings, catalogos ni datos preexistentes.
- No se intento revertir ningun movimiento mediante SQL.
- La unica mutacion de infraestructura fue aplicar la migracion EF pendiente al SQL Server local autorizado.

## Pruebas posteriores

- `dotnet build ACHInterbank.sln -c Release`: OK, 0 warnings, 0 errores.
- `npm run build`: OK; conserva el warning conocido de Browserslist por navegadores fuera del soporte de esta version de Angular.
- `npx playwright test e2e/transactions-proc-transacciones-preflight.spec.ts --project=chromium`: 5 passed, 0 failed, 0 skipped, 1 worker.
- No se repitieron pruebas backend focalizadas porque no hubo cambios de codigo.

## Veredicto

**NO-GO**.

La compuerta SOAP fallo antes del upload y, adicionalmente, faltaban variables LIVE obligatorias. No existe evidencia combinada de Playwright LIVE, modo efectivo, auditoria, request/response, codigo SOAP, log correlacionado ni estado final de cola.

## Riesgos restantes

- Servicio SOAP local no disponible en el puerto esperado.
- Operacion `Proc_Transacciones` no pudo confirmarse por WSDL en esta ronda.
- Modo efectivo `Live`, endpoint efectivo y mapping no verificados mediante API autenticada.
- Cuenta, monto, institucion receptora/origen y transaccion correlacionable no verificados por falta de variables autorizadas.
- Cola preexistente y restauracion Quartz no inspeccionadas porque las compuertas posteriores no se abrieron.
- Integracion LIVE y resultado monetario permanecen sin validar.
- PostgreSQL conserva soporte en codigo/CI, pero no fue motor de esta ronda SQL Server.

---

## Segunda ronda — SOAP local disponible

### Identificacion y alcance

- Fecha local: 2026-07-12 16:47:17 -05:00 (America/Bogota).
- Fecha UTC: 2026-07-12T21:47:17Z.
- Commit probado: `2dcbbfc985da8b517bb3c1580289c7e0cc612403`.
- Motor previsto: SQL Server local; la migracion y el schema ya estaban confirmados y no se reaplicaron.
- Resultado: **NO-GO preventivo antes del upload**.
- Transmisiones consumidas en esta ronda: 0 de 1 autorizada.

### SOAP local

- TCP `localhost:7083`: disponible.
- WSDL `http://localhost:7083/WSCFAACH.svc?wsdl`: HTTP 200.
- Operacion `Proc_Transacciones`: presente en el WSDL.
- Directorio `C:\WebServices\WSCFAACH\Log`: existe y es legible.
- Snapshot UTC: 2026-07-12T21:46:40.4601803Z.
- Snapshot inicial: 2 archivos preexistentes; no se detecto ningun archivo nuevo o modificado porque no hubo transmision.

### API, UI y configuracion efectiva

- Contenedor API: `achinterbank-api`, estado `running`, puerto publicado `843`.
- Contenedor SPA: `achinterbank-spa`, estado `running`, puerto publicado `743`.
- La API en ejecucion no tiene `ProcTransacciones__Mode` ni endpoint ProcTransacciones inyectados en su entorno.
- `ACH_API_URL`, `ACH_UI_URL`, `ACH_USER` y `ACH_PASS`: no estaban inyectadas en el proceso de validacion.
- No fue posible autenticar ni consultar `GET /api/users/soap-integrations`.
- `effectiveMode`, `enabled`, `mappingReady` y endpoint efectivo: no confirmados.
- No se asumio `localhost` como endpoint interno de un contenedor.

### Datos autorizados

- Cuenta autorizada/enmascarada: no disponible; la variable obligatoria no estaba inyectada.
- Monto autorizado: no disponible; la variable obligatoria no estaba inyectada.
- Endpoint esperado: no disponible; la variable obligatoria no estaba inyectada.
- Origen, receptor CFA, DFI receptor, transaccion correlacionable y mapping: no validados por cierre de compuerta.
- Variables SQL Server requeridas por `G36RuntimeDb`: no estaban inyectadas.
- No se inventaron ni reutilizaron valores desde archivos locales o fallbacks.

### Ejecucion Playwright LIVE

Comando autorizado, no ejecutado:

```powershell
npx playwright test `
  e2e/transactions-proc-transacciones.spec.ts `
  --project=chromium `
  --workers=1 `
  --retries=0 `
  --repeat-each=1 `
  --trace=on
```

- Resultado: 0 passed, 0 failed, 0 skipped, 0 retries, 0 workers iniciados y 0 transmisiones.
- `IDTRAN`: no generado para LIVE.
- `IDLOTE`: no enviado.
- `DispatchQueueId`: no creado.
- `CorrelationId`: no creado.
- Codigo SOAP y clasificacion: no disponibles.
- Estado final de cola: no creado.

### Evidencia, validaciones negativas y log

- No se creo registro en `IncomingNachaIntegrationExecution`.
- No existe request/response persistido ni codigo SOAP para esta ronda.
- No se creo ingestion ni elemento de cola E2E.
- No hubo request outbound; por ausencia de transmision, no se envio `<METODO>`, `Proc_Contrapartidas`, `RegistrarRespuestaTransaccion` ni `PLValidarUsuarioBV`.
- No existe bloque de log correlacionado con `IDTRAN`/`IDLOTE`.
- Archivos del snapshot inicial:
  - `C:\WebServices\WSCFAACH\Log\Trama_ACH_20260701.log`, 4258 bytes, ultima modificacion UTC 2026-07-01T05:57:45.9298493Z.
  - `C:\WebServices\WSCFAACH\Log\Trama_ACH_20260710.log`, 2145 bytes, ultima modificacion UTC 2026-07-10T22:54:57.6648445Z.

### Limpieza y restauracion

- Quartz no fue acelerado ni modificado.
- `IncomingNachaPostProcessing` no fue modificado.
- No se abrieron conexiones E2E ni se insertaron datos de prueba.
- No hubo datos de ingestion que limpiar ni evidencia LIVE que preservar.
- No se borraron pendientes ajenos, transacciones, bancos, ciclos, mappings o catalogos.
- No se intento revertir ningun movimiento mediante SQL.

### Veredicto de segunda ronda

**NO-GO**.

Aunque el SOAP local ya estaba disponible, las compuertas de API/configuracion y variables obligatorias fallaron. No se pudo confirmar `effectiveMode=Live`, endpoint efectivo, mapping, cuenta, monto, instituciones, autenticacion ni conexion SQL E2E. Conforme al guardrail, no se hizo upload ni se ejecuto Playwright LIVE.

---

## Tercera ronda - API Live verificada y bloqueo funcional antes del upload

### Identificacion y alcance

- Fecha local: 2026-07-12 17:52:06 -05:00 (America/Bogota).
- Fecha UTC: 2026-07-12T22:52:06Z.
- Commit probado: `c2eddba4a2e38a27e52be3d272fe123fb32ea4f6`.
- Motor: SQL Server local en `127.0.0.1:1433`.
- Resultado: **NO-GO preventivo en Fase 8, antes del upload**.
- Playwright LIVE ejecutado: no.
- Transmisiones consumidas: 0 de 1 autorizada.

### Variables y readiness de infraestructura

- `ACH_USER`, `ACH_PASS`, `ACH_E2E_PROC_TRANSACCIONES_RECEIVER_ACCOUNT` y `ACH_E2E_PROC_TRANSACCIONES_EXPECTED_AMOUNT`: presentes; no se registraron valores sensibles.
- API `http://localhost:843`: HTTP 200.
- SPA `http://localhost:743`: HTTP 200.
- SQL Server `127.0.0.1:1433`: TCP disponible y contenedor healthy.
- SOAP Windows `http://localhost:7083/WSCFAACH.svc`: HTTP 200.
- WSDL local: HTTP 200; `Proc_Transacciones` presente.
- Schema SQL Server: 13 tablas requeridas y 17 columnas de auditoria SOAP confirmadas.
- Migracion `20260711003700_AddIncomingNachaProcTransaccionesSoapAudit`: ya aplicada; no se reaplico.

### API y conectividad SOAP desde Docker

- Se creo un override temporal fuera del repositorio para `ProcTransacciones__Mode=Live`.
- La imagen API anterior no exponia `procTransaccionesEffectiveSettings`; se reconstruyo exclusivamente `achinterbank-api:local` desde el `HEAD` actual.
- SQL Server y SPA no se reconstruyeron ni recrearon.
- Desde la red `achinterbank-onprem_ach_onprem`, `host.docker.internal` resolvio a `192.168.65.254` y el puerto 7083 respondio.
- WCF devolvio HTTP 400 con el Host normal `host.docker.internal:7083`, pero HTTP 200 con `Host: localhost:7083`.
- El cliente existente `WscfaachSoapClient` soporta `WSCFAACH:HostHeader`; se inyecto temporalmente `WSCFAACH__HostHeader=localhost:7083` sin cambiar codigo.
- Con esa configuracion, el WSDL fue accesible desde Docker y contenia `Proc_Transacciones`.

### Configuracion efectiva temporal

La consulta autenticada confirmo antes del preflight funcional:

```text
operation = Proc_Transacciones
effectiveMode = Live
endpoint = http://host.docker.internal:7083/WSCFAACH.svc
enabled = true
mappingReady = true
```

- Se guardo snapshot completo previo fuera del repositorio.
- Se actualizo por API autenticada unicamente el endpoint de `Proc_Transacciones`.
- Los demas mappings WSCFAACH, todos los mappings WSAxon y el resto de campos de `Proc_Transacciones` permanecieron sin cambios.
- No se modificaron `Proc_Contrapartidas`, `RegistrarRespuestaTransaccion`, mappings por SQL ni logica de negocio.

### Preflight Playwright no monetario

Comando ejecutado con un worker:

```powershell
npx playwright test `
  e2e/transactions-proc-transacciones-preflight.spec.ts `
  --project=chromium `
  --workers=1
```

Resultado: 5 passed, 0 failed, 0 skipped. Se confirmaron `IDTRAN` dinamico, parser compatible, bloqueo de cuenta/monto no autorizados, bloqueo de DryRun y correlacion estricta de log.

### Bloqueo funcional del fixture y base

- Fixture: 1060 bytes, 10 registros de 106 bytes.
- Cuenta del fixture: `********7777`.
- `TransactionCode=22`: confirmado.
- `IDLOTE=0000001`: confirmado.
- BatchHeader y BatchControl: consistentes.
- Colision por nombre de archivo: 0.
- La cuenta del fixture no coincide con `ACH_E2E_PROC_TRANSACCIONES_RECEIVER_ACCOUNT`.
- El monto del fixture no coincide exactamente con `ACH_E2E_PROC_TRANSACCIONES_EXPECTED_AMOUNT`.
- Entidad CFA `IsDefaultSource=true` con DFI receptor coincidente: 0; se requeria exactamente 1.
- Entidad origen externa `IsDefaultSource=false` coincidente: 0; se requeria al menos 1.
- Transaccion receptora configurada para cuenta/entidad CFA: 0; se requeria al menos 1.

La carga se bloqueo porque continuar habria usado una cuenta y un monto distintos de los autorizados y no existia correlacion institucional/transaccional valida en SQL Server. No se inventaron valores, no se crearon seeds y no se alteraron datos o mappings por SQL.

### Upload, SOAP, evidencia y clasificacion

- Uploads: 0.
- Ingestiones E2E: 0; la tabla de ingestiones quedo con conteo total 0.
- Elementos de cola E2E: 0.
- `IncomingNachaIntegrationExecution`: no creado.
- Requests outbound: 0.
- Respuestas SOAP: 0.
- Transmisiones monetarias: 0.
- Clasificacion operacional/funcional/tecnica: no evaluada porque no hubo ejecucion.
- No se envio `<METODO>`, `Proc_Contrapartidas`, `RegistrarRespuestaTransaccion` ni `PLValidarUsuarioBV`.
- Los dos logs SOAP preexistentes conservaron tamano y fecha anteriores a la ronda; no hubo bloque correlacionado nuevo.

### Limpieza y restauracion

- Quartz no se acelero ni modifico.
- `IncomingNachaPostProcessing` no se modifico.
- No hubo registros E2E que eliminar.
- No se modificaron datos preexistentes ni pendientes ajenos.
- No existio movimiento de core y no se intento revertir nada mediante SQL.
- El snapshot SOAP se reaplico mediante API autenticada.
- El endpoint de `Proc_Transacciones` volvio a `http://localhost:7083/WSCFAACH.svc`.
- La API se recreo sin override; el modo efectivo volvio a `DryRun` y se retiro el HostHeader temporal.
- La imagen API reconstruida se conservo actualizada.

### Veredicto de tercera ronda

**NO-GO**.

Las compuertas tecnicas de API Live, DTO efectivo, mapping y conectividad Docker/SOAP se abrieron correctamente. La compuerta funcional/monetaria fallo antes del upload por desacuerdo de cuenta y monto autorizados y por ausencia de receptor CFA, origen externo y transaccion receptora correlacionables en SQL Server. Para retomar, se deben suministrar variables que coincidan exactamente con el fixture sintetico autorizado y provisionar los datos institucionales/transaccionales mediante el mecanismo normal de aplicacion o seeding aprobado; no mediante SQL directo. Despues debe repetirse desde Fase 1 y volver a ejecutar Fase 8 antes de considerar la unica ejecucion LIVE.

---

## Ejecucion LIVE consumida - bloqueo de mapping antes de SOAP

- Resultado: **NO-GO**.
- Upload: si.
- SOAP: no alcanzado.
- Request SOAP: no construido.
- Response SOAP: no recibida.
- Codigo: `MAPPING_INVALID`.
- Detalle: `FUNCTIONAL_MAPPING_PLACEHOLDER`.
- `AttemptCount`: 1.
- Movimiento monetario: no.
- Restauracion: completada.

No se repitio el spec LIVE. Los identificadores de ingestion, cola y correlacion se conservaron unicamente en la evidencia operativa enmascarada; no se copio payload ni dato personal a esta documentacion.

## Homologacion productiva de mappings - Commit 4.2

### Evidencia validada

- Log: `docs/uat/Logs_Ejemplos/Trama_ACH_20260626.log`.
- SHA-256: `36CF1C99C118EEFD90DB2FD93FCC4CA98F0811944E606239C22A814713984C6C`.
- Bloques correlacionados `INICIO Proc_Transacciones` / `FIN Proc_Transacciones: R96`: 1576.
- Parametros presentes en todos los bloques: `TIPTRAN`, `BCORECEP`, `BCOORIG`, `NORIG`, `NCTAORIG`, `IDORIG`, `DESTRAN`, `FECEFEC`, `NCTARECEP`, `MONTO`, `NRECEP`, `IDRECEP`, `DISCRE`, `INFPAG`, `IDTRAN`, `IDLOTE`, `IREVER`, `IDCAMCOMPE`, `ILR`.
- Parametros ausentes: `TREG`, `CONV`, `PROD`, `REGLOTE`, `LIBRE`, `DIRECCIONIP`, `LIBRE1`.
- Datos personales copiados: no.
- XML productivo persistido: no.

### Contrato y decision funcional

El WSDL vigente conserva los siete parametros ausentes con `minOccurs=0`; por ello permanecen en el contrato como opcionales, sin `SEED`, y se omiten del outbound cuando no tienen valor. `ILR` aparece en el log legacy, pero no pertenece al WSDL vigente y no se envia. `<METODO>` sigue siendo metadato interno legacy y tampoco se envia.

La homologacion permanece bloqueada. `FinancialInstitution` no posee un codigo core aprobado distinto de routing/transit/check digit para derivar `BCORECEP` y `BCOORIG`. El log confirma que `IDLOTE` contiene seis digitos, pero no demuestra que corresponda a `BatchSequenceNumber`, al `BatchNumber` NACHA-M de siete digitos ni a otro consecutivo. Tampoco existe equivalencia de catalogo publicada para `IDCAMCOMPE`. No se inventaron conversiones ni se truncaron valores.

`mappingReady` utiliza ahora el mismo evaluator que el dispatch y expone codigo de incidencia y nombres de parametros bloqueantes sin valores sensibles. Hasta homologar las fuentes anteriores, el resultado es **NO-GO MAPPING**. Durante esta fase no se ejecuto upload, Playwright LIVE, Quartz ni SOAP.
