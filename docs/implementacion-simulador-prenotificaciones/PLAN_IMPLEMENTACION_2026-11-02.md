**Avance 2026-11-02 / I01:** contrato, enums, endpoints de consulta y tests implementados; comprobaciones estáticas 20/20 PASS; pendiente `dotnet build/test`. El plan original sigue vigente para I02–I07.

# Plan de implementación incremental — Respuestas a prenotificaciones salientes

**Proyecto:** ACH Interbank  
**Base técnica:** `ACHInterbank_PROYECTO_COMPLETO_CORRECCIONES_CENIT_F08_2026-11-02.zip` (`ACHInterbank_ajustado`)  
**Fecha:** 2026-11-02  
**Alcance:** SPA `/uat/nacha-inbound-simulator`, backend, generador NACHA-M, descarga, `NachaUpload`, reconciliación de la prenotificación, aceptación tácita, auditoría y pruebas.  
**Estado del documento:** plan propuesto, sin modificaciones al código ni pruebas runtime ejecutadas.

## 1. Objetivo y semántica normativa

Simular **el resultado de prenotificaciones previamente originadas por ACH Interbank** hacia bancos externos en ACH Colombia o CENIT. Cada resultado debe estar vinculado con una `AchTransaction` original identificable, su trazabilidad NACHA y, cuando exista, su `PrenotificationValidationAttempt` / `ThirdPartyAccountValidation`.

Se admiten dos rutas **diferentes**:

- **Devolución expresa:** el participante receptor devuelve la prenotificación; simular un **archivo NACHA-M de devolución** con causa aplicable, importe cero y vínculo inequívoco al trace original; después **descarga y carga manual en `NachaUpload`**. No crear aprobaciones ni rechazos ficticios con registros que carezcan de un formato homologado.
- **Sin devolución en plazo:** **no se genera archivo de aceptación positiva**. Se comprueba el vencimiento del plazo que corresponda y se ejercita de forma aislada el flujo de `AchTacitAcceptanceJobHandler` en UAT, con tiempo controlado; el simulador no modifica estados directamente. No tratar la espera previa al vencimiento como aprobación.

Base normativa en los archivos provistos: ACH Colombia V35, numeral 2.10.3.1, señala que sin devolución dentro de plazo la prenotificación se entiende exitosa; el Anexo 2 CENIT, numeral 4.8, exige devolución dentro de la misma fecha valor, hasta el último ciclo de devoluciones, y habilita al originador si vence sin devolución. El manual NACHA-M CENIT (2026-05-07) establece registros de longitud 106 y reglas de adenda, validación y formato.

**Fuera de alcance:** envíos externos, importación automática desde el generador, mutación directa de saldos, creación de nuevas prenotificaciones salientes y simulación de respuestas positivas NACHA-M sin formato oficial sustentado.

## 2. Línea base verificada en código

- Ya existe `IncomingPrenotificationResponse` en `NachaInboundSimulationType` y la opción «Respuesta de prenotificación» en Angular, pero el `PreviewAsync` la bloquea con `DIFFERENTIAL_GENERATOR_NOT_HOMOLOGATED` al no ser devolución implementada.
- El listado `ListEligibleDifferentialTransactionsAsync` recupera operaciones salientes, pero el frontend solo pide `state=Pending` sin filtrar explícitamente `IsPrenotification`. La interfaz combina cuadro de texto de referencias con una selección tabular y requiere selección tabular aunque se hayan escrito referencias; esos controles deben unificarse.
- `ResolveReferencesAsync` verifica cámara, banco y `IsPrenotification`, pero no revalida de modo completo estado `Pending`, respuesta previa, identidad del ciclo de origen frente al ciclo de respuesta y coincidencia con el intento canónico.
- El servicio ya usa `INachaFileBuilder.BuildReturnOutAsync` para devoluciones oficiales y posee `ValidateReturnReasonAsync`; el flujo de prenotificación devuelta debe reutilizarlos en vez de crear un constructor paralelo.
- Ya existe `AchTacitAcceptanceJobHandler` y `PrenotificationValidationAttempt.ResponseDeadlineAtUtc`; para pruebas determinísticas conviene inyectar `TimeProvider` en el job en vez de modificar el reloj del sistema.
- La descarga SPA con `window.open` puede fallar al no incorporar el JWT. Priorizar su corrección para probar el flujo completo.
- El simulador mantiene `generatedOnly=true`, `autoImported=false`, `uploadRequired=true`, `externalTransmission=false`, lo que debe conservarse para la ruta de devolución.

