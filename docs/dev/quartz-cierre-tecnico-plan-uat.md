# Quartz — cierre técnico y plan UAT

## 1) Resumen ejecutivo
Este documento cierra técnicamente la fase de hardening Quartz y define un plan UAT ejecutable para validar en ambiente real:
- `RAMJobStore` en Development.
- `Persistent AdoJobStore` en UAT con PostgreSQL.
- Schema `QRTZ_*` aplicado y accesible.
- Sincronización DB↔Quartz operativa.
- Ejecución trazable por `TaskExecutionLogs`.

## 2) Estado técnico alcanzado
- `SchedulerSyncService` implementa reconciliación DB↔Quartz (full + incremental).
- `DynamicJobExecutor` centraliza ejecución de tareas.
- `DynamicJob` permite `AllowParallel`.
- `NonConcurrentDynamicJob` aplica serialización para `SkipIfRunning/Queue`.
- `RetryOnFailure`, `MaxRetries`, `RetryBackoffSeconds` implementados.
- `CalendarPolicy` usa `TimeZoneId` con fallback `America/Bogota`.
- `ShiftToNextBusinessDay` no usa `RescheduleJob` destructivo.
- Persistent store quedó **configurable** (no activado por defecto en Development).
- Scripts `QRTZ_*` en `artifacts/sql/quartz/`.

## 3) Arquitectura final Quartz
- `SchedulerSyncService`: sincroniza `TaskDefinitions` en Quartz (`job:{id}` / `trg:{id}` / `db-tasks`).
- `DynamicJobExecutor`: carga task, evalúa calendario, resuelve handler, ejecuta retry, guarda logs.
- `DynamicJob` / `NonConcurrentDynamicJob`: wrappers para semántica de concurrencia.
- `QuartzJobStoreOptions`: decide RAM vs Persistent por configuración.

## 4) Configuración por ambiente
### Development/local
- `Quartz:JobStore:Mode=RAM`
- `Clustered=false`
- No requiere `QRTZ_*`
- Recomendado para pruebas rápidas/locales

### UAT (PostgreSQL)
- `Quartz__JobStore__Mode=Persistent`
- `Quartz__JobStore__Provider=Postgres`
- `Quartz__JobStore__TablePrefix=QRTZ_`
- `Quartz__JobStore__Clustered=false` inicial (o `true` si hay más de un nodo)
- `Quartz__JobStore__PerformSchemaValidation=true`
- Schema oficial Quartz `QRTZ_*` aplicado en PostgreSQL

### Producción
- Igual a UAT con ajuste de `Clustered` según topología
- Relojes sincronizados entre nodos
- Observabilidad sobre `QRTZ_*` y `TaskExecutionLogs`

## 5) Checklist previo UAT
1. Confirmar appsettings/env de UAT.
2. Confirmar conexión PostgreSQL.
3. Aplicar script oficial Quartz PostgreSQL `QRTZ_*`.
4. Validar tablas `QRTZ_*`.
5. Validar permisos usuario DB sobre `QRTZ_*`.
6. Confirmar `TaskDefinitions` disponibles.
7. Confirmar handlers `ITaskHandler` registrados.
8. Confirmar API arranca sin error.
9. Confirmar logs Quartz sin schema validation error.
10. Confirmar inicio de `SchedulerSyncService`.

## 6) Plan UAT paso a paso
### A) Validar RAM en Development
1. Ejecutar API con `Mode=RAM`.
2. Confirmar que no exige `QRTZ_*`.
3. Confirmar programación en memoria por `SchedulerSyncService`.
4. Confirmar trazas en `TaskExecutionLogs`.

### B) Validar Persistent en UAT/Postgres
1. Configurar ENV:
   - `Quartz__JobStore__Mode=Persistent`
   - `Quartz__JobStore__Provider=Postgres`
   - `Quartz__JobStore__TablePrefix=QRTZ_`
   - `Quartz__JobStore__PerformSchemaValidation=true`
2. Aplicar script oficial `QRTZ_*` PostgreSQL.
3. Arrancar API.
4. Confirmar ausencia de error de schema Quartz.
5. Confirmar carga de `QRTZ_JOB_DETAILS` y `QRTZ_TRIGGERS`.

### C) Validar reconciliación
1. Crear/habilitar `TaskDefinition`.
2. Esperar ciclo de sync.
3. Verificar `QRTZ_JOB_DETAILS` / `QRTZ_TRIGGERS`.
4. Deshabilitar task.
5. Verificar eliminación en scheduler.
6. Rehabilitar task.
7. Verificar recreación.

### D) Validar ejecución
1. Definir task de prueba con cron corto / `EveryNMinutes`.
2. Esperar ejecución.
3. Verificar `QRTZ_FIRED_TRIGGERS` durante ejecución.
4. Verificar `TaskExecutionLogs.Success=true`.
5. Forzar error controlado (o handler inexistente controlado).
6. Verificar `TaskExecutionLogs.Success=false` + `Error`.

