# Registro de correcciones — F01

**Fecha:** 2026-11-02. **Origen:** I10 `HALLAZGOS_CONSOLIDADOS_CENIT.md`. **Código y reversibilidad:** `F01_CAMBIOS.patch`, manifest `MANIFIESTO_F01.md`; restauración mediante ZIP base/diff inverso. El reemplazo no afecta normas, WSDL, migraciones, CUD ni rutas oficiales de respuesta.

| ID | Antes: archivo/método | Causa raíz | Cambio mínimo | Evidencia y riesgo | Estado |
|---|---|---|---|---|---|
| HC-CENIT-003 | `CenitNettingService.CalculateAsync` (detalle `IncludedInSettlement=true`); `CenitCycleExecutionService.StartExecutionAsync` | Estimación neta presentada como inclusión efectiva | `IncludedInSettlement=false`, `DecisionReason` simulado y neteo idempotente; Quartz ya no ejecuta neteo por sí mismo | `F01_CAMBIOS.patch`; falta DB real y resultados oficiales | IMPLEMENTADO — PENDIENTE DE VERIFICACIÓN |
| HC-CENIT-005 | `TransactionPriorityPolicy.ResolvePriorityAsync`; `LiquidityOptimizationService.OptimizeCycleCoreAsync` | Prioridad solo por tipo genérico y efecto financiero automático | Orden informativo SEC y signo de devolución según Anexo 2 §5.1 (10 niveles); si falta SEC/código, prioridad no determinada `0`; ningún efecto de simulación se aplica al ciclo | 13 casos C# propuestos sin ejecutar; vigencia de SEC/códigos reales a contrastar | IMPLEMENTADO — PENDIENTE DE VERIFICACIÓN |
| HC-CENIT-006 | `LiquidityOptimizationService.OptimizeCycleCoreAsync` (C4/C5 `ReturnedByOperator`/`DXX-LIQ`); `AchCycleSchedulerHandler`→`CenitCycleExecutionService` | Liquidez `Simulated` determinaba rechazo real | Proyección sin escritura en `AchTransaction`, lote, despacho, cola o eventos; decisiones `Simulated*` sin rechazo oficial; la ruta Quartz únicamente espera salida oficial | Script de comprobación estática 12/12; falta prueba runtime de la ruta | IMPLEMENTADO — PENDIENTE DE VERIFICACIÓN |

Archivos productivos: `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/{CenitCycleExecutionService,CenitNettingService,LiquidityOptimizationService,TransactionPriorityPolicy,AchPendingCycleEligibilityService}.cs`. El quinto solo impide confundir evidencia `Simulated*` nueva con procesamiento oficial y conserva los marcadores históricos. Las modificaciones mantienen intactos `CenitChamberResponseService`, jobs y contratos ACH Colombia.

**Decisiones:** (i) evitar incluir el simulador en el flujo Quartz, (ii) mantener simulación explícita para consulta y nunca como fuente de rechazo/cambio de ciclo, (iii) no consumir entradas encoladas en F01 hasta resolver incorporación/atomicidad F03; permanecen `Queued`, (iv) no corregir retroactivamente datos históricos con valor `IncludedInSettlement=true` sin reconciliación, (v) conservar API original de consulta, (vi) no desarrollar CUD. Antes de F03 comprobar que `AwaitingOfficialResult` no cree estados de cierre incoherentes y permitir avance del siguiente ciclo sin procesar doble.

**Fuente normativa:** DSP-152 (27-feb-2025) artículos 18–19; Anexo 2 §5.1–5.2; más manual NACHA-M CENIT 07-may-2026 para campos SEC. Ver `../auditoria-estatica-cenit/FUENTES_NORMATIVAS_Y_DIFERENCIAS.md`. **Pendientes normativos:** vigencia externa de versión NACHA-M y reglas ambiguas DN-CEN-007/008, sin cambios de F01.


## F02 — 2026-11-02 (base F01, SHA-256 `8beee25554db99142677d6c084cb08cb9cf864967e864cbc61dd6c72459084e1`)

