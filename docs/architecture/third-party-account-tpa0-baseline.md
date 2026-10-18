# TPA0 baseline — Third-party account governance

**Programa:** `TPA-GOV-001`  
**Fase ejecutada:** `TPA0` únicamente  
**Fecha:** 2026-10-11  
**SHA:** `UNAVAILABLE_SOURCE_ARCHIVE_NO_GIT_METADATA`  
**Estado:** `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`  
**Cambio funcional intencional:** ninguno  
**Migraciones/DB:** ninguna  
**Cambio normativo:** ninguno

## 1. Gate de entrada y baseline reproducible

Se ejecutaron, antes de modificar el árbol:

```text
git status --short
  -> fatal: not a git repository

git rev-parse HEAD
  -> fatal: not a git repository

python scripts/validate-solution-projects.py
  -> SOLUTION GRAPH: OK
     solution projects: 7
     disk projects: 7
     project references: all targets exist and are loaded by the solution
```

Al no existir metadata Git en el source archive, el SHA contractual de esta ejecución es `UNAVAILABLE_SOURCE_ARCHIVE_NO_GIT_METADATA`.

Se revalidó `docs/ai/third-party-accounts/TPA0_BASELINE_INVENTORY.json` contra el árbol antes de agregar characterization tests: todos los fingerprints listados para las piezas TPA0 coincidían con el paquete entregado.

Después de los cambios TPA0 se hizo una comparación byte-a-byte contra el ZIP fuente y se confirmó **0 diferencias** en:

- `src/**`;
- `src/**/Migrations/**`;
- `docs/normativa/**`;
- SPA de producción bajo `web/ach-interbank-ui/src/**`, excluyendo `*.spec.ts`.

Por tanto, los cambios de esta fase quedan limitados a pruebas de caracterización y documentación/state/memory.

## 2. Inventario de `CustomerThirdParty`

### 2.1 Modelo y constraints actuales

`CustomerThirdParty` contiene `CustomerId`, institución destino, cuenta destino, identificación del receptor, estado global `Pending/Active/Rejected`, una sola referencia de prenotificación y una sola evidencia/resumen de validación (`ValidationCycleId`, `ValidationReceivedAt`, `ValidationMessage`) (`src/Cfa.ACHInterbank.Domain/Models/ACH/CustomerThirdParty.cs:6-26`).

Su identidad persistida es única por `(CustomerId, DestinationInstitutionId, DestinationAccountNumber, RecipientIdNumber)` y `Status` es concurrency token (`src/Cfa.ACHInterbank.Persistence/Configuration/CustomerThirdPartyConfiguration.cs:14-20,38-44`). No existe `AccountType` en `CustomerThirdParty`; `CustomerAccount` tampoco conserva tipo de cuenta, sólo `CustomerId` y `AccountNumber` (`src/Cfa.ACHInterbank.Domain/Models/ACH/CustomerAccount.cs`).

### 2.2 Readers

| Reader | Evidencia | Uso |
|---|---|---|
| `CustomerThirdPartyRepository.FindAsync` | `src/Cfa.ACHInterbank.Persistence/ACH/Repositories/Implementation/CustomerThirdPartyRepository.cs:19-29` | Lookup normalizado desde onboarding de prenotificación. |
| `CustomerThirdPartyAppService` | `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/CustomerThirdPartyAppService.cs:21-171` | Read model paginado consumido por API/SPA. |
| `AchStateTransitionService` | `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchStateTransitionService.cs:176-195` | Busca el legacy vinculado a la prenotificación antes de sincronizar resultado automático. |
| `IncomingNachaPrenotificationResolver` | `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/IncomingNachaPrenotificationResolver.cs:72-107` | Resuelve candidato por cuenta/identidad/institución/vínculo entrante. |
| `CustomerThirdPartiesController` | `src/Cfa.ACHInterbank.Api/Controllers/CustomerThirdPartiesController.cs:21-54` | GET-only. |
| SPA transaction selector | `web/ach-interbank-ui/src/app/features/transactions/services/transactions-api.service.ts:87-114` | Consulta legacy con `status=Active`. |

### 2.3 Writers

