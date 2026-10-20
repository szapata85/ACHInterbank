# REFACTOR-CA-SOLID-RES-001 — R1 Application Boundary Cleanup

## State

- Phase: `R1`
- State: `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`
- Execution mode: `LOCAL_ADVANCEMENT`
- Current Git SHA: `UNAVAILABLE_SOURCE_ARCHIVE_NO_GIT_METADATA`
- Next allowed phase: `R2` (not executed in this increment)

R0 remains `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`. Local advancement permits structural implementation to continue when infrastructure is missing, but it does not convert R0 or R1 to `VERIFIED`/`CLOSED`.

## Scope implemented

### R1A — HTTP / middleware / filters

Presentation concerns previously under `Cfa.ACHInterbank.Application` were moved to `Cfa.ACHInterbank.Api`: HTTP exception mapping, request helpers, MVC filters and middleware. The menu use case no longer reads `HttpContext` directly. `Application` now exposes `ICurrentUserContext`; `Api` implements it with `HttpCurrentUserContext`.

### R1B — logging boundary

The concrete NLog `LoggerManager` moved to `Api`. `Application` retains only `ILoggerManager`. The JWK external service receives the logging port through DI and passes it to the existing response helper so the previous logging side effect is not intentionally removed.

### R1C — WCF connected service

The generated `wsAutorizadorOM1` connected service moved from `Application` to `External`. Its generated namespace remains unchanged for caller compatibility. WCF package references required by that generated transport moved with it to `Cfa.ACHInterbank.External.csproj`.

### R1D — certificate/X509 adapter

`RsaKeyProvider`, including its `X509Store` access, moved to `External.Security.Certificates`. The existing `IRsaKeyProvider` contract remains in `Application`, so the use case boundary remains transport/infrastructure agnostic.

### R1E — Application project boundary packages

After static zero-consumer inspection, `Cfa.ACHInterbank.Application.csproj` no longer references ASP.NET MVC/HTTP packages, NLog, or WCF packages. The R0 architecture ratchet now has zero allowlisted Application violations for ASP.NET, WCF, NLog and X509Store, and zero boundary-package exceptions.

The single legacy manual-`HttpClient` Application allowlist entry remains unchanged because it belongs to later resilience work, not R1.

## Preserved invariants

No ACH Colombia or CENIT rule was changed. No NACHA-M layout, cause, cycle, window, return semantic, PUBLISH snapshot authority, golden file, expected result, idempotency/hash/audit rule, cluster/recovery behavior, managed-MFT semantic, retry policy, EF model, schema or migration was changed. No `PaymentRailOperationalStrategy` design was introduced by R1.

## Evidence available in this environment

- `python scripts/validate-solution-projects.py`: PASS before R1 and PASS after R1; 7 solution projects, 7 disk projects and all project-reference targets loaded.
- Static R1 boundary inspection: PASS; Application has zero ASP.NET/WCF/NLog/X509Store source violations and zero boundary package references targeted by R1.
- Architecture ratchet inspection: R1 source/package allowlists reduced to zero; the pre-existing manual `HttpClient` exception remains exactly one file.
- Protected-artifact pristine-tree diff: NACHA golden files and their fingerprint characterization test, normative documents, SQL Server/PostgreSQL migrations and SPA are unchanged by R1.
- `python scripts/scan_result_solid.py`: reports the existing broad program Result-pattern/SRP backlog outside R1; this was not treated as an R1 cleanup mandate.

## Evidence unavailable

The delivered archive contains no `.git`, and this host has no `dotnet` or Docker. Therefore exact-commit controls, Release build, architecture tests, focal HTTP/SOAP/certificate tests and the broad backend suite are `NOT_RUN_ENVIRONMENT`. R1 must not be called `VERIFIED` or `CLOSED` until those gates execute on the exact tree/commit.
