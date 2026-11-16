# Informe de corrección focal — contadores de T6 NACHA-M, 2026-11-15

**Origen exclusivo:** `ACHInterbank_PROYECTO_COMPLETO_I13_HOTFIX_ACHCOL_FRONTERA_ADMISION_D01_D02_2026-11-15.zip` (SHA-256 `77dbc0ec87151836e054c7c4df9eac2f99734c1927eab7bb9ca9b9e491ef62a2`).
**Estado:** `IMPLEMENTADO_PENDIENTE_COMPILACION_XUNIT_DB`. No abre I14. No modifica homologación, UAT ni condiciones de cierre de los trece correctivos CENIT.

## Hallazgo reproducible en fuente

- Antes, `NachaParserService.ParseAndSaveDetailedAsync` incrementaba `totalEntries` solamente después de la validación inicial positiva, por lo que una entrada T6 rechazada por D01/D02 no aparecía en `NachaParseResult.TotalEntries`.
- `IncomingNachaIngestionAppService.IngestAsync` restaba además `ErrorCount` de ese subtotal y reportaba como inválidas tantas **fallas** como errores, aunque varias fallas pueden afectar una misma T6.
- El filtrado final `EnforceAddendaRequirements` puede rechazar una T6 previamente admitida; el conteo antes de ese paso no refleja el número final de transacciones aceptadas. Una aserción anterior sobre una fecha inválida presuponía el significado contrario de `TotalEntries`.

## Contrato corregido y criterio de aceptación

| Campo | Semántica después del hotfix |
|---|---|
| `NachaParseResult.TotalEntries` | Número de registros físicos T6 correctamente leídos, independientemente de su aceptación transaccional |
| `NachaParseResult.ValidEntries` | Número final de entradas aceptadas después de las validaciones funcionales y de adendas |
| `NachaParseResult.InvalidEntries` | `TotalEntries - ValidEntries`, número de T6 no aceptadas |
| `NachaParseResult.ErrorCount` | Número de hallazgos de validación; puede ser diferente de `InvalidEntries` |
| Persistencia `IncomingNachaFileProcessingResult` | `TotalEntries`, `ValidCount`, `InvalidCount` reciben directamente los tres primeros valores, sin restar `ErrorCount` |

**Invariante:** `TotalEntries == ValidEntries + InvalidEntries`, con contadores no negativos. Cuando el servicio de ingesta recibe un resultado inconsistente, falla cerrado (`NACHA_ENTRY_COUNTS_INCONSISTENT`) y no ejecuta posprocesamiento. Ejemplo: 2 T6 físicas, una aceptada y otra rechazada D01 ⇒ total 2, válidas 1, inválidas 1, sin importar cuántos mensajes de validación tenga el rechazo.

## Archivos/métodos modificados

1. `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaParserService.cs` — `ParseAndSaveDetailedAsync`: conteo de T6 antes de reglas funcionales; cierre después de adendas.
2. `src/Cfa.ACHInterbank.Application/ACH/Models/IncomingNachaIngestionDtos.cs` — `NachaParseResult.ValidEntries/InvalidEntries`.
3. `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/IncomingNachaIngestionAppService.cs` — `IngestAsync`: mapeo y control de consistencia.
4. `tests/Cfa.ACHInterbank.Tests/NachaFunctionalValidationTests.cs` — caso físico de dos lotes con D01 (2/1/1), validación tardía de adenda y aserción de T6 rechazada.
5. `tests/Cfa.ACHInterbank.Tests/AchTransactionNachaTests.cs` — aclara y verifica `TotalEntries=1, ValidEntries=0, InvalidEntries=1` en archivo físico D01/D02.
6. `tests/Cfa.ACHInterbank.Tests/IncomingNachaIngestionAppServiceTests.cs` — escenarios de ingesta 2/1/1 con 1 o 2 fallas, guardia para resultado incoherente y actualización de fixture mock.
7. `tests/Cfa.ACHInterbank.Tests/CenitOrdinaryInbound2026ParserTests.cs` — adapta únicamente las aserciones previas de T6 rechazadas PPD/CCD al contrato físico/final; sin modificar reglas CENIT.
8. `scripts/test/achcol_nacha_transaction_counts_static_check.py` — comprobador estático específico, no sustituye ejecución del código C#.

## Ejecución y evidencia verificable

- **Ejecutado:** `python scripts/test/achcol_nacha_transaction_counts_static_check.py` → 28/28 verificaciones de texto fuente, sin errores.
- **Ejecutado:** 15/15 scripts de regresión estática disponibles (12 CENIT históricos, HC-CENIT-002, frontera ACHCOL y hotfixes predespliegue) → exit code 0, ver `evidencias/regresion_scripts.json`.
- **Comprobación de integridad de proyecto/ZIP:** consultar `evidencias/integridad_paquete.json` una vez empaquetado.
- **No ejecutado:** `dotnet restore`, `dotnet build`, `dotnet test`, xUnit, pruebas SQLite/SQL Server/PostgreSQL y pruebas contra operador. SDK .NET/compilador C# no disponibles en el entorno; **no considerar aprobada ninguna prueba xUnit**. **UAT no ejecutada.**

## Riesgos residuales, despliegue y rollback

- Falta validar compilación y el efecto funcional del conteo en el posprocesador y las vistas/reportes durante la integración técnica. Las verificaciones de fuente no demuestran el comportamiento runtime.
- Debe confirmarse el caso con múltiples lotes, fecha efectiva distinta, fallas múltiples sobre una T6, rechazo por adenda y replay/duplicados usando SQL y archivos reales.
- No cambian las causales D01/D02/D04, validaciones físicas fatales, reglas ACH R10, CENIT ROR ni el calendario. Tampoco hay migraciones, nuevas dependencias, modificaciones de NuGet o cambios UI.
- Para revertir este hotfix, restaurar solamente los siete archivos C# afectados desde el ZIP base conservando todos los demás correctivos; investigar cualquier reporte de conteos previo al hotfix antes de usarlo para conciliación.

**Próxima actividad:** compilar en Visual Studio/.NET 10, ejecutar las pruebas focales y de regresión completas y luego pruebas técnicas de ingesta/consolidación. No UAT hasta la fase final; no homologado ni aprobado para producción.
