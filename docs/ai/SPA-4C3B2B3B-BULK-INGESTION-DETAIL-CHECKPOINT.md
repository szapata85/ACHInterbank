# SPA 4C.3B2b3b — Bulk Ingestion Detail Checkpoint

## Estado

`BULK_INGESTION_DETAIL_4C3B2B3B_IMPLEMENTED_VALIDATION_PENDING`

La implementación frontend del micro-lote quedó aplicada y la validación estática pasó. Angular test/build no pudieron ejecutarse porque el árbol `node_modules` no contiene `node_modules/.bin/ng` y el registry npm continuó sin responder dentro del timeout corto permitido.

## Baseline exacto

- `ACHInterbank-v39-spa-4C3B2b3a-bulk-ingestion-tracking.zip`
- Superficie objetivo única: `/transactions/bulk-ingestion/:batchId`
- Componente: `BulkIngestionDetailComponent`
- Ruta/permiso preservados: `bulk-ingestion/:batchId`, `CanManageAch`

No se avanzó a NACHA upload ni se modificaron upload, tracking, otras superficies de transactions, clearing-houses, ach-cycles o backend.

## Archivos modificados

Implementación funcional:

- `web/ach-interbank-ui/src/app/features/transactions/components/bulk-ingestion-detail/bulk-ingestion-detail.component.ts`
- `web/ach-interbank-ui/src/app/features/transactions/components/bulk-ingestion-detail/bulk-ingestion-detail.component.html`
- `web/ach-interbank-ui/src/app/features/transactions/components/bulk-ingestion-detail/bulk-ingestion-detail.component.scss`
- `web/ach-interbank-ui/src/app/features/transactions/components/bulk-ingestion-detail/bulk-ingestion-detail.component.spec.ts`

Evidencia:

- `docs/ai/SPA-4C3B2B3B-BULK-INGESTION-DETAIL-CHECKPOINT.md`

Antes de agregar este checkpoint, la comparación completa contra v39 mostró exactamente cuatro archivos funcionales modificados: los cuatro archivos del componente detail indicados arriba.

## Comportamiento preservado

### Carga

La implementación continúa usando exactamente:

- `getSummary(batchId)`;
- `getBatchItems(batchId, page, pageSize, status)`.

Se preservan:

- `batchId` leído desde `ActivatedRoute.snapshot.paramMap`;
- página inicial `1`;
- `pageSize = 25`;
- filtro de estado de ítems local al request;
- `UiGrillaEmpresarial` para histórico de intentos;
- `UiGrillaEmpresarial` para ítems;
- `statusLabel()` y sus textos actuales.

El cambio de filtro sigue:

1. actualizando `itemStatusFilter`;
2. reiniciando `page` a `1`;
3. llamando de nuevo `getBatchItems(...)`.

La paginación continúa calculando `totalPages` desde `itemsPage.total` y `pageSize`, sin introducir paginación/sorting/filtering adicionales en las grillas.

## Reintentos preservados

Se mantienen ambos scopes del modelo actual:

- `BulkIngestionRetryScope.Full = 1`;
- `BulkIngestionRetryScope.FailedOnly = 2`.

La llamada permanece:

```text
retry(batchId, { scope })
```

La UI continúa habilitando retry únicamente cuando el lote está en:

- `PartiallyProcessed`;
- `Failed`.

Se conserva:

- cero retry automático;
- bloqueo de la acción cuando `isLoading()` está activo;
- mensaje de éxito `Reintento solicitado correctamente.`;
- `loadAll()` después del éxito;
- `error.message` en fallo.

No se convirtió `Full` en `FailedOnly` ni viceversa.

## Cancelación preservada

La llamada continúa siendo:

```text
cancel(batchId)
```

`canCancel()` conserva exactamente los estados permitidos:

- `Uploaded`;
- `Parsed`;
- `Validated`;
- `Queued`;
- `Processing`;
- `Retrying`.

Continúa sin ofrecer cancelación para:

- `PartiallyProcessed`;
- `Completed`;
- `Failed`;
- `Cancelled`.

Se preservan:

- mensaje `Cancelación del lote registrada.`;
- `loadAll()` después del éxito;
- `error.message` en fallo;
- cero retry/cancel automático.

## Migración foundation 4C.1

Se conserva:

- `app-page-header`;
- ambas `UiGrillaEmpresarial`.

Se retiró completamente del template funcional:

- `<ui-boton>`;
- `ui-selector-buscable` para el estado de ítem.

Se adoptó localmente:

