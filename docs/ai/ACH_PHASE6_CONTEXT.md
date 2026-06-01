# ACHInterbank - Contexto Permanente IA - Fase 6 NACHA-M

## 1. Proposito del documento

Este archivo contiene el contexto permanente para que asistentes de IA como Codex, Cursor o Claude Code trabajen sobre la Fase 6 del proyecto ACHInterbank sin requerir prompts largos repetidos en cada tarea.

Cualquier tarea futura relacionada con NACHA-M, ACH Colombia, CENIT, procesamiento entrante, golden files, totales, trazabilidad o integracion SOAP debe leer este archivo antes de modificar codigo.

## 2. Estado actual de Fase 6

- Fase 6A: COMPLETADA.
- Fase 6B.1: COMPLETADA.
- Fase 6B.2: COMPLETADA.
- Fase 6B.3A: COMPLETADA.
- Fase 6B.3B: COMPLETADA.
- Fase 6B.3C: COMPLETADA a nivel tecnico automatizado con golden files semirreales.
- Fase 6B.3C.1: COMPLETADA.
- Fase 6B.4: COMPLETADA para flujo interno end-to-end automatizado.
- Siguiente fase: Fase 6C.3 - read-store persistido/consultas operativas reales sanitizadas.
- Productivo: NO-GO.

## 3. Commits cerrados conocidos

### Fase 6B.3B

Commit:
`fbd33a281577a4dcaa3095f810a88fc7e265313b`

Resumen:
Se implemento `INachaControlTotalsCalculator` / `NachaControlTotalsCalculator`, `EntryHash`, `BlockCount`, `FileIdModifier` MAN-004 V32, `EntryAddendaCount`, `TotalDebitAmountInCents`, `TotalCreditAmountInCents`, totales Batch/File, padding con records de 9, validacion calculado vs renderizado y trace/auditoria con `Phase=6B.3B`.

Resultado:
- Build Release OK.
- Tests Release OK: 1242 passed, 0 failed, 1 skipped, total 1243.
- Productivo NO-GO.

### Fase 6B.3C parcial

Commit:
`2e8ab8432e0e9d64f5308c275133cca891e7e025`

Resumen:
Se implemento la base de la suite funcional NACHA-M: `NachaFunctionalValidationTests`, `NachaGoldenFileComparer`, `NachaFixedWidthAssertions`, `NachaFunctionalModels`, `NachaFunctionalTraceAssertions` y metadata de fixtures.

Resultado:
- Build Release OK.
- Tests Release OK: 1279 passed, 0 failed, 1 skipped, total 1280.
- Riesgo pendiente en ese momento: faltaban snapshots fisicos `.ach` / `.RET`.

### Fase 6B.3C.1

Commit:
`3b3fd60a44c4b4e7fc0d7161e1cb88845b930c14`

Resumen:
Se materializaron golden files fisicos byte-stable `.ach` y `.RET` bajo `TestData/Nacha/GoldenFiles` para ACH Colombia y CENIT.

Golden files agregados:
- `ACHColombia/Outgoing/ACH_COL_OUT_001.ach`
- `ACHColombia/Incoming/ACH_COL_IN_001.ach`
- `ACHColombia/Returns/ACH_COL_RET_001.RET`
- `CENIT/Outgoing/CENIT_OUT_001.ach`
- `CENIT/Incoming/CENIT_IN_001.ach`
- `CENIT/Returns/CENIT_RET_001.RET`

Todos pesan 1060 bytes:
- 10 registros fixed-width.
- 106 caracteres por registro.

Resultado:
- Build Release OK.
- Tests Release OK: 1310 passed, 0 failed, 1 skipped, total 1311.
- Productivo NO-GO.
- Los golden files son semirreales y no reemplazan certificacion oficial con ACH Colombia/CENIT.

### Fase 6B.4

Commit:
`4406395dbd4e1922917672122fd34d4810a98550`

Resumen:
Se implemento el flujo interno end-to-end automatizado de procesamiento entrante NACHA-M.

