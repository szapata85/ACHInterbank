# TPA9A1 — SINGLE transaction hardening

## Scope

TPA9A1 hardens only the existing **SINGLE** transaction-registration flow. The source baseline is `ACHInterbank-R18-TPA8C-COHERENCE-CLOSURE.zip`.

Out of scope and unchanged: BULK, TPA9A2, TPA10, SPA, backfill, schema/migrations, ACH Colombia/CENIT business rules, chamber policy, transaction classification and external refactors.

## Result

The SINGLE flow keeps the same business sequence and routing, but its transaction boundary is hardened for atomicity, idempotent race recovery, cancellation and failure reuse of the scoped `AchDbContext`.

### Atomicity and unit of work

`AchTransactionService.RegisterTransactionCoreAsync` continues to open one explicit database transaction before legacy customer/account onboarding. The same transaction contains:

1. legacy `Customer` / `CustomerAccount` onboarding and ID materialization;
2. canonical `ThirdParty` / `ThirdPartyAccount` onboarding, including its internal `SaveChangesAsync` calls;
3. batch resolution and prerequisite checks;
4. ACH transaction persistence;
5. canonical validation monetary-use or prenotification-attempt persistence when applicable;
6. legacy prenotification compatibility handling;
7. batch total/service-class updates;
8. pending contrapartida dispatch persistence when applicable;
9. `IUnitOfWork.CommitAsync`;
10. database transaction commit.

A failure before the database transaction commit rolls the entire SINGLE attempt back, including canonical onboarding rows that may already have been materialized by internal `SaveChangesAsync` calls.

### Idempotency and concurrency

Canonical onboarding already had unique-conflict + re-read handling and remains unchanged.

TPA9A1 adds one bounded whole-attempt retry for a relational unique-key conflict in the SINGLE orchestration. The failed transaction is disposed/rolled back, the change tracker is cleared, and the flow re-runs from database state. This closes the SINGLE side of the legacy check-then-add race for `Customer`, `CustomerAccount` and compatibility `CustomerThirdParty` creation without changing their natural keys or business rules.

The retry is deliberately bounded to one retry. A persistent/invalid duplicate is not hidden: a second unique conflict propagates normally.

Existing duplicate-policy rejection and sequential prenotification onboarding idempotency remain authoritative and unchanged.

### Cancellation

`AchTransactionService` now observes cancellation before validation/orchestration entry, at each attempt boundary, and immediately before unit-of-work and database commit. All existing asynchronous calls continue receiving the caller token.

`ThirdPartyOnboardingService` now performs an explicit cancellation pre-check before normalization/query work in both public ensure operations. Its existing unique-conflict re-read logic remains intact.

A cancellation raised after transaction/onboarding persistence but before commit rolls back the database transaction and clears the failed attempt from the scoped change tracker.

### Error handling and context reuse

On any failed SINGLE transaction attempt, `AchTransactionService` clears the `AchDbContext` change tracker after transaction rollback. This prevents rolled-back `Added`/`Modified` entities from leaking into a caller retry or an EF execution-strategy retry using the same scoped context.

The original exception is preserved for non-recoverable failures. Only relational unique-key conflicts receive the single bounded retry.

## Direct test evidence

Direct tests for the two touched services cover:

- `RegisterTransactionAsync_WhenDispatchPersistenceFails_RollsBackSilentOnboarding`: now uses the real canonical onboarding service and verifies legacy and canonical master rows plus the ACH transaction are all rolled back.
- `RegisterTransactionAsync_RecoversLegacyOnboardingUniqueConflict_WithCleanRetry`: injects a relational unique-conflict during legacy SINGLE onboarding and verifies exactly one successful customer/account/canonical/transaction result after the clean retry.
- `RegisterTransactionAsync_WhenCancellationArrivesBeforeCommit_RollsBackAndClearsTracker`: cancels after persistence work and verifies rollback plus an empty change tracker.
- `RegisterTransactionAsync_WhenUnitOfWorkCommitFails_RollsBackAndClearsTracker`: verifies a unit-of-work commit failure does not escape the database transaction and leaves no ghost tracked state.
- Existing `RegisterTransactionAsync_WhenPolicyRejectsDuplicate_DoesNotCreateSecondInitialStateEvent` and `RegisterTransactionAsync_Prenotification_OnboardsParticipantsIdempotently` continue to evidence duplicate/idempotent behavior.
- Existing `EnsureAccountAsync_RecoversUniqueConflict_WithIdempotentReread` and `EnsureAccountAsync_RecoversConcurrentAccountUniqueConflict_WithIdempotentReread` continue to evidence canonical unique-conflict recovery.
- `EnsureAccountAsync_PreCanceledToken_DoesNotCreateCanonicalRows` adds direct cancellation evidence for canonical onboarding.

## Validation

`.NET` runtime/SDK is not installed in the supplied execution environment. Focal tests and build are therefore recorded as `NOT_RUN_ENVIRONMENT`; this does not block TPA9A1 completion per task instruction.

No runtime or provider claim is made beyond the static/code-level evidence above.

## Remaining gap

`GAP-TPA-009` is narrowed: the SINGLE orchestration now has bounded unique-conflict recovery for legacy onboarding races. Equivalent BULK hardening remains outstanding and belongs to TPA9A2, which was **not executed** here.

# TPA9A2.1 — BULK atomicity/cancellation hardening

## Scope

TPA9A2.1 uses `ACHInterbank-R19-TPA9A1-SINGLE-HARDENING.zip` as its only source baseline and hardens only `AchBulkTransactionService` for item/chunk atomicity, rollback, cancellation, change-tracker cleanup and isolation between BULK items/chunks.

Explicitly out of scope here: BULK concurrency/unique-conflict recovery (TPA9A2.2), SINGLE, SPA, backfill, schema/migrations, selector behavior, ACH Colombia/CENIT rules and general refactors.

## Result

### Item atomicity inside the existing chunk transaction

The existing database transaction remains the commit boundary for each BULK chunk. TPA9A2.1 adds an EF savepoint before every item. A handled item validation/business-orchestration failure (`ArgumentException` / `InvalidOperationException`, matching the pre-existing per-item failure contract) now rolls back to that item's savepoint before the item is reported as failed.

This prevents writes already materialized by legacy `Customer`/`CustomerAccount` onboarding or canonical `ThirdParty`/`ThirdPartyAccount` onboarding from leaking into a later successful item in the same chunk.

A successful item is flushed with `SaveChangesAsync` while the chunk database transaction is still open. This is deliberately a flush, not a transaction commit: the existing chunk-level `IUnitOfWork.CommitAsync` followed by database commit remains authoritative. The flush makes earlier successful items durable *inside the open transaction* so the tracker can be safely cleared/re-hydrated after a later item rollback without losing those earlier successes.

### Chunk atomicity and unit of work

Chunk semantics are preserved:

1. items are processed under one explicit database transaction per chunk;
2. successful item state is accumulated inside that transaction;
3. batch totals/service class are updated only for batches touched by successful items;
4. `IUnitOfWork.CommitAsync` runs once for the chunk;
5. the database transaction commits only after the unit-of-work commit succeeds.

Any unexpected exception, cancellation, or unit-of-work failure before database commit explicitly rolls back the current chunk and clears the tracker. Already committed prior chunks are not rolled back, preserving the pre-existing chunk boundary.

### Tracker cleanup and item/chunk isolation

After an item savepoint rollback, `AchDbContext.ChangeTracker.Clear()` removes rolled-back `Added`/`Modified` state. The BULK customer cache is then rebuilt from the database while the same chunk transaction remains open, so previous successful items remain visible but the failed item's legacy customer/account objects cannot contaminate later items.

The customer cache is also now created per chunk rather than once for the entire request. This prevents detached customer/account objects left by the end-of-chunk tracker clear from being reused by the next chunk.

Touched batches are carried as persisted IDs and re-read before aggregate updates, so an item-level tracker clear cannot leave stale batch instances as the aggregate-update authority.

### Cancellation and rollback

Cancellation is checked at request entry, chunk and item boundaries, and immediately before unit-of-work and database commit. Existing async calls continue to receive the caller token.

If cancellation occurs after an item has already materialized legacy/canonical state, the current chunk is explicitly rolled back with a non-cancelable cleanup token and the tracker is cleared before the cancellation is rethrown. Prior chunks that already committed remain intact.

## Direct test evidence

Added direct BULK tests cover:

- `RegisterBulkAsync_WhenItemFailsAfterCanonicalOnboarding_RollsBackOnlyThatItemAndDoesNotContaminateNextItem`: first item fails after real canonical onboarding; its legacy/canonical account state is absent while the next item in the same chunk succeeds.
- `RegisterBulkAsync_WhenCancellationOccursInLaterChunk_PreservesCommittedPriorChunkAndRollsBackCurrentChunk`: a cancellation in item 51 rolls back the second chunk, preserves the first 50-item committed chunk and leaves an empty tracker.
- `RegisterBulkAsync_WhenUnitOfWorkCommitFails_RollsBackChunkAndClearsTracker`: materialized legacy/canonical rows are rolled back when chunk unit-of-work commit fails, and no tracked ghost state remains.

Existing total-success, partial-success and total-failure tests remain unchanged and continue to evidence the public BULK result semantics.

## Validation

The supplied execution environment has no `.NET` runtime/SDK. Focal test execution and build are therefore recorded as `NOT_RUN_ENVIRONMENT`; this does not block TPA9A2.1 per task instruction.

A targeted static scope/structure check was performed against only the permitted TPA9A2.1 files.

## Remaining gap / next phase

TPA9A2.1 does **not** inspect or change unique-conflict/concurrency recovery. `GAP-TPA-009` therefore remains open for the BULK concurrency slice and is reserved for TPA9A2.2. No TPA9A2.2, TPA9A3 or TPA10 work is included in this change.

# TPA9A2.2 — BULK concurrency hardening

## Scope

TPA9A2.2 uses `ACHInterbank-R20-TPA9A2-1-BULK-ATOMICITY-HARDENING.zip` as its only source baseline and reviews only BULK concurrency for legacy `Customer` / `CustomerAccount` onboarding plus the already-shared canonical `ThirdParty` / `ThirdPartyAccount` onboarding path.

The TPA9A2.1 chunk transaction, per-item savepoint, rollback, cancellation and tracker-cleanup semantics are preserved. Out of scope and unchanged: SINGLE, SPA, backfill, schema/migrations, selector behavior, ACH Colombia/CENIT rules, general refactors and TPA9A3/TPA10.

## Result

### Legacy Customer / CustomerAccount race recovery

`AchBulkTransactionService` now gives the legacy customer/account materialization stage one bounded recovery attempt for a relational unique-key conflict. The retry boundary is deliberately narrow:

1. the item savepoint already exists;
2. only legacy `Customer` / `CustomerAccount` ensure + its immediate `SaveChangesAsync` have run;
3. on a classified unique conflict, the item is rolled back to that savepoint;
4. the `ChangeTracker` is cleared;
5. the customer cache is re-read from the database so the concurrently committed winner is visible;
6. the natural-key ensure is executed once more.

No canonical onboarding, ACH transaction persistence, prenotification handling, cycle queueing, monetary-use recording or dispatch persistence is blindly replayed by this recovery path. A second/persistent unique conflict is not hidden and propagates with the pre-existing chunk-failure behavior.

This preserves TPA9A2.1 atomicity while preventing a stale BULK customer cache from turning a normal concurrent winner into a duplicate or a poisoned tracker.

### Canonical ThirdParty / ThirdPartyAccount concurrency

