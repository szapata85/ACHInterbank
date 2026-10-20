# Informe comparativo técnico, normativo y de seguridad NACHA-M

## 1. Alcance y decisión ejecutiva

Se auditó la generación NACHA-M de salida, el parser asociado, el metamodelo table-driven, perfiles ACH Colombia/CENIT, controles, naming, secuencias, persistencia, validación, trazabilidad, pruebas y tres archivos reales controlados. La ejecución fue exclusivamente diagnóstica: no se modificó código, pruebas, configuración, migraciones, bases de datos ni archivos reales; no se ejecutaron flujos financieros ni integraciones externas.

Decisión preliminar:

- **ACH Colombia: NO-GO.** El archivo de ACHInterbank es físicamente estable, pero las posiciones/longitudes críticas contradicen el MAN-004 V32 disponible, y existen brechas de lote, T6/T7 y privacidad.
- **CENIT: NO-GO.** El perfil publicado se declara placeholder y no existe en el repositorio la ficha técnica NACHA-M/Manual STA que demuestre las reglas críticas. La semejanza del tercero con ACHCOL no permite aprobar CENIT.

No se afirma que el aplicativo elimine el riesgo de sanciones. El objetivo del plan es reducir riesgo mediante reglas documentadas, trazabilidad, perfiles independientes, validación cerrada, pruebas reproducibles, controles de seguridad y aprobación humana antes de LIVE.

## 2. Archivos analizados

| Archivo lógico | Identificador físico | SHA-256 | Tamaño / estructura |
|---|---|---|---|
| Generado por ACHInterbank | `0001283.001.20260714.23` | `8E18D416227CAE8321328D1E1D9243C28E05D2DDBE8FB3292C2E49C8A1C1FACA` | 71.020 bytes; 670×106; 67 bloques |
| Tercero ACH Colombia | `0001283.001.20250331.1.OUT` | `F090B5D4BFAB75FE04CD19313EA1ED467D0205F0FC603DE255CF9688C4753518` | 44.520 bytes; 420×106; 42 bloques |
| Tercero CENIT | `0001283.002.20250331.1` | `3566E425E7786B841482612C6EBC507ECD4E41996A1B8391D1CF5BE7F29468BE` | 2.120 bytes; 20×106; 2 bloques |

Los tres carecen de BOM, CR, LF y tabs; no tienen residuo al dividir por 106 ni datos posteriores al último bloque. No se reprodujo el contenido completo de ningún registro.

## 3. Documentación utilizada

| Documento | Emisor | Versión / fecha | SHA-256 PDF | Capítulos usados | Aplicación / duda |
|---|---|---|---|---|---|
| Manual de Servicio ACH Transferencias Interbancarias, DDS-DIS-MAN-004 | ACH Colombia | V32, enero 2025 | `D83585B53B31A3A70E4861412F48FF8306ED0D2F439A7443C05E540F6B5736EE` | 6.1.2–6.1.4, 6.1.10.1/3, fichas T1/T5/T6/T7/T8/T9 | Fuente principal ACHCOL; confirmar vigencia/alcance CFA y conflicto de rótulo interno V31 |
| Anexo 2 Manual Operativo CENIT | Banco de la República | Fecha visible 27-02-2025; versión no demostrada | `AD6BB2FC48CCF78CE0BDB980BBFFFCAF9D42E52882CC16559A9336F41CFC902D` | Ciclos, canales, NACHAM, STA | No contiene layout ni naming NACHA-M |
| CENIT Anexo A | Banco de la República | No demostrada | `D3A8F12EC49876CBFF516DAA3A1651693C5DAE0D75DC2DF8FFE733C1A8A00EFE` | Causales | No define formato físico |
| CENIT Anexo B | Banco de la República | No demostrada | `ADF05ED85BF8EF136C61A1560D073EB6B375D50571530DC679164AF78C2A530A` | Rechazos D03/D04/D05 | Cita Manual STA ausente |
| Contexto Fase 6 y documentos Opción C | Proyecto CFA | 2026 | N/A | Dirección arquitectónica | Fuente interna, subordinada a normativa |

No se encontraron el Manual de Especificaciones NACHA-M CENIT ni el Manual de Especificaciones STA citado por el Anexo 2. Esa ausencia es bloqueante.

## 4. Arquitectura encontrada

La solución conserva límites de Clean Architecture:

- Application define interfaces y modelos (`INachaFileBuilder`, `INachaParserService`, resolver, calculator y contratos de naming).
- Domain contiene transacciones/lotes/ciclos y el metamodelo `CfgProfile`, `CfgProfileRecord`, `CfgLayoutVariant`, `CfgLayoutField`, `CfgFieldSourceDefinition`, `CfgFieldRule` y `CfgRuleSet`.
- Persistence implementa builder, parser, renderers, validadores, perfiles seed, secuencias y EF para SQL Server/PostgreSQL.
- Api compone endpoints y middleware.

La ruta oficial resuelve un perfil publicado por cámara/flujo/dirección/vigencia, exige records 1/5/6/7/8/9 y no hace fallback legacy dentro de `BuildOfficialTableDrivenFileAsync`. Los perfiles ACH y CENIT son entidades distintas y los cálculos críticos permanecen en código, alineados con la decisión de arquitectura Opción C.

Sin embargo:

- `NachaFileBuilder` concentra 3.387 líneas, switches, aliases, defaults y formateos.
- El parser no usa perfiles y realiza numerosos `Substring` con offsets fijos.
- El renderer legacy y ramas iniciales truncan con `Substring(0, Length)`.
- `appsettings.Development.json` usa TABLE_DRIVEN, pero `appsettings.json` usa HYBRID.
- `NachaConfigValidationService` codifica como “esperadas” las longitudes erróneas del seeder, no valida posiciones exactas y no cubre íntegramente T6/T7.
- El renderer oficial no ejecuta las `CfgFieldRule` habilitadas; el modelo no guarda fuente/version/numeral/sensibilidad como atributos obligatorios.

## 5. Evaluación table-driven

Resultado: **table-driven parcial, no apto para LIVE**.

Fortalezas:

- Perfil separado por cámara, versión, vigencia, estado, flujo y dirección.
- Campos con posición, longitud, padding, justificación, format y source.
- Fail-fast del renderer oficial para fuente faltante, overlap, longitud excedida y cálculo inválido.
- Calculator centralizado para conteos, hash, montos, bloques, padding y FileId.
- Persistencia EF, historial y publicación.

Brechas del descriptor frente al objetivo:

| Capacidad | Estado |
|---|---|
| Cámara/record/campo/posición/longitud/alineación/relleno/formato/source | Cumple |
| Tipo de dato explícito | No cumple; se infiere en trace |
| Obligatoriedad explícita por variante/flujo | No cumple; trace fuerza `Required=true` |
| Valores permitidos | No cumple como atributo de campo |
| Normalizador/truncamiento explícitos | No cumple; JSON y lógica dispersa |
| Sensibilidad y estrategia de máscara | No cumple |
| RuleId/fuente/versión/numeral | No cumple |
| Severidad | Cumple sólo en `CfgFieldRule`, no en descriptor |
| Validación cruzada | No cumple; existe parcialmente en código y no es trazable como regla normativa |
| Ejecución de rules en generación | No cumple |

No se propone reemplazar el diseño table-driven. Se debe completar el metamodelo y reducir la lógica de cámara fuera de perfil, conservando en un core no editable los cálculos críticos.

## 6. Comparación física

| Propiedad | ACHInterbank | Tercero ACHCOL | Tercero CENIT |
|---|---|---|---|
| Longitud registro | 106 | 106 | 106 |
| Terminadores | Ninguno | Ninguno | Ninguno |
| Bloques | 67 | 42 | 2 |
| Padding T9 | 6 | 2 | 2 |
| Resto físico | 0 | 0 | 0 |
| Tabs/Unicode multibyte | No | No; un byte monobyte permitido | No |

ACHInterbank cumple la estructura física descrita por MAN-004. Esto no compensa los errores internos de campo. Para CENIT, el resultado sólo es comparativo porque la regla oficial no está disponible.

## 7. Comparación estructural ACHInterbank vs tercero ACHCOL

El archivo de ACHInterbank coincide exactamente con el perfil seed actual. El tercero ACHCOL coincide con las posiciones del MAN-004 V32 auditadas. La diferencia no es un simple dato transaccional: el perfil de ACHInterbank comprime varias fechas/totales y desplaza los campos posteriores.

Resumen de offsets:

| Grupo | MAN-004 / tercero | Perfil ACHInterbank | Resultado |
|---|---|---|---|
| T1 fecha | 24–31, 8 | 24–29, 6 | No cumple |
| T1 hora/FileId/size/block/format | 32–42 | 30–40 | No cumple |
| T1 nombres/reference/reservado | 43–106 | 41–106 con cortes 64/87/95 | No cumple |
| T5 fechas | 64–71 y 72–79 | 64–69 y 70–75 | No cumple |
| T5 status/origen/lote | 83 / 84–91 / 92–98 | 79 / 80–87 / 88–94 | No cumple |
| T6 monto | 30–47, 18 | 30–39, 10 | No cumple |
| T6 ID/nombre/indicador/trace | 48–102 | 40–94 | No cumple |
| T8 montos/lote | 21–56 con 18+18; lote 100–106 | 21–44 con 12+12; lote 88–94 | No cumple |
| T9 montos/reservado | 32–67 con 18+18; 68–106 | 32–55 con 12+12; 56–106 | No cumple |

## 8. Comparación estructural ACHInterbank vs tercero CENIT

El tercero CENIT usa 106, bloques de 10 y offsets equivalentes a los observados en MAN-004. El archivo de ACHInterbank auditado usa el perfil desplazado y contiene identificación semántica de CENIT en el header. Esto demuestra una diferencia de interoperabilidad, no una regla CENIT.

El seeder CENIT:

- replica las posiciones erróneas del perfil ACHCOL;
- usa constantes de routing de UAT;
- declara literalmente una fuente “pendiente de homologación formal” y “placeholder”;
- queda `PUBLICADO` y vigente desde 2026-01-01.

Publicar técnicamente un placeholder no lo convierte en perfil normativo.

## 9. Comparación ACHCOL vs CENIT de terceros

Ambos archivos de terceros comparten forma física de 106, T1/5/6/7/8/9, bloques 10 y controles reconciliables con el layout tipo MAN-004. Difieren en cantidad de lotes/detalles, contenido semántico, ReferenceCode, cámara y nombre externo. Esas diferencias pueden ser de datos o perfil.

No se concluye que CENIT adopte las reglas de ACHCOL. La fuente CENIT disponible sólo confirma NACHAM y operación por ciclos; no entrega campos.

## 10. Controles y consistencia

### ACHInterbank

- Conteos, EntryHash, débitos, créditos, batch count, block count y padding se recalculan correctamente cuando se interpretan con el layout actual.
- Los T5/T8 coinciden 4/4 en las posiciones incorrectas 88–94.
- Bajo MAN-004, 0/4 pares T5/T8 son válidos, 0/4 controles de lote completos son válidos y el control de archivo completo no cumple por los totales/posiciones.
- El cálculo es aprovechable; el render no es conforme.

### Tercero ACHCOL

- 78/78 lotes reconciliaron conteo, hash, débitos, créditos y lote T5/T8.
- El control de archivo reconcilió lotes, bloques, detalles+adendas, hash y totales.
- 130/130 T6/T7 estaban asociados inmediatamente y con sufijo coincidente.

### Tercero CENIT

- 4/4 lotes y el control de archivo reconciliaron con el mismo algoritmo comparativo.
- Este resultado no es una certificación ni una fuente normativa CENIT.

## 11. “Ciclo 1”

El literal está en posiciones físicas **87–93** del T1 de ACHInterbank. Con el MAN-004:

- 87–88 pertenecen al final del nombre de la entidad origen inmediato;
- 89–93 pertenecen al comienzo del código de referencia 89–96;
- el desplazamiento nace de la fecha de seis posiciones y de los offsets posteriores del perfil.

`FileHeaderRecord.From` asigna `ReferenceCode` desde el header y, si está vacío, desde `AchCycle.CycleName`. El MAN-004 sólo describe ese campo como “Código del sistema”; no autoriza el literal de ciclo. El tercero ACHCOL deja el campo oficial en blanco; el tercero CENIT contiene un patrón alfanumérico distinto. Ninguno es fuente normativa.

Conclusión ACHCOL: **No cumple** en la ubicación actual y **No demostrado** como valor del ReferenceCode. El ciclo ACHCOL está demostrado en el componente final del nombre externo y en la ventana operacional, no como texto libre de T1.

Conclusión CENIT: **No demostrado — bloqueante para LIVE**. El manual operativo describe ciclos, pero no indica su representación en el archivo.

## 12. Lotes, consecutivos e idempotencia

El modelo actual distingue parcialmente:

- PK `AchBatch.Id` para ordenación técnica;
- `AchBatch.BatchSequenceNumber` persistido;
- `BatchNumberSequence` por cámara + fecha + originador + política;
- `ExternalFileSequence` para el consecutivo del nombre;
- FileId derivado del consecutivo externo 1–36;
- ciclo/fecha en `AchCycle`.

