# REFACTOR-CA-SOLID-RES-001 — R5C Inbound Persistence

Date: 2026-10-10  
Mode: `LOCAL_ADVANCEMENT`  
State: `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`

## Exact plan scope

The implementation plan defines R5C as **inbound persistence**: persist the already parsed and validated inbound result through an explicit persistence component while preserving transaction behavior, lineage and exact-file membership. The R5 phase prompt is explicit that R5C must not advance into R5D post-processing.

R5A physical framing/tokenization remains owned by `NachaPhysicalParser`. R5B validation remains owned by the four validation components introduced in R5B. R5C changes only the ownership of the EF write that stores the validated NACHA graph.

## Implementation

R5C adds internal `NachaInboundPersistenceCoordinator` with a single responsibility:

1. receive the already parsed/validated `ParsedNachaHeader` graph;
2. map it through the existing `ParsedNachaEntityMapper`;
3. attach the complete graph through `AchDbContext.NachaHeaders.AddRange`;
4. execute the same single `SaveChangesAsync` that `NachaParserService` executed before R5C.

`NachaParserService` now delegates only that write via `PersistAsync(headers, ct)`. No public contract or DI registration changes. The mapper remains a pure mapper and the coordinator does not parse, validate, route, dispatch, access files, perform MFT, call HTTP/SOAP or execute post-processing.

## Persistence ownership before -> after

| Concern | Before R5C | After R5C |
|---|---|---|
| physical parsing | `NachaPhysicalParser` | unchanged |
| selected PUBLISH + structural/semantic validation | R5B validators | unchanged |
| parsed -> EF entity graph mapping | `ParsedNachaEntityMapper` | unchanged |
| `NachaHeaders.AddRange` for parsed graph | `NachaParserService` | `NachaInboundPersistenceCoordinator` |
| parsed-graph `SaveChangesAsync` | `NachaParserService` | `NachaInboundPersistenceCoordinator` |
| relational ingestion transaction begin/commit/rollback | `IncomingNachaIngestionAppService` | unchanged |
| direct-parser implicit EF transaction for one `SaveChangesAsync` | EF Core | unchanged |
| source-file fingerprint/reprocess admission | `IncomingNachaIngestionAppService` | unchanged |
| NACHA identity duplicate/replay guard | `NachaParserService` | unchanged |
| Return/ROR state transitions | existing parser/state-transition owner | unchanged; outside R5C |
| operational correlation/classification/dispatch | existing post-parse processor | unchanged; reserved for R5D |
| source-file/MFT lifecycle | existing ingestion/MFT owners | unchanged |

There is exactly one owner of the parsed-graph write after R5C.

## Transaction boundary preservation

R5C intentionally **does not** call `BeginTransactionAsync`, `CommitAsync` or `RollbackAsync` in the new persistence coordinator.

The existing relational ingestion transaction still begins in `IncomingNachaIngestionAppService` before `ParseAndSaveDetailedAsync`, encloses the parser write plus the existing post-parse work/final ingestion-state save, and commits/rolls back in the same owner and order as before R5C. `NachaInboundPersistenceCoordinator.SaveChangesAsync` participates in that ambient transaction.

For direct parser callers that do not have an explicit outer transaction, the same single EF `SaveChangesAsync` remains the write boundary; R5C does not split it into multiple commits or add a new explicit transaction.

The `ChangeTracker.AutoDetectChangesEnabled` lifecycle remains in `NachaParserService` unchanged because moving its scope would alter the execution context seen by the existing post-save Return transition path and would exceed the narrow R5C write extraction.

## Exact-file membership and lineage

`ParsedNachaEntityMapper` remains byte-identical and continues to preserve:

- `ParsedNachaHeader.IncomingNachaFileIngestionId -> NachaHeader.IncomingNachaFileIngestionId`;
- one `NachaID` across header, batch, entry, addenda, batch-control and file-control rows;
- entry -> batch association via batch number;
- addenda -> entry association via the existing seven-digit sequence suffix;
- parent navigation relationships used by EF to persist the graph in the same save operation.

Focused R5C characterization covers exact-file membership, parent/child lineage, Type 99 Return/ROR fields and ambient-transaction rollback participation.

## Idempotency / duplicate / recovery

R5C does not move or reinterpret idempotency policy.

The two existing policy owners remain:

- `IncomingNachaIngestionAppService`: source-file SHA-256/size fingerprint, canonical ingestion selection, duplicate handling and reprocess admission;
- `NachaParserService`: NACHA header identity duplicate check, authorized reprocess check and replay `NachaID` derivation.

The persistence coordinator does not calculate hashes, detect duplicates, authorize replay, mutate reprocess markers or introduce retry around the non-idempotent unit. Existing failure behavior is preserved: a validation exception occurs before `PersistAsync`; a write failure propagates; the existing outer relational transaction is rolled back by the ingestion owner; terminal ingestion audit state is persisted afterward by the existing failure path.

## Return / ROR

Return and Return-of-Return persistence semantics are unchanged. Type 99 fields are still mapped by `ParsedNachaEntityMapper` and written in the same graph/save operation. Cause, original trace, new trace, association and CENIT ROR semantics are unchanged.

The existing `ApplyReturnStateTransitionsAsync` call remains **after** parsed-graph persistence and is not moved into the persistence coordinator. Its correlation, regulatory policy checks and state mutations are outside R5C and remain for R5D/later authorized work.

## Audit / hash

Impact on hash authority is `NONE`:

- source-file SHA-256 remains owned by ingestion;
- parser `NachaID` and authorized-replay derivation remain at their existing points;
- the persistence coordinator does not recalculate hashes or change the representation being hashed.

