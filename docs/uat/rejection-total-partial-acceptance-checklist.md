# Checklist UAT — Rechazo total vs rechazo parcial vs devolución parcial

## 1. Propósito
Este checklist valida en UAT:
- diferencia entre `Accepted`, `RejectedTotal` y `RejectedPartial`;
- diferencia entre rechazo parcial por registro y devolución parcial por monto;
- diferencia entre rechazo, devolución aplicada, orphan y resolución manual audit-only;
- evidencia esperada por cada caso;
- impacto en estado, eventos, auditoría, ROR, contabilidad y conciliación;
- criterios de salida de NO-GO.

## 2. Estado actual
- **GO técnico:** sí.
- **GO UAT controlado:** sí.
- **NO-GO productivo:** sí.
- Ya existe matriz total-vs-partial.
- Ya existen tests de caracterización.
- El checklist no habilita producción.
- Falta ejecución UAT con evidencia y firmas.

## 3. Fuentes de referencia
- `docs/audits/total-vs-partial-rejection-matrix-current.md`
- `docs/audits/incoming-return-e2e-orphan-matrix-current.md`
- `docs/uat/incoming-return-orphan-acceptance-checklist.md`
- `docs/audits/cause-code-normative-matrix-ach-cenit-sta-current.md`
- `docs/uat/cause-code-acceptance-checklist.md`
- `docs/uat/nacha-records-acceptance-checklist.md`
- `docs/uat/outbound-return-state-traceability-acceptance-checklist.md`
- `tests/Cfa.ACHInterbank.Tests/RejectionTotalVsPartialCharacterizationTests.cs`
- tests de `AchCauseCodePolicy`.
- tests de incoming/orphan/manual resolution.

## 4. Alcance UAT
Cobertura mínima:
- Ruta A incoming: `AchIncomingReturnIngestionService`.
- Ruta B incoming: `IncomingNachaPostParseProcessor`.
- `Accepted`.
- `RejectedTotal`.
- `RejectedPartial`.
- `NotFound`.
- `Ambiguous`.
- `NoResuelto`.
- `ManualResolvedAuditOnly`.
- Duplicate incoming file.
- Reprocess.
- Outbound `ReturnFileGenerated`.
- ROR.
- Contabilidad/conciliación.
- `FileRejectTotal`/`FileRejectPartial`.
- `Dxx`/`Ixxx`/`Rxx`/`DEVxx`.

## 5. Definiciones de aceptación

| Concepto | Definición UAT | Resultado esperado | Evidencia obligatoria | Estado UAT |
|---|---|---|---|---|
| Accepted | Todos los registros válidos aplicados. | Aplicación completa sin rechazos. | decision, conteos, eventos, estado final. | Pendiente |
| RejectedTotal | 0 aplicaciones en archivo/lote. | Sin cambios de estado ni state events. | decision=RejectedTotal, `UpdatedTransactionCount=0`, failures/audit. | Pendiente |
| RejectedPartial | Mezcla por registros (aplicados+rechazados). | Solo aplicados cambian estado/evento. | decision, conteos aplicados/rechazados, failures por rechazado. | Pendiente |
| IncomingReturnApplied | Devolución entrante aplicada con linaje. | Cambio de estado + evento aplicable. | state event + payload/evidence. | Pendiente |
| AmountPartialReturn | Devolución parcial por monto. | **No modelado** en fase actual. | evidencia de no-campo/no-evento/no-estado específico. | Fuera de alcance |
| NotFound | Orphan sin match. | No estado, no state event, manual review. | processing event + `IncomingReturnUnresolved`. | Pendiente |
| Ambiguous | Orphan con múltiples candidatos. | No estado, manual review, candidatos preservados. | evidence con `candidateTransactionIds`. | Pendiente |
| NoResuelto | Estado unresolved pendiente. | Sin aplicación automática. | estado/link/eventos de seguimiento. | Pendiente |
| ManualResolvedAuditOnly | Cierre manual audit-only. | No applied return, no estado, no contabilidad. | `OrphanManualResolution` + `IncomingReturnManualResolved`. | Pendiente |
| FileRejectTotal | Rechazo técnico total. | No aplicación transaccional. | código Dxx + evidencia rechazo. | Pendiente |
| FileRejectPartial | Rechazo técnico parcial por registro. | Parcialidad de rechazo, no parcialidad monetaria. | evidencia por registro. | Pendiente |
| TechnicalOperatorResponse | Resultado técnico/operativo. | No causal devolución regulatoria. | código Ixxx + evento operativo. | Pendiente |
| InternalOnly | Código interno (`DXX-LIQ`). | No exposición externa. | evidencia de restricción por flow. | Pendiente |

