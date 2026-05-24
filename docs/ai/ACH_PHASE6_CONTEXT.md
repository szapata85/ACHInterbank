# ACHInterbank - Contexto Permanente IA - Fase 6 NACHA-M

## 1. Proposito del documento

Este archivo contiene el contexto permanente para que asistentes de IA como Codex, Cursor o Claude Code trabajen sobre la Fase 6 del proyecto ACHInterbank sin requerir prompts largos repetidos en cada tarea.

Cualquier tarea futura relacionada con NACHA-M, ACH Colombia, CENIT, procesamiento entrante, golden files, totales, trazabilidad o integracion SOAP debe leer este archivo antes de modificar codigo.

## 2. Estado actual de Fase 6

- Fase 6A: COMPLETADA.
- Fase 6B.1: COMPLETADA.
- Fase 6B.2: COMPLETADA.
- Fase 6B.3A: COMPLETADA.
- Fase 6B.3B: COMPLETADA.
- Fase 6B.3C: COMPLETADA a nivel tecnico automatizado con golden files semirreales.
- Fase 6B.3C.1: COMPLETADA.
- Fase 6B.4: COMPLETADA para flujo interno end-to-end automatizado.
- Siguiente fase: Fase 6B.5 - Integracion SOAP operativa controlada.
- Productivo: NO-GO.

## 3. Commits cerrados conocidos

### Fase 6B.3B

Commit:
`fbd33a281577a4dcaa3095f810a88fc7e265313b`

Resumen:
Se implemento `INachaControlTotalsCalculator` / `NachaControlTotalsCalculator`, `EntryHash`, `BlockCount`, `FileIdModifier` MAN-004 V32, `EntryAddendaCount`, `TotalDebitAmountInCents`, `TotalCreditAmountInCents`, totales Batch/File, padding con records de 9, validacion calculado vs renderizado y trace/auditoria con `Phase=6B.3B`.

Resultado:
- Build Release OK.
- Tests Release OK: 1242 passed, 0 failed, 1 skipped, total 1243.
- Productivo NO-GO.

### Fase 6B.3C parcial

Commit:
`2e8ab8432e0e9d64f5308c275133cca891e7e025`

Resumen:
Se implemento la base de la suite funcional NACHA-M: `NachaFunctionalValidationTests`, `NachaGoldenFileComparer`, `NachaFixedWidthAssertions`, `NachaFunctionalModels`, `NachaFunctionalTraceAssertions` y metadata de fixtures.

Resultado:
- Build Release OK.
- Tests Release OK: 1279 passed, 0 failed, 1 skipped, total 1280.
- Riesgo pendiente en ese momento: faltaban snapshots fisicos `.ach` / `.RET`.

### Fase 6B.3C.1

Commit:
`3b3fd60a44c4b4e7fc0d7161e1cb88845b930c14`

Resumen:
Se materializaron golden files fisicos byte-stable `.ach` y `.RET` bajo `TestData/Nacha/GoldenFiles` para ACH Colombia y CENIT.

Golden files agregados:
- `ACHColombia/Outgoing/ACH_COL_OUT_001.ach`
- `ACHColombia/Incoming/ACH_COL_IN_001.ach`
- `ACHColombia/Returns/ACH_COL_RET_001.RET`
- `CENIT/Outgoing/CENIT_OUT_001.ach`
- `CENIT/Incoming/CENIT_IN_001.ach`
- `CENIT/Returns/CENIT_RET_001.RET`

Todos pesan 1060 bytes:
- 10 registros fixed-width.
- 106 caracteres por registro.

Resultado:
- Build Release OK.
- Tests Release OK: 1310 passed, 0 failed, 1 skipped, total 1311.
- Productivo NO-GO.
- Los golden files son semirreales y no reemplazan certificacion oficial con ACH Colombia/CENIT.

### Fase 6B.4

Commit:
`4406395dbd4e1922917672122fd34d4810a98550`

Resumen:
Se implemento el flujo interno end-to-end automatizado de procesamiento entrante NACHA-M.

