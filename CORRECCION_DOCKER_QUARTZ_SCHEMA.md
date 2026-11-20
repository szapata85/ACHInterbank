# ACHInterbank — Quartz.NET 4.7.1 desde migraciones EF Core (SQL Server)

**Actualizado:** 2026-11-18.

## Implementación

- `docker-compose.yml` **ya no define** el contenedor `quartz-schema`.
- `20261118110000_QuartzSqlServer471Schema` incorpora el esquema `QRTZ_*` al historial de migraciones EF Core SQL Server, con un recurso SQL embebido en `Cfa.ACHInterbank.Persistence.Migrations.SqlServer.dll`.
- El recurso es una copia inmutable del SQL no destructivo `artifacts/sql/quartz/sqlserver-qrtz-schema.sql`: 12 tablas, 12 PK, 4 FK, 12 índices y 26 ampliaciones verificadas para Quartz.NET 4.7.1.
- Los lotes `GO` se separan antes de enviarlos a EF Core. No requiere `sqlcmd`, montaje de archivos ni ejecución de un contenedor adicional.
- El script es idempotente sobre tablas ya existentes. Los jobs, triggers y datos de Quartz se conservan. El `Down` de la migración **no borra** las tablas QRTZ, incluso en caso de rollback.
- `Quartz__JobStore__PerformSchemaValidation=true` sigue activo.
- El perfil clúster **SQL Server** también utiliza la migración en el nodo 01, antes del arranque del nodo 02. Los perfiles PostgreSQL mantienen por ahora su estrategia independiente.

## Primer arranque

Conservar el archivo de credenciales local y la contraseña del volumen SQL Server. Desde CMD y la raíz del proyecto:

```cmd
docker-ach.cmd config
docker-ach.cmd infra
docker-ach.cmd migrate
docker-ach.cmd up --no-seed
docker-ach.cmd status
```

`docker-ach.cmd migrate` ejecuta `dotnet ef database update`; requiere el SDK .NET 10 instalado en el host. Alternativamente, `docker-ach.cmd up` construye e inicia la API, que ejecuta `Database.MigrateAsync()` antes de inicializar Quartz cuando `Database__ApplyMigrations=true` (valor por defecto del Compose).

`docker-ach.cmd infra` **solo levanta SQL Server**, sin crear otra instancia. Si se usa `up --no-auto-migrations` contra una base nueva, primero debe haberse completado `docker-ach.cmd migrate`; de lo contrario Quartz rechazará el arranque porque faltará `QRTZ_*`.

## Instalaciones existentes

1. Realizar un respaldo verificado de SQL Server.
2. Conservar el volumen existente (usar `docker-ach.cmd down` **sin** `-v`).
3. Reconstruir la imagen de API y aplicar la nueva migración con `docker-ach.cmd up` o ejecutar `docker-ach.cmd migrate` desde el host.
4. Verificar que el historial `__EFMigrationsHistory` tenga `20261118110000_QuartzSqlServer471Schema`, que existan las 12 tablas y que `/health/ready` esté disponible.

Consulta de verificación SQL Server:

```sql
SELECT MigrationId FROM dbo.__EFMigrationsHistory WHERE MigrationId = '20261118110000_QuartzSqlServer471Schema';
SELECT COUNT(*) AS QuartzTables FROM sys.tables WHERE schema_id = SCHEMA_ID('dbo') AND name LIKE 'QRTZ[_]%';
```

No ejecutar `docker compose down -v`, `docker-ach.cmd reset --confirm-data-loss` ni los scripts destructivos `src/Scripts/tables_sqlServer.sql` para esta transición.

## Validación

`python scripts/test/verify_quartz_4_7_1_sqlserver.py` permite comprobar estáticamente migración, recurso y Compose. La verificación funcional requiere ejecutar la API contra SQL Server con .NET/Docker disponibles.