Archivos nuevos principales:
- `src/Cfa.ACHInterbank.Application/ACH/Interfaces/INachaIncomingFileProcessor.cs`
- `src/Cfa.ACHInterbank.Application/ACH/Models/NachaIncomingFileProcessingModels.cs`
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaIncomingFileProcessor.cs`
- `tests/Cfa.ACHInterbank.Tests/NachaIncomingEndToEndProcessingTests.cs`

Servicios/modelos agregados:
- `INachaIncomingFileProcessor`
- `NachaIncomingFileProcessor`
- `NachaIncomingFileRequest`
- `NachaIncomingFileProcessingResult`
- `NachaIncomingDecision`
- `NachaIncomingFlowType`
- `NachaIncomingDecisionType`
- `NachaSoapOperationCandidate`

Resultado:
- Build Release OK.
- Tests Release OK: 1325 passed, 0 failed, 1 skipped, total 1326.
- No se toco motor table-driven.
- No se modificaron golden files.
- No hubo migraciones.
- No se invoco SOAP real.
- Productivo NO-GO.

### Fase 6B.5.1 - Gateway SOAP controlado endurecido

Commit:
`b3b37455355b4c975df25a55ac44be7c6996e808`

Resumen:
Fase 6B.5.1 completada. Se creo/agrego `AGENTS.md`, se endurecieron modelos SOAP internos, mapper y executor dry-run. Se agrego validacion de contexto auditable, compatibilidad operacion/decision, bloqueo `BlockedByNoGo` para `DryRun=false`, payload sin credenciales y trace `Phase=6B.5`. No se invoca SOAP real.

Resultado:
- Build Release OK.
- Tests Release OK: 1339 passed, 0 failed, 1 skipped, total 1340.
- Productivo permanece NO-GO.

### Fase 6B.5.2 - Mapping detallado de payloads SOAP

Commit:
`32f882876fc0df49d56fda5fddca95d0c77daf92`

Resumen:
Fase 6B.5.2 completada. Se agregaron `INachaSoapPayloadMapper`, modelos de payload SOAP tipados y `NachaSoapPayloadMapper`. Se implemento mapping detallado para `ProcContrapartidas`, `ProcTransacciones`, `RegistrarRespuestaTransaccion`, `None` y `ManualReviewRequired`, con validacion de reglas monetarias/no monetarias, sanitizacion de summaries y puente con dry-run. No se invoca SOAP real.

Resultado:
- Build Release OK.
- Tests Release OK: 1371 passed, 0 failed, 1 skipped, total 1372.
- Productivo permanece NO-GO.

### Fase 6B.5.3 - Gateway SOAP simulado con adaptadores mockeados

Commit:
`ac32dde1149b45823b38fbbcc9010cc72aeff006`

Resumen:
Fase 6B.5.3 completada. Usa payloads tipados de 6B.5.2 e implementa un gateway SOAP simulado con adaptadores mockeados por operacion. Permite simular exito, SOAP faults, timeouts y fallas controladas; bloquea `ProductiveExecution=true` y `AllowExternalSoapInvocation=true`; sanitiza summaries y mantiene `Phase=6B.5`. No invoca SOAP real, no crea clientes SOAP reales y no usa credenciales.

Resultado:
- Build Release OK.
- Tests Release OK: 1399 passed, 0 failed, 1 skipped, total 1400.
- Productivo permanece NO-GO.

### Fase 6B.5.4 - Resiliencia, auditoria operacional e idempotencia SOAP

Commit:
`b8244315e098e132a5979195282ef5eeb68e8dda`

Estado:
Completada.

Resumen:
Fase 6B.5.4 completada. Agrega resiliencia operacional sobre el gateway SOAP simulado de 6B.5.3, incluyendo idempotencia in-memory, auditoria de intentos, retry policy, clasificacion de timeouts/SOAP faults/fallas, calculo de backoff sin sleeps largos, bloqueo de duplicados y resultados sanitizados. No invoca SOAP real, no agrega endpoints reales, no usa credenciales y mantiene Productivo NO-GO.

Archivos principales:
- `src/Cfa.ACHInterbank.Application/ACH/Models/NachaSoapResilienceModels.cs`
- `src/Cfa.ACHInterbank.Application/ACH/Interfaces/INachaSoapIdempotencyStore.cs`
- `src/Cfa.ACHInterbank.Application/ACH/Interfaces/INachaSoapAttemptAuditor.cs`
- `src/Cfa.ACHInterbank.Application/ACH/Interfaces/INachaSoapResiliencePolicyEvaluator.cs`
- `src/Cfa.ACHInterbank.Application/ACH/Interfaces/INachaSoapResilientExecutor.cs`
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaSoapInMemoryIdempotencyStore.cs`
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaSoapInMemoryAttemptAuditor.cs`
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaSoapResiliencePolicyEvaluator.cs`
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaSoapResilientExecutor.cs`
- `tests/Cfa.ACHInterbank.Tests/NachaSoapResilienceAndIdempotencyTests.cs`

Resultado:
- Build Release OK.
- Tests Release OK: 1431 passed, 0 failed, 1 skipped, total 1432.
- Productivo permanece NO-GO.
- Queda listo para Fase 6B.5.5: preparacion UAT/control operativo de integracion SOAP real sin habilitar productivo.

### Fase 6B.5.5 - Preparacion UAT/control operativo de SOAP real sin habilitar productivo

Commit:
`d110195b996797f5ce1f09ea302c2eeeccc8ec1f`

Estado:
Completada.

Resumen:
Fase 6B.5.5 completada. Prepara controles operativos para una futura integracion SOAP real en UAT/controlado, incluyendo feature flags, readiness checks, validacion segura de endpoints/certificados, operational gate y placeholder bloqueado para cliente SOAP real. Esta fase no invoca SOAP real, no agrega endpoints reales, no usa credenciales, no mueve dinero y mantiene Productivo NO-GO.

Archivos principales:
- `src/Cfa.ACHInterbank.Application/ACH/Models/NachaSoapUatControlModels.cs`
- `src/Cfa.ACHInterbank.Application/ACH/Interfaces/INachaSoapUatReadinessChecker.cs`
- `src/Cfa.ACHInterbank.Application/ACH/Interfaces/INachaSoapEndpointSafetyValidator.cs`
- `src/Cfa.ACHInterbank.Application/ACH/Interfaces/INachaSoapCertificateReadinessValidator.cs`
- `src/Cfa.ACHInterbank.Application/ACH/Interfaces/INachaSoapOperationalGate.cs`
- `src/Cfa.ACHInterbank.Application/ACH/Interfaces/INachaRealSoapClientAdapter.cs`
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaSoapUatReadinessChecker.cs`
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaSoapEndpointSafetyValidator.cs`
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaSoapCertificateReadinessValidator.cs`
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaSoapOperationalGate.cs`
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaBlockedRealSoapClientAdapter.cs`
- `tests/Cfa.ACHInterbank.Tests/NachaSoapUatOperationalControlTests.cs`

Resultado:
- Build Release OK.
- Tests Release OK: 1464 passed, 0 failed, 1 skipped, total 1465.
- Productivo permanece NO-GO.
- Queda listo para Fase 6B.5.6 o Fase 6C: wiring controlado de UAT real/fakes certificados, sin habilitar productivo.

### Fase 6B.5.6 - Wiring UAT controlado y readiness operacional final

Commit:
`81d53ac84f07a9cc88e75b1d8d507cfd98e9feaa`

Estado:
Completada.

Resumen:
Fase 6B.5.6 completada. Conecta las piezas backend SOAP/UAT ya construidas para producir un readiness operacional final por decision NACHA-M, orquestando payload mapper, request mapper, operational gate, readiness checker, simulated gateway/resilient executor y cliente real bloqueado. Produce resultado consolidado, auditoria sanitizada, resumen de resiliencia/simulacion, bloqueos NO-GO y cortes tempranos de pipeline. Esta fase no invoca SOAP real, no agrega endpoints reales, no usa credenciales, no mueve dinero y mantiene Productivo NO-GO.

Archivos principales:
- `src/Cfa.ACHInterbank.Application/ACH/Models/NachaSoapUatOrchestrationModels.cs`
- `src/Cfa.ACHInterbank.Application/ACH/Interfaces/INachaSoapUatOrchestrator.cs`
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaSoapUatOrchestrator.cs`
- `tests/Cfa.ACHInterbank.Tests/NachaSoapUatOrchestrationTests.cs`

Resultado:
- Build Release OK.
- Tests Release OK: 1496 passed, 0 failed, 1 skipped, total 1497.
- Productivo permanece NO-GO.
- Queda listo para Fase 6C: cierre operativo/certificacion UAT formal, parametrizacion final o exposicion controlada por API/UI sin habilitar productivo.

### Fase 6C.1 - SPA Angular: consulta operativa NACHA-M y readiness SOAP

Estado:
Completada.

Commit:
`0af1fa4491c9c5622161baa640f28b0d2e3fc61f`

Resumen:
Se agrego una primera capa SPA Angular read-only para consulta operativa NACHA-M y readiness SOAP. La ruta `/ach/nacha/operational-dashboard` muestra estado Productivo NO-GO, modo SOAP simulado/dry-run, archivos NACHA-M demo seguros, decisiones funcionales, readiness UAT, auditoria sanitizada Phase 6B.5 y bloqueos operativos. No se agregaron endpoints backend nuevos en esta fase; la pantalla consume un servicio Angular local con datos demo seguros marcados como read-only.

Archivos principales:
- `web/ach-interbank-ui/src/app/features/nacha-operational/`
- `web/ach-interbank-ui/src/app/app-routing.module.ts`

Resultado:
- Backend build Release OK: 0 warnings, 0 errors.
- Backend tests Release OK: 1496 passed, 0 failed, 1 skipped, total 1497.
- Angular build OK.
- Angular tests OK: 239 success.
- No se invoca SOAP real.
- No se mueve dinero.
- No se agregan credenciales ni endpoints reales.
- No se modifican golden files.
- No se toca table-driven.
- Productivo permanece NO-GO.
- Queda listo para Fase 6C.2: conectar consultas read-only a endpoints backend reales/sanitizados o ampliar vistas operativas sin habilitar ejecucion.

### Fase 6C.2 - API/read-models operativos read-only para alimentar la SPA

Estado:
Completada.

Commit:
`8f611085c36c593e0a2ed0561f86d8f019066d6f`

Resumen:
Se agregaron read-models backend sanitizados, servicio read-only y endpoints GET para alimentar el dashboard SPA de Fase 6C.1 con resumen operativo NACHA-M, archivos, decisiones, readiness SOAP/UAT y auditoria Phase 6B.5. Angular ahora consume `GET /api/ach/nacha/operational/dashboard` y conserva fallback demo seguro si la API no responde. Los endpoints son solo lectura, no ejecutan SOAP real, no mueven dinero, no exponen credenciales/payloads/cuentas/documentos completos, no modifican golden files, no tocan table-driven y mantienen Productivo NO-GO.

Endpoints:
- `GET /api/ach/nacha/operational/dashboard`
- `GET /api/ach/nacha/operational/summary`
- `GET /api/ach/nacha/operational/files`
- `GET /api/ach/nacha/operational/decisions`
- `GET /api/ach/nacha/operational/soap-readiness`
- `GET /api/ach/nacha/operational/audit`

Archivos principales:
- `src/Cfa.ACHInterbank.Application/ACH/Models/NachaOperationalReadModels.cs`
- `src/Cfa.ACHInterbank.Application/ACH/Interfaces/INachaOperationalReadModelService.cs`
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaOperationalReadModelService.cs`
- `src/Cfa.ACHInterbank.Api/Controllers/NachaOperationalReadinessController.cs`
- `tests/Cfa.ACHInterbank.Tests/NachaOperationalReadModelTests.cs`
- `web/ach-interbank-ui/src/app/features/nacha-operational/`

