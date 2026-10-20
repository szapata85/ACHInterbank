# TPA2 — Silent canonical onboarding v2 / legacy coexistence

**Program:** TPA-GOV-001  
**Phase:** TPA2 only  
**Date:** 2026-10-11  
**Scope:** generic silent onboarding for `ThirdParty` + `ThirdPartyAccount` from existing SINGLE/BULK creation paths. No chamber validation, eligibility, new prenotification lifecycle, SPA/read cutover, or legacy retirement.

## 1. Result

TPA2 introduces one application port and one persistence implementation:

```text
AchTransactionService ---------┐
                               ├─> IThirdPartyOnboardingService
AchBulkTransactionService -----┘          |
                                          v
Customer -> ThirdParty -> ThirdPartyAccount
```

The onboarding contract contains only canonical master-data dimensions:

- `CustomerId`;
- third-party document type/number, display name and person type;
- destination financial institution;
- `AccountType`;
- destination account number;
- optional technical discovery source (`SingleTransaction` / `BulkTransaction`).

It has no `ClearingHouse`, credit/debit nature, PPD/CCD/CTX, prenotification status, cycle, deadline, SLA, cause code, or validation result. `ThirdPartyAccountEnrollment` is not introduced.

## 2. Shared service and normalization

`IThirdPartyOnboardingService.EnsureAccountAsync` is defined in Application and implemented by `ThirdPartyOnboardingService` in Persistence. SINGLE and BULK invoke the same service; neither contains its own ThirdParty/ThirdPartyAccount matching algorithm.

The implementation reuses the TPA1 normalization and keys without widening them:

```text
ThirdParty:
  CustomerId + DocumentType(normalized) + DocumentNumberNormalized

ThirdPartyAccount:
  ThirdPartyId + FinancialInstitutionId + AccountType + AccountNumberNormalized
```

Normalization remains:

- catalog codes: trim + invariant uppercase;
- document number: trim + invariant uppercase;
- display name: trim;
- account number: trim only.

The same canonical account is therefore reusable by any later clearing-house context; chamber is not an identity dimension.

## 3. Compatibility adapter from transaction creation

`ThirdPartyOnboardingRequestMapper` is a pure compatibility mapper. It receives the `Customer` objects already resolved by the existing customer/account onboarding path and transaction data, then creates a canonical request only when the required master identity is available.

It returns no request when, among other required data, the source `Customer` cannot be resolved or recipient identification/name/account/institution data is absent. In those cases transaction behavior remains on the legacy-compatible path; no synthetic document/account identity is created by the canonical mapper.

The existing transaction contract does not carry an explicit recipient document-type field. TPA2 therefore consumes the document/person type already resolved on the legacy recipient `Customer` rather than adding a new heuristic inside canonical onboarding. This dependency is retained explicitly as `GAP-TPA-013`; a later ingress/backfill decision is required to remove reliance on legacy recipient identity resolution for non-default document types.

## 4. SINGLE integration

Within the existing EF execution strategy, SINGLE now starts its database transaction before customer/master onboarding:

```text
validate current request/policy
-> begin DB transaction
-> existing Customer/CustomerAccount silent onboarding
-> SaveChanges (materialize Customer ids; still inside transaction)
-> map canonical request when identity is sufficient
-> IThirdPartyOnboardingService.EnsureAccountAsync
-> existing batch/classification/persist flow
-> existing PrenotificationHandler when applicable
-> existing dispatch persistence
-> final UnitOfWork SaveChanges
-> commit DB transaction
```

The internal `SaveChanges` calls are not independent commits because they run inside the transaction created by the orchestrator. A later failure rolls back the unit of work.

No clearing-house input is passed to canonical onboarding. Existing routing/classification remains after master onboarding.

## 5. BULK integration

BULK uses the same mapper and `IThirdPartyOnboardingService` for every item. Each existing chunk now owns an explicit database transaction so the early ID-materialization saves and the final transaction persistence remain atomic at chunk level.

```text
for each chunk:
  begin DB transaction
  for each item:
    existing Customer/CustomerAccount silent onboarding/cache
    SaveChanges (inside chunk transaction)
    canonical mapper
    shared onboarding service
    existing batch/classification/persist/prenote/dispatch behavior
  final UnitOfWork SaveChanges
  commit when at least one item persisted
  otherwise rollback
```

