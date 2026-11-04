# I09 — Auditoría estática transversal ACH Colombia: causales, ROR, NACHA-M y operaciones fuera de ciclo

**Corte:** 2026-11-01. **Estado:** I09 TERMINADO como revisión estática (NO es certificación ni aceptación de ACH Colombia). Código original preservado sin modificaciones. Base: ZIP I08.

## Alcance y fuentes

1. *Manual Servicio ACH Transferencias Interbancarias V35*, abril 2026: Anexo 3 (D01–D32, causales devolución por operador), Anexo 9 (23 códigos Rxx), capítulos sobre transacciones, devoluciones, archivos y conciliación.
2. *Nuevos Ciclos de Proceso y Horarios* (siete ciclos): cortes y ventanas. Se conserva discrepancia documental conocida en C4 y vigencia declarada en código desde 2026-10-14, sin presuponer despliegue externo.
3. Código fuente del ZIP I08, rutas `src/` y `tests/` auditadas sin necesidad de ejecutar .NET, BD, SFTP o una cámara.

**Regla de lectura:** `TRAZADA` = existe control explícito identificable, **no** equivale a test integrado aprobado. `PARCIAL` = existe solución parcial pero falta enforcement en la ruta completa o evidencia suficiente. `NO_DEMOSTRADA` = evidencia insuficiente para afirmarla. `INCUMPLIMIENTO_ESTATICO` = contradicción lógica verificable en el código contra la referencia documental; no cuantifica transacciones afectadas en producción.

## Matriz compacta regla por regla

