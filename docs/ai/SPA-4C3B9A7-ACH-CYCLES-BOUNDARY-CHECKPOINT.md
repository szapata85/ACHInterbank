# SPA 4C.3B9a7 — ACH Cycles Boundary Checkpoint

## Estado

`ACH_CYCLES_BOUNDARY_4C3B9A7_IMPLEMENTED_VALIDATION_PENDING`

## Alcance ejecutado

Baseline: `ACHInterbank-v60-spa-4C3B9a6-transaction-types-boundary.zip`.

Se movió el boundary compartido de ciclos operativos desde `features/ach-cycles` a `core`, sin cambiar lógica funcional, contratos HTTP ni backend.

No se avanzó a otro boundary B9.

## Ownership canónico

Movidos:

- `features/ach-cycles/services/ach-cycles-api.service.ts` -> `core/services/ach-cycles-api.service.ts`
- `features/ach-cycles/models/ach-cycle.model.ts` -> `core/models/ach-cycle.model.ts`

El modelo movido es byte-a-byte idéntico al baseline.

El service es funcionalmente idéntico; el único cambio dentro del archivo fue el ajuste de la ruta relativa de `ApiService` requerido por su nueva ubicación:

- antes: `../../../core/services/api.service`
- ahora: `./api.service`

Su import del modelo continúa resolviendo a `../models/ach-cycle.model` y ahora ambos archivos pertenecen a `core`.

No quedaron reexports ni copias legacy en `features/ach-cycles`.

## Contratos preservados

`AchCyclesApiService` conserva exactamente:

- `search()`
- `getById()`
- `create()`
- `update()`
- `getCurrentConfigurations()`

Modelos conservados exactamente:

- `AchCycleSummary`
- `AchCycleConfigurationOption`
- `AchCycleFilter`
- `SaveAchCycleRequest`
- `PagedAchCycleResponse`

No se dividió el service, no se crearon wrappers y no se modificaron endpoints, parámetros, payloads ni normalización de respuestas.

## Consumidores actualizados

Se cambiaron únicamente imports en los consumidores reales y sus specs afectados:

ACH cycles:

- `features/ach-cycles/components/ach-cycle-list.component.ts`
- `features/ach-cycles/components/ach-cycle-list.component.spec.ts`
- `features/ach-cycles/components/ach-cycle-form.component.ts`
- `features/ach-cycles/components/ach-cycle-form.component.spec.ts`

Transactions:

- `features/transactions/components/transaction-list/transaction-list.component.ts`
- `features/transactions/components/transaction-list/transaction-list.component.spec.ts`
- `features/transactions/components/ach-returns-management/ach-returns-management.component.ts`
- `features/transactions/components/ach-returns-management/ach-returns-management.component.spec.ts`

Reports:

- `features/reports/components/traceability-report.component.ts`
- `features/reports/components/traceability-report.component.spec.ts`

Los 10 archivos fueron comparados contra el ZIP baseline revirtiendo únicamente las rutas de import: su contenido restante es byte-a-byte idéntico.

## Validación estática

PASS:

- 1 sola definición de `AchCyclesApiService`;
- 1 sola definición de `AchCycleSummary`;
- 1 sola definición de `AchCycleConfigurationOption`;
- 1 sola definición de `AchCycleFilter`;
- 1 sola definición de `SaveAchCycleRequest`;
- 1 sola definición de `PagedAchCycleResponse`;
- 0 imports desde `features/ach-cycles/services/ach-cycles-api.service`;
- 0 imports desde `features/ach-cycles/models/ach-cycle.model`;
- archivos legacy de service/model ausentes;
- todas las rutas relativas TypeScript del SPA resuelven después del movimiento;
- 0 imports `core -> features`;
- 0 nuevos imports `feature -> feature` respecto del baseline;
- se eliminaron 9 edges feature -> `ach-cycles` existentes desde `transactions` y `reports`;
- cuerpos funcionales de los 10 consumidores: idénticos al baseline salvo imports;
- modelo movido: byte-a-byte idéntico;
- service movido: idéntico salvo la ruta relativa de `ApiService`;
- B9a1–a6 no aparecen en el diff del lote y permanecen intactos;
- backend: 1474 archivos bajo `src/`, 0 cambios.

Antes de agregar este checkpoint, el diff contra el baseline era exactamente:

- 2 archivos core agregados por movimiento;
- 2 archivos legacy eliminados;
- 10 consumidores/specs modificados solo en imports;
- 0 otros cambios.

TypeScript global 5.8.3, `transpileModule` sobre los 12 TS/spec nuevos o modificados: 0 errores sintácticos.

## Validación ejecutable real

Entorno:

- Node `v22.16.0`
- npm `10.9.2`
- TypeScript global `5.8.3`
- `node_modules/.bin/ng`: ausente

Se realizó una sola comprobación corta del registry:

- `timeout 12s npm ping` -> exit 124, después de `npm notice PING https://registry.npmjs.org/`.

Tests afectados intentados en una sola ejecución para:

- `ach-cycle-list.component.spec.ts`
- `ach-cycle-form.component.spec.ts`
- `transaction-list.component.spec.ts`
- `ach-returns-management.component.spec.ts`
- `traceability-report.component.spec.ts`

Resultado:

- exit 127, `ng: not found`.

Build:

- `npm run build` -> exit 127, `ng: not found`.

No se ejecutó ni repitió `npm ci`. No se ejecutó backend.

## Archivos funcionales agregados/eliminados/modificados

Agregados por movimiento:

- `web/ach-interbank-ui/src/app/core/services/ach-cycles-api.service.ts`
- `web/ach-interbank-ui/src/app/core/models/ach-cycle.model.ts`

Eliminados:

- `web/ach-interbank-ui/src/app/features/ach-cycles/services/ach-cycles-api.service.ts`
- `web/ach-interbank-ui/src/app/features/ach-cycles/models/ach-cycle.model.ts`

Imports modificados:

- `web/ach-interbank-ui/src/app/features/ach-cycles/components/ach-cycle-list.component.ts`
- `web/ach-interbank-ui/src/app/features/ach-cycles/components/ach-cycle-list.component.spec.ts`
- `web/ach-interbank-ui/src/app/features/ach-cycles/components/ach-cycle-form.component.ts`
- `web/ach-interbank-ui/src/app/features/ach-cycles/components/ach-cycle-form.component.spec.ts`
- `web/ach-interbank-ui/src/app/features/transactions/components/transaction-list/transaction-list.component.ts`
- `web/ach-interbank-ui/src/app/features/transactions/components/transaction-list/transaction-list.component.spec.ts`
- `web/ach-interbank-ui/src/app/features/transactions/components/ach-returns-management/ach-returns-management.component.ts`
- `web/ach-interbank-ui/src/app/features/transactions/components/ach-returns-management/ach-returns-management.component.spec.ts`
- `web/ach-interbank-ui/src/app/features/reports/components/traceability-report.component.ts`
- `web/ach-interbank-ui/src/app/features/reports/components/traceability-report.component.spec.ts`
