# REFACTOR-CA-SOLID-RES-001 — R4B official NACHA record rendering extraction

## State

- Phase: `R4B`
- State: `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`
- Execution mode: `LOCAL_ADVANCEMENT`
- Current Git SHA: `UNAVAILABLE_SOURCE_ARCHIVE_NO_GIT_METADATA`
- Prior phases: R0/R1/R2/R3/R4A remain `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`
- Next allowed phase: `R4C` (not executed in this increment)

R4B is structurally implemented on the delivered R4A tree. It is not `VERIFIED` or `CLOSED`: the archive contains no `.git` metadata and this host has no `dotnet` or Docker. This increment is restricted to record-rendering ownership; it does not introduce the R4C assembly pipeline, R4D chamber policies, R5 parsing work, or `PaymentRailOperationalStrategy`.

## Exact R4B scope implemented

The R4 plan authorizes record-renderer extraction while preserving the current PUBLISH/table-driven engine and byte-for-byte official output. The plan lists T1/T5/T6/T7/T8/T9 renderers as examples but explicitly requires names/abstractions to fit the current model and forbids forcing an abstraction that duplicates the table-driven engine. The current official path renders all six record codes through one published `CfgLayoutVariant`/`CfgFieldRule` mechanism, so R4B uses one internal generic renderer instead of six wrapper classes.

`INachaOfficialRecordRenderer` / `NachaOfficialRecordRenderer` now owns only deterministic physical rendering of one official record:

- resolve the configured source value for the current record object;
- execute published `CfgFieldRule` transformations/validations;
- apply official field formatting, repertoire/type/allowed-value/overflow/length validation;
- place/pad the value into the configured fixed-width field position;
- emit the existing field trace and safe line-level validation evidence.

The renderer receives the record code, record object, published materialized layout, audit collector, deterministic diagnostic context, sequence and line number. It is internal/stateless and has no DB, HTTP, filesystem, service-provider/global-configuration, MFT or rail-policy dependency. It reuses the R4A `NachaDeterministicFieldFormatter` instead of reimplementing source-value resolution.

Two private `BuildRecordInternalAsync` methods with no callers were also removed from `NachaFileBuilder`; the active legacy/configured path already delegates to the pre-existing `INachaFixedWidthRecordRenderer`, so removing those dead copies does not change runtime ownership.

## Responsibilities intentionally retained in `NachaFileBuilder`

R4B does not start R4C. The facade continues to own orchestration and all side-effect/policy boundaries that determine *which* records exist and *when* they are emitted:

- the six public `INachaFileBuilder` methods and caller compatibility;
- chamber/profile/PUBLISH snapshot resolution and materialized-layout validation;
- ACH Colombia versus CENIT flow decisions and historical diagnostic-context selection;
- transaction selection, batch construction, T6/T7 association and record sequence inputs;
- assembly order for T1/T5/T6/T7/T8/T9;
- the existing authoritative `INachaControlTotalsCalculator` composition;
- file blocking/padding orchestration through R4A `NachaFileBlocking`;
- semantic/inter-record validation;
- SHA-256 generation audit, persistence, traceability and side effects.

Keeping these concerns in the facade is deliberate: moving them would be R4C/R4D work and would exceed this increment.

## Public/API/DI impact

`INachaFileBuilder` remains 6/6 methods. No public interface, route, DTO, constructor signature, DI registration, persistence model or API contract changes. The renderer is an internal stateless implementation referenced by the builder without adding a public dependency. Existing callers, MFT/jobs, Returns/ROR and audit workflows therefore keep the same public integration surface.

## Architecture ratchet

`scripts/validate-r4b-nacha-record-renderers.py` is the R4B host-independent ratchet. It enforces:

- the six public `INachaFileBuilder` methods remain unchanged;
- the internal official renderer contract/implementation exist and the builder delegates official field rendering to them;
- builder-owned official source-value resolution, field-rule execution, official formatting/placement/trace and dead `BuildRecordInternalAsync` copies are absent;
- profile/PUBLISH selection, assembly, control totals, semantic validation and audit ownership remain in the builder;
- the renderer contains no DB/HTTP/filesystem/global-config/service-provider/MFT, chamber-specific ACH/CENIT policy, `PaymentRailOperationalStrategy`, control-total or assembly-pipeline dependency;
- the renderer consumes the R4A deterministic formatter and the existing table-driven `CfgLayoutVariant`/`CfgFieldRule` model;
- R4B focused characterization source is present.

