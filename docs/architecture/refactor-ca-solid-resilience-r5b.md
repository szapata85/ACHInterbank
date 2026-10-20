# REFACTOR-CA-SOLID-RES-001 — R5B Inbound Validation

Date: 2026-10-10  
Mode: `LOCAL_ADVANCEMENT`  
State: `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`

## Exact plan scope

The implementation plan defines R5B as **inbound validation** and explicitly separates:

- physical structure;
- constraints from the selected immutable PUBLISH snapshot;
- regulatory/semantic rules;
- correlation when applicable.

R5A already owns physical byte/framing validation and fixed-width tokenization in `NachaPhysicalParser`, so R5B does not reopen that boundary. R5C persistence and R5D post-processing are intentionally not executed here.

## Implementation

R5B extracts the validation responsibility that remained embedded in `NachaParserService` into explicit internal components:

- `NachaInboundValidator`: DB-free generic/inter-record validation, selected PUBLISH snapshot constraints, addenda requirements, batch/file control consistency, regulatory error semantics and shared validation helpers;
- `NachaInboundCenitValidationRules`: DB-free CENIT-only batch/service, transaction, addenda association/sequence and CTX cardinality rules;
- `NachaInboundAchColombiaValidationRules`: DB-free ACH Colombia compatibility validation kept separate from CENIT; currently owns the pre-existing PSE/CCD batch classification used by the sequence policy;
- `NachaInboundDatabaseValidator`: explicit read-only EF boundary for validation that requires persisted facts. Its queries use `AsNoTracking()` and it has no write/persistence side effects.

`NachaParserService` remains the orchestration owner. It resolves the selected profile/PUBLISH snapshot, invokes R5A physical parsing, interprets T1/T5/T6/T7/T8/T9 records, delegates R5B validation, preserves duplicate/reprocess checks, maps/persists the validated graph and executes the existing Return/post-processing flow.

No new public interface or DI registration is introduced. The existing internal `ValidateCenitCtxAddendaCardinality` method on `NachaParserService` remains only as a compatibility delegate for existing tests/callers; rule ownership is `NachaInboundCenitValidationRules`.

## Validation classification and ownership before -> after

| Validation concern | Before R5B | After R5B |
|---|---|---|
| Physical byte/framing validation | `NachaPhysicalParser` (R5A) | unchanged `NachaPhysicalParser` |
| T1/T5/T6/T7/T8/T9 interpretation | `NachaParserService` | unchanged `NachaParserService` |
| selected PUBLISH identity/service/T6/T7/cardinality constraints | `NachaParserService` | `NachaInboundValidator` |
| inter-record sequence/batch/file-control consistency | `NachaParserService` | `NachaInboundValidator` |
| CENIT profile/service/T6/T7/CTX cardinality | `NachaParserService` | `NachaInboundCenitValidationRules` |
| ACH Colombia PSE/CCD compatibility validation | `NachaParserService` | `NachaInboundAchColombiaValidationRules` |
| validation requiring persisted sequence/account/institution/customer facts | `NachaParserService` | `NachaInboundDatabaseValidator` (read-only queries) |
| duplicate file / authorized replay ownership | `NachaParserService` | unchanged; not moved by R5B |
| persistence / `SaveChangesAsync` / transaction ordering | `NachaParserService` + `AchDbContext` | unchanged; reserved for R5C |
| Return state transitions / operational correlation / downstream actions | existing parser/post-processing owners | unchanged; reserved for R5D/later authorized work |
| hash/audit | existing ingestion/generation owners | unchanged |
| MFT/source-file/reprocess | existing MFT/ingestion owners | unchanged |

The R5B components return/throw the same validation results at the same orchestration points. They do not persist results, own files, perform HTTP, dispatch notifications or execute downstream operational actions.

## Error semantics

The extraction preserves the existing distinction and observable ordering between regulatory and technical failures:

- regulatory Dxx failures continue to use the active rejection catalog and the `[Regulatory]` marker;
- technical/structural failures keep their existing codes/messages and `[Technical]` marker where previously used;
- fail-fast versus accumulated `NachaValidationFailure` behavior is retained at the same call sites;
- a file-level structural/regulatory failure is not converted into a transaction rejection, and transaction validation failures remain accumulated where they were accumulated before R5B.

