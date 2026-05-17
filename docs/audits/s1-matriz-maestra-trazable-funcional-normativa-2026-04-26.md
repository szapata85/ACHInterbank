# S1 — Matriz maestra trazable funcional-normativa (ACHInterbank)

> Referencia UAT complementaria: `docs/uat/incoming-return-orphan-acceptance-checklist.md`.


**Fecha:** 2026-04-26 (UTC)  
**Objetivo:** Trazabilidad auditable: requisito normativo/funcional → fuente documental → implementación → pruebas → evidencia → dueño → estado.  
**Estado base:** P0 técnico backend en verde, producción funcional/normativa en NO-GO (según scorecard).  

---

## 1) Convenciones de estado

- **Listo (GO)**: requisito con documento fuente + implementación + prueba + evidencia vigente + dueño.
- **Parcial (UAT GO)**: existe implementación/prueba técnica, pero falta cierre normativo/externo/operativo.
- **Bloqueado (NO-GO)**: brecha normativa/operativa crítica para salida productiva.

## 2) Dueños (RACI mínimo)

- **Negocio ACH**: reglas funcionales, aceptación de operación.
- **Compliance/Normativa**: conformidad regulatoria y fuentes oficiales.
- **Arquitectura Backend**: diseño, implementación y decisiones técnicas.
- **QA Backend**: cobertura de pruebas y evidencia reproducible.
- **Seguridad**: firma/cifrado/certificados/controles de exposición.
- **Operaciones**: runbooks, UAT operativo, transición a producción.
- **Equipo SPA**: experiencia operativa UI y consumo de APIs.

---

## 3) Matriz maestra