## 3. Incrementos propuestos

### I01 — Contrato y casos de uso

**Cambios**
1. Formalizar resultados: `Returned` (genera NACHA-M) y `NoReturnByDeadline` (sin archivo, prueba del job); dejar los otros valores `Approved/Confirmed/Failed` fuera de esta funcionalidad hasta disponer de contrato oficial verificable.
2. En `NachaInboundSimulationEnums.cs`, preferir una operación explícita `IncomingPrenotificationReturn` para el archivo de devolución; mantener compatibilidad de lectura para el valor legado `IncomingPrenotificationResponse` y etiquetarlo como no homologado hasta migrar UX/API. No reutilizar un código como `28` como supuesta aprobación.
3. Precisar campos de solicitud: `OriginalPrenotificationIds` (identificadores internos), `OriginalCycleId` contextual, `ResponseCycleId`, `BusinessDate`, `ClearingHouseCode`, `RespondingFinancialInstitutionId`, `ReasonCode` y `IdempotencyKey` cuando se genera archivo. Conservar campos antiguos únicamente como adaptación compatible y documentada.
4. Definir estados finales esperados con los valores reales del dominio, sin cadenas decorativas ni efectos sobre datos productivos.

**Criterio de cierre:** contrato público y ejemplos por cámara revisados; casos de uso sin interpretaciones ambiguas.

### I02 — Consulta y elegibilidad única en backend

**Cambios**
1. Agregar filtro `IsPrenotification=true`, cámara, banco receptor, fecha de emisión, ciclo original y estado al listado de elegibles; cuando exista registro canónico, usar `PrenotificationValidationAttempt` como referencia autoritativa del plazo y situación de respuesta.
2. Validar origen CFA/default → destinatario externo; origen realmente enviado y aún a la espera de respuesta; trace e identificadores completos; correspondencia de SEC/tipo de transacción; monto cero; banco que devuelve idéntico al destinatario de la prenotificación original.
3. Rechazar respuesta previa, intento ya resuelto, transacción finalizada, referencias repetidas/ambiguas, selección de cámaras mezcladas, causas no vigentes y más de `MaxEntriesPerSimulation` referencias **reales**.
4. **Separar ciclo original y ciclo de respuesta**. La devolución puede tramitarse en un ciclo posterior al de envío original; validar que el ciclo de respuesta sea uno permitido para la devolución por cámara y plazo, no imponer igualdad entre ambos ciclos.
5. Centralizar las mismas reglas en listado, preview y generate; revalidar al generar con control de concurrencia, y reservar idempotentemente una respuesta simulada para impedir doble generación accidental.

**Código relevante:** `NachaInboundSimulationService.cs`, `NachaInboundSimulationModels.cs`, `NachaInboundSimulatorController.cs`, consultas de `AchTransactions`, `AchResponses`, `PrenotificationValidationAttempts`.

**Criterio de cierre:** envío directo a API tampoco permite eludir ninguna validación de la SPA.

### I03 — Generación normativa de devolución de prenotificación

**Cambios**
1. Enrutar `IncomingPrenotificationReturn` por `BuildOfficialReturnFileAsync` → `INachaFileBuilder.BuildReturnOutAsync`; extender `IsReturnScenario` y el catálogo de causas sin inventar códigos.
2. Asegurar importe **0**, tipo/dirección de devolución acorde a la transacción original y perfil de cámara, adenda tipo **99** cuando corresponda al perfil oficial, código de devolución aplicable, trace original preservado y trace nuevo único.
3. Reutilizar el SEC y los campos originales de la transacción/lote; utilizar el perfil NACHA-M vigente de **cada** cámara y controles de longitud 106, totales, bloques y nombre de archivo.
4. Aplicar control de consecutivos atómico (eliminar `CountAsync + 1` como reserva de secuencia) y completar escritura de archivo, metadata y evidencia con recuperación ante fallas parciales.
5. Mantener descarga manual y evidencia SHA-256, sin activar autoimportación ni transmisión.

**Criterio de cierre:** fixtures de devolución de prenotificación crédito y débito validan contra los respectivos parsers/perfiles ACH Colombia y CENIT; no existe archivo de aprobación positiva inventado.

### I04 — Integración real con NachaUpload y resolución

