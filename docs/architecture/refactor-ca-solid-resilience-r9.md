# REFACTOR-CA-SOLID-RES-001 — R9 deterministic time and async consistency

## State and scope

- Phase implemented: `R9` only.
- Mode: `LOCAL_ADVANCEMENT`.
- Input state confirmed before change: `R0–R8B = IMPLEMENTED_UNVERIFIED_ENVIRONMENT`, `NEXT_ALLOWED_PHASE=R9`, R9 not executed.
- Output state: `R0–R9 = IMPLEMENTED_UNVERIFIED_ENVIRONMENT`; `NEXT_ALLOWED_PHASE=R10`. R10 is **not** executed by this package.
- Target framework: all product/test projects target `net10.0`, so native `TimeProvider` is available. No clock package was added.
- Contractual result: `BEHAVIORAL_DIFF=NONE`, `NORMATIVE_DIFF=NONE`, `DB_MIGRATIONS=NONE`.

R9 is intentionally not a mechanical repository-wide replacement of wall-clock calls. The selected scope is limited to product-owned time that determines cycle/config eligibility, MFT/recovery eligibility, Return-of-Return SLA eligibility, scheduler state/misfire behavior, and retry/heartbeat/recovery cadence. Pure audit-only timestamps, framework/Quartz internals, and unrelated code remain outside automatic migration.

## Pre-change inventory

Static discovery over R8B `src/**/*.cs` before modification found:

- `DateTime.Now = 5`
- `DateTime.UtcNow = 302`
- `DateTime.Today = 0`
- `DateTimeOffset.Now = 0`
- `DateTimeOffset.UtcNow = 54`
- `TimeProvider` references `=119`
- `Task.Delay = 7`
- raw `.Result` tokens `=96` (mostly domain/result properties; not treated as 96 sync-over-async sites)
- `.Wait() = 0`
- `.GetAwaiter().GetResult() = 4`

Quartz is already used extensively, and the repository already has explicit operational-time/business-date owners: `IOperationalTimeSnapshotProvider` / `OperationalTimeSnapshotProvider`, `OperationalCycleWindowResolver`, `IOperationalCalendarService` / `OperationalCalendarService`, and clearing-house configured timezone resolution. Those owners are reused; R9 introduces no generic `IClock`, `IOperationalClock`, `IBusinessClock`, or `ITimeService`.

### Classification and selected routes

| Route/file | Class | Why selected | Pre-R9 time semantics |
|---|---|---|---|
| `ClearingHouseCycleConfigService` | A — behavior-determining | default effective date determines which cycle/cutoff policy is current | UTC calendar date (`DateTime.UtcNow.Date`) |
| `AchColombiaManagedMftInboundWorkflow` | A — behavior-determining | recovery eligibility uses retry delay and persisted attempt/creation timestamps | UTC instant/date; configured retry delay |
| `AchResponseReprocessDispatcher` | A — behavior-determining | lease expiration, heartbeat renewal and completion ownership depend on UTC now | UTC instant; existing lease/heartbeat durations |
| `ReturnOfReturnOrchestrator` | A — behavior-determining | SLA expiration and eligibility-request instant depend on UTC now | UTC instant; strict `now > deadline` expiration |
| `SchedulerAdminService` | A/C | own offline/degraded state, manual metadata and preview reference time depend on now | UTC instant plus explicitly configured task timezone |
| `SchedulerSyncService` | A/C | schedulability, expiry, sync cursor and reconciliation date depend on now | UTC instant/date; configured cron timezone |
| `SchedulerRuntimeService` | A/C | runtime heartbeat evidence and cadence depend on now | UTC instant; existing cadence |
| `DynamicJobExecutor` | A/C | custom misfire decision, duration and retry backoff depend on now/delay | UTC instant; Quartz scheduled fire time retained |
| `SchedulerClusterProbeHandler` | A/D | recovery-probe timestamps and controlled delay depend on now | UTC instant; existing probe delay |

The selected production scope contained exactly `38` behavior-determining real-clock accesses before R9 (`DateTime.UtcNow=16`, `DateTimeOffset.UtcNow=22`) and `5` operational `Task.Delay` sites. It contained no true sync-over-async: `.Result=0`, `.Wait()=0`, `.GetAwaiter().GetResult()=0` after classifying the dispatcher’s `Result`/`ResultCode` domain properties correctly.

### Explicit exclusions

- `AchCycleScheduler.GetNextValidProcessingDate` still has the pre-existing compatibility `.GetAwaiter().GetResult()` method. Static search found no production/test caller; R9 does not break `IAchCycleScheduler` merely to remove an unused synchronous compatibility member.
- Managed-MFT outbound publication and lifecycle audit timestamps are not migrated because the reviewed timestamps do not determine retry eligibility; its automatic operational window already uses the existing operational-time owner.
- Bulk retry audit timestamps remain out because retry eligibility there is count/state driven, not elapsed-time driven.
- R8B certificate-management clock sites remain untouched because the reviewed uses are audit/state timestamps and R8B source cohesion is protected.
- Quartz/framework clock internals are not wrapped or replaced. Only product-owned scheduler logic is made deterministic.
- No R10 cleanup, schema work, NACHA layout/golden change, normative change, transport/session redesign, retry redesign, Return/ROR policy redesign, or SPA work is included.

