# ACHInterbank — Plan de implementación del refactor Clean Architecture, SOLID y Resiliencia

**Programa:** `REFACTOR-CA-SOLID-RES-001`  
**Baseline analizado:** paquete `ACHInterbank-v124-DOTNET-10.0.12-SDK-10.0.401-SECURE-DEPS-2026-10-09`  
**Propósito:** ejecutar el refactor en ChatGPT modo Chat, fase por fase, con evidencia, pruebas y estado persistente en `MEMORY.md`.  
**Idioma del plan:** español.  
**Idioma de `MEMORY.md`:** inglés, conforme a `AGENTS.md`.

---

## 1. Objetivo

Refactorizar ACHInterbank para elevar el cumplimiento efectivo de:

- Clean Architecture;
- principios SOLID;
- patrones de diseño aplicados de forma consistente;
- resiliencia de integraciones externas;
- determinismo temporal y testabilidad;
- observabilidad y logging seguro;
- enforcement arquitectónico automático en CI;

sin modificar por conveniencia el comportamiento normativo, operativo o financiero ya aceptado para ACH Colombia y CENIT.

Este programa es un **refactor incremental**, no una reescritura.

---

## 2. Fuentes y precedencia de autoridad

Antes de cualquier cambio, ChatGPT debe aplicar esta precedencia:

1. `AGENTS.md`: reglas permanentes de desarrollo y seguridad.
2. `MEMORY.md`: decisiones durables, invariantes, cierres y estado aceptado.
3. Código, configuración y pruebas del commit/árbol actual.
4. Documentación canónica vigente bajo `docs/`.
5. Fuentes normativas aplicables a ACH Colombia/CENIT.
6. Este plan de refactor.

Si este plan contradice código actual respaldado por evidencia o una decisión cerrada de `MEMORY.md`, **no debe imponerse el plan automáticamente**. Debe registrarse el bloqueo y preservarse la evidencia.

### Documentos que deben leerse al iniciar el programa

- `AGENTS.md`
- `MEMORY.md`
- `README.md`
- `docs/CURRENT_PROJECT_STATUS.md`
- `docs/ACH_COLOMBIA_INTERNAL_CLOSURE.md`
- `docs/PENDING_WORK_CLASSIFICATION.md`
- `docs/architecture/result-pattern-solid-plan.md`
- `docs/architecture/payment-rail-operational-strategy-blueprint-2026-04-25.md`

### Nota sobre blueprints existentes

El blueprint `PaymentRailOperationalStrategy` es una evolución arquitectónica separada. **No debe activarse como alcance implícito de este refactor.** Este programa puede preparar fronteras para futura extensibilidad, pero no debe introducir un nuevo rail, migrar ClearingHouse a PaymentRail ni alterar contratos aceptados sin una fase/decisión explícita posterior.

---

## 3. Estado técnico de partida observado

El análisis estático del paquete v124 confirmó, entre otros puntos:

- `Cfa.ACHInterbank.Application.csproj` referencia ASP.NET abstractions/core, NLog y paquetes WCF.
- `GlobalExceptionMiddleware`, filtros HTTP y otros componentes web viven dentro de `Application`.
- `RsaKeyProvider` accede a `X509Store` desde `Application`.
- `LoggerManager` conoce NLog directamente desde `Application`.
- `NachaFileBuilder.cs`: aproximadamente **4.798 líneas**.
- `NachaParserService.cs`: aproximadamente **2.247 líneas**.
- `IncomingNachaCommandCenterService.cs`: aproximadamente **1.502 líneas**.
- `IncomingNachaPostProcessingOrchestrator.cs`: aproximadamente **1.179 líneas**.
- `AchReturnsService.cs`: aproximadamente **1.066 líneas**.
- `ReportsController.cs`: aproximadamente **1.028 líneas**.
- Existe `SoapArchitectureDiagnosticTests` con un test diagnóstico en `Skip`.
- Existen construcciones manuales de `HttpClient` fuera del `IHttpClientFactory`/pipeline registrado.
- Se observaron múltiples usos de `AppSettings.Settings`.
- Se observaron usos amplios de `DateTime.UtcNow` / `DateTimeOffset.UtcNow` en código de runtime.

Estas métricas son **baseline**, no criterios absolutos de diseño. El objetivo no es “cumplir un número”, sino reducir responsabilidades, acoplamiento y riesgo.

---

## 4. Invariantes que el refactor NO puede romper

### 4.1 Funcionales y regulatorias

1. ACH Colombia y CENIT se evalúan y evolucionan por separado.
2. ACH Colombia V35 continúa como autoridad normativa vigente hasta decisión posterior explícita.
3. No alterar causales, layouts, códigos, ciclos, ventanas, retornos, nombres de archivo, límites o semántica NACHA-M sin evidencia normativa.
4. Los PUBLISH snapshots inmutables siguen siendo autoridad de runtime para flujos ordinarios soportados.
5. No reintroducir layouts estáticos legacy como fallback para flujos ordinarios soportados.
6. No modificar golden files NACHA-M salvo cambio normativo probado y explícitamente solicitado.
7. La política aceptada R10/DEV14 permanece cerrada; no rederivarla durante este refactor.
8. CENIT Gateway/PO y homologación externa permanecen independientes del refactor interno.

