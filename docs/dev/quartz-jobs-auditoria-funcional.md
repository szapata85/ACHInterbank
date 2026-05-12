# Auditoría funcional Quartz.NET — ACH Interbank

## 1) Resumen ejecutivo
Esta auditoría confirma que el proyecto sí ejecuta jobs Quartz en producción, pero con riesgos importantes en sincronización dinámica, calendarios, concurrencia declarada vs efectiva y portabilidad de zona horaria. La fuente de verdad del scheduling dinámico está en `TaskDefinition` + `SchedulerSyncService` + `DynamicJob`.

**Estado general:** Parcial / Riesgo productivo.

---

## 2) Arquitectura actual Quartz
- Registro Quartz en DI: `AddQuartz` + `AddQuartzHostedService(WaitForJobsToComplete=true)`.  
- Servicio adicional de sincronización: `SchedulerSyncService` (`BackgroundService`) que consulta BD cada minuto y hace `ScheduleJob(... replace=true)`.  
- Ejecución dinámica: `DynamicJob` resuelve handler por `TaskDefinition.Code` (`IEnumerable<ITaskHandler>`).  
- Job dedicado bulk ingestion: `ProcessBulkIngestionBatchJob`, programado ad-hoc por `AchBulkJobScheduler`.

### Observación clave de configuración
No se encontró configuración explícita de job store persistente Quartz (ni `QRTZ_*`/AdoJobStore en `appsettings`/compose), por lo que la operación parece depender del job store por defecto (RAM) y resincronización desde BD al iniciar proceso.

---

## 3) Inventario de jobs `IJob`
1. `DynamicJob` (`src/.../ACH/Quartz/Jobs/DynamicJob.cs`).
2. `ProcessBulkIngestionBatchJob` (`src/.../ACH/Quartz/Jobs/Implementation/ProcessBulkIngestionBatchJob.cs`).

## 4) Inventario de handlers `ITaskHandler`
1. `AchCycleSeederHandler` → Code `AchCycleSeeder`.
2. `AchCycleSchedulerHandler` → Code `AchCycleScheduler`.
3. `SeedBankHolidaysHandler` → Code `SeedBankHolidays`.
4. `CheckBankHolidaysHandler` → Code `CheckBankHolidays`.
5. `AchTacitAcceptanceJobHandler` → Code `AchTacitAcceptanceJob`.
6. `AchContrapartidasByCycleHandler` → Code `AchContrapartidasByCycle`.
7. `IncomingNachaPostProcessingHandler` → Code `IncomingNachaPostProcessing`.

---

## 5) Inventario de `TaskDefinition` (seed)
Sembradas por `TaskDefinitionSeeder`:
- `AchCycleSeeder` (Cron anual).
- `AchCycleScheduler` (DailyAtTime).
- `SeedBankHolidays` (Cron anual, `IgnoreCalendar`).
- `AchTacitAcceptanceJob` (EveryNMinutes=30).
- `AchContrapartidasByCycle` (EveryNMinutes=5).
- `IncomingNachaPostProcessing` (EveryNMinutes=3).

Parámetros semilla (`TaskParameterSeeder`):
- `SeedBankHolidays`: `SeedNextYears`, `Years`.
- `AchTacitAcceptanceJob`: `BatchSize`.
- `AchContrapartidasByCycle`: `MaxTransactionsPerCycle`, `ChunkSize`, `MaxCyclesPerRun`.
- `IncomingNachaPostProcessing`: `ChunkSize`.

### Matching handler vs task
- **TaskDefinition sin handler:** ninguno en semilla.
- **Handler sin TaskDefinition seed:** `CheckBankHolidays` (no aparece en `TaskDefinitionSeeder`).

---

## 6) Flujo `SchedulerSyncService` (diagnóstico)
### Qué hace bien
- Sincroniza primer ciclo con reconciliación completa y ciclos posteriores por `UpdatedAt > _lastSync` con watermark seguro (`syncStartedAt`).
- Elimina jobs si `Status=Disabled` o `EndAt` vencido.
- Construye trigger por periodicidad (Once, EveryNMinutes, HourlyAtMinute, DailyAtTime, Weekly, Monthly, Cron).