No expected result, fixture or golden file is changed to accommodate the refactor.

## ACH Colombia / CENIT separation

R5B keeps chamber-specific validation explicit rather than introducing a rail mega-validator. CENIT-only rules live in `NachaInboundCenitValidationRules`; ACH Colombia compatibility logic lives in `NachaInboundAchColombiaValidationRules`; shared/PUBLISH semantics remain in the generic validator.

The two productive `Contains("CENIT")` occurrences in `NachaConfigValidationService` are within R5B configuration/chamber-validation scope and are removed. `ResolveChamberCode` now prefers exact canonical `ClearingHouse.Code` equality (`CENIT` / `ACH`). A legacy compatibility path remains only as exact trimmed display-name equality for rows that predate canonical Code population; it is not a substring match and does not use a database ID.

The other eight tracked substring occurrences are outside R5B validation ownership and are intentionally unchanged:

- `NachaInboundSimulationService`: `7 -> 7` (simulation/operational behavior; not validation extraction);
- `IncomingNachaIngestionAppService`: `1 -> 1` (ingestion normalization/orchestration; not moved in R5B).

Tracked productive inbound/config substring debt therefore changes `10 -> 8`, entirely from `NachaConfigValidationService 2 -> 0`. No allowlist is added and no later subphase is inferred merely from the remaining debt.

### Classification of the ten R4E-reported productive substring sites

| Component / site | Existing behavior | R5B classification | R5B action |
|---|---|---|---|
| `NachaConfigValidationService.ResolveChamberCode` — canonical code check | classified config validation rail from `ClearingHouse.Code` by partial match | expressly R5B chamber/profile validation | replaced with exact canonical code equality |
| `NachaConfigValidationService.ResolveChamberCode` — display-name fallback | inferred CENIT from display-name substring when code did not resolve | expressly R5B compatibility validation | preserved compatibility only as exact trimmed `CENIT` equality |
| `NachaInboundSimulationService` ~377 | blocks CENIT R60–R74 from ordinary Return simulation | simulation/operational path, outside R5B | unchanged |
| `NachaInboundSimulationService` ~998 | blocks CENIT CTX Return simulation | simulation/operational path, outside R5B | unchanged |
| `NachaInboundSimulationService` ~1090 | chooses ReturnOut build request chamber code | simulation/generation orchestration, outside R5B | unchanged |
| `NachaInboundSimulationService` ~1092 | chooses ReturnOut normative-version metadata | simulation/generation orchestration, outside R5B | unchanged |
| `NachaInboundSimulationService` ~1365 | chooses simulated output filename suffix | simulator source-file naming, outside R5B | unchanged |
| `NachaInboundSimulationService` ~1385 | normalizes chamber code before differential-profile resolution | simulator profile-resolution orchestration, outside R5B | unchanged |
| `NachaInboundSimulationService` ~1402 | chooses simulator output directory | simulator filesystem routing, outside R5B | unchanged |
| `IncomingNachaIngestionAppService` ~903 | normalizes chamber code before ingestion profile resolution | ingestion orchestration, outside R5B | unchanged |

The remaining eight sites are reported as debt only; R5B does not assign them to R5C, R5D or another later phase without explicit plan authority.

## Return / ROR

R5B does not reinterpret Return or Return-of-Return. Existing Type 99 cause, original trace, addenda association and required-field semantics are preserved by the extracted validation rules. Functional Return correlation/state mutation remains in its existing post-processing owner and is not moved into the DB validator.

The CENIT ROR fixture is unchanged. Focused R5B characterization preserves the existing Type 99 reason/original-trace acceptance semantics without changing fixture bytes or expected output.

## Idempotency / persistence / audit / hash

Impact on idempotency/deduplication is `NONE`. File duplicate detection, persisted hashes, reprocess markers/claims and state transitions keep their existing owners and ordering.

Impact on persistence is `NONE`. R5B introduces no repository, unit-of-work, entity/mapping or transaction change and no migration. The DB-backed validation component performs read-only queries only; `SaveChangesAsync` remains in `NachaParserService` for R5C to address later if authorized.

Impact on audit/hash is `NONE`; their existing owners and ordering are unchanged.

## MFT / source-file ownership

Impact on MFT/source-file handling is `NONE`. R5B validation components do not receive, rename, acknowledge, move, archive, delete or reprocess source files and do not own SFTP/managed-file transport behavior.

