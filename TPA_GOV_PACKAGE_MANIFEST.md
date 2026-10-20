# TPA-GOV-001 package manifest — Design Revision R2

## Base preservada

- Proyecto completo basado en `ACHInterbank-R10-NACHA-CONFIG-UX-FINAL.zip`.
- Esta revisión es documental/pre-implementación.
- No agrega migraciones ni cambios funcionales del programa TPA.

## Corrección arquitectónica R2

El diseño inicial de planificación incluía `ThirdPartyAccountEnrollment`. Antes de iniciar TPA0 se corrigió:

- onboarding/master data = `Customer -> ThirdParty -> ThirdPartyAccount`, genérico y agnóstico de cámara;
- `ClearingHouseId`, naturaleza, servicio, ciclos y prenotificación pertenecen a `ThirdPartyAccountValidation`/`PrenotificationValidationAttempt`;
- elegibilidad es derivada y contextual;
- una cuenta descubierta por una cámara se reutiliza en cualquier otra sin duplicar master.

## Documentación canónica

- `docs/architecture/THIRD_PARTY_ACCOUNT_GOVERNANCE_ARCHITECTURE.md`
- `docs/architecture/THIRD_PARTY_ACCOUNT_SILENT_ONBOARDING_CONTRACT.md`
- `docs/architecture/THIRD_PARTY_ACCOUNT_GOVERNANCE_IMPLEMENTATION_PLAN.md`
- `docs/adr/ADR-TPA-001-SILENT-ONBOARDING-AS-INVARIANT.md`
- `docs/adr/ADR-TPA-002-GENERIC-MASTER-CONTEXTUAL-VALIDATION.md`
- `docs/adr/ADR-TPA-003-USE-EXISTING-CLEARING-HOUSE-RULE-AUTHORITY.md`
- `docs/adr/ADR-TPA-004-BRANCH-BY-ABSTRACTION-LEGACY-CUSTOMERTHIRDPARTY.md`
- `docs/adr/ADR-TPA-005-AUTOMATIC-INTERBANK-RESULTS.md`
- `docs/adr/ADR-TPA-006-ELIGIBILITY-IS-DERIVED.md`
- `docs/normativa/THIRD_PARTY_ACCOUNT_NORMATIVE_TRACEABILITY.md`
- `docs/data/THIRD_PARTY_ACCOUNT_DATA_MODEL_MIGRATION.md`
- `docs/api/THIRD_PARTY_ACCOUNT_API_CONTRACT.md`
- `docs/ux/THIRD_PARTY_ACCOUNT_UX_SPEC.md`
- `docs/security/THIRD_PARTY_ACCOUNT_SECURITY_AND_PRIVACY.md`
- `docs/operations/THIRD_PARTY_ACCOUNT_OPERATIONS_RUNBOOK.md`
- `docs/testing/THIRD_PARTY_ACCOUNT_TEST_STRATEGY.md`
- `docs/ai/THIRD_PARTY_ACCOUNT_GOVERNANCE_STATE.md`
- `docs/ai/THIRD_PARTY_ACCOUNT_GOVERNANCE_PHASE_PROMPTS.md`
- `docs/ai/third-party-accounts/MASTER_PROMPT_CHATGPT.md`
- `docs/ai/third-party-accounts/START_HERE.md`
- `docs/ai/third-party-accounts/TPA0_BASELINE_INVENTORY.json`

## Root integration

- `AGENTS.md`: guardrail explícito de onboarding agnóstico de cámara.
- `MEMORY.md`: decisiones TPA corregidas antes de implementación.
- `README.md`: entrada al programa.
- `docs/DOCUMENTATION_INDEX.md`: índice canónico.
- `PROMPT_TPA0_CHATGPT.txt` y `TPA_GOV_MASTER_PROMPT_CHATGPT.md`: copia del prompt inicial.

## Estado inicial

- `program_status=PLANNED`
- `design_revision=TPA-DESIGN-R2`
- `active_phase=TPA0`
- `next_allowed_phase=TPA0`
- no se ha ejecutado TPA0/TPA1.

## Validación de empaquetado R2

- `python scripts/validate-solution-projects.py`: `SOLUTION GRAPH: OK`.
- árboles `src/**`, `tests/**` y `web/ach-interbank-ui/src/**`: verificados sin cambios respecto al snapshot tomado antes de esta corrección documental.
- `dotnet` y Docker no están disponibles en este host; no se declara build/test runtime PASS.
- no se ejecutó TPA0; `NEXT_ALLOWED_PHASE` continúa en `TPA0`.