| ID | Subproceso | Requisito | Evidencia `ruta:línea` | Estado | Dictamen limitado |
|---|---|---|---|---|---|
| T01 | Devoluciones | Selección de transacciones y petición no vacía | `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchReturnOriginalCorrelationComponent.cs:57-71` | **TRAZADA** | Se rechazan selecciones vacías o repetidas. |
| T02 | Devoluciones | Prohibición de mezcla de cámaras de origen/destino | `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchReturnOriginalCorrelationComponent.cs:73-96` | **TRAZADA** | La cámara de ciclo de devolución debe coincidir con la original. |
| T03 | Devoluciones | Exclusión de doble generación y bloqueo concurrente | `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchReturnsService.cs:87-98` | **TRAZADA** | Lock e historial de retornos; confirmar en BD concurrente durante UAT. |
| T04 | Devoluciones | Validación por causal Rxx, clase, fecha y estado | `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchReturnEligibilityService.cs:49-94` | **PARCIAL** | Consulta catálogo y política, pero no prueba todas las combinaciones de las 23 causales en ambiente. |
| T05 | Devoluciones | Ventana exacta día/ciclo original→ciclo de devolución | `src/Cfa.ACHInterbank.Application/ACH/Models/RegulatoryCycleScheduleCatalog.cs:178-185` | **PARCIAL** | H01: matriz 7C declarada sin uso demostrable en AchReturnsService; no asumir enforcement por AllowsReturn. |
| T06 | Devoluciones | Orden de oportunidad R10 cuando C1 cruza medianoche | `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchReturnEligibilityPolicyComponent.cs:148-173` | **INCUMPLIMIENTO_ESTATICO** | H07: OrderBy(StartTime) coloca C1 tras C2–C7; falla orden operacional para C1→C2–C5 elegibles. |
| T07 | Devoluciones | Base R10: evidencia de prenotificación/autorización | `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchReturnEligibilityPolicyComponent.cs:176-210` | **PARCIAL** | Existe consulta de evidencia; no demostrar exhaustivamente todas las fuentes/escenarios de negocio. |
| T08 | Devoluciones | DEV14 separado de flujo ReturnOut | `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchReturnEligibilityPolicyComponent.cs:16-24` | **TRAZADA** | DEV14 se impide en ReturnOut y remite a reclamaciones. |
| T09 | Devoluciones | Valor original completo; prenotificación valor cero | `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchReturnsService.cs:93-116` | **TRAZADA** | El importe se toma de transacción original; prenote en cero. Validación financiera posterior separada. |
| T10 | Devoluciones | Devolución de ciclos del día hábil anterior | `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchReturnOriginalCorrelationComponent.cs:117-160` | **PARCIAL** | Excepción R10 en mezcla de ciclo; falta corroborar todas las ventanas ordinarias interdiarias y festivos. |
| O01 | Devolución de devolución | ROR exige origen que sea devolución | `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchReturnOfReturnEligibilityService.cs:18-26` | **TRAZADA** | Se descarta origen que no sea Return. |
| O02 | Devolución de devolución | Causal origen, nueva causal y política por cámara | `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchReturnOfReturnEligibilityService.cs:45-111` | **PARCIAL** | La política valida códigos; homología normativa ACH Colombia ROR no demostrada en toda su casuística. |
| O03 | Devolución de devolución | Registro de flujo exige estados elegibles | `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/ReturnOfReturnOrchestrator.cs:44-56` | **TRAZADA** | Fuente Return y estados delimitados. |
| O04 | Devolución de devolución | No emitir dos archivos NACHA productivos para mismo conjunto | `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchReturnOfReturnFileGenerationService.cs:218-230` | **TRAZADA** | Detección por auditorías de flujos; prueba de concurrencia DB pendiente. |
| O05 | Devolución de devolución | Política ciclo ROR en generación | `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchReturnOfReturnFileGenerationService.cs:262-313` | **PARCIAL** | Comprobación condicionada a _cycleTransactionPolicy != null; validar inyección efectiva y ausencia de bypass. |
| O06 | Devolución de devolución | Separación artefacto de auditoría / NACHA-M | `src/Cfa.ACHInterbank.Api/Controllers/AchReturnOfReturnController.cs:97-165` | **TRAZADA** | Endpoints distintos: GenerateAsync produce texto ROR|; GenerateNachaAsync salida productiva. |
| O07 | Devolución de devolución | Validación completa del archivo ROR NACHA-M | `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchReturnOfReturnFileGenerationService.cs:343-388` | **PARCIAL** | Se construyen registros y totales. Método ValidateNachaRecords (584) no aparece invocado dentro de este archivo; comprobar controles alternos del builder. |
| O08 | Devolución de devolución | Aprobación normativa de ROR ACH Colombia y combinación exacta causal/ciclo | `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchReturnOfReturnEligibilityService.cs:97-111` | **NO_DEMOSTRADA** | Existe catálogo de políticas, pero este anexo de ACH Colombia no acredita por sí solo la totalidad de escenarios ROR. |
| N01 | NACHA-M | Longitud 106 y clase de registros 1,5,6,7,8,9 | `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaInboundValidation.cs:330-352` | **PARCIAL** | Se revisaron controles de lotes/bloques; conformidad por cada layout debe probarse con fixtures oficiales. |
| N02 | NACHA-M | Controles totales/contadores para lote y archivo | `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaControlTotalsCalculator.cs:1-188` | **TRAZADA** | Calculadora específica; no garantiza igualdad con planilla externa. |
| N03 | NACHA-M | Construcción y partición por perfil y ciclo | `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaFileBuilder.cs:72-98` | **TRAZADA** | Delegación a coordinador por ciclo, retorno y archivo. |
| N04 | NACHA-M | Adenda devolución y datos de trazabilidad | `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchReturnOfReturnFileGenerationService.cs:325-355` | **PARCIAL** | Tipo 7 generado con causal/trace; validar contrato ACH y casos límite en UAT. |
| N05 | NACHA-M | Fecha efectiva, nombre y encabezado compatibles | `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/IncomingNachaAdmissionValidator.cs:59-96` | **TRAZADA** | Compara fechas extraídas y retorna códigos de rechazo cuando difieren. |
| N06 | NACHA-M | Código D01/D02/D04 coherente con Anexo 3 ACH | `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaInboundValidation.cs:330-352` | **INCUMPLIMIENTO_ESTATICO** | H09: D02 se emplea para múltiplos de diez, D04 para totales de lote y D01 en línea 906 para secuencia; V35 Anexo 3 asigna significados distintos. |
| J01 | Rechazos/causales | Cobertura de 23 causales Rxx del Anexo 9 | `src/Cfa.ACHInterbank.Persistence/Configuration/ReturnReasonConfiguration.cs:23-63` | **TRAZADA** | Comparación de códigos literal: 23/23 enumerados; aplicación correcta por clase/ciclo no certificada. |
| J02 | Rechazos/causales | Cobertura D01–D32 del Anexo 3 | `src/Cfa.ACHInterbank.Persistence/Configuration/ReturnReasonConfiguration.cs:64-95` | **TRAZADA** | 32/32 enumerados; se mantiene discrepancia H09 de asignación en parser. |
| J03 | Rechazos/causales | Separación Dxx por flujo y cámara | `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchCauseCodePolicy.cs:22-46` | **PARCIAL** | Clasifica Dxx como FileRejection de modo genérico, sin distinguir el sentido específico de devolución por operador ACH Colombia; H09. |
| J04 | Rechazos/causales | Clasificación de devolución, rechazo operador y DEV14 entrante | `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/IncomingNachaFunctionalClassifier.cs:47-83` | **TRAZADA** | Clasifica retorno según causal/adenda; requiere vinculación y procesamiento posterior. |
| J05 | Rechazos/causales | Acuse externo e impacto final de un Dxx | `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/IncomingNachaFunctionalClassifier.cs:76-83` | **NO_DEMOSTRADA** | Clasificar un Dxx no demuestra aceptación en cámara ni aplicación financiera. |
| M01 | Fuera de ciclo/MFT | Envío automático limitado a ventana de transmisión | `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchColombiaManagedMftOutboundWorkflow.cs:25-50` | **TRAZADA** | La guarda opera exclusivamente en origen Automatic. |
| M02 | Fuera de ciclo/MFT | Salida manual no vetada globalmente por hora | `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchColombiaManagedMftOutboundWorkflow.cs:33-50` | **TRAZADA** | Hay ruta Manual independiente; se conserva habilitación por configuración. |
| M03 | Fuera de ciclo/MFT | Habilitación/autorización y auditoría envío manual | `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchColombiaManagedMftImplementationSupport.cs:63-73` | **PARCIAL** | ManualOutboundAllowed existe; revisar RBAC y autorización efectiva en controlador y ambiente. |
| M04 | Fuera de ciclo/MFT | Recepción tardía conservada con observación | `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchColombiaManagedMftInboundWorkflow.cs:404-451` | **TRAZADA** | InboundOutsideExpectedWindow es evento, no rechazo global. |
| M05 | Fuera de ciclo/MFT | Reintento saliente solo en estados y límites establecidos | `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchColombiaManagedMftRetryReprocessComponent.cs:16-54` | **TRAZADA** | Requiere contenido retenido, estado y máximo de reintentos. |
| M06 | Fuera de ciclo/MFT | Reproceso entrante restringido a estados elegibles | `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchColombiaManagedMftRetryReprocessComponent.cs:57-82` | **TRAZADA** | Solo Inbound con ingesta y estado Rejected o Failed. |
| M07 | Fuera de ciclo/MFT | Recepción ACHCOL sin ciclo interno, por archivo físico | `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/IncomingNachaAdmissionValidator.cs:49-69` | **TRAZADA** | Condicionada a flag explícito, cámara y correlación determinística; no equivale a autorización SOAP. |
| M08 | Fuera de ciclo/MFT | Despacho de recepción sin ciclo debe ser clasificado, no ejecutado ciegamente | `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/IncomingNachaDispatchEligibilityPolicy.cs:115-121` | **TRAZADA** | FileBased devuelve evaluación distinta y no autoriza SOAP por sí misma. |
| M09 | Fuera de ciclo/MFT | Confirmación aceptación ACH del archivo transmitido fuera de ventana | `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchColombiaManagedMftOutboundWorkflow.cs:280-309` | **NO_DEMOSTRADA** | La respuesta del adaptador/transmisión no prueba compensación ni aceptación normativa externa. |
| F01 | Compensación y cierre | Horarios planilla, pago y liberación de los siete ciclos | `src/Cfa.ACHInterbank.Application/ACH/Models/RegulatoryCycleScheduleCatalog.cs:187-237` | **TRAZADA** | Parámetros definidos; no suple comprobante externo de liquidación. |
| F02 | Compensación y cierre | Cuadre planilla–pago–banco–ledger y respuesta final | `src/Cfa.ACHInterbank.Persistence/ACH/Quartz/Jobs/Implementation/AchColombiaManagedMftHandlers.cs:253-315` | **NO_DEMOSTRADA** | Presencia de handlers no acredita conciliación financiera con estados externos verificables; H03. |
| F03 | Compensación y cierre | Reglas de producto PSE/SSS/DIAN por ciclo | `src/Cfa.ACHInterbank.Application/ACH/Services/CycleTransactionPolicy.cs:56-71` | **PARCIAL** | La política distingue clases funcionales, no subproductos; H08 pendiente de enforcement completo. |

