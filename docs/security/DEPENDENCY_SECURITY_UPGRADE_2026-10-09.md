# Actualización de dependencias y endurecimiento de vulnerabilidades — 2026-10-09

## Alcance

Actualización de dependencias directas y de tooling del proyecto ACHInterbank manteniendo .NET 10, Angular 21 LTS y las líneas mayores compatibles cuando una migración de major version implicaría cambios funcionales o de licencia. Se agregan gates de auditoría para dependencias directas y transitivas.

**Importante:** este host no tiene `dotnet` y no tiene acceso DNS al registro npm. Por lo tanto, aquí no se certifica “cero vulnerabilidades” ni se marca el artefacto como release-ready. El CI queda configurado para rechazar advisories NuGet (`NU1901`–`NU1904`) y cualquier advisory npm (`audit-level=low`) cuando se ejecute en un runner con red.

## Cambios NuGet directos

| Paquete | Antes | Ahora | Proyecto |
|---|---:|---:|---|
| `Microsoft.OpenApi` | `2.9.0` | `2.12.0` | `src/Cfa.ACHInterbank.Api/Cfa.ACHInterbank.Api.csproj` |
| `NLog` | `6.1.2` | `6.2.1` | `src/Cfa.ACHInterbank.Api/Cfa.ACHInterbank.Api.csproj` |
| `NLog.Extensions.Logging` | `6.1.2` | `6.2.1` | `src/Cfa.ACHInterbank.Api/Cfa.ACHInterbank.Api.csproj` |
| `NLog.Web.AspNetCore` | `6.1.2` | `6.2.1` | `src/Cfa.ACHInterbank.Api/Cfa.ACHInterbank.Api.csproj` |
| `Scalar.AspNetCore` | `2.13.22` | `2.17.14` | `src/Cfa.ACHInterbank.Api/Cfa.ACHInterbank.Api.csproj` |
| `AutoMapper` | `16.1.1` | `16.2.0` | `src/Cfa.ACHInterbank.Application/Cfa.ACHInterbank.Application.csproj` |
| `JWT` | `11.0.0` | `11.1.0` | `src/Cfa.ACHInterbank.Application/Cfa.ACHInterbank.Application.csproj` |
| `MediatR` | `14.1.0` | `14.2.0` | `src/Cfa.ACHInterbank.Application/Cfa.ACHInterbank.Application.csproj` |
| `Microsoft.AspNetCore.Http.Abstractions` | `2.3.9` | `2.3.13` | `src/Cfa.ACHInterbank.Application/Cfa.ACHInterbank.Application.csproj` |
| `Microsoft.AspNetCore.Mvc.Abstractions` | `2.3.9` | `2.3.13` | `src/Cfa.ACHInterbank.Application/Cfa.ACHInterbank.Application.csproj` |
| `Microsoft.AspNetCore.Mvc.Core` | `2.3.9` | `2.3.13` | `src/Cfa.ACHInterbank.Application/Cfa.ACHInterbank.Application.csproj` |
| `NLog` | `6.1.2` | `6.2.1` | `src/Cfa.ACHInterbank.Application/Cfa.ACHInterbank.Application.csproj` |
| `System.IdentityModel.Tokens.Jwt` | `8.17.0` | `8.23.0` | `src/Cfa.ACHInterbank.Application/Cfa.ACHInterbank.Application.csproj` |
| `Microsoft.Extensions.Http.Resilience` | `10.4.0` | `10.10.0` | `src/Cfa.ACHInterbank.External/Cfa.ACHInterbank.External.csproj` |
| `SSH.NET` | `2025.1.0` | `2026.0.0` | `src/Cfa.ACHInterbank.External/Cfa.ACHInterbank.External.csproj` |
| `System.IdentityModel.Tokens.Jwt` | `8.17.0` | `8.23.0` | `src/Cfa.ACHInterbank.External/Cfa.ACHInterbank.External.csproj` |
| `Npgsql.EntityFrameworkCore.PostgreSQL` | `10.0.1` | `10.0.3` | `src/Cfa.ACHInterbank.Persistence/Cfa.ACHInterbank.Persistence.csproj` |
| `Quartz` | `3.18.0` | `3.21.0` | `src/Cfa.ACHInterbank.Persistence/Cfa.ACHInterbank.Persistence.csproj` |
| `Quartz.Extensions.DependencyInjection` | `3.18.0` | `3.21.0` | `src/Cfa.ACHInterbank.Persistence/Cfa.ACHInterbank.Persistence.csproj` |
| `Quartz.Extensions.Hosting` | `3.18.0` | `3.21.0` | `src/Cfa.ACHInterbank.Persistence/Cfa.ACHInterbank.Persistence.csproj` |
| `Quartz.Serialization.Json` | `3.18.0` | `3.21.0` | `src/Cfa.ACHInterbank.Persistence/Cfa.ACHInterbank.Persistence.csproj` |
| `QuestPDF` | `2026.2.4` | `2026.9.1` | `src/Cfa.ACHInterbank.Persistence/Cfa.ACHInterbank.Persistence.csproj` |
| `Microsoft.NET.Test.Sdk` | `18.0.1` | `18.10.1` | `tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj` |
| `Moq` | `4.20.72` | `4.21.0` | `tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj` |
| `PdfPig` | `0.1.12` | `0.1.16` | `tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj` |
| `coverlet.collector` | `6.0.4` | `10.1.0` | `tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj` |

## Cambios frontend / tooling

