# Evidencia y cierre técnico parcial — HOTFIX FINAL predespliegue ACH Interbank

**Corte:** 2026-11-15. **Línea base única:** `ACHInterbank_PROYECTO_COMPLETO_I13_PRE_DESPLIEGUE_3_HOTFIXES_2026-11-15.zip` (SHA-256 `2f37ef69b7f97d2c73db2ba872aa4205ce70c916a5f0f82f153e91f476485242`). **Alcance:** únicamente dos pendientes ACH Colombia. Se preservan los 13 correctivos CENIT, los tres hotfixes anteriores y HC-CENIT-002/C5/fecha ROR.

## HOTFIX FINAL 01 — D01 / D02 / D04, límites técnicos y por operador

**Anterior:** `ValidateAchColombiaEffectiveDate` omitía fecha nula/vacía y aceptaba como válido el estado sin fecha operativa, por tanto podía ignorar D02 o no evaluar D01. `ParseAndSaveDetailedAsync` convertía un fallo de fecha de lote en excepción de archivo y no propagaba la causal V35 por cada entrada del lote. El control de futuro contra fecha de proceso tampoco impedía procesarlo.

**Posterior:** `NachaInboundValidator.GetAchColombiaEffectiveDateFailure` evalúa fecha efectiva `null`, vacía, fecha imposible, anterior, actual y posterior. Los códigos V35 del Anexo 3 son **D01 (efectiva anterior), D02 (fecha no válida/ajena al día de proceso) y D04 (prenotificación distinta de cero)**. Cuando falta la fecha operativa *oficial* del ciclo, la validación queda en fallo cerrado **técnico** `ACHCOL_OPERATIONAL_DATE_UNRESOLVED`: nunca supone ni inventa un D01 sin comparación demostrable. Esta decisión preserva el significado normativo y corrige la omisión de control. `NachaParserService.ParseAndSaveDetailedAsync` registra el D01/D02 por **entrada T6** del lote afectado mientras sigue comprobando los conteos T8/T9, bloque físico y relleno; el resto de lotes válidos conserva su tratamiento. CENIT conserva su catálogo independiente; sin cambios en ROR.

**Métodos:** `GetAchColombiaEffectiveDateFailure`, `ValidateAchColombiaEffectiveDate` (`NachaInboundValidation.cs`); `ParseAndSaveDetailedAsync` (`NachaParserService.cs`). **Pruebas xUnit escritas:** siete variantes de fechas (incluidas null, espacios, pasado y futuro), tres variantes sin fecha operativa, un positivo de fecha correcta y aislamiento CENIT; cuatro variantes físicas NACHA T5 para D01/D02, un físico T6 para D04, un físico T1 sin ciclo operativo; control previo del archivo válido. Se ajustaron tres aserciones heredadas de error físico incorrectamente rotulado y la de alcance de catálogo en `CenitI11StaNachaRejectionDomainsTests.cs`. **No se han ejecutado estas pruebas .NET**.

**Riesgo:** el archivo físico real del operador, su catálogo de causas por transacción y el contrato de `IncomingNachaAdmissionValidator` deben verificarse en integración, pues la admisión de un archivo puede rechazarlo antes de llegar a este parser. El fail-closed por fecha operativa ausente exige resolver/parametrizar el ciclo, no inferirlo del encabezado físico.

## HOTFIX FINAL 02 — R10 C1–C7 y calendario bancario

**Anterior:** `ValidateAchColombiaR10Async` prohibía toda transición entre días operativos y aplicaba un límite genérico `MaxCycles=4` a los siete ciclos, permitiendo/negando pares distintos de los especificados en la tabla vigente. La corrección anterior del inicio físico de C1 (19:01 del día calendario anterior) ya existía y se conserva.

**Posterior:** `RegulatoryCycleScheduleCatalog.AllowsAchColombiaSevenCycleReturn` expone la **matriz normativa existente**, sin copiarla o modificarla. Origen/devolución debe ser día hábil validado con `IOperationalCalendarService`; el origen puede ser mismo día operativo o **día hábil inmediatamente anterior** calculado por `GetPreviousBusinessDayAsync`, pasando sobre fines de semana y festivos. Para política vigente desde el 14-10-2026 la matriz discrimina expresamente `CurrentOperationalDay` y `PreviousOperationalDay` para cada par C1–C7; el destino debe autorizar `AllowsReturn`. Un ciclo no resoluble, fecha no hábil, cámara distinta o par prohibido falla cerrado. La política histórica previa a la activación de siete ciclos mantiene su cómputo de oportunidades y `MaxCycles` original; no se reescribe retroactivamente. El número de ciclo C1 se asocia siempre a su **fecha operativa de procesamiento**, no a la fecha civil de apertura.

