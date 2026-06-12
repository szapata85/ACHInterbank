# E2E Playwright - NACHA-M UAT Evidence

Estas pruebas generan evidencia funcional y visual UAT de pantallas NACHA-M/SOAP read-only.

## Comandos

```bash
npm run build
npm test -- --watch=false --browsers=ChromeHeadless
npm run e2e
npm run e2e:headed
npm run e2e:report
```

La suite CENIT contra ambiente vivo es opt-in para no romper CI cuando no existe API local:

```bash
RUN_CENIT_E2E=true ACH_UI_URL=http://localhost:743 ACH_API_URL=http://localhost:843 ACH_USER=admin ACH_PASS="..." npx playwright test e2e/cenit-routes.spec.ts --project=chromium
```

Si `RUN_CENIT_E2E` no es `true`, `e2e/cenit-routes.spec.ts` se omite explicitamente. Para validar readiness de base de datos en UAT puede usarse `ACH_API_HEALTH_URL` apuntando a `/health/ready`; por defecto se usa `ACH_API_URL/health/live`.

## G3.6 PostgreSQL real

G3.6 usa SPA, API y PostgreSQL reales. Requiere una base UAT dedicada y previamente provisionada; las pruebas no crean esquema, no ejecutan migraciones y restauran las definiciones de ciclo/tarea que ajustan temporalmente.

Variables PostgreSQL:

```bash
POSTGRES_HOST=127.0.0.1
POSTGRES_PORT=5432
POSTGRES_DB=ACHInterbank
POSTGRES_USER=example_user
POSTGRES_PASSWORD=example_password_change_me
API_BASE_URL=http://localhost:843
SPA_BASE_URL=http://localhost:743
```

Inbound hasta `Proc_Transacciones` dry-run:

```bash
RUN_UAT_E2E_POSTGRES=true RUN_UAT_NACHA_UPLOAD=true RUN_UAT_DISPATCH=true npx playwright test e2e/uat-nacha-inbound-postgres-dispatch.spec.ts --project=chromium
```

Outbound hasta `Proc_Contrapartidas` dry-run:

```bash
RUN_UAT_E2E_POSTGRES=true RUN_UAT_NACHA_EXPORT=true RUN_UAT_CONTRAPARTIDAS=true npx playwright test e2e/uat-nacha-export-postgres-contrapartidas.spec.ts --project=chromium
```

Los specs esperan los task codes existentes `IncomingNachaPostProcessing` y `AchContrapartidasByCycle`. Quartz se ejecuta mediante `SchedulerSyncService` y se valida en `TaskExecutionLog`; no existe endpoint de disparo para pruebas.

G3.6B demuestra correlación entre NachaExport y dispatch por `AchCycleId`. No afirma causalidad NachaExport -> Proc_Contrapartidas.
El caso positivo crea por API una prenotificación UAT de valor cero, la madura diez días calendario y la aísla en otro batch/ciclo existente. No crea `AchCycles` ni desactiva la regla de tres días hábiles.

Los resultados dry-run conservan los estados reales: no se interpretan como movimiento monetario exitoso y nunca habilitan SOAP externo.

En CI, `angular-ci.yml` publica `playwright-report`, `playwright-test-results` y `uat-evidence-playwright` con `if: always()`.

## Evidencia generada

- Reporte HTML en `playwright-report`.
- Artefactos por prueba en `test-results`.
- Screenshots funcionales generados por specs de evidencia.
- Trace/video/screenshot automaticos en fallos, segun `playwright.config.ts`.

## Alcance

- Valida `/ach/nacha/operational-dashboard`.
- Valida `/ach/nacha/config-profiles` como modelo oficial read-only.
- Valida detalle operativo de archivo NACHA-M.
- Valida flujo de exportacion con `cycleId` y sin `/NachaExport/{hash}`.
- Valida rutas legacy como deprecated/read-only.
- Valida `/ach/nacha/soap-uat-console`.
- Valida ausencia de acciones peligrosas como ejecucion SOAP real, movimiento monetario, edicion de perfiles o carga productiva.
- Valida G3.6A inbound con persistencia desagregada, Quartz y evidencia `Proc_Transacciones` dry-run.
- Valida G3.6B outbound con naming oficial, auditoria y correlacion `AchCycleId` hacia `Proc_Contrapartidas` dry-run.

## Restricciones

- Productivo permanece NO-GO.
- No se ejecuta SOAP real.
- No se prueban acciones monetarias.
- No se usan credenciales reales ni endpoints productivos.
- Los screenshots son evidencia UAT automatizada, no certificacion oficial ACH Colombia/CENIT.
