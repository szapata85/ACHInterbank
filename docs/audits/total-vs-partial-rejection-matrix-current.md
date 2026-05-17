# Matriz vigente — Rechazo total vs rechazo parcial vs devolución parcial

## 1. Propósito
Esta matriz define de forma canónica y trazable la diferencia entre:
- rechazo total de archivo/lote;
- rechazo parcial de registros;
- `Accepted` / aplicación completa;
- devolución entrante aplicada;
- devolución parcial por monto;
- huérfanas/no resueltas;
- resolución manual audit-only;
- separación entre `Rxx/DEVxx`, `Dxx`, `Ixxx` e internos;
- impacto en estado, eventos, contabilidad, ROR, outbound e incoming.

## 2. Estado actual
- **GO técnico:** sí.
- **GO UAT controlado:** sí.
- **NO-GO productivo:** sí.
- Existe separación técnica parcial entre taxonomías de causal/código.
- Existe caracterización por tests (incluyendo `test(rejections): characterize total vs partial rejection behavior`).
- Falta UAT formal con evidencia firmada por negocio/operaciones/riesgo/compliance.
- Esta matriz formaliza la frontera semántica, **pero no habilita producción**.

## 3. Fuentes revisadas
- `docs/dev/devoluciones-ach-auditoria-cenit-ach-colombia.md`
- `docs/audits/s1-matriz-maestra-trazable-funcional-normativa-2026-04-26.md`
- `docs/audits/go-nogo-scorecard-funcional-normativo-2026-04-26.md`
- `docs/audits/cause-code-normative-matrix-ach-cenit-sta-current.md`
- `docs/uat/cause-code-acceptance-checklist.md`
- `docs/audits/incoming-return-e2e-orphan-matrix-current.md`
- `docs/uat/incoming-return-orphan-acceptance-checklist.md`
- `docs/audits/nacha-record-level-normative-matrix-ach-cenit-current.md`
- `docs/uat/nacha-records-acceptance-checklist.md`
- `docs/uat/outbound-return-state-traceability-acceptance-checklist.md`
- `tests/Cfa.ACHInterbank.Tests/RejectionTotalVsPartialCharacterizationTests.cs`
- tests existentes de cause policy, incoming/orphan y manual resolution.

## 4. Definiciones canónicas

| Concepto | Definición | Aplica a | Cambia estado transacción | Crea AchTransactionStateEvent | Contabilidad | ROR | Observaciones |
|---|---|---|---|---|---|---|---|
| Accepted | Todos los registros válidos de la ingesta son aplicados. | Incoming Ruta A | Sí, para todos los válidos. | Sí, para todos los aplicados. | Potencialmente conciliable según flujo. | Potencial por linaje aplicado. | Sin failures de rechazo funcional. |
| RejectedTotal | Ningún registro es aplicado. | Incoming Ruta A / file-level reject | No. | No. | No debe generar asiento. | No habilita. | Puede conservar failures/audit/evidencia de rechazo. |
| RejectedPartial | Algunos registros se aplican y otros se rechazan. | Incoming Ruta A | Sí, **solo** aplicados. | Sí, **solo** aplicados. | Solo aplicados podrían alimentar conciliación. | Solo aplicados con linaje válido. | Parcialidad por registro, **no por monto**. |
| IncomingReturnApplied | Devolución entrante aplicada sobre transacción original. | Incoming Ruta A/B applied | Sí (según ruta/estado vigente). | Sí. | Puede ser insumo de conciliación. | Sí, con linaje válido. | Evento esperado: `IncomingReturnApplied` cuando aplica. |
| AmountPartialReturn / devolución parcial por monto | Devolución monetaria parcial de una transacción. | No modelada actualmente | N/A | N/A | Requeriría diseño contable nuevo. | N/A | No confundir con `RejectedPartial`. |
| NotFound | Linker no encuentra transacción destino. | Incoming Ruta B | No. | No. | No. | No. | Queda como orphan/unresolved con evidencia. |
| Ambiguous | Linker encuentra múltiples candidatos no determinísticos. | Incoming Ruta B | No. | No. | No. | No. | Requiere revisión manual; no aplicación automática. |
| NoResuelto | Estado de resolución pendiente/no determinada. | Incoming Ruta B | No. | No. | No. | No. | Categoría de unresolved/orphan. |
| ManualResolvedAuditOnly | Cierre manual operativo/auditoría de orphan. | Incoming manual resolution | No. | No (de estado transaccional). | No. | No por sí sola. | No equivale a devolución aplicada. |
| FileRejectTotal | Rechazo total de archivo/flujo técnico. | Rechazo de archivo | No. | No. | No. | No. | Puede usar `Dxx` según policy de flujo. |
| FileRejectPartial | Rechazo parcial técnico de registros de archivo. | Rechazo de archivo/registro | Solo si existe lógica de aplicación separada. | Solo si se aplicó registro (según flujo). | Parcial por registro aplicado. | Condicional por linaje aplicado. | No implica parcialidad monetaria. |
| TechnicalOperatorResponse | Resultado técnico/operativo (integración). | Operator/Command Center | No por defecto. | No por defecto. | No por defecto. | No por defecto. | Taxonomía típica `Ixxx`. |
| InternalOnly | Código interno no expuesto como causal externa. | Flujos internos | No por sí mismo. | No por sí mismo. | No por sí mismo. | No por sí mismo. | `DXX-LIQ` permanece interno. |

