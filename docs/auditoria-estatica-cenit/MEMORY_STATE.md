# MEMORY_STATE — continuidad final CENIT

## Corte final I10

**Fecha:** 2026-11-02. **Incremento I10 COMPLETADO.** **Auditoría I01–I10: 10/10 = 100 % de hitos; NO de cumplimiento/certificación.** Base íntegra siguiente: `ACHInterbank_PROYECTO_COMPLETO_AUDITORIA_CENIT_I10_2026-11-02.zip` raíz `ACHInterbank_ajustado/`.

## Estado final

- Reglas I01–I09: **333 filas** (66 cumplimiento estático delimitado, 193 parcial, 44 incumplimiento estático en ruta, 30 no demostrable).
- I10: **20 verificaciones de cierre** (3 cumple delimitado, 3 parcial, 13 incumple, 1 no demostrable); corresponden a normas/casos ya evaluados, no 20 requisitos nuevos únicos.
- **13 hallazgos acumulados, 13 abiertos, 0 cerrados, 0 nuevos I10:** `HC-CENIT-001…013`; prioridades y correcciones en `HALLAZGOS_CONSOLIDADOS_CENIT.md` y `PLAN_CORRECCIONES_MINIMAS_CENIT.md`.
- **Decisión de alcance permanente:** CUD externo con revisión manual de Tesorería. **No** API CUD, neteo oficial propio, liquidación ni asientos. Sí control de respuestas/estados por CENIT y conciliación. Efectos transaccionales de simulación Quartz (`003/005/006`) permanecen como defectos de software, no falta de API.
- **Estado documental:** DN-CEN-001…008 conservadas; NACHA 2026 por fecha interna con vigencia operativa a confirmar. DSP-152 2025 y Anexo 2 verificables en web oficial; no se excluyen circulares no aportadas.
- **Resultado:** no conformidad estática delimitada; **no homologado**, sin pruebas .NET/DB/Quartz ni operador/CUD, con 22 escenarios posteriores propuestos y NO ejecutados.
- **Código productivo:** sin cambios en I01–I10. No alterar ACH Colombia, contratos, migraciones, configuraciones ni pruebas dentro de este cierre.

## Documentos de consulta para futura corrección

1. `DICTAMEN_ESTATICO_FINAL_CENIT.md` y `MATRIZ_CUMPLIMIENTO_FINAL_CENIT.md`.
2. `HALLAZGOS_CONSOLIDADOS_CENIT.md`, `REGISTRO_HALLAZGOS.md`, `PLAN_CORRECCIONES_MINIMAS_CENIT.md`.
3. `MATRIZ_PRUEBAS_POSTERIORES_CENIT.md` y `I10_REGLA_A_REGLA.md`.
4. `FUENTES_NORMATIVAS_Y_DIFERENCIAS.md`, `IXX_REGLA_A_REGLA.md` y manifiestos I01–I10.

## Próxima fase, fuera de este plan

**Correcciones mínimas y pruebas de integración/certificación**, solo por decisión expresa del usuario. Conservar los IDs `HC-CENIT-001…013`, crear un registro de corrección/prueba por cada uno y actualizar estado únicamente con evidencia ejecutada. No repetir I01–I10 ni afirmar pruebas ya aprobadas.

---

## Historial de memoria I09 y anteriores (conservado)

# MEMORY_STATE — auditoría estática incremental CENIT

**LEER PRIMERO — corte I09, 2026-11-02.** Base de continuidad **I10**: `ACHInterbank_PROYECTO_COMPLETO_AUDITORIA_CENIT_I09_2026-11-02.zip` (raíz `ACHInterbank_ajustado/`). No reconstruir I01–I09; no modificar productivo ni ACH Colombia sin autorización.

## Estado actual I09

