# Informe de implementación real — Hotfix focal H01 y H02

**Fecha:** 2026-11-15. **ZIP base exclusivo:** `ACHInterbank_PROYECTO_COMPLETO_AUD_E2E01_E2E04_FINAL_2026-11-15.zip` (`5bca98ea7f78402b92b7104cbf0d1522135a5529fdb466fd50775374eab1d85c`). **Producción:** NO-GO.

## H01 — P1 intentos agotados

**Antes:** `ContrapartidaDispatchRecovery.RecoverStaleAsync` pasaba `AttemptCount=4` a 5 al recuperar una operación `Preparing`, mientras la selección `AttemptCount < 5` rechazaba el nuevo `RetryPending`. La recuperación se anotaba como si fuera un intento de SOAP (`AttemptNumber=5`).

**Después:** la recuperación no incrementa `AttemptCount` ni altera `LastAttemptAtUtc` (reserved para ejecución), pero preserva auditoría inmutable en `ContrapartidaDispatchAttempts` con `AttemptNumber` negativo único por ítem para distinguir acciones internas de envíos. El máximo compartido es cinco. `Preparing` con contador menor a cinco puede reintentar en ciclo permitido; `InFlight`, incertidumbre o límite alcanzado nunca se retransmiten. Se agregó barrido atómico, independientemente del ciclo, para registros **históricos** `RetryPending` agotados: `ContrapartidaReportFailed`, `RequiresReconciliation=true`, `NextAttemptAtUtc=null`, código `H01_RETRY_LIMIT_RECONCILIATION` y evento de auditoría nuevo; el contador histórico no se inventa ni resetea. Cada escaneo es idempotente mediante CAS. La auditoría negativa no sustituye los números positivos de ejecuciones reales.

## H02 — P2 reintento manual desde ciclo cerrado

**Antes:** `RetryBatchAsync` cargaba directamente `sourceBatch.AchCycleId`, fallando en ciclo cerrado, ventana vencida o calendario no operativo, sin seleccionar destino válido.

**Después:** `FindActiveRetryCycleAsync` resuelve un ciclo en estado abierto, con configuración vigente, cámara activa, calendario operativo y política de débitos; el destino no puede tener fecha anterior a la del lote origen ni a la del ciclo actual del ítem. No existe fallback al ciclo cerrado. Sin destino seguro devuelve un resultado diferido y **no crea batch ni llama SOAP**. Antes de cada ítem vuelve a consultar ciclo/ventana/calendario/política. Reasignación de `ContrapartidaDispatchItem.AchCycleId` y claim (`Retrying/Preparing`) se escriben en un único `ExecuteUpdateAsync` CAS por ítem con comprobación de token/fase/contador/estado/conciliación. Solo el reclamante puede persistir intención `InFlight` y enviar. Se registra la reasignación y el nuevo batch apunta al ciclo destino; no cambia el ciclo histórico de `AchTransaction`/`AchBatch`. Respuestas o estados inciertos siguen bloqueados para reenvío manual/automático.

## Ejecución real y límites

- **Ejecutado:** 39/39 controles focales de fuente/modelo SQLite; 31/31 verificador AUD-E2E anterior; 35/35 verificador E2E previo; 9/9 scripts estáticos ACH/CENIT. Todos los 12 comandos retornaron código 0. Registros completos en `evidencias/COMMAND_EXIT_CODES.json`.
- **Añadido (NO ejecutado):** escenarios xUnit de límites 0/4/5, agotamiento heredado, reinicios, envíos quinto intento, recuperación concurrente (y tests previos), ciclo cerrado, calendario bloqueado, resultado SOAP incierto, y dos workers manuales disputando el claim.
- **No ejecutado:** `dotnet restore/build/test` (SDK .NET ausente), PostgreSQL/SQL Server reales, SOAP real, UAT. Las comprobaciones SQLite ejecutan **un modelo SQL equivalente**, no el código EF Core; las comprobaciones estáticas **no prueban compilación**.
- **Migraciones:** ninguna nueva. `ContrapartidaDispatchItems` y `ContrapartidaDispatchAttempts` ya contienen los campos/índice único utilizados en ambos motores; no se modificaron snapshots ni normativas.
- **Riesgos pendientes:** verificación de comportamiento y compilación real, simultaneidad con dos procesos SQL Server/PG, prueba de fallo transaccional durante intent SOAP, revisión de registros legados requeridos para conciliación.

**Estado H01:** implementación fuente terminada / aceptación técnica pendiente. **Estado H02:** implementación fuente terminada / aceptación técnica pendiente. No se afirma homologación, UAT aprobada ni ausencia demostrada de regresiones en runtime.
