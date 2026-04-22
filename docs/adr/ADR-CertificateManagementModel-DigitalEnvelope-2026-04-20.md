# ADR — Certificate Management Model for NACHA Digital Envelope

- **ID:** ADR-CertificateManagementModel-DigitalEnvelope-2026-04-20
- **Fecha:** 2026-04-21 (UTC)
- **Estado:** Propuesto para revisión
- **Ámbito:** Modelo de gestión de certificados para sobre digital NACHA-M (`.ENV/.env`) en ACHInterbank
- **Fase:** Diseño (sin implementación)

## 1. Resumen ejecutivo

Este ADR define el modelo de dominio, datos y servicios para gestión segura de certificados en el flujo de sobre digital NACHA-M. Se establece un esquema versionado por contexto (cámara, ambiente, propósito, holder), con activación única por contexto, trazabilidad completa, rotación y revocación auditables, y manejo de secretos sin contraseñas en claro.

Se distingue explícitamente entre:
- **Normativa ACH confirmada** (Anexo 21 y sección de certificados), y
- **Decisiones internas de seguridad** (gobierno lifecycle, secretos, restricciones API, controles de activación).

## 2. Problema

El modelo actual no cubre los requisitos de gobierno de certificados para operación segura y auditable:
- storage de password en claro,
- upsert destructivo sin historial,
- ausencia de estados lifecycle completos,
- ausencia de activación única por contexto,
- trazabilidad de uso insuficiente.

Esto impide implementar hardening seguro de sobre digital en fase posterior.

## 3. Decisión propuesta

1. Adoptar un **modelo versionado** centrado en `DigitalCertificate` + `DigitalCertificateVersion`.
2. Introducir catálogos/enums de propósito, ambiente, estado, holder y modo de almacenamiento.
3. Definir reglas de activación estrictas con unicidad por contexto y bloqueo de estados inválidos.
4. Separar metadata X.509 de material sensible (private key/password) mediante estrategia de secretos con `SecretRef`.
5. Incorporar auditoría técnica de carga/activación/uso/rotación y bitácora de operaciones de sobre digital.

### Clasificación de esta decisión
- **Normativa ACH confirmada:** uso de certificados digitales X.509 v3, RSA 2048, PKCS#12/.cer, y uso de certificado receptor/firmante en flujo seguro.
- **Decisión interna de seguridad:** versionado detallado, estados internos, reglas de activación, modelo de secretos, API masking y auditoría ampliada.

## 4. Entidades de dominio propuestas

> Nota: nombres de clase orientativos. Diseño conceptual; no implementación.

### 4.1 DigitalCertificate
**Propósito:** Identidad lógica del certificado (agrupador de versiones).  
**Campos sugeridos:**
- `Id`
- `Code` (identificador funcional)
- `DisplayName`
- `Description`
- `IsDeletedLogical`
- `CreatedAtUtc`, `CreatedBy`
- `UpdatedAtUtc`, `UpdatedBy`

**Relaciones:** 1:N con `DigitalCertificateVersion`.

**Invariantes:**
- `Code` único lógico.
- No hard-delete si tiene versiones usadas.

### 4.2 DigitalCertificateVersion
**Propósito:** Versión concreta utilizable en operación.  
**Campos mínimos (solicitados + recomendados):**
- `Id`, `DigitalCertificateId`
- `ClearingHouseId`
- `Environment` (enum)
- `Purpose` (enum)
- `HolderType` (enum)
- `Status` (enum)
- `VersionNumber`
- `Subject`, `Issuer`, `SerialNumber`, `Thumbprint`, `FingerprintSha256`
- `NotBefore`, `NotAfter`, `HasPrivateKey`
- `KeyAlgorithm`, `KeySize`, `SignatureAlgorithm`
- `RawPublicCertificate` (si aplica)
- `PrivateMaterialStorageMode` (enum)
- `SecretRef`, `FileRef`
- `UploadedAtUtc`, `UploadedBy`
- `ActivatedAtUtc`, `RevokedAtUtc`
- `ReplacedByVersionId`
- `ValidationSummaryJson` (opcional, no sensible)
- `RowVersion`

**Invariantes:**
- una única versión activa por contexto (`ClearingHouseId`,`Environment`,`Purpose`,`HolderType`).
- no puede estar `Active` si está fuera de vigencia.
- `HasPrivateKey=true` obligatorio para propósitos que requieren firma/descifrado.
- `SecretRef` obligatorio si `PrivateMaterialStorageMode` requiere secreto externo.

