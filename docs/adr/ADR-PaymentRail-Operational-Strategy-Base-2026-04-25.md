# ADR — Base contractual multi-riel (Fase 1 bridge)

- Fecha: 2026-04-25
- Estado: Aprobado
- Alcance: Contratos base y resolver multi-riel sin mover lógica operacional actual.

## Contexto

ACHInterbank opera hoy ACH Colombia y CENIT de forma productiva, con múltiples decisiones basadas en `ClearingHouseId` y `ClearingHouse.Code`.  
El objetivo de Fase 1 es preparar arquitectura multi-riel sin tocar lógica crítica vigente (ciclos, dispatch, returns, netting/liquidez, parser, command center, state machine, observabilidad).

## Decisión

Se introducen contratos y bridge no invasivo para estrategia operacional por riel:

1. Modelo base de riel:
   - `PaymentRailCodes` (`ACH_COLOMBIA`, `CENIT`, `UNKNOWN`)
   - `PaymentRailCapabilityDescriptor`
   - `PaymentRailOperationalContext`
   - requests/results de resolución y bridge.

2. Contratos:
   - `IPaymentRailOperationalStrategy`
   - `IPaymentRailOperationalStrategyResolver`
   - `IClearingHouseToPaymentRailMapper`

3. Resolver + mapping explícito:
   - Mapping fijo `ClearingHouseId -> RailCode` (1->ACH_COLOMBIA, 2->CENIT).
   - Mapping fijo de códigos (`ACH`, `ACHCOL`, `ACH COLOMBIA`, `CENIT`).
   - Si hay conflicto entre id/código, aplica fail-closed (`UNKNOWN`).

4. Estrategias de Fase 1:
   - `AchColombiaPaymentRailOperationalStrategy` (bridge no-op).
   - `CenitPaymentRailOperationalStrategy` (bridge no-op).
   - `UnknownPaymentRailOperationalStrategy` (fail-closed).

5. Registro DI:
   - Se registran mapper, estrategias y resolver en `AddApplication`.

## Consecuencias

### Positivas

- Se crea superficie contractual para evolución multi-riel sin romper ACH/CENIT.
- Se reduce riesgo de expansión con condicionales dispersos por cámara.
- Se habilita entrada controlada de rieles futuros (SWIFT/BRE-B) vía estrategia dedicada.

### Trade-offs

- En Fase 1 no hay cambio funcional; sólo estructura de contratos y resolución.
- Persisten temporalmente decisiones legacy en servicios existentes hasta fases posteriores.

## Guardrails

- No se modificó lógica de:
  - `IncomingNachaCycleResolver`
  - `IncomingNachaDispatchPlanner`
  - `IncomingNachaPostProcessingOrchestrator`
  - `AchReturnsService`
  - `CenitCycleExecutionService`
  - `LiquidityOptimizationService`
- No se crean migraciones EF ni cambios de esquema productivo.

## Plan siguiente

Fase 2: implementar estrategias operacionales ACH/CENIT con wrappers sobre servicios existentes, manteniendo shadow compare y sin cortar fallback legacy.