### Hallazgos
1. **(Mitigado)** Se elimina `scheduler.Start()` manual en `SchedulerSyncService`; el arranque queda delegado a `AddQuartzHostedService`.
2. **(Mitigado parcial)** Watermark endurecido con `syncStartedAt`; `_lastSync` sólo avanza al cerrar ciclo exitoso para reducir pérdida por carrera.
3. **(Mitigado)** Se incorpora reconciliación completa periódica DB↔Quartz para reponer jobs faltantes y validar drift.
4. **(Mitigado)** Se agrega limpieza de jobs huérfanos por grupo dinámico (`db-tasks`).
5. **`BuildTrigger` no tolera `TimeZoneId` inválido** (`FindSystemTimeZoneById` puede lanzar excepción y afecta ciclo completo).
6. **Concurrencia declarada no aplicada por tarea**: bloque `if SkipIfRunning` no implementa diferencia real (el job clase ya decide concurrencia).
7. **Sin misfire policy explícita** en cron/simple triggers.

---

## 7) Flujo `DynamicJob` (diagnóstico)
### Implementado
- Lee `TaskId` desde `JobDataMap`.
- Carga `TaskDefinition + Parameters`.
- Crea `TaskExecutionLog` inicial y guarda `StartedAt`.
- Resuelve handler por `Code` y ejecuta.
- Registra `FinishedAt`, `Success`, `Output/Error`.

### Hallazgos críticos/parciales
1. **`[DisallowConcurrentExecution]` fijo**: fuerza comportamiento tipo SkipIfRunning para todos los `DynamicJob`; `AllowParallel` y `Queue` no se implementan efectivamente.
2. **(Mitigado)** `RetryOnFailure`/`MaxRetries`/`RetryBackoffSeconds` implementados en `DynamicJob`; pendiente hardening avanzado (p. ej. backoff exponencial/jitter) según criticidad operativa.
3. **(Mitigado parcial)** Calendar policy ahora evalúa fecha local por `TimeZoneId` efectivo de task con fallback seguro a `America/Bogota` para `null/vacío/inválido`; pendiente hardening de observabilidad avanzada.
4. **(Mitigado)** `ShiftToNextBusinessDay` ya no reemplaza destructivamente el trigger recurrente; se adopta estrategia de skip seguro hasta próximo disparo hábil (Opción A).
5. **Si falla `SaveChanges` del log inicial/final**, no hay estrategia de recuperación ni fallback logging.

---

## 8) `ProcessBulkIngestionBatchJob` (diagnóstico)
- Registrado en DI (`AddTransient`).
- Programado por `AchBulkJobScheduler` con `StartNow()` y job/trigger identity basada en `batchId+timestamp`.
- `BatchId` inválido: registra warning y retorna.
- `AttemptId` opcional (nullable long).
- Tiene `[DisallowConcurrentExecution]`, pero cada ejecución usa identity distinta, por lo que no evita paralelismo entre batches distintos.
- Pasa `FireInstanceId` a `ProcessBatchAsync` como ayuda de trazabilidad/idempotencia.

**Riesgo:** Sin persistencia Quartz explícita, jobs en cola podrían perderse en reinicio abrupto antes de disparar.

---

## 9) Modelo y persistencia scheduler
- `TaskDefinition` incluye campos de calendario/concurrencia/retry/periodicidad.
- `TimeOfDay` persiste vía `TimeOfDayTicks` (long).
- `TaskParameters`: índice único por (`TaskDefinitionId`,`Key`).
- `TaskExecutionLog`: índice por `TaskDefinitionId`.
- `UpdatedAt/CreatedAt`: se actualizan en `SaveChanges` para entidades auditables.

**Riesgo de diseño:** hay configuración duplicada de tabla de `TaskDefinition` (`Tasks` en `IEntityTypeConfiguration` y `TaskDefinition` en `OnModelCreating`), potencial fuente de confusión/deriva.

---

## 10) Esperado vs actual (resumen)
| Componente | Esperado | Implementado | Estado | Riesgo | Recomendación |
|---|---|---|---|---|---|
| AddQuartz/HostedService | Arranque único y claro | Arranque delegado a HostedService (sin Start manual) | Mitigado | Bajo | Mantener guardrail de no redundancia |
| SchedulerSyncService | Sync robusta sin pérdida | Primer sync completo + incremental con watermark + reconciliación periódica | Mitigado parcial | Medio | Continuar con métricas de drift/recovery |
| BuildTrigger Cron/Weekly/etc | Trigger correcto + misfire | Correcto base, sin misfire explícito | Parcial | Medio | Definir misfire policies |
| Calendar OnlyBusinessDays | Basado en TZ de task | Evaluación por TZ efectiva + fallback Bogotá | Mitigado parcial | Medio | Fortalecer monitoreo/alertas por fallback TZ |
| ShiftToNextBusinessDay | Diferir sin romper recurrencia | Skip controlado sin `RescheduleJob` destructivo (Opción A) | Mitigado | Medio | Evaluar trigger one-shot adicional en siguiente iteración |
| ConcurrencyPolicy | `AllowParallel/Skip/Queue` efectivos | `DisallowConcurrentExecution` global en DynamicJob | Bug | Crítico | Implementar semántica real por policy |
| RetryOnFailure | Reintentos según config | Retry controlado implementado en DynamicJob | Mitigado | Medio | Evaluar política avanzada (exponencial/jitter) |
| TaskExecutionLog | Trazabilidad completa | Básica, sin resiliencia de persistencia | Parcial | Medio | Fallback logging/telemetría |
| Handler resolution | Code↔handler 1:1 | Funciona para seeds | OK/Parcial | Medio | Alertar handlers huérfanos |
| ProcessBulkIngestionBatchJob | Cola robusta post-restart | Schedule ad-hoc en Quartz local | Parcial | Alto | Persistencia Quartz o cola durable |

