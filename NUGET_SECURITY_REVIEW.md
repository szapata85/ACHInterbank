# NuGet Security Review — 2026-10-15

## Motivo

Visual Studio/NuGet reportaba NU1902/NU1903 como errores para `System.Security.Cryptography.Xml` 10.0.12 y `System.Security.Cryptography.Pkcs` 10.0.12/9.0.11. El repositorio mantiene intencionalmente `NuGetAudit=true`, `NuGetAuditMode=all` y NU1901–NU1904 como errores; no se deshabilitó ni suprimió la auditoría.

## Cambios aplicados

- .NET SDK: `10.0.401` -> `10.0.402` (`global.json`, CI y scripts).
- Runtime/ASP.NET Core: `10.0.12` -> `10.0.13` donde estaba fijado.
- Paquetes Microsoft de la línea de servicing .NET/ASP.NET/EF Core: `10.0.12` -> `10.0.13`.
- `System.Security.Cryptography.Xml`: `10.0.12` -> `10.0.13`.
- `System.Security.Cryptography.Pkcs`: pin directo `10.0.13` en el proyecto API para evitar la resolución vulnerable transitiva observada como 9.0.11.
- `dotnet-ef`: `10.0.12` -> `10.0.13`.
- Docker: runtime `10.0.13`, SDK `10.0.402`.
- El script `scripts/repair-visualstudio-restore.ps1` ahora ejecuta auditoría directa y transitiva después de restaurar.

## Validación realizada en el paquete

Se validó estáticamente que los 7 proyectos de la solución existen, sus `ProjectReference` resuelven correctamente y los 7 `.csproj` son XML válido. También se verificó la ausencia de referencias activas a `10.0.12`/`10.0.401` en proyectos, Docker, workflows y scripts (documentos históricos pueden conservar esas versiones).

Este entorno de revisión no dispone de `dotnet`, por lo cual la resolución real del grafo NuGet, compilación y pruebas deben ejecutarse en una estación con .NET SDK 10.0.402 y acceso a los feeds configurados.

## Comandos de verificación local

Desde la raíz de la solución:

```powershell
dotnet --version
dotnet restore ACHInterbank.sln
dotnet list ACHInterbank.sln package --vulnerable --include-transitive
dotnet build ACHInterbank.sln -c Debug --no-restore
dotnet test ACHInterbank.sln -c Debug --no-build
```