Resultado:
- Backend build Release OK: 0 warnings, 0 errors.
- Backend tests Release OK: 1516 passed, 0 failed, 1 skipped, total 1517.
- Angular build OK.
- Angular tests OK: 241 success.
- Productivo permanece NO-GO.
- Queda listo para Fase 6C.3: reemplazar provider demo backend por read-store persistido/consultas operativas reales sanitizadas sin habilitar acciones criticas.

### Fase 6C.2E - Playwright evidence para dashboard NACHA-M read-only

Estado:
Completada.

Commit:
Pendiente de commit.

Resumen:
Se agrego Playwright/Chromium para generar evidencia funcional y visual del dashboard NACHA-M read-only en `/ach/nacha/operational-dashboard`. La suite E2E valida banner Productivo NO-GO, SOAP real deshabilitado, resumen, secciones de archivos/decisiones/readiness/auditoria Phase 6B.5, fallback demo cuando falla el endpoint backend y ausencia de acciones peligrosas. Genera screenshot `dashboard-full-page.png`, reporte HTML y conserva trace/video/screenshot en fallos.

Archivos principales:
- `web/ach-interbank-ui/playwright.config.ts`
- `web/ach-interbank-ui/e2e/nacha-operational-dashboard.spec.ts`
- `web/ach-interbank-ui/e2e/README.md`
- `web/ach-interbank-ui/package.json`
- `web/ach-interbank-ui/package-lock.json`

Resultado:
- Backend build Release OK: 0 warnings, 0 errors.
- Backend tests Release OK: 1516 passed, 0 failed, 1 skipped, total 1517.
- Angular build OK.
- Angular tests OK: 241 success.
- Playwright E2E OK: 3 passed.
- Evidencia generada en `web/ach-interbank-ui/test-results/.../dashboard-full-page.png`.
- Reporte Playwright generado en `web/ach-interbank-ui/playwright-report/index.html`.
- CI Playwright queda pendiente para una fase posterior.
- No se invoca SOAP real.
- No se mueve dinero.
- No se agregan credenciales ni endpoints reales.
- No se modifican golden files.
- No se toca table-driven.
- Productivo permanece NO-GO.
- Queda listo para Fase 6C.3: read-store persistido/consultas operativas reales sanitizadas y eventual publicacion de artefactos E2E en CI.

### Prerrequisito 6C.3 - Control de NachaExport 422

Estado:
Completado.

Commit:
`7afa7149fff42e1f0b80c711de95a4369e0a1900`

Resumen:
Se diagnostico que `GET /NachaExport/{cycleId}` espera identificador de ciclo ACH (`cycleId`), no hash de archivo ni hash de contenido. `AchCycleExportDto` expone `CycleId` y `ExportIdentifier`; la SPA usa solo `row.cycleId` para descargar y no hace fallback a `id`, `hash`, `nachaId`, `fileHash` ni `exportIdentifier`. Playwright valida que no se llame `/NachaExport/{hash}`. Un 422 es funcional cuando el ciclo existe pero no tiene contenido/lotes NACHA-M exportables; un identificador inexistente retorna 404 controlado. Se agrego metadata `IsExportable`/`ExportUnavailableReason` al listado exportable, la SPA deshabilita descarga para filas no exportables o demo y muestra mensajes controlados para 422 sin exponer errores crudos ni datos sensibles.

Resultado:
- Backend build Release OK: 0 warnings, 0 errors.
- Backend tests Release OK: 1521 passed, 0 failed, 1 skipped, total 1522.
- Angular build OK.
- Angular tests OK: 251 success.
- Playwright E2E OK: 5 passed.
- No se invoca SOAP real.
- No se mueve dinero.
- No se agregan credenciales ni endpoints reales.
- No se modifican golden files.
- No se toca table-driven.
- Productivo permanece NO-GO.

### Fase 6C.3A - Auditoria SPA de endpoints Legacy NACHA-M

Estado:
Completada.

Objetivo:
Auditar y corregir pantallas/servicios Angular que todavia consuman o presenten como oficiales los endpoints legacy `nacha-layouts` / `nacha-record-definitions`, expuestos en SPA bajo `/ach-cycles/nacha/layouts` y `/ach-cycles/nacha/definitions`.

Reglas:
- Estas rutas pertenecen al modelo legacy de layouts/definitions.
- La administracion oficial NACHA-M futura debe basarse en `nacha-config profiles`.
- `CfgProfile`, `CfgLayoutVariant` y `CfgLayoutField` son la base de transicion para Fase 6C.4.
- Las pantallas legacy, si siguen accesibles, deben quedar como diagnostico/read-only/deprecated, no como parametrizacion oficial.
- No iniciar el read-store persistido de Fase 6C.3 en esta fase.
- Dashboard 6C.1/6C.2 no debe consumir endpoints legacy.
- NachaExport debe seguir usando `cycleId`.
- Productivo permanece NO-GO.

