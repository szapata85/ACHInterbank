> Nota G3.5.2: las referencias a proveedores de secretos retirados son historicas y obsoletas desde el cleanup `ebf7a8a5`; no describen el stack vigente.

# Matriz vigente — Sobre digital, firma y certificados

## 1. Propósito
Esta matriz consolida el estado actual de sobre digital, firma digital, cifrado/descifrado, certificados X.509, llave privada/pública, validación de vigencia/cadena, revocación, auditoría y riesgos productivos en ACH/CENIT/NACHA.

## 2. Estado actual
- Base técnica implementada: **sí**.
- GO técnico por componente implementado: **sí, condicionado**.
- GO UAT controlado: **sí, parcial**.
- NO-GO productivo: **sí**.
- Firma obligatoria en apertura de sobre: **sí**.
- Legacy/bypass exitoso: **no permitido**.
- Validación de private key: **endurecida**.
- Vigencia/cadena: **implementada/parcial según configuración**.
- Revocación CRL/OCSP: **no implementada**, modo explícito actual `NoCheck`.
- Esta matriz **no habilita producción**.

## 3. Fuentes
- `src/Cfa.ACHInterbank.Application/ACHSobreDigital/Implementation/CryptoServiceScoped.cs`
- `src/Cfa.ACHInterbank.Application/ACHSobreDigital/Implementation/DigitalEnvelopeSignatureValidator.cs`
- `src/Cfa.ACHInterbank.Application/Services/EncryptionService/Implementations/RsaKeyProvider.cs`
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/CertificateManagement/DigitalEnvelopeCertificateResolver.cs`
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/CertificateManagement/DigitalEnvelopeSignatureAuditService.cs`
- `src/Cfa.ACHInterbank.Domain/Models/ACHSobreDigital/DigitalEnvelopeModel.cs`
- `src/Cfa.ACHInterbank.Domain/Models/ACHSobreDigital/SignedMessageModel.cs`
- `tests/Cfa.ACHInterbank.Tests/DigitalEnvelopeCertificateCharacterizationTests.cs`
- `tests/Cfa.ACHInterbank.Tests/DigitalEnvelopeSignatureFailCloseTests.cs`
- `docs/dev/devoluciones-ach-auditoria-cenit-ach-colombia.md`
- `docs/audits/s1-matriz-maestra-trazable-funcional-normativa-2026-04-26.md`
- `docs/audits/go-nogo-scorecard-funcional-normativo-2026-04-26.md`

## 4. Matriz de componentes
| Componente | Archivo | Existe | Estado | Observación |
|---|---|---|---|---|
| CryptoServiceScoped | Application/ACHSobreDigital/Implementation | Sí | Implementado | Sobre firmado obligatorio + fail-close. |
| DigitalEnvelopeSignatureValidator | Application/ACHSobreDigital/Implementation | Sí | Implementado | Firma, vigencia, thumbprint, cadena opcional. |
| RsaKeyProvider | Application/Services/EncryptionService | Sí | Implementado | Resolución de certificados + integración resolver. |
| DigitalEnvelopeCertificateResolver | Persistence/ACH/Services/CertificateManagement | Sí | Implementado | Selección CM/legacy y decrypt histórico. |
| DigitalEnvelopeSignatureAuditService | Persistence/ACH/Services/CertificateManagement | Sí | Implementado | Registro `result/errorCode/flags`. |
| DigitalEnvelopeModel | Domain/Models/ACHSobreDigital | Sí | Implementado | XML custom envelope. |
| SignedData | Domain/Models/ACHSobreDigital | Sí | Implementado | XML custom signedData. |
| DigitalEnvelopeSignatureValidationOptions | Domain/Models/Configurations | Sí | Parcial | Configurable; no habilita bypass exitoso. |
| appsettings firma/sobre | Api/appsettings*.json | Sí | Parcial | Dev/UAT con chain opcional. |
| tests caracterización | tests/Cfa.ACHInterbank.Tests | Sí | Implementado | Cobertura de seguridad y regresión. |

