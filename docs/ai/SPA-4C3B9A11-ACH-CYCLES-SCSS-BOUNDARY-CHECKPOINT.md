# SPA 4C.3B9a11 — ACH-Cycles SCSS Boundary checkpoint

Baseline: `ACHInterbank-v64-spa-4C3B9a10-audit-display-shared.zip`

## Scope

This micro-batch removes only the SCSS feature-to-feature dependency from `ach-cycles` to `admin` caused by `@use '../../admin/components/users-list.component' as *;` in `ach-cycle-list.component.scss`.

No TypeScript, HTML, backend, routing, services/models, or other B9 boundary is changed.

## Dependency retired

Removed from:

- `web/ach-interbank-ui/src/app/features/ach-cycles/components/ach-cycle-list.component.scss`

The retired dependency was:

```scss
@use '../../admin/components/users-list.component' as *;
```

No `@use`, `@import`, or `@forward` from any `ach-cycles` SCSS file to another feature remains.

## Local styles preserved

Inspection of `users-list.component.scss` showed that the ACH cycle list template actually depended only on the generic filter layout emitted by that stylesheet:

- `.filters` as a wrapping flex row;
- center alignment;
- 1rem gap;
- `mat-form-field` flex basis of 280px;
- mobile `mat-form-field { width: 100%; }` at 768px.

`ach-cycle-list.component.scss` now owns only those minimal rules locally. The previous 1rem gap is represented by the existing foundation token `var(--space-4)`. The component's own effective `min-width: 10rem` remains unchanged.

The existing `.cycle-tabs` styles were not redesigned. They continue using the existing foundation color tokens.

## Preserved behavior and markup

Byte comparison against the v64 baseline confirms these files are unchanged:

- `ach-cycle-list.component.ts`
- `ach-cycle-list.component.html`
- `features/admin/components/users-list.component.scss`

Therefore the following remain unchanged:

- `app-page-header`;
- `cycle-tabs` markup and active navigation;
- filter controls and submission behavior;
- `app-table`;
- create/edit/export navigation and Material buttons;
- responsive template structure.

No hardcoded hex/rgb color was introduced in the target SCSS.

## Static validation

Pre-checkpoint SHA-256 comparison against the extracted v64 baseline:

- baseline files: **3604**;
- modified baseline files: **1**;
  - `web/ach-interbank-ui/src/app/features/ach-cycles/components/ach-cycle-list.component.scss`;
- added files before checkpoint: **0**;
- removed files: **0**;
- `ach-cycle-list.component.ts`: byte-for-byte intact;
- `ach-cycle-list.component.html`: byte-for-byte intact;
- `users-list.component.scss`: byte-for-byte intact;
- backend `src/`: **1474 files**, changed: **0**;
- B9a1–B9a10 checkpoint files: **10**, changed: **0**;
- hardcoded hex/rgb in target SCSS: **0**;
- `@use/@import/@forward` in `features/ach-cycles/**/*.scss`: **0**.

Because every baseline file except the target SCSS is byte-for-byte unchanged, no TypeScript feature dependency can have been introduced. The only dependency change is removal of the existing `ach-cycles -> admin` SCSS edge; new feature-to-feature dependencies: **0**.

## Executable validation

Per this micro-batch instruction, only Angular CLI availability was checked:

```text
ng: not found
```

`web/ach-interbank-ui/node_modules/.bin/ng` is absent.

Not executed by instruction:

- `npm ping`;
- `npm ci`;
- Angular tests;
- Angular build;
- backend build/tests.

## Final state

`ACH_CYCLES_SCSS_BOUNDARY_4C3B9A11_IMPLEMENTED_VALIDATION_PENDING`
