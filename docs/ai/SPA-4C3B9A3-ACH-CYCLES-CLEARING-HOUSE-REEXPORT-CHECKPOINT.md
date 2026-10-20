# SPA 4C.3B9a3 — ACH-Cycles Clearing-House Reexport Retirement Checkpoint

## Baseline and scope

- Mandatory baseline: `ACHInterbank-v56-spa-4C3B9a2-clearing-house-context-boundary.zip`.
- Objective: retire only the `ClearingHousesApiService` and `ClearingHouseOption` reexports from `features/ach-cycles` and point all consumers of those reexports directly to their already-existing canonical `core` ownership.
- Canonical service: `core/services/clearing-houses-api.service.ts`.
- Canonical model: `core/models/clearing-house.model.ts`.
- No backend, routing, `ClearingHousesService`, full `ClearingHouse`, special-dates boundary, cycle-config boundary, operational-restrictions boundary, or other B9 boundary was changed.
- B9a1 and B9a2 remain intact.

## References inspected before the change

The frontend search confirmed the following production consumers still reached the clearing-house service/model through `features/ach-cycles`:

1. `AchCycleListComponent`;
2. `AchCycleFormComponent`;
3. `NachaExportComponent`;
4. `ClearingHousePreferencesComponent`;
5. `NachaInboundSimulatorComponent`.

Focused specs for those surfaces also imported `ClearingHousesApiService` through the ACH-cycles service reexport. The preferences spec also imported `ClearingHouseOption` through the ACH-cycles model reexport.

Other consumers already using the canonical core paths were left untouched.

## Reexports retired

Removed from `features/ach-cycles/services/ach-cycles-api.service.ts`:

```ts
export { ClearingHousesApiService } from '../../../core/services/clearing-houses-api.service';
```

Removed from `features/ach-cycles/models/ach-cycle.model.ts`:

```ts
export type { ClearingHouseOption } from '../../../core/models/clearing-house.model';
```

`AchCyclesApiService`, `AchCycleSummary`, and all other ACH-cycle contracts/methods remain unchanged.

## Consumers updated

Only import declarations were changed in the following consumer files:

- `features/ach-cycles/components/ach-cycle-list.component.ts`;
- `features/ach-cycles/components/ach-cycle-list.component.spec.ts`;
- `features/ach-cycles/components/ach-cycle-form.component.ts`;
- `features/ach-cycles/components/ach-cycle-form.component.spec.ts`;
- `features/ach-cycles/components/nacha-export.component.ts`;
- `features/ach-cycles/components/nacha-export.component.spec.ts`;
- `features/catalogs/components/clearing-house-preferences.component.ts`;
- `features/catalogs/components/clearing-house-preferences.component.spec.ts`;
- `features/uat/components/nacha-inbound-simulator/nacha-inbound-simulator.component.ts`;
- `features/uat/components/nacha-inbound-simulator/nacha-inbound-simulator.component.spec.ts`.

The production consumers now import `ClearingHousesApiService` from `core/services/clearing-houses-api.service` and `ClearingHouseOption` from `core/models/clearing-house.model` wherever the type is used.

A normalized comparison with the v56 baseline after removing import declarations confirms the full non-import bodies of all ten consumer TS/spec files are unchanged.

## Contract and behavior preservation

The canonical core files were not edited:

- `core/services/clearing-houses-api.service.ts` is byte-for-byte identical to v56;
- `core/models/clearing-house.model.ts` is byte-for-byte identical to v56.

Therefore the existing service methods, endpoint paths, payloads, caching behavior, and `ClearingHouseOption` shape are unchanged. This batch changes ownership access only; it does not introduce wrappers or duplicate implementations.

The two ACH-cycles files differ from v56 only by removal of the two reexport statements. `AchCyclesApiService` behavior and the ACH-cycle model definitions are otherwise identical.

## Static validation

PASS for the requested static scope.