### 4.2 Integridad transaccional

1. Mantener idempotencia, deduplicación, hash y trazabilidad.
2. Nunca reenviar una operación después de una respuesta exitosa o funcionalmente definitiva.
3. Un Return entrante debe correlacionar trace y monto original exactos antes de mutar la transacción original.
4. No introducir doble publicación/procesamiento en clúster.
5. No confundir publicación en la frontera MFT con aceptación por ACH Colombia.

### 4.3 Seguridad

1. Nunca introducir credenciales reales, secretos, certificados, datos personales ni cuentas completas en código, logs, tests o documentación.
2. No conectar a infraestructura productiva real.
3. No reducir controles de host key, cifrado, permisos o segregación.
4. Logging estructurado debe redactar datos sensibles.

### 4.4 Calidad

1. No debilitar assertions ni alterar expected results para hacer pasar una prueba.
2. No cerrar una fase con pruebas rojas.
3. Si el host no permite ejecutar build/tests, usar estado `IMPLEMENTED_UNVERIFIED`, nunca `VERIFIED`.
4. EF Code First continúa como autoridad de esquema; no crear migraciones si el refactor no cambia el modelo persistente.

---

## 5. Modelo de estados del programa

Cada fase y cada gap se maneja con uno de estos estados:

| Estado | Significado |
|---|---|
| `PLANNED` | Alcance definido, implementación no iniciada. |
| `IN_PROGRESS` | ChatGPT está modificando la fase actual. |
| `IMPLEMENTED_UNVERIFIED` | Código terminado, pero falta evidencia ejecutable requerida. |
| `VERIFIED` | Implementación y gates definidos para la fase pasaron en el árbol/commit actual. |
| `BLOCKED` | No se puede continuar sin decisión/evidencia externa o prerequisito. |
| `DEFERRED` | Fuera de alcance por decisión explícita. |
| `CLOSED` | Fase cerrada con evidencia suficiente y documentación/memory actualizadas. |

### Regla de transición

```text
PLANNED -> IN_PROGRESS -> IMPLEMENTED_UNVERIFIED -> VERIFIED -> CLOSED
                     \-> BLOCKED
PLANNED/IN_PROGRESS  \-> DEFERRED (solo decisión explícita)
```

`VERIFIED` exige evidencia de las pruebas de la fase. `CLOSED` exige además actualización de documentación y `MEMORY.md` cuando haya una decisión durable.

---

## 6. Protocolo de ejecución para cada sesión de ChatGPT

Cada chat debe trabajar **una sola fase o subfase**.

### 6.1 Inicio obligatorio

1. Leer `AGENTS.md`.
2. Leer el encabezado y secciones relevantes de `MEMORY.md`.
3. Leer este plan y ubicar la fase actual en el state memory.
4. Ejecutar:

```cmd
 git status --short
 git rev-parse HEAD
 python scripts/validate-solution-projects.py
```

5. Inspeccionar el código actual; no asumir que los paths o clases del baseline siguen iguales.
6. Confirmar dependencias/callers antes de mover una clase pública.

### 6.2 Durante la fase

- aplicar cambios pequeños y cohesivos;
- preservar comportamiento;
- agregar characterization/regression tests antes de cambios de alto riesgo;
- no avanzar de fase por iniciativa propia;
- no mezclar cleanup no relacionado;
- no introducir nueva arquitectura solo para “hacerlo más elegante” si no es necesaria para el criterio de aceptación.

### 6.3 Gates de salida

Orden recomendado:

```cmd
python scripts/validate-solution-projects.py
dotnet build ACHInterbank.sln -c Release
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --no-build
```

Para cambios SPA:

```cmd
cd web\ach-interbank-ui
npm ci
npm run build
npm test -- --watch=false --browsers=ChromeHeadless
```

Además se deben ejecutar primero pruebas focales del módulo modificado.

### 6.4 Cierre de sesión

ChatGPT debe devolver:

1. estado final de la fase;
2. archivos modificados;
3. decisiones de arquitectura tomadas;
4. pruebas ejecutadas y resultado exacto;
5. riesgos o bloqueos;
6. `git diff --stat`;
7. cambio propuesto/realizado a `MEMORY.md`;
8. siguiente fase permitida, sin ejecutarla.

---

# 7. Backlog maestro de hallazgos

## 7.1 P0 — Guardrails y fronteras críticas

