# Checklist UAT — Devolución saliente normal: estado, evento y trazabilidad

> Referencia UAT complementaria: `docs/uat/incoming-return-orphan-acceptance-checklist.md`.


## 1. Propósito
Validar la generación de devolución saliente normal con evidencia técnica y funcional sobre:
- creación de `AchReturnGenerated`;
- evento audit-only `ReturnFileGenerated`;
- payload trazable (`PayloadJson`);
- hash SHA-256 del archivo (`contentSha256`);
- idempotencia DB-first;
- no cambio de estado de transacción;
- relación con ROR, incoming y conciliación;
- criterios de salida de NO-GO productivo.

## 2. Estado actual
- **GO técnico:** sí.
- **GO UAT controlado:** sí.
- **NO-GO productivo:** sí.
- Se genera `AchReturnGenerated`.
- Se genera `AchTransactionStateEvent` por transacción.
- `AchTransaction.State` **no cambia**.
- No implica transmisión a cámara.
- No implica aceptación de cámara.
- No implica asiento contable.
- Idempotencia endurecida por índice único DB-first.
- Pendiente UAT formal con acta y firmas.

## 3. Alcance UAT
Cobertura mínima:
- devolución saliente ACH;
- devolución saliente CENIT;
- una transacción;
- múltiples transacciones;
- reintento duplicado;
- dos instancias/nodos;
- causal válida;
- causal inválida;
- relación con ROR;
- relación con incoming;
- conciliación.

## 4. Checklist técnico

| ID | Control | Validación esperada | Evidencia | Estado | Observaciones |
|---|---|---|---|---|---|
| T-01 | `AchReturnGenerated` creado | Existe registro por transacción devuelta | DB query + evidencia API | Pendiente | |
| T-02 | `AchTransactionStateEvent` creado | Existe evento por transacción devuelta | DB query + payload | Pendiente | |
| T-03 | Cardinalidad de evento | 1 `ReturnFileGenerated` por transacción | Conteo eventos/transacciones | Pendiente | |
| T-04 | `schemaVersion` | `schemaVersion = 1` | `PayloadJson` | Pendiente | |
| T-05 | `stateChanged` | `stateChanged = false` | `PayloadJson` | Pendiente | Audit-only |
| T-06 | Estado previo/nuevo | `previousState == newState` | `PayloadJson` + DB | Pendiente | No state machine en esta fase |
| T-07 | Estado técnico transmisión | `transmissionStatus = GeneratedNotTransmitted` | `PayloadJson` | Pendiente | No transmisión real |
| T-08 | Estado productivo | `productiveStatus = TechnicalGeneratedOnly` | `PayloadJson` | Pendiente | No aceptación de cámara |
| T-09 | Hash de contenido | `contentSha256` coincide con archivo generado | Re-cálculo SHA-256 + payload | Pendiente | UTF-8 bytes del archivo |
| T-10 | Nombres de archivo | `fileName` y `externalFileName` presentes | `PayloadJson` + respuesta | Pendiente | |
| T-11 | Causal | `returnReasonCode` presente | `PayloadJson` + request | Pendiente | |
| T-12 | Ciclo | `returnCycleId` presente | `PayloadJson` + request | Pendiente | |
| T-13 | Cámara | `clearingHouseId/code/name` presentes | `PayloadJson` | Pendiente | |
| T-14 | Trazas | original/new trace + sequence presentes | `PayloadJson` | Pendiente | |
| T-15 | Conteos | `recordCount` y `returnCount` presentes | `PayloadJson` + archivo | Pendiente | |
| T-16 | No cambio de estado | `AchTransaction.State` no cambia | DB before/after | Pendiente | |
| T-17 | No cambio de fecha estado | `StateChangedAtUtc` no cambia | DB before/after | Pendiente | |
| T-18 | Fallo sin persistencia | Si falla generación, no hay fila/evento | DB + error | Pendiente | |
| T-19 | Sin evento duplicado | Reintento duplicado no crea 2º evento | DB counts | Pendiente | |
| T-20 | Índice único | Existe índice único funcional | Model metadata + migración | Pendiente | |
| T-21 | Multiinstancia sin duplicado | Dos instancias no duplican filas | Test SQLite shared DB | Pendiente | |
| T-22 | Fila única final | 1 `AchReturnGenerated` final | DB count | Pendiente | |
| T-23 | Evento único final | 1 `AchTransactionStateEvent` final | DB count | Pendiente | |

## 5. Checklist funcional UAT
Casos mínimos:
1. ACH genera DEV14 correctamente.
2. ACH multi-transacción en un archivo.
3. CENIT genera R01 correctamente.
4. Reintento del mismo request es rechazado.
5. Dos usuarios intentan misma devolución.
6. Dos nodos/instancias intentan misma devolución.
7. Causal inválida es rechazada sin evento.
8. Archivo conserva estructura NACHA 1/5/6/7/8/9.
9. Hash de payload coincide con archivo descargado.
10. Evento audit-only no cambia estado transaccional.
11. ROR no se afecta por este cambio.
12. Incoming no se afecta por este cambio.
13. Conciliación identifica `GeneratedNotTransmitted`.

