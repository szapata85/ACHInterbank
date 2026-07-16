# Evidencia de persistencia — Ejecución 3

Fecha de ejecución: 2026-07-16 (America/Bogota)

## Alcance

Se endureció exclusivamente la fecha operacional y la reserva del consecutivo externo del nombre físico. El `BatchNumber` NACHA-M T5/T8 continúa siendo un ordinal local que inicia en `0000001` por archivo y no usa proveedores persistidos.

## Flujo implementado

`solicitud lógica → snapshot Bogotá → idempotency hash → reserva transaccional ZZZ → FileId → nombre externo → generación → persistencia de registry → finalización de reserva`

Un mismo `operationKey` obtiene el mismo snapshot temporal dentro del scope. La reserva almacena hashes SHA-256 de la clave de idempotencia y del fingerprint; no persiste la clave funcional en claro.

## Componentes

- `IOperationalTimeSnapshotProvider` y `OperationalTimeSnapshotProvider`: resolución multiplataforma de `America/Bogota`, snapshot único y fallo explícito si no existe una zona autorizada.
- `ExternalFileNameReservation`: cámara, scope, fecha operacional, hashes, secuencia, FileId, nombre, estado, auditoría y token de concurrencia.
- `IExternalFileNameReservationService`: reserva y finalización idempotentes dentro de transacción `Serializable`.
- `ExternalFileNameBuilder`: usa reserva idempotente cuando recibe `IdempotencyKey`; conserva la ruta no idempotente sólo para consumidores legacy identificados.
- `ExternalFileNameRegistry.GenerationReservationId`: correlación única entre reserva y archivo persistido.
- `NachaExportController`: clave lógica estable por cámara y ciclo; snapshot compartido con el builder.

## Validaciones reproducibles

| Validación | Resultado |
|---|---|
| Bogotá antes/después de medianoche y servidor UTC | Cumple |
| Snapshot único para T1, fecha operacional, naming y auditoría | Cumple |
| Misma solicitud repetida | Reutiliza ZZZ, FileId y nombre |
| Clave reutilizada con fingerprint diferente | Falla de forma cerrada |
| Error antes/después de reservar | Reserva recuperable por la misma clave |
| Rollback de reserva | No consume un valor confirmado |
| Límite diario | Falla antes de persistir el valor 37 |
| Índices únicos | Confirmados en SQL Server y PostgreSQL |
| Persistencia tras reinicio | Confirmada |

## Resultados

- SQL Server: 4/4 pruebas del proveedor aprobadas; escenario específico de concurrencia/idempotencia aprobado.
- PostgreSQL: 16/16 pruebas del proveedor aprobadas; escenario específico de concurrencia/idempotencia aprobado.
- Suite offline final: 1.791 aprobadas, 1 diagnóstica omitida, 0 fallidas.
- Build Release final: 0 warnings, 0 errores.
- Prueba de regresión T5/T8: dos archivos del mismo día reinician en lote 1 y el mock estricto demuestra cero llamadas a `IBatchNumberGenerator`.

## Gates conservados

- El naming contractual final ACH Colombia permanece bloqueado para LIVE mediante `AchColExternalNamingHomologated=false` mientras la relación contractual ZZZ/FileId/participante siga no demostrada.
- CENIT permanece NO-GO / no homologado.
- No se garantiza ausencia absoluta de sanciones; los controles reducen riesgo mediante validación, unicidad, trazabilidad, pruebas y aprobación humana.

