# Incremento NPT_PRE_XSD_FINAL — código focal antes de XSD

**Implementado en fuente, no habilitado productivamente:** NPT-01 F4.2 contexto estricto file-based; NPT-02 bloqueo físico diferencial addenda 99 sin adaptador y excepción normativa CENIT F07; NPT-05 mapping candidato de origen real con guardia contractual; NPT-11 correlación con vínculo original sin inventar archivo/fecha. Informe `ACHInterbank_ajustado/docs/npt-pre-xsd-final/INFORME_NPT_PRE_XSD_FINAL.md`.

**Pendiente obligatorio:** ejecutar .NET 10 restore/build/xUnit, regresión completa en SQL Server/PostgreSQL, obtención oficial WSCFAACH XSD y homologación F5/NCTAORIG, layouts/vectores diferenciales de cada cámara, sin UAT hasta fase final. NO-GO Live. 31/31 fuente/WSDL metadatos, pero no prueba runtime.

---

# Anexo — cierre focal de siete NPT (2026-11-15)

**Fase C0 código entregado:** NPT-03,04,06,08,09,10,11: fencing de finalizaciones de CFA/Axon, cuarentena watchdog, captura de evidencia tardía, errores posteriores al SOAP, respuesta SOAP estricta y correlación NACHA física. Pruebas C# incorporadas, no compiladas en este entorno; sin migraciones nuevas. **NO-GO**.

**G1 pendiente:** .NET 10 restore/build/test; corregir fallos reales antes de considerar cerrado un hallazgo. **G2 pendiente:** dos bases SQL Server/PostgreSQL y migraciones. **G3 pendiente:** dos workers/hubs Quartz, SOAP simulator instrumentado con contador de envíos, fallos/crashes, watchdog reiniciando host, 38 escenarios E2E. **G4 futuro:** homologación contractual y UAT final, fuera de este incremento.

NPT-01/02/05/14 no tocados, NPT-07/12/13 preservados. Informe completo en `ACHInterbank_ajustado/docs/cierre-focal-npt-2026-11-15/INFORME_CIERRE_FOCAL_NPT.md`.

---

# Actualización de plan NPT — 15/11/2026

Incremento focal de fuente implementado con NO-GO. Dependencias externas NPT-01/02/05/14 conservadas, seguridad monetaria aplicada para NPT-03/04/06/07/08/09/10/11/12/13. Para cerrar: ejecutar los 38 escenarios con carreras/crash/rollback en SQL Server/PostgreSQL y SOAP local instrumentado, conseguir XSD y homologación; UAT sólo al final. Véase docs/cierre-npt-2026-11-15/INFORME_IMPLEMENTACION_NPT_01_14.md.

---

# Anexo de continuidad 2026-11-15 — RES-H02-01/02 (pre SOAP Live)

Implementación focal: guardas de ventana y ciclo pos-intención SOAP, claim Quartz vinculado a `AchCycleId` y aborto previo al transporte `NotExecuted`. 27/27 verificaciones estáticas/SQLite, 12/12 regresiones y grafo 7/7; .NET, pruebas SQL Server/PostgreSQL, SOAP y UAT no ejecutadas. **NO-GO** hasta G1–G3. Plan detallado: `ACHInterbank_ajustado/docs/hotfix-res-h02-2026-11-15/PLAN_IMPLEMENTACION_RES_H02.md`. No altera los 13 hallazgos CENIT ni la fase E2E-03.

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

# Registro de hotfixes predespliegue — posterior a I13 (2026-11-15)

**Línea base:** `ACHInterbank_PROYECTO_COMPLETO_I13_ROR_C5_HOTFIX_2026-11-15.zip`; SHA-256 `3600efca392c1664701d066810aa9df11d8f2b55e282e1f18496a76e4b9001aa`. Este bloque es un addendum sobre el historial I00–I13, NO es un incremento I14; se mantienen sus estados originales y las verificaciones de cierre pendientes.

| Hotfix | Estado anterior | Cambio aplicado | Estado posterior | Pruebas y evidencia |
|---|---|---|---|---|
| HOTFIX-01 ACH D01/D02/D04 | Defecto detectado en auditoría integral 2026-11-15 | Reparadas atribuciones normativas D01/D02/D04, validación efectiva de fechas y monto en prenotificación, aislamiento CENIT | `IMPLEMENTADO_PENDIENTE_COMPILACION_XUNIT` | `NachaInboundValidationSeparationTests.cs` y tres pruebas de fixtures actualizadas; estáticas focales PASS, xUnit no ejecutado |
| HOTFIX-02 ACH R10 C1–C7 | Orden erróneo por hora | Orden absoluto por inicio en el día anterior para C1, conservación de `MaxCycles` y `AllowsReturn` | `IMPLEMENTADO_PENDIENTE_COMPILACION_XUNIT` | `AchReturnsFileByClearingHouseTests.cs` con matrices C1–C7 y medianoche; xUnit no ejecutado |
| HOTFIX-03 CENIT ROR calendario | C5 ya corregido por HC-CENIT-002, falta validar fecha hábil | Reusa `IOperationalCalendarService` en rama C5 y siguiente día; C5 y C2 próximo día preservados | `IMPLEMENTADO_PENDIENTE_COMPILACION_XUNIT` | `CenitReturnOfReturn2026Tests.cs` con sábado/domingo/festivo/hábil; xUnit no ejecutado |

**Archivos fuente:** `ACHInterbank_ajustado/src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/{NachaInboundValidation,NachaParserService,AchReturnEligibilityPolicyComponent,CenitReturnOfReturnService}.cs`. **Sin migraciones, cambios en NuGet, esquemas, UI ni desarrollo I14.**

**Ejecución:** 31/31 estáticas focales; 20/20 estáticas HC-CENIT-002; 12/12 verificadores CENIT (391/391 aserciones fuente); grafo solución 7/7. SDK .NET 10 no disponible, así que `restore`, `build`, `test`, xUnit, integración multidatabase y UAT **NO EJECUTADOS**. No se ha cerrado formalmente ningún HC-CENIT. Riesgos: compilación y regresión dinámica pendientes; cobertura de calendarios y esquema ACH con archivos reales.

**Siguiente actividad:** desde `ACHInterbank_ajustado/`, dotnet restore, build Release, tests focales y regresión total .NET 10.0.403 con TRX; continuar despliegue técnico para pruebas. UAT reservada para final, producción NO-GO.

**Informe técnico ampliado:** `ACHInterbank_ajustado/docs/hotfix-pre-despliegue-2026-11-15/INFORME_PRE_DESPLIEGUE.md`.

---

## Historial y plan original I00–I13 íntegros

# Corrección puntual posterior a I13 — ROR CENIT: C5 normativo

**Fecha:** 2026-11-15 · **Línea base exclusiva:** `ACHInterbank_PROYECTO_COMPLETO_I13_HC-CENIT-013_2026-11-15.zip` · SHA-256 `d1331e86c4ee0063a86e1ad07b23b1b340efde842f6f179c91f7038919f239cd`. **No se abre un incremento I14.**

Se ha corregido `CenitReturnOfReturnService.ValidateOperationalWindowAsync`: ya no se calcula el último ciclo a partir de `Max()` en `AchCycles`, porque una jornada incompleta (C1–C4) convertía C4 en supuesto último ciclo. La regla intradía exige padre C2/C3/C4 y destino **C5**. Ambas rutas saliente y entrante usan el mismo validador. La regla del segundo ciclo del siguiente día hábil no se modificó. **No existe autorización operacional expresa incorporada en este paquete para sustituir C5**; sin ella se falla en cerrado.

`CenitReturnOfReturn2026Tests.cs` ahora configura C1–C5, actualiza casos positivos para C5, incluye negativos C4 con calendario completo e incompleto, cobertura entrante y matriz C1–C5. Se conservan las pruebas históricas de plazo siguiente día hábil, correlación y R74.

**Evidencia efectivamente ejecutada:** 20/20 suites previas con 542/542 verificaciones estáticas/documentales PASS, más 20/20 comprobaciones focales de fuente PASS. **NO EJECUTADO** .NET SDK, `dotnet restore/build/test`, SQLite/SQL Server/PostgreSQL, concurrentes, integración NACHA-M ni homologación oficial. Pruebas xUnit agregadas: cuatro métodos (ocho variantes), **0 ejecuciones**. Los controles estáticos no prueban compilación ni ejecución. El resultado normativo permanece **0/13 hallazgos formalmente cerrados**, UAT única final, producción **NO-GO**.

Fuente normativa disponible dentro del ZIP I13: `ACHInterbank_ajustado/docs/normativa/md/CENIT-DSP-152-Anexo-2.md` §3 y §§4.3/4.6 (último ciclo intradía y plazo alternativo siguiente día hábil). Se preservan los archivos de ACH Colombia, migraciones, dependencias, contratos, SDK/lockfiles y las correcciones I00–I13.

**Pruebas por ejecutar en ambiente .NET 10:** `dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj --filter FullyQualifiedName~CenitReturnOfReturn2026Tests` desde `ACHInterbank_ajustado/`, con informe TRX, SQLite y PostgreSQL/SQL Server según corresponda. Validar también respuestas reales de operador, fechas valor y correlación del archivo físico. **No declarar cierre formal hasta concluirlo.**

**Rollback:** recuperar exactamente las modificaciones focales desde el ZIP I13 (servicio, clase de pruebas, nueva prueba estática y documentos asociados); antes de despliegue, inspeccionar cualquier ROR histórico generado en C4 por configuraciones incompletas. No revertir movimientos financieros automáticamente.

---

# PLAN DE IMPLEMENTACIÓN — 13 CASOS CENIT, UN INCREMENTO POR CASO

**ACH Interbank | Fecha de corte:** 2026-11-15  
**Versión:** 3.14-I13 / NACK por transacción reforzado; pruebas dinámicas pendientes / ZIP integral I12 como base única  
**Árbol fuente preservado:** `ACHInterbank_ajustado/` del ZIP `ACHInterbank_MENU_CAPACIDADES_OCULTAS_V2_LOGS_SOAP_SIN_ENMASCARAR_60DIAS_2026-11-05.zip`  
**SHA-256 fuente:** `18c1893b996210023cb2c29b3c5cdbf65fa8787dc7fa0dd839e4518d1021c05f`  
**Estado:** I00 DIAGNOSTICADO; I01–I13 IMPLEMENTADOS_PENDIENTES_VERIFICACION_AMBIENTE; siguientes G1–G3 técnicos, G4 UAT única, G5 cierre. I01 no está certificado y HC-CENIT-001 sigue ABIERTO; NO-GO de producción. El avance de implementación no se bloquea por indisponibilidad de ambientes; la liberación sí exige evidencia.

## 1. Cómo interpretar este plan

El usuario ha solicitado **13 casos, incremento por incremento**. Por eso I01–I13 corresponden uno a uno a `HC-CENIT-001`…`HC-CENIT-013`, sin agrupaciones. I00 es un diagnóstico técnico no bloqueante (no cuenta como caso) y G1–G5 son controles de cierre (no son incrementos adicionales). El caso 12 incorpora la brecha nueva **CE-01** del parser físico ADV; no se oculta como prueba pendiente.

**Estado actualizado al 14/11:** I01 incorpora prevalidación C5 e I02 fija el origen raw exacto de ROR C1 sin cierre normativo. Las implementaciones históricas siguen presentes; **0/13 se consideran cerradas**; las 22 pruebas CENIT PT-01..22 siguen sin ejecutarse y PT-23 es nueva. En cada incremento se revisa primero la implementación existente. Se corrige código cuando haya una prueba fallida **o una incompatibilidad comprobable por inspección estática/contrato**; si la infraestructura impide ejecutar la prueba, se implementa y se deja la verificación marcada como pendiente, nunca como aprobada. CE-01 en I12 ya tiene brecha estática identificada. No se reescribe el proyecto por principio.

**Especificaciones:** se utilizan las referencias normativas como criterios de comprobación, pero los PDF/Markdown normativos **no se duplican en este ZIP**. Las fuentes permanecen bajo custodia del proyecto/equipo; las referencias internas del árbol original se conservan como parte de la integridad del código. No se declara que una norma esté vigente externamente sin confirmación del operador.

## 1.1. Reglas rectoras obligatorias para TODAS las entregas (solicitud del usuario)

**R1 — Modalidad de trabajo.** Trabajar en **ChatGPT, modalidad Chat, modelo GPT-6 con esfuerzo de razonamiento High** cuando el usuario seleccione esa opción en el cliente. La selección de esfuerzo es un control de la interfaz del usuario: el asistente no puede activarla desde el repositorio ni certificar una selección que no verificó. No condicionar la continuidad del desarrollo a esta configuración.

