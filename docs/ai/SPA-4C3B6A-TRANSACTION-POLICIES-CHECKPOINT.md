# SPA 4C.3B6a — Transaction Policies Checkpoint

## Baseline

`ACHInterbank-v47-spa-4C3B5b-cycle-config-foundation.zip`

## Scope implemented

Only the productive transaction-policies surface was migrated to the 4C.1 frontend foundation:

- `/clearing-houses/:id/transaction-policies`
- `web/ach-interbank-ui/src/app/features/clearing-houses/transaction-policies.component.ts`
- `web/ach-interbank-ui/src/app/features/clearing-houses/transaction-policies.component.html`
- `web/ach-interbank-ui/src/app/features/clearing-houses/transaction-policies.component.scss`
- `web/ach-interbank-ui/src/app/features/clearing-houses/transaction-policies.component.spec.ts`

No routing, service, model, backend, cycle, catalog, or residual-rule implementation was changed.

## Current implementation inspected before modification

### Route and permissions

Canonical route remains:

`/clearing-houses/:id/transaction-policies`

Route guard remains `permissionGuard` with the existing permission set:

- `Config.Read`
- `Config.Manage`
- `CanReadAch`
- `CanManageAch`

Component-local permission checks remain unchanged:

- manage: `Config.Manage` or `CanManageAch`
- read: `Config.Read`, `Config.Manage`, `CanReadAch`, or `CanManageAch`
- contextual cycle visibility: `ClearingHouses.View` or `ClearingHouses.ManageCycles`
- contextual special-date visibility: `ClearingHouses.View` or `ClearingHouses.ManageSpecialDates`

### Real APIs used by the component

The productive component uses `ClearingHousesService.get(id)` plus these existing `TransactionPoliciesService` operations:

- `list(clearingHouseId)` → `GET api/clearing-houses/{id}/transaction-policies`
- `create(clearingHouseId, request)` → `POST api/clearing-houses/{id}/transaction-policies`
- `updateMetadata(clearingHouseId, id, request)` → `PATCH api/clearing-houses/{id}/transaction-policies/{policyId}/metadata`
- `close(clearingHouseId, id, effectiveTo)` → `POST api/clearing-houses/{id}/transaction-policies/{policyId}/close`
- `activate(clearingHouseId, id)` → `POST api/clearing-houses/{id}/transaction-policies/{policyId}/activate`
- `preview(clearingHouseId, transactionType, effectiveEntryDate)` → `POST api/clearing-houses/{id}/transaction-policies/preview`

`TransactionPoliciesService` and `transaction-policies.models.ts` are byte-for-byte unchanged from v47.

### Real policy fields and validation

The editor continues to use the current fields only:

- `transactionType`
- `prenotificationMode`
- `prenotificationLeadBusinessDays`
- `effectiveFrom`
- `effectiveTo`
- `normativeSource`
- `normativeReference`
- `notes`

Preserved validation and dependencies:

- transaction type required;
- prenotification mode required;
- lead days integer, minimum 0, maximum 365;
- lead days cleared and disabled for `Optional` / `NotApplicable`;
- `effectiveFrom` required;
- `effectiveTo >= effectiveFrom` when supplied;
- normative source and reference required and max 250 chars;
- notes max 2000 chars;
- current trim behavior for normative source, reference, and notes.

There is no local search/filter form on this page in the current baseline. Current/debit/credit and future policy summaries are derived from the loaded history without changing backend requests.

## Versioning, metadata edit, activation, and conflicts preserved

The current implementation is effective-date versioning; it does not expose a numeric `version` or `expectedVersion` field.

Create/version behavior remains unchanged:

- `createVersion()` opens the existing editor;
- `create()` receives the existing `CreateTransactionPolicyVersionRequest`;
- `isActive: true` remains part of the create payload;
- `effectiveFrom` / `effectiveTo` semantics remain unchanged;
- backend overlap/duplicate authority remains in the backend.

Metadata editing remains intentionally narrower than functional editing:

- `editMetadata(policy)` disables transaction type, rule mode, lead days, and effective dates;
- only normative source/reference/notes are sent to `updateMetadata()`.

Validity and activation operations remain unchanged:

- `close()` asks for an effective end date and calls `close(...)`;
- `activate()` retains the explicit confirmation and calls `activate(...)` for the selected version.

Existing conflict handling remains unchanged:

- HTTP 409 during save marks the form with `overlap: true`;
- the backend error detail/message/title is still shown through the existing snackbar fallback chain;
- no automatic retry, merge, or frontend duplicate heuristic was added.

## Foundation 4C.1 migration

