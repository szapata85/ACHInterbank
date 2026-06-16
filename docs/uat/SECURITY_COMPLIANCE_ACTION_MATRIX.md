# Matriz acciones Seguridad/Compliance - Fase 6D.8

Productivo permanece NO-GO. Acciones iniciales pendientes salvo dependencias no aplicables.

Estados iniciales permitidos: `Pendiente`, `En analisis`, `Bloqueado por tercero`, `Diferido`, `No aplica`.

| ID accion | Origen | Observacion/decision relacionada | Responsable | Tipo | Prioridad | Accion requerida | Evidencia esperada | Estado | Fase destino | Observacion |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| ACT-001 | Paquete 6D.7 | DEC-001 | Mesa UAT | Documental | Media | Registrar decision formal cuando sea recibida | `SECURITY_COMPLIANCE_DECISION_RECORD.md` actualizado | Pendiente | 6D.8 | Sin decision recibida |
| ACT-002 | Seguridad | DEC-001 / SEC-CHK-013 | CFA Seguridad | Seguridad | Alta | Emitir revision Seguridad sobre paquete | Acta o comentario formal sanitizado | Pendiente | Revision externa | No aprobado |
| ACT-003 | Compliance | DEC-001 / SEC-CHK-014 | Compliance/Auditoria | Compliance | Alta | Emitir revision Compliance sobre paquete | Acta o comentario formal sanitizado | Pendiente | Revision externa | No aprobado |
| ACT-004 | Gap analysis | DEC-002 | CFA Seguridad | Seguridad | Alta | Aprobar canal seguro de intercambio | Evidencia canal aprobado sin secretos | Pendiente | Pre-habilitacion | No usar canales informales |
| ACT-005 | Custodia | DEC-003 | CFA Seguridad | Certificados/endpoints | Alta | Aprobar custodia mecanismo corporativo de secretos o equivalente | Acta custodia aprobada | Pendiente | Pre-habilitacion | Sin carga aun |
| ACT-006 | Ambiente | DEC-007 | CFA Tecnologia | Tecnologia | Alta | Adjuntar evidencia ambiente UAT aislado | Evidencia sanitizada de segregacion | Pendiente | Pre-habilitacion | Falta evidencia |
| ACT-007 | Terceros | DEC-009/DEC-012 | ACH Colombia/CENIT | Tercero | Alta | Confirmar RACI, ventanas y evidencias oficiales | Confirmacion externa sanitizada | Bloqueado por tercero | UAT externo | No cerrar sin tercero |
| ACT-008 | Registro placeholders | DEC-004/DEC-005 | CFA Seguridad/Tecnologia | Certificados/endpoints | Critica | Mantener certificados/endpoints sin cargar hasta aprobacion | Registro placeholder sin valores reales | Pendiente | Pre-habilitacion | No URLs, no thumbprints |
| ACT-009 | NO-GO | DEC-010/DEC-011 | Auditoria/Compliance | Riesgo | Critica | Ratificar NO-GO y bloqueo SOAP real | Acta NO-GO y bloqueo SOAP | Pendiente | Revision externa | Sin autorizacion productiva |
| ACT-010 | Certificacion oficial | DEC-012 | ACH Colombia/CENIT | UAT externo | Alta | Obtener certificacion oficial en fase posterior | Evidencia oficial terceros | Bloqueado por tercero | Certificacion | Fuera de alcance 6D.8 |

## Notas

- No hay acciones aprobadas en esta matriz.
- Cualquier observacion futura debe agregarse sin borrar historial.
- Las acciones de terceros no deben cerrarse con evidencia interna.
