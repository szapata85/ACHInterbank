# Refactor State Memory — ACHInterbank

> This file is an execution state companion for `REFACTOR-CA-SOLID-RES-001`. Durable verified decisions/closures must be merged compactly into the repository root `MEMORY.md`. Keep this file and root memory consistent while the program is active.

## Program head

- PROGRAM_ID: `REFACTOR-CA-SOLID-RES-001`
- PROGRAM_STATE: `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`
- BASELINE_PACKAGE: `ACHInterbank-v124-DOTNET-10.0.12-SDK-10.0.401-SECURE-DEPS-2026-10-09`
- BASELINE_ACCEPTED_FUNCTIONAL_COMMIT_FROM_MEMORY: `22d10822c5222c3c72a76b7279d6a885dfcbc75b`
- CURRENT_IMPLEMENTATION_SHA: `UNAVAILABLE_SOURCE_ARCHIVE_NO_GIT_METADATA`
- CURRENT_PHASE: `R10`
- NEXT_ALLOWED_PHASE: `NONE`
- LAST_VERIFIED_PHASE: `NONE`
- GLOBAL_RELEASE_STATE: `NOT_ESTABLISHED`

## Program invariants

- `INV-REF-001`: Refactor must preserve accepted ACH Colombia and CENIT functional/normative behavior unless a separate proven normative change is explicitly authorized.
- `INV-REF-002`: Immutable PUBLISH snapshot authority for supported ordinary NACHA flows must remain intact.
- `INV-REF-003`: No NACHA golden file may change merely to accommodate refactoring.
- `INV-REF-004`: ACH Colombia and CENIT rules remain isolated; no cross-rail inference.
- `INV-REF-005`: Existing idempotency, deduplication, hash, audit, cluster-safety and recovery semantics must not be weakened.
- `INV-REF-006`: Publication to the CFA MFT boundary must not be represented as ACH Colombia pickup, receipt or acceptance without evidence.
- `INV-REF-007`: External production infrastructure, real financial networks and real secrets remain prohibited during development/refactor validation.
- `INV-REF-008`: EF Code First remains schema authority; structural refactoring alone must not create schema migrations.
- `INV-REF-009`: A phase cannot be `VERIFIED` without scope-appropriate executable evidence on the current tree/commit.
- `INV-REF-010`: If the host lacks .NET/Docker/frontend dependencies needed for a gate, use `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`; never infer success.
- `INV-REF-011`: Existing accepted R10/DEV14 and Return/ROR decisions remain closed and are not rederived by this program.
- `INV-REF-012`: Operational dates continue to use configured operational timezone, never UTC date by default.
- `INV-REF-013`: In LOCAL_ADVANCEMENT mode, missing Git/.NET/Docker does not block the next implementation increment after the current increment is structurally implemented; it never promotes a phase to VERIFIED/CLOSED.

## Phase state

| Phase | State | Scope | Evidence | Notes |
|---|---|---|---|---|
| R0 | IMPLEMENTED_UNVERIFIED_ENVIRONMENT | baseline + architecture ratchet + characterization | EVID-REF-R0-001..004, EVID-REF-R0-LOCAL-001 | .NET/Docker and Git metadata unavailable; local advancement permits later implementation but R0 is not VERIFIED/CLOSED |
| R1 | IMPLEMENTED_UNVERIFIED_ENVIRONMENT | clean Application boundary | EVID-REF-R1-001..005 | R1 acceptance structure implemented; executable build/tests unavailable |
| R2 | IMPLEMENTED_UNVERIFIED_ENVIRONMENT | DI/configuration/fail-fast | EVID-REF-R2-001..005 | R2 acceptance structure implemented; executable build/tests unavailable |
| R3 | IMPLEMENTED_UNVERIFIED_ENVIRONMENT | external resilience + secure logging | EVID-REF-R3-001..006 | R3 acceptance structure implemented; executable build/tests unavailable |
| R4A | IMPLEMENTED_UNVERIFIED_ENVIRONMENT | low-risk deterministic extraction from NachaFileBuilder | EVID-REF-R4A-001..006 | facade/output invariant structurally preserved; executable build/tests unavailable |
| R4B | IMPLEMENTED_UNVERIFIED_ENVIRONMENT | record rendering decomposition | EVID-REF-R4B-001..006 | official table-driven record rendering extracted; facade/assembly authority preserved; executable build/tests unavailable |
| R4C | IMPLEMENTED_UNVERIFIED_ENVIRONMENT | generation assembly pipeline | EVID-REF-R4C-001..006 | deterministic official assembly extracted; policy/PUBLISH/totals authority remains in facade; executable build/tests unavailable |
| R4D | IMPLEMENTED_UNVERIFIED_ENVIRONMENT | chamber/flow policies | EVID-REF-R4D-001..006 | official/ReturnOut chamber-flow decisions encapsulated by code/profile without DB-ID switch; executable build/tests unavailable |
| R4E | IMPLEMENTED_UNVERIFIED_ENVIRONMENT | facade thinning + remove remaining runtime fallbacks | EVID-REF-R4E-001..006 | two explicit CENIT fallbacks plus one Type7 display-name partial-match removed from builder generation scope; facade thinned; executable build/tests unavailable |
| R5A | IMPLEMENTED_UNVERIFIED_ENVIRONMENT | deterministic physical parser extraction | EVID-REF-R5A-001..006 | byte/framing + fixed-width tokenization moved to DB-free component; executable build/tests unavailable |
| R5B | IMPLEMENTED_UNVERIFIED_ENVIRONMENT | inbound validation layers | EVID-REF-R5B-001..006 | generic/PUBLISH, CENIT, ACH Colombia and read-only DB validation extracted; executable build/tests unavailable |
| R5C | IMPLEMENTED_UNVERIFIED_ENVIRONMENT | inbound persistence stage | EVID-REF-R5C-001..006 | parsed/validated graph write isolated; transaction/lineage/idempotency semantics preserved; executable build/tests unavailable |
| R5D | IMPLEMENTED_UNVERIFIED_ENVIRONMENT | inbound post-processing stage pipeline | EVID-REF-R5D-001..006 | seven explicit stages; ordering/idempotency/transactions preserved; executable build/tests unavailable |
| R6A | IMPLEMENTED_UNVERIFIED_ENVIRONMENT | MFT interface segregation | EVID-REF-R6A-001..004 | five capability contracts + compatibility facade; implementation/lifecycle unchanged; executable build/tests unavailable |
| R6B | IMPLEMENTED_UNVERIFIED_ENVIRONMENT | MFT implementation split | EVID-REF-R6B-001..005 | seven cohesive implementation owners + compatibility facade; lifecycle/retry/session semantics unchanged; executable build/tests unavailable |
| R6C | IMPLEMENTED_UNVERIFIED_ENVIRONMENT | incoming command center query/operations separation | EVID-REF-R6C-001..004 | 12 query + 4 manual-dispatch capability methods; routes/permissions unchanged; executable build/tests unavailable |
| R7 | IMPLEMENTED_UNVERIFIED_ENVIRONMENT | Returns/ROR SRP refactor | EVID-REF-R7-001..004 | seven internal owners; accepted policies preserved; executable build/tests unavailable |
| R8A | IMPLEMENTED_UNVERIFIED_ENVIRONMENT | ReportsController/API modularization | EVID-REF-R8A-001..009 | 20 actions preserved across seven capability controllers; executable build/tests unavailable |
| R8B | IMPLEMENTED_UNVERIFIED_ENVIRONMENT | selected CertificateManagementServices source-cohesion cleanup | EVID-REF-R8B-001..007 | 10 existing service owners split into dedicated compilation units; executable build/tests unavailable |
| R9 | IMPLEMENTED_UNVERIFIED_ENVIRONMENT | deterministic operational time + async consistency | EVID-REF-R9-001..008 | selected time behavior deterministic; executable .NET evidence unavailable |
| R10 | IMPLEMENTED_UNVERIFIED_ENVIRONMENT | final internal recertification + evidence consolidation | EVID-REF-R10-001..006 | static recertification complete; required executable/commit evidence unavailable, so no CLOSED promotion |

## Finding state

### P0

- `ARCH-001`: IMPLEMENTED_UNVERIFIED_ENVIRONMENT — R1 removed the targeted ASP.NET/NLog/WCF package references from Application; executable recertification is pending.
- `ARCH-002`: IMPLEMENTED_UNVERIFIED_ENVIRONMENT — R1 moved HTTP middleware from Application to Api; executable recertification is pending.
- `ARCH-003`: IMPLEMENTED_UNVERIFIED_ENVIRONMENT — R1 moved MVC/filter/HTTP exception concerns from Application to Api; executable recertification is pending.
- `ARCH-004`: IMPLEMENTED_UNVERIFIED_ENVIRONMENT — R1 moved the WCF generated/transport concern from Application to External; executable recertification is pending.
- `ARCH-005`: IMPLEMENTED_UNVERIFIED_ENVIRONMENT — R1 moved X509Store access from Application to External behind `IRsaKeyProvider`; executable recertification is pending.
- `ARCH-006`: IMPLEMENTED_UNVERIFIED_ENVIRONMENT — R1 moved the concrete NLog adapter from Application to Api and retained the Application logging port; executable recertification is pending.
- `ARCH-009`: IMPLEMENTED_UNVERIFIED_ENVIRONMENT — blocking source/package/project architecture ratchets were added and the non-enforcing skipped SOAP diagnostic was removed; .NET execution is pending.
- `SRP-001`: IN_PROGRESS — R4A extracted deterministic field-resolution/legacy-formatting and blocking/padding; R4B extracted official table-driven fixed-width field rendering/rules/trace; R4C extracted deterministic official file assembly/order; R4D extracted official/ReturnOut ACH Colombia/CENIT chamber-flow decisions into internal code/profile-resolved policies. `NachaFileBuilder` remains the 6/6 facade and is 98 lines after R4E, with the existing implementation moved to internal `NachaFileGenerationCoordinator`; executable verification is pending.
- `SRP-002`: OPEN — `NachaFileBuilder` has excessive collaborator count at baseline.
- `DIP-001`: OPEN — R2 removed targeted hidden runtime-service fallbacks from Api/Application/External, but 57 pre-existing Persistence fallback occurrences remain ratcheted for later focused phases.
- `SRP-003`: IMPLEMENTED_UNVERIFIED_ENVIRONMENT — R5A-R5D now separate physical parsing, validation, parsed-graph persistence and post-processing boundaries. R5D does not reopen the direct-parser Return compatibility path; executable verification remains pending.
- `SRP-005`: IMPLEMENTED_UNVERIFIED_ENVIRONMENT — R5D reduces `IncomingNachaPostProcessingOrchestrator` from 1179 to 79 lines and delegates to seven explicit post-processing stages; executable verification remains pending.
- `RES-001`: IMPLEMENTED_UNVERIFIED_ENVIRONMENT — R3 removed all production direct `new HttpClient()` construction and moved targeted HTTP/SOAP transport ownership to named `IHttpClientFactory` clients; executable recertification is pending.
- `RES-002`: IMPLEMENTED_UNVERIFIED_ENVIRONMENT — R3 moved `HttpClientService` to a named factory client with legacy-equivalent 100s timeout and no transport retry because POST idempotency is not guaranteed; executable recertification is pending.
- `SEC-001`: IMPLEMENTED_UNVERIFIED_ENVIRONMENT — R3 removed the targeted curl/header/body, WCF payload, health/server serialization, reset-link and token/assertion secret logging paths and added a secure-logging ratchet; broader OBS-001 crash-file logging remains separate/open.
- `GOV-001`: IMPLEMENTED_UNVERIFIED_ENVIRONMENT — R0 added a shrinking legacy allowlist and R1 reduced its targeted Application boundary source/package exceptions to zero; executable proof is pending.
- `GOV-002`: IMPLEMENTED_UNVERIFIED_ENVIRONMENT — existing critical characterization coverage was inventoried and SHA-256 byte-stability guards were added for six golden files plus the CENIT ROR synthetic fixture; executable proof is pending.
- `GOV-003`: OPEN — dual SQL Server/PostgreSQL evidence must remain for affected persistence paths.
- `GOV-004`: OPEN — cluster/MFT/Quartz no-duplicate and recovery semantics must be preserved.

