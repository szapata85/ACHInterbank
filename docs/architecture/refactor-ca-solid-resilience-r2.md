# REFACTOR-CA-SOLID-RES-001 — R2 DI, configuration and fail-fast

## State

- Phase: `R2`
- State: `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`
- Execution mode: `LOCAL_ADVANCEMENT`
- Current Git SHA: `UNAVAILABLE_SOURCE_ARCHIVE_NO_GIT_METADATA`
- Prior phases: R0/R1 remain `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`
- Next allowed phase: `R3` (not executed in this increment)

R2 is structurally implemented on the delivered R1 tree. It is not `VERIFIED` or `CLOSED`: the archive has no `.git` metadata and this host has no `dotnet` or Docker.

## R2 scope implemented

### Cohesive configuration instead of the `AppSettings.Settings` global

All production references to `AppSettings.Settings` were removed. Runtime consumers now receive cohesive options through DI:

- `Token` for authentication/JWT/token clients;
- `TokenGeneric` plus `Integrations` for client assertion flows;
- `ApplicationEncryptionOptions` for the legacy application encryption key;
- `CertificatesSettings` for legacy certificate-store fallback metadata;
- `ServesSettings` for health/load-balancer pools;
- `AuthenticationUiOptions` for the password-reset base URL;
- `WcfAuthorizationOptions` for the legacy WCF timeout.

`External.DependencyInjectionService` binds these options from the existing configuration keys. The existing enabled-server filtering previously performed by `GetMethodExtensions` is preserved with `PostConfigure<ServesSettings>`, and the now-unused global initialization/extension path was removed. No replacement mega `IAppSettings` abstraction was introduced.

JWT configuration is fail-fast at composition: issuer/audience/secret are required and token options use `ValidateOnStart` for required signing fields and positive expiration. The health/load-balancer server lists are also validated at startup before the existing enabled-server `PostConfigure` filtering runs. Other optional/legacy settings keep their existing feature-specific activation semantics rather than making unrelated features mandatory at process startup.

### Hidden runtime collaborator construction removed from critical boundaries

R2 removed the targeted hidden construction in Api/Application/External and made essential collaborators explicit:

- `NachaExportController` requires `ILogger<NachaExportController>` and `IOperationalTimeSnapshotProvider`; it no longer creates a Persistence provider as a fallback.
- `PostFilter` requires `ILoggerManager`; it no longer creates `LoggerManager` internally.
- `AchClearingHouseStrategy` and `CenitClearingHouseStrategy` require `ICycleNumberResolver`; `ClearingHouseStrategyFactory` receives that resolver through DI. The existing ACH Colombia/CENIT strategy selection semantics are unchanged.
- `AchColombiaSftpMftAdapter` and `AchColombiaManagedMftConnectionTester` receive `IAchColombiaSftpSessionFactory`; the SFTP implementation factory is composed in External DI rather than hidden in constructors.
- `AchColombiaManagedFileExchangeService` now requires encryption, MFT options, connection testing and operational-cycle-window collaborators instead of nullable runtime fallbacks/direct construction.
- Auth/token/client-assertion/encryption/certificate/health/load-balancer/WCF consumers receive configuration through DI options rather than a static global.

No `PaymentRailOperationalStrategy` design was introduced or expanded by R2. Pre-existing PaymentRail code was left untouched.

## Architecture ratchet

`RefactorArchitectureRatchetTests` now blocks:

- any production `AppSettings.Settings` usage;
- runtime-service `?? new ...` fallbacks in Api/Application/External;
- direct `new LoggerManager(...)`;
- hidden `new SshNetAchColombiaSftpSessionFactory(...)`;
- direct API construction of `OperationalTimeSnapshotProvider`.

A new exact shrinking allowlist records 57 pre-existing runtime-service fallback occurrences in Persistence. These are existing debt owned by later focused phases (including NACHA/Returns/facade thinning); R2 did not relax them or add new fallback debt. New Persistence entries are prohibited and removals must shrink the allowlist.

`scripts/validate-r2-di-configuration.py` mirrors the host-independent R2 zero-violation checks and enforces that the Persistence fallback count cannot grow above the R2 baseline. The older Application manual-`HttpClient` ratchet exception remains exactly as-is because managed HTTP/resilience work belongs to R3.

## Preserved invariants

R2 intentionally changes composition/configuration ownership, not financial behavior. It does not change:

- ACH Colombia or CENIT normative rules or their separation;
- NACHA-M record layouts, codes, causes, cycles, windows, limits or Return/ROR semantics;
- PUBLISH snapshot authority or golden/expected output;
- idempotency, content hashes, audit/traceability or cluster/recovery semantics;
- managed-MFT publication/claim/archive semantics;
- retry policy or HTTP/SOAP/WCF resilience behavior;
- EF model/schema or migrations.

The legacy manual `HttpClient` paths are deliberately not changed in R2. No blind retry was added.

## Static evidence available in this environment

- `python scripts/validate-solution-projects.py`: PASS after R2; 7 solution projects, 7 disk projects, all project references load.
- `python scripts/validate-r2-di-configuration.py`: PASS; production `AppSettings.Settings` = 0, critical Api/Application/External runtime-service fallbacks = 0, direct API operational-time provider construction = 0, Persistence legacy fallback debt = 57/57.
- R1→R2 source metrics: `AppSettings.Settings` 15→0; critical Api/Application/External service fallbacks 3→0; direct `LoggerManager` construction 1→0; hidden SFTP session-factory construction 2→0; direct API operational-time provider construction 1→0.
- `python scripts/scan_result_solid.py`: exits 1 for the known broad program backlog; its output is byte-identical to the captured R1 pre-R2 baseline, so R2 did not add findings to that scan.
- Pristine-tree protected-artifact comparison: NACHA golden/fingerprint assets, `docs/normativa`, SQL Server/PostgreSQL migration trees and SPA are unchanged by R2.

## Executable evidence unavailable

`git status`/`git rev-parse` cannot provide repository evidence because `.git` is absent. `dotnet` and Docker are unavailable, so Release build, R2 focal tests, executable architecture ratchet, NACHA golden/fingerprint tests and broad backend regression are `NOT_RUN_ENVIRONMENT`.

Therefore R2 remains `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`. `NEXT_ALLOWED_PHASE=R3` applies only to a later LOCAL_ADVANCEMENT increment; R3 was not executed here.
