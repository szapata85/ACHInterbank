# Matriz consolidada S1 — requisito → norma → código → prueba → evidencia por cámara

## 1. Propósito

Este documento consolida el estado vigente de trazabilidad S1 para:
- requisito;
- norma/fuente;
- cámara;
- regla funcional;
- implementación;
- prueba;
- evidencia;
- UAT;
- dueño;
- estado;
- brecha;
- criterio de salida.

Aclaraciones:
- Este documento **no habilita producción**.
- Este documento **no reemplaza** matrices específicas por dominio.
- Este documento **consolida estado vigente** según fuentes internas del repositorio.
- Se mantiene **NO-GO productivo** mientras existan P0 abiertos.

## 2. Reglas anti-inferencia

- No se declara cumplimiento sin fuente localizada.
- No se declara cierre sin prueba localizada.
- No se declara cierre UAT sin evidencia/aprobación humana.
- No se declara cierre productivo sin evidencias externas/operativas exigidas.
- ACH Colombia y CENIT se separan cuando aplique.
- Evidencia técnica automatizada no equivale a aprobación humana.
- Evidencia UAT asistida por IA no equivale a firma de Operaciones/Negocio/Compliance.

## 3. Fuentes normativas localizadas

| Documento | Ruta | Cámara/Fuente | Uso | Estado |
|---|---|---|---|---|
| ACH Colombia V3.2 | `docs/normativa/md/ACH-Colombia-V32.md` | ACH Colombia | Fuente primaria para reglas ACH/NACHA donde aplica | Localizada |
| CENIT DSP-152 Anexo 2 | `docs/normativa/md/CENIT-DSP-152-Anexo-2.md` | CENIT / Banco de la República | Fuente primaria para ciclos, horarios y marco operativo CENIT | Localizada |
| CENIT Anexo A Causales Devolución | `docs/normativa/md/CENIT-Anexo-A-Causales-Devolucion.md` | CENIT / Banco de la República | Fuente primaria de causales Rxx de devolución | Localizada |
| CENIT Anexo B Causales Rechazo | `docs/normativa/md/CENIT-Anexo-B-Causales-Rechazo.md` | CENIT / Banco de la República | Fuente primaria de causales de rechazo | Localizada |

## 4. Clasificación de evidencia

| Tipo de evidencia | Descripción | Permite GO técnico | Permite GO UAT | Permite GO productivo | Observación |
|---|---|---|---|---|---|
| Prueba automatizada | Unit/integration/characterization tests | Sí | Parcial | No | Requiere evidencia operativa adicional |
| Evidencia técnica reproducible | Logs/salidas reproducibles en repo | Sí | Parcial | No | No reemplaza aprobación humana |
| Evidencia UAT asistida por IA | Reportes UAT con asistencia IA | Parcial | Parcial | No | No equivale a firma humana |
| Evidencia UAT humana | Ejecución UAT por áreas dueñas | Parcial | Sí | Parcial | Requiere formalización de aprobación |
| Evidencia externa/oficial | Validación de cámara/operador/tercero | Parcial | Parcial | Sí (según dominio) | Crítica para dominios bloqueados |
| Acta/aprobación formal | Aprobaciones Negocio/Compliance/Ops/Sec | Parcial | Sí | Sí (si no hay P0) | Debe estar trazada |
| Evidencia operacional productiva | Evidencia real de operación homologada | Parcial | Parcial | Sí | Requisito para salida de NO-GO |

Regla: evidencia técnica automatizada por sí sola no habilita producción.

## 5. Estados de trazabilidad

- **Cerrado trazablemente**: requisito + fuente + regla + código + test + evidencia + UAT/aprobación (si aplica) + dueño + estado + brecha cerrada.
- **Parcial**: existe base técnica o documental, falta al menos un elemento.
- **Débil**: avance fuerte en un eje, pero sin cierre normativo/UAT/external.
- **Bloqueado**: falta P0 normativo, externo, operativo o productivo.
- **No encontrado**: no se localiza evidencia suficiente en repositorio.

## 6. Matriz consolidada S1 por dominio y cámara

