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
