# Actualización .NET 10.0.12 / SDK 10.0.401 — 2026-10-09

## Objetivo

Alinear el repositorio ACHInterbank con el servicing vigente de .NET 10 solicitado para este paquete:

- Runtime / ASP.NET Core Runtime: **10.0.12**.
- SDK: **10.0.401**.
- Target Framework: se conserva **`net10.0`**.

La página oficial de descargas de Microsoft para .NET 10 reporta 10.0.12 como security patch y SDK 10.0.401, con .NET Runtime y ASP.NET Core Runtime 10.0.12 incluidos en ese SDK:

- https://dotnet.microsoft.com/en-us/download/dotnet/10.0

## Cambios aplicados

1. `global.json` fijado a SDK `10.0.401` con `rollForward=latestPatch`.
2. Docker API actualizado a:
   - `mcr.microsoft.com/dotnet/sdk:10.0.401`
   - `mcr.microsoft.com/dotnet/aspnet:10.0.12`
3. `RuntimeFrameworkVersion` de la API actualizado a `10.0.12`.
4. Paquetes Microsoft/System ligados al servicing .NET 10 actualizados a `10.0.12` donde aplica: ASP.NET Core, EF Core, Microsoft.Extensions y `System.Security.Cryptography.Xml`.
5. `dotnet-ef` local actualizado a `10.0.12`.
6. GitHub Actions, setup Codex, UAT y documentación operativa actualizados a SDK `10.0.401`.
7. No se alteraron los metadatos `ProductVersion` históricos de migraciones EF ya generadas.
8. No se modificaron paquetes con versionado independiente, por ejemplo `Microsoft.Extensions.Http.Resilience 10.4.0`, `System.ServiceModel.* 10.0.652802`, Quartz, Npgsql u otros terceros.

## Validación ejecutada en este entorno

- `python scripts/validate-solution-projects.py`: **OK**.
- Los 7 `.csproj` de `src/` y `tests/` se parsearon como XML válido: **OK**.
- Revisión estática de referencias activas: no quedan pins operativos `10.0.203`, `10.0.300`, `10.0.301`, runtime `10.0.8` ni paquetes servicing Microsoft `10.0.5/10.0.8/10.0.9` en la configuración activa actualizada.
- La disponibilidad oficial de .NET Runtime 10.0.12 y SDK 10.0.401 fue contrastada con Microsoft.
- La disponibilidad de los paquetes NuGet principales `10.0.12` y `dotnet-ef 10.0.12` fue contrastada con NuGet.org.

## Limitación de evidencia

Este host de preparación no tiene `dotnet` ni Docker instalados y no permite descargar el SDK desde el shell, por lo que aquí no fue posible ejecutar `dotnet restore`, `dotnet build`, `dotnet test` ni construir la imagen Docker.

El gate ejecutable final en un host con conectividad/toolchain debe ser:

```powershell
python scripts/validate-solution-projects.py
dotnet --version
dotnet --info
dotnet tool restore
dotnet restore ACHInterbank.sln
dotnet build ACHInterbank.sln -c Release
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --no-build
```

Se espera `dotnet --version` = `10.0.401` y runtimes `Microsoft.NETCore.App 10.0.12` / `Microsoft.AspNetCore.App 10.0.12` disponibles.
