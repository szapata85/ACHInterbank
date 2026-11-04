# I08 — ACH Colombia: auditoría estática del ciclo 7 y archivos NACHA-M fuera de ciclos

**Corte:** 2026-11-01 · **Estado:** I08 TERMINADO como auditoría estática; C7 **NO certificado**. Hereda el ZIP I07; código productivo intacto. Se incorpora expresamente la observación del usuario: **ciertas operaciones con archivos NACHA-M pueden ejecutarse fuera de la ventana de un ciclo**. No se presume que generar/recibir/transmitir un archivo equivalga a compensarlo en esa misma franja.

## 1. Norma utilizada sin mezclar cámaras

**Fuente documental:** *Nuevos Ciclos de Proceso y Horarios.pdf*, página 3, fila C7 (ACH Colombia; información privada). Envío **16:01–18:00**, cierre **18:00**, planilla **18:00–18:30**, pago **18:30–18:45**, liberación **18:45–19:00** (hora Bogotá). Devoluciones monetarias débito/crédito y prenotificaciones originales de **C3/C4/C5/C6 del mismo día operacional n**. Se enumeran **crédito**, **prenotificación débito/crédito** y **PSE/SSS/DIAN**. **No se enumera débito monetario ordinario nuevo.** Las devoluciones de débitos son una clase distinta de transacción.

La vigencia `ACHCOL-7C-20261014` consta en el **código**; no se verifica que la cámara, el despliegue productivo ni el canal externo lo hayan confirmado. El Manual V35 de abril 2026 incluye la programación previa de 5 ciclos; se conserva H02 sin fingir reconciliación normativa.

## 2. Regla por regla — 23 controles

`TRAZADA` = código/test localizado; `PARCIAL` = lógica parcial o control con evidencia incompleta; `NO_DEMOSTRADA` = no se acreditó enforcement en el trayecto revisado. **No** son resultados de certificación. Los paths cortos remiten a `src/.../ACH/...` y `tests/Cfa.ACHInterbank.Tests`; consultar el mapa de rutas de I07 para prefijos comunes.

