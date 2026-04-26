# Validación técnica — Prompt 5 Shadow Compare Cycle + Dispatch (2026-04-25)

## Alcance

Implementación de shadow compare real en ciclo y dispatch, con operación legacy intacta (sin cutover).

## Cambios ejecutados

1. Servicio de comparación:
   - `IPaymentRailShadowCompareService`
   - `PaymentRailShadowCompareService`

2. Cycle shadow compare:
   - `IncomingNachaCycleResolver` ejecuta comparación pasiva y adjunta `shadowCompare` en evidencia.
   - traza técnica `PAYMENT_RAIL_SHADOW_COMPARE_CYCLE`.

3. Dispatch shadow compare:
   - `IncomingNachaDispatchPlanner` ejecuta comparación pasiva sobre capability dispatch.
   - traza técnica `PAYMENT_RAIL_SHADOW_COMPARE_DISPATCH`.

4. DI:
   - Registro de `IPaymentRailShadowCompareService` en `AddApplication`.

## Conciliación de evidencia (Prompt 5A)

Se detectó una contradicción previa entre:

- una salida de entorno indicando `dotnet: command not found`, y
- este documento reportando build/tests en verde.

Para cerrar la brecha, se ejecutó una **revalidación real y trazable** en el entorno actual con setup explícito de SDK.

## Setup y verificación de SDK ejecutados

```bash
bash scripts/codex/setup-codex-env.sh
export DOTNET_ROOT=$HOME/.dotnet
export PATH=$HOME/.dotnet:$HOME/.dotnet/tools:$PATH
dotnet --info
dotnet --list-sdks
dotnet ef --version
cat global.json
git status --short
git log --oneline -10
```

Resultado:

- SDK detectado e instalado: **.NET SDK 10.0.203**.
- Runtime host: **10.0.7**.
- EF CLI: **10.0.7**.
- `global.json` fija `10.0.203` con `rollForward: disable`.

## Pruebas ejecutadas (revalidación real)

```bash
dotnet build ACHInterbank.sln -c Release
```
- Build OK (**0 errores, 0 warnings**).

```bash
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --filter "FullyQualifiedName~PaymentRail|FullyQualifiedName~RoutingStrategyServiceTests"
```
- Passed: 13
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

Prompt 5A conciliado y validado: existe evidencia inequívoca de SDK disponible, build real y tests reales con conteos consistentes (13/63/193/2), manteniendo shadow compare pasivo y **legacy como source of truth**, sin cutover ni cambios funcionales.
