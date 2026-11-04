# I01_MEMORY_STATE — fotografía inmutable — auditoría estática incremental CENIT

**LEER PRIMERO EN EL PRÓXIMO CHAT.** Corte: **2026-11-01**. Punto de partida: `ACHInterbank_PROYECTO_COMPLETO_AUDITORIA_ACHCOL_I10_2026-11-01.zip`; la copia resultante está íntegra y preserva ACH Colombia I01–I10.

## Estado general

- **Último incremento realmente completado:** **I01 Inventario y plan (COMPLETADO)**.
- **Avance de HITOS:** **1/10 = 10 %**; siguientes I02–I10 = **9 incrementos**. Cumplimiento regulatorio certificado **NO medido**.
- **Reglas preliminares examinadas:** **12** (`I01_REGLA_A_REGLA.md`): **0 cumplen integralmente**, **8 parciales**, **0 incumplimientos estáticos demostrados**, **4 no demostrables**. No confundir “parcial” con veredicto de ciclo.
- **Hallazgos demostrados:** **0 nuevos; 0 acumulados; 0 abiertos; 0 cerrados**. **Discrepancias documentales:** DN-CEN-001…006, clasificadas aparte.
- **Pruebas dinámicas/banco:** **NO_EJECUTADAS**; .NET/DB/Quartz/SFTP-MFT/CUD/homologación no aprobados ni reprobados en I01.
- **Modificación productiva:** NINGUNA; todos los 4.541 archivos del ZIP de entrada comparados byte a byte vía SHA-256; 0 cambios en código/config/tests/docs previos.

## Lecturas obligatorias antes de I02

1. Este archivo; luego `PLAN_INCREMENTAL.md`, `ESTADO_INCREMENTOS.md`, `REGISTRO_HALLAZGOS.md`.
2. `FUENTES_NORMATIVAS_Y_DIFERENCIAS.md` (fechas de fuente y matiz Anexo A R60–R74); `I01_INVENTARIO_ARQUITECTURA.md` y `I01_REGLA_A_REGLA.md`.
3. `MANIFIESTO_I01.md`, `SHA256_LINEA_BASE_ACHCOL_I10.csv` para integridad. No editar históricos de ACH Colombia.

## Decisiones técnicas vinculantes

- Auditar el runtime y sus llamadas. Una tabla CENIT, un seeder, un DTO o un test aislado no prueban cumplimiento.
- DSP-152/Anexo 2 **27-feb-2025** para sesiones, orden/liquidación y regla 4/5; formato NACHA-M **07-may-2026 adjunto**, vigencia externa no verificada; anexos A y B **28-nov-2023** según portada. El nombre `ceos_dsp_152_MAY_27_2022.md` **no** acredita edición íntegramente 2022.
- CENIT 5 ciclos: 07:30–10:30, 11:00–13:00, 13:30–15:00, 15:30–17:15, 17:45–18:45; quinto con salida límite 19:15. No transportar las 7 sesiones ACH Colombia.
- C1 y C5 NO admiten todo; R23 excepción; ROR **R60–R74** incluidos Anexo A (no solo R60–69). Diferenciar STA Dxx y otros rechazos de cámara/transacción.
- **Nunca imponer veto global horario**: descomponer preparación, recepción, envío automático/manual, encolamiento, compensación y reproceso. En 1–3 insuficiencia → trasladar pendientes; 4–5 optimizar/rechazar según §5.2 del Anexo 2 de 2025, sin 15 minutos adicionales para fondeo.
- No inventar asientos CUD, resultados de pruebas ni vigencia definitiva de documentos no confirmados; el WSDL CFA/WSAxon NO equivale a contrato de CENIT.

## Código y pruebas identificados

Rutas y lista de archivos en `I01_INVENTARIO_ARQUITECTURA.md`. Entrada/retorno: `IncomingNachaPostParseProcessor`, `AchReturnsService`, `CenitIncomingReturnPolicy`; ROR: `CenitReturnOfReturnService`, `CenitReturnOfReturn2026Layout`; scheduler/calendario: `AchCycleSchedulerHandler`, `CenitOperatingCalendarPolicy`, `CenitCycleQueueService`; liquidación: `CenitCycleExecutionService`, `CenitNettingService` y `ILiquidityOptimizationService`; salida/perfiles: `NachaFileBuilder`, `NachaOfficialAssemblyPipeline`; respuesta: `CenitChamberResponseService`; SQL Server/Postgres migraciones. Tests con nombre CENIT inventariados, no ejecutados.

## Siguiente incremento — I02

Auditar **CENIT ciclo 1**, 07:30–10:30 y transición a ciclo 2: apertura, recepción, cutoff, salida, operaciones permitidas, prenotes, **R23 y ROR del día hábil anterior**, exclusión de devoluciones ordinarias, cola fuera de ventana, fecha valor/día hábil Bogotá, NACHA/adendas/rechazos, netting/optimización/estados. Reconstruir rutas concretas hasta persistencia y rechazos; generar una entrada por regla, consolidar hallazgos por causa raíz, mantener los SHA de archivos anteriores y construir ZIP entero I02.

**Estado final I01:** completado por presencia de documentos y ZIP verificado (ver `MANIFIESTO_I01.md`).
