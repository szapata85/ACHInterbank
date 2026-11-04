# I05 — Guardias de exportación NACHA y regresiones CENIT

**Iniciativa:** ACH-CR-001 · **corte:** 2026-10-25 · **estado:** IMPLEMENTADO ESTÁTICAMENTE / RUNTIME NO EJECUTADO.

## Alcance y hallazgo principal

La generación por ciclo toma una instantánea de `NachaDataLoader.LoadByCycleAsync` antes de persistir `AchFileExportTransaction`. Existía una ventana de carrera con I02 durante el renderizado/cifrado. Además, `NachaSecurityOperationService.GenerateNachaInternalAsync` hacía descargable una salida construida sin pasar por `IAchFileExportAuditService`. Las rutas `NachaExportController` y `AchColombiaManagedMftOutboundWorkflow` ya auditaban explícitamente su membresía y no se duplicó ese proceso.

## Cambios as-built

1. **`AchFileExportAuditService`**, solo `ExportKind=NACHA` y proveedor relacional: validación de ciclo, estado exportable y membresía exacta + inserción del archivo/membresías en una misma transacción **`Serializable`** con `CreateExecutionStrategy`, conservando el `SemaphoreSlim` preexistente para idempotencia local. Ante cambio de ciclo previo, **se lanza excepción antes de entregar**; el caller debe abortar. En caso de transacción EF ambientada para NACHA, la operación se niega con `NACHA_EXPORT_AMBIENT_TRANSACTION_UNSUPPORTED`, evitando aislamiento desconocido. Otros `ExportKind` y proveedor InMemory conservan flujo previo. No se creó lock distribuido, migración ni tabla.
2. **`NachaSecurityOperationService`**: nuevo uso de `IAchFileExportAuditService` (interfaz de Application ya existente). Registra cada partición NACHA con IDs concretos, nombre, hash y conteo **antes** de `IOperationArtifactStore.SaveAsync` y de marcar `DownloadAvailable`. Un fallo de admisión no publica el archivo; una partición anterior ya registrada puede quedar sin publicación y exige seguimiento, sin reenviar automáticamente.
3. **`AchPendingCycleEligibilityService`**: si hay cualquier exportación NACHA o evidencia `PUBLISH` asociada al ciclo **fuente**, detiene toda reasignación desde ese ciclo con `SOURCE_CYCLE_EXPORT_EXISTS`, aun si la membresía histórica parece completa. Es conservador y puede retener pendientes legítimos: requiere revisión operativa si sucede.
4. **CENIT**: si hay ejecución `CenitCycleExecution` del ciclo o cola/decisión de liquidez vinculada a ciclo fuente, niega la reasignación transversal con `CENIT_CYCLE_PROCESSING_EVIDENCE`. No se alteran `LiquidityOptimizationService`, `CenitCycleExecutionService` ni la cola regulatoria.
5. Se conservan las barreras I01 (`EXPORT_EVIDENCE_PRESENT`, `PROCESSING_EVIDENCE_PRESENT`), revalidación de I02 con `Serializable`, y doble inhabilitación I03 Quartz (`Disabled` + `EnableExecution=false`).

## Invariantes y protocolo de competencia (teórico, PENDIENTE DE PRUEBA RELACIONAL)

- **I02 gana antes de la admisión NACHA:** la exportación rechaza la membresía si el ID ya no pertenece al ciclo o perdió estado exportable; no debe ofrecerse/hacerse handoff del archivo construido.
- **Admisión NACHA gana antes de I02:** I02 observa evidencia de exportación de ciclo/transacción y se abstiene. Dos transacciones `Serializable` que compiten deben serializarse o abortar al menos una; la respuesta concreta depende del proveedor relacional.
- **Archivos parciales CENIT:** guardar evidencias de las particiones previas no autoriza a publicar/reintentar las siguientes sin diagnóstico. No se afirma atomicidad entre múltiples artefactos ni entre DB y almacenamiento externo.
- **Operaciones posteriores al registro** (transporte a terceros, retorno, compensación y aceptación cámara) continúan con los contratos existentes. I05 no cambia estados ni crea reintentos de red.

## Regresiones escritas, sin ejecutar

- `AchPendingCycleEligibilityServiceTests`: archivo NACHA completo de *otra* transacción en ciclo fuente y ciclo CENIT con ejecución existente, ambos denegados.
- `AchFileExportAuditServiceTests`: ID trasladado antes de auditar se rechaza sin registrar exportación y el mismo nombre no puede sustituir membresías.
- Validación estática I05 verifica transacción `Serializable` en auditoría y el orden de auditar antes de hacer artefacto descargable en la ruta de seguridad; protege la conservación de I01–I04 y exige I03 inactivo.
- **NO EJECUTADO:** compilación .NET, xUnit, SQL Server / PostgreSQL con dos procesos, idempotencia de eventos contra BD real, Quartz, transferencias MFT ni homologación externa.

## Riesgos abiertos de liberación (no cerrar por inspección)

- `RISK-CR-001`: mitigado en rutas auditadas por código, **NO CERRADO / PENDIENTE RUNTIME**: prueba de carreras reales (`Serializable`, índices, deadlocks/serialización, retry, persistencia de evidencia). Revisar rutas futuras/directas que publiquen sin `IAchFileExportAuditService`.
- Posible bloqueo conservador de pendings por archivo de otra transacción en mismo ciclo. Preferir revisión antes que duplicidad.
- Un archivo NACHA ya construido no es prueba de presentación a la cámara; la ausencia de respuesta externa tampoco autoriza reenviarlo.
- Ruta multiarchivo no tiene transacción atómica global DB + artifacts; si falla la segunda partición, no liberar el conjunto.
- El scheduler se mantiene deshabilitado; I06 es cierre de evidencia estática, no go-live.

## Archivos funcionales modificados

- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchPendingCycleEligibilityService.cs`
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchFileExportAuditService.cs`
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaSecurity/NachaSecurityOperationService.cs`
- `tests/Cfa.ACHInterbank.Tests/AchPendingCycleEligibilityServiceTests.cs`
- `tests/Cfa.ACHInterbank.Tests/AchFileExportAuditServiceTests.cs`
- `scripts/validate-cycle-reassignment-i05-static.py`

**Siguiente incremento:** I06 — consolidación/verificación estática final y plan de homologación. **Incrementos restantes después de I05: 1.**
