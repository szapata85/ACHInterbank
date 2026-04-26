# Revalidación estricta — Prompt 8 API read-only Capability Registry (2026-04-26)

## Objetivo

Cerrar formalmente Prompt 8 con evidencia ejecutada de:

1. API read-only compila y expone solo GET.
2. Autorización/política fina está aplicada.
3. No existen endpoints de escritura en el controller Prompt 8.
4. DTO de salida no expone campos sensibles obvios.
5. No hay regresión en suites PaymentRail/Routing, IncomingNacha, NACHA/Mapping/BatchNumber y DI.
6. EF migrations list se mantiene consistente.
7. Sin cambios criptográficos.
8. Workflow PostgreSQL se mantiene manual-only.

## Verificación técnica Prompt 8 (controller/políticas/read-only)

```bash
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --filter "FullyQualifiedName~PaymentRailCapabilityRegistryControllerTests|FullyQualifiedName~PaymentRailCapabilityRegistryAuthorizationPolicyTests"
```

- Passed: 10
- Failed: 0

Cobertura agregada:

- controller declara únicamente `HttpGet`;
- acciones exigen `CanViewPaymentRailCapabilityRegistry`;
- fallback de policy validado para `CanManageAch`/`CanReadAch`;
- capability unknown retorna `NotFound`;
- rail inválido retorna `BadRequest`;
- DTO sin nombres sensibles típicos (`password/secret/privateKey/token/iv`).

## Revalidación build + regresión funcional

```bash
dotnet build ACHInterbank.sln -c Release
```

- Build OK (0 errores, warnings preexistentes de nulabilidad).

```bash
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --filter "FullyQualifiedName~PaymentRail|FullyQualifiedName~RoutingStrategyServiceTests"
```

- Passed: 30
- Failed: 0

```bash
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --filter "FullyQualifiedName~IncomingNacha"
```

- Passed: 63
- Failed: 0

```bash
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --filter "FullyQualifiedName~Nacha|FullyQualifiedName~Mapping|FullyQualifiedName~BatchNumber"
```

- Passed: 193
- Failed: 0

```bash
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --filter "FullyQualifiedName~DependencyInjection"
```

- Passed: 2
- Failed: 0

## EF/migrations consistency

```bash
dotnet ef migrations list \
  --project src/Cfa.ACHInterbank.Persistence/Cfa.ACHInterbank.Persistence.csproj \
  --startup-project src/Cfa.ACHInterbank.Api/Cfa.ACHInterbank.Api.csproj \
  --context AchDbContext
```

- Migration Prompt 7 confirmada:
  - `20260426025056_AddPaymentRailCapabilityRegistryPhase7`.
- Sin PostgreSQL local en `localhost:5432`, no se puede calcular applied/pending, pero el listado de migraciones es consistente.

## Verificación ausencia de escritura


```bash
rg -n "\[Http(Post|Put|Patch|Delete)" src/Cfa.ACHInterbank.Api/Controllers/PaymentRailCapabilityRegistryController.cs
```

- Sin coincidencias: controller Prompt 8 no expone métodos HTTP de escritura.

## Verificación no-crypto / manual-only


```bash
git diff --name-only 60308c2..HEAD | rg "CryptoServiceScoped|RsaKeyProvider|OpenEnvelopeAsync|ACHSobreDigital|Encryption"
```

```bash
git diff --name-only 60308c2..HEAD | rg "Quartz|Workflow|Job|Scheduler"
```

- No hay archivos de criptografía tocados (`CryptoServiceScoped`, `RsaKeyProvider`, `OpenEnvelopeAsync`, XML/AES/RSA).
- Cambios concentrados en controller Prompt 8, tests y documentación de revalidación.

## Conclusión formal

Prompt 8 queda revalidado en modo gobernanza read-only:

- API administrativa de consulta activa;
- sin POST/PUT/PATCH/DELETE en Prompt 8;
- legacy continúa como source of truth;
- PaymentRail continúa en paralelo/shadow;
- sin cutover;
- sin cambios funcionales ACH/CENIT;
- sin cambios criptográficos;
- workflow PostgreSQL manual-only (sin automatismos nuevos).