`ThirdPartyOnboardingService` required no production refactor. Its existing behavior already performs natural-key lookup, catches classified unique conflicts on `ThirdParty` and `ThirdPartyAccount` inserts, detaches only state introduced by the failed save, and re-reads the winning row. Its direct concurrency tests remain the evidence for both canonical identities.

Because the BULK legacy recovery happens before calling canonical onboarding, the new retry does not duplicate the canonical recovery logic already shared with SINGLE.

### Item/chunk isolation

The concurrency retry uses the existing item savepoint and tracker cleanup from TPA9A2.1. Earlier successful items in the same chunk remain flushed inside the still-open chunk transaction, while the losing item's stale legacy entities are removed before re-read. The chunk-level `IUnitOfWork.CommitAsync` and database commit order are unchanged.

No retry is introduced after non-idempotent or not-proven-idempotent downstream orchestration.

## Direct test evidence

Added direct BULK tests cover:

- `RegisterBulkAsync_RecoversConcurrentLegacyCustomerUniqueConflict_ByRereadingWinner`: creates a concurrent legacy `Customer` winner after BULK has built its cache but before the chunk transaction starts, then verifies the losing item recovers, reuses the persisted customer/account and completes without duplication.
- `RegisterBulkAsync_RecoversConcurrentLegacyCustomerAccountUniqueConflict_WithoutDuplicate`: performs the same stale-cache race for an existing customer's missing account and verifies the winner account is reused without duplication.

Existing direct canonical tests remain unchanged and continue to cover:

- `EnsureAccountAsync_RecoversUniqueConflict_WithIdempotentReread` for `ThirdParty`;
- `EnsureAccountAsync_RecoversConcurrentAccountUniqueConflict_WithIdempotentReread` for `ThirdPartyAccount`.

TPA9A2.1 rollback/cancellation/tracker-isolation tests remain unchanged and continue to guard the savepoint/chunk behavior surrounding the new narrow retry.

## Validation

The supplied execution environment has no `.NET` runtime/SDK. Focal test execution and build are therefore `NOT_RUN_ENVIRONMENT`, as allowed by the task. A targeted static review was limited to the permitted TPA9A2.2 files.

## GAP-TPA-009 status

The BULK race surfaces explicitly reviewed in TPA9A2.2 are now hardened for `Customer`, `CustomerAccount`, `ThirdParty` and `ThirdPartyAccount` without blind downstream retries.

`GAP-TPA-009` is **not fully closed** because its pre-existing definition also includes compatibility `CustomerThirdParty` check-then-add behavior. `PrenotificationHandler` was explicitly outside the permitted files for TPA9A2.2, so that compatibility race was neither inspected nor changed here. The gap is narrowed to that remaining compatibility surface rather than being closed without evidence.

# TPA9A2.3 — CustomerThirdParty compatibility concurrency hardening

## Scope

TPA9A2.3 uses `ACHInterbank-R21-TPA9A2-2-BULK-CONCURRENCY-HARDENING.zip` as its only source baseline and closes only the remaining legacy compatibility race in `PrenotificationHandler` for `CustomerThirdParty` check-then-add behavior.

Out of scope and unchanged: SINGLE/BULK orchestration, canonical `ThirdParty` / `ThirdPartyAccount` onboarding, SPA, backfill, schema/migrations, selector behavior, ACH Colombia/CENIT rules, TPA9A3 and TPA10.

## Result

### CustomerThirdParty unique-conflict recovery

The existing natural-key lookup and legacy status semantics are preserved. When a new compatibility `CustomerThirdParty` must be queued, `PrenotificationHandler` now uses the already-open SINGLE/BULK database transaction as the conflict-detection boundary:

1. the handler performs the existing normalized natural-key lookup;
2. if no row exists, it queues the same `CustomerThirdParty` as before;
3. only when an explicit database transaction is already active, it creates a local savepoint and flushes the current EF unit so the database unique constraint is evaluated before leaving the compatibility boundary;
4. a PostgreSQL `23505`, SQL Server `2601`/`2627`, or SQLite constraint error is treated as a unique-conflict candidate;
5. the handler rolls back only to its local savepoint and re-reads the natural-key winner;
6. when the winner exists, the losing attempted `CustomerThirdParty` is detached and the winner is reused;
7. when no winner exists, the original persistence exception is rethrown.

There is no blind orchestration retry. The handler does not replay canonical onboarding, transaction business validation, queueing, dispatch, or other non-idempotent work. The conflict flush occurs inside the caller's existing database transaction and does not commit it.

### Idempotency and legacy final states

A concurrent winner in `Pending` state is linked to the current prenotification using the same existing compatibility method. A winner already in `Active` or `Rejected` remains final: its status, prior prenotification link and validation evidence are not reset.

The sequential path is unchanged, and a failed duplicate insert is removed from the EF tracker before control returns. This prevents a later outer `SaveChangesAsync` from attempting the same duplicate compatibility row again.

### Canonical onboarding isolation

No canonical onboarding code is changed or duplicated. `ThirdParty` / `ThirdPartyAccount` remain governed by `ThirdPartyOnboardingService` and its existing natural-key unique-conflict re-read behavior. TPA9A2.3 only addresses the legacy `CustomerThirdParty` compatibility projection.

## Direct test evidence

Added direct `PrenotificationHandler` tests cover:

- `HandleAsync_RecoversConcurrentCustomerThirdPartyUniqueConflict_ByRereadingWinner`: injects a relational unique-conflict at the in-transaction flush, verifies a second natural-key read, detaches the losing insert, reuses the winner and leaves no duplicate `CustomerThirdParty` queued in the tracker.
- `HandleAsync_ConcurrentWinnerWithFinalStatus_IsRereadWithoutReset`: exercises both `Active` and `Rejected` winners and verifies that final status, prior prenotification link and validation evidence are preserved.
- `HandleAsync_DoesNotRetryOrReread_ForNonUniquePersistenceFailure`: verifies that unrelated persistence failures are not converted into a retry/re-read path.

Existing direct tests continue to cover sequential reuse, normalized identity matching, missing source-customer behavior, final-state preservation on the non-racing path and temporary-key customer navigation.

## Validation

The supplied execution environment has no `.NET` runtime/SDK. Focal `PrenotificationHandlerTests` and build are therefore `NOT_RUN_ENVIRONMENT`, which does not block this phase per task instruction.

A targeted static review was limited to the permitted TPA9A2.3 files, plus the conditional durable `MEMORY.md` update for the gap closure.

## GAP-TPA-009 status

`GAP-TPA-009` is **CLOSED_TPA9A2_3**. Evidence is now present for all previously identified concurrent onboarding surfaces:

- SINGLE legacy orchestration: bounded unique-conflict recovery from TPA9A1;
- BULK `Customer` / `CustomerAccount`: bounded savepoint/re-read recovery from TPA9A2.2;
- canonical `ThirdParty` / `ThirdPartyAccount`: existing shared onboarding unique-conflict re-read path;
- compatibility `CustomerThirdParty`: in-transaction unique-conflict detection, savepoint rollback, winner re-read and losing-entry detach from TPA9A2.3.

TPA9A3 and TPA10 were not executed.


# TPA9A3.1 — Validation / prenotification lifecycle hardening

## Scope

TPA9A3.1 uses `ACHInterbank-R22-TPA9A2-3-CUSTOMER-THIRD-PARTY-CONCURRENCY-HARDENING.zip` as its only source baseline and hardens only the canonical validation/prenotification lifecycle implemented by `ThirdPartyAccountValidationService` plus its direct lifecycle tests.

Explicitly out of scope and unchanged: TPA9A3.2, TPA10, SINGLE/BULK orchestration, eligibility, SPA, backfill, schema/migrations, manual approval/rejection, general refactors and ACH Colombia/CENIT business rules.

## Result

### Attempt idempotency and safe replay

Registration now treats both the supplied idempotency key and the persisted `AchTransactionId` as replay evidence. A replay of the same persisted prenotification transaction reuses the existing append-only attempt even when the caller supplies a different retry key, preventing a second attempt for the same ACH transaction.

A key/transaction collision is reused only when the persisted attempt belongs to the same stable validation context (`ThirdPartyAccount`, clearing house, transaction nature, service type and prenotification validation type). If the idempotency key or transaction points to incompatible evidence, the service fails safe instead of returning an unrelated attempt or creating another one.

### Re-prenotification and `CurrentAttemptId`

A genuinely new prenotification transaction still appends a new attempt with the next attempt number. The prior attempt is never deleted or reset. The contextual validation moves to the new attempt and `CurrentAttemptId` is updated to that new attempt.

The two persistence steps required to materialize the generated attempt id and then point `CurrentAttemptId` at it are now protected as one persistence scope on relational providers: the service starts a local transaction when no caller transaction exists, or creates a local savepoint inside an existing transaction when savepoints are supported. Failure or cancellation rolls that scope back and detaches/reloads the failed attempt state before control leaves the lifecycle boundary. This prevents a committed append-only attempt from being left behind with a stale parent pointer by the service's own two-save sequence.

### Unique-conflict recovery

The existing bounded retry remains bounded, but recovery is narrower. After a classified unique violation the service first re-reads an idempotent winner by transaction/key. If there is no such winner, a retry is allowed only when the exact attempted `(ValidationId, AttemptNumber)` was won concurrently. Any other unique violation is rethrown instead of being treated as a lifecycle race.

After a sequence race, the contextual validation is re-read before the retry so stale parent status or `CurrentAttemptId` from the losing context cannot overwrite the concurrent winner.

### Terminal state and historical evidence protection

`Accepted`, `TacitlyAccepted` and `Rejected` attempts are terminal. Once an attempt has a terminal status/resolution, later duplicate or conflicting automatic events return without modifying resolution, rejection causal, resolved timestamp, evidence reference, correlation reference or source cycle.

A late response for an older non-terminal attempt may still close that historical attempt, but it no longer rewinds the contextual validation if a newer re-prenotification is already current. Parent status and `CurrentAttemptId` are updated only when the resolved attempt is current; a missing pointer is repaired only when the resolved attempt is provably the latest append-only attempt.

### Rejection / causal and tacit acceptance

The first automatic rejection continues to persist the supplied return code/detail and the first terminal evidence. Tacit acceptance remains an automatic `AppliedTacitly -> TacitlyAccepted` transition; no manual approval/rejection path is introduced.

ACH Colombia/CENIT policy values and causales are unchanged.

### TechnicalReview fail-safe

`TechnicalReview` remains the fail-safe when no effective rule exists. It is also now used whenever a rule requires a prenotification response deadline but the deadline cannot be resolved from the supplied operational context, regardless of whether tacit acceptance is enabled. The service therefore does not create an apparently normal `AwaitingResponse` attempt with missing required timing context.

### Cancellation

Cancellation is checked before lifecycle work and immediately before persistence stages. A pre-canceled registration creates neither a validation nor an attempt; a pre-canceled resolution does not mutate the attempt/current validation state. Relational registration rollback uses a non-cancelable cleanup token after a failure/cancellation.

## Direct test evidence

Updated/added `ThirdPartyAccountValidationLifecycleTests` cover:

- replay by the same `AchTransactionId` with a different retry key reuses the original attempt and keeps the attempt count unchanged;
- an idempotency-key collision with a different ACH transaction fails safe without a duplicate;
- re-prenotification appends attempt 2 and moves `CurrentAttemptId` to it;
- a terminal rejected attempt cannot have its causal/evidence replaced by either a replay or a conflicting accepted event;
- a late resolution of attempt 1 does not overwrite the pending/current state of attempt 2;
- missing deadline context routes to `TechnicalReview` even when tacit acceptance is disabled;
- an injected relational failure while persisting `CurrentAttemptId` rolls back the just-inserted attempt and restores the parent validation state;
- pre-canceled registration creates no validation/attempt;
- pre-canceled resolution leaves the pending attempt and current pointer unchanged;
- existing tests continue to cover rejection causal persistence, automatic tacit acceptance, missing effective policy fail-safe and legacy compatibility isolation.

