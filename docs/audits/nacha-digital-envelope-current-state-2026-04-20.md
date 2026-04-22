# Auditoría técnica y normativa — Estado actual NACHA-M sobre digital (`.env`)

**Fecha de auditoría:** 2026-04-21 (UTC)  
**Alcance:** Solo auditoría documental/técnica. No se implementó código, no se crearon pruebas, no se modificó lógica productiva.

## 1) Resumen ejecutivo

- **Existe implementación funcional de sobre digital** en backend (creación/apertura de sobre) y endpoint de exportación NACHA con opción de cifrado y salida con sufijo `.ENV`.  
- **Existe evidencia normativa fuerte** en `ACH-Colombia-V32.md` (Anexo 21) para un formato *Digital Envelope* XML con firma + cifrado híbrido (RSA + AES), incluyendo ejemplo para NACHA-M.  
- **No se encontró evidencia explícita de la extensión `.env` en minúscula** en la normativa extraída a Markdown; en código se usa `.ENV` (mayúscula). **Requiere confirmación normativa** de exactitud de extensión/case-sensitive.  
- **Gobierno de certificados está parcial:** hay carga y consulta de certificados (incluyendo metadata X.509), pero **sin historial/versionado**, **sin estado activo/inactivo/revocado**, y con **almacenamiento de password en texto claro** en base de datos.  
- **Auditoría criptográfica está parcial:** se audita exportación NACHA (incluye indicador `IsEncrypted`) pero **no** se auditan hashes, certificado usado, serial, errores cripto, ni operación de descifrado.  
- **Pruebas criptográficas son insuficientes:** hay pruebas de `NachaExportController` para naming y uso de flujo cifrado, pero no hay pruebas de roundtrip, expiración de certificado, password inválida, private key, ni validación criptográfica integral.

## 2) Evidencia ACH V32 sobre cifrado `.env` / sobre digital

### 2.1 Hallazgos normativos confirmados

- El manual indica que ACH usa un sistema de cifrado de archivos y remite al Anexo 21 para la mensajería encriptada de archivos NACHA-M. **Estado: Confirmado.**
- En Anexo 21 se describe explícitamente **Digital Envelope** con:
  - firma con llave privada RSA,
  - cifrado de contenido con AES-256,
  - cifrado de llave simétrica con certificado X.509 del receptor usando RSA. **Estado: Confirmado.**
- Se define estructura XML del sobre, incluyendo `recipientInfo`, `certificateInfo`, `keyEncryptionAlgorithm`, `encryptedKey`, `contentEncryptionAlgorithm`, `encryptedContent`. **Estado: Confirmado.**
- Se incluye ejemplo de algoritmos: `RSA/NONE/PKCS1Padding` y `AES/CBC/PKCS5padding`. **Estado: Confirmado.**
- Se especifica que el contenido firmado viene comprimido en ZIP y Base64. **Estado: Confirmado.**
- Se especifican requisitos de certificados digitales (X.509 v3, RSA 2048, compatibilidad PKCS#12 y `.cer`, CRL/OCSP, etc.). **Estado: Confirmado.**

### 2.2 Puntos no explícitos en la evidencia localizada

- No se encontró referencia explícita a extensión de archivo **`.env` en minúscula** en la versión Markdown auditada; sí existe la definición de mensajería encriptada/sobre digital y en código se usa `.ENV`. **Estado: Requiere confirmación normativa.**
- No se identificó mención explícita a CMS/PKCS#7 como formato contenedor estándar para el sobre (aunque se usa terminología `SignedData`). **Estado: Requiere confirmación normativa.**
- No se identificó, en las secciones revisadas, política normativa explícita de rotación/versionado operativo de certificados en BD del participante. **Estado: Requiere confirmación normativa.**

## 3) Estado actual del código de cifrado

### 3.1 Componentes encontrados

- `NachaExportController.ExportEncrypted` genera NACHA plano y, según política (`IDigitalEnvelopePolicy`) o `forceEncryption`, crea sobre digital y responde archivo `application/xml` con sufijo `.ENV`.  
- `CryptoServiceScoped.CreateEnvelopeAsync` implementa flujo de:
  1) firma del hash SHA-256,
  2) compresión ZIP del contenido + Base64,
  3) cifrado de mensaje firmado con AES/CBC/PKCS7,
  4) cifrado de llave AES con RSA PKCS#1 v1.5,
  5) serialización XML de `DigitalEnvelopeModel`.  
