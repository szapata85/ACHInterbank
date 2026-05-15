# Auditoría técnica de devoluciones ACH — CENIT vs ACH Colombia

Fecha: 2026-05-12  
Alcance: diagnóstico documental (sin cambios funcionales)

## 1) Inventario técnico revisado

Se revisaron los artefactos solicitados en dominio, aplicación, persistencia, API y pruebas, incluyendo:
- `AchReturnsController`, `IAchReturnsService`, `AchReturnsService`.
- `AchTransaction`, `AchTransactionStateEvent`, `AchTransferStateEnum`, `AchStateEventSourceEnum`.
- `AchReturnGenerated`, `ReturnReason`, `AchReturnCode`, `AchReturnPolicy`, `AchReturnOfReturnPolicy`, `ReturnOfReturnFlow`.
- `IncomingNachaFileIngestion`, `IncomingNachaFileProcessingResult`, `IncomingNachaTransactionLink`, `IncomingNachaEntryClassification`, `IncomingNachaProcessingEvent`, `IncomingNachaDispatchQueue`.
- Servicios/parsers/jobs de incoming NACHA (`IncomingNachaIngestionAppService`, `NachaParserService`, post-procesamiento y linker).
- `ExternalFileNameRegistry`, `ClearingHouse`, `ClearingHouseCycleConfig`, `AchCycle`, `TransactionCodeCatalog`.
- Seeds regulatorios (`RegulatoryCatalogSeeder`, `ClearingHouseCycleConfigSeeder`) y pruebas relacionadas (`AchReturns*`, `IncomingNacha*`, `ReportServicesDataQualityTests`, `ReturnOfReturn*`).

---

## 2) Matriz obligatoria CENIT vs ACH Colombia (12 puntos)

| # | Punto | Estado actual en código | ¿Diferencia CENIT/ACH? | Entidades/servicios | Gap detectado | Riesgo operacional | Recomendación | Prioridad |
|---|---|---|---|---|---|---|---|---|
| 1 | Códigos de causal permitidos | Existe catálogo `AchReturnCode` + validación por `IAchRegulatoryCatalogService`. | Parcial: se usa `RegulatorySource` string, no `ClearingHouseId` obligatorio en regla. | `AchReturnCode`, `RegulatoryCatalogSeeder`, `AchReturnsService`. | Modelo no impone segregación robusta por cámara. | Causal válida en una cámara puede aplicarse en otra por error. | Normalizar reglas por `ClearingHouseId` + versión. | Crítica |
| 2 | Causales por tipo transacción | `AchReturnPolicy.TransactionType` + CSV de causales. | No explícita por cámara. | `AchReturnPolicy`, `ValidateReturnPolicyAsync`. | Política global, no por cámara/ciclo. | Incumplimiento regulatorio por rail. | Política por cámara+tipo+flujo. | Crítica |
| 3 | Plazos máximos de devolución | `MaxDaysAllowed` y `AchReturnPolicy.MaxDays`; además ventana `MaxCyclesForReturn=4` hardcoded en salida. | Parcial: días/ciclos no unificados por cámara. | `AchReturnCode`, `AchReturnPolicy`, `AchReturnsService`. | Mezcla de reglas en días y en ciclos globales. | Rechazos y devoluciones fuera de SLA regulatorio. | Unificar SLA por cámara y flujo (días + ciclos). | Crítica |
| 4 | Ciclos habilitados para devolver | Se valida antigüedad por orden de ciclo y constante 4. | No, constante global. | `AchReturnsService`, `AchCycle`. | No parametrización por cámara ni vigencia regulatoria. | Falsos elegibles/no elegibles. | Regla por `ClearingHouseCycleConfig`/catálogo regulatorio. | Alta |
| 5 | Manejo devolución de devolución | Hay modelo/política y orquestador `ReturnOfReturnOrchestrator`. | No explícita por cámara en política base. | `AchReturnOfReturnPolicy`, `ReturnOfReturnFlow`, orquestador. | Falta gobierno por cámara/ciclo/nombre archivo. | Retornos encadenados no conformes. | Incluir cámara en políticas y trazabilidad E2E. | Alta |
| 6 | Validaciones de archivo | Parser NACHA valida estructura y catálogos rechazo (`Dxx`) + deduplicación en ingesta. | Parcial vía resolución de cámara para incoming. | `NachaParserService`, `IncomingNachaIngestionAppService`, `AchFileRejectionCode`. | Falta separación exhaustiva de reglas retorno incoming por cámara. | Falsos positivos/negativos en rechazo de archivos. | Catálogo de validación por cámara y tipo archivo (entrada/salida/retorno). | Alta |
| 7 | Rechazo total vs devolución parcial | Existen conceptos de rechazo de archivo y devoluciones de transacción, pero no frontera funcional completamente explícita en un módulo único. | No totalmente. | Parser, catálogo rechazo, `AchReturnsService`. | Ambigüedad operacional y técnica entre rechazo y devolución. | Tratamiento incorrecto de eventos contables/operativos. | Separar políticas, estados y reporting para ambos flujos. | Crítica |
| 8 | Formato/nombre de archivos | Salida returns usa `RET_{cycleId}_{timestamp}.RET` hardcoded. Incoming sí usa `ExternalFileNamePolicy`. | No en salida; parcial en incoming. | `AchReturnsService`, `ExternalFileNameRegistry/Policy`. | Inconsistencia entre flujos y cámaras. | Incumplimiento naming externo por cámara. | Llevar salida returns a `ExternalFileNamePolicy` por cámara. | Alta |
| 9 | Códigos de entidad | En salida se usan DFI de transacción; destino inmediato tipo 1 está hardcoded ACH Colombia (`000101006`). | No (hardcoded). | `AchReturnsService`, `ClearingHouse`. | Valores fijos no gobernados por cámara/fecha. | Archivos inválidos ante CENIT u otros cambios. | Parametrizar por cámara y vigencia. | Crítica |
| 10 | Horarios recepción/compensación | Existen `ClearingHouseCycleConfig` y `AchCycle` con ventanas. | Sí en configuración de ciclos; no totalmente acoplado a reglas de devolución. | `ClearingHouseCycleConfig`, `AchCycle`, resolver de ciclo incoming. | Reglas de devolución no consumen plenamente esas ventanas por cámara. | Incumplimiento de cutoff de devoluciones. | SLA/cutoff de devolución por cámara + integración con elegibilidad. | Alta |
| 11 | Reglas de aplicación contable | No se observa módulo explícito separado de contabilidad para devoluciones en este flujo. | No. | Servicios ACH y estados transaccionales. | Mezcla potencial entre orquestación operativa y contable. | Descadres y trazabilidad insuficiente. | Diseñar módulo contable separado y auditable. | Alta |
| 12 | Mensajes de error | Mensajes en `InvalidOperationException` y catálogos de rechazo; no completamente normalizados por cámara/flujo. | Parcial. | `AchReturnsService`, parser, catálogo rechazo. | Falta taxonomía estándar de errores por cámara. | Soporte/operación difícil; UAT inconsistente. | Catálogo de errores parametrizado por cámara+flujo+severidad. | Media |

---

## 3) Análisis de devolución de salida

### Flujo actual
1. `GET /ach-returns/cycles/{cycleId}/transactions` lista transacciones y marca elegibilidad por:
   - duplicado en `AchReturnGenerated`;
   - ventana de 4 ciclos (`MaxCyclesForReturn`) por orden de `AchCycle` en la cámara.
2. `POST /ach-returns/generate-file` valida:
   - selección no vacía;
   - no duplicados en request;
   - existencia y pertenencia al ciclo;
   - no mezclar ciclos;
   - no devolver `TransactionTypeEnum.Return/Reversal`;
   - no duplicar devolución ya registrada;
   - regla regulatoria causal/política vía `IAchRegulatoryCatalogService`.
3. Genera registros NACHA 1/5/6/7/8/9 + addenda tipo 99.
4. Persiste `AchReturnGenerated`.
5. Retorna archivo plano.

### Hallazgos clave
- **Persistencia:** sí, en `AchReturnGenerated`.
- **Actualización de `AchTransaction`:** no se evidencia cambio de estado de la transacción original al generar archivo.
- **`AchTransactionStateEvent`:** no se crea en este flujo.
- **Idempotencia:** parcial (validación de duplicado por existencia previa), no hay llave idempotente explícita por request.
- **Concurrencia de secuencia:** `GenerateNewReturnSequenceAsync` usa lectura de máximo secuencial del día; bajo concurrencia alta puede requerir refuerzo transaccional/lock lógico.
- **Hardcoded:** `MaxCyclesForReturn=4`, `ImmediateDestinationAchColombia`, `ReturnOriginatorId`, `ReturnBatchNumber`, patrón filename.

### Respuestas explícitas
- ¿La transacción original queda en `ReturnedByOperator`? **No, no en este flujo actual.**
- ¿Se llena `ReturnReasonCode`? **Sí, en `AchReturnGenerated`; no en `AchTransaction` en este flujo.**
- ¿Se crea `AchTransactionStateEvent`? **No en la generación de salida actual.**
- ¿Se diferencia por `ClearingHouse`? **Parcial (ciclo/cámara para orden de ciclos), pero reglas y naming aún globales/hardcoded.**
- ¿Se parametrizan códigos de entidad? **No completamente; hay campos hardcoded y derivaciones directas DFI.**
- ¿La ventana de devolución es global o por cámara? **Global de facto (`MaxCyclesForReturn=4`) aunque cálculo usa ciclos de la cámara.**
- ¿El nombre de archivo es configurable? **No, está hardcoded en el servicio de salida.**
- ¿Se separa rechazo total de devolución parcial? **No de forma explícita y completa en una política unificada.**

---

## 4) Análisis de devolución de entrada

