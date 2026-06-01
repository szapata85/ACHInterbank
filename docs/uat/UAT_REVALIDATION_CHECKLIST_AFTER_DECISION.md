# Checklist revalidacion posterior a decision - Fase 6D.11C

Productivo permanece NO-GO. Este checklist se ejecuta solo cuando llegue decision formal.

Estados sugeridos: `Pendiente`, `Validado`, `Observado`, `Bloqueado`, `No aplica`.

| ID | Control | Responsable sugerido | Estado inicial | Observacion |
| --- | --- | --- | --- | --- |
| REV-001 | Validar decision formal recibida | Mesa UAT | Pendiente | Requiere evidencia formal |
| REV-002 | Validar alcance autorizado | Comite/Mesa UAT | Pendiente | No asumir alcance implicito |
| REV-003 | Validar Productivo NO-GO | Compliance/Auditoria | Pendiente | No cambia a GO |
| REV-004 | Validar observaciones | Mesa UAT | Pendiente | Usar plan de observaciones |
| REV-005 | Validar autorizacion intercambio externo | Seguridad/Operaciones | Pendiente | Solo canal aprobado |
| REV-006 | Validar recepcion certificados/endpoints | Seguridad/Tecnologia | Pendiente | No cargar sin aprobacion especifica |
| REV-007 | Validar SOAP real bloqueado | Seguridad/Mesa UAT | Pendiente | Fase posterior separada |
| REV-008 | Validar evidencia requerida | QA/UAT | Pendiente | Sin datos sensibles |
| REV-009 | Validar riesgos actualizados | Mesa UAT | Pendiente | Actualizar `UAT_RISKS_AND_GAPS.md` |
| REV-010 | Validar necesidad de build/tests/e2e | QA/DevOps | Pendiente | Solo si hay cambios codigo/workflow/package |
| REV-011 | Validar contexto actualizado | Mesa UAT | Pendiente | `ACH_PHASE6_CONTEXT.md` |
| REV-012 | Validar no secretos en repo | Seguridad | Pendiente | Sin URLs/certs/thumbprints |
| REV-013 | Validar no datos reales no autorizados | Compliance | Pendiente | Solo datos sinteticos/autorizados |

## Resultado esperado

Ningun avance operativo debe iniciar hasta cerrar o aceptar formalmente los controles aplicables.
