# SPA 4C.3B9a19 — Incoming NACHA Component Ownership Checkpoint

## Scope

Baseline: `ACHInterbank-v72-spa-4C3B9a18-soap-integration-settings-ownership.zip`

Objective completed: remove the final four TypeScript feature-to-feature imports `incoming-nacha-command-center -> nacha-operational` by moving the incoming NACHA operational dashboard/detail UI to its command-center owner, preserving routes, selectors, permissions, behavior, backend contracts, and remaining legitimate `nacha-operational` surfaces.

No later lot was started and no backend file was modified.

## Original four residual imports

The v72 baseline contained exactly these four cross-feature imports:

1. `features/incoming-nacha-command-center/incoming-nacha-command-center.module.ts`
   - `NachaOperationalDashboardComponent` from `../nacha-operational/pages/nacha-operational-dashboard.component`
2. `features/incoming-nacha-command-center/incoming-nacha-command-center.module.ts`
   - `NachaOperationalFileDetailComponent` from `../nacha-operational/pages/nacha-operational-file-detail.component`
3. `features/incoming-nacha-command-center/incoming-nacha-command-center-routing.module.ts`
   - `NachaOperationalDashboardComponent` from `../nacha-operational/pages/nacha-operational-dashboard.component`
4. `features/incoming-nacha-command-center/incoming-nacha-command-center-routing.module.ts`
   - `NachaOperationalFileDetailComponent` from `../nacha-operational/pages/nacha-operational-file-detail.component`

No other TypeScript feature-to-feature import existed in the baseline.

## Components moved to the owner

Moved from `features/nacha-operational/pages` to `features/incoming-nacha-command-center/pages`:

- `nacha-operational-dashboard.component.ts`
- `nacha-operational-dashboard.component.html`
- `nacha-operational-dashboard.component.scss`
- `nacha-operational-dashboard.component.spec.ts`
- `nacha-operational-file-detail.component.ts`
- `nacha-operational-file-detail.component.html`
- `nacha-operational-file-detail.component.spec.ts`

The dashboard HTML/SCSS/spec and file-detail HTML/spec are byte-for-byte identical to v72. The two component TS files are byte-for-byte identical after normalizing only the owner-local model import path described below.

Selectors remain exactly:

- `app-nacha-operational-dashboard`
- `app-nacha-operational-file-detail`

## Exclusive support ownership moved with the components

Pre-move inspection showed that both components depended on support files physically owned by `nacha-operational`, and those support files were exclusive to these two components plus their own focused specs. Leaving them behind would have replaced the four original edges with new command-center -> nacha-operational edges, violating the lot objective.

Moved to `features/incoming-nacha-command-center`:

- `services/incoming-nacha-command-center.service.ts`
- `services/incoming-nacha-command-center.service.spec.ts`
- `presentation/incoming-nacha-presentation.ts`
- `presentation/incoming-nacha-presentation.spec.ts`
- baseline model `models/incoming-nacha-command-center.models.ts`

The destination already had a distinct file named `models/incoming-nacha-command-center.models.ts` with different command-center contracts. To avoid collision or DTO merging, the moved model file is now:

- `models/incoming-nacha-operational.models.ts`

Its contents and exported contracts are byte-for-byte identical to the baseline file. Only three imports were adjusted to the non-colliding filename: dashboard TS, file-detail TS, and `IncomingNachaCommandCenterService`.

The moved service logic, endpoints, request parameters, presentation helpers, and focused support specs are otherwise unchanged.

## Shared stylesheet dependency preserved inside nacha-operational

Inspection found two legitimate remaining `nacha-operational` pages that reused `nacha-operational-dashboard.component.scss` through `styleUrls`:

- `AchReconciliationConsoleComponent`
- `NachaSoapUatConsoleComponent`

Because the dashboard stylesheet had to move with the dashboard component, its baseline contents were copied byte-for-byte to the neutral owner-local file:

- `features/nacha-operational/pages/nacha-operational-console.shared.scss`

Only the `styleUrls` metadata of those two remaining components was changed to point to that local stylesheet. Their functional TS bodies are otherwise byte-for-byte identical to v72. This prevents a new SCSS feature-to-feature dependency and preserves their presentation without moving or redesigning those components.

## Routing and module adjustments

Updated only owner paths in:

- `features/incoming-nacha-command-center/incoming-nacha-command-center.module.ts`
- `features/incoming-nacha-command-center/incoming-nacha-command-center-routing.module.ts`

Both now import the two components from local `./pages/...` paths. After normalizing only those import paths, both files are byte-for-byte identical to v72.

Preserved public routes and compatibility routes:

- `/incoming-nacha-command-center`
- `/incoming-nacha-command-center/files/:fileId`
- `/incoming-nacha-command-center/ingestions/:fileId`
- historical `/ach/nacha/operational-dashboard` redirect -> `/incoming-nacha-command-center`
- historical `/ach/nacha/operational-dashboard/files/:fileId` redirect -> `/incoming-nacha-command-center/files/:fileId`

`app-routing.module.ts` is byte-for-byte unchanged. `nacha-operational-routing.module.ts` and `nacha-operational.module.ts` are byte-for-byte unchanged. Guards and permissions remain unchanged.

## Functional preservation evidence

Byte-for-byte move checks passed for:

- dashboard HTML, SCSS, spec
- file-detail HTML, spec
- exclusive model file
- service spec
- presentation implementation and spec

Normalized exact-byte checks passed for:

- dashboard TS: only model import filename changed
- file-detail TS: only model import filename changed
- command-center service TS: only model import filename changed
- command-center routing module: only component import ownership paths changed
- command-center module: only component import ownership paths changed
- reconciliation console TS: only `styleUrls` metadata changed
- SOAP/UAT console TS: only `styleUrls` metadata changed

The retained `nacha-operational-console.shared.scss` is byte-for-byte identical to the baseline dashboard SCSS.

Therefore selectors, inputs/outputs, service calls, query params, filters, pagination, list-to-detail navigation, loading/empty/error handling, tabs/timeline/detail behavior, permissions, and responsive styles were not functionally changed.

## Static validation

Post-change scan results:

- TypeScript feature -> feature imports: **0**
- SCSS feature -> feature dependencies: **0**
- `core -> features`: **0**
- `shared -> features`: **0**
- references to legacy moved component/support paths under `features/nacha-operational`: **0**
- definitions of `NachaOperationalDashboardComponent`: **1**
- definitions of `NachaOperationalFileDetailComponent`: **1**
- new feature -> feature imports: **0**
- backend `src/`: **1474 files, 0 changed**
- B9a1-B9a18 checkpoint artifacts: **18 files, 0 changed**

Before this checkpoint was added, the baseline manifest comparison showed:

- 4 existing files modified only for ownership/style metadata
- 13 files added at their new owner/local-style paths
- 12 legacy files removed
- no unrelated file changes

## Executable validation

Per prompt, executable validation was limited to checking Angular CLI presence:

`web/ach-interbank-ui/node_modules/.bin/ng` -> `ng: not found`

No npm registry probe, `npm ci`, Angular tests, or build was executed.

## Final state

`INCOMING_NACHA_OWNERSHIP_4C3B9A19_IMPLEMENTED_VALIDATION_PENDING`
