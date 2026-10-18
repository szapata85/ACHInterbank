# REFACTOR-CA-SOLID-RES-001 — R4D chamber/flow policy encapsulation

## State

- Phase: `R4D`
- State: `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`
- Execution mode: `LOCAL_ADVANCEMENT`
- Current Git SHA: `UNAVAILABLE_SOURCE_ARCHIVE_NO_GIT_METADATA`
- Prior phases: R0/R1/R2/R3/R4A/R4B/R4C remain `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`
- Next allowed phase: `R4E` (not executed in this increment)

R4D is structurally implemented on the delivered R4C tree. It is not `VERIFIED` or `CLOSED`: the source archive has no `.git` metadata and this host has no `dotnet` or Docker. The plan limits R4D to encapsulating ACH Colombia/CENIT chamber/flow decisions after the R4C pipeline, resolving them by semantic code/profile/configuration rather than database IDs, while preserving the table-driven/PUBLISH authority. R4E fallback removal/facade thinning and all R5 work are explicitly out of scope.

## Exact R4D scope implemented

A new internal `INachaChamberFlowPolicy` boundary plus `NachaChamberFlowPolicyResolver` encapsulates only the chamber/flow decisions that were embedded in the official/ReturnOut generation paths. The resolver selects by the already-resolved canonical clearing-house code (`ACH` or `CENIT`), never by database IDs. The policies own:

- ACH Colombia versus CENIT ReturnOut/ROR physical decision inputs: record length, blocking factor, selection-flow value and generation-audit phase, including the existing CENIT ROR PPD-only scope/error codes;
- whether ordinary generation requires CENIT service partitioning;
- ordinary-flow decisions derived from the already-resolved PUBLISH profile, including CENIT CTX sequence/association requirements and the ACH Colombia Type-7 published-variant behavior;
- the existing renderer diagnostic chamber context values;
- the existing CENIT LIVE/no-homologation and general LIVE official-engine gates;
- chamber-specific official-layout snapshot validation for ACH Colombia and CENIT variants.

`AchColombiaNachaChamberFlowPolicy` and `CenitNachaChamberFlowPolicy` keep their rail-specific semantics separate. `CompatibilityNachaChamberFlowPolicy` preserves the pre-R4D behavior for a non-canonical code instead of silently reclassifying it as one of the two rails. This is a compatibility boundary, not a new rail model.

## Responsibilities intentionally retained in `NachaFileBuilder`

R4D does not move side effects or later-phase fallback cleanup. `NachaFileBuilder` remains the six-method public `INachaFileBuilder` facade and continues to own/orchestrate:

- canonical clearing-house/profile resolution and PUBLISH snapshot selection;
- transaction selection and batch/data preparation;
- CENIT partition configuration lookup and invocation of the existing partitioner after the policy says partitioning is required;
- CTX cardinality/sequence mechanics and T6/T7 association validation using policy decisions already resolved;
- `INachaControlTotalsCalculator` invocation;
- R4B record rendering and R4C official assembly pipeline invocation;
- semantic/inter-record validation;
- SHA-256 calculation;
- generation audit persistence and ordering of side effects;
- the separate ReturnOut/ROR physical generation path and its persistence/audit flow.

Two legacy name-based compatibility fallbacks (`ClearingHouse.Name.Contains("CENIT")`) remain in the legacy/runtime-fallback path. The plan assigns concrete fallback removal and facade thinning to R4E; removing them in R4D would advance scope without the R4E DI/test gate. They are therefore explicitly retained and ratcheted as remaining debt, not newly introduced debt.

## Ownership before -> after