### P1

- `ARCH-007`: IMPLEMENTED_UNVERIFIED_ENVIRONMENT — the known menu-query HttpContext leakage was replaced by `ICurrentUserContext`; executable recertification is pending.
- `ARCH-008`: IMPLEMENTED_UNVERIFIED_ENVIRONMENT — R2 removed the known direct API construction of the Persistence operational-time provider and made it constructor-DI mandatory; executable recertification is pending.
- `ARCH-010`: IMPLEMENTED_UNVERIFIED_ENVIRONMENT — R2 removed all production `AppSettings.Settings` usages and replaced them with cohesive options; executable recertification is pending.
- `OCP-001`: IMPLEMENTED_UNVERIFIED_ENVIRONMENT — R4D encapsulated ACH Colombia/CENIT flow decisions and R4E removed the two explicitly deferred CENIT fallbacks plus one Type7 display-name partial-match from the builder generation scope; outbound rail resolution/opt-in now uses canonical/configured code plus exact code comparison and the R4D policy resolver, with DB-ID inference at zero. Executable verification is pending.
- `SRP-004`: OPEN — parsing and persistence concerns require separation.
- `SRP-006`: OPEN — managed file exchange service mixes execution/admin/query/credential concerns (~996 lines baseline).
- `ISP-001`: OPEN — managed file exchange contract is broad.
- `SRP-007`: OPEN — incoming command center service is broad (~1502 lines baseline).
- `ISP-002`: OPEN — command center query/actions share a broad interface.
- `SRP-008`: OPEN — returns service is broad (~1066 lines baseline).
- `DIP-002`: OPEN — returns/runtime services use concrete fallback construction where found.
- `SRP-009`: OPEN — ReportsController is broad (~1028 lines baseline).
- `DIP-003`: OPEN — R2 eliminated critical-boundary hidden construction and introduced a shrinking Persistence fallback ratchet; remaining Persistence fallback debt is intentionally deferred to later focused phases.
- `OCP-002`: OPEN — legacy clearing-house strategy factory/switch must be removed or DI-resolved if still active.
- `TEST-001`: OPEN — static clock usage is widespread; only behavior-sensitive routes are in refactor scope.
- `TEST-002`: IMPLEMENTED_UNVERIFIED_ENVIRONMENT — selected MFT inbound recovery timing now uses injected `TimeProvider` with fake-time boundary coverage; .NET execution remains unavailable.
- `RES-003`: IMPLEMENTED_UNVERIFIED_ENVIRONMENT — R3 moved health transport behind `IHealthProbeClient`, uses the explicit idempotent health named client, and restricts bounded transport retry/circuit/concurrency policy to GET health probing.
- `RES-004`: IMPLEMENTED_UNVERIFIED_ENVIRONMENT — R3 preserves explicit WCF binding timeouts, classifies failures for diagnostics, safely closes/aborts clients and adds no retry because authorization operations are side-effecting or not proven idempotent; executable recertification is pending.
- `ASYNC-001`: OPEN — selected R9 operational routes have zero true sync-over-async, but the four pre-existing repository-wide `.GetAwaiter().GetResult()` sites outside the selected R9 execution paths remain out of scope.
- `SEC-002`: IMPLEMENTED_UNVERIFIED_ENVIRONMENT — R3 target integration request/response logs now emit sanitized endpoint/operation/status/elapsed/exception metadata only; payloads, credentials, bearer/API keys and sensitive external response bodies are not logged.
- `OBS-001`: OPEN — direct crash-log file write exists in middleware at baseline.
- `CFG-001`: IMPLEMENTED_UNVERIFIED_ENVIRONMENT — R2 made targeted critical collaborators mandatory and added cohesive option binding plus fail-fast JWT/token and required server-list startup validation; executable recertification is pending.
- `MOD-001`: OPEN — ACH Colombia/CENIT decisions remain dispersed in some central services.

### P2

- `MOD-002`: OPEN — large official-profile seeder cleanup candidate.
- `MOD-003`: OPEN — certificate management service cleanup candidate.
- `MOD-004`: OPEN — command-center DTO organization cleanup candidate.

## Decisions for this refactor

- `DEC-REF-001`: This program is incremental refactoring, not a rewrite.
- `DEC-REF-002`: The existing PaymentRail blueprint is not automatically in scope. No new rail abstraction is introduced unless a later explicit phase/decision authorizes it.
- `DEC-REF-003`: Architecture tests use a shrinking legacy allowlist (ratchet) until a boundary is fully clean, then the allowlist entry is removed permanently.
- `DEC-REF-004`: `NachaFileBuilder` remains a compatibility facade during extraction; public callers are not migrated en masse in the first NACHA subphase.
- `DEC-REF-005`: NACHA output equivalence is byte-for-byte for characterized official scenarios unless the source contains intentionally variable fields explicitly normalized by the existing tests.
- `DEC-REF-006`: Resilience policies are operation-specific. Retrying is not mandatory; non-idempotent operations must fail safely rather than receive a blind retry policy.
- `DEC-REF-007`: File/class size metrics are indicators, not acceptance rules. Cohesion, dependency direction, testability and behavior preservation govern closure.
- `DEC-REF-008`: No schema migration is expected from this refactor unless a separately justified design change requires persistence.

## Known refactor traps

- `TRAP-REF-001`: Do not “fix” a golden mismatch by updating the golden file without a normative change.
- `TRAP-REF-002`: Do not replace one god class with one god facade that simply delegates to arbitrary helpers without coherent ownership.
- `TRAP-REF-003`: Do not introduce interfaces for pure local helpers/value operations solely to reduce constructor code.
- `TRAP-REF-004`: Do not apply a generic retry policy to all SOAP/WCF calls; determine idempotency first.
- `TRAP-REF-005`: Do not move infrastructure types into Domain merely to remove them from Application.
- `TRAP-REF-006`: Do not convert every `DateTime.UtcNow` mechanically; only behavior-sensitive operational time is a priority.
- `TRAP-REF-007`: Do not implement the PaymentRail blueprint as an incidental byproduct of decomposing ACH/CENIT code.
- `TRAP-REF-008`: Do not merge ACH Colombia and CENIT policies into a generic abstraction that erases regulatory differences.
- `TRAP-REF-009`: Do not claim global SOLID/Clean Architecture compliance from static scans alone.
- `TRAP-REF-010`: Do not mark a phase CLOSED on a different commit than the one that produced the evidence.

## Evidence log

Use one row per verified evidence set.

