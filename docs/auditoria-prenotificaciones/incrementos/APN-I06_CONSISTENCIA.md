# APN-I06 — Consistencia de validación y autorización monetaria

**Estado:** EJECUTADO_ESTATICAMENTE; HOMOLOGACION_RUNTIME_PENDIENTE. **Código productivo modificado:** no.

## Matriz funcional que se debe comprobar

| Estado de validación | Elegibilidad monetaria esperada en contexto que exige prenotificación | Verificación |
|---|---|---|
| `NotStarted` / `PendingSend` / `PendingResponse` | Bloquear mientras falte validación | Política de débito y cuenta |
| `Accepted` | Permitir solo si vigencia, naturaleza y cámara coinciden | Contexto completo |
| `TacitlyAccepted` | Permitir solo si el vencimiento es válido y no existe devolución pendiente | Job, archivo final y estados |
| `Rejected` | Bloquear o requerir corrección | Causal, intento y tercero |
| `RequiresCorrection` | Bloquear hasta nuevo intento válido | Intentos y revalidación |
| `TechnicalReview` | Fail-closed | Estado, cuenta y operador |
| `Expired` / `Suspended` | Bloquear y revalidar conforme a política | Fecha y suspensión |

**Excepción:** la prenotificación crédito es opcional según la política del servicio/cámara. No interpretar esta matriz como obligación general de prenotificar todos los créditos.

## Código revisado

`ThirdPartyAccountValidationService.Start...` crea intentos asociados a cámara y naturaleza; `ApplyTransactionResolutionAsync` protege intentos terminales y resuelve el intento actual. `AchStateTransitionService` sincroniza validación y tercero. `TransactionPrerequisitePolicyService` y políticas de elegibilidad requieren pruebas de conjunto.

**Punto crítico:** si llega devolución tras `AppliedTacitly`, la transición de transacción es posible, pero el intento terminal no se reescribe. Verificar en P06/P17 si el sistema termina con `ThirdPartyAccountValidationStatus.TacitlyAccepted`, estado de tercero `Rejected` y transacción `ReturnedByEpr`: si ocurre, precisar corrección de consistencia que preserve evidencia append-only sin reescribir hechos históricos.

Pruebas pendientes: P06, P08, P12, P17–P20 y estado de UI/servicios.
