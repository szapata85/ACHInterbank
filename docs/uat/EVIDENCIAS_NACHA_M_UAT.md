# Evidencias NACHA-M UAT

Fecha: 2026-05-19 America/Bogota  
Ambiente: Docker Compose local, SPA `http://localhost:743`, API `http://localhost:843`.

## Resumen

Se generaron evidencias de intento de exportacion NACHA-M para ACH Colombia y CENIT con transacciones sinteticas. No se obtuvo archivo NACHA-M valido: los archivos finales quedaron en 0 bytes y el modulo NACHA security registro falla por prenotificacion previa ausente.

## Evidencias Por Camara

| Camara | Carpeta | Estado archivo | Hash archivo valido | Resultado |
|---|---|---|---|---|
| ACH Colombia | `docs/uat/evidencias/nacha-m-uat/ach-colombia/` | No generado validamente | N/A | FAIL/BLOQUEADO |
| CENIT | `docs/uat/evidencias/nacha-m-uat/cenit/` | No generado validamente | N/A | FAIL/BLOQUEADO |

## Archivos De Evidencia

| Camara | Archivo | Proposito |
|---|---|---|
| ACH Colombia | `attempt_1_proxy_html_response.html` | Evidencia de fallback Angular previo a correccion de proxy. |
| ACH Colombia | `attempt_2_zero_byte_response.txt` | Evidencia de respuesta vacia posterior. |
| ACH Colombia | `attempt_3_export_response.txt` / `_headers.txt` | Reintento final, `HTTP 200` con `Content-Length: 0`. |
| ACH Colombia | `metadata.json` | Metadata UAT, intentos, bloqueo y controles. |
| ACH Colombia | `validation_report.md` | Reporte de validacion de archivo. |
| ACH Colombia | `matriz_campo_a_campo.md` | Matriz por registro, marcada no validable/falla por ausencia de archivo. |
| CENIT | `attempt_1_proxy_html_response.html` | Evidencia de fallback Angular previo a correccion de proxy. |
| CENIT | `attempt_2_zero_byte_response.txt` | Evidencia de respuesta vacia posterior. |
| CENIT | `attempt_3_export_response.txt` / `_headers.txt` | Reintento final, `HTTP 200` con `Content-Length: 0`. |
| CENIT | `metadata.json` | Metadata UAT, intentos, bloqueo y controles. |
| CENIT | `validation_report.md` | Reporte de validacion de archivo. |
| CENIT | `matriz_campo_a_campo.md` | Matriz por registro, marcada no validable/falla por ausencia de archivo. |

## Controles De Seguridad

- No se usaron datos reales.
- No se incluyeron passwords, tokens ni certificados privados.
- No se transmitieron archivos a ACH Colombia ni CENIT.
- No se generaron instrucciones productivas de pago.
- No se modificaron reglas ACH/NACHA-M/CENIT/ROR.

## Conclusion

La evidencia es suficiente para diagnosticar el bloqueo, pero no para cerrar DEF-UAT-020. Productivo permanece **NO-GO**.
