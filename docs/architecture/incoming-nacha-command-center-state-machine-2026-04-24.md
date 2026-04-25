# Incoming NACHA Command Center — State Machine (Dispatch Manual)

Fecha: 2026-04-24  
Ámbito: `IncomingNachaDispatchQueue` para operaciones manuales de Command Center inbound.

## Objetivo

Homologar transiciones manuales (`retry`, `unblock`, `requeue`, `mark-failed-final`) con:

- matriz explícita estado/evento,
- guardas con código auditable,
- salida de `allowedActions` para SPA futura,
- auditoría homogénea en `IncomingNachaProcessingEvents`.

## Matriz manual implementada

Estados soportados del dispatch:

- `Queued`
- `Dispatching`
- `Dispatched`
- `Confirmed`
- `RetryPending`
- `FailedFinal`
- `Blocked`
- `WaitingWindow`

Eventos manuales:

- `ManualRetry` → target `Queued`
- `ManualUnblock` → target `Queued`
- `ManualRequeue` → target `Queued`
- `ManualMarkFailedFinal` → target `FailedFinal`

### Guardas

- `ManualRetry`: permitido desde `Queued`, `Dispatched`, `RetryPending`.
- `ManualUnblock`: permitido solo desde `Blocked`.
- `ManualRequeue`: permitido desde `Queued`, `Dispatched`, `RetryPending`, `Blocked`, `WaitingWindow`.
- `ManualMarkFailedFinal`: permitido desde `Queued`, `Dispatched`, `RetryPending`, `Blocked`, `WaitingWindow`.

Restricciones operativas de hardening (Prompt 4B):

- `Confirmed` es terminal (sin acciones manuales).
- `FailedFinal` es terminal en esta fase (sin acciones manuales).
- `Blocked` no permite `retry` directo; solo `unblock` o `mark-failed-final`.
- `Dispatching` no permite acciones manuales.
- `WaitingWindow` no habilita `retry` manual sin política explícita (feature flag interno actual: deshabilitado).

Para transiciones inválidas, se retorna código:

- `INCOMING_NACHA_STATE_MACHINE_GUARD_<GUARD_CODE>`

Para transiciones válidas:

- `INCOMING_NACHA_STATE_MACHINE_OK_<GUARD_CODE>`

## Integración en Command Center

`IncomingNachaCommandCenterService` ahora:

1. Evalúa transición con `IIncomingNachaStateMachineService`.
2. Aplica estado destino solo si la transición es válida.
3. Registra auditoría homogénea con:
   - `EventType = "DispatchTransition"`
   - `Message = "Event:<Event>;IdempotencyKey:<Key>"`
   - `EventStatus = "Applied"` para transiciones válidas y `EventStatus = "Rejected"` para rechazos por guarda.
   - `EvidenceJson` con `previousStatus`, `currentStatus`, `transitionEvent`, `resultCode`, `justification` y `performedBy`.

Además, `GetQueue` y `GetQueueDetail` exponen `AllowedActions` por item.

## Notas de cumplimiento

- No se alteró parser NACHA ni criptografía.
- No se cambiaron endpoints públicos del Command Center.
- Se reutilizó el contrato `IIncomingNachaStateMachineService` existente.
