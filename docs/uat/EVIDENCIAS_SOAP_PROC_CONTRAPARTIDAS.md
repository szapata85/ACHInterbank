# Evidencias SOAP Proc_Contrapartidas

Fecha: 2026-05-19 America/Bogota.

## Archivos

| Camara | Archivo | Hash SHA256 | Estado |
|---|---|---|---|
| ACH Colombia | `docs/uat/evidencias/soap-proc-contrapartidas/ach-colombia/proc_contrapartidas_envelope_sanitizado.xml` | Ver `proc_contrapartidas_envelope_metadata.json` | XML OK |
| ACH Colombia | `docs/uat/evidencias/soap-proc-contrapartidas/ach-colombia/proc_contrapartidas_envelope_metadata.json` | N/A | Metadata OK |
| ACH Colombia | `docs/uat/evidencias/soap-proc-contrapartidas/ach-colombia/proc_contrapartidas_validation_report.md` | N/A | Reporte OK |
| CENIT | `docs/uat/evidencias/soap-proc-contrapartidas/cenit/proc_contrapartidas_envelope_sanitizado.xml` | Ver `proc_contrapartidas_envelope_metadata.json` | XML OK |
| CENIT | `docs/uat/evidencias/soap-proc-contrapartidas/cenit/proc_contrapartidas_envelope_metadata.json` | N/A | Metadata OK |
| CENIT | `docs/uat/evidencias/soap-proc-contrapartidas/cenit/proc_contrapartidas_validation_report.md` | N/A | Reporte OK |

## Observaciones

- Los XML se generaron desde payloads del sistema y se sanitizaron por construccion: no contienen passwords, tokens ni certificados privados.
- No se hizo invocacion manual a endpoint SOAP.
- El runtime registro intentos automaticos fallidos por DNS contra endpoint externo/no resoluble. Se documenta como brecha de configuracion UAT/mock.
- No hay evidencia de transmision externa exitosa.

## Decision

La evidencia SOAP queda **OK para dry-run documental**, pero no cierra integracion externa ni homologacion. Productivo permanece **NO-GO**.