## 5. Matriz de sobre digital
| Control | Estado actual | Evidencia | Riesgo | Próximo control |
|---|---|---|---|---|
| XML custom | Implementado | `DigitalEnvelopeModel`/`SignedData` | Interoperabilidad externa pendiente | Validación externa formal |
| RecipientInfo obligatorio | Implementado | `OpenEnvelopeAsync` + tests | Bajo | Mantener |
| CertificateInfo obligatorio | Implementado | `OpenEnvelopeAsync` | Bajo | Mantener |
| EncryptedKey obligatorio | Implementado | `OpenEnvelopeAsync` | Bajo | Mantener |
| EncryptedContent obligatorio | Implementado | `OpenEnvelopeAsync` | Bajo | Mantener |
| AES-CBC | Implementado | `CryptoServiceScoped` | Deuda algorítmica futura | Revisar contra política aprobada |
| RSA PKCS#1 v1.5 | Implementado | `CryptoServiceScoped` | Deuda algorítmica futura | Cambios solo con aprobación |
| Identifier/Timestamp | Implementado | modelo envelope | Bajo | Mantener |
| Apertura/descifrado | Implementado | `OpenEnvelopeAsync` | Medio | Hardening operativo |
| Creación/cifrado | Implementado | `CreateEnvelopeAsync` | Medio | Hardening operativo |
| fail-close | Implementado | excepciones funcionales | Bajo | Mantener |
| no retorno plano ante error | Implementado | tests fail-close | Bajo | Mantener |

No usa `SignedCms`/`EnvelopedCms` ni PKCS#7 nativo; es implementación XML custom. Cambiar formato/algoritmos requiere confirmación documental ACH/CENIT.

## 6. Matriz de firma digital
| Control | Estado actual | Evidencia | Riesgo | Próximo control |
|---|---|---|---|---|
| firma obligatoria | Implementado | validator + OpenEnvelope | Bajo | Mantener |
| SHA256withRSA | Implementado | `SignedData.SignatureAlgorithm` + validator | Medio | Política formal de algoritmos |
| hash SHA-256 | Implementado | validator | Bajo | Mantener |
| verify con llave pública | Implementado | validator | Bajo | Mantener |
| rechazo contenido alterado | Implementado | tests | Bajo | Mantener |
| rechazo firma alterada | Implementado | tests | Bajo | Mantener |
| rechazo unsigned | Implementado | tests | Bajo | Mantener |
| EnableSignatureValidation=false sin bypass | Implementado | `CryptoServiceScoped` | Bajo | Mantener |
| AllowLegacyUnsignedEnvelope sin éxito | Implementado | tests | Bajo | Mantener |
| fail-close obligatorio | Implementado | excepciones funcionales | Bajo | Mantener |
| auditoría OK/FAILED | Implementado | audit service/tests | Medio | payload canónico |

## 7. Matriz de certificados X.509
| Control | Estado actual | Evidencia | Riesgo | Próximo control |
|---|---|---|---|---|
| cert firmante | Implementado | `CertSign` | Medio | Runbook |
| cert receptor cifrado | Implementado | `CertCrypt` | Medio | Runbook |
| cert descifrado | Implementado | `CertDecrypt` | Medio | Runbook |
| cert público valida firma | Implementado | validator | Bajo | Mantener |
| cert público cifra | Implementado | `GetRSAPublicKey` | Bajo | Mantener |
| cert privado firma | Implementado | RequireRsaPrivateKey | Bajo | Mantener |
| cert privado descifra | Implementado | RequireRsaPrivateKey | Bajo | Mantener |
| búsqueda issuer/serial/thumbprint | Implementado | resolver/provider | Medio | Evidencia operativa |
| store X509 | Implementado | `RsaKeyProvider` | Parcial | Validar permisos |
| PFX/CER | Implementado | resolver/loader | Parcial | Runbook |
| proveedor de secretos retirado/SecretRef | No aplica | modelo operativo vigente | Bajo | Mantener exclusión arquitectónica |
| HasPrivateKey | Implementado | helper+resolver | Bajo | Mantener |
| GetRSAPrivateKey controlado | Implementado | helper seguro | Bajo | Mantener |

- Modelo operativo vigente: BD guarda solo metadata/inventario/auditoría/evidencia; private key/PFX/password fuera de BD.

## 8. Matriz de llave privada
| Operación | Requiere private key | Validación actual | Error funcional | Observación |
|---|---|---|---|---|
| SIGN | Sí | `RequireRsaPrivateKey` | `CERTIFICATE_PRIVATE_KEY_REQUIRED/NOT_AVAILABLE` | Obligatorio |
| DECRYPT | Sí | `RequireRsaPrivateKey` | `CERTIFICATE_PRIVATE_KEY_REQUIRED/NOT_AVAILABLE` | Obligatorio |
| VERIFY SIGNATURE | No | pública en validator | N/A | Permitido |
| ENCRYPT TO RECIPIENT | No | pública en cifrado | N/A | Permitido |
| carga PFX | Sí aplica | resolver/loader | según loader | Parcial |
| carga store | Sí aplica | provider | según provider | Parcial |
| proveedor de secretos retirado/SecretRef en carga de certificados | No aplica | modelo operativo vigente | Bajo | Mantener exclusión arquitectónica |