### Evaluación E2E
- **Recepción de archivo:** Sí (`IncomingNachaIngestionAppService`).
- **Parseo tipo 6/7/99:** Sí, `NachaParserService` parsea detalles y addendas; clasificación funcional contempla devoluciones.
- **Extracción causal / original trace:** Sí, modelado en `IncomingNachaEntryClassification` (`ReturnReasonCode`, `OriginalTraceRef`) y clasificación funcional.
- **Búsqueda transacción original:** Sí, existe linker (`IncomingNachaTransactionLinker`) y tabla de enlaces.
- **Actualizar `AchTransaction` a `ReturnedByEpr`:** existe mecanismo de transición en parser/post-procesamiento, pero requiere consolidar evidencia de aplicación específica en todos los caminos de devolución.
- **Llenar `ReturnReasonCode` / `OriginalTraceRef`:** el modelo lo soporta; aplicación depende del flujo post-parse.
- **`AchTransactionStateEvent` source EPR:** existe enum/infraestructura; verificar cobertura total en todas las ramas.
- **Crear `IncomingNachaTransactionLink` / `IncomingNachaProcessingEvent`:** sí.
- **Idempotencia / duplicados:** sí por hash+tamaño en ingesta y llaves de dispatch.
- **Rechazo total de archivo:** sí por parser y catálogos (`Dxx`), además bloqueo por política de nombre externo.

### Respuestas explícitas
- ¿Existe parser de devolución entrante? **Sí.**
- ¿Existe servicio que aplique devolución entrante? **Existe pipeline de ingesta+post-proceso; la aplicación de estado está parcialmente distribuida en parser/orquestación.**
- ¿Se actualiza estado de transacción? **Soportado por arquitectura; requiere endurecer garantía E2E específica para devoluciones.**
- ¿Se audita el payload? **Sí (hash, evidencias JSON, payload XML en ejecución dispatch, eventos).**
- ¿Hay mensajes de error normalizados? **Parcialmente (catálogos y mensajes), no totalmente homologado por cámara/flujo.**
- ¿Está separado CENIT vs ACH Colombia? **Parcialmente (resolución de cámara/ciclo), no completamente en reglas de devolución regulatorias.**

---

## 5) Análisis de devolución de devolución

### Estado actualizado ROR (cierre técnico del PR)

- **Cerrado técnicamente**: el flujo Return Of Return ya no está en estado pendiente técnico.
- **Endpoints implementados**:
  - `POST /ach-returns/return-of-return/evaluate` (evaluación de elegibilidad).
  - `POST /ach-returns/return-of-return/generate-audit-file` (evidencia interna pipe con líneas `ROR|...` y `FLOW|...`).
  - `POST /ach-returns/return-of-return/generate-nacha-file` (archivo NACHA-M productivo independiente con registros 1/5/6/7/8/9).
- **UI disponible**: SPA Angular operativa en `/transactions/returns-ror`.
- **Separación de modos**:
  - `audit-mode`: no requiere evaluación previa elegible.
  - `nacha productivo`: exige evaluación previa elegible (gating operativo).
  - manejo de errores funcionales con `409` visible en UI para failures de elegibilidad/regla.
- **Pruebas ejecutadas (estado reportado de cierre técnico)**:
  - `dotnet restore` ✅
  - `dotnet build -c Release` ✅
  - `dotnet test` filtrado ✅ (291 passed)
  - `npm run build` ✅
  - `npm test` ⚠️ pendiente por dependencia `libatk-1.0.so.0` en `ChromeHeadless`.

### Estado de readiness ROR

- **GO técnico**: Sí.
- **GO para UAT controlado**: Sí.
- **GO productivo**: **No** (permanece en **NO-GO productivo** hasta cierre normativo + UAT + firmas).

### Pendientes UAT / normativos por cámara

- **CENIT**:
  - validar que la devolución de devolución aplicada en UAT cumple causales y reglas documentadas en CENIT (Anexo A y reglas operativas vigentes).
  - confirmar aceptación de archivo ROR NACHA-M por circuito de cámara según proceso operativo homologado.
- **ACH Colombia**:
  - confirmar aplicabilidad del flujo ROR contra manual ACH Colombia v3.2 y reglas de operador/devolución que correspondan al caso de uso.
  - validar aceptación operativa del naming externo definitivo en el intercambio con contraparte.

### Riesgos residuales

- Riesgo de **desalineación normativa** si no se formaliza confirmación explícita de cámara para ROR.
- Riesgo de **rechazo operativo externo** si naming/formato no quedan cerrados en validación real por contraparte.
- Riesgo de **falsa percepción de Go-Live** si no se conserva la distinción entre GO técnico/UAT y NO-GO productivo.

---

## 6) Modelo regulatorio recomendado (objetivo, sin implementar)

### Principios
1. `ClearingHouseId` obligatorio en toda regla regulatoria de devoluciones.
2. Versionado temporal (`EffectiveFrom/To`) y auditoría de cambios.
3. Separación por flujo: **salida**, **entrada**, **devolución de devolución**, **rechazo de archivo**.

### Recomendación de diseño
- Extender `AchReturnCode` con `ClearingHouseId`, `FlowType`, `TransactionTypeScope`, `EffectiveFrom/To`.
- Extender `AchReturnPolicy` con `ClearingHouseId`, `Direction`, `MaxCycles`, `AllowedCyclesPolicy`, `ErrorCatalogNamespace`.
- Extender `AchReturnOfReturnPolicy` con `ClearingHouseId`, `Direction`, `RequiresOriginalFlowType`.
- Extender `ReturnReason` para trazabilidad normativa por cámara/fuente.
- Integrar salida a `ExternalFileNameRegistry/Policy` para naming/formato por cámara.
- Extender `ClearingHouseCycleConfig` o crear entidad `ReturnWindowPolicy` por cámara/ciclo/flujo.
- Crear catálogo de mensajes (`AchOperationalErrorCatalog`) con códigos normalizados.
- Crear módulo separado de reglas contables (`AchReturnAccountingPolicy`) desacoplado del parser/orquestador.

### Cobertura explícita requerida
- ReturnReasonCode por cámara.
- Causal por tipo de transacción.
- Causal por flujo (entrada/salida/return-of-return).
- Plazos máximos por cámara.
- Ciclos permitidos por cámara.
- Formato/nombre archivo por cámara.
- Códigos entidad por cámara.
- Mensajes de error parametrizados.
- Rechazo total vs devolución parcial.
- Reglas contables separadas.

---

## 7) Plan de remediación por commits

1. `docs(returns): auditar reglas de devolución por cámara` ✅ (este commit)  
2. `refactor(returns): parametrizar reglas de devolución por ClearingHouse`  
3. `data(returns): sembrar causales CENIT y ACH Colombia`  
4. `fix(returns): actualizar estado y trazabilidad al generar devolución de salida`  
5. `fix(returns): alinear elegibilidad con generación de devoluciones`  
6. `feat(returns): procesar devoluciones entrantes y aplicar a transacción original`  
7. `feat(returns): soportar devolución de devolución por cámara`  
8. `feat(returns): separar rechazo total de archivo y devolución parcial`  
9. `feat(returns): parametrizar nombres de archivo y códigos entidad por cámara`  
10. `docs(returns): plan UAT devoluciones salida/entrada`


## Avance Fase 2.1

- Las reglas de devolución (`AchReturnCode`, `AchReturnPolicy`, `AchReturnOfReturnPolicy`) ahora incorporan `ClearingHouseId` como dimensión obligatoria para segmentación por cámara.
- Se agregó vigencia regulatoria con `EffectiveFrom`/`EffectiveTo` en las tres entidades.
- Se agregó segmentación funcional con `Direction`/`FlowType` (según corresponda por entidad) para preparar separación por flujo.
- La migración `AddClearingHouseToReturnRules` se validó de forma local para inspección técnica, pero **no se versiona** en esta tarea por política.
- Queda pendiente la Fase 2.2: aplicar validación regulatoria efectiva por cámara en servicios funcionales.

## Avance Fase 2.3A

- `RegulatoryCatalogSeeder` ahora resuelve explícitamente las cámaras CENIT y ACH Colombia.
- Se eliminó la resolución global silenciosa de una sola cámara para sembrado regulatorio de devoluciones.
- La separación real de códigos y políticas por cámara queda pendiente para la Fase 2.3B.

## Avance Fase 2.3B

- Los códigos de devolución (`AchReturnCode`) ya se separan por cámara (`ClearingHouseId`).
- `RegulatorySource = CENIT` queda asociado a CENIT.
- `RegulatorySource = ACH` y `RegulatorySource = OPERADOR` quedan asociados a ACH Colombia.
- `UpsertReturnCodesAsync` ya usa clave funcional `ClearingHouseId + Code + FlowType`.
- Las políticas de devolución (`AchReturnPolicy`) ya se separan por cámara.
- `AllowedReturnCodesCsv` ya no mezcla códigos CENIT con ACH/OPERADOR.
- `UpsertReturnPoliciesAsync` ya usa clave funcional `ClearingHouseId + TransactionType + Direction + FlowType`.
- Las políticas de devolución de devolución (`AchReturnOfReturnPolicy`) ya se separan por cámara.
- `AllowedNewReturnCodesCsv` ya no mezcla códigos entre cámaras.
- `UpsertReturnOfReturnPoliciesAsync` ya usa clave funcional `ClearingHouseId + OriginalReturnCode + Direction + FlowType`.
- Fase 2.3B queda cerrada.

## Cierre técnico Fase 2 - Modelo regulatorio por cámara

### Estado general

La **Fase 2** queda cerrada a nivel técnico (modelo, persistencia, catálogo regulatorio y contratos de validación), con los siguientes resultados:

- reglas de devolución diferenciadas por `ClearingHouseId`;
- validaciones regulatorias por cámara en servicios de catálogo;
- resolución explícita de cámaras CENIT y ACH Colombia en seeder;
- códigos de devolución separados por cámara;
- políticas de devolución separadas por cámara;
- políticas de devolución de devolución separadas por cámara;
- sin migraciones versionadas para este bloque de cambios (según política vigente del proyecto);
- suite completa en verde reportada en los commits de cierre de fase.

### Commits de Fase 2

Secuencia lógica de cierre (hash + título):

- `1121bb3` — `refactor(returns): agregar cámara a reglas de devolución`
- `c5c778b` — `test(returns): reforzar metadata de defaults en reglas de devolución` *(microfix relacionado a defaults en memoria)*
- `9d03783` — `fix(returns): alinear pruebas con migraciones no versionadas` *(microfix y hardening de cierre técnico)*
- `8217c29` — `refactor(returns): validar reglas regulatorias por ClearingHouse`
- `00585f1` — `refactor(returns): resolver cámaras en seeder regulatorio`
- `31ab476` — `data(returns): separar códigos de devolución por cámara`
- `53e57b4` — `data(returns): separar políticas de devolución por cámara`
- `d56b538` — `data(returns): separar políticas de devolución de devolución por cámara`

> Referencia de extracción: `git log --oneline --grep="returns" -n 30`.

### Matriz de cierre Fase 2

| Componente | Estado | Evidencia técnica | Riesgo residual |
|---|---|---|---|
| AchReturnCode | Cerrado | Separación por `ClearingHouseId`, vigencia y flujo; seed por cámara sin mezcla. | Ajuste de catálogo real UAT vs seed técnico. |
| AchReturnPolicy | Cerrado | Políticas por cámara con `AllowedReturnCodesCsv` filtrado por cámara. | Parametría regulatoria fina por banco participante. |
| AchReturnOfReturnPolicy | Cerrado | Políticas por cámara con `OriginalReturnCode` y `AllowedNewReturnCodesCsv` validados por cámara. | Casuística operativa avanzada en producción. |
| IAchRegulatoryCatalogService | Cerrado | Contratos con `clearingHouseId` obligatorio para validaciones regulatorias. | Dependencia de correcta propagación de contexto en futuros módulos. |
| AchRegulatoryCatalogService | Cerrado | Filtros por cámara, vigencia, dirección y flujo en validaciones. | Requiere monitoreo de performance en catálogos voluminosos. |
| AchReturnsService / NachaParserService / ReturnOfReturnOrchestrator | Cerrado en alcance Fase 2 | Propagación de cámara y carga/resolución de ciclo para validar reglas. | Endurecimiento funcional adicional en Fase 3/4/5. |
| RegulatoryCatalogSeeder | Cerrado | Resolución explícita CENIT/ACH Colombia y upserts con clave funcional por cámara. | Gobierno de cambios de seed frente a normativa real. |
| Tests de metadata | Cerrado | Cobertura de restricciones/modelo y defaults esperados. | Evolución de modelo exige mantener tests de regresión. |
| Tests de no mezcla por cámara | Cerrado | Pruebas dedicadas para códigos, políticas y devolución de devolución. | Escenarios edge de catálogos productivos no modelados aún. |
| Migraciones/snapshot | Cerrado por política | No versionados en estos commits por lineamiento vigente. | Requiere estrategia controlada de despliegue DB fuera de este bloque. |

## Preparación Fase 3 - Endurecer devoluciones de salida

Foco recomendado de implementación:

1. Elegibilidad de devolución de salida.
2. Validación de causal contra cámara.
3. Validación de tipo de transacción.
4. Validación de plazo máximo.
5. Validación de estado transaccional.
6. Validación de addenda requerida.
7. Idempotencia para no devolver dos veces la misma transacción.
8. Concurrencia al generar archivo.
9. Rechazo parcial vs rechazo total en generación.
10. Auditoría de payload y decisión.
11. Mensajes de error orientados a operación.
12. Pruebas golden master de archivos.

Commits sugeridos para Fase 3:

- `refactor(returns): centralizar elegibilidad de devolución de salida`
- `feat(returns): validar causal y plazo en devolución de salida`
- `feat(returns): reforzar idempotencia de devolución de salida`
- `test(returns): cubrir generación de archivo por cámara`
- `docs(returns): plan UAT devoluciones de salida`

## Pendientes posteriores

### Fase 4 - Endurecer devoluciones de entrada

- parseo y clasificación de archivos entrantes;
- resolución de cámara desde archivo/ciclo;
- linking contra transacción original;
- validación de causal entrante;
- validación de duplicados;
- control de rechazo total/parcial;
- auditoría de payload;
- actualización de estado transaccional.

### Fase 5 - Devolución de devolución funcional

- orquestación funcional;
- unicidad por transacción;
- validación de devolución previa;
- validación por cámara;
- reglas de estado;
- idempotencia;
- pruebas de flujo completo.

### Fase 6 - Validaciones de archivo, rechazo total/parcial y mensajes

- validaciones estructurales NACHA;
- errores técnicos vs regulatorios;
- rechazo total vs parcial;
- catálogo de mensajes;
- trazabilidad operativa.

### Fase 7 - Contabilidad, conciliación y plan UAT

- aplicación contable;
- conciliación por cámara/ciclo/archivo;
- evidencias UAT;
- consultas SQL de verificación;
- matriz de casos de prueba;
- rollback operativo.

## Riesgos y controles para UAT

| Riesgo | Impacto | Control recomendado | Fase donde se atiende |
|---|---|---|---|
| Catálogos reales CENIT/ACH no coinciden con seed técnico | Rechazos indebidos o reglas incompletas | Mesa regulatoria + homologación de catálogo antes de go-live | Fase 3/4 |
| Causal válida para una cámara usada en otra | Incumplimiento regulatorio | Validación estricta por `ClearingHouseId` + pruebas cruzadas | Fase 3 |
| Plazos máximos distintos por cámara | Devoluciones fuera de ventana | Reglas de plazo por cámara con pruebas de frontera | Fase 3 |
| Ciclos habilitados para devolver no modelados funcionalmente | Elegibilidad inconsistente | Parametrizar y validar ventanas por ciclo/cámara | Fase 3 |
| Doble devolución por concurrencia | Duplicidad operativa/contable | Idempotencia + controles transaccionales de concurrencia | Fase 3 |
| Devolución de devolución sin original válido | Flujo inconsistente y riesgo de rechazo | Validación estricta de trazabilidad de original | Fase 5 |
| Rechazo total/parcial no parametrizado | Decisiones operativas ambiguas | Reglas explícitas y pruebas por escenario | Fase 6 |
| Mensajes operativos insuficientes | Mayor tiempo de soporte y errores humanos | Catálogo de mensajes operativos accionables | Fase 6 |
| Diferencias de naming/formato de archivo por cámara | Rechazo por cámara compensadora | Golden master + validadores por cámara | Fase 3/6 |
| Impacto contable no conciliado | Descadres financieros y auditoría negativa | Conciliación diaria por cámara/ciclo/archivo | Fase 7 |

## Avance Fase 3.1 - Elegibilidad centralizada de devolución de salida

- Se creó un servicio centralizado `IAchReturnEligibilityService` / `AchReturnEligibilityService` para evaluar elegibilidad de devolución saliente de forma estructurada.
- La resolución de cámara usa `ClearingHouseId` obtenido desde `AchCycle` de la transacción.
- La validación regulatoria se delega en `IAchRegulatoryCatalogService` para causal y política.
- Este cambio no modifica generación de archivo, naming de archivo ni estado final de transacciones.
- Pendientes para siguiente fase: validación fuerte de causal/plazo, idempotencia de solicitudes, control de concurrencia y verificación golden master.

- `IAchReturnEligibilityService` quedó registrado en DI y `AchReturnsService` ya no instancia manualmente `AchReturnEligibilityService`; se mantiene sin cambios generación/naming/estado.

## Avance Fase 3.2 - Validación de causal y plazo en devolución de salida

- La causal de devolución ahora se valida como obligatoria y se normaliza (trim + uppercase).
- Los códigos alfanuméricos como `DEV14` se preservan sin truncamiento.
- La validación de causal se ejecuta por `ClearingHouseId` resuelto desde `AchCycle`.
- El plazo máximo se valida vía `IAchRegulatoryCatalogService.ValidateReturnPolicyAsync`.
- Estado transaccional y addenda requerida se validan vía política regulatoria del catálogo.
- No hubo cambios en generación, naming ni estados finales de archivo/transacción.
- Pendientes: idempotencia, concurrencia, rechazo parcial/total y golden master por cámara.

## Avance Fase 3.3 - Idempotencia de devolución de salida

- Se rechazan transacciones repetidas dentro de la misma solicitud de devolución.
- Se rechazan transacciones ya devueltas/procesadas usando estado y modelo existente (`AchReturnGenerated`).
- `AchReturnsService` no genera archivo parcial cuando detecta duplicados en la solicitud.
- No hubo cambios en formato NACHA, naming ni estados finales.
- Riesgo residual: concurrencia avanzada multi-nodo / lock fuerte en DB queda para fase posterior.
- Pendientes: concurrencia avanzada, rechazo parcial/total y golden master por cámara.

## Avance Fase 3.4 - Concurrencia básica en devolución de salida

- Se agregó control de concurrencia in-process por `TransactionId` para generación de devoluciones.
- Los locks se adquieren en orden estable (ascendente) para evitar deadlocks.
- Se mantiene idempotencia por estado y por `AchReturnGenerated` después de adquirir lock.
- No hubo cambios en formato NACHA, naming ni estados finales.
- Límite actual: no cubre múltiples instancias/nodos.
- Pendiente UAT/hardening: índice único y/o lock DB/distribuido para concurrencia multi-nodo.

## Avance Fase 3.5 - Cobertura de generación por cámara