### 4.3 CertificateUsageLog
**Propósito:** Trazabilidad de uso operativo de certificado.
**Campos:** `Id`, `CertificateVersionId`, `OperationType`, `OperationId`, `ContextJson`, `Result`, `ErrorCode`, `CreatedAtUtc`, `CreatedByProcess`.

### 4.4 CertificateRotationHistory
**Propósito:** Historial de sustitución/rotación.
**Campos:** `Id`, `PreviousVersionId`, `NewVersionId`, `Reason`, `RotatedAtUtc`, `RotatedBy`, `TicketRef`.

### 4.5 CertificateLoadAudit
**Propósito:** Auditoría de carga/validación inicial.
**Campos:** `Id`, `CertificateVersionId`, `LoadSource`, `ValidationResult`, `ValidationErrorsJson`, `LoadedAtUtc`, `LoadedBy`.

### 4.6 DigitalEnvelopeOperationLog
**Propósito:** Bitácora criptográfica de operación de sobre digital (integración futura).
**Campos:** `Id`, `Direction`, `ClearingHouseId`, `Environment`, `Purpose`, `CertificateVersionId`, `FileNameIn`, `FileNameOut`, `HashPlainSha256`, `HashEncryptedSha256`, `SizeBefore`, `SizeAfter`, `Result`, `ErrorCode`, `OccurredAtUtc`, `Actor`.

## 5. Enums/catálogos propuestos

## 5.1 CertificatePurpose
- `OutboundEncryption`
- `InboundDecryption`
- `OutboundSigning`
- `InboundSignatureValidation`

## 5.2 CertificateEnvironment
- `Test`
- `Production`

## 5.3 CertificateStatus
- `Draft`
- `Active`
- `Inactive`
- `Expired`
- `Revoked`
- `Replaced`
- `PendingSecretBinding`
- `Invalid`

## 5.4 CertificateHolderType
- `Participant`
- `ClearingHouse`
- `ThirdPartyProvider`

## 5.5 CertificateStorageMode
- `DatabaseEncrypted`
- `ExternalSecretReference`
- `FileReference`
- `HsmReference`
- `KeyVaultReference`

## 5.6 CertificateMaterialType
- `PublicCertificate`
- `PrivateKeyPair`
- `CertificateChain`

### Permitidos fase 1
- **Permitidos:** `ExternalSecretReference`, `KeyVaultReference`, `DatabaseEncrypted` (solo con cifrado fuerte y controles estrictos).
- **No recomendados fase 1:** `FileReference` (solo contingencia local), `HsmReference` si no está disponible.

## 6. Modelo de base de datos EF Core (Code First)

> Diseño propuesto para implementación posterior.

### 6.1 Tablas
1. `DigitalCertificates`
2. `DigitalCertificateVersions`
3. `CertificateUsageLogs`
4. `CertificateRotationHistories`
5. `CertificateLoadAudits`
6. `DigitalEnvelopeOperationLogs`

### 6.2 Columnas y constraints clave

#### DigitalCertificates
- PK: `Id`
- Unique: `Code` (filtrado por borrado lógico)
- Concurrency: `RowVersion`

#### DigitalCertificateVersions
- PK: `Id`
- FK: `DigitalCertificateId` -> `DigitalCertificates(Id)` (Restrict)
- FK: `ReplacedByVersionId` -> `DigitalCertificateVersions(Id)` (Restrict, nullable)
- Índices:
  - `IX_DCV_Context` (`ClearingHouseId`,`Environment`,`Purpose`,`HolderType`)
  - `IX_DCV_Thumbprint`
  - `IX_DCV_SerialNumber`
  - `IX_DCV_NotAfter`
- Unique parcial (estado activo):
  - único por (`ClearingHouseId`,`Environment`,`Purpose`,`HolderType`) donde `Status='Active'`
- Nullable recomendados:
  - `SecretRef` nullable para públicos
  - `RawPublicCertificate` nullable según material
  - `RevokedAtUtc`, `ActivatedAtUtc`, `ReplacedByVersionId` nullable
- Non-nullables críticos:
  - contexto, `Status`, `VersionNumber`, `Thumbprint`, `NotBefore`, `NotAfter`, `HasPrivateKey`, `RowVersion`

#### CertificateUsageLogs
- FK: `CertificateVersionId` (Restrict)
- Índices por `OccurredAtUtc`, `OperationType`, `Result`

#### CertificateRotationHistories
- FKs a versión previa/nueva (Restrict)
- Unique (`PreviousVersionId`,`NewVersionId`)

#### CertificateLoadAudits
- FK: `CertificateVersionId` (Restrict)
- Índice por `LoadedAtUtc`

#### DigitalEnvelopeOperationLogs
- FK opcional: `CertificateVersionId` (Restrict)
- Índices por (`OccurredAtUtc`,`ClearingHouseId`,`Environment`,`Purpose`,`Result`)

