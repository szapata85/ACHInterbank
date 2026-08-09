# RET.ORPHAN.E2E.1 — evidencia de devolución entrante huérfana

Fecha de ejecución: 2026-08-08  
Base HEAD: `8f1b300b7a0f198a86113590120f01ebe8075ee4`  
Brecha: `RET-GAP-007`  
Decisión: **PARCIAL — workflow operativo downstream demostrado; admisión física bloqueada**.

## Causa original y flujo encontrado

La ingesta vigente entra por `NachaUploadController`, ejecuta `IncomingNachaIngestionAppService.IngestAsync`, selecciona un perfil, parsea y persiste header/lotes/entries/addendas, y finalmente invoca `IncomingNachaPostParseProcessor`. El clasificador reconoce la devolución y el correlador intenta primero rastreo original exacto y luego la clave compuesta. Una correlación no inequívoca se conserva como `IncomingNachaTransactionLink` no final y exige resolución manual.

El servicio manual anterior sólo convertía el vínculo en final y escribía un evento operativo; no entregaba la devolución correlacionada al pipeline oficial de aplicación. Por tanto no demostraba lifecycle, causal catalogada, evento de estado ni atomicidad entre vínculo y aplicación.

La investigación utilizó Codebase Memory incremental en los proyectos `ACHInterbank` y `ACHInterbank-normativa`, con `search_graph`, `search_code`, `trace_path` y fragmentos de símbolos concretos. La decisión funcional se contrastó con ACH Colombia V35, especialmente 6.6 y 6.7. No se reindexó repetidamente ni se exploraron áreas fuera de alcance.

## Implementación

- La resolución manual valida cámara, monto, cuenta receptora y rastreo original antes de aceptar una candidata.
- La lista de candidatas es sólo una ayuda operativa; nunca autoasocia por similitud.
- Un `ExecuteUpdate` condicional sobre el vínculo no final constituye el claim DB-first. Dos contextos independientes no pueden adquirirlo a la vez.
- Claim, vínculo final, aplicación del retorno, evento de estado y auditoría se ejecutan dentro de una estrategia/transacción relacional.
- La aplicación llama a `IIncomingNachaPostParseProcessor.ApplyLinkedReturnAsync`; no existe un segundo motor de devoluciones.
- La transición canónica conserva causal `R31`, `AchReturnCodeId`, descripción resuelta e idempotency key existente.
- Un replay del mismo vínculo/objetivo devuelve el resultado persistido con `IsIdempotentReplay=true`; otro objetivo se rechaza.
- No se agregó SOAP ni movimiento monetario. No se modificó el guard de generación CENIT.
- La API expone consulta, detalle, candidatas y resolución con las policies existentes de lectura y `CanManageAch`.
- La SPA Angular Material ofrece búsqueda, estados de carga/vacío/error, investigación, comparación, confirmación explícita, prevención de doble clic y feedback en español.

## Gate físico demostrado

El upload real de un `.OUT` ACH Colombia llega al selector de perfil, pero responde HTTP 422 con:

```text
ProfileSelectionStatus=ProfileNotFound
Profile=OFFICIAL_ACH_ENTRADA_DEVOLUCION_V1_0
```

El repositorio contiene una caracterización explícita que espera ese fail-closed. No existe un perfil entrante publicado/homologado que autorice inventar records, variants o fields. El E2E conserva esta respuesta como evidencia y siembra únicamente la evidencia ya parseada/persistida para demostrar el workflow posterior contra runtime real. Esa siembra **no** se presenta como evidencia del parser ni permite cerrar `RET-GAP-007`.

## Evidencia ejecutable

### Backend focalizado

```text
ret-orphan-characterization.trx: 24 total, 24 passed, 0 failed, 0 skipped
ret-orphan-backend-focused.trx: 4 total, 4 passed, 0 failed, 0 skipped
```

Los casos nuevos atraviesan servicios reales y prueban conservación de huérfana, consulta, resolución válida, pipeline oficial, causal/evento, replay idempotente y rechazo de candidata incompatible. Las 24 caracterizaciones preservan correlación automática, ausencia de efectos antes de resolución, reproceso, DEV/CENIT y demás invariantes vigentes relacionadas.

### Concurrencia provider-specific