**Control de alcance:** 41 reglas transversales revisadas: 23 trazadas, 12 parciales, 4 no demostradas, 2 incumplimientos lógicos estáticos. No son porcentajes de certificación.

## Cruce literal de causales

**Anexo 9 ACH V35:** 23 Rxx: R01, R02, R03, R04, R06, R07, R08, R09, R10, R12, R13, R14, R15, R16, R17, R20, R23, R29, R30, R31, R32, R33, R35. Los 23 aparecen en `ReturnReasonConfiguration.cs`. Esto verifica solo la *presencia* del catálogo, **no** que cada causal se valide conforme a las cuatro clases (débito/crédito × prenotificación/monetaria) ni al momento de retorno.

**Anexo 3 ACH V35:** D01 a D32 (32 códigos). Los 32 aparecen en `ReturnReasonConfiguration.cs`, pero `AchCauseCodePolicy.cs:35-46` los categoriza globalmente como `FileRejection`. En ACH V35, el Anexo 3 los denomina *devolución por operador*, mientras en CENIT los Dxx del Anexo B son de rechazo STA. **No se deben trasladar semánticas entre cámaras.**

**H09 — nueva discrepancia estática alta:**

| Código | ACH V35, Anexo 3 | Código observado | Ruta |
|---|---|---|---|
| D01 | Fecha efectiva menor que fecha de proceso | Secuencia duplicada por fecha | `NachaInboundValidation.cs:899-907` |
| D02 | Fecha efectiva no válida | Registros del archivo no múltiplos de 10 | `NachaInboundValidation.cs:344-347` |
| D04 | Prenotificación con valor diferente de cero | Descuadre del número de lotes | `NachaInboundValidation.cs:333-352` |

