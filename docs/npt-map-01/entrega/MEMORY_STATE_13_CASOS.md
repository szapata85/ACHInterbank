# H-01/H-02/H-03 — transporte XSD y NCTAORIG (2026-11-16)

**NO-GO Live.** Aplicada validación final de solicitud contra XSD local en clientes CFA `Proc_Transacciones` y Axon `RegistrarRespuestaTransaccion`, para XML directo y parámetros. Guard Live `NCTAORIG` ahora niega también ausente/vacío/nil mientras no exista homologación de cuenta del originador. Prueba de readiness corregida; otras rutas no modificadas. **38/38** XSD offline, **33/33** controles previos, **21/21** focales offline, **7/7** regresiones históricas Python; verificador NPT anterior **40/42**, discrepancias históricas. SDK dotnet ausente (127); xUnit y compilación NO ejecutados; SQL Server/PostgreSQL y UAT NO ejecutados. No migraciones ni nuevos paquetes. Informe `docs/npt-transport-xsd-h010203/INFORME_H010203.md`.

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

# Nota técnica de continuidad — RES-H02-01/02 (2026-11-15)

Mitigaciones de claim de ciclo y preflight SOAP implementadas, sin migración nueva. Evidencias estáticas y modelos SQLite aprobados; .NET y SQL Server/PostgreSQL/Live/UAT pendientes. NO-GO para SOAP Live. Ver `ACHInterbank_ajustado/docs/hotfix-res-h02-2026-11-15/INFORME_RES_H02_FINAL.md`. Se conserva íntegra la memoria normativa e incremental anterior.

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

# Estado acumulado — hotfixes previos a despliegue, 2026-11-15

**ZIP de origen:** `ACHInterbank_PROYECTO_COMPLETO_I13_ROR_C5_HOTFIX_2026-11-15.zip`; SHA-256 `3600efca392c1664701d066810aa9df11d8f2b55e282e1f18496a76e4b9001aa`. **Hotfixes:** 01 `ACH D01/D02/D04`, 02 `ACH R10 C1–C7`, 03 `CENIT ROR calendario bancario`. Los tres están `IMPLEMENTADOS_PENDIENTES_DE_COMPILACION_Y_PRUEBAS_DINAMICAS`. Se modificaron cuatro fuentes C# y seis archivos de pruebas; detalle por método y riesgos en `ACHInterbank_ajustado/docs/hotfix-pre-despliegue-2026-11-15/INFORME_PRE_DESPLIEGUE.md`. Se mantuvo HC-CENIT-002 (C5 obligatorio), plazo hasta C2 siguiente día hábil, 13 casos históricos y evidencias I00–I13.

**Verificado mediante scripts:** 31/31 focales + 20/20 HC-CENIT-002 + 391/391 aserciones de 12 verificadores históricos y grafo Visual Studio 7/7. **NO ejecutado:** restore/build/xUnit, SQL/DB, UAT, homologación. **Formal:** 0/13 históricos cerrados; NO-GO producción. **Riesgos:** falta compilación, regresión, pruebas de calendarios y operador real. **Siguiente actividad:** G1–G3 pruebas y despliegue técnico; G4 UAT única solo al final.

---

## Historial anterior íntegro

# Corrección puntual posterior a I13 — ROR CENIT: C5 normativo

**Fecha:** 2026-11-15 · **Línea base exclusiva:** `ACHInterbank_PROYECTO_COMPLETO_I13_HC-CENIT-013_2026-11-15.zip` · SHA-256 `d1331e86c4ee0063a86e1ad07b23b1b340efde842f6f179c91f7038919f239cd`. **No se abre un incremento I14.**

Se ha corregido `CenitReturnOfReturnService.ValidateOperationalWindowAsync`: ya no se calcula el último ciclo a partir de `Max()` en `AchCycles`, porque una jornada incompleta (C1–C4) convertía C4 en supuesto último ciclo. La regla intradía exige padre C2/C3/C4 y destino **C5**. Ambas rutas saliente y entrante usan el mismo validador. La regla del segundo ciclo del siguiente día hábil no se modificó. **No existe autorización operacional expresa incorporada en este paquete para sustituir C5**; sin ella se falla en cerrado.

`CenitReturnOfReturn2026Tests.cs` ahora configura C1–C5, actualiza casos positivos para C5, incluye negativos C4 con calendario completo e incompleto, cobertura entrante y matriz C1–C5. Se conservan las pruebas históricas de plazo siguiente día hábil, correlación y R74.

**Evidencia efectivamente ejecutada:** 20/20 suites previas con 542/542 verificaciones estáticas/documentales PASS, más 20/20 comprobaciones focales de fuente PASS. **NO EJECUTADO** .NET SDK, `dotnet restore/build/test`, SQLite/SQL Server/PostgreSQL, concurrentes, integración NACHA-M ni homologación oficial. Pruebas xUnit agregadas: cuatro métodos (ocho variantes), **0 ejecuciones**. Los controles estáticos no prueban compilación ni ejecución. El resultado normativo permanece **0/13 hallazgos formalmente cerrados**, UAT única final, producción **NO-GO**.

