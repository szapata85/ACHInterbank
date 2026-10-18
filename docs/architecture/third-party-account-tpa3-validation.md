# TPA3 — Contextual account validation, prenotification attempts and eligibility

**Program:** TPA-GOV-001  
**Phase:** TPA3 only  
**Date:** 2026-10-11  
**Scope:** operational validation after canonical onboarding. No SPA, read cutover, master-data retirement, backfill, or TPA4 work.

## 1. Boundary preserved

TPA3 keeps the TPA2 onboarding boundary unchanged:

```text
Customer -> ThirdParty -> ThirdPartyAccount
                                  |
                                  | onboarding ends here
                                  v
                    ThirdPartyAccountValidation
                                  |
                                  v
                    PrenotificationValidationAttempt
```

`ThirdParty` and `ThirdPartyAccount` remain clearing-house agnostic. No clearing-house, credit/debit nature, PPD/CCD/CTX, cycle, prenotification result or SLA field was added to either master entity. `ThirdPartyAccountEnrollment` is not introduced.

SINGLE and BULK still use the same `IThirdPartyOnboardingService` before any validation concern. TPA3 only starts a validation attempt after a prenotification transaction has been persisted and a canonical account reference is available.

## 2. Validation model

### `ThirdPartyAccountValidation`

One contextual validation row is identified by:

```text
ThirdPartyAccountId
+ ClearingHouseId
+ TransactionNature
+ ServiceType(normalized; empty when not applicable)
+ ValidationType
```

It stores the effective rule reference and policy effective date, current contextual status, current attempt id, effective transaction date, optional expiration and optional last monetary-use evidence.

Supported contextual states are:

- `NotStarted`;
- `PendingSend`;
- `PendingResponse`;
- `Accepted`;
- `TacitlyAccepted`;
- `Rejected`;
- `RequiresCorrection`;
- `TechnicalReview`;
- `Expired`;
- `Suspended`.

These states are not master-data administrative states and do not overwrite `ThirdPartyAccount.Status`.

### `PrenotificationValidationAttempt`

Attempts are append-only and preserve:

- monotonically increasing `AttemptNumber` within a validation context;
- optional `AchTransactionId`;
- unique `IdempotencyKey`;
- sent/deadline/resolution timestamps;
- accepted/tacit/rejected/correction/technical outcome;
- structured return code/detail;
- evidence and correlation references;
- source cycle and optional source-file reference.

A new prenotification after an accepted or rejected attempt creates a new attempt under the same contextual validation instead of rewriting prior evidence.

## 3. Policy authority and effective resolution

`ClearingHouseTransactionRule` remains the single effective-dated rule authority and is extended with generic policy data required by TPA3:

- `PrenotificationResponseDeadlineMode`;
- `PrenotificationResponseWindowCycles`;
- `TacitAcceptanceEnabled`;
- `ValidationRetentionMonthsAfterLastMonetaryUse`.

`IClearingHouseValidationPolicyService` resolves an active rule by clearing house, transaction nature/type and effective date. Overlapping effective candidates fail closed instead of choosing an arbitrary rule.

Service/SEC is retained on the validation context (`PPD`, `CCD`, `CTX` when available). The currently approved prenotification requirement/deadline rules are generic inside each chamber/nature, so no unapproved service-specific rule split is invented in `ClearingHouseTransactionRule`. SINGLE and BULK pass the already-resolved `TransactionBatchContext.SourceServiceClassCode` into the validation context.

Response deadlines are separate from `AchTransaction.SlaDeadlineAtUtc`, which remains the existing return SLA:

- `CycleCount`: resolve the configured number of subsequent persisted chamber cycles and use the final cycle cutoff;
- `OperationalDayEnd`: use the last persisted cycle cutoff on the same operational date;
- `NotApplicable`: no automatic deadline.

All cycle times are converted using the clearing-house operational timezone. No fixed clock cutoff is embedded in the validation service.

## 4. Chamber policies

### ACH Colombia V35 — credit

Source authority: DDS-DIS-MAN-004 V35, especially 2.4.8, 2.10.2, 2.10.3, 2.10.3.1 and 2.10.3.2.

Configured behavior:

- prenotification is `Optional`;
- absence of prenotification does not block credit;
- when a credit prenotification is used, its configured lead is **3 business days**;
- response/tacit deadline is configured as **4 subsequent cycles**;
- absence of a return by that configured deadline permits automatic tacit acceptance.