La diferencia de etiquetas/motivos está verificada, pero **no se probó** si ese resultado llega a ACH Colombia, qué perfil selecciona el parser ni qué archivos operativos quedan afectados. No se debe corregir por sustitución global de códigos. Verificar primero **cámara + flujo + causal + fase** y preservar codificación CENIT/STA independiente.

## ROR: separación crítica

La ruta `AchReturnOfReturnController.cs:97-165` expone dos funciones distintas: artefacto de auditoría (`GenerateAsync`, contenido `ROR|...`) y archivo NACHA-M (`GenerateNachaAsync`). **No confundir archivo informativo con NACHA productivo.** `GenerateNachaAsync` valida no duplicación por conjunto de flujos (`AchReturnOfReturnFileGenerationService.cs:218-230`), y evalúa clase por ciclo cuando el servicio de política está presente (`262-313`). La política es opcional en el constructor, y el método local `ValidateNachaRecords` solo aparece declarado en `584-591`, sin invocación dentro del archivo: clasificar **PARCIAL** hasta verificar DI y controles equivalentes de la ruta productiva.

## NACHA-M fuera de ciclos — política mantenida D-I08-01

- **Generar/preparar** NACHA-M no equivale a **transmitir** ni a **compensar**.
- **Automático saliente:** aplica una guarda de ventana (`AchColombiaManagedMftOutboundWorkflow.cs:49-50,311-329`).
- **Manual saliente:** está sujeto a la habilitación específica de origen manual, sin la misma guarda automática. La operación manual no autoriza una transacción de tipo prohibido para el ciclo.
- **Entrante tardío:** conservar archivo, correlación y evento `InboundOutsideExpectedWindow`; una marca temporal tardía no equivale a causal de rechazo.
- **Ingesta sin ciclo interno ACHCOL:** admitida solo por el camino de archivo físico habilitado explícitamente, con nombre/encabezado y cámara comprobados (`IncomingNachaAdmissionValidator.cs:49-69,124-179`); aceptación de ingesta no es autorización de compensación ni SOAP.
- **Reintento/reproceso:** observar estado, trazabilidad, contenido retenido, idempotencia, permisos y ciclo operativo; no inventar liquidación fuera de sesión.

## Acciones prioritarias para dictamen I10 (no cambios de código en I09)

1. **H09 ALTA:** cerrar correspondencia por cámara de D01/D02/D04 y todos los Dxx, distinguiendo rechazo fatal de archivo, devolución por operador, devolución ordinaria y STA; exigir prueba parametrizada por código/rail. Corrección mínima acotada si se confirma uso productivo del parser.
2. **H01 ALTA:** verificar enforcement real de ventanas `ReturnWindows` por ciclo original, día operativo y ciclo de devolución. Asegurar que R10 no sea excepción indiscriminada.
3. **H07 ALTA:** resolver orden lógico del C1 nocturno antes de computar oportunidad R10. Probar C1→C2/C3/C4/C5 y negaciones C1→C6/C7.
4. **H04 ALTA/PARCIAL:** trazar inyección y gobernanza de ROR ACH, validación NACHA completa, política de fecha/ciclo y unicidad concurrente; no confundir endpoint de auditoría con archivo bancario.
5. **H03/H08:** conciliación real y enforcement producto×ciclo; documentar lo no demostrable sin ambiente.
6. **D-I08-01:** mantener tratamiento diferenciando manual/automático/recepción tardía; no introducir bloqueo global NACHA por hora.

## Verificaciones efectivamente ejecutadas

- Comparación estática de códigos del manual contra `ReturnReasonConfiguration.cs`: **23/23 Rxx** y **32/32 Dxx** presentes. No cubre reglas de aplicabilidad ni semántica completa.
- Revisiones de rutas `src/` y `tests/`, referencias `ruta:línea` verificadas contra ZIP base.
- Integridad de paquete, SHA256 de herencia y unicidad al generar I09.
- **NO_EJECUTADO:** `dotnet test`, base de datos, Quartz, integración SFTP/MFT, planillas bancarias, compensación real, conciliación, homologación y respuesta externa. No bloquea auditoría estática ni equivale a aprobado.

**Siguiente I10:** consolidar hallazgos únicos H01–H09; dictamen estático con evidencias, prioridades y pendientes de homologación; mantener I01–I09 en ZIP completo. **0 ciclos certificados.**
