# SPA 4C.3B8a — Clearing House Special Dates Checkpoint

## Baseline

`ACHInterbank-v52-spa-4C3B7b-nacha-export.zip`

## Scope

This lot migrates only the canonical special-dates surface at:

- `/clearing-houses/:id/special-dates`
- `ClearingHouseSpecialDatesComponent`

The component remains physically under `features/catalogs` and continues consuming the existing clearing-house context navigation. B9 ownership/boundary cleanup is deliberately deferred.

Out of scope and unchanged:

- other catalogs;
- clearing-house admin/context implementation;
- transaction policies;
- cycle configuration;
- ACH cycles / NACHA export;
- transactions;
- routing;
- services/models/contracts;
- backend.

The B0 inventory, B7b checkpoint and foundation 4C.1 checkpoint were read before implementation.

## Real component inspection

### Route and permissions

The canonical route remains `/clearing-houses/:id/special-dates` in `app-routing.module.ts` and is guarded by `permissionGuard` with the existing permission set:

- `ClearingHouses.View`;
- `ClearingHouses.ManageSpecialDates`.

The component continues reading `:id` from `ActivatedRoute.paramMap` and rejects non-positive/non-integer camera IDs locally with the existing message.

Write actions remain controlled by:

- `ClearingHouses.ManageSpecialDates`.

Context-navigation visibility remains driven by the existing read permissions for policies, cycles and special dates.

### APIs/services actually used

No service was changed.

The screen continues using:

- `ClearingHousesService.get(id)` to load the fixed clearing house;
- `ClearingHouseSpecialDatesService.list(year, clearingHouseId)` -> `GET api/clearing-house-special-dates`;
- `ClearingHouseSpecialDatesService.create(payload)` -> `POST api/clearing-house-special-dates`;
- `ClearingHouseSpecialDatesService.update(payload)` -> `PUT api/clearing-house-special-dates/{id}`;
- `ClearingHouseSpecialDatesService.changeStatus(id, isActive)` -> `PATCH api/clearing-house-special-dates/{id}/status`;
- `BankHolidaysAdminService.list(year)` for the existing date warning/validation flow.

There is no delete operation in the real frontend surface. Records are activated/inactivated instead.

### Fields and validation actually present

The persisted contract remains `ClearingHouseSpecialDate` with:

- `id`;
- `clearingHouseId`;
- optional `clearingHouseName`;
- `date`;
- `description`;
- `isActive`;
- optional `isWeekend`;
- optional `isNationalHoliday`;
- optional `calendarWarning`;
- optional `createdAt` / `updatedAt`.

The editor still exposes only date and description. Date is required; description is required and limited to 200 characters. The route camera remains fixed in the payload.

No explicit selectable “special day type” exists in this contract/UI, so none was invented. Weekend/national-holiday information continues to be handled by the existing warning metadata and bank-holiday lookup.

The existing duplicate-date behavior remains unchanged: same-camera duplicates are blocked locally, while weekend/national-holiday dates only produce warnings and remain saveable.

### Versioning/concurrency/conflicts

No `version`, `expectedVersion`, ETag, or other optimistic-concurrency field exists in this frontend contract or payload.

There is no status-code-specific 409 branch in the component. Save errors continue to preserve the backend message; messages containing the existing duplicate/existence wording set the `duplicateDate` control error, and other backend messages remain attached to the form. No merge/retry behavior was added.

## Functional behavior preserved

`ClearingHouseSpecialDatesComponent` class logic and helper functions are byte-for-byte identical to the v52 baseline. Only imports and standalone presentation-module registrations changed in the TypeScript file.

Preserved unchanged:

- camera ID from `:id`;
- initial `forkJoin` load of camera + year dates;
- year backend filter;
- local active/inactive status filter;
- local description filter;
- filter reset/reload behavior;
- MatTable sorting and pagination;
- date formatting and weekday rendering;
- create/edit flows;
- status activation/inactivation confirmation and API call;
- weekend/national-holiday warnings;
- same-camera duplicate validation;
- backend messages and snackbars;
- metrics;
- mobile presentation;
- contextual navigation.