- **I01–I09 COMPLETADOS = 9/10 (90 % de hitos); I10 PENDIENTE**. El avance **NO** mide cumplimiento normativo ni certificación.
- **50 reglas I09:** 16 cumplimiento estático delimitado, 23 parcial, 8 incumplimiento estático demostrado en ruta, 3 no demostrable. 8 filas incumplidas corresponden a **6 causas raíz** (`HC-CENIT-001/003/005/006/012/013`).
- **Hallazgos:** 1 nuevo `HC-CENIT-013`, **13 acumulados, 13 abiertos, 0 cerrados**. `REGISTRO_HALLAZGOS.md` y `I09_REGLA_A_REGLA.md` contienen evidencia y acciones; se preservan todos los IDs anteriores.
- **DECISIÓN DE ALCANCE VINCULANTE:** CUD es **frontera externa operada/verificada manualmente por Tesorería**. ACH Interbank NO debe construir API CUD, ejecutar liquidación, repetir algoritmo oficial ni contabilizar. Sí debe procesar las salidas CENIT que le corresponden y controlar estados, idempotencia, rechazos y conciliación. **Falta de API CUD no es hallazgo.** Reglamento DSP-152 arts.18–19 / Anexo 2 §5.2, documentos de alcance internos.
- **Impacto en HC-003/005/006:** no pedir saldo CUD online ni completar un motor equivalente al de CENIT. Evitar que neteo/optimización **simulados desde Quartz** cambien estados operativos finales o sean representados como liquidación. `AchCycleSchedulerHandler:81-103` → `CenitCycleExecutionService:71-90` → neteo → `LiquidityOptimizationService:105-229` marca `ReturnedByOperator` en C4/C5 desde `AvailableLiquidity Simulated`. Mantener hallazgos abiertos hasta arreglo.
- **NUEVO `HC-CENIT-013`:** `CenitChamberResponseService.ImportItemAsync:188-207,229-285` cruza rechazo de ítem CENIT por traza, pero cambia `AchFileExport.ChamberResponseState` global, no `AchTransaction.State`; no se observó evento transaccional. Solución acotada en ingestión de respuesta oficial, no CUD.
- **HC-012** permanece (ADV 280/ADV no interpretado en rutas de respuesta), **HC-001** sigue (cola tardía consumida sin inclusión); no generar IDs por ciclo/archivo.
- **Conciliación:** `AchOperationalReconciliationService` admite `ExternalEvidence` y revisiones pero contiene invariante con fuente ACH-Colombia-V35:2.6.2 no condicionado por cámara; como no se localizó consumidor productivo de ese servicio en I09, se clasificó PARCIAL, sin presumir ejecución CENIT ni nuevo incumplimiento. `Balance` se refiere a consistencia de evidencia suministrada, no confirmación CUD.
- **Fuentes:** DSP-152 27-feb-2025 arts.18–19 y Anexo 2 §§3.1–3.3,5.1–5.2; NACHA-M 07-may-2026 §§7.3.2–7.3.3; Anexo A/B según servicio; contexto operativo del proyecto. Referencias precisas en matriz I09. No fuente posterior acreditada que cambie reparto de competencias.
- **Pruebas NO ejecutadas:** no .NET, DB, Quartz, CENIT gateway real, CUD ni certificación. No se modificó productivo/pruebas/config. Ver manifest/hash del I09.
- **SIGUIENTE I10:** emitir dictamen final, matriz consolidada, 13 hallazgos sin renumerar, correcciones mínimas compatibles con frontera CUD y pruebas externas diferenciadas. Entregar ZIP completo con `DICTAMEN_ESTATICO_FINAL_CENIT.md`, matrices, `I10_MEMORY_STATE.md` y `MEMORY_STATE.md` final. **No inferir homologación.**

## Corte histórico I08 (conservado, cifras correspondientes a I08)


**LEER PRIMERO. Corte I08: 2026-11-01.** ZIP completo de continuidad: `ACHInterbank_PROYECTO_COMPLETO_AUDITORIA_CENIT_I08_2026-11-01.zip`. Raíz `ACHInterbank_ajustado/`. **Base I09 = I08**, NO reconstruir I01–I08 y NO alterar productivo/ACH Colombia sin autorización.

