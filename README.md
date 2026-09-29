# ACH Interbank

ACH Interbank es la solución para procesamiento operativo de transferencias interbancarias ACH Colombia y CENIT. El repositorio contiene backend .NET 10, persistencia EF Core para PostgreSQL/SQL Server, SPA Angular, pruebas, migraciones y documentación normativa/operativa.

## Arranque local desde Visual Studio 2026

La configuración de base de datos de Development se mantiene en `src/Cfa.ACHInterbank.Api/appsettings.Development.json`, en la sección solicitada:

```json
"ConnectionStrings": {
  "PostgresConnection": "",
  "SqlConnection": ""
}
```

`Database:Provider` determina cuál de las dos se usa. Si está en `SqlServer`, `SqlConnection` debe contener una cadena válida; si está en `Postgres`, debe existir `PostgresConnection`. Puede editar el JSON manualmente o usar:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\configure-visualstudio-development.ps1
```

El helper escribe la cadena seleccionada directamente en `appsettings.Development.json`; no usa .NET User Secrets. Si desea que además levante el SQL Server Docker del proyecto, use `-StartDocker`. **No versionar contraseñas reales en Git.** Consulte `docs/BUILD_AND_TROUBLESHOOTING.md` para SQL Server, autenticación integrada y PostgreSQL.


## Estado actual

Las capacidades cerradas internamente y los pendientes vigentes están consolidados en `docs/CURRENT_PROJECT_STATUS.md` y `MEMORY.md`.

**No interpretar el repositorio como RELEASE_READY/GO productivo.** La interoperabilidad externa CENIT Gateway/PO, la homologación del MFT empresarial de ACH Colombia y la certificación UAT/release del commit exacto siguen siendo dependencias separadas.

## Requisitos de desarrollo

- .NET SDK **10.0.300** o parche compatible según `global.json`.
- Visual Studio 2026 con workload de ASP.NET/.NET y soporte .NET 10, o CLI `dotnet`.
- Node/npm para `web/ach-interbank-ui`.
- PostgreSQL o SQL Server según el perfil de ejecución.

Ver `docs/BUILD_AND_TROUBLESHOOTING.md` antes del primer restore.

## Estructura principal

| Ruta | Propósito |
|---|---|
| `ACHInterbank.sln` | Solución principal .NET. |
| `src/Cfa.ACHInterbank.Api` | API ASP.NET Core. |
| `src/Cfa.ACHInterbank.Application` | Casos de uso, contratos y reglas de aplicación. |
| `src/Cfa.ACHInterbank.Domain` | Entidades y modelos de dominio. |
| `src/Cfa.ACHInterbank.Persistence` | DbContext, repositorios, servicios y migraciones PostgreSQL. |
| `src/Cfa.ACHInterbank.Persistence.Migrations.SqlServer` | **Proyecto de migraciones SQL Server; debe permanecer cargado en la solución.** |
| `src/Cfa.ACHInterbank.External` | Adaptadores e integraciones externas. |
| `tests/Cfa.ACHInterbank.Tests` | Pruebas automatizadas backend. |
| `web/ach-interbank-ui` | SPA Angular. |
| `docs/normativa` | Copias de trabajo de fuentes normativas y matrices de trazabilidad. |
| `docs/uat` | Planes, evidencias y material UAT. |
| `docs/operations` | Runbooks y evidencia operativa. |
| `docs/go-live-readiness` | Material histórico/de readiness; contrastar siempre con el estado vigente. |

## Validación previa al restore

Antes de abrir o restaurar la solución, se puede verificar la integridad del grafo de proyectos sin NuGet ni `dotnet`:

```powershell
python scripts/validate-solution-projects.py
```

El resultado esperado es:

```text
SOLUTION GRAPH: OK
  solution projects: 7
  disk projects:     7
  project references: all targets exist and are loaded by the solution
```

Este control evita la regresión que produjo `NU1105` al existir `Cfa.ACHInterbank.Persistence.Migrations.SqlServer.csproj` en disco y estar referenciado por la API, pero no estar incluido en `ACHInterbank.sln`.

## Restore, build y pruebas backend

Con Visual Studio cerrado, si vienes de una copia anterior o tienes errores de restore persistentes:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\repair-visualstudio-restore.ps1
```

O manualmente:

```powershell
python scripts/validate-solution-projects.py
dotnet restore ACHInterbank.sln
dotnet build ACHInterbank.sln -c Release
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --no-build
```

Las migraciones EF Core solo deben aplicarse en ambientes autorizados, con el proveedor correcto, respaldo y procedimiento operativo aprobado.

## SPA Angular

```powershell
cd web/ach-interbank-ui
npm ci
npm run build
npm test -- --watch=false --browsers=ChromeHeadless
```

## Fuentes normativas vigentes en este baseline

- ACH Colombia: Manual de Servicio Transferencias Interbancarias **V35, abril de 2026**.
- CENIT: Manual de Especificaciones Formato NACHA-M, **7 de mayo de 2026**, más DSP-152/Anexo 2 y anexos aplicables.
- V32 se conserva solo como histórico cuando una prueba o compatibilidad explícita lo requiere.
- V36 no se adopta como autoridad de implementación hasta una decisión posterior explícita.

La integridad de las copias normativas se documenta en `docs/normativa/SOURCE_MANIFEST.md`.

## Documentación canónica para continuar el proyecto

1. `docs/CURRENT_PROJECT_STATUS.md` — auditoría consolidada código vs. fuentes y backlog vigente.
2. `docs/BUILD_AND_TROUBLESHOOTING.md` — restore/build/NU1105 y recuperación de Visual Studio.
3. `docs/DOCUMENTATION_INDEX.md` — índice reducido de documentación vigente vs. histórica.
4. `MEMORY.md` — decisiones durables y estado técnico aceptado.
5. `AGENTS.md` — instrucciones permanentes para agentes de desarrollo.

Los documentos históricos bajo `docs/` siguen siendo evidencia útil, pero no deben usarse aisladamente para inferir el estado actual.

### Visual Studio 2026 quick start database

For the `Development` profile, the API uses SQL Server LocalDB through `ConnectionStrings:SqlConnection` and applies EF Core migrations automatically. This lets the API start directly from Visual Studio without first storing a database password in the repository. If you prefer the SQL Server 2025 Docker runtime, replace that one development connection string with your Docker `sa` connection and password.
