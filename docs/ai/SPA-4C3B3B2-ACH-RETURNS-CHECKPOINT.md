# SPA 4C.3B3b2 — ACH Returns Checkpoint

## Estado

`ACH_RETURNS_4C3B3B2_IMPLEMENTED_VALIDATION_PENDING`

La implementación frontend solicitada quedó aplicada. La validación Angular ejecutable continúa bloqueada por ausencia de dependencias locales y falta de respuesta del registry npm dentro del timeout corto permitido; no se simularon resultados.

## Baseline exacto

- `ACHInterbank-v42-spa-4C3B3b1-ror.zip`
- Lote: `4C.3B3b2 — ACH RETURNS`
- Superficie única migrada: `/transactions/returns`
- Componente: `AchReturnsManagementComponent`

No se avanzó a 4C.3B4 ni se modificó ROR, NACHA upload, bulk-ingestion, backend, routing, permisos, services/contracts o `ach-cycles`.

## Archivos modificados

Únicamente la superficie ACH Returns y su prueba focal:

- `web/ach-interbank-ui/src/app/features/transactions/components/ach-returns-management/ach-returns-management.component.ts`
- `web/ach-interbank-ui/src/app/features/transactions/components/ach-returns-management/ach-returns-management.component.html`
- `web/ach-interbank-ui/src/app/features/transactions/components/ach-returns-management/ach-returns-management.component.scss`
- `web/ach-interbank-ui/src/app/features/transactions/components/ach-returns-management/ach-returns-management.component.spec.ts`
- `docs/ai/SPA-4C3B3B2-ACH-RETURNS-CHECKPOINT.md`

Antes de agregar este checkpoint, la comparación completa de `features/transactions` contra v42 mostró exactamente cuatro archivos funcionales modificados: TS/HTML/SCSS/spec de `ach-returns-management`.

## Lógica y APIs preservadas

La lógica de negocio de `AchReturnsManagementComponent` permanece sin cambios. El único ajuste TS de producción fue presentacional: se retiró `MatProgressBarModule` porque sus dos progress bars locales fueron sustituidos por el primitive canónico `UiEstadoCargaComponent`, ya exportado por `SharedModule`.

Se conservan exactamente las APIs actuales:

- `AchCyclesApiService.search({ page: 1, pageSize: 100 })`;
- `ReturnReasonsApiService.getForReturns()`;
- `AchReturnsApiService.getTransactionsByCycle(cycleId)`;
- `AchReturnsApiService.generateFile(request)`.

`AchReturnsApiService`, `ReturnReasonsApiService`, `transactions.models.ts` y `transactions-routing.module.ts` quedaron byte-a-byte iguales al baseline.

## Carga de ciclos y causales

Se conserva:

- carga inicial de ciclos con `search({ page: 1, pageSize: 100 })`;
- carga de causales con `getForReturns()`;
- filtrado adicional local `reason.isForReturn`;
- manejo independiente de ambas cargas y del error de catálogos;
- `cycleId` como campo obligatorio antes de consultar transacciones.

El acoplamiento existente `transactions -> ach-cycles` mediante `AchCyclesApiService` y `AchCycleSummary` se mantiene deliberadamente para B9. No se movieron contratos ni se introdujeron imports cross-feature nuevos.

## Consulta, filtros y selección

Se preservan sin cambios funcionales:

- `cycleId`;
- `eligibility: all | eligible | blocked`;
- `query` sobre traza/referencia/código;
- `getTransactionsByCycle(cycleId)`;
- filtro de elegibilidad en memoria;
- filtro `query.trim().toLocaleLowerCase('es-CO')`;
- `UiGrillaEmpresarial`;
- columnas, paginación y ordenamiento de AG Grid;
- selección únicamente de filas `isEligible`;
- detalle read-only mediante `MatDialog`;
- enmascaramiento de cuentas en el detalle.

La selección sigue excluyendo filas no elegibles antes de construir cualquier generación.

## Generación .RET preservada

Se mantiene exactamente:

- causal seleccionada mediante `MatDialog`;
- `selectedItems` construido solo con filas elegibles y seleccionadas;
- `transactionId`;
- `returnReasonCode`;
- `achColombiaR10Basis` únicamente cuando aplica;
- `cycleId`;
- request `generateFile({ cycleId, items: selectedItems })`;
- descarga mediante `Blob` / `URL.createObjectURL`;
- nombre `devoluciones_{cycleId}.RET`;
- cero generación automática;
- cero retry automático;
- prevención de doble generación mediante `generating`;
- `lastGenerated` con archivo, ciclo, causal y cantidad de transacciones;
- mensaje de éxito actual;
- detalle backend sanitizado/fallback en error.

## R10 preservado

No se modificó la semántica de R10.

El diálogo conserva:

- `reasonCode`;
- `achColombiaR10Basis` solo si `reasonCode === 'R10'`;
- valores actuales `1` y `2`;
- `Validators.required` para la base R10;
- bloqueo del botón de generación cuando R10 no tiene base válida.

El payload continúa enviando `achColombiaR10Basis` únicamente cuando existe una base seleccionada. No se duplicaron reglas backend en Angular.

