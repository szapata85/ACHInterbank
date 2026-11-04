# Registro de pruebas F01

## Pruebas fuente C# (no ejecutadas)

Proyecto existente `tests/Cfa.ACHInterbank.Tests/CenitOperationalGovernanceTests.cs`:

- **F01-T01** insuficiencia simulada ciclo 2 no cambia ciclo, lote, despacho, cola, estado ni evento; repetir sin duplicar decisión (modificada).
- **F01-T02** fallo en escritura de decisiones provoca rollback de transacción y no cambia registros (preexistente, no modificada).
- **F01-T03 / T04** insuficiencia simulada C4/C5 no crea `ReturnedByOperator` ni causal `DXX-LIQ` (modificada, dos casos).
- **F01-T05** ruta `CenitCycleExecutionService` con cola pendiente, reintento y espera oficial, no consume ni crea neteo/optimización (nueva).
- **F01-T06** cámara distinta de CENIT no ejecuta esa ruta (nueva).
- **F01-T07** neteo informativo no registra `IncludedInSettlement=true` y evita duplicado por ejecución (nueva).
- **F01-T08…T20** 13 combinaciones de prioridad SEC, crédito/débito/devolución y casos no determinables (nueva teoría).

**Referencia matriz I10:** `PT-04`, `PT-05`, `PT-06`; siguen PENDIENTES hasta ejecución real de DB/Quartz y contraste normativo. `PT-20` regresión ACH Colombia también pendiente.

## Ejecutadas en ambiente local

`python scripts/test/cenit_f01_static_check.py` → **12/12 PASS**. Son únicamente condiciones de fuente (presencia/ausencia de llamadas peligrosas y casos de prueba), NO evidencia de que el servicio compile, corra, opere con DB/Quartz ni procese correctamente respuestas reales.

## No ejecutables aquí

- `dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj --filter FullyQualifiedName~CenitOperationalGovernanceTests` — SDK .NET ausente.
- Integración SQL Server/PostgreSQL, Quartz en ejecución real, transporte CENIT, ACK/NACK/ADV y Tesorería — sin infraestructura ni muestras oficiales.
- Regresión ACH Colombia — no ejecutada; comparación de integridad del código **sí** realizada, no equivale a regresión funcional.

Para ejecutar en CI: restaurar dependencias, compilar solución, ejecutar suite xUnit (incluida priorización), fixtures SQLite, después SQL transaccional y Quartz bajo reloj controlado; revisar `AchTransaction`, `AchTransactionStateEvents`, `CenitNettingDetails`, `CenitCycleQueues`, `LiquidityOptimizationDecisions` y salida de `CenitChamberResponseService`. Registrar logs, IDs, DB antes/después y archivos originales del operador. **Resultado actual de .NET: PENDIENTE, no APROBADO.**

**Integridad de parche (ejecutada):** `patch --dry-run -p1` sobre seis rutas originales desde ZIP I10: 6/6 PASS, 0 conflictos. No equivale a una prueba xUnit.


## Registro F02 — 2026-11-02

**Fuente C# incorporada, NO ejecutada:** `CenitChamberResponseLifecycleTests.cs`: 7 `[Fact]` y 1 `[Theory]` con dos datos nuevos (9 casos propuestos), más ajustes a 3 pruebas previas de OPERATOR/múltiples NACK/rechazo de archivo. Incluye:

- F02-T01 NACK individual con dos operaciones: modifica únicamente la afectada; archivo permanece Pending, evento único, reintento y ACK posterior.
- F02-T02 NACK individual después de FileAck técnico sigue actualizando solo operación.
- F02-T03 BatchNo sin TraceNo: no se imputa una transacción arbitraria.
- F02-T04 TraceNo oficial inexistente y metadato caller divergente: fail-closed, estado intacto.
- F02-T05 NACK múltiples correlacionados: eventos separados y estado agregado intacto (test existente ampliado).
- F02-T06 ADV clase 280/SEC ADV, códigos 81/82: 2 registros, importes COP, correlación ciclo, sin eventos/asientos, reintento idempotente.
- F02-T07 ADV con sobre histórico Reconciliation: interpreta el contenido 280/ADV.
- F02-T08 ADV con SEC o clase erróneos: 2 casos negativos, fail-closed.
- F02-T09 ADV con total de control alterado: fail-closed.
- F02-T10 ADV sin ciclo verificable: evidencia no aplicada, sin simulación de liquidación.

