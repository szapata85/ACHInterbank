# Matriz vigente — Reportería, conciliación operativa y revisión contable de terceros

## 1. Propósito
Esta matriz consolida el estado actual de reportería ACH/CENIT/NACHA, trazabilidad, conciliación operativa, revisión contra contabilidad de terceros, diferencias, evidencia CUD, neteo/liquidez, huérfanas/no resueltas, ROR, rechazos total/parcial y límites explícitos de no contabilización.

Esta matriz no habilita producción.

## 2. Decisión arquitectónica no contable
**ACHInterbank NO:** contabiliza; genera asientos definitivos; genera ledger/journal/posting; integra APIs contables en línea; reemplaza core contable; reemplaza software contable de terceros; registra movimientos contables oficiales.

**ACHInterbank SÍ:** reporta; consolida información; genera evidencia; facilita conciliación operativa; permite revisión contra terceros; identifica diferencias; traza archivo/ciclo/cámara/transacción/causal/estado/evidencia; soporta auditoría.

## 3. Estado actual
- Base técnica de reportes: **sí**.
- Base técnica de trazabilidad: **sí**.
- Base conciliatoria: **parcial**.
- Revisión contable terceros: **parcial**.
- GO técnico: **sí, acotado/parcial**.
- GO UAT controlado: **sí, parcial**.
- NO-GO productivo: **sí**.
- Esta matriz no habilita producción.

## 4. Fuentes revisadas
- docs/dev/devoluciones-ach-auditoria-cenit-ach-colombia.md
- docs/audits/s1-matriz-maestra-trazable-funcional-normativa-2026-04-26.md
- docs/audits/go-nogo-scorecard-funcional-normativo-2026-04-26.md
- docs/audits/outbound-return-state-traceability-matrix-current.md
- docs/uat/outbound-return-state-traceability-acceptance-checklist.md
- docs/audits/incoming-return-e2e-orphan-matrix-current.md
- docs/uat/incoming-return-orphan-acceptance-checklist.md
- docs/audits/total-vs-partial-rejection-matrix-current.md
- docs/uat/rejection-total-partial-acceptance-checklist.md
- docs/audits/cenit-cycles-netting-liquidity-cud-matrix-current.md
- docs/uat/cenit-cycles-liquidity-cud-acceptance-checklist.md
- tests/Cfa.ACHInterbank.Tests/AccountingReviewBoundaryCharacterizationTests.cs
- ReportsController
- AchTraceabilityController
- AchTransactionReportService
- AchReturnRejectionReportService
- AchNachaCycleReportService
- AchAuditHistoryReportService
- AchTraceabilityService

## 5. Matriz de inventario actual
| Componente | Archivo/área | Existe | Estado | Observación |
|---|---|---|---|---|
| ReportsController | API | Sí | Implementado | Reportes operativos + PDF. |
| AchTraceabilityController | API | Sí | Implementado | Consulta transacción/rango. |
| AchTransactionReportService | Persistence/Reports | Sí | Implementado | Enviadas/recibidas. |
| AchReturnRejectionReportService | Persistence/Reports | Sí | Implementado | Devoluciones/rechazos. |
| AchNachaCycleReportService | Persistence/Reports | Sí | Implementado | NACHA files/ciclos. |
| AchAuditHistoryReportService | Persistence/Reports | Sí | Implementado | Auditoría/histórico. |
| Reportes PDF | Reports + QuestPDF | Sí | Implementado | Exportación PDF. |
| Reporte conciliación agregado | Reports/Reconciliation | Sí | Parcial | Diferencias agregadas. |
| Trazabilidad transacción/rango | Traceability | Sí | Implementado | Correlación operativa. |
| Neteo CENIT | ACH/CENIT | Sí | Parcial | Base técnica, cierre E2E pendiente. |
| Liquidez | ACH/CENIT | Sí | Parcial | Base técnica, validación E2E pendiente. |
| Evidencia CUD | Docs/operación | Sí | Solo documentado | CUD sin API runtime. |
| Huérfanas/no resueltas | Incoming retorno | Sí | Parcial | Cobertura operativa, falta reporte dedicado. |
| Manual audit-only | Incoming/ROR | Sí | Parcial | Evidencia operativa, no asiento. |
| ROR | Return-of-return | Sí | Parcial | Trazable/reportable, pendiente cierre integral. |
| Rechazo total/parcial | Rechazos | Sí | Parcial | Riesgo semántico parcial vs monto. |
| Estados conciliatorios formales | Reporting | No | No encontrado | No state machine formal completa. |
| Excel/CSV | Reporting | No | No encontrado | En módulo actual no formal. |
| APIs contables | API | No | No encontrado | No integración contable online. |
| Ledger/journal/posting | Dominio/API | No | No encontrado | Boundary no-contable vigente. |

