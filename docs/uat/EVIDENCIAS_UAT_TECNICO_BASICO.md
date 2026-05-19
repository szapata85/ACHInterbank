# Evidencias UAT Tecnico Basico - ACH Interbank

Fecha de generacion: 2026-05-18 America/Bogota  
Version: 0.2 reintento autenticado cerrado  
Rama ejecutada: `fix/spa-docker-runtime-proxy-and-images`  
Commit: `141484fc78434322ff87f25c8914002719b35264`  
Clasificacion: no incluir password, token completo, datos reales, datos personales ni secretos.

## Indice De Evidencias

| ID evidencia | Escenario | Tipo | Descripcion | Ruta o referencia segura | Responsable | Fecha | Estado | Observacion |
|---|---|---|---|---|---|---|---|---|
| EV-TECH-001 | UAT-TECH-001 | HTTP | `GET http://localhost:743/health/live` HTTP 200 JSON. | Consola Codex, salida sanitizada. | QA/DevOps | 2026-05-18 | OK | Sin datos sensibles. |
| EV-TECH-002 | UAT-TECH-002 | HTTP | `GET http://localhost:743/health/ready` HTTP 200 JSON. | Consola Codex, salida sanitizada. | QA/DevOps | 2026-05-18 | OK | Sin datos sensibles. |
| EV-TECH-003 | UAT-TECH-003 | HTTP | `POST /auth/login` con credenciales dummy devuelve 401 JSON. | Consola Codex, salida sanitizada. | QA/DevOps | 2026-05-18 | OK | Confirma proxy Auth y respuesta API. |
| EV-TECH-004 | UAT-TECH-004 | HTTP | `GET /navigation/menu` sin token devuelve 401 desde API. | Consola Codex, salida sanitizada. | QA/DevOps | 2026-05-18 | OK | Confirma proxy Navigation y auth. |
| EV-TECH-005 | UAT-TECH-005 | Configuracion | Variables demo presentes en proceso. | Consola Codex, booleanos de presencia. | QA/DevOps | 2026-05-18 | OK | No se imprimieron valores. |
| EV-TECH-006 | UAT-TECH-005 | HTTP | `POST /auth/login` con usuario demo `admin` devuelve 200 JSON y token presente. | Consola Codex, salida sanitizada. | QA/DevOps | 2026-05-18 | OK | Password no documentada. |
| EV-TECH-007 | UAT-TECH-006 | JWT | Token decodificado localmente: HS256, `sub/name/unique_name` presentes, expiracion presente. | Consola Codex, salida sanitizada. | QA/Seguridad | 2026-05-18 | OK con observacion | Token completo no impreso; rol visible `Admin`, no `ACH.Operator`. |
| EV-TECH-008 | UAT-TECH-007 | HTTP | `GET /navigation/menu` con Bearer devuelve HTTP 200 JSON. | Consola Codex, salida sanitizada. | QA/DevOps | 2026-05-18 | OK | Menu contiene opciones de administracion y ACH. |
| EV-TECH-009 | UAT-TECH-008 | HTTP | `GET /api/roles` con Bearer devuelve HTTP 200 JSON. | Consola Codex, salida sanitizada. | QA/DevOps | 2026-05-18 | OK | Read-only. |
| EV-TECH-010 | UAT-TECH-008 | HTTP | `GET /api/users` con Bearer devuelve HTTP 200 JSON. | Consola Codex, salida sanitizada. | QA/DevOps | 2026-05-18 | OK | No se modificaron usuarios. |
| EV-TECH-011 | UAT-TECH-008 | HTTP | `GET /api/ach/responses` con Bearer devuelve HTTP 200 JSON. | Consola Codex, salida sanitizada. | QA/DevOps | 2026-05-18 | OK | Read-only. |
| EV-TECH-012 | UAT-TECH-008 | HTTP | Rutas adicionales inexistentes `/api/ach/batches`, `/api/ach/cycles`, `/api/institutions` devuelven 404 sin HTML. | Consola Codex, salida sanitizada. | QA/DevOps | 2026-05-18 | OK tecnico | Evidencia de proxy, no de funcionalidad. |
| EV-TECH-013 | UAT-TECH-009 | Log SPA | Logs muestran `POST /auth/login` 200, `POST /auth/refresh` 200, `GET /navigation/menu` 200. | `docker compose logs achinterbank-spa --tail=120`. | QA/DevOps | 2026-05-18 | OK | No se imprimen tokens. |
| EV-TECH-014 | UAT-TECH-009 | Log SPA | Logs muestran navegacion a dashboard, transacciones, reportes, ACH responses y usuarios. | `docker compose logs achinterbank-spa --tail=120`. | QA/DevOps | 2026-05-18 | OK observado | No se crearon ni modificaron datos. |
| EV-TECH-015 | UAT-TECH-010 | PostgreSQL | `Test-NetConnection localhost -Port 5432` OK. | Consola Codex. | QA/DevOps | 2026-05-18 | OK | DB local accesible para UAT tecnico. |
| EV-TECH-016 | UAT-TECH-010 | PostgreSQL | `docker compose port postgres 5432` devuelve `127.0.0.1:5432`. | Consola Codex. | QA/DevOps | 2026-05-18 | OK | Loopback local. |
| EV-TECH-017 | UAT-TECH-010 | PostgreSQL | `docker exec achinterbank-postgres pg_isready -h localhost -p 5432` OK. | Consola Codex. | QA/DevOps | 2026-05-18 | OK | Sin imprimir connection strings. |
| EV-TECH-018 | UAT-TECH-011 | Seguridad | Busqueda en logs no encuentra tokens/passwords completos; no hay 500 en endpoints validados. | `docker compose logs ... | Select-String`. | QA/Seguridad | 2026-05-18 | OK con observacion | EF debug muestra nombre de columna `PasswordHash` sin valores. |
| EV-TECH-019 | UAT-TECH-011 | Bundle SPA | Artefactos `dist` no contienen `localhost:843` ni `achinterbank-api:8080`. | Escaneo local sanitizado. | QA/Seguridad | 2026-05-18 | OK | Llamadas productivas usan rutas relativas/proxy. |
| EV-TECH-020 | UAT-TECH-009 | Browser | Browser integrado no se pudo controlar desde esta sesion con herramienta ejecutable. | Limitacion de herramienta Codex. | QA/DevOps | 2026-05-18 | PARCIAL | No bloquea UAT tecnico HTTP con token. |