Fuente normativa disponible dentro del ZIP I13: `ACHInterbank_ajustado/docs/normativa/md/CENIT-DSP-152-Anexo-2.md` §3 y §§4.3/4.6 (último ciclo intradía y plazo alternativo siguiente día hábil). Se preservan los archivos de ACH Colombia, migraciones, dependencias, contratos, SDK/lockfiles y las correcciones I00–I13.

**Pruebas por ejecutar en ambiente .NET 10:** `dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj --filter FullyQualifiedName~CenitReturnOfReturn2026Tests` desde `ACHInterbank_ajustado/`, con informe TRX, SQLite y PostgreSQL/SQL Server según corresponda. Validar también respuestas reales de operador, fechas valor y correlación del archivo físico. **No declarar cierre formal hasta concluirlo.**

**Rollback:** recuperar exactamente las modificaciones focales desde el ZIP I13 (servicio, clase de pruebas, nueva prueba estática y documentos asociados); antes de despliegue, inspeccionar cualquier ROR histórico generado en C4 por configuraciones incompletas. No revertir movimientos financieros automáticamente.

---

# ACTUALIZACIÓN DE CONTINUIDAD VIGENTE — I13 (2026-11-15)

**Base exclusiva:** `ACHInterbank_PROYECTO_COMPLETO_I12_HC-CENIT-012_2026-11-15.zip` SHA-256 `3b47395c4eb58e984ff259e5b6d6357dc47eaaff071ead7acb74e8fd8f79fdf7`. Integridad ZIP verificada.
**Resultado:** I13 refuerza la correlación transaccional (`TraceNo` canónico), falla seguro ante causales contradictorias para una misma traza y comprueba el hash en choques concurrentes de identidad de respuesta. Se conserva el NACK individual de F02 y la separación del estado de archivo. **542/542 (509 heredados + 33 I13) controles estáticos/documentales PASS** + **8/8 validaciones independientes de fixtures XML**, todas sin ejecución C#. Cinco métodos xUnit agregados, **0 ejecutados**, SDK .NET ausente; pruebas SQL/reintentos multi-worker PT-14/PT-15 y operador pendientes. npm ci offline ENOTCACHED; advisories actuales npm/NuGet NO VERIFICADOS.
**Decisión:** I01–I13 implementados en fuente y todos los HC-CENIT-001..013 **ABIERTOS**; **0/13 cerrados**, UAT consolidada al final, producción NO-GO. **Siguiente fase: G1–G3 técnicas, G4 UAT única, G5 cierre; no hay I14.** Evidencia `EVIDENCIAS/I13/`.

---

# ACTUALIZACIÓN DE CONTINUIDAD VIGENTE — I12 (2026-11-15)

**Base exclusiva:** ZIP I11 SHA-256 `16e0df38b57345ac6aae654df5db158039186a35c954a2bbf79953ce0993a498`.
**Resultado:** I12 CE-01 corregido en lector ADV físico contiguo de 106 posiciones y reconocimiento en `Reconciliation`. Se validan lote/archivo/padding, códigos 81–88, sin efectos CUD. Regresión **468/468** + focal **41/41** = **509/509 estáticos/documentales**. Fixture sintético 16/16; **xUnit 0 ejecutados, .NET SDK ausente**. PT-13/PT-19/PT-23 pendientes, sin homologación ni conciliación Tesorería certificadas. **HC-CENIT-012 ABIERTO; 0/13 cerrados; UAT única al final; producción NO-GO.**
**Próximo:** I13 — HC-CENIT-013, línea base exclusiva ZIP integral I12. Evidencia `EVIDENCIAS/I12/`.

---

# ACTUALIZACIÓN DE CONTINUIDAD VIGENTE — I11 (2026-11-15)

**Base exclusiva:** ZIP integral I10 SHA-256 `16bd09e318c6689a89755332a4d107f5cc94b48b8f5721372761d09539b5fbfa`.
**Estado:** HC-CENIT-011 ABIERTO; corrección I11 implementada con verificación de cámara/perfil y separación de catálogos NACHA/STA. 468/468 regresión + 36/36 focal, controles **estáticos**; 0 pruebas xUnit ejecutadas (SDK no disponible); PT-12 PENDIENTE. STA end-to-end no implementado: no atribuirle homologación. UAT única final; NO-GO.
**Siguiente:** I12 — HC-CENIT-012, base exclusiva ZIP I11, evidencia `EVIDENCIAS/I11/`.

---

# ACTUALIZACIÓN DE CONTINUIDAD VIGENTE — I10 (2026-11-15)

**Base exclusiva:** ZIP integral I09 SHA-256 `af88b09bb1d56f7f28a03b2b7a738da806f3bd9b79f889bfd398503d91e79034`.
**Estado:** HC-CENIT-010 ABIERTO; I10 implementado con verificación técnica pendiente; 470/470 controles estáticos/documentales ejecutados PASS; **0 pruebas xUnit ejecutadas** (SDK ausente); PT-11/PT-20 PENDIENTES. Se escribió una matriz de 256 bytes y cobertura de salida CENIT sin alterar ACH Colombia. UAT reservada al final; NO-GO.
**Siguiente:** I11 — HC-CENIT-011, única base el ZIP íntegro I10. Evidencia `EVIDENCIAS/I10/`.

