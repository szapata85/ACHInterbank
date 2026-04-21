# Certificate Management Phase 1 — Implementación (Digital Envelope)

## 1. Qué se implementó

- Nuevo modelo versionado de certificados y logs para gobierno de certificados.
- Nuevos enums de propósito, ambiente, estado, holder, storage mode y material type.
- Configuraciones EF Core Code First con índices, restricciones e intención de unicidad por contexto activo.
- Servicios de carga, validación, activación, revocación, selección, catálogo, auditoría y usage logging.
- API administrativa mínima para carga/listado/versiones/activación/revocación/validación/auditoría.
- Pruebas unitarias/integración iniciales con certificados auto-firmados de prueba.

## 2. Qué NO se implementó

- Integración con `CryptoServiceScoped`.
- Reemplazo de `RsaKeyProvider`.
- Hardening de `identifier/IV`.
- Cambios a flujo real de cifrado/descifrado de sobre digital.
- Eliminación del modelo legado `DigitalEnvelopeCertificate`.

## 3. Modelo de datos

Tablas nuevas propuestas e implementadas vía EF model:
- `DigitalCertificates`
- `DigitalCertificateVersions`
- `CertificateUsageLogs`
- `CertificateRotationHistories`
- `CertificateLoadAudits`
- `DigitalEnvelopeOperationLogs`

Incluye `RowVersion` en entidades principales y restricciones de delete `Restrict` en historial/logs.

## 4. Servicios

Implementados:
- `CertificateLoadService`
- `CertificateValidationService`
- `CertificateActivationService`
- `CertificateSelectionService`
- `CertificateRotationService`
- `CertificateSecretProtectorService`
- `CertificateAuditService`
- `CertificateUsageLoggerService`
- `CertificateCatalogService`

## 5. Endpoints

Controlador:
- `CertificateManagementController` (`/nacha-security/certificates/management`)

Endpoints:
- `POST /public`
- `POST /private`
- `GET /`
- `GET /{id}/versions`
- `POST /versions/{id}/activate`
- `POST /versions/{id}/revoke`
- `POST /versions/{id}/validate`
- `GET /audit`

## 6. Seguridad de secretos

- Password de PFX solo en memoria para validación de carga.
- No persistencia de password en entidades nuevas.
- `SecretRef` para modos externos.
- API devuelve `SecretRefMasked` y no material privado.

## 7. Pruebas

Se agregaron pruebas para:
- extracción metadata cer/pfx,
- rechazo de password inválida,
- validaciones de activación,
- reemplazo de versión activa,
- selección por contexto,
- revocación + auditoría,
- ausencia de campos sensibles en DTO API,
- ausencia de secretos en logs,
- validación básica de modelo EF.

## 8. Riesgos

- Índice único activo por contexto depende de soporte de índice filtrado del proveedor.
- Pendiente validación integral con migración real Postgres en todos los entornos.
- Falta integración con sobre digital todavía (planeado fase posterior).

## 9. Próximos pasos

1. Aplicar y validar migración en Postgres real.
2. Afinar enforcement de unicidad activa por contexto por proveedor.
3. Integrar selección de certificados a capa de sobre digital (fase siguiente).
4. Ejecutar pruebas end-to-end de operación con certificados de prueba.
