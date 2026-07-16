# Migraciones — Ejecución 3

Fecha: 2026-07-16

## SQL Server

- Migración: `20260716210406_HardenExternalFileNameReservationsExecution3`.
- Crea `ExternalFileNameReservations`.
- Añade `ExternalFileNameRegistries.GenerationReservationId` nullable.
- Añade FK restrictiva e índices únicos de idempotencia, secuencia y registry/reserva.
- Snapshot actualizado en el ensamblado dedicado de SQL Server.

## PostgreSQL

- Migración: `20260716210752_HardenExternalFileNameReservationsExecution3`.
- Crea la misma capacidad lógica con tipos y token de concurrencia del proveedor.
- Añade la columna, FK e índices equivalentes.
- Snapshot PostgreSQL actualizado.

## Compatibilidad y datos existentes

- La nueva FK es nullable; los registros existentes no requieren backfill destructivo.
- No se borran tablas ni filas existentes.
- No se reinician identities.
- `Down` elimina únicamente FK, índices, columna y tabla introducidos por esta ejecución.

## Validación aplicada

| Proveedor | Esquema limpio | Esquema existente | Down | Up posterior | Constraints |
|---|---|---|---|---|---|
| SQL Server | Cumple | Cumple | Cumple | Cumple | Cumple |
| PostgreSQL | Cumple | Cumple | Cumple | Cumple | Cumple |

Se aplicó la migración SQL Server al runtime local controlado. También se validaron ambas cadenas completas en bases desechables. El rollback se comprobó hasta la migración inmediatamente anterior y la reaplicación terminó en verde.

## Riesgos y rollback

- Riesgo: despliegue de binarios nuevos sin migración. Mitigación: gate de migraciones pendientes y health/readiness.
- Riesgo: reservas en estado reservado por caída. Mitigación: reuso idempotente y auditoría; no reasignación por `MAX`.
- Rollback: detener generación, ejecutar `Down` sólo si no existen consumidores nuevos activos y restaurar binarios; nunca eliminar reservas manualmente para liberar números.

