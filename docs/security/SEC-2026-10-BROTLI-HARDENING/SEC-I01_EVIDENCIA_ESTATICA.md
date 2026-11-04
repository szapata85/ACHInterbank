# SEC-I01 — Evidencia de implementación estática (.NET 10)

**Fecha:** 2026-10-26  
**Incremento:** `SEC-I01` — CR-SEC-001  
**Estado:** `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`  
**ZIP base inmediato:** `ACHInterbank_ACH_CR_I06_FIX01_SEC_PLAN_MEMORY_COMPLETO.zip`  
**SHA-256 base:** `d7f7597aa225b441fa934f4abf25606596f4f718c5d80b26f5cddbc205cf15a7`

## 1. Cambios aplicados

- Runtime imagen ASP.NET Core `10.0.13` -> **`10.0.14`**.
- SDK imagen de compilación `10.0.402` -> **`10.0.403`**.
- SDK `global.json` `10.0.402` -> **`10.0.403`**, conserva `rollForward: latestPatch`.
- `RuntimeFrameworkVersion` de la API `10.0.13` -> **`10.0.14`**.
- **20 referencias NuGet directas** `10.0.13` -> `10.0.14`, exclusivamente familias Microsoft ASP.NET, EF Core, Extensions y System.Security.Cryptography; se conservan los paquetes de terceros y el proveedor Npgsql `10.0.3` sin cambios.
- Sin cambios a `.cs`, esquema/migraciones, NACHA-M, comunicaciones o jobs Quartz. Angular/Node/Nginx fuera de SEC-I01 (SEC-I02).

## 2. Inventario de referencias NuGet directas modificadas

| Proyecto | Referencia | Antes | Después |
|---|---|---|---|
| `src/Cfa.ACHInterbank.Api/Cfa.ACHInterbank.Api.csproj` | `Microsoft.EntityFrameworkCore.Design` | `10.0.13` | `10.0.14` |
| `src/Cfa.ACHInterbank.Api/Cfa.ACHInterbank.Api.csproj` | `Microsoft.EntityFrameworkCore.Tools` | `10.0.13` | `10.0.14` |
| `src/Cfa.ACHInterbank.Api/Cfa.ACHInterbank.Api.csproj` | `Microsoft.AspNetCore.OpenApi` | `10.0.13` | `10.0.14` |
| `src/Cfa.ACHInterbank.Api/Cfa.ACHInterbank.Api.csproj` | `System.Security.Cryptography.Pkcs` | `10.0.13` | `10.0.14` |
| `src/Cfa.ACHInterbank.Application/Cfa.ACHInterbank.Application.csproj` | `Microsoft.Extensions.DependencyInjection.Abstractions` | `10.0.13` | `10.0.14` |
| `src/Cfa.ACHInterbank.Application/Cfa.ACHInterbank.Application.csproj` | `Microsoft.Extensions.Logging.Console` | `10.0.13` | `10.0.14` |
| `src/Cfa.ACHInterbank.Application/Cfa.ACHInterbank.Application.csproj` | `System.Security.Cryptography.Xml` | `10.0.13` | `10.0.14` |
| `src/Cfa.ACHInterbank.External/Cfa.ACHInterbank.External.csproj` | `Microsoft.AspNetCore.Authentication.JwtBearer` | `10.0.13` | `10.0.14` |
| `src/Cfa.ACHInterbank.Persistence.Migrations.SqlServer/Cfa.ACHInterbank.Persistence.Migrations.SqlServer.csproj` | `Microsoft.EntityFrameworkCore.Design` | `10.0.13` | `10.0.14` |
| `src/Cfa.ACHInterbank.Persistence.Migrations.SqlServer/Cfa.ACHInterbank.Persistence.Migrations.SqlServer.csproj` | `Microsoft.EntityFrameworkCore.SqlServer` | `10.0.13` | `10.0.14` |
| `src/Cfa.ACHInterbank.Persistence.Migrations.SqlServer/Cfa.ACHInterbank.Persistence.Migrations.SqlServer.csproj` | `System.Security.Cryptography.Pkcs` | `10.0.13` | `10.0.14` |
| `src/Cfa.ACHInterbank.Persistence/Cfa.ACHInterbank.Persistence.csproj` | `Microsoft.EntityFrameworkCore` | `10.0.13` | `10.0.14` |
| `src/Cfa.ACHInterbank.Persistence/Cfa.ACHInterbank.Persistence.csproj` | `Microsoft.EntityFrameworkCore.SqlServer` | `10.0.13` | `10.0.14` |
| `src/Cfa.ACHInterbank.Persistence/Cfa.ACHInterbank.Persistence.csproj` | `System.Security.Cryptography.Pkcs` | `10.0.13` | `10.0.14` |
| `src/Cfa.ACHInterbank.Persistence/Cfa.ACHInterbank.Persistence.csproj` | `Microsoft.AspNetCore.DataProtection.EntityFrameworkCore` | `10.0.13` | `10.0.14` |
| `src/Cfa.ACHInterbank.Persistence/Cfa.ACHInterbank.Persistence.csproj` | `Microsoft.Extensions.Configuration.Abstractions` | `10.0.13` | `10.0.14` |
| `src/Cfa.ACHInterbank.Persistence/Cfa.ACHInterbank.Persistence.csproj` | `Microsoft.Extensions.Http` | `10.0.13` | `10.0.14` |
| `tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj` | `Microsoft.EntityFrameworkCore.Sqlite` | `10.0.13` | `10.0.14` |
| `tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj` | `Microsoft.EntityFrameworkCore.InMemory` | `10.0.13` | `10.0.14` |
| `tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj` | `Microsoft.AspNetCore.TestHost` | `10.0.13` | `10.0.14` |