- Se agregaron pruebas de generación de devolución para CENIT y ACH Colombia.
- El flujo depende de elegibilidad por cámara antes de generar archivo.
- Se valida que un rechazo por causal/cámara no genera `AchReturnGenerated`.
- Se mantiene sin cambios formato NACHA, naming, golden master y estados.
- Pendiente: golden master específico por cámara para UAT con archivos reales CENIT/ACH.

## Plan UAT - Devoluciones de salida

### Objetivo UAT

Validar operativamente que el proceso de devolución de salida:
- respeta cámara CENIT / ACH Colombia;
- valida causal por cámara;
- valida plazo máximo por política regulatoria;
- valida estado/addenda;
- evita duplicados;
- controla concurrencia básica in-process;
- genera archivo sin cambios de formato/naming;
- registra evidencia en `AchReturnGenerated`;
- mantiene golden master existente sin alteraciones.

### Matriz de casos UAT por cámara

| ID caso | Cámara | Escenario | Datos mínimos | Resultado esperado | Evidencia técnica | Estado esperado |
|---|---|---|---|---|---|---|
| UAT-SAL-001 | CENIT | Devolución válida | Tx CENIT elegible + causal válida | Se genera archivo y devolución | `AchReturnGenerated` con `ReturnCycleId` CENIT y `ReturnReasonCode` normalizado | Aprobado |
| UAT-SAL-002 | ACH Colombia | Devolución válida | Tx ACH elegible + causal válida | Se genera archivo y devolución | `AchReturnGenerated` con ciclo ACH correcto | Aprobado |
| UAT-SAL-003 | CENIT | Causal cruzada no CENIT | Tx CENIT + causal de otra cámara | Rechazo antes de generar | No existe `AchReturnGenerated` para la tx | Rechazado controlado |
| UAT-SAL-004 | ACH Colombia | Causal cruzada no ACH | Tx ACH + causal de otra cámara | Rechazo antes de generar | No existe `AchReturnGenerated` para la tx | Rechazado controlado |
| UAT-SAL-005 | CENIT | Plazo excedido | Tx CENIT fuera de ventana | `RETURN_POLICY_REJECTED` | Mensaje de política regulatoria | Rechazado controlado |
| UAT-SAL-006 | ACH Colombia | Plazo excedido | Tx ACH fuera de ventana | `RETURN_POLICY_REJECTED` | Mensaje de política regulatoria | Rechazado controlado |
| UAT-SAL-007 | Ambas | Tx repetida en request | Mismo `TransactionId` duplicado | Rechazo por duplicado antes de generar | Sin archivo parcial ni filas nuevas | Rechazado controlado |
| UAT-SAL-008 | Ambas | Tx ya devuelta por estado | `ReturnedByOperator` / `ReturnedByEpr` | `RETURN_ALREADY_PROCESSED` | Failure estructurado | Rechazado controlado |
| UAT-SAL-009 | Ambas | Tx ya incluida | Existe `AchReturnGenerated` previo | `RETURN_ALREADY_INCLUDED_IN_FILE` | Failure estructurado + sin nueva fila | Rechazado controlado |
| UAT-SAL-010 | Ambas | 2 solicitudes simultáneas misma tx | Concurrencia misma `TransactionId` | Serialización por lock in-process | Trazas/orden de ejecución | Aprobado |
| UAT-SAL-011 | Ambas | Paralelo tx distintas | Dos `TransactionId` distintos | Sin bloqueo innecesario | Tiempos/orden de tareas | Aprobado |
| UAT-SAL-012 | Ambas | Golden master/formato | Suite de generación actual | Sin cambios de formato/naming | `AchPreproductionCertificationTests` / `AchTransactionNachaTests` en verde | Aprobado |

### Validación de causal y plazo

- Causal obligatoria.
- Normalización `trim + uppercase`.
- Preservación de códigos alfanuméricos como `DEV14`.
- Validación por `ClearingHouseId`.
- Plazo máximo delegado a política regulatoria (`ValidateReturnPolicyAsync`).
- Estado y addenda delegados a política regulatoria.

### Validación de idempotencia

- Duplicado dentro del request.
- Estado `ReturnedByOperator` / `ReturnedByEpr`.
- Existencia previa en `AchReturnGenerated`.
- Comportamiento esperado: no generar archivo parcial.

### Validación de concurrencia

- Lock in-process por `TransactionId`.
- Adquisición en orden ascendente estable.
- No cubre multi-nodo.
- Recomendación UAT: ejecutar pruebas simultáneas en una sola instancia y registrar trazas.

### Evidencias esperadas en AchReturnGenerated

Campos a revisar:
- `OriginalTransactionId`
- `ReturnCycleId`
- `ReturnReasonCode`
- `Amount`
- `OriginalSequenceNumber`
- `NewSequenceNumber`
- `ReceiverEntityCode`
- `OriginatorEntityCode`
- `FileName`
- `GeneratedAtUtc`

SQL orientativo:

```sql
SELECT *
FROM AchReturnGenerated
WHERE OriginalTransactionId = <id>;

SELECT OriginalTransactionId, COUNT(*)
FROM AchReturnGenerated
GROUP BY OriginalTransactionId
HAVING COUNT(*) > 1;
```

> Nota: nombres reales de tabla/columna pueden variar según provider/EF.

### Validación de golden master

- No modificar archivos expected.
- Ejecutar suite completa.
- Revisar tests existentes (`AchPreproductionCertificationTests`, `AchTransactionNachaTests`).
- Confirmar que formato/naming no cambió.

### Riesgos UAT pendientes

| Riesgo | Impacto | Control actual | Control futuro recomendado | Fase sugerida |
|---|---|---|---|---|
| Lock actual solo in-process | Carrera entre instancias | Lock por `TransactionId` en proceso único | Índice único y/o lock DB/distribuido | Fase 4.x |
| Multi-nodo puede generar carrera | Doble devolución | Idempotencia por estado + `AchReturnGenerated` | Restricción fuerte por `OriginalTransactionId` + aislamiento transaccional | Fase 4.x |
| Catálogo técnico difiere de regla real de cámara | Rechazos funcionales en UAT | Validación por catálogo actual por cámara | Certificación regulatoria final por cámara | Fase 5 |
| Diferencias formato/naming por cámara | Rechazo por cámara | Golden master actual | Golden master específico CENIT/ACH | Fase 5 |
| Rechazo parcial vs total no totalmente parametrizado | Ambigüedad operativa | Rechazo controlado por políticas vigentes | Parametrización explícita parcial/total | Fase 6 |
| Contabilidad/conciliación pendiente | Riesgo de descuadre | Evidencia técnica de generación | Cierre contable/conciliación end-to-end | Fase 7 |

### Checklist operativo UAT

- [ ] Confirmar catálogo CENIT cargado.
- [ ] Confirmar catálogo ACH Colombia cargado.
- [ ] Ejecutar devolución válida CENIT.
- [ ] Ejecutar devolución válida ACH.
- [ ] Probar causal cruzada.
- [ ] Probar plazo vencido.
- [ ] Probar transacción repetida en request.
- [ ] Probar transacción ya devuelta.
- [ ] Probar `AchReturnGenerated` previo.
- [ ] Probar concurrencia misma transacción.
- [ ] Probar concurrencia transacciones distintas.
- [ ] Ejecutar suite automatizada.
- [ ] Revisar golden master.
- [ ] Registrar evidencias SQL.
- [ ] Registrar riesgos no bloqueantes.

## Avance Fase 4.1 - Ingesta centralizada de devoluciones de entrada

- Se creó un servicio central de ingesta de devoluciones de entrada (`IAchIncomingReturnIngestionService`).
- Se separan responsabilidades de parseo, clasificación de retornos y linking contra transacción original.
- `ClearingHouseId` se resuelve desde la transacción/ciclo original cuando existe vínculo.
- Se reportan fallas estructuradas (`FILE_EMPTY`, `ORIGINAL_TRACE_MISSING`, `ORIGINAL_TRANSACTION_NOT_FOUND`, `CLEARING_HOUSE_MISSING`, `RETURN_REASON_MISSING`).
- En esta fase no se cambian estados finales de transacciones.
- En esta fase no se define rechazo total/parcial.
- Pendientes: validación regulatoria de entrada, duplicados, rechazo total/parcial, auditoría de payload y actualización de estado.

## Validación de cierre Fase 4.1 - Ingesta centralizada de devoluciones de entrada

- Se valida el cierre del commit publicado `be5c8afd3571599297ea9ed2b3c76a2c9f902ad4`.
- La Fase 4.1 queda aceptada como ingesta centralizada de devoluciones de entrada.
- Componentes confirmados:
  - `IAchIncomingReturnIngestionService`.
  - modelos `AchIncomingReturnIngestionRequest`, `AchIncomingReturnIngestionResult`, `AchIncomingReturnItem`, `AchIncomingReturnIngestionFailure`.
  - `AchIncomingReturnIngestionService`.
  - registro DI `Scoped`.
  - tests de ingesta.
- Validaciones confirmadas:
  - archivo vacío produce `FILE_EMPTY`.
  - devolución entrante tipo `7/99` se clasifica.
  - se extrae causal de devolución.
  - se extrae traza original.
  - se vincula contra transacción original por `TraceNumber` / `OriginalTraceRef`.
  - `ClearingHouseId` se resuelve desde `AchCycle`.
  - se reportan fallas estructuradas.
- Límites confirmados:
  - no cambia estado final de transacciones.
  - no genera `AchReturnGenerated`.
  - no genera archivo de salida.
  - no define rechazo total/parcial.
  - no ejecuta contabilidad.
- Evidencia de validación:
  - build Release en verde.
  - test filtrado en verde.
  - suite completa en verde.
- Pendientes Fase 4.2:
  - validación regulatoria de devoluciones entrantes por cámara.
  - duplicados de archivo entrante.
  - auditoría de payload.
  - rechazo total/parcial.
  - actualización controlada de estado.

