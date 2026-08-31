# Project Memory — ACHInterbank

## Purpose

This file stores compact, durable project knowledge: canonical functional state, active gaps, important closures, architecture/business decisions, invariants, known traps, release evidence, and the next delivery objective. Current executable repository evidence outranks historical status documents.

## Current Canonical Functional Completion State

### GAP-REFRESH-002

STATUS: CLOSED — AUDIT/RECONSTRUCTION COMPLETE
LAST_REFRESH: 2026-08-24
SCOPE: Production operational completion beyond the reconstructed Returns backlog
REPOSITORY_BRANCH: ACH-Interbank-Postgresql
REPOSITORY_HEAD: eee79474efd6d8f10b294b8c4640cb3f4a2ff3ba
VERDICT: The local SOAP transaction core, variable cycle creation, Quartz runtime/administration, file-name reservation, and closed Returns capabilities remain intact. Production ordinary operation is incomplete because chamber-ready ordinary profiles, automatic chamber handoff/reception, CENIT chamber-response lifecycle, atomic transaction trace allocation, full operational cycle policy configuration, and unified traceability are not all closed.
NEW_GAPS: OPS-GAP-001, OPS-GAP-002, OPS-GAP-003, OPS-GAP-004, OPS-GAP-005, OPS-GAP-006
REUSED_OWNERS: CENIT-FORMAT-NACHAM, NACHA-RULE-METADATA, RET-GAP-018
RETURNS_STATE: Existing CLOSED/SUPERSEDED Returns items remain unchanged; RET-GAP-019 remains externally blocked.

### GAP-REFRESH-001 / GAP-REFRESH-001A / GAP-REFRESH-001B

STATUS: CLOSED
LAST_REFRESH: 2026-08-23
SCOPE: Operational and transactional functional completion; RET-GAP-019 provisional normative interpretation consolidation
REPOSITORY_BRANCH: ACH-Interbank-Postgresql
REPOSITORY_HEAD: 36af8b1c61ae72c766446c01ecb9ebc0b7c79838

### ACH Colombia

STATUS: INCOMPLETE
ACTIVE_GAPS: RET-GAP-019, OPS-GAP-002; shared NACHA-RULE-METADATA and RET-GAP-018 also apply.

ORDINARY_STATE: PARTIAL. ProcContrapartidas/ProcTransacciones, persistence, duplicate policy, and manual export/upload paths exist. Four explicit published ACH Colombia V35 ordinary profiles cover original/prenotification, inbound/outbound, and credit/debit variants; generation and parsing request version 35.0 and fail closed on missing, ambiguous, unsupported-version, direction, or family selection. Automatic MFT handoff/reception remains absent.
RETURNS_STATE: Previously closed ACH Colombia Return capabilities remain CLOSED. RET-GAP-019 remains the only open Returns-specific item.

### CENIT

STATUS: INCOMPLETE
ACTIVE_GAPS: CENIT-FORMAT-NACHAM, OPS-GAP-003, OPS-GAP-004; shared NACHA-RULE-METADATA and RET-GAP-018 also apply.

ORDINARY_STATE: INTERNALLY CLOSED, with production transport BLOCKED. CENIT-NACHA-OUT-001 closed the May 7, 2026 ordinary PPD/CCD outbound physical profile and official naming. CENIT-NACHA-OUT-002A added 0..N files, explicit membership, PPD/CCD cardinality, post-partition controls, and collision-safe naming. CENIT-NACHA-OUT-002B added independently selectable original/prenotification CTX profiles with official table-driven Type-5/6/7 layouts, 1..9,999 transaction-owned addendas, per-entry addenda sequencing and association, multi-entry batches, and fail-closed profile selection. Evidence: Release build 8 projects with 0 errors/warnings; CTX 6/6, partition boundaries 16/16, ordinary CENIT 6/6, official-profile seeder/resolver 38/38, resolver 8/8, and focused ACH Colombia round-trip regression passed. CENIT-NACHA-OUT-002 is internally CLOSED. Profiles remain non-homologated and CENIT LIVE stays fail-closed; inbound work, Gateway/PO, and ACK/NACK/operator-rejection remain open.
RETURNS_STATE: Previously closed CENIT Return In, Return Out, Return of Return, differential-response, and managed Return transport capabilities remain CLOSED.

