# REFACTOR-CA-SOLID-RES-001 — R4E facade thinning and canonical chamber resolution

## State

- Phase: `R4E`
- State: `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`
- Execution mode: `LOCAL_ADVANCEMENT`
- Current Git SHA: `UNAVAILABLE_SOURCE_ARCHIVE_NO_GIT_METADATA`
- Prior phases: R0/R1/R2/R3/R4A/R4B/R4C/R4D remain `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`
- Next allowed phase: `R5A` (not executed in this increment)

R4E is structurally implemented on the delivered R4D tree. It is not `VERIFIED` or `CLOSED`: the source archive has no `.git` metadata and this host has no `dotnet` or Docker. The plan assigns R4E only to facade thinning and removal of the concrete remaining runtime fallbacks after R4D. R5 parser/post-processing work is out of scope.

## Exact R4E scope implemented

R4D explicitly left two outbound legacy/runtime fallbacks for R4E:

```text
ClearingHouse.Name.Contains("CENIT") ? "CENIT" : "ACH"
```

Both occurrences were in the legacy/configured ordinary-generation path. A required broad R4E scan also found one additional builder-scope clearing-house heuristic in the Type7 table-driven opt-in gate: it compared the clearing-house display name with configured values using a partial `Contains` match. R4E removes all three builder-scope name heuristics and reuses the canonical/configured resolution established by prior work:

```text
NachaClearingHouseProfileCodeResolver
  -> canonical clearing-house code
  -> NachaChamberFlowPolicyResolver
  -> ACH Colombia / CENIT / compatibility policy
```

No new resolver, free-text heuristic, partial match or database-ID switch was introduced. `NachaClearingHouseProfileCodeResolver` still prefers the configured profile clearing-house code and otherwise maps an exact operational code/name against `CatClearingHouses` to obtain the canonical code. That exact catalog mapping is configuration resolution, not direct name-to-rail branching.

## Facade thinning

`NachaFileBuilder` remains the public `[Scoped]` `INachaFileBuilder` facade with exactly the same six interface methods and the same constructor/DI surface used by callers and tests. Its generation implementation was moved to the internal `NachaFileGenerationCoordinator`.

Metrics:

- `NachaFileBuilder`: 3,349 -> 98 lines;
- public facade methods: 6 -> 6;
- deterministic formatter/blocking/official-renderer/official-assembly components: 4 -> 4;
- renderer files: 3 -> 3;
- assembly components: 1 -> 1;
- chamber/flow policy file: 1 -> 1;
- new public contracts: 0;
- `PaymentRailOperationalStrategy`: 0 -> 0.

A direct generation-body comparison of the R4D builder with the new coordinator shows only the authorized chamber-resolution replacements, the Type7 clearing-house opt-in change from display-name partial matching to exact canonical-code matching, and the parameter plumbing required to pass the already-resolved canonical code into the legacy runtime-config resolver. No unrelated generation algorithm was rewritten.

The facade deliberately keeps the three pre-existing constructor fallback sites already covered by the Persistence architecture debt ratchet (`DailyResetBatchNumberGenerator`, `OperationalTimeSnapshotProvider`, `CycleCalendarGuard`) in the same file rather than moving them and creating new allowlist paths. R4E does not expand the R2 allowlist.

## Ownership before -> after

| Concern | R4D owner | R4E owner / authority |
|---|---|---|
| public `INachaFileBuilder` contract and DI entry | `NachaFileBuilder` | unchanged `NachaFileBuilder` facade |
| legacy/runtime ordinary chamber identification | two display-name `Contains("CENIT")` fallbacks | canonical `NachaClearingHouseProfileCodeResolver` |
| Type7 clearing-house table-driven opt-in | partial display-name `Contains(configValue)` | exact match against already-resolved canonical clearing-house code |
| chamber/flow policy selection | `NachaChamberFlowPolicyResolver` | unchanged; exact canonical code |
| generation orchestration implementation | `NachaFileBuilder` | internal `NachaFileGenerationCoordinator` |
| PUBLISH snapshot/profile/layout resolution | builder + existing resolver | same resolver/selection semantics, invoked by coordinator |
| transaction selection / batch preparation | builder | same implementation in coordinator |
| control totals calculation | `INachaControlTotalsCalculator` | unchanged unique authority |
| official record rendering | `INachaOfficialRecordRenderer` | unchanged |
| official physical assembly | `NachaOfficialAssemblyPipeline` | unchanged |
| semantic validation | existing validator invoked by builder | same validator invoked by coordinator |
| SHA-256 | builder | same single calculation site moved with implementation to coordinator |
| audit/persistence | builder | same single persistence site moved with implementation to coordinator |
| ReturnOut/ROR physical generation | separate builder path | same separate path moved intact with implementation to coordinator |
| MFT/SFTP / jobs / recovery | existing owners | unchanged |

Moving implementation out of the facade changes the internal class holding the code, not the functional authority, selection dimensions, side-effect ordering or persistence/hash semantics.

## ACH Colombia / CENIT separation

The R4D policies remain separate concrete implementations. R4E does not homogenize rail behavior and does not leak chamber selection into the deterministic R4B renderer or R4C assembly pipeline. The canonical code is resolved before policy selection; the policy resolver performs exact code matching for `ACH` and `CENIT` and keeps the pre-existing compatibility policy for non-canonical codes.

Focused characterization extends the existing `NachaFileBuilderFileIntegrityClosureTests` with display name `ACH CENIT Legacy Label` and expected canonical code `ACH`. The purpose is to prove that a display label containing the text `CENIT` no longer reclassifies a valid ACH configuration. This source test is not reported as executed because .NET is unavailable.