| ID | Hallazgo | Objetivo |
|---|---|---|
| `ARCH-001` | Application referencia ASP.NET/NLog/WCF | limpiar frontera Application |
| `ARCH-002` | Middleware HTTP dentro de Application | mover presentación a API |
| `ARCH-003` | Filtros/Exception HTTP dentro de Application | separar concerns HTTP |
| `ARCH-004` | Connected Service WCF dentro de Application | mover proxy a External |
| `ARCH-005` | `RsaKeyProvider` usa X509Store desde Application | puerto de certificado + adapter External |
| `ARCH-006` | Logger concreto NLog en Application | abstracción/logging framework fuera |
| `ARCH-009` | Arquitectura no bloqueada por CI | architecture tests tipo ratchet |
| `SRP-001` | `NachaFileBuilder` god class | descomposición incremental |
| `SRP-002` | constructor con demasiadas colaboraciones | reducir responsabilidad real |
| `DIP-001` | fallbacks `?? new ConcreteDependency` | composición exclusiva por DI |
| `SRP-003` | `NachaParserService` monolítico | separar parse/validate/persist |
| `SRP-005` | post-processing orchestrator monolítico | pipeline de etapas |
| `RES-001` | cliente SOAP/HTTP crea `HttpClient` manual | pipeline resiliente administrado |
| `RES-002` | `HttpClientService` crea clients manuales | typed/named clients |
| `SEC-001` | riesgo de logging de payload/curl sensible | redacción y metadata segura |
| `GOV-001` | falta enforcement integral | pruebas bloqueantes |
| `GOV-002` | refactor NACHA de alto riesgo | golden/characterization primero |
| `GOV-003` | dual DB | mantener SQL Server/PostgreSQL gates |
| `GOV-004` | Quartz/MFT cluster-aware | mantener no duplicidad/recovery |

## 7.2 P1 — SOLID, modularidad y resiliencia transversal

| ID | Hallazgo | Objetivo |
|---|---|---|
| `ARCH-007` | Application depende de `IHttpContextAccessor` | `ICurrentUserContext` |
| `ARCH-008` | controllers crean implementaciones Persistence | constructor DI obligatorio |
| `ARCH-010` | `AppSettings.Settings` global | options/configuration ports |
| `OCP-001` | builder con ramas por cámara/flujo | policies/strategies componibles |
| `SRP-004` | parser parsea y persiste | CQS/pipeline |
| `SRP-006` | MFT service mezcla ejecución/admin/query/credentials | ISP/CQRS |
| `ISP-001` | interfaz MFT ancha | contratos por capacidad |
| `SRP-007` | command center mezcla reads/writes | CQRS |
| `ISP-002` | command center interface ancha | separar query/operations |
| `SRP-008` | returns service grande | eligibility/assembly/generation policies |
| `DIP-002` | returns crea servicios concretos | DI obligatoria |
| `SRP-009` | ReportsController grande | controllers por recurso |
| `DIP-003` | fallbacks concretos distribuidos | fail-fast composition |
| `OCP-002` | strategy factory por IDs concretos | resolver DI o remover código muerto |
| `TEST-001` | tiempo estático ampliamente usado | `TimeProvider`/clock port en lógica operativa |
| `TEST-002` | MFT usa reloj global | reloj determinista |
| `RES-003` | health client manual en Application | Infrastructure + managed client |
| `RES-004` | WCF resilience no homogénea | policy por idempotencia |
| `ASYNC-001` | sync-over-async | async end-to-end |
| `SEC-002` | logging completo de request/response | redacción |
| `OBS-001` | escritura directa de crash log | logging centralizado |
| `CFG-001` | dependencias runtime nullable | fail-fast DI |
| `MOD-001` | reglas ACHCOL/CENIT dispersas | módulos/policies separados |

## 7.3 P2 — Cleanup estructural posterior

| ID | Hallazgo | Objetivo |
|---|---|---|
| `MOD-002` | seeder oficial muy grande | separar por rail/versión/flujo |
| `MOD-003` | certificate management grande | separar lectura/admin/validation |
| `MOD-004` | DTO command center muy grande | organizar por feature |

---

# 8. Plan de implementación por fases

## R0 — Baseline, characterization y architecture guardrails

**Estado inicial:** `PLANNED`  
**Prioridad:** P0  
**Objetivo:** crear una red de seguridad antes de mover lógica crítica.

### Alcance

- No modificar comportamiento funcional.
- No mover todavía `NachaFileBuilder` ni parser.
- No crear migraciones.

### Trabajo

1. Capturar SHA/branch y working tree.
2. Ejecutar baseline focal y broad si el host lo permite.
3. Inventariar:
   - referencias de proyectos;
   - paquetes de Application;
   - archivos Application con ASP.NET/WCF/NLog/X509;
   - `new HttpClient`;
   - `AppSettings.Settings`;
   - `?? new` para dependencias runtime;
   - tamaños de clases críticas;
   - uso de `DateTime.UtcNow` en rutas operacionales.
4. Crear pruebas de arquitectura **ratchet**, no una regla que deje todo rojo desde el primer commit.
5. Crear/fortalecer characterization tests para:
   - generación NACHA ACH Colombia;
   - generación CENIT PPD/CCD/CTX;
   - inbound ordinary parse;
   - Return/Return-of-Return existentes;
   - MFT outbound/inbound, duplicate, retry, uncertain;
   - Quartz cluster/idempotency donde exista harness.
6. Confirmar que golden files existentes permanecen byte-for-byte.
7. Crear `docs/architecture/refactor-ca-solid-resilience-baseline.md` con métricas y allowlist inicial.

