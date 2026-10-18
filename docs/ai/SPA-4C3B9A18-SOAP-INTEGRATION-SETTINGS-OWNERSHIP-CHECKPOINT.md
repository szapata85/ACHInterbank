# SPA 4C.3B9a18 — SOAP Integration Settings Ownership Checkpoint

## Scope

Baseline: `ACHInterbank-v71-spa-4C3B9a17-nacha-config-api-boundary.zip`

Objective completed: remove the final `integrations -> admin` ownership boundary by moving `SoapIntegrationSettingsComponent` from the admin feature to the integrations feature without changing functional behavior, routes, permissions, backend, or any other B9 boundary.

No incoming-NACHA boundary work was implemented.

## Ownership move

Moved byte-for-byte:

- `web/ach-interbank-ui/src/app/features/admin/components/soap-integration-settings.component.ts`
  -> `web/ach-interbank-ui/src/app/features/integrations/pages/soap-integration-settings.component.ts`
- `web/ach-interbank-ui/src/app/features/admin/components/soap-integration-settings.component.html`
  -> `web/ach-interbank-ui/src/app/features/integrations/pages/soap-integration-settings.component.html`
- `web/ach-interbank-ui/src/app/features/admin/components/soap-integration-settings.component.scss`
  -> `web/ach-interbank-ui/src/app/features/integrations/pages/soap-integration-settings.component.scss`
- `web/ach-interbank-ui/src/app/features/admin/components/soap-integration-settings.component.spec.ts`
  -> `web/ach-interbank-ui/src/app/features/integrations/pages/soap-integration-settings.component.spec.ts`

All four moved files are byte-for-byte identical to their baseline contents. The component continues consuming `SoapIntegrationSettingsService` directly from `core/services/soap-integration-settings.service.ts`; no wrapper or additional feature dependency was introduced.

The four legacy admin files were removed after reference validation confirmed no consumers remained.

## Routing and module adjustments

Only one existing source file required an ownership-path adjustment:

- `features/integrations/integrations-routing.module.ts`
  - import changed from `../admin/components/soap-integration-settings.component`
  - to `./pages/soap-integration-settings.component`

The remainder of the routing file is byte-for-byte identical to the baseline after normalizing only that import path.

The routed component is standalone, as it already was in the baseline. Therefore no declaration/import change was necessary in either module:

- `features/integrations/integrations.module.ts`: byte-for-byte unchanged
- `features/admin/admin.module.ts`: byte-for-byte unchanged and never declared/imported this component

`IntegrationWorkspaceComponent` and its template are byte-for-byte unchanged.

## Route, selector, permissions, and behavior preserved

Preserved exactly:

- selector: `app-soap-integration-settings`
- public route within integrations: `/integraciones/soap-settings`
- legacy redirect: `/soap-integrations -> /integraciones/soap-settings`
- top-level integrations guards: `roleGuard`, `permissionGuard`
- roles: `Admin`, `ACH.Operator`
- permissions: `CanReadAch`, `CanManageAch`
- navigation to `/integraciones/mappings`
- forms and validation behavior
- explicit edit/cancel/save behavior
- prevention of concurrent/double save
- loading, reloading, and saving states
- notifications and backend error handling
- responsive HTML/SCSS

The component's own `canManage` check remains unchanged: `Config.Manage` or `CanManageAch`.

Because the TS/HTML/SCSS/spec quartet moved without any byte changes, all business and presentation behavior is preserved by construction.

## Static validation

Before adding this checkpoint, the complete baseline diff was exactly:

- 4 files added under `features/integrations/pages` by move
- 4 legacy files removed from `features/admin/components`
- 1 routing file modified only by the component import path
- 0 other files changed

Validation results:

- references to legacy `features/admin/components/soap-integration-settings.component`: 0
- definitions of `SoapIntegrationSettingsComponent`: 1
- admin references to `SoapIntegrationSettingsComponent`: 0
- moved component imports from another feature: 0
- `core -> features`: 0 imports
- `shared -> features`: 0 imports
- backend `src/`: 1474 files, 0 changed
- B9a1-B9a17 evidence artifacts present in the baseline: unchanged

Feature-to-feature TypeScript import scan:

- baseline residual count: 5
- final residual count: 4
- removed: the single `integrations -> admin` import
- added: 0

The four remaining imports are the pre-existing `incoming-nacha-command-center -> nacha-operational` boundary and are outside this lot.

## Executable validation

Per prompt, executable validation was limited to checking Angular CLI presence:

`web/ach-interbank-ui/node_modules/.bin/ng` -> `ng: not found`

Therefore no npm registry probe, `npm ci`, Angular tests, or build was executed.

## Final state

`SOAP_INTEGRATION_SETTINGS_OWNERSHIP_4C3B9A18_IMPLEMENTED_VALIDATION_PENDING`