- `RsaKeyProvider` resuelve certificados desde repositorio de BD (si existen) y hace fallback a almacén/configuración de sistema.

### 3.2 Evaluación de alineación normativa

- Firma + cifrado híbrido + XML: **Parcial/alto alineamiento** con Anexo 21.  
- Algoritmos declarados en XML (`RSA/NONE/PKCS1Padding`, `AES/CBC/PKCS5padding`): **Parcial**, porque implementación usa APIs .NET equivalentes (`PKCS7` para padding simétrico) y requiere validación de interoperabilidad byte-a-byte con ACH/CENIT.  
- Construcción del `identifier` e IV: **Brecha crítica de compatibilidad**; normativa describe derivación específica del IV desde `identifier` con procedimiento concreto, pero cifrado actual usa `aes.GenerateIV()` y `identifier` distinto. **Requiere confirmación normativa + prueba de interoperabilidad.**

## 4) Estado actual del código de descifrado

- Existe endpoint `/SobreDigital/decrypt` y método `OpenEnvelopeAsync`. **Confirmado.**
- El descifrado intenta:
  - descifrar llave con RSA privada,
  - derivar IV desde `identifier` vía SHA-256 truncado,
  - descifrar AES,
  - deserializar mensaje firmado,
  - descomprimir ZIP,
  - calcular hash y preparar verificación de firma.
- **Brecha técnica relevante:** la verificación de firma (`VerifyHash`) está comentada y no se valida resultado de autenticidad/integridad antes de devolver contenido. **Alto riesgo.**
- **Inconsistencia potencial de certificados en apertura:** se usa `ObtenerCertificate("CertSign")` como “receptor” para abrir sobre; requiere validación contra modelo operativo real (certificado de descifrado esperado). **Requiere confirmación técnica/normativa.**

## 5) Estado actual de certificados

### 5.1 Cobertura existente

- Modelo `DigitalEnvelopeCertificate` guarda: `FileName`, `Type`, `RawData`, `Password`, `HasPrivateKey`, `Subject`, `Issuer`, `Thumbprint`, `NotBefore`, `NotAfter`, `UploadedAt`.  
- API administrativa `nacha-security/certificates` permite listar/subir/eliminar certificados y parsea X.509 (`LoadCertificate`/`LoadPkcs12`).
- Tipos actuales: `EncryptionPublic` y `SigningKeyPair`.

### 5.2 Brechas de gobierno

- **Password PFX se persiste en texto plano (`Password`)** junto al certificado: incumple práctica de seguridad exigida para secretos.  
- **Sin historial/versionado real:** `Upsert` elimina previos por tipo y conserva solo uno activo implícito.  
- **Sin estado de ciclo de vida** (activo/inactivo/expirado/revocado/reemplazado) como entidad de negocio.  
- **Sin selección contextual** por cámara/ambiente/propósito (pruebas/prod, cifrado/descifrado/firma/validación).  
- **Sin campo explícito de serial en entidad** (aunque se usa en runtime desde certificado) y sin políticas de rotación auditables.  
- **Sin evidencia de validación CRL/OCSP en tiempo de uso.**

## 6) Estado actual de pruebas

- Existen pruebas unitarias de `NachaExportController` para:
  - exportación plana,
  - exportación cifrada (mock de `CreateEnvelopeAsync`),
  - naming CENIT y normalización identificador,
  - manejo de error fatal NACHA.  
- No se encontraron pruebas para:
  - cifrado/descifrado real criptográfico,
  - roundtrip plano → `.ENV` → plano,
  - expiración de certificado,
  - certificado sin llave privada,
  - password inválida de PFX,
  - selección de certificado activo por ambiente/cámara/propósito,
  - auditoría criptográfica detallada.

## 7) Brechas encontradas

