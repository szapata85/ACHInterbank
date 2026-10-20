# NAV1 — Baseline y guardas

## Objetivo
Congelar evidencia antes de cambiar el menú.

## Tocar
Pruebas/documentación de navegación; no comportamiento productivo.

## Verificar
- `MenuItemConfiguration` y todos los seeders de `Persistence/Navigation/Seeders`.
- `DbInitializer`.
- `GetMenuForCurrentUserHandler`.
- `NavigationService` y `MainLayoutComponent`.
- rutas SPA y permisos.

## Entregables
- inventario actual actualizado si difiere de la matriz;
- tests de caracterización necesarios;
- `MEMORY_STATE.md` con `NAV1=CLOSED`;
- ZIP full.

## No hacer
No reordenar, no renombrar, no crear roles, no retirar filtro Production.

## Gate
Sólo abrir NAV2 si el baseline es reproducible.
