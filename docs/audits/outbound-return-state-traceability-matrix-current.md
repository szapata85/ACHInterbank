# Matriz vigente — Devolución saliente: estado, evento, trazabilidad e idempotencia

## 1. Propósito
Esta matriz consolida el estado actual de la devolución saliente normal en ACHInterbank, cubriendo:

- estado de transacción original;
- eventos de estado;
- trazabilidad funcional;
- trazabilidad técnica;
- idempotencia;
- auditoría de archivo;
- relación con ROR;
- relación con incoming;
- relación con conciliación/contabilidad;
- criterios de salida de NO-GO productivo.

## 2. Estado actual
- **GO técnico:** sí.
- **GO UAT controlado:** sí.
- **NO-GO productivo:** sí.
- `AchReturnGenerated` es la evidencia técnica actual de devolución generada.
- `AchTransaction.State` no cambia al generar devolución saliente.
- `AchTransactionStateEvent` no se crea en `GenerateReturnsFileAsync`.
- La idempotencia actual es suficiente para instancia única / flujo lógico básico.
- La idempotencia actual no es suficiente para multiinstancia sin constraint DB o control distribuido.
- No existe lifecycle completo de transmisión/aceptación/rechazo en `AchReturnGenerated`.
- No existe cierre contable explícito en este flujo.

## 3. Fuentes revisadas

| Fuente | Ruta / clase | Tipo | Alcance | Estado | Observaciones |
|---|---|---|---|---|---|
| Servicio principal outbound return | `AchReturnsService` | Código | Flujo de generación saliente | Vigente | Persiste `AchReturnGenerated`; no cambia estado ni crea evento. |
| Entidad de evidencia de generación | `AchReturnGenerated` | Código | Persistencia de devolución generada | Vigente | Cobertura parcial de trazabilidad operacional. |
| Entidad transaccional | `AchTransaction` | Código | Estado funcional de transacción original | Vigente | Estado no cambia al generar archivo de devolución. |
| Evento de estado | `AchTransactionStateEvent` | Código | Auditoría de transición de estado | Vigente | No utilizado en `GenerateReturnsFileAsync`. |
| Lock de generación | `AchReturnGenerationLockService` | Código | Concurrencia por transacción | Vigente | Lock en memoria; no distribuido. |
| Configuración EF devolución | `AchReturnGeneratedConfiguration` | Código | Índices/restricciones | Vigente | Índice compuesto no único. |
| Patrón de transición/evento | `AchStateTransitionService` | Código | Referencia para eventos auditables | Vigente | Patrón útil para futuras remediaciones. |
| Ingesta incoming | `AchIncomingReturnIngestionService` | Código | Cruce outbound/incoming | Vigente | Incoming sí afecta estado (`ReturnedByEpr`). |
| Elegibilidad ROR | `AchReturnOfReturnEligibilityService` | Código | Dependencias con devoluciones previas | Vigente | Riesgo si outbound carece de evento/lifecycle. |
| Generación ROR | `AchReturnOfReturnFileGenerationService` | Código | Archivo y auditoría ROR | Vigente | Tiene auditoría específica en su flujo. |
| Caracterización técnica actual | `AchOutboundReturnStateAndIdempotencyCharacterizationTests` | Test | Congela comportamiento vigente | Vigente | Confirma brechas de estado/evento/idempotencia. |
| Auditoría técnica devoluciones | `docs/dev/devoluciones-ach-auditoria-cenit-ach-colombia.md` | Documento | Contexto funcional/normativo | Vigente | Base de diagnóstico y brechas históricas. |
| Matriz maestra S1 | `docs/audits/s1-matriz-maestra-trazable-funcional-normativa-2026-04-26.md` | Documento | Gobernanza de readiness | Vigente | Mantiene NO-GO productivo. |
| Scorecard GO/NO-GO | `docs/audits/go-nogo-scorecard-funcional-normativo-2026-04-26.md` | Documento | Decisión operativa | Vigente | Incluye brechas críticas. |
| Matriz de causales | `docs/audits/cause-code-normative-matrix-ach-cenit-sta-current.md` | Documento | Causales por cámara/flujo | Vigente | GO técnico/UAT controlado, NO-GO productivo. |
| Checklist UAT causales | `docs/uat/cause-code-acceptance-checklist.md` | Documento | Evidencia UAT causales | Vigente | Falta cierre firmable. |
| Matriz record-level NACHA | `docs/audits/nacha-record-level-normative-matrix-ach-cenit-current.md` | Documento | Reglas 1/5/6/7/8/9 | Vigente | Cierre productivo pendiente. |
| Checklist UAT NACHA | `docs/uat/nacha-records-acceptance-checklist.md` | Documento | Validación UAT registros | Vigente | CurrentLayout sigue provisional. |
| Matriz naming externo | `docs/audits/external-filename-normative-matrix-ach-cenit-sta-current.md` | Documento | Naming por flujo/cámara | Vigente | NO-GO productivo vigente. |
| Checklist UAT naming | `docs/uat/naming-returns-ror-acceptance-checklist.md` | Documento | Verificación naming | Vigente | Fallbacks provisionales no equivalen a GO productivo. |

