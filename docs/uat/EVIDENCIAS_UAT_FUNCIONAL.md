# Evidencias UAT Funcional Sintetico - ACH Interbank

Fecha de generacion/revalidacion: 2026-05-18 / 2026-05-19 America/Bogota
Version: 0.8 cierre rol ACH.Operator
Rama ejecutada: `fix/uat-operator-role-seed`
Commit base: `efac77e4`
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
| EV-FUNC-013 | Evento inicial historico | PostgreSQL/API | `AchTransactionStateEvents` devuelve `0` eventos para la transaccion sintetica historica `UAT-SINT-001`; no se ejecuto backfill. | `docker exec` + trazabilidad API. | Observacion historica documentada |
| EV-FUNC-014 | Idempotencia | HTTP/API | Reintento del mismo payload devuelve HTTP 400 con rechazo controlado por duplicado. | API directa. | OK con observacion |
| EV-FUNC-015 | Trazabilidad historica | HTTP/API | `GET /api/ach-traceability/transactions/1` HTTP 200; origen/destino sinteticos; eventos `0` para registro historico. | API directa + tests backend. | Observacion historica |
| EV-FUNC-016 | Conciliacion | HTTP/API | `GET /api/reports/reconciliation` HTTP 200 para ciclo/fecha sinteticos. | API directa. | OK |
| EV-FUNC-017 | ROR/CENIT lectura | HTTP/API | Politicas ROR, causas, colas CENIT y trazabilidad CENIT responden 200. | API directa. | OK |
| EV-FUNC-018 | Logs API | Logs | Muestra revisada sin errores 500 criticos ni tokens/passwords completos. | `docker compose logs achinterbank-api --tail=900`. | OK |
| EV-FUNC-019 | Logs SPA | Logs | Evidencia historica: Nginx registraba rutas funcionales raiz con 200 y tamano `2123`, consistente con `index.html`. | `docker compose logs achinterbank-spa --tail=260`. | Cerrado por reintento |
| EV-FUNC-020 | Logs PostgreSQL | Logs | PostgreSQL sigue healthy; FATAL previos por usuarios `root`/`sa` se mantienen como observacion operativa. | `docker compose logs postgres --tail=120`. | OK con observacion |
| EV-FUNC-021 | Build/runtime SPA | Docker | `docker compose config --quiet`, `docker compose build achinterbank-spa` y `docker compose up -d` ejecutados; SPA queda Up. | Consola Codex, salida sanitizada. | OK |
| EV-FUNC-022 | Proxy funcional sin token | HTTP | `/financial-institutions`, `/ach-cycles`, `/clearing-houses`, `/transactions/company-entry-descriptions` devuelven 401 desde API, no HTML. | `curl.exe` por `http://localhost:743`. | OK |
| EV-FUNC-023 | Proxy funcional con token | HTTP | Las 4 rutas funcionales devuelven 200 JSON con Bearer demo, sin `index.html`. | Token no documentado completo. | OK |
| EV-FUNC-024 | Transacciones por proxy SPA | HTTP | `/transactions`, `/transactions/1`, `/transactions/policies/preview` devuelven 200 JSON; `POST /transactions` duplicado devuelve 400 JSON controlado. | `http://localhost:743`, salida sanitizada. | OK con observacion |
| EV-FUNC-025 | Diagnostico DEF-UAT-017/018 | Codigo | Se confirmo que el evento inicial faltaba en `TransactionPersister.PersistAsync` y que la idempotencia observada vive en `TransactionPolicyService` sin header `Idempotency-Key`. | Revision de entidades, servicios, constraints y tests. | OK |
| EV-FUNC-026 | Correccion evento inicial | Codigo/Test | Nuevas transacciones agregan evento `Pending -> Pending`, `Source=System`, `ReasonCode=CREATED`; duplicado rechazado no crea segundo evento. | `TransactionPersister.cs`, `AchTransactionNachaTests.cs`. | OK |
| EV-FUNC-027 | Build Release | Build | `dotnet build ACHInterbank.sln -c Release` finaliza con 0 errores y 0 warnings. | Consola Codex. | OK |
| EV-FUNC-028 | Suite backend completa | Test | `dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release` ejecuta 1090 pruebas: 1088 OK, 1 omitida, 1 falla existente DEF-UAT-011. | Consola Codex. | OK parcial por defecto preexistente |
| EV-FUNC-029 | Runtime API corregido | Docker | `docker compose build achinterbank-api` y `docker compose up -d achinterbank-api` ejecutados sin borrar volumenes; API queda Up. | Consola Codex. | OK; NU1903 corregida posteriormente en EV-FUNC-037 |
| EV-FUNC-030 | Revalidacion DEF-UAT-017 | HTTP/API | `POST http://localhost:743/transactions` crea `UAT-SINT-TRACE-001`, transaction ID `2`, estado `Pending`. | Token no documentado completo; datos sinteticos. | OK |
| EV-FUNC-031 | Evento inicial revalidado | PostgreSQL/API | `UAT-SINT-TRACE-001` tiene 1 evento `Pending -> Pending`, `Source=System`, `ReasonCode=CREATED`, `PayloadJson` presente. | `docker exec` + `GET /api/ach-traceability/transactions/2`. | OK |
| EV-FUNC-032 | Idempotencia no duplica evento | HTTP/PostgreSQL | Reintento identico devuelve 400 JSON controlado; `transaction_count=1`, `event_count=1`. | `POST /transactions` y consulta read-only. | OK |
| EV-FUNC-033 | Build/test focal revalidado | Build/Test | `dotnet build ACHInterbank.sln -c Release` OK; `AchTransactionNachaTests` 17/17 OK. | Consola Codex. | OK |
| EV-FUNC-034 | Contrato idempotencia DEF-UAT-018 | Documentacion/Test | Contrato actual observado formalizado: deduplicacion previa a persistencia por ciclo/tipo/monto/cuentas/`TransactionExternalId` o `Reference`, respuesta 400 JSON. | `docs/go-live-readiness/CONTRATO_IDEMPOTENCIA_TRANSACCIONES.md`, `TransactionPolicyServiceTests`, `TransactionsControllerTests`. | Cerrado documentalmente |
| EV-FUNC-035 | CI backend local | Build/Test | `dotnet build` OK y suite backend completa OK tras cierre de BatchResolver fixture/timezone. | Consola Codex; 1091 OK, 1 omitida, 0 fallas. | OK |
| EV-FUNC-036 | CI Angular local | Build/Test | `npm run build` OK y `npm test -- --watch=false --browsers=ChromeHeadless` OK. | Consola Codex; 147 specs OK. | OK |
| EV-FUNC-037 | Seguridad dependencias | NuGet | `System.Security.Cryptography.Xml` transitiva por `System.ServiceModel.*` se fijo en `10.0.8`; `dotnet list ... --vulnerable` queda sin vulnerabilidades. | `Cfa.ACHInterbank.Application.csproj`. | OK |
| EV-FUNC-038 | NACHA layouts proxy | HTTP/Docker | `docker compose build achinterbank-spa` y `up -d`; `/nacha-layouts`, `/nacha-record-definitions` y `/nacha-config/catalogos-filtro` por `:743` devuelven 401 sin token y JSON con Bearer. | Token no documentado completo. | OK tecnico |
| EV-FUNC-039 | Rol `ACH.Operator` | Auth/Seed | `admin` queda asignado a `Admin` y `ACH.Operator` por seed/migracion controlada; login y JWT sanitizados muestran ambos roles. | `UserRoleConfiguration`, migracion `AddAdminOperatorRoleSeed`, `UserRoleSeedTests`; runtime `:743` login 200, token enmascarado, roles `Admin,ACH.Operator`. | OK |

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
| `POST http://localhost:743/auth/login` tras cierre DEF-UAT-015 | 200, JSON, roles respuesta/JWT `Admin,ACH.Operator`; token enmascarado, password no impresa. |
| `GET http://localhost:743/navigation/menu` tras cierre DEF-UAT-015 | 200, JSON con Bearer. |
| `GET http://localhost:743/api/roles`, `/api/users`, `/api/ach/responses` tras cierre DEF-UAT-015 | 200, JSON con Bearer. |
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
| `GET http://localhost:843/api/ach-traceability/transactions/1` | 200, JSON; sin eventos iniciales para `UAT-SINT-001` historica. |
| `GET http://localhost:843/api/reports/reconciliation` | 200. |
| `dotnet test ... --filter "FullyQualifiedName~AchTransactionNachaTests"` | 17 pruebas OK, incluyendo evento inicial y duplicado sin evento repetido. |
| `POST http://localhost:743/transactions` con `UAT-SINT-TRACE-001` | 201, JSON, transaccion ID `2`, estado `Pending`. |
| `GET http://localhost:743/transactions/2` | 200, JSON, referencia `UAT-SINT-TRACE-001`, estado `Pending`. |
| `GET http://localhost:743/api/ach-traceability/transactions/2` | 200, JSON, 1 evento inicial `Pending -> Pending`, `Source=System`, `ReasonCode=CREATED`. |
| Reintento `POST http://localhost:743/transactions` con `UAT-SINT-TRACE-001` | 400, JSON, duplicado equivalente controlado. |
| `TransactionPolicyServiceTests.PreviewAsync_CurrentContractReturnsDuplicateMessageAndSyntheticKey` | Valida `WouldDuplicate=true`, mensaje controlado e `IdempotencyKey` informativo. |
| `TransactionsControllerTests.CreateTransaction_ReturnsBadRequestJson_WhenDuplicatePolicyRejects` | Valida que el controller conserva `HTTP 400` y cuerpo JSON con `message`. |
| `GET http://localhost:743/nacha-layouts` sin token | 401 controlado desde API, no HTML. |
| `GET http://localhost:743/nacha-record-definitions` sin token | 401 controlado desde API, no HTML. |
| `GET http://localhost:743/nacha-config/catalogos-filtro` sin token | 401 controlado desde API, no HTML. |
| `GET http://localhost:743/nacha-layouts` con Bearer | 200, `application/json`, 6 registros, no HTML. |
| `GET http://localhost:743/nacha-record-definitions` con Bearer | 200, `application/json`, 6 registros, no HTML. |
| `GET http://localhost:743/nacha-config/catalogos-filtro` con Bearer | 200, `application/json`, no HTML. |
| `dotnet list ACHInterbank.sln package --vulnerable --include-transitive` | Sin paquetes vulnerables reportados tras fijar `System.Security.Cryptography.Xml` 10.0.8. |
| `dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release` | 1091 OK, 1 omitida, 0 fallas. |
| `npm test -- --watch=false --browsers=ChromeHeadless` | 147 specs OK. |

