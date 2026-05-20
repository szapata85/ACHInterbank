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

## Evidencia 2026-05-20 - Runtime UAT con reglas aplicadas

Se revalido runtime Docker despues de aplicar la migracion `AddClearingHouseRulesMenuAndRuntimeSeeds` y correcciones acotadas de prerequisitos NACHA.

| Camara | Prenotificacion UAT | Transaccion credito UAT | Ciclo | Resultado export | Archivo | SHA256 |
|---|---:|---:|---|---|---|---|
| ACH Colombia | 246 (`UAT-ACH-PRE-001`) | 248 (`UAT-ACH-CRED-001`) | `7301fd9bf4c1bd7383399cd9d844fd1ccbd97649` | HTTP 200, 1060 bytes, 10 registros | `docs/uat/evidencias/nacha-m-uat/ach-colombia/nacha-m-uat-ach-colombia-20260520.ach` | `8EA137CBDCEA6CC4280E5183A66FD29983FE0BF0D4F42732A477AC18DD211844` |
| CENIT | 247 (`UAT-CEN-PRE-001`) | 249 (`UAT-CEN-CRED-001`) | `52933d1ba0406af3e64800e809c5e73bab36dddd` | HTTP 200, 1060 bytes, 10 registros | `docs/uat/evidencias/nacha-m-uat/cenit/nacha-m-uat-cenit-20260520.ach` | `248205FCE69769B8047FEED94346E2E9910918B386D553BC46D6F1218B3D125C` |

Registros detectados en ambos archivos: 1:1, 5:1, 6:2, 7:2, 8:1, 9:3. Los archivos fueron generados por `/NachaExport/{cycleId}`, no manualmente, y no se transmitieron a camaras externas.

Limitacion controlada: no se creo transaccion debito monetaria post-prenotificacion porque la prenotificacion efectiva es 2026-05-20 y la regla exige 3 dias habiles. Crear un debito exportable en la misma sesion requeriria backdating o esperar la ventana normativa; no se hizo bypass.

## Actualizacion 2026-05-20 - DEF-UAT-020 nomenclatura y NACHA-M UAT

Estado productivo: NO-GO.

Resultado del ciclo controlado:

| Camara | Archivo generado | SHA256 | ZZZ | Campo 7 registro 1 | Registros | Resultado |
|---|---|---|---:|---|---|---|
| ACH Colombia | docs/uat/evidencias/nacha-m-uat/ach-colombia/0001283.002.1 | E4DAEEE551596D067357953C552CD521871F635F6703D27700171EBC10A0026E | 002 | B | 1/5/6/7/8/9 | OK tecnico UAT |
| CENIT | docs/uat/evidencias/nacha-m-uat/cenit/0001283.001.1 | FD52F7834ADEC53C720E4A877B1D48A8AC15B149BEB7FAFB91EC57CF1B88FCD4 | 001 | A | 1/5/6/7/8/9 | OK tecnico UAT; homologacion normativa formal pendiente |

Evidencia comun:

- Patron aplicado: RRRRTTT.ZZZ.1.
- Originador: Cooperativa Financiera de Antioquia, unico FinancialInstitution.IsDefaultSource=true.
- RRRR=0001 y TTT=283 derivados de la configuracion de CFA.
- Mapeo validado: 001 -> A y 002 -> B en registro tipo 1 campo 7.
- Archivos generados por /NachaExport/{cycleId}; no fueron creados manualmente.
- Sin transmision externa a ACH Colombia o CENIT.
- Proc_Contrapartidas permanece en DryRun para UAT/local.

Observacion normativa:

- ACH Colombia se valida contra MAN-004 V32.
- CENIT se valida tecnicamente con ejemplos disponibles en el proyecto y queda pendiente homologacion normativa formal.
