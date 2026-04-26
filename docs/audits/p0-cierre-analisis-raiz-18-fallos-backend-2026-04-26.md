# Cierre P0 — Análisis raíz de 18 fallos backend (.NET) para readiness de producción

**Fecha:** 2026-04-26  
**Fuente de verdad del fallo:** `docs/audits/evidence/dotnet-test-release.txt`  
**Resultado base:** 394 total / 376 passed / 18 failed.

---

## 1) Lista exacta de los 18 tests fallidos

(Extraída de `docs/audits/evidence/dotnet-test-release.txt` y consolidada en `docs/audits/evidence/p0-failed-tests-list-2026-04-26.txt`)

1. `ClearingHouseCycleConfigServiceTests.GetCurrentByClearingHouseAsync_ReturnsLatestVersionPerCycleForDate`
2. `AchBulkTransactionServiceTests.RegisterBulkAsync_ReturnsTotalSuccess_WhenAllItemsAreValid`
3. `AchBulkTransactionServiceTests.RegisterBulkAsync_ReturnsTotalFailure_WhenAllItemsFailValidation`
4. `AchBulkTransactionServiceTests.RegisterBulkAsync_ReturnsPartialSuccess_WhenOneItemFailsValidation`
5. `AchBulkTransactionServiceTests.RegisterBulkAsync_FailsItems_WhenDuplicateReferencesExistInRequestOrPersistence`
6. `AchBulkTransactionServiceTests.RegisterBulkAsync_Throws_WhenBatchExceedsMaxItems`
7. `PrenotificationHandlerTests.HandleAsync_CreatesThirdParty_WithCustomerNavigation_WhenCustomerIsTrackedWithTemporaryKey`
8. `ClearingHouseCycleConfigSeederTests.SeedAsync_CreatesUsefulScenariosForAchAndCenit`
9. `CenitOperationalGovernanceTests.CenitCalendarPolicy_Throws_WhenCycleCountIsNotFive`
10. `ReportServicesDataQualityTests.ReconciliationReport_CalculatesTotalsDiffsAndInconsistencies`
11. `ReportServicesDataQualityTests.ReturnsReport_FiltersByReturnCodes_AndResolvesCausalAndOriginalTransaction`
12. `BulkTransactionScenarioSeederTests.SeedAsync_IsIdempotent`
13. `BulkTransactionScenarioSeederTests.SeedAsync_CreatesBulkScenarioTransactions_InDevelopment`
14. `ContrapartidaDispatchPersistenceServiceTests.EnsurePendingDispatchAsync_UsesBatchNavigation_WhenTransactionBatchIdIsTemporary`
15. `TransactionPolicyServiceTests.PreviewAsync_RejectsDuplicateTransactionsWithinCycle`
16. `TransactionPolicyServiceTests.PreviewAsync_RejectsWhenOutsideCycleWindow`
17. `TransactionPolicyServiceTests.PreviewAsync_DetectsDuplicateByTransactionExternalId_WithoutDependingOnLegacyReference`
18. `TransactionPolicyServiceTests.ValidateRequest_RejectsInvalidNaturalPersonIdentity`

---

## 2) Agrupación por causa raíz

## CR-1 — Limitaciones de traducción LINQ/SQLite (TimeSpan + GroupBy/OrderBy)
**Evidencia firma:** `could not be translated`, `SQLite does not support expressions of type 'TimeSpan' in ORDER BY`.  
**Afecta:** #1, #9.

Diagnóstico:
- En `ClearingHouseCycleConfigService.GetCurrentByClearingHouseAsync` se hace `GroupBy(...).Select(First...).OrderBy(CutoffTime)` en query traducida por EF; SQLite no traduce consistentemente este patrón.
- En `CenitOperatingCalendarPolicy.ValidateCycleConsistencyAsync` se ordena por `StartTime` (TimeSpan); SQLite lanza `NotSupportedException`.

Clasificación:
- **Limitación SQLite** + **deuda preexistente de cross-provider compatibility**.

---

## CR-2 — Colisión de datos de fixture/seed (UNIQUE en `CompanyEntryDescription.Id`)
**Evidencia firma:** `SQLite Error 19: 'UNIQUE constraint failed: CompanyEntryDescription.Id'`.  
**Afecta:** #2, #3, #4, #5, #6, #7, #14.

Diagnóstico:
- Múltiples tests insertan explícitamente `CompanyEntryDescriptionCatalog { Id = 1, ... }` en contextos donde ya existe seed o prerequisito previo con el mismo Id (o se reusa una inicialización que lo deja cargado).
- La falla ocurre antes del assert de negocio; por tanto invalida el valor de estas pruebas como señal funcional.

