# Evidencia de análisis NACHA-M

Fecha: 2026-07-16. Modalidad: diagnóstico estático, lectura de bytes y validaciones no destructivas. No se ejecutaron generadores contra base de datos, uploads, SOAP, MFT/SFTP/FTP, seeds, migraciones, Playwright, pruebas funcionales ni servicios LIVE.

## Estado inicial del repositorio

```text
Rama: ACH-Interbank-Postgresql
Commit: b168ba2e Se agregan referencas reales para que codex los analice
Estado inicial:
?? docs/uat/certificados_pruebas/
```

`docs/uat/certificados_pruebas/` era un cambio preexistente y no fue inspeccionado ni modificado como parte del alcance NACHA-M.

## Control Git de referencias reales

Comandos ejecutados antes de leer contenido:

```powershell
git status --short -- docs/referencias-reales
git ls-files docs/referencias-reales
git check-ignore -v docs/referencias-reales/*
```

Resultado:

- Los tres archivos reales están **rastreados por Git** en el commit actual.
- `git check-ignore` no reportó exclusión.
- `.gitignore` no contiene el par requerido `/docs/referencias-reales/**` y `!/docs/referencias-reales/README.md`.
- El commit actual introdujo los tres binarios/textos reales. No se inspeccionó ni publicó su contenido en Git history; sólo se verificó el `--stat`.
- No se ejecutó `git rm`, reescritura de historia ni modificación automática.

Recomendación posterior, sujeta a aprobación y plan de privacidad:

1. Añadir la exclusión requerida y un README no sensible.
2. Retirar del índice con `git rm --cached -r -- docs/referencias-reales` en una ejecución autorizada.
3. Evaluar alcance de exposición en remotos/clones/backups y necesidad de reescritura controlada de historia.
4. Rotar o invalidar cualquier correlador si el análisis de incidente determina que procede.

## Identificación física anonimizada

| Origen lógico | Nombre físico | Bytes | Modificación local | SHA-256 | Registros × longitud | Bloques | BOM/EOL/tabs | No ASCII | Tipos físicos |
|---|---|---:|---|---|---|---:|---|---:|---|
| ACHInterbank | `0001283.001.20260714.23` | 71.020 | 2026-07-16 08:31:59 -05:00 | `8E18D416227CAE8321328D1E1D9243C28E05D2DDBE8FB3292C2E49C8A1C1FACA` | 670×106 | 67 | No/0/0 | 0 | T1=1; T5=4; T6=327; T7=327; T8=4; T9=7, de los cuales 6 son padding |
| Tercero ACHCOL | `0001283.001.20250331.1.OUT` | 44.520 | 2025-05-19 18:14:23 -05:00 | `F090B5D4BFAB75FE04CD19313EA1ED467D0205F0FC603DE255CF9688C4753518` | 420×106 | 42 | No/0/0 | 1 | T1=1; T5=78; T6=130; T7=130; T8=78; T9=3, de los cuales 2 son padding |
| Tercero CENIT | `0001283.002.20250331.1` | 2.120 | 2025-03-31 14:03:54 -05:00 | `3566E425E7786B841482612C6EBC507ECD4E41996A1B8391D1CF5BE7F29468BE` | 20×106 | 2 | No/0/0 | 0 | T1=1; T5=4; T6=4; T7=4; T8=4; T9=3, de los cuales 2 son padding |

El byte no ASCII del archivo ACHCOL es un carácter de una letra permitida por la tabla de caracteres del MAN-004 cuando se interpreta en una codificación monobyte compatible. Sin BOM no es posible distinguir de forma concluyente entre las codificaciones monobyte candidatas. No se publica el byte dentro de su registro ni su contexto transaccional.

## Método de lectura y controles

1. Lectura con `System.IO.File.ReadAllBytes`.
2. Hash con `Get-FileHash -Algorithm SHA256`.
3. Separación por offsets de 106 bytes, sin usar CR/LF.
4. Conteo de BOM, CR, LF, tabs, bytes mayores a 127 y residuo módulo 106.
5. Clasificación del primer byte de cada registro y distinción entre T9 de control y padding de 106 caracteres `9`.
6. Recalculo agregado de conteos, hash, débitos, créditos, bloques y correspondencias sin imprimir valores individuales.
7. Comparación paralela de offsets del perfil actual y offsets del MAN-004 V32.

