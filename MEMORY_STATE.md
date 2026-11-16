# NPT_H02_H03_FINAL — LEER PRIMERO (16-11-2026)

**HOTFIX SOLO H-02/H-03. NO-GO SOAP Live.** Fuente: `ACHInterbank_HOTFIX_XSD_TRANSPORTE_NCTAORIG_H010203_2026-11-16.zip`.

Regla definitiva: **`Transactions.SourceAccountNumber → Proc_Transacciones.NCTAORIG`**, sin cuenta receptora, constantes ni fallbacks. Se retiró exclusivamente el bloqueo de *homologación de procedencia* (`PROC_TRANSACCIONES_NCTAORIG_CONTRACT_NOT_HOMOLOGATED` y guard equivalente), no los controles de origen válido, XML, permisos, configuración, idempotencia ni F5 ACH Colombia file-based.

Informe y evidencias: `ACHInterbank_ajustado/docs/npt-h02-h03-final/INFORME_NPT_H02_H03_FINAL.md`.

**Validación real:** 23/23 comprobaciones focales estáticas; seis regresiones Python/SQLite con salida 0; validación XML Schema offline salida 0. Se intentaron `dotnet --info`, `restore`, `build`, `test`; todos **exit 127** por falta del SDK. **xUnit no ejecutado**; no SQL Server/PostgreSQL, UAT ni SOAP Live.

**Estado:** H-02 corregido en fuente, H-03 pruebas corregidas; ambas pendientes de compilación y xUnit en .NET 10. Sin nuevas migraciones, dependencias ni modificación de WSDL/XSD.

---

# H-01/H-02/H-03 — transporte XSD y NCTAORIG (2026-11-16)

**NO-GO Live.** Aplicada validación final de solicitud contra XSD local en clientes CFA `Proc_Transacciones` y Axon `RegistrarRespuestaTransaccion`, para XML directo y parámetros. Guard Live `NCTAORIG` ahora niega también ausente/vacío/nil mientras no exista homologación de cuenta del originador. Prueba de readiness corregida; otras rutas no modificadas. **38/38** XSD offline, **33/33** controles previos, **21/21** focales offline, **7/7** regresiones históricas Python; verificador NPT anterior **40/42**, discrepancias históricas. SDK dotnet ausente (127); xUnit y compilación NO ejecutados; SQL Server/PostgreSQL y UAT NO ejecutados. No migraciones ni nuevos paquetes. Informe `docs/npt-transport-xsd-h010203/INFORME_H010203.md`.

---

# NPT_SOAP_XSD_FINAL_01 — 2026-11-16

**Base:** `ACHInterbank_NPT_PRE_XSD_FINAL_2026-11-15.zip`. **Decisión: NO-GO para SOAP Live.** Cinco XSD y dos WSDL de Sources embebidos sin cambios. XSD offline 38/38 (`lxml`, no .NET), fuente/contratos 33/33, siete regresiones históricas salida 0. NPT general 40/42: una expectativa heredada Axon y otra exige SOAP 1.2 no declarado por WSDL SOAP 1.1. SDK `dotnet` ausente: `--info`, restore, build y test exit 127; xUnit escritos/no ejecutados. Sin UAT ni movimientos bancarios. NPT-01 y NPT-05 parciales por homologación; NPT-10 y 14 implementados en fuente, pendiente de compilación/prueba de ejecución. No faltan XSD; pendientes externos: autorización F5, procedencia `NCTAORIG` y semántica de códigos bancarios.

**Informe y matriz:** `docs/npt-soap-xsd-final-01/{INFORME_NPT_SOAP_XSD_FINAL_01.md,MATRIZ_WSDL_XSD_IMPLEMENTACION.md}`. Tests y scripts dentro de la solución. Sin migraciones ni nuevos NuGet. Guardas Live intactas.

---

# NPT_PRE_XSD_FINAL — 2026-11-15

NO-GO para Live. Fuente focal NPT-01/02/05/11: F4.2 bloquea filas con ciclos/transacciones/links impropios y cabeceras ajenas; físico 7/99 sin adaptador queda en `DifferentialPhysicalContractGate`, preservando CENIT ROR oficial; `NCTAORIG` mapping candidato `transaction.sourceAccountNumber` con readiness y Live cerrados; correlación de prenotificación verifica archivo, cámara, link final, adenda, tipo de referencia y ciclo. NPT-01/05/11 parcialmente corregidos, NPT-02 bloqueado externamente para flujo físico funcional. 31/31 verificaciones Python fuente/WSDL; 6 regresiones históricas seleccionadas exit 0; NPT general 41/42 falla heredada Axon. `dotnet` exit 127; xUnit, SQL Server/PostgreSQL, UAT y SOAP real no ejecutados. Sin migraciones. Informe `docs/npt-pre-xsd-final/INFORME_NPT_PRE_XSD_FINAL.md`.

---

# NPT-CFA-LATE-01 — hotfix focal 2026-11-15

**Estado:** corregido en fuente, pruebas EF Core y proveedores pendientes, **NO-GO Live**. En `IncomingNachaPostProcessingPipeline.PreserveLateEvidenceAsync` se sustituyó la creación duplicada de `IncomingNachaIntegrationExecution` por la inserción inmutable idempotente de `NptLateOutcomeFenced` en `IncomingNachaProcessingEvents`, con JSON de XML, correlación, hashes y resultados. El índice único `(EntryDetailId,AttemptNumber)` permanece inalterado. Seis xUnit SQLite (intento real persistido, no sobrescritura, duplicados, 3 workers, respuesta distinta y trigger rollback), escritos/no ejecutados. Guard de fuente/modelo SQLite 15/15 PASS; 5 regresiones históricas exit 0, NPT focal 26/26; NPT antiguo 41/42 por Axon preexistente. dotnet info/restore/build/test exit 127 (SDK ausente). Sin migraciones, SOAP, Live ni UAT. Informe: `ACHInterbank_ajustado/docs/hotfix-npt-cfa-late-01/INFORME_NPT_CFA_LATE_01.md`.

---

# NPT focal — 2026-11-15 (siete puntos, NO-GO)

**NPT-03/04/06/08/09/10/11:** corregidos en fuente, pendientes de build, xUnit, SQL Server/PostgreSQL y simuladores SOAP instrumentados. Fencing de finalización CFA/Axon, watchdog independiente, evidencia tardía, no replay incierto, validación estricta del parser y correlación NACHA por vínculo final/archivo/cámara. Fuente de reauditoría posterior no disponible; se utilizaron los siete hallazgos descritos por el usuario. **NO-GO**: no UAT ni Live.