## 3. Verificaciones disponibles en el ambiente de esta entrega

| Control | Resultado |
|---|---|
| Integridad JSON de `global.json` | `PASS_STATIC` |
| Parseo XML de los 6 `.csproj` intervenidos | `PASS_STATIC` |
| Integridad de etiquetas Docker y propiedad API | `PASS_STATIC` |
| Conteo de referencias modificadas | `PASS_STATIC` — 20 referencias en 6 proyectos |
| Ausencia de `10.0.13` en referencias directas activas de esos 6 `.csproj` | `PASS_STATIC` |
| No cambios en fuente `.cs` / contratos ACH / CENIT | `PASS_STATIC` mediante conjunto controlado de entradas modificadas |
| Diff de cambios textuales | `SEC-I01_DIFF_PATCH.txt` incluido |
| `dotnet restore/build/test/publish` | `NOT_RUN_ENVIRONMENT` (SDK ausente) |
| Construcción y escaneo Docker / runtime en contenedor | `NOT_RUN_ENVIRONMENT` (Docker ausente) |
| Resolución remota NuGet y compatibilidad real de todos los paquetes | `NOT_RUN_ENVIRONMENT` |
| Integración SQL Server/PostgreSQL y UAT monetario | `NOT_RUN_ENVIRONMENT` |

**Advertencia:** las entradas antiguas `obj/project.assets.json`, `obj/*.nuget.g.targets`, `bin` que ya existían en el ZIP original son **artefactos generados obsoletos** y pueden citar `10.0.13`; no se han editado ni deben usarse como prueba del grafo de dependencias corregido. Rehacer restore limpio y **no desplegar binarios preexistentes**.

## 4. Proveedores y consideraciones

- `Microsoft.EntityFrameworkCore.*` 10.0.14 aparece publicado en NuGet; conservar el proveedor `Npgsql.EntityFrameworkCore.PostgreSQL` 10.0.3 por ausencia de una necesidad verificada de modificarlo en SEC-I01. Probar interacciones EF en ambas BD.
- Las dependencias nativas de la imagen Linux (e.g. libgssapi y curl vía apt-get) requieren build y escaneo de imagen base. **No se verificaron digests de imagen**: antes de despliegue fijar digests realmente obtenidos de registry.
- No se crearon ni alteraron migraciones SQL; cualquier cambio en migraciones derivado de un futuro build requiere revisión separada.
- Las versiones corregidas se basan en Microsoft 25-oct-2026 y NuGet, pero la disponibilidad/compatibilidad para todas las plataformas se verifica al hacer restore y build. Nunca marcar PASS ejecutable por coincidencia estática.

## 5. Validación ejecutable pendiente — backlog arrastrado

```bash
# En raíz del repositorio, con SDK .NET 10.0.403
rm -rf src/*/obj tests/*/obj tools/*/obj  # solo artefactos generados, tras backup si aplica
dotnet --info
dotnet restore ACHInterbank.sln
dotnet build ACHInterbank.sln --no-restore -c Release
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --no-build
dotnet list ACHInterbank.sln package --vulnerable --include-transitive
dotnet publish src/Cfa.ACHInterbank.Api/Cfa.ACHInterbank.Api.csproj -c Release
# Solo donde Docker está disponible y con políticas del entorno
# docker compose build --pull --no-cache achinterbank-api
# docker exec <contenedor-api> dotnet --list-runtimes
```

> El comando `dotnet list package` se incluye como referencia del plan; en SDKs que usen la nueva sintaxis, adaptar a `dotnet package list`. Verificar comandos efectivos según la versión instalada.

## 6. Criterio de avance sin ambiente

- `GO_INCREMENT=YES`: avanzar a `SEC-I02-A_PATCH_NODE_NGINX_DOCKERFILE` aunque no esté disponible un runtime .NET/Docker.
- `GO_DEPLOY=NO`, `GO_LIVE_MONETARY=NO`: sin ejecución y aprobación de pruebas no hay evidencia de seguridad de producción.
- `SEC-I01` **no remedia** contraseñas SHA-256 ni credencial inicial (`SEC-I04`), ni Node/Nginx (`SEC-I02`).

## 7. Referencias externas usadas solo para selección de versión

- Microsoft: https://devblogs.microsoft.com/dotnet/dotnet-october-2026-oob-security-updates/
- Descargas .NET 10 (Runtime 10.0.14 / SDK 10.0.403): https://dotnet.microsoft.com/en-us/download/dotnet/10.0
- NuGet Gallery: https://www.nuget.org/packages/Microsoft.EntityFrameworkCore/10.0.14
- NuGet Gallery: https://www.nuget.org/packages/Microsoft.AspNetCore.Authentication.JwtBearer/10.0.14
- NuGet Gallery: https://www.nuget.org/packages/System.Security.Cryptography.Pkcs/10.0.14
