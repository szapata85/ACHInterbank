# Quartz.NET en producción

## Arquitectura

`ACHInterbankScheduler` se ejecuta dentro de cada réplica de la API. En perfiles persistentes usa `JobStoreTX`, las mismas tablas `QRTZ_` y la misma base de datos funcional; Quartz resuelve adquisición y locks distribuidos, mientras `TaskExecutionLog` conserva el historial funcional visible al usuario. Angular consume únicamente `/api/scheduler` y el backend traduce códigos funcionales permitidos a `JobKey`; nunca acepta tipos .NET, SQL ni nombres arbitrarios.

El cierre de la API espera los jobs en curso. Cada réplica usa `Quartz__InstanceId=AUTO`, un `Quartz__InstanceName` propio y publica heartbeat funcional en `SchedulerInstanceStates`. El health check de readiness distingue base de datos, scheduler iniciado, store persistente e instancia de clúster.

## Configuración operativa

Los valores se enlazan desde `Quartz` y `Quartz:JobStore`:

- `SchedulerName`, `InstanceId`, `InstanceName`.
- `MaxConcurrency`, `MaxBatchSize`, `BatchFireAheadMilliseconds` y `AcquireTriggersWithinLock`.
- `Mode=Persistent`, `Provider=Postgres|SqlServer`, `TablePrefix=QRTZ_` y `Clustered=true`.
- `ClusterCheckinIntervalSeconds`, `MisfireThresholdMilliseconds`, `InstanceHeartbeatSeconds` y `OfflineThresholdSeconds`.
- `PerformSchemaValidation=true` para fallar con un diagnóstico explícito cuando el esquema no existe o está incompleto.

Los perfiles Production y los dos perfiles de clúster no usan `RAMJobStore`. Development puede usar memoria para desarrollo aislado; cualquier prueba distribuida lo reemplaza explícitamente por store persistente.

## Esquema y locks

Los scripts versionados son `artifacts/sql/quartz/postgres-qrtz-schema.sql` y `artifacts/sql/quartz/sqlserver-qrtz-schema.sql`. Crean las 11 tablas internas de Quartz 3.18 de forma no destructiva, no borran datos, aceptan reaplicación y rechazan un esquema parcial. Estas tablas no forman parte del modelo EF funcional. Las migraciones EF agregan el historial, heartbeats, sonda técnica, índices, unicidad e información de programación.

La exclusión distribuida proviene de `QRTZ_LOCKS` y `JobStoreTX`; `AcquireTriggersWithinLock` se mantiene configurable. Los handlers no concurrentes usan además `[DisallowConcurrentExecution]`, sin tratarlo como garantía de exactamente una vez.

## Misfire, recuperación e idempotencia

Cada tarea persiste `DoNothing` o `FireAndProceed`. El sincronizador aplica la instrucción de misfire al cron/simple trigger y el listener registra `Misfired` sin propagar fallos a Quartz. La descripción en español y la política son visibles y editables desde el SPA.

`RequestsRecovery` se habilita por tarea, no globalmente. Los handlers financieros no se marcaron recuperables. La sonda técnica —solo habilitable fuera de Production mediante `Scheduler__Probe__Enabled=true`— permite verificar recovery y guarda `IsRecovery`, fire original, instancia recuperadora, inicio y resultado. Su efecto usa una clave única `ProbeKey`; el historial usa `ExecutionId` único y la ejecución manual usa `RequestId` e `IdempotencyKey` únicos más una clave activa por tarea.

## Seguridad y operación

Permisos independientes: `Scheduler.View`, `Scheduler.History.View`, `Scheduler.Execute`, `Scheduler.ManageSchedule`, `Scheduler.PauseResume` y `Scheduler.ViewInstances`. El backend valida siempre el permiso. Ejecutar ahora exige un GUID de request, motivo de 10 a 500 caracteres y una tarea habilitada; un request repetido devuelve la ejecución original y una tarea no concurrente activa devuelve 409. Usuario, motivo, correlation ID, instancia y resultado quedan auditados. Pausar, reanudar y editar programación no permiten forzar concurrencia ni modificar tipos de job.

La ruta `/scheduler/tasks` ofrece resumen, tareas, programación legible, cron avanzado, zona horaria, misfire, instancias, historial paginado y ejecución manual. El polling es moderado; las acciones se ocultan según permisos y se bloquean durante el envío.

## Docker

PostgreSQL:

```powershell
docker compose -f docker-compose.scheduler-cluster.yml up -d --build
docker compose -f docker-compose.scheduler-cluster.yml ps
docker compose -f docker-compose.scheduler-cluster.yml down
```

SQL Server:

```powershell
docker compose -f docker-compose.scheduler-cluster.sqlserver.yml up -d --build
docker compose -f docker-compose.scheduler-cluster.sqlserver.yml ps
docker compose -f docker-compose.scheduler-cluster.sqlserver.yml down
```

Cada perfil inicia el motor, aplica primero el esquema Quartz, levanta `achinterbank-api-01`, `achinterbank-api-02` y el SPA, y asigna puertos host distintos. Las credenciales deben inyectarse mediante variables de entorno; no deben añadirse a logs ni documentación.

## Diagnóstico

1. Consultar `/health/ready` en ambas APIs y exigir `database`, `scheduler`, `persistentStore` y `clusterInstance` saludables.
2. Consultar `/api/scheduler/instances` y comparar ID, heartbeat y jobs activos.
3. Revisar `/api/scheduler/history` por `ExecutionId` o `CorrelationId`, sin usar las tablas Quartz como historial de usuario.
4. Verificar que las dos réplicas comparten `SchedulerName`, base y prefijo, pero no `InstanceName` ni ID automático.
5. Si el esquema está ausente o parcial, detener la API, aplicar el script correspondiente y reiniciar; no borrar tablas para corregirlo.