| Tipo | Paquete | Antes | Ahora |
|---|---|---:|---:|
| `dependencies` | `@angular/animations` | `^21.2.19` | `^21.2.25` |
| `dependencies` | `@angular/common` | `^21.2.19` | `^21.2.25` |
| `dependencies` | `@angular/compiler` | `^21.2.19` | `^21.2.25` |
| `dependencies` | `@angular/core` | `^21.2.19` | `^21.2.25` |
| `dependencies` | `@angular/forms` | `^21.2.19` | `^21.2.25` |
| `dependencies` | `@angular/platform-browser` | `^21.2.19` | `^21.2.25` |
| `dependencies` | `@angular/platform-browser-dynamic` | `^21.2.19` | `^21.2.25` |
| `dependencies` | `@angular/router` | `^21.2.19` | `^21.2.25` |
| `dependencies` | `@material-symbols/font-400` | `^0.45.9` | `^0.47.6` |
| `dependencies` | `zone.js` | `~0.15.1` | `~0.16.3` |
| `devDependencies` | `@angular-devkit/build-angular` | `^21.2.19` | `^21.2.25` |
| `devDependencies` | `@angular/cli` | `^21.2.19` | `^21.2.25` |
| `devDependencies` | `@angular/compiler-cli` | `^21.2.19` | `^21.2.25` |
| `devDependencies` | `@playwright/test` | `^1.61.1` | `^1.62.1` |
| `devDependencies` | `@types/pg` | `^8.20.0` | `^8.23.1` |
| `devDependencies` | `javascript-obfuscator` | `^5.5.0` | `^5.9.0` |
| `devDependencies` | `pg` | `^8.22.0` | `^8.23.1` |
| `overrides` | `@modelcontextprotocol/sdk` | `^1.29.0` | `1.30.0` |
| `overrides` | `ajv` | `^8.20.0` | `8.20.0` |
| `overrides` | `body-parser` | `1.20.6` | `1.20.8` |
| `overrides` | `engine.io` | `6.6.9` | `6.6.11` |
| `overrides` | `express` | `4.22.2` | `4.22.3` |
| `overrides` | `hono` | `4.12.31` | `4.13.13` |
| `overrides` | `postcss` | `^8.5.22` | `8.5.29` |
| `overrides` | `qs` | `6.15.3` | `6.16.0` |
| `overrides` | `tar` | `^7.5.21` | `7.5.22` |
| `overrides` | `undici` | `^7.28.0` | `7.30.0` |
| `overrides` | `uuid` | `^14.0.0` | `14.0.2` |
| `overrides` | `ws` | `8.21.1` | `8.22.0` |

## Runtime, imágenes y CI

- .NET SDK fijado en `10.0.401` y runtime ASP.NET Core `10.0.12`.
- Node de build SPA fijado en `24.21.0-alpine` (LTS).
- npm del build SPA fijado en `12.1.0`.
- nginx fijado en `1.30.5-alpine`.
- GitHub Actions `upload-artifact` normalizado a `v6`.
- Workflows .NET/Node con versiones exactas (`10.0.401` / `24.21.0`) en lugar de comodines donde aplicaba.

## Controles de seguridad agregados

- `Directory.Build.props`: `NuGetAudit=true`, `NuGetAuditMode=all` y `NU1901;NU1902;NU1903;NU1904` como errores.
- CI .NET ejecuta además `dotnet list ACHInterbank.sln package --vulnerable --include-transitive`.
- CI Angular ejecuta `npm audit --omit=dev --audit-level=low` y `npm audit --audit-level=low`.
- `npm ci` continúa usando lockfile; no se reemplazó por `npm install`.

## Decisiones de compatibilidad

- Quartz se actualizó dentro de la línea 3.x (`3.21.0`) y no se forzó el salto a Quartz 4 sin migración/compilación.
- xUnit permanece en la línea v2 (`2.9.3`); NuGet la marca legacy y recomienda `xunit.v3`, pero esa migración es un cambio de framework de pruebas, no un patch de seguridad.
- FluentAssertions permanece en v6 para evitar una migración mayor/licenciamiento sin validación; el gate NuGet detectará advisories si aparecieran.
- Angular se mantiene en 21 LTS y se sube a `21.2.25`, evitando el salto de major a Angular 22 sin ciclo de migración.
- `@modelcontextprotocol/sdk` se mantiene en `1.30.0`, que es el nivel esperado por Angular CLI 21.2.25; no se fuerza 1.32.x porque su grafo cambia (incluye Express 5) y requiere regeneración/validación completa del lock.

## Validación ejecutada en este host

- `python scripts/validate-solution-projects.py`: **OK (7/7 proyectos)**.
- Parseo de todos los `.csproj`: **OK**.
- Parseo de todos los `.json`: **OK**.
- Parseo de workflows YAML: **OK**.
- Comprobación offline de `npm ci`: el lock superó la validación de consistencia y llegó a la fase de descarga; se detuvo únicamente porque el paquete `zone.js@0.16.3` no estaba en caché y el host está en modo sin red.
- `dotnet restore/build/test`: **NO EJECUTABLE en este host** (`dotnet` no está instalado).
- `npm audit`: **NO EJECUTABLE aquí contra el advisory DB** por falta de resolución DNS hacia `registry.npmjs.org`.

## Gate requerido antes de aprobar release

En un runner conectado ejecutar, en este orden:

```bash
python scripts/validate-solution-projects.py
dotnet restore ACHInterbank.sln
dotnet list ACHInterbank.sln package --vulnerable --include-transitive
dotnet build ACHInterbank.sln -c Release --no-restore
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --no-build
cd web/ach-interbank-ui
npm ci
npm audit --audit-level=low
npm run build
npm test -- --watch=false --browsers=ChromeHeadless
```

El gate debe quedar verde antes de afirmar “cero vulnerabilidades conocidas” para el árbol restaurado exacto.