## 6. Matriz de límites contables
| Elemento | Estado actual | Permitido | Observación |
|---|---|---|---|
| asiento contable definitivo | No encontrado | No permitido | No aplica al alcance. |
| ledger | No encontrado | No permitido | No aplica al alcance. |
| journal | No encontrado | No permitido | No aplica al alcance. |
| posting | No encontrado | No permitido | No aplica al alcance. |
| accounting API online | No encontrado | No permitido | No API contable en línea. |
| core contable | Externo | No permitido | No reemplazo. |
| sistema terceros | Externo | No permitido | Solo revisión contra terceros. |
| revisión contable terceros | Parcial | Permitido | Soporte por reportes/evidencia. |
| reporte conciliatorio | Parcial | Permitido | Operativo, no contable. |
| diferencia operacional | Parcial | Permitido | Brecha de formalización. |
| evidencia CUD | Solo documentado | Permitido | Evidencia operacional, no API CUD. |
| manual audit-only | Parcial | Permitido | No aplicación contable. |

## 7. Matriz de reportería disponible
| Reporte | Existe | Estado | Filtros | Exportación | Brecha |
|---|---|---|---|---|---|
| transacciones enviadas | Sí | Implementado | fecha/cámara/ciclo/estado/ref/banco/tipo | PDF | Excel/CSV no encontrado |
| transacciones recibidas | Sí | Implementado | fecha/cámara/ciclo/estado/ref/banco/tipo | PDF | Excel/CSV no encontrado |
| devoluciones | Sí | Implementado | fecha/causal/cámara/estado/ref | PDF | falta vista terceros dedicada |
| rechazos | Sí | Implementado | fecha/causal/cámara/estado/ref | PDF | parcial semántica rechazo |
| archivos NACHA | Sí | Implementado | fecha/cámara | PDF | sin CSV/Excel formal |
| ciclos | Sí | Implementado | fecha/cámara/nombre | PDF | sin CSV/Excel formal |
| conciliación agregada | Sí | Parcial | fecha/cámara/ciclo | PDF | falta conciliación formal terceros |
| auditoría | Sí | Implementado | user/action/entity/rango | PDF | falta cierre aprobación |
| histórico | Sí | Implementado | rango/tx/state/source | PDF | falta modelo estados conciliatorios |
| trazabilidad por transacción | Sí | Implementado | id transacción | N/A | no contable, operativo |
| trazabilidad por rango | Sí | Implementado | rango/estado/ciclo | N/A | no contable, operativo |
| ROR | Sí | Parcial | flujo/evaluación/archivo | Parcial | falta reporte conciliatorio dedicado |
| huérfanas/no resueltas | Sí | Parcial | incoming/orphan | No encontrado | falta reporte dedicado |
| manual audit-only | Sí | Parcial | command center/manual | No encontrado | falta reporte dedicado |
| neteo | Sí | Parcial | ciclo/cámara | No encontrado | falta cierre E2E |
| liquidez | Sí | Parcial | ciclo/cámara/decisión | No encontrado | falta cierre E2E |
| evidencia CUD | Sí | Solo documentado | operacional | No encontrado | sin runtime homologado |
| diferencias contra terceros | Sí | Parcial | conciliación | PDF parcial | falta modelo formal |

## 8. Matriz de conciliación operativa
| Dimensión conciliatoria | Existe | Estado | Fuente | Brecha | Próximo control |
|---|---|---|---|---|---|
| por archivo | Sí | Parcial | reportes/trazabilidad | falta modelo tercero | matriz terceros |
| por hash archivo | Sí | Parcial | ingestión/evidencia | falta estandarización | control hash formal |
| por cámara | Sí | Implementado | filtros reportes | cierre UAT | checklist UAT |
| por ciclo | Sí | Implementado | filtros reportes | cierre operativo | runbook |
| por fecha operacional | Sí | Parcial | fechas de proceso | normalización | control fecha |
| por transacción | Sí | Implementado | traceability | faltan estados conciliatorios | modelo estados |
| por valor | Sí | Parcial | totales/diferencias | falta tercero | third-party diff |
| por estado | Sí | Implementado | filtros estado | taxonomía conciliatoria | estados formales |
| por causal | Sí | Implementado | devoluciones/rechazos | cierre normativo | validación UAT |
| por devolución saliente | Sí | Parcial | returns | falta reporte dedicado tercero | reporte dedicado |
| por devolución entrante | Sí | Parcial | incoming | falta cobertura dedicada | reporte dedicado |
| por ROR | Sí | Parcial | ROR flows | falta vista conciliatoria | reporte dedicado |
| por huérfanas | Sí | Parcial | orphan matrix | falta exportación dedicada | reporte dedicado |
| por manual audit-only | Sí | Parcial | command center | riesgo confusión aplicado | etiqueta explícita |
| por neteo/liquidez | Sí | Parcial | CENIT services | cierre E2E pendiente | evidencia E2E |
| por evidencia CUD | Sí | Solo documentado | matriz CUD | no runtime formal | modelo evidencia |
| contra reportes de terceros | Sí | Parcial | revisión manual | falta contrato formal | checklist tercero |
| contra extracto externo | No | No encontrado | N/A | no modelado | definir alcance |
| diferencias | Sí | Parcial | reconciliation report | falta formalización | ThirdPartyDifferenceReport |
| aprobación/cierre | Sí | Parcial | UAT/ops | falta workflow formal | runbook + estados |

