# SPA 4C.3C0a — NACHA Config Admin Inventory (READ-ONLY)

## Baseline e integridad

- Baseline: `ACHInterbank-v74-spa-4C3B9-checkpoint.zip`
- SHA-256 esperado: `3a9fc04c192f72453547747b4c41c5f9b21df75916f1a4345b7016cf9b4ea320`
- SHA-256 observado: `3a9fc04c192f72453547747b4c41c5f9b21df75916f1a4345b7016cf9b4ea320`
- Integridad del baseline: **PASS**.

Este lote es de inventario READ-ONLY. No se modificó source, backend, rutas, contratos/endpoints, permisos, HTML/SCSS/TS, `package*.json`, `tsconfig*` ni `angular.json`. Este documento es el único artefacto agregado.

## Boundary inventariado

`web/ach-interbank-ui/src/app/features/nacha-config-admin/` contiene **26 archivos**:

- 2 archivos de módulo/routing;
- 18 archivos bajo `pages/`;
- 2 archivos bajo `presentation/`;
- 4 archivos bajo `services/`.

Páginas productivas detectadas (sin duplicados de clase ni selector):

| Página | Ownership | Tipo | Responsabilidad observada |
|---|---|---|---|
| `NachaConfigProfilesPageComponent` | `pages/nacha-config-profiles-page.*` | NgModule, no standalone | dashboard/listado de perfiles, filtros/catálogos, creación de borrador, validación y navegación al workspace |
| `NachaConfigProfileWorkspacePageComponent` | `pages/nacha-config-profile-workspace-page.*` | NgModule, no standalone | detalle de perfil, edición de borrador, validar/publicar/inactivar/archivar/clonar y navegación a records/variants-fields |
| `NachaConfigRecordsPageComponent` | `pages/nacha-config-records-page.*` | standalone | selección de perfil, records y edición de secuencia para borrador |
| `NachaConfigVariantsFieldsPageComponent` | `pages/nacha-config-variants-fields-page.*` | standalone | variantes/campos/reglas, edición y presentación funcional/técnica |

`nacha-config-admin.module.ts` declara únicamente las dos páginas no-standalone y consume `SharedModule` + routing. Las páginas `records` y `variants-fields` son standalone y se referencian directamente desde routing.

## Rutas públicas, guards y permisos

Parent público en `app-routing.module.ts`:

- `/nacha-config-admin`
- `canActivate: [roleGuard, permissionGuard]`
- roles: `Admin`, `ACH.Operator`
- permissions: `Config.Read`, `Config.Manage`, `CanReadAch`, `CanManageAch`

Children canónicos en `nacha-config-admin-routing.module.ts` (todos repiten `roleGuard` + `permissionGuard` y el mismo set de roles/permisos):

- `/nacha-config-admin/perfiles` → `NachaConfigProfilesPageComponent`
- `/nacha-config-admin/records` → `NachaConfigRecordsPageComponent`
- `/nacha-config-admin/variants-fields` → `NachaConfigVariantsFieldsPageComponent`
- `/nacha-config-admin/perfiles/:id` → `NachaConfigProfileWorkspacePageComponent`
- `/nacha-config-admin` → redirect interno a `perfiles`

Compatibilidad existente fuera del boundary, preservada y solo inventariada:

- `/ach/nacha/config-profiles` → `/nacha-config-admin/perfiles` (`nacha-operational-routing.module.ts`).

Los guards usan semántica **OR** sobre los arrays (`AuthService.hasPermission()` / `hasRole()` emplean `some`). No se cambió esta semántica.

Nota de preservación: las páginas no usan exactamente la misma comprobación de permiso para habilitar acciones de escritura. `profiles` y `profile-workspace` consultan `['Config.Manage', 'CanManageAch']`, mientras `records` y `variants-fields` consultan `CanManageAch`. Es comportamiento actual a preservar durante una migración; no se intentó normalizarlo aquí.

## Services y contratos consumidos

### Facades del feature

`NachaConfigQueryService` delega lecturas en `core/services/nacha-config-api.service.ts`. Sus métodos actuales son:

- `perfiles()`
- `dashboardReadOnly()`
- `perfilesReadOnly()`
- `detalleReadOnly()`
- `variantesReadOnly()`
- `fieldsReadOnly()`
- `catalogosFiltro()`
- `detalle()`
- `historial()`
- `snapshots()`

Call sites productivos dentro del boundary:

- profiles: `dashboardReadOnly`, `perfilesReadOnly`, `catalogosFiltro`;
- profile-workspace: `detalle`;
- records: `perfilesReadOnly`, `detalle`;
- variants-fields: `perfilesReadOnly`, `detalle`.