Errores relevantes: `CERTIFICATE_PRIVATE_KEY_REQUIRED`, `CERTIFICATE_PRIVATE_KEY_NOT_AVAILABLE`, `CERTIFICATE_NOT_AVAILABLE`.

## 9. Matriz de vigencia y cadena
| Control | Estado actual | Error funcional | Configuración | Riesgo |
|---|---|---|---|---|
| NotBefore firmante | Implementado | `SIGNER_CERTIFICATE_NOT_YET_VALID` | `FailWhenSignerCertificateExpired` | Bajo |
| NotAfter firmante | Implementado | `SIGNER_CERTIFICATE_EXPIRED` | `FailWhenSignerCertificateExpired` | Bajo |
| cert local SIGN expirado | Implementado | `CERTIFICATE_EXPIRED` | runtime helper | Bajo |
| cert local DECRYPT expirado | Implementado | `CERTIFICATE_EXPIRED` | runtime helper | Bajo |
| cert ENCRYPT expirado | Implementado | `CERTIFICATE_EXPIRED` | runtime helper | Bajo |
| ValidateSignerCertificateChain | Implementado | `SIGNER_CERTIFICATE_NOT_TRUSTED` | config option | Medio |
| self-signed dev/UAT | Implementado | aceptado si chain off | `ValidateSignerCertificateChain=false` | Controlado |
| chain inválida runtime local | Implementado | `CERTIFICATE_CHAIN_NOT_TRUSTED` | helper + config | Medio |
| trust store productivo | Parcial | N/A | externo | Alto |
| certificados cámara/entidad | Parcial | N/A | externo | Alto |

## 10. Matriz de revocación
| Control | Estado actual | Evidencia | Riesgo | Recomendación |
|---|---|---|---|---|
| `X509RevocationMode.NoCheck` | Implementado | validator/helper | Alto productivo | Definir política aprobada |
| CRL | No encontrado | N/A | Alto | Diseñar soporte con infraestructura |
| OCSP | No encontrado | N/A | Alto | Diseñar soporte con infraestructura |
| revocación online/offline | No encontrado | N/A | Alto | Política ops/riesgo |
| dependencia infraestructura externa | Parcial | docs | Medio | Plan técnico + UAT |
| comportamiento UAT | Controlado | tests/docs | Medio | Formalizar checklist |
| condición productiva | No cerrado | scorecard | Alto | Cierre comité |

No se activó CRL/OCSP sin infraestructura real. `NoCheck` queda como deuda explícita controlada.

## 11. Matriz de auditoría criptográfica
| Campo | Estado actual | Evidencia | Brecha |
|---|---|---|---|
| result | Implementado | audit service/tests | N/A |
| errorCode | Implementado | audit service/tests | Estandarización futura |
| signerThumbprint | Implementado | audit service/tests | Masking homogéneo |
| signerSerialNumber | Implementado | audit service/tests | Masking homogéneo |
| signatureAlgorithm | Implementado | audit service/tests | N/A |
| failCloseApplied | Implementado | audit service/tests | N/A |
| legacyBypassUsed | Implementado (false esperado) | tests | Monitoreo continuo |
| actor | Implementado | audit service/tests | N/A |
| timestamp | Implementado | `OccurredAtUtc` | N/A |
| hash contenido | Parcial | operaciones específicas | normalizar payload |
| correlationId | Parcial | operation id en servicios | normalizar payload |
| secretRefMasked | No aplica para certificados | modelo operativo vigente | N/A | No aplica |
| no private key | Implementado | tests | N/A |
| no PFX password | Implementado | tests | N/A |

## 12. Matriz de legacy/bypass
No hay compatibilidad productiva legacy; sobres unsigned fallan siempre. `AllowLegacyUnsignedEnvelope` y `EnableSignatureValidation=false` no habilitan éxito.

| Elemento legacy | Estado anterior | Estado actual | Evidencia | Riesgo residual |
|---|---|---|---|---|
| unsigned bypass | existía compatibilidad temporal | eliminado funcionalmente | tests/runtime | Bajo |
| AllowLegacyUnsignedEnvelope | podía influir en aceptación | no habilita éxito | tests | Bajo |
| EnableSignatureValidation=false | podía implicar bypass | no permite éxito inseguro | runtime/tests | Bajo |
| LegacyBypassUsed exitoso | posible histórico | no permitido | tests | Bajo |

