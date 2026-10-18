# SPA 4C.3B9a2 — Clearing House Context Navigation Boundary Checkpoint

## Baseline and scope

- Mandatory baseline: `ACHInterbank-v55-spa-4C3B9a1-financial-institutions-boundary.zip`.
- Objective: remove the feature-to-feature ownership debt of `ClearingHouseContextNavigationComponent` by moving only that reusable component to `shared/components`.
- No backend, routing, permissions, APIs, `ClearingHousesService`, `ClearingHouse`, clearing-house dialogs, or other B9 boundary was changed.
- B9a1 Financial Institutions boundary remains intact.

## References inspected before the move

The complete frontend reference search found exactly three production consumers of `ClearingHouseContextNavigationComponent`:

1. `features/clearing-houses/transaction-policies.component.ts`;
2. `features/transactions/components/cycle-config-management/cycle-config-management.component.ts`;
3. `features/catalogs/components/clearing-house-special-dates.component.ts`.

The component's own focused spec was the only spec importing it directly. `clearing-house-special-dates.component.spec.ts` checks the rendered selector but does not import the component path directly.

Before this batch, the two consumers outside `clearing-houses` imported the component directly from `features/clearing-houses`, which was the B9 ownership debt documented in prior checkpoints.

## Boundary move

Moved from:

- `features/clearing-houses/clearing-house-context-navigation.component.ts`;
- `features/clearing-houses/clearing-house-context-navigation.component.scss`;
- `features/clearing-houses/clearing-house-context-navigation.component.spec.ts`.

Moved to:

- `shared/components/clearing-house-context-navigation.component.ts`;
- `shared/components/clearing-house-context-navigation.component.scss`;
- `shared/components/clearing-house-context-navigation.component.spec.ts`.

The old three files no longer exist.

The SCSS and focused spec are byte-for-byte identical to their baseline contents; only their ownership path changed.

## Minimal shared contract

`shared` does not import `features/clearing-houses/clearing-houses.models`.

The shared component exports only the structural contract it needs:

```ts
export interface ClearingHouseContext {
  id: number;
  code: string;
  name: string;
  isActive: boolean;
}
```

The input is now `ClearingHouseContext`. Existing `ClearingHouse` objects remain assignable through TypeScript structural typing; the complete clearing-house model was not moved or duplicated.

The component's remaining local shared dependency was adjusted from its old feature-relative path to `./ui/ui-etiqueta-estado.component`.

## Consumers updated

Only the import path for `ClearingHouseContextNavigationComponent` changed in the three production consumers:

- `TransactionPoliciesComponent` -> `../../shared/components/clearing-house-context-navigation.component`;
- `CycleConfigManagementComponent` -> `../../../../shared/components/clearing-house-context-navigation.component`;
- `ClearingHouseSpecialDatesComponent` -> `../../../shared/components/clearing-house-context-navigation.component`.

Byte comparison after restoring only the prior import line proves the full contents of all three consumer TypeScript files are otherwise identical to v55. No functional consumer logic changed.

No consumer HTML or consumer spec needed an import-path edit.

## Behavior preserved

The component keeps the same selector and inline template:

- selector: `app-clearing-house-context-navigation`;
- return link: `/clearing-houses`;
- identity: clearing-house `name` and `code`;
- status: `Activa` / `Inactiva` through `UiEtiquetaEstadoComponent`;
- `canReadPolicies` controls `/clearing-houses/:id/transaction-policies`;
- `canReadCycles` controls `/clearing-houses/:id/cycles`;
- `canReadSpecialDates` controls `/clearing-houses/:id/special-dates`;
- `routerLinkActive` remains unchanged for active-tab behavior;
- responsive SCSS remains byte-for-byte unchanged.

A normalized-newline comparison confirms the complete inline template is identical to the baseline. No route, permission input, visual behavior, or responsive rule changed.

## Boundary/static validation

PASS.

Pre-checkpoint comparison against v55 showed exactly nine changed paths:

- 3 deleted files at the old `features/clearing-houses` location;
- 3 added files at the new `shared/components` location;
- 3 production consumer TypeScript files modified only for the component import path.

Confirmed:

- zero imports of the component through the old `features/clearing-houses` path;
- zero old component files remain under `features/clearing-houses`;
- `shared` has zero imports resolving into any `features/*` path;
- the minimal shared contract contains only `id`, `code`, `name`, `isActive`;
- all three production consumer bodies are unchanged outside their import path;
- selector, inline template, routes, permission inputs, `routerLinkActive`, active/inactive status behavior, and responsive SCSS are preserved;
- cross-feature import-edge comparison versus v55 found `0` new feature-to-feature edges and removed the two context-navigation edges from `transactions -> clearing-houses` and `catalogs -> clearing-houses`;
- B9a1 core files `financial-institution.model.ts` and `financial-institutions-api.service.ts` retain their v55 SHA-256 hashes;
- because the pre-checkpoint diff contains only the nine expected frontend paths, routing, APIs, services/models outside the moved boundary, B9a1 consumers, and backend are byte-for-byte unchanged;
- relative imports under `shared` resolve successfully in the static import-resolution scan.

No other B9 boundary was advanced.

## Executable validation actually attempted

Observed environment:

- Node: `v22.16.0`;
- npm: `10.9.2`;
- global TypeScript: `5.8.3`;
- `web/ach-interbank-ui/node_modules/.bin/ng`: absent.

Exactly one short registry check was run:

- command: `timeout 12s npm ping`;
- exit code: `124`;
- observed output: `npm notice PING https://registry.npmjs.org/`.

No `npm ci` was run or retried.

Affected Angular tests were actually attempted for:

- moved shared context-navigation spec;
- transaction-policies spec;
- cycle-config-management spec;
- clearing-house-special-dates spec.

Command result:

- exit code: `127`;
- result: `ng: not found`.

SPA build was actually attempted:

- command: `npm run build`;
- exit code: `127`;
- result: `ng: not found`.

Global TypeScript `5.8.3` `transpileModule(..., reportDiagnostics=true)` static syntax validation reported `0` syntax errors for the moved component/spec and the three changed consumer TypeScript files.

No backend executable validation was run because backend is explicitly out of scope and statically unchanged.

## Files changed by this batch

Moved/owned by shared:

- `web/ach-interbank-ui/src/app/shared/components/clearing-house-context-navigation.component.ts`;
- `web/ach-interbank-ui/src/app/shared/components/clearing-house-context-navigation.component.scss`;
- `web/ach-interbank-ui/src/app/shared/components/clearing-house-context-navigation.component.spec.ts`.

Removed old ownership:

- `web/ach-interbank-ui/src/app/features/clearing-houses/clearing-house-context-navigation.component.ts`;
- `web/ach-interbank-ui/src/app/features/clearing-houses/clearing-house-context-navigation.component.scss`;
- `web/ach-interbank-ui/src/app/features/clearing-houses/clearing-house-context-navigation.component.spec.ts`.

Import-only production consumer changes:

- `web/ach-interbank-ui/src/app/features/clearing-houses/transaction-policies.component.ts`;
- `web/ach-interbank-ui/src/app/features/transactions/components/cycle-config-management/cycle-config-management.component.ts`;
- `web/ach-interbank-ui/src/app/features/catalogs/components/clearing-house-special-dates.component.ts`.

Documentation:

- `docs/ai/SPA-4C3B9A2-CLEARING-HOUSE-CONTEXT-BOUNDARY-CHECKPOINT.md`.

## Final status

`CLEARING_HOUSE_CONTEXT_BOUNDARY_4C3B9A2_IMPLEMENTED_VALIDATION_PENDING`

Implementation and static validation are complete. Angular executable validation remains pending because `ng` is unavailable and the single registry check timed out. No VERIFIED/UAT/release claim is made.
