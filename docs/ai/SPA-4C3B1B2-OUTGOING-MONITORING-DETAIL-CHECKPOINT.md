# SPA 4C.3B1b2 — Outgoing Monitoring Detail Checkpoint

## Baseline y alcance

- Baseline obligatorio: `ACHInterbank-v34-spa-4C3B1b1-outgoing-monitoring-list.zip`.
- Superficie migrada: `/transactions/outgoing-monitoring/:id` (`OutgoingTransactionMonitoringDetailComponent`) únicamente.
- La superficie permanece estrictamente read-only.
- No se modificaron outgoing-monitoring list, transaction-list, integration-result, create/bulk/NACHA/returns/cycle-configs, clearing-houses, ach-cycles ni backend.
- No se avanzó a B2.

## Archivos modificados

- `web/ach-interbank-ui/src/app/features/transactions/outgoing-monitoring/outgoing-transaction-monitoring-detail.component.ts`
- `web/ach-interbank-ui/src/app/features/transactions/outgoing-monitoring/outgoing-transaction-monitoring-detail.component.html`
- `web/ach-interbank-ui/src/app/features/transactions/outgoing-monitoring/outgoing-transaction-monitoring-detail.component.scss`
- `web/ach-interbank-ui/src/app/features/transactions/outgoing-monitoring/outgoing-transaction-monitoring-detail.component.spec.ts`
- `docs/ai/SPA-4C3B1B2-OUTGOING-MONITORING-DETAIL-CHECKPOINT.md`

Antes de crear este checkpoint, la comparación completa contra v34 reportó exactamente cuatro archivos funcionales modificados y cero archivos funcionales agregados/eliminados.

## Comportamiento read-only preservado

La carga sigue obteniendo el identificador desde `ActivatedRoute.paramMap` y consultando exclusivamente:

- `OutgoingTransactionMonitoringApiService.getDetail(id)`.

`reload()` conserva el mismo `id` de `route.snapshot.paramMap` y vuelve a ejecutar únicamente `getDetail(id)`. No se agregaron comandos, edición, reproceso, stores ni llamadas backend nuevas.

La navegación `back()` continúa enviando a:

- `/transactions/outgoing-monitoring`.

`OutgoingTransactionMonitoringApiService`, sus endpoints/contracts y `outgoing-transaction-monitoring.models.ts` permanecen byte-a-byte iguales al baseline.

## Manejo de errores preservado

Se mantiene la separación explícita del baseline:

- HTTP 404: `Transacción no encontrada` y navegación de regreso;
- HTTP 401/403: `Acceso no autorizado` y navegación de regreso;
- otros errores: estado de error general con acción de reintento que ejecuta `reload()`.

Los estados 404/403 siguen siendo presentaciones locales deliberadas porque comunican mejor la causa específica. El error general usa ahora `UiEstadoErrorComponent`. No se colapsaron los tres casos.

## Información preservada

No se eliminaron campos ni bloques existentes. El template conserva:

- `summary`;
- `classification`;
- `integration`;
- `files`;
- `transportAttempts`;
- `transportResults`;
- `responses`;
- `returns`;
- `timeline`;
- `warnings`;
- `technicalDetail`.

También permanecen las etiquetas operacionales, causal/contexto, evidencia de transporte y acuse, identificadores de contenido y la línea de tiempo con `eventIcon()`, `stageCode`, `stageDisplayName`, `occurredAtUtc`, `title`, `description`, `outcomeDisplayName`, `severity` e `isTechnical`.

La revisión estática de referencias funcionales del template encontró 89 property paths en el baseline. Todos permanecen; `summary.transactionExternalId` solo cambió de acceso local `item.summary.transactionExternalId` a la expresión equivalente del `app-page-header` `detail()?.summary?.transactionExternalId`.

### Información técnica autorizada

`Información técnica autorizada` continúa siendo read-only y condicional a que el backend entregue `technicalDetail`. Se preservan exclusivamente los campos ya existentes:

- `transactionId`;
- `lastIntegrationMethod`;
- `lastIntegrationMode`;
- `lastIntegrationCode`;
- `lastIntegrationDurationMs`;
- `lastCorrelationId`.

