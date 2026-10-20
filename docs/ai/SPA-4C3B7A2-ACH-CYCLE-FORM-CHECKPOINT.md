# SPA 4C.3B7a2 — ACH Cycle Form Checkpoint

## Baseline

`ACHInterbank-v50-spa-4C3B7a1-ach-cycles-list.zip`

## Scope

This lot migrates only `AchCycleFormComponent`, used by:

- `/ach-cycles/new`
- `/ach-cycles/:id/edit`

Out of scope and unchanged:

- `/ach-cycles` list from B7a1;
- NACHA export;
- ACH cycles routing;
- services/models/contracts;
- backend;
- B9 boundary/re-export cleanup;
- B7b and later work.

The B7a1 checkpoint and the 4C.1 foundation checkpoint were read before implementation.

## Functional logic preserved

The TypeScript class body from `export class AchCycleFormComponent` onward is byte-for-byte equivalent in content to the v50 baseline. The only component TypeScript changes are Angular Material presentation imports and the standalone `imports` array.

Preserved unchanged:

- `ClearingHousesApiService.list()`;
- `AchCyclesApiService.getById()`;
- `getCurrentConfigurations()`;
- `create()`;
- `update()`;
- `configurationContextChanged()`;
- `configurationLabel()`;
- `save()`;
- `toErrorMessage()`;
- required `clearingHouseId`;
- required `processingDate`;
- required `clearingHouseCycleConfigId`;
- `rescheduleOnHoliday`;
- `isSaving` double-save guard;
- successful navigation to `/ach-cycles`.

No concurrency/`expectedVersion` behavior was introduced.

## Current configuration behavior preserved

The query remains exactly:

`getCurrentConfigurations(clearingHouseId, processingDate)`

The client filter remains exactly:

`item.isActive && item.isCurrent`

When chamber/date context changes, the existing logic still:

- clears only `clearingHouseCycleConfigId` when the current selection is incompatible;
- preserves `processingDate`;
- preserves `rescheduleOnHoliday`;
- never invents a configuration.

The save payload still takes canonical values from the selected configuration:

- `id` -> `clearingHouseCycleConfigId`;
- `cycleName`;
- `startTime`;
- `endTime`;
- `cutoffTime`.

`SaveAchCycleRequest` was not modified.

## Edit flow preserved

The edit flow remains unchanged:

- read route param `:id`;
- `getById(id)`;
- patch chamber/date/configuration/holiday flag;
- load current configurations with the existing selected configuration id;
- preserve that id only when it remains in the active/current set;
- `update(id, payload)`.

## Foundation 4C.1 adoption

The visual migration is local to the form:

- local `<h2>` title -> `app-page-header`;
- chamber native `<select>` -> `MatFormField` + `MatSelect`;
- operating date -> `MatFormField` + `matInput` date input;
- cycle configuration native `<select>` -> `MatFormField` + `MatSelect`;
- holiday native checkbox -> `MatCheckbox`;
- cancel/save actions -> Material buttons;
- configuration loading -> `UiEstadoCarga`;
- functional/backend error -> `UiEstadoError` while retaining `role="alert"`;
- `app-error-message` remains in place for control validation messages;
- the existing inline `No hay configuraciones activas y vigentes...` message remains inline as requested.

No new UI primitive was created.

The legacy SCSS dependency:

`@use '../../admin/components/user-form.component' as *;`

was removed. The replacement SCSS is local and uses foundation design tokens. There are zero hardcoded hex/rgb values in the target TS/HTML/SCSS surface.

## Boundary debt deliberately preserved

The existing re-exports remain unchanged for B9:

- `ClearingHousesApiService` from `ach-cycles-api.service.ts`;
- `ClearingHouseOption` from `ach-cycle.model.ts`.

No new feature-to-feature import was introduced.

## Focused tests updated

Only `ach-cycle-form.component.spec.ts` was updated.

Coverage includes:

- initial clearing-house load;
- create path and exact canonical payload;
- edit `:id`, patch, preserved selected configuration, and `update()`;
- active/current configuration filtering;
- incompatible context clearing only the configuration id;
- preservation of processing date and holiday rescheduling flag;
- payload times/name/id sourced from the selected canonical configuration;
- shared loading state while configurations are pending;
- backend save error preserving the form;
- backend detail shown without `[object Object]`;
- double-save prevention;
- Material/foundation controls present and native select/checkbox controls retired.

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

- command: `npm test -- --watch=false --browsers=ChromeHeadless --include=src/app/features/ach-cycles/components/ach-cycle-form.component.spec.ts`
- exit code: `127`
- result: `ng: not found`

SPA build was actually attempted:

- command: `npm run build`
- exit code: `127`
- result: `ng: not found`

Global TypeScript `5.8.3` `transpileModule` static syntax check reported:

- `ach-cycle-form.component.ts`: `0` diagnostics;
- `ach-cycle-form.component.spec.ts`: `0` diagnostics.

No backend executable validation was run because backend is out of scope and statically unchanged.

## Static validation

PASS.

Before this checkpoint was added, comparison against v50 showed exactly four changed files and no added/deleted files:

- `ach-cycle-form.component.ts`
- `ach-cycle-form.component.html`
- `ach-cycle-form.component.scss`
- `ach-cycle-form.component.spec.ts`

Confirmed byte-for-byte unchanged against v50:

- all 4 B7a1 `ach-cycle-list.component.*` files;
- all 7 NACHA-export component/service/model files covered by the boundary check;
- `ach-cycles-routing.module.ts`;
- all 29 frontend routing TS files;
- all 5 `ach-cycles` service/model files;
- all 1474 backend files under repository `src/`.

Additional static checks:

- component class body/functional TypeScript unchanged from v50;
- zero new feature-to-feature imports;
- zero hardcoded hex/rgb values in form TS/HTML/SCSS;
- zero legacy SCSS `@use` dependency on the admin user form;
- zero native `<select>` elements in the target template;
- zero native checkbox inputs in the target template;
- `app-page-header`, Material selects/date/checkbox/buttons, `UiEstadoCarga`, `UiEstadoError`, and `app-error-message` are present.

## Final status

`ACH_CYCLE_FORM_4C3B7A2_IMPLEMENTED_VALIDATION_PENDING`

Implementation and static validation are complete. Angular executable validation remains pending because `ng` is unavailable and the single registry check timed out. No VERIFIED/UAT/release claim is made.