Before adding this checkpoint, the baseline comparison showed exactly 12 changed frontend paths: the 10 import-only consumer files plus the 2 ACH-cycles files from which the reexports were removed.

Confirmed:

- `0` imports of `ClearingHousesApiService` from an ACH-cycles path;
- `0` imports of `ClearingHouseOption` from `ach-cycle.model`;
- the ACH-cycles service no longer reexports `ClearingHousesApiService`;
- the ACH-cycles model no longer reexports `ClearingHouseOption`;
- exactly one canonical `ClearingHousesApiService` class definition exists in its core service file;
- exactly one canonical `ClearingHouseOption` interface definition exists in its core model file;
- all ten affected consumer non-import bodies are unchanged;
- relative imports in all 12 changed TS/spec files resolve statically;
- feature-to-feature import-edge comparison against v56 found `0` new feature-to-feature edges;
- B9a1 files `financial-institution.model.ts` and `financial-institutions-api.service.ts` are byte-for-byte intact;
- B9a2 shared context-navigation TS/SCSS/spec are byte-for-byte intact;
- canonical clearing-house core service/model files are byte-for-byte intact;
- all `1474` backend `src/` files are byte-for-byte intact.

Global TypeScript `5.8.3` `transpileModule(..., reportDiagnostics=true)` syntax validation on all 12 changed TS/spec files reported `0` syntax errors.

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
- observed output includes `npm notice PING https://registry.npmjs.org/`.

No `npm ci` was run or retried.

Affected Angular tests were actually attempted in one command for:

- ACH cycle list spec;
- ACH cycle form spec;
- NACHA export spec;
- clearing-house preferences spec;
- NACHA inbound simulator spec.

Result:

- exit code: `127`;
- result: `ng: not found`.

SPA build was actually attempted:

- command: `npm run build`;
- exit code: `127`;
- result: `ng: not found`.

No backend executable validation was run because backend is explicitly out of scope and statically unchanged.

## Files changed by this batch

Import-only consumer changes:

- `web/ach-interbank-ui/src/app/features/ach-cycles/components/ach-cycle-list.component.ts`;
- `web/ach-interbank-ui/src/app/features/ach-cycles/components/ach-cycle-list.component.spec.ts`;
- `web/ach-interbank-ui/src/app/features/ach-cycles/components/ach-cycle-form.component.ts`;
- `web/ach-interbank-ui/src/app/features/ach-cycles/components/ach-cycle-form.component.spec.ts`;
- `web/ach-interbank-ui/src/app/features/ach-cycles/components/nacha-export.component.ts`;
- `web/ach-interbank-ui/src/app/features/ach-cycles/components/nacha-export.component.spec.ts`;
- `web/ach-interbank-ui/src/app/features/catalogs/components/clearing-house-preferences.component.ts`;
- `web/ach-interbank-ui/src/app/features/catalogs/components/clearing-house-preferences.component.spec.ts`;
- `web/ach-interbank-ui/src/app/features/uat/components/nacha-inbound-simulator/nacha-inbound-simulator.component.ts`;
- `web/ach-interbank-ui/src/app/features/uat/components/nacha-inbound-simulator/nacha-inbound-simulator.component.spec.ts`.

Reexport retirement:

- `web/ach-interbank-ui/src/app/features/ach-cycles/services/ach-cycles-api.service.ts`;
- `web/ach-interbank-ui/src/app/features/ach-cycles/models/ach-cycle.model.ts`.

Documentation:

- `docs/ai/SPA-4C3B9A3-ACH-CYCLES-CLEARING-HOUSE-REEXPORT-CHECKPOINT.md`.

## Final status

`ACH_CYCLES_CLEARING_HOUSE_REEXPORT_4C3B9A3_IMPLEMENTED_VALIDATION_PENDING`

Implementation and static validation are complete. Angular executable validation remains pending because `ng` is unavailable and the single registry check timed out. No VERIFIED/UAT/release claim is made.
