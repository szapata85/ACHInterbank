# Matriz vigente — Devolución entrante E2E y huérfanas

> Referencia UAT complementaria: `docs/uat/incoming-return-orphan-acceptance-checklist.md`.


## 1. Propósito
Esta matriz consolida el estado vigente de devolución entrante en ACHInterbank para:
- aplicación E2E de devolución entrante;
- rutas incoming coexistentes;
- estado de transacción;
- eventos de estado;
- huérfanas/no resueltas;
- linking;
- idempotencia;
- trazabilidad;
- rechazo total/parcial;
- command center;
- relación con outbound;
- relación con ROR;
- conciliación/contabilidad;
- criterios de salida de NO-GO productivo.

## 2. Estado actual
- **GO técnico:** sí.
- **GO UAT controlado:** sí.
- **NO-GO productivo:** sí.
- Incoming tiene capacidad funcional técnica.
- Existen dos rutas incoming coexistentes.
- La auditoría de estado no es homogénea entre rutas.
- La Ruta A cambia estado sin evento de estado transaccional.
- La Ruta B cambia estado con evento de estado transaccional.
- Huérfanas/no resueltas se preservan con evidencia, pero la resolución manual final E2E no está cerrada.
- Idempotencia de archivo existe en Ruta B (hash+tamaño).
- Idempotencia por registro/huérfana/multiarchivo/multinodo requiere hardening.

## 3. Fuentes revisadas

| Fuente | Ruta / clase | Tipo | Alcance | Estado | Observaciones |
|---|---|---|---|---|---|
| Servicio ingesta incoming (Ruta A) | `AchIncomingReturnIngestionService` | Código | Addenda 7/99, decisión y update estado directo | Vigente | Cambia estado a `ReturnedByEpr` sin `AchTransactionStateEvent` en esta ruta. |
| Ingesta app incoming (Ruta B) | `IncomingNachaIngestionAppService` | Código | Upload, hash/tamaño, resolución ciclo/cámara, naming, parse | Vigente | Detecta duplicado por hash+tamaño. |
| Post-proceso incoming (Ruta B) | `IncomingNachaPostParseProcessor` | Código | Clasificación, linking, efectos de negocio, processing events | Vigente | Usa transición de estado y conserva evidencia operativa. |
| Linker incoming | `IncomingNachaTransactionLinker` | Código | NotFound/Ambiguous/Exact | Vigente | NotFound/Ambiguous quedan no resueltos/manual review. |
| API upload | `NachaUploadController` | Código | Entrada API y respuesta de ingesta | Vigente | Expone estados de ingesta y errores. |
| Parser NACHA | `NachaParserService` | Código | Parseo y persistencia técnica | Vigente | Integrado en Ruta B. |
| Validador semántico | `NachaSemanticValidator` | Código | Reglas semánticas de parsing | Vigente | Parte de validación de estructura/semántica del flujo parser. |
| Servicio de transición | `AchStateTransitionService` | Código | Cambio estado con auditoría | Vigente | Crea `AchTransactionStateEvent` con payload. |
| Evento estado transaccional | `AchTransactionStateEvent` | Entidad | Trazabilidad de transición | Vigente | Cobertura efectiva en Ruta B aplicada; no homogénea con Ruta A. |
| Evento de procesamiento incoming | `IncomingNachaProcessingEvent` | Entidad | Evidencia operativa de pipeline | Vigente | Base para command center/observabilidad. |
| Vínculo incoming-transacción | `IncomingNachaTransactionLink` | Entidad | EvidenceJson, tipo de vínculo y finalización | Vigente | Registra NotFound/Ambiguous/NoResuelto. |
| Caracterización incoming/orphan | `AchIncomingReturnApplicationAndOrphanCharacterizationTests` | Tests | Evidencia de comportamiento actual | Vigente | Confirma diferencia de auditoría entre rutas e idempotencia hash+tamaño. |
| Matriz causales | `docs/audits/cause-code-normative-matrix-ach-cenit-sta-current.md` | Documento | Causales por cámara/flujo | Vigente | Referencia para causal incoming y límites UAT. |
| Matriz registros NACHA | `docs/audits/nacha-record-level-normative-matrix-ach-cenit-current.md` | Documento | Registros 1/5/6/7/8/9 | Vigente | Referencia para record 7/addenda 99. |
| Matriz naming | `docs/audits/external-filename-normative-matrix-ach-cenit-sta-current.md` | Documento | Naming externo inbound/outbound | Vigente | Ruta B integra policy inbound; NO-GO productivo se mantiene. |
| Matriz outbound trazabilidad | `docs/audits/outbound-return-state-traceability-matrix-current.md` | Documento | Estado/evento outbound y relación incoming | Vigente | Diferenciar generated outbound vs applied incoming. |

