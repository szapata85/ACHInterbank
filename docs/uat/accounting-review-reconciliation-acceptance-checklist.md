# Checklist UAT — Reportería, conciliación operativa y revisión contable de terceros

## 1. Propósito
Validar en UAT reportería ACH/CENIT/NACHA, trazabilidad, conciliación operativa, revisión contra contabilidad de terceros, diferencias, evidencia CUD, neteo/liquidez, huérfanas/no resueltas, manual audit-only, ROR, rechazo total/parcial, exportaciones, auditoría y aprobaciones.

Este checklist no habilita producción.

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
- Este checklist no habilita producción.

## 4. Fuentes
- `docs/audits/accounting-review-reconciliation-matrix-current.md`
- `tests/Cfa.ACHInterbank.Tests/AccountingReviewBoundaryCharacterizationTests.cs`
- `ReportsController`
- `AchTraceabilityController`
- `AchTransactionReportService`
- `AchReturnRejectionReportService`
- `AchNachaCycleReportService`
- `AchAuditHistoryReportService`
- `AchTraceabilityService`
- `docs/audits/go-nogo-scorecard-funcional-normativo-2026-04-26.md`
- `docs/audits/s1-matriz-maestra-trazable-funcional-normativa-2026-04-26.md`

## 5. Alcance UAT
Validar: reportes transacciones enviadas/recibidas, devoluciones, rechazos, archivos NACHA, ciclos, conciliación agregada, auditoría/histórico, trazabilidad por transacción/rango, huérfanas/no resueltas, manual audit-only, ROR, neteo, liquidez, evidencia CUD, diferencias contra terceros, exportaciones PDF y brecha Excel/CSV.

## 6. Checklist de frontera no-contable
| ID | Control | Resultado esperado | Evidencia | Estado | Observación |
|---|---|---|---|---|---|
| UAT-ACR-001 | confirmar que no existe contabilización | No contabiliza | consulta funcional + evidencia | Pendiente | boundary no-contable |
| UAT-ACR-002 | confirmar que no hay asiento contable definitivo | No asientos definitivos | checklist + matriz | Pendiente | no contable |
| UAT-ACR-003 | confirmar que no hay ledger/journal/posting | No encontrado en superficie productiva | tests frontera + revisión | Pendiente | no aplica |
| UAT-ACR-004 | confirmar que no hay API contable online | No API contable online | inventario API | Pendiente | no integra contabilidad |
| UAT-ACR-005 | confirmar que reportes son soporte de revisión de terceros | Sí, soporte operativo | reportes + acta UAT | Pendiente | no asiento |
| UAT-ACR-006 | confirmar que conciliación es operativa, no contable | Sí, conciliación operativa | reporte conciliación | Pendiente | parcial |
| UAT-ACR-007 | confirmar que eventos de estado ACH no son asientos | Eventos ACH ≠ asiento | trazabilidad/histórico | Pendiente | semántica |
| UAT-ACR-008 | confirmar que evidencia CUD no es liquidación contable | CUD = evidencia operacional | matriz CUD | Pendiente | sin API CUD |
| UAT-ACR-009 | confirmar que manual audit-only no aplica contablemente | audit-only no aplica contable | caso UAT manual | Pendiente | no aplicado contable |
| UAT-ACR-010 | confirmar que RejectedPartial no significa devolución parcial por monto | RejectedPartial por registro | caso rechazo parcial | Pendiente | evitar confusión |

## 7. Checklist de reportes operativos
| ID | Reporte | Filtros mínimos | Resultado esperado | Exportación | Evidencia | Estado |
|---|---|---|---|---|---|---|
| UAT-REP-001 | transacciones enviadas | fecha/cámara/ciclo/estado/ref/tipo | conteos/totales/valores consistentes | PDF | captura + PDF | Pendiente |
| UAT-REP-002 | transacciones recibidas | fecha/cámara/ciclo/estado/ref/tipo | conteos/totales/valores consistentes | PDF | captura + PDF | Pendiente |
| UAT-REP-003 | devoluciones | fecha/causal/cámara/estado/ref | causal y valores coherentes | PDF | reporte + evidencia | Pendiente |
| UAT-REP-004 | rechazos | fecha/causal/cámara/estado/ref | distinción rechazo/devolución | PDF | reporte + evidencia | Pendiente |
| UAT-REP-005 | archivos NACHA | fecha/cámara | archivo y métricas visibles | PDF | reporte + PDF | Pendiente |
| UAT-REP-006 | ciclos | fecha/cámara/nombre | ciclos y conteos visibles | PDF | reporte + PDF | Pendiente |
| UAT-REP-007 | conciliación agregada | fecha/cámara/ciclo | diferencias agregadas visibles | PDF | reporte conciliación | Pendiente |
| UAT-REP-008 | auditoría | user/action/entity/rango | trazas auditables | PDF | reporte auditoría | Pendiente |
| UAT-REP-009 | histórico | rango/transacción/state/source | timeline consistente | PDF | reporte histórico | Pendiente |
| UAT-REP-010 | trazabilidad transacción | transactionId | correlación operativa | N/A | respuesta endpoint | Pendiente |
| UAT-REP-011 | trazabilidad rango | rango/estado/ciclo | correlación por rango | N/A | respuesta endpoint | Pendiente |

