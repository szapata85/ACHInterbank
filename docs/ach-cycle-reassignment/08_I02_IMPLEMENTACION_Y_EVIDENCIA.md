# ACH-CR-001 — I02: reasignación transaccional y auditoría (2026-10-25)

**Estado exacto:** `IMPLEMENTADO_ESTATICAMENTE`, sin compilación/pruebas .NET ejecutadas ni homologación de concurrencia con generadores/exportadores externos. No autoriza automatización, despliegue o go-live.

## Alcance real

- Contrato de Application `IAchPendingCycleReassignmentService`, resultado `AchPendingCycleReassignmentResult` sin detalle sensible.
- Implementación `[Scoped] AchPendingCycleReassignmentService` en Persistence. Una transacción por invocación, sin cambios a creación, Quartz, NACHA, Rule Engine, Domain, migraciones ni APIs.
- Recalcula elegibilidad vía `IAchPendingCycleEligibilityService.EvaluateAsync` **dentro** de la transacción relacional `Serializable` iniciada mediante la `ExecutionStrategy` de EF. No confía en evaluaciones previas.
- Compara ciclo/estado/lote asignados con los propuestos; busca lote por `IAchBatchRepository.FindForTransactionAsync` usando las claves actuales de `BatchResolver` (ciclo, empresa, identificación, descripción, fecha); **deniega** si atributos adicionales del lote destino no coinciden. Si falta lote, copia únicamente los datos identificadores/servicio sin clonar transacciones; `BatchSequenceNumber=0` siguiendo el precedente del optimizador CENIT.
- Guarda en una misma unidad transaccional: lote destino si es nuevo, `AchTransaction.AchCycleId`, `AchBatchId`, `AchTransactionStateEvent` con `FromState=ToState=Pending`, `ReasonCode=CYCLE_REASSIGNED`, llave `ACH-CR-001:{tx}:{from}:{to}`, motivo y correlación `from/toCycle`, `from/toBatch`, fecha valor y UTC. No altera `StateChangedAtUtc` (el estado financiero no cambió).
- Usa **sin duplicar reglas** `ITransactionPersister.UpdateBatchTotalsAsync` y `UpdateBatchServiceClassCodeAsync` para ambos lotes; persistencia dentro de la misma transacción relacional. Los `SaveChangesAsync` intermedios **no constituyen commit** y son necesarios para materializar ID de lote/recalcular agregados persistidos.
- No cambia fecha valor, cámara, identidad externa, traza, monto, cuentas, tipo, clasificación ni adendas. No transmite ni crea artefacto de salida.

## Límites y condiciones operativas de seguridad

1. Las pruebas InMemory omiten la apertura de transacción (proveedor no relacional), **únicamente** para cubrir lógica sin SQL real. La atomicidad real exige PostgreSQL/SQL Server y pruebas de integración.
2. `Serializable` protege dos ejecutores I02 y consultas bajo la transacción, pero **no demuestra seguridad ante un exportador que haya leído y retenido una instantánea anterior antes del traslado**. El flujo generador/exportador no fue cambiado en I02. I05 debe coordinar un protocolo de consistencia de asignación/membresía previo a transmisión; **no activar job de forma automática en producción antes de cerrar este riesgo**.
3. El comando rechaza un `DbContext` que ya tenga cambios pendientes (scope propio recomendado). En excepción relacional, limpia el tracking antes de reintentos de la estrategia EF para que los `SaveChangesAsync` previos sin commit no falseen la siguiente evaluación. No reemplaza una prueba de rollback real.
4. No hay índice único para `IdempotencyKey` del evento. I02 comprueba la misma clave dentro de la operación serializable y vuelve a evaluar estado, pero no hay prueba de estrés ni reconciliación en BD compartida; casos `CR-16`, `CR-17`, `CR-18` continúan con validación ejecutable pendiente.
5. El lote destino preexistente con metadatos incompatibles causa `SKIPPED/TARGET_BATCH_INCOMPATIBLE`; no se crea lote con la misma clave lógica para evadir la restricción.
6. Posibles abortos serializables, interbloqueos o fallos de proveedor se propagan y deben ser observados/reintentados por infraestructura autorizada. No se ocultan fallas como “traslado exitoso”.
7. Restricción de fecha valor y caso creación sábado->lunes siguen abiertos hasta I04.

## Pruebas y verificaciones

- Pruebas nuevas escritas: `AchPendingCycleReassignmentServiceTests.cs`: movimiento, segundo intento, reutilización, lote incompatible, exportado, estado no Pending, ciclo aún abierto, ID inválido (8 `[Fact]`). **NO EJECUTADAS**.
- Validadores scripts `validate-cycle-reassignment-static.py`, `validate-cycle-reassignment-i01-static.py`, `validate-cycle-reassignment-i02-static.py`; resultados en el informe externo JSON de I02.
- Sin nuevos paquetes NuGet, sin migraciones y sin modificar ficheros fuente regulatorios.
- **Pendiente en host con SDK .NET 10 y BD local:** `dotnet build ACHInterbank.sln -c Release`; `dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --filter AchPendingCycleReassignmentServiceTests`; tests SQL Server/PostgreSQL de rollback ante falla entre dos `SaveChanges`, concurrencia múltiple, compatibilidad de serialización; regresión exportación y respuesta.

## Evidencia para continuidad

- Código: `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchPendingCycleReassignmentService.cs`, contratos `src/Cfa.ACHInterbank.Application/ACH/...`.
- Funcionalidad **opt-in/no consumidor registrado aún**: Quartz se abordará en I03. Que la implementación tenga `[Scoped]` la hace disponible por DI de convención, pero sin invocación automática.
- Riesgo crítico `RISK-CR-001` **abierto** por el acoplamiento con el generador/exportador. Ningún test unitario del servicio sustituye la homologación del flujo externo.
- Siguiente incremento: **I03**, invocador acotado Quartz; mantener desactivado el despliegue automático hasta cerrar barrera de exportación I05.
