# SPA 4C.3B.0 — Inventario Transactions + Clearing Houses + Cycles

## 1. Baseline y alcance

Baseline obligatorio inspeccionado:

`ACHInterbank-v31-spa-4C3A3b3-ach-responses-status-mappings.zip`

Este lote es **solo de inspección, clasificación y partición**. No se implementó migración visual, no se modificó comportamiento, no se alteró backend, no se generaron migrations y no se avanzó a 4C.3B1.

Fuentes de contexto leídas antes del inventario:

- `AGENTS.md`
- `MEMORY.md`
- `docs/ai/SPA-INTEROPERABILITY-UX-ARCH-4A-DIAGNOSTIC.md`
- `docs/ai/SPA-FOUNDATION-4C1-CHECKPOINT.md`
- `docs/ai/SPA-4C3A3B3-ACH-RESPONSES-STATUS-MAPPINGS-CHECKPOINT.md`

Los diagnósticos históricos se usaron únicamente como contexto. Las conclusiones de este documento se verificaron contra el código actual de v31.

## 2. Resumen ejecutivo

El baseline actual mantiene **cuatro conceptos de ciclo diferentes** que no deben fusionarse:

1. `AchCycle`: instancia operativa de un ciclo para una fecha/cámara, administrada por `/ach-cycles` y persistida en `AchCycles`.
2. `ClearingHouseCycleConfig`: configuración efectiva/versionada de ventanas y clases de transacción permitidas por cámara, administrada canónicamente por `/clearing-houses/:id/cycles` y persistida en `ClearingHouseCycleConfigs`.
3. Scheduler/cortes: ejecución automática que consume la configuración vigente para crear/operar ciclos; la SPA del scheduler declara tareas `usesCycleSchedule` y no permite editar directamente el horario de esas tareas.
4. Políticas transaccionales por cámara: reglas de prenotificación/validación/prerrequisitos, administradas por `/clearing-houses/:id/transaction-policies` y persistidas sobre `ClearingHouseTransactionRules`.

También se verificaron dos deudas concretas de routing/ownership:

- `/transactions/cycle-configs` apunta a `CycleConfigManagementComponent`, pero la pantalla exige `route.paramMap.get('id')`; esa ruta no suministra `:id`, por lo que hoy es una ruta no canónica estructuralmente inválida. El mismo componente funciona en la ruta canónica `/clearing-houses/:id/cycles`.
- `ClearingHouseTransactionRulesComponent` y `ClearingHouseTransactionRulesApiService` siguen presentes en `features/transactions`, pero el componente no tiene ruta ni consumidor de producción; `/transactions/clearing-house-rules` es un redirect a `/clearing-houses`. Debe tratarse como residual frontend, sin inferir que el backend de reglas sea residual.

La deuda de acoplamiento pendiente después de 4C.2 también sigue presente: `nacha-security/components/nacha-certificate-manager.component.ts` importa `DestinationInstitution` y `FinancialInstitutionsApiService` desde `features/transactions`. Existe además un patrón más amplio de contratos/query services reutilizables alojados en features y consumidos desde otras features.

## 3. Inventario frontend — `transactions`

### 3.1 Estructura

- Módulo: `transactions.module.ts`.
- Routing: `transactions-routing.module.ts` + spec.
- Componentes standalone/routed:
  - `TransactionListComponent`
  - `TransactionCreateComponent`
  - `TransactionBulkCreateComponent`
  - `BulkIngestionUploadComponent`
  - `BulkIngestionTrackingComponent`
  - `BulkIngestionDetailComponent`
  - `NachaUploadComponent`
  - `AchReturnsManagementComponent`
  - `AchReturnOfReturnManagementComponent`
  - `CycleConfigManagementComponent`
  - `OutgoingTransactionMonitoringListComponent`
  - `OutgoingTransactionMonitoringDetailComponent`
- Componentes no routed:
  - `TransactionIntegrationResultComponent`, usado como presentación auxiliar del resultado de integración.
  - `ClearingHouseTransactionRulesComponent`, sin consumidor de producción encontrado; solo componente/spec.
- Servicios:
  - `TransactionsApiService`
  - `BulkIngestionApiService`
  - `BulkIngestionTrackingApiService`
  - `NachaUploadService`
  - `AchReturnsApiService`
  - `ReturnReasonsApiService`
  - `FinancialInstitutionsApiService`
  - `ClearingHouseCycleConfigsApiService`
  - `ClearingHouseTransactionRulesApiService`
  - `OutgoingTransactionMonitoringApiService`
- Modelos/helpers:
  - `transactions.models.ts`
  - `transactions.types.ts`
  - `transaction-policy.validators.ts`
  - `outgoing-monitoring/outgoing-transaction-monitoring.models.ts`
- Tamaño actual: 79 archivos; 30 TS no-spec, 21 specs, 14 templates HTML y 14 SCSS.

### 3.2 Estado de foundation/UI

