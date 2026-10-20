# REFACTOR-CA-SOLID-RES-001 — R3 external resilience and secure integration logging

## State

- Phase: `R3`
- State: `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`
- Execution mode: `LOCAL_ADVANCEMENT`
- Current Git SHA: `UNAVAILABLE_SOURCE_ARCHIVE_NO_GIT_METADATA`
- Prior phases: R0/R1/R2 remain `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`
- Next allowed phase: `R4A` (not executed in this increment)

R3 is structurally implemented on the delivered R2 tree. It is not `VERIFIED` or `CLOSED`: the archive has no `.git` metadata and this host has no `dotnet` or Docker.

## Exact R3 scope implemented

### R3A — managed HTTP/SOAP clients

All production `new HttpClient()` construction was removed from the R3 target paths. `IHttpClientFactory` now owns handler lifetime through explicit named clients:

- `ach-health-probe-idempotent` for health GET probes and the internal server probe endpoint;
- `ach-generic-http-no-retry` for the generic/token HTTP adapter;
- `ach-wscfaach-soap-no-retry` for WSCFAACH SOAP-over-HTTP;
- `ach-wsaxon-soap-no-retry` for WSAXON response notification SOAP-over-HTTP.

`HealthProbeClient` implements the Application `IHealthProbeClient` port so Application no longer owns HTTP transport. `ServersController` also resolves the explicit health client rather than receiving an unqualified default `HttpClient`.

`Resilience:ExternalHttp` contains operation-oriented profiles. Only the health GET profile receives transport retry/circuit-breaker/concurrency-limiter/timeout because GET health probing is idempotent and duplicate probes do not create financial side effects. The generic HTTP and SOAP named clients deliberately receive **no transport retry, circuit breaker or bulkhead rejection policy**: their operation semantics are not uniformly idempotent and an uncertain result must remain observable to the owning workflow rather than be replayed by transport middleware.

Timeout ownership is explicit:

- health GET: `Resilience:ExternalHttp:HealthProbe:TimeoutSeconds` (5 seconds by default in the supplied configuration);
- generic HTTP/token POST: 100 seconds, preserving the legacy `HttpClient` default timeout while moving construction to the factory;
- WSCFAACH SOAP: the persisted integration mapping remains timeout authority per operation;
- WSAXON notification SOAP: the persisted integration mapping remains timeout authority.

The previous global `Resilience:Soap` pipeline on WSCFAACH had a default retry count of three and a separate global timeout. R3 removes that transport replay layer so monetary/side-effecting SOAP operations are not blindly duplicated and mapping-specific timeouts remain authoritative.

### R3B — WCF authorization transport

`ConnectionAuth` remains the WCF transport adapter and now records explicit timeout/classification behavior without adding retry. Binding open/send/receive/close timeouts continue to come from `WcfAuthorizationOptions` (`appSettings:prueba`).

The WCF operations `WCFBM`, `WCFPSE`, `WCFTransfiya` and `WCFRedeban` are treated as side-effecting or not-proven-idempotent. R3 therefore does not replay them. Timeout returns the existing `"timeout"` sentinel; other failures preserve the legacy empty-string outcome. Communication failures are classified for diagnostics as transient/uncertain, definitive remote fault, or generic uncertain, but classification does not itself cause retry. Faulted clients are aborted and non-faulted clients are closed safely.

### R3C — secure logging

R3 removes target-path logging that could disclose secrets or financial payloads:

- no generated curl containing headers/body/bearer/API keys;
- no raw generic HTTP response body in logs or exception text;
- no raw WCF request/response payload;
- no raw SOAP/XML response body on failures;
- no serialized health/server endpoint objects;
- no password-reset recipient/token link;
- no client credentials or client assertions from token-generation failures.

Integration logs retain only safe operational metadata such as operation name, sanitized endpoint, elapsed time, HTTP status/body length, exception class, retry ownership and uncertainty classification. Endpoint sanitization strips embedded credentials, query and fragment.

### R3D — fault-injection and characterization coverage

`ExternalHttpResilienceTests` adds host-executable test source for:

- transient HTTP 5xx followed by success on idempotent health GET;
- synthetic connection failure followed by success on health GET;
- retries exhausted;
- timeout with retry disabled;
- caller cancellation without replay;
- circuit-open behavior on the idempotent health client;
- definitive generic POST HTTP error with exactly one transport attempt and sensitive-body/token redaction.

SOAP characterization tests now assert exactly one transport request on HTTP failure for WSAXON/WSCFAACH. Existing `NotificarRespuestaAchUseCaseTests` continue to characterize a WSAXON timeout as an uncertain result requiring manual review rather than a blind transport replay.

## Operation-by-operation resilience decision

