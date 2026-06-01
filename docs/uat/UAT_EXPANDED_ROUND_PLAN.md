# Plan UAT ampliado ACH Colombia/CENIT - Fase 6D.2

Productivo permanece NO-GO. La ronda ampliada usa datos sinteticos/anonimizados, evidencia sanitizada y controles read-only.

## Objetivo

Ejecutar una ronda UAT ampliada con dataset sintetico mas representativo, cubriendo los hallazgos de ronda 1 y reforzando causales, inconsistencias, manual review, ciclos CENIT y evidencia CI.

## Alcance

- ACH Colombia y CENIT.
- NACHA-M salida/entrada, `.RET`, rechazos/devoluciones, prenotificaciones, ROR, conciliacion, dashboard, detalle archivo, config profiles, consola SOAP/UAT read-only y export `cycleId`.
- Validacion de estados conciliado, pendiente, inconsistente y manual review.

## Exclusiones

- Produccion, SOAP real, certificados reales, datos reales de clientes, movimiento monetario real, mutaciones criticas, legacy oficial y `/NachaExport/{hash}`.
- Certificacion oficial ACH Colombia/CENIT.

## Precondiciones

- Ambiente UAT aislado disponible.
- Dataset sintetico ampliado aprobado y cargado.
- CI backend/Angular/Playwright verde o con observaciones no bloqueantes.
- Evidencias Playwright publicadas.
- Operaciones ACH/CENIT disponibles para validar causales y ciclos.

## Datos requeridos

- Lotes ACH Colombia y CENIT con debit/credit, prenotes, returns, ROR e inconsistencias controladas.
- Casos de causal conocida, causal ambigua y causal no homologada.
- Ciclos CENIT sinteticos con cola/neteo sin movimiento real.

## Escenarios a repetir

UAT-ACH-001 a UAT-ACH-009, UAT-CEN-001 a UAT-CEN-006 y UAT-TRV-001 a UAT-TRV-008, priorizando los observados/no ejecutados de ronda 1.

## Escenarios nuevos

| ID | Camara | Objetivo | Evidencia esperada | Estado inicial |
| --- | --- | --- | --- | --- |
| UAT-EXP-001 | Ambas | Causal ambigua clasifica manual review | Consola conciliacion | Pendiente |
| UAT-EXP-002 | Ambas | Respuesta diferencial inconsistente queda sin mutacion | Conciliacion/read-only | Pendiente |
| UAT-EXP-003 | CENIT | Ciclo/cola/neteo sintetico visible sin movimiento | Dashboard/read-store | Pendiente |
| UAT-EXP-004 | ACH Colombia | Prenote rechazada con causal homologada | Conciliacion/auditoria | Pendiente |
| UAT-EXP-005 | Ambas | Guardas read-only y NO-GO en evidencia CI | Playwright artifacts | Pendiente |

## Criterios de entrada

- Dataset ampliado sin datos reales ni secretos.
- Hallazgos ronda 1 clasificados.
- Checklist ampliado en estado listo para ejecutar.

## Criterios de salida

- Resultados por escenario registrados.
- Defectos con severidad/estado/responsable.
- Evidencias CI y UAT anexadas.
- Decision Go/No-Go UAT documentada. Esto no habilita productivo.

## Decision Go/No-Go UAT

La decision permitida es continuar, repetir o bloquear UAT ampliado. La decision productiva permanece NO-GO hasta certificacion oficial, SOAP real controlado aprobado y comite explicito.
