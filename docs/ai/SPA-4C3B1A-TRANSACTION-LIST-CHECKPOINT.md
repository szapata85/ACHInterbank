# SPA 4C.3B1a — Transaction List + Integration Result Checkpoint

## Baseline

- Baseline obligatorio: `ACHInterbank-v32-spa-4C3B0-transactions-clearing-houses-cycles-inventory.zip`.
- Alcance implementado: `TransactionListComponent` y `TransactionIntegrationResultComponent` únicamente.
- No se avanzó a B1b.
- No se modificaron backend, routing, `TransactionsApiService`, modelos/types, `ach-cycles`, `clearing-houses`, NACHA Security ni `outgoing-monitoring`.

## Archivos modificados

- `web/ach-interbank-ui/src/app/features/transactions/components/transaction-list/transaction-list.component.ts`
- `web/ach-interbank-ui/src/app/features/transactions/components/transaction-list/transaction-list.component.html`
- `web/ach-interbank-ui/src/app/features/transactions/components/transaction-list/transaction-list.component.scss`
- `web/ach-interbank-ui/src/app/features/transactions/components/transaction-list/transaction-list.component.spec.ts`
- `web/ach-interbank-ui/src/app/features/transactions/components/transaction-integration-result/transaction-integration-result.component.ts`
- `web/ach-interbank-ui/src/app/features/transactions/components/transaction-integration-result/transaction-integration-result.component.html`
- `web/ach-interbank-ui/src/app/features/transactions/components/transaction-integration-result/transaction-integration-result.component.scss`
- `web/ach-interbank-ui/src/app/features/transactions/components/transaction-integration-result/transaction-integration-result.component.spec.ts`

Este checkpoint es el único archivo adicional respecto del baseline.

## Transaction list — comportamiento preservado

Se mantuvieron sin cambio funcional:

- `UiGrillaEmpresarial` como grilla principal.
- filtros por fecha efectiva, cámara compensadora y ciclo ACH;
- carga de cámaras y ciclos;
- agrupación por lote;
- definición/selección de columnas;
- paginación interna de la grilla (`paginacion=true`, `tamanoPagina=10`);
- navegación a creación, carga masiva y seguimiento de lotes;
- `TransactionsApiService.getAll()`;
- `TransactionsApiService.getIntegrationResult()`;
- semántica de ciclo:
  - sin cámara seleccionada: `achCycleId = null` y `achCycleName = selectedCycleId`;
  - con cámara seleccionada: `achCycleId = selectedCycleId` y `achCycleName = null`;
- fecha efectiva enviada en `YYYY-MM-DD` cuando existe;
- `clearingHouseId` sin alteración de contrato;
- vista de devoluciones:
  - recibidas: `21`, `31`, `51`;
  - enviadas: `26`, `36`, `56`.

La migración visual retiró todos los `<ui-boton>` de esta superficie y utiliza botones Angular Material. Los estados de carga, error y vacío usan los primitives compartidos de foundation 4C.1; `UiGrillaEmpresarial` permanece intacta.

## Desacople mínimo realizado

Se corrigió únicamente la indirección de cámara confirmada en 4C.3B0:

- `ClearingHousesApiService` ahora se importa directamente desde `core/services/clearing-houses-api.service`.
- `ClearingHouseOption` ahora se importa directamente desde `core/models/clearing-house.model`.

Se preservaron deliberadamente:

- `AchCyclesApiService` desde `features/ach-cycles`;
- `AchCycleSummary` desde `features/ach-cycles`.

La dependencia `transactions -> ach-cycles` para consulta de ciclos queda pendiente para B9, tal como fue inventariada en 4C.3B0. No se introdujeron imports feature -> feature nuevos.

## Integration result — comportamiento preservado

Se mantuvieron exactamente:

- `latest`;
- `history`;
- `transportStatus`;
- `businessStatus`;
- labels actuales de negocio;
- `Failed` y `TimedOut` como `Error técnico`;
- `Rejected` como `Rechazado` funcional cuando el transporte no falló.

