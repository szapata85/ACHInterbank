# REFACTOR-CA-SOLID-RES-001 — R5D Inbound Post-processing Pipeline

Date: 2026-10-10  
Mode: `LOCAL_ADVANCEMENT`  
State: `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`

## Exact plan scope

The implementation plan defines R5D as **inbound post-processing** and names `IncomingNachaPostProcessingOrchestrator` explicitly. The authorized decomposition is a pipeline/orchestrator with the following stages:

1. resolve context;
2. eligibility;
3. link/correlation;
4. state transition;
5. dispatch planning;
6. external dispatch when applicable;
7. audit/events.

Each stage must have explicit input/output boundaries and propagate cancellation. The R5 criteria also require preserving PUBLISH authority, exact-file membership/lineage, idempotency/recovery, and the rule that `None`/duplicate/manual-review paths must not produce a SOAP resend.

R5D does **not** authorize redesign of R5A physical parsing, R5B validation, R5C parsed-graph persistence, the ingestion transaction boundary, MFT/source-file lifecycle, simulation/generation orchestration, or R6.

## Implementation

R5D keeps the public `IncomingNachaPostProcessingOrchestrator` contract and constructor stable while reducing the class to an in-process serialization guard plus delegation to an internal `IncomingNachaPostProcessingPipeline`.

The internal pipeline contains seven explicit stage components:

- `IncomingNachaResolveContextStage` — loads persisted dispatch context and runtime integration settings;
- `IncomingNachaEligibilityStage` — evaluates waiting-window release, cycle policy, operational window and operational calendar;
- `IncomingNachaLinkCorrelationStage` — consumes already-created ingestion/classification links and checks whether a prior successful integration execution already exists;
- `IncomingNachaStateTransitionStage` — applies queue/transaction state transitions and the existing retry/backoff decisions;
- `IncomingNachaDispatchPlanningStage` — resolves the existing integration operation and preserves delegation to Proc_Contrapartidas;
- `IncomingNachaExternalDispatchStage` — prepares/maps the existing request, enforces mapping readiness/snapshot consistency and performs at most the existing single Proc_Transacciones send when operating mode permits it;
- `IncomingNachaAuditEventsStage` — owns integration execution/result audit projection, mapping trace writes and processing events.

The pipeline receives work that is already parsed, validated and persisted. It does not call `NachaPhysicalParser`, R5B validators, `NachaInboundPersistenceCoordinator`, `ParsedNachaEntityMapper`, or `NachaHeaders.AddRange`, and it does not create a new explicit transaction.

## Ordering before -> after

The observable post-processing order is preserved. Before R5D the monolithic orchestrator performed the following sequence; after R5D the same sequence is coordinated by the pipeline stages:

1. load integration retry-code policy;
2. reevaluate `WaitingWindow` queues;
3. persist any waiting-window queue/event changes with the existing first post-processing `SaveChangesAsync`;
4. select due queued/retry-pending IDs and load queue + ingestion + classification context;
5. for each queue, check prior successful execution before any resend;
6. evaluate cycle policy, operational window and calendar;
7. mark dispatching and create the same `DispatchStarted` event;
8. resolve dispatch operation;
9. delegate Proc_Contrapartidas targets when that existing operation is selected;
10. otherwise create the integration execution, validate persisted correlation/context, prepare/map the request and record request/mapping audit;
11. perform the existing Proc_Transacciones call only when the operating mode allows live dispatch;
12. parse/catalog the response, record response/result audit, then apply the same queue/transaction state outcome;
13. after the queue loop, invoke the same Proc_Contrapartidas job targets;
14. persist queue/state/audit results with the existing final post-processing `SaveChangesAsync`.

No new commit is introduced between these steps.

## Ownership before -> after

| Concern | Before R5D | After R5D |
|---|---|---|
| public post-processing entry point / in-process serialization | `IncomingNachaPostProcessingOrchestrator` | unchanged; thin facade |
| context/runtime resolution | monolithic orchestrator | `IncomingNachaResolveContextStage` |
| eligibility/window/calendar | monolithic orchestrator | `IncomingNachaEligibilityStage` |
| existing ingestion/classification correlation consumption + prior-success check | monolithic orchestrator | `IncomingNachaLinkCorrelationStage` |
| queue/transaction state transitions + retry/backoff | monolithic orchestrator | `IncomingNachaStateTransitionStage` |
| integration operation selection | monolithic orchestrator | `IncomingNachaDispatchPlanningStage` |
| Proc_Transacciones request preparation/send/response interpretation | monolithic orchestrator | `IncomingNachaExternalDispatchStage` |
| integration execution/result audit + events/mapping trace | monolithic orchestrator | `IncomingNachaAuditEventsStage` |
| Proc_Contrapartidas job invocation | monolithic orchestrator | pipeline orchestration, same relative point |
| parsed NACHA graph persistence | `NachaInboundPersistenceCoordinator` | unchanged |
| ingestion transaction begin/commit/rollback | `IncomingNachaIngestionAppService` | unchanged |
| source-file fingerprint/reprocess admission | `IncomingNachaIngestionAppService` | unchanged |
| direct-parser Return state-transition compatibility path | `NachaParserService` | unchanged |
| ingestion post-parse classification/link creation | `IncomingNachaPostParseProcessor` | unchanged |
| MFT/source-file lifecycle | existing MFT/ingestion owners | unchanged |
| simulation/generation orchestration | `NachaInboundSimulationService` | unchanged |

The R5D link/correlation stage does **not** create new operational links or reinterpret Return/ROR. It consumes the correlation/classification data already created by the ingestion path and checks prior successful integration execution to prevent duplicate dispatch.

## Transaction and SaveChanges boundaries

R5D does not call `BeginTransactionAsync`, `CommitAsync` or `RollbackAsync`.

The explicit relational ingestion transaction remains owned by `IncomingNachaIngestionAppService`, exactly as in R5C. The parsed NACHA graph remains written by the single R5C `NachaInboundPersistenceCoordinator.SaveChangesAsync` owner.

The post-processing orchestrator already had two `SaveChangesAsync` sites for its own queue/state/audit work, and R5D preserves both:

- one after waiting-window reevaluation when changes exist;
- one after per-queue processing and any Proc_Contrapartidas job invocation.

Those writes are not parsed-graph persistence and were not moved into R5C. R5D therefore does not change one commit into several or several into one, does not introduce nested transactions, and does not move an external call across a newly-created transaction boundary.

## Idempotency, duplicate and recovery

Ownership is unchanged:

- source-file SHA-256 + size fingerprint, canonical ingestion, duplicate admission and `ForceReprocess` remain in `IncomingNachaIngestionAppService`;
- NACHA identity/replay guards remain in `NachaParserService`;
- post-processing prior-success detection remains a dispatch guard, now isolated in `IncomingNachaLinkCorrelationStage`;
- retry scheduling remains state-transition policy, not a transport retry.

The Proc_Transacciones call remains a single direct send site. R5D does not introduce Polly/local transport retry around a potentially non-idempotent downstream operation. An uncertain exception remains blocked/manual-reconciliation behavior and is not automatically resent.

Existing `None`/unsupported planning, duplicate/prior-success and manual-review paths retain their no-resend behavior. Focused source characterization was added for the prior-success/duplicate dispatch guard, while existing tests remain the characterization authority for classification, manual review, Return correlation and recovery.

## Return / ROR

R5D does not reinterpret Return/ROR rules. Type 99, cause, original trace, new trace, association and CENIT semantics remain unchanged.

The direct-parser compatibility `ApplyReturnStateTransitionsAsync` path in `NachaParserService` is intentionally untouched because the R5D plan specifically targets `IncomingNachaPostProcessingOrchestrator`; moving that compatibility path would reopen a different boundary without explicit authorization.

The production ingestion classification/linking and Return correlation created by `IncomingNachaPostParseProcessor` also remain in place. R5D consumes their persisted context for dispatch decisions rather than becoming a second owner of correlation.

## Audit / hash

R5D moves existing post-processing audit/event logic into `IncomingNachaAuditEventsStage` without changing audit fields, hashes, correlation IDs or ordering.

Hash authority is unchanged:

- source-file fingerprint remains owned by ingestion and is still computed over the original source bytes;
- NACHA identity/replay hashes remain owned by the parser;
- post-processing request/response audit hashes remain computed over the same request/response payloads at the same logical point.

The new stage pipeline does not recalculate the source-file hash or change its representation.

## MFT and source-file lifecycle

Impact is `NONE`. The plan's R5D section does not authorize MFT/source-file move/archive/delete/acknowledgment work, and the new pipeline contains no MFT/SFTP/filesystem ownership. Existing recovery/reprocess/source-file owners remain unchanged.

## ACH Colombia / CENIT and remaining substring debt

The eight known productive `Contains("CENIT")` occurrences were classified against the documented R5D scope:

- `NachaInboundSimulationService`: `7 -> 7`. These sites remain simulation/generation/filename/profile/output-routing compatibility paths. R5D explicitly targets the post-processing orchestrator, not simulation.
- `IncomingNachaIngestionAppService`: `1 -> 1`. This site belongs to ingestion/profile orchestration and the existing transaction/reprocess owner, not the named R5D post-processing orchestrator.
- total remaining: `8 -> 8`.

Neither component is modified by R5D. No new allowlist is introduced, no DB ID is used to infer rail, and `PaymentRailOperationalStrategy` remains absent. The remaining debt is recorded without assigning it arbitrarily to R6.

Occurrence-by-occurrence classification on the R5C input tree:

| Site | Current behavior | R5D scope | Canonical authority / replacement boundary | Risk if changed in R5D |
|---|---|---|---|---|
| `NachaInboundSimulationService.cs:377` | simulation guard for CENIT Return-of-Return cause | No | simulation already has `ClearingHouse`/profile context; any exact-code migration belongs with simulation policy ownership | could alter simulated Return/ROR acceptance rather than post-processing |
| `NachaInboundSimulationService.cs:998` | blocks simulated CENIT CTX Return scope | No | same simulation chamber/profile context | could alter normative simulation behavior/CTX compatibility |
| `NachaInboundSimulationService.cs:1090` | emits simulated audit/config chamber code (`CENIT` vs `ACH`) | No | simulation request/chamber profile | could change persisted simulation audit metadata |
| `NachaInboundSimulationService.cs:1092` | selects simulated normative version (`CENIT` version vs `V35`) | No | simulation profile/normative context | could change generated simulation metadata and expected artifacts |
| `NachaInboundSimulationService.cs:1365` | chooses simulated output filename extension | No | simulation output policy | could change compatibility filename semantics |
| `NachaInboundSimulationService.cs:1385` | converts simulation chamber to config code for config lookup | No | simulation/config profile boundary | could select a different configuration row |
| `NachaInboundSimulationService.cs:1402` | chooses simulation output directory bucket (`cenit` vs `ach-colombia`) | No | simulation output-routing policy | could move generated files and change source/output ownership semantics |
| `IncomingNachaIngestionAppService.cs:903` | normalizes selected ingestion chamber code to config code | No | ingestion/profile/config resolution boundary | changing it here could alter config selection inside the transaction/reprocess owner |

R5D therefore leaves all eight byte-identical. An exact replacement must be authorized in the phase that owns each simulation/ingestion compatibility boundary and must preserve legacy valid configurations; residual debt alone is not sufficient authorization.

## Architecture ratchet

Static R5D measures for the exact tree:

- `NachaParserService`: `1076 -> 1076` lines;
- `IncomingNachaPostProcessingOrchestrator`: `1179 -> 79` lines;
- physical parser components: `1 -> 1`;
- R5B validation components: `4 -> 4`;
- R5C persistence coordinator: `1 -> 1`;
- explicit R5D post-processing stage components: `0 -> 7`;
- parsed-graph `SaveChangesAsync` owner total: `1 -> 1` (`NachaInboundPersistenceCoordinator`);
- post-processing queue/state/audit `SaveChangesAsync` sites: `2 -> 2`;
- explicit ingestion transaction owner: `1 -> 1` (`IncomingNachaIngestionAppService`); new R5D transaction owners: `0`;
- Proc_Transacciones direct send sites: `1 -> 1`; new local/blind retry sites: `0`;
- physical-parser writes: `0`; validator writes: `0`;
- DB-ID rail inference: `0 -> 0`;
- `PaymentRailOperationalStrategy`: `0 -> 0`;
- Persistence fallback ratchet: `57 -> 57`;
- new architecture debt: `0`.

`scripts/validate-r5d-inbound-postprocessing.py` enforces these R5D boundaries together with the prior R5A-R5C ownership ratchets.

## Characterization and environment status

Focused R5D source/tests cover the stage boundary and the prior-success no-resend behavior. Existing tests provide the deterministic regression inventory required by the R5 prompt for classification, duplicate handling, manual review, Return correlation and recovery. Expected results, TestData, goldens and fixtures are not changed.

The current environment has no `.git`, .NET SDK or Docker. Therefore Release build, focused .NET tests, executable Return/ROR/golden/fingerprint characterization and broad backend/runtime suites are `NOT_RUN_ENVIRONMENT`. R5D is correctly recorded as `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`, not `VERIFIED/CLOSED`.

## Behavioral / normative / database impact

- `BEHAVIORAL_DIFF: NONE`
- `NORMATIVE_DIFF: NONE`
- `DB_MIGRATIONS: NONE`

No NACHA-M layout, record type, cause code, addenda rule, cycle/window/limit, PUBLISH authority, ACH Colombia/CENIT regulatory rule, entity mapping or schema is changed.

## Next boundary

`NEXT_ALLOWED_PHASE=R6A` under `LOCAL_ADVANCEMENT` after R5D structural implementation. R6A is **not executed** in this package. No remaining substring debt or unrelated cleanup is used to infer R6 implementation scope.
