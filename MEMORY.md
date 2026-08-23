# ACHInterbank Project Memory

## 00. MEMORY CONTRACT

Purpose:
Keep only durable, verified project knowledge that prevents future agents from
rediscovering decisions, closed defects, invariants, known traps, release evidence,
and current delivery state.

This file is NOT a source of truth for:
- source code;
- ACH Colombia regulations;
- CENIT regulations;
- runtime behavior;
- CI execution results not linked to evidence.

Authoritative sources:
1. Runtime/tests/commit for implemented behavior.
2. Codebase Memory MCP for code structure and implementation.
3. Local DB RAG for ACH Colombia and CENIT normative requirements.
4. Project Memory MCP for prior verified decisions, state and evidence.

Rules:
- Store verified facts only.
- Never infer OPEN/CLOSED status.
- Every CLOSED item must have evidence.
- Replace superseded facts instead of appending duplicates.
- Do not store source code.
- Do not copy regulatory documents into this file.
- Store only normative references or identifiers.
- Do not store transient logs.
- Do not store secrets, credentials, private keys, certificates, tokens,
  connection strings, production account data or PII.
- Keep this file compact.
- Prefer stable IDs for searchable knowledge.

Retrieval protocol:
1. Call get_project_memory(head_only=true).
2. Search only the keywords relevant to the current task.
3. Retrieve only the line ranges needed.
4. Never load the complete MEMORY.md unless it is demonstrably small.

Write protocol:
Write only when one of these changes:
- durable architecture decision;
- business invariant;
- confirmed gap;
- closed gap;
- known trap/root cause;
- verified release evidence;
- release candidate;
- next delivery objective.

Do not write ordinary investigation progress.

---

## 01. PROJECT IDENTITY

PROJECT: ACHInterbank
DOMAIN: Colombian interbank ACH processing
PHASE: Delivery hardening / UAT / release preparation

Primary clearing houses:
- ACHCOL
- CENIT

Backend:
- .NET 10
- Clean Architecture

Frontend:
- Angular

Runtime:
- Docker Compose

Persistence:
- SQL Server
- PostgreSQL variants

Primary interchange:
- NACHA-M

---

## 02. CONTEXT ROUTING

CTX-PROJECT-MEMORY:
Use Project Memory MCP for previous decisions, status, gaps, closures,
known traps and release evidence.

CTX-CODEBASE:
Use Codebase Memory MCP before broad filesystem searches for architecture,
symbols, dependencies, call paths and implementation locations.

CTX-NORMATIVE:
Use Local DB RAG for ACH Colombia and CENIT regulatory and technical rules.
Do not reproduce regulatory text in Project Memory.

CTX-RTK:
Use RTK to reduce noisy command output and execution context.

CTX-FILESYSTEM:
Read complete files only after targeted retrieval tools are insufficient.

---

## 03. STABLE ARCHITECTURAL DECISIONS

DEC-NACHA-PROFILES-001:
Official NACHA-M generation uses configuration/profile based Option C.
Do not introduce legacy hardcoded layouts.

DEC-DEFAULT-SOURCE-001:
CFA is the default/source financial institution in the application domain.

DEC-INBOUND-SIMULATOR-001:
Inbound simulation represents an external institution sending transactions to CFA.

DEC-INBOUND-UPLOAD-001:
The inbound simulator generates a file for manual user upload.
It must not auto-import the generated file.

DEC-CYCLE-001:
Every transaction must be associated with an ACH operational cycle.

DEC-DATE-001:
Operational calendar dates must remain calendar dates.
Do not reinterpret them through UTC conversion.

DEC-DIFFERENTIAL-001:
Differential Response is handled through the configured SOAP integration flow.
It is not a physical differential NACHA file generation requirement.

---

## 04. BUSINESS INVARIANTS

INV-IDEMPOTENCY-001:
Replay of an already processed business operation must not create duplicate
financial effects.

INV-RETURN-ORIGINAL-001:
A return must be correlated with the correct original transaction.

INV-RETURN-AMOUNT-001:
Return correlation must not accept an original transaction whose amount differs
from the returned EntryDetail amount.

