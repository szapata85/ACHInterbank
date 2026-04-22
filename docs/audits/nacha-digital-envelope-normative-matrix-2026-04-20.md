# Matriz normativa y técnica — Anexo 21 ACH V32 (Sobre Digital NACHA-M)

**Fecha:** 2026-04-21 (UTC)  
**Fase:** Análisis normativo / matriz técnica (sin implementación).  
**Insumos obligatorios usados:**
- `docs/normativa/md/ACH-Colombia-V32.md`
- `docs/normativa/pdf/ACH-Colombia-V32.pdf` (presencia verificada; extracción textual automática limitada en entorno)
- `docs/audits/nacha-digital-envelope-current-state-2026-04-20.md`
- `docs/adr/ADR-NACHA-DigitalEnvelope-Certificates-2026-04-20.md`

---

## 1) Resumen ejecutivo

1. **Formato objetivo normativo:** La evidencia explícita del Anexo 21 describe un **Digital Envelope XML** (no CMS/PKCS#7 explícito como contenedor principal).  
2. **Estructura XML:** Se identifican nodos clave obligatorios/esperados (`identifier`, `recipientInfo`, `certificateInfo`, `encryptedKey`, `encryptedContentInfo`, etc.) y una estructura interna firmada (`signerInfo`, `contentInfo`, `encryptedDigest`).  
3. **Algoritmos:** El ejemplo normativo incluye `RSA/NONE/PKCS1Padding` para clave y `AES/CBC/PKCS5padding` para contenido; hash/firma y ZIP+Base64 también están descritos.  
4. **Punto crítico:** La norma sí describe derivación/uso de `identifier` e IV; la implementación actual usa `aes.GenerateIV()` para cifrar y otra derivación para descifrar, lo que deja **riesgo crítico de incompatibilidad/interoperabilidad**.  
5. **Firma digital:** La norma indica verificación de firma digital; descifrado que entregue contenido sin verificar firma queda **No conforme / riesgo crítico**.  
6. **Certificados:** Se confirma X.509 v3, RSA 2048, PKCS#12/.cer y consulta CRL/OCSP; falta definir enforcement operativo exacto para runtime (algunas reglas quedan en WARNING/AUDIT ONLY hasta confirmación operativa).  
7. **Extensión `.env/.ENV`:** No se encontró regla explícita de case exacto en la evidencia revisada; salida `.ENV` + aceptación inbound case-insensitive queda como decisión técnica temporal. **Requiere confirmación normativa** para hard enforcement.

---

## 2) Fuentes revisadas

| Fuente | Evidencia clave | Estado |
|---|---|---|
| `docs/normativa/md/ACH-Colombia-V32.md` | Anexo 21 Digital Envelope, estructura XML, algoritmos, ZIP/Base64, firma/verificación, requisitos de certificados | Principal |
| `docs/normativa/pdf/ACH-Colombia-V32.pdf` | Archivo presente; no se logró extracción textual robusta con tooling disponible en entorno | Parcial (validación de presencia) |
| `docs/audits/nacha-digital-envelope-current-state-2026-04-20.md` | Hallazgos previos de implementación actual | Insumo |
| `docs/adr/ADR-NACHA-DigitalEnvelope-Certificates-2026-04-20.md` | Decisiones arquitectónicas previas | Insumo |

### Evidencia normativa puntual usada
- Certificados digitales (X.509 v3, RSA 2048, PKCS#12/.cer, CRL/OCSP).  
- Referencia a anexo de mensajería encriptada para NACHA-M.  
- Definición completa de Digital Envelope (Anexo 21) y ejemplo de algoritmos/estructura.

---

## 3) Matriz de estructura XML (Anexo 21)

> Convención Estado: **Confirmado / Parcial / No encontrado / Requiere confirmación normativa**

| Nodo XML | Padre | Obligatorio | Tipo de dato | Ejemplo normativo | Regla extraída | Estado | Confianza | Impacto implementación |
|---|---|---:|---|---|---|---|---|---|
| `envelope` (root) | N/A | Sí (implícito por ejemplo) | XML element | `<!DOCTYPE envelope ...><envelope>` | El sobre se representa en XML con root envelope | Confirmado | Alta | HARD |
| `version` / `versión` | `envelope` | Sí | string/int | `1` | Valor esperado “1” | Confirmado | Alta | HARD |
| `identifier` | `envelope` | Sí | numérico/string | valor numérico largo | Se construye con serial firmante + aleatorio; se usa para IV | Confirmado | Alta | HARD |
| `timestamp` | `envelope` | Sí | datetime string | `Tue ... COT ...` | Marca de tiempo del sobre | Confirmado | Media | WARNING |
| `recipientInfo` | `envelope` | Sí | object | bloque XML | Contiene datos del receptor y cifrado de llave | Confirmado | Alta | HARD |
| `certificateInfo` (receptor) | `recipientInfo` | Sí | object | `issuer`, `serial` | Identifica certificado receptor | Confirmado | Alta | HARD |
| `issuer` (receptor) | `certificateInfo` | Sí | DN string | `CN=..., O=...` | DN según RFC4519 | Confirmado | Alta | HARD |
| `serial` (receptor) | `certificateInfo` | Sí | string/num | serial numérico | Serial del certificado | Confirmado | Alta | HARD |
| `keyEncryptionAlgorithm` | `recipientInfo` | Sí | string | `RSA/NONE/PKCS1Padding` | Algoritmo de cifrado de llave simétrica | Confirmado | Alta | HARD |
| `encryptedKey` | `recipientInfo` | Sí | Base64 | blob base64 | Llave simétrica cifrada para receptor | Confirmado | Alta | HARD |
| `encryptedContentInfo` | `envelope` | Sí | object | bloque XML | Metadata y payload cifrado | Confirmado | Alta | HARD |
| `contentType` | `encryptedContentInfo` | Sí | string | `signedData` | Contenido cifrado corresponde a mensaje firmado | Confirmado | Alta | HARD |
| `contentEncryptionAlgorithm` | `encryptedContentInfo` | Sí | string | `AES/CBC/PKCS5padding` | Algoritmo de cifrado del contenido firmado | Confirmado | Alta | HARD |
| `encryptedContent` | `encryptedContentInfo` | Sí | Base64 | blob base64 | Payload firmado y cifrado | Confirmado | Alta | HARD |
| `SignedData` (mensaje interno) | contenido descifrado | Sí | XML element | estructura firmada | El contenido descifrado es un XML firmado | Confirmado | Alta | HARD |
| `signerInfo` | `SignedData` | Sí | object | bloque XML | Identifica firmante y algoritmo | Confirmado | Alta | HARD |
| `signatureAlgorithm` | `signerInfo` | Sí | string | descrito por anexo | Algoritmo de firma del mensaje | Confirmado | Alta | HARD |
| `certificateInfo` (firmante) | `signerInfo` | Sí | object | `issuer`,`serial` | Referencia al cert de firma | Confirmado | Alta | HARD |
| `certificate` | `signerInfo` | Sí | Base64 | cert en ASCII base64 | Certificado para verificar firma | Confirmado | Alta | HARD |
| `contentInfo` | `SignedData` | Sí | Base64 | ZIP+Base64 | Contenido original comprimido y codificado | Confirmado | Alta | HARD |
| `encryptedDigest` | `SignedData` | Sí | Base64 | firma base64 | Firma digital del contenido en claro | Confirmado | Alta | HARD |

---

## 4) Matriz de algoritmos

| Propósito | Algoritmo normativo (Anexo 21) | Equivalente .NET | Estado | Confianza | Riesgo | Recomendación |
|---|---|---|---|---|---|---|
| Hash para firma | Hash criptográfico según `signatureAlgorithm` (ejemplo implementación usa SHA-256) | `SHA256` | Parcial | Media | Medio | Confirmar vector oficial de firma exacta por cámara |
| Firma RSA | Firma con llave privada RSA | `RSA.SignHash(..., RSASignaturePadding.Pkcs1)` | Confirmado | Alta | Alto si no se verifica | HARD + verificación obligatoria |
| Verificación firma | Se debe usar certificado para verificar firma | `RSA.VerifyHash(...)` | Confirmado | Alta | Crítico si omitido | HARD fail-close |
| Cifrado contenido | AES-256 | `Aes.KeySize=256` | Confirmado | Alta | Alto | HARD |
| Modo contenido | CBC | `CipherMode.CBC` | Confirmado | Alta | Alto | HARD |
| Padding contenido | `PKCS5padding` | `PaddingMode.PKCS7` (equivalencia para block 16 bytes) | Parcial | Media | Medio | Implementar + validar byte-a-byte interoperabilidad |
| Cifrado de llave | `RSA/NONE/PKCS1Padding` | `RSAEncryptionPadding.Pkcs1` | Parcial | Alta | Alto | HARD + prueba interoperabilidad |
| Compresión | ZIP en `contentInfo` | utilitario ZIP | Confirmado | Alta | Alto si se altera orden | HARD |
| Codificación | Base64 (`certificate`, `contentInfo`, `encryptedDigest`, payload cifrado) | `Convert.ToBase64String` / parse Base64 | Confirmado | Alta | Medio | HARD |

**Nota técnica requerida:**
- Mapeo `PKCS5padding` ↔ `PKCS7` en .NET para AES es técnicamente plausible, pero debe validarse contra vector oficial ACH/CENIT para interoperabilidad exacta.

---

## 5) Derivación `identifier` / IV (crítico)

### Evidencia normativa extraída
- `identifier` se genera concatenando serial del certificado del firmante + número aleatorio.
- El `identifier` se describe como IV usado en cifrado AES.
- El anexo describe procedimiento para calcular IV ejecutando 1 bloque AES sobre `identifier` con la llave de contenido (derivada de `encryptedKey`).

### Respuestas solicitadas
1. **¿La norma define cómo derivar IV desde `identifier`?**  
   **Sí, de forma explícita en Anexo 21.** → **Confirmado**.
2. **¿Permite IV aleatorio puro (`aes.GenerateIV()`)?**  
   **No explícitamente en la evidencia revisada.** → **Requiere confirmación normativa** (tendencia: no).
3. **¿`aes.GenerateIV()` actual es compatible?**  
   **No probado; alto riesgo de no compatibilidad** con derivación descrita. → **Requiere vector oficial**.
4. **¿`OpenEnvelopeAsync` derivando IV desde `identifier` es compatible?**  
   **Parcial/no demostrado**; la derivación actual (SHA-256 truncado) no coincide explícitamente con texto normativo del bloque AES.
5. **¿Hay inconsistencia cifrado vs descifrado actual?**  
   **Sí.** Cifrado usa IV generado aleatoriamente; descifrado deriva IV desde `identifier` por otro método.
6. **¿Se puede implementar sin vector oficial?**  
   Se puede implementar tentativa, pero **no debe considerarse hardening final**.
7. **¿Debe ser HARD REQUIREMENT?**  
   **Sí** (interoperabilidad crítica).

### Clasificación
- Derivación normada de IV: **Confirmado**.
- Compatibilidad implementación actual: **No conforme / Requiere vector oficial**.

---

## 6) Firma digital y verificación obligatoria

### Qué dice la norma (resumen)
- Se firma la información protegida (mensaje en claro) con llave privada del firmante.
- `encryptedDigest` contiene firma digital del `contentInfo`.
- Se incluye certificado del firmante para verificar la firma.
- El receptor debe usar dicho certificado para verificar firma.

### Conclusión de conformidad
- Descifrado que no aplique verificación obligatoria o ignore su resultado = **No conforme**.
- Estado actual (verificación comentada en `OpenEnvelopeAsync`) = **Riesgo crítico**.

---

## 7) ZIP / Base64

### Determinación normativa
1. **Qué se comprime:** `contentInfo` (contenido original protegido).  
2. **Orden lógico:** firma de contenido + empaque definido por estructura firmada, con `contentInfo` en ZIP+Base64 antes de encapsular/cifrar.  
3. **Qué va en Base64:** `certificate`, `contentInfo`, `encryptedDigest`, `encryptedKey`, `encryptedContent`.  
4. **ZIP/Base64 obligatorios:** evidenciados en anexo para estructura de mensaje firmado. → **Confirmado**.

### Comparación con implementación actual
- `CreateEnvelopeAsync` hace ZIP+Base64 de contenido y firma hash; comportamiento general es **parcialmente alineado**, pendiente validación byte-a-byte (serialización/orden exacto).

---

## 8) Requisitos de certificados

| Requisito | Evidencia normativa | Clasificación de implementación |
|---|---|---|
| X.509 v3 | Se exige estándar ITU X.509 v3 | HARD REQUIREMENT |
| RSA 2048 | Se exige longitud RSA 2048 | HARD REQUIREMENT |
| PKCS#12 y `.cer` | Compatibilidad exigida | HARD REQUIREMENT |
| CRL/OCSP | Emisor debe ofrecer consulta CRL/OCSP | WARNING / AUDIT ONLY (hasta definir enforcement runtime por canal) |
| Vigencia | Se define fecha inicio/fin con límites | HARD REQUIREMENT |
| Cert receptor (llave pública) para cifrar | Anexo 21 lo exige | HARD REQUIREMENT |
| Llave privada firmante | Anexo 21 lo exige | HARD REQUIREMENT |
| Certificado en mensaje firmado | Anexo 21 lo exige | HARD REQUIREMENT |
| Password PFX en texto plano | Prohibición de seguridad interna del programa | NO PERMITIDO (HARD SECURITY) |

---

## 9) Extensión `.env` / `.ENV`

### Resultado de evidencia
- No se encontró en las líneas normativas revisadas una regla explícita de case exacto (`.env` vs `.ENV`) para el archivo encriptado de NACHA-M.

### Conclusiones técnicas de fase 1
1. **Outbound default recomendado:** `.ENV` (compatibilidad actual de plataforma).
2. **Inbound recomendado:** aceptar `.ENV` y `.env` (case-insensitive).
3. **Bloqueo por case:** no recomendable sin confirmación formal de cámara.
4. **Configurabilidad:** sí, por cámara/ambiente.

### Estado
- Regla de case exacto: **Requiere confirmación normativa**.

---

## 10) Matriz de decisión implementable (fase 1)

| Regla | Evidencia | Estado | Confianza | Implementar fase 1 como | Riesgo si se implementa mal |
|---|---|---|---|---|---|
| Formato XML Anexo 21 | Anexo 21 Digital Envelope | Confirmado | Alta | HARD REQUIREMENT | Rechazo interoperabilidad |
| Tags obligatorios de sobre | Estructura sobre digital XML | Confirmado | Alta | HARD REQUIREMENT | Parseo inválido/rechazo |
| Firma RSA | Texto de firma + mensaje firmado | Confirmado | Alta | HARD REQUIREMENT | Integridad comprometida |
| Verificación firma obligatoria | Uso de certificado para verificar firma | Confirmado | Alta | HARD REQUIREMENT | Aceptación de payload alterado |
| AES-256 CBC | Texto y ejemplo | Confirmado | Alta | HARD REQUIREMENT | Incompatibilidad cripto |
| RSA PKCS1 para `encryptedKey` | `RSA/NONE/PKCS1Padding` | Confirmado | Alta | HARD REQUIREMENT | Imposibilidad descifrado |
| ZIP en `contentInfo` | Texto explícito ZIP+Base64 | Confirmado | Alta | HARD REQUIREMENT | No lectura del payload |
| Base64 en campos definidos | Texto explícito | Confirmado | Alta | HARD REQUIREMENT | Errores de serialización |
| `identifier`/IV derivado por regla Anexo 21 | Texto específico de IV | Confirmado | Alta | HARD REQUIREMENT + vector oficial | Incompatibilidad crítica |
| Extensión `.ENV/.env` case exacto | No explícito hallado | Requiere confirmación normativa | Media | WARNING configurable | Rechazo operativo por naming |
| X.509 v3 | Sección certificados | Confirmado | Alta | HARD REQUIREMENT | Incumplimiento normativo |
| RSA 2048 | Sección certificados | Confirmado | Alta | HARD REQUIREMENT | Incumplimiento normativo |
| PKCS#12 / `.cer` | Sección certificados | Confirmado | Alta | HARD REQUIREMENT | Incompatibilidad de carga |
| CRL/OCSP | Sección certificados | Confirmado (existencia), enforcement runtime parcial | Media | WARNING / AUDIT ONLY | Control incompleto de revocación |
| Password PFX en claro | Política seguridad interna + riesgo | Confirmado (prohibición interna) | Alta | HARD REQUIREMENT (prohibir) | Compromiso de llaves |
| Auditoría criptográfica completa | Requisito programa/ADR | Parcial normativo | Media | HARD REQUIREMENT interno | No trazabilidad regulatoria |

---

## 11) Comparación contra implementación actual

| Requisito normativo | Implementación actual | Estado | Severidad | Acción recomendada |
|---|---|---|---|---|
| XML Anexo 21 | `CryptoServiceScoped` construye XML con nodos principales | Parcial | Alta | Encapsular y validar contra vector oficial |
| `identifier`/IV normativo | Cifra con `aes.GenerateIV()`; descifra derivando SHA-256(identifier) truncado | No conforme / no probado | Crítico | Reimplementar derivación según anexo + vector oficial |
| Firma y verificación obligatoria | Firma existe; verificación en descifrado comentada | No conforme | Crítico | Hacer verify fail-close obligatorio |
| AES-256 CBC + padding | Usa AES-256 CBC y PKCS7 | Parcial | Alta | Validar equivalencia PKCS5/PKCS7 byte-a-byte |
| Cifrado llave RSA PKCS1 | Usa `RSAEncryptionPadding.Pkcs1` y marca algoritmo | Conforme parcial | Media | Confirmar interoperabilidad |
| ZIP/Base64 | Implementado en `contentInfo` y payload | Parcial | Media | Validar orden/serialización exacta |
| Selección de certificados por contexto | `RsaKeyProvider` usa tipo simple (CertCrypt/CertSign), sin contexto rico | Parcial | Alta | Modelo versionado por cámara/ambiente/propósito |
| Persistencia segura de secretos | `DigitalEnvelopeCertificate.Password` existe y se usa | No conforme | Crítico | Eliminar password en claro; usar SecretRef |
| Historial/rotación certificados | Repositorio `Upsert` destructivo por tipo | No conforme | Alto | Versionado + estado + rotación |
| Extensión archivo encriptado | `ExportEncrypted` retorna `.ENV` | Parcial | Medio | Mantener default + inbound case-insensitive configurable |
| Endpoints certificados | Cargan/listan metadata, pero sin gobierno completo | Parcial | Alto | Separar admin lifecycle, políticas y auditoría |

---

## 12) Recomendación de fases

### Fase 1 (bloqueante)
1. Cerrar confirmación normativa/interoperabilidad de derivación `identifier/IV` con vector oficial.
2. Forzar verificación obligatoria de firma en descifrado.
3. Remover persistencia de password en claro y migrar a gestor de secretos.
4. Implementar gobierno de certificados versionado por contexto.

### Fase 2
1. Endurecer auditoría criptográfica integral.
2. Ejecutar pruebas unitarias/integración/E2E/interop.
3. Activar rollout gradual por cámara/ambiente.

---

## 13) Preguntas abiertas

1. ¿Case exacto exigido por ACH/CENIT para extensión del archivo encriptado (`.env` o `.ENV`)?
2. ¿Existe vector oficial completo (input/output) para validar `identifier`/IV y serialización XML?
3. ¿El control CRL/OCSP debe aplicarse en runtime para cada operación o en validación previa de onboarding?
4. ¿Se exige canonicalización XML específica (encoding/orden exacto de nodos/saltos de línea)?
5. ¿Existen variantes por cámara/servicio que modifiquen algoritmos o estructura de sobre?

---

## 14) Evidencia utilizada (líneas clave)

- Certificados (X.509v3, RSA 2048, PKCS#12/.cer, CRL/OCSP): sección 5.1.5.  
- Referencia a Anexo 21 de mensajería encriptada NACHA-M.  
- Anexo 21: estructura Digital Envelope, `identifier`/IV, `SignedData`, algoritmos ejemplo, ZIP/Base64, verificación de firma.
