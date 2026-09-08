# MFT_EXTERNAL_READINESS_AUDIT

## STATUS

JOB: MFT-EXTERNAL-READINESS-001. AUDIT-ONLY. 2026-09-07.
Final state: **PROVEN_INTERNAL_PRODUCT_DELTA** (Verdict C).
Source examined: `ACH-Interbank-Postgresql`, `fde26d133947309384e3d9fb0b163c9c3c7acd95`; initial working tree clean.
Functional investigation stopped upon proving the manual outbound retry permission bypass below. No implementation or external execution occurred. Other classifications reflect only evidence already inspected, not completed enterprise certification.

## SCOPE

ACH Colombia application-to-enterprise-MFT boundary, Administration consumption, transport semantics and production execution gates. No NACHA format, CENIT, Returns, global UAT or release re-audit.
Product/test/Angular/database changes: NO. Migrations: NONE.

Evidence owners (paths relative to repository root):

| Ref | Current executable owner |
|---|---|
| P | `src/Cfa.ACHInterbank.Application/ACH/Interfaces/IAchColombiaManagedFileExchange.cs`: `IAchColombiaManagedMftAdapter` |
| M | `src/Cfa.ACHInterbank.Application/ACH/Models/AchManagedFileTransferModels.cs`: artifact/result/effective configuration/Administration DTOs |
| F | `src/Cfa.ACHInterbank.External/Connections/AchColombiaManagedMftFolderAdapter.cs`: outbound, pickup, archive, validation |
| S | `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchColombiaManagedFileExchangeService.cs`: shared use cases, Administration, retry, persistence projections |
| C | `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchColombiaManagedMftConfigurationProvider.cs`: `GetEffectiveAsync` |
| D | `src/Cfa.ACHInterbank.Domain/Models/ACH/AchManagedFileTransfer.cs`: transfer/events/configuration |
| O | `src/Cfa.ACHInterbank.Application/ACH/Configuration/AchColombiaManagedMftOptions.cs`; `src/Cfa.ACHInterbank.Api/appsettings.json`, `AchColombiaManagedMft` section |
| DI | `src/Cfa.ACHInterbank.External/DependencyInjectionService.cs`: `AddExternal`, lines 38-40 |
| API | `src/Cfa.ACHInterbank.Api/Controllers/AchColombiaFileExchangeController.cs`: execute/retry/Administration endpoints |
| Q | `src/Cfa.ACHInterbank.Persistence/ACH/Quartz/Jobs/Implementation/AchColombiaManagedMftHandlers.cs`: inbound/outbound `ExecuteAsync` |
| UI | `web/ach-interbank-ui/src/app/features/ach-colombia-mft-administration/ach-colombia-mft-administration.component.ts` |

## ACCEPTED BASELINES

OPS-GAP-002.2A, OPS-GAP-002.2B, OPS-GAP-002.2C, RET-GAP-018, OPS-GAP-006 and RET-GAP-019 remain accepted CLOSED historical baselines. No broad reopening. OPS-GAP-002 remains internally closed with external deployment pending.
The narrow new finding is a direct current-source contradiction of the accepted 2B requirement: manual execution additionally requires the corresponding manual permission. It does not invalidate or re-audit unrelated certified behavior.

Project Memory retrieval: head/TOC, allowed keyword searches, then lines 64-101 only. Existing 2B/2C test and CI evidence accepted as historical evidence; no rerun or new certification claimed. Codebase Memory identified owners before concrete file reads. Its `trace_path` incorrectly resolved `HandoffOutboundAsync` to a CENIT class; that edge was discarded in favor of P/S/DI/F. No index synchronization. Local RAG was unnecessary; no regulatory question was raised. Official OpenAI documentation was consulted only for the requested implementation-model recommendation.

## CURRENT APPLICATION BOUNDARY

ACHInterbank produces an encrypted envelope, retains its immutable bytes/hash and export membership, and hands the envelope to an application-managed filesystem boundary. Enterprise MFT must consume that boundary and deliver externally. Inbound, it must publish complete envelopes to the pickup boundary; ACHInterbank claims, retains, ingests and archives them. `Transferred` currently means filesystem handoff, not remote delivery or counterparty acceptance (S/F).
No GoAnywhere API, direct SFTP connection, remote host authentication or remote receipt polling exists in this execution path. Filling Provider/Protocol/Endpoint metadata does not select another adapter.

## TRANSPORT PORT

