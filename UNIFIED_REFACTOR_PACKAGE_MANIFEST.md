# ACHInterbank unified refactor package manifest

Package purpose: provide one upload containing the complete ACHInterbank v124 source package plus the planning/state-memory material for `REFACTOR-CA-SOLID-RES-001`.

## Base project

- Source archive: `ACHInterbank-v124-DOTNET-10.0.12-SDK-10.0.401-SECURE-DEPS-2026-10-09.zip`
- Base code/config/tests/docs are preserved from that archive.
- Root `AGENTS.md` is unchanged.
- Root `MEMORY.md` differs only by the append-only initial durable block for `REFACTOR-CA-SOLID-RES-001`.

## Added refactor documentation

Canonical execution files:

- `docs/architecture/REFACTOR_CA_SOLID_RESILIENCE_IMPLEMENTATION_PLAN.md`
- `docs/ai/REFACTOR_CA_SOLID_RESILIENCE_STATE.md`
- `docs/ai/REFACTOR_CA_SOLID_RESILIENCE_PHASE_PROMPTS.md`
- `docs/ai/refactor/MASTER_PROMPT_CHATGPT.md`
- `docs/ai/refactor/START_HERE.md`

Source-package copies retained under `docs/ai/refactor/`:

- `PLAN_IMPLEMENTACION_REFACTOR_CA_SOLID_RESILIENCIA.md`
- `MEMORY_REFACTOR_STATE.md`
- `MEMORY_PATCH_INITIAL.md`
- `MEMORY_WITH_REFACTOR_INITIALIZED.md`
- `PHASE_PROMPTS.md`

## Initial execution state

- Program: `REFACTOR-CA-SOLID-RES-001`
- State: `PLANNED`
- Current phase: `R0`
- Next allowed phase: `R0`
- No functional product refactor has been applied by packaging.
