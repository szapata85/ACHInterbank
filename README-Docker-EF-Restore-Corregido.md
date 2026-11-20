> **Actualización 2026-11-18:** el inicializador Docker `quartz-schema` ya no se usa en SQL Server: el esquema de Quartz.NET 4.7.1 se incorpora mediante la migración EF Core `20261118110000_QuartzSqlServer471Schema`.

# ACHInterbank NAV6 — corrección de migraciones EF SQL Server

## Causa del error

Quartz termina exitosamente (`Exited (0)`). A continuación `docker-ach.cmd migrate` fallaba porque el SDK .NET estaba disponible, pero faltaba restaurar la herramienta local `dotnet-ef` declarada en `.config/dotnet-tools.json` (versión 10.0.13). Sin aplicar las migraciones, el seed fallaba con `Invalid object name 'IntegrationMethods'`.

## Corrección incorporada

`tools/AchInterbank.DockerCli/Program.cs` ahora ejecuta **`dotnet tool restore`** automáticamente antes de llamar a **`dotnet ef database update`** durante la acción `migrate`. El ejecutor ahora solo espera a SQL Server; ya no consulta ni inicia un contenedor `quartz-schema`. El BAT y CMD siguen lanzando el ejecutor .NET. No se requiere PowerShell.

## Uso desde CMD en la raíz del proyecto

```bat
docker-ach.cmd migrate
docker-ach.cmd up --seed --no-auto-migrations --no-build
docker-ach.cmd status
```

Si `dotnet tool restore` informa un problema de NuGet o de conectividad, resuélvelo antes de ejecutar `up`; en una instalación nueva la primera restauración requiere acceso al origen de paquetes configurado. Una alternativa manual equivalente es `dotnet tool restore` seguida de `docker-ach.cmd migrate`.

## Verificación SQL Server Management Studio

```sql
USE ACHInterbank;
SELECT OBJECT_ID(N'dbo.IntegrationMethods', N'U') AS IntegrationMethodsObjectId;
SELECT MigrationId FROM dbo.__EFMigrationsHistory ORDER BY MigrationId;
```

`IntegrationMethodsObjectId` debe contener un identificador no nulo cuando la tabla exista en `dbo`. El prefijo de esquema puede variar si tu modelo fue personalizado. Si el historial dice que está migrado, pero falta la tabla, no generes ni borres migraciones sin inspeccionar el esquema.

## Precauciones

- Conserva tu `src/Cfa.ACHInterbank.Api/appsettings.json` actual (con tu contraseña local de `sa`) al copiar archivos del ZIP. El ZIP distribuido no incluye una contraseña SQL real. Configúrala localmente sin versionarla.
- No uses `reset` para corregir este error: eliminaría el volumen y los datos SQL Server.
- El ZIP completo contiene el proyecto previamente entregado más esta corrección; el ZIP pequeño incluye solamente el ejecutor modificado y esta guía.
- Se validaron el contenido del archivo ZIP y los cambios de código. No se pudo ejecutar Docker o .NET en este entorno.
