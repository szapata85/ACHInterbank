# ADR — Shadow Compare pasivo en Cycle + Dispatch (Prompt 5)

- Fecha: 2026-04-25
- Estado: Aprobado
- Relación: extiende wrappers pasivos Prompt 4

## Contexto

La plataforma ya cuenta con bridge y wrappers pasivos, pero faltaba ejecutar comparación real legacy vs PaymentRail en capacidades críticas de entrada:

- resolución de ciclo;
- elegibilidad/planeación de dispatch.

## Decisión

1. Nuevo servicio `IPaymentRailShadowCompareService` / `PaymentRailShadowCompareService` para comparar decisiones legacy vs wrapper.
2. Integración pasiva en `IncomingNachaCycleResolver`:
   - calcula shadow compare en paralelo;
   - registra traza técnica `PAYMENT_RAIL_SHADOW_COMPARE_CYCLE`;
   - incluye resultado en `EvidenceJson`.
3. Integración pasiva en `IncomingNachaDispatchPlanner`:
   - calcula wrapper shadow por capability dispatch;
   - registra traza `PAYMENT_RAIL_SHADOW_COMPARE_DISPATCH`;
   - no altera estados/colas legacy.

## Principio operativo

- Legacy sigue decidiendo.
- Shadow compare no corta operación.
- Si shadow falla/no equivalente, se conserva decisión legacy.
- Sin cutover.

## Guardrails preservados

- Sin cambio de source of truth para ciclo o dispatch.
- Sin cambios en parser NACHA, command center, state machine, returns, netting/liquidity.
- Sin cambios DB/migraciones.
- Sin cambios criptográficos.

## Validación

- Tests PaymentRail + routing + incoming + mapping/nacha + DI en verde.
- Test de resolver de ciclo verifica presencia de `shadowCompare` en evidencia.