**R2 — Ambientes no bloquean el desarrollo.** Ausencia de SDK, Visual Studio, Docker, SQL Server/PostgreSQL, Quartz, acceso bancario, credenciales, red o dependencias de laboratorio **no suspende la implementación**. Ejecutar todo lo verificable (inspección estática, tests con dobles, fixtures, compilación si está disponible) y continuar al incremento siguiente en el orden pactado. En el registro de cada caso separar `IMPLEMENTADO_PENDIENTE_VERIFICACION_AMBIENTE`, `VERIFICADO_TECNICAMENTE` y `CERRADO`. Prohibido presentar pruebas no ejecutadas como PASS. El faltante se traslada al registro de deuda de verificación y se resuelve en las puertas finales; **GO de producción sí queda impedido** mientras haya pruebas críticas, vulnerabilidades relevantes o UAT pendientes.

**R3 — UAT exclusivamente al final.** Durante I00–I13: pruebas unitarias, de componentes, contratos, integración técnica reproducible y regresión que el entorno permita; **NO convocar, ejecutar ni exigir firma UAT por incremento**. Tras terminar los 13 incrementos y remediaciones transversales definidas para el proyecto integral: ejecutar batería de regresión/homologación técnica, luego **una fase UAT consolidada** con negocio y participantes autorizados; registrar sus incidencias y revalidar, después decidir GO/NO-GO. Homologación técnica con la cámara no equivale a UAT de negocio.

**R4 — Calidad de implementación y arquitectura.** Respetar las capas y dependencias de arquitectura limpia (Dominio sin referencias a infraestructura, Aplicación depende de contratos, Infraestructura implementa adaptadores, API/UI son entradas); SOLID, SRP, DI, inmutabilidad razonable, separación de responsabilidades y patrones usados **solo cuando reducen acoplamiento** (Strategy para reglas por cámara/ciclo, Adapter para conectores, Factory cuando hay variantes legítimas, Repository/UoW si ya encajan; evitar abstracciones ornamentales). Revisión obligatoria de manejo de errores, idempotencia, atomicidad transaccional, concurrencia, trazabilidad, rendimiento, límites de memoria, validación de entradas, seguridad, compatibilidad y migraciones. La calidad prevalece sobre agregar funciones o refactorizar innecesariamente. No degradar ACH Colombia al corregir CENIT.

**R5 — Dependencias modernas, seguras y reproducibles.** En cada incremento inventariar los proyectos `.csproj`, referencias transitivas NuGet, `package.json` y `package-lock.json`. Consultar las versiones **estables más recientes disponibles y compatibles** (avisos oficiales NuGet/npm, compatibilidad .NET 10/Angular, soporte upstream, CVE/GHSA) antes de elegir cambios; **no afirmar "última versión" sin verificación externa actual**. Eliminar/migrar paquetes obsoletos o vulnerables en el ámbito del incremento, o registrar una remediación transversal priorizada si requiere migración mayor. No usar versiones flotantes ni ejecutar `npm audit fix --force` indiscriminadamente; fijar versiones, actualizar lockfiles y verificar resolución transitoria. Si el servicio de advisories está inaccesible, `VULNERABILIDADES_NO_VERIFICADAS`, no `SIN_VULNERABILIDADES`. No instalar dependencias ocultas ni suprimir alertas con excepciones sin evaluación y justificación explícita.

**R6 — Disciplina de plan y secuencia.** No saltar incrementos ni abrir nuevos grandes frentes. Orden fijo **I00 → I01 → … → I13**; I00 es baseline **no bloqueante**. En cada caso revisar código existente → definir defecto/criterios → parche mínimo (si requerido) → pruebas técnicas posibles → revisión de dependencias/arquitectura → actualización documental → ZIP integral → avanzar. Si aparece un defecto ajeno al caso, registrar con severidad, pero no desviar el incremento salvo riesgo inmediato de corrupción financiera o secreto expuesto. Los pendientes ACH Colombia AC-01..04 y SEC-01 siguen visibles y se abordan antes de UAT integral, sin adulterar el contador de 13 casos CENIT.

**R7 — Entrega física de un ZIP integral por incremento ejecutado.** Al terminar **cada** I00–I13 emitir un ZIP completo y autocontenido, no solo `diff`: (a) todo `ACHInterbank_ajustado/` con el código real y sus archivos modificados, migraciones y lockfiles; (b) `PLAN_IMPLEMENTACION_13_CASOS/PLAN_IMPLEMENTACION_13_CASOS.md` actualizado con el resultado del caso; (c) `MEMORY_STATE.md` y `MEMORY_STATE.json` sincronizados; (d) `LEER_PRIMERO.md` con versión e incremento; (e) evidencia técnica **sanitizada** mínima, hashes y lista de archivos alterados. Mantener nombres/estructura de documentos y los históricos existentes. El nuevo ZIP pasa a ser **la única base para el próximo incremento** cuando el usuario lo coloque en `sources`; nunca volver a un ZIP anterior ni perder modificaciones de otro incremento. Excluir la carpeta duplicada `DOCUMENTACION_NORMATIVA`, SDK instalados, `node_modules`, credenciales, secretos y respuestas bancarias sensibles; conservar documentos históricos **que ya formen parte del repositorio fuente**. Verificar integridad ZIP, archivo presente, JSON válido, conteo y SHA-256 antes de compartir.

### Contrato de calidad reproducible por incremento

1. **Restauración/compilación:** verificar `global.json` y Visual Studio compatible con .NET 10 SDK **10.0.403** (o patch soportado conforme a `rollForward`); revisar referencias de proyecto, restores, feeds públicos/privados, target frameworks, workloads y artefactos de migración. Cuando se disponga de herramientas: `dotnet restore ACHInterbank.sln`, `dotnet build ACHInterbank.sln -c Release --no-restore`, `dotnet test ACHInterbank.sln -c Release --no-build` (usar la sintaxis/test runner compatible y capturar TRX si está soportado). Si restore/build falla por fuente o código, **corregir el fallo del código/dependencias dentro del alcance**; si es infraestructura externa, documentar causa exacta y continuar sin inventar resultado.
2. **Seguridad NuGet:** aprovechar `Directory.Build.props` existente (`NuGetAudit=true`, `NuGetAuditMode=all`, NU1901–NU1904 como errores). Ejecutar, cuando sea viable, `dotnet package list --vulnerable --include-transitive` y `dotnet package list --outdated --include-transitive`; confirmar advisories actuales antes de upgrade. Revisar que los paquetes realmente restauren y compile la solución; exigir versiones estables compatibles, no saltos de major ciegos.
3. **Frontend reproducible:** en `web/ach-interbank-ui`, `npm ci`, `npm audit --audit-level=low`, `npm outdated`, `npm run build` y pruebas automáticas aplicables; si se modifica el grafo npm, actualizar **ambos** `package.json`/`package-lock.json`, validar overrides y no mezclar cambios Angular no relacionados con el caso.
4. **Análisis técnico:** revisar warnings, nullable/analizadores activos, seguridad de logs/PII, DI, pruebas positivas/negativas y límites de protocolos; cuando falten DB/Quartz/cámara, sustituir por doubles para **tests técnicos locales**, anotando que no demuestran integración/homologación real.
5. **Cierre de incremento vs cierre normativo:** se permite `INCREMENTO_ENTREGADO_CON_PRUEBAS_PENDIENTES` con código y ZIP íntegros; **no** se permite `CASO_CERRADO` ni `GO` sin ejecutar/verificar controles obligatorios. `UAT = RESERVADA_FASE_FINAL` en todos los casos I00–I13.

### Checklist de entrega exacta (todos los Ixx)

- Matriz `archivo → cambio → por qué → riesgo → prueba` y versión/estado de paquetes directos y transitivos; no atribuir a ambiente un error de código.
- Lista precisa de `PASS/FAIL/NO_EJECUTADA`, comando, SDK/Node/Visual Studio, exit code, logs o ausencia documentada.
- Validación positiva + negativa + idempotencia/concurrencia cuando aplique y control de no regresión en ambas cámaras.
- Actualización de **ambos** archivos de memoria, próximo `Ixx`, dependencias de entorno para verificación final y decisión local honesta.
- Nuevo ZIP autocontenido con fuente completa y documentación igual de actualizada; cotejar conteo/CRC/SHA y excluir únicamente carpetas de normativa adicional y datos secretos.

## 2. Matriz ejecutiva de 13 incrementos

| Incremento | Caso | Prioridad | Tema | Prueba(s) | Trabajo inicial | Estado |
|---|---|---|---|---|---|---|
| I01 | HC-CENIT-001 | P1 | Incorporación de cola entre sesiones | PT-01, PT-02, PT-20, PT-21 | Parche C5 pre-commit + cinco xUnit creados | IMPLEMENTADO, VERIFICACIÓN PENDIENTE |
| I02 | HC-CENIT-002 | P1 | Correlación de devolución de devolución (ROR) en C1 | PT-03 | Enlace raw anclado al evento, rechazo de ambiguos + 7 xUnit previstos | IMPLEMENTADO, VERIFICACIÓN PENDIENTE |
| I03 | HC-CENIT-003 | P1 | Separar neteo simulado de liquidación oficial | PT-04, PT-05 | Corrección CENIT + API/UX, 4 xUnit y 1 Jasmine; 22/22 estáticas, ejecución real pendiente | Implementado, ABIERTO |
| I04 | HC-CENIT-004 | P1 | Regla R23 para devoluciones del mismo día | PT-07, PT-17 | Brecha de fecha de recepción corregida; 19 casos xUnit escritos sin ejecutar | Implementado, pendiente verificación ambiental |
| I05 | HC-CENIT-005 | P2 | Prioridades del simulador PPD/CCD/CTX | PT-06 | 10→1 para candidatos; SEC/códigos estrictos, nivel 0 no consume; 25 xUnit nuevos + 5 filas negativas añadidas | Implementado, pendiente verificación ambiental |
| I06 | HC-CENIT-006 | P1 | Rechazo por liquidez: la simulación no decide | PT-04, PT-05 | Protección de reintentos Quartz y estados finales; 6 xUnit no ejecutadas | Implementado, pendiente verificación ambiental |
| I07 | HC-CENIT-007 | P1 | R06 en quinta sesión CENIT | PT-09 | Día operacional de ambos AchCycle, evidencia R06 en ingesta y retorno; 21 xUnit no ejecutadas | Implementado, pendiente verificación ambiental |
| I08 | HC-CENIT-008 | P1 | ROR causal R74 y adenda requerida | PT-10 | Evidencia de crédito y adenda original centralizada; pruebas escritas, no ejecutadas | Implementado; verificación pendiente |
| I09 | HC-CENIT-009 | P1 | Plazos R23: 15 días calendario y siguiente hábil | PT-08 | Reloj del ciclo real, fecha de notificación Bogotá y excepción preliminar exclusiva CENIT R23 corregidos; xUnit/SQLite escritos sin ejecutar | Implementado, pendiente verificación ambiental |
| I10 | HC-CENIT-010 | P2 | Alfabeto NACHA-M específico de CENIT | PT-11, PT-20 | Guarda de salida CENIT antes de hash + pruebas | Implementado, verificación pendiente |
| I11 | HC-CENIT-011 | P1 | Separar errores de STA y NACHA-M | PT-12 | Catálogos separados por cámara/servicio; pruebas escritas, no ejecutadas | Implementado; verificación pendiente |
| I12 | HC-CENIT-012 | P1 | ADV 280, códigos 81–88 y compatibilidad física CE-01 | PT-13, PT-19, PT-23 | Parser físico CE-01 + pruebas, sin ejecución .NET | Implementado; verificación pendiente |
| I13 | HC-CENIT-013 | P1 | NACK por transacción, no rechazo global del archivo | PT-14, PT-15 | Guarda de traza canónica, motivos contradictorios y conflicto de fuente; 5 xUnit nuevos, 0 ejecutados | Implementado, ABIERTO; verificación pendiente |

## 3. I00 — línea base reproducible, NO BLOQUEANTE

I00 no cuenta entre los 13 casos. Se ejecuta primero como diagnóstico y preparación; **no es puerta de bloqueo por ambiente**. Los errores reproducibles de código/dependencias se corrigen; carencias exclusivamente ambientales se anotan y el plan continúa con I01.