**Validación realizada:** 26/26 controles fuente/modelo SQLite + 5 verificadores históricos (28/28, 35/35, 39/39, 27/27, 31/31) con exit 0. El verificador NPT histórico salió 1 (41/42; esperaba CommitAsync antiguo en cancelación Axon, reemplazado por TryFinalizeAsync con token de persistencia independiente). `dotnet --info/restore/build/test` intentados, exit 127: no SDK. Nuevas pruebas xUnit escritas, no ejecutadas. No migración nueva. NPT-01/02/05/14 intactos y bloqueados externamente; NPT-07/12/13 intactos.

**Rutas:** `docs/cierre-focal-npt-2026-11-15/INFORME_CIERRE_FOCAL_NPT.md`, `MATRIZ_PRUEBAS_FOCAL.md`, `COMANDOS_REPRODUCIBLES.md`, `INVENTARIO_CAMBIOS.md`.

---

# Estado NPT — 2026-11-15

**NO-GO.** Implementación de fuente NPT-03/04/06/07/08/09/10/11/12/13 pendiente de compilación, xUnit y pruebas de infraestructura. Bloqueados por homologación NPT-01/02/05/14. No se ejecutó UAT ni SOAP Live. Se preservan resultados históricos sin promocionarlos a validación del incremento actual. Véase `docs/cierre-npt-2026-11-15/INFORME_IMPLEMENTACION_NPT_01_14.md`.

---

# ESTADO MÁS RECIENTE — RES-H02-01/02, cierre focal previo a SOAP Live (2026-11-15)

**Fuente:** `ACHInterbank_PROYECTO_COMPLETO_HOTFIX_FOCAL_H01_H02_2026-11-15.zip` (SHA-256 `0c9662d1e44a8b68e7760ebe5c23c04b70b835acd61269cebdc2e3fcaf8de631`). **Estado:** implementado en C#, NO-GO hasta compilación, xUnit, pruebas de concurrencia SQL Server/PostgreSQL y cierre de ciclo coordinado. UAT no ejecutada.

- **RES-H02-01:** ambas rutas (`ProcessPendingAsync` y `RetryBatchAsync`) revalidan estado Open, ventana, calendario, política y lease/ciclo a la salida del mapper y **después de intención InFlight, antes de SOAP**. Si se cierra, se aborta por CAS, `RetryPending`, intento técnico negativo `NotExecuted`, `AttemptCount` inalterado, y elegibilidad para nuevo ciclo solo cuando está demostrado que no hubo envío. La incertidumbre pasa a conciliación.
- **RES-H02-02:** claim Quartz incluye `AchCycleId` + cámara + Open + fase Idle + condiciones históricas. Lectura del item reclamado también verifica ciclo y fase; claim no exitoso corta bucle para evitar selección infinita. Manual conserva claim atómico y hace el nuevo preflight.
- **Ejecución:** 27/27 focales fuente/SQLite, 12/12 scripts regresión, grafo 7/7; dotnet comandos código 127 (no disponible), xUnit escritos sin ejecutar, SQL Server/PostgreSQL/Live/UAT no ejecutados. Nuevos cuatro métodos xUnit (seis casos).
- **Riesgo no resuelto hasta entorno:** cierre entre la última guarda DB y envío I/O necesita protocolo de exclusión compartido con cierre de ciclos o garantía del receptor. No declarar seguridad Live absoluta aún.
- **Informe/plan:** `docs/hotfix-res-h02-2026-11-15/` y `LEER_PRIMERO.md`. Migración nueva no necesaria, sin modificaciones de documentación normativa ni E2E-03 independiente.

---

# ESTADO ACTUAL — HOTFIX FOCAL H01 / H02 (2026-11-15)

**Fuente exclusiva:** `ACHInterbank_PROYECTO_COMPLETO_AUD_E2E01_E2E04_FINAL_2026-11-15.zip` (SHA-256 `5bca98ea7f78402b92b7104cbf0d1522135a5529fdb466fd50775374eab1d85c`). Se conserva el árbol íntegro de origen sin eliminar ni duplicar documentación normativa. **Estado:** código y pruebas escritos, validación de compilación y proveedores pendiente; **NO-GO**. UAT **NO** ejecutada.

- **H01 (P1):** `RecoverStaleAsync` ya no incrementa `AttemptCount` al recuperar un lease: con cuatro intentos SOAP previos, el próximo SOAP sigue siendo el quinto. El evento de recuperación se audita en la tabla de intentos con ordinal negativo único (`-1`, `-2`, ...) para no consumir ni colisionar con numeraciones reales. Las operaciones de recuperación `InFlight`/incertidumbre siguen a conciliación sin reenvío. Los registros heredados que quedaron en `RetryPending` con `AttemptCount >= 5` se trasladan atómicamente a `ContrapartidaReportFailed` con `RequiresReconciliation=true`, sin reiniciar el contador histórico ni enviar otro débito. Quartz invoca el escáner incluso sin ciclos abiertos.
- **H02 (P2):** `RetryBatchAsync` deja de usar el ciclo del lote original como destino obligado. Selecciona un ciclo actualmente abierto, vigente y permitido para débito (política + calendario bancario + ventana); sin destino seguro difiere sin SOAP. Reasignación de la cola y claim se hacen en un único CAS que comprueba ciclo, fase `Idle`, token, estado, contador y conciliación. Vuelve a validar el ciclo antes de cada ítem; si cambia a cerrado, no reclama los restantes. El `AchTransaction`/`AchBatch` y su fecha/ciclo monetario original permanecen inalterados. Resultado SOAP incierto conserva bloqueo también en reintento manual.
- **No se añadieron migraciones:** se reutilizan columnas/índices existentes compatibles con SQL Server/PostgreSQL. Se preserva E2E-01/E2E-02, AUD-E2E-01..04, los 13 correctivos CENIT y hotfixes ACH Colombia. No se intervino E2E-03 externo.
- **Ejecuciones reales:** 39/39 controles focales de fuente/modelo SQLite, 31/31 y 35/35 controles previos E2E y 9/9 verificadores de regresión ACH/CENIT (todos con salida 0). Validación sintáctica heurística de llaves C# (no compilación). **xUnit H01/H02 incorporados, sin ejecutar** por falta de `dotnet`; **build .NET, Postgres, SQL Server, SOAP real, UAT no ejecutados**. Las simulaciones SQLite **NO prueban** concurrencia real de EF Core ni semántica SQL de otros proveedores.
- **Ruta técnica:** `docs/hotfix-h01-h02-2026-11-15/` con plan, informe, manifest y evidencias. Cierre definitivo H01/H02 requiere compilar, ejecutar xUnit y probar concurrencia/reinicio en SQL Server y PostgreSQL con transporte SOAP simulado. No iniciar UAT.

