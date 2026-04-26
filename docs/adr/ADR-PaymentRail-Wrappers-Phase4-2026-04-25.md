# ADR — PaymentRail ACH/CENIT Wrappers pasivos (Prompt 4)

- Fecha: 2026-04-25
- Estado: Aprobado
- Relación: extiende Fase 1 (contratos) y Fase 1.5 (adapter bridge)

## Contexto

La plataforma ya resuelve `RailCode` en paralelo, pero las estrategias ACH/CENIT aún eran stubs básicos.
Se requiere elevarlas a wrappers operacionales pasivos con capacidades explícitas y soporte de shadow compare, sin cambiar el owner funcional legacy.

## Decisión

1. Se amplía `IPaymentRailOperationalStrategy` para incluir:
   - `CapabilityStatuses` por capacidad;
   - `EvaluateCapabilityWrapper(...)`;
   - `BuildCapabilityShadowSnapshot(...)`.

2. Se introducen modelos wrapper:
   - `PaymentRailCapabilityKind`
   - `PaymentRailCapabilityExecutionMode`
   - `PaymentRailCapabilityStatus`
   - `PaymentRailWrapperCallRequest`
   - `PaymentRailWrapperCallResult`

3. `AchColombiaPaymentRailOperationalStrategy` y `CenitPaymentRailOperationalStrategy` pasan a wrappers pasivos reales:
   - exponen capacidades precisas por riel;
   - devuelven decisión `PAYMENT_RAIL_WRAPPER_PASSIVE`;
   - preservan `UseLegacyDecision=true` y `BehaviorChanged=false`.

4. `UnknownPaymentRailOperationalStrategy` mantiene fail-closed de wrapper:
   - `PAYMENT_RAIL_WRAPPER_UNKNOWN_FAIL_CLOSED`.

5. `PaymentRailContextService` y `PaymentRailResolvedContext` ahora transportan `CapabilityStatuses` para consumo interno y fases de shadow compare.

## Guardrails preservados

- Cycle/dispatch/returns/netting/liquidity siguen siendo legacy owner.
- No se movió lógica funcional en parser NACHA, command center, state machine, resiliencia u observabilidad funcional.
- Sin cambios DB/migraciones.
- Sin cambios criptográficos.

## Validación

- Tests de wrappers/capabilities (ACH/CENIT/UNKNOWN).
- Tests existentes de contexto/resolver/routing siguen validados para no-regresión.
