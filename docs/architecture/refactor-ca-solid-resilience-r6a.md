# REFACTOR-CA-SOLID-RES-001 — R6A MFT Contract / Interface Segregation

## Scope confirmed

R6A is limited to the plan-defined contract segregation around `IAchColombiaManagedFileExchangeService`. It does **not** split the concrete MFT workflow implementation, change transport mechanics, or refactor the Incoming Command Center. The existing ACH Colombia service remains the single concrete implementation, and the existing MFT/SFTP adapters, session factory, configuration provider and connection tester keep their owners and behavior.

The independent CENIT transport contract `ICenitGatewayTransportAdapter` was inspected and is outside the named R6A target. It remains byte-identical. ACH Colombia and CENIT transport policies are not merged.

## Before: caller → interface → implementation

| Caller | Contract before R6A | Methods actually used | Concrete implementation |
|---|---|---|---|
| `AchColombiaFileExchangeController` | `IAchColombiaManagedFileExchangeService` | all 15 service operations | `AchColombiaManagedFileExchangeService` |
| `AchColombiaManagedMftOutboundHandler` | `IAchColombiaManagedFileExchangeService` | `ExecuteOutboundAsync` | `AchColombiaManagedFileExchangeService` |
| `AchColombiaManagedMftInboundHandler` | `IAchColombiaManagedFileExchangeService` | `ExecuteInboundAsync` | `AchColombiaManagedFileExchangeService` |

The two Quartz handlers therefore depended on 14 operations they did not use. The controller legitimately spans the complete HTTP management surface, but it did not express which capability each endpoint used.

## After: capability contracts

`IAchColombiaManagedFileExchangeService` is retained as a temporary compatibility facade. It declares no operations of its own and aggregates five capability contracts:

| Capability | Operations | Classification |
|---|---|---|
| `IAchColombiaManagedMftExecutionService` | `ExecuteOutboundAsync`, `ExecuteInboundAsync` | workflow execution; side-effecting |
| `IAchColombiaManagedMftTransferReadService` | `QueryAsync`, `GetAsync` | read/query |
| `IAchColombiaManagedMftTransferOperationsService` | `RetryAsync`, `ReprocessAsync`, `ArchiveAsync`, `RetireAsync`, `DownloadAsync`, `TestConnectionAsync` | manual operational commands / connection probe; side-effecting where already side-effecting |
| `IAchColombiaManagedMftAdministrationService` | `GetConfigurationAsync`, `UpdateConfigurationAsync`, `GetAdministrationAsync`, `UpdateAdministrationAsync` | configuration/administration read + update |
| `IAchColombiaManagedMftCredentialAdministrationService` | `SetCredentialAsync` | credential administration; side-effecting |

`DownloadAsync` remains in the operations contract rather than the read contract because the existing implementation records a `Downloaded` audit event and saves it. R6A does not reclassify it as a pure read or alter that behavior. `TestConnectionAsync` remains with manual operational capabilities because it performs the existing connection probe; no connection/session ownership moves.

All 15 pre-existing operations remain with their original names, parameter shapes, `CancellationToken` support, exception behavior and implementation bodies. No one-method-per-interface decomposition was introduced: outbound and inbound execution remain one cohesive execution capability as described by the R6A plan.

## Consumer migration

| Caller | Contracts after R6A |
|---|---|
| `AchColombiaFileExchangeController` | execution + transfer read + transfer operations + administration + credential administration |
| `AchColombiaManagedMftOutboundHandler` | execution only |
| `AchColombiaManagedMftInboundHandler` | execution only |

Production callers no longer inject the compatibility facade. The facade remains solely as an explicit compatibility surface and as the conventionally registered service identity for the unchanged concrete implementation.

## DI / composition root

The existing `[Scoped]` convention continues to register `AchColombiaManagedFileExchangeService` as `IAchColombiaManagedFileExchangeService`. R6A adds scoped composition-root aliases from each capability contract to that existing facade registration. Therefore all capability contracts resolve to the same scoped concrete instance; no duplicate implementation or new lifetime is introduced.

No functional consumer uses `IServiceProvider`, service locator, hidden `new SshNet...`, or direct SFTP session construction. The R2-owned SFTP factory/session construction remains unchanged in the External composition root and authorized factory implementation.

## MFT lifecycle and ownership preserved

R6A changes no implementation body. The following owners remain exactly as in R5D:

