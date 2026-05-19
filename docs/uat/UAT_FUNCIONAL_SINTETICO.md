# UAT Funcional Sintetico - ACH Interbank

Fecha de ejecucion/revalidacion: 2026-05-18 / 2026-05-19 America/Bogota
Version: 0.7 estabilizacion final UAT/readiness
Rama ejecutada: `fix/spa-functional-root-routes-proxy`
Commit base: `49b810f9`
Ambiente: Docker Compose local, SPA `http://localhost:743`, API directa `http://localhost:843`  
Clasificacion: no incluir password, token completo, datos reales, cuentas reales, certificados reales ni archivos externos productivos.

## Alcance

Ejecutar UAT funcional sintetico controlado sobre ACH Interbank usando unicamente datos sinteticos/anonimizados, validando datos maestros, creacion de una transaccion sintetica, persistencia, trazabilidad, auditoria, conciliacion basica e idempotencia.

Usuario demo: `admin`  
Password: no documentada; tomada desde variable de entorno.  
Token: recibido y no documentado completo; evidencia enmascarada `eyJ...Iso`.  
Roles esperados: `Admin`, `ACH.Operator`.  
Roles observados: `Admin`; `ACH.Operator` no visible en respuesta/JWT. Diagnostico 2026-05-19: el rol existe en seed (`RoleConfiguration`), pero `UserRoleConfiguration` asigna al usuario demo solo `Admin`; el token autoriza endpoints por permisos `CanManageAch`/`CanReadAch` derivados de `Admin`.

## Resultado Ejecutivo

| Control | Resultado | Evidencia |
|---|---|---|
| Docker runtime | OK | `postgres` healthy, API Up, SPA Up. |
| Health live/ready via SPA | OK | `GET /health/live` y `GET /health/ready` por `:743` devuelven 200 JSON. |
| Login real demo | OK | `POST /auth/login` por `:743` devuelve 200 JSON y token usable. |
| Menu autenticado | OK | `GET /navigation/menu` por `:743` devuelve 200 JSON con Bearer. |
| Endpoints protegidos tecnicos | OK | `/api/roles`, `/api/users`, `/api/ach/responses` devuelven 200 JSON con Bearer. |
| Rutas funcionales SPA Docker | OK tecnico | Reintento tras ajuste Nginx: sin token responden 401 no HTML; con token responden 200 JSON para `/financial-institutions`, `/ach-cycles`, `/clearing-houses`, `/transactions/company-entry-descriptions`. |
| Datos maestros API directa | OK con observaciones | API directa `:843` expone datos maestros suficientes; ACH cycles se resuelve/genera on-demand. |
| Transaccion sintetica | OK API directa y reintento proxy | `POST /transactions` por `:843` creo transaccion ID `1`; reintento por `:743` con mismo payload devuelve 400 JSON controlado por duplicado. |
| Persistencia DB | OK | PostgreSQL contiene la transaccion sintetica con timestamps y referencias sinteticas. |
| Evento inicial | OK REVALIDADO | `UAT-SINT-TRACE-001` creo transaccion ID `2` y evento inicial `Pending -> Pending`, `Source=System`, `ReasonCode=CREATED`. `UAT-SINT-001` historica conserva `state_event_count=0` sin backfill. |
| Trazabilidad API | OK REVALIDADO | `GET /api/ach-traceability/transactions/2` responde 200 JSON con 1 evento inicial. |
| Conciliacion basica | OK API directa | `GET /api/reports/reconciliation` responde 200 para ciclo/fecha sinteticos. |
| Idempotencia | OK DOCUMENTADO / DECISION EVOLUTIVA | Reintento del mismo payload devuelve 400 con mensaje de duplicado controlado; contrato actual observado queda formalizado en `docs/go-live-readiness/CONTRATO_IDEMPOTENCIA_TRANSACCIONES.md`. |
| Logs | OK con observaciones | Sin patrones criticos de 500/secreto en muestra API; SPA levanta estable tras ajuste Nginx. |
| UAT funcional sintetico | PARCIALMENTE OK | Core API funcional, reintento HTTP desde SPA Docker, DEF-UAT-017 y contrato documental DEF-UAT-018 pasan; quedan evidencia visual, actas y UAT bancario. |
| Productivo | NO-GO | No hay UAT bancario formal, actas firmadas, validacion externa ni cierre de brechas criticas. |

