# Checklist UAT — Devolución entrante E2E y huérfanas/no resueltas

## 1. Propósito
Este checklist valida integralmente para incoming:
- recepción de archivo incoming;
- idempotencia de archivo canonical;
- parsing/linking;
- aplicación de devolución entrante;
- eventos de estado;
- huérfanas/no resueltas;
- payload de evidencia;
- resolución manual audit-only;
- no duplicidad;
- relación con outbound;
- relación con ROR;
- conciliación/contabilidad;
- criterios de salida de NO-GO productivo.

## 2. Estado actual
- **GO técnico:** sí.
- **GO UAT controlado:** sí.
- **NO-GO productivo:** sí.
- Ruta A aplica estado `ReturnedByEpr` y crea evento `IncomingReturnApplied`.
- Ruta B aplica vía `AchStateTransitionService` y crea evento.
- Huérfanas/no resueltas se preservan con `EvidenceJson`.
- Resolución manual actual es **audit-only**.
- Resolución manual **no cambia `AchTransaction.State`**.
- Resolución manual **no crea `AchTransactionStateEvent`**.
- Resolución manual **no implica asiento contable**.
- Idempotencia canonical incoming existe con índice parcial.
- Reprocesos controlados siguen permitidos.
- Falta UAT formal y firmas.

## 3. Alcance UAT
- incoming ACH.
- incoming CENIT.
- archivo canonical nuevo.
- archivo duplicado mismo hash/tamaño.
- reproceso permitido.
- devolución linked/applied Ruta A.
- devolución linked/applied Ruta B.
- NotFound.
- Ambiguous.
- RejectedTotal.
- RejectedPartial.
- huérfana resuelta manualmente audit-only.
- duplicado orphan.
- relación con outbound `ReturnFileGenerated`.
- relación con ROR.
- conciliación.

## 4. Checklist técnico por ruta incoming

| ID | Ruta | Control | Resultado esperado | Evidencia | Estado | Observaciones |
|---|---|---|---|---|---|---|
| RTA-01 | Ruta A | procesa addenda 7/99 | registros válidos procesados | logs + evidencia UAT | Pendiente | |
| RTA-02 | Ruta A | decide Accepted / RejectedTotal / RejectedPartial | decisión correcta por contenido | resultado de ingesta | Pendiente | |
| RTA-03 | Ruta A | cambia a ReturnedByEpr solo en aplicadas | solo aplicadas cambian estado | DB before/after | Pendiente | |
| RTA-04 | Ruta A | crea `AchTransactionStateEvent` `IncomingReturnApplied` | evento por aplicadas | DB + payload | Pendiente | |
| RTA-05 | Ruta A | no crea evento si RejectedTotal | cero eventos | DB query | Pendiente | |
| RTA-06 | Ruta A | RejectedPartial crea evento solo para aplicadas | cardinalidad correcta | DB query | Pendiente | |
| RTA-07 | Ruta A | duplicado intra-archivo no duplica evento | evento único | DB query | Pendiente | |
| RTB-01 | Ruta B | carga archivo | ingesta creada | `IncomingNachaFileIngestion` | Pendiente | |
| RTB-02 | Ruta B | valida hash/tamaño | fingerprint persistido | DB + evidencia | Pendiente | |
| RTB-03 | Ruta B | resuelve cámara/ciclo | resolución persistida | `ResolutionEvidenceJson` | Pendiente | |
| RTB-04 | Ruta B | valida nombre externo inbound si aplica | policy aplicada | logs/payload | Pendiente | |
| RTB-05 | Ruta B | parsea | parse result persistido | processing result | Pendiente | |
| RTB-06 | Ruta B | clasifica | clasificación persistida | `IncomingNachaEntryClassification` | Pendiente | |
| RTB-07 | Ruta B | linkea | link final/no resuelto persistido | `IncomingNachaTransactionLink` | Pendiente | |
| RTB-08 | Ruta B | aplica estado vía `AchStateTransitionService` cuando corresponde | transición auditada | `AchTransactionStateEvent` | Pendiente | |
| RTB-09 | Ruta B | NotFound/Ambiguous quedan manual review/no resuelto | sin cambio de estado | link + events | Pendiente | |
| RTB-10 | Ruta B | genera `IncomingNachaProcessingEvent` | eventos de pipeline | DB query | Pendiente | |