1. Verificar hash de fuente, `global.json` (**.NET SDK 10.0.403**), `dotnet --info`, solución, NuGet, Node/Angular, dependencias y `git status` si existe Git. Congelar ZIP original y registrar rama/commit.
2. Ejecutar `dotnet restore`, `dotnet build -c Release` y `dotnet test` con registros TRX y retorno real; activar DB SQL Server/PostgreSQL de acuerdo al test, migraciones, Quartz y reloj/calendario Bogotá. Un script estático no sustituye compilación.
3. Ejecutar validadores estáticos F01–F08 y reconciliar las dos guardas históricas fallidas; archivar resultados como estado baseline, sin convertirlos automáticamente en incumplimientos normativos.
4. Inventariar rutas reales de entrada, preparación, publicación, respuesta, conciliación y reintento. Confirmar versión documental de cada servicio con las cámaras antes de homologar.
5. Crear `EVIDENCIAS/I00/` fuera del repositorio si contiene raw o datos personales; archivos de prueba anonimizados con hash, fecha, entorno y operador.

**Criterio de salida para avanzar a I01:** inventario y baseline ejecutado hasta donde sea posible, causas precisas de faltantes, herramientas/dependencias identificadas y lista de pruebas técnicas pendientes. Si falta ambiente, marcar I00 **DIAGNOSTICADO_CON_VERIFICACIONES_PENDIENTES** y **continuar I01**; no marcar build/tests/UAT como realizados. La aceptación final permanece pendiente.

## 4. Ejecución detallada — trece casos independientes

**Plantilla común por incremento:** (a) contrastar regla y código vigente; (b) definir escenarios positivos/negativos antes del cambio; (c) corregir ante defecto probado por prueba **o evidencia estática inequívoca**, aunque falte un ambiente externo; (d) ejecutar pruebas técnicas y regresión disponibles, marcar las demás NO_EJECUTADA; (e) restaurar/compilar/analizar dependencias modernas y vulnerabilidades cuando sea viable; (f) documentar diff, evidencia real, pendientes y rollback; (g) actualizar PLAN, ambos MEMORY_STATE y README y **entregar ZIP íntegro** para `sources`. UAT se reserva para el final.

### I01 — HC-CENIT-001: Incorporación de cola entre sesiones

**Prioridad:** P1 · **Dependencia:** I00 · **Estado inicial:** implementado en fuente, abierto; código condicionado a prueba fallida o incumplimiento estático comprobable; sin bloqueo por ambiente.  
**Referencia normativa a consultar:** DSP-152 Anexo 2, cap. 1 §3.3, colas y cinco sesiones.  
**Componentes de partida:** `CenitCycleQueueService.IncorporateForCycleAsync; exportación de archivos/cierre de sesión`.  
**Pruebas de referencia:** PT-01, PT-02, PT-20, PT-21.

**Objetivo.** Asegurar que los archivos recibidos fuera de la ventana elegible se incorporen una sola vez en la siguiente sesión autorizada y que el ciclo 5 no procese transacciones monetarias ordinarias nuevas.

**Secuencia de implementación y verificación:**
1. Reconstruir la ruta recepción → cola persistida → incorporación → armado del lote → exportación, distinguiendo sesión del calendario y fecha valor.
2. Preparar un fixture de archivo tardío C1→C2 y otro de C4→C5; comprobar que los archivos no elegibles para C5 no se compensan.
3. Ejecutar servicio y scheduler Quartz sobre una BD real con reloj controlado, fallo antes/después de commit y reintento del mismo archivo.
4. Modificar únicamente el guardado/orden de incorporación si el ensayo demuestra duplicación, pérdida de archivo o procesamiento en sesión equivocada.

**Caso positivo:** C1 tardío se procesa en C2 una sola vez; devolución elegible C4 se incorpora en C5 conservando fecha valor.

**Caso(s) negativo(s):** Reintento y dos workers no duplican; transacción monetaria ordinaria no ingresa en C5; caída DB no consume la cola sin persistir el lote.

**Criterio de aceptación:** Conteos de cola, lotes y transacciones coinciden y una única transición a Consumed queda auditada. No regresión ACH Colombia.

**Evidencias técnicas mínimas:** diff/commit y archivos modificados (si hay cambio), paquetes actualizados con versión y motivo, resultado REAL de restore/build/tests/auditoría de vulnerabilidades (`PASS`/`FAIL`/`NO_EJECUTADA`), DB/Quartz/fixture y hash cuando aplique, regresión ambas cámaras o su deuda explícita; ZIP íntegro, PLAN y dos MEMORY_STATE actualizados. **UAT: solo al final; sin firma UAT aquí.** **Rollback:** Revertir solo la transición/cambio defectuoso y restaurar snapshot de datos de prueba; no retroceder mensajes ya aceptados por operador.

**Ejecución real I01 (14/11/2026):** `IMPLEMENTADO_PENDIENTE_VERIFICACION_AMBIENTE`. Análisis de código identificó una brecha de consumo prematuro en C5: la incorporación confirmaba cola antes de validar tipos de sesión en NACHA. Corrección en `CenitCycleQueueService.cs` con resolución del número de sesión desde el nombre y prevalidación de clase `Return/ReturnOfReturn` y política antes de la mutación. Cinco pruebas C# agregadas en `CenitCycleQueueIncorporationF03Tests.cs` sin ejecución (SDK .NET ausente). Verificación real limitada a F01–F08 151/151 **estáticas/documentales** + nuevo I01 15/15 estáticas. F03 checker ajustado para distinguir lectura de causal vs asignación; regresión F03 17/17. `npm ci` offline ENOTCACHED, build Angular no ejecutable, vulnerabilidades actuales NuGet/npm NO VERIFICADAS, sin cambios de dependencias/migraciones. **PT-01, PT-02, PT-20, PT-21 PENDIENTES; 0/13 casos cerrados; UAT RESERVADA_FINAL; NO-GO.** Riesgo residual explícito: C5 con cola ordinaria no elegible se rechaza sin consumo, pero puede bloquear la incorporación de una devolución válida hasta tratamiento operativo; resolver/aceptar procedimiento antes del cierre normativo. Evidencia: `EVIDENCIAS/I01/INFORME_I01.md` y logs; siguiente entrega I02.

### I02 — HC-CENIT-002: Correlación de devolución de devolución (ROR) en C1

**Prioridad:** P1 · **Dependencia:** I01 · **Estado actual:** `IMPLEMENTADO_PENDIENTE_VERIFICACION_AMBIENTE`; **ABIERTO** hasta pruebas PT-03 y homologación.  
**Referencia normativa a consultar:** DSP-152 Anexo 2 §4.3/4.6, devolución de devolución.  
**Componentes de partida:** `CenitReturnOfReturnService; generador NACHA-M para ROR`.  
**Pruebas de referencia:** PT-03.

**Objetivo.** Correlacionar ROR del primer ciclo con la devolución y la transacción original exactas, sin crear un ROR huérfano o asociarlo al padre equivocado.

**Secuencia de implementación y verificación:**
1. Inspeccionar identificadores de transacción, devolución padre, secuencia NACHA y estado de origen al entrar por C1.
2. Construir una ROR positiva y dos padres similares con secuencias distintas; seguirla desde DB hasta el archivo físico.
3. Verificar un solo ROR autorizado para la devolución y la conservación de importe, fecha valor y trazas.
4. Si falla la relación, corregir la clave de búsqueda/guardado mínima, no reconstruir todo el servicio.

**Caso positivo:** La ROR apunta a la devolución original y transacción base correctas, aun en el primer ciclo.

**Caso(s) negativo(s):** Padre inexistente, distinto o repetido no produce ROR válida; reenvío repetido es idempotente.

**Criterio de aceptación:** Relaciones de tres niveles comprobadas en DB, archivo NACHA real y auditoría, con controles negativos.

**Evidencias técnicas mínimas:** diff/commit y archivos modificados (si hay cambio), paquetes actualizados con versión y motivo, resultado REAL de restore/build/tests/auditoría de vulnerabilidades (`PASS`/`FAIL`/`NO_EJECUTADA`), DB/Quartz/fixture y hash cuando aplique, regresión ambas cámaras o su deuda explícita; ZIP íntegro, PLAN y dos MEMORY_STATE actualizados. **UAT: solo al final; sin firma UAT aquí.** **Rollback:** Revertir solo ajuste de correlación y tratar ROR emitidas erróneamente por procedimiento operativo, no con borrado ciego.

### I03 — HC-CENIT-003: Separar neteo simulado de liquidación oficial

**Prioridad:** P1 · **Dependencia:** I01 · **Estado inicial:** implementado en fuente, abierto; código condicionado a prueba fallida o incumplimiento estático comprobable; sin bloqueo por ambiente.  
**Referencia normativa a consultar:** DSP-152 Reglamento/CENIT, compensación y liquidación contra CUD.  
**Componentes de partida:** `CenitNettingService; IncludedInSettlement; estados financieros`.  
**Pruebas de referencia:** PT-04, PT-05.

**Objetivo.** Impedir que una simulación de posición multilateral se interprete como compensación o liquidación oficialmente confirmadas por CENIT/CUD.

**Secuencia de implementación y verificación:**
1. Enumerar los estados afectados por el neteo local y las operaciones que publican/escriben IncludedInSettlement.
2. Ejecutar posición simulada con saldo suficiente e insuficiente y capturar DB antes/después.
3. Verificar que solo una respuesta autorizada del operador puede elevar un estado a oficial y que el log/UX no usan sinónimos ambiguos.
4. Corregir asignaciones de estado únicamente si una ruta de simulación atribuye liquidación oficial.

**Caso positivo:** El simulador calcula y muestra saldos sin mutar asientos oficiales.

**Caso(s) negativo(s):** Sin respuesta del operador, no se marca SettlementConfirmed ni equivalente, aun con simulación exitosa.

**Criterio de aceptación:** Evidencia de DB/ledger separa neteo local de constancia externa; Tesorería valida el límite de responsabilidades.

**Evidencias técnicas mínimas:** diff/commit y archivos modificados (si hay cambio), paquetes actualizados con versión y motivo, resultado REAL de restore/build/tests/auditoría de vulnerabilidades (`PASS`/`FAIL`/`NO_EJECUTADA`), DB/Quartz/fixture y hash cuando aplique, regresión ambas cámaras o su deuda explícita; ZIP íntegro, PLAN y dos MEMORY_STATE actualizados. **UAT: solo al final; sin firma UAT aquí.** **Rollback:** Revertir mutación local y reconciliar estados contra evidencia externa, nunca fabricar asiento CUD.

### I04 — HC-CENIT-004: Regla R23 para devoluciones del mismo día

**Prioridad:** P1 · **Dependencia:** I01 · **Estado inicial:** implementado en fuente, abierto; código condicionado a prueba fallida o incumplimiento estático comprobable; sin bloqueo por ambiente.  
**Referencia normativa a consultar:** DSP-152 Anexo 2 §3.2, §4.2, R23.  
**Componentes de partida:** `CenitIncomingReturnPolicy.EvaluateR23; regla por sesión/fecha valor`.  
**Pruebas de referencia:** PT-07, PT-17.

**Objetivo.** Respetar siguiente sesión y reglas específicas para R23 sin aplicar indiscriminadamente la excepción de reclamación de días anteriores.

**Secuencia de implementación y verificación:**
1. Separar escenarios R23 misma fecha de R23 posterior por rechazo del receptor.
2. Construir entradas C1/C2/C3/C4 con intentos R23 en sesión siguiente, sesión misma y sesión no habilitada.
3. Contrastar fecha valor, existencia de notificación y prohibiciones del primer/quinto ciclo.
4. Aplicar parche focal de política solo ante aceptación indebida o falso rechazo reproducible.

**Caso positivo:** Una R23 elegible se tramita en el ciclo autorizado; archivo conserva causal y trazabilidad.

**Caso(s) negativo(s):** R23 mismo ciclo o bajo excepción de días anteriores sin condición válida se rechaza.

**Criterio de aceptación:** Matriz de sesiones/fecha valor con resultado trazable y acuse del operador donde proceda.

**Evidencias técnicas mínimas:** diff/commit y archivos modificados (si hay cambio), paquetes actualizados con versión y motivo, resultado REAL de restore/build/tests/auditoría de vulnerabilidades (`PASS`/`FAIL`/`NO_EJECUTADA`), DB/Quartz/fixture y hash cuando aplique, regresión ambas cámaras o su deuda explícita; ZIP íntegro, PLAN y dos MEMORY_STATE actualizados. **UAT: solo al final; sin firma UAT aquí.** **Rollback:** Restablecer política previa y aislar archivos generados en entorno de prueba.

### I05 — HC-CENIT-005: Prioridades del simulador PPD/CCD/CTX

