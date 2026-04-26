# Validación técnica — Prompt 7 Capability Registry auditable por riel (2026-04-26)

## Alcance

> Nota: Ver revalidación cruzada en `docs/operations/payment-rail-capability-registry-phase7b-revalidation-2026-04-26.md` para evidencia ejecutada nuevamente en entorno con .NET disponible.

Se implementa registro auditable de capacidades por riel para gobernanza y consulta de estado efectivo, sin cambio de comportamiento operacional.

## Cambios ejecutados

1. Modelo persistente:
   - `PaymentRailCapabilityRegistryEntry`;
   - configuración EF `PaymentRailCapabilityRegistryEntryConfiguration`;
   - `DbSet` en `AchDbContext`.
2. Contratos/modelos Application:
   - `PaymentRailCapabilityRegistryState`;
   - `PaymentRailCapabilityRegistryCodes`;
   - `PaymentRailCapabilityRegistryItem`;
   - `UpsertPaymentRailCapabilityRegistryRequest`;
   - `IPaymentRailCapabilityRegistryService`.
3. Servicio persistence:
   - `PaymentRailCapabilityRegistryService` con consulta efectiva y upsert auditable.
4. Pruebas:
   - `PaymentRailCapabilityRegistryServiceTests`.

## Validación ejecutada

```bash
dotnet ef migrations list \
  --project src/Cfa.ACHInterbank.Persistence/Cfa.ACHInterbank.Persistence.csproj \
  --startup-project src/Cfa.ACHInterbank.Api/Cfa.ACHInterbank.Api.csproj \
  --context AchDbContext
```
- Migrations detectadas:
  - `20260420215632_AddExternalFileNamePolicyPhase1`
  - `20260421183417_AddCertificateManagementDigitalEnvelope`
  - `20260422112419_AddNachaSecurityOperations`
  - `20260426025056_AddPaymentRailCapabilityRegistryPhase7`
- Nota de entorno: sin PostgreSQL local activo no se pudo determinar estado aplicado/pending, pero sí se validó generación/listado de migraciones.

```bash
dotnet build ACHInterbank.sln -c Release
```
- Build OK.

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

## Conclusión

Prompt 7 implementado en modo gobernanza:

- legacy sigue decidiendo;
- PaymentRail continúa en paralelo/shadow;
- capability registry es auditable y consultable;
- sin cutover;
- sin cambios criptográficos;
- workflow manual-only (sin reactivar automatismos).