Cierre:
- Inventario corto: ver `docs/ai/ACH_PHASE6_LEGACY_AUDIT.md`.
- Referencias legacy encontradas: rutas Angular `/ach-cycles/nacha/layouts` y `/ach-cycles/nacha/definitions`; servicios SPA `NachaLayoutsService` y `NachaRecordDefinitionsService`; componentes `NachaLayoutsComponent` y `NachaRecordDefinitionsComponent`; endpoints backend `nacha-layouts` y `nacha-record-definitions`; modelos DTO legacy de layouts/definitions; tests y documentacion.
- Clasificacion final: SPA legacy queda `ActiveDiagnosticOnly`; servicios Angular quedan legacy/deprecated; endpoints backend quedan `BackendCompatibilityOnly`/diagnostico; tests/documentacion quedan `TestOnly`/contexto; administracion oficial queda en `nacha-config profiles`.
- Acciones Angular: las pantallas legacy siguen accesibles solo como diagnostico read-only con banner `LEGACY / Deprecated`; se ocultaron acciones de crear/editar/eliminar; navegacion oficial filtra entradas legacy y prefiere `NACHA Config` (`/nacha-config-admin/perfiles`); se dejo redirect futuro `/ach/nacha/config-profiles`.
- Acciones backend: controladores legacy marcados `[Obsolete]` y metadata Swagger/endpoint como diagnostico deprecado; POST/PUT/DELETE responden 410 sin llamar servicios; GET se mantiene por compatibilidad diagnostica.
- Dashboard 6C.1/6C.2 no consume `nacha-layouts` ni `nacha-record-definitions`; Playwright lo valida con interceptores.
- NachaExport permanece protegido: SPA/E2E siguen validando que no se solicite `/NachaExport/{hash}` y que el flujo use `cycleId`.
- Pendiente 6C.4: administracion oficial completa de `nacha-config profiles` con `CfgProfile`, `CfgLayoutVariant`, `CfgLayoutField`, camaras ACH Colombia/CENIT, records 1/5/6/7/8/9, versionamiento y estados Draft/Published/Deprecated/Archived.
- Resultado verificacion: `dotnet build ACHInterbank.sln -c Release` OK (0 warnings, 0 errors); `dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --no-build` OK (1525 passed, 1 skipped); `npm run build` OK; `npm test -- --watch=false --browsers=ChromeHeadless` OK (255 success); `npm run e2e` OK (9 passed).
- No se inicio read-store persistido 6C.3, no se ejecuto SOAP real, no se movio dinero, no se tocaron golden files ni motor table-driven, y Productivo permanece NO-GO.

### Fase 6C.3 - Read-store operativo persistido para dashboard NACHA-M

Estado:
Completada.

Objetivo:
Reemplazar progresivamente el provider demo seguro del backend por consultas read-only sobre persistencia operativa NACHA-M/SOAP para alimentar el dashboard `/ach/nacha/operational-dashboard`, manteniendo fallback demo, DTOs sanitizados, endpoints GET-only, NO-GO productivo y protecciones de NachaExport `cycleId`.

Reglas:
- El read-store debe consultar `NachaHeaders`, `BatchHeaders`, `EntryDetails`, `AddendaRecords`, `BatchControls`, `FileControls` y fuentes entrantes/SOAP persistidas cuando existan.
- Usar `AsNoTracking`, limitar resultados, ordenar por fecha descendente y no llamar `SaveChanges`.
- Si faltan decisiones/readiness/auditoria persistidas, devolver secciones parciales/read-only con warnings, sin inventar decisiones reales ni ejecutar orquestadores.
- No ejecutar SOAP real, no mover dinero, no exponer credenciales, payloads SOAP completos, cuentas ni documentos completos.
- No generar migraciones, no tocar golden files, no tocar motor table-driven.
- Dashboard debe indicar fuente backend read-only/demo/parcial.
- Productivo permanece NO-GO.

Cierre:
- Commit: pendiente de commit.
- Archivos principales: `INachaOperationalReadStore`, `NachaOperationalReadStore`, `NachaOperationalReadModelService`, `NachaOperationalReadModels`, `NachaOperationalReadStoreTests`, dashboard Angular `nacha-operational`, specs Playwright del dashboard.
- Endpoints mantenidos sin cambios y GET-only: `/api/ach/nacha/operational/dashboard`, `/summary`, `/files`, `/decisions`, `/soap-readiness`, `/audit`.
- Read-store persistido: consulta `NachaHeaders` con `BatchHeaders`, `EntryDetails`, `AddendaRecords`, `BatchControls`, `FileControls`, ingestion/resultados entrantes, clasificaciones, cola/ejecuciones de integracion y eventos operativos. Usa `AsNoTracking`, limita resultados y ordena por fecha descendente.
- Fallback: si no hay headers persistidos o falla la consulta, el servicio conserva demo seguro read-only. Si hay archivos pero faltan decisiones/readiness/auditoria, marca datos parciales y agrega warnings controlados.
- Sanitizacion: no expone cuentas/documentos completos, hash completo de correlation/header, payload XML SOAP ni credenciales; `WouldInvokeRealSoap=false`, `ProductiveExecution=false`.
- Angular: el dashboard muestra `Fuente: backend read-only`, `Fuente: demo seguro` o `Fuente: parcial`, mantiene NO-GO visible y no agrega acciones criticas.
- NachaExport: sin regresion; SPA/tests/E2E siguen bloqueando fallback a `id/hash/fileHash/exportIdentifier` y no solicitan `/NachaExport/{hash}`.
- Legacy: dashboard no consume `nacha-layouts` ni `nacha-record-definitions`; Playwright mantiene guard.
- Verificacion: `dotnet build ACHInterbank.sln -c Release` OK (0 warnings, 0 errors); `dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --no-build -- RunConfiguration.MaxCpuCount=1` OK (1552 passed, 1 skipped); `npm run build` OK; `npm test -- --watch=false --browsers=ChromeHeadless` OK (263 success); `npm run e2e` OK (11 passed).
- Nota operativa: una corrida backend paralela previa aborto por crash interno CLR/EF en un test existente de `AchContrapartidasByCycleHandlerTests`; la corrida secuencial completa paso sin fallos.
- Queda listo para Fase 6C.4: administracion oficial `nacha-config profiles` con `CfgProfile`, `CfgLayoutVariant`, `CfgLayoutField`, camaras ACH Colombia/CENIT, records 1/5/6/7/8/9, versionamiento y estados Draft/Published/Deprecated/Archived.
- No se ejecuto SOAP real, no se movio dinero, no se editaron perfiles, no se generaron migraciones, no se tocaron golden files ni motor table-driven, y Productivo permanece NO-GO.

### Fase 6C.4 - Administracion oficial read-only de nacha-config profiles

Estado:
Completada.

Resumen:
Se agrego contrato backend oficial GET-only `/api/ach/nacha/config-profiles` para dashboard/listado/detalle/by-code/variants/fields, con read-models sanitizados sobre `CfgProfile`, `CfgProfileRecord`, `CfgLayoutVariant` y `CfgLayoutField`. La SPA oficial reutiliza `/nacha-config-admin/perfiles`; `/ach/nacha/config-profiles` redirige alli. La UI muestra modelo oficial, legacy deprecated y Productivo NO-GO, sin crear/editar/publicar/archivar/borrar ni llamar command service. Legacy layouts/definitions siguen diagnostico read-only y no son fuente oficial.

Resultado:
- Backend build OK; tests backend OK: 1565 passed, 1 skipped.
- Angular build OK; tests Angular OK: 270 success.
- Playwright OK: 18 passed.
- No SOAP real, no migraciones, no golden files, no motor table-driven, no `/NachaExport/{hash}`.
- Productivo permanece NO-GO.

### Fase 6C.5 - Detalle operativo read-only por archivo NACHA-M

Estado:
Completada.

Resumen:
Se agrego detalle operativo GET-only `/api/ach/nacha/operational/files/{fileId}` usando el `fileId` sanitizado del dashboard (`nacha-{HeaderId}`). El read-store proyecta Header, BatchHeaders, EntryDetails, AddendaRecords, BatchControls, FileControls y TotalsSummary con `AsNoTracking`, limites de filas, warnings parciales y sanitizacion de cuentas/documentos/nombres/trazas/correlation. La SPA agrega `/ach/nacha/operational-dashboard/files/:fileId`, enlazada desde el dashboard solo para archivos persistidos, con banners NO-GO/read-only y sin acciones criticas.

