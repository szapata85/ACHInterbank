# I05 — Paridad mínima entre SOAP legacy y contract-driven

**Programa:** `SOAP-MAP-SAFE-001`  
**Fecha:** 2026-11-01  
**Estado:** `IMPLEMENTED / NOT_RUN_ENVIRONMENT` (sin afirmación de certificación).  
**Base:** entrega completa I04. No hay cambios de seed ni parametrización del usuario.

## Decisión

Durante `UseContractDrivenRequest=true`, los dos mappers monetarios construyen el request contract-driven con el motor existente, **pero antes de devolver el XML** lo comparan con el request legacy del mismo contrato tipado:

- Operación exacta, namespace `http://tempuri.org/` y `SOAPAction` baseline de cada método.
- Sobre de salida SOAP 1.1 obligatorio.
- Campos en idéntico orden, con idénticos nombres XML y valores serializados.
- Distinción entre elemento vacío, `xsi:nil` y campo omitido; no comparar prefijos XML arbitrarios.
- Diferencia ⇒ excepción `PROC_TRANSACCIONES_WIRE_PARITY_MISMATCH` o `PROC_CONTRAPARTIDAS_WIRE_PARITY_MISMATCH` **antes de transporte**.

Se usa `CfaSoapOperationParityGuard` (helper interno, sin servicio nuevo ni persistencia); no se introduce otro motor ni se alteran los source providers, transformations o parser.

| Operación | Paridad que queda protegida | Estado funcional |
| --- | --- | --- |
| `Proc_Contrapartidas` | Contrato tipado y cuerpo contract-driven, 22 campos del baseline, secuencia, nulos, namespace y SOAPAction | Implementado; `OFEMP`, `OFIDARCH`, `OFIDLOT`, `OFST`, `OFDIRECCIONIP` siguen sin homologación |
| `Proc_Transacciones` | Parámetros legacy y cuerpo contract-driven, omisiones actuales, orden, namespace y SOAPAction | Implementado; `NCTAORIG`, `IDORIG` e `INFPAG` mantienen pendientes de homologación |
| `RegistrarRespuestaTransaccion` | **Sin cambio**: `RegistrarRespuestaRequestParityPolicy.EnsureBaselineV1` ya obliga al contrato v1 de siete campos exactos, nombre, orden y tipos; el writer y gateway conservan su comportamiento | I04 implementado, pruebas ejecutables pendientes |

La comprobación **no sustituye** la homologación de fuentes SQL contra el significado bancario. El cuerpo legacy es baseline técnico temporal, no documento normativo. El nuevo camino no se activa por este incremento.

## Código (mínimo)

1. `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/CfaSoapOperationParityGuard.cs` — compara SOAP usando `XElement` (sin guardar payload ni añadir dependencias).
2. `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/ProcContrapartidasRequestMapper.cs` — llama al guard antes de devolver el XML contract-driven.
3. `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/ProcTransaccionesRequestMapper.cs` — igual, preservando serialización legacy y flags.
4. `tests/Cfa.ACHInterbank.Tests/CfaSoapOperationParityGuardTests.cs` — casos positivos y negativos de operación, valores, orden, nombre, empty/nil, SOAPAction, envelope.

## Evidencia y límites

- `python scripts/validate-solution-projects.py` → `SOLUTION GRAPH: OK` (7/7).
- SDK `dotnet`: ausente; **no** se ejecutaron compilación ni xUnit (`NOT_RUN_ENVIRONMENT`). Los tests se añadieron pero no se declaran `PASS`.
- No se ejecutaron BD, endpoints SOAP, UAT ni Live monetario.
- Sin cambios en NACHA-M, WSDL, migrations, opciones/feature flags, seed, scheduler, SPA o motor central.

## Gates diferidos para I06 (CMD)

```cmd
python scripts/validate-solution-projects.py
dotnet build ACHInterbank.sln -c Release
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --filter "FullyQualifiedName~CfaSoapOperationParityGuardTests|FullyQualifiedName~IntegrationMappingEndToEndTests|FullyQualifiedName~NachaDesagregadoIntegrationMappingTests|FullyQualifiedName~RespuestaTransaccionesAchGatewayTests"
```

**Cutover:** `NOT_AUTHORIZED` hasta P8.4, validación ejecutable y homologaciones pendientes. La UAT corresponde únicamente a I07.