### Diseño del ratchet

Mientras existan violaciones legacy, la prueba debe:

- mantener una lista explícita de excepciones actuales;
- fallar si aparece una violación nueva;
- eliminar excepciones en cada fase;
- terminar R1/R3 con las reglas objetivo sin allowlist para las fronteras cerradas.

### Criterios de aceptación

- Baseline documentado.
- No cambio funcional.
- Architecture tests detectan nuevas violaciones.
- Golden/characterization tests pasan.
- No se cambia expected output para acomodar el refactor.

### Estado memory al cerrar

`REF-R0-GUARDRAILS = VERIFIED/CLOSED` con SHA y evidencia.

### Prompt ChatGPT — R0

> Ejecuta únicamente la fase R0 del plan `PLAN_IMPLEMENTACION_REFACTOR_CA_SOLID_RESILIENCIA.md`. Lee primero `AGENTS.md`, el estado relevante de `MEMORY.md`, `docs/CURRENT_PROJECT_STATUS.md`, `docs/architecture/result-pattern-solid-plan.md` y este plan. No cambies comportamiento funcional. Captura el baseline del árbol actual, crea architecture tests tipo ratchet para impedir nuevas violaciones, fortalece characterization/golden tests de las rutas críticas indicadas y documenta la allowlist inicial. Ejecuta validación focal, build Release y suite backend si el host lo permite. Si no puede ejecutarse, deja `IMPLEMENTED_UNVERIFIED`, no `VERIFIED`. Actualiza el state memory solo con evidencia. No avances a R1.

---

## R1 — Limpieza de la frontera Application

**Prerequisito:** R0 `CLOSED`.

### Objetivo

Conseguir que `Application` deje de contener concerns de presentación e infraestructura directa.

### Subfase R1A — HTTP/Middleware/Filters

Mover desde `Application` hacia `Api`:

- middlewares;
- filtros MVC;
- exception handling HTTP;
- cualquier tipo que dependa de `HttpContext`, `RequestDelegate`, `IActionFilter`, `ProblemDetails` o MVC.

Introducir puertos de Application solo cuando un caso de uso realmente necesite información contextual, por ejemplo:

```csharp
public interface ICurrentUserContext
{
    string? UserId { get; }
    IReadOnlyCollection<string> Permissions { get; }
}
```

La implementación basada en `HttpContext` pertenece a Api.

### Subfase R1B — Logging

- Retirar dependencia directa de NLog desde Application.
- Preferir `ILogger<T>` si no introduce acoplamiento indebido para el caso; alternativamente, un puerto mínimo de logging/auditoría cuando el dominio de aplicación lo justifique.
- NLog permanece como provider de host/composition, no como dependencia de caso de uso.

### Subfase R1C — WCF/Connected Services

- Mover generated proxy/Connected Service de Application a External.
- Application conserva contratos agnósticos de transporte.
- Mantener mapper/DTO interno libre de términos SOAP/WSDL cuando ya existe un contrato limpio.

### Subfase R1D — Certificados/X509

- Crear puerto de Application para obtener material criptográfico/certificado según necesidad.
- Implementación `X509Store` en External.
- No filtrar secretos/certificados privados al frontend.

### Subfase R1E — csproj

Cuando el código ya no lo requiera, retirar de `Application.csproj`:

- ASP.NET Core MVC/HTTP packages;
- NLog;
- WCF packages.

No retirar un paquete hasta confirmar cero consumidores válidos.

### Criterios de aceptación

- Application no referencia ASP.NET MVC/HTTP para presentación.
- Application no referencia NLog.
- Application no referencia `System.ServiceModel`.
- Application no abre `X509Store`.
- Tests de arquitectura R0 eliminan esas excepciones de allowlist.
- Comportamiento HTTP/SOAP/certificados permanece igual.

### Prompt ChatGPT — R1

> Ejecuta solo R1. Usa el ratchet creado en R0. Limpia la frontera `Cfa.ACHInterbank.Application`: mueve concerns HTTP/middleware/filtros a Api, proxies WCF a External, acceso X509 a un adapter External detrás de un puerto, y elimina NLog directo de Application. No cambies contratos funcionales, reglas ACH/CENIT ni semántica de errores salvo el mínimo necesario para conservar comportamiento. Retira paquetes del csproj solo cuando no tengan consumidores. Ejecuta pruebas focales de auth/error handling/SOAP/certificados, architecture tests, build y regresión afectada. Reduce la allowlist, actualiza memory con evidencia y no avances a R2.

---

## R2 — DI, configuración y fail-fast

**Prerequisito:** R1 `CLOSED`.

### Objetivo

Eliminar service construction oculta y globals de configuración en rutas runtime, de forma incremental.

### Trabajo

1. Identificar `?? new ConcreteDependency(...)` y `new SomeService(...)` dentro de servicios de runtime.
2. Clasificar:
   - value object/helper puro: puede mantenerse si realmente es local y sin IO/state;
   - servicio/capacidad runtime: debe entrar por DI.