## 9. Matriz por flujo ACH
| Flujo | Contabiliza | Reporta | Concilia | Estado actual | Riesgo | Evidencia |
|---|---|---|---|---|---|---|
| outbound normal | NO | Sí | Sí | Implementado/Parcial | Medio | reportes transacciones |
| outbound return | NO | Sí | Sí | Parcial | Medio | reportes devoluciones |
| incoming return aplicada | NO | Sí | Sí | Parcial | Alto | matriz incoming |
| incoming return NotFound/Ambiguous | NO | Sí | Sí | Parcial | Alto | orphan matrix |
| manual resolution audit-only | NO | Sí | Sí | Parcial | Alto | command center/auditoría |
| ROR generado | NO | Sí | Sí | Parcial | Alto | ROR docs/tests |
| ROR recibido/aplicado si existe | NO | Sí | Sí | No encontrado | Alto | no encontrado explícito |
| rechazo total | NO | Sí | Sí | Parcial | Alto | rejection matrix |
| rechazo parcial | NO | Sí | Sí | Parcial | Alto | rejection matrix |
| devolución parcial por monto | NO | No | No | No encontrado | Alto | no modelada |
| liquidez insuficiente DXX-LIQ | NO | Sí | Sí | Parcial | Alto | CENIT liquidez |
| neteo CENIT | NO | Sí | Sí | Parcial | Alto | netting matrix |
| evidencia CUD | NO | Sí | Sí | Solo documentado | Crítico | CUD matrix |
| archivo aceptado/rechazado por cámara | NO | Sí | Sí | Parcial | Medio | reportes/rechazos |
| archivo firmado/cifrado | NO | Sí | Sí | Parcial | Medio | digital envelope matrix |
| archivo no procesado por error técnico | NO | Sí | Sí | Parcial | Alto | auditoría/eventos |

## 10. Matriz de estados reporting/conciliación
| Estado | Existe | Estado actual | Observación | Recomendación |
|---|---|---|---|---|
| ReportPending | No | No encontrado | No estado formal actual | Definir si aplica |
| ReportGenerated | No | No encontrado | No estado formal actual | Definir si aplica |
| ReportExported | No | No encontrado | No estado formal actual | Definir si aplica |
| ReconciliationPending | No | No encontrado | No estado formal actual | Definir |
| Reconciled | No | No encontrado | No estado formal actual | Definir |
| ReconciliationMismatch | No | No encontrado | No estado formal actual | Definir |
| ReconciliationManualReview | No | No encontrado | No estado formal actual | Definir |
| EvidencePending | No | No encontrado | No estado formal actual | Definir |
| EvidenceAttached | No | No encontrado | No estado formal actual | Definir |
| EvidenceRejected | No | No encontrado | No estado formal actual | Definir |
| CudEvidencePending | No | No encontrado | No estado formal actual | Definir |
| CudEvidenceConfirmed | No | No encontrado | No estado formal actual | Definir |
| CudEvidenceRejected | No | No encontrado | No estado formal actual | Definir |
| ThirdPartyAccountingReviewPending | No | No encontrado | No estado formal actual | Definir |
| ThirdPartyAccountingReviewed | No | No encontrado | No estado formal actual | Definir |
| ThirdPartyAccountingDifference | No | No encontrado | No estado formal actual | Definir |

Nota: estos no deben ser estados contables, sino de revisión/reporting.