| Concern | R4C owner | R4D owner |
|---|---|---|
| PUBLISH snapshot/profile resolution | `NachaFileBuilder` / existing resolver | unchanged |
| transaction selection / batch preparation | `NachaFileBuilder` | unchanged |
| ACH Colombia/CENIT official/ReturnOut chamber-flow decisions | embedded branches in `NachaFileBuilder` | `INachaChamberFlowPolicy` implementations |
| control totals calculation | `INachaControlTotalsCalculator` | unchanged |
| official record rendering | `INachaOfficialRecordRenderer` | unchanged |
| official physical assembly | `NachaOfficialAssemblyPipeline` | unchanged |
| semantic validation | `NachaFileBuilder` / existing validator | unchanged |
| SHA-256 | `NachaFileBuilder` | unchanged |
| audit/persistence | `NachaFileBuilder` | unchanged |
| ReturnOut/ROR physical generation | separate builder path | separate builder path; only policy inputs extracted |
| MFT/SFTP / jobs / recovery | existing owners | unchanged |

The new policy layer has no DB, HTTP, filesystem, global configuration, `IServiceProvider`, PUBLISH resolver, control-total calculator, assembly pipeline, SHA, audit/persistence, MFT/SFTP or job dependency. `PaymentRailOperationalStrategy` is not introduced.

## PUBLISH/snapshot authority

R4D does not move or reinterpret authoritative snapshot selection. `ResolveOfficialRuntimeConfigAsync`, `BuildConfigResolutionRequest`, `ResolvePublishedOrdinaryAsync`, published identity checks and materialized-layout retrieval remain in the facade/existing resolver. A policy receives the already-resolved profile/layout context and only applies the same chamber-specific gate/snapshot comparison that previously lived in the builder.

`ResolveCenitOutboundPoliciesAsync` now receives the canonical clearing-house code that the facade already resolved rather than restating the literal `CENIT` when issuing the existing PUBLISH-resolution request. This removes duplicated chamber identity without changing the resolver, request dimensions, PUBLISH selection or table-driven authority.

## Control totals, SHA, audit and persistence

`INachaControlTotalsCalculator` / `NachaControlTotalsCalculator` remains the sole calculation authority. The R4C pipeline still consumes/verifies totals and R4D policies do not reference totals.

SHA-256 remains computed once in `NachaFileBuilder`; generation audit persistence remains there as well. R4D introduces no second hash or persistence owner and does not change side-effect ordering, idempotency, recovery or cluster behavior.

## ReturnOut / ROR

ReturnOut/ROR stays outside `NachaOfficialAssemblyPipeline`. R4D only encapsulates the pre-existing rail/flow decision inputs used by `BuildReturnOutAsync`: ACH V35 versus CENIT physical length/blocking, CENIT ROR detection, the existing PPD-only rule and existing selection/audit labels. No Return/ROR layout, causal, addenda, ordering, control, hash or persistence behavior is redesigned. The CENIT ROR fixture remains unchanged.

## ACH Colombia / CENIT separation

The two rail policies are concrete and separate. CENIT CTX profile recognition is confined to the CENIT policy; ACH Colombia published Type-7 behavior is confined to the ACH policy. The common base contains only rail-neutral LIVE official-mode and descriptor helper mechanics. The official assembly pipeline remains rail-agnostic, and no common policy homogenizes rules that differ between rails.

## Public/API/DI impact

`INachaFileBuilder` remains 6/6 methods. No public controller/API contract, DTO, constructor signature or DI registration changes. The policy boundary is `internal`; no production service registration is required because it is a stateless pure decision boundary resolved from the canonical code already held by the facade. No schema/entity/migration change is introduced.

## Architecture ratchet

`scripts/validate-r4d-chamber-flow-policies.py` is the host-independent R4D ratchet. It enforces:

- public facade remains 6/6;
- canonical code/profile policy resolution exists with no `ClearingHouseId`/DB-ID switch;
- official ReturnOut and ordinary builder paths no longer own direct `ACH`/`CENIT`/CENIT-CTX decisions;
- the R4C assembly pipeline remains rail-agnostic;
- PUBLISH/profile resolution, transaction/batch preparation, authoritative totals, semantic validation, SHA-256 and audit persistence remain with existing owners;
- policy code has no DB/HTTP/filesystem/global-config/service-provider/PUBLISH/totals/hash/persistence/MFT/SFTP dependency;
- no `PaymentRailOperationalStrategy` appears in R4D scope;
- renderer and assembly component counts do not change.