## 4. Flujo actual de devolución saliente
1. recepción request;
2. validación request;
3. carga ciclo/cámara;
4. lock en memoria por transacciones;
5. selección transacciones;
6. validación misma cámara/ciclo;
7. chequeo `AchReturnGenerated` previo;
8. validación elegibilidad;
9. validación regulatoria;
10. validación causal rail-flow;
11. generación NACHA;
12. validación record-level;
13. naming externo;
14. persistencia `AchReturnGenerated`;
15. respuesta con archivo.

| Paso | Componente | Acción | Evidencia actual | Riesgo | Próxima acción |
|---|---|---|---|---|---|
| 1-2 | `AchReturnsService` | Valida request e items | Implementado | Bajo | Mantener caracterización. |
| 3 | `AchReturnsService` + `AchCycle` | Carga ciclo/cámara | Implementado | Bajo | Mantener validaciones cruzadas. |
| 4 | `AchReturnGenerationLockService` | Lock por transaction ids | Implementado | Alto multiinstancia | Evolucionar a control distribuido/DB-first. |
| 5-6 | `AchReturnsService` | Carga tx y valida consistencia de ciclo | Implementado | Medio | Aumentar evidencia operativa. |
| 7 | `AchReturnGenerated` | Detecta devolución ya generada | Implementado | Medio | Endurecer deduplicación DB. |
| 8 | `IAchReturnEligibilityService` | Evalúa elegibilidad saliente | Implementado | Medio | Seguir UAT de bordes regulatorios. |
| 9 | `IAchRegulatoryCatalogService` | Reglas de causal/política | Implementado | Medio | Mantener trazabilidad norma→test. |
| 10 | `IAchCauseCodePolicy` | Governing rail-flow de causal | Implementado | Medio | Persistir warnings relevantes. |
| 11 | `AchReturnsService` | Construcción contenido NACHA | Implementado | Medio | No regresión con golden tests. |
| 12 | `INachaRecordFieldValidator` | Validación record-level | Implementado | Medio | Mantener evidencia UAT por cámara. |
| 13 | `IExternalFileNamePolicy` | Genera/valida nombre externo | Implementado | Medio | Persistir correlación externa interna. |
| 14 | `AchReturnGenerated` | Persistencia evidencia técnica | Implementado | Alto (trazabilidad parcial) | Enriquecer payload auditable. |
| 15 | `AchReturnsController` | Retorna archivo al cliente | Implementado | Medio | Añadir consulta audit trail posterior. |

## 5. Matriz de estado de transacción original

| Momento | Estado esperado funcional | Estado actual técnico | Implementado | Evidencia | Brecha | Severidad |
|---|---|---|---|---|---|---|
| Antes de devolución | Pendiente/estado previo válido | `AchTransaction.State` vigente | Sí | Estado actual del registro | Ninguna | Baja |
| Seleccionada para devolución | Estado intermedio seleccionada | No existe estado dedicado | No | N/A | No granularidad de selección | Media |
| Elegible | Estado intermedio elegible | No existe estado dedicado | No | N/A | No granularidad de elegibilidad | Media |
| Archivo generado | Estado “devolución generada” | **No cambia `AchTransaction.State`** | No | `AchReturnGenerated` | Brecha crítica de state lifecycle | Alta |
| Archivo transmitido | Estado transmitida | No existe | No | N/A | No lifecycle de transmisión | Alta |
| Archivo aceptado por cámara | Estado aceptada | No existe | No | N/A | No lifecycle aceptación | Alta |
| Archivo rechazado por cámara | Estado rechazada | No existe | No | N/A | No lifecycle rechazo | Alta |
| Compensación/conciliación | Estado conciliado/contabilizado | Depende de estados actuales | Parcial | Reportes por estado | Devolución generada puede no reflejarse | Alta |
| Reversión/anulación si aplica | Estado reversado/anulado | No explícito para este flujo | No | N/A | Tratamiento no definido en outbound | Media |

