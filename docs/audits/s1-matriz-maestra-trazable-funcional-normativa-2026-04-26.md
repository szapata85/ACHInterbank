# S1 — Matriz maestra trazable funcional-normativa (ACHInterbank)

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
| S1-07 | Return-of-return | `CENIT Anexo A` (R60-R74) + ADR phase6 | `ReturnOfReturnOrchestrator`, `AchReturnOfReturnPolicy` | `CenitOperationalGovernanceTests` | `payment-rail-shadow-compare-phase6-validation` | Negocio + Arquitectura | Parcial (UAT GO) | Falta cobertura E2E operacional y aprobación normativa explícita. |
| S1-08 | Ciclos ACH | `docs/normativa/md/ACH-Colombia-V32.md` | `AchCyclesController`, `ClearingHouseCycleConfigService` | `ClearingHouseCycleConfigServiceTests`, seeder tests | evidencia P0 CR-1 | Operaciones + QA | Parcial (UAT GO) | Requiere tabla de equivalencia norma-ciclo por horario oficial. |
| S1-09 | Ciclos CENIT (5 ciclos consecutivos) | `docs/normativa/md/CENIT-DSP-152-Anexo-2.md` | `CenitOperatingCalendarPolicy` | `CenitOperationalGovernanceTests` | `p0-cr1-targeted-tests-2026-04-26.txt` | Operaciones + QA | Parcial (UAT GO) | Falta evidencia operativa externa de calendario en entorno homologado. |
| S1-10 | Neteo CENIT | ADR shadow compare phase6 + DSP/Anexo 2 | `CenitNettingService` | `CenitOperationalGovernanceTests` + paymentrail tests | `payment-rail-shadow-compare-phase6-validation` | Operaciones + Arquitectura | **Bloqueado (NO-GO)** | Falta validación E2E real de neteo con criterios de compensación/liquidación. |
| S1-11 | Liquidez/CUD | DSP/CENIT + runbooks operativos | `LiquidityOptimizationService` | `CenitOperationalGovernanceTests` | `payment-rail-shadow-compare-phase6-validation` | Operaciones + Negocio | **Bloqueado (NO-GO)** | Falta validación operacional real/CUD y aceptación formal de reglas. |
| S1-12 | Naming externo ACH/CENIT/STA | `ADR-ExternalFileNamePolicy-*`, matriz normativa externa filename | `ExternalFileNamePolicy`, `NachaExportController` | `ExternalFileNamePolicyPhase1Tests`, `NachaExportControllerTests` | `docs/audits/external-filename-normative-matrix-ach-cenit-sta-2026-04-20*.md` | Compliance + Arquitectura | **Bloqueado (NO-GO)** | Documento declara reglas ACH pendientes de confirmación normativa. |
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
- **Parcial (UAT GO):** S1-01,02,03,04,05,06,07,08,09,14,15,16,17,18,19.
- **Bloqueado (NO-GO productivo):** S1-10,11,12,13,20.

---

## 5) Criterios de salida (gatillos para pasar a GO productivo)

1. Cierre de S1-12 (naming externo) con confirmación normativa oficial documentada.
2. Cierre de S1-13 (sobre digital) con validación externa/interoperabilidad oficial.
3. Cierre de S1-10/S1-11 con evidencia E2E operacional de neteo/liquidez/CUD.
4. Cierre de S1-20 con ejecución y firma de checklist UAT/runbooks sin pendientes críticos.
5. Firma conjunta de negocio + compliance + operaciones + seguridad + arquitectura.

---

## 6) Prompts siguientes sugeridos

- **S2:** Cierre normativo de naming externo ACH/CENIT/STA (fuente primaria + reglas congeladas).
- **S3:** Validación externa del sobre digital (vector oficial, criterios de aceptación y evidencia).
- **S4:** E2E operativo neteo/liquidez/CUD en entorno homologado.
- **S5:** Cierre de plan UAT NACHA Security y acta de conformidad.
- **S6:** Acta final Go/No-Go unificada y limpieza de drift documental.