## Validation

The supplied execution environment has no `.NET` runtime/SDK. Focal lifecycle tests and build are therefore `NOT_RUN_ENVIRONMENT`, as explicitly allowed by the task.

A targeted static scope/structure review was limited to the four permitted TPA9A3.1 files. No repository-wide listing or source exploration was performed.

## Gap / phase status

No schema change was required and no new durable governance gap or design decision was introduced. The TPA9A3.1 lifecycle hardening is complete at static/code-evidence level with runtime execution deferred by environment.

TPA9A3.2 and TPA10 were not executed.


# TPA9A3.2 — Contextual eligibility hardening

## Scope

TPA9A3.2 uses `ACHInterbank-R23-TPA9A3-1-VALIDATION-LIFECYCLE-HARDENING.zip` as its only source baseline and hardens only the contextual eligibility decision implemented by `ThirdPartyAccountEligibilityService` plus direct eligibility evidence in `ThirdPartyAccountValidationLifecycleTests`.

Explicitly out of scope and unchanged: TPA9A3.1 lifecycle behavior, TPA9A4, TPA10, SINGLE/BULK orchestration, SPA/selector work, backfill, schema/migrations, general refactors and ACH Colombia/CENIT business-rule values.

## Result

### Fail-closed policy and master context

Eligibility now performs an explicit cancellation pre-check before any read. The canonical `ThirdPartyAccount` and its canonical `ThirdParty` must both exist and be administratively `Active`; a missing relationship is treated the same as a blocked master instead of being dereferenced. Legacy `CustomerThirdParty.Status == Active` is not read and remains non-authoritative for eligibility.

A missing effective policy remains `Unsupported/POLICY_NOT_CONFIGURED`. When the policy resolver returns a non-null rule, eligibility now defensively verifies that the returned rule is active, belongs to the requested clearing house and transaction nature, and covers the requested effective date. An inconsistent resolver result fails closed as `TechnicalReview/POLICY_CONTEXT_MISMATCH`; no ACH Colombia or CENIT rule value is changed.

### Contextual validation matching

The validation read continues to require the exact canonical account, clearing house, transaction nature, normalized service type and prenotification validation type. It now uses `SingleOrDefaultAsync` rather than selecting an arbitrary first row, so duplicate contextual rows fail closed instead of making a nondeterministic authorization choice.

A validation whose own `EffectiveDate` is later than the requested transaction effective date cannot authorize that earlier transaction and returns `PrenotificationPending/VALIDATION_NOT_EFFECTIVE_YET`. Existing policy-version comparison remains authoritative: a different effective rule row still returns `RequiresRevalidation/POLICY_VERSION_CHANGED`. Existing retention/expiration and configured business-day lead behavior remain unchanged.

### State handling and positive evidence

Existing non-authorizing mappings are preserved and directly evidenced: `PendingSend`/`PendingResponse` -> `PrenotificationPending`, `Rejected` -> `Rejected`, `RequiresCorrection` -> `RequiresRevalidation`, `TechnicalReview`/`Suspended` -> `TechnicalReview`, and `Expired` -> `ValidationExpired`.

For the two positive parent states (`Accepted` and `TacitlyAccepted`), eligibility now requires matching positive append-only attempt evidence before returning `Allowed`. Missing attempt evidence, a mismatched resolution, or a non-null `CurrentAttemptId` that does not point to the latest attempt fails closed as `TechnicalReview/VALIDATION_EVIDENCE_INCONSISTENT`. This consumes TPA9A3.1 lifecycle evidence without mutating it.

Optional prenotification semantics are preserved: when no matching validation exists and the effective rule is optional, the result remains `AllowedWithoutPrenotification`; mandatory contexts remain `RequiresPrenotification`.

### Determinism, cancellation and read failures

Attempt selection is explicitly ordered by descending attempt number and then id. `EvaluateAsync` remains read-only (`AsNoTracking`) and repeated evaluation of unchanged persisted context produces the same decision without creating or updating validation data.

Pre-canceled calls propagate cancellation before account/policy reads. Infrastructure/policy read failures are not translated into an allowing decision; they propagate to the caller. Duplicate contextual validation rows likewise propagate an integrity failure rather than being resolved by arbitrary row order.

## Direct test evidence

`ThirdPartyAccountValidationLifecycleTests` now includes direct eligibility coverage for:

- missing policy -> non-authorizing `Unsupported`;
- inactive canonical account and inactive canonical third party -> `Blocked`, including evidence that legacy `CustomerThirdParty.Active` is not authorization authority;
- a resolver-supplied rule outside the requested effective context -> `TechnicalReview`;
- all requested non-positive validation states remaining non-authorizing;
- exact chamber + nature + normalized service type + requested-date matching, including future-dated validation rejection for an earlier transaction date;
- positive parent state without matching append-only evidence -> `TechnicalReview`;
- deterministic/read-only repeated evaluation;
- duplicate contextual validation rows failing closed instead of arbitrary selection;
- pre-canceled evaluation propagating cancellation before reads;
- canonical-master and policy read failures propagating without conversion to `Allowed`.

Existing eligibility tests continue to evidence effective policy version changes, ACH Colombia/CENIT separation, optional-credit behavior, lead-day handling and CENIT debit retention without cross-chamber leakage.

## Validation

The supplied execution environment does not provide the `.NET` runtime/SDK. Focal `ThirdPartyAccountValidationLifecycleTests` and build are therefore `NOT_RUN_ENVIRONMENT`, as explicitly allowed by the task.

A targeted static scope/structure review is limited to the four permitted TPA9A3.2 files. No repository-wide listing or source exploration is performed.

## Gap / phase status

No schema change, ACH Colombia/CENIT rule change, lifecycle mutation, new durable governance gap or new durable design decision is introduced. TPA9A3.2 is complete at static/code-evidence level with runtime execution deferred by environment.

TPA9A4 and TPA10 were not executed.

# TPA9A4 — Backfill/read-cutover/shadow hardening

## Scope

TPA9A4 uses `ACHInterbank-R24-TPA9A3-2-ELIGIBILITY-CONTEXT-HARDENING.zip` as its only source baseline and hardens only the existing TPA8A backfill plus the TPA8B compatibility read cutover/shadow behavior in `ThirdPartyAccountBackfillService` and `CustomerThirdPartyAppService`, with direct evidence in `ThirdPartyAccountBackfillServiceTests` and `ThirdPartyAccountReadShadowTests`.

Explicitly out of scope and unchanged: TPA9A3 lifecycle/eligibility behavior, TPA9B, TPA10, SINGLE/BULK orchestration, SPA/selector behavior, schema/migrations, ACH Colombia/CENIT business rules and general refactors. Legacy storage and shadow comparison remain available for rollback/observation.

## Result

### Backfill checkpoint, replay and partial failure

The keyset checkpoint remains `AfterLegacyThirdPartyId`, but `LastProcessedLegacyThirdPartyId` now means the last **contiguously checkpoint-safe** row rather than the last row merely scanned. Deterministic terminal outcomes (`Created`, `Reused`, `SkippedIncomplete`, `Ambiguous`, and equivalent history outcomes) may advance the checkpoint. An unexpected master/history failure blocks further checkpoint advancement for the batch and forces `HasMore=true`, even when the failed row was the physical last row. Later rows may still be attempted for isolation/observability, but replay resumes from the last safe checkpoint and relies on the existing natural-key/idempotency rules instead of skipping the failed row.

Unexpected per-record failures and cancellations clear the EF change tracker before continuing/rethrowing, so an `Added`/`Modified` ghost from a failed master/history attempt cannot be flushed by a later record in the same batch or by a caller retry using the same scoped context.

Pre-canceled execution still propagates cancellation before canonical writes. Cancellation after prior successful batches remains replay-safe because the caller retains the prior checkpoint; no cancellation is converted into a successful summary.

### Historical evidence is not rewritten on replay

When an ACH transaction is already linked to one canonical prenotification attempt, replay now requires the canonical parent status plus attempt status/resolution to agree with the exact legacy transaction outcome before treating the row as reused. A changed/conflicting legacy outcome is reported as `CANONICAL_LINKED_HISTORY_STATE_CONFLICT`; canonical append-only evidence is not overwritten to make replay succeed.

Concurrent idempotent-attempt recovery likewise accepts a winner only when its attempt outcome and any already-final parent state match the historical outcome being reconstructed. Conflicting concurrent history is reported as ambiguous rather than rewritten.

Malformed linked-transaction destination identity is classified as conflicting evidence (`LINKED_TRANSACTION_IDENTITY_INVALID`) instead of surfacing as an unexpected retryable failure or being normalized by guesswork. No document type, account type, chamber, nature, policy version, deadline or expiry is invented.

### Canonical compatibility read fail-safe

`GET /api/customer-third-parties` remains canonical-first. Source-account resolution still requires exactly one `CustomerId`; missing or ambiguous source ownership produces no productive rows and never picks a first match.

If the canonical account/read-projection path fails after legacy compatibility rows were read, cancellation is rethrown but other canonical read/projection failures fail closed to the existing conservative legacy projection: rows may remain visible for rollback/compatibility, but every such row is `Pending` and cannot satisfy an `Active` or `Rejected` filter. Legacy `CustomerThirdParty.Status` is never promoted to functional authority.

Legacy natural-key normalization is also defensive: malformed/incomplete legacy account/document data cannot crash the cutover merge and is left as a conservative legacy-only row rather than being used to invent a canonical identity match.

### Final-state evidence in the compatibility projection

A canonical validation no longer projects `Active`/`Rejected` from the parent enum alone. `Accepted`, `TacitlyAccepted` and `Rejected` compatibility states require matching latest append-only attempt status/resolution; when `CurrentAttemptId` is present it must point to that latest attempt. Missing or contradictory evidence degrades the compatibility status to `Pending`. This is a read-side integrity guard only; it does not change TPA9A3 lifecycle or eligibility logic.

### Shadow/rollback isolation and privacy

The legacy shadow slice now uses the same unique source-`CustomerId` resolution as the productive cutover path. It no longer uses a first matching source identity when the source account is ambiguous, preventing observational data from implying a customer mapping that productive reads reject.

Shadow comparison remains non-authoritative: failures after the canonical response is constructed do not alter the productive response. Shadow/canonical-fallback logs record only stable internal batch/context facts and exception type; exception objects/messages are not attached because provider exception text may contain account/document values. Full account numbers, recipient documents and names are not written to these logs.

## Direct test evidence

`ThirdPartyAccountBackfillServiceTests` adds coverage for:

- unexpected per-record failure not advancing the safe checkpoint, `HasMore=true`, PII-safe failure logging, and successful replay from the unchanged checkpoint;
- replay with a changed/conflicting legacy outcome preserving the existing canonical parent/attempt evidence and reporting ambiguity instead of overwriting history;
- pre-canceled backfill creating no canonical master, validation or attempt rows.

`ThirdPartyAccountReadShadowTests` adds coverage for:

