# SPA 4C.3B1b1 — Outgoing Monitoring List Checkpoint

## Baseline y alcance

- Baseline obligatorio: `ACHInterbank-v33-spa-4C3B1a-transaction-list.zip`.
- Superficie migrada: `/transactions/outgoing-monitoring` (`OutgoingTransactionMonitoringListComponent`) únicamente.
- No se modificó `/transactions/outgoing-monitoring/:id`.
- No se modificaron transaction-list, integration-result, create/bulk/NACHA/returns/cycle-configs, clearing-houses, ach-cycles ni backend.
- No se avanzó a B1b2.

## Archivos modificados

- `web/ach-interbank-ui/src/app/features/transactions/outgoing-monitoring/outgoing-transaction-monitoring-list.component.ts`
- `web/ach-interbank-ui/src/app/features/transactions/outgoing-monitoring/outgoing-transaction-monitoring-list.component.html`
- `web/ach-interbank-ui/src/app/features/transactions/outgoing-monitoring/outgoing-transaction-monitoring-list.component.scss`
- `web/ach-interbank-ui/src/app/features/transactions/outgoing-monitoring/outgoing-transaction-monitoring-list.component.spec.ts`
- `docs/ai/SPA-4C3B1B1-OUTGOING-MONITORING-LIST-CHECKPOINT.md`

Antes de crear este checkpoint, la comparación completa contra v33 reportó exactamente cuatro archivos modificados y cero archivos agregados/eliminados.

## Comportamiento preservado

La migración fue exclusivamente de presentación/foundation. Se mantuvo la lógica existente del componente sin cambiar contratos ni requests:

- rango inicial construido con `daysAgo(7)` hasta la fecha actual;
- validación `fromDate <= toDate`;
- máximo de 90 días;
- `minimumAmount`/`maximumAmount`, incluyendo no negativos y orden mínimo/máximo;
- filtros de cámara, ciclo, institución destino, external id, trace, response code, tipo, estado de proceso, resultado inicial, situación posterior, devolución, atención e importes;
- `transactionExternalId` y `traceNumber` con `trim()`;
- `responseCode` con `trim().toUpperCase()`;
- paginación backend con tamaños `10/25/50/100`;
- sorting backend y traducción `updated -> lastUpdatedAt`;
- `requestSequence` para el estado de carga de búsquedas concurrentes;
- persistencia/restauración de filtros en `sessionStorage` bajo `outgoing-monitoring-filters`;
- navegación a `/transactions/outgoing-monitoring/:id`.

## Catálogo dinámico de ciclos

Se mantuvo la implementación local existente de `OutgoingTransactionMonitoringApiService.getCycles()`; no se movió a `AchCyclesApiService`.

Permanece exactamente el comportamiento de:

- cargar ciclos inicialmente sin cámara (`getCycles(undefined)`);
- recargar al cambiar `clearingHouseId`;
- limpiar `cycleId` cuando cambia la cámara;
- descartar una selección de ciclo que ya no pertenezca al catálogo recibido;
- loading/error/retry del catálogo;
- `switchMap` + `cycleRequestSequence` para proteger el estado ante respuestas fuera de orden.

Se agregó cobertura focal para confirmar que una respuesta tardía de una cámara anterior no reemplaza el catálogo vigente.

## Foundation 4C.1

Se adoptaron localmente:

- `PageHeaderComponent` (`app-page-header`);
- `UiEstadoCargaComponent`;
- `UiEstadoErrorComponent`;
- `UiEstadoVacioComponent`.

Se preservó `MatTable` y toda la tabla/listado responsive existente. No se introdujo `UiGrillaEmpresarial`, una tabla shared nueva ni otra foundation.

Se retiraron los paneles locales de heading/loading/error/empty únicamente donde quedaron reemplazados por primitives 4C.1. El botón de actualización y los controles de filtros continúan usando Angular Material.

No se forzó `UiEtiquetaEstadoComponent`: los labels/values funcionales y la semántica visual actual de proceso/resultado permanecen sin cambio.

## Pruebas focales actualizadas

`outgoing-transaction-monitoring-list.component.spec.ts` cubre ahora, además de las pruebas preexistentes:

- búsqueda inicial y paginación backend;
- request de todos los filtros, trim y uppercase de `responseCode`;
- validación de orden de fechas, rango máximo de 90 días y montos;
- catálogo de ciclos por cámara, limpieza de ciclo y retry;
- protección ante respuesta tardía del catálogo de ciclos;
- paginación backend;
- sorting backend;
- restauración de `sessionStorage`;
- navegación a detail;
- error 401/403 con mensaje de permisos;
- error general y reintento sin perder filtros;
- presencia de `app-page-header`, primitives de estado y conservación de `MatTable`.

## Validación ejecutable real

Entorno:

- Node: `v22.16.0`.
- npm: `10.9.2`.
- `node_modules/.bin/ng`: ausente.

Se realizó una sola comprobación corta del registry, sin ejecutar/repetir `npm ci`:

- Comando: `timeout 12s npm ping`
- Exit code: `124`
- Evidencia: `npm notice PING https://registry.npmjs.org/` y timeout sin respuesta.

Intentos reales:

1. `npm test -- --watch=false --browsers=ChromeHeadless --include=src/app/features/transactions/outgoing-monitoring/outgoing-transaction-monitoring-list.component.spec.ts`
   - Exit code: `127`
   - Causa: `ng: not found`.
2. `npm run build`
   - Exit code: `127`
   - Causa: `ng: not found`.

No se ejecutó backend.

Como comprobación complementaria, TypeScript global `5.8.3` ejecutó `transpileModule(..., reportDiagnostics=true)` sobre el componente y su spec con `0` diagnostics sintácticos. Esta evidencia no sustituye Angular build/tests.

## Validación estática

Comparación contra v33:

- B1a (`transaction-list` + `transaction-integration-result`): byte-a-byte intacto (8 archivos);
- outgoing-monitoring detail: byte-a-byte intacto (4 archivos);
- `OutgoingTransactionMonitoringApiService`: byte-a-byte intacto;
- `outgoing-transaction-monitoring.models.ts`: byte-a-byte intacto;
- `transactions-routing.module.ts`: byte-a-byte intacto;
- permisos de la ruta permanecen `OutgoingTransactions.Monitor.Read`, `CanReadAch`, `CanManageAch`;
- backend `src/`: byte-a-byte intacto (`1475` archivos);
- `features/ach-cycles`: byte-a-byte intacto (`18` archivos);
- `features/clearing-houses`: byte-a-byte intacto (`17` archivos);
- cero nuevos imports feature -> feature en la superficie objetivo;
- cero hardcoded hex en los archivos funcionales objetivo;
- `requestSequence`, `cycleRequestSequence`, sessionStorage, paginación, sorting, trims, uppercase y validación de 90 días permanecen en código.

## Estado

`OUTGOING_MONITORING_LIST_4C3B1B1_IMPLEMENTED_VALIDATION_PENDING`

La implementación y validación estática están completas. No existe evidencia Angular ejecutable suficiente porque `ng` no está instalado y el registry npm continuó sin responder en la única comprobación permitida.