**Prioridad:** P2 · **Dependencia:** I03 · **Estado actual:** `IMPLEMENTADO_PENDIENTE_VERIFICACION_AMBIENTE`; **ABIERTO** hasta PT-06, .NET y homologación.  
**Referencia normativa a consultar:** DSP-152 Anexo 2 §5, algoritmo de optimización.  
**Componentes de partida:** `TransactionPriorityPolicy; simulador de optimización CENIT`.  
**Pruebas de referencia:** PT-06.

**Objetivo.** Respetar el orden de prioridades normativas de procesamiento en la simulación sin conferir carácter oficial a la prioridad local.

**Secuencia de implementación y verificación:**
1. Identificar categorías SEC PPD, CCD y CTX y las diez prioridades registradas.
2. Crear fixture parametrizada con todas las clases, devoluciones y empates, más permutaciones de orden de entrada.
3. Comparar orden calculado con tabla aplicable en normativa aportada y conservar la estabilidad entre ejecuciones.
4. Si falla, corregir exclusivamente tabla/regla de clasificación y volver a verificar neteo no oficial.

**Caso positivo:** Orden esperado reproducible para PPD/CCD/CTX y transacciones de devolución.

**Caso(s) negativo(s):** Clase desconocida o inconsistente no recibe prioridad arbitraria; simulador no liquida.

**Criterio de aceptación:** Diez niveles y empates documentados mediante test ejecutado sin efectos financieros productivos.

**Evidencias técnicas mínimas:** diff/commit y archivos modificados (si hay cambio), paquetes actualizados con versión y motivo, resultado REAL de restore/build/tests/auditoría de vulnerabilidades (`PASS`/`FAIL`/`NO_EJECUTADA`), DB/Quartz/fixture y hash cuando aplique, regresión ambas cámaras o su deuda explícita; ZIP íntegro, PLAN y dos MEMORY_STATE actualizados. **UAT: solo al final; sin firma UAT aquí.** **Rollback:** Restablecer regla de prioridad sin alterar transacciones liquidadas por terceros.

### I06 — HC-CENIT-006: Rechazo por liquidez: la simulación no decide

**Prioridad:** P1 · **Dependencia:** I03 · **Estado actual:** `IMPLEMENTADO_PENDIENTE_VERIFICACION_AMBIENTE`; **ABIERTO** hasta PT-04/PT-05, build, DB y homologación. I06 preserva estados finales o no reconocidos frente al reintento Quartz y no fabrica rechazo local.  
**Referencia normativa a consultar:** DSP-152 Anexo 2 §5, liquidación/optimización.  
**Componentes de partida:** `CenitCycleExecutionService; AwaitingOfficialResult; ReturnedByOperator`.  
**Pruebas de referencia:** PT-04, PT-05.

**Objetivo.** Evitar rechazos definitivos por fondos insuficientes basados solo en cálculo o evaluación local.

**Secuencia de implementación y verificación:**
1. Seguir la transición de un ciclo simulado con insuficiencia de saldo hasta respuesta de archivo del operador.
2. Provocar saldo insuficiente y correcto; registrar estados, eventos y cualquier publicación de rechazo.
3. Simular ausencia de ACK/NACK oficial y llegada posterior de respuesta, verificando idempotencia.
4. Corregir únicamente si ReturnedByOperator u otro estado definitivo se produce antes de mensaje oficial.

**Caso positivo:** Con simulación insuficiente, transacción espera resultado oficial y no pierde la posibilidad de conciliación.

**Caso(s) negativo(s):** No se fabrica rechazo ni se altera ledger sin NACK/aviso autorizado.

**Criterio de aceptación:** Estado AwaitingOfficialResult o equivalente persiste hasta evidencia externa, con prueba DB/Quartz.

**Evidencias técnicas mínimas:** diff/commit y archivos modificados (si hay cambio), paquetes actualizados con versión y motivo, resultado REAL de restore/build/tests/auditoría de vulnerabilidades (`PASS`/`FAIL`/`NO_EJECUTADA`), DB/Quartz/fixture y hash cuando aplique, regresión ambas cámaras o su deuda explícita; ZIP íntegro, PLAN y dos MEMORY_STATE actualizados. **UAT: solo al final; sin firma UAT aquí.** **Rollback:** Revertir transición y recomponer desde bitácora oficial, nunca a partir de saldos simulados.

### I07 — HC-CENIT-007: R06 en quinta sesión CENIT

**Prioridad:** P1 · **Dependencia:** I04 · **Estado inicial:** implementado en fuente, abierto; código condicionado a prueba fallida o incumplimiento estático comprobable; sin bloqueo por ambiente.  
**Referencia normativa a consultar:** DSP-152 Anexo 2 §3.2, excepciones en C5.  
**Componentes de partida:** `CenitIncomingReturnPolicy; validación por sesión de origen`.  
**Pruebas de referencia:** PT-09.

**Objetivo.** Permitir R06 en C5 solo cuando corresponda a origen C4 del mismo día y se cumpla el resto de requisitos.

**Secuencia de implementación y verificación:**
1. Crear una devolución R06 destinada a C5 para orígenes C1, C2, C3, C4 y día anterior.
2. Ejercitar construcción, validación y salida física del mensaje en cada escenario.
3. Verificar que la decisión identifica sesión y día operativo verdaderos, no solo hora del sistema.
4. Modificar predicado de elegibilidad solo si la prueba demuestra bypass o rechazo inválido.

**Caso positivo:** R06 de C4 del mismo día es elegible en C5 cuando demás condiciones son correctas.

**Caso(s) negativo(s):** R06 con C1/C2/C3 u otro día de origen no elude la regla de C5.

**Criterio de aceptación:** PT-09 aprobado, causal preservada, evidencia de archivo y acuses donde aplique.

**Evidencias técnicas mínimas:** diff/commit y archivos modificados (si hay cambio), paquetes actualizados con versión y motivo, resultado REAL de restore/build/tests/auditoría de vulnerabilidades (`PASS`/`FAIL`/`NO_EJECUTADA`), DB/Quartz/fixture y hash cuando aplique, regresión ambas cámaras o su deuda explícita; ZIP íntegro, PLAN y dos MEMORY_STATE actualizados. **UAT: solo al final; sin firma UAT aquí.** **Rollback:** Reversión limitada de política sin reemitir devoluciones reales.

### I08 — HC-CENIT-008: ROR causal R74 y adenda requerida

**Prioridad:** P1 · **Dependencia:** I02 · **Estado actual:** `IMPLEMENTADO_PENDIENTE_VERIFICACION_AMBIENTE`; **ABIERTO** hasta PT-10, .NET, DB, NACHA físico y homologación. I08 unificó validación en un gate de evidencia original, rechazando adendas de devolución como sustituto.  
**Referencia normativa a consultar:** Anexo A CENIT R74/R32; Manual NACHA-M §7 y Anexo 1.5.  
**Componentes de partida:** `CenitReturnOfReturnService; lectura adenda NACHA`.  
**Pruebas de referencia:** PT-10.

**Objetivo.** Aceptar R74 únicamente ante devolución padre R32 de crédito monetario, con campo 3 de adenda requerido.

**Secuencia de implementación y verificación:**
1. Montar transacciones padre R32 crédito, R32 débito, R02 y R32 crédito sin campo 3.
2. Verificar tipo monetario, origen/padre, contenido de adenda y trazabilidad física 106 caracteres.
3. Probar doble ROR y secuencia incorrecta para garantizar control de unicidad.
4. Corregir solo validación selectiva o contenido de adenda que falle las pruebas.

**Caso positivo:** R74 con padre R32 crédito monetario y campo 3 completo es procesable.

**Caso(s) negativo(s):** R02, R32 débito, adenda incompleta o padre inexistente se rechazan con causa válida.

**Criterio de aceptación:** Relación padre/archivo y contenido de campo 3 comprobados; no duplicado de ROR.

**Evidencias técnicas mínimas:** diff/commit y archivos modificados (si hay cambio), paquetes actualizados con versión y motivo, resultado REAL de restore/build/tests/auditoría de vulnerabilidades (`PASS`/`FAIL`/`NO_EJECUTADA`), DB/Quartz/fixture y hash cuando aplique, regresión ambas cámaras o su deuda explícita; ZIP íntegro, PLAN y dos MEMORY_STATE actualizados. **UAT: solo al final; sin firma UAT aquí.** **Rollback:** Restituir validación anterior y tratar entradas ya emitidas con flujo formal de devolución.

### I09 — HC-CENIT-009: Plazos R23: 15 días calendario y siguiente hábil

**Prioridad:** P1 · **Dependencia:** I04 · **Estado inicial:** implementado en fuente, abierto; código condicionado a prueba fallida o incumplimiento estático comprobable; sin bloqueo por ambiente.  
**Referencia normativa a consultar:** DSP-152 Anexo 2 §4.2; Anexo A R23.  
**Componentes de partida:** `Política R23; calendario bancario de Bogotá; servicio de devoluciones`.  
**Pruebas de referencia:** PT-08.

**Objetivo.** Computar los 15 días calendario máximos y el plazo de tramitación al día hábil bancario siguiente a la notificación verificable.

**Secuencia de implementación y verificación:**
1. Separar fecha de entrada original, notificación de rechazo y fecha de envío de devolución.
2. Probar día 0, día 15 y día 16, además de notificación viernes, festivos en Bogotá, cambio de mes/año y cambios de horario.
3. Confirmar la fuente calendario bancario y el origen de la fecha de notificación; fallar cerrado ante falta de evidencia.
4. Modificar solamente cómputo de plazos/calendario cuando exista desajuste reproducible.

**Caso positivo:** R23 dentro del plazo y notificada correctamente se tramita a más tardar el siguiente hábil bancario aplicable.

**Caso(s) negativo(s):** Fuera de 15 días, notificación no demostrada o plazo vencido no se presenta como devolución oportuna.

**Criterio de aceptación:** PT-08 aprobado con calendario y fecha de notificación auditables, más evidencia de operador cuando aplique.

**Evidencias técnicas mínimas:** diff/commit y archivos modificados (si hay cambio), paquetes actualizados con versión y motivo, resultado REAL de restore/build/tests/auditoría de vulnerabilidades (`PASS`/`FAIL`/`NO_EJECUTADA`), DB/Quartz/fixture y hash cuando aplique, regresión ambas cámaras o su deuda explícita; ZIP íntegro, PLAN y dos MEMORY_STATE actualizados. **UAT: solo al final; sin firma UAT aquí.** **Rollback:** Restablecer cálculo y retener expedientes fuera de ventana para tratamiento entre participantes fuera del sistema.

**Resultado I09 (15/11/2026):** Implementada protección contra fecha valor de devolución alterada y contra la selección intradía por coincidencia accidental de fechas valor. El plazo máximo se evalúa por `ReturnOperationalDate` y la fecha original recibida por `AchCycle.ProcessingDate`. La notificación UTC/Local se convierte a día bancario de Bogotá antes de consultar `IOperationalCalendarService`; los valores `Unspecified` se interpretan como fecha local bancaria. Las rutas CENIT entrante y saliente utilizan el mismo reloj. El validador genérico incorpora únicamente la excepción de crédito R23 CENIT por 15 días, sin alterar ACH Colombia u otras causales. PT-08 sigue PENDIENTE de compilación, pruebas SQL/Quartz/NACHA-M, calendario operativo de Bogotá y trazabilidad de recepción de notificación. **I09 implementado pero no certificado; HC-CENIT-009 ABIERTO.** Ver `EVIDENCIAS/I09/INFORME_I09.md`.

### I10 — HC-CENIT-010: Alfabeto NACHA-M específico de CENIT

**Prioridad:** P2 · **Dependencia:** I00 · **Estado inicial:** implementado en fuente, abierto; código condicionado a prueba fallida o incumplimiento estático comprobable; sin bloqueo por ambiente.  
**Referencia normativa a consultar:** Manual NACHA-M CENIT 07-may-2026, §4.1.  
**Componentes de partida:** `NachaPhysicalParser.IsAcceptedByte; perfil físico por cámara`.  
**Pruebas de referencia:** PT-11, PT-20.

**Objetivo.** Validar caracteres permitidos por el perfil CENIT sin introducir rechazos indebidos de archivos de ACH Colombia.

**Secuencia de implementación y verificación:**
1. Confirmar lector de bytes, codificación y lista de caracteres NACHA-M CENIT de 2026.
2. Crear fixture con Ñ/ñ, acentos, caracteres de control, separadores y caracteres permitidos en ambos perfiles.
3. Probar parser de CENIT y regresión ACH Colombia con los mismos casos y una muestra golden.
4. Cambiar solo selector/perfil de caracteres si la política se mezcla o acepta bytes indebidos.

