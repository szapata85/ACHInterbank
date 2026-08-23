# ACHInterbank Project Memory

## Durable Architecture and Integration Contracts

DEC-NACHA-PROFILES-001:
NACHA-M generation and parsing are table/profile-driven. Clearing-house layouts,
fields, filenames and business rules must remain configuration/catalog/profile-driven;
do not introduce legacy hardcoded layouts.

DEC-CLASSIFICATION-001:
Transaction direction and monetary route are determined from whether the source or
destination is the default institution (CFA): CFA-source debit uses
`ProcContrapartidas`; external-source credit uses `ProcTransacciones`.
Prenotifications have no monetary route. Ambiguous or unsupported combinations
require manual review and must not be dispatched.

DEC-INBOUND-SIMULATOR-001:
The inbound simulator represents an external origin sending to a distinct
destination. It only generates and records a NACHA-M file; it neither auto-imports,
transmits externally, nor changes transaction states. A separate explicit upload is
required.

DEC-DIFFERENTIAL-001:
Differential prenotification responses are processed through the configured
`RegistrarRespuestaTransaccion` integration/mapping path, not by generating a
physical differential NACHA-M file. The operation is non-monetary: no balance or
monetary movement may be created.

DEC-ROR-POLICY-001:
Return-of-return eligibility is evaluated against the original return's clearing
house, configured cause-code policy, and regulatory catalog. Do not reuse a rule
from one clearing house for another.

## Durable Invariants and Traps

INV-RETURN-CORRELATION-001:
For an incoming return linked by exact original trace reference, the candidate
original transaction amount must equal the returned entry amount. A mismatch must
not create the link.

INV-DIFFERENTIAL-IDEMPOTENCY-001:
A differential prenotification response must correlate to the pending
prenotification, persist its trace, and be replay-safe. Clearing-house and identity
mismatches must fail without a state or monetary effect.

TRAP-DATE-UTC-001:
Operational calendar dates are date values. Resolve cycle windows in their configured
local time zone; do not reinterpret operational dates through UTC/JavaScript date
conversion.

TRAP-FUTURE-CYCLE-001:
A preview-selected future cycle must not silently generate D+1 processing while the
local operational day remains D.

## Current Evidence State

RC-CURRENT:
STATUS: NOT_CERTIFIED
CI: NOT_AUDITED
RUNTIME_E2E: NOT_AUDITED
GAUNTLET: NOT_RUN
UAT: NOT_READY

GAP-EVIDENCE-001:
The repository contains CI workflows and focused automated coverage for return,
differential-response, inbound-simulator, and CENIT flows, but this audit found no
current executable CI or runtime-E2E result tied to a release candidate. Do not
claim CLOSED, VERIFIED, UAT_READY, or RELEASE_READY without fresh evidence.

NEXT-BOOTSTRAP-001:
Before a release or UAT decision, record the exact candidate commit and attach
current CI plus runtime-backed evidence for the affected flow.