# Paquete UAT de evidencia - Fase 6C

## Indice minimo

- Matriz requisito-norma-codigo-prueba-evidencia: `docs/uat/REQUIREMENT_TRACEABILITY_MATRIX.md`.
- Evidencia Playwright/CI: `docs/uat/PLAYWRIGHT_EVIDENCE.md`.
- Contexto Phase 6: `docs/ai/ACH_PHASE6_CONTEXT.md`.
- Auditoria legacy: `docs/ai/ACH_PHASE6_LEGACY_AUDIT.md`.
- Golden files semirreales: `tests/Cfa.ACHInterbank.Tests/TestData/Nacha/GoldenFiles`.
- Workflow evidencia UI: `.github/workflows/angular-ci.yml`.

## Uso para comite

1. Revisar filas con estado `Parcial`, `Pendiente`, `Requiere certificacion oficial` o `No aplica productivo NO-GO`.
2. Validar artefactos CI `playwright-report`, `playwright-test-results` y `uat-evidence-playwright`.
3. Confirmar que no hay SOAP real, movimientos monetarios, mutaciones criticas, legacy oficial ni `/NachaExport/{hash}`.

## Limitacion

Este paquete organiza evidencia automatizada y trazabilidad interna. No reemplaza certificacion oficial ACH Colombia/CENIT ni aprobacion formal de salida productiva.