3. Hacer obligatorias las dependencias esenciales.
4. Migrar `AppSettings.Settings` a options/ports específicos por vertical, priorizando:
   - External clients;
   - auth/token;
   - encryption/certificates;
   - health/load balancer.
5. Composition root valida configuración al startup (`ValidateOnStart` cuando aplique).
6. No crear una mega clase `IAppSettings` como sustituto del global actual.

### Criterios

- Servicios críticos no crean sus collaborators concretos.
- Configuración por opciones cohesivas.
- Errores de configuración fallan al inicio, no a mitad de transacción.
- Architecture tests/scan impiden nuevos globals/fallback DI en runtime.

### Prompt — R2

> Ejecuta solo R2. Elimina creación oculta de servicios runtime (`?? new`, `new` de collaborators) y migra gradualmente `AppSettings.Settings` hacia options/ports cohesivos, empezando por las rutas de integración y seguridad. No conviertas helpers puros en interfaces sin beneficio. Configuración inválida debe fallar de forma temprana. Conserva comportamiento, tests y secretos fuera de logs. Actualiza el ratchet/memory y no avances a R3.

---

## R3 — Resiliencia y seguridad de integraciones externas

**Prerequisito:** R2 `CLOSED`.

### Objetivo

Asegurar que todas las integraciones externas críticas tengan una política explícita, coherente con idempotencia y tipo de operación.

### R3A — HTTP/SOAP sobre HttpClient

Priorizar:

- `WsAxonRespuestaTransaccionesSoapClient`;
- `HttpClientService`;
- health HTTP legacy.

Refactor:

- typed/named clients por `IHttpClientFactory`;
- pipeline de timeout/retry/circuit breaker/bulkhead donde sea seguro;
- configuración por integration mapping/options;
- handlers administrados por DI.

### R3B — WCF

Para `ConnectionAuth` y otros WCF:

- adapter de transporte;
- timeout explícito;
- clasificación de excepciones transitorias vs definitivas;
- retry solo si la operación es segura/idempotente o cuenta con idempotency key/protocolo equivalente;
- no copiar automáticamente la policy HTTP si semánticamente no aplica.

### R3C — Logging seguro

Eliminar logging de:

- curl completo con secretos;
- Authorization headers;
- payload SOAP/XML sensible;
- cuentas completas;
- secretos de certificado/credential.

Mantener:

- correlation/operation ID;
- mapping ID;
- endpoint sanitizado;
- elapsed time;
- status técnico;
- exception class;
- hash cuando sea seguro.

### R3D — Tests de fault injection

Agregar escenarios:

- timeout;
- connection refused;
- HTTP 5xx;
- circuit open;
- retries agotados;
- resultado incierto;
- cancelación;
- operación definitiva que no debe reintentarse.

### Criterios

- Cero `new HttpClient()` en runtime productivo salvo excepción justificada/documentada.
- Cada cliente externo crítico tiene política declarada.
- No hay retry ciego para operaciones monetarias/no idempotentes.
- Logs no contienen datos sensibles.

### Prompt — R3

> Ejecuta solo R3. Normaliza clientes HTTP/SOAP/WCF con adapters y resiliencia explícita basada en la semántica real de cada operación. Migra `new HttpClient()` a `IHttpClientFactory`/typed clients, pero no agregues retries a operaciones no idempotentes sin garantía. Añade fault-injection tests y logging seguro con redacción. Conserva los contratos funcionales y la idempotencia existente. No avances a R4.

---

## R4 — Descomposición segura de `NachaFileBuilder`

**Prerequisito:** R0–R3 `CLOSED`.

**Riesgo:** muy alto. Ejecutar en subfases independientes.

### Regla de oro

La salida oficial NACHA-M para escenarios caracterizados debe permanecer **byte-for-byte idéntica**.

### R4A — Convertir el builder actual en facade/orchestrator sin cambiar output

1. Identificar responsabilidades reales por región/método.
2. Extraer primero componentes puramente deterministas sin IO:
   - control totals;
   - padding/blocking;
   - normalización/formato de campos;
   - secuencias/adenda helpers si ya tienen contratos claros.
3. Mantener `NachaFileBuilder` como facade para callers actuales.
4. No mover todavía decisiones regulatorias entre cámaras.

### R4B — Record renderers

Introducir contratos pequeños, por ejemplo:

- `IFileHeaderRenderer` (T1)
- `IBatchHeaderRenderer` (T5)
- `IEntryDetailRenderer` (T6)
- `IAddendaRenderer` (T7)
- `IBatchControlRenderer` (T8)
- `IFileControlRenderer` (T9)

Los nombres exactos deben ajustarse al modelo actual; no forzar una abstracción que duplique el motor table-driven.

### R4C — Assembly pipeline

Separar:

- selección del profile PUBLISH;
- selección de transacciones;
- construcción de lotes;
- rendering;
- control totals;
- validation;
- artifact/audit.

### R4D — Policies por cámara/flujo

Solo después de estabilizar el pipeline:

- encapsular decisiones específicas ACH Colombia/CENIT;
- no usar switch rígido por IDs de DB;
- preferir resolver por código/perfil/configuración existente;
- conservar table-driven authority.

### R4E — Eliminar fallbacks y adelgazar facade

Objetivo indicativo:

- `NachaFileBuilder` termina como orquestador del orden de ~300–500 líneas o menos si el diseño natural lo permite;
- el número exacto no es gate; cohesión y testabilidad sí.

### Tests obligatorios

- golden outbound ACH Colombia;
- CENIT PPD/CCD/CTX;
- monetaria/prenotificación;
- Returns/ROR si usan el builder;
- límites de batches/entries/addendas;
- published profile selection;
- historical PUBLISH immutability;
- dual DB donde el flujo persista estado;
- no cambio de hashes/artifacts esperados salvo timestamps deliberadamente excluidos.

### Prompt — R4A

> Ejecuta únicamente R4A. No rediseñes todo `NachaFileBuilder`. Usa characterization/golden tests, identifica responsabilidades y extrae únicamente componentes deterministas de bajo riesgo, manteniendo el builder como facade y salida byte-for-byte. No cambies reglas ACH Colombia/CENIT ni perfiles PUBLISH. Ejecuta golden tests completos y no avances a R4B.

### Prompt — R4B/R4C

> Ejecuta únicamente la subfase indicada R4B o R4C. Continúa la descomposición incremental del builder, mantén compatibilidad de callers y output byte-for-byte. No introduzcas un modelo PaymentRail ni reescribas el motor table-driven. Si una extracción obliga a reinterpretar una regla normativa, detente y marca BLOCKED. No avances automáticamente.

### Prompt — R4D/R4E

> Ejecuta solo la subfase indicada R4D o R4E. Encapsula decisiones específicas por cámara/flujo usando la configuración y contratos existentes, sin switch rígido por IDs y sin alterar PUBLISH authority. Elimina fallbacks concretos restantes únicamente después de contar con DI y tests. Ejecuta regresión NACHA integral y no avances.

---

## R5 — Parser inbound y post-processing pipeline

### Objetivo

Separar parsing físico, validación, persistencia y acciones posteriores.

### R5A — Parser físico determinista

Objetivo conceptual:

```text
File/Text Reader
  -> Record Tokenizer/Parser
  -> Parsed NACHA model
```

Debe poder probarse sin `AchDbContext`.

### R5B — Validación

Separar:

- estructura física;
- constraints del snapshot PUBLISH seleccionado;
- reglas regulatorias/semánticas;
- correlación cuando aplique.

### R5C — Persistencia

Persistir el resultado validado en un componente explícito/transaccional.

### R5D — Post-processing

Convertir `IncomingNachaPostProcessingOrchestrator` en pipeline/orchestrator de etapas:

- resolve context;
- eligibility;
- link/correlation;
- state transition;
- dispatch planning;
- external dispatch cuando corresponda;
- audit/events.

Cada etapa debe declarar input/output y cancellation.

### Criterios

- parser físico unit-testable sin DB;
- no se pierde PUBLISH snapshot authority;
- exact-file membership y lineage preservados;
- idempotencia y recovery preservados;
- `None`/duplicate/manual-review no disparan SOAP.

### Prompt — R5

> Ejecuta solo la subfase R5 indicada. Separa parsing físico, validación, persistencia y post-processing sin cambiar semántica inbound ni autoridad PUBLISH. Antes de mover dispatch/correlation, crea regression tests deterministas de clasificación, duplicate, manual review, Return correlation y recovery. Mantén transacciones y estados actuales. No avances a la siguiente subfase sin pruebas verdes.

---

## R6 — MFT Managed File Exchange + Command Center: SRP/ISP/CQRS

### R6A — MFT contracts

Dividir responsabilidades actualmente agrupadas en `IAchColombiaManagedFileExchangeService` en capacidades cohesivas, por ejemplo:

- ejecución outbound/inbound;
- transfer query/read model;
- administration/configuration;
- credential administration;
- connection test/manual operations.

Mantener una facade temporal si evita breaking changes masivos.

### R6B — MFT implementation

Separar:

- workflow de outbound;
- workflow de inbound;
- retry/reprocess;
- archive/retire;
- query projections;
- admin/credentials.

### R6C — Incoming Command Center

Separar:

- query/read service;
- manual operations/commands;
- timeline/read model si corresponde.

### Criterios

- Los consumidores dependen solo de las capacidades que usan.
- No se alteran estados persistidos por conveniencia.
- `Transferred` no pasa a significar aceptación remota.
- Claim/recovery/idempotencia/duplicate permanecen.
- cluster-safe behavior preservado.

### Prompt — R6

> Ejecuta únicamente R6A, R6B o R6C según el state memory. Aplica ISP/CQRS de forma incremental con facades temporales si ayudan a controlar impacto. No cambies estados MFT, semántica de aceptación externa, ventanas, credenciales ni Quartz. Prueba duplicate, retry, uncertain, claim/recovery y dos instancias cuando el harness esté disponible. No avances automáticamente.

---

## R7 — Returns / Return-of-Return

