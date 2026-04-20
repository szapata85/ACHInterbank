# AGENTS.md — ACHInterbank (raíz)

Este archivo aplica a todo el repositorio. Si existe otro `AGENTS.md` en subdirectorios, el más profundo tiene prioridad en su scope.

## Stack técnico
- Backend: .NET 10 (`net10.0`), EF Core Code First, PostgreSQL/SQL Server.
- API principal: `src/Cfa.ACHInterbank.Api`.
- Persistencia: `src/Cfa.ACHInterbank.Persistence`.
- Tests backend: `tests/Cfa.ACHInterbank.Tests`.
- Frontend SPA: Angular (`web/ach-interbank-ui`) con Karma + `ChromeHeadless`.

## Reglas de arquitectura y dominio (obligatorias)
1. EF Code First es la fuente de verdad para esquema (no scripts SQL manuales como primarios).
2. Mantener Clean Architecture (Application/Domain/Persistence/Api).
3. No mover cálculos críticos NACHA (hash/totales/block count/controles) a configuración editable.
4. No romper fallback legacy ni shadow compare en cambios del generador.
5. Toda parametrización de negocio debe ser auditable.

## Setup reproducible para Codex
1. Ejecutar setup de entorno:
   ```bash
   bash scripts/codex/setup-codex-env.sh
   ```
2. Levantar PostgreSQL de test:
   ```bash
   docker compose -f docker-compose.test.yml --env-file .env.test.example up -d
   ```
3. Restaurar/build backend:
   ```bash
   dotnet restore ACHInterbank.sln
   dotnet build ACHInterbank.sln -c Release
   ```
4. Aplicar migraciones EF (Postgres):
   ```bash
   dotnet ef database update \
     --project src/Cfa.ACHInterbank.Persistence/Cfa.ACHInterbank.Persistence.csproj \
     --startup-project src/Cfa.ACHInterbank.Api/Cfa.ACHInterbank.Api.csproj \
     --context AchDbContext
   ```
5. Ejecutar tests backend:
   ```bash
   dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release
   ```
6. Frontend (Angular):
   ```bash
   cd web/ach-interbank-ui
   npm ci
   npm run build
   npm test -- --watch=false --browsers=ChromeHeadless
   ```

## Si algo falla
- Reportar el error exacto (comando + salida + causa probable).
- No afirmar pruebas ejecutadas si no se ejecutaron realmente.
- Si falta SDK/servicio, indicar bloqueo con acción concreta para destrabar.

## Convenciones frontend
- UI visible en español.
- Listados en AG-GRID/ui-grilla-empresarial.
- Formularios reactivos.
- Acciones críticas: loading + disabled + anti doble click.
