# Evidencia de pruebas — Ejecución 2 NACHA-M

Fecha: 2026-07-16  
Alcance: ACH Colombia, layout MAN-004 V32 disponible localmente, motor table-driven, parser, controles y seguridad.  
Clasificación: evidencia técnica anonimizada; no certificación, no homologación y no aprobación LIVE.

## Controles de ejecución

- No se ejecutaron SOAP, MFT, SFTP, FTP, cargas a cámara, Playwright LIVE, migraciones, seed de ambientes externos ni contenedores.
- No se utilizaron archivos de `docs/referencias-reales` como fixture u oracle.
- Las entradas de prueba son sintéticas; los documentos/cuentas/Trace no se publican en esta evidencia.
- CENIT se verificó sólo como placeholder bloqueado para LIVE.
- Las suites con PostgreSQL, runtime SQL Server, paquete externo CENIT y evidencia productiva fueron excluidas expresamente.

## Artefactos sintéticos

| Artefacto | Propósito | Tamaño lógico | SHA-256 / control |
|---|---|---:|---|
| `tests/fixtures/nacha-m/ACHCOL/valid/achcol-v32-minimal.nacha.b64` | Golden ACHCOL válido, codificado en base64 para preservar 1060 bytes exactos | 10 registros × 106 bytes | `062A0B3242CC789823D67485D7DDFDB984EF97D6EBB3B254E47F5D67B49F3AF8` sobre bytes NACHA decodificados |
| `tests/fixtures/nacha-m/ACHCOL/invalid/account-overflow.case.json` | Overflow de campo financiero | N/A | Caso sintético; no contiene dato real |
| `tests/fixtures/nacha-m/ACHCOL/invalid/line-ending.case.json` | Rechazo de CR/LF | N/A | Caso sintético |
| `tests/fixtures/nacha-m/CENIT/invalid/not-homologated.case.json` | Rechazo CENIT LIVE | N/A | No declara layout CENIT válido |

El golden ACHCOL tiene un lote, una entrada, una adenda y cuatro fillers finales. No tiene BOM, CR ni LF. Sus montos son bajos y sintéticos; no se documentan valores individuales.

## Pruebas dirigidas concluidas

| Comando / filtro | Resultado | Cobertura principal |
|---|---|---|
| Suite offline amplia con exclusiones de infraestructura | **1.767 pasan, 1 omitida, 0 fallan, 1.768 total** | Regresión backend offline aplicable; sin PostgreSQL/SQL Server runtime/paquetes externos/evidencia productiva |
| Filtro combinado `OfficialNachaGenerationTableDrivenTests` + `NachaFunctionalValidationTests`, ejecutado después del build final | **118/118 pasan**, 0 fallan, 0 omitidas | Verificación final de reglas oficiales, parser, seguridad y funcionales sobre binario Release actualizado |
| `dotnet test ... --filter FullyQualifiedName~OfficialNachaGenerationTableDrivenTests` | **50/50 pasan**, 0 fallan, 0 omitidas | Descriptores, ejecución de reglas, T1/T5/T6/T7/T8/T9, lotes, T6/T7, overflow, parser, round-trip, privacidad y CENIT LIVE |
| `dotnet test ... --filter FullyQualifiedName~NachaFunctionalValidationTests` | **68/68 pasan**, 0 fallan, 0 omitidas | Golden sintético, controles físicos/funcionales, parser y escenarios de rechazo CENIT |
| Filtro conjunto de builder, seeder, administración, mapping, preproducción y transacciones | **94/94 pasan**, 0 fallan, 0 omitidas | Compatibilidad DEVELOPMENT, expectativas normativas actualizadas y no regresión de cálculos |
| Filtro generación ACHCOL + lotes + integridad | **9/9 pasan**, 0 fallan, 0 omitidas | Reinicio local, T5/T8 y salida física |

Las ejecuciones de 9 y 68 pruebas se solapan parcialmente con los grupos de 94 y 50; no se suman para declarar un total único de casos distintos.

