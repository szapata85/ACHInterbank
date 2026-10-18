# REFACTOR-CA-SOLID-RES-001 — R4A deterministic `NachaFileBuilder` extraction

## State

- Phase: `R4A`
- State: `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`
- Execution mode: `LOCAL_ADVANCEMENT`
- Current Git SHA: `UNAVAILABLE_SOURCE_ARCHIVE_NO_GIT_METADATA`
- Prior phases: R0/R1/R2/R3 remain `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`
- Next allowed phase: `R4B` (not executed in this increment)

R4A is structurally implemented on the delivered R3 tree. It is not `VERIFIED` or `CLOSED`: the archive has no `.git` metadata and this host has no `dotnet` or Docker. The implementation is intentionally limited to deterministic, IO-free responsibilities already owned by `NachaFileBuilder`; no record-renderer decomposition or later R4 work was started.

## Exact R4A scope implemented

The R4 plan authorizes only low-risk deterministic extraction while keeping `NachaFileBuilder` as the existing facade and preserving byte-for-byte NACHA output. R4A therefore extracted only responsibilities that do not select a chamber, profile, workflow, side effect or persistence path:

- `NachaDeterministicFieldFormatter` now owns identifier normalization, reflection/dictionary value resolution, `CONST:` resolution, legacy primitive formatting and Type-5 settlement-date normalization. The code was moved without changing its formatting rules.
- `NachaFileBlocking` now owns deterministic block-count/padding arithmetic, configured blocking-factor resolution and physical padding-record construction.
- the pre-existing `INachaControlTotalsCalculator` / `NachaControlTotalsCalculator` remains the control-total component; R4A reuses it and does not create a duplicate abstraction.
- `NachaFileBuilder` remains the public `INachaFileBuilder` implementation and delegates to these deterministic components. Its source size changes from the R3 baseline of 4,798 lines to 4,575 lines.

R4A does **not** introduce `IFileHeaderRenderer`, `IBatchHeaderRenderer`, `IEntryDetailRenderer`, `IAddendaRenderer`, `IBatchControlRenderer`, `IFileControlRenderer`, an assembly pipeline, chamber policy objects, or any R4B+ component.

## Impact inspection before modification

The public facade was not moved or renamed. The following dependency surface was inspected before editing:

- callers: `NachaExportController`, ACH Colombia managed-file exchange, outbound return artifact generation, Returns, Return-of-Return, inbound simulation and NACHA security operations;
- DI: the existing `[Scoped]` `NachaFileBuilder` registration convention and `INachaFileBuilder` contract remain unchanged; the new helpers are internal static components and add no DI registration;
- tests: builder unit/mapping tests, table-driven official generation, ACH transaction NACHA tests, control-total tests, golden/fingerprint characterization and Return/ROR characterization;
- persistence/PUBLISH: profile resolution, published snapshot selection, materialized-layout validation and audit persistence remain in the builder and were not reinterpreted;
- MFT/jobs/workflows: managed-file exchange, Return/ROR workflow, retry/recovery and dispatch ownership are unchanged;
- idempotency/hash/audit/trace: existing membership, PUBLISH authority, SHA-256 file hash, generation audit, trace entries and workflow idempotency remain untouched;
- rail separation: no extracted helper contains ACH Colombia or CENIT policy. Chamber-specific decisions remain where they were in the facade/table-driven configuration path.

No API route, Application interface, database model, migration, external filename policy, MFT/SFTP contract or job contract changed.

## Architecture ratchet

`scripts/validate-r4a-nacha-builder-facade.py` is the R4A host-independent ratchet. It enforces:

- the six existing `INachaFileBuilder` facade methods remain present;
- `NachaFileBuilder` remains the `INachaFileBuilder` implementation;
- the extracted helpers are internal/static and contain no DB, HTTP, filesystem, service-provider, chamber-policy or `PaymentRailOperationalStrategy` dependency;
- the builder delegates field resolution/normalization and blocking/padding to the extracted components;
- the pre-existing control-total calculator remains delegated;
- the extracted deterministic methods are no longer reimplemented inside the builder;
- no R4B renderer contracts are introduced;
- the builder is smaller than the 4,798-line R3 baseline.

R4A metrics: `NachaFileBuilder` 4,798 -> 4,575 lines; extracted deterministic helper components 0 -> 2; R4B renderer contracts introduced 0 -> 0; R4A-scope `PaymentRailOperationalStrategy` references 0 -> 0. The R2 Persistence fallback allowlist remains exactly 57/57 and the R3 external-resilience ratchet remains unchanged.

## Characterization and validation source

`NachaBuilderDeterministicComponentsTests` adds focused test source for the extracted deterministic behavior:

- normalized/qualified dictionary identifier resolution;
- legacy DateTime/decimal/bool/null formatting;
- block and padding count calculation;
- configured blocking-factor and 106-character padding construction;
- fail-closed invalid blocking factor.

Existing golden, table-driven, Return/ROR and functional tests were not modified to change expected results. They could not be executed on this host because `dotnet` is unavailable.

## Preserved invariants and behavioral diff

`BEHAVIORAL_DIFF=NONE` is the R4A intent and static result. No intentional financial, domain, normative or transport behavior change is introduced. In particular R4A does not change:

- ACH Colombia or CENIT rules, layouts, causes, transaction codes, cycles, windows, limits, Returns/ROR semantics or chamber separation;
- PUBLISH snapshot authority or published profile selection;
- NACHA golden files, expected results or fingerprint characterization;
- idempotency, deduplication, file membership, hashes, audit, traceability or cluster safety;
- retry/recovery, MFT/SFTP publication semantics or external acceptance semantics;
- EF model/schema, PostgreSQL migrations or SQL Server migrations;
- SPA/docs-spa.

The extracted field helper contains no chamber token and the extracted blocking helper contains no business rule; they only host deterministic code that was already executed by the builder.

## Static evidence available in this environment

- `python scripts/validate-solution-projects.py`: PASS; 7 solution projects, 7 disk projects, all project-reference targets loaded.
- `python scripts/validate-r2-di-configuration.py`: PASS; `AppSettings.Settings` = 0, targeted hidden collaborators = 0, Persistence fallback debt = 57/57.
- `python scripts/validate-r3-external-resilience.py`: PASS; R3 transport/logging ratchet remains intact.
- `python scripts/validate-r4a-nacha-builder-facade.py`: PASS; facade 6/6 unchanged, builder 4,798 -> 4,575 lines, deterministic components extracted, R4B contracts = 0, R4A-scope PaymentRail coupling = 0.
- `python scripts/scan_result_solid.py`: exits 1 for the known broad program backlog; its output SHA-256 remains `e43aacb21b2b84e4f93793bfa1ced9805d16f644fc061fadeb7705e7cf802ad5`, byte-identical before and after R4A source changes.
- protected-artifact SHA-256 comparison against the R3 input: all `tests/**/TestData/**` files (15), `docs/normativa` files (12), Persistence migration files (92), SQL Server migration-project files (79), SPA/docs-spa files (776), and the NACHA golden fingerprint characterization test are byte-identical.

## Executable evidence unavailable

The input archive contains no `.git`. `dotnet` and Docker are unavailable on this host. Therefore the Release build, R4A focused tests, architecture test suite, golden/fingerprint tests and broad backend suite are `NOT_RUN_ENVIRONMENT`.

R4A remains `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`. `NEXT_ALLOWED_PHASE=R4B` is calculated for a subsequent LOCAL_ADVANCEMENT increment only; R4B was not executed here.
