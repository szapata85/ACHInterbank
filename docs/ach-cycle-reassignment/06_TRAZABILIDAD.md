# ACH-CR-001 — Matriz de trazabilidad a fuentes y código

## Código del ZIP base (inspección estática I00)

Se verificó la presencia y el contenido de los siguientes archivos; nombres y responsabilidades reales, no promesas sobre ejecución.

| Fuente | Evidencia observada | Uso/riesgo |
|---|---|---|
| `src/Cfa.ACHInterbank.Domain/Models/ACH/AchTransaction.cs` | `AchCycleId`, `AchBatchId`, `EffectiveEntryDate`, `State`, `TransactionExternalId`, `FileExportMemberships`, `StateEvents`, `ContrapartidaDispatchItem` | Identidad, elegibilidad y escritura controlada |
| `.../Domain/Models/ACH/AchCycle.cs` | `ProcessingDate`, ventanas/corte, `OperationalStatus`, `ClearingHouseId`, política de configuración | Resolver oportunidad y verificar expiración |
| `.../Domain/Models/ACH/AchBatch.cs` | Lote, ciclo, fecha efectiva, totales debit/credit | Coherencia de lote destino |
| `.../Persistence/ACH/Services/Implementation/RoutingStrategyService.cs` | Selecciona cámara por preferencias y luego ciclo; usa calendario y ventanas | Reutilizar piezas, **no permitir cambio de cámara automático** |
| `.../Persistence/ACH/Services/Implementation/BatchResolver.cs` | Asigna al crear y valida ventana; `EnsureCycleIsOpenForTransactions` se aplica fuera de ventana no CENIT | Validar caso sábado/proyección futura ACHCOL en I01 |
| `.../Persistence/ACH/Services/Implementation/NachaDataLoader.cs` | `LoadByCycleAsync` filtra `t.AchCycleId == cycleId` y `ExportableStates` | El nuevo ciclo determina selección NACHA; no basta cambiar referencia aislada |
| `.../Persistence/ACH/Services/Implementation/LiquidityOptimizationService.cs` | Reasigna ciclo y lote por insuficiencia CENIT en ciclos 1–3 y registra decisión/eventos/cola | Reutilización de patrón; política **no transversal** |
| `.../Persistence/ACH/Services/Implementation/CenitCycleQueueService.cs` | Añade registro con `OriginalAchCycleId`, `TargetAchCycleId` | No usar como cola para ACH Colombia |
| `.../Persistence/ACH/Services/Implementation/CenitCycleExecutionService.cs` | Consume cola CENIT marcándola `Consumed` y luego ejecuta netting/optimización | Investigar coherencia con ciclo de transacción para no duplicar |
| `.../Domain/Models/ACH/AchFileExportTransaction.cs` | Membresía por `AchTransactionId`, ciclo, lote y TraceNumber | Fuente importante para fail-closed |
| `.../Domain/Models/ACH/AchFileExport.cs` | Ciclo de vida de archivo, transmisión, ACK, hashes y versión | No presumir no envío ante respuesta pendiente |
| `.../Domain/Models/ACH/AchTransactionStateEvent.cs` | Motivo, JSON, idempotencia, timestamp, estado anterior/nuevo | Auditoría sin entidad adicional |
| `.../Persistence/ACH/Quartz/Jobs/Implementation/AchCycleSchedulerHandler.cs` | Adaptador `ITaskHandler`, composición DI, reloj | Patrón job, no depositar allí la regla |
| `.../Persistence/ACH/Quartz/Jobs/Implementation/AchContrapartidasByCycleHandler.cs` | Procesa ciclos activos con job reutilizable | Revisar el orden con futuro reasignador |
| `AGENTS.md`, `MEMORY.md`, `docs/CURRENT_PROJECT_STATUS.md` | Restricciones permanentes, memoria global, situación de cámaras | Mantener sin cambios durante I00 |

**Prefijo de las rutas abreviadas (`...`):** `src/Cfa.ACHInterbank.*`, según la ruta concreta indicada en la columna. Todos los archivos señalados están incluidos en el proyecto base.

## Fuentes normativas ya empaquetadas en el ZIP

