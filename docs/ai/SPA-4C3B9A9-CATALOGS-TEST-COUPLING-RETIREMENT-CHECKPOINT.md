# SPA 4C.3B9a9 — Catalogs test coupling retirement checkpoint

Baseline: `ACHInterbank-v62-spa-4C3B9a8-residual-boundary-inventory.zip`

## Scope

This micro-batch removes only the test-only `catalogs -> customer-third-parties` cross-feature boundary identified by B9a8. No productive code, routing, services, models, backend, or other B9 boundary is changed.

## References confirmed before change

B9a8 identified the residual boundary in exactly one catalogs spec:

- `web/ach-interbank-ui/src/app/features/catalogs/components/catalogs-phase3.component.spec.ts`

That spec imported:

- `CustomerThirdPartiesComponent` from `features/customer-third-parties/components/customer-third-parties.component`
- `CustomerThirdPartiesService` from `features/customer-third-parties/services/customer-third-parties.service`

Repository search confirmed these were the only `catalogs -> customer-third-parties` references. They were test-only; no productive catalogs file imported the feature.

The two foreign-feature tests embedded in the catalogs phase-3 spec directly instantiated `CustomerThirdPartiesComponent` and mocked `CustomerThirdPartiesService`. They were not required to exercise any catalogs behavior. `customer-third-parties` already owns focused component and service specs, so no replacement cross-feature stub is necessary.

## Change performed

Modified only:

- `web/ach-interbank-ui/src/app/features/catalogs/components/catalogs-phase3.component.spec.ts`

Removed only:

- import of `CustomerThirdPartiesComponent`
- import of `CustomerThirdPartiesService`
- test `CustomerThirdParties_ShouldLoadInitialPage`
- test `CustomerThirdParties_ShouldShowError_WhenApiFails`
- the providers/mocks local to those two removed foreign-feature tests

The remaining catalogs tests for Financial Institutions, Bank Holidays, and Catalog Types are unchanged.

No productive source file was modified.

## Static validation

Repository search after the change:

- `catalogs -> customer-third-parties` imports/references: **0**
- new feature-to-feature imports introduced: **0**

SHA-256 manifest comparison against the extracted v62 baseline, before this checkpoint was added:

- baseline files modified: **1**
- modified path: `web/ach-interbank-ui/src/app/features/catalogs/components/catalogs-phase3.component.spec.ts`
- added paths: **0**
- removed paths: **0**
- productive catalogs files changed: **0**
- `features/customer-third-parties` files changed: **0** (9 files remain byte-for-byte identical)
- backend `src/` files changed: **0** (1474 files remain byte-for-byte identical)

Because the only baseline modification is the one catalogs spec and its diff consists only of import/test removal, B9a1–B9a8 artifacts and all unrelated code remain byte-for-byte intact.

## Executable validation

Per this micro-batch instruction, only Angular CLI availability was checked.

Result:

```text
ng: not found
```

`web/ach-interbank-ui/node_modules/.bin/ng` is absent, therefore the affected Angular spec was not executed.

Not executed by instruction:

- `npm ping`
- `npm ci`
- Angular build
- unrelated tests
- backend build/tests

## Final state

`CATALOGS_TEST_COUPLING_4C3B9A9_IMPLEMENTED_VALIDATION_PENDING`
