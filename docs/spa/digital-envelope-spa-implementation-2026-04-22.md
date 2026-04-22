# Digital Envelope SPA Implementation - Prompt 13 (revalidación formal)

Fecha de revalidación: 2026-04-22 (UTC)

## 1) Commit Angular identificado
- Commit: `fcdb5895e2803a1e394860d536c5f587350ef96f`
- Mensaje: `feat(ui): implement nacha security operations module for spa`
- Alcance: cambios en `web/ach-interbank-ui` (rutas, componentes, servicios y modelos de `nacha-security`).

## 2) Evidencia ejecutada (comandos reales)
```bash
git status --short
git log --oneline -8
git show --stat HEAD
git show --name-only HEAD
git log --oneline -- web/ach-interbank-ui | head -n 10

find web/ach-interbank-ui/src/app -maxdepth 5 -type f | sort | grep -Ei "nacha|security|certificate|envelope|interoperability|audit|operation" || true
rg -n "NachaSecurityOperationsApiService|CertificateManagementApiService|InteroperabilityApiService|manual-encrypt|manual-decrypt|generate-encrypted|authorizeDownload|downloadArtifact|SIGNATURE_VALIDATION_FAILED|secretRefMasked" web/ach-interbank-ui/src -S

cd web/ach-interbank-ui
npm ci --include=dev
npm run build
npm test -- --watch=false --browsers=ChromeHeadless

rg -n "password|pfx|privateKey|secretRef|SecretRef|SecretRefMasked|localStorage|sessionStorage|crypto|subtle|encrypt|decrypt|sign|certificate|download" web/ach-interbank-ui/src -S
rg -n "SIGNATURE_VALIDATION_FAILED|authorizeDownload|downloadArtifact|Blob|createObjectURL|localStorage|sessionStorage|plain|content|downloadAvailable" web/ach-interbank-ui/src/app -S

git diff -- src/Cfa.ACHInterbank.Application/ACHSobreDigital/Implementation/CryptoServiceScoped.cs
git diff -- src/Cfa.ACHInterbank.Application/Services/EncryptionService/Implementations/RsaKeyProvider.cs

sed -n '1,220p' .github/workflows/postgres-integration-tests.yml
```

## 3) Resultado de build/tests SPA
- `npm ci --include=dev`: OK.
- `npm run build`: OK.
- `npm test -- --watch=false --browsers=ChromeHeadless`: FALLA por entorno/herramienta:
  - `No binary for ChromeHeadless browser on your platform. Please, set "CHROME_BIN" env variable.`
  - Error Karma: `TypeError: Cannot read properties of undefined (reading 'filter')`.
  - Error adicional: `invalid rimraf options`.

## 4) Seguridad frontend (revalidación)
- No se encontró uso de `crypto.subtle` en `nacha-security`.
- No se observó persistencia de password/PFX de `nacha-security` en `localStorage` ni `sessionStorage`.
- Se modela y visualiza `secretRefMasked` para certificados/versiones, no `SecretRef` plano.
- Flujo de descarga en manual decrypt: primero autoriza (`authorize-download`) y luego descarga blob (`download`), sin persistir token/archivo en storage del navegador.
- Ante error backend (`SIGNATURE_VALIDATION_FAILED`), el componente presenta error sanitizado y no renderiza contenido NACHA plano.

## 5) Restricciones de alcance confirmadas
- No se implementó criptografía en Angular.
- No se cambió `CryptoServiceScoped`, `OpenEnvelopeAsync`, `RsaKeyProvider`, ni `identifier/IV`.
- Workflow de PostgreSQL permanece manual-only (`workflow_dispatch`).

---

## Revalidación Prompt 14 (2026-04-22 UTC)

- Se validó implementación de permisos finos (`CanGenerate*`, `CanManual*`, `CanDownload*`, `CanViewNachaSecurityAudit`, `CanManageCertificates`) en backend + SPA.
- Se confirmó endurecimiento de descarga segura en backend y frontend:
  - autorización previa,
  - permisos por tipo de artefacto,
  - bloqueo de plano si `SIGNATURE_VALIDATION_FAILED`,
  - sanitización de filename en cliente.
- No se introdujo WebCrypto para cifrado/firma/descifrado de negocio en Angular.
- No se modificó criptografía restringida (identifier/IV/XML/AES/RSA/padding).
- Pruebas de no regresión ejecutadas:
  - backend permisos/descarga: 12/12,
  - DigitalEnvelope/Signature/OpenEnvelope/SecretResolver: 36/36,
  - NACHA/Mapping/BatchNumber: 166/166.
- Frontend:
  - `npm ci`: OK,
  - `npm run build`: OK,
  - `npm test`: pendiente por limitación de entorno (CHROME_BIN + karma/rimraf).