- a canonical positive parent state with contradictory latest attempt evidence degrading to `Pending` and not matching the `Active` filter;
- a canonical projection failure falling back to legacy-visible `Pending` rows only, never legacy `Active` authority;
- ambiguous source-account ownership producing no productive row and no inferred legacy shadow customer slice;
- shadow exceptions whose messages contain account/document/name data not leaking that PII through `CustomerThirdPartyAppService` logging;
- pre-canceled productive reads propagating cancellation and not invoking shadow comparison.

Existing tests continue to cover dry-run/idempotent TPA8A replay, neutral-context restart, legacy-only conservative fallback, canonical-over-legacy authority, multi-context non-inference, filter parity and shadow failure isolation.

## Validation

The supplied execution environment does not provide the `.NET` runtime/SDK. Focal `ThirdPartyAccountBackfillServiceTests`, `ThirdPartyAccountReadShadowTests`, and solution build are therefore `NOT_RUN_ENVIRONMENT`, as explicitly allowed by the task.

A targeted static scope/structure review is limited to the six permitted TPA9A4 files. No repository-wide source review, schema change, SPA change, selector change, SINGLE/BULK change, TPA9B work or TPA10 work is performed.

## Gap / phase status

TPA9A4 introduces no new durable governance decision and closes no additional numbered GAP-TPA item. `GAP-TPA-013` remains open because recipient document type is still not invented during ingress/backfill. `GAP-TPA-016` remains open because no historical ACH Colombia V35 activation date is inferred or backfilled.

TPA9A4 is complete at static/code-evidence level with runtime execution deferred by environment. TPA9B and TPA10 were not executed.

# TPA9B1 — SINGLE/BULK operational observability hardening

## Scope

TPA9B1 uses `ACHInterbank-R25-TPA9A4-BACKFILL-READ-CUTOVER-HARDENING.zip` as its only source baseline and changes only operational telemetry in `AchTransactionService` and `AchBulkTransactionService`, with direct evidence in `AchTransactionNachaTests` and `AchBulkTransactionServiceTests`.

Explicitly out of scope and unchanged: TPA9A3 lifecycle/eligibility, TPA9A4 backfill/read cutover/shadow behavior, TPA9B2, TPA10, SPA, schema/migrations, ACH Colombia/CENIT rules, transaction acceptance semantics and general refactors.

## Result

### SINGLE observability

`AchTransactionService` now emits low-cardinality counters and structured logs for the terminal outcomes `accepted`, `rejected`, `canceled` and `failed`. Rejection stages distinguish request validation, transaction-policy rejection, canonical prerequisite rejection and classification rejection.

The existing bounded unique-conflict retry now emits a dedicated retry counter/log with reason `unique_conflict`. Transaction-scope rollback paths emit a rollback counter/log whose reason distinguishes `unique_conflict`, `canceled` and `failed`. No retry limit, rollback behavior, validation decision or commit ordering is changed.

### BULK observability

`AchBulkTransactionService` now emits a terminal operation outcome (`accepted`, `partial`, `rejected`, `canceled` or `failed`) plus per-item outcomes. Per-item rejection telemetry carries only a bounded stage vocabulary such as `request_validation`, `transaction_policy`, `canonical_prerequisite`, `classification` and `processing`; successful items are observed only after their chunk commits.

The existing legacy-onboarding unique-conflict recovery emits a dedicated retry event and item-savepoint rollback event. Chunk rollback telemetry distinguishes no-success, cancellation and unexpected failure. Canceled and unexpected item paths are observable without changing the existing response/exception behavior.

### Metric cardinality and logging privacy

Meters are `Cfa.ACHInterbank.ACH.Single` and `Cfa.ACHInterbank.ACH.Bulk`. Counters use only bounded tags (`outcome`, `stage`, `reason`, and BULK rollback `scope`). Transaction ids, references, batch references, account numbers, documents, names and exception text are never metric labels.

Logs likewise contain only bounded outcome/stage/reason fields, exception **type** when needed, and BULK item index for operational correlation. They do not write request payloads, account/document/name values or exception messages/objects. Telemetry emission is best-effort/no-throw so a metrics listener or logging provider failure cannot change ACH processing behavior.

## Direct test evidence

`AchTransactionNachaTests` now verifies:
- accepted SINGLE outcome metric/log plus absence of source/destination account, document and recipient name in logs;
- canonical prerequisite rejection is distinguished from success/failure and does not log request PII;
- unique-conflict retry and rollback telemetry is emitted while the existing retry succeeds;
- cancellation emits canceled + rollback telemetry while preserving rollback/tracker cleanup;
- unexpected unit-of-work failure emits failed + rollback telemetry and does not copy a deliberately sensitive exception message into logs.

`AchBulkTransactionServiceTests` now verifies:
- accepted operation and committed per-item metrics/logs;
- partial validation failure yields per-item `rejected/request_validation` telemetry and a `partial` operation outcome;
- canonical prerequisite rejection is distinguishable from request validation;
- cancellation emits canceled + chunk rollback telemetry without changing committed-prior-chunk semantics;
- unexpected chunk commit failure emits failed + rollback telemetry without logging a deliberately sensitive exception message;
- concurrent legacy customer unique-conflict recovery emits retry + savepoint rollback telemetry.

## Validation

The supplied execution environment does not provide the `.NET` runtime/SDK. Focal `AchTransactionNachaTests`, `AchBulkTransactionServiceTests`, and solution build are therefore `NOT_RUN_ENVIRONMENT`, as explicitly allowed by the task.

A targeted static scope/structure review is limited to the six permitted TPA9B1 files. No repository-wide source review, functional-rule change, schema change, SPA change, TPA9B2 work or TPA10 work is performed.

## Gap / phase status

TPA9B1 introduces no new durable governance decision and closes no additional numbered `GAP-TPA-*` item. Existing open gaps remain unchanged. TPA9B1 is complete at static/code-evidence level with runtime execution deferred by environment. TPA9B2 and TPA10 were not executed.

# TPA9B2.1 — selector/canonical-read query efficiency hardening

## Scope

TPA9B2.1 uses `ACHInterbank-R26-TPA9B1-SINGLE-BULK-OBSERVABILITY-HARDENING.zip` as its only source baseline and changes only query efficiency in `TransactionThirdPartyAccountSelectorService` and `ThirdPartyAccountReadService`, with direct evidence in `TransactionThirdPartyAccountSelectorTests` and `ThirdPartyAccountReadShadowTests`.

Explicitly out of scope and unchanged: eligibility decisions/rules, TPA9A3 lifecycle, TPA9A4 backfill/read-cutover semantics, SINGLE/BULK behavior, SPA, schema/migrations, caching, ACH Colombia/CENIT rules, TPA9B2.2, TPA10 and general refactors.

## Result

### Selector query shape

The canonical selector candidate query remains `AsNoTracking` and keeps the same customer + destination-institution filters and ordering, but no longer materializes `ThirdPartyAccount`, `ThirdParty` and `FinancialInstitution` entity graphs through `Include`. It projects only the scalar fields required to build the selector response. No validation history or prenotification attempts are loaded by the selector candidate query.

Context resolution and eligibility semantics are unchanged. The existing scalar `IThirdPartyAccountEligibilityService.EvaluateAsync` contract is still invoked exactly once per projected candidate. TPA9B2.1 does not duplicate eligibility logic or introduce a selector cap/pagination contract because either change would alter current semantics.

### Canonical read query shape

`ThirdPartyAccountReadService.GetAsync` still applies every canonical filter before ordering/pagination and retains the existing `MaxPageSize = 100` clamp. The prior page-id round trip was removed: the filtered root query is paged first and then loads only the graph needed for that page. This preserves ordering and response semantics while removing a duplicate root read.

Main canonical reads (`GetAsync` and `GetByAccountIdAsync`) no longer hydrate complete attempt history for every validation. The validation include now orders attempts by `(AttemptNumber DESC, Id DESC)` and loads only one attempt, exactly matching the pre-existing `LatestAttempt` projection semantics. Full append-only history remains available only through `GetValidationHistoryAsync`.

`GetValidationHistoryAsync` remains unchanged and continues to load full append-only history only when that explicit history endpoint is requested.

### N+1 and bounded work

Canonical entity/history materialization does not perform per-account relationship queries: related master/validation data are eager-loaded as part of the paged read, and only the latest attempt is loaded for the summary read. Contextual eligibility remains one existing service evaluation per returned item when an eligibility context is requested; this dependency was not changed because eligibility is explicitly outside TPA9B2.1. On the canonical paged read that work is bounded by the existing 100-row page limit.

The transaction selector likewise evaluates eligibility once per candidate and performs no duplicate evaluation. No artificial selector limit was introduced because that would change the existing result contract.

## Direct test evidence

`TransactionThirdPartyAccountSelectorTests` now verifies that:
- selector candidates are projected without materializing canonical `ThirdPartyAccount` / `ThirdParty` / `FinancialInstitution` entity graphs;
- eligibility is evaluated exactly once per projected candidate.

`ThirdPartyAccountReadShadowTests` now verifies that:
- a canonical summary read with many historical attempts materializes only the latest attempt for each validation while preserving the same `LatestAttempt` result;
- page size remains clamped to 100 and pagination is applied before canonical graph loading, with disjoint first/second pages and the correct total;

## Validation

The supplied execution environment does not provide the `.NET` runtime/SDK. Focal selector/read tests and solution build are therefore `NOT_RUN_ENVIRONMENT`, as explicitly allowed by the task.

A targeted static scope/structure review is limited to the six permitted TPA9B2.1 files. It confirms the selector no longer uses entity `Include`, the canonical paged read no longer performs the page-id pre-query, main canonical reads load only one attempt per validation, `AsNoTracking` is preserved, and the existing 100-row canonical page cap remains in place. No repository-wide review was performed.

## Gap / phase status

TPA9B2.1 introduces no new durable governance decision and closes no additional numbered `GAP-TPA-*` item. Existing governance gaps remain unchanged. The scalar per-item eligibility service boundary is intentionally unchanged because eligibility and API-contract redesign are outside this phase. TPA9B2.1 is complete at static/code-evidence level with runtime execution deferred by environment. TPA9B2.2 and TPA10 were not executed.

# TPA9B2.2 — canonical administration query/command efficiency hardening

## Scope

TPA9B2.2 uses `ACHInterbank-R27-TPA9B2-1-SELECTOR-READ-QUERY-EFFICIENCY-HARDENING.zip` as its only source baseline and changes only query/command efficiency in `ThirdPartyAdministrationService`, with direct evidence in `ThirdPartyAdministrationApiTests`. `ThirdPartiesController` was not required and was not opened or changed.

Explicitly out of scope and unchanged: onboarding semantics, eligibility, validation lifecycle, selector, SPA, backfill/read cutover, schema/migrations, caching, ACH Colombia/CENIT rules, TPA9B3, TPA10 and general refactors. Customer isolation, idempotency, concurrency behavior, audit semantics, masking/PII controls and immutable banking identity are preserved.

## Result

### Administrative reads and pagination

Administrative third-party reads remain `AsNoTracking`, customer-scoped, filtered in the database and projected only to the DTO fields required by the API. The existing `MaxPageSize = 100` clamp, deterministic `(DisplayName, Id)` ordering, count-before-page contract and account-count projection are unchanged. No relationship `Include` or history/validation materialization was added.

`CreateThirdPartyAsync` no longer performs a second canonical re-read after the onboarding result. Its single post-onboarding read now uses the same no-tracking `ThirdPartyProjection` used by the administrative read path, supplies the metadata conflict checks, account count and response DTO in one query, and therefore removes a duplicate root query without changing create/reuse/conflict semantics.

### Command query shape and redundant writes

