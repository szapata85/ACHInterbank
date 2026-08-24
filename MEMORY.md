# Project Memory — ACHInterbank

## Purpose

This file stores compact, durable project knowledge: canonical functional state, active gaps, important closures, architecture/business decisions, invariants, known traps, release evidence, and the next delivery objective. Current executable repository evidence outranks historical status documents.

## Current Canonical Functional Completion State

### GAP-REFRESH-001 / GAP-REFRESH-001A

STATUS: CLOSED
LAST_REFRESH: 2026-08-23
SCOPE: Operational and transactional functional completion; RET-GAP-019 normative correction
REPOSITORY_BRANCH: ACH-Interbank-Postgresql
REPOSITORY_HEAD: c1ffb6ef1d805307e8046c5761b6332309bcf14d

### ACH Colombia

STATUS: INCOMPLETE
ACTIVE_GAPS: RET-GAP-019

Completed functional flows:
- ordinary outgoing and incoming transactions;
- outgoing Return lifecycle, DB-first concurrency, V35 table-driven physical generation for valid Annex causes, transport, acknowledgement, and replay protection;
- incoming Return parsing, exact original transaction linkage, amount invariant, lifecycle, audit, orphan safety, and idempotency;
- prenotification differential response through RegistrarRespuestaTransaccion;
- generate-only inbound simulator support.

Only confirmed residual:
- RET-GAP-019: DEV14 is an Integra ACH reclamation/workflow novelty, not a physical Addenda 99 cause. V35 contains conflicting physical-cause instructions: section 2.11.12 prescribes R10 for the debit reclamation, while the Addenda 99 field points to Annex 9, whose R10/R12/R13/R29 meanings are contextual and conflict with both that prose and Annex 4. No manual-wide deterministic rule may be persisted until ACH Colombia resolves the erratum.

Decisive evidence:
- ACH Colombia V35 section 2.7.6 and Annex 7 define DEV14 as a debit no-consent reclamation/workflow novelty;
- V35 section 2.11.12, page 95, requires a physical R10 Return and separately a DEV14 reclamation request;
- V35 Addenda 99 requires a three-character Annex 9 cause;
- V35 Annex 9, pages 248-251, and Annex 4, pages 273-274, assign conflicting meanings to R10/R12/R13;
- OfficialNachaGenerationTableDrivenTests.ReturnOutAchV35_WhenCauseIsNotInAnnex9_ShouldFailClosed proves only that DEV14 cannot be emitted physically.

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

STATUS: BLOCKED-BY-NORMATIVE-CONTRADICTION
SCOPE: ACH Colombia / outgoing Return / no-consent debit
PROBLEM: DEV14 is a workflow/reclamation code, but V35 contradicts itself on the physical Rxx. Section 2.11.12 page 95 prescribes R10 for the Return; Addenda 99 explicitly points to Annex 9, where R10 means no authorization/prenotification, R12 means unauthorized originator, R13 is a natural-person receiver-request Return, and R29 is a corporate unauthorized-originator Return. Annex 4 pages 273-274 instead assigns the natural-person receiver-request meaning to R10 and a sold-branch meaning to R12.
CURRENT_BEHAVIOR: The application accepts/stores DEV14 as if it were a Return reason, forwards the selected code unchanged, and the official V35 builder fails closed with NACHA_ALLOWED_VALUE_INVALID without persisting a Return.
REQUIRED_CONTEXT_AVAILABLE: PARTIAL — Customer.PersonType and AchTransaction.IsPrenotification exist, but the Return request carries only TransactionId and ReturnReasonCode; it lacks a structured workflow code, underlying dispute reason, authorization/revocation state, and a reliable receiver-type discriminator in the generation path.
REMAINING_WORK: Obtain an authoritative V35 erratum or formal ACH Colombia clarification that resolves section 2.11.12 versus Annexes 9 and 4. After clarification, define the required context contract before implementing any configuration-driven physical-cause resolution.
DEPENDENCIES: ACH Colombia authoritative clarification; then a context/data-model contract if the confirmed rule is contextual.
ACCEPTANCE_EVIDENCE: Versioned clarification identifying the governing physical-cause table and each DEV14 scenario; explicit treatment of R10/R12/R13/R29 and Annex 10 reasons; verified discriminator inventory; configuration-driven design; fail-closed ambiguity; focal and provider-specific lifecycle/idempotency tests; CI green.
CONFIDENCE: HIGH that DEV14 is workflow-only and V35 is internally contradictory; HIGH that current context is incomplete; no Rxx mapping is accepted as canonical.