---

# MEMORY_STATE — ACH Interbank / 13 incrementos CENIT

**Fecha de corte:** 2026-11-15  
**Versión:** PLAN-CENIT-13-V3.13-I12 (dominios de rechazo STA y NACHA-M; PT-12 pendiente)
**Fuente original:** `ACHInterbank_MENU_CAPACIDADES_OCULTAS_V2_LOGS_SOAP_SIN_ENMASCARAR_60DIAS_2026-11-05.zip`  
**SHA-256 fuente:** `18c1893b996210023cb2c29b3c5cdbf65fa8787dc7fa0dd839e4518d1021c05f`  
**Proyecto:** `ACHInterbank_ajustado/`, con correcciones focales I01–I12 y pruebas; base integral I10 preservada.  
**Archivo rector:** `PLAN_IMPLEMENTACION_13_CASOS/PLAN_IMPLEMENTACION_13_CASOS.md`  
**Próximo incremento:** **I13 / HC-CENIT-013**; I01–I11 implementados con comprobación estática, verificaciones ambientales pendientes.  
**Dictamen:** **NO-GO** producción. I01–I10 sin build/xUnit ni PT formal; CONTINUAR I11 sin ocultar riesgos de C5 ni eventos ROR históricos ambiguos.

## Estado canónico

- **13/13** HC-CENIT-001..013 con implementación histórica registrada; I01 con prevalidación C5, I02 con anclaje raw ROR C1, I03 con simulación explícita e I04 con guarda de recepción R23 e I05 con prioridades estrictas del simulador; **0/13** cerrados; todos requieren verificación dinámica.
- **PT-01..PT-22:** 22 pendientes en los registros históricos. **PT-23:** adicional pendiente para ADV físico de 106 caracteres contiguos.
- **CE-01:** defecto adicional estático de parser ADV con `Split('\n')`; se **corrige en I12**, no es un decimocuarto caso CENIT histórico.
- **AC-01/02/03:** diferencias de código ACH Colombia no corregidas aquí. **AC-04:** trazabilidad de rutas pendiente; código solo si bypass probado. **SEC-01:** logging SOAP íntegro requiere aprobación.
- **Controles estáticos históricos:** 137/137 PASS F01–F07; **NO equivalen a dotnet test/build**. PT no ejecutadas en auditoría previa.
- **Documentación normativa adjunta en la anterior entrega:** **excluida** del nuevo ZIP por solicitud del usuario. Se conserva intacto todo el contenido histórico del árbol fuente original, incluido `docs/`.

## Directrices obligatorias ratificadas por el usuario el 14/11/2026

1. **Modelo/método:** ChatGPT en Chat, GPT-6 High **seleccionado en la interfaz por el usuario**; no declarar configuración activa sin comprobación ni paralizar avance por ella.
2. **Sin bloqueo por ambiente:** completar el código y entregar cada ZIP aunque no exista SDK, Visual Studio, DB, Quartz, acceso cámara o red; registrar limitaciones como `NO_EJECUTADA`/`PENDIENTE_VERIFICACION_AMBIENTE`. Un error REAL de compilación o contrato se corrige, no se excusa como ambiente. Liberación de producción sigue `NO-GO` sin evidencia.
3. **UAT al final:** `UAT_ESTADO = RESERVADA_FASE_FINAL` durante I00–I13; ejecutar una UAT consolidada únicamente después de código, regresión y homologación técnica, nunca por incremento.
4. **Calidad:** Clean Architecture, SOLID, DI y patrones apropiados; revisar idempotencia, concurrencia, persistencia, observabilidad segura, rendimiento, backward compatibility, SQL/migraciones, pruebas positivas/negativas y regresión de las dos cámaras.
5. **Dependencias:** buscar últimas versiones **estables y compatibles** de NuGet/npm y evaluar advisories de directas/transitivas en cada incremento; actualizar `.csproj`, `package.json`/`package-lock.json` si es necesario. Mantener audit NuGet y `npm audit`; sin acceso a advisories => `NO_VERIFICADO`, no `SEGURO`. No adivinar versiones ni ignorar CVE.
6. **Orden inalterable:** I00 → I01 → I02 → … → I13. Sin desviaciones por temas laterales; ACH AC-01..04/SEC-01 quedan como pendientes antes de UAT integral.
7. **Entrega por incremento:** ZIP con **todo** `ACHInterbank_ajustado/`, `LEER_PRIMERO.md`, `PLAN_IMPLEMENTACION_13_CASOS/PLAN_IMPLEMENTACION_13_CASOS.md`, `MEMORY_STATE.md` y `MEMORY_STATE.json` actualizados y evidencia sanitizada; SHA e inventario, preservando código y documentos previos. **El ZIP emitido es la base obligatoria del incremento siguiente** al colocarse en `sources`. Sin carpeta extra `DOCUMENTACION_NORMATIVA/`.

