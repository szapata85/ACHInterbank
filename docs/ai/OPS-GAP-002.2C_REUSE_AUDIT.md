# OPS-GAP-002.2C — reuse audit

Phase A completed against `cab1c6064281593238288dcb546a0a3fdfbccf4f`, branch `ACH-Interbank-Postgresql`, initially clean worktree. No product edits preceded this classification.

## Evidence owners

- M: `src/Cfa.ACHInterbank.Domain/Models/ACH/AchManagedFileTransfer.cs` — transfer, configuration, events, existing states.
- S: `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchColombiaManagedFileExchangeService.cs` — execution, recovery, queries, detail, commands.
- A: `src/Cfa.ACHInterbank.Api/Controllers/AchColombiaFileExchangeController.cs` — authorized existing API.
- F: `src/Cfa.ACHInterbank.External/Connections/AchColombiaManagedMftFolderAdapter.cs` — atomic folder handoff, claim/recovery, archive, safe adapter diagnostics.
- Q: `src/Cfa.ACHInterbank.Persistence/ACH/Quartz/Jobs/Implementation/AchColombiaManagedMftHandlers.cs` — existing automatic task actors and shared execution.
- P: `src/Cfa.ACHInterbank.Persistence/Configuration/AchManagedFileTransferConfiguration.cs` — common EF mapping, uniqueness, concurrency, retained evidence.
- U: `web/ach-interbank-ui/src/app/features/ach-colombia-file-exchange/` — actual Operations template, component, API service.
- C: `web/ach-interbank-ui/src/app/features/ach-colombia-mft-administration/ach-colombia-mft-administration.component.ts` and `AchColombiaManagedMftConfigurationProvider.cs` — Administration and effective configuration.
- L: `src/Cfa.ACHInterbank.Persistence/ACH/OutgoingTransactionMonitoring/OutgoingTransactionMonitoringQueryService.cs` — RET-GAP-018 exact `AchFileExportTransactions`, dispatch attempts by dispatch-item transaction ID, chamber responses, existing timeline. Incoming command center owns ingestion/transaction detail.

## Reuse gate

| Capability | Current owner | Classification | Evidence | Delta |
|---|---|---|---|---|
| A Transfer identity | M/S/U | REUSE_WITH_SMALL_EXTENSION | Durable ID/hash; detail template omits ID | Display durable identity |
| B HandOff lifecycle | S/F | REUSE_AS_IS | Prepare, persist, attempt, result states | None |
| C Inbound lifecycle | S/F | REUSE_AS_IS | Claim, retained content, ingestion, recovery | None |
| D Outbound lifecycle | S/F | REUSE_AS_IS | Existing builder/envelope/export audit/handoff | None |
| E Retry/recovery | S/U | REUSE_WITH_SMALL_EXTENSION | Existing command guards; UI offers every command in every state | Project command eligibility using existing guards |
| F Duplicate/concurrency | P/S/F | REUSE_AS_IS | Unique identity/hash, optimistic token, atomic move, collision hash verification | None |
| G Archive/retirement | S/U | REUSE_WITH_SMALL_EXTENSION | Durable dates, retirement reason and retained bytes exist | Show dates/reason and actual download availability |
| H Correction lineage | M/S/L/U | REUSE_WITH_SMALL_EXTENSION | Existing predecessor and export/ingestion foreign keys | Navigate existing predecessor and lineage owners |
| I Events/history | M/S/U | REUSE_WITH_SMALL_EXTENSION | Existing history; UI hides result; timestamp-only ordering | Display result; deterministic timestamp/ID ordering |
| J Download | S/A/U | REUSE_WITH_SMALL_EXTENSION | Existing audited content endpoint | Project retained-content availability |
| K Manual execution | S/A/U | REUSE_AS_IS | Existing permissioned commands and confirmations | None |
| L Quartz execution | Q/S | REUSE_WITH_SMALL_EXTENSION | Task code recorded in actor; retries/reprocess reuse initial origin | Record actual execution origin in existing events |
| M Error classification | M/S/U | REUSE_WITH_SMALL_EXTENSION | Durable LastErrorCode/LastAttemptAtUtc omitted from DTO | Expose safe code, timestamp, intervention guidance |
| N Monitoring/search | S/A/U | REUSE_WITH_SMALL_EXTENSION | Date/direction/status/origin/cycle filters; fixed latest 500; UI omits cycle and several states | Expose all states/cycle, filename/ID/archive search and bounded pages |
| O Administration | C/S | REUSE_WITH_SMALL_EXTENSION | Profile/routes/endpoint/retry/retention/credential controls exist; four execution flags round-trip but no controls | Expose existing automatic/manual flags in Administration |
| P Operations UI | U | REUSE_WITH_SMALL_EXTENSION | Existing list/detail/timeline/actions/menu | Extend existing screen only |
| Q HandOff visibility | M/S/U/L | REUSE_WITH_SMALL_EXTENSION | Export/ingestion/correlation evidence retained but not presented | Project identifiers and existing exact-file members; link existing monitors |
| R Audit/security | S/F/A/P | REUSE_AS_IS | Protected credential, redacted audit, safe adapter errors | No credential or raw physical storage path projection |
| S Authorization | A/U | REUSE_AS_IS | CanReadAch / CanManageAch; route guards | Preserve policies |
| T Cross-provider persistence | P/S | REUSE_WITH_SMALL_EXTENSION | Initial Phase A classified REUSE_AS_IS; new provider execution proved the existing list's UpdatedAt.UtcDateTime SQL projection fails on PostgreSQL | Convert the materialized timestamp in memory; no schema delta |
| Enterprise transport/deployment | External operator | OUT_OF_SCOPE_EXTERNAL | Folder boundary is application handoff, not enterprise delivery confirmation | No GoAnywhere/SFTP implementation |

