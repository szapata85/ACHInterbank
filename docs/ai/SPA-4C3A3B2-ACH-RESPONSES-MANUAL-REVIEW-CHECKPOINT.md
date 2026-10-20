# SPA 4C.3A.3b2 — ACH Responses Manual Review + Resolución de Huérfanas Checkpoint

## Baseline

Baseline obligatorio aplicado:

`ACHInterbank-v29-spa-4C3A3b1-ach-response-detail-reprocess.zip`

El lote se limita exclusivamente a `/ach-responses/manual-review`. No se avanzó a `/ach-responses/status-mappings` (4C.3A.3b3) ni a 4C.3B. No se modificaron backend, routing, `AchResponsesApiService`, modelos/contracts, detail, permisos, Alias ni NACHA Security.

## Archivos modificados

Se modificaron únicamente los cuatro archivos de la superficie manual-review:

- `web/ach-interbank-ui/src/app/features/ach-responses/pages/ach-response-manual-review-page.component.ts`
- `web/ach-interbank-ui/src/app/features/ach-responses/pages/ach-response-manual-review-page.component.html`
- `web/ach-interbank-ui/src/app/features/ach-responses/pages/ach-response-manual-review-page.component.scss`
- `web/ach-interbank-ui/src/app/features/ach-responses/pages/ach-response-manual-review-page.component.spec.ts`

Se agregó este checkpoint:

- `docs/ai/SPA-4C3A3B2-ACH-RESPONSES-MANUAL-REVIEW-CHECKPOINT.md`

## Carga inicial preservada

`ngOnInit()` conserva dos cargas independientes:

1. `loadManualReviewCases()`;
2. `loadOrphans()`.

La consulta de huérfanas continúa invocando exactamente:

```text
getOrphans(undefined, undefined)
```

No se agregaron filtros backend ni dependencia entre ambas cargas.

## Bandeja de revisión manual preservada

`UiGrillaEmpresarial` permanece como grilla de la bandeja; no se reemplazó por `MatTable` ni se introdujo AG Grid adicional.

Estados críticos preservados exactamente:

- `NoHomologada`
- `RequiereRevisionManual`
- `ErrorFuncional`
- `PendienteReintento`

Defaults preservados:

```text
estadoProcesamiento = NoHomologada
pageNumber = 1
pageSize = 20
```

Se conservan los filtros existentes y su normalización mediante `normalizeAchFilter`: fechas, estado crítico, tipo de respuesta, id de transacción, cámara, entidad origen y entidad destino.

La paginación conserva `previousPage()`, `nextPage()` y `setPageSize()`. `clearFilters()` vuelve a `NoHomologada`, página 1, tamaño 20 y limpia el resto. `openDetail(row)` continúa navegando a `/ach-responses/:id`.

## Prioridad y estado crítico

Clasificación preservada sin cambios:

- Alta: `NoHomologada`, `ErrorFuncional`;
- Media: `RequiereRevisionManual`, `PendienteReintento`;
- Baja: cualquier otro valor.

Los renderers DOM existentes de `UiGrillaEmpresarial` se conservaron deliberadamente mediante `createAchBadgeElement` y `createAchButtonElement`. No se introdujo infraestructura Angular de cell-renderers.

## Selección de huérfana

`selectOrphan(orphan)` conserva exactamente la carga del formulario:

```text
orphanId = orphan.id
expectedVersion = orphan.version
reason = ''
functionalReference = orphan.resolvedReference ?? ''
reject = false
```

`expectedVersion` continúa siendo parte del Reactive Form, no se expone como campo editable y no se recalcula desde frontend.

## Inicio de revisión

`beginSelectedReview()` conserva como requeridos:

- `orphanId`;
- `expectedVersion`;
- `reason`;
- `reason.trim().length >= 5`.

La llamada permanece:

```text
beginOrphanReview(orphanId, expectedVersion, reason.trim())
```

No se cambió el orden de argumentos ni se genera una nueva versión en frontend.

En éxito se conserva:

1. notificación `Revisión iniciada.`;
2. `selectOrphan(item)` con el item retornado por backend;
3. actualización de `expectedVersion` a `item.version` mediante esa selección;
4. `loadOrphans()`.