## Foundation 4C.1 adoption

Only local presentation was migrated:

- local loading card -> `UiEstadoCarga`;
- local error card -> `UiEstadoError`, preserving the explicit “Volver a cámaras” Material action;
- local hero -> `app-page-header` with the existing create action;
- local empty state -> `UiEstadoVacio`;
- active/inactive `MatChip` -> `UiEtiquetaEstadoComponent`, because this is a direct two-state visual mapping with no lost semantics;
- all existing filters/editor/actions remain Angular Material;
- obsolete hero/state/empty CSS removed;
- all local hardcoded hex colors replaced with foundation tokens.

`MatTable`, `MatDialog`, `MatPaginator`, `MatSort`, mobile cards, metrics and the per-save spinner remain in place.

`MatChipsModule` was removed because no `mat-chip` remains on this screen. `MatProgressSpinnerModule` remains required by the editor save spinner and the legacy redirect component.

No new UI primitive was created.

## B9 boundary debt deliberately preserved

The screen remains physically under `features/catalogs` while importing existing symbols from `features/clearing-houses`:

- `ClearingHouseContextNavigationComponent`;
- `ConfirmationDialogComponent` / `ConfirmationDialogData`;
- `ClearingHouse`;
- `ClearingHousesService`.

Those feature-to-feature imports already existed in the baseline. No new feature-to-feature import was introduced. Ownership cleanup remains deferred to B9.

## Focused tests updated

Only `clearing-house-special-dates.component.spec.ts` was updated.

Coverage now explicitly preserves/checks:

- load by contextual `:id`;
- fixed camera and backend year query;
- local status and description filters;
- weekend and national-holiday warnings;
- duplicate-date validation;
- create and edit payloads for the route camera;
- activation/inactivation without delete;
- backend error preservation in the editor;
- contextual navigation links;
- `app-page-header`;
- `UiEstadoCarga` / `UiEstadoError` / `UiEstadoVacio`;
- `UiEtiquetaEstadoComponent`;
- existing legacy redirect behavior.

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

- command: `npm test -- --watch=false --browsers=ChromeHeadless --include=src/app/features/catalogs/components/clearing-house-special-dates.component.spec.ts`;
- exit code: `127`;
- result: `ng: not found`.

SPA build was actually attempted:

- command: `npm run build`;
- exit code: `127`;
- result: `ng: not found`.

Global TypeScript `5.8.3` `transpileModule` static syntax check reported:

- `clearing-house-special-dates.component.ts`: `0` diagnostics;
- `clearing-house-special-dates.component.spec.ts`: `0` diagnostics.

No backend executable validation was run because backend is out of scope and statically unchanged.

## Static validation

PASS.

Before this checkpoint was added, comparison against v52 showed exactly four changed functional files:

- `clearing-house-special-dates.component.ts`;
- `clearing-house-special-dates.component.html`;
- `clearing-house-special-dates.component.scss`;
- `clearing-house-special-dates.component.spec.ts`.

Confirmed byte-for-byte unchanged against v52:

- all 19 files under `features/ach-cycles` (B7 list/form/NACHA export included);
- all 17 files under `features/clearing-houses`;
- all 73 files under `features/transactions`;
- all 8 catalog service files;
- all 6 catalog model files;
- all 29 frontend routing files checked;
- all 1474 backend files under repository `src/`.

Additional static checks:

- `ClearingHouseSpecialDatesService` unchanged;
- `ClearingHouseSpecialDate` model unchanged;
- canonical and legacy routing unchanged;
- zero new feature-to-feature imports;
- zero `<ui-boton>` on the target surface;
- zero hardcoded hex/rgb values in target TS/HTML/SCSS;
- `app-page-header`, shared loading/error/empty states and direct active/inactive shared status label present.

## Final status

`CLEARING_HOUSE_SPECIAL_DATES_4C3B8A_IMPLEMENTED_VALIDATION_PENDING`

Implementation and static validation are complete. Angular executable validation remains pending because `ng` is unavailable and the single registry check timed out. No VERIFIED/UAT/release claim is made.