| ID | Regla esperada C7 | Evidencia estática `archivo:línea` | Resultado | Comentario |
|---|---|---|---|---|
| AC-C7-01 | Política ACHCOL siete ciclos, vigencia declarada desde 14-oct-2026 | `RegulatoryCycleScheduleCatalog.cs:152-156,254-267; ClearingHouseCyclePolicyResolver.cs:35-66` | **TRAZADA** | Versionado en código; no despliegue externo verificado. |
| AC-C7-02 | Ventana de envío C7 16:01–18:00 y corte a 18:00 | `RegulatoryCycleScheduleCatalog.cs:230-237,295-315; RegulatoryCycleScheduleCatalogTests.cs:27-55` | **TRAZADA** | Catálogo y aserciones existentes; sin Quartz ejecutado. |
| AC-C7-03 | Planilla 18:00–18:30 | `RegulatoryCycleScheduleCatalog.cs:230-237,310-315; ClearingHouseCyclePolicyResolver.cs:150-165` | **TRAZADA** | Franja de etapa definida y orden validado. |
| AC-C7-04 | Pago compensación 18:30–18:45 | `RegulatoryCycleScheduleCatalog.cs:230-237; ClearingHouseCyclePolicyResolver.cs:150-165` | **PARCIAL** | Falta evidencia de pago real H03. |
| AC-C7-05 | Liberación 18:45–19:00 | `RegulatoryCycleScheduleCatalog.cs:230-237; AchColombiaManagedMftHandlers.cs:277-312` | **PARCIAL** | Polling automático en ventana, no prueba barrera de pago/acuses H03. |
| AC-C7-06 | Bloquear débito monetario ordinario nuevo C7 | `RegulatoryCycleScheduleCatalog.cs:230-237; CycleTransactionPolicy.cs:56-71; NachaTransactionValidationService.cs:52-72` | **TRAZADA** | La clase MonetaryDebit se niega; distinguir devolución débito. |
| AC-C7-07 | Permitir crédito monetario ordinario C7 | `RegulatoryCycleScheduleCatalogTests.cs:103-108; CycleTransactionPolicy.cs:56-71` | **TRAZADA** | Permiso de clase; no recepción bancaria verificada. |
| AC-C7-08 | Permitir prenotificaciones de crédito/débito C7 | `RegulatoryCycleScheduleCatalogTests.cs:103-108; CycleTransactionPolicy.cs:77-105` | **TRAZADA** | Conservar código NACHA y adenda; validación de campos I09. |
| AC-C7-09 | Distinguir devolución débito de nuevo débito monetario | `CycleTransactionPolicy.cs:57-64,77-90; RegulatoryCycleScheduleCatalog.cs:230-237` | **TRAZADA** | Return es clase funcional independiente; no constituye autorización causal ni de origen. |
| AC-C7-10 | Devoluciones ordinarias elegibles de C3/C4/C5/C6 actual | `RegulatoryCycleScheduleCatalog.cs:178-185; RegulatoryCycleScheduleCatalogTests.cs:110-131; AchReturnsService.cs:79-116` | **NO_DEMOSTRADA** | H01: ventana definida y testeada en catálogo; enforcement de fuente en ruta general no demostrado. |
| AC-C7-11 | Excluir devolución de operación original C2 actual | `RegulatoryCycleScheduleCatalogTests.cs:133-150; CycleTransactionPolicy.cs:56-71` | **NO_DEMOSTRADA** | Negativa explícita de test catálogo C7/C2, pero la política genérica solo ve AllowsReturn H01. |
| AC-C7-12 | Excluir P1–P7, C1, C7 como orígenes de devolución ordinaria C7 | `RegulatoryCycleScheduleCatalog.cs:178-185; AchReturnsService.cs:93-116` | **NO_DEMOSTRADA** | No afirmar aceptación por omisión; falta control vinculante en ruta general H01. |
| AC-C7-13 | Validar día y ciclo operativo originales (no solo hora civil) | `RegulatoryCycleScheduleCatalog.cs:90-106; AchReturnOriginalCorrelationComponent.cs:73-96,117-156` | **NO_DEMOSTRADA** | Correlación existente no prueba que se pase SourceDay/CycleNumber a ventana C7 H01. |
| AC-C7-14 | Validar R10 adicionalmente a la ventana ordinaria | `AchReturnEligibilityPolicyComponent.cs:123-175,213-235` | **PARCIAL** | Ordenamiento StartTime de R10 H07 abierto; C1→C7 está fuera de la ventana ordinaria, no afirmar defecto nuevo C7. |
| AC-C7-15 | Correlación, unicidad y no duplicar devoluciones | `AchReturnOriginalCorrelationComponent.cs:73-96,117-156; AchReturnsService.cs:87-98,128-202` | **PARCIAL** | Controles separados de elegibilidad por ventana. |
| AC-C7-16 | Causales, valor original y restricciones sobre devoluciones | `AchReturnEligibilityPolicyComponent.cs:16-80; AchReturnsService.cs:93-116` | **PARCIAL** | Causales completas y ROR quedan en I09. |
| AC-C7-17 | Autorizar PSE/SSS/DIAN en C7 según producto | `CompanyEntryDescriptionCatalogConfiguration.cs:73-78; CycleTransactionPolicy.cs:56-71` | **NO_DEMOSTRADA** | H08: descripciones existen; enforcement producto×ciclo no probado. |
| AC-C7-18 | Envío automático NACHA-M solo en ventana de remisión C7 | `AchColombiaManagedMftOutboundWorkflow.cs:25-50,311-329; AchColombiaManagedMftHandlers.cs:25-65` | **TRAZADA** | Control específico solo para origen Automatic; no convertirlo en veto global a archivos. |
| AC-C7-19 | Ejecución MFT manual también fuera de ventana, sujeta a habilitación | `AchColombiaManagedMftImplementationSupport.cs:63-73; AchColombiaManagedMftOutboundWorkflow.cs:25-50,70-96; AchColombiaManagedFileExchangeTests.cs:161-181` | **PARCIAL** | ManualOutboundAllowed y ruta sin guarda automática comprobados en código; aceptación externa, roles y autorización de excepción por certificar. |
| AC-C7-20 | Generación NACHA-M separada de transmisión automática | `NachaSecurityOperationService.cs:240-274; NachaFileGenerationCoordinator.cs:374` | **PARCIAL** | Generación puede invocarse independientemente; verificar vigencia, elegibilidad, fecha valor y posterior envío; no prueba compensación extrahoraria. |
| AC-C7-21 | Polling inbound automático únicamente en ventana de liberación | `AchColombiaManagedMftHandlers.cs:259-274,277-312` | **TRAZADA** | Scheduler omite polling fuera de franja; no implica que un archivo tardío deba rechazarse. |
| AC-C7-22 | Recepción tardía y reproceso manual conservan archivo y trazabilidad | `AchColombiaManagedMftInboundWorkflow.cs:404-451; AchColombiaManagedFileExchangeTests.cs:374-409; RUNBOOK_UAT_Y_PREPRODUCTIVO.md:332-342` | **PARCIAL** | InboundOutsideExpectedWindow se registra sin rechazo regulatorio inventado; operación y aceptación externas no comprobadas. |
| AC-C7-23 | Conciliación al cierre C7: planilla, pago, resultado y aplicación | `RegulatoryCycleScheduleCatalog.cs:230-237; AchColombiaManagedMftHandlers.cs:253-315` | **NO_DEMOSTRADA** | H03: falta evidencia bancaria integrada y ledger conciliado. |

