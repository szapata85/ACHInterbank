# Concurrencia del consecutivo externo

Fecha: 2026-07-16

## Invariantes

- ZZZ es independiente de PK, Trace Number, ciclo y `BatchNumber` T5/T8.
- La clave persistida actual es cámara + scope de naming + fecha operacional.
- `CycleId` no participa en unicidad.
- No se añadió participante origen al índice porque su obligatoriedad contractual todavía no está demostrada. El naming final LIVE permanece bloqueado por esta brecha.
- Valor inicial: 1. Límite implementado por el patrón vigente: 36; el siguiente valor falla de forma cerrada.

## Estrategia común

1. Transacción `Serializable`.
2. Búsqueda por hash de idempotencia.
3. Validación del fingerprint de la solicitud.
4. Reserva provider-specific.
5. Inserción de reserva y commit atómico.
6. Reintentos acotados sólo para colisión/serialización/deadlock reconocidos.

No se usa lock estático, `SELECT MAX + 1` sin protección ni aislamiento por defecto como garantía principal.

## SQL Server

- `UPDATE` condicional sobre la fila diaria con protección transaccional.
- Código controlado `51036` cuando el contador alcanzó el máximo.
- Constraint único de secuencia y constraint único de idempotencia.
- Excepciones del proveedor se normalizan a una excepción de dominio segura.

## PostgreSQL

- `INSERT ... ON CONFLICT ... DO UPDATE` con condición `LastValue < máximo`.
- Participa en la transacción ambiente de la reserva.
- Constraint único de secuencia y constraint único de idempotencia.

## Evidencia

| Escenario | SQL Server | PostgreSQL |
|---|---:|---:|
| 50 solicitudes diferentes concurrentes | 50 únicas, sin duplicados | 50 únicas, sin duplicados |
| 50 reintentos concurrentes de la misma solicitud | una reserva reutilizada | una reserva reutilizada |
| Cambio de día | reinicia en 1 | reinicia en 1 |
| Límite | 36 permitido; 37 rechazado sin consumo | 36 permitido; 37 rechazado sin consumo |
| Reinicio de aplicación | reserva consultable y reutilizable | reserva consultable y reutilizable |
| Down/Up de migración | aprobado | aprobado |

## Riesgo residual

El scope contractual definitivo por participante y la correspondencia exacta ZZZ/FileId siguen no demostrados. No se debe abrir el gate de naming LIVE hasta disponer de documento contractual vigente y prueba de homologación.