| Elemento | Estado actual |
| --- | --- |
| `app-page-header` | presente en 9 templates: returns, ROR, bulk upload/tracking/detail, rules, NACHA upload, bulk create y list |
| `<ui-boton>` | presente todavía en ROR, bulk upload/tracking/detail y transaction list; 22 pares apertura/cierre detectados en la feature |
| `UiEstadoVacio/Error` | usado explícitamente solo en transaction list |
| `UiEstadoCarga` | sin consumo explícito en la feature |
| `UiEtiquetaEstadoComponent` | sin consumo |
| `UiGrillaEmpresarial` | usado por list, returns, bulk create, NACHA upload, bulk tracking/detail y rules |
| MatTable | `cycle-config-management` y outgoing monitoring list |
| Material | mezcla amplia; forms/cards/table/dialog/etc. ya presentes en varias pantallas |
| CSS foundation tokens | presentes en varias pantallas nuevas, pero no uniformemente |
| hardcoded hex | 107 ocurrencias en 11 SCSS; especialmente altos en `transaction-create`, `cycle-config-management` y rules |

Casos de deuda visual particularmente claros:

- `transaction-create` no usa `app-page-header` y conserva 34 colores hex locales.
- `cycle-config-management` no usa `app-page-header`, usa Material/MatTable pero 18 hex locales.
- `clearing-house-transaction-rules` usa grilla empresarial pero CSS completamente local, sin tokens foundation.
- outgoing monitoring ya usa Material system tokens (`--mat-sys-*`) y no tiene hex locales, pero no usa `app-page-header` ni shared states.

## 4. Inventario frontend — `clearing-houses`

### 4.1 Estructura

No existe `ClearingHousesModule` ni routing module propio. Las rutas se declaran directamente en `app-routing.module.ts` con lazy standalone components.

Archivos principales:

- `ClearingHousesComponent` — administración de cámaras.
- `TransactionPoliciesComponent` — políticas transaccionales/versionadas por cámara.
- `ClearingHouseContextNavigationComponent` — navegación contextual entre políticas, ciclos y fechas especiales.
- `ConfirmationDialogComponent` y `CloseValidityDialogComponent` en `clearing-house-dialogs.component.ts`.
- `ClearingHousesService` + `clearing-houses.models.ts`.
- `TransactionPoliciesService` + `transaction-policies.models.ts`.
- 17 archivos totales: 8 TS no-spec, 4 specs, 2 HTML y 3 SCSS.

### 4.2 Estado de foundation/UI

| Elemento | Estado actual |
| --- | --- |
| `<ui-boton>` | 0 |
| `app-page-header` | 0 |
| `UiEstadoCarga/Vacio/Error` | 0 |
| `UiEtiquetaEstadoComponent` | 0 |
| `UiGrillaEmpresarial` | 0 |
| Material | alto: MatTable, forms, dialog, tabs, chips, cards, paginator, etc. |
| MatTable | administración de cámaras y políticas transaccionales |
| estados loading/error/empty | implementados localmente principalmente con Material cards/spinners |
| hardcoded hex | 55 ocurrencias en los tres SCSS; no usa tokens foundation |

`ClearingHouseContextNavigationComponent` enlaza canónicamente a:

- `/clearing-houses/:id/transaction-policies`
- `/clearing-houses/:id/cycles`
- `/clearing-houses/:id/special-dates`

No enlaza a preferencias ni a restricciones operacionales.

## 5. Inventario frontend — `ach-cycles`

### 5.1 Estructura

- `ach-cycles.module.ts`
- `ach-cycles-routing.module.ts`
- `AchCycleListComponent`
- `AchCycleFormComponent`
- `NachaExportComponent`
- `models/ach-cycle.model.ts`
- `models/ach-cycle-export.model.ts`
- `AchCyclesApiService`
- `NachaExportApiService`
- 18 archivos totales: 9 TS no-spec, 3 specs, 3 HTML y 3 SCSS.

`ach-cycle.model.ts` reexporta `ClearingHouseOption` desde `core/models/clearing-house.model.ts`, y `ach-cycles-api.service.ts` reexporta `ClearingHousesApiService` desde `core/services/clearing-houses-api.service.ts`. Esos reexports son una indirection histórica que hace parecer que otras features dependen de `ach-cycles` para algo que ya vive en `core`.

### 5.2 Estado de foundation/UI

| Elemento | Estado actual |
| --- | --- |
| `<ui-boton>` | 0 |
| `app-page-header` | lista y exportación; formulario no |
| shared loading/empty/error | 0 |
| `UiEtiquetaEstadoComponent` | 0 |
| `UiGrillaEmpresarial` | 0 |
| lista | usa `app-table` legacy + inputs/selects/buttons nativos |
| form | inputs/select/checkbox/buttons nativos + `app-error-message` |
| export | Material + MatTable, pero estados locales |
| hardcoded hex | 13 ocurrencias; list/export |

## 6. Matriz de rutas actuales

### 6.1 Transactions

El parent `/transactions` aplica `roleGuard` + `permissionGuard`, con roles `Admin` o `ACH.Operator` y permisos `CanManageAch` o `CanReadAch`. `AuthService.hasRole/hasPermission` usa semántica **any-of** para los arrays.

