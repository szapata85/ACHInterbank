# TPA7 — Integración transaccional y selector canónico (TPA7A + TPA7B)

## Alcance

TPA7A integra la elegibilidad contextual de `ThirdPartyAccount` en los caminos de creación **SINGLE** y **BULK** sin cambiar SPA, sin cutover de lecturas, sin backfill y sin retirar `CustomerThirdParty`.

Paquete de trabajo de esta fase: `ACHInterbank-R12-TPA6-ADMIN-UX.zip`.

La secuencia de autoridad queda:

```text
input
  -> onboarding genérico Customer -> ThirdParty -> ThirdPartyAccount
  -> routing/batch (cámara + fecha efectiva + service type)
  -> política efectiva ClearingHouseTransactionRule
  -> elegibilidad contextual IThirdPartyAccountEligibilityService
  -> prerequisito ITransactionPrerequisitePolicyService
  -> persistencia AchTransaction
```

La identidad maestra continúa siendo agnóstica de cámara. La cámara aparece únicamente después del onboarding, una vez `BatchResolver` resolvió el contexto operativo.

## SINGLE

`AchTransactionService` mantiene el onboarding silencioso existente y, después de `BatchResolver.ResolveAsync`, ejecuta el guard server-side antes de clasificación/persistencia:

1. resuelve naturaleza `Credit`/`Debit`;
2. resuelve la política efectiva para `ClearingHouseId + naturaleza + EffectiveEntryDate`;
3. si existe `ThirdPartyAccount` canónica, evalúa elegibilidad con `ThirdPartyAccountId + cámara + naturaleza + fecha efectiva + SourceServiceClassCode`;
4. valida el resultado con `ValidateForRegistration`;
5. sólo si el prerequisito es válido continúa hacia clasificación y `TransactionPersister`.

Las prenotificaciones no requieren una validación previa para poder crearse; su propio registro sigue alimentando el lifecycle canónico ya implementado.

## BULK

`AchBulkTransactionService` usa la misma secuencia por ítem y los mismos servicios de autoridad. Un fallo de elegibilidad/prerequisito se captura con la semántica preexistente del lote como `ITEM_VALIDATION_FAILED`, y el ítem no llega a `TransactionPersister`.

No se introdujo un segundo modelo de reglas para BULK ni una variante de onboarding dependiente de cámara. El lookup legacy de `IAchRegulatoryCatalogService` se conserva como compatibilidad/observabilidad, pero en BULK deja de bloquear por sí solo una transacción antes de conocer la cámara; la autoridad de prenotificación es la policy efectiva resuelta después de routing, igual que en SINGLE.

## Semántica de prerequisito

`TransactionPrerequisitePolicyService.ValidateForRegistration` es el punto común que convierte política efectiva + elegibilidad contextual en una decisión de creación.

| Caso | Resultado |
| --- | --- |
| Prenotificación | Permitida; no exige validación previa |
| Sin política efectiva | Bloquea (`TRANSACTION_POLICY_NOT_CONFIGURED`) |
| Política `Mandatory` sin identidad/elegibilidad canónica | Bloquea cerrado (`TRANSACTION_ELIGIBILITY_REQUIRED`) |
| Política no obligatoria sin identidad canónica suficiente | Permite; no inventa una prenotificación obligatoria |
| Elegibilidad calculada con otra versión de policy | Bloquea (`TRANSACTION_ELIGIBILITY_POLICY_MISMATCH`) |
| `Allowed` | Permite |
| `AllowedWithoutPrenotification` con policy no obligatoria | Permite |
| `RequiresPrenotification` / `PrenotificationPending` / `ValidationExpired` / `Rejected` / `RequiresRevalidation` / `TechnicalReview` / `Blocked` / `Unsupported` | Bloquea antes de persistencia |

Esto preserva `INV-TPA-006`: el débito falla cerrado cuando la policy exige validación, mientras un crédito con prenotificación opcional no se convierte en obligatorio.

Cuando una prenotificación opcional sí fue usada, la elegibilidad contextual existente sigue siendo la autoridad para sus plazos efectivos; TPA7A no duplica ese cálculo.

## Evidencia de uso monetario

Cuando una transacción monetaria fue autorizada por una elegibilidad que referencia una `ThirdPartyAccountValidation`, SINGLE y BULK llaman `IThirdPartyAccountValidationService.RecordMonetaryUseAsync` dentro de la misma unidad transaccional.

