# Project Memory — ACHInterbank

This file records current durable decisions and verified project state. It is not a normative, source-code, runtime, or CI oracle. Current evidence can supersede an entry. Detailed human-facing documentation remains in `docs/`; historical versions remain in Git.

## Current accepted baseline

- BRANCH: ACH-Interbank-Postgresql
- ACCEPTED_FUNCTIONAL_COMMIT_BEFORE_REPOSITORY_CLEANUP: 22d10822c5222c3c72a76b7279d6a885dfcbc75b
- At that exact SHA, latest remote GitHub Actions baseline succeeded for build-and-test, NACHA config immutability on SQL Server and PostgreSQL, ReturnOut concurrency on both providers, and Outgoing monitor on both providers. This is product-scope CI evidence, not a certification of a later maintenance commit or external production readiness.
- ACH Colombia current normative authority: V35, April 2026. ACH Colombia V36 is not current implementation authority without a later explicit decision. Current accepted CENIT ordinary documentation: May 7, 2026 NACHA-M manual and applicable current CENIT documents. Evaluate the two rails independently.
- GAUNTLET: not established for the current maintenance descendant. UAT/USER_ACCEPTED/RELEASE_READY: not established globally.

## Current capability state

| ID | State | Durable contract |
| --- | --- | --- |
| DBCONTEXT_IMMUTABILITY_HARDENING | CLOSED | AchDbContext synchronous/asynchronous tracked writes enforce prior-publication immutability, including transitive owned records/rules, detached writes, append-only publication history, and narrow lifecycle retirement. No production bypass. |
| NACHA-CONFIG-IMMUTABILITY-CI-COVERAGE | CLOSED | Published NACHA configuration immutability is protected by accepted real SQL Server and PostgreSQL CI coverage. |
| OFFICIAL_PROFILE_VERSION_WORKFLOW | CLOSED | Supported ordinary flows select effective immutable PUBLICADO/PUBLISH winners by exact dimensions, priority, then highest major/minor; explicit exact/major constraints remain supported and malformed or ambiguous selection fails closed. |
| INBOUND_TXCODE_PUBLICATION_BASELINE | CLOSED | ACH 35.1/V35 and CENIT PPD/CCD/CTX inbound successors publish complete immutable T6 tuple authority; predecessor snapshots remain intact. |
| INBOUND_PRESELECTION_CUTOVER | CLOSED | Supported ordinary inbound selection uses PUBLISH T5 service, T6 tuple, version priority, and snapshot layout; missing, ambiguous, malformed, or conflicting authority fails closed. |
| INBOUND_POSTSELECTION_NON_PSE | CLOSED | Supported ACH ORIGINAL/PRENOTIFICACION and CENIT PPD/CCD/CTX ORIGINAL/PRENOTIFICACION parse and validate against the exact selected immutable PUBLISH snapshot. SUPPORTED NON-PSE ORDINARY INBOUND STATIC LAYOUT AUTHORITY: NONE. |
| SUPPORTED ORDINARY OUTBOUND STATIC LAYOUT AUTHORITY | CLOSED | PUBLISH snapshot governs supported ordinary outbound runtime; exact official descriptor comparison occurs at publication/bootstrap, not after materialization. |
| TXCODE_ORDINARY | CLOSED | Registration converges every reachable immutable PUBLISH transaction-code authority; generation validates persisted code and Type-7 semantics do not infer from literal codes. |
| GAP-CENIT-PUBLISHED-CARDINALITY-001 | CLOSED | CENIT inbound/outbound cardinality is selected from the exact PUBLISH profile and transaction tuple; historical snapshots remain readable and immutable. |
| OPS-GAP-001 | CLOSED | ACH Colombia V35 ordinary original/prenotification profile conformance. |
| OPS-GAP-002 / OPS-GAP-002.2A–D | INTERNALLY CLOSED | Managed MFT handoff, persistent administration, monitoring, retry permission gate, and fresh-provider migration discovery are internally certified; enterprise deployment, topology, credentials, connectivity, contract, and homologation remain external. |
| OPS-GAP-004 | CLOSED LOCAL RUNTIME E2E | CENIT ACK/NACK/operator rejection, reconciliation, and no-activity lifecycle is locally certified; external Gateway homologation is separate. |
| OPS-GAP-005 | CLOSED | Provider-native atomic daily trace allocation with database uniqueness and 15-digit trace format; CENIT Return-of-Return uses the target-cycle date allocator. |
| OPS-GAP-006 | CLOSED | Effective-dated chamber-isolated ClearingHouseCycleConfig governs stages, timezone, transaction eligibility, and consumers. |
| CENIT-FORMAT-NACHAM | INTERNALLY CLOSED | May 7, 2026 table-driven ordinary PPD/CCD/CTX profiles are implemented in both directions; external homologation remains pending. |
| RET-GAP-018 | CLOSED | Unified durable lineage, exact-file membership, dispatch/SOAP and chamber-response monitoring. |
| RET-GAP-019 | CLOSED | Accepted R10 Return / DEV14 Claims split is implemented and certified; Claims module remains separate. |
| NACHA-RULE-METADATA | PARTIAL | Closed ordinary transaction-code, outbound/inbound authority, CENIT cardinality, version workflow, and immutability slices do not close the whole program. Other CTX/metadata residuals remain. |
| PSE-SCOPE-001 | DEFERRED / OUT OF CURRENT SCOPE | PSE-specific inbound trace/range semantics need separate authoritative documentation and certification. |

