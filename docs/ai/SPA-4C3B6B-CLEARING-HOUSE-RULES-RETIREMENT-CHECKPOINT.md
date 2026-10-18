# SPA 4C.3B6b — Clearing-House Rules Frontend Retirement Checkpoint

## Baseline

`ACHInterbank-v48-spa-4C3B6a-transaction-policies.zip`

## Scope

This lot resolves only the residual frontend implementation formed by:

- `ClearingHouseTransactionRulesComponent`
- `ClearingHouseTransactionRulesApiService`

The productive `/clearing-houses/:id/transaction-policies` surface, its service/models, routing, permissions, and the backend rule service/controllers remain out of scope and unchanged.

No work from B7 or B9 was started.

## Pre-removal reference inspection

A complete frontend reference search was run before deletion for:

- `ClearingHouseTransactionRulesComponent`
- `ClearingHouseTransactionRulesApiService`
- `clearing-house-transaction-rules`

The only references found under `web/ach-interbank-ui/src/app` were:

1. the component implementation itself;
2. the component spec;
3. the residual API service implementation itself;
4. the API service spec;
5. the component's import/injection of that same residual API service.

No productive route, module import, declaration, export, template consumer, or other frontend consumer referenced the component.

No productive frontend consumer referenced `ClearingHouseTransactionRulesApiService` outside the residual component.

`TransactionsModule` did not import or declare the component because it was standalone, so there was no module registration to remove.

The residual service used `providedIn: 'root'`; there was no provider registration elsewhere to remove.

## Legacy route preserved

The existing legacy route remains unchanged:

`/transactions/clearing-house-rules` → redirect to `/clearing-houses`

`transactions-routing.module.ts` and its spec are byte-for-byte identical to the v48 baseline.

No new route and no replacement screen were created.

## Productive transaction-policies dependency check

The productive transaction-policies surface does **not** depend on the retired frontend service.

It continues to use:

- `TransactionPoliciesComponent`
- `TransactionPoliciesService`
- `transaction-policies.models.ts`

A targeted search found no reference from `features/clearing-houses` to:

- `ClearingHouseTransactionRulesApiService`
- `clearing-house-transaction-rules-api.service`

All four productive `transaction-policies.component.*` files are byte-for-byte identical to v48.

## Files removed

Exactly six frontend files were removed:

- `web/ach-interbank-ui/src/app/features/transactions/components/clearing-house-transaction-rules/clearing-house-transaction-rules.component.ts`
- `web/ach-interbank-ui/src/app/features/transactions/components/clearing-house-transaction-rules/clearing-house-transaction-rules.component.html`
- `web/ach-interbank-ui/src/app/features/transactions/components/clearing-house-transaction-rules/clearing-house-transaction-rules.component.scss`
- `web/ach-interbank-ui/src/app/features/transactions/components/clearing-house-transaction-rules/clearing-house-transaction-rules.component.spec.ts`
- `web/ach-interbank-ui/src/app/features/transactions/services/clearing-house-transaction-rules-api.service.ts`
- `web/ach-interbank-ui/src/app/features/transactions/services/clearing-house-transaction-rules-api.service.spec.ts`

Before this checkpoint was added, the full repository comparison against v48 reported:

- added files: `0`
- changed files: `0`
- deleted files: `6`

Those six deleted files are exactly the residual frontend listed above.

## Backend deliberately preserved

The backend rule implementation is **not** residual and was not removed or edited.

Current backend consumers include:

- `IClearingHouseTransactionRuleService`
- `ClearingHouseTransactionRuleService`
- `ClearingHouseTransactionRulesController`
- `ClearingHouseTransactionPoliciesController`

The productive transaction-policies controller shares `IClearingHouseTransactionRuleService`, confirming that deleting the backend would have been incorrect.

Static hash comparison against v48 confirmed all `1474` files under repository `src/` are byte-for-byte unchanged.

No backend tests, migrations, seeds, controllers, application services, persistence services, or contracts were modified.

## Final frontend residue search

After deletion, a frontend search under `web/ach-interbank-ui/src/app` for:

- `ClearingHouseTransactionRulesComponent`
- `ClearingHouseTransactionRulesApiService`
- `clearing-house-transaction-rules`

returned zero matches.

The backend route string `api/clearing-house-transaction-rules` intentionally remains in backend code and is not classified as frontend residue.

## Executable validation actually attempted

Environment observed:

- Node: `v22.16.0`
- npm: `10.9.2`
- `web/ach-interbank-ui/node_modules/.bin/ng`: absent

Exactly one short npm-registry connectivity check was executed:

- command: `timeout 12s npm ping`
- exit code: `124`
- output reached: `npm notice PING https://registry.npmjs.org/`
- cause: registry did not respond before the timeout

No repeated `npm ci` was attempted.

Related frontend tests were actually attempted:

- command: `npm test -- --watch=false --browsers=ChromeHeadless --include=src/app/features/transactions/transactions-routing.module.spec.ts --include=src/app/features/clearing-houses/transaction-policies.component.spec.ts`
- exit code: `127`
- cause: `ng: not found`

SPA build was actually attempted:

- command: `npm run build`
- exit code: `127`
- cause: `ng: not found`

No backend executable validation was run because backend is out of scope and statically unchanged.

## Static validation

PASS.

Confirmed against v48:

- zero frontend references remain to `ClearingHouseTransactionRulesComponent`;
- zero frontend references remain to `ClearingHouseTransactionRulesApiService`;
- `/transactions/clearing-house-rules` redirect remains byte-for-byte unchanged;
- productive transaction-policies files remain byte-for-byte unchanged;
- `transactions-routing.module.ts` and its spec remain byte-for-byte unchanged;
- all `1474` backend `src/` files remain byte-for-byte unchanged;
- no service/model used by productive transaction-policies was changed;
- no functional routing/permission configuration was changed;
- no new cross-feature import was introduced;
- no B7 work was started.

## Final status

`CLEARING_HOUSE_RULES_FRONTEND_RETIREMENT_4C3B6B_IMPLEMENTED_VALIDATION_PENDING`

The residual frontend retirement is implemented. Angular executable validation remains pending because the dependency tree is unavailable and the registry check timed out; no VERIFIED claim is made.
