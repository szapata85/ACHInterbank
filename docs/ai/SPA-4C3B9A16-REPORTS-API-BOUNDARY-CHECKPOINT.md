# SPA 4C.3B9a16 — Reports API Boundary Checkpoint

## Scope

Baseline: `ACHInterbank-v69-spa-4C3B9a15-ach-colombia-file-exchange-boundary.zip`

Objective completed: remove the `cenit -> reports` feature boundary caused by `ReportsApiService` and its inline report DTOs, while preserving behavior and leaving backend untouched.

No subsequent B9 boundary was implemented.

## Ownership move

Moved:

- `web/ach-interbank-ui/src/app/features/reports/services/reports-api.service.ts`
  -> `web/ach-interbank-ui/src/app/core/services/reports-api.service.ts`
- `web/ach-interbank-ui/src/app/features/reports/models/accounting-review-export.model.ts`
  -> `web/ach-interbank-ui/src/app/core/models/accounting-review-export.model.ts`

Legacy files were removed after confirming zero references. No wrapper, re-export, or duplicate implementation was left under `features/reports`.

## Consumers updated

Only import paths changed in these consumers/specs:

- `features/cenit/components/cenit-operation-page.component.ts`
- `features/cenit/services/cenit-operations-api.service.ts`
- `features/reports/components/accounting-review-export.component.ts`
- `features/reports/components/reconciliation-report.component.ts`
- `features/reports/components/reconciliation-report.component.spec.ts`
- `features/reports/components/report-list-page.component.ts`
- `features/reports/components/report-list-page.component.spec.ts`
- `features/reports/components/traceability-report.component.ts`
- `features/reports/components/traceability-report.component.spec.ts`

Binary comparison after removing import declarations confirms the non-import body of all nine files is identical to the baseline.

## Service and contracts preserved

`ReportsApiService` moved as one complete unit. Its content is byte-for-byte equal to the baseline service after applying only the required `ApiService` import-path relocation from the former feature location to core.

The move therefore preserves all inline report contracts and all existing methods, including the families:

- `TraceabilityReport*`
- `TransactionMovementReport*`
- `ReturnRejectionReport*`
- `NachaFileReport*`
- `CycleReport*`
- `ReconciliationReport*`
- `AuditReport*`
- `HistoryReport*`

Endpoints, params, payloads, `HttpClient`/`ApiService` behavior, response typing, PDF/export behavior, and report logic are unchanged.

`core/models/accounting-review-export.model.ts` is byte-for-byte identical to the former feature model. `AccountingReviewExportFormat` and `AccountingReviewExportRequest` are unchanged.

Static definition checks:

- `ReportsApiService`: exactly 1 definition, in core.
- `AccountingReviewExportRequest`: exactly 1 definition, in core.
- legacy reports service/model paths: 0 references.
- `cenit -> reports`: 0 TypeScript imports after the change.

## Boundary validation

Cross-feature TypeScript import scan:

- baseline feature->feature imports: 11
- final feature->feature imports before this checkpoint: 9
- removed: exactly the 2 `cenit -> reports` imports
- added: 0
- `core -> features`: 0
- `shared -> features`: 0

B9a1 through B9a15 evidence artifacts: 15 files present, 0 changed.

Backend static comparison:

- `src/`: 1474 files
- changed: 0

Before adding this checkpoint, the complete baseline diff was exactly:

- 2 core files added by move
- 2 legacy reports files removed
- 9 consumers/specs modified only in imports
- no templates, SCSS, routing, backend, or other functional files changed

## Executable validation

Per prompt, executable validation was limited to checking Angular CLI presence:

`web/ach-interbank-ui/node_modules/.bin/ng` -> `ng: not found`

Therefore no npm registry probe, `npm ci`, Angular tests, or build was executed.

## Final state

`REPORTS_API_BOUNDARY_4C3B9A16_IMPLEMENTED_VALIDATION_PENDING`