| Evidence ID | Phase | SHA | Command/Test | Result | Date | Notes |
|---|---|---|---|---|---|---|
| EVID-REF-000 | baseline | unknown | static package audit | observed | 2026-10-10 | source package baseline; executable recertification still required |
| EVID-REF-R0-001 | R0 | unavailable | `python scripts/validate-solution-projects.py` | PASS | 2026-10-10 | 7 solution projects, 7 disk projects, all project-reference targets loaded |
| EVID-REF-R0-002 | R0 | unavailable | static mirror of architecture ratchet + SHA-256 fixture checks | PASS_STATIC_ONLY | 2026-10-10 | current allowlists/packages/project references and six goldens + CENIT ROR fixture match the documented baseline |
| EVID-REF-R0-003 | R0 | unavailable | `dotnet --info`; `docker --version` | TOOLING_UNAVAILABLE | 2026-10-10 | both commands exit 127; build/tests/runtime gates not executable |
| EVID-REF-R0-004 | R0 | unavailable | `git status --short`; `git rev-parse HEAD` | GIT_METADATA_UNAVAILABLE | 2026-10-10 | both commands exit 128 because the delivered source archive contains no `.git`; accepted historical commit is not promoted to current SHA |
| EVID-REF-R0-LOCAL-001 | R0 | unavailable | local advancement decision | IMPLEMENTED_UNVERIFIED_ENVIRONMENT | 2026-10-10 | R0 is not retried/closed on this host; missing infrastructure is recorded but does not block implementing R1 |
| EVID-REF-R1-001 | R1 | unavailable | `python scripts/validate-solution-projects.py` before and after R1 | PASS | 2026-10-10 | 7 solution projects, 7 disk projects, all project-reference targets loaded |
| EVID-REF-R1-002 | R1 | unavailable | static Application boundary scan + architecture-ratchet inspection | PASS_STATIC_ONLY | 2026-10-10 | Application ASP.NET/WCF/NLog/X509 violations = 0; boundary package allowlist = 0; legacy manual HttpClient allowlist remains one out-of-scope file |
| EVID-REF-R1-003 | R1 | unavailable | `python scripts/scan_result_solid.py` | FAIL_EXISTING_PROGRAM_BACKLOG | 2026-10-10 | broad Result/SRP scan reports existing program-wide findings outside R1; no cleanup was attempted outside phase scope |
| EVID-REF-R1-004 | R1 | unavailable | .NET build/tests and Git SHA controls | NOT_RUN_ENVIRONMENT | 2026-10-10 | `dotnet` unavailable and archive has no `.git`; R1 cannot be VERIFIED/CLOSED |
| EVID-REF-R1-005 | R1 | unavailable | pristine-tree diff for protected artifacts | PASS_STATIC_ONLY | 2026-10-10 | NACHA golden files, golden fingerprint test, normative docs, SQL Server/PostgreSQL migrations and SPA are byte-unchanged by R1 |
| EVID-REF-R2-001 | R2 | unavailable | `python scripts/validate-solution-projects.py` after R2 | PASS | 2026-10-10 | 7 solution projects, 7 disk projects, all project-reference targets loaded |
| EVID-REF-R2-002 | R2 | unavailable | `python scripts/validate-r2-di-configuration.py` + R1/R2 static metric comparison | PASS_STATIC_ONLY | 2026-10-10 | production AppSettings.Settings 15->0; critical Api/Application/External service fallbacks 3->0; direct LoggerManager 1->0; hidden SFTP factory 2->0; API operational-time provider construction 1->0; Persistence fallback debt remains 57/57 |
| EVID-REF-R2-003 | R2 | unavailable | `python scripts/scan_result_solid.py` compared with captured R1 pre-R2 output | FAIL_EXISTING_PROGRAM_BACKLOG_UNCHANGED | 2026-10-10 | command still exits 1 for broad existing backlog; pre/post output is byte-identical |
| EVID-REF-R2-004 | R2 | unavailable | pristine-tree protected-artifact comparison | PASS_STATIC_ONLY | 2026-10-10 | NACHA golden/fingerprint assets, `docs/normativa`, SQL Server/PostgreSQL migrations and SPA are byte-unchanged by R2 |
| EVID-REF-R2-005 | R2 | unavailable | Git/.NET/Docker executable gates | NOT_RUN_ENVIRONMENT | 2026-10-10 | archive has no `.git`; `dotnet` and Docker are unavailable, so R2 cannot be VERIFIED/CLOSED |
| EVID-REF-R3-001 | R3 | unavailable | `python scripts/validate-solution-projects.py`; `python scripts/validate-r2-di-configuration.py`; `python scripts/validate-r3-external-resilience.py` | PASS / PASS_STATIC_ONLY | 2026-10-10 | solution graph and R2 ratchet remain valid; R3 static ratchet reports zero direct HttpClient construction, zero raw action injection, required named clients, no banned target logging and operation-specific retry settings |
| EVID-REF-R3-002 | R3 | unavailable | R2/R3 static transport metric comparison | PASS_STATIC_ONLY | 2026-10-10 | product direct `new HttpClient` 3->0; raw API default HttpClient action injection 1->0; old global WSCFAACH retry pipeline removed; health GET is the only transport-retried profile |
| EVID-REF-R3-003 | R3 | unavailable | R3 fault-injection/characterization source inspection | PASS_STATIC_ONLY | 2026-10-10 | added timeout/connection/5xx/circuit/retry-exhaustion/cancellation/no-retry tests; existing notification timeout characterization preserves manual-review uncertain-result ownership |
| EVID-REF-R3-004 | R3 | unavailable | `python scripts/scan_result_solid.py` compared with captured pre-R3 R2 output | FAIL_EXISTING_PROGRAM_BACKLOG_UNCHANGED | 2026-10-10 | command exits 1 for the existing program backlog; pre/post output is byte-identical |
| EVID-REF-R3-005 | R3 | unavailable | R2/R3 protected-artifact SHA-256 comparison | PASS_STATIC_ONLY | 2026-10-10 | `TestData`, NACHA golden fingerprint test, `docs/normativa`, PostgreSQL/SQL Server migration trees and SPA are byte-unchanged by R3 |
| EVID-REF-R3-006 | R3 | unavailable | Git/.NET/Docker executable gates | NOT_RUN_ENVIRONMENT | 2026-10-10 | archive has no `.git`; `dotnet` and Docker are unavailable, so build/tests cannot verify or close R3 |
| EVID-REF-R4A-001 | R4A | unavailable | `python scripts/validate-solution-projects.py`; R2/R3 ratchets; `python scripts/validate-r4a-nacha-builder-facade.py` | PASS / PASS_STATIC_ONLY | 2026-10-10 | solution graph and prior ratchets remain valid; R4A facade 6/6 methods unchanged, builder 4798->4575 lines, two deterministic components extracted, R4B contracts=0, R4A-scope PaymentRail coupling=0 |
| EVID-REF-R4A-002 | R4A | unavailable | focused R4A test-source inspection | PASS_STATIC_ONLY | 2026-10-10 | deterministic identifier/value resolution, legacy formatting, block/padding arithmetic and invalid blocking-factor coverage added without modifying expected outputs |
| EVID-REF-R4A-003 | R4A | unavailable | `python scripts/scan_result_solid.py` before/after R4A | FAIL_EXISTING_PROGRAM_BACKLOG_UNCHANGED | 2026-10-10 | command exits 1 for existing broad backlog; output remains byte-identical with SHA-256 e43aacb21b2b84e4f93793bfa1ced9805d16f644fc061fadeb7705e7cf802ad5 |
| EVID-REF-R4A-004 | R4A | unavailable | R3/R4A protected-artifact SHA-256 comparison | PASS_STATIC_ONLY | 2026-10-10 | `tests/**/TestData` 15/15, `docs/normativa` 12/12, Persistence migrations 92/92, SQL Server migration project 79/79, SPA/docs-spa 776/776 and golden fingerprint characterization are byte-unchanged |
| EVID-REF-R4A-005 | R4A | unavailable | scope/impact inspection | PASS_STATIC_ONLY | 2026-10-10 | INachaFileBuilder/API/MFT/Returns/ROR/jobs/PUBLISH/hash/audit/idempotency contracts not changed; helpers contain no chamber policy, IO, DB or PaymentRailOperationalStrategy dependency |
| EVID-REF-R4A-006 | R4A | unavailable | Git/.NET/Docker executable gates | NOT_RUN_ENVIRONMENT | 2026-10-10 | archive has no `.git`; `dotnet` and Docker are unavailable, so Release build, focused/golden/backend tests and runtime gates cannot verify or close R4A |
| EVID-REF-R4B-001 | R4B | unavailable | `python scripts/validate-solution-projects.py`; R2/R3/R4A ratchets; `python scripts/validate-r4b-nacha-record-renderers.py` | PASS / PASS_STATIC_ONLY | 2026-10-10 | solution/prior ratchets remain valid; facade 6/6, builder 4575->3796 lines, implementation-root renderer files 2->3, internal renderer interfaces 0->1, R4B-scope PaymentRail coupling=0 |
| EVID-REF-R4B-002 | R4B | unavailable | focused R4B source characterization + renderer ownership inspection | PASS_STATIC_ONLY | 2026-10-10 | exact fixed-width order/width/padding and deterministic-repeat test source added; renderer owns official field resolution/rules/formatting/trace while assembly, totals, semantic validation and audit remain in the facade |
| EVID-REF-R4B-003 | R4B | unavailable | `python scripts/scan_result_solid.py` compared with R4A captured output | FAIL_EXISTING_PROGRAM_BACKLOG_UNCHANGED | 2026-10-10 | command exits 1 for existing broad backlog; output remains byte-identical with SHA-256 e43aacb21b2b84e4f93793bfa1ced9805d16f644fc061fadeb7705e7cf802ad5 |
| EVID-REF-R4B-004 | R4B | unavailable | R4A/R4B protected-artifact SHA-256 comparison | PASS_STATIC_ONLY | 2026-10-10 | TestData/goldens/fingerprint/CENIT ROR, `docs/normativa`, Persistence migrations, SQL Server migration project and SPA/docs-spa are byte-unchanged |
| EVID-REF-R4B-005 | R4B | unavailable | scope/impact inspection | PASS_STATIC_ONLY | 2026-10-10 | public builder contract/callers/DI, PUBLISH selection, control totals, assembly order, hash/audit/idempotency, Returns/ROR and MFT semantics remain owned where they were; renderer has no IO/DB/global-config/rail-policy dependency and no R4C pipeline was introduced |
| EVID-REF-R4B-006 | R4B | unavailable | Git/.NET/Docker executable gates | NOT_RUN_ENVIRONMENT | 2026-10-10 | archive has no `.git`; `dotnet` and Docker are unavailable, so Release build, focused renderer tests, golden/fingerprint/CENIT ROR tests and broad backend suite cannot verify or close R4B |
| EVID-REF-R4C-001 | R4C | unavailable | `python scripts/validate-solution-projects.py`; R2/R3/R4A/R4B ratchets; `python scripts/validate-r4c-nacha-assembly-pipeline.py` | PASS / PASS_STATIC_ONLY | 2026-10-10 | solution/prior ratchets remain valid; facade 6/6, builder 3796->3693 lines, deterministic components 3->4, renderer files 3->3, assembly components 0->1, new R4C internal interfaces=0 |
| EVID-REF-R4C-002 | R4C | unavailable | focused R4C source characterization + ownership inspection | PASS_STATIC_ONLY | 2026-10-10 | assembly session owns T1/T5/T6/T7/T8/T9 composition/state, structural T6/T7 grouping, rendered T8/T9 control verification and padding; exact policy/data preparation stays in facade |
| EVID-REF-R4C-003 | R4C | unavailable | `python scripts/scan_result_solid.py` compared with R4B captured output | FAIL_EXISTING_PROGRAM_BACKLOG_UNCHANGED | 2026-10-10 | command exits 1 for existing broad backlog; final output remains byte-identical to R4B baseline with SHA-256 e43aacb21b2b84e4f93793bfa1ced9805d16f644fc061fadeb7705e7cf802ad5 |
| EVID-REF-R4C-004 | R4C | unavailable | R4B/R4C protected-artifact SHA-256 comparison | PASS_STATIC_ONLY | 2026-10-10 | TestData/six goldens/fingerprint/CENIT ROR, `docs/normativa`, Persistence migrations, SQL Server migration project, SPA/docs-spa and MFT/SFTP candidates are byte-unchanged |
| EVID-REF-R4C-005 | R4C | unavailable | scope/impact inspection | PASS_STATIC_ONLY | 2026-10-10 | PUBLISH snapshot/profile selection, ACH Colombia/CENIT policy, authoritative control-total calculation, semantic validation, SHA-256, audit persistence, idempotency, Returns/ROR and MFT remain with prior owners; pipeline has no IO/DB/global-config/rail-policy/hash/persistence dependency |
| EVID-REF-R4C-006 | R4C | unavailable | Git/.NET/Docker executable gates | NOT_RUN_ENVIRONMENT | 2026-10-10 | archive has no `.git`; `dotnet` and Docker are unavailable, so Release build, focused assembly tests, deterministic NACHA/golden/fingerprint/CENIT ROR tests and broad backend suite cannot verify or close R4C |

