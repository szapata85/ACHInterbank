# Solicitud decision ejecutiva - Fase 6D.9

Productivo permanece NO-GO. Estados iniciales permitidos: `Pendiente`, `No aplica`. Ninguna decision inicia aprobada.

| ID | Decision solicitada | Responsable sugerido | Evidencia soporte | Criterio decision | Impacto si se aprueba | Impacto si se rechaza | Restricciones | Estado inicial |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| EXD-001 | Autorizar revision formal Seguridad/Compliance | Comite UAT | `SECURITY_COMPLIANCE_REVIEW_REQUEST.md` | Paquete completo, sin secretos | Permite revision formal | Requiere ajustar paquete | No habilita productivo | Pendiente |
| EXD-002 | Autorizar coordinacion UAT externo ACH Colombia/CENIT | Operaciones + Mesa UAT | `EXTERNAL_APPROVAL_PACKAGE_INDEX.md` | RACI y ventanas propuestos | Permite agenda externa | Se mantiene UAT interno | Sin ejecucion externa sin seguridad | Pendiente |
| EXD-003 | Autorizar intercambio controlado parametros UAT | Seguridad | `UAT_SECRET_CUSTODY_MODEL.md` | Canal seguro aprobado | Permite intercambio controlado | Bloquea parametros externos | Sin secretos fuera de canal aprobado | Pendiente |
| EXD-004 | Autorizar preparacion ambiente aislado | Tecnologia + Seguridad | `PRE_UAT_TECHNICAL_HARDENING.md` | Segregacion verificable | Permite evidencia ambiente | Bloquea pre-habilitacion | Sin produccion | Pendiente |
| EXD-005 | Autorizar recepcion controlada certificados/endpoints UAT | Seguridad + Tecnologia | `UAT_CERTIFICATE_ENDPOINT_REGISTER.md` | Custodia y canal aprobados | Permite recepcion sin carga automatica | Bloquea pruebas integradas | Sin URLs/secretos en repo | Pendiente |
| EXD-006 | Mantener Productivo NO-GO | Auditoria/Compliance | `PRODUCTIVE_NO_GO_ATTESTATION.md` | NO-GO explicito | Evita interpretacion productiva | Riesgo de alcance ambiguo | No cambia a GO | Pendiente |
| EXD-007 | Mantener SOAP real bloqueado hasta fase posterior | Seguridad + Mesa UAT | `SECURITY_COMPLIANCE_DECISION_MATRIX.md` | Bloqueo ratificado | Reduce riesgo operativo | Riesgo de ejecucion no autorizada | Sin SOAP real | Pendiente |
| EXD-008 | Mantener datos reales prohibidos | Compliance | `UAT_RISKS_AND_GAPS.md` | Dataset sintetico validado | Reduce riesgo privacidad | Bloquea si se requieren datos reales | Sin datos reales | Pendiente |
| EXD-009 | Aprobar continuidad hacia UAT externo condicionado | Comite UAT | Paquete 6D.9 | Riesgos aceptados y pendientes visibles | Permite siguiente fase documental/externa | Mantiene pausa UAT externo | No certifica oficialmente | Pendiente |

## Resultado esperado

Registrar decision ejecutiva y observaciones sin inventar aprobaciones. Cualquier aprobacion debe quedar respaldada por acta o evidencia formal.
