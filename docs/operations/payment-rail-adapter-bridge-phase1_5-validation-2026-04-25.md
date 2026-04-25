# Validación técnica — PaymentRail Adapter Bridge Fase 1.5 (2026-04-25)

## Alcance

Implementación de bridge no invasivo para resolver y transportar `RailCode` en paralelo al flujo legacy ACH/CENIT, sin mover lógica operacional.

## Cambios aplicados

1. Nuevo servicio reutilizable de contexto de riel:
   - `IPaymentRailContextService`
   - `PaymentRailContextService`
   - modelos `PaymentRailResolvedContext` y `PaymentRailShadowCompareSnapshot`

2. Integración de bridge en `RoutingStrategyService`:
   - Resuelve `RailCode` desde `ClearingHouseId/Code` en paralelo.
   - Emite traza técnica `PAYMENT_RAIL_RESOLVED`.
   - Mantiene retorno legacy (`AchCycle.Id`) sin cambios de decisión.
   - Incluye fallback `NullPaymentRailContextService` para modo pasivo/fail-safe.

3. Registro DI:
   - `IPaymentRailContextService` registrado en `AddApplication`.

4. Pruebas:
   - Nuevos tests de `PaymentRailContextService`.
   - Test de no-regresión de `RoutingStrategyService` con bridge activado.
   - Ajuste de pruebas de routing a fecha fija hábil (`2026-04-22`) para evitar flakiness de fin de semana.

## Pruebas ejecutadas

```bash
dotnet build ACHInterbank.sln -c Release
```

- Build: OK (sin errores; warnings legacy preexistentes).

```bash
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --filter "FullyQualifiedName~PaymentRail|FullyQualifiedName~RoutingStrategyServiceTests|FullyQualifiedName~IncomingNachaDispatchPlannerTests|FullyQualifiedName~IncomingNachaCycleResolverTests|FullyQualifiedName~IncomingNachaCommandCenterServiceTests"
```

- Passed: 22
- Failed: 0

```bash
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --filter "FullyQualifiedName~DependencyInjection"
```

- Passed: 2
- Failed: 0

## Verificación de no invasión

- No se movió lógica de ciclo/dispatch/returns/netting/liquidez.
- No se tocaron parser NACHA, command center, state machine, resiliencia ni observabilidad funcional.
- No se hicieron cambios en criptografía ni esquema de base de datos.
- El bridge agrega instrumentación/contexto paralelo para fases futuras (incluyendo shadow compare).
