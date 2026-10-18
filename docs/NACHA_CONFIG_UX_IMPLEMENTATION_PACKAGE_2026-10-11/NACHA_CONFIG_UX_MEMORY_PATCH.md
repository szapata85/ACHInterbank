# Patch sugerido para `MEMORY.md` — NACHA-CONFIG-UX-001

> No aplicar automáticamente si `MEMORY.md` tiene cambios concurrentes. Revisar diff y EOL según `AGENTS.md`.

## NACHA configuration operational UX program — 2026-10-11

- DEC-NACHA-UX-001: Program `NACHA-CONFIG-UX-001` is PLANNED. Its scope is the operational UX/read-command surface for NACHA-M configuration; it must not rewrite official NACHA generation semantics, resolver authority, PUBLISH snapshots, chamber rules, golden files, or normative behavior.
- DEC-NACHA-UX-002: The SPA implementation must use Angular Material 21 + Reactive Forms and inherit runtime branding through the existing `BrandingService`/CSS/Material token bridge. Feature-level brand colors, logos, institution names, or a second theming system are prohibited.
- DEC-NACHA-UX-003: User-visible operational language is Spanish-first with no functional Spanglish. Technical codes/paths/JSON/DSL belong only in an advanced diagnostic section.
- DEC-NACHA-UX-004: Angular must not be the authority for chamber/flow/direction/service/status/record/source/rule/field labels. Existing Cat*/Cfg* data and server read models are the primary source; the current frontend presentation maps are transitional debt and must not grow.
- DEC-NACHA-UX-005: The target UX is one contextual workspace: chamber -> operation/flow -> direction -> service -> file structure -> record -> field -> source/rules -> validate/preview -> changes -> publish -> history. Published profiles remain immutable and evolve through draft/clone/publication.
- DEC-NACHA-UX-006: No new schema is approved initially. UX0/UX1 must first exploit existing catalog/config/history tables. If a safe humanized source-option catalog cannot be derived, implementation must stop and record a schema proposal instead of hardcoding Angular options.
- DEC-NACHA-UX-007: Any edit of `CfgFieldSourceDefinition` must protect shared references; if a changed source is referenced by multiple layout fields, the command path must clone/rebind the source for the selected field rather than mutate unrelated fields.
- DEC-NACHA-UX-008: Preview must not duplicate NACHA rendering logic in TypeScript. Structural preview + server validation are always allowed; byte/content preview may reuse or extract a pure server renderer only with behavior-preserving evidence.
- GAP-NACHA-UX-001: Current frontend still contains functional hardcoded mappings in `nacha-config-presentation.catalog.ts`; service catalog filtering is not chamber-dependent; field editing exposes only a subset of persisted configuration; history lacks humanized before/after; resolver preview is not a full operational preview; existing evidence includes a 404 detail-route screenshot that requires route regression coverage.
- NEXT-NACHA-UX-001: `NEXT_ALLOWED_PHASE=UX0`. Execute one phase at a time using `NACHA_CONFIG_UX_START_HERE.md`, `NACHA_CONFIG_UX_STATE.md`, `NACHA_CONFIG_UX_ARCHITECTURE.md`, and `NACHA_CONFIG_UX_IMPLEMENTATION_PLAN.md`. Do not advance without phase-specific executable evidence.
