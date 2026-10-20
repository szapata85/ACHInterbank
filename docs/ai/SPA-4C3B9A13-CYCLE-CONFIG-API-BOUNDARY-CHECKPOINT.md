# SPA 4C.3B9a13 — Cycle Config API Boundary checkpoint

Baseline: `ACHInterbank-v66-spa-4C3B9a12-roles-boundary.zip`

## Scope

This micro-batch removes only the `operational-restrictions -> transactions` boundary caused by the clearing-house cycle configuration API and its shared contracts.

No backend, routing, permissions, transaction/cycle business behavior, or other B9 boundary is changed.

## Ownership change

The API service moved from:

- `features/transactions/services/clearing-house-cycle-configs-api.service.ts`

to the canonical transverse owner:

- `core/services/clearing-house-cycle-configs-api.service.ts`

The following contracts were extracted from `features/transactions/transactions.models.ts` into:

- `core/models/clearing-house-cycle-config.model.ts`

Moved contracts:

- `ClearingHouseCycleConfigItem`
- `ClearingHouseCycleConfigFilters`
- `UpsertCycleConfigRequest`
- `InactivateCycleConfigRequest`

Direct byte comparison against v66 confirms the extracted contract block is identical. The remainder of `transactions.models.ts` is byte-for-byte the v66 file with only that exact block removed; `CycleStatusFilter`, `CycleValidityFilter`, and all other transaction models remain in place.

No wrapper or re-export was left under `transactions`.

## Service contract preserved

Direct comparison after removing import declarations confirms the moved `ClearingHouseCycleConfigsApiService` body is identical to v66.

Preserved exactly:

- `basePath = 'clearing-house-cycle-configs'`
- `getByClearingHouse(filters)`
- `getCurrentByClearingHouse(clearingHouseId, effectiveAt?)`
- `createVersion(payload)`
- `inactivate(id, payload)`
- current query parameter construction and payloads
- `map((items) => items ?? [])` in both read methods

Only the imports were adjusted so `ApiService` and the moved contracts resolve inside `core`.

## Consumers updated

Only imports changed in these four consumers/specs:

- `features/transactions/components/cycle-config-management/cycle-config-management.component.ts`
- `features/transactions/components/cycle-config-management/cycle-config-management.component.spec.ts`
- `features/operational-restrictions/operational-restrictions.component.ts`
- `features/operational-restrictions/operational-restrictions.component.spec.ts`

`CycleConfigManagementComponent` continues importing the local transaction-only filters `CycleStatusFilter` and `CycleValidityFilter` from `transactions.models.ts`.

Comparison after removing import declarations is identical to v66 for all four files. No functional body changed.

## Static validation

Pre-checkpoint SHA-256 comparison against v66:

- baseline files: **3608**
- modified baseline files: **5**
- added files before checkpoint: **2**
- removed files: **1**
- backend `src/` files: **1474**
- backend changes: **0**
- B9a1–B9a12 checkpoint files changed: **0**

The five modified baseline files are exactly the four import-only consumers/specs plus `transactions.models.ts`. The two additions are the new core model/service; the one removal is the legacy transaction service.

Boundary checks:

- `ClearingHouseCycleConfigsApiService` definitions: **1**, in core
- each of the four moved contracts has exactly **1** definition, in core
- references to the legacy service path: **0**
- `operational-restrictions -> transactions` imports for this boundary: **0**
- imports `core -> features`: **0**
- imports `shared -> features`: **0**
- concrete TypeScript feature-to-feature imports: **20 -> 17**
- new feature-to-feature imports versus v66: **0**
- removed concrete imports: **3**, all from `operational-restrictions -> transactions`
- backend remains byte-for-byte unchanged

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

`CYCLE_CONFIG_API_BOUNDARY_4C3B9A13_IMPLEMENTED_VALIDATION_PENDING`
