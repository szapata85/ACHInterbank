# SPA 4C.3B9a14 — Customers API Boundary checkpoint

Baseline: `ACHInterbank-v67-spa-4C3B9a13-cycle-config-api-boundary.zip`

## Scope

This micro-batch removes only the `transactions -> customers` boundary caused by `CustomersApiService` and the customer contracts.

No backend, routing, permissions, customer behavior, transaction behavior, or other B9 boundary is changed.

## Ownership change

The customer model moved from:

- `features/customers/models/customer.model.ts`

to:

- `core/models/customer.model.ts`

The API service moved from:

- `features/customers/services/customers-api.service.ts`

to:

- `core/services/customers-api.service.ts`

No wrapper or re-export was left under `features/customers`.

The feature still owns its components, routing, module, and the existing service spec location.

## Contracts preserved

Direct byte comparison confirms the moved model file is identical to the v67 source.

Preserved exactly:

- `CustomerSummary`
- `CustomerDetail`
- `SaveCustomerRequest`
- every field, optionality, nullability, and type in those contracts

There is exactly one definition of each contract in the resulting SPA source tree.

## Service contract preserved

Comparison after removing import declarations confirms the moved `CustomersApiService` body is identical to v67.

Preserved exactly:

- `basePath = 'customers'`
- `getAll()`
- `getById(id)`
- `create(request)`
- `update(id, request)`
- `delete(id)`
- current endpoints, payloads, return types, and `ApiService` behavior

Only imports were adjusted so the service resolves `ApiService` and customer contracts from `core`.

## Consumers updated

Only imports changed in these consumers/specs:

- `features/customers/components/customers-list.component.ts`
- `features/customers/components/customer-form.component.ts`
- `features/transactions/components/transaction-create/transaction-create.component.ts`
- `features/transactions/components/transaction-create/transaction-create.component.spec.ts`
- `features/customers/services/customers-api.service.spec.ts`

Comparison after removing import declarations is identical to v67 for all five files. No functional body changed.

The service spec remains physically in `features/customers/services/` and now imports the core service directly.

## Static validation

Pre-checkpoint SHA-256 comparison against v67:

- modified baseline files: **5**
- added files before checkpoint: **2**
- removed files: **2**
- backend `src/` files: **1474**
- backend changes: **0**

The five modified baseline files are exactly the five import-only consumers/specs listed above. The two additions are the core model/service. The two removals are the legacy feature model/service.

Boundary checks:

- `transactions -> customers` concrete TypeScript imports: **4 -> 0**
- references to the legacy customer model/service paths: **0**
- `CustomersApiService` definitions: **1**, in core
- `CustomerSummary` definitions: **1**, in core
- `CustomerDetail` definitions: **1**, in core
- `SaveCustomerRequest` definitions: **1**, in core
- imports `core -> features`: **0**
- imports `shared -> features`: **0**
- concrete TypeScript feature-to-feature imports: **17 -> 13**
- new feature-to-feature imports versus v67: **0**
- B9a1–B9a13 checkpoint artifacts changed: **0**
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

`CUSTOMERS_API_BOUNDARY_4C3B9A14_IMPLEMENTED_VALIDATION_PENDING`
