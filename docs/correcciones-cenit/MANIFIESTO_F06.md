# MANIFIESTO_F06 — Correcciones CENIT 2026-11-02

**Base inmediata:** `ACHInterbank_PROYECTO_COMPLETO_CORRECCIONES_CENIT_F05_2026-11-02.zip` SHA-256 `df34d0c4c6f2bee8c5099657cdae9272da1f861beaff42bb71879dbd80e84ed0`, 4.644 entradas verificadas por CRC y SHA; ZIP destino `ACHInterbank_PROYECTO_COMPLETO_CORRECCIONES_CENIT_F06_2026-11-02.zip`. SHA del ZIP final en `.zip.sha256` lateral (no autorreferenciable).

| Indicador | Resultado |
|---|---:|
| Entradas F05 conservadas | 4644/4644 |
| Archivos originales modificados de forma intencional | 11 (5 C# + 6 documentos de seguimiento) |
| Archivos nuevos F06 | 7 (6 documentos + 1 script de pruebas estáticas) |
| Entradas del ZIP final esperadas | 4651 |
| Archivos eliminados | 0 |
| Archivos productivos modificados | 3 |
| Archivos de pruebas xUnit modificados | 2 |
| Nuevos escenarios xUnit fuente | 25, NO EJECUTADOS |

## Archivos C# modificados
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaInboundValidation.cs`
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaParserService.cs`
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaPhysicalParser.cs`
- `tests/Cfa.ACHInterbank.Tests/NachaInboundValidationSeparationTests.cs`
- `tests/Cfa.ACHInterbank.Tests/NachaPhysicalParserTests.cs`

## Archivos nuevos
- `docs/correcciones-cenit/F06_CAMBIOS.patch`
- `docs/correcciones-cenit/F06_COMPARACION_SHA256.tsv`
- `docs/correcciones-cenit/F06_EVIDENCIA_CONTROLES_ESTATICOS.txt`
- `docs/correcciones-cenit/F06_INFORME_CORRECCIONES.md`
- `docs/correcciones-cenit/F06_MEMORY_STATE.md`
- `docs/correcciones-cenit/MANIFIESTO_F06.md`
- `scripts/test/cenit_f06_static_check.py`

## Controles y límites

- Todos los archivos F05 preservados: los 4.633 archivos no modificados se comparan por SHA-256 idéntico (4.644 entradas totales - 11 modificados); modificaciones exactas en `F06_COMPARACION_SHA256.tsv`; CRC e inspección del ZIP nuevo al emitir la entrega.
- Reversibilidad: `F06_CAMBIOS.patch` aplicado/revertible en seco sobre 5 fuentes originales, sin migraciones ni eliminaciones. Restaurar documentos desde ZIP F05 tras reversión.
- Pruebas de fuente: F06 24/24, F01–F05 89/89. Pruebas xUnit C# ejecutadas 0; SDK .NET ausente; SQL, Quartz, gateway, muestras oficiales, regresión ACH Colombia no ejecutados. No afirmar homologación.
- Dxx STA permanece en el catálogo específico existente; CENIT NACHA-M no los comunica como error físico/fatal/formal. ACH Colombia conserva comportamiento histórico y no se modificaron archivos exclusivos.
