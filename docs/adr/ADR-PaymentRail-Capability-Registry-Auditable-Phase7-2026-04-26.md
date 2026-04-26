# ADR — Capability Registry auditable por riel (Prompt 7)

- Fecha: 2026-04-26
- Estado: Aprobado
- Relación: extiende Prompt 1..6B

## Contexto

La plataforma multi-riel ya resuelve `RailCode`, expone wrappers pasivos y ejecuta shadow compare en capacidades críticas. Faltaba una capa auditable para gobernar estado efectivo de capacidades por riel sin alterar ownership legacy.

## Decisión

1. Crear registro auditable `PaymentRailCapabilityRegistry` con estado por capacidad y vigencia temporal.
2. Exponer servicio `IPaymentRailCapabilityRegistryService` para:
   - consultar capacidades efectivas por riel (`GetEffectiveCapabilitiesAsync`);
   - registrar cambios auditable/manual-only (`UpsertCapabilityAsync`).
3. Mantener prioridad de fuentes:
   - `RegistryOverride` (si existe entrada activa);
   - `StrategyDefault` (si no hay override).
4. Mantener comportamiento operacional sin cutover:
   - legacy sigue decidiendo;
   - PaymentRail sigue en paralelo/shadow;
   - registry no reemplaza owners funcionales en esta fase.

## Estados soportados

- Enabled
- Disabled
- ShadowOnly
- NotSupported
- Planned

## Guardrails

- Sin cambios criptográficos.
- Sin migración de source of truth.
- Sin cutover.
- Workflow manual-only para cambios del registry.