---

# Memoria del último hotfix — AUD-E2E-01..04 (2026-11-15)

- **Fuente exclusiva:** `ACHInterbank_PROYECTO_COMPLETO_E2E01_E2E02_2026-11-15.zip` (SHA-256 `df172cf3d96079792ea19091cead336b7a76455dcacd7c6929c2a6b8c2989b55`).
- **AUD-E2E-01 / P0:** ámbito de la transacción y rollback/commit/dispose corregidos en `AchTransactionService.cs`. Liberar `CurrentTransaction` antes de leer ganador de colisión única.
- **AUD-E2E-02 / P1:** CAS de expiración, fase, token, intentos y conciliación en `ContrapartidaDispatchRecovery.cs`.
- **AUD-E2E-03 / P1:** recuperados `NOT_SENT` pueden reasignar su cola a ciclo permitido abierto con meta de origen/fecha; no reenvío outcome incierto; Quartz no escanea ciclos cerrados para despacho.
- **AUD-E2E-04 / P1:** intención SOAP y transición `InFlight` confirmadas en una transacción; XML, SHA-256, correlación, mapper, endpoint y modo persistidos antes de red.
- **Comprobación efectiva:** 31/31 focales y SQLite modelo, 35/35 previas, 9/9 scripts de regresión; 8 nuevos tests xUnit incorporados pero **NO ejecutados**. SDK .NET ausente; SQL Server, PostgreSQL y UAT no utilizados. Estado `IMPLEMENTADO_PENDIENTE_COMPILACION_XUNIT_MULTIDB`; NO-GO.
- **Plan / evidencias:** `docs/hotfix-aud-e2e01-e2e04-2026-11-15/`.

---

# ESTADO ACTUAL — HOTFIX E2E-01 / E2E-02 (2026-11-15)

**ZIP fuente único:** `ACHInterbank_PROYECTO_COMPLETO_I13_HOTFIX_CONTEOS_NACHA_T6_2026-11-15.zip` (SHA-256 `60a4f6e627c8dd62ea271af84c80a776c903d966a9626c25508a7cb8e6b7e8a0`). **Resultado:** implementado en C# y dos migraciones EF; 35/35 controles estáticos + simulaciones SQL SQLite, nueve scripts estáticos heredados con salida 0. **Build .NET y xUnit no ejecutados** (SDK ausente, `dotnet` código 127); PostgreSQL/SQL Server sin ejecutar; UAT no ejecutada; producción NO-GO. Los correctivos CENIT I00-I13 y los hotfixes ACH Colombia permanecen; E2E-03 a E2E-07 no se han modificado.

- **E2E-01:** índice único filtrado real por entidad originadora + ID externo en SQL Server/PostgreSQL, preflight histórico sin borrado, fingerprint SHA-256 de instrucción, reconsulta dentro de transacción y ante colisión, reutilización sin crear cola/despacho, conflicto HTTP 409 por contenido distinto. Datos históricos sin fingerprint deben conciliarse.
- **E2E-02:** lease durable en ambos flujos de dispatch y manual, frontera `Preparing` / `InFlight` persistida antes de SOAP, recuperación Quartz aun sin ciclo abierto, CAS concurrente, auditoría de intentos y conciliación obligatoria en resultado desconocido, sin reenvío automático.
- **Plan de pruebas restante:** restaurar .NET 10, build Release, xUnit focal y completo, ensayo de migración SQL Server/PG con examen de duplicados, pruebas reales de concurrencia/caída/reinicio SOAP con observación de una sola emisión. No realizar UAT antes de G4.
- **Evidencia:** `ACHInterbank_ajustado/docs/hotfix-e2e01-e2e02-2026-11-15/INFORME_E2E.md` y `evidencias/`; diff `ARCHIVOS_MODIFICADOS.md`.

---

# HOTFIX FOCAL — CONTADORES DE ENTRADAS NACHA-M (2026-11-15)

**Fuente exclusiva** `ACHInterbank_PROYECTO_COMPLETO_I13_HOTFIX_ACHCOL_FRONTERA_ADMISION_D01_D02_2026-11-15.zip` · SHA-256 `77dbc0ec87151836e054c7c4df9eac2f99734c1927eab7bb9ca9b9e491ef62a2`. **Estado** `IMPLEMENTADO_PENDIENTE_COMPILACION_XUNIT_DB`; se preservan todos los antecedentes I00–I13, hotfixes ACHCOL D01/D02/D04, R10 y la frontera de admisión, y CENIT ROR/C5 con calendario bancario. No se inicia I14.

- **Antes:** parser contaba exclusivamente T6 inicialmente válidas; ingesta restaba otra vez `ErrorCount`; errores por T6 se confundían con transacciones rechazadas, y las validaciones posteriores de adendas no se reflejaban en los conteos.
- **Después:** T6 físicas (`TotalEntries`), T6 finales aceptadas (`ValidEntries`) y rechazadas (`InvalidEntries`) quedan separadas de fallas (`ErrorCount`); en ingesta `ValidCount`/`InvalidCount` mapean directamente; resultado incoherente se bloquea sin posprocesar.
- **Fuente:** `NachaParserService.ParseAndSaveDetailedAsync`, `NachaParseResult`, `IncomingNachaIngestionAppService.IngestAsync`.
- **Pruebas de código añadidas/ajustadas:** `NachaFunctionalValidationTests` (lotes físicos mixtos D01, rechazo de adenda, error temprano), `AchTransactionNachaTests` (D01/D02 file-based), `IncomingNachaIngestionAppServiceTests` (2/1/1 con una o dos fallas y error de contrato), y `CenitOrdinaryInbound2026ParserTests` (actualización de aserciones del conteo físico CENIT sin modificar lógica).
- **Verificaciones realmente ejecutadas:** 28/28 controles estáticos focales; 15/15 verificadores estáticos de referencia; sin errores observados. **No ejecutado:** compilación, xUnit, pruebas SQL/integración y UAT. No se han cerrado formalmente los 13 hallazgos CENIT. Producción: NO-GO.
- **Evidencia y riesgos:** `ACHInterbank_ajustado/docs/hotfix-nacha-transaction-counts-2026-11-15/INFORME_CONTEOS_TRANSACCIONES.md` y `evidencias/`. Restan compilación real, ejecución de archivos NACHA físicos, comprobación de persistencia/reprocesos, reportes/UI, distintas cámaras y regresión con calendarios.
- **Próximo:** ejecutar `dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj --filter "FullyQualifiedName~NachaFunctionalValidationTests|FullyQualifiedName~AchTransactionNachaTests|FullyQualifiedName~IncomingNachaIngestionAppServiceTests"`, además de compilación y suite completa en .NET 10; continuar G1–G3 sin iniciar UAT.

