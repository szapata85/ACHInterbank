# INFORME — I02-CENIT — detección pasiva de condiciones de devolución

**Base usada:** `ACHInterbank_COMPLETO_DEVOLUCIONES_AUTOMATICAS_I01_CENIT_2026-11-02.zip`. **Resultado:** `ACHInterbank_COMPLETO_DEVOLUCIONES_AUTOMATICAS_I02_CENIT_2026-11-02.zip`. Incremento independiente CENIT, **NO-GO productivo**.

## Alcance implementado

Se incorporó `CenitPassiveReturnDetector` como servicio de **lectura**, registrado en DI y expuesto por `CenitPassiveReturnDetectionController` (`GET /api/cenit/devoluciones-automaticas/deteccion`). Reutiliza el control de configuración exclusiva I01, las ejecuciones ya persistidas `IncomingNachaIntegrationExecution`, cola `IncomingNachaDispatchQueue`, correlación `IncomingNachaTransactionLink`, transacciones de ciclo CENIT y catálogo específico `AchReturnCode` contrastado con `CenitIncomingReturnPolicy.CauseDefinitions`.

La evidencia debe estar: `Live`, `Proc_Transacciones`, transporte confirmado, rechazo funcional irreversible, resultado del núcleo completado, catálogo de respuesta resuelto, enlace final y exacto, cola `FailedFinal/IFUNC`, clasificación funcional elegible de entrada, cámara CENIT en ejecución/cola/ciclo, transacción entrante clasificada y no devuelta, sin reintento pendiente ni revisión manual emitida por el núcleo. Si falta una condición no presenta indicio. Se busca el **último intento por transacción** antes de clasificar. Se descartan transacciones con devoluciones ya generadas. La consulta se acota a 500 ejecuciones y hasta 100 transacciones.

Solo para causas con coincidencia de código `Rxx` en el catálogo CENIT **activo, aplicable, vigente y del flujo correcto**, cotejadas con causales CENIT para crédito/débito, se presenta `INDICIO_CAUSAL_CENIT`. Códigos no verificables producen `REVISION_CAUSAL`, R06/R23 aplicables o R34 `REVISION_SOLICITUD`, y prenotificaciones `REVISION_PRENOTIFICACION`. Ningún estado autoriza devoluciones. Datos de cuentas, XML SOAP o información privada no se copian a la API.

**No se modificaron:** generador, batch, calendario, servicios manuales CENIT, lógica ACH Colombia, pipeline de integración, SPA, migraciones, transporte o auditoría central. Se añadió únicamente el registro DI en un archivo compartido; la lectura no guarda registros.

## Normativa contrastada (fuentes adjuntas, no inferencias ACH)

- DSP-152 Anexo 2 de 27/02/2025, capítulos 1, secciones 3 y 4: cinco ciclos con clases de transacciones diferenciadas y plazos propios; ejecución permitida se evaluará en I03.
- Anexo A de causales (28/11/2023): catálogos PPD/CCD/CTX, R06 solicitud del Originador con condiciones, R23 solicitud del Receptor dentro de plazos, R34 cancelación manual, R60–R69 devolución de devolución.
- Anexo B: D01–D06 son **rechazos de archivo STA**, no causales de devolución de fondos.
- Manual NACHA-M CENIT de 07/05/2026: campos, adendas y registros, reservado a I03/I05; I02 no construye archivos.
- Manual de Usuario CENIT: operación por sesiones y responsabilidades del participante; sin activar procesos por modo de configuración.

Se mantienen pendientes la vigencia documental cruzada y las reglas que requieren evidencias bancarias no presentes en fuente.

## Pruebas y criterios de aceptación

El archivo `CenitPassiveReturnDetectionI02Tests.cs` incluye escenarios xUnit: caso confirmado, estado inicial desactivado, modos CON_APROBACION/AUTOMATICO sin ejecución, aislamiento ACH, causas Dxx/no catalogadas/no aplicables, R06/R23/R34, prenotificación, transacción ya devuelta, fallo técnico/simulador, enlace no final, respuesta posterior exitosa, permisos y límites. **No ejecutadas en .NET**. Validador de fuente 50/50; no interpretar como pruebas bancarias.

## Riesgos

La lectura es acotada y no persistente, no propone fecha/ciclo de salida, no asegura notificación al Originador, ni garantiza idempotencia frente a concurrencia futura. La categoría 'indicio' **no** significa elegibilidad normativa; se exige revalidación I03/I04 y homologación CENIT. Se preserva el NO-GO de ambas cámaras.

## Avance total

| Cámara | Incremento(s) | Estado al cierre de I02-CENIT | Pendientes del plan actual |
|---|---|---|---|
| CENIT | **I01** | Incorporado al código; ejecución/homologación pendientes | — |
| CENIT | **I02** | Incorporado al código; validador estático 50/50; xUnit/DB pendientes | — |
| CENIT | **I03–I06** | No iniciados | **4 incrementos** |
| ACH Colombia | **I01–I08** | Incorporados al código histórico; no homologados | — |
| ACH Colombia | **I09-B** | Ejecución en entorno compilado pendiente | **1 fase obligatoria de validación** |
| ACH Colombia | **I09-A** | Preparación técnica condicional | **0–1 fase opcional** |

**Conteo:** 6 incrementos CENIT planificados; **2 incorporados**, **4 por implementar**. Para las dos cámaras restan **4 incrementos CENIT + 1 fase obligatoria I09-B ACH**, más **I09-A solo si el entorno lo necesita**. Esto es el conteo del plan actual, **no** una garantía de puesta en producción ni un número cerrado de iteraciones de corrección.


**Parada:** I02-CENIT. **Siguiente propuesto:** I03-CENIT, preparación controlada sin generación ni envío automático.
