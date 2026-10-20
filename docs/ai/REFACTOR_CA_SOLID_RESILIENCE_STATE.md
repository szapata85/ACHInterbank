# Refactor State Memory — ACHInterbank

> This file is an execution state companion for `REFACTOR-CA-SOLID-RES-001`. Durable verified decisions/closures must be merged compactly into the repository root `MEMORY.md`. Keep this file and root memory consistent while the program is active.

## Program head

- PROGRAM_ID: `REFACTOR-CA-SOLID-RES-001`
- PROGRAM_STATE: `IN_PROGRESS`
- BASELINE_PACKAGE: `ACHInterbank-v124-DOTNET-10.0.12-SDK-10.0.401-SECURE-DEPS-2026-10-09`
- BASELINE_ACCEPTED_FUNCTIONAL_COMMIT_FROM_MEMORY: `22d10822c5222c3c72a76b7279d6a885dfcbc75b`
- CURRENT_IMPLEMENTATION_SHA: `UNAVAILABLE_SOURCE_ARCHIVE_NO_GIT_METADATA`
- CURRENT_PHASE: `R0`
- NEXT_ALLOWED_PHASE: `R0`
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
- `INV-REF-010`: If the host lacks .NET/Docker/frontend dependencies needed for a gate, use `IMPLEMENTED_UNVERIFIED`; never infer success.
- `INV-REF-011`: Existing accepted R10/DEV14 and Return/ROR decisions remain closed and are not rederived by this program.
- `INV-REF-012`: Operational dates continue to use configured operational timezone, never UTC date by default.

## Phase state

| Phase | State | Scope | Evidence | Notes |
|---|---|---|---|---|
| R0 | IMPLEMENTED_UNVERIFIED | baseline + architecture ratchet + characterization | EVID-REF-R0-001..004 | .NET/Docker and Git metadata unavailable; R0 cannot be VERIFIED/CLOSED |
| R1 | PLANNED | clean Application boundary | none | requires R0 CLOSED |
| R2 | PLANNED | DI/configuration/fail-fast | none | requires R1 CLOSED |
| R3 | PLANNED | external resilience + secure logging | none | requires R2 CLOSED |
| R4A | PLANNED | low-risk deterministic extraction from NachaFileBuilder | none | golden output invariant |
| R4B | PLANNED | record rendering decomposition | none | table-driven engine preserved |
| R4C | PLANNED | generation assembly pipeline | none | caller compatibility |
| R4D | PLANNED | chamber/flow policies | none | no DB-ID switch target |
| R4E | PLANNED | facade thinning + remove remaining runtime fallbacks | none | full outbound regression |
| R5A | PLANNED | physical parser extraction | none | DB-independent tests |
| R5B | PLANNED | validation layers | none | PUBLISH authority preserved |
| R5C | PLANNED | persistence stage | none | transactional behavior preserved |
| R5D | PLANNED | post-processing pipeline | none | dispatch/idempotency preserved |
| R6A | PLANNED | MFT interface segregation | none | facade allowed temporarily |
| R6B | PLANNED | MFT implementation split | none | lifecycle unchanged |
| R6C | PLANNED | incoming command center CQRS | none | API behavior preserved |
| R7 | PLANNED | Returns/ROR SRP refactor | none | accepted policies not reopened |
| R8A | PLANNED | ReportsController/API modularization | none | routes/policies preserved |
| R8B | PLANNED | secondary large-class cleanup | none | evidence-based only |
| R9 | PLANNED | deterministic time + async cleanup | none | operational timezone preserved |
| R10 | PLANNED | final internal recertification | none | no external/UAT claims |

## Finding state

### P0

