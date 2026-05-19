# Evidencia Tecnica UAT Runtime - ACH Interbank

Fecha de generacion: 2026-05-18  
Version: 0.1 preliminar  
Rama ejecutada: `fix/angular-transaction-create-specs`  
Rama objetivo del proyecto: `ACH-Interbank-Postgresql`  
Commit: `3f167663`  
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
| Docker build | OK | `docker compose build` construyo `achinterbank-api:local` y `achinterbank-spa:local`. |
| Docker runtime | PARCIAL | `postgres`, `achinterbank-api` y `achinterbank-spa` levantaron; falta resolver enrutamiento SPA->API por puerto 743. |
| PostgreSQL runtime | OK | Contenedor `achinterbank-postgres` en estado `healthy`. |
| API live | OK | `GET http://localhost:843/health/live` respondio 200. |
| API ready | OK | `GET http://localhost:843/health/ready` respondio 200 con `database=Healthy`. |
| SPA runtime | OK tecnico | `GET http://localhost:743` respondio 200 con `index.html`. |
| SPA hacia API same-origin | BRECHA | `GET http://localhost:743/api/ach/responses` devuelve `index.html`, no proxy a API. |
| OpenAPI | OK con observacion | `GET http://localhost:843/openapi/v1.json` respondio 200 en aproximadamente 49 segundos, tamano aprox. 749 KB. |
| Scalar | OK | `GET http://localhost:843/scalar` respondio 200. |
| OpenBao/secrets | PENDIENTE / NO APLICA compose actual | OpenBao no esta en `docker-compose.yml`; hay scripts y docs historicas. |
| Productivo | NO-GO | Persisten brechas UAT, seguridad, OpenBao, proxy SPA/API y aprobaciones. |

## Servicios y puertos

| Servicio | Imagen | Estado observado | Puerto host | Puerto contenedor | Observacion |
|---|---|---|---:|---:|---|
| `postgres` | `postgres:16` | Up / healthy | No publicado | 5432 | Volumen `ach_postgres_data`. |
| `achinterbank-api` | `achinterbank-api:local` | Up | 843 | 8080 | Health live/ready OK. |
| `achinterbank-spa` | `achinterbank-spa:local` | Up | 743 | 80 | Sirve SPA; no proxy hacia API. |

## Validaciones ejecutadas

| Comando | Resultado | Observacion |
|---|---|---|
| `git branch --show-current` | OK | `fix/angular-transaction-create-specs`. |
| `git rev-parse --short HEAD` | OK | `3f167663`. |
| `docker --version` | OK | Docker `29.4.3`. |
| `docker compose version` | OK | Compose `v5.1.3`. |
| `docker compose config --quiet` | OK | Configuracion valida. |
| `docker compose build` | OK | API y SPA construyen desde Docker. |
| `docker compose up -d` | OK | Contenedores levantados sin `down -v` ni borrado de volumenes. |
| `docker compose ps` | OK | Tres servicios `Up`; PostgreSQL `healthy`. |
| `docker compose logs --tail=120` | OK con observaciones | EF aplico migraciones al iniciar; logs verbosos de EF/Quartz. |
| `Invoke-WebRequest http://localhost:843/health/live` | OK | HTTP 200. |
| `Invoke-WebRequest http://localhost:843/health/ready` | OK | HTTP 200, DB healthy. |
| `Invoke-WebRequest http://localhost:743` | OK | HTTP 200, SPA servida. |
| `Invoke-WebRequest http://localhost:843/scalar` | OK | HTTP 200. |
| `Invoke-WebRequest http://localhost:843/openapi/v1.json` | OK lento | Primer intento con 20s hizo timeout; reintento con 180s respondio 200 en aprox. 49s. |
| `docker exec achinterbank-postgres ... information_schema.tables` | OK | 130 tablas en esquema `public`. |

## Logs relevantes resumidos

- PostgreSQL inicializo base `ACHInterbank` y quedo listo para conexiones.
- La API ejecuto migraciones EF Core en startup por `Database__ApplyMigrations=true`.
- Se observaron inserts en `__EFMigrationsHistory` y creacion de indice de idempotencia para ingestion NACHA entrante.
- El mensaje inicial de PostgreSQL sobre `__EFMigrationsHistory` inexistente aparece durante la deteccion normal previa a crear/aplicar historial.
- Nginx de la SPA sirvio `index.html` correctamente.
- Las rutas `/api`, `/health` y `/openapi` consultadas en el puerto 743 fueron atendidas por fallback SPA, no por API.
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
| API relativa desde SPA | BRECHA | `http://localhost:743/api/ach/responses` devolvio HTML SPA. |
| Nginx proxy API | NO ENCONTRADO | `web/ach-interbank-ui/nginx.conf` solo contiene `try_files` hacia `index.html`. |

## OpenAPI / Scalar

| Elemento | Estado | Evidencia |
|---|---|---|
| Scalar UI | OK | HTTP 200 en `/scalar`. |
| OpenAPI JSON | OK lento | HTTP 200 en `/openapi/v1.json`, aprox. 49s, 181 paths. |
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
| RUNTIME-01 | SPA productiva usa base relativa, pero Nginx del contenedor no proxya API. | Bloquea UAT tecnico E2E desde SPA en compose actual. | Definir reverse proxy UAT o ajustar `nginx.conf`/compose para enrutar rutas API al servicio `achinterbank-api`. |
| RUNTIME-02 | OpenAPI tarda aprox. 49 segundos en generarse. | Puede causar timeouts de validacion/observabilidad. | Evaluar cache/generacion previa o ampliar timeout operativo para evidencia. |
| RUNTIME-03 | `System.Security.Cryptography.Xml` 10.0.0 reporta vulnerabilidad alta. | Riesgo de seguridad pre-go-live. | Revisar advisory y actualizar paquete de forma controlada con pruebas. |
| RUNTIME-04 | `.env` esta versionado. | Riesgo de secretos si contiene valores reales. | Revision segura, rotacion si aplica y destrackeo controlado. |
| RUNTIME-05 | Migraciones automaticas activas en compose. | Puede no ser politica aceptada para UAT/preproductivo. | Definir si UAT usa migracion automatica o ventana DBA controlada. |

## Conclusion

El stack Docker queda validado tecnicamente para API, PostgreSQL, build de imagenes, health checks directos y SPA servida. Sin embargo, **no debe declararse todavia ambiente UAT tecnico E2E**, porque la SPA en `http://localhost:743` no enruta llamadas `/api` hacia la API y devuelve `index.html`.

Estado recomendado: **runtime Docker parcial; candidato a UAT tecnico solo despues de cerrar el enrutamiento SPA->API o publicar un reverse proxy UAT aprobado**.

Estado productivo: **NO-GO**.