Resultado:
- Backend build OK; tests backend OK: 1575 passed, 1 skipped.
- Angular build OK; tests Angular OK: 283 success.
- Playwright OK: 20 passed.
- No SOAP real, no movimientos monetarios, no mutaciones, no legacy oficial, no `/NachaExport/{hash}`.
- Productivo permanece NO-GO.

### Fase 6C.6 - Consola SOAP/UAT read-only y auditoria operacional

Estado:
Completada.

Resumen:
Se agrego consola SOAP/UAT read-only con contrato GET-only `/api/ach/nacha/soap-uat-console` para dashboard, candidates, candidate y audit. Reutiliza el read-store operacional persistido para proyectar candidatos SOAP, readiness, bloqueos NO-GO, simulacion, resiliencia, idempotencia y auditoria sanitizada sin ejecutar orquestadores, gateways ni SOAP real. La SPA agrega `/ach/nacha/soap-uat-console` con banners NO-GO/SOAP deshabilitado/read-only, badges operativos y sin acciones criticas.

Resultado:
- Backend build OK; tests backend OK: 1588 passed, 1 skipped.
- Angular build OK; tests Angular OK: 295 success.
- Playwright OK: 27 passed.
- No SOAP real, no movimientos monetarios, no mutaciones, no endpoints/certificados/secretos/payloads completos, no legacy oficial, no `/NachaExport/{hash}`.
- Productivo permanece NO-GO.

### Fase 6C.7 - Publicacion de evidencias Playwright en CI y paquete UAT

Estado:
Completada.

Resumen:
Se extendio `angular-ci.yml` para ejecutar Playwright E2E despues de build y unit tests Angular, instalando Chromium en CI y publicando artefactos con `if: always()`. Se creo documentacion UAT compacta para el paquete de evidencia automatizada Playwright, cubriendo dashboard operacional, config profiles, detalle archivo NACHA-M, exportacion por `cycleId`, legacy deprecated y consola SOAP/UAT.

Resultado:
- Artefactos CI: `playwright-report`, `playwright-test-results`, `uat-evidence-playwright`.
- Angular build/tests y Playwright deben ejecutarse desde el workflow Angular.
- La evidencia automatizada no reemplaza certificacion oficial ACH Colombia/CENIT.
- No SOAP real, no movimientos monetarios, no mutaciones criticas, no secretos, no legacy oficial, no `/NachaExport/{hash}`.
- Productivo permanece NO-GO.

### Fase 6C.8 - Consola de conciliacion/read-only de respuestas y estados ACH

Estado:
Completada.

Resumen:
Se agrego API GET-only `/api/ach/reconciliation` y SPA `/ach/reconciliation` para conciliacion operativa read-only de respuestas diferenciales, devoluciones `.RET`, rechazos, ROR, prenotificaciones y estados internos. Cruza fuentes persistidas ACH/NACHA-M cuando existen y marca datos parciales con warnings sanitizados.

Resultado:
- Endpoints: dashboard, items, item por id y item por correlationId.
- UI read-only con banners Productivo NO-GO, sin movimientos monetarios y sin acciones criticas.
- Playwright cubre carga, detalle, no mutaciones, no legacy y no `/NachaExport/{hash}`.
- No SOAP real, no movimientos monetarios, no mutaciones, no secretos/payloads/cuentas/documentos completos, no legacy oficial.
- Productivo permanece NO-GO.

### Fase 6C.9 - Matriz requisito-norma-codigo-prueba-evidencia

Estado:
Completada.

Resumen:
Se agrego matriz UAT/auditoria `docs/uat/REQUIREMENT_TRACEABILITY_MATRIX.md` que conecta ACH Colombia MAN-004 V32, CENIT/Banco de la Republica, reglas NACHA-M, codigo, pruebas backend/Angular/Playwright, evidencia CI y estado. Se agrego indice compacto `docs/uat/UAT_EVIDENCE_PACKAGE.md` y referencia desde `docs/uat/PLAYWRIGHT_EVIDENCE.md`.

Resultado:
- Matriz documental read-only con 30 filas/requisitos.
- Marca golden files como semirreales y certificacion ACH Colombia/CENIT como pendiente/oficial.
- No agrega API/SPA, no ejecuta SOAP real, no mueve dinero, no muta estados, no usa secretos, no legacy oficial, no `/NachaExport/{hash}`.
- Productivo permanece NO-GO.

### Fase 6C.10 - Preparacion UAT formal ACH Colombia/CENIT

Estado:
Completada.

Resumen:
Se agrego paquete documental UAT formal con preparacion, checklist, escenarios ACH Colombia/CENIT y matriz de riesgos/brechas. El indice `docs/uat/UAT_EVIDENCE_PACKAGE.md` enlaza matriz 6C.9, evidencia Playwright, preparacion formal, checklist, escenarios y riesgos.

Resultado:
- Documentos: `UAT_FORMAL_PREPARATION_ACH_CENIT.md`, `UAT_EXECUTION_CHECKLIST.md`, `UAT_TEST_SCENARIOS_ACH_CENIT.md`, `UAT_RISKS_AND_GAPS.md`.
- Alcance cubre ACH Colombia, CENIT, NACHA-M entrada/salida, `.RET`, respuestas, prenotificaciones, ROR, config profiles, dashboard, detalle, SOAP/UAT read-only, conciliacion y evidencia CI.
- No agrega codigo/API/SPA, no ejecuta SOAP real, no mueve dinero, no muta estados, no usa datos reales/secretos, no legacy oficial, no `/NachaExport/{hash}`.
- Certificacion oficial ACH Colombia/CENIT y SOAP real quedan pendientes/controlados.
- Productivo permanece NO-GO.

### Fase 6C.11 - Hardening tecnico pre-UAT y cierre de brechas automatizadas

Estado:
Completada.

Resumen:
Se agrego hardening tecnico pre-UAT y checklist automatizado para estabilizar comandos, workflows, evidencia Playwright y guardas criticos antes de UAT formal. Se revisaron workflows backend/frontend y se documento mitigacion de warnings Browserslist/Node y crash paralelo CLR/EF.

Resultado:
- Nuevos docs: `docs/uat/PRE_UAT_TECHNICAL_HARDENING.md`, `docs/uat/PRE_UAT_AUTOMATED_CHECKLIST.md`.
- `angular-ci.yml` mantiene artefactos Playwright con `if: always()` y timeout de job.
- `dotnet-ci.yml` usa `RunConfiguration.MaxCpuCount=1` para estabilidad backend.
- `package.json` conserva una sola declaracion de `@playwright/test`.
- No agrega features, no ejecuta SOAP real, no mueve dinero, no muta estados, no usa secretos, no legacy oficial, no `/NachaExport/{hash}`.
- Productivo permanece NO-GO.

### Fase 6D.1 - Ejecucion UAT controlada con datos sinteticos representativos

Estado:
Completada.

