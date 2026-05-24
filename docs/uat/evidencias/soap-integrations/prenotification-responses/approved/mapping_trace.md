# Mapping Trace - Prenotificacion Aprobada

Fecha: 2026-05-23

| Campo destino | Fuente | Valor sanitizado | Requerido | Fallback |
|---|---|---|---|---|
| `ANSIDLOTE` | `BatchHeaders.BatchNumber` | `1` | Si | No |
| `ANSIDTX` | `EntryDetails.SequenceNumber` | `000128300012345` | Si | No |
| `ANSST` | `DifferentialResponse.CodigoEstadoExterno` | `00` | Si | No |
| `ANSIDREVER` | `AddendaRecords.OriginalTraceNumber` | `000128300012345` | Si | No |

Resultado: prenotificacion CFA pendiente procesada como aprobada/exitosa.

Guardrail: `movesMoney=false`, `monetaryMovementCreated=false`, `balancesAffected=false`.