## Estado actual I08

- **Hitos 8/10 (80 % del plan de auditoría, NO porcentaje de conformidad ni certificación)**. I01–I08 terminados; pendientes I09 (netting, CUD, conciliación, contingencia) e I10 (dictamen, matriz de cumplimiento, priorización y pruebas de certificación).
- **I08: 55 reglas**: **16 cumple estáticamente en ruta delimitada, 29 parcial, 7 incumple en rutas delimitadas, 3 no demostrable**. No sumar filas solapadas como defectos únicos.
- **Hallazgos I08: 3 nuevos** (`HC-CENIT-010` repertorio Ñ/ñ, `HC-CENIT-011` mezcla códigos Dxx STA en validación NACHA-M, `HC-CENIT-012` ausencia de tratamiento específico ADV en rutas oficiales). **Acumulados 12, abiertos 12, cerrados 0**. `HC-CENIT-001` ampliado (archivo fuera de ventana y consumo de cola). `HC-CENIT-002…009` permanecen abiertos, sin declararlos cerrados en esta auditoría.
- **Fuentes I08**: Manual NACHA-M CENIT 07-may-2026 §§3–7, Anexo 1, Anexo 2 tablas 2/3/7; DSP-152 Anexo 2 27-feb-2025 §§3.1–3.3, 12.1; Anexo B 28-nov-2023 solo **STA**. Manual de mayo 2026 usado por fecha interna; confirmación de publicación externa posterior pendiente. DN-CEN-001…007 preservadas y DN-CEN-008 añadida (alcance de nomenclaturas de archivo según dirección/canal).
- **Ruta saliente rastreada**: `NachaFileGenerationCoordinator.BuildNachaFileAsync/BuildReturnOutAsync` → `NachaChamberFlowPolicyResolver.Cenit` → perfiles `CenitOrdinaryOutbound2026Layout`/`CenitCtxOutbound2026Layout`/`CenitReturnOut2026Layout` → `NachaSemanticValidator`, bloque 10, `ExternalFileNameBuilder/Validator`. CTX separado, controles de adenda y conteos presentes con límites documentados en matriz.
- **Ruta entrante rastreada**: `IncomingNachaIngestionAppService.IngestAsync` → `IncomingNachaCycleResolver` → `IncomingNachaAdmissionValidator` → `NachaParserService.ParseAndSaveDetailedAsync` → `NachaPhysicalParser` → `NachaInboundValidator`/`NachaInboundCenitValidationRules` → persistencia. Rechazos formales/fatales no se equiparan a STA.
- **Respuestas y gateway**: `CenitLocalGatewayPickupHostedService` → `CenitChamberResponseService.ImportAsync/Parse`. ACK/NACK XML y conciliación opaca presentes. **ADV** clase 280 SEC ADV requiere tratamiento específico no observado; no se afirma daño externo de CUD ni ausencia de sistema externo al ZIP.
- **Fuera de ventana**: DSP-152 §3.3 exige recepción y cola; `HC-CENIT-001` sigue demostrado en `CenitCycleQueueService` → `CenitCycleExecutionService` → `CenitNettingService`. `IncomingNachaAdmissionValidator` bloquea ingestion cuando la ventana del ciclo vinculado ha expirado, **clasificado parcial** porque dirección y contrato de recepción de operador no prueban que aplique exactamente §3.3; evitar inferir bloqueo global por horario.
- **Archivos examinados**: `NachaPhysicalParser.cs`, `NachaParserService.cs`, `NachaInboundValidation.cs`, `NachaRecordFieldValidator.cs`, `NachaSemanticValidator.cs`, `NachaControlTotalsCalculator.cs`, `NachaFileBlocking.cs`, `NachaFileGenerationCoordinator.cs`, `NachaChamberFlowPolicy.cs`, `CenitOrdinaryInbound2026Layout.cs`, `CenitOrdinaryOutbound2026Layout.cs`, `CenitCtxOutbound2026Layout.cs`, `CenitReturnIn2026Layout.cs`, `CenitReturnOut2026Layout.cs`, `CenitReturnOfReturn2026Layout.cs`, `IncomingNachaIngestionAppService.cs`, `IncomingNachaAdmissionValidator.cs`, `IncomingNachaCycleResolver.cs`, `NachaIncomingFileProcessor.cs`, `CenitChamberResponseService.cs`, `CenitLocalGatewayFolderTransportAdapter.cs`, `CenitLocalGatewayPickupHostedService.cs`, `ExternalFileNameBuilder.cs`, `ExternalFileNameValidator.cs`, `CenitOfficialFileNameParser.cs`, `RegulatoryCatalogSeeder.cs`, `NachaReceiverNameHelper.cs`; pruebas fuente: `NachaPhysicalParserTests.cs`, `CenitOrdinaryInbound2026ParserTests.cs`, `CenitOutboundFilePartitionerTests.cs`, `CenitOfficialFileNameParserTests.cs`, `NachaControlTotalsCalculatorTests.cs`, `CenitChamberResponseLifecycleTests.cs`, `NachaInboundValidationSeparationTests.cs`, `NachaFunctionalValidationTests.cs`. **Pruebas observadas, NO ejecutadas.**
- **Límites**: sin .NET, SQL Server/Postgres, Quartz, gateway CENIT real, CUD, WSEBRA/STA, archivos ADV externos ni certificación. No se tocaron código, configuración, migraciones ni pruebas. Se compararán SHA-256 y CRC antes de completar entrega.
- **Próximo I09**: reconstruir flujo del netting y optimizador (HC-003/005/006), saldos CUD reales vs simulados, liquidación, avisos ADV y correlación, reintentos, pendientes, conciliación, servicio externo, contingencias; no inventar evidencia CUD.