### Shared

STATUS: INCOMPLETE for production operational scope
ACTIVE_GAPS: NACHA-RULE-METADATA, RET-GAP-018

COMPLETED_CORE: DB-first incoming duplicate handling, API duplicate policy, classification/audit convergence, clearing-house isolation, local SOAP dispatch for ProcContrapartidas and ProcTransacciones, non-monetary idempotent RegistrarRespuestaTransaccion, variable cycle-count creation, versioned chamber-specific cycle stages and transaction eligibility, Quartz runtime/administration, atomic external filename reservation, and atomic cross-provider transaction trace allocation.
RESIDUAL: Chamber-specific NACHA policy/snapshots remain in code; unified durable lineage remains partial.

## Active Functional Backlog

### OPS-GAP-001

STATUS: CLOSED
SCOPE: ACH Colombia / ordinary original and prenotification / inbound and outbound / V35 NACHA-M profiles
CANONICAL_KEY: ORDINARY/ACHCOL/BIDIRECTIONAL/NACHA_PROFILE/V35_CONFORMANCE
IMPLEMENTED: Explicit V35 profiles OFFICIAL_ACH_{SALIDA,ENTRADA}_{ORIGINAL,PRENOTIFICACION}_V35_0; credit/debit monetary and prenotification type-7 variants; bidirectional resolver integration; legacy ordinary V1/V32 profiles retired; incoming hardcoded profile fallback removed.
EVIDENCE: Commit 36af8b1c61ae72c766446c01ecb9ebc0b7c79838; Release build 8 projects/0 errors/0 warnings; profile resolution 33/33; generation, parsing, round-trip, and existing ACH Return-out profile regression 63/63; incoming ingestion 16/16 and database-backed incoming processing 15/15.
STABILIZATION: V35 Type-7 compatibility regression fixed by 6830260fbad6d5f9673be0ef6269f8cc0a905033; stale legacy Type-99 characterization fixtures aligned by 45d947c1e80701ca147f58f87d1427139f8ede58; GitHub dotnet-ci run 32777030756 succeeded for build-and-test, Outgoing monitor multi-database, and ReturnOut concurrency multi-database. No Return business behavior was reopened.

### OPS-GAP-002

STATUS: MISSING — UNBLOCKED FROM OPS-GAP-006 DEPENDENCY
SCOPE: ACH Colombia / ordinary / bidirectional / managed MFT handoff and reception
CANONICAL_KEY: ORDINARY/ACHCOL/BIDIRECTIONAL/TRANSPORT/MFT_HANDOFF_RECEPTION
REQUIRED: Automate cycle-qualified generation, envelope creation, atomic CFA-managed handoff, inbound pickup/decryption/verification, archive, retry, duplicate protection, result correlation, and monitoring. The enterprise MFT/SFTP product remains external.

### OPS-GAP-003

STATUS: BLOCKED
SCOPE: CENIT / ordinary / bidirectional / Gateway or PO file boundary
CANONICAL_KEY: ORDINARY/CENIT/BIDIRECTIONAL/TRANSPORT/GATEWAY_FILE_EXCHANGE
REQUIRED: Automate cycle-qualified output placement and inbound pickup at the approved Gateway/PO boundary with atomicity, archive, retry, duplicate protection, and monitoring. Completion depends on CENIT-FORMAT-NACHAM; the BLOCKED state is owned by the unresolved approved CFA-to-Gateway operational contract.

### OPS-GAP-004

