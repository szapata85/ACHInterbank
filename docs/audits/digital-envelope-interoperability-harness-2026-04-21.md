# Digital Envelope Interoperability Harness (2026-04-21)

## 1) Objetivo

Crear un harness de interoperabilidad para medir y comparar el sobre digital Anexo 21 sin modificar criptografía productiva.

## 2) Qué valida el harness

- Presencia de nodos XML requeridos.
- Algoritmos declarados (`RSA/NONE/PKCS1Padding`, `AES/CBC/PKCS5padding`).
- `identifier` presente y diagnóstico de IV derivado.
- Presencia/validez Base64 de `encryptedKey` y `encryptedContent`.
- Roundtrip sintético cifrar→descifrar→validar firma.
- Reporte estructural estable y exportable.

## 3) Qué no modifica

- No cambia XML productivo.
- No cambia `identifier`.
- No cambia IV ni derivación.
- No cambia AES/RSA/padding.
- No cambia generación de `SignedData`.

## 4) Cómo cargar vector oficial

Ruta esperada:

`tests/Cfa.ACHInterbank.Tests/Fixtures/DigitalEnvelope/OfficialVectors/`

Archivos esperados:
- `official-envelope.env`
- `official-plain-nacha.txt`
- `official-public-cert.cer`
- `official-metadata.json`

Si faltan, tests oficiales quedan en modo pending (no fallan).

## 5) Cómo interpretar reporte

`DigitalEnvelopeInteroperabilityReport` incluye:

- formato detectado,
- nodos requeridos,
- algoritmos declarados,
- `identifier` y longitud,
- diagnóstico de IV,
- validación firma (roundtrip),
- metadata de cifrado,
- validación Base64/ZIP,
- metadata de certificado,
- diferencias/warnings,
- bandera `RequiresOfficialVector`.

## 6) Estado actual identifier/IV

- La implementación observada mantiene derivación actual:
  - IV = primeros 16 bytes de SHA-256(identifier UTF-8).
- Sin vector oficial no se puede cerrar interoperabilidad byte-a-byte externa.

## 7) Pruebas ejecutadas

- `dotnet build ACHInterbank.sln -c Release`
- `dotnet test ... --filter "FullyQualifiedName~Interoperability|FullyQualifiedName~DigitalEnvelope|FullyQualifiedName~Signature|FullyQualifiedName~OpenEnvelope"`
- `dotnet test ... --filter "FullyQualifiedName~Nacha|FullyQualifiedName~Mapping|FullyQualifiedName~BatchNumber"`
- `rg -n "Password|PfxPassword|PrivateKey|RawPrivate|SecretRef|Secret|RawData|ToBase64String|Export" ...`

## 8) Riesgos

- Falta vector oficial para validación externa definitiva de `identifier/IV`.
- Comparación oficial queda pendiente hasta recibir material formal ACH/CENIT.

## 9) Próximos pasos

1. Cargar vector oficial en `OfficialVectors/`.
2. Ejecutar pruebas de comparación oficial estructura/contenido/firma/identifier-IV.
3. Registrar brechas observadas sin cambiar criptografía productiva hasta aprobación formal.


## 10) Referencia de certificación

- Solicitud formal y runbook operativo del vector oficial: `docs/certification/digital-envelope-official-vector-request-2026-04-21.md`.
- Mantener `OfficialVectors/` sin secretos y ejecutar validación oficial solo con artefactos entregados por ACH/CENIT.
