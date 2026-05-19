# Docker Runtime Readiness - ACH Interbank

Fecha de generacion: 2026-05-18  
Version: 0.1 preliminar  
Rama ejecutada: `fix/spa-docker-runtime-proxy-and-images`  
Commit: `db3bdb27`  
Ambiente: Docker Compose local  
Validacion humana requerida: si  
Clasificacion: evidencia tecnica sin secretos ni datos reales.

## Veredicto

| Area | Estado | Observacion |
|---|---|---|
| CI backend remoto | OK | `dotnet-ci` reportado OK. |
| CI frontend remoto | OK | `angular-ci` reportado OK. |
| Compose config | OK | `docker compose config --quiet` paso. |
| Compose build | OK | API y SPA construyen imagenes locales. |
| Compose runtime | OK tecnico | Servicios levantan y SPA proxya a API. |
| PostgreSQL | OK | `healthy`, base creada, esquema presente. |
| API health | OK | `/health/live` y `/health/ready` HTTP 200. |
| SPA static runtime | OK | `http://localhost:743` HTTP 200. |
| SPA/API integration via same-origin | OK tecnico | `/api/ach/responses` en puerto 743 devuelve 401 desde API, no `index.html`. |
| OpenAPI/Scalar | OK con observacion | Scalar OK; OpenAPI OK pero lento. |
| OpenBao | PENDIENTE / NO APLICA compose actual | No esta en compose principal. |
| Go productivo | NO-GO | Persisten brechas UAT real/anonimizado, seguridad y operacion. |

## Estrategia elegida

Se adopta **Opcion B - proxy Nginx + rutas relativas**:

- API host: `http://localhost:843`.
- API interna Docker: `http://achinterbank-api:8080`.
- SPA host: `http://localhost:743`.
- `environment.prod.ts`: `apiBaseUrl: ''`.
- Nginx proxya `/api/`, `/health/`, `/openapi/` y `/scalar` hacia `achinterbank-api:8080`.

No se adopta Node 26 porque Angular 21 soporta oficialmente Node `^20.19.0`, `^22.12.0` y `^24.0.0`; para UAT/readiness se usa Node 24.

## Inventario Docker

| Elemento | Ruta/Evidencia | Estado |
|---|---|---|
| Compose principal | `docker-compose.yml` | Existe. |
| API Dockerfile | `src/Cfa.ACHInterbank.Api/Dockerfile` | Existe; .NET 10 SDK/runtime. |
| SPA Dockerfile | `web/ach-interbank-ui/Dockerfile` | Existe; actualizado a `node:24-alpine` y `nginx:1.30.1-alpine`. |
| SPA Nginx | `web/ach-interbank-ui/nginx.conf` | Sirve SPA y contiene proxy API/health/OpenAPI/Scalar. |
| `.env` | `.env` | Existe y esta trackeado; revisar sin exponer valores. |
| `.env.example` | `.env.example` | Existe con placeholders. |
| Volumen DB | `ach_postgres_data` | Creado por compose. No se borro ni se removio. |

## Servicios

| Servicio | Imagen | Build | Runtime | Puerto | Health |
|---|---|---|---|---|---|
| `postgres` | `postgres:16` | Imagen base | Up / healthy | interno 5432 | Compose healthcheck OK. |
| `achinterbank-api` | `achinterbank-api:local` | OK | Up | `843:8080` | `/health/live` OK, `/health/ready` OK. |
| `achinterbank-spa` | `achinterbank-spa:local` | OK | Up | `743:80` | HTTP `/` OK; proxy API OK. |

## Resultados de comandos

| Comando | Resultado |
|---|---|
| `docker --version` | Docker `29.4.3`. |
| `docker compose version` | Docker Compose `v5.1.3`. |
| `docker compose config --quiet` | OK. |
| `npm run build` | OK. |
| `npm test -- --watch=false --browsers=ChromeHeadless` | OK, 147 specs. |
| `docker compose build achinterbank-spa` | OK; SPA construida con Node 24 y Nginx 1.30.1. |
| `docker compose up -d` | OK; creo red/volumen y levanto servicios. |
| `docker compose ps` | OK; tres contenedores `Up`, PostgreSQL `healthy`. |
| `docker compose logs --tail=120` | OK con observaciones de migracion y logs EF verbosos. |
| `GET http://localhost:843/health/live` | HTTP 200, `status=Healthy`. |
| `GET http://localhost:843/health/ready` | HTTP 200, `database=Healthy`. |
| `GET http://localhost:743` | HTTP 200, `index.html`. |
| `GET http://localhost:843/scalar` | HTTP 200. |
| `GET http://localhost:843/openapi/v1.json` | HTTP 200 con timeout ampliado; aprox. 79s. |
| `GET http://localhost:743/health/live` | HTTP 200 JSON desde API por proxy. |
| `GET http://localhost:743/health/ready` | HTTP 200 JSON desde API por proxy. |
| `GET http://localhost:743/openapi/v1.json` | HTTP 200 JSON desde API por proxy; aprox. 96s. |
| `GET http://localhost:743/scalar` | HTTP 200 Scalar por proxy. |
| `GET http://localhost:743/api/ach/responses` | HTTP 401 desde API; confirma proxy y auth intacta. |
| `GET http://achinterbank-api:8080/health/*` desde contenedor SPA | OK live/ready. |

## Riesgos observados

| Riesgo | Severidad | Evidencia | Recomendacion |
|---|---|---|---|
| SPA no enrutaba API en compose actual | CERRADO TECNICAMENTE para UAT basico | `nginx.conf` ahora proxya rutas API; `/api/ach/responses` devuelve 401 desde API. | Ejecutar UAT tecnico con datos anonimizados y usuarios/roles. |
| Vulnerabilidad NU1903 en `System.Security.Cryptography.Xml` 10.0.0 | ALTA seguridad | Warning durante `docker compose build`. | Actualizar dependencia de forma controlada y ejecutar CI completo. |
| OpenAPI lento | MEDIA | 79s directo y 96s via proxy para `/openapi/v1.json`. | Documentar timeout o optimizar generacion. |
| `.env` versionado | ALTA seguridad | `.env` existe y esta trackeado. | Revisar contenido, rotar si aplica, destrackear con procedimiento aprobado. |
| Migraciones automaticas en startup | MEDIA operacion | `Database__ApplyMigrations=true`. | Decidir si UAT/preproductivo permite auto-migrate o requiere DBA. |
| OpenBao fuera de compose principal | MEDIA/ALTA si aplica a certificados | Scripts/docs existen, servicio no. | Definir alcance UAT: OpenBao, secret manager externo o waiver. |

## Decision de readiness

El compose actual es **apto para validacion tecnica directa de API, PostgreSQL, OpenAPI/Scalar, SPA estatica y proxy SPA->API**.

El compose actual queda **apto para UAT tecnico E2E basico desde SPA**, sujeto a validacion funcional con datos anonimizados, usuarios/roles y acta/evidencias.

Estado productivo: **NO-GO**.