## 4. Rutas incoming actuales

| Ruta | Entrada | Parser/Linking | Estado transacción | Evento estado | Huérfanas | Idempotencia | Uso esperado | Riesgo |
|---|---|---|---|---|---|---|---|---|
| **Ruta A — `AchIncomingReturnIngestionService`** | Addenda 7/99 (raw content) | Parse básico 7/99 + vínculo por trace/original trace | Cambia directo a `ReturnedByEpr` cuando aplica | No crea `AchTransactionStateEvent` en esta ruta | Registra no linked/fallas | Dedup intra-archivo | Ingesta centralizada de devolución entrante por addenda | Auditoría de estado no homogénea + idempotencia histórica incompleta |
| **Ruta B — `IncomingNachaIngestionAppService` + `IncomingNachaPostParseProcessor` + `IncomingNachaTransactionLinker`** | Upload archivo NACHA | Hash+tamaño, resolución ciclo/cámara, naming inbound, parser, classifier, linker | Cambia estado vía `AchStateTransitionService` cuando aplica | Sí crea `AchTransactionStateEvent` al aplicar transición | NotFound/Ambiguous/NoResuelto/manual review | Duplicado de archivo por hash+tamaño | Pipeline operacional + command center | Hardening pendiente para idempotencia por registro/huérfana/multiarchivo |

## 5. Flujo E2E actual
1. recepción/upload;
2. validación archivo;
3. hash/tamaño;
4. idempotencia de archivo;
5. resolución ciclo/cámara;
6. validación naming externo inbound;
7. parseo NACHA;
8. clasificación funcional;
9. extracción causal/trace;
10. linking;
11. validación causal/regulatoria;
12. decisión accepted/rejected/orphan;
13. actualización estado si aplica;
14. evento de estado si aplica;
15. processing events;
16. evidencia command center;
17. respuesta al API.

| Paso | Componente | Acción | Evidencia actual | Brecha | Severidad | Próxima acción |
|---|---|---|---|---|---|---|
| 1 | `NachaUploadController` | Recibe archivo y metadatos | Request/response API | N/A | Baja | Mantener |
| 2 | `NachaUploadController` | Valida extensión/tamaño/content-type | Errores explícitos API | N/A | Baja | Mantener |
| 3 | `IncomingNachaIngestionAppService` | Calcula hash+tamaño | `FileHashSha256` + `FileSize` | Homologar uso entre rutas | Media | Estándar común |
| 4 | `IncomingNachaIngestionAppService` | Duplicado por hash+tamaño | Estado `Duplicado` + processing result | No cubre idempotencia por registro aplicado | Alta | Hardening DB-first |
| 5 | `IIncomingNachaCycleResolver` | Resuelve cámara/ciclo | `ResolutionEvidenceJson` + estados | Ambigüedad requiere operación manual | Media | Fortalecer runbook |
| 6 | `IExternalFileNamePolicy` | Valida naming inbound | Outcome de policy | Alinear evidencia con Ruta A | Media | Homologar payload |
| 7 | `NachaParserService` | Parsea y persiste | Totales + fallas parser | N/A | Baja | Mantener |
| 8 | `IncomingNachaPostParseProcessor` | Clasifica funcionalmente | `IncomingNachaEntryClassification` + events | N/A | Baja | Mantener |
| 9 | Classifier/Addenda | Obtiene causal/trace | Classification fields + addenda | N/A | Baja | Mantener |
| 10 | `IncomingNachaTransactionLinker` | Vincula original | `IncomingNachaTransactionLink` + `EvidenceJson` | Resolución manual final incompleta | Alta | Trail manual final |
| 11 | `IAchRegulatoryCatalogService` / cause policy | Valida causal/policy | Failures y bloqueos | Homologar traza cross-ruta | Media | Estandarización |
| 12 | Ruta A/B | Decide accepted/rejected/orphan | Decision audit (A) + events/links (B) | Semántica no homogénea | Alta | Matriz de decisión común |
| 13 | Ruta A/B | Actualiza estado | Estado final en `AchTransaction` | Ruta A sin evento de estado | **P0** | Evento auditado en Ruta A |
| 14 | `AchStateTransitionService` (Ruta B) | Crea evento de estado | `AchTransactionStateEvent` | Ruta A no equivalente | **P0** | Homologar ambas rutas |
| 15 | `IncomingNachaProcessingEvent` | Registra procesamiento | Timeline operacional | Faltan tipos finales manual resolution | Alta | Cerrar catálogo de eventos |
| 16 | Command center | Consulta detalle/cola/eventos | Endpoints y DTOs observabilidad | Resolver cierre manual E2E | Alta | Feature resolución manual trazable |
| 17 | API response | Entrega outcome | Estado/errores/totales | Unificar contrato inter-rutas | Media | Contrato estándar incoming |

