# REFACTOR-CA-SOLID-RES-001 — R0 baseline and architecture ratchet

**Captured:** 2026-10-10
**Phase:** R0 — baseline, characterization and architecture guardrails
**State:** `IMPLEMENTED_UNVERIFIED`

## 1. Scope and evidence limits

R0 adds guardrails only. It does not move production classes, change ACH Colombia or CENIT behavior, alter PUBLISH authority, change NACHA-M layouts or golden outputs, or create database migrations.

The delivered source archive does **not** contain `.git` metadata. Therefore `git status --short` and `git rev-parse HEAD` both fail with `fatal: not a git repository`, and the exact current implementation SHA/branch cannot be established from this package. `MEMORY.md` records `22d10822c5222c3c72a76b7279d6a885dfcbc75b` as the accepted functional commit before repository cleanup, but that value is historical context and is **not** asserted as the SHA of this unpacked tree.

Executable backend validation is also unavailable on this host because `dotnet` is not installed. Docker is not installed. Node/npm are available, but R0 changes no SPA files, so frontend execution is not part of this phase gate.

Pre-change command evidence:

```text
git status --short
=> exit 128: fatal: not a git repository

git rev-parse HEAD
=> exit 128: fatal: not a git repository

python scripts/validate-solution-projects.py
=> exit 0
SOLUTION GRAPH: OK
  solution projects: 7
  disk projects:     7
  project references: all targets exist and are loaded by the solution

dotnet --info
=> exit 127: dotnet: command not found

docker --version
=> exit 127: docker: command not found

node --version
=> v22.16.0
npm --version
=> 10.9.2
```

## 2. Project dependency baseline

Current project references are intentionally frozen by `RefactorArchitectureRatchetTests` so a new dependency edge cannot appear unnoticed.

| Project | Current project references |
|---|---|
| `Cfa.ACHInterbank.Domain` | none |
| `Cfa.ACHInterbank.Application` | `Domain` |
| `Cfa.ACHInterbank.External` | `Application`, `Domain` |
| `Cfa.ACHInterbank.Persistence` | `Application`, `Domain` |
| `Cfa.ACHInterbank.Persistence.Migrations.SqlServer` | `Persistence` |
| `Cfa.ACHInterbank.Api` | `Application`, `Domain`, `External`, `Persistence`, `Persistence.Migrations.SqlServer` |
| `Cfa.ACHInterbank.Tests` | `Api`, `Application`, `Domain`, `Persistence`, `Persistence.Migrations.SqlServer` |

The Application project currently has nine package references that belong to the known presentation/transport/concrete-logging legacy boundary and are therefore ratcheted as explicit exceptions:

- `Microsoft.AspNetCore.Http.Abstractions`
- `Microsoft.AspNetCore.Mvc.Abstractions`
- `Microsoft.AspNetCore.Mvc.Core`
- `NLog`
- `System.ServiceModel.Duplex`
- `System.ServiceModel.Federation`
- `System.ServiceModel.Http`
- `System.ServiceModel.NetTcp`
- `System.ServiceModel.Security`

The rule is exact and shrinking: a new matching package fails the test, and when one of these packages is removed in a later authorized phase its allowlist entry must be removed in the same change.

## 3. Application boundary allowlist

`RefactorArchitectureRatchetTests` records the current source-level exceptions and fails on either a new violation or an unacknowledged removal.

### ASP.NET / HTTP concerns in Application — 11 files

- `src/Cfa.ACHInterbank.Application/Exceptions/ExceptionManager.cs`
- `src/Cfa.ACHInterbank.Application/Helpers/AddressIp/AddressIp.cs`
- `src/Cfa.ACHInterbank.Application/Helpers/ConvertDataSend/ConvertDataSend.cs`
- `src/Cfa.ACHInterbank.Application/Helpers/Filters/PostFilter.cs`
- `src/Cfa.ACHInterbank.Application/Helpers/Filters/RestrictToLanFilter.cs`
- `src/Cfa.ACHInterbank.Application/Helpers/Middleware/GlobalExceptionMiddleware.cs`
- `src/Cfa.ACHInterbank.Application/Helpers/Middleware/RequestResponseLoggingMiddleware.cs`
- `src/Cfa.ACHInterbank.Application/Helpers/Middleware/SecurityHeadersMiddleware.cs`
- `src/Cfa.ACHInterbank.Application/Helpers/Middleware/TokenJwtMiddleware.cs`
- `src/Cfa.ACHInterbank.Application/Helpers/Middleware/WafMiddleware.cs`
- `src/Cfa.ACHInterbank.Application/Navigation/Queries/GetMenuForCurrentUserHandler.cs`

