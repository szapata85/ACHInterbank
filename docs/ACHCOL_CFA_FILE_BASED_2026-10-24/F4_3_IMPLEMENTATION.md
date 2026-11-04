# F4.3 — Protección de reintentos manuales y simulación segura, sin sobreingeniería

**Programa:** `ACHCOL-NACHA-CFA-EXTCYCLE-001`  
**Fecha:** 2026-10-25  
**Base:** `ACHInterbank_ACHCOL_CFA_F4_2_UNIFICADO.zip`  
**Entrega:** `ACHInterbank_ACHCOL_CFA_F4_3_UNIFICADO.zip`  
**Estado:** `IMPLEMENTED_UNVERIFIED`  
**Siguiente:** `F5.1` — homologación contractual del mapeo `Proc_Transacciones`.

## Qué cambia (mínimo necesario)

F4.2 ya dejó las filas `AchColFileBased` en estado `Blocked`, con prevalidación local sin SOAP. El procesamiento `CycleBound` ya tiene estados, auditoría, reintentos y manejo de incertidumbre. **No se crea una segunda cola, un segundo worker, una nueva tabla ni otra política de backoff.**

Una brecha real permanecía en el Command Center: su máquina de estados, compartida con el flujo tradicional, ofrecía `unblock`, `requeue` y `retry` aun para filas ACHCOL sin mapeo CFA autorizado. F4.3 cierra esa salida en el servidor y en las proyecciones de acciones disponibles (detalle y listado).

- Las acciones `ManualRetry`, `ManualUnblock` y `ManualRequeue` para `SourceKind=AchColFileBased` son rechazadas con el código estable `ACHCOL_FILEBASED_MANUAL_RELEASE_FORBIDDEN_F43` **independientemente del feature flag** y del modo SOAP.
- Cada intento rechazado deja un `IncomingNachaProcessingEvent` de transición `Rejected`, con ID de cola, actor, justificación y clave idempotente en la evidencia habitual. No se crean `IncomingNachaIntegrationExecution`, ni nuevos intentos ni fechas de reintento.
- Para cierre administrativo sin transmisión previa se conserva `ManualMarkFailedFinal` solo cuando `AttemptCount=0` y no existe ejecución CFA registrada. Si alguna ejecución existe (éxito, error o desenlace incierto), tampoco puede cerrarse automáticamente como falla final: requiere conciliación operacional y el diseño posterior F5.
- Las acciones disponibles en las vistas de detalle/listado no ofrecen reintentar, reencolar ni desbloquear ACHCOL. La protección **efectiva** está en el servicio, de manera independiente de la interfaz.
- `CycleBound` conserva sus transiciones, reintentos y estados previos. `IncomingNachaFileBasedPreparationStage` sigue simulando/prevalidando localmente con registros `Blocked`, sin SOAP incluso si el modo legado está configurado en `Live`.

## Qué no se implementa en F4.3

- **NO** liberar despachos ACHCOL (`Queued`, `RetryPending`, `Dispatched`), ni generar XML de CFA, ni abrir una segunda ruta `Live`.
- **NO** asumir que `ExternalCycleNumber` sustituye `AchCycleId`/`cycle.id` en un mapeo SOAP: F5.1 homologará contrato y fuentes.
- **NO** añadir migraciones, colas, catálogos o motores genéricos de reconciliación. Los controles existentes se reutilizan.
- **NO** declarar ejecutadas las pruebas .NET/BD/CFA que no pudieron realizarse; comprobación integral en F6.

## Cambios de fuentes

1. `IncomingNachaCommandCenterOperationsService.cs`: guard de origen y de intentos CFA previo a las transiciones manuales, evento de rechazo con código estable.
2. `IncomingNachaCommandCenterQueryService.cs`: cálculo de acciones seguras según `SourceKind` tanto en listado como en detalle.
3. `IncomingNachaCommandCenterServiceTests.cs`: regresiones para retry, unblock y requeue; cierre sin envío; bloqueo de cierre con intento previo; visibilidad de acciones.
4. `IncomingNachaFileBasedOrchestrationF42Tests.cs`: prevalidación en modo `Live` del despacho tradicional con mock SOAP estricto; para ACHCOL es una **simulación local**, no un envío.
5. `PLAN.md`, `MEMORY_STATE.md`, `CHATGPT_MICRO_START.md`, `README_PAQUETE_UNIFICADO.md`, este informe.

## Decisiones técnicas

- **DEC-057 — Reusar resiliencia existente:** sin esquemas, componentes de colas ni motores de reintentos adicionales para ACHCOL en F4.3.
- **DEC-058 — Guard incondicional en servidor:** acciones manuales de liberación/reenvío prohibidas para `AchColFileBased` hasta F5; ocultarlas en UI no es suficiente.
- **DEC-059 — Incertidumbre fail-closed:** una cola ACHCOL con algún intento/execution registrado no admite `MarkFailedFinal` manual sin conciliación; nunca se interpreta timeout como fallo financiero definitivo.
- **DEC-060 — Simulación mínima:** aprovechar la prevalidación F4.2 como simulación sin SOAP; no crear un nuevo endpoint de simulación ni transmitir desde F4.3.

## Evidencia y límites

- Las pruebas nuevas son xUnit/InMemory, escritas sobre las APIs existentes y sin conectividad real. **No ejecutadas**: no se encuentra el SDK de .NET 10 en este entorno.
- Comprobación estática del guard, salida de acciones, ausencia de cambios de esquema y separación del worker tradicional. El proyecto completo y sus metadatos Git se preservan en el ZIP (verificación de entradas y CRC en cierre).
- Concurrencia multiinstancia, comportamiento real de PostgreSQL/SQL Server, respuestas SOAP inciertas y certificación de integración siguen pendientes para F6. F5 diseñará mapeos y liberación eventual, sin envíos reales no autorizados.
- El nuevo guard no resuelve por sí solo idempotencia distribuida ni define procedimientos de conciliación CFA; no se afirma esa capacidad.
