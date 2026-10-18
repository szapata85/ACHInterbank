# SPA 4C.3B8b — Clearing House Preferences Checkpoint

## Scope

Baseline: `ACHInterbank-v53-spa-4C3B8a-clearing-house-special-dates.zip`.

This batch changes only the productive catalog surface:

- `/catalogs/clearing-house-preferences`;
- `ClearingHousePreferencesComponent`.

It does not advance to B9 and does not change special dates, other catalogs, routing, permissions, services/models, or backend.

## Real implementation inspected

### Route and permission

`CatalogsRoutingModule` owns `/catalogs/clearing-house-preferences`. The route remains guarded by `permissionGuard` with permission `CanManageAch`.

### Services/APIs actually used

The component continues to use exactly these existing services:

- `InstitutionClearingHousePreferencesService.list()` -> `GET institution-clearing-house-preferences`;
- `InstitutionClearingHousePreferencesService.create(...)` -> `POST institution-clearing-house-preferences`;
- `InstitutionClearingHousePreferencesService.update(id, ...)` -> `PUT institution-clearing-house-preferences/{id}`;
- `InstitutionClearingHousePreferencesService.delete(id)` -> `DELETE institution-clearing-house-preferences/{id}`;
- `FinancialInstitutionsApiService.getAll()` -> `GET financial-institutions` with the existing `includeInactive=false` default;
- `ClearingHousesApiService.listAdministrative()` -> `GET api/clearing-houses`.

No service or endpoint was changed.

### Fields and dynamic catalogs

`InstitutionClearingHousePreference` remains unchanged with:

- `id`;
- `financialInstitutionId` / `financialInstitutionName`;
- `clearingHouseId` / `clearingHouseName`;
- `isDefault`;
- `priority`;
- `isActive`.

Create continues to allow choosing a dynamic institution and clearing house. Institutions are still restricted to `FinancialInstitutionStatusEnum.Active` and sorted by name; clearing houses remain in the administrative API order. Edit continues to keep institution/camera fixed and changes only priority/default/status.

Priority semantics remain exactly:

- `1` = `Alta`;
- `2` = `Normal`;
- `3` = `Baja`.

The existing AG Grid quick search and per-column filters remain the filtering behavior. No new backend filtering was introduced.

### Operations, confirmations, status, and validation

Real operations remain:

- create relation;
- edit priority/default/status metadata;
- activate/inactivate through the existing `update(...)` operation;
- delete relation.

Activation/inactivation still requires the existing Material confirmation dialog. Inactivation of a default relation still preserves the existing warning text. Delete still requires explicit destructive confirmation.

Validation remains unchanged:

- create institution required and `>= 1`;
- create clearing house required and `>= 1`;
- priority required and constrained to `1..3`;
- duplicate-save protection through `saving`;
- backend errors continue through `catalogErrorMessage(...)` and the existing notification service.

There is no `version`, `expectedVersion`, ETag, or other optimistic-concurrency contract in this frontend surface. None was invented.

## Functional behavior preserved

`ClearingHousePreferencesComponent` TypeScript functional logic is unchanged. The only TypeScript production change is removal of the now-unused `MatProgressBarModule` presentation import/registration.

Preserved unchanged:

- `loadPreferences()`;
- `loadCatalogs()`;
- dynamic institutions and clearing houses;
- AG Grid column/search filters;
- priority mapping/normalization;
- `startCreate()` / `create()`;
- `startEdit()` / `save()`;
- `toggleActive()` and confirmation flow;
- `deletePreference()` and confirmation flow;
- success/error messages;
- duplicate-operation prevention;
- page size `10`;
- existing AG Grid row action renderer and navigation-free catalog behavior.

## Foundation 4C.1 adoption

Only local presentation was migrated:

- local catalog heading -> `app-page-header`;
- catalog loading -> `UiEstadoCarga`;
- catalog recoverable error -> `UiEstadoError`;
- operation error -> `UiEstadoError` without automatic retry;
- enterprise grid retained as `UiGrillaEmpresarial`, which continues to own its canonical `UiEstadoCarga`, `UiEstadoError`, and `UiEstadoVacio` list states;
- existing Angular Material buttons, selects, checkboxes, cards and dialog behavior retained;
- local save/catalog progress bars replaced by the shared loading state, so `MatProgressBarModule` is no longer required;
- all local hardcoded hex/rgb colors replaced with foundation semantic tokens;
- dead local hero/error/progress responsive CSS removed.

