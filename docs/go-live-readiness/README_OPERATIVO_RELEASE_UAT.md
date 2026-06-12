# README Operativo Release UAT - ACH Interbank

Fecha de generacion: 2026-05-18  
Version: 0.1 preliminar  
Rama analizada: `ACH-Interbank-Postgresql`  
Estado: guia operativa preliminar; no ejecutar comandos destructivos sin autorizacion.

## 1. Objetivo

Guiar la preparacion y validacion no destructiva de un release UAT de ACH Interbank. No incluye secretos reales ni datos sensibles.

## 2. Rutas Relevantes

| Elemento | Ruta |
|---|---|
| Solucion | `ACHInterbank.sln` |
| API | `src/Cfa.ACHInterbank.Api` |
| Persistence | `src/Cfa.ACHInterbank.Persistence` |
| Tests backend | `tests/Cfa.ACHInterbank.Tests` |
| SPA | `web/ach-interbank-ui` |
| Compose principal | `docker-compose.yml` |
| Compose test | `docker-compose.test.yml` |
| Env ejemplo test | `.env.test.example` |
| Docs UAT | `docs/uat` |
| Go-live readiness | `docs/go-live-readiness` |

## 3. Comandos No Destructivos Sugeridos

No ejecutar desde este documento automaticamente; registrar salida en evidencia cuando se ejecuten.

```bash
bash scripts/codex/setup-codex-env.sh
dotnet restore ACHInterbank.sln
dotnet build ACHInterbank.sln -c Release
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release
```

## 4. PostgreSQL De Test

```bash
docker compose -f docker-compose.test.yml --env-file .env.test.example up -d
docker compose -f docker-compose.test.yml --env-file .env.test.example logs postgres-ach-test
```

No borrar volumenes durante UAT sin aprobacion explicita.

## 5. Migraciones EF

Validar primero contra ambiente controlado. No ejecutar migraciones productivas desde este README.

```bash
dotnet ef database update \
  --project src/Cfa.ACHInterbank.Persistence/Cfa.ACHInterbank.Persistence.csproj \
  --startup-project src/Cfa.ACHInterbank.Api/Cfa.ACHInterbank.Api.csproj \
  --context AchDbContext
```

## 6. SPA Angular

```bash
cd web/ach-interbank-ui
npm ci
npm run build
npm test -- --watch=false --browsers=ChromeHeadless
```

Validar antes que `environment.prod.ts` no apunte a `localhost` para UAT/productivo.

Estado actual: `environment.prod.ts` usa base relativa para despliegue detras del mismo reverse proxy. Si UAT/preproductivo requiere dominio dedicado de API, definirlo por pipeline o configuracion de ambiente aprobada.

## 7. Docker Compose

```bash
docker compose build
docker compose up -d
docker compose logs achinterbank-api
docker compose logs achinterbank-spa
```

Consideraciones:

- No usar credenciales reales en compose.
- Los defaults del compose principal son placeholders locales/de demo; para UAT/preproductivo usar variables no versionadas o secret manager.
- La custodia de secretos no se resuelve en `docker-compose.yml` principal; si aplica, usar el mecanismo aprobado del ambiente.

## 8. Health Checks

```bash
curl http://localhost:843/health/live
curl http://localhost:843/health/ready
```

Observacion: los health checks actuales validan live y ready con DB. Quartz/externos requieren evidencia adicional o monitoreo alterno.

Evidencia runtime Docker 2026-05-18:

| Validacion | Resultado | Observacion |
|---|---|---|
| `docker compose config --quiet` | OK | Configuracion valida. |
| `docker compose build achinterbank-spa` | OK | SPA construye con `node:24-alpine` y `nginx:1.30.1-alpine`. |
| `docker compose up -d` | OK directo | PostgreSQL, API y SPA levantaron. |
| `http://localhost:843/health/live` | OK | HTTP 200. |
| `http://localhost:843/health/ready` | OK | HTTP 200 con DB healthy. |
| `http://localhost:743` | OK | SPA estatica servida. |
| `http://localhost:743/api/...` | OK tecnico | Proxy Nginx hacia API; endpoint protegido devuelve 401, no `index.html`. |
| `POST http://localhost:743/auth/login` | OK tecnico | Proxy Nginx hacia API; credenciales dummy devuelven 401 JSON, no 405 ni `index.html`. |
| `GET http://localhost:743/navigation/menu` | OK tecnico | Proxy Nginx hacia API; sin token devuelve 401, no `index.html`. |
| `localhost:5432` | OK tecnico/local | PostgreSQL publicado en `127.0.0.1:${POSTGRES_HOST_PORT:-5432}:5432`; no implica aprobacion productiva. |
| `http://localhost:843/openapi/v1.json` | OK lento | HTTP 200 con timeout ampliado; aprox. 79s directo y 96s via proxy. |

Ver detalle en `docs/uat/EVIDENCIA_TECNICA_UAT_RUNTIME.md` y `docs/go-live-readiness/DOCKER_RUNTIME_READINESS.md`.

## 9. Secretos Y Certificados

- No registrar tokens, passwords, PFX, llaves privadas ni certificados privados en Git.
- `.env` real no debe versionarse; `.gitignore` protege futuros archivos locales, pero si `.env` ya estaba trackeado requiere revision humana y posible rotacion.
- Usar `secretRef` enmascarado cuando aplique.
- Si aplica un mecanismo externo de custodia de secretos, validar el procedimiento aprobado y documentar resultado sin exponer credenciales.

## 10. Checklist Antes De Entregar A Negocio

- [ ] Build backend ejecutado y evidenciado.
- [ ] Tests backend ejecutados y evidenciados.
- [ ] Build SPA ejecutado y evidenciado.
- [ ] Ambiente UAT levantado.
- [ ] Health checks OK.
- [ ] Datos UAT anonimizados preparados.
- [ ] Usuarios/roles UAT disponibles.
- [ ] Matriz de escenarios publicada.
- [ ] Indice de evidencias preparado.
- [ ] Matriz de defectos preparada.

## 11. Checklist Despues De Ejecutar UAT

- [ ] Todos los escenarios ejecutados o justificados.
- [ ] Evidencias registradas con hash/referencia.
- [ ] Defectos clasificados.
- [ ] Riesgos aceptados o rechazados.
- [ ] Acta UAT firmada.
- [ ] Scorecard actualizado.
- [ ] Comite informado.

## 12. Registro De Evidencias

Usar `docs/uat/INDICE_EVIDENCIAS_UAT.md`.

Evidencias sensibles deben quedar en repositorio seguro externo; en Git solo va la referencia y hash.

## 13. Registro De Defectos

Usar `docs/uat/MATRIZ_DEFECTOS_UAT.md`.

Defectos bloqueantes o altos requieren decision formal antes de go productivo.

## 14. Validacion Local 2026-05-18

| Comando | Resultado | Observacion |
|---|---|---|
| `dotnet restore ACHInterbank.sln` | OK | Proyectos al dia. |
| `dotnet build ACHInterbank.sln -c Release` | OK | 0 errores, 0 warnings. |
| `dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release` | PARCIAL | 1086 OK, 1 skip, 1 falla existente en certificacion preproductiva de ciclo cerrado. |
| `dotnet test ... --filter ...correcciones...` | OK | 5/5 pruebas nuevas o relacionadas pasaron. |
| `npm run build` | OK | Build Angular exitoso; advertencia Browserslist fuera de soporte. |
| `npm test -- --watch=false --browsers=ChromeHeadless` | OK | 147 specs OK en validacion local posterior; Angular CI remoto reportado OK. |
| `npm test -- --include=...interoperability-api.service.spec.ts` | OK | 3/3 pruebas de contrato SPA de interoperabilidad pasaron. |
| `docker compose config --quiet` | OK | Validacion sintactica OK. |

## 15. Validacion Docker Runtime 2026-05-18

| Comando | Resultado | Observacion |
|---|---|---|
| `docker compose config --quiet` | OK | Sin errores. |
| `docker compose build achinterbank-spa` | OK | Build SPA OK con `node:24-alpine` y `nginx:1.30.1-alpine`. |
| `docker compose up -d` | OK | No se ejecuto `down -v` ni borrado de volumenes. |
| `docker compose ps` | OK | `postgres` healthy; API y SPA Up. |
| `Invoke-WebRequest http://localhost:843/health/live` | OK | HTTP 200. |
| `Invoke-WebRequest http://localhost:843/health/ready` | OK | HTTP 200, DB healthy. |
| `Invoke-WebRequest http://localhost:743` | OK | SPA servida. |
| `Invoke-WebRequest http://localhost:843/scalar` | OK | HTTP 200. |
| `Invoke-WebRequest http://localhost:843/openapi/v1.json` | OK con observacion | Requiere timeout amplio; aprox. 79s. |
| `Invoke-WebRequest http://localhost:743/health/live` | OK | HTTP 200 JSON desde API por proxy. |
| `Invoke-WebRequest http://localhost:743/health/ready` | OK | HTTP 200 JSON desde API por proxy. |
| `Invoke-WebRequest http://localhost:743/openapi/v1.json` | OK lento | Retorna JSON OpenAPI por proxy; aprox. 96s. |
| `Invoke-WebRequest http://localhost:743/scalar` | OK | Retorna Scalar por proxy, no SPA. |
| `Invoke-WebRequest http://localhost:743/api/ach/responses` | OK tecnico | Retorna 401 desde API; autorizacion intacta. |
| `Invoke-WebRequest -Method Post http://localhost:743/auth/login` | OK tecnico | Retorna 401 JSON desde API con credenciales dummy; no retorna 405 ni HTML SPA. |
| `Invoke-WebRequest http://localhost:743/navigation/menu` | OK tecnico | Retorna 401 desde API sin token; no retorna HTML SPA. |
| `docker compose port postgres 5432` | OK | `127.0.0.1:5432`. |
| `Test-NetConnection localhost -Port 5432` | OK | `TcpTestSucceeded=True`. |

Decision: el compose queda apto para UAT tecnico E2E basico desde SPA, incluido login via `/auth/`, menu via `/navigation/` y PostgreSQL local para troubleshooting controlado, condicionado a datos anonimizados, usuarios/roles y evidencias. Productivo sigue NO-GO.