## Estado histórico conservado — I07

**LEER PRIMERO. Corte I07 = 2026-11-01.** Último ZIP íntegro: `ACHInterbank_PROYECTO_COMPLETO_AUDITORIA_CENIT_I07_2026-11-01.zip`. Raíz `ACHInterbank_ajustado/`. **Base del próximo I08 = I07**; no reconstruir I01–I07 ni alterar productivo/ACH Colombia.

## Estado actualizado I07

- **Hitos 7/10 (70 % de auditoría, NO certificación); I01–I07 completos, I08–I10 pendientes.**
- **I07: 64 reglas**: 7 cumplimiento estático en ruta delimitada, 48 parcial, 8 incumplimiento estático demostrado, 1 no demostrable. Filas repetidas por causales y controles no equivalen a hallazgos únicos.
- **Hallazgos: 2 nuevos, 9 acumulados, 9 abiertos, 0 cerrados.** Nuevos `HC-CENIT-008` (ROR R74 acepta devolución padre distinta de R32) y `HC-CENIT-009` (R23 acepta fecha tope aportada sin cálculo siguiente día hábil). `HC-002`, `HC-004`, `HC-007` ampliados, `HC-001/003/005/006` pendientes sin cambios. Detalles y localizaciones en `I07_REGLA_A_REGLA.md` y `REGISTRO_HALLAZGOS.md`.
- **Fuentes:** Anexo A y B 28-11-2023; DSP-152 Anexo 2 27-02-2025; NACHA-M 07-05-2026 aportado, vigencia pública externa sin comprobación nueva. STA D01–D06 **separado de Return/ROR**. DN-CEN-001…007 sin solución normativa inventada.
- **Rutas:** `AchReturnsService.GenerateReturnsFileAsync` → `AchReturnEligibilityPolicyComponent` → `CenitIncomingReturnPolicy`; entrada `IncomingNachaPostParseProcessor`; ROR `CenitReturnOfReturnService` → `AchRegulatoryCatalogService` → `AchReturnOfReturnPolicies` sembrado por `RegulatoryCatalogSeeder`. Contraejemplo estático R74 padre R02 y R23 fecha límite autodeclarada, no se ejecutaron pruebas.
- **Límites:** no se ejecutó .NET ni DB/Quartz/CUD/SEBRA/STA ni certificación. 0 cambios código/config/pruebas. Todos los controles son estáticos, y los motivos fácticos de R61–R73 están parciales por falta de validación de soporte sin inferir automáticamente defecto material.
- **Archivos I07 examinados:** `CenitIncomingReturnPolicy.cs`, `CenitIncomingReturnPolicyModels.cs`, `AchReturnDtos.cs`, `AchReturnsService.cs`, `AchReturnCausePolicyComponent.cs`, `AchReturnEligibilityPolicyComponent.cs`, `AchReturnOriginalCorrelationComponent.cs`, `AchReturnOfReturnEligibilityService.cs`, `AchReturnOfReturnFileGenerationService.cs`, `CenitReturnOfReturnService.cs`, `CenitReturnOfReturn2026Layout.cs`, `CenitReturnIn2026Layout.cs`, `CenitRawReturnContractGate.cs`, `AchCauseCodePolicy.cs`, `AchRegulatoryCatalogService.cs`, `RegulatoryCatalogSeeder.cs`, `IncomingNachaPostParseProcessor.cs`, `IncomingNachaIngestionAppService.cs`, `NachaChamberFlowPolicy.cs`; tests fuente `CenitIncomingReturnPolicyTests.cs`, `CenitReturnOfReturn2026Tests.cs`, `AchReturnIdempotencyTests.cs`, `RegulatoryCatalogSeederReturnOfReturnPoliciesByClearingHouseTests.cs`.
- **Siguiente I08:** NACHA-M PPD/CCD/CTX 106 caracteres, adendas, contadores, ADV, rechazos formal/fatal y STA real (separación), envío/recepción fuera de ciclo y encolamiento; verificar HC-001, trazabilidad de archivo y contrato. Producir `I08_REGLA_A_REGLA.md`, snapshot, manifiesto, hashes, ZIP completo y verificación SHA.

