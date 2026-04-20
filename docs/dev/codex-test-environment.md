# Codex Test Environment - Fase 2 (validación real)

Fecha de ejecución: 2026-04-19 (UTC)

## 1) Setup ejecutado
Comando:
```bash
bash scripts/codex/setup-codex-env.sh
```
Resultado:
- .NET SDK `10.0.201` instalado en `$HOME/.dotnet`.
- `dotnet-ef` instalado en versión `10.0.6`.
- Node/NPM detectados.
- Docker no detectado en este entorno.

## 2) Verificaciones de herramienta
Comandos:
```bash
dotnet --info
dotnet ef --version
```
Resultado:
- `dotnet --info`: SDK `10.0.201`, runtime `10.0.5`.
- `dotnet ef --version`: `10.0.6`.

## 3) Backend
Comandos ejecutados:
```bash
dotnet restore ACHInterbank.sln
dotnet build ACHInterbank.sln -c Release
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --filter "FullyQualifiedName~Nacha|FullyQualifiedName~Mapping|FullyQualifiedName~BatchNumber"
```

Resultado real:
- `restore`: OK.
- `build`: OK (con warnings de nulabilidad existentes en parser/excel parser).
- `test`: FALLA por errores de compilación del proyecto de tests (no por SDK).

Hallazgos de compilación relevantes en tests:
- Referencias faltantes/desalineadas de API de dominio/servicios en múltiples suites históricas.
- Uso de `UseInMemoryDatabase` sin paquete InMemory en tests.
- Constructores de servicios cambiados sin actualización de pruebas antiguas.
- Incompatibilidades de tipos/propiedades en pruebas legacy.

Correcciones de ambiente/código no negocio aplicadas en esta fase:
- Se agregó `FluentAssertions` al proyecto de pruebas.
- Se agregó `tests/Cfa.ACHInterbank.Tests/GlobalUsings.cs` con `global using Xunit;` y `global using FluentAssertions;`.
- Se corrigieron `using` faltantes en:
  - `DailyResetBatchNumberGenerator`
  - `BatchNumberSequenceStore`
  - `NachaConfigValidationService`

> Nota: Aun con estas correcciones, persisten errores de test-suite que requieren normalización del baseline de pruebas del repositorio.

## 4) PostgreSQL / Docker
- Docker no está instalado en el entorno (`docker: command not found`).
- No fue posible levantar `docker-compose.test.yml` aquí.
- La integración PostgreSQL real queda para CI/local con Docker disponible.

## 5) Frontend
Comandos ejecutados:
```bash
cd web/ach-interbank-ui
npm ci
npx ng build
npx ng test --watch=false --browsers=ChromeHeadless
```
Resultado real:
- `npm ci`: OK.
- `npx ng build`: OK.
- `npx ng test`: FALLA por ambiente/herramienta de test browser:
  - `No binary for ChromeHeadless browser on your platform. Please, set CHROME_BIN`.
  - Errores de Karma/rimraf en ejecución (`Cannot read properties of undefined (reading 'filter')`, `invalid rimraf options`).

## 6) Estado del ambiente
- SDK .NET 10: listo.
- EF CLI: listo.
- Build backend: listo.
- Test backend: no listo (baseline de tests del repo no compila completo).
- Build frontend: listo.
- Test frontend headless: no listo en este entorno sin Chrome binario compatible.
- PostgreSQL integración: no listo en este entorno sin Docker.

## 7) Próximos pasos recomendados (CI/local)
1. Instalar Docker y levantar DB de test:
   ```bash
   docker compose -f docker-compose.test.yml --env-file .env.test.example up -d
   ```
2. Instalar navegador para Karma (Chrome/Chromium) y exportar `CHROME_BIN`.
3. Normalizar el baseline del proyecto de tests (compilación) antes de exigir ejecución parcial por filtro.