Only local presentation debt was changed:

- local loading card/spinner → `UiEstadoCargaComponent`;
- local error card → `UiEstadoErrorComponent`, preserving the existing Material back action;
- custom policy hero → `PageHeaderComponent` (`app-page-header`);
- local history empty state → `UiEstadoVacioComponent`;
- existing Material buttons, forms, dialogs, table, menu, datepickers, chips, expansion panel and save spinner remain;
- hardcoded local hex/rgb colors were replaced with shared foundation tokens.

`MatChip` remains for `Vigente`, `Futura`, `Histórica`, and `Inactiva`. Those four values carry distinct effective-date semantics, so they were not collapsed into `UiEtiquetaEstadoComponent` merely for visual uniformity.

There are zero `<ui-boton>` uses and zero hardcoded hex/rgb colors in the target surface after this lot.

## Residual rules explicitly untouched

The following residual frontend and service are byte-for-byte unchanged from v47:

- `features/transactions/components/clearing-house-transaction-rules/*`
- `features/transactions/services/clearing-house-transaction-rules-api.service.ts`
- its service spec

No B6b decision was implemented here.

## Boundary debt deliberately preserved for B9

No architecture move was performed. The existing cross-feature dependency remains exactly as it was in v47:

- `transaction-policies.component.ts` / models / service continue using `TransactionTypeEnum` from `features/transactions/transactions.types`.

No new `features/* -> features/*` import was introduced. Context navigation and clearing-house services remain in their current `features/clearing-houses` ownership.

## Tests updated

Only `transaction-policies.component.spec.ts` was expanded.

The focal spec now covers the real current behavior:

- load by `:id` and route-id changes;
- invalid clearing-house id;
- HTTP load error;
- shared loading/error/empty states and `app-page-header`;
- current operational summary behavior;
- normative detail and traceability editor;
- lead-day validation and Optional-mode dependency;
- exact create-version request including trims and effective dates;
- metadata-only edit request;
- existing HTTP 409 overlap handling with no retry;
- future-version activation through the existing API;
- close-validity request with selected effective date;
- existing permission-derived management surface.

Tests were not changed to hide failures.

## Executable validation actually attempted

Environment observed:

- Node: `v22.16.0`
- npm: `10.9.2`
- `node_modules/.bin/ng`: absent
- global TypeScript: `5.8.3`

Because Angular dependencies are absent, exactly one short registry check was made:

- command: `timeout 12s npm ping`
- exit code: `124`
- output reached: `npm notice PING https://registry.npmjs.org/`
- cause: npm registry did not respond before timeout

No repeated `npm ci` was attempted.

Actual frontend commands attempted:

1. Focal test:
   - command: `npm test -- --watch=false --browsers=ChromeHeadless --include=src/app/features/clearing-houses/transaction-policies.component.spec.ts`
   - exit code: `127`
   - cause: `ng: not found`
2. SPA build:
   - command: `npm run build`
   - exit code: `127`
   - cause: `ng: not found`

Complementary syntax-only validation with global TypeScript 5.8.3 and `transpileModule(..., reportDiagnostics=true)`:

- `transaction-policies.component.ts`: 0 syntax diagnostics
- `transaction-policies.component.spec.ts`: 0 syntax diagnostics

This syntax-only check does not replace Angular tests/build.

No backend command was executed because backend is out of scope and statically unchanged.

## Static validation against v47

PASS.

Confirmed:

- before this checkpoint, the only functional files changed versus v47 were the four `transaction-policies.component.*` files;
- `TransactionPoliciesService` is byte-for-byte unchanged;
- `transaction-policies.models.ts` is byte-for-byte unchanged;
- `app-routing.module.ts` is byte-for-byte unchanged, including the canonical route and permission set;
- B5 cycle-config target remains byte-for-byte unchanged;
- residual `ClearingHouseTransactionRulesComponent` and its API service/spec remain byte-for-byte unchanged;
- all 1474 backend files under repository `src/` are byte-for-byte unchanged;
- component TypeScript functional logic is unchanged; its diff is limited to shared foundation imports and standalone-component registrations;
- no new cross-feature import was introduced;
- zero hardcoded hex/rgb colors remain in TS/HTML/SCSS of the target surface;
- zero `<ui-boton>` remains in the target surface;
- Material table/dialog behavior and policy status chips remain.

## Final status

`TRANSACTION_POLICIES_4C3B6A_IMPLEMENTED_VALIDATION_PENDING`

Angular executable validation remains pending because the dependency tree is unavailable and the npm registry check timed out. No claim of VERIFIED is made.