| Documento en el proyecto | Localizador normativo / hallazgo |
|---|---|
| `docs/normativa/md/CENIT-DSP-152-Anexo-2.md` | Cap. 1 §3.1–3.3: cinco ciclos/cola de archivos fuera de ventana; §5.2: reglas de liquidez en ciclos 1–3 y 4–5 |
| `docs/normativa/md/Archivos de CENIT/Manual de Usuario CENIT.md` | §3.1–3.6, transacciones por sesión; 4 última sesión monetaria, 5 devoluciones/ROR |
| `docs/normativa/md/Archivos de CENIT/Manual de Especificaciones Formato NACHA-M CENIT.md` | §2: diferencia rechazo de archivo, rechazo formal y devolución; §3 estructura |
| `docs/normativa/md/ACH-Colombia-V35.md` | §2.4.1: cinco ciclos vigentes en V35; archivos/horarios según cámara |
| `docs/CURRENT_PROJECT_STATUS.md` | Candidata 7 ciclos inactiva; `EffectiveFrom` ausente y ambigüedad C4 11:01pm/am; prueba de runtime pendiente |

Fuente candidata externa cargada en el proyecto de ChatGPT pero **no integrada como norma vigente al ZIP**: `Nuevos Ciclos de Proceso y Horarios.pdf`. No activar política 7C por sí solo. El documento fuente contiene errata visible "cinco (7)" y comienzo de C4 "11:01pm". El diseño no debe resolver esos datos por suposición.

## Diferenciar evidencia y propuesta

- **Fuente/documento:** lo dicho literalmente por la cámara o manual del proyecto.
- **Observación de código:** estructura/letra verificada en el ZIP de I00; sin ejecución.
- **Diseño de ACH-CR-001:** política interna aún no implementada, salvo que `CHANGELOG.md` de un incremento posterior confirme lo contrario.

## Referencias de decisiones de continuidad

- `AGENTS.md`, reglas para fuentes normativas, Clean Architecture, no cambios a goldens y certificados.
- `MEMORY.md` global: estado de capacidades anteriores. No modificarlo solo para hacer reflejo duplicado en I00.
- `MEMORY_STATE.md` de ACH-CR-001: único control de avance de esta iniciativa.


## Evidencia de implementación I01

`AchPendingCycleEligibilityService` llama al calendario de cámara (`IOperationalCalendarService`), las ventanas con TZ (`IOperationalCycleWindowResolver`) y `ICycleTransactionPolicy`; también verifica pertenencia a archivo a través de `AchFileExportTransaction` y exportes `NACHA`. En la ruta controlada MFT (`AchColombiaManagedMftOutboundWorkflow`) aparece la etiqueta `NACHA`, mientras `NachaConfigPublicationService` usa `PUBLISH` para snapshots de configuración. Se documenta esta diferencia para no suponer falsamente que `PUBLISH` es siempre archivo transaccional. La validación de I01 es estática, no evidencia de recepción de red.


## Fuente de implementación I02

La persistencia nueva no codifica horarios de cámara: invoca `AchPendingCycleEligibilityService` I01 y reutiliza `ICycleTransactionPolicy`. El lote se resuelve por `IAchBatchRepository` (búsqueda/creación coherente con `BatchResolver`) y se recalcula por `ITransactionPersister`. El registro de modificación es `AchTransactionStateEvent`, configurado en `AchTransactionStateEventConfiguration` (`ReasonCode` longitud máxima 20; `IdempotencyKey` longitud 128). Atomicidad relacional por `AchDbContext`/EF; garantía inter-sistemas de exportación **pendiente**. Las referencias normativas I00–I01 permanecen sin alterar.

## Nueva trazabilidad de implementación I03

