# Project Memory — ACHInterbank

## Purpose

This file stores compact, durable project knowledge: canonical functional state, active gaps, important closures, architecture/business decisions, invariants, known traps, release evidence, and the next delivery objective. Current executable repository evidence outranks historical status documents.

## Current Canonical Functional Completion State

### GAP-REFRESH-001

STATUS: CLOSED
LAST_REFRESH: 2026-08-23
SCOPE: Operational and transactional functional completion
REPOSITORY_BRANCH: ACH-Interbank-Postgresql
REPOSITORY_HEAD: 178d01b148cec66c1909fb110da3577ad0c5a8e1

### ACH Colombia

STATUS: INCOMPLETE
ACTIVE_GAPS: RET-GAP-019

Completed functional flows:
- ordinary outgoing and incoming transactions;
- outgoing Return lifecycle, DB-first concurrency, V35 table-driven physical generation, transport, acknowledgement, and replay protection;
- incoming Return parsing, exact original transaction linkage, amount invariant, lifecycle, audit, orphan safety, and idempotency;
- prenotification differential response through RegistrarRespuestaTransaccion;
- generate-only inbound simulator support.

Only confirmed residual:
- RET-GAP-019: the business request code DEV14 has no authoritative deterministic mapping to a physical three-character ACH Colombia V35 Addenda 99 Rxx cause. The official V35 ReturnOut builder correctly fails closed for DEV14.

Decisive evidence:
- commits ec16753d, 56684255, e22fd346;
- OfficialNachaGenerationTableDrivenTests.ReturnOutAchV35_WhenCauseIsNotInAnnex9_ShouldFailClosed;
- AchIncomingReturnIngestionServiceTests and IncomingNachaTransactionLinkerTests;
- ACH Colombia V35 sections 2.7.6, 6.6, 6.7, Annexes 7 and 9 retrieved through Local RAG.

### CENIT

STATUS: COMPLETE
ACTIVE_GAPS: none

Completed functional flows:
- ordinary outgoing and incoming transactions;
- Return In 2026 parsing, cause validation, original linkage, lifecycle, audit, orphan safety, and replay protection;
- Return Out 2026 physical generation, persistence, dispatch, acknowledgement/result correlation, retry, and idempotency;
- CENIT Return of Return creation and ingestion with clearing-house-specific eligibility/window rules;
- prenotification differential response and generate-only simulator support.

Decisive evidence:
- commits 189cf2d1, 24852318, 03d03457, 54606de2, d9306080;
- CenitReturnIn2026ParserTests (8 passing at refresh);
- CenitReturnOut2026Tests (5 passing at refresh);
- CenitReturnOfReturn2026Tests (13 passing at refresh);
- AchOutboundReturnTransportEndToEndTests (7 passing at refresh);
- successful CENIT feature-commit CI and successful multi-database ReturnOut jobs;
- CENIT Manual de Especificaciones de Archivos NACHA-M dated 2026-05-07 retrieved through Local RAG.

### Shared

STATUS: COMPLETE for current operational/transactional scope
ACTIVE_GAPS: none

Shared completed capabilities include DB-first incoming idempotency, classification/audit convergence, exact correlation, clearing-house isolation, generate-only simulator behavior, and non-monetary idempotent differential responses.

## Active Functional Backlog

### RET-GAP-019

STATUS: CONFIRMED-OPEN
SCOPE: ACH Colombia / outgoing Return / no-consent debit
PROBLEM: DEV14 is a valid internal/business request but is not a valid physical three-character Annex 9 cause. V35 does not identify a unique Rxx mapping or the contextual attributes needed to select one deterministically.
CURRENT_BEHAVIOR: The official V35 table-driven builder fails closed with NACHA_ALLOWED_VALUE_INVALID and does not persist a generated Return.
REMAINING_WORK: Obtain authoritative ACH Colombia evidence or an approved later business decision defining the deterministic DEV14-to-Rxx resolution matrix; only then implement configuration/catalog-driven resolution and prove the physical file and lifecycle.
DEPENDENCIES: Primary ACH Colombia normative/business decision.
ACCEPTANCE_EVIDENCE: Versioned authoritative rule; configuration-driven mapping; focal generation tests for every branch and fail-closed ambiguity; SQL Server/PostgreSQL lifecycle/idempotency evidence; CI green.
CONFIDENCE: HIGH

## Recently Closed / Reconciled History

