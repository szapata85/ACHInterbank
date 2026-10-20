# NAV6 - SQL Server Docker sin PowerShell

**Entorno:** NAV6 (.NET SDK 10 + Docker Compose). No requiere PowerShell ni Bash en Windows. El script `docker-ach.cmd` usa CMD, y el ejecutor `tools/AchInterbank.DockerCli` funciona también con `dotnet` en macOS/Linux. No crea `.env` ni duplica contraseñas en YAML.

## 1. Configuración única de contraseña

Edite **solo los apartados indicados** del archivo original `src/Cfa.ACHInterbank.Api/appsettings.Development.json`, **sin borrar las otras secciones**:

```json
"Database": {
  "Provider": "SqlServer",
  "ApplyMigrations": true,
  "ApplySeed": false
},
"ConnectionStrings": {
  "PostgresConnection": "",
  "SqlConnection": "Server=127.0.0.1,1433;Database=ACHInterbank;User Id=sa;Password=CAMBIAR_POR_CLAVE_FUERTE;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
}
```

La clave debe cumplir la política de SQL Server (recomendado 12+ caracteres con mayúsculas, minúsculas, números y símbolos). Si la clave incluye `;`, escríbala entre comillas dentro de la cadena ADO.NET, escapando esas comillas correctamente en JSON. No suba este archivo con la contraseña real a Git. En particular, si ya está versionado, `.gitignore` por sí solo NO impide su inclusión en commits.

> No utilice LocalDB ni `Trusted_Connection=True`: Docker SQL Server requiere autenticación `sa` para este stack local.

## 2. Ejecutar desde CMD en Windows

Abra **Símbolo del sistema (cmd.exe)** dentro de la carpeta donde está `ACHInterbank.sln`.

Validar Docker Compose sin imprimir contraseña:

```cmd
docker-ach.cmd config
```

Si necesita **borrar la base anterior de este stack** (operación destructiva, afecta al volumen persistente ACHInterbank):

```cmd
docker-ach.cmd reset --confirm-data-loss
```

Iniciar SQL Server, preparar Quartz y **aplicar las migraciones EF Core SQL Server existentes**:

```cmd
docker-ach.cmd migrate
```

Construir e iniciar API + Angular, **ejecutando el seed** sin repetir migraciones ya aplicadas:

```cmd
docker-ach.cmd up --seed --no-auto-migrations
```

**Arranque cotidiano, sin repetir el seed** (una vez inicializada la base de datos):

```cmd
docker-ach.cmd up
```

En `docker-compose.yml`, `Database__ApplySeed` ahora usa `${DATABASE_APPLY_SEED:-false}`:
por defecto **no se ejecuta `DbInitializer.SeedAllAsync()` en cada arranque**.
No cambia la aplicación de migraciones, que se mantiene habilitada por defecto en este stack.
El archivo `appsettings.Development.json` ya incluye `"ApplySeed": false`.

- **Primera instalación o actualización de catálogos:** ejecutar explícitamente `docker-ach.cmd up --seed` (o `--seed --no-auto-migrations` si las migraciones ya están aplicadas).
- **Operación desde Docker Desktop:** el botón *Play* de un contenedor ya creado conserva sus variables de entorno anteriores. Después de aplicar este cambio, use `docker-ach.cmd up --no-build` desde la carpeta del proyecto para que Docker Compose recree la API si detecta un cambio de configuración; luego podrá reiniciarla normalmente.
- **Ejecución manual desde la API:** se conserva `POST /Maintenance/seed`, autenticado y protegido por la política `MaintenanceSeed`.
- **Compose sin el ejecutor:** puede habilitar temporalmente `DATABASE_APPLY_SEED=true` si lo necesita; en el uso habitual déjelo ausente o en `false`.

La base de datos debe tener previamente los catálogos y permisos requeridos. Este ajuste no incluye un seed inicial ni garantiza que una base recién creada esté lista sin ejecutar la carga controlada.
Los Compose especializados de CI, PostgreSQL y clúster no se modifican.