STATUS: CLOSED — LOCAL RUNTIME E2E CERTIFIED
SCOPE: CENIT / ordinary / inbound chamber responses
CANONICAL_KEY: ORDINARY/CENIT/INBOUND/CHAMBER_RESPONSE/ACK_NACK_OPERATOR_REJECT
IMPLEMENTED: Dedicated CENIT ACK/NACK/operator-rejection XML, reconciliation, and no-activity lifecycle linked to ordinary outbound file membership; exact file/transaction correlation; persisted unresolved/ambiguous outcomes; semantic idempotency; protected terminal transitions; ProblemDetails API; and CENIT operational UI with pending and terminal-state presentation. FileNack preserves every FileErrorHandling item, while reconciliation and no-activity are owned by the operational CENIT cycle rather than an ordinary export. This remains independent from Returns, differential responses, and RegistrarRespuestaTransaccion.
EVIDENCE: CENIT-RUNTIME-E2E-001 passed official-like namespaced ACK, file NACK, operator rejection, multi-error FileNack, reconciliation, and no-activity fixtures; Release build 8 projects with 0 errors/warnings; lifecycle/API/persistence 16/16; Gateway adapter 1/1; outbound partitioning 17/17; table-driven NACHA-M 70/70; Angular operations 8/8; Docker SQL Server/API/SPA health green; and real SPA -> API -> database Playwright 1/1.
RUNTIME: The local folder-backed test Gateway used atomic input/output/archive exchange and certified PPD+ACK, CCD+NACK, CTX multi-error operator rejection, reconciliation, no activity, replay idempotency, terminal conflict protection, durable lineage, API, and operational SPA visibility. No Banco de la República connectivity or institutional homologation was executed; OPS-GAP-003 remains the external Gateway/PO boundary.

### OPS-GAP-005

STATUS: CLOSED
SCOPE: Shared / outgoing transaction identity
CANONICAL_KEY: ORDINARY/SHARED/OUTBOUND/SEQUENCE/TRANSACTION_TRACE_ALLOCATION
ROOT_CAUSE: TransactionPersister used MAX()+1 followed by an existence check, so independent contexts could select the same daily trace before either insert committed.
IMPLEMENTED: A per-originating-DFI/per-effective-date database allocator uses PostgreSQL ON CONFLICT DO UPDATE RETURNING and SQL Server serializable UPDLOCK/HOLDLOCK allocation on short dedicated connections. Provider migrations backfill allocator state, synchronize externally inserted traces through database triggers, and enforce unique (EffectiveEntryDate, TraceNumber). OPS-GAP-005A corrected CENIT Return-of-Return creation to use this allocator for the target cycle processing date instead of the independent return-trace sequence keyed by request date.
CONTRACT: 15 digits (8-digit originating DFI + 7-digit sequence); daily reset; range 1-6999999; uniqueness by effective entry date and complete trace number.
EVIDENCE: Core commit 4eff8e7; CENIT ROR correction 872d356; Release build 0 errors/0 warnings; focal CENIT ROR 13/13; PostgreSQL and SQL Server RaceMatrix 1/1 each; OutboundReturnMultiDb 4/4; GitHub Actions run 32799729074 succeeded on 872d356. Core real-provider independent-context concurrency 96/96 and clean two-API runtime concurrency 100/100 per provider had persisted=100, distinct=100, duplicates=0; empty-database migrations, seed/bootstrap, second startup, and live/ready health passed for both providers.

### OPS-GAP-006