La actualización:

- exige coincidencia entre `ValidationId` y `ThirdPartyAccountId`;
- almacena `UsedAtUtc` en UTC;
- sólo avanza `LastMonetaryUseAtUtc`, nunca lo retrocede;
- no crea una validación nueva ni reasigna evidencia entre cuentas.

Con esto el cálculo de retención contextual (por ejemplo, políticas que dependen del último uso monetario) ya dispone de evidencia escrita desde los caminos monetarios canónicos.

## Compatibilidad legacy

TPA7A no elimina ni corta `CustomerThirdParty`.

- el onboarding `Customer -> ThirdParty -> ThirdPartyAccount` sigue compartido por SINGLE/BULK;
- `PrenotificationHandler` y la compatibilidad legacy permanecen en su lugar;
- no hay backfill histórico;
- no hay migración de esquema;
- no hay cambio de SPA ni del selector legacy;
- no hay cutover global de lectura.

Cuando el ingress legado no aporta identidad suficiente para crear una cuenta canónica, una policy obligatoria falla cerrada; una policy opcional no transforma esa ausencia en obligación normativa. `GAP-TPA-013` permanece abierto para resolver explícitamente el tipo documental de receptor en ingress.

## Tests focales

Se añadieron coberturas para:

- prerequisito obligatorio sin elegibilidad canónica: fail closed;
- policy opcional sin elegibilidad canónica: no inventa prenotificación obligatoria;
- estado contextual pendiente: bloquea;
- SINGLE: débito obligatorio bloqueado antes de persistencia;
- SINGLE: crédito opcional sin prenotificación permitido;
- SINGLE: evidencia `LastMonetaryUseAtUtc` invocada cuando una validación autoriza;
- BULK: mismos tres comportamientos;
- lifecycle: `RecordMonetaryUseAsync` actualiza únicamente la validación perteneciente a la cuenta canónica.

`python scripts/validate-solution-projects.py` y el guard estático TPA7A (sin SPA/migrations; orden onboarding -> routing -> prerequisite -> persistencia) se ejecutaron con resultado `PASS`.

El runtime .NET no está instalado en el entorno de trabajo, por lo que `dotnet test`/`dotnet build` quedan registrados como `NOT_RUN_ENVIRONMENT`.

## Fuera de alcance

TPA7A no ejecuta TPA7B ni TPA8. En particular no modifica Angular/SPA, no cambia el selector de cuentas, no hace cutover, no ejecuta backfill y no retira estructuras legacy.

# TPA7B — Selector transaccional canónico contextual

## Baseline y alcance

TPA7B parte exclusivamente de `ACHInterbank-R13-TPA7A-TRANSACTION-ELIGIBILITY.zip`. Esta fase cambia el **selector de cuenta destino de creación de transacciones** para dejar de consultar el estado global `CustomerThirdParty.Status = Active` y consumir una lectura canónica contextual.

No cambia el onboarding silencioso, no modifica el guard de registro server-side de TPA7A, no elimina `CustomerThirdParty`, no hace backfill, no realiza cutover global de lecturas y no ejecuta TPA7C ni TPA8.

## Query contextual de backend

El nuevo endpoint `GET /api/transactions/third-party-accounts/selector` recibe sólo el contexto que ya existe en el formulario de transacción:

- `sourceAccountNumber`;
- `destinationInstitutionId`;
- `type` (`Credit`/`Debit`);
- `companyEntryDescriptionId`.

`TransactionThirdPartyAccountSelectorService` resuelve en backend:

```text
sourceAccountNumber
  -> Customer dueño de la cuenta origen
  -> institución destino activa
  -> IRoutingStrategyService
  -> ciclo/cámara resueltos
  -> ProcessingDate como effectiveDate
  -> CompanyEntryDescriptionCatalog.StandardEntryClassCode como serviceType
  -> ThirdPartyAccount canónicas por Customer + institución destino
  -> IThirdPartyAccountEligibilityService por cuenta
  -> EligibilityDecision + code + message + isSelectable
```

La lectura usa la misma autoridad de routing que `BatchResolver`, pero **no invoca `BatchResolver`** porque un selector de lectura no debe crear/reutilizar `AchBatch` ni mutar estado transaccional. El contexto efectivo de cámara y fecha sale del ciclo resuelto por `IRoutingStrategyService`, y el `serviceType` sale del mismo catálogo usado por el registro transaccional.

