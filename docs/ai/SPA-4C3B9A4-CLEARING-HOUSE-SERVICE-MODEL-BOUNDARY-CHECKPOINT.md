# SPA 4C.3B9a4 — Clearing House Service/Model Boundary Checkpoint

## Estado

`CLEARING_HOUSE_SERVICE_MODEL_BOUNDARY_4C3B9A4_IMPLEMENTED_VALIDATION_PENDING`

## Alcance ejecutado

Baseline: `ACHInterbank-v57-spa-4C3B9a3-ach-cycles-clearing-house-reexport.zip`.

Se eliminó el ownership `feature -> feature` causado por `ClearingHouse` y `ClearingHousesService` desde `features/clearing-houses`, sin cambios de lógica funcional ni backend.

No se avanzó a ningún otro boundary B9.

## Inspección previa / consumidores reales

`ClearingHouse` se consumía en:

- `features/clearing-houses/clearing-houses.component.ts`
- `features/clearing-houses/transaction-policies.component.ts`
- `features/transactions/components/cycle-config-management/cycle-config-management.component.ts`
- `features/catalogs/components/clearing-house-special-dates.component.ts`
- `features/operational-restrictions/operational-restrictions.component.ts`
- indirectamente por `ClearingHousesService` en su contrato de retorno.

`ClearingHousesService` se consumía en producción en:

- clearing-houses admin
- transaction policies
- cycle config management
- clearing-house special dates
- operational restrictions
- NACHA operational dashboard
- UAT NACHA inbound simulator

También se actualizaron únicamente los imports de sus specs afectados.

`clearing-houses.models.ts` contenía además `ClearingHouseInput`, `ClearingHousePage`, `NachaProfileOption` y `PaymentRailOption`. Estos DTOs forman parte directa de las firmas de `ClearingHousesService`, por lo que se colocaron junto con `ClearingHouse` en core para evitar una dependencia `core -> feature`. No se duplicó ninguna definición.

## Decisión de ownership

Se consolidó el modelo en el archivo core ya existente:

- `core/models/clearing-house.model.ts`

Se movió el service, conservando su nombre, a:

- `core/services/clearing-houses.service.ts`

No se fusionó `ClearingHousesService` dentro de `ClearingHousesApiService`: ambos tienen contratos distintos y una colisión semántica real en `list()` (`ClearingHousesApiService.list()` consulta opciones operativas/cacheadas; `ClearingHousesService.list(search, isActive, page)` consulta administración paginada). Fusionarlos habría cambiado el token DI o el contrato público.

Se eliminaron:

- `features/clearing-houses/clearing-houses.models.ts`
- `features/clearing-houses/clearing-houses.service.ts`

## Contratos y comportamiento preservados

Validación estática contra el baseline:

- cuerpo de `ClearingHousesService`: idéntico después de retirar únicamente imports;
- `path = 'api/clearing-houses'`: intacto;
- `list(search, isActive, page)`: intacto, incluyendo trim, `pageSize: 20` y params;
- `get(id)`, `create(value)`, `update(id, value)`, `changeStatus(id, isActive)`: intactos;
- `profiles(code)` y endpoint `/nacha-profiles`: intactos;
- `paymentRailOptions()` y endpoint `/payment-rail-options`: intactos;
- definiciones movidas desde `clearing-houses.models.ts`: texto contractual preservado;
- `ClearingHouseOption` y `ClearingHousesApiService` existentes en core: byte-a-byte intactos.

Los 14 archivos consumidores/specs modificados mantienen su contenido no-import exactamente igual al baseline.

## Imports actualizados

Todos los consumidores de `ClearingHouse` ahora importan desde `core/models/clearing-house.model`.

Todos los consumidores de `ClearingHousesService` ahora importan desde `core/services/clearing-houses.service`.

`NachaInboundSimulatorComponent` importa `NachaProfileOption` desde el mismo modelo core porque es parte de la firma del service movido; su lógica no cambió.

## Validación estática

PASS:

- 0 referencias frontend a `features/clearing-houses/clearing-houses.models`;
- 0 referencias frontend a `features/clearing-houses/clearing-houses.service`;
- una sola definición de `ClearingHouse`;
- una sola definición de `ClearingHousesService`;
- 0 imports `core -> features`;
- 0 nuevos imports `feature -> feature`;
- se eliminaron 14 edges `feature -> clearing-houses` asociados al boundary;
- imports relativos de los TS modificados: 0 sin resolver;
- backend: 1474 archivos bajo `src/`, 0 cambios;
- B9a1: `financial-institution.model.ts` y `financial-institutions-api.service.ts` intactos;
- B9a2: `shared/components/clearing-house-context-navigation.*` intacto;
- B9a3: `ach-cycles-api.service.ts` y `ach-cycle.model.ts` intactos;
- `core/services/clearing-houses-api.service.ts` intacto.

TypeScript global 5.8.3, `transpileModule` sobre los 16 TS/spec nuevos o modificados: 0 errores sintácticos.

## Validación ejecutable real

Entorno:

- Node `v22.16.0`
- npm `10.9.2`
- `node_modules/.bin/ng`: ausente

Se realizó una sola comprobación corta del registry:

- `timeout 12s npm ping` -> exit 124; quedó esperando `https://registry.npmjs.org/`.

Tests afectados intentados en una sola ejecución:

- clearing-houses admin
- transaction policies
- cycle config management
- clearing-house special dates
- operational restrictions
- NACHA operational dashboard
- UAT NACHA inbound simulator

Resultado: exit 127, `ng: not found`.

Build:

- `npm run build` -> exit 127, `ng: not found`.

No se ejecutó ni repitió `npm ci`. No se ejecutó backend.

## Archivos funcionales modificados/movidos

Core:

- `web/ach-interbank-ui/src/app/core/models/clearing-house.model.ts`
- `web/ach-interbank-ui/src/app/core/services/clearing-houses.service.ts` (nuevo ownership)

Eliminados del feature:

- `web/ach-interbank-ui/src/app/features/clearing-houses/clearing-houses.models.ts`
- `web/ach-interbank-ui/src/app/features/clearing-houses/clearing-houses.service.ts`

Imports-only en consumidores/specs:

- `features/clearing-houses/clearing-houses.component.ts`
- `features/clearing-houses/clearing-houses.component.spec.ts`
- `features/clearing-houses/transaction-policies.component.ts`
- `features/clearing-houses/transaction-policies.component.spec.ts`
- `features/transactions/components/cycle-config-management/cycle-config-management.component.ts`
- `features/transactions/components/cycle-config-management/cycle-config-management.component.spec.ts`
- `features/catalogs/components/clearing-house-special-dates.component.ts`
- `features/catalogs/components/clearing-house-special-dates.component.spec.ts`
- `features/operational-restrictions/operational-restrictions.component.ts`
- `features/operational-restrictions/operational-restrictions.component.spec.ts`
- `features/nacha-operational/pages/nacha-operational-dashboard.component.ts`
- `features/nacha-operational/pages/nacha-operational-dashboard.component.spec.ts`
- `features/uat/components/nacha-inbound-simulator/nacha-inbound-simulator.component.ts`
- `features/uat/components/nacha-inbound-simulator/nacha-inbound-simulator.component.spec.ts`
