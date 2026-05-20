# Evidencia Runtime Dry-Run Proc_Contrapartidas

Fecha: 2026-05-19 America/Bogota.

## Escenario

Se creo una prenotificacion sintetica de monto cero para validar el guardrail de `Proc_Contrapartidas` sin invocar endpoint externo:

- Referencia: `UAT-SOAP-DRYRUN-001`
- TransactionId: `245`
- Datos: sinteticos/anonimizados.
- Endpoint externo: no invocado.

## Resultado

El scheduler `AchContrapartidasByCycle` proceso el item y registro:

| Campo | Valor |
|---|---|
| Estado final item | `ContrapartidaReportFailed` |
| AttemptCount | `1` |
| LastErrorCode | `PROC_DRY_RUN` |
| RetryEligible | `false` |
| Mensaje | `Proc_Contrapartidas dry-run: envelope generado, no transmitido.` |
| TaskExecutionLog | `Processed=1, Success=0, Failed=1, Partial=0, Chunks=1` |

Logs API confirmaron:

`Proc_Contrapartidas dry-run: envelope generado, no transmitido. Mode=dry-run CycleId=2ada513804193e8aa8771252660182fdc7a55862 ClearingHouseId=1`

No se observo `SOAP request`, `SOAP_EXCEPTION`, DNS externo ni transmision a endpoint productivo en la ventana validada.

## Decision

DEF-UAT-022 queda cerrado tecnicamente para ambiente UAT/local con guardrail `DryRun` por defecto. La integracion real con endpoint UAT/mock autorizado sigue pendiente de homologacion.
