> Nota G3.5.2: las referencias a proveedores de secretos retirados son historicas y obsoletas desde el cleanup `ebf7a8a5`; no describen el stack vigente.

# ADR — NACHA Digital Envelope & Certificate Governance

- **ID:** ADR-NACHA-DigitalEnvelope-Certificates-2026-04-20
- **Fecha:** 2026-04-21 (UTC)
- **Estado:** Propuesto (para revisión de arquitectura / seguridad / cumplimiento)
- **Tipo:** Arquitectura + Seguridad + Cumplimiento normativo
- **Alcance:** Cifrado/descifrado sobre digital NACHA-M (`.ENV` / `.env`), verificación de firma, gobierno de certificados PFX/CER/CRT, manejo de secretos y auditoría criptográfica.
- **Fuentes normativas obligatorias:** `docs/normativa/md/*`, `docs/normativa/pdf/*`.
- **Insumo previo obligatorio:** `docs/audits/nacha-digital-envelope-current-state-2026-04-20.md`.

---

## 1. Contexto

El repositorio ACHInterbank ya contiene una implementación funcional de exportación NACHA con sobre digital y generación de archivo con sufijo `.ENV`, más endpoints para cifrado/descifrado y administración básica de certificados.

La auditoría previa identificó brechas críticas en interoperabilidad criptográfica (derivación `identifier/IV`), verificación obligatoria de firma en descifrado y seguridad de secretos (password de PFX persistida en claro), además de brechas de gobierno de certificados y de auditoría criptográfica.

## 2. Problema

Se requiere una decisión de arquitectura para definir:

1. Formato objetivo de sobre digital y límites de compatibilidad normativa.
2. Flujo normativamente alineado de cifrado/descifrado NACHA-M.
3. Gobierno robusto de certificados por cámara/ambiente/propósito, con historial y rotación.
4. Estrategia de secretos sin contraseñas en texto plano.
5. Auditoría criptográfica trazable y estrategia de pruebas por fases.

## 3. Estado actual (resumen técnico)

### 3.1 Cifrado/Exportación
- `NachaExportController.ExportEncrypted` genera NACHA, evalúa política de cifrado y retorna archivo `.ENV` cuando corresponde.
- `CryptoServiceScoped.CreateEnvelopeAsync` implementa firma + compresión ZIP + Base64 + cifrado AES + cifrado de llave RSA + XML sobre digital.

### 3.2 Descifrado
- `CryptoServiceScoped.OpenEnvelopeAsync` parsea XML, descifra llave y contenido, descomprime, calcula hash.
- La verificación de firma está comentada/no exigida como condición de éxito.

### 3.3 Certificados
- Existe `DigitalEnvelopeCertificate` y API de carga/listado/eliminación.
- Se persiste `Password` en entidad.
- Repositorio con `Upsert` destructivo por tipo (sin historial real de versiones/estados).

### 3.4 Auditoría y pruebas
- Existe auditoría de exportación de archivo (`IsEncrypted`, conteos, nombre), no auditoría criptográfica profunda.
- Existen pruebas de controlador para flujo export/cifrado básico y naming; no hay pruebas cripto integrales ni roundtrip real `.ENV`.

## 4. Evidencia normativa ACH V32

### 4.1 Evidencia explícita (Confirmado)
1. ACH V32 remite a Anexo 21 para mensajería encriptada de archivos NACHA-M.
2. Anexo 21 define **Digital Envelope** XML con firma y cifrado híbrido:
   - firma con llave privada RSA,
   - cifrado de contenido con AES-256,
   - cifrado de llave con certificado X.509 receptor usando RSA.
