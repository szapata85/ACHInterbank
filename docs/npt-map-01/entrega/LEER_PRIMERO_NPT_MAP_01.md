# NPT_H02_H03_FINAL — LEER PRIMERO (16-11-2026)

**HOTFIX SOLO H-02/H-03. NO-GO SOAP Live.** Fuente: `ACHInterbank_HOTFIX_XSD_TRANSPORTE_NCTAORIG_H010203_2026-11-16.zip`.

Regla definitiva: **`Transactions.SourceAccountNumber → Proc_Transacciones.NCTAORIG`**, sin cuenta receptora, constantes ni fallbacks. Se retiró exclusivamente el bloqueo de *homologación de procedencia* (`PROC_TRANSACCIONES_NCTAORIG_CONTRACT_NOT_HOMOLOGATED` y guard equivalente), no los controles de origen válido, XML, permisos, configuración, idempotencia ni F5 ACH Colombia file-based.

Informe y evidencias: `ACHInterbank_ajustado/docs/npt-h02-h03-final/INFORME_NPT_H02_H03_FINAL.md`.

**Validación real:** 23/23 comprobaciones focales estáticas; seis regresiones Python/SQLite con salida 0; validación XML Schema offline salida 0. Se intentaron `dotnet --info`, `restore`, `build`, `test`; todos **exit 127** por falta del SDK. **xUnit no ejecutado**; no SQL Server/PostgreSQL, UAT ni SOAP Live.

**Estado:** H-02 corregido en fuente, H-03 pruebas corregidas; ambas pendientes de compilación y xUnit en .NET 10. Sin nuevas migraciones, dependencias ni modificación de WSDL/XSD.

---

# LEER PRIMERO — H-01/H-02/H-03 · XSD en transporte (2026-11-16)

**Estado NO-GO Live.** ZIP íntegro actualizado sobre `ACHInterbank_NPT_SOAP_XSD_FINAL_01.zip`; leer `ACHInterbank_ajustado/docs/npt-transport-xsd-h010203/INFORME_H010203.md`. H-01: XSD final para clientes CFA/AXON XML directo, sin POST de XML inválido; H-02: guard Live cuenta originador `NCTAORIG` ausente, vacío y nil; H-03: readiness negativo coherente. XSD offline 38/38, fuente 33/33, focal 21/21, 7 regresiones Python exit 0; NPT general heredado 40/42. `dotnet` exit 127 sin SDK: no build/xUnit. No migraciones, no UAT, no Live. Dependencia restante: homologación oficial cuenta origen y autorización operativa F5 por separado.

---

# LEER PRIMERO — NPT_SOAP_XSD_FINAL_01 (2026-11-16)

**Última entrega del proyecto completo. Estado: NO-GO SOAP Live.** Dentro de `ACHInterbank_ajustado/docs/npt-soap-xsd-final-01/` están el informe, la matriz WSDL/XSD, los comandos, las evidencias y el plan. Los cinco XSD originales (2 CFA y 3 Axon) están embebidos y validados offline. Se añadió validación XML Schema a solicitudes y respuestas, controles exactos de `SOAPAction` y validación de fuente de `NCTAORIG`, manteniendo F5 sin autorización y el guard de Live. NPT-01/05 **PARCIAL**; NPT-10/14 **CORREGIDO TÉCNICAMENTE EN FUENTE, pendiente compilación**. Verificación `lxml` 38/38, verificación estática 33/33 y 7 regresiones históricas salida 0; verificador NPT anterior 40/42. `dotnet` ausente (127); xUnit NO ejecutados, sin UAT, sin SQL Server/PostgreSQL ni SOAP real. No se inventó homologación ni se autorizó Live.

---

# LEER PRIMERO — NPT_PRE_XSD_FINAL (2026-11-15)