## Memoria histórica de I06 (conservada; cifras previas pertenecen a corte I06)



**LEER PRIMERO PARA CONTINUAR. Corte 2026-11-01.** Último ZIP completo: `ACHInterbank_PROYECTO_COMPLETO_AUDITORIA_CENIT_I06_2026-11-01.zip`, raíz `ACHInterbank_ajustado/`. **Base inmediata I07 = I06**; no reconstruir I01–I06 ni modificar ACH Colombia/productivo sin solicitud expresa.

## Estado general, hitos y evidencias

- **I01–I06 COMPLETADOS = 6/10 (60 % hitos)**, I07–I10 pendientes. Esto NO mide conformidad normativa ni certificación.
- **I06 (C5 17:45–18:45; salida límite 19:15): 36 reglas**, **10 cumplen estáticamente en ruta delimitada, 17 parcial, 6 incumplimiento estático demostrado, 3 no demostrable**. I05 34 (7/18/5/4); I04 30 (5/18/4/3); I03 28 (3/18/3/4); I02 24 (2/14/3/5); I01 12 preliminares (0/8/0/4). Solapamientos de reglas entre ciclos; no calcular porcentaje de conformidad sumando filas.
- **Hallazgos: I06 nuevos 1 (`HC-CENIT-007`), acumulados 7, abiertos 7, cerrados 0**. Por causa raíz IDs 001–007 en `REGISTRO_HALLAZGOS.md`. En I06 6 incumplimientos = HC-001/003/004/005/006/007. HC-002 permanece abierto solo en la ruta de C1, no atribuir por defecto a C5.
- **Documentos principales:** `docs/normativa/md/CENIT-DSP-152-Anexo-2.md` (27-feb-2025 §§3.1–3.3, 4.2–4.8, 5, 12.1); circular DSP-152 reglamento (art.19); Anexo A 28-nov-2023; Anexo B solo STA; Manual Usuario CENIT 13-jun-2025; NACHA-M 07-may-2026 aportado (vigencia externa no confirmada). Diferencias DN-CEN-001…007 en `FUENTES_NORMATIVAS_Y_DIFERENCIAS.md`.
- **Limitaciones:** únicamente inspección de código fuente, rutas, configuración, contratos y fuentes de pruebas. **NO** ejecutados .NET/test suite, SQL Server, PostgreSQL, Quartz, CUD, SEBRA, MFT/STA, integración ni certificación bancaria. **Ningún cambio productivo**.