## 6. Matriz de eventos de estado

| Evento funcional | AchTransactionStateEvent actual | Existe | Payload actual | Payload requerido | Severidad | Próxima acción |
|---|---|---|---|---|---|---|
| ReturnSelected | No generado | No | N/A | ids + causal + ciclo + requestedBy + timestamp | Media | Evaluar en fase de audit trail. |
| ReturnEligible | No generado | No | N/A | resultado elegibilidad + causal normalizada + warnings | Media | Evaluar en fase de audit trail. |
| ReturnFileGenerated | No generado | **No** | N/A | `OriginalTransactionId`, `ReturnReasonCode`, `ReturnCycleId`, `ClearingHouseId`, `FileName`, `ExternalFileName`, `OriginalTraceNumber`, `NewTraceNumber`, `Amount`, `RequestedBy`, `GeneratedAtUtc`, `ContentSha256`, `Source`, `Warnings`, `GenerationMode` | **Alta** | Prioridad inmediata de remediación. |
| ReturnFileTransmissionRequested | No generado | No | N/A | correlación transmisión + operador + timestamp | Alta | Definir fase transmisión. |
| ReturnFileTransmitted | No generado | No | N/A | evidencia envío + canal + acuse | Alta | Definir fase transmisión. |
| ReturnFileAccepted | No generado | No | N/A | acuse aceptación cámara | Alta | Integrar respuesta cámara. |
| ReturnFileRejected | No generado | No | N/A | causal rechazo archivo/registro | Alta | Integrar respuesta cámara. |
| ReturnGenerationDuplicatedRejected | No generado | No | N/A | request + motivo duplicado + tx id | Media | Registrar rechazo duplicado. |
| ReturnGenerationFailed | No generado | No | N/A | detalle técnico + functional code + timestamp | Media | Estandarizar evento de fallo. |

Patrón potencial de implementación: `AchStateTransitionService` para emitir evento con estructura de payload auditable.

## 7. Matriz AchReturnGenerated

| Campo | Existe hoy | Uso actual | Suficiente para auditoría | Suficiente para idempotencia | Brecha | Próxima acción |
|---|---|---|---|---|---|---|
| OriginalTransactionId | Sí | Correlación transacción origen | Parcial | Sí parcial | Falta contexto adicional | Mantener + enriquecer payload relacionado |
| ReturnCycleId | Sí | Correlación ciclo | Parcial | Sí parcial | Sin lifecycle | Mantener |
| ReturnReasonCode | Sí | Causal aplicada | Parcial | Sí parcial | Sin vínculo a warnings persistidos | Persistir warnings/evidencias |
| Amount | Sí | Monto devuelto | Parcial | No | Sin estado operativo | Mantener |
| NewSequenceNumber | Sí | Secuencia nueva | Parcial | No | No distingue transmisión | Mantener |
| OriginalSequenceNumber | Sí | Secuencia original | Parcial | No | Sin trazabilidad extendida | Mantener |
| ReceiverEntityCode | Sí | Código entidad receptora | Parcial | No | Sin contexto operativo | Mantener |
| OriginatorEntityCode | Sí | Código entidad originadora | Parcial | No | Sin contexto operativo | Mantener |
| FileName | Sí | Nombre persistido de archivo | Parcial | Parcial | No separa explícitamente externo/interno | Separar y correlacionar nombres |
| GeneratedAtUtc | Sí | Timestamp de generación | Parcial | Parcial | Sin `CreatedAtUtc` de auditoría completa | Completar metadata temporal |
| RequestedBy | No | N/A | No | No | Falta actor | Agregar en fase de trazabilidad |
| ContentSha256 | No | N/A | No | No | Falta integridad de archivo | Agregar hash persistido |
| ExternalFileName | No (se usa `FileName`) | N/A explícito | No | No | Correlación naming parcial | Separar campo externo/interno |
| TransmissionStatus | No | N/A | No | No | Sin lifecycle transmisión | Agregar en fase lifecycle |
| AcceptedAtUtc | No | N/A | No | No | Sin hito aceptación | Agregar fase transmisión |
| RejectedAtUtc | No | N/A | No | No | Sin hito rechazo | Agregar fase transmisión |
| Source | No | N/A | No | No | Sin origen de operación | Agregar origen |
| CreatedAtUtc | No | N/A | No | No | Falta temporalidad de persistencia | Agregar metadata |
| UpdatedAtUtc | No | N/A | No | No | Falta auditoría de cambios | Agregar metadata |

## 8. Matriz de idempotencia

| Control | Existe hoy | Alcance | Cubre multiinstancia | Riesgo | Evidencia | Próxima acción |
|---|---|---|---|---|---|---|
| Lock en memoria | Sí | Por transaction ids en instancia | No | Alto | `AchReturnGenerationLockService` | Migrar a lock distribuido/DB-first |
| Chequeo `AchReturnGenerated` | Sí | Detección lógica previa | Parcial | Medio | Consulta previa en servicio | Endurecer con constraint único o equivalente |
| Índice no único | Sí | Performance de búsqueda | No | Alto | `AchReturnGeneratedConfiguration` | Definir constraint operacional fuerte |
| Unique constraint DB | No | N/A | No | Alto | No existe | Evaluar estrategia de unicidad |
| Deduplicación por OriginalTransactionId | Parcial | Bloqueo lógico actual | Parcial | Medio | Validación por existencia | Formalizar regla explícita |
| Deduplicación por OriginalTransactionId + ReturnReasonCode | Parcial | Derivable por índice no único | No | Medio | Índice compuesto no único | Evaluar unicidad por regla de negocio |
| Deduplicación por OriginalTransactionId + ReturnReasonCode + ReturnCycleId | Parcial | Derivable por índice no único | No | Medio | Índice compuesto no único | Endurecer en DB |
| Deduplicación por archivo/hash | No | N/A | No | Alto | No persistencia hash | Persistir hash y regla de duplicado |
| Deduplicación por request id | No | N/A | No | Medio | No idempotency key | Diseñar idempotency key |
| Control de retry | Parcial | Reintento falla por existencia previa | Parcial | Medio | Lógica actual | Formalizar retries seguros |
| Control concurrencia multi nodo | No | N/A | No | Alto | Lock local únicamente | Implementar control distribuido |
| Recovery ante fallo parcial | Parcial | Depende del punto de fallo | No | Alto | Sin mecanismo explícito | Diseñar compensación/reintentos |

**Escenarios obligatorios (estado actual):**
- mismo request dos veces: segunda ejecución rechazada por existencia lógica en `AchReturnGenerated` (si primera persistió).
- dos usuarios mismo tiempo: control parcial en misma instancia; sin garantía multiinstancia.
- dos nodos mismo tiempo: riesgo de carrera por lock no distribuido.
- falla después de generar contenido antes de persistir: no queda evidencia en `AchReturnGenerated`; potencial reproceso.
- falla después de persistir antes de responder: cliente puede reintentar y recibir rechazo por “ya registrada”.
- mismo archivo con distinto filename: no hay deduplicación por hash actualmente.
- misma transacción con causal diferente: requiere regla explícita de negocio/DB para evitar ambigüedad.
- mismo request con ciclo diferente: hoy se valida consistencia de ciclo en request/tx seleccionadas; regla de reproceso cross-cycle debe formalizarse.

## 9. Matriz de trazabilidad

