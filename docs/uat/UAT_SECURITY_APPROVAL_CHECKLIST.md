# Checklist aprobacion Seguridad UAT - Fase 6D.5

Estados permitidos: `Pendiente`, `Aprobado`, `Aprobado con observacion`, `Bloqueado`, `No aplica`.

Productivo permanece NO-GO. Seguridad no esta aprobada hasta completar este checklist.

| ID | Control | Evidencia esperada | Responsable | Estado | Observacion |
| --- | --- | --- | --- | --- | --- |
| SEC-CHK-001 | Ambiente aislado validado | Evidencia ambiente UAT segregado | CFA Seguridad | Pendiente | Sin produccion |
| SEC-CHK-002 | Dataset sintetico validado | Aprobacion dataset sin datos reales | Mesa UAT | Pendiente | Sin clientes reales |
| SEC-CHK-003 | No datos reales | Revision evidencia/dataset | Compliance | Pendiente | Obligatorio |
| SEC-CHK-004 | No secretos en repo | Revision repo/docs/CI | CFA Seguridad | Pendiente | Sin URLs reales ni certificados |
| SEC-CHK-005 | No endpoints reales en docs | Revision documentacion | CFA Tecnologia | Pendiente | Usar placeholders |
| SEC-CHK-006 | Certificados por canal aprobado | Acta entrega sin material secreto | CFA Seguridad | Pendiente | No cargar aun |
| SEC-CHK-007 | Certificados validados por Seguridad | Validacion cadena/vigencia sin exponer valores | CFA Seguridad | Pendiente | Sin thumbprints reales en docs |
| SEC-CHK-008 | Custodia definida | OpenBao/Vault o mecanismo aprobado | CFA Seguridad | Pendiente | Acceso minimo |
| SEC-CHK-009 | Rotacion/revocacion definida | Procedimiento aprobado | Compliance | Pendiente | Incluye incidente |
| SEC-CHK-010 | Logging sanitizado | Muestra logs sin datos sensibles | CFA Tecnologia | Pendiente | Sin payload completo |
| SEC-CHK-011 | SOAP real bloqueado hasta autorizacion | Acta bloqueo/autorizacion pendiente | Mesa UAT | Pendiente | No ejecutar |
| SEC-CHK-012 | Productivo NO-GO | Acta/comite NO-GO | Auditoria/Compliance | Pendiente | No cambia estado |
| SEC-CHK-013 | Aprobacion Seguridad | Decision formal | CFA Seguridad | Pendiente | Requerida |
| SEC-CHK-014 | Aprobacion Compliance | Decision formal | Compliance | Pendiente | Requerida |
| SEC-CHK-015 | Aprobacion Tecnologia | Decision formal | CFA Tecnologia | Pendiente | Requerida |
