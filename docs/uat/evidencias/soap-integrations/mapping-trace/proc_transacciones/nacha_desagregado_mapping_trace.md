# Mapping trace - Proc_Transacciones desde NACHA-M desagregado

Fecha: 2026-05-23  
Ambiente: UAT/local  
Productivo: NO-GO

## Resultado

`Proc_Transacciones` puede construir payload usando el NACHA-M de entrada desagregado como fuente principal cuando aplica.

Campos caracterizados por prueba automatizada:

| Target SOAP | Fuente controlada | Valor UAT |
|---|---|---|
| `TIPTRAN` | `entryDetails.transactionCode` | `22` |
| `MONTO` | `entryDetails.amount` | `4321.5` |
| `IDTRAN` | `entryDetails.sequenceNumber` | `999900001234567` |
| `NORIG` | `batchHeaders.companyName` | `BANCO EXTERNO UAT` |
| `INFPAG` | `addendaRecords.infofromOriginator` | `PAGO UAT DESAGREGADO` |
| `BCORECEP` | `nachaHeaders.immediateDestination` | `0001283` |
| `BCOORIG` | `nachaHeaders.immediateOrigin` | `9999000` |

## Guardrails

- Readiness se valida antes del payload.
- Missing mapping bloquea controladamente por pruebas existentes.
- DryRun/Disabled no transmite externamente.
- No hay SQL libre.
- No se exponen secretos.