La consulta de cuentas se hace sobre `ThirdPartyAccount`/`ThirdParty` canónicos. No consulta `CustomerThirdParties` ni su `Status` para decidir qué opciones mostrar o habilitar.

## Decisión de selección

El backend devuelve también cuentas no elegibles para que la SPA pueda explicar el estado contextual. `isSelectable` se deriva exclusivamente de `EligibilityDecision`:

| EligibilityDecision | Seleccionable |
| --- | --- |
| `Allowed` | Sí |
| `AllowedWithoutPrenotification` | Sí |
| cualquier otra decisión contextual | No |

Con esto, un crédito cuya policy hace la prenotificación opcional sigue siendo seleccionable cuando la elegibilidad devuelve `AllowedWithoutPrenotification`. La SPA no traduce ni reinterpreta esa policy.

Una cuenta administrativamente inactiva, sin policy efectiva, pendiente, rechazada, expirada, bloqueada o que requiera revalidación puede aparecer con el `eligibilityCode`/`eligibilityMessage` generado por backend, pero queda deshabilitada para selección.

## SPA

`TransactionsApiService` deja de llamar `api/customer-third-parties` con `status=Active`. En su lugar llama el endpoint contextual y pasa Customer implícito por la cuenta origen, institución destino, tipo de transacción y descripción de entrada.

`TransactionCreateComponent`:

- recarga el selector cuando cambia cuenta origen, institución destino, naturaleza o descripción de entrada;
- no filtra elegibilidad por `Active`, cámara, prenotificación, SLA, ciclos ni policy en Angular;
- muestra el `eligibilityCode` y `eligibilityMessage` recibidos del backend;
- deshabilita opciones únicamente con `isSelectable` recibido del backend;
- usa los datos canónicos devueltos para cuenta, tipo de cuenta e identidad del receptor;
- conserva la entrada manual de cuenta para prenotificaciones como existía antes de TPA7B.

La lectura del selector es ayuda de UX, no una autorización final. Al enviar la transacción, el pipeline TPA7A vuelve a resolver routing/policy/elegibilidad/prerequisito server-side antes de persistir. Una respuesta antigua del selector no puede saltarse ese guard.

## Compatibilidad legacy y cierre de GAP-TPA-004

`CustomerThirdParty` permanece operativo para los flujos legacy que todavía lo requieren, pero el selector de creación de transacciones ya no depende de `CustomerThirdParty.Status = Active`.

La cobertura focal crea explícitamente un `CustomerThirdParty` legacy en estado `Active` y fuerza `EligibilityDecision.Rejected` para la cuenta canónica equivalente. El selector devuelve la cuenta como no seleccionable y conserva el motivo de rechazo canónico. Esto demuestra que `Active` legacy no autoriza el nuevo selector.

Por lo tanto, `GAP-TPA-004` queda `CLOSED_TPA7B` para el selector transaccional. Este cierre no implica eliminación de la tabla/clase legacy ni un cutover global de otras lecturas.

## Tests focales TPA7B

Se añadieron coberturas para:

- routing contextual separado de ACH Colombia y CENIT y propagación de cámara/effective date/service type hacia elegibilidad;
- naturaleza `Credit` y `Debit` y preservación de `AllowedWithoutPrenotification` para crédito opcional;
- cuenta canónica rechazada aunque exista `CustomerThirdParty` legacy `Active`;
- forwarding del endpoint de Transactions hacia el query contextual;
- `TransactionsApiService` sin `status=Active` ni endpoint legacy;
- SPA consumiendo `isSelectable`, código y mensaje de backend sin recalcular reglas normativas.

`python scripts/validate-solution-projects.py` y el guard estático TPA7B pasan. El runtime .NET no está disponible (`NOT_RUN_ENVIRONMENT`). El ZIP tampoco contiene `web/ach-interbank-ui/node_modules`, por lo que los tests/build Angular quedan `NOT_RUN_ENVIRONMENT`; se ejecuta un guard de sintaxis TypeScript sobre los archivos tocados.

## Fuera de alcance

TPA7B no modifica SINGLE/BULK onboarding, no reemplaza la autoridad server-side de TPA7A, no elimina `CustomerThirdParty`, no agrega migraciones, no hace backfill, no realiza cutover global y no ejecuta TPA7C ni TPA8.

# TPA7C — Cierre de paridad selector/registro

## Baseline y resultado

