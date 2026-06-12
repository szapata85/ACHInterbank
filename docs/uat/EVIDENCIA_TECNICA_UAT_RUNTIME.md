# Evidencia Tecnica UAT Runtime - ACH Interbank

Fecha de generacion/revalidacion: 2026-05-18 / 2026-06-12
Version: 0.2 cierre tecnico G3.5-G3.6
Rama ejecutada/objetivo: `ACH-Interbank-Postgresql`
Commit vigente: `e57211506d381acc43d398e72277911720e6323e`
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
| Docker runtime | OK tecnico | `postgres`, `achinterbank-api` y `achinterbank-spa` levantaron; proxy SPA->API/Auth/Navigation validado por puerto 743. |
| PostgreSQL runtime | OK | Contenedor `achinterbank-postgres` en estado `healthy`; publicado solo en loopback `127.0.0.1:5432->5432` para UAT tecnico/local. |
| API live | OK | `GET http://localhost:843/health/live` respondio 200. |
| API ready | OK | `GET http://localhost:843/health/ready` respondio 200 con `database=Healthy`. |
| SPA runtime | OK tecnico | `GET http://localhost:743` respondio 200 con `index.html`. |
| SPA hacia API same-origin | OK tecnico | `GET http://localhost:743/api/ach/responses` respondio 401 desde API, no `index.html`; auth intacta. |
| SPA hacia Auth same-origin | OK tecnico | `POST http://localhost:743/auth/login` con credenciales dummy respondio 401 JSON desde API, no 405 ni `index.html`. |
| SPA hacia Navigation same-origin | OK tecnico | `GET http://localhost:743/navigation/menu` sin token respondio 401 desde API, no `index.html`; con token valido debe devolver JSON del menu. |
| OpenAPI | OK con observacion | `GET http://localhost:843/openapi/v1.json` respondio 200 en aproximadamente 79s; via proxy `http://localhost:743/openapi/v1.json` respondio 200 en aproximadamente 96s. |
| Scalar | OK | `GET http://localhost:843/scalar` respondio 200. |
| Custodia de secretos | FUERA DEL COMPOSE | El stack UAT no depende de un servicio externo de secretos. |
| G3.6A inbound | GO tecnico | SPA/API/PostgreSQL/Quartz reales; 2/2 Playwright; `Proc_Transacciones` dry-run. |
| G3.6B outbound | GO tecnico con observacion | 2/2 Playwright; `Proc_Contrapartidas` dry-run; correlacion por `AchCycleId`. |
| Productivo | NO-GO | Persisten brechas UAT real/anonimizado, seguridad y aprobaciones. |

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
| `postgres` | `postgres:16` | Up / healthy | 5432 en `127.0.0.1` | 5432 | Volumen `ach_postgres_data`; publicacion local parametrizada con `POSTGRES_HOST_PORT`. |
| `achinterbank-api` | `achinterbank-api:local` | Up | 843 | 8080 | Health live/ready OK. |
| `achinterbank-spa` | `achinterbank-spa:local` | Up | 743 | 80 | Sirve SPA y proxya `/auth`, `/navigation`, `/api`, `/health`, `/openapi`, `/scalar` hacia API. |

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
| `Invoke-WebRequest http://localhost:743/navigation/menu` | OK tecnico | HTTP 401 desde API sin token; no retorna `index.html`. |
| `docker compose port postgres 5432` | OK | `127.0.0.1:5432`. |
| `Test-NetConnection localhost -Port 5432` | OK | `TcpTestSucceeded=True`. |
| `docker exec achinterbank-postgres pg_isready -h localhost -p 5432` | OK | `localhost:5432 - accepting connections`. |
| `docker compose exec -T achinterbank-spa ... achinterbank-api:8080/health/*` | OK | API interna Docker responde live/ready desde la red del compose. |
| `docker exec achinterbank-postgres ... information_schema.tables` | OK | 130 tablas en esquema `public`. |