## Normative Decisions and Contradictions

### NORM-DEV14-V35-001

DEV14 CLASSIFICATION: WORKFLOW.

V35 contains a manual-internal contradiction:
- section 2.11.12, page 95: generate physical R10 and create a separate DEV14 reclamation request;
- Addenda 99: the physical cause is three characters and must come from Annex 9;
- Annex 9, pages 248-251: R10=no authorization/prenotification, R12=unauthorized originator, R13=natural-person receiver-request Return, R29=corporate unauthorized-originator Return;
- Annex 4, pages 273-274: R10=natural-person receiver-request Return and R12=sold branch.

NORMATIVE CONTRADICTION UNRESOLVED. The Addenda-to-Annex-9 cross-reference is strong technical evidence, but it does not erase the explicit conflicting procedure and formal Annex 4 table. Do not persist or implement DEV14->R10, DEV14->R13, DEV14->R29, or another direct mapping without an authoritative clarification.

## Recently Closed / Reconciled History

- RET-GAP-001, RET-GAP-002: incoming application/audit convergence and DB-first idempotency remain CLOSED.
- RET-GAP-003, RET-GAP-004, RET-GAP-017: ACH Colombia ReturnOut lifecycle, V35 physical generation for valid Annex causes, transport, acknowledgement, and idempotency remain CLOSED.
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

ACH Colombia V35 incoming and outgoing Return Addenda 99 uses:
- a three-character physical cause at positions 4-6, governed by Annex 9;
- original trace at positions 7-21.

DEV14 is not valid in this physical field.

### TRAP-NORMATIVE-VERSION-001

Local RAG contains normative artifacts from different effective dates. Resolve the latest applicable document and supersession before using a rule; do not trust search rank alone. For current ACH Colombia behavior, use V35 April 2026.

### TRAP-RET-V35-TEST-FIXTURES-001

At refresh, the main dotnet CI run for e22fd346 failed 11 tests because two legacy characterization helpers still wrote a five-character cause and original trace at the pre-V35 offset:
- AchIncomingReturnApplicationAndOrphanCharacterizationTests: 7 failures;
- RejectionTotalVsPartialCharacterizationTests: 4 failures.

The corrected V35 ingestion and linker focal suites pass. This is a stale automated-evidence fixture contradiction, not a second functional GAP. It must be repaired before CI can certify a candidate.

## Release Candidate State

### RC-CURRENT

COMMIT: c1ffb6ef1d805307e8046c5761b6332309bcf14d
STATUS: NOT_CERTIFIED
CI: NOT_GREEN — latest relevant dotnet-ci run 32452953613 at e22fd346 failed the main test job; build, ReturnOut multi-database, and outgoing-monitor jobs passed.
RUNTIME_E2E: FOCAL_LOCAL_ONLY — targeted return suites passed; no complete runtime-backed certification for HEAD.
GAUNTLET: NOT_RUN
UAT: NOT_READY

## Next Job

### NEXT-DELIVERY-001

JOB_ID: RET.ACH.OUT.CAUSAL.V35.ERRATUM
OBJECTIVE: Obtain and version an authoritative ACH Colombia clarification resolving the physical-cause conflict among V35 section 2.11.12 page 95, Annex 9 pages 248-251, and Annex 4 pages 273-274 for DEV14 debit reclamations.
WHY_NEXT: RET-GAP-019 is the only active functional gap; implementing any direct or contextual Rxx rule before resolving the V35 contradiction would encode an unsupported regulatory choice.
REQUIRED_CONTEXT: V35 section 2.7.6; section 2.11.12 page 95; Addenda 99 specification; Annex 7 DEV14; Annex 9 R07/R10/R12/R13/R29; Annex 10 debit-reclamation reasons; Annex 4 conflicting table.
EXCLUDED: Production/test/config changes, speculative Rxx mappings, CENIT, accounting/reconciliation, generic observability, and transport refactoring.
ACCEPTANCE: Versioned authoritative clarification that identifies the governing physical cause or complete decision matrix and required discriminator fields, sufficient to design a configuration-driven implementation without inference.

## Context Routing Guardrails

- Current executable repository evidence outranks historical backlog documents and Project Memory.
- ACH Colombia and CENIT must be evaluated independently.
- Operational Return processing remains separate from accounting/reconciliation unless a direct transactional dependency is demonstrated.
- External homologation and release certification are separate from internal functional completion.
- Project Memory is not a normative or source-code oracle.
