# Backend implementation — NACHA Security Operations API (2026-04-22)

## Alcance implementado (Prompt 12)

Se implementó backend para operación SPA con `operationId`:

- `POST /nacha-security/operations/nacha/generate`
- `POST /nacha-security/operations/nacha/generate-encrypted`
- `POST /nacha-security/operations/envelope/manual-encrypt`
- `POST /nacha-security/operations/envelope/manual-decrypt`
- `GET /nacha-security/operations/{operationId}`
- `POST /nacha-security/operations/{operationId}/authorize-download`
- `GET /nacha-security/operations/{operationId}/download`
- `GET /nacha-security/operations/audit`

## DTOs/Contratos

Definidos en `NachaSecurityOperationsContracts.cs`:

- `DigitalEnvelopeOperationDto`
- `DigitalEnvelopeOperationArtifactDto`
- `DigitalEnvelopeOperationErrorDto`
- `DigitalEnvelopeCertificateSummaryDto`
- `NachaGenerateRequest`
- `ManualEnvelopeRequest`
- `DownloadAuthorizationResult`
- `OperationDownloadDescriptor`

## Persistencia y migración EF

- Nueva entidad: `NachaSecurityOperation`.
- Nueva configuración EF: `NachaSecurityOperationConfiguration`.
- Nuevo `DbSet`: `AchDbContext.NachaSecurityOperations`.
- Migración EF generada: `20260422112419_AddNachaSecurityOperations`.
  - `Up()` crea tabla `NachaSecurityOperations` + índices (`OperationId` único y `RequestedAtUtc/OperationType/Status`).
  - `Down()` elimina tabla.

## Descarga segura

- Descarga desacoplada por `operationId`.
- Requiere autorización previa (`authorize-download`).
- Ventana temporal de autorización (`DownloadAuthorizedUntilUtc`).
- Validación de expiración (`DownloadExpiresAtUtc`).
- No se retorna contenido plano en JSON (solo metadata/hash/estado).

## Auditoría y seguridad

- Se guarda metadata de operación (`status`, hashes, `operationId`, timestamps, errores sanitizados).
- No se persiste contenido de archivo en BD.
- Artefactos temporales en filesystem (`OperationArtifactStore`) fuera de repo (`Path.GetTempPath()/achinterbank/operations` por defecto).
- No se tocó `CryptoServiceScoped`, `OpenEnvelopeAsync`, `RsaKeyProvider`, `identifier`, `IV`, XML, AES/RSA/padding.

## Revalidación ejecutada

Comandos ejecutados y resultado real:

1. `bash scripts/codex/setup-codex-env.sh` ✅
2. `dotnet --info` ✅
3. `dotnet ef --version` ✅
4. `dotnet restore ACHInterbank.sln` ✅
5. `dotnet build ACHInterbank.sln -c Release` ✅
6. `dotnet test ... --filter "FullyQualifiedName~NachaSecurityOperation|...|Download"` ✅ (5/5)
7. `dotnet test ... --filter "FullyQualifiedName~Signature|FullyQualifiedName~OpenEnvelope|FullyQualifiedName~DigitalEnvelope|FullyQualifiedName~CertificateResolver|FullyQualifiedName~SecretResolver"` ✅ (35/35)
8. `dotnet test ... --filter "FullyQualifiedName~Nacha|FullyQualifiedName~Mapping|FullyQualifiedName~BatchNumber"` ✅ (159/159)

## CI workflow

`.github/workflows/postgres-integration-tests.yml` se mantiene manual-only:

- `on: workflow_dispatch`
- job guard: `if: github.event_name == 'workflow_dispatch'`
- sin triggers automáticos (`push/pull_request/schedule/workflow_run`).

## Revalidación Prompt 14 (2026-04-22 UTC)

- Permisos finos vigentes para endpoints de operaciones NACHA security y certificados.
- Validación de descarga segura confirmada:
  - autorización temporal previa a descarga,
  - expiración/autorización verificadas,
  - verificación de permisos por tipo de artefacto (`CanDownloadPlainNacha`/`CanDownloadEnvelope`),
  - bloqueo explícito de descarga de plano cuando existe `SIGNATURE_VALIDATION_FAILED`.
- Evidencia de no regresión backend:
  - operaciones/permisos/descarga: 12/12,
  - DigitalEnvelope/Signature/OpenEnvelope/SecretResolver: 36/36,
  - NACHA/Mapping/BatchNumber: 166/166.
- Confirmado: no se cambió criptografía base ni hardening de `identifier/IV`.

## Addendum OpenBao (2026-04-23)
- `CertificateStorageMode.OpenBaoReference` habilita persistencia de material privado en OpenBao KVv2.
- Upload de `.pfx` privado ahora permite que backend genere `SecretRef` automáticamente (`openbao://...`) y persista solo metadata+ref.
- Resolución de secretos para firma/descifrado usa `OpenBaoCertificateSecretProvider`.
- Bootstrap UAT: la API ahora puede leer token OpenBao desde archivo (`ApiTokenFilePath`) para evitar pasos manuales posteriores al `compose up`.