Impact on audit/tracing is `NONE`. Ingestion processing results/events and transaction state-transition audit keep their existing owners and ordering. R5C introduces no new audit row or correlation identifier.

## ACH Colombia / CENIT and remaining heuristics

R5C persistence is common infrastructure for the already validated common entity model and does not merge regulatory rules. ACH Colombia/CENIT validation remains separated in R5B components.

The eight productive `Contains("CENIT")` occurrences left by R5B are outside the persistence write extraction and remain unchanged:

- `NachaConfigValidationService`: `0 -> 0`;
- `NachaInboundSimulationService`: `7 -> 7`;
- `IncomingNachaIngestionAppService`: `1 -> 1`;
- total tracked productive inbound/config substring debt: `8 -> 8`.

The simulation sites control simulation/ReturnOut generation/naming/profile/output routing. The ingestion site normalizes chamber code for profile-resolution orchestration. R5C does not assign those sites to R5D or any other later phase merely because they remain debt.

DB-ID rail inference remains `0`; `PaymentRailOperationalStrategy` remains `0`.

## Focused characterization added

`NachaInboundPersistenceSeparationTests` adds executable characterization for hosts with .NET available:

- the persistence coordinator writes the full parsed graph while preserving exact ingestion membership and parent/child lineage;
- Type 99 Return/ROR fields survive persistence unchanged;
- the coordinator participates in an already-open transaction and does not commit it: rolling back the caller-owned transaction removes the graph.

The tests are added without changing expected production values, TestData, goldens or fixtures. They are not reported as executed on this host because `dotnet` is unavailable.

## R5C architecture ratchet

`scripts/validate-r5c-inbound-persistence.py` enforces:

- `NachaParserService` lines: `1077 -> 1076`;
- physical parser components: `1 -> 1`, R5A source hash unchanged;
- validation components: `4 -> 4`, R5B validation source hash unchanged;
- write-owning inbound persistence components: `0 -> 1`;
- direct parsed-graph persistence definitions in `NachaParserService`: `2 -> 0`;
- parsed-graph `SaveChangesAsync` ownership: parser `1 -> 0`, persistence coordinator `0 -> 1`, total `1 -> 1`;
- transaction boundary owner: `IncomingNachaIngestionAppService 1 -> 1`; coordinator transaction ownership `0`;
- duplicate/idempotency policy owners: `2 -> 2`; coordinator policy ownership `0`;
- write side effects in R5A physical parser: `0`;
- write side effects in R5B validators: `0`;
- `Contains("CENIT")`: config `0 -> 0`, simulation `7 -> 7`, ingestion `1 -> 1`, total `8 -> 8`;
- inbound DB-ID rail inference: `0 -> 0`;
- `PaymentRailOperationalStrategy`: `0 -> 0`;
- new R5C architecture debt: `0`.

The R2 Persistence fallback ratchet remains `57/57`; no allowlist is expanded.

## Validation evidence on this host

### PASS executed

The following host-independent commands execute with exit 0 on the R5C workspace:

- `python scripts/validate-solution-projects.py`;
- `python scripts/validate-r2-di-configuration.py`;
- `python scripts/validate-r3-external-resilience.py`;
- `python scripts/validate-r4a-nacha-builder-facade.py`;
- `python scripts/validate-r4b-nacha-record-renderers.py`;
- `python scripts/validate-r4c-nacha-assembly-pipeline.py`;
- `python scripts/validate-r4d-chamber-flow-policies.py`;
- `python scripts/validate-r4e-chamber-resolution.py`;
- `python scripts/validate-r5a-inbound-architecture.py`;
- `python scripts/validate-r5b-inbound-validation.py`;
- `python scripts/validate-r5c-inbound-persistence.py`;
- Python compilation of the R5A/R5B/R5C ratchet scripts.

### FAIL executed

`python scripts/scan_result_solid.py` exits 1 for the pre-existing program backlog and is not reported as PASS. Its R5B baseline and R5C output are byte-identical, both SHA-256 `b45d83724c3b6bd0a13014de7bf890f8b281e8ad24d9f97fdd40dde799f93b12`; backlog is unchanged.

### PASS_STATIC_ONLY

R5B -> R5C protected comparison confirms byte identity for all 15 `tests/**/TestData` files, all six NACHA goldens, fingerprint characterization, the CENIT ROR fixture, 12 normative files, 92 Persistence/PostgreSQL migration files, all 79 SQL Server migration-project files, `docs/spa`, all 37 MFT/SFTP-matching paths, the R5A physical parser and the R5B validation/config-validation boundaries. The complete workspace diff contains only the eight authorized R5C paths (four modified, four added), with no removed path.

### NOT_RUN_ENVIRONMENT

Release build, focused R5C/R5B/R5A tests, persistence/idempotency/reprocess tests, executable inbound characterization, golden/fingerprint/CENIT ROR and broad backend/runtime suites are not run because `dotnet` is unavailable. Docker E2E is also not run. Git SHA/status cannot be established because the source archive contains no `.git`.

## Environment and state

The source archive has no `.git`, and this host does not provide `dotnet` or Docker. Therefore Release build, focused R5C/R5B/R5A tests, persistence/idempotency/reprocess tests, golden/fingerprint/CENIT ROR suites and broad backend/runtime suites cannot be executed here and must not be reported as PASS.

`BEHAVIORAL_DIFF=NONE`  
`NORMATIVE_DIFF=NONE`  
`DB_MIGRATIONS=NONE`

R5C remains `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`. Under `LOCAL_ADVANCEMENT`, the structurally implemented increment permits `NEXT_ALLOWED_PHASE=R5D`; R5D/R6 and later phases are not executed in this package.