**Ejecución real de comprobaciones de fuente:** `python scripts/test/cenit_f02_static_check.py` -> **13/13 PASS**. Reejecución `python scripts/test/cenit_f01_static_check.py` -> **12/12 PASS**. Estas verificaciones no prueban compilación, idempotencia transaccional en base real ni aceptación de CENIT. **xUnit ejecutadas 0, aprobadas 0, fallidas 0**; .NET 10 SDK no instalado en el contenedor. El fixture ADV es sintético y NO se presenta como muestra oficial.

**Comando pendiente en CI:** `dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj --filter FullyQualifiedName~CenitChamberResponseLifecycleTests` (previa restauración); luego SQL Server/PostgreSQL: prueba de commit atómico, dos receptores concurrentes, colisiones de idempotencia y prenotificaciones. **PT-13** exige ADV real, importes/referencias y contraste con Tesorería (sin CUD API). **PT-19** requiere conciliación externa. **PT-20** regresión ACH Colombia requiere suite real. **No hay homologación declarada.**


## Registro F03 — 2026-11-02

**Pruebas añadidas sin ejecutar:** 8 `[Fact]` en `CenitCycleQueueIncorporationF03Tests.cs`:

- F03-T01 éxito C1→C2, corrección lote/totales, estado Pending, evento y reintento 0.
- F03-T02 archivo destino ya exportado impide consumir cola.
- F03-T03 despacho previo impide incorporar y duplicar operación.
- F03-T04 error forzado en inserción de evento deja lote/transacción/cola sin cambios (rollback).
- F03-T05 día operativo distinto de fecha valor: se detiene sin alterar valor.
- F03-T06 cámara ACH Colombia no permite incorporación CENIT.
- F03-T07 cola de dos ítems: segundo inválido aborta todos en transacción.
- F03-T08 operación ya asignada a ciclo destino: consume una vez sin duplicar lote.

**Ejecución estática real:** `scripts/test/cenit_f03_static_check.py` 17/17 PASS; repetición F01 12/12 y F02 13/13. **xUnit .NET ejecutadas 0; aprobadas 0; fallidas ejecutadas 0.** Pendiente dotnet SDK, SQL relacional, transacciones concurrentes, exportador real, Quartz, muestra oficial CENIT, PT-01/02/03, PT-20, PT-21 y PT-22. Comparación hash de archivos no constituye prueba de regresión funcional ACH Colombia.


## Registro F04 — 2026-11-02

**Nuevos casos fuente xUnit (22; NO ejecutados):** `CenitF04ReturnPolicyTests.cs`: 8 `[Fact]` + 12 combinaciones `[InlineData]` = 20 (R23 mismo día, R06 C5, plazo R23, notificación faltante, >15 días, fecha inconsistente, límite inclusivo). `CenitF04CalendarRouteTests.cs`: 2 `[InlineData]` de la llamada real `AchReturnEligibilityPolicyComponent` con calendario simulado y fecha límite del cliente falseada. `CenitIncomingReturnPolicyTests.cs` preexistente ajustada para nuevo contrato de evidencia de plazo posterior; casos existentes conservados.

**Ejecutadas de verdad:** `python scripts/test/cenit_f04_static_check.py` 21/21 PASS, F03 17/17, F02 13/13, F01 12/12; `patch -p1` aplicación y `patch -R --dry-run -p1` sobre origen exacto F03 correctos. **No ejecutadas:** `dotnet test` (SDK .NET 10 ausente), DB/Quartz/gateway, pruebas CENIT con archivos reales, regresión funcional ACH Colombia. Aprobadas xUnit **0**; fallidas xUnit ejecutadas **0**; pendiente de ejecutar **22 nuevos + suites existentes**.

**Comandos CI:** `dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj --filter "FullyQualifiedName~CenitIncomingReturnPolicyTests|FullyQualifiedName~CenitF04"`; después correr suite completa, en PostgreSQL y SQL Server verificar valores, eventos y rechazos de R23/R06 en rutas de entrada y salida; `PT-07/PT-08/PT-09/PT-20/PT-21/PT-22`, logs, hora de recepción de transacción y notificación auditables. Confirmar que ningún ReturnOut se genera en estado revisión manual y que no existe duplicado en reintento.


