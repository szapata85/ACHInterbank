# Validacion UAT - Respuesta Diferencial de Prenotificacion Rechazada

Fecha: 2026-05-23

Resultado: `OK TECNICO UAT`.

## Validaciones

- Prenotificacion CFA pendiente identificada por `EntryDetails.SequenceNumber`.
- Cruce con NACHA-M desagregado validado: `NachaHeaders`, `BatchHeaders`, `EntryDetails`, `AddendaRecords`, `BatchControls`, `FileControls`.
- `IntegrationMappingSet` publicado consumido.
- `IntegrationMappingTrace` y entradas campo-a-campo persistidas.
- Causal `R03` mapeada y registrada.
- Estado final: `ReturnedByEpr`.
- Evento de estado creado.
- No movimiento monetario.
- No afectacion de saldos.
- No transmision externa.

Productivo: **NO-GO**.
