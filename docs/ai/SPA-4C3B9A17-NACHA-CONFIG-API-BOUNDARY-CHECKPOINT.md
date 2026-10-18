# SPA 4C.3B9a17 — NACHA Config API Boundary Checkpoint

## Scope

Baseline: `ACHInterbank-v70-spa-4C3B9A16-reports-api-boundary.zip`

Objective completed: remove the `uat -> nacha-config-admin` feature boundary caused by `NachaConfigApiService`, `NachaConfigProfileReadModel`, the complete NACHA configuration model contract set, and the shared read-retry helper, while preserving behavior and leaving backend untouched.

No subsequent B9 boundary was implemented.

## Ownership move

Moved:

- `web/ach-interbank-ui/src/app/features/nacha-config-admin/models/nacha-config-admin.models.ts`
  -> `web/ach-interbank-ui/src/app/core/models/nacha-config.model.ts`
- `web/ach-interbank-ui/src/app/features/nacha-config-admin/services/nacha-config-api.service.ts`
  -> `web/ach-interbank-ui/src/app/core/services/nacha-config-api.service.ts`
- `web/ach-interbank-ui/src/app/features/nacha-config-admin/services/nacha-config-read-retry.ts`
  -> `web/ach-interbank-ui/src/app/core/services/nacha-config-read-retry.ts`

The three legacy files were removed after all imports were updated. No wrappers, re-exports, or duplicate contracts/services were left under `features/nacha-config-admin`.

The model and read-retry files are byte-for-byte identical to their baseline versions. The API service differs only in the import paths required by its new core location.

## Consumers updated

Only import paths changed in the following consumers/specs:

- `features/nacha-config-admin/pages/nacha-config-official-readonly-page.component.spec.ts`
- `features/nacha-config-admin/pages/nacha-config-profile-workspace-page.component.ts`
- `features/nacha-config-admin/pages/nacha-config-profiles-page.component.ts`
- `features/nacha-config-admin/pages/nacha-config-records-page.component.ts`
- `features/nacha-config-admin/pages/nacha-config-records-page.component.spec.ts`
- `features/nacha-config-admin/pages/nacha-config-variants-fields-page.component.ts`
- `features/nacha-config-admin/pages/nacha-config-variants-fields-page.component.spec.ts`
- `features/nacha-config-admin/services/nacha-config-api.service.spec.ts`
- `features/nacha-config-admin/services/nacha-config-command.service.ts`
- `features/nacha-config-admin/services/nacha-config-query.service.ts`
- `features/nacha-config-admin/services/nacha-config-state.service.ts`
- `features/uat/components/nacha-inbound-simulator/nacha-inbound-simulator.component.ts`
- `features/uat/components/nacha-inbound-simulator/nacha-inbound-simulator.component.spec.ts`

Binary comparison after reversing only the new import paths confirms the complete remaining content of all 13 consumers/specs is identical to the baseline.

The NACHA config feature retains its pages, routing/module, presentation code, and orchestration services (`NachaConfigQueryService`, `NachaConfigCommandService`, `NachaConfigStateService`) in feature ownership.

## Contracts, service, and retry preserved

`core/models/nacha-config.model.ts` contains the complete former `nacha-config-admin.models.ts` contract set with no functional or textual changes. Static declaration checks found 24 exported model contracts and exactly one declaration of every moved contract, including `NachaConfigProfileReadModel`.

`NachaConfigApiService` remains one complete service. After reversing only its three import-location adjustments, it is byte-for-byte identical to the baseline service. Preserved behavior includes:

- `basePath = 'nacha-config'`
- `readOnlyBasePath = 'api/ach/nacha/config-profiles'`
- all read-only methods
- all administrative methods
- all endpoints, params, payloads, and response types
- the same `ApiService` HTTP behavior
- the same read retry usage

`core/services/nacha-config-read-retry.ts` is byte-for-byte identical to the baseline helper. Preserved exactly:

- `NACHA_CONFIG_READ_MAX_RETRIES = 2`
- `NACHA_CONFIG_READ_FALLBACK_DELAY_MS = 1000`
- `NachaConfigReadRetryEvent`
- `retryNachaConfigRead()`
- `retryAfterDelayMs()`
- HTTP 429-only retry behavior, `Retry-After` parsing, fallback delay, warning text, and timing behavior

Static uniqueness checks found one definition of `NachaConfigApiService`, one `NachaConfigReadRetryEvent`, and one definition of each retry constant/function.

## Boundary validation

Source validation confirms:

- `uat -> nacha-config-admin`: 0 TypeScript imports after the change
- references to the three legacy source paths: 0
- `core -> features`: 0
- `shared -> features`: 0
- new feature->feature imports: 0

Cross-feature TypeScript import scan:

- baseline feature->feature imports: 9
- final feature->feature imports before this checkpoint: 5
- removed: exactly the 4 UAT imports to NACHA config admin (service + model in component + spec)
- added: 0
- SCSS cross-feature imports: 0

The five residual TypeScript imports are unrelated pre-existing boundaries:

- 4 `incoming-nacha-command-center -> nacha-operational`
- 1 `integrations -> admin`

B9a1 through B9a16 checkpoint/evidence artifacts present in the baseline remain unchanged.

Backend static comparison:

- `src/`: 1474 files
- changed: 0

Before adding this checkpoint, the complete baseline diff was exactly:

- 3 core files added by move
- 3 legacy NACHA config files removed
- 13 consumers/specs modified only in imports
- no templates, SCSS, routing, backend, or unrelated functional files changed

## Executable validation

Per prompt, executable validation was limited to checking Angular CLI presence:

`web/ach-interbank-ui/node_modules/.bin/ng` -> `ng: not found`

Therefore no npm registry probe, `npm ci`, Angular tests, or build was executed.

## Final state

`NACHA_CONFIG_API_BOUNDARY_4C3B9A17_IMPLEMENTED_VALIDATION_PENDING`
