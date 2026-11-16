# HOTFIX FINAL AUD-E2E-01..04 — Informe técnico / 2026-11-15

## Fuente única y límites

Fuente única: `ACHInterbank_PROYECTO_COMPLETO_E2E01_E2E02_2026-11-15.zip` (SHA-256 `df172cf3d96079792ea19091cead336b7a76455dcacd7c6929c2a6b8c2989b55`). No se copian ni duplican documentos normativos. Se conservan todas las rutas y archivos originales; los cambios están delimitados a la creación transaccional, el dispatcher de Contrapartida, recuperación, Quartz, EF/migraciones y pruebas/documentación.

**Estado:** `IMPLEMENTADO_EN_FUENTE_PENDIENTE_COMPILACION_XUNIT_MULTIDB`. **NO-GO** para producción/homologación; **UAT NO EJECUTADA**. No se modifican los módulos funcionales designados E2E-03 a E2E-07; `AUD-E2E-03` significa *recuperación de Contrapartida con ciclo cerrado*, no ese módulo funcional.

## Defectos y resolución

| Hallazgo | Corrección en código real | Estado demostrable |
|---|---|---|
| **AUD-E2E-01 (P0)** `dbTransaction` fuera de alcance | `AchTransactionService`: transacción `await using` en el ámbito de todo intento; `CommitAsync`, `RollbackAsync(CancellationToken.None)` y `DisposeAsync` antes de consultar ganador único. Se conservan la huella, índice y resolución idempotente. | Corregido en fuente; compilación aún no demostrada |
| **AUD-E2E-02 (P1)** carrera entre recuperación y transporte | `ContrapartidaDispatchRecovery`: `ExecuteUpdateAsync` CAS compara simultáneamente ID, estado, token, **fase**, valor del **vencimiento**, número de intento y estado de conciliación. Si `Preparing` pasó a `InFlight` no se puede recuperar como `NOT_SENT`. | Corregido en fuente; competencia multidb pendiente |
| **AUD-E2E-03 (P1)** recuperación sobre ciclo cerrado | Tras aprobar calendario, ventana, estado `Open` y política que permite débito, `ContrapartidaDispatchJobService` reasigna **solo** `RetryPending` con código probado `E2E02_PRE_SEND_CRASH`, sin conciliación, al ciclo permitido de la misma cámara y fecha no anterior. CAS persiste ciclo de destino, origen y timestamp. El ciclo de la transacción monetaria original permanece inalterado. Quartz omite ciclos cerrados. `ProcessTransactionAsync(int)` puede resolver el ciclo abierto. | Corregido en fuente; ejecución de calendario/ciclo y SOAP pendiente |
| **AUD-E2E-04 (P1)** falta de evidencia SOAP previa | Se guardan por ítem (`ContrapartidaDispatchItem`) y, en lotes de una sola transacción, también en `ContrapartidaDispatchBatch`, antes de transporte y dentro de la **misma transacción DB** que cambia `Preparing -> InFlight`: `DispatchCorrelationId`, XML, SHA-256, `MappingSetId`, `MappingVersion`, `MappingSnapshotHash`, endpoint, modo y timestamp. Se conserva la auditoría posterior. La operación aborta si no se puede confirmar el intent. | Corregido en fuente; persistencia/race entre DB y SOAP pendiente de integración |

## Secuencia revisada y casos de caída