### Objetivo

Reducir responsabilidad de `AchReturnsService` sin reinterpretar reglas cerradas.

### Separaciones objetivo

- eligibility policy;
- original transaction correlation;
- return batch assembly;
- NACHA return artifact generation;
- cause/policy resolution;
- state transitions;
- audit/monitoring.

### Guardrails especiales

- No reabrir `DEC-ACHCOL-R10-DEV14-001`.
- DEV14 nunca se convierte en Rxx/Addenda 99.
- Mantener reglas CENIT y ACH Colombia separadas.
- ROR no debe inferir políticas de otra cámara.

### Tests

- causas permitidas;
- exact original trace/amount;
- R10 same-day policy aceptada;
- DEV14 rechazado del ReturnOut;
- CENIT R23/ROR contextual;
- duplicate/idempotency;
- provider-native trace allocation y concurrencia.

### Prompt — R7

> Ejecuta solo R7. Refactoriza `AchReturnsService` por responsabilidades, preservando exactamente las decisiones cerradas de Returns/ROR en `MEMORY.md`. No rederives R10/DEV14 ni mezcles CENIT con ACH Colombia. Usa los tests regulatorios existentes como contrato y agrega characterization antes de mover lógica. No avances a R8.

---

## R8 — API/controller modularization y large-class cleanup secundario

### R8A — ReportsController

Separar endpoints por recurso/capacidad sin alterar rutas públicas salvo que exista una fase de compatibilidad explícita.

Preferir:

- nuevos controllers con rutas existentes mediante atributos equivalentes;
- handlers/query services dedicados;
- no duplicar lógica en controller.

### R8B — servicios secundarios grandes

Evaluar individualmente, no por tamaño automático:

- `CertificateManagementServices`;
- official profile seeders;
- DTO agrupados;
- servicios >500 líneas restantes.

Solo refactorizar si existe responsabilidad múltiple demostrable.

### Criterios

- API contract compatible;
- OpenAPI/Scalar sin regresiones relevantes;
- authorization policies idénticas;
- controller no crea dependencias concretas;
- no introducir wrappers inútiles.

### Prompt — R8

> Ejecuta únicamente R8A o el item R8B autorizado. Modula controllers/servicios grandes preservando rutas, policies, DTOs y semántica. No uses el tamaño de archivo como única razón para crear abstracciones. Ejecuta API contract tests/OpenAPI y regresión de autorización. No avances a R9.

---

## R9 — Tiempo determinista y consistencia async

### Objetivo

Eliminar dependencias del reloj real en lógica operacional crítica y sync-over-async.

### Estrategia

No reemplazar los 133 archivos a ciegas. Priorizar:

1. ciclos/ventanas/cutoffs;
2. MFT/retry/recovery;
3. Returns/ROR;
4. scheduler/misfire/business date;
5. auditoría donde el timestamp afecte lógica.

Preferir `TimeProvider` .NET cuando sea suficiente; usar un puerto operacional solo si agrega semántica real de timezone/business date.

### Async

- eliminar `.GetAwaiter().GetResult()` y `.Result` en caminos async;
- propagar `CancellationToken` cuando sea razonable;
- no convertir APIs sync puras en async sin necesidad.

### Criterios

- pruebas de límites horarios usan fake time;
- timezone operacional preservada;
- no usar UTC date como business date por accidente;
- C1/C7/MFT/retries reproducibles.

### Prompt — R9

> Ejecuta solo R9 sobre las rutas operacionales priorizadas. Introduce `TimeProvider`/clock abstraction donde el tiempo determine comportamiento, no como reemplazo mecánico global. Elimina sync-over-async de rutas tocadas y preserva la timezone operacional configurada. Agrega boundary tests con fake time. No avances a R10.

---

## R10 — Recertificación interna del refactor y cierre documental

### Objetivo

Probar que el refactor cambió estructura, no comportamiento contractual.

### Gates mínimos

1. `python scripts/validate-solution-projects.py`
2. `dotnet build ACHInterbank.sln -c Release`
3. backend broad suite completa;
4. suites focales de architecture, NACHA, inbound, returns, MFT, Quartz, SOAP/integrations;
5. SQL Server + PostgreSQL donde aplique;
6. SPA build/test si fue tocada;
7. controlled E2E de MFT/Quartz si el ambiente lo soporta;
8. scan final de:
   - packages prohibidos en Application;
   - `new HttpClient`;
   - global AppSettings en runtime;
   - `?? new` de services;
   - architecture allowlist;
   - clases críticas y dependency counts;
9. revisar `git diff` por cambios normativos accidentales/golden files.

### Documentación

Actualizar:

- `docs/CURRENT_PROJECT_STATUS.md`;
- documento de cierre en `docs/architecture/`;
- `MEMORY.md` con cierres durables;
- `README.md` solo si cambia la experiencia de desarrollo visible.

### Estado permitido

- `REFACTOR-CA-SOLID-RES-001 = CLOSED` solo si todos los gates internos definidos pasan en el commit exacto.
- No implica `UAT_READY`, `USER_ACCEPTED`, `RELEASE_READY` ni homologación externa.