| ID | Requisito normativo/funcional | Documento fuente | Implementación (módulo/ruta) | Prueba(s) | Evidencia | Dueño principal | Estado | Brecha/observación |
|---|---|---|---|---|---|---|---|---|
| S1-01 | NACHA-M parser (inbound) | `docs/architecture/incoming-nacha-processing-analysis-2026-04-23.md` | `NachaParserService`, `IncomingNachaIngestionAppService` | `IncomingNacha*Tests` | docs de revalidación IncomingNacha | Arquitectura + QA | Parcial (UAT GO) | Falta matriz normativa firmable por regla de parser vs norma externa. |
| S1-02 | NACHA-M builder/generación | `docs/auditoria-nacha-m-2026-04-17.md`, `docs/reporting/questpdf-implementation-phases.md` | `INachaFileBuilder`/builder en Persistence | `AchTransactionNachaTests`, `Nacha*Tests` | evidencias `Nacha|Mapping|BatchNumber` en docs/ops/dev | Arquitectura + QA | Parcial (UAT GO) | Falta consolidación requisito→regla normativa→test en tabla única. |
| S1-03 | Cargue inbound | `docs/api/incoming-nacha-command-center-api-2026-04-24.md` | `NachaUploadController`, `IncomingNachaIngestionAppService` | `IncomingNachaIngestionAppServiceTests` | `docs/operations/incoming-nacha-uat-controlled-evidence-2026-04-25.md` | Operaciones + QA | Parcial (UAT GO) | Requiere acta de operación con criterios de producción. |
| S1-04 | Clasificación inbound | `docs/architecture/incoming-nacha-command-center-state-machine-2026-04-24.md` | `IncomingNachaFunctionalClassifier`, `IncomingNachaPostParseProcessor` | `IncomingNachaFunctionalClassifierTests`, `IncomingNachaPostParseProcessorTests` | evidencias incoming-nacha ops | Arquitectura + QA | Parcial (UAT GO) | Validación normativa detallada por causal pendiente de consolidar. |
| S1-05 | Prenotificaciones | `docs/normativa/md/CENIT-Anexo-A-Causales-Devolucion.md` (R10/R31) | `PrenotificationHandler`, `AchPrenotificationPolicy` | `PrenotificationHandlerTests`, `IncomingNachaPrenotificationResolverTests` | evidencia de tests backend P0 close | Negocio ACH + QA | Parcial (UAT GO) | Falta sign-off de negocio sobre reglas finales por cámara/flujo. |
| S1-06 | Devoluciones | `docs/normativa/md/CENIT-Anexo-A-Causales-Devolucion.md`, `docs/normativa/md/ACH-Colombia-V32.md` | `AchReturnsService`, catálogos regulatorios | `RegulatoryCatalogSeederTests`, `ReportServicesDataQualityTests` | `p0-cr4-*`, `dotnet-test-release-2026-04-26-p0-close.txt` | Negocio + Compliance | Parcial (UAT GO) | Cierre regulatorio integral (catálogo vigente firmado) pendiente. |
| S1-07 | Return-of-return | `docs/normativa/md/CENIT-Anexo-A-Causales-Devolucion.md`, `docs/normativa/md/ACH-Colombia-V32.md` + ADR phase6 | Endpoints: `/ach-returns/return-of-return/evaluate`, `/generate-audit-file`, `/generate-nacha-file`; `ReturnOfReturnOrchestrator`, `AchReturnOfReturnPolicy`, SPA `/transactions/returns-ror` | Evidencia técnica reportada: restore/build/test backend (291), build SPA; pendiente prueba SPA en ChromeHeadless por `libatk-1.0.so.0` | Evidencia ROR audit pipe (`ROR|...`, `FLOW|...`) + archivo NACHA-M independiente (1/5/6/7/8/9) | Negocio + Arquitectura + Operaciones + Compliance | Cerrado técnico / UAT GO controlado / NO-GO productivo | Cierre técnico completado; pendiente UAT E2E y aprobación normativa explícita por cámara (CENIT/ACH Colombia) antes de GO productivo. |
| S1-08 | Ciclos ACH | `docs/normativa/md/ACH-Colombia-V32.md` | `AchCyclesController`, `ClearingHouseCycleConfigService` | `ClearingHouseCycleConfigServiceTests`, seeder tests | evidencia P0 CR-1 | Operaciones + QA | Parcial (UAT GO) | Requiere tabla de equivalencia norma-ciclo por horario oficial. |
| S1-09 | Ciclos CENIT (5 ciclos consecutivos) | `docs/normativa/md/CENIT-DSP-152-Anexo-2.md` | `CenitOperatingCalendarPolicy` | `CenitOperationalGovernanceTests` | `p0-cr1-targeted-tests-2026-04-26.txt` | Operaciones + QA | Parcial (UAT GO) | Falta evidencia operativa externa de calendario en entorno homologado. |
| S1-10 | Neteo CENIT | ADR shadow compare phase6 + DSP/Anexo 2 | `CenitNettingService` | `CenitOperationalGovernanceTests` + paymentrail tests | `payment-rail-shadow-compare-phase6-validation` | Operaciones + Arquitectura | **Bloqueado (NO-GO)** | Falta validación E2E real de neteo con criterios de compensación/liquidación. |
| S1-11 | Liquidez/CUD | DSP/CENIT + runbooks operativos | `LiquidityOptimizationService` | `CenitOperationalGovernanceTests` | `payment-rail-shadow-compare-phase6-validation` | Operaciones + Negocio | **Bloqueado (NO-GO)** | Falta validación operacional real/CUD y aceptación formal de reglas. |
| S1-12 | Naming externo ACH/CENIT/STA | `ADR-ExternalFileNamePolicy-*`, `docs/audits/external-filename-normative-matrix-ach-cenit-sta-current.md` (vigente) | `ExternalFileNamePolicy`, `NachaExportController`, `AchReturnsService`, `AchReturnOfReturnFileGenerationService` | `ExternalFileNamePolicyPhase1Tests`, `NachaExportControllerTests` | `docs/audits/external-filename-normative-matrix-ach-cenit-sta-current.md` | Compliance + Arquitectura | **Bloqueado (NO-GO)** | Matriz vigente consolidada; persisten hardcodes RET/RORNACHA y cobertura parcial por flujo, por lo que se mantiene NO-GO productivo hasta remediación + firma. |
| S1-13 | Sobre digital / firma / cifrado | `docs/normativa/md/ACH-Colombia-V32.md` (Anexo 21), ADR digital envelope | `DigitalEnvelopeSignatureValidator`, `NachaSecurityOperationService` | `DigitalEnvelopeSignatureFailCloseTests`, `DigitalEnvelopeInteroperabilityHarnessTests` | auditorías de digital envelope + plan UAT | Seguridad + Compliance | **Bloqueado (NO-GO)** | Falta cierre de interoperabilidad oficial externa/vector definitivo. |
| S1-14 | Certificados / OpenBao | `docs/architecture/openbao-integration-2026-04-22.md` | `DigitalEnvelopeCertificate*`, resolvers/repositories, config OpenBao | `DigitalEnvelopeCertificateResolverTests`, `NachaSecurityOperationsControllerTests` | docs dev/openbao + uat plan | Seguridad + Operaciones | Parcial (UAT GO) | Faltan evidencias de hardening final productivo y sign-off seguridad. |
| S1-15 | Reportes operativos/auditoría | `docs/reporting/ach-reporting-module-architecture.md` | `ReportsController`, servicios de reportes | `ReportServicesDataQualityTests`, `ReportsControllerTests` | `p0-cr4-reportservices-targeted-tests-2026-04-26.txt` | Negocio + QA | Parcial (UAT GO) | Falta matriz report-to-regulation firmable por compliance/negocio. |
| S1-16 | Command Center | `docs/api/incoming-nacha-command-center-api-2026-04-24.md` | `IncomingNachaCommandCenterService/Controller` | `IncomingNachaCommandCenterServiceTests` | ops revalidation incoming nacha | Operaciones + QA | Parcial (UAT GO) | Falta acta operativa de uso en contingencia real. |
| S1-17 | Observabilidad / auditoría | runbook observabilidad + arquitectura resiliencia | `IncomingNacha*Observability` endpoints/servicios | `IncomingNacha*` suites | `docs/operations/incoming-nacha-observability-revalidation-2026-04-24.md` | Operaciones + Arquitectura | Parcial (UAT GO) | Falta evidencia de KPIs/SLO en ventana operativa productiva. |
| S1-18 | PaymentRail / capability registry | ADR phase7 + docs operations phase7/8/9/10 | servicios PaymentRail, capability registry API | `PaymentRail*Tests` | `docs/operations/payment-rail-capability-registry-phase*-*.md` | Arquitectura + QA | Parcial (UAT GO) | Falta cierre formal de gobernanza multi-riel para producción total. |
| S1-19 | SPA operativa | docs SPA + frontend architecture | `web/ach-interbank-ui` (no auditado en este prompt) | suites Karma históricas | docs phase9/10 spa validation | Equipo SPA + QA | Parcial (UAT GO) | Requiere revalidación final integrada backend+spa para corte Go-Live. |
| S1-20 | UAT / runbooks / evidencia | `docs/uat/nacha-security-uat-plan-2026-04-22.md`, runbooks ops | artefactos de operaciones y evidencia | checklists UAT y escenarios | docs/operations + docs/audits/evidence | Operaciones + Compliance | **Bloqueado (NO-GO)** | Plan UAT contiene múltiples escenarios aún en estado Pendiente. |

---

## 4) Resumen por estado

- **Listo (GO):** 0 dominios con cierre funcional-normativo completo firmado.
- **Parcial (UAT GO):** S1-01,02,03,04,05,06,08,09,14,15,16,17,18,19.
- **Cerrado técnico / UAT GO controlado / NO-GO productivo:** S1-07 (Return-of-return).
- **Bloqueado (NO-GO productivo):** S1-10,11,12,13,20.

---

## 5) Criterios de salida (gatillos para pasar a GO productivo)

1. Cierre de S1-12 (naming externo) con confirmación normativa oficial documentada.
2. Cierre de S1-13 (sobre digital) con validación externa/interoperabilidad oficial.
3. Cierre de S1-10/S1-11 con evidencia E2E operacional de neteo/liquidez/CUD.
4. Cierre de S1-20 con ejecución y firma de checklist UAT/runbooks sin pendientes críticos.
5. Firma conjunta de negocio + compliance + operaciones + seguridad + arquitectura.

---


- **Nota de vigencia naming:** la referencia operativa actual para este dominio es `docs/audits/external-filename-normative-matrix-ach-cenit-sta-current.md`; v1/v2 quedan como histórico.

## 6) Prompts siguientes sugeridos

- **S2:** Cierre normativo de naming externo ACH/CENIT/STA (fuente primaria + reglas congeladas).
- **S3:** Validación externa del sobre digital (vector oficial, criterios de aceptación y evidencia).
- **S4:** E2E operativo neteo/liquidez/CUD en entorno homologado.
- **S5:** Cierre de plan UAT NACHA Security y acta de conformidad.
- **S6:** Acta final Go/No-Go unificada y limpieza de drift documental.

