# REFACTOR-CA-SOLID-RES-001 — R4C NACHA official assembly pipeline extraction

## State

- Phase: `R4C`
- State: `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`
- Execution mode: `LOCAL_ADVANCEMENT`
- Current Git SHA: `UNAVAILABLE_SOURCE_ARCHIVE_NO_GIT_METADATA`
- Prior phases: R0/R1/R2/R3/R4A/R4B remain `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`
- Next allowed phase: `R4D` (not executed in this increment)

R4C is structurally implemented on the delivered R4B tree. It is not `VERIFIED` or `CLOSED`: the source archive contains no `.git` metadata and this host has no `dotnet` or Docker. This increment is limited to the deterministic physical assembly boundary of the ordinary official NACHA path. It does not introduce R4D chamber/flow policies, R5 work, `PaymentRailOperationalStrategy`, or a new control-total implementation.

## Exact R4C scope implemented

The R4C plan calls for an assembly pipeline while preserving PUBLISH/table-driven authority, callers and byte-for-byte output. `NachaOfficialAssemblyPipeline` now owns the deterministic physical composition that was previously embedded in `NachaFileBuilder`:

- structural T1 -> T5 -> T6 -> T7 -> T8 -> T9 emission order;
- concatenation of records through the existing R4B `INachaOfficialRecordRenderer`;
- structural batch/detail/addenda state so T7 records remain associated with the currently open T6 entry;
- verification of rendered T8/T9 control fields against the already-calculated authoritative totals;
- final file blocking/padding through the R4A `NachaFileBlocking` helper;
- the existing file-integrity and padding audit trace entries that are a consequence of physical assembly.

The pipeline is deliberately stage-oriented (`AppendFileHeader`, `BeginBatch`, `BeginEntry`, `AppendAddenda`, `EndEntry`, `EndBatch`, `Complete`) rather than accepting an independently rebuilt file model. This preserves the existing timing of preparation and validation in the facade: for example, T5 is rendered before the existing CENIT CTX checks, T6 is rendered before the existing no-addenda/association checks, and a later validation failure therefore retains the same already-rendered record count used by generation audit.

## Responsibilities intentionally retained in `NachaFileBuilder`

R4C does not reinterpret or relocate policy/side-effect ownership. The facade remains responsible for:

- the six public `INachaFileBuilder` methods and caller compatibility;
- PUBLISH snapshot/profile resolution and materialized-layout validation;
- ACH Colombia versus CENIT policy and flow decisions;
- transaction selection, batch preparation, sequence inputs and record-data construction;
- CENIT CTX sequence checks plus T6/T7 association, type-7 policy and layout selection;
- invocation of the existing authoritative `INachaControlTotalsCalculator`;
- semantic/inter-record validation after the file is assembled;
- SHA-256 content hashing;
- generation audit persistence, traceability, idempotency ownership and all other side effects.

`BuildReturnOutAsync`/ROR remains on its separate existing physical path. R4C only renames its private append helper to `AppendReturnOutOfficialRecords` so it is not confused with the ordinary official assembly path; Return/ROR rules, bytes and control behavior are not redesigned.

## Control totals ownership

`INachaControlTotalsCalculator` / `NachaControlTotalsCalculator` remains the single calculator and authority for batch/file control totals. `NachaOfficialAssemblyPipeline` receives `NachaBatchControlTotals` and `NachaFileControlTotals` as inputs. It does not recompute financial totals, hashes, counts from business data, or create an alternative totals implementation; it only verifies that the already-rendered T8/T9 control fields match the authoritative calculated values before final composition completes.

## PUBLISH, audit, hash and side-effect ownership

PUBLISH selection/snapshot authority remains in `NachaFileBuilder` through the existing official-runtime resolution path. The R4C pipeline receives an already-resolved render context and already-materialized layouts; it does not query configuration or identify the rail.

SHA-256 computation, semantic validation and `PersistGenerationAuditAsync` remain in the facade after assembly. No DB, HTTP, filesystem, `IServiceProvider`, global configuration, MFT/SFTP, job, recovery or persistence dependency is introduced into the pipeline.

## ACH Colombia / CENIT separation

The pipeline contains no `CENIT`, ACH Colombia, clearing-house ID, transaction-type or rail-detection branch. Chamber/flow decisions stay in the facade and existing policy helpers. The pipeline only receives records/layouts/totals that the facade has already selected. Therefore R4C does not homogenize distinct ACH Colombia/CENIT semantics and does not pre-implement R4D.

## Public/API/DI impact

`INachaFileBuilder` remains 6/6 methods. No public interface, controller/API contract, DTO, constructor signature, DI registration, persistence entity or migration changes. `NachaOfficialAssemblyPipeline` is an internal implementation detail constructed per official file generation and introduces no R4C interface.

## Architecture ratchet

`scripts/validate-r4c-nacha-assembly-pipeline.py` is the host-independent R4C ratchet. It enforces:

- the six public facade methods remain unchanged;
- `NachaFileBuilder` delegates ordinary official physical assembly to `NachaOfficialAssemblyPipeline`;
- ordinary builder-owned `StringBuilder` composition, generic official append helper, final padding and rendered T8/T9 verification are absent from the official assembly method;
- the pipeline owns structural T1/T5/T6/T7/T8/T9 sequencing, composition, rendered control verification and R4A blocking/padding integration;
- PUBLISH/profile resolution, authoritative totals calculation, transaction/batch preparation, CENIT/ACH policy/association checks, semantic validation, SHA-256 and audit persistence stay in the builder/existing owners;
- the pipeline contains no DB/HTTP/filesystem/global-config/service-provider/MFT/SFTP/hash/persistence/rail-policy or `PaymentRailOperationalStrategy` coupling;
- no new R4C internal interface or second control-total implementation is introduced.

Metrics for this increment: `NachaFileBuilder` 3,796 -> 3,693 lines; deterministic components 3 -> 4; implementation-root renderer files 3 -> 3; assembly components 0 -> 1; new R4C internal interfaces 0 -> 0; public facade methods 6 -> 6. The R2 Persistence fallback debt remains 57/57 and R3 transport/security metrics remain unchanged.

The R4B validator was adjusted only to remain forward-compatible after the authorized R4C extraction: it still enforces the R4B renderer purity/table-driven boundary while allowing the assembly consumer to be the new R4C pipeline.

## Characterization and byte-equivalence evidence

`NachaOfficialAssemblyPipelineTests` adds focused source characterization for exact structural order, record-width-preserving concatenation, existing padding behavior, file-integrity/padding trace and deterministic repeated output. Expected/golden bytes are not modified.

Static R4B -> R4C comparison preserves byte-identically all repository copies of `tests/**/TestData`, the six NACHA goldens, golden fingerprint characterization and the CENIT ROR fixture. `docs/normativa`, Persistence/PostgreSQL migrations, the SQL Server migration project, SPA/docs-spa and MFT/SFTP files are also unchanged. Because `dotnet` is unavailable, generated NACHA byte equivalence, SHA equality for generated equivalent inputs and the executable golden/fingerprint/ROR characterization cannot be rerun here; those checks are `NOT_RUN_ENVIRONMENT`, not PASS.

## Preserved invariants and behavioral diff

`BEHAVIORAL_DIFF=NONE`. `NORMATIVE_DIFF=NONE`. R4C intentionally does not change:

- layouts, record widths, field positions, padding, ordering, batch/detail/addenda relationships, record counts, control totals or block counts;
- ACH Colombia/CENIT codes, causes, cycles, windows, limits, Returns/ROR or NACHA-M semantics;
- PUBLISH snapshot/profile authority or historical immutability;
- golden expected bytes/fingerprints;
- SHA-256 meaning, generation audit, idempotency, cluster safety, retry/recovery or MFT/SFTP semantics;
- EF Code First schema, PostgreSQL migrations, SQL Server migrations/project or SPA/docs-spa.

## Static evidence available in this environment

- `python scripts/validate-solution-projects.py`: PASS.
- `python scripts/validate-r2-di-configuration.py`: PASS; `AppSettings.Settings` = 0, targeted hidden collaborators = 0, Persistence fallback debt = 57/57.
- `python scripts/validate-r3-external-resilience.py`: PASS; direct product `new HttpClient()` = 0, raw default injections = 0, missing managed registrations = 0, sensitive logging findings = 0, health retry sites = 2, generic retry = 0, SOAP retry = 0.
- `python scripts/validate-r4a-nacha-builder-facade.py`: PASS.
- `python scripts/validate-r4b-nacha-record-renderers.py`: PASS with the authorized R4C assembly consumer.
- `python scripts/validate-r4c-nacha-assembly-pipeline.py`: PASS; facade 6/6, builder 3,796 -> 3,693 lines, deterministic components 3 -> 4, renderers 3 -> 3, assembly 0 -> 1, new R4C internal interfaces = 0 and forbidden pipeline dependencies = 0.
- `python scripts/scan_result_solid.py`: exits 1 for the known broad program backlog; its output remains byte-identical to the captured R4B output with SHA-256 `e43aacb21b2b84e4f93793bfa1ced9805d16f644fc061fadeb7705e7cf802ad5`.

## Executable evidence unavailable

`git status --short` and `git rev-parse HEAD` fail because the input archive has no `.git`. `dotnet` and Docker are unavailable, so Release build, focused R4C/R4A/R4B tests, architecture tests, deterministic NACHA tests, golden/fingerprint tests, CENIT ROR characterization and the broad backend suite are `NOT_RUN_ENVIRONMENT`.

R4C therefore remains `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`. `NEXT_ALLOWED_PHASE=R4D` is calculated for a later LOCAL_ADVANCEMENT increment only; R4D and R5 were not executed here.
