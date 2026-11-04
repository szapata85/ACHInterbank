# Fotografía independiente I05 — 2026-11-01


**LEER PRIMERO PARA CONTINUAR. Corte 2026-11-01.** Último ZIP completo: `ACHInterbank_PROYECTO_COMPLETO_AUDITORIA_CENIT_I05_2026-11-01.zip`, raíz `ACHInterbank_ajustado/`. **Base inmediata de I06 = I05.** No reconstruir I01–I04; no modificar productivo ni ACH Colombia sin instrucción explícita.

## Estado y cifras

- **I01–I05 COMPLETADOS = 5/10, 50 % de hitos**, I06–I10 pendientes. No mide conformidad ni certificación.
- **I05:** 34 reglas; 7 cumplimiento estático local, 18 parcial, 5 incumplimientos estáticos demostrados, 4 no demostrables. I04 30 (5/18/4/3); I03 28 (3/18/3/4); I02 24 (2/14/3/5); I01 12 preliminares (0/8/0/4). Solapamientos entre ciclos, no sumar porcentajes.
- **Hallazgos I05:** 1 nuevo (`HC-CENIT-006`); **6 acumulados, 6 abiertos, 0 cerrados**. HC-001/003/004/005 aplican C4 o transición; HC-002 restringido a C1. Referencias en `REGISTRO_HALLAZGOS.md`.
- **Norma principal:** Anexo 2 DSP-152 27-feb-2025 §§3.1–3.3, 4.1–4.8, 5.1–5.2, 12.1–12.2; Reglamento art.19; Anexo A 28-nov-2023; Anexo B solo STA; guía CENIT-WEB 13-jun-2025; NACHA-M 07-may-2026 aportado (vigencia pública no asegurada). Registro DN-CEN-001…006 en `FUENTES_NORMATIVAS_Y_DIFERENCIAS.md`.
- **NO se ejecutaron** pruebas .NET, SQL/Postgres, Quartz, CUD, STA/MFT, SEBRA ni certificación. Todas las evidencias son inspección estática de código, catálogos, contratos y tests. Ningún cambio productivo.

## Hallazgos abiertos y decisiones de continuidad

1. **HC-CENIT-001 (Alta):** cola tardía `BatchResolver.cs:248-251` → `CenitCycleQueueService.cs:21-47` → `CenitCycleExecutionService.cs:71-85` marca `Consumed` sin mover el lote/ciclo; `CenitNettingService.cs:50-53` filtra ciclo destino. C3→C4 y C4→C5 requieren además validar elegibilidad del nuevo ciclo; no crear hallazgo nuevo por la misma causa.
2. **HC-CENIT-002 (Alta):** ruta específica ROR C1 no asigna `SourceReturnTransactionId` (`CenitReturnOfReturnService.cs:106-115`) y generador C1 necesita evidencia de fuente (`AchReturnOfReturnFileGenerationService.cs:391-462`). No atribuirlo a C4 o C5 sin ruta nueva.
3. **HC-CENIT-003 (Alta):** netting antes de optimización `CenitCycleExecutionService.cs:84-85`; `CenitNettingService.cs:84-105` guarda todos `IncludedInSettlement=true`; `LiquidityOptimizationService.cs:173-199` puede rechazar C4 sin recálculo. No afirmar CUD ejecutado.
4. **HC-CENIT-004 (Alta):** R23 mismo día permitido en C2–C5 (`CenitIncomingReturnPolicy.cs:234-266`), frente a descripción en Anexo 2 §3.2 de R23 de días anteriores. Contrastar interpretación normativa con autoridad si se aporta otra edición; no inventar excepción.
5. **HC-CENIT-005 (Alta):** prioridad general `TransactionPriorityPolicy.cs:18-26` y `AchRegulatoryCatalogService.cs:20-27` ignora PPD/CCD/CTX y retorno débito/crédito; `LiquidityOptimizationService.cs:111-117` la consume para C4. Normativa §5.1 tabla 1–10.
6. **HC-CENIT-006 (Alta, NUEVO):** en C4 liquidez **Simulated** = créditos del mismo ciclo (`CenitNettingService.cs:63-80`); `LiquidityOptimizationService.cs:105-120,173-199` decide `Rejected` y `ReturnedByOperator` sin saldo CUD. Es falla de decisión interna estática **distinta** de netting stale HC-003. Corrección mínima: no rechazar definitivamente en operación real sin saldo confirmado y mecanismo de conciliación.