Reglas explícitas:
- `RejectedTotal`: 0 aplicaciones.
- `RejectedPartial`: parcialidad por registros, no por monto.
- `AmountPartialReturn`: no modelado.
- `ManualResolvedAuditOnly`: no applied return.
- Orphans: no estado, no contabilidad.

## 6. Checklist UAT — Accepted

| ID | Caso | Entrada | Resultado esperado | Evidencia | Estado | Observación |
|---|---|---|---|---|---|---|
| A-01 | Archivo/lote con todos válidos | Registros linkeables y causales válidas | `Decision=Accepted` | response + audit | Pendiente | |
| A-02 | Aplicación total | Todos válidos | Todos quedan aplicados | DB estado/transiciones | Pendiente | |
| A-03 | Conteo aplicado | N válidos | `UpdatedTransactionCount = N` | resultado + DB | Pendiente | |
| A-04 | Eventos por aplicado | N aplicados | N `AchTransactionStateEvent` | query eventos | Pendiente | |
| A-05 | Sin failures de rechazo | Archivo válido | failures de rechazo = 0 | response/audit | Pendiente | |
| A-06 | No rechazo | Archivo válido | no `RejectedPartial/RejectedTotal` | decision | Pendiente | |
| A-07 | Causal válida | Rxx/DEVxx por cámara/flujo | aceptación de causal | evidencia policy/catalog | Pendiente | |
| A-08 | Trazabilidad base | Archivo procesado | archivo/hash/ciclo/cámara presentes | payload/reportes | Pendiente | |

## 7. Checklist UAT — RejectedTotal

| ID | Caso | Entrada | Resultado esperado | Evidencia | Estado | Observación |
|---|---|---|---|---|---|---|
| RT-01 | Sin registros aplicables | archivo inválido/no aplicable | `Decision=RejectedTotal` | response/audit | Pendiente | |
| RT-02 | Fallo total por causal/policy/linking | todos fallan | 0 aplicados | failures detallados | Pendiente | |
| RT-03 | Decisión total | archivo rechazado total | `RejectedTotal` | decision | Pendiente | |
| RT-04 | Conteo aplicado | rechazo total | `UpdatedTransactionCount=0` | response | Pendiente | |
| RT-05 | Estado no cambia | cualquier tx involucrada | no `ReturnedByEpr/ReturnedByOperator` por esta ruta | DB before/after | Pendiente | |
| RT-06 | Sin state events | rechazo total | 0 `AchTransactionStateEvent` | query eventos | Pendiente | |
| RT-07 | Conserva evidencia | rechazo total | failures/audit preservados | audit payload | Pendiente | |
| RT-08 | Sin contabilidad | rechazo total | no asiento | conciliación/contabilidad | Pendiente | |
| RT-09 | Sin ROR | rechazo total | no habilita ROR | trazabilidad ROR | Pendiente | |
| RT-10 | No orphan/manual | rechazo total | no reclasificar como manual resolution | eventos/rutas | Pendiente | |
| RT-11 | No monto parcial | rechazo total | no confundir con devolución parcial por monto | evidencia semántica | Pendiente | |

## 8. Checklist UAT — RejectedPartial

| ID | Caso | Entrada | Resultado esperado | Evidencia | Estado | Observación |
|---|---|---|---|---|---|---|
| RP-01 | Mezcla válidos/inválidos | archivo mixto | `Decision=RejectedPartial` | response | Pendiente | |
| RP-02 | Decisión parcial | mezcla | `RejectedPartial` | decision | Pendiente | |
| RP-03 | Aplicación selectiva | mezcla | solo válidos aplicados | DB + conteos | Pendiente | |
| RP-04 | Estado selectivo | mezcla | solo aplicados cambian estado | DB before/after | Pendiente | |
| RP-05 | Evento selectivo | mezcla | solo aplicados crean state event | query eventos | Pendiente | |
| RP-06 | Rechazado conserva estado | registro rechazado | estado previo intacto | DB | Pendiente | |
| RP-07 | Rechazado sin state event | registro rechazado | 0 state event para rechazado | query eventos | Pendiente | |
| RP-08 | Evidence por rechazado | mezcla | failures por registro rechazado | failures/audit | Pendiente | |
| RP-09 | Conteos consistentes | mezcla | aplicado + rechazado consistente | response/audit | Pendiente | |
| RP-10 | No monto parcial | mezcla | no interpretarlo como devolución parcial por monto | evidencia semántica | Pendiente | |
| RP-11 | Sin asiento rechazado | mezcla | rechazados sin contabilidad | reporte contable | Pendiente | |
| RP-12 | ROR condicionado | mezcla | solo aplicados con linaje válido podrían habilitar ROR | trazabilidad | Pendiente | |

