# SPA 4C.3B9a8 — Residual Cross-Feature Boundary Inventory

## Estado

`SPA_4C3B9A8_RESIDUAL_BOUNDARY_INVENTORY_COMPLETE_NO_IMPLEMENTATION`

## Alcance

Baseline: `ACHInterbank-v61-spa-4C3B9a7-ach-cycles-boundary.zip`.

Este lote es únicamente inventario. No se modificó código funcional, no se movieron archivos y no se tocó backend. El único archivo agregado al baseline es este checkpoint.

Se escaneó recursivamente:

- `web/ach-interbank-ui/src/app/features/**/*.ts`
- `web/ach-interbank-ui/src/app/features/**/*.scss`

Se resolvieron imports TypeScript estáticos/dinámicos y dependencias SCSS `@use/@forward/@import` relativas, y se reportaron únicamente dependencias `features/X -> features/Y` con `X != Y`.

### Convención de conteo

Para evitar ambigüedad:

- **28 edges residuales** = 28 ocurrencias concretas de import/dependencia desde un archivo consumidor hacia un archivo propietario de otra feature.
- Esas 28 ocurrencias colapsan a **12 direcciones feature -> feature**.
- Agrupadas por `origen + destino + archivo propietario`, resultan **18 boundaries propietarios**.
- Hay **19 archivos consumidores** con al menos un cruce: 13 productivos y 6 specs.
- De las 28 ocurrencias, 19 son productivas y 9 pertenecen a specs.

No se encontraron aliases `tsconfig` que oculten cruces adicionales; `tsconfig.json` no define `paths`.

## Matriz origen -> destino -> símbolos

| # | Origen | Destino | Símbolos / dependencia | Ocurrencias | Clasificación |
|---|---|---|---|---:|---|
| 1 | `ach-colombia-mft-administration` | `ach-colombia-file-exchange` | `AchColombiaFileExchangeService`, `ManagedMftAdministration` | 2 | **A — MOVE_TO_CORE** |
| 2 | `ach-cycles` | `admin` | SCSS `@use users-list.component` | 1 | **B — MOVE_TO_SHARED** |
| 3 | `audit` | `audit-shared` | `auditDateRangeValidator`, `sanitizeAuditText`, `summarizeChangedFields`, `toLocalDateParam` | 1 | **B — MOVE_TO_SHARED** |
| 4 | `auth-logs` | `audit-shared` | `auditDateRangeValidator`, `sanitizeAuditText`, `toLocalDateParam` | 1 | **B — MOVE_TO_SHARED** |
| 5 | `catalogs` | `customer-third-parties` | `CustomerThirdPartiesComponent`, `CustomerThirdPartiesService` | 2 | **C — RETIRE** |
| 6 | `cenit` | `reports` | `CycleReportFilter`, `CycleReportRow`, `ReportsApiService`, `ReturnRejectionReportFilter`, `ReturnRejectionReportRow` | 2 | **A — MOVE_TO_CORE** |
| 7 | `incoming-nacha-command-center` | `nacha-operational` | `NachaOperationalDashboardComponent`, `NachaOperationalFileDetailComponent` | 4 | **D — KEEP_WITH_JUSTIFICATION** |
| 8 | `integrations` | `admin` | `SoapIntegrationSettingsComponent` | 1 | **D — KEEP_WITH_JUSTIFICATION** |
| 9 | `navigation` | `admin` | `RoleSummary`, `RolesApiService` | 3 | **A — MOVE_TO_CORE** |
| 10 | `operational-restrictions` | `transactions` | `ClearingHouseCycleConfigsApiService`, `ClearingHouseCycleConfigItem` | 3 | **A — MOVE_TO_CORE** |
| 11 | `transactions` | `customers` | `CustomersApiService`, `CustomerSummary` | 4 | **A — MOVE_TO_CORE** |
| 12 | `uat` | `nacha-config-admin` | `NachaConfigApiService`, `NachaConfigProfileReadModel` | 4 | **A — MOVE_TO_CORE** |

Clasificación por dirección: A=6, B=3, C=1, D=2.

## Detalle por boundary propietario

### 1. `ach-colombia-mft-administration -> ach-colombia-file-exchange`

