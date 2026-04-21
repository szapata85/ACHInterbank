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

## 13. Revalidación de evidencia pendiente (Prompt 5B) — 2026-04-21 UTC

### 13.1 Tooling ejecutado
Comandos:
```bash
export DOTNET_ROOT=$HOME/.dotnet
export PATH=$HOME/.dotnet:$HOME/.dotnet/tools:$PATH

dotnet --info
dotnet ef --version
```
Resultado:
- `dotnet --info`: SDK `10.0.201`, runtimes `10.0.5`.
- `dotnet ef --version`: `10.0.6`.

### 13.2 Migración EF
Comandos:
```bash
ls -la src/Cfa.ACHInterbank.Persistence/DataBase/Migrations/Postgres | grep Certificate

dotnet ef migrations list \
  --project src/Cfa.ACHInterbank.Persistence/Cfa.ACHInterbank.Persistence.csproj \
  --startup-project src/Cfa.ACHInterbank.Api/Cfa.ACHInterbank.Api.csproj \
  --context AchDbContext
```
Resultado real:
- `grep Certificate`: sin resultados (no existe archivo de migración con `Certificate` en el nombre en carpeta Postgres).
- `migrations list`: muestra `20260420215632_AddExternalFileNamePolicyPhase1` y reporta error de conexión a `localhost:5432`, por lo que no puede determinar estado aplicado.

Conclusión de estado EF en esta revalidación:
- No hay evidencia de migración EF física `AddCertificateManagementDigitalEnvelope` en la carpeta de migraciones Postgres.
- El snapshot contiene entidades de certificate management, pero falta artefacto de migración dedicado con ese nombre.
- No se puede confirmar consistencia final de migración sin corregir este gap.

### 13.3 Seguridad de secretos (hallazgos relevantes)
Comando:
```bash
rg -n "Password|PfxPassword|PrivateKey|RawPrivate|SecretRef|Secret|RawData|ToBase64String|Export" src/Cfa.ACHInterbank.* tests/Cfa.ACHInterbank.Tests -S
```

Clasificación en scope Certificate Management fase 1:
- Seguro:
  - Password de request privada se usa para cargar PKCS#12 en memoria y no se mapea a entidad persistida.
  - `SecretRef` existe en modelo de versión y en respuesta se expone enmascarado como `SecretRefMasked`.
  - Test de API valida que DTO no incluya `Password`, `RawPrivateKey`, `SecretRef`, `PrivateMaterial`.
  - Test de logs valida no contener patrones de secreto.
- Requiere ajuste:
  - Ninguno nuevo en el scope de phase 1.
- Inseguro:
  - Ninguno detectado en los archivos de Certificate Management phase 1.

Confirmaciones explícitas (scope Certificate Management phase 1):
1. No existe campo `Password` en entidades nuevas de certificate management: **confirmado**.
2. No se persiste password PFX: **confirmado**.
3. No se devuelve password por DTO/API: **confirmado**.
4. No se devuelve material privado: **confirmado**.
5. `SecretRef` en responses aparece enmascarado: **confirmado** (`SecretRefMasked`).
6. Logs no contienen password ni secret material: **confirmado** por prueba.
7. Los tests cubren estos puntos: **confirmado**.

### 13.4 Tests Certificate / DigitalEnvelope
Comando:
```bash
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj \
  -c Release \
  --no-build \
  --filter "FullyQualifiedName~Certificate|FullyQualifiedName~DigitalEnvelope" \
  -v minimal
```
Resultado:
- Total: **11**
- Passed: **11**
- Failed: **0**
- Skipped: **0**

### 13.5 No regresión NACHA
Comando:
```bash
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj \
  -c Release \
  --no-build \
  --filter "FullyQualifiedName~Nacha|FullyQualifiedName~Mapping|FullyQualifiedName~BatchNumber" \
  -v minimal
```
Resultado:
- Total: **154**
- Passed: **154**
- Failed: **0**
- Skipped: **0**

### 13.6 Suite completa
Comando:
```bash
dotnet test ACHInterbank.sln -c Release --no-build -v minimal
```
Resultado:
- Total: **278**
- Passed: **253**
- Failed: **25**
- Skipped: **0**

Comparación con baseline reciente documentado:
- Se mantiene exactamente en **278/253/25/0**.

### 13.7 Validación de no cambios en crypto real
Comandos:
```bash
git diff -- src/Cfa.ACHInterbank.Application/ACHSobreDigital/Implementation/CryptoServiceScoped.cs
git diff -- src/Cfa.ACHInterbank.Application/Services/EncryptionService/Implementations/RsaKeyProvider.cs
```
Resultado:
- Ambos diffs vacíos en esta revalidación.