Archivos nuevos principales:
- `src/Cfa.ACHInterbank.Application/ACH/Interfaces/INachaIncomingFileProcessor.cs`
- `src/Cfa.ACHInterbank.Application/ACH/Models/NachaIncomingFileProcessingModels.cs`
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaIncomingFileProcessor.cs`
- `tests/Cfa.ACHInterbank.Tests/NachaIncomingEndToEndProcessingTests.cs`

Servicios/modelos agregados:
- `INachaIncomingFileProcessor`
- `NachaIncomingFileProcessor`
- `NachaIncomingFileRequest`
- `NachaIncomingFileProcessingResult`
- `NachaIncomingDecision`
- `NachaIncomingFlowType`
- `NachaIncomingDecisionType`
- `NachaSoapOperationCandidate`

Resultado:
- Build Release OK.
- Tests Release OK: 1325 passed, 0 failed, 1 skipped, total 1326.
- No se toco motor table-driven.
- No se modificaron golden files.
- No hubo migraciones.
- No se invoco SOAP real.
- Productivo NO-GO.

## 4. Decision arquitectonica oficial

Opcion C: usar `nacha-config profiles` como modelo oficial.

Implicaciones:
- Separar parametrizacion por camara ACH Colombia y CENIT.
- Eliminar dependencia funcional de layouts/definitions legacy.
- Hacer que `NachaFileBuilder` genere desde perfiles publicados/vigentes.
- Fallar controladamente si falta parametrizacion.
- Mantener enfoque table-driven.
- Preparar la SPA para administrar perfiles NACHA-M por camara en fases posteriores.
- No volver a logica hardcoded si el perfil puede resolver la regla.

## 5. Reglas NACHA-M vigentes

### Naming ACH Colombia MAN-004 V32

Formato:

```text
RRRRTTT.ZZZ.1
```

Donde:
- RRRR = codigo de ruta de entidad originadora.
- TTT = codigo de transito.
- ZZZ = consecutivo diario 001-036.

Archivos de devolucion:
- Usan extension `.RET`.

### FileIdModifier

Regla:
- 001-026 => A-Z.
- 027-036 => 0-9.
- Fuera de 001-036 debe fallar controladamente.

### Totales NACHA-M

Los totales Batch/File deben incluir:
- `EntryAddendaCount`.
- `EntryHash`.
- `TotalDebitAmountInCents`.
- `TotalCreditAmountInCents`.
- `BatchCount`.
- `BlockCount`.
- `PaddingRecordCount`.
- Conteos fisicos antes/despues de padding.

### Padding

Regla:
- Padding final con records de 9.
- Alineacion segun `BLOCKINGFACTOR` oficial.
- Si falta `BLOCKINGFACTOR`, fallback controlado al estandar 10.
- No se permite padding intermedio.

### Fixed-width

Regla actual usada en golden files:
- 106 caracteres por registro.
- 10 registros para snapshots actuales.
- 1060 bytes por archivo golden actual.

## 6. Golden files fisicos

Ruta:

```text
tests/Cfa.ACHInterbank.Tests/TestData/Nacha/GoldenFiles
```

Archivos:
- `ACHColombia/Outgoing/ACH_COL_OUT_001.ach`
- `ACHColombia/Incoming/ACH_COL_IN_001.ach`
- `ACHColombia/Returns/ACH_COL_RET_001.RET`
- `CENIT/Outgoing/CENIT_OUT_001.ach`
- `CENIT/Incoming/CENIT_IN_001.ach`
- `CENIT/Returns/CENIT_RET_001.RET`

Reglas:
- Son semirreales.
- Estan anonimizados.
- Son byte-stable.
- Se usan para regresion automatizada.
- No contienen datos productivos.
- No sustituyen certificacion oficial con ACH Colombia/CENIT.
- No deben modificarse sin intencion explicita y actualizacion de pruebas.

Helpers relacionados:
- `NachaGoldenFileComparer`.
- `NachaFixedWidthAssertions`.
- `NachaTestDataPaths`.
- `NachaFixtureSensitivityAssertions`.
- `NachaFunctionalTraceAssertions`.
- `NachaFunctionalModels`.

## 7. Reglas SOAP para fases posteriores

La Fase 6B.4 solo dejo candidatos SOAP, no ejecucion real.

Interpretacion funcional:

### Proc_Contrapartidas

- Mueve debitos monetarios de una transaccion originada por CFA.
- Debe usarse solo cuando la decision funcional indique movimiento monetario tipo debito originado por CFA.

### Proc_Transacciones

- Mueve creditos monetarios de una transaccion originada por otra entidad financiera.
- Debe usarse solo cuando la decision funcional indique credito monetario originado externamente hacia CFA.

### RegistrarRespuestaTransaccion

- Solo registra notificaciones/respuestas diferenciales.
- No debe hacer movimientos monetarios.
- Aplica para respuestas diferenciales, rechazos, devoluciones o notificaciones que no mueven dinero.

### Reglas criticas

- Respuestas diferenciales no mueven dinero.
- Archivos `.RET` no mueven dinero directamente.
- Prenotificaciones aprobadas/rechazadas no mueven dinero.
- Si hay ambiguedad, la decision debe ser `ManualReviewRequired`.
- La integracion SOAP real debe hacerse en una fase controlada posterior.
- No invocar SOAP real desde tests automatizados.
- Usar mocks, dry-run o gateway controlado.

## 8. Estado productivo

Productivo permanece NO-GO.

Razones:
- Los golden files son semirreales.
- Falta certificacion oficial con ACH Colombia/CENIT.
- Falta integracion SOAP real controlada.
- Falta UAT funcional.
- Falta aprobacion operativa/tecnica.
- Falta plan de rollback y monitoreo productivo.

Ninguna fase debe cambiar Productivo a GO sin instruccion explicita y validacion formal.

## 9. Comandos estandar de build/test

```powershell
dotnet build ACHInterbank.sln -c Release
```

```powershell
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release
```

Para segunda ejecucion rapida cuando ya existe build valido:

```powershell
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --no-build
```

Criterio esperado:
- Build succeeded.
- 0 warnings.
- 0 errors.
- Tests passing.
- Mantener o aumentar cobertura.
- No reducir pruebas sin justificacion.

## 10. Convenciones de tests

- Usar golden files fisicos existentes cuando aplique.
- No usar datos productivos reales.
- No modificar golden files salvo cambio intencional.
- Tests deben ser deterministicos.
- Evitar `DateTime.Now`, `Guid.NewGuid` o valores aleatorios sin control.
- Si hay campos variables, fijarlos o normalizarlos.
- Usar mocks para SOAP.
- No invocar servicios externos reales.
- Verificar `Phase` en trace segun fase:
  - `6B.3B` para totales.
  - `6B.4` para procesamiento entrante.
  - `6B.5` para integracion SOAP controlada.
- Validar `ProductiveExecution=false` en flujos simulados o no productivos.
- Validar que respuestas diferenciales, `.RET` y prenotificaciones no generen movimiento monetario.
- Preferir pruebas pequenas y focalizadas sobre pruebas enormes.
- Mantener nombres de tests descriptivos.

## 11. Instrucciones para futuras tareas con IA

Antes de implementar cualquier fase futura:
1. Leer este archivo.
2. Inspeccionar el estado actual del repo.
3. Revisar `git status`.
4. No asumir que el working tree esta limpio.
5. No reescribir arquitectura existente.
6. No tocar produccion.
7. No introducir datos sensibles.
8. No generar migraciones salvo necesidad clara.
9. No ejecutar SOAP real.
10. Mantener Productivo NO-GO.
11. Entregar resumen final con:
    - Archivos modificados.
    - Archivos nuevos.
    - Tests agregados/modificados.
    - Comandos ejecutados.
    - Resultado de build.
    - Resultado de tests.
    - Riesgos pendientes.
    - Estado productivo.
