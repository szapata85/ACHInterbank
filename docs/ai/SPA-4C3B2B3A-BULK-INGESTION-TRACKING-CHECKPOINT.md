# SPA 4C.3B2b3a — Bulk Ingestion Tracking Checkpoint

## Estado

`BULK_INGESTION_TRACKING_4C3B2B3A_IMPLEMENTED_VALIDATION_PENDING`

Angular tests/build remain blocked by the unavailable npm registry/dependency tree. No result was simulated.

## Baseline exacto

`ACHInterbank-v38-spa-4C3B2b2-bulk-ingestion-upload.zip`

Scope único: `/transactions/bulk-ingestion/tracking` / `BulkIngestionTrackingComponent`.

No se avanzó a `/transactions/bulk-ingestion/:batchId` ni se modificó upload, otras superficies de transactions, clearing-houses, ach-cycles o backend.

## Archivos modificados

- `web/ach-interbank-ui/src/app/features/transactions/components/bulk-ingestion-tracking/bulk-ingestion-tracking.component.ts`
- `web/ach-interbank-ui/src/app/features/transactions/components/bulk-ingestion-tracking/bulk-ingestion-tracking.component.html`
- `web/ach-interbank-ui/src/app/features/transactions/components/bulk-ingestion-tracking/bulk-ingestion-tracking.component.scss`
- `web/ach-interbank-ui/src/app/features/transactions/components/bulk-ingestion-tracking/bulk-ingestion-tracking.component.spec.ts`
- este checkpoint

Antes de agregar este checkpoint, el diff byte-a-byte contra v38 contenía exactamente los cuatro archivos del componente.

## Implementación real preservada

### API/service

La superficie sigue consumiendo exclusivamente `BulkIngestionTrackingApiService`:

- `getBatch(batchId)` para búsqueda manual, carga de recientes y refresh individual;
- `retry(batchId, request)` para el reintento explícito.

`BulkIngestionTrackingApiService` permanece byte-a-byte igual al baseline. No se alteraron endpoint, contracts, models ni routing.

Ruta/permiso preservados:

- `/transactions/bulk-ingestion/tracking`
- `CanManageAch`

### Búsqueda manual

Se conserva:

- búsqueda por batch id;
- `trim()` del identificador antes de `getBatch`;
- batch vacío no llama API y muestra warning;
- `upsertRow()` agrega o actualiza el lote;
- orden descendente por `uploadedAtUtc` al hacer upsert;
- persistencia de recientes después de una búsqueda exitosa;
- limpieza del campo después del éxito;
- `error.message` en fallo.

### Recent batches / localStorage

Clave preservada exactamente:

`ach.bulk.recentBatchIds`

Se mantiene:

- lectura tolerante a ausencia o JSON inválido;
- solo IDs string;
- máximo 25 IDs al persistir;
- actualización mediante `refreshFromRecent()`;
- fallo aislado de un batch reciente no hace fallar toda la carga (`catchError(() => of(null))`);
- orden final por `uploadedAtUtc` descendente.

Durante la inspección se encontró que el target ya tenía la intención de mantener orden temporal mediante `upsertRow()`, pero `refreshFromRecent()` dependía del orden incidental de los IDs persistidos. Se hizo explícito el mismo orden descendente al materializar los resultados de `forkJoin`; no cambia API, modelo ni criterio funcional.

### Filtro local y KPIs

El filtro por estado sigue siendo local; no genera llamadas backend.

Valores internos preservados:

- `ALL`
- `1` Uploaded / Cargado
- `2` Parsed / Parseado
- `3` Validated / Validado
- `4` Queued / En cola
- `5` Processing / Procesando
- `6` PartiallyProcessed / Parcialmente procesado
- `7` Completed / Completado
- `8` Failed / Fallido
- `9` Retrying / Reintentando
- `10` Cancelled / Cancelado

Los KPIs se siguen calculando exclusivamente sobre `filteredRows()`:

- lotes;
- registros;
- exitosos;
- fallidos.

Se corrigió localmente una dependencia reactiva ya implícita: `filteredRows()` ahora consume el `statusFilter` signal que el componente ya actualizaba desde `status.valueChanges`. Antes el signal existía pero el computed leía directamente el `FormControl`, que no es una dependencia Signal y podía dejar el resultado cacheado. Esto preserva el comportamiento funcional declarado del filtro sin cambiar valores, contracts ni requests.

## Reintento sensible preservado

La UI continúa ofreciendo reintento únicamente cuando `canRetry(status)` es verdadero para:

- `BulkIngestionBatchStatus.PartiallyProcessed`;
- `BulkIngestionBatchStatus.Failed`.

La llamada permanece exactamente:

```text
retry(batchId, {
  scope: BulkIngestionRetryScope.FailedOnly
})
```

Se conserva:

- cero retry automático;
- prevención de iniciar desde el método mientras `isLoading()` está activo;
- mensaje de éxito `Reintento del lote solicitado.`;
- `refreshSingle(batchId)` después del éxito;
- `error.message` en fallo.

No se amplió el conjunto de estados reintentables ni se cambió `FailedOnly`.

Los botones DOM `detail` / `retry` dentro del `cellRenderer` de AG Grid permanecen como estaban; no se introdujo Angular cell-renderer infrastructure ni se cambió `data-action`.

## Navegación preservada

`openDetail(batchId)` continúa navegando a:

`/transactions/bulk-ingestion/:batchId`

No se agregó navegación adicional.

## Migración foundation 4C.1

Se conserva:

- `app-page-header`;
- `UiGrillaEmpresarial` y su loading/empty handling integrado.

Se retiró de la superficie:

- `<ui-boton>`;
- `ui-selector-buscable` para el filtro de estado.

Se adoptó localmente:

- `MatFormFieldModule`;
- `MatInputModule`;
- `MatSelectModule`;
- `MatButtonModule`.

El batch id ahora usa `MatFormField + matInput`, el estado `MatFormField + MatSelect`, y las acciones principales botones Material. No se creó ningún primitive compartido nuevo.

Los KPI hardcoded colors fueron sustituidos por tokens foundation:

- exitosos -> `var(--color-success-text)`;
- fallidos -> `var(--color-error-text)`.

Resultado estático: cero hardcoded hex/rgb en TS/HTML/SCSS de la superficie.

## Pruebas focales actualizadas

Solo se actualizó `bulk-ingestion-tracking.component.spec.ts`.

Cubre:

- búsqueda vacía no llama API;
- búsqueda válida agrega y actualiza un lote existente;
- carga de IDs recientes;
- orden de recientes por `uploadedAtUtc` descendente;
- JSON inválido de localStorage se tolera como lista vacía;
- filtro local por estado;
- `canRetry()` solo para `PartiallyProcessed` y `Failed`;
- retry envía `BulkIngestionRetryScope.FailedOnly`;
- éxito de retry muestra mensaje y refresca el lote con `getBatch`;
- navegación al detail;
- persistencia máxima de 25 IDs;
- cero `<ui-boton>`;
- uso de Material y permanencia de `UiGrillaEmpresarial`.

No se añadieron pruebas/capacidades de cancelación, detail ni upload en este lote.

## Validación ejecutable real

Entorno:

- Node: `v22.16.0`;
- npm: `10.9.2`;
- `node_modules/.bin/ng`: ausente;
- TypeScript global: `5.8.3`.

Se hizo una única comprobación corta del registry, sin ejecutar/repetir `npm ci`:

- comando: `timeout 12s npm ping`;
- exit code: `124`;
- causa: `registry.npmjs.org` no respondió dentro del timeout;
- salida observable: `npm notice PING https://registry.npmjs.org/`.

Intentos reales:

1. `npm test -- --watch=false --browsers=ChromeHeadless --include=src/app/features/transactions/components/bulk-ingestion-tracking/bulk-ingestion-tracking.component.spec.ts`
   - exit code: `127`;
   - causa: `ng: not found`.
2. `npm run build`
   - exit code: `127`;
   - causa: `ng: not found`.

No se ejecutó backend.

Validación sintáctica complementaria con TypeScript global `5.8.3` y `transpileModule(..., reportDiagnostics=true)`:

- `bulk-ingestion-tracking.component.ts`: 0 diagnostics sintácticos;
- `bulk-ingestion-tracking.component.spec.ts`: 0 diagnostics sintácticos.

Esta comprobación no sustituye Angular test/build.

## Validación estática contra v38

PASS antes de agregar este checkpoint.

El diff funcional contra el baseline contenía exactamente cuatro archivos, todos pertenecientes a `BulkIngestionTrackingComponent`.

Se confirmó byte-a-byte sin cambios:

- bulk-ingestion upload;
- bulk-ingestion detail;
- `BulkIngestionTrackingApiService`;
- `transactions.models.ts` / contracts/types;
- routing y permisos;
- todos los lotes B1;
- B2a transaction create;
- B2b1 transaction bulk create;
- clearing-houses;
- ach-cycles;
- backend `src/`.

También se confirmó:

- cero nuevos imports `feature -> feature` en la superficie;
- cero hardcoded brand hex/rgb nuevos;
- cero `<ui-boton>` en tracking;
- `UiGrillaEmpresarial` permanece;
- scope de retry `FailedOnly` permanece;
- localStorage mantiene `ach.bulk.recentBatchIds` y límite 25;
- detail no fue modificado.

## Pendiente deliberado

Fuera de este lote permanecen:

- `/transactions/bulk-ingestion/:batchId`;
- NACHA upload;
- returns / return-of-return;
- cycle configs;
- clearing-houses / ach-cycles;
- boundaries cross-feature reservados para B9.

No se avanzó al detail.