## Evidencia De Datos Maestros

| Dominio | Evidencia |
|---|---|
| Clearing Houses | 2 registros observados por API directa. |
| Financial Institutions | Registros seed presentes; instituciones sinteticas UAT creadas con IDs `92` y `93`. |
| ACH Cycles | Ciclo operativo usado `bd379e941269bb868bc2fb391b2fcc9d0feac357`, `Ciclo 1`, estado `Open`. |
| Company Entry Descriptions | 38 registros observados. |
| Cause Codes | 56 return reasons, 20 return codes, 11 file rejection codes. |
| ROR | 4 return-of-return policies observadas. |
| NACHA-M | 6 record definitions y 6 layouts observados por API directa y proxy SPA Docker; validacion normativa campo-a-campo sigue parcial. |
| CENIT | Queues 0; traceability 1 posterior a transaccion sintetica. |

## Evidencia DB Sanitizada

| Consulta | Resultado |
|---|---|
| Transaccion sintetica | ID `1`, referencia `UAT-SINT-001`, external ID `UAT-SINT-001`, monto `1000`, estado `Pending`, source institution `92`, destination institution `93`. |
| Timestamps | `CreatedAt` presente, `StateChangedAtUtc` presente. |
| Eventos de estado | `state_event_count = 0` para `UAT-SINT-001` historica; no se aplico migracion ni backfill. |
| Transaccion revalidacion | ID `2`, referencia `UAT-SINT-TRACE-001`, external ID `UAT-SINT-TRACE-001`, monto `1000`, estado `Pending`, source institution `92`, destination institution `93`. |
| Evento inicial revalidacion | `event_count = 1`; `FromState=Pending`, `ToState=Pending`, `Source=System`, `ReasonCode=CREATED`, `CreatedAt` presente, `PayloadJson` presente. |
| Idempotencia revalidacion | Despues del reintento duplicado: `transaction_count = 1`, `event_count = 1`. |
| Cliente/cuentas sinteticas | Conteo de cliente sintetico `999999999`: 2; cuentas sinteticas `0000000001`/`0000000002`: 2. |
| Auditoria | `audit_rows = 1` para transaccion sintetica. |