| Writer | Evidencia | Efecto |
|---|---|---|
| `PrenotificationHandler` | `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/PrenotificationHandler.cs:24-74` | Crea `Pending` o religa una prenotificación si el registro sigue `Pending`; no resetea `Active/Rejected`. |
| `AchStateTransitionService` | `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchStateTransitionService.cs:160-195` | `AppliedTacitly/Certified -> Active`; `ReturnedByOperator/ReturnedByEpr -> Rejected`. |
| `IncomingNachaPrenotificationResolver` | `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/IncomingNachaPrenotificationResolver.cs:173-180` | Activa automáticamente un candidato determinístico. |

No se encontró writer HTTP de resultado interbancario. `CustomerThirdPartiesController` declara únicamente `[HttpGet]` (`src/Cfa.ACHInterbank.Api/Controllers/CustomerThirdPartiesController.cs:21-54`).

El resolvedor entrante puede devolver `RequiresManualReview=true` cuando la correlación es ambigua o insuficiente, pero en esos casos **no aplica** un resultado al tercero (`IncomingNachaPrenotificationResolver.cs:109-170`). Esa revisión no equivale a una API humana de aprobar/rechazar.

## 3. Onboarding silencioso actual

### 3.1 SINGLE

`AchTransactionService` ejecuta `EnsureCustomerAndAccountsAsync` **antes** de `IBatchResolver.ResolveAsync`, es decir, antes de que el contexto de cámara/ciclo sea necesario (`AchTransactionService.cs:132-152`).

La rutina:

1. normaliza documento/cuenta con `Trim()`;
2. resuelve tipos de documento/persona desde catálogo;
3. busca primero por `(DocumentType, DocumentNumber)` y luego permite el fallback legacy de un único cliente por número;
4. crea `Customer` + `CustomerAccount` si no existen;
5. agrega la cuenta si el Customer ya existe y no la posee (`AchTransactionService.cs:205-299`).

Después de persistir la transacción, sólo si es prenotificación se invoca `PrenotificationHandler` (`AchTransactionService.cs:152-167`). El handler resuelve el Customer desde la cuenta origen; si no existe, retorna y deja la transacción ACH registrable sin crear `CustomerThirdParty` (`PrenotificationHandler.cs:31-38`).

**Caracterización:** el onboarding silencioso de `Customer/CustomerAccount` es genérico y no recibe clearing house. El onboarding legacy de `CustomerThirdParty`, en cambio, está acoplado a que la operación sea una prenotificación y a que la cuenta origen resuelva un `Customer` interno.

No se identificó llamada de red en `EnsureCustomerAndAccountsAsync` ni en `PrenotificationHandler`; sus dependencias son repositorios/contexto local.

### 3.2 BULK

`AchBulkTransactionService` precarga en una sola consulta los Customers relevantes y construye caches por documento/tipo y por número (`AchBulkTransactionService.cs:345-369`). Por ítem, realiza el onboarding de `Customer/CustomerAccount` antes de resolver batch/cámara (`AchBulkTransactionService.cs:155-170`).

Su `EnsureCustomerAndAccountsAsync` replica la semántica single para originador y receptor (`AchBulkTransactionService.cs:372-403`) y el helper usa el cache para reutilizar Customer/cuentas dentro del mismo batch (`AchBulkTransactionService.cs:405-455`). Luego, si el ítem es prenotificación, llama al mismo `IPrenotificationHandler` (`AchBulkTransactionService.cs:181-184`).

**Diferencia single vs bulk:** single consulta repositorio por transacción y corre bajo execution strategy + DB transaction de la operación; bulk precarga/cacha Customers, procesa por chunks, acumula resultados por ítem, hace commit por chunk y limpia el ChangeTracker al cerrar cada chunk. La semántica de alta/enriquecimiento es equivalente, pero la estrategia de lookup/reuso/reintento no es la misma.

## 4. Idempotencia y reintentos

### 4.1 Lo que existe

