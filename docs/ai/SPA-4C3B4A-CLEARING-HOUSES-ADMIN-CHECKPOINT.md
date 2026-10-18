# SPA 4C.3B4a — Clearing Houses Admin Checkpoint

## Estado

`CLEARING_HOUSES_ADMIN_4C3B4A_IMPLEMENTED_VALIDATION_PENDING`

Angular tests/build remain blocked because the frontend dependency tree is not installed and the npm registry did not respond within the single short connectivity check permitted by this lot.

## Baseline exacto

- `ACHInterbank-v43-spa-4C3B3b2-ach-returns.zip`
- Scope: only the primary `/clearing-houses` administrative surface.
- No work was advanced into clearing-house cycles, transaction policies, `/transactions/cycle-configs`, `ach-cycles`, catalogs, backend, migrations or seeds.

## Superficie real inspeccionada

The route is declared directly in `app-routing.module.ts`; there is no `ClearingHousesModule` or feature routing module.

- Route: `/clearing-houses`
- Component: `ClearingHousesComponent`
- Parent route permission: `ClearingHouses.View`
- Service: `ClearingHousesService`
- Models: `clearing-houses.models.ts`
- Table: Angular Material `MatTable`

The component actually uses these service operations:

- `list(search, isActive, page)` -> `GET api/clearing-houses`
- `create(input)` -> `POST api/clearing-houses`
- `update(id, input)` -> `PUT api/clearing-houses/{id}`
- `changeStatus(id, isActive)` -> `PATCH api/clearing-houses/{id}/status`
- `profiles(code)` -> `GET api/clearing-houses/nacha-profiles`
- `paymentRailOptions()` -> `GET api/clearing-houses/payment-rail-options`

`ClearingHousesService.get(id)` exists but is not consumed by the principal component and was not altered.

No delete operation exists in this surface; none was invented.

## Permisos/capabilities preservados

The component retains the existing capability checks exactly:

- create: `ClearingHouses.Create`
- update: `ClearingHouses.Update`
- status change: `ClearingHouses.ChangeStatus`
- cycle navigation: `ClearingHouses.ManageCycles`
- special-date navigation: `ClearingHouses.ManageSpecialDates`
- transaction-policy navigation: any of `Config.Read`, `Config.Manage`, `CanReadAch`, `CanManageAch`
- operational-restrictions navigation: `OperationalRestrictions.Read`

Routing and permission configuration are byte-for-byte unchanged from v43.

## Comportamiento administrativo preservado

The implementation retains the current behavior without contract changes:

- initial loading of payment-rail options and the clearing-house list;
- search plus `all | active | inactive` state filtering;
- page reset on filter application/clear;
- backend paging with page size returned by the API;
- local sorting over the loaded page;
- metrics for total, active and current-page rows;
- create flow and defaults;
- edit flow;
- local read-only detail selection;
- code normalization through `trim().toUpperCase()`;
- registered NACHA profile loading after a valid clearing-house code;
- exclusion of the `UNKNOWN` payment-rail option from the selectable catalog;
- disabling payment-rail editing while an existing clearing house is active;
- dirty-form discard confirmation through the existing Material dialog;
- activation/deactivation confirmation and status endpoint;
- success snackbars and existing backend error-detail rendering;
- navigation to transaction policies, cycles, special dates and operational restrictions.

## Campos y validaciones preservados

The create/edit form remains unchanged functionally:

- `code`: required, pattern `^[A-Z0-9][A-Z0-9_-]{1,19}$`
- `name`: required, max length 120
- `originCode`: required, max length 20
- `timeZoneId`: required, default `America/Bogota`
- `holidayStrategy`: required, default `Colombian`
- `paymentRailCode`
- `requiresNachaProfile`
- `nachaProfileId`

No enum/value, field name, payload or endpoint was changed.

## Versionamiento/concurrencia

The existing optimistic-concurrency value is preserved exactly:

- edit uses the selected row's `updatedAt`;
- save constructs `ClearingHouseInput` with `expectedUpdatedAt: selected?.updatedAt`;
- the value is still sent to `update(id, input)`.

The baseline does **not** contain a status-specific HTTP 409 branch or automatic reload/retry path in this component. Errors, including backend conflict details, flow through the existing `errorText()` logic. This behavior was preserved; no new retry, merge or concurrency semantics were introduced.

The status-change operation does not use `expectedUpdatedAt` in the current service contract; it remains `changeStatus(id, isActive)`.

## Foundation 4C.1 adoptada

The page was already Material-based. This lot performs only local foundation cleanup:

- custom hero -> canonical `app-page-header`;
- existing actions remain Material buttons/links through the header action template;
- local loading card -> `UiEstadoCargaComponent`;
- local recoverable error card -> `UiEstadoErrorComponent`, preserving retry through `load()`;
- local empty card -> `UiEstadoVacioComponent`, while preserving the separate `Limpiar filtros` action;
- state and operational-readiness chips -> `UiEtiquetaEstadoComponent` as a visual-only mapping;
- `MatTable`, paginator, sort, dialogs, form fields, menu and Material actions are preserved;
- `MatChipsModule` was removed because the local chips are no longer consumed;
- the save button's existing inline Material spinner is retained because it represents an in-button saving state, not page loading.

