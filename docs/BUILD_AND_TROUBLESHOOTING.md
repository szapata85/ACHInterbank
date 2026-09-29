# Build y troubleshooting — ACHInterbank

**Baseline:** 2026-09-29  
**SDK requerido:** .NET 10.0.300 (`global.json`, `rollForward=latestPatch`)

## 1. NU1105 de `Cfa.ACHInterbank.Persistence.Migrations.SqlServer`

### Causa confirmada

El proyecto físico existe en:

```text
src/Cfa.ACHInterbank.Persistence.Migrations.SqlServer/
  Cfa.ACHInterbank.Persistence.Migrations.SqlServer.csproj
```

`Cfa.ACHInterbank.Api.csproj` lo referencia mediante `ProjectReference`. En la copia recibida, ese proyecto no estaba cargado en `ACHInterbank.sln`. Visual Studio/NuGet intentaba resolver una referencia a un proyecto que existía en disco pero no formaba parte del grafo cargado por la solución, produciendo `NU1105` también de forma transitiva en el proyecto de pruebas.

### Corrección aplicada

`ACHInterbank.sln` ahora incluye los siete proyectos .NET presentes en `src/` y `tests/`, incluido:

```text
Cfa.ACHInterbank.Persistence.Migrations.SqlServer
```

Además se agregó `scripts/validate-solution-projects.py`, que falla si:

- un `.csproj` presente en `src/` o `tests/` no está en la solución;
- la solución apunta a un `.csproj` inexistente;
- un `ProjectReference` apunta a un archivo inexistente; o
- un `ProjectReference` válido apunta a un proyecto que no está cargado en la solución.

El workflow `.github/workflows/dotnet-ci.yml` ejecuta este control antes del restore, de modo que una futura omisión de proyecto en la solución falle con una causa explícita y no reaparezca como `NU1105`.

## 2. Recuperación limpia en Visual Studio 2026

Cerrar Visual Studio antes de ejecutar el saneamiento. La caché `.vs` y los directorios `bin/obj` son regenerables.

Opción recomendada:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\repair-visualstudio-restore.ps1
```

El script valida el SDK, comprueba el grafo, elimina solo cachés de compilación locales, ejecuta restore y build Release. Las pruebas pueden omitirse con `-SkipTests`.

Secuencia manual equivalente:

```powershell
dotnet --info
dotnet --list-sdks
python scripts/validate-solution-projects.py

Remove-Item -Recurse -Force .vs -ErrorAction SilentlyContinue
Get-ChildItem -Recurse -Directory -Filter bin | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
Get-ChildItem -Recurse -Directory -Filter obj | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue

dotnet nuget locals all --clear
dotnet restore ACHInterbank.sln
dotnet build ACHInterbank.sln -c Release --no-restore
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --no-build
```

## 3. Qué debe mostrar el validador

```text
SOLUTION GRAPH: OK
  solution projects: 7
  disk projects:     7
  project references: all targets exist and are loaded by the solution
```

Si aparece otro número, no continuar con una corrección manual de NuGet hasta revisar la solución y los `ProjectReference`.

## 4. Requisitos de SDK

El repositorio usa `net10.0` en todos sus proyectos .NET y fija:

```json
{
  "sdk": {
    "version": "10.0.300",
    "rollForward": "latestPatch"
  }
}
```

Si `dotnet --list-sdks` no muestra un SDK 10.0.3xx compatible, instalar/actualizar el SDK o el workload de Visual Studio antes de investigar errores secundarios de restore.

## 5. Migraciones SQL Server y PostgreSQL

- Las migraciones SQL Server residen en el proyecto dedicado `Cfa.ACHInterbank.Persistence.Migrations.SqlServer`.
- PostgreSQL conserva sus migraciones bajo Persistence.
- No mover migraciones de proveedor para "resolver" un restore.
- No eliminar el `ProjectReference` SQL Server desde API: forma parte del diseño actual.
- No aplicar migraciones a una base compartida/productiva durante una validación de compilación.

## 6. Límite de la validación de este paquete

En el entorno usado para preparar este paquete no está instalado el SDK `dotnet`, por lo que la corrección se validó estáticamente mediante:

- existencia y XML válido de los siete `.csproj`;
- correspondencia completa entre `.csproj` y `ACHInterbank.sln`;
- resolución de todos los `ProjectReference`;
- revisión del grafo de dependencias y del `global.json`.

El build/test final debe ejecutarse en tu estación con .NET 10/Visual Studio 2026. No se declara compilación exitosa sin esa evidencia.

## 7. Error al iniciar: `No configured connection string was found for provider 'SqlServer'`

Este error ocurre **después de compilar** y es distinto de `NU1105`. La aplicación llegó a `AddPersistence`, pero el proveedor configurado no tiene una cadena de conexión no vacía.

La configuración local se mantiene directamente en `src/Cfa.ACHInterbank.Api/appsettings.Development.json`:

```json
"Database": {
  "Provider": "SqlServer"
},
"ConnectionStrings": {
  "PostgresConnection": "",
  "SqlConnection": ""
}
```

Con `Provider = SqlServer`, `SqlConnection` debe contener una cadena válida. Con `Provider = Postgres`, debe contenerse `PostgresConnection`. No existe fallback silencioso a otra base.

### Visual Studio + SQL Server existente/Docker

Puede editar la cadena manualmente o ejecutar desde la raíz:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\configure-visualstudio-development.ps1
```

El helper solicita la contraseña y escribe `Database:Provider`, `Database:ApplyMigrations`, `Database:ApplySeed` y la cadena elegida **directamente en `appsettings.Development.json`**.

Para iniciar además el SQL Server Docker del proyecto:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\configure-visualstudio-development.ps1 -StartDocker
```

Para una base local con autenticación integrada de Windows:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\configure-visualstudio-development.ps1 -IntegratedSecurity
```

Para aplicar también el seed de desarrollo:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\configure-visualstudio-development.ps1 -StartDocker -ApplySeed
```

### PostgreSQL

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\configure-visualstudio-development.ps1 `
  -Provider Postgres -HostName localhost -Port 5432 -Database ACHInterbank -UserName postgres
```

Después reinicie el perfil `https`, `http` o `IIS Express` en Visual Studio.

> Nota: este mecanismo responde a la decisión del proyecto de mantener las cadenas bajo `ConnectionStrings` en el archivo de configuración de Development. Evite confirmar a Git contraseñas reales o copiar credenciales de UAT/Producción a este archivo.

## Visual Studio 2026: default Development database

The `Development` profile now contains a non-secret SQL Server LocalDB connection under the canonical `ConnectionStrings` section:

```json
"ConnectionStrings": {
  "PostgresConnection": "",
  "SqlConnection": "Server=(localdb)\\MSSQLLocalDB;Database=ACHInterbank;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
}
```

`Database:ApplyMigrations` is enabled in Development, so the SQL Server schema is created/updated automatically when the API starts. `Database:ApplySeed` remains disabled to avoid silently changing reference/business data.

If the workstation uses the repository SQL Server 2025 Docker runtime instead of LocalDB, replace only `ConnectionStrings:SqlConnection` with the local Docker connection string and keep `Database:Provider` as `SqlServer`. The Docker runtime requires the operator-provided `MSSQL_SA_PASSWORD`; no real password is committed to source control.