- `CustomerThirdPartyRepository.FindAsync` busca por la identidad única normalizada antes de `AddAsync` (`CustomerThirdPartyRepository.cs:19-35`).
- `PrenotificationHandler` reutiliza el registro `Pending`; si ya está `Active/Rejected` lo deja intacto (`PrenotificationHandler.cs:42-73`).
- Bulk conserva cache intra-request, evitando recrear el mismo Customer durante ese request (`AchBulkTransactionService.cs:345-369,427-455`).
- DB tiene unique constraints para Customer `(DocumentType, DocumentNumber)`, CustomerAccount `(CustomerId, AccountNumber)` y CustomerThirdParty identity (`CustomerConfiguration.cs:26-27`, `CustomerAccountConfiguration.cs:18`, `CustomerThirdPartyConfiguration.cs:38-44`).
- La transición de estado ACH sí implementa idempotencia explícita por `IdempotencyKey`, incluido recovery de unique violation + re-read (`AchStateTransitionService.cs:47-63,92-139`).
- `CustomerThirdParty.ApplyAutomaticNachaResult` no vuelve a mutar un resultado final y rechaza evidencia que corresponda a otra prenotificación (`CustomerThirdParty.cs:45-93`).

### 4.2 Límite observado

El onboarding `Customer/CustomerAccount/CustomerThirdParty` usa patrón **check-then-add**; no se observó catch de unique conflict + re-read equivalente al de `AchStateTransitionService`. La execution strategy de single no transforma por sí sola una colisión funcional de unicidad en idempotencia. Por ello la idempotencia secuencial está caracterizada, pero la concurrencia de dos altas iguales queda como gap para TPA2.

## 5. Resolución automática de prenotificaciones

`AchStateTransitionService` sincroniza automáticamente:

- `AppliedTacitly` o `Certified` -> `CustomerThirdParty.Active`;
- `ReturnedByOperator` o `ReturnedByEpr` -> `CustomerThirdParty.Rejected` (`AchStateTransitionService.cs:160-195`).

El job `AchTacitAcceptanceJobHandler` selecciona prenotificaciones `Pending` vencidas y ejecuta la transición `AppliedTacitly` automáticamente (`AchTacitAcceptanceJobHandler.cs:31-62`).

Los tests existentes `DifferentialPrenotificationResponseProcessorTests` ya cubren respuesta exitosa -> `Active`, rechazo -> `Rejected`, aislamiento por cámara/correlación y repetición controlada. `CustomerThirdPartyGovernanceTests.Controller_DoesNotExposeManualStatusWriteEndpoint` caracteriza la ausencia de endpoint POST/PUT/PATCH/DELETE para decidir el resultado.

No se agregó aprobación/rechazo manual y no se modificó este flujo.

## 6. Policy baseline por cámara

La autoridad de runtime es `ClearingHouseTransactionRule`, que almacena cámara, naturaleza, tipo, modo de prenotificación, lead days, validación de identificación, vigencia y metadata normativa (`src/Cfa.ACHInterbank.Domain/Models/ACH/ClearingHouseTransactionRule.cs:7-31`). `TransactionPrerequisitePolicyService` resuelve una única regla efectiva por cámara/naturaleza/tipo/fecha (`TransactionPrerequisitePolicyService.cs:125-153`).

| Cámara/naturaleza | Config actual | Characterization existente | Fuente de proyecto | Resultado TPA0 |
|---|---|---|---|---|
| ACH Colombia débito | Mandatory, `lead=3` | sin prenote falla; <3 business days falla; ==3 pasa | V35 2.11.4/2.11.4.2: obligatoria y al menos 3 días; además menciona 4 ciclos operativos | Dirección general consistente; no se cambió. |
| ACH Colombia crédito | Optional, `lead=null` | sin prenote pasa | V35 2.10.3: opcional; 2.10.3.2: **si se usa**, mínimo 3 días hábiles | Opcionalidad correcta, pero el lead condicional de prenote opcional no está representado/enforced. GAP-TPA-011. |
| CENIT débito | Mandatory, `lead=null` | sin prenote falla; prenote same-day no inventa +3 | Anexo 2 4.7: obligatoria; 4.8: devolución misma fecha valor, máximo último ciclo | Se mantiene separado de ACH Colombia; no copia +3. |
| CENIT crédito | Optional, `lead=null` | sin prenote pasa | Anexo 2 4.7: “podrá”; no obligatoria | Consistente al nivel caracterizado. |