`CreateAccountAsync` now validates ownership through a no-tracking scalar projection containing only the canonical third-party identity fields required by the existing onboarding request. It no longer tracks/materializes the full `ThirdParty` entity merely to read those values. Financial-institution validation, onboarding, idempotent natural-key handling and the final account read remain unchanged.

Account status commands continue to validate the complete `Customer -> ThirdParty -> ThirdPartyAccount` ownership chain in one database predicate. The prior `Include(x => x.ThirdParty)` was removed because the navigation is used only by the ownership predicate; EF can translate that predicate without materializing/tracking the parent entity. The account itself remains tracked because it is the mutation target.

`UpdateThirdPartyAsync` now skips `SaveChangesAsync` when the normalized display name is already equal to the stored administrative name. The response, logging and identity immutability contract are unchanged, and no audit event is lost because there is no persisted field change in this no-op case. Existing status commands already avoided redundant saves when the requested status matched the stored status.

### Ownership and N+1 review

No per-row administrative ownership lookup was introduced. List and single reads use database projections; account status ownership is validated in the same account query; account creation performs one customer-scoped third-party identity projection rather than loading its relationship graph. `Accounts.Count` remains a server-side aggregate in the third-party projection rather than an N+1 collection load.

No duplicate Customer/ThirdParty/Account ownership query was removed by weakening isolation. All ownership predicates continue to include `CustomerId`, `ThirdPartyId` and, where applicable, `ThirdPartyAccountId`.

## Direct test evidence

`ThirdPartyAdministrationApiTests` now additionally verifies that:
- a canonical administrative third-party read leaves both `ThirdParty` and `ThirdPartyAccount` entity types untracked while preserving the account count;
- account-status ownership validation updates the account without materializing/tracking the parent `ThirdParty` relationship;
- a no-op third-party display-name update preserves the same response and creates no additional audit persistence.

Existing tests continue to cover create/reuse conflicts, immutable identity-bearing fields, customer isolation, masking/audit privacy, canonical account idempotency and administrative deactivation semantics.

## Validation

The supplied execution environment does not provide the `.NET` runtime/SDK. Focal `ThirdPartyAdministrationApiTests` and solution build are therefore `NOT_RUN_ENVIRONMENT`, as explicitly allowed by the task.

A targeted static scope/structure review was limited to `ThirdPartyAdministrationService.cs`, `ThirdPartyAdministrationApiTests.cs` and the two required governance documents. It confirms the create path reuses one projected post-onboarding read, account creation uses a no-tracking identity projection, account ownership no longer materializes `ThirdParty`, read paths remain no-tracking/paged, and no-op third-party updates avoid a redundant save. `ThirdPartiesController.cs` was not needed. No repository-wide review was performed.

## Gap / phase status

TPA9B2.2 introduces no new durable governance decision and closes no additional numbered `GAP-TPA-*` item. Existing governance gaps remain unchanged. TPA9B2.2 is complete at static/code-evidence level with runtime execution deferred by environment. TPA9B3 and TPA10 were not executed.

# TPA9B2.3 — TPA8A backfill query/resume efficiency hardening

## Scope

TPA9B2.3 uses `ACHInterbank-R28-TPA9B2-2-ADMIN-QUERY-COMMAND-EFFICIENCY-HARDENING.zip` as its only source baseline and changes only efficiency/resume behavior inside the existing TPA8A `ThirdPartyAccountBackfillService`, with direct evidence in `ThirdPartyAccountBackfillServiceTests`.

Explicitly out of scope and unchanged: TPA8B read cutover/shadow, administration, SINGLE/BULK, eligibility, validation lifecycle semantics, selector, SPA, schema/migrations, caching, ACH Colombia/CENIT rules, TPA9B3, TPA10 and general refactors. No identity, account type, validation history, chamber/nature or policy data is inferred.

## Result

### Keyset batch and bounded materialization

The backfill keeps the existing deterministic keyset contract: `Id > AfterLegacyThirdPartyId`, ordered by legacy `Id`, with `Take(BatchSize + 1)` used only to determine `HasMore`. The single materialized list is now trimmed in place when the lookahead row exists instead of allocating a second batch array. Only the requested `BatchSize` rows are processed and counted as `Scanned`; the lookahead row remains untouched until the next checkpointed call.

Canonical account lookup no longer materializes `ThirdPartyAccount` plus its `ThirdParty` relationship merely to distinguish zero/one/multiple candidates. Account type, when known from transaction evidence, is pushed into the database predicate; the query projects only `(Id, ThirdPartyId)` and reads at most two rows because the backfill only needs the `0 / 1 / >1` distinction. The same `Take(2)` ambiguity bound is applied to recipient-customer and canonical-third-party evidence queries.

Recipient-customer evidence is projected to only the identity/name fields required by the existing non-inference rules. Multiple recipient-customer matches now terminate as `RECIPIENT_IDENTITY_AMBIGUOUS` before issuing an unnecessary canonical-third-party lookup. When one recipient customer exists, the canonical third-party query is additionally filtered by the evidence-backed document type; when no recipient customer exists, the existing canonical document-only ambiguity rules remain unchanged.

### Linked-attempt and validation-history query shape

Linked prenotification-attempt lookups remain deliberately repeated between master resolution and history reconstruction so the history stage still observes a concurrent winner that appears after master resolution. Removing that second read would change concurrency/replay semantics and was therefore rejected.

The repeated reads are bounded and narrower: both stop after two rows because only `0 / 1 / >1` is meaningful. Master resolution still loads the account + third-party identity graph required by `IdentityMatches`; the later history re-read loads only the parent validation, not the account/third-party graph.

Existing validation-history detection no longer `Include`s/materializes the complete append-only `Attempts` collection. The tracked validation row is projected together with server-side `Attempts.Any()` evidence. The same shape is used for unique-conflict recovery of a concurrently created validation. Historical attempts remain untouched and append-only; the optimization changes only whether they are hydrated into the EF change tracker.

The late concurrent-attempt probe is now `AsNoTracking` because that attempt is read only for conflict/idempotency verification. Required validation/attempt persistence boundaries are unchanged; no `SaveChangesAsync` was removed where it materializes an identity, establishes the neutral resumable validation context, persists the attempt, or assigns `CurrentAttemptId`.

### Checkpoint, replay and cancellation

`LastProcessedLegacyThirdPartyId` retains the TPA9A4 meaning: the last contiguously checkpoint-safe legacy row. `Created`, `Reused`, deterministic incomplete and ambiguous outcomes advance that checkpoint when no prior unexpected failure blocked it; unexpected master/history failure still forces `HasMore=true` and prevents checkpoint advancement past the failed row. Replay remains driven by the same natural keys, linked transaction evidence and append-only-history conflict rules.

A cancellation observation point was added immediately after successful master resolution and before history reconstruction. If cancellation arrives after master persistence, the tracker is cleared and cancellation is propagated before any history query/write. Existing per-record/start checks and cancellation-token propagation remain unchanged. Persisted master data is not rolled back or invented; the next run resumes through the existing idempotent master reuse path.

### Incomplete/ambiguous rows and write efficiency

Rows with incomplete legacy master identity continue to stop before canonical lookup/onboarding. Ambiguous recipient evidence stops before onboarding and, where possible, before unrelated canonical evidence queries. No extra work is introduced to manufacture missing identity or history.

The review found no safe redundant write boundary to remove without weakening resumability or `CurrentAttemptId` integrity. Consequently write semantics remain unchanged; efficiency gains are from earlier filtering, scalar/bounded projections, no-tracking read probes and avoiding historical collection hydration.

## Direct test evidence

`ThirdPartyAccountBackfillServiceTests` now additionally verifies that:

- a `BatchSize=2` call with three source rows processes only two rows, reports `HasMore`, checkpoints at row 2, and resumes row 3 on the next call;
- incomplete master identity returns `SKIPPED_INCOMPLETE` without invoking canonical onboarding;
- an existing validation with a larger unrelated attempt history is classified with the same `EXISTING_VALIDATION_HISTORY_WITHOUT_TRANSACTION_LINK` result without materializing those attempts into the change tracker;
- cancellation immediately after master persistence propagates before validation-history work and clears tracked state, while the already-persisted master remains available for idempotent replay.

Existing tests continue to cover dry-run behavior, repeated apply idempotency, checkpoint replay, neutral-context recovery, failure-blocked checkpoint recovery, conflicting-history non-overwrite, incomplete/ambiguous evidence classification and pre-canceled execution.

## Validation

The supplied execution environment does not provide the `.NET` runtime/SDK. Focal `ThirdPartyAccountBackfillServiceTests` and solution build are therefore `NOT_RUN_ENVIRONMENT`, as explicitly allowed by the task.

A targeted static scope/structure review was limited to `ThirdPartyAccountBackfillService.cs`, `ThirdPartyAccountBackfillServiceTests.cs` and the two required governance documents. No repository-wide review was performed.

## Gap / phase status

TPA9B2.3 introduces no new durable governance decision and closes no additional numbered `GAP-TPA-*` item. Existing governance gaps remain unchanged. TPA9B2.3 is complete at static/code-evidence level with runtime execution deferred by environment. TPA9B3 and TPA10 were not executed.


# TPA9B3.1 — canonical administration security hardening

## Scope

TPA9B3.1 uses `ACHInterbank-R29-TPA9B2-3-BACKFILL-QUERY-RESUME-EFFICIENCY-HARDENING.zip` as its only source baseline. Review and changes are restricted to `ThirdPartiesController.cs`, `ThirdPartyAdministrationService.cs`, `ThirdPartyAdministrationApiTests.cs` and the two TPA governance documents.

Only administration security is in scope: `CanReadAch` / `CanManageAch` authorization wiring, `Customer -> ThirdParty -> ThirdPartyAccount` isolation and IDOR resistance, commands against foreign-customer resources, document/account masking, audit privacy, 403/404/409 disclosure, and accidental DTO exposure. Onboarding, eligibility/lifecycle, selector/SPA, backfill, schema, GAP-TPA-012 cryptography and general refactors are unchanged. TPA9B3.2 and TPA10 are not executed.

## Result

### Authorization and customer/resource isolation

`ThirdPartiesController` remains authenticated at controller level. Every GET action remains explicitly protected by `CanReadAch`; every POST/PUT mutation remains explicitly protected by `CanManageAch`; there is no `AllowAnonymous` action in the controller. Authorization policy-handler internals were not opened because they are outside the permitted file set.

`ThirdPartyAdministrationService` required no production refactor for ownership. Its existing reads and commands already fail closed on the complete ownership chain:

- third-party reads/updates/status commands bind `ThirdParty.CustomerId == customerId` and `ThirdParty.Id == thirdPartyId`;
- account creation first resolves the parent third party with the same customer/id pair and rejects an onboarding result that resolves to a different `ThirdPartyId`;
- account update/status commands use one predicate requiring account id, parent third-party id and parent customer id;
- a foreign customer, a mismatched parent third-party id, or a foreign account therefore resolves as not found/null and does not mutate canonical state.

The focal tests were expanded to exercise read isolation plus update/status/create-account operations across the wrong customer and wrong parent id, and to verify that the canonical account and third-party state remain unchanged after those attempts.

### Masking and DTO surface

The existing administrative mapping still emits `DocumentNumberMasked` only (`****` plus the last four characters for a normal document) and never places `DocumentNumberNormalized` in `ThirdPartyAdministrationDto`. Existing account reads continue to return `AccountNumberMasked`; TPA9B3.1 adds serialization evidence that the full document and full account number are absent from the administrative command responses.