- `MatButtonModule`;
- `MatFormFieldModule`;
- `MatSelectModule`;
- botones Material para ambos reintentos, cancelación y paginación;
- `MatFormField + MatSelect` para el filtro de estado de ítem;
- `UiEstadoCargaComponent` para la carga inicial del resumen, sin sustituir el loading/empty integrado de la grilla de ítems.

No se creó ningún primitive/shared component nuevo ni cell renderer nuevo.

El selector Material conserva los valores funcionales existentes `1..4`; la opción vacía `Todos` representa el mismo estado sin filtro (`null`) que ya soportaba el control anterior al limpiar la selección.

### CSS

Se sustituyeron hardcodes locales únicamente por tokens foundation:

- `#f8fafc` -> `var(--color-surface-muted)`;
- `#166534` -> `var(--color-success-text)`;
- `#991b1b` -> `var(--color-error-text)`.

La acción destructiva Material usa `var(--color-error)` / `var(--color-on-error)`.

Resultado estático: cero hardcoded hex/rgb en TS/HTML/SCSS funcionales de detail.

## Pruebas focales actualizadas

Solo se actualizó `bulk-ingestion-detail.component.spec.ts`.

Cubre:

- carga de summary + items con `batchId`, página `1`, `pageSize 25` y filtro `null`;
- filtro de ítem reinicia página a `1` y conserva el status en el request;
- paginación y límites de página;
- textos de `statusLabel()`;
- `canRetry()` solo para `PartiallyProcessed` / `Failed`;
- retry `Full` envía exactamente `{ scope: Full }` y recarga después del éxito;
- retry `FailedOnly` envía exactamente `{ scope: FailedOnly }` y recarga después del éxito;
- `canCancel()` para todos los estados permitidos y no permitidos actuales;
- cancelación llama `cancel(batchId)` y recarga después del éxito;
- cero `<ui-boton>` renderizado;
- presencia de Material form/select;
- permanencia de las dos `UiGrillaEmpresarial`.

No se cambiaron tests para ocultar fallos ni se agregaron capacidades fuera del baseline.

## Validación ejecutable real

Entorno observado:

- Node: `v22.16.0`;
- npm: `10.9.2`;
- `node_modules/.bin/ng`: ausente;
- TypeScript global: `5.8.3`.

Se realizó una única comprobación corta del registry:

- comando: `timeout 12s npm ping`;
- exit code: `124`;
- causa: `registry.npmjs.org` no respondió dentro del timeout;
- salida observable: `npm notice PING https://registry.npmjs.org/`.

Intentos reales:

1. `npm test -- --watch=false --browsers=ChromeHeadless --include=src/app/features/transactions/components/bulk-ingestion-detail/bulk-ingestion-detail.component.spec.ts`
   - exit code: `127`;
   - causa: `ng: not found`.
2. `npm run build`
   - exit code: `127`;
   - causa: `ng: not found`.

No se ejecutó backend.

Validación sintáctica complementaria con TypeScript global `5.8.3` y `transpileModule(..., reportDiagnostics=true)`:

- `bulk-ingestion-detail.component.ts`: 0 diagnostics sintácticos;
- `bulk-ingestion-detail.component.spec.ts`: 0 diagnostics sintácticos.

Esta comprobación no sustituye Angular test/build.

## Validación estática contra v39

PASS antes de agregar este checkpoint.

Se confirmó byte-a-byte sin cambios:

- bulk-ingestion upload;
- bulk-ingestion tracking;
- `BulkIngestionTrackingApiService`;
- `transactions.models.ts` / enums / contracts;
- `transactions-routing.module.ts` y permiso de la ruta;
- backend `src/` completo: 1475 archivos sin cambios.

También se confirmó:

- `getSummary(batchId)` permanece;
- `getBatchItems(batchId, page, pageSize, status)` permanece;
- scopes `Full=1` y `FailedOnly=2` permanecen en el modelo sin cambios;
- `retry(batchId, { scope })` permanece;
- `cancel(batchId)` permanece;
- ambas `UiGrillaEmpresarial` permanecen;
- cero `<ui-boton>` en el template detail;
- cero nuevos imports `feature -> feature` en los TS modificados;
- cero hardcoded hex/rgb en TS/HTML/SCSS funcionales del detail.

## Pendiente deliberado

Fuera de este lote permanecen, sin modificación:

- NACHA upload;
- returns / return-of-return;
- cycle configs;
- clearing-houses / ach-cycles;
- boundaries cross-feature reservados para B9.

No se avanzó a NACHA upload.