**Caso positivo:** Archivo CENIT válido respeta exactamente layout y alfabeto; ACH Colombia mantiene su perfil.

**Caso(s) negativo(s):** CENIT rechaza caracteres ajenos al manual y controles ASCII inválidos.

**Criterio de aceptación:** PT-11 y PT-20 ejecutadas, diferencias explicadas por perfil sin falso positivo cruzado.

**Evidencias técnicas mínimas:** diff/commit y archivos modificados (si hay cambio), paquetes actualizados con versión y motivo, resultado REAL de restore/build/tests/auditoría de vulnerabilidades (`PASS`/`FAIL`/`NO_EJECUTADA`), DB/Quartz/fixture y hash cuando aplique, regresión ambas cámaras o su deuda explícita; ZIP íntegro, PLAN y dos MEMORY_STATE actualizados. **UAT: solo al final; sin firma UAT aquí.** **Rollback:** Revertir ajuste de perfil y no reescribir bytes de archivos originales.

**Resultado I10 (15/11/2026):** el parser entrante de CENIT ya separaba el repertorio ASCII permitido del Ñ/ñ Latin-1 legado de ACH Colombia. Se corrigió la salida CENIT con un control final antes de `Encoding.ASCII.GetBytes`/hash/auditoría, en ReturnOut, legacy y tabla oficial, compartiendo el predicado del parser. ACH Colombia no cambia su política. Se escribieron 6 pruebas xUnit (una matriz exhaustiva de 256 bytes y 8 datos negativos en teoría), cero xUnit ejecutadas por SDK ausente; 470/470 comprobaciones de fuente/fixture PASS. PT-11/PT-20 siguen pendientes de ejecución y homologación; **HC-CENIT-010 ABIERTO**. Ver `EVIDENCIAS/I10/INFORME_I10.md`.

### I11 — HC-CENIT-011: Separar errores de STA y NACHA-M

**Prioridad:** P1 · **Dependencia:** I10 · **Estado actual:** `IMPLEMENTADO_PENDIENTE_VERIFICACION_AMBIENTE`, **ABIERTO** hasta PT-12, ejecución .NET/DB, rutas de consumo y homologación. I11 aisló el catálogo de rechazos por cámara/servicio y agregó guardas de identidad.  
**Referencia normativa a consultar:** Anexo B CENIT D01–D06 (STA); Manual NACHA-M §4.3.  
**Componentes de partida:** `NachaInboundValidation / selector de dominio; catálogo STA D01–D06`.  
**Pruebas de referencia:** PT-12.

**Objetivo.** Evitar que rechazos de archivos del servicio STA se interpreten como causales NACHA-M de compensación y viceversa.

**Secuencia de implementación y verificación:**
1. Catalogar entrada por servicio, canal, tipo de archivo, gravedad y causa.
2. Construir un STA con D02 y un NACHA-M con error físico/estructura parecido, más archivo válido de cada servicio.
3. Seguir respuesta del validador, código, descripción e interfaz/persistencia de rechazo hasta el consumidor.
4. Corregir únicamente el selector de dominio o la traducción de código que no respete la cámara/servicio.

**Caso positivo:** STA D01–D06 quedan dentro del dominio STA y los fallos NACHA conservan su catálogo.

**Caso(s) negativo(s):** El mismo D02 textual en servicios distintos jamás se asigna sin revisar el dominio y fase.

**Criterio de aceptación:** PT-12 aprobado, pruebas golden de ambos servicios y sin regresión ACH Colombia.

**Evidencias técnicas mínimas:** diff/commit y archivos modificados (si hay cambio), paquetes actualizados con versión y motivo, resultado REAL de restore/build/tests/auditoría de vulnerabilidades (`PASS`/`FAIL`/`NO_EJECUTADA`), DB/Quartz/fixture y hash cuando aplique, regresión ambas cámaras o su deuda explícita; ZIP íntegro, PLAN y dos MEMORY_STATE actualizados. **UAT: solo al final; sin firma UAT aquí.** **Rollback:** Revertir mapeo por servicio; no alterar códigos históricos persistidos sin migración aprobada.

**Ejecución I11 (2026-11-15):** se conservó la clasificación local `CENIT-NACHA-FATAL` existente. Se corrigió carga no filtrada de `AchFileRejectionCodes` por `Code`, que mezclaba Dxx de STA y potenciales causas ACH. Ahora CENIT-NACHA no consulta STA; ACH consulta solo catálogo propio/global no-STA; las colisiones en un ámbito fallan cerradas, y el perfil/cámara deben ser consistentes. Selector STA puro D01–D06; **sin adaptador STA productivo**. 468/468 controles de regresión estáticos/documentales y 36/36 focales PASS; xUnit escrito no ejecutado (SDK ausente), PT-12 PENDIENTE; producción NO-GO. Informe `EVIDENCIAS/I11/INFORME_I11.md`.

### I12 — HC-CENIT-012: ADV 280, códigos 81–88 y compatibilidad física CE-01

**Prioridad:** P1 · **Dependencia:** I03 e I10 · **Estado inicial:** implementado en fuente, abierto; requiere código CE-01 + pruebas.  
**Referencia normativa a consultar:** Manual NACHA-M CENIT 07-may-2026, §3 y §7.3.3; ADV 280.  
**Componentes de partida:** `CenitChamberResponseService.ParseAdv; CenitChamberResponseLifecycleTests`.  
**Pruebas de referencia:** PT-13, PT-19, PT-23.

**Objetivo.** Conciliar avisos ADV 280 con códigos 81–88 y corregir CE-01: lector que actualmente depende de saltos de línea pese a registros físicos contiguos de 106 caracteres.

**Secuencia de implementación y verificación:**
1. Crear PT-23 con archivo físico de registros 1/5/6/8/9 de 106 posiciones concatenados, sin CR/LF, con códigos 81–88 y totales verificados.
2. Reproducir fallo del parser existente, separando framing físico de clasificación del tipo de mensaje ADV/RECONCILIATION.
3. Implementar lectura por tramos de 106 caracteres, controles de longitud, estructura, adenda/relleno y truncamiento. Adaptar CR/LF solo si un contrato oficial de transporte documenta dicha transformación.
4. Validar ACK/ADV/avisos y conciliación sin marcar pago ni generar asiento oficial CUD.
5. Reejecutar PT-13/PT-19/PT-23, regresión de parsers NACHA y replay con acuses reales o fixtures oficiales.

**Caso positivo:** ADV contiguo válido se interpreta, totales/códigos 81–88 quedan conciliables con fecha/sesión e identificación.

**Caso(s) negativo(s):** Truncado, orden erróneo, carácter inválido, importe/control alterado o duplicado no se convierte en asiento.

**Criterio de aceptación normativo final:** PT-23 ejecutada y roja→verde, PT-13/19 aprobadas y evidencia Tesorería de conciliación manual; CUD sigue fuera de aplicación. **Entrega I12 intermedia permitida:** código y tests escritos, evidencia estática, pruebas no ejecutadas consignadas si falta entorno.

**Evidencias técnicas mínimas:** diff/commit y archivos modificados (si hay cambio), paquetes actualizados con versión y motivo, resultado REAL de restore/build/tests/auditoría de vulnerabilidades (`PASS`/`FAIL`/`NO_EJECUTADA`), DB/Quartz/fixture y hash cuando aplique, regresión ambas cámaras o su deuda explícita; ZIP íntegro, PLAN y dos MEMORY_STATE actualizados. **UAT: solo al final; sin firma UAT aquí.** **Rollback:** Revertir solo el parser físico con restauración de snapshot de DB; conservar raw y hash para reproceso autorizado, sin reposteo financiero.

### I13 — HC-CENIT-013: NACK por transacción, no rechazo global del archivo

**Prioridad:** P1 · **Dependencia:** I01 e I12 · **Estado inicial:** implementado en fuente, abierto; código condicionado a prueba fallida o incumplimiento estático comprobable; sin bloqueo por ambiente.  
**Referencia normativa a consultar:** Manual CENIT WEB y NACHA-M, respuestas individuales ACK/NACK.  
**Componentes de partida:** `CenitChamberResponseService.ImportItemAsync; estado de archivo/ítem`.  
**Pruebas de referencia:** PT-14, PT-15.

**Objetivo.** Aplicar el NACK a la transacción individual afectada, preservando las aceptadas y evitando dobles efectos al recibir respuestas duplicadas o concurrentes.

**Secuencia de implementación y verificación:**
1. Preparar archivo mixto con tres transacciones, dos ACK y una NACK referida por secuencia específica.
2. Importar respuesta por primera vez, reimportarla y procesarla simultáneamente desde dos workers con DB real.
3. Comprobar que agregación de archivo refleja estado parcial sin rechazar ítems no afectados; verificar eventos y ledger.
4. Corregir solo emparejamiento por ítem, locking o idempotencia si hay cruce/doble evento.

**Caso positivo:** Solo la transacción NACK modifica su estado y conserva motivo; las ACK siguen aceptadas.

**Caso(s) negativo(s):** Respuesta repetida, simultánea, desconocida o mal correlacionada no modifica ítems ajenos ni duplica eventos.

**Criterio de aceptación:** PT-14/15 aprobadas con transacciones y eventos en BD antes/después, concurrencia y no regresión ACH.

**Evidencias técnicas mínimas:** diff/commit y archivos modificados (si hay cambio), paquetes actualizados con versión y motivo, resultado REAL de restore/build/tests/auditoría de vulnerabilidades (`PASS`/`FAIL`/`NO_EJECUTADA`), DB/Quartz/fixture y hash cuando aplique, regresión ambas cámaras o su deuda explícita; ZIP íntegro, PLAN y dos MEMORY_STATE actualizados. **UAT: solo al final; sin firma UAT aquí.** **Rollback:** Retraer cambio de importación y recuperar desde acuses inmutables/backup bajo autorización operativa.

## 5. Puertas finales después de I13 — pruebas técnicas acumuladas, homologación y UAT ÚNICA (NO son casos adicionales)

- **G1 / software:** todos los incrementos terminan con pruebas positivas y negativas ejecutadas, registro de fallas corregidas, suite de regresión ACH/CENIT, análisis de concurrencia/idempotencia, build Release real y migraciones verificadas. PASS de 137 controles estáticos históricos por sí solo **no** cierra un caso.
- **G2 / integración:** archivos PPD/CCD/CTX, ROR, ACK/NACK, ADV y STA conformes a esquema y contratos vigentes; sesiones CENIT 1–5 con prueba en ambiente de homologación autorizado. Ejecutar también PT-16, PT-17, PT-18, PT-20, PT-21 y PT-22 si no quedaron cubiertas por los incrementos.
- **G3 / Tesorería y seguridad:** PT-19 con conciliación CUD **manual** (no inferir CUD de ACK/ADV); aprobación explícita para logging SOAP crudo o endurecimiento/deshabilitación por defecto con cifrado, ACL, retención y auditoría. Ninguna evidencia sensible en ZIP público.
- **G4 / UAT consolidada (solo aquí):** una vez terminados I00–I13 y corregidos AC-01..04/SEC-01 exigibles para el proyecto integral, ejecutar UAT de negocio y usuarios autorizados sobre el sistema completo; registrar casos, evidencias, defectos, correcciones y repetición de pruebas. No exigir UAT en incrementos anteriores.
- **G5 / cierre y liberación:** cada HC-CENIT-001..013 pasa por `IMPLEMENTADO → PROBADO_TECNICAMENTE → EVIDENCIADO → UAT_FINAL_APROBADA → APROBADO → CERRADO`. Si falta PT aplicable, seguridad, firma UAT, integración u homologación exigida, dictamen **NO-GO**. Entregar acta de cierre, versión, responsables, matriz de riesgo y rollback.

## 6. Pendientes de ACH Colombia fuera de los 13 casos CENIT

Para evitar presentar el proyecto integral como «solo pruebas», registrar **fuera** de I01–I13 las diferencias encontradas el 14/11/2026:

| ID | Situación | Tratamiento antes de producción |
|---|---|---|
| AC-01 | R10 ordena ciclos por `StartTime` (C1 nocturno) | Corregir secuencia operativa C1–C7 y ejecutar regresión |
| AC-02 | D01/D02/D04 asociados a errores NACHA con otra semántica | Corregir matriz de causales por cámara y tipo de error |
| AC-03 | D33 ACH Colombia ausente en semilla | Incorporar y probar seed/migración idempotente |
| AC-04 | `ReturnWindows` no trazado uniformemente a todas las rutas de ReturnOut | Trazar ruta por ruta; corregir solo bypass demostrado |
| SEC-01 | SOAP sin enmascarar por defecto | Evaluar y asegurar o deshabilitar con autorización de Seguridad |