## Evidencia HTTP Sanitizada

| Comando/accion | Resultado |
|---|---|
| `GET http://localhost:743/health/live` | HTTP 200, `application/json`, no HTML. |
| `GET http://localhost:743/health/ready` | HTTP 200, `application/json`, no HTML. |
| `POST http://localhost:743/auth/login` con dummy | HTTP 401, `application/json`; no 405 ni HTML. |
| `GET http://localhost:743/navigation/menu` sin token | HTTP 401; no HTML SPA. |
| `POST http://localhost:743/auth/login` con demo | HTTP 200, `application/json`; token presente; token enmascarado `eyJ...6_k`. |
| `GET http://localhost:743/navigation/menu` con Bearer | HTTP 200, `application/json`; menu JSON. |
| `GET http://localhost:743/api/roles` con Bearer | HTTP 200, `application/json`; no HTML. |
| `GET http://localhost:743/api/users` con Bearer | HTTP 200, `application/json`; no HTML. |
| `GET http://localhost:743/api/ach/responses` con Bearer | HTTP 200, `application/json`; no HTML. |

## Evidencia Logs Sanitizada

| Fuente | Observacion |
|---|---|
| API | Login demo 200, Bearer validado correctamente, endpoints protegidos sin 500. |
| SPA/Nginx | Respuestas 200 para `/auth/login`, `/auth/refresh`, `/navigation/menu`, `/api/roles`, `/api/users`, `/api/ach/responses`. |
| PostgreSQL | Servicio listo; existen errores previos por roles inexistentes `root`/`sa`, sin passwords impresos. |

## Clasificacion

UAT tecnico autenticado basico: **OK con observaciones**.  
UAT funcional con datos sinteticos: **PENDIENTE**.  
Productivo: **NO-GO**.
