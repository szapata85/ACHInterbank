# UAT NACHA-M Campo a Campo - ACH Colombia y CENIT

Fecha: 2026-05-19 America/Bogota  
Rama: `uat/nacha-m-soap-proc-contrapartidas`  
Commit base: `f8c39d5`
Ambiente: Docker Compose local, SPA `http://localhost:743`, API `http://localhost:843`.

## Resultado Ejecutivo

Se ejecuto UAT funcional integrado con datos sinteticos/anonimizados para ACH Colombia y CENIT. Se crearon transacciones de salida UAT por API autenticada y se intento generar archivo NACHA-M UAT por el generador real del sistema.

Resultado actualizado: **BLOQUEADO / PARCIAL** para validacion normativa campo-a-campo, con correccion tecnica aplicada a exportacion vacia. No se obtuvo archivo NACHA-M valido para ninguna camara porque las transacciones UAT no cumplen prenotificacion previa. Tras DEF-UAT-021, `/NachaExport/{cycleId}` ya no devuelve `HTTP 200` con archivo 0 bytes; ahora responde `HTTP 422` JSON con `NACHA_EXPORT_PREREQUISITE_FAILED` y causa funcional. No se aplico bypass, backdating, cambio de reglas ACH/NACHA-M/CENIT ni generacion manual de archivo.

Productivo permanece **NO-GO**.

## Transacciones UAT

Datos maestros UAT usados/ajustados:

| Dato maestro | ID | Uso |
|---|---:|---|
| Cooperativa Financiera de Antioquia | 34 | Institucion origen default (`IsDefaultSource=true`), equivalente runtime a CFA Cooperativa Financiera |
| Banco UAT Destino | 93 | Destino ACH Colombia, preferencia default ACH Colombia |
| Banco UAT Destino CENIT | 94 | Destino CENIT, preferencia default CENIT |
| Company Entry Description `TRASLADOS` | 30 | Descripcion de lote sintetica |

| Camara | Referencia | TransactionId | Estado | Ciclo | Resultado |
|---|---|---:|---|---|---|
| ACH Colombia | `UAT-ACHCOL-NACHA-SOAP-001` | 3 | `Pending` | `2ada513804193e8aa8771252660182fdc7a55862` | Creada y persistida |
| CENIT | `UAT-CENIT-NACHA-SOAP-001` | 4 | `Pending` | `7c0c26327f06a20d751ef72fc379ca6fe7166803` | Creada y persistida |

Ambas transacciones usan datos sinteticos, monto `1000`, cuentas no reales y destinos UAT sinteticos. El origen/default source queda en CFA/Cooperativa Financiera de Antioquia; `Banco UAT Origen` ya no es default.

## Intentos De Generacion NACHA-M

| Camara | Intento | Resultado | Evidencia |
|---|---:|---|---|
| ACH Colombia | 1 | `/NachaExport` devolvio fallback Angular `index.html`; se corrigio proxy Nginx. | `docs/uat/evidencias/nacha-m-uat/ach-colombia/attempt_1_proxy_html_response.html` |
| ACH Colombia | 2 | Respuesta 0 bytes. | `docs/uat/evidencias/nacha-m-uat/ach-colombia/attempt_2_zero_byte_response.txt` |
| ACH Colombia | 3 | `HTTP 200`, `Content-Length: 0`; no HTML, no JSON, pero archivo vacio. | `docs/uat/evidencias/nacha-m-uat/ach-colombia/attempt_3_export_response_headers.txt` |
| ACH Colombia | 4 | `HTTP 422` JSON controlado; causa: transaccion 3 sin prenotificacion previa. No se genero archivo vacio. | `docs/uat/evidencias/nacha-m-uat/ach-colombia/attempt_4_controlled_422_response.txt` |
| CENIT | 1 | `/NachaExport` devolvio fallback Angular `index.html`; se corrigio proxy Nginx. | `docs/uat/evidencias/nacha-m-uat/cenit/attempt_1_proxy_html_response.html` |
| CENIT | 2 | Respuesta 0 bytes. | `docs/uat/evidencias/nacha-m-uat/cenit/attempt_2_zero_byte_response.txt` |
| CENIT | 3 | `HTTP 200`, `Content-Length: 0`; no HTML, no JSON, pero archivo vacio. | `docs/uat/evidencias/nacha-m-uat/cenit/attempt_3_export_response_headers.txt` |
| CENIT | 4 | `HTTP 422` JSON controlado; causa: transaccion 4 sin prenotificacion previa. No se genero archivo vacio. | `docs/uat/evidencias/nacha-m-uat/cenit/attempt_4_controlled_422_response.txt` |

## Validacion Campo a Campo

No se puede marcar OK tecnico normativo porque no existe archivo NACHA-M valido generado por el sistema en esta ejecucion.

