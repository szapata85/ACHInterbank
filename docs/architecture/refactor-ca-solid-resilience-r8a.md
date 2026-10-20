# REFACTOR-CA-SOLID-RES-001 — R8A ReportsController modularization

## Phase state

- Phase: `R8A`
- Execution mode: `LOCAL_ADVANCEMENT`
- State: `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`
- Source SHA: `UNAVAILABLE_SOURCE_ARCHIVE_NO_GIT_METADATA`
- Behavioral diff: `NONE`
- Normative diff: `NONE`
- DB migrations: `NONE`
- Next allowed phase after this increment: `R8B` (not executed here)

## Exact plan scope

The implementation plan defines R8A only as modularization of the 1,028-line `ReportsController`: split endpoints by report resource/capability, preserve the existing public routes through equivalent attributes, prefer existing dedicated handlers/query services, and do not duplicate controller logic. Acceptance requires API-contract compatibility, no relevant OpenAPI/Scalar regression, identical authorization policies, no controller-created concrete dependencies, and no useless wrappers.

R8B secondary large-class cleanup, R9 deterministic-time/async work, and any residual debt outside `ReportsController` are explicitly outside this increment.

## Responsibility classification

- **E — API (primary):** HTTP routing, authorization metadata, response mapping, report endpoint grouping and OpenAPI-facing MVC controller identity.
- **B — Application/use case (delegation only):** controllers continue to delegate to the already-existing report services; no new application use case or application contract was introduced.
- **F — Observability (preserved):** traceability report logging/timeout handling moved with its endpoint and preserves the legacy logger category.
- **A/C/D/G:** no domain-policy, persistence, external-transport, or security-policy ownership moved in R8A.

## Ownership before -> after

| Concern | Before R8A | After R8A |
|---|---|---|
| 20 `/api/reports` actions | one `ReportsController` | seven capability/resource controllers |
| transaction reports | `ReportsController` | `TransactionReportsController` |
| Return/rejection reports | `ReportsController` | `ReturnRejectionReportsController` |
| NACHA-file/cycle reports | `ReportsController` | `NachaCycleReportsController` |
| reconciliation reports | `ReportsController` | `ReconciliationReportsController` |
| audit/history reports | `ReportsController` | `AuditHistoryReportsController` |
| traceability PDF | `ReportsController` | `TraceabilityReportsController` |
| accounting-review export | `ReportsController` | `AccountingReviewReportsController` |
| repeated clearing-house request validation | private helper in `ReportsController` | one protected helper in `ReportsClearingHouseControllerBase` |
| report query/generation logic | existing Application/Persistence report services | unchanged existing services |

The old endpoint controller is retired. The seven new endpoint controllers all keep `[Route("api/reports")]`, `[Authorize]`, the original action route/verb metadata, and `CanReadAch` on all 20 actions. `ControllerContractNameAttribute("Reports")` is an MVC controller-model convention that preserves the logical `ControllerActionDescriptor.ControllerName` value `Reports`, so the unchanged OpenAPI operation-id and integration-tag transformers continue to see the pre-R8A controller identity.

## Contracts, callers and DI

### Public HTTP contract

- Public HTTP actions: `20 -> 20`.
- Route prefix: `api/reports -> api/reports`.
- Verbs/action templates: exact pre-R8A map preserved.
- Action authorization: `CanReadAch` remains on `20/20` actions; controller-level authenticated boundary remains on every modular controller.
- DTOs, query/body parameters, response types, response descriptions, endpoint summaries/descriptions and file content types are copied unchanged with their actions.
- Existing OpenAPI operation-id/tag transformers are byte-identical to R7; logical MVC controller name remains `Reports` through the controller-model convention.

### Service contracts and callers

No Application report interface changed. Existing dependencies remain:

- `IReportGenerator`
- `IAchTransactionReportService`
- `IAchReturnRejectionReportService`
- `IAchNachaCycleReportService`
- `IAchReconciliationReportService`
- `IAchAuditHistoryReportService`
- `IClearingHouseService`
- `IAccountingReviewExportAppService`

The caller topology changes only inside the API presentation layer: each action now resides on its capability controller and calls the same service method with the same request/filter construction. No production service caller was added or removed outside `/api/reports`.

### DI

No registration or lifetime change was required. MVC constructor activation remains the owner. Maximum constructor fan-in is reduced from `9 -> 3`; controllers receive only abstractions (`I*`) or `ILoggerFactory`. No service locator or concrete operational dependency is introduced.

