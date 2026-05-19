# Docker Runtime Readiness - ACH Interbank

Fecha de generacion: 2026-05-18  
Version: 0.1 preliminar  
Rama ejecutada: `fix/angular-transaction-create-specs`  
Commit: `3f167663`  
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
| Compose runtime | PARCIAL | Servicios levantan, pero SPA no proxya a API. |
| PostgreSQL | OK | `healthy`, base creada, esquema presente. |
| API health | OK | `/health/live` y `/health/ready` HTTP 200. |
| SPA static runtime | OK | `http://localhost:743` HTTP 200. |
| SPA/API integration via same-origin | CRITICO UAT TECNICO | `/api` en puerto 743 devuelve `index.html`. |
| OpenAPI/Scalar | OK con observacion | Scalar OK; OpenAPI OK pero lento. |
| OpenBao | PENDIENTE / NO APLICA compose actual | No esta en compose principal. |
| Go productivo | NO-GO | Persisten brechas UAT, seguridad y operacion. |

## Inventario Docker

| Elemento | Ruta/Evidencia | Estado |
|---|---|---|
| Compose principal | `docker-compose.yml` | Existe. |
| API Dockerfile | `src/Cfa.ACHInterbank.Api/Dockerfile` | Existe; .NET 10 SDK/runtime. |
| SPA Dockerfile | `web/ach-interbank-ui/Dockerfile` | Existe; Node 22 + Nginx. |
| SPA Nginx | `web/ach-interbank-ui/nginx.conf` | Sirve SPA; no contiene proxy API. |
| `.env` | `.env` | Existe y esta trackeado; revisar sin exponer valores. |
| `.env.example` | `.env.example` | Existe con placeholders. |
| Volumen DB | `ach_postgres_data` | Creado por compose. No se borro ni se removio. |

## Servicios

| Servicio | Imagen | Build | Runtime | Puerto | Health |
|---|---|---|---|---|---|
| `postgres` | `postgres:16` | Imagen base | Up / healthy | interno 5432 | Compose healthcheck OK. |
| `achinterbank-api` | `achinterbank-api:local` | OK | Up | `843:8080` | `/health/live` OK, `/health/ready` OK. |
| `achinterbank-spa` | `achinterbank-spa:local` | OK | Up | `743:80` | Sin healthcheck compose; HTTP `/` OK. |

## Resultados de comandos

| Comando | Resultado |
|---|---|
| `docker --version` | Docker `29.4.3`. |
| `docker compose version` | Docker Compose `v5.1.3`. |
| `docker compose config --quiet` | OK. |
| `docker compose build` | OK; API y SPA construidas. |
| `docker compose up -d` | OK; creo red/volumen y levanto servicios. |
| `docker compose ps` | OK; tres contenedores `Up`, PostgreSQL `healthy`. |
| `docker compose logs --tail=120` | OK con observaciones de migracion y logs EF verbosos. |
| `GET http://localhost:843/health/live` | HTTP 200, `status=Healthy`. |
| `GET http://localhost:843/health/ready` | HTTP 200, `database=Healthy`. |
| `GET http://localhost:743` | HTTP 200, `index.html`. |
| `GET http://localhost:843/scalar` | HTTP 200. |
| `GET http://localhost:843/openapi/v1.json` | HTTP 200 con timeout ampliado; aprox. 49s. |
| `GET http://localhost:743/api/ach/responses` | HTTP 200 `text/html`; BRECHA, retorna SPA y no API. |

## Riesgos observados

| Riesgo | Severidad | Evidencia | Recomendacion |
|---|---|---|---|
| SPA no enruta API en compose actual | ALTA para UAT tecnico E2E | `nginx.conf` sin proxy; `/api` en puerto 743 devuelve HTML. | Definir reverse proxy UAT o modificar Nginx/compose en fase aprobada. |
| Vulnerabilidad NU1903 en `System.Security.Cryptography.Xml` 10.0.0 | ALTA seguridad | Warning durante `docker compose build`. | Actualizar dependencia de forma controlada y ejecutar CI completo. |
| OpenAPI lento | MEDIA | 49s para `/openapi/v1.json`. | Documentar timeout o optimizar generacion. |
| `.env` versionado | ALTA seguridad | `.env` existe y esta trackeado. | Revisar contenido, rotar si aplica, destrackear con procedimiento aprobado. |
| Migraciones automaticas en startup | MEDIA operacion | `Database__ApplyMigrations=true`. | Decidir si UAT/preproductivo permite auto-migrate o requiere DBA. |
| OpenBao fuera de compose principal | MEDIA/ALTA si aplica a certificados | Scripts/docs existen, servicio no. | Definir alcance UAT: OpenBao, secret manager externo o waiver. |

## Decision de readiness

El compose actual es **apto para validacion tecnica directa de API, PostgreSQL, OpenAPI/Scalar y SPA estatica**.

El compose actual **no queda apto todavia para UAT tecnico E2E desde SPA**, porque la configuracion productiva relativa de la SPA requiere un reverse proxy que no existe en `web/ach-interbank-ui/nginx.conf`.

Estado productivo: **NO-GO**.
