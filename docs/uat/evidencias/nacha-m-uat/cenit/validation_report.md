# Validacion NACHA-M UAT - CENIT

Referencia UAT: UAT-CENIT-NACHA-SOAP-001
TransactionId: 4
CycleId: 7c0c26327f06a20d751ef72fc379ca6fe7166803

| Control | Resultado | Evidencia |
|---|---|---|
| Archivo generado por sistema | FALLA CONTROLADA | /NachaExport responde 422 por prenotificacion previa ausente; no genera archivo vacio como exito |
| Archivo no vacio | FALLA | No hay archivo exportable hasta cumplir prenotificacion UAT valida |
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