| EVID-REF-R4D-001 | R4D | unavailable | `python scripts/validate-solution-projects.py`; R2/R3/R4A/R4B/R4C ratchets; `python scripts/validate-r4d-chamber-flow-policies.py` | PASS / PASS_STATIC_ONLY | 2026-10-10 | facade 6/6; builder 3693->3349 lines; deterministic formatter/blocking/renderer/assembly 4->4; renderers 3->3; assembly 1->1; policy files 0->1; internal policy interfaces 0->1; policy implementations 3; forbidden policy dependencies and PaymentRail references=0 |
| EVID-REF-R4D-002 | R4D | unavailable | focused R4D policy characterization + ownership inspection | PASS_STATIC_ONLY | 2026-10-10 | ACH/CENIT ReturnOut/ROR, ordinary CTX/Type7, render-context and gate decisions are code/profile-resolved in separate policies; PUBLISH/profile, transaction/batch preparation, totals, rendering, assembly, semantic validation, SHA and audit/persistence retain prior owners |
| EVID-REF-R4D-003 | R4D | unavailable | `python scripts/scan_result_solid.py` compared before/after R4D | FAIL_EXISTING_PROGRAM_BACKLOG_UNCHANGED | 2026-10-10 | command exits 1 for existing broad backlog; captured output is byte-identical before/after R4D with SHA-256 343240794946d5b8cb48bce2209baa37e755ab1bb443012f9cef5ef73d038db6 |
| EVID-REF-R4D-004 | R4D | unavailable | R4C/R4D protected-artifact SHA-256 comparison | PASS_STATIC_ONLY | 2026-10-10 | all 15 TestData files, six goldens, CENIT ROR fixture, fingerprint characterization, `docs/normativa`, PostgreSQL/Persistence migrations, SQL Server migrations project, SPA/docs-spa and MFT/SFTP-related source/configuration are byte-unchanged |
| EVID-REF-R4D-005 | R4D | unavailable | scope/impact inspection | PASS_STATIC_ONLY | 2026-10-10 | no public/API/DI/schema change; official assembly remains rail-agnostic; no DB-ID switch or PaymentRail; two pre-existing legacy name-based fallbacks remain intentionally for R4E and no new allowlist/exception was added |
| EVID-REF-R4D-006 | R4D | unavailable | Git/.NET/Docker executable gates | NOT_RUN_ENVIRONMENT | 2026-10-10 | archive has no `.git`; `dotnet` and Docker are unavailable, so Release build, focused policy tests, deterministic/golden/fingerprint/CENIT ROR tests and broad backend suite cannot verify or close R4D |
| EVID-REF-R4E-001 | R4E | unavailable | R1-R4D ratchets + `python scripts/validate-r4e-chamber-resolution.py` | PASS / PASS_STATIC_ONLY | 2026-10-10 | facade remains 6/6; builder 3349->98 lines; exact `ClearingHouse.Name.Contains("CENIT")` outbound fallbacks 2->0; additional equivalent outbound name heuristics 1->0; DB-ID rail inference 0->0; PaymentRail references=0 |
| EVID-REF-R4E-002 | R4E | unavailable | source-move/facade-thinning comparison | PASS_STATIC_ONLY | 2026-10-10 | pre-R4E builder generation body moved to internal `NachaFileGenerationCoordinator`; generation-body diff is limited to the authorized canonical chamber-resolution replacements, the Type7 clearing-house opt-in exact-code replacement, and required parameter plumbing |
| EVID-REF-R4E-003 | R4E | unavailable | focused chamber-resolution characterization source + ownership inspection | PASS_STATIC_ONLY | 2026-10-10 | misleading display name `ACH CENIT Legacy Label` resolves as ACH through exact configured catalog mapping; PUBLISH/profile, totals, rendering, assembly, semantic validation, SHA and audit/persistence semantics are unchanged; ReturnOut/ROR stays separate |
| EVID-REF-R4E-004 | R4E | unavailable | `python scripts/scan_result_solid.py` compared before/after R4E | FAIL_EXISTING_PROGRAM_BACKLOG_UNCHANGED | 2026-10-10 | command exits 1 for existing broad backlog; captured output is byte-identical before/after R4E with SHA-256 343240794946d5b8cb48bce2209baa37e755ab1bb443012f9cef5ef73d038db6 |
| EVID-REF-R4E-005 | R4E | unavailable | R4D/R4E protected-artifact comparison | PASS_STATIC_ONLY | 2026-10-10 | TestData/six goldens/fingerprints/CENIT ROR, `docs/normativa`, PostgreSQL/Persistence migrations, SQL Server migration project, SPA/docs-spa and MFT/SFTP paths are byte-unchanged; no schema/normative/golden update |
| EVID-REF-R4E-006 | R4E | unavailable | Git/.NET/Docker executable gates | NOT_RUN_ENVIRONMENT | 2026-10-10 | archive has no `.git`; `dotnet` and Docker are unavailable, so Release build, focused R4E/R4D/R4A-R4C tests, executable golden/fingerprint/CENIT ROR characterization and broad backend suite cannot verify or close R4E |
| EVID-REF-R5A-001 | R5A | unavailable | R1-R4E ratchets + `python scripts/validate-r5a-inbound-architecture.py` | PASS / PASS_STATIC_ONLY | 2026-10-10 | physical parsing owner coupled to AchDbContext 1->0; DB-free deterministic physical parser components 0->1; direct physical framing implementation in `NachaParserService` 1->0; new public interfaces=0; PaymentRail references=0 |
| EVID-REF-R5A-002 | R5A | unavailable | R4E/R5A source ownership diff | PASS_STATIC_ONLY | 2026-10-10 | byte/framing checks and fixed-width tokenization moved unchanged to internal `NachaPhysicalParser`; `INachaParserService`, profile/PUBLISH resolution, validation, persistence, Return/ROR interpretation and post-processing retain prior owners |
| EVID-REF-R5A-003 | R5A | unavailable | focused physical parser characterization source | PASS_STATIC_ONLY | 2026-10-10 | DB-independent ACH happy path, invalid physical structure, Latin-1 repertoire and CENIT ROR physical-order/addenda characterization added without changing expected inputs/outputs or fixtures |
| EVID-REF-R5A-004 | R5A | unavailable | `python scripts/scan_result_solid.py` compared before/after R5A | FAIL | 2026-10-10 | command exits 1 for existing broad backlog; locally captured R4E input and R5A output are byte-identical with SHA-256 b45d83724c3b6bd0a13014de7bf890f8b281e8ad24d9f97fdd40dde799f93b12 |
| EVID-REF-R5A-005 | R5A | unavailable | R4E/R5A protected-artifact comparison | PASS_STATIC_ONLY | 2026-10-10 | all 15 TestData files, six goldens, CENIT ROR fixture, fingerprints, `docs/normativa`, PostgreSQL/Persistence migrations, SQL Server migration project, SPA/docs-spa, MFT/SFTP and unrelated outbound R4A-R4E source are byte-unchanged; no migration/normative/golden update |
| EVID-REF-R5A-006 | R5A | unavailable | Git/.NET/Docker executable gates | NOT_RUN_ENVIRONMENT | 2026-10-10 | archive has no `.git`; `dotnet` and Docker are unavailable, so Release build, focused R5A/inbound/golden/fingerprint/CENIT ROR tests and broad backend/runtime suites cannot verify or close R5A |
| EVID-REF-R5B-001 | R5B | unavailable | R1-R5A ratchets + `python scripts/validate-r5b-inbound-validation.py` | PASS / PASS_STATIC_ONLY | 2026-10-10 | parser lines 2180->1077; parser-owned validation method definitions 20->1 compatibility delegate; physical parser 1->1 with R5A source hash unchanged; extracted validation components 0->4 and DB-free validation components 0->3 |
| EVID-REF-R5B-002 | R5B | unavailable | ownership/source inspection | PASS_STATIC_ONLY | 2026-10-10 | selected PUBLISH/inter-record/regulatory validation moved to `NachaInboundValidator`; CENIT and ACH Colombia rules are separate; persistence-dependent checks use explicit `NachaInboundDatabaseValidator` read-only queries; persistence/post-processing/idempotency/MFT owners are unchanged |
| EVID-REF-R5B-003 | R5B | unavailable | inbound chamber heuristic ratchet | PASS_STATIC_ONLY | 2026-10-10 | `NachaConfigValidationService Contains("CENIT")` 2->0 via exact canonical code equality; simulation 7->7 and ingestion 1->1 remain out of scope; total tracked debt 10->8; DB-ID rail inference 0->0; PaymentRail=0; Persistence fallback ratchet 57/57 |
| EVID-REF-R5B-004 | R5B | unavailable | focused validation characterization source | PASS_STATIC_ONLY | 2026-10-10 | source characterization covers duplicate sequence D04, CENIT T7 owner association, CENIT PPD/CCD-vs-CTX separation and valid Return Type99 reason/original trace; fixtures/expected results are unchanged |
| EVID-REF-R5B-005 | R5B | unavailable | `scan_result_solid.py` + R5A/R5B protected-artifact comparison | FAIL / PASS_STATIC_ONLY | 2026-10-10 | SOLID scan exits 1 for existing backlog and its captured output is byte-identical to R5A with SHA-256 b45d83724c3b6bd0a13014de7bf890f8b281e8ad24d9f97fdd40dde799f93b12; protected TestData/goldens/fingerprint/CENIT ROR/normative/schema/UI/MFT/outbound artifacts are unchanged |
| EVID-REF-R5B-006 | R5B | unavailable | Git/.NET/Docker executable gates | NOT_RUN_ENVIRONMENT | 2026-10-10 | archive has no `.git`; `dotnet` and Docker are unavailable, so Release build, focused R5B/R5A/inbound/golden/fingerprint/CENIT ROR tests and broad backend/runtime suites cannot verify or close R5B |
| EVID-REF-R5C-001 | R5C | unavailable | R1-R5B ratchets + `python scripts/validate-r5c-inbound-persistence.py` | PASS / PASS_STATIC_ONLY | 2026-10-10 | parser lines 1077->1076; write-owning inbound persistence components 0->1; direct parsed-graph persistence definitions in parser 2->0; parsed-graph SaveChanges owner moves parser 1->0 / coordinator 0->1 with total 1->1 |
| EVID-REF-R5C-002 | R5C | unavailable | transaction/idempotency ownership inspection | PASS_STATIC_ONLY | 2026-10-10 | `IncomingNachaIngestionAppService` remains the sole explicit relational transaction owner 1->1; persistence coordinator begins/commits/rolls back 0; idempotency policy owners remain ingestion fingerprint/reprocess + parser NACHA identity/replay 2->2 |
| EVID-REF-R5C-003 | R5C | unavailable | lineage/Return persistence characterization source | PASS_STATIC_ONLY | 2026-10-10 | focused tests cover exact ingestion membership, parent/child lineage, Type99 Return/ROR field preservation and caller-owned transaction rollback; `ParsedNachaEntityMapper` behavior remains unchanged |
| EVID-REF-R5C-004 | R5C | unavailable | inbound chamber/persistence ratchet | PASS_STATIC_ONLY | 2026-10-10 | physical parser 1->1 and R5B validation components 4->4 unchanged; write side effects in both remain 0; Contains(CENIT) config 0->0 / simulation 7->7 / ingestion 1->1, total 8->8; DB-ID inference 0; PaymentRail=0; Persistence fallback 57/57 |
| EVID-REF-R5C-005 | R5C | unavailable | `scan_result_solid.py` + R5B/R5C protected-artifact comparison | FAIL / PASS_STATIC_ONLY | 2026-10-10 | SOLID scan exits 1 for existing backlog and is byte-identical R5B->R5C with SHA-256 b45d83724c3b6bd0a13014de7bf890f8b281e8ad24d9f97fdd40dde799f93b12; protected TestData/goldens/fingerprint/CENIT ROR/normative/schema/UI/MFT/outbound and R5A/R5B boundary artifacts are unchanged except authorized R5C plumbing/docs/tests/validator |
| EVID-REF-R5C-006 | R5C | unavailable | Git/.NET/Docker executable gates | NOT_RUN_ENVIRONMENT | 2026-10-10 | archive has no `.git`; `dotnet` and Docker are unavailable, so Release build, focused R5C/R5B/R5A, persistence/idempotency/reprocess, inbound/golden/fingerprint/CENIT ROR and broad backend/runtime suites cannot verify or close R5C |
| EVID-REF-R5D-001 | R5D | unavailable | R1-R5C ratchets + `python scripts/validate-r5d-inbound-postprocessing.py` | PASS / PASS_STATIC_ONLY | 2026-10-10 | orchestrator 1179->79 lines; explicit post-processing stages 0->7; NachaParserService 1076->1076; physical parser 1->1, validation components 4->4 and persistence coordinator 1->1 remain intact |
| EVID-REF-R5D-002 | R5D | unavailable | post-processing ordering/ownership inspection | PASS_STATIC_ONLY | 2026-10-10 | waiting-window and final post-processing SaveChanges sites remain 2->2; parsed-graph SaveChanges owner remains R5C total 1->1; ingestion explicit transaction owner remains 1->1; R5D adds no begin/commit/rollback |
| EVID-REF-R5D-003 | R5D | unavailable | downstream/idempotency/recovery inspection | PASS_STATIC_ONLY | 2026-10-10 | direct Proc_Transacciones send site 1->1, new local/blind retry sites=0; prior-success remains no-resend; manual-review/uncertain outcomes remain blocked/reconciliation semantics; duplicate/reprocess owners are unchanged |
| EVID-REF-R5D-004 | R5D | unavailable | Return/ROR + inbound substring scope inspection | PASS_STATIC_ONLY | 2026-10-10 | direct-parser Return compatibility and ingestion post-parse correlation owners are unchanged; simulation Contains(CENIT) 7->7 and ingestion 1->1 remain outside named R5D orchestrator scope, total 8->8; DB-ID inference=0; PaymentRail=0; Persistence fallback=57/57 |
| EVID-REF-R5D-005 | R5D | unavailable | `scan_result_solid.py` + R5C/R5D protected-artifact comparison | FAIL / PASS_STATIC_ONLY | 2026-10-10 | SOLID scan exits 1 for existing backlog and is byte-identical R5C->R5D with SHA-256 b45d83724c3b6bd0a13014de7bf890f8b281e8ad24d9f97fdd40dde799f93b12; protected TestData/goldens/fingerprint/CENIT ROR/normative/schema/UI/MFT/outbound and R5A-R5C boundary artifacts remain unchanged except authorized R5D source/tests/docs/ratchet |
| EVID-REF-R5D-006 | R5D | unavailable | Git/.NET/Docker executable gates | NOT_RUN_ENVIRONMENT | 2026-10-10 | archive has no `.git`; `dotnet` and Docker are unavailable, so Release build, focused R5D/R5C/R5B/R5A, state-transition/Return-ROR/idempotency/recovery/downstream/golden/fingerprint and broad backend/runtime suites cannot verify or close R5D |