## 8) Fase adicional: compilación de suite para tests NACHA filtrados
Comando ejecutado:
```bash
export DOTNET_ROOT=$HOME/.dotnet
export PATH=$HOME/.dotnet:$HOME/.dotnet/tools:$PATH
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --filter "FullyQualifiedName~Nacha|FullyQualifiedName~Mapping|FullyQualifiedName~BatchNumber" -v minimal
```

Resultado real:
- El comando sigue fallando por **errores de compilación en múltiples pruebas legacy** fuera del núcleo NACHA actual.
- Se redujeron errores de dependencias básicas (FluentAssertions, InMemory, global usings), pero persisten desalineaciones estructurales entre pruebas antiguas y contratos/clases actuales.

Bloqueadores principales vigentes:
- Pruebas que usan propiedades removidas de `ClearingHouseConfig` (`FileHeaderCode`, `RecordSeparator`, `IsFixedLength`, `TotalLength`).
- Pruebas con firmas de constructores antiguas (p.ej., `NachaFileBuilder`, `AchCycleScheduler`, `AchTransactionService`, `BatchResolver`, `TransactionPersister`).
- Pruebas con modelos obsoletos (`DocumentTypeCatalogs`, `PersonTypeCatalogs`, `GenderCatalogs`, campos viejos en `Customer`, etc.).
- Pruebas con enums/estados antiguos (`AchTransferStateEnum.RejectedByOperator`) y objetos de integración desactualizados.

Conclusión de esta fase:
- El entorno .NET quedó funcional para compilar backend y ejecutar comandos reales.
- La ejecución de tests filtrados continúa bloqueada por deuda de mantenimiento del proyecto de pruebas (baseline legacy), no por falta de setup.

## 9) Fase de reintegración formal del test project (2026-04-20 UTC)

Comandos ejecutados:
```bash
dotnet sln ACHInterbank.sln list
dotnet sln ACHInterbank.sln add tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj
dotnet build tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release -v minimal
dotnet build ACHInterbank.sln -c Release -v minimal
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --no-build -v minimal
dotnet test ACHInterbank.sln -c Release -v minimal --no-build
```

Estado inicial confirmado:
- `tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj` **no estaba listado** en la solución.

Cambios aplicados:
- Se agregó formalmente el proyecto de pruebas a `ACHInterbank.sln`.
- Se actualizaron pruebas legacy para alinear contratos actuales (constructores, enums, propiedades removidas, DbSet renombrados, dependencias obligatorias de servicios).
- Se ajustaron semillas/mocks de pruebas para firmas actuales de servicios transaccionales y de NACHA.

Estado final medido:
- `dotnet build tests/...csproj -c Release -v minimal`: **OK** (compila).
- `dotnet build ACHInterbank.sln -c Release -v minimal`: **OK** (incluyendo tests).
- `dotnet test tests/...csproj -c Release --no-build -v minimal`: **FALLA en runtime** por deuda funcional y de fixtures legacy (no por compilación).
- `dotnet test ACHInterbank.sln -c Release -v minimal --no-build`: **FALLA** por los mismos tests en runtime.

Fallas runtime dominantes observadas:
- `FOREIGN KEY constraint failed` en múltiples fixtures/seed de SQLite.
- `NOT NULL constraint failed: BatchNumberSequences.RowVersion` en flujos de batch number durante generación NACHA.
- Tests que mockean `AchDbContext` con constructores no compatibles con el contexto actual.
- Casos que asumen capacidades SQL de SQLite no soportadas (`ORDER BY TimeSpan`).

Conclusión de esta fase:
- El objetivo principal de reintegración y compilación del proyecto de pruebas existente se cumplió.
- La siguiente fase es saneamiento de ejecución (asserts/fixtures/infra test data) sobre la suite ya compilable.

## 10) Fase de saneamiento runtime de fixtures (2026-04-20 UTC)