- RET-GAP-001, RET-GAP-002: incoming application/audit convergence and DB-first idempotency remain CLOSED.
- RET-GAP-003, RET-GAP-004, RET-GAP-017: ACH Colombia ReturnOut lifecycle, V35 physical generation, transport, acknowledgement, and idempotency remain CLOSED.
- RET-GAP-006, RET-GAP-016: former CENIT ReturnOut/ReturnIn gaps are CLOSED by the 2026 layouts, services, tests, and transport implementation.
- RET-GAP-012: CENIT Return of Return is CLOSED.
- RET-GAP-013, RET-GAP-014: ACH Colombia and CENIT prenotification differential responses are CLOSED by clearing-house-isolated RegistrarRespuestaTransaccion processing.
- RET-GAP-015: simulator support is CLOSED for the current generate-only scope; differential responses are not physical simulator files.
- RET-GAP-005 is SUPERSEDED by the later CENIT 2026 file contract and managed transport design.
- RET-GAP-011 is SUPERSEDED by the current no-participant-ACH-ROR decision; CENIT ROR remains a separate rail.
- RET-GAP-008, RET-GAP-009, RET-GAP-018 are OUT-OF-SCOPE for the current functional completion backlog: accounting/reconciliation and generic observability remain separate unless a transactional flow directly depends on them.
- RET-CENIT-IN-000, RET-CENIT-IN-001, RET-CENIT-OUT-001, RET-CENIT-ROR-001, CENIT-RET-TRANSPORT-001, and DIFF-RESP-001 are delivery aliases/sub-jobs, not additional active functional gaps.
- Do not reopen a CLOSED item without contradictory current repository evidence.

## Durable Architecture and Business Decisions

### DEC-NACHA-PROFILES-001

NACHA-M behavior is configuration/profile driven. Official flows must use published, unambiguous profiles and fail closed when required profiles are absent or invalid. Do not add hardcoded legacy layouts.

### DEC-CLASSIFICATION-001

Operational classification is per entry:
- CFA as source -> Debit -> ProcContrapartidas;
- external institution as source -> Credit -> ProcTransacciones;
- Prenotification -> no monetary SOAP call;
- ambiguous semantics -> ManualReviewRequired.

### DEC-INBOUND-SIMULATOR-001

The inbound simulator represents an external non-default counterparty and is generate-only: create the file and metadata, but do not auto-import, transmit, or mutate transaction state.

### DEC-DIFFERENTIAL-001

Prenotification differential responses are processed through RegistrarRespuestaTransaccion rather than a physical differential NACHA file. The operation is non-monetary and idempotent.

### DEC-ROR-POLICY-001

Return-of-return eligibility depends on the original Return clearing house, configured cause catalog, transaction type, and elapsed operational window. ACH Colombia and CENIT are separate rails and must not infer rules from each other.

## Durable Invariants

### INV-RETURN-CORRELATION-001

An incoming Return must match the exact original trace and the original transaction amount. A trace match with a different amount is not an exact correlation and must not mutate the original transaction.

### INV-DIFFERENTIAL-IDEMPOTENCY-001

Differential responses must not move money and must not be applied twice.

### INV-DEFINITIVE-RESPONSE-001

A transaction with a successful or functionally definitive SOAP response must not be resent.

## Known Traps

### TRAP-DATE-UTC-001

Do not derive business dates from UTC date when the configured operational timezone governs the day.

### TRAP-FUTURE-CYCLE-001

Cycle selection and simulator requests must not target a future operational date.

### TRAP-ADDENDA99-001

ACH Colombia V35 incoming Return Addenda 99 uses:
- three-character cause at positions 4-6;
- original trace at positions 7-21.

### TRAP-RET-V35-TEST-FIXTURES-001

At refresh, the main dotnet CI run for e22fd346 failed 11 tests because two legacy characterization helpers still wrote a five-character cause and original trace at the pre-V35 offset:
- AchIncomingReturnApplicationAndOrphanCharacterizationTests: 7 failures;
- RejectionTotalVsPartialCharacterizationTests: 4 failures.

The corrected V35 ingestion and linker focal suites pass. This is a stale automated-evidence fixture contradiction, not a second functional GAP. It must be repaired before CI can certify a candidate.

## Release Candidate State

### RC-CURRENT

COMMIT: 178d01b148cec66c1909fb110da3577ad0c5a8e1
STATUS: NOT_CERTIFIED
CI: NOT_GREEN — latest relevant dotnet-ci run 32452953613 at e22fd346 failed the main test job; build, ReturnOut multi-database, and outgoing-monitor jobs passed.
RUNTIME_E2E: FOCAL_LOCAL_ONLY — targeted return suites passed; no complete runtime-backed certification for HEAD.
GAUNTLET: NOT_RUN
UAT: NOT_READY

## Next Job

### NEXT-DELIVERY-001

JOB_ID: RET.ACH.OUT.CAUSAL.V35.EVIDENCE
OBJECTIVE: Obtain and version the authoritative ACH Colombia decision that maps a DEV14 no-consent return request to a physical Annex 9 Rxx cause, or defines the exact contextual decision matrix.
WHY_NEXT: RET-GAP-019 is the only confirmed active functional gap and blocks complete ACH Colombia outgoing no-consent Return processing.
EXCLUDED: CENIT implementation, accounting/reconciliation, generic reporting/observability, infrastructure refactoring, and speculative mappings.
ACCEPTANCE: Authoritative versioned evidence sufficient to define a deterministic configuration-driven rule; no implementation should be invented before that evidence exists.

## Context Routing Guardrails

- Current executable repository evidence outranks historical backlog documents and Project Memory.
- ACH Colombia and CENIT must be evaluated independently.
- Operational Return processing remains separate from accounting/reconciliation unless a direct transactional dependency is demonstrated.
- External homologation and release certification are separate from internal functional completion.
- Project Memory is not a normative or source-code oracle.
