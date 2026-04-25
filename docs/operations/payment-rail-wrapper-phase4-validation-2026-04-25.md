# Validación técnica — PaymentRail Wrappers ACH/CENIT (Prompt 4) — 2026-04-25

## Alcance

Evolución de stubs ACH/CENIT a wrappers pasivos reales en la capa PaymentRail, sin mover source of truth operacional legacy.

## Cambios implementados

1. Extensión de contrato de estrategia:
   - `CapabilityStatuses`
   - `EvaluateCapabilityWrapper(...)`
   - `BuildCapabilityShadowSnapshot(...)`

2. Modelos wrapper por capacidad:
   - `PaymentRailCapabilityKind`
   - `PaymentRailCapabilityExecutionMode`
   - `PaymentRailCapabilityStatus`
   - `PaymentRailWrapperCallRequest`
   - `PaymentRailWrapperCallResult`

3. Estrategias ACH/CENIT enriquecidas como wrappers pasivos:
   - capacidades explícitas por riel/capability;
   - decisión wrapper pasiva (`PAYMENT_RAIL_WRAPPER_PASSIVE`);
   - `UseLegacyDecision=true` y `BehaviorChanged=false`.

4. UNKNOWN mantiene fail-closed:
   - `PAYMENT_RAIL_WRAPPER_UNKNOWN_FAIL_CLOSED`.

5. Contexto bridge enriquecido:
   - `PaymentRailResolvedContext` ahora incluye `CapabilityStatuses`.

## Reglas preservadas

- Ciclo, dispatch, returns, netting y liquidez siguen en owner legacy.
- Sin cambios en parser NACHA, command center, state machine, resiliencia ni observabilidad funcional.
- Sin cambios criptográficos ni migraciones de base de datos.

## Pruebas ejecutadas

```bash
dotnet build ACHInterbank.sln -c Release
```
- Build: OK (0 errores; warnings legacy preexistentes).

```bash
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --filter "FullyQualifiedName~PaymentRail|FullyQualifiedName~RoutingStrategyServiceTests"
```
- Passed: 11
- Failed: 0

```bash
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --filter "FullyQualifiedName~IncomingNacha"
```
- Passed: 63
- Failed: 0

```bash
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --filter "FullyQualifiedName~Nacha|FullyQualifiedName~Mapping|FullyQualifiedName~BatchNumber"
```
- Passed: 193
- Failed: 0

```bash
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --filter "FullyQualifiedName~DependencyInjection"
```
- Passed: 2
- Failed: 0

## Conclusión

Prompt 4 validado: wrappers ACH/CENIT operacionales pasivos listos para shadow compare futuro, sin alterar comportamiento funcional legacy.