O ejecutar el flujo de reparación/validación incluido:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\repair-visualstudio-restore.ps1
```

## Correcciones adicionales de compilación/restauración — 2026-10-15

Tras la primera ejecución en Visual Studio 2026 se corrigieron además:

- `Microsoft.OpenApi`: `2.12.0` -> `2.12.2`. `Microsoft.AspNetCore.OpenApi` 10.0.13 exige `Microsoft.OpenApi >= 2.12.2 && < 3.0.0`; esto elimina `NU1605` sin saltar a la rama 3.x incompatible con ese rango.
- `GetTokenWithClientAssertion`: se introdujo un alias explícito para `Cfa.ACHInterbank.Domain.Models.Configurations.Integrations`, porque el namespace hermano `Cfa.ACHInterbank.Application.Integrations` ocultaba el tipo y producía `CS0118`.
- `LoadBalancer`: se agregó el `using Cfa.ACHInterbank.Domain.Models.Configurations;` requerido para resolver `SingletonAttribute`, eliminando `CS0246`.

Estas correcciones no deshabilitan `TreatWarningsAsErrors`, `NuGetAudit` ni las reglas NU1901–NU1904.

## Correcciones adicionales de compilación — ronda 2

Tras la siguiente ejecución en Visual Studio 2026 se corrigieron:

- `CS9000` en la migración PostgreSQL `20261012231000_IntegrationsAddContractVersioningBaseline.cs`: el delimitador de cierre de la cadena raw multilínea (`"""`) quedó en su propia línea, como exige C#.
- Se corrigió preventivamente el mismo defecto en la migración equivalente de SQL Server `20261012231100_IntegrationsAddContractVersioningBaseline.cs`, antes de que apareciera al compilar ese proyecto.
- `CS0246` en `AuthenticationService.cs`: se agregó `using Cfa.ACHInterbank.Domain.Models.Configurations;` para resolver `ScopedAttribute`.
- La revisión global de los atributos de lifetime encontró otros tres archivos que usaban `[Scoped]` sin importar el namespace del atributo; también se corrigieron preventivamente:
  - `NachaConfigHistoryService.cs`
  - `TransactionThirdPartyAccountSelectorService.cs`
  - `ConnectionsStructuralMenuAuthorizationSeeder.cs`

Validación estática posterior: 7 `.csproj` XML válidos, todos los `ProjectReference` existentes, 2.100 archivos C# inspeccionados, 0 usos detectados de `[Scoped]`/`[Singleton]`/`[Transient]` sin el namespace requerido y 0 cierres de cadenas raw multilínea detectados fuera de su propia línea.

El entorno de revisión continúa sin `dotnet`, por lo que el `restore/build/test` real debe ejecutarse en la estación de desarrollo.

## Correcciones adicionales de compilación — ronda 3

Tras la siguiente ejecución en Visual Studio 2026 se corrigieron:

- `CS0234`/`CS0246` en `AchDbContext.cs` para `Microsoft.AspNetCore.Http` e `IHttpContextAccessor`: `Cfa.ACHInterbank.Persistence` es un proyecto `Microsoft.NET.Sdk` pero el contexto usa `IHttpContextAccessor`; se agregó `FrameworkReference` a `Microsoft.AspNetCore.App`. Se prefirió la referencia al shared framework sobre introducir un paquete legado `Microsoft.AspNetCore.Http.*`.
- `CS0246` en `Cfa.ACHInterbank.External/DependencyInjectionService.cs`: se agregó el `using Cfa.ACHInterbank.Application.Services.EncryptionService.Interfaces;` requerido por `IEncryptionService`.
- `CS0246` para `AchManagedFileExecutionOrigin` en `AchColombiaManagedFileExchangeService.cs`: se agregó `using Cfa.ACHInterbank.Domain.Models.ACH;`.
- Los dos `CS0535` de `AchColombiaManagedFileExchangeService` eran consecuencia del tipo `AchManagedFileExecutionOrigin` no resuelto; al resolver el enum, las firmas vuelven a coincidir exactamente con `IAchColombiaManagedMftExecutionService`.

Validación estática posterior: grafo de 7 proyectos correcto, todos los `.csproj` XML válidos, 0 usos detectados de `IEncryptionService` sin su namespace y 0 usos detectados de `AchManagedFileExecutionOrigin` sin el namespace del modelo de dominio. El entorno de revisión continúa sin `dotnet`, por lo que la compilación real debe confirmarse en la estación de desarrollo.

## Build fix 4 — PKCS transitivo en Persistence

Visual Studio continuó reportando `System.Security.Cryptography.Pkcs 9.0.11` al restaurar `Cfa.ACHInterbank.Persistence` de forma independiente. La causa está en el grafo de SQL Server: `Microsoft.EntityFrameworkCore.SqlServer 10.0.13` depende de `Microsoft.Data.SqlClient >= 6.1.7`; para su grupo `net9.0` (compatible con `net10.0`), `Microsoft.Data.SqlClient 6.1.7` permite `System.Security.Cryptography.Pkcs >= 9.0.11`. Al no existir un pin directo dentro del proyecto Persistence, NuGet puede resolver el mínimo 9.0.11, que quedó afectado por los advisories de octubre de 2026.

Se agregó un `PackageReference` directo a `System.Security.Cryptography.Pkcs` `10.0.13` en:

- `Cfa.ACHInterbank.Persistence`
- `Cfa.ACHInterbank.Persistence.Migrations.SqlServer` (preventivamente, porque también referencia EF Core SQL Server y se restaura/audita como proyecto independiente)

El pin existente en `Cfa.ACHInterbank.Api` se conserva. No se deshabilitó `NuGetAudit` ni se suprimieron `NU1902/NU1903`.

## Build fix 5 — errores C# de Persistence

Tras la siguiente compilación en Visual Studio 2026 se corrigieron los errores de compilación reportados en `Cfa.ACHInterbank.Persistence`:

- `CS0176` en `IncomingNachaPostProcessingPipeline`: cinco métodos declarados `static` en `IncomingNachaStateTransitionStage` se estaban invocando mediante la instancia `_stateTransition`. Las llamadas ahora se califican con el tipo `IncomingNachaStateTransitionStage`.
- `CS0103` para `BindingFlags` y `JsonSerializer` en `NachaFileGenerationCoordinator`: se agregaron `using System.Reflection;` y `using System.Text.Json;`.
- `CS0103` para `NachaReceiverNameHelper` y `DigitoChequeoHelper` en `NachaInboundValidation`: se agregó `using Cfa.ACHInterbank.Domain.Helpers;`.
- `CS0103` para `RegulatoryCycleScheduleCatalog` en `SchedulerSyncService`: se agregó `using Cfa.ACHInterbank.Application.ACH.Models;`.
- `CS0120` en `SchedulerSyncService`: `MarkSynchronizationSucceededAsync` y `MarkSynchronizationFailedAsync` dejaron de ser `static`, ya que usan el campo de instancia `_timeProvider`.
- `CS0103` para `TransactionPrerequisitePolicyService` en `AchTacitAcceptanceJobHandler`: se agregó el namespace de `Cfa.ACHInterbank.Persistence.ACH.Services.Implementation`.
- `CS0136` en `IntegrationMappingSetService`: se eliminaron colisiones de alcance del identificador `source` usando nombres explícitos (`exactSource`, `semanticSource`, `removedSource`).

Los `CS0006` reportados en API, migraciones y tests eran errores en cascada: esos proyectos no podían encontrar los ensamblados de referencia porque `Cfa.ACHInterbank.Persistence` no había logrado compilar. No se agregó ningún workaround sobre referencias binarias; deben desaparecer una vez compile Persistence.

Se ejecutó `python scripts/validate-solution-projects.py` y el grafo de la solución quedó `OK`: 7 proyectos en disco, 7 en la solución y todos los `ProjectReference` válidos. El entorno de revisión sigue sin disponer del SDK `dotnet`, por lo que la compilación real debe confirmarse en la estación de desarrollo.

## Build fix 6 — opciones DigitalEnvelope en API

Partiendo del source `ACHInterbank-NAV6.zip`, `Program.cs` usa `DigitalEnvelopeCertificateOptions` y `DigitalEnvelopeSignatureValidationOptions`, ambos definidos en `Cfa.ACHInterbank.Domain.Models.Configurations`. Se agregó `using Cfa.ACHInterbank.Domain.Models.Configurations;` para evitar los `CS0246` reportados previamente.

## Build fix 7 — tests OpenAPI / reflection e interceptores

La ronda actual se aplicó directamente sobre el source `ACHInterbank-NAV6.zip` y corrige:

- `CS0246` para `MethodInfo` (y de forma asociada `BindingFlags`) en `OpenApiDocumentGenerationTests.cs`: se agregó `using System.Reflection;`.
- `CS9137` emitido por `Microsoft.AspNetCore.OpenApi.SourceGenerators.XmlCommentGenerator` en el proyecto de tests: se habilitó el namespace de interceptores generado con `<InterceptorsNamespaces>$(InterceptorsNamespaces);Microsoft.AspNetCore.OpenApi.Generated</InterceptorsNamespaces>` dentro de `Cfa.ACHInterbank.Tests.csproj`, exactamente en el proyecto que compila el código generado.

No se deshabilitó el generador OpenAPI, no se silenció `CS9137`, y no se modificó `NuGetAudit` ni la política NU1901–NU1904.


## Build fix 8 — consolidación de errores de tests sobre `ACHInterbank-NAV6.zip`

Esta ronda conserva como base autoritativa el `ACHInterbank-NAV6.zip` de `sources` (que ya incorpora los fixes 1–6) y mantiene acumulativamente el fix 7. Se corrigieron los errores reportados al compilar `Cfa.ACHInterbank.Tests`:

- `CS0117`: `NavigationInformationArchitectureSeeder.ParametersRootRoute` era un nombre obsoleto; el contrato público vigente es `ParametersAdministrationRoute`.
- `CS1503`: `IncomingNachaCommandCenterService` ya no recibe `AchDbContext` + state machine directamente; el test ahora construye las capacidades segregadas `IncomingNachaCommandCenterQueryService` y `IncomingNachaCommandCenterOperationsService` y las pasa al facade.
- `CS1503`: `PagedResponse<T>.Items` es `IEnumerable<T>`; las aserciones de paginación usan `Count()` en vez del method group `Count`.
- `CS1503`: los identificadores de permisos y roles de navegación son `Guid`; el helper `AssertAuthorizationEqual` ahora trabaja con `IReadOnlyDictionary<int, Guid[]>`.
- `CS0246`: se agregó el namespace de `CustomerThirdPartyQuery` (`Cfa.ACHInterbank.Domain.Entities.Transactions.Dtos`) en `ThirdPartyAccountBackfillServiceTests`.
- `CS0246`: se agregó el namespace de `IProcContrapartidasFunctionalMappingResolver` (`Cfa.ACHInterbank.Application.Integrations.Interfaces`) en `IntegrationContractDrivenSoapBodyWriterTests`.
- `CS0246` para `JwksController`, `ServersController` y `TestsController`: el source API los excluye intencionalmente con `Compile Remove`. Los tests OpenAPI ya no reintroducen referencias de tipo en tiempo de compilación; conservan las 8 rutas históricamente excluidas en el inventario y validan que esos tres controllers no formen parte del assembly de producción. No se reactivaron endpoints internos/sensibles.
- `CS7036`: los tests de `AchColombiaManagedMftConnectionTester` suministran el nuevo `IAchColombiaSftpSessionFactory` requerido también al construir escenarios `ManagedFolder`.
- `CS8198`: se eliminó una declaración `out var` dentro de una expresión usada por FluentAssertions. La coincidencia se calcula primero con LINQ `Any` y luego se aserta el booleano.
- `CS0136`: en `AchBulkTransactionServiceTests` el receptor sembrado dentro del scope interno se renombró a `seededRecipient`, evitando colisión con el receptor leído posteriormente.

Validación estática posterior:

- `python scripts/validate-solution-projects.py`: `SOLUTION GRAPH: OK` (7 proyectos / 7 proyectos, todos los `ProjectReference` resueltos).
- 7 archivos `.csproj` parsean como XML válido.
- No quedan referencias a `ParametersRootRoute`, al constructor antiguo `IncomingNachaCommandCenterService(context, ...)`, a `IReadOnlyDictionary<int, int[]>` en el helper NAV2.2, ni `typeof(JwksController|ServersController|TestsController)` en el test OpenAPI.
- `scripts/validate-r6c-incoming-command-center.py`: `R6C INCOMING COMMAND CENTER STATIC RATCHET: OK`.

El entorno de revisión sigue sin `dotnet`; por ello el `restore/build/test` real debe confirmarse en Visual Studio 2026 / SDK 10.0.402.

## BUILD-FIXED-9 — composición DI y clave de cifrado

Se corrigió el fallo de ejecución `appSettings:key es obligatorio para el servicio de cifrado` que aparecía al materializar jobs de Quartz. La causa era que `EncryptionService` validaba la llave AES en el constructor, por lo que una simple resolución del grafo de dependencias podía fallar aunque la operación de cifrado no se estuviera usando.

La validación no fue eliminada ni se agregó una llave por defecto. La llave se valida de forma diferida en el primer `Encrypt`/`Decrypt`; si falta o no mide 16, 24 o 32 bytes UTF-8, la operación sigue fallando cerrada con el mismo error. Esto permite que perfiles MFT deshabilitados o flujos que no usan cifrado puedan componerse sin exigir un secreto innecesario. Cuando una operación realmente usa cifrado, `appSettings:key` sigue siendo obligatorio y debe venir de configuración segura, por ejemplo `appSettings__key` en variables de entorno o secretos del entorno.

## BUILD-FIXED-10 — Scalar/OpenAPI runtime hardening

- Base: `ACHInterbank-NAV6.zip` from project sources.
- Scalar is now explicitly bound to `/openapi/{documentName}.json` via `WithOpenApiRoutePattern`.
- `/openapi` and `/scalar` are excluded from the global fixed-window rate limiter so Scalar cannot lose its document to an HTTP 429 during bootstrap/reloads.
- The OpenAPI endpoint is served directly; the OpenAPI-only output-cache and response-compression middleware were removed to eliminate avoidable failure/corruption layers between `MapOpenApi` and Scalar.
- `GlobalExceptionMiddleware` now records exception message and first inner-exception message in local `crash.log` while keeping the HTTP response sanitized.
- Added `scripts/check-scalar-openapi.ps1` to verify `/openapi/v1.json` and `/scalar/` independently.
- Replaced the cache/compression-specific OpenAPI test with a direct endpoint smoke test that asserts HTTP success and valid OpenAPI JSON.

## BUILD-FIXED-11 — Scalar/OpenAPI runtime hardening

- Scalar UI was reachable, but `/openapi/v1.json` could be invalid/unavailable when documentation-only transformers threw at runtime.
- `OpenApiExamplesTransformer` and `OpenApiResponseDescriptionsTransformer` now log and skip localized metadata failures instead of taking down the entire document.
- Added Development-only `/openapi/_diagnostics` to force OpenAPI generation/serialization and expose the exact exception chain.
- `GlobalExceptionMiddleware` now records `Exception.ToString()` for complete nested exception diagnostics.
- NuGet audit/security settings were not relaxed.