## Foundation 4C.1 adoptada

La pantalla ya utilizaba Material y `app-page-header`; este lote realizó únicamente limpieza local de estados.

Se conservaron:

- `app-page-header`;
- `UiGrillaEmpresarial`;
- `MatDialog` para detalle y causal;
- Material form fields/selects/buttons;
- chips de resumen.

Se sustituyeron estados locales por primitives canónicos cuando no se duplicaba el comportamiento de la grilla:

- carga de catálogos -> `UiEstadoCargaComponent`;
- generación de archivo -> `UiEstadoCargaComponent`;
- error de catálogos -> `UiEstadoErrorComponent` sin retry artificial;
- estado inicial -> `UiEstadoVacioComponent`;
- error de consulta -> `UiEstadoErrorComponent` conservando `retry()`;
- ciclo sin transacciones -> `UiEstadoVacioComponent`;
- error de generación -> `UiEstadoErrorComponent` sin retry automático.

La carga de transacciones no recibió un segundo loading externo: `UiGrillaEmpresarial` continúa recibiendo `[cargando]="loading"`, evitando duplicar estados de carga.

Se retiraron:

- `MatProgressBarModule` de esta superficie;
- los dos `<mat-progress-bar>` locales;
- CSS `.state-panel` y `.state-panel--error` que quedó muerto.

No se crearon primitives nuevos ni se rediseñó la pantalla.

## Tests focales actualizados

Solo se actualizó `ach-returns-management.component.spec.ts`.

La prueba focal cubre:

- carga de ciclos con `{ page: 1, pageSize: 100 }`;
- carga de causales y filtrado `isForReturn`;
- obligatoriedad de ciclo antes de llamar API;
- `getTransactionsByCycle`;
- estado inicial sin falso vacío de resultados;
- ciclo sin transacciones;
- error de carga sanitizado;
- filtros `eligible` / `blocked` / `query` sin nueva llamada backend;
- `clearFilters()`;
- columnas y selección de AG Grid;
- detalle y enmascaramiento de cuentas;
- selección solo de elegibles;
- generación normal con request exacto;
- nombre `devoluciones_cycle-1.RET`;
- R10 con `achColombiaR10Basis` estructurado;
- prevención de doble generación;
- error de generación;
- primitives foundation sin reemplazar `UiGrillaEmpresarial`;
- ausencia de `.state-panel` y `<ui-boton>`;
- permiso `CanManageAch` en `/transactions/returns`.

No se cambiaron expectativas para ocultar defectos.

## Validación ejecutable real

Entorno observado:

- Node: `v22.16.0`;
- npm: `10.9.2`;
- `node_modules/.bin/ng`: ausente;
- TypeScript global: `5.8.3`.

Se hizo una sola comprobación corta del registry, según el prompt:

- comando: `timeout 12s npm ping`;
- exit code: `124`;
- causa: `registry.npmjs.org` no respondió dentro del timeout;
- salida observable: `npm notice PING https://registry.npmjs.org/`.

No se ejecutó `npm ci` repetidamente porque el registry continuó bloqueado.

Intentos reales:

1. `npm test -- --watch=false --browsers=ChromeHeadless --include=src/app/features/transactions/components/ach-returns-management/ach-returns-management.component.spec.ts`
   - exit code: `127`;
   - causa: `ng: not found`.
2. `npm run build`
   - exit code: `127`;
   - causa: `ng: not found`.

No se ejecutó backend.

Validación sintáctica complementaria con TypeScript global `5.8.3` y `transpileModule(..., reportDiagnostics=true)`:

- component `.ts`: 0 diagnostics sintácticos;
- spec `.ts`: 0 diagnostics sintácticos.

Esta comprobación no sustituye Angular test/build.

## Validación estática contra v42

PASS.

Se confirmó byte-a-byte sin cambios:

- ROR completo (`AchReturnOfReturnManagementComponent`: TS/HTML/SCSS/spec);
- `AchReturnsApiService`;
- `ReturnReasonsApiService`;
- `transactions.models.ts`;
- `transactions-routing.module.ts` y permiso `CanManageAch`;
- `ach-cycles` completo: 18 archivos;
- backend `src/` completo: 1475 archivos.

También se confirmó:

- únicos archivos funcionales modificados dentro de `features/transactions`: los cuatro archivos de `ach-returns-management`;
- `AchCyclesApiService` / `AchCycleSummary` siguen en su boundary actual, sin resolver B9;
- cero imports `feature -> feature` nuevos;
- cero `<ui-boton>` en ACH Returns;
- cero hardcoded hex/rgb en TS/HTML/SCSS funcionales de ACH Returns;
- `UiGrillaEmpresarial` y `MatDialog` permanecen;
- `generateFile` mantiene request, Blob y nombre `.RET`;
- no se modificaron backend, migrations, contracts ni permisos.

## Pendiente deliberado

Fuera de este lote permanece:

- desacople `transactions -> ach-cycles` para B9;
- cualquier trabajo B4+;
- validación Angular ejecutable cuando el entorno disponga de dependencias/registry.

No se avanzó a B4.
