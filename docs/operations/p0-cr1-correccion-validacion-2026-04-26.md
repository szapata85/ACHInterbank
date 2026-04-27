# Validación de corrección P0 (CR-1) — 2026-04-26

## Alcance
Corrección controlada de compatibilidad EF Core provider para los dos fallos CR-1:
1. `ClearingHouseCycleConfigServiceTests.GetCurrentByClearingHouseAsync_ReturnsLatestVersionPerCycleForDate`
2. `CenitOperationalGovernanceTests.CenitCalendarPolicy_Throws_WhenCycleCountIsNotFive`

Sin tocar CR-4.

## Cambio técnico aplicado
### 1) `ClearingHouseCycleConfigService.GetCurrentByClearingHouseAsync`
- Se reemplazó query no portable `GroupBy(...).Select(First...).OrderBy(CutoffTime)` en SQL por flujo de 2 fases:
  - fase SQL-safe: filtro por cámara/vigencia + `Include(ClearingHouse)` + materialización;
  - fase en memoria: `GroupBy` por ciclo, selección de versión vigente más reciente y orden por `CutoffTime`.
- Regla funcional preservada: “última versión activa por ciclo dentro de vigencia”.

### 2) `CenitOperatingCalendarPolicy.ValidateCycleConsistencyAsync`
- Se eliminó `OrderBy(StartTime)` traducido en SQL para SQLite.
- Se materializa primero y luego se ordena en memoria por `StartTime`.
- Regla funcional preservada: validación de 5 ciclos y secuencia CENIT 1..5.

## Evidencia de pruebas
### CR-1 targeted
- Evidencia: `docs/audits/evidence/p0-cr1-targeted-tests-2026-04-26.txt`
- Resultado: **Passed 2 / Failed 0**.

### Revalidación set original de 18 P0
- Evidencia: `docs/audits/evidence/p0-18-tests-revalidation-after-cr1-2026-04-26.txt`
- Resultado: **Passed 16 / Failed 2**.
- Fallos remanentes: únicamente CR-4 (reportes):
  - `ReportServicesDataQualityTests.ReconciliationReport_CalculatesTotalsDiffsAndInconsistencies`
  - `ReportServicesDataQualityTests.ReturnsReport_FiltersByReturnCodes_AndResolvesCausalAndOriginalTransaction`

## Impacto de performance
- El cambio materializa solo datasets de configuración acotados (ciclos/cycle-config por cámara y fecha), por lo que el impacto esperado es bajo y aceptable para estos servicios de configuración.