## Avance Fase 4.2 - Validación regulatoria de devoluciones entrantes por cámara

- La ingesta valida causal entrante contra catálogo regulatorio por `ClearingHouseId`.
- La cámara se resuelve desde la transacción/ciclo original (`AchCycle`).
- La política regulatoria valida plazo, estado y addenda para devoluciones entrantes.
- Se preservan códigos alfanuméricos como `DEV14` en la validación.
- Se reportan fallas `INCOMING_RETURN_CODE_REJECTED` e `INCOMING_RETURN_POLICY_REJECTED`.
- No se cambian estados finales en esta fase.
- No se genera archivo de salida en esta fase.
- No se crea `AchReturnGenerated` desde ingesta.
- Pendientes: duplicados de archivo entrante, auditoría payload, rechazo total/parcial y actualización controlada de estado.

## Avance Fase 4.3 - Detección de duplicados en devoluciones entrantes

- Se detectan duplicados dentro del mismo archivo usando una clave funcional por cámara/transacción/causal.
- La causal se normaliza antes de comparar duplicados.
- Códigos alfanuméricos como `DEV14` se preservan en la detección.
- Se reporta la falla `INCOMING_RETURN_DUPLICATE_IN_FILE`.
- Si no existe modelo persistente específico de auditoría de ingesta entrante para esta fase, el control contra histórico queda como riesgo residual documentado.
- No se cambian estados finales de transacciones.
- No se genera archivo de salida.
- No se crea `AchReturnGenerated`.
- No se decide rechazo total/parcial en esta fase.
- Pendientes: auditoría persistente de payload, rechazo total/parcial y actualización controlada de estado.

## Avance Fase 4.4 - Auditoría interna del payload de devoluciones entrantes

- La ingesta retorna un resumen audit-friendly del archivo procesado.
- Se calcula hash SHA-256 del contenido de entrada.
- Se calcula hash SHA-256 por registro procesado.
- Se exponen previews limitados de registros y no el payload completo.
- Se auditan conteos, registros procesados y fallas detectadas.
- No se persiste auditoría todavía porque no existe modelo aprobado para esta fase.
- No se cambian estados finales.
- No se genera archivo de salida.
- No se crea `AchReturnGenerated`.
- No se decide rechazo total/parcial.
- Pendientes: auditoría persistente de payload, rechazo total/parcial y actualización controlada de estado.

## Avance Fase 4.5 - Clasificación interna de rechazo total/parcial

- La ingesta clasifica el resultado como `Accepted`, `RejectedTotal` o `RejectedPartial`.
- La clasificación es interna y audit-friendly; no ejecuta rechazo operativo todavía.
- `RejectedTotal` aplica a archivo vacío o cuando ningún retorno puede procesarse de forma confiable.
- `RejectedPartial` aplica cuando existe al menos un retorno válido junto con al menos una falla.
- La decisión se refleja en el resultado y en la auditoría interna.
- No se cambian estados finales.
- No se genera archivo de respuesta ni archivo de salida.
- No se crea `AchReturnGenerated`.
- No se ejecuta contabilidad.
- Pendientes: respuesta operativa de rechazo, persistencia de auditoría y actualización controlada de estado.

## Avance Fase 4.6 - Actualización controlada de estado en devoluciones entrantes

- La ingesta aplica actualización de estado solo cuando la decisión es `Accepted` o `RejectedPartial`.
- Si la decisión es `RejectedTotal`, no se actualiza ninguna transacción.
- Solo se actualizan transacciones vinculadas y sin fallas asociadas del item.
- Se usa estado existente `ReturnedByEpr` para devoluciones entrantes desde cámara/EPR.
- La actualización es operativa mínima y no implica contabilidad.
- No se genera archivo de respuesta ni de salida.
- No se crea `AchReturnGenerated` desde ingesta.
- La decisión y conteo de actualizaciones quedan en el resultado/auditoría interna.
- Pendientes: persistencia formal de auditoría y política operativa final de rechazo total/parcial.

## Cierre técnico Fase 4 - Devoluciones de entrada

### Estado general

La Fase 4 queda cerrada funcionalmente a nivel técnico para devoluciones de entrada con los siguientes alcances implementados:

- ingesta centralizada;
- parseo y clasificación de registros entrantes tipo devolución;
- linking contra transacción original;
- resolución de `ClearingHouseId` desde `AchCycle`;
- validación regulatoria entrante por cámara;
- detección de duplicados dentro del archivo;
- auditoría interna del payload;
- clasificación interna `Accepted`, `RejectedTotal`, `RejectedPartial`;
- actualización controlada de estado a `ReturnedByEpr`;
- bloqueo de actualización para duplicados;
- sin contabilidad;
- sin generación de archivo de respuesta;
- sin generación de archivo de salida;
- sin creación de `AchReturnGenerated` desde ingesta.

### Commits de Fase 4

- `10daf61` — `Introduce return eligibility, incoming-return ingestion and in-process locking; integrate into returns flow and add tests`
- `75385e0` — `feat(returns): auditar payload de devoluciones entrantes`
- `cbc7388` — `feat(returns): clasificar rechazo total o parcial en devoluciones entrantes`
- `851fece` — `test(returns): ordenar pruebas de ingesta entrante`
- `2bed8c7` — `feat(returns): actualizar estado de devoluciones entrantes controladamente`
- `466b06b` — `fix(returns): evitar actualizar devoluciones entrantes duplicadas`

### Matriz de cierre Fase 4

| Componente | Estado | Evidencia técnica | Riesgo residual |
|---|---|---|---|
| `IAchIncomingReturnIngestionService` | Cerrado | Servicio central de ingesta consolidado. | Contrato interno puede crecer en Fase 5. |
| Parseo NACHA entrante | Cerrado para `7/99` | Pruebas de parseo/ingesta en suite dedicada. | Validar contra fixtures reales CENIT/ACH. |
| Linking contra original | Cerrado | Búsqueda por `TraceNumber`/`OriginalTraceRef`. | Trazas incompletas en archivos reales. |
| Resolución de cámara | Cerrado | `ClearingHouseId` desde `AchCycle`. | Históricos sin ciclo/cámara. |
| Validación regulatoria entrante | Cerrado | `ValidateReturnCodeAsync` + `ValidateReturnPolicyAsync`. | Parametrización adicional en UAT regulatorio. |
| Duplicados dentro del archivo | Cerrado | `INCOMING_RETURN_DUPLICATE_IN_FILE`. | Duplicados contra histórico pendiente. |
| Auditoría interna | Cerrado (resultado interno) | Hashes SHA-256, previews limitados, records/failures. | Persistencia formal pendiente. |
| Clasificación total/parcial | Cerrado (interno) | `Accepted` / `RejectedTotal` / `RejectedPartial`. | Respuesta operativa pendiente. |
| Actualización de estado | Cerrado controladamente | Cambio a `ReturnedByEpr` con decisión interna. | Integración contable/conciliación pendiente. |
| Duplicados y actualización | Corregido | Duplicados no actualizan estado. | Falta control histórico persistente. |
| Contabilidad | Fuera de alcance | No se ejecuta contabilidad en ingesta. | Fase posterior. |
| Respuesta operativa | Fuera de alcance | No se genera archivo de respuesta. | Fase 5/6 según diseño. |

### Criterios de aceptación de Fase 4

- [x] Ingesta centralizada.
- [x] Parseo de devolución entrante.
- [x] Linking contra transacción original.
- [x] Cámara resuelta desde ciclo original.
- [x] Validación de causal por cámara.
- [x] Validación de política por cámara.
- [x] Duplicados de archivo detectados.
- [x] Auditoría interna del payload.
- [x] Clasificación total/parcial interna.
- [x] Actualización controlada a `ReturnedByEpr`.
- [x] Duplicados no actualizan estado.
- [x] Sin generación de archivo de respuesta.
- [x] Sin `AchReturnGenerated` desde ingesta.
- [x] Sin contabilidad.
- [x] Suite completa verde.

### Riesgos residuales Fase 4

| Riesgo | Impacto | Control actual | Control futuro recomendado | Fase sugerida |
|---|---|---|---|---|
| Auditoría no persistente | Trazabilidad limitada post-ejecución | Audit result con hashes/previews | Modelo persistente de ingesta entrante | Fase 6 / hardening UAT |
| Duplicados contra histórico | Reproceso de archivo anterior | Duplicados intra-archivo | Índice/tabla persistente de auditoría | Fase 6 |
| Reglas reales CENIT/ACH pueden diferir | Falsos rechazos/aprobaciones | Catálogo por cámara | Parametrización certificada por cámara | UAT |
| Respuesta operativa de rechazo pendiente | Falta de cierre formal operativo | Clasificación interna | Motor de respuesta total/parcial | Fase 5/6 |
| Contabilidad pendiente | Movimiento operativo sin reflejo contable | Sin contabilidad en ingesta | Integración contable y conciliación | Fase 7 |
| Históricos sin ciclo/cámara | Imposibilidad de validar por cámara | `CLEARING_HOUSE_MISSING` | Limpieza/migración de históricos | UAT/hardening |

## Preparación Fase 5 - Devolución de devolución funcional

Objetivo: implementar el flujo funcional de devolución de devolución sobre la base regulatoria por cámara existente (`AchReturnOfReturnPolicy`), evitando mezcla entre CENIT y ACH Colombia.

Objetivos técnicos:

- centralizar elegibilidad de devolución de devolución;
- validar causal original y nueva causal;
- validar `AchReturnOfReturnPolicy` por `ClearingHouseId`;
- resolver cámara desde devolución origen / ciclo / transacción original;
- evitar devolución de devolución duplicada;
- auditar decisión;
- no mezclar reglas CENIT vs ACH Colombia;
- definir actualización de estado controlada;
- mantener formato/naming si se genera archivo;
- preparar tests por cámara.

