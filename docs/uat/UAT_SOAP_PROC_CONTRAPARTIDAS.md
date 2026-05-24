# UAT SOAP Proc_Contrapartidas

Fecha: 2026-05-19 America/Bogota  
Ambiente: Docker Compose local.

## Objetivo

Generar evidencia tecnica del envelope XML SOAP para `Proc_Contrapartidas` en modo dry-run/evidencia, sin invocar manualmente endpoint productivo ni transmitir a camaras reales.

## Resultado Ejecutivo

Se obtuvo el body `Proc_Contrapartidas` generado por el sistema en `ContrapartidaDispatchBatches.RequestPayloadXml` para las transacciones sinteticas de ACH Colombia y CENIT. Se construyo el envelope SOAP con el formato del cliente real `WscfaachSoapClient`, se valido XML bien formado y se calcularon hashes SHA256.

No se hizo invocacion manual SOAP. Tras DEF-UAT-022, el runtime UAT/local queda con guardrail `ProcContrapartidas:Mode=DryRun` por defecto. Se valido una prenotificacion sintetica `UAT-SOAP-DRYRUN-001` (TransactionId `245`): el job registro `PROC_DRY_RUN`, `RetryEligible=false` y el mensaje `Proc_Contrapartidas dry-run: envelope generado, no transmitido.` No se observo `SOAP request`, error DNS externo ni transmision a endpoint productivo en la ventana validada.

## Evidencia

| Camara | TransactionId | Referencia | XML sanitizado | Estado |
|---|---:|---|---|---|
| ACH Colombia | 3 | `UAT-ACHCOL-NACHA-SOAP-001` | `docs/uat/evidencias/soap-proc-contrapartidas/ach-colombia/proc_contrapartidas_envelope_sanitizado.xml` | OK dry-run |
| CENIT | 4 | `UAT-CENIT-NACHA-SOAP-001` | `docs/uat/evidencias/soap-proc-contrapartidas/cenit/proc_contrapartidas_envelope_sanitizado.xml` | OK dry-run |

## Controles

| Control | Resultado |
|---|---|
| XML bien formado | OK |
| Operacion `Proc_Contrapartidas` presente | OK |
| Credenciales/tokens/certificados privados | No presentes |
| Invocacion manual a endpoint SOAP | No |
| Transmision productiva exitosa | No |
| Endpoint UAT/mock autorizado | No disponible / no evidenciado |
| Guardrail UAT/local `DryRun` | OK |
| Runtime sin transmision externa en dry-run | OK |

## Brecha

DEF-UAT-022 queda cerrado tecnicamente para UAT/local: el job automatico de contrapartidas opera en modo dry-run salvo configuracion explicita `Live`. La integracion real con endpoint UAT/mock autorizado sigue pendiente para homologacion externa.

Productivo permanece **NO-GO**.
