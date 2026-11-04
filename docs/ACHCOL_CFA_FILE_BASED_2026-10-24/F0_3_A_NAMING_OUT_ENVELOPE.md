# F0.3.A — Aclaración de nomenclatura ACH Colombia (`.OUT` / `.OUT.env`)

- **Programa:** `ACHCOL-NACHA-CFA-EXTCYCLE-001`
- **Fecha:** 2026-10-24
- **Estado:** REGLA DE ENTRADA ADOPTADA PARA F1.1; **documentación corregida**, no implementación ni prueba final.
- **Origen de la decisión:** aclaración explícita del solicitante: ACH Colombia entrega archivos terminados en `.out` si ya están descifrados y `.out.env` si llegan en sobre digital cifrado.
- **Baseline de código:** `ACHInterbank_ACHCOL_CFA_F0_3_UNIFICADO.zip`; sin cambios de producción en este adéndum.

## Decisión F0.3.A / DEC-016

Las extensiones finales son parte **del nombre físico recibido**, no son el número de ciclo. Para entradas ACHCOL:

1. `*.OUT` (comparación de extensiones sin distinguir mayúsculas/minúsculas) representa un archivo NACHA-M **en claro**. No se invoca descifrado; pasan bytes al parser y al flujo de validación.
2. `*.OUT.env` representa **un sobre digital cifrado**. Se valida cámara ACHCOL y se usa el servicio de sobre digital existente; sólo si descifrado/verificación de firma y controles de certificados tienen éxito, se entregan los bytes originales al pipeline NACHA-M. El nombre interno lógico quita **solamente el sufijo final `.env`**, conservando `.OUT` y el nombre exterior para trazabilidad.
3. Los **segmentos que preceden a `.OUT`** contienen metadatos, incluidos consecutivo `ZZZ` y **ciclo externo** `N` cuando corresponde. El último componente numérico **antes** de `.OUT` identifica el ciclo externo; no `.OUT` ni `.env`.
4. Ejemplos concretos respaldados por tests actuales: `0001283.001.20260731.1.OUT` y `0001283.001.20260731.1.OUT.env`. Generalizar a los ciclos `8`, `9`, `10` y siguientes (`0001283.008.20260731.8.OUT`, `0001283.009.20260731.9.OUT.env`, sujetos a validación de fecha/nombre/cámara), **sin** exigir `AchCycleId` correspondiente. Las letras `H` e `I` están en el encabezado NACHA-M, no en las extensiones.
5. La gramática mínima **confirmada por evidencia de tests** es `RRRRTTT.ZZZ.AAAAMMDD.N.OUT[.env]`. F0.3 describía `RRRRTTT.ZZZ.N` como nombre operativo lógico para ciertos contextos, pero **no debe imponerse como nombre físico entrante**. Confirmar perfiles/nombres alternativos desde código y homologación, sin asumir que todos los flujos usan fecha o que la ruta outbound es igual a la inbound.
6. **Seguridad:** nunca interpretar los bytes de `.env` como texto plano por un fallback, ni descifrar silenciosamente archivos `.OUT`; fallos de firma, certificado, integridad y nombre/cámara deben fallar cerrados y quedar auditados.
7. **Límite funcional:** se refiere a archivos `OUT` de ACHCOL. `RET` tiene reglas propias, ya reconocidas en MFT en formato cifrado; no cambiar la ruta de devoluciones al añadir soporte de `.OUT` en claro.

## Hallazgos estáticos en el código de F0.3

