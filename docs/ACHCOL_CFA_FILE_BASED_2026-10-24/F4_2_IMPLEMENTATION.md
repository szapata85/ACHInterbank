# F4.2 — Prevalidación de colas ACHCOL en el orquestador, sin transmisión CFA

**Programa:** `ACHCOL-NACHA-CFA-EXTCYCLE-001`  
**Fecha:** 2026-10-25  
**Base:** `ACHInterbank_ACHCOL_CFA_F4_1_UNIFICADO.zip`  
**Entrega:** `ACHInterbank_ACHCOL_CFA_F4_2_UNIFICADO.zip`  
**Estado:** `IMPLEMENTED_UNVERIFIED`  
**Próxima microtarea:** F4.3 — errores inciertos, reintentos, trazas y simulación local.

## Alcance ejecutado

El orquestador de postprocesamiento contiene ahora una etapa acotada `IncomingNachaFileBasedPreparationStage` **anterior e independiente** de la selección tradicional `CycleBound`. Se habilita solo mediante `AchCol:AllowCycleIndependentInboundDispatch=true` y nunca invoca SOAP. Selecciona por prioridad/ID, con un límite de 1 a 500 por ejecución, únicamente filas ACHCOL `Blocked` y con código inicial `ACHCOL_DISPATCH_NOT_ENABLED_F33`, sin intentos ni reintentos pendientes.

La etapa resuelve en lote `IncomingNachaFileIngestion`, `IncomingNachaEntryClassification`, `EntryDetail`, `NachaHeader` y `ClearingHouse` sin cargar un `AchCycle` inexistente. Reconstruye y valida el contexto de cada fila, utiliza `IncomingNachaDispatchEligibilityPolicy.EvaluateFileBasedAsync` de F4.1 y recalcula la identidad canónica con `IncomingNachaFileBasedDispatchIdentity.TryBuild`. Rechaza discrepancias de clasificación/ingesta, cámara/fecha o huella de idempotencia.

### Transiciones permitidas: únicamente entre motivos de bloqueo

| Estado inicial | Resultado F4.2 | Estado persistido | Transporte |
| --- | --- | --- | --- |
| `Blocked / ACHCOL_DISPATCH_NOT_ENABLED_F33` | Contexto elegible, idempotencia coherente | `Blocked / ACHCOL_MAPPING_NOT_READY_F42` | **NO** |
| `Blocked / ACHCOL_DISPATCH_NOT_ENABLED_F33` | Contexto faltante o incoherente | `Blocked / ACHCOL_PREFLIGHT_REJECTED_F42` | **NO** |
| `Blocked / ACHCOL_MAPPING_NOT_READY_F42` | Ya inspeccionado | Sin cambio ni evento duplicado | **NO** |
| `Blocked / ACHCOL_PREFLIGHT_REJECTED_F42` | Rechazado | Sin reintento automático | **NO** |
| Feature flag apagado | Cualquier candidato | Sin cambio | **NO** |

Se conserva `NextAttemptAtUtc=null`, `AttemptCount=0` y no se crean `IncomingNachaIntegrationExecution` ni `Queued`/`RetryPending`. La selección de trabajo efectivamente transmitible continúa filtrando exclusivamente `SourceKind=CycleBound`. Un modo SOAP `Live` en la configuración de la ruta tradicional **no levanta ese bloqueo ACHCOL**. F5 definirá las condiciones de autorización de mapeos / envío. F4.3 definirá políticas de respuesta incierta y simulación. La liberación real no está implementada en F4.2.

El resumen de ejecución discrimina prevalidaciones y rechazos ACHCOL sin registrarlos como envíos ni como `Picked` por el despachador. Se conserva el límite de la transacción de ingesta (sin volver a parsear NACHA) y se escriben únicamente cambios de cola y auditoría. No se modifica la configuración del feature flag (apagado por defecto).

## Archivos modificados o añadidos

1. `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/IncomingNachaFileBasedPreparationStage.cs` — etapa nueva con consulta acotada, revalidación, huella y bloqueos F4.2.
2. `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/IncomingNachaPostProcessingPipeline.cs` — invoca preflight antes del worker CycleBound y registra contador/resumen sin enviar ACHCOL.
3. `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/IncomingNachaPostProcessingOrchestrator.cs` — inyección de `IConfiguration` para flag específico ACHCOL, ausente por defecto.
4. `tests/Cfa.ACHInterbank.Tests/IncomingNachaFileBasedOrchestrationF42Tests.cs` — casos `H/008/.8`, `I/009/.9`, `H/008/.10`, `.OUT/.OUT.env`, flag OFF, prenotificación convertida, clave alterada y reejecución sin eventos duplicados; todo sin SOAP.
5. `PLAN.md`, `MEMORY_STATE.md`, `CHATGPT_MICRO_START.md`, `README_PAQUETE_UNIFICADO.md` — estado y decisiones.

**No se añadieron migraciones PostgreSQL/SQL Server; no se modificó el cliente CFA, el mapper ni los formatos NACHA existentes.**

## Decisiones F4.2

- **DEC-052:** Interpretar el segundo pedido textual `F4.1` como continuación de la siguiente microtarea `F4.2`, dado que el estado F4.1 estaba implementado y la conversación exige progreso incremental; no duplicar F4.1.
- **DEC-053:** La preparación ACHCOL es una etapa independiente, ordenada y acotada del worker legado; no ampliar la consulta que envía `CycleBound`.
- **DEC-054:** Revalidar datos persistidos y la huella de idempotencia inmediatamente antes del trabajo futuro; las incongruencias quedan bloqueadas, sin intentos automáticos.
- **DEC-055:** Un candidato válido solo cambia a `Blocked/ACHCOL_MAPPING_NOT_READY_F42`; no generar XML, ejecuciones ni transmisiones Live/DryRun desde F4.2. F5 debe homologar mapping antes de levantarlo.
- **DEC-056:** La idempotencia de la prevalidación proviene de la transición a un motivo de bloqueo definitivo; el job recurrente no debe duplicar eventos en una ejecución secuencial. Concurrencia entre nodos y bloqueo distribuido quedan en deuda F6.

## Verificaciones y limitaciones

- Verificación estática de consultas, filtro de origen, ausencia de SOAP en la nueva etapa, límites del chunk, estados, claves y separación R5D; comparación del ZIP nuevo frente a los archivos heredados y lectura de integridad. El alcance de esas verificaciones se reportará al cerrar el ZIP.
- Nuevas pruebas unitarias xUnit/InMemory escritas, **no ejecutadas**: no existe SDK .NET en el entorno de esta sesión. Sin pruebas reales de PostgreSQL/SQL Server, carga, concurrencia ni endpoint CFA.
- Riesgo pendiente: el semáforo existente es local al proceso; en multinstancia, una carrera de prevalidación puede duplicar auditoría. La fila permanece `Blocked` en cualquier caso. F6 debe verificar coordinación multiworker e índice único de idempotencia; F4.3 abordará reintentos y estados inciertos.
- No se autoriza emitir pagos reales ni certificar E2E/UAT antes de F5/F6 y aprobación operativa.