## 6. Checklist de idempotencia
Validar explícitamente:
- mismo request dos veces;
- dos servicios separados;
- dos `DbContext` contra la misma DB;
- unique constraint activo;
- error funcional de duplicado;
- no duplicidad en `AchReturnGenerated`;
- no duplicidad en `AchTransactionStateEvent`;
- retry después de éxito;
- falla antes de persistir;
- falla por causal/policy.

## 7. Checklist de auditoría
Preguntas UAT obligatorias:
- ¿quién generó?
- ¿cuándo?
- ¿qué archivo?
- ¿qué hash?
- ¿qué cámara?
- ¿qué ciclo?
- ¿qué causal?
- ¿qué transacción original?
- ¿qué trace original?
- ¿qué trace nuevo?
- ¿qué monto?
- ¿qué estado previo/nuevo?
- ¿estado de transmisión?
- ¿aceptación/rechazo?
- ¿warnings?
- ¿conciliación?

Brechas a marcar como pendientes si no están estructuradas:
- `RequestedBy`;
- IP/origen técnico de solicitud.

## 8. Checklist de pruebas técnicas
Suites/filtros mínimos:
- `AchOutboundReturnStateAndIdempotencyCharacterizationTests`
- `AchOutboundReturnConcurrencyIdempotencyTests`
- `AchReturnsFileByClearingHouseTests`

Filtros de ejecución:
- `AchReturns|OutboundReturn|ReturnGenerated|StateEvent|Idempotency|Traceability`
- `CauseCode|AchCauseCodePolicy|Nacha|ExternalFileName|ReturnOfReturn|IncomingReturn`

## 9. Relación con ROR
- Outbound ya deja evento y hash en payload audit-only.
- ROR no debe depender solo del evento hasta cierre normativo.
- Riesgo de duplicidad mitigado por idempotencia DB-first.
- Pendiente UAT ROR específico sobre devolución generada.

## 10. Relación con incoming
- Incoming no se modificó en esta fase.
- Outbound `Generated` no equivale a incoming `Returned`.
- Debe distinguirse explícitamente: generado / transmitido / aceptado / recibido / rechazado.

## 11. Conciliación/contabilidad
- No hay asiento contable explícito en esta fase.
- Conciliación debe distinguir `GeneratedNotTransmitted`.
- Pendiente definición contable y reporte operativo de ciclo completo.

## 12. Evidencia requerida
Adjuntar como mínimo:
- archivo NACHA generado;
- hash SHA-256 recalculado;
- `PayloadJson` del evento;
- fila `AchReturnGenerated`;
- fila `AchTransactionStateEvent`;
- prueba de duplicado rechazado;
- prueba de multiinstancia;
- logs de ejecución;
- acta UAT;
- firmas de negocio, operaciones, riesgo, compliance y tecnología.

## 13. Criterios de salida de NO-GO productivo
Checklist mínimo de salida:
1. UAT ACH completo.
2. UAT CENIT completo.
3. Evento `ReturnFileGenerated` validado.
4. Payload trazable validado.
5. Hash validado.
6. Idempotencia multiinstancia validada.
7. Reintentos seguros validados.
8. Sin impacto funcional ROR.
9. Sin impacto funcional incoming.
10. Conciliación definida.
11. Contabilidad definida.
12. Aprobación negocio.
13. Aprobación operaciones.
14. Aprobación riesgo/compliance.
15. Aprobación tecnología.
16. Scorecard funcional-normativo actualizado.

## 14. Riesgos residuales
- Falta lifecycle completo de transmisión/aceptación/rechazo.
- `RequestedBy` / IP aún no estructurados.
- Contabilidad pendiente.
- Conciliación operativa pendiente de cierre.
- UAT con cámara pendiente de firma formal.
- Riesgo de confundir generación técnica con transmisión real.

## 15. Decisión vigente
- **GO técnico:** sí.
- **GO UAT controlado:** sí.
- **NO-GO productivo:** sí.
- Próximo hito: devolución de entrada E2E y manejo de huérfanas.


## Referencia cruzada outbound vs incoming

Para diferenciar explícitamente `outbound generated` vs `incoming applied` y su trazabilidad:

- `docs/audits/incoming-return-e2e-orphan-matrix-current.md`

## Referencia cruzada total vs partial

Para la frontera semántica canónica entre `RejectedTotal`, `RejectedPartial`, `Accepted`, orphan/unresolved, manual audit-only y la distinción formal frente a devolución parcial por monto, ver:

- `docs/audits/total-vs-partial-rejection-matrix-current.md`

## Referencia cruzada checklist UAT total vs partial

Para la validación UAT paso-a-paso de `Accepted`, `RejectedTotal`, `RejectedPartial`, orphan/unresolved, manual audit-only, separación de códigos y relación con ROR/contabilidad, ver:

- `docs/uat/rejection-total-partial-acceptance-checklist.md`

- Referencia complementaria ciclos/neteo/liquidez/evidencia CUD: `docs/audits/cenit-cycles-netting-liquidity-cud-matrix-current.md` (no cambia decisión NO-GO productivo).