## Hallazgos pendientes y decisiones

1. **HC-CENIT-001 Alta:** cola fuera de ventana `BatchResolver.cs:248-251` → `CenitCycleQueueService.cs:21-47` → `CenitCycleExecutionService.cs:71-85` consume sin reasignar la operación; `CenitNettingService.cs:50-53` filtra ciclo real. En C4→C5, validar además elegibilidad C5 antes de ejecución. No crear nuevo ID para cada ciclo.
2. **HC-CENIT-002 Alta:** solo ROR originado en C1, falta `SourceReturnTransactionId` (`CenitReturnOfReturnService.cs:106-115`/`AchReturnOfReturnFileGenerationService.cs:391-462`). No generalizar a ROR C5 sin nueva evidencia.
3. **HC-CENIT-003 Alta:** netting registra `IncludedInSettlement=true` para todos (`CenitNettingService.cs:84-105`) antes de optimización (`CenitCycleExecutionService.cs:84-90`). C4 y C5 rechazan pendientes sin recomposición; recomputar membresía neta antes de CUD.
4. **HC-CENIT-004 Alta:** excepción R23 mismo día autorizada por política en C2–C5 (`CenitIncomingReturnPolicy.cs:234-266`), no corresponde literalmente con «devoluciones de días anteriores» §3.2. Contrastar cualquier interpretación oficial posterior; no reetiquetar como R06.
5. **HC-CENIT-005 Alta:** optimización utiliza prioridad por `TransactionTypeEnum` (`TransactionPriorityPolicy.cs:18-26`, `AchRegulatoryCatalogService.cs:20-27`, `LiquidityOptimizationService.cs:111-117`), no tabla 1–10 PPD/CCD/CTX y retornos (§5.1); impacta C4 y C5.
6. **HC-CENIT-006 Alta:** C4 y C5 rechazados por insuficiencia sobre `AvailableLiquidity` tipo **Simulated** (créditos internos) en `CenitNettingService.cs:63-80` y `LiquidityOptimizationService.cs:105-120,173-199`, no saldo confirmado CUD. No declarar asiento CUD real errado.
7. **HC-CENIT-007 Alta, NUEVO I06:** la rama R06 del `CenitIncomingReturnPolicy` (`Evaluate:70-88`, `EvaluateR06:188-231`) es llamada por `AchReturnsService.cs:95-107`/`AchReturnEligibilityPolicyComponent.cs:95-120` y por `IncomingNachaPostParseProcessor.cs:581-598`; si origen C1–C3 y retorno C5, puede retornar Allowed con flags operativos positivos al omitir regla `EvaluateOrdinaryMonetaryReturn` de ciclo inmediato. §3.2 C5 restringe los retornos ordinarios al C4; corrección mínima: validación de sesión de origen en C5 manteniendo controles R06 y contrastando aclaraciones regulatorias en I07.

## C5: controles confirmados, límites y documentación