| Ruta | Componente | Servicio/API principal | Permiso frontend declarado | Clasificación |
| --- | --- | --- | --- | --- |
| `/transactions` | redirect | — | parent | redirect a `/transactions/list` |
| `/transactions/list` | `TransactionListComponent` | GET `api/transactions`; GET `api/transactions/{id}/integration-result`; GET `api/ach-cycles`; GET `api/clearing-houses/operational` | `CanReadAch` | canónica; consulta |
| `/transactions/create` | `TransactionCreateComponent` | GET policy/company descriptions/institutions/customers; POST `api/transactions` | `CanManageAch` | canónica; escribe estado |
| `/transactions/bulk-create` | `TransactionBulkCreateComponent` | POST `transactions/bulk/submit` | `CanManageAch` | canónica; escribe estado |
| `/transactions/bulk-ingestion/upload` | `BulkIngestionUploadComponent` | POST `transactions/bulk-ingestion/upload` | `CanManageAch` | canónica; escribe estado |
| `/transactions/bulk-ingestion/tracking` | `BulkIngestionTrackingComponent` | GET batch; POST retry | `CanManageAch` | canónica; consulta + comando |
| `/transactions/bulk-ingestion/:batchId` | `BulkIngestionDetailComponent` | GET items/summary; POST retry/cancel | `CanManageAch` | canónica; consulta + comandos |
| `/transactions/nacha-upload` | `NachaUploadComponent` | POST `NachaUpload/upload`; GET `NachaUpload/records` | `CanManageAch` | canónica; upload + consulta |
| `/transactions/returns` | `AchReturnsManagementComponent` | GET `ach-returns/cycles/{cycleId}/transactions`; POST `ach-returns/generate-file`; GET return reasons | `CanManageAch` | canónica; consulta + generación |
| `/transactions/returns-ror` | `AchReturnOfReturnManagementComponent` | POST evaluate/audit-file/NACHA-file | `CanManageAch` | canónica; evaluación + generación |
| `/transactions/outgoing-monitoring` | `OutgoingTransactionMonitoringListComponent` | GET `api/transactions/outgoing-monitoring` + catálogos | `OutgoingTransactions.Monitor.Read` o `CanReadAch` o `CanManageAch` | canónica; read-only |
| `/transactions/outgoing-monitoring/:id` | `OutgoingTransactionMonitoringDetailComponent` | GET `api/transactions/outgoing-monitoring/{id}` | mismo que listado | canónica; read-only |
| `/transactions/cycle-configs` | `CycleConfigManagementComponent` | `clearing-house-cycle-configs` | `CanManageAch` | **no canónica / defectuosa**: el componente requiere `:id` y esta ruta no lo provee |
| `/transactions/clearing-house-rules` | redirect | — | parent | legacy redirect a `/clearing-houses` |

`ClearingHouseTransactionRulesComponent` no figura en el routing actual. Su service usa `api/clearing-house-transaction-rules` y `api/transaction-prerequisite-policy/preview`, pero solo el componente residual y sus specs lo consumen en frontend.

### 6.2 Clearing houses y superficies contextuales

| Ruta | Ubicación física del componente | APIs | Permisos frontend | Clasificación |
| --- | --- | --- | --- | --- |
| `/clearing-houses` | `features/clearing-houses` | `api/clearing-houses` + profiles/payment rails | `ClearingHouses.View` parent; capacidades de escritura se validan además en UI/API | canónica; administración |
| `/clearing-houses/:id/cycles` | **`features/transactions`** | `clearing-house-cycle-configs` | `ClearingHouses.View` o `ClearingHouses.ManageCycles` | canónica; configuración versionada de ciclos |
| `/clearing-houses/:id/transaction-policies` | `features/clearing-houses` | `api/clearing-houses/{id}/transaction-policies` | `Config.Read`, `Config.Manage`, `CanReadAch`, `CanManageAch` | canónica; configuración transaccional |
| `/clearing-houses/:id/special-dates` | **`features/catalogs`** | `api/clearing-house-special-dates` | `ClearingHouses.View` o `ClearingHouses.ManageSpecialDates` | canónica; calendario especial por cámara |
| `/catalogs/clearing-house-preferences` | `features/catalogs` | `institution-clearing-house-preferences` | `CanManageAch` | canónica para prioridades/preferencias de institución-cámara; no está dentro del context nav |
| `/catalogs/clearing-house-special-dates` | `features/catalogs` | no opera datos directamente | permisos de special dates | legacy redirect al contexto `/clearing-houses/:id/special-dates` |

Administración de cámaras usa backend fine-grained `ClearingHouses.View/Create/Update/ChangeStatus`. Fechas especiales usa `ClearingHouses.View/ManageSpecialDates`. Preferencias sigue protegida por `CanManageAch`.

### 6.3 ACH cycles

