# SPA 4C.3B7a1 — ACH Cycles List Checkpoint

## Baseline

`ACHInterbank-v49-spa-4C3B6b-clearing-house-rules-retirement.zip`

## Scope

This lot migrates only the productive `/ach-cycles` list surface implemented by `AchCycleListComponent` onto the 4C.1 foundation.

Out of scope and unchanged:

- ACH cycle new/edit form;
- NACHA-M export;
- ACH cycle routing;
- backend;
- B7a2 and later work;
- B9 boundary cleanup.

The requested context was read before implementation: the B0 inventory, the B6b retirement checkpoint, and the 4C.1 foundation checkpoint.

## Behavior preserved

The existing list behavior remains in place:

- `AchCyclesApiService.search()` remains the cycle query API;
- clearing houses are still loaded through the existing `ClearingHousesApiService` re-export;
- `clearingHouseCode` query-param resolution remains unchanged;
- filters remain `clearingHouseId`, `startDate`, and `endDate`;
- paging remains `page` / `pageSize` with initial values `1` / `10`;
- `submit()` still resets `page` to `1` and then calls `load()`;
- `formatDate()`, `formatTime()`, and `formatStatus()` are unchanged;
- create navigation remains `/ach-cycles/new`;
- edit navigation remains `/ach-cycles/:id/edit`;
- export navigation remains `/ach-cycles/nacha/export`.

The only TypeScript state added is `loadError`, used strictly to surface the already-existing cycle-query failure through the shared grid error presentation. It is reset when a query starts and set when the existing `search()` error handler runs. The existing notification message remains unchanged.

No API/service/model contract was changed.

## B5a route ownership preserved

The `Reglas de procesamiento` link remains exactly context-sensitive:

- with a selected `clearingHouseId`: `/clearing-houses/{id}/cycles`;
- without a selected chamber: `/clearing-houses`.

No routing file was edited.

## Foundation 4C.1 adoption

The page already used `app-page-header`; it remains the page header.

Local visual migration performed:

- native clearing-house `<select>` -> `MatFormField` + `MatSelect`;
- native date controls -> `MatFormField` + `matInput` date inputs;
- filter, row, create, and export buttons -> Material buttons;
- existing `app-table` is retained as explicitly required;
- loading and empty presentation continue through `app-table` -> `UiGrillaEmpresarial`, which already uses `UiEstadoCarga` and `UiEstadoVacio`;
- the existing query error is now passed to `app-table`, which uses `UiEstadoError` through `UiGrillaEmpresarial`;
- all four local hardcoded cycle-tab hex colors were replaced by foundation tokens.

No MatTable or direct `UiGrillaEmpresarial` replacement was introduced. No new primitive was created.

## Boundary debt deliberately preserved

The historical re-exports remain unchanged:

- `ClearingHousesApiService` from `ach-cycles-api.service.ts`;
- `ClearingHouseOption` from `ach-cycle.model.ts`.

Their B9 cleanup was not started. No new feature-to-feature import was introduced.

## Focused tests updated

Only `ach-cycle-list.component.spec.ts` was updated.

The focused spec covers:

- initial clearing-house and cycle load;
- retained time/status formatting on loaded rows;
- `clearingHouseCode` query-param resolution;
- chamber/date filters and page reset to `1`;
- `page` / `pageSize` pagination behavior;
- B5a canonical processing-rules link with and without chamber context;
- create/edit/export navigation;
- shared loading state;
- shared error state plus the pre-existing notification;
- shared empty state;
- Material filter/action controls while retaining `app-table`.

## Executable validation actually attempted

Observed environment:

- Node: `v22.16.0`
- npm: `10.9.2`
- `web/ach-interbank-ui/node_modules/.bin/ng`: absent

Exactly one short registry check was run:

- command: `timeout 12s npm ping`
- exit code: `124`
- output reached: `npm notice PING https://registry.npmjs.org/`

No `npm ci` was run or retried.

Focused test was actually attempted:

- command: `npm test -- --watch=false --browsers=ChromeHeadless --include=src/app/features/ach-cycles/components/ach-cycle-list.component.spec.ts`
- exit code: `127`
- result: `ng: not found`

SPA build was actually attempted:

- command: `npm run build`
- exit code: `127`
- result: `ng: not found`

Global TypeScript `5.8.3` `transpileModule` static syntax check reported:

- `ach-cycle-list.component.ts`: `0` syntactic diagnostics;
- `ach-cycle-list.component.spec.ts`: `0` syntactic diagnostics.

No backend executable validation was run because backend is out of scope and statically unchanged.

## Static validation

PASS.

Before this checkpoint was added, the repository comparison against v49 showed exactly four changed files and no added/deleted files:

- `ach-cycle-list.component.ts`
- `ach-cycle-list.component.html`
- `ach-cycle-list.component.scss`
- `ach-cycle-list.component.spec.ts`

Confirmed byte-for-byte unchanged against v49:

- all 4 `ach-cycle-form.component.*` files;
- all 7 NACHA-export component/service/model files covered by the B7a1 boundary check;
- `ach-cycles-routing.module.ts`;
- all 29 frontend routing TS files;
- all 5 `ach-cycles` service/model files;
- all 1474 backend files under repository `src/`.

Additional static checks:

- zero new feature-to-feature imports in the changed component;
- zero hardcoded hex/rgb values in the list TS/HTML/SCSS surface;
- zero native `<select>` controls in the target template;
- all target `<button>` elements use Material button directives;
- both target date inputs use `matInput`;
- `app-table` remains present.

## Final status

`ACH_CYCLES_LIST_4C3B7A1_IMPLEMENTED_VALIDATION_PENDING`

Implementation and static validation are complete. Angular executable validation remains pending because `ng` is unavailable and the registry check timed out. No VERIFIED/UAT/release claim is made.
