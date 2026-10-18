# START HERE — TPA-GOV-001

Este paquete contiene el proyecto completo ACHInterbank R10 y la documentación corregida para implementar incrementalmente terceros/cuentas/validaciones.

## Decisión que debes recordar antes de tocar código

**El onboarding de tercero y cuenta es genérico e independiente de ACH Colombia/CENIT.** La cámara sólo participa posteriormente en elegibilidad y validación.

## Lectura inicial

1. `AGENTS.md`
2. entradas `TPA-*` de `MEMORY.md`
3. `docs/ai/THIRD_PARTY_ACCOUNT_GOVERNANCE_STATE.md`
4. `docs/architecture/THIRD_PARTY_ACCOUNT_GOVERNANCE_IMPLEMENTATION_PLAN.md`
5. `docs/architecture/THIRD_PARTY_ACCOUNT_GOVERNANCE_ARCHITECTURE.md`
6. `docs/architecture/THIRD_PARTY_ACCOUNT_SILENT_ONBOARDING_CONTRACT.md`
7. `docs/normativa/THIRD_PARTY_ACCOUNT_NORMATIVE_TRACEABILITY.md`
8. `docs/testing/THIRD_PARTY_ACCOUNT_TEST_STRATEGY.md`

## Primer chat

Pega `docs/ai/third-party-accounts/MASTER_PROMPT_CHATGPT.md` y ejecuta **TPA0 solamente**.

Una sola fase por chat. No avanzar con gates rojos, contradicción normativa o evidencia obligatoria ausente.