## Evidencia De Logs Sanitizada

| Fuente | Observacion |
|---|---|
| API | Sin 500 criticos en la revalidacion; se observo warning esperado por regla de negocio de duplicado: `Ya existe una transaccion equivalente para el mismo ciclo.` |
| SPA/Nginx | Fallback historico de rutas funcionales cerrado. Revalidacion NACHA: rutas `/nacha-layouts`, `/nacha-record-definitions` y `/nacha-config/catalogos-filtro` ya no devuelven `index.html`. |
| PostgreSQL | Servicio healthy; se mantienen FATAL previos por usuarios inexistentes `root`/`sa` como observacion no bloqueante del UAT funcional API. |

## Conclusiones De Evidencia

La evidencia permite sostener que el core API funcional sintetico creo, persistio y rechazo duplicado de forma controlada para una transaccion sintetica. Tras los ajustes de `web/ach-interbank-ui/nginx.conf`, las rutas funcionales raiz y catalogos NACHA usados por Angular ya responden desde API por `http://localhost:743` y no devuelven `index.html`.

DEF-UAT-017 queda cerrado funcionalmente para nuevas transacciones: `UAT-SINT-TRACE-001` genero el evento inicial esperado y el reintento duplicado no genero transaccion ni evento adicional. No se hizo backfill de `UAT-SINT-001`. DEF-UAT-018 queda cerrado documentalmente para el contrato actual observado: duplicado equivalente retorna `HTTP 400` JSON controlado, sin segunda transaccion y sin segundo evento inicial. Las alternativas `409 Conflict`, replay idempotente o header `Idempotency-Key` quedan como decision evolutiva.

UAT funcional sintetico: **PARCIALMENTE OK** por evidencia visual y actas formales pendientes.
Productivo: **NO-GO**.
