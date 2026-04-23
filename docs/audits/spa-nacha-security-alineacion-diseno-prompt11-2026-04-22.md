# Diseño funcional/técnico SPA↔Backend — Gobierno de Certificados + NACHA-M + Sobre Digital (Prompt 11)

**Fecha:** 2026-04-22 (UTC)  
**Fase:** Diseño (sin implementación)  
**Alcance:** Alineación de consola Angular con capacidades backend existentes/propuestas para certificados, generación NACHA-M, sobre digital `.ENV/.env`, auditoría e interoperabilidad.

---

## 1) Restricciones y principios rectores

1. **Angular opera, backend ejecuta criptografía y norma.**
2. **Sin cambios criptográficos en esta fase** (`CryptoServiceScoped`, `OpenEnvelopeAsync`, `identifier/IV`, XML, AES/RSA/padding).
3. **Sin implementación** de pantallas ni endpoints (solo blueprint).
4. **Sin cambios de workflows CI** (se preserva manual-only en PostgreSQL integration tests).
5. **No exposición de secretos** (PFX raw, private keys, passwords, SecretRef completo, contenido plano cuando firma falla).

---

## 2) Inspección inicial (estado actual real)

### 2.1 Backend inspeccionado

#### 2.1.1 NACHA export / NACHA + sobre digital
- Existe `NachaExportController` con:
  - `GET /NachaExport/{cycleId}` (NACHA plano).
  - `GET /NachaExport/{cycleId}/sobre-digital?forceEncryption=true|false` (NACHA + sobre digital).
- En el flujo actual:
  - usa `INachaFileBuilder` para construir NACHA;
  - usa `IExternalFileNamePolicy` para nombre externo;
  - audita export en `IAchFileExportAuditService`;
  - cifra por `ICryptoServiceScoped.CreateEnvelopeAsync` cuando aplica.

#### 2.1.2 Sobre digital manual
- Existe `SobreDigitalController` con:
  - `POST /SobreDigital/encrypt` (recibe archivo y devuelve `.ENV`).
  - `POST /SobreDigital/decrypt` (recibe `.ENV` y devuelve plano).
- Usa `ICryptoServiceScoped` para cifrado/descifrado backend.
- **Gap de diseño/seguridad:** no tiene `[Authorize]` ni contrato explícito de error sanitizado/operationId para operación financiera trazable.

#### 2.1.3 Gobierno de certificados
- Existen dos superficies:
  1) `DigitalEnvelopeCertificatesController` (`/nacha-security/certificates`) para listado/carga/eliminación básica.
  2) `CertificateManagementController` (`/nacha-security/certificates/management`) con capacidades más maduras:
     - carga pública/privada,
     - listado filtrado,
     - versiones,
     - activar/revocar,
     - validar,
     - auditoría.
- `CertificateManagementController` ya enmascara `SecretRef` vía `MaskSecret`.

#### 2.1.4 Auditoría existente
- `AuditLogsController` (`/api/audit-logs`) permite consulta paginada/filtrada.
- `AchFileExportAuditService` registra generación de archivos NACHA (`isEncrypted`, totales, nombre de archivo).
- `CertificateManagementServices` incluye auditoría de carga y `CertificateUsageLoggerService` con `operationId`.
- `CryptoServiceScoped.OpenEnvelopeAsync` audita validación de firma vía `IDigitalEnvelopeSignatureAuditService`.

#### 2.1.5 Interoperabilidad/vector oficial
- Existe harness y documentación operativa para vector oficial.
- Estado formal documentado: hardening `identifier/IV` bloqueado hasta vector oficial.

### 2.2 Frontend inspeccionado

#### 2.2.1 Estructura y rutas
- Existe feature `nacha-security` con rutas:
  - `/nacha-security/certificates`
  - `/nacha-security/sobre-digital`
- Existe feature `ach-cycles` con pantalla de export NACHA:
  - `/ach-cycles/nacha/export` (incluye botón “generar con sobre digital”).

#### 2.2.2 Servicios HTTP ya existentes
- `NachaExportApiService` consume `NachaExport/{cycleId}` y `NachaExport/{cycleId}/sobre-digital`.
- `SobreDigitalService` consume `SobreDigital/encrypt` y `SobreDigital/decrypt`.
- `DigitalEnvelopeCertificatesService` consume `nacha-security/certificates` (superficie básica, no management avanzado).

#### 2.2.3 Auth/roles/permisos
- App usa `authGuard`, `roleGuard`, `permissionGuard`.
- `nacha-security` actual **no** declara guardas finas por operación crítica (aprovecha autenticación general de layout).
- `ach-cycles/nacha/export` sí usa permisos (`CanReadAch`).

#### 2.2.4 UX operacional actual
- Hay anti-doble click básico en export/operación manual (flags de descarga/proceso).
- Descarga es inmediata tras respuesta backend (blob).
- **Gap:** no existe modelo explícito de `operationId`, estado asíncrono, ni política de descarga autorizada por operación.

---

## 3) Matriz de cobertura: qué ya existe vs qué falta