### E) Validar retry
1. Configurar task de prueba: `RetryOnFailure=true`, `MaxRetries=2`, `RetryBackoffSeconds=0..5`.
2. Ejecutar escenario de fallo y recuperación controlada (si existe handler de prueba).
3. Verificar `Output/Error` con intentos.
4. Si no existe handler de prueba UAT, usar evidencia de unit tests y registrar pendiente funcional controlado.

### F) Validar concurrencia
- **SkipIfRunning**: job largo + trigger corto; verificar no solapamiento.
- **Queue**: validar serialización básica (no cola durable avanzada).
- **AllowParallel**: permitir solapamiento solo con handler idempotente/seguro.

### G) Validar reinicio
1. Con persistent activo, detener API.
2. Confirmar persistencia de `QRTZ_TRIGGERS`.
3. Iniciar API.
4. Confirmar reanudación de scheduler.
5. Verificar que reconciliación no duplique jobs.

## 7) Casos de prueba UAT
- API inicia sin excepción Quartz.
- `QRTZ_*` accesible y consistente.
- Alta/baja/reactivación de tasks sincroniza correctamente.
- Logs de ejecución reflejan éxito/error.
- Retry muestra intentos.
- Concurrencia respeta policy.

## 8) Consultas SQL de verificación
```sql
SELECT * FROM "TaskDefinitions";
SELECT * FROM "TaskParameters";
SELECT * FROM "TaskExecutionLogs" ORDER BY "StartedAt" DESC LIMIT 50;

SELECT * FROM "QRTZ_JOB_DETAILS";
SELECT * FROM "QRTZ_TRIGGERS";
SELECT * FROM "QRTZ_FIRED_TRIGGERS";
SELECT * FROM "QRTZ_SCHEDULER_STATE";
SELECT * FROM "QRTZ_LOCKS";

SELECT COUNT(*) FROM "QRTZ_JOB_DETAILS";
SELECT COUNT(*) FROM "QRTZ_TRIGGERS";
SELECT COUNT(*) FROM "TaskExecutionLogs";

SELECT *
FROM "TaskExecutionLogs"
WHERE "Success" = false
ORDER BY "StartedAt" DESC
LIMIT 50;

SELECT "TaskDefinitionId", "StartedAt", "FinishedAt", "Success"
FROM "TaskExecutionLogs"
ORDER BY "TaskDefinitionId", "StartedAt" DESC;
```

## 9) Criterios de aceptación
- API arranca sin errores Quartz.
- `QRTZ_*` existe y es accesible.
- `QRTZ_TRIGGERS` se llena.
- `SchedulerSyncService` crea/recrea jobs.
- Disabled/expired se eliminan.
- `TaskExecutionLogs` registra éxito/error.
- Retry evidenciado o justificado por unit tests si no hay handler de prueba UAT.
- SkipIfRunning/Queue no solapan.
- AllowParallel permite solapamiento solo con handler seguro.
- Reinicio no pierde triggers persistent.
- No hay huérfanos tras reconciliación.

## 10) Evidencias esperadas
- Capturas/logs de arranque.
- Salidas SQL (`QRTZ_*` + `TaskExecutionLogs`).
- Registro de casos ejecutados y resultado (Pass/Fail/Blocked).
- Evidencia de configuración usada por ambiente.

## 11) Riesgos pendientes
- `Queue` actual: serialización básica, no cola durable avanzada.
- Prueba multi-nodo real pendiente si `Clustered=true`.
- Misfire policies funcionales pendientes.
- Backoff avanzado (jitter/exponencial) pendiente.
- Observabilidad/alertas de drift/locks pendiente.
- Validar con handlers ACH reales bajo ventanas controladas para evitar ejecuciones financieras no deseadas.

## 12) Plan de rollback
- Development: volver a `Mode=RAM`.
- UAT/Prod: desactivar `Persistent` por configuración si incidente operativo.
- No eliminar `QRTZ_*` sin análisis de impacto/estado de jobs y aprobación DBA.

## 13) Recomendaciones para producción
- Mantener script oficial Quartz versionado por infraestructura.
- Activar `Clustered=true` solo con validación multi-nodo.
- Monitorear `QRTZ_FIRED_TRIGGERS`, locks, y fallos en `TaskExecutionLogs`.
- Definir runbook de incidentes Quartz (drift, locks, misfires, reconcilación).

## 14) Referencias
- `docs/dev/quartz-jobs-auditoria-funcional.md`
- `docs/dev/quartz-persistent-store-operacion.md`
- `artifacts/sql/quartz/README.md`
- `artifacts/sql/quartz/postgres-qrtz-schema.sql`
- `artifacts/sql/quartz/sqlserver-qrtz-schema.sql`