## 9. Checklist UAT — Devolución parcial por monto
- No está modelada actualmente.
- No hay campo específico.
- No hay evento específico.
- No hay estado específico.
- No se debe usar `RejectedPartial` para este caso.
- Si negocio la requiere, abrir análisis normativo/contable separado.

| ID | Control | Resultado esperado | Evidencia | Estado |
|---|---|---|---|---|
| AMT-01 | `RejectedPartial` no cambia monto | montos de tx no se fraccionan por semántica de rechazo parcial | DB / resultados de caracterización | Pendiente |
| AMT-02 | Evento inexistente | no existe `AmountPartialReturn` | catálogo/eventos | Pendiente |
| AMT-03 | Sin contabilidad parcial por monto | no asiento parcial derivado de esta semántica | reporte contable | Pendiente |
| AMT-04 | UAT fuera de alcance | no certificar devolución parcial por monto en esta fase | acta UAT | Pendiente |

## 10. Checklist UAT — Orphans/no resueltas

| ID | Caso | Resultado esperado | Evidencia | Estado |
|---|---|---|---|---|
| OR-01 | NotFound | unresolved/manual review | event + evidence | Pendiente |
| OR-02 | Ambiguous | unresolved/manual review | event + candidates | Pendiente |
| OR-03 | NoResuelto | pendiente de resolución | estado/link/evento | Pendiente |
| OR-04 | `ManualReviewRequired=true` | marcado para revisión manual | payload | Pendiente |
| OR-05 | `IncomingReturnUnresolved` | evidence estandarizada | `EvidenceJson` | Pendiente |
| OR-06 | candidates en ambiguous | preservados | `candidateTransactionIds` | Pendiente |
| OR-07 | `stateChanged=false` | no transición | payload | Pendiente |
| OR-08 | `applied=false` | no aplicación | payload | Pendiente |
| OR-09 | Sin state event | 0 `AchTransactionStateEvent` | query | Pendiente |
| OR-10 | Sin contabilidad | no asiento | reporte contable | Pendiente |
| OR-11 | Sin ROR | no habilita ROR | trazabilidad | Pendiente |
| OR-12 | No es RejectedPartial | taxonomía separada | matriz + evidencia | Pendiente |

## 11. Checklist UAT — Resolución manual audit-only

| ID | Caso | Resultado esperado | Evidencia | Estado |
|---|---|---|---|---|
| MR-01 | NotFound + MarkAsIgnored | cierre audit-only | evento resolución | Pendiente |
| MR-02 | Ambiguous + MarkAsRejected | cierre audit-only | evento resolución | Pendiente |
| MR-03 | KeepPending | continuidad de revisión manual | evidencia workflow | Pendiente |
| MR-04 | LinkToTransaction audit-only | no aplicación real automática | evidence json | Pendiente |
| MR-05 | `ResolvedBy` requerido | validación obligatoria | request/audit | Pendiente |
| MR-06 | `OrphanManualResolution` | processing event creado | query eventos | Pendiente |
| MR-07 | `IncomingReturnManualResolved` | payload esperado | `EvidenceJson` | Pendiente |
| MR-08 | `stateChanged=false` | no cambio estado | payload | Pendiente |
| MR-09 | `applied=false` | no devolución aplicada | payload | Pendiente |
| MR-10 | `achTransactionStateEventCreated=false` | sin state event | DB | Pendiente |
| MR-11 | Sin contabilidad | no asiento | reporte contable | Pendiente |
| MR-12 | Sin ROR | no habilita ROR | trazabilidad | Pendiente |
| MR-13 | Segundo intento | `AlreadyResolved` | respuesta + no duplicación | Pendiente |
| MR-14 | Link audit-only | no equivale a applied return | evidencia semántica | Pendiente |

