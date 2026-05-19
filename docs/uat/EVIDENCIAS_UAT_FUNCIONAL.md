# Evidencias UAT Funcional Sintetico - ACH Interbank

Fecha de generacion: 2026-05-18 America/Bogota  
Version: 0.2 reintento proxy funcional  
Rama ejecutada: `fix/spa-docker-runtime-proxy-and-images`  
Commit: `261b1e0537e5d941f4d5f39c28bc4dc06d24f805`  
Clasificacion: no incluir password, token completo, datos reales, cuentas reales, certificados reales ni secretos.

## Indice De Evidencias

| ID evidencia | Escenario | Tipo | Descripcion | Referencia segura | Estado |
|---|---|---|---|---|---|
| EV-FUNC-001 | Pre-check runtime | Docker | `docker compose ps` muestra PostgreSQL healthy, API Up y SPA Up. | Consola Codex, salida sanitizada. | OK |
| EV-FUNC-002 | Health live | HTTP | `GET http://localhost:743/health/live` HTTP 200 JSON. | Consola Codex. | OK |
| EV-FUNC-003 | Health ready | HTTP | `GET http://localhost:743/health/ready` HTTP 200 JSON. | Consola Codex. | OK |
| EV-FUNC-004 | Login demo | HTTP | `POST http://localhost:743/auth/login` HTTP 200 JSON con token presente. | Token enmascarado `eyJ...Iso`; password no impresa. | OK |
| EV-FUNC-005 | Menu autenticado | HTTP | `GET http://localhost:743/navigation/menu` HTTP 200 JSON con Bearer. | Consola Codex. | OK |
| EV-FUNC-006 | Endpoints protegidos | HTTP | `/api/roles`, `/api/users`, `/api/ach/responses` HTTP 200 JSON con Bearer. | Consola Codex. | OK |
| EV-FUNC-007 | Proxy funcional SPA | HTTP/Log SPA | Evidencia historica: rutas raiz funcionales por `:743` devolvian `text/html`/`index.html` con 200 antes del ajuste Nginx. | Logs Nginx SPA y respuestas HTTP sanitizadas. | Cerrado por reintento |
| EV-FUNC-008 | Datos maestros API directa | HTTP | Catalogos y configuraciones consultados por `:843` responden JSON. | Consola Codex. | OK con observaciones |
| EV-FUNC-009 | Datos sinteticos | HTTP/API | Creacion de `Banco UAT Origen` ID `92`, `Banco UAT Destino` ID `93` y preferencias sinteticas. | API directa, sin datos reales. | OK |
| EV-FUNC-010 | Preview transaccion | HTTP/API | Preview de `UAT-SINT-001` permite envio, sin duplicado inicial. | API directa. | OK |
| EV-FUNC-011 | Creacion transaccion | HTTP/API | `POST /transactions` HTTP 201, transaccion ID `1`, estado `Pending`. | API directa. | OK |
| EV-FUNC-012 | Persistencia DB | PostgreSQL | `AchTransactions` contiene referencia `UAT-SINT-001`, monto `1000`, estado `Pending`, timestamps presentes. | `docker exec` + `psql`, salida sanitizada. | OK |
| EV-FUNC-013 | Evento inicial | PostgreSQL/API | `AchTransactionStateEvents` devuelve `0` eventos para la transaccion sintetica. | `docker exec` + trazabilidad API. | FALLA/OBS |
| EV-FUNC-014 | Idempotencia | HTTP/API | Reintento del mismo payload devuelve HTTP 400 con rechazo controlado por duplicado. | API directa. | OK con observacion |
| EV-FUNC-015 | Trazabilidad | HTTP/API | `GET /api/ach-traceability/transactions/1` HTTP 200; origen/destino sinteticos; eventos `0`. | API directa. | PARCIAL |
| EV-FUNC-016 | Conciliacion | HTTP/API | `GET /api/reports/reconciliation` HTTP 200 para ciclo/fecha sinteticos. | API directa. | OK |
| EV-FUNC-017 | ROR/CENIT lectura | HTTP/API | Politicas ROR, causas, colas CENIT y trazabilidad CENIT responden 200. | API directa. | OK |
| EV-FUNC-018 | Logs API | Logs | Muestra revisada sin errores 500 criticos ni tokens/passwords completos. | `docker compose logs achinterbank-api --tail=900`. | OK |
| EV-FUNC-019 | Logs SPA | Logs | Evidencia historica: Nginx registraba rutas funcionales raiz con 200 y tamano `2123`, consistente con `index.html`. | `docker compose logs achinterbank-spa --tail=260`. | Cerrado por reintento |
| EV-FUNC-020 | Logs PostgreSQL | Logs | PostgreSQL sigue healthy; FATAL previos por usuarios `root`/`sa` se mantienen como observacion operativa. | `docker compose logs postgres --tail=120`. | OK con observacion |
| EV-FUNC-021 | Build/runtime SPA | Docker | `docker compose config --quiet`, `docker compose build achinterbank-spa` y `docker compose up -d` ejecutados; SPA queda Up. | Consola Codex, salida sanitizada. | OK |
| EV-FUNC-022 | Proxy funcional sin token | HTTP | `/financial-institutions`, `/ach-cycles`, `/clearing-houses`, `/transactions/company-entry-descriptions` devuelven 401 desde API, no HTML. | `curl.exe` por `http://localhost:743`. | OK |
| EV-FUNC-023 | Proxy funcional con token | HTTP | Las 4 rutas funcionales devuelven 200 JSON con Bearer demo, sin `index.html`. | Token no documentado completo. | OK |
| EV-FUNC-024 | Transacciones por proxy SPA | HTTP | `/transactions`, `/transactions/1`, `/transactions/policies/preview` devuelven 200 JSON; `POST /transactions` duplicado devuelve 400 JSON controlado. | `http://localhost:743`, salida sanitizada. | OK con observacion |

