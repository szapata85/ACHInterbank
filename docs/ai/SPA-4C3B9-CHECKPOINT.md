# SPA 4C.3B9 — READ-ONLY Closure Checkpoint

## Baseline

- ZIP: `ACHInterbank-v73-spa-4C3B9a19-incoming-nacha-ownership.zip`
- Expected SHA-256: `8512fc5ad1baf06d45ced1f0cf35c2f94878788d69f73facd2165d8ecad5f552`
- Observed SHA-256: `8512fc5ad1baf06d45ced1f0cf35c2f94878788d69f73facd2165d8ecad5f552`
- Baseline integrity: **PASS**

This checkpoint is read-only with respect to application/backend source. No source, routes, API contracts, package files, Angular/TypeScript configuration, HTML, SCSS, or TS was modified. This markdown file is the only checkpoint artifact added.

## Static boundary scan

A read-only resolver scanned `web/ach-interbank-ui/src/app/**/*.ts`, resolving static imports/exports, dynamic `import()`, and relative/baseUrl-local module paths. `tsconfig.json` has `baseUrl: "./"` and no `paths` aliases.

| Boundary | Count |
|---|---:|
| feature -> feature TypeScript | **0** |
| core -> features TypeScript | **0** |
| shared -> features TypeScript | **0** |

SCSS scan covered relative `@use`, `@forward`, `@import`, plus Angular TS `styleUrl/styleUrls` references:

| SCSS boundary | Count |
|---|---:|
| SCSS directive cross-feature | **0** |
| TS stylesheet metadata cross-feature | **0** |
| Total SCSS cross-feature | **0** |

Read-only scan result:

```text
TS_FEATURE_TO_FEATURE 0
TS_CORE_TO_FEATURES 0
TS_SHARED_TO_FEATURES 0
SCSS_DIRECTIVE_CROSS_FEATURE 0
SCSS_STYLE_METADATA_CROSS_FEATURE 0
```

## Obvious duplicate / legacy move residue scan

Checks performed against the post-B9 tree:

- duplicate Angular component class names: **0**;
- duplicate Angular component selectors: **0**;
- known legacy B9 move paths still present: **0**;
- representative moved-owner definitions (`AchColombiaFileExchangeService`, `SoapIntegrationSettingsComponent`, `RolesApiService`, `ClearingHouseCycleConfigsApiService`, `CustomersApiService`, `ReportsApiService`, `NachaConfigApiService`, `NachaOperationalDashboardComponent`, `NachaOperationalFileDetailComponent`): **exactly 1 definition each**.

This is an “obvious duplicates” check, not a semantic whole-program equivalence proof.

## Incoming NACHA public routes

The canonical parent remains in `src/app/app-routing.module.ts`:

- line 283: `path: 'incoming-nacha-command-center'`
- lines 291-294: lazy-loads `IncomingNachaCommandCenterModule`

The child routes remain in `features/incoming-nacha-command-center/incoming-nacha-command-center-routing.module.ts`:

- lines 12-16: `path: ''` -> `/incoming-nacha-command-center`
- lines 18-22: `path: 'files/:fileId'` -> `/incoming-nacha-command-center/files/:fileId`
- lines 24-28: `path: 'ingestions/:fileId'` -> `/incoming-nacha-command-center/ingestions/:fileId`

Historical compatibility redirects remain in `features/nacha-operational/nacha-operational-routing.module.ts`, mounted under parent `/ach`:

- lines 10-13: `/ach/nacha/operational-dashboard` -> `/incoming-nacha-command-center`
- lines 15-17: `/ach/nacha/operational-dashboard/files/:fileId` -> `/incoming-nacha-command-center/files/:fileId`

No route or guard was changed by this checkpoint.

## Backend diff evidence across B9

All **19/19** B9 artifacts (`B9a1` through `B9a19`, including the read-only `B9a8` inventory) are present. Their immediate-baseline SHA/diff evidence records the backend as unchanged at every B9 step. The B9a8 inventory explicitly records preservation of all baseline SHA-256 values and **1474 backend files byte-for-byte intact**; B9a19 records `src/`: **1474 files, 0 changed**.

Current baseline backend manifest:

```text
backend_files=1474
backend_manifest_sha256=eae4abfe54896d8bb12eb196fe91381754ae8934e75ec4e5fd1fd0ef30aa0a56
```

The v74 checkpoint ZIP is also diff-checked against the supplied v73 baseline for `src/`; expected result: **0 backend differences**. Thus the historical B9 diff chain and this checkpoint both preserve backend unchanged.

## Angular executable validation

Only the requested path was checked:

```text
web/ach-interbank-ui/node_modules/.bin/ng
ng: not found
```

No `npm ping`, no `npm ci`, no registry retry, no Angular build, and no Angular tests were executed.

Therefore this checkpoint does **not** declare the SPA VERIFIED.

## Commands / evidence summary

```bash
sha256sum ACHInterbank-v73-spa-4C3B9a19-incoming-nacha-ownership.zip

# Read-only TypeScript + SCSS resolver over src/app
python3 <read-only-boundary-scan>

# Public route evidence
nl -ba web/ach-interbank-ui/src/app/app-routing.module.ts | sed -n '280,318p'
nl -ba web/ach-interbank-ui/src/app/features/incoming-nacha-command-center/incoming-nacha-command-center-routing.module.ts | sed -n '1,60p'
nl -ba web/ach-interbank-ui/src/app/features/nacha-operational/nacha-operational-routing.module.ts | sed -n '1,49p'

# Backend manifest
find src -type f -print0 | sort -z | xargs -0 sha256sum > /tmp/b9_backend_manifest.sha256
sha256sum /tmp/b9_backend_manifest.sha256

# Angular CLI presence only
test -x web/ach-interbank-ui/node_modules/.bin/ng && web/ach-interbank-ui/node_modules/.bin/ng version || echo 'ng: not found'
```

## State

`4C3B9_CHECKPOINT_STATIC_OK_ANGULAR_VALIDATION_PENDING`

No next lot was started.
