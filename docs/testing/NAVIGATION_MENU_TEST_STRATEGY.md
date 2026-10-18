# Estrategia de pruebas — NAV-MENU-IA-001

## Principio

El cambio no se considera cerrado porque “se ve bien”. Debe probar seed, autorización, rutas y SPA.

## Backend

1. Seed desde base limpia.
2. Ejecutar seed dos veces: mismo resultado.
3. Sin rutas activas duplicadas por funcionalidad canónica.
4. Sin `Order` repetido entre hermanos.
5. Padre/hijo correcto para cada ítem de la matriz.
6. Admin/Operator reciben sólo lo esperado.
7. Herramientas técnicas: permiso dedicado; no dependen de Production.
8. Acceso directo por URL protegido igual o mejor que el menú.
9. Probar `GetMenuForCurrentUserHandler.ShouldInclude` con roles/permisos técnicos.

Reusar/ampliar pruebas existentes:
- `IncomingNachaCommandCenterMenuSeederTests`
- `OutgoingTransactionMonitoringSeederTests`
- `OperationalRestrictionsMenuSeederTests`
- `MenuItemsServiceHierarchyTests`

## SPA

1. `NavigationService` no cambia labels/hijos recibidos salvo filtros legacy documentados.
2. Orden recursivo estable.
3. Main layout renderiza 3 niveles.
4. Padres con landing conservan una vía de navegación válida.
5. Textos visibles aprobados en rutas, breadcrumbs y títulos.
6. No aparecen CFA/MFT/SOAP/UAT/Command Center/Capability Registry en navegación funcional.
7. Menú móvil y compacto siguen funcionando.

Reusar/ampliar:
- `main-layout.component.spec.ts`
- `app-routing-access.spec.ts`
- `navigation-menu-items-ux.spec.ts`
- `navigation-menu-items-live.spec.ts`
- `spa-routes-smoke.spec.ts`

## Cobertura de rutas

Agregar una prueba o validador que obligue a clasificar cada ruta funcional como:

```text
MENU
CONTEXTUAL
TECNICA
COMPATIBILIDAD
REDIRECT
FUERA_ALCANCE
```

## Comandos de referencia

```text
python scripts/validate-solution-projects.py
dotnet build ACHInterbank.sln -c Release
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --no-build
```

Para SPA usar los comandos definidos por el `package.json` actual; no inventar scripts. Ejecutar focal primero y luego suite amplia cuando sea viable.

## Sin Git

La evidencia de cada fase se asocia al ZIP por SHA-256 y al listado `files_touched` de `MEMORY_STATE.md`.