**NO-GO SOAP Live / producción.** Base SHA-256 `07da2c52c5200f003a8b95dc3de20dc6d878fea48f2982c89d0289accbdd3d40` del HOTFIX NPT-CFA-LATE-01. Único incremento focal de NPT-01/02/05/11: F4.2 refuerza identidad file-based y sigue `Blocked`; físico diferencial 7/99 se cuarentena antes de parser sin adaptador (CENIT ROR F07 oficial preservado); mapping semilla `NCTAORIG` candidata = `transaction.sourceAccountNumber` pero readiness y Live siguen cerrados; correlación valida archivo, cámara, addenda, vínculo y referencia original.

**Informe:** `ACHInterbank_ajustado/docs/npt-pre-xsd-final/INFORME_NPT_PRE_XSD_FINAL.md` y evidencias en `evidencias/`. Pruebas C# nuevas o actualizadas **escritas y no ejecutadas** por SDK .NET ausente: `--info`, `restore`, `build`, `test` devolvieron 127. 31/31 controles Python de fuente/WSDL, regresiones seleccionadas sin error; verificador NPT heredado 41/42 (expectativa previa sobre Axon). No UAT, no SOAP Live, no migraciones ni datos normativos duplicados.

---

# LEER PRIMERO — HOTFIX NPT-CFA-LATE-01 (P1, 2026-11-15)

**Entregable:** ZIP completo, no diff, basado en `ACHInterbank_CIERRE_FOCAL_7_NPT_2026-11-15.zip`. Ruta: `ACHInterbank_ajustado/docs/hotfix-npt-cfa-late-01/INFORME_NPT_CFA_LATE_01.md`.

`PreserveLateEvidenceAsync` conserva resultados SOAP CFA tardíos como eventos append-only idempotentes, sin recrear `IncomingNachaIntegrationExecution` ni alterar conciliación. Seis pruebas xUnit SQLite añadidas, **no ejecutadas** (SDK .NET ausente). Verificador independiente de fuente/modelo SQLite **15/15 PASS**, regresiones históricas 5/5 + focal 26/26 PASS; verificador NPT previo 41/42 con discrepancia Axon preexistente. `dotnet --info/restore/build/test` todos exit 127. Sin migraciones nuevas, sin UAT, sin Live. **NO-GO productivo** hasta pruebas .NET e integración SQL Server/PostgreSQL.

---

# LEER PRIMERO — cierre focal NPT-03/04/06/08/09/10/11 (2026-11-15)

Este es el **proyecto completo** del ZIP anterior, con código focal, pruebas y evidencias nuevas. Comienza en `ACHInterbank_ajustado/docs/cierre-focal-npt-2026-11-15/INFORME_CIERRE_FOCAL_NPT.md`. Matriz en `MATRIZ_PRUEBAS_FOCAL.md`, diff exacto en `INVENTARIO_CAMBIOS.md` y comandos en `COMANDOS_REPRODUCIBLES.md`.

Siete hallazgos **corregidos en fuente, pendientes de validación .NET/SQL/SOAP**, no declarados cerrados en producción. 26/26 validaciones estáticas/modelo SQLite; cinco suites de regresión salieron 0; verificador NPT heredado 41/42 con única expectativa de diseño obsoleta. `dotnet` no instalado (exit 127), xUnit y pruebas SQL/HTTP no ejecutadas. NO-GO Live. NPT-07/12/13 preservados; NPT-01/02/05/14 no modificados y siguen bloqueados por homologación. No UAT ni migraciones nuevas.

---

# LEER PRIMERO — cierre incremental NPT-01…NPT-14 (15-11-2026)

**Paquete completo** del mismo árbol fuente, no parche diferencial. Base auditada SHA-256 `f28654ce10542c9acaec7d42d454a9ddd0c6e19744e8046c26141b620b42eaa9`. Se conservan proyectos, los artefactos previos, los 13 correctivos CENIT y hotfixes sin eliminar documentación normativa. **NO-GO SOAP Live y producción**.

Corregidos en fuente, **sin ejecutar xUnit ni bases**: NPT-03/04/06/07/08/09/10/11/12/13. Dependientes de homologación oficial: NPT-01/02/05/14. Los archivos físicos diferenciales y ACH Colombia file-based siguen bloqueados; el mapping `NCTAORIG` no se inventa. No se hizo UAT.

**Ruta principal:** `ACHInterbank_ajustado/docs/cierre-npt-2026-11-15/INFORME_IMPLEMENTACION_NPT_01_14.md`; matriz de 38 casos en el mismo directorio; evidencia de verificador sólo estático en `evidencias/`. `MEMORY_STATE.md` y `.json` actualizados tanto en el proyecto como en el estado canónico.

**Ejecución pendiente:** `dotnet restore ACHInterbank.sln`, `dotnet build ACHInterbank.sln -c Release --no-restore`, `dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --no-build`. Requiere dos bases de prueba, SOAP local determinista, y XSD bancarios para cerrar homologaciones. Ver `COMANDOS_REPRODUCIBLES.md`. **Los fakes y verificadores Python no son integración .NET.**

---

# ACH Interbank — LEER PRIMERO · RES-H02-01/02 (15-11-2026)

**Fuente única:** `ACHInterbank_PROYECTO_COMPLETO_HOTFIX_FOCAL_H01_H02_2026-11-15.zip` · SHA-256 `0c9662d1e44a8b68e7760ebe5c23c04b70b835acd61269cebdc2e3fcaf8de631`. Este ZIP conserva los **cuatro elementos raíz** (`ACHInterbank_ajustado/`, `EVIDENCIAS/`, `PLAN_IMPLEMENTACION_13_CASOS/`, `LEER_PRIMERO.md`). Sustituye el paquete anterior completo, no es un parche.

**Correcciones:** doble guarda de ciclo/lease inmediatamente antes del SOAP, con validación final del ciclo en BD; aborto atómico y auditable sin transmisión; claim programado reforzado por `AchCycleId`. Se reutiliza la reasignación segura de H02 y se preserva bloqueo por conciliación de E2E-02. No nuevas migraciones ni dependencias.

**Evidencia realmente ejecutada:** 27/27 controles focales estáticos/modelo SQLite, 12/12 scripts de regresión, grafo 7/7. **No ejecutado:** build .NET ni xUnit (`dotnet` código 127), pruebas SQL Server/PostgreSQL, SOAP integrado/Live y UAT. **NO-GO producción y SOAP Live** hasta pruebas runtime y coordinación de cierre de ciclo sin ventana TOCTOU.

**Rutas de continuidad:** `ACHInterbank_ajustado/docs/hotfix-res-h02-2026-11-15/INFORME_RES_H02_FINAL.md`, `PLAN_IMPLEMENTACION_RES_H02.md`, `ARCHIVOS_MODIFICADOS.md`, `evidencias/`, estados `ACHInterbank_ajustado/MEMORY_STATE.*`.

**Comandos pendientes (no ejecutados satisfactoriamente):**
```bash
cd ACHInterbank_ajustado
dotnet restore ACHInterbank.sln
dotnet build ACHInterbank.sln -c Release
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj --filter "FullyQualifiedName~ContrapartidaDispatchJobServiceTests"
```
UAT reservada para cierre general. No se duplicó documentación normativa.

---

## Histórico intacto de entregas previas

# ACH INTERBANK — LEER PRIMERO (HOTFIX H01/H02 · 2026-11-15)

**ZIP fuente:** `ACHInterbank_PROYECTO_COMPLETO_AUD_E2E01_E2E04_FINAL_2026-11-15.zip` · SHA-256 `5bca98ea7f78402b92b7104cbf0d1522135a5529fdb466fd50775374eab1d85c`. Este paquete reemplaza al ZIP previo **como proyecto completo**, no como parche parcial.

**Estado actual: NO-GO para producción.** H01/H02 están implementados en fuente y cuentan con pruebas xUnit añadidas, pero no se ejecutaron la compilación .NET ni las pruebas C# porque el entorno no tiene SDK. **UAT no se ejecutó** (reservada para el cierre general).

**Inicio de lectura:** `ACHInterbank_ajustado/docs/hotfix-h01-h02-2026-11-15/INFORME_H01_H02.md`; plan `ACHInterbank_ajustado/docs/hotfix-h01-h02-2026-11-15/PLAN_IMPLEMENTACION_H01_H02.md`; evidencias e inventario `ACHInterbank_ajustado/docs/hotfix-h01-h02-2026-11-15/` y `ACHInterbank_ajustado/MEMORY_STATE.md` y `ACHInterbank_ajustado/MEMORY_STATE.json`.

**H01:** Recuperar un lease en fase `Preparing` no consume intento SOAP; los eventos internos reciben número negativo de auditoría distinto de la numeración de envíos. Las filas antiguas agotadas pasan a conciliación sin retransmitirse automáticamente; `InFlight`/resultado incierto continúa protegido.

**H02:** Los reintentos manuales validan y utilizan un ciclo abierto, vigente, permitido para débito; un origen cerrado no se reutiliza. La reasignación y la reclamación se confirman atómicamente. Sin ciclo seguro, la operación queda pendiente/diferida, sin SOAP, hasta uno permitido. El historial monetario originario no se altera.

**Para validar fuera de este entorno (no ejecutado aquí):**

```bash
cd ACHInterbank_ajustado
dotnet --info
dotnet restore ACHInterbank.sln
dotnet build ACHInterbank.sln -c Release --no-restore
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --no-build --filter "FullyQualifiedName~ContrapartidaDispatchJobServiceTests"
# Después ejecutar test suite completa y carreras contra SQL Server y PostgreSQL.
```

**Ejecutado en este entorno:** `cd ACHInterbank_ajustado && python scripts/test/h01_h02_static_sqlite_check.py` (39/39) y 11 verificadores heredados, todos salida 0; ver `ACHInterbank_ajustado/docs/hotfix-h01-h02-2026-11-15/evidencias/COMMAND_EXIT_CODES.json`. Estos controles NO son compilación .NET ni integración SOAP.

**Migraciones:** no se necesitan cambios de esquema nuevos. No se han modificado las migraciones preexistentes. Ningún documento normativo se añadió o duplicó.

---

## Historial de entregas anteriores (conservado)

# LEER PRIMERO — ACTUALIZACIÓN FINAL AUD-E2E-01..04 (2026-11-15)

**Archivo fuente único:** `ACHInterbank_PROYECTO_COMPLETO_E2E01_E2E02_2026-11-15.zip` · SHA-256 `df172cf3d96079792ea19091cead336b7a76455dcacd7c6929c2a6b8c2989b55`. Correcciones del P0 de alcance de `dbTransaction` y de los P1 de CAS de recuperación, ciclo cerrado y evidencia SOAP anterior a transporte incorporadas a `ACHInterbank_ajustado/`. Migraciones PostgreSQL/SQL Server `20261115183000_E2EFinalCycleAndSoapIntent.cs`, 8 xUnit adicionales y reporte `ACHInterbank_ajustado/docs/hotfix-aud-e2e01-e2e04-2026-11-15/INFORME_FINAL.md`.

**Validaciones ejecutadas:** 31/31 focales estáticas + SQLite modelo; 35/35 estáticas previas; 9/9 regresiones estáticas ACH/CENIT, todas salida cero. **No se compiló, ni se ejecutaron pruebas xUnit, ni se aplicaron migraciones reales, ni se hizo UAT**. Falta `dotnet` y bases SQL Server/PostgreSQL. **Producción y homologación: NO-GO.**