**Propietario actual:** `features/ach-colombia-file-exchange/ach-colombia-file-exchange.service.ts`  
**Tipo:** service + model  
**Símbolos:** `AchColombiaFileExchangeService`, `ManagedMftAdministration`  
**Consumidores externos:** 2 (1 productivo, 1 spec)

- `features/ach-colombia-mft-administration/ach-colombia-mft-administration.component.ts`
- `features/ach-colombia-mft-administration/ach-colombia-mft-administration.component.spec.ts`

**Clasificación: A — MOVE_TO_CORE.** El mismo API service es usado productivamente por dos features distintas. El service además posee operaciones de intercambio y de administración MFT; un micro-lote debe mover el boundary contractual completo necesario, incluidos sus modelos dependientes, sin duplicar ni envolver el API.

### 2. `ach-cycles -> admin`

**Propietario actual:** `features/admin/components/users-list.component.scss`  
**Tipo:** styles  
**Dependencia:** `@use '../../admin/components/users-list.component' as *;`  
**Consumidores externos:** 1 productivo

- `features/ach-cycles/components/ach-cycle-list.component.scss`

**Clasificación: B — MOVE_TO_SHARED.** La lista de ciclos reutiliza CSS de una pantalla admin (`.filters` y reglas de layout) como si fuera una primitive visual. Debe extraerse el estilo realmente reusable a shared/foundation o sustituirse por estilo local foundation; no debe mantenerse un `@use` feature -> feature.

### 3. `audit -> audit-shared`

**Propietario actual:** `features/audit-shared/audit-display.util.ts`  
**Tipo:** util  
**Símbolos:** `auditDateRangeValidator`, `sanitizeAuditText`, `summarizeChangedFields`, `toLocalDateParam`  
**Consumidores externos:** 1 productivo

- `features/audit/components/audit-log.component.ts`

**Clasificación: B — MOVE_TO_SHARED.** Son helpers de presentación/validación de auditoría, no una pantalla ni un servicio de dominio.

### 4. `auth-logs -> audit-shared`

**Propietario actual:** `features/audit-shared/audit-display.util.ts`  
**Tipo:** util  
**Símbolos:** `auditDateRangeValidator`, `sanitizeAuditText`, `toLocalDateParam`  
**Consumidores externos:** 1 productivo

- `features/auth-logs/components/auth-log.component.ts`

**Clasificación: B — MOVE_TO_SHARED.** Mismo boundary visual/utilitario que el punto anterior; un único movimiento a shared debe cerrar ambos edges.

### 5. `catalogs -> customer-third-parties` — component

**Propietario actual:** `features/customer-third-parties/components/customer-third-parties.component.ts`  
**Tipo:** component  
**Símbolo:** `CustomerThirdPartiesComponent`  
**Consumidores externos:** 1, solo spec

- `features/catalogs/components/catalogs-phase3.component.spec.ts`

**Clasificación: C — RETIRE.** No hay consumidor productivo desde catalogs; el cruce existe únicamente porque un spec agregador de catalogs prueba una pantalla de otra feature.

### 6. `catalogs -> customer-third-parties` — service

**Propietario actual:** `features/customer-third-parties/services/customer-third-parties.service.ts`  
**Tipo:** service  
**Símbolo:** `CustomerThirdPartiesService`  
**Consumidores externos:** 1, solo spec

- `features/catalogs/components/catalogs-phase3.component.spec.ts`

**Clasificación: C — RETIRE.** Igual que el componente: no hay dependencia productiva. El spec de catalogs no debería poseer pruebas de otra feature.

### 7. `cenit -> reports`

**Propietario actual:** `features/reports/services/reports-api.service.ts`  
**Tipo:** service + models  
**Símbolos:** `CycleReportFilter`, `CycleReportRow`, `ReportsApiService`, `ReturnRejectionReportFilter`, `ReturnRejectionReportRow`  
**Consumidores externos:** 2 productivos

- `features/cenit/services/cenit-operations-api.service.ts`
- `features/cenit/components/cenit-operation-page.component.ts`

**Clasificación: A — MOVE_TO_CORE.** CENIT delega productivamente consultas a `ReportsApiService` y usa sus DTOs. El API/contrato de reportes ya es transversal. El siguiente lote debe preservar endpoints y evitar duplicar servicios.