**Estados permitidos separados:** `NO_INICIADO` → `EN_IMPLEMENTACION` → `IMPLEMENTADO_PENDIENTE_VERIFICACION_AMBIENTE` o `VERIFICADO_TECNICAMENTE` → `UAT_FINAL_APROBADA` → `CERRADO` (requiere evidencia oficial cuando aplique). No es válido marcar cerrado por entregar código.

**Proceso al retomar:** localizar el ZIP más reciente en `sources` → confirmar hash/estructura → leer este MEMORY_STATE y el PLAN → iniciar estrictamente `next_increment` → modificar código/documentos pertinentes → intentar pruebas técnicas y auditorías de dependencias → actualizar ambos estados → emitir/verificar ZIP íntegro → consignar SHA descargable y `next_increment`.

**Dependencias técnicas observadas en el ZIP:** `global.json` fija .NET SDK 10.0.403 (rollForward latestPatch), solución `ACHInterbank.sln` con .NET 10, Angular (`web/ach-interbank-ui`) y lock npm. `Directory.Build.props` ya tiene NuGetAudit + NU1901-NU1904 como errores. **No se ha demostrado restore/build/auditoría de advisories** en este ajuste documental.

## Incrementos, identificación y seguimiento

| Incremento | Caso | Estado técnico | Pruebas pendientes | Código nuevo |
|---|---|---|---|---|
| I01 | HC-CENIT-001 | **IMPLEMENTADO_PENDIENTE_VERIFICACION_AMBIENTE** | PT-01, PT-02, PT-20, PT-21 | Prevalidación C5 + 5 xUnit sin ejecutar |
| I02 | HC-CENIT-002 | **IMPLEMENTADO_PENDIENTE_VERIFICACION_AMBIENTE** | PT-03 | Anclaje NACHA por evento + 7 escenarios xUnit sin ejecutar |
| I03 | HC-CENIT-003 | **IMPLEMENTADO_PENDIENTE_VERIFICACION_AMBIENTE** | PT-04, PT-05 | Guard CENIT; API/UX no oficial; 4 xUnit, 1 Jasmine no ejecutados |
| I04 | HC-CENIT-004 | **IMPLEMENTADO_PENDIENTE_VERIFICACION_AMBIENTE** | PT-07, PT-17 | 19 xUnit previstos, guarda de fecha recepción; no ejecutados |
| I05 | HC-CENIT-005 | **IMPLEMENTADO_PENDIENTE_VERIFICACION_AMBIENTE** | PT-06 | Política/orden 10→1, negativos, 25 xUnit nuevos + 5 filas, no ejecutados |
| I06 | HC-CENIT-006 | **IMPLEMENTADO_PENDIENTE_VERIFICACION_AMBIENTE** | PT-04, PT-05 | Guarda reintento Quartz y 6 escenarios xUnit no ejecutados |
| I07 | HC-CENIT-007 | **IMPLEMENTADO_PENDIENTE_VERIFICACION_AMBIENTE** | PT-09 | Guarda C4/C5 por día operacional, 21 xUnit no ejecutadas |
| I08 | HC-CENIT-008 | **IMPLEMENTADO_PENDIENTE_VERIFICACION_AMBIENTE** | PT-10 | Validación R74 compartida, adenda 05 original/campo 3 y 15 variantes xUnit no ejecutadas |
| I09 | HC-CENIT-009 | **IMPLEMENTADO_PENDIENTE_VERIFICACION_AMBIENTE** | PT-08 | R23 15 días y fecha operacional, xUnit/SQLite sin ejecutar |
| I10 | HC-CENIT-010 | **IMPLEMENTADO_PENDIENTE_VERIFICACION_AMBIENTE** | PT-11, PT-20 | Guarda CENIT salida y pruebas de matriz de bytes no ejecutadas |
| I11 | HC-CENIT-011 | **IMPLEMENTADO_PENDIENTE_VERIFICACION_AMBIENTE** | PT-12 | Catálogo por cámara/fuente, selector STA puro, xUnit sin ejecutar |
| I12 | HC-CENIT-012 | **IMPLEMENTADO_PENDIENTE_VERIFICACION_AMBIENTE** | PT-13, PT-19, PT-23 | Lector ADV físico 106, xUnit pendientes |
| I13 | HC-CENIT-013 | **NO INICIADO**; base histórica implementada | PT-14, PT-15 | Si prueba falla o evidencia estática lo exige |

## Dependencias críticas y reglas de continuidad

