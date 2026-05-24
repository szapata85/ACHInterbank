# Configuracion Reglas Camara Prenotificacion

Fecha: 2026-05-19  
Estado: implementado para UAT controlado  
Productivo: **NO-GO**

## Proposito

Centralizar las reglas de prenotificacion y validacion por camara de compensacion y naturaleza de transaccion para exportacion NACHA-M, sin quemar reglas normativas en codigo.

## Modelo

Entidad: `ClearingHouseTransactionRule`

Campos principales:

- `ClearingHouseId`
- `TransactionNature`: `Debit` / `Credit`
- `TransactionType`
- `RequiresPrenotification`
- `PrenotificationMode`: `Mandatory` / `Optional` / `NotApplicable`
- `RequiresReceiverIdentificationValidation`
- `ReceiverIdentificationValidationMode`
- `AppliesToNachaExport`
- `AppliesToMonetaryTransactions`
- `EffectiveFrom`
- `EffectiveTo`
- `IsActive`
- `NormativeSource`
- `NormativeReference`
- `Notes`

## API

| Metodo | Ruta | Uso |
|---|---|---|
| GET | `/api/clearing-house-transaction-rules` | Listar reglas. |
| GET | `/api/clearing-house-transaction-rules/{id}` | Consultar detalle. |
| POST | `/api/clearing-house-transaction-rules` | Crear regla. |
| PUT | `/api/clearing-house-transaction-rules/{id}` | Actualizar regla. |
| PATCH | `/api/clearing-house-transaction-rules/{id}/activate` | Activar regla. |
| PATCH | `/api/clearing-house-transaction-rules/{id}/deactivate` | Inactivar regla. |
| POST | `/api/transaction-prerequisite-policy/preview` | Simular decision de prerequisitos. |

## SPA

Ruta: `/transactions/clearing-house-rules`  
Menu: `Transacciones > Reglas por camara`

## Comportamiento NACHA Export

- Si la regla vigente exige prenotificacion y no existe evidencia previa, export NACHA-M falla con respuesta funcional controlada.
- Si la regla vigente indica prenotificacion opcional, no bloquea por ausencia de prenotificacion.
- Si no existe regla vigente para camara/naturaleza/tipo, la exportacion debe fallar con `NACHA_EXPORT_RULE_NOT_CONFIGURED`.
- No se genera archivo 0 bytes como evidencia exitosa.

## Control Normativo

Toda regla requiere fuente y referencia normativa. La modificacion de reglas debe quedar sujeta a aprobacion de compliance/operaciones antes de preproductivo/productivo.
## Revalidacion runtime 2026-05-20

- Pantalla SPA: `/transactions/clearing-house-rules` responde como ruta Angular.
- Menu dinamico: `/navigation/menu` incluye `Transacciones > Reglas por camara`.
- API reglas: `/api/clearing-house-transaction-rules` devuelve 4 reglas activas.
- Preview politica: ACH Colombia y CENIT aplican debito obligatorio y credito opcional.
- Productivo: **NO-GO**.
