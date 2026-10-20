# REFACTOR-CA-SOLID-RES-001 — R8B Certificate management service cohesion

## State

- Phase: `R8B`
- Mode: `LOCAL_ADVANCEMENT`
- State reached: `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`
- Previous phases: `R0`–`R8A` remain `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`
- Next allowed phase after this increment: `R9` (not executed here)
- `BEHAVIORAL_DIFF=NONE`
- `NORMATIVE_DIFF=NONE`
- `DB_MIGRATIONS=NONE`

## Exact scope selected from the R8B plan

The R8B plan requires secondary large-service cleanup to be evaluated individually and explicitly names `CertificateManagementServices`, official profile seeders, grouped DTOs, and remaining services over 500 lines. It also forbids using file size alone as the reason to introduce abstractions.

The selected R8B item is **only the plan-named `CertificateManagementServices` compilation unit** in Persistence. The 1,327-line source file contained ten already-distinct public service owners plus two shared internal helper owners in one physical compilation unit:

- secret-material acceptance policy;
- activation validation;
- certificate load/register/managed-preview-save workflow;
- active-certificate selection;
- activation/revocation;
- safe deletion;
- rotation history;
- usage audit logging;
- load-audit query;
- certificate catalog query;
- shared DTO mapping / functional-status evaluation.

That is demonstrated source-level multiple responsibility independent of line count. R8B therefore makes the existing owners physically explicit without changing their responsibilities or creating new public abstractions.

The other named R8B candidates were evaluated and intentionally left out:

- `NachaConfigOfficialProfilesSeeder.cs` remains byte-identical. Although large, it is the cohesive official-profile/PUBLISH seeding authority and is highly normative; size alone is insufficient evidence for a split.
- `CertificateManagementDtos.cs` remains byte-identical. At 148 lines it is a cohesive certificate-management contract set and has no demonstrated ownership defect.
- other >500-line services are not changed merely for size. Several belong to protected R4–R8A boundaries, and no additional owner split was authorized by evidence in this increment.

## Ownership before → after

| Responsibility | R8A physical owner | R8B physical owner | Logical owner change |
|---|---|---|---|
| Shared mapping / functional status | `CertificateManagementServices.cs` | `CertificateManagementMapper.cs` | none |
| Secret acceptance policy | grouped file / `CertificateSecretProtectorService` | `CertificateSecretProtectorService.cs` | none |
| Activation validation | grouped file / `CertificateValidationService` | `CertificateValidationService.cs` | none |
| Load/register/managed certificate workflow | grouped file / `CertificateLoadService` | `CertificateLoadService.cs` | none |
| Active selection | grouped file / `CertificateSelectionService` | `CertificateSelectionService.cs` | none |
| Activate/revoke | grouped file / `CertificateActivationService` | `CertificateActivationService.cs` | none |
| Safe deletion | grouped file / `CertificateDeletionService` | `CertificateDeletionService.cs` | none |
| Rotation history | grouped file / `CertificateRotationService` | `CertificateRotationService.cs` | none |
| Usage logging | grouped file / `CertificateUsageLoggerService` | `CertificateUsageLoggerService.cs` | none |
| Load-audit query | grouped file / `CertificateAuditService` | `CertificateAuditService.cs` | none |
| Catalog query | grouped file / `CertificateCatalogService` | `CertificateCatalogService.cs` | none |

The legacy grouped file is retired. Public certificate service classes remain `10 -> 10`; maximum public service classes per compilation unit becomes `10 -> 1`. Every extracted class/helper body is byte-equivalent after line-ending normalization to its R8A body.

## Contracts, callers and DI

Public Application certificate contracts are unchanged and byte-identical:

- `ICertificateCatalogService`
- `ICertificateLoadService`
- `ICertificateSelectionService`
- `ICertificateActivationService`
- `ICertificateDeletionService`
- `ICertificateRotationService`
- `ICertificateValidationService`
- `ICertificateSecretProtector`
- `ICertificateAuditService`
- `ICertificateUsageLogger`

`CertificateManagementDtos.cs` is byte-identical, so serialized/request/response contract shape is unchanged.

Production callers remain on the same interfaces: `CertificateManagementController`, `CertificateBootstrapHostedService`, `DigitalEnvelopeCertificateResolver`, `NachaExportDigitalEnvelopeService`, and the existing certificate services that collaborate through `ICertificateSecretProtector` / `ICertificateValidationService`. No caller is redirected to a concrete implementation.