El botón de cierre se migró a Angular Material (`mat-icon-button`). Los colores locales hardcoded se sustituyeron por tokens semánticos existentes (`--color-primary`, success/error/warning tokens) sin cambiar la semántica de estados.

## Pruebas focales actualizadas

`transaction-list.component.spec.ts` cubre, además de las validaciones previas:

- request exacto y semántica `achCycleId` vs `achCycleName`;
- navegación de las acciones de la bandeja;
- códigos received/sent de devoluciones;
- llamada a `getIntegrationResult()`;
- ausencia de `<ui-boton>` y permanencia de `UiGrillaEmpresarial`;
- import directo de `ClearingHousesApiService` desde core mediante el contrato de test.

`transaction-integration-result.component.spec.ts` cubre:

- labels de `BusinessStatus`;
- `Failed` y `TimedOut` como error técnico;
- `Rejected` como rechazo funcional;
- preservación/render de `latest` + `history`;
- botón Material de cierre y emisión de `closePanel`.

## Validación ejecutable real

Entorno detectado:

- Node: `v22.16.0`.
- npm: `10.9.2`.
- `node_modules/.bin/ng`: ausente.

Se realizó una sola comprobación corta del registry, sin repetir `npm ci`:

- Comando: `timeout 12s npm ping`
- Resultado: exit code `124`.
- Causa observada: quedó esperando respuesta de `https://registry.npmjs.org/` hasta agotar el timeout.

Intentos reales de ejecución:

1. `npm test -- --watch=false --browsers=ChromeHeadless --include=src/app/features/transactions/components/transaction-list/transaction-list.component.spec.ts`
   - exit code `127`
   - causa: `ng: not found`.
2. `npm test -- --watch=false --browsers=ChromeHeadless --include=src/app/features/transactions/components/transaction-integration-result/transaction-integration-result.component.spec.ts`
   - exit code `127`
   - causa: `ng: not found`.
3. `npm run build`
   - exit code `127`
   - causa: `ng: not found`.

No se ejecutó backend.

Como evidencia complementaria únicamente sintáctica, TypeScript global `5.8.3` ejecutó `transpileModule(..., reportDiagnostics=true)` sobre los cuatro `.ts` modificados (component + spec de ambas superficies) con `0` diagnostics. Esta comprobación no sustituye Angular build/tests.

## Validación estática

Comparación contra v32:

- backend `src/`: byte-a-byte igual (`1475` archivos);
- `features/ach-cycles`: byte-a-byte igual (`18` archivos);
- `features/clearing-houses`: byte-a-byte igual (`17` archivos);
- `transactions/outgoing-monitoring`: byte-a-byte igual (`11` archivos);
- `transactions-routing.module.ts`: byte-a-byte igual;
- `services/transactions-api.service.ts`: byte-a-byte igual;
- `transactions.models.ts`: byte-a-byte igual;
- `transactions.types.ts`: byte-a-byte igual;
- exactamente ocho archivos funcionales/de prueba cambiaron antes de crear este checkpoint;
- cero `<ui-boton>` en `transaction-list`;
- `UiGrillaEmpresarial` permanece;
- `ClearingHousesApiService` y `ClearingHouseOption` se consumen directamente desde core;
- no se añadió ningún import cross-feature nuevo;
- no existen hardcoded hex en los dos componentes objetivo tras la migración;
- se conservaron las expresiones que envían `achCycleId`/`achCycleName`;
- se conservaron los sets `21/31/51` y `26/36/56`;
- se conservaron `getAll()` y `getIntegrationResult()`;
- Integration Result conserva tratamiento diferenciado de error técnico y rechazo funcional.

## Estado

`TRANSACTION_LIST_4C3B1A_IMPLEMENTED_VALIDATION_PENDING`

La implementación y validación estática están completas, pero no existe evidencia Angular ejecutable suficiente porque las dependencias no están instaladas y el registry npm continúa inaccesible desde el entorno.
