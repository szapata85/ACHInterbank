# Solicitud de Vector Oficial de Interoperabilidad — Sobre Digital ACH/CENIT (Anexo 21)

**Fecha:** 2026-04-21 (UTC)  
**Proyecto:** ACHInterbank  
**Estado:** Preparación de certificación (sin cambios criptográficos productivos)  
**Alcance:** Solicitar/recibir/custodiar/cargar/validar vector oficial para interoperabilidad `.ENV/.env` en backend.

**Nota operativa:** Este documento actúa también como runbook operativo interno.

---

## 1) Objetivo de la solicitud

Solicitar a ACH/CENIT (o cámara aplicable) un **vector oficial** para validar interoperabilidad técnica del sobre digital Anexo 21, con evidencia objetiva de:

- estructura XML esperada,
- algoritmos aplicados,
- validación de firma,
- descifrado esperado,
- criterios de aceptación para operación backend y operación manual controlada.

> Esta fase **no** cambia `identifier`, IV, derivación, XML, AES/RSA/padding, `SignedData`, `CryptoServiceScoped`, `OpenEnvelopeAsync` ni `RsaKeyProvider`.

---

## 2) Contexto del proyecto

Estado confirmado del programa:

- Certificate Management fase 1 implementada.
- Harness sintético de interoperabilidad disponible.
- Validación fail-close de firma habilitada en backend.
- Sin vector oficial ACH/CENIT, no procede hardening final de `identifier/IV`.
- PostgreSQL integration workflow se mantiene manual-only (`workflow_dispatch`).

---

## 3) Alcance de interoperabilidad solicitado

Se solicita certificación sobre estos flujos:

1. **Descifrado oficial:** `.ENV` oficial → NACHA-M plano esperado.
2. **Validación de firma oficial:** firma válida/ inválida y comportamiento esperado.
3. **Comparación de estructura:** nodos XML, algoritmos declarados y metadatos certificados.
4. **Verificación de compatibilidad de `identifier/IV`:** criterio exacto de aceptación para cerrar brecha.
5. **Criterios operativos para futura pantalla SPA** (backend-only crypto).

---

## 4) Insumos exactos requeridos a ACH/CENIT

### 4.1 Archivos obligatorios

1. `official-envelope.env`  
2. `official-plain-nacha.txt`  
3. `official-public-signing-cert.cer`  
4. `official-public-encryption-cert.cer` *(si aplica)*  
5. `official-chain-ca.cer` *(si aplica)*  
6. `official-crl-or-ocsp-info.txt` *(si aplica)*  
7. `official-metadata.json`  
8. `official-expected-report.json` *(si la cámara lo provee)*

### 4.2 Archivo opcional de alto control

9. `official-receiver-private-test-cert.pfx` *(solo si la cámara autoriza y entrega material privado de prueba)*

**Regla:** este PFX **no se sube al repositorio**. Se custodia fuera de Git y se inyecta por secreto seguro.

### 4.3 Criterios operativos solicitados a la cámara

10. Criterios de certificación para:
- cifrado de NACHA-M plano,
- descifrado de `.ENV`,
- validación de firma,
- manejo de errores,
- formato de respuesta esperado para operación manual/backend.

---

## 5) Formato esperado de entrega

- Canal seguro acordado (SFTP seguro, repositorio documental cifrado o canal institucional equivalente).
- Hash SHA-256 de cada archivo enviado.
- Acta/nota técnica con:
  - versión de estándar,
  - ambiente (Certification/Production),
  - fecha de generación,
  - vigencia del vector,
  - contactos técnicos.

---

## 6) Estructura esperada de `official-metadata.json`

```json
{
  "provider": "ACH Colombia",
  "standard": "ACH Colombia V32",
  "annex": "Anexo 21",
  "environment": "Certification",
  "generatedAt": "2026-04-21T00:00:00Z",
  "fileExtension": ".ENV",
  "contentType": "NACHA-M",
  "expectedAlgorithms": {
    "keyEncryption": "RSA/NONE/PKCS1Padding",
    "contentEncryption": "AES/CBC/PKCS5padding",
    "signature": "SHA256withRSA",
    "compression": "ZIP",
    "encoding": "Base64"
  },
  "identifier": {
    "value": "",
    "encoding": "UTF-8",
    "ivDerivation": "",
    "ivExpectedHex": "",
    "requiresExactMatch": true
  },
  "certificates": {
    "signerThumbprint": "",
    "receiverThumbprint": "",
    "chainValidationRequired": false,
    "crlRequired": false,
    "ocspRequired": false
  },
  "expectedValidation": {
    "plainContentSha256": "",
    "envelopeSha256": "",
    "signatureValid": true,
    "roundtripExpected": true
  },
  "operationContext": {
    "supportsManualEncryptScreen": true,
    "supportsManualDecryptScreen": true,
    "plainContentMayBeReturnedToAuthorizedUser": true,
    "plainContentMustNotBeReturnedWhenSignatureFails": true
  },
  "notes": ""
}
```