Excel/CSV: pendiente si negocio lo requiere en este módulo.

## 8. Checklist de conciliación operativa
| ID | Dimensión | Resultado esperado | Evidencia | Estado | Brecha |
|---|---|---|---|---|---|
| UAT-CON-001 | archivo | conciliación por archivo | reporte/traza | Pendiente | formalización tercero |
| UAT-CON-002 | hash archivo | hash visible/trazable | evidencia hash | Pendiente | normalización |
| UAT-CON-003 | cámara | conciliación por cámara | filtros reporte | Pendiente | cierre UAT |
| UAT-CON-004 | ciclo | conciliación por ciclo | filtros reporte | Pendiente | cierre operativo |
| UAT-CON-005 | fecha operacional | conciliación por fecha | reporte | Pendiente | homologación |
| UAT-CON-006 | transacción | conciliación por transacción | traza | Pendiente | estados formales |
| UAT-CON-007 | valor | diferencias de valor visibles | reconciliación | Pendiente | terceros |
| UAT-CON-008 | estado | diferencias por estado | filtros | Pendiente | taxonomía |
| UAT-CON-009 | causal | diferencias por causal | reportes devolución/rechazo | Pendiente | validación |
| UAT-CON-010 | devolución saliente | conciliable | reporte devoluciones | Pendiente | dedicado |
| UAT-CON-011 | devolución entrante | conciliable | incoming/orphan | Pendiente | dedicado |
| UAT-CON-012 | ROR | conciliable | evidencia ROR | Pendiente | dedicado |
| UAT-CON-013 | huérfanas | visibles/no resueltas | orphan matrix | Pendiente | exportación |
| UAT-CON-014 | manual audit-only | visible audit-only | command center + evidencia | Pendiente | evitar confusión |
| UAT-CON-015 | neteo/liquidez | conciliable parcial | matriz CENIT | Pendiente | E2E pendiente |
| UAT-CON-016 | evidencia CUD | trazable operacional | evidencia CUD | Pendiente | runtime pendiente |
| UAT-CON-017 | terceros | revisión contra tercero | acta comparación | Pendiente | contrato formal |
| UAT-CON-018 | diferencias | diferencias visibles | reporte conciliación | Pendiente | formalización |
| UAT-CON-019 | aprobación/cierre | aprobación auditable | acta/firma | Pendiente | workflow |

## 9. Checklist por flujo ACH
| ID | Flujo | Contabiliza | Debe reportar | Debe conciliar | Resultado esperado | Evidencia | Estado |
|---|---|---|---|---|---|---|---|
| UAT-FLW-001 | outbound normal | NO | Sí | Sí | visible y conciliable | reporte tx | Pendiente |
| UAT-FLW-002 | outbound return | NO | Sí | Sí | visible y conciliable | reporte devoluciones | Pendiente |
| UAT-FLW-003 | incoming return aplicada | NO | Sí | Sí | trazable y conciliable | incoming trace | Pendiente |
| UAT-FLW-004 | incoming return NotFound/Ambiguous | NO | Sí | Sí | no resuelto/manual review | orphan evidence | Pendiente |
| UAT-FLW-005 | manual resolution audit-only | NO | Sí | Sí | audit-only reportable | manual evidence | Pendiente |
| UAT-FLW-006 | ROR generado | NO | Sí | Sí | reportable/trazable | evidencia ROR | Pendiente |
| UAT-FLW-007 | ROR recibido/aplicado si existe | NO | Sí | Sí | brecha explícita si no existe | acta UAT | Pendiente |
| UAT-FLW-008 | rechazo total | NO | Sí | Sí | rechazo sin aplicación contable | reporte rechazo | Pendiente |
| UAT-FLW-009 | rechazo parcial | NO | Sí | Sí | por registro, no por monto | reporte rechazo parcial | Pendiente |
| UAT-FLW-010 | devolución parcial por monto | NO | No | No | no modelada actual | evidencia no encontrado | Pendiente |
| UAT-FLW-011 | liquidez insuficiente DXX-LIQ | NO | Sí | Sí | marcado interno | evidencia liquidez | Pendiente |
| UAT-FLW-012 | neteo CENIT | NO | Sí | Sí | reportable parcial | evidencia neteo | Pendiente |
| UAT-FLW-013 | evidencia CUD | NO | Sí | Sí | evidencia operacional | soporte CUD | Pendiente |
| UAT-FLW-014 | archivo aceptado/rechazado cámara | NO | Sí | Sí | estado visible | reporte archivo/rechazo | Pendiente |
| UAT-FLW-015 | archivo firmado/cifrado | NO | Sí | Sí | evidencia de operación | evidencia sobre digital | Pendiente |
| UAT-FLW-016 | archivo no procesado por error técnico | NO | Sí | Sí | incidente trazable | auditoría/evento | Pendiente |