`UiEtiquetaEstadoComponent` was not forced into the AG Grid DOM renderers: doing so would require replacing the existing grid renderer mechanism. The existing `Activa/Inactiva` and `Sí/No` pills are preserved and now use semantic foundation tokens, with no semantic change and no new primitive/renderer introduced.

No `<ui-boton>` exists on this screen.

## B9 boundaries deliberately preserved

Existing feature-to-feature dependencies remain exactly as in the baseline:

- `ClearingHousesApiService` re-export through `features/ach-cycles`;
- `ClearingHouseOption` from `features/ach-cycles`;
- `FinancialInstitutionsApiService`, `DestinationInstitution`, and `FinancialInstitutionStatusEnum` from `features/transactions`.

No boundary was moved and no new feature-to-feature import was introduced. Cleanup remains B9 work.

## Focused tests updated

Only `clearing-house-preferences.component.spec.ts` was updated.

Coverage preserves/checks:

- preferences initial load;
- dynamic active institutions and administrative clearing houses;
- existing grid search/column filters;
- priority values/labels;
- required create fields;
- valid create payload;
- duplicate-create protection while saving;
- edit payload limited to editable fields;
- invalid priority blocks update;
- activate/inactivate confirmation and status payload;
- delete confirmation;
- backend/catalog errors;
- `app-page-header`;
- shared loading/error/empty states while retaining `UiGrillaEmpresarial`;
- absence of `ui-boton`.

## Executable validation actually attempted

Observed environment:

- Node: `v22.16.0`;
- npm: `10.9.2`;
- global TypeScript: `5.8.3`;
- `web/ach-interbank-ui/node_modules/.bin/ng`: absent.

Exactly one short registry check was run:

- command: `timeout 12s npm ping`;
- exit code: `124`;
- output reached: `npm notice PING https://registry.npmjs.org/`.

No `npm ci` was run or retried.

Focused test was actually attempted:

- command: `npm test -- --watch=false --browsers=ChromeHeadless --include=src/app/features/catalogs/components/clearing-house-preferences.component.spec.ts`;
- exit code: `127`;
- result: `ng: not found`.

SPA build was actually attempted:

- command: `npm run build`;
- exit code: `127`;
- result: `ng: not found`.

Global TypeScript `5.8.3` `transpileModule` static syntax check reported:

- `clearing-house-preferences.component.ts`: `0` syntactic diagnostics;
- `clearing-house-preferences.component.spec.ts`: `0` syntactic diagnostics.

No backend executable validation was run because backend is explicitly out of scope and statically unchanged.

## Static validation

PASS.

Before this checkpoint was added, comparison against v53 showed exactly four changed functional files:

- `clearing-house-preferences.component.ts`;
- `clearing-house-preferences.component.html`;
- `clearing-house-preferences.component.scss`;
- `clearing-house-preferences.component.spec.ts`.

Confirmed byte-for-byte unchanged against v53:

- all 4 `clearing-house-special-dates.component.*` B8a files;
- all 8 catalog service files;
- all 6 catalog model files;
- every other file under `features/catalogs`;
- all 19 files under `features/ach-cycles`;
- all 17 files under `features/clearing-houses`;
- all 73 files under `features/transactions`;
- all 29 frontend routing files checked;
- all 1474 backend files under repository `src/`.

Additional static checks:

- `InstitutionClearingHousePreferencesService` unchanged;
- `InstitutionClearingHousePreference` model unchanged;
- `CatalogsRoutingModule` and `CanManageAch` permission unchanged;
- zero new feature-to-feature imports;
- zero `<ui-boton>` in the target template;
- zero hardcoded hex/rgb values in target production TS/HTML/SCSS;
- `app-page-header` and shared loading/error states present;
- empty/loading/error list presentation remains provided by `UiGrillaEmpresarial` foundation primitives.

## Final status

`CLEARING_HOUSE_PREFERENCES_4C3B8B_IMPLEMENTED_VALIDATION_PENDING`

Implementation and static validation are complete. Angular executable validation remains pending because `ng` is unavailable and the single registry check timed out. No VERIFIED/UAT/release claim is made.
