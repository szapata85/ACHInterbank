<!-- LOCAL_CONTROLLED_SOAP_LIVE_AUTHORIZATION -->

## Excepción autorizada: SOAP LIVE local controlado

La declaración "Productivo NO-GO" aplica exclusivamente a:

- endpoints externos de producción;
- infraestructura productiva de CFA, ACH Colombia o CENIT;
- credenciales productivas;
- conexiones a redes financieras externas;
- movimientos monetarios reales;
- ejecución contra cuentas o sistemas core productivos.

No aplica a pruebas técnicas LIVE locales expresamente autorizadas.

Está permitido ejecutar SOAP real contra el servicio WCF local controlado:

- Windows: http://localhost:7083/WSCFAACH.svc
- Docker: http://host.docker.internal:7083/WSCFAACH.svc
- HostHeader: localhost:7083

Esta llamada corresponde a una prueba local de integración y no a producción.

Condiciones obligatorias:

1. API, SPA, SQL Server y WCF deben estar ejecutándose localmente.
2. No se permite ningún endpoint SOAP diferente de localhost o host.docker.internal.
3. Los archivos autorizados pueden provenir de producción, pero se usan exclusivamente como fixtures controlados de prueba.
4. No se deben imprimir datos personales, cuentas completas, credenciales, tokens o XML completo.
5. Se permite corregir código, configuración, ingestión y deduplicación durante la ejecución.
6. Se permiten hasta tres uploads mientras no exista cola con intento ni ejecución SOAP.
7. Se permite exactamente una llamada SOAP real por autorización.
8. Después del primer intento SOAP quedan prohibidos nuevos uploads, retries y nuevos dispatch.
9. La API debe restaurarse a DryRun al finalizar.
10. Toda la evidencia de ingestión, cola, request, response y log debe conservarse.

La autorización local no cambia el estado Productivo NO-GO del sistema.
<!-- /LOCAL_CONTROLLED_SOAP_LIVE_AUTHORIZATION -->

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