## 3.1 Pantallas existentes
1. Gestión básica de certificados de sobre digital.
2. Herramienta manual cifrar/descifrar.
3. Export de NACHA con opción de sobre digital.

## 3.2 Pantallas faltantes (objetivo Prompt 11)
1. Dashboard de seguridad NACHA-M/Sobre Digital.
2. Gobierno de certificados por versiones (flujo management completo).
3. Generación NACHA-M con preview/validación/totales/nombre externo.
4. Generación NACHA-M cifrada “de una vez” con trazabilidad completa.
5. Operación manual endurecida por rol + estado + reglas fail-close visibles.
6. Auditoría especializada de sobre digital/certificados/NACHA export.
7. Interoperabilidad/vector oficial con Go/No-Go de hardening.

## 3.3 Endpoints existentes útiles
- `NachaExport/*` (plano y cifrado).
- `SobreDigital/encrypt|decrypt`.
- `nacha-security/certificates`.
- `nacha-security/certificates/management/*`.
- `api/audit-logs`.

## 3.4 Endpoints faltantes/recomendados (diseño)
> **Nota:** propuesta de contrato; no implementar en esta fase.

1. **Operaciones con `operationId`**
   - `POST /nacha-security/operations/nacha/generate`
   - `POST /nacha-security/operations/nacha/generate-encrypted`
   - `POST /nacha-security/operations/envelope/manual-encrypt`
   - `POST /nacha-security/operations/envelope/manual-decrypt`
2. **Estado y descarga segura**
   - `GET /nacha-security/operations/{operationId}`
   - `POST /nacha-security/operations/{operationId}/authorize-download`
   - `GET /nacha-security/operations/{operationId}/download`
3. **Auditoría especializada**
   - `GET /nacha-security/audit/operations`
   - `GET /nacha-security/audit/certificates`
4. **Interoperabilidad oficial**
   - `GET /nacha-security/interoperability/status`
   - `POST /nacha-security/interoperability/run-harness`
   - `GET /nacha-security/interoperability/reports/{reportId}`

---

## 4) Diseño de módulo SPA propuesto

## 4.1 Nombre y principio
- Módulo recomendado: `NachaSecurityModule` (evolución del existente), con submódulos:
  - `SecurityDashboardModule`
  - `CertificateGovernanceModule`
  - `NachaGenerationModule`
  - `DigitalEnvelopeOperationsModule`
  - `SecurityAuditModule`
  - `InteroperabilityModule`

## 4.2 Rutas propuestas

```text
/security/digital-envelope/dashboard
/security/certificates
/security/certificates/:id/versions
/security/nacha/generate
/security/nacha/generate-encrypted
/security/digital-envelope/manual-encrypt
/security/digital-envelope/manual-decrypt
/security/digital-envelope/audit
/security/digital-envelope/interoperability
```

## 4.3 Layout/UX operacional

1. **Dashboard**
   - KPIs: operaciones hoy, fallas fail-close, certificados por vencer, estado vector oficial.
   - Alertas operativas (sin datos sensibles).
2. **Patrón transaccional unificado**
   - Confirmación previa en acciones críticas.
   - Estado `PENDING/RUNNING/SUCCESS/FAILED`.
   - Visualización de `operationId`, hash, timestamps UTC.
3. **Descarga**
   - Nunca descarga directa sin validar operación/rol.
   - Flujo explícito “autorizar descarga” + expiración.
4. **No mostrar plano por defecto**
   - Solo metadata + hash + nombre.
   - Plano solo si backend lo autoriza y firma válida.

---

## 5) Modelo de autorización (propuesto)

Roles mínimos exigidos por negocio:
- `AdminCertificados`
- `OperadorNacha`
- `OperadorSobreDigital`
- `SupervisorOperaciones`
- `Auditor`
- `SoporteTecnico`
- `AdministradorSistema`

### 5.1 Matriz resumida
- Activar/revocar/reemplazar certificado: `AdminCertificados` (+ opcional 4 ojos con `SupervisorOperaciones`).
- Generar NACHA-M plano: `OperadorNacha`.
- Generar NACHA-M cifrado: `OperadorNacha` (+ política opcional supervisor).
- Cifrado manual: `OperadorSobreDigital`.
- Descifrado manual: `OperadorSobreDigital` restringido.
- Descargar plano: permiso explícito adicional (policy dedicada).
- Ver auditoría: `Auditor`, `SupervisorOperaciones`, `SoporteTecnico` (lectura sanitizada).

### 5.2 Guardas Angular propuestas
- `permissionGuard` por ruta + `criticalActionGuard` para confirmaciones en cliente.
- Resolver de permisos efectivos al entrar al módulo para ocultar acciones no autorizadas.

---

## 6) Contratos DTO propuestos (backend→SPA)

## 6.1 Operación estándar

