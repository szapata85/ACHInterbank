# Validación de corrección P0 (CR-2 / CR-3) — 2026-04-26

## Alcance
Corrección controlada de pruebas backend afectadas por:
- **CR-2**: colisión de datos de fixture (`UNIQUE CompanyEntryDescription.Id`).
- **CR-3**: prerequisitos relacionales incompletos (`FOREIGN KEY constraint failed`).

Sin tocar CR-1 ni CR-4.

## Cambios aplicados (resumen)
1. Fixtures de tests pasaron de `CompanyEntryDescriptionId = 1` hardcodeado a resolución de catálogo existente (`NOMINAS`) seeded por EF.
2. Se reforzaron prerequisitos relacionales en tests:
   - `ClearingHouseConfig` antes de `ClearingHouse`.
   - `FinancialInstitution` válidas con `CheckDigit` calculado.
   - `AchCycle`/`AchBatch` consistentes con FK.
3. Seeds de pruebas quedaron idempotentes en escenarios clave.
4. `BulkTransactionScenarioSeederTests` dejó de mockear extensión `IsDevelopment()` (Moq no soporta extensión) y usa `EnvironmentName = Development`.

## Ejecución de validación
### Suite objetivo CR-2/CR-3
Comando:
```bash
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release \
  --filter "FullyQualifiedName~AchBulkTransactionServiceTests|FullyQualifiedName~PrenotificationHandlerTests|FullyQualifiedName~ClearingHouseCycleConfigSeederTests|FullyQualifiedName~BulkTransactionScenarioSeederTests|FullyQualifiedName~ContrapartidaDispatchPersistenceServiceTests|FullyQualifiedName~TransactionPolicyServiceTests"
```

Resultado:
- **Passed: 14**
- **Failed: 0**

### Revalidación de las 18 pruebas de la matriz original
Comando ejecutado con filtro de las 18 pruebas del análisis P0.

Resultado:
- **Passed: 14**
- **Failed: 4**
- Los 4 fallos remanentes son exactamente CR-1 y CR-4:
  - `CenitOperationalGovernanceTests.CenitCalendarPolicy_Throws_WhenCycleCountIsNotFive` (CR-1)
  - `ClearingHouseCycleConfigServiceTests.GetCurrentByClearingHouseAsync_ReturnsLatestVersionPerCycleForDate` (CR-1)
  - `ReportServicesDataQualityTests.ReconciliationReport_CalculatesTotalsDiffsAndInconsistencies` (CR-4)
  - `ReportServicesDataQualityTests.ReturnsReport_FiltersByReturnCodes_AndResolvesCausalAndOriginalTransaction` (CR-4)

## Conclusión
- **CR-2 y CR-3 cerrados para el set impactado en este prompt.**
- Estado global sigue bloqueado por CR-1 y CR-4 (fuera de alcance de esta corrección).
