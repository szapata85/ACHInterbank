# BUILD-FIXED-12 — OpenAPI schema Guid serialization exception

## Evidence

The Development endpoint `/openapi/_diagnostics` returned HTTP 500 inside
`System.Text.Json.Schema.JsonSchemaExporter.MapJsonSchemaCore`, reporting:

`System.Text.Json.JsonException: The JSON value could not be converted to System.Guid.`

The failure happens while `Microsoft.AspNetCore.OpenApi.OpenApiSchemaService.CreateSchema`
is producing a **response** schema. It precedes the custom OpenAPI operation and document
transformers and is not caused by Scalar, rate limiting, or an invalid URL.

## Identified candidates and narrow correction

Reviewing `src/Cfa.ACHInterbank.Api/Contracts/AchResponses/AchResponseApiContracts.cs`
revealed that the public response positional records `AchResponseDetailResponse` and
`AchResponseStatusMappingResponse` expose an **optional** `Guid Version = default`
constructor parameter. `JsonSchemaExporter` serializes constructor parameter default
values into property schemas; value-type defaults in reflection metadata can be problematic.

Make these public response constructor parameters explicit/required (`ClearingHouseId`,
`AppliedMappingId` or `Priority`, and `Version`). The sole production call sites in
`AchResponseQueryApiMapper` already pass every argument, so output JSON property names,
response fields, mapper behavior, and persisted data are unchanged. No change is made to
the application-layer DTOs or database.

This is a **targeted source-level hypothesis**, not a runtime-proven correction. It must
be validated by a .NET 10 build plus a GET to `/openapi/v1.json`.

## Additional diagnostics and regression

- Adds `OpenApiGuidSchemaRegressionTests` to individually export both response contracts'
  JSON schemas with System.Text.Json's production schema engine.
- Adds Development-only `/openapi/_schema-diagnostics` to export each application response
  schema independently and return the failing .NET types and stack traces.
- Updates `scripts/check-scalar-openapi.ps1` to collect schema diagnostics automatically
  if OpenAPI returns non-2xx.
- Preserves NuGet audit settings, OpenAPI transformers, authentication policies, and the
  `/openapi/v1.json` route. The diagnostics endpoints are Development-only and excluded
  from the OpenAPI discovery surface.

## Validate on Visual Studio 2026 / .NET 10 SDK

From the repository root:

```powershell
dotnet restore ACHInterbank.sln --force-evaluate
dotnet test .\tests\Cfa.ACHInterbank.Tests\Cfa.ACHInterbank.Tests.csproj --filter FullyQualifiedName~OpenApiGuidSchemaRegressionTests
dotnet build ACHInterbank.sln -c Debug --no-restore
```

Start the API using the Visual Studio Development launch profile, then check:

1. `https://localhost:7269/openapi/v1.json` (should return JSON and HTTP 200)
2. `https://localhost:7269/scalar/` (should display the `v1` API document)
3. If still HTTP 500: `https://localhost:7269/openapi/_schema-diagnostics` to obtain
   the specific response DTO type that fails.
4. `https://localhost:7269/openapi/_diagnostics` retains the full generation exception.

No .NET SDK is available in this analysis environment, so build/test/runtime success
cannot be claimed yet.
