# REFACTOR-CA-SOLID-RES-001 — R6C Incoming Command Center

Date: 2026-10-10  
Mode: `LOCAL_ADVANCEMENT`  
State reached: `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`

## Exact scope confirmed

The R6 plan defines R6C only as the separation of the **Incoming Command Center** into a query/read side and manual operations/commands, with a timeline/read model only where the existing data supports it. The phase prompt adds two hard constraints: **do not change the public API or permissions** and **do not advance to R7**.

In this repository, “Incoming Command Center” means the existing inbound NACHA-M operational surface rooted at `IncomingNachaCommandCenterController` and route `/incoming-nacha-command-center`. It operates over inbound ingestion, classification/linking, dispatch queue, integration executions, processing events and the existing manual dispatch state machine. It is **not** `AchColombiaFileExchangeController`, not a new managed-MFT cockpit, not a CENIT unification layer, and not authorization to introduce a new database/read model or SPA work.

R6C therefore does not add CQRS infrastructure, MediatR handlers, endpoints, tables, migrations, transfer lifecycle actions, MFT execution actions, administration/configuration, credential operations or frontend changes.

## Before → after ownership map

| Concern | R6B input | R6C owner |
| --- | --- | --- |
| UI/API caller | Existing SPA/API calls to `/incoming-nacha-command-center` | Unchanged; SPA files are untouched |
| API controller | 17 actions using one 16-method `IIncomingNachaCommandCenterService` plus orphan-resolution service | Same 17 actions/routes/policies; controller injects query + operations capabilities, while orphan resolution remains separate |
| Query/read boundary | 12 read methods mixed with 4 manual operations in one interface/service | `IIncomingNachaCommandCenterQueryService` + `IncomingNachaCommandCenterQueryService` |
| Manual operations | 4 dispatch mutations mixed with reads | `IIncomingNachaCommandCenterOperationsService` + `IncomingNachaCommandCenterOperationsService` |
| Compatibility | Wide interface declared all 16 methods directly | `IIncomingNachaCommandCenterService` is an aggregate of the two capabilities with 0 direct methods; thin facade delegates for compatibility; production consumers = 0 |
| Orphan manual resolution | Separate `IIncomingNachaOrphanManualResolutionService` | Unchanged and remains separate |
| Read model/timeline | Existing EF projections over inbound persisted entities/events | Same projections moved to query owner; no new persistent read model |
| Audit/events | Manual dispatch transition event persisted by the mixed service | Same `IncomingNachaProcessingEvents` write/event payload in operations owner |
| State transition/idempotency | Existing `IIncomingNachaStateMachineService` + event-based idempotency key check | Unchanged; operations owner delegates to the same state machine and preserves the same replay guard |
| MFT R6A/R6B | Separate ACH Colombia managed-MFT capabilities and seven R6B owners | Unchanged; R6C has no managed-MFT dependency |
| Security | Controller action policies | Unchanged |

## Query/read surface

The command-center query API remains exactly 12 GET actions and 12 query capability methods:

1. observability summary;
2. ingestion list;
3. ingestion detail;
4. ingestion validations;
5. ingestion batches;
6. ingestion transactions;
7. transaction addendas;
8. orphan list;
9. orphan detail;
10. orphan candidates;
11. dispatch queue list;
12. dispatch queue detail.

`IncomingNachaCommandCenterQueryService` owns these projections. It uses the existing `AchDbContext` entities with `AsNoTracking` for read paths and contains no `SaveChangesAsync`, EF update/delete operation, transport execution, managed-MFT action or dispatch mutation.

### Existing read model and timeline

No new table, entity, event store or read-model persistence is introduced. The read side continues to project from existing persisted authority:

- `IncomingNachaFileIngestion` and processing results for file/ingestion state;
- `IncomingNachaDispatchQueue` for operational dispatch state;
- `IncomingNachaEntryClassification` and transaction-link evidence for classification/correlation;
- `IncomingNachaIntegrationExecution` for integration attempts and technical/functional outcome;
- `IncomingNachaProcessingEvents` for event history and the observability timeline.

The observability timeline remains an hourly projection of persisted processing events. Queue and ingestion detail continue to return persisted events ordered with the existing ordering. R6C does not fabricate missing events, change timestamps/actors/correlation, or make logs authoritative.

## Commands / operational actions

The public command-center API remains exactly five POST actions:

- orphan `resolve` — pre-existing and still delegated to `IIncomingNachaOrphanManualResolutionService`;
- queue `retry`;
- queue `unblock`;
- queue `requeue`;
- queue `mark-failed-final`.

Only the four dispatch-queue commands are part of the new operations capability. They keep the existing state-machine transitions and event-based idempotency guard. R6C adds no local retry loop and does not dispatch `Proc_Transacciones` directly.

The following are **not** command-center operations in R6C: managed-file retry/reprocess, archive, retire, audited download, connection test, configuration, credentials, outbound execution or inbound MFT execution. Those remain under the R6A capability contracts and R6B implementation owners exposed by `AchColombiaFileExchangeController` where already applicable.

## Application / Persistence boundary

Application now exposes two cohesive command-center contracts:

- `IIncomingNachaCommandCenterQueryService` — 12 read methods;
- `IIncomingNachaCommandCenterOperationsService` — 4 manual dispatch operations.

The compatibility `IIncomingNachaCommandCenterService` inherits both contracts and declares zero direct methods. `IncomingNachaCommandCenterController` no longer consumes the compatibility aggregate.

Persistence owns the two concrete services and a thin compatibility facade. This follows the repository’s existing service/use-case pattern; no new mediator, command bus or persistent CQRS architecture is introduced.

