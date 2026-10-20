# REFACTOR-CA-SOLID-RES-001 — R6B MFT Implementation Separation

## Scope confirmed

R6B is limited to the implementation split named by the refactor plan for ACH Colombia managed MFT: outbound workflow, inbound workflow, retry/reprocess, archive/retire lifecycle operations, query projections, and administration/credentials. R6A capability contracts and production caller boundaries remain intact. R6B does **not** redesign the SFTP/managed-folder adapters, connection/session mechanics, retry policy, file lifecycle, persisted state meanings, Quartz triggers, or the Incoming Command Center reserved for R6C.

`ICenitGatewayTransportAdapter` and the CENIT transport remain independent and outside R6B. No common ACH Colombia/CENIT transport policy is introduced.

## Responsibility map before R6B

Before R6B, the 1,000-line `AchColombiaManagedFileExchangeService` implemented all five R6A capabilities and directly coordinated the following concerns:

| Concern | Owner before R6B | External/secondary owner that already existed |
|---|---|---|
| outbound orchestration | `AchColombiaManagedFileExchangeService` | managed-MFT adapter performs physical handoff |
| inbound orchestration / persisted-result recovery | `AchColombiaManagedFileExchangeService` | managed-MFT adapter performs pickup/claim mechanics |
| query/read projection | `AchColombiaManagedFileExchangeService` | EF query provider |
| manual retry/reprocess | `AchColombiaManagedFileExchangeService` | outbound/inbound operation paths |
| archive/retire/download lifecycle coordination | `AchColombiaManagedFileExchangeService` | adapter performs physical inbound archive/move |
| functional MFT administration | `AchColombiaManagedFileExchangeService` | persisted configuration entity/options defaults |
| credential administration | `AchColombiaManagedFileExchangeService` | existing encryption service |
| operation-local audit/persistence | `AchColombiaManagedFileExchangeService` | existing export audit service + EF persistence |
| session/connection creation | **not the service** | existing adapter + `SshNetAchColombiaSftpSessionFactory` |
| connection diagnostic | existing connection tester reached through service | `IAchColombiaManagedMftConnectionTester` |

The service constructor therefore carried 12 direct collaborators. Session creation itself had already been moved out by R2 and was not a valid R6B extraction target.

## Components after R6B

The compatibility service is now a 105-line composition/delegation facade over seven cohesive implementation owners:

| Component | Capability / responsibility owned | Side effects and dependencies |
|---|---|---|
| `AchColombiaManagedMftOutboundWorkflow` | outbound execution and the existing handoff-attempt state machine | generation, envelope, export audit, transfer persistence, adapter resolution; no new retry |
| `AchColombiaManagedMftInboundWorkflow` | inbound execution, duplicate/claim recovery and persisted-result processing orchestration | ingestion, transfer persistence, adapter pickup, recovery guards; delegates only post-result archive coordination |
| `AchColombiaManagedMftRetryReprocessComponent` | manual retry eligibility/command and manual reprocess command | reuses the exact outbound handoff and inbound processing paths; no duplicate transfer implementation |
| `AchColombiaManagedMftLifecycleComponent` | manual archive, retire, audited download, and post-persist inbound archive coordination | keeps existing event/persistence ordering; receives the already-resolved adapter for inbound archive |
| `AchColombiaManagedMftQueryProjectionComponent` | transfer query/detail projection | read-only EF projection; no transport ownership |
| `AchColombiaManagedMftAdministrationComponent` | functional configuration and administration | persisted config + existing options defaults; no operational setting changes |
| `AchColombiaManagedMftCredentialComponent` | credential update | existing encryption + persisted credential state; no secret logging |

`AchColombiaManagedMftImplementationSupport` is a shared internal pure/support helper for existing validation, mapping, hashing and normalization primitives. It is not a new workflow, transport, retry, lifecycle, persistence, or policy owner.

## R6A contracts and facade preserved

The five R6A contracts are byte-identical:

- `IAchColombiaManagedMftExecutionService`;
- `IAchColombiaManagedMftTransferReadService`;
- `IAchColombiaManagedMftTransferOperationsService`;
- `IAchColombiaManagedMftAdministrationService`;
- `IAchColombiaManagedMftCredentialAdministrationService`.

