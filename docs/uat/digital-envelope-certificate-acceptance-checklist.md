# Checklist UAT — Sobre digital, firma y certificados

## 1. Propósito
Validar en UAT:
- creación de sobre digital;
- apertura/descifrado;
- firma obligatoria;
- validación de firma;
- certificados X.509;
- llave privada;
- llave pública;
- vigencia;
- cadena;
- auditoría;
- rechazo fail-close;
- criterios de salida NO-GO cripto.

## 2. Estado actual
- Base técnica implementada: **sí**.
- GO técnico por componente implementado: **sí, condicionado**.
- GO UAT controlado: **sí, parcial**.
- NO-GO productivo: **sí**.
- El checklist no habilita producción.
- Legacy/bypass exitoso: **no permitido**.
- Revocación CRL/OCSP: **pendiente/no implementada**.
- EKU/KeyUsage: **pendiente/no implementado**.
- Runbook operativo de certificados: **pendiente**.

## 3. Fuentes
- `docs/audits/digital-envelope-signature-certificate-matrix-current.md`
- `tests/Cfa.ACHInterbank.Tests/DigitalEnvelopeCertificateCharacterizationTests.cs`
- `tests/Cfa.ACHInterbank.Tests/DigitalEnvelopeSignatureFailCloseTests.cs`
- `src/Cfa.ACHInterbank.Application/ACHSobreDigital/Implementation/CryptoServiceScoped.cs`
- `src/Cfa.ACHInterbank.Application/ACHSobreDigital/Implementation/DigitalEnvelopeSignatureValidator.cs`
- `src/Cfa.ACHInterbank.Application/Services/EncryptionService/Implementations/RsaKeyProvider.cs`
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/CertificateManagement/DigitalEnvelopeCertificateResolver.cs`
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/CertificateManagement/DigitalEnvelopeSignatureAuditService.cs`
- `src/Cfa.ACHInterbank.Domain/Models/ACHSobreDigital/DigitalEnvelopeModel.cs`
- `src/Cfa.ACHInterbank.Domain/Models/ACHSobreDigital/SignedMessageModel.cs`
- `docs/audits/go-nogo-scorecard-funcional-normativo-2026-04-26.md`
- `docs/audits/s1-matriz-maestra-trazable-funcional-normativa-2026-04-26.md`

## 4. Alcance UAT
- sobre válido firmado;
- sobre sin firma;
- firma alterada;
- contenido alterado;
- certificado firmante vencido;
- certificado firmante aún no vigente;
- certificado receptor vencido;
- certificado sin private key;
- certificado público para validación;
- chain validation on/off;
- auditoría OK/FAILED;
- no secretos en logs;
- evidencia para operaciones/riesgo/compliance.

## 5. Checklist sobre digital
| ID | Control | Resultado esperado | Evidencia | Estado | Observación |
|---|---|---|---|---|---|
| DE-UAT-001 | Crear sobre digital válido | Sobre generado | archivo y log | Pendiente | XML custom actual |
| DE-UAT-002 | Abrir sobre digital válido | Contenido recuperado íntegro | salida + hash | Pendiente | fail-close activo |
| DE-UAT-003 | RecipientInfo obligatorio | Rechazo controlado | errorCode/audit | Pendiente | no plano |
| DE-UAT-004 | CertificateInfo obligatorio | Rechazo controlado | errorCode/audit | Pendiente | no plano |
| DE-UAT-005 | EncryptedKey obligatorio | Rechazo controlado | errorCode/audit | Pendiente | no plano |
| DE-UAT-006 | EncryptedContent obligatorio | Rechazo controlado | errorCode/audit | Pendiente | no plano |
| DE-UAT-007 | Identifier presente | Campo presente | payload envelope | Pendiente | trazabilidad |
| DE-UAT-008 | Timestamp presente | Campo presente | payload envelope | Pendiente | trazabilidad |
| DE-UAT-009 | No retorno plano ante error | Nunca se retorna contenido plano | logs/audit | Pendiente | fail-close |
| DE-UAT-010 | Formato XML custom | Confirmado | evidencia técnica | Pendiente | no PKCS#7 nativo |
| DE-UAT-011 | No SignedCms/EnvelopedCms | Confirmado | evidencia técnica | Pendiente | formato actual |