## Registro F05 — 2026-11-02

**Casos fuente xUnit incorporados/modificados (NO ejecutados):**
- F05-T01: `F05_Cycle1_CorrelatesPersistedSourceReturnBeforeCreatingFlow`: dos combinaciones (source Return persistida y raw-only) sobre registro ROR C1 con evento, valor y ciclo.
- F05-T02: `GenerateNachaAsync_CenitCycle1_UsesVerifiedRawWhenNoSeparateReturnRowExists`: no necesita transacción Return duplicada, exige raw previo al generador NACHA.
- F05-T03: `GenerateNachaAsync_CenitCycle1_RejectsMissingParentRawLink`: bloquea archivo si falta raw/evento correlacionado.
- F05-T04: `GenerateNachaAsync_CenitCycle1_RejectsMismatchedReturnTrace`: bloquea traza inconsistente.
- F05-T05: `F05_R74_MustHaveR32Parent`: padre R02 no habilita R74 incluso si mock de catálogo la permite.
- F05-T06: `F05_R74_RequiresMonetaryCreditAndOriginalPaymentInformation`: dos combinaciones sin/con adenda 05, incluye reintento idempotente.
- F05-T07: `Seeder_ShouldCreateExactCenitR60ThroughR74CatalogAndPolicies` ajustada: R74 sólo en políticas R32, excluida para otras causales; comprueba códigos por cámara.
- Se conservan pruebas originales C1 calendario, C2/C5, ACH Colombia y ROR generación. **Ninguna corrida de xUnit se certifica**.

**Ejecución real:** F05 `python scripts/test/cenit_f05_static_check.py` 26/26 PASS; repetición F01 12/12, F02 13/13, F03 17/17, F04 21/21; parche seco aplicar/revertir 6/6. **dotnet SDK ausente; xUnit ejecutadas 0; aprobadas 0; fallidas ejecutadas 0**. Integración E2E, SQLite, PostgreSQL, SQL Server, gateway CENIT, muestras oficiales, idempotencia concurrente y regresión ACH Colombia: no ejecutados.

**Comando para CI:** `dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj --filter "FullyQualifiedName~CenitReturnOfReturn2026Tests|FullyQualifiedName~AchReturnOfReturnFileGenerationServiceTests|FullyQualifiedName~RegulatoryCatalogSeederReturnOfReturnPoliciesByClearingHouseTests"`; posteriormente suite total y ensayos con archivos CENIT reales. No cerrar HC-CENIT-002/008 antes de ejecutar/inspeccionar resultados.


## Registro F06 — 2026-11-02

- **F06-T01–04**: NACHA-M CENIT con Ñ, ñ, vocal acentuada y tabulación, rechazo de bytes inválidos (4 xUnit fuente).
- **F06-T05–09**: NACHA-M CENIT admite caracteres documentados a/Z/0/#/= (5 xUnit fuente).
- **F06-T10–12**: CENIT fatal por CR/LF, primer registro incorrecto, bloque de relleno incorrecto (3 xUnit fuente).
- **F06-T13–17**: aislar D01/D02/D04/D05/D06 de STA ante validación NACHA CENIT, sin adoptar descripciones STA (5 xUnit fuente).
- **F06-T18**: duplicado de secuencia CENIT en validador real no emite D04 (1 xUnit fuente).
- **F06-T19**: compatibilidad del rechazo de ACH Colombia (1 xUnit fuente).
- **F06-T20–23**: selección de perfiles CENIT ordinario/CTX/Return/ROR (4 xUnit fuente).
- **F06-T24**: perfiles desconocido y ACH no son catalogados CENIT (1 xUnit fuente).
- **F06-T25**: fallo formal de entrada se conserva por registro, no en STA (1 xUnit fuente).
- **Retenido:** `ParseAsync_NormativeLatin1Enye_ShouldRemainAccepted` para ACH Colombia, junto con pruebas físicas anteriores.
- **Ejecutadas:** `python scripts/test/cenit_f06_static_check.py` 24/24 PASS y F01–F05 89/89 PASS; diff aplicable/reversible 5/5, CRC/SHA final. **xUnit ejecutadas=0; aprobadas=0; fallidas ejecutadas=0.**
- **Sin ejecutar:** `dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj --filter "FullyQualifiedName~NachaPhysicalParserTests|FullyQualifiedName~NachaInboundValidationSeparationTests"` porque falta SDK; luego ejecutar suite completa de ACH Colombia, NACHA CENIT real con rechazo físico/formal, STA D01–D06, DB SQL y rutas de importación de archivo publicado; **PT-11/PT-12/PT-20/PT-22** aún PENDIENTES.


