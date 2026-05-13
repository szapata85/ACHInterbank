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

- Existe **modelo + orquestación** (`AchReturnOfReturnPolicy`, `ReturnOfReturnFlow`, `ReturnOfReturnOrchestrator`).
- Se valida política de causal original/nueva vía catálogo regulatorio.
- Se controla unicidad según política (`IsUniquePerTransaction`) a nivel de validación.
- Diferenciación por cámara: **insuficiente** (no eje principal del modelo regulatorio actual).
- Vinculación a ciclo CENIT/ACH: existe referencia operativa en flujo, pero no regla de elegibilidad por cámara completamente parametrizada.
- Generación de archivo: depende del flujo general de returns; no se observa un generador dedicado con naming/reglas específicos por cámara para return-of-return.

### Respuestas explícitas
- ¿Solo existe modelo o también orquestación? **Ambos.**
- ¿Se valida causal original? **Sí.**
- ¿Se valida nueva causal? **Sí.**
- ¿Se controla unicidad? **Sí, por política.**
- ¿Se diferencia por cámara? **No de forma robusta.**
- ¿Se vincula a ciclo CENIT/ACH? **Parcialmente.**
- ¿Se genera archivo? **Sí por flujo de returns general, sin especialización fuerte por cámara para este subcaso.**

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