## 5. Matriz de decisión incoming Ruta A

| Decisión | Condición | Registros aplicados | Registros rechazados | Estado transacción | Evento estado | Evidencia | Riesgo |
|---|---|---:|---:|---|---|---|---|
| Accepted | Todos los registros válidos/linkeados y permitidos por policy/catálogo. | Todos | 0 | Cambia en todos los aplicados. | `AchTransactionStateEvent` por aplicado. | Audit/failures vacíos o sin rechazos. | Confundir con aceptación de cámara externa. |
| RejectedTotal | 0 registros aplicables (archivo vacío, no linkeable total, policy/catálogo totalmente rechazado, etc.). | 0 | Todos los evaluados | No cambia. | No crea. | Failures + auditoría de ingesta. | Aplicación accidental indebida si no se respeta condición. |
| RejectedPartial | Mezcla de registros aplicables y no aplicables. | Algunos | Algunos | Cambia solo en aplicados. | Solo para aplicados. | Failures del subconjunto rechazado + evidencia aplicada. | Confundir con devolución parcial por monto. |

**Regla canónica:** `RejectedPartial` **NO** representa devolución parcial por monto.

## 6. Matriz incoming Ruta B / PostParse

| Resultado linker/postparse | Criterio | Aplica estado | ProcessingEvent | EvidenceJson | ManualReviewRequired | Es rechazo parcial | Observación |
|---|---|---|---|---|---|---|---|
| Linked/Applied | Match determinístico y reglas permiten transición. | Sí | Sí | Sí | No | No necesariamente | Es aplicación entrante. |
| NotFound | Sin candidato válido. | No | `LinkingBloqueado` u homólogo | `IncomingReturnUnresolved` | Sí | No | Es orphan/unresolved. |
| Ambiguous | Múltiples candidatos no determinísticos. | No | `LinkingBloqueado` u homólogo | `IncomingReturnUnresolved` + candidates | Sí | No | No debe aplicar estado automáticamente. |
| NoResuelto | Resolución pendiente/no determinística. | No | Sí (seguimiento) | Sí | Sí | No | Estado de trabajo manual/operativo. |
| ManualResolvedAuditOnly | Resolución manual de orphan (ignore/link audit-only). | No | `OrphanManualResolution` | `IncomingReturnManualResolved` | No (queda resuelto) | No | No equivale a devolución aplicada. |

## 7. Matriz outbound

| Escenario outbound | Resultado actual | AchReturnGenerated | ReturnFileGenerated event | Estado transacción | Es RejectedPartial | Observación |
|---|---|---|---|---|---|---|
| Archivo generado con todos los ítems válidos | Genera archivo y persistencia de generadas. | Sí | Sí (según trazabilidad vigente). | No cambia en generación audit-only. | No | Generación técnica no equivale a aceptación de cámara. |
| Transacción duplicada/ya generada | Rechazo/control de duplicado. | No para duplicada nueva | No para inválida | No | No | No existe estado explícito `OutboundRejectedPartial`. |
| Causal/policy inválida | Falla validación y no debe persistir inválida. | No para inválida | No para inválida | No | No | Separación causal/policy obligatoria. |
| Error antes de persistir | Falla transaccional previa a persistencia. | No | No | No | No | Debe quedar evidencia de error técnico. |
| Rechazo por NACHA validator | Bloqueo técnico estructural. | No (o rollback) | No | No | No | Rechazo técnico, no causal devolución. |
| Generación parcial outbound | Se interpreta como procesamiento de válidas + exclusión/fallo de inválidas según lógica vigente. | Solo válidas | Solo válidas | No | No (sin estado explícito) | No interpretar exclusión como devolución parcial por monto. |

