# I03 — Automatización opt-in Quartz de reasignación pendiente

**Corte:** 2026-10-25 · **Iniciativa:** ACH-CR-001 · **Base:** ZIP I02 completo · **Salida:** ZIP I03 completo.

## Resultado

Se añade `AchPendingCycleReassignmentHandler : ITaskHandler` en `Persistence/ACH/Quartz/Jobs/Implementation/` para invocar el comando I02 por cada transacción candidata. Se registra en el `SchedulerTaskCatalog` existente como `ACH_PENDING_CYCLE_REASSIGNMENT` (handler `AchPendingCycleReassignment`) y en `TaskDefinitionSeeder`. Usa registro DI de handlers vía Scrutor, `DynamicJobExecutor`, `SchedulerSyncService` y auditoría `TaskExecutionLog` ya implementados. No se agrega un temporizador independiente, esquema SQL, endpoint, migración, servicio externo o nueva abstracción de dominio.

## Activación: **NO HABILITAR EN PRODUCCIÓN**

La definición inicial se crea con `TaskStatusEnum.Disabled`, `ManualExecutionEnabled=false`, `ConcurrencyPolicy=SkipIfRunning`, `RetryOnFailure=false`, `RequestsRecovery=false`, `MisfirePolicy=DoNothing`, periodicidad por defecto 5 minutos y calendario actual. El catálogo no admite ejecución manual del task. El handler añade una **segunda barrera** obligatoria de configuración: `AchCycleReassignment:EnableExecution` debe ser exactamente `true` para ejecutar; cualquier ausencia, valor inválido o `false`, o tarea `Disabled`/`Paused`, produce una salida sin cambios. Esta segunda condición NO certifica que los exportadores sean seguros: **I05 debe resolver y probar la carrera antes de habilitarla operativamente**. No se añade esta clave a `appsettings` para evitar activar accidentalmente.

## Algoritmo implementado

1. Tomar hora UTC mediante `TimeProvider`; preseleccionar únicamente ciclos operacionales alrededor de la jornada actual (margen UTC de -1 a +1 día) y determinar **corte vencido** mediante `IOperationalCycleWindowResolver` y TZ oficial configurada, registrando/omitiendo ciclos con zona desconocida.
2. Seleccionar `AchTransactions` mediante proyección de ids `AsNoTracking`, dirección saliente determinada, origen CFA y estado `Pending`; excluir retornos/reversiones y evidencia relacional de archivo o contrapartida.
3. Leer por paginación **keyset por `Id`** en fragmentos de máximo 50, con topes `BatchSize` (predeterminado 25, máximo 100) y `MaxScanned` (predeterminado 250, máximo 1000). Los parámetros persistidos no válidos toman valor por defecto. Cada candidato se pasa **únicamente** a `IAchPendingCycleReassignmentService.ReassignAsync`, sin mutación del handler.
4. El comando I02 vuelve a ejecutar toda la validación I01: calendario, cámara, fecha valor, ciclo permitido, exportación, despacho, cola CENIT, liquidación y archivo de pertenencia incierta; si pasa, guarda nuevo lote/ciclo y evento mediante unidad de trabajo relacional.
5. Registrar `trasladadas`, `omitidas`, `fallidas`, `evaluadas`; continuar ante un error particular y lanzar error agregado al final si hubo fallas, para que Quartz marque ejecución como fallida. La cancelación se propaga sin ocultarla.

**Límite conservador conocido:** los candidatos ordenan desde menor Id en cada ejecución. Si los primeros `MaxScanned` ids permanecen perpetuamente inelegibles (por motivos que solo I01 detecta), podrían postergar candidatos posteriores. No se incorpora checkpoint ni cursor persistido en I03 para evitar sobreingeniería o cambio de esquema; registrar el comportamiento real en I06 y decidir un ajuste basado en evidencia.

**No se añade `[DisallowConcurrentExecution]` al handler**, porque el handler no es un `IJob`; la coordinación a nivel job corresponde al `NonConcurrentDynamicJob`/`ConcurrencyPolicy=SkipIfRunning` del scheduler ya existente. Aún no hay protocolo común con un exportador concurrente; tampoco puede inferirse ese protocolo de Serializable en I02.

## Archivos agregados

- `src/Cfa.ACHInterbank.Persistence/ACH/Quartz/Jobs/Implementation/AchPendingCycleReassignmentHandler.cs`
- `tests/Cfa.ACHInterbank.Tests/AchPendingCycleReassignmentHandlerTests.cs` — nueve `[Fact]` escritos.
- `scripts/validate-cycle-reassignment-i03-static.py`
- `docs/ach-cycle-reassignment/09_I03_IMPLEMENTACION_Y_EVIDENCIA.md`

## Archivos de código modificados

- `src/Cfa.ACHInterbank.Persistence/Scheduler/SchedulerTaskCatalog.cs`: entrada para la tarea.
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/Seeders/TaskDefinitionSeeder.cs`: crear definición deshabilitada y metadatos legibles; no alterar tareas previas ajenas.

## Escenarios de los 9 tests escritos

1. Task habilitada, flag ausente/false: no invoca I02.
2. Flag true, task Disabled: no invoca I02.
3. Task Paused: no invoca I02.
4. Más candidatas que BatchSize: respeta límite y cuenta éxitos.
5. Corte aún no vencido: no selecciona candidatas.
6. Membresía de archivo presente: se excluye antes de invocar I02.
7. Una candidata denegada: continúa con la siguiente.
8. Una candidata falla: continúa, contabiliza y finaliza con error agregado.
9. Cancelación solicitada: se propaga, sin invocar I02.

**Pruebas unitarias: ESCRITAS, NO EJECUTADAS.** SDK .NET y proveedores relacionales no disponibles en este ambiente. Deben ejecutarse `dotnet build` y `dotnet test` en CI, y pruebas SQL Server/PostgreSQL con Quartz persistente, cluster/recovery, orden exportador/archivo y revisión UAT antes de despliegue. La inspección Python verifica estructura y contratos por patrones de texto; no sustituye compilación ni asegura semántica transaccional.

## Dependencias/risks abiertos

- `RISK-CR-001`: carrera exportador/dispatch frente a la reasignación; **CRÍTICO** hasta I05. Mantener ejecución desactivada.
- `RISK-CR-002`: `BatchResolver` en sábado con ciclo futuro ACHCOL; I04.
- `RISK-CR-003`: fecha valor después de último ciclo / siguiente jornada; I04.
- `RISK-CR-004`: convivencia con CENIT queue y liquidez; I05.
- `RISK-CR-006`: ACH Colombia 7C candidata, no activada; sin cambiar política.
- `RISK-CR-007`: build / runtime / UAT no efectuados.
- `RISK-CR-008`: selección acotada desde primer Id sin checkpoint inter-ejecución; medir starvation/repetición antes de ampliar alcance.

## Validación y entrega

- `scripts/validate-cycle-reassignment-i03-static.py`: controles del handler, catálogo, seeder, tests, memoria y documentación.
- Ejecutar además validadores I01 e I02, comprobación estructural del ZIP completo y CRC, comparar ruta+tamaño+CRC contra ZIP I02 y revisar proyectos de solución.
- El SHA-256 del ZIP final va en archivo `.sha256` externo (no autorreferenciar el artefacto).

**NEXT_INCREMENT: I04.** **Restan 3 incrementos**: I04, I05, I06. Estado de ejecución real: `NOT_RUN_ENVIRONMENT`; no declarar listo para producción.
