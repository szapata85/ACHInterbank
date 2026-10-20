# SPA 4C.3B9a1 — Financial Institutions Boundary Checkpoint

## Scope

Baseline: `ACHInterbank-v54-spa-4C3B8b-clearing-house-preferences.zip`.

This batch only removes the incorrect `transactions` ownership of the shared financial-institution read contract/API:

- `DestinationInstitution`;
- `FinancialInstitutionsApiService`.

No backend code and no unrelated B9 boundary were changed.

## References inspected before the move

The production consumers found for the shared read model/API were:

- `features/transactions/components/transaction-create/transaction-create.component.ts`;
- `features/ach-claims/ach-claims-list.component.ts`;
- `features/catalogs/components/clearing-house-preferences.component.ts`;
- `features/nacha-security/components/nacha-certificate-manager.component.ts`;
- `features/uat/components/nacha-inbound-simulator/nacha-inbound-simulator.component.ts`.

Additional production consumers of `DestinationInstitution` were:

- `features/catalogs/components/financial-institutions.component.ts`;
- `features/catalogs/services/financial-institution-admin.service.ts`.

Focused specs for those consumers also referenced the old paths. No barrel/index export was found for either symbol.

The inspected shared service contains no transaction-specific behavior: it injects `ApiService` and exposes only `getAll(includeInactive = false)` over `GET financial-institutions` with the existing `includeInactive` query parameter.

`DestinationInstitution` is also not transaction-specific. Its `status` property uses `FinancialInstitutionStatusEnum`. Because that enum is part of the same financial-institution contract and was also consumed outside `transactions`, it was moved with the model into the same core file so the new core contract does not depend back on a feature. Enum values remain exactly `Active = 1` and `Inactive = 2`. This is a dependency-preserving companion move, not a separate B9 boundary cleanup.

## Boundary move

New ownership:

- `core/models/financial-institution.model.ts`
  - `DestinationInstitution`;
  - `FinancialInstitutionStatusEnum`.
- `core/services/financial-institutions-api.service.ts`
  - `FinancialInstitutionsApiService`.

Removed old ownership:

- `DestinationInstitution` removed from `features/transactions/transactions.models.ts`;
- `FinancialInstitutionStatusEnum` removed from `features/transactions/transactions.types.ts`;
- `features/transactions/services/financial-institutions-api.service.ts` deleted.

There is exactly one remaining definition of each moved symbol. No wrapper, duplicate implementation, or compatibility copy was left behind.

## Contracts and behavior preserved

Exact static comparison against v54 confirmed:

- `DestinationInstitution` interface block is byte-for-byte identical after relocation;
- `FinancialInstitutionStatusEnum` block is byte-for-byte identical after relocation;
- `FinancialInstitutionsApiService` class body is byte-for-byte identical after relocation, excluding import paths required by the new directory.

The service still has exactly:

- method: `getAll(includeInactive = false)`;
- endpoint: `financial-institutions`;
- payload/query behavior: `{ params: { includeInactive } }`;
- result contract: `DestinationInstitution[]`.

No functional body in any consumer/spec was changed. Static comparison after removing import declarations confirms all affected consumer files differ only in imports.

## Imports updated

All current consumers now import the shared contract/API from `core` instead of `features/transactions`.

Updated production consumers:

- Transaction Create;
- ACH Claims;
- Clearing House Preferences;
- Financial Institutions catalog/admin service;
- NACHA Security certificate manager;
- UAT NACHA inbound simulator.

Only affected specs were updated, and only for import paths/types.

No new feature-to-feature import was introduced by this batch; every newly introduced consumer dependency resolves to `core`.

## Executable validation actually attempted

Observed environment:

- Node: `v22.16.0`;
- npm: `10.9.2`;
- global TypeScript: `5.8.3`;
- `web/ach-interbank-ui/node_modules/.bin/ng`: absent.

Exactly one short registry check was run:

- command: `timeout 12s npm ping`;
- exit code: `124`;
- output reached: `npm notice PING https://registry.npmjs.org/`.

No `npm ci` was run or retried.

Related frontend tests were actually attempted with the six affected focused specs:

- ACH Claims;
- Clearing House Preferences;
- Financial Institutions catalog;
- NACHA Security certificate manager;
- Transaction Create;
- UAT NACHA inbound simulator.

Result:

- exit code: `127`;
- result: `ng: not found`.

SPA build was actually attempted:

- command: `npm run build`;
- exit code: `127`;
- result: `ng: not found`.

Global TypeScript `5.8.3` `transpileModule` static syntax validation reported `0` errors for all 17 changed/new TypeScript/spec files checked. A relative-import resolution scan across the SPA source also passed after the move.

No backend executable validation was run because backend is explicitly out of scope and statically unchanged.

## Static validation

PASS.

Before this checkpoint was added, the v54 comparison showed exactly the expected boundary/import changes: 18 changed paths total, consisting of two new core files, one deleted old service, the two source files from which the model/enum definitions were extracted, and import-only changes in the affected production/spec consumers.

Confirmed:

- zero frontend imports of `DestinationInstitution` from `transactions`;
- zero frontend imports of `FinancialInstitutionsApiService` from `transactions`;
- zero frontend imports of the companion `FinancialInstitutionStatusEnum` from `transactions`;
- old `features/transactions/services/financial-institutions-api.service.ts` no longer exists;
- exactly one `DestinationInstitution` definition remains;
- exactly one `FinancialInstitutionsApiService` definition remains;
- exactly one `FinancialInstitutionStatusEnum` definition remains;
- endpoint and service method behavior are unchanged;
- affected consumer/spec bodies are unchanged outside imports;
- zero new feature-to-feature imports;
- all 1474 backend files under repository `src/` are byte-for-byte unchanged;
- all files outside the expected boundary/import set remained unchanged before checkpoint creation.

## Final status

`FINANCIAL_INSTITUTIONS_BOUNDARY_4C3B9A1_IMPLEMENTED_VALIDATION_PENDING`

Implementation and static validation are complete. Angular executable validation remains pending because `ng` is unavailable and the single registry check timed out. No VERIFIED/UAT/release claim is made.