**Aclaración:** outbound no tiene estado explícito `OutboundRejectedPartial`.

## 8. Matriz de causales/códigos

| Código | Tipo | Flujos permitidos | Flujos prohibidos | Sale a cámara como causal devolución | Observación |
|---|---|---|---|---|---|
| `Rxx` | Causal regulatoria devolución | IncomingReturn / OutboundReturn / ReturnOfReturn (según rail/policy) | FileRejectTotal / FileRejectPartial / Technical-only | Sí | No usar como rechazo técnico de archivo. |
| `DEVxx` | Causal regulatoria devolución | IncomingReturn / OutboundReturn / ReturnOfReturn (según rail/policy) | FileRejectTotal / FileRejectPartial / Technical-only | Sí | Equivale a causal de devolución, no técnica. |
| `D01..D06` | Rechazo archivo/registro | FileRejectTotal / FileRejectPartial (y/o command center técnico) | Return flows (`Incoming/Outbound/ROR`) | No | `Dxx` no debe salir como `Rxx`. |
| `I500/I503/ITIMEOUT/ISOAP/IFUNC` | Técnico/integración | OperatorResponse / CommandCenter | Return flows y file reject normativo | No | Taxonomía operativa, no causal regulatoria. |
| `DXX-LIQ` | Interno | InternalOnly | External flows (return/file reject técnicos externos) | No | No debe exponerse a cámara como causal externa. |

## 9. Matriz de estado/evento/auditoría

| Escenario | Cambia AchTransaction.State | ToState esperado | Crea AchTransactionStateEvent | Crea IncomingNachaProcessingEvent | EvidenceJson | Payload esperado |
|---|---|---|---|---|---|---|
| Accepted Ruta A | Sí | `ReturnedByEpr` (ruta entrante aplicada) | Sí | Puede existir evento de proceso adicional | Sí | `IncomingReturnApplied` (stateChanged=true). |
| RejectedTotal Ruta A | No | N/A | No | Puede existir evidencia/failure de ingesta | Sí (audit/failures) | Rechazo file-level sin aplicación. |
| RejectedPartial aplicado | Sí (solo aplicados) | `ReturnedByEpr` aplicado | Sí (solo aplicados) | Sí (según pipeline) | Sí | Payload de aplicación por aplicado. |
| RejectedPartial rechazado | No | N/A | No | Sí (rechazo/bloqueo/diagnóstico) | Sí | Failure/reason por registro. |
| Ruta B applied | Sí | según transición definida | Sí | Sí | Sí | Evidencia de linking + aplicación. |
| NotFound | No | N/A | No | Sí (`LinkingBloqueado`) | Sí (`IncomingReturnUnresolved`) | `applied=false`, `stateChanged=false`. |
| Ambiguous | No | N/A | No | Sí (`LinkingBloqueado`) | Sí (`IncomingReturnUnresolved`) | `manualReviewRequired=true`. |
| ManualResolvedAuditOnly | No | N/A | No | Sí (`OrphanManualResolution`) | Sí (`IncomingReturnManualResolved`) | `applied=false`, `stateChanged=false`. |
| Duplicate file | No | N/A | No | Sí (resultado duplicado) | Sí | Idempotencia canonical hash+tamaño. |
| Outbound ReturnFileGenerated | No (audit-only actual) | Estado sin transición funcional | Sí/Evento trazable según implementación vigente | N/A | Sí (`PayloadJson`) | `GeneratedNotTransmitted`/audit-only. |

## 10. Matriz de idempotencia

| Escenario | Llave/control actual | Resultado esperado | Riesgo multiinstancia | Evidencia |
|---|---|---|---|---|
| duplicate incoming file canonical | Hash + tamaño canonical | No reprocesar como nuevo ingreso base | Medio (si no hay lock distribuido fuerte) | Resultado `Duplicado` + audit trail. |
| reprocess `IsReprocess=true` | `ParentIngestionId` + hash/tamaño | Reproceso controlado con trazabilidad | Medio | evidencia `ReprocesoAutorizado`. |
| RejectedTotal reproceso | mismas reglas de validación | Mantiene no aplicación si siguen fallas | Medio | decisiones/failures consistentes. |
| RejectedPartial reproceso | filtros de duplicidad + validaciones | Solo aplica elegibles no rechazados | Medio | conteos applied/rejected auditables. |
| duplicate orphan event | control por evento existente (según implementación) | Evitar duplicados semánticos | Medio | cardinalidad de eventos por entry/addenda. |
| manual resolution `AlreadyResolved` | guard de resolución única | Segundo intento no crea nuevo cierre | Bajo/Medio | respuesta `AlreadyResolved` + no duplicación. |
| outbound duplicate return | validación de ya generada/locks/idempotencia DB-first | No duplicar devolución generada | Medio | rechazo funcional + no doble persistencia. |
| file reject Dxx event | policy de flujo + evidencia de rechazo | Mantener separación Dxx vs return reason | Medio | event log + policy issues. |

