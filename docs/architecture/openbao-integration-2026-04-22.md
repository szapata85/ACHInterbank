# OpenBao Integration Decision (2026-04-22)

## Decisión técnica
- Implementación principal: **OpenBao on-prem** en Docker Compose.
- Alternativa documentada: HashiCorp Vault (compatible API), no implementada como principal en esta fase.
- Estrategia de bootstrap UAT: **sidecar `openbao-bootstrap` automatizado** (idempotente), para dejar el stack operativo con `docker compose up -d --build`.

## Arquitectura objetivo
- Angular: consola operativa (sube `.pfx` + password temporal por request).
- API .NET: valida PKCS#12 en memoria, extrae metadata X.509, guarda secreto en OpenBao, persiste metadata + `SecretRef` en BD.
- PostgreSQL: metadata/auditoría/versionado, nunca private key en claro.
- OpenBao: secreto real (PKCS#12 base64 + password) bajo KV v2.
- Bootstrap UAT: inicializa/unseal/configura policy+token y publica token en volumen compartido para API.

## SecretRef flow
1. Upload privado `POST /nacha-security/certificates/management/private`.
2. API valida PFX en memoria (`X509CertificateLoader`).
3. API escribe en OpenBao `secret/data/certificates/<environment>/ch-<id>/<purpose>/v<version>`.
4. API persiste versión con `PrivateMaterialStorageMode=OpenBaoReference` y `SecretRef=openbao://...`.
5. Resolvedor (`DigitalEnvelopeCertificateResolver`) solicita `ICertificateSecretResolver`.
6. `OpenBaoCertificateSecretProvider` recupera secreto y reconstruye `X509Certificate2` en memoria (ephemeral keyset).
7. Flujo de firma/descifrado consume material sin exponerlo al frontend.

## Riesgos y mitigación
- OpenBao no disponible: fail-close si `FailIfSecretProviderUnavailable=true`.
- Token ausente/inválido: error controlado `OPENBAO_TOKEN_MISSING/OPENBAO_READ_FAILED`.
- SecretRef inválido: `OPENBAO_SECRETREF_INVALID`.

## UAT vs Producción
- UAT/laboratorio: bootstrap automático aceptado para reducir fricción operativa.
- Producción: init/unseal y emisión de credenciales deben separarse por funciones, con hardening y controles adicionales.

## Justificación regulatoria/seguridad
- Evita persistencia de private key/password en BD.
- Mantiene backend como único ejecutor criptográfico.
- Permite trazabilidad vía metadata + logs de uso/auditoría.