Gate: **PROVEN_PRODUCT_DELTA**. Only extensions named above are authorized by this audit. No new persisted monitoring model, lineage owner, scheduler, business state or transport.

## Administration versus Operations

Administration already loads and saves profile enabled state, endpoint metadata, three routes, retry/retention settings and protected credential metadata. Rotation submits a write-only secret and clears the input after success. The persisted route provider feeds the existing folder adapter. The four execution flags are persisted by the API and consumed by the service but absent from the actual Administration template: expose them there, not in Operations.

RetryDelaySeconds and RetentionDays are stored policy values; the inspected execution path does not establish a scheduled retry deadline or automatic retention purge. Do not invent either in monitoring or reopen 2B policy behavior. Current configured physical routes are not historical per-attempt route snapshots. Existing logical boundary/storage evidence must not be misrepresented as historical deployment configuration. No historical physical-route reconstruction is certified.

## Lifecycle and RET-GAP-018 boundary

Outbound: builder -> envelope -> export audit with exact transaction membership -> immutable transfer/content -> folder handoff -> Transferred/RetryPending/Uncertain/Failed -> existing retry -> retained archive/retirement -> existing events.

Inbound: external folder availability -> atomic processing claim -> immutable transfer -> existing ingestion -> duplicate/restart handling -> archive/retirement -> existing events and ingestion ID.

The transfer's original execution origin is provenance, not necessarily the origin of subsequent attempts. Existing events have their own origin/actor fields; fix future writes at the existing execution boundary without fabricating historical origins. Task actors identify the handler code; individual Quartz fire IDs were not retained and cannot be reconstructed.

RET-GAP-018 already owns exact file membership, deterministic business lineage, dispatch/SOAP attempts and chamber-response timeline. Reuse its membership table and navigate its detail. MFT attempts remain in the existing MFT event history because they are file handoff events, not SOAP transaction dispatch attempts. Inbound business detail remains in the incoming command center.

## Validation

