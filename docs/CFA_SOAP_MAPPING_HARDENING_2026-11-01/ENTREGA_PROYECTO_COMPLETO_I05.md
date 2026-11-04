# Entrega integral — SOAP-MAP-SAFE-001 / I05

**Archivo:** `ACHInterbank_PROYECTO_COMPLETO_SOAP_MAP_SAFE_001_I05_INTEGRADO_2026-11-01.zip`  
**Base:** ZIP completo I04 del 2026-11-01, con I01–I04 incluidos.  
**Contenido:** solución .NET, API, infraestructura, migraciones, tests, SPA Angular, scripts y documentación Markdown; **no requiere aplicar patches**.

## Archivos de desarrollo I05

- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/CfaSoapOperationParityGuard.cs` (nuevo helper pequeño)
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/ProcContrapartidasRequestMapper.cs` (paridad previa a retorno contract-driven)
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/ProcTransaccionesRequestMapper.cs` (paridad previa a retorno contract-driven)
- `tests/Cfa.ACHInterbank.Tests/CfaSoapOperationParityGuardTests.cs` (nuevo)

## Markdown I05

- `DECISIONES_I05_PARIDAD_SOAP.md`
- `ENTREGA_PROYECTO_COMPLETO_I05.md`
- `MEMORY_STATE.md` y `PLAN_INCREMENTAL.md` actualizados; `README.md` y `MEMORY.md` de raíz actualizados.

## Validaciones y estado

| Incremento | Estado | Deuda abierta |
| --- | --- | --- |
| I01 | `IMPLEMENTED / NOT_RUN_ENVIRONMENT` | Compilación, xUnit, concurrencia y throughput |
| I02 | `IMPLEMENTED / NOT_RUN_ENVIRONMENT / BLOCKED_FUNCTIONAL` | 5 campos de contrapartidas por homologar |
| I03 | `IMPLEMENTED / NOT_RUN_ENVIRONMENT / BLOCKED_FUNCTIONAL` | Cuenta origen y semántica de campos por homologar |
| I04 | `IMPLEMENTED / NOT_RUN_ENVIRONMENT` | Validación ejecutable, concurrencia y confirmación de persistencia |
| **I05** | **`IMPLEMENTED / NOT_RUN_ENVIRONMENT`** | Pruebas de paridad escritas, compilación/xUnit pendientes |
| I06 | `TODO` (**siguiente**) | Cierre técnico, tests y deuda de ambiente (sin UAT) |
| I07 | `TODO` (final) | UAT y homologación bancaria |

`UseContractDrivenRequest` sigue desactivado y sin cutover. Live monetario no autorizado. UAT no iniciado. Para reglas y evidencias leer `DECISIONES_I05_PARIDAD_SOAP.md`.
