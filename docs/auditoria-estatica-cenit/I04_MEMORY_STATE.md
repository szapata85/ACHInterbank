# I04_MEMORY_STATE — auditoría estática incremental CENIT

**LEER PRIMERO PARA CONTINUAR. Corte 2026-11-01.** Último ZIP completo `ACHInterbank_PROYECTO_COMPLETO_AUDITORIA_CENIT_I04_2026-11-01.zip`, raíz `ACHInterbank_ajustado/`. Usar **I04** como base inmediata de I05; no reconstruir I01–I03. No modificar código productivo ni la implementación ACH Colombia sin orden explícita.

## Estado general al cerrar I04

- **I01–I04 COMPLETADOS**; I05–I10 pendientes: **4/10 = 40 % de hitos**, NO porcentaje de conformidad normativa/certificación.
- **I04: 30 reglas, 5 cumplen estáticamente en ruta delimitada, 18 parciales, 4 incumplimientos estáticos demostrados, 3 no demostrables**. I03 28 (3/18/3/4); I02 24 (2/14/3/5); I01 12 preliminares (8 parcial, 4 no demostrables). Las reglas por ciclo tienen solapamiento; no sumar como porcentaje de conformidad global.
- **Hallazgos nuevos 1, acumulados 5, abiertos 5, cerrados 0.** HC-CENIT-001/003 se amplían a transición C3→C4; HC-CENIT-004 se confirma en C3; HC-CENIT-002 sigue circunscrito a ROR C1; **nuevo HC-CENIT-005**: la prioridad CENIT para optimización ignora PPD/CCD/CTX y retorno débito/crédito. Identificadores y correcciones en `REGISTRO_HALLAZGOS.md`.
- **Fuentes:** DSP-152 Anexo 2 27-feb-2025 §§3.1–3.3, 4.1–4.8, 5.1–5.2; Anexo A 28-nov-2023; Anexo B exclusivamente STA; Manual CENIT-WEB 13-jun-2025; NACHA-M 07-may-2026 (edición aportada, vigencia externa no certificada). Diferencias DN-CEN-001…006 y criterio de uso en `FUENTES_NORMATIVAS_Y_DIFERENCIAS.md`; no resolver por intuición.
- **Pruebas:** .NET/DB/Quartz/SEBRA/CUD/MFT/cámara y certificación NO ejecutadas. Se leyó código y tests; sin cambios productivos. Integridad SHA-256 y CRC en `MANIFIESTO_I04.md` y `SHA256_ADICIONES_I04.csv`.

## Causas raíz activas

1. **HC-CENIT-001 (Alta):** `BatchResolver.cs:248-251` → `CenitCycleQueueService.cs:21-47` encola pero no reasigna ciclo/lote; `CenitCycleExecutionService.cs:71-85` consume; `CenitNettingService.cs:50-53` excluye transacciones con `AchCycleId` antiguo. C1→C2, C2→C3, C3→C4. **No** confundir con liquidez (`LiquidityOptimizationService.cs:139-170`) que sí cambia ciclo.
2. **HC-CENIT-002 (Alta):** creación ROR C1 deja `SourceReturnTransactionId` sin rellenar (`CenitReturnOfReturnService.cs:106-115`); generador contextual C1 exige fuente Return anterior (`AchReturnOfReturnFileGenerationService.cs:391-462`). No extrapolado a C3.
3. **HC-CENIT-003 (Alta):** `CenitCycleExecutionService.cs:84-85` invoca netting antes de optimización; `CenitNettingService.cs:50-105` incluye todo, mientras `LiquidityOptimizationService.cs:135-170` difiere C3→C4 sin reconciliar detalles netos. No afirmar contabilización CUD demostrada.
4. **HC-CENIT-004 (Alta):** `CenitIncomingReturnPolicy.cs:137-160,263-266` admite R23 de mismo día en C2–C5, incluido C3, aunque tabla DSP-152 §3.2 describe R23 **de días anteriores**. Policy consumida por Return In/Out en `IncomingNachaPostParseProcessor.cs` y `AchReturnEligibilityPolicyComponent.cs`. Validación temporal y vigencia a confrontar con autoridad antes de corregir.
5. **HC-CENIT-005 (Alta, I04):** `TransactionPriorityPolicy.cs:18-26` resuelve solo `transaction.Type`; `AchRegulatoryCatalogService.cs:20-27,235-243` devuelve una prioridad por tipo, omite servicio PPD/CCD/CTX, sentido de retorno; `RegulatoryCatalogSeeder.cs:449-455` mantiene catálogo genérico y filas ReturnDebit/ReturnCredit sin consumo; `LiquidityOptimizationService.cs:111-117` ordena por esa prioridad antes de C3→C4. Norma §5.1 10 prioridades. **Corrección mínima CENIT específica**, preservar ACHCOL.

## I04: archivos examinados

`RegulatoryCycleScheduleCatalog.cs`, `CycleTransactionPolicy.cs`, `AchCycleScheduler.cs`, `AchCycleSchedulerHandler.cs`, `CenitOperatingCalendarPolicy.cs`, `BatchResolver.cs`, `CenitCycleQueueService.cs`, `CenitCycleExecutionService.cs`, `CenitNettingService.cs`, `LiquidityOptimizationService.cs`, `TransactionPriorityPolicy.cs`, `AchRegulatoryCatalogService.cs`, `RegulatoryCatalogSeeder.cs`, `CenitIncomingReturnPolicy.cs`, `IncomingNachaPostParseProcessor.cs`, `IncomingNachaCycleResolver.cs`, `AchReturnsService.cs`, `AchReturnEligibilityPolicyComponent.cs`, `CenitReturnOfReturnService.cs`, `AchReturnOfReturnFileGenerationService.cs`, `AchPendingCycleReassignmentService.cs`, `CenitChamberResponseService.cs`, `CenitLocalGatewayPickupHostedService.cs`, `AchOutboundReturnDispatchService.cs`, `OperationalCalendarService.cs`, `AchTransaction.cs`, `TransactionTypeEnum.cs`. Pruebas fuente `CenitReturnOfReturn2026Tests.cs`, `CenitOperationalGovernanceTests.cs`, `CenitIncomingReturnPolicyTests.cs` **solo inspeccionadas**. Fuente normativa exacta en `I04_REGLA_A_REGLA.md` (30 filas).

## I05 — siguiente incremento

Auditar CENIT **ciclo 4, 15:30–17:15**, última sesión de entrada ordinaria, devoluciones de C3, prenotes, C4→C5, R23, ROR y cierre. **Especial** DSP-152 2025 §5.2(b): optimización con fondos disponibles de CUD, sin 15 minutos extra, recálculo multilateral y rechazo de pendientes, sin trasladar a C5 ordinarias prohibidas. Reusar hallazgos 001–005 sin duplicar: revisar especialmente HC-005 en orden y HC-003 en recálculo. Prioridad en I05: no declarar prueba CUD ejecutada ni inventar regulación. Generar `I05_REGLA_A_REGLA.md`, fotografía, memoria, manifiesto, hashes y ZIP completo.
