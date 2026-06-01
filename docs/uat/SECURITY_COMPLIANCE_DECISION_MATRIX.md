# Matriz decision Seguridad/Compliance - Fase 6D.7

Estados permitidos: `Pendiente`, `Aprobado`, `Aprobado con observaciones`, `Rechazado`, `No aplica`, `Bloqueado`.

Productivo permanece NO-GO. Estado inicial: todo `Pendiente` o `No aplica`.

| ID decision | Decision requerida | Responsable | Insumo/evidencia | Criterio de aprobacion | Estado | Observacion |
| --- | --- | --- | --- | --- | --- | --- |
| DEC-001 | Aprobar revision documental | Seguridad/Compliance | Paquete 6D.7 | Paquete completo y sin secretos | Pendiente | No aprobado |
| DEC-002 | Aprobar canal seguro de intercambio | CFA Seguridad | Gap analysis, custodia | Canal formal definido | Pendiente | Sin canal aprobado |
| DEC-003 | Aprobar custodia de secretos | CFA Seguridad | `UAT_SECRET_CUSTODY_MODEL.md` | OpenBao/Vault o mecanismo aprobado | Pendiente | Sin custodia aprobada |
| DEC-004 | Aprobar recepcion certificados UAT | CFA Seguridad | Registro placeholder | Canal y custodia aprobados | Pendiente | No recibidos |
| DEC-005 | Aprobar registro endpoints UAT sin valores en repo | Tecnologia + Seguridad | Registro placeholder | Sin URLs reales en repo/docs | Pendiente | No configurar aun |
| DEC-006 | Aprobar uso dataset sintetico | Mesa UAT + Compliance | Dataset y escenarios | Sin datos reales | Pendiente | No certificado oficial |
| DEC-007 | Aprobar ambiente aislado | Tecnologia + Seguridad | Evidencia ambiente | Segregacion verificada | Pendiente | Evidencia faltante |
| DEC-008 | Aprobar logging sanitizado | Tecnologia + Compliance | Extractos sanitizados | Sin payloads/datos sensibles | Pendiente | Evidencia faltante |
| DEC-009 | Aprobar ejecucion UAT externo controlado | Mesa UAT + Operaciones | RACI, ventanas, seguridad | Seguridad/Compliance aprobados | Pendiente | No ejecutar aun |
| DEC-010 | Mantener productivo NO-GO | Auditoria/Compliance | Acta NO-GO | NO-GO explicito | Pendiente | Anexo preparado |
| DEC-011 | Bloquear SOAP real hasta autorizacion posterior | Mesa UAT + Seguridad | Acta bloqueo | Sin SOAP real previo | Pendiente | Control requerido |
| DEC-012 | Certificacion oficial ACH/CENIT | ACH Colombia/CENIT | Evidencia oficial terceros | Certificacion formal emitida | No aplica | Fuera del alcance 6D.7 |
