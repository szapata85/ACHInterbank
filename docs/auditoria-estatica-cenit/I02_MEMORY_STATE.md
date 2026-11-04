# MEMORY_STATE — auditoría estática incremental CENIT

**LEER PRIMERO EN EL SIGUIENTE CHAT.** Corte **2026-11-01**. Último paquete válido `ACHInterbank_PROYECTO_COMPLETO_AUDITORIA_CENIT_I02_2026-11-01.zip`; raíz ZIP `ACHInterbank_ajustado/`. **Nunca reconstruir desde I01 cuando existe este ZIP I02.** Preservar ACH Colombia I01–I10 y código.

## Estado al terminar I02

- I01 inventario **COMPLETADO**, I02 CENIT ciclo 1 **COMPLETADO**. **2/10 hitos = 20 %** del plan, NO porcentaje de conformidad. **I03–I10 pendientes (8)**.
- **I02 examinó 24 reglas:** 2 cumple estáticamente solo en ruta delimitada; 14 parcial; 3 incumplimiento estático demostrado; 5 no demostrable. **I01 preliminar:** 12 (8 parcial/4 no demostrable) **no sumar a los 24 como reglas C1** para obtener un porcentaje normativo.
- **Hallazgos:** nuevos I02 **3**, acumulados **3**, abiertos **3**, cerrados **0**: HC-CENIT-001 (cola fuera de ventana sin incorporar ciclo destino); HC-CENIT-002 (ROR C1 sin transacción fuente Return en flujo creado); HC-CENIT-003 (netting incluía transacciones antes de diferimiento por liquidez). `REGISTRO_HALLAZGOS.md` contiene mínimos correctivos y referencias; **no crear duplicados**.
- **Pruebas .NET, BD, Quartz, transporte, CUD, certificación, ACK/NACK/ADV:** NO EJECUTADAS. **Cero cambios** en código/configuración/pruebas/otros docs precedentes; solo docs de auditoría CENIT actualizados o añadidos. Integridad detallada en `MANIFIESTO_I02.md`, CSV hashes nuevos y `SHA256_LINEA_BASE_ACHCOL_I10.csv` histórico.

## Decisiones vinculantes

1. Seguir las fuentes y versión en `FUENTES_NORMATIVAS_Y_DIFERENCIAS.md`: DSP-152 Anexo 2 **27-feb-2025**; Manual CENIT-WEB **13-jun-2025**; NACHA-M CENIT adjunto **07-may-2026** (vigencia externa no certificada); Anexo A/B **28-nov-2023**. Discrepancias DN-CEN-001…006 abiertas como clasificación documental, no como bugs. STA D01–D06 NO son causales de retorno.
2. CENIT 5 sesiones, NO las 7 de ACH Colombia. **C1 07:30–10:30**: monetarias/prenotificaciones, excluye devoluciones ordinarias, admite R23 de días anteriores y ROR de devoluciones del día hábil bancario inmediatamente anterior. No deducir veto total de `AllowsReturnOfReturn=false`: `AchReturnOfReturnFileGenerationService.cs:287-305` permite excepción contextual, afectada por HC-CENIT-002.
3. Distinguir encolamiento por recepción fuera de ventana (`CenitCycleQueueService`, **HC-CENIT-001**) de traslado por insuficiencia en ciclos 1–3 (`LiquidityOptimizationService`, que sí actualiza `AchCycleId`). Nunca imponer bloqueos globales fuera de horario. Guardar fecha valor y huso Bogotá.
4. `CenitNettingService` usa `SimulatedLiquidity`, y `CenitCycleExecutionService` llama netting ANTES que optimización. **HC-CENIT-003** es inconsistencia de membresía interna, NO afirmación de doble CUD ejecutado. Liquidez externa y asientos siguen NO DEMOSTRABLES.
5. Nunca interpretar un test en código como aprobado; no modificar implementación sin autorización expresa. El ROR incluye R60–R74 (Anexo A), no solo R60–R69.

## Archivos y rutas auditadas en I02

`docs/auditoria-estatica-cenit/I02_REGLA_A_REGLA.md` tiene 24 filas con fuente/condición/ciclo/archivo y líneas/evaluación/evidencia/hallazgo/acción, y trazas de tres defectos. Fuentes principales: `RegulatoryCycleScheduleCatalog.cs:166-176`, `CycleTransactionPolicy.cs:57-71`, `AchCycleScheduler.cs:65-123`, `AchCycleSchedulerHandler.cs:59-103`, `CenitOperatingCalendarPolicy.cs:31-126`, `BatchResolver.cs:131-175,248-251`, `AchTransactionService.cs:335-341`, `AchBulkTransactionService.cs:269-278`, `CenitCycleQueueService.cs:21-47`, `CenitCycleExecutionService.cs:34-93`, `CenitNettingService.cs:36-105`, `LiquidityOptimizationService.cs:60-229`, `CenitIncomingReturnPolicy.cs:39-89,137-160,234-275`, `IncomingNachaPostParseProcessor.cs:444-456,508-598`, `AchReturnsService.cs:79-120`, `CenitReturnOfReturnService.cs:26-119,226-269`, `AchReturnOfReturnFileGenerationService.cs:151-185,287-305,391-462`, `NachaTransactionValidationService.cs:52-79`; pruebas identificadas pero no ejecutadas `CenitCycleCalendarCharacterizationTests`, `CenitIncomingReturnPolicyTests`, `CenitReturnOfReturn2026Tests`, `CycleTransactionPolicyTests`.

## Instrucciones para I03

**Auditar CENIT ciclo 2 11:00–13:00**: apertura, entradas monetarias/no monetarias, devoluciones de C1 en ciclo inmediato, R23, ROR previo día y límite segundo ciclo, recepción→cola→archivo, NACHA/ACK/NACK, compensación y diferimientos C2→C3, fecha valor/día hábil y aplicación. Reutilizar hallazgos HC-CENIT-001/002/003 si misma causa raíz, ampliar evidencia. Investigar si C2 presenta rechazo indebido por política C1 residual y seguimiento idempotente. Crear `I03_REGLA_A_REGLA.md`, actualizar memoria, manifiesto y ZIP **completo** con hashes. I04–I10 quedan según plan.
