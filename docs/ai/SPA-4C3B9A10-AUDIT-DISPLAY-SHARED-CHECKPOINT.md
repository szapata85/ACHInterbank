# SPA 4C.3B9a10 — Audit Display Shared checkpoint

Baseline: `ACHInterbank-v63-spa-4C3B9a9-catalogs-test-coupling-retirement.zip`

## Scope

This micro-batch moves only the cross-feature audit display utility from `features/audit-shared` to `shared/utils` and updates its two real consumers. No functional logic, backend, routing, services, models, or other B9 boundary is changed.

## Consumers confirmed before change

Repository search identified exactly two productive consumers of `audit-display.util.ts`:

- `web/ach-interbank-ui/src/app/features/audit/components/audit-log.component.ts`
- `web/ach-interbank-ui/src/app/features/auth-logs/components/auth-log.component.ts`

No spec imported the utility directly, so no spec was modified.

## Move performed

Moved byte-for-byte:

- from `web/ach-interbank-ui/src/app/features/audit-shared/audit-display.util.ts`
- to `web/ach-interbank-ui/src/app/shared/utils/audit-display.util.ts`

The moved file has the same SHA-256 as the baseline source:

`ce7d51fee229cf4e4c403dae33d11e1d62ae71359a6eb0242213d914004e7a9f`

The now-empty `features/audit-shared` directory was removed. No legacy reexport was added.

## Logic preserved

The following logic is byte-for-byte unchanged in the moved utility:

- `auditDateRangeValidator`
- `toLocalDateParam`
- `sanitizeAuditText`
- `summarizeChangedFields`
- secret assignment regex
- bearer/JWT redaction
- sensitive-field detection
- fallback texts
- maximum-length behavior
- date handling

For both consumers, a baseline-byte comparison with only the import path substituted matches the final file exactly. Therefore their functional bodies are unchanged.

## Static validation

Pre-checkpoint SHA-256 manifest comparison against the extracted v63 baseline:

- baseline files: **3603**
- current files: **3603**
- modified baseline files: **2**
  - `audit-log.component.ts` — import path only
  - `auth-log.component.ts` — import path only
- added files: **1**
  - `shared/utils/audit-display.util.ts`
- removed files: **1**
  - `features/audit-shared/audit-display.util.ts`
- Angular specs modified: **0**
- backend `src/` files: **1474**, changed: **0**

Additional checks:

- source references to `features/audit-shared`: **0**
- copies of `audit-display.util.ts`: **1**
- imports from `shared` to any `features/*`: **0**
- new feature-to-feature imports introduced: **0**
- B9a1–B9a9 checkpoint files: **byte-for-byte intact**

Because the complete baseline manifest differs only in the two expected import-only consumers plus the single file move, B9a1–B9a9 implementation artifacts and all unrelated source files remain intact.

## Executable validation

Per this micro-batch instruction, only Angular CLI availability was checked:

```text
ng: not found
```

`web/ach-interbank-ui/node_modules/.bin/ng` is absent.

Not executed by instruction:

- `npm ping`
- `npm ci`
- Angular tests
- Angular build
- backend build/tests

## Final state

`AUDIT_DISPLAY_SHARED_4C3B9A10_IMPLEMENTED_VALIDATION_PENDING`
