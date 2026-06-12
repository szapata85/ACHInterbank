# Checklist aprobacion Seguridad UAT - Fase 6D.5

Estados permitidos: `Pendiente`, `Aprobado`, `Aprobado con observacion`, `Bloqueado`, `No aplica`.

Productivo permanece NO-GO. Seguridad no esta aprobada hasta completar este checklist.

| ID | Control | Evidencia esperada | Responsable | Estado | Observacion | Simulacion 6D.6 |
| --- | --- | --- | --- | --- | --- | --- |
| SEC-CHK-001 | Ambiente aislado validado | Evidencia ambiente UAT segregado | CFA Seguridad | Pendiente | Sin produccion | Pendiente evidencia |
| SEC-CHK-002 | Dataset sintetico validado | Aprobacion dataset sin datos reales | Mesa UAT | Pendiente | Sin clientes reales | Listo para revision |
| SEC-CHK-003 | No datos reales | Revision evidencia/dataset | Compliance | Pendiente | Obligatorio | Pendiente Compliance |
| SEC-CHK-004 | No secretos en repo | Revision repo/docs/CI | CFA Seguridad | Pendiente | Sin URLs reales ni certificados | Pendiente Seguridad |
| SEC-CHK-005 | No endpoints reales en docs | Revision documentacion | CFA Tecnologia | Pendiente | Usar placeholders | Listo para revision |
| SEC-CHK-006 | Certificados por canal aprobado | Acta entrega sin material secreto | CFA Seguridad | Pendiente | No cargar aun | Pendiente tercero |
| SEC-CHK-007 | Certificados validados por Seguridad | Validacion cadena/vigencia sin exponer valores | CFA Seguridad | Pendiente | Sin thumbprints reales en docs | Pendiente tercero |
| SEC-CHK-008 | Custodia definida | mecanismo corporativo de secretos o mecanismo aprobado | CFA Seguridad | Pendiente | Acceso minimo | Listo para revision |
| SEC-CHK-009 | Rotacion/revocacion definida | Procedimiento aprobado | Compliance | Pendiente | Incluye incidente | Listo para revision |
| SEC-CHK-010 | Logging sanitizado | Muestra logs sin datos sensibles | CFA Tecnologia | Pendiente | Sin payload completo | Pendiente evidencia |
| SEC-CHK-011 | SOAP real bloqueado hasta autorizacion | Acta bloqueo/autorizacion pendiente | Mesa UAT | Pendiente | No ejecutar | Listo para revision |
| SEC-CHK-012 | Productivo NO-GO | Acta/comite NO-GO | Auditoria/Compliance | Pendiente | No cambia estado | Listo para revision |
| SEC-CHK-013 | Aprobacion Seguridad | Decision formal | CFA Seguridad | Pendiente | Requerida | Pendiente Seguridad |
| SEC-CHK-014 | Aprobacion Compliance | Decision formal | Compliance | Pendiente | Requerida | Pendiente Compliance |
| SEC-CHK-015 | Aprobacion Tecnologia | Decision formal | CFA Tecnologia | Pendiente | Requerida | Pendiente evidencia |

## Nota 6D.6

La columna `Simulacion 6D.6` clasifica preparacion documental. No equivale a aprobacion formal.

## Nota 6D.7

Solicitud formal preparada en `SECURITY_COMPLIANCE_REVIEW_REQUEST.md`. Esto no cambia ningun estado a `Aprobado`; Seguridad/Compliance siguen pendientes.

## Nota 6D.8

El registro de decision y el plan de respuesta a observaciones quedan documentados en `SECURITY_COMPLIANCE_DECISION_RECORD.md` y `SECURITY_COMPLIANCE_OBSERVATION_RESPONSE_PLAN.md`. Esto no cambia ningun control a `Aprobado`; las aprobaciones Seguridad/Compliance/Tecnologia siguen pendientes.