Resumen:
Se ejecuto/documento ronda UAT controlada con dataset sintetico representativo, mapeando 23 escenarios ACH Colombia/CENIT/transversales contra golden files semirreales, evidencias Playwright y reportes sanitizados existentes. Se creo matriz de defectos/hallazgos y resumen ejecutivo.

Resultado:
- Nuevos docs: `UAT_SYNTHETIC_DATASET.md`, `UAT_EXECUTION_ROUND_1.md`, `UAT_DEFECTS_MATRIX.md`, `UAT_ROUND_1_EXECUTIVE_SUMMARY.md`.
- Resultado ronda 1: 23 escenarios; 13 OK, 9 observados, 0 bloqueados, 1 no ejecutado.
- Hallazgos principales: certificacion oficial pendiente, ambiente UAT externo/datos cargados pendiente, homologacion formal de causales pendiente, ciclos/colas/neteo CENIT pendiente.
- No agrega codigo, no modifica golden files, no ejecuta SOAP real, no mueve dinero, no usa datos reales/secretos, no legacy oficial, no `/NachaExport/{hash}`.
- Recomendacion: continuar UAT formal ampliado; Productivo permanece NO-GO.

### Fase 6D.2 - Cierre de hallazgos UAT ronda 1 y preparacion UAT ampliado

Estado:
Completada.

Resumen:
Se clasificaron y priorizaron los 6 hallazgos de ronda 1, cerrando quick wins documentales y dejando brechas externas/certificacion diferidas con plan de accion. Se preparo UAT ampliado con nuevos escenarios de manual review, inconsistencias, ciclo/cola/neteo CENIT y dataset sintetico ampliado.

Resultado:
- Nuevos docs: `UAT_ROUND_1_FINDINGS_CLOSURE.md`, `UAT_EXPANDED_ROUND_PLAN.md`, `UAT_EXPANDED_SYNTHETIC_DATASET.md`.
- Hallazgos: 1 cerrado documentalmente, 1 parcial, 1 diferido, 3 pendientes externos/certificacion/UAT ampliado.
- Nuevos escenarios: UAT-EXP-001 a UAT-EXP-005; nuevos datasets propuestos: DS-EXP-*.
- No agrega codigo/tests/workflows, no modifica golden files, no ejecuta SOAP real, no mueve dinero, no usa datos reales/secretos, no legacy oficial, no `/NachaExport/{hash}`.
- Recomendacion: ejecutar UAT ampliado en ambiente aislado con dataset sintetico cargado; Productivo permanece NO-GO.

### Fase 6D.3 - Ejecucion UAT ampliado con dataset sintetico cargado

Estado:
Completada.

Resumen:
Se documento ejecucion UAT ampliada con dataset sintetico cargado para UAT-EXP-001 a UAT-EXP-005, validando manual review, inconsistencias read-only, prenotes no monetarias y guardas CI/NO-GO. Ciclo/cola/neteo CENIT queda observado por requerir evidencia externa con operador.

Resultado:
- Nuevos docs: `UAT_EXPANDED_EXECUTION_ROUND.md`, `UAT_EXPANDED_DEFECTS_UPDATE.md`, `UAT_EXPANDED_EXECUTIVE_SUMMARY.md`.
- Resultado ronda ampliada: 5 escenarios; 4 OK, 1 observado, 0 bloqueados, 0 no ejecutados.
- Hallazgos: UAT-FND-006 cerrado, UAT-FND-003 parcial, UAT-FND-005 diferido, UAT-FND-001/002/004 pendientes, UAT-FND-007 nuevo por CENIT externo.
- No agrega codigo/tests/workflows, no modifica golden files, no ejecuta SOAP real, no mueve dinero, no usa datos reales/secretos, no legacy oficial, no `/NachaExport/{hash}`.
- Recomendacion: continuar UAT formal con terceros ACH Colombia/CENIT; Productivo permanece NO-GO.

### Fase 6D.4 - Preparacion de UAT externo con terceros ACH Colombia/CENIT

Estado:
Completada.

Resumen:
Se preparo el paquete documental para coordinar UAT externo con ACH Colombia y Banco de la Republica/CENIT, incluyendo readiness, RACI, ventanas, controles de seguridad y solicitudes de evidencia. UAT externo queda pendiente/no ejecutado.

Resultado:
- Nuevos docs: `EXTERNAL_UAT_PREPARATION_ACH_CENIT.md`, `EXTERNAL_UAT_RACI.md`, `EXTERNAL_UAT_WINDOW_PLAN.md`, `EXTERNAL_UAT_SECURITY_CONTROLS.md`, `EXTERNAL_UAT_EVIDENCE_REQUESTS.md`.
- UAT-FND-007 queda asociado a ventana/evidencia CENIT externa; no se cierra sin tercero.
- Riesgos nuevos: dependencia terceros, homologacion causales, ventana CENIT, certificados/endpoints, SOAP real controlado, datos externos, evidencia oficial y aprobacion Seguridad/Compliance.
- No agrega codigo/tests/workflows, no crea credenciales/endpoints/certificados, no ejecuta SOAP real, no mueve dinero, no usa datos reales/secretos, no legacy oficial, no `/NachaExport/{hash}`.
- Recomendacion: agendar UAT externo con RACI aceptado y seguridad aprobada; Productivo permanece NO-GO.

### Fase 6D.5 - Paquete de aprobacion de seguridad y control certificados/endpoints UAT

Estado:
Completada.

Resumen:
Se preparo paquete documental para aprobacion Seguridad/Compliance previo a recibir o cargar certificados, endpoints, parametros tecnicos o secretos UAT. Todo queda como placeholder pendiente y bajo custodia corporativa.

Resultado:
- Nuevos docs: `SECURITY_APPROVAL_PACKAGE.md`, `UAT_CERTIFICATE_ENDPOINT_REGISTER.md`, `UAT_SECRET_CUSTODY_MODEL.md`, `UAT_SECURITY_APPROVAL_CHECKLIST.md`, `UAT_SECURITY_EVIDENCE_REQUESTS.md`.
- Certificados/endpoints/secretos: pendientes, no cargados, no aprobados; sin URLs, thumbprints ni valores reales.
- Riesgos nuevos: custodia secretos, endpoints no aprobados, certificados vencidos/incorrectos, logging sensible, exposicion de datos, autorizacion incompleta y uso indebido SOAP real.
- No agrega codigo/tests/workflows, no crea credenciales/endpoints/certificados, no ejecuta SOAP real, no mueve dinero, no usa datos reales/secretos, no legacy oficial, no `/NachaExport/{hash}`.
- Recomendacion: solicitar aprobacion Seguridad/Compliance antes de cualquier carga en UAT aislado; Productivo permanece NO-GO.

### Fase 6D.6 - Simulacion aprobacion Seguridad/UAT y pre-habilitacion externa

Estado:
Completada.

Resumen:
Se simulo documentalmente la revision Seguridad/Compliance y se preparo checklist de pre-habilitacion externa ACH Colombia/CENIT. La simulacion clasifica controles y evidencias, pero no otorga aprobacion formal.

