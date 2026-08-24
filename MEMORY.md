# Project Memory — ACHInterbank

## Purpose

This file stores compact, durable project knowledge: canonical functional state, active gaps, important closures, architecture/business decisions, invariants, known traps, release evidence, and the next delivery objective. Current executable repository evidence outranks historical status documents.

## Current Canonical Functional Completion State

### GAP-REFRESH-001 / GAP-REFRESH-001A / GAP-REFRESH-001B

STATUS: CLOSED
LAST_REFRESH: 2026-08-23
SCOPE: Operational and transactional functional completion; RET-GAP-019 provisional normative interpretation consolidation
REPOSITORY_BRANCH: ACH-Interbank-Postgresql
REPOSITORY_HEAD: efa3bc75f3c020af149555e2b469fe6102a5217f

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
SEMANTIC_STATE: CONFIRMED-OPEN — evidence-supported interpretation; external authoritative clarification pending
SCOPE: ACH Colombia / outgoing Return / no-consent debit
PROBLEM: DEV14 is a business/claims workflow code, while the physical NACHA-M Addenda 99 return cause must be an Rxx. V35 section 2.11.12 page 95 prescribes R10 for a previously applied debit returned after a receiver claim and separately requires a DEV14 reversal request when the receiver does not accept the debit. Section 6.6 makes the physical cause mandatory, AN(3), at positions 4-6, and explicitly delegates it to Annex 9, whose R10/R12/R13/R29 meanings are contextual and conflict with section 2.11.12; Annex 4 also conflicts with Annex 9.
PROVEN: DEV14 classifies the no-consent claim/workflow and cannot be physically emitted in the three-character Addenda 99 cause field. The section 6.6 -> Annex 9 cross-reference is the strongest physical-field evidence. This contradiction already exists in V32, and V35 change history records earlier Annex 9 changes while V34 -> V35 identifies no change to these return-cause semantics.
PROVISIONAL_INTERPRETATION: The strongest supported project hypothesis is that DEV14 remains the claim/workflow classification while the physical Rxx is selected from the actual underlying reason and context defined by Annex 9. This hypothesis is not an authoritative ACH Colombia rule and does not define a complete deterministic matrix.
CURRENT_BEHAVIOR: The application accepts/stores DEV14 as a workflow-like Return reason and forwards the selected code unchanged; the official V35 builder rejects it with NACHA_ALLOWED_VALUE_INVALID before persisting a generated Return or state event. This fail-closed behavior is required while the ambiguity remains.
PROHIBITED_UNTIL_CLARIFIED: Do not implement DEV14->R10, DEV14->R13, DEV14->R29, any other universal mapping, or a speculative contextual matrix.
AUTHORITY_STATUS: Formal clarification has been requested from ACH Colombia and is pending.
REQUIRED_CONTEXT_AVAILABLE: PARTIAL — Customer.PersonType and AchTransaction.IsPrenotification exist, but the Return request carries only TransactionId and ReturnReasonCode; it lacks a structured workflow code, underlying dispute reason, authorization/revocation state, and a reliable receiver-type discriminator in the generation path.
EXACT_CLOSURE_EVIDENCE: Either (1) official ACH Colombia clarification specifying the exact physical Rxx rule, or (2) an authoritative decision matrix specifying every contextual branch required to resolve the physical Rxx deterministically.
FUTURE_IMPLEMENTATION_GUARDRAIL: After authoritative evidence arrives and the gap is reassessed, prefer a deterministic configuration/catalog-driven rule rather than scattered hardcoded conditions; retain fail-closed handling for missing or ambiguous context.
CONFIDENCE: HIGH that DEV14 is workflow-only, the physical field requires Annex 9 Rxx semantics, and V35 is internally contradictory; no DEV14-to-Rxx mapping is canonical.

## Normative Decisions and Contradictions

### NORM-DEV14-V35-001

DEV14 CLASSIFICATION: BUSINESS/CLAIMS WORKFLOW.
PHYSICAL ADDENDA 99 CLASSIFICATION: ANNEX 9 Rxx.

V35 contains a manual-internal contradiction:
- section 2.7.6 and Annex 7: DEV14 identifies a request to return a non-consented ACH debit;
- section 2.11.12, page 95: generate physical R10 for the receiver-claim Return and create a separate DEV14 reversal request when the receiver does not accept the debit;
- section 6.6 Addenda 99: the physical cause is mandatory, AN(3), positions 4-6, and must be selected according to Annex 9;
- Annex 9, pages 248-251: R10=no authorization/prenotification, R12=unauthorized originator, R13=natural-person receiver-request Return, R29=corporate receiver-request Return under its catalog conditions;
- Annex 4, pages 273-274: R10=natural-person receiver-request Return and R12=sold branch;
- V32 already contains the same section 2.11.12 R10/DEV14 split and Annex 9 mismatch; V35 change history records earlier Annex 9 updates, while V34 -> V35 lists no change to these cause semantics.

NORMATIVE CONTRADICTION UNRESOLVED. Section 6.6 -> Annex 9 is the strongest evidence governing the physical field, but it does not erase the conflicting procedure or define a complete deterministic DEV14-to-Rxx matrix. The evidence-supported project hypothesis is workflow DEV14 plus a context-specific Annex 9 Rxx; it is explicitly provisional and not an authoritative ACH Colombia rule. Keep generation fail-closed, and do not persist or implement DEV14->R10, DEV14->R13, DEV14->R29, another universal mapping, or a speculative matrix. Formal ACH Colombia clarification is pending.

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
