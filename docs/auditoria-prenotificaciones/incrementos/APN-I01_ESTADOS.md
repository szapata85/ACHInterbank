# APN-I01 — Inventario de estados, eventos e intentos

**Estado:** EJECUTADO_ESTATICAMENTE; HOMOLOGACION_RUNTIME_PENDIENTE. **Código productivo modificado:** no.

## Inventario verificado

| Modelo | Estado / mecanismo | Ruta de código |
|---|---|---|
| `ThirdPartyAccountValidationStatus` | `NotStarted`, `PendingSend`, `PendingResponse`, `Accepted`, `TacitlyAccepted`, `Rejected`, `RequiresCorrection`, `TechnicalReview`, `Expired`, `Suspended` | `src/Cfa.ACHInterbank.Domain/Models/ACH/Enums/ThirdPartyAccountValidationEnums.cs` |
| `PrenotificationValidationAttemptStatus` | `Created`, `Queued`, `Sent`, `AwaitingResponse`, `Accepted`, `TacitlyAccepted`, `Rejected`, `RequiresCorrection`, `TechnicalReview`, `Failed`, `Cancelled` | Misma ruta |
| `AchTransferStateEnum` | `Pending -> AppliedTacitly`, `Pending -> ReturnedByEpr` y retornos tardíos | `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchStateTransitionService.cs`, `ValidateTransition` |
| `PrenotificationValidationAttempt` | `AchTransactionId`, `SentAtUtc`, `ResponseDeadlineAtUtc`, `Resolution`, `ReturnCode`, `EvidenceReference`, `SourceCycleId` | `src/Cfa.ACHInterbank.Domain/Models/ACH/PrenotificationValidationAttempt.cs` |
| `CustomerThirdParty` | `Active` o `Rejected` sincronizado desde estado de prenotificación | `AchStateTransitionService.SynchronizePrenotificationThirdPartyAsync` |

## Transiciones y puntos de entrada

`AchStateTransitionService.TransitionAsync` valida origen/estado, guarda evento, coordina validación y estado del tercero. `ThirdPartyAccountValidationService.ApplyTransactionResolutionAsync` marca el intento y la validación, si el intento no era terminal y si es vigente. El catálogo de enums no es una demostración de cobertura de todas sus transiciones.

### Riesgo localizado

`AppliedTacitly -> ReturnedByEpr` está expresamente permitido; el intento anteriormente `TacitlyAccepted` no puede mutar por política de evidencia append-only. Se exige verificar coherencia entre transacción, validación, intento y tercero (APN-R04). **No afirmar inconsistencia de datos observada sin ejecutar P06/P17.**

**Pendiente:** pruebas runtime de estados, concurrencia e idempotencia. **Evidencia:** `evidencias/APN_EVIDENCIA_ESTATICA.json`.
