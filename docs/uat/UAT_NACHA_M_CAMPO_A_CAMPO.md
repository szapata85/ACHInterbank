# UAT NACHA-M Campo a Campo - ACH Colombia y CENIT

Fecha: 2026-05-19 America/Bogota  
Rama: `uat/nacha-m-soap-proc-contrapartidas`  
Commit base: `98934e01`  
Ambiente: Docker Compose local, SPA `http://localhost:743`, API `http://localhost:843`.

## Resultado Ejecutivo

Se ejecuto UAT funcional integrado con datos sinteticos/anonimizados para ACH Colombia y CENIT. Se crearon transacciones de salida UAT por API autenticada y se intento generar archivo NACHA-M UAT por el generador real del sistema.

Resultado: **BLOQUEADO / PARCIAL**. No se obtuvo archivo NACHA-M valido para ninguna camara. Los intentos finales por `/NachaExport/{cycleId}` respondieron `HTTP 200` con `Content-Length: 0`. La ruta `nacha-security/operations/nacha/generate` registro falla controlada por regla de negocio: transaccion sin prenotificacion previa. No se aplico bypass, backdating, cambio de reglas ACH/NACHA-M/CENIT ni generacion manual de archivo.

Productivo permanece **NO-GO**.

## Transacciones UAT

Datos maestros sinteticos usados/ajustados para evitar bancos productivos reales:

| Dato maestro | ID | Uso |
|---|---:|---|
| Banco UAT Origen | 92 | Institucion origen default en runtime UAT local |
| Banco UAT Destino | 93 | Destino ACH Colombia, preferencia default ACH Colombia |
| Banco UAT Destino CENIT | 94 | Destino CENIT, preferencia default CENIT |
| Company Entry Description `TRASLADOS` | 30 | Descripcion de lote sintetica |

| Camara | Referencia | TransactionId | Estado | Ciclo | Resultado |
|---|---|---:|---|---|---|
| ACH Colombia | `UAT-ACHCOL-NACHA-SOAP-001` | 3 | `Pending` | `2ada513804193e8aa8771252660182fdc7a55862` | Creada y persistida |
| CENIT | `UAT-CENIT-NACHA-SOAP-001` | 4 | `Pending` | `7c0c26327f06a20d751ef72fc379ca6fe7166803` | Creada y persistida |

Ambas transacciones usan datos sinteticos, monto `1000`, cuentas no reales y bancos UAT sinteticos.

## Intentos De Generacion NACHA-M

| Camara | Intento | Resultado | Evidencia |
|---|---:|---|---|
| ACH Colombia | 1 | `/NachaExport` devolvio fallback Angular `index.html`; se corrigio proxy Nginx. | `docs/uat/evidencias/nacha-m-uat/ach-colombia/attempt_1_proxy_html_response.html` |
| ACH Colombia | 2 | Respuesta 0 bytes. | `docs/uat/evidencias/nacha-m-uat/ach-colombia/attempt_2_zero_byte_response.txt` |
| ACH Colombia | 3 | `HTTP 200`, `Content-Length: 0`; no HTML, no JSON, pero archivo vacio. | `docs/uat/evidencias/nacha-m-uat/ach-colombia/attempt_3_export_response_headers.txt` |
| CENIT | 1 | `/NachaExport` devolvio fallback Angular `index.html`; se corrigio proxy Nginx. | `docs/uat/evidencias/nacha-m-uat/cenit/attempt_1_proxy_html_response.html` |
| CENIT | 2 | Respuesta 0 bytes. | `docs/uat/evidencias/nacha-m-uat/cenit/attempt_2_zero_byte_response.txt` |
| CENIT | 3 | `HTTP 200`, `Content-Length: 0`; no HTML, no JSON, pero archivo vacio. | `docs/uat/evidencias/nacha-m-uat/cenit/attempt_3_export_response_headers.txt` |

## Validacion Campo a Campo

No se puede marcar OK tecnico normativo porque no existe archivo NACHA-M valido generado por el sistema en esta ejecucion.

| Registro | ACH Colombia | CENIT | Resultado |
|---|---|---|---|
| 1 | No validable, archivo 0 bytes | No validable, archivo 0 bytes | FALLA |
| 5 | No validable, archivo 0 bytes | No validable, archivo 0 bytes | FALLA |
| 6 | No validable, archivo 0 bytes | No validable, archivo 0 bytes | FALLA |
| 7 | No validable | No validable | PENDIENTE/NO VALIDABLE |
| 8 | No validable, archivo 0 bytes | No validable, archivo 0 bytes | FALLA |
| 9 | No validable, archivo 0 bytes | No validable, archivo 0 bytes | FALLA |

Matrices por camara:

- `docs/uat/evidencias/nacha-m-uat/ach-colombia/matriz_campo_a_campo.md`
- `docs/uat/evidencias/nacha-m-uat/cenit/matriz_campo_a_campo.md`

## Diagnostico

- Proxy SPA/Nginx: corregido para `/NachaExport/`.
- Generador real NACHA-M: no produjo archivo valido por endpoint de descarga; queda defecto por respuesta vacia `HTTP 200`.
- Generacion por modulo NACHA security: confirma precondicion funcional no cumplida: falta prenotificacion previa.
- Documentacion normativa especifica por camara: parcial/no completa en repositorio; no permite homologacion campo-a-campo oficial.
- No hubo transmision de archivos a ACH Colombia ni CENIT.

## Defectos

| Defecto | Estado | Observacion |
|---|---|---|
| DEF-UAT-020 | Abierto / Parcial | Falta validacion campo-a-campo y homologacion/waiver; el archivo real UAT no se genero validamente. |
| DEF-UAT-021 | Abierto | `/NachaExport/{cycleId}` devuelve `HTTP 200` con 0 bytes para ciclos con transacciones sinteticas; debe responder error controlado si no hay archivo exportable. |
| DEF-UAT-022 | Abierto | Job `Proc_Contrapartidas` intento automaticamente endpoint SOAP externo/no resoluble en ambiente UAT; requiere modo dry-run/mock o guardrail de ambiente. |

## Decision

NACHA-M + validacion normativa por camara queda **PARCIAL / BLOQUEADO**. Se mantiene **NO-GO productivo**.
