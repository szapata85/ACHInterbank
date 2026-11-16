# Informe — tres hotfixes predespliegue (2026-11-15)

**Línea base exclusiva:** `ACHInterbank_PROYECTO_COMPLETO_I13_ROR_C5_HOTFIX_2026-11-15.zip`. **SHA-256:** `3600efca392c1664701d066810aa9df11d8f2b55e282e1f18496a76e4b9001aa`. Archivo con cuatro elementos superiores: `ACHInterbank_ajustado/`, `EVIDENCIAS/`, `PLAN_IMPLEMENTACION_13_CASOS/` y `LEER_PRIMERO.md`. Entrega acumulada I13 con HC-CENIT-002 conservado. **No se abrió I14 ni se repitió auditoría CENIT.**

## HOTFIX-01 — ACH Colombia D01/D02/D04

**Antes:** `NachaInboundValidator.ValidateEntrySequenceStructure`, `ValidateBatchSequenceAndControlConsistency`, `ValidateCurrentBatchControl`, `ValidateFileControlConsistency` y `NachaInboundDatabaseValidator.ValidateEntrySequencePolicyAsync` asociaban errores estructurales a D04/D02/D01. `NachaParserService.ParseAndSaveDetailedAsync` usaba D01 para archivo duplicado y D02 para relleno incorrecto; la prenotificación no cero no reportaba D04. Algunos catálogos de pruebas conservaban descripciones antiguas incorrectas.

**Después:** fallas técnicas de secuencia, duplicado de archivo, conteo físico, lotes y relleno dejan de atribuir D01/D02/D04 y se reportan como `Technical` (fallo fatal local, no causal inventada). `ValidateAchColombiaEffectiveDate` aplica **D02** para fecha efectiva inválida y **D01** para efectiva anterior a la fecha operativa *resuelta del ciclo*; no usa la fecha de creación del archivo como calendario oficial cuando no se resolvió un ciclo. `ValidateEntryAsync` emite **D04** si la prenotificación ACH Colombia tiene monto distinto de cero; CENIT conserva su propio dominio de errores. `GetRegulatoryError` fija la descripción normativa V35 de estas tres causales aun si existen filas históricas con semántica incorrecta.

**Norma:** `docs/normativa/md/ACH-Colombia-V35.md`, Anexo 3: D01 fecha anterior, D02 fecha inválida, D04 prenotificación con monto no cero. No equivalen a D01/D02/D04 de STA-CENIT (`docs/normativa/md/CENIT-Anexo-B-Causales-Rechazo.md`).

**Pruebas escritas (no ejecutadas):** casos xUnit de fecha anterior, fecha inválida, fecha válida/futura, aislamiento CENIT, monto de prenotificación, conteo técnico y regresión de secuencia en `NachaInboundValidationSeparationTests.cs`. Se actualizaron descripciones de los catálogos sintéticos de tres suites de procesamiento. **Riesgo residual:** no se ejecutó parser real. Validar con archivo NACHA físico que D01, D02, D04 y el control de fecha de procesamiento coinciden con el comportamiento del operador. Las rutas D05/D06 preexistentes pueden requerir revisión posterior, pero NO se modificaron por estar fuera del alcance de estos tres hotfixes.

## HOTFIX-02 — ACH Colombia R10, C1–C7

**Antes:** `AchReturnEligibilityPolicyComponent.ValidateAchColombiaR10Async` ordenaba la jornada por `TimeSpan StartTime`; C1 que comienza a las 19:01 del día calendario anterior quedaba después de C2–C7.

**Después:** se ordena por `ResolveOperationalCycleStart(AchCycle)`, fecha/hora completa del inicio operacional. Para `Ciclo 1`/`C1` que cruza medianoche se usa día calendario anterior, conservando `ProcessingDate` como fecha operativa. Se mantiene `MaxCycles` efectivo, la secuencia de oportunidades `AllowsReturn`, base R10 y rechazo fuera del día operativo.

**Pruebas escritas (no ejecutadas):** siete filas parametrizadas C1–C7 con límite cuatro, escenario límite positivo C4 y negativos C5–C7, orden en cruce de medianoche y convención `C1`; permanece la batería histórica R10 previa. Archivo `AchReturnsFileByClearingHouseTests.cs`.

**Riesgo residual:** confirmar en runtime los valores efectivos de `StartTime`, `CutoffTime` y `CycleName` en todas las políticas/configuraciones institucionales; validar días hábiles y feriados con datos reales tras despliegue.

## HOTFIX-03 — CENIT calendario bancario ROR

**Antes:** `CenitReturnOfReturnService.ValidateOperationalWindowAsync` preservaba C5 obligatorio (HC-CENIT-002), pero admitía destinos intradía de sábado, domingo o festivo si existía la fila del ciclo.

**Después:** `IOperationalCalendarService.IsBusinessDayAsync` bloquea una fecha destino no hábil en la rama intradía C5 y en la rama del siguiente día hábil (`CENIT_ROR_NON_BUSINESS_DAY`); no añade excepciones ni crea servicio de calendario. `GetNextBusinessDayAsync`, límite C2 y regla C5/parental C2–C4 permanecen. Ambas direcciones (`CreateOutgoingAsync` e `IngestIncomingAsync`) invocan el mismo validador. No se modifica persistencia ni esquema.