Metrics for this increment: `NachaFileBuilder` 4,575 -> 3,796 lines; R4A deterministic helpers 2 -> 2; implementation-root renderer files 2 -> 3; new internal R4B renderer interfaces 0 -> 1; public facade methods 6 -> 6; R4B-scope `PaymentRailOperationalStrategy` references 0 -> 0. R2 Persistence fallback debt remains 57/57 and all R3 transport/security ratchets remain unchanged.

The R4A validator was adjusted only to be forward-compatible once R4B is legitimately present: it no longer treats the existence of an R4B renderer contract as a phase violation, while it still enforces the R4A facade, helper, control-total and rail-neutrality invariants.

## Characterization and byte-equivalence evidence

`NachaOfficialRecordRendererTests` adds focused test source that characterizes configured field order, exact width/padding, trace order/status and repeated deterministic output for the same inputs. The expected bytes/goldens are not modified.

Static R4A->R4B artifact comparison confirms the repository copies of all six NACHA golden files, the golden fingerprint characterization source and the CENIT ROR fixture are byte-identical. Because `dotnet` is unavailable, generated-file byte equivalence could not be re-executed; the executable golden/fingerprint/ROR tests are therefore `NOT_RUN_ENVIRONMENT`, not PASS.

## Preserved invariants and behavioral diff

`BEHAVIORAL_DIFF=NONE`. `NORMATIVE_DIFF=NONE`. R4B does not intentionally change:

- ACH Colombia or CENIT rules, layouts, fixed record sizes, field positions, padding, record order, transaction codes, causes, cycles, windows, limits or Return/ROR semantics;
- PUBLISH snapshot authority, profile selection, control totals, record counts or block count ownership;
- golden/expected bytes or fingerprint characterization;
- idempotency, deduplication, membership, SHA-256 hashing, audit/traceability or cluster safety;
- retry/recovery or MFT/SFTP publication semantics;
- EF Code First schema, PostgreSQL migrations or SQL Server migration project;
- SPA/docs-spa.

The renderer contains no ACH Colombia/CENIT business branch. The small render context only carries already-resolved diagnostic chamber strings from the facade so historical error/audit text remains stable without moving rail policy into the deterministic renderer.

## Static evidence available in this environment

- `python scripts/validate-solution-projects.py`: PASS; solution/project graph remains complete.
- `python scripts/validate-r2-di-configuration.py`: PASS; `AppSettings.Settings` = 0, targeted hidden collaborators = 0, Persistence fallback debt = 57/57.
- `python scripts/validate-r3-external-resilience.py`: PASS; R3 transport/logging ratchet remains intact.
- `python scripts/validate-r4a-nacha-builder-facade.py`: PASS; R4A facade/helper/control-total ratchet remains intact and is forward-compatible with authorized R4B ownership.
- `python scripts/validate-r4b-nacha-record-renderers.py`: PASS; facade 6/6, builder 4,575 -> 3,796 lines, renderer files 2 -> 3, internal renderer interfaces 0 -> 1, forbidden renderer dependencies = 0, R4B-scope PaymentRail coupling = 0.
- `python scripts/scan_result_solid.py`: exits 1 for the known broad program backlog; output remains byte-identical to the captured R4A output with SHA-256 `e43aacb21b2b84e4f93793bfa1ced9805d16f644fc061fadeb7705e7cf802ad5`.
- protected-artifact comparison against R4A: TestData/goldens/fingerprint/CENIT ROR, `docs/normativa`, Persistence migrations, SQL Server migration project and SPA/docs-spa are byte-identical.

## Executable evidence unavailable

`git status --short` and `git rev-parse HEAD` fail because the input archive has no `.git`. `dotnet` and Docker are unavailable, so the Release build, focused R4B tests, architecture tests, deterministic NACHA suite, golden/fingerprint tests, CENIT ROR characterization and broad backend suite are `NOT_RUN_ENVIRONMENT`.

R4B therefore remains `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`. `NEXT_ALLOWED_PHASE=R4C` is calculated for a later LOCAL_ADVANCEMENT increment only; R4C was not executed here.