**Estos cinco elementos no se cuentan como CENIT I01–I13.** Tampoco se consideran cerrados por el éxito de las pruebas CENIT. Deben constar en un backlog separado para la decisión de GO del **proyecto integral**. Si el objetivo fuese solamente certificar CENIT, el acta debe limitar expresamente el alcance.

## 7. Evidencia y definición de terminado por caso

Registrar `EVIDENCIAS/Ixx/` (preferentemente en repositorio seguro externo) con: `CASO.md` (regla, alcance, archivo/método, cambio, revisor), `baseline.log`, `test.trx`, fixture sin datos sensibles y hash SHA-256, consulta DB anonimizada, resultado de dos cámaras, estado de homologación técnica (pendiente/realizada), decisión de revisión técnica y mecanismo de rollback. Las respuestas reales de operador deben provenir del entorno autorizado: una fixture local **no** demuestra aceptación bancaria. No almacenar credenciales ni XML SOAP sensible en el ZIP.

Después de cada incremento actualizar `PLAN_IMPLEMENTACION_13_CASOS/PLAN_IMPLEMENTACION_13_CASOS.md`, `MEMORY_STATE.md`, `MEMORY_STATE.json` y `LEER_PRIMERO.md` con estado exacto, código tocado, versiones y riesgos de paquetes, SHA/commit, pruebas PASS/FAIL/NO_EJECUTADA, pendientes ambientales, próximo incremento y SHA del ZIP emitido en el registro externo de entrega (el ZIP no puede contener su propio hash). Entregar siempre el **proyecto completo** y preservarlo como base siguiente. **No sobreescribir** `ACHInterbank_ajustado/MEMORY_STATE.md`, auditorías I01–I10 ni registros F01–F08 históricos; el memory state canónico actual es el que vive en `PLAN_IMPLEMENTACION_13_CASOS/`.

## 8. Orden para ejecutar sin sobreingeniería

**Secuencia principal (sin saltos):** I00 → I01 → I02 → I03 → I04 → I05 → I06 → I07 → I08 → I09 → I10 → I11 → I12 → I13 → subsanación transversal AC-01..04/SEC-01 (si aplica al GO integral) → G1–G3 técnicas → G4 UAT única → G5 cierre. Dependencias ambientales indisponibles **no frenan la codificación del siguiente Ixx**; sí se anotan para G1–G5. Se pueden paralelizar solo preparaciones técnicas puntuales, respetando el orden de entregas y sin mezclar ramas. **Secuencia I01–I13 implementada:** siguientes puertas G1–G3 técnicas, G4 UAT consolidada, G5 cierre; no crear I14. I00/I01–I10 no cierran ningún caso CENIT.

**Fuentes de trazabilidad incluidas en el proyecto original:** `docs/auditoria-estatica-cenit/MATRIZ_PRUEBAS_POSTERIORES_CENIT.md`, `docs/correcciones-cenit/F08_MATRIZ_CIERRE_HALLAZGOS.md`, `docs/correcciones-cenit/F08_PLAN_PRUEBAS_Y_APROBACIONES.md`. Los manuales normativos externos **no se incluyen como carpeta adicional** en esta entrega.


## 6. Ejecución real de I00 — línea base (2026-11-14)

**Estado:** `DIAGNOSTICADO_CON_VERIFICACIONES_PENDIENTES`; próximo incremento **I01 / HC-CENIT-001**; `NO-GO` producción; UAT `RESERVADA_FASE_FINAL`. El estado es de preparación técnica, NO cierre normativo. No se modificó código de producción ni pruebas.

- ZIP de entrada V3 SHA-256 `99753a4cca023d73670c9c2bcaec5d0428a4349973c87c08c015a98a34f78b5e`, 5.760 entradas y CRC válido. SHA del archivo histórico de fuente solo consta como dato heredado, no verificable aquí. Git: sin `.git` en paquete, no hay rama/commit verificables.
- F01–F08: 154/154 controles **estáticos/documentales** PASS efectivamente ejecutados; F08 tres comprobaciones condicionadas al ZIP F07 histórico **no ejecutadas**. El histórico 137/137 y las ejecuciones actuales usan configuraciones y recuentos diferentes.
- SDK .NET 10.0.403 no instalado: `dotnet restore/build/test` **NO EJECUTADOS**; 0 tests xUnit ejecutados, no se fabrica TRX. SQL Server/PostgreSQL/Quartz y homologación no ejecutados.
- npm: verificador de lock PASS_STATIC (1.140 paquetes, 28 `integrity` faltantes en entradas registry); `npm ci --offline` falló por caché ENOTCACHED; `npm audit` y `npm outdated` sin verificación por error DNS EAI_AGAIN; `npm run build` no ejecutable por `ng` ausente. Nuevas CVE/versiones compatibles **no verificadas**.
- Inventario fuente: ocho `.csproj` en net10.0, 53 referencias PackageReference directas (48 pares nombre+versión). Entradas y rutas de ciclo, exportación, recepción, respuesta, conciliación y reintento localizadas en `EVIDENCIAS/I00/INVENTARIO_RUTAS.md`.
- La referencia en §3 a “dos guardas históricas fallidas” carece de IDs/logs específicos en el plan V3. No falló ninguna guarda F01–F08 en esta corrida; su identificación/reproducción histórica **sigue pendiente** y no se declara reparada.
- **Evidencia trazable:** `EVIDENCIAS/I00/INFORME_I00.md`, `ENTORNO.json`, `static_results.json`, `execution_results.json`, `DEPENDENCIAS_INVENTARIO.json`, `BASELINE_SHA256_ORIGEN.tsv`, logs individuales. Todos los documentos originales de `ACHInterbank_ajustado/` preservados.
- **Salida autorizada:** continuar I01 sin bloqueo ambiental; resolver deuda de verificaciones en G1–G5. CE-01 sigue en I12, AC-01..04 y SEC-01 fuera de los 13 casos antes de UAT integral.
- **Rollback:** restaurar ZIP V3 de entrada con SHA anterior; al ser I00 documental, no hay rollback de BD ni de código productivo. El ZIP I00 íntegro emitido es la única base autorizada para I01.

## 9. Registro de ejecución I01 (2026-11-14)

- Estado: `IMPLEMENTADO_PENDIENTE_VERIFICACION_AMBIENTE`, caso `HC-CENIT-001` **ABIERTO**, sigue I02. El código C5 pre-commit se corrigió en `CenitCycleQueueService.cs`, con cinco pruebas C# nuevas y guarda I01. No cambiar I02–I13 por anticipado.
- Fuente única: I00 SHA-256 `e29571f44ea0f30bf25212357a74e626d7d1311e56adef18fb73b2a480f90a19`. Códigos, checksums y diff en `EVIDENCIAS/I01/CAMBIOS_I01.json` y `CODIGO_CAMBIOS.patch`.
- Pruebas realmente ejecutadas: 151/151 F01–F08 estáticas/documentales y 15/15 I01 estáticas. C# 5 tests agregados, **cero xUnit ejecutados**; SDK ausente, ninguna evidencia SQL/Quartz/2 workers/cámara, ninguna PT formal cerrada. No hay build ni TRX.
- Npm lockfiles intactos: 1.140 entradas, dependencias directas sincronizadas; npm ci offline falla ENOTCACHED; auditoría offline reporta 0 de caché, CVE actuales no verificados; NuGet también NO VERIFICADO.
- PT-02 riesgo residual: cola C5 no elegible queda `Queued` y el fallo cerrado puede bloquear una devolución válida en el mismo export hasta gestión operativa. Debe comprobarse/mitigarse antes del cierre I01 normativo y GO. No mutar fecha valor para resolverlo por suposición.
- `NO-GO` producción; UAT final única y posterior a I13 + G1–G3. ZIP íntegro I01 es la única base para I02; SHA del propio ZIP se consigna fuera del contenido.


## 10. Registro de ejecución I02 (2026-11-14)

- **Fuente única I01:** ZIP `ACHInterbank_PROYECTO_COMPLETO_I01_HC-CENIT-001_2026-11-14.zip`, SHA-256 `ba4e5adddf6747d86af983b64e251e992937ae1e87e1d693609a8e4839e5e0d9`; CRC/lectura PASS. Se preservaron todos los archivos preexistentes no modificados byte a byte.
- **Incompatibilidad estática corregida:** `CreateOutgoingAsync` y el generador ROR CENIT elegían `.OrderByDescending(LinkedAtUtc).FirstOrDefaultAsync()` de todos los raw con mismo original y causal. Dos retornos de distinta secuencia podían seleccionar el padre equivocado. Ahora `IncomingNachaPostParseProcessor` adjunta `incomingReturnLinkId` solo a la evidencia del evento CENIT; `CenitParentReturnEvidenceResolver` verifica GUID, enlace final, adenda 99, trace original, amount, claves de entry/addenda y motivo; ambos servicios reutilizan esa resolución en registro y en prevalidación/exportación. Evento histórico sin pin solo avanza con **único** candidato; varios candidatos dan `CENIT_ROR_SOURCE_RETURN_AMBIGUOUS`. No se crea una Return financiera sintética.
- **Archivos:** nuevos `CenitParentReturnEvidenceResolver.cs` y `scripts/test/cenit_i02_ror_static_check.py`; existentes `IncomingNachaPostParseProcessor.cs`, `CenitReturnOfReturnService.cs`, `AchReturnOfReturnFileGenerationService.cs`; se ampliaron dos clases xUnit. Sin migraciones ni cambios a catálogos, WSDL, `.csproj`, npm o código de ACH Colombia. Los cambios de recepción se condicionan a CENIT (`cenitPolicy is not null`); la evidencia de ACH Colombia no se reescribe.
- **Pruebas realmente ejecutadas:** 151/151 F01–F08 checks estáticos/documentales; 15/15 I01 estáticos; 26/26 I02 estáticos = **192/192 comprobaciones de texto/estructura**, no pruebas C#. Se añadieron **7 escenarios xUnit** (3 creación, 4 generación con teoría de dos filas) y se mantienen los anteriores; **0 xUnit ejecutados**. .NET 10 SDK no instalado, `restore`, `build`, `dotnet test`, SQL Server/PostgreSQL/Quartz, concurrencia y operador no ejecutados. `npm ci --offline` ENOTCACHED, `npm run build` sin Angular CLI; `npm audit --offline` reporta 0 solo en caché, online `EAI_AGAIN`: advisories npm actuales **NO_VERIFICADOS**, NuGet **NO_VERIFICADO**. No actualizar versiones por conjetura.
- **Riesgo conocido:** eventos históricos de devolución sin pin y múltiples links raw compatibles quedan bloqueados para revisión documental/manual. La unicidad DB del `ParentIncomingReturnStateEventId` sigue disponible, pero reintentos de varios workers y DB reales no están certificados. PT-03 requiere prueba DB→NACHA 106×n→auditoría, viernes/lunes/feriados, importes, duplicados y operador. **I01 conserva riesgo C5 pendiente**.
- **Decisión:** `HC-CENIT-002` **ABIERTO**; I02 `IMPLEMENTADO_PENDIENTE_VERIFICACION_AMBIENTE`; casos CENIT cerrados **0/13**, PT-03 **PENDIENTE**, UAT `RESERVADA_FASE_FINAL`, producción **NO-GO**. Siguiente: **I03 / HC-CENIT-003**; su base única es el ZIP integral I02. Evidencia `EVIDENCIAS/I02/INFORME_I02.md`, `RESULTADOS_I02.json`, logs y `CODIGO_CAMBIOS.patch`.


### Registro de implementación I03 — 2026-11-14

**Fuente única:** I02 ZIP SHA-256 `a46913998e9667425e47df8e881c7a42e2fa8c6548a0c8c85781737454b60152`. **HC-CENIT-003 ABIERTO**, `IMPLEMENTADO_PENDIENTE_VERIFICACION_AMBIENTE`, **NO-GO**. Se corrigió la guardia de cámara en `CenitNettingService.CalculateAsync` incluso para snapshots idempotentes y se añadió delimitación explícita de simulación y filtrado CENIT al endpoint `net-positions`; se aclaró el texto de la pantalla de neteo. La segregación estructural Quartz/financiera de F01 se conservó, sin cambios a CUD ni a ACH Colombia.