| Ruta | Componente | API | Permiso frontend | Clasificación |
| --- | --- | --- | --- | --- |
| `/ach-cycles` | `AchCycleListComponent` | GET `api/ach-cycles` | `CanReadAch` | canónica; consulta de instancias operativas |
| `/ach-cycles/new` | `AchCycleFormComponent` | POST `api/ach-cycles`; GET config vigente | `CanManageAch` | canónica; crea instancia operativa |
| `/ach-cycles/:id/edit` | `AchCycleFormComponent` | GET/PUT `api/ach-cycles/{id}`; GET config vigente | `CanManageAch` | canónica; modifica instancia operativa |
| `/ach-cycles/nacha/export` | `NachaExportComponent` | GET `api/ach-cycles/exportable`; GET `NachaExport/{cycleId}` / sobre digital | `CanReadAch` | canónica; consulta/descarga |
| `/ach-cycles/nacha/layouts` | redirect | — | — | legacy -> `/not-found` |
| `/ach-cycles/nacha/definitions` | redirect | — | — | legacy -> `/not-found` |
| `/nacha-layouts` | root redirect | — | — | legacy -> `not-found` |
| `/nacha-record-definitions` | root redirect | — | — | legacy -> `not-found` |

El menú persistido contiene `/ach-cycles` y `/ach-cycles/nacha/export`, y la navegación frontend filtra las rutas NACHA legacy. La lista de ciclos conserva además un link visible **`/transactions/cycle-configs`**, que conduce a la ruta no canónica descrita arriba.

## 7. Backend/endpoints y políticas relevantes

No se modificó backend; solo se inspeccionó para identificar el contrato real detrás de las rutas.

### 7.1 Transactions

- `TransactionsController`: rutas `[controller]` y `api/transactions`.
  - GET list / company-entry-descriptions / detail / integration-result / integration-readiness: `P0.TransactionsRead` = `Transactions.Read` o fallback `CanReadAch`.
  - GET `policies/preview`: `P0.TransactionsPolicyPreview` = `Transactions.PolicyPreview` o `CanReadAch`.
  - POST create: `P0.TransactionsCreate` = `Transactions.Create` o `CanManageAch`.
  - POST `bulk` y `bulk/submit`: `P0.TransactionsBulkSubmit` = `Transactions.BulkSubmit` o `CanManageAch`.
- `BulkIngestionController`, base `api/transactions/bulk-ingestion`:
  - upload: `BulkIngestion.Upload` o `CanManageAch`.
  - read/items/summary: `BulkIngestion.Read` o `CanReadAch`.
  - retry/cancel: fine-grained correspondiente o `CanManageAch`.
- `NachaUploadController`, base `NachaUpload`:
  - POST `upload`: `Nacha.Upload` o `CanManageAch`.
  - GET `records`: `Nacha.Read` o `CanReadAch`.
- Returns:
  - `AchReturnsController`: `Returns.Read`/`Returns.GenerateFile` con fallbacks ACH generales.
  - `AchReturnOfReturnController`: evaluate = read; generation endpoints = generate-file.
- Outgoing monitoring:
  - `OutgoingTransactionMonitoringController`, base `api/transactions/outgoing-monitoring`.
  - Read policy acepta `OutgoingTransactions.Monitor.Read`, `CanReadAch` o `CanManageAch`.
  - Detalle técnico requiere policy específica o `CanManageAch`.

### 7.2 Clearing house configuration

- `ClearingHousesController`, `clearing-houses` y `api/clearing-houses`: View/Create/Update/ChangeStatus; además `operational` permite `CanReadAch`.
- `ClearingHouseCycleConfigsController`, `clearing-house-cycle-configs`:
  - GET/list/current: `ClearingHouses.View`.
  - POST/create version, inactivate, status: `ClearingHouses.ManageCycles`.
- `ClearingHouseTransactionPoliciesController`, `api/clearing-houses/{id}/transaction-policies`:
  - read/current/get/preview: `Config.Read` o `CanReadAch`.
  - create version/update metadata/close/activate: `Config.Manage` o `CanManageAch`.
- `ClearingHouseTransactionRulesController`, `api/clearing-house-transaction-rules`, usa el **mismo `IClearingHouseTransactionRuleService`** que el controller de transaction policies, pero expone la API CRUD histórica de reglas. El frontend actual de rules está unrouted; esto no convierte al backend en residual.
- `TransactionPrerequisitePolicyController`, `api/transaction-prerequisite-policy/preview`: `CanReadAch`.
- `ClearingHouseSpecialDatesController`: View/ManageSpecialDates.
- `InstitutionClearingHousePreferencesController`: `CanManageAch`.

### 7.3 ACH cycles

- `AchCyclesController`, `ach-cycles` y `api/ach-cycles`:
  - GET/list/exportable/get: `CanReadAch`.
  - POST/PUT/repair/delete: `CanManageAch`.
- `NachaExportController`, base `NachaExport`: GET normal y sobre digital con `CanReadAch`.

## 8. Acoplamientos entre features

Clasificación usada:

- **A — legítima/deliberada:** dependencia semántica real, sin indicio actual de residualidad.
- **B — deuda arquitectónica:** ownership/layering incorrecto o UI de una feature alojada en otra.
- **C — contrato/query/helper reutilizable:** el concepto es legítimamente compartido pero su ubicación en una feature fuerza acoplamiento; candidato a `core`/shared data-access/model.
- **D — navegación únicamente:** vínculo por router, no import funcional.
- **E — residual/no usado:** sin consumidor productivo/ruta activa encontrado.

### 8.1 Desde `transactions`

| Origen | Import destino | Símbolo | Clasificación | Opción mínima futura |
| --- | --- | --- | --- | --- |
| `transaction-list.component.ts` | `ach-cycles` | `AchCyclesApiService`, `AchCycleSummary`, `ClearingHouseOption`, reexport `ClearingHousesApiService` | C | consumir un read model/query boundary en core; para clearing house, importar directamente el servicio/modelo ya existentes en `core` |
| `ach-returns-management.component.ts` | `ach-cycles` | `AchCyclesApiService`, `AchCycleSummary` | C | extraer consulta de ciclos reutilizable; no mover lógica de returns |
| `clearing-house-transaction-rules.component.ts` | `ach-cycles` | reexport `ClearingHousesApiService` | C/E | si la pantalla sobrevive, importar el servicio core directamente; hoy la pantalla es residual |
| `cycle-config-management.component.ts` | `clearing-houses` | models/service/context nav/dialogs | **B** | ubicar la superficie canónica de ciclos bajo ownership clearing-house o crear un boundary local claro; no cambiar endpoint/modelo |
| `transaction-create.component.ts` | `customers` | `CustomersApiService`, `CustomerSummary` | A/C | dependencia funcional real; si se busca aislamiento estricto, extraer solo el query model/service reutilizable |

### 8.2 Hacia `transactions`

| Consumidor | Símbolo importado desde transactions | Clasificación | Opción mínima futura |
| --- | --- | --- | --- |
| `nacha-security/nacha-certificate-manager.component.ts` | `DestinationInstitution`, `FinancialInstitutionsApiService` | **C — deuda explícita pendiente de 4C.2** | mover/reutilizar un `FinancialInstitutionOption` + read service en `core`; luego ambos consumidores importan core |
| `catalogs/financial-institutions*` | `DestinationInstitution`, `FinancialInstitutionStatusEnum` | C | mover contratos administrativos/compartidos a boundary neutral; catalogs no debe depender de transactions |
| `catalogs/clearing-house-preferences.component.ts` | financial-institution model/service/status | C | mismo boundary de institución financiera |
| `clearing-houses/transaction-policies*` | `TransactionTypeEnum` | **C** | mover enum/contrato transaccional compartido a core/model común, sin cambiar valores |
| `operational-restrictions*` | `ClearingHouseCycleConfigsApiService`, `ClearingHouseCycleConfigItem`, `TransactionTypeEnum`, `AccountTypeEnum` | C/B | extraer contratos/query de ciclo y enums; mantener restricciones como feature propia |
| `ach-claims/ach-claims-list.component.ts` | financial-institution model/service | C | mismo read boundary neutral |
| `uat/nacha-inbound-simulator` | financial-institution model/service | C | mismo read boundary neutral; UAT no debe forzar ownership en transactions |

### 8.3 Desde/hacia `clearing-houses`

- `catalogs/clearing-house-special-dates.component.ts` importa `ClearingHouse`, `ClearingHousesService`, context navigation y dialog local de `clearing-houses`: **B/C**. La ruta canónica ya es clearing-house contextual, pero el código vive en catalogs.
- `nacha-operational-dashboard.component.ts` importa `ClearingHousesService`: **C**; la consulta de cámaras es reutilizable y ya existe un query service más liviano en `core` para opciones operativas, aunque el dashboard puede requerir un modelo más rico.
- `operational-restrictions` importa servicio/model/dialog de clearing-houses: **C/B**, especialmente el dialog de UI local.
- `uat/nacha-inbound-simulator` importa `ClearingHousesService` y `NachaProfileOption`: **C**.

### 8.4 Desde/hacia `ach-cycles`

- `reports/traceability-report.component.ts` importa `AchCyclesApiService`: **C**.
- `catalogs/clearing-house-preferences` y `uat/nacha-inbound-simulator` importan `ClearingHousesApiService`/`ClearingHouseOption` a través de **reexports de ach-cycles**, aunque las implementaciones reales están en `core`: **C con indirection residual**. La corrección mínima es importar directamente desde core.
- `transactions` consume `AchCyclesApiService`/models como se indicó arriba: **C**.

### 8.5 Navegación-only

Los enlaces router entre transaction pages, la navegación contextual de clearing houses y los redirects legacy son **D** cuando no arrastran imports de dominio/service. El problema de `/transactions/cycle-configs` no es solo navegación: la ruta activa monta un componente que exige un parámetro ausente.

## 9. Deuda de acoplamiento heredada de 4C.2 — confirmación concreta

La deuda pendiente sigue confirmada en v31:

- **Origen:** `web/ach-interbank-ui/src/app/features/nacha-security/components/nacha-certificate-manager.component.ts`
- **Símbolos:** `DestinationInstitution`, `FinancialInstitutionsApiService`.
- **Destino actual:** `features/transactions/transactions.models.ts` y `features/transactions/services/financial-institutions-api.service.ts`.
- **Consumidores adicionales del mismo boundary:** catalogs, ach-claims y UAT.
- **Endpoint real:** GET `financial-institutions`, backend `FinancialInstitutionsController`, lectura con `CanReadAch`.
- **Opción mínima de desacople:** crear/reutilizar en `core` un contrato de opción de institución financiera y un query service read-only sobre el mismo endpoint, migrando imports sin cambiar API, DTO backend ni lógica NACHA Security. La administración de instituciones puede permanecer en catalogs.

No se realizó el movimiento en este lote.

## 10. Ciclos: separación de modelos reales

### 10.1 A — Instancias operativas `AchCycle`

**Frontend**

- `/ach-cycles`, `/ach-cycles/new`, `/ach-cycles/:id/edit`.
- `AchCycleListComponent`, `AchCycleFormComponent`.
- `AchCyclesApiService`.
- DTOs TS: `AchCycleSummary`, `AchCycleFilter`, `SaveAchCycleRequest`, `PagedAchCycleResponse`, `AchCycleConfigurationOption`.

**Backend**

- Controller: `AchCyclesController`.
- Application contract: `IAchCycleAppService`.
- Implementation: `AchCycleAppService`.
- Entity: `Domain/Models/ACH/AchCycle.cs`.
- Persistence: `AchDbContext.AchCycles`.
- Relación: `AchCycle.ClearingHouseCycleConfigId` apunta a la configuración que originó la instancia.

**Semántica observada**

Un `AchCycle` tiene `ProcessingDate`, ventana, cutoff, estado operativo y relación a cámara; es una **instancia operativa fechada**, no la definición efectiva/versionada de la política de ciclos.

Existe `AchCycleTemplate`, pero no se encontró ningún consumidor actual fuera de su propia declaración. Debe clasificarse como residual/inactivo hasta nueva evidencia; no es la fuente runtime del catálogo.

### 10.2 B — Configuración efectiva/versionada `ClearingHouseCycleConfig`

**Frontend**

- Ruta canónica: `/clearing-houses/:id/cycles`.
- Componente físico actual: `features/transactions/components/cycle-config-management`.
- Service: `ClearingHouseCycleConfigsApiService`.
- DTOs TS: `ClearingHouseCycleConfigItem`, filters, `UpsertCycleConfigRequest`, `InactivateCycleConfigRequest`.

**Backend**

- Controller: `ClearingHouseCycleConfigsController`.
- Application contract: `IClearingHouseCycleConfigService`.
- Implementation: `ClearingHouseCycleConfigService`.
- Entity: `ClearingHouseCycleConfig`.
- Persistence: `AchDbContext.ClearingHouseCycleConfigs`.

**Campos/semántica**

Incluye versión de política, nombre de ciclo, start/end/cutoff/output release, ventanas de planilla/pago/liberación, permisos por clase de transacción, vigencia e `IsActive`. Esta configuración gobierna la creación y comportamiento de ciclos; no es un `AchCycle` operativo.

### 10.3 C — Scheduler/cortes

**Frontend relacionado**

- `/scheduler/tasks`, `TaskDefinitionsComponent`, permiso `Scheduler.View`.
- El modelo expone `usesCycleSchedule`, `canEditSchedule` y `operationalContexts`.
- Para tareas cycle-governed, la UI muestra “Gobernada por ciclos” y bloquea la edición directa del schedule; remite a la configuración del ciclo.

**Backend**

- `SchedulerTaskCatalog` registra, entre otros:
  - `AchCycleSeeder` — “Actualizar ciclos de compensación”.
  - `AchCycleScheduler` — “Preparar ciclos operativos”.
  - `AchContrapartidasByCycle` — despacho gobernado por ciclo.
- `AchCycleSchedulerHandler` obtiene `IAchCycleScheduler`.
- `AchCycleScheduler` resuelve la política vigente mediante `IClearingHouseCyclePolicyResolver`, calendario/ventana operativa y crea `AchCycle` persistiendo `ClearingHouseCycleConfigId`.

Los cortes y ventanas provienen de la política de ciclo; scheduler es consumidor/orquestador, no una segunda configuración editable equivalente.

### 10.4 D — Transaction policies por cámara

**Frontend**

- `/clearing-houses/:id/transaction-policies`.
- `TransactionPoliciesComponent` + `TransactionPoliciesService`.
- DTOs: `TransactionPolicy`, create-version, update-metadata y preview.

**Backend**

- Controller canónico: `ClearingHouseTransactionPoliciesController`.
- Servicios: `IClearingHouseTransactionRuleService` + `ITransactionPrerequisitePolicyService`.
- Implementaciones: `ClearingHouseTransactionRuleService`, `TransactionPrerequisitePolicyService`.
- Entidad/persistencia: `ClearingHouseTransactionRule`, `AchDbContext.ClearingHouseTransactionRules`.