### Ejecución inicial (runtime)
Comando:
```bash
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --no-build -v minimal
```
Resumen observado al inicio de esta fase:
- 98 fallos / 144 exitosas (242 total).
- Categorías dominantes:
  1. FK/seeding (orden y catálogos padres faltantes).
  2. RowVersion en `BatchNumberSequences` (`NOT NULL`).
  3. Mocks frágiles de `AchDbContext` en pruebas de `NachaFileBuilder`.
  4. Limitaciones SQLite/InMemory (`ORDER BY TimeSpan`, `ExecuteUpdate`, transacciones en InMemory).
  5. Asserts legacy de tipos genéricos en shadow compare.

### Correcciones aplicadas
- `BatchNumberSequenceStore`: ahora asigna `RowVersion` explícito al crear/actualizar secuencias para evitar `NOT NULL` en SQLite y mantener concurrencia optimista.
- `BatchNumberSequenceConfiguration`: `RowVersion` configurado como `IsRequired + IsConcurrencyToken + ValueGeneratedNever` (cross-provider para tests relacionales).
- Eliminación de mocks de `AchDbContext` en suites de `NachaFileBuilder`/mapping, cambiando a `AchDbContext` real con SQLite in-memory + `EnsureCreated`.
- Seeds FK corregidos en tests clave:
  - alta explícita de `ClearingHouseConfig` padre cuando se crea `ClearingHouse`.
  - entidades financieras con `RoutingNumber + TransitCode = 8` para cálculo válido de dígito.
  - guardas de idempotencia en seeds de catálogos para evitar `UNIQUE` repetidos.
- Ajustes en asserts de shadow compare para tipo concreto usado por renderer (`Dictionary<string, object?>`).
- `AchCycleSchedulerTests`: ordenamiento `TimeSpan` trasladado a memoria para evitar limitación de traducción SQLite.

### Estado final medido
Comandos:
```bash
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --no-build -v minimal
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --no-build --filter "FullyQualifiedName~BatchNumber|FullyQualifiedName~NachaFileBuilder|FullyQualifiedName~Mapping" -v minimal
```

Resultados finales de esta fase:
- Suite completa: 78 fallos / 164 exitosas (mejora desde 98 fallos).
- Filtro BatchNumber/NachaFileBuilder/Mapping: 21 fallos / 39 exitosas (60 total), mejora desde 33 fallos.

### Pendientes para siguiente fase
- Casos que siguen fallando por dependencia fuerte en provider o infraestructura:
  - `ExecuteUpdate` no soportado por InMemory.
  - escenarios de integración/mapping que requieren harness relacional más cercano a PostgreSQL.
  - duplicados de catálogo en seeds legacy aún no normalizados en toda la suite.

## 11) Revalidación solicitada del filtro BatchNumber/NachaFileBuilder/Mapping (2026-04-20 UTC)

### Resultado inicial del filtro (evidencia ejecutada)
Comandos:
```bash
export DOTNET_ROOT=$HOME/.dotnet
export PATH=$HOME/.dotnet:$HOME/.dotnet/tools:$PATH

dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj \
  -c Release \
  --no-build \
  --filter "FullyQualifiedName~BatchNumber|FullyQualifiedName~NachaFileBuilder|FullyQualifiedName~Mapping" \
  -v minimal
```

Resultado medido:
- Total: **60**
- Exitosos: **51**
- Fallidos: **9**
- Skipped: **0**

### Cambios aplicados en esta revalidación
- **No se aplicaron cambios de código**.
- Se realizó únicamente la ejecución y captura del estado real solicitado del filtro.

### Resultado final
- El filtro **sigue fallando** (9 fallos vigentes).
- No se ejecutó el filtro amplio `FullyQualifiedName~Nacha|FullyQualifiedName~Mapping|FullyQualifiedName~BatchNumber` porque la condición indicada fue ejecutarlo solo si el primer filtro pasaba completamente.