1. I00: EJECUTADO como diagnóstico; ZIP V3 íntegro SHA-256 `99753a4cca023d73670c9c2bcaec5d0428a4349973c87c08c015a98a34f78b5e`, F01–F08 154/154 estáticas PASS, SDK/DB ausentes. Ver `EVIDENCIAS/I00`; **continuar I01**. No generar PASS ficticios.
2. I01–I13: un caso por incremento, sin saltos. Implementar ante fallo de prueba o defecto estático demostrado. Evidenciar positivo, negativo, replay y regresión cuando sea ejecutable; si falta ambiente, avanzar código con deuda explícita; registrar «sin cambio de fuente» si ya cumple.
3. I12: no omitir CE-01. El archivo ADV 280 puede presentarse en registros físicos consecutivos de 106 posiciones y el parser `Split('\n')` debe verificarse/corregirse con PT-23.
4. No confundir ACH Colombia (7 ciclos) con CENIT (5 sesiones), ni códigos STA/NACHA; no inventar conector CUD por neteo/ACK/ADV.
5. Los manuales normativos oficiales no están duplicados en el ZIP nuevo. Pedir confirmación de vigencia/horarios al operador para homologar, especialmente tabla ACH con erratas.
6. Preservar fuente completa e historial `docs/` y `MEMORY_STATE.md` previo; solo editar este nuevo `MEMORY_STATE` durante la implementación.
7. Después de un incremento registrar: `fecha, id, archivos, hash/commit, versiones NuGet/npm, vulnerabilidades consultadas/no verificadas, pruebas PASS/FAIL/NO_EJECUTADA, evidencias, deuda ambiental, revisión técnica, rollback, ZIP integral y siguiente incremento` en PLAN y ambos MEMORY_STATE. UAT se mantiene diferida.
8. El GO integral exige, además de CENIT, resolver AC-01..04, seguridad SOAP, confirmaciones externas, conciliación Tesorería y **UAT consolidada FINAL**. Hasta ello mantener NO-GO; seguir codificando en secuencia.

## Formato histórico de continuación tras I00 (conservado para trazabilidad)

```yaml
incremento_actual: I01
caso_actual: HC-CENIT-001
incremento_previo: I00
estado_i00: DIAGNOSTICADO_CON_VERIFICACIONES_PENDIENTES
fuente_productiva_modificada_en_i00: false
pruebas_estaticas_i00: "154/154 PASS en F01-F08"
xunit_ejecutadas_i00: 0
sdk: AUSENTE
ambiente_db: NO_EJECUTADO
advisories_nuget_npm: NO_VERIFICADOS
uat: RESERVADA_FASE_FINAL
siguiente_accion: evaluar I01 por prueba/contrato e implementar parche mínimo si es requerido
salida: zip integral I00 con plan, memorias, informe y logs; NO-GO producción
```

**Nota histórica V3:** la entrega de planificación inicial no ejecutó pruebas. En V3.1-I00 sí se ejecutaron exclusivamente controles estáticos/documentales, no .NET ni UAT. Todos los casos CENIT permanecen NO_INICIADOS y la UAT sigue reservada para la fase final.


## Registro de ejecución I00

Se verificó SHA del ZIP de entrada y CRC; no se verificó el SHA del ZIP fuente histórico no adjunto. `F01..F08` reejecutados (154/154 estáticos/documentales). `dotnet restore`, `build`, `test` imposibles al faltar SDK; sin TRX. Verificador npm del lock PASS_STATIC; npm ci offline (ENOTCACHED), audit/outdated (EAI_AGAIN), build (ng ausente). Se detectaron 28 registros del lock sin integrity, a revisar sin confundir con CVE. No hubo cambios de código/productos/dependencias. 23 pruebas PT distintas siguen pendientes y 0 de los 13 casos se cierran. Tres checks F08 dependientes de F07 no repetidos. Las “dos guardas históricas” siguen sin identificador que permita reconciliación. `next_increment=I01`; `NO-GO` hasta G1–G5. Evidencia completa `EVIDENCIAS/I00/INFORME_I00.md`.

## Registro I01 — 2026-11-14

- **Base exclusiva:** I00 ZIP SHA-256 `e29571f44ea0f30bf25212357a74e626d7d1311e56adef18fb73b2a480f90a19`; código I01 en `CenitCycleQueueService.cs`; cinco escenarios xUnit añadidos; checker F03 corregido para no señalar lectura como escritura; script estático I01 nuevo. Sin cambios de migración, `csproj`, `package-lock` o ACH Colombia.
- **Defecto estático corregido:** ruta exportación consumía cola C5 antes de validar la clase de sesión. Ahora C5 exige política configurada y clase Return/ReturnOfReturn antes del commit; sesión ambigua falla cerrada. Preserva fecha valor, estado de negocio, lote y auditoría; rollback serializable.
- **Ejecución:** 151/151 F01–F08 estáticas/documentales PASS; 15/15 I01 source guard PASS. **0 xUnit, 0 SQL Server/PostgreSQL/SQLite runtime, 0 Quartz, 0 pruebas de dos workers y 0 CENIT externo**; .NET SDK ausente. `npm ci --offline` ENOTCACHED, build Angular sin `ng`, `npm audit --offline` devuelve 0 de caché sin advisories actuales verificadas, NuGet no evaluable.
- **Pruebas formales:** PT-01, PT-02, PT-20, PT-21 TODAS pendientes; las cinco pruebas C# son diseño ejecutable pendiente de ejecutar. Riesgo vivo: una cola C5 incompatible bloquea la exportación hasta decisión operacional; no hay remapeo automático seguro con fecha valor.
- **Estado:** I01 `IMPLEMENTADO_PENDIENTE_VERIFICACION_AMBIENTE`, caso 001 `ABIERTO`, 13/13 abiertos, UAT `RESERVADA_FASE_FINAL`, producción `NO-GO`. **next_increment = I02**. Evidencia `EVIDENCIAS/I01/INFORME_I01.md`, `RESULTADOS_I01.json`, logs y patch.

