# Incoming NACHA Inbound — Resiliencia de Integración y Compensaciones (Prompt 5)

Fecha: 2026-04-24

## Objetivo

Endurecer el despacho inbound hacia `Proc_Transacciones` para separar correctamente:

- fallas técnicas retryables,
- rechazos funcionales no retryables,
- agotamiento de reintentos,
- transiciones de ventana operativa (`WaitingWindow`),
- trazabilidad/auditoría automática de decisiones.

## Cambios implementados

1. **Retry policy configurable** (`IncomingNacha:DispatchResilience`)
   - `MaxAttempts`
   - `InitialBackoffSeconds`
   - `BackoffMultiplier`
   - `MaxBackoffSeconds`
   - `EnableJitter` / `JitterMaxSeconds`

2. **Backoff exponencial parametrizable**
   - `NextAttemptAtUtc = now + backoff(attemptCount)`
   - Jitter opcional para evitar thundering herd.

3. **Clasificación técnica vs funcional**
   - Rechazo funcional (`IsFunctionalRejection`) → `FailedFinal` con código `IFUNC`.
   - Fallas técnicas retryables → `RetryPending` con códigos normalizados (`I500`, `I503`, `ITIMEOUT`, `ISOAP`) y reintento programado.
   - Si se agotan intentos → `FailedFinal` con evento `MaxAttemptsExceeded`.

4. **Manejo robusto de `WaitingWindow`**
   - Ítems en `WaitingWindow` con `NextAttemptAtUtc` vencido se liberan de forma masiva a `Queued`.
   - El resultado del run expone cuántos ítems se liberaron desde `WaitingWindow`.

5. **Auditoría automática y compensaciones**
   - Eventos técnicos/operativos automáticos en `IncomingNachaProcessingEvents`:
     - `DispatchStarted`
     - `IntegrationSucceeded`
     - `IntegrationRetryableFailed`
     - `IntegrationNonRetryableFailed`
     - `IntegrationTechnicalFailed`
     - `MaxAttemptsExceeded`
     - `DispatchBlockedByMapping`
     - `IntegrationContextMissing`
   - Evidencia mínima serializada por evento (`queueId`, `status`, `attemptCount`, `code`).

## Matriz operativa (dispatch automático)

- `Queued`/`RetryPending` elegibles para ejecución.
- `Dispatching` estado transitorio interno de ejecución.
- `Confirmed` en éxito o éxito parcial.
- `RetryPending` en falla técnica retryable con intentos disponibles.
- `FailedFinal` en rechazo funcional o agotamiento de intentos.
- `Blocked` en inconsistencia de contexto/mapping inválido.
- `WaitingWindow` liberado a `Queued` al vencimiento de `NextAttemptAtUtc`.

## Compatibilidad con Prompt 4C

- No se alteraron endpoints públicos del Command Center.
- No se rompió la state machine manual.
- Se mantiene `AllowedActions` y auditoría de transiciones manuales.
