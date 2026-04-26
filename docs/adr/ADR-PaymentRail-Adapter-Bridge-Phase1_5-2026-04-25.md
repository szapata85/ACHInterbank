# ADR — PaymentRail Adapter Bridge Fase 1.5 (instrumentación no invasiva)

- Fecha: 2026-04-25
- Estado: Aprobado
- Relación: extiende `ADR-PaymentRail-Operational-Strategy-Base-2026-04-25.md`

## Contexto

La Fase 1 dejó contratos, mapper y resolver multi-riel, pero aún no se usaban dentro de un flujo legacy operativo.  
Se requiere iniciar adopción incremental sin modificar decisiones funcionales ACH/CENIT.

## Decisión

Se introduce un **Adapter Bridge no invasivo** con resolución paralela de `RailCode`:

1. Nuevo servicio reutilizable `IPaymentRailContextService` / `PaymentRailContextService` para:
   - resolver contexto de riel desde `ClearingHouseId/Code`;
   - construir snapshot de shadow-compare (legacy vs rail).

2. Nuevo modelo de puente:
   - `PaymentRailResolvedContext`
   - `PaymentRailShadowCompareSnapshot`

3. Integración segura en `RoutingStrategyService`:
   - mantiene la selección legacy de ciclo exactamente igual;
   - resuelve `RailCode` en paralelo;
   - emite traza técnica `PAYMENT_RAIL_RESOLVED` para auditoría/telemetría;
   - fallback a `NullPaymentRailContextService` para fail-safe no disruptivo.

4. DI:
   - registro de `IPaymentRailContextService` en `AddApplication`.

## Consecuencias

### Positivas

- `RailCode` queda transportable en contexto interno sin romper comportamiento actual.
- Se habilita base para shadow compare en fases de migración funcional.
- Se agrega visibilidad técnica del rail resuelto.

### Guardrails preservados

- No se movió lógica de ciclo/dispatch/returns/netting/liquidez.
- No se modificaron parser NACHA, command center, state machine, resiliencia u observabilidad funcional.
- No cambios DB/migraciones.
- No cambios criptográficos.

## Validación

- Tests nuevos de `PaymentRailContextService`.
- Test de no-regresión en `RoutingStrategyService` verificando que la decisión legacy de ciclo no cambia al activar bridge.
