# SPA 4C.3B5a — Cycle Config Route Ownership Checkpoint

## Baseline

- Mandatory baseline: `ACHInterbank-v45-spa-4C3B4b-clearing-house-context.zip`
- Scope: routing/ownership correction only for clearing-house cycle configuration.
- No visual migration was performed.
- No backend, API contract, model, service, cycle configuration behavior, clearing-house admin behavior, context-navigation behavior, transaction-policy behavior or catalog behavior was changed.
- This lot does not advance to 4C.3B5b.

## Current routing inspected before the change

The baseline contained two paths related to clearing-house cycle configuration:

1. Canonical route in `app-routing.module.ts`:
   - `/clearing-houses/:id/cycles`
   - lazy-loads `CycleConfigManagementComponent`;
   - child permissions remain `ClearingHouses.View` / `ClearingHouses.ManageCycles` under the existing `clearing-houses` parent guard.
2. Legacy/non-canonical route in `transactions-routing.module.ts`:
   - `/transactions/cycle-configs`
   - mounted `CycleConfigManagementComponent` directly;
   - permission `CanManageAch`;
   - did not provide the `:id` parameter required by the component.

`AchCycleListComponent` also exposed a visible list-level link `Reglas de procesamiento` pointing to `/transactions/cycle-configs`.

This matches the route-ownership defect documented in 4C.3B0.

## Route ownership decision

### Canonical route

`/clearing-houses/:id/cycles` remains the sole route that mounts `CycleConfigManagementComponent`.

The canonical route itself was not edited. Static comparison confirms `app-routing.module.ts` is byte-for-byte identical to v45.

Preserved configuration:

- path: `:id/cycles` under `/clearing-houses`;
- `CycleConfigManagementComponent` lazy load;
- existing permission guard;
- permissions: `ClearingHouses.View`, `ClearingHouses.ManageCycles`;
- clearing-house `:id` remains the route parameter consumed by the component.

### Legacy `/transactions/cycle-configs`

The legacy path no longer mounts `CycleConfigManagementComponent` without a clearing-house id.

It now:

- remains registered as `cycle-configs` for compatibility;
- uses `pathMatch: 'full'`;
- redirects to `/clearing-houses` so the user can choose a real clearing house;
- retains the existing `permissionGuard` and `CanManageAch` route metadata;
- has no component assigned.

No clearing-house id is guessed or hardcoded.

The obsolete direct import of `CycleConfigManagementComponent` was removed from `transactions-routing.module.ts` because that routing file no longer mounts the component.

`TransactionsModule` was deliberately not refactored in this micro-lot; moving physical ownership/import structure belongs to later ownership/boundary work, not this routing correction.

## AchCycleList canonical link

The existing `Reglas de procesamiento` link is list-level, not a row action. The list already has a real clearing-house context in `filterForm.controls.clearingHouseId`, and each `AchCycleSummary` also carries a `clearingHouseId`.

The link now follows this safe rule:

- when the current list context has `clearingHouseId`, navigate to `/clearing-houses/{clearingHouseId}/cycles`;
- when no clearing house is selected, navigate to `/clearing-houses` rather than inventing an id.

No new navigation service, store, model or cross-feature import was introduced.

`AchCycleListComponent` TypeScript is byte-for-byte unchanged; only its existing router link template was corrected.

## CycleConfigManagementComponent unchanged

The component and its current behavior were intentionally left untouched.

Byte-for-byte unchanged relative to v45:

- `cycle-config-management.component.ts`
- `cycle-config-management.component.html`
- `cycle-config-management.component.scss`
- `cycle-config-management.component.spec.ts`

Its services and models also remain unchanged.

No API endpoint, request payload, clearing-house parameter handling or cycle configuration logic changed.

## Tests changed/added

Routing tests were updated only for the ownership change:

- `transactions-routing.module.spec.ts`
  - confirms `cycle-configs` no longer mounts a component;
  - confirms redirect to `/clearing-houses`;
  - confirms `pathMatch: 'full'`;
  - confirms `permissionGuard` and `CanManageAch` metadata remain.