## Datos Sinteticos Usados

| Dato | Valor |
|---|---|
| Cliente | `Cliente UAT Sintetico` como dato de prueba; payload operativo uso `CLIENTE UAT` por limite de 16 caracteres en `companyName`. |
| Documento | `999999999` |
| Cuenta origen | `0000000001` |
| Cuenta destino | `0000000002` |
| Banco origen | `Banco UAT Origen` |
| Banco destino | `Banco UAT Destino` |
| Monto | `1000` |
| Referencia | `UAT-SINT-001` |
| TransactionExternalId | `UAT-SINT-001` |
| Tipo | `Credit` (`1`) |
| Cuenta | `Checking` (`1`) |

No se usaron datos reales, cuentas reales, bancos productivos reales, certificados reales, archivos NACHA-M productivos ni conexiones externas ACH/CENIT.

## Pre-check Funcional

| Item | Resultado |
|---|---|
| `docker compose ps` | OK: `achinterbank-postgres` healthy, `achinterbank-api` Up, `achinterbank-spa` Up. |
| `GET http://localhost:743/health/live` | OK: HTTP 200 JSON. |
| `GET http://localhost:743/health/ready` | OK: HTTP 200 JSON. |
| Login demo | OK: HTTP 200 JSON; password no impresa. |
| Token | OK: recibido y usado; token completo no documentado. |
| Menu autenticado | OK: HTTP 200 JSON. |
| Endpoints protegidos | OK: `/navigation/menu`, `/api/roles`, `/api/users`, `/api/ach/responses` con Bearer. |

## Datos Maestros Validados

| Dominio | Resultado | Observacion |
|---|---|---|
| Clearing Houses | OK | Endpoint directo `:843/clearing-houses` responde JSON; 2 registros observados. |
| Financial Institutions | OK | Endpoint directo responde JSON; existian instituciones seed y se crearon 2 instituciones sinteticas UAT (`92`, `93`) para el caso controlado. |
| ACH Cycles | OK con observacion | Endpoint directo inicialmente sin ciclos abiertos; el backend resolvio/genero on-demand `Ciclo 1` para la transaccion. |
| Cause Codes | OK | `return-reasons` 56, `return-codes` 20, `file-rejection-codes` 11. |
| Estados transaccionales | PARCIAL | Estado inicial persistido como `Pending`; evento inicial corregido tecnicamente para nuevas transacciones, sin backfill de `UAT-SINT-001`. |
| Event types | PARCIAL | No se valido catalogo dedicado; el evento inicial implementado usa fuente `System` y `ReasonCode=CREATED`. |
| Configuracion ROR | OK lectura | `return-of-return-policies` responde 200, 4 politicas observadas. |
| Configuracion NACHA-M | OK tecnico / PARCIAL normativo | `nacha-record-definitions` y `nacha-layouts` responden 200 con 6 registros por API directa y proxy SPA Docker. La ruta `nacha-record-layouts` no es endpoint real. Falta validacion normativa campo-a-campo y homologacion externa. |
| Configuracion CENIT | OK lectura | `cenit/queues` y `cenit/traceability` responden 200; cola sin registros y trazabilidad con 1 registro posterior a la transaccion. |
| Conciliacion | OK lectura | `api/reports/reconciliation` responde 200 para el ciclo/fecha sinteticos. |

## Creacion De Transaccion Sintetica

