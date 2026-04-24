# Incoming NACHA Command Center API (Prompt 3) — 2026-04-24

## Alcance

Implementación backend mínima viable para operación manual auditada de cola inbound NACHA-M.

- Incluye consulta de ingestas/cola y acciones manuales (`retry`, `unblock`, `requeue`, `mark-failed-final`).
- No incluye SPA ni state machine enterprise completa.

## Seguridad

- Lectura: `CanReadAch`
- Operación manual: `CanManageAch`

## Endpoints

Base route: `/incoming-nacha-command-center`

1. `GET /ingestions`
   - Lista ingestas con paginación/filtros (`IngestionStatus`, `ParsingStatus`, `CorrelationId`, `FileName`).

2. `GET /ingestions/{ingestionId}`
   - Detalle de ingesta con cola asociada y eventos.

3. `GET /queue`
   - Lista items de cola con paginación/filtros (`IngestionId`, `QueueStatus`, `ClearingHouseId`, `AchCycleId`).

4. `GET /queue/{queueId}`
   - Detalle de item de cola, clasificación, ejecuciones de integración y eventos.

5. `POST /queue/{queueId}/retry`
6. `POST /queue/{queueId}/unblock`
7. `POST /queue/{queueId}/requeue`
8. `POST /queue/{queueId}/mark-failed-final`

Payload común para acciones manuales:

```json
{
  "justification": "texto obligatorio >= 8 chars",
  "idempotencyKey": "clave-obligatoria",
  "priority": 50
}
```

## Reglas operativas implementadas

1. Toda acción manual exige:
   - `Justification` obligatoria,
   - `IdempotencyKey` obligatoria,
   - usuario operador (`performedBy`).

2. Idempotencia manual:
   - Se registra evento `ManualAction{Action}` con mensaje `IdempotencyKey:{key}`.
   - Repetición de misma clave devuelve replay idempotente sin reprocesar.

3. Validaciones por estado:
   - `retry`: bloquea `Confirmed`, `FailedFinal`, `Dispatching`.
   - `unblock`: solo permite desde `Blocked`.
   - `requeue`: bloquea `Confirmed`.
   - `mark-failed-final`: bloquea `Confirmed`.

4. Auditoría:
   - Toda operación manual exitosa registra `IncomingNachaProcessingEvent` con evidencia JSON (estado previo/actual, justificación, clave idempotente, actor).

## DTOs para SPA futura

- `IncomingNachaIngestionListItemDto`
- `IncomingNachaIngestionDetailDto`
- `IncomingNachaQueueListItemDto`
- `IncomingNachaQueueDetailDto`
- `IncomingNachaIntegrationExecutionDto`
- `IncomingNachaProcessingEventDto`
- `IncomingNachaManualActionResultDto`

## Pruebas relevantes

- `IncomingNachaCommandCenterServiceTests`
  - retry exitoso con auditoría,
  - rechazo por estado inválido,
  - idempotencia de acción manual.

- No regresión ejecutada en suites NACHA/Mapping/BatchNumber.