1. **Creación y persistencia**: verificación de clave originador/externo antes del resto; segundo lookup y escritura protegidos con `BeginTransaction` y restricción única; una colisión devuelve la transacción ganadora solo si fingerprint coincide; distinto contenido provoca conflicto.
2. **Cola**: `ContrapartidaDispatchItem` se crea en el mismo UOW/transaction; el envío usa claim CAS `Preparing` con token y expiración.
3. **Antes del SOAP**: construir XML/mapper; transacción DB atómica escribe `InFlight` e intención duradera con hash/correlación antes de habilitar transporte. Un fallo de persistencia aborta el camino de envío. En reintentos manuales con varios ítems el intent de cada SOAP es independiente; no se reutiliza el único campo de lote para todas las operaciones.
4. **Durante/después del SOAP**: se guardan respuesta y auditoría existente. Si se pierde el resultado Live, `RequiresReconciliation` bloquea reintento tanto automático como manual.
5. **Recuperación / reinicio**: `Preparing` vencida y sin evidencia de envío → `RetryPending`; `InFlight` vencida → falla con conciliación. El inspector es acotado a 100 por ejecución. Si se cerró el ciclo, solo la recuperación `NOT_SENT` puede pasar por CAS a un ciclo abierto y permitido. Si no hay un ciclo permitido, la cola espera uno; no fuerza un despacho.
6. **Límite inevitable de garantía**: un crash justo después de guardar `InFlight` y antes de abrir red quedará conservadoramente para conciliación. No se puede garantizar exactamente una aplicación en banco sin confirmación externa o idempotencia del core; se evita **retransmitir automáticamente** débitos inciertos.

## Pruebas realmente ejecutadas

- `python scripts/test/aud_e2e_final_static_and_sqlite_check.py`: 31/31 pruebas de fuente y un modelo CAS equivalente SQLite; salida 0. Se añadió un control por reinicio Quartz y calendario a los escenarios xUnit, no ejecutados.
- `python scripts/test/e2e01_e2e02_source_static_check.py`: 35/35 pruebas de fuente/modelo SQLite con punto de intención actualizado; salida 0.
- Nueve comprobadores estáticos existentes de regresión de ACH Colombia / CENIT: **9/9**, salida 0. Logs por comando en `evidencias/REGRESSION_01..09.txt`.
- Compilación y tests xUnit: **NO EJECUTADOS** por `dotnet` ausente (comandos devuelven 127). Se añadieron **8** casos nuevos xUnit (race fase, persistencia/reintento de intención, expiración, recuperación y ciclo cerrado, calendario, resultado incierto, reinicio Quartz). No confundir código de prueba escrito con ejecutado.
- PostgreSQL/SQL Server reales, migración aplicada y pruebas de colisiones interproceso: **NO EJECUTADOS** por falta de bases. No se usó SOAP Live; UAT reservada para el final.

## Migraciones y consideraciones de despliegue

- Nuevas migraciones `20261115183000_E2EFinalCycleAndSoapIntent.cs` para **PostgreSQL y SQL Server**, con snapshots actualizados, son posteriores a las migraciones previas `20261115170000` de unicidad/lease. Cero borrados masivos y cero cambios de restricciones de idempotencia.
- Antes de desplegar, respaldar datos y ejecutar consulta **read-only** de duplicados históricos e identificar leases activos; si se detectan duplicados, conciliar fuera del sistema antes de migrar. Probar Up y Down en entornos desechables con ambos motores, verificar tamaños/retención del XML y secreto/PII de auditoría.
- Después de migrar, compilar .NET 10, ejecutar xUnit total y focal, pruebas de carrera multiinstancia, cron/calendar CENIT/ACH Colombia, reanudación después de reinicio, falldown pre/durante/post SOAP, huellas y 0 despachos duplicados al mock core. Verificar fecha efectiva/normativa cuando el reintento pasa a una fecha operativa posterior.
- No hacer UAT ahora. Sin pruebas reales, mantener **NO-GO**.

## Comandos de verificación pendientes (no ejecutar contra producción)

```bash
dotnet restore ACHInterbank.sln
dotnet build ACHInterbank.sln --configuration Release --no-restore
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj --configuration Release --filter 'FullyQualifiedName~AUD_E2E|FullyQualifiedName~E2E01|FullyQualifiedName~E2E02|FullyQualifiedName~ContrapartidaDispatchJobServiceTests|FullyQualifiedName~AchContrapartidasByCycleHandlerTests'
dotnet test ACHInterbank.sln --configuration Release
```

La consulta de preflight histórica permanece en el informe previo `docs/hotfix-e2e01-e2e02-2026-11-15/INFORME_E2E.md` (sin duplicar normas).