**Conteo I08: 23 reglas = 9 TRAZADAS, 8 PARCIALES, 6 NO_DEMOSTRADAS, 0 incumplimientos nuevos confirmados.** Los hallazgos anteriores se amplían, no se cuentan como nuevos.

## 3. Regla diferenciada: NACHA-M fuera de ciclos

Una condición de **recepción, preparación, generación, transmisión, aplicación contable o compensación** no debe reducirse a un único `IsInsideCycle`:

| Situación | Evidencia del proyecto | Conclusión estrictamente estática |
|---|---|---|
| Generar NACHA-M, sin enviarlo automáticamente | `NachaSecurityOperationService.cs:240-274` invoca al builder por ciclo, en ruta independiente | No se identificó la guarda `EnsureAutomaticOutboundInsideSendingWindow` en esta ruta. No prueba que todos los artefactos sean elegibles ni su aceptación. |
| MFT **automático saliente** | `AchColombiaManagedMftOutboundWorkflow.cs:49-50,311-329`; prueba `AutomaticOutbound_OutsideCycleSendingWindow_ShouldFailClosedBeforeBuildingFile` | Se impide envío automático fuera de ventana de remisión. Esto no debe extrapolarse a ejecución manual. |
| MFT **manual saliente** | `AchColombiaManagedMftImplementationSupport.cs:63-73` requiere `ManualOutboundAllowed`; en `AchColombiaManagedMftOutboundWorkflow.cs:49-50` la guarda de ventana se ejecuta solo para `Automatic`; prueba `ManualOutbound_ShouldRemainAvailableWhenAutomaticExecutionIsDisabled` | **Existe una ruta manual no sujeta a la misma guarda de ventana automática.** Requiere controles de habilitación, permisos, idempotencia y justificación; no se puede declarar autorización normativa de compensación extrahoraria con esta sola prueba. |
| Polling **automático entrante** | `AchColombiaManagedMftHandlers.cs:261-274` | El scheduler omite polling fuera de la ventana de liberación; esto no prohíbe la llegada física o el tratamiento posterior de un archivo. |
| Archivo **recibido fuera de horario esperado** | `AchColombiaManagedMftInboundWorkflow.cs:404-451`; test `LateInbound_ShouldPreserveResolvedAchCycleIdFromIngestion`; `docs/operations/RUNBOOK_UAT_Y_PREPRODUCTIVO.md:332-338` | Se registra observación `InboundOutsideExpectedWindow` y se conserva asociación con ciclo. El runbook ordena conservar archivo/evidencia y **no inventar rechazo regulatorio**. |
| Asignar transacciones a ciclo futuro | `BatchResolver.cs:131-160,256-275` | Existe lógica de fecha operativa y proyección previa a ciclo del día hábil siguiente. No equiparar originación anticipada con liquidación anticipada; consultar I09 para escenarios y límites. |

