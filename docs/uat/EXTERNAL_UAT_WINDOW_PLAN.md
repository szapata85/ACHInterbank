# Plan de ventanas UAT externo ACH Colombia/CENIT - Fase 6D.4

Productivo permanece NO-GO. Las ventanas propuestas son de coordinacion; no implican ejecucion real hasta aprobacion formal.

| Ventana | Objetivo | Prerequisitos | Actividades | Evidencias | Responsable | Estado inicial |
| --- | --- | --- | --- | --- | --- | --- |
| W-001 | Kickoff tecnico externo | RACI aceptado, paquete enviado | Revisar alcance, restricciones, canales y contactos | Acta kickoff, asistentes, acuerdos | Mesa UAT | Pendiente |
| W-002 | Parametros y seguridad | Seguridad aprobada, canales seguros | Intercambiar parametros, endpoints UAT y certificados sin secretos en repo | Matriz parametros, aprobacion seguridad | CFA Seguridad | Pendiente |
| W-003 | ACH Colombia formato/salida | Dataset ACH cargado, perfil publicado | Validar salida NACHA-M, naming, totales y `.RET` | Acuse/evidencia ACH Colombia | CFA Operaciones | Pendiente |
| W-004 | ACH Colombia entrada/respuestas | Archivo entrante sintetico disponible | Validar entrada, prenotes, rechazos/devoluciones y conciliacion | Evidencia conciliacion, causales | Mesa UAT | Pendiente |
| W-005 | CENIT formato/ciclo | Dataset CENIT cargado, ventana acordada | Validar salida/entrada, ciclo/cola/neteo sintetico | Evidencia CENIT, logs sanitizados | Operaciones CENIT | Pendiente |
| W-006 | CENIT `.RET`/causales/ROR | Anexos A/B revisados | Validar `.RET`, causales y ROR | Evidencia causal/ROR | Mesa UAT | Pendiente |
| W-007 | Cierre UAT externo | Evidencias completas o defectos abiertos | Consolidar defectos, riesgos y decision UAT | Acta cierre UAT externo | Auditoria/Compliance | Pendiente |

## Criterios de suspension

- Solicitud de usar datos reales no autorizados.
- SOAP real sin aprobacion formal.
- Movimiento monetario real o riesgo de ejecucion monetaria.
- Secreto/certificado expuesto fuera de canal seguro.
- Uso de legacy como fuente oficial o `/NachaExport/{hash}`.
- Evidencia de mutacion critica desde consolas read-only.

## Rollback / no avance

No hay rollback productivo porque produccion no se habilita. Ante suspension, conservar evidencia sanitizada, abrir hallazgo, bloquear la ventana y mantener Productivo NO-GO.