### 6.3 Delete behavior
- **Restrict** para entidades históricas y logs.
- No borrado físico de versiones utilizadas.

### 6.4 Concurrency
- `RowVersion` en `DigitalCertificates` y `DigitalCertificateVersions`.

## 7. Reglas de activación/rotación

1. Solo una versión `Active` por `(ClearingHouseId, Environment, Purpose, HolderType)`.
2. No activar certificado expirado (`NotAfter < now`).
3. No activar certificado aún no vigente (`NotBefore > now`).
4. Si propósito requiere privada (`OutboundSigning`, `InboundDecryption`), `HasPrivateKey=true` obligatorio.
5. No usar certificado público para propósito de llave privada.
6. Activar uno nuevo implica cerrar anterior (`Replaced`/`Inactive`) + registrar `CertificateRotationHistory`.
7. Toda activación/revocación/inactivación registra auditoría.
8. No hard-delete de versiones usadas en operaciones.

### Rotación
- Rotación programada (ej. N días antes de `NotAfter`) y rotación reactiva (revocación/incidente).
- Estado transitorio permitido: `PendingSecretBinding` hasta ligar secreto válido.

## 8. Manejo de secretos propuesto

## 8.1 Opción preferida fase 1
- `PrivateMaterialStorageMode = KeyVaultReference` o `ExternalSecretReference`.
- Persistir solo `SecretRef` + metadata no sensible.

## 8.2 Fallback local (solo si no hay vault)
- `DatabaseEncrypted` con cifrado fuerte y llaves fuera de BD (DPAPI/ProtectedData o equivalente de plataforma).
- Control estricto de acceso y rotación de llaves de cifrado de aplicación.

## 8.3 Prohibido
- Password PFX en texto plano.
- Retornar secretos/material privado por API.
- Loggear secretos.
- Subir certificados reales a repositorio.

## 8.4 CER/CRT público
- Puede persistirse como `RawPublicCertificate` con controles de integridad (thumbprint/fingerprint).

## 8.5 Rotación de secretos
- `SecretRef` versionado y auditable.
- Revalidación de acceso al secreto en activación y uso.

## 9. Interfaces y servicios Application propuestos

## 9.1 ICertificateCatalogService
**Responsabilidad:** consulta de catálogos/listados/versiones.  
**Métodos sugeridos:**
- `GetCertificatesAsync(filter)`
- `GetCertificateVersionsAsync(certificateId)`
- `GetVersionDetailAsync(versionId)`

## 9.2 ICertificateLoadService
**Responsabilidad:** carga/parse/normalización de CER/CRT/PFX test/prod.  
**Métodos:**
- `LoadPublicCertificateAsync(request)`
- `RegisterPrivateCertificateAsync(request)`

**Errores esperados:** formato inválido, password inválida, metadata inconsistente.

## 9.3 ICertificateSelectionService
**Responsabilidad:** seleccionar versión activa por contexto/propósito.  
**Métodos:**
- `SelectActiveAsync(context)`

## 9.4 ICertificateActivationService
**Responsabilidad:** activar/inactivar/revocar/reemplazar.  
**Métodos:**
- `ActivateVersionAsync(versionId, actor)`
- `DeactivateVersionAsync(versionId, actor, reason)`
- `RevokeVersionAsync(versionId, actor, reason)`

## 9.5 ICertificateRotationService
**Responsabilidad:** orquestar rotación programada/reactiva.  
**Métodos:**
- `RotateAsync(oldVersionId, newVersionId, reason, actor)`

## 9.6 ICertificateValidationService
**Responsabilidad:** validar vigencia, metadata, contexto y reglas de propósito.  
**Métodos:**
- `ValidateForActivationAsync(versionId)`
- `ValidateForOperationAsync(context, versionId)`

## 9.7 ICertificateSecretProtector
**Responsabilidad:** abstraer obtención/protección de material sensible.  
**Métodos:**
- `BindSecretAsync(versionId, secretRef)`
- `ResolveSecretAsync(versionId)`
- `RotateSecretRefAsync(versionId, newRef)`

## 9.8 ICertificateAuditService
**Responsabilidad:** auditoría de ciclo de vida/carga/activación.  
**Métodos:**
- `AuditLoadAsync(...)`
- `AuditActivationAsync(...)`
- `AuditRevocationAsync(...)`

## 9.9 ICertificateUsageLogger
**Responsabilidad:** registrar uso en operaciones criptográficas.  
**Métodos:**
- `LogUsageAsync(usageEvent)`

## 10. Integración futura con sobre digital

