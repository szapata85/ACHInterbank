# MEMORY_STATE — auditoría estática incremental CENIT

**LEER PRIMERO PARA CONTINUAR. Corte 2026-11-01.** Último ZIP completo `ACHInterbank_PROYECTO_COMPLETO_AUDITORIA_CENIT_I03_2026-11-01.zip`, raíz `ACHInterbank_ajustado/`. Usar **I03**, no reconstruir desde I01/I02. No alterar implementación ACH Colombia ni código productivo.

## Estado general al cerrar I03

- **I01 (inventario), I02 (ciclo 1), I03 (ciclo 2) COMPLETADOS**; I04–I10 pendientes: **3/10 = 30 % de hitos**, NO cumplimiento regulatorio.
- **I03: 28 reglas**: **3 cumple estáticamente en ruta delimitada**, **18 parcial**, **3 incumplimiento estático demostrado**, **4 no demostrable**. I02: 24 (2/14/3/5); I01: 12 preliminares (8 parcial, 4 no demostrable), no sumar denominadores entre distintos alcances para certificar.
- **Hallazgos I03:** nuevos **1**, acumulados **4**, abiertos **4**, cerrados **0**. `HC-CENIT-001` cola fuera de ventana (I02, ampliado C2); `HC-CENIT-002` ROR C1 sin fuente Return (I02, **no extrapolar** a C2); `HC-CENIT-003` netting previo a diferimiento (I02, ampliado C2); **`HC-CENIT-004` R23 mismo día admitido en C2–C5 aunque Anexo 2 §3.2 describe R23 de días anteriores**. Registro completo: `REGISTRO_HALLAZGOS.md`.
- Fuente operativa: DSP-152 Anexo 2 27-feb-2025, §3.1–3.3, §4.1–4.8, §5.2; Anexo A 28-nov-2023; Manual CENIT-WEB 13-jun-2025 §3.2; NACHA-M adjunto 07-may-2026 (vigencia externa no confirmada). Clasificaciones documentales DN-CEN-001…006 siguen en `FUENTES_NORMATIVAS_Y_DIFERENCIAS.md`. STA no es compensación/retorno.
- **Pruebas ejecutadas:** ninguna (.NET/BD/Quartz/CUD/MFT/cámara/certificación). Leído código y tests; no declarar tests aprobados. Código/configuración/tests y documentos anteriores a I03 sin cambios; integridad ver `MANIFIESTO_I03.md` y `SHA256_ADICIONES_I03.csv`.

## Hallazgos: decisiones y evidencias reutilizables

1. **HC-CENIT-001:** `BatchResolver.cs:248-251` → `CenitCycleQueueService.cs:21-47` registra Target pero no reasigna `AchCycleId`; `CenitCycleExecutionService.cs:71-85` consume antes de `CenitNettingService.cs:50-53`, que no ve transacciones del ciclo previo. Afecta C1 tardío→C2 y C2 tardío→C3. No confundir con diferimiento por liquidez, que reasigna lote/ciclo en `LiquidityOptimizationService.cs:139-170`.
2. **HC-CENIT-002:** `CenitReturnOfReturnService.cs:106-115` no llena `SourceReturnTransactionId`, y guard contextual C1 en `AchReturnOfReturnFileGenerationService.cs:444-462` requiere fuente Return del día hábil anterior. La ruta C2 usa padre NACHA (`:479-520`) y no está demostrado el mismo defecto; no duplicar.
3. **HC-CENIT-003:** `CenitCycleExecutionService.cs:84-85` llama netting antes de optimización; `CenitNettingService.cs:50-105` incluye y marca todas como liquidables, `LiquidityOptimizationService.cs:135-170` puede diferir a C3 sin ajustar. Liquidez es simulada, no probar perjuicio CUD.
4. **HC-CENIT-004:** `CenitIncomingReturnPolicy.cs:137-160` restringe R23 previo día solo en C1, mientras `:263-266` admite R23 mismo día en C2–C5. La ruta real de Return In y Out invoca policy (`IncomingNachaPostParseProcessor.cs:444-456,581-598`, `AchReturnEligibilityPolicyComponent.cs:95-120`). `CenitIncomingReturnPolicyTests.cs:140-176` espera esa admisión (NO ejecutado). Norma Anexo 2 §3.2 describe «Devoluciones de días anteriores (R23)»; acción mínima: comprobar antecedente de día anterior en todas las sesiones y revisar fuente si se argumenta interpretación excepcional intradía.

## Archivos inspeccionados en I03

`I03_REGLA_A_REGLA.md` contiene **28 filas** con fuente/condición/ciclo/clase/método/rango/evidencia/acción. Inspeccionados: `RegulatoryCycleScheduleCatalog.cs`, `CycleTransactionPolicy.cs`, `AchCycleScheduler.cs`, `AchCycleSchedulerHandler.cs`, `CenitOperatingCalendarPolicy.cs`, `BatchResolver.cs`, `CenitCycleQueueService.cs`, `CenitCycleExecutionService.cs`, `CenitNettingService.cs`, `LiquidityOptimizationService.cs`, `CenitIncomingReturnPolicy.cs`, `IncomingNachaPostParseProcessor.cs`, `AchReturnsService.cs`, `AchReturnOriginalCorrelationComponent.cs`, `AchReturnEligibilityPolicyComponent.cs`, `CenitReturnOfReturnService.cs`, `AchReturnOfReturnFileGenerationService.cs`, `OperationalCalendarService.cs`, `NachaTransactionValidationService.cs`; pruebas `CenitIncomingReturnPolicyTests.cs`, `CenitReturnOfReturn2026Tests.cs`, `CenitOperationalGovernanceTests.cs` (lectura, no ejecución); fuentes normativas locales Anexo 2/Anexo A/Manual NACHA y CENIT-WEB. WSDL existentes preservados, no certificados como CUD.

## I04 — siguiente incremento

Auditar **CENIT ciclo 3, 13:30–15:00**, especialmente devoluciones C2, prenotes, R23 día anterior, ROR del día hábil anterior **fuera de plazo desde C3**, ROR same-day diferido hasta C5, cola C2→C3, fecha valor, netting y diferimientos C3→C4, archivos antes de C4, seguridad/idempotencia. Ampliar hallazgos 001–004 solo si misma causa; registrar nuevos exclusivamente al demostrar distinta raíz. Generar `I04_REGLA_A_REGLA.md`, fotografía `I04_MEMORY_STATE.md`, manifiesto/hashes e íntegro ZIP. No modificar código; no tests dinámicos si herramientas no disponibles.