```text
SQL Server: 1 total, 1 passed, 0 failed, 0 skipped — 00:00:57.9602241
PostgreSQL: 1 total, 1 passed, 0 failed, 0 skipped — 00:00:25.3156249
TRX: tests/Cfa.ACHInterbank.Tests/TestResults/ret-orphan-multidb.trx
```

Dos servicios y contextos independientes compiten por el mismo vínculo. Sólo una resolución aplica el efecto; la otra observa replay/conflicto controlado sin segundo evento.

### Build y CI

```text
dotnet build ACHInterbank.sln -c Release
Build succeeded; 0 warnings; 0 errors; 00:01:40.70

npx ng test --watch=false --browsers=ChromeHeadless --code-coverage=false --progress=false
689 total; 689 passed; 0 failed; 0 skipped; 47.649 s Karma / 127.5 s comando

npx ng test ... --include=incoming-nacha-orphans-page.component.spec.ts
3 total; 3 passed; 0 failed; 0 skipped; 0.987 s
```

La suite backend completa fue iniciada una sola vez con `--no-build` y TRX solicitado. No produjo resumen ni TRX y excedió el timeout a `00:20:04.1`; sus procesos residuales fueron detenidos por PID. No se declara verde ni se repitió sin cambios.

### Docker LIVE y Playwright

Las imágenes API y SPA se construyeron desde el código final. Estado observado:

```text
achinterbank-api: healthy — http://localhost:843/health/live = 200
achinterbank-spa: healthy — http://localhost:743 = 200
achinterbank-sqlserver: healthy — 127.0.0.1:1433
job4-postgres: healthy — 127.0.0.1:5544
```

Comando focalizado:

```text
npx playwright test e2e/ret-orphan-resolution-live.spec.ts --project=chromium --workers=1
1 passed; 0 failed; 19.8 s
```

El navegador realizó login real, upload real y comprobó el gate 422; luego investigó la huérfana persistida, comparó dos candidatas compatibles, seleccionó una, confirmó la relación, verificó causal/lifecycle y repitió el POST para demostrar replay idempotente.

### Persistencia antes/después

Antes del vínculo manual del escenario exitoso había dos transacciones candidatas y cero efectos de devolución. Consulta posterior directa a SQL Server:

```json
{"scenario":"RET-ORPHAN-E2E-18086942","ingestionId":"3C754B6E-B27B-457F-9D88-53616BD2F6F9","transactions":2,"returnedTransactions":1,"stateEvents":1,"finalLinks":1,"pendingLinks":0,"manualResolutionEvents":1,"reasonCode":"R31","catalogLinks":1,"resolvedBy":"operador.ach"}
```

La repetición HTTP devolvió `IsIdempotentReplay=true` y el mismo `AchTransactionStateEventId`; los contadores permanecieron en un vínculo final, una transición y una auditoría.

## Artefactos

- `tests/Cfa.ACHInterbank.Tests/TestResults/ret-orphan-characterization.trx`
- `tests/Cfa.ACHInterbank.Tests/TestResults/ret-orphan-backend-focused.trx`
- `tests/Cfa.ACHInterbank.Tests/TestResults/ret-orphan-multidb.trx`
- `web/ach-interbank-ui/playwright-report/index.html`
- `web/ach-interbank-ui/test-results/ret-orphan-resolution-live-bc074-istida-sin-doble-aplicación-chromium/01-devolucion-sin-relacion.png`
- `web/ach-interbank-ui/test-results/ret-orphan-resolution-live-bc074-istida-sin-doble-aplicación-chromium/02-comparacion-y-confirmacion.png`
- `web/ach-interbank-ui/test-results/ret-orphan-resolution-live-bc074-istida-sin-doble-aplicación-chromium/03-resolucion-aplicada.png`

## Riesgos residuales y veredicto

1. Falta el perfil oficial publicado/homologado `OFFICIAL_ACH_ENTRADA_DEVOLUCION_V1_0`; no existe evidencia completa archivo → parser → huérfana.
2. La suite backend global no finalizó dentro de 20 minutos, aunque build, 28 pruebas backend focalizadas, ambos providers, Angular CI y Playwright quedaron verdes.

`RET-GAP-007 ABIERTO — falta perfil entrante ACH Colombia homologado y evidencia física completa archivo → parser → huérfana; la resolución downstream quedó demostrada.`

Próximo JOB único: `RET.RETURNIN.PROFILE.HOMOLOGATION.1`.