| ID | Dominio | Cámara | Requisito | Fuente normativa/documental | Regla funcional esperada | Código/ruta | Prueba automatizada | Evidencia | UAT/checklist | Dueño/RACI | Estado trazabilidad | Estado GO/NO-GO | Brecha principal | Criterio de salida |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| S1-01 | NACHA-M parser inbound | Ambas | Parseo inbound NACHA-M | `docs/architecture/incoming-nacha-processing-analysis-2026-04-23.md` | Parseo y persistencia consistente | `NachaParserService`, `IncomingNachaIngestionAppService` | `IncomingNacha*Tests` | Revalidaciones incoming en docs/operations | `docs/uat/nacha-records-acceptance-checklist.md` | Arquitectura + QA | Parcial | GO técnico controlado / UAT parcial / NO-GO prod | Falta cierre normativo firmable por regla/cámara | Matriz firmable regla↔norma↔test |
| S1-02 | NACHA-M builder/generación | Ambas | Generación 1/5/6/7/8/9 | `docs/auditoria-nacha-m-2026-04-17.md`, `docs/audits/nacha-record-level-normative-matrix-ach-cenit-current.md` | Builder con trazabilidad por campo y cámara | `NachaFileBuilder`/servicios de generación | `Nacha*Tests` / `AchTransactionNachaTests` | Matriz record-level + evidencias técnicas | `docs/uat/nacha-records-acceptance-checklist.md` | Arquitectura + QA | Parcial | GO técnico controlado / UAT parcial / NO-GO prod | Falta consolidación formal requisito→norma→test | Confirmación por cámara/flujo |
| S1-03 | Cargue inbound | Transversal | Carga y validación de archivo inbound | `docs/api/incoming-nacha-command-center-api-2026-04-24.md` | Carga controlada y trazable | `NachaUploadController`, `IncomingNachaIngestionAppService` | `IncomingNachaIngestionAppServiceTests` | `docs/operations/incoming-nacha-uat-controlled-evidence-2026-04-25.md` | `docs/uat/incoming-return-orphan-acceptance-checklist.md` | Operaciones + QA | Parcial | GO técnico controlado / UAT parcial / NO-GO prod | Falta acta operativa de cierre | Evidencia UAT humana formal |
| S1-04 | Clasificación inbound | Transversal | Clasificación funcional incoming | `docs/architecture/incoming-nacha-command-center-state-machine-2026-04-24.md` | Clasificación reproducible por estado/regla | `IncomingNachaFunctionalClassifier` | `IncomingNachaFunctionalClassifierTests` | Evidencia docs/operations incoming | `docs/uat/incoming-return-orphan-acceptance-checklist.md` | Arquitectura + QA | Parcial | GO técnico controlado / UAT parcial / NO-GO prod | Falta consolidación normativa por causal/cámara | Trazabilidad causal↔norma↔test |
| S1-05 | Prenotificaciones | CENIT (documentado) / Ambas (técnico) | Reglas de prenotificación | `docs/normativa/md/CENIT-Anexo-A-Causales-Devolucion.md` | Resolver prenotificación con causal aplicable | `PrenotificationHandler`, `AchPrenotificationPolicy` | `PrenotificationHandlerTests`, `IncomingNachaPrenotificationResolverTests` | Evidencia técnica backend en docs/audits/evidence | `docs/uat/nacha-records-acceptance-checklist.md` | Negocio ACH + QA | Parcial | GO técnico controlado / UAT parcial / NO-GO prod | Falta sign-off negocio por cámara/flujo | Aprobación funcional por cámara |
| S1-06 | Devoluciones | Ambas | Reglas de devolución por cámara | `docs/normativa/md/ACH-Colombia-V32.md`, `docs/normativa/md/CENIT-Anexo-A-Causales-Devolucion.md` | Validación causal/política por cámara | `AchReturnsService`, catálogos regulatorios | `RegulatoryCatalogSeederTests`, `ReportServicesDataQualityTests` | `docs/audits/cause-code-normative-matrix-ach-cenit-sta-current.md` | `docs/uat/cause-code-acceptance-checklist.md` | Negocio + Compliance | Parcial | GO técnico controlado / UAT parcial / NO-GO prod | Cierre regulatorio integral pendiente | Catálogo firmado por cámara |
| S1-07 | Return-of-return | Ambas | ROR controlado por cámara | `docs/normativa/md/ACH-Colombia-V32.md`, `docs/normativa/md/CENIT-Anexo-A-Causales-Devolucion.md`, docs dev de devoluciones | Evaluar y generar ROR con gating | Endpoints `/ach-returns/return-of-return/*`, `ReturnOfReturnOrchestrator` | pruebas ROR reportadas en docs | Evidencia técnica ROR (audit-mode/productivo) | `docs/uat/naming-returns-ror-acceptance-checklist.md` | Negocio + Arq + Ops + Compliance | Débil | GO técnico controlado / UAT GO controlado / NO-GO prod | Falta UAT E2E + aprobación normativa por cámara | Cierre UAT humano y validación por cámara |
| S1-08 | Ciclos ACH | ACH Colombia | Configuración de ciclos ACH | `docs/normativa/md/ACH-Colombia-V32.md` | Ciclos con vigencia y cutoff | `AchCyclesController`, `ClearingHouseCycleConfigService` | `ClearingHouseCycleConfigServiceTests` | Evidencias P0 CR1 | `docs/uat/cenit-cycles-liquidity-cud-acceptance-checklist.md` (referencia operativa) | Operaciones + QA | Parcial | GO técnico controlado / UAT parcial / NO-GO prod | Falta equivalencia formal norma-horarios | Validación formal por calendario/ciclo |
| S1-09 | Ciclos CENIT | CENIT | 5 ciclos y consistencia calendario | `docs/normativa/md/CENIT-DSP-152-Anexo-2.md` | Consistencia de ciclos CENIT | `CenitOperatingCalendarPolicy` | `CenitOperationalGovernanceTests`, `CenitCycleCalendarCharacterizationTests` | Evidencia targeted tests | `docs/uat/cenit-cycles-liquidity-cud-acceptance-checklist.md` | Operaciones + QA | Parcial | GO técnico controlado / UAT parcial / NO-GO prod | Falta evidencia operativa externa homologada | Validación externa/operativa |
| S1-10 | Neteo CENIT | CENIT | Neteo multilateral por ciclo | `docs/normativa/md/CENIT-DSP-152-Anexo-2.md`, `docs/audits/cenit-cycles-netting-liquidity-cud-matrix-current.md` | Neteo trazable y conciliable | `CenitNettingService`/modelos netting | `CenitOperationalGovernanceTests` | Evidencia shadow compare/ops | `docs/uat/cenit-cycles-liquidity-cud-acceptance-checklist.md` | Operaciones + Arquitectura | Bloqueado | NO-GO prod | Falta validación E2E real neteo→liquidación | Cierre E2E homologado |
| S1-11 | Liquidez/CUD | CENIT | Decisión liquidez y frontera CUD | `docs/audits/cenit-cycles-netting-liquidity-cud-matrix-current.md`, `docs/normativa/md/CENIT-DSP-152-Anexo-2.md` | Separar simulación y evidencia CUD real | `LiquidityOptimizationService` | `CenitOperationalGovernanceTests`/caracterización | Matriz liquidez/CUD actual | `docs/uat/cenit-cycles-liquidity-cud-acceptance-checklist.md` | Operaciones + Negocio | Bloqueado | NO-GO prod | Falta cierre operacional CUD/evidencia | Evidencia CUD operacional validada |
| S1-12 | Naming externo ACH/CENIT/STA | Ambas | Naming por cámara/flujo | `docs/audits/external-filename-normative-matrix-ach-cenit-sta-current.md` | Naming externo conforme por cámara | `ExternalFileNamePolicy`, `NachaExportController` | `ExternalFileNamePolicyPhase1Tests`, `NachaExportControllerTests` | Matriz naming vigente | `docs/uat/naming-returns-ror-acceptance-checklist.md` | Compliance + Arquitectura | Bloqueado | NO-GO prod | Cobertura parcial/hardcodes documentados | Remediación + firma normativa |
| S1-13 | Sobre digital/firma/cifrado | Transversal | Seguridad criptográfica NACHA-M | `docs/audits/digital-envelope-signature-certificate-matrix-current.md`, `docs/normativa/md/ACH-Colombia-V32.md` | Firma/cifrado con validación controlada | `NachaSecurityOperationService`, validadores firma | `DigitalEnvelopeSignatureFailCloseTests` | Matriz digital envelope + runbooks | `docs/uat/digital-envelope-certificate-acceptance-checklist.md` | Seguridad + Compliance | Bloqueado | NO-GO prod | Falta interoperabilidad oficial externa | Validación externa y acta |
| S1-14 | Certificados | Transversal | Gestión de certificados | `docs/audits/digital-envelope-signature-certificate-matrix-current.md` | Alta/rotación/validación auditables | `DigitalEnvelopeCertificatesController`, `CertificateManagementController` | `DigitalEnvelopeCertificateResolverTests` | Evidencias openbao/uat | `docs/uat/digital-envelope-certificate-acceptance-checklist.md` | Seguridad + Operaciones | Parcial | GO técnico controlado / UAT parcial / NO-GO prod | Falta hardening/sign-off final | Aprobación seguridad |
| S1-15 | Reportes operativos/auditoría | Transversal | Reportería trazable y exportable | `docs/audits/accounting-review-reconciliation-matrix-current.md`, `docs/reporting/ach-reporting-module-architecture.md` | Reportes con frontera no contable | `ReportsController`, servicios reportes | `ReportServicesDataQualityTests`, `ReportsControllerTests` | Evidencias CR4/reporting | `docs/uat/accounting-review-reconciliation-acceptance-checklist.md` | Negocio + QA | Parcial | GO técnico controlado / UAT parcial / NO-GO prod | Falta matriz firmada compliance/negocio | Cierre UAT humano |
| S1-16 | Command Center | Transversal | Control operativo inbound | `docs/api/incoming-nacha-command-center-api-2026-04-24.md` | Operación y trazabilidad de cola/estado | `IncomingNachaCommandCenter*` | `IncomingNachaCommandCenterServiceTests` | Revalidación ops incoming | `docs/uat/incoming-return-orphan-acceptance-checklist.md` | Operaciones + QA | Parcial | GO técnico controlado / UAT parcial / NO-GO prod | Falta acta contingencia real | Cierre operativo firmado |
| S1-17 | Observabilidad/auditoría | Transversal | KPIs, eventos, trazabilidad | `docs/operations/incoming-nacha-observability-runbook-2026-04-24.md` | Observabilidad reproducible | endpoints/servicios observabilidad incoming | suites IncomingNacha* | `docs/operations/incoming-nacha-observability-revalidation-2026-04-24.md` | `docs/uat/incoming-return-orphan-acceptance-checklist.md` | Operaciones + Arquitectura | Parcial | GO técnico controlado / UAT parcial / NO-GO prod | Falta evidencia KPI/SLO productiva | Evidencia operativa homologada |
| S1-18 | PaymentRail/capability registry | Transversal | Gobernanza de capacidades | docs operations phase7-10 | Registro de capacidades por rail | `PaymentRail*`, `PaymentRailCapabilityRegistryController` | `PaymentRail*Tests` | evidencias phase docs | UAT operativo phase docs | Arquitectura + QA | Parcial | GO técnico controlado / UAT parcial / NO-GO prod | Falta cierre gobernanza productiva completa | Cierre formal multi-riel |
| S1-19 | SPA operativa | Transversal | Consumo operativo UI | docs SPA/arquitectura frontend | Operación UI alineada a backend | `web/ach-interbank-ui` | suites Karma históricas (documentadas) | evidencia docs SPA | checklists UAT relacionados | Equipo SPA + QA | Parcial | GO técnico controlado / UAT parcial / NO-GO prod | Revalidación integrada final pendiente | UAT integrado firmado |
| S1-20 | UAT/runbooks/evidencia | Transversal | Cierre UAT y operación | `docs/audits/go-nogo-scorecard-funcional-normativo-2026-04-26.md` + checklists UAT | Plan y ejecución UAT con aprobación humana | artefactos docs/ops/uat | checklists y reportes UAT | docs/uat + docs/operations | todos los checklists vinculados | Operaciones + Compliance | Bloqueado | NO-GO prod | Pendientes críticos y firmas | Acta final sin pendientes críticos |