### Continuación I02

```yaml
incremento_actual: I02
caso_actual: HC-CENIT-002
incremento_previo: I01
estado_i01: IMPLEMENTADO_PENDIENTE_VERIFICACION_AMBIENTE
closed_cases: 0
ci_dotnet: NO_EJECUTADO_SDK_AUSENTE
i01_static: '166/166 PASS (151 F01–F08 + 15 I01)'
pruebas_pendientes: [PT-01, PT-02, PT-20, PT-21]
uat: RESERVADA_FASE_FINAL
formal_decision: NO-GO
next_increment: I02
```


## Registro I02 — 2026-11-14

- **Base exclusiva:** I01 SHA-256 `ba4e5adddf6747d86af983b64e251e992937ae1e87e1d693609a8e4839e5e0d9`; I02 no modifica archivos externos al ZIP padre ni ejecuta migraciones. Dos archivos fuente nuevos (resolver y checker), tres productivos y dos de pruebas modificados. El resolver CENIT sustituye la elección arbitraria de `LinkedAtUtc` por `incomingReturnLinkId` del evento, validado contra el raw; datos históricos sin pin y más de un candidato fallan cerrados, no seleccionan el más reciente.
- **7 escenarios xUnit añadidos; 0 ejecutados.** Test fuente positivo con dos devoluciones similares y trace diferente, replay, rechazo ambiguo, pin ajeno, validación de generador, enlace cambiado, captura semántica enviada a `INachaFileBuilder`. No son PT-03 aprobadas ni evidencia de archivo oficial. La prueba del constructor con mock no reemplaza golden-master físico ni homologación.
- **Ejecución:** 151/151 checks F01–F08 + 15/15 I01 + 26/26 I02 = 192/192 estáticos/documentales PASS; .NET SDK 10.0.403 ausente, sin restore/build/xUnit/SQL/Quartz/operador. npm ci offline ENOTCACHED, ng ausente; auditoría npm local 0 no prueba CVE actual, npm online EAI_AGAIN, NuGet advisories no verificados; sin cambios de dependencias.
- **Riesgos:** evento histórico sin pin más de un raw => revisión manual; verificar transacciones con dos workers y DB reales, valores y fecha valor en NACHA físico/auditoría; I01 C5 mezcla bloqueante permanece. **HC-CENIT-002 ABIERTO**, 0/13 cerrados, PT-03 pendiente, UAT final reservada, producción NO-GO. `next_increment=I03`. Ver `EVIDENCIAS/I02/INFORME_I02.md` y `RESULTADOS_I02.json`.


## 11. Registro de ejecución I03 (2026-11-14)

- **Base exclusiva:** ZIP I02 SHA-256 `a46913998e9667425e47df8e881c7a42e2fa8c6548a0c8c85781737454b60152`, contenido preservado sin reemplazos ajenos; `HC-CENIT-003` **ABIERTO** y `I03` **IMPLEMENTADO_PENDIENTE_VERIFICACION_AMBIENTE**. F01 ya separaba Quartz de neteo simulado, no se reimplementó.
- **Brechas estáticas remediadas:** `CenitNettingService` permite invocación para otra cámara incluso con snapshot previo; ahora valida CENIT antes de devolver/crear. `CenitOperationsController.net-positions` ahora filtra cámara y marca contractualmente `InternalSimulation`, `representsOfficialSettlement=false`, `officialEvidenceIncluded=false` también sin datos. UX cambia títulos y columna a «simulada (no liquidada)». No produce contabilidad ni estado CUD. Sin migraciones, cambios de paquetes ni modificaciones de la lógica de ACH Colombia.
- **Pruebas observadas:** F01–F08 151/151 + I01 15/15 + I02 26/26 + I03 22/22 = **214/214 PASS exclusivamente estáticas/documentales/fixtures**. 4 xUnit y 1 Jasmine nuevos **0 ejecutados**, parse TypeScript 2/2, .NET SDK/SQL/Quartz/CUD/cámara ausentes; npm ci offline ENOTCACHED, npm audit offline 0 solo según caché, advisories npm/NuGet actuales no verificados. **PT-04/PT-05 PENDIENTES**; DB/ledger y Tesorería requeridos.
- **Riesgos:** consumidores que ignoran metadatos todavía pueden interpretar posición como oficial; inspeccionar en despliegue. Riesgo C5 de I01 y ambigüedad histórica ROR I02 siguen abiertos. UAT final reservada, **0/13** hallazgos cerrados; producción **NO-GO**.
- **Evidencias:** `EVIDENCIAS/I03/INFORME_I03.md`, `RESULTADOS_I03.json`, `CAMBIOS_I03.json`, `CODIGO_CAMBIOS.patch`, logs. **Siguiente I04 / HC-CENIT-004** exclusivamente desde ZIP I03.