- Agenda `RegulatoryCycleScheduleCatalog.cs:172-176`: C5 17:45–18:45, output 19:15, `AllowsMonetaryCredit/Debit/CreditPrenotification/DebitPrenotification=false`. `CycleTransactionPolicy.cs:56-105`, `NachaTransactionValidationService.cs:52-73`, `ContrapartidaDispatchJobService.cs:1297-1312` consumen la política. No afirmar cierre externo ni todos los canales cubiertos.
- `CenitIncomingReturnPolicy.cs:163-186` permite devolución ordinaria monetaria C4→C5 en fecha valor y exige ciclo inmediato; prenotes deben retornar antes de último ciclo. **DN-CEN-007**: tensión textual de §3.2 «devoluciones del ciclo anterior» vs §4.8 «máximo en último ciclo de Devoluciones» para prenotes C1–C3. No inventar respuesta ni nuevo hallazgo hasta criterio autorizado.
- `CenitReturnOfReturnService.cs:226-269`: ROR del mismo día, devolución padre C2–C4 → C5; previos día hasta C2 del siguiente hábil. Revisar fecha `processedAtUtc.Date` en frontera de extensión C5 a 19:00 Bogotá (00:00 UTC siguiente): riesgo observado, aún **parcial** por falta de ruta/permiso de extensión.
- `LiquidityOptimizationService.cs:173-199` rechaza en C5 los pendientes por insuficiencia, **no** los difiere por saldo a día posterior; fallos HC-003/005/006 invalidan declarar cumplimiento integral.
- Distinción horaria: entrada antes de 18:45, entrega salida hasta 19:15 y ampliación excepcional autorizada §12.1 hasta 15 minutos para envío; **no** espera extra para fondos (suprimida 2025). Archivos fuera de ventana se reciben/encolan (HC-001), no se veta globalmente.
- `DXX-LIQ` es marcador interno, no causal Rxx de retorno ni Dxx STA. La respuesta de operador real y aviso ADV no demostrados.
- **Archivos examinados I06 (código):** `RegulatoryCycleScheduleCatalog.cs`, `CycleTransactionPolicy.cs`, `BatchResolver.cs`, `CenitIncomingReturnPolicy.cs`, `CenitReturnOfReturnService.cs`, `CenitCycleExecutionService.cs`, `CenitCycleQueueService.cs`, `CenitNettingService.cs`, `LiquidityOptimizationService.cs`, `CenitOperatingCalendarPolicy.cs`, `NachaTransactionValidationService.cs`, `IncomingNachaPostProcessingPipeline.cs`, `IncomingNachaDispatchEligibilityPolicy.cs`, `IncomingNachaPostParseProcessor.cs`, `AchReturnsService.cs`, `AchReturnEligibilityPolicyComponent.cs`, `AchReturnOriginalCorrelationComponent.cs`, `AchReturnOfReturnEligibilityService.cs`, `AchRegulatoryCatalogService.cs`, `CenitChamberResponseService.cs`, `CenitLocalGatewayPickupHostedService.cs`, `ContrapartidaDispatchJobService.cs`, `AchCauseCodePolicy.cs`, `AchCycleSchedulerHandler.cs`, `CycleCalendarGuard.cs`. **Tests leídos no ejecutados:** `CenitIncomingReturnPolicyTests.cs`, `CenitReturnOfReturn2026Tests.cs`, `CenitCycleCalendarCharacterizationTests.cs`, `CenitOperationalGovernanceTests.cs`.

## Siguiente I07

Auditar **devoluciones, devoluciones de devoluciones y causales**, Anexo A R01–R35 aplicables y R60–R74; R06/R23 límites, plazo, aplicación por servicio/fecha/estado, duplicidad, causal por operador y contrato STA separado. Priorizar HC-002/004/007, extender hallazgos previos sin duplicarlos. Crear `I07_REGLA_A_REGLA.md`, foto de memoria, manifiesto, hashes y ZIP completo. I08 NACHA-M+archivos/STA; I09 CUD+netting/conciliación; I10 dictamen final.

**Integridad I06:** base I05 (4576 entradas). Verificar comparación SHA-256 contra I05 al generar I06. Cambios autorizados solo a 5 documentos de auditoría (`PLAN_INCREMENTAL`, `MEMORY_STATE`, `ESTADO_INCREMENTOS`, `REGISTRO_HALLAZGOS`, `FUENTES_NORMATIVAS_Y_DIFERENCIAS`), más cuatro documentos nuevos I06; todo código/ACH Colombia/normativa/códigos previos inalterados.