El seeder confirma reglas distintas por cámara (`RegulatoryCatalogSeeder.cs:538-615`), pero la metadata ACH Colombia todavía dice `MAN-004 ACH Colombia V32` (`:558,576`) aunque la autoridad del proyecto es V35.

## 7. Verificaciones pedidas sin corrección

### 7.1 Madurez global +3 business days

Confirmada. `PrenotificationQueryService.Map` calcula siempre `AddBusinessDays(EffectiveEntryDate, 3)` y exige esa madurez para `CanBeUsedForDebit` (`PrenotificationQueryService.cs:53-64`). No toma clearing house ni regla efectiva. Esto contradice la necesidad de madurez contextual, especialmente CENIT, pero TPA0 no lo corrige.

### 7.2 `SlaDeadlineAtUtc` y fallback de aceptación tácita

Confirmado un acoplamiento semántico importante:

1. `BatchResolver` produce un campo explícitamente llamado `ReturnSlaDeadlineAtUtc` usando el quinto ciclo próximo (`BatchResolver.cs:205-218,248-264`).
2. `TransactionPersister` copia ese valor a `AchTransaction.SlaDeadlineAtUtc` para cualquier transacción, incluida prenotificación (`TransactionPersister.cs:80-93`).
3. `AchTacitAcceptanceJobHandler` usa exactamente `SlaDeadlineAtUtc` para decidir aceptación tácita de prenotificaciones; sólo si es null cae al fallback `EffectiveEntryDate.Date < utcNow.Date` (`AchTacitAcceptanceJobHandler.cs:36-42`).

Así, la fecha que gobierna tacit acceptance puede ser un SLA calculado con semántica de devoluciones, y el fallback es un cambio de día UTC, no una policy explícita por cámara/ciclo. No se corrigió.

### 7.3 Carencias del modelo legacy

Confirmadas: no hay entidad `ThirdParty` separada, no hay `ThirdPartyAccount`, no hay `AccountType` persistido en legacy ni en `CustomerAccount`, sólo existe una referencia de prenotificación y un resultado global final. Además `LinkPendingPrenotification` prohíbe volver a `Pending` si el status ya es final (`CustomerThirdParty.cs:28-43`), por lo que el modelo no puede representar un nuevo intento sin otro mecanismo.

### 7.4 Selector SPA global `Active`

Confirmado en `getActiveThirdParties`: siempre envía `status: 'Active'`, sin cámara/naturaleza/fecha efectiva (`transactions-api.service.ts:87-114`). Se agregó test de caracterización; no se cambió el selector.

### 7.5 ACH Colombia V32 vs V35

Confirmada metadata desactualizada: las reglas seed ACH Colombia referencian V32 (`RegulatoryCatalogSeeder.cs:558,576`), mientras `docs/normativa/THIRD_PARTY_ACCOUNT_NORMATIVE_TRACEABILITY.md` y el manual del proyecto establecen V35 como autoridad. No se actualizó metadata en TPA0.

### 7.6 Acoplamiento del onboarding a cámara

**No confirmado.** En single y bulk, `Customer/CustomerAccount` se asegura antes de `BatchResolver`, y `PrenotificationHandler` no recibe `ClearingHouseId` ni consulta `ClearingHouseTransactionRule`. No se encontró dependencia de ACH Colombia/CENIT/PPD/CCD/CTX dentro de esas rutinas de onboarding.

Sí existe acoplamiento legacy distinto: `CustomerThirdParty` sólo nace/relinkea desde una prenotificación y requiere resolver Customer por source account. Se registra separadamente como GAP-TPA-008; no debe trasladarse al diseño canónico.

## 8. Registro de gaps TPA0