## 6. Checklist firma digital
- sobre firmado válido se acepta.
- `SHA256withRSA` registrado/validado.
- hash `SHA-256` validado.
- contenido alterado se rechaza.
- firma alterada se rechaza.
- sobre unsigned se rechaza.
- `EnableSignatureValidation=false` no habilita bypass.
- `AllowLegacyUnsignedEnvelope=true` no habilita éxito.
- fail-close obligatorio.
- auditoría de éxito/fallo.

## 7. Checklist certificados X.509
- certificado firmante existe.
- certificado receptor/cifrado existe.
- certificado descifrado existe.
- certificado público valida firma.
- certificado público cifra.
- certificado privado firma.
- certificado privado descifra.
- issuer/serial/thumbprint se registran o resuelven si aplica.
- PFX/CER validado si aplica.
- store X509 validado si aplica.
- SecretRef/OpenBao validado solo si aplica y existe en ambiente.

## 8. Checklist llave privada
| Operación | Requiere private key | Resultado esperado | Evidencia | Estado |
|---|---|---|---|---|
| SIGN | Sí | Falla si no hay llave privada | errorCode + audit | Pendiente |
| DECRYPT | Sí | Falla si no hay llave privada | errorCode + audit | Pendiente |
| VERIFY SIGNATURE | No | Opera con cert público | test/evidencia UAT | Pendiente |
| ENCRYPT TO RECIPIENT | No | Opera con cert público | test/evidencia UAT | Pendiente |
| Cert sin key para firmar | Sí aplica | Rechazo funcional | `CERTIFICATE_PRIVATE_KEY_REQUIRED/NOT_AVAILABLE` | Pendiente |
| Cert sin key para descifrar | Sí aplica | Rechazo funcional | `CERTIFICATE_PRIVATE_KEY_REQUIRED/NOT_AVAILABLE` | Pendiente |

No exponer password/PFX/private key en logs.

## 9. Checklist vigencia
- certificado firmante expirado falla.
- certificado firmante aún no vigente falla.
- certificado local SIGN expirado falla.
- certificado local DECRYPT expirado falla.
- certificado ENCRYPT expirado falla.
- certificado vigente pasa.
- errores funcionales esperados:
  - `SIGNER_CERTIFICATE_NOT_YET_VALID`
  - `SIGNER_CERTIFICATE_EXPIRED`
  - `CERTIFICATE_NOT_YET_VALID`
  - `CERTIFICATE_EXPIRED`

## 10. Checklist cadena de confianza
- `ValidateSignerCertificateChain=true` rechaza self-signed no confiado.
- `ValidateSignerCertificateChain=false` permite dev/UAT controlado con self-signed.
- chain inválida falla con error funcional.
- trust store productivo definido.
- certificados de cámara/entidad inventariados.
- evidencia de trust store adjunta.
- errores esperados:
  - `SIGNER_CERTIFICATE_NOT_TRUSTED`
  - `CERTIFICATE_CHAIN_NOT_TRUSTED`

## 11. Checklist revocación
CRL/OCSP real no está implementado actualmente; modo actual: `X509RevocationMode.NoCheck`.

- confirmar `NoCheck` documentado.
- definir política requerida para productivo.
- definir si revocación será CRL, OCSP, offline o proceso operacional.
- identificar fuente de listas/servicio.
- identificar responsable operativo.
- definir comportamiento ante revocación no disponible.
- mantener criterio NO-GO hasta política aprobada.

## 12. Checklist EKU/KeyUsage
EKU/KeyUsage no está implementado actualmente.

- EKU requerido para firma (si aplica).
- EKU requerido para cifrado (si aplica).
- KeyUsage `DigitalSignature` (si aplica).
- KeyUsage `KeyEncipherment/DataEncipherment` (si aplica).
- decisión negocio/riesgo/compliance.
- criterio para productivo.

