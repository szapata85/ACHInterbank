# ACHInterbank Project Memory

## Durable Architecture and Integration Contracts

DEC-NACHA-PROFILES-001:
NACHA-M record layouts, field definitions, rendering/parsing metadata, filenames,
transaction/cause catalogs and clearing-house format rules are
profile/configuration/catalog-driven. Do not introduce legacy hardcoded layouts.
Domain invariants may remain explicit application/domain logic.

DEC-CLASSIFICATION-001:
Transaction direction and monetary route are determined from whether the source or
destination is the default institution (CFA): CFA-source debit uses
`ProcContrapartidas`; external-source credit uses `ProcTransacciones`.
Prenotifications have no monetary route. Ambiguous or unsupported combinations
require manual review and must not be dispatched.

DEC-INBOUND-SIMULATOR-001:
The inbound simulator emulates a non-default external counterparty responding to
CFA, resolved through `IsDefaultSource`, never a hardcoded institution ID. CFA
cannot be that counterparty. Generation is file-only: no auto-import, external
transmission or transaction-state change; upload remains an explicit user action.

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

TRAP-ADDENDA99-001:
Return Addenda type 99 uses a 3-character return cause at positions 4-6 and the
15-character original transaction sequence at positions 7-21. Never parse a
5-character cause or shift the original-sequence offset.

TRAP-NORMATIVE-VERSION-001:
Local RAG contains normative artifacts from different effective dates. Resolve the
latest applicable document and supersession before using a rule; do not trust rank alone.

## Current Evidence State

RC-CURRENT:
COMMIT: UNSET
STATUS: NOT_CERTIFIED
CI: NOT_AUDITED
RUNTIME_E2E: NOT_AUDITED
GAUNTLET: NOT_RUN
UAT: NOT_READY

EVID-RC-001:
No fresh CI or runtime-backed E2E evidence is attached to RC-CURRENT. Do not infer
CLOSED, VERIFIED, UAT_READY or RELEASE_READY from implementation or historical evidence.

OPEN-GAPS-CURRENT:
STATUS: NOT_AUDITED
Do not infer that no functional gaps exist. Refresh open gaps from current
implementation, tests, normative evidence and Git before release/UAT planning.
