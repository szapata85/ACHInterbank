# Incoming NACHA Processing — Auditoría técnica y blueprint faseable (2026-04-23)

## 1) Objetivo y alcance

Este documento consolida una **auditoría estática** del estado actual de procesamiento de archivos NACHA entrantes (inbound) y propone un **modelo objetivo faseable** (Prompt 2..10), sin introducir cambios funcionales en backend ni frontend.

**Alcance auditado**:
- Backend .NET (API + servicios de aplicación/dominio + persistencia EF Core).
- Modelo de persistencia y trazabilidad operacional.
- Pruebas automatizadas existentes relacionadas con incoming, linking, dispatch, prenotificación y devoluciones.
- Superficie frontend Angular relacionada con carga NACHA y devoluciones ACH.
- Gaps y riesgos para operación enterprise (idempotencia, estados, gobernanza regulatoria, observabilidad, ejecución en ventana/ciclo).

---

## 2) Resumen ejecutivo (estado actual)

### Fortalezas ya implementadas

1. **Pipeline inbound end-to-end base existe**:
   - Ingesta con hash SHA256 + control duplicidad por huella/size.
   - Resolución de cámara/ciclo previa a parseo.
   - Parseo y persistencia de detalle NACHA.
   - Post-proceso con clasificación funcional, linking, efectos de negocio y encolamiento de dispatch.

2. **Persistencia operacional rica para trazabilidad**:
   - Entidades separadas para ingesta, resultados de procesamiento, clasificación, linking, eventos de procesamiento, cola de despacho e historial de ejecución de integración.

3. **Gobernanza regulatoria de devoluciones presente**:
   - `AchReturnsService` valida causal y política regulatoria antes de generar archivo.
   - Controles de ventana de ciclos y prevención de doble devolución.

4. **Capa de API utilizable para operación básica**:
   - Upload NACHA y consulta de registros cargados.
   - Consulta de transacciones elegibles por ciclo y generación de archivo de devoluciones.

5. **Cobertura de pruebas relevante**:
   - Suites dedicadas a ingesta, post-proceso, orquestación, linker, resolver de prenotificación, dispatch planner y validación relacional.

### Brechas principales (P0/P1)

1. **No hay “command center” operacional inbound completo en API/UI**:
   - Falta endpoint/UI para gestionar cola de dispatch (reintentos manuales, desbloqueo, override auditado, replay controlado).

2. **Máquina de estados funcional está distribuida y parcialmente implícita**:
   - Existen estados en entidades/enum, pero no un modelo explícito único documentado y versionado para todo el flujo inbound.

3. **Orquestación de dispatch con “waiting window” parcialmente materializada**:
   - El orquestador re-habilita `WaitingWindow`, pero no se evidencia loop de recalculo de elegibilidad de ventana por política de negocio en cada ciclo de vida.

4. **Frontend inbound con foco en upload/listado, no en operación industrial**:
   - Falta visibilidad en UI de clasificación, linking confidence, eventos, cola de integración, intentos y acciones de remediación.

5. **Observabilidad y SLAs**:
   - Existe huella de ejecución (`IncomingNachaIntegrationExecution`) pero faltan tableros/indicadores formales y contratos de SLO/SLA operativos.

---

## 3) Inventario de capacidades implementadas (matriz)