**Pruebas reales:** F01–F08 **151/151**; I01 **15/15**; I02 **26/26**; I03 **22/22**, total **214/214** solo estáticas/documentales/fixtures; TypeScript parseo **2/2 PASS**. xUnit **4 nuevos/0 ejecutados**, Jasmine **1 nuevo/0 ejecutados**; .NET SDK ausente, DB/Quartz/CUD sin ejecutar, npm ci offline ENOTCACHED, advisories actuales no verificados. **PT-04/PT-05 pendientes** con snapshots DB/ledger y revisión Tesorería. Riesgos I01/I02 intactos; UAT al final. Evidencia: `EVIDENCIAS/I03/INFORME_I03.md`, `RESULTADOS_I03.json`, logs, patch y SHA. **Siguiente I04/HC-CENIT-004** desde ZIP integral I03, sin saltar orden.


## 12. Registro de ejecución I04 (2026-11-14)

- **Única base de código:** ZIP integral I03 SHA-256 `70ba7ce57fe17aa95fbf874b1f8d7980ee118f3f7c5c89e3fd62da0a5f2874de`, CRC íntegro. I04 no importa código de I00/I01/I02 ni modifica fuentes normativas externas.
- **HC-CENIT-004 / defecto corregido:** la excepción de R23 de días anteriores se decidía por diferencia de Fecha Valor, sin impedir que una entrada con Fecha Valor anterior pero **recibida realmente en el día de la devolución** obtuviera el permiso de reclamo posterior. Se endurece C1 y se clasifica la recepción del mismo día en `CenitIncomingReturnPolicy`: la Fecha Valor anterior no autoriza saltear el ciclo inmediato ni la regla de igual Fecha Valor. Se valida que `OriginalCycleNumber` no exceda la última sesión. Las rutas entrada/salida ya transportan `AchCycle.ProcessingDate` como `OriginalReceivedDate`; no se modifica el contrato.
- **Criterio conservado del plan:** R23 mismo día con Fecha Valor coincidente y evidencia de solicitud se tramita solo en la siguiente sesión; C1 del mismo día y saltos C1→C3 están prohibidos. R23 auténtica de un día anterior se mantiene bajo su rama separada. La interpretación normativa del R23 intradía requiere contrastar evidencia del operador. Los plazos/feriados y 15 días se mantienen sin cambios para I09; no se registra GO por inspección estática.
- **Cambios mínimos:** 1 archivo C# productivo modificado, 1 clase de pruebas nueva (10 Fact + 2 Theory con 9 InlineData = **19 casos xUnit previstos**) y 1 verificador estático nuevo. Ningún controlador, DB, catálogos, migración, paquete, WSDL, UI ni código de ACH Colombia fue cambiado.
- **Ejecución REAL:** F01–F08 151/151 + I01 15/15 + I02 26/26 + I03 22/22 + I04 24/24 = **238/238 verificaciones estáticas/documentales**. 0 xUnit ejecutados, SDK .NET ausente, sin compilación ni DB/Quartz ni operador CENIT, sin archivo NACHA-M físico/ACK de PT-07/PT-17. npm lock consistente; `npm ci --offline` ENOTCACHED, `npm audit --offline` informó 0 según caché no actual, `npm outdated --offline` falló; NuGet y advisories npm **NO VERIFICADOS**. Sin paquetes cambiados.
- **Riesgos y rollback:** si la semántica oficial de R23 intradía difiere de la interpretación del F04 histórico, se requiere evidencia formal y ajuste controlado; revertir solo `CenitIncomingReturnPolicy.cs` de I04 previa validación del cambio y aislar fixtures NACHA de prueba. Riesgos I01 C5 mixta, I02 ROR histórico ambiguo, I03 simulación no oficial permanecen.
- **Estado:** `IMPLEMENTADO_PENDIENTE_VERIFICACION_AMBIENTE`; `HC-CENIT-004` **ABIERTO**, PT-07 y PT-17 **PENDIENTES**, casos cerrados **0/13**, UAT `RESERVADA_FASE_FINAL`, decisión de producción **NO-GO**. Evidencia `EVIDENCIAS/I04/INFORME_I04.md` y logs. **Siguiente I05 / HC-CENIT-005** desde ZIP integral I04.


## 13. Registro de ejecución I05 (2026-11-14)

- **Base exclusiva:** ZIP I04 SHA-256 `eaaa3f03ce9fe53c6febae7a64baed58415202266cec9126393a9b7232b39930`, integridad CRC OK. No cambios provenientes de otra entrega.
- **Hallazgo probado por fuente:** la política ya tenía diez valores 10..1, pero el motor simulador recorría `OrderBy(priority)` (nivel 1 primero para consumir liquidez, opuesto a preferir 10 cuando la norma describe mínima prioridad como primera diferible) y dejaba nivel 0 participar como candidato. Además la clasificación de devoluciones aceptaba códigos ordinarios/prenotificación, y faltaba el guard CENIT antes del snapshot de liquidez.
- **Solución:** clasificador de devoluciones solo `21/31/51` versus `26/36/56`, coherencia de transacciones ordinarias, nivel 0 `SimulatedUnclassified` sin consumo; simulación candidata 10→1, monto descendente, ID ascendente y cámara CENIT explícita. Sin movimiento de estado ni liquidación oficial. La decisión `SimulatedUnclassified` es considerada evidencia incierta por `AchPendingCycleEligibilityService`, y puede impedir un traslado automático hasta revisión; PT-06 debe revisar su interacción, no se desactiva preventivamente el guard.
- **Pruebas escritas:** `CenitI05PrioritySimulationTests.cs` 25 casos xUnit (10 tabla, 12 inválidos, 3 integraciones) y cinco InlineData negativos añadidos a la teoría existente, **0 ejecutados**. Regresión y control focal **270/270 PASS exclusivamente estáticas/documentales** (F01–F08 151; I01 15; I02 26; I03 22; I04 24; I05 32).
- **Ambiente:** .NET SDK 10.0.403 NO INSTALADO, sin dotnet restore/build/test, SQL/Quartz/CUD/operador; npm ci offline FAIL ENOTCACHED, audit offline 0 únicamente según caché local, advisories npm/NuGet actuales NO VERIFICADOS. Ninguna dependencia/migración alterada.
- **Estado y rollback:** HC-CENIT-005 **ABIERTO**, PT-06 pendiente; 0/13 cierres, UAT consolidada RESERVADA_FASE_FINAL, producción **NO-GO**. Revertir exclusivamente política/motor/test I05 en caso de contrato contradicho, sin reemitir archivos financieros. Ver `EVIDENCIAS/I05/INFORME_I05.md` y `RESULTADOS_I05.json`. **Siguiente I06 / HC-CENIT-006** exclusivamente desde ZIP integral I05.


## Registro I06 — HC-CENIT-006 (2026-11-14)

- **Base exclusiva:** ZIP I05 SHA-256 `6816e5507831f9806c907542203a505475930506c687e983064c27f666e4df4b`; CRC válido, no se importan fuentes de ningún otro ZIP.
- **Defecto estático y corrección focal:** en `CenitCycleExecutionService.StartExecutionAsync`, un reintento Quartz podía sobrescribir a `AwaitingOfficialResult` un estado no reconocido o ya marcado `CompletedAtUtc`. Ahora se preservan registros finalizados y el estado `AwaitingOfficialResult`; solo `Pending/Running` transitan a espera, y cualquier estado no reconocido sin finalización falla cerrado antes del cambio. No se crea rechazo `ReturnedByOperator` ni liquidación por insuficiencia simulada.
- **Regresión:** 151/151 F01–F08 + 119/119 I01–I05 + 32/32 I06 = **302/302 comprobaciones de fuente/documentales** realmente ejecutadas; no prueba comportamiento .NET. Dos teorías y dos Facts agregados (**6 variantes xUnit; 0 ejecutadas**) para liquidez 0/1000, reintentos, estado final, estado anómalo y NACK oficial correlacionado posterior/replay.
- **Ambiente:** .NET 10 SDK, SQL/Quartz/CUD/cámara ausentes; restore, build, xUnit, PT-04/PT-05 y homologación **NO_EJECUTADOS**. `npm ci --offline` FAIL ENOTCACHED zone.js; `npm audit --offline` reporta 0 solo según caché local; vulnerabilidades actuales npm/NuGet NO_VERIFICADAS. Sin cambio de versiones ni migraciones.
- **Riesgos y gobierno:** reconciliación de sesión y concurrencia Quartz/importación oficial requieren DB/operador. No se presume que `CenitChamberResponseService` finalice `CenitCycleExecution` al aplicar NACK de transacción. HC-CENIT-006 **ABIERTO**, 0/13 casos cerrados, UAT RESERVADA_FASE_FINAL y producción **NO-GO**. Ver `EVIDENCIAS/I06/INFORME_I06.md`, `RESULTADOS_I06.json`, patch, inventario y logs. Siguiente **I07**, exclusivamente del ZIP I06.


## Registro I07 — HC-CENIT-007 (2026-11-15)

- **Base exclusiva:** ZIP I06 SHA-256 `6ea66136355f746709bf028a4dbb229487779869c4809c76230e7f0321847513`, se preserva todo I06, sin importar código de otra entrega.
- **Brecha y corrección:** C5 R06 exigía C4 pero comparaba solo Fecha Valor. Ahora el guard compara día operacional real de C4 y C5; el modelo tiene `ReturnOperationalDate` opcional. Ambos callsites (ReturnOut e ingesta) aportan `AchCycle.ProcessingDate` y la ingesta R06 bloquea numeración detectada conflictiva frente al ciclo resuelto, sin permitir que un hint simule una sesión distinta.
- **Pruebas:** nueva clase xUnit con **13 métodos / 21 variantes escritas, 0 ejecutadas**; fixtures F04/F07 actualizados. Regresión **151/151** F01–F08, **151/151** I01–I06, control I07 **37/37**: **339/339 comprobaciones estáticas/documentales PASS**. No constituyen compilación.
- **Ambiente y riesgo:** falta SDK .NET 10.0.403, SQL/PostgreSQL/Quartz y acceso a operador; no dotnet restore/build/test ni NACHA-M físico/PT-09. `npm ci --offline` FAIL ENOTCACHED zone.js, audit offline 0 según caché y `npm outdated --offline` FAIL. Vulnerabilidades y versiones actuales NuGet/npm **NO VERIFICADAS**. Sin migraciones ni actualizaciones de dependencia; ACH Colombia sin cambio productivo. R06 C5 sin fecha confiable exige revisión manual.
- **Estado:** HC-CENIT-007 **ABIERTO**, PT-09 pendiente, 0/13 formalmente cerrados, UAT `RESERVADA_FASE_FINAL`, producción **NO-GO**. Ver `EVIDENCIAS/I07/INFORME_I07.md`, `RESULTADOS_I07.json`, diferencias, comprobaciones, baseline SHA. **Siguiente I08 / HC-CENIT-008** desde ZIP I07.


## Registro I08 — HC-CENIT-008 (2026-11-15)

- **Base exclusiva:** ZIP integral I07 SHA-256 `97cc3009bb45ad09dd95301136ee052b48b9a293d84513a6067a5d1e43d03d79`. No se mezclaron otras versiones del código ni se duplicó documentación normativa externa. Se preserva el historial íntegro.
- **Defecto estático demostrado:** `CenitReturnOfReturnService` y `AchReturnOfReturnFileGenerationService` comprobaban R74 con consultas duplicadas, aceptando referencias parciales como sustituto del campo 3 físico; la relación entre adenda 05 y **registro original de crédito** carecía de una corroboración explícita de traza, importe, código monetario y enlace final.
- **Corrección mínima:** `CenitR74OriginalCreditEvidence` centraliza validación para ROR entrante/saliente y generación; exige crédito monetario no prenotificación, código 22/32/42/52, adenda original tipo 05 sin causal de retorno, enlace final al registro original con traza/importe/código/NachaID coherentes. Para adenda física CENIT valida campo 3 (80 caracteres, ID numérica no cero y reservados PPD); si hay raw 05 este prevalece sobre referencias locales. Para créditos salientes sin raw permite evidencias semánticas 05 propias con identificación empresarial no vacía numérica. R74 solo con padre R32, reglas de flujo, adenda ROR 99, duplicados e I02 conservadas.
- **Pruebas escritas, no ejecutadas:** 15 variantes xUnit adicionales en `CenitReturnOfReturn2026Tests.cs`: forma del campo 3 de registro de 106 caracteres, raw positivo/negativo, reserva, crédito/prenotificación/débito, código monetario inconsistente, retorno 99 ajeno, traza del original incorrecta, conflicto con referencia local, registro externo ROR 99/R74 con fuente R32, replay. **PT-10 permanece PENDIENTE** hasta archivo físico y evidencia relacional reales.
- **Comprobaciones reales:** 151/151 F01–F08 + 188/188 I01–I07 + 44/44 I08 = **383/383 SOLO ESTÁTICAS/DOCUMENTALES**. SDK .NET 10 ausente: 0 `dotnet restore/build/test`, 0 xUnit; bases de datos, Quartz, cámara CENIT/ACK no ejecutados. `npm ci --offline` falla ENOTCACHED (`zone.js`); auditoría npm offline no verifica avisos actuales; NuGet/npm `VULNERABILIDADES_NO_VERIFICADAS`. No cambios de dependencias, migraciones, esquema, ACH Colombia ni frontend.
- **Riesgo:** documentación raw heredada sin `PaymentRelatedInformation` completo queda sometida a verificación de ID y traza; puede requerir revisión manual legítima. Validar flujo positivo de adenda 05 desde fuente con DB y generación real y pruebas simultáneas sobre unicidad. Reversión limitada del helper/callsites, nunca anular o reemitir transacciones oficiales automáticamente.
- **Estado:** I08 `IMPLEMENTADO_PENDIENTE_VERIFICACION_AMBIENTE`; HC-CENIT-008 **ABIERTO**, **0/13** cierres. UAT `RESERVADA_FASE_FINAL`, producción **NO-GO**. Evidencia `EVIDENCIAS/I08/INFORME_I08.md`, `RESULTADOS_I08.json`, patches, hashes y logs. **Siguiente I09** exclusivamente desde ZIP integral I08.