Reflection guards now reject accidental raw/internal fields on the two administration DTOs reviewed by this phase, including raw/normalized document or account numbers, row/concurrency tokens, idempotency keys, validation evidence/correlation references and source-file references. No DTO shape was expanded.

### Audit privacy

Mutation audit action names remain stable. The controller no longer copies caller-controlled `X-Correlation-ID` into the EF audit scope. Audit correlation now uses the server-owned ASP.NET Core `TraceIdentifier`, preventing a caller from persisting an arbitrary header containing document/account/name data in audit records.

`ThirdPartyAdministrationService` structured logs were reviewed and remain PII-free: they contain internal customer/third-party/account ids, bounded status/created/conflict-recovery facts, and no document number, account number, display name or request payload. Existing EF audit suppression tests continue to prove that canonical document/name/account values are absent from audit before/after payloads.

### Error disclosure

Administration actions no longer reflect `ArgumentException.Message` or `ThirdPartyAdministrationConflictException.Message` into API responses. Invalid requests return the stable `INVALID_REQUEST` code with a generic message. Conflict/not-found responses use generic messages. Every not-found exception code is collapsed to `NOT_FOUND` to avoid resource-type/existence oracles; the small bounded set of non-not-found administration conflict codes is preserved for compatibility, while unknown conflict codes are collapsed to `CONFLICT` so an exception code cannot become a secondary PII exfiltration channel.

Cross-customer and mismatched-resource paths still return the same plain not-found behavior from the action/service boundary rather than disclosing the owner customer, parent third party or account. 403 remains produced by the existing ASP.NET Core authorization policies before action execution; this controller adds no sensitive 403 payload.

## Direct test evidence

`ThirdPartyAdministrationApiTests` now additionally verifies that:

- controller-level authentication is present, no action is anonymous, GET endpoints use `CanReadAch`, and all mutations use `CanManageAch`;
- foreign-customer reads and third-party/account commands fail closed, wrong-parent account commands fail closed, and those attempts do not mutate the owned canonical records;
- foreign-owned and nonexistent third-party update targets are indistinguishable at the controller boundary (`404 NotFound` with no owner/resource detail);
- full document/account numbers are absent from serialized administration responses while masked values remain present;
- a deliberately PII-bearing exception message is not reflected by 400, 404 or 409 responses;
- deliberately PII-bearing unknown conflict codes are collapsed to bounded generic codes;
- a deliberately PII-bearing `X-Correlation-ID` is not copied to audit scope and the server trace id is used instead;
- reviewed administration DTOs do not expose raw/normalized banking identity or technical validation/audit fields.

Existing tests continue to cover create/reuse conflicts, banking-identity immutability, validation-history technical-field suppression, revalidation customer isolation, EF audit PII suppression and administrative deactivation semantics.

## Validation

The supplied execution environment does not provide the `.NET` runtime/SDK. Focal `ThirdPartyAdministrationApiTests` and solution build are therefore `NOT_RUN_ENVIRONMENT`, as explicitly allowed by the task.

A targeted static scope/structure guard is limited to the five permitted files. It confirms that no file outside the requested scope is modified, the administration service ownership predicates remain customer/parent scoped, exception messages are no longer returned, caller correlation headers are no longer persisted to audit scope, and no onboarding/eligibility/lifecycle/selector/SPA/backfill/schema/cryptography code was changed. No repository-wide review was performed.

## Gap / phase status

TPA9B3.1 closes two phase-local security findings: reflected exception/code disclosure through administration errors, and caller-controlled correlation data entering the audit scope. It does not close a numbered `GAP-TPA-*` item.

`GAP-TPA-012` explicitly remains open: account numbers are still stored according to the existing normalized plaintext posture and no new cryptography or schema change is introduced. The current numbered open set remains `GAP-TPA-001`, `GAP-TPA-008`, `GAP-TPA-012`, `GAP-TPA-013` and `GAP-TPA-016`.

TPA9B3.1 is complete at static/code-evidence level with runtime execution deferred by environment. `MEMORY.md` is unchanged because no new durable design decision or governance gap was introduced. TPA9B3.2 and TPA10 were not executed.


# TPA9B3.2 — revalidation / prenotification security hardening

## Scope

TPA9B3.2 uses `ACHInterbank-R30-TPA9B3-1-ADMIN-SECURITY-HARDENING.zip` as its only source baseline. Review and changes are restricted to `ThirdPartyAccountRevalidationService.cs`, `ThirdPartyAccountValidationService.cs`, `ThirdPartyAdministrationApiTests.cs`, `ThirdPartyAccountValidationLifecycleTests.cs`, the `/revalidations` action in `ThirdPartiesController.cs`, and the two TPA governance documents.

Only security of revalidation/prenotification request and processing is in scope: `CanManageAch`, `Customer -> ThirdParty -> ThirdPartyAccount` ownership, IDOR resistance, idempotency-context binding, backend validation of clearing house / nature / service type / effective date, protection against manual decision paths, 400/404/409 privacy, PII/evidence-free logs and audit scope, and fail-closed `TechnicalReview`. ACH Colombia/CENIT business rules, general administration, SINGLE/BULK, selector/SPA, backfill, schema, cryptography and general refactors are unchanged. TPA9B3.3 and TPA10 are not executed.

## Result

### Authorization, ownership and IDOR

The existing `/api/customers/{customerId}/third-parties/{thirdPartyId}/accounts/{thirdPartyAccountId}/revalidations` action remains authenticated and explicitly protected by `CanManageAch`; no manual approve/reject route was added.

`ThirdPartyAccountRevalidationService.RequestAsync` continues to resolve the target account with one fail-closed predicate over the full ownership chain: account id, parent `ThirdPartyId`, and `ThirdParty.CustomerId`. A foreign customer, wrong parent third party, or nonexistent/foreign account returns the same null/not-found result before any prenotification command is submitted. The source account is also rechecked against the resolved Customer's own accounts before an idempotency replay can be returned, so a reused key cannot bypass source-account ownership validation.

### Idempotency security

Administrative revalidation idempotency is now bound to the persisted validation context, not only the destination account. A replay is accepted only when the existing attempt belongs to the same:

- `ThirdPartyAccountId`;
- clearing house;
- transaction nature;
- prenotification validation type;
- effective date; and
- normalized service type.

A key reused for another account or any incompatible validation context fails with the bounded internal `IDEMPOTENCY_KEY_CONFLICT`, which the API collapses to the generic 409 contract. The lower `ThirdPartyAccountValidationService` replay guard also now includes `EffectiveDate` in addition to its existing account/chamber/nature/service-type/transaction checks, preventing a transaction/idempotency replay from crossing effective-date context.

The original behavior that a repeated callback for the same ACH transaction may reuse its already persisted attempt remains intact; no new idempotency schema or cryptographic fingerprint is introduced.

### Backend context validation

For a new administrative revalidation request, the backend still derives destination banking identity from the canonical `ThirdPartyAccount`, verifies the supplied source account belongs to the owning Customer, and verifies `ServiceType` against the active `CompanyEntryDescriptionCatalog` entry selected by `CompanyEntryDescriptionId`.

The resolved prenotification policy is now defensively checked before transaction registration. A non-null rule must be active, belong to the requested clearing house and transaction nature, and cover the requested effective date (`EffectiveFrom` / `EffectiveTo`). A resolver result outside that context is rejected as `POLICY_CONTEXT_MISMATCH`; `PrenotificationMode.NotApplicable` remains non-startable. This is a security validation of resolver output only; no ACH Colombia/CENIT rule value or eligibility rule is changed.

At the validation-lifecycle boundary, an undefined `TransactionNature` or default `EffectiveDate` is rejected before policy access. If the policy resolver returns missing or inconsistent context, or returns a context in which prenotification is not applicable, the attempt is fail-closed to `TechnicalReview` rather than normal `AwaitingResponse`; deadline resolution is not invoked for such a context. Existing missing-deadline fail-closed behavior remains unchanged.

### Manual decision protection and TechnicalReview

The administration controller still exposes no approve/reject action. `ApplyTransactionResolutionAsync` remains an automatic transaction-state bridge, but it now refuses to promote a `TechnicalReview` attempt to Accepted, TacitlyAccepted or Rejected. Therefore missing/inconsistent policy or timing context cannot later become an authorizing/denying business outcome merely because a callback carries a terminal ACH transaction state. Human administration still cannot set validation outcomes directly.

### Error and audit/log privacy

`ThirdPartiesController` required no production change in TPA9B3.2. The TPA9B3.1 generic command-error contract is reused by `/revalidations`: invalid input maps to generic 400, ownership misses map to plain/generic 404, and bounded service conflicts map to generic 409 without reflecting exception messages, document/account data, evidence or caller payloads.

The revalidation action continues to set only the stable audit action and the server-owned ASP.NET Core `TraceIdentifier`; caller `X-Correlation-ID` is not copied into audit scope. Revalidation and validation lifecycle structured logs remain limited to internal ids, clearing-house id, bounded enum/status facts, attempt number and timestamps; they do not log document number, destination/source account number, idempotency key, evidence reference, source-file reference, return detail, or request payload. Domain evidence remains persisted as lifecycle evidence where required; this phase does not add it to log/audit scope.

## Direct test evidence

`ThirdPartyAdministrationApiTests` now additionally verifies that:

- the `/revalidations` action is specifically protected by `CanManageAch` and no approve/reject route exists;
- wrong customer, wrong parent third party and wrong account all fail closed before transaction registration;
- the same idempotency key cannot be reused on another owned destination account or across clearing-house, transaction-nature, effective-date, or service-type context;
- a replay cannot bypass source-account ownership validation;
- a resolver-supplied rule for the wrong clearing-house context is rejected before the ACH transaction service is invoked;
- revalidation logs exclude full document/account/source-account and idempotency-key values;
- revalidation 409/404 responses remain generic and the audit action/correlation are server-owned.

`ThirdPartyAccountValidationLifecycleTests` now additionally verifies that:

- idempotency replay cannot cross effective-date context;
- invalid transaction nature/default effective date are rejected before policy resolution;
- a resolver-context mismatch routes to `TechnicalReview` and skips deadline resolution;
- a `PrenotificationMode.NotApplicable` lower-boundary attempt also fails closed to `TechnicalReview`;
- a `TechnicalReview` attempt cannot be promoted by Certified/Returned callbacks; and
- sensitive idempotency/source-file/evidence/return-detail values are absent from lifecycle structured logs.

Existing lifecycle tests continue to cover append-only attempts, transaction/idempotency collision safety, immutable terminal attempts, late older-attempt resolution, missing deadline/policy `TechnicalReview`, cancellation safety, persistence rollback and automatic tacit acceptance.

## Validation

The supplied execution environment does not provide the `.NET` runtime/SDK. Focal `ThirdPartyAdministrationApiTests`, `ThirdPartyAccountValidationLifecycleTests` and solution build are therefore `NOT_RUN_ENVIRONMENT`, as explicitly allowed by the task.

A targeted static scope/security guard is limited to the seven explicitly permitted files. It verifies the exact revalidation authorization attribute, ownership predicate, replay-context dimensions, backend policy-context checks, `TechnicalReview` callback guard, generic controller error mapping and PII/evidence-free structured logging templates. No repository-wide review was performed.

## Gap / phase status

TPA9B3.2 closes three phase-local security findings: idempotency-key reuse across incompatible validation contexts, trust of inconsistent resolver context at prenotification start, and promotion of fail-closed `TechnicalReview` attempts by terminal transaction callbacks. It closes no numbered `GAP-TPA-*` item.