---

# ESTADO ACTUAL — AJUSTE DE FRONTERA ACHCOL D01/D02 (2026-11-15)

**Fuente exclusiva:** `ACHInterbank_PROYECTO_COMPLETO_I13_HOTFIX_FINAL_ACH_DATES_R10_2026-11-15.zip` · SHA-256 `8c5c776758573b32f781088e516c5ab63789ebea362103164c7935f2af04fdfc`. **Estado:** `IMPLEMENTADO_PENDIENTE_COMPILACION_XUNIT`. Se conserva I00–I13, el calendario ROR CENIT, C5 y los hotfixes anteriores. No se abre I14.

- **Defecto anterior:** `IncomingNachaAdmissionValidator` rechazaba `EFFECTIVE_DATE_MISMATCH`/`ACHCOL_EFFECTIVE_DATE_INVALID` antes de que `NachaParserService` generase D01/D02 por T6; la ruta ACHCOL sin ciclo interno no utilizaba la fecha operativa resuelta.
- **Corrección:** admisión difiere fechas efectivas erróneas **solo** para cámara verificada ACHCOL; mantiene formato físico, correlación T1/nombre, calendario, fecha/ciclo CENIT y autorización. Parser preserva contexto de fecha operativa sin inferir un `AchCycle`, compara contra T1 y falla cerrado cuando no concuerda; D01/D02 siguen evaluándose por lote/T6 y D04 no cambia.
- **Archivos y métodos:** `IncomingNachaAdmissionValidator.ValidateAsync`, `NachaParserService.ParseAndSaveDetailedAsync/ParseFileHeaderLinq`; tests en `IncomingNachaAdmissionAchColFileBasedTests`, `IncomingNachaTraceabilityCoreTests`, `AchTransactionNachaTests`. Se incorpora un verificador focal y evidencia sin duplicar normativa.
- **Ejecución real:** 30/30 controles estáticos nuevos, 41/41 controles previos; 12/12 verificadores históricos CENIT y 20/20 HC-CENIT-002 pasaron. SDK .NET ausente: **compilación, xUnit y pruebas DB no ejecutadas**. UAT no ejecutada.
- **Riesgos y próximo paso:** compilar y ejecutar xUnit/ingesta física multiperfil/DB durante G1–G3; validar persistencia y devolución física por operador. No declarar producción ni cierre normativo, **0/13 CENIT formalmente cerrados**. Ver `ACHInterbank_ajustado/docs/hotfix-ingestion-boundary-2026-11-15/INFORME_FRONTERA_INGESTA.md`.

---

# ESTADO ACTUAL — HOTFIX FINAL PRE-DESPLIEGUE ACH COLOMBIA (2026-11-15)

**Base estricta:** `ACHInterbank_PROYECTO_COMPLETO_I13_PRE_DESPLIEGUE_3_HOTFIXES_2026-11-15.zip` · SHA-256 `2f37ef69b7f97d2c73db2ba872aa4205ce70c916a5f0f82f153e91f476485242`. **No se abre I14 y no se modifica ningún correctivo CENIT ni el calendario ROR.**

- **HOTFIX FINAL-01 (D01/D02/D04):** `IMPLEMENTADO_PENDIENTE_COMPILACION_XUNIT`. Aceptación de fecha efectiva nula/vacía eliminada. D01 y D02 se notifican por entrada/lote como devoluciones por operador V35, no como rechazo físico. Cuando la fecha operativa no puede resolverse se detiene en fallo técnico cerrado (`ACHCOL_OPERATIONAL_DATE_UNRESOLVED`), sin inventar D01. Se conserva D04 de prenotificación no cero y catálogos segregados. Fuentes: `NachaInboundValidation.cs`, `NachaParserService.cs`; tests xUnit de fechas y archivos físicos añadidos.
- **HOTFIX FINAL-02 (R10):** `IMPLEMENTADO_PENDIENTE_COMPILACION_XUNIT`. Matriz oficial C1–C7 por jornada actual / anterior hábil; utiliza calendario bancario, cruces de medianoche y comprueba `AllowsReturn`. Historial anterior de cinco ciclos preservado. Fuentes: `AchReturnEligibilityPolicyComponent.cs`, `RegulatoryCycleScheduleCatalog.cs`; tests xUnit de 98 pares y ocho escenarios integrados añadidos.
- **Verificaciones ejecutadas:** focal **41/41** estáticas; CENIT histórico **12/12 scripts, 391/391** comprobaciones estáticas; HC-CENIT-002 **20/20** estáticas; solución **7/7** proyectos presentes y sin referencias faltantes. Todos son controles de fuente, NO pruebas .NET.
- **No ejecutado:** `dotnet restore/build/test`, xUnit y DB por SDK .NET ausente; UAT tampoco. **0/13 hallazgos históricos cerrados**, NO-GO producción. Restan pruebas de compilación, archivo NACHA físico de operador e integración de reglas R10.
- **Informe y evidencias:** `ACHInterbank_ajustado/docs/hotfix-final-pre-despliegue-2026-11-15/INFORME_HOTFIX_FINAL.md`, carpeta `evidencias/`. **Próximo:** recorrido técnico G1-G3 para pruebas, UAT única G4 al final.

---

## Historial anterior íntegro

# ACTUALIZACIÓN HOTFIX PRE-DESPLIEGUE — 2026-11-15

**Base:** `ACHInterbank_PROYECTO_COMPLETO_I13_ROR_C5_HOTFIX_2026-11-15.zip` · SHA-256 `3600efca392c1664701d066810aa9df11d8f2b55e282e1f18496a76e4b9001aa`. **Resultado:** HOTFIX-01/02/03 implementados en fuente y acompañados de pruebas xUnit escritas. NO compilados ni ejecutados en .NET (SDK 10 no disponible). Se conservan todos los bloques históricos que siguen debajo, incluidos I00–I13, F01–F08, auditorías y estado **0/13 CENIT cerrados formalmente**. UAT NO ejecutada; producción NO-GO.