**Cambios**
1. Comprobar que la ingesta clasifique la devolución como devolución (no como nueva prenotificación) y correlacione inequívocamente con `OriginalTraceNumber`, banco, cámara y datos de la operación original.
2. Confirmar y, si falta, incorporar el enlace entre `IncomingNachaFileIngestion`, `AchTransaction`, `AchResponses`, `PrenotificationValidationAttempt` y `ThirdPartyAccountValidation` usando el servicio de transición existente, de forma idempotente.
3. La devolución válida debe conservar causal/evidencia y dejar la cuenta en el estado real de negocio que corresponda (`Rejected`/`RequiresCorrection` u otro establecido por reglas); no sobreescribir resoluciones ya finales.
4. Mantener los controles actuales de autorización, parser, hash, deduplicación y validación de cámara/ciclo.

**Criterio de cierre:** descarga → upload manual → registro de devolución → estado del intento y validación coherentes → auditoría completa; reupload de archivo igual no duplica efectos.

### I05 — Vencimiento sin devolución (aceptación tácita)

**Cambios**
1. Presentar en SPA «Sin devolución — verificar vencimiento» como escenario **sin archivo NACHA-M**; mostrar regla, plazo calculado, ciclo y evidencia de política, evitando llamarlo «confirmación recibida».
2. Extraer/injectar `TimeProvider` o reloj de dominio en `AchTacitAcceptanceJobHandler` y sus pruebas para correr casos de tiempo controlado sin adelantar reloj global ni editar arbitrariamente campos de producción.
3. En entorno de prueba aislado, ejecutar el job existente con datos de UAT y validar su transición `AppliedTacitly` y la resolución canónica `TacitlyAccepted` cuando corresponda al flujo existente. Si el job solo cambia `AchTransaction`, añadir la sincronización faltante mediante el servicio de dominio, no mediante SQL suelto.
4. Comparar ACH Colombia (plazos/ciclos configurados) y CENIT (fin del día operacional); conservar configuración oficial mediante `ClearingHouseTransactionRules` y política vigente.
5. Bloquear la aceptación tácita cuando exista devolución válida, plazo no vencido, intento ya resuelto o política sin habilitación.

**Criterio de cierre:** antes del deadline no cambia estado; vencido y sin devolución el job aplica una sola transición; con devolución no aplica aceptación tácita. El simulador sigue sin cambiar estados directamente.

### I06 — SPA integrada y control de UX

**Cambios**
1. En `nacha-inbound-simulator.component.html/.ts` agregar sección visible **«Respuestas a prenotificaciones enviadas»** dentro de Respuestas diferenciales, no una pantalla duplicada.
2. Mostrar tabla paginada con referencia, trace, cámara, banco receptor, tipo crédito/débito, fecha/ciclo de origen, estado, plazo y motivo de ineligibilidad. Usar selección por filas y retirar/delimitar el textarea libre para evitar fuentes contradictorias.
3. Al elegir «Devolución», pedir causal oficial, fecha y ciclo de respuesta; ofrecer preview con los datos originales, generar, descargar y mostrar instrucciones para NachaUpload.
4. Al elegir «Sin devolución», mostrar deadline y resultado de una evaluación temporal controlada; **no mostrar botón de descarga** ni generar NACHA ficticio.
5. Limpiar selección al cambiar banco, cámara, fecha o escenario. Aplicar límite por cantidad de filas seleccionadas.
6. Corregir descarga autenticada con `HttpClient` + Blob, y mostrar 401/403/422 correctamente; mantener permisos específicos `TestTools.Access`, `NachaSimulatorGenerateDifferential`, `NachaSimulatorDownload`.
7. Etiquetar claramente «UAT/local», mecanismo efectivo, acción pendiente de carga manual y casos «no homologados».

**Criterio de cierre:** un usuario de pruebas puede seleccionar una prenotificación real del entorno, generar devolución válida y cargarla manualmente sin copiar trazas a mano; puede comprobar por separado el vencimiento sin devolución.

### I07 — Pruebas, regresión y despliegue controlado