## Required update protocol

Before editing root `MEMORY.md`:

```cmd
git diff -- MEMORY.md
git ls-files --eol MEMORY.md
```

After editing:

```cmd
git diff -- MEMORY.md
git diff --ignore-space-at-eol -- MEMORY.md
```

Only durable verified decisions, invariants, closures, blockers and evidence pointers belong in root `MEMORY.md`. Raw logs and transient step-by-step notes stay in this state file or phase documentation.

## Session handoff template

```text
PROGRAM: REFACTOR-CA-SOLID-RES-001
PHASE: <R#>
STATE: <IN_PROGRESS|IMPLEMENTED_UNVERIFIED|VERIFIED|BLOCKED|CLOSED>
SHA: <sha>
CHANGED: <short file/module summary>
TESTS: <exact commands and results>
ARCH_RATCHET: <exceptions removed/remaining>
BEHAVIOR_CHANGE: NONE | <explicit authorized change>
NORMATIVE_CHANGE: NONE | <reference>
DB_MIGRATION: NONE | <migration ids>
BLOCKERS: <none/list>
MEMORY_UPDATE: <ids added/updated>
NEXT_ALLOWED_PHASE: <phase or NONE>
```

## R0 implementation note — 2026-10-10

- Added `tests/Cfa.ACHInterbank.Tests/RefactorArchitectureRatchetTests.cs` with shrinking allowlists for Application ASP.NET/WCF/NLog/X509/manual-HttpClient source violations, boundary packages, project references, and a zero-exception Domain guard.
- Added `tests/Cfa.ACHInterbank.Tests/NachaFunctional/NachaGoldenFingerprintCharacterizationTests.cs` to freeze the byte-level baseline for the six current NACHA golden files and the CENIT normative synthetic ROR fixture.
- Removed the skipped, non-enforcing SOAP architecture diagnostic; the remaining SOAP architecture tests stay active.
- Added `docs/architecture/refactor-ca-solid-resilience-baseline.md` with the baseline inventory and verification limits.
- No production source, golden file, expected result, schema, migration, ACH Colombia rule, CENIT rule, PUBLISH semantics, MFT lifecycle, idempotency or retry behavior changed.
- At the R0 verification attempt, root `MEMORY.md` was not edited because the mandatory Git-backed closure checks were unavailable. The later R1 LOCAL_ADVANCEMENT session records the durable unverified-environment state without claiming R0 verification/closure.

## R1 implementation note — 2026-10-10

- Executed only R1 under LOCAL_ADVANCEMENT; R0 remains `IMPLEMENTED_UNVERIFIED_ENVIRONMENT` and was not retried or promoted.
- R1A moved Application HTTP exception handling, address/conversion helpers tied to HTTP, MVC filters and middleware to `Cfa.ACHInterbank.Api`; `GetMenuForCurrentUserHandler` now consumes the transport-agnostic `ICurrentUserContext` port implemented by Api `HttpCurrentUserContext`.
- R1B moved the concrete NLog `LoggerManager` to Api/composition and retained the Application `ILoggerManager` port; JWK response logging is supplied by DI rather than direct construction in Application.
- R1C moved the `wsAutorizadorOM1` generated WCF connected service to External and moved the required WCF package references from Application to External.
- R1D moved `RsaKeyProvider`/`X509Store` access to External behind the existing `IRsaKeyProvider` Application port.
- R1E removed ASP.NET MVC/HTTP, NLog and WCF boundary package references from `Cfa.ACHInterbank.Application.csproj` after static zero-consumer checks; the architecture ratchet now has zero R1 boundary source/package exceptions.
- The legacy Application manual-`HttpClient` allowlist entry remains unchanged because it belongs to later resilience work, not R1.
- Tests/callers/DI were adjusted for the moved boundaries and new current-user adapter. No ACH Colombia/CENIT rules, NACHA layouts/causes/cycles/windows, PUBLISH snapshot semantics, golden files, expected results, MFT semantics, retry policy, schema or migration changed.
- `python scripts/validate-solution-projects.py` passes before and after R1; static R1 boundary inspection passes. `python scripts/scan_result_solid.py` still reports the known broad program backlog outside R1.
- The source archive has no `.git`; `dotnet` and Docker are unavailable. Build and .NET tests are `NOT_RUN_ENVIRONMENT`; R1 remains `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`, not VERIFIED/CLOSED.

## R2 implementation note — 2026-10-10

- Executed only R2 under LOCAL_ADVANCEMENT; R0 and R1 remain `IMPLEMENTED_UNVERIFIED_ENVIRONMENT` and were not promoted.
- Removed the `AppSettings.Settings` static runtime global. Auth/token/client-assertion, encryption/certificates, health/load-balancer and WCF consumers now receive cohesive options through DI; no mega settings interface was introduced.
- JWT/token configuration and required health/load-balancer server lists are bound in the composition root with fail-fast validation, including `ValidateOnStart`. Existing enabled-server filtering is preserved through `PostConfigure<ServesSettings>`.
- Eliminated targeted hidden runtime collaborators: API operational-time provider fallback, `PostFilter` concrete logger construction, clearing-house resolver fallback, SFTP session-factory construction, and nullable MFT execution/admin collaborators. ACH Colombia and CENIT strategy behavior remains separate and unchanged.
- Added R2 architecture ratchets: production `AppSettings.Settings` is forbidden; Api/Application/External runtime-service `?? new` fallbacks are zero; direct logger/SFTP-factory/API operational-time-provider construction is forbidden; 57 pre-existing Persistence fallbacks are recorded in a shrinking allowlist for later phases.
- Added `scripts/validate-r2-di-configuration.py` for host-independent structural enforcement. It passes on the R2 tree. `scan_result_solid.py` still exits 1 with byte-identical output to the captured R1 pre-R2 baseline.
- No manual HttpClient/resilience policy was changed; those concerns remain R3. No golden, normative, expected-result, schema/migration, ACH Colombia/CENIT rule, PUBLISH, idempotency, hash/audit, cluster/recovery or MFT semantic change was made.
- The source archive has no `.git`; `dotnet` and Docker are unavailable. Build and .NET tests are `NOT_RUN_ENVIRONMENT`; R2 remains `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`, not VERIFIED/CLOSED.

## R3 implementation note — 2026-10-10

- Executed only R3 under LOCAL_ADVANCEMENT; R0/R1/R2 remain `IMPLEMENTED_UNVERIFIED_ENVIRONMENT` and were not promoted.
- Replaced the three production direct `new HttpClient()` sites and the raw API default HttpClient action usage with explicit named `IHttpClientFactory` clients. Health GET alone receives bounded retry/circuit/concurrency/timeout because it is idempotent.
- Removed the legacy global WSCFAACH SOAP retry pipeline. Generic HTTP POST, WSCFAACH/WSAXON SOAP and WCF authorization calls receive no blind transport retry; SOAP timeout remains owned by persisted mappings and WCF timeout by its existing option.
- `ConnectionAuth` now classifies WCF failures for diagnostics and closes/aborts clients safely while preserving the existing `"timeout"` versus empty-string outcomes.
- Hardened target logs so curl/header/body secrets, WCF/SOAP payloads, health/server objects, password-reset links, client credentials and assertions are redacted/not emitted.
- Added R3 fault-injection test source and static ratchets. Existing notification timeout behavior continues to route uncertain WSAXON results to manual review rather than hidden transport replay.
- No ACH Colombia/CENIT normative rule, NACHA layout/code/cause/cycle/window/limit/Return/ROR behavior, PUBLISH authority, golden/expected result, schema/migration, MFT retry/recovery or SPA behavior changed.
- The source archive has no `.git`; `dotnet` and Docker are unavailable. Build and .NET tests are `NOT_RUN_ENVIRONMENT`; R3 remains `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`, not VERIFIED/CLOSED.

## R4A implementation note — 2026-10-10

- Executed only R4A in LOCAL_ADVANCEMENT; R4B/R4C/R4D/R4E were not started.
- Kept `NachaFileBuilder` as the existing `INachaFileBuilder` facade and did not change its six public contract methods or callers.
- Moved deterministic field identifier/value resolution, legacy primitive formatting and Type-5 settlement-date normalization into internal `NachaDeterministicFieldFormatter`.
- Moved deterministic block/padding arithmetic, configured blocking-factor resolution and physical padding construction into internal `NachaFileBlocking`.
- Reused the pre-existing `INachaControlTotalsCalculator`; no duplicate control-total abstraction was introduced.
- Added focused test source and `scripts/validate-r4a-nacha-builder-facade.py`; during R4A the ratchet prevented premature R4B renderer work. R4B later made that ratchet forward-compatible while preserving the R4A facade/helper/control-total invariants.
- No ACH Colombia/CENIT rule, NACHA layout/code/cause/cycle/window/limit/Return/ROR behavior, PUBLISH authority, golden/expected result, schema/migration, MFT retry/recovery, idempotency/hash/audit or SPA behavior changed intentionally.
- The source archive has no `.git`; `dotnet` and Docker are unavailable. Build and .NET tests are `NOT_RUN_ENVIRONMENT`; R4A remains `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`, not VERIFIED/CLOSED.

## R4B implementation note — 2026-10-10

- Executed only R4B in LOCAL_ADVANCEMENT; R4C/R4D/R4E and all R5+ work were not started.
- Kept `NachaFileBuilder` as the existing six-method `INachaFileBuilder` facade. Public callers, constructor/DI contract and public API shape are unchanged.
- Extracted the official table-driven physical record rendering subtree into internal stateless `INachaOfficialRecordRenderer` / `NachaOfficialRecordRenderer`: source-value resolution, configured field-rule execution, official field formatting/validation, fixed-width placement/padding and field-trace production.
- Used one generic renderer rather than six T1/T5/T6/T7/T8/T9 wrappers because the current official engine is PUBLISH/table-driven and the R4 plan explicitly says not to force abstractions that duplicate it. Record code and published layout remain inputs, so no rail policy is embedded in the renderer.
- Removed two private uncalled `BuildRecordInternalAsync` copies of rendering logic; active legacy/configured record rendering continues through the pre-existing `INachaFixedWidthRecordRenderer`.
- Kept profile/PUBLISH resolution, chamber/flow decisions, transaction/batch construction, record assembly order, T6/T7 association, control totals, block padding orchestration, semantic validation, SHA/audit persistence and side effects in `NachaFileBuilder`; this deliberately does not start R4C.
- Added `NachaOfficialRecordRendererTests` source and `scripts/validate-r4b-nacha-record-renderers.py`. Updated the R4A static validator only so its historical no-R4B gate does not reject the now-authorized R4B contract; all R4A invariants remain enforced.
- Prior R1/R2/R3/R4A static ratchets and the new R4B ratchet pass. The broad SOLID scan still exits 1 with output byte-identical to R4A. Protected byte fixtures and normative/schema/UI trees are unchanged.
- No intentional ACH Colombia/CENIT, NACHA, Return/ROR, PUBLISH, MFT, idempotency/hash/audit, retry/recovery or persistence behavior changed.
- The source archive has no `.git`; `dotnet` and Docker are unavailable. Build and .NET tests are `NOT_RUN_ENVIRONMENT`; R4B remains `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`, not VERIFIED/CLOSED.

