# TPA5 — Canonical third-party administration API

**Program:** TPA-GOV-001  
**Phase:** TPA5 only  
**Date:** 2026-10-11  
**Scope:** administrative commands over canonical `Customer -> ThirdParty -> ThirdPartyAccount`, validation-history reads, and a command to request a new contextual prenotification. No SPA work, no global read cutover, no legacy retirement and no manual interbank result decision.

## 1. Phase invariants

TPA5 keeps the master independent from clearing-house policy:

```text
Customer
  -> ThirdParty
      -> ThirdPartyAccount
```

`ThirdParty` and `ThirdPartyAccount` create/update contracts contain no `ClearingHouseId`, transaction nature, service class, prenotification result, cycle or SLA. ACH Colombia/CENIT enter only through the separate revalidation command and the TPA3 policy/validation services.

The existing `CustomerThirdParty` model, legacy read authority, SINGLE/BULK silent onboarding and TPA4 shadow read remain operational. TPA5 does not alter the transaction SPA selector or globally cut productive reads to the canonical model.

## 2. Command boundary

TPA5 adds `IThirdPartyAdministrationService` for chamber-free master-data administration and reuses `IThirdPartyOnboardingService` as the only creator of canonical identity. `EnsureThirdPartyAsync` was added to the onboarding port so explicit manual third-party creation and silent SINGLE/BULK account discovery use the same normalization, natural keys and unique-conflict/re-read behavior.

Canonical natural keys remain unchanged:

```text
ThirdParty:
CustomerId + DocumentType + DocumentNumberNormalized

ThirdPartyAccount:
ThirdPartyId + FinancialInstitutionId + AccountType + AccountNumberNormalized
```

Manual create therefore does not implement a second identity algorithm.

### 2.1 ThirdParty commands

Supported operations:

- create explicitly under a `Customer`;
- update administrative display name;
- activate/deactivate administratively.

`DocumentType`, `DocumentNumber` and `PersonType` are intentionally not present in the generic update contract. They are identity-bearing inputs and are not silently rewritten after validation history may exist. A correction that changes identity requires an explicit replacement/correction strategy rather than mutating the key or reusing historical interbank evidence implicitly.

An idempotent create with the same natural identity and the same administrative metadata returns the existing master. If the natural identity already exists but the request carries conflicting administrative identity metadata, the command returns `DUPLICATE_IDENTITY_CONFLICT` and requires an explicit update/correction instead of silently overwriting the winner of a concurrent or earlier create.

### 2.2 ThirdPartyAccount commands

Supported operations:

- list accounts belonging to a third party;
- create explicitly;
- update the currently permitted administrative field (`Status`);
- activate/deactivate administratively.

Banking identity (`FinancialInstitutionId + AccountType + AccountNumber`) is not mutable through the update API. Changing that tuple means creating the replacement canonical account and deactivating the former account. This avoids moving historical chamber-specific validation attempts to a different bank identity.

Administrative `Active` never means interbank eligible. Eligibility remains a derived TPA3 decision for a concrete chamber/nature/service/effective-date context. TPA5 additionally makes parent `ThirdParty` administrative inactivity a blocking condition in `IThirdPartyAccountEligibilityService` so an active child account cannot bypass an inactive parent.

## 3. API surface

Base route:

`/api/customers/{customerId}/third-parties`

| Method | Route | Policy | Purpose |
|---|---|---|---|
| GET | `/api/customers/{customerId}/third-parties` | `CanReadAch` | Paged canonical third parties for a customer |
| GET | `/api/customers/{customerId}/third-parties/{thirdPartyId}` | `CanReadAch` | Canonical third-party detail |
| POST | `/api/customers/{customerId}/third-parties` | `CanManageAch` | Explicit/idempotent third-party create |
| PUT | `/api/customers/{customerId}/third-parties/{thirdPartyId}` | `CanManageAch` | Allowed administrative update |
| PUT | `/api/customers/{customerId}/third-parties/{thirdPartyId}/status` | `CanManageAch` | Activate/deactivate third party |
| GET | `/api/customers/{customerId}/third-parties/{thirdPartyId}/accounts` | `CanReadAch` | List accounts of the third party |
| POST | `/api/customers/{customerId}/third-parties/{thirdPartyId}/accounts` | `CanManageAch` | Explicit/idempotent account create |
| PUT | `/api/customers/{customerId}/third-parties/{thirdPartyId}/accounts/{accountId}` | `CanManageAch` | Allowed account administrative update |
| PUT | `/api/customers/{customerId}/third-parties/{thirdPartyId}/accounts/{accountId}/status` | `CanManageAch` | Activate/deactivate account |
| GET | `/api/customers/{customerId}/third-parties/{thirdPartyId}/accounts/{accountId}/validation-history` | `CanReadAch` | Contextual validations and append-only attempts |
| POST | `/api/customers/{customerId}/third-parties/{thirdPartyId}/accounts/{accountId}/revalidations` | `CanManageAch` | Request a new contextual prenotification/revalidation |

No approve/reject endpoint or command exists.

All nested service operations verify `CustomerId -> ThirdPartyId -> ThirdPartyAccountId` ownership before reading or mutating state, so a valid identifier from another customer does not become an IDOR path.

## 4. Revalidation command

`IThirdPartyAccountRevalidationService` accepts only the validation context needed to request a new prenotification:

- `ClearingHouseId`;
- `TransactionNature`;
- `EffectiveDate`;
- `ServiceType`;
- active `CompanyEntryDescriptionId`;
- Customer-owned source account;
- caller-supplied `IdempotencyKey`.

