# Mapping Trace - Prenotificacion Rechazada

Fecha: 2026-05-23

| Campo destino | Fuente | Valor sanitizado | Requerido | Fallback |
|---|---|---|---|---|
| `ANSIDLOTE` | `BatchHeaders.BatchNumber` | `1` | Si | No |
| `ANSIDTX` | `EntryDetails.SequenceNumber` | `000128300012345` | Si | No |
| `ANSST` | `DifferentialResponse.CodigoEstadoExterno` | `RJ` | Si | No |
| `ANCLC` | `DifferentialResponse.CodigoCausalExterna` | `R03` | Si | No |
| `ANSIDREVER` | `AddendaRecords.OriginalTraceNumber` | `000128300012345` | Si | No |

Resultado: prenotificacion CFA pendiente procesada como rechazada por causal `R03`.

Guardrail: `movesMoney=false`, `monetaryMovementCreated=false`, `balancesAffected=false`.