## R4C implementation note — 2026-10-10

- Executed only R4C in LOCAL_ADVANCEMENT; R4D/R4E and all R5+ work were not started.
- Kept `NachaFileBuilder` as the existing six-method public facade. Public callers, constructor/DI surface and API contracts are unchanged.
- Added internal `NachaOfficialAssemblyPipeline`, a per-build deterministic assembly session. It owns physical concatenation and state for T1 -> T5 -> T6 -> T7 -> T8 -> T9, keeps T7 structurally under an open T6, verifies rendered T8/T9 values against the already-calculated authoritative totals, and applies `NachaFileBlocking` padding.
- Preserved error/audit record-count sequencing by letting the facade prepare/validate each batch and entry at the same point between assembly stages; the pipeline exposes only the rendered-record count and has no IO or persistence.
- Kept PUBLISH/profile resolution, ACH Colombia/CENIT branching and cross-field policy, transaction/batch preparation, `INachaControlTotalsCalculator.Calculate`, semantic validation, SHA-256, audit persistence, idempotency and MFT side effects in the builder/existing owners. ReturnOut/ROR physical flow was not rewritten; its existing helper was only renamed to distinguish it from ordinary official assembly.
- Added `NachaOfficialAssemblyPipelineTests` source and `scripts/validate-r4c-nacha-assembly-pipeline.py`. Updated the R4B validator only to become forward-compatible with the now-authorized R4C assembly component while preserving R4B renderer invariants.
- Prior R1-R4B static ratchets and the R4C ratchet pass. The broad SOLID scan remains the same known program backlog. Protected TestData/goldens/fingerprint/CENIT ROR, normative/schema/UI/MFT trees and unrelated source remain byte-identical to R4B.
- No intentional ACH Colombia/CENIT, NACHA, Return/ROR, PUBLISH, control-total, MFT, idempotency/hash/audit, retry/recovery or persistence behavior changed.
- The source archive has no `.git`; `dotnet` and Docker are unavailable. Build and .NET tests are `NOT_RUN_ENVIRONMENT`; R4C remains `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`, not VERIFIED/CLOSED.

## R4D implementation note — 2026-10-10

- Executed only R4D in LOCAL_ADVANCEMENT; R4E and all R5+ work were not started.
- Added internal `INachaChamberFlowPolicy` with separate ACH Colombia, CENIT and compatibility implementations resolved by canonical code and the already-resolved profile/configuration. No database-ID switch and no `PaymentRailOperationalStrategy` were introduced.
- Extracted official/ReturnOut rail/flow decisions, CENIT LIVE/homologation gates, render diagnostic context and rail-specific official-layout snapshot checks. PUBLISH/profile selection, transaction/batch preparation, authoritative totals, R4B rendering, R4C assembly, semantic validation, SHA-256, audit/persistence and ReturnOut/ROR physical side effects remain with their prior owners.
- `NachaFileBuilder` is 3349 lines (3693 before R4D), facade 6/6. Renderer count remains 3, assembly count 1, deterministic formatter/blocking/renderer/assembly set remains 4. Two legacy name-based compatibility fallbacks remain intentionally for R4E.
- Added `NachaChamberFlowPolicyTests` source and `scripts/validate-r4d-chamber-flow-policies.py`. R1-R4D static ratchets pass; the broad SOLID scan still exits 1 with byte-identical pre-existing backlog output. Protected TestData/goldens/fingerprint/CENIT ROR, normative/schema/UI/MFT trees and unrelated source remain byte-identical to R4C.
- The source archive has no `.git`; `dotnet` and Docker are unavailable. Build and .NET tests are `NOT_RUN_ENVIRONMENT`; R4D remains `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`, not VERIFIED/CLOSED.

`NEXT-REF-007`: `NEXT_ALLOWED_PHASE=R4E` under LOCAL_ADVANCEMENT. Execute only R4E in a subsequent increment; do not implement R4E/R5 in the R4D session or retroactively mark R0–R4D VERIFIED/CLOSED without exact-tree executable evidence.

## R4E implementation note — 2026-10-10

- Executed only R4E in LOCAL_ADVANCEMENT; R5A and all later work were not started.
- Removed the two explicitly deferred outbound `ClearingHouse.Name.Contains("CENIT")` fallbacks and one additional builder-scope Type7 clearing-house opt-in partial-match heuristic found by the required broad scan. Legacy/runtime generation now resolves the chamber through the existing `NachaClearingHouseProfileCodeResolver`; Type7 opt-in and policy selection use exact canonical codes. No DB-ID inference or replacement string heuristic was added.
- Thinned `NachaFileBuilder` from 3349 to 98 lines while preserving its six public methods and constructor/DI surface. The generation implementation now lives in internal `NachaFileGenerationCoordinator`; a direct generation-body comparison against R4D shows only the authorized chamber-resolution edits, the Type7 exact canonical-code opt-in replacement, and related parameter plumbing.
- PUBLISH/profile selection, transaction/batch preparation, authoritative totals, R4B rendering, R4C assembly, semantic validation, SHA-256, audit/persistence and the separate ReturnOut/ROR physical path keep the same semantics and ordering inside the coordinator/existing owners.
- Updated R4A-R4D ratchets only for forward-compatible ownership after the authorized facade split and added `scripts/validate-r4e-chamber-resolution.py`. All R1-R4E static ratchets pass. The broad SOLID scan still exits 1 with byte-identical pre-existing backlog output.
- Protected TestData/six goldens/fingerprints/CENIT ROR, normative/schema/UI/MFT paths remain byte-identical to R4D. No migration, normative or expected-output change was made.
- The source archive has no `.git`; `dotnet` and Docker are unavailable. Build and .NET tests are `NOT_RUN_ENVIRONMENT`; R4E remains `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`, not VERIFIED/CLOSED.

`NEXT-REF-008`: `NEXT_ALLOWED_PHASE=R5A` under LOCAL_ADVANCEMENT. Execute only R5A in a subsequent increment; do not implement R5A or later work in the R4E session or retroactively mark R0–R4E VERIFIED/CLOSED without exact-tree executable evidence.

## R5A implementation note — 2026-10-10

- Executed only R5A in LOCAL_ADVANCEMENT; R5B/R5C/R5D/R6 and later work were not started.
- Confirmed the plan scope as deterministic physical inbound parsing only: stream/byte framing and fixed-width record tokenization into an ordered physical NACHA model that is testable without `AchDbContext`.
- Added internal `NachaPhysicalParser`, `NachaPhysicalFile` and `NachaPhysicalRecord`. The component has no DB/EF, logger, configuration, HTTP, service-provider, filesystem, MFT/SFTP or persistence dependency.
- `NachaParserService` now delegates physical parsing to the new component; the pre-existing internal `ReadPhysicalRecordsAsync` path remains as a thin compatibility delegate. PUBLISH/profile resolution, T1/T5/T6/T7/T8/T9 interpretation, validation, duplicate/reprocess checks, persistence, state transitions, Return/ROR behavior and post-processing remain unchanged owners.
- The 10 productive inbound/config `Contains("CENIT")` occurrences reported by R4E were inspected and are outside R5A physical parsing. Counts remain exactly 2 in `NachaConfigValidationService`, 7 in `NachaInboundSimulationService`, 1 in `IncomingNachaIngestionAppService`; R5A-scope count is 0->0, DB-ID rail inference remains 0->0, and no new allowlist was added.
- Added DB-independent physical parser characterization for ACH happy path, invalid structure, Latin-1 `Ñ`/`ñ`, and CENIT ROR raw Type 7/order preservation, plus `scripts/validate-r5a-inbound-architecture.py`.
- R1-R5A static ratchets pass. `scan_result_solid.py` still exits 1 with byte-identical locally captured R4E/R5A backlog output. Protected TestData/six goldens/fingerprint/CENIT ROR, normative/schema/UI/MFT and outbound R4A-R4E source remain byte-identical to R4E.
- The source archive has no `.git`; `dotnet` and Docker are unavailable. Build and .NET tests are `NOT_RUN_ENVIRONMENT`; R5A remains `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`, not VERIFIED/CLOSED.

`NEXT-REF-009`: `NEXT_ALLOWED_PHASE=R5B` under LOCAL_ADVANCEMENT. Do not execute R5B/R5C/R5D/R6 or later work in the R5A session and do not infer R0–R5A VERIFIED/CLOSED without exact-tree executable evidence.

## R5B implementation note — 2026-10-10

- Executed only R5B in LOCAL_ADVANCEMENT; R5C/R5D/R6 and later work were not started.
- Confirmed the plan scope as inbound validation: selected PUBLISH constraints, inter-record/control validation, regulatory/semantic rules and correlation when applicable. R5A physical parsing remains closed and byte-identical; persistence and post-processing remain reserved for R5C/R5D.
- Added explicit validation components for shared/PUBLISH rules, CENIT-only rules, ACH Colombia compatibility rules and persistence-dependent read-only checks. Validators have no write persistence, HTTP, filesystem, managed-file transport, dispatch or post-processing side effects.
- Removed the two R5B-scope config-validation `Contains("CENIT")` sites using exact canonical chamber code equality. Simulation 7 and ingestion 1 sites remain untouched/out of scope, so tracked debt is 10->8; DB-ID inference remains 0 and no allowlist is expanded.
- Added focused source characterization and `scripts/validate-r5b-inbound-validation.py`. R1-R5B static ratchets pass; `scan_result_solid.py` still exits 1 with byte-identical historical backlog output.
- The source archive has no `.git`; `dotnet` and Docker are unavailable. Build and .NET tests are `NOT_RUN_ENVIRONMENT`; R5B remains `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`, not VERIFIED/CLOSED.

`NEXT-REF-010`: `NEXT_ALLOWED_PHASE=R5C` under LOCAL_ADVANCEMENT. Do not execute R5C/R5D/R6 or later work in the R5B session and do not infer R0–R5B VERIFIED/CLOSED without exact-tree executable evidence.

## R5C implementation note — 2026-10-10

- Executed only R5C in LOCAL_ADVANCEMENT; R5D/R6 and later work were not started.
- Confirmed the plan scope as inbound persistence: isolate the already parsed/validated graph write while preserving transaction behavior, exact-file membership and lineage. R5A physical parsing and R5B validation boundaries remain closed and unchanged.
- Added `NachaInboundPersistenceCoordinator` as the single owner of parsed-graph `AddRange` + `SaveChangesAsync`; existing `ParsedNachaEntityMapper` is reused. `IncomingNachaIngestionAppService` remains the sole explicit relational transaction owner, and direct parser calls retain one EF SaveChanges boundary.
- Duplicate/reprocess/hash/audit ownership remains unchanged. Return/ROR state transitions stay after persistence and outside R5C. The eight remaining `Contains("CENIT")` sites stay 7 simulation + 1 ingestion, unchanged and unassigned to a later phase merely from debt.
- Added focused persistence characterization and `scripts/validate-r5c-inbound-persistence.py`. R1-R5C static ratchets pass; `scan_result_solid.py` remains historical-backlog FAIL and must not be reported as PASS.
- The source archive has no `.git`; `dotnet` and Docker are unavailable. Build and .NET tests are `NOT_RUN_ENVIRONMENT`; R5C remains `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`, not VERIFIED/CLOSED.

