# E2E-01/E2E-02 — Relación de archivos modificados vs ZIP original

Comparación byte a byte mediante CRC-32 con el ZIP de entrada. Los 5.645 archivos se preservan, con cambios focales y documentación de seguimiento. **No hay normativa duplicada.**

- **existing:** 5645
- **changed_originals:** 21
- **new_files:** 23
- **unchanged_originals:** 5624
- **missing_originals:** 0

## Archivos originales modificados

- `ACHInterbank_ajustado/MEMORY_STATE.md`
- `ACHInterbank_ajustado/src/Cfa.ACHInterbank.Api/Controllers/TransactionsController.cs`
- `ACHInterbank_ajustado/src/Cfa.ACHInterbank.Domain/Models/ACH/AchTransaction.cs`
- `ACHInterbank_ajustado/src/Cfa.ACHInterbank.Domain/Models/ACH/ContrapartidaDispatchModels.cs`
- `ACHInterbank_ajustado/src/Cfa.ACHInterbank.Persistence.Migrations.SqlServer/DataBase/Migrations/SqlServer/AchDbContextModelSnapshot.cs`
- `ACHInterbank_ajustado/src/Cfa.ACHInterbank.Persistence/ACH/Quartz/Jobs/Implementation/AchContrapartidasByCycleHandler.cs`
- `ACHInterbank_ajustado/src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchTransactionService.cs`
- `ACHInterbank_ajustado/src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/ContrapartidaDispatchJobService.cs`
- `ACHInterbank_ajustado/src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/TransactionPersister.cs`
- `ACHInterbank_ajustado/src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/TransactionPolicyService.cs`
- `ACHInterbank_ajustado/src/Cfa.ACHInterbank.Persistence/Configuration/AchTransactionConfiguration.cs`
- `ACHInterbank_ajustado/src/Cfa.ACHInterbank.Persistence/Configuration/ContrapartidaDispatchItemConfiguration.cs`
- `ACHInterbank_ajustado/src/Cfa.ACHInterbank.Persistence/DataBase/AchDbContext.cs`
- `ACHInterbank_ajustado/src/Cfa.ACHInterbank.Persistence/DataBase/Migrations/Postgres/AchDbContextModelSnapshot.cs`
- `ACHInterbank_ajustado/tests/Cfa.ACHInterbank.Tests/AchTransactionNachaTests.cs`
- `ACHInterbank_ajustado/tests/Cfa.ACHInterbank.Tests/ContrapartidaDispatchJobServiceTests.cs`
- `ACHInterbank_ajustado/tests/Cfa.ACHInterbank.Tests/TransactionPolicyServiceTests.cs`
- `LEER_PRIMERO.md`
- `PLAN_IMPLEMENTACION_13_CASOS/MEMORY_STATE.json`
- `PLAN_IMPLEMENTACION_13_CASOS/MEMORY_STATE.md`
- `PLAN_IMPLEMENTACION_13_CASOS/PLAN_IMPLEMENTACION_13_CASOS.md`

## Archivos nuevos (excluido este manifiesto autodescriptivo)

- `ACHInterbank_ajustado/docs/hotfix-e2e01-e2e02-2026-11-15/ARCHIVOS_MODIFICADOS.md`
- `ACHInterbank_ajustado/docs/hotfix-e2e01-e2e02-2026-11-15/DESPLIEGUE_SUPERVISION.md`
- `ACHInterbank_ajustado/docs/hotfix-e2e01-e2e02-2026-11-15/INFORME_E2E.md`
- `ACHInterbank_ajustado/docs/hotfix-e2e01-e2e02-2026-11-15/evidencias/DOTNET_ATTEMPTS_NOT_RUN.txt`
- `ACHInterbank_ajustado/docs/hotfix-e2e01-e2e02-2026-11-15/evidencias/E2E01_E2E02_STATIC_AND_SQLITE_MODEL.txt`
- `ACHInterbank_ajustado/docs/hotfix-e2e01-e2e02-2026-11-15/evidencias/ENVIRONMENT.txt`
- `ACHInterbank_ajustado/docs/hotfix-e2e01-e2e02-2026-11-15/evidencias/MANIFEST_SOURCE_DIFF.json`
- `ACHInterbank_ajustado/docs/hotfix-e2e01-e2e02-2026-11-15/evidencias/REGRESSION_01.txt`
- `ACHInterbank_ajustado/docs/hotfix-e2e01-e2e02-2026-11-15/evidencias/REGRESSION_02.txt`
- `ACHInterbank_ajustado/docs/hotfix-e2e01-e2e02-2026-11-15/evidencias/REGRESSION_03.txt`
- `ACHInterbank_ajustado/docs/hotfix-e2e01-e2e02-2026-11-15/evidencias/REGRESSION_04.txt`
- `ACHInterbank_ajustado/docs/hotfix-e2e01-e2e02-2026-11-15/evidencias/REGRESSION_05.txt`
- `ACHInterbank_ajustado/docs/hotfix-e2e01-e2e02-2026-11-15/evidencias/REGRESSION_06.txt`
- `ACHInterbank_ajustado/docs/hotfix-e2e01-e2e02-2026-11-15/evidencias/REGRESSION_07.txt`
- `ACHInterbank_ajustado/docs/hotfix-e2e01-e2e02-2026-11-15/evidencias/REGRESSION_08.txt`
- `ACHInterbank_ajustado/docs/hotfix-e2e01-e2e02-2026-11-15/evidencias/REGRESSION_09.txt`
- `ACHInterbank_ajustado/docs/hotfix-e2e01-e2e02-2026-11-15/evidencias/REGRESSION_SUMMARY.txt`
- `ACHInterbank_ajustado/scripts/test/e2e01_e2e02_source_static_check.py`
- `ACHInterbank_ajustado/src/Cfa.ACHInterbank.Persistence.Migrations.SqlServer/DataBase/Migrations/SqlServer/20261115170000_E2E01IdempotencyE2E02ContrapartidaLease.cs`
- `ACHInterbank_ajustado/src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/ContrapartidaDispatchRecovery.cs`
- `ACHInterbank_ajustado/src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/TransactionIdempotency.cs`
- `ACHInterbank_ajustado/src/Cfa.ACHInterbank.Persistence/DataBase/Migrations/Postgres/20261115170000_E2E01IdempotencyE2E02ContrapartidaLease.cs`
- `EVIDENCIAS/E2E01_E2E02_RESULTADOS.md`

**Pruebas pendientes:** SDK .NET 10/build/xUnit; SQL Server y PostgreSQL reales; duplicados históricos; migraciones físicas; estrés concurrente y SOAP; reinicio del scheduler. UAT no ejecutada. NO-GO.