## Registro I09 — 2026-11-15

- **Base exclusiva:** ZIP I08 SHA-256 `8b54f60eeb7c0a68cab35ccd14bc7bece294e0d6fc198d6604b4bfce2ba97021`. Sin incorporación de otro ZIP, normativa fuera del paquete, migraciones ni dependencias nuevas.
- **Defecto y corrección:** R23 posterior contrastaba fecha valor contra día 15 y el plazo calculado; una fecha valor backdated podía ocultar la presentación real. `CenitIncomingReturnPolicy` verifica día real del ciclo receptor, plazo calendario inclusivo y notificación bancarizada a Bogotá; las rutas salida/entrada calculan siguiente día hábil desde la fecha de notificación local. El control genérico `Credit MaxDays=1` se exceptúa únicamente para CENIT crédito R23, usando recepción original resuelta y límite de 15; ACH Colombia y otras causales sin cambios.
- **Pruebas:** control I09 y regresión F01–F08/I01–I08 **solo estáticos y documentales**; escenarios xUnit y SQLite escritos **sin ejecutar**. SDK .NET, Docker/DB, CENIT/CUD, UAT no disponibles. npm ci offline FAIL ENOTCACHED (`zone.js`), advisories actuales npm/NuGet no verificadas. PT-08 pendiente. No se infiere aceptación bancaria ni estado de producción.
- **Riesgo:** confirmar origen/auditoría de `ReceiverNotificationReceivedDate` y si `AchCycle.ProcessingDate` coincide con entrada efectiva al Participante Receptor; el calendario local debe homologarse con festivos y fechas especiales CENIT. La simulación NACHA de respuestas mantiene su filtro de ventana independiente, que requiere ensayo en PT-08; no es evidencia bancaria.
- **Decisión:** `HC-CENIT-009` ABIERTO, 0/13 cierres; UAT única al final y producción NO-GO. **Siguiente I10** con línea base exclusiva ZIP I09.


## Registro I10 — 2026-11-15

- **Base exclusiva:** ZIP I09 SHA-256 `af88b09bb1d56f7f28a03b2b7a738da806f3bd9b79f889bfd398503d91e79034`; cambios limitados a parser físico, generador, prueba nueva, script I10 y documentación de evidencia/continuidad.
- **Corrección:** comprobación final del alfabeto CENIT antes de hash y registro; sin conversión silenciosa de caracteres ilegales, con predicado compartido por parser de entrada. ACH Colombia sin cambio de perfil.
- **Ejecución real:** 470/470 controles estáticos/documentales (443 heredados + 27 I10). Pruebas C# agregadas pero 0 ejecutadas; falta SDK .NET, SQL/Quartz/gateway. npm ci offline ENOTCACHED, advisories actuales no verificados. Sin cambios de dependencias ni migraciones.
- **Decisión:** I10 `IMPLEMENTADO_PENDIENTE_VERIFICACION_AMBIENTE`, PT-11/PT-20 **PENDIENTES**, HC-CENIT-010 **ABIERTO**, 0/13 casos cerrados, UAT única final, producción NO-GO. Siguiente I11 desde ZIP integral I10.


## Registro I12 — HC-CENIT-012 (2026-11-15)

- **Única base de código:** ZIP integral I11 SHA-256 `16e0df38b57345ac6aae654df5db158039186a35c954a2bbf79953ce0993a498`; integridad CRC verificada. No se importaron archivos de código de otros ZIP. La norma se consultó como criterio, no como nueva base de código.
- **Defecto CE-01 confirmado:** `CenitChamberResponseService.ParseAdv` requería `Split('\n')`, a pesar de registros NACHA-M físicos consecutivos de 106 posiciones; metadatos `Reconciliation` tampoco reconocían ADV contiguo. Los avisos 81–88 y perfil 280/ADV ya existían y fueron preservados.
- **Corrección I12:** lector de tramos de 106, compatibilidad explícita con el transporte histórico delimitado por líneas, validación del repertorio CENIT, encabezado, clase/SEC, código y monto, sumas y ruta de lote, totales de archivo, número de bloques, espacios reservados y relleno 9; comprobación íntegra antes de persistir evidencia. No se infiere asiento CUD o estado financiero. `Reconciliation` con registro tipo 1/5 se somete a validación ADV en vez de clasificarse opacamente.
- **Pruebas escritas:** ampliación `CenitChamberResponseLifecycleTests.cs`: 4 nuevos métodos, 13 variantes negativas del método parametrizado, pruebas positivas de 81–88 e idempotencia/compatibilidad; **0 casos xUnit ejecutados**. Fixture sintético empaquetado sin CR/LF con 20 registros físicos (SHA-256 `fa8f280076b216cdf604eec1466e5f3ff316d6de9d4a70ce9446e84816023009`) y **16/16 validaciones independientes del fixture**, que NO ejercitan el código C#.
- **Pruebas ejecutadas:** regresión de fuente **468/468** + focal I12 **41/41** = **509/509 comprobaciones estáticas/documentales**. .NET restore/build/test: **NO_EJECUTADOS, SDK ausente**. SQL/Quartz/operador/conciliación Tesorería PT-13, PT-19, PT-23: **NO EJECUTADOS**. npm ci offline: FAIL ENOTCACHED zone.js; npm audit offline muestra 0 **solo caché local**, advisories vigentes npm/NuGet **NO_VERIFICADOS**. No se cambiaron paquetes, migraciones, frontend ni ACH Colombia productivo.
- **Estado y riesgos:** `IMPLEMENTADO_PENDIENTE_VERIFICACION_AMBIENTE`; HC-CENIT-012 **ABIERTO**, 0/13 cerrados. Requiere PT-23 rojo→verde real en .NET, SQL y comparativa con archivos oficiales para PT-13; PT-19 exige conciliación manual por Tesorería, sin CUD automático. Interpretación del transporte con saltos de línea mantenida solo por compatibilidad histórica y pendiente de confirmación por operador. Sin UAT por incremento; UAT FINAL y producción **NO-GO**.
- **Rollback:** conservar el ZIP I11 previo, revertir solo el lector ADV y el script F02 de verificación tras autorización; preservar acuses/identificadores originales para reproceso controlado. Siguiente **I13 / HC-CENIT-013** sobre ZIP integral I12.
- **Evidencia:** `EVIDENCIAS/I12/INFORME_I12.md`, `RESULTADOS_I12.json`, `fixture_validation.json`, logs, diferencia y hashes. SHA del ZIP I12 se entrega externamente para evitar autocitación circular.


## Registro I13 — HC-CENIT-013 (2026-11-15)

- **Base de código exclusiva:** ZIP integral I12 SHA-256 `3b47395c4eb58e984ff259e5b6d6357dc47eaaff071ead7acb74e8fd8f79fdf7`; integridad CRC verificada; no se usaron fuentes de código de otras versiones.
- **Lo que ya cumplía I12:** importación de NACK transaccional por `TraceNo`, estado `ReturnedByOperator`, evento individual y preservación de `AchFileExport.ChamberResponseState`. No se duplicó esta lógica ni se crearon APIs de liquidación.
- **Brechas probadas por inspección:** (a) la exportación podía correlacionar una traza a un `AchTransactionId` cuya traza canónica era distinta; (b) dos causales contradictorias para una misma traza en un único mensaje podían aplicar la primera arbitrariamente; (c) la recuperación ante choque `DbUpdateException` de identidad de fuente devolvía `IsDuplicate=true` sin comparar el hash del ganador.
- **Corrección focal:** `CenitChamberResponseService.cs` compara traza de transacción antes de alterar estado, anula `AchTransactionId` del resultado cuando hay conflicto y registra `CENIT_TRANSACTION_TRACE_MISMATCH`; detecta causales contradictorias de la misma traza antes de aplicar cualquier fila de esa traza y marca cada ítem `CENIT_NACK_CONFLICTING_REASONS_FOR_TRACE` conservándolo para revisión; en carrera de respuesta de la misma fuente compara `ContentSha256` del ganador y devuelve `CENIT_RESPONSE_ID_CONFLICT` si difiere. Otros ítems válidos siguen su tratamiento independiente.
- **Pruebas escritas:** cinco métodos xUnit SQLite para tres transacciones con ACK técnico + NACK individual, NACK múltiples distintos en el mismo archivo, conflicto de razones, asociación inconsistente y conflicto de fuente; **0 ejecutados**. Los controles `PT-14/PT-15` de concurrencia real multi-worker, datos antes/después en SQL Server/PostgreSQL y fixtures oficiales permanecen pendientes. El ACK técnico no acredita por sí solo estados individuales.
- **Ejecución reproducible:** 542/542 (509 heredados + 33 I13) verificaciones de fuente/documentales PASS; ocho validaciones independientes de fixtures XML sintéticos PASS (no ejecutan C#). `dotnet restore/build/test`: **NO_EJECUTADOS, SDK 10.0.403 no instalado**. SQL, Quartz, gateway y homologación: **NO_EJECUTADOS**. `npm ci --offline` falla `ENOTCACHED` (`zone.js`), `npm audit --offline` muestra cero avisos solo de caché local, `npm outdated --offline` falla; seguridad NuGet/npm **NO VERIFICADA**. Sin cambios de versiones de dependencias ni migraciones.
- **Riesgo residual:** concurrencia real en dos workers, estados históricos, diferencias de proveedores, idempotencia en fallas transitorias; no se certifica con pruebas estáticas. Remediaciones AC-01..04 y SEC-01 permanecen en cola transversal, no son casos CENIT nuevos.
- **Estado:** I13 `IMPLEMENTADO_PENDIENTE_VERIFICACION_AMBIENTE`; **HC-CENIT-013 ABIERTO**, **0/13** cierres formales, `UAT = RESERVADA_FASE_FINAL`, producción **NO-GO**. Evidencia `EVIDENCIAS/I13/`.
- **Rollback:** conservar ZIP I12 y evidencia de las respuestas, revertir únicamente lógica adicional de `CenitChamberResponseService` tras evaluación operativa y recuperación autorizada de estados; nunca alterar o reemitir instrucciones financieras automáticamente.
- **Siguiente actividad:** **G1–G3 técnicas + deuda de PT-01..PT-23 + seguridad**, luego **G4 UAT única** y **G5 dictamen/cierre**. No abrir I14.


## Adenda focal NPT-CFA-LATE-01 — 2026-11-15

Se corrigió solamente el archivo productivo `IncomingNachaPostProcessingPipeline.cs`: la respuesta tardía ahora se archiva como evento con idempotencia por PK determinista, conservando el intento y la cola en conciliación. Sin cambios de migración, WSDL, Axon, CENIT ni ACH Colombia. Se incorporaron seis casos xUnit reproducibles, pendientes de ejecución por SDK ausente. Siguiente paso técnico: restaurar/compilar .NET 10; ejecutar xUnit, dos proveedores y contadores SOAP locales. NO-GO y UAT final pendiente. Ver `ACHInterbank_ajustado/docs/hotfix-npt-cfa-late-01/` para informe, matriz y evidencias.