## 7. Vista por cámara

### 7.1 ACH Colombia

| Dominio | Fuente ACH | Código | Tests | Evidencia | Estado | Brecha |
|---|---|---|---|---|---|---|
| Parser/Builder NACHA-M | `docs/normativa/md/ACH-Colombia-V32.md` | `NachaParserService`, builders | `Nacha*Tests` | Matriz record-level + evidence docs | Parcial | Falta cierre firmable campo/flujo |
| Devoluciones | `ACH-Colombia-V32.md` | `AchReturnsService` | `RegulatoryCatalogSeederTests` | Matriz de causales current | Parcial | Falta cierre regulatorio firmado |
| ROR | `ACH-Colombia-V32.md` | `ReturnOfReturnOrchestrator` | pruebas ROR reportadas | evidencia técnica ROR | Débil | Falta UAT E2E humana |
| Ciclos ACH | `ACH-Colombia-V32.md` | `ClearingHouseCycleConfig*` | pruebas de config/ciclos | evidencia CR1 | Parcial | Falta equivalencia formal norma-horario |
| Naming externo | matrices naming current | `ExternalFileNamePolicy` | pruebas naming/export | matriz naming | Bloqueado | Falta cierre externo por flujo |
| Sobre/certificados | V32 + matrices digital envelope | servicios security/certificates | pruebas certificados/firma | runbooks + matrices | Bloqueado/Parcial | Falta validación externa oficial |