The accepted closed slices above must not be reopened without contradictory current evidence. Internal closure does not imply external homologation or global release readiness.

## Additional accepted contracts

- Current ordinary profile winners: inbound ACH 35.1 and CENIT PPD/CCD/CTX 1.2; outbound ACH 35.1, CENIT PPD/CCD original/prenotification 1.3, CTX original 1.2, CTX prenotification 1.3. Selection is effective-date and request dependent; predecessor PUBLISH artifacts remain immutable.
- CLOSE-CENIT-RECEIVING-2026-001: CLOSED. Under the May 7, 2026 CENIT manual, PPD monetary Credit/Debit requires exactly one T7; PPD prenotification Credit allows 0–1 and Debit exactly one; CCD monetary/prenotification Credit/Debit requires one; CTX monetary allows 1–9,999 and CTX prenotification requires one. Owned association, per-entry sequence, count, trace shape, range, and duplicate safeguards remain. Operator receiving files need not start at suffix 0000001 or ascend globally/per batch.
- Previously accepted Return closures remain closed: RET-GAP-001–004, 006, 012–014, 016–019 and CENIT Return In/Out/ROR and managed Return transport. RET-GAP-005 and 011 are superseded; RET-GAP-008/009 are outside ordinary transactional backlog absent a demonstrated dependency. Do not infer ACH Colombia Return-of-Return policy from CENIT.
- MFT_EXTERNAL_READINESS: APPLICATION_READY_EXTERNAL_DEPLOYMENT_PENDING. Internal manual retry gating and provider migrations are certified; no external enterprise MFT connectivity, credentials, or homologation is certified.

## Open residuals

- OPS-GAP-003: BLOCKED on approved CFA-to-CENIT Gateway/PO operational file-exchange contract. CENIT LIVE remains fail closed pending that boundary; local simulator is disabled by default and prohibited in Production.
- ACHCOL-CLAIMS-DEV14: CONFIRMED GAP, IMPLEMENTATION PENDING. DEV14 belongs to the Claims module, not NACHA-M ReturnOut.
- NACHA-RULE-METADATA: PARTIAL. Do not relabel the whole program CLOSED because supported ordinary non-PSE authority is closed.
- External ACH Colombia managed MFT/GoAnywhere deployment and homologation remain operational dependencies.
- Global UAT and release certification require new exact-commit evidence.

## Durable decisions and invariants

### DEC-NACHA-PROFILES-001 / PUBLISH authority

Official NACHA-M behavior is profile/configuration driven. Supported ordinary runtime must use the exact selected immutable PUBLISH snapshot for applicable semantics and physical layout; required missing, invalid, or ambiguous authority fails closed. Publication snapshots preserve generation-critical data and trace lineage by value. Published profiles and referenced definitions cannot be silently changed in place; a new generation/version is required. Official-profile seeding compares existing published definitions semantically. Direct AchDbContext tracked writes enforce this invariant. Do not add legacy hardcoded static fallback. ACH 35.1/V35 and the accepted CENIT publication successors are current ordinary winners according to effective date and selection; historical snapshots remain intact.

### DEC-ACHCOL-R10-DEV14-001

Accepted 2026-09-06 for ACH Colombia non-consented debit. R10 is the NACHA-M Return path within the configured maximum four-cycle same-day rejection window when no prenotification exists or no Receiver User authorization/agreement is explicitly established. Unknown data does not prove absent authorization. Effective-dated AchReturnPolicy.MaxCycles and scheduled cycle snapshots govern the opportunity; do not treat four cycles as an absolute CycleNumber, four hours, or a hardcoded cutoff. Expired or previous-day cases belong to the separate DEV14 Claims path. DEV14 is rejected before ReturnOut persistence/generation, absent from its active cause catalog, and never mapped to R10/R13/R29/another Rxx or serialized into Addenda 99. RET-GAP-019 is CLOSED; ACHCOL-CLAIMS-DEV14 remains pending. Evidence: GitHub Actions 34154650922 passed broad backend and isolated SQL Server/PostgreSQL ReturnOut/monitor jobs.

