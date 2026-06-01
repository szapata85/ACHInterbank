# Paquete UAT de evidencia - Fase 6C

## Indice minimo

- Matriz requisito-norma-codigo-prueba-evidencia: `docs/uat/REQUIREMENT_TRACEABILITY_MATRIX.md`.
- Evidencia Playwright/CI: `docs/uat/PLAYWRIGHT_EVIDENCE.md`.
- Preparacion UAT formal ACH Colombia/CENIT: `docs/uat/UAT_FORMAL_PREPARATION_ACH_CENIT.md`.
- Checklist ejecucion UAT: `docs/uat/UAT_EXECUTION_CHECKLIST.md`.
- Escenarios UAT ACH/CENIT: `docs/uat/UAT_TEST_SCENARIOS_ACH_CENIT.md`.
- Riesgos y brechas UAT: `docs/uat/UAT_RISKS_AND_GAPS.md`.
- Hardening tecnico pre-UAT: `docs/uat/PRE_UAT_TECHNICAL_HARDENING.md`.
- Checklist automatizado pre-UAT: `docs/uat/PRE_UAT_AUTOMATED_CHECKLIST.md`.
- Dataset sintetico UAT ronda 1: `docs/uat/UAT_SYNTHETIC_DATASET.md`.
- Ejecucion UAT controlada ronda 1: `docs/uat/UAT_EXECUTION_ROUND_1.md`.
- Matriz defectos/hallazgos UAT: `docs/uat/UAT_DEFECTS_MATRIX.md`.
- Resumen ejecutivo UAT ronda 1: `docs/uat/UAT_ROUND_1_EXECUTIVE_SUMMARY.md`.
- Cierre hallazgos ronda 1: `docs/uat/UAT_ROUND_1_FINDINGS_CLOSURE.md`.
- Plan UAT ampliado: `docs/uat/UAT_EXPANDED_ROUND_PLAN.md`.
- Dataset sintetico ampliado: `docs/uat/UAT_EXPANDED_SYNTHETIC_DATASET.md`.
- Contexto Phase 6: `docs/ai/ACH_PHASE6_CONTEXT.md`.
- Auditoria legacy: `docs/ai/ACH_PHASE6_LEGACY_AUDIT.md`.
- Golden files semirreales: `tests/Cfa.ACHInterbank.Tests/TestData/Nacha/GoldenFiles`.
- Workflow evidencia UI: `.github/workflows/angular-ci.yml`.

## Uso para comite

1. Revisar filas con estado `Parcial`, `Pendiente`, `Requiere certificacion oficial` o `No aplica productivo NO-GO`.
2. Validar artefactos CI `playwright-report`, `playwright-test-results` y `uat-evidence-playwright`.
3. Ejecutar checklist automatizado pre-UAT y registrar observaciones tecnicas.
4. Revisar dataset sintetico y ejecucion controlada ronda 1.
5. Revisar cierre de hallazgos ronda 1 y plan de UAT ampliado.
6. Ejecutar checklist y escenarios UAT con datos sinteticos ampliados en ambiente formal.
7. Registrar defectos/hallazgos, riesgos/brechas y decision de comite.
8. Confirmar que no hay SOAP real, movimientos monetarios, mutaciones criticas, legacy oficial ni `/NachaExport/{hash}`.

## Limitacion

Este paquete organiza evidencia automatizada y trazabilidad interna. No reemplaza certificacion oficial ACH Colombia/CENIT ni aprobacion formal de salida productiva.
