# Revalidación técnica — Prompt 7B Capability Registry (2026-04-26)

## Contexto de cierre

Se ejecuta revalidación 7B para cerrar la contradicción entre un reporte textual previo de entorno sin `dotnet` y la evidencia documental de Prompt 7A.

Esta revalidación **no introduce features** y solo verifica el estado actual del repositorio en la rama de trabajo.

## Setup ejecutado

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

### Resultado

- SDK detectado: `10.0.203`.
- `dotnet-ef` detectado: `10.0.7`.
- `global.json` fijo en `10.0.203` con `rollForward: disable`.
- `git status --short` sin cambios locales antes de revalidación.

## Revalidación de build y migraciones

```bash
dotnet restore ACHInterbank.sln
dotnet build ACHInterbank.sln -c Release
```

- Restore OK.
- Build OK (0 errores, 9 warnings preexistentes de nulabilidad).

```bash
dotnet ef migrations list \
  --project src/Cfa.ACHInterbank.Persistence/Cfa.ACHInterbank.Persistence.csproj \
  --startup-project src/Cfa.ACHInterbank.Api/Cfa.ACHInterbank.Api.csproj \
  --context AchDbContext
```

- Migrations list ejecutado correctamente.
- Se confirma presencia de:
  - `20260420215632_AddExternalFileNamePolicyPhase1`
  - `20260421183417_AddCertificateManagementDigitalEnvelope`
  - `20260422112419_AddNachaSecurityOperations`
  - `20260426025056_AddPaymentRailCapabilityRegistryPhase7`
- Sin PostgreSQL local (`localhost:5432`) no se pudo determinar estado aplicado/pending; listado de migraciones sí validado.

## Revalidación de pruebas (mismos filtros reportados en 7A)

```bash
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --filter "FullyQualifiedName~PaymentRailCapabilityRegistryServiceTests"
```
- Passed: 2
- Failed: 0

```bash
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --filter "FullyQualifiedName~PaymentRail|FullyQualifiedName~RoutingStrategyServiceTests"
```
- Passed: 17
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

## Conclusión de auditoría

- La contradicción queda cerrada: en esta ejecución 7B el entorno sí tiene .NET 10 y permite reproducir build/tests.
- Se confirma que el estado final del repo mantiene el alcance de gobernanza/shadow sin cutover.
- No se introdujeron cambios operacionales ACH/CENIT, criptografía ni automatismos de workflow.
