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
| Runtime | `docs/uat/evidencias/soap-proc-contrapartidas/runtime_dry_run_validation.md` | N/A | Guardrail dry-run OK |

## Observaciones

- Los XML se generaron desde payloads del sistema y se sanitizaron por construccion: no contienen passwords, tokens ni certificados privados.
- No se hizo invocacion manual a endpoint SOAP.
- Antes del guardrail, el runtime registro intentos automaticos fallidos por DNS contra endpoint externo/no resoluble.
- Despues del guardrail, el runtime registro `PROC_DRY_RUN` para `UAT-SOAP-DRYRUN-001` sin `SOAP request`, sin DNS externo y sin transmision externa.

## Decision

La evidencia SOAP queda **OK para dry-run documental y guardrail tecnico UAT/local**. No cierra integracion externa ni homologacion con endpoint UAT/mock autorizado. Productivo permanece **NO-GO**.
