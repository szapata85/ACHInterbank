# UAT Funcional Sintetico - ACH Interbank

Fecha de ejecucion: 2026-05-18 America/Bogota  
Version: 0.1 ejecucion controlada  
Rama ejecutada: `fix/spa-docker-runtime-proxy-and-images`  
Commit: `261b1e0537e5d941f4d5f39c28bc4dc06d24f805`  
Ambiente: Docker Compose local, SPA `http://localhost:743`, API directa `http://localhost:843`  
Clasificacion: no incluir password, token completo, datos reales, cuentas reales, certificados reales ni archivos externos productivos.

## Alcance

Ejecutar UAT funcional sintetico controlado sobre ACH Interbank usando unicamente datos sinteticos/anonimizados, validando datos maestros, creacion de una transaccion sintetica, persistencia, trazabilidad, auditoria, conciliacion basica e idempotencia.

Usuario demo: `admin`  
Password: no documentada; tomada desde variable de entorno.  
Token: recibido y no documentado completo; evidencia enmascarada `eyJ...Iso`.  
Roles esperados: `Admin`, `ACH.Operator`.  
Roles observados: `Admin`; `ACH.Operator` no visible en respuesta/JWT, pero el token autorizo endpoints protegidos del alcance.

## Resultado Ejecutivo

| Control | Resultado | Evidencia |
|---|---|---|
| Docker runtime | OK | `postgres` healthy, API Up, SPA Up. |
| Health live/ready via SPA | OK | `GET /health/live` y `GET /health/ready` por `:743` devuelven 200 JSON. |
| Login real demo | OK | `POST /auth/login` por `:743` devuelve 200 JSON y token usable. |
| Menu autenticado | OK | `GET /navigation/menu` por `:743` devuelve 200 JSON con Bearer. |
| Endpoints protegidos tecnicos | OK | `/api/roles`, `/api/users`, `/api/ach/responses` devuelven 200 JSON con Bearer. |
| Rutas funcionales SPA Docker | FALLA | Varias rutas raiz funcionales devuelven `index.html` por `:743`: `/financial-institutions`, `/ach-cycles`, `/clearing-houses`, `/transactions/company-entry-descriptions`. |
| Datos maestros API directa | OK con observaciones | API directa `:843` expone datos maestros suficientes; ACH cycles se resuelve/genera on-demand. |
| Transaccion sintetica | OK API directa | `POST /transactions` por `:843` creo transaccion ID `1`, referencia `UAT-SINT-001`, estado `Pending`. |
| Persistencia DB | OK | PostgreSQL contiene la transaccion sintetica con timestamps y referencias sinteticas. |
| Evento inicial | FALLA/OBSERVACION | No se genero evento inicial en `AchTransactionStateEvents`; `state_event_count=0`. |
| Trazabilidad API | PARCIAL | `GET /api/ach-traceability/transactions/1` responde 200, pero sin eventos iniciales. |
| Conciliacion basica | OK API directa | `GET /api/reports/reconciliation` responde 200 para ciclo/fecha sinteticos. |
| Idempotencia | OK controlado con observacion | Reintento del mismo payload devuelve 400 con mensaje de duplicado controlado. |
| Logs | OK con observaciones | Sin patrones criticos de 500/secreto en muestra API; logs SPA evidencian defecto de proxy funcional. |
| UAT funcional sintetico | PARCIALMENTE OK | Core API funcional sintetico pasa; E2E desde SPA Docker queda bloqueado por proxy funcional. |
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
| Estados transaccionales | PARCIAL | Estado inicial persistido como `Pending`; no se observo evento inicial asociado. |
| Event types | PARCIAL | No se valido catalogo dedicado; trazabilidad no mostro evento inicial. |
| Configuracion ROR | OK lectura | `return-of-return-policies` responde 200, 4 politicas observadas. |
| Configuracion NACHA-M | PARCIAL | `nacha-record-definitions` responde 200 con 6 definiciones; `nacha-record-layouts` no respondio como endpoint esperado. |
| Configuracion CENIT | OK lectura | `cenit/queues` y `cenit/traceability` responden 200; cola sin registros y trazabilidad con 1 registro posterior a la transaccion. |
| Conciliacion | OK lectura | `api/reports/reconciliation` responde 200 para el ciclo/fecha sinteticos. |

## Creacion De Transaccion Sintetica

Endpoint real: `POST http://localhost:843/transactions`  
Motivo de uso API directa: la SPA Docker por `:743` no proxya rutas raiz funcionales y devuelve `index.html` para varias rutas necesarias.

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
| Evento inicial | No generado en tabla de eventos de estado. |

Nota operativa: se creo un ciclo auxiliar `UAT-SINT-CICLO`, pero no fue usado por la transaccion porque quedo cerrado por ventana horaria. El backend resolvio el ciclo operativo abierto `Ciclo 1`. La fuente default se restauro al valor previo despues de crear la transaccion sintetica.

## Idempotencia

Se reintento la misma operacion con la misma referencia y el mismo payload.

| Control | Resultado |
|---|---|
| HTTP status | 400 Bad Request. |
| Mensaje | Duplicado equivalente para el mismo ciclo. |
| Comportamiento | Rechazo controlado/deduplicacion funcional. |
| Observacion | El contrato deberia definir si corresponde 409 Conflict o idempotency key formal. |

## Trazabilidad Y Auditoria

| Fuente | Resultado |
|---|---|
| API `GET /transactions/1` | HTTP 200; referencia, monto, estado y timestamps correctos. |
| API `GET /api/ach-traceability/transactions/1` | HTTP 200; origen/destino sinteticos visibles; eventos `0`. |
| PostgreSQL `AchTransactions` | Fila persistida con referencia `UAT-SINT-001`, monto `1000`, estado `Pending`. |
| PostgreSQL `AchTransactionStateEvents` | `0` eventos para la transaccion sintetica. |
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
| DEF-UAT-016 | Bloqueante para UAT funcional SPA | Rutas funcionales raiz no proxied por SPA Docker devuelven `index.html`. |
| DEF-UAT-017 | Alta/Media | Creacion de transaccion no genera evento inicial de estado. |
| DEF-UAT-018 | Media | Idempotencia controlada con HTTP 400; falta contrato explicito 409/idempotency key. |
| DEF-UAT-019 | Media/Baja | Endpoint esperado de layouts NACHA-M no responde como catalogo disponible. |

## Clasificacion Final

UAT tecnico autenticado basico: **OK con observaciones**.  
UAT funcional sintetico: **PARCIALMENTE OK** por API directa; **bloqueado para cierre E2E desde SPA Docker** por defecto de proxy funcional.  
Productivo: **NO-GO**.