This closes the previous behavior where Optional credit bypassed its lead entirely.

### ACH Colombia V35 — debit

Source authority: DDS-DIS-MAN-004 V35, especially 2.4.8, 2.11.4, 2.11.4.1 and 2.11.4.2.

Configured behavior:

- prenotification is `Mandatory`;
- configured lead remains **3 business days**;
- response/tacit deadline is **4 subsequent cycles**;
- receiver identification validation remains mandatory per configured rule.

### CENIT — credit

Source authority: DSP-152 Anexo 2, 4.7 and 4.8.

Configured behavior:

- credit prenotification is `Optional`;
- no ACH-Colombia `+3 business days` rule is copied into CENIT;
- when a prenotification is sent, its return/tacit deadline is the configured operational-day end (last CENIT cycle for the same value date).

### CENIT — debit

Source authority: DSP-152 Anexo 2, 4.7 and 4.8; retention authority remains the CENIT NACHA-M rule traced by `THIRD_PARTY_ACCOUNT_NORMATIVE_TRACEABILITY.md`.

Configured behavior:

- debit prenotification is `Mandatory`;
- no global three-day lead is applied;
- return/tacit deadline is the last CENIT cycle on the same value date;
- only CENIT debit policy carries the configured `13`-month retention after last monetary use.

Eligibility derives expiration from `LastMonetaryUseAtUtc + configured retention` only when that policy value exists; the retention is not extrapolated to ACH Colombia.

## 5. Eligibility is contextual

`IThirdPartyAccountEligibilityService` evaluates:

```text
canonical account
+ clearing house
+ transaction nature
+ service context
+ effective rule/version
+ validation history/current attempt
= ThirdPartyAccountEligibilityDecision
```

Decisions include:

`Allowed | AllowedWithoutPrenotification | RequiresPrenotification | PrenotificationPending | ValidationExpired | Rejected | RequiresRevalidation | TechnicalReview | Blocked | Unsupported`.

Important semantics:

- an inactive/missing canonical account is blocked administratively;
- no effective policy returns `Unsupported` / technical fail-closed semantics rather than a guessed chamber rule;
- Mandatory without validation returns `RequiresPrenotification`;
- Optional without validation returns `AllowedWithoutPrenotification`;
- accepted/tacit validations are still subject to any configured business-day lead, anchored to the prenotification effective/processing date rather than the UTC transport date;
- when the effective functional policy version changes, a validation recorded against the prior rule returns `RequiresRevalidation` instead of being silently reused;
- rejection stays contextual and does not delete the master account;
- expiration is applied only from configured authority/evidence.

TPA3 does **not** wire this new eligibility service into the global SPA selector or perform the planned TPA7 transaction-selection cutover.

## 6. Prenotification lifecycle and automatic resolution

When SINGLE/BULK creates a prenotification and canonical onboarding produced an account reference:

1. transaction persistence materializes the transaction id inside the existing database transaction/chunk transaction;
2. transaction code resolves credit/debit nature;
3. `SourceServiceClassCode` carries service context;
4. `IThirdPartyAccountValidationService.RegisterPrenotificationAttemptAsync` ensures/reuses the contextual validation;
5. a distinct prenotification creates the next append-only attempt;
6. the effective rule resolves the response deadline;
7. missing/unresolvable policy/deadline routes the attempt to `TechnicalReview`, never to a synthetic accepted/rejected result.

The attempt registration is idempotent by `IdempotencyKey`. Context creation and attempt-number races use database unique constraints plus targeted detach/re-read/retry; the service does not clear the caller's full change tracker.

`AchStateTransitionService` is the explicit bridge for automatic outcomes:

- `Certified` -> canonical `Accepted`;
- `AppliedTacitly` -> canonical `TacitlyAccepted`;
- `ReturnedByOperator` / `ReturnedByEpr` -> canonical `Rejected` with structured cause/evidence.

The same transition continues updating `CustomerThirdParty` through the existing legacy synchronization. There is no user command or endpoint that directly sets the interbank result.

## 7. Tacit acceptance / SLA correction

`AchTacitAcceptanceJobHandler` no longer treats `AchTransaction.SlaDeadlineAtUtc` as the prenotification response deadline and no longer falls back to “next UTC date”.

For TPA3 attempts it consumes `PrenotificationValidationAttempt.ResponseDeadlineAtUtc`, produced from the effective chamber rule and persisted cycle schedule. A compatibility path for pre-TPA3 pending prenotifications computes the same chamber-policy deadline without writing synthetic validation history.

