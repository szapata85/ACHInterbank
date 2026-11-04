# MANIFIESTO_F04 — Correcciones CENIT, fecha 2026-11-02

**Base F03:** `ACHInterbank_PROYECTO_COMPLETO_CORRECCIONES_CENIT_F03_2026-11-02.zip`, SHA-256 `60f8061775ab19a76eac1c3d69061ed036976fe85b1976fedd4e79e5c47abcb1`; **ZIP F04:** `ACHInterbank_PROYECTO_COMPLETO_CORRECCIONES_CENIT_F04_2026-11-02.zip`; el SHA-256 final se genera fuera del ZIP en el archivo `.sha256` para evitar autorreferencia.

| Ítem | Cantidad |
|---|---:|
| Ficheros originales F03 conservados | 4628 |
| Ficheros originales modificados deliberadamente (código/tests + docs) | 12 |
| Ficheros nuevos F04 (incluido manifiesto) | 9 |
| Ficheros eliminados | 0 |
| Entradas ficheros en ZIP F04 | 4637 |
| Archivos productivos editados | 5 |
| Archivos de prueba originales editados | 1 |
| Nuevos archivos de prueba | 2 |
| Nuevo script de prueba estática | 1 |

**Cambios de código originales (6):**
- `src/Cfa.ACHInterbank.Application/ACH/Models/CenitIncomingReturnPolicyModels.cs`
- `src/Cfa.ACHInterbank.Application/ACH/Services/CenitIncomingReturnPolicy.cs`
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchReturnEligibilityPolicyComponent.cs`
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchReturnsService.cs`
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/IncomingNachaPostParseProcessor.cs`
- `tests/Cfa.ACHInterbank.Tests/CenitIncomingReturnPolicyTests.cs`

**Nuevos archivos (además de documentos):**
- `scripts/test/cenit_f04_static_check.py`
- `tests/Cfa.ACHInterbank.Tests/CenitF04CalendarRouteTests.cs`
- `tests/Cfa.ACHInterbank.Tests/CenitF04ReturnPolicyTests.cs`

**Verificaciones:** SHA-256 por entrada frente F03; CRC del ZIP final; `patch` aplicado y reversión en seco 6/6; F01 12/12, F02 13/13, F03 17/17, F04 21/21 controles estáticos PASS. Sin pruebas .NET ejecutadas ni certificación. **ACH Colombia:** ningún archivo externo a estos cambios está editado. Rutas compartidas afectadas delimitadas por `IsCenit`/código CENIT. Mantener las auditorías previas y todos los ZIP históricos de corrección incorporados como contenido documental.

**Reversión:** `F04_CAMBIOS.patch` en sentido inverso para los 6 archivos originales C#/test; eliminar 2 archivos nuevos xUnit y 1 script; revertir documentos F04 según tabla comparativa, o restaurar F03 íntegro y verificar su SHA-256.

**Alcance externo aún abierto:** calendario bancario/fecha real de ingreso, festivos y notificación; pruebas dinámicas, archivos homologados, PT-07/08/09/20/21/22. CUD fuera de alcance.
