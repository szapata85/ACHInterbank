# ACH Interbank

ACH Interbank es una solucion para procesamiento operativo de transferencias interbancarias ACH, con backend .NET, persistencia EF Core, PostgreSQL, SPA Angular y soporte documental para UAT / go-live readiness.

Estado actual documentado: candidato a UAT controlado. No declarar GO productivo sin acta UAT firmada, evidencias, aprobaciones de negocio/operaciones/seguridad y cierre o aceptacion formal de riesgos.

## Estructura del repositorio

| Ruta | Proposito |
|---|---|
| `ACHInterbank.sln` | Solucion principal .NET. |
| `src/Cfa.ACHInterbank.Api` | API principal ASP.NET Core. |
| `src/Cfa.ACHInterbank.Application` | Casos de uso, DTOs, contratos y reglas de aplicacion. |
| `src/Cfa.ACHInterbank.Domain` | Entidades, enums y modelos de dominio. |
| `src/Cfa.ACHInterbank.Persistence` | EF Core, DbContext, configuraciones, migraciones y servicios persistentes. |
| `src/Cfa.ACHInterbank.External` | Integraciones externas. |
| `tests/Cfa.ACHInterbank.Tests` | Pruebas automatizadas backend. |
| `web/ach-interbank-ui` | SPA Angular. |
| `docs/uat` | Plan, escenarios, datos, acta, evidencias y defectos UAT. |
| `docs/go-live-readiness` | Checklist, scorecard, brechas, matriz SPA/backend/norma/UAT y paquete comite. |
| `docs/security` | Revision de seguridad pre-go-live. |
| `docs/operations` | Runbooks y evidencias operativas. |

## Backend .NET

Comandos no destructivos sugeridos:

```powershell
dotnet restore ACHInterbank.sln
dotnet build ACHInterbank.sln -c Release
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release
```

Migraciones EF Core: aplicar solo en ambientes autorizados, con backup previo y aprobacion de DBA/operaciones. No ejecutar migraciones como parte de una revision documental o sin ventana aprobada.

## SPA Angular

La SPA esta en `web/ach-interbank-ui`.

```powershell
cd web/ach-interbank-ui
npm ci
npm run build
npm test -- --watch=false --browsers=ChromeHeadless
```

La configuracion productiva no debe apuntar a `localhost`. Si API y SPA se publican tras el mismo reverse proxy, usar ruta relativa; si se requiere dominio dedicado, parametrizarlo en pipeline o configuracion de despliegue aprobada.

## PostgreSQL y Docker Compose

El compose principal es `docker-compose.yml`. Los defaults incluidos son placeholders locales/de demostracion y no son aptos para UAT/preproductivo/productivo.

```powershell
docker compose config
docker compose build
docker compose up -d
docker compose logs --tail=200
```

No usar `docker compose down -v` salvo instruccion operativa explicita. No borrar volumenes como mecanismo normal de rollback.

## Secretos y datos sensibles

- No versionar `.env` reales.
- No versionar contrasenas, tokens, certificados privados, llaves privadas, PFX reales ni datos personales/financieros.
- Usar `.env.example` y `.env.test.example` solo como plantillas sanitizadas.
- Usar vault/secret manager o mecanismo aprobado para UAT/preproductivo/productivo.
- Evidencias con datos sensibles deben almacenarse fuera de Git y referenciarse por ID, hash o ruta segura.

## Documentacion UAT y go-live

Documentos principales:

- `docs/uat/PLAN_UAT_DATOS_REALES.md`
- `docs/uat/ESCENARIOS_UAT_DATOS_REALES.md`
- `docs/uat/ACTA_UAT_DATOS_REALES_TEMPLATE.md`
- `docs/go-live-readiness/README_OPERATIVO_RELEASE_UAT.md`
- `docs/go-live-readiness/CHECKLIST_GO_NO_GO.md`
- `docs/go-live-readiness/SCORECARD_GO_LIVE_READINESS.md`
- `docs/go-live-readiness/BRECHAS_CRITICAS_GO_LIVE.md`
- `docs/security/REVISION_SEGURIDAD_PRE_GO_LIVE.md`
- `docs/operations/RUNBOOK_UAT_Y_PREPRODUCTIVO.md`

## OpenBao

El repositorio contiene scripts y documentacion para OpenBao bajo `scripts/openbao`, `ops/openbao` y documentos en `docs/architecture` / `docs/dev`. El compose principal no debe asumirse como stack UAT completo con OpenBao si no existe decision operativa aprobada.

## Readiness

Nivel actual: candidato a UAT controlado. Productivo permanece NO-GO hasta cerrar o aceptar formalmente brechas de UAT, seguridad, secretos, CENIT, firma/sobre digital, rollback, health checks y evidencias.
