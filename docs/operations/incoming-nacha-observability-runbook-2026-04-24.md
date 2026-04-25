# Runbook operativo — Observabilidad Inbound NACHA-M

Fecha: 2026-04-24

## Objetivo

Definir una guía mínima de operación para el dashboard de observabilidad inbound (`/incoming-nacha-command-center/observability`) usando los agregados del Command Center API.

## Fuente de datos

Endpoint backend:

- `GET /incoming-nacha-command-center/observability/summary?windowHours={1..168}`

Agregados incluidos:

- salud del pipeline (ingestas, cola, backlog, blocked, retry pending, waiting window, failed final, confirmed, aging);
- distribución por estado (ingesta y cola);
- top de errores;
- agrupación por cámara/ciclo;
- timeline por hora (Applied/Rejected/transiciones retry pending y failed final).

## Criterios operativos sugeridos (alerta visual)

> Estos umbrales son operativos (P1), no regulatorios finales.

1. **BlockedItems >= 1**
   - acción: revisar `topErrors` + abrir cola dispatch filtrada por estado `Blocked`.
2. **RetryPendingItems >= 5**
   - acción: revisar ventana de reintentos y disponibilidad de integración externa.
3. **WaitingWindowItems >= 5**
   - acción: validar calendario operativo/ciclo y hora de liberación.
4. **FailedFinalItems >= 1**
   - acción: abrir detalle de item y validar causa funcional/técnica.
5. **OldestQueueAgeMinutes >= 60**
   - acción: tratar backlog como incidente operativo.

## Flujo de triage

1. Abrir observabilidad con ventana de 24h.
2. Validar KPIs rojos (blocked/retry pending/waiting window/failed final/aging).
3. Revisar `Top errores` para priorizar causa dominante.
4. Revisar `Métricas por cámara/ciclo` para localizar foco.
5. Entrar a `Cola dispatch` y luego a detalle de item para acción manual auditada si aplica.
6. Confirmar en `Timeline operativo` que eventos `Applied` aumentan y `Rejected` disminuyen.

## Interpretación rápida de timeline

- `ManualApplied` alto con descenso de backlog: mitigación efectiva.
- `ManualRejected` alto: posible intento de acción no permitida por state machine/estado.
- picos de `RetryPendingTransitions`: degradación técnica externa/intermitencia.
- picos de `FailedFinalTransitions`: problema funcional o agotamiento de reintentos.

## Notas de seguridad

- El dashboard no ejecuta criptografía ni maneja secretos.
- No persiste datos operativos sensibles en `localStorage/sessionStorage`.
- La UI no calcula state machine; solo consume agregados y `AllowedActions` backend.