| Capability | Estado | Evidencia técnica | Comentario |
|---|---|---|---|
| Upload NACHA inbound con validaciones de archivo | Implementado | `NachaUploadController` | Valida tamaño, extensión y MIME antes de ingesta. |
| Idempotencia de archivo por hash+tamaño | Implementado | `IncomingNachaIngestionAppService` | Duplicado detectado y registrado como resultado de procesamiento. |
| Reproceso controlado (`ForceReprocess` + `ParentIngestionId`) | Implementado | `IncomingNachaIngestionAppService` | Requiere consistencia hash/tamaño y evita reproceso duplicado del mismo parent. |
| Resolución cámara/ciclo previa al parseo | Implementado | `IncomingNachaIngestionAppService` + resolver | Bloquea/pendiente cuando no resuelve. |
| Integración con política de nombre externo inbound | Implementado | `IncomingNachaIngestionAppService` + `IExternalFileNamePolicy` | Hard block si validación de política falla. |
| Parseo detallado NACHA + persistencia | Implementado | `INachaParserService.ParseAndSaveDetailedAsync` | Graba métricas de lotes/entradas/adendas y warnings/errors. |
| Clasificación funcional por entry/addenda | Implementado | `IncomingNachaFunctionalClassifier` | Reglas por transaction code + addenda 99 + reason. |
| Linking a `AchTransaction` con múltiples estrategias | Implementado | `IncomingNachaTransactionLinker` + post-proceso | Maneja exact/ambiguous/notfound. |
| Bloqueo automático ante linking no determinístico | Implementado | `IncomingNachaPostParseProcessor` | Marca `EligibilityStatus.Bloqueada` + evento. |
| Resolver de prenotificación entrante | Implementado | `IncomingNachaPrenotificationResolver` | Aplica resolución determinística o deriva a revisión manual. |
| Aplicación de transición de estado para devoluciones/rechazos | Implementado | `IncomingNachaPostParseProcessor` + `IAchStateTransitionService` | Enruta por causal/regulatory source. |
| Planeación de cola de dispatch con key idempotente | Implementado | `IncomingNachaDispatchPlanner` | Usa hash de claves de negocio para idempotencia en cola. |
| Orquestación de dispatch hacia `Proc_Transacciones` | Implementado | `IncomingNachaPostProcessingOrchestrator` | Gestiona confirmación/retry/final fail/blocked. |
| Reintentos con backoff acotado | Implementado | `IncomingNachaPostProcessingOrchestrator` | `MaxAttempts=5`, backoff incremental hasta 30 min. |
| Registro de ejecución de integración (payload/hash/códigos) | Implementado | `IncomingNachaIntegrationExecution` | Buen punto de partida para auditoría forense. |
| API devoluciones por ciclo y generación archivo | Implementado | `AchReturnsController` + `AchReturnsService` | Incluye validación regulatoria y reglas de ventana. |
| UI carga NACHA + listado básico | Implementado parcial | `nacha-upload` component/service | Sin cockpit detallado de post-proceso/dispatch. |
| UI gestión devoluciones ACH | Implementado parcial | `ach-returns-management` | Flujo útil, pero acotado a selección causal + descarga archivo. |
| Operación manual de cola inbound (retry/unblock/requeue) | Gap | No endpoint dedicado identificado | Recomendado como fase temprana P0/P1. |
| Dashboard operacional inbound (SLA, aging, bloqueo) | Gap | No módulo dedicado identificado | Necesario para operación enterprise. |

---

## 4) Clasificación de gaps: configuración vs código

### 4.1 Gaps resolubles principalmente por configuración/datos (Prompt 2)

1. **Catalogación/reglas regulatorias completas** (causales, políticas, vigencias, source mapping operador/EPR).
2. **Parámetros operativos de ventana y prioridad por cámara/ciclo** (si ya hay políticas consumibles por servicios existentes).
3. **Matriz de errores SOAP y semántica retryable/no-retryable** para mejor taxonomía operacional.
4. **Tablas de mapeo/normalización para evidencias y codificación de causa de bloqueo** (sin cambiar core de flujo).

### 4.2 Gaps que requieren código backend (Prompt 3+)

1. **API de command center inbound**:
   - listar cola por estado/aging,
   - retry manual seguro,
   - desbloqueo con justificación auditada,
   - replan/requeue controlado.

2. **Modelo unificado explícito de state machine inbound**:
   - catálogo de estados/eventos/transiciones válido por contexto,
   - guard clauses formales,
   - bitácora de transición homogénea.

3. **Política de recalculo de elegibilidad `WaitingWindow`** en loop operacional robusto.
4. **Sagas/compensaciones explícitas** para pasos críticos que hoy dependen de manejo local de excepciones.

### 4.3 Gaps frontend (Prompt 6+)