## Reglas C4 y pruebas fuente

- Ciclo 4 15:30–17:15 último de monetarias/prenotes; ciclo 5 no admite entradas ordinarias. `RegulatoryCycleScheduleCatalog.cs:166-176`, `CycleTransactionPolicy.cs:56-74`, `BatchResolver.cs:163-175`.
- Devoluciones C3 ordinarias en C4 con fecha valor igual y secuencia inmediata: `CenitIncomingReturnPolicy.cs:163-173`; R23 requiere revisión HC-004; prenotes §4.8.
- ROR previo día máximo segundo ciclo y ROR mismo día solo quinto: `CenitReturnOfReturnService.cs:226-269`; test/contrato PPD específico.
- Optimización C4/5, si faltan fondos, rechaza pendientes (no difiere C4→C5): `LiquidityOptimizationService.cs:173-199`. No hay recálculo posterior `CenitCycleExecutionService.cs:84-90`; sin evidencia de CUD real.
- **NO CONFUNDIR:** 15 minutos de espera para cubrir fondos eliminados en §§5.2/Reglamento, y **prórroga excepcional autorizada** hasta 15 minutos de §12.1 para problemas de envío de archivos.
- `DXX-LIQ` es código **interno** en `AchCauseCodePolicy.cs:28-32`, no causal STA ni retorno CENIT.
- Archivos examinados I05: `RegulatoryCycleScheduleCatalog.cs`, `CycleTransactionPolicy.cs`, `BatchResolver.cs`, `AchCycleScheduler.cs`, `AchCycleSchedulerHandler.cs`, `CenitOperatingCalendarPolicy.cs`, `CenitCycleQueueService.cs`, `CenitCycleExecutionService.cs`, `CenitNettingService.cs`, `LiquidityOptimizationService.cs`, `TransactionPriorityPolicy.cs`, `AchRegulatoryCatalogService.cs`, `CenitIncomingReturnPolicy.cs`, `IncomingNachaPostParseProcessor.cs`, `CenitReturnOfReturnService.cs`, `CenitChamberResponseService.cs`, `CenitLocalGatewayPickupHostedService.cs`, `AchCauseCodePolicy.cs`, `CycleCalendarGuard.cs`, `NachaChamberFlowPolicy.cs`, `AchTransaction.cs`. Tests fuente `CenitOperationalGovernanceTests.cs`, `CenitReturnOfReturn2026Tests.cs`, `CenitIncomingReturnPolicyTests.cs` inspeccionadas, **NO ejecutadas**.

## Próximo I06: CENIT ciclo 5

Auditar **17:45–18:45**, salida límite **19:15**, solo devoluciones monetarias/no monetarias de C4, ROR de devoluciones C2–C4 de hoy y R23 de días anteriores según §3.2; prohibir créditos/débitos y prenotes ordinarias, distinguiendo recepción fuera de horario de compensación. Verificar optimización quinta (sin espera adicional), estados/ADV, liquidación CUD no demostrada, calendario, idempotencia, extensión excepcional §12.1. Reusar 001–006 sin duplicar. Crear `I06_REGLA_A_REGLA.md`, `I06_MEMORY_STATE.md`, `MANIFIESTO_I06.md`, hashes, ZIP completo.

**Integridad:** verificar `MANIFIESTO_I05.md` y `SHA256_ADICIONES_I05.csv`; se preservan los 4.572 archivos I04, con modificaciones autorizadas únicamente a cuatro documentos de seguimiento y cuatro nuevos I05.
