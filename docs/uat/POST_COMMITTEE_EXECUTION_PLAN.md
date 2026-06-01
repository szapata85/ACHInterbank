# Plan ejecucion posterior comite - Fase 6D.10

Productivo permanece NO-GO. Este plan se activa solo con decision formal registrada.

## Plan condicionado por decision

| Decision | Accion permitida | Restriccion |
| --- | --- | --- |
| Aprobado | Preparar intercambio controlado de parametros UAT, mantener ambiente aislado, solicitar canal seguro y validar custodia antes de recibir secretos | No cargar certificados/endpoints hasta aprobacion especifica |
| Aprobado con observaciones | Registrar observaciones, atender acciones, validar evidencia y volver al comite/responsable cuando aplique | No avanzar sobre observaciones criticas abiertas |
| Rechazado | Registrar causa, bloquear avance y preparar plan de remediacion | No intercambio, no carga, no ejecucion externa |
| Bloqueado | Mantener pausa formal hasta resolver dependencia/riesgo | No avance operativo |
| Diferido | Programar decision en fase posterior | No autorizacion nueva |
| Pendiente | Mantener espera formal | No carga/intercambio/ejecucion externa |

## Fases posteriores posibles

- Revision formal Seguridad/Compliance.
- Pre-habilitacion externa con ACH Colombia/CENIT.
- Intercambio seguro de parametros UAT.
- Preparacion ambiente UAT aislado.
- Recepcion controlada de certificados/endpoints.
- UAT externo controlado con datos sinteticos.

## Criterios de entrada

- Decision formal de comite registrada.
- Riesgos y observaciones actualizados.
- Evidencia soporte disponible y sanitizada.
- Productivo NO-GO ratificado.

## Criterios de salida

- Acciones post-comite cerradas o aceptadas formalmente.
- Evidencias de decision y observaciones registradas.
- Seguridad/Compliance sin bloqueos para el alcance aprobado.
- Sin secretos, URLs reales, certificados, thumbprints ni datos reales en repo/docs.

## Responsables

| Area | Responsabilidad |
| --- | --- |
| Comite UAT | Emitir decision y condiciones |
| Seguridad | Canal seguro, custodia, certificados/endpoints |
| Compliance/Auditoria | NO-GO, datos reales, evidencia |
| Tecnologia | Ambiente aislado y controles tecnicos |
| Operaciones/Mesa UAT | Coordinacion UAT externo y seguimiento |
| ACH Colombia/CENIT | Evidencia y parametros externos cuando aplique |

## Restricciones

No SOAP real, no movimientos monetarios reales, no datos reales, no secretos en repo, no legacy oficial, no `/NachaExport/{hash}`.