Consultar estado y logs:

```cmd
docker-ach.cmd status
docker-ach.cmd logs
```

Detener el proyecto sin borrar los datos:

```cmd
docker-ach.cmd down
```

> `migrate` ejecuta `dotnet ef database update --project src/Cfa.ACHInterbank.Persistence.Migrations.SqlServer --startup-project src/Cfa.ACHInterbank.Api --context AchDbContext` con la cadena de conexión del appsettings apuntando al host. Debe disponer de la herramienta `dotnet ef` (`dotnet tool install --global dotnet-ef --version 10.*`, si no está instalada) y de .NET SDK 10.

Si no va a ejecutar EF manualmente, `docker-ach.cmd up` también puede aplicar migraciones al iniciar la API cuando `Database:ApplyMigrations=true`. Evite ejecutar ambas rutas simultáneamente.

## 3. Mismo flujo en otra terminal sin PowerShell

En Git Bash, Linux o macOS (si el host soporta SQL Server Docker y dispone de .NET SDK 10), use:

```sh
dotnet run --project tools/AchInterbank.DockerCli -- config
dotnet run --project tools/AchInterbank.DockerCli -- migrate
dotnet run --project tools/AchInterbank.DockerCli -- up --seed --no-auto-migrations
dotnet run --project tools/AchInterbank.DockerCli -- status
```

El ejecutor realiza las llamadas a Docker con el entorno de **cada proceso hijo** y no guarda la clave en otra fuente de configuración.

## 4. Conexión en SQL Server Management Studio

| Campo | Valor |
|---|---|
| Servidor | `127.0.0.1,1433` |
| Autenticación | SQL Server Authentication |
| Usuario | `sa` |
| Contraseña | La contraseña del `appsettings.Development.json` |
| Base de datos | `ACHInterbank` |
| Trust Server Certificate | Activado solo para desarrollo |

Verificación SQL:

```sql
USE ACHInterbank;
SELECT MigrationId, ProductVersion FROM dbo.__EFMigrationsHistory ORDER BY MigrationId;
SELECT COUNT(*) AS TotalUsuarios FROM dbo.Users;
```

API Docker `http://localhost:843`; Angular `http://localhost:743`. La API en Visual Studio puede usar `https://localhost:7269` con la misma cadena host `127.0.0.1,1433`.

## 5. Alcance, compatibilidad y seguridad

- `docker-compose.yml` mantiene exactamente los servicios de desarrollo SQL Server: `sqlserver`, `quartz-schema`, `achinterbank-api`, `achinterbank-spa`. El archivo `docker-compose.sqlserver.yml` queda como shim de compatibilidad; no es necesario incluirlo en los comandos nuevos.
- Los Compose especializados (Postgres, clúster, pruebas E2E, certificación) permanecen sin modificaciones, porque los usa infraestructura de pruebas distinta.
- `.dockerignore` impide copiar `appsettings.Development.json` durante el build de la API; el contenedor lo recibe mediante bind mount en ejecución. La cadena de conexión del contenedor se cambia a `sqlserver,1433` por variable de entorno.
- El valor de contraseña puede ser visible para quienes tengan acceso administrativo a Docker (por ejemplo, `docker inspect`). No es arquitectura de secretos de producción.
- `down` conserva los datos; `reset --confirm-data-loss` borra el volumen del stack. Eliminar contenedores no equivale a eliminar volúmenes. Una contraseña nueva no reconfigura la contraseña del volumen SQL inicializado anteriormente.
- El ejecutor no invoca PowerShell. Docker Compose incluye `/bin/bash` **dentro** del contenedor Linux `quartz-schema`, lo que no requiere Bash instalado en Windows.
- **Validación realizada:** análisis estático del YAML, archivos incluidos, rutas y código de arranque. **Validación pendiente en equipo destino:** compilar el ejecutor con .NET SDK 10, verificar `docker-ach.cmd config`, levantar SQL Server y ejecutar migraciones/seed. En el entorno de preparación no se dispone de Docker ni `dotnet`.
