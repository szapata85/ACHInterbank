# SPA 4C.3B2a — Transaction Create Checkpoint

## Baseline y alcance

- Baseline obligatorio: `ACHInterbank-v35-spa-4C3B1b2-outgoing-monitoring-detail.zip`.
- Superficie migrada: `/transactions/create` (`TransactionCreateComponent`) únicamente.
- No se modificaron bulk, NACHA upload, returns/ROR, cycle-configs, clearing-houses, ach-cycles, outgoing-monitoring, transaction-list, integration-result ni backend.
- No se avanzó a 4C.3B2b.

## Archivos modificados

- `web/ach-interbank-ui/src/app/features/transactions/components/transaction-create/transaction-create.component.html`
- `web/ach-interbank-ui/src/app/features/transactions/components/transaction-create/transaction-create.component.scss`
- `web/ach-interbank-ui/src/app/features/transactions/components/transaction-create/transaction-create.component.spec.ts`
- `docs/ai/SPA-4C3B2A-TRANSACTION-CREATE-CHECKPOINT.md`

`transaction-create.component.ts` permanece byte-a-byte igual al baseline. Por tanto, la migración no reescribió lógica de formulario, catálogos, policy preview ni submit.

## Inventario real del componente

### Ruta y permiso

La ruta sigue siendo:

- `/transactions/create`
- `permissionGuard`
- permiso: `CanManageAch`

`transactions-routing.module.ts` permanece byte-a-byte igual al baseline.

### APIs y catálogos consumidos

El componente conserva exactamente estos consumidores reales:

- `CustomersApiService.getAll()` — catálogo de clientes originadores.
- `FinancialInstitutionsApiService.getAll()` — catálogo de entidades destino; la UI filtra únicamente `FinancialInstitutionStatusEnum.Active`.
- `TransactionsApiService.getCompanyEntryDescriptions()` — catálogo de conceptos/descripciones de entrada.
- `TransactionsApiService.getActiveThirdParties(sourceAccountNumber)` — cuentas destino/terceros activos dependientes de la cuenta origen; no se consulta para prenotificación.
- `TransactionsApiService.previewPolicy(payload)` — preview de regla operativa vigente cuando existen los campos mínimos necesarios.
- `TransactionsApiService.createTransaction(payload)` — creación individual.

No se cambiaron endpoints ni firmas. `TransactionsApiService` permanece byte-a-byte igual.

### Campos reales

El `FormGroup` actual conserva:

- `customerId` (selección local opcional; no forma parte de `TransactionDraft` enviado);
- `amount`;
- `transactionExternalId`;
- `type`;
- `accountType`;
- `isPrenotification`;
- `destinationInstitutionId`;
- `sourceAccountNumber`;
- `destinationAccountNumber`;
- `recipientIdNumber`;
- `recipientName`;
- `requiresIdentityValidation`;
- `companyName`;
- `companyIdentification`;
- `sourcePersonType`;
- `recipientPersonType`;
- `companyEntryDescriptionId`;
- `addendas[]` con `addendaType`, `collectorId`, `receiverCustomerCode`, `serviceDescription` e `information`.

Los autocompletes mantienen controles de búsqueda locales separados y sincronizan los IDs/valores funcionales existentes.

### Validaciones preservadas

Sin cambios en TypeScript:

- `amount`: required; positivo para operación monetaria; cero para prenotificación; máximo vigente y máximo dos decimales.
- `transactionExternalId`: required, máximo 64.
- `type` y `accountType`: required.
- `destinationInstitutionId`: required y mínimo 1.
- cuentas origen/destino: required, solo 6–18 dígitos; además deben ser diferentes.
- `recipientIdNumber`: máximo 20 y requerido según tipo/validación de identidad.
- `recipientName`: máximo 100 y requerido cuando existe identificación.
- `companyName`: required, máximo 16.
- `companyIdentification`: required, patrón `[A-Z0-9]{4,10}`.
- tipos de persona: required.
- `companyEntryDescriptionId`: required, mínimo 1.
- addenda por defecto tipo `05`; `information` required máximo 80.
- para débito/reverso, `collectorId`, `receiverCustomerCode` y `serviceDescription` conservan sus requerimientos y longitudes actuales.
- `recipientIdentityValidator()` permanece.
- un `policyPreview.canSubmit === false` sigue impidiendo el submit mediante la validación actual.
- los autocompletes obligatorios mantienen `realSelectionValidator` para impedir texto arbitrario no asociado a una opción real.

## Flujo de submit preservado

`submit()` permanece byte-a-byte igual:

1. evita doble envío cuando `isSubmitting` ya está activo;
2. si el formulario es inválido, marca controles, muestra el resumen y enfoca el primer error, sin llamar API;
3. parsea el monto formateado y vuelve a rechazar monto inválido/no positivo cuando aplica;
4. normaliza addendas con el helper existente;
5. construye `TransactionDraft` conservando conversiones actuales:
   - `type`/`accountType` a número/enums actuales;
   - `isPrenotification` y `requiresIdentityValidation` a boolean;
   - `destinationInstitutionId` y `companyEntryDescriptionId` a número;
   - `transactionExternalId.trim()`;
   - cuentas solo con dígitos y máximo 18;
   - `recipientIdNumber`/`recipientName` con trim o `undefined`;
   - `companyName.trim()`;
   - `companyIdentification.trim().toUpperCase()`;
   - valores `PN/PJ` actuales;
   - addendas con trim y `addendaType.toUpperCase()`.
6. llama únicamente `createTransaction(sanitized)`.

Éxito preservado:

- conserva `createdResponse`;
- mensaje/notificación de éxito;
- limpia/restaura formulario y addenda por defecto;
- limpia catálogos de terceros/preview local;
- restablece autocompletes;
- navega a `/transactions`.

Error preservado:

- desactiva `isSubmitting`;
- conserva los datos del formulario;
- usa el `Error.message` entregado por el service o el fallback existente;
- notifica el error;
- no hay retry automático.

### Idempotencia, versionamiento y concurrencia

El baseline no implementa en esta superficie un `commandId`, header/key de idempotencia separado, `expectedVersion` ni tratamiento HTTP 409 específico. `transactionExternalId` se conserva como identificador funcional y participa en el preview/payload exactamente como antes. `TransactionPolicyPreview` puede contener `idempotencyKey`, pero `TransactionCreateComponent` no lo utiliza como comando/versión de submit. No se añadió ningún mecanismo nuevo.

## Migración foundation 4C.1

La lógica TS no se modificó. Solo se migró presentación local:

- encabezado legacy local -> `app-page-header`;
- estado de carga del catálogo -> `UiEstadoCargaComponent`;
- error visible -> `UiEstadoErrorComponent` sin introducir retry inexistente;
- el vínculo de cada error del resumen usa `mat-button` manteniendo el mismo `focusIssue()`;
- controles de formulario, autocompletes, selects, checkboxes y acciones continúan usando Angular Material;
- cero `<ui-boton>` en la superficie;
- estilos locales de colores hardcoded fueron sustituidos por tokens compartidos (`--color-*`, `color-mix`) sin cambiar layout funcional.

No se creó ningún primitive shared nuevo.

## Boundaries deliberadamente no resueltos

No se movieron ni rediseñaron dependencias existentes. En particular:

- `FinancialInstitutionsApiService` y `DestinationInstitution` permanecen en `transactions`;
- `CustomersApiService` y `CustomerSummary` siguen siendo imports existentes desde `features/customers`;
- no se introdujo `AchCyclesApiService` ni ningún import feature -> feature nuevo.

El cierre de boundaries identificado en 4C.3B0 permanece reservado para B9.

## Pruebas focales