## Evidencia HTTP Sanitizada

| Accion | Resultado |
|---|---|
| `GET http://localhost:743/health/live` | 200, JSON, no HTML. |
| `GET http://localhost:743/health/ready` | 200, JSON, DB ready. |
| `POST http://localhost:743/auth/login` con demo | 200, JSON, token recibido; password no impresa. |
| `GET http://localhost:743/navigation/menu` con Bearer | 200, JSON. |
| `GET http://localhost:743/api/roles` con Bearer | 200, JSON. |
| `GET http://localhost:743/api/users` con Bearer | 200, JSON. |
| `GET http://localhost:743/api/ach/responses` con Bearer | 200, JSON. |
| `GET http://localhost:743/financial-institutions` sin token | 401 desde API, no HTML. |
| `GET http://localhost:743/ach-cycles` sin token | 401 desde API, no HTML. |
| `GET http://localhost:743/clearing-houses` sin token | 401 desde API, no HTML. |
| `GET http://localhost:743/transactions/company-entry-descriptions` sin token | 401 desde API, no HTML. |
| `GET http://localhost:743/financial-institutions` con Bearer | 200, `application/json`, no HTML. |
| `GET http://localhost:743/ach-cycles` con Bearer | 200, `application/json`, no HTML. |
| `GET http://localhost:743/clearing-houses` con Bearer | 200, `application/json`, no HTML. |
| `GET http://localhost:743/transactions/company-entry-descriptions` con Bearer | 200, `application/json`, no HTML. |
| `GET http://localhost:743/transactions` con Bearer | 200, `application/json`, no HTML. |
| `GET http://localhost:743/transactions/1` con Bearer | 200, `application/json`, no HTML. |
| `GET http://localhost:743/transactions/policies/preview` con Bearer | 200, `application/json`, no HTML. |
| `POST http://localhost:743/transactions` duplicado con Bearer | 400, `application/json`, rechazo controlado por duplicado, no HTML. |
| `POST http://localhost:843/transactions` | 201, JSON, transaccion sintetica creada. |
| Reintento `POST http://localhost:843/transactions` | 400, JSON, rechazo controlado por duplicado. |
| `GET http://localhost:843/transactions/1` | 200, JSON. |
| `GET http://localhost:843/api/ach-traceability/transactions/1` | 200, JSON; sin eventos iniciales. |
| `GET http://localhost:843/api/reports/reconciliation` | 200. |

## Evidencia De Datos Maestros

| Dominio | Evidencia |
|---|---|
| Clearing Houses | 2 registros observados por API directa. |
| Financial Institutions | Registros seed presentes; instituciones sinteticas UAT creadas con IDs `92` y `93`. |
| ACH Cycles | Ciclo operativo usado `bd379e941269bb868bc2fb391b2fcc9d0feac357`, `Ciclo 1`, estado `Open`. |
| Company Entry Descriptions | 38 registros observados. |
| Cause Codes | 56 return reasons, 20 return codes, 11 file rejection codes. |
| ROR | 4 return-of-return policies observadas. |
| NACHA-M | 6 record definitions observadas; layouts endpoint pendiente/defectuoso. |
| CENIT | Queues 0; traceability 1 posterior a transaccion sintetica. |

## Evidencia DB Sanitizada

| Consulta | Resultado |
|---|---|
| Transaccion sintetica | ID `1`, referencia `UAT-SINT-001`, external ID `UAT-SINT-001`, monto `1000`, estado `Pending`, source institution `92`, destination institution `93`. |
| Timestamps | `CreatedAt` presente, `StateChangedAtUtc` presente. |
| Eventos de estado | `state_event_count = 0`. |
| Cliente/cuentas sinteticas | Conteo de cliente sintetico `999999999`: 2; cuentas sinteticas `0000000001`/`0000000002`: 2. |
| Auditoria | `audit_rows = 1` para transaccion sintetica. |

## Evidencia De Logs Sanitizada

| Fuente | Observacion |
|---|---|
| API | Sin coincidencias criticas revisadas para `fail`, `exception`, `500`, `password`, `token` en muestra de 900 lineas. |
| SPA/Nginx | Registros 200 con tamano `2123` para `/financial-institutions`, `/clearing-houses`, `/ach-cycles`, `/transactions/company-entry-descriptions`, consistente con fallback SPA indebidamente aplicado a rutas API funcionales. |
| PostgreSQL | Servicio healthy; se mantienen FATAL previos por usuarios inexistentes `root`/`sa` como observacion no bloqueante del UAT funcional API. |

## Conclusiones De Evidencia

La evidencia permite sostener que el core API funcional sintetico creo, persistio y rechazo duplicado de forma controlada para una transaccion sintetica. Tras el ajuste de `web/ach-interbank-ui/nginx.conf`, las rutas funcionales raiz requeridas por pantallas transaccionales ya responden desde API por `http://localhost:743` y no devuelven `index.html`.

UAT funcional sintetico: **PARCIALMENTE OK** por observaciones restantes de trazabilidad/evento inicial, contrato de idempotencia, evidencia visual y actas formales.  
Productivo: **NO-GO**.