## Registro I04 — 2026-11-14

- **Base exclusiva:** ZIP I03 SHA-256 `70ba7ce57fe17aa95fbf874b1f8d7980ee118f3f7c5c89e3fd62da0a5f2874de`. `CenitIncomingReturnPolicy` ahora no considera automáticamente “de días anteriores” una devolución cuyo registro original fue **recibido hoy** pero posee Fecha Valor anterior. La rama de C1 exige recepción anterior a día de devolución; si un recibo es del mismo día, la rama intradía exige Fecha Valor coincidente y ciclo inmediato. Causales R23/R06 separadas. Validación originalCycle<=lastCycle.
- **Pruebas reales:** 238/238 solo estáticas/documentales (151 F01–F08, 15 I01, 26 I02, 22 I03, 24 I04). Se añadieron **19 casos xUnit parametrizados**, **0 ejecutados**; .NET SDK, SQL, Quartz y CENIT externo no disponibles; PT-07/PT-17 pendientes. npm ci offline ENOTCACHED; npm audit offline 0 conforme caché local insuficiente; npm outdated offline falló; vulnerabilidades actuales npm/NuGet no verificadas. No se modificaron dependencias, migraciones, UI ni código ACH Colombia.
- **Gobierno:** HC-CENIT-004 **ABIERTO**, `IMPLEMENTADO_PENDIENTE_VERIFICACION_AMBIENTE`, 0/13 casos cerrados; validar interpretación normativa R23 del mismo día con operador; UAT `RESERVADA_FASE_FINAL`; producción **NO-GO**; preservar riesgos I01–I03. Evidencia `EVIDENCIAS/I04/INFORME_I04.md`, `RESULTADOS_I04.json`, patch y hashes. **Próximo I05 / HC-CENIT-005**, usando ZIP completo I04.


## Registro I05 — 2026-11-14

- **Base única:** ZIP integral I04 SHA-256 `eaaa3f03ce9fe53c6febae7a64baed58415202266cec9126393a9b7232b39930`; sin mezcla de código de otras entregas, sin supresión de fuentes I04.
- **Defectos estáticos y cambio focal:** `TransactionPriorityPolicy` aceptaba códigos de pago/prenotificación como retorno; `LiquidityOptimizationService` procesaba prioridad ascendente (mínima primero), permitía clase 0 consumir y omitía guard por cámara antes de consulta idempotente. Ahora exige código NACHA coherente, orden informativo descendente 10→1 con monto/ID estables y `SimulatedUnclassified` sin consumo; preserva simulación sin aplicación financiera. La `SimulatedUnclassified` se considera evidencia incierta en `AchPendingCycleEligibilityService`, con posible bloqueo conservador de reasignación: verificar con PT-06.
- **Pruebas:** F01–F08 151/151, I01 15/15, I02 26/26, I03 22/22, I04 24/24, I05 32/32 = **270/270 SOLO ESTÁTICAS/DOCUMENTALES**. Clase I05 con 25 xUnit previstos + 5 filas negativas nuevas en clase heredada; **0 ejecutados**. .NET SDK ausente; SQL/Quartz/CUD/cámara NO_EJECUTADOS, npm ci offline ENOTCACHED, npm audit offline 0 solo caché; advisories actuales NuGet/npm NO_VERIFICADOS.
- **Gobierno:** PT-06 y homologación pendientes; HC-CENIT-005 ABIERTO, 0/13 cierres. UAT RESERVADA_FASE_FINAL, producción NO-GO. Riesgos I01–I04 permanecen. Ver `EVIDENCIAS/I05/INFORME_I05.md` y `RESULTADOS_I05.json`; siguiente I06 usando ZIP integral I05.


## Registro I06 — 2026-11-14

- **Base única:** ZIP integral I05 SHA-256 `6816e5507831f9806c907542203a505475930506c687e983064c27f666e4df4b`, con integridad verificable; I01–I05 preservados.
- **Defecto y cambio:** `CenitCycleExecutionService` podía reescribir un `Status` no reconocido/terminal a `AwaitingOfficialResult` en reintento. I06 conserva `CompletedAtUtc` y estados `Completed/AwaitingOfficialResult`; rechaza estado no reconocido en ausencia de finalización. No transforma `SimulatedInsufficientLiquidity` en `ReturnedByOperator`; esa separación histórica ya estaba implementada.
- **Pruebas:** 302/302 fuente/documentales PASS (151 F01–F08 + 119 I01–I05 + 32 I06); 6 variantes xUnit escritas en dos clases heredadas, **0 ejecutadas**. SDK .NET ausente, sin build, SQL/Quartz/operador, PT-04/PT-05 o UAT. npm ci offline ENOTCACHED; advisories NuGet/npm actuales NO_VERIFICADOS. Sin migraciones/dependencias alteradas.
- **Riesgos:** corrida real de reintentos y respuestas oficiales debe demostrar concurrencia e idempotencia con DB. Una respuesta NACK individual no equivale a cierre/liquidación oficial de ciclo. UAT al final, HC-006 ABIERTO, 0/13 cerrados, **NO-GO**. Evidencia `EVIDENCIAS/I06/INFORME_I06.md`; próximo I07 desde ZIP integral I06.