1. **Interoperabilidad normativa del IV/identifier no demostrada** (posible incompatibilidad con formato esperado Anexo 21).  
2. **Ausencia de verificación efectiva de firma en descifrado** (código comentado).  
3. **Gestión insegura de secretos**: password de certificado en texto plano en BD.  
4. **Gobierno de certificados incompleto**: sin historial/versionado/estado/rotación por contexto.  
5. **Auditoría criptográfica incompleta** respecto a requerimientos de trazabilidad.  
6. **Cobertura de pruebas insuficiente** para riesgos cripto y regulatorios.  
7. **Extensión `.env` minúscula no confirmada** en evidencia normativa revisada (código usa `.ENV`).

## 8) Matriz de riesgos

| ID | Riesgo | Severidad | Probabilidad | Impacto | Evidencia | Mitigación sugerida |
|---|---|---|---|---|---|---|
| R1 | Incompatibilidad de sobre digital por derivación IV/identifier | Crítico | Media | Alto (rechazo de archivos) | Implementación actual vs Anexo 21 | Prueba de interoperabilidad contra vector oficial + ADR técnica |
| R2 | Descifrado sin verificación efectiva de firma | Crítico | Media | Alto (integridad/autenticidad) | `VerifyHash` no aplicado | Activar y hacer obligatoria verificación/fallo seguro |
| R3 | Password PFX almacenada en claro | Alto | Alta | Alto (compromiso de llave) | Campo `Password` persistido | KMS/secret manager + cifrado en reposo + no persistir secreto claro |
| R4 | Sin historial y estados de certificados | Alto | Alta | Medio/Alto (operación/auditoría) | Upsert destructivo por tipo | Modelo versionado con estado, vigencia, reemplazo, auditoría |
| R5 | Auditoría cripto limitada | Alto | Media | Alto (hallazgo regulatorio) | Solo `IsEncrypted` y métricas básicas | Bitácora criptográfica con hash/cert/resultado/error |
| R6 | Falta de pruebas de escenarios críticos | Alto | Alta | Alto | No hay suite cripto integral | Plan QA por fases con certificados de prueba |
| R7 | Ambigüedad normativa de extensión `.env`/`.ENV` | Medio | Media | Medio | No hallazgo explícito en md | Confirmación formal ACH/CENIT y regla configurable documentada |

## 9) Recomendación de siguientes fases

### Fase 0 — Cierre normativo (ADR)
- Congelar decisiones de formato en un ADR con tabla **Confirmado / Parcial / Requiere confirmación normativa**.
- Confirmar formalmente con ACH/CENIT: extensión exacta (`.env` vs `.ENV`), derivación de IV, firma obligatoria en todos los flujos, compresión obligatoria.

### Fase 1 — Hardening de certificados
- Diseñar gobierno certificado versionado (sin pérdida histórica), con estado y contexto (ambiente/cámara/propósito).
- Remover password en claro de persistencia; integrar secreto seguro externo.

### Fase 2 — Conformidad criptográfica e interoperabilidad
- Validar implementación contra vectores de prueba de ACH/CENIT.
- Forzar verificación de firma en descifrado y manejo de errores criptográficos auditables.

### Fase 3 — Observabilidad y auditoría regulatoria
- Registrar hash plano/cifrado, certificado utilizado, serial/thumbprint, operación, resultado, error y tamaños.

### Fase 4 — QA integral
- Suite automatizada de roundtrip y casos negativos con certificados de prueba únicamente.

## 10) Veredicto de auditoría

- **Veredicto:** **listo para ADR** y **requiere confirmación normativa adicional** antes de implementación/hardening.  
- **No listo para implementación directa** sin cierre de brechas críticas (R1, R2, R3).

## 11) Fuentes inspeccionadas en repositorio

- Normativa base: `docs/normativa/md/ACH-Colombia-V32.md`, `docs/normativa/md/CENIT-*.md`, y artefactos en `docs/normativa/pdf/*` (sin extracción automática disponible en el entorno actual).
- Código: API, Application, Domain, Persistence y pruebas backend relacionadas con sobre digital, certificados y exportación NACHA.
