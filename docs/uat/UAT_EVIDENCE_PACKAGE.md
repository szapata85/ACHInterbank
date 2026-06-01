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
- Ejecucion UAT ampliada: `docs/uat/UAT_EXPANDED_EXECUTION_ROUND.md`.
- Actualizacion defectos UAT ampliado: `docs/uat/UAT_EXPANDED_DEFECTS_UPDATE.md`.
- Resumen ejecutivo UAT ampliado: `docs/uat/UAT_EXPANDED_EXECUTIVE_SUMMARY.md`.
- Preparacion UAT externo ACH/CENIT: `docs/uat/EXTERNAL_UAT_PREPARATION_ACH_CENIT.md`.
- RACI UAT externo: `docs/uat/EXTERNAL_UAT_RACI.md`.
- Plan de ventanas UAT externo: `docs/uat/EXTERNAL_UAT_WINDOW_PLAN.md`.
- Controles de seguridad UAT externo: `docs/uat/EXTERNAL_UAT_SECURITY_CONTROLS.md`.
- Solicitudes de evidencia externa: `docs/uat/EXTERNAL_UAT_EVIDENCE_REQUESTS.md`.
- Paquete aprobacion Seguridad/Compliance: `docs/uat/SECURITY_APPROVAL_PACKAGE.md`.
- Registro certificados/endpoints UAT: `docs/uat/UAT_CERTIFICATE_ENDPOINT_REGISTER.md`.
- Modelo custodia secretos UAT: `docs/uat/UAT_SECRET_CUSTODY_MODEL.md`.
- Checklist aprobacion Seguridad: `docs/uat/UAT_SECURITY_APPROVAL_CHECKLIST.md`.
- Solicitudes evidencia Seguridad: `docs/uat/UAT_SECURITY_EVIDENCE_REQUESTS.md`.
- Simulacion aprobacion Seguridad/UAT: `docs/uat/SECURITY_APPROVAL_SIMULATION.md`.
- Checklist pre-habilitacion externa: `docs/uat/EXTERNAL_PRE_ENABLEMENT_CHECKLIST.md`.
- Gap analysis evidencia Seguridad: `docs/uat/SECURITY_EVIDENCE_GAP_ANALYSIS.md`.
- Borrador solicitud Seguridad/Compliance: `docs/uat/SECURITY_APPROVAL_REQUEST_DRAFT.md`.
- Solicitud formal Seguridad/Compliance: `docs/uat/SECURITY_COMPLIANCE_REVIEW_REQUEST.md`.
- Indice paquete aprobacion externa: `docs/uat/EXTERNAL_APPROVAL_PACKAGE_INDEX.md`.
- Matriz decision Seguridad/Compliance: `docs/uat/SECURITY_COMPLIANCE_DECISION_MATRIX.md`.
- Checklist envio revision Seguridad: `docs/uat/SECURITY_REVIEW_SUBMISSION_CHECKLIST.md`.
- Declaracion Productivo NO-GO: `docs/uat/PRODUCTIVE_NO_GO_ATTESTATION.md`.
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
6. Revisar ejecucion UAT ampliada y actualizacion de defectos.
7. Revisar paquete `EXTERNAL_UAT_*` para coordinacion con ACH Colombia/CENIT.
8. Revisar paquete Seguridad 6D.5 antes de recibir/cargar certificados o endpoints.
9. Revisar simulacion 6D.6 y brechas antes de enviar solicitud formal.
10. Revisar paquete formal 6D.7 para presentacion a Seguridad/Compliance.
11. Ejecutar checklist y escenarios UAT con terceros en ambiente formal solo tras aprobacion.
12. Registrar defectos/hallazgos, riesgos/brechas y decision de comite.
13. Confirmar que no hay SOAP real, movimientos monetarios, mutaciones criticas, legacy oficial ni `/NachaExport/{hash}`.

## Limitacion

Este paquete organiza evidencia automatizada y trazabilidad interna. No reemplaza certificacion oficial ACH Colombia/CENIT ni aprobacion formal de salida productiva.

## Estado coordinacion externa

Listo para coordinacion externa documental con ACH Colombia/CENIT. No listo para productivo.

## Estado seguridad 6D.5

Preparacion de Seguridad/Compliance documentada. Certificados/endpoints/secretos permanecen pendientes, no cargados y no aprobados.

## Estado simulacion 6D.6

Listo para solicitar revision Seguridad/Compliance. No aprobado y no listo para cargar secretos/certificados/endpoints.

## Estado paquete formal 6D.7

Listo para presentacion a Seguridad/Compliance. Revision y decisiones siguen pendientes; no aprobado.
