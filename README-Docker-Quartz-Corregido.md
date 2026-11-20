# ACHInterbank — SQL Server y Quartz.NET sin contenedor de esquema

**Actualizado: 2026-11-18**

Quartz.NET 4.7.1 usa el esquema `QRTZ_*` instalado mediante la migración EF Core SQL Server `20261118110000_QuartzSqlServer471Schema`. No se levanta un contenedor `quartz-schema`.

## Requisitos

- Docker Desktop (para SQL Server, API y SPA).
- SDK .NET 10 si se usa `docker-ach.cmd migrate` desde el host. El comando `up` puede aplicar las migraciones desde la API, sin instalar `dotnet-ef` en el host.
- Configuración SQL Server válida en `src/Cfa.ACHInterbank.Api/appsettings.json`; no sobrescribir las credenciales locales ni publicarlas.

## Arranque recomendado en CMD

```cmd
docker-ach.cmd config
docker-ach.cmd up --no-seed
docker-ach.cmd status
docker-ach.cmd logs
```

El servicio `achinterbank-api` espera que SQL Server esté healthy y ejecuta `Database.MigrateAsync()` antes del arranque de Quartz, dado que el Compose utiliza `Database__ApplyMigrations=true` por defecto. La API crea la base si no existe y aplica tanto migraciones de negocio como la de Quartz. No hace falta ejecutar ningún script con `sqlcmd` ni levantar otro contenedor.

Para aplicar explícitamente las migraciones antes de arrancar la API:

```cmd
docker-ach.cmd infra
docker-ach.cmd migrate
docker-ach.cmd up --no-seed
```

`--no-auto-migrations` únicamente es seguro si la base ya tiene las migraciones aplicadas, incluida `20261118110000_QuartzSqlServer471Schema`.

Para mantener un volumen existente utilizar `docker-ach.cmd down` (sin `-v`). Evitar `docker-ach.cmd reset --confirm-data-loss` salvo que se requiera eliminar todos los datos. Antes de actualizar una base real se recomienda un respaldo verificado.

Consultar [CORRECCION_DOCKER_QUARTZ_SCHEMA.md](CORRECCION_DOCKER_QUARTZ_SCHEMA.md) para las verificaciones SQL y el detalle de migración.