## 10. Checklist huérfanas/no resueltas/manual audit-only
- NotFound se reporta como no resuelto.
- Ambiguous se reporta como revisión manual.
- `manualReviewRequired=true` visible/reportable si aplica.
- `candidateTransactionIds` preservado si aplica.
- manual audit-only no cambia estado contable.
- manual audit-only no genera asiento.
- manual audit-only no se reporta como aplicado.
- evidencia JSON permanece trazable.
- actor/fecha/comentario auditables si aplica.
- no se duplica evidencia por reproceso.

## 11. Checklist ROR
- ROR generado es reportable.
- ROR evaluado es trazable.
- ROR con archivo generado es reportable.
- ROR no genera asiento.
- ROR no genera posting.
- ROR correlacionable con transacción original.
- ROR correlacionable con causal.
- ROR correlacionable con archivo/ciclo/cámara si aplica.
- ROR duplicado no debe generar doble evidencia conciliatoria.
- brechas de ROR recibido/aplicado quedan explícitas si no existen.

## 12. Checklist rechazo total/parcial
- rechazo total se reporta como lote/archivo sin aplicación.
- rechazo total no genera asiento.
- rechazo parcial se reporta por registros aceptados/rechazados.
- rechazo parcial no significa devolución parcial por monto.
- registros rechazados tienen causal/evidencia.
- registros aplicados se distinguen de rechazados.
- diferencias se revisan por cantidad/valor/causal.
- exportación/evidencia UAT adjunta.

## 13. Checklist neteo, liquidez y CUD
- neteo CENIT reportable.
- posición neta reportable.
- liquidez Processed/Deferred/Rejected reportable.
- DXX-LIQ marcada como interna.
- CUD no tiene API.
- CUD no contabiliza.
- CUD como evidencia operacional.
- evidencia CUD manual trazable si aplica.
- evidencia CUD por archivo/reporte trazable si aplica.
- conciliación CUD vs neteo/liquidez parcial/pendiente si no existe.
- cierre E2E neteo/liquidez/CUD se mantiene NO-GO si no está cerrado.

## 14. Checklist de diferencias contra terceros
- diferencia por archivo.
- diferencia por ciclo.
- diferencia por cámara.
- diferencia por transacción.
- diferencia por valor.
- diferencia por estado.
- diferencia por causal.
- diferencia por ROR.
- diferencia por huérfanas/no resueltas.
- diferencia por CUD/evidencia.
- reporte de diferencias exportable si existe.
- si no existe reporte formal de terceros, marcar pendiente.

## 15. Checklist de trazabilidad
Validar trazabilidad desde/hacia: archivo NACHA, hash archivo, cámara, ciclo, fecha operacional, transacción ACH, retorno saliente, retorno entrante, ROR, causal, estado, evento de estado, evento de procesamiento, reporte, conciliación, evidencia CUD, usuario/actor, timestamp, exportación.

## 16. Checklist de auditoría
Validar auditoría de: generación reporte, exportación PDF, conciliación, diferencia, ajuste manual, evidencia CUD, aprobación, actor, timestamp, payload before/after si aplica, hash/correlationId si aplica, no exposición de datos sensibles innecesarios.

## 17. Checklist de idempotencia reportería/conciliación
Validar o dejar pendiente explícito: idempotencia reporte por rango/archivo, conciliación por ciclo/cámara, duplicate file, reprocess, incoming duplicado, ROR duplicado, manual audit-only, evidencia CUD duplicada, exportación repetida, índice único si aplica, test asociado.

Diferenciar: idempotencia ACH existente; idempotencia reportería no formal; idempotencia conciliatoria no formal.