### 7.2 CENIT

| Dominio | Fuente CENIT | Código | Tests | Evidencia | Estado | Brecha |
|---|---|---|---|---|---|---|
| Causales | `CENIT-Anexo-A-Causales-Devolucion.md` | `AchReturnsService` + catálogos | `RegulatoryCatalogSeederTests` | matriz causales current | Parcial | Falta cierre por cámara/flujo firmado |
| Ciclos CENIT | `CENIT-DSP-152-Anexo-2.md` | `CenitOperatingCalendarPolicy` | `CenitOperationalGovernanceTests` | evidencia CR1/caracterización | Parcial | Falta evidencia externa homologada |
| Neteo | `CENIT-DSP-152-Anexo-2.md` + matriz CENIT | `CenitNettingService` | pruebas CENIT | evidencia shadow compare | Bloqueado | Falta E2E real |
| Liquidez/CUD boundary | matriz CENIT liquidez/CUD | `LiquidityOptimizationService` | pruebas CENIT | checklist CENIT UAT | Bloqueado | Falta evidencia CUD operacional |
| Naming CENIT | matriz naming current | `ExternalFileNamePolicy` | pruebas naming | matriz naming | Bloqueado | Cobertura parcial por flujo |
| ROR/devoluciones | Anexo A + DSP-152 | `ReturnOfReturn*` / `AchReturnsService` | pruebas ROR/devoluciones | evidencias docs/dev + uat | Débil/Parcial | Falta validación normativa/operativa final |

