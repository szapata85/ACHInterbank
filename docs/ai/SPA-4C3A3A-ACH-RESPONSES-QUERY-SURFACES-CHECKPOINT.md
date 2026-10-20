# SPA 4C.3A.3a — ACH Responses Query/Read Surfaces Checkpoint

## Baseline

Baseline obligatorio aplicado:

`ACHInterbank-v27-spa-4C3A2-alias-permissions-retirement.zip`

El lote se limita a las superficies read-only de `ach-responses`. No se avanzó a 4C.3A.3b, no se modificó backend, no se tocaron DTOs/contracts/API, Alias, NACHA Security ni otras features.

## Superficies migradas

Se migraron únicamente:

- `/ach-responses` — `ach-response-list-page.component.*`
- `/ach-responses/dashboard` — `ach-response-dashboard-page.component.*`
- `/ach-responses/audit` — `ach-response-audit-page.component.ts`
- `/ach-responses/:id/notification-attempts` — `ach-response-attempts-page.component.*`

Se agregó prueba focal para auditoría:

- `ach-response-audit-page.component.spec.ts`

## Foundation 4C.1 adoptada

Las cuatro superficies conservan `app-page-header` y dejan de consumir `ui-boton`.

Se adoptó Angular Material de forma local:

- `MatButtonModule` para acciones.
- `MatFormFieldModule`, `MatInputModule` y `MatSelectModule` para filtros y entradas de list/dashboard/audit.
- `MatTableModule` para la tabla simple de intentos de notificación.

Primitives compartidos 4C.1 utilizados:

- `UiEstadoCargaComponent`.
- `UiEstadoErrorComponent`.
- `UiEstadoVacioComponent` donde aplica.
- `UiEtiquetaEstadoComponent` para el estado de notificación en attempts.
- `UiGrillaEmpresarialComponent` se conserva en la bandeja; no fue sustituido por `MatTable`.

No se creó ningún shared primitive nuevo y el árbol `web/ach-interbank-ui/src/app/shared` permanece byte-a-byte igual al baseline.

## Comportamiento preservado

### Bandeja `/ach-responses`

- Se conservaron todos los filtros y su normalización.
- Se conservaron exactamente `pageNumber` y `pageSize` en el request.
- Se conservaron columnas, renderers y acción de navegación a detalle de `UiGrillaEmpresarial`.
- Se conservó loading/error/empty mediante la grilla empresarial.
- Se conservaron navegación anterior/siguiente y cambio de tamaño de página.
- Se humanizó únicamente el encabezado local `Command Center Respuestas ACH` a `Bandeja de respuestas ACH`.

### Dashboard

- Se conserva una sola llamada agregada `getDashboard` con los mismos filtros.
- Se conservaron KPIs, cálculos, tasas y rutas de navegación existentes.
- Loading/error continúan usando primitives canónicos.

### Notification attempts

- Se conserva la carga por response id con `getAttempts(id)`.
- Se conservaron navegación a detalle y a bandeja.
- Se conservan los 15 campos visibles y los formatters existentes.
- La tabla simple fue migrada localmente a `MatTable`; no se introdujo AG Grid.
- El `estado-pill`/status CSS local fue retirado de esta página y el estado se muestra con `UiEtiquetaEstadoComponent`.

### Audit

- El value interno `response` continúa llamando `getResponseAudit`.
- El value interno `mapping` continúa llamando `getMappingAudit`; solo el texto visible se humanizó a `Homologación`.
- Se preservaron `entityId`, metadata técnica, previous/new state, actor y reason.
- Se incorporaron los primitives canónicos de loading/empty/error sin cambiar el contrato de auditoría.

## Contratos, rutas y permisos preservados

Validación estática confirmó que permanecen byte-a-byte iguales al baseline:

- `ach-responses-routing.module.ts`
- `services/ach-responses-api.service.ts`
- `models/ach-responses.models.ts`

Por lo tanto:

- las cuatro rutas objetivo siguen existiendo;
- `CanReadAch` sigue intacto en routing;
- los endpoints de `AchResponsesApiService` no cambiaron;
- el `PagedResponse<T>` propio de ach-responses mantiene exactamente `{ items, pageNumber, pageSize, totalCount, totalPages }` y no fue unificado con core.

## Fuera de alcance / deuda deliberadamente pendiente para 4C.3A.3b

Permanecen sin migración funcional o visual en este lote, byte-a-byte iguales al baseline:

- `/ach-responses/:id` — `ach-response-detail-page.component.*`
- `/ach-responses/manual-review` — `ach-response-manual-review-page.component.*`
- `/ach-responses/status-mappings` — `ach-response-status-mappings-page.component.*`

