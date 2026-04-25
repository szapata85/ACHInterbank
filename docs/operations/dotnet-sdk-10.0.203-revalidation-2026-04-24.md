# Pin real SDK .NET 10.0.203 — revalidación global controlada

Fecha: 2026-04-24

## Cambios versionados

1. `global.json` fijado a `10.0.203` con `rollForward: disable`.
2. `scripts/codex/setup-codex-env.sh` actualizado para instalar/usar `10.0.203`.
3. `src/Cfa.ACHInterbank.Api/Dockerfile` actualizado a `mcr.microsoft.com/dotnet/sdk:10.0.203`.

## Evidencia de entorno (ejecución real)

```bash
bash scripts/codex/setup-codex-env.sh
export DOTNET_ROOT=$HOME/.dotnet
export PATH=$HOME/.dotnet:$HOME/.dotnet/tools:$PATH

dotnet --info
dotnet --list-sdks
dotnet ef --version
```

Resultados:

- SDK activo: `10.0.203`
- SDKs instalados: `10.0.203`
- dotnet-ef: `10.0.7`

## Compilación

```bash
dotnet restore ACHInterbank.sln
dotnet build ACHInterbank.sln -c Release
```

Resultado: compilación exitosa (warnings preexistentes de nullability).

## Suites críticas ejecutadas

```bash
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release \
  --filter "FullyQualifiedName~IncomingNacha|FullyQualifiedName~CommandCenter|FullyQualifiedName~StateMachine|FullyQualifiedName~Observability|FullyQualifiedName~Resilience" -v minimal
```
Resultado: `63/63`.

```bash
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release \
  --filter "FullyQualifiedName~Nacha|FullyQualifiedName~Mapping|FullyQualifiedName~BatchNumber" -v minimal
```
Resultado: `193/193`.

```bash
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release \
  --filter "FullyQualifiedName~DigitalEnvelope|FullyQualifiedName~Signature|FullyQualifiedName~OpenEnvelope" -v minimal
```
Resultado: `32/32`.

```bash
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release \
  --filter "FullyQualifiedName~Certificate|FullyQualifiedName~SecretRef|FullyQualifiedName~OpenBao|FullyQualifiedName~Vault" -v minimal
```
Resultado: `35/35`.

## Validaciones complementarias

```bash
cd web/ach-interbank-ui
npm ci
npm run build
npm test -- --watch=false --browsers=ChromeHeadless
```

- `npm ci`: OK.
- `npm run build`: OK.
- `npm test`: falla por entorno (sin `CHROME_BIN` y error runtime Karma/rimraf).

```bash
rg -n "workflow_dispatch|push:|pull_request|schedule|workflow_run" .github/workflows/postgres-integration-tests.yml -S
```

Resultado: workflow se mantiene manual-only (`workflow_dispatch` + guard por evento).