The service requires the canonical third party and account to be administratively active, confirms the source account belongs to the owning Customer, verifies that the selected company-entry description resolves to the requested standard-entry-class/service type, and **always** calls `IClearingHouseValidationPolicyService.ResolveAsync` before submission. A missing effective rule fails with `POLICY_NOT_CONFIGURED`; `PrenotificationMode=NotApplicable` fails with `PRENOTIFICATION_NOT_APPLICABLE`.

The command delegates transaction creation to the existing ACH transaction pipeline through `IAchTransactionService.RegisterCanonicalPrenotificationAsync`. The pipeline is not given a new master identity algorithm: it is told the existing `ThirdPartyAccountId`, while legacy Customer/CustomerAccount and `CustomerThirdParty` compatibility behavior remains in place.

After `IBatchResolver` resolves the real route, the transaction pipeline verifies that the resolved `ClearingHouseId` and operational effective date are the ones requested by the administrative validation command. A mismatch aborts the transaction with `CLEARING_HOUSE_ROUTE_MISMATCH` or `EFFECTIVE_DATE_ROUTE_MISMATCH`; the API never fabricates a route merely to satisfy a requested chamber.

The actual canonical validation attempt is still created by `IThirdPartyAccountValidationService`, which resolves the TPA3 policy again against the routed context. Its automatic TPA3 state bridge remains the only path that can turn interbank evidence into Accepted/TacitlyAccepted/Rejected.

## 5. Idempotency and concurrency

### Master data

Manual and silent create use the TPA2 database-arbitrated algorithm:

1. normalize and read by the TPA1 natural key;
2. attempt insert if absent;
3. let the provider unique constraint arbitrate races;
4. detach only entities introduced by the failed save attempt;
5. re-read the database winner;
6. return that stable canonical identifier.

No blind retry of the caller's unit of work is introduced.

### Revalidation

`IdempotencyKey` is required and bounded to the existing persistence length (160 characters). The revalidation service first reads an existing attempt by idempotency key. If none exists, the TPA3 validation service still performs its own unique-conflict/re-read arbitration. If a concurrent request has already bound that idempotency key to another ACH transaction, the newly-created transaction path throws before commit, rolls back the loser transaction, then returns the winning attempt. Reusing the same key for another canonical account is a conflict (`IDEMPOTENCY_KEY_CONFLICT`).

A genuinely new retry/revalidation uses a new idempotency key and produces another append-only `PrenotificationValidationAttempt`; it does not overwrite a prior attempt.

## 6. Validation-history read

`IThirdPartyAccountReadService.GetValidationHistoryAsync` returns all validation contexts for the canonical account and all attempts ordered newest-first within each context. The DTO contains operational identifiers/statuses, effective policy references, timestamps, return code and safe cycle identifier.

It deliberately omits:

- full account/document values;
- validation-attempt `IdempotencyKey`;
- `ReturnDetail`;
- `EvidenceReference`;
- `CorrelationReference`;
- `SourceFileReference`.

This read remains canonical and does not change the productive legacy read authority established before TPA5.

## 7. Authorization, audit and privacy

The controller follows existing policies instead of inventing roles:

- reads: `CanReadAch`;
- administrative writes and revalidation request: `CanManageAch`.

Controller commands set the existing EF audit action/correlation scope. Audit serialization suppresses full third-party/customer identifiers, account numbers and transaction/prenotification technical evidence for the entities touched by this flow. Structured logs emitted by the new services contain internal IDs, statuses and non-PII context only; they do not log document numbers, account numbers or display names.

Read DTOs mask document/account numbers to the last four characters where those values are present.

## 8. Legacy compatibility

TPA5 preserves branch-by-abstraction:

- `CustomerThirdParty` remains mapped and operational;
- TPA4 legacy productive reads/shadow comparison remain unchanged;
- SINGLE/BULK continue silent canonical onboarding;
- explicit manual creation converges on the same onboarding service;
- a successful administrative prenotification still flows through the existing `PrenotificationHandler`, preserving the legacy compatibility record where current behavior does so;
- no SPA selector or global read cutover is introduced.

No schema migration is required for TPA5.

## 9. Tests added

`ThirdPartyAdministrationApiTests` covers:

- create/update/deactivate ThirdParty;
- create/update/deactivate ThirdPartyAccount;
- idempotent natural-key reuse and duplicate metadata conflict;
- customer ownership isolation;
- existing read/manage authorization policies and absence of approve/reject routes;
- masked administrative reads and EF audit suppression of full document/account/name values;
- complete validation-attempt history without evidence/detail/idempotency leakage;
- policy-resolved, idempotent revalidation request;
- chamber-free master create contracts;
- manual/silent onboarding convergence;
- parent administrative deactivation blocking contextual eligibility without mutating the stored interbank validation outcome.

## 10. Gaps and phase boundary

`GAP-TPA-005` is closed by the canonical administrative command API implemented in this phase.

The following remain open and are not claimed as solved by TPA5:

- `GAP-TPA-004`: SPA still selects legacy global `Active`;
- `GAP-TPA-013`: generic transaction ingress still lacks explicit recipient document type;
- `GAP-TPA-014`: monetary paths still do not maintain `LastMonetaryUseAtUtc`;
- `GAP-TPA-015`: historical/backfill identity remains incomplete;
- `GAP-TPA-016`: no exact historical V32/V35 split date is invented.

TPA5 does not implement TPA6 UI, canonical selector cutover, global read cutover, mass backfill, legacy deletion or final hardening.