INV-CYCLE-DATE-001:
Transaction, operational date, processing date, cycle and generated file must
remain mutually coherent.

INV-NACHA-STRUCTURE-001:
NACHA-M generation and parsing must be driven by the applicable clearing-house
profile and normative rules, not by ad-hoc hardcoding.

---

## 05. VERIFIED CLOSURES

CLOSE-DIFF-RESP-001:
STATUS: CLOSED
Capability:
End-to-end Differential Response through WSAxonRespuestaTransacciones /
RegistrarRespuestaTransaccion.
Verified behavior includes unique correlation, persistence, SOAP invocation,
auditability and replay protection.

CLOSE-CI-STAB-002:
STATUS: CLOSED
Scope:
Angular/backend stabilization related to prenotification destination-account
preservation and ROR operational/effective date selection.

CLOSE-CENIT-RET-TRANSPORT-001:
STATUS: CLOSED
Scope:
CENIT Return Out transport runtime-backed E2E.
Key correction:
Use clearing-house local operational date and calendar-date semantics.
Replay remains duplicate/idempotent.

CLOSE-RET-AMOUNT-GAP:
STATUS: CLOSED
Root cause:
Original trace correlation did not compare transaction amounts.
Correction:
Amount mismatch prevents linkage and records OriginalTraceRefAmountMismatch.

CLOSE-RET-ADDENDA99-GAP:
STATUS: CLOSED
Root cause:
Return code and original trace positions in Addenda 99 were parsed incorrectly.
Correction:
Return reason is exactly three characters and original trace starts immediately
after the reason field.

---

## 06. KNOWN TRAPS

TRAP-DATE-UTC-001:
Never use JavaScript Date conversion to reinterpret clearing-house operational
calendar dates.

TRAP-FUTURE-CYCLE-001:
A preview-selected future cycle must not silently generate D+1 processing while
the local operational day D remains active.

TRAP-RETURN-AMOUNT-001:
Exact original trace alone is insufficient correlation evidence if amount differs.

TRAP-ADDENDA99-001:
Do not parse a five-character return reason from Addenda 99.
The return reason is three characters.

TRAP-SELF-VALIDATION-001:
A successful implementation agent result is not sufficient release evidence.
Independent deterministic or adversarial verification is required.

---

## 07. NORMATIVE POINTERS

NORM-ACHCOL:
Authoritative details must be retrieved from Local DB RAG.
Known corpus includes ACH Colombia service/manual material.
Do not cache complete rules here.

NORM-CENIT:
Authoritative details must be retrieved from Local DB RAG.
Known corpus includes DSP-152, operational manuals, return/rejection causes
and NACHA-M specifications.
Do not cache complete rules here.

---

## 08. RELEASE CANDIDATE

RC-CURRENT:
COMMIT: UNSET
STATUS: NOT_CERTIFIED
CI: UNSET
RUNTIME_E2E: UNSET
GAUNTLET: NOT_RUN
UAT: NOT_READY

Rule:
A release certification applies only to the exact commit recorded here.
Any subsequent code change invalidates the previous release-candidate certification.

---

## 09. OPEN GAPS

GAP-REFRESH-REQUIRED:
Do not infer the current open-gap list from old memory.
Before release work, reconcile this section against the latest verified project status.

---

## 10. DELIVERY QUALITY MODEL

Delivery states:
IMPLEMENTED
VERIFIED
GAUNTLET_PASSED
UAT_READY
USER_ACCEPTED

A development JOB being CLOSED does not by itself imply UAT_READY.

---

## 11. CURRENT OBJECTIVE

NEXT-DELIVERY-001:
Convert the already implemented ACHInterbank capabilities into evidence-backed,
user-deliverable UAT candidates.

Priority:
stability, operational correctness, normative compliance, runtime evidence and
operator usability over uncontrolled new feature growth.

---

## 12. SESSION HANDOFF

LAST_VERIFIED_COMMIT: UNSET
LAST_VERIFIED_AT: UNSET
CURRENT_WORK_ITEM: UNSET
NEXT_ACTION: Refresh release state and open gaps from current repository evidence.