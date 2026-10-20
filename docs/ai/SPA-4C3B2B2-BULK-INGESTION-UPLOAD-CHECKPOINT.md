# SPA 4C.3B2b2 — Bulk Ingestion Upload Checkpoint

## Baseline

Baseline obligatorio usado:

`ACHInterbank-v37-spa-4C3B2b1-transaction-bulk-create.zip`

Este micro-lote modifica únicamente la superficie frontend de upload:

`/transactions/bulk-ingestion/upload`

No se avanzó a tracking/detail, NACHA upload, returns, cycle-configs, clearing-houses, ach-cycles ni backend.

Estado del lote:

`BULK_INGESTION_UPLOAD_4C3B2B2_IMPLEMENTED_VALIDATION_PENDING`

## Implementación real inspeccionada

### Componente y permiso

- Componente: `BulkIngestionUploadComponent`.
- Ruta: `/transactions/bulk-ingestion/upload`.
- Guard: `permissionGuard`.
- Permiso declarado: `CanManageAch`.
- La ruta y el permiso permanecen byte-a-byte iguales al baseline.

### API/service realmente usado

El upload **no usa** `BulkIngestionApiService`. La implementación real usa:

`BulkIngestionTrackingApiService.upload(file, batchReference?, clientRequestId?)`

El service conserva exactamente:

- `FormData` multipart;
- campo `file` obligatorio;
- campo `batchReference` opcional, enviado solo cuando `trim()` no queda vacío;
- campo `clientRequestId` opcional, enviado solo cuando `trim()` no queda vacío;
- endpoint `POST transactions/bulk-ingestion/upload`;
- response `BulkFileUploadResponse`;
- manejo de errores existente en `mapError()`.

Tanto `BulkIngestionTrackingApiService` como `BulkIngestionApiService` permanecen byte-a-byte iguales al baseline. No se modificaron contratos ni `transactions.models.ts`/`transactions.types.ts`.

### Archivo y validaciones

Se preservó exactamente la selección nativa de archivo. No se agregó drag-and-drop ni parsing frontend.

Formatos aceptados:

- `.json`
- `.csv`
- `.xlsx`
- `.xls`

`acceptedFormats` permanece como:

`.json,.csv,.xlsx,.xls`

Validación local preservada:

- archivo obligatorio;
- extensión evaluada sin distinguir mayúsculas/minúsculas;
- tamaño mayor que `0`;
- tamaño máximo `20 * 1024 * 1024` bytes (20 MB);
- archivo inválido no llama al API.

No se cambió ningún formato admitido ni se movió procesamiento del backend al frontend.

### Campos/formulario

`metadataForm` conserva únicamente:

- `batchReference` opcional;
- `clientRequestId` opcional.

No se añadieron validadores ni campos. El componente continúa pasando los valores crudos al service; el service existente conserva el `trim()` antes de agregarlos a `FormData`.

`clientRequestId` continúa siendo el único campo opcional de correlación/idempotencia expuesto por esta pantalla. No se genera una llave nueva en frontend.

## Submit preservado

`upload()` conserva:

1. prevención de doble submit mediante `isSubmitting`;
2. archivo requerido;
3. validación de extensión/tamaño antes de llamar backend;
4. limpieza de `errorMessage` al iniciar;
5. `take(1)`;
6. `finalize(() => isSubmitting.set(false))`;
7. llamada exacta a `BulkIngestionTrackingApiService.upload(...)`;
8. cero retry automático.

No se introdujo optimistic update ni un segundo flujo de ingestión.

## Resultado y navegación preservados

En éxito se conserva:

- `successBatchId = response.batchId`;
- persistencia en `localStorage['ach.bulk.recentBatchIds']`;
- deduplicación por batch id;
- límite de los 25 batch ids recientes;
- notificación `Lote {batchReference} registrado. Estado inicial: {status}.`;
- sin navegación automática.

La pantalla baseline solo presenta en su card de éxito el identificador `batchId`; el estado se comunica en la notificación existente. `BulkFileUploadResponse.message` no tenía un bloque dedicado de warnings/resultados en esta pantalla y no se inventó uno.

En error se conserva:

- `errorMessage = error.message`;
- notificación de error con el mismo mensaje;
- la pantalla permanece disponible para corrección/reintento manual.

Navegación existente preservada:

- `Ver seguimiento` / `Ir a seguimiento` -> `/transactions/bulk-ingestion/tracking`;
- `Abrir detalle de lote` -> `/transactions/bulk-ingestion/{batchId}` solo cuando existe `successBatchId`.

No se añadió seguimiento embebido: tracking/detail siguen siendo superficies separadas.

## Migración foundation 4C.1

Se mantuvo `app-page-header`, ya presente en el baseline.

La migración visual local realizó:

- `batchReference` -> `MatFormField` + `matInput`;
- `clientRequestId` -> `MatFormField` + `matInput`;
- acciones -> `MatButtonModule`;
- submit en progreso -> `UiEstadoCargaComponent`;
- error de upload/validación -> `UiEstadoErrorComponent` sin retry automático;
- input `type=file` permanece nativo porque Angular Material no aporta un file picker equivalente y cambiarlo alteraría el flujo.

Se retiraron todos los `<ui-boton>` de la superficie.

No se creó ningún primitive shared nuevo.

## CSS local

Se sustituyeron hardcoded colors por tokens foundation existentes:

- resumen -> `--color-surface-muted`;
- archivo válido -> `--color-success-surface` / `--color-success-text`;
- archivo inválido -> `--color-error-surface` / `--color-error-text`;
- card de éxito -> tokens success + `color-mix(...)`.

Resultado estático: cero hardcoded hex/rgb en los cuatro archivos de la superficie.

## Pruebas focales actualizadas

Solo se actualizó `bulk-ingestion-upload.component.spec.ts`.

Se cubre:

- formatos aceptados;
- selección de archivo y limpieza de estado previo;
- archivo ausente;
- extensión no soportada;
- archivo vacío;
- archivo mayor a 20 MB;
- request válido con archivo + metadata existente;
- prevención de doble submit;
- respuesta exitosa y batch id;
- persistencia de batch reciente;
- error del API;
- navegación a tracking;
- navegación a detail únicamente con batch id;
- uso de `app-page-header`/Material;
- cero `<ui-boton>`.

No se escribieron tests de capacidades inexistentes como drag-and-drop, parsing de archivos, seguimiento inline o warnings dedicados.

## Validación ejecutable real

Entorno:

- Node: `v22.16.0`;
- npm: `10.9.2`;
- `node_modules/.bin/ng`: ausente;
- TypeScript global: `5.8.3`.

Se realizó una sola comprobación corta del registry, sin repetir `npm ci`:

- comando: `timeout 12s npm ping`;
- exit code: `124`;
- causa: `registry.npmjs.org` no respondió dentro del timeout; salida: `npm notice PING https://registry.npmjs.org/`.

Intentos reales:

1. `npm test -- --watch=false --browsers=ChromeHeadless --include=src/app/features/transactions/components/bulk-ingestion-upload/bulk-ingestion-upload.component.spec.ts`
   - exit code: `127`;
   - causa: `ng: not found`.
2. `npm run build`
   - exit code: `127`;
   - causa: `ng: not found`.

No se ejecutó backend.

Validación sintáctica complementaria con TypeScript global `5.8.3` y `transpileModule(..., reportDiagnostics=true)`:

- `bulk-ingestion-upload.component.ts`: 0 diagnostics sintácticos;
- `bulk-ingestion-upload.component.spec.ts`: 0 diagnostics sintácticos.

Esta comprobación no sustituye Angular test/build.

## Validación estática contra v37

PASS antes de agregar este checkpoint.

El diff funcional contra el baseline contenía exactamente cuatro archivos:

- `bulk-ingestion-upload.component.ts`;
- `bulk-ingestion-upload.component.html`;
- `bulk-ingestion-upload.component.scss`;
- `bulk-ingestion-upload.component.spec.ts`.

El cuerpo funcional del component TS permanece igual; solo se agregaron imports Material y los módulos a `@Component.imports`.

Se confirmó byte-a-byte sin cambios:

- backend `src/`: 1475 archivos;
- B1a transaction-list: 4 archivos;
- B1a transaction-integration-result: 4 archivos;
- B1b outgoing monitoring: 11 archivos;
- B2a transaction-create: 4 archivos;
- B2b1 transaction-bulk-create: 4 archivos;
- bulk-ingestion tracking: 4 archivos;
- bulk-ingestion detail: 4 archivos;
- clearing-houses: 17 archivos;
- ach-cycles: 18 archivos;
- `BulkIngestionApiService`;
- `BulkIngestionTrackingApiService`;
- `transactions.models.ts`;
- `transactions.types.ts`;
- `transactions-routing.module.ts`.

También se confirmó:

- cero `<ui-boton>` en bulk-ingestion upload;
- cero nuevos imports `feature -> feature`;
- cero hardcoded brand hex nuevos;
- routing/permisos intactos;
- contracts/endpoints intactos.

## Pendiente deliberado

Permanecen fuera de este lote:

- `/transactions/bulk-ingestion/tracking`;
- `/transactions/bulk-ingestion/:batchId`;
- transaction bulk create;
- NACHA upload;
- returns / return-of-return;
- cycle configs;
- clearing-houses / ach-cycles;
- boundaries cross-feature reservados para B9.

No se avanzó a tracking/detail.