Se mantuvieron y ampliaron las pruebas existentes de `transaction-create.component.spec.ts`. Cubren, entre otros:

- carga de clientes, entidades y descripciones de entrada;
- default de débito y addenda `05`;
- selección/autocomplete y catálogos dependientes;
- validaciones obligatorias, cruzadas y condicionales;
- construcción exacta del request normalizado;
- submit inválido sin llamada a `createTransaction`;
- bloqueo de doble submit;
- éxito, reset y notificación;
- error backend conservando datos;
- prenotificación con monto cero;
- foundation 4C.1 (`app-page-header`, shared loading/error, controles Material);
- cero `ui-boton`.

No se agregaron tests para commandId/expectedVersion/409 porque esas capacidades no existen en el baseline de esta superficie.

## Validación ejecutable real

Entorno:

- Node: `v22.16.0`.
- npm: `10.9.2`.
- `node_modules/.bin/ng`: ausente.

Siguiendo el bloqueo heredado, se hizo una sola comprobación corta del registry y no se ejecutó `npm ci` repetidamente:

- Comando: `timeout 12s npm ping`
- Exit code: `124`
- Causa: `registry.npmjs.org` no respondió dentro del timeout (`npm notice PING https://registry.npmjs.org/`).

Intentos reales:

1. `npm test -- --watch=false --browsers=ChromeHeadless --include=src/app/features/transactions/components/transaction-create/transaction-create.component.spec.ts`
   - Exit code: `127`
   - Causa: `ng: not found`.
2. `npm run build`
   - Exit code: `127`
   - Causa: `ng: not found`.

No se ejecutó backend.

Como evidencia complementaria, TypeScript global `5.8.3` ejecutó una comprobación sintáctica con `transpileModule(..., reportDiagnostics=true)` sobre `transaction-create.component.ts` y su spec: `0` diagnostics sintácticos. Esto no sustituye Angular build/tests.

## Validación estática contra v35

Antes de crear este checkpoint, el diff funcional contra v35 contenía únicamente:

- `transaction-create.component.html`;
- `transaction-create.component.scss`;
- `transaction-create.component.spec.ts`.

Confirmaciones:

- `transaction-create.component.ts`: byte-a-byte intacto.
- B1a transaction-list: 4 archivos byte-a-byte intactos.
- B1a integration-result: 4 archivos byte-a-byte intactos.
- B1b outgoing-monitoring list/detail y service/models: 11 archivos byte-a-byte intactos.
- `TransactionsApiService`: byte-a-byte intacto.
- `transactions.models.ts`: byte-a-byte intacto.
- `transactions.types.ts`: byte-a-byte intacto.
- `transactions-routing.module.ts`: byte-a-byte intacto; `/transactions/create` y `CanManageAch` permanecen.
- transaction bulk-create: 4 archivos byte-a-byte intactos.
- bulk-ingestion upload/tracking/detail: 12 archivos byte-a-byte intactos.
- NACHA upload: 4 archivos byte-a-byte intactos.
- returns: 4 archivos byte-a-byte intactos.
- return-of-return: 4 archivos byte-a-byte intactos.
- cycle-config: 4 archivos byte-a-byte intactos.
- `features/clearing-houses`: 17 archivos byte-a-byte intactos.
- `features/ach-cycles`: 18 archivos byte-a-byte intactos.
- backend `src/`: 1475 archivos byte-a-byte intactos.
- cero imports feature -> feature nuevos: el TS del componente no cambió.
- cero hardcoded hex/rgb en los archivos funcionales objetivo después de la migración.
- cero `<ui-boton>` en el template de create.

## Estado

`TRANSACTION_CREATE_4C3B2A_IMPLEMENTED_VALIDATION_PENDING`

La implementación visual y la validación estática están completas. No existe evidencia Angular ejecutable suficiente porque `ng` no está instalado y el registry npm siguió sin responder en la única comprobación permitida.