Clasificación:
- **Fixture incompleto** + **deuda preexistente de aislamiento de pruebas**.

---

## CR-3 — Prerequisitos relacionales incompletos / orden de seed inconsistente (FOREIGN KEY)
**Evidencia firma:** `SQLite Error 19: 'FOREIGN KEY constraint failed'`.  
**Afecta:** #8, #12, #13, #15, #16, #17, #18 (y parcialmente #14/#7 por contaminación cruzada).

Diagnóstico:
- Tests de seed/política/transacción crean entidades ACH con dependencias fuertes (batch/cycle/financial institutions/catalogs) y algunos caminos no garantizan el orden o la completitud de relaciones requeridas.
- En varios casos el fallo se dispara en `SeedCatalog/SeedPrerequisites` antes de ejecutar la lógica principal.

Clasificación:
- **Fixture incompleto** (principal), con posible **bug de test setup**.

---

## CR-4 — Desalineación de expectativas en reportes (asserts no coinciden con resultado real)
**Evidencia firma:** `Assert.Equal() Failure: Values differ` y `Strings differ`.  
**Afecta:** #10, #11.

Diagnóstico:
- Diferencias entre dataset sembrado y agregaciones/devuelve actual de servicios de reportes.
- Puede ser: (a) expectativa obsoleta de test; (b) regresión funcional real; (c) contaminación de catálogos de causal en fixture.

Clasificación:
- **Expectativa obsoleta o regresión funcional (pendiente decisión funcional)**.

---

## 3) Matriz accionable test por test (P0)

| # | Test fallido | Causa raíz | Tipo | Impacto funcional | Severidad | Corrección recomendada (sin implementar aún) | Riesgo corrección | Tests de cierre | ¿Bloquea producción? |
|---|---|---|---|---|---|---|---|---|---|
| 1 | ClearingHouseCycleConfigServiceTests.GetCurrent... | CR-1 | Limitación SQLite / compatibilidad query | Media (gobernanza de ciclos) | Alta | Reescribir query en 2 fases (SQL-safe + orden cliente) preservando regla; validar SQLite y PostgreSQL | Medio | test actual + suite de ciclo config | Sí (suite roja) |
| 2 | AchBulkTransactionServiceTests.TotalSuccess | CR-2 | Fixture incompleto | Media (bulk onboarding) | Alta | Aislar catalog seed con ids dinámicos/lookup por término y no id fijo | Bajo | 5 tests de AchBulkTransactionService | Sí |
| 3 | AchBulkTransactionServiceTests.TotalFailure | CR-2 | Fixture incompleto | Media | Alta | Igual #2 | Bajo | idem | Sí |
| 4 | AchBulkTransactionServiceTests.PartialSuccess | CR-2 | Fixture incompleto | Media | Alta | Igual #2 | Bajo | idem | Sí |
| 5 | AchBulkTransactionServiceTests.DuplicateReferences | CR-2 | Fixture incompleto | Media | Alta | Igual #2 | Bajo | idem | Sí |
| 6 | AchBulkTransactionServiceTests.BatchExceedsMax | CR-2 | Fixture incompleto | Media | Alta | Igual #2 | Bajo | idem | Sí |
| 7 | PrenotificationHandlerTests.HandleAsync... | CR-2/CR-3 | Fixture + FK | Media (alta trazabilidad cliente/tercero) | Alta | Completar prerequisitos relacionales y eliminar colisión catálogo | Medio-bajo | PrenotificationHandlerTests | Sí |
| 8 | ClearingHouseCycleConfigSeederTests.SeedAsync... | CR-3 | Fixture/FK | Baja-media (seed de arranque) | Alta | Asegurar orden de seed y prerequisitos de cámara/catálogos antes de cycle config | Medio | ClearingHouseCycleConfigSeederTests | Sí |
| 9 | CenitOperationalGovernanceTests.CenitCalendarPolicy... | CR-1 | Limitación SQLite | Alta (regla CENIT 5 ciclos) | Alta | Evitar ORDER BY TimeSpan traducido en SQLite (materializar y ordenar en memoria controlada) | Medio | CenitOperationalGovernanceTests completo | Sí |
|10 | ReportServicesDataQualityTests.Reconciliation... | CR-4 | Expectativa obsoleta o regresión | Alta (reporting operativo/auditoría) | Alta | Revisar contrato funcional de métricas vs dataset; ajustar test o corregir servicio según decisión funcional | Medio-alto | ReportServicesDataQualityTests + snapshot de dataset | Sí |
|11 | ReportServicesDataQualityTests.ReturnsReport... | CR-4 | Expectativa obsoleta o regresión | Alta | Alta | Revisar causal DEV14 y resolver colisión de catálogo / filtro causal / mapping original | Medio-alto | idem #10 | Sí |
|12 | BulkTransactionScenarioSeederTests.IsIdempotent | CR-3 | Fixture/FK | Media (seed UAT/Dev) | Alta | Reforzar prerequisitos de entidades relacionadas y evitar llaves huérfanas | Medio | ambos tests de BulkTransactionScenarioSeeder | Sí |
|13 | BulkTransactionScenarioSeederTests.Creates... | CR-3 | Fixture/FK | Media | Alta | Igual #12 | Medio | idem | Sí |
|14 | ContrapartidaDispatchPersistenceServiceTests... | CR-2/CR-3 | Fixture + FK | Media-alta (dispatch contrapartida) | Alta | Semilla mínima explícita y estable para catálogo/batch/tx + evitar ids colisionantes | Medio | test actual + smoke de dispatch persistence | Sí |
|15 | TransactionPolicyServiceTests.RejectsDuplicate... | CR-3 | Fixture/FK | Alta (control duplicados) | Alta | Corregir `SeedCatalog`/dependencias exigidas por FK antes de persistir transacciones | Medio | 4 tests de TransactionPolicyService | Sí |
|16 | TransactionPolicyServiceTests.RejectsWhenOutside... | CR-3 | Fixture/FK | Alta (ventana operativa) | Alta | Igual #15 | Medio | idem | Sí |
|17 | TransactionPolicyServiceTests.DetectsDuplicateByTransactionExternalId... | CR-3 | Fixture/FK | Alta | Alta | Igual #15 | Medio | idem | Sí |
|18 | TransactionPolicyServiceTests.ValidateRequest... | CR-3 | Fixture/FK | Alta | Alta | Igual #15 | Medio | idem | Sí |