Resultado:
- Nuevos docs: `SECURITY_APPROVAL_SIMULATION.md`, `EXTERNAL_PRE_ENABLEMENT_CHECKLIST.md`, `SECURITY_EVIDENCE_GAP_ANALYSIS.md`, `SECURITY_APPROVAL_REQUEST_DRAFT.md`.
- Resultado simulado: aprobacion formal NO otorgada; pre-habilitacion externa condicionada; listo para solicitar revision, no para cargar secretos/certificados/endpoints.
- Certificados/endpoints/secretos siguen pendientes/no cargados; algunos quedan en `Pendiente intercambio seguro`.
- Riesgos nuevos: confusion simulacion/aprobacion, evidencia ambiente aislado faltante, canal seguro no aprobado, acta NO-GO pendiente y evidencia terceros pendiente.
- No agrega codigo/tests/workflows, no crea credenciales/endpoints/certificados, no ejecuta SOAP real, no mueve dinero, no usa datos reales/secretos, no legacy oficial, no `/NachaExport/{hash}`.
- Recomendacion: enviar borrador de solicitud a Seguridad/Compliance y bloquear cualquier carga hasta decision formal; Productivo permanece NO-GO.

### Fase 6D.7 - Solicitud formal Seguridad/Compliance y paquete aprobacion externa

Estado:
Completada.

Resumen:
Se convirtio el borrador 6D.6 en paquete formal listo para presentar a Seguridad, Compliance, Tecnologia, Operaciones y Mesa UAT. Incluye solicitud formal, indice de anexos, matriz de decision, checklist de envio y declaracion Productivo NO-GO.

Resultado:
- Nuevos docs: `SECURITY_COMPLIANCE_REVIEW_REQUEST.md`, `EXTERNAL_APPROVAL_PACKAGE_INDEX.md`, `SECURITY_COMPLIANCE_DECISION_MATRIX.md`, `SECURITY_REVIEW_SUBMISSION_CHECKLIST.md`, `PRODUCTIVE_NO_GO_ATTESTATION.md`.
- Estado: listo para presentacion; revision/aprobaciones siguen pendientes, sin decisiones aprobadas.
- Decisiones DEC-001 a DEC-011 quedan `Pendiente`; DEC-012 `No aplica`.
- No agrega codigo/tests/workflows, no crea credenciales/endpoints/certificados, no ejecuta SOAP real, no mueve dinero, no usa datos reales/secretos, no legacy oficial, no `/NachaExport/{hash}`.
- Recomendacion: presentar paquete formal y esperar decision antes de cualquier intercambio/carga; Productivo permanece NO-GO.

### Fase 6D.8 - Registro decision Seguridad/Compliance y plan observaciones

Estado:
Completada.

Resumen:
Se agrego mecanismo documental para registrar la decision formal Seguridad/Compliance, gestionar observaciones, acciones por responsable y evidencias asociadas sin inventar aprobaciones ni cambiar estados.

Resultado:
- Nuevos docs: `SECURITY_COMPLIANCE_DECISION_RECORD.md`, `SECURITY_COMPLIANCE_OBSERVATION_RESPONSE_PLAN.md`, `SECURITY_COMPLIANCE_ACTION_MATRIX.md`, `SECURITY_COMPLIANCE_DECISION_EVIDENCE_LOG.md`.
- Decision Seguridad/Compliance: pendiente; decision recibida: no recibida; DEC-001 a DEC-011 siguen `Pendiente`, DEC-012 sigue `No aplica`.
- Certificados/endpoints/secretos siguen pendientes/no cargados, sin URLs, thumbprints ni valores reales.
- No agrega codigo/tests/workflows, no crea credenciales/endpoints/certificados, no ejecuta SOAP real, no mueve dinero, no usa datos reales/secretos, no legacy oficial, no `/NachaExport/{hash}`.
- Recomendacion: presentar paquete, registrar la decision cuando llegue y atender observaciones antes de cualquier intercambio/carga; Productivo permanece NO-GO.

### Fase 6D.9 - Paquete presentacion ejecutiva para comite y solicitud decision

Estado:
Completada.

Resumen:
Se preparo paquete ejecutivo para Seguridad, Compliance, Tecnologia, Operaciones y comite UAT, consolidando estado, evidencias, decisiones pendientes, anexos, riesgos y NO-GO productivo.

Resultado:
- Nuevos docs: `EXECUTIVE_COMMITTEE_PACKAGE.md`, `EXECUTIVE_PRESENTATION_BRIEF.md`, `EXECUTIVE_DECISION_REQUEST.md`, `EXECUTIVE_MEETING_MINUTES_TEMPLATE.md`, `EXECUTIVE_APPENDIX_CHECKLIST.md`, `EXECUTIVE_RISK_STATUS_DASHBOARD.md`.
- Comite/Seguridad/Compliance: pendiente; no hay aprobaciones ni decision recibida.
- Certificados/endpoints/secretos siguen pendientes/no cargados, sin URLs, thumbprints ni valores reales.
- No agrega codigo/tests/workflows, no crea credenciales/endpoints/certificados, no ejecuta SOAP real, no mueve dinero, no usa datos reales/secretos, no legacy oficial, no `/NachaExport/{hash}`.
- Recomendacion: presentar paquete ejecutivo, registrar decision y observaciones, y mantener bloqueada cualquier carga/intercambio hasta aprobacion formal; Productivo permanece NO-GO.

### Fase 6D.10 - Registro decision comite y plan ejecucion posterior

Estado:
Completada.

Resumen:
Se preparo mecanismo documental para registrar la decision formal del comite ejecutivo y planear ejecucion posterior segun resultado: aprobado, aprobado con observaciones, rechazado, bloqueado, diferido o pendiente.

Resultado:
- Nuevos docs: `EXECUTIVE_COMMITTEE_DECISION_RECORD.md`, `POST_COMMITTEE_EXECUTION_PLAN.md`, `EXECUTIVE_OBSERVATION_RESPONSE_PLAN.md`, `POST_COMMITTEE_ACTION_MATRIX.md`, `COMMITTEE_DECISION_EVIDENCE_LOG.md`.
- Decision de comite: pendiente; decision recibida: no recibida; no hay aprobaciones marcadas.
- Certificados/endpoints/secretos siguen pendientes/no cargados, sin URLs, thumbprints ni valores reales.
- No agrega codigo/tests/workflows, no crea credenciales/endpoints/certificados, no ejecuta SOAP real, no mueve dinero, no usa datos reales/secretos, no legacy oficial, no `/NachaExport/{hash}`.
- Recomendacion: esperar decision formal, registrarla con evidencia y ejecutar solo el plan permitido por esa decision; Productivo permanece NO-GO.

### Fase 6D.11C - Congelamiento controlado paquete UAT y espera decision externa

Estado:
Completada.

Resumen:
Se congelo documentalmente el paquete UAT/comite/seguridad en el commit base `229498168cc0d1820ecb8276358b6d836cb904cb`, dejando protocolo de espera, control de cambios y checklist minimo de revalidacion posterior a decision.

Resultado:
- Nuevos docs: `UAT_PACKAGE_FREEZE_RECORD.md`, `UAT_EXTERNAL_DECISION_WAITING_PROTOCOL.md`, `UAT_PACKAGE_CHANGE_CONTROL.md`, `UAT_REVALIDATION_CHECKLIST_AFTER_DECISION.md`.
- Estado paquete: congelado/en espera; decision externa y decision comite siguen pendientes/no recibidas; no hay aprobaciones marcadas.
- Certificados/endpoints/secretos siguen pendientes/no cargados, sin URLs, thumbprints ni valores reales.
- No agrega codigo/tests/workflows, no crea credenciales/endpoints/certificados, no ejecuta SOAP real, no mueve dinero, no usa datos reales/secretos, no legacy oficial, no `/NachaExport/{hash}`.
- Recomendacion: no ampliar alcance; esperar decision externa formal y reabrir solo mediante control de cambios/revalidacion; Productivo permanece NO-GO.