## Implementation

### Native `TimeProvider`

`TimeProvider.System` is registered once in the Application composition root. The nine selected consumers receive `TimeProvider` explicitly, with an optional `TimeProvider.System` fallback retained on concrete constructors for compatibility with existing direct construction in tests/helpers. No service locator or custom static clock was added.

The selected behavior-determining wall-clock sites become `38 -> 0`; selected `TimeProvider` consumers become `1 -> 9`. Host-local clock use in the selected scope stays `0 -> 0`.

### Timezone and business-date preservation

R9 does **not** reinterpret existing dates:

| Flow | Before | After | Semantic preservation |
|---|---|---|---|
| current clearing-house cycle policy | `DateTime.UtcNow.Date` | `timeProvider.GetUtcNow().UtcDateTime.Date` | still UTC calendar date; no accidental Bogotá business-date substitution |
| MFT inbound provisional/recovery timing | UTC `DateTime` wall clock | injected UTC instant/date | retry delay and exact `<= cutoff` rule unchanged |
| Return-of-Return SLA | UTC `DateTime` wall clock | one injected UTC instant captured per registration | exact `now > deadline` expiration rule unchanged; exact deadline remains eligible |
| scheduler admin/sync/runtime | `DateTimeOffset.UtcNow` / one UTC date | injected UTC `DateTimeOffset` / UTC date | configured cron/timezone paths unchanged |
| custom Quartz wrapper misfire | wall UTC vs Quartz scheduled fire time | injected UTC vs same Quartz scheduled fire time | threshold remains strict `lateness > threshold` |

The existing timezone/business-date semantic owners are byte-identical to R8B:

- operational timezone owners: `3 -> 3` (`OperationalTimeSnapshotProvider`, `ClearingHouseOperationalTimeZone`, `ClearingHouseCyclePolicyResolver`);
- business-date owners: `2 -> 2` (`OperationalTimeSnapshotProvider`, `OperationalCalendarService`).

No selected route starts depending on host local timezone. The configured `America/Bogota`/clearing-house timezone paths remain owned where they were before R9.

### Retry/recovery and async consistency

The five selected operational delays now use the native `Task.Delay(..., TimeProvider, CancellationToken)` overload. Their duration, retry count, backoff, lease/heartbeat cadence and cancellation token are unchanged:

- `AchResponseReprocessDispatcher` heartbeat;
- `SchedulerSyncService` one-minute reconciliation loop;
- `SchedulerRuntimeService` runtime heartbeat loop;
- `DynamicJobExecutor` configured retry backoff;
- `SchedulerClusterProbeHandler` controlled probe delay.

Retry/recovery delay ownership remains `3 -> 3` for the behavior-owning R9 routes (MFT inbound recovery eligibility, response-reprocess lease/heartbeat, dynamic-job retry backoff). Transaction ownership in the selected scope remains `1 -> 1`; `AchResponseReprocessDispatcher` still owns its existing two transaction sites. R9 does not move persistence or transaction boundaries.

Selected sync-over-async remains zero before/after:

- `.Result`: `0 -> 0` true `Task.Result` sites;
- `.Wait()`: `0 -> 0`;
- `.GetAwaiter().GetResult()`: `0 -> 0`;
- selected delay cancellation gaps: `0 -> 0`.

No public HTTP/DTO/SOAP/WCF/MFT/NACHA interface is changed for async propagation.

## Deterministic boundary characterization

The project’s existing `FixedTimeProvider` test double is reused; no package, `Thread.Sleep`, real polling or wall-clock wait is added. Selected fake-time boundary sources become `2 -> 5`.

Coverage added/strengthened:

- `ClearingHouseCycleConfigServiceTests`: one tick before the UTC effective-date boundary, exactly at `00:00:00Z`, and after the date boundary. This characterizes the existing UTC-date policy selection without turning it into business date.
- `AchColombiaManagedFileExchangeTests`: MFT recovery one tick before the configured delay, exactly at the `<= cutoff` boundary, and one tick after it; persisted recovery timestamps are based on the fixed instant.
- `CenitOperationalGovernanceTests`: Return-of-Return SLA one tick before deadline, exactly at deadline, and one tick after deadline; eligibility request and orchestrated timestamp use the same injected instant.
- `DynamicJobRetryTests`: custom misfire one tick before, exactly at, and one tick after the 60-second threshold; existing scheduler/retry tests now use a fixed UTC instant.
- Existing `CycleOperationalWindowTests` continue to characterize operational cycle boundaries/timezone conversion.