## 11. Diferencia formal: rechazo parcial vs devolución parcial

- **Rechazo parcial**:
  - parcialidad por registros dentro de una ingesta/archivo/lote;
  - unos registros se aplican y otros se rechazan;
  - no implica devolución parcial de monto.

- **Devolución parcial por monto**:
  - implicaría devolver solo una parte monetaria de una transacción;
  - no está modelada actualmente en los componentes revisados;
  - no tiene evento/campo/estado propio;
  - requiere análisis normativo/contable separado si el negocio la solicita.

## 12. Relación con ROR
- ROR requiere linaje de devolución aplicada.
- `RejectedTotal` no habilita ROR.
- Registro rechazado dentro de `RejectedPartial` no habilita ROR.
- Huérfana unresolved no habilita ROR.
- `ManualResolvedAuditOnly` no habilita ROR por sí sola.
- Solo applied + lineage validado debería habilitar ROR.

## 13. Relación con contabilidad y conciliación
- `Accepted`/applied puede alimentar conciliación.
- `RejectedTotal` no debe afectar estado transaccional ni contabilidad.
- `RejectedPartial` solo afecta aplicadas.
- Rechazadas no deben generar asiento.
- `ManualResolvedAuditOnly` no genera asiento.
- Devolución parcial por monto requeriría diseño contable explícito.
- Esta matriz es insumo del punto de auditoría de contabilidad/conciliación.

## 14. Trazabilidad requerida
Evidencia mínima por caso:
- archivo;
- hash;
- tamaño;
- cámara;
- ciclo;
- decision;
- record id / entry / addenda;
- trace original;
- return reason;
- failure code;
- processing event;
- state event si aplica;
- `EvidenceJson`;
- actor si manual;
- result counts;
- status final.

## 15. Brechas P0/P1/P2

### P0
- confundir `RejectedPartial` con devolución parcial por monto;
- aplicar estado a registros rechazados;
- permitir `Dxx/Ixxx` como causal devolución;
- habilitar ROR desde orphan/manual audit-only;
- generar contabilidad desde manual audit-only.

### P1
- falta checklist UAT específico total-vs-partial;
- falta reporting operacional por categoría semántica;
- command center requiere nombres/etiquetas inequívocas;
- payloads aún heterogéneos entre rutas.

### P2
- métricas operativas consolidadas;
- visualización UI por categoría semántica;
- normalización final de payloads.

## 16. Criterios de salida
1. RejectedTotal caracterizado.
2. RejectedPartial caracterizado.
3. Accepted caracterizado.
4. AmountPartialReturn declarado no modelado.
5. NotFound/Ambiguous diferenciados.
6. ManualResolvedAuditOnly diferenciado.
7. Rxx/DEVxx vs Dxx/Ixxx documentado.
8. Eventos por escenario definidos.
9. Idempotencia por escenario definida.
10. Relación ROR definida.
11. Relación contabilidad definida.
12. UAT checklist creado.
13. Reporte/command center definido.
14. Firmas negocio/operaciones/riesgo/compliance.
15. Scorecard actualizado.

## 17. Decisión vigente
- **GO técnico:** sí.
- **GO UAT controlado:** sí.
- **NO-GO productivo:** sí.
- Esta matriz reduce ambigüedad semántica, pero no habilita producción.
- Próximo paso recomendado: estandarizar eventos de rechazo y/o crear checklist UAT específico total-vs-partial.

## Referencia cruzada checklist UAT total vs partial

Para la validación UAT paso-a-paso de `Accepted`, `RejectedTotal`, `RejectedPartial`, orphan/unresolved, manual audit-only, separación de códigos y relación con ROR/contabilidad, ver:

- `docs/uat/rejection-total-partial-acceptance-checklist.md`

- Referencia complementaria ciclos/neteo/liquidez/evidencia CUD: `docs/audits/cenit-cycles-netting-liquidity-cud-matrix-current.md` (no cambia decisión NO-GO productivo).

- Referencia matriz vigente de sobre/firma/certificados: `docs/audits/digital-envelope-signature-certificate-matrix-current.md` (no modifica decisión NO-GO productivo).