- Evidencia UAT de soporte para naming externo: `docs/uat/naming-returns-ror-acceptance-checklist.md` (mantiene NO-GO productivo hasta cierre formal).

- Matriz NACHA-M por registro y cámara (fuente vigente): `docs/audits/nacha-record-level-normative-matrix-ach-cenit-current.md` (mantiene NO-GO productivo hasta cierre formal).

- Evidencia UAT de registros NACHA-M: `docs/uat/nacha-records-acceptance-checklist.md` (controla aceptación por cámara y salida de NO-GO productivo).

- Matriz vigente de causales por cámara/flujo: `docs/audits/cause-code-normative-matrix-ach-cenit-sta-current.md` (mantiene NO-GO productivo hasta cierre normativo/UAT firmado).

- Evidencia UAT complementaria de causales por cámara/flujo: `docs/uat/cause-code-acceptance-checklist.md` (pendiente cierre formal; mantiene NO-GO productivo).

- Referencia de control vigente para devolución saliente (estado/evento/idempotencia): `docs/audits/outbound-return-state-traceability-matrix-current.md` (mantiene NO-GO productivo).
- Checklist UAT de estado/evento/trazabilidad de devolución saliente: `docs/uat/outbound-return-state-traceability-acceptance-checklist.md` (control UAT formal para salida de NO-GO productivo).


## Referencia adicional vigente — incoming/orphan

Se agrega como soporte de trazabilidad vigente:

- `docs/audits/incoming-return-e2e-orphan-matrix-current.md`

Esta inclusión no cambia el estado global de readiness: se mantiene **NO-GO productivo**.

## Referencia cruzada total vs partial

Para la frontera semántica canónica entre `RejectedTotal`, `RejectedPartial`, `Accepted`, orphan/unresolved, manual audit-only y la distinción formal frente a devolución parcial por monto, ver:

- `docs/audits/total-vs-partial-rejection-matrix-current.md`

## Referencia cruzada checklist UAT total vs partial

Para la validación UAT paso-a-paso de `Accepted`, `RejectedTotal`, `RejectedPartial`, orphan/unresolved, manual audit-only, separación de códigos y relación con ROR/contabilidad, ver:

- `docs/uat/rejection-total-partial-acceptance-checklist.md`

- Referencia complementaria ciclos/neteo/liquidez/evidencia CUD: `docs/audits/cenit-cycles-netting-liquidity-cud-matrix-current.md` (no cambia decisión NO-GO productivo).

- Referencia UAT ciclos/liquidez/evidencia CUD: `docs/uat/cenit-cycles-liquidity-cud-acceptance-checklist.md` (no cambia decisión NO-GO productivo).

- Referencia matriz vigente de sobre/firma/certificados: `docs/audits/digital-envelope-signature-certificate-matrix-current.md` (no modifica decisión NO-GO productivo).

- Referencia checklist UAT de sobre/firma/certificados: `docs/uat/digital-envelope-certificate-acceptance-checklist.md` (no modifica decisión NO-GO productivo).