**Pruebas escritas (no ejecutadas):** las fechas operativas sintéticas antiguas del domingo 2026-08-16 fueron trasladadas al miércoles 2026-08-19, con vencimiento siguiente día hábil jueves 20, fecha límite viernes 21, y datos de secuencia día del año ajustados de 228 a 231 donde corresponde. Los fixtures físicos de formato no operacional conservan su fecha original. Se agregaron matrices negativas para sábado 2026-08-22, domingo 2026-08-23 y festivo de Asunción 2026-08-17, escenarios entrantes/salientes, C5 hábil y próximo día con destino no hábil. **Riesgo residual:** falta verificar `OperationalCalendarService` con infraestructura y calendarios institucionales, especialmente festivos y cierres especiales parametrizados.

## Evidencias ejecutadas

- **31/31** verificaciones documentales de código de los tres hotfixes (`evidencias/hotfix_31_controles_estaticos.log`).
- **20/20** verificaciones previas del hotfix HC-CENIT-002 (`evidencias/regresion_hc_cenit002_20_controles.log`).
- **12/12** scripts disponibles `cenit_iXX_*` concluyeron con código 0; **391/391** comprobaciones estáticas en esos 12 scripts (`evidencias/regresion_cenit_incrementos.json`, logs individuales). El script I11 no existe con el patrón IXX; la separación del catálogo CENIT se verificó en el focal.
- **Grafo `.sln`: PASS**, siete proyectos en `src/` y `tests/`, sin referencias `ProjectReference` ausentes (`evidencias/grafo_sln.log`).
- **SDK .NET 10: AUSENTE** (`dotnet`: command not found). No se ejecutaron `dotnet restore`, `dotnet build`, `dotnet test`, xUnit, SQL Server, SQLite/PostgreSQL, pruebas de integración ni UAT. **No hay pruebas runtime aprobadas.**
- No se cambiaron `*.csproj`, `*.sln`, `global.json`, versiones de NuGet ni migraciones. No se hicieron actualizaciones de dependencias. Se conservan scripts y configuración originales.

## Estado y límites

- **Tres hotfixes:** código implementado; pruebas C# agregadas pero **sin ejecución**. Estado: `IMPLEMENTADO / PENDIENTE DE COMPILACIÓN Y REGRESIÓN DINÁMICA`.
- **13 HC históricos:** implementaciones y documentos originales preservados; **0/13 cerrados formalmente**, sujeto a pruebas PT existentes, evidencias runtime y homologación independiente. UAT no se realizó. Producción **NO-GO**.
- **Fuera de alcance sin alterar:** migraciones, código financiero de CUD, motor de compensación, reglas R06/R23/R74, otros catálogos, UI, frameworks, infraestructura de pruebas y dependencias.
- **Siguiente actividad:** en entorno con SDK .NET 10.0.403, ejecutar `python scripts/validate-solution-projects.py`, `dotnet restore ACHInterbank.sln`, `dotnet build ACHInterbank.sln -c Release`, `dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --filter "FullyQualifiedName~NachaInboundValidationSeparationTests|FullyQualifiedName~AchReturnsFileByClearingHouseTests|FullyQualifiedName~CenitReturnOfReturn2026Tests"` y luego regresión global más SQLite/SQL Server/PostgreSQL conforme infraestructura. Registrar TRX, corregir eventuales fallos, continuar recorrido de despliegue **solo para pruebas técnicas**, no UAT.

## Archivos y preservación

Fuente modificada:

- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaInboundValidation.cs`
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaParserService.cs`
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchReturnEligibilityPolicyComponent.cs`
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/CenitReturnOfReturnService.cs`

Pruebas modificadas: `tests/Cfa.ACHInterbank.Tests/{NachaInboundValidationSeparationTests,AchReturnsFileByClearingHouseTests,CenitReturnOfReturn2026Tests,NachaIncomingEndToEndProcessingTests,NachaFunctionalValidationTests,NachaUploadPostgresUatEndToEndTests}.cs`.

Prueba estática nueva: `scripts/test/predeploy_three_hotfixes_static_check.py`.

Documentación actualizada en sus ubicaciones originales: `ACHInterbank_ajustado/MEMORY_STATE.md`, `PLAN_IMPLEMENTACION_13_CASOS/MEMORY_STATE.md`, `PLAN_IMPLEMENTACION_13_CASOS/MEMORY_STATE.json`, `PLAN_IMPLEMENTACION_13_CASOS/PLAN_IMPLEMENTACION_13_CASOS.md` y `LEER_PRIMERO.md`. Se preservó cada historial completo como bloque anterior o como campos previos del JSON. `EVIDENCIAS/I00`–`EVIDENCIAS/I13` y toda la evidencia histórica de nivel superior se conservan byte a byte. Otras fuentes y configuraciones originales se conservan sin editar.
