# SPA 4C.3B2b1 — Transaction Bulk Create Checkpoint

## Baseline

Baseline obligatorio aplicado:

`ACHInterbank-v36-spa-4C3B2a-transaction-create.zip`

Alcance implementado únicamente sobre:

`web/ach-interbank-ui/src/app/features/transactions/components/transaction-bulk-create`

No se avanzó a bulk-ingestion upload/tracking/detail ni a ningún otro micro-lote.

## Superficie real encontrada

Ruta actual:

`/transactions/bulk-create`

Componente:

`TransactionBulkCreateComponent`

Permiso conservado:

`CanManageAch`

La superficie real no es un editor de filas con alta/baja manual. Es una creación masiva por **payload JSON inline**. El usuario pega un objeto JSON con `batchReference`, `chunkSize` y `transactions`, valida localmente y luego ejecuta un submit síncrono. No se inventaron controles de agregar/quitar filas porque esa capacidad no existe en el baseline.

Tampoco existen cargas iniciales de catálogos en este componente. No se añadieron catálogos ni dependencias nuevas.

## API real preservada

Servicio consumido:

`BulkIngestionApiService`

Método:

`submit(request)`

Endpoint encapsulado por el service, sin cambios:

`POST transactions/bulk/submit`

`BulkIngestionApiService` permanece byte-a-byte igual al baseline.

El submit conserva exactamente:

- `sourceType = BulkIngestionSourceType.InlineTransactions`;
- `processingMode = BulkIngestionProcessingMode.Synchronous`;
- `batchReference` validado/normalizado;
- `chunkSize` validado/normalizado;
- `transactions` validadas localmente.

No se modificaron `TransactionsApiService`, `transactions.models.ts` ni `transactions.types.ts`.

## Campos y estructura reales

El JSON utiliza el contrato existente `BulkAchTransactionRequest`:

- `batchReference`;
- `chunkSize`;
- `transactions`.

Cada item conserva el contrato `BulkAchTransactionItemRequest`, que incluye entre otros:

- `amount`;
- `transactionExternalId`;
- `reference`;
- `type`;
- `accountType`;
- `isPrenotification`;
- `destinationInstitutionId`;
- `sourceAccountNumber`;
- `destinationAccountNumber`;
- `companyName`;
- `companyIdentification`;
- `companyEntryDescriptionId`;
- tipos de persona opcionales;
- identificación/nombre receptor opcionales;
- `requiresIdentityValidation` opcional;
- `addendas` opcionales.

No se agregaron, eliminaron ni reinterpretaron campos.

## Validación y normalización preservadas

La lógica TypeScript funcional quedó intacta salvo imports Material requeridos por el template.

Se conserva:

- JSON vacío -> error;
- JSON inválido -> error;
- raíz distinta de objeto -> error;
- ausencia de `transactions` o lista vacía -> error;
- `batchReference` obligatorio;
- fallback de `batchReference` al valor local `BATCH-YYYYMMDD-001` cuando no viene en el JSON;
- `chunkSize` por defecto `200`;
- rango recomendado/aceptado `50-1000`;
- valores fuera del rango -> warning y uso de `200`;
- validación estructural por item existente;
- presencia de `transactionExternalId` o `reference` como llave operativa;
- trim de `transactionExternalId`;
- trim de `reference`;
- warning cuando se usa únicamente `reference` legado;
- addendas y demás propiedades se preservan sin transformaciones nuevas.

La validación estructural existente sigue comprobando únicamente los campos que ya verificaba el baseline. No se endureció ni amplió para “corregir” el contrato desde frontend.

## Submit y resultados preservados

`submit()` conserva:

1. prevención de doble submit mediante `isSubmitting`;
2. rechazo local si no existe payload previamente validado;
3. uso de `BulkIngestionApiService.submit()`;
4. processing mode síncrono;
5. ausencia de retry automático;
6. ausencia de navegación automática en éxito.

Cuando existe `immediateResult`:

- se conserva el response completo;
- los resultados por item siguen ordenándose por `index`;
- se mantienen `totalReceived`, `totalProcessed`, `totalSucceeded`, `totalFailed`;
- éxito total -> notificación success;
- éxito parcial -> warning con éxitos/fallos;
- fallo total -> notificación error;
- `formatError()` conserva `errorMessage`, luego `errorCode`, luego `Sin detalle`.

Si el backend acepta la solicitud sin `immediateResult`, se mantiene el warning actual:

`La plataforma aceptó la solicitud pero no entregó resultado síncrono.`

Los errores del service siguen mostrándose con `error.message` y fallback local. No se alteró la semántica de 400/401/general encapsulada por `BulkIngestionApiService`.

La única navegación existente sigue siendo Cancelar -> `/transactions/list`.

## Foundation 4C.1

Se mantuvo `app-page-header`, que ya existía en el baseline.

La migración visual local realizó:

- textarea nativo -> `MatFormField` + `textarea matInput`;
- acciones nativas -> `MatButtonModule` (`mat-stroked-button`, `mat-flat-button`, `mat-button`);
- estado de submit -> `UiEstadoCargaComponent`;
- error visible -> `UiEstadoErrorComponent` sin botón de retry inexistente;
- preview vacío -> `UiEstadoVacioComponent`;
- `UiGrillaEmpresarial` se conserva para resultados por item.

El baseline ya tenía **0** usos de `<ui-boton>` en esta superficie. El lote mantiene **0** y sustituye los botones HTML nativos por Material.

No se crearon primitives shared nuevos.

## CSS local

Se retiraron estilos locales que quedaron sustituidos por Material/shared states y se eliminaron selectores muertos asociados a una tabla HTML/badges que el template actual no consume.

Los hardcoded hex/rgb del SCSS objetivo fueron reemplazados por tokens foundation existentes:

- `--color-success*`;
- `--color-warning*`;
- `--color-error*`;
- `--color-border`;
- `--color-text`;
- `color-mix(...)` cuando aplica.

Resultado: cero hardcoded hex/rgb en TS/HTML/SCSS funcionales de la superficie.

## Boundaries

No se resolvió deuda cross-feature de B0/B9.

Este componente no añadió ningún import `feature -> feature` nuevo. Sus dependencias funcionales continúan limitadas al feature `transactions`, `core`, `shared`, Angular Material y AG Grid.

No se modificaron:

- `FinancialInstitutionsApiService`;
- `DestinationInstitution`;
- `AchCyclesApiService`;
- clearing-houses;
- ach-cycles.

## Pruebas focales actualizadas

`transaction-bulk-create.component.spec.ts` fue ampliado para cubrir el comportamiento real:

- estado inicial sin llamadas API ni carga de catálogos inexistente;
- JSON inválido;
- fila estructuralmente inválida;
- normalización de `batchReference`;
- fallback de `chunkSize` y warning;
- trim de `transactionExternalId`/`reference`;
- conservación de addendas;
- referencia legado transicional;
- submit sin payload validado no llama API;
- request exacto con `InlineTransactions` + `Synchronous`;
- prevención de doble submit;
- éxito total;
- orden de resultados por `index`;
- éxito parcial y detalle por item;
- respuesta sin `immediateResult`;
- error del API;
- primitives foundation;
- cero `ui-boton`.

No se agregaron tests de agregar/quitar filas porque el componente real no posee ese flujo.

## Validación ejecutable real

Entorno detectado:

- Node: `v22.16.0`;
- npm: `10.9.2`;
- TypeScript global: `5.8.3`;
- `node_modules/.bin/ng`: ausente.

Se hizo una sola comprobación corta del registry, sin repetir `npm ci`:

- comando: `timeout 12s npm ping`;
- exit code: `124`;
- causa: `registry.npmjs.org` no respondió dentro del timeout; la salida quedó en `npm notice PING https://registry.npmjs.org/`.

Intentos reales:

1. `npm test -- --watch=false --browsers=ChromeHeadless --include=src/app/features/transactions/components/transaction-bulk-create/transaction-bulk-create.component.spec.ts`
   - exit code: `127`;
   - causa: `ng: not found`.
2. `npm run build`
   - exit code: `127`;
   - causa: `ng: not found`.

No se ejecutó backend.

Validación sintáctica complementaria con TypeScript global `5.8.3` y `transpileModule(..., reportDiagnostics=true)`:

- `transaction-bulk-create.component.ts`: `0` diagnostics sintácticos;
- `transaction-bulk-create.component.spec.ts`: `0` diagnostics sintácticos.

Esta comprobación no sustituye Angular test/build.

## Validación estática contra v36

PASS.

Se confirmó byte-a-byte sin cambios:

- B1a `transaction-list`: 4 archivos;
- B1a `transaction-integration-result`: 4 archivos;
- B1b outgoing monitoring list/detail/service/models: 11 archivos;
- B2a `transaction-create`: 4 archivos;
- bulk-ingestion upload: 4 archivos;
- bulk-ingestion tracking: 4 archivos;
- bulk-ingestion detail: 4 archivos;
- NACHA upload: 4 archivos;
- returns: 4 archivos;
- return-of-return: 4 archivos;
- cycle-config: 4 archivos;
- `TransactionsApiService`;
- `BulkIngestionApiService`;
- `transactions.models.ts`;
- `transactions.types.ts`;
- `transactions-routing.module.ts` y, por tanto, ruta/permisos;
- `features/clearing-houses`: 17 archivos;
- `features/ach-cycles`: 18 archivos;
- backend `src/`: 1475 archivos.

La ruta permanece:

`/transactions/bulk-create`

con:

`CanManageAch`

Validaciones adicionales:

- cero `<ui-boton>` en el template objetivo;
- cero nuevos imports cross-feature;
- cero hardcoded hex/rgb en los archivos funcionales objetivo;
- `sourceType = InlineTransactions` intacto;
- `processingMode = Synchronous` intacto;
- doble-submit guard intacto;
- contratos y endpoint intactos.

Antes de crear este checkpoint, el diff funcional contra v36 estaba limitado a los cuatro archivos del `TransactionBulkCreateComponent`.

## Estado

`TRANSACTION_BULK_CREATE_4C3B2B1_IMPLEMENTED_VALIDATION_PENDING`

La implementación visual y la validación estática están completas. Angular tests/build no pudieron ejecutarse por ausencia de `ng` y registry npm no disponible en la única comprobación permitida.
