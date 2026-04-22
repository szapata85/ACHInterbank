# Integración controlada: Certificate Management → Sobre Digital (2026-04-21)

## 1) Alcance integrado

Se integró resolución de certificados para sobre digital con prioridad en Certificate Management fase 1 y fallback legacy controlado por configuración.

Incluye:
- Opciones tipadas de resolución (`DigitalEnvelopeCertificateOptions`).
- Resolver/adapter `IDigitalEnvelopeCertificateResolver` + `DigitalEnvelopeCertificateResolver`.
- Integración de `RsaKeyProvider` para consumir el resolver.
- Trazabilidad de fuente de certificado (CertificateManagement/Legacy/None).
- Logging de uso para certificados de Certificate Management y bitácora de fallback/errores en `DigitalEnvelopeOperationLogs`.

## 2) Qué NO se tocó

No se modificó:
- formato XML del sobre digital,
- estructura `SignedData`,
- derivación de `identifier`,
- derivación/algoritmo de IV,
- algoritmos/padding (`AES/CBC/PKCS5padding`, `RSA/NONE/PKCS1Padding`),
- frontend,
- SOAP,
- `ExternalFileNamePolicy`,
- lógica NACHA-M.

## 3) Opciones nuevas

`DigitalEnvelope:CertificateResolver`:
- `UseCertificateManagement`
- `AllowLegacyCertificateFallback`
- `FailIfCertificateManagementUnavailable`
- `Environment`
- `DefaultClearingHouseId`
- `PreferActiveCertificateManagementVersion`
- `LogCertificateSource`

Regla operacional:
1. Si `UseCertificateManagement=true`, se intenta resolver activo desde Certificate Management.
2. Si falla y `AllowLegacyCertificateFallback=true`, se usa legacy y se audita fallback.
3. Si falla y fallback está deshabilitado, se retorna error claro (o excepción si `FailIfCertificateManagementUnavailable=true`).
4. Si `UseCertificateManagement=false`, flujo legacy.

## 4) Flujo de resolución integrado

1. `CryptoServiceScoped` solicita certificados por clave lógica (`CertSign`, `CertCrypt`, `CertDecrypt`).
2. `RsaKeyProvider` invoca `IDigitalEnvelopeCertificateResolver`.
3. Resolver intenta Certificate Management por propósito/contexto.
4. Si no hay material utilizable y fallback permitido, usa `DigitalEnvelopeCertificate` legacy.
5. Registra auditoría de fuente/resultado en `DigitalEnvelopeOperationLogs`.
6. Cuando la fuente es Certificate Management, registra `CertificateUsageLog`.

## 5) Fallback legacy

Se mantiene compatibilidad temporal:
- Legacy sigue disponible y no se elimina.
- Se registra warning/auditoría cuando ocurre fallback desde modo Certificate Management.

## 6) Auditoría

Se audita por resolución:
- propósito,
- cámara,
- ambiente,
- source,
- `CertificateVersionId` (si aplica),
- resultado,
- código de error (si aplica),
- actor/proceso.

No se registran secretos ni passwords.

## 7) Evidencia normativa usada (ACH Colombia V32)

### Confirmado
- El documento describe explícitamente el formato Digital Envelope y su estructura XML.
- Se documenta firma con llave privada RSA, cifrado de contenido con AES-256 y cifrado de llave con certificado X.509 receptor usando RSA.
- Se documenta que `identifier` e IV tienen derivación específica para este esquema.

### Parcial
- La guía técnica existe en V32 (Anexo 21 referenciado), pero no se validó interoperabilidad externa en esta fase.

### Requiere confirmación normativa
- Cualquier cambio de formato XML, identifier, IV o algoritmo distinto al definido en la guía.
- Hardening fail-close de verificación de firma sin romper compatibilidad histórica.

## 8) Pruebas ejecutadas

- `dotnet build ACHInterbank.sln -c Release` ✅
- `dotnet test ... --filter "DigitalEnvelope|CertificateManagement|CertificateResolver|ExportEncrypted"` ✅ (19/19)
- `dotnet test ... --filter "Nacha|Mapping|BatchNumber"` ✅ (154/154)

## 9) Riesgos residuales

- El modelo phase 1 actual no resuelve secreto externo real (`SecretRef`) a material privado (vault/hsm pendiente).
- Para propósitos que requieren private key desde Certificate Management, puede requerir fallback legacy hasta completar integración de secretos.
- Hardening de firma fail-close y vector oficial identifier/IV sigue pendiente por fase.

## 10) Próximos pasos

1. Implementar `SecretRef` resolver real (KeyVault/HSM) para private key sin persistencia insegura.
2. Endurecer verificación de firma con estrategia de rollout/fail-close gradual.
3. Ejecutar validación de interoperabilidad cruzada con vectores oficiales.