3. Define tags/estructura del sobre (`recipientInfo`, `certificateInfo`, `encryptedKey`, `contentEncryptionAlgorithm`, `encryptedContent`) y mensaje firmado (`SignedData`).
4. Define compresión ZIP + Base64 para `contentInfo` del mensaje firmado.
5. Incluye ejemplo algorítmico: `RSA/NONE/PKCS1Padding` y `AES/CBC/PKCS5padding`.
6. Define lineamientos de certificados (X.509 v3, RSA 2048, PKCS#12 + .cer, CRL/OCSP, etc.).

### 4.2 Evidencia no explícita (Requiere confirmación normativa)
1. No se evidencia de forma explícita en el material revisado la obligatoriedad exacta de extensión `.env` minúscula vs `.ENV` mayúscula.
2. No se evidencia que el formato objetivo sea CMS/PKCS#7 estándar; el anexo describe estructura XML Digital Envelope propia del servicio.
3. No se evidencia en ACH V32 una regla operacional de implementación de historial/rotación en BD del participante (es una decisión de diseño interno de cumplimiento y control).

## 5. Decisión arquitectónica principal

### 5.1 Decisión
**Se adopta como formato objetivo primario el Digital Envelope XML descrito en ACH V32 Anexo 21, encapsulado detrás de puertos de Application e implementación en Infrastructure, con validación de interoperabilidad obligatoria antes de hardening productivo.**

### 5.2 Razonamiento
- La normativa explícita revisada describe Digital Envelope XML específico con campos/algoritmos esperados.
- No hay evidencia suficiente para reemplazar por CMS/PKCS#7/EnvelopedCms sin confirmación normativa adicional.
- Por riesgo operativo, se conserva la base actual, pero se rediseña en componentes verificables y gobernados.

### 5.3 Estado de decisión por tema
- **Formato XML Digital Envelope ACH:** Confirmado.
- **Uso de CMS/PKCS#7 como formato principal:** Requiere confirmación normativa.
- **Reemplazo total inmediato de implementación actual:** Parcial (solo después de pruebas de interoperabilidad y hardening).
- **Encapsulación por adapter/strategy:** Confirmado (decisión de arquitectura interna).

## 6. Formato de sobre digital recomendado

Se define perfil **`AchV32DigitalEnvelopeXmlProfile`** con los siguientes elementos:

1. **Sobre externo XML** con:
   - `version`,
   - `identifier`,
   - `timestamp`,
   - `recipientInfo/certificateInfo` (`issuer`, `serial`),
   - `keyEncryptionAlgorithm`,
   - `encryptedKey`,
   - `encryptedContentInfo` (`contentType`, `contentEncryptionAlgorithm`, `encryptedContent`).
2. **Contenido interno firmado** (payload firmado) con:
   - `signerInfo` (`signatureAlgorithm`, `certificateInfo`, `certificate`),
   - `contentInfo` (ZIP + Base64),
   - `encryptedDigest` (firma).
3. **Algoritmos objetivo iniciales (según evidencia):**
   - `keyEncryptionAlgorithm`: RSA PKCS#1 v1.5 (representado como `RSA/NONE/PKCS1Padding`),
   - `contentEncryptionAlgorithm`: AES/CBC/PKCS5padding (mapeo operativo en .NET con PKCS7).

> Nota de cumplimiento: la equivalencia exacta de padding/serialización debe validarse por vector oficial de interoperabilidad.

## 7. Flujo de cifrado propuesto (outbound)

1. Generar NACHA-M plano sin alteración funcional del contenido.
2. Calcular hash del plano para auditoría.
3. Seleccionar certificado privado activo de firma de entidad por contexto (cámara/ambiente/propósito).
4. Firmar contenido según perfil ACH V32.
5. Comprimir ZIP contenido firmado si aplica por Anexo 21 (actual: sí en `contentInfo`).
6. Codificar Base64 según estructura esperada.
7. Generar llave simétrica AES.
8. Cifrar contenido firmado.
9. Cifrar llave AES con certificado público activo de receptor ACH/cámara.
10. Construir XML Digital Envelope con `certificateInfo`, `encryptedKey`, `encryptedContent`.
11. Emitir archivo con extensión configurable de salida (`.ENV` default; `.env` opcional por parametrización).
12. Registrar auditoría criptográfica completa (sin contenido sensible).
13. Entregar archivo final.

### Errores/validaciones mínimas de cifrado
- Certificado de firma inexistente/inactivo/expirado/revocado → error bloqueante auditado.
- Certificado público receptor inexistente/expirado → error bloqueante auditado.
- Algoritmo no permitido por perfil → error bloqueante auditado.
- Falla de construcción XML/tag obligatorio → error bloqueante auditado.

## 8. Flujo de descifrado propuesto (inbound)

1. Recibir `.ENV` o `.env`.
2. Parsear XML y validar esquema lógico mínimo.
3. Resolver contexto cámara/ambiente/propósito para seleccionar PFX privado activo de descifrado.
4. Descifrar `encryptedKey` con privada.
5. Derivar IV según regla normativa confirmada para `identifier`.
6. Descifrar `encryptedContent`.
7. Parsear mensaje firmado interno.
8. Descomprimir ZIP y decodificar Base64 según perfil.
9. **Validar firma obligatoriamente** (fail-close): si falla, no se entrega contenido válido.
10. Validar certificado firmante según política (vigencia, cadena/trust, revocación según capacidades).
11. Entregar NACHA plano a pipeline de parser.
12. Auditar operación y resultado.

### Errores/validaciones mínimas de descifrado
- Estructura XML inválida/incompleta → error bloqueante auditado.
- Password inválida o private key inaccesible → error bloqueante auditado.
- Certificado inactivo/expirado/revocado → error bloqueante auditado.
- Firma inválida → error bloqueante auditado.

## 9. Gobierno de certificados propuesto (modelo conceptual)

### 9.1 Entidades conceptuales
1. `DigitalCertificate` (identidad lógica)
2. `DigitalCertificateVersion` (material/versionamiento)
3. `CertificatePurpose` (cifrado saliente, descifrado entrante, firma saliente, validación firma entrante)
4. `CertificateEnvironment` (pruebas/producción)
5. `CertificateStatus` (activo, inactivo, expirado, revocado, reemplazado)
6. `CertificateUsageLog`
7. `CertificateRotationHistory`
8. `DigitalEnvelopeOperationLog`

### 9.2 Atributos/controles requeridos
- Cámara, ambiente, propósito, vigencia (`notBefore/notAfter`), estado.
- `thumbprint`, `fingerprint`, `serial`, `subject`, `issuer`, `hasPrivateKey`, algoritmo/longitud de llave.
- Activación única por contexto `(camara, ambiente, propósito)`.
- Historial completo sin `upsert` destructivo.
- Trazabilidad de quién/qué proceso activó, reemplazó o revocó.

## 10. Manejo de secretos propuesto

### 10.1 Decisión
**Prohibido almacenar password PFX en texto plano en BD.**

### 10.2 Estrategia recomendada (fase 1)
- BD guarda solo metadata + referencia de secreto (`SecretRef`) + material público si aplica.
- Secretos en gestor dedicado (preferido: Azure Key Vault / AWS Secrets Manager / gestor corporativo de secretos).
- Fallback local controlado: DPAPI/ProtectedData para entorno no cloud, con rotación y acceso mínimo.
- Privadas nunca expuestas por API; solo uso en memoria con mínimo tiempo de vida.
- Logging con enmascaramiento total de secretos.

### 10.3 Pruebas
- Solo certificados de prueba (autofirmados o fixtures controlados).
- Contraseñas dummy no reutilizables.

## 11. Auditoría criptográfica propuesta

Registrar por operación (encrypt/decrypt/verify):

- `operationId`, fecha/hora UTC, usuario/proceso.
- Cámara, ambiente, propósito.
- Archivo origen/destino (metadatos), extensión generada.
- Hash SHA-256 plano, hash SHA-256 cifrado (si aplica).
- Tamaño antes/después.
- Certificado usado: `thumbprint`, `serial`, `subject`, `issuer`, `versionId`.
- Resultado (`success/failure`) y `errorCode/errorDetail` sanitizado.

**No registrar contenido plano ni material secreto.**

## 12. Estrategia de pruebas futuras

### 12.1 Unit tests
- Parse/validación XML Digital Envelope.
- Firma/verificación obligatoria.
- ZIP/Base64.
- Cifrado/descifrado con casos positivos/negativos.
- Derivación `identifier/IV`.
- Validación de metadata y estado de certificado.

### 12.2 Integration tests
- Selección de certificado activo por contexto.
- Historial/versionado/rotación.
- Auditoría criptográfica persistida.
- Roundtrip plano → `.ENV`/`.env` → plano.

### 12.3 E2E técnico
- Generar NACHA, cifrar, descifrar, parsear, validar registros 1/5/6/7/8/9.
- No regresión con `ExternalFileNamePolicy`, `BatchNumberSequence` y filtros existentes.

### 12.4 Interoperabilidad
- Validación byte-a-byte contra vector oficial ACH/CENIT cuando esté disponible.
- Sin vector oficial, mantener estado “no listo para hardening”.

## 13. Plan de migración desde implementación actual

### 13.1 Qué se conserva temporalmente
- Endpoint de exportación cifrada y patrón de envoltura sobre archivo NACHA.
- Capacidad actual de carga/listado de certificados como base transitoria.

### 13.2 Qué se depreca
- Persistencia de `Password` en claro.
- `Upsert` destructivo sin historial.
- Flujo de descifrado que no exige verificación de firma.

### 13.3 Qué se reemplaza
- Repositorio/modelo de certificados por esquema versionado y contextual.
- `CryptoServiceScoped` detrás de adapter/profile con validaciones normativas estrictas.
- Auditoría simple de exportación por auditoría criptográfica integral.

### 13.4 Migración de datos
- Migrar certificados existentes a `DigitalCertificateVersion` conservando metadata y `UploadedAt` como referencia histórica.
- Marcar registros sin secreto migrado como `pending_secret_binding`.
- Proceso controlado para re-carga/re-vinculación de secretos sin exposición.

### 13.5 Compatibilidad temporal
- Ventana dual de lectura (modelo viejo + nuevo) solo durante transición.
- Cutover con bandera de feature por cámara/ambiente.

## 14. Decisión sobre extensión `.env` / `.ENV`

### 14.1 Qué se sabe
- Código actual de exportación utiliza `.ENV`.
- Normativa revisada confirma mensajería encriptada y Digital Envelope, pero no evidencia concluyente de case exacto obligatorio de extensión.

### 14.2 Decisión
- **Outbound:** default `.ENV` por compatibilidad actual.
- **Inbound:** aceptar `.ENV` y `.env` (case-insensitive) mientras no exista restricción explícita de cámara.
- **Configurabilidad:** habilitar parámetro por cámara/ambiente para forzar extensión exacta cuando exista confirmación normativa u operativa.

### 14.3 Estado
- `.ENV` default: Parcial (operativo actual, pendiente confirmación normativa de case).
- aceptación `.env` inbound: Confirmado como decisión técnica de resiliencia.
- imposición rígida de case único: Requiere confirmación normativa.

## 15. Preguntas abiertas (normativas/técnicas)

1. ¿ACH/CENIT exige extensión exacta `.env` o `.ENV` en todos los canales?
2. ¿La derivación de IV desde `identifier` tiene vector oficial de prueba/publicación formal?
3. ¿Se exige validación de revocación online (CRL/OCSP) en tiempo de operación o es control ex-ante?
4. ¿Existen archivos `.ENV` de certificación con expected output oficial para pruebas de interoperabilidad?
5. ¿Hay diferencias por cámara/servicio (ACH Transferencias vs otros) para firma/cifrado/compresión?
6. ¿La serialización XML requiere normalización específica (encoding, saltos de línea, orden estricto de tags) validada por ACH?

## 16. Decisiones por fase

### Fase 1 (obligatoria antes de hardening productivo)
- Cerrar confirmaciones normativas críticas (extensión, IV/identifier, vector interoperabilidad).
- Rediseñar gobierno de certificados y secretos (sin password en claro).
- Exigir verificación de firma en descifrado (política fail-close).
- Diseñar auditoría criptográfica completa.

### Fase 2
- Ejecutar plan de migración técnica y de datos.
- Activar pruebas integrales y pruebas de interoperabilidad.
- Habilitar rollout gradual por cámara/ambiente.

## 17. Riesgos principales

### Críticos
1. Interoperabilidad fallida por derivación `identifier/IV` no validada.
2. Aceptación de contenido sin verificación obligatoria de firma.

### Altos
1. Exposición de secretos por persistencia de password en claro.
2. Falta de historial/versionado/estado de certificados.
3. Falta de pruebas cripto y de interoperabilidad.

### Medios
1. Ambigüedad normativa de case de extensión.
2. Ambigüedad de política CRL/OCSP runtime por cámara.

### Bajos
1. Diferencias menores de serialización XML sin impacto funcional (si receptor tolera).

## 18. Veredicto ADR

- **ADR listo para revisión**.
- **No listo para hardening definitivo de CryptoService** hasta confirmar interoperabilidad normativa/técnica.
- **No listo para implementación completa de Certificate Management** sin definir modelo versionado y estrategia de secretos aprobada por seguridad.