## 4. Decision arquitectonica oficial

Opcion C: usar `nacha-config profiles` como modelo oficial.

Implicaciones:
- Separar parametrizacion por camara ACH Colombia y CENIT.
- Eliminar dependencia funcional de layouts/definitions legacy.
- Hacer que `NachaFileBuilder` genere desde perfiles publicados/vigentes.
- Fallar controladamente si falta parametrizacion.
- Mantener enfoque table-driven.
- Preparar la SPA para administrar perfiles NACHA-M por camara en fases posteriores.
- No volver a logica hardcoded si el perfil puede resolver la regla.

## 5. Reglas NACHA-M vigentes

### Naming ACH Colombia MAN-004 V32

Formato:

```text
RRRRTTT.ZZZ.1
```

Donde:
- RRRR = codigo de ruta de entidad originadora.
- TTT = codigo de transito.
- ZZZ = consecutivo diario 001-036.

Archivos de devolucion:
- Usan extension `.RET`.

### FileIdModifier

Regla:
- 001-026 => A-Z.
- 027-036 => 0-9.
- Fuera de 001-036 debe fallar controladamente.

### Totales NACHA-M

Los totales Batch/File deben incluir:
- `EntryAddendaCount`.
- `EntryHash`.
- `TotalDebitAmountInCents`.
- `TotalCreditAmountInCents`.
- `BatchCount`.
- `BlockCount`.
- `PaddingRecordCount`.
- Conteos fisicos antes/despues de padding.

### Padding

Regla:
- Padding final con records de 9.
- Alineacion segun `BLOCKINGFACTOR` oficial.
- Si falta `BLOCKINGFACTOR`, fallback controlado al estandar 10.
- No se permite padding intermedio.

### Fixed-width

Regla actual usada en golden files:
- 106 caracteres por registro.
- 10 registros para snapshots actuales.
- 1060 bytes por archivo golden actual.

## 6. Golden files fisicos

Ruta:

```text
tests/Cfa.ACHInterbank.Tests/TestData/Nacha/GoldenFiles
```

Archivos:
- `ACHColombia/Outgoing/ACH_COL_OUT_001.ach`
- `ACHColombia/Incoming/ACH_COL_IN_001.ach`
- `ACHColombia/Returns/ACH_COL_RET_001.RET`
- `CENIT/Outgoing/CENIT_OUT_001.ach`
- `CENIT/Incoming/CENIT_IN_001.ach`
- `CENIT/Returns/CENIT_RET_001.RET`

Reglas:
- Son semirreales.
- Estan anonimizados.
- Son byte-stable.
- Se usan para regresion automatizada.
- No contienen datos productivos.
- No sustituyen certificacion oficial con ACH Colombia/CENIT.
- No deben modificarse sin intencion explicita y actualizacion de pruebas.

Helpers relacionados:
- `NachaGoldenFileComparer`.
- `NachaFixedWidthAssertions`.
- `NachaTestDataPaths`.
- `NachaFixtureSensitivityAssertions`.
- `NachaFunctionalTraceAssertions`.
- `NachaFunctionalModels`.

## 7. Reglas SOAP para fases posteriores

La Fase 6B.4 solo dejo candidatos SOAP, no ejecucion real.

Interpretacion funcional:

### Proc_Contrapartidas

- Mueve debitos monetarios de una transaccion originada por CFA.
- Debe usarse solo cuando la decision funcional indique movimiento monetario tipo debito originado por CFA.

### Proc_Transacciones

- Mueve creditos monetarios de una transaccion originada por otra entidad financiera.
- Debe usarse solo cuando la decision funcional indique credito monetario originado externamente hacia CFA.

### RegistrarRespuestaTransaccion

- Solo registra notificaciones/respuestas diferenciales.
- No debe hacer movimientos monetarios.
- Aplica para respuestas diferenciales, rechazos, devoluciones o notificaciones que no mueven dinero.

### Reglas criticas

- Respuestas diferenciales no mueven dinero.
- Archivos `.RET` no mueven dinero directamente.
- Prenotificaciones aprobadas/rechazadas no mueven dinero.
- Si hay ambiguedad, la decision debe ser `ManualReviewRequired`.
- La integracion SOAP real debe hacerse en una fase controlada posterior.
- No invocar SOAP real desde tests automatizados.
- Usar mocks, dry-run o gateway controlado.

## 8. Estado productivo

Productivo permanece NO-GO.

Razones:
- Los golden files son semirreales.
- Falta certificacion oficial con ACH Colombia/CENIT.
- Falta integracion SOAP real controlada.
- Falta UAT funcional.
- Falta aprobacion operativa/tecnica.
- Falta plan de rollback y monitoreo productivo.

Ninguna fase debe cambiar Productivo a GO sin instruccion explicita y validacion formal.

## 9. Comandos estandar de build/test

```powershell
dotnet build ACHInterbank.sln -c Release
```

```powershell
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release
```

Para segunda ejecucion rapida cuando ya existe build valido:

```powershell
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --no-build
```

Criterio esperado:
- Build succeeded.
- 0 warnings.
- 0 errors.
- Tests passing.
- Mantener o aumentar cobertura.
- No reducir pruebas sin justificacion.

## 10. Convenciones de tests

- Usar golden files fisicos existentes cuando aplique.
- No usar datos productivos reales.
- No modificar golden files salvo cambio intencional.
- Tests deben ser deterministicos.
- Evitar `DateTime.Now`, `Guid.NewGuid` o valores aleatorios sin control.
- Si hay campos variables, fijarlos o normalizarlos.
- Usar mocks para SOAP.
- No invocar servicios externos reales.
- Verificar `Phase` en trace segun fase:
  - `6B.3B` para totales.
  - `6B.4` para procesamiento entrante.
  - `6B.5` para integracion SOAP controlada.
- Validar `ProductiveExecution=false` en flujos simulados o no productivos.
- Validar que respuestas diferenciales, `.RET` y prenotificaciones no generen movimiento monetario.
- Preferir pruebas pequenas y focalizadas sobre pruebas enormes.
- Mantener nombres de tests descriptivos.

## 11. Instrucciones para futuras tareas con IA

Antes de implementar cualquier fase futura:
1. Leer este archivo.
2. Inspeccionar el estado actual del repo.
3. Revisar `git status`.
4. No asumir que el working tree esta limpio.
5. No reescribir arquitectura existente.
6. No tocar produccion.
7. No introducir datos sensibles.
8. No generar migraciones salvo necesidad clara.
9. No ejecutar SOAP real.
10. Mantener Productivo NO-GO.
11. Entregar resumen final con:
    - Archivos modificados.
    - Archivos nuevos.
    - Tests agregados/modificados.
    - Comandos ejecutados.
    - Resultado de build.
    - Resultado de tests.
    - Riesgos pendientes.
    - Estado productivo.