- `app-routing-access.spec.ts`
  - confirms canonical `/clearing-houses/:id/cycles` remains registered;
  - confirms its existing permission guard and permissions remain;
  - confirms it still lazy-loads a component and is not a redirect.
- new `ach-cycle-list.component.spec.ts`
  - confirms that without clearing-house context the list does not invent an id and links to `/clearing-houses`;
  - confirms that selecting clearing house `7` produces `/clearing-houses/7/cycles`.

No `CycleConfigManagementComponent` test was changed.

## Executable validation actually attempted

Environment observed:

- `node_modules/.bin/ng`: absent
- global TypeScript: `5.8.3`

A single short npm-registry check was performed because Angular dependencies are not installed:

- command: `timeout 12s npm ping`
- exit code: `124`
- observed output: `npm notice PING https://registry.npmjs.org/`
- cause: the registry did not respond before the timeout

No repeated `npm ci` was attempted while the registry remained unavailable.

Actual frontend validation attempts:

1. Focal routing/link tests
   - command: `npm test -- --watch=false --browsers=ChromeHeadless --include=src/app/features/transactions/transactions-routing.module.spec.ts --include=src/app/app-routing-access.spec.ts --include=src/app/features/ach-cycles/components/ach-cycle-list.component.spec.ts`
   - exit code: `127`
   - cause: `ng: not found`
2. SPA build
   - command: `npm run build`
   - exit code: `127`
   - cause: `ng: not found`

No backend command was executed because backend is out of scope and byte-for-byte unchanged.

Complementary syntax-only validation with global TypeScript `5.8.3` using `transpileModule(..., reportDiagnostics=true)`:

- `transactions-routing.module.ts`: 0 syntax diagnostics;
- `transactions-routing.module.spec.ts`: 0 syntax diagnostics;
- `app-routing-access.spec.ts`: 0 syntax diagnostics;
- `ach-cycle-list.component.spec.ts`: 0 syntax diagnostics.

This syntax check does not replace Angular tests/build.

## Static validation against v45

PASS.

Confirmed:

- `/clearing-houses/:id/cycles` still exists and still loads `CycleConfigManagementComponent`;
- canonical permissions remain `ClearingHouses.View` / `ClearingHouses.ManageCycles`;
- `/transactions/cycle-configs` no longer mounts `CycleConfigManagementComponent`;
- the legacy route redirects to `/clearing-houses` and retains its `CanManageAch` guard metadata;
- `AchCycleListComponent` no longer links to `/transactions/cycle-configs`;
- with a selected `clearingHouseId`, its link is `/clearing-houses/{id}/cycles`;
- without a selected id, it safely links to `/clearing-houses`;
- `CycleConfigManagementComponent` is byte-for-byte intact;
- `app-routing.module.ts` is byte-for-byte intact;
- `AchCycleListComponent` TypeScript is byte-for-byte intact;
- `ach-cycles-routing.module.ts` is byte-for-byte intact;
- all 17 files under `features/clearing-houses` are byte-for-byte intact, including B4a/B4b;
- transaction/ach-cycle services and models included in the comparison are byte-for-byte intact;
- all 1474 backend files under `src/` are byte-for-byte intact;
- no new feature-to-feature import was introduced.

Only the following functional/test files differ from v45:

- `web/ach-interbank-ui/src/app/features/transactions/transactions-routing.module.ts`
- `web/ach-interbank-ui/src/app/features/transactions/transactions-routing.module.spec.ts`
- `web/ach-interbank-ui/src/app/app-routing-access.spec.ts`
- `web/ach-interbank-ui/src/app/features/ach-cycles/components/ach-cycle-list.component.html`
- new `web/ach-interbank-ui/src/app/features/ach-cycles/components/ach-cycle-list.component.spec.ts`

Documentation added:

- `docs/ai/SPA-4C3B5A-CYCLE-ROUTE-OWNERSHIP-CHECKPOINT.md`

## Final status

`CYCLE_ROUTE_OWNERSHIP_4C3B5A_IMPLEMENTED_VALIDATION_PENDING`

Angular executable validation remains pending because dependencies are unavailable and the npm registry check timed out. No claim of VERIFIED is made.