- `ARCH-001`: OPEN — Application package references include ASP.NET/NLog/WCF.
- `ARCH-002`: OPEN — HTTP middleware lives in Application.
- `ARCH-003`: OPEN — MVC/filter/HTTP exception concerns live in Application.
- `ARCH-004`: OPEN — WCF generated/transport concerns exist in Application.
- `ARCH-005`: OPEN — X509Store access exists in Application.
- `ARCH-006`: OPEN — NLog concrete dependency exists in Application.
- `ARCH-009`: IMPLEMENTED_UNVERIFIED — blocking source/package/project architecture ratchets were added and the non-enforcing skipped SOAP diagnostic was removed; .NET execution is pending.
- `SRP-001`: OPEN — `NachaFileBuilder` is a multi-responsibility god class (~4798 lines at baseline).
- `SRP-002`: OPEN — `NachaFileBuilder` has excessive collaborator count at baseline.
- `DIP-001`: OPEN — runtime services use concrete fallback construction.
- `SRP-003`: OPEN — `NachaParserService` is monolithic (~2247 lines at baseline).
- `SRP-005`: OPEN — incoming post-processing orchestrator is oversized/multi-responsibility (~1179 lines baseline).
- `RES-001`: OPEN — at least one SOAP/HTTP client creates HttpClient directly.
- `RES-002`: OPEN — legacy HttpClientService manually creates clients.
- `SEC-001`: OPEN — legacy logging paths require redaction review.
- `GOV-001`: IMPLEMENTED_UNVERIFIED — R0 added a shrinking legacy allowlist for Application boundary concerns/packages/project references; executable proof is pending.
- `GOV-002`: IMPLEMENTED_UNVERIFIED — existing critical characterization coverage was inventoried and SHA-256 byte-stability guards were added for six golden files plus the CENIT ROR synthetic fixture; executable proof is pending.
- `GOV-003`: OPEN — dual SQL Server/PostgreSQL evidence must remain for affected persistence paths.
- `GOV-004`: OPEN — cluster/MFT/Quartz no-duplicate and recovery semantics must be preserved.

### P1

- `ARCH-007`: OPEN — IHttpContextAccessor leakage into Application must be replaced where used by cases.
- `ARCH-008`: OPEN — direct concrete Persistence creation from API must be eliminated where present.
- `ARCH-010`: OPEN — AppSettings.Settings global usage remains.
- `OCP-001`: OPEN — chamber/flow branches remain in central NACHA generation.
- `SRP-004`: OPEN — parsing and persistence concerns require separation.
- `SRP-006`: OPEN — managed file exchange service mixes execution/admin/query/credential concerns (~996 lines baseline).
- `ISP-001`: OPEN — managed file exchange contract is broad.
- `SRP-007`: OPEN — incoming command center service is broad (~1502 lines baseline).
- `ISP-002`: OPEN — command center query/actions share a broad interface.
- `SRP-008`: OPEN — returns service is broad (~1066 lines baseline).
- `DIP-002`: OPEN — returns/runtime services use concrete fallback construction where found.
- `SRP-009`: OPEN — ReportsController is broad (~1028 lines baseline).
- `DIP-003`: OPEN — concrete fallback construction exists beyond a single service.
- `OCP-002`: OPEN — legacy clearing-house strategy factory/switch must be removed or DI-resolved if still active.
- `TEST-001`: OPEN — static clock usage is widespread; only behavior-sensitive routes are in refactor scope.
- `TEST-002`: OPEN — MFT operational behavior must use deterministic time where relevant.
- `RES-003`: OPEN — health HTTP client exists inside Application/legacy code.
- `RES-004`: OPEN — WCF resilience semantics require explicit classification.
- `ASYNC-001`: OPEN — sync-over-async exists in certificate path at baseline.
- `SEC-002`: OPEN — request/response logging requires sensitive-data review.
- `OBS-001`: OPEN — direct crash-log file write exists in middleware at baseline.
- `CFG-001`: OPEN — essential runtime dependencies/config should fail fast.
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
- Root `MEMORY.md` was not edited because the mandatory pre-write Git diff/EOL checks cannot run without repository metadata. Add a compact closure/evidence entry only after R0 is executable on an exact Git commit.

## Current next action

`NEXT-REF-001`: Verify and close R0 on a Git-backed host with .NET 10. Capture exact SHA/working tree, run the focal architecture/golden tests, Release build and broad backend suite, then update root `MEMORY.md` if evidence passes. `NEXT_ALLOWED_PHASE` remains `R0`; do not start R1.