---

## 11) Hallazgos clasificados
### Críticos
1. ConcurrencyPolicy no respetada realmente (AllowParallel/Queue).
2. ShiftToNextBusinessDay puede romper recurrencia de cron.

### Altos
1. Retry básico mitigado; pendiente hardening avanzado de estrategia de backoff.
2. Dependencia probable de RAMJobStore (sin persistencia explícita).
3. CalendarPolicy evaluada con hora local, no TZ task.

### Medios
1. Ventana de carrera residual en `_lastSync` mitigada parcialmente con `syncStartedAt`; requiere observabilidad de borde en producción.
2. Reconciliación y borrado de huérfanos mitigados; pendiente hardening operativo (métricas/alertas).
3. `CheckBankHolidaysHandler` sin task seed asociada.
4. Configuración de tabla `TaskDefinition` duplicada (`Tasks` vs `TaskDefinition`).

### Bajos
1. Comentarios de código que sugieren comportamiento no implementado totalmente.

---

## 12) Gaps de pruebas
Pruebas presentes:
- `ProcessBulkIngestionBatchJobTests` (validación básica argumentos/flujo).
- Tests de handlers puntuales (p.ej. contrapartidas).

Faltan pruebas clave:
1. `SchedulerSyncService` sincronización incremental/reconciliación/borrado.
2. `DynamicJob` calendar policies por TZ.
3. `ShiftToNextBusinessDay` sin pérdida de recurrencia (agregado guardrail unitario que verifica ausencia de `RescheduleJob`/`shifted:` en `DynamicJob`).
4. ConcurrencyPolicy efectiva.
5. RetryOnFailure/MaxRetries/Backoff.
6. Matriz Code handler vs TaskDefinition (huérfanos).

---

## 13) Plan sugerido de commits (separados)
1. **feat/quartz:** implementar semántica real de `ConcurrencyPolicyEnum`.
2. **feat/quartz:** implementar `RetryOnFailure/MaxRetries/RetryBackoffSeconds` en `DynamicJob`.
3. **fix/quartz:** corregir `ShiftToNextBusinessDay` para no romper triggers recurrentes.
4. **fix/quartz:** calendario por `TimeZoneId` efectivo.
5. **refactor/quartz:** reconciliación robusta DB↔Quartz + watermark seguro.
6. **ops/quartz:** definir job store persistente/clustering/misfires para producción.
7. **test/quartz:** suite guardrail de scheduling dinámico.

---

## 14) Qué NO se cambió
- No se cambió lógica productiva de jobs.
- No se tocaron schedules ni handlers.
- No se tocaron migraciones ni contratos API.
- No se tocó SPA / transactions/create / Command Center.

---

## 15) Validación manual recomendada (local/UAT)
1. Crear/editar `TaskDefinition` y verificar alta/actualización en Quartz (logs + ejecución).
2. Cambiar `Status=Disabled` y confirmar eliminación de job en scheduler.
3. Probar `ShiftToNextBusinessDay` y confirmar que no se pierde recurrencia.
4. Probar task con `TimeZoneId` no local y validar calendario.
5. Simular fallo handler para verificar trazabilidad en `TaskExecutionLog`.
6. Reiniciar API y validar recuperación de jobs esperados.

---

## 16) Consultas SQL útiles
```sql
SELECT * FROM "TaskDefinitions";
SELECT * FROM "TaskParameters";
SELECT * FROM "TaskExecutionLogs" ORDER BY "StartedAt" DESC;
SELECT "Code", "Status", "PeriodicityType", "CronExpression", "TimeZoneId" FROM "TaskDefinitions";
SELECT * FROM "TaskExecutionLogs" WHERE "Success" = false ORDER BY "StartedAt" DESC;
```

Si existen tablas QRTZ_* en el ambiente:
```sql
SELECT * FROM "QRTZ_TRIGGERS";
SELECT * FROM "QRTZ_JOB_DETAILS";
```
