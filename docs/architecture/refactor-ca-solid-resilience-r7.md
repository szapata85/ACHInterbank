# REFACTOR-CA-SOLID-RES-001 — R7 Returns / Return-of-Return

Date: 2026-10-10  
Mode: `LOCAL_ADVANCEMENT`  
State reached: `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`

## Exact scope confirmed

The implementation plan defines R7 narrowly: reduce the responsibility of `AchReturnsService` without reinterpreting any closed Return/Return-of-Return rule. The authorized separations are eligibility policy, original-transaction correlation, return batch assembly, NACHA return artifact generation, cause/policy resolution, state transitions, and audit/monitoring. R8 controller/secondary-service work is explicitly outside this increment.

R7 is therefore a structural refactor of the existing outbound Return owner. It does not add a public endpoint, command, query, transport, database object, migration, retry policy, payment-rail strategy, or UI surface. Existing dedicated incoming-Return and Return-of-Return owners remain separate and byte-identical.

## Closed behavior preserved

R7 treats accepted memory and existing regulatory characterization as the contract rather than re-deriving policy:

- `DEC-ACHCOL-R10-DEV14-001` remains closed: accepted ACH Colombia R10 uses the existing same-operational-day/effective `AchReturnPolicy.MaxCycles` decision; DEV14 remains Claims-only and cannot enter ReturnOut/Addenda 99.
- ACH Colombia and CENIT resolution remain code/profile based and separate.
- CENIT ordinary Return rejects R60–R74 and keeps ROR in its dedicated CENIT Return-of-Return flow.
- Existing exact original trace/amount mapping, provider-native Return trace reservation, duplicate/idempotency protection, state transition, audit payload, SHA-256 and file naming behavior are retained.
- PUBLISH/NACHA rendering, layout, control totals and R4 assembly authority remain in their existing owners; R7 only invokes the existing `INachaFileBuilder` ReturnOut contract.

## Owner before → after

| Responsibility | R6C input owner | R7 owner |
| --- | --- | --- |
| Public Return contract | `IAchReturnsService` (2 methods) | unchanged, 2 methods |
| Return API | `AchReturnsController` (1 GET + 1 POST) | unchanged by hash |
| Overall Return orchestration / transaction boundary | 1,066-line `AchReturnsService` | 205-line `AchReturnsService` coordinator |
| Eligibility / R10 / CENIT ordinary-Return policy | `AchReturnsService` | `AchReturnEligibilityPolicyComponent` |
| Original cycle/transaction correlation and CENIT raw evidence | `AchReturnsService` | `AchReturnOriginalCorrelationComponent` |
| Cause/rail-flow cause policy | `AchReturnsService` | `AchReturnCausePolicyComponent` |
| Return semantic entry, batch/header composition and trace allocation | `AchReturnsService` | `AchReturnBatchComposerComponent` |
| ReturnOut artifact + external-name policy | `AchReturnsService` | `AchReturnArtifactGenerationComponent` |
| State transition to `ReturnedByEpr` | `AchReturnsService` | `AchReturnStateTransitionComponent` |
| Return-file audit payload, SHA and existing payment-rail shadow compare | `AchReturnsService` | `AchReturnAuditMonitoringComponent` |
| Incoming Return | dedicated existing implementation | unchanged |
| Return-of-Return / CENIT ROR | dedicated existing implementations | unchanged |

The class name `AchReturnBatchComposerComponent` implements the plan's “return batch assembly” responsibility while avoiding collision with the R4 architecture ratchet that deliberately reserves `*Assembly*.cs` in the NACHA generation implementation directory for `NachaOfficialAssemblyPipeline`.

## Contracts, callers and DI

No public/internal cross-layer contract is added for R7. `IAchReturnsService` remains byte-identical with `GetTransactionsByCycleAsync` and `GenerateReturnsFileAsync`; production callers remain exactly two (`AchReturnsController` and `AchOutboundReturnDispatchService`). `AchReturnsController` remains byte-identical with the same routes and authorization attributes.

The existing convention-based scoped registration remains `IAchReturnsService -> AchReturnsService`; `DependencyInjectionService.cs` is byte-identical. The seven R7 components are internal implementation owners composed by `AchReturnsService` from the dependencies it already receives. This preserves all existing direct-construction tests and named constructor arguments while avoiding service locator / `IServiceProvider` usage. The two pre-existing Persistence constructor fallbacks for state-transition and Return trace-sequence services remain exactly where they were semantically; the global Persistence fallback ratchet stays 57.

## Orchestration and side effects

The coordinator preserves the pre-R7 order:

1. validate non-empty/non-duplicate request;
2. resolve original/return cycle and chamber;
3. enforce DEV14/CENIT scope guard;
4. acquire the existing return-generation lock;
5. correlate selected original transactions and already-generated state;
6. evaluate transaction/R10/generic eligibility, cause policy and CENIT policy;
7. map exact original trace/amount/entity data and existing shadow comparison;
8. require the existing NACHA builder/name policy and validate one participant;
9. open the same optional relational DB transaction;
10. reserve the same provider-native trace range;
11. create `AchReturnGenerated` rows and perform the same relational pre-artifact save;
12. build batches/header and invoke the existing ReturnOut builder/name policy twice (provisional + audited final);
13. persist final file name;
14. call the same canonical state-transition service with the same event source, reason, payload, original trace and idempotency key;
15. commit, or rollback and map the same unique-conflict path to `AchReturnAlreadyGeneratedException`.

No HTTP, SOAP/WCF, MFT/SFTP, filesystem dispatch, notification or retry ownership is introduced by the extracted components. The legacy private no-policy naming fallback is retained inside the artifact owner for source continuity but remains unreachable from the active ReturnOut path, which continues to require `IExternalFileNamePolicy` before persistence/artifact generation.

## Persistence and transaction impact

R7 introduces no schema or model change. `AchReturnsService` remains the single explicit ReturnOut transaction coordinator:

- `BeginTransactionAsync`: `1 -> 1`;
- facade `SaveChangesAsync`: `2 -> 2`;
- extracted components owning transaction/commit: `0`;
- generated-row `AddRange`: `1 -> 1`;
- unique-violation classification/reload path: preserved;
- trace reservation sites: `1 -> 1`, moved to batch-composition owner;
- canonical `TransitionAsync` sites: `1 -> 1`, moved to state-transition owner.

`DB_MIGRATIONS: NONE`.

## NACHA artifact / audit invariants

The semantic `NachaReturnOutEntry`, batch grouping/order, service-class calculation, transaction-code mapping, ACH/CENIT header logic, provisional/final `BuildReturnOutAsync` calls, naming policy, file-id modifier, record count and returned bytes preserve the pre-R7 values and ordering. R4 formatter/renderer/assembly/PUBLISH/totals/SHA owners are not modified.

The state transition continues to generate `ReturnFileGenerated` payload with source `AchReturnsService.GenerateReturnsFileAsync`, original/new trace, amount, Return reason, file name, record/return count, chamber, R10 basis and lowercase SHA-256 of the final file content. No audit/event store is added and no secret is logged.

## Retry, recovery, idempotency and concurrency

R7 adds no retry. Existing generation locking, already-generated lookup, provider-native sequence reservation, relational unique-key conflict handling and state-transition idempotency key (`outbound-return-v1:{transactionId}`) are preserved. Recovery/reprocess and managed-MFT lifecycle belong to other existing owners and are untouched.

## Security / authorization

`AchReturnsController` is byte-identical. R7 does not alter route policies, authentication, current-user handling, HTTP status mapping or exception mapping. No ASP.NET type is introduced into Application/Persistence boundaries beyond their existing host-independent contracts.

## ACH Colombia / CENIT / ROR boundary

R7 does not unify rails. Chamber recognition remains exact (`ACH` / `ACHCOL` / `ACHCOLOMBIA` versus `CENIT`). The eight unrelated productive `Contains("CENIT")` sites remain 7 in `NachaInboundSimulationService` plus 1 in `IncomingNachaIngestionAppService` (`8 -> 8`). Dedicated CENIT ROR eligibility/file/orchestration/controller artifacts are pinned byte-identical by the R7 validator, so ROR cannot inherit ordinary Return policy through this refactor.

The existing payment-rail **resolver/shadow comparison** dependency is merely moved with its prior monitoring logic. R7 does not introduce the prohibited concrete `PaymentRailOperationalStrategy`: exact concrete references remain `0 -> 0`.

## Characterization and focused tests

Before moving logic, the existing Return/ROR characterization/regulatory suites were identified as the behavior contract, including:

- `AchReturnsFileByClearingHouseTests` for allowed causes, DEV14 rejection and accepted R10 same-day/window/basis behavior;
- `AchOutboundReturnStateAndIdempotencyCharacterizationTests` for original trace, state and audit payload;
- `AchReturnIdempotencyTests` and `AchOutboundReturnConcurrencyIdempotencyTests` for duplicate/concurrency behavior;
- `CenitReturnOut2026Tests` for CENIT Return context and R60–R74 separation;
- `CenitReturnOfReturn2026Tests` for independent CENIT ROR rules/idempotency;
- existing multi-provider Return tests for provider-native trace/persistence behavior.

R7 adds `AchReturnsR7SeparationTests` to characterize the unchanged two-method public contract, exact original trace/amount/entity/transaction-code mapping at the new batch owner, and the legacy Return-file audit source/hash payload. These .NET tests cannot be executed on this host because `dotnet` is absent.

## Architecture ratchet

`scripts/validate-r7-returns-separation.py` enforces the real R7 scope and protected boundaries. Current metrics:

- `AchReturnsService`: `1066 -> 205` lines;
- extracted target responsibility owners: `0 -> 7`;
- public `IAchReturnsService` methods: `2 -> 2`;
- production `IAchReturnsService` callers: `2 -> 2`;
- controller: `1 GET + 1 POST`, hash unchanged;
- DI composition file: hash unchanged;
- Return transaction owner: `1 -> 1`;
- facade `SaveChangesAsync`: `2 -> 2`; extracted commit sites `0`;
- trace reservation: `1 -> 1`;
- `BuildReturnOutAsync`: `2 -> 2`;
- generic outgoing eligibility evaluation: `1 -> 1`;
- canonical state transition: `1 -> 1`;
- direct HTTP/SFTP/MFT construction in R7 scope: `0`;
- service locator: `0`;
- new blind retry sites: `0`;
- DB schema/migration dependencies introduced: `0`;
- Persistence fallback debt: `57 -> 57`;
- out-of-scope `Contains("CENIT")`: `8 -> 8`;
- concrete `PaymentRailOperationalStrategy`: `0 -> 0`;
- new R7 architecture debt: `0`.

The protected `IAchReturnsService`, controller, DI file, incoming Return service, all dedicated ROR owners/controller, and `NachaFileBuilder` are hash-pinned. R2→R6C ratchets remain applicable and are not loosened.

## Validation status

All host-independent validators from solution graph and R2 through R7 pass with exit 0 on the final tree. `scan_result_solid.py` executes and returns exit 1, so it is **not PASS**. R6C and R7 outputs are byte-identical: 237 Result-pattern findings and 13 SRP heuristic flags, i.e. historical backlog is **identical**, not increased.

The source package has no `.git` metadata. This host has neither `dotnet` nor Docker. Release build, focused R7 tests, R4/R5/R6 regressions, goldens/fingerprint, CENIT ROR and broad backend execution are `NOT_RUN_ENVIRONMENT`; R7 is not `VERIFIED` or `CLOSED`.

## Impact

- `BEHAVIORAL_DIFF: NONE`
- `NORMATIVE_DIFF: NONE`
- `DB_MIGRATIONS: NONE`

No expected result, golden, fixture, NACHA rule/layout/cause/cycle, R10/DEV14 decision, ROR rule, public API, authorization rule, transport behavior, retry/recovery rule, MFT lifecycle, schema, configuration or SPA behavior is intentionally changed.

## Next boundary

Under `LOCAL_ADVANCEMENT`, structurally implemented R7 enables `NEXT_ALLOWED_PHASE=R8A`, the first authorized R8 subphase in the program state. R8A/R8B/R9 are **not executed** in this package.
