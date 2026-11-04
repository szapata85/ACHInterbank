# MANIFIESTO_F05 — Entrega correctiva CENIT — 2026-11-02

**Base inmediata:** `ACHInterbank_PROYECTO_COMPLETO_CORRECCIONES_CENIT_F04_2026-11-02.zip`, SHA-256 `9bf9b515affe93f1a54076326ad1eb081b2fc1538c518faf06519de6afe9183a`; **ZIP salida:** `ACHInterbank_PROYECTO_COMPLETO_CORRECCIONES_CENIT_F05_2026-11-02.zip`. SHA-256 del ZIP se genera en archivo lateral `.sha256` (no puede autocitarse en el propio ZIP sin circularidad).

| Ítem | Resultado |
|---|---:|
| Entradas originales ZIP F04 | 4637 |
| Entradas F04 conservadas | 4637 |
| Archivos originales cambiados intencionalmente (fuente + documentación) | 12 |
| Nuevos archivos F05 | 7 |
| Archivos eliminados | 0 |
| Entradas de ZIP F05 | 4644 |
| Archivos productivos modificados | 3 |
| Archivos de pruebas xUnit existentes modificados | 3 |
| Nuevos archivos xUnit | 0 |
| Nuevo script de verificación estática | 1 |

### Cambios existentes en fuente C# (6)
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchReturnOfReturnFileGenerationService.cs`
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/CenitReturnOfReturnService.cs`
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/Seeders/RegulatoryCatalogSeeder.cs`
- `tests/Cfa.ACHInterbank.Tests/AchReturnOfReturnFileGenerationServiceTests.cs`
- `tests/Cfa.ACHInterbank.Tests/CenitReturnOfReturn2026Tests.cs`
- `tests/Cfa.ACHInterbank.Tests/RegulatoryCatalogSeederReturnOfReturnPoliciesByClearingHouseTests.cs`

### Nuevos archivos F05
- `docs/correcciones-cenit/F05_CAMBIOS.patch`
- `docs/correcciones-cenit/F05_COMPARACION_SHA256.tsv`
- `docs/correcciones-cenit/F05_EVIDENCIA_CONTROLES_ESTATICOS.txt`
- `docs/correcciones-cenit/F05_INFORME_CORRECCIONES.md`
- `docs/correcciones-cenit/F05_MEMORY_STATE.md`
- `docs/correcciones-cenit/MANIFIESTO_F05.md`
- `scripts/test/cenit_f05_static_check.py`

### Verificaciones y excepciones

- Ficheros originales F04 no editados: SHA-256 idéntico al del ZIP F04. Archivos modificados: `F05_COMPARACION_SHA256.tsv` (hash individual antes/después). Ficheros nuevos íntegros en ZIP final.
- CRC final mediante `ZipFile.testzip()`; recuento de rutas y contenido SHA-256 comprobado al leer ZIP recién generado.
- Reversión del código original: `F05_CAMBIOS.patch` aplicado y revertido en seco (`patch -p1 --dry-run`, `patch -R -p1 --dry-run`), 6/6 archivos. Restaurar ZIP F04 o aplicar inverso + retirar nuevos docs/script; no migraciones ni eliminaciones.
- Controles estáticos: F01 12/12, F02 13/13, F03 17/17, F04 21/21, F05 26/26. **No** son prueba de compilación .NET.
- No existe `dotnet` SDK; xUnit 0 ejecutadas/0 aprobadas/0 fallidas ejecutadas. Bases SQL, Quartz, gateways, operador y regresión funcional ACH Colombia pendientes.
- Regresión de archivo ACH Colombia: archivos fuera de los 6 cambios C# intencionales y documentación no alterados; el cambio C# compartido solo se activa en la ruta de cámara CENIT. El seeder de ACH Colombia se mantiene independiente. No se afirma homologación.
- Riesgos C1: validar raw/evento/fecha efectiva en muestras CENIT; en falta de evidencia de correlación falla cerrado, no sintetiza retorno financiero.