---

## 7) Criterios de aceptación (pass/fail)

### 7.1 Pass mínimo

1. El harness carga vector oficial completo.
2. Estructura XML requerida válida.
3. Algoritmos declarados coinciden con metadata oficial.
4. Firma verificada como válida según certificado oficial de firma.
5. Contenido plano resultante coincide con `official-plain-nacha.txt` y/o hash oficial.
6. Reglas `identifier/IV` quedan explícitas y evaluables.
7. Resultado documentado con evidencias reproducibles.

### 7.2 Fail crítico

- Firma inválida con vector declarado válido.
- Descifrado inconsistente contra plano esperado.
- Divergencia algorítmica no autorizada.
- Falta de información clave para `identifier/IV`.

---

## 8) Plan de custodia segura

1. **Repositorio Git:** solo placeholders/fixtures sintéticos.
2. **Material sensible (PFX/secretos):** almacenado en gestor de secretos corporativo.
3. **Control de acceso:** mínimo privilegio + trazabilidad de descarga/uso.
4. **Caducidad:** rotación y fecha de expiración del material de prueba.
5. **Auditoría:** registrar uso del vector (quién/cuándo/para qué) sin exponer secretos.

---

## 9) Procedimiento de carga al harness

Ruta de trabajo en repo:

`tests/Cfa.ACHInterbank.Tests/Fixtures/DigitalEnvelope/OfficialVectors/`

Pasos:

1. Copiar archivos oficiales no sensibles (`.env/.txt/.cer/.json`) a `OfficialVectors/`.
2. Verificar hashes SHA-256 entregados por cámara.
3. Si existe PFX privado de prueba, **no copiar al repo**; referenciarlo por secreto externo.
4. Ejecutar pruebas objetivo:
   - `FullyQualifiedName~Interoperability|FullyQualifiedName~DigitalEnvelope|FullyQualifiedName~Signature|FullyQualifiedName~OpenEnvelope`
5. Ejecutar no regresión NACHA:
   - `FullyQualifiedName~Nacha|FullyQualifiedName~Mapping|FullyQualifiedName~BatchNumber`
6. Generar reporte de brechas y decisión de continuidad.

---

## 10) Matriz de validación del vector oficial

| Dimensión | Evidencia | Criterio | Resultado esperado |
|---|---|---|---|
| XML envelope | `official-envelope.env` | nodos requeridos | PASS |
| Algoritmos | metadata + envelope | match exacto | PASS |
| Firma | cert firmante + signedData | validación OK | PASS |
| Descifrado | plain esperado/hash | match exacto | PASS |
| identifier/IV | metadata + cálculo | match exacto o regla aprobada | PASS |
| Certificados | thumbprints/cadena | consistencia | PASS |
| Errores sanitizados | pruebas negativas | no exponer secretos | PASS |

---

## 11) Plan de ejecución y reporte

1. Recepción formal del vector + acta.
2. Verificación de integridad/hash.
3. Ejecución de harness y filtros definidos.
4. Consolidación de hallazgos:
   - conformidades,
   - brechas,
   - riesgos,
   - decisiones.
5. Acta interna de cierre de ronda de interoperabilidad.
6. Go/No-Go para hardening de `identifier/IV`.

---

## 12) Criterio de decisión para hardening `identifier/IV`

Se habilita hardening solo si:

- existe vector oficial completo y aprobado,
- existe regla formal verificable de `identifier/IV`,
- pruebas de interoperabilidad son 100% reproducibles,
- no hay regresión funcional/security.

Si cualquiera falla: **no hardening**.

---

## 13) Preguntas técnicas a la cámara