**Decisión D-I08-01:** no implementar una regla general tipo `NACHA_OUTSIDE_ANY_CYCLE_REJECT`. Separar por **tipo de operación, origen automatic/manual, cámara, fecha valor, ciclo operativo destino, permiso y condiciones de compensación**, sin nuevos servicios o abstracciones en esta auditoría. **H09 no se abre** al no haberse confirmado un incumplimiento; se conserva como caso transversal prioritario de I09.

**Casos mínimos de regresión posteriores:** automático fuera 16:01–18:00 negado; manual habilitado fuera de ventana a través de ruta autorizada sin reclasificarlo como automático; manual no habilitado negado; archivo tardío recibido conservado y marcado, no duplicado; crédito ordinario C7 permitido; débito monetario ordinario C7 rechazado; devolución de débito C3–C6 analizada como Return, no MonetaryDebit; C2→C7 retorno negado; ningún envío manual genera aceptación/liquidación ficticia.

## 4. Continuidad de hallazgos (sin duplicados)

- **H01 ALTA:** `ReturnWindow(7, Current(3)..Current(6))` está en el catálogo y su test; no se demostró chequeo de ciclo/día original en `AchReturnsService` (C2 actual fuera de ventana). C7 específico.
- **H02 MEDIA:** manual V35 de 5 ciclos frente a política de 7, vigencia declarada en código, aplicación en ambiente no confirmada.
- **H03 ALTA:** planilla/pago/liberación C7 en catálogo, conciliación y acuse externo no acreditados.
- **H04 PENDIENTE:** ROR / devolución de una devolución se audita I09, no confundir con devolución corriente.
- **H05 MEDIA / H07 ALTA:** R10 posee límites particulares y ordenamiento `StartTime` no equivalente al orden lógico del ciclo 1 nocturno. **No afirmar indebidamente un defecto nuevo C7 por C1→C7**, que está fuera de la ventana ordinaria.
- **H06 MEDIA condicional:** fecha operativa y ciclo 1 nocturno, pendiente revisión transversal.
- **H08 MEDIA:** catálogo de PSE/SSS/DIAN encontrado, enforcement por producto/ciclo no demostrado.
- **D-I08-01:** los archivos NACHA-M tienen rutas fuera de ventana según modo; no rechazo automático universal, pero tampoco compensación extrahoraria automática. Aclaración agregada a `MEMORY_STATE` y transferida a I09.

## 5. Verificaciones y límites

- **14/14 chequeos ligeros** sobre presencia de textos/código y nombres de pruebas de catálogo, política C7, veto débito, ventana de retorno, manual MFT, automatic out-of-window y recepción tardía. **No se ejecutaron** suites .NET ni pruebas de integración.
- Se contrastó fuente documental C7 (página 3) con `RegulatoryCycleScheduleCatalog.cs:230-237`, `RegulatoryCycleScheduleCatalogTests.cs:27-55,103-150` y rutas MFT citadas.
- Sin DB, Quartz, SFTP, conciliación real ni homologación externa ejecutada; no bloquea inspección estática. Ninguna ejecución no realizada cuenta como aprobada.
- **I09 siguiente:** causales completas, ROR, NACHA-M, rechazos, producto×ciclo, H01/H03/H04/H05/H07/H08 y escenarios de archivos extrahorarios por modalidad. **I10** consolidará dictamen.
