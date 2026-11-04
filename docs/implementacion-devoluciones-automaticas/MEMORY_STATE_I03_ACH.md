# MEMORY_STATE — DEV-AUTO — I03-ACH

**Corte:** 2026-11-02. **Línea base utilizada:** ZIP completo I02-ACH. **Último incremento:** I03-ACH en fuente; ejecución no homologada. **Base siguiente:** `ACHInterbank_COMPLETO_DEVOLUCIONES_AUTOMATICAS_I03_ACHCOLOMBIA_2026-11-02.zip`. **Siguiente:** I04-ACH. **CENIT:** independiente, no iniciado. **Producción:** NO-GO.

## Invariantes

1. Usar siempre ZIP completo de I03 para I04, no archivos aislados. Preservar `MEMORY.md` y `MEMORY_STATE.md` raíz y memorias históricas. La memoria de iniciativa es este archivo junto a `MEMORY_STATE.md` en `docs/implementacion-devoluciones-automaticas/`.
2. No mezclar reglas de ACH Colombia con CENIT. Cambio funcional I03 únicamente para `ACHCOL`; nueva tabla y DbSet son infraestructura, sin ruta CENIT activa. SPA intacta.
3. I01 configura `DESACTIVADO`, `CON_APROBACION`, `AUTOMATICO` por cámara. API ACH Colombia protegida; `ProcessingConnected=false`. I02 observa rechazos funcionales reales Live `Proc_Transacciones` y registra evento solo si llega a receptor; no generar retorno por preflight ni fallo técnico.
4. I03 añade reserva durable `AchColombiaReturnPreparations`, con índice único `OriginalTransactionId` e índice único `SourceEventId` SQL Server. Solo eventos `AchColApplicationFailureDetected` corroborados con ejecución real y causal Rxx de catálogos ACH. No prepara con `DESACTIVADO`.
5. I03 se ejecuta después de persistir el resultado core en `IncomingNachaPostProcessingPipeline` y se aísla del resultado financiero ante excepciones. Nunca llama generador NACHA-M ni envío.
6. Ventanas ACH: cuatro ciclos posteriores, calculados por número de ciclo persistido; para siete ciclos confirmados aplicar `AchColombiaSevenCycleCandidate.ReturnWindows`. No ordenar por `StartTime` porque ciclo 1 empieza la noche previa. Fallas de agenda o catálogo => no reservar.
7. Crédito tardío comprobado => `REVISION_TARDIA_CREDITO` sin ciclo sugerido ni envío. Débito y prenotificación tardíos => no preparar. Revalidar hora y elegibilidad antes de aprobación/generación.
8. No afirmar idempotencia transversal entre reserva y generación manual: el generador actual conserva su índice `UX_AchReturnGenerated_OriginalTransaction`; la conciliación de candidatos y manuales queda en I04.
9. SQL Server cuenta con migración I03 y snapshot. Las migraciones PG para configuración I01 / preparación I03 están pendientes; ningún GO de despliegue multiplataforma sin cerrar esta brecha.
10. Falta de SDK, SQL o ambiente de homologación **no bloquea implementación segura**, pero pruebas no ejecutadas no pueden marcarse PASADAS.

## Código y evidencias

- `AchColombiaReturnPreparation.cs`, `AchColombiaReturnPreparationConfiguration.cs`, `AchColombiaReturnPreparationService.cs`, `AchDbContext.cs`, `IncomingNachaPostProcessingPipeline.cs`.
- `20261102230000_AchColombiaReturnPreparationI03.cs` y `AchDbContextModelSnapshot.cs` SQL Server.
- `AchColombiaReturnPreparationI03Tests.cs` y `automatic_returns_i03_ach_static_check.py`.
- Informe `INFORME_I03_ACH.md`, evidencia `EVIDENCIA_I03_ACH.md`, manifiesto `MANIFIESTO_I03_ACH.json`.

## Pruebas y puertas

- Grafo 7/7: PASS. Invariantes I02 18/18: PASS_STATIC. Invariantes I03 25/25: PASS_STATIC.
- .NET compilación/xUnit: NOT_RUN_NO_SDK; SQL Server migración y concurrencia: NOT_RUN; UAT y homologación de ciclos: PENDING; Angular: no tocado.
- I00 documental terminado; I01-ACH, I02-ACH, **I03-ACH incorporados en código**, pruebas runtime pendientes. CENIT I01–I06 no iniciados. 

## Punto exacto de continuación

**I04-ACH**: consultar candidatas con permisos, aprobación/rechazo manual, verificar de nuevo causal, vencimiento y si otra devolución ya fue generada manualmente; SPA Angular Material, UX simple y texto español. No habilitar envío real. Parada en I03-ACH, ZIP completo firmado con SHA-256 en entrega externa.