| Pregunta de auditoría | Respuesta actual | Fuente actual | Brecha | Evidencia requerida |
|---|---|---|---|---|
| ¿Quién generó? | No estructurado | N/A persistente | Alta | `RequestedBy` persistido/evento |
| ¿Cuándo? | `GeneratedAtUtc` | `AchReturnGenerated` | Parcial | timestamps completos (created/updated/event) |
| ¿Cámara? | Derivable por ciclo | `AchCycle` | Parcial | persistencia directa en evidencia |
| ¿Ciclo? | Sí | `ReturnCycleId` | Baja | Mantener |
| ¿Causal? | Sí | `ReturnReasonCode` | Baja | Mantener + warnings |
| ¿Transacción original? | Sí | `OriginalTransactionId` | Baja | Mantener |
| ¿Trace original? | Sí (secuencia original) | `OriginalSequenceNumber` | Parcial | traza completa homologada |
| ¿Trace nuevo? | Sí (secuencia nueva) | `NewSequenceNumber` | Parcial | traza completa homologada |
| ¿Archivo? | Sí | `FileName` | Parcial | separar interno/externo |
| ¿Hash? | No | N/A | Alta | `ContentSha256` |
| ¿Filename externo? | Parcial (mismo campo) | `FileName` | Media | `ExternalFileName` dedicado |
| ¿Validaciones ejecutadas? | Parcial (runtime/log) | Servicio/logs | Media | auditoría persistida por ejecución |
| ¿Warnings? | Logs | logger | Media | warnings persistidos por retorno |
| ¿Usuario? | No estructurado | N/A | Alta | actor en payload |
| ¿IP/origen? | No estructurado | N/A | Media | metadatos de origen |
| ¿Estado transmisión? | No | N/A | Alta | lifecycle transmisión |
| ¿Aceptación/rechazo cámara? | No | N/A | Alta | integración de acuses |
| ¿Conciliación? | Parcial por estado transacción | reportes estado | Alta | vista dedicada generated vs transmitted |
| ¿Reversión? | No explícito | N/A | Media | regla y evidencia de anulación |

## 10. Relación con ROR

| Punto | Estado actual | Riesgo | Próxima acción |
|---|---|---|---|
| Dependencia funcional ROR | ROR depende de transacciones tipo `Return` y policy propia | Medio | Mantener gobernanza independiente y trazable |
| Evento outbound normal | Outbound normal no crea evento de estado | Alto | Agregar `ReturnFileGenerated` auditable |
| Duplicidad | Riesgo de ROR sobre devolución duplicada si falla idempotencia multiinstancia | Alto | Endurecer idempotencia DB-first |
| Evidencia base | `AchReturnGenerated` aporta evidencia parcial | Medio | Enriquecer payload y correlación |
| Elegibilidad futura | Falta vínculo explícito de evento outbound con ROR | Medio | Vincular evento/payload para criterios ROR |

## 11. Relación con incoming

| Punto | Estado actual | Riesgo | Próxima acción |
|---|---|---|---|
| Estado incoming | Incoming actualiza a `ReturnedByEpr` | Bajo/Medio | Mantener cobertura de tests incoming |
| Estado outbound | Outbound normal no actualiza estado | Alto | Definir estrategia de lifecycle outbound |
| Ambigüedad operacional | Posible ambigüedad entre devolución enviada vs recibida | Alto | Diferenciar hitos outbound/incoming |
| Eventos cruzados | Eventos incoming/outbound no unificados | Medio | Definir taxonomía de eventos |
| Rechazo total/parcial | Incoming tiene decisiones de rechazo; outbound no lifecycle equivalente | Medio | Alinear trazabilidad entre flujos |

## 12. Relación con conciliación/contabilidad

| Punto | Estado actual | Brecha | Severidad | Próxima acción |
|---|---|---|---|---|
| Base de conciliación | Conciliación usa `AchTransaction.State` | Outbound generated no cambia estado | Alta | Incluir fuente generated/transmitted en conciliación |
| Devolución generada no transmitida | Puede no verse explícitamente | Falta lifecycle técnico-operativo | Alta | Exponer estado de archivo de devolución |
| Asiento contable | No explícito en este flujo | Tratamiento contable no cerrado | Alta | Definir integración contable |
| Ciclo transmisión/aceptación/rechazo | No existe completo | Falta evidencia de resultado cámara | Alta | Incorporar eventos y estados de transmisión |
| Reporte por causal/cámara/ciclo | Parcial | No cobertura completa de audit trail | Media | Expandir query/reportes auditables |
| Detección de duplicados | Parcial lógica | Sin constraint fuerte | Alta | Endurecer idempotencia |

## 13. Brechas clasificadas

### P0 — Bloqueo técnico/productivo
- Falta evento auditable `ReturnFileGenerated`.
- Idempotencia no distribuida/multiinstancia.
- Índice no único en lugar de restricción fuerte operacional.
- Ausencia de lifecycle transmisión/aceptación/rechazo.
- Conciliación puede no ver devolución generada al depender del estado transaccional.