| Código | Uso comprobado por inspección estática |
|---|---|
| `Persistence/ACH/Quartz/Jobs/Implementation/AchPendingCycleReassignmentHandler.cs` | Handler acotado, guardias de ejecución, lectura keyset y delegación a comando I02. |
| `Persistence/Scheduler/SchedulerTaskCatalog.cs` | Reconoce `ACH_PENDING_CYCLE_REASSIGNMENT`/`AchPendingCycleReassignment`, no permite ejecución manual. |
| `Persistence/ACH/Services/Implementation/Seeders/TaskDefinitionSeeder.cs` | Seed de tarea inicialmente Disabled, sin retries ni recuperación automática. |
| `Persistence/ACH/Quartz/Jobs/DynamicJobExecutor.cs` | Registro existente de ejecución/errores en Quartz, reutilizado sin cambios. |
| `Persistence/ACH/Quartz/Jobs/NonConcurrentDynamicJob.cs` | Control existente de concurrencia del job, **no** coordina operaciones con exportadores. |
| `tests/Cfa.ACHInterbank.Tests/AchPendingCycleReassignmentHandlerTests.cs` | Nueve pruebas escritas sin ejecución runtime. |

Esto es una adaptación interna del proyecto; no representa un mandato normativo nuevo de ACH Colombia o CENIT.

## Evidencia I04 — proyección y calendario operativo

- `Persistence/ACH/Services/Implementation/RoutingStrategyService.cs`: proyecta por `GetNextBusinessDayAsync`; I04 omite ciclos Closed/Cancelled.
- `Persistence/ACH/Services/Implementation/BatchResolver.cs`: I04 admite `Before` para **fecha futura únicamente**, con calendario hábil y política de tipo sin cambiar; no modifica CENIT.
- `Persistence/ACH/Services/Implementation/AchPendingCycleEligibilityService.cs`: I01 continúa limitando el destino a `ProcessingDate == EffectiveEntryDate` y excluye jornada no hábil.
- `Persistence/ACH/Services/Implementation/AchPendingCycleReassignmentService.cs`: I02 revalida destino en misma fecha; audita y conserva fecha.
- `tests/Cfa.ACHInterbank.Tests/AchCycleCalendarProjectionTests.cs` y `AchPendingCycleEligibilityServiceTests.cs`: 10 escenarios añadidos en I04, escritos sin ejecutar.
- Fuentes normativas disponibles **no proporcionan una autorización contractual específica** para mutar la fecha valor de la instrucción originada; la decisión conservadora es interna y debe confirmarse con producto/cámara antes de habilitar rollover.

## Evidencia de código I05

- `.../Persistence/ACH/Services/Implementation/AchFileExportAuditService.cs`: generación NACHA con admisión serializable y verificación de membresía, antes de registro de archivo.
- `.../Persistence/ACH/Services/Implementation/NachaSecurity/NachaSecurityOperationService.cs`: auditoría por cada partición ANTES de almacenar y habilitar descarga; el flujo manual cifrar/descifrar sigue separado.
- `.../Persistence/ACH/Services/Implementation/AchPendingCycleEligibilityService.cs`: guardia de ciclo exportado y actividad CENIT de ciclo fuente.
- `.../Persistence/ACH/Services/Implementation/AchColombiaManagedMftOutboundWorkflow.cs`: ya llamaba auditoría NACHA antes del handoff, sin modificación I05.
- `.../Api/Controllers/NachaExportController.cs`: ya auditaba antes de devolver archivo, sin modificación I05.
- `.../Persistence/ACH/Services/Implementation/LiquidityOptimizationService.cs`: mecanismo regulado CENIT separado; NO modificado en I05.

La norma CENIT describe diferimiento/liquidez; la decisión de admisión serializable y la denegación conservadora por ciclo son políticas internas de seguridad, no una instrucción textual del operador.

## Trazabilidad de cierre I06

- `I01 → I05`: los hechos de cada modificación son los archivos históricos `07_...` a `11_...`; no se reescriben como pruebas ejecutadas.
- `I06`: `12_I06_AUDITORIA_Y_CIERRE.md` consolida código/arquitectura, decisiones y riesgos; `13_PLAN_HOMOLOGACION_GO_NO_GO.md` contiene las puertas verificables pendientes.
- Los manuales CENIT, ACH Colombia V35 y NACHA-M del repositorio/contexto son referencias de política; el cambio de fecha valor y la activación de 7C **no están autorizados** por la implementación de reasignación.
- Toda evidencia SQL, Quartz, MFT, respuesta de cámara, UAT o aprobación futura debe anexarse con fecha, ambiente, artefacto y resultado explícitos; hoy: **NO EJECUTADA**.