### Propuesta de commits Fase 5

1. `refactor(returns): centralizar elegibilidad de devolución de devolución`
   - Crear servicio interno para elegibilidad.
   - Resolver contexto de devolución origen.
   - No generar aún.

2. `feat(returns): validar devolución de devolución por cámara`
   - Usar `ValidateReturnOfReturnAsync`.
   - Validar `ClearingHouseId`, causal original, nueva causal, plazo y flujo.

3. `feat(returns): reforzar idempotencia de devolución de devolución`
   - Evitar duplicados.
   - Validar devolución de devolución previa.

4. `feat(returns): generar archivo de devolución de devolución`
   - Solo si el flujo requiere salida NACHA.
   - Mantener naming/formato.

5. `test(returns): cubrir devolución de devolución CENIT y ACH`
   - Tests de no mezcla por cámara.
   - Causales permitidas/rechazadas.

6. `docs(returns): plan UAT devolución de devolución`
   - Casos UAT por cámara.
   - Evidencias y riesgos.

### Límites explícitos para Fase 5

- No asumir que devolución de devolución es igual a devolución simple.
- No reutilizar reglas de `AchReturnPolicy` si existe `AchReturnOfReturnPolicy`.
- No usar cámara global.
- No usar fallback a primera cámara.
- No usar hardcoded `1`.
- No generar contabilidad en el primer commit de Fase 5.
- No mezclar salida/entrada en el mismo commit.
- No tocar migraciones salvo decisión explícita.


## Cierre Fase 5.1 (2026-05-14)
- Se incorporó `IsUniquePerTransaction` en `AchReturnOfReturnEligibilityResult`.
- `AchReturnOfReturnEligibilityService` ahora expone ese flag desde `ValidateReturnOfReturnAsync(...)` y concentra la validación regulatoria de devolución de devolución.
- `ReturnOfReturnOrchestrator` dejó de invocar nuevamente `ValidateReturnOfReturnAsync(...)`; consume el resultado centralizado de elegibilidad para controlar unicidad por transacción.
- Se añadieron pruebas de cobertura para validar propagación de unicidad y rechazo regulatorio, además de registro DI del nuevo servicio.


## Avance Fase 5.2 - Validación de devolución de devolución por cámara

- La elegibilidad de devolución de devolución valida usando el `ClearingHouseId` resuelto desde la devolución/transacción origen.
- La validación regulatoria se mantiene centralizada en `AchReturnOfReturnEligibilityService`.
- `ValidateReturnOfReturnAsync(...)` recibe la cámara correcta y evita mezcla entre CENIT y ACH Colombia.
- `AchReturnOfReturnPolicy` sigue siendo la fuente regulatoria para devolución de devolución; no se reutiliza `AchReturnPolicy`.
- `IsUniquePerTransaction` se conserva como resultado de la política de la cámara evaluada.
- No se usa cámara global, fallback ni hardcoded `1`.
- No se genera archivo.
- No se cambian estados.
- No se ejecuta contabilidad.
- Pendientes: idempotencia avanzada, generación de archivo de devolución de devolución, cobertura UAT por cámara.


## Avance Fase 5.3 - Idempotencia de devolución de devolución

- El registro de `ReturnOfReturnFlow` refuerza idempotencia operativa antes de persistir.
- Se bloquea duplicado exacto `SourceReturnTransactionId + ReturnOfReturnTransactionId`.
- Se bloquea reutilización de `ReturnOfReturnTransactionId` incluso cuando `IsUniquePerTransaction = false`.
- Cuando `IsUniquePerTransaction = true`, se bloquean múltiples devoluciones de devolución para el mismo `SourceReturnTransactionId`.
- La decisión de unicidad sigue dependiendo de `IsUniquePerTransaction` obtenido en elegibilidad centralizada por cámara.
- No se genera archivo.
- No se cambian estados.
- No se ejecuta contabilidad.


## Avance Fase 5.4 - Generación de archivo de devolución de devolución

- Se crea `IAchReturnOfReturnFileGenerationService` / `AchReturnOfReturnFileGenerationService` como servicio separado del orquestador.
- `ReturnOfReturnOrchestrator.RegisterAsync(...)` mantiene responsabilidad exclusiva de registro/idempotencia de flujo.
- La generación se realiza en memoria (sin persistir archivo, sin migraciones, sin nuevas tablas).
- El resultado expone `FileName`, `ContentText`, `Content`, `GeneratedFlowCount`, `FlowIds`, `Failures`.
- Naming determinístico de fase: `ROR_{ClearingHouseId}_{yyyyMMddHHmmss}.ach`.
- No se reejecuta validación regulatoria ni `ValidateReturnOfReturnAsync` en esta fase de generación.
- No se genera contabilidad ni se cambian estados finales.
- Pendiente: homologación NACHA/UAT y persistencia formal de artefactos para fase posterior.

## Cierre técnico Fase 5 - Devolución de devolución funcional

### Estado general
Fase 5 queda cerrada técnicamente para la base funcional de devolución de devolución con alcance implementado en:
- elegibilidad centralizada;
- validación regulatoria con `AchReturnOfReturnPolicy`;
- validación por `ClearingHouseId`;
- no mezcla CENIT vs ACH Colombia;
- propagación de `IsUniquePerTransaction`;
- idempotencia reforzada;
- generación en memoria de archivo de devolución de devolución;
- naming determinístico interno;
- cobertura de generación complementaria;
- sin contabilidad;
- sin endpoints públicos nuevos;
- sin migraciones;
- sin persistencia formal del archivo generado;
- sin modificación de golden master de devolución simple.

### Commits de Fase 5
> Nota: listado con hashes cortos reales disponibles en la rama actual. Si algún cambio fue consolidado por squash/merge, se conserva el commit visible en esta rama.

- `e97ebf3` `Centralize Return-of-Return eligibility, add in-memory file generation service and integrate with orchestrator` (consolidado en esta rama; engloba elegibilidad, validación por cámara, idempotencia y generación en memoria).
- `7408247` `test(returns): cubrir generación de archivo por cámara`.
- `209131b` `docs(returns): cerrar Fase 5 y preparar UAT devolución de devolución`.

### Matriz de cierre Fase 5
| Componente | Estado | Evidencia técnica | Riesgo residual |
|---|---|---|---|
| Elegibilidad centralizada | Cerrado | `IAchReturnOfReturnEligibilityService` | Reglas reales pueden variar en UAT |
| Resultado de elegibilidad | Cerrado | `IsUniquePerTransaction` | Política por cámara debe validarse con archivos reales |
| Validación regulatoria | Cerrado | `ValidateReturnOfReturnAsync` | Homologación con CENIT/ACH |
| No mezcla por cámara | Cerrado | Tests CENIT vs ACH Colombia | Catálogos productivos deben estar completos |
| Idempotencia | Cerrado | Controles por `SourceReturnTransactionId`, `ReturnOfReturnTransactionId`, combinación exacta | Concurrencia multi-nodo |
| Generación de archivo | Cerrado en memoria | `IAchReturnOfReturnFileGenerationService` | Formato NACHA definitivo pendiente UAT |
| Naming | Cerrado como naming determinístico interno | `ROR_{ClearingHouseId}_{yyyyMMddHHmmss}.ach` | Naming productivo/certificado pendiente |
| Persistencia del artefacto | Pendiente | No se usa `AchReturnGenerated` | Trazabilidad formal del archivo generado |
| Estados finales | Fuera de alcance | Generación no cambia estados | Definir transición operacional posterior |
| Contabilidad | Fuera de alcance | No se ejecuta contabilidad | Integración contable/conciliación pendiente |
| Endpoints públicos | Fuera de alcance | No se modifican contratos públicos | Exposición controlada pendiente |
| Golden master | Preservado | No se modifica golden master de devolución simple | Golden master específico return-of-return pendiente |

### Criterios de aceptación de Fase 5
- [x] Elegibilidad centralizada de devolución de devolución.
- [x] Uso de `AchReturnOfReturnPolicy`.
- [x] `ValidateReturnOfReturnAsync` centralizado.
- [x] `IsUniquePerTransaction` expuesto.
- [x] Validación por `ClearingHouseId`.
- [x] No mezcla CENIT vs ACH Colombia.
- [x] Idempotencia reforzada.
- [x] Registro de `ReturnOfReturnFlow` separado de generación.
- [x] Generación en memoria.
- [x] Naming determinístico interno.
- [x] Validación de cámara consistente source/ror.
- [x] Tests de generación y cobertura complementaria.
- [x] Sin contabilidad.
- [x] Sin cambios de endpoints públicos.
- [x] Sin migraciones.
- [x] Sin persistencia formal de archivo.
- [x] Sin cambios al golden master de devolución simple.
- [x] Suite completa verde.

### Riesgos pendientes
| Riesgo | Impacto | Control actual | Control futuro recomendado | Fase sugerida |
|---|---|---|---|---|
| Formato NACHA definitivo pendiente | Archivo en memoria puede no cumplir homologación final | Golden master preliminar técnico por cámara | Golden master homologado/certificado por cámara con archivos reales CENIT/ACH Colombia | UAT / Fase 6 |
| Persistencia formal del artefacto | Trazabilidad limitada | Resultado en memoria | Entidad/auditoría persistente aprobada | Fase 6 |
| Naming productivo | Puede requerir nombre certificado por operador/cámara | `ROR_{ClearingHouseId}_{yyyyMMddHHmmss}.ach` | Integración con naming oficial | UAT |
| Contabilidad pendiente | Flujo técnico sin asiento contable | Contabilidad no ejecutada | Integración contable/conciliación | Fase 7 |
| Concurrencia multi-nodo | Riesgo de duplicados distribuidos | Idempotencia por consultas | Constraints/locks persistentes (si se aprueban migraciones) | Hardening producción |
| Catálogos reales incompletos | Falsos rechazos/aprobaciones | Catálogo por cámara | Carga certificada CENIT/ACH | UAT |
| Endpoints públicos pendientes | Flujo no expuesto operativamente | Servicios internos | API controlada con autorización/auditoría | Fase posterior |

## Plan UAT - Devolución de devolución CENIT y ACH Colombia
Objetivo: validar que la devolución de devolución funcione por cámara, sin mezcla de reglas, respetando idempotencia, generación en memoria y límites actuales.

### Golden master preliminar para UAT técnico
- Se agregaron golden masters preliminares por cámara para la generación en memoria de devolución de devolución.
- CENIT queda cubierto con `ClearingHouseId = 7001`.
- ACH Colombia queda cubierto con `ClearingHouseId = 7002`.
- Los tests validan `FileName`, `ContentText`, `Content` ASCII, `GeneratedFlowCount` y `FlowIds`.
- Los tests validan rechazo por mezcla de cámaras CENIT/ACH.
- Los tests validan múltiples flows de la misma cámara con orden estable.
- Estos golden masters son técnicos/preliminares.
- No equivalen a homologación NACHA final.
- El formato definitivo queda pendiente de UAT/certificación por cámara.

| ID | Cámara | Escenario | Entrada | Resultado esperado | Evidencia esperada |
|---|---|---|---|---|---|
| UAT-ROR-001 | CENIT | Devolución de devolución permitida | source return CENIT + nueva causal permitida | Elegible, flow registrado, archivo en memoria generado | `ReturnOfReturnFlow`, `FileName ROR_...`, contenido generado |
| UAT-ROR-002 | ACH Colombia | Devolución de devolución permitida | source return ACH + nueva causal permitida | Igual al caso CENIT por su cámara | `ReturnOfReturnFlow`, `FileName ROR_...`, contenido generado |
| UAT-ROR-003 | CENIT | Causal permitida solo en ACH | causal fuera de política CENIT | Rechazo por política | Failure `RETURN_OF_RETURN_POLICY_REJECTED` |
| UAT-ROR-004 | ACH Colombia | Causal permitida solo en CENIT | causal fuera de política ACH | Rechazo por política | Failure `RETURN_OF_RETURN_POLICY_REJECTED` |
| UAT-ROR-005 | CENIT | source/ror con cámaras distintas | flow inconsistente | No genera archivo | Failure `CLEARING_HOUSE_MISSING` |
| UAT-ROR-006 | CENIT | Duplicado exacto source + ror | registro repetido | Rechazo por idempotencia | evidencia de rechazo por duplicado |
| UAT-ROR-007 | ACH Colombia | Reutilización de `ReturnOfReturnTransactionId` | ror transaction ya usada | Rechazo por idempotencia | evidencia de rechazo por ror repetida |
| UAT-ROR-008 | CENIT | `IsUniquePerTransaction = true` con source ya usado | nueva ror sobre mismo source | Rechazo | evidencia de unicidad por source |
| UAT-ROR-009 | ACH Colombia | `IsUniquePerTransaction = false` con source previo | nueva ror transaction distinta | Permitido | flow adicional válido |
| UAT-ROR-010 | CENIT | Generación múltiples flows misma cámara | 2+ flows CENIT | Archivo en memoria con count correcto | `GeneratedFlowCount`, `FlowIds`, `ContentText` |
| UAT-ROR-011 | ACH Colombia | Intento de generar flows mezclados CENIT + ACH | lote mixto | Rechazo de generación | Failure de cámara |
| UAT-ROR-012 | Ambas | Validar no contabilidad | ejecución completa | No se crean asientos | evidencia contable nula |
| UAT-ROR-013 | Ambas | Validar no persistencia formal del archivo | ejecución completa | No se crea `AchReturnGenerated` | evidencia de ausencia |
| UAT-ROR-014 | Ambas | Validar no cambio de estados | ejecutar generación | Estados originales permanecen | comparación estado previo/posterior |

### Evidencias esperadas UAT
- registro en `ReturnOfReturnFlow`;
- resultado de elegibilidad;
- `ClearingHouseId` evaluado;
- `OriginalReturnReasonCode`;
- `NewReturnReasonCode`;
- `IsUniquePerTransaction`;
- resultado de generación en memoria;
- `FileName`;
- `ContentText`;
- `GeneratedFlowCount`;
- `FlowIds`;
- ausencia de `AchReturnGenerated`;
- ausencia de asientos contables;
- estados sin modificación;
- logs/evidencia técnica del test.
- golden master preliminar CENIT aprobado;
- golden master preliminar ACH Colombia aprobado;
- `FileName` esperado:
  - `ROR_7001_20260514123456.ach`
  - `ROR_7002_20260514123456.ach`
- `ContentText` exacto comparado contra baseline preliminar;
- `Content` ASCII equivalente a `ContentText`;
- rechazo de generación cuando se mezclan flows de CENIT y ACH Colombia;
- validación de orden estable para múltiples flows de una misma cámara.

### Límites explícitos post-Fase 5
- La generación actual es en memoria.
- No hay persistencia formal del archivo.
- No hay contabilidad.
- No hay endpoint público nuevo.
- No hay naming productivo certificado.
- No hay golden master NACHA definitivo para devolución de devolución.
- No se debe asumir homologación final CENIT/ACH hasta UAT.
- No se modificó devolución simple.
- No se modificó devolución entrante.

## Avance Fase 6 - Auditoría persistente de archivo de devolución de devolución
- Se agregó auditoría específica para devolución de devolución mediante `AchReturnOfReturnGeneratedFileAudit` y `AchReturnOfReturnGeneratedFileAuditFlow`.
- No se reutiliza `AchReturnGenerated`.
- Se persisten metadatos técnicos: `FileName`, `ClearingHouseId`, `GeneratedAtUtc`, `GeneratedFlowCount`, `ContentLength`, `ContentSha256`, `RequestedBy`, `Source` y `CreatedAtUtc`.
- Se persiste relación explícita con los `ReturnOfReturnFlow` incluidos.
- No se persiste contenido completo del archivo (`ContentText` ni bytes de `Content`).
- No se cambian estados de transacciones.
- No hay contabilidad.
- No hay endpoints públicos nuevos.

### Decisión de migración
En esta rama las migraciones se encuentran ignoradas/no versionadas (`**/Migrations/` en `.gitignore`). Por tanto, el snapshot se revierte en este commit correctivo para evitar inconsistencias de modelo versionado sin migración física. La migración física queda pendiente para la política de despliegue del equipo. Antes de usar la auditoría persistente en ambiente real debe generarse/aplicarse la migración correspondiente.

## Política de migraciones EF no versionadas
- En este proyecto/rama las migraciones EF no se versionan.
- Las migraciones se generan/aplican en ejecución, pruebas o flujo operativo local del equipo.
- La carpeta `Migrations` está ignorada/no hace parte del commit funcional.
- El `AchDbContextModelSnapshot` no debe usarse como fuente de verdad para exigir commits de migración en esta rama si la política es no versionar migraciones.
- La auditoría ROR queda lista a nivel de modelo/configuración/servicio/tests.
- Para usar auditoría ROR en una BD real, el ambiente debe generar/aplicar la migración física correspondiente.
- `Database.Migrate()` aplica migraciones compiladas existentes; no genera migraciones nuevas automáticamente.
- Si el equipo usa `EnsureCreated` o recreación de BD en pruebas, validar que las tablas de auditoría existan antes de ejecutar UAT.

## Checklist de despliegue UAT - Auditoría de devolución de devolución

### Objetivo del checklist
- Validar que la auditoría persistente de archivo de devolución de devolución pueda ejecutarse en UAT.
- Confirmar que las tablas requeridas existen antes de habilitar uso operativo.
- Confirmar que se persiste metadata, hash SHA-256 y relación de flows.
- Confirmar que no se persiste contenido completo del archivo.
- Confirmar que no se toca contabilidad ni estados de transacciones.

### Precondiciones
- [ ] Confirmar rama/versión a desplegar.
- [ ] Confirmar mecanismo usado por el equipo para crear esquema:
  - migración generada/aplicada localmente;
  - script SQL operativo;
  - recreación de BD de pruebas;
  - `EnsureCreated` si aplica solo para pruebas.
- [ ] Confirmar que la migración física fue generada por el flujo oficial del equipo.
- [ ] Confirmar que la migración crea las tablas:
  - `AchReturnOfReturnGeneratedFileAudits`
  - `AchReturnOfReturnGeneratedFileAuditFlows`
- [ ] Confirmar que la migración incluye FK hacia `ReturnOfReturnFlow`.
- [ ] Confirmar que la migración no modifica tablas de devolución simple.
- [ ] Confirmar que la migración no modifica catálogos regulatorios.
- [ ] Confirmar backup/snapshot de BD antes de aplicar.
- [ ] Confirmar ventana de UAT.
- [ ] Confirmar credenciales/permisos para aplicar migración.
- [ ] Confirmar plan de rollback.
- [ ] Confirmar que no se depende de una migración versionada en esta rama.

### Validación post-migración
- [ ] Verificar existencia de tabla `AchReturnOfReturnGeneratedFileAudits`.
- [ ] Verificar existencia de tabla `AchReturnOfReturnGeneratedFileAuditFlows`.
- [ ] Verificar columnas:
  - `FileName`
  - `ClearingHouseId`
  - `GeneratedAtUtc`
  - `GeneratedFlowCount`
  - `ContentLength`
  - `ContentSha256`
  - `RequestedBy`
  - `Source`
  - `CreatedAtUtc`
- [ ] Verificar que no existen columnas:
  - `ContentText`
  - `Content`
  - `FileBytes`
- [ ] Verificar FK de flows hacia auditoría.
- [ ] Verificar FK de flows hacia `ReturnOfReturnFlow`.
- [ ] Verificar índice por `FileName`.
- [ ] Verificar índice/longitud de `ContentSha256`.

### Prueba funcional UAT
1. Preparar `ReturnOfReturnFlow` válido para CENIT.
2. Ejecutar generación en memoria.
3. Confirmar `IsGenerated = true`.
4. Confirmar `FileName`.
5. Confirmar `ContentSha256`.
6. Confirmar `AuditId`.
7. Consultar auditoría persistida.
8. Confirmar metadata.
9. Confirmar relación con flows.
10. Confirmar ausencia de contenido completo.
11. Repetir caso para ACH Colombia.
12. Ejecutar caso fallido y confirmar que no crea auditoría.

### SQL orientativo para validación UAT
```sql
SELECT
    "Id",
    "FileName",
    "ClearingHouseId",
    "GeneratedAtUtc",
    "GeneratedFlowCount",
    "ContentLength",
    "ContentSha256",
    "RequestedBy",
    "Source",
    "CreatedAtUtc"
FROM "AchReturnOfReturnGeneratedFileAudits"
ORDER BY "Id" DESC
LIMIT 10;
```

```sql
SELECT
    "Id",
    "AchReturnOfReturnGeneratedFileAuditId",
    "ReturnOfReturnFlowId"
FROM "AchReturnOfReturnGeneratedFileAuditFlows"
ORDER BY "Id" DESC
LIMIT 20;
```

### Criterios de aceptación UAT
- [ ] Migración física generada por el flujo oficial del equipo.
- [ ] Migración aplicada correctamente en UAT.
- [ ] Tablas `AchReturnOfReturnGeneratedFileAudits` y `AchReturnOfReturnGeneratedFileAuditFlows` creadas.
- [ ] Generación exitosa crea una auditoría.
- [ ] Generación fallida no crea auditoría.
- [ ] `AuditId` retornado cuando la generación es exitosa.
- [ ] `ContentSha256` retornado y persistido.
- [ ] `ContentSha256` tiene longitud 64.
- [ ] `ContentLength` coincide con los bytes generados en memoria.
- [ ] `GeneratedFlowCount` coincide con los flows incluidos.
- [ ] La relación con `ReturnOfReturnFlow` queda persistida.
- [ ] No se persiste `ContentText`.
- [ ] No se persiste `Content` binario.
- [ ] No se persiste `FileBytes`.
- [ ] No se crea `AchReturnGenerated`.
- [ ] No cambian estados de transacciones.
- [ ] No se generan asientos contables.
- [ ] No se modifica devolución simple.
- [ ] No se modifica devolución entrante.
- [ ] No se exponen endpoints públicos nuevos.
- [ ] Evidencias SQL y logs quedan adjuntos al caso UAT.

### Rollback
- Si la migración falla antes de completarse, detener el despliegue y no habilitar la auditoría persistente.
- Si la migración se aplicó parcialmente, ejecutar el rollback oficial del equipo según la herramienta de despliegue utilizada.
- Si se requiere reversión manual en UAT, debe hacerse bajo aprobación técnica y con backup previo.
- En reversión manual, eliminar primero registros hijos de `AchReturnOfReturnGeneratedFileAuditFlows`.
- Luego eliminar registros de `AchReturnOfReturnGeneratedFileAudits`.
- No eliminar registros de `ReturnOfReturnFlow`.
- No eliminar transacciones ACH.
- No modificar catálogos regulatorios.
- No modificar registros de devolución simple.
- No revertir cambios de negocio fuera del alcance de auditoría.
- Documentar evidencia del rollback:
  - hora;
  - responsable;
  - scripts ejecutados;
  - resultado;
  - validación posterior.

```sql
-- Ejemplo UAT: ejecutar solo bajo aprobación y con backup previo.
DELETE FROM "AchReturnOfReturnGeneratedFileAuditFlows"
WHERE "AchReturnOfReturnGeneratedFileAuditId" IN (
    SELECT "Id"
    FROM "AchReturnOfReturnGeneratedFileAudits"
    WHERE "GeneratedAtUtc" >= TIMESTAMP '2026-05-14 00:00:00'
);

DELETE FROM "AchReturnOfReturnGeneratedFileAudits"
WHERE "GeneratedAtUtc" >= TIMESTAMP '2026-05-14 00:00:00';
```

### Riesgos pendientes
| Riesgo | Impacto | Control actual | Acción pendiente |
|---|---|---|---|
| Migración física pendiente en rama con `Migrations` ignoradas | Auditoría persistente no utilizable en ambiente real hasta migrar DB | Documentación + checklist UAT + decisión explícita de migración pendiente | Generar y aplicar migración por flujo oficial antes de habilitar en UAT real |
| Migraciones EF no versionadas | Diferencias entre modelo y BD si el ambiente no aplica correctamente el esquema | Documentación, tests de metadata EF y checklist post-migración/post-creación de esquema | Automatizar validación de esquema en arranque/UAT o script operativo de verificación |
| Error operativo al ejecutar rollback manual | Pérdida de trazabilidad de auditoría o inconsistencias en UAT | Guía de rollback con orden hijo->padre y restricción de alcance | Ejecutar rollback solo con aprobación técnica, backup y evidencia posterior |
| Datos sin hash o hash inválido por configuración incorrecta | Debilita trazabilidad e integridad de evidencia | Validación de `ContentSha256` (longitud 64) en checklist UAT | Adjuntar evidencia SQL/log por caso UAT y bloquear pase si no cumple |
| Cambios fuera de alcance (contabilidad/endpoints/devolución simple) durante despliegue | Riesgo funcional y regulatorio | Checklist explicita no contabilidad, no endpoints públicos, no cambios de devolución simple/entrante | Gate de despliegue con revisión técnica previa y verificación post-ejecución |

## Bloqueo técnico - Migración física de auditoría ROR
- Se intentó generar la migración física `AddReturnOfReturnGeneratedFileAudit`.
- El intento fue bloqueado porque EF detecta drift previo en el modelo regulatorio.
- El drift incluye cambios no relacionados con auditoría ROR:
  - `AchReturnCode`
  - `AchReturnPolicy`
  - `AchReturnOfReturnPolicy`
  - campos de cámara/flujo/vigencia
  - relaciones con `ClearingHouse`
  - índices regulatorios
- Por seguridad no se versionó la migración.
- No se debe commitear una migración ROR mezclada con cambios regulatorios históricos.
- El drift EF bloquea únicamente la creación de una migración versionada aislada.
- No bloquea la continuidad funcional si la política del equipo es no versionar migraciones.
- La auditoría ROR sigue implementada a nivel de modelo/config/servicio.
- La auditoría ROR requiere que el ambiente genere/aplique la migración física por el flujo operativo local antes de uso real.
- El snapshot se mantiene limpio/restaurado según política de rama.
- Próximo paso recomendado:
  1. no commitear migración mezclada con drift regulatorio;
  2. ejecutar generación/aplicación de esquema por flujo operativo local del equipo;
  3. verificar tablas de auditoría antes de UAT.

### Evidencia de drift detectado
- `AchReturnCode`: el modelo/config actual incluye `ClearingHouseId`, `FlowType`, `EffectiveFrom`, `EffectiveTo`, relación con `ClearingHouse` e índice compuesto por cámara/código/flujo/vigencia.
- `AchReturnPolicy`: el modelo/config actual incluye `ClearingHouseId`, `Direction`, `FlowType`, `EffectiveFrom`, `EffectiveTo`, `MaxCycles` e índice compuesto por cámara/tipo/dirección/flujo/activo.
- `AchReturnOfReturnPolicy`: el modelo/config actual incluye `ClearingHouseId`, `Direction`, `FlowType`, `EffectiveFrom`, `EffectiveTo`, relación con `ClearingHouse` e índice compuesto por cámara/código original/dirección/flujo/activo.
- El `AchDbContextModelSnapshot` vigente en rama no refleja esas dimensiones regulatorias en el mismo nivel que el modelo/config actual, por lo que EF intenta reconciliarlas junto con la migración de auditoría ROR.

## 8) No regresión

- No se modifica el flujo de devolución normal en esta actualización documental.
- No se modifica el flujo incoming NACHA.
- No se modifica parser NACHA ni clasificación funcional.
- No se crean migraciones ni se altera esquema en esta tarea.
- No se tocan reglas regulatorias base; solo se actualiza estado de cierre técnico ROR y readiness documental.

## 9) Pendientes para GO productivo

1. Validación ROR con datos reales en circuito operativo controlado.
2. Validación/certificación por cámara (CENIT y ACH Colombia según aplique).
3. Aceptación formal de formato NACHA-M ROR por contraparte/cámara.
4. Confirmación de naming externo definitivo para intercambio productivo.
5. Trazabilidad cerrada requisito→norma→código→prueba→evidencia para ROR.
6. Acta UAT cerrada sin pendientes críticos.
7. Firma de negocio, compliance y operaciones para habilitación productiva.


## 10) Referencia vigente de naming externo (ACH/CENIT/STA)

- Para todo análisis de naming externo aplicable a devoluciones y devolución de devolución, la fuente vigente es `docs/audits/external-filename-normative-matrix-ach-cenit-sta-current.md`.
- Las matrices `docs/audits/external-filename-normative-matrix-ach-cenit-sta-2026-04-20.md` y `docs/audits/external-filename-normative-matrix-ach-cenit-sta-2026-04-20-v2.md` quedan como histórico documental.
- El estado de readiness de naming externo se mantiene en **NO-GO productivo** hasta cierre normativo/operativo y remediación de hardcodes (`RET_...`, `RORNACHA_...`).

- Fuente de control vigente para revisión de registros NACHA-M (1/5/6/7/8/9) por cámara: `docs/audits/nacha-record-level-normative-matrix-ach-cenit-current.md`.