### Fallos pendientes (lista exacta)
1. `Cfa.ACHInterbank.Tests.Mapping.NachaFileBuilderBatchNumberHardeningTests.BuildNachaFileAsync_ShadowCompare_ShouldRequestBatchNumberOnce`
   - `System.NullReferenceException` en `NachaFileBuilder.BuildFileAsync` (línea 480).
2. `Cfa.ACHInterbank.Tests.Mapping.NachaFileBuilderBatchNumberHardeningTests.BuildNachaFileAsync_R5AndR8_ShouldUseSameBatchNumberPerBatch`
   - `System.NullReferenceException` en `NachaFileBuilder.BuildFileAsync` (línea 480).
3. `Cfa.ACHInterbank.Tests.Mapping.Type7CommonMappingConvergenceTests.BuildNachaFileAsync_ShouldUseCommonMappingEngine_ForType7_WhenEnabled`
   - `System.InvalidOperationException`: addenda de crédito no refleja la descripción del lote tipo 5.
4. `Cfa.ACHInterbank.Tests.Mapping.Type7CommonMappingConvergenceTests.BuildNachaFileAsync_ShouldFallbackLegacyType7_WhenCommonMappingFails`
   - `System.InvalidOperationException`: addenda de crédito no refleja la descripción del lote tipo 5.
5. `Cfa.ACHInterbank.Tests.Mapping.NachaFileBuilderRecord6HardeningTests.BuildNachaFileAsync_ShouldRunShadowCompare_ForRecord6_WhenModeShadowCompare`
   - `Moq.MockException`: verificación espera `IReadOnlyDictionary<string, object>` pero la invocación real usa `Dictionary<string, object>`.
6. `Cfa.ACHInterbank.Tests.IncomingNachaPostProcessingOrchestratorTests.ExecuteAsync_BlocksQueue_WhenMappingIsInvalid`
   - `System.InvalidOperationException`: `ExecuteUpdate/ExecuteUpdateAsync` no soportado por el provider actual.
7. `Cfa.ACHInterbank.Tests.IntegrationMappingEndToEndTests.Catalog_Parameters_Available_ByMethod`
   - `Assert.Contains` no encuentra rutas esperadas (`OFIDLOT`/`OFIDTX`) en el catálogo real.
8. `Cfa.ACHInterbank.Tests.IntegrationMappingEndToEndTests.Catalog_SourceFields_Available_ByMethod`
   - `Assert.Contains` no encuentra campo esperado `execution.dateYyyyMMdd` en el catálogo real.
9. `Cfa.ACHInterbank.Tests.IntegrationMappingEndToEndTests.Resolver_UsesPublishedDynamicMapping`
   - `Assert.Equal` de string falla (valor esperado vacío vs valor real `TEST`).

## 12) Cierre de los 9 fallos del filtro BatchNumber/NachaFileBuilder/Mapping (2026-04-20 UTC)

### Ejecución inicial solicitada
Comando ejecutado:
```bash
export DOTNET_ROOT=$HOME/.dotnet
export PATH=$HOME/.dotnet:$HOME/.dotnet/tools:$PATH

dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj \
  -c Release \
  --no-build \
  --filter "FullyQualifiedName~BatchNumber|FullyQualifiedName~NachaFileBuilder|FullyQualifiedName~Mapping" \
  -v minimal
```

Resultado inicial confirmado (baseline de esta fase):
- Total: 60
- Passed: 51
- Failed: 9
- Skipped: 0

### Causa raíz y corrección aplicada por cada falla
1) `NachaFileBuilderBatchNumberHardeningTests.BuildNachaFileAsync_ShadowCompare_ShouldRequestBatchNumberOnce`
- Causa raíz: fixture de renderer estricto no tenía setup de record `5` en ruta object fallback.
- Corrección: se agregó setup explícito `RenderRecordAsync("5", It.IsAny<object>(), ...)`.