## 5. Checklist funcional UAT

| ID | Caso UAT | Cámara | Entrada | Resultado esperado | Evidencia | Estado |
|---|---|---|---|---|---|---|
| UAT-01 | ACH incoming válido aplicado | ACH | archivo válido | aplicado correctamente | payload + DB | Pendiente |
| UAT-02 | CENIT incoming válido aplicado | CENIT | archivo válido | aplicado correctamente | payload + DB | Pendiente |
| UAT-03 | Ruta A aplica ReturnedByEpr con evento IncomingReturnApplied | ACH/CENIT | addenda aplicable | estado+evento | DB + payload | Pendiente |
| UAT-04 | Ruta B aplica estado con AchStateTransitionService | ACH/CENIT | linked final | estado+evento | DB + payload | Pendiente |
| UAT-05 | RejectedTotal no cambia estado ni crea evento de estado | ACH/CENIT | archivo inválido total | sin transición | DB + decisión | Pendiente |
| UAT-06 | RejectedPartial aplica solo registros válidos | ACH/CENIT | mezcla válida/inválida | parcial correcto | DB + conteos | Pendiente |
| UAT-07 | NotFound queda no resuelto | ACH/CENIT | trace inexistente | orphan persistida | link+event | Pendiente |
| UAT-08 | Ambiguous queda en revisión manual | ACH/CENIT | múltiples candidatos | revisión manual | link+event | Pendiente |
| UAT-09 | Archivo duplicado mismo contenido distinto nombre retorna Duplicado | ACH/CENIT | mismo hash/size | `Duplicado` | response + DB | Pendiente |
| UAT-10 | Reproceso autorizado no es bloqueado | ACH/CENIT | `IsReprocess=true` | permitido | DB query | Pendiente |
| UAT-11 | Huérfana NotFound se cierra audit-only | ACH/CENIT | resolución manual | evento manual | event payload | Pendiente |
| UAT-12 | Huérfana Ambiguous se cierra audit-only preservando candidatos | ACH/CENIT | resolución manual | candidatos preservados | payload | Pendiente |
| UAT-13 | Segunda resolución manual retorna AlreadyResolved | ACH/CENIT | doble llamada | idempotente | result + events | Pendiente |
| UAT-14 | Incoming applied no se confunde con outbound generated | ACH/CENIT | flujo mixto | trazas separadas | reportes/evidencia | Pendiente |
| UAT-15 | ROR no se habilita por huérfanas no aplicadas | ACH/CENIT | orphan unresolved | no habilita ROR | evidencia funcional | Pendiente |
| UAT-16 | Conciliación distingue applied/orphan/rejected/duplicate | ACH/CENIT | set mixto | clasificación correcta | reporte | Pendiente |

## 6. Checklist de archivo incoming e idempotencia

| ID | Control | Regla actual | Resultado esperado | Evidencia | Estado |
|---|---|---|---|---|---|
| IDP-01 | FileHashSha256 calculado | obligatorio | hash persistido | DB | Pendiente |
| IDP-02 | FileSize calculado | obligatorio | tamaño persistido | DB | Pendiente |
| IDP-03 | índice único parcial canonical | activo | constraint operativo | metadata/migración | Pendiente |
| IDP-04 | filtro IsReprocess=false | activo | aplica solo canonical | metadata | Pendiente |
| IDP-05 | duplicate canonical bloqueado | enforced | no 2 canonical | test/evidencia | Pendiente |
| IDP-06 | duplicate upload retorna Duplicado | funcional | respuesta duplicado | response | Pendiente |
| IDP-07 | parser no corre en duplicado | optimización/idempotencia | no parse 2º upload | logs/mock | Pendiente |
| IDP-08 | reproceso IsReprocess=true permitido | controlado | crea fila reprocess | DB | Pendiente |
| IDP-09 | ParentIngestionId preservado si aplica | trazabilidad | parent correcto | DB | Pendiente |
| IDP-10 | no duplicate processing | idempotencia | no doble ejecución | events/DB | Pendiente |
| IDP-11 | no duplicate orphan events | idempotencia | evento único | DB query | Pendiente |