### 7.3 Transversal

- Reportes.
- Command center.
- Observabilidad.
- PaymentRail.
- UAT/runbooks.
- Certificados (gobernanza transversal de seguridad).

## 8. Brechas por tipo

### 8.1 Brechas de fuente normativa
- Falta cierre firmable por campo/flujo/cámara en varios dominios.
- En algunos dominios la fuente existe pero la regla no está cerrada formalmente contra evidencia final.

### 8.2 Brechas de código
- Existen hardcodes/cobertura parcial documentados en matrices vigentes para ciertos flujos (naming/returns/ROR).
- Módulos implementados sin enlace normativo final firmado por cámara en todos los casos.

### 8.3 Brechas de pruebas
- Hay pruebas, pero no siempre existe asociación formal requisito↔test↔norma por cámara.
- Falta diferenciación explícita ACH vs CENIT en ciertas trazas de aceptación.

### 8.4 Brechas de evidencia
- Evidencia técnica automatizada no reemplaza UAT humana.
- Evidencia externa/oficial pendiente en dominios críticos.

### 8.5 Brechas UAT
- Checklists existentes con pendientes para salida productiva.
- Falta acta/firma humana consolidada sin pendientes críticos.

### 8.6 Brechas de RACI
- RACI base existe; sign-off por dominio pendiente en varios casos.

### 8.7 Drift documental
- Existen matrices current e históricas; requiere política explícita current-vs-historical.

## 9. Riesgos P0/P1/P2

### P0
- S1-10 Neteo CENIT E2E.
- S1-11 Liquidez/CUD.
- S1-12 Naming externo ACH/CENIT/STA.
- S1-13 Sobre digital interoperabilidad externa.
- S1-20 UAT/runbooks/evidencia firmada.
- Cualquier dominio sin fuente normativa demostrada cuando aplica.

### P1
- Consolidación por cámara.
- Tests de caracterización ACH vs CENIT.
- Cierre UAT asistida vs humana.
- Gobernanza de evidencia.

### P2
- Limpieza documental.
- Dashboards de trazabilidad.
- Generación automática de matriz desde tests/docs.

## 10. Criterios de salida para cerrar punto 11

1. Todas las filas S1 con fuente localizada o brecha explícita.
2. Todas las filas S1 con código o “No aplica/No encontrado”.
3. Todas las filas S1 con prueba o brecha explícita.
4. Todas las filas S1 con evidencia clasificada.
5. Todas las filas S1 con cámara identificada.
6. ACH y CENIT no mezclados sin evidencia.
7. Checklists UAT referenciados.
8. Dueño/RACI referenciado.
9. Scorecard actualizado.
10. NO-GO productivo conservado si hay P0.

## 11. Veredicto de la matriz consolidada

- GO técnico matriz: **Sí, limitado/controlado**.
- GO UAT matriz: **Parcial/controlado**.
- GO productivo: **NO**.
- NO-GO productivo vigente.

## 12. Próximos pasos recomendados

1. `test(traceability): add ACH-vs-CENIT traceability assertions per domain`.
2. `docs(uat): add human-signoff evidence classification gates`.
3. `docs(governance): freeze current-vs-historical matrix policy`.
4. `docs(traceability): update scorecard once human UAT/evidence closes`.