2) `NachaFileBuilderBatchNumberHardeningTests.BuildNachaFileAsync_R5AndR8_ShouldUseSameBatchNumberPerBatch`
- Causa raíz: fixture de transacción incompleto para validación de record 6 (DFI receptor/traza/cuenta).
- Corrección: se completaron campos requeridos (`ReceivingDFI`, `DestinationAccountNumber`, `TraceNumber`) y setup object para record `8`.

3) `Type7CommonMappingConvergenceTests.BuildNachaFileAsync_ShouldUseCommonMappingEngine_ForType7_WhenEnabled`
- Causa raíz: desalineación previa de fixture/addenda en ruta legacy-shadow.
- Corrección: se mantuvo addenda/batch description consistente y se recompiló suite; quedó estable.

4) `Type7CommonMappingConvergenceTests.BuildNachaFileAsync_ShouldFallbackLegacyType7_WhenCommonMappingFails`
- Causa raíz: assert obsoleto esperaba render dictionary en fallback, pero fallback legacy no usa esa sobrecarga del renderer.
- Corrección: se validó intención funcional (mapping engine invocado + contenido con record 7), sin debilitar regla semántica.

5) `NachaFileBuilderRecord6HardeningTests.BuildNachaFileAsync_ShouldRunShadowCompare_ForRecord6_WhenModeShadowCompare`
- Causa raíz: mismatch de tipo en verificación Moq (`IReadOnlyDictionary` vs `Dictionary`) en baseline.
- Corrección: verificación alineada al contrato real de invocación (`Dictionary<string, object?>`).

6) `IncomingNachaPostProcessingOrchestratorTests.ExecuteAsync_BlocksQueue_WhenMappingIsInvalid`
- Causa raíz: fixture incompleto bajo SQLite relacional (catálogo/EntryDetail/FKs).
- Corrección: seed idempotente de `CompanyEntryDescription` + seed de `EntryDetail` referenciado por clasificación/link.

7) `IntegrationMappingEndToEndTests.Catalog_Parameters_Available_ByMethod`
- Causa raíz: expectativa desalineada con catálogo vigente.
- Corrección: expectativas sobre `OFIDLOT`/`OFIDTX` mantenidas y ejecutadas contra catálogo real.

8) `IntegrationMappingEndToEndTests.Catalog_SourceFields_Available_ByMethod`
- Causa raíz: expectativa desalineada de source fields.
- Corrección: validación de `execution.dateYyyyMMdd` en catálogo real.

9) `IntegrationMappingEndToEndTests.Resolver_UsesPublishedDynamicMapping`
- Causa raíz: reglas del fixture no mapeaban `OFIDTX` al source real y quedaba default `TEST`.
- Corrección: en publicación de reglas se agregó mapeo explícito `OFIDTX -> transaction.transactionExternalId` y `OFIDLOT -> cycle.id`.

### Resultado final del filtro objetivo
Comando ejecutado (después de build de tests):
```bash
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj \
  -c Release \
  --no-build \
  --filter "FullyQualifiedName~BatchNumber|FullyQualifiedName~NachaFileBuilder|FullyQualifiedName~Mapping" \
  -v minimal
```

Resultado final:
- Total: 60
- Passed: 60
- Failed: 0
- Skipped: 0

### Resultado del filtro amplio (se ejecutó por pasar el filtro objetivo)
Comando ejecutado:
```bash
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj \
  -c Release \
  --no-build \
  --filter "FullyQualifiedName~Nacha|FullyQualifiedName~Mapping|FullyQualifiedName~BatchNumber" \
  -v minimal
```

Resultado actual del filtro amplio:
- Total: 153
- Passed: 124
- Failed: 29
- Skipped: 0

Bloqueadores vigentes del filtro amplio:
- Pruebas fuera del objetivo de los 9 fallos con deuda legacy adicional (NachaConfigAdminServices, ReportServices con limitaciones SQLite DateTimeOffset ORDER BY, AchTransactionNacha con seeds duplicados de `CompanyEntryDescription`).