## Registro F07 — 2026-11-02

- **Ejecutadas realmente**: `python scripts/test/cenit_f07_regression_check.py` **24/24 PASS**. Comprobó llamadas entre servicios y pruebas de integridad byte a byte de 6 archivos golden ACH Colombia/CENIT (10 registros × 106 bytes), además del ROR sintético CENIT de 10 registros; NO ejecutó el parser C#. Se reejecutaron las seis suites estáticas anteriores (12+13+17+21+26+24 = **113/113 PASS**). Evidencia completa: `F07_EVIDENCIA_REGRESION_ESTATICA.txt`.
- **Pruebas xUnit incorporadas pero no ejecutadas (11 casos)**: 2 enyes ACH/CENIT + 1 cambio reversible de dominio STA/ACH + 4 fuentes R06 C5 + 1 R23 mismo día + 1 D04 no admisible como devolución + 2 variantes de orden ACK/NACK/ADV e idempotencia. Clases: `CenitF07CrossPhaseRegressionTests`, `CenitChamberResponseLifecycleTests`.
- **Comprobación real de requisito previo**: `bash scripts/test/run_cenit_f07_dotnet_regression.sh` devuelve **exit 2, SDK ausente**. **xUnit ejecutadas=0, aprobadas=0, fallidas ejecutadas=0.** El runner exige .NET y ejecuta primero las pruebas transversales, luego la suite xUnit completa incluyendo ACH Colombia; guarda TRX solo si se ejecuta.
- **No ejecutado**: compilación, prueba SQLite/SQL Server/PostgreSQL, Quartz en concurrencia, gateway con archivos oficiales, STA real, conciliación Tesorería y regresión funcional ACH Colombia. No declarar cierre ni aprobación de homologación. No hay habilitación de CUD.
- **Para CI/ambiente preparado**: `bash scripts/test/run_cenit_f07_dotnet_regression.sh` (.NET SDK 10.0.402, dependencias disponibles, variables de entorno adecuadas), recopilar TRX, logs DB e identificadores de operador y ejecutar matriz PT-01 a PT-22 de `docs/auditoria-estatica-cenit/MATRIZ_PRUEBAS_POSTERIORES_CENIT.md`. La mera presencia de las pruebas no certifica su resultado.


## Registro F08 — 2026-11-02

- **Ejecutado realmente:** todas las suites estáticas F01–F07: 12+13+17+21+26+24+24 = **137/137 PASS**; F08 `python scripts/test/cenit_f08_closure_audit.py --baseline-zip ...F07...zip` **17/17 PASS** (documentos I10, 13 HC, 22 PT y conservación productiva). Total **154/154** verificaciones estáticas/documentales, NO equivalente a pruebas C# compiladas.
- **Intento de runner:** `bash scripts/test/run_cenit_f07_dotnet_regression.sh` **exit 2** / SDK .NET ausente. `global.json`: **10.0.403**. **xUnit ejecutadas=0; aprobadas=0; fallidas ejecutadas=0**.
- **PT-01–PT-22:** **22 PENDIENTES — NO EJECUTADAS**, incluyendo E2E, SQL, Quartz, operador, ACH Colombia funcional, conciliación Tesorería y vigencia normativa.
- **No hay homologación ni cierre:** 13 HC implementados con falta de evidencia de eficacia, 0 formalmente cerrados. Revisar `F08_PLAN_PRUEBAS_Y_APROBACIONES.md` y `F08_DICTAMEN_FINAL_CONDICIONADO.md`.
