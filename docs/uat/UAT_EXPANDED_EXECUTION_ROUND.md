# Ejecucion UAT ampliada - Fase 6D.3

Productivo permanece NO-GO. Esta ejecucion es controlada/documental con dataset sintetico cargado para evidencia UAT interna; no ejecuta SOAP real, no mueve dinero, no usa datos reales, no modifica golden files y no habilita productivo.

## Datos de ejecucion

- Fecha: 2026-06-01.
- Commit base informado: `f5ee93645fe95a306f7cde3c0a0c46784783512a`.
- Ambiente objetivo: aislado/UAT controlado con datos sinteticos.
- Dataset cargado: `docs/uat/UAT_EXPANDED_SYNTHETIC_DATASET.md`.
- Evidencia base: Playwright/CI, consolas read-only, matriz RTM y golden files semirreales.

## Alcance ejecutado

- Escenarios nuevos UAT-EXP-001 a UAT-EXP-005.
- Revalidacion documental de hallazgos UAT-FND-001 a UAT-FND-006.
- Validacion de manual review, inconsistencias, ciclo CENIT sintetico y guardas NO-GO/read-only.

## Exclusiones

- Certificacion oficial ACH Colombia/CENIT.
- SOAP real, certificados reales, endpoints productivos y payloads reales.
- Movimientos monetarios, mutaciones criticas, datos reales de clientes, legacy oficial y `/NachaExport/{hash}`.

## Resultados por escenario

| ID escenario | Dataset usado | Camara | Objetivo | Pasos resumidos | Evidencia esperada | Evidencia obtenida/documentada | Resultado | Observacion | Hallazgo asociado |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| UAT-EXP-001 | DS-EXP-CONC-MANUAL, DS-EXP-ACH-RET-002 | Ambas | Causal ambigua clasifica manual review | Consultar respuesta ambigua en conciliacion read-only | Badge manual review, sin mutacion | `ach-reconciliation.spec.ts`, `UAT_DEFECTS_MATRIX.md` | OK | Cierra control documental de causales ambiguas; homologacion oficial sigue pendiente | UAT-FND-003 |
| UAT-EXP-002 | DS-EXP-CONC-INCONS, DS-EXP-CEN-IN-002 | Ambas | Respuesta diferencial inconsistente queda sin mutacion | Revisar inconsistencia y guardas read-only | Estado inconsistente, sin POST/PUT/PATCH/DELETE | `ach-reconciliation.spec.ts`, `PRE_UAT_AUTOMATED_CHECKLIST.md` | OK | Evidencia automatizada cubre no mutacion | UAT-FND-003 |
| UAT-EXP-003 | DS-EXP-CEN-CYCLE | CENIT | Ciclo/cola/neteo sintetico visible sin movimiento | Revisar dashboard/read-store con ciclo sintetico documentado | Estado trazable, warning si fuente parcial | `UAT_EXPANDED_SYNTHETIC_DATASET.md`, dashboard/read-store | Observado | Requiere ambiente UAT externo para ejecutar ciclo/cola/neteo representativo | UAT-FND-004 |
| UAT-EXP-004 | DS-EXP-PRENOTE-REJ, DS-EXP-PRENOTE-APP | ACH Colombia | Prenote rechazada con causal homologada | Revisar prenote no monetaria y causal | Estado no monetario, causal sanitizada | Reportes prenote sinteticos, conciliacion read-only | OK | Cubre evidencia sintetica; certificacion oficial no aplica aun | UAT-FND-003 |
| UAT-EXP-005 | DS-EXP-CI-GUARDS, DS-EXP-SOAP-READONLY | Ambas | Guardas read-only, NO-GO y no hash en CI | Revisar artefactos Playwright y checklist automatizado | No SOAP real, no mutaciones, no `/NachaExport/{hash}` | `PLAYWRIGHT_EVIDENCE.md`, `PRE_UAT_AUTOMATED_CHECKLIST.md` | OK | Guardas criticas confirmadas por evidencia existente | UAT-FND-006 |

## Resumen numerico

| Total escenarios | OK | Observado | Bloqueado | No ejecutado |
| --- | --- | --- | --- | --- |
| 5 | 4 | 1 | 0 | 0 |

## Cierre

La ronda ampliada mejora cobertura sintetica sobre causales, manual review, inconsistencias y guardas CI. No reemplaza certificacion oficial ni ejecucion con terceros. Productivo permanece NO-GO.
