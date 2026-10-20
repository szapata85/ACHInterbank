# RET.PROFILE.ACHCOL.INBOUND.RETURN.1 — evidencia controlada

Fecha de ejecución: 2026-08-09. Ambiente: Docker local controlado, API `localhost:843`, SPA `localhost:743`, SQL Server local y PostgreSQL local para pruebas multi-proveedor.

## Resultado

`PARCIAL`. `ProfileNotFound` quedó resuelto y la discontinuidad física se cerró hasta una Return huérfana real. No se declara `RET-GAP-007` cerrado porque la base fresca no contiene la transacción original referenciada por la addenda 99 y no existe un flujo normal de alta que permita preservar ese rastreo externo.

## Perfil y normativa

- Perfil: `OFFICIAL_ACH_ENTRADA_DEVOLUCION_V1_0`, ACH + RETORNO + ENTRADA, publicado, homologado, versión 1.0.
- Fuente: ACH Colombia V35, secciones 6.6 y 6.7.
- Addenda 99 declarativa: causal en posiciones 4–6, rastreo original en 7–21 y nuevo rastreo/secuencia en 82–96.
- Selección: cámara ACH, dirección entrada, flujo retorno, mensaje diferencial y addenda tipo 99.
- El parser permanece genérico; los offsets y variantes viven en `NachaConfig`.

## Inventario original

Directorio inmutable: `docs/uat/certificados_pruebas/archivo_prueba/ACH Colombia`.

| Archivo | Bytes | Observación |
| --- | ---: | --- |
| `0001283.001.20260727.1.OUT.env` | 13218 | Sobre digital original; descifrado operativo, sin Return utilizable. |
| `0001283.002.20260727.1.OUT.env` | 8482 | Sobre digital original. |
| `0001283.003.20260727.1.OUT.env` | 10530 | Sobre digital original usado; contiene una addenda 99 R04. |
| `0001283.004.20260727.1.OUT.env` | 8545 | Sobre digital original. |
| `0001283.005.20260727.1.OUT.env` | 7778 | Sobre digital original. |

Los originales no fueron editados, sobrescritos, recifrados ni eliminados.

## Certificados y criptografía

- `ACHcolombia.cer`: certificado público de validación de ACH Colombia.
- `CFA.pfx`: certificado de CFA con clave privada para firma/descifrado; su contraseña se leyó únicamente en memoria desde el material local y no se registró.
- La carga/configuración se hizo desde la SPA real. El intento duplicado fue rechazado conforme a la política existente.
- El servicio oficial de sobre digital resolvió los certificados desde el contenedor y descifró en memoria; no hubo script criptográfico alterno ni persistencia del plaintext en disco.
- Prueba de certificado/sobre: 1/1 passed; el sobre `.001` produjo 19080 bytes estructurados desde 13218 bytes cifrados.

## Upload, parser y persistencia

El sobre `.003` se cargó desde `/transactions/nacha-upload`. El backend ejecutó descifrado oficial, selección de perfil y parser genérico. Resultado fresco:

| Entidad | Cardinalidad |
| --- | ---: |
| Ingesta canónica | 1 |
| File Header | 1 |
| Batch Header | 15 |
| Entry Detail persistidas | 2 |
| Addenda persistidas | 2 |
| Batch Control | 15 |
| File Control | 1 |
| Clasificaciones | 2 |
| Vínculos no finales | 2 |
| Return addenda 99 R04 | 1 |

La ingesta conserva `ProfileCode=OFFICIAL_ACH_ENTRADA_DEVOLUCION_V1_0` y `ProfileVersion=1.0`. La R04 conserva un rastreo original de 15 caracteres y el campo corto de secuencia de addenda permanece vacío, como corresponde al mapping V35.

## Idempotencia

El reupload del mismo `.003` devolvió `Duplicado`. Permanecieron una ingesta canónica, una Return funcional y las mismas cardinalidades NACHA; no hubo segundo vínculo funcional, transición, evento ni aplicación.

## Huérfana y límite de cierre

La Return R04 nació exclusivamente del archivo físico y aparece en **Devoluciones recibidas sin relación**. Antes de cualquier resolución existen cero aplicaciones y los vínculos permanecen no finales.

La base fresca tiene cero transacciones con el rastreo original exacto. El prefijo corresponde a la institución origen canónica, pero el consecutivo original está separado en 6.723.803 unidades del siguiente consecutivo generado por el alta normal. La API normal no acepta un rastreo externo. Por eso no se fabricó candidata por SQL, no se alteró el archivo y no se relajó `IncomingNachaOrphanCompatibilityPolicy`.

## Pruebas y CI

- Perfil focal final: 6/6 passed.
- Persistencia de identidad seleccionada: 1/1 passed.
- Fuente/constante/expresión declarativa: 1/1 passed.
- Matriz SQL Server + PostgreSQL: 2/2 passed.
- Playwright certificados/cripto: 1/1 passed.
- Playwright archivo/perfil/parser/huérfana/duplicado: 1/1 passed.
- Build Release: 0 warnings, 0 errors.
- Backend completo: 2215 total, 2194 passed, 11 skipped, 10 failed, 37m40s. Ocho fallos fueron suites multi-DB sin flags/conexiones, uno timeout OpenAPI y uno de fuente declarativa corregido y revalidado 1/1. No se declara CI global verde.

## Veredicto

`RET-GAP-007 ABIERTO — falta una transacción original local legítima que preserve el rastreo exacto de la R04 para resolver esa misma huérfana física sin seed post-parser ni SQL.`