No se encontraron call sites productivos del facade para `perfiles`, `detalleReadOnly`, `variantesReadOnly`, `fieldsReadOnly`, `historial` ni `snapshots`. Esto es inventario de deuda/candidatos; **no implica que sea seguro borrarlos** sin un lote específico.

`NachaConfigCommandService` encapsula normalización de errores y delega:

- create/edit/clone: `crearBorrador`, `editarBorrador`, `clonar`;
- lifecycle: `validar`, `publicar`, `inactivar`, `archivar`;
- detail mutation: `actualizarSecuencia`, `actualizarVariante`, `actualizarField`, `actualizarRule`;
- `preview`.

Las páginas consumen todos los anteriores salvo `preview`; el facade `preview` no tiene call site dentro de `nacha-config-admin`.

`NachaConfigStateService` mantiene `perfil$`, `validacion$` y `rowVersionActual()`, pero el scan de `src/app` no encontró ningún consumidor de la clase ni de sus métodos fuera de su propio archivo. Es el candidato residual arquitectónico más claro entre los services, pero no se modifica en este lote.

### Contratos/core

El feature depende de:

- `core/models/nacha-config.model.ts` para profile/detail/record/layout variant/field/rule, validation, dashboard/read models, filter catalogs y API error;
- `core/services/nacha-config-api.service.ts` como transporte;
- `core/services/nacha-config-read-retry.ts` para retries acotados de lecturas;
- `AuthService` y `NotificationService`;
- `roleGuard` y `permissionGuard`.

El API core expone dos familias de paths ya existentes, ambas consumidas por el feature/facades:

- read-only: `api/ach/nacha/config-profiles...` (`dashboard`, listado, detail, variants, fields);
- administración: `nacha-config...` (`perfiles`, `catalogos-filtro`, lifecycle, records/variantes/fields/rules, historial/snapshots, resolver-preview).

No se cambió ningún contrato ni endpoint.

## Imports y primitives compartidos

El scan global de TypeScript (`src/app/**/*.ts`, imports/exports estáticos y `import()`) arrojó:

```text
feature -> feature: 0
core -> features: 0
shared -> features: 0
```

Dentro de `nacha-config-admin`, las dependencias de aplicación son únicamente hacia `core`, `shared` o archivos locales del mismo feature. No hay import TypeScript a otro feature.

Shared UI/primitives observados en templates:

- navegación/layout: `ui-migas-pan`, `ui-encabezado-pagina`, `app-page-header`, `ui-tarjeta`;
- feedback/estado: `ui-alerta`, `ui-estado-carga`, `ui-estado-error`, `ui-estado-vacio`;
- forms/actions: `ui-boton`, `ui-campo-texto`, `ui-campo-textarea`, `ui-campo-fecha`, `ui-campo-numero`, `ui-error-campo`, `ui-modal-confirmacion`;
- data: `ui-grilla-empresarial`;
- TypeScript compartido directo: `OpcionSelectorBuscable` desde `shared/components/ui/ui-selector-buscable.component`.

También hay HTML nativo local (`button`, `select`, `input`, `table`, etc.), especialmente en `records` y `variants-fields`.

## Deuda visual / SCSS concreta

El boundary contiene **5 SCSS / 1.099 líneas** (incluyendo un SCSS residual señalado abajo). En los SCSS activos se observa una mezcla de tokens CSS y valores locales/hard-coded.

| SCSS | Líneas | Hex occurrences | CSS `var(...)` | Observación |
|---|---:|---:|---:|---|
| profiles | 132 | 8 | 8 | mezcla de tokens/fallbacks y hex directos |
| profile-workspace | 106 | 5 | 13 | mejor uso relativo de tokens, pero conserva `#fff` local |
| records | 148 | 12 | 7 | estados success/error/info con colores locales |
| variants-fields | 701 | 87 | 7 | concentración principal de deuda visual/local styling; 31 hex únicos, 3 media queries, 1 `!important` |
| official-readonly residual | 12 | 0 | 0 | solo reglas locales con `::ng-deep`; no tiene componente TS/HTML correspondiente |

Totales del boundary: **112 referencias hex**, **36 valores hex únicos**, **35 usos de CSS vars**, **2 `::ng-deep`** (ambos en el SCSS residual) y **1 `!important`**.

No hay `@use`, `@forward` o `@import` SCSS cross-feature ni metadata `styleUrl/styleUrls` apuntando a otro feature:

```text
SCSS directive cross-feature: 0
TS stylesheet metadata cross-feature: 0
```