STATUS: CLOSED
SCOPE: Shared / clearing-house cycle policy and runtime consumption
CANONICAL_KEY: ORDINARY/SHARED/BIDIRECTIONAL/CYCLE_POLICY/OPERATIONAL_STAGES_ELIGIBILITY
DECISION: ClearingHouseCycleConfig is the single effective-dated, timezone-resolved chamber policy. Each version owns arbitrary cycles, open/input-cutoff/processing-close/output-release stages, and eligibility for monetary credit/debit, credit/debit prenotification, Return, and Return-of-Return. Resolution rejects missing, overlapping, ambiguous, invalid-timezone, duplicate-cycle, or invalid-stage policies.
CONSUMERS: Transaction creation/validation, outbound assignment, inbound resolution/queue execution, NACHA generation, ProcContrapartidas and ProcTransacciones dispatch, Return-of-Return generation, and Quartz synchronization consume the authoritative policy or its scheduled cycle snapshot.
EVIDENCE: Release build 8 projects/0 errors/0 warnings; impacted backend regression 242/242; scheduler 5/5; Angular administration 9/9 and production build succeeded; PostgreSQL and SQL Server migration/model checks and fresh-database persistence tests 1/1 each. Runtime resolver proof held ACHCOL ACH-V1 with 7 cycles and Cycle 1 Return allowed simultaneously with CENIT CENIT-V1 with 5 cycles and Cycle 1 Return denied; effective-version and timezone boundaries were deterministic.
CI_STABILIZATION: OPS-GAP-006 CI stabilization completed. Root cause: STALE TEST FIXTURE. Commit: d910301c9a3788197546e7c698087d8185389b08. GitHub Actions run: 33015421841. build-and-test: SUCCESS.
DEPENDENCY: OPS-GAP-002 is unblocked from the OPS-GAP-006 dependency perspective; it remains a separate unimplemented gap.

### CENIT-FORMAT-NACHAM

STATUS: INTERNALLY_CLOSED / EXTERNAL_HOMOLOGATION_PENDING — EXISTING OWNER REUSED
CURRENT_DELTA: The May 7, 2026 specification drives table-driven ordinary PPD/CCD and CTX profiles in both directions. CENIT-NACHA-IN-001 added explicit inbound original/prenotification metadata, reused the official physical descriptors under section 7.3.1, enabled CTX 1..9,999 owned Addenda parsing with per-entry sequence/trace association, preserved received operator batches, and validated physical T8/T9 controls. Evidence: Release build 8 projects/0 errors/0 warnings; inbound focal 19/19; official profiles 46/46; shared inbound parser 12/12; ingestion 16/16; Return-In 8/8; Return-of-Return 2/2. CENIT-FORMAT-NACHAM is internally closed. External homologation remains pending; OPS-GAP-003 Gateway/PO and OPS-GAP-004 ACK/NACK remain separate operational gaps.

### NACHA-RULE-METADATA

STATUS: PARTIAL — EXISTING OWNER REUSED
CURRENT_DELTA: Profile resolution and physical record reading are table-driven, but current official generation still contains chamber-specific normative snapshots, batch policy, settlement-date policy, and cross-field branches in code. Business variability must remain behind the profile/configuration boundary.

### RET-GAP-018

STATUS: PARTIAL — EXISTING OWNER REUSED
CURRENT_DELTA: Durable transaction/cycle/file and monitoring components exist, but the transaction detail selects a file by cycle rather than exact membership and does not reconstruct dispatcher/SOAP/transport/ACK lineage. This is now a direct operational dependency, not a new gap.

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
- RET-GAP-008 and RET-GAP-009 remain outside the ordinary transactional backlog unless a direct dependency is demonstrated. RET-GAP-018 is reused as PARTIAL because unified durable lineage is a direct production-operability dependency.
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

COMMIT: eee79474efd6d8f10b294b8c4640cb3f4a2ff3ba
STATUS: NOT_CERTIFIED
CI: NOT_REVALIDATED FOR THIS HEAD — previous relevant evidence is not transferable.
RUNTIME_E2E: FOCAL_LOCAL_ONLY — cycle configuration 7/7 and traceability 1/1 passed from the existing Release build; a further focal CENIT validation run was stopped after not completing in the audit window. No chamber transport E2E exists.
GAUNTLET: NOT_RUN
UAT: NOT_READY

## Next Job