The numbered open set remains `GAP-TPA-001`, `GAP-TPA-008`, `GAP-TPA-012`, `GAP-TPA-013` and `GAP-TPA-016`. No new cryptography or schema change is introduced, and `GAP-TPA-012` remains open.

TPA9B3.2 is complete at static/code-evidence level with runtime execution deferred by environment. `MEMORY.md` is unchanged because no durable governance decision or numbered gap changed. TPA9B3.3 and TPA10 were not executed.

# TPA9B3.3 — Backfill operation security hardening

## Scope

TPA9B3.3 uses `ACHInterbank-R31-TPA9B3-2-REVALIDATION-PRENOTIFICATION-SECURITY-HARDENING.zip` as its only source baseline and is restricted to security of the existing TPA8A/TPA9A4/TPA9B2.3 backfill endpoint and service.

Explicitly unchanged: read cutover/shadow semantics, SINGLE/BULK, eligibility/lifecycle, SPA/selector, schema/migrations, cryptography, ACH Colombia/CENIT rule values, and general refactors. TPA9B3.4 and TPA10 are not executed.

## Result

### Authorization and request boundary

`POST /api/third-party-accounts/backfill` remains protected by the controller-level authentication requirement plus `[Authorize(Policy = "CanManageAch")]`; no alternate or anonymous execution path is introduced.

The controller no longer reflects `ArgumentException.Message` in the 400 response. Invalid backfill inputs now return the stable `INVALID_BACKFILL_REQUEST` code with a generic message. Backfill audit correlation is also server-owned through `HttpContext.TraceIdentifier`; a caller-provided `X-Correlation-ID` is not persisted into the audit scope.

### Input validation and checkpoint bounds

The existing `BatchSize` security bound remains `1..1000`. Negative checkpoints remain invalid. A positive `AfterLegacyThirdPartyId` is now additionally rejected when it exceeds the current legacy-source high-water mark, so a manipulated out-of-range checkpoint cannot make an execution appear successfully complete without scanning any source rows.

The keyset contract itself is unchanged: `Id > AfterLegacyThirdPartyId`, deterministic `Id` ordering, `Take(BatchSize + 1)` lookahead, and last-contiguously-checkpoint-safe result semantics remain as defined by TPA9A4/TPA9B2.3. No server-side checkpoint store, signed cursor, schema, or cryptographic mechanism is introduced.

### Dry-run separation and cancellation

Dry-run still performs evidence/query evaluation but does not invoke canonical onboarding, create master rows, create validation/attempt history, or repair `CurrentAttemptId`. Direct tests now cover both a would-create dry-run and a pre-existing canonical history case whose missing pointer remains unchanged under dry-run.

Cancellation behavior remains fail-safe: request-entry and per-record cancellation checks are preserved, the post-master/pre-history cancellation point remains in place, and observed cancellation clears tracked state before rethrow. Persisted master rows created before cancellation are intentionally retained for the existing idempotent resume path; no rollback semantics are invented.

### Canonical-history replay integrity

Replay of an already linked prenotification transaction now verifies the canonical validation context before any `CurrentAttemptId` repair is allowed. The linked validation must agree with backend evidence for clearing house, transaction nature, service type, validation type, and effective date. Missing service/context evidence fails closed; a mismatch is classified as `CANONICAL_LINKED_HISTORY_CONTEXT_CONFLICT`.

This closes a phase-local integrity path in which a transaction-linked attempt with matching status/resolution but an incompatible canonical validation context could previously be accepted as reused and have its parent pointer repaired. Replay still never rewrites conflicting canonical status/resolution evidence, and the TPA9A4 `CANONICAL_LINKED_HISTORY_STATE_CONFLICT` guard remains unchanged.

### Privacy

Backfill logs remain aggregate/technical only. Per-record failures log the internal legacy row id plus exception type, not exception objects/messages. Summary logging contains only mode/count/checkpoint facts. No document number, account number, customer/third-party name, evidence reference, validation message, source payload, or exception message is added.

The issue collection returned by the service continues to expose only internal legacy ids plus stable category/code values. No new PII-bearing DTO fields are introduced.

## Direct test evidence

`ThirdPartyAccountBackfillServiceTests` now additionally verifies that:

- dry-run never invokes onboarding and creates no canonical master/history rows;
- dry-run does not repair an existing canonical validation pointer;
- `BatchSize=0` and `BatchSize=1001` are rejected;
- a checkpoint beyond the current legacy high-water mark is rejected before onboarding/canonical writes; and
- replay with a mismatched linked canonical validation context reports `CANONICAL_LINKED_HISTORY_CONTEXT_CONFLICT` and does not repair/rebind or overwrite that history.

Existing tests continue to cover deterministic checkpoint resume, bounded lookahead, repeated apply idempotency, unexpected-failure checkpoint blocking/recovery, conflicting historical outcome non-overwrite, cancellation after master persistence, pre-canceled execution, incomplete/ambiguous evidence, and PII-safe failure logging.

## Validation

The supplied execution environment does not provide the `.NET` runtime/SDK. Focal `ThirdPartyAccountBackfillServiceTests` and solution build are therefore `NOT_RUN_ENVIRONMENT`, as explicitly allowed by the task.

A targeted static scope/security guard is limited to the five explicitly permitted files. It verifies `CanManageAch`, generic backfill 400 handling, server-owned audit correlation, batch/checkpoint guards, dry-run write gates, linked-history context validation, cancellation guards, and PII/evidence-free logging templates. No repository-wide review was performed.

## Gap / phase status

TPA9B3.3 closes four phase-local findings: backfill 400 exception-message disclosure, caller-controlled audit correlation, out-of-range checkpoint acceptance, and linked-history context reuse/repair across incompatible canonical validation context. It closes no numbered `GAP-TPA-*` item.

The numbered open set remains `GAP-TPA-001`, `GAP-TPA-008`, `GAP-TPA-012`, `GAP-TPA-013` and `GAP-TPA-016`. No schema or cryptography change is introduced.

TPA9B3.3 is complete at static/code-evidence level with runtime execution deferred by environment. `MEMORY.md` is unchanged because no durable governance decision or numbered gap changed. TPA9B3.4 and TPA10 were not executed.


# TPA9B3.4 — transactional selector / canonical-read security hardening

## Scope

TPA9B3.4 uses `ACHInterbank-R32-TPA9B3-3-BACKFILL-SECURITY-HARDENING.zip` as its only source baseline. Review and changes are restricted to `TransactionThirdPartyAccountSelectorService.cs`, `ThirdPartyAccountReadService.cs`, `ThirdPartyAccountsController.cs`, `TransactionThirdPartyAccountSelectorTests.cs`, `ThirdPartyAccountReadShadowTests.cs`, and the two TPA governance documents.

Only security of the transactional selector and canonical read is in scope: endpoint authorization, Customer isolation/IDOR, raw document/account exposure, masking, backend ownership/context authority, generic 400/404 behavior, logging privacy, and protection against turning a backend-ineligible account into a selectable candidate from frontend-controlled state. ACH Colombia/CENIT rules, SINGLE/BULK, administration, backfill, validation lifecycle, SPA, schema, cryptography and general refactors are unchanged. TPA9B4 and TPA10 are not executed.

## Result

### Authorization and Customer isolation

`ThirdPartyAccountsController` remains authenticated at controller level and both GET actions remain explicitly protected by `CanReadAch`; no anonymous read path is introduced.

Public canonical reads are now fail-closed to an explicit positive `customerId`. `ThirdPartyAccountReadService.GetAsync` also requires that Customer scope, so an accidental unscoped list read cannot enumerate canonical accounts across Customers. The `GET /api/third-party-accounts/{thirdPartyAccountId}` action no longer calls the unscoped account-id read method; it resolves the id through the same Customer-scoped paged query (`CustomerId + ThirdPartyAccountId`) and returns a plain `404` when the pair does not resolve. The existing unscoped service method is retained for already-trusted internal compatibility, but it is no longer exposed by this controller.

The transaction selector continues to resolve the owning Customer exclusively from the supplied source account through the backend repository and filters candidates by `ThirdParty.CustomerId == resolvedCustomer.Id` plus destination institution before eligibility evaluation. New evidence adds a second Customer with an account at the same destination institution and proves that the foreign candidate is neither returned nor evaluated.

### IDOR and frontend context trust

The canonical read endpoint no longer accepts caller-supplied eligibility context as authority. If any of `eligibilityClearingHouseId`, `eligibilityTransactionNature`, `eligibilityEffectiveDate` or `eligibilityServiceType` is supplied, the controller returns a bounded generic 400 and does not call the read service. This prevents a caller from manufacturing a different chamber/nature/date/service context and then treating that diagnostic read result as an authoritative transactional selection decision.

Transactional selection keeps the existing backend context derivation: nature is resolved from the transaction type, service type is loaded from the active company-entry-description catalog, clearing house is resolved by the routing strategy, and effective date comes from the resolved ACH cycle. The frontend supplies identifiers/transaction intent only; it does not supply the authoritative chamber, service type or effective date used by eligibility.

`IsSelectable` remains computed only from `IThirdPartyAccountEligibilityService`: only `Allowed` or `AllowedWithoutPrenotification` produce `true`. Rejected/blocked decisions remain non-selectable regardless of client rendering/state; no frontend boolean is accepted by the selector request.

### Masking / response minimization

The selector no longer returns canonical `AccountNumberNormalized` or `DocumentNumberNormalized` values. Before constructing each selector DTO it applies the same response masking shape used by the canonical read (`****` plus the last four alphanumeric characters, or `N/A` for short/empty values). The backend eligibility code/message are also defensively redacted against the exact canonical account/document values before they enter the response. Account/document values may still be read internally to build the masked projection and perform that redaction, but the serialized selector response contains only masked forms.

`ThirdPartyAccountReadService` continues to expose `DocumentNumberMasked` and `AccountNumberMasked` only. Its existing serialization evidence remains in place and confirms that full canonical document/account values and validation evidence/correlation/file references are absent from the DTO payload.

### Error and logging privacy

Missing/invalid Customer scope and attempted client eligibility context return bounded generic 400 objects with stable codes (`CUSTOMER_SCOPE_REQUIRED` / `CLIENT_ELIGIBILITY_CONTEXT_NOT_ALLOWED`) and no echo of search text, account number, document number or supplied service type. A Customer/account mismatch in the by-id route returns plain `404` with no owner/resource detail. No new 409 path is introduced by these read-only actions.

The three reviewed production files contain no application logging statements and TPA9B3.4 adds none. No request payload, document, account, name or exception message is written to logs by this scope.

## Direct test evidence

`TransactionThirdPartyAccountSelectorTests` now additionally verifies that:

- serialized ACH Colombia and CENIT selector responses contain masked account/document values and never the raw canonical values, even when a mocked backend eligibility message deliberately contains those values;
- a second Customer with a third-party account at the same destination institution is not returned and its account is never passed to eligibility evaluation; and
- existing rejected canonical eligibility remains non-selectable while backend-resolved chamber/nature/effective-date/service-type behavior remains unchanged.

`ThirdPartyAccountReadShadowTests` now additionally verifies that:

- canonical paged reads reject missing/non-positive Customer scope;
- a Customer-scoped read cannot return a foreign Customer's canonical account;
- controller-level authentication plus `CanReadAch` remains present and no read action is anonymous;
- the controller rejects missing Customer scope and caller-supplied eligibility context before invoking the service, without reflecting deliberately PII-bearing search/service-type values; and
- by-id reads invoke the canonical query with both `CustomerId` and `ThirdPartyAccountId`, returning plain `404` for a non-resolving scoped pair.

