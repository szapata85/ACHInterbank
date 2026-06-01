# RACI UAT externo ACH Colombia/CENIT - Fase 6D.4

Productivo permanece NO-GO. RACI para coordinacion externa; no autoriza SOAP real ni movimientos monetarios.

Roles: R=Responsable, A=Aprueba, C=Consultado, I=Informado.

| Actividad | CFA Tecnologia | CFA Seguridad | CFA Operaciones | ACH Colombia | Banco Republica/CENIT | Proveedor/core/SOAP | Auditoria/Compliance | Mesa UAT |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Preparacion ambiente UAT aislado | R | C | C | I | I | C | C | A |
| Intercambio de parametros | R | C | A | C | C | C | I | I |
| Certificados | C | A/R | I | C | C | C | I | I |
| Endpoints UAT | R | A/C | I | C | C | C | I | I |
| Credenciales UAT | C | A/R | I | C | C | C | I | I |
| Carga dataset sintetico | R | C | A | I | I | C | I | C |
| Ejecucion archivos salida | R | I | A | C | C | C | I | R |
| Recepcion archivos entrada | R | I | A | C | C | C | I | R |
| Validacion `.RET` | C | I | R | C | C | I | C | R |
| Conciliacion | R | I | A | C | C | C | C | R |
| Evidencias | R | C | C | C | C | C | A | R |
| Aprobacion UAT | C | C | R | C | C | I | A | R |
| Gestion de defectos | R | C | A | C | C | C | I | R |
| Decision No-Go/Go UAT | C | C | A | C | C | I | A | R |
| Productivo NO-GO | A/R | A/R | A/R | I | I | I | A/R | I |

## Nota

La decision Go/No-Go UAT no equivale a decision productiva. Productivo permanece NO-GO hasta certificacion oficial, integracion controlada autorizada y comite productivo.