## 7. Checklist de aplicación de devolución entrante

| ID | Control | Ruta | Resultado esperado | Evidencia | Estado |
|---|---|---|---|---|---|
| APP-01 | estado previo capturado | A/B | `previousState` presente | payload | Pendiente |
| APP-02 | estado final ReturnedByEpr/ReturnedByOperator según ruta | A/B | transición correcta | DB + payload | Pendiente |
| APP-03 | FromState/ToState en evento | A/B | campos completos | event record | Pendiente |
| APP-04 | ReasonCode | A/B | causal presente | payload | Pendiente |
| APP-05 | PayloadJson | A/B | parseable y estructurado | JSON parse | Pendiente |
| APP-06 | source | A/B | source correcto | payload | Pendiente |
| APP-07 | trace original | A/B | trace presente | payload | Pendiente |
| APP-08 | fileName/fileHash | A/B | metadata presente | payload | Pendiente |
| APP-09 | causal | A/B | causal normalizada | payload | Pendiente |
| APP-10 | cámara/ciclo | A/B | contexto operativo | payload | Pendiente |
| APP-11 | no evento duplicado | A/B | cardinalidad 1x | DB | Pendiente |
| APP-12 | no estado cuando no hay linking | B | sin transición | DB | Pendiente |

## 8. Checklist de huérfanas/no resueltas

| ID | Caso | Criterio | Resultado esperado | Evidencia | Estado |
|---|---|---|---|---|---|
| ORP-01 | NotFound por trace inexistente | sin candidato | orphan persistida | link/event | Pendiente |
| ORP-02 | Ambiguous por múltiples candidatos | >1 candidato | revisión manual | link/event | Pendiente |
| ORP-03 | NoResuelto | sin cierre automático | requiere manual | link | Pendiente |
| ORP-04 | ManualReviewRequired=true | unresolved | bandera activa | clasificación | Pendiente |
| ORP-05 | EvidenceJson IncomingReturnUnresolved | unresolved | payload estándar | JSON | Pendiente |
| ORP-06 | candidateTransactionIds | ambiguous | candidatos presentes | JSON | Pendiente |
| ORP-07 | stateChanged=false | unresolved | sin cambio estado | JSON | Pendiente |
| ORP-08 | applied=false | unresolved | no aplicado | JSON | Pendiente |
| ORP-09 | no AchTransactionStateEvent | unresolved | sin evento de estado | DB | Pendiente |
| ORP-10 | no cambio de AchTransaction.State | unresolved | estado intacto | DB | Pendiente |
| ORP-11 | no duplicidad de LinkingBloqueado | idempotencia | evento único | DB | Pendiente |
| ORP-12 | no duplicidad de link orphan | idempotencia | link único | DB | Pendiente |
| ORP-13 | resolución manual audit-only disponible | operación | trazabilidad final | service/event | Pendiente |

## 9. Checklist de resolución manual audit-only

| ID | Control | Resultado esperado | Evidencia | Estado |
|---|---|---|---|---|
| MR-01 | servicio `IIncomingNachaOrphanManualResolutionService` | disponible | contrato/código | Pendiente |
| MR-02 | acción MarkAsIgnored | soportada | request/result | Pendiente |
| MR-03 | acción MarkAsRejected | soportada | request/result | Pendiente |
| MR-04 | acción KeepPending | soportada | request/result | Pendiente |
| MR-05 | acción LinkToTransaction como audit-only | soportada sin aplicar estado | payload/event | Pendiente |
| MR-06 | ResolvedBy obligatorio | validación funcional | resultado Invalid | Pendiente |
| MR-07 | comentario/razón/correlación | persistidos | payload | Pendiente |
| MR-08 | evento OrphanManualResolution | creado | processing event | Pendiente |
| MR-09 | payload IncomingReturnManualResolved | estructurado | JSON parse | Pendiente |
| MR-10 | manualReviewRequired=false | cierre operativo | payload | Pendiente |
| MR-11 | previousResolutionReason | preservado | payload | Pendiente |
| MR-12 | candidateTransactionIds preservados | preservación evidencia | payload | Pendiente |
| MR-13 | resolvedAchTransactionId si aplica | trazabilidad | payload | Pendiente |
| MR-14 | stateChanged=false | audit-only | payload | Pendiente |
| MR-15 | applied=false | audit-only | payload | Pendiente |
| MR-16 | achTransactionStateEventCreated=false | audit-only | payload | Pendiente |
| MR-17 | AlreadyResolved en segunda llamada | idempotencia | resultado | Pendiente |
| MR-18 | no contabilidad | audit-only | evidencia operativa | Pendiente |
| MR-19 | no aplicación automática | audit-only | evidencia funcional | Pendiente |

