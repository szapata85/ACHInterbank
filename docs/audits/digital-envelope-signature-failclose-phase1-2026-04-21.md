# Digital Envelope Signature Fail-Close - Fase 1 (2026-04-21)

## 1) Qué se endureció

- Se implementó validación obligatoria de firma para flujo de apertura de sobre digital (`OpenEnvelopeAsync`) mediante `IDigitalEnvelopeSignatureValidator`.
- Se removió la ruta efectiva donde la verificación de firma estaba comentada/no aplicada y se agregó fallo explícito cuando la firma no valida.

## 2) Cómo funciona fail-close

1. Se descifra sobre digital igual que antes (RSA + AES + IV derivado desde `identifier`).
2. Se extrae `signedData` y contenido plano.
3. Se ejecuta validación de firma RSA (`SHA256withRSA`).
4. Si la firma es inválida y `FailCloseOnInvalidSignature=true`, se lanza `DigitalEnvelopeSignatureValidationException` y no se retorna NACHA plano.

## 3) Flags de compatibilidad

`DigitalEnvelope:SignatureValidation`:

- `EnableSignatureValidation`
- `FailCloseOnInvalidSignature`
- `FailWhenSignerCertificateMissing`
- `FailWhenSignerCertificateExpired`
- `ValidateSignerCertificateThumbprint`
- `ValidateSignerCertificateChain`
- `AllowLegacyUnsignedEnvelope`
- `LogSignatureValidationDetails`
- `AuditInvalidSignature`
- `Environment`

## 4) Error codes

- `SIGNATURE_VALIDATION_FAILED`
- `SIGNER_CERTIFICATE_MISSING`
- `SIGNER_CERTIFICATE_EXPIRED`
- `SIGNER_CERTIFICATE_NOT_TRUSTED`
- `SIGNATURE_ALGORITHM_NOT_SUPPORTED`
- `SIGNED_CONTENT_MISMATCH`
- `SIGNATURE_VALIDATION_DISABLED_WARNING`
- `LEGACY_UNSIGNED_ENVELOPE_NOT_ALLOWED`

## 5) Auditoría

Se registra evento `InboundSignatureValidation` en `DigitalEnvelopeOperationLogs` con:
- resultado,
- error code (sanitizado),
- thumbprint/serial enmascarados,
- algoritmo de firma,
- indicadores `failClose` y `legacyBypass`.

No se audita NACHA plano ni private key.

## 6) Qué no se tocó

- Estructura XML del sobre.
- `identifier`.
- IV/derivación.
- AES.
- RSA cifrado de llave.
- padding.
- ZIP/Base64.
- workflows automáticos (PostgreSQL workflow permanece manual-only).

## 7) Pruebas

- Setup usado en entorno:
  - `bash scripts/codex/setup-codex-env.sh`
  - `export DOTNET_ROOT=$HOME/.dotnet`
  - `export PATH=$HOME/.dotnet:$HOME/.dotnet/tools:$PATH`
  - `dotnet --info` (SDK 10.0.201)
  - `dotnet ef --version` (10.0.7)
- `dotnet build ACHInterbank.sln -c Release`
- `dotnet test ... --filter "FullyQualifiedName~Signature|FullyQualifiedName~OpenEnvelope|FullyQualifiedName~DigitalEnvelope|FullyQualifiedName~CertificateResolver|FullyQualifiedName~SecretResolver"`
- `dotnet test ... --filter "FullyQualifiedName~Nacha|FullyQualifiedName~Mapping|FullyQualifiedName~BatchNumber"`
- `rg -n "Password|PfxPassword|PrivateKey|RawPrivate|SecretRef|Secret|RawData|ToBase64String|Export" ...`

Resultado de ejecución:
- Build: OK.
- Filtro Signature/OpenEnvelope/DigitalEnvelope/CertificateResolver/SecretResolver: `21/21 passed`.
- No regresión NACHA/Mapping/BatchNumber: `154/154 passed`.

## 8) Riesgos residuales

- `identifier/IV` interoperable oficial sigue pendiente de vector externo.
- `ValidateSignerCertificateChain` queda configurable y por defecto deshabilitado en fase 1 para evitar falsos negativos por trust chain de ambientes.
- existe compatibilidad temporal con `AllowLegacyUnsignedEnvelope` en ambientes no productivos.

## 9) Próximos pasos

1. Activar validación de cadena por ambiente con truststore controlado.
2. Definir política productiva para deshabilitar bypass legacy unsigned.
3. Validar interoperabilidad con vector oficial para `identifier/IV`.
