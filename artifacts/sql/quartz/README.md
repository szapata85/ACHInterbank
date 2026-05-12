# Quartz QRTZ_* schema scripts

## Estado
- **Development/local**: usar `Quartz:JobStore:Mode=RAM` (sin tablas `QRTZ_*`).
- **UAT/Producción**: usar `Quartz:JobStore:Mode=Persistent` con script oficial Quartz para la versión instalada (**3.18.0**).

## PostgreSQL (recomendado en este branch)
Para PostgreSQL, aplicar el script oficial de Quartz.NET 3.18.0 para AdoJobStore (tablas `QRTZ_*`).

> Nota: este repositorio no inventa un script parcial/manual para PostgreSQL. Debe usarse el script oficial del paquete/versionado por infraestructura.

## SQL Server (alternativa)
Se incluye un script operativo en este repositorio:

- `artifacts/sql/quartz/sqlserver-qrtz-schema.sql`

Este script debe aplicarse por DBA/infra fuera de migraciones EF del dominio ACH.