| ID | Causa/implementación anterior | Fuente | Cambio concreto y rutas | Pruebas / estado | Pendiente / regresión |
|---|---|---|---|---|---|
| HC-CENIT-013 | `CenitChamberResponseService.ImportItemAsync`, F01 ~líneas 188–285: correlación de FileNack individual resuelve `AchTransactionId`, pero cambia `AchFileExport.ChamberResponseState` global | NACHA-M 07-may-2026 §7.3.2; DSP-152 Anexo 2 §3.3 | `CenitChamberResponseService.cs` asigna estado `ReturnedByOperator` solo a `AchTransaction` correlacionada por `TraceNo` oficial, crea `AchTransactionStateEvent` con fuente Operator/código/payload, evita duplicado y no modifica estado agregado para NACK individual; ACK y NACK de archivo siguen independientes | F02-T01–05; 0 xUnit ejecutadas; 13/13 controles estáticos PASS; **IMPLEMENTADO — PENDIENTE DE VERIFICACIÓN** | Integración DB, eventos terceros/prenotificación, concurrencia multi-worker y fixtures oficiales; ACH Colombia: archivos originales inmutados, regresión funcional no ejecutada |
| HC-CENIT-012 | `CenitChamberResponseService.Parse`, F01 ~líneas 346–420: sólo XML/conciliación genérica/actividad; no interpreta clase 280/ADV | NACHA-M CENIT 07-may-2026 §7.3.3 y Anexo 2 tablas 7,8 | `CenitChamberResponseService.cs` interpreta NACHA-M de 106 posiciones, SEC ADV, clase 280, códigos 81–88, fecha valor, lote, referencia/importe, cuenta de control, correlación por ciclo en `CenitChamberResponses` existente; acepta `ADV` o sobre de `Reconciliation` físico; no cambia transacciones, no contabiliza ni toca CUD | F02-T06–10; 0 xUnit ejecutadas; 13/13 controles estáticos PASS; **IMPLEMENTADO — PENDIENTE DE VERIFICACIÓN** | Contrastar archivo de aviso real con operador, valores netos y evidencia Tesorería; validación NACHA exhaustiva formal queda en F06; ACH Colombia: código original inmutado |

**Archivos productivos (1):** `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/CenitChamberResponseService.cs`. **Pruebas fuente (1):** `tests/Cfa.ACHInterbank.Tests/CenitChamberResponseLifecycleTests.cs`. **Script nuevo (1):** `scripts/test/cenit_f02_static_check.py`. **Reversión:** aplicar `F02_CAMBIOS.patch` en reversa sobre árbol F02 limpio con `patch -R -p1`, restaurar documentos F01 desde ZIP de partida y eliminar archivos nuevos F02, o extraer ZIP F01 directamente. La aplicación del parche y pruebas C# requiere CI. No se modificó ningún contrato ni archivo de ACH Colombia.


## F03 — 2026-11-02 (base F02 SHA-256 `361d8d4a8e3d14313283a50155cfbc9633c37f5bd1f69e5755f09e68720e31a4`)

| ID | Origen / causa | Fuente | Cambio / evidencia | Estado y pendientes |
|---|---|---|---|---|
| HC-CENIT-001 | `CenitCycleQueueService.EnqueueAsync`, `CenitCycleExecutionService.StartExecutionAsync`, `NachaDataLoader.LoadByCycleAsync`: la cola guardaba destino pero la transacción seguía perteneciendo a otro ciclo y la salida no la seleccionaba | DSP-152 Anexo 2 2025 §3.3 | `ICenitCycleQueueService.IncorporateForCycleAsync` incorpora antes de exportar NACHA-M con ciclo/lote/evento/totales/cola en una transacción SERIALIZABLE; 4 archivos productivos localizados; `F03_CAMBIOS.patch`, 8 xUnit fuente, 17/17 estáticos | IMPLEMENTADO — PENDIENTE DE VERIFICACIÓN. Fecha valor interdiaria se bloquea (F04); compilación/SQL concurrente/exportación CENIT y regresión ACH pendientes. No se toca CUD. |

**Archivo original, clase y método:** `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/CenitCycleQueueService.cs`, `CenitCycleQueueService.EnqueueAsync`, `CenitCycleExecutionService.StartExecutionAsync` sin cambio, `NachaFileGenerationCoordinator.BuildNachaFilesByCycleAsync`, `NachaDataLoader.LoadByCycleAsync` sin modificación. Línea original: ver HC-001 I10 en `../auditoria-estatica-cenit/HALLAZGOS_CONSOLIDADOS_CENIT.md` (§ rutas 21–85). Prueba fuente: `tests/Cfa.ACHInterbank.Tests/CenitCycleQueueIncorporationF03Tests.cs` (8 casos; 0 ejecutados). Contratos WSDL, ACH Colombia, CUD y reglas de devolución inalterados. Restauración en `F03_CAMBIOS.patch` o ZIP F02.


## F04 — 2026-11-02 (base F03 SHA-256 `60f8061775ab19a76eac1c3d69061ed036976fe85b1976fedd4e79e5c47abcb1`)