Estas reglas gobiernan prenotificación/validación/prerrequisitos, no las ventanas del scheduler.

### 10.5 Catálogo regulatorio/seeding

`RegulatoryCycleScheduleCatalog` es una fuente estática de fingerprints/policies regulatorias y bootstrap usada por seeders, runtime policy support y tests. No tiene ruta SPA y no debe confundirse con una pantalla administrativa. `ClearingHouseCycleConfigSeeder` materializa la baseline histórica en configuración persistida.

## 11. Clearing houses — ownership actual

| Capacidad | Ubicación frontend real | Ruta canónica | Nota |
| --- | --- | --- | --- |
| administración de cámaras | `features/clearing-houses` | `/clearing-houses` | ownership coherente |
| navegación contextual | `features/clearing-houses` | embebida en subrutas | enlaza policies/cycles/special-dates |
| ciclos/configuración | **`features/transactions`** | `/clearing-houses/:id/cycles` | ownership físico inconsistente; endpoint propio de cycle config |
| políticas transaccionales | `features/clearing-houses` | `/clearing-houses/:id/transaction-policies` | ownership coherente, pero importa enum desde transactions |
| fechas especiales | **`features/catalogs`** | `/clearing-houses/:id/special-dates` | ownership físico inconsistente con ruta contextual |
| preferencias institución-cámara | `features/catalogs` | `/catalogs/clearing-house-preferences` | superficie separada, no context nav |
| restricciones operacionales CFA | `features/operational-restrictions` | `/operational-restrictions` | feature separada; consume cámara + cycle config y puede referenciar `ClearingHouseCycleConfigId` |

No se encontró evidencia para fusionar estas capacidades. Las similitudes de UI no significan equivalencia funcional.

## 12. Hallazgos de routing/navigation que requieren lote explícito

1. **`/transactions/cycle-configs`**: ruta activa no canónica, sin `:id`, mientras el componente requiere un id de cámara. El link “Reglas de procesamiento” de `AchCycleListComponent` apunta a esa ruta. Debe corregirse en un micro-lote de routing/ownership con pruebas, no durante una migración visual incidental.
2. **`/transactions/clearing-house-rules`**: redirect intencional a `/clearing-houses`; filtrado como legacy en `NavigationService`.
3. **`ClearingHouseTransactionRulesComponent`**: no routed y sin consumidor de producción; candidato a retiro frontend solo después de confirmar que no existe deep-link/documentación/uso externo requerido. El backend de reglas sigue activo y compartido con transaction-policies.
4. **`/catalogs/clearing-house-special-dates`**: legacy redirect al contexto de cámara; también filtrado como legacy.
5. **NACHA layouts/definitions bajo ach-cycles**: redirects a not-found, igualmente filtrados de navegación.

## 13. Partición propuesta de 4C.3B

La partición busca no mezclar read-only con comandos sensibles, ni ciclo operativo con configuración, ni visual migration con ownership/routing de alto riesgo.

### 4C.3B1 — Transactions query/monitoring

Alcance sugerido:

- `/transactions/list`
- `TransactionIntegrationResultComponent`
- `/transactions/outgoing-monitoring`
- `/transactions/outgoing-monitoring/:id`

Objetivo: foundation visual para superficies de consulta y reducción de acoplamiento trivial (por ejemplo imports de clearing-house core) sin tocar creación, bulk, returns ni cycle config.

### 4C.3B2 — Transaction create + bulk originación

Alcance sugerido:

- `/transactions/create`
- `/transactions/bulk-create`
- `/transactions/bulk-ingestion/upload`
- `/transactions/bulk-ingestion/tracking`
- `/transactions/bulk-ingestion/:batchId`

Preservar contratos P0/P1 y comandos retry/cancel. Separar cualquier desacople de instituciones financieras si requiere mover contratos compartidos.

### 4C.3B3a — NACHA upload

Alcance sugerido:

- `/transactions/nacha-upload`

Motivo para micro-lote propio: upload de archivo NACHA-M y consulta de records usan políticas/backend distintos; no mezclar con cambios de returns o cycles.

### 4C.3B3b — Returns y Return-of-Return

Alcance sugerido:

- `/transactions/returns`
- `/transactions/returns-ror`

Preservar generación de archivos, causal/eligibilidad y selección de ciclos. No alterar reglas NACHA-M ni Returns.

### 4C.3B4 — Clearing-house administration + context shell

Alcance sugerido:

- `/clearing-houses`
- `ClearingHouseContextNavigationComponent`
- dialogs locales solo en la medida necesaria para la propia feature

Objetivo: migrar administración de cámaras y shell contextual a 4C.1 sin tocar todavía cycle configs ni transaction policies.

### 4C.3B5 — Clearing-house cycle configuration + route ownership

Alcance sugerido:

- ruta canónica `/clearing-houses/:id/cycles`
- `CycleConfigManagementComponent`
- `ClearingHouseCycleConfigsApiService`/DTOs solo para desacople/ownership, **sin cambiar contratos**
- resolver explícitamente `/transactions/cycle-configs` y el link desde `/ach-cycles`