## 12. Checklist UAT — Códigos y causales

| ID | Código | Caso | Resultado esperado | Evidencia | Estado |
|---|---|---|---|---|---|
| CC-01 | Rxx | devolución por cámara/flujo | permitido solo en flujos de devolución | policy/tests/UAT | Pendiente |
| CC-02 | DEVxx | devolución por cámara/flujo | permitido solo en flujos de devolución | policy/tests/UAT | Pendiente |
| CC-03 | D01..D06 | file/record rejection | permitido en file reject | policy/tests/UAT | Pendiente |
| CC-04 | Dxx en returns | incoming/outbound/ROR | rechazado | policy evidence | Pendiente |
| CC-05 | I500/I503/ITIMEOUT/ISOAP/IFUNC | técnico/operador/command center | permitido técnico | policy evidence | Pendiente |
| CC-06 | Ixxx en returns | causal devolución | rechazado | policy evidence | Pendiente |
| CC-07 | DXX-LIQ | InternalOnly | solo interno | policy evidence | Pendiente |
| CC-08 | Rxx/DEVxx en file reject | file reject flows | rechazado | policy evidence | Pendiente |

## 13. Checklist UAT — Estado, evento y auditoría

| Escenario | Cambia estado | Crea state event | Crea processing event | EvidenceJson esperado | Actor | Estado UAT |
|---|---|---|---|---|---|---|
| Accepted | Sí | Sí | Según ruta | incoming applied evidence | sistema/operador | Pendiente |
| RejectedTotal | No | No | Sí (audit/failure) | rechazo total evidence | sistema | Pendiente |
| RejectedPartial aplicado | Sí | Sí | Sí | payload aplicación | sistema | Pendiente |
| RejectedPartial rechazado | No | No | Sí | failure/reject evidence | sistema | Pendiente |
| Ruta B applied | Sí | Sí | Sí | linking+aplicación | sistema | Pendiente |
| NotFound | No | No | Sí | `IncomingReturnUnresolved` | sistema | Pendiente |
| Ambiguous | No | No | Sí | `IncomingReturnUnresolved` + candidates | sistema | Pendiente |
| ManualResolvedAuditOnly | No | No | Sí | `IncomingReturnManualResolved` | resolvedBy | Pendiente |
| Duplicate file | No | No | Sí | evidencia idempotencia | sistema | Pendiente |
| Outbound ReturnFileGenerated | No (audit-only) | Sí/trace event | N/A | payload outbound | operador/sistema | Pendiente |

## 14. Checklist UAT — Idempotencia

| ID | Escenario | Llave/control | Resultado esperado | Evidencia | Riesgo | Estado UAT |
|---|---|---|---|---|---|---|
| IDE-01 | Duplicate incoming canonical | hash+tamaño | no nueva ingesta base | resultado duplicado | Medio | Pendiente |
| IDE-02 | Reprocess `IsReprocess=true` | parent + hash+tamaño | reproceso controlado | evidence reprocess | Medio | Pendiente |
| IDE-03 | RejectedTotal reprocesado | mismas reglas | mantiene no aplicación cuando persiste falla | decisiones comparadas | Medio | Pendiente |
| IDE-04 | RejectedPartial reprocesado | filtros + policy | aplica solo elegibles | conteos comparados | Medio | Pendiente |
| IDE-05 | Duplicate orphan event | control de duplicidad | no duplicación semántica | cardinalidad evento | Medio | Pendiente |
| IDE-06 | Manual resolution duplicate | guard AlreadyResolved | segundo intento rechazado | respuesta + conteo | Bajo/Medio | Pendiente |
| IDE-07 | Outbound duplicate return | validación/lock/idempotencia | no doble generación | rows/eventos | Medio | Pendiente |
| IDE-08 | File reject Dxx event | policy flow | separación Dxx vs return | eventos/policy | Medio | Pendiente |

## 15. Checklist UAT — Relación con outbound