### NEXT-DELIVERY-001

JOB_ID: OPS.ACHCOL.ORDINARY.V35.PROFILES.1
OBJECTIVE: Implement the current ACH Colombia V35 ordinary original/prenotification profile family for inbound and outbound flows and make those flows fail closed on profile/configuration evidence rather than V32 seeds or absent profiles.
WHY_NEXT: It is the first internal P0 dependency for both ordinary directions, the V35 source is available, and it requires no unresolved external clarification.
INCLUDED: V35 rule extraction, profile/layout/field/rule data, inbound and outbound selection, removal of V32 ordinary-profile authority, profile-boundary cleanup required by the flow, focal/golden/negative tests, build, and local runtime generation/parsing evidence.
EXCLUDED: Returns, RET-GAP-019, CENIT, MFT/SFTP transport, external homologation/certification, cycle-policy redesign, and unrelated UI.
ACCEPTANCE: Published unambiguous V35 ordinary profiles for both directions and prenotification; no legacy fallback; no ordinary V32 authority; generated and parsed artifacts pass V35 structural/semantic tests; missing/ambiguous profile fails before persistence or dispatch; build and focal regression tests pass.

## Recent Sessions
- 2026-08-31: CENIT-RUNTIME-E2E-001 locally certified the ordinary CENIT outbound-to-response lifecycle through Docker SQL Server/API/SPA, an atomic folder-backed test Gateway, real persistence/API, and Playwright; ACK, NACK, multi-error operator rejection, reconciliation, no activity, idempotency, and terminal protection passed, while external Banco Gateway/PO homologation remains separate under OPS-GAP-003.
- 2026-08-30: CENIT-NACHA-IN-001 implemented direction-aware ordinary inbound PPD/CCD/CTX profiles by reusing the official physical descriptors under section 7.3.1; multi-Addenda ownership, received batches, controls, profile failures, and shared Return/ACH regressions passed. CENIT-FORMAT-NACHAM is internally closed; external homologation and OPS-GAP-003/004 remain separate.
- 2026-08-30: CENIT-NACHA-OUT-002B implemented the official CENIT CTX table-driven outbound profile from sections 3.2/5.1/5.2/6.2 and Annexes 1.2/1.4/1.5/1.8/1.9, including multi-addenda sequencing/association and fail-closed profile selection; CENIT-NACHA-OUT-002 is internally closed, with external homologation pending.
- 2026-08-30: CENIT-NACHA-OUT-002A resolved the outbound single-file blocker and PPD/CCD cardinality/batch allocation with an additive 0..N artifact result, post-partition controls, exact membership, and unique per-file naming.

- 2026-08-25: OPS-GAP-006 CLOSED with effective-dated chamber-isolated cycle stages and transaction eligibility consumed by validation, resolution, generation, dispatch, and Quartz; PostgreSQL, SQL Server, backend, and Angular evidence passed.

- 2026-08-24: OPS-GAP-005 CLOSED with provider-native atomic daily trace allocation, database uniqueness enforcement, and clean PostgreSQL/SQL Server two-replica proofs of 100/100 persisted distinct traces.

- 2026-08-24: OPS-GAP-001 remains CLOSED after V35 Type-7 parser stabilization and test-only legacy Type-99 fixture alignment; GitHub dotnet-ci run 32777030756 was fully green.

- 2026-08-24: GAP-REFRESH-002 reconstructed the non-Returns production backlog at HEAD eee79474; six OPS gaps were added, existing CENIT/NACHA/traceability owners were reused, and closed Returns state was preserved.

## Context Routing Guardrails

- Current executable repository evidence outranks historical backlog documents and Project Memory.
- ACH Colombia and CENIT must be evaluated independently.
- Operational Return processing remains separate from accounting/reconciliation unless a direct transactional dependency is demonstrated.
- External homologation and release certification are separate from internal functional completion.
- Project Memory is not a normative or source-code oracle.