---

## 4) Priorización y orden recomendado de solución

## Fase P0-1 (primero, menor riesgo / mayor desbloqueo)
1. **CR-2** (colisión `CompanyEntryDescription.Id`) en tests bulk/prenotification/dispatch.  
2. **CR-3** (FK de fixtures) en TransactionPolicy, seeders y dispatch.

Objetivo: recuperar en verde ~14/18 tests rápidamente y estabilizar baseline de prueba.

## Fase P0-2 (segundo, compatibilidad proveedor)
3. **CR-1** en `ClearingHouseCycleConfigService` y `CenitOperatingCalendarPolicy` para comportamiento consistente SQLite/PostgreSQL.

Objetivo: cerrar brecha de provider-compatibility sin cambiar reglas funcionales.

## Fase P0-3 (tercero, decisión funcional reportes)
4. **CR-4** en reportes (métricas y causales). Validar con PO/funcional si cambió la regla o si hay regresión.

Objetivo: cerrar con decisión explícita y trazable (test update vs fix productivo puntual).

---

## 5) Plan de corrección recomendado (sin implementación en este prompt)

1. **Estabilizar fixtures comunes**
   - Unificar helper de semilla mínima por test domain (catalogs, clearing house, cycles, FIs).
   - Eliminar ids hardcodeados conflictivos cuando no son imprescindibles.
   - Verificar independencia de cada test respecto de seed global implícito.

2. **Cerrar compatibilidad SQLite/EF para TimeSpan y GroupBy-First**
   - Evitar expresiones no traducibles en rutas ejercidas por tests SQLite.
   - Mantener validación cruzada con PostgreSQL (no degradar ejecución real).

3. **Resolver divergencia en reportes con criterio de negocio firmado**
   - Confirmar contrato de “Sent/Received/Returned” y causal mapping esperado.
   - Congelar dataset canónico de prueba para evitar drift.

4. **Revalidación obligatoria de salida**
   - `dotnet test ...` completo.
   - `dotnet test ... --filter` por grupos corregidos.
   - (Recomendado) corrida PostgreSQL para confirmar no introducir sesgo SQLite-only.

---

## 6) Decisión de bloqueo de producción

- **Conclusión:** estos 18 fallos **sí bloquean** readiness productivo desde control de calidad (P0), aunque no todos impliquen defecto productivo directo.  
- Razón: la suite no está en estado confiable para certificar estabilidad de dominios críticos (policy, bulk, CENIT governance, reportes, seed operativo).

