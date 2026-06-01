# AGENTS.md - ACHInterbank

Before making changes, read:

- `docs/ai/ACH_PHASE6_CONTEXT.md`

This file is the short entry point for Codex, OpenCode, Claude Code and other coding agents. The permanent Phase 6 NACHA-M context lives in `docs/ai/ACH_PHASE6_CONTEXT.md`.

## Core Rules

- Do not execute real SOAP calls.
- Do not add real credentials.
- Do not use real customer data.
- Do not change production status.
- Productivo remains NO-GO.
- Do not modify NACHA-M golden files unless explicitly requested.
- Do not modify the table-driven NACHA-M engine unless fixing a proven bug.
- Do not generate migrations unless explicitly required.
- Do not reduce test coverage.
- Do not remove existing tests.
- Preserve Clean Architecture boundaries: Application contracts, Domain rules, Persistence implementations, Api composition.
- EF Code First remains the source of truth for schema changes.

## Build And Test Commands

Use these commands after code changes:

```bash
dotnet build ACHInterbank.sln -c Release
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release
```

For a second run after a successful build:

```bash
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --no-build
```

Expected result:

- Build succeeded.
- 0 warnings.
- 0 errors.
- Tests passing.

## Repository Map

- Main API: `src/Cfa.ACHInterbank.Api`
- Application contracts/models: `src/Cfa.ACHInterbank.Application`
- Domain models/rules: `src/Cfa.ACHInterbank.Domain`
- Persistence/infrastructure: `src/Cfa.ACHInterbank.Persistence`
- Backend tests: `tests/Cfa.ACHInterbank.Tests`
- NACHA golden files: `tests/Cfa.ACHInterbank.Tests/TestData/Nacha/GoldenFiles`
- Permanent AI context: `docs/ai/ACH_PHASE6_CONTEXT.md`

## Phase 6 Current Guardrails

- Fase 6B.3C/6B.3C.1 golden files are semireal, anonymized and not official certification artifacts.
- Fase 6B.4 prepares internal incoming NACHA decisions.
- Fase 6B.5 prepares controlled SOAP boundaries only.
- `Proc_Contrapartidas` and `Proc_Transacciones` are monetary candidates and must remain blocked from real execution until an explicit later phase.
- `RegistrarRespuestaTransaccion` is non-monetary and must not move money.
- `None`, duplicates and `ManualReviewRequired` must not execute SOAP.