TPA7C parte exclusivamente de `ACHInterbank-R14-TPA7B-CANONICAL-SELECTOR.zip` y revisa la coherencia entre el selector contextual de TPA7B y los guards server-side de registro de TPA7A. No se requiere refactor ni cambio de producción: los dos lados ya consumen la misma tupla normativa de elegibilidad y el registro conserva la autoridad final.

TPA7C no ejecuta TPA8, no hace backfill, no agrega migraciones, no elimina `CustomerThirdParty` y no realiza cutover global.

## Paridad de contexto selector -> registro

La elegibilidad canónica se evalúa con la misma semántica contextual en los tres caminos:

| Contexto | Selector TPA7B | SINGLE TPA7A | BULK TPA7A |
| --- | --- | --- | --- |
| cámara | ciclo resuelto por `IRoutingStrategyService` | `BatchResolver.ClearingHouseId` | `BatchResolver.ClearingHouseId` |
| naturaleza | `TransactionPrerequisitePolicyService.ResolveNature(type)` | mismo `ResolveNature(type)` | mismo `ResolveNature(type)` |
| `serviceType` | `CompanyEntryDescriptionCatalog.StandardEntryClassCode` | `TransactionBatchContext.SourceServiceClassCode` | `TransactionBatchContext.SourceServiceClassCode` |
| `effectiveDate` | `AchCycle.ProcessingDate` del ciclo resuelto | `TransactionBatchContext.EffectiveEntryDate` | `TransactionBatchContext.EffectiveEntryDate` |
| autoridad | `IThirdPartyAccountEligibilityService` | misma interfaz + `ITransactionPrerequisitePolicyService` | misma interfaz + `ITransactionPrerequisitePolicyService` |

El selector es una lectura de UX y no congela autorización. Al confirmar una operación, SINGLE/BULK vuelven a resolver routing, policy efectiva y elegibilidad antes de persistir. Por eso un cambio de ciclo/policy entre lectura y submit puede volver no elegible una opción que antes era seleccionable, y el backend la rechaza antes de persistencia.

## Semántica preservada

- `AllowedWithoutPrenotification` continúa seleccionable y la policy opcional no se convierte en obligatoria.
- Una policy obligatoria de débito falla cerrada si la elegibilidad no demuestra el prerequisito.
- SINGLE y BULK ejecutan onboarding canónico genérico antes de routing y usan las mismas autoridades posteriores.
- El onboarding `Customer -> ThirdParty -> ThirdPartyAccount` no recibe cámara, naturaleza, `serviceType` ni fecha efectiva como dimensiones de identidad.
- `CustomerThirdParty` permanece sólo por compatibilidad; el selector contextual no consulta `CustomerThirdParty.Status`, `status=Active` ni el endpoint legacy para autorizar cuentas.
- La SPA sólo transmite contexto, muestra `eligibilityCode`/`eligibilityMessage` y respeta `isSelectable`; no calcula policy, SLA, ciclos ni decisiones regulatorias.

## Cobertura focal de cierre

La cobertura de TPA7B ya demuestra para el selector ACH Colombia/CENIT la propagación exacta de cámara, naturaleza, fecha efectiva y `serviceType`, además de crédito/débito, cuenta rechazada y ausencia de autoridad de `Active` legacy.

TPA7C refuerza las pruebas de registro existentes para verificar explícitamente que SINGLE y BULK envían a `IThirdPartyAccountEligibilityService` la misma tupla contextual (`ClearingHouseId`, `TransactionNature`, `EffectiveDate`, `ServiceType = PPD`) tanto en débito obligatorio como en crédito opcional. No se modifica código de producción.

`python scripts/validate-solution-projects.py` y el guard estático focal TPA7C pasan. El runtime .NET no está disponible, por lo que `dotnet test`/`dotnet build` quedan `NOT_RUN_ENVIRONMENT`. Las dependencias Angular tampoco están instaladas; no se cambia producción frontend en TPA7C.

## Cierre de TPA7

TPA7A + TPA7B + TPA7C quedan coherentes: selector y registro comparten contexto, el backend conserva autoridad final, SINGLE/BULK convergen y las reglas de opcionalidad/fail-closed permanecen intactas. `GAP-TPA-013`, `GAP-TPA-015` y `GAP-TPA-016` permanecen abiertos sin intento de resolución incidental.

`TPA7=CLOSED` y `NEXT_ALLOWED_PHASE=TPA8`. TPA8 no se ejecuta en este incremento.