| ID | Caso | Resultado esperado | Evidencia | Estado |
|---|---|---|---|---|
| OUT-01 | `ReturnFileGenerated` outbound | no equivale a incoming applied | matrix/eventos | Pendiente |
| OUT-02 | No generada outbound | no equivale a `RejectedPartial` incoming | trazabilidad cruzada | Pendiente |
| OUT-03 | Error NACHA outbound | técnico, no devolución parcial | validator/logs | Pendiente |
| OUT-04 | Duplicado outbound | no contamina incoming | evidencias separadas | Pendiente |
| OUT-05 | Conciliación | distinguir `GeneratedNotTransmitted` vs `IncomingApplied` | reporte conciliación | Pendiente |

## 16. Checklist UAT — Relación con ROR

| ID | Caso | Resultado esperado | Evidencia | Estado |
|---|---|---|---|---|
| ROR-01 | Accepted/applied + linaje válido | puede habilitar ROR | trazabilidad linaje | Pendiente |
| ROR-02 | RejectedTotal | no habilita ROR | matriz + evidencia | Pendiente |
| ROR-03 | Rechazado en RejectedPartial | no habilita ROR | evidencia por registro | Pendiente |
| ROR-04 | NotFound/Ambiguous | no habilitan ROR | orphan evidence | Pendiente |
| ROR-05 | ManualResolvedAuditOnly | no habilita ROR | manual resolution evidence | Pendiente |
| ROR-06 | Dependencia ROR | requiere applied + lineage, no link audit-only | criterios UAT | Pendiente |

## 17. Checklist UAT — Relación con contabilidad y conciliación

| ID | Caso | Resultado esperado | Evidencia | Estado |
|---|---|---|---|---|
| CON-01 | Accepted/applied | entra a conciliación operativa | reporte | Pendiente |
| CON-02 | RejectedTotal | no genera asiento | reporte contable | Pendiente |
| CON-03 | RejectedPartial | solo aplicados se consideran | reporte/estado | Pendiente |
| CON-04 | Rechazados | no generan asiento | reporte contable | Pendiente |
| CON-05 | ManualResolvedAuditOnly | no genera asiento | evidencia manual | Pendiente |
| CON-06 | AmountPartialReturn | requiere diseño contable explícito | acta de alcance | Pendiente |
| CON-07 | Separación de reporte | categorías separadas (accepted, rejected total, rejected partial aplicado/rechazado, orphan, manual resolved audit-only, duplicate, outbound generated, ROR generated) | reporte operativo | Pendiente |

## 18. Evidencia requerida
Adjuntar mínimo:
- archivo NACHA;
- hash;
- tamaño;
- cámara;
- ciclo;
- decision;
- result counts;
- failures;
- trace original;
- return reason;
- record/entry/addenda;
- `IncomingNachaProcessingEvent`;
- `IncomingNachaTransactionLink`;
- `AchTransactionStateEvent`;
- `EvidenceJson`;
- `PayloadJson`;
- actor manual;
- `resolvedAtUtc`;
- screenshots/reportes command center si aplica;
- resultado tests;
- acta UAT;
- firmas.

## 19. Criterios de salida de NO-GO
1. Accepted validado con evidencia.
2. RejectedTotal validado con evidencia.
3. RejectedPartial validado con evidencia.
4. AmountPartialReturn declarado fuera de alcance.
5. NotFound validado.
6. Ambiguous validado.
7. ManualResolvedAuditOnly validado.
8. Dxx/Ixxx/Rxx/DEVxx validados por flow.
9. State/event matrix validada.
10. Idempotencia validada.
11. ROR impact validado.
12. Contabilidad/conciliación definida.
13. Command center/reportes definidos.
14. UAT ACH ejecutado.
15. UAT CENIT ejecutado.
16. Firma negocio.
17. Firma operaciones.
18. Firma riesgo/compliance.
19. Aprobación tecnología.
20. Scorecard actualizado.

## 20. Riesgos residuales
- confundir `RejectedPartial` con devolución parcial por monto;
- confundir manual resolved audit-only con applied;
- reportes operativos sin categoría clara;
- command center sin etiquetas claras;
- contabilidad no cerrada;
- ROR habilitado por señal incorrecta;
- UAT pendiente con datos reales;
- NO-GO productivo vigente.

## 21. Decisión vigente
- **GO técnico:** sí.
- **GO UAT controlado:** sí.
- **NO-GO productivo:** sí.
- El checklist es evidencia UAT requerida, no autorización productiva.
- Próximo paso recomendado después del checklist: decidir si hace falta estandarizar eventos de rechazo o pasar a contabilidad/conciliación según brechas UAT.