Estas páginas todavía pueden consumir `ui-boton`; su retiro corresponde exclusivamente a 4C.3A.3b.

No se modificaron routing, API service, modelos, backend, migrations, permissions, Alias, transactions, cycles, clearing-houses, nacha-operational ni incoming-nacha-command-center.

## Pruebas focales

Se actualizaron/agregaron pruebas para demostrar:

- LIST: request de filtros, `pageNumber/pageSize`, carga en `UiGrillaEmpresarial`, navegación a detalle, anterior/siguiente y cambio de page size.
- DASHBOARD: request de filtros, KPIs, cálculo/rates y navegación desde KPI (pruebas existentes conservadas).
- ATTEMPTS: carga por id, loading/error/empty, navegación, campos visibles y uso de `UiEtiquetaEstadoComponent`.
- AUDIT: `getResponseAudit`, `getMappingAudit`, value interno `mapping`, validación, loading/empty/error.

## Validación realmente ejecutada

### Instalación frontend

Primer intento real:

```text
npm ci
```

Resultado real:

```text
BLOCKED_BY_ENVIRONMENT
El proceso no terminó dentro de 180 segundos y el harness agotó el timeout.
El proceso npm que quedó vivo fue terminado explícitamente; no se dejó trabajo en background.
```

No se obtuvo un exit code del proceso shell porque el harness terminó la llamada por timeout. Como diagnóstico de red se ejecutó:

```text
timeout 15s npm ping
=> exit 124
=> npm notice PING https://registry.npmjs.org/
```

Esto confirma que el registry no respondió dentro del intervalo de diagnóstico. Un segundo intento controlado de `npm ci` también quedó bloqueado hasta agotar el timeout del harness y fue terminado; el `node_modules` parcial se eliminó. No se repitieron instalaciones adicionales.

### Tests focales de las cuatro superficies

Comando:

```text
npm test -- --watch=false --browsers=ChromeHeadless \
  --include='src/app/features/ach-responses/pages/ach-response-list-page.component.spec.ts' \
  --include='src/app/features/ach-responses/pages/ach-response-dashboard-page.component.spec.ts' \
  --include='src/app/features/ach-responses/pages/ach-response-attempts-page.component.spec.ts' \
  --include='src/app/features/ach-responses/pages/ach-response-audit-page.component.spec.ts'
```

Resultado real:

```text
exit 127
sh: 1: ng: not found
```

### Tests relacionados de ach-responses

Comando:

```text
npm test -- --watch=false --browsers=ChromeHeadless --include='src/app/features/ach-responses/**/*.spec.ts'
```

Resultado real:

```text
exit 127
sh: 1: ng: not found
```

### Build frontend

Comando:

```text
npm run build
```

Resultado real:

```text
exit 127
sh: 1: ng: not found
```

No se ejecutó ninguna suite backend ni migration.

## Validación estática ejecutada

La validación estática local finalizó con `STATIC_VALIDATION=PASS` y confirmó:

- rutas objetivo presentes;
- 7 route guards de ach-responses continúan usando `CanReadAch`;
- routing, service y models byte-a-byte iguales al baseline;
- `PagedResponse<T>` propio sin cambios;
- cero `ui-boton` en las cuatro superficies objetivo;
- `ui-boton` permanece únicamente donde se espera para páginas 4C.3A.3b;
- `UiGrillaEmpresarial` permanece en list;
- attempts usa `MatTable` y `UiEtiquetaEstadoComponent`, sin `estado-pill` local;
- detail/manual-review/status-mappings byte-a-byte iguales al baseline;
- cero hardcoded brand hex nuevos en las cuatro superficies;
- ningún nuevo import cross-feature;
- shared foundation byte-a-byte igual al baseline;
- backend `src/` byte-a-byte igual al baseline (1475 archivos comparados).

Adicionalmente se ejecutó un parse/transpile sintáctico de los TS/specs afectados con el TypeScript global disponible y todos los archivos resultaron sin diagnostics sintácticos. Esta comprobación no sustituye Angular build/tests.

## Backend unchanged

El árbol completo `src/` fue comparado por SHA-256 contra el baseline obligatorio y no presenta cambios byte-a-byte. Esto incluye NACHA Security y las áreas backend de Alias previamente cerradas.

## Estado final

`ACH_RESPONSES_QUERY_SURFACES_IMPLEMENTED_VALIDATION_PENDING`

No se usa `VERIFIED` porque no fue posible ejecutar Angular tests ni build con un dependency tree utilizable.