| Operation | Idempotency / side-effect assessment | Timeout owner | Retry owner | R3 transport policy | Duplication safety rationale |
|---|---|---|---|---|---|
| Health probe GET | Idempotent observation | Health profile (5s default) | Transport health client | bounded transient retry + circuit + concurrency limit | duplicate GET probes do not create payment side effects |
| Internal `ServersController` GET probe | Idempotent observation | Health profile | Transport health client | same health named client | same GET-only semantics; response body contract preserved |
| Generic/token HTTP POST | Idempotency not guaranteed | Generic profile, 100s legacy-equivalent timeout | Calling workflow, if any | no transport retry/circuit/bulkhead | a replayed POST could duplicate external side effects; R3 does not infer safety |
| WSCFAACH `Proc_Transacciones` | Monetary/side-effecting; not safe for blind replay | persisted SOAP mapping | persisted dispatch/idempotency workflow | no transport retry/circuit/bulkhead | timeout/transport failure can be outcome-uncertain; replay risks duplicate processing |
| WSCFAACH `Proc_Contrapartidas` | Monetary/side-effecting; not safe for blind replay | persisted SOAP mapping | contrapartida dispatch/reconciliation workflow | no transport retry/circuit/bulkhead | uncertain delivery must remain visible to reconciliation rather than be replayed |
| WSCFAACH `PLValidarUsuarioBV` | Read-like but formal idempotency guarantee is not established | persisted SOAP mapping | caller | no transport retry/circuit/bulkhead | shared transport does not assume semantics that are not documented |
| WSAXON `RegistrarRespuestaTransaccion` | Non-monetary notification with existing idempotency/audit workflow, but timeout is explicitly outcome-uncertain | persisted SOAP mapping | notification attempt/reprocess workflow | no transport retry/circuit/bulkhead | application-level attempt state/manual review owns uncertainty; transport replay would bypass that audit boundary |
| WCF BM/PSE/Transfiya/Redeban | side effects/idempotency not proven | WCF binding option | caller/workflow | no retry; classify only | retry without protocol-level idempotency could duplicate authorization-side effects |

No retry was added to MFT/SFTP. Existing MFT retry/recovery, idempotency keys, publication/uncertainty semantics and cluster-safety remain owned by the existing MFT workflow.

## Architecture ratchet

R3 tightens the existing architecture ratchet:

- production source must contain zero direct `new HttpClient()` constructions;
- the former Application manual-`HttpClient` allowlist entry is removed;
- API actions may not receive an unqualified `[FromServices] HttpClient` for these external paths;
- required named clients must remain registered;
- the old global `Resilience:Soap` block is forbidden;
- the no-retry helper may not add `AddRetry` or an `AddResilienceHandler` replay/rejection pipeline;
- known sensitive logging fragments are forbidden.

`scripts/validate-r3-external-resilience.py` mirrors these R3 controls for hosts without .NET. R1 and R2 ratchets remain unchanged, including the exact 57-entry Persistence fallback allowlist.

## Preserved invariants

R3 changes transport ownership and safe failure policy only. It does not change:

- ACH Colombia/CENIT rule separation;
- NACHA-M layouts, codes, causes, cycles, windows, limits, Return/ROR behavior or PUBLISH snapshot authority;
- golden files or expected results;
- financial/domain mapping behavior;
- idempotency hashes, trace/audit state, cluster-safety, retry/recovery ownership or MFT semantics;
- EF model/schema or migrations;
- SPA behavior.

The only intentional transport-level behavioral change is R3-authorized: unsafe blind WSCFAACH transport retry is removed, while safe bounded retry is introduced only for idempotent health GET probes. Generic POST/SOAP/WCF operations do not gain blind retry.

## Static evidence available in this environment

- `python scripts/validate-solution-projects.py`: PASS; 7 solution projects, 7 disk projects, all project-reference targets loaded.
- `python scripts/validate-r2-di-configuration.py`: PASS; R2 ratchet remains intact, including Persistence debt 57/57.
- `python scripts/validate-r3-external-resilience.py`: PASS; direct `new HttpClient` = 0, raw default action `HttpClient` injection = 0, required named clients present, unsafe logging fragments = 0, health retry > 0, generic/SOAP retry = 0, generic timeout = 100s.
- R2→R3 static metrics: direct product `new HttpClient` 3→0; raw API default `HttpClient` action injection 1→0; target banned sensitive-log files 6→0 using the final R3 banned-fragment set.
- `python scripts/scan_result_solid.py`: still exits 1 for the known broad program backlog; its output is byte-identical to the captured pre-R3 R2 output.
- Protected-artifact comparison: all `tests/**/TestData/**` files (15), `docs/normativa` files (12), Persistence migration files (92), SQL Server migration project files (79), SPA/docs-spa files (776), and the NACHA golden fingerprint characterization test are byte-identical to R2.

## Executable evidence unavailable

The input archive contains no `.git`. `dotnet` and Docker are unavailable on this host. Therefore Release build, R3 focal HTTP/SOAP/WCF tests, architecture ratchet execution, NACHA golden/fingerprint tests and the broad backend suite are `NOT_RUN_ENVIRONMENT`.

R3 remains `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`. `NEXT_ALLOWED_PHASE=R4A` is calculated for a subsequent LOCAL_ADVANCEMENT increment only; R4A was not executed here.