Para ACHCOL, el MAN-004 define el lote T5 como secuencial, ascendente, único dentro del archivo e iniciando en 1; T8 debe repetirlo. El `DailyResetBatchNumberGenerator` incrementa entre archivos del mismo día y originador. Es una política técnicamente concurrente, pero normativamente incorrecta para este campo. La secuencia externa diaria ZZZ sí es otro concepto y no debe mezclarse con el lote.

Riesgos adicionales:

- ordenación por PK para asignar lote;
- reutilización de `BatchSequenceNumber` persistido sin verificar el scope de cámara/fecha/archivo;
- ausencia de límite explícito de 7 dígitos en el store;
- `DateOnly.FromDateTime(processingDate.Date)` sin conversión uniforme a America/Bogota;
- comportamiento SQL/PG con rowversion/retries que aún debe probarse en la fase autorizada.

Para CENIT, inicio/reinicio/unicidad de lote no están demostrados.

## 13. Naming

ACHCOL:

- MAN-004 V32 demuestra `RRRRTTT.ZZZ.1`, ZZZ diario 001–036 y correlación con FileId.
- El builder soporta esa forma conceptual; el seeder guarda extensión `.ach`, aunque el builder no la adjunta en esa rama.
- El archivo comparativo usa patrón 7.3.8.sufijo con `.OUT`; puede pertenecer a otra interfaz/dirección. No se adopta por analogía.

CENIT:

- Builder/parser implementan `7 dígitos.3 dígitos.8 dígitos.sufijo`, sin extensión.
- El tercero coincide en forma.
- El manual local no demuestra este patrón y remite a un Manual STA ausente.

## 14. Parser

`NachaParserService` no es table-driven. Abre `StreamReader`, ejecuta un único `ReadLineAsync`, obtiene el tamaño mediante `Substring(36,3)` y segmenta sólo esa cadena. Los archivos auditados no tienen EOL, por lo cual el caso observado puede entrar completo en una línea; cualquier CR/LF inesperado, BOM, header inválido o perfil distinto altera el comportamiento. Luego usa offsets MAN-004 de 8/18 dígitos, lo cual revela una contradicción interna: el parser espera el layout correcto mientras el generador oficial seed produce el layout comprimido.

El parser persiste entidades y estados; no fue ejecutado por la restricción de no modificar bases de datos.

## 15. Seguridad y privacidad

Hallazgos críticos:

1. Los tres archivos reales están rastreados en Git y no ignorados.
2. `SanitizeTraceValue` sólo detecta secretos técnicos; no enmascara nombres, identificaciones, cuentas, valores, referencias ni Trace Numbers.
3. Cada trace guarda raw, rendered y una vista de la línea; una prueba exige reconstruir el registro completo desde el trace.
4. `HistConfigChange.AfterJson` persiste esa auditoría.
5. Excepciones del parser incluyen importes e identificadores; el builder puede incluir Trace Number; el middleware global registra mensaje y stack.
6. El middleware HTTP omite multipart y descargas binarias, pero su lista de nombres sensibles no cubre todos los campos financieros/personales.
7. El nombre físico real aparece como constante en tests/Playwright, creando correlación empresarial aunque no copie el contenido.

Controles positivos: read models operacionales específicos sí aplican máscaras y banderas de no datos sensibles; estos patrones deben reutilizarse.

## 16. Pruebas y evidencia

Se identificaron pruebas para resolver perfiles, fail-fast, rendering, controles, padding, FileId, naming, concurrencia, SQL/PG, golden files y privacidad básica. Los golden files se declaran explícitamente semirreales, anonimizados y no certificados.

La suite actual no puede ser oracle normativo porque los seeder tests afirman las posiciones erróneas. El test de privacidad sólo busca secretos técnicos y, simultáneamente, otra prueba requiere reconstrucción exacta del registro desde la traza.

No se ejecutaron pruebas. Sólo se hizo descubrimiento con `--list-tests --no-build --no-restore`, sin ejecutar casos.

## 17. Conclusiones

- La arquitectura Opción C es recuperable y no debe reemplazarse por concatenación imperativa.
- El defecto principal ACHCOL está en los perfiles/validadores y en la secuencia física T6/T7, no en la aritmética central.
- El perfil CENIT no puede permanecer publicado como “oficial” con una fuente placeholder.
- La política de lote ACHCOL debe reiniciar por archivo; el consecutivo externo ZZZ sí es diario y separado.
- La protección de datos debe incorporarse como propiedad table-driven, no como búsqueda de palabras secretas.
- Antes de LIVE se requieren fuente vigente, RuleId, implementación, prueba, evidencia y aprobación humana por cada regla crítica.