### P1 — UAT/control
- Trazabilidad parcial por archivo/transacción.
- Warnings no persistidos de forma estructurada.
- `RequestedBy` no persistido.
- `ContentSha256` no persistido.
- `ExternalFileName` no separado explícitamente.
- No consulta UI/API dedicada para audit trail outbound return.

### P2 — Mejora
- Payload estándar de auditoría outbound.
- Normalización de logs y códigos de evento.
- Refactor de state machine completo (fase posterior).
- Integración contable posterior con evidencia trazable.

## 14. Decisión recomendada
Se recomienda **Opción D + B incremental**:
1. caracterizar comportamiento actual;
2. documentar matriz vigente;
3. agregar evento auditable `ReturnFileGenerated`;
4. endurecer idempotencia DB-first;
5. agregar payload trazable;
6. evaluar lifecycle completo en fase posterior.

No se recomienda introducir state machine completa de inmediato porque:
- riesgo alto de regresión transversal;
- requiere alineación adicional con conciliación/contabilidad/UAT;
- puede impactar flujos incoming/ROR/reportes si se acelera sin fase intermedia.

## 15. Plan de remediación por commits

| Commit | Objetivo | Archivos probables | Tests | Riesgo | Rollback |
|---|---|---|---|---|---|
| 1. `docs(returns): add outbound return state-traceability matrix` | Consolidar diagnóstico vigente | `docs/audits/outbound-return-state-traceability-matrix-current.md` + referencias cruzadas | N/A (docs) | Bajo | Revert commit documental |
| 2. `feat(returns): add outbound return state event audit` | Emitir `ReturnFileGenerated` por transacción | Servicio returns + modelo evento | Unit/integration de eventos | Medio | Feature flag + revert |
| 3. `feat(returns): persist outbound return traceability payload` | Persistir payload mínimo (actor/hash/naming) | Entidad/audit model + persistence | Tests de persistencia/auditoría | Medio | Migración reversible o rollback controlado |
| 4. `feat(returns): harden outbound return idempotency` | DB-first idempotency + control de carreras | Config EF/servicio/repo | Concurrencia/duplicados | Alto | Rollback a lógica previa + mitigación operativa |
| 5. `test(returns): add concurrency/idempotency tests` | Cubrir escenarios multiinstancia/retry/fallo parcial | tests returns | Concurrency tests | Medio | Revert tests |
| 6. `docs(uat): add outbound return state traceability checklist` | Alinear UAT con nuevas evidencias | docs UAT | N/A (docs) | Bajo | Revert docs |
| 7. `feat(returns): expose outbound return audit query endpoint` | Consulta API de trazabilidad outbound | API/app/persistence | API tests | Medio | Feature toggle endpoint |
| 8. `feat(ui): show outbound return audit trail in Angular` | Visibilidad operativa audit trail | UI Angular | UI tests | Medio | Toggle funcional en UI |

## 16. Criterios de salida de NO-GO productivo
1. `ReturnFileGenerated` crea evento por transacción.
2. Evento incluye payload mínimo.
3. `AchReturnGenerated` contiene o relaciona trazabilidad suficiente.
4. Idempotencia DB-first implementada.
5. Unique constraint o equivalente operacional validado.
6. Concurrencia multiinstancia validada.
7. Retry seguro validado.
8. Estado/lifecycle documentado.
9. ROR no se afecta.
10. Incoming no se afecta.
11. Conciliación identifica devolución generada.
12. Contabilidad define tratamiento.
13. UAT con evidencia por archivo/transacción.
14. Firma negocio.
15. Firma operaciones.
16. Firma compliance/riesgo.
17. Aprobación técnica.

## 17. Decisión vigente
- **GO técnico:** sí.
- **GO UAT controlado:** sí.
- **NO-GO productivo:** sí.
- Próximo hito técnico: evento auditable `ReturnFileGenerated`.
- Próximo hito de riesgo: idempotencia DB-first / multiinstancia.

## Referencia cruzada UAT
- Checklist UAT de estado/evento/trazabilidad outbound return:
  - `docs/uat/outbound-return-state-traceability-acceptance-checklist.md`

- Referencia complementaria ciclos/neteo/liquidez/evidencia CUD: `docs/audits/cenit-cycles-netting-liquidity-cud-matrix-current.md` (no cambia decisión NO-GO productivo).