Endpoint real: `POST /transactions`  
Primera ejecucion: API directa `http://localhost:843/transactions`.  
Reintento proxy funcional: `http://localhost:743/transactions` con mismo payload, sin crear datos nuevos; devuelve JSON controlado por duplicado.

Payload sanitizado:

```json
{
  "amount": 1000,
  "transactionExternalId": "UAT-SINT-001",
  "reference": "UAT-SINT-001",
  "type": 1,
  "accountType": 1,
  "isPrenotification": false,
  "destinationInstitutionId": 93,
  "sourceAccountNumber": "0000000001",
  "destinationAccountNumber": "0000000002",
  "recipientIdNumber": "999999999",
  "recipientName": "CLIENTE UAT",
  "requiresIdentityValidation": false,
  "companyName": "CLIENTE UAT",
  "companyIdentification": "999999999",
  "companyEntryDescriptionId": 1,
  "sourcePersonType": "PJ",
  "recipientPersonType": "PN",
  "addendas": []
}
```

Resultado:

| Validacion | Resultado |
|---|---|
| Preview | `CanSubmit=True`, duplicado `False`. |
| Creacion | HTTP 201. |
| ID transaccion | `1`. |
| Estado inicial | `Pending` (`1`). |
| Ciclo usado | `bd379e941269bb868bc2fb391b2fcc9d0feac357`, `Ciclo 1`. |
| Banco origen sintetico | `92`, `Banco UAT Origen`. |
| Banco destino sintetico | `93`, `Banco UAT Destino`. |
| Trace | Presente, formato sintetico `999990010000001`. |
| Timestamps | `CreatedAt` y `StateChangedAtUtc` presentes en DB. |
| Auditoria | 1 registro de auditoria relacionado con transaccion observado. |
| Evento inicial | No generado para la transaccion historica `UAT-SINT-001`; corregido tecnicamente para nuevas transacciones sin migracion/backfill. |

Nota operativa: se creo un ciclo auxiliar `UAT-SINT-CICLO`, pero no fue usado por la transaccion porque quedo cerrado por ventana horaria. El backend resolvio el ciclo operativo abierto `Ciclo 1`. La fuente default se restauro al valor previo despues de crear la transaccion sintetica.

## Idempotencia

Se reintento la misma operacion con la misma referencia y el mismo payload.

| Control | Resultado |
|---|---|
| HTTP status | 400 Bad Request. |
| Mensaje | Duplicado equivalente para el mismo ciclo. |
| Comportamiento | Rechazo controlado/deduplicacion funcional. |
| Contrato observado | La deduplicacion actual se evalua antes de persistir usando ciclo, tipo, monto, cuentas origen/destino y `TransactionExternalId` como identificador operativo, con fallback a `Reference` cuando no existe external id. |
| Documento formal | `docs/go-live-readiness/CONTRATO_IDEMPOTENCIA_TRANSACCIONES.md`. |
| Estado documental | Cerrado para contrato actual observado: `400 Bad Request` JSON controlado, sin persistencia duplicada y sin evento inicial adicional. |
| Observacion evolutiva | No existe contrato por header `Idempotency-Key`, hash de payload ni replay; decidir en arquitectura si se conserva 400 o se migra a 409 Conflict/replay idempotente. |

## Diagnostico DEF-UAT-017 / DEF-UAT-018

| Defecto | Diagnostico | Accion aplicada | Estado |
|---|---|---|---|
| DEF-UAT-017 | La creacion persistia `AchTransaction.State=Pending` y `StateChangedAtUtc`, pero no agregaba fila en `AchTransactionStateEvents`; los eventos se creaban solo en transiciones posteriores. | Se agrego evento inicial en `TransactionPersister.PersistAsync`: `Pending -> Pending`, `Source=System`, `ReasonCode=CREATED`, payload tecnico sanitizado. Se agregaron pruebas y se revalido en runtime con `UAT-SINT-TRACE-001`. | Cerrado funcionalmente para nuevas transacciones; sin backfill historico. |
| DEF-UAT-018 | La idempotencia real es deduplicacion funcional previa a persistencia, no replay exacto ni contrato por header. El 400 actual es controlado por politica. | No se cambio el comportamiento HTTP; se formalizo el contrato actual observado, se agregaron pruebas de caracterizacion y se documentaron decisiones evolutivas. | Cerrado documentalmente para contrato actual; abierto solo como decision evolutiva si se requiere 409/Idempotency-Key/replay. |