This keeps automatic resolution while separating prenotification response timing from the existing return SLA.

## 8. Legacy bridge and re-prenotification

`CustomerThirdParty` remains mapped and operational; there is no read cutover.

Legacy final rows (`Active`/`Rejected`) are deliberately not reset to `Pending` by a new prenotification. TPA3 canonical history, however, creates a new attempt and can resolve that new attempt independently. This is the Branch-by-Abstraction bridge required to support re-prenotification without corrupting legacy final-state evidence.

The canonical validation/attempt model, not `CustomerThirdParty.Active/Rejected`, is the future source for contextual eligibility. TPA3 does not yet redirect legacy readers.

## 9. GAP corrections in this phase

- **GAP-TPA-002 CLOSED:** maturity/lead is policy-driven; there is no global `+3 business days` in `PrenotificationQueryService`.
- **GAP-TPA-003 CLOSED:** tacit acceptance uses the dedicated validation-attempt response deadline; return SLA and UTC-date fallback are no longer prenotification authorities.
- **GAP-TPA-006 CLOSED:** ACH Colombia transaction-rule seed metadata now names V35, and the additive migration upgrades existing V32 transaction-rule rows where present.
- **GAP-TPA-010 CLOSED_CANONICAL:** re-prenotification is represented by a new canonical attempt even when legacy `CustomerThirdParty` is final; the legacy row remains unchanged for compatibility.
- **GAP-TPA-011 CLOSED:** ACH Colombia credit remains Optional, but a configured 3-business-day lead is enforced when an optional credit prenotification is actually used.

## 10. Schema and migrations

TPA3 requires additive schema because validation lifecycle tables and rule-policy fields did not exist in TPA2.

PostgreSQL:

- `20261011184000_AddThirdPartyAccountValidationLifecycle`.

SQL Server:

- `20261011184100_AddThirdPartyAccountValidationLifecycle`.

Both migrations:

- add the four generic response/retention policy fields to `ClearingHouseTransactionRules`;
- create `ThirdPartyAccountValidations`;
- create `PrenotificationValidationAttempts`;
- add contextual and attempt idempotency indexes;
- preserve `CustomerThirdParty` and TPA1 master tables;
- update only the relevant existing transaction-rule metadata/policy values for the approved ACH Colombia V35/CENIT authorities.

No SPA schema or endpoint cutover is included.

## 11. Tests added/extended

`ThirdPartyAccountValidationLifecycleTests` covers:

- independent ACH Colombia credit/debit and CENIT credit/debit policies;
- effective policy version by date and revalidation after a functional policy-version change;
- contextual eligibility rather than a global boolean;
- ACH Colombia optional-credit lead when prenotified;
- CENIT-only retention/expiration derivation;
- ACH four-cycle vs CENIT operational-day response deadline modes;
- idempotent attempt registration;
- structured rejection cause;
- multiple attempts/re-prenotification;
- technical review when policy cannot be resolved;
- automatic tacit-acceptance state;
- canonical re-prenotification without resetting a final legacy status.

`TransactionPrerequisitePolicyServiceTests` additionally characterizes the ACH Colombia V35 credit rule: no prenotification remains valid, but when a prenotification is supplied its configured three-business-day lead is enforced.

Execution remains environment-qualified: `dotnet`/Docker absence is reported as `NOT_RUN_ENVIRONMENT`, never as PASS.

## 12. Deferred integration evidence / remaining gaps

- TPA3 models and derives CENIT retention when `LastMonetaryUseAtUtc` evidence exists, but the global monetary-transaction read/cutover path is intentionally not changed in TPA3. Recording last monetary use from the future canonical transaction-selection path remains a later integration concern (tracked as `GAP-TPA-014`).
- Pre-TPA3 historical prenotifications without sufficient canonical account identity are handled by policy-driven tacit-acceptance compatibility only; TPA3 does not invent canonical account/validation history. Backfill remains a later controlled phase (tracked as `GAP-TPA-015`).
- The baseline ACH Colombia transaction-rule seed has a historical effective date that predates V35, while the supplied V35 authority identifies April 2026 without an exact effective day. TPA3 corrects the current rule metadata/behavior to V35 but does not invent a historical V32/V35 split date; precise historical version backfill is tracked as `GAP-TPA-016`.
- Full compilation, provider migration execution and cluster/job runtime evidence are deferred to an environment with .NET SDK and PostgreSQL/SQL Server containers.