Este lote debe preservar `ClearingHouseCycleConfig` como concepto distinto de `AchCycle` y no tocar scheduler backend.

### 4C.3B6 — Transaction policies + residual rules decision

Alcance sugerido:

- `/clearing-houses/:id/transaction-policies`
- mover `TransactionTypeEnum` a boundary neutral si es necesario para eliminar `clearing-houses -> transactions`
- decisión explícita sobre frontend residual `ClearingHouseTransactionRulesComponent`/service; no eliminar backend por asociación.

Si el retiro residual resulta mayor que una limpieza obvia, dividirlo en **4C.3B6a active policy UI** y **4C.3B6b stale rules retirement**.

### 4C.3B7 — ACH operational cycles + NACHA export

Alcance sugerido:

- `/ach-cycles`
- `/ach-cycles/new`
- `/ach-cycles/:id/edit`
- `/ach-cycles/nacha/export`

Migrar lista/form/export conservando `AchCycle` como instancia operativa y su vínculo a `ClearingHouseCycleConfig`. Retirar reexports de clearing-house core solo si todos los consumidores se migran de forma segura.

### 4C.3B8 — Clearing-house adjacent configuration

Alcance sugerido:

- `/clearing-houses/:id/special-dates` (físicamente catalogs)
- `/catalogs/clearing-house-preferences`
- normalización de ownership/imports estrictamente necesaria para estas superficies

No mezclar `operational-restrictions` salvo que se abra un lote específico; hoy es un consumidor de cycle configs, no parte de la misma UI administrativa.

### 4C.3B9 — Cross-feature boundary cleanup final

Solo después de las migraciones de superficie:

- financial-institution read model/service neutral para transactions/nacha-security/catalogs/claims/UAT;
- cycle query/model neutral donde sea necesario;
- clearing-house direct imports desde `core` en lugar de reexports de ach-cycles;
- enums transaccionales compartidos fuera de `features/transactions`;
- revisión de imports features -> features restantes.

Este cierre debe ser estrictamente arquitectónico y no cambiar endpoints, payloads o permisos.

## 14. Orden y dependencias recomendadas

1. **B1** primero: read-only y bajo riesgo; da patrón visual para transactions.
2. **B2** después: comandos de originación/bulk con contratos preservados.
3. **B3a/B3b** separados por sensibilidad NACHA/Returns.
4. **B4** antes de B5/B6: foundation del shell clearing-house contextual.
5. **B5** después de B4: resolve ruta/ownership de cycle config con contexto estable.
6. **B6** después de B4: políticas y decisión residual de rules.
7. **B7** tras B5: `AchCycle` puede seguir consumiendo config vigente sin confundir ownership.
8. **B8** para superficies clearing-house alojadas en catalogs.
9. **B9** al final para quitar acoplamientos ya expuestos sin convertir cada migración visual en un refactor transversal.

No combinar B5 y B7: uno administra `ClearingHouseCycleConfig`; el otro administra `AchCycle` operativo.

## 15. Deuda deliberadamente no resuelta en 4C.3B.0

- No se retiró `<ui-boton>` ni CSS legacy.
- No se movieron componentes entre features.
- No se corrigió `/transactions/cycle-configs`.
- No se retiró `ClearingHouseTransactionRulesComponent`.
- No se modificaron reexports de `ach-cycles`.
- No se movieron `TransactionTypeEnum`, `DestinationInstitution`, financial-institution services ni cycle config contracts.
- No se tocaron `RegulatoryCycleScheduleCatalog`, seeders, scheduler, entities, controllers ni persistence.
- No se modificó NACHA Security.
- No se modificaron permisos/routing existentes.

## 16. Validación del lote de inventario

Validación realmente ejecutada:

- `python scripts/validate-solution-projects.py` -> **exit 0**, `SOLUTION GRAPH: OK`.
- Búsquedas estáticas focales de routes/imports/components/services/controllers/policies y consumidores.
- No se ejecutaron Angular tests/build ni backend tests porque este lote no modifica código ejecutable.

Hashes de referencia de v31 antes de agregar este documento:

- `features/transactions` — 79 archivos — `9e635f06ad64c95069731e84182cc7c9be5c064427d70136c281b96b9d0a0c0a`
- `features/clearing-houses` — 17 archivos — `8a185e2609762618c231863981e2e003695ff11f4a39ecb737f2eeb2987542ce`
- `features/ach-cycles` — 18 archivos — `a6a690fb6d69de58b187fc2a224755ecc351e370faecf1beee4842dcc51a9e3b`
- backend `src/` — 1475 archivos — `f76d894cf506902022e96c173ade1af702438a5e27c25a24b2fc83a604835be6`

El único cambio de 4C.3B.0 debe ser este documento de inventario/checkpoint.

## 17. Estado

`SPA_4C3B0_INVENTORY_COMPLETE_NO_IMPLEMENTATION`

No se avanzó a 4C.3B1.