1. ¿La derivación de IV desde `identifier` debe coincidir byte-a-byte con una fórmula formal específica?
2. ¿Existe restricción obligatoria de extensión `.env` vs `.ENV`?
3. ¿Requisitos obligatorios de CRL/OCSP en certificación y producción?
4. ¿Se admite variación de serialización XML (espacios/encoding) o se exige canonicalización específica?
5. ¿Cuál es el criterio de aceptación oficial para firma en escenarios de error/parcial?

---

## 14) Responsabilidades

### CFA / ACHInterbank
- Custodia segura de insumos.
- Ejecución técnica del harness.
- Reporte transparente de brechas.
- No exponer secretos ni contenido sensible en logs/documentación pública.

### ACH/CENIT / Cámara
- Entrega de vector oficial y metadata técnica completa.
- Confirmación de criterios de certificación.
- Resolución de ambigüedades normativas (identifier/IV, extensión, validaciones).

---

## 15) Plantilla de correo/carta formal

**Asunto:** Solicitud de vector oficial de interoperabilidad — Sobre Digital Anexo 21 ACH V32

Estimados,

En el marco de la certificación técnica del servicio ACHInterbank, solicitamos formalmente la entrega de un **vector oficial de interoperabilidad** para el sobre digital de archivos NACHA-M conforme al **Anexo 21 (ACH V32)**.

Agradecemos remitir, por canal seguro institucional, los siguientes insumos:

- `official-envelope.env`
- `official-plain-nacha.txt`
- `official-public-signing-cert.cer`
- `official-public-encryption-cert.cer` (si aplica)
- `official-chain-ca.cer` (si aplica)
- `official-crl-or-ocsp-info.txt` (si aplica)
- `official-metadata.json`
- `official-expected-report.json` (si aplica)
- `official-receiver-private-test-cert.pfx` (solo si está autorizado para pruebas, con lineamientos de custodia)

Solicitamos incluir hash SHA-256 de cada archivo, fecha/hora de generación, ambiente aplicable y criterios de aceptación oficial.

El objetivo de esta solicitud es validar interoperabilidad backend, habilitar operación manual controlada y preparar el backend para una futura pantalla SPA (sin criptografía en frontend), manteniendo trazabilidad y cumplimiento.

Quedamos atentos a sus comentarios técnicos y confirmación de entrega.

Cordialmente,  
Equipo ACHInterbank (CFA)

---

## 16) Instrucciones internas para equipo ACHInterbank

1. No subir artefactos sensibles al repo.
2. Mantener `OfficialVectors/` con archivos no sensibles y controlados.
3. Registrar evidencia de ejecución (comandos, salidas, hashes, fecha UTC).
4. Escalar inmediatamente cualquier ambigüedad normativa crítica.
5. No modificar criptografía productiva hasta aprobación formal de certificación.

---

## 17) Consideraciones para futura pantalla Angular (sin implementarla)

La pantalla podrá:

- cargar NACHA-M plano para solicitar cifrado,
- cargar `.ENV/.env` para solicitar descifrado,
- descargar resultado,
- consultar estado y auditoría sanitizada.

La pantalla **no** debe:

- cifrar/descifrar localmente,
- manejar llaves privadas/passwords,
- calcular `identifier/IV`,
- validar firma como fuente de verdad,
- exponer contenido plano cuando firma falle.

---

## 18) Criterios de seguridad backend para atender esa pantalla

1. Criptografía exclusivamente backend.
2. Autorización fuerte por operación (encrypt/decrypt).
3. Validación fail-close de firma en descifrado.
4. Entrega de plano solo a usuarios autorizados y solo cuando validación sea exitosa.
5. Logs sanitizados (sin secretos/PFX/passwords).
6. Auditoría con identificadores trazables y hashes de evidencia.

---

## 19) CI / GitHub Actions / PostgreSQL harness (obligatorio)

- `postgres-integration-tests.yml` debe permanecer **manual-only**.
- Trigger único permitido: `workflow_dispatch`.
- Guard de job: `if: github.event_name == 'workflow_dispatch'`.
- No habilitar `push`, `pull_request`, `pull_request_target`, `schedule`, `workflow_run`.
- Scripts locales se conservan:
  - `scripts/test/run-postgres-integration-tests.sh`
  - `scripts/test/run-postgres-integration-tests.ps1`
- Se conserva `docker-compose.test.yml`.

---

## 20) Resultado esperado de esta fase

- Paquete documental listo para solicitud formal de vector oficial.
- Proceso de custodia y ejecución definido.
- Criterios de aceptación claros.
- Preparación backend/operativa para futura SPA sin comprometer seguridad.