Resultados reproducibles principales:

| Validación | ACHInterbank | Tercero ACHCOL | Tercero CENIT |
|---|---:|---:|---:|
| Header con posiciones MAN-004 | No cumple | Cumple | Coincide físicamente; no demuestra norma CENIT |
| Header con perfil actual | Cumple | No cumple | No cumple |
| Pareja lote T5/T8 en posiciones MAN-004 | 0/4 | 78/78 | 4/4 comparativas |
| Pareja lote T5/T8 en posiciones del perfil actual | 4/4 | 0/78 | 0/4 |
| Forma T6 en posiciones MAN-004 | 0/327 | 130/130 | 4/4 comparativas |
| Forma T6 en posiciones del perfil actual | 327/327 | 0/130 | 0/4 |
| T6 seguido inmediatamente por T7 | 4/327 | 130/130 | 4/4 comparativas |
| Sufijo T7 igual al T6 con offsets MAN-004 | 0/327 | 130/130 | 4/4 comparativas |
| Controles de lote completos MAN-004 | 0/4 | 78/78 | 4/4 comparativas |
| Control de archivo completo MAN-004 | No cumple | Cumple | Coincide comparativamente |
| Controles según layout actual de ACHInterbank | 4/4 lotes y archivo | No aplica | No aplica |

“Coincide comparativamente” no equivale a cumplimiento CENIT.

## Inventario técnico obligatorio

