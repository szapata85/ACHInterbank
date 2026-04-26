# ADR — Shadow Compare pasivo en Returns, Return-of-Return, Netting y Liquidity (Prompt 6)

- Fecha: 2026-04-26
- Estado: Aprobado
- Relación: extiende Prompt 5/5A

## Contexto

La transición multi-riel ya tiene shadow compare pasivo en cycle/dispatch. Faltaba cubrir capacidades críticas adicionales:

- devoluciones (returns);
- devolución de devolución (return-of-return);
- netting CENIT;
- optimización de liquidez CENIT.

## Decisión

1. Extender `IPaymentRailShadowCompareService` para comparación pasiva de capacidades `Return`, `Netting` y `Liquidity`.
2. Integrar comparación pasiva en:
   - `AchReturnsService` (generación de devoluciones);
   - `ReturnOfReturnOrchestrator` (registro de return-of-return);
   - `CenitNettingService` (cálculo de netting);
   - `LiquidityOptimizationService` (optimización de liquidez).
3. Registrar trazas técnicas:
   - `PAYMENT_RAIL_SHADOW_COMPARE_RETURN`;
   - `PAYMENT_RAIL_SHADOW_COMPARE_RETURN_OF_RETURN`;
   - `PAYMENT_RAIL_SHADOW_COMPARE_NETTING`;
   - `PAYMENT_RAIL_SHADOW_COMPARE_LIQUIDITY`.

## Principio operativo

- Legacy sigue decidiendo.
- PaymentRail sólo compara en paralelo.
- Si shadow falla/no equivalente, la operación legacy no se interrumpe.
- No hay cutover.

## Guardrails preservados

- Sin cambios a criptografía ni envelope.
- Sin cambios parser NACHA/command center/state machine/returns netting functional ownership.
- Sin migraciones DB.