```json
{
  "operationId": "op_20260422_000123",
  "operationType": "NACHA_GENERATE_ENCRYPTED",
  "status": "SUCCESS",
  "requestedAtUtc": "2026-04-22T10:20:30Z",
  "finishedAtUtc": "2026-04-22T10:20:33Z",
  "requestedBy": "user@bank",
  "error": null,
  "artifacts": {
    "downloadAvailable": true,
    "downloadExpiresAtUtc": "2026-04-22T10:35:33Z",
    "externalFileName": "ABC123.001.1.ENV",
    "plainHashSha256": "...",
    "envelopeHashSha256": "..."
  },
  "certificateSummary": {
    "signingThumbprintMasked": "****A1B2",
    "encryptionThumbprintMasked": "****C3D4",
    "secretRefMasked": "****9XYZ"
  },
  "audit": {
    "traceId": "...",
    "failCloseApplied": true,
    "legacyFallbackUsed": false
  }
}
```

## 6.2 Error sanitizado

```json
{
  "code": "SIGNATURE_VALIDATION_FAILED",
  "message": "No fue posible validar la firma del sobre digital.",
  "operationId": "op_20260422_000123",
  "retryable": false,
  "timestampUtc": "2026-04-22T10:20:34Z"
}
```

## 6.3 Interoperabilidad/vector

```json
{
  "officialVectorStatus": "PENDING",
  "officialMetadataLoaded": false,
  "lastHarnessRunUtc": "2026-04-21T22:10:00Z",
  "identifierIvHardening": {
    "allowed": false,
    "reason": "Official vector not approved"
  },
  "goNoGo": "NO_GO"
}
```

---

## 7) Manejo de errores y fail-close (SPA)

1. **Catálogo de códigos** (mostrar solo código+mensaje sanitario):
   - `SIGNATURE_VALIDATION_FAILED`
   - `LEGACY_UNSIGNED_ENVELOPE_NOT_ALLOWED`
   - `NACHA_VALIDATION_ERROR`
   - `UNAUTHORIZED_DOWNLOAD`
   - `CERTIFICATE_EXPIRED`
   - `OPERATION_NOT_FOUND`
2. **Regla UX crítica:**
   - si backend marca firma inválida/fail-close, **no habilitar preview ni descarga de plano**.
3. **Trazabilidad visible:**
   - todo error debe renderizar `operationId` para soporte/auditoría.

---

## 8) Descarga segura (flujo objetivo)

1. SPA solicita operación (generar/cifrar/descifrar).
2. Backend retorna `operationId` y estado.
3. SPA consulta estado hasta `SUCCESS` o `FAILED`.
4. Para descargar:
   - SPA solicita `authorize-download`.
   - Backend valida rol/permisos/reglas fail-close.
   - Backend emite token temporal de descarga o habilita endpoint por ventana corta.
5. Backend registra evento de descarga en auditoría.
6. Archivo temporal expira y se elimina.

---

## 9) Plan de pruebas (diseño QA)

## 9.1 Backend API/seguridad
1. Autorización por rol para cada endpoint propuesto.
2. `manual-decrypt` bloqueado si firma inválida (assert: no contenido plano).
3. Descarga rechazada sin autorización explícita.
4. Sanitización de errores (sin stack trace/secretos).

## 9.2 Integración SPA↔Backend
1. Flujo NACHA plano end-to-end con `operationId`.
2. Flujo NACHA cifrado end-to-end con hashes y nombre externo.
3. Flujo manual cifrar/descifrar con estados y UX de loading/disabled.
4. Error handling con códigos y reintento controlado.

## 9.3 No regresión funcional obligatoria
1. Mantener pruebas NACHA/Mapping/BatchNumber en baseline verde esperado del programa.
2. Mantener DigitalEnvelope/Signature/OpenEnvelope en verde.
3. No cambiar `identifier/IV` sin vector oficial (assert de guardrail documental y de pruebas).

## 9.4 Auditoría/regulatorio
1. Cada operación debe generar traza consultable por `operationId`.
2. Auditor consulta sin exposición de secretos.
3. Validación de retención/expiración de artefactos temporales.

---

## 10) Brechas priorizadas (backlog de diseño)

## 10.1 P0 (antes de salir a operación crítica)
1. Unificar modelo por `operationId` para generación/cifrado/descifrado/descarga.
2. Endurecer autorización en sobre digital manual y descarga.
3. Cerrar contrato de error sanitizado homogéneo.
4. Integrar SPA con `CertificateManagementController` (versionado, activación, revocación, validación).

## 10.2 P1 (siguiente iteración)
1. Dashboard de seguridad operacional.
2. Auditoría especializada para sobre digital/certificados.
3. Interoperabilidad UI con estado vector oficial y reportes harness.

## 10.3 P2 (tras vector oficial)
1. Flujo de Go/No-Go de hardening `identifier/IV` desde backend.
2. Automatizar reportes ejecutivos de conformidad de interoperabilidad.

---

## 11) Conclusión ejecutiva

El repositorio ya tiene base funcional para:
- exportar NACHA-M,
- cifrar/descifrar sobre digital en backend,
- administrar certificados (básico + management),
- auditar partes críticas.

La alineación objetivo del Prompt 11 requiere **orquestación operacional y de seguridad** (operationId, autorizaciones finas, contratos unificados, rutas SPA especializadas, descarga segura y observabilidad), sin alterar criptografía ni reglas bloqueadas por vector oficial.
