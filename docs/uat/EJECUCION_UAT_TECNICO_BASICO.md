# Ejecucion UAT Tecnico Basico Autenticado - ACH Interbank

Fecha de ejecucion: 2026-05-18 America/Bogota  
Version: 0.2 reintento autenticado cerrado  
Rama ejecutada: `fix/spa-docker-runtime-proxy-and-images`  
Commit: `141484fc78434322ff87f25c8914002719b35264`  
Ambiente: Docker Compose local, SPA `http://localhost:743`, API `http://localhost:843`  
Clasificacion: no incluir password, token completo, datos personales, datos reales ni certificados privados.

## Alcance

Validar UAT tecnico autenticado basico desde la SPA Docker con usuario demo sintetico aprobado.

Usuario demo usado: `admin`  
Password: no documentada; tomada desde `ACH_UAT_DEMO_PASSWORD`.  
Token: recibido y no documentado completo; evidencia enmascarada `eyJ...6_k`.  
Roles esperados: `Admin`, `ACH.Operator`.  
Roles confirmados: parcial; respuesta/JWT exponen `Admin`, no se observa `ACH.Operator`.  
Tipo: seed sintetico.

## Resultado Ejecutivo

| Control | Resultado | Evidencia |
|---|---|---|
| Git/rama/commit | OK | Rama `fix/spa-docker-runtime-proxy-and-images`, commit `141484fc78434322ff87f25c8914002719b35264`. |
| Variables demo | OK | `ACH_UAT_DEMO_USERNAME` y `ACH_UAT_DEMO_PASSWORD` presentes; valores no impresos. |
| Docker runtime | OK | `postgres` healthy, `achinterbank-api` Up, `achinterbank-spa` Up. |
| Health live via SPA | OK | `GET http://localhost:743/health/live` HTTP 200 JSON. |
| Health ready via SPA | OK | `GET http://localhost:743/health/ready` HTTP 200 JSON. |
| Login dummy | OK tecnico | `POST /auth/login` con credenciales dummy responde 401 JSON desde API. |
| Navigation sin token | OK tecnico | `GET /navigation/menu` sin token responde 401 desde API, no `index.html`. |
| Login real demo | OK | `POST /auth/login` con usuario `admin` responde 200 JSON y token presente. |
| Claims JWT | OK con observacion | JWT HS256, `sub`, `name`, `unique_name` presentes, expiracion presente; rol observado `Admin`. |
| Menu con token | OK | `GET /navigation/menu` HTTP 200 JSON; no `index.html`. |
| Endpoints protegidos read-only | OK | `/navigation/menu`, `/api/roles`, `/api/users`, `/api/ach/responses` responden 200 JSON con Bearer. |
| Proxy rutas API | OK | `/auth/login`, `/navigation/menu`, `/api/ach/responses`, `/health/live`, `/health/ready` no devuelven `index.html`. |
| Bundle SPA | OK | No contiene `localhost:843` ni `achinterbank-api:8080` en artefactos `dist`. |
| Navegacion visual | PARCIAL | Logs SPA muestran login, refresh, dashboard, menu y pantallas protegidas; browser integrado Codex sin herramienta ejecutable en esta sesion. |
| Logs | OK con observaciones | Sin 500 criticos ni tokens/passwords completos; EF debug muestra nombre de columna `PasswordHash` sin valores. PostgreSQL tiene FATAL previos por usuarios `root`/`sa`. |
| UAT tecnico autenticado basico | OK con observaciones | Criterios tecnicos de autenticacion, token, menu, endpoints y logs cumplidos. |
| Productivo | NO-GO | UAT funcional, actas, seguridad, OpenBao si aplica, backup/restore/rollback y evidencia funcional siguen pendientes. |

## Pre-check

| Item | Resultado |
|---|---|
| Rama actual | `fix/spa-docker-runtime-proxy-and-images`. |
| Commit | `141484fc78434322ff87f25c8914002719b35264`. |
| Git status inicial | Ya existian documentos UAT/go-live modificados/no rastreados; no se revirtieron. |
| Variables | `ACH_UAT_DEMO_USERNAME` presente, `ACH_UAT_DEMO_PASSWORD` presente; valores no impresos. |
| PostgreSQL | `achinterbank-postgres` Up healthy, publicado en `127.0.0.1:5432`. |
| API | `achinterbank-api` Up, `0.0.0.0:843->8080`. |
| SPA | `achinterbank-spa` Up, `0.0.0.0:743->80`. |
| Proxy `/auth/` | OK tecnico. |
| Proxy `/navigation/` | OK tecnico. |
| Proxy `/api/` | OK tecnico. |
| Proxy `/health/` | OK tecnico. |

## Escenarios Ejecutados

