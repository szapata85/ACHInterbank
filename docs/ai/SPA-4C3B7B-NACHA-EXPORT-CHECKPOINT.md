# SPA 4C.3B7b — NACHA Export Foundation Checkpoint

## Baseline

`ACHInterbank-v51-spa-4C3B7a2-ach-cycle-form.zip`

## Scope

This lot migrates only `NachaExportComponent` to the 4C.1 visual foundation.

Out of scope and unchanged:

- ACH cycle list and new/edit form from B7a;
- `NachaExportApiService`;
- `BlobDownloadService`;
- ACH cycle export models/contracts;
- routing/permissions;
- backend;
- B9 boundary/re-export cleanup;
- B8 and later work.

The B7a2 and foundation 4C.1 checkpoints were read before implementation.

## Functional behavior preserved

`nacha-export.component.ts` is byte-for-byte unchanged from the v51 baseline.

The existing behavior therefore remains unchanged, including:

- `getExportableCycles()` for backend filtering;
- `downloadFile(cycleId, false)` for plain NACHA-M;
- `downloadFile(cycleId, true)` for the digital envelope;
- `BlobDownloadService.save()`;
- `presentNachaError()`;
- explicit-only `retryOperation()`;
- the per-cycle `operations` `Map` double-operation guard;
- `load()` after a successful download;
- no automatic retry.

## Filters preserved

Unchanged:

- `clearingHouseId`;
- `startDate`;
- `endDate`;
- local `search` filtering;
- `dateRangeValidator()`;
- `clearFilters()`;
- form disable/enable during `load()`.

Search remains local. Chamber/date filters remain backend filters.

## Functional export states preserved

The four existing states remain represented with `MatChip` and the same `statusLabel()` / `statusClass()` behavior:

- `Disponible`;
- `Generado`;
- `Protegido`;
- `Bloqueado`.

`actionUnavailableReason()` is unchanged.

The chip CSS now uses existing foundation semantic/design tokens instead of local hardcoded colors. `UiEtiquetaEstadoComponent` was deliberately not introduced because it would collapse the existing four-state semantics.

## Operational errors and manual retry preserved

`app-operational-error-panel` remains unchanged and still presents the current `ApplicationDownloadError` / `OperationalErrorView` output, including:

- `errorCode`;
- `traceId`;
- the existing operational detail;
- the `NACHA_FIELD_RULE_FAILED` presentation from `presentNachaError()`.

No additional technical detail is exposed. Retry remains user-triggered only through `retryOperation()`; no automatic retry was added.

## Foundation 4C.1 adoption

Only local presentation was changed:

- primary loading state -> `UiEstadoCarga`;
- primary load error -> `UiEstadoError`, preserving explicit retry through `load()`;
- empty state -> `UiEstadoVacio`, retaining the existing `Limpiar filtros` Material action;
- hardcoded colors for all four status chips -> foundation tokens;
- the mobile unavailable reason hardcoded error color -> `--color-error-text`;
- obsolete local `.state-container` styles removed.

Preserved unchanged:

- `app-page-header`;
- breadcrumb;
- filter cards;
- operational summary;
- `MatTable` desktop view;
- mobile cards/list;
- per-row Material spinner during export;
- `MatChip` status semantics.

No new UI primitive was created. `MatProgressSpinnerModule` remains required because per-row export spinners are still used.

## Boundary debt deliberately preserved

The existing re-exports remain unchanged for B9:

- `ClearingHousesApiService`;
- `ClearingHouseOption`.

No new feature-to-feature import was introduced.

## Focused tests updated

Only `nacha-export.component.spec.ts` was updated.

Coverage now explicitly includes/preserves:

- initial load;
- date-range validation;
- backend chamber/date filters;
- clear filters;
- shared loading state;
- shared load-error state;
- shared empty state;
- plain NACHA-M export with `encrypted = false`;
- digital-envelope export with `encrypted = true`;
- required `cycleId`;
- block when `isExportable` is false;
- per-cycle double-operation guard;
- `errorCode` + `traceId` operational presentation;
- `NACHA_FIELD_RULE_FAILED` humanized presentation;
- explicit/manual retry with no automatic second request;
- row progress cleanup after error.

## Executable validation actually attempted

Observed environment:

- Node: `v22.16.0`;
- npm: `10.9.2`;
- `web/ach-interbank-ui/node_modules/.bin/ng`: absent.

Exactly one short registry check was run:

- command: `timeout 12s npm ping`;
- exit code: `124`;
- output reached: `npm notice PING https://registry.npmjs.org/`.

No `npm ci` was run or retried.

Focused test was actually attempted:

- command: `npm test -- --watch=false --browsers=ChromeHeadless --include=src/app/features/ach-cycles/components/nacha-export.component.spec.ts`;
- exit code: `127`;
- result: `ng: not found`.

SPA build was actually attempted:

- command: `npm run build`;
- exit code: `127`;
- result: `ng: not found`.

Global TypeScript `5.8.3` `transpileModule` static syntax check reported:

- `nacha-export.component.ts`: `0` diagnostics;
- `nacha-export.component.spec.ts`: `0` diagnostics.

No backend executable validation was run because backend is out of scope and statically unchanged.

## Static validation

PASS.

Before this checkpoint was added, comparison against v51 showed exactly three changed files and no added/deleted functional files:

- `nacha-export.component.html`;
- `nacha-export.component.scss`;
- `nacha-export.component.spec.ts`.

Confirmed byte-for-byte unchanged against v51:

- `nacha-export.component.ts` (all functional TypeScript unchanged);
- all 8 B7a list/form component files;
- all 5 `ach-cycles` service/model files, including `NachaExportApiService` and export models;
- all 29 frontend routing TS files;
- all 1474 backend files under repository `src/`.

Additional static checks:

- zero new feature-to-feature imports;
- zero hardcoded hex/rgb values in NACHA export TS/HTML/SCSS;
- zero legacy local `state-container` usage;
- `UiEstadoCarga`, `UiEstadoError`, and `UiEstadoVacio` present;
- `app-operational-error-panel`, `MatTable`, `MatChip`, breadcrumb, summary, mobile layout, and row spinners retained.

## Final status

`NACHA_EXPORT_4C3B7B_IMPLEMENTED_VALIDATION_PENDING`

Implementation and static validation are complete. Angular executable validation remains pending because `ng` is unavailable and the single registry check timed out. No VERIFIED/UAT/release claim is made.