`DependencyInjectionService.cs` is byte-identical. The ten scoped interface→implementation mappings remain `10 -> 10` with the same lifetimes and implementation types. No service locator, new wrapper facade, or new interface is introduced.

## API / OpenAPI / authorization

R8B does not touch API/controller code. `CertificateManagementController.cs` is byte-identical, therefore:

- route templates and HTTP verbs are unchanged;
- request binding and DTOs are unchanged;
- status/error mapping is unchanged;
- OpenAPI summaries/descriptions/response metadata are unchanged;
- `[Authorize]` and certificate-management policies are unchanged;
- cancellation behavior remains delegated exactly as before.

`HTTP_CONTRACT_DIFF: NONE`.

R8A Reports remains outside R8B. Its controllers, base, `ControllerContractNameAttribute`, R8A characterization and OpenAPI transformers are unchanged; the R8A ratchet remains authoritative for `20 -> 20` report actions, `CanReadAch`, routes and logical controller-name compatibility.

## Side effects, persistence and transactions

R8B moves no behavior between logical owners. Across the split certificate service sources, the R8A side-effect sites remain:

- `SaveChangesAsync`: `12 -> 12`;
- entity `.Add(...)`: `23 -> 23`;
- entity `.Remove(...)`: `2 -> 2`;
- `AsNoTracking`: `20 -> 20`;
- explicit transaction begin/commit/rollback: `0 -> 0`;
- external HTTP/SOAP/SFTP/MFT transport ownership: `0 -> 0`;
- local/blind retry ownership: `0 -> 0`;
- `CryptographicOperations.ZeroMemory`: `1 -> 1`;
- `DateTime.UtcNow` sites: `15 -> 15` (R9 deterministic-time work is not executed here).

No EF model, schema, migration, transaction boundary, state transition, recovery policy, or idempotency rule changes.

## Security

Certificate material handling is security-sensitive, so R8B uses a body-identity ratchet rather than rewriting logic. Password/private-material handling, encrypted database storage checks, secret-reference rules, validation, revocation/deletion guards, zeroing of encrypted material, audit logging, and controller authorization remain exactly with their prior owners. No secret is added to logs or configuration and no private material is moved into source.

## Ratchet and characterization

Added `scripts/validate-r8b-certificate-management-service-cohesion.py`. It:

- re-runs the complete R1→R8A ratchet chain through the existing R8A validator and additionally protects R8A with exact hashes plus Reports public-surface metrics;
- requires retirement of `CertificateManagementServices.cs`;
- requires the eleven R8B compilation units and exact normalized R8A body hashes;
- protects Application certificate interfaces/DTOs, DI, `CertificateManagementController`, the official profile seeder, R8A architecture record and R8A validator by exact hash;
- keeps the ten scoped service mappings and class/interface ownership exact;
- preserves persistence/read/security side-effect counts;
- rejects transaction, transport, retry, service-locator or `PaymentRailOperationalStrategy` introduction in the R8B scope;
- preserves the Persistence fallback allowlist (`57 -> 57`) and known out-of-scope `Contains("CENIT")` debt (`8 -> 8`).

The broad `scan_result_solid.py` is historical backlog evidence, not a pass gate. Its R8A baseline is 237 Result-pattern findings and 13 SRP flags with exit 1; the final R8B run must be classified against that baseline and never reported PASS merely because it is unchanged.

## Explicitly outside R8B

- R9 deterministic time / async consistency;
- all official NACHA profile/PUBLISH seeding semantics;
- DTO contract redesign;
- Reports modularization or HTTP/OpenAPI changes;
- R4 outbound, R5 inbound, R6 MFT/Command Center, R7 Returns/ROR;
- `NachaInboundSimulationService` / `IncomingNachaIngestionAppService` `Contains("CENIT")` debt;
- Persistence fallback allowlist cleanup;
- ACH Colombia/CENIT rules, layouts, causes, cycles, windows, Return/ROR policy;
- SPA;
- migrations/schema.

## Environment limitation

The source archive has no `.git`, and this host has no `dotnet` or Docker. Static Python/source ratchets can be executed, but build, .NET focal/regression/API/OpenAPI/authorization suites, Docker/E2E and provider runtime validation are `NOT_RUN_ENVIRONMENT`. R8B therefore remains `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`, not `VERIFIED` or `CLOSED`.
