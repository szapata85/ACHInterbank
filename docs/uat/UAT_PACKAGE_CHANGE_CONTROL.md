# Control cambios paquete UAT congelado - Fase 6D.11C

Productivo permanece NO-GO. Estado inicial: sin cambios aprobados.

## Reglas de control

- Todo cambio debe tener origen, impacto, aprobacion requerida y evidencia.
- Cambios funcionales o tecnicos requieren decision formal antes de ejecutarse.
- Correcciones documentales menores no deben cambiar estados de aprobacion.
- No se permiten secretos, URLs reales, certificados, thumbprints ni datos reales.

## Cuando se puede modificar

- Decision formal externa recibida.
- Observacion de comite, Seguridad, Compliance o tercero.
- Correccion de evidencia/documento.
- Cambio normativo verificado.
- Ajuste tecnico aprobado para revalidacion.

## Autorizacion requerida

| Tipo cambio | Aprobacion requerida |
| --- | --- |
| Documental menor | Mesa UAT |
| Correccion de evidencia | Mesa UAT + responsable evidencia |
| Observacion de comite | Comite/responsable asignado |
| Ajuste de seguridad | Seguridad |
| Cambio tecnico | Tecnologia + QA/UAT |
| Cambio normativo | Compliance + Operaciones |

## Registro inicial

| ID cambio | Origen | Descripcion | Impacto | Aprobacion requerida | Estado | Evidencia |
| --- | --- | --- | --- | --- | --- | --- |
| CHG-001 | Congelamiento 6D.11C | Baseline documental congelado | Control de version UAT | No aplica | Registrado | `UAT_PACKAGE_FREEZE_RECORD.md` |
| CHG-002 | Decision externa | Pendiente decision formal | Bloquea reapertura | Comite/Seguridad/Compliance | Pendiente | Por adjuntar |
| CHG-003 | Observaciones futuras | Pendiente recepcion | Puede requerir ajustes | Responsable segun observacion | Pendiente | Por adjuntar |

## Prohibiciones

No usar este control para aprobar productivo, SOAP real, movimientos monetarios, datos reales o carga de certificados/endpoints sin decision formal especifica.
