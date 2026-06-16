# NACHA-M functional golden files

Estos golden files son snapshots funcionales semirreales, anonimizados y versionados para pruebas automatizadas. No contienen datos productivos y no deben interpretarse como archivos certificados oficialmente por ACH Colombia o CENIT. Su proposito es proteger la estabilidad del motor table-driven, la generacion fixed-width, los totales Batch/File, el padding, el naming y el parseo funcional.

## Estructura

Los snapshots fisicos viven en `GoldenFiles`:

- `ACHColombia/Outgoing/ACH_COL_OUT_001.ach`
- `ACHColombia/Incoming/ACH_COL_IN_001.ach`
- `ACHColombia/Returns/ACH_COL_RET_001.RET`
- `CENIT/Outgoing/CENIT_OUT_001.ach`
- `CENIT/Incoming/CENIT_IN_001.ach`
- `CENIT/Returns/CENIT_RET_001.RET`

Los nombres usan una convencion funcional legible: camara, flujo y numero de escenario. El nombre oficial NACHA-M sigue el patron `RRRRTTT.ZZZ.N` y los archivos `.RET` conservan extension `.RET` para validar el flujo de devoluciones. Los `.ach` son solo fixtures fisicos internos.

## Reglas de los snapshots

Cada archivo es byte-stable: con los datos deterministas del test, el contenido esperado debe ser identico entre ejecuciones. Si cambia un byte, la suite reporta la primera diferencia con linea, posicion, record type, contexto y ruta del snapshot.

Los snapshots deben cumplir longitud oficial de 106 caracteres por registro, record types validos, FileControl antes del padding, padding solo al final con registros de `9`, y cantidad total de registros multiplo del blocking factor 10.

## Datos

Los datos son ficticios y anonimizados. No se deben versionar nombres de clientes reales, documentos reales, cuentas reales, correos, telefonos, referencias productivas ni textos como `PRODUCCION`, `CLIENTE REAL`, `REAL CLIENT` o `DATOS REALES`.

## Actualizacion intencional

Si un cambio funcional esperado modifica la salida NACHA-M:

1. Revisar el diff del test que falla y confirmar que el cambio es intencional.
2. Regenerar solo el snapshot afectado.
3. Verificar fixed-width, padding, totales y sensibilidad.
4. Ejecutar `dotnet build ACHInterbank.sln -c Release`.
5. Ejecutar `dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release`.
6. Documentar en el commit por que cambio el snapshot.

## Cobertura

La suite cubre salidas ACH Colombia y CENIT generadas por el builder table-driven, entradas NACHA-M semirreales, devoluciones `.RET`, comparacion byte a byte, comparacion con line endings normalizados, integridad fixed-width, padding, control totals y proteccion basica contra datos sensibles.

## Limites

Estos golden files no reemplazan certificacion oficial con ACH Colombia o CENIT. Productivo permanece NO-GO.
