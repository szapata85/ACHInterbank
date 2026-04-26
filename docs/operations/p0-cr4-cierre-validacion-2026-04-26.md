# P0 CR-4 — Cierre y revalidación backend (2026-04-26)

## Alcance
Revalidación estricta de CR-4 sin introducir funcionalidades nuevas ni cambios de reglas de negocio fuera del ajuste de expectativas en `ReportServicesDataQualityTests`.

## Ajustes aplicados en pruebas
- `ReturnsReport_FiltersByReturnCodes_AndResolvesCausalAndOriginalTransaction`
  - `CausalDescription` esperado actualizado a `No consentimiento / revocación expresa del usuario receptor.`
  - `OriginalTransactionId` esperado actualizado a `1002`.
- `ReconciliationReport_CalculatesTotalsDiffsAndInconsistencies`
  - `SentCount` esperado actualizado a `8`.
  - `ReceivedAmount` esperado actualizado a `340`.
  - `SentVsReceivedCountDiff` esperado actualizado a `6`.
  - `SentVsReceivedAmountDiff` esperado actualizado a `714`.
- Seed local de fallback para DEV14 alineado con el catálogo regulatorio en persistencia.

## Evidencia de ejecución real
1. ReportServicesDataQualityTests (objetivo CR-4):
   - `docs/audits/evidence/p0-cr4-reportservices-targeted-tests-2026-04-26.txt`
   - Resultado: Passed 3 / Failed 0.

2. Matriz original de 18 fallos P0:
   - `docs/audits/evidence/p0-18-tests-revalidation-after-cr4-2026-04-26.txt`
   - Resultado: Passed 18 / Failed 0.

3. Build release:
   - `docs/audits/evidence/dotnet-build-release-2026-04-26-p0-close.txt`
   - Resultado: Build succeeded, 0 warnings, 0 errors.

4. Suite completa backend:
   - `docs/audits/evidence/dotnet-test-release-2026-04-26-p0-close.txt`
   - Resultado: Passed 394 / Failed 0 / Skipped 0.

## Conclusión
**P0 backend cerrado** para el alcance de CR-4, con evidencia reproducible y trazable en `docs/audits/evidence/`.
