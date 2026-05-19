# Evidencia Tecnica UAT Runtime - ACH Interbank

Fecha de generacion: 2026-05-18  
Version: 0.1 preliminar  
Rama ejecutada: `fix/spa-docker-runtime-proxy-and-images`  
Rama objetivo del proyecto: `ACH-Interbank-Postgresql`  
Commit: `db3bdb27`  
Ambiente: Docker Compose local en Windows 11 / Docker Desktop  
Validacion humana requerida: si  
Clasificacion: no incluir secretos, tokens, certificados privados, datos personales ni datos reales en Git.

## Resumen

| Control | Resultado | Evidencia |
|---|---|---|
| dotnet-ci remoto | OK | Reportado como OK por contexto de ejecucion. |
| angular-ci remoto | OK | Reportado como OK por contexto de ejecucion. |
| Docker version | OK | Docker `29.4.3`. |
| Docker Compose version | OK | Docker Compose `v5.1.3`. |
| Docker compose config | OK | `docker compose config --quiet` finalizo con exit code 0. |
| Docker build | OK | `docker compose build achinterbank-spa` construyo `achinterbank-spa:local` con Node 24 y Nginx 1.30.1; `docker compose up -d` reconstruyo API OK. |
| Docker runtime | OK tecnico | `postgres`, `achinterbank-api` y `achinterbank-spa` levantaron; proxy SPA->API validado por puerto 743. |
| PostgreSQL runtime | OK | Contenedor `achinterbank-postgres` en estado `healthy`. |
| API live | OK | `GET http://localhost:843/health/live` respondio 200. |
| API ready | OK | `GET http://localhost:843/health/ready` respondio 200 con `database=Healthy`. |
| SPA runtime | OK tecnico | `GET http://localhost:743` respondio 200 con `index.html`. |
| SPA hacia API same-origin | OK tecnico | `GET http://localhost:743/api/ach/responses` respondio 401 desde API, no `index.html`; auth intacta. |
| SPA hacia Auth same-origin | OK tecnico | `POST http://localhost:743/auth/login` con credenciales dummy respondio 401 JSON desde API, no 405 ni `index.html`. |
| OpenAPI | OK con observacion | `GET http://localhost:843/openapi/v1.json` respondio 200 en aproximadamente 79s; via proxy `http://localhost:743/openapi/v1.json` respondio 200 en aproximadamente 96s. |
| Scalar | OK | `GET http://localhost:843/scalar` respondio 200. |
| OpenBao/secrets | PENDIENTE / NO APLICA compose actual | OpenBao no esta en `docker-compose.yml`; hay scripts y docs historicas. |
| Productivo | NO-GO | Persisten brechas UAT real/anonimizado, seguridad, OpenBao si aplica y aprobaciones. |

## Estrategia SPA -> API

Estrategia elegida: **Opcion B - proxy Nginx + rutas relativas**.

Justificacion:

- Es mas parecida a un despliegue detras de reverse proxy.
- Evita publicar `localhost:843` dentro del build productivo de Angular.
- Mantiene `environment.prod.ts` con `apiBaseUrl: ''`.
- El backend interno Docker queda en `http://achinterbank-api:8080`.
- No cambia logica Angular, backend, autenticacion ni autorizacion.

No se usa Node 26 porque Angular 21 soporta oficialmente Node `^20.19.0`, `^22.12.0` y `^24.0.0`; Node 26 no debe adoptarse para readiness/UAT hasta soporte oficial.

| Elemento | Antes | Despues |
|---|---|---|
| Node Docker SPA | `node:22-alpine` | `node:24-alpine` |
| Nginx Docker SPA | `nginx:1.27-alpine` | `nginx:1.30.1-alpine` |
| API host | `http://localhost:843` | `http://localhost:843` |
| API interna Docker | NO DOCUMENTADO EN NGINX | `http://achinterbank-api:8080` |
| SPA host | `http://localhost:743` | `http://localhost:743` |
| `apiBaseUrl` productivo | `http://localhost:843` | `''` |

## Servicios y puertos

| Servicio | Imagen | Estado observado | Puerto host | Puerto contenedor | Observacion |
|---|---|---|---:|---:|---|
| `postgres` | `postgres:16` | Up / healthy | No publicado | 5432 | Volumen `ach_postgres_data`. |
| `achinterbank-api` | `achinterbank-api:local` | Up | 843 | 8080 | Health live/ready OK. |
| `achinterbank-spa` | `achinterbank-spa:local` | Up | 743 | 80 | Sirve SPA y proxya `/auth`, `/api`, `/health`, `/openapi`, `/scalar` hacia API. |

## Validaciones ejecutadas

| Comando | Resultado | Observacion |
|---|---|---|
| `git branch --show-current` | OK | `fix/angular-transaction-create-specs`. |
| `git rev-parse --short HEAD` | OK | `3f167663`. |
| `docker --version` | OK | Docker `29.4.3`. |
| `docker compose version` | OK | Compose `v5.1.3`. |
| `docker compose config --quiet` | OK | Configuracion valida. |
| `npm run build` | OK | Build Angular local exitoso. |
| `npm test -- --watch=false --browsers=ChromeHeadless` | OK | 147 specs OK. |
| `docker compose build achinterbank-spa` | OK | SPA construye con `node:24-alpine` y `nginx:1.30.1-alpine`. |
| `docker compose up -d` | OK | Contenedores levantados sin `down -v` ni borrado de volumenes. |
| `docker compose ps` | OK | Tres servicios `Up`; PostgreSQL `healthy`. |
| `docker compose logs --tail=120` | OK con observaciones | EF aplico migraciones al iniciar; logs verbosos de EF/Quartz. |
| `Invoke-WebRequest http://localhost:843/health/live` | OK | HTTP 200. |
| `Invoke-WebRequest http://localhost:843/health/ready` | OK | HTTP 200, DB healthy. |
| `Invoke-WebRequest http://localhost:743` | OK | HTTP 200, SPA servida. |
| `Invoke-WebRequest http://localhost:843/scalar` | OK | HTTP 200. |
| `Invoke-WebRequest http://localhost:843/openapi/v1.json` | OK lento | HTTP 200 en aprox. 79s. |
| `Invoke-WebRequest http://localhost:743/health/live` | OK | HTTP 200 JSON desde API por proxy. |
| `Invoke-WebRequest http://localhost:743/health/ready` | OK | HTTP 200 JSON desde API por proxy. |
| `Invoke-WebRequest http://localhost:743/openapi/v1.json` | OK lento | HTTP 200 JSON desde API por proxy en aprox. 96s. |
| `Invoke-WebRequest http://localhost:743/scalar` | OK | HTTP 200 Scalar via proxy, no SPA. |
| `Invoke-WebRequest http://localhost:743/api/ach/responses` | OK tecnico | HTTP 401 desde API; no retorna `index.html`. |
| `Invoke-WebRequest -Method Post http://localhost:743/auth/login` | OK tecnico | HTTP 401 JSON desde API con credenciales dummy; no retorna 405 ni `index.html`. |
| `docker compose exec -T achinterbank-spa ... achinterbank-api:8080/health/*` | OK | API interna Docker responde live/ready desde la red del compose. |
| `docker exec achinterbank-postgres ... information_schema.tables` | OK | 130 tablas en esquema `public`. |

## Logs relevantes resumidos

- PostgreSQL inicializo base `ACHInterbank` y quedo listo para conexiones.
- La API ejecuto migraciones EF Core en startup por `Database__ApplyMigrations=true`.
- Se observaron inserts en `__EFMigrationsHistory` y creacion de indice de idempotencia para ingestion NACHA entrante.
- El mensaje inicial de PostgreSQL sobre `__EFMigrationsHistory` inexistente aparece durante la deteccion normal previa a crear/aplicar historial.
- Nginx de la SPA sirvio `index.html` correctamente.
- Nginx de la SPA proxyo `/auth`, `/api`, `/health`, `/openapi` y `/scalar` hacia `achinterbank-api:8080`.
- `/api/ach/responses` via `http://localhost:743` respondio 401, confirmando que ya llega al backend y mantiene autorizacion.
- `/auth/login` via `http://localhost:743` respondio 401 JSON con credenciales dummy, confirmando que ya llega al backend y no cae en Nginx estatico.
- Durante `docker compose build`, `dotnet restore` reporto warning NU1903 de vulnerabilidad alta en `System.Security.Cryptography.Xml` 10.0.0. Requiere revision de seguridad y actualizacion controlada.

## Resultado API y base de datos

| Elemento | Estado | Evidencia |
|---|---|---|
| API viva | OK | `/health/live` HTTP 200. |
| API lista | OK | `/health/ready` HTTP 200. |
| Conexion DB desde API | OK | `database=Healthy`. |
| Esquema DB | OK tecnico | 130 tablas en `public`. |
| Migraciones automaticas | OK tecnico / PENDIENTE VALIDAR operacion | Compose activa `Database__ApplyMigrations=true`; validar si UAT debe aplicar migraciones automaticas o por ventana controlada. |
| Seeds | PENDIENTE VALIDAR | Logs muestran consultas de scheduler; no se valido catalogo funcional completo. |

## Resultado SPA

| Elemento | Estado | Evidencia |
|---|---|---|
| SPA servida | OK | `http://localhost:743` HTTP 200. |
| Build productivo en Docker | OK | `npm run build -- --configuration production` dentro de Docker finalizo OK. |
| API relativa desde SPA | OK tecnico | `http://localhost:743/api/ach/responses` respondio 401 desde API, no HTML SPA. |
| Auth relativa desde SPA | OK tecnico | `http://localhost:743/auth/login` respondio 401 JSON desde API con credenciales dummy, no 405 ni HTML SPA. |
| Nginx proxy API/Auth | OK | `web/ach-interbank-ui/nginx.conf` proxya `/auth`, `/api`, `/health`, `/openapi` y `/scalar`. |

## OpenAPI / Scalar

| Elemento | Estado | Evidencia |
|---|---|---|
| Scalar UI | OK | HTTP 200 en `/scalar` directo y via proxy `:743/scalar`. |
| OpenAPI JSON | OK lento | HTTP 200 en `/openapi/v1.json`, aprox. 79s directo y 96s via proxy. |
| Endpoints criticos en OpenAPI | PARCIAL | Se observaron rutas ACH, transactions, NACHA, returns, reports y health. Requiere matriz formal para UAT. |

## OpenBao / secretos

| Control | Estado | Evidencia |
|---|---|---|
| OpenBao en compose principal | NO ENCONTRADO | `docker-compose.yml` no define servicio OpenBao. |
| Scripts OpenBao | EXISTE | `scripts/openbao/*`. |
| Docs OpenBao | EXISTE | `docs/architecture/openbao-integration-2026-04-22.md`, `docs/dev/docker-compose-openbao-uat-2026-04-22.md`. |
| Runtime actual requiere OpenBao | NO | `DigitalEnvelope__CertificateSecretResolver__FailIfSecretProviderUnavailable=false`; OpenBao no se activo. |
| UAT con secretos externos | PENDIENTE | Requiere decision seguridad/operacion. |

## Brechas encontradas

| ID | Brecha | Impacto | Accion recomendada |
|---|---|---|---|
| RUNTIME-01 | SPA productiva usa base relativa y Nginx proxya API/Auth correctamente. | Mejora UAT tecnico E2E basico desde SPA. | Cerrada tecnicamente; validar funcionalmente con usuarios/datos anonimizados. |
| RUNTIME-02 | OpenAPI tarda aprox. 79-96 segundos en generarse. | Puede causar timeouts de validacion/observabilidad. | Evaluar cache/generacion previa o ampliar timeout operativo para evidencia. |
| RUNTIME-03 | `System.Security.Cryptography.Xml` 10.0.0 reporta vulnerabilidad alta. | Riesgo de seguridad pre-go-live. | Revisar advisory y actualizar paquete de forma controlada con pruebas. |
| RUNTIME-04 | `.env` esta versionado. | Riesgo de secretos si contiene valores reales. | Revision segura, rotacion si aplica y destrackeo controlado. |
| RUNTIME-05 | Migraciones automaticas activas en compose. | Puede no ser politica aceptada para UAT/preproductivo. | Definir si UAT usa migracion automatica o ventana DBA controlada. |

## Conclusion

El stack Docker queda validado tecnicamente para API, PostgreSQL, build de imagenes, health checks directos, SPA servida y proxy SPA->API/Auth. Las rutas `/auth`, `/api`, `/health`, `/openapi` y `/scalar` desde `http://localhost:743` ya no caen al fallback Angular.

Estado recomendado: **ambiente apto para UAT tecnico E2E basico desde SPA**, condicionado a ejecutar escenarios con datos anonimizados, usuarios/roles y evidencias formales.

Estado productivo: **NO-GO**.