| Gap | Estado | Evidencia |
|---|---|---|
| `GAP-TPA-001` | CONFIRMED | `CustomerThirdParty.cs:6-26`; `CustomerAccount.cs`; sin AccountType/master separado/historial multi-intento. |
| `GAP-TPA-002` | CONFIRMED | `PrenotificationQueryService.cs:53-64`: +3 business days global. |
| `GAP-TPA-003` | CONFIRMED | `BatchResolver.cs:218,248-264` crea Return SLA; `TransactionPersister.cs:92` lo persiste; `AchTacitAcceptanceJobHandler.cs:36-42` lo usa para prenotes. |
| `GAP-TPA-004` | CONFIRMED | SPA `transactions-api.service.ts:87-114` filtra global `status=Active`. |
| `GAP-TPA-005` | CONFIRMED | `CustomerThirdPartiesController.cs:21-54` es GET-only; no existe command surface de master. |
| `GAP-TPA-006` | CONFIRMED | Seeder ACH Colombia usa metadata V32 (`RegulatoryCatalogSeeder.cs:558,576`). |
| `GAP-TPA-007` | CLOSED_NOT_FOUND | No se encontró dependencia de clearing house en `EnsureCustomerAndAccountsAsync` single/bulk ni en `PrenotificationHandler`. |
| `GAP-TPA-008` | CONFIRMED | Legacy `CustomerThirdParty` se crea/relinkea sólo si `request.IsPrenotification` y sólo cuando source account resuelve Customer (`PrenotificationHandler.cs:24-38,62-74`). |
| `GAP-TPA-009` | CONFIRMED | Onboarding usa check-then-add con uniques, sin unique-conflict -> re-read; contraste con idempotencia explícita de `AchStateTransitionService.cs:120-139`. |
| `GAP-TPA-010` | CONFIRMED | Status final legacy impide `LinkPendingPrenotification`; no existe historial multi-intento (`CustomerThirdParty.cs:28-43,77-93`). |
| `GAP-TPA-011` | CONFIRMED | ACH Colombia V35 credit prenote es opcional pero si se usa exige 3 business days (`ACH-Colombia-V35.md:5058-5095`); seed credit tiene `lead=null` y policy retorna OK inmediatamente para cualquier regla no Mandatory (`TransactionPrerequisitePolicyService.cs:98-101`). |

## 9. Characterization tests agregados

Se agregaron, sin modificar producción:

### `PrenotificationHandlerTests`

- `HandleAsync_CreatesPendingThirdParty_WhenSourceCustomerExistsAndIdentityIsNew`
- `HandleAsync_ReusesPendingThirdParty_AndAddsOnlyOnceAcrossRetry`
- `HandleAsync_DoesNothing_WhenSourceCustomerCannotBeResolved`

Se conserva el test existente de `Active/Rejected` que confirma que un resultado definitivo no se resetea.

### `AchBulkTransactionServiceTests`

- `RegisterBulkAsync_Prenotification_OnboardsMissingCustomersAccountsAndThirdParty`
- `RegisterBulkAsync_Prenotification_ReusesExistingCustomersAccountsAndPendingThirdParty`

### SPA

- `getActiveThirdParties_ShouldKeepLegacyGlobalActiveSelector`

Cobertura relevante preexistente que TPA0 inventarió y mantiene:

- `CustomerThirdPartyGovernanceTests` — ausencia de endpoint manual y reglas de resultado automático;
- `DifferentialPrenotificationResponseProcessorTests` — `Active/Rejected`, correlación y duplicate response;
- `TransactionPrerequisitePolicyServiceTests` — ACH Colombia crédito/débito y CENIT crédito/débito por separado.

## 10. Evidencia de ejecución

### Ejecutado

```text
python scripts/validate-solution-projects.py
PASS — SOLUTION GRAPH: OK
```

También se ejecutó el guard byte-a-byte contra el ZIP fuente: `src`, migrations, normativa y SPA productiva quedaron sin diferencias.

### No ejecutado por ambiente

```text
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj ...
NOT_RUN_ENVIRONMENT — dotnet: command not found

docker --version
NOT_RUN_ENVIRONMENT — docker: command not found
```

Para SPA se intentó `npm ci`; expiró por timeout de instalación y no dejó `node_modules`. En consecuencia, el focal Angular/Karma **no se reporta como PASS** y queda `NOT_RUN_ENVIRONMENT_DEPENDENCY_INSTALL_TIMEOUT`.

Dado que la estrategia TPA exige no cerrar una fase sin evidencia obligatoria ejecutada, TPA0 queda `IMPLEMENTED_UNVERIFIED_ENVIRONMENT` y `NEXT_ALLOWED_PHASE=TPA0`. **No se ejecutó TPA1.**