| ID | Escenario | Resultado | Observacion |
|---|---|---|---|
| UAT-TECH-001 | Health live via SPA | OK | HTTP 200 JSON desde API. |
| UAT-TECH-002 | Health ready via SPA | OK | HTTP 200 JSON, DB healthy. |
| UAT-TECH-003 | Login dummy negativo | OK | HTTP 401 JSON desde API; no 405 ni `index.html`. |
| UAT-TECH-004 | Navigation sin token | OK | HTTP 401 desde API; confirma `[Authorize]` y proxy. |
| UAT-TECH-005 | Login real demo por variable de entorno | OK | HTTP 200 JSON; password no impresa. |
| UAT-TECH-006 | Token/JWT claims | OK con observacion | Token recibido; rol visible `Admin`; `ACH.Operator` no visible. |
| UAT-TECH-007 | Menu con token | OK | HTTP 200 JSON, menu visible para rol autorizado. |
| UAT-TECH-008 | Endpoints protegidos read-only | OK | `/api/roles`, `/api/users`, `/api/ach/responses` HTTP 200 JSON. |
| UAT-TECH-009 | Navegacion visual automatizada | PARCIAL | Validada por logs SPA previos/actuales; no se pudo controlar browser integrado desde esta sesion. |
| UAT-TECH-010 | PostgreSQL local | OK | `Test-NetConnection localhost -Port 5432` OK; `docker compose port` OK; `pg_isready` OK. |
| UAT-TECH-011 | Logs y secretos | OK con observaciones | No se observan tokens/passwords completos; sin 500 en endpoints validados. |

## Endpoints Validados

| Endpoint | Metodo | Token | Resultado | Observacion |
|---|---|---|---|---|
| `/health/live` | GET | No | 200 JSON | API viva via proxy SPA. |
| `/health/ready` | GET | No | 200 JSON | API lista y DB healthy via proxy SPA. |
| `/auth/login` | POST | No | 401 JSON con dummy | Login negativo controlado; no expone token. |
| `/navigation/menu` | GET | No | 401 | Protegido correctamente; no retorna `index.html`. |
| `/auth/login` | POST | Credenciales demo | 200 JSON | Token recibido; password no impresa. |
| `/navigation/menu` | GET | Si | 200 JSON | Menu: Dashboard, Usuarios, Identidad y colores, Reglas de contrasena, Bloqueo de acceso, Ciclos ACH, Exportar NACHA, Layouts NACHA, Definiciones NACHA, Catalogos, Instituciones financieras, Prioridades camaras. |
| `/api/roles` | GET | Si | 200 JSON | Endpoint protegido read-only validado. |
| `/api/users` | GET | Si | 200 JSON | Endpoint protegido read-only validado; no se modificaron usuarios. |
| `/api/ach/responses` | GET | Si | 200 JSON | Endpoint protegido read-only validado. |
| `/api/ach/batches` | GET | Si | 404 sin HTML | Ruta no existente; proxy no devuelve SPA fallback. |
| `/api/ach/cycles` | GET | Si | 404 sin HTML | Ruta no existente; proxy no devuelve SPA fallback. |
| `/api/institutions` | GET | Si | 404 sin HTML | Ruta no existente; proxy no devuelve SPA fallback. |

## Pantallas Observadas Por Logs

| Pantalla | Estado | Evidencia |
|---|---|---|
| Login | OK observado | Logs Nginx muestran carga de `/auth/login` y `POST /auth/login` 200. |
| Dashboard | OK observado | Logs muestran redireccion y llamadas desde `/dashboard`. |
| Transacciones crear/listar | OK observado | Logs muestran rutas `/transactions/create`, `/transactions/list`; no se crearon transacciones. |
| Reportes | OK observado | Logs muestran carga de `/reports`. |
| ACH responses | OK observado | Logs muestran `/ach-responses`, `/api/ach/responses` y dashboard. |
| Administracion usuarios | OK observado | Logs muestran `/users`, `/api/roles`, `/api/users`; no se modificaron usuarios. |

## Logs Y Seguridad

- No se imprimio password.
- No se documento token completo.
- No se usaron datos reales.
- No se ejecutaron migraciones manuales.
- No se borraron volumenes.
- Logs API muestran `POST /auth/login` 200 y autenticacion Bearer exitosa en endpoints protegidos.
- Logs SPA muestran `POST /auth/login` 200, `POST /auth/refresh` 200, `GET /navigation/menu` 200, `GET /api/roles` 200, `GET /api/users` 200 y `GET /api/ach/responses` 200.
- Logs API no muestran token completo ni password; se observa nombre de columna `PasswordHash` por debug EF, sin valores.
- Logs PostgreSQL muestran errores previos por usuarios inexistentes `root` y `sa`; revisar origen operativo si persisten. No se imprimieron passwords.

## Defectos / Observaciones

| ID | Severidad | Descripcion | Estado |
|---|---|---|---|
| DEF-UAT-013 | Alta | Variables demo no disponibles en ejecucion anterior. En este reintento estan presentes y permitieron login real controlado. | Cerrado |
| DEF-UAT-014 | Media | Browser integrado no pudo aportar evidencia visual automatizada confiable. | Abierto como limitacion de herramienta |
| DEF-UAT-015 | Media | Rol esperado `ACH.Operator` ya aparece junto con `Admin` en respuesta de login/JWT para `admin`; menu y endpoints read-only responden 200 con Bearer. | Cerrado para UAT controlado |
| OBS-UAT-001 | Baja | Logs PostgreSQL tienen FATAL previos por usuarios `root`/`sa` inexistentes. | Abierto operativo |

## Conclusion

UAT tecnico autenticado basico: **OK con observaciones**.

Se cerro el bloqueo por variables, se ejecuto login real con usuario seed sintetico `admin`, se recibio token, se valido menu con Bearer y endpoints protegidos read-only sin `index.html` ni 500. La observacion principal es que `ACH.Operator` no se confirma en respuesta/JWT, aunque la autorizacion basica opera con `Admin`.

UAT funcional con transacciones sinteticas: **PENDIENTE**.  
Productivo: **NO-GO**.