### 8. `incoming-nacha-command-center -> nacha-operational` — dashboard

**Propietario actual:** `features/nacha-operational/pages/nacha-operational-dashboard.component.ts`  
**Tipo:** component  
**Símbolo:** `NachaOperationalDashboardComponent`  
**Consumidores externos:** 2 productivos

- `features/incoming-nacha-command-center/incoming-nacha-command-center-routing.module.ts`
- `features/incoming-nacha-command-center/incoming-nacha-command-center.module.ts`

**Clasificación: D — KEEP_WITH_JUSTIFICATION.** El componente es una página de dominio, no una primitive shared ni un contrato core. Está productivamente montado por `/incoming-nacha-command-center`, mientras `nacha-operational` mantiene redirects hacia esa ruta. La corrección adecuada es un micro-lote de **ownership directo hacia la feature que lo monta**, no moverlo a core/shared ni retirarlo sin migrar rutas/imports.

### 9. `incoming-nacha-command-center -> nacha-operational` — file detail

**Propietario actual:** `features/nacha-operational/pages/nacha-operational-file-detail.component.ts`  
**Tipo:** component  
**Símbolo:** `NachaOperationalFileDetailComponent`  
**Consumidores externos:** 2 productivos

- `features/incoming-nacha-command-center/incoming-nacha-command-center-routing.module.ts`
- `features/incoming-nacha-command-center/incoming-nacha-command-center.module.ts`

**Clasificación: D — KEEP_WITH_JUSTIFICATION.** Misma razón del dashboard: es una página productiva específica del command center y requiere un move-to-owner dedicado; no es candidato válido a shared/core.

### 10. `integrations -> admin`

**Propietario actual:** `features/admin/components/soap-integration-settings.component.ts`  
**Tipo:** component  
**Símbolo:** `SoapIntegrationSettingsComponent`  
**Consumidores externos:** 1 productivo

- `features/integrations/integrations-routing.module.ts`

**Clasificación: D — KEEP_WITH_JUSTIFICATION.** La página está productivamente montada como `/integraciones/soap-settings` y usa contratos core de SOAP. No es un componente visual genérico. La corrección arquitectónica es mover la página a la feature `integrations`; hasta ese move-to-owner no debe forzarse a shared/core ni retirarse.

### 11. `navigation -> admin` — model

**Propietario actual:** `features/admin/models/user.model.ts`  
**Tipo:** model  
**Símbolo:** `RoleSummary`  
**Consumidores externos:** 1 productivo

- `features/navigation/components/navigation-menu.component.ts`

**Clasificación: A — MOVE_TO_CORE.** El resumen de roles es contrato transversal usado por navegación y admin.

### 12. `navigation -> admin` — service

**Propietario actual:** `features/admin/services/users-api.service.ts`  
**Tipo:** service  
**Símbolo:** `RolesApiService`  
**Consumidores externos:** 2 (1 productivo, 1 spec)

- `features/navigation/components/navigation-menu.component.ts`
- `features/navigation/components/navigation-menu.component.spec.ts`

**Clasificación: A — MOVE_TO_CORE.** `RolesApiService` es un read service transversal (`api/roles`). Debe extraerse sin mover innecesariamente `UsersApiService` si este último sigue siendo admin-only.

### 13. `operational-restrictions -> transactions` — service

**Propietario actual:** `features/transactions/services/clearing-house-cycle-configs-api.service.ts`  
**Tipo:** service  
**Símbolo:** `ClearingHouseCycleConfigsApiService`  
**Consumidores externos:** 2 (1 productivo, 1 spec)

- `features/operational-restrictions/operational-restrictions.component.ts`
- `features/operational-restrictions/operational-restrictions.component.spec.ts`

**Clasificación: A — MOVE_TO_CORE.** Es el boundary conocido `operational-restrictions -> transactions`; el API de configuración de ciclos ya se consume fuera de transactions.

### 14. `operational-restrictions -> transactions` — model

**Propietario actual:** `features/transactions/transactions.models.ts`  
**Tipo:** model  
**Símbolo:** `ClearingHouseCycleConfigItem`  
**Consumidores externos:** 1 productivo

- `features/operational-restrictions/operational-restrictions.component.ts`