Because `.NET` is unavailable in this environment, these test sources are implemented but **not executed** here; they are `NOT_RUN_ENVIRONMENT`, not PASS.

## Architecture ratchet

Added `scripts/validate-r9-deterministic-time-async.py`. It deliberately measures only the selected operational routes and reports:

- behavior-determining real clocks `38 -> 0`;
- selected `TimeProvider` consumers `1 -> 9`;
- operational semantic time ports `1 -> 1`;
- host-local-time dependencies `0 -> 0`;
- fake-time boundary test sources `2 -> 5`;
- true `.Result` / `.Wait()` / `.GetAwaiter().GetResult()` `0 -> 0`;
- cancellation gaps `0 -> 0`;
- timezone owners `3 -> 3`, business-date owners `2 -> 2`, retry/recovery delay owners `3 -> 3`, transaction owners `1 -> 1`;
- Persistence fallback debt `57 -> 57`, productive `Contains("CENIT")` debt `8 -> 8`, concrete `PaymentRailOperationalStrategy` `0 -> 0`;
- new R9 architecture debt `=0`.

Two prior validator-only adjustments are required so the ratchet chain understands the authorized R9 delta without weakening product boundaries:

1. `validate-r7-returns-separation.py` accepts either the exact R7 `ReturnOfReturnOrchestrator` hash or the exact authorized R9 deterministic-time hash. Every other R7 protected hash is unchanged.
2. `validate-r8b-certificate-management-service-cohesion.py` reuses the Persistence-fallback metric already emitted by its successful R8A/R2 chain instead of spawning R2 a second time. R8B certificate sources/contracts/DI/controller remain protected exactly as before; this only removes redundant validator execution.

## Validation evidence

### Executed static validation

The following completed with exit `0` and are classified `PASS_STATIC_ONLY`:

- `python scripts/validate-solution-projects.py`
- every architecture validator R2 through R8B
- `python scripts/validate-r9-deterministic-time-async.py`

The R9 ratchet reports the exact metrics above and invokes the complete prior R1–R8B chain.

`python scripts/scan_result_solid.py` was executed and remains exit `1`, therefore it is classified `FAIL executed`, not PASS. R8B and R9 each report `237` Result-pattern findings and `13` SRP flags. A normalized semantic comparison (file/signature for Result findings, file owner for SRP findings) has no additions and no removals: `UNCHANGED`; only R9 source line movement is irrelevant to that semantic set.

### Environment-limited executable validation

The input ZIP has no `.git` directory. `git` binary is present, but repository-history/diff commands cannot provide Git evidence for this archive. `dotnet` is unavailable and Docker is unavailable.

Therefore all requested .NET build/runtime suites are `NOT_RUN_ENVIRONMENT`, including Release build, focal R9 tests, cycle/window/cutoff tests, MFT/recovery tests, Returns/ROR tests, scheduler/misfire tests, async/cancellation tests, architecture tests, R4–R8 regressions, golden/fingerprint/CENIT ROR and broad backend suite. Docker/E2E is also `NOT_RUN_ENVIRONMENT`.

No runtime PASS is inferred from static validation.

## Protected R8B → R9 comparison

A SHA-256 comparison against the original R8B ZIP confirms zero unauthorized changes in the protected sets:

- `tests/**/TestData`: 15 files, `0` changed;
- `docs/normativa`: 12 files, `0` changed;
- PostgreSQL migration set: 92 files, `0` changed;
- SQL Server migration set/project: 79 files, `0` changed;
- R8A Reports protected API/OpenAPI sources: 11 files, `0` changed; actions remain `20`, `CanReadAch=20/20`, logical Reports metadata preserved;
- R8B Certificate Management protected set: 27 files, `0` changed; grouped legacy unit remains absent, public services `10`, maximum public services/file `1`, contracts and scoped mappings remain `10 -> 10`;
- `web/**`: 772 files, `0` changed; `docs/spa/**`: 4 files, `0` changed;
- MFT outbound workflow, lifecycle and retry/reprocess component are byte-identical; only the explicitly authorized inbound recovery clock route is changed;
- R7 public contracts/controller/dedicated Return/ROR owners remain protected; only the explicitly authorized `ReturnOfReturnOrchestrator` time source changes.

The six NACHA goldens, fingerprint characterization, CENIT ROR fixture and related protected fixtures are inside unchanged TestData/protected sets. CENIT transport and SFTP/session mechanics are not redesigned.

## Result

R9 reaches `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`. The implementation changes only time-source/delay testability in the selected operational routes and preserves existing timezone, business-date, cutoff, retry, transaction, Return/ROR and Quartz semantics. No executable .NET evidence is available on this host, so R9 is not `VERIFIED` or `CLOSED`.

`NEXT_ALLOWED_PHASE=R10` under LOCAL_ADVANCEMENT. R10 was not executed and no R10 recertification/closure work is included.