## Duplicados / residual evidente

- **0** clases Angular Component duplicadas dentro del boundary.
- **0** selectors Angular duplicados dentro del boundary.
- `pages/nacha-config-official-readonly-page.component.spec.ts` no prueba un componente de ese nombre: importa y describe `NachaConfigRecordsPageComponent`; es casi duplicado del spec de records (el spec canónico contiene además un caso adicional de secuencias duplicadas/cancelación).
- `pages/nacha-config-official-readonly-page.component.scss` no tiene `.ts` ni `.html` homólogos y no aparece referenciado por `styleUrl/styleUrls`; es un orphan evidente.
- `NachaConfigStateService` no tiene consumidores encontrados en `src/app`; candidato residual, no eliminado.
- `services/nacha-config-api.service.spec.ts` vive dentro del feature pero prueba `core/services/NachaConfigApiService`; es deuda de ownership de tests, no un defecto funcional.
- El boundary mezcla dos páginas NgModule y dos standalone; no es un duplicado, pero sí una frontera de migración que conviene tratar incrementalmente.

## Riesgos para una migración posterior

1. **Permisos de escritura no uniformes** entre páginas: cualquier mecanización debe preservar exactamente los checks actuales.
2. **Contratos en core compartidos con otros consumers**: `NachaConfigApiService` también tiene consumidores fuera de `nacha-config-admin`; moverlo mecánicamente al feature rompería el boundary global y no es un primer paso seguro.
3. **Dos familias de API/read models** conviven en el mismo servicio; separar ownership sin cambiar contrato requiere lotes pequeños.
4. **`variants-fields` concentra complejidad** (1.257 líneas TS + 434 HTML + 701 SCSS) y no es un buen primer objetivo de refactor/move.
5. **Facades con métodos sin call site actual** deben tratarse como deuda documentada, no como borrado automático.

## Primer micro-cambio mecánico sugerido (NO implementado)

El primer cambio de menor riesgo sería **retirar únicamente el par orphan**:

- `pages/nacha-config-official-readonly-page.component.spec.ts`
- `pages/nacha-config-official-readonly-page.component.scss`

Motivo: no existe `nacha-config-official-readonly-page.component.ts/html`, el SCSS no tiene referencia de metadata y el spec realmente instancia `NachaConfigRecordsPageComponent`, duplicando casi por completo su spec canónico. Antes de ese futuro cambio bastaría repetir el reference scan y ejecutar tests Angular cuando exista `ng`.

No se propone mover API/models/state en ese primer micro-lote.

## Validaciones READ-ONLY

### Source/config y backend

El árbol de salida se comparó con una extracción prístina del baseline. Resultado esperado/observado después de agregar este documento:

```text
full-tree diff: solo docs/ai/SPA-4C3C0A-NACHA-CONFIG-ADMIN-INVENTORY.md
source/config diff: 0
backend diff: 0
```

### Angular CLI

Se comprobó **únicamente**:

```text
web/ach-interbank-ui/node_modules/.bin/ng
ng: not found
```

No se ejecutó `npm ping`, `npm ci`, registry retries, build ni tests.

Por tanto, este lote **no declara VERIFIED**.

## Evidencia / comandos

```bash
sha256sum ACHInterbank-v74-spa-4C3B9-checkpoint.zip

find web/ach-interbank-ui/src/app/features/nacha-config-admin -type f | sort
rg -n "nacha-config-admin|NachaConfigAdmin" web/ach-interbank-ui/src/app

nl -ba web/ach-interbank-ui/src/app/app-routing.module.ts | sed -n '255,275p'
nl -ba web/ach-interbank-ui/src/app/features/nacha-config-admin/nacha-config-admin-routing.module.ts | sed -n '1,90p'
nl -ba web/ach-interbank-ui/src/app/features/nacha-operational/nacha-operational-routing.module.ts | sed -n '38,43p'

python3 <read-only TypeScript/SCSS boundary resolver>
rg -n --glob '*.scss' '#[0-9A-Fa-f]{3,8}\\b|var\\(--|::ng-deep|!important' web/ach-interbank-ui/src/app/features/nacha-config-admin

rg -n "NachaConfigStateService" web/ach-interbank-ui/src/app
rg -n "nacha-config-official-readonly-page" web/ach-interbank-ui/src/app

test -x web/ach-interbank-ui/node_modules/.bin/ng && echo 'ng: found' || echo 'ng: not found'
```

## Estado

`4C3C0A_NACHA_CONFIG_ADMIN_INVENTORY_COMPLETE_ANGULAR_VALIDATION_PENDING`

No se avanzó al siguiente lote.