## 6. Matriz de aplicación sobre transacción original

| Escenario | Ruta | Estado inicial | Estado final actual | Evento actual | Evidencia | Brecha | Severidad |
|---|---|---|---|---|---|---|---|
| Devolución linked aplicada | Ruta A | Pending | ReturnedByEpr | No | Decision/Audit interno | Estado sin evento | **P0** |
| Devolución linked aplicada | Ruta B | Pending | ReturnedByEpr o ReturnedByOperator | Sí | `AchTransactionStateEvent` + processing event | Homologar con Ruta A | Alta |
| NotFound | Ruta B | Sin cambio | Sin cambio | No evento de estado; sí processing/link event | `IncomingNachaTransactionLink.EvidenceJson` | Falta cierre manual final | Alta |
| Ambiguous | Ruta B | Sin cambio | Sin cambio | No evento de estado; sí processing/link event | EvidenceJson con candidatos | Falta cierre manual final | Alta |
| RejectedTotal | Ruta A | Pending | Sin cambio | No | Failures + decision | Eventing/reasoning estandarizado | Media |
| RejectedPartial | Ruta A | Mixto | Solo válidas aplicadas | No | Failures + counts | Estado aplicado sin evento | **P0** |
| Duplicate file | Ruta B | N/A | N/A | No evento de estado | Estado `Duplicado` + processing result | Estándar de evento funcional | Media |
| Duplicate intra-archivo | Ruta A | Pending | Evita doble aplicación | No | `INCOMING_RETURN_DUPLICATE_IN_FILE` | Falta dedup histórico DB-first | Alta |

## 7. Matriz de huérfanas / no resueltas

| Caso | Criterio | Estado actual | Evidencia preservada | ¿Tiene evento? | ¿Resolución manual final? | Brecha | Próxima acción |
|---|---|---|---|---|---|---|---|
| NotFound por original trace inexistente | No candidato único | NoResuelto/Manual review | `originalTrace`, `returnReasonCode`, `evidenceJson`, `fileId`, `entry/addenda`, `createdAt` | Sí, processing event | No cerrada | Falta evento final + actor + linkage final | Implementar `ManualResolved` audit trail |
| Ambiguous por múltiples candidatos | >1 candidato viable | Bloqueada/Manual review | `originalTrace`, `returnReasonCode`, candidatos en `evidenceJson`, contexto de archivo | Sí, processing event | No cerrada | Falta decisión final auditable | Flujo resolución manual con evidencia |
| NoResuelto base | No hay link final | `NoResuelto` base | Link base con `EvidenceJson` pendiente | Puede existir evento de pipeline | No cerrada | Semántica final no consolidada | Estándar de lifecycle orphan |
| PendienteResolucion | Ciclo/cámara no resuelto | Ingestion bloqueada/pendiente | resolution evidence + warnings/errors | Sí, processing result | No aplica (aún) | Operación manual no completa E2E | Runbook + eventos finales |
| Revisión manual | Clasificación/link bloqueado | Requiere acción humana | events + link evidence + metadata ingesta | Sí, parciales | No cerrada | Falta evento final auditable | Feature manual resolution |
| Transacción aparece después | Late arrival | No automatizado integral | evidencia histórica parcial | No garantizado | No cerrada | Reproceso/control pendiente | Política de re-link/reprocess |
| Mismo registro llega dos veces | Duplicate lógico | Cobertura parcial (A intra-file / B file-level) | failures/resultados por ruta | Parcial | No | Falta dedup histórico por registro | Unique/index + reglas |
| Misma huérfana en dos archivos | Multiarchivo | No endurecido | evidencia por archivo separada | Parcial | No | Riesgo duplicidad y ruido operativo | Idempotencia orphan DB-first |

