# Evidencias Consulta Prenotificaciones CFA

Fecha: 2026-05-20  
Endpoint: `GET /api/prenotifications/by-reference/{reference}`  
Tipo: consulta autenticada read-only

## Resultado

| Camara | Referencia | TransactionId | Status | StatusDescription | IsMatured | CanBeUsedForDebit | Mensaje |
|---|---|---:|---|---|---|---|---|
| ACH Colombia | `UAT-ACH-PRE-CFA-001` | 256 | Pending | Pendiente | false | false | La prenotificacion esta pendiente. Puede exportarse como prenotificacion, pero aun no habilita debito monetario posterior. |
| CENIT | `UAT-CEN-PRE-CFA-001` | 257 | Pending | Pendiente | false | false | La prenotificacion esta pendiente. Puede exportarse como prenotificacion, pero aun no habilita debito monetario posterior. |

## Archivos de evidencia

- `docs/uat/evidencias/prenotificaciones-cfa/ach-colombia/prenotification_query_response.json`
- `docs/uat/evidencias/prenotificaciones-cfa/ach-colombia/prenotification_status_metadata.json`
- `docs/uat/evidencias/prenotificaciones-cfa/cenit/prenotification_query_response.json`
- `docs/uat/evidencias/prenotificaciones-cfa/cenit/prenotification_status_metadata.json`

No contiene secretos, tokens completos, certificados ni datos reales.