Así, la versión retornada por el backend sustituye a la versión previa después de iniciar revisión.

## Resolución de huérfana — asociación

`resolveSelectedOrphan()` mantiene como requeridos en todos los casos:

- `orphanId`;
- `expectedVersion`;
- justificación con mínimo 5 caracteres después de `trim()`.

Para asociación (`reject=false`), `functionalReference` sigue siendo obligatoria y se envía con `trim()`.

La llamada permanece conceptualmente:

```text
resolveOrphan(
  orphanId,
  expectedVersion,
  reason.trim(),
  functionalReference.trim(),
  false
)
```

En éxito se conserva el mensaje de asociación, reset completo de `resolutionForm`, `loadOrphans()` y `loadManualReviewCases()`. No se agrega navegación automática.

## Resolución de huérfana — rechazo

Para rechazo (`reject=true`), `functionalReference` no es obligatoria y se conserva el envío de `null`:

```text
resolveOrphan(
  orphanId,
  expectedVersion,
  reason.trim(),
  null,
  true
)
```

No se invirtió el flag ni se sustituyó `null` por string vacío. Se mantiene el mensaje diferenciado `Respuesta huérfana rechazada.`.

## Concurrencia HTTP 409

`handleOperationError()` mantiene tratamiento explícito de 409:

- warning: `Otro usuario modificó el caso. Se recargó la versión vigente.`;
- `loadOrphans()`;
- sin retry automático del comando;
- sin auto-merge ni reenvío del POST con la versión vencida.

Para errores distintos de 409 se conserva `error.error.detail` cuando es string y el fallback `No fue posible completar la operación.`.

## Migración visual Foundation 4C.1

Se mantiene `app-page-header` y `UiGrillaEmpresarial`.

Se retiraron completamente los usos de `<ui-boton>` de manual-review y se adoptaron:

- `MatButtonModule` para Buscar, Limpiar, Anterior, Siguiente, Seleccionar, Iniciar revisión y Resolver;
- `MatFormFieldModule`;
- `MatInputModule`;
- `MatSelectModule`;
- `MatCheckboxModule`.

El formulario de resolución sigue siendo Reactive Forms. `reason` conserva `Validators.required` + `Validators.minLength(5)`. `expectedVersion` sigue en el form y no es editable.

Para el bloque de huérfanas se adoptaron los estados canónicos:

- `UiEstadoCargaComponent`;
- `UiEstadoVacioComponent`;
- `UiEstadoErrorComponent`.

Se agregó únicamente el flag local `orphanError`. El retry del estado de error ejecuta solo `loadOrphans()`.

`resolutionStatus` se presenta con `UiEtiquetaEstadoComponent` sin modificar su valor textual. El mapeo visual local es:

- `Resolved` / `Associated` / `Completed` -> `exitoso`;
- `Pending` / `InReview` -> `pendiente`;
- `Rejected` / `Failed` -> `fallido`;
- cualquier otro valor -> `inactivo`.

No se inventaron estados funcionales ni se cambiaron contratos.

## Estado de ach-response-status.scss

`ach-response-manual-review-page.component.scss` conserva deliberadamente:

```text
@use '../styles/ach-response-status';
```

Motivo: los renderers DOM existentes dentro de `UiGrillaEmpresarial` continúan usando las clases `prioridad-*` y `estado-*` provistas por ese stylesheet. Forzar su retiro en este lote rompería la presentación de badges de la grilla o requeriría infraestructura de cell-renderers fuera de alcance.

`styles/ach-response-status.scss` no fue modificado ni eliminado. Su limpieza, si queda habilitada tras 4C.3A.3b3, permanece como deuda posterior.

## Pruebas focales actualizadas

`ach-response-manual-review-page.component.spec.ts` cubre ahora:

