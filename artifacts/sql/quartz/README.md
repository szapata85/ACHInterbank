# Quartz QRTZ_* schema scripts

## Reglas de esta carpeta
- Los scripts aquí son **exclusivamente** para objetos Quartz `QRTZ_*`.
- **No** deben incluir tablas/columnas del dominio ACH o SOAP.

## Estado
- **Development/local**: usar `Quartz:JobStore:Mode=RAM` (sin tablas `QRTZ_*`).
- **UAT/Producción**: usar `Quartz:JobStore:Mode=Persistent` con script oficial Quartz para la versión instalada (**3.18.0**).

## PostgreSQL (recomendado en este branch, alternativa SQL Server soportada)
Para PostgreSQL, aplicar el script oficial de Quartz.NET 3.18.0 para AdoJobStore (tablas `QRTZ_*`).

> Nota: este repositorio no inventa un script parcial/manual para PostgreSQL. Debe usarse el script oficial del paquete/versionado por infraestructura.

## SQL Server (alternativa)
Se incluye un script operativo en este repositorio:

- `artifacts/sql/quartz/sqlserver-qrtz-schema.sql`

Ese script define `DECLARE @DropDb BIT = 0` por defecto.

- Si se requiere recreación limpia, DBA debe cambiar manualmente a `@DropDb = 1` bajo ventana controlada y aprobación formal.
- `@DropDb = 1` elimina tablas `QRTZ_*` existentes y puede borrar jobs/triggers persistidos.

Estos scripts deben aplicarse por DBA/infra fuera de migraciones EF del dominio ACH.


- `postgres-qrtz-schema.sql` es un placeholder controlado: usar script oficial PostgreSQL para ejecución real.
- `sqlserver-qrtz-schema.sql` es artefacto operativo real, debe mantenerse únicamente con objetos QRTZ_*.
- Ambos se gestionan fuera de migraciones EF del dominio ACH.