## 11. Matriz de idempotencia de reportes/conciliación
| Control | Existe | Estado | Riesgo | Recomendación |
|---|---|---|---|---|
| idempotencia reporte por rango | No formal | Parcial | duplicidad de ejecución | contrato idempotente |
| idempotencia reporte por archivo | No formal | Parcial | reprocesos | clave funcional |
| idempotencia conciliación por ciclo | No formal | Parcial | doble conciliación | lock lógico |
| idempotencia conciliación por cámara | No formal | Parcial | inconsistencia | estrategia por cámara |
| duplicate file | Sí (ACH) | Implementado ACH | propagación a reporting | extender a reportes |
| reprocess | Sí (ACH) | Parcial | repetición de reportes | control ejecución |
| incoming duplicado | Sí (ACH) | Implementado ACH | lectura duplicada | enlazar reporting |
| ROR duplicado | Sí (parcial) | Parcial | doble evidencia | idempotencia ROR reportes |
| manual audit-only | Sí | Parcial | confusión aplicado | etiqueta explícita |
| evidencia CUD duplicada | No formal | No encontrado | inconsistencia documental | índice/clave evidencia |
| exportación repetida | No formal | No encontrado | evidencia duplicada | control exportación |
| índice único | Parcial | Parcial | duplicidad | definir por artefacto |
| test | Parcial | Parcial | regresión | pruebas específicas |

Diferenciación: idempotencia ACH existente (parcial/implementada en flujos); idempotencia de reportería no formal; idempotencia conciliatoria no formal.

## 12. Matriz neteo, liquidez y CUD
| Elemento | Reporta | Concilia | Contabiliza | Estado | Brecha |
|---|---|---|---|---|---|
| neteo CENIT | Sí | Sí | NO | Parcial | cierre E2E pendiente |
| posición neta | Sí | Sí | NO | Parcial | evidencia externa pendiente |
| liquidez Processed | Sí | Sí | NO | Parcial | homologación operativa |
| liquidez Deferred | Sí | Sí | NO | Parcial | gobernanza operativa |
| liquidez Rejected | Sí | Sí | NO | Parcial | gobernanza operativa |
| DXX-LIQ | Sí | Sí | NO | Parcial | evitar interpretación externa |
| evidencia CUD manual | Sí | Sí | NO | Solo documentado | runtime pendiente |
| evidencia CUD archivo/reporte | Sí | Sí | NO | Solo documentado | runtime pendiente |
| cierre ciclo operacional | Sí | Sí | NO | Parcial | runbook + UAT |
| cierre ciclo conciliatorio | Sí | Sí | NO | No encontrado | modelo objetivo pendiente |

Aclaraciones: no API CUD; no contabilización CUD; CUD es evidencia operacional; cierre E2E sigue pendiente.

## 13. Matriz de trazabilidad
| Origen/Dato | Trazable hoy | Estado | Brecha |
|---|---|---|---|
| archivo NACHA | Sí | Implementado | granularidad documental |
| hash archivo | Sí | Parcial | estandarización |
| cámara | Sí | Implementado | cierre operativo |
| ciclo | Sí | Implementado | cierre operativo |
| fecha operacional | Sí | Parcial | normalización |
| transacción ACH | Sí | Implementado | ninguna crítica técnica |
| retorno saliente | Sí | Parcial | reporte dedicado |
| retorno entrante | Sí | Parcial | reporte dedicado |
| ROR | Sí | Parcial | reporte conciliatorio dedicado |
| causal | Sí | Implementado | homologación por flujo |
| estado | Sí | Implementado | estados conciliatorios faltantes |
| evento de estado | Sí | Implementado | enriquecimiento |
| evento de procesamiento | Sí | Parcial | cobertura uniforme |
| reporte | Sí | Implementado | exportación avanzada |
| conciliación | Sí | Parcial | formalización tercero |
| evidencia CUD | Sí | Solo documentado | runtime |
| usuario/actor | Sí | Parcial | workflow aprobación |
| timestamp | Sí | Implementado | zona horaria operativa |
| exportación | Sí | Parcial | Excel/CSV formal |

## 14. Matriz de auditoría
| Evento/auditoría | Existe | Estado | Brecha |
|---|---|---|---|
| generación reporte | Sí | Parcial | normalización de evento |
| exportación PDF | Sí | Implementado | exportaciones adicionales |
| conciliación | Sí | Parcial | estados formales |
| diferencia | Sí | Parcial | formalización tercero |
| ajuste manual | Sí | Parcial | control aprobación |
| evidencia CUD | Sí | Solo documentado | runtime |
| aprobación | Sí | Parcial | flujo formal |
| actor | Sí | Implementado | segregación completa |
| timestamp | Sí | Implementado | consistencia TZ |
| payload before/after | Sí | Parcial | estandarización |
| hash/correlationId | Sí | Parcial | unificación |

