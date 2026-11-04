# Contrato API — I02, solo lectura

## Buscar prenotificaciones originadas

`GET /api/uat/nacha-inbound-simulator/prenotifications/originated`

Filtros: `ClearingHouseCode` (requerido), `RespondingFinancialInstitutionId`, `FromDate`, `ToDate`, `Search`, `Page`, `PageSize` (1–100). Resultado: `PrenotificationCandidatePage`, incluye filas con motivos por los que no se puede simular la respuesta. Solo incluye operaciones `IsPrenotification && Type==Prenotification`, con banco originador local y destino externo, en la cámara indicada. Nunca muestra cuentas completas.

## Verificar selección de operación existente

`POST /api/uat/nacha-inbound-simulator/prenotifications/eligibility-preview`

Mismo JSON del contrato I01 (`Outcome`, `ClearingHouseCode`, `OriginalPrenotificationIds`, `OriginalCycleId`, `ResponseCycleId` según Outcome, `BusinessDate`, `RespondingFinancialInstitutionId`, `ReasonCode`, `IdempotencyKey` para Returned).

Respuesta: `ShapeValid` (forma), `BusinessEligible` (snapshot de consulta), `ExecutionEnabled=false` (I02; invariable), `ContractVersion`, `Violations[]`, `Candidates[]`.

Las infracciones contienen `Code`, `Field`, `Message` y no exponen números de cuenta. `BusinessEligible=true` **no reserva** la clave de idempotencia, ni garantiza elegibilidad en el instante de generar. I03 debe recalcularla transaccionalmente. No se modifica `POST /generate`, que mantiene bloqueo del escenario 10.

## Seguridad

`[Authorize(Policy=FineGrainedPermissions.TestTools.Access)]` a nivel controlador y `[Authorize(Policy=P1Policies.NachaSimulatorRead)]` en ambos endpoints. `AsNoTracking()` en lecturas. Solo modo UAT/Local mediante `NachaInboundSimulatorOptions.IsUatLike()`.

## Causales relevantes

`PRENOTIFICATION_NOT_FOUND`, `NOT_A_PRENOTIFICATION`, `NOT_ORIGINATED_HERE`, `PRENOTIFICATION_NOT_PENDING`, `PRENOTIFICATION_NOT_TRANSMITTED`, `PRENOTIFICATION_ATTEMPT_NOT_ACTIVE`, `PRENOTIFICATION_ALREADY_RESPONDED`, `PRENOTIFICATION_CORRELATION_INCOMPLETE`, `PRENOTIFICATION_DEADLINE_UNKNOWN`, `RETURN_DEADLINE_EXPIRED`, `PRENOTIFICATION_DEADLINE_NOT_REACHED`, `RESPONSE_CYCLE_INVALID`, `PRENOTIFICATION_WRONG_RESPONDER`, `PRENOTIFICATION_WRONG_CLEARING_HOUSE`, `CENIT_RETURN_OUTSIDE_VALUE_DATE`.

## Diferencias por cámara

ACHCOL: respeta `ResponseDeadlineAtUtc` calculado por las reglas de cámara ya resueltas. CENIT: además, en Returned se exige la misma fecha valor del ciclo original. Nunca se crea un archivo de aceptación tácita.