## Registro I07 — 2026-11-15

- **Base única I06:** `ACHInterbank_PROYECTO_COMPLETO_I06_HC-CENIT-006_2026-11-14.zip` SHA-256 `6ea66136355f746709bf028a4dbb229487779869c4809c76230e7f0321847513`; origen completo preservado.
- **Cambio:** C5 R06 se habilita solo para C4 con Fecha Valor compatible, misma fecha operacional original/destino obtenida de `AchCycle.ProcessingDate`; incompleta = revisión manual, distinta = rechazo. Ingesta R06 no confía en un ciclo detectado que contradiga la BD. Sin alteraciones de ACH Colombia productivo, ledger, dependencias o migraciones.
- **Pruebas:** 13 métodos/21 escenarios xUnit escritos, **0 ejecutados**. **339/339** estáticas/documentales PASS (151 F01–F08 + 151 I01–I06 + 36 I07); no equivalen a `dotnet test`. `PT-09` NO_EJECUTADA, archivo físico NACHA-M no construido en ejecución, homologación pendiente.
- **Ambiente:** dotnet SDK 10.0.403, SQL/Quartz/CENIT ausentes; npm ci offline FAIL `ENOTCACHED zone.js`, audit offline 0 solo según cache; paquetes NuGet/npm actuales no verificables. **I07 IMPLEMENTADO_PENDIENTE_VERIFICACION_AMBIENTE, HC-CENIT-007 ABIERTO, 0/13 cerrados, UAT al final, NO-GO.** `next_increment=I08`. Evidencia en `EVIDENCIAS/I07/`.


## Registro I08 — 2026-11-15

- **Base única:** ZIP I07 SHA-256 `97cc3009bb45ad09dd95301136ee052b48b9a293d84513a6067a5d1e43d03d79`. Código anterior y documentación preservados. I08 introduce helper focal CENIT R74; no modifica ACH Colombia.
- **Hallazgo:** la aceptación R74 y su generación validaban por duplicado una adenda tipo 05 sin asegurar siempre la correlación física con el registro **original** de crédito. Ahora se comprueban naturaleza monetaria, código de crédito, enlace final NACHA, trace, importe, campo 3 PPD, ID y reservados; adenda 99 no sirve como evidencia original. Si existe una adenda física incompleta, una referencia local no puede suplantarla. Los originales salientes sin raw usan adenda 05 semántica e ID.
- **Pruebas REALMENTE ejecutadas:** F01–F08 151/151 + I01–I07 188/188 + I08 44/44 = **383/383 estáticas/documentales PASS**. 15 variantes xUnit añadidas, **0 ejecutadas** por falta de SDK; PT-10, DB y homologación pendientes. npm ci offline FAIL ENOTCACHED, advisories actuales NuGet/npm NO_VERIFICADOS.
- **Estado:** HC-CENIT-008 **ABIERTO**, `IMPLEMENTADO_PENDIENTE_VERIFICACION_AMBIENTE`; 0/13 cerrados, UAT una sola vez al final, producción NO-GO. Ver `EVIDENCIAS/I08`; **next_increment=I09**, partir únicamente del ZIP integral I08.


## Registro I09 — 2026-11-15

- **Base única:** ZIP integral I08 SHA-256 `8b54f60eeb7c0a68cab35ccd14bc7bece294e0d6fc198d6604b4bfce2ba97021`; se conservan I00–I08.
- **Código:** política R23 compara plazo de 15 días y siguiente día hábil contra `AchCycle.ProcessingDate` del ciclo receptor, no contra Fecha Valor; falta de día operacional posterior requiere revisión manual; fecha valor posterior no puede ser distinta al día de presentación. Notificación UTC/Local se normaliza a America/Bogota antes de calendario; Unspecified se trata como hora local bancaria. El catálogo genérico exceptúa exclusivamente crédito R23 CENIT del plazo común `Credit MaxDays=1`, sin cambiar ACH Colombia ni otras causales. Pruebas xUnit/SQLite y checker focal incorporados.
- **Pruebas:** resultados de fuente (ver `EVIDENCIAS/I09/static_runs.json`) únicamente estáticos/documentales; C# restore/build/xUnit y SQLite **0 ejecutados** por ausencia de SDK. SQL/Quartz/CENIT/operador/UAT sin ejecutar. npm ci --offline ENOTCACHED zone.js; NuGet/npm advisories actuales no verificadas. PT-08 pendiente.
- **Pendientes críticos:** auditoría de origen de notificación del Receptor y fecha efectiva de ingreso al Participante Receptor, calendario bancario homologado, archivo NACHA, DB real e idempotencia. HC-CENIT-009 ABIERTO, 0/13 cierres; producción NO-GO, UAT al final.
- **Siguiente:** I10 / HC-CENIT-010, usar exclusivamente ZIP integral I09. Evidencias `EVIDENCIAS/I09/`.