## 13. Checklist auditoría criptográfica
- result `SUCCESS`.
- result `FAILED`.
- `errorCode`.
- `signerThumbprint`.
- `signerSerialNumber`.
- `signatureAlgorithm`.
- `failCloseApplied`.
- `legacyBypassUsed` siempre `false` en flujos exitosos.
- `actor`.
- `timestamp`.
- hash/correlationId si aplica.
- `secretRefMasked` si aplica.
- no private key.
- no password PFX.
- no material sensible.

## 14. Checklist no legacy/no bypass
- sobre unsigned rechazado.
- `AllowLegacyUnsignedEnvelope` no permite aceptación.
- `EnableSignatureValidation=false` no permite aceptación.
- `LegacyBypassUsed` no aparece como éxito.
- no retorno de contenido plano por bypass.
- evidencia auditada del rechazo.
- decisión justificada: no hay producción y no se requiere compatibilidad histórica.

## 15. Checklist almacenamiento/permisos certificados
- PFX protegido.
- contraseña PFX protegida.
- store `CurrentUser/LocalMachine` definido si aplica.
- permisos IIS/Linux validados si aplica.
- usuario de proceso validado.
- acceso a private key validado.
- certificados públicos separados de privados.
- no secretos en appsettings.
- si aplica OpenBao/SecretRef, validar acceso y masking.
- evidencia de permisos adjunta.

## 16. Checklist relación con ACH/CENIT/NACHA
- sobre protege archivo/contenido.
- cripto no reemplaza validación NACHA.
- cripto no cambia causales.
- cripto no cambia ROR.
- cripto no cambia incoming/outbound.
- cripto no cambia ciclos CENIT/neteo/liquidez/CUD.
- cripto no cambia contabilidad/conciliación.
- evidencia cripto se correlaciona con archivo/proceso.

## 17. Evidencia UAT requerida
- archivo de prueba válido firmado.
- archivo unsigned rechazado.
- archivo con firma alterada rechazado.
- archivo con contenido alterado rechazado.
- certificado vencido rechazado.
- certificado no vigente rechazado.
- certificado sin private key rechazado.
- certificado público usado para verificar.
- evidencia de trust store.
- configuración chain on/off.
- evidencia de NoCheck/revocación pendiente.
- logs/auditoría OK/FAILED.
- evidencia de no secretos.
- acta UAT.
- firmas negocio/operaciones/riesgo/compliance/tecnología.

## 18. Criterios de salida de NO-GO cripto
1. Firma obligatoria validada.
2. Sobres unsigned rechazados.
3. Firma inválida rechazada.
4. Contenido alterado rechazado.
5. Private key requerida para firmar.
6. Private key requerida para descifrar.
7. Cert público permitido solo para verificar/cifrar.
8. Vigencia validada.
9. Cadena definida por política.
10. Trust store productivo validado.
11. Revocación definida por política.
12. EKU/KeyUsage definido o justificado.
13. Certificados productivos inventariados.
14. Store/permisos validados.
15. Rotación definida.
16. Auditoría revisada.
17. No secretos en logs.
18. Runbook aprobado.
19. Evidencia UAT generada.
20. Aprobación negocio.
21. Aprobación operaciones.
22. Aprobación riesgo/compliance.
23. Aprobación tecnología.
24. Scorecard actualizado.

## 19. Riesgos residuales
- CRL/OCSP no implementado.
- EKU/KeyUsage no implementado.
- trust store productivo no cerrado.
- permisos de private key no validados en ambiente real.
- rotación/caducidad no operativizada.
- runbook pendiente.
- algoritmo/padding pendiente de confirmación normativa/política.
- evidencia UAT real pendiente.
- NO-GO productivo vigente.

## 20. Decisión vigente
- Base técnica implementada: **sí**.
- GO técnico por componente implementado: **sí, condicionado**.
- GO UAT controlado: **sí, parcial**.
- NO-GO productivo: **sí**.
- Este checklist no habilita producción.
- Próximo paso recomendado: `docs(ops): add certificate operations runbook`.

- Referencia runbook operativo de certificados: `docs/ops/certificate-operations-runbook.md` (no modifica decisión NO-GO productivo).