`NEXT-REF-011`: `NEXT_ALLOWED_PHASE=R5D` under LOCAL_ADVANCEMENT. Do not execute R5D/R6 or later work in the R5C session and do not infer R0-R5C VERIFIED/CLOSED without exact-tree executable evidence.

## R5D implementation note — 2026-10-10

- Executed only R5D in LOCAL_ADVANCEMENT; R6 and later work were not started.
- Confirmed the plan scope as decomposition of `IncomingNachaPostProcessingOrchestrator` into resolve-context, eligibility, link/correlation, state-transition, dispatch-planning, external-dispatch and audit/events stages, each with explicit boundaries/cancellation.
- Preserved post-processing ordering: waiting-window reevaluation save, queue eligibility/prior-success guard, operation planning, optional external dispatch, response/state/audit, Proc_Contrapartidas delegation, then final post-processing save. Parsed-graph persistence remains R5C-owned and the ingestion transaction owner remains unchanged.
- No new transport retry was added; direct Proc_Transacciones send remains one site and prior-success/manual-review/uncertain outcomes preserve no-blind-resend semantics. Return/ROR ownership, fingerprint/reprocess, hash/audit authority and MFT lifecycle remain unchanged.
- The eight remaining productive `Contains("CENIT")` sites stay simulation 7 + ingestion 1, outside the named R5D orchestrator scope and unassigned to R6 merely from residual debt.
- Added focused source characterization and `scripts/validate-r5d-inbound-postprocessing.py`. R1-R5D static ratchets pass; `scan_result_solid.py` remains historical-backlog FAIL and must not be reported as PASS.
- The source archive has no `.git`; `dotnet` and Docker are unavailable. Build and .NET tests are `NOT_RUN_ENVIRONMENT`; R5D remains `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`, not VERIFIED/CLOSED.

`NEXT-REF-012`: `NEXT_ALLOWED_PHASE=R6A` under LOCAL_ADVANCEMENT. Do not execute R6A/R6B/R6C or later work in the R5D session and do not infer R0-R5D VERIFIED/CLOSED without exact-tree executable evidence.

## R6A implementation note — 2026-10-10

- Executed only R6A in LOCAL_ADVANCEMENT; R6B/R6C/R7 and later work were not started.
- Confirmed the plan target as contract segregation around `IAchColombiaManagedFileExchangeService`; the independent CENIT transport contract is out of scope and remains byte-identical.
- Redistributed the existing 15 operations into five public capability contracts: execution 2, transfer read 2, transfer operations/connection probe 6, administration/configuration 4, credential administration 1. The legacy facade remains as a compatibility aggregate with zero direct method declarations.
- Production consumers migrated from the facade: controller -> five capabilities; outbound/inbound Quartz handlers -> execution capability only. DI aliases all five capabilities to the same existing scoped facade/concrete instance; no lifetime or implementation owner changed.
- The concrete managed-MFT service, ACH Colombia adapters/resolver/connection tester/configuration provider and CENIT transport implementation are pinned byte-identical by the R6A ratchet. Retry/recovery, idempotency, claim/cluster safety, source-file lifecycle, archive/retire/download audit, credentials, hashing, Quartz/windows and `Transferred` semantics remain unchanged.
- R2-R6A static ratchets pass. `scan_result_solid.py` remains exit 1 with byte-identical R5D/R6A output SHA-256 `336daf505830f8c0a52c6440dd19e990a0f6ecd979952132450b9381c6cb064d`; it is not PASS.
- The source archive has no `.git`; `dotnet` and Docker are unavailable. Build and .NET tests are `NOT_RUN_ENVIRONMENT`; R6A remains `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`, not VERIFIED/CLOSED.

`NEXT-REF-013`: `NEXT_ALLOWED_PHASE=R6B` under LOCAL_ADVANCEMENT. Do not execute R6B/R6C/R7 or later work in the R6A session and do not infer R0-R6A VERIFIED/CLOSED without exact-tree executable evidence.

## R6B implementation note — 2026-10-10

- Executed only R6B in LOCAL_ADVANCEMENT; R6C/R7 and later work were not started.
- The plan-defined managed-MFT implementation split moves outbound workflow, inbound workflow/recovery, retry/reprocess, archive/retire/audited-download lifecycle, query projection, functional administration and credential administration out of the 1,000-line `AchColombiaManagedFileExchangeService`. The facade is now 105 lines and composes seven cohesive owners plus the unchanged connection tester.
- R6A remains intact: five capability interfaces are unchanged, the compatibility aggregate still declares zero direct methods, production facade consumers remain 0, and all capability aliases still resolve the same scoped facade instance.
- Session/factory ownership remains in the existing External adapter/`SshNetAchColombiaSftpSessionFactory`; transport implementation hashes are unchanged. Retry command owner remains 1, lifecycle archive-completion owner remains 1, adapter resolution remains 3 sites, and R6B adds no local/blind retry.
- Operation-local audit/persistence was not extracted into a new layer. Exact managed-MFT write/event call-site counts are preserved (`SaveChangesAsync` 24->24; `AddEvent` 37->37), and the existing export-audit owner remains unchanged. Idempotency/duplicate guards, claim recovery, concurrency tokens, persisted-result-before-archive semantics, `Transferred` meaning and source-file lifecycle remain unchanged.
- Protected TestData/goldens/fingerprint/CENIT ROR, normative docs, PostgreSQL/SQL Server migrations, R4 outbound/R5 inbound areas, R6A contracts and External/CENIT transport artifacts are unchanged outside authorized R6B files. Persistence fallback debt remains 57->57; out-of-scope `Contains("CENIT")` remains 8->8.
- R2-R6B host-independent validators pass. `scan_result_solid.py` remains exit 1 and its R6A/R6B output is byte-identical (SHA-256 `336daf505830f8c0a52c6440dd19e990a0f6ecd979952132450b9381c6cb064d`); it is not PASS.
- The source archive has no `.git`; `dotnet` and Docker are unavailable. Build and .NET tests are `NOT_RUN_ENVIRONMENT`; R6B remains `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`, not VERIFIED/CLOSED.

`NEXT-REF-014`: `NEXT_ALLOWED_PHASE=R6C` under LOCAL_ADVANCEMENT. Do not execute R6C/R7 or later work in the R6B session and do not infer R0-R6B VERIFIED/CLOSED without exact-tree executable evidence.

## R6C implementation note — 2026-10-10

- Executed only R6C in LOCAL_ADVANCEMENT; R7 and later work were not started.
- Confirmed Incoming Command Center as the existing inbound NACHA operational API at `/incoming-nacha-command-center`, not the ACH Colombia managed-MFT control plane and not a CENIT unification surface.
- Split the former 16-method direct command-center contract into `IIncomingNachaCommandCenterQueryService` (12 read methods) and `IIncomingNachaCommandCenterOperationsService` (4 manual dispatch commands). The compatibility aggregate now declares zero direct methods and production compatibility consumers move 1->0.
- The controller keeps exactly 12 GET + 5 POST actions, routes and authorization policies. Its 12 reads use the query capability; retry/unblock/requeue/mark-failed-final use the operations capability; orphan manual resolution remains separately owned.
- Query projections continue to use the existing persisted ingestion, queue, classification/link, integration-execution and processing-event data. Timeline is a projection of existing events; no read-model table, event store or schema owner was added.
- Manual commands preserve the existing state-machine guards, event-based idempotency/replay checks, correlation/audit `DispatchTransition` event and single save. No local/blind retry, SFTP/session construction, lifecycle/recovery rewrite or managed-MFT dependency was added.
- R6A contracts and R6B managed-MFT owners are unchanged. CENIT transport/configuration/credentials and SPA are unchanged; `Contains("CENIT")` stays 8->8 and Persistence fallback debt stays 57->57. `DB_MIGRATIONS=NONE`.
- R2-R6C host-independent validators pass. `scan_result_solid.py` remains exit 1 with unchanged backlog magnitude (237 Result-pattern findings, 13 SRP flags); the output delta is only command-contract line-number movement and is not reported as PASS.
- The source archive has no `.git`; `dotnet` and Docker are unavailable. Build and .NET tests are `NOT_RUN_ENVIRONMENT`; R6C remains `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`, not VERIFIED/CLOSED.

`NEXT-REF-015`: `NEXT_ALLOWED_PHASE=R7` under LOCAL_ADVANCEMENT. Do not execute R7 in the R6C increment and do not infer R0-R6C VERIFIED/CLOSED without exact-tree executable evidence.

## R7 implementation note — 2026-10-10

- Executed only R7 in LOCAL_ADVANCEMENT; R8A/R8B/R9 and later work were not started.
- The plan-defined scope is only decomposition of `AchReturnsService`: eligibility policy, original-transaction correlation, return batch composition, NACHA ReturnOut artifact generation, cause/policy resolution, state transition, and audit/monitoring. The public two-method service contract, two production callers, controller/routes/authorization and convention DI registration are unchanged.
- `AchReturnsService` shrinks 1066->205 lines and remains the overall transaction coordinator. Explicit transaction/save semantics stay `BeginTransactionAsync` 1->1 and `SaveChangesAsync` 2->2; trace reservation 1->1, ReturnOut build 2->2 and canonical transition 1->1 move only to their cohesive internal owners.
- Closed Returns/ROR decisions remain intact: accepted R10 same-day/effective-cycle behavior is copied without reinterpretation; DEV14 remains Claims-only; CENIT ordinary Return still rejects R60-R74; dedicated incoming Return and ROR owners/controllers are byte-identical. Audit source/hash/original trace/amount, generation lock, duplicate/idempotency and unique-conflict recovery semantics are preserved.
- R7 introduces no transport, service locator, blind retry, schema/migration, concrete `PaymentRailOperationalStrategy`, public API or SPA change. Persistence fallback debt remains 57->57; unrelated productive `Contains("CENIT")` remains 8->8.
- Added focused separation characterization and `scripts/validate-r7-returns-separation.py`. Solution plus R2-R7 static validators pass. `scan_result_solid.py` remains exit 1 and R6C/R7 raw output is byte-identical (237 Result-pattern findings, 13 SRP flags), so it is not PASS.
- The source archive has no `.git`; `dotnet` and Docker are unavailable. Build and .NET Return/ROR/regression tests are `NOT_RUN_ENVIRONMENT`; R7 remains `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`, not VERIFIED/CLOSED.

`NEXT-REF-016`: `NEXT_ALLOWED_PHASE=R8A` under LOCAL_ADVANCEMENT. Do not execute R8A/R8B/R9 or later work in the R7 session and do not infer R0-R7 VERIFIED/CLOSED without exact-tree executable evidence.

## R8A implementation note — 2026-10-10