| # | Componente | Proyecto / archivo / clase o método | Responsabilidad y cámara | Dependencia / prueba encontrada | Riesgo |
|---:|---|---|---|---|---|
| 1 | Generador | Persistence, `NachaFileBuilder.BuildOfficialTableDrivenFileAsync` | Construcción 1/5/6/7/8/9, ambas | Resolver, perfiles, calculator; `OfficialNachaGenerationTableDrivenTests` | Clase de 3.387 líneas y lógica imperativa residual |
| 2 | Parser | Persistence, `NachaParserService` | Parseo/persistencia inbound, ambas | EF y catálogos; functional/incoming tests | Sólo lee una línea y usa offsets imperativos; no ejecutar en auditoría |
| 3 | Motor table-driven | Domain/Persistence, `CfgProfile*`, `NachaConfigResolver` | Perfil por cámara/flujo/dirección/vigencia | EF config; resolver tests | Existe, pero reglas críticas no se ejecutan en render |
| 4 | Descriptores | Domain, `CfgLayoutField`, `CfgFieldSourceDefinition`, `CfgFieldRule` | Posición, longitud, padding, source, format | Config admin/seeder tests | Sin sensibilidad, fuente normativa, obligatoriedad ni truncamiento first-class |
| 5 | Perfil ACHCOL | Seeder, `OFFICIAL_ACH_SALIDA_ORIGINAL_V1_0` | Salida original ACHCOL | MAN-004 declarado; seeder tests | Posiciones/longitudes contradicen MAN-004 V32 |
| 6 | Perfil CENIT | Seeder, `OFFICIAL_CENIT_SALIDA_ORIGINAL_V1_0` | Salida original CENIT | Fuente placeholder; seeder tests | Publicado sin ficha técnica homologada |
| 7 | Registro T1 | Seeder `BuildRecord1`; `FileHeaderRecord.From` | Header por cámara | Header/table-driven tests | Fecha 6, offsets desplazados, ReferenceCode desde ciclo |
| 8 | Registro T5 | Seeder `BuildRecord5`; `BatchHeaderRecord.From` | Encabezado lote | Batch/header tests | Fechas 6 y lote/status/origen desplazados |
| 9 | Registro T6 | Seeder `BuildRecord6`; `BuildEntryDetailRecordsOfficialAsync` | Detalle | Record6/mapping tests | Monto 10, indicador y trace desplazados |
| 10 | Registro T7 | Seeder `BuildRecord7`; strategy/renderer | Adenda | Type7 tests | Genérico; builder agrupa todos los T6 y luego T7 |
| 11 | Registro T8 | Seeder `BuildRecord8`; `BatchControlRecord.From` | Control lote | Calculator/control tests | Totales 12 y lote desplazado |
| 12 | Registro T9 | Seeder `BuildRecord9`; `FileControlRecord.From` | Control archivo/padding | Calculator/control tests | Totales 12 y reservado incorrecto |
| 13 | Nombre físico | `ExternalFileNameBuilder`, parsers y policy | ACHCOL/CENIT/retornos | Filename tests | Contratos/extensiones inconsistentes; fuente CENIT ausente |
| 14 | Hash de archivo | Builder, SHA-256 sobre `Encoding.ASCII` | Auditoría interna | Trace tests | ASCII puede reemplazar caracteres; no hay política de encoding aprobada |
| 15 | Totales | `NachaControlTotalsCalculator` | Conteos/hash/montos | Calculator tests | Algoritmo sólido, pero longitudes provienen de perfil erróneo |
| 16 | Bloques | Calculator | `ceil(registros/BlockSize)` | Calculator tests | Cumple muestra; regla CENIT no demostrada |
| 17 | Padding | Builder `BuildPaddingRecord` | T9 al final | Padding tests | Cumple muestra; depende de block size |
| 18 | Factor bloqueo | Perfil T1 + calculator | Configura tamaño de bloque | Config/control tests | Valor correcto en offset incorrecto ACHCOL |
| 19 | Codificación | Builder retorna `string`; SHA usa ASCII | Salida | No hay prueba normativa por cámara | Política implícita y pérdida potencial de caracteres |
| 20 | Terminadores | Builder concatena sin EOL | Salida | Golden tests | Cumple muestra; parser usa `ReadLineAsync` y queda desacoplado |
| 21 | Trace Number | Seeder/builder/parser | T6 y correlación T7 | Record6/Type7 tests | Posición errónea y exposición en excepciones/trazas |
| 22 | Indicador adenda | Seeder/type7 strategy | T6↔T7 | Type7 tests | Posición errónea; no existe gate físico previo |
| 23 | FileId | Calculator + filename policy | ZZZ↔A-Z/0-9 | Filename/control tests | Perfil ubica en 34; policy escribe posición 36 |
| 24 | Número lote | Builder + daily generator | T5/T8 | Batch tests | Política diaria contradice reinicio por archivo ACHCOL |
| 25 | Consecutivo diario | External filename sequence services | Nombre externo | Provider tests | Más robusto que lote, pero fecha no normalizada a Bogotá |
| 26 | Ciclo operacional | `AchCycle`, filename builder | Nombre/metadato | Cycle/filename tests | Se usa además como ReferenceCode sin fuente |
| 27 | Fecha operacional | `AchCycle.ProcessingDate` | Perfil, secuencias y nombre | Date/reset tests | `DateTime.Date`/`Today` sin frontera Bogotá uniforme |
| 28 | Persistencia secuencias | `BatchNumberSequence`, `ExternalFileSequence` | Lotes y nombres | EF configs/migrations | Dos conceptos de secuencia con políticas distintas |
| 29 | Prevención duplicados | `ExternalFileDuplicateGuard`, registry | Nombre/hash según flujo | Filename/Postgres tests | Regla CENIT y llave de duplicado no homologadas |
| 30 | Idempotencia | Batch persisted reuse, filename registry | Reintentos | Batch/filename tests | Lote persistido se reutiliza sin probar cámara/fecha de origen |
| 31 | Concurrencia | RowVersion, índice único, Postgres upsert, SQL locks | SQL Server/PostgreSQL | Concurrency/provider tests | Batch store genérico depende de retry/rowversion; sin límite 7 dígitos |
| 32 | Validación previa | `NachaConfigValidationService`; semantic validator | Publicación/emisión | Config validation tests | Valida longitudes erróneas, no posiciones exactas y omite T6/T7 completas |
| 33 | Auditoría | `HistConfigChange`, generation trace, filename logs | Trazabilidad | Trace tests | Persiste campos y líneas reconstruibles con datos sensibles |
| 34 | Enmascaramiento | `SanitizeTraceValue`; logging middleware | Trazas/logs/API | Security test sólo secretos | No enmascara datos financieros/personales por campo |
| 35 | Aprobación LIVE | `CfgPublishRequest`; checklists UAT | Gobierno | Documentación/approval surfaces | Perfil publicado no equivale a aprobación normativa/humana LIVE |

