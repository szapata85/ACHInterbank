# Validación técnica — Prompt 7 Capability Registry auditable por riel (2026-04-26)

## Alcance

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
dotnet build ACHInterbank.sln -c Release
```
- Build OK.

```bash
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --filter "FullyQualifiedName~PaymentRailCapabilityRegistryServiceTests"
```
- Passed: 2
- Failed: 0

## Conclusión

Prompt 7 implementado en modo gobernanza:

- legacy sigue decidiendo;
- PaymentRail continúa en paralelo/shadow;
- capability registry es auditable y consultable;
- sin cutover.
