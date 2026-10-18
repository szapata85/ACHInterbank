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

Para el frente ACH Colombia FASE 1–8, el estado del paquete actual es **IMPLEMENTACIÓN COMPLETADA / VALIDACIÓN EJECUTABLE FINAL PENDIENTE**. El harness integral controlado de FASE 8 está preparado, pero en el host de preparación no pudieron ejecutarse build/tests .NET, Docker E2E ni la validación frontend completa. Esto **no** significa producción lista, homologación con ACH Colombia, UAT aprobada, aceptación de usuario ni salida a producción.

El resumen para continuar sin reconstruir toda la historia está en `docs/ACH_COLOMBIA_INTERNAL_CLOSURE.md`. El estado general se consolida en `docs/CURRENT_PROJECT_STATUS.md` y `MEMORY.md`; la clasificación de pendientes propios, externos y UAT está en `docs/PENDING_WORK_CLASSIFICATION.md`.

El programa **NACHA-CONFIG-UX-001** quedó **CLOSED** en UX6 con `NEXT_ALLOWED_PHASE=NONE`. La parametrización NACHA-M conserva motor/goldens/migraciones intactos y cierra el endurecimiento de accesibilidad, Angular Material, branding dinámico, navegación, estados, protección de `PUBLICADO`, publicación fail-closed y aislamiento ACH/CENIT. Los gates Angular/Playwright/.NET no ejecutables en este host están documentados como `NOT_RUN_ENVIRONMENT`, no como PASS; ver `docs/NACHA_CONFIG_UX_IMPLEMENTATION_PACKAGE_2026-10-11/NACHA_CONFIG_UX_STATE.md`.

**No interpretar el repositorio como RELEASE_READY/GO productivo.** La interoperabilidad externa CENIT Gateway/PO, la homologación MFT/SFTP de ACH Colombia y la certificación UAT/release del artefacto exacto siguen siendo dependencias separadas.

## Requisitos de desarrollo

- .NET SDK **10.0.402** o parche compatible según `global.json`.
- Visual Studio 2026 con workload de ASP.NET/.NET y soporte .NET 10, o CLI `dotnet`.
- Node.js **24.21.0 LTS** y npm **12.1.0** para `web/ach-interbank-ui` (mismos niveles fijados en Docker/CI).
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
2. `docs/ACH_COLOMBIA_INTERNAL_CLOSURE.md` — handoff corto FASE 1–9 y control final pendiente.
3. `docs/BUILD_AND_TROUBLESHOOTING.md` — restore/build/NU1105 y recuperación de Visual Studio.
4. `docs/DOCUMENTATION_INDEX.md` — índice reducido de documentación vigente vs. histórica.
5. `MEMORY.md` — decisiones durables y estado técnico aceptado.
6. `AGENTS.md` — instrucciones permanentes para agentes de desarrollo.

Los documentos históricos bajo `docs/` siguen siendo evidencia útil, pero no deben usarse aisladamente para inferir el estado actual.

### Visual Studio 2026 quick start database

For the `Development` profile, the API uses SQL Server LocalDB through `ConnectionStrings:SqlConnection` and applies EF Core migrations automatically. This lets the API start directly from Visual Studio without first storing a database password in the repository. If you prefer the SQL Server 2025 Docker runtime, replace that one development connection string with your Docker `sa` connection and password.

## Programa de administración de terceros y cuentas — TPA-GOV-001

El paquete incluye un programa incremental para evolucionar `CustomerThirdParty` hacia **master data genérico** cliente → tercero → cuentas, **preservando el onboarding silencioso existente en creación individual y masiva de transacciones**. El onboarding es independiente de ACH Colombia/CENIT; la cámara aparece después, únicamente en elegibilidad y validaciones operativas. El programa mantiene la resolución automática de prenotificaciones y usa branch-by-abstraction para evitar un corte big bang.

Inicio: `docs/ai/third-party-accounts/START_HERE.md`. La primera fase permitida es **TPA0 (characterization/baseline únicamente)**. El prompt optimizado para ChatGPT modo Chat está en `docs/ai/third-party-accounts/MASTER_PROMPT_CHATGPT.md`.


## CFA SOAP UX + Mapping dinámico — programa incremental

La implementación del rediseño de administración CFA y del mapping SOAP versionado para `Proc_Contrapartidas`, `Proc_Transacciones` y `RegistrarRespuestaTransaccion` está definida en:

`docs/CFA_SOAP_UX_MAPPING_IMPLEMENTATION_PACKAGE_2026-10-12/`

Para una conversación nueva de ChatGPT use el prompt mínimo de `CHATGPT_MICRO_START.md`. El estado canónico `MEMORY_STATE.md` indica el único `next_micro_prompt` permitido, de modo que cada conversación ejecute un cambio pequeño y verificable.
