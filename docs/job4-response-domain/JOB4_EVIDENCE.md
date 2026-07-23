# JOB 4 — Evidencia del dominio de respuestas ACH

## Identificación

- Commit base: `8a78d320529cb94ff05747e230626de9219dd6b4`.
- Rama: `job4/dominio-respuestas`.
- HEAD funcional: `3da55a2decb305d993e44246035bc70995e2ff83`.
- HEAD de entrega: commit que contiene este documento (`git rev-parse HEAD`).
- Commits funcionales:
  - `3d26ecc79fcb53ccbf533c85bfec052341039061` — dominio, aplicación, API, persistencia, migraciones y pruebas backend.
  - `3da55a2decb305d993e44246035bc70995e2ff83` — workspace Angular y Playwright.

## Arquitectura implementada

- Resolución de mappings por `ClearingHouseId`, tipo/flujo, código, vigencia y prioridad, con resultados explícitos `Matched`, `NoMatch` y `Ambiguous`.
- Política central de transiciones; identidad lógica estable e índice único; recuperación de la inserción concurrente perdedora.
- Token `Guid Version` neutral por proveedor y respuestas HTTP 409 con `ProblemDetails`.
- Huérfanas persistentes, revisión manual exacta, conciliación operacional resolutiva y auditoría append-only.
- CRUD y activación de mappings con detección de vigencias incompatibles; desactivación sin borrado histórico.
- Solicitud gobernada e idempotente de reproceso, con motivo, correlación e intento persistido. El despacho/completado contra el pipeline real queda pendiente y determina el NO-GO.
- SPA con selector de cámara, mappings, revisión manual, conciliación, auditoría, manejo de 409 y vista móvil.

## Migraciones

- PostgreSQL: `20260722235815_Job4ResponseDomain`.
- SQL Server: `20260722235905_Job4ResponseDomain`.
- Ambos proveedores: migración forward, rollback y reaplicación ejecutados correctamente sobre contenedores locales.
- Backfill determinístico únicamente para cámaras reconocibles; filas legacy no correlacionables se preservan.

## Resultados ejecutados

| Bloque | Ejecutadas | Aprobadas | Fallidas | Omitidas | Resultado |
|---|---:|---:|---:|---:|---|
| Build Release | 1 | 1 | 0 | 0 | 0 warnings, 0 errors |
| Focalizadas finales de dominio/idempotencia | 19 | 19 | 0 | 0 | Aprobado |
| Filtro funcional amplio JOB 4 | 439 | 439 | 0 | 0 | Aprobado |
| Backend completo, resultado lógico final | 1917 | 1912 | 0 | 5 | Aprobado con omisiones preexistentes |
| Multimotor real | 2 | 2 | 0 | 0 | SQL Server y PostgreSQL aprobados |
| Angular unitarias | 458 | 458 | 0 | 0 | Aprobado |
| Angular build | 1 | 1 | 0 | 0 | Aprobado |
| Playwright Chromium real | 2 | 2 | 0 | 0 | Aprobado |

La primera invocación de la suite completa no recibió `CLEARING_HOUSES_REQUIRE_DATABASES=true`: informó 1910 aprobadas, 5 omitidas y 2 fallas de precondición. Esos dos casos se repitieron con conexiones locales reales y aprobaron 2/2. No hubo llamadas SOAP ni operaciones monetarias.

Playwright utilizó API, SPA y SQL Server locales sin `page.route` ni mocks de API. Validó mapping, edición, 409, recepción concurrente duplicada, huérfana, asociación manual, solicitud de reproceso, conciliación, auditoría y viewport móvil; errores JavaScript, HTTP 404 inesperados y HTTP 500 inesperados: 0.

## Archivos principales

- `src/Cfa.ACHInterbank.Domain/Models/ACH/AchResponseOperations.cs`
- `src/Cfa.ACHInterbank.Domain/Models/ACH/AchResponseStatePolicy.cs`
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchResponseOperationsService.cs`
- `src/Cfa.ACHInterbank.Api/Controllers/AchResponseOperationsController.cs`
- `src/Cfa.ACHInterbank.Api/Controllers/AchReconciliationController.cs`
- `tests/Cfa.ACHInterbank.Tests/Job4ResponseDomainTests.cs`
- `web/ach-interbank-ui/src/app/features/ach-responses`
- `web/ach-interbank-ui/e2e/ach-responses-job4.spec.ts`

# JOB 4.1 — Dispatcher de reprocesos

- Commit base: `cefccb2748c0c2cbf4372d7afcd1bb9176524b14`.
- Rama: `job4.1/reprocess-dispatcher`.
- Dispatcher: tarea Quartz canónica `ach-response-reprocess-dispatcher`, handler registrado por el patrón dinámico existente y ejecución manual por `/scheduler/tasks`.
- Claim: `ExecuteUpdateAsync` condicional para `Pending` o `Running` con lease vencido; el ganador asigna instancia, heartbeat y lease.
- Recuperación: los leases vencidos se reclaman y auditan como `ReprocessLeaseRecovered`; el lease se renueva explícitamente antes del pipeline.
- Pipeline: reevaluación sobre `AchResponse` persistida, sin crear recepción ni incrementar duplicados; reutiliza correlation ID y checkpoints de notificación.
- Terminales: `Completed`, `FailedFunctional`, `FailedTechnical`; la respuesta termina respectivamente en `Reprocesada`, `RequiereRevisionManual` o `ErrorTecnico`.
- Migraciones: `20260723000000_Job41ReprocessDispatcher` para PostgreSQL y SQL Server; incluyen columnas de lease/concurrencia, backfill de versión e índice de adquisición.
- Ejecutado: `dotnet restore`, build Release (0 warnings/0 errors) y pruebas focalizadas (9 aprobadas, 0 fallidas, 0 omitidas). La suite backend completa fue iniciada y excedió el límite de 120 s sin resultado final; no se contabiliza como aprobada.
- No ejecutado: validación real SQL Server/PostgreSQL, cluster Quartz de dos instancias, Angular y Playwright; requieren servicios/conexiones locales no disponibles en este worktree.