No se expusieron secretos ni campos nuevos. No se añadieron inputs, textarea, select ni contenido editable a ese panel.

## Foundation 4C.1

Se adoptaron localmente:

- `PageHeaderComponent` (`app-page-header`);
- `UiEstadoCargaComponent`;
- `UiEstadoErrorComponent` para el error general;
- botones Material existentes para volver, regresar y navegación/reintento específico.

Se retiró el `MatProgressSpinnerModule` local porque el estado de carga ahora usa el primitive compartido. Se conservan `MatCard`, `MatExpansion` y `MatIcon` para la presentación específica del detalle.

No se forzó `UiEtiquetaEstadoComponent`: los estados y eventos operacionales del detail tienen semántica específica y se conservaron sin reinterpretación.

El CSS local se redujo únicamente en las piezas sustituidas por foundation (`heading`, `eyebrow` y panel local de loading/error). Los demás layouts de cards, archivos, timeline y detalle técnico se conservaron. No se agregaron hardcoded brand hex.

## Pruebas focales actualizadas

`outgoing-transaction-monitoring-detail.component.spec.ts` cubre explícitamente:

- `id` válido -> `getDetail(id)`;
- 404 -> `Transacción no encontrada`;
- 401 y 403 -> `Acceso no autorizado`;
- error general;
- `reload()` sobre el mismo id;
- `back()` -> `/transactions/outgoing-monitoring`;
- visibilidad de summary;
- archivos, intentos/resultados de transporte, responses, returns y timeline;
- `technicalDetail` read-only cuando está autorizado;
- ausencia del panel técnico cuando backend no lo entrega;
- adopción de `app-page-header` sin convertir la superficie en editable.

## Validación ejecutable real

Entorno:

- Node: `v22.16.0`.
- npm: `10.9.2`.
- `node_modules/.bin/ng`: ausente.

Se hizo una sola comprobación corta del registry; no se ejecutó/reintentó `npm ci`:

- Comando: `timeout 12s npm ping`
- Exit code: `124`
- Evidencia: `npm notice PING https://registry.npmjs.org/` y timeout sin respuesta.

Intentos reales:

1. `npm test -- --watch=false --browsers=ChromeHeadless --include=src/app/features/transactions/outgoing-monitoring/outgoing-transaction-monitoring-detail.component.spec.ts`
   - Exit code: `127`
   - Causa: `ng: not found`.
2. `npm run build`
   - Exit code: `127`
   - Causa: `ng: not found`.

No se ejecutó backend.

Como comprobación complementaria, TypeScript global `5.8.3` ejecutó `transpileModule(..., reportDiagnostics=true)` sobre el componente y su spec con `0` diagnostics sintácticos. Esto no sustituye Angular build/tests.

## Validación estática contra v34

- B1a transaction-list: byte-a-byte intacto (4 archivos).
- B1a integration-result: byte-a-byte intacto (4 archivos).
- B1b1 outgoing-monitoring list: byte-a-byte intacto (4 archivos).
- `OutgoingTransactionMonitoringApiService`: byte-a-byte intacto.
- `outgoing-transaction-monitoring.models.ts`: byte-a-byte intacto.
- `transactions-routing.module.ts`: byte-a-byte intacto.
- Ruta `outgoing-monitoring/:id`: intacta.
- Permisos de la ruta: `OutgoingTransactions.Monitor.Read`, `CanReadAch`, `CanManageAch`, intactos.
- Backend `src/`: byte-a-byte intacto (1475 archivos).
- `features/ach-cycles`: byte-a-byte intacto (18 archivos).
- `features/clearing-houses`: byte-a-byte intacto (17 archivos).
- Cero imports nuevos feature -> feature en los archivos objetivo.
- Cero hardcoded hex en los archivos funcionales objetivo.
- `getDetail(id)`, 404, 401/403 y `eventIcon()` permanecen explícitos en código.

## Estado

`OUTGOING_MONITORING_DETAIL_4C3B1B2_IMPLEMENTED_VALIDATION_PENDING`

La implementación y validación estática están completas. No existe evidencia Angular ejecutable suficiente porque `ng` no está instalado y el registry npm continuó sin responder en la única comprobación permitida.