| ID | Causa raíz / código previo | Norma | Cambio productivo / diff | Pruebas fuente y estado | Pendiente / ACH Colombia |
|---|---|---|---|---|---|
| HC-CENIT-004 | `CenitIncomingReturnPolicy.EvaluateR23` F03 241–266 admitía mismo día en cualquier C2–C5 | Anexo 2 2025 §§3.2/4.2, Anexo A R23 | `CenitIncomingReturnPolicy.cs`: mismo día solo en ciclo original + 1; conserva excepción días anteriores C1–C5 | `CenitF04ReturnPolicyTests.cs` 6 combinaciones; **IMPLEMENTADO — PENDIENTE DE VERIFICACIÓN** | PT-07: confirmar muestra operador y potencial discrepancia; ACH Colombia sin modificación propia, regresión funcional pendiente |
| HC-CENIT-007 | `EvaluateR06` F03 188–231 aceptaba C5 desde C1–C3 con indicador de inmediatez | Anexo 2 2025 §3.2; Anexo A R06 | `CenitIncomingReturnPolicy.cs`: C5 exige C4 mismo día ANTES de la excepción R06; resto evidencia R06 intacta | 6 escenarios nuevos C5; **IMPLEMENTADO — PENDIENTE DE VERIFICACIÓN** | PT-09 y DB/gateway reales; ACH Colombia pendiente |
| HC-CENIT-009 | `EvaluateR23` F03 268–275 confiaba en `ReceiverRejectionDeadlineDate` suministrado | Anexo 2 2025 §4.2 | DTO: fecha notificación separada, fecha de recepción original y límite calculado; `AchReturnEligibilityPolicyComponent` + `AchReturnsService` y `IncomingNachaPostParseProcessor` usan `IOperationalCalendarService` y `AchCycle.ProcessingDate`; policy exige 15 días + día hábil calculado | 10 escenarios adicionales y 2 casos de ruta con Mock; **IMPLEMENTADO — PENDIENTE DE VERIFICACIÓN** | PT-08: notificación/recepción verificable, festivos, ejecución real .NET/DB; ACH Colombia sin rutas afectadas intencionalmente |

**Archivos exactos modificados:** ver F04_INFORME_CORRECCIONES.md y `F04_CAMBIOS.patch`. **Evidencia:** F04 21/21 controles estáticos PASS y diff reversible; 22 escenarios xUnit nuevos NO ejecutados. **Datos extra:** no se cambian estados definitivos por simulación (F01), respuesta NACK/ADV (F02), colas (F03), migraciones, WSDL, ACH Colombia ni CUD.


## F05 — 2026-11-02 (base ZIP F04)

| Hallazgo | Causa raíz / archivos y métodos originales | Fuente | Corrección en código real | Estado / pruebas / ACH Colombia |
|---|---|---|---|---|
| HC-CENIT-002 | `CenitReturnOfReturnService.CreateOutgoingAsync` no guardaba source Return, y `AchReturnOfReturnFileGenerationService.ValidateCenitCycle1ReturnOfReturnExceptionAsync` hacía fallback a `OriginalTransaction` | DSP-152 Anexo 2 2025 §§3.2, 4.3, 4.6 | Correlación por evento + raw + adenda 99 + ciclo + traza + importe. FK `SourceReturnTransactionId` solo si existe transacción Return única ya persistida. No crea entidades ni asientos. `F05_CAMBIOS.patch` | IMPLEMENTADO — PENDIENTE DE VERIFICACIÓN; pruebas fuente en dos clases existentes, xUnit 0 ejecutadas; ACH Colombia sin cambios específicos |
| HC-CENIT-008 | Seeder permite R74 para cualquier causal; validación ROR genérica sin R32/crédito/adenda original | Anexo A 2023 R74, NACHA-M 2026 §4.4.2 | Catálogo CENIT R74 solo para padre R32, validación repetida de crédito monetario y campo 3 adenda 05 para ingreso/salida/generación, sin usar adenda 99 | IMPLEMENTADO — PENDIENTE DE VERIFICACIÓN; pruebas fuente R74 + seeder, xUnit 0 ejecutadas; catálogo ACH Colombia intacto |

**Archivos fuente modificados: 3 productivos + 3 pruebas existentes**; script estático nuevo F05, documentos/manifiesto y patch reversibles. Sin migraciones, WSDL ni CUD. Rutas/archivos exactos en `F05_INFORME_CORRECCIONES.md` y `MANIFIESTO_F05.md`. **Pendiente adicional:** validar muestras oficiales y correlación raw histórica; no se cierran hallazgos.


## F06 — 2026-11-02 (base F05)