**Matriz C1–C7 (retorno -> orígenes admitidos):** C1 anterior 4/5/6/7; C2 anterior 5/6/7 y actual 1; C3 anterior 6/7 y actual 1/2; C4 anterior 7 y actual 1/2/3; C5 actual 1/2/3/4; C6 actual 2/3/4/5; C7 actual 3/4/5/6. Fuente: documento suministrado *Nuevos Ciclos de Proceso y Horarios*, tablas de actividades y transacciones para enviar C1–C7, y configuración inmutable preexistente.

**Métodos:** `ValidateAchColombiaR10Async` y `ResolveOperationalCycleStart` preservado (`AchReturnEligibilityPolicyComponent.cs`); `AllowsAchColombiaSevenCycleReturn` añadido (`RegulatoryCycleScheduleCatalog.cs`). **Pruebas xUnit escritas:** 98 pares de matriz (49 por día actual y 49 por día previo); ocho escenarios integrados con lunes festivo, sábado origen, domingo destino, origen hábil anterior, mismo día, hueco de un día hábil intermedio y pares inválidos; pruebas R10 existentes actualizadas para devolver desde un ciclo de origen diferente del ciclo de retorno (uso explícito de `ReturnCycleId`), mantener límites históricos y fechas fijas hábiles. **Sin ejecución .NET**.

**Riesgo:** contrastar política con configuraciones reales vigentes y validar generación física ReturnOut en SQL Server/PostgreSQL y ambientes del operador. La simulación de la matriz con controles textuales **no reemplaza** ejecución C#.

## Comprobaciones ejecutadas de verdad

- 41/41 controles **estáticos** focales `scripts/test/predeploy_three_hotfixes_static_check.py` PASS (no ejecución de .NET).
- 12/12 scripts estáticos de regresión CENIT I01–I13, **391/391 comprobaciones de fuente**, PASS.
- `cenit_post_i13_ror_c5_static_check.py` **20/20 controles estáticos**, PASS; regla C5 intradía y calendario ROR preservados.
- Verificación de proyectos `.sln` y referencias `ProjectReference`: **7 proyectos declarados, 0 ausentes; 0 referencias rotas**, según `evidencias/grafo_proyectos.log`.
- SHA-256 y comparación de contenido contra fuente única registrados en `evidencias/cambios_fuente_vs_zip.json`; no se suprimió ningún elemento original.

**NO EJECUTADO**: `dotnet --info/restore/build/test` (SDK requerido 10.0.403 y compiladores no instalados), xUnit, integración real NACHA-M, base de datos, transacciones concurrentes, homologación de cámara, UAT. Ausencia de ejecución **no significa PASS**. Nuevas dependencias NuGet: ninguna; proyectos, paquetes y migraciones originales sin cambios.

**Estado:** hotfix final 01 y 02 **IMPLEMENTADOS / VERIFICACIÓN DINÁMICA PENDIENTE**. Los 13 correctivos históricos permanecen implementados y pendientes de cierre formal (0/13 cerrados). **NO-GO producción**, UAT reservada para fase final. **Próxima actividad:** G1 restore + compilación + xUnit dirigido, G2 regresión integral y proveedores DB, G3 despliegue técnico exclusivo para pruebas, luego G4 UAT consolidada al final.

**Comandos para ambiente con .NET 10.0.403** (desde `ACHInterbank_ajustado/`):

```bash
dotnet restore ACHInterbank.sln
dotnet build ACHInterbank.sln -c Release
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --filter "FullyQualifiedName~NachaInboundValidationSeparationTests|FullyQualifiedName~AchTransactionNachaTests|FullyQualifiedName~AchReturnsFileByClearingHouseTests|FullyQualifiedName~CenitReturnOfReturn2026Tests"
```

La evidencia estática está en `evidencias/regresion_estatica_ejecutada.json`, sus logs individuales, `entorno_compilacion.log`, `grafo_proyectos.log` y `cambios_fuente_vs_zip.json`. Estas últimas evidencias de integridad no son certificaciones regulatorias.
