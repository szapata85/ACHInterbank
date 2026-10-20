# SPA 4C.3B5b — Cycle Config Foundation Checkpoint

## Baseline

- Required baseline: `ACHInterbank-v46-spa-4C3B5a-cycle-route-ownership.zip`
- Scope: frontend-only migration of `CycleConfigManagementComponent` at canonical route `/clearing-houses/:id/cycles` onto the 4C.1 visual foundation.
- Out of scope and unchanged: routing ownership from B5a, `ach-cycles`, transaction policies, backend, services/contracts/models, permissions and B9 boundary cleanup.

## Files modified

Functional/test surface:

- `web/ach-interbank-ui/src/app/features/transactions/components/cycle-config-management/cycle-config-management.component.ts`
- `web/ach-interbank-ui/src/app/features/transactions/components/cycle-config-management/cycle-config-management.component.html`
- `web/ach-interbank-ui/src/app/features/transactions/components/cycle-config-management/cycle-config-management.component.scss`
- `web/ach-interbank-ui/src/app/features/transactions/components/cycle-config-management/cycle-config-management.component.spec.ts`

Documentation:

- `docs/ai/SPA-4C3B5B-CYCLE-CONFIG-FOUNDATION-CHECKPOINT.md`

The component TypeScript functional body is unchanged. Relative to v46, its only production-code changes are imports/standalone-registration of the shared 4C.1 primitives.

## Functional behavior preserved

The component still obtains the clearing-house id exclusively from route parameter `:id`. Invalid/non-positive ids still short-circuit with `La cámara indicada no es válida.` and do not request clearing-house/cycle data.

Initial load remains a `forkJoin` of:

- `ClearingHousesService.get(id)`
- `ClearingHouseCycleConfigsApiService.getByClearingHouse({ clearingHouseId: id, effectiveAt })`

Reload continues to query only `getByClearingHouse()` with the current route camera and effective reference date.

Preserved local filters:

- cycle name;
- active/inactive state;
- current/future/historical validity;
- effective reference date.

Preserved metrics and history:

- total/current/future/inactive counts;
- next operational window;
- version history sorted by `effectiveFrom` descending;
- current `MatTable`, `MatSort`, `MatPaginator` behavior;
- desktop table and mobile cards.

Preserved create/version/inactivation operations:

- `createVersion(payload)` continues to receive `UpsertCycleConfigRequest` with the route clearing-house id;
- `inactivate(id, { effectiveTo })` remains unchanged;
- dialogs and confirmation flow remain unchanged;
- success/error snackbars and backend error detail mapping remain unchanged.

Preserved fields and validation include:

- `cycleName`, max length 60;
- start/end/cutoff/output-release times;
- optional compensation statement/payment/output-release expanded stage windows;
- transaction eligibility toggles for credit/debit, prenotifications, Return and Return of Return;
- required `effectiveFrom`;
- time format validation;
- start/end ordering;
- cutoff within the operating window;
- output release not before close;
- complete expanded-stage requirement and close → statement → payment → release order;
- `validateVersionConflicts()` duplicate and historical-overlap checks;
- backend duplicate/overlap error mapping.

No endpoint, request payload, enum/value, effective-date semantics, backend message handling or permission was changed.

## Permissions preserved

The component retains the current permission checks exactly:

- manage cycles: `ClearingHouses.ManageCycles`;
- policy-context visibility: `Config.Read`, `Config.Manage`, `CanReadAch`, `CanManageAch`;
- cycle-context visibility: `ClearingHouses.View`, `ClearingHouses.ManageCycles`;
- special-date visibility: `ClearingHouses.View`, `ClearingHouses.ManageSpecialDates`.

## Foundation 4C.1 migration

Local visual debt was replaced only where 4C.1 already provides a canonical primitive:

- custom `cycle-hero` → `app-page-header`;
- page loading card/spinner → `UiEstadoCargaComponent`;
- page error card → `UiEstadoErrorComponent` plus the existing Material back-navigation action;
- local empty state → `UiEstadoVacioComponent`;
- hardcoded local colors → shared design tokens (`--color-text`, `--color-text-muted`, `--color-text-soft`, `--color-primary`, `--color-border`, `--color-error-surface`, `--color-error-text`).

Preserved intentionally:

- Material table, sorting and paginator;
- Material dialogs;
- metrics;
- `ClearingHouseContextNavigationComponent`;
- `MatChip` for `Vigente`, `Futura`, `Histórica`, `Inactiva`, because these four business distinctions were not collapsed into a generic canonical status mapping;
- `MatProgressSpinnerModule`, because the save button still uses `<mat-spinner>` while a create/version request is in flight. The page-level loading spinner was removed.

There are zero hardcoded hex/rgb colors in the target component after the migration.

## B9 boundary debt deliberately preserved

As required, no ownership/boundary refactor was performed. The existing imports from `transactions` into `features/clearing-houses` remain:

- `ConfirmationDialogComponent` / `ConfirmationDialogData`;
- `ClearingHouseContextNavigationComponent`;
- `ClearingHouse`;
- `ClearingHousesService`.

These were already present in v46. No new feature-to-feature import was introduced. Moving these contracts/primitives to an appropriate core/shared boundary remains deferred to B9.

## Tests updated

`cycle-config-management.component.spec.ts` continues to cover the existing functional flow and now also asserts the 4C.1 states:

- route id loads the fixed camera and cycle configuration;
- invalid route id makes no new API requests and renders shared error state;
- route id changes reload the new camera;
- HTTP load error remains differentiated and renders shared error state;
- shared loading and empty states are rendered;
- local name/state/validity filtering remains;
- stage/cutoff validation and duplicate detection remain;
- historical overlap validation remains;
- create-version request continues to use the route clearing-house id and existing payload shape;
- expanded-stage ordering remains;
- versioning/history/inactivation behavior remains;
- `app-page-header` and Material table remain present;
- manage permission remains active under the existing permission stub.

Tests were not weakened to bypass failures.

## Executable validation actually attempted

Environment observed:

- Node: `v22.16.0`
- npm: `10.9.2`
- `node_modules/.bin/ng`: absent
- global TypeScript: `5.8.3`

Because Angular dependencies are absent, exactly one short npm registry check was made:

- command: `timeout 12s npm ping`
- exit code: `124`
- output reached: `npm notice PING https://registry.npmjs.org/`
- cause: registry did not respond before timeout

No repeated `npm ci` was attempted.

Actual Angular commands attempted:

1. Focal test:
   - command: `npm test -- --watch=false --browsers=ChromeHeadless --include=src/app/features/transactions/components/cycle-config-management/cycle-config-management.component.spec.ts`
   - exit code: `127`
   - cause: `ng: not found`
2. SPA build:
   - command: `npm run build`
   - exit code: `127`
   - cause: `ng: not found`

No backend command was executed because backend is out of scope and statically unchanged.

Complementary syntax-only validation using global TypeScript `5.8.3` and `transpileModule(..., reportDiagnostics=true)`:

- `cycle-config-management.component.ts`: 0 syntax diagnostics;
- `cycle-config-management.component.spec.ts`: 0 syntax diagnostics.

This syntax-only check does not replace Angular tests or build.

## Static validation against v46

PASS.

Confirmed:

- B5a routing files are byte-for-byte unchanged;
- canonical `/clearing-houses/:id/cycles` route ownership remains unchanged;
- all 19 files under `features/ach-cycles` are byte-for-byte unchanged;
- all 17 files under `features/clearing-houses` are byte-for-byte unchanged, preserving B4a/B4b;
- the 75 `features/transactions` files outside the cycle-config target directory are byte-for-byte unchanged;
- `ClearingHouseCycleConfigsApiService` is byte-for-byte unchanged;
- `transactions.models.ts` is byte-for-byte unchanged;
- `app-routing.module.ts` and `app-routing-access.spec.ts` are byte-for-byte unchanged;
- all 1474 backend files under repository `src/` are byte-for-byte unchanged;
- the component TypeScript functional body is unchanged; only shared foundation imports/standalone registrations were added;
- no new feature-to-feature import was introduced;
- zero hardcoded hex/rgb colors remain in the target component;
- shared `PageHeader`, loading, error and empty primitives are used;
- `MatChip` semantics for cycle validity/state remain unchanged.

## Final status

`CYCLE_CONFIG_4C3B5B_IMPLEMENTED_VALIDATION_PENDING`

Angular executable validation remains pending because dependencies are unavailable and the npm registry check timed out. No claim of VERIFIED is made.
