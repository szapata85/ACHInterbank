# Certificate SecretRef Resolution - Fase 1 (2026-04-21)

## 1) Arquitectura implementada

Se implementó un puerto estable para resolución de secretos de certificados privados:
- `ICertificateSecretResolver`
- `ICertificateSecretProvider`
- `ICertificateSecretProviderResolver`

Modelos:
- `CertificateSecretResolutionRequest`
- `CertificateSecretResolutionResult`
- `CertificateSecretProviderType`
- `CertificateSecretMaterial`

## 2) Providers implementados

1. `InMemoryCertificateSecretProvider`
   - uso controlado para pruebas/desarrollo,
   - resuelve `SecretRef` a `X509Certificate2` con private key en memoria,
   - no persiste secreto.

2. `ExternalSecretReferenceCertificateProvider`
   - placeholder controlado,
   - retorna `SECRET_PROVIDER_NOT_CONFIGURED` cuando no hay backend real.

3. `KeyVaultCertificateSecretProvider`
   - placeholder,
   - retorna `KEYVAULT_NOT_CONFIGURED`/`KEYVAULT_PROVIDER_DISABLED`.

4. `HsmCertificateSecretProvider`
   - placeholder,
   - retorna `HSM_NOT_CONFIGURED`/`HSM_PROVIDER_DISABLED`.

Resolver:
- `CertificateSecretProviderResolver` selecciona provider por `CertificateStorageMode`.
- `CertificateSecretResolver` unifica resolución y error tipado.

## 3) Integración con Certificate Management y Sobre Digital

- `DigitalEnvelopeCertificateResolver` ahora intenta resolver private key vía `ICertificateSecretResolver` cuando:
  - el propósito requiere private key,
  - existe `SecretRef` en versión activa,
  - el storage mode apunta a proveedor externo/keyvault/hsm.
- Si falla y fallback legacy está habilitado, usa legacy.
- Si falla y fallback está deshabilitado, retorna error claro.

## 4) Seguridad de secretos

- No se persiste password PFX en nuevo modelo.
- No se guarda private key en claro en BD.
- `SecretRef` se enmascara en resultados de resolución (`****xxxxxx`).
- No se exponen secretos por API.
- No se registran bytes PFX ni private key en auditoría/logs.

## 5) Auditoría / sanitización

Se registra por resolución:
- propósito,
- cámara,
- ambiente,
- resultado,
- error code,
- actor,
- certificateVersionId (si aplica).

Se evita registrar secreto completo, password y material privado.

## 6) Configuración

Nueva configuración tipada:
- `DigitalEnvelope:CertificateSecretResolver`
  - `EnableInMemoryProvider`
  - `EnableExternalSecretReferenceProvider`
  - `EnableKeyVaultProvider`
  - `EnableHsmProvider`
  - `FailIfSecretProviderUnavailable`
  - `MaskSecretRefInLogs`
  - `DisableInMemoryProviderInProduction`

## 7) Pruebas

Se agregaron pruebas para:
- resolución in-memory de certificado privado por SecretRef,
- error cuando provider no está configurado,
- enmascaramiento de SecretRef,
- uso de private key desde Certificate Management cuando SecretRef resuelve,
- fallback legacy cuando SecretRef falla y está permitido,
- fallo claro cuando SecretRef falla y fallback está deshabilitado.

Revalidación ejecutada en este entorno Codex (2026-04-21 UTC):
- Setup: `bash scripts/codex/setup-codex-env.sh`
- Variables:
  - `export DOTNET_ROOT=$HOME/.dotnet`
  - `export PATH=$HOME/.dotnet:$HOME/.dotnet/tools:$PATH`
- `dotnet --info`: SDK `10.0.201`.
- `dotnet ef --version`: `10.0.7`.
- `dotnet restore ACHInterbank.sln`: OK.
- `dotnet build ACHInterbank.sln -c Release`: OK (0 errores, warnings de nulabilidad preexistentes).
- Filtro SecretRef/SecretResolver/CertificateResolver/DigitalEnvelope/CertificateManagement: `24/24 passed`.
- No regresión NACHA/Mapping/BatchNumber: `154/154 passed`.

## 8) Riesgos residuales

- KeyVault/HSM reales siguen como placeholder (sin conexión productiva).
- Persisten rutas legacy por compatibilidad gradual.
- Hardening fail-close de firma y validación interoperable identifier/IV continúan fuera de alcance.

## 9) Próximos pasos

1. Implementar adapter real a secret manager (KeyVault/HSM) por ambiente.
2. Endurecer política de fallback en producción.
3. Integrar métricas/alertas de tasa de fallback por propósito.
4. Definir rollout para fail-close de firma sin romper compatibilidad.