No network call is added to this path.

## 6. Idempotency and concurrency

Sequential idempotency is obtained by the TPA1 natural keys and normalized queries. Concurrency does not trust check-then-add alone:

1. read by natural key;
2. when absent, attempt insert;
3. let the TPA1 unique index arbitrate concurrent equivalent inserts;
4. classify provider unique violations with `RelationalDatabaseExceptionClassifier`;
5. detach the failed candidate and audit entries produced by that failed save attempt only;
6. re-read the natural key;
7. return the winner if present, otherwise rethrow.

This is implemented independently for `ThirdParty` and `ThirdPartyAccount`. The service never calls `ChangeTracker.Clear()`, because doing so would discard unrelated transaction/legacy changes belonging to the caller's unit of work.

`OriginSource` is enrichment metadata, not identity. A previously populated source is not overwritten merely because the account is rediscovered from another path.

## 7. Legacy coexistence / dual-write boundary

TPA2 is additive Branch by Abstraction:

- existing `Customer` / `CustomerAccount` onboarding remains operational;
- `CustomerThirdParty` remains mapped and authoritative for its current legacy consumers;
- `PrenotificationHandler` still creates/reuses `CustomerThirdParty` only in the current prenotification flow;
- prenotification automatic resolution remains unchanged;
- no read path is redirected to canonical master;
- no legacy status semantics are copied into canonical administrative status;
- no backfill or retirement occurs in TPA2.

For prenotification creation this means both canonical master and the required legacy `CustomerThirdParty` can be written in the same transaction. For ordinary transactions there was no legacy `CustomerThirdParty` write to duplicate; canonical master is simply additive to the existing Customer/CustomerAccount behavior.

## 8. Safe observability

`ThirdPartyOnboardingService` logs only internal ids, institution id, creation/recovery flags and technical discovery source. It does not log document numbers, names, or account numbers.

The generic EF audit serializer was also constrained for the new canonical entities so that:

- `ThirdParty.DocumentNumberNormalized` is excluded;
- `ThirdParty.DisplayName` is excluded;
- `ThirdPartyAccount.AccountNumberNormalized` is excluded.

Other entity audit fields are unaffected by this targeted filter.

## 9. Schema / migrations

TPA2 requires **no migration**. It uses the additive TPA1 schema and provider-specific unique indexes already introduced by:

- PostgreSQL: `20261011173000_AddThirdPartyMasterData`;
- SQL Server: `20261011173100_AddThirdPartyMasterData`.

No migration or model-snapshot file is changed by TPA2.

## 10. Tests added

`ThirdPartyOnboardingServiceTests` covers:

- create + normalize + sequential idempotent reuse;
- reuse one `ThirdParty` across multiple accounts;
- ThirdParty unique-conflict + re-read;
- ThirdPartyAccount unique-conflict + re-read;
- source Customer not resolvable;
- insufficient recipient data without synthetic identity;
- chamber/prenotification dimensions absent from the onboarding contract;
- safe application logs and safe canonical audit payloads.

Existing service test suites add integrations for:

- SINGLE canonical master creation;
- BULK canonical master creation through the same service;
- prenotification legacy `CustomerThirdParty` and canonical master coexistence.

Runtime `.NET` execution is deferred when the SDK is unavailable; this does not convert deferred tests into PASS.

## 11. Durable gap carried from transaction ingress

### GAP-TPA-013 — Recipient document type is not explicit in current transaction ingress

The current SINGLE/BULK transaction models expose recipient id, name and optional person type, but not recipient document type, while canonical ThirdParty identity includes `DocumentType`.

TPA2 does not add a second guessing rule. The compatibility mapper consumes the document type of the legacy recipient `Customer` resolved by the existing path. If recipient identity itself is absent, canonical onboarding is skipped. A future explicit document-type ingress/backfill strategy is needed to eliminate dependence on legacy recipient identity defaults for first-seen/non-default document types.

This does not justify adding clearing-house context to master identity and does not block TPA3 validation/eligibility work.