## 13. Relación con flujos ACH/CENIT/NACHA
| Flujo | Usa sobre/firma/certificado | Estado actual | Riesgo | Evidencia requerida |
|---|---|---|---|---|
| outbound NACHA | Sí | Implementado | Medio | UAT externa |
| incoming NACHA | Sí | Implementado | Medio | UAT externa |
| returns | Parcial | Parcial | Medio | matriz funcional |
| ROR | Parcial | Parcial | Medio | matriz funcional |
| parser | No aplica directo | No aplica | N/A | N/A |
| naming externo | No aplica directo | Parcial | Alto | cierre normativo |
| causales | No aplica directo | Parcial | Alto | cierre normativo |
| rechazo total/parcial | No aplica directo | Parcial | Medio | UAT |
| ciclos CENIT | No aplica directo | Parcial | Alto | UAT |
| neteo/liquidez/CUD | No aplica directo | Parcial | Alto | evidencia CUD |
| contabilidad/conciliación | No aplica directo | Parcial | Alto | control operativo |

La capa cripto protege sobre/transporte de archivo; no reemplaza validación funcional NACHA ni decisiones de negocio de causales/ROR/conciliación.

## 14. Pruebas existentes
| Suite/Test | Cubre | Estado | Observación |
|---|---|---|---|
| DigitalEnvelopeCertificateCharacterizationTests | sobre/firma/cert/private key/cadena/vigencia/auditoría | Implementado | Suite principal de caracterización |
| DigitalEnvelopeSignatureFailCloseTests | fail-close, tamper, unsigned | Implementado | Rechazo obligatorio |
| private key tests | SIGN/DECRYPT sin key | Implementado | errores funcionales |
| vigencia tests | expired/not-yet-valid | Implementado | códigos funcionales |
| cadena tests | chain on/off | Implementado | dev controlado con chain off |
| unsigned tests | rechazo siempre | Implementado | sin bypass exitoso |
| happy path tests | create/open válidos | Implementado | no regresión |
| auditoría tests | FAILED/SUCCESS/campos | Implementado | no secretos |

## 15. Brechas P0/P1/P2
- **P0**
  - CRL/OCSP no implementado para productivo.
  - EKU/KeyUsage no validado.
  - política productiva de trust store/cadena no cerrada.
  - evidencia operativa de certificados productivos pendiente.
  - rotación/caducidad operacional no cerrada.
  - definición formal de algoritmo/padding según documento ACH/CENIT pendiente si aplica.
- **P1**
  - checklist UAT específico pendiente.
  - runbook de certificados pendiente.
  - evidencia permisos store/IIS/Linux pendiente.
  - alertas de expiración pendientes.
  - matriz de roles/segregación pendiente.
  - política de renovación/revocación pendiente.
- **P2**
  - HSM.
  - AD CS.
  - automatización rotación.
  - dashboard de certificados.
  - alertas.
  - No aplica proveedor de secretos retirado/SecretRef para certificados.

## 16. Recomendación
- No conviene más hardening runtime antes de cierre documental/UAT, salvo hallazgo crítico.
- Próximo paso recomendado: `docs(uat): add digital envelope certificate acceptance checklist`.
- Luego: `docs(ops): add certificate operations runbook`.
- Posterior runtime (solo con decisión e infraestructura):
  - `feat(crypto): add certificate usage policy validation`
  - `feat(crypto): add revocation policy support`

## 17. Criterios de salida de NO-GO cripto
1. Firma obligatoria validada.
2. Sobres unsigned rechazados.
3. Firma inválida rechazada.
4. Contenido alterado rechazado.
5. Private key requerida para firmar.
6. Private key requerida para descifrar.
7. Cert público permitido solo para verificar/cifrar.
8. Vigencia validada.
9. Cadena definida por política.
10. Revocación definida por política.
11. EKU/KeyUsage definido o justificado.
12. Certificados productivos inventariados.
13. Store/permisos validados.
14. Rotación definida.
15. Evidencia UAT generada.
16. Auditoría revisada.
17. No secretos en logs.
18. Runbook aprobado.
19. Firma operaciones/riesgo/compliance/tecnología.
20. Scorecard actualizado.

## 18. Decisión vigente
- Base técnica implementada: **sí**.
- GO técnico por componente implementado: **sí, condicionado**.
- GO UAT controlado: **sí, parcial**.
- NO-GO productivo: **sí**.
- La matriz no habilita producción.
- No queda legacy/bypass exitoso.
- Brechas principales: revocación, EKU/KeyUsage, evidencia operativa, rotación y runbook.

- Referencia checklist UAT de sobre/firma/certificados: `docs/uat/digital-envelope-certificate-acceptance-checklist.md` (no modifica decisión NO-GO productivo).

- Referencia runbook operativo de certificados: `docs/ops/certificate-operations-runbook.md` (no modifica decisión NO-GO productivo).