## Behavior characterization

Before moving code, the R7 action bodies/attributes and helper behavior were treated as the compatibility contract. Static post-move comparison confirms all 20 action documentation/attribute/method blocks are text-identical to the R7 source after line-ending normalization. `NormalizeDateRange` is likewise text-identical after line-ending normalization. Clearing-house validation has identical normalized body text after the required access-modifier change from private to protected.

Focused characterization was added/updated for:

- exact 20-action route/verb/policy surface and logical `Reports` controller identity;
- OpenAPI contract lookup across the modular controllers;
- traceability PDF success, invalid ranges, timeout and fast execution;
- accounting-review endpoint construction/export behavior;
- accounting-review boundary characterization after the endpoint becomes its own controller.

No expected product output, golden file or normative fixture was changed.

## Side effects, persistence and transactions

The report controllers did not own database writes or transactions before R8A and do not own them after R8A.

- `SaveChangesAsync` in R8A controller scope: `0 -> 0`.
- explicit begin/commit/rollback in R8A controller scope: `0 -> 0`.
- state-transition ownership in R8A controller scope: `0 -> 0`.
- HTTP/SOAP/WCF/MFT/SFTP transport ownership in R8A controller scope: `0 -> 0`.
- retry/recovery ownership in R8A controller scope: `0 -> 0`.
- idempotency/hash/concurrency-token ownership: unchanged in existing downstream services; no new guard or bypass is introduced.
- audit persistence ownership: unchanged. Traceability endpoint logging remains presentation observability only.

The traceability endpoint preserves its 30-second linked cancellation timeout and error/status mapping. Its logger is created with the exact legacy category `Cfa.ACHInterbank.Api.Controllers.ReportsController`, avoiding a log-category behavior change caused solely by the class split.

## Security and rail isolation

Authorization policies and authenticated controller boundaries are unchanged. R8A introduces no `HttpContext` dependency into Application, no secret/certificate change, and no logging of new sensitive values.

R8A is rail-neutral reporting presentation work. It changes neither ACH Colombia nor CENIT policy, layouts, causes, cycles, windows, Return/ROR logic, managed-MFT semantics, CENIT transport, or SFTP/session behavior. The eight known out-of-scope productive `Contains("CENIT")` sites remain unchanged (`7 + 1`).

## Architecture ratchet

`scripts/validate-r8a-reports-controller-modularization.py` requires all prior solution/R2-R7 ratchets and enforces only R8A-specific ownership/compatibility:

- legacy `ReportsController`: `1 / 1028 lines -> retired`;
- endpoint controllers: `1 -> 7`;
- public report actions: `20 -> 20`;
- max controller constructor dependencies: `9 -> 3`;
- clearing-house validation owners: `1 -> 1` and call sites `14 -> 14`;
- controller concrete operational dependencies/service locator: `0 -> 0`;
- controller DB writes/transactions/external transports/retry ownership: `0 -> 0`;
- Persistence fallback allowlist: `57 -> 57`;
- out-of-scope `Contains("CENIT")`: `8 -> 8`;
- concrete `PaymentRailOperationalStrategy` references: `0 -> 0`;
- new R8A architecture debt: `0`.

The broad `scan_result_solid.py` remains an executed failure for historical backlog: R7 baseline and final R8A raw output are byte-identical, with `237` Result-pattern findings and `13` SRP flags. Classification: `UNCHANGED`; it is not PASS.

## Protected-scope invariants

R8A does not authorize changes to `tests/**/TestData`, six NACHA goldens, fingerprint characterization assets, CENIT ROR fixtures, `docs/normativa`, PostgreSQL/SQL Server migrations, R4 outbound, R5 inbound, R6 managed-MFT/Command Center, R7 Returns, SFTP/session factory, CENIT transport, or SPA. Final R7->R8A byte comparison must keep these protected areas unchanged.

## Environment gaps

The R7 archive contains no `.git`, so current Git SHA/status evidence is unavailable. This host also lacks `dotnet` and Docker. Therefore Release build, focal API/OpenAPI/authorization tests, architecture tests, R7/R6/R5/R4 regressions, golden/fingerprint/CENIT ROR tests, broad backend suite, and Docker runtime/E2E cannot be executed here and remain `NOT_RUN_ENVIRONMENT`.

Under `LOCAL_ADVANCEMENT`, that limitation does not block a structurally implemented R8A, but it does prevent `VERIFIED`/`CLOSED` status.
