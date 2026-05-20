# Evidencias NACHA-M UAT

Fecha: 2026-05-19 America/Bogota  
Ambiente: Docker Compose local, SPA `http://localhost:743`, API `http://localhost:843`.

## Resumen

Se generaron evidencias de intento de exportacion NACHA-M para ACH Colombia y CENIT con transacciones sinteticas. No se obtuvo archivo NACHA-M valido porque las transacciones no cumplen prenotificacion previa. DEF-UAT-021 fue corregido: el endpoint `/NachaExport/{cycleId}` ya no devuelve `HTTP 200` con archivo 0 bytes; devuelve `HTTP 422` JSON controlado con causa funcional.

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
| ACH Colombia | `attempt_4_controlled_422_response.txt` | Revalidacion post-fix DEF-UAT-021: `HTTP 422` JSON por prenotificacion previa ausente, sin archivo vacio. |
| ACH Colombia | `metadata.json` | Metadata UAT, intentos, bloqueo y controles. |
| ACH Colombia | `validation_report.md` | Reporte de validacion de archivo. |
| ACH Colombia | `matriz_campo_a_campo.md` | Matriz por registro, marcada no validable/falla por ausencia de archivo. |
| CENIT | `attempt_1_proxy_html_response.html` | Evidencia de fallback Angular previo a correccion de proxy. |
| CENIT | `attempt_2_zero_byte_response.txt` | Evidencia de respuesta vacia posterior. |
| CENIT | `attempt_3_export_response.txt` / `_headers.txt` | Reintento final, `HTTP 200` con `Content-Length: 0`. |
| CENIT | `attempt_4_controlled_422_response.txt` | Revalidacion post-fix DEF-UAT-021: `HTTP 422` JSON por prenotificacion previa ausente, sin archivo vacio. |
| CENIT | `metadata.json` | Metadata UAT, intentos, bloqueo y controles. |
| CENIT | `validation_report.md` | Reporte de validacion de archivo. |
| CENIT | `matriz_campo_a_campo.md` | Matriz por registro, marcada no validable/falla por ausencia de archivo. |

## Controles De Seguridad

- No se usaron datos reales.
- No se incluyeron passwords, tokens ni certificados privados.
- No se transmitieron archivos a ACH Colombia ni CENIT.
- No se generaron instrucciones productivas de pago.
- No se modificaron reglas ACH/NACHA-M/CENIT/ROR.
- Banco origen default runtime validado: `Cooperativa Financiera de Antioquia` ID 34 es la unica institucion activa con `IsDefaultSource=true`.

## Conclusion

La evidencia es suficiente para diagnosticar el bloqueo, pero no para cerrar DEF-UAT-020. Productivo permanece **NO-GO**.

## Evidencia 2026-05-19 - Reglas parametrizadas

Se agrego soporte tecnico para que NACHA Export consulte reglas parametrizadas de prenotificacion:

- Entidad EF: `ClearingHouseTransactionRule`.
- Migracion: `AddClearingHouseTransactionRules`.
- API: `/api/clearing-house-transaction-rules`.
- Preview: `/api/transaction-prerequisite-policy/preview`.
- SPA: `/transactions/clearing-house-rules`.
- Tests focalizados: `TransactionPrerequisitePolicyServiceTests`, `RegulatoryCatalogSeederTests`.

Resultado: la ausencia de regla o de prenotificacion obligatoria se trata como prerequisito funcional controlado; no se habilito bypass ni backdating.
