# SPA Foundation 4C.1 Checkpoint

## Foundation

- Visual foundation: Angular Material 21 remains the primary visual foundation. The existing static Material palette is retained only to generate baseline structure; runtime system tokens are bridged to shared design tokens.
- Branding: `BrandingSettings -> BrandingService -> runtime CSS design tokens -> Material system tokens + login/shell/shared`. `BrandingService` is the only runtime CSS-variable application point.
- Product name: `ACH Interbank` remains a centralized frontend fallback because `BrandingSettings` has no product-name field; no backend contract was added.
- Shared isolation: the reusable clearing-house catalog service/contract lives in `core`; `shared` no longer imports `features/*`.

## Canonical primitives

- Page header: `PageHeaderComponent` (`app-page-header`). `ui-encabezado-pagina` is a compatibility adapter for gradual migration.
- Confirmation: `OperationalConfirmDialogComponent` (Material dialog), including primary or destructive confirmation. Existing inline/modal variants remain compatibility debt.
- Loading / empty / recoverable error: `UiEstadoCargaComponent`, `UiEstadoVacioComponent`, `UiEstadoErrorComponent`; contextual copy remains configurable.
- Status/badge: `UiEtiquetaEstadoComponent`, backed by semantic success/warning/error tokens.
- Tables: use Angular Material table primitives for new/simple shared tables; keep `UiGrillaEmpresarialComponent` / AG Grid where enterprise grid behavior is required. `TableComponent` remains a compatibility wrapper and is not a universal MatTable/AG Grid abstraction.

## Deliberately deferred

- 4C.2: feature migration onto this foundation.
- 4C.3: legacy UI retirement, remaining architecture/core debt, Alias SPA retirement and broader primitive-consumer cleanup.
- 4C.4: transversal UX-language/error humanization and contextual-copy migration.
- `PagedResponse<T>`: the `{ items, total, page, pageSize }` shape is centralized in `core`; the ACH responses `{ pageNumber, totalCount, totalPages }` variant remains separate intentionally.

## Validation

- Node: `v22.16.0`.
- npm: `10.9.2`.
- Focused tests: NOT EXECUTED — BLOCKED_BY_ENVIRONMENT. The single allowed `npm ci` attempt did not leave a usable dependency tree.
- SPA build: NOT EXECUTED — BLOCKED_BY_ENVIRONMENT for the same dependency-installation issue.
- Static validation: PASS, including zero `shared -> features` imports, a single runtime branding application point, no second theme/branding manager, no new foundation brand hexes, unchanged backend trees, and valid relative SPA imports.
- Solution project validation: PASS (`scripts/validate-solution-projects.py`).