### WCF / generated transport in Application — 1 file

- `src/Cfa.ACHInterbank.Application/Connected Services/wsAutorizadorOM1/Reference.cs`

### Concrete NLog in Application — 1 file

- `src/Cfa.ACHInterbank.Application/Helpers/Logs/Implementations/LoggerManager.cs`

### X509Store access in Application — 1 file

- `src/Cfa.ACHInterbank.Application/Services/EncryptionService/Implementations/RsaKeyProvider.cs`

### Manual HttpClient construction in Application — 1 file

- `src/Cfa.ACHInterbank.Application/Helpers/DNS/Implemetations/CheckHealthAsync.cs`

The Domain project is guarded as a zero-exception boundary for ASP.NET, WCF, NLog, X509Store and manual `HttpClient` construction.

## 4. Cross-cutting static inventory for later phases

These are lexical baseline measurements, not closure claims. In particular, `?? new` includes harmless value/default construction as well as runtime dependency fallbacks and must be classified before R2 changes.

| Signal | Baseline |
|---|---:|
| `new HttpClient(...)` | 3 occurrences in 2 source files |
| `AppSettings.Settings` | 15 occurrences in 14 source files |
| `?? new ...(` lexical candidates | 97 occurrences in 59 source files |
| `DateTime.UtcNow` / `DateTimeOffset.UtcNow` | 355 occurrences in 133 source files |

Manual `HttpClient` construction is currently present in:

- `src/Cfa.ACHInterbank.Application/Helpers/DNS/Implemetations/CheckHealthAsync.cs`
- `src/Cfa.ACHInterbank.External/Connections/HttpClientService.cs`

`AppSettings.Settings` is present in Application, External and API. Application consumers include DNS/load-balancer, authentication, token, client-assertion and encryption paths. External consumers include token/client-assertion/auth/HTTP connection services. This is R2 inventory only; R0 does not change configuration semantics.

High-count operational/static-clock hotspots include:

- `AchColombiaManagedFileExchangeService.cs`: 13 UTC-now references.
- `SchedulerAdminService.cs`: 9.
- `SchedulerSyncService.cs`: 8.
- `IncomingNachaPostParseProcessor.cs`: 7.
- `ReportsController.cs`: 7.

R9 must classify behavior-sensitive operational time instead of mechanically replacing all 355 occurrences.

## 5. Critical class-size baseline

Line counts are indicators only and are not acceptance rules.

| Class/file | Lines |
|---|---:|
| `NachaFileBuilder.cs` | 4,798 |
| `NachaParserService.cs` | 2,247 |
| `IncomingNachaPostProcessingOrchestrator.cs` | 1,179 |
| `AchColombiaManagedFileExchangeService.cs` | 996 |
| `IncomingNachaCommandCenterService.cs` | 1,502 |
| `AchReturnsService.cs` | 1,066 |
| `ReportsController.cs` | 1,028 |

These measurements establish the R0 baseline only. Later phases must improve cohesion and dependency direction without using line count as a substitute for design evidence.

## 6. Characterization coverage already present

R0 inspected existing tests before adding new guards. Current coverage already includes the critical routes identified by the plan:

- **ACH Colombia and CENIT outbound / PUBLISH authority:** `OfficialNachaGenerationTableDrivenTests` covers published ACH/CENIT profiles, fail-closed behavior, CENIT PPD/CCD/CTX rendering, byte-sensitive physical rules, controls, traces and ACH Colombia parser round-trips.
- **Ordinary inbound:** `NachaIncomingEndToEndProcessingTests` covers ACH Colombia and CENIT golden inputs, duplicate detection, invalid length/control totals, manual review and dispatch classification.
- **Returns:** `AchOutboundReturnConcurrencyIdempotencyTests` protects duplicate-row/race behavior and same-transaction/different-cause invariants.
- **Return-of-Return:** `CenitReturnOfReturn2026Tests` covers the normative synthetic fixture, build/parse/correlation/idempotency and CENIT timing rules; `AchReturnOfReturnFileGenerationServiceTests` covers current ACH/CENIT file-generation characterization and CENIT cycle-1 isolation from ACH Colombia.
- **Managed MFT:** `AchColombiaManagedFileExchangeTests` covers outbound duplicate logical execution, inbound duplicate/content conflict, claim/recovery/archive failure, retry/uncertain behavior, immutable content and boundary evidence semantics.
- **Quartz / cluster behavior:** `QuartzConcurrencyPolicyTests` and `QuartzPersistentStoreConfigurationTests` cover non-concurrent job policy and persistent PostgreSQL/SQL Server cluster configuration.

R0 does not rewrite those tests or their expected results.

## 7. New byte-stability guard

`NachaGoldenFingerprintCharacterizationTests` adds SHA-256 fingerprints for the six existing NACHA golden files plus the CENIT normative synthetic ROR fixture. It also asserts that the golden-file inventory remains exactly the current six files. This makes an unreviewed byte change or silent golden replacement fail deterministically.

| Fixture | SHA-256 |
|---|---|
| `GoldenFiles/ACHColombia/Incoming/ACH_COL_IN_001.ach` | `60240f257d818ac1a1b648c48de9f9cfb2a2e61b2110374f5cb4347b49d66032` |
| `GoldenFiles/ACHColombia/Outgoing/ACH_COL_OUT_001.ach` | `779cabff79863e66cac79b77e91525687e3b6e72cff104b96caaeff9fa63315f` |
| `GoldenFiles/ACHColombia/Returns/ACH_COL_RET_001.RET` | `fde736a96c1c24be0392e1e56bdd71e4910b63b287d5e8c47479a93afd7b96ee` |
| `GoldenFiles/CENIT/Incoming/CENIT_IN_001.ach` | `16d87fc282fd4bb144a08962bf10d52f8c97745a4aaf31131c786f801ceceb93` |
| `GoldenFiles/CENIT/Outgoing/CENIT_OUT_001.ach` | `626670feaf2f9323f92518f6d905d079864283efd6b0ab80df7084f303330836` |
| `GoldenFiles/CENIT/Returns/CENIT_RET_001.RET` | `047fab8f6a35a4e063974c0dabe9f736cd90fd0840e75ab672c831bfdc40ca95` |
| `NormativeSynthetic/Cenit/Ror/CENIT_ROR_IN_001.ach` | `33991f152af67ef07669b6bfc476b4d8d27263c43b8507b522a69c8823d8fdb4` |

The fingerprints document the current baseline; they are not permission to update a golden when production code changes. Any future fingerprint/golden update still requires an explicitly authorized normative/functional change and separate evidence.

## 8. R0 ratchet design

The R0 guardrails use a shrinking legacy allowlist:

1. current known Application violations are explicit;
2. a new violating file/package/project edge fails;
3. removing a legacy violation also requires removing the corresponding allowlist entry, making progress visible in the diff;
4. Domain has no legacy exception for the guarded concerns;
5. the old skipped SOAP architecture diagnostic was removed because it asserted no enforceable rule; the new ratchet is blocking when executable.

No production source code was changed in R0.

## 9. Verification status and next gate

R0 implementation is complete in this unpacked source tree, but the phase is **not VERIFIED and not CLOSED** because:

- exact current Git SHA/branch cannot be established from an archive without `.git`;
- .NET 10 is unavailable, so the new architecture/golden tests, Release build and backend suite cannot be executed;
- Docker is unavailable, so any runtime/provider/cluster gate cannot be rerun here.

Required verification on a Git-backed host with the required toolchain:

```cmd
git status --short
git rev-parse HEAD
python scripts/validate-solution-projects.py
dotnet build ACHInterbank.sln -c Release
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --no-build --filter "FullyQualifiedName~RefactorArchitectureRatchetTests|FullyQualifiedName~NachaGoldenFingerprintCharacterizationTests"
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --no-build
```

R1 is not allowed until R0 reaches `VERIFIED` and then `CLOSED` on the exact Git commit that produced the evidence.