Metrics: `NachaFileBuilder` 3,693 -> 3,349 lines; deterministic formatter/blocking/renderer/assembly components 4 -> 4; renderer files 3 -> 3; assembly components 1 -> 1; chamber/flow policy files 0 -> 1; new internal R4D policy interfaces 0 -> 1 with three policy implementations (ACH Colombia, CENIT, compatibility); public facade methods 6 -> 6. R2 Persistence fallback debt remains 57/57, and the two legacy name-based builder fallbacks are intentionally retained for R4E rather than hidden by a new exception.

## Characterization and byte-equivalence evidence

`NachaChamberFlowPolicyTests` adds focused source characterization for code-based policy resolution, ACH ReturnOut V35 decisions, CENIT ROR PPD-only scope/error codes, ACH/CENIT ordinary Type-7/CTX decisions, render-context values and generation gates. These are source tests only on this host because .NET is unavailable.

Static R4C -> R4D comparison preserves byte-identically all 15 `tests/**/TestData` files, including all six NACHA goldens and the CENIT ROR fixture. The fingerprint characterization source is unchanged. `docs/normativa`, PostgreSQL/Persistence migrations, the SQL Server migrations project, SPA/docs-spa and MFT/SFTP-related source/configuration are unchanged. No golden or expected byte was updated.

Because `dotnet` is unavailable, generated NACHA byte equivalence, SHA-256 equality for equivalent generated content, executable goldens/fingerprints and CENIT ROR characterization are `NOT_RUN_ENVIRONMENT`, not PASS.

## Preserved invariants and behavioral diff

`BEHAVIORAL_DIFF=NONE`. `NORMATIVE_DIFF=NONE`. R4D intentionally does not change:

- record width, field positions, padding, T1/T5/T6/T7/T8/T9 ordering, batch order, T6/T7 association, counts, control totals, block count or file padding;
- ACH Colombia/CENIT codes, causals, cycles, windows, limits, Return/ROR or NACHA-M semantics;
- PUBLISH snapshot authority, historical immutability or profile/layout selection dimensions;
- golden outputs/fingerprints;
- SHA-256 meaning, audit, persistence, idempotency, cluster safety, retry/recovery or MFT/SFTP semantics;
- EF Code First schema or migrations.

## Static evidence available in this environment

- `python scripts/validate-solution-projects.py`: PASS.
- `python scripts/validate-r2-di-configuration.py`: PASS; `AppSettings.Settings` = 0, targeted hidden collaborators = 0, Persistence fallback debt = 57/57.
- `python scripts/validate-r3-external-resilience.py`: PASS; direct product `new HttpClient()` = 0, raw default injections = 0, missing managed registrations = 0, sensitive logging findings = 0, health retry sites = 2, generic retry = 0, SOAP retry = 0.
- `python scripts/validate-r4a-nacha-builder-facade.py`: PASS.
- `python scripts/validate-r4b-nacha-record-renderers.py`: PASS.
- `python scripts/validate-r4c-nacha-assembly-pipeline.py`: PASS.
- `python scripts/validate-r4d-chamber-flow-policies.py`: PASS; facade 6/6, builder 3,693 -> 3,349, renderers 3 -> 3, assembly 1 -> 1, new policy interface 1, implementations 3, forbidden policy dependencies 0, `PaymentRailOperationalStrategy` 0.
- `python scripts/scan_result_solid.py`: exits 1 for the known broad program backlog; its captured output is byte-identical before/after R4D on this host with SHA-256 `343240794946d5b8cb48bce2209baa37e755ab1bb443012f9cef5ef73d038db6`.

## Executable evidence unavailable

`git status --short` and `git rev-parse HEAD` fail because the input archive has no `.git`. `dotnet` and Docker are unavailable, so Release build, focused R4D/R4A/R4B/R4C tests, architecture tests, deterministic NACHA tests, golden/fingerprint tests, CENIT ROR characterization and the broad backend suite are `NOT_RUN_ENVIRONMENT`.

R4D therefore remains `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`. `NEXT_ALLOWED_PHASE=R4E` is calculated for a later LOCAL_ADVANCEMENT increment only; R4E, R5 and later work were not executed here.
