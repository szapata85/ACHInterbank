# Matriz decision Seguridad/Compliance - Fase 6D.7

Estados permitidos: `Pendiente`, `Aprobado`, `Aprobado con observaciones`, `Rechazado`, `No aplica`, `Bloqueado`.

Productivo permanece NO-GO. Estado inicial: todo `Pendiente` o `No aplica`.

Registro formal asociado: `docs/uat/SECURITY_COMPLIANCE_DECISION_RECORD.md`.
Solicitud ejecutiva asociada: `docs/uat/EXECUTIVE_DECISION_REQUEST.md`.
Decision comite asociada: `docs/uat/EXECUTIVE_COMMITTEE_DECISION_RECORD.md`.

| ID decision | Decision requerida | Responsable | Insumo/evidencia | Criterio de aprobacion | Estado | Observacion |
| --- | --- | --- | --- | --- | --- | --- |
| DEC-001 | Aprobar revision documental | Seguridad/Compliance | Paquete 6D.7 | Paquete completo y sin secretos | Pendiente | No aprobado |
| DEC-002 | Aprobar canal seguro de intercambio | CFA Seguridad | Gap analysis, custodia | Canal formal definido | Pendiente | Sin canal aprobado |
| DEC-003 | Aprobar custodia de secretos | CFA Seguridad | `UAT_SECRET_CUSTODY_MODEL.md` | mecanismo corporativo de secretos o mecanismo aprobado | Pendiente | Sin custodia aprobada |
| DEC-004 | Aprobar recepcion certificados UAT | CFA Seguridad | Registro placeholder | Canal y custodia aprobados | Pendiente | No recibidos |
| DEC-005 | Aprobar registro endpoints UAT sin valores en repo | Tecnologia + Seguridad | Registro placeholder | Sin URLs reales en repo/docs | Pendiente | No configurar aun |
| DEC-006 | Aprobar uso dataset sintetico | Mesa UAT + Compliance | Dataset y escenarios | Sin datos reales | Pendiente | No certificado oficial |
| DEC-007 | Aprobar ambiente aislado | Tecnologia + Seguridad | Evidencia ambiente | Segregacion verificada | Pendiente | Evidencia faltante |
| DEC-008 | Aprobar logging sanitizado | Tecnologia + Compliance | Extractos sanitizados | Sin payloads/datos sensibles | Pendiente | Evidencia faltante |
| DEC-009 | Aprobar ejecucion UAT externo controlado | Mesa UAT + Operaciones | RACI, ventanas, seguridad | Seguridad/Compliance aprobados | Pendiente | No ejecutar aun |
| DEC-010 | Mantener productivo NO-GO | Auditoria/Compliance | Acta NO-GO | NO-GO explicito | Pendiente | Anexo preparado |
| DEC-011 | Bloquear SOAP real hasta autorizacion posterior | Mesa UAT + Seguridad | Acta bloqueo | Sin SOAP real previo | Pendiente | Control requerido |
| DEC-012 | Certificacion oficial ACH/CENIT | ACH Colombia/CENIT | Evidencia oficial terceros | Certificacion formal emitida | No aplica | Fuera del alcance 6D.7 |

## Nota 6D.8

La Fase 6D.8 agrega registro de decision y plan de observaciones. Sin evidencia formal, DEC-001 a DEC-011 permanecen `Pendiente` y DEC-012 permanece `No aplica`.

## Nota 6D.9

El paquete ejecutivo 6D.9 referencia esta matriz para solicitar decision de comite. No cambia estados: DEC-001 a DEC-011 siguen `Pendiente` y DEC-012 sigue `No aplica`.

## Nota 6D.10

La decision de comite sigue pendiente/no recibida. Esta matriz no cambia estados sin evidencia formal: DEC-001 a DEC-011 siguen `Pendiente` y DEC-012 sigue `No aplica`.