| Hallazgo | Causa raíz / antes | Fuente | Corrección localizada y evidencia | Pruebas y estado |
|---|---|---|---|---|
| HC-CENIT-010 | `NachaPhysicalParser.IsAcceptedByte` admitía Ñ/ñ para CENIT; `NachaParserService` no pasaba identidad | NACHA-M CENIT 2026 §4.1 / tabla 3 `ERR_INV_PATTERN` | Perfil publicado/ordinario/Return/ROR controla alfabeto byte por cámara, falla física sin afectar ACH Colombia; 3 fuentes productivas compartidas, `F06_CAMBIOS.patch` | 12 casos xUnit fuente + estáticos 24/24; IMPLEMENTADO — PENDIENTE DE VERIFICACIÓN, PT-11, .NET y operador |
| HC-CENIT-011 | `NachaInboundValidator.ThrowRegulatory` consumía Dxx STA y su descripción para errores de conteo/hash/estructura NACHA | Anexo B STA 2023 vs NACHA-M CENIT 2026 §§4.3/7.3.2 y Anexo 2 | `SelectNachaErrorDomain` en ruta de parser y DB, clasificación local `CENIT-NACHA-FATAL`, salida formal por entrada intacta; catálogo Dxx STA preservado | 13 casos xUnit fuente + estáticos 24/24; IMPLEMENTADO — PENDIENTE DE VERIFICACIÓN, PT-12 y comparación STA real |

**Archivos originales exactos:** los 3 productivos y 2 pruebas se enumeran en `F06_INFORME_CORRECCIONES.md`; diff reversible `F06_CAMBIOS.patch`, hashes `F06_COMPARACION_SHA256.tsv`. Fuente de auditoría: `../auditoria-estatica-cenit/HALLAZGOS_CONSOLIDADOS_CENIT.md` §§HC-CENIT-010/011. ACH Colombia: ninguna ruta propia alterada, regresión dinámica pendiente. CUD: fuera de alcance.


## F07 — 2026-11-02 (base ZIP F06)

**Objetivo transversal:** comprobar conjuntamente correcciones HC-CENIT-001–013 y aislar ACH Colombia, sin confundir pruebas de fuente con homologación. **No se modifican servicios productivos** por no identificar un defecto nuevo reproducido con la infraestructura disponible. Se incorpora `tests/Cfa.ACHInterbank.Tests/CenitF07CrossPhaseRegressionTests.cs` y se amplía `CenitChamberResponseLifecycleTests.cs` para cubrir ACK/NACK/ADV en ambos órdenes. Las comprobaciones F07 y el runner xUnit están en `scripts/test/`. 24/24 fuente/fixtures y 113/113 estáticos anteriores aprobados. 11 casos xUnit agregados sin ejecutar. **13 hallazgos implementados pendientes de verificación; 0 cerrados.** Referencias de entrega: `F07_INFORME_REGRESION.md`, `F07_EVIDENCIA_REGRESION_ESTATICA.txt`, `F07_CAMBIOS.patch`, `MANIFIESTO_F07.md`. Sin cambios en ACH Colombia, CUD, WSDL, base ni migraciones.


## F08 — 2026-11-02 (base ZIP F07, SHA-256 `86e4ea5ccf30b1155fbc3a9477889bc611b499d326210b2f042fe5bd99b2c5f0`)

**Cierre documental condicionado:** se consolidó la implementación de HC-CENIT-001–013 sin añadir ni alterar código productivo, WSDL, migraciones, pruebas C#, golden ACH Colombia ni procesos CUD. Se añadieron la matriz de decisiones F08, los 22 PT pendientes con responsables/evidencia, checklist GO/NO-GO y dictamen final condicionado. Se corrigió solo a nivel documental la discrepancia entre mención previa SDK 10.0.402 y el valor real de `global.json` **10.0.403**. Ver `F08_INFORME_ENTREGA.md`, `F08_DICTAMEN_FINAL_CONDICIONADO.md`, `F08_MATRIZ_CIERRE_HALLAZGOS.md`, `F08_PLAN_PRUEBAS_Y_APROBACIONES.md` y `F08_CAMBIOS.patch`.

**Evidencias ejecutadas**: 137/137 estáticas históricas + 17/17 F08 documentales (no son pruebas .NET). Runner .NET no ejecuta por ausencia SDK, exit 2. **13 HC ABIERTOS PENDIENTES DE VERIFICACIÓN, 0 cerrados; 22 PT NO EJECUTADOS.** Producción/homologación NO-GO; revalidar con suite, SQL/Quartz/operador/Tesorería y actas posteriores.