Visual mappings only:

- active -> `activo`, text `Activa`
- inactive -> `inactivo`, text `Inactiva`
- ready -> `exitoso`, text `Configuración completa`
- not ready -> `pendiente`, text `Configuración pendiente` in the table and `Configuración incompleta` in the existing detail wording

No functional value is changed by these labels.

The baseline already had zero `<ui-boton>` in this surface; the migrated page continues with zero usages.

## CSS local

Local brand-like hardcoded colors in this page were replaced with existing foundation tokens such as:

- `--color-text`
- `--color-text-muted`
- `--color-text-soft`
- `--color-primary`
- `--color-surface`
- `--color-surface-muted`
- `--color-border`
- `--color-warning-text`
- shared spacing/radius tokens

Dead CSS associated with the removed custom hero, state cards, empty card icon treatment and local chip states was removed.

Static search reports zero hardcoded hex/rgb/rgba values in the functional TS/HTML/SCSS for `ClearingHousesComponent`.

## Archivos modificados

Functional files:

- `web/ach-interbank-ui/src/app/features/clearing-houses/clearing-houses.component.ts`
- `web/ach-interbank-ui/src/app/features/clearing-houses/clearing-houses.component.html`
- `web/ach-interbank-ui/src/app/features/clearing-houses/clearing-houses.component.scss`
- `web/ach-interbank-ui/src/app/features/clearing-houses/clearing-houses.component.spec.ts`

Documentation:

- `docs/ai/SPA-4C3B4A-CLEARING-HOUSES-ADMIN-CHECKPOINT.md`

Before adding this checkpoint, the exact v43 diff contained only the four functional component files above.

## Tests focales actualizados

Only the primary `ClearingHousesComponent` spec was changed.

Coverage retained/added for the real behavior includes:

- list rendering and functional state/detail text;
- search/state filters;
- normalized create and double-submit prevention;
- backend functional error detail;
- activation/status operation;
- payment-rail option loading and `UNKNOWN` exclusion;
- edit/save of payment rail;
- disabled payment-rail control for active clearing houses;
- status-change functional requirement errors;
- operational-restrictions navigation;
- Material table/forms;
- `app-page-header`;
- `UiEtiquetaEstadoComponent`;
- shared loading/error/empty primitives;
- zero `<ui-boton>`;
- `expectedUpdatedAt` preservation on update;
- existing Material confirmation dialog.

No tests were weakened to mask failures.

## Validación ejecutable real

Environment observed:

- Node: `v22.16.0`
- npm: `10.9.2`
- `node_modules/.bin/ng`: absent
- global TypeScript: `5.8.3`

Single short registry check:

- command: `timeout 12s npm ping`
- exit code: `124`
- observed cause: `registry.npmjs.org` did not respond within the timeout
- observable output included: `npm notice PING https://registry.npmjs.org/`

No repeated `npm ci` attempt was made while the registry remained blocked.

Actual execution attempts:

1. Focal test
   - command: `npm test -- --watch=false --browsers=ChromeHeadless --include=src/app/features/clearing-houses/clearing-houses.component.spec.ts`
   - exit code: `127`
   - cause: `ng: not found`
2. SPA build
   - command: `npm run build`
   - exit code: `127`
   - cause: `ng: not found`

No backend tests/builds were run.

Complementary syntax-only validation with global TypeScript 5.8.3 `transpileModule(..., reportDiagnostics=true)`:

- component TS: 0 syntax diagnostics
- component spec TS: 0 syntax diagnostics

This syntax check does not replace Angular build/tests.

## Validación estática contra v43

PASS.

Confirmed byte-for-byte unchanged:

- all `features/transactions`: 79 files — therefore completed B1/B2/B3 surfaces remain intact;
- all `features/ach-cycles`: 18 files;
- all backend `src/`: 1475 files;
- all other `features/clearing-houses` files outside the four target component files;
- `clearing-houses.service.ts`;
- `clearing-houses.models.ts`;
- transaction-policies TS/HTML/SCSS/spec;
- `app-routing.module.ts` and its permissions/routes.

Additional static checks:

- zero new feature-to-feature imports in the target component;
- zero `<ui-boton>` in the target template;
- zero hardcoded hex/rgb/rgba values in target functional TS/HTML/SCSS;
- `MatTable` remains;
- cycles/policies remain untouched;
- `/transactions/cycle-configs` remains untouched;
- no backend, migration, seed, catalog or `ach-cycles` modification.

## Deuda deliberadamente pendiente

Still outside this lot:

- `/clearing-houses/:id/cycles`;
- transaction policies;
- the structurally invalid legacy `/transactions/cycle-configs` ownership/routing debt identified in 4C.3B0;
- `ach-cycles` migration;
- catalog ownership/boundaries;
- B9 cross-feature boundary cleanup;
- executable Angular validation once dependencies/registry are available.

No work was advanced to B4b/B5.