| Registro | ACH Colombia | CENIT | Resultado |
|---|---|---|---|
| 1 | No validable, sin archivo NACHA-M no vacio | No validable, sin archivo NACHA-M no vacio | PENDIENTE |
| 5 | No validable, sin archivo NACHA-M no vacio | No validable, sin archivo NACHA-M no vacio | PENDIENTE |
| 6 | No validable, sin archivo NACHA-M no vacio | No validable, sin archivo NACHA-M no vacio | PENDIENTE |
| 7 | No validable | No validable | PENDIENTE/NO VALIDABLE |
| 8 | No validable, sin archivo NACHA-M no vacio | No validable, sin archivo NACHA-M no vacio | PENDIENTE |
| 9 | No validable, sin archivo NACHA-M no vacio | No validable, sin archivo NACHA-M no vacio | PENDIENTE |

Matrices por camara:

- `docs/uat/evidencias/nacha-m-uat/ach-colombia/matriz_campo_a_campo.md`
- `docs/uat/evidencias/nacha-m-uat/cenit/matriz_campo_a_campo.md`

## Diagnostico

- Proxy SPA/Nginx: corregido para `/NachaExport/`.
- Generador real NACHA-M: no produjo archivo valido porque las transacciones no tienen prenotificacion previa valida.
- Export API: DEF-UAT-021 corregido tecnicamente; no retorna archivo 0 bytes como exito cuando faltan prerequisitos.
- Generacion por modulo NACHA security: confirma precondicion funcional no cumplida: falta prenotificacion previa.
- Banco origen default: runtime corregido para una unica institucion `IsDefaultSource=true`: `Cooperativa Financiera de Antioquia` ID 34. `Banco UAT Origen` ID 92 queda activo pero no default.
- Documentacion normativa especifica por camara: parcial/no completa en repositorio; no permite homologacion campo-a-campo oficial.
- No hubo transmision de archivos a ACH Colombia ni CENIT.

## Defectos

| Defecto | Estado | Observacion |
|---|---|---|
| DEF-UAT-020 | Abierto / Parcial | Falta validacion campo-a-campo y homologacion/waiver; el archivo real UAT no se genero validamente. |
| DEF-UAT-021 | Cerrado tecnico | `/NachaExport/{cycleId}` responde `HTTP 422` JSON controlado si faltan prerequisitos o no hay contenido exportable; no devuelve archivo 0 bytes como exito. |
| DEF-UAT-022 | Cerrado tecnico UAT/local | `Proc_Contrapartidas` queda en `DryRun` por defecto; se genero evidencia runtime `PROC_DRY_RUN` sin transmision externa. |

## Decision

NACHA-M + validacion normativa por camara queda **PARCIAL / BLOQUEADO**. Se mantiene **NO-GO productivo**.

## Actualizacion 2026-05-19 - Parametrizacion de prenotificacion por camara

Se implemento parametrizacion administrable para reglas de prenotificacion por camara/naturaleza/tipo mediante `ClearingHouseTransactionRule`.

| Camara | Naturaleza | Regla | Fuente | Estado UAT |
|---|---|---|---|---|
| ACH Colombia | Debito | Prenotificacion obligatoria | MAN-004 V32 | Implementada por seed; pendiente reintento con prenotificacion UAT valida. |
| ACH Colombia | Credito | Prenotificacion opcional | MAN-004 V32 | Implementada por seed; no bloquea export por ausencia de prenotificacion. |
| CENIT | Debito | Prenotificacion obligatoria | DSP-152 Anexo 2 | Implementada por seed; pendiente reintento con prenotificacion UAT valida. |
| CENIT | Credito | Prenotificacion no obligatoria/opcional | DSP-152 Anexo 2 | Implementada por seed; no bloquea export por ausencia de prenotificacion. |

La validacion campo-a-campo de registros 1/5/6/7/8/9 sigue pendiente hasta generar archivos NACHA-M UAT no vacios por el sistema.

## Revalidacion 2026-05-20 - Archivos UAT no vacios

Se generaron archivos NACHA-M UAT no vacios por sistema para ambas camaras usando transacciones sinteticas. El alcance de esta evidencia es tecnico: confirma generacion, registros requeridos y no transmision externa. La homologacion normativa campo-a-campo sigue pendiente.

| Camara | Archivo | Bytes | Registros | Estado tecnico | Estado normativo |
|---|---|---:|---|---|---|
| ACH Colombia | `docs/uat/evidencias/nacha-m-uat/ach-colombia/nacha-m-uat-ach-colombia-20260520.ach` | 1060 | 1/5/6/7/8/9 presentes | OK parcial | Pendiente homologacion/waiver |
| CENIT | `docs/uat/evidencias/nacha-m-uat/cenit/nacha-m-uat-cenit-20260520.ach` | 1060 | 1/5/6/7/8/9 presentes | OK parcial | Pendiente homologacion/waiver |

Transacciones debit post-prenotificacion: pendientes. Las prenotificaciones creadas tienen fecha efectiva 2026-05-20; el sistema conserva la restriccion de 3 dias habiles y no se aplico backdating.