> Diseño de integración (sin implementación ahora).

1. `NachaExportController` / servicio de exportación solicita certificado activo via `ICertificateSelectionService` para `OutboundSigning` y `OutboundEncryption`.
2. Servicio de descifrado inbound solicita activo para `InboundDecryption` y validación para `InboundSignatureValidation`.
3. `RsaKeyProvider`/servicio criptográfico se desacopla a través de interfaz de selección y resolución de secreto.
4. Cada operación cifra/descifra/firma/verifica registra en `DigitalEnvelopeOperationLog` + `CertificateUsageLog`.
5. Fallas de selección/secret/vigencia/firma deben ser fail-close y auditables.

## 11. Plan de migración desde modelo actual

### 11.1 Fuente actual
- `DigitalEnvelopeCertificate` (tipos: `EncryptionPublic`, `SigningKeyPair`).
- Campo `Password` actual.
- `Upsert` destructivo por tipo.

### 11.2 Estrategia de migración
1. Migrar metadata existente a `DigitalCertificate` + `DigitalCertificateVersion`.
2. Preservar `RawData` como material transitorio según clasificación (público/privado).
3. Mapear tipos actuales:
   - `EncryptionPublic` -> `OutboundEncryption` (holder según contexto)
   - `SigningKeyPair` -> `OutboundSigning`/`InboundDecryption` según uso real (requiere análisis por cámara)
4. Registros con material privado sin secreto gestionado -> `PendingSecretBinding`.
5. No perder histórico: registrar evento de migración en `CertificateLoadAudit`.
6. Compatibilidad temporal: lectura dual controlada durante transición.
7. Retiro del modelo viejo tras cutover y validación operativa.

### 11.3 Passwords existentes
- No migrar password en claro a nuevo modelo.
- Forzar rebinding de secreto (vault/secretRef) y limpiar campo heredado en ventana controlada.

## 12. API administrativa futura (diseño)

### Endpoints sugeridos
- `POST /nacha-security/certificates/public`
- `POST /nacha-security/certificates/private`
- `GET /nacha-security/certificates`
- `GET /nacha-security/certificates/{id}/versions`
- `POST /nacha-security/certificates/versions/{id}/activate`
- `POST /nacha-security/certificates/versions/{id}/revoke`
- `POST /nacha-security/certificates/versions/{id}/replace`
- `POST /nacha-security/certificates/versions/{id}/validate`
- `GET /nacha-security/certificates/audit`
- `GET /nacha-security/certificates/usage`

### Campos que NO deben devolverse nunca
- Password/secret material.
- Private key raw bytes.
- Secret values resolved.
- Cadenas sensibles de conexión o refs internas completas (solo alias seguro).

## 13. Estrategia de pruebas futura

### Unit
- Parse CER/CRT.
- Parse PFX de prueba.
- Cálculo thumbprint/fingerprint.
- Reglas vigencia (`NotBefore/NotAfter`).
- Validación de `HasPrivateKey` por propósito.
- Password incorrecta.
- Estados inválidos y transición inválida.
- Activación única por contexto.

### Integration
- Persistencia versionada con constraints.
- Activación reemplaza versión previa.
- Historial de rotación.
- Auditoría de carga/activación/uso.
- Selección por contexto.
- `SecretRef` obligatorio para privados en modo externo.

### Security
- Password no persiste en claro.
- API no expone material privado.
- Logs sin secretos.

## 14. Riesgos

### Críticos
- Definición/operación de secretos insuficiente puede bloquear fase 1.
- Migración sin compatibilidad temporal podría afectar operación de cifrado/descifrado.

### Altos
- Diseño incompleto de activación única por contexto.
- Falta de trazabilidad de uso de certificados.

### Medios
- Diferencias de mapeo de propósitos actuales a nuevos propósitos.

### Bajos
- Sobrecarga operativa inicial en administración lifecycle.

## 15. Preguntas abiertas

1. ¿Qué vault corporativo se adopta como estándar fase 1 (Azure/AWS/HashiCorp)?
2. ¿Se permite `DatabaseEncrypted` en producción o solo `SecretRef` externo?
3. ¿CRL/OCSP se valida en runtime en todas las operaciones o por ventanas?
4. ¿Cómo mapear exactamente `SigningKeyPair` actual a propósitos inbound/outbound por cámara?
5. ¿Qué estrategia de rollout por cámara/ambiente minimiza riesgo operativo?

## 16. Veredicto del diseño

- Diseño listo para revisión.
- Listo para implementación de Certificate Management fase 1, sujeto a decisión final de estrategia de secretos y validación de seguridad.
- Requiere validación de seguridad adicional antes de ejecución técnica.