**Nota explícita:** `LinkToTransaction` en este alcance es vinculación/auditoría operacional, **no** aplicación de estado ni contabilidad.

## 10. Checklist de trazabilidad

| Pregunta de auditoría | Fuente actual | Evidencia esperada | Estado | Brecha |
|---|---|---|---|---|
| qué archivo llegó | IncomingNachaFileIngestion | fileName | Pendiente | |
| quién lo cargó | IncomingNachaFileIngestion | uploadedBy | Pendiente | |
| cuándo | IncomingNachaFileIngestion | uploadedAtUtc | Pendiente | |
| hash | IncomingNachaFileIngestion | fileHashSha256 | Pendiente | |
| tamaño | IncomingNachaFileIngestion | fileSize | Pendiente | |
| cámara | ingestión/resolución | clearingHouseId | Pendiente | |
| ciclo | ingestión/resolución | achCycleId | Pendiente | |
| causal | classification/evidence | returnReasonCode | Pendiente | |
| trace original | addenda/evidence | originalTraceNumber | Pendiente | |
| entry detail | link/event | entryDetailId | Pendiente | |
| addenda | link/event | addendaId | Pendiente | |
| transacción original | link | achTransactionId | Pendiente | |
| estado previo | state event | previousState | Pendiente | |
| estado nuevo | state event | newState | Pendiente | |
| evento | state event | eventType | Pendiente | |
| processing event | IncomingNachaProcessingEvent | eventType/status | Pendiente | |
| huérfana | link/event | IncomingReturnUnresolved | Pendiente | |
| candidatos | link evidence | candidateTransactionIds | Pendiente | |
| resolución manual | manual service/event | OrphanManualResolution | Pendiente | |
| resolvedBy | manual payload | resolvedBy | Pendiente | |
| resolvedAtUtc | manual payload | resolvedAtUtc | Pendiente | |
| comentario | manual payload | comment | Pendiente | |
| duplicado | ingestion status | Duplicado | Pendiente | |
| reproceso | ingestion row | IsReprocess/ParentIngestionId | Pendiente | |
| conciliación | reporte operativo | estado consolidado | Pendiente | definición final |

## 11. Checklist de pruebas técnicas

| ID | Suite/Test | Cobertura | Resultado esperado | Evidencia | Estado |
|---|---|---|---|---|---|
| TST-01 | AchIncomingReturnApplicationAndOrphanCharacterizationTests | Ruta A/B + orphan characterization | verde | reporte test | Pendiente |
| TST-02 | IncomingNachaDuplicateFileAndOrphanIdempotencyTests | duplicate file + orphan idempotencia | verde | reporte test | Pendiente |
| TST-03 | IncomingNachaOrphanManualResolutionServiceTests | manual resolution audit-only | verde | reporte test | Pendiente |
| TST-04 | IncomingNachaFileIngestionConfigurationTests | índice canonical partial unique | verde | reporte test | Pendiente |
| TST-05 | IncomingNachaIngestionAppServiceTests | ingesta, duplicado, reproceso | verde | reporte test | Pendiente |
| TST-06 | IncomingNachaPostParseProcessorTests | notfound/ambiguous/linking/applied | verde | reporte test | Pendiente |
| TST-07 | IncomingNachaTransactionLinkerTests | linking exact/notfound/ambiguous | verde | reporte test | Pendiente |