**Clasificación: A — MOVE_TO_CORE.** Debe viajar con el contrato/API de cycle configs necesario para eliminar el edge sin reexport legacy.

### 15. `transactions -> customers` — model

**Propietario actual:** `features/customers/models/customer.model.ts`  
**Tipo:** model  
**Símbolo:** `CustomerSummary`  
**Consumidores externos:** 2 (1 productivo, 1 spec)

- `features/transactions/components/transaction-create/transaction-create.component.ts`
- `features/transactions/components/transaction-create/transaction-create.component.spec.ts`

**Clasificación: A — MOVE_TO_CORE.** `CustomerSummary` es un contrato read transversal usado por originación de transacciones.

### 16. `transactions -> customers` — service

**Propietario actual:** `features/customers/services/customers-api.service.ts`  
**Tipo:** service  
**Símbolo:** `CustomersApiService`  
**Consumidores externos:** 2 (1 productivo, 1 spec)

- `features/transactions/components/transaction-create/transaction-create.component.ts`
- `features/transactions/components/transaction-create/transaction-create.component.spec.ts`

**Clasificación: A — MOVE_TO_CORE.** El API de customers es usado productivamente por `transactions`. Un lote de boundary debe preservar CRUD/endpoints completos y evitar un wrapper duplicado.

### 17. `uat -> nacha-config-admin` — service

**Propietario actual:** `features/nacha-config-admin/services/nacha-config-api.service.ts`  
**Tipo:** service  
**Símbolo:** `NachaConfigApiService`  
**Consumidores externos:** 2 (1 productivo, 1 spec)

- `features/uat/components/nacha-inbound-simulator/nacha-inbound-simulator.component.ts`
- `features/uat/components/nacha-inbound-simulator/nacha-inbound-simulator.component.spec.ts`

**Clasificación: A — MOVE_TO_CORE.** UAT usa productivamente el API de configuración NACHA. La superficie del service también contiene operaciones administrativas de escritura, por lo que este move es más delicado: debe preservar firmas/endpoints y evitar crear un segundo API read-only sin una decisión explícita.

### 18. `uat -> nacha-config-admin` — model

**Propietario actual:** `features/nacha-config-admin/models/nacha-config-admin.models.ts`  
**Tipo:** model  
**Símbolo:** `NachaConfigProfileReadModel`  
**Consumidores externos:** 2 (1 productivo, 1 spec)

- `features/uat/components/nacha-inbound-simulator/nacha-inbound-simulator.component.ts`
- `features/uat/components/nacha-inbound-simulator/nacha-inbound-simulator.component.spec.ts`

**Clasificación: A — MOVE_TO_CORE.** El read model es compartido por UAT y configuración NACHA; debe acompañar el boundary del API o quedar en un modelo core coherente.

## Deudas conocidas revisadas explícitamente

- `operational-restrictions -> transactions`: **sí permanece**, únicamente por `ClearingHouseCycleConfigsApiService` y `ClearingHouseCycleConfigItem`.
- `catalogs -> otras features`: **sí permanece**, pero únicamente en el spec `catalogs-phase3.component.spec.ts` hacia `customer-third-parties`; no hay consumidor productivo en catalogs para ese edge.
- `transactions -> otras features`: **sí permanece**, hacia `customers` por `CustomersApiService` y `CustomerSummary`.
- `clearing-houses -> otras features`: **0 edges residuales** detectados.
- `ach-cycles -> otras features`: **1 edge residual**, exclusivamente SCSS hacia `admin/users-list.component.scss`.

## Invariantes B9a1–B9a7

### `core -> features` y `shared -> features`

Escaneo de imports TS y dependencias SCSS:

- `core -> features`: **0**
- `shared -> features`: **0**

### Paths/símbolos legacy retirados

Se verificó que siguen ausentes y con 0 referencias legacy:

- `features/transactions/services/financial-institutions-api.service.ts`
- `features/transactions/transactions.types.ts`
- `features/clearing-houses/clearing-house-context-navigation.component.ts`
- `features/ach-cycles/services/ach-cycles-api.service.ts`
- `features/ach-cycles/models/ach-cycle.model.ts`
- `features/clearing-houses/clearing-houses.service.ts`
- `features/clearing-houses/clearing-houses.models.ts`
- `ConfirmationDialogComponent`
- `ConfirmationDialogData`