**Pruebas mínimas**
- Unitarias: clasificación de escenario, causa por cámara, importe cero, original/new trace, adenda/perfil, estado, vencimiento y `TimeProvider`.
- API adversarial: operación no enviada, no pendiente, otro banco, otra cámara, retorno previo, duplicada, trace ausente, ciclo original/resp. incompatibles, exceso de filas, causal inválida, concurrencia.
- Integración: ACH Colombia y CENIT para prenotificación crédito y débito → devolución NACHA oficial → descarga → `NachaUpload` → correlación → estado en `AchTransaction` y validación de tercero, sin cambios colaterales.
- Sin devolución: antes/después del deadline, días festivos/ciclos, devolución concurrente, doble ejecución del job.
- Seguridad: 401 sin JWT, 403 sin permisos, feature flag apagado, no transmission, no autoimport, limpieza de datos de prueba.
- E2E Angular/Playwright: corregir selectores Material obsoletos, estados del listado, filtros, carga de causa, preview, descarga, errores, ruta sin archivo.
- Resiliencia: dos generaciones simultáneas, conflicto de secuencia, falla en archivo/BD/evidencia y limpieza/compensación.

**Puerta de salida:** ningún flujo sin homologar aparece operativo, todas las pruebas anteriores con evidencia real, cero errores de severidad alta en el trayecto de prenotificación, y revisión sin regresiones a entradas/devoluciones existentes.

## 4. Dependencias, prioridades y restricciones

1. **Bloqueantes**: descarga autenticada (SPA-SIM-01), correlación/elegibilidad/revalidación (SPA-SIM-04, 05, 06), secuencias y evidencia (SPA-SIM-07, 08). No cerrar la funcionalidad sin esas correcciones focalizadas.
2. **Contraste normativo**: reutilizar perfiles publicados vigentes y catálogo de causales para cada cámara. La corrección de descripción de lote CENIT (SPA-SIM-02) es necesaria en los flujos afectados; el generador de devoluciones oficial debe ser la fuente preferente.
3. **Feature flags**: mantener deshabilitada la nueva ruta hasta que fixtures y E2E estén aceptados. Habilitar por separado devolución y prueba de aceptación tácita si se requiere entrega gradual.
4. **Compatibilidad**: no romper la enumeración o históricos `IncomingPrenotificationResponse` sin migración controlada de DTO/SPA.
5. **Sin sobreingeniería**: extender controladores, servicios y tablas existentes; migración nueva solo si resulta imprescindible para reservar idempotencia/identidad de archivo de forma transaccional o almacenar auditoría ausente.

## 5. Criterios funcionales de aceptación

| ID | Caso | Resultado verificable |
|---|---|---|
| PRE-01 | Seleccionar prenotificación pendiente originada por ACH Interbank | Aparece con traza, banco, cámara y plazo correctos |
| PRE-02 | Intentar responder una no enviada o finalizada | Bloqueada también en API |
| PRE-03 | Devolución ACH Colombia con causal aplicable | Archivo oficial NACHA-M válido, monto 0, correlación original |
| PRE-04 | Devolución CENIT con causal aplicable | Archivo válido para perfil CENIT y ciclo permitido |
| PRE-05 | Descargar y cargar por NachaUpload | Resultado y validación de tercero actualizados exactamente una vez |
| PRE-06 | Reenviar idéntico archivo | Sin actualización duplicada |
| PRE-07 | Sin devolución antes del plazo | Permanece pendiente |
| PRE-08 | Sin devolución después del plazo | Job de aceptación tácita resuelve conforme a política |
| PRE-09 | Con devolución antes del vencimiento | No puede pasar a aceptación tácita |
| PRE-10 | Cámara/banco/ciclo incompatibles | Error funcional controlado |
| PRE-11 | Usuario sin permisos | 401/403 según corresponda |
| PRE-12 | Dos generaciones paralelas | Sin colisiones ni evidencia inconsistente |
| PRE-13 | Variar cámara/escenario en la SPA | Selecciones previas limpiadas |
| PRE-14 | Funcionalidad deshabilitada | Ningún efecto ni archivo generado |

## 6. Entregables de cada incremento

Por incremento: código cambiado, diff de archivos, pruebas nuevas/actualizadas, resultados de ejecución, evidencia NACHA de prueba (solo datos sintéticos o autorizados), registro de hallazgos resueltos/no resueltos y decisión de continuar. No alterar las reglas de ACH Colombia ni CENIT fuera del alcance de esta funcionalidad.

## 7. Orden de implementación recomendado

**Primero I01 e I02** para fijar semántica e integridad; **después I03 e I04** para cerrar devolución end-to-end; **I05** para la aceptación tácita con clock controlado; **I06** para presentar ambos flujos correctamente; finalmente **I07** para certificación y activación gradual. En cada incremento ejecutar regresión focalizada. No declarar homologación normativa sin fixture y pruebas de integración reales.