### Prompt — R10

> Ejecuta únicamente R10. No implementes nuevas mejoras. Ejecuta la recertificación interna del refactor sobre el commit actual: architecture tests, build Release, suite backend, provider-specific tests aplicables, SPA si fue tocada y controlled E2E disponible. Compara outputs/golden, revisa diff y actualiza documentación/memory. Si falta cualquier gate por limitación de ambiente, usa `IMPLEMENTED_UNVERIFIED` o `VERIFIED` parcial según evidencia; no declares CLOSED global. No avances a otro programa.

---

# 9. Criterios de diseño objetivo al terminar

## 9.1 Dependencias

```text
Api ----------------------> Application ----------------------> Domain
 |                              ^
 |                              |
 +---- composition ------------+

Persistence ------------------> Application / Domain
External ---------------------> Application / Domain

Domain: no depende de Application/Persistence/External/Api
Application: no depende de Api/Persistence/External ni de frameworks de presentación/transporte concretos
```

La dirección exacta debe respetar el diseño real de contratos; no introducir referencias circulares.

## 9.2 NACHA outbound

Objetivo conceptual:

```text
NachaGenerationUseCase/Facade
  -> PublishedProfileResolver
  -> TransactionSelection
  -> BatchAssembly
  -> RecordRendering/TableDrivenEngine
  -> ControlTotals
  -> SemanticValidation
  -> Artifact/Audit
```

Los nombres exactos pueden variar. Se debe reutilizar el motor table-driven y no duplicarlo.

## 9.3 Inbound

```text
Input
  -> Physical Parse
  -> Profile Resolution
  -> Structural/Semantic Validation
  -> Persist
  -> Post-processing Pipeline
      -> Correlation
      -> Eligibility
      -> State
      -> Dispatch plan
      -> External adapter
      -> Audit
```

## 9.4 Integraciones

Cada integración externa debe tener:

- puerto/adapter;
- client lifecycle administrado;
- timeout;
- política explícita de retry o explícita ausencia de retry;
- circuit breaker/bulkhead cuando aplique;
- cancellation;
- logging seguro;
- tests de fallos;
- idempotency semantics documentadas.

---

# 10. Métricas de seguimiento

Registrar al inicio y al cierre de cada fase, sin convertirlas en objetivos aislados:

- violaciones de fronteras arquitectónicas;
- excepciones de allowlist del ratchet;
- `new HttpClient()` en runtime;
- `AppSettings.Settings` en runtime;
- fallbacks `?? new Service`;
- classes >500 y >1000 líneas;
- constructores con >10 dependencias;
- interfaces con >10 métodos;
- usos de reloj estático en rutas operacionales;
- tests focales/broad passed/failed/skipped;
- dual-provider evidence;
- golden file diffs;
- warnings nuevos de build.

Una mejora métrica no justifica por sí sola un cambio si aumenta complejidad conceptual.

---

# 11. Política de commits/PRs

Preferencia:

- un PR por fase o subfase de alto riesgo;
- no mezclar formateo masivo con cambios estructurales;
- commit intermedio solo cuando deja el árbol compilable/testeable;
- mensajes sugeridos:
  - `refactor(architecture): add boundary ratchet and characterization guards`
  - `refactor(application): remove web and transport infrastructure dependencies`
  - `refactor(external): unify resilient managed http clients`
  - `refactor(nacha): extract deterministic outbound generation components`

Antes de cerrar:

```cmd
git status --short
git diff --stat
git diff --check
git diff -- MEMORY.md
git diff --ignore-space-at-eol -- MEMORY.md
```

---

# 12. Condiciones de bloqueo

Detener la fase y marcar `BLOCKED` si:

1. el refactor requiere decidir una regla normativa no respaldada;
2. una golden/regression test revela una divergencia funcional no entendida;
3. un cambio exige reescribir PUBLISH snapshots históricos;
4. se necesita conectar a infraestructura productiva real;
5. un retry seguro no puede establecerse para una operación externa;
6. hay una contradicción entre `MEMORY.md` y código actual que afecta una decisión cerrada;
7. la fase exige una migración de DB no justificada por el refactor;
8. el cambio rompe CENIT para resolver ACH Colombia o viceversa.

---

# 13. Definition of Done global

El programa solo puede cerrarse cuando:

- `Application` esté libre de dependencias directas de presentación/transporte/infraestructura identificadas en el scope;
- DI/composition sea explícita y fail-fast en servicios críticos;
- integrations críticas tengan resiliencia y logging seguro coherentes;
- `NachaFileBuilder` y parser hayan sido descompuestos sin alterar outputs/reglas;
- MFT/Command Center/Returns tengan responsabilidades más pequeñas y contratos cohesivos;
- tiempo operacional crítico sea determinista en tests;
- architecture tests bloqueen regresiones;
- build/tests definidos pasen en el commit exacto;
- `MEMORY.md` registre cierres, gaps y evidencia;
- no se haga ninguna afirmación de homologación/UAT/release que no esté respaldada.