- **HOTFIX-01 (ACH D01/D02/D04):** antes las fallas técnicas suplantaban causales ACH; después fechas y prenotificación tienen códigos V35 correctos, archivos/contadores y duplicados físicos son técnicos, catálogo CENIT STA segregado. Archivos: `NachaInboundValidation.cs`, `NachaParserService.cs`, pruebas `NachaInboundValidationSeparationTests.cs` y tres fixtures de catálogo. Pruebas C# no ejecutadas.
- **HOTFIX-02 (ACH R10 C1–C7):** antes clasificación por hora aislada; después inicio operativo con fecha y C1 día anterior, MaxCycles y bases R10 conservadas. Archivo `AchReturnEligibilityPolicyComponent.cs`; pruebas `AchReturnsFileByClearingHouseTests.cs`. Pruebas C# no ejecutadas.
- **HOTFIX-03 (CENIT ROR bancario):** antes faltaba comprobar fecha de operación; después bloqueo por `IOperationalCalendarService.IsBusinessDayAsync` para C5 intradía y fecha destino siguiente día, ruta común In/Out; HC-CENIT-002 C5 y deadline C2 preservados. Archivos `CenitReturnOfReturnService.cs` y `CenitReturnOfReturn2026Tests.cs`; fechas de domingo corregidas. Pruebas C# no ejecutadas.

**Pruebas ejecutadas:** focal 31/31 controles estáticos, regresión HC-CENIT-002 20/20, 12/12 scripts históricos disponibles (391/391 aserciones estáticas) y 7/7 proyectos del grafo de solución. **No equivalen a compilación ni pruebas funcionales**. Evidencia: `docs/hotfix-pre-despliegue-2026-11-15/INFORME_PRE_DESPLIEGUE.md` y `evidencias/`. **Pendientes:** restore/build/xUnit, pruebas de regresión e integración DB, calendario institucional, archivos NACHA reales, despliegue técnico y pruebas posteriores; UAT solo al finalizar el proyecto. **Riesgo colateral observado fuera de alcance:** descripciones/atribuciones D05/D06 legadas en validaciones estructurales, sin modificación.

**Próxima actividad:** compilar y ejecutar las pruebas en un entorno .NET 10.0.403, resolver fallos probados sin alterar normas, desplegar a ambiente de pruebas técnicas y continuar la validación prevista. No aprobar homologación ni producción.

---

# Corrección puntual posterior a I13 — ROR CENIT: C5 normativo

**Fecha:** 2026-11-15 · **Línea base exclusiva:** `ACHInterbank_PROYECTO_COMPLETO_I13_HC-CENIT-013_2026-11-15.zip` · SHA-256 `d1331e86c4ee0063a86e1ad07b23b1b340efde842f6f179c91f7038919f239cd`. **No se abre un incremento I14.**

Se ha corregido `CenitReturnOfReturnService.ValidateOperationalWindowAsync`: ya no se calcula el último ciclo a partir de `Max()` en `AchCycles`, porque una jornada incompleta (C1–C4) convertía C4 en supuesto último ciclo. La regla intradía exige padre C2/C3/C4 y destino **C5**. Ambas rutas saliente y entrante usan el mismo validador. La regla del segundo ciclo del siguiente día hábil no se modificó. **No existe autorización operacional expresa incorporada en este paquete para sustituir C5**; sin ella se falla en cerrado.

`CenitReturnOfReturn2026Tests.cs` ahora configura C1–C5, actualiza casos positivos para C5, incluye negativos C4 con calendario completo e incompleto, cobertura entrante y matriz C1–C5. Se conservan las pruebas históricas de plazo siguiente día hábil, correlación y R74.

**Evidencia efectivamente ejecutada:** 20/20 suites previas con 542/542 verificaciones estáticas/documentales PASS, más 20/20 comprobaciones focales de fuente PASS. **NO EJECUTADO** .NET SDK, `dotnet restore/build/test`, SQLite/SQL Server/PostgreSQL, concurrentes, integración NACHA-M ni homologación oficial. Pruebas xUnit agregadas: cuatro métodos (ocho variantes), **0 ejecuciones**. Los controles estáticos no prueban compilación ni ejecución. El resultado normativo permanece **0/13 hallazgos formalmente cerrados**, UAT única final, producción **NO-GO**.

Fuente normativa disponible dentro del ZIP I13: `ACHInterbank_ajustado/docs/normativa/md/CENIT-DSP-152-Anexo-2.md` §3 y §§4.3/4.6 (último ciclo intradía y plazo alternativo siguiente día hábil). Se preservan los archivos de ACH Colombia, migraciones, dependencias, contratos, SDK/lockfiles y las correcciones I00–I13.

**Pruebas por ejecutar en ambiente .NET 10:** `dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj --filter FullyQualifiedName~CenitReturnOfReturn2026Tests` desde `ACHInterbank_ajustado/`, con informe TRX, SQLite y PostgreSQL/SQL Server según corresponda. Validar también respuestas reales de operador, fechas valor y correlación del archivo físico. **No declarar cierre formal hasta concluirlo.**

**Rollback:** recuperar exactamente las modificaciones focales desde el ZIP I13 (servicio, clase de pruebas, nueva prueba estática y documentos asociados); antes de despliegue, inspeccionar cualquier ROR histórico generado en C4 por configuraciones incompletas. No revertir movimientos financieros automáticamente.

---

# ACTUALIZACIÓN CENIT I11 — 2026-11-15

I11 (HC-CENIT-011) implementado con selección de catálogo de rechazo por cámara y segregación STA D01–D06 / NACHA-M. Pruebas xUnit escritas sin ejecución por SDK .NET ausente; PT-12 pendiente. 504/504 verificaciones estáticas/documentales. HC-CENIT-011 ABIERTO; 0/13 cerrados, UAT única final, NO-GO. Fuente exclusiva: ZIP integral I10, SHA-256 `16bd09e318c6689a89755332a4d107f5cc94b48b8f5721372761d09539b5fbfa`. Siguiente I12 sobre ZIP I11. Evidencia `EVIDENCIAS/I11/INFORME_I11.md`.

---

# ACTUALIZACIÓN VIGENTE — I07-R1 (2026-11-02)

**Paquete acumulativo vigente:** `ACHInterbank_COMPLETO_SIMULADOR_PRENOTIFICACIONES_I07R1_2026-11-02.zip`. Este encabezado prevalece sobre menciones históricas a I07 en el resto del archivo.

**Avance concreto:** la SPA invalida respuestas de previsualización tardías al cambiar opción, cámara, prenotificación o causal; la descarga backend valida tamaño y SHA-256 del contenido antes de registrar `Downloaded` y retorna 422 controlado si el archivo difiere. Añadidas regresiones Angular y xUnit, junto a verificador I07-R1.

**Verificaciones ejecutadas:** grafo 7/7; estáticas I01–I07 200/200; estáticas I07-R1 20/20; TypeScript transpile sintaxis 2/2. **NO ejecutadas**: .NET (sin SDK), npm install/build (caché offline carece de zone.js@0.16.3), Karma/Playwright, UAT/BD/NachaUpload. **PRE-01..PRE-14 = 0/14 homologadas**. No habilitar producción ni afirmar certificación.

