# TPA9 operational runbook — third-party accounts

## Purpose and authority

This runbook operationalizes the behavior already hardened and documented through TPA9. It does not add a product feature, change ACH Colombia/CENIT rules, change eligibility/lifecycle semantics, add schema/cryptography, or authorize manual validation decisions.

Source governance for this runbook is `docs/architecture/third-party-account-tpa9-hardening.md` and `docs/ai/THIRD_PARTY_ACCOUNT_GOVERNANCE_STATE.md` in the TPA9B4 baseline lineage.

**TPA9 status:** `CLOSED` at static/architectural hardening level.  
**Next allowed phase:** `TPA10`.  
**Runtime certification:** pending TPA10; TPA9 focal .NET tests/build were repeatedly `NOT_RUN_ENVIRONMENT` in the supplied environment.

## Operational invariants

1. Canonical master authority is `Customer -> ThirdParty -> ThirdPartyAccount`; validation/eligibility is contextual and separate from master identity.
2. Backend services resolve Customer/clearing-house/nature/service-type/effective-date authority. Frontend state is not transaction authorization.
3. Mandatory validation failures and insufficient required context fail closed; `TechnicalReview` is not approval.
4. Validation attempt history is append-only; final Accepted/TacitlyAccepted/Rejected evidence must not be rewritten.
5. Raw document/account values must not be exposed in administrative, selector or canonical-read responses or placed in operational logs/audit messages.
6. Legacy `CustomerThirdParty` remains compatibility/rollback data and is not productive eligibility authority.

## Deployment gate

Before deployment/certification:

- Record the exact application package and a database backup/restore point.
- Confirm no TPA9 schema/migration is pending.
- Confirm `CanReadAch` and `CanManageAch` policy configuration for the intended roles.
- Execute the TPA10 runtime suite: Release build, focal TPA9 tests and PostgreSQL/SQL Server provider verification.
- Verify telemetry sinks are configured before traffic: no raw banking identity, payload or exception-message capture.
- Define the backfill checkpoint and run dry-run before any apply operation.
- Confirm an operational owner exists for `TechnicalReview` investigation without manual approve/reject mutation.
- Confirm rollback ownership and the pre-TPA9 application target are approved.

Do not certify TPA9 runtime behavior until the TPA10 verification items pass in the deployment environment.

## Authorization and privacy smoke checks

Run HTTP smoke tests with allowed and disallowed identities:

- Canonical reads require authentication and `CanReadAch`.
- Administration, revalidation and backfill require `CanManageAch`.
- Foreign-Customer third party/account ids must fail closed and must not reveal owner/resource detail.
- Canonical by-id reads must be scoped by Customer.
- Missing Customer scope or client-supplied eligibility context must fail with bounded generic 400 responses.
- Not-found/conflict paths must not reflect account/document/name values or exception messages.
- Administrative, selector and read responses must show masked account/document values only.

Inspect application/audit/platform logs produced by these smoke calls and reject certification if raw document/account/name/payload or exception-message data appears.

## SINGLE/BULK observability

Expected meters documented by TPA9B1:

- `Cfa.ACHInterbank.ACH.Single`
- `Cfa.ACHInterbank.ACH.Bulk`

Expected low-cardinality tag vocabulary: `outcome`, `stage`, `reason`, and BULK rollback `scope`.

Operationally watch:

- SINGLE terminal outcomes: accepted/rejected/canceled/failed;
- SINGLE unique-conflict retry and rollback reason;
- BULK terminal outcomes: accepted/partial/rejected/canceled/failed;
- BULK per-item rejection stage;
- BULK legacy-onboarding retry/savepoint rollback and chunk rollback reason.

Metrics/logs must not use transaction ids, references, batch references, account/document/name values or exception text as metric labels. Structured logs may use bounded outcome/stage/reason fields, exception **type** when needed and BULK item index. Telemetry failures must not alter ACH processing behavior.

## Backfill procedure

### 1. Prepare

- Use `POST /api/third-party-accounts/backfill` only under `CanManageAch`.
- Determine the intended `BatchSize` (`1..1000`).
- Determine the starting `AfterLegacyThirdPartyId`; it must be non-negative and not above the current legacy high-water mark.
- Preserve the last known safe checkpoint externally with the change/deployment record. TPA9 does not add a signed/server-persisted checkpoint store.

### 2. Dry-run

Always run the target window in dry-run first.

Expected dry-run properties:

- queries/evidence evaluation may execute;
- no canonical onboarding/master create;
- no validation/attempt-history create;
- no `CurrentAttemptId` repair;
- returned issues contain internal legacy ids and stable categories/codes, not PII.

A dry-run that reports unexpected conflicts, ambiguous evidence or missing context is a stop condition for apply until reviewed.

### 3. Apply

Apply with the same validated checkpoint/batch intent. Processing remains deterministic by legacy id and keyset `Id > AfterLegacyThirdPartyId`.

Use `LastProcessedLegacyThirdPartyId` only as the **last contiguously checkpoint-safe** row. An unexpected master/history failure prevents checkpoint advancement past that row and forces further work to remain visible. Resume from the returned safe checkpoint after correcting the operational cause.

### 4. Replay/integrity rules

Replay may reuse canonical master/history only when natural keys and linked transaction evidence are compatible. It must not rewrite canonical status/resolution to match changed legacy data.

A linked canonical history mismatch in clearing house, transaction nature, service type, validation type, effective date or historical outcome is a conflict/ambiguous condition for investigation. Do not repair/rebind manually to force backfill success.

### 5. Cancellation

