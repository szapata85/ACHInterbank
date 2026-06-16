# Simulacion aprobacion Seguridad/UAT - Fase 6D.6

Productivo permanece NO-GO. Esta simulacion no otorga aprobacion formal, no carga secretos, certificados ni endpoints reales y no habilita SOAP real.

## Resumen ejecutivo

Se simulo documentalmente la revision de controles Seguridad/Compliance para preparar una solicitud formal de pre-habilitacion externa ACH Colombia/CENIT. El paquete esta listo para solicitar revision, pero no para cargar secretos/certificados ni ejecutar UAT externo.

## Objetivo de la simulacion

Clasificar controles y evidencias por estado de preparacion, brecha y responsable antes de pedir aprobacion formal de Seguridad/Compliance.

## Alcance

- Checklist Seguridad 6D.5.
- Registro placeholder de certificados/endpoints.
- Custodia de secretos.
- Evidencias externas y de seguridad.
- Pre-habilitacion externa ACH Colombia/CENIT.

## Exclusiones

- Aprobacion real de Seguridad o Compliance.
- Recepcion/carga de certificados, endpoints, secretos o URLs reales.
- SOAP real, datos reales, movimientos monetarios, archivos productivos, legacy oficial y `/NachaExport/{hash}`.

## Resultado simulado

- Aprobacion formal: NO otorgada.
- Pre-habilitacion externa: condicionada.
- Productivo: NO-GO.

## Estado de controles

| Control | Evidencia requerida | Responsable | Estado simulado | Brecha | Accion requerida |
| --- | --- | --- | --- | --- | --- |
| Ambiente UAT aislado | Evidencia ambiente segregado | CFA Seguridad | Pendiente evidencia | Falta acta/captura sanitizada | Adjuntar evidencia ambiente |
| Dataset sintetico | Declaracion dataset | Mesa UAT | Listo para revision | Falta aprobacion formal | Firmar aprobacion dataset |
| No datos reales | Revision Compliance | Compliance | Pendiente Compliance | Falta dictamen | Revisar muestras anonimizadas |
| No secretos en repo | Revision repo/docs/CI | CFA Seguridad | Pendiente Seguridad | Falta acta revision | Ejecutar revision formal |
| No endpoints reales en docs | Revision documentacion | CFA Tecnologia | Listo para revision | Sin acta | Validar placeholders |
| Certificados por canal aprobado | Acta entrega | CFA Seguridad | Pendiente tercero | No recibidos | Definir canal con terceros |
| Certificados validados | Validacion cadena/vigencia | CFA Seguridad | Pendiente tercero | No recibidos | Validar tras recepcion segura |
| Custodia definida | mecanismo corporativo de secretos o mecanismo aprobado | CFA Seguridad | Listo para revision | Falta aprobacion formal | Aprobar custodia |
| Rotacion/revocacion | Procedimiento aprobado | Compliance | Listo para revision | Falta firma | Aprobar procedimiento |
| Logging sanitizado | Extractos sanitizados | CFA Tecnologia | Pendiente evidencia | Falta muestra formal | Adjuntar extractos |
| SOAP real bloqueado | Acta bloqueo | Mesa UAT | Listo para revision | Falta acta | Registrar bloqueo |
| Productivo NO-GO | Acta/comite | Auditoria/Compliance | Listo para revision | Falta acta formal | Adjuntar decision NO-GO |
| Aprobacion Seguridad | Decision formal | CFA Seguridad | Pendiente Seguridad | No solicitada formalmente | Enviar solicitud |
| Aprobacion Compliance | Decision formal | Compliance | Pendiente Compliance | No solicitada formalmente | Enviar solicitud |
| Aprobacion Tecnologia | Decision formal | CFA Tecnologia | Pendiente evidencia | No emitida | Consolidar paquete |

## Conclusion

Listo para solicitar revision Seguridad/Compliance. No listo para cargar secretos, certificados, endpoints reales ni ejecutar SOAP real.