## 15. Matriz de pruebas actuales
| Suite/Test | Cubre | Estado | Brecha |
|---|---|---|---|
| AccountingReviewBoundaryCharacterizationTests | boundary no-contable | Implementado | ampliar idempotencia reportes |
| ReportsControllerTests | endpoints reportes | Implementado | cobertura terceros |
| ReportServicesDataQualityTests | calidad datos reportes | Implementado | diferencias terceros |
| AchTraceabilityServiceTests | trazabilidad | Implementado | exportación dedicada |
| tests de returns | devoluciones | Implementado | reportes conciliatorios dedicados |
| tests de ROR | return-of-return | Implementado | cobertura conciliatoria |
| tests huérfanas/manual audit-only | incoming/orphan/manual | Implementado | evitar confusión aplicada |
| tests rechazo total/parcial | rechazos | Implementado | frontera parcial por monto |
| tests neteo/liquidez | CENIT operativo | Parcial | cierre E2E |
| tests CUD/evidencia | evidencia CUD | Parcial | runtime conciliable |
| tests exportación | PDF/reportes | Parcial | Excel/CSV |
| tests idempotencia reportería/conciliación | reportería/conciliación | No encontrado | agregar caracterización |

## 16. Brechas P0/P1/P2
**P0**
- Boundary no-contable debe quedar documentado y protegido.
- Falta reporte conciliatorio de terceros formal.
- Falta reporte de diferencias contra tercero/extracto.
- Falta estados conciliatorios formales.
- Falta idempotencia de reportes/conciliación.
- Falta CUD evidencia runtime conciliable.
- Falta cierre E2E neteo/liquidez/CUD.
- Riesgo de confundir manual audit-only con aplicado.
- Riesgo de confundir RejectedPartial con devolución parcial por monto.
- NO-GO productivo vigente.

**P1**
- Checklist UAT reportería/conciliación pendiente.
- Runbook operativo pendiente.
- Excel/CSV pendiente si se requiere.
- Reportes dedicados para huérfanas/manual/ROR/CUD.
- Pruebas de idempotencia reportería/conciliación.
- Evidencia de aprobación/cierre conciliatorio.

**P2**
- Dashboard conciliatorio.
- Alertas de diferencias.
- Conciliación asistida.
- Exportación avanzada.
- Parametrización de reportes.
- Integración futura por archivo/exportación, no API online.

## 17. Modelo objetivo
El sistema debe evolucionar a:
- AccountingReviewReport;
- ReconciliationEvidence;
- ThirdPartyDifferenceReport;
- CudOperationalEvidence;
- ReconciliationExport;
- ReconciliationReviewStatus.

Aclaración: no son asientos; no son ledger; no son journal; no son posting; no sustituyen contabilidad de terceros.

## 18. Criterios de salida NO-GO del punto 10
1. Boundary no-contable documentado.  
2. Tests no-contables pasan.  
3. Matriz reportes/conciliación aprobada.  
4. Checklist UAT creado.  
5. Runbook conciliación creado.  
6. Reporte conciliatorio mínimo definido.  
7. Diferencias contra terceros definidas.  
8. Huérfanas/no resueltas reportables.  
9. Manual audit-only reportable.  
10. ROR reportable.  
11. Rechazo total/parcial reportable.  
12. Neteo/liquidez reportable.  
13. Evidencia CUD reportable.  
14. Estados conciliatorios definidos.  
15. Idempotencia reportería definida.  
16. Idempotencia conciliación definida.  
17. Exportación requerida definida.  
18. Evidencia UAT adjunta.  
19. Aprobación negocio.  
20. Aprobación operaciones.  
21. Aprobación riesgo/compliance.  
22. Aprobación tecnología.  
23. Scorecard actualizado.

## 19. Recomendación
- Opción C sigue recomendada.
- Ya se ejecutó primer commit técnico de tests.
- Siguiente recomendado: `docs(uat): add accounting-review reconciliation checklist`.
- Después: `docs(ops): add reconciliation operations runbook`.
- Luego, si se aprueba: `feat(reporting): add accounting-review report model`.

No se recomienda integración contable online.

## 20. Decisión vigente
- GO técnico actual: **sí, acotado/parcial**.
- GO UAT controlado: **sí, parcial**.
- NO-GO productivo: **sí**.
- El sistema no contabiliza.
- Esta matriz no habilita producción.

- Referencia checklist UAT punto 10: `docs/uat/accounting-review-reconciliation-acceptance-checklist.md`.

- Referencia runbook operativo conciliación punto 10: `docs/ops/reconciliation-operations-runbook.md`.