1. **Panel de operación inbound** (colas, métricas, filtros avanzados, acciones manuales auditables).
2. **Vista de detalle de ingesta** (clasificación/linking/eventos/ejecuciones SOAP).
3. **UX de remediación** con anti doble click, loading state granular y confirmaciones para acciones críticas.

---

## 5) Estado actual de la “máquina de estados” (observado)

### 5.1 Ingesta (archivo)

Estados observados en flujo:
- `Recibido`
- `EnValidacion`
- `PendienteResolucion` o `Bloqueado` (si cámara/ciclo no resuelto/ambiguo)
- `ListoParaParseo`
- `Completado` / `Fallido` / `Duplicado`

### 5.2 Parseo / resultado

Estados observados en parseo:
- `NoEjecutado`
- `EnProceso`
- `Exitoso` / `ExitosoConAdvertencias`
- `FallidoReprocesable`

### 5.3 Clasificación y linking

- Clasificación funcional se persiste por `EntryDetail` (+ addenda asociada).
- Linking produce tipos determinísticos y no determinísticos (`Ambiguous`, `NotFound`, etc.).
- Si linking no es determinístico, se bloquea elegibilidad automática y se exige intervención manual.

### 5.4 Dispatch queue

Estados observados:
- `Queued`
- `Dispatching`
- `WaitingWindow`
- `RetryPending`
- `Confirmed`
- `Blocked`
- `FailedFinal`

### 5.5 Integración SOAP

- Se registra ejecución por item de cola con request/response payload, hash, código/mensaje de respuesta e indicadores de éxito/reintento.

---

## 6) Modelo objetivo propuesto (state machine enterprise)

> Objetivo: unificar semántica, reducir ambigüedad operativa y habilitar automation + operación manual controlada.

### 6.1 Dominios de estado separados (recomendado)

1. **FileIngestionState** (nivel archivo).
2. **EntryResolutionState** (nivel entrada: clasificación+linking+prenote).
3. **DispatchState** (nivel queue item).
4. **IntegrationState** (nivel intento de integración externa).

Evitar mezclar semánticas de diferentes niveles en un solo enum, pero mantener trazabilidad de correlación entre los cuatro.

### 6.2 Eventos canónicos

- `FileReceived`, `CycleResolved`, `CycleAmbiguous`, `CycleUnresolved`
- `ParseStarted`, `ParseSucceeded`, `ParseFailed`
- `EntryClassified`, `LinkResolved`, `LinkAmbiguous`, `LinkNotFound`
- `PrenoteResolved`, `PrenoteManualReview`
- `DispatchPlanned`, `DispatchWindowOpen`, `DispatchSent`, `DispatchConfirmed`, `DispatchRetryScheduled`, `DispatchFailedFinal`, `DispatchManuallyUnblocked`

### 6.3 Guardas críticas

- Ningún `DispatchSent` si `EntryResolutionState != Eligible`.
- Ninguna transición manual sin `justification` + `actor` + `ticket/reference`.
- Ningún retry ilimitado: caps por tipo de error y por ventana temporal.

---

## 7) Blueprint faseable (Prompt 2..10)

## Prompt 2 — Config & normativa (rápido impacto)

**Meta**: cerrar huecos de catálogos/políticas sin tocar lógica core.
- Completar catálogo de causales/políticas (vigencias, fuentes regulatorias, flags operativos).
- Definir taxonomía de errores integración (retryable/non-retryable/manual).
- Normalizar parámetros de ventanas/ciclos por cámara.

**Entregables**:
- matriz regulatoria versionada,
- seeders/migraciones de catálogo,
- checklist de auditoría de configuración.

## Prompt 3 — Backend: command center inbound API

**Meta**: operación industrial del pipeline.
- Endpoints para listar cola + filtros de aging/severidad.
- Acciones manuales: retry, unblock, requeue, mark-final (según política).
- Todas las acciones con auditoría obligatoria.

**Entregables**:
- contratos API,
- servicios de aplicación,
- tests unit/integration de seguridad e idempotencia.

## Prompt 4 — Backend: state machine explícita

