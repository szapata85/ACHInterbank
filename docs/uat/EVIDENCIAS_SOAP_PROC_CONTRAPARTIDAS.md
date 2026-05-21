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

## Auditoria settings vs mappings - 2026-05-21

`Proc_Contrapartidas` queda clasificado como `MonetaryDebitRequest`: debito monetario originado por CFA. El flujo usa `soap-settings` para endpoint/SOAP Action y exige `IntegrationMappingSet` publicado mediante `ProcContrapartidasFunctionalMappingResolver`.

El hallazgo `DEF-UAT-SOAP-MAP-001` queda cerrado tecnicamente: si no existe mapping publicado activo para campos requeridos, `ProcContrapartidasRequestMapper` falla controladamente con `INTEGRATION_MAPPING_REQUIRED` y el dispatch bloquea cualquier resolucion `UsedFallback=true` con `REQUIRED_MAPPING_USES_FALLBACK` antes de construir XML, DryRun o dispatch.

Evidencia agregada:

- `docs/uat/evidencias/soap-integrations/mapping-trace/proc_contrapartidas/mapping_trace.md`
- `docs/uat/evidencias/soap-integrations/mapping-trace/proc_contrapartidas/mapping_trace.json`
- `docs/uat/evidencias/soap-integrations/mapping-trace/proc_contrapartidas/envelope_or_payload_sanitizado.xml`