- init con `search()` + `getOrphans(undefined, undefined)`;
- default `NoHomologada`, page 1 / size 20;
- filtro `ErrorFuncional` y normalización de strings;
- `clearFilters()`;
- límites de previous/next y reset al cambiar page size;
- navegación a detail;
- `selectOrphan()` con `version -> expectedVersion` y `resolvedReference`;
- begin sin orphan y con reason menor de 5;
- begin válido con reason trimmeado;
- actualización de expectedVersion con el item retornado;
- asociación: functionalReference obligatoria, argumentos trimmeados, reset y recargas;
- rechazo: reference opcional, `null`, `reject=true` y mensaje diferenciado;
- 409 en begin y resolve con warning + reload de huérfanas y una sola invocación del comando;
- error no-409 con `error.error.detail` y fallback;
- error de carga de huérfanas expuesto mediante estado canónico;
- clasificación de prioridad y renderers de grilla conservados;
- mapeo local de `resolutionStatus`;
- cero `<ui-boton>`, controles Material, `UiEtiquetaEstadoComponent` y `UiGrillaEmpresarial` presentes.

## Validación ejecutable realmente intentada

### Diagnóstico de dependencias

```text
node --version
=> v22.16.0

npm --version
=> 10.9.2

node_modules/.bin/ng
=> missing / dependency tree no utilizable
```

Se ejecutó una única comprobación corta del registry:

```text
timeout 12s npm ping
=> exit 124
=> npm notice PING https://registry.npmjs.org/
```

El registry no respondió durante el timeout. Conforme al alcance, no se ejecutó `npm ci` repetidamente ni se dejó una instalación bloqueada.

### Test focal manual-review

```text
npm test -- --watch=false --browsers=ChromeHeadless \
  --include='src/app/features/ach-responses/pages/ach-response-manual-review-page.component.spec.ts'
```

Resultado real:

```text
exit 127
sh: 1: ng: not found
```

### Tests relacionados de ach-responses

```text
npm test -- --watch=false --browsers=ChromeHeadless \
  --include='src/app/features/ach-responses/**/*.spec.ts'
```

Resultado real:

```text
exit 127
sh: 1: ng: not found
```

### Build frontend

```text
npm run build
```

Resultado real:

```text
exit 127
sh: 1: ng: not found
```

No se ejecutó backend.

## Validación sintáctica adicional

Los dos archivos TypeScript modificados (`component.ts` y `component.spec.ts`) se procesaron con TypeScript global 5.8.3 mediante `transpileModule`.

Resultado:

```text
component.ts: 0 syntactic diagnostics
component.spec.ts: 0 syntactic diagnostics
```

Esta comprobación es únicamente sintáctica y no sustituye Angular build/tests.

## Validación estática obligatoria

Resultado: PASS.

Se confirmó:

- `/ach-responses/manual-review` sigue registrada;
- la ruta conserva `CanReadAch`;
- `AchResponsesApiService` es byte-a-byte igual al baseline;
- `ach-responses.models.ts` es byte-a-byte igual al baseline;
- backend completo `src/` es byte-a-byte igual al baseline: 1475 archivos comparados;
- los cuatro archivos detail son byte-a-byte iguales al baseline;
- los cuatro archivos status-mappings son byte-a-byte iguales al baseline;
- `ach-responses-routing.module.ts` es byte-a-byte igual al baseline;
- cero `<ui-boton>` en el template manual-review;
- `UiGrillaEmpresarial` permanece;
- `beginOrphanReview` conserva `expectedVersion`;
- `resolveOrphan` conserva `expectedVersion`;
- `reject=true` continúa enviando `functionalReference=null`;
- la justificación conserva `trim()` y mínimo 5 caracteres;
- HTTP 409 continúa tratado explícitamente;
- `getOrphans(undefined, undefined)` permanece;
- no se agregaron hardcoded hex en los archivos manual-review modificados;
- no se agregaron imports cross-feature;
- antes de agregar este checkpoint, la comparación completa contra el baseline reportó exactamente cuatro archivos modificados: los cuatro manual-review, sin archivos borrados ni otros archivos agregados.

## Deuda deliberadamente pendiente

Fuera de alcance y byte-a-byte igual al baseline:

- `/ach-responses/status-mappings` -> 4C.3A.3b3.

No se avanzó a ese lote.

## Estado final

`ACH_RESPONSE_MANUAL_REVIEW_IMPLEMENTED_VALIDATION_PENDING`

No se usa `VERIFIED` porque Angular tests/build no pudieron ejecutarse con un dependency tree instalado y el registry npm permaneció inaccesible en la comprobación permitida.
