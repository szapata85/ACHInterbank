# Quartz Persistent Store — operación

## 1) Resumen
- Development/local: `RAMJobStore`.
- UAT/Producción: `AdoJobStore` persistente con tablas `QRTZ_*`.
- Recomendación actual del branch: **PostgreSQL**.
- SQL Server: alternativa documentada.

## 2) Estado actual
- `appsettings*.json` define por defecto `Quartz:JobStore:Mode=RAM`.
- No se activa persistent store por defecto en Development.

## 3) Modo recomendado UAT/Prod
- `Quartz__JobStore__Mode=Persistent`
- `Quartz__JobStore__Provider=Postgres`
- `Quartz__JobStore__TablePrefix=QRTZ_`
- `Quartz__JobStore__Clustered=true`

## 4) Proveedor recomendado y alternativa
- Recomendado: PostgreSQL (alineado con `Database:Provider=Postgres` en este branch).
- Alternativa: SQL Server (si la plataforma decide centralizar Quartz ahí).

## 5) QRTZ_* y migraciones EF de dominio
Las tablas `QRTZ_*` son infraestructura de Quartz, no entidades del dominio ACH. En SQL Server se crean/actualizan con la migración EF Core `20261118110000_QuartzSqlServer471Schema`, que ejecuta SQL embebido idempotente; en PostgreSQL se mantiene la preparación operativa específica del motor.

## 6) Configuración (appsettings/env)
Variables sugeridas:
- `Quartz__JobStore__Mode=Persistent`
- `Quartz__JobStore__Provider=Postgres`
- `Quartz__JobStore__TablePrefix=QRTZ_`
- `Quartz__JobStore__Clustered=true`
- `Quartz__JobStore__ClusterCheckinIntervalSeconds=20`
- `Quartz__JobStore__MisfireThresholdMilliseconds=60000`
- `Quartz__JobStore__PerformSchemaValidation=true`

## 7) Preparación de BD
1. En SQL Server ejecutar `Database.MigrateAsync()` (o `docker-ach.cmd migrate`) con `Database:ApplyMigrations=true`, usando la migración Quartz.NET 4.7.1. En PostgreSQL seguir el procedimiento oficial de esquema del motor.
2. Verificar permisos del usuario DB para CRUD/locks sobre `QRTZ_*`.
3. Activar `Mode=Persistent` por configuración.
4. Iniciar API y validar logs de Quartz.

## 8) Clustering
Activar `Clustered=true` cuando haya múltiples nodos compartiendo la misma BD `QRTZ_*`.
Requisitos:
- reloj sincronizado entre nodos;
- misma BD compartida;
- observabilidad de locks/fired triggers.

## 9) Rollback
- Development: volver a `Mode=RAM`.
- UAT/Prod: no borrar `QRTZ_*` sin análisis de jobs/estado.

## 10) SQL útiles
```sql
SELECT * FROM "QRTZ_JOB_DETAILS";
SELECT * FROM "QRTZ_TRIGGERS";
SELECT * FROM "QRTZ_FIRED_TRIGGERS";
SELECT * FROM "QRTZ_SCHEDULER_STATE";
SELECT * FROM "QRTZ_LOCKS";
```

## 11) Riesgos pendientes
- misfire policies funcionales;
- observabilidad de drift/sync;
- pruebas multi-nodo reales;
- cola durable avanzada para `Queue`.


## 12) Configuración multi-motor

| Ambiente | Quartz Mode | Provider | ConnectionString | Script |
|---|---|---|---|---|
| Dev | RAM | N/A | N/A | No requiere |
| UAT Postgres | Persistent | Postgres | PostgresConnection | script oficial PostgreSQL |
| UAT SQL Server | Persistent | SqlServer | SqlConnection | EF Core `20261118110000_QuartzSqlServer471Schema` |
| Producción | Persistent | según motor | según motor | oficial/control DBA |

> No ejecutar script SQL Server con `@DropDb=1` sin aprobación DBA.
> No usar `postgres-qrtz-schema.sql` placeholder como schema real. Para PostgreSQL usar script oficial Quartz.NET de la versión instalada.