### DEC-CLASSIFICATION-001 / DEC-INBOUND-SIMULATOR-001 / DEC-DIFFERENTIAL-001

Classification is per entry: CFA-source Debit -> ProcContrapartidas; external-source Credit -> ProcTransacciones; prenotification -> no monetary SOAP; ambiguity -> ManualReviewRequired. The inbound simulator represents an external non-default counterparty and is generate-only: no automatic import, transmission, or transaction mutation. Prenotification differential responses use non-monetary, idempotent RegistrarRespuestaTransaccion, not a physical simulator file.

### DEC-ROR-POLICY-001 / INV-RETURN-CORRELATION-001

Return-of-return eligibility uses the original Return clearing house, configured cause, transaction type, and elapsed operational window; ACH Colombia and CENIT rules remain separate. An incoming Return must match exact original trace and amount before mutating the original transaction.

### INV-DIFFERENTIAL-IDEMPOTENCY-001 / INV-DEFINITIVE-RESPONSE-001

Differential responses must not move money or apply twice. Never resend a transaction after a successful or functionally definitive SOAP response.

### INV-CENIT-LOCAL-GATEWAY-001

CENIT local Gateway simulator is disabled by default and prohibited in Production. Local runtime E2E does not certify Banco de la República connectivity or institutional homologation.

## Known traps

- TRAP-DATE-UTC-001: Business dates use the configured operational timezone, not UTC date by default.
- TRAP-FUTURE-CYCLE-001: Cycle selection and simulator requests cannot target future operational dates.
- TRAP-ADDENDA99-001: ACH Colombia V35 Return Addenda 99 has three-character physical cause at positions 4–6 and original trace at 7–21. DEV14 cannot occupy that field.
- TRAP-NORMATIVE-VERSION-001: Local RAG may contain different effective versions; check applicability and supersession, not search rank. ACH Colombia V35 April 2026 is current; V36 needs a later explicit decision.
- STALE TEST FIXTURE is a known source of historical CI failures, including pre-V35 Addenda 99 offset/length fixtures and old DEV14 ReturnOut expectations. Those were corrected; do not treat those old failures as current functional gaps.

## Evidence pointers

- OFFICIAL_PROFILE_VERSION_WORKFLOW: 295 focused tests, 2707 passed/0 failed/15 skipped broad backend, Release build, and fresh SQL Server/PostgreSQL migration/seed/restart and HTTP selection proof.
- DBCONTEXT_IMMUTABILITY_HARDENING: 37 focused guard tests, 200 affected fixture tests, 2744 passed/0 failed/15 skipped broad backend, Release build, and real SQL Server/PostgreSQL persistence, LIVE mutation isolation, restart/reseed proof.
- INBOUND_POSTSELECTION_NON_PSE: 2749 passed/0 failed/15 skipped broad backend retry, Release build, real SQL Server/PostgreSQL migration/seed, HTTP ingestion, PUBLISH checks, restart/reseed, and LIVE mutation isolation. No schema, migration, snapshot-format, or historical PUBLISH rewrite.
- OPS-GAP-005: real independent-context concurrency 96/96 and clean two-API runtime 100/100 distinct persisted traces per provider; GitHub Actions 32799729074.
- OPS-GAP-002.2D: retry permission 3/3, managed MFT 36/36, fresh-provider monitor 2/2; GitHub Actions 34182482659.
- CENIT-RUNTIME-E2E-001: Docker SQL Server/API/SPA and folder-backed Gateway lifecycle with Playwright, idempotency, and terminal-state protection; external homologation not claimed.
- Latest accepted product commit and remote CI status are recorded above. Do not transfer these certifications to a later changed product commit.

## PROJECT_REPOSITORY_CLEANUP

STATUS: CLOSED for the current-tree maintenance scope. Generated tracked CI/Playwright output under `_ci_artifacts/` and `pt-results/` was removed, narrow root ignore rules were added, and permanent agent instructions/current project memory were consolidated. Product code, tests, workflows, schema, and migrations were unchanged; the Release solution build passed. Historical Git blobs remain reachable and are a separate optional hygiene concern. No global product recertification or external UAT follows from this cleanup.

## Context routing

Use Project Memory for prior verified state, Codebase Memory for source structure, Local RAG for normative rules, current Git/source/tests for current implementation, and direct runtime evidence for observed behavior. Preserve contradictions instead of forcing current evidence to match memory. Keep future memory updates compact and English.