Filtros de ejecución referencia:
- `IncomingReturn|IncomingNacha|Orphan|Unresolved|PostParse|Linker|StateEvent|Idempotency|ManualResolution`
- `AchReturns|ReturnOfReturn|CauseCode|Nacha|ExternalFileName`

## 12. Relación con outbound
- `ReturnFileGenerated` outbound es generación técnica saliente.
- `Incoming applied` es aplicación de devolución entrante.
- No deben confundirse.
- incoming duplicate/orphan no debe invalidar outbound sin proceso formal.
- conciliación debe distinguir `GeneratedNotTransmitted` vs `IncomingApplied`.
- hash/eventos deben cruzarse en reportes futuros.

## 13. Relación con ROR
- ROR requiere linaje claro.
- huérfanas no aplicadas no habilitan ROR.
- resolución manual audit-only tampoco habilita ROR por sí sola.
- ROR futuro debe verificar applied/lineage, no solo link audit-only.
- `candidateTransactionIds` ayudan a revisión pero no son aplicación.

## 14. Relación con conciliación/contabilidad
- incoming applied puede afectar conciliación operativa.
- huérfana unresolved no debe afectar contabilidad.
- manual resolution audit-only no genera asiento.
- se requiere definición posterior para aplicación contable/manual.
- reportar por estado: applied, duplicate, orphan, manual resolved audit-only, rejected total/partial.

## 15. Evidencia requerida para UAT
- archivo NACHA incoming.
- hash SHA-256.
- tamaño.
- registro IncomingNachaFileIngestion.
- registro IncomingNachaProcessingEvent.
- registro IncomingNachaTransactionLink.
- EvidenceJson IncomingReturnUnresolved.
- EvidenceJson IncomingReturnManualResolved.
- AchTransactionStateEvent para aplicadas.
- prueba duplicado canonical.
- prueba reproceso.
- prueba NotFound.
- prueba Ambiguous.
- prueba AlreadyResolved.
- acta UAT.
- firma negocio.
- firma operaciones.
- firma riesgo/compliance.
- aprobación tecnología.

## 16. Criterios de salida de NO-GO productivo
1. UAT incoming ACH completado.
2. UAT incoming CENIT completado.
3. Ruta A validada con evento.
4. Ruta B validada con evento.
5. RejectedTotal validado.
6. RejectedPartial validado.
7. NotFound validado.
8. Ambiguous validado.
9. Payload IncomingReturnUnresolved validado.
10. Payload IncomingReturnManualResolved validado.
11. Idempotencia canonical validada.
12. Reprocesos controlados validados.
13. No duplicidad de LinkingBloqueado validada.
14. No duplicidad de link orphan validada.
15. Resolución manual audit-only validada.
16. AlreadyResolved validado.
17. No impacto outbound.
18. No impacto ROR.
19. Conciliación definida.
20. Contabilidad definida para aplicación real/manual futura.
21. Command center validado.
22. Reportes operativos definidos.
23. Firma negocio.
24. Firma operaciones.
25. Firma riesgo/compliance.
26. Aprobación técnica.
27. Scorecard actualizado.

## 17. Riesgos residuales
- resolución manual actual es audit-only, no aplicación real.
- LinkToTransaction no equivale a cambio de estado.
- no hay asiento contable en manual resolution.
- command center/UI puede requerir cierre adicional.
- reportes de conciliación aún pendientes.
- UAT con cámaras pendiente.
- riesgo de confundir “manual resolved” con “applied”.
- **NO-GO productivo se mantiene**.

## 18. Decisión vigente
- **GO técnico:** sí.
- **GO UAT controlado:** sí.
- **NO-GO productivo:** sí.
- Próximo hito después del checklist: decidir si se implementa aplicación manual real a transacción o se pasa a conciliación/reportes.
- No se habilita producción sin UAT y firmas.

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

> Referencia punto 10 (reportería/conciliación revisión contable terceros, no contable): `docs/audits/accounting-review-reconciliation-matrix-current.md`.

- Referencia checklist UAT punto 10: `docs/uat/accounting-review-reconciliation-acceptance-checklist.md`.
