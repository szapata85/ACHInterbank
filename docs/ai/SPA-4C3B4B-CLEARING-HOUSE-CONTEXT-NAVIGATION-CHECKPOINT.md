# SPA 4C.3B4b — Clearing House Context Navigation Checkpoint

## Baseline

- Mandatory baseline: `ACHInterbank-v44-spa-4C3B4a-clearing-houses-admin.zip`
- Scope: frontend-only local foundation cleanup of `ClearingHouseContextNavigationComponent`.
- No consumer page, routing, backend, cycles, policies, special-dates or other feature behavior was changed.

## Surface inspected and migrated

Target:

- `web/ach-interbank-ui/src/app/features/clearing-houses/clearing-house-context-navigation.component.ts`
- `web/ach-interbank-ui/src/app/features/clearing-houses/clearing-house-context-navigation.component.scss`
- `web/ach-interbank-ui/src/app/features/clearing-houses/clearing-house-context-navigation.component.spec.ts`

The component is standalone and keeps its inline template. There is no separate HTML file for this surface in the baseline.

## Navigation and permissions preserved

The existing contextual navigation remains unchanged functionally:

- return link: `/clearing-houses`;
- identity: clearing-house `name` and `code`;
- status text: `Activa` / `Inactiva` from `clearingHouse.isActive`;
- `canReadPolicies` controls `/clearing-houses/:id/transaction-policies`;
- `canReadCycles` controls `/clearing-houses/:id/cycles`;
- `canReadSpecialDates` controls `/clearing-houses/:id/special-dates`;
- `routerLinkActive` continues to drive the active Material tab and `aria-current="page"`.

No route or permission value was widened, narrowed, renamed or moved.

## Foundation 4C.1 cleanup

The component was already based on Angular Material. This lot only performs local visual cleanup:

- retained `MatCard`, `MatButton`, `MatIcon`, `MatTabs` and `MatTooltip`;
- replaced the local `MatChip` active/inactive treatment with canonical `UiEtiquetaEstadoComponent`;
- visual mapping is direct and semantic-only:
  - `isActive=true` -> `estado="activo"`, text `Activa`;
  - `isActive=false` -> `estado="inactivo"`, text `Inactiva`;
- removed the now-unused `MatChipsModule` import;
- replaced local hardcoded text colors with existing tokens:
  - `#64748b` -> `var(--color-text-soft)`;
  - `#475569` -> `var(--color-text-muted)`;
- removed the dead `.context-card__inactive` rule and its hardcoded inactive colors.

Static scan after the change reports zero hardcoded hex/rgb/rgba values in the target TS/SCSS. The tabs/card layout was not redesigned and no new shared primitive was created.

## Tests focales

Only `clearing-house-context-navigation.component.spec.ts` was updated.

Coverage now explicitly checks:

- return link to `/clearing-houses`;
- preserved policies/cycles/special-dates links;
- visibility of all three contextual links according to their corresponding permission input;
- active tab behavior through `routerLinkActive` / `aria-current`;
- clearing-house name and code;
- `Activa` and `Inactiva` rendering through `ui-etiqueta-estado`.

No consumer-page tests were changed.

## B9 boundary debt deliberately preserved

`ClearingHouseContextNavigationComponent` remains physically under `features/clearing-houses` as required by this lot.

The existing cross-feature consumers remain byte-for-byte unchanged:

- `features/transactions/components/cycle-config-management/cycle-config-management.component.ts`
  imports `ClearingHouseContextNavigationComponent` from `features/clearing-houses`;
- `features/catalogs/components/clearing-house-special-dates.component.ts`
  imports the same component from `features/clearing-houses`.

This is the existing B9 architecture debt: reusable context navigation is owned by one feature while being consumed directly by two other features. No new feature-to-feature import was introduced in B4b.

Minimal B9 direction, not implemented here: move the reusable navigation boundary to `shared` only together with a neutral clearing-house context contract/input that does **not** make `shared` import a feature model (or pass the primitive context inputs directly). Route ownership and consumer behavior should remain unchanged during that future decoupling.

## Executable validation actually attempted

Environment observed:

- Node: `v22.16.0`
- npm: `10.9.2`
- `node_modules/.bin/ng`: absent
- global TypeScript: `5.8.3`

A single short npm-registry check was performed:

- command: `timeout 12s npm ping`
- exit code: `124`
- observed output: `npm notice PING https://registry.npmjs.org/`
- cause: registry did not respond before the timeout

No repeated `npm ci` was attempted while the registry remained unavailable.

Actual Angular attempts:

1. Focal test
   - command: `npm test -- --watch=false --browsers=ChromeHeadless --include=src/app/features/clearing-houses/clearing-house-context-navigation.component.spec.ts`
   - exit code: `127`
   - cause: `ng: not found`
2. SPA build
   - command: `npm run build`
   - exit code: `127`
   - cause: `ng: not found`

No backend validation was executed because backend is out of scope and unchanged.

Complementary syntax-only validation with global TypeScript `5.8.3` using `transpileModule(..., reportDiagnostics=true)`:

- component TS: 0 syntax diagnostics;
- component spec TS: 0 syntax diagnostics.

This syntax check does not replace Angular tests/build.

## Static validation against v44

PASS.

Byte-for-byte unchanged relative to the mandatory baseline:

- B4a admin functional files: `clearing-houses.component.ts/html/scss/spec.ts`;
- `app-routing.module.ts`;
- all 137 files under `features/transactions` + `features/catalogs`, including both cross-feature consumers;
- all 14 other `features/clearing-houses` files outside the three B4b target files;
- all 1474 backend files under `src/`.

Additional static checks:

- zero new feature-to-feature imports in the target component;
- zero hardcoded hex/rgb/rgba values in target functional TS/SCSS;
- canonical `UiEtiquetaEstadoComponent` is used for the clearing-house active/inactive state;
- all three contextual routes and permission gates remain in the component;
- `routerLinkActive` remains on each contextual tab;
- no cycles, policies, special-dates, routing, backend or consumer source file was modified.

## Files modified

Functional:

- `web/ach-interbank-ui/src/app/features/clearing-houses/clearing-house-context-navigation.component.ts`
- `web/ach-interbank-ui/src/app/features/clearing-houses/clearing-house-context-navigation.component.scss`
- `web/ach-interbank-ui/src/app/features/clearing-houses/clearing-house-context-navigation.component.spec.ts`

Documentation:

- `docs/ai/SPA-4C3B4B-CLEARING-HOUSE-CONTEXT-NAVIGATION-CHECKPOINT.md`

## Final status

`CLEARING_HOUSE_CONTEXT_4C3B4B_IMPLEMENTED_VALIDATION_PENDING`

Angular executable validation remains pending because dependencies are unavailable and the npm registry check timed out. No claim of VERIFIED is made.