## 18. Checklist exportaciones
- PDF disponible para reportes actuales.
- PDF contiene filtros aplicados.
- PDF contiene totales.
- PDF contiene fecha/hora generación.
- PDF contiene usuario/actor si aplica.
- PDF contiene cámara/ciclo si aplica.
- Excel/CSV existe o queda pendiente.
- exportación repetida no altera datos.
- exportación no genera asiento.
- exportación no cambia estado contable.

## 19. Evidencia UAT requerida
- capturas/reportes transacciones enviadas.
- capturas/reportes transacciones recibidas.
- reportes devoluciones.
- reportes rechazos.
- reportes archivos NACHA.
- reportes ciclos.
- reporte conciliación agregada.
- reporte auditoría/histórico.
- trazabilidad transacción.
- trazabilidad rango.
- casos NotFound/Ambiguous.
- caso manual audit-only.
- caso ROR.
- caso rechazo total.
- caso rechazo parcial.
- caso DXX-LIQ si aplica.
- evidencia neteo/liquidez.
- evidencia CUD operacional si aplica.
- reporte de diferencias.
- exportaciones PDF.
- acta UAT.
- aprobación negocio.
- aprobación operaciones.
- aprobación riesgo/compliance.
- aprobación tecnología.

## 20. Criterios de salida NO-GO del punto 10
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

## 21. Riesgos residuales
- confusión entre reporte y contabilización;
- confusión entre evento ACH y asiento;
- confusión manual audit-only vs aplicado;
- confusión RejectedPartial vs devolución parcial por monto;
- falta reporte formal de terceros;
- falta estados conciliatorios;
- falta idempotencia reportería/conciliación;
- falta Excel/CSV si negocio lo exige;
- CUD sin API y sin cierre E2E runtime;
- neteo/liquidez sin cierre E2E productivo;
- NO-GO productivo vigente.

## 22. Decisión vigente
- GO técnico actual: **sí, acotado/parcial**.
- GO UAT controlado: **sí, parcial**.
- NO-GO productivo: **sí**.
- El sistema no contabiliza.
- Este checklist no habilita producción.
- Próximo recomendado: `docs(ops): add reconciliation operations runbook`.

- Referencia runbook operativo conciliación punto 10: `docs/ops/reconciliation-operations-runbook.md`.


## Prerrequisitos técnicos disponibles para UAT

- [x] Endpoint backend de exportación disponible.
- [x] Formato PDF disponible.
- [x] Formato CSV disponible.
- [x] Formato Excel/XLSX disponible.
- [x] Contenido visible en español.
- [x] Frontera no-contable visible.
- [x] Población parcial desde servicios existentes.
- [x] Pruebas automatizadas backend.
- [ ] Evidencia UAT formal adjunta.
- [ ] Validación negocio.
- [ ] Validación operaciones.
- [ ] Validación riesgo/compliance.
- [ ] Validación tecnología.
- [ ] Cierre E2E CUD/neteo/liquidez.
- [ ] Scorecard productivo aprobado.

## Escenarios UAT específicos punto 10

1. Descargar PDF con filtros.
2. Descargar CSV con filtros.
3. Descargar XLSX con filtros.
4. Validar español en contenido visible.
5. Validar que no afirma contabilización/asientos.
6. Validar que no existe API CUD.
7. Validar warning CUD cuando no hay evidencia runtime.
8. Validar returns/rejections.
9. Validar ROR.
10. Validar diferencias.
11. Validar evidencia NACHA/auditoría.
12. Validar manual audit-only.
13. Validar huérfanas cuando la fuente exista.
14. Validar que ACHInterbank soporta revisión de terceros y no contabiliza.


## 22. Evidencia UAT asistida por IA (punto 10)
- [x] Evidencia UAT automatizada/asistida por IA disponible (`tests/Cfa.ACHInterbank.Tests/AccountingReviewUatEvidenceHarnessTests.cs`).
- [x] Reporte de ejecución UAT asistida por IA disponible (`docs/uat/accounting-review-ai-assisted-uat-execution-report.md`).
- [ ] Aprobación humana de GO UAT formal pendiente.
- [ ] Acta UAT formal pendiente.
- [ ] GO productivo pendiente (NO-GO vigente).

Referencia de trazabilidad consolidada: para trazabilidad requisito→norma→código→prueba→evidencia por cámara, ver `docs/audits/s1-requirement-norm-code-test-evidence-closure-matrix-current.md`. Esta referencia no cambia NO-GO productivo.


Referencia de compuertas de evidencia y aprobación humana: para clasificación de evidencia, GO UAT formal y aprobación humana, ver `docs/uat/human-signoff-evidence-classification-gates.md`. Esta referencia no cambia NO-GO productivo.
