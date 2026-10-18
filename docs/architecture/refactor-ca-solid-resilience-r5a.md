# REFACTOR-CA-SOLID-RES-001 — R5A deterministic inbound physical parser

## State

- Phase: `R5A`
- State: `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`
- Execution mode: `LOCAL_ADVANCEMENT`
- Current Git SHA: `UNAVAILABLE_SOURCE_ARCHIVE_NO_GIT_METADATA`
- Prior phases: R0/R1/R2/R3/R4A/R4B/R4C/R4D/R4E remain `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`
- Next allowed phase: `R5B` (not executed in this increment)

R5A is structurally implemented on the delivered R4E tree. It is not `VERIFIED` or `CLOSED`: the source archive has no `.git` metadata and this host has no `dotnet` or Docker. The implementation plan assigns R5A only to the deterministic physical inbound parser boundary:

```text
File/Text Reader
  -> Record Tokenizer/Parser
  -> Parsed physical NACHA model
```

R5B validation, R5C persistence and R5D post-processing are explicitly out of scope.

## Exact R5A scope confirmed from the plan

R5A extracts byte/framing parsing and fixed-width record tokenization from `NachaParserService` into a deterministic component that can be tested without `AchDbContext`. The following are intentionally not moved in R5A:

- PUBLISH snapshot/profile selection;
- chamber/profile resolution;
- structural/semantic/regulatory validation beyond the existing physical framing checks;
- entry validation and Return/ROR interpretation;
- duplicate/reprocess decisions;
- persistence and transaction ordering;
- state transitions, correlation, dispatch planning or post-processing;
- MFT/source-file ownership.

This boundary avoids prematurely implementing R5B/R5C/R5D.

## Implementation

Added internal `NachaPhysicalParser` plus immutable physical token models:

- `NachaPhysicalFile` — selected physical record length and ordered tokens;
- `NachaPhysicalRecord` — zero-based source index, record type and exact raw fixed-width content.

The component owns only the physical behavior previously embedded in `NachaParserService`:

- stream-to-bytes read;
- empty-file detection;
- BOM rejection;
- CR/LF rejection;
- accepted byte repertoire check;
- selected record-width divisibility;
- exact record tokenization;
- first-record Type 1 check;
- Type 1 declared record-size check;
- Type 1 blocking-factor check.

The error codes/messages and check ordering were copied without changing semantics. The physical parser has no `AchDbContext`, EF Core, logger, configuration, HTTP, service provider, MFT/SFTP, persistence or filesystem ownership.

`NachaParserService.ParseAndSaveDetailedAsync` now delegates physical parsing to `NachaPhysicalParser`, then continues with the pre-existing interpretation, validation and persistence flow. The existing internal `ReadPhysicalRecordsAsync` path is retained as a compatibility delegate for current tests/callers; it no longer owns framing logic.

No public interface or DI registration was added. `INachaParserService` and its scoped implementation remain unchanged.

## Ownership before -> after

| Concern | R4E owner | R5A owner / authority |
|---|---|---|
| stream/byte read and framing | `NachaParserService` | `NachaPhysicalParser` |
| record-width/tokenization | `NachaParserService` | `NachaPhysicalParser` |
| ordered raw record type/content | implicit `List<string>` in parser service | `NachaPhysicalFile` / `NachaPhysicalRecord` |
| PUBLISH snapshot/profile resolution | `NachaParserService` + `NachaProfileRecordReader` | unchanged |
| field interpretation T1/T5/T6/T7/T8/T9 | `NachaParserService` | unchanged; later R5 work only if authorized |
| ACH Colombia/CENIT semantic validation | existing parser/layout/catalog services | unchanged |
| duplicate/reprocess decision | existing parser/ingestion persistence | unchanged |
| persistence transaction/order | `NachaParserService` / `AchDbContext` | unchanged |
| Return/ROR interpretation/correlation | existing parser/post-processing owners | unchanged |
| hash/audit | existing ingestion/generation owners | unchanged |
| MFT/source-file/recovery | existing MFT/ingestion owners | unchanged |

The physical parser intentionally returns exact raw records and does not infer rail, profile, Return/ROR semantics or business meaning.

## ACH Colombia / CENIT resolution

R5A does not alter chamber resolution. The selected profile/layout still reaches the physical parser through the already-resolved `NachaProfileRecordReader` instance when present. The pure component does not query the database and does not derive chamber identity from names or database IDs.

The ten productive `Contains("CENIT")` occurrences reported by R4E were inspected individually and are all outside R5A physical parsing scope:

| Component / occurrence | Current responsibility | R5A classification |
|---|---|---|
| `NachaConfigValidationService` x2 | configuration/chamber validation fallback | later/outside R5A; exact later subphase not inferred |
| `NachaInboundSimulationService` line ~377 | simulator Return/ROR scenario gate | later/outside R5A; exact later subphase not inferred |
| `NachaInboundSimulationService` line ~998 | simulator CENIT CTX Return gate | later/outside R5A; exact later subphase not inferred |
| `NachaInboundSimulationService` lines ~1090/~1092 | simulator ReturnOut build code/version selection | later/outside R5A; exact later subphase not inferred |
| `NachaInboundSimulationService` line ~1365 | simulator external filename selection | later/outside R5A; exact later subphase not inferred |
| `NachaInboundSimulationService` line ~1385 | simulator differential-profile code selection | later/outside R5A; exact later subphase not inferred |
| `NachaInboundSimulationService` line ~1402 | simulator output-directory routing | later/outside R5A; exact later subphase not inferred |
| `IncomingNachaIngestionAppService` line ~903 | ingestion config-code normalization before profile resolution | later/outside R5A; exact later subphase not inferred |

