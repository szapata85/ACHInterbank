# Integraciones SOAP internas asociadas a rutas REST visibles en Scalar

**Fecha (UTC):** 2026-04-26  
**Ámbito:** `Proc_Transacciones` y `Proc_Contrapartidas`  
**Objetivo:** documentar, para equipo de desarrollo y soporte, cómo las rutas REST publicadas en Scalar se relacionan con las integraciones SOAP internas.

---

## 1) Resumen ejecutivo

En ACHInterbank, las integraciones SOAP no se exponen como rutas SOAP en Scalar. En su lugar:

- las rutas REST registran transacciones, ingestas y acciones operativas;
- los servicios de Persistence orquestan despacho;
- `IWscfaachSoapClient` construye y envía el `soap:Envelope` hacia los métodos remotos `Proc_Transacciones` y `Proc_Contrapartidas`.

Por diseño, la integración se desacopla en cuatro capas:

1. **Disparo funcional REST** (controller).
2. **Orquestación interna** (service/job/orchestrator).
3. **Construcción y parseo SOAP** (mappers/parsers).
4. **Persistencia de trazabilidad y auditoría** (tablas de ejecución/eventos/intentos).

---

## 2) Mapa REST → integración SOAP

## 2.1 Proc_Transacciones

| Ruta REST (visible en Scalar) | Cuándo participa en SOAP | Servicio interno que la conecta | Observación operativa |
|---|---|---|---|
| `POST /NachaUpload/upload` | Después de ingesta y clasificación, cuando se generan items elegibles en cola de dispatch | `IIncomingNachaIngestionAppService` + pipeline de post-proceso inbound | No invoca SOAP en línea HTTP; habilita procesamiento asíncrono posterior. |
| `POST /incoming-nacha-command-center/queue/{queueId}/retry` | Rehabilita item para nuevo intento de `Proc_Transacciones` | `IIncomingNachaCommandCenterService` + `IncomingNachaStateMachineService` | Acción manual de contingencia; el SOAP lo ejecuta el orquestador al correr el task. |
| `POST /incoming-nacha-command-center/queue/{queueId}/unblock` | Igual: habilita flujo para reintentar integración | idem | Controlado por estado permitido. |
| `POST /incoming-nacha-command-center/queue/{queueId}/requeue` | Reencola item para ejecución SOAP posterior | idem | Requiere justificación operativa. |

## 2.2 Proc_Contrapartidas

| Ruta REST (visible en Scalar) | Cuándo participa en SOAP | Servicio interno que la conecta | Observación operativa |
|---|---|---|---|
| `POST /Transactions` | Al registrar transacción, se crea/asegura pendiente de contrapartida | `IAchTransactionService` → `IContrapartidaDispatchPersistenceService` | No llama SOAP directo; deja item listo para job. |
| `POST /Transactions/bulk` | Igual, por cada transacción válida del lote | `IAchBulkTransactionService` + persistencia de dispatch | El SOAP lo ejecuta el task programado/manual. |
| Gestión de tasks (`/TaskDefinitions`) | Permite parametrizar ejecución de jobs que consumen cola | Scheduler + handlers | El método SOAP se ejecuta en job `AchContrapartidasByCycle`. |

> Nota: `Proc_Contrapartidas` se ejecuta principalmente por tarea de Quartz (`AchContrapartidasByCycleHandler`), no por invocación HTTP síncrona.

---

## 3) Componentes técnicos por integración

## 3.1 Cliente externo común

**Clase consumidora externa:** `WscfaachSoapClient` (`src/Cfa.ACHInterbank.External/Connections/WscfaachSoapClient.cs`).

Responsabilidades:

- resolver endpoint y `SoapAction` por método desde `ISoapIntegrationSettingsService`;
- construir `soap:Envelope` y `soap:Body`;
- enviar `POST text/xml` con `HttpClientFactory`;
- retornar XML crudo de respuesta;
- elevar excepción técnica si HTTP status no es exitoso.

Métodos relevantes:

- `ProcTransaccionesAsync(...)` → acción SOAP `Proc_Transacciones`.
- `ProcContrapartidasAsync(...)` → acción SOAP `Proc_Contrapartidas`.

---

## 4) Proc_Transacciones

## 4.1 Objetivo funcional

Despachar transacciones inbound NACHA-M elegibles hacia el backend externo de transacciones, registrando resultado técnico/funcional para decidir confirmación, reintento o falla final.

