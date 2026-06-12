# Registro placeholder certificados/endpoints UAT - Fase 6D.5

Productivo permanece NO-GO. No incluir URLs reales, secretos, certificados, thumbprints reales, contrasenas ni rutas sensibles.

Estados permitidos: `Pendiente`, `Pendiente intercambio seguro`, `Recibido sin cargar`, `Validado por seguridad`, `Cargado en ambiente aislado`, `Bloqueado`, `No aplica`.

| ID | Camara/proveedor | Tipo | Ambiente | Responsable entrega | Responsable custodia | Estado | Fecha esperada | Evidencia requerida | Observacion |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| CERT-ACH-CLIENT-001 | ACH Colombia | Certificado cliente | UAT aislado | ACH Colombia | CFA Seguridad | Pendiente intercambio seguro | Por definir | Acta entrega segura sin material secreto | No cargar sin aprobacion |
| CERT-ACH-SERVER-001 | ACH Colombia | Certificado servidor | UAT aislado | ACH Colombia | CFA Seguridad | Pendiente | Por definir | Validacion cadena/caducidad sin thumbprint real | Placeholder |
| CERT-ACH-CA-001 | ACH Colombia | CA/intermedia | UAT aislado | ACH Colombia | CFA Seguridad | Pendiente | Por definir | Validacion CA por canal aprobado | Placeholder |
| END-ACH-SOAP-001 | ACH Colombia | Endpoint SOAP | UAT aislado | ACH Colombia | CFA Tecnologia | Pendiente intercambio seguro | Por definir | Matriz endpoint aprobada sin URL real | SOAP bloqueado hasta autorizacion |
| END-ACH-XFER-001 | ACH Colombia | Endpoint SFTP/transferencia si aplica | UAT aislado | ACH Colombia | CFA Tecnologia | Pendiente | Por definir | Evidencia canal transferencia aprobado | No incluir URL |
| CERT-CEN-CLIENT-001 | Banco Republica/CENIT | Certificado cliente | UAT aislado | CENIT | CFA Seguridad | Pendiente intercambio seguro | Por definir | Acta entrega segura sin material secreto | Placeholder |
| CERT-CEN-SERVER-001 | Banco Republica/CENIT | Certificado servidor | UAT aislado | CENIT | CFA Seguridad | Pendiente | Por definir | Validacion cadena/caducidad sin thumbprint real | Placeholder |
| CERT-CEN-CA-001 | Banco Republica/CENIT | CA/intermedia | UAT aislado | CENIT | CFA Seguridad | Pendiente | Por definir | Validacion CA por canal aprobado | Placeholder |
| END-CEN-SOAP-001 | Banco Republica/CENIT | Endpoint SOAP | UAT aislado | CENIT | CFA Tecnologia | Pendiente intercambio seguro | Por definir | Matriz endpoint aprobada sin URL real | SOAP bloqueado hasta autorizacion |
| END-CEN-XFER-001 | Banco Republica/CENIT | Endpoint SFTP/transferencia si aplica | UAT aislado | CENIT | CFA Tecnologia | Pendiente | Por definir | Evidencia canal transferencia aprobado | No incluir URL |
| END-CEN-QUERY-001 | Banco Republica/CENIT | Endpoint consulta/servicio si aplica | UAT aislado | CENIT | CFA Tecnologia | Pendiente | Por definir | Aprobacion endpoint consulta sin URL real | No aplica si no se requiere |

## Regla de custodia

Todo material real debe recibirse por canal aprobado, custodiarse en mecanismo corporativo de secretos o mecanismo corporativo aprobado y cargarse solo en ambiente UAT aislado tras aprobacion de Seguridad/Compliance.