The repository-wide scan also identifies ten pre-existing `Contains("CENIT")` sites outside the R4E builder/facade scope: two in `NachaConfigValidationService`, seven in `NachaInboundSimulationService`, and one in `IncomingNachaIngestionAppService`. They are not moved or rewritten in R4E because the documented R4E prompt is limited to remaining concrete fallbacks from the builder/facade, while inbound parser/post-processing work starts at R5A. They are reported explicitly rather than hidden behind a new allowlist.

## PUBLISH / snapshot

R4E does not alter PUBLISH authority. The same `INachaConfigResolver`, `ResolvePublishedOrdinaryAsync`, profile/layout identity checks, effective-date dimensions, materialized layout retrieval and historical immutability behavior remain. Only the containing class changes from facade to coordinator. There is no new snapshot owner, resolver or fallback.

## Control totals, rendering, assembly, SHA and audit

- `INachaControlTotalsCalculator` remains the only totals calculator.
- `NachaOfficialRecordRenderer` remains the official table-driven renderer.
- `NachaOfficialAssemblyPipeline` remains the physical T1/T5/T6/T7/T8/T9 assembly owner and remains rail-agnostic.
- SHA-256 is still calculated once in the same generation sequence.
- generation audit persistence still occurs once in the same sequence.
- no new DB, HTTP, filesystem, MFT/SFTP or side-effect dependency was added to deterministic components.

## ReturnOut / ROR

ReturnOut/ROR is not merged with ordinary assembly and is not redesigned. Its physical parameters, profile/layout, cause/addenda/order, hash and audit/persistence flow are unchanged. R4E only moves the already-existing implementation behind the thin facade; chamber/flow decisions continue to come from the R4D policies. The CENIT ROR fixture is unchanged.

## R4E architecture ratchet

`scripts/validate-r4e-chamber-resolution.py` is the host-independent R4E ratchet. It enforces:

- public facade remains 6/6 and <= 500 lines;
- internal `NachaFileGenerationCoordinator` owns the moved generation implementation;
- outbound `ClearingHouse.Name.Contains("CENIT")`: 2 -> 0;
- additional equivalent outbound name heuristics: 1 -> 0;
- database-ID rail inference: 0 -> 0;
- canonical resolver + exact code policy resolver remain the resolution authority;
- renderer and assembly component sets remain unchanged and rail-agnostic;
- `PaymentRailOperationalStrategy` remains absent;
- no new chamber-resolution exception/allowlist is introduced;
- focused misleading-display-name characterization source exists.

R4A-R4D validators were updated only to recognize the now-authorized R4E coordinator as the generation owner; their original constraints remain enforced.

## Static evidence available in this environment

PASS executed:

- `python scripts/validate-solution-projects.py`;
- `python scripts/validate-r2-di-configuration.py` (`AppSettings.Settings` 0, critical hidden collaborators 0, Persistence fallback debt 57/57);
- `python scripts/validate-r3-external-resilience.py` (`new HttpClient()` 0, raw default injection 0, managed-client gaps 0, sensitive logging 0, health retry 2, generic retry 0, SOAP retry 0);
- `python scripts/validate-r4a-nacha-builder-facade.py`;
- `python scripts/validate-r4b-nacha-record-renderers.py`;
- `python scripts/validate-r4c-nacha-assembly-pipeline.py`;
- `python scripts/validate-r4d-chamber-flow-policies.py`;
- `python scripts/validate-r4e-chamber-resolution.py`.

FAIL executed:

- `python scripts/scan_result_solid.py` exits 1 for the known broad program backlog. Captured R4D and R4E output is byte-identical with SHA-256 `343240794946d5b8cb48bce2209baa37e755ab1bb443012f9cef5ef73d038db6`; the backlog neither grew nor shrank in R4E.

PASS_STATIC_ONLY:

- moved generation-body comparison: only the authorized chamber-resolution changes, Type7 exact canonical-code opt-in replacement and parameter plumbing;
- 15/15 `tests/**/TestData` files unchanged, including all 6/6 NACHA goldens;
- fingerprint-related files unchanged;
- CENIT ROR-related fixture/test artifacts unchanged;
- 12/12 `docs/normativa` files unchanged;
- 92/92 Persistence/PostgreSQL migration files unchanged;
- 79/79 SQL Server migration-project files unchanged;
- 776/776 SPA + `docs/spa` files unchanged;
- all path-identified MFT/SFTP artifacts unchanged;
- no schema or migration was generated.

## Executable evidence unavailable

`git status --short` and `git rev-parse HEAD` fail because the input archive has no `.git`. `dotnet`, `csc`/`msbuild` and Docker are unavailable, so Release build, focused R4E/R4D/R4A-R4C tests, architecture .NET tests, deterministic NACHA generation, executable golden/fingerprint tests, CENIT ROR characterization and the broad backend suite are `NOT_RUN_ENVIRONMENT`, not PASS.

## Behavioral / normative / schema impact

- `BEHAVIORAL_DIFF=NONE` for valid configured/canonical chamber configurations. The removed fallback no longer silently classifies a misconfigured display label; this is the R4E architecture ratchet, not a financial/domain rule change.
- `NORMATIVE_DIFF=NONE`.
- `DB_MIGRATIONS=NONE`.
- no layout, record width, offset, padding, ordering, code, cause, cycle, window, limit, Return/ROR, PUBLISH snapshot, control-total, SHA, audit, idempotency, cluster-safety, retry/recovery or MFT/SFTP semantic change is authorized or introduced.

R4E therefore remains `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`. `NEXT_ALLOWED_PHASE=R5A` is calculated only for a later LOCAL_ADVANCEMENT increment; R5A/R5 and later work were not executed here.