**Regla de continuidad:** usar solo este ZIP íntegro para I07-R2; no rehacer ni perder I00–I07. Continuar verificaciones/correcciones seguras aunque falte ambiente; entregar siempre un ZIP completo, memoria, estado, hashes, pruebas con su resultado real y pendientes. Sin sobreingeniería, sin otros componentes UI.

**Documentación vigente:** `docs/implementacion-simulador-prenotificaciones/INFORME_I07R1.md`, `ESTADO_I07R1.md`, `MATRIZ_ACEPTACION.md`, `EVIDENCIA_I07R1.json`.

---

# MEMORY_STATE — ACH Interbank / simulador de respuesta a prenotificaciones

**Corte:** 2026-11-02  
**Paquete:** I07 — correcciones de regresión incorporadas; sin compilación Angular/.NET ni homologación runtime  
**Último ZIP íntegro vigente:** `ACHInterbank_COMPLETO_SIMULADOR_PRENOTIFICACIONES_I07_2026-11-02.zip` (derivado de I00; no reutilizar ZIP de F08 como base)  
**Directorio raíz del proyecto dentro del ZIP:** `ACHInterbank_ajustado/`  
**Continuidad:** usar siempre el **último ZIP incremental** como única base, no reconstruir el proyecto desde memoria de chat.

## Objetivo vigente

Implementar en la SPA `/uat/nacha-inbound-simulator` la **simulación de respuestas a prenotificaciones SALIENTES originadas por ACH Interbank**, dirigida a ACH Colombia y CENIT. La solución debe cubrir dos rutas, separadas y normativamente fieles:

1. **Devolución expresa:** seleccionar prenotificación original elegible; generar archivo oficial NACHA-M de devolución (monto cero, causal aplicable, traza original, nuevo trace, adenda y ciclo válido); descargarlo mediante JWT y **cargarlo manualmente por NachaUpload** para validar la correlación y el estado.
2. **Sin devolución dentro del plazo:** no crear supuestas confirmaciones positivas NACHA-M. Comprobar la **aceptación tácita** con tiempo controlado y flujo de dominio/job existente sin alterar reloj global ni estados a mano.

**Restricción:** UAT/local exclusivamente. No transmisión a cámaras/bancos reales, no movimientos monetarios, no autoimportación, no escrituras productivas. Mantener perfiles y reglas separados por ACH Colombia/CENIT. Compatibilidad con contratos históricos salvo migración explícita y probada.

## Estado verificado y afirmaciones permitidas

- La línea base contiene rutas de SPA, backend, builder, NachaUpload, `PrenotificationValidationAttempt.ResponseDeadlineAtUtc` y `AchTacitAcceptanceJobHandler` identificados en el análisis previo.
- Existe `IncomingPrenotificationResponse` en la selección/contrato pero el flujo diferencial no devolutivo está bloqueado por `DIFFERENTIAL_GENERATOR_NOT_HOMOLOGATED`; **no declararlo implementado**.
- La auditoría del simulador del 2026-11-02 registró **12 hallazgos abiertos** y 14 comprobaciones por patrones estáticos; **esas comprobaciones no equivalen a pruebas runtime**.
- **I00 = COMPLETADO** solo para documentación, seguimiento y entrega íntegra del proyecto; no constituye implementación de la funcionalidad.
- **I01 = EN_PROGRESO**: contrato, enums, 2 endpoints read-only, validación de forma, bloqueo del escenario y tests C# incorporados. **20/20 comprobaciones estáticas PASS** y grafo .sln PASS; no hay dotnet disponible para compilar/ejecutar tests C#. I02 código incorporado con 26/26 PASS_STATIC, runtime .NET pendiente; I03 e I04 código incorporado, I05 código incorporado (36/36 PASS_STATIC), I06 código UI incorporado (32/32 PASS_STATIC, pruebas Angular no ejecutadas); I07 con correcciones puntuales, 24/24 PASS_STATIC; sigue pendiente la homologación I07-R. No hay verificación E2E/runtime de la nueva función.
- El proyecto tiene un `MEMORY.md` anterior y múltiples estados históricos: **no sobrescribirlos ni presentarlos como estados de este nuevo trabajo**. Este archivo documenta únicamente la iniciativa del simulador de respuestas a prenotificaciones.

## Referencias normativas provistas

- ACH Colombia `ACH-Colombia-V35.md` (abril 2026), numeral **2.10.3.1**: ausencia de devolución en el plazo se considera prenotificación exitosa.
- CENIT `CENIT-DSP-152-Anexo-2.md` (febrero 2025), numeral **4.8**: devolución en la misma fecha valor, hasta el último ciclo; vencimiento sin devolución habilita la operación posterior.
- Manual de Especificaciones NACHA-M CENIT (mayo 2026), registros de 106 caracteres, controles, adendas y tablas de códigos.
- Mantener manuales de ambos operadores sin asumir que los plazos y ciclos son iguales.

## Plan de incrementos

| Incremento | Estado al corte | Resultado faltante |
|---|---|---|
| I00 — Empaquetado y seguimiento | COMPLETADO (documental) | Sin cambios funcionales |
| I01 — Contrato y casos de uso | EN_PROGRESO | Código y documentos realizados; faltan `dotnet build` y `dotnet test` |
| I02 — Consulta y elegibilidad | EN_PROGRESO | Consulta paginada, policy y preview incorporados; compilar y ejecutar Xunit/API; reserva única en I03 |
| I03 — Archivo NACHA-M devolución | EN_PROGRESO | Endpoint y builder incorporados; falta compilar, fixtures y concurrency SQL |
| I04 — NachaUpload y reconciliación | EN_PROGRESO | Guarda de correlación y pruebas C# incorporadas; faltan compilación, fixtures, reupload y E2E |
| I05 — Sin devolución/aceptación tácita | EN_PROGRESO | Código de reloj virtual/real incorporado; runtime pendiente |
| I06 — SPA completa | EN_PROGRESO | Código UI + 5 tests añadidos; build Angular, Karma, Playwright y JWT runtime pendientes |
| I07 — Regresión y cierre UAT | EN_PROGRESO | Código corregido + estáticas PASS; pruebas reales PRE-01..PRE-14 aún no ejecutadas; fase I07-R |

## Riesgos y dependencias conocidos