`IAchColombiaManagedFileExchangeService` remains the compatibility aggregate and still declares zero methods directly. Production facade consumers remain `0`; the controller continues to inject the five capability contracts and inbound/outbound Quartz handlers continue to inject only the execution capability.

The facade itself does not own `AchDbContext`, adapter resolution, options, encryption, ingestion, NACHA generation, persistence calls or transfer mechanics after R6B. It composes the seven R6B owners plus the unchanged connection tester and delegates the same 15 R6A operations.

## DI / composition root

All seven R6B components are registered `Scoped`. The existing scoped `AchColombiaManagedFileExchangeService` remains the `IAchColombiaManagedFileExchangeService` implementation. The five R6A capability aliases still resolve that same scoped facade instance, preserving the R6A lifetime/identity guarantee.

No service locator, functional `IServiceProvider`, static singleton, hidden `new SftpClient`, or hidden `new SshNetAchColombiaSftpSessionFactory` is introduced.

## Session and factory ownership

Session ownership is deliberately unchanged:

- `SshNetAchColombiaSftpSessionFactory` remains the sole constructor of SSH.NET SFTP sessions;
- the SFTP adapter opens/disposes one session for an outbound handoff operation;
- inbound pickup/claim uses its existing operation-scoped session;
- inbound archive uses its existing operation-scoped archive session;
- the connection tester uses the existing factory/session abstraction;
- R6B workflows resolve the existing managed-MFT adapter but do not create sessions.

Exceptions and cancellation therefore continue to dispose sessions through the existing adapter/factory boundaries. R6B introduces no shared, singleton, cross-job or cross-request session reuse and no reconnection policy.

## File lifecycle and ordering

R6B preserves the observed lifecycle rather than inventing a generic one. Outbound execution still builds the same payload/envelope and persists the same transfer/audit state around the existing adapter handoff. Inbound execution still follows the existing adapter claim/pickup semantics, persists/reuses the transfer identity and duplicate guards, processes the retained content, persists the result, and only then attempts inbound archive. A persisted result with an incomplete archive remains recoverable through the same persisted markers and active claim/storage reference.

Manual archive, retire and download keep their existing guards and event ordering. `DownloadAsync` remains side-effecting because it records the `Downloaded` event and saves it. No upload/download/list/move/archive/delete/acknowledgment ordering or path/name convention is changed.

## Retry, reprocess and recovery ownership

The pre-existing manual retry owner remains exactly one logical owner. `AchColombiaManagedMftRetryReprocessComponent` validates the same retry eligibility and reuses `AchColombiaManagedMftOutboundWorkflow.HandoffAsync`; it does not wrap upload/handoff in a new retry loop. Reprocess similarly reuses the exact inbound processing primitive and retains its pre-existing extra persistence point.

Inbound recovery remains owned by `AchColombiaManagedMftInboundWorkflow`: duplicate/claim recovery, persisted-result-before-archive checks, recovery-delay eligibility, concurrency handling and partial-failure semantics are preserved. The lifecycle component owns only the already-existing archive completion operation once the persisted-result precondition is satisfied.

No `Polly`, `Task.Delay`, sleep, blind retry, or new retry around upload/archive/retire/delete/acknowledge is added. Existing unknown-result handling remains in the unchanged transport adapter.

## Idempotency and cluster safety

The following signals keep their existing meaning and checks:

- content SHA-256 / logical transfer identity;
- operational date, cycle and corrected-transfer linkage;
- outbound persisted status and attempt counters;
- retained content and remote handoff state;
- inbound content hash + size duplicate guard;
- active claim/storage reference;
- incoming ingestion linkage and persisted-result marker;
- concurrency token refreshes and `DbUpdateException`/`DbUpdateConcurrencyException` handling;
- archive/retire timestamps and references.

R6B does not move atomic remote claim/rename mechanics out of the adapter and does not weaken the persisted duplicate winner/recovery boundaries.

## Audit / persistence ownership

R6B does **not** introduce a standalone audit/persistence abstraction. Persistence stays with the operation that already owned each write so ordering and transaction behavior remain visible at the workflow boundary. Consequently the monolith's persistence sites are distributed among the extracted operation owners, but they are not duplicated or centralized into a new layer.

Static preservation checks record exactly `24 -> 24` `SaveChangesAsync` call sites and `37 -> 37` managed-MFT `AddEvent` sites across the facade/components/support. The existing `IAchFileExportAuditService` remains the outbound export-audit owner. Failure events, unknown-result state, post-ingestion persistence and archive-pending state retain their original order.