No copiar documentación normativa ni descartar el resto de esta entrega. Ver plan y lista de cambios en el informe nuevo.

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

# LEER PRIMERO — Tres hotfixes predespliegue ACH Interbank

**Fecha:** 2026-11-15 · **Fuente exclusiva:** `ACHInterbank_PROYECTO_COMPLETO_I13_ROR_C5_HOTFIX_2026-11-15.zip` (SHA-256 `3600efca392c1664701d066810aa9df11d8f2b55e282e1f18496a76e4b9001aa`). El ZIP fuente tenía cuatro elementos superiores: `ACHInterbank_ajustado/`, `EVIDENCIAS/`, `PLAN_IMPLEMENTACION_13_CASOS/` y `LEER_PRIMERO.md`. Todos se preservan; NO es suficiente conservar solo el árbol de proyecto.

**HOTFIX-01:** D01/D02/D04 ACH Colombia (V35). **HOTFIX-02:** R10 ACH Colombia, orden real C1–C7. **HOTFIX-03:** ROR CENIT, fecha operativa hábil, con HC-CENIT-002/C5 y siguiente día hábil conservados.

**Código:** cambios implementados en cuatro servicios/componentes C# y seis archivos de pruebas existentes; pruebas unitarias añadidas, **sin ejecutar**. **Evidencia ejecutada:** 31/31 controles estáticos focales, 20/20 estáticos HC-CENIT-002, 12/12 verificadores históricos (391/391 aserciones estáticas), grafo .sln 7/7. **No ejecutado:** `dotnet restore/build/test` por SDK ausente; tampoco UAT. **0/13 hallazgos históricos formalmente cerrados. NO-GO producción.**

**Estado y plan existentes actualizados:** `PLAN_IMPLEMENTACION_13_CASOS/MEMORY_STATE.md`, `PLAN_IMPLEMENTACION_13_CASOS/MEMORY_STATE.json`, `PLAN_IMPLEMENTACION_13_CASOS/PLAN_IMPLEMENTACION_13_CASOS.md`, y `ACHInterbank_ajustado/MEMORY_STATE.md` con historia íntegra conservada. Evidencias nuevas: `ACHInterbank_ajustado/docs/hotfix-pre-despliegue-2026-11-15/INFORME_PRE_DESPLIEGUE.md` y directorio adjunto `evidencias/`. **Siguiente actividad:** compilación, xUnit y pruebas de regresión técnica con SDK .NET 10.0.403; después continuar despliegue para pruebas, sin homologación ni UAT anticipada.

---

## Registro anterior íntegro (HC-CENIT-002, I13 y anteriores)

# Corrección puntual posterior a I13 — ROR CENIT: C5 normativo

**Fecha:** 2026-11-15 · **Línea base exclusiva:** `ACHInterbank_PROYECTO_COMPLETO_I13_HC-CENIT-013_2026-11-15.zip` · SHA-256 `d1331e86c4ee0063a86e1ad07b23b1b340efde842f6f179c91f7038919f239cd`. **No se abre un incremento I14.**

Se ha corregido `CenitReturnOfReturnService.ValidateOperationalWindowAsync`: ya no se calcula el último ciclo a partir de `Max()` en `AchCycles`, porque una jornada incompleta (C1–C4) convertía C4 en supuesto último ciclo. La regla intradía exige padre C2/C3/C4 y destino **C5**. Ambas rutas saliente y entrante usan el mismo validador. La regla del segundo ciclo del siguiente día hábil no se modificó. **No existe autorización operacional expresa incorporada en este paquete para sustituir C5**; sin ella se falla en cerrado.

`CenitReturnOfReturn2026Tests.cs` ahora configura C1–C5, actualiza casos positivos para C5, incluye negativos C4 con calendario completo e incompleto, cobertura entrante y matriz C1–C5. Se conservan las pruebas históricas de plazo siguiente día hábil, correlación y R74.