**Campos de evidencia mínima esperada (objetivo de hardening):**
`originalTrace`, `returnReasonCode`, `fileName`, `fileHash`, `clearingHouseId`, `achCycleId`, `entry detail`, `addenda`, `evidenceJson`, `createdAt`, `requestedBy/uploadedBy`.

## 8. Matriz de eventos de estado incoming

| Evento funcional | Ruta actual | AchTransactionStateEvent existe | FromState | ToState | Source | ReasonCode | Payload | Brecha |
|---|---|---|---|---|---|---|---|---|
| IncomingReturnApplied | Ruta B | Sí | Sí | Sí | Sí | Sí | Sí | Homologar con Ruta A |
| IncomingReturnApplied | Ruta A | No | N/A | N/A | N/A | N/A | N/A | **P0: aplica estado sin evento** |
| IncomingReturnRejected | Ruta A/B | No homogéneo | Parcial | Parcial | Parcial | Parcial | Parcial | Definir contrato de evento funcional |
| IncomingReturnUnresolved | Ruta B | No (estado no cambia) | N/A | N/A | N/A | Parcial | En processing events | Estandarizar evento funcional unresolved |
| IncomingReturnAmbiguous | Ruta B | No (estado no cambia) | N/A | N/A | N/A | Parcial | En processing events | Estandarizar evento funcional ambiguous |
| IncomingReturnManualResolved | Ruta B | No cerrado | N/A | N/A | N/A | N/A | N/A | Falta implementación E2E |
| IncomingDuplicateIgnored | Ruta A/B | No homogéneo | N/A | N/A | N/A | Parcial | Failure/result | Estandarizar audit event |
| IncomingFileDuplicateDetected | Ruta B | No | N/A | N/A | N/A | N/A | processing result | Puede formalizarse evento funcional |

## 9. Matriz de rechazo total/parcial

| Escenario | Ruta | Decisión actual | Código/categoría | Persistencia | Evento | Riesgo | Próxima acción |
|---|---|---|---|---|---|---|---|
| Accepted | Ruta A | `Accepted` | Rxx/DEVxx válidas | Audit result | No | Aplicación sin evento | Emitir evento funcional |
| RejectedTotal | Ruta A | `RejectedTotal` | Archivo vacío / 0 linked / rechazo regulatorio total | Failures + decision | No | Trazabilidad no homogénea | Estandarizar payload |
| RejectedPartial | Ruta A | `RejectedPartial` | Mixto válido+falla | Failures + decision + count | No | Parcial aplicado sin evento | Evento por transacción aplicada |
| Dxx/Ixxx | Ruta B / políticas | Rechazo técnico/operacional | Dxx/Ixxx | processing/events/catálogo | Parcial | Debe mantenerse separado de Rxx/DEVxx | Matriz de códigos por flujo |
| Rxx/DEVxx return reason | Ruta A/B | Aplicación/bloqueo según policy | Rxx/DEVxx | A: audit interno; B: transitions/events | No homogéneo | Divergencia de auditoría | Homologar trazabilidad |
| Conteo linked vs failed | Ruta A | Decision basado en conteo | Failures + linked counts | Sí en resultado | No | Doble semántica inter-ruta | Métrica unificada |
| Registro inválido | Ruta A/B | Falla de parse/policy/link | Failure stage/codes | Sí | Parcial | No taxonomía única | Estandarizar códigos |
| Archivo inválido | Ruta B | Bloqueado/Fallido | Parser/Policy errors | Sí | No | Operación manual intensiva | Endurecer controles previos |
| Regla regulatoria rechazada | Ruta A/B | Rechazo/bloqueo | Policy/code rejected | Sí | Parcial | Falta correlación cross-ruta | Evidencia unificada |