## Fuentes documentales y hashes

| Documento | Emisor | Versión / fecha | Ruta local | SHA-256 | Uso | Vigencia aparente / duda |
|---|---|---|---|---|---|---|
| Manual de Servicio ACH Transferencias Interbancarias | ACH Colombia | V32, enero 2025 | `docs/normativa/pdf/ACH-Colombia-V32.pdf` | `D83585B53B31A3A70E4861412F48FF8306ED0D2F439A7443C05E540F6B5736EE` | 6.1.2–6.1.4, 6.1.10.1/3, fichas T1/T5/T6/T7/T8/T9 | Copia más reciente local; capítulo conserva rótulo V31; vigencia CFA por confirmar |
| Anexo 2 Manual Operativo CENIT | Banco de la República | Fecha 27-02-2025; versión no visible | `docs/normativa/pdf/CENIT-DSP-152-Anexo-2.pdf` | `AD6BB2FC48CCF78CE0BDB980BBFFFCAF9D42E52882CC16559A9336F41CFC902D` | Ciclos, NACHAM, canales, STA | No contiene layout NACHA-M |
| CENIT Anexo A Causales de Devolución | Banco de la República | No demostrada | `docs/normativa/pdf/CENIT-Anexo-A-Causales-Devolucion.pdf` | `D3A8F12EC49876CBFF516DAA3A1651693C5DAE0D75DC2DF8FFE733C1A8A00EFE` | Contexto de devoluciones | No define layout |
| CENIT Anexo B Causales de Rechazo | Banco de la República | No demostrada | `docs/normativa/pdf/CENIT-Anexo-B-Causales-Rechazo.pdf` | `ADF05ED85BF8EF136C61A1560D073EB6B375D50571530DC679164AF78C2A530A` | D04/D05 y formato erróneo | Cita Manual STA ausente |
| Contexto Fase 6 | Proyecto CFA | Estado local | `docs/ai/ACH_PHASE6_CONTEXT.md` | No usado como fuente normativa | Arquitectura y guardrails | Decisión interna, menor jerarquía |
| Dirección table-driven | Proyecto CFA | 2026-05-24 | `docs/architecture/NACHA_CONFIG_TABLE_DRIVEN_OFFICIAL.md` | No usado como fuente normativa | Opción C/fail-fast | Productivo NO-GO |

Las transcripciones Markdown de los PDF se usaron para búsquedas y lectura textual; la fuente primaria catalogada sigue siendo el PDF. SHA-256 de transcripciones: ACHCOL `06F7F37A65817C9E830CAACB68ECE984E7BEC60BF24B13C14F072DF03AE597D2`; CENIT Anexo 2 `ABF9862DF69B2B62D9E43E8B9B0D1A5C9D864A759733EAB763760E383BA15F5A`.

## Validaciones de entorno ejecutadas

- `.NET SDK 10.0.301`; solución con seis proyectos.
- Descubrimiento `dotnet test --list-tests --no-build --no-restore`: salida exitosa; 1.741 casos descubiertos, 854 relacionados por nombre con NACHA/CENIT/naming/lotes. No se ejecutó ningún caso.
- Docker 29.6.1 y Compose 5.3.0; `docker compose config --services` reportó API y SPA. No se levantaron ni alteraron contenedores.
- Revisión estática de migraciones SQL Server y PostgreSQL; no se ejecutó ninguna migración ni conexión.

## Limitaciones

- No se verificó visualmente cada página del PDF contra la transcripción local.
- No se consultaron fuentes externas; la auditoría se limitó a documentación disponible en el repositorio.
- No se ejecutaron tests, build, parser, generador, DB, API, SPA ni Playwright.
- No se afirma certificación, ausencia de sanciones ni cumplimiento productivo.