**Evidencia efectivamente ejecutada:** 20/20 suites previas con 542/542 verificaciones estáticas/documentales PASS, más 20/20 comprobaciones focales de fuente PASS. **NO EJECUTADO** .NET SDK, `dotnet restore/build/test`, SQLite/SQL Server/PostgreSQL, concurrentes, integración NACHA-M ni homologación oficial. Pruebas xUnit agregadas: cuatro métodos (ocho variantes), **0 ejecuciones**. Los controles estáticos no prueban compilación ni ejecución. El resultado normativo permanece **0/13 hallazgos formalmente cerrados**, UAT única final, producción **NO-GO**.

Fuente normativa disponible dentro del ZIP I13: `ACHInterbank_ajustado/docs/normativa/md/CENIT-DSP-152-Anexo-2.md` §3 y §§4.3/4.6 (último ciclo intradía y plazo alternativo siguiente día hábil). Se preservan los archivos de ACH Colombia, migraciones, dependencias, contratos, SDK/lockfiles y las correcciones I00–I13.

**Pruebas por ejecutar en ambiente .NET 10:** `dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj --filter FullyQualifiedName~CenitReturnOfReturn2026Tests` desde `ACHInterbank_ajustado/`, con informe TRX, SQLite y PostgreSQL/SQL Server según corresponda. Validar también respuestas reales de operador, fechas valor y correlación del archivo físico. **No declarar cierre formal hasta concluirlo.**

**Rollback:** recuperar exactamente las modificaciones focales desde el ZIP I13 (servicio, clase de pruebas, nueva prueba estática y documentos asociados); antes de despliegue, inspeccionar cualquier ROR histórico generado en C4 por configuraciones incompletas. No revertir movimientos financieros automáticamente.

---

# LEER PRIMERO — ACH Interbank / I13 / HC-CENIT-013

**Fecha de corte:** 2026-11-15 · **Plan:** PLAN-CENIT-13-V3.14-I13 · **Estado:** `IMPLEMENTADO_PENDIENTE_VERIFICACION_AMBIENTE` · **Siguiente fase:** G1–G3 técnicas, G4 UAT consolidada, G5 cierre (sin I14).

## Base exclusiva

`ACHInterbank_PROYECTO_COMPLETO_I12_HC-CENIT-012_2026-11-15.zip` · SHA-256 `3b47395c4eb58e984ff259e5b6d6357dc47eaaff071ead7acb74e8fd8f79fdf7`. Se verificó la integridad ZIP y únicamente se utilizó su árbol como código base. Todos sus archivos se conservan; el detalle de diferencias se encuentra en `EVIDENCIAS/I13/CAMBIOS_I13.json`.

## Corrección I13

El NACK por `TraceNo` ya actualizaba una operación, no el archivo entero. I13 agrega validación de la traza real del `AchTransaction` frente a su membresía NACHA-M, resuelve causales contradictorias por la misma traza con revisión sin mutación y valida el hash de origen tras conflicto concurrente de persistencia. Las otras trazas no se rechazan por propagación. La aceptación técnica del archivo no se equipara a aceptación individual. Sin cambio en ACH Colombia productivo, liquidación, ledger, dependencias, migraciones, interfaz ni formato NACHA.

## Evidencia y deuda

`EVIDENCIAS/I13/INFORME_I13.md` y logs: **542/542** comprobaciones estáticas/documentales, **8/8** comprobaciones Python del fixture XML sintético; no prueban runtime C#. **5 nuevos xUnit escritos, 0 ejecutados**, SDK .NET ausente. PT-14/PT-15 de concurrencia y SQL Server/PostgreSQL siguen PENDIENTES, al igual que las pruebas anteriores PT-01..23. `npm ci --offline` falla `ENOTCACHED`; avisos de seguridad actuales no verificables desde la caché.

**13/13 implementados, 0/13 normativamente cerrados. UAT única final; NO-GO producción.**