**Meta**: formalizar transiciones y reglas.
- Tabla/objeto de transición permitido por estado/evento.
- Validación central de guardas.
- Emisión homogénea de eventos de dominio para observabilidad.

**Entregables**:
- motor de transición,
- pruebas de matriz de transición,
- documentación de estados/eventos.

## Prompt 5 — Backend: robustez de integración y compensaciones

**Meta**: resiliencia operacional.
- Estrategias claras por tipo de error SOAP.
- Reintento exponencial parametrizable con caps.
- Compensaciones y marcas de reconciliación.

**Entregables**:
- policy engine de retry,
- pruebas de caos/controladas,
- métricas por tipo de falla.

## Prompt 6 — Frontend: cockpit inbound

**Meta**: visibilidad y control operativo.
- Pantalla de cola inbound (AG-GRID), filtros, estados, aging, acciones.
- Panel de KPIs (pendientes, bloqueados, retry, SLA breach).
- Acciones críticas con loading + disabled + anti doble click.

**Entregables**:
- módulo SPA inbound-operations,
- pruebas unitarias Angular,
- smoke de integración API.

## Prompt 7 — Frontend: detalle de ingesta y trazabilidad

**Meta**: diagnóstico rápido por caso.
- Vista drill-down de ingesta -> entries -> clasificación -> linking -> eventos -> ejecuciones.
- Visualización de evidencia JSON y timeline de eventos.

## Prompt 8 — QA/E2E y certificación operativa

**Meta**: confianza de release.
- E2E de escenarios críticos (ambigüedad, retry, unlock, final fail).
- Dataset de certificación (golden files) y validación reproducible.

## Prompt 9 — Observabilidad & SRE

**Meta**: operación por SLO.
- Dashboards (latencia E2E, ratio bloqueos, ratio retries, MTTR manual).
- Alertas por aging y acumulación en cola.
- Runbooks y playbooks por incidente.

## Prompt 10 — Hardening de gobierno y rollout

**Meta**: despliegue seguro multi-entorno.
- Feature flags por capacidad.
- Rollout progresivo y shadow/compare en procesos sensibles.
- Controles de cumplimiento y auditoría interna.

---

## 8) Priorización sugerida

### P0 (inmediato)
1. Prompt 2 (config/normativa).
2. Prompt 3 (API command center mínimo: listar + retry + unblock auditado).
3. Definir documento oficial de state machine (aunque implementación total sea Prompt 4).

### P1 (corto plazo)
1. Prompt 4 (motor explícito de transición).
2. Prompt 6 (cockpit inbound básico en SPA).

### P2 (mediano)
1. Prompt 5 (resiliencia avanzada/compensaciones).
2. Prompt 7/9 (trazabilidad profunda + observabilidad full).
3. Prompt 10 (rollout/gobierno avanzado).

---

## 9) Riesgos y mitigaciones

1. **Riesgo de sobrecarga operativa manual** por falta de tooling.
   - Mitigar con Prompt 3/6 temprano.

2. **Riesgo de divergencia semántica de estados** entre servicios.
   - Mitigar con Prompt 4 + contrato único de transición.

3. **Riesgo de deuda de observabilidad** (diagnóstico lento en incidentes).
   - Mitigar con Prompt 7/9 y KPIs obligatorios por release.

4. **Riesgo de cambios normativos** no absorbidos a tiempo.
   - Mitigar con Prompt 2 y gobernanza de catálogos versionados.

---

## 10) Checklist de salida de auditoría

- [x] Inventario de capacidades backend/frontend.
- [x] Clasificación de gaps por configuración vs código.
- [x] Estado actual de estados/transiciones observado.
- [x] Modelo objetivo de máquina de estados enterprise.
- [x] Plan faseable Prompt 2..10 con prioridades P0/P1/P2.
- [x] Riesgos y mitigaciones.

---

## 11) Nota final

El repositorio ya contiene una base inbound **funcional y avanzada**; el principal salto pendiente no es “crear desde cero”, sino **industrializar operación y gobierno**: state machine explícita, command center operativo, observabilidad SRE y UX de remediación.
