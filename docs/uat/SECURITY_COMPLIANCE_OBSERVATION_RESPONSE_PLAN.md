# Plan respuesta observaciones Seguridad/Compliance - Fase 6D.8

Productivo permanece NO-GO. Este plan organiza observaciones; no aprueba UAT externo ni habilita secretos.

## Objetivo

Definir el flujo para recibir, clasificar, responder y validar observaciones emitidas por Seguridad, Compliance, Tecnologia, Operaciones, terceros ACH/CENIT o Auditoria.

## Tipos de observacion

- Seguridad.
- Compliance.
- Tecnologia.
- Operaciones.
- Terceros ACH/CENIT.
- Auditoria.

## Severidades

| Severidad | Criterio |
| --- | --- |
| Critica | Bloquea avance, seguridad, privacidad, NO-GO o integridad financiera. |
| Alta | Bloquea UAT externo hasta cierre o aceptacion formal. |
| Media | Requiere correccion planificada antes o durante UAT controlado. |
| Baja | Mejora documental o evidencia complementaria. |
| Observacion | Comentario sin bloqueo inmediato. |

## Estados

| Estado | Uso |
| --- | --- |
| Recibida | Observacion registrada sin analisis completo. |
| En analisis | Responsable evalua impacto, evidencia y accion. |
| En correccion | Accion de cierre en curso. |
| Respondida | Respuesta enviada para validacion. |
| Validada | Responsable acepta cierre. |
| Rechazada | Respuesta no aceptada; requiere nueva accion. |
| Diferida | No bloquea el alcance actual o depende de tercero/fase posterior. |

## Proceso de atencion

1. Registrar observacion, origen, severidad y responsable.
2. Verificar si bloquea Productivo NO-GO, SOAP real, datos reales, secretos o certificados/endpoints.
3. Definir accion requerida y evidencia esperada.
4. Responder con evidencia sanitizada.
5. Validar cierre o registrar diferimiento formal.
6. Actualizar matriz de acciones, riesgos y log de evidencias.

## SLA sugerido

| Severidad | Atencion sugerida |
| --- | --- |
| Critica | Atencion prioritaria y bloqueo hasta decision formal. |
| Alta | Atencion prioritaria antes de cualquier intercambio externo. |
| Media | Plan de correccion antes de ejecucion UAT controlada. |
| Baja | Correccion documental planificada. |
| Observacion | Registro y respuesta cuando aplique. |

Los SLA son orientativos y no comprometen fechas reales.

## Evidencia requerida

- Respuesta escrita.
- Documento actualizado o anexo.
- Evidencia sanitizada.
- Responsable de validacion.
- Estado final.

## Control NO-GO

Ninguna observacion respondida puede interpretarse como aprobacion productiva, autorizacion SOAP real, movimiento monetario o carga de secretos/certificados/endpoints.