**READY_EXTENSION_POINT**, with an explicit semantic contract to preserve; not a claim that every vendor protocol is already supported.

| Port member (Application layer, P/M) | Inputs / result |
|---|---|
| `Enabled` | Boolean capability property; F reports options, while actual calls use C's effective DB profile when present |
| `HandoffOutboundAsync` | File name, bytes, SHA-256, cancellation; `AchManagedMftResult(Succeeded, Retryable, Uncertain, Code, Message, StorageReference)` |
| `PickupInboundAsync` | Cancellation; list of `AchManagedMftArtifact(FileName, Content, ContentSha256, ClaimReference)` |
| `ArchiveInboundAsync` | Claimed artifact, cancellation; archive reference string |

S depends on P, not F. DI registers F as scoped in all environments. A replacement can preserve these operations without changing domain/use-case logic; it must resolve configuration internally and implement stable claim/recovery and idempotent handoff. The port supplies name/hash/bytes, not durable transfer ID or remote acknowledgement callbacks. Inbound pickup/archive failures use exceptions, not `AchManagedMftResult`.
S maps outbound success to `Transferred`, otherwise prioritizes `Uncertain`, then `RetryPending`, then `Failed`. It can reissue retained bytes for pending/uncertain/interrupted outbound work within its retry allowance. Therefore an external adapter must reconcile/idempotently handle repeats; an uncertain result alone does not prohibit a later attempt. The proven manual gate defect is in S, not a reason to redesign P.

## CURRENT ADAPTER CLASSIFICATION

**APPLICATION_MANAGED_FOLDER_BOUNDARY**. Production may enable it; there is no ACH Colombia environment prohibition in F/C/S/DI. Production filesystem suitability is not certified.
Activation uses persisted `ProfileEnabled` and per-direction execution flags. Without a DB row, S initializes from O; C can fall back to O. O defaults disabled; automatic flags default false, manual flags true behind the profile gate. Persisted profile state is authoritative, so setting only `AchColombiaManagedMft:Enabled=false` does not disable an already-enabled DB profile.
Configured DB outbound/inbound/archive routes are consumed; `ProcessingPath` and `MaximumFileBytes` remain deployment options (default limit 10 MiB). This is independent of the CENIT local-gateway guard.

## RESPONSIBILITY MATRIX

These are boundary responsibilities required by current source, not evidence that an enterprise team has accepted them.

| Direction / step | Owner | Evidence or remaining agreement |
|---|---|---|
| Outbound transaction / NACHA / encrypted envelope | ACHINTERBANK | S: builder, filename policy, envelope and export audit |
| Outbound durable transfer / retained content | ACHINTERBANK | S/D, before handoff |
| Outbound staging publication | ACHINTERBANK | F: temporary write then non-overwriting move |
| Staging pickup and custody transition | SHARED_CONTRACT | Must accept final-name publication and agree consumption/reconciliation rules |
| Enterprise MFT transfer / downstream retries | ENTERPRISE_MFT | Deployment and configuration pending |
| External destination receipt/acceptance | EXTERNAL_COUNTERPARTY | Success/acknowledgement agreement pending |
| App archive / monitoring | ACHINTERBANK | S: retained archive and durable events |
| Remote archive / delivery monitoring | ENTERPRISE_MFT | External retention and evidence ownership pending |
| Inbound external publication | EXTERNAL_COUNTERPARTY | Source-side exchange contract |
| Inbound external transport | ENTERPRISE_MFT | Deliver complete envelope to agreed route |
| Pickup publication/custody convention | SHARED_CONTRACT | Atomic publication and exclusive claim semantics |
| Claim / durable transfer / ingestion / business processing | ACHINTERBANK | F and S; existing ingestion owner reused |
| Inbound processing-file archive / monitoring | ACHINTERBANK | F archive after S processing/rejection; retained evidence in D |
| Cross-boundary incident/receipt reconciliation | UNRESOLVED | MISSING_EXTERNAL_CONTRACT, not an automatic product gap |

## ADMINISTRATION READINESS

