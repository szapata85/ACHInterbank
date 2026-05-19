# UAT SOAP Proc_Contrapartidas

Fecha: 2026-05-19 America/Bogota  
Ambiente: Docker Compose local.

## Objetivo

Generar evidencia tecnica del envelope XML SOAP para `Proc_Contrapartidas` en modo dry-run/evidencia, sin invocar manualmente endpoint productivo ni transmitir a camaras reales.

## Resultado Ejecutivo

Se obtuvo el body `Proc_Contrapartidas` generado por el sistema en `ContrapartidaDispatchBatches.RequestPayloadXml` para las transacciones sinteticas de ACH Colombia y CENIT. Se construyo el envelope SOAP con el formato del cliente real `WscfaachSoapClient`, se valido XML bien formado y se calcularon hashes SHA256.

No se hizo invocacion manual SOAP. El runtime registro intentos automaticos del job `ContrapartidaDispatchJobService` hacia un endpoint externo/no resoluble, fallando por DNS. No hay evidencia de transmision exitosa, pero se registra brecha de configuracion UAT/mock.

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

## Brecha

DEF-UAT-022 queda abierto: el job automatico de contrapartidas debe operar con endpoint UAT/mock autorizado o modo dry-run deshabilitado para transmision externa durante UAT controlado.

Productivo permanece **NO-GO**.