## 10. Matriz de idempotencia incoming

| Control | Existe hoy | Ruta | Clave | Cubre multiarchivo | Cubre multiinstancia | Riesgo | Próxima acción |
|---|---|---|---|---|---|---|---|
| Hash+tamaño de archivo | Sí | B | `FileHashSha256 + FileSize` | Parcial | Parcial | No dedup por registro aplicado | Extender a control DB-first integral |
| Filename | Parcial | B | `FileName` (informativo) | No | No | Mismo contenido distinto nombre | No usar como único control |
| External filename | Parcial | B | Policy outcome | No | No | No equivale idempotencia lógica | Separar naming de idempotencia |
| Original trace + reason | Parcial | A | Clave funcional intra-file | No | No | Duplicado histórico posible | Constraint/tabla histórica |
| Entry/addenda sequence | Parcial | B | Contexto linkage | Parcial | No | Reproceso multiarchivo | Fingerprint por registro |
| TransactionId + reason + fileId | Parcial | A/B | No estándar único | Parcial | No | Divergencia inter-ruta | Modelo canónico incoming item |
| Dedup intra-archivo | Sí | A | Set en memoria por request | No | No | Multiarchivo/multinodo | Persistir huella histórica |
| Dedup histórico | No completo | A/B | N/A | No | No | Duplicidad operacional | DB-first hardening |
| Unique constraint DB | Parcial (otros dominios) | A/B | No específico integral incoming return item | Parcial | Parcial | Carrera concurrente | Índices únicos dedicados |
| Lock | Parcial | A/B | In-process / no uniforme | No | No | Condiciones de carrera | Lock distribuido o DB isolation |
| Retry seguro | Parcial | B | Reprocess controlado por parent/hash | Parcial | Parcial | Reintentos de orphan no cerrados | Política retry por orphan |
| Duplicate file | Sí | B | hash+tamaño | Sí (si mismo contenido) | Parcial | Puede quedar gap multi-nodo | Reforzar transaccionalidad |
| Duplicate orphan | No completo | B | N/A | No | No | Ruido y backlog manual | Clave orphan canónica |
| Duplicate applied return | Parcial | A/B | Dedup parcial | Parcial | No | Doble update/event posible según ruta | Endurecer por DB-first |

## 11. Matriz de trazabilidad incoming

| Pregunta de auditoría | Respuesta actual | Fuente actual | Brecha | Evidencia requerida |
|---|---|---|---|---|
| Qué archivo llegó | Sí | Ingestion/file metadata | Homologar entre rutas | FileName + FileId |
| Quién lo cargó | Sí | UploadedBy/RequestedBy | Homologar requestedBy final | Actor único auditable |
| Cuándo | Sí | UploadedAt/ReceivedAt | Unificar timestamp principal | timeline canónico |
| Hash | Sí (Ruta B) | FileHashSha256 | Ruta A no homogénea persistida | Hash estándar por flujo |
| Tamaño | Sí (Ruta B) | FileSize | Ruta A no homogénea | tamaño común |
| Cámara | Sí parcial | Resolver/AchCycle | Casos no resueltos | campo obligatorio cuando exista |
| Ciclo | Sí parcial | ResolvedAchCycleId | No siempre resuelto | fallback y señalización |
| Causal | Sí | classification/audit | Taxonomía unificada | catálogo de causal |
| Trace original | Sí parcial | Addenda/classification/link | Variabilidad por ruta | campo canonical |
| Trace devolución | Parcial | Entry sequence/trace | No estándar único | mapping canonical |
| Transacción original | Parcial | Linker / originalTx | Ambiguous/NotFound | resolved/unresolved state |
| Estado previo | Sí en Ruta B evento | state event | Ruta A sin evento | registrar FromState siempre |
| Estado nuevo | Sí | tx.State | Ruta A sin evento | evento con ToState |
| Evento | Parcial | state events/processing events | No homogéneo | contrato único |
| Processing event | Sí (Ruta B) | IncomingNachaProcessingEvent | Cobertura parcial ruta A | extender a ruta A |
| Outcome | Sí | decision/status | Semántica doble inter-ruta | glosario único |
| Huérfana | Sí parcial | links + events | manual closure pendiente | estado orphan final |
| Manual reviewer | No completo | command center parcial | falta audit trail completo | reviewer + justification |
| Resolución final | No completa | N/A | brecha funcional | evento final manual |
| Rechazo total/parcial | Sí (Ruta A) | decision/failures | no homogéneo con Ruta B | modelo común |
| Warnings/policy | Parcial | warnings JSON/failures | persistencia homogénea pendiente | warning catalog |
| Conciliación | Parcial | reportes generales | no cierre E2E específico incoming | reporte incoming aplicado/orphan/rejected |