None is modified or hidden behind a new allowlist. Counts remain 2 + 7 + 1 = 10.

## Return / ROR

R5A does not change Return or Return-of-Return interpretation, causes, addenda mapping, association, correlation or state transitions. The physical parser only preserves the ordered raw Type 7 record exactly.

Focused characterization reads the existing CENIT ROR synthetic fixture into the pure physical parser after removing fixture line separators used only by the test representation, and asserts the existing T1/T5/T6/T7/T8/T9 order plus the Annex-1.7 ROR Type 7 fields. The fixture itself is byte-unchanged.

## Idempotency / duplicates / persistence / audit / hash

No idempotency or deduplication code moved. The existing duplicate-header/reprocess checks, `IncomingNachaFileIngestion` semantics and side-effect guards remain in the same service paths.

No persistence code moved. `AchDbContext`, `SaveChangesAsync`, entity mapping, Return state transitions and post-parse processing remain outside `NachaPhysicalParser` and execute in the same order as R4E.

No hashing or audit owner changed. No new hash, persistence or audit side effect exists in the physical parser.

## MFT / source-file ownership

R5A does not receive, rename, acknowledge, move, archive, delete or reprocess source files. It accepts a `Stream` only. Managed-folder/SFTP polling, source-file claims, recovery and reprocess semantics remain in their pre-existing owners.

## Characterization added

`NachaPhysicalParserTests` is DB-independent and covers:

- ACH Colombia physical happy path and exact fixed-width record order;
- invalid physical structure with the existing error codes for CR/LF, residual bytes, invalid first record and blocking factor;
- accepted Latin-1 `Ñ`/`ñ` repertoire behavior;
- CENIT Return-of-Return fixture physical order and Type 7 field preservation.

Duplicate/idempotency, unknown profile resolution and business validation are not moved into these tests because those concerns are outside R5A and retain their existing suites.

## R5A architecture ratchet

`scripts/validate-r5a-inbound-architecture.py` enforces:

- physical framing owner coupled to `AchDbContext`: 1 -> 0;
- DB-free deterministic physical parser components: 0 -> 1;
- direct physical framing implementation in `NachaParserService`: 1 -> 0;
- `NachaParserService` delegates to `NachaPhysicalParser`;
- physical parser forbidden DB/EF/config/logger/HTTP/service-provider/MFT/SFTP/persistence/filesystem tokens: 0;
- inbound `Contains("CENIT")` R4E debt remains exactly `2 / 7 / 1`, with R5A-scope count `0 -> 0` and total remaining `10`;
- inbound DB-ID rail inference: `0 -> 0`;
- new public interfaces: `0`;
- `PaymentRailOperationalStrategy`: `0`;
- focused DB-independent characterization source exists;
- new R5A architecture debt: `0`.

The R2 Persistence fallback ratchet remains `57/57`; no allowlist is expanded.

## Static validation in this environment

### PASS executed

- `python scripts/validate-solution-projects.py`;
- `python scripts/validate-r2-di-configuration.py`;
- `python scripts/validate-r3-external-resilience.py`;
- `python scripts/validate-r4a-nacha-builder-facade.py`;
- `python scripts/validate-r4b-nacha-record-renderers.py`;
- `python scripts/validate-r4c-nacha-assembly-pipeline.py`;
- `python scripts/validate-r4d-chamber-flow-policies.py`;
- `python scripts/validate-r4e-chamber-resolution.py`;
- `python scripts/validate-r5a-inbound-architecture.py`.

### FAIL executed

- `python scripts/scan_result_solid.py` exits 1 for the known broad program backlog. The locally captured R4E input baseline and R5A output are byte-identical with SHA-256 `b45d83724c3b6bd0a13014de7bf890f8b281e8ad24d9f97fdd40dde799f93b12`; backlog is unchanged.

### PASS_STATIC_ONLY

- direct R4E -> R5A source diff shows only delegation/extraction of the pre-existing physical framing block plus the new pure token model/tests/ratchet/documentation;
- all 15 `tests/**/TestData` files remain byte-identical, including 6/6 NACHA goldens and the CENIT ROR fixture;
- fingerprint-related artifacts remain byte-identical;
- all 12 `docs/normativa` files remain byte-identical;
- all 92 PostgreSQL/Persistence migration files remain byte-identical;
- all 79 SQL Server migration-project files remain byte-identical;
- SPA/docs-spa and MFT/SFTP paths are unchanged;
- outbound R4A-R4E implementation files are unchanged;
- no schema/migration was generated.

### NOT_RUN_ENVIRONMENT

- `git status --short`: source archive has no `.git`;
- `git rev-parse HEAD`: source archive has no `.git`;
- `dotnet build ACHInterbank.sln -c Release`: `dotnet` unavailable;
- focused R5A tests and existing inbound characterization suites: `dotnet` unavailable;
- golden/fingerprint/CENIT ROR executable tests: `dotnet` unavailable;
- broad backend suite: `dotnet` unavailable;
- Docker/runtime E2E: Docker unavailable.

No unavailable command is reported as PASS.

## Behavioral / normative / schema impact

- `BEHAVIORAL_DIFF=NONE`.
- `NORMATIVE_DIFF=NONE`.
- `DB_MIGRATIONS=NONE`.

Physical record widths, offsets, record-type ordering, addenda bytes, sequence bytes and existing structural error detection remain unchanged. No chamber rule, cause, code, cycle, window, limit, Return/ROR rule, PUBLISH authority, idempotency, persistence, audit, hash or MFT semantic was changed.

R5A therefore remains `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`. `NEXT_ALLOWED_PHASE=R5B` is calculated only for a later LOCAL_ADVANCEMENT increment; R5B/R5C/R5D/R6 and later work were not executed here.