- Final focal backend: `dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --no-build --no-restore --filter FullyQualifiedName~AchColombiaManaged --logger trx;LogFileName=ops-gap-002-2c-final-focal.trx`: 33 passed, 0 failed, 0 skipped. Covers lifecycle, recovery, archive/download, credential redaction, composition, authorization and new projections/origins.
- Angular affected Operations/Administration component specs: 10 passed in Chrome Headless. `npx tsc --noEmit -p tsconfig.app.json`: exit 0. `npx ng build --configuration production`: exit 0, hash `c7c39171fa089e4a`.
- A wider local `FullyQualifiedName~AchColombia` run was intentionally aborted after 77 passed / 0 failed because it retained the test DLL lock. It is **not** passing regression evidence. The overlapping solution build failed with MSB3027/MSB3021 (testhost PID 12384); sequential rebuild and new CI are required.
- Sequential `dotnet build ACHInterbank.sln -c Release --no-restore --maxcpucount:1`: Build succeeded, 0 warnings, 0 errors.
- Certified product commit: `523f5ae530085761ca494ccdb6b6ed6d67a71a46`.
- [Dotnet CI 34161860666](https://github.com/szapata85/ACHInterbank/actions/runs/34161860666): SUCCESS. Build-and-test: 2471 passed / 0 failed / 15 skipped; Outgoing monitor: SQL Server + PostgreSQL 2 passed / 0 skipped, including new MFT detail, exact-file membership, filters, paging and exact UTC timestamp; ReturnOut concurrency: SQL Server + PostgreSQL 4 passed / 0 skipped.
- [Angular CI 34161860829](https://github.com/szapata85/ACHInterbank/actions/runs/34161860829): SUCCESS for build-and-test and existing runtime-backed-e2e (7 passed / 1 skipped). This is regression evidence, not enterprise MFT homologation or a claim that every test is unskipped.
- Final state: **CLOSED — OPERATIONS/HANDOFF MONITORING DELTA IMPLEMENTED AND CERTIFIED**. 2A, 2B, RET-GAP-018, OPS-GAP-006 and RET-GAP-019 remain accepted closed baselines. External enterprise MFT deployment remains separate.
- Project Memory verification: bounded MCP searches/reads for OPS-GAP-002, 2A, 2B, 2C, RET-GAP-018, Managed MFT and HandOff confirm the closure/boundaries. Read-only hash check passed. Stale RET-GAP-018 active/partial references and the closed NACHA-RULE-METADATA.2B Next Job were reconciled; RC-CURRENT identifies the exact certified product commit. UTF-8/LF restored after MCP writes, with semantic-only Git diff.
- CI run `34161388727`, Outgoing monitor job `101863786754`: SQL Server passed; PostgreSQL failed at Managed MFT `QueryAsync` with ``No coercion operator is defined between types 'System.DateTimeOffset' and 'System.Nullable`1[System.DateTime]'.`` The inherited timestamp conversion inside SQL was a proven projection defect. The correction materializes the bounded scalar page before UTC conversion; filters/paging remain in SQL and no content bytes are loaded by the list. This is a narrow 2C monitoring fix, not a redesign of 2A/2B.

## Delivered monitoring projection

| Operator question | Outbound evidence / presentation | Inbound evidence / presentation |
|---|---|---|
| Identity, chamber, direction, filename | Existing transfer ID, ACHCOL scope, direction/name in detail | Same |
| Operational date / cycle | Existing persisted fields | Existing ingestion-resolved fields |
| Source/destination routes | Link to existing Administration, explicitly current configuration | Same; processing claim physical path is not exposed |
| State, attempts, latest attempt | Existing status/count plus LastAttemptAtUtc projection | Same |
| Last successful step | Derived from chronological existing events | Same |
| Failure and intervention | Existing safe error code/message, status and eligible-command projection | Existing ingestion diagnostic and link to command center |
| Timestamps | Creation, last attempt, transfer, processing, archive, retirement | Same, absent steps shown as absent |
| Manual/automatic/task | Initial provenance separately labeled; existing per-event origin/actor including task code | Same |
| Retry / uncertain | Existing states and retry-limit/content eligibility; no invented retry deadline | Existing ingestion state and reprocess guard; no new command |
| Archive / retained content / download | Existing dates/reason; actual retained-content availability | Same |
| Generated/imported file | AchFileExportId | IncomingNachaFileIngestionId and existing detail navigation |
| Business transactions | Exact AchFileExportTransactions IDs -> RET-GAP-018 outgoing detail | Existing ingestion detail owns entry/transaction correlation |
| Correction lineage | Existing CorrectedFromTransferId navigation | Existing ingestion lineage owner |
| Timeline | Existing event model, timestamp then ID, result now displayed | Same |

Historical physical-route snapshots and individual Quartz fire IDs were not recorded and are not synthesized. The deployed enterprise MFT status beyond the application handoff remains external. Previously written event origins are preserved; the corrected origin applies to new attempts/recovery/reprocess events. These evidence limits do not introduce another internal monitoring owner.
