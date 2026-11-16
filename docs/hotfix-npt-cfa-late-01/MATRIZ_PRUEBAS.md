# Matriz de pruebas NPT-CFA-LATE-01

| ID | Escenario | Mecanismo | Estado real |
|---|---|---|---|
| CFA-L01 | Intento persistido + índice único, respuesta tardía | xUnit `PersistedAttempt_RejectsDuplicateExecution_ButLateResultUsesExistingEventTable` | Escrito, NO ejecutado (.NET ausente) |
| CFA-L02 | XML y código históricos no sobrescritos | xUnit `EarlierSoapEvidence_IsNeverOverwrittenByAConflictingLateResponse` | Escrito, NO ejecutado |
| CFA-L03 | Replay idéntico con timestamps distintos | xUnit `RepeatedIdenticalLateResult_IsIdempotent_AndDoesNotChangeReconciliation` | Escrito, NO ejecutado |
| CFA-L04 | Tres workers concurrentes misma respuesta | xUnit `ConcurrentLateWorkers_ArchiveOnce_WithOneOriginalAttempt` | Escrito, NO ejecutado |
| CFA-L05 | Dos respuestas diferentes de un intento | xUnit `DistinctLateBankOutcomes_OnSameAttempt_RetainBothWithoutOverwriting` | Escrito, NO ejecutado |
| CFA-L06 | Fallo de BD y rollback | xUnit `ArchiveInsertFailure_RollsBackEvidence_LeavesAttemptAndQuarantineIntact` | Escrito, NO ejecutado |
| CFA-L07 | Invariantes fuente y modelo SQLite independiente | `python scripts/test/npt_cfa_late_static_sqlite_check.py` | 15/15 PASS, no ejecuta C# |
| CFA-L08 | Regresiones focales NPT | `python scripts/test/npt_focal_sqlite_source_guard.py` | 26/26 PASS, modelo/fuente |
| CFA-L09 | Regresiones E2E/H01-H02/RES-H02/ACH | cinco scripts históricos | 5/5 exit 0, solo fuente/modelo |
| CFA-L10 | NPT histórico, cancelación Axon | `python scripts/test/npt_static_guard.py` | 41/42, FAIL heredado (patrón anterior Axon) |
| CFA-L11 | Compilación .NET 10 y xUnit | `dotnet restore/build/test` | No ejecutado: exit 127 SDK no instalado |
| CFA-L12 | SQL Server / PostgreSQL con transacciones y PK reales | `dotnet test` filtrando test de integración y pipelines de BD | Pendiente infraestructura |
| CFA-L13 | Fallos de transporte/cancelación y contadores físicos SOAP | Simulador remoto instrumentado | Pendiente, no ejecutar Live/UAT |