| Setting | Classification | Consumption / limitation |
|---|---|---|
| Profile enabled | EXISTS_AND_RUNTIME_CONSUMED | S execute gates and C/F effective enablement |
| Outbound automatic permission | EXISTS_AND_RUNTIME_CONSUMED | S `IsEnabled`, Q automatic origin |
| Outbound manual permission | EXISTS_AND_RUNTIME_CONSUMED | Normal execute uses it; explicit retry bypass is the proven blocker |
| Inbound automatic/manual permissions | EXISTS_AND_RUNTIME_CONSUMED | S `ExecuteInboundAsync` / `IsEnabled` |
| Profile/provider/protocol/endpoint/port/principal metadata | EXISTS_BUT_NOT_RUNTIME_CONSUMED | Persisted/projected by S and exposed by API/UI; not transport dispatch/authentication inputs |
| Outbound/inbound/archive routes | EXISTS_AND_RUNTIME_CONSUMED | C to F; outbound archive is retained DB content, not a move to ArchiveLocation |
| Processing route / maximum bytes | EXISTS_AND_RUNTIME_CONSUMED | O through C/F; deployment configuration, outside Administration UI |
| Maximum retries | EXISTS_AND_RUNTIME_CONSUMED | S eligibility/count checks |
| Retry delay seconds | EXISTS_BUT_NOT_RUNTIME_CONSUMED | Validated, persisted and projected; inspected S/Q/F do not enforce a delay from this field |
| Retention days | EXISTS_BUT_NOT_RUNTIME_CONSUMED | Validated, persisted and projected; inspected archive/retire path has no timed retention enforcement |
| Credential metadata and protected value | EXISTS_BUT_NOT_RUNTIME_CONSUMED | S write protection and safe read metadata exist; F does not authenticate with the stored credential |
| OS/share identity, remote credentials and remote routes | EXTERNAL_ONLY | Infrastructure/MFT provisioning |

No extra internal delta is inferred from the metadata-only fields: a requirement for application-enforced delay/expiry or vendor authentication has not been established. They must not be represented as already-enforced deployment semantics. No new application-owned setting was proven missing for the existing filesystem boundary.

## CREDENTIAL READINESS

**EXTERNAL_CREDENTIAL_PROVISIONING_ONLY** for the current folder boundary. S `SetCredentialAsync` protects a type-tagged secret through `IEncryptionService`; read DTOs expose configured/type/update metadata only. Existing PostgreSQL/SQL Server at-rest security remains accepted.
F uses process filesystem access, not stored Principal/Endpoint/ProtectedCredential. No credential runtime resolver is present in this path; absence of a DB MFT secret does not block folder operations and is not an anonymous remote-login fallback. OS/share service identity must be provisioned externally. Sufficiency for a hypothetical authenticated remote adapter is **NOT_PROVEN** until its contract is known. No secret values, ciphertext or real payloads were inspected. No secret-manager change is proposed.

## CONNECTIVITY / INFRASTRUCTURE

| Requirement | Classification |
|---|---|
| Mounted/shared staging, durable processing storage, rename semantics, network path/DNS/firewall/allowlists/ports as selected by topology | INFRASTRUCTURE_REQUIRED |
| Application process/share identity, ACLs, enterprise service identity, remote authentication material and applicable certificates | SECURITY_PROVISIONING_REQUIRED |
| Enterprise MFT availability/deployment, routes, schedules, filters, destination connection and external archive | ENTERPRISE_MFT_CONFIGURATION_REQUIRED |
| Filesystem versus another boundary; custody, receipt, replay and reconciliation rules; homologation environment and acceptance owner | MISSING_EXTERNAL_CONTRACT |

The code cannot provision or certify any of these. No external endpoint was contacted and no enterprise MFT configuration was changed.

Minimum dependency contract:

| Concept | Current-app classification |
|---|---|
| Accessible staging filesystem; outbound/inbound/processing/archive routes; process identity and filesystem permissions | REQUIRED_BY_CURRENT_APP |
| Final-file pickup/publication convention; usable non-overwriting rename/move semantics | REQUIRED_BY_CURRENT_APP |
| Endpoint identifier, remote host, DB transport principal/authentication material, GoAnywhere API | NOT_REQUIRED_BY_CURRENT_APP (metadata may still be stored) |
| OS authentication material mechanism / shared mount topology | UNKNOWN_UNTIL_EXTERNAL_CONTRACT |
| Remote success definition, acknowledgement, downstream retries/archive/duplicates | UNKNOWN_UNTIL_EXTERNAL_CONTRACT |
| App-side retry eligibility, duplicate protection and retained archive | REQUIRED_BY_CURRENT_APP; existing source owns these |

## TRANSFER CONTRACT