Existing masking, bounded-page, latest-attempt, cutover isolation and shadow-log privacy tests remain unchanged.

## Validation

The supplied execution environment does not provide the `.NET` runtime/SDK. Focal `TransactionThirdPartyAccountSelectorTests`, `ThirdPartyAccountReadShadowTests` and solution build are therefore `NOT_RUN_ENVIRONMENT`, as explicitly allowed by the task.

A targeted static scope/security guard is limited to the seven explicitly permitted files. It verifies `CanReadAch`, positive Customer scope, Customer-scoped by-id resolution, rejection of client eligibility context, selector backend context derivation, raw document/account masking, backend-only `IsSelectable`, absence of application logs in the reviewed production files, and that no file outside the requested scope is modified. No repository-wide review was performed.

## Gap / phase status

TPA9B3.4 closes three phase-local security findings: raw canonical banking identity exposure in the transactional selector, unscoped public canonical-read enumeration/by-id access, and acceptance of frontend-controlled eligibility context by the public canonical read. It closes no numbered `GAP-TPA-*` item.

The numbered open set remains `GAP-TPA-001`, `GAP-TPA-008`, `GAP-TPA-012`, `GAP-TPA-013` and `GAP-TPA-016`. `GAP-TPA-012` remains open because at-rest normalized account storage is unchanged; no schema or cryptography change is introduced.

TPA9B3.4 is complete at static/code-evidence level with runtime execution deferred by environment. `MEMORY.md` is unchanged because no durable governance decision or numbered gap changed. TPA9B4 and TPA10 were not executed.

# TPA9B4 — TPA9 operational-readiness closure

## Scope

TPA9B4 uses `ACHInterbank-R33-TPA9B3-4-SELECTOR-CANONICAL-READ-SECURITY-HARDENING.zip` as its only source baseline. This increment is documentation/governance only: it reviews the existing TPA9 hardening record, records operational readiness and defines the runtime verification boundary for TPA10.

No production code, schema, ACH Colombia/CENIT rule, SINGLE/BULK behavior, eligibility/lifecycle behavior, selector/read behavior, backfill behavior or SPA behavior is changed. TPA10 is not executed.

## TPA9 hardening closure summary

TPA9A/TPA9B collectively harden the existing canonical third-party-account capability without changing its normative ACH/CENIT rules:

- **TPA9A1 / TPA9A2.1 / TPA9A2.2 / TPA9A2.3:** SINGLE/BULK transaction boundaries, rollback/cancellation, tracker cleanup and bounded recovery for legacy/canonical unique-key races, including the remaining compatibility `CustomerThirdParty` race. `GAP-TPA-009` is closed.
- **TPA9A3.1 / TPA9A3.2:** validation/prenotification lifecycle and contextual eligibility fail closed, preserve append-only terminal evidence and require sufficient backend policy/master context. `TechnicalReview` remains the fail-safe for insufficient required context.
- **TPA9A4:** TPA8A backfill/read-cutover/shadow paths retain checkpoint-safe replay, append-only-history protection, conservative legacy fallback and non-authoritative shadow isolation.
- **TPA9B1:** SINGLE/BULK observability adds bounded low-cardinality metrics/logs without PII or exception-message labels.
- **TPA9B2.1 / TPA9B2.2 / TPA9B2.3:** selector/read, administration and backfill query paths are bounded and avoid unnecessary materialization/N+1 work while preserving semantics.
- **TPA9B3.1 / TPA9B3.2 / TPA9B3.3 / TPA9B3.4:** administration, revalidation/prenotification, backfill and selector/read security are hardened for authorization, Customer isolation/IDOR resistance, backend context authority, masking/error privacy, audit/log privacy and replay integrity.

All TPA9 phase-local static guards documented in this file passed. Every focal `.NET` test/build invocation documented during TPA9 remains `NOT_RUN_ENVIRONMENT` because the supplied environment did not provide the .NET runtime/SDK. TPA9 closure therefore means **architecture/code-review hardening is complete and operational verification is explicitly handed to TPA10**; it is not a claim that provider/runtime certification has already run.

## Operational readiness

The operational procedure is recorded in `docs/runbooks/third-party-account-tpa9-operations.md`. The deployment gate requires preserving the backend-authoritative Customer/context model, fail-closed validation behavior, masking/privacy constraints, and the existing TPA8 compatibility/rollback surfaces.

There is no TPA9 schema/migration change to coordinate or reverse. Legacy `CustomerThirdParty` storage remains present for compatibility/rollback diagnostics, but productive canonical authority remains `Customer -> ThirdParty -> ThirdPartyAccount` plus contextual validation/eligibility. Legacy-only or contradictory compatibility projections remain conservative (`Pending`) and must not be promoted into transaction authority.

## Rollback and legacy compatibility

The last documented pre-TPA9 application baseline is the TPA8C closure package used as the TPA9A1 source. Because TPA9 introduced no schema/migration, a TPA9 application rollback does not require a TPA9 database downgrade. Any rollback must still preserve database backups and must not delete canonical history or rewrite append-only attempts.

Backfill replay remains checkpoint/idempotency based rather than destructive. The safe checkpoint is the last contiguously checkpoint-safe legacy id; unexpected failures prevent advancement past the failed row. Legacy storage and shadow comparison remain available for diagnosis, and shadow failure cannot alter the already-built productive canonical response.

## Observability readiness

TPA9B1 defines the directly named transaction meters:

- `Cfa.ACHInterbank.ACH.Single`
- `Cfa.ACHInterbank.ACH.Bulk`

The bounded tag vocabulary is `outcome`, `stage`, `reason` and BULK rollback `scope`. Logs/metrics must not carry transaction/reference ids as labels, account/document/name values, payloads or exception messages. TPA10 must verify actual runtime emission and no-throw behavior under the configured telemetry providers.

Backfill logging remains aggregate/technical (mode/count/checkpoint plus internal legacy row id and exception **type** for record failures). Selector/canonical-read security scope intentionally adds no application logger; operational monitoring for those reads must therefore use platform HTTP status/latency plus the existing PII-safe shadow/canonical-fallback signals rather than introducing raw banking identity into logs.

## Backfill readiness

Operational backfill rules remain:

1. endpoint requires authentication plus `CanManageAch`;
2. run **dry-run first** for the intended checkpoint/batch window;
3. `BatchSize` must remain within `1..1000`;
4. checkpoint must be non-negative and must not exceed the current legacy high-water mark;
5. apply uses deterministic keyset order `Id > AfterLegacyThirdPartyId` with last-contiguously-safe checkpoint semantics;
6. dry-run must not invoke onboarding, create canonical master/history or repair `CurrentAttemptId`;
7. on unexpected per-row failure, do not advance checkpoint past the failed row; replay from the returned safe checkpoint after diagnosis;
8. cancellation propagates and tracked state is cleared; master rows already committed before the post-master cancellation point may remain and are reused idempotently on replay;
9. linked canonical history must match backend evidence for chamber, nature, service type, validation type, effective date and historical outcome; conflicts are reported, never rewritten to force convergence.

## TechnicalReview / fail-closed operations

`TechnicalReview` is an operational hold state, not a manual approval path. Missing effective policy, missing required response-deadline context, `NotApplicable` prenotification context where processing cannot be proven, or inconsistent backend policy/context must fail closed rather than becoming `AwaitingResponse`/Accepted/Rejected through operator action. Terminal accepted/tacitly-accepted/rejected attempts remain append-only evidence and must not be rewritten.

Operations should treat an increase in `TechnicalReview` as a signal to investigate policy/context/configuration/runtime evidence. Resolution belongs to correcting the missing/inconsistent backend context and retrying through the supported automatic lifecycle; no direct manual approve/reject capability is authorized by TPA9.

## Selector / canonical-read monitoring

The selector must continue to derive Customer from the backend source account, filter candidate accounts to that Customer and derive transaction nature, clearing house, service type and effective date in backend services. `IsSelectable` remains a derived backend eligibility decision; client state is not authorization.

Public canonical reads require positive Customer scope, by-id access is Customer-scoped, raw document/account values are not returned and client-supplied eligibility context is rejected. TPA10 runtime smoke tests must verify these boundaries with foreign-Customer ids and deliberately manipulated context, while inspecting responses/logs for PII leakage.

## Known risks retained for TPA10 and later governance

The following numbered gaps remain intentionally open and are not TPA9B4 blockers because the user explicitly excluded their resolution from this phase:

- `GAP-TPA-001` — legacy `CustomerThirdParty` / `CustomerAccount` model semantics remain less expressive than canonical master/history.
- `GAP-TPA-008` — legacy `CustomerThirdParty` onboarding remains coupled to prenotification/source-Customer resolution.
- `GAP-TPA-012` — normalized account numbers still use the existing plaintext-at-rest posture; TPA9 adds masking/privacy but no new cryptography/schema.
- `GAP-TPA-013` — generic transaction ingress still does not carry explicit recipient document type.
- `GAP-TPA-016` — precise historical ACH Colombia V32/V35 transition-day reconstruction remains unavailable and is not invented.

## TPA10 runtime verification scope

TPA10 is the next allowed phase and must perform runtime/provider verification rather than redesign TPA9. At minimum it should verify:

- Release build plus the focal TPA9 SINGLE, BULK, lifecycle/eligibility, administration/revalidation, backfill and selector/read/shadow tests that were deferred as `NOT_RUN_ENVIRONMENT`;
- relational transaction/savepoint, unique-conflict recovery, cancellation and tracker-cleanup behavior on the supported PostgreSQL and SQL Server providers;
- low-cardinality SINGLE/BULK metrics and PII-safe structured logging under the configured telemetry providers;
- backfill dry-run/apply/checkpoint/resume/cancellation and linked-history conflict behavior against representative legacy/canonical data;
- `CanReadAch` / `CanManageAch`, Customer isolation/IDOR and generic 400/404/409 behavior at the HTTP boundary;
- masking/no-raw banking identity in administration, selector and canonical-read payloads and absence of sensitive exception/payload data in audit/logging;
- fail-closed `TechnicalReview`, terminal append-only attempt protection and absence of any manual approve/reject path;
- canonical selector/read authority after deployment, including foreign-Customer and client-context manipulation smoke cases;
- rollback drill to the documented pre-TPA9 application baseline with no TPA9 schema downgrade, while preserving canonical/legacy data and checkpoint state.

## Pre-deployment / certification checklist

Before certifying TPA9 runtime behavior in TPA10, confirm all of the following:

- approved deployment package and database backup/restore point are recorded;
- no TPA9 schema/migration is pending;
- authorization policies are configured and exercised for read/manage roles;
- supported database-provider runtime tests pass;
- focal TPA9 tests/build pass in the certification environment;
- telemetry sinks show bounded tags and PII-free messages;
- a backfill dry-run is captured before any apply execution, with BatchSize/checkpoint/high-water validation;
- checkpoint/resume and cancellation recovery are demonstrated without rewriting canonical history;
- `TechnicalReview` alerts/operational review path is agreed and does not include manual decision mutation;
- selector/canonical-read Customer isolation, masking and frontend-context rejection are smoke-tested;
- legacy compatibility/shadow behavior is observed as non-authoritative and conservative;
- rollback procedure is rehearsed or otherwise approved with data-preservation expectations explicit;
- the five numbered open gaps remain recorded as accepted/deferred risks rather than silently treated as closed.

## Phase status

No documentary or architectural blocker was found within the TPA9 record reviewed in this phase. No new feature or production-code change is required for closure.

`TPA9=CLOSED`

`NEXT_ALLOWED_PHASE=TPA10`

TPA10 was **not executed** by TPA9B4.