| Ruta/componente | Evidencia verificada | Implicación |
|---|---|---|
| `src/Cfa.ACHInterbank.Api/Controllers/NachaUploadController.cs` | `AllowedExtensions` incluye `.OUT` y `.env` | El **upload manual** ya acepta ambas clases por extensión, pero falta validar ciclo externo sin `AchCycle`. |
| `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/IncomingNachaIngestionAppService.cs` | Si el nombre termina `.env`, valida cámara ACHCOL, llama `IManagedDigitalEnvelopeService.DecryptAsync`, cambia `processingFileBytes/Name` y computa hash; si no, usa bytes originales | **Reutilizar el flujo canónico** para soportar el parsing tras descifrado. |
| `tests/Cfa.ACHInterbank.Tests/ManagedDigitalEnvelopeServiceTests.cs` | `BuildDecryptedFileName("0001283.001.20250331.1.OUT.env")` → `...1.OUT`; roundtrip cifrar/descifrar | Extensión final `.env` se elimina sin cambiar `.OUT`. |
| `tests/Cfa.ACHInterbank.Tests/IncomingNachaIngestionAppServiceTests.cs` | Fixture `0001283.001.20260731.1.OUT.env` → `...1.OUT` antes de resolver ciclo y parsear | La nomenclatura física entrante incluye **fecha y tipo OUT**, omisiones del parser propuesto inicialmente. |
| `src/Cfa.ACHInterbank.External/Connections/AchColombiaManagedMftFolderAdapter.cs` | `IsEligible` permite `.OUT.env` y `.RET.env`; **no `.OUT`** | **Brecha real:** descubrimiento automático Managed Folder no observa `*.OUT` en claro. Ampliar de forma controlada a `.OUT` en F1.1/F1.2. |
| `src/Cfa.ACHInterbank.External/Connections/AchColombiaSftpMftAdapter.cs` | `IsEligibleFinalName` permite `.OUT.env` y `.RET.env`; **no `.OUT`** | **Brecha real:** descubrimiento SFTP no observa `*.OUT` en claro. Ampliar junto con Managed Folder sin aceptar `.partial`, archivos claim ni nombres inseguros. |
| `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchColombiaManagedMftImplementationSupport.cs` | `IsAuthorizedInboundEnvelopeName` solo acepta `.OUT.env`, `.RET.env` | Separar política de ingreso **cifrado** de la nueva política de ingreso plano. No suprimir controles de legitimidad de origen/integridad del canal. |

## Nueva secuencia de implementación F1.1/F1.2

**F1.1 — Reconocimiento, normalización y parsing de nombre físico:**
- Clasificar `*.OUT` (plain) frente a `*.OUT.env` (encrypted) y preservar `OriginalPhysicalFileName` y `CanonicalPlainFileName` (por ejemplo `...8.OUT.env` → `...8.OUT`).
- Reconocer la estructura física conocida `RRRRTTT.ZZZ.AAAAMMDD.N.OUT[.env]`, sin límite arbitrario 7 en `N`. Sólo el nombre físico seguro debe ingresar a la etapa de contenido.
- Extraer `FileSequence/ZZZ`, `ExternalCycleNumber/N`, `FileDate` si aplica; dejar `AchCycleId` **independiente y opcional** según ruta aprobada; no inventar `AchCycle`.
- No extender el parser de CENIT ni admitir suffix ambiguo `.env` de cualquier cámara.

**F1.2 — Ingesta desde cada canal y header:**
- Conservar el decrypt canónico existente y la misma trayectoria de parser/negocio.
- Actualizar selección de archivos entrantes MFT folder/SFTP para `*.OUT` en claro, además de `*.OUT.env` cifrados; preservar `RET` por su ruta.
- Validar que `FileIdModifier` (`H`, `I`), `ZZZ` (008/009) y `N` (8/9/10) se mantengan como campos **distintos**.
- Registrar la doble identidad/hash de bytes cifrados y en claro de forma segura; no persistir el plaintext descifrado a disco ni registrar datos sensibles.

## Matriz de pruebas añadida a F6 (sin bloquear F1–F5)

| ID | Archivo entrante | Resultado esperado |
|---|---|---|
| AT-21 | `0001283.008.20260731.8.OUT` + header `H` | Ingreso en claro, parsing, `FileSequence=8`, `ExternalCycleNumber=8`; sin ciclo 8 interno requerido. |
| AT-22 | `0001283.009.20260731.9.OUT.env` + header interno `I` | Descifrado y firma válidos, nombre canónico `...9.OUT`, parser y despacho elegible sin ciclo 9. |
| AT-23 | Igual AT-22, firma/certificado inválido | Rechazo seguro sin parser ni despacho. |
| AT-24 | `0001283.008.20260731.10.OUT.env` + header interno `H` | `FileSequence=8` independiente de `ExternalCycleNumber=10`. |
| AT-25 | Carpeta Managed MFT contiene `.OUT` y `.OUT.env` | Descubre ambos y aplica ruta plain/decrypt según nombre, sin duplicados. |
| AT-26 | Bandeja SFTP contiene `.OUT` y `.OUT.env` | Descubre ambos, respeta claim/archive y evita archivos ocultos/temporales. |
| AT-27 | Archivo `.OUT` ya descifrado | No intenta descifrar y conserva bytes/hash de entrada. |
| AT-28 | Archivo `.RET.env` o cámara CENIT | Sin regresiones de flujo existente. |

**Nota de fuentes:** revisé los archivos de proyecto disponibles y el repositorio incluido; no pude verificar directamente el contenido de la conversación pasada con Apps Script/Sheets. Esta decisión se apoya en la especificación del solicitante y en los fuentes/test locales citados; no atribuir hallazgos a ese Sheet sin acceder a él.