- outbound generation, encryption, idempotency and publication orchestration: `AchColombiaManagedFileExchangeService`;
- outbound handoff mechanics and unknown-result reconciliation: existing managed-folder/SFTP adapters;
- inbound discover/claim/download semantics: existing adapter;
- inbound persisted-result recovery and archive completion: existing service + adapter;
- retry eligibility/status ownership: existing service; no new retry policy or retry loop;
- reprocess ownership: existing service / inbound ingestion path;
- archive/retire/download audit behavior: existing service;
- connection/session, host-key validation and credential decryption: existing External connection components;
- protected credential persistence/encryption: existing service/configuration owners;
- duplicate/idempotency guards, content hash, transfer audit/events and concurrency token behavior: existing owners;
- cluster-safety/claim recovery: existing atomic claim and persisted duplicate guards;
- source-file lifecycle and archive ordering: unchanged.

No upload/download/list/move/archive/delete behavior is reordered. No acknowledgment semantics are changed. No retry is added to upload, rename/move, delete or any other operation whose repeatability is not already proven.

## Independent CENIT flow

`ICenitGatewayTransportAdapter` and `CenitLocalGatewayFolderTransportAdapter` remain byte-identical. Their outbound handoff, inbound pickup and archive semantics are not hidden behind the ACH Colombia contracts. R6A introduces no chamber branching into common MFT callers and no shared operational policy across ACH Colombia/CENIT.

## Ratchet

Static R6A measures:

- production wide-service dependencies: `IAchColombiaManagedFileExchangeService` callers `3 -> 0`;
- compatibility facade direct methods: `15 -> 0`, while its inherited aggregate surface remains explicitly documented for compatibility;
- new capability contracts: `0 -> 5` public, `0` internal;
- methods by capability: execution `2`, read `2`, operations `6`, administration `4`, credential `1` (`15` total, unchanged operation inventory);
- consumer-method unused dependency edges: `28 -> 2` (the two Quartz handlers each see only the sibling execution operation in the cohesive execution capability);
- direct concrete MFT dependencies from Application/API/use cases: `0 -> 0`;
- direct SFTP session construction outside the authorized factory: `0`;
- hidden SFTP factory construction outside composition root: `0`;
- service locator in MFT consumers: `0`;
- manual retry owner sites: `1 -> 1`; new blind/local retry sites: `0 -> 0`;
- implementation duplication: `0`;
- Persistence fallback ratchet: `57 -> 57`;
- out-of-scope `Contains("CENIT")`: simulation `7 -> 7`, ingestion `1 -> 1`, total `8 -> 8`;
- `PaymentRailOperationalStrategy` references in R6A scope: `0`;
- new architecture debt: `0`.

`scripts/validate-r6a-mft-interface-segregation.py` additionally pins the R5D hashes of the concrete managed-MFT implementation, ACH Colombia adapters/resolver/connection tester/configuration provider and the independent CENIT transport implementation. This makes R6A fail statically if implementation/lifecycle mechanics are changed under the contract-only phase.

## Focused tests

R6A adds `AchColombiaManagedMftInterfaceSegregationTests` to characterize:

- exact method membership of each capability contract;
- compatibility facade aggregation without new operations;
- consumer constructor dependency on capability contracts instead of the facade;
- `CancellationToken` preservation.

`AchColombiaManagedMftCompositionTests` now also requires all five capability contracts to resolve to the same scoped facade/concrete instance. Existing MFT/SFTP adapter, retry/recovery, window and operational trigger tests are not rewritten to change expected behavior.

## Behavioral / normative / database impact

- `BEHAVIORAL_DIFF: NONE`
- `NORMATIVE_DIFF: NONE`
- `DB_MIGRATIONS: NONE`

No NACHA-M, PUBLISH, cycle/window, filename/path, status, `Transferred` semantics, retry/recovery, credentials, Quartz schedule, hash, audit, source-file lifecycle, ACH Colombia rule or CENIT rule is changed.

## Environment status and next boundary

The source archive has no `.git`, and this host does not provide the .NET SDK or Docker. Host-independent Python ratchets can be executed, but Release build and .NET tests must remain `NOT_RUN_ENVIRONMENT`. R6A therefore can reach only `IMPLEMENTED_UNVERIFIED_ENVIRONMENT` in this environment.

Under `LOCAL_ADVANCEMENT`, successful structural implementation enables `NEXT_ALLOWED_PHASE=R6B`. R6B and later phases are **not executed** in this package.