Cancellation propagates. Tracked state is cleared on observed cancellation/failure. If cancellation occurs after a canonical master row was already persisted but before history work, the master may remain; resume relies on idempotent master reuse. Do not delete the row solely to make replay look clean.

### 6. Logging

Backfill summary logs should remain mode/count/checkpoint-oriented. Per-record failure logging uses internal legacy id plus exception **type** only. Do not log document number, account number, names, evidence references, validation messages, source payloads or exception messages.

## TechnicalReview handling

`TechnicalReview` means the automatic lifecycle cannot safely prove the required context/decision. Typical documented causes include missing effective policy, required response deadline that cannot be resolved, or insufficient/inconsistent backend context.

Operational response:

1. identify the missing/inconsistent configuration/evidence using internal ids and PII-safe diagnostics;
2. correct the authoritative backend configuration/data source where appropriate;
3. retry through the supported revalidation/prenotification flow;
4. verify the lifecycle remains automatic and append-only.

Do **not** create an operator/API/UI path that directly sets an interbank prenotification to Accepted/TacitlyAccepted/Rejected. A terminal attempt is evidence and must not be edited to clear an incident.

Escalate sustained or sudden growth in `TechnicalReview` as a configuration/policy/provider incident for TPA10/runtime investigation.

## Selector and canonical-read monitoring

The selector must derive Customer from the source account in backend and must never evaluate/return candidates outside that Customer. Clearing house, transaction nature, service type and effective date remain backend-derived. `IsSelectable` is derived only from backend eligibility.

Canonical reads require positive Customer scope; by-id reads are Customer-scoped; client-supplied eligibility context is rejected; only masked banking identity is returned.

TPA9B3.4 intentionally has no dedicated application logger in the reviewed selector/read production files. During TPA10 and production operation, monitor platform-level HTTP status/latency/error-rate for these routes and the existing PII-safe canonical/shadow fallback signals. Do not add raw account/document values merely to improve troubleshooting.

Suggested alert conditions for operational review (thresholds are environment-specific and must be agreed in TPA10):

- material increase in selector/read 400 or 404 rates;
- unexpected 5xx on canonical read/selector;
- shadow/canonical fallback failure signal increase;
- customer-isolation/authorization smoke failure;
- response masking regression detected by synthetic smoke tests.

## Rollback procedure

TPA9 introduced no schema/migration. The documented pre-TPA9 application baseline is the TPA8C closure package (`ACHInterbank-R18-TPA8C-COHERENCE-CLOSURE.zip`) referenced by the TPA9A1 record.

For an application rollback:

1. stop new rollout traffic according to the platform deployment procedure;
2. preserve database and telemetry evidence; do not delete canonical master/history rows;
3. deploy the approved pre-TPA9 application package if rollback is authorized;
4. no TPA9 database downgrade is expected because TPA9 added no schema migration;
5. retain `CustomerThirdParty` compatibility storage and canonical data;
6. preserve the last safe backfill checkpoint; do not advance it to hide an incomplete batch;
7. smoke-test legacy compatibility/canonical read behavior and authorization before restoring traffic;
8. if backfill was in progress, resume only after the rollback/forward-fix decision is explicit; rely on natural-key/idempotent replay rather than data deletion.

Legacy/shadow surfaces are diagnostic/compatibility aids, not permission to re-promote legacy global `Active` into transaction authority.

## Known accepted/deferred risks

Do not close these during TPA9 operations:

- `GAP-TPA-001`: legacy master/status model remains less expressive than canonical master/history.
- `GAP-TPA-008`: legacy third-party onboarding remains coupled to prenotification/source-Customer resolution.
- `GAP-TPA-012`: normalized account numbers retain the existing plaintext-at-rest posture; no TPA9 cryptography/schema change.
- `GAP-TPA-013`: generic transaction ingress lacks explicit recipient document type.
- `GAP-TPA-016`: exact historical ACH Colombia V32/V35 transition day remains unresolved and must not be invented.

## TPA10 verification checklist

TPA10 should close the runtime evidence gap, not redesign the TPA9 contracts. Required verification scope:

- [ ] `dotnet build ACHInterbank.sln -c Release` succeeds.
- [ ] Deferred focal SINGLE tests pass, including atomicity, retry, cancellation, rollback/tracker cleanup and telemetry.
- [ ] Deferred focal BULK tests pass, including savepoints, chunk boundaries, concurrency recovery, cancellation and telemetry.
- [ ] Deferred validation lifecycle/contextual eligibility tests pass, including idempotent replay, terminal evidence and fail-closed `TechnicalReview`.
- [ ] Deferred administration/revalidation security tests pass, including `CanReadAch`/`CanManageAch`, Customer ownership, IDOR, generic errors and audit privacy.
- [ ] Deferred backfill tests pass, including dry-run, bounds, safe checkpoint, replay/history conflicts and cancellation.
- [ ] Deferred selector/read/shadow tests pass, including Customer isolation, masking, backend context authority and plain/generic errors.
- [ ] PostgreSQL provider verifies transaction/savepoint/unique-conflict/cancellation behavior.
- [ ] SQL Server provider verifies transaction/savepoint/unique-conflict/cancellation behavior.
- [ ] Metrics/logging are emitted with bounded tags and without PII/exception messages; telemetry failure remains no-throw.
- [ ] Backfill dry-run then apply/resume is exercised on representative data.
- [ ] `TechnicalReview` is observed fail-closed and no manual approve/reject route exists.
- [ ] Selector/read foreign-Customer and client-context manipulation smoke tests fail closed.
- [ ] Rollback drill/approval confirms no TPA9 schema downgrade and preserves canonical/legacy data plus checkpoint state.

Only after these runtime checks should TPA10 make a deployment/certification determination.
