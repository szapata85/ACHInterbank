# Checklist pre-habilitacion externa ACH Colombia/CENIT - Fase 6D.6

Estados permitidos: `Listo para revision`, `Pendiente evidencia`, `Pendiente tercero`, `Pendiente Seguridad`, `Pendiente Compliance`, `Bloqueado`.

Productivo permanece NO-GO. Este checklist no autoriza ejecucion externa ni carga de secretos.

| ID | Precondicion | Responsable | Evidencia requerida | Estado | Bloqueante | Observacion |
| --- | --- | --- | --- | --- | --- | --- |
| PRE-EXT-001 | Ambiente UAT aislado definido | CFA Tecnologia | Acta/captura sanitizada | Pendiente evidencia | Si | Sin produccion |
| PRE-EXT-002 | Dataset sintetico aprobado | Mesa UAT | Declaracion dataset | Listo para revision | Si | Sin datos reales |
| PRE-EXT-003 | RACI aceptado | Mesa UAT | `EXTERNAL_UAT_RACI.md` + aceptacion | Pendiente tercero | Si | Falta aceptacion externa |
| PRE-EXT-004 | Ventanas UAT propuestas | Mesa UAT | `EXTERNAL_UAT_WINDOW_PLAN.md` | Listo para revision | No | Fechas por confirmar |
| PRE-EXT-005 | Endpoints externos pendientes de recepcion | Terceros + CFA Tecnologia | Registro placeholder | Pendiente tercero | Si | No hay URLs reales |
| PRE-EXT-006 | Certificados pendientes de recepcion | Terceros + CFA Seguridad | Registro placeholder | Pendiente tercero | Si | No hay certificados reales |
| PRE-EXT-007 | Custodia de secretos definida | CFA Seguridad | Modelo custodia | Listo para revision | Si | Falta aprobacion formal |
| PRE-EXT-008 | Canal seguro intercambio definido | CFA Seguridad | Acta canal aprobado | Pendiente Seguridad | Si | No usar canales informales |
| PRE-EXT-009 | Logging sanitizado | CFA Tecnologia | Extractos sanitizados | Pendiente evidencia | Si | Sin payload completo |
| PRE-EXT-010 | CI/Playwright evidence disponible | DevOps | Artefactos CI | Listo para revision | No | Evidencia automatizada |
| PRE-EXT-011 | Productivo NO-GO | Auditoria/Compliance | Acta/comite | Listo para revision | Si | No cambia estado |
| PRE-EXT-012 | SOAP real bloqueado | Mesa UAT | Acta bloqueo | Listo para revision | Si | No ejecutar |
| PRE-EXT-013 | Autorizacion Seguridad | CFA Seguridad | Decision formal | Pendiente Seguridad | Si | No aprobada |
| PRE-EXT-014 | Autorizacion Compliance | Compliance | Decision formal | Pendiente Compliance | Si | No aprobada |
| PRE-EXT-015 | Autorizacion terceros | ACH Colombia/CENIT | Confirmacion ventana/evidencia | Pendiente tercero | Si | No recibida |