## Focused characterization added

`NachaInboundValidationSeparationTests` adds source-level characterization for the extracted validation boundary:

- duplicate in-file sequence keeps the regulatory D04 failure semantics;
- CENIT Type 7 owner association mismatch keeps the existing failure token;
- CENIT PPD/CCD versus CTX profile/service separation is preserved;
- Return Type 99 reason/original-trace requirements remain accepted when valid.

These tests are not reported as executed on this host because `dotnet` is unavailable.

## R5B architecture ratchet

`scripts/validate-r5b-inbound-validation.py` enforces:

- `NachaParserService` lines: `2180 -> 1077`;
- validation method definitions owned by `NachaParserService`: `20 -> 1`, where the single remaining method is a thin compatibility delegate;
- physical parser components: `1 -> 1`, with the R5A source SHA-256 unchanged;
- extracted validation components: `0 -> 4` (generic/PUBLISH, CENIT, ACH Colombia, explicit read-only DB boundary);
- DB-free validation components: `0 -> 3`;
- persistence side effects inside validators: `0`;
- managed-file/HTTP/filesystem/service-provider dependencies inside validators: `0`;
- `NachaConfigValidationService Contains("CENIT")`: `2 -> 0`;
- `NachaInboundSimulationService Contains("CENIT")`: `7 -> 7` (outside R5B);
- `IncomingNachaIngestionAppService Contains("CENIT")`: `1 -> 1` (outside R5B);
- total tracked productive inbound/config substring debt: `10 -> 8`;
- inbound DB-ID rail inference: `0 -> 0`;
- `PaymentRailOperationalStrategy`: `0 -> 0`;
- new R5B architecture debt: `0`.

The R2 Persistence fallback ratchet remains `57/57`; no allowlist is expanded.

## Validation evidence on this host

### PASS executed

The following host-independent validators execute with exit 0 on the R5B workspace:

- `python scripts/validate-solution-projects.py`;
- `python scripts/validate-r2-di-configuration.py`;
- `python scripts/validate-r3-external-resilience.py`;
- `python scripts/validate-r4a-nacha-builder-facade.py`;
- `python scripts/validate-r4b-nacha-record-renderers.py`;
- `python scripts/validate-r4c-nacha-assembly-pipeline.py`;
- `python scripts/validate-r4d-chamber-flow-policies.py`;
- `python scripts/validate-r4e-chamber-resolution.py`;
- `python scripts/validate-r5a-inbound-architecture.py`;
- `python scripts/validate-r5b-inbound-validation.py`.

### FAIL executed

- `python scripts/scan_result_solid.py` exits 1 for the pre-existing broad program backlog. The captured R5A baseline and R5B output are byte-identical with SHA-256 `b45d83724c3b6bd0a13014de7bf890f8b281e8ad24d9f97fdd40dde799f93b12`; backlog neither grows nor shrinks.
- During implementation, the first R5B architecture-ratchet run also exited 1 because its forbidden-token check matched the literal text `MFT` in an XML documentation comment. The comment was reworded without changing runtime code; the ratchet was rerun and passes. This was a validator false positive, not product behavior failure.

### PASS_STATIC_ONLY

Static ownership/diff checks confirm R5A physical parsing remains byte-identical, R5C/R5D responsibilities remain in their original owners, no write-side effect exists in the validators, no DB-ID rail discriminator is introduced, and protected normative/golden/schema/UI/MFT/outbound artifacts remain unchanged from the R5A input package.

### NOT_RUN_ENVIRONMENT

The R5A source archive has no `.git`, and this host does not provide `dotnet` or Docker. Therefore Release build, focused R5B tests, R5A physical parser tests, executable inbound characterization, golden/fingerprint/CENIT ROR suites and the broad backend/runtime suites are not executed here and must not be reported as PASS.

## Behavioral / normative / schema impact

- `BEHAVIORAL_DIFF=NONE`.
- `NORMATIVE_DIFF=NONE`.
- `DB_MIGRATIONS=NONE`.

R5B remains `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`. Under `LOCAL_ADVANCEMENT`, the structurally implemented increment permits `NEXT_ALLOWED_PHASE=R5C`; R5C/R5D/R6 and later phases are not executed in this package.
