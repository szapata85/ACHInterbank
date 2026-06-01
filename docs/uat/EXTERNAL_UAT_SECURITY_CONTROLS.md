# Controles de seguridad UAT externo - Fase 6D.4

Productivo permanece NO-GO. No se deben registrar secretos, certificados, credenciales, payloads completos ni datos reales en el repositorio.

## Controles

| Control | Regla | Evidencia esperada | Estado |
| --- | --- | --- | --- |
| Certificados | Solo por canal seguro y con aprobacion de Seguridad | Registro de entrega sin material secreto | Pendiente |
| Endpoints | Solo endpoints UAT autorizados; no productivos | Matriz endpoints UAT sin credenciales | Pendiente |
| Credenciales | No se escriben en docs, codigo ni CI | Aprobacion Seguridad / vault externo | Pendiente |
| Secretos | Prohibido incluir secretos en evidencia | Revision Compliance | Pendiente |
| Segregacion ambiente | UAT aislado de produccion | Evidencia de ambiente/URL UAT | Pendiente |
| Datos | Solo sinteticos/anonimizados salvo autorizacion formal | Dataset aprobado | Pendiente |
| Auditoria | Logs sanitizados, sin cuentas/documentos completos | Extractos sanitizados | Pendiente |
| SOAP real | Bloqueado hasta autorizacion formal | Acta autorizacion o bloqueo explicito | Pendiente |
| NO-GO productivo | Productivo no cambia de estado | Acta/comite NO-GO | Pendiente |
| Payloads | No exponer XML/SOAP completo | Muestras sanitizadas | Pendiente |

## Paquete aprobacion 6D.5

- Paquete de aprobacion: `docs/uat/SECURITY_APPROVAL_PACKAGE.md`.
- Registro placeholder certificados/endpoints: `docs/uat/UAT_CERTIFICATE_ENDPOINT_REGISTER.md`.
- Modelo de custodia: `docs/uat/UAT_SECRET_CUSTODY_MODEL.md`.
- Checklist Seguridad: `docs/uat/UAT_SECURITY_APPROVAL_CHECKLIST.md`.
- Evidencias Seguridad/Compliance: `docs/uat/UAT_SECURITY_EVIDENCE_REQUESTS.md`.

Los controles de certificados/endpoints permanecen `Pendiente` hasta aprobacion formal. SOAP real permanece bloqueado.

## Reglas operativas

- No cargar certificados reales en git, CI o documentacion.
- No crear endpoints reales en configuracion del repo.
- No usar datos reales de clientes sin autorizacion formal.
- No ejecutar SOAP real sin ventana aprobada, control de seguridad y trazabilidad.
- No mover dinero ni generar archivos productivos reales.