1. Descarga con `window.open` puede omitir JWT; usar `HttpClient` blob y permiso Download.
2. Validación diferencial actualmente inconsistente entre selección y generación; no confiar en validación de UI.
3. Reutilización de trace/ciclo y control de respuestas duplicadas requieren comprobación rigurosa.
4. Consecutivos no atómicos y escritura de archivo/DB/evidencia fragmentada; corregir antes de cierre.
5. E2E anteriores con selectores Material desactualizados; actualizar como parte de implementación.
6. Algunos archivos de evidencia históricos contienen etiquetas de prenotificación, **no prueban homologación de la nueva ruta**.

## Instrucción inequívoca para el siguiente turno (I07-R)

1. Abrir el **ZIP incremental más reciente**, revisar este `MEMORY_STATE.md`, el `docs/implementacion-simulador-prenotificaciones/ESTADO_INCREMENTAL.md`, el plan y la auditoría.
2. Continuar **I07-R** (homologación runtime de I07) sin repetir auditoría total; priorizar Angular y pruebas integradas NACHA-M/aceptación por cámara.
3. Ejecutar lo ejecutable. Si falta .NET, Node, Postgres u otro componente: registrar `NO_EJECUTADA_POR_AMBIENTE`, hacer verificaciones estáticas o pruebas independientes cuando sea posible, **continuar el trabajo seguro del incremento**. Nunca reportar `PASS` sin ejecución; no habilitar operación sin evidencias requeridas.
4. Actualizar el plan si cambia el diseño, `ESTADO_INCREMENTAL.md`, este `MEMORY_STATE.md`, informe y changelog; incorporar diff y pruebas en la carpeta de evidencias.
5. Entregar **el ZIP completo con TODO el proyecto** (no solo patch) y resumen: implementado, probado, pendiente, bloqueado, riesgos, siguiente incremento.
6. Si una restricción del entorno impide una prueba, no ocultarla ni usarla como excusa para detener cambios de código que sí se puedan efectuar. Si la validación final exige UAT real, señalarlo pendiente, sin falsos cierres.

## Cómo continuar en ChatGPT en modo chat

Adjuntar el ZIP incremental más reciente y escribir: «Continúa I07-R del simulador de respuestas a prenotificaciones. Usa `MEMORY_STATE.md` y el plan interno. Entrega ZIP COMPLETO, cambios y estado». Repetir con cada incremento posterior; cada nueva entrega sustituye a la anterior como base única. Este archivo es memoria **documental portátil**, no una modificación de la memoria del producto ChatGPT.

## Estado específico de I01 (2026-11-02)

- Nuevos símbolos: `NachaInboundSimulationType.IncomingPrenotificationReturn=10`, `PrenotificationSimulationOutcome.{Returned,NoReturnByDeadline}`, `PrenotificationSimulationContractRequest`, `PrenotificationSimulationContract`.
- Endpoints I01: `GET /api/uat/nacha-inbound-simulator/prenotifications/contract` y `POST /api/uat/nacha-inbound-simulator/prenotifications/validate-contract`, ambos protegidos por TestTools.Access + NachaSimulatorRead; **no ejecutan escenarios**.
- La validación I01 comprueba solo forma: cámara, ids, originalCycle, responseCycle según resultado, businessDate, respondingBank, reason e idempotencyKey. NO afirma que la operación existe o es elegible.
- El `GenerateAsync` anterior invoca Preview; Preview devuelve `PRENOTIFICATION_RETURN_NOT_IMPLEMENTED` para escenario 10, por lo que NO construye un supuesto NACHA. El legado escenario 3 sigue como estaba; no remapearlo implícitamente.
- Evidencias: `docs/implementacion-simulador-prenotificaciones/INFORME_I01.md`, `CONTRATO_PRENOTIFICACIONES_I01.md`, `CASOS_USO_I01.md`, `EVIDENCIA_I01.txt` y `scripts/test/prenotification_simulator_i01_static_check.py`.
- Entorno: no hay SDK dotnet; `python scripts/validate-solution-projects.py` PASS, `python scripts/test/prenotification_simulator_i01_static_check.py` PASS 20/20. Las pruebas Xunit añadidas NO EJECUTADAS.
- La funcionalidad no se activa en I01; próximas tareas: I02 elegibilidad con base de datos/idempotencia y I03 archivo NACHA-M homologado; I05 tiempo de job; I06 SPA.

## Estado específico de I02 (2026-11-02)

- Nuevos símbolos: `PrenotificationCandidateQuery`, `PrenotificationCandidatePage`, `PrenotificationBusinessEligibilityPreviewResponse`, `PrenotificationSimulationEligibilityPolicy`.
- Nuevos endpoints protegidos: `GET /api/uat/nacha-inbound-simulator/prenotifications/originated` y `POST /api/uat/nacha-inbound-simulator/prenotifications/eligibility-preview`; ambos solo lectura y UAT/local.
- Elegibilidad estricta exige transacción prenotificación saliente, cámara y entidad coherentes, trace y correlación, archivo con exportación transmitida/reconocida, único intento actual pendiente con deadline y sin respuesta previa. ACHCOL/CENIT separados por reglas; CENIT devolución misma fecha valor.
- **`BusinessEligible=true` NO habilita ejecución**. `ExecutionEnabled=false`; no se crea archivo ni cambia estado. No se reserva idempotency key ni se afirma garantía de concurrencia: I03 lo implementará dentro de una transacción con constraint UNIQUE.
- Resultado de pruebas: 26/26 `PASS_STATIC` y grafo proyectos 7/7; `dotnet` no está instalado: build/Xunit/API/BD `NO_EJECUTADA_POR_AMBIENTE`, no reportar falsamente PASS.
- Ruta de evidencia: `docs/implementacion-simulador-prenotificaciones/INFORME_I02.md`, `CONTRATO_ELEGIBILIDAD_I02.md`, `EVIDENCIA_I02.txt`, `scripts/test/prenotification_simulator_i02_static_check.py`.
- **Siguiente: I03**, reserva idempotente y retorno NACHA-M homologado. Usar I02 ZIP completo como base única.

## Estado específico de I03 (2026-11-02)