Los 5 imports/refs actuales que contienen el nombre `clearing-house-context-navigation.component` apuntan al ownership canónico `shared/components` o son el propio component/spec shared; no reintroducen el path anterior.

Persisten los ownerships canónicos de B9a1–B9a7 en `core`/`shared`, incluyendo financial institutions, clearing-house context navigation, clearing-house model/service, operational confirmation dialog, transaction type enums y ACH cycles API/model.

## Validación ejecutada

Por instrucción de este lote **no** se ejecutaron:

- `npm ping`
- tests Angular
- `npm run build`
- `npm ci`

Sí se ejecutó `python scripts/validate-solution-projects.py` porque existe en el baseline.

Resultado real:

```text
SOLUTION GRAPH: OK
  solution projects: 7
  disk projects:     7
  project references: all targets exist and are loaded by the solution
```

## Evidencia de no modificación funcional

Antes de crear este checkpoint se generó un manifiesto SHA-256 de los **3601 archivos** del baseline extraído.

Después de crear el checkpoint se validó el árbol contra ese manifiesto. Resultado real:

- baseline: 3601 archivos;
- final: 3602 archivos;
- 0 hashes baseline modificados;
- 0 paths baseline eliminados;
- exactamente 1 path nuevo: `docs/ai/SPA-4C3B9A8-RESIDUAL-BOUNDARY-INVENTORY.md`.

Backend baseline: **1474 archivos bajo `src/`**. Como los 3601 archivos baseline conservan exactamente su SHA-256, backend queda byte-a-byte intacto.

## Propuesta de siguientes micro-lotes

Ordenados del más mecánico/seguro al más delicado, sin implementar nada en 4C.3B9a8:

1. **Retiro de coupling test-only `catalogs -> customer-third-parties`**: sacar del spec agregador de catalogs las pruebas que pertenecen a la feature `customer-third-parties`; 0 impacto productivo.
2. **Audit display util boundary**: mover `audit-display.util.ts` desde `features/audit-shared` a `shared` y actualizar `audit` + `auth-logs`; luego retirar `audit-shared` si queda vacío.
3. **ACH cycle list SCSS boundary**: eliminar el `@use` a `admin/users-list.component.scss` extrayendo solo el layout reusable a shared/foundation o dejando estilo local mínimo.
4. **Navigation roles boundary**: mover `RoleSummary` y `RolesApiService` a core, sin mover `UsersApiService`/modelos admin no compartidos.
5. **Clearing-house cycle-config API/model boundary**: mover a core `ClearingHouseCycleConfigsApiService` y los contratos cycle-config compartidos necesarios; cerrar `operational-restrictions -> transactions`.
6. **Customer API/model boundary**: mover a core `CustomersApiService` + contratos customer necesarios; actualizar customers y transaction-create solo por imports.
7. **ACH Colombia file-exchange/MFT boundary**: mover a core el API/contratos compartidos entre file exchange y MFT administration, cuidando las dependencias `Transfer*` del mismo service.
8. **CENIT reports boundary**: mover/extractar a core el API y contratos de reports usados por CENIT sin duplicar endpoints ni cambiar `ReportsApiService` funcionalmente.
9. **NACHA config UAT boundary**: resolver `NachaConfigApiService` + `NachaConfigProfileReadModel`; es más delicado porque el service mezcla lecturas compartidas con mutaciones administrativas.
10. **SOAP integration page ownership**: mover `SoapIntegrationSettingsComponent` de `admin` a `integrations` preservando `/integraciones/soap-settings`; no moverlo a shared/core.
11. **Incoming NACHA page ownership**: mover dashboard/detail desde `nacha-operational` al owner `incoming-nacha-command-center`, preservando redirects/rutas legacy y módulos. Es el lote más sensible por routing y compatibilidad de navegación.

No se propone ningún micro-lote adicional sobre B9a1–B9a7 porque el escaneo no detectó reintroducciones de esos boundaries.

## Estado final exacto

`SPA_4C3B9A8_RESIDUAL_BOUNDARY_INVENTORY_COMPLETE_NO_IMPLEMENTATION`