| Concept | Outbound | Inbound |
|---|---|---|
| Atomic publication | APPLICATION_CONTRACT_ALREADY_DEFINES: `.{name}.{guid}.tmp` write then final non-overwriting `File.Move` | EXTERNAL_CONTRACT_MUST_DEFINE: publisher must expose complete final files atomically |
| Pickup/temp convention | EXTERNAL_CONTRACT_MUST_DEFINE: MFT ignores temporary files | APPLICATION_CONTRACT_ALREADY_DEFINES: only non-dot `.env` files; exclusive-open readiness check, move to processing, recovery reads processing |
| Ownership transition/completion | APPLICATION_CONTRACT_ALREADY_DEFINES: final local publication is handoff success; external custody must be agreed | APPLICATION_CONTRACT_ALREADY_DEFINES: move to processing is claim; ingestion completion is a later event |
| Remote success/acknowledgement | EXTERNAL_CONTRACT_MUST_DEFINE: no remote receipt operation in P | EXTERNAL_CONTRACT_MUST_DEFINE: claim/ingestion does not acknowledge upstream remotely |
| Retry ownership | APPLICATION_CONTRACT_ALREADY_DEFINES: application handoff attempts; EXTERNAL_CONTRACT_MUST_DEFINE downstream delivery retry ownership | APPLICATION_CONTRACT_ALREADY_DEFINES recovery/ingestion reuse; EXTERNAL_CONTRACT_MUST_DEFINE upstream retransmission |
| Manual retry permission | PROVEN_INTERNAL_BLOCKER: disabled manual flag bypassed | NOT_APPLICABLE to outbound `RetryAsync` |
| Uncertain delivery | APPLICATION_CONTRACT_ALREADY_DEFINES result/state and later reattempt; EXTERNAL_CONTRACT_MUST_DEFINE idempotent reconciliation after MFT consumes staging | EXTERNAL_CONTRACT_MUST_DEFINE upstream uncertainty; P pickup/archive expose exceptions rather than typed uncertainty |
| Duplicate/replay | APPLICATION_CONTRACT_ALREADY_DEFINES durable export/content protection and existing-target hash match/collision; EXTERNAL_CONTRACT_MUST_DEFINE remote dedup after target removal | APPLICATION_CONTRACT_ALREADY_DEFINES hash/size duplicate and same-name-different-content handling; EXTERNAL_CONTRACT_MUST_DEFINE upstream replay convention |
| Archive timing | APPLICATION_CONTRACT_ALREADY_DEFINES retained DB archive on handoff success; external staging removal is separate | APPLICATION_CONTRACT_ALREADY_DEFINES physical archive after processing/rejection and duplicate paths; archive is not proof of business acceptance |
| Retention | APPLICATION_CONTRACT_ALREADY_DEFINES explicit retirement preserves history; EXTERNAL_CONTRACT_MUST_DEFINE required timed retention and downstream ownership | Same; `RetentionDays` is not evidence of a purge scheduler |
| Correction | APPLICATION_CONTRACT_ALREADY_DEFINES eligible predecessor and a new linked transfer | APPLICATION_CONTRACT_ALREADY_DEFINES eligible reprocess through existing ingestion linkage |

Filesystem correctness is conditional on deployment semantics. In particular, inbound-to-processing and processing-to-archive moves need compatible storage; an exclusive-open probe alone is not a publication protocol. No GoAnywhere behavior is inferred.

## OBSERVABILITY

**REUSE_AS_IS**: D/S preserve transfer status, attempts, event result/order/origin, timestamps, retained bytes, archive and existing export/ingestion/correction lineage. **REQUIRES_EXTERNAL_ADAPTER_MAPPING**: a replacement maps success/retryability/uncertainty and safe code/message/storage reference into M. Messages/codes must already be safe: S persists the adapter's supplied text. Inbound exceptions require adapter-defined safe handling within the existing port semantics.
No **PROVEN_INTERNAL_MODEL_GAP**. Separate remote delivery telemetry is an external contract concern; existing `Transferred` cannot be relabeled as remote acceptance. Physical pickup exceptions before an artifact exists do not constitute a new durable transfer outcome.

## KNOWN EVIDENCE LIMITS

Historical route snapshot verdict: **NOT_PROVEN** required for enterprise homologation/auditability; no approved external or regulatory requirement supplied. Current routes remain available in Administration. No snapshots proposed.
Quartz fire ID verdict: **NOT_PROVEN** required externally; not needed by the inspected application calls. Existing execution origin remains accepted; no persistence change proposed.
Old event origins remain unchanged. Historical CI/E2E proves only its stated application scope. The test named `ProductionComposition_WithManagedMftDisabled_ShouldResolveManagedMftAndSchedulerGraph` actually creates `Environments.Development`; its name cannot certify Production execution.
This audit used current executable source/control-flow evidence, not a new runtime reproduction. Existing test sources inspected: `tests/Cfa.ACHInterbank.Tests/AchColombiaManagedMftFolderAdapterTests.cs` and `AchColombiaManagedMftCompositionTests.cs`. No tests/build/provider matrix/Playwright/external homologation ran. Findings after the stop point were not expanded into further investigations.

## PRODUCTION FAIL-CLOSED

**NON_COMPLIANT**, narrowly because a manual outbound retry can publish despite `ManualOutboundAllowed=false`. This is an application-owned permission requirement independent of vendor behavior.

Other observed protections: disabled defaults; normal execute checks direction permission and profile; C requires configured routes; F rejects blank/root routes and unsafe outbound names, validates bytes/hash, and reports access/hash/collision failures. Missing configuration can throw at runtime; no MFT-specific startup validation or Production-only prohibition is present in the inspected registration/path. Some filesystem exceptions occur outside the typed result try/catch and propagate; they do not prove delivery. The adapter cannot choose a remote network protocol from endpoint metadata. Production enablement of an explicitly configured folder boundary is not itself the non-compliance.

The DB profile gate still blocks F's handoff when disabled; the demonstrated case keeps it enabled and turns off only manual outbound execution. Do not claim the profile switch is bypassed or that unauthenticated users can invoke the endpoint.

## HOMOLOGATION PLAN

Readiness below identifies each scenario's immediate prerequisite; all real exchanges also require an authorized homologation environment, agreed contract, provisioned identities/storage/network and approved operations. READY_TO_EXECUTE means an existing local/application check can be selected, not that it ran here. Deployment acceptance remains blocked by the internal finding.

| Scenario | Readiness | Evidence to collect |
|---|---|---|
| Outbound automatic/manual normal execution | NEEDS_MFT_CONFIGURATION | Flags/schedule or authorized manual call, one durable identity and published artifact |
| Outbound successful external handoff | NEEDS_EXTERNAL_CONTRACT | Final-file custody receipt, hash and distinction from remote acceptance |
| Outbound retryable failure/manual permission denial | INTERNAL_BLOCKER | After 2D, disabled manual retry makes zero adapter calls; permitted retry retains same identity/bytes |
| Outbound uncertain outcome | NEEDS_EXTERNAL_CONTRACT | Disconnect/consume-before-confirm scenarios reconciled without remote duplication |
| Outbound duplicate/replay protection | NEEDS_EXTERNAL_CONTRACT | Existing-target hash match/collision plus consumed-target remote reconciliation |
| Outbound archive | NEEDS_EXTERNAL_CONTRACT | DB retained archive versus MFT staging-removal/downstream retention evidence |
| Outbound monitoring visibility | READY_TO_EXECUTE | Existing detail/history/export membership against synthetic transfer |
| Inbound automatic/manual pickup | NEEDS_MFT_CONFIGURATION | Publish eligible complete envelope and exercise configured trigger |
| Inbound successful claim | NEEDS_NETWORK | Agreed mount/move semantics, sole processing claim and retained identity |
| Inbound restart recovery | NEEDS_MFT_CONFIGURATION | Restart after claim and after durable receipt; resume without double ingestion |
| Inbound duplicate protection | NEEDS_EXTERNAL_CONTRACT | Exact-content replay and changed-content/same-name response agreed upstream |
| Inbound ingestion | NEEDS_CREDENTIAL_PROVISIONING | Existing envelope certificate/key prerequisites plus agreed fixture and linked ingestion result |
| Inbound archive | NEEDS_MFT_CONFIGURATION | Claim leaves processing; success/rejection and archive history remain distinguishable |
| Inbound monitoring visibility | READY_TO_EXECUTE | Existing detail/history/ingestion navigation against synthetic transfer |
| Credential secrecy | READY_TO_EXECUTE | Existing safe Administration response checks with synthetic values only |
| Authorized operations | INTERNAL_BLOCKER | API authorization plus profile/direction permission enforced on manual retry |
| File integrity/hash | NEEDS_EXTERNAL_CONTRACT | Same envelope bytes/hash at each agreed boundary; no unsupported remote hash guarantee |
| Exact durable transfer traceability | READY_TO_EXECUTE | Existing durable ID/export membership/ingestion/predecessor links |

## REQUIRED EXTERNAL INFORMATION

Six questions for the MFT/infrastructure team; no secrets requested:

1. Which product/version and homologation topology will consume/publish the current filesystem boundary? If it cannot, what approved alternative boundary is required?
2. What are outbound, inbound, processing and archive paths/mounts, process/service identities and ACL ownership; what network/DNS/firewall/ports and authentication/certificate provisioning are required?
3. Can publication use complete final `.env` files, ignore dot/temp files, and support exclusive non-overwriting moves with recoverable processing storage?
4. When does MFT accept custody, when may it remove outbound staging, what counts as remote success, and is a separate acknowledgement required? How are ambiguous handoff and replay reconciled after staging removal?
5. Who owns each retry and archive/retention operation on both sides, including duplicate/rejected files and incident escalation? Are timed application retention/delay, historical route snapshots or Quartz fire correlation actually required?
6. Who provides the homologation environment/fixtures and signs operational acceptance with linked application and enterprise delivery evidence?

## INTERNAL PRODUCT DELTA

**OPS-GAP-002.2D proposed: enforce manual outbound permission on explicit retry. Implementation not authorized by this audit.**

Exact source proof at audited commit:

1. API `POST transfers/{id}/retry` requires `CanManageAch` and calls S `RetryAsync`.
2. S lines 222-232 checks actor/key, outbound direction, `RetryPending` or `Uncertain`, retained content and retry allowance. It does not check `ManualOutboundAllowed`.
3. S line 231 calls `HandoffAsync(..., Manual, ...)`; lines 385-392 persist an attempt and invoke P.
4. C `GetEffectiveAsync` passes `ProfileEnabled` and paths only. F `HandoffOutboundAsync` checks effective Enabled, bytes/hash/path and target state; no manual permission exists in its inputs/configuration.
5. DI registers that F without an environment restriction. Therefore Production follows the same path.

Concrete reproducer specification (not executed): an authorized manager, profile enabled, manual outbound disabled, valid accessible routes, retained outbound `RetryPending` transfer with retry allowance remaining, and absent staging target. Calling retry reaches F and can commit the final file. Required behavior is zero handoff when manual outbound is disabled. Normal `ExecuteOutboundAsync` already gates the same manual origin through S `IsEnabled` (lines 42 and 475-476).

Root cause/owner: S explicit retry entry point skips the direction/origin execution gate used by normal execution. The accepted 2B `PROFILE_CONTRACT` supplies the requirement; no GoAnywhere contract is needed. This is one permission-enforcement delta, not missing enterprise transport.

Acceptance criteria for the separate implementation JOB:

- Both retry-eligible statuses with `ManualOutboundAllowed=false` must produce zero adapter calls and zero outbound attempt side effects, even for `CanManageAch` users and an enabled profile.
- Require the profile and manual outbound permission before an explicit manual handoff; disabled profile must remain fail-closed.
- With both permitted, preserve retained bytes/hash/identity, retry limits, definitive-success replay prevention and actual Manual event origin.
- Add a focal service regression using a spy/fake adapter and API-path coverage where needed; cover enabled/disabled gates and retain existing automatic execution behavior. Validate affected tests/build without claiming external homologation.

Exclusions: no vendor adapter/API/SFTP, schema, new secret manager, NACHA engine/format, monitoring owner, historical route snapshots, Quartz fire persistence, retry-delay/retention implementation or broad baseline reopening. No changes to these were made.
Recommended model/effort: GPT-6 Astra / high, an engineering recommendation for the bounded permission fix and regression review; coding and high effort support are documented by [OpenAI](https://developers.openai.com/api/docs/models/gpt-6-astra).

## FINAL VERDICT

**PROVEN_INTERNAL_PRODUCT_DELTA**. Exactly one independently implementable blocker is documented: explicit outbound retry bypasses the configured manual outbound permission. OPS-GAP-002.2D meets the creation gate solely for that behavior. No implementation occurred.
Enterprise deployment, credentials, connectivity, transfer contract and homologation remain external work regardless of 2D. Fixing 2D will not certify those dependencies or automatically establish application readiness for an unapproved vendor contract.

## RECOMMENDED NEXT JOB

Select the narrowly scoped **OPS-GAP-002.2D** above for implementation and focal verification, then resume the external contract/readiness decision with the six answers. Keep `NEXT_JOB` in existing Project Memory unchanged pending prioritization. Documentation-only audit; no push or external operational action.