## Logs relevantes resumidos

- PostgreSQL inicializo base `ACHInterbank` y quedo listo para conexiones.
- La corrida histórica original ejecutó migraciones EF Core en startup.
- Desde G3.5.2, Compose usa `Database__ApplyMigrations=${DATABASE_APPLY_MIGRATIONS:-false}` y no migra por defecto.
- Las pruebas G3.6 deben usar una base previamente provisionada.
- Nginx de la SPA sirvio `index.html` correctamente.
- Nginx de la SPA proxyo `/auth`, `/navigation`, `/api`, `/health`, `/openapi` y `/scalar` hacia `achinterbank-api:8080`.
- `/api/ach/responses` via `http://localhost:743` respondio 401, confirmando que ya llega al backend y mantiene autorizacion.
- `/auth/login` via `http://localhost:743` respondio 401 JSON con credenciales dummy, confirmando que ya llega al backend y no cae en Nginx estatico.
- `/navigation/menu` via `http://localhost:743` respondio 401 sin token, confirmando que ya llega al backend y no cae al fallback Angular; con token valido debe devolver JSON del menu.
- PostgreSQL quedo publicado en `localhost:5432` solo para UAT tecnico/local controlado; esto no implica exposicion productiva aprobada.
- Durante `docker compose build`, `dotnet restore` reporto warning NU1903 de vulnerabilidad alta en `System.Security.Cryptography.Xml` 10.0.0. Revalidacion 2026-05-19: corregido tecnicamente con referencia explicita `System.Security.Cryptography.Xml` 10.0.8 y `dotnet list ... --vulnerable` sin hallazgos.

## Resultado API y base de datos

| Elemento | Estado | Evidencia |
|---|---|---|
| API viva | OK | `/health/live` HTTP 200. |
| API lista | OK | `/health/ready` HTTP 200. |
| Conexion DB desde API | OK | `database=Healthy`. |
| Esquema DB | OK tecnico | 130 tablas en `public`. |
| Migraciones automaticas | DESHABILITADAS POR DEFECTO | Solo pueden habilitarse explícitamente fuera de G3.6 con `DATABASE_APPLY_MIGRATIONS=true`. |
| Seeds | PENDIENTE VALIDAR | Logs muestran consultas de scheduler; no se valido catalogo funcional completo. |

## Resultado SPA

| Elemento | Estado | Evidencia |
|---|---|---|
| SPA servida | OK | `http://localhost:743` HTTP 200. |
| Build productivo en Docker | OK | `npm run build -- --configuration production` dentro de Docker finalizo OK. |
| API relativa desde SPA | OK tecnico | `http://localhost:743/api/ach/responses` respondio 401 desde API, no HTML SPA. |
| Auth relativa desde SPA | OK tecnico | `http://localhost:743/auth/login` respondio 401 JSON desde API con credenciales dummy, no 405 ni HTML SPA. |
| Navigation relativa desde SPA | OK tecnico | `http://localhost:743/navigation/menu` respondio 401 desde API sin token, no HTML SPA. |
| Nginx proxy API/Auth/Navigation | OK | `web/ach-interbank-ui/nginx.conf` proxya `/auth`, `/navigation`, `/api`, `/health`, `/openapi` y `/scalar`. |

## OpenAPI / Scalar

| Elemento | Estado | Evidencia |
|---|---|---|
| Scalar UI | OK | HTTP 200 en `/scalar` directo y via proxy `:743/scalar`. |
| OpenAPI JSON | OK lento | HTTP 200 en `/openapi/v1.json`, aprox. 79s directo y 96s via proxy. |
| Endpoints criticos en OpenAPI | PARCIAL | Se observaron rutas ACH, transactions, NACHA, returns, reports y health. Requiere matriz formal para UAT. |

## Custodia de secretos

| Control | Estado | Evidencia |
|---|---|---|
| Servicio externo en compose | NO REQUERIDO | `docker-compose.yml` contiene solo PostgreSQL, API y SPA. |
| Dependencia runtime | NO | La API arranca sin un servicio externo de secretos. |
| UAT con secretos reales | BLOQUEADO | Requiere mecanismo corporativo aprobado y queda fuera de G3.6. |

## Brechas encontradas

| ID | Brecha | Impacto | Accion recomendada |
|---|---|---|---|
| RUNTIME-01 | SPA productiva usa base relativa y Nginx proxya API/Auth correctamente. | Mejora UAT tecnico E2E basico desde SPA. | Cerrada tecnicamente; validar funcionalmente con usuarios/datos anonimizados. |
| RUNTIME-02 | OpenAPI tarda aprox. 79-96 segundos en generarse. | Puede causar timeouts de validacion/observabilidad. | Evaluar cache/generacion previa o ampliar timeout operativo para evidencia. |
| RUNTIME-03 | `System.Security.Cryptography.Xml` 10.0.0 reportaba vulnerabilidad alta. | Riesgo mitigado tecnicamente; requiere monitoreo continuo. | Corregido con 10.0.8; build/test/list vulnerable OK. |
| RUNTIME-04 | `.env` esta versionado. | Riesgo de secretos si contiene valores reales. | Revision segura, rotacion si aplica y destrackeo controlado. |
| RUNTIME-05 | Migraciones automaticas deshabilitadas por defecto. | Evita cambios de esquema accidentales durante UAT. | Mantener `DATABASE_APPLY_MIGRATIONS=false` en G3.6. |
| RUNTIME-06 | PostgreSQL publicado en `localhost:5432`. | Facilita UAT tecnico local y troubleshooting con DBeaver/pgAdmin. | Mantener restringido a `127.0.0.1`; no asumir esta exposicion para productivo. |

## Conclusion

El stack Docker queda validado tecnicamente para API, PostgreSQL, build de imagenes, health checks directos, SPA servida y proxy SPA->API/Auth/Navigation. Las rutas `/auth`, `/navigation`, `/api`, `/health`, `/openapi` y `/scalar` desde `http://localhost:743` ya no caen al fallback Angular.

Estado recomendado: **ambiente apto para UAT tecnico E2E basico desde SPA**, condicionado a ejecutar escenarios con datos anonimizados, usuarios/roles y evidencias formales.

Estado productivo: **NO-GO**.

## Addendum runtime 2026-06-12 - G3.6

| Flujo | Componentes reales | Quartz | Evidencia persistida | Resultado |
|---|---|---|---|---|
| G3.6A inbound | Angular SPA, API, PostgreSQL, NachaUpload y parser | `IncomingNachaPostProcessing` | `TaskExecutionLog`, `IncomingNachaDispatchQueue`, `IncomingNachaIntegrationExecution` y tablas NACHA-M | 2/2; `Proc_Transacciones` dry-run; sin fallback a ciclo 1 |
| G3.6B outbound | Angular SPA, API, PostgreSQL y NachaExport | `AchContrapartidasByCycle` | `TaskExecutionLog`, `AchFileExports`, registry de filename, batches/items/attempts | 2/2; `PROC_DRY_RUN`; nombre `RRRRTTT.ZZZ.6` |

Controles:

- Base previamente provisionada; no se ejecutaron migraciones.
- `Database__ApplyMigrations=false` por defecto.
- No se crearon `AchCycles`.
- Configuracion temporal de ciclos/tasks restaurada por los specs.
- `.ach` se uso solo como extension fisica del fixture inbound; el nombre externo fue `0001283.001.6`.
- Los estados `FailedFinal`/`Failed` observados en dry-run no representan fallo de la prueba ni exito monetario: prueban que no hubo transmision SOAP real.
- G3.6B correlaciona export y dispatch por `AchCycleId`; no demuestra que NachaExport cause el dispatch.

Regresion asociada: build Release 0 warnings/errores; backend 1652 aprobadas y 1 omitida; Angular 347/347.