## Administration and credentials

Functional configuration moves cohesively to `AchColombiaManagedMftAdministrationComponent`; credential mutation moves to `AchColombiaManagedMftCredentialComponent`. Existing validation, concurrency token handling, options defaults, encrypted credential storage, host-key requirements and exception messages are preserved. No operational configuration values or secrets are changed, and no secret is logged.

## CENIT and R6C boundary

CENIT transport code remains byte-identical. The eight out-of-scope `Contains("CENIT")` occurrences remain simulation `7` + ingestion `1` = `8`; R6B does not use that residual debt to infer later scope.

R6C is not executed. In particular, R6B does not create a new Incoming Command Center query/command API, timeline/read model, controller route, or manual-operation CQRS surface. Query projection extracted here is only the implementation owner of the already-existing R6A managed-MFT read capability expressly named by the R6B plan.

## Ratchet

`scripts/validate-r6b-mft-implementation-separation.py` records:

- `AchColombiaManagedFileExchangeService`: `1000 -> 105` lines;
- cohesive implementation owners: `1 -> 7`, plus one internal pure/support helper;
- facade direct collaborators: `12 -> 8` (seven R6B owners + existing connection tester);
- R6A capability interfaces: `5 -> 5`, compatibility facade direct methods `0 -> 0`;
- production compatibility-facade consumers: `0 -> 0`;
- session creation owner: `1 -> 1`, existing factory unchanged;
- retry command owner: `1 -> 1`; new blind/local retry sites `0`;
- lifecycle archive-completion owner: `1 -> 1`;
- adapter-resolution sites: `3 -> 3`;
- managed-MFT `SaveChangesAsync`: `24 -> 24`;
- managed-MFT `AddEvent`: `37 -> 37`;
- direct transfer implementation duplication: `0`;
- direct SFTP/session construction outside the authorized composition/factory boundary: `0`;
- service locator in MFT implementation/consumers: `0`;
- Persistence fallback debt: `57 -> 57` through the preserved R2 ratchet;
- out-of-scope `Contains("CENIT")`: `8 -> 8`;
- `PaymentRailOperationalStrategy` references in R6B scope: `0`;
- new R6B architecture debt: `0`.

The R6A validator is made narrowly successor-aware: when the exact R6B implementation-split marker exists, it continues to enforce the R6A contract/caller/DI boundary and uses the explicit R6B retry owner instead of incorrectly requiring the pre-R6B monolith body hash. No generic allowlist is added; protected External MFT/SFTP, CENIT transport, configuration provider, handlers, controller and R6A interface hashes remain pinned.

## Focused tests

R6B adds/updates focused tests so the existing behavior remains exercised through the new composition:

- `AchColombiaManagedMftImplementationSeparationTests` characterizes the facade's allowed collaborators, the unchanged R6A contract membership and cohesive component ownership;
- existing managed-MFT service tests now construct the extracted owners and continue to exercise outbound/inbound, retry/reprocess, archive/download/retire, administration and credential behavior through the compatibility facade;
- composition tests require the seven R6B components to resolve while preserving one scoped facade instance for all five R6A capability aliases;
- the existing query-focused multi-DB test uses the query projection component while retaining facade behavior.

Runtime execution remains environment-dependent; static characterization is not reported as a .NET test pass when the SDK is unavailable.

## Behavioral / normative / database impact

- `BEHAVIORAL_DIFF: NONE`
- `NORMATIVE_DIFF: NONE`
- `DB_MIGRATIONS: NONE`

No NACHA-M rule/layout, PUBLISH authority, golden/fixture, cycle/window, transfer status meaning, filename/path, credential, Quartz schedule, source-file lifecycle, retry/recovery policy, ACH Colombia rule or CENIT rule is changed.

## Environment status and next boundary

The source archive has no `.git`, and this host does not provide the .NET SDK or Docker. Host-independent Python ratchets can be executed, but Release build and .NET tests remain `NOT_RUN_ENVIRONMENT`. R6B can therefore reach only `IMPLEMENTED_UNVERIFIED_ENVIRONMENT` on this host.

Under `LOCAL_ADVANCEMENT`, successful R6B structural implementation enables `NEXT_ALLOWED_PHASE=R6C`. R6C and later phases are **not executed** in this package.
