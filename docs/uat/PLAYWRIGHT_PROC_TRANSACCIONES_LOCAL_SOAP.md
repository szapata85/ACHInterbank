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