## Security and authorization

Public routes and policies are unchanged:

- all 12 GET actions retain `P1Policies.CommandCenterRead`;
- orphan resolution retains `CanManageAch`;
- retry retains `P1Policies.CommandCenterRetry`;
- unblock retains `P1Policies.CommandCenterUnblock`;
- requeue retains `P1Policies.CommandCenterRequeue`;
- mark-failed-final retains `P1Policies.CommandCenterMarkFailedFinal`.

The controller remains the HTTP host boundary. Manual actor identity continues to be read from the authenticated principal in API and passed as data to the operations contract; no `HttpContext` dependency is introduced into Application or Persistence.

## Idempotency, concurrency and error semantics

Manual dispatch actions preserve the existing behavior:

- justification and idempotency-key validation are unchanged;
- replay detection uses the same `DispatchTransition` event identity;
- `IIncomingNachaStateMachineService` remains the transition/eligibility owner;
- invalid transitions still persist the same rejected transition audit and raise `InvalidOperationException` for the controller’s existing HTTP 409 mapping;
- successful transitions persist the same applied event and queue state;
- terminal/blocked/waiting-window guards are unchanged;
- cancellation tokens continue through EF operations.

No new claims, locks, concurrency tokens, retry loops, dispatch calls or state meanings are introduced.

## R6A / R6B and MFT boundary

R6C uses **no R6A managed-MFT capability** because the Incoming Command Center in this program is the inbound NACHA dispatch/observability surface, not the ACH Colombia managed-file exchange API. All five R6A contracts and all seven R6B implementation owners remain unchanged. In particular:

- managed-MFT retry/reprocess ownership is unchanged;
- archive/retire/download lifecycle ownership is unchanged;
- inbound/outbound managed-MFT execution is unchanged;
- session/factory/SFTP ownership is unchanged;
- configuration and credential administration remain separate;
- `AchColombiaFileExchangeController` is unchanged.

This avoids creating a false coupling between inbound NACHA dispatch commands and managed-MFT lifecycle operations.

## ACH Colombia / CENIT boundary

R6C does not unify ACH Colombia and CENIT and does not modify `ICenitGatewayTransportAdapter` or CENIT transport. The eight inherited out-of-scope `Contains("CENIT")` sites remain simulation 7 + ingestion 1 = 8. `PaymentRailOperationalStrategy` is not introduced.

## Ratchet

`scripts/validate-r6c-incoming-command-center.py` records:

- command-center API commands: `5 -> 5` (4 dispatch operations + existing orphan resolution);
- command-center API queries: `12 -> 12`;
- controller actions: `17 -> 17`;
- public route/action/authorization surface hash: unchanged;
- wide command-center contract: `16 direct methods -> 0 direct methods` on compatibility aggregate;
- query capability: `0 -> 12` methods;
- operations capability: `0 -> 4` methods;
- production compatibility-facade consumers: `1 -> 0`;
- mixed implementation: `1502` lines -> query `1312`, operations `218`, thin compatibility facade `63`;
- query-side `SaveChangesAsync`: `0`;
- command transition `SaveChangesAsync`: `1 -> 1`;
- direct concrete managed-MFT dependencies from R6C API/Application: `0`;
- direct SFTP/session construction: `0`;
- service locator: `0`;
- new blind retry sites: `0`;
- duplicated retry/reprocess implementation: `0`;
- DB schema changes: `0`;
- authorization gaps: `0`;
- inherited `Contains("CENIT")`: `8 -> 8`;
- `PaymentRailOperationalStrategy`: `0`;
- new R6C architecture debt: `0`.

The R2 persistence fallback ratchet remains `57 -> 57`, and all R2→R6B validators continue to pass.

## Focused tests

R6C preserves the existing `IncomingNachaCommandCenterServiceTests` behavior through the compatibility facade and adds `IncomingNachaCommandCenterSeparationTests` to characterize:

- 12 query methods vs 4 manual operations;
- zero direct methods on the compatibility aggregate;
- query and operations implementations do not implement each other’s capability;
- controller depends on query + operations capabilities and keeps orphan resolution separate.

Existing command-center behavior tests continue to cover query projections, state-machine allowed actions, retry/unblock/requeue/final guards, idempotent replay, audit events, technical/functional execution projection, masking and observability. Existing authorization tests remain unchanged.

## Validation status

Host-independent validators `validate-solution-projects.py` and R2→R6C pass with exit 0. `scan_result_solid.py` still exits 1 and therefore is **not** PASS. Its backlog magnitude is unchanged from R6B: 237 Result-pattern findings and 13 SRP heuristic flags; the textual output changes only because the four manual method line numbers moved in the segregated interface file.

The source package has no `.git` metadata and this host has no `dotnet` or Docker. Build, focused .NET tests, authorization tests, regression suites and runtime/cluster tests are therefore `NOT_RUN_ENVIRONMENT`. R6C is not `VERIFIED` or `CLOSED`.

## Impact

- `BEHAVIORAL_DIFF: NONE`
- `NORMATIVE_DIFF: NONE`
- `DB_MIGRATIONS: NONE`

No endpoint, permission, NACHA-M rule/layout, expected result, golden/fixture, MFT lifecycle, retry/recovery, transport, operational configuration, credential, SPA behavior or persisted state meaning is intentionally changed.

## Next boundary

Under `LOCAL_ADVANCEMENT`, successful structural implementation of R6C enables `NEXT_ALLOWED_PHASE=R7`. R7 is **not executed** in this package.