## 12. Relación con devolución saliente

| Punto | Estado actual | Riesgo | Próxima acción |
|---|---|---|---|
| `ReturnFileGenerated` outbound != incoming applied | Correcto a nivel conceptual | Confusión operativa si no se separan estados | Glosario y eventos diferenciados |
| Incoming puede afectar estado transacción | Sí (A/B) | Doble semántica returned/generated | Modelo de lifecycle integrado |
| generated/transmitted/accepted/returned | No completamente unificado | Lecturas erróneas en operación | Matriz transversal de estados |
| Relación hash/evento outbound | Parcial | Conciliación incompleta | Cruce por correlation/file hash |
| generated-not-transmitted vs incoming-returned | No cerrado | Descuadre operacional/reporting | Reporte de conciliación cruzada |

## 13. Relación con ROR

| Punto | Estado actual | Riesgo | Próxima acción |
|---|---|---|---|
| ROR requiere linaje claro | Reconocido | Linaje incompleto en ruta sin evento | Homologar payload/eventos incoming |
| Incoming aplicado puede base para ROR | Sí funcionalmente posible | Si falta evidencia de causal/source | payload estándar y reason lineage |
| Huérfanas no deberían habilitar ROR | Objetivo de control | Orphan mal resuelta podría contaminar ROR | reglas explícitas de elegibilidad |
| Duplicate incoming afecta ROR | Riesgo identificado | Doble semántica de origen | hardening idempotencia |
| Causal original/new reason preservadas | Parcial | incompleto cross-ruta | normalizar campos canónicos |
| Eventos/payload para trazabilidad futura | Parcial | auditoría insuficiente en Ruta A | evento auditado Ruta A |

## 14. Relación con conciliación/contabilidad

| Punto | Estado actual | Brecha | Severidad | Próxima acción |
|---|---|---|---|---|
| Incoming aplicado impacta estado | Sí | Sin modelo contable E2E en este alcance | Alta | Definir integración contable |
| Cierre contable E2E | No cerrado | Falta diseño/ejecución | Alta | Roadmap contable |
| Reporte de huérfanas | Parcial | No consolidado operativo | Alta | reporte orphan estándar |
| Reporte rejected total/partial | Parcial | No homogéneo inter-ruta | Media | dashboard unificado |
| Automático vs manual | Parcial | Falta distinción final | Alta | evento final manual |
| Reporte por archivo/cámara/ciclo/causal | Parcial | gaps en data contract | Media | esquema de reporte canonical |
| Conciliación contra outbound | Parcial | generated vs applied no totalmente consolidado | Alta | matriz reconciliación cruzada |

## 15. Brechas clasificadas

### P0 — Bloqueo técnico/productivo
- Ruta A aplica estado sin evento de estado transaccional.
- Idempotencia histórica de registros/huérfanas no DB-first.
- Resolución manual de huérfanas no cerrada con evento final.
- Posible duplicidad multiarchivo/multinodo.
- Riesgo de estado inconsistente entre rutas.

### P1 — UAT/control
- Command center/manual resolution con trazabilidad incompleta.
- Warnings/policies no necesariamente persistidos de forma homogénea.
- Hash/filename/evidence no homologado entre rutas.
- Falta checklist UAT específico incoming/orphan.
- Falta reporte consolidado de huérfanas/rechazos.

