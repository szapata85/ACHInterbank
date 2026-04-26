# Validación técnica — Prompt 6 Shadow Compare Returns + Return-of-Return + Netting + Liquidity (2026-04-26)

## Alcance

Implementación de shadow compare pasivo para capacidades de retorno y ciclo CENIT avanzado, manteniendo a legacy como source of truth.

## Cambios ejecutados

1. Extensión de `IPaymentRailShadowCompareService` y `PaymentRailShadowCompareService` para:
   - `CompareReturnOperation`;
   - `CompareNettingOperation`;
   - `CompareLiquidityOperation`.
2. Integración pasiva:
   - `AchReturnsService` con traza `PAYMENT_RAIL_SHADOW_COMPARE_RETURN`.
   - `ReturnOfReturnOrchestrator` con traza `PAYMENT_RAIL_SHADOW_COMPARE_RETURN_OF_RETURN`.
   - `CenitNettingService` con traza `PAYMENT_RAIL_SHADOW_COMPARE_NETTING`.
   - `LiquidityOptimizationService` con traza `PAYMENT_RAIL_SHADOW_COMPARE_LIQUIDITY`.
3. Pruebas actualizadas:
   - `PaymentRailShadowCompareServiceTests` cubre Return/Netting/Liquidity.

## Revalidación formal Prompt 6A (2026-04-26)

### Setup y verificación SDK

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

- SDK activo: **10.0.203**
- EF CLI: **10.0.7**
- `global.json` fijo en `10.0.203` (`rollForward: disable`)

### Validación ejecutada

```bash
dotnet build ACHInterbank.sln -c Release
```
- Build OK (0 errores, warnings preexistentes).

```bash
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --filter "FullyQualifiedName~PaymentRail|FullyQualifiedName~RoutingStrategyServiceTests"
```
- Passed: 15
- Failed: 0

```bash
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --filter "FullyQualifiedName~PaymentRail|FullyQualifiedName~AchReturns|FullyQualifiedName~ReturnOfReturn|FullyQualifiedName~CenitNetting|FullyQualifiedName~LiquidityOptimization"
```
- Passed: 18
- Failed: 0

Corrección aplicada para estabilizar suite SQLite (sin cambios funcionales operativos):

- se completó seed de prerequisitos FK/NOT NULL en `CenitOperationalGovernanceTests` (configuración de cámara, catálogo de descripción, instituciones con `CheckDigit`);
- se ajustó `ResolveNextCycleIdAsync` para evaluación equivalente y compatible con traducción EF Core SQLite.

```bash
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --filter "FullyQualifiedName~PaymentRailShadowCompareServiceTests"
```
- Passed: 4
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

Prompt 6A revalidado con ejecución real y conteos explícitos:

- legacy se mantiene como source of truth;
- shadow compare se mantiene pasivo/fail-open;
- sin cutover;
- sin cambios criptográficos;
- workflow se mantiene manual-only (no reactivación automática).
