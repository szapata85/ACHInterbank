# F03 — Encolamiento y transición CENIT entre ciclos

**Fecha:** 2026-11-02. **Base:** ZIP F02 verificado SHA-256 `361d8d4a8e3d14313283a50155cfbc9633c37f5bd1f69e5755f09e68720e31a4`. **Hallazgo único:** HC-CENIT-001 (P1), 2025 DSP-152 Anexo 2 §3.3. **Resultado:** implementado con revisión estática, pendiente de ejecución dinámica y certificación; ningún hallazgo cerrado.

## Causa raíz reconfirmada y ruta productiva

`CenitCycleQueueService.EnqueueAsync` guardaba `OriginalAchCycleId` y `TargetAchCycleId` sin mover la transacción. `NachaDataLoader.LoadByCycleAsync` selecciona solo `AchTransaction.AchCycleId` y batches de la fecha destino. La fase F01 dejó `CenitCycleExecutionService.StartExecutionAsync` esperando resultados oficiales, sin consumir colas para evitar falsos éxitos. Por tanto, una cola pendiente no ingresaba al universo que genera registros NACHA-M para el ciclo destino.

Ruta revisada: `AchTransactionService` / `AchBulkTransactionService`→`BatchResolver` (decisión fuera de ventana)→`CenitCycleQueueService.EnqueueAsync`→`NachaExportController`/`NachaSecurityOperationService`→`NachaFileBuilder.BuildNachaFilesByCycleAsync`→`NachaFileGenerationCoordinator`→**`ICenitCycleQueueService.IncorporateForCycleAsync`**→`NachaDataLoader.LoadByCycleAsync`→renderizado/partición NACHA-M. Quartz/CenitCycleExecution sigue esperando resultado oficial; no hay neteo simulado ni CUD.

## Corrección mínima y seguridad

1. En el camino exclusivo CENIT del generador, incorporar la cola **antes** de recargar las transacciones y seleccionar lotes. El generador falla sin producir un archivo parcial si existe un error de incorporación. No se toca la ruta ACH Colombia.
2. `CenitCycleQueueService` utiliza la transacción relacional `Serializable` y estrategia EF Core para confirmar conjuntamente el ciclo real de la transacción, lote de destino existente o nuevo, evento `CENIT_QUEUE_ATTACHED`, totales de origen/destino, y `Status=Consumed`/hora de consumo de cola. La modificación del estado de la transferencia y de su fecha de cambio está prohibida.
3. Si la transacción ya está asignada al mismo ciclo destino (encolada por recepción antes de apertura), se confirma incorporación sin crear lote adicional. El reintento después del éxito devuelve 0 entradas pendientes; un segundo `EnqueueAsync` de la misma operación no crea nueva cola consumida.
4. Prevenciones: cámara distinta, origen inconsistido, transacción no `Pending`, evidencia de despacho/membresía de exportación, destino con archivo exportado, ciclo cancelado, lote incompatible y fecha valor diferente. Ninguna de estas condiciones cambia financieramente la transacción.

**Limitación importante:** la transferencia cuya `EffectiveEntryDate` corresponde a un día distinto al día operacional del ciclo destino queda `Queued`, se bloquea su exportación y se exige revisión de fecha valor en F04 / confirmación CENIT; no se cambia automáticamente la fecha de valor. No es un cierre integral de todos los escenarios interdiarios. La exclusión de estados definitivos por simulación F01 permanece.

**Concurrencia pendiente:** verificar en PostgreSQL/SQL Server dos incorporaciones y exportación concurrentes, especialmente cuando el exportador recibe una instantánea del lote. La transacción `Serializable` protege esta operación, pero la ruta de exportación no aplica un protocolo de bloqueo global; certificarla antes de habilitar producción. No se añadieron migraciones, entidades, tablas ni servicios bancarios.

## Evidencia de pruebas

- Se añadió `tests/Cfa.ACHInterbank.Tests/CenitCycleQueueIncorporationF03Tests.cs` con **8 `[Fact]`**: incorporación y lote, idempotencia, export previo, evidencia de despacho, error de persistencia/rollback, fecha valor interdiaria, cámara ACH distinta, rollback con múltiples ítems, y cola del mismo ciclo antes de ventana (la primera prueba incluye repetición). Los test fixtures son sintéticos, sin certificación.
- Ejecutado realmente: `python scripts/test/cenit_f03_static_check.py` **17/17 PASS**; reejecutados F01 **12/12 PASS** y F02 **13/13 PASS**. Se compararon SHA-256 y CRC de los archivos originales, incluidos ACH Colombia.
- **NO ejecutadas:** compilación, `dotnet test`, SQLite xUnit, base SQL Server/PostgreSQL, Quartz, gateways CENIT y pruebas de integración. El SDK .NET no está disponible aquí. Por ello HC-CENIT-001 = **IMPLEMENTADO — PENDIENTE DE VERIFICACIÓN**, no cerrado.
- Referencias: `../auditoria-estatica-cenit/HALLAZGOS_CONSOLIDADOS_CENIT.md` HC-001, `MATRIZ_PRUEBAS_POSTERIORES_CENIT.md` PT-01/PT-02/PT-03/PT-20/PT-21, `PLAN_CORRECCIONES_MINIMAS_CENIT.md`.

## Pruebas externas requeridas antes del cierre

`dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj --filter FullyQualifiedName~CenitCycleQueueIncorporationF03Tests` y suite completa; después usar operación fuera de ventana con traza y lote reales, target C2..C5, reintento y caída entre creación lote/commit, exportación simultánea, siguiente día hábil, transacción previamente enviada, cadena de eventos y estado oficial posterior. Verificar exactamente 1 miembro del archivo final, cola `Consumed` solo con incorporación duradera, totales idénticos, sin doble operación en archivo, y no regresión ACH Colombia. Requiere acceso DB/operador y logs con hashes.

**Reversión:** aplicar `F03_CAMBIOS.patch` al revés en los 4 archivos productivos de F02 (`patch -R -p1`), suprimir test y script nuevos y documentos F03; o recuperar íntegro ZIP F02 y confirmar hash. 