## Estabilizacion Final 2026-05-19

| Control | Resultado |
|---|---|
| `dotnet build ACHInterbank.sln -c Release` | OK, 0 errores. |
| `dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release` | OK, 1091 pruebas aprobadas, 1 omitida, 0 fallas. |
| BatchResolver preexistente | Cerrado: falla era fixture no deterministico por timezone; se ajusto solo el test para usar ciclo cerrado respecto al `FixedTimeProvider`. |
| `npm run build` | OK. |
| `npm test -- --watch=false --browsers=ChromeHeadless` | OK, 147 specs. |
| NU1903 | Corregido: `System.Security.Cryptography.Xml` queda en `10.0.8`; `dotnet list ... --vulnerable` no reporta vulnerabilidades. |
| NACHA layouts por `:743` | OK: sin token devuelve 401 controlado; con token devuelve JSON para `/nacha-layouts`, `/nacha-record-definitions` y `/nacha-config/catalogos-filtro`. |
| `ACH.Operator` | Abierto por seed/seguridad: rol existe, pero `admin` no tiene relacion seed con ese rol. No se modifico auth ni BD. |

## Revalidacion Runtime DEF-UAT-017

Fecha: 2026-05-19 America/Bogota.

Se reconstruyo y reinicio solo `achinterbank-api` para asegurar que el runtime Docker ejecutara la correccion de `TransactionPersister`. No se borraron volumenes ni se ejecutaron migraciones manuales.

| Control | Resultado |
|---|---|
| Referencia usada | `UAT-SINT-TRACE-001` |
| HTTP creacion | `POST http://localhost:743/transactions` -> 201 JSON |
| TransactionId | `2` |
| Estado inicial | `Pending` (`1`) |
| Banco origen sintetico | `92`, `Banco UAT Origen`; usado como default temporal y restaurado al finalizar |
| Banco destino sintetico | `93`, `Banco UAT Destino` |
| Evento inicial DB | 1 evento en `AchTransactionStateEvents` |
| FromState / ToState | `Pending` -> `Pending` |
| Source | `System` |
| ReasonCode | `CREATED` |
| PayloadJson | Presente; incluye `eventType=TransactionCreated` y referencia sintetica |
| Trazabilidad API | `GET /api/ach-traceability/transactions/2` -> 200 JSON con 1 evento |
| Idempotencia | Reintento identico -> 400 JSON controlado: `Ya existe una transaccion equivalente para el mismo ciclo.` |
| No duplicacion | `transaction_count=1`, `event_count=1` para `UAT-SINT-TRACE-001` |
| Restore default source | OK; default original ID `34` restaurado y `92` queda no default |

DEF-UAT-017 queda cerrado funcionalmente para nuevas transacciones. No se hizo backfill de transacciones historicas.

## Reintento Proxy Funcional SPA Docker

Cambio aplicado: `web/ach-interbank-ui/nginx.conf`, agregando locations explicitos antes del fallback Angular.

Validacion sin token por `http://localhost:743`:

| Ruta | Resultado |
|---|---|
| `/financial-institutions` | 401 desde API, no HTML. |
| `/ach-cycles` | 401 desde API, no HTML. |
| `/clearing-houses` | 401 desde API, no HTML. |
| `/transactions/company-entry-descriptions` | 401 desde API, no HTML. |

Validacion con token demo por `http://localhost:743`:

| Ruta | Resultado |
|---|---|
| `/financial-institutions` | 200 JSON, no `index.html`. |
| `/ach-cycles` | 200 JSON, no `index.html`. |
| `/clearing-houses` | 200 JSON, no `index.html`. |
| `/transactions/company-entry-descriptions` | 200 JSON, no `index.html`. |
| `/transactions` | 200 JSON, no `index.html`. |
| `/transactions/1` | 200 JSON, no `index.html`. |
| `/transactions/policies/preview` | 200 JSON, no `index.html`. |
| `POST /transactions` con payload duplicado | 400 JSON controlado, no `index.html`. |

## Trazabilidad Y Auditoria

| Fuente | Resultado |
|---|---|
| API `GET /transactions/1` | HTTP 200; referencia, monto, estado y timestamps correctos. |
| API `GET /api/ach-traceability/transactions/1` | HTTP 200; origen/destino sinteticos visibles; eventos `0` para `UAT-SINT-001` historica. |
| API `GET /api/ach-traceability/transactions/2` | HTTP 200; evento inicial presente para `UAT-SINT-TRACE-001`. |
| PostgreSQL `AchTransactions` | Fila persistida con referencia `UAT-SINT-001`, monto `1000`, estado `Pending`. |
| PostgreSQL `AchTransactions` revalidacion | Fila persistida con referencia `UAT-SINT-TRACE-001`, ID `2`, estado `Pending`, source institution `92`, destination institution `93`. |
| PostgreSQL `AchTransactionStateEvents` | `0` eventos para la transaccion sintetica historica; `UAT-SINT-TRACE-001` tiene 1 evento inicial `Pending -> Pending`, `System`, `CREATED`. |
| Auditoria DB | 1 registro relacionado con transaccion observado. |

## Conciliacion Basica

| Endpoint | Resultado | Observacion |
|---|---|---|
| `GET /api/reports/reconciliation` | HTTP 200 | Responde para fecha/ciclo sinteticos; no ejecuta conciliacion bancaria real. |
| `GET /api/reports/history` | HTTP 200 | Responde para transaccion sintetica; revisar consistencia con ausencia de evento inicial. |

## Devoluciones / ROR / CENIT

| Dominio | Resultado |
|---|---|
| ROR policies | HTTP 200, lectura controlada. |
| Return reasons/codes | HTTP 200, catalogos presentes. |
| CENIT queues | HTTP 200, sin cola operativa real. |
| CENIT traceability | HTTP 200, 1 registro posterior a la transaccion sintetica. |

No se generaron devoluciones reales, archivos reales ni conexiones externas.

## Defectos Derivados

| ID | Severidad | Resumen |
|---|---|---|
| DEF-UAT-016 | Cerrado | Rutas funcionales raiz fueron proxied por Nginx y revalidadas por `:743` sin devolver `index.html`. |
| DEF-UAT-017 | Alta/Media | Cerrado funcionalmente para nuevas transacciones: `UAT-SINT-TRACE-001` genero evento inicial y el duplicado no creo evento adicional. |
| DEF-UAT-018 | Media | Cerrado documentalmente: contrato actual observado formalizado como deduplicacion funcional previa a persistencia con HTTP 400 JSON; decisiones 409/idempotency key/replay quedan evolutivas. |
| DEF-UAT-019 | Media/Baja | Cerrado tecnicamente: endpoint real `/nacha-layouts` responde JSON; `/nacha-record-layouts` era ruta esperada incorrecta. |
| DEF-UAT-020 | Alta/Media | Abierto: validacion NACHA-M campo-a-campo y homologacion externa de registros 1/5/6/7/8/9 pendiente. |

## Clasificacion Final

UAT tecnico autenticado basico: **OK con observaciones**.  
UAT funcional sintetico: **PARCIALMENTE OK** por API directa, reintento HTTP desde SPA Docker, cierre funcional de DEF-UAT-017 y cierre documental de DEF-UAT-018; siguen pendientes evidencia visual/acta formal y UAT bancario.
Productivo: **NO-GO**.