- `EVID-REF-R8A-001`: Executed only R8A in LOCAL_ADVANCEMENT; R8B/R9 and later work were not started.
- `EVID-REF-R8A-002`: The plan-defined scope is only `ReportsController` modularization. The 1,028-line monolith is retired and its exact 20 HTTP actions are split into seven report capability/resource controllers. Route prefix, action verbs/templates, DTO/parameter metadata, response metadata and `CanReadAch` authorization remain unchanged.
- `EVID-REF-R8A-003`: Existing Application report contracts and service owners remain unchanged. MVC DI now constructs capability-specific controllers with maximum constructor fan-in 9->3; no service locator, concrete operational dependency, registration/lifetime change or wrapper service is introduced.
- `EVID-REF-R8A-004`: One shared API base owns clearing-house validation (owner 1->1, action calls 14->14). Traceability retains its exact date normalization, linked cancellation/30-second timeout, status/exception mapping and legacy `ReportsController` logger category. Controller persistence/transactions/state transitions/external transports/retry ownership remain 0->0.
- `EVID-REF-R8A-005`: `ControllerContractNameAttribute("Reports")` preserves `ControllerActionDescriptor.ControllerName=Reports`, keeping the unchanged OpenAPI operation-id and tag transformers compatible with the pre-R8A surface. All 20 moved action attribute/body blocks and `NormalizeDateRange` are text-identical to R7 after line-ending normalization.
- `EVID-REF-R8A-006`: Added `ReportsR8AContractCharacterizationTests` and `scripts/validate-r8a-reports-controller-modularization.py`; updated existing report/OpenAPI/accounting-review characterizations only where the owning controller type changed.
- `EVID-REF-R8A-007`: R2-R8A host-independent validators pass. Persistence fallback debt remains 57->57, out-of-scope productive `Contains("CENIT")` remains 8->8 and concrete `PaymentRailOperationalStrategy` remains 0->0. `scan_result_solid.py` remains exit 1 and R7/R8A output is byte-identical at 237 Result-pattern findings and 13 SRP flags (`UNCHANGED`, not PASS).
- `EVID-REF-R8A-008`: Protected TestData/goldens/fingerprint/CENIT ROR, normative docs, PostgreSQL/SQL Server migrations, R4 outbound, R5 inbound, R6 MFT/Command Center, R7 Returns, SFTP/session factory, CENIT transport and SPA remain outside R8A and byte-identical. `BEHAVIORAL_DIFF=NONE`, `NORMATIVE_DIFF=NONE`, `DB_MIGRATIONS=NONE`.
- `EVID-REF-R8A-009`: The source archive has no `.git`; `dotnet` and Docker are unavailable. Build and .NET API/OpenAPI/authorization/regression tests are `NOT_RUN_ENVIRONMENT`; R8A remains `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`, not VERIFIED/CLOSED.

`NEXT-REF-017`: `NEXT_ALLOWED_PHASE=R8B` under LOCAL_ADVANCEMENT. Do not execute R8B/R9 or later work in the R8A increment and do not infer R0-R8A VERIFIED/CLOSED without exact-tree executable evidence.

## R8B implementation note — 2026-10-10

- `EVID-REF-R8B-001`: Executed only R8B in LOCAL_ADVANCEMENT. The plan-named `CertificateManagementServices` item was selected because the 1,327-line compilation unit physically grouped ten distinct public service owners plus two internal helpers; R9 and later work were not started.
- `EVID-REF-R8B-002`: `CertificateManagementServices.cs` is retired. The existing owners are placed in eleven cohesive source files (ten one-service files plus shared mapper/status helpers). Public service classes remain 10->10, max public service classes per compilation unit becomes 10->1, and every moved class/helper body is byte-equivalent to R8A after line-ending normalization.
- `EVID-REF-R8B-003`: Application certificate interfaces/DTOs, production callers, `CertificateManagementController`, authorization/OpenAPI surface and `DependencyInjectionService` are byte-identical. Ten scoped interface-to-implementation registrations remain 10->10; no new interface, wrapper, service locator or concrete caller dependency is introduced.
- `EVID-REF-R8B-004`: Persistence/security ownership is unchanged: `SaveChangesAsync` 12->12, entity Add 23->23, Remove 2->2, `AsNoTracking` 20->20, explicit transactions 0->0, external transports/retries 0->0, `DateTime.UtcNow` 15->15 and `ZeroMemory` 1->1. No schema/migration, idempotency, recovery or secret-handling semantic changes are introduced.
- `EVID-REF-R8B-005`: Other R8B candidates remain intentionally untouched: official profile seeder and certificate DTO contracts are byte-identical; other >500-line services are not refactored by size alone and protected R4-R8A owners are not reopened. Reports remains byte-identical with 20 actions, `CanReadAch` 20/20 and logical `Reports` controller-name compatibility preserved. Persistence fallback debt remains 57->57, out-of-scope `Contains("CENIT")` remains 8->8 and `PaymentRailOperationalStrategy` remains 0->0.
- `EVID-REF-R8B-006`: Added `scripts/validate-r8b-certificate-management-service-cohesion.py` and `docs/architecture/refactor-ca-solid-resilience-r8b.md`. The R8B static ratchet passes, including the complete R1-R8A chain plus exact R8A Reports hashes/surface. `scan_result_solid.py` remains exit 1 with byte-identical R8A/R8B output SHA-256 `035ea42c468c38fbace75219d0535df3cf7a783c4403fc3879ac1d88ad3f6499`: 237 Result-pattern findings and 13 SRP flags (`UNCHANGED`, not PASS).
- `EVID-REF-R8B-007`: The host still lacks Git metadata, .NET and Docker, so build/.NET/API/OpenAPI/authorization/runtime/E2E evidence is `NOT_RUN_ENVIRONMENT`; R8B remains `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`, not VERIFIED/CLOSED.

`NEXT-REF-018`: `NEXT_ALLOWED_PHASE=R9` under LOCAL_ADVANCEMENT. Do not execute R9 or later work in the R8B increment and do not infer R0-R8B VERIFIED/CLOSED without exact-tree executable evidence.

## R9 implementation note — 2026-10-10

- `EVID-REF-R9-001`: Executed only R9 in LOCAL_ADVANCEMENT. Selected routes are current cycle-policy effective-date resolution, ACH Colombia managed-MFT inbound recovery, response-reprocess lease/heartbeat, Return-of-Return SLA, scheduler admin/sync/runtime, dynamic-job custom misfire/retry and scheduler cluster probe. R10 was not executed.
- `EVID-REF-R9-002`: Native `TimeProvider` is the clock seam. `TimeProvider.System` is registered once; selected behavior-determining real-clock accesses become 38->0 and selected TimeProvider consumers 1->9. No competing generic clock port is created. Existing operational timezone owners remain 3->3 and business-date owners 2->2, byte-identical.
- `EVID-REF-R9-003`: Five selected `Task.Delay` sites use the TimeProvider overload with unchanged durations and cancellation tokens. Retry/recovery delay owners remain 3->3, selected transaction owners 1->1, and selected true sync-over-async remains `.Result=0`, `.Wait()=0`, `.GetAwaiter().GetResult()=0`. Public HTTP/DTO/SOAP/WCF/MFT/NACHA contracts are unchanged.
- `EVID-REF-R9-004`: Fake-time boundary coverage uses the existing `FixedTimeProvider`: cycle-config UTC date before/at/after effective boundary, MFT recovery before/at/after retry cutoff, ROR SLA before/at/after deadline, and custom scheduler misfire before/at/after threshold. Selected fake-time sources become 2->5; no real sleep/polling is introduced.
- `EVID-REF-R9-005`: Added `scripts/validate-r9-deterministic-time-async.py` and `docs/architecture/refactor-ca-solid-resilience-r9.md`. Solution and R2-R9 host-independent validators are `PASS_STATIC_ONLY`. R7 validator compatibility accepts only the exact R7 baseline or exact authorized R9 ROR hash; R8B validator reuses the already-executed R2 metric from its R8A chain rather than spawning R2 twice.
- `EVID-REF-R9-006`: `scan_result_solid.py` remains exit 1 (`FAIL executed`), with 237 Result-pattern findings and 13 SRP flags. Normalized R8B/R9 semantic finding sets are identical: no additions/removals (`UNCHANGED`, not PASS). Persistence fallback debt remains 57->57, productive `Contains("CENIT")` 8->8 and concrete `PaymentRailOperationalStrategy` 0->0.
- `EVID-REF-R9-007`: SHA-256 comparison to R8B confirms protected TestData/goldens/fingerprint/CENIT-ROR, normative docs, PostgreSQL/SQL Server migrations, R8A Reports, R8B Certificate Management, web/SPA and non-authorized MFT owners unchanged. `BEHAVIORAL_DIFF=NONE`, `NORMATIVE_DIFF=NONE`, `DB_MIGRATIONS=NONE`.
- `EVID-REF-R9-008`: Input ZIP has no `.git`; `dotnet` and Docker are unavailable. Build and requested .NET/runtime/E2E suites are `NOT_RUN_ENVIRONMENT`; R0-R9 remain `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`, not VERIFIED/CLOSED.

`NEXT-REF-019`: `NEXT_ALLOWED_PHASE=R10` under LOCAL_ADVANCEMENT. Do not execute R10/recertification in the R9 increment and do not infer R0-R9 VERIFIED/CLOSED without exact-artifact executable evidence.

## R10 recertification note — 2026-10-11

- `EVID-REF-R10-001`: R10 was executed as recertification/documentary closure only. No productive or test source was changed. Exact R9 tree pins remain `src/**` 1,559 files / SHA-256 `517e414875074a9eeca9071a20ca4230986e5d3136cd6e62455369b8b11ea91e` and `tests/**` 373 files / SHA-256 `d1a9a58a3dc5b744d19e6034e38577e64a71b0ba429b2b1063e1a74f81847f62`.
- `EVID-REF-R10-002`: `scripts/validate-r10-final-recertification.py` consolidates and requires the complete R1-R9 ratchet chain. Static program metrics remain at their R9 targets: R1 forbidden Application boundaries 0; R2 AppSettings/hidden collaborators 0 with Persistence fallback 57; R3 transport/retry/sensitive-log debt 0; R4 facade 6/6; R5 seven stages/single parsed-graph persistence owner; R6 facade/SFTP/service-locator/query-side-effect debt 0; R7 2 methods/2 callers; R8A 7 controllers/20 actions/20 `CanReadAch`; R8B 10 services/max 1 per file/10 contracts/10 DI mappings; R9 selected real clock 0, TimeProvider consumers 9 and sync-over-async 0.
- `EVID-REF-R10-003`: Final SOLID scan remains exit 1 with 237 Result-pattern findings and 13 SRP flags; the finding set is semantically unchanged from R9. It is residual debt, not a PASS and not a new R10 regression.
- `EVID-REF-R10-004`: Protected TestData, six NACHA goldens, fingerprint characterization, CENIT ROR fixture, `docs/normativa`, PostgreSQL scripts, SQL Server migration project, SPA/web and all R4-R9 productive/test architecture remain byte-identical to R9. `BEHAVIORAL_DIFF=NONE`, `NORMATIVE_DIFF=NONE`, `DB_MIGRATIONS=NONE`.
- `EVID-REF-R10-005`: `.git` metadata is absent; `git status --short` and `git rev-parse HEAD` return 128. `dotnet` and Docker are not installed (command unavailable). Required Release build, architecture/focused/provider-specific tests, goldens/fingerprint/CENIT-ROR runtime checks and broad backend suite are `NOT_RUN_ENVIRONMENT`. No runtime PASS is claimed.
- `EVID-REF-R10-006`: R10 and the program remain `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`; no R0-R9 phase is retroactively promoted. Missing exact-commit provenance plus mandatory .NET/provider/runtime evidence blocks `VERIFIED/CLOSED`. Controlled Docker MFT/Quartz E2E is conditional on environment support and is not by itself the closure blocker when Docker is absent.

## Current next action

`NEXT_ALLOWED_PHASE=NONE`. The plan defines no R11. Await executable recertification of this exact R10 artifact in an environment that can establish Git provenance and run the mandatory .NET/provider/runtime gates; do not advance to another refactor phase.