## Reglas verificadas

| Área | Evidencia verificable | Resultado |
|---|---|---|
| T1 | 106; fecha 24–31; hora 32–35; id 36; 106/10/1; nombres; ReferenceCode blanco; reservado | Confirmado por prueba offline |
| T5 | fechas 8; settlement opcional en 80–82; status; entidad; lote 7; reservado | Confirmado para variante probada; settlement semántico pendiente |
| Lote | inicia 1 en cada archivo; múltiple lote; dos archivos; T5/T8; overflow | Confirmado por prueba offline |
| T6 | monto 18; ID 15; nombre 22; indicador 87; Trace 88–102; overflow/repertorio | Confirmado por prueba offline |
| T7 | variantes crédito/débito; secuencia; sufijo; asociación inmediata | Confirmado técnicamente; conflicto documental pendiente |
| T8/T9 | conteos/hash conservados; totales 18; lote; bloques/padding | Confirmado por prueba offline |
| Parser | bytes exactos; sin EOL/BOM/residuos; mismo descriptor; round-trip | Confirmado por prueba offline |
| Seguridad | error estructurado con RuleId; sin valor completo; trace no reconstruible | Confirmado en superficies modificadas |
| CENIT | LIVE bloqueado; ACHCOL no bloqueado por ese gate | Confirmado por prueba offline |

## Build y restore

- `dotnet restore`: completado; proyectos actualizados.
- `dotnet build ACHInterbank.sln -c Release --no-restore`: verificación final completada con 0 warnings y 0 errores.
- Las pruebas dirigidas recompilaron el proyecto de tests en Release después de los últimos cambios de código/prueba.
- No se ejecutó ninguna migración aunque el proyecto de migraciones se compiló como parte de la solución.

## Suite offline amplia

Filtro utilizado:

```text
Category!=Postgres
FullyQualifiedName!~CenitProcTransaccionesPackageCharacterizationTests
FullyQualifiedName!~ProcTransaccionesProductionEvidenceTests
FullyQualifiedName!~SqlServerBootstrapRuntimeTests
FullyQualifiedName!~SqlServerDataProtectionRuntimeTests
```

Resultado final: **1.767 pasan, 1 omitida, 0 fallan, 1.768 total**, duración reportada 5 min 39 s. La prueba omitida es un diagnóstico arquitectónico SOAP marcado explícitamente `SKIP`; no se deshabilitó durante esta ejecución.

Antes del resultado final, un intento agotó 120 segundos y otro 360 segundos. El segundo dejó el runner hijo activo pese al timeout del shell; se verificó su línea de comando, se confirmó que correspondía al mismo filtro local y se detuvieron únicamente esos procesos de prueba. La tercera ejecución usó margen suficiente, conservó el mismo filtro y concluyó en verde. No quedaron servicios ni procesos LIVE iniciados.

## Pruebas no ejecutadas

| Grupo | Motivo |
|---|---|
| PostgreSQL y SQL Server runtime | Requieren infraestructura/persistencia; expresamente fuera de alcance hasta Ejecución 3/4 |
| Migraciones | Prohibidas en esta ejecución |
| SOAP, Proc_Transacciones, MFT/SFTP/FTP | Servicios LIVE/transmisión prohibidos |
| CENIT package externo / evidencia productiva | Requieren material o rutas externas; CENIT no homologado |
| Playwright LIVE | Transmisión/UI LIVE fuera de alcance |
| Homologación ACH Colombia | Sólo puede producirla la cámara/entorno autorizado y aprobación humana |

## Interpretación

Un build verde y las pruebas sintéticas reducen el riesgo de regresión, pero no demuestran cumplimiento productivo. Persisten reglas no demostradas y controles externos; por ello ACHCOL continúa NO-GO y CENIT continúa NO-GO / NOT HOMOLOGATED / BLOCKED FOR LIVE.