- Nuevo contrato de ejecución `POST /api/uat/nacha-inbound-simulator/prenotifications/generate-return` y `INachaInboundSimulationService.GeneratePrenotificationReturnAsync`, guardados por TestTools.Access + NachaSimulatorGenerateDifferential.
- Valida contrato, elegibilidad I02, causal y perfil homologado RETORNO/ENTRADA; crea NACHA-M mediante `BuildOfficialReturnFileAsync`, monto cero, correlación y trace nuevo, sin autoimportar ni alterar estados.
- Reserva `SimulationId` determinista SHA-256 con índice UNIQUE existente y huella de payload; usa transacción serializable para revalidación, bloqueo de archivos previos, consecutivos, escritura y evidencia. Intenta borrar el directorio físico ante rollback. No crear archivo positivo de aceptación tácita.
- Ruta genérica escenario 10 continúa bloqueada; endpoint dedicado I03 es el único que puede generar retornos de prenotificaciones.
- `PASS`: proyecto 7/7, verificaciones I01 20/20, I02 26/26, I03 31/31 `PASS_STATIC`. `NO_EJECUTADA_POR_AMBIENTE`: .NET SDK, tests C#, archivos reales, BD concurrency, importación NachaUpload. **No homologado**.
- Riesgos: bloqueo en concurrencia debe verificarse en SQL Server/PostgreSQL; fallos bruscos pueden dejar archivos físicos huérfanos; verificar semántica NACHA real y el impacto de la auditoría del builder en transacción.
- Evidencia: `docs/implementacion-simulador-prenotificaciones/INFORME_I03.md`, `CONTRATO_GENERACION_I03.md`, `EVIDENCIA_I03.txt`, `scripts/test/prenotification_simulator_i03_static_check.py`.
- Próximo I04: importación manual NachaUpload, correlación y resultados idempotentes.

## Estado específico de I04 (2026-11-02)

- Nueva guarda `IncomingPrenotificationReturnGuard` en la entrada NachaUpload, ANTES de la transición; no es un segundo aplicador.
- Correlación por trace original/adenda 99, monto cero, cámara, intento vigente, estado terminal y procedencia del archivo I03 por SHA-256.
- Hallazgo reparado: el hash `IngestAsync` es hexadecimal **minúsculo** y el hash I03 **mayúsculo**; se normaliza a mayúsculo antes de comparar. No confiar en nombre editable del archivo.
- El motor `AchStateTransitionService` ya sincroniza validación, tercero y estado; se reutiliza. Se ajustó catalogación/prueba CENIT con el flag `IsPrenotification` para originales cuyo `Type` es Credit o Debit.
- `PASS_STATIC`: I04 31/31; I01 20/20; I02 26/26; I03 31/31. Grafo de 7 proyectos PASS. Nuevos 12 casos Xunit de política y 2 pruebas de orquestación incorporados, **NO EJECUTADOS**.
- `NO_EJECUTADA_POR_AMBIENTE`: dotnet build/test, fixtures NACHA-M, E2E ACHCOL/CENIT, concurrency DB, comprobación de hash tras desencripción. No homologado.
- Evidencias y contrato: `docs/implementacion-simulador-prenotificaciones/{INFORME_I04.md,CONTRATO_INTEGRACION_I04.md,EVIDENCIA_I04.txt}` y `scripts/test/prenotification_simulator_i04_static_check.py`.
- **Próximo I05**: simular vencimiento SIN devolución ni archivo positivo; usar reloj de prueba aislado y `AchTacitAcceptanceJobHandler`. Continuar en chat aunque el ambiente carezca de .NET.

## Estado específico de I06 (2026-11-02)

- Código SPA implementado en la pantalla original, sin dependencias, rutas ni submódulos nuevos. Tres pasos: consulta por cámara/referencia, selección y tipo de respuesta, preflight/confirmación.
- Devolución: `GET originated`, `POST eligibility-preview`, `POST generate-return` con causal y ciclo desde API; se descarga blob JWT y sigue NachaUpload manual. Sin importación automática.
- Sin devolución: `POST tacit-acceptance/preview` con hora virtual opcional y `apply-due` solo si reloj REAL + API cumplen elegibilidad. Nunca NACHA positivo ficticio.
- Selección individual intencional para evitar errores de banca/ciclos y no sobreingeniería. Se oculta opción legado del selector, pero permanecen los enums/contratos y etiqueta en historial.
- Archivos modificados: service TS, component TS/HTML/SCSS/spec; ver `MANIFIESTO_I06.json`. Nuevo verificador `scripts/test/prenotification_simulator_i06_static_check.py`.
- **PASS_STATIC 32/32 I06 + 144/144 I01–I05**, **PASS** grafo de 7 proyectos. Cinco pruebas Karma incorporadas, no ejecutadas; TypeScript solo comprobó sintaxis.
- `npm ci --offline` falló ENOTCACHED `zone.js@0.16.3`; compilación Angular/Karma/Playwright NO EJECUTADA. No se certifica UX visual, seguridad JWT ni homologación NACHA end-to-end.
- **Próximo I07:** regresión, ejecución de pruebas, cumplimiento de ACHCOL/CENIT, revisión de ciclos reales de devolución, correcciones mínimas y certificación UAT con trazabilidad de pendientes.

## Estado específico I07 (2026-11-02) — ENTREGA ACTUAL

- ZIP completo I07 derivado estrictamente del ZIP I06; mantiene los archivos heredados y añade correcciones/tests/documentos I07. **Usar I07, no I06, como nueva base.**
- Corregido `ListAvailableCyclesAsync` para `IncomingPrenotificationReturn`: los ciclos configurados no requieren retornos previos; no expone ciclos cancelados; marca cantidad de retornos existentes como cero. Otros escenarios sin cambios. Preflight I02 sigue verificando ciclo/fecha.
- SPA: versiones de consulta y de catálogo para descartar respuestas tardías al cambiar cámara/búsqueda/cierre/selección; limpieza de resultados obsoletos, una prenotificación a la vez.
- Preview de aceptación tácita: condiciones `ExecutionEnabled` alineadas con `apply-due`. Reloj virtual sigue siendo solo consulta y no se crea archivo positivo.
- Tests fuente añadidos C# (ACHCOL/CENIT/cancelado) y Angular (cámara, cierre, catálogos). Verificador I07 24/24 `PASS_STATIC`; regresión I01–I06 176/176, acumulado **200/200**; grafo .sln 7/7 PASS. Ninguno es runtime.
- `NO_EJECUTADA_POR_AMBIENTE`: SDK .NET ausente; `npm ci --offline` falla por `zone.js@0.16.3` no cacheado; Angular/Karma/Playwright y UAT/BD no ejecutados. **0/14 PRE homologadas**; no habilitar producción.
- Archivos nuevos: `docs/implementacion-simulador-prenotificaciones/{INFORME_I07.md,EVIDENCIA_I07.txt,PLAN_EJECUCION_UAT_I07.md,I07_CAMBIOS_CODIGO.patch}`, `scripts/test/prenotification_simulator_i07_static_check.py`, `tests/Cfa.ACHInterbank.Tests/NachaInboundSimulatorTests.I07.cs`.
- **Próxima actividad I07-R:** ejecutar pruebas reales y corregir errores demostrados, actualizar `MATRIZ_ACEPTACION.md`, estados y MEMORY_STATE, entregar ZIP completo nuevamente. Evitar sobreingeniería y nuevas funcionalidades.
