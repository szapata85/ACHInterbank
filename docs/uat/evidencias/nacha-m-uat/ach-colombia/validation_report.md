# Validacion NACHA-M UAT - ACH Colombia

Referencia UAT: UAT-ACHCOL-NACHA-SOAP-001
TransactionId: 3
CycleId: 2ada513804193e8aa8771252660182fdc7a55862

| Control | Resultado | Evidencia |
|---|---|---|
| Archivo generado por sistema | FALLA | /NachaExport respondio 0 bytes; nacha-security fallo por prenotificacion previa ausente |
| Archivo no vacio | FALLA | attempt_2/attempt_3 = 0 bytes |
| No HTML/JSON error | PARCIAL | intento 1 fue HTML; intentos 2/3 no fueron HTML pero vacios |
| Registros 1/5/6/8/9 | NO VALIDABLE | no hay archivo NACHA valido |
| Registro 7 | NO VALIDABLE | no hay archivo NACHA valido |
| Totales/hash/block count | NO VALIDABLE | no hay archivo NACHA valido |
| Validacion normativa camara | PENDIENTE | documentacion camara parcial/no homologada |
| Transmision externa | OK | no se transmitio archivo NACHA a camara real |

Diagnostico:
- Intento 1: la ruta /NachaExport devolvia index.html por falta de location explicito en Nginx.
- Correccion aplicada: location /NachaExport/ agregado en web/ach-interbank-ui/nginx.conf y SPA reconstruida.
- Intentos 2 y 3: la API respondio HTTP 200 con Content-Length 0.
- nacha-security/operations/nacha/generate registro falla controlada por regla: transaccion sin prenotificacion previa.
- No se aplico bypass de regla, backdating, cambio NACHA-M ni cambio ACH/CENIT.

Conclusion: archivo NACHA-M UAT no valido/no generado para esta camara. DEF-UAT-020 permanece abierto/parcial.