## 4.2 Orquestación interna

1. `IncomingNachaPostProcessingHandler` ejecuta task `IncomingNachaPostProcessing`.
2. Invoca `IncomingNachaPostProcessingOrchestrator.ExecuteAsync(...)`.
3. El orquestador toma items `Queued/RetryPending` de `IncomingNachaDispatchQueue`.
4. `ProcTransaccionesRequestMapper` resuelve contrato y genera cuerpo SOAP.
5. `IWscfaachSoapClient.ProcTransaccionesAsync` envía request.
6. `ProcTransaccionesResponseParser` clasifica respuesta.
7. Se persiste `IncomingNachaIntegrationExecution` y se actualiza estado de cola.
8. Se registra evento automático en `IncomingNachaProcessingEvents`.

## 4.3 Datos enviados

`ProcTransaccionesRequestMapper` construye contrato con parámetros como:

- identidad y cuentas (`NCTAORIG`, `NCTARECEP`, `IDORIG`, `IDRECEP`),
- monetarios (`MONTO`),
- trazabilidad (`IDTRAN`, `IDLOTE`, `IDCAMCOMPE`, `DIRECCIONIP`),
- campos de negocio y auxiliares (`TIPTRAN`, `TREG`, `LIBRE`, `LIBRE1`, etc.).

## 4.4 Datos recibidos

`ProcTransaccionesResponseParser` extrae, entre otros:

- código de respuesta (`RTAACH`, `ANSST`, `Code`, etc.),
- mensaje (`RTALOC`, `ANSMEN`, `faultstring`, etc.),
- XML crudo para auditoría técnica.

## 4.5 SOAP Fault y clasificación de errores

Reglas principales:

- si existe nodo `Fault`, se considera SOAP Fault;
- SOAP Fault entra como **retryable técnico** salvo agotamiento de intentos;
- si no es éxito y no es retryable, se clasifica como **rechazo funcional**.

Normalización operativa en orquestador:

- códigos técnicos: `I500`, `I503`, `ITIMEOUT`, `ISOAP`;
- rechazo funcional consolidado: `IFUNC`.

## 4.6 Impacto en reintentos

- si es retryable y `AttemptCount < MaxAttempts`, estado → `RetryPending` con `NextAttemptAtUtc`;
- si agota intentos, estado → `FailedFinal`;
- si el mapeo es inválido o falta contexto, estado → `Blocked`.

## 4.7 Auditoría y trazabilidad

Persistencias clave:

- `IncomingNachaIntegrationExecution`: request/response, hashes, mapping version, código/mensaje, éxito/retryable;
- `IncomingNachaProcessingEvents`: eventos `DispatchStarted`, `IntegrationSucceeded`, `IntegrationRetryableFailed`, `IntegrationNonRetryableFailed`, `MaxAttemptsExceeded`, etc.

---

## 5) Proc_Contrapartidas

## 5.1 Objetivo funcional

Reportar/confirmar contrapartidas por ciclo ACH hacia servicio externo, con control por batch, resultado por item y soporte de reintento.

## 5.2 Cuándo se invoca

- sobre items pendientes en `ContrapartidaDispatchItems`;
- normalmente por task `AchContrapartidasByCycle` (Quartz);
- también en reintentos manuales de batch dentro del `ContrapartidaDispatchJobService`.

## 5.3 Orquestación interna

1. `AchContrapartidasByCycleHandler.ExecuteAsync(...)` detecta ciclos activos en ventana.
2. Por ciclo llama `ContrapartidaDispatchJobService.ProcessCycleAsync(...)`.
3. El service reclama items elegibles (`Pending/QueuedForContrapartida/RetryPending`).
4. `ProcContrapartidasRequestMapper.ResolveAsync(...)` produce contrato (mapping publicado o fallback transicional).
5. `IWscfaachSoapClient.ProcContrapartidasAsync(...)` envía SOAP.
6. `ProcContrapartidasResponseParser` interpreta respuesta global y por transacción.
7. Registra `ContrapartidaDispatchAttempt` por item y actualiza estado final.

## 5.4 Datos enviados

Contrato `ProcContrapartidasRequestContract` incluye campos como:

- identificación y cuenta (`OFNIT`, `OFCTA`),
- débito/crédito y fecha (`OFDD`, `OFFECHEFEC`, `OFMONDEB`, `OFMONCRE`),
- trazabilidad (`OFIDARCH`, `OFIDLOT`, `OFIDTX`, `OFIDCAMCOMPE`, `OFDIRECCIONIP`),
- campos de salida/estado (`ANSIDLOTE`, `ANSST`, `ANSIDTX`, etc.).

## 5.5 Datos recibidos

El parser extrae:

- código y mensaje global,
- bandera de SOAP Fault,
- clasificación retryable/funcional,
- resultados por item (cuando el XML trae resultados por transacción).

## 5.6 SOAP Fault y clasificación técnica/funcional

- SOAP Fault o excepción de transporte → técnico, potencialmente retryable;
- códigos de negocio no retryables → rechazo funcional;
- respuesta parcial por item permite mezclar `Success/Partial/Failed` en un mismo batch.

## 5.7 Reintentos

- cada item incrementa `AttemptCount`;
- si falla retryable y no supera máximo, estado `RetryPending` con próxima ventana;
- agotado máximo, estado `ContrapartidaReportFailed`.

## 5.8 Auditoría y trazabilidad

Persistencias clave:

- `ContrapartidaDispatchBatch`: resumen por ejecución (mapping, payloads, conteos);
- `ContrapartidaDispatchAttempt`: evidencia por item (request/response, código externo, retryEligible);
- `ContrapartidaDispatchItem`: estado operativo y última correlación.

---

## 6) Configuración y seguridad operativa

`SoapIntegrationSettingsService` administra mapeos por método (`MethodName`, `Endpoint`, `SoapAction`, `Enabled`, parámetros de entrada), persistidos en `SoapIntegrationSetting` como JSON.

Buenas prácticas para ambientes:

- no hardcodear endpoints reales en código operativo;
- mantener `Enabled` por método para bloqueo controlado;
- auditar cambios de configuración SOAP;
- no exponer credenciales ni endpoints sensibles en documentación pública.

---

## 7) Errores esperados y lectura operativa

## 7.1 Técnicos (retryable)

- timeout/transporte HTTP,
- SOAP Fault de infraestructura,
- parser error por respuesta no parseable,
- servicio remoto temporalmente no disponible.

Acción: `RetryPending` si hay intentos disponibles; escalar si se agota política.

## 7.2 Funcionales (no retryable)

- rechazo de negocio por validaciones de datos,
- reglas de cámara/ciclo no cumplidas,
- respuesta explícita no retryable en parser.

Acción: `FailedFinal`/`ContrapartidaReportFailed`, análisis funcional y corrección de datos.

---

## 8) Pruebas existentes y faltantes

## 8.1 Evidencia de pruebas existentes

- `ProcTransaccionesRequestMapperTests`.
- `ProcTransaccionesResponseParserTests`.
- `IncomingNachaPostProcessingOrchestratorTests` (incluye caso `Fault/faultstring`, retries y estados).
- `IntegrationMappingEndToEndTests` (resolución de mapping funcional para contrapartidas).

## 8.2 Faltantes recomendados para producción

1. Pruebas dedicadas de `ProcContrapartidasResponseParser` (casos globales y por item).
2. Pruebas automatizadas de `ContrapartidaDispatchJobService` con:
   - éxito total,
   - parcial por item,
   - SOAP Fault retryable,
   - agotamiento de intentos.
3. Pruebas de contrato con proveedor SOAP en ambiente de integración (sin datos sensibles).
4. Tablero operativo con métricas de tasa de fallo técnico vs funcional por método.

---

## 9) Estado para decisión de avance/no avance

**Fortalezas actuales:**

- desacople claro entre REST y transporte SOAP;
- persistencia de trazabilidad técnica;
- clasificación de errores y política de reintentos implementada;
- control de configuración por método SOAP.

**Brechas para endurecimiento productivo:**

- cobertura de pruebas insuficiente en rama `Proc_Contrapartidas` parser/job;
- necesidad de pruebas de contrato externas recurrentes;
- formalizar runbook de operación para respuesta ante degradación SOAP.

**Conclusión técnica:**

La arquitectura actual permite operación controlada de `Proc_Transacciones` y `Proc_Contrapartidas` desde flujos REST visibles en Scalar, con buen nivel de trazabilidad. Para cierre robusto de producción se recomienda completar pruebas faltantes en contrapartidas y evidencias de contrato externas.
