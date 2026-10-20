# SPA 4C.3B9a15 — ACH Colombia File Exchange Boundary checkpoint

Baseline: `ACHInterbank-v68-spa-4C3B9a14-customers-api-boundary.zip`

## Scope

This micro-batch removes only the `ach-colombia-mft-administration -> ach-colombia-file-exchange` frontend boundary caused by `AchColombiaFileExchangeService` and `ManagedMftAdministration`.

No backend, routing, permissions, endpoint, payload, business behavior, or other B9 boundary is changed.

## Ownership change

The service moved from:

- `features/ach-colombia-file-exchange/ach-colombia-file-exchange.service.ts`

to:

- `core/services/ach-colombia-file-exchange.service.ts`

The transfer contracts moved from:

- `features/ach-colombia-file-exchange/ach-colombia-file-exchange.models.ts`

to:

- `core/models/ach-colombia-file-exchange.model.ts`

`ManagedMftAdministration`, previously declared at the end of the feature service file, now lives in the same core model.

The legacy service/model files were deleted after confirming zero remaining references. No wrapper or legacy re-export remains.

The `ach-colombia-file-exchange` feature keeps its visual component/template/styles and therefore retains visual ownership only.

## Contracts preserved

Direct byte construction/comparison against the v68 sources confirms that the new core model contains the original transfer model file unchanged plus the original `ManagedMftAdministration` interface unchanged.

Preserved exactly:

- `TransferDirection`
- `ExecutionOrigin`
- `TransferStatus`
- `TransferSummary`
- `TransferEvent`
- `TransferDetail`
- `TransferFilter`
- `ManagedMftAdministration`

There is exactly one definition of each of these contracts in the resulting SPA source tree.

## Service preserved

Comparison after removing import declarations confirms that the `AchColombiaFileExchangeService` class body is identical to v68.

Preserved exactly:

- `base = 'api/ach-colombia/file-exchange'`
- `list()`
- `detail()`
- `executeOutbound()`
- `executeInbound()`
- `retry()`
- `reprocess()`
- `archive()`
- `retire()`
- `download()`
- `administration()`
- `updateAdministration()`
- `setCredential()`
- all current endpoints, `HttpParams`, payloads, response types, `ApiService.resolveUrl()` usage, and HTTP behavior

The only service changes are its ownership path, imports to core dependencies/models, and removal of the moved `ManagedMftAdministration` declaration from the service file.

## Consumers updated

Only imports changed in these four consumers/specs:

- `features/ach-colombia-file-exchange/ach-colombia-file-exchange.component.ts`
- `features/ach-colombia-file-exchange/ach-colombia-file-exchange.component.spec.ts`
- `features/ach-colombia-mft-administration/ach-colombia-mft-administration.component.ts`
- `features/ach-colombia-mft-administration/ach-colombia-mft-administration.component.spec.ts`

Comparison after removing import declarations is byte-identical to v68 for all four files. No functional body changed.

## Static validation

Pre-checkpoint comparison against v68:

- modified baseline files: **4**
- added files before checkpoint: **2**
- removed files: **2**
- modified consumers are exactly the four import-only files listed above
- additions are exactly the new core model/service
- removals are exactly the two legacy feature model/service files

Boundary checks:

- `ach-colombia-mft-administration -> ach-colombia-file-exchange`: **0** remaining TypeScript imports
- references to the legacy service/model paths: **0**
- `AchColombiaFileExchangeService` definitions: **1**, in core
- each moved model/contract definition: **1**, in core
- imports `core -> features`: **0**
- new feature-to-feature imports versus v68: **0**
- concrete feature-to-feature TypeScript imports detected by the same scanner: **13 -> 11**; the two removed imports are the component/spec MFT boundary targeted by this batch
- B9a1–B9a14 checkpoint/inventory artifacts: **14 / 0 changes**
- backend `src/` files: **1474 / 0 changes**

## Executable validation

Per this micro-batch instruction, only Angular CLI availability was checked:

```text
ng: not found
```

`web/ach-interbank-ui/node_modules/.bin/ng` is absent.

Not executed by instruction:

- `npm ping`
- `npm ci`
- Angular tests
- Angular build
- backend build/tests

## Final state

`ACH_COLOMBIA_FILE_EXCHANGE_BOUNDARY_4C3B9A15_IMPLEMENTED_VALIDATION_PENDING`