### P2 — Mejora
- Payload estándar incoming.
- UI refinada de operación incoming/orphan.
- Métricas operativas avanzadas.
- Reportes enriquecidos.
- Conciliación avanzada.
- Cierre contable integral.

## 16. Decisión recomendada
**Recomendación:** Opción **B** primero, luego **C/D** incremental:
1. tests de caracterización;
2. matriz incoming/orphan;
3. evento auditado para Ruta A;
4. payload/evidence estándar;
5. idempotencia DB-first por archivo/registro/huérfana;
6. resolución manual trazable;
7. checklist UAT;
8. luego mejoras UI/reportes.

No se recomienda rediseño total inmediato porque:
- elevaría riesgo de romper parser/linking/command center;
- hoy coexisten dos rutas y hay que converger incrementalmente;
- requiere UAT por cámara/operación (ACH/CENIT);
- impacta trazabilidad downstream en ROR/conciliación.

## 17. Plan de remediación por commits

| Commit | Objetivo | Archivos probables | Tests | Riesgo | Rollback |
|---|---|---|---|---|---|
| `test(incoming): characterize incoming return application and orphan behavior` | Congelar comportamiento actual | `tests/.../AchIncomingReturnApplicationAndOrphanCharacterizationTests.cs` y suites relacionadas | Unit tests incoming/linker/post-parse | Bajo | Revert de commit de tests |
| `docs(incoming): add incoming return E2E and orphan matrix` | Consolidar diagnóstico vigente | `docs/audits/incoming-return-e2e-orphan-matrix-current.md` + referencias cruzadas | Revisión documental | Bajo | Revert docs |
| `feat(incoming): add incoming return applied state event audit` | Homologar auditoría de estado en Ruta A | Servicio Ruta A + event payload | Unit/integration de state-event | Medio | Feature flag/revert |
| `feat(incoming): persist orphan traceability payload` | Persistencia canónica orphan | modelos incoming/orphan payload | Tests de persistencia/orphan fields | Medio | rollback esquema/lógica |
| `feat(incoming): harden incoming file and orphan idempotency` | DB-first idempotencia | índices/constraints + servicios | Concurrencia/idempotencia | Alto | rollback migración/feature |
| `test(incoming): add duplicate file/orphan idempotency tests` | Cobertura duplicados multiarchivo/multinodo | suites incoming idempotency | Tests concurrentes determinísticos | Medio | revert tests |
| `feat(incoming): add orphan manual resolution audit trail` | Cerrar E2E manual | command center + eventos + evidencias | Tests manual resolution | Alto | revert feature |
| `docs(uat): add incoming return and orphan acceptance checklist` | Cierre UAT documentado | `docs/uat/...incoming-orphan...` | Revisión QA/ops/compliance | Bajo | revert docs |

## 18. Criterios de salida de NO-GO productivo
- [ ] Ruta A crea evento de estado.
- [ ] Ruta B conserva evento de estado.
- [ ] Eventos tienen payload estandarizado.
- [ ] Huérfanas preservan evidencia mínima.
- [ ] Resolución manual crea evento final.
- [ ] Idempotencia archivo DB-first validada.
- [ ] Idempotencia registro/huérfana validada.
- [ ] Duplicate file no duplica procesamiento.
- [ ] Duplicate record no duplica estado/evento.
- [ ] NotFound/Ambiguous trazables.
- [ ] RejectedTotal/RejectedPartial trazables.
- [ ] Outbound no se afecta.
- [ ] ROR no se afecta.
- [ ] Conciliación reporta incoming applied/orphan/rejected.
- [ ] Contabilidad definida.
- [ ] UAT ACH cerrado.
- [ ] UAT CENIT cerrado.
- [ ] Firma negocio.
- [ ] Firma operaciones.
- [ ] Firma riesgo/compliance.
- [ ] Aprobación técnica.
- [ ] Scorecard actualizado.

## 19. Decisión vigente
- **GO técnico:** sí.
- **GO UAT controlado:** sí.
- **NO-GO productivo:** sí.
- Próximo hito técnico: evento auditado para Ruta A **o** payload canónico de huérfanas según riesgo operativo.
- Próximo hito documental: checklist UAT incoming/orphan.
