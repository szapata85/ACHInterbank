# NAV5 — Cobertura final de pantallas y rutas

## Estado

`IMPLEMENTED_UNVERIFIED_ENVIRONMENT`

Baseline exclusivo: `ACHInterbank-R34-NAV-IA-NAV4.2J-FULL-PROJECT.zip` — SHA-256 `3f3c297f2df4ee11e085ca32172a3f7b6667b8fc00ebf7a0b0dbfae5a1e61515`. No se usó Git. NAV6 no se ejecuta en esta fase.

## Objetivo

Demostrar desde el código que todas las rutas SPA reales tienen una clasificación funcional única y que el menú publica únicamente destinos primarios coherentes. La existencia de una route no implica que deba convertirse en `MENU`.

## Universo exacto

El scanner AST parte de `APP_ROUTES` y expande `loadChildren`, lazy routes, `children` y `redirectTo`. El resultado final es **142 routes**:

- `MENU`: **70**
- `CONTEXTUAL`: **36**
- `TECHNICAL`: **12**
- `REDIRECT`: **24**
- `UNREACHABLE_OR_DEAD`: **0**
- `UNKNOWN`: **0**

El inventario trazable completo está en `NAV5_SCREEN_ROUTE_COVERAGE_INVENTORY.md`.

## Cobertura de menú

Después del delta NAV5 hay **70 destinos navegables de menú** y las **70 routes `MENU` están cubiertas exactamente una vez**. No quedan `MENU_MISSING`, destinos de menú inexistentes, duplicados indebidos ni superficies `CONTEXTUAL`/`TECHNICAL` publicadas como entrada principal.

Los nueve nodos activos no navegables son agrupadores intencionales: cinco routes redirect (`/transactions`, `/nacha-config-admin`, `/nacha-security`, `/logs`, `/uat`) y cuatro identificadores estructurales sin route SPA exacta (`/seguimiento-control`, `/seguimiento-control/respuestas-transacciones`, `/administracion-parametros`, `/administracion`).

## Gaps encontrados y resolución

Se demostraron **4 gaps**:

1. `WRONG_ORDER` — `/nacha-config-admin/perfiles`: el seeder temprano deja Order 1; NAV5 normaliza el estado final a Order 10 bajo `/nacha-config-admin`.
2. `REDIRECT_INCONSISTENCY` — `/nacha-config-admin/records`: el componente sólo redirige al workspace de perfiles; el `MenuItem` final queda inactivo.
3. `REDIRECT_INCONSISTENCY` — `/nacha-config-admin/variants-fields`: mismo caso; el `MenuItem` final queda inactivo.
4. `ORPHAN_CONTEXTUAL_ROUTE` — `/nacha-security/certificates/:id/versions`: la route y pantalla existen, pero no se encontró `routerLink` ni navegación programática hacia ella. Permanece contextual, fuera del menú, y se documenta como residual; agregar una acción UI reabriría NAV4/fuera del alcance NAV5.

**Corregidos:** 3. **Bloqueados por seguridad:** 0. **Residual:** 1 contextual huérfano documentado.

## Delta productivo permitido

Único archivo productivo modificado:

`src/Cfa.ACHInterbank.Persistence/Navigation/Seeders/NavigationInformationArchitectureSeeder.cs`

Cambios old→new:

- `/nacha-config-admin/perfiles`: Order `1 → 10` en el estado final del reconciliador.
- `/nacha-config-admin/records`: `IsActive true → false` en el estado final.
- `/nacha-config-admin/variants-fields`: `IsActive true → false` en el estado final.

Rutas, componentes, permisos, asociaciones históricas de autorización y lógica funcional no cambian. El detalle reversible está en `NAV5_NAVIGATION_DELTA.json`.

## Seguridad e invariantes

NAV5 no introduce permisos y no modifica `Permission`, `RolePermission`, `MenuItemPermission`, `MenuItemRole`, policies, guards, grants, fallback de seguridad, `GetMenuForCurrentUserHandler`, seeders NAV3 ni NAV3.5C. La desactivación de los dos aliases de workspace conserva sus filas y enlaces de autorización.

`NavigationService`, `MenuRouteSurfaceService` y `NavigationNamingService` permanecen byte-equivalentes al baseline NAV4.2J. El árbol `src/app` completo permanece byte-equivalente, por lo que NAV4 copy/navigation controls no se reabren.

## Decisiones

- **DEC-NAV-5-001:** No toda ruta SPA pertenece al menú; cada ruta se clasifica `MENU`, `CONTEXTUAL`, `TECHNICAL` o `REDIRECT` según su función real.
- **DEC-NAV-5-002:** Una ruta `MENU` debe tener cobertura única y coherente en la IA; rutas contextuales/técnicas no se promueven artificialmente al menú.
- **DEC-NAV-5-003:** NAV5 no introduce permisos nuevos ni modifica el modelo de seguridad; gaps que requieran rediseño de seguridad se bloquean y documentan.

## Validación ejecutada

- `scripts/validate-nav5-screen-route-coverage.py`: **PASS — 809 assertions**. Además del inventario/cobertura, compara el baseline ZIP autorizado, verifica que los únicos cambios/nuevos archivos sean los NAV5 permitidos, congela seguridad/NAV4/NAV3.5C/solution graph, valida referencias de permisos existentes y demuestra que el reverse de `NAV5_NAVIGATION_DELTA.json` reconstruye byte-a-byte el seeder NAV4.2J.
- Parser TypeScript global, sin instalar dependencias: **PASS — 374 archivos productivos, 0 parse diagnostics**.
- `scripts/validate-solution-projects.py`: **PASS — 7/7 projects**, referencias existentes y cargadas por la solución.
- Replays NAV4 sobre proyección de fase (revirtiendo sólo deltas copy posteriores y proyectando temporalmente el seeder NAV5 al baseline NAV4.2J, con restauración posterior): NAV4.2J **PASS 445**, NAV4.2I **PASS 405**, NAV4.2H **PASS 1034**, NAV4.2G **PASS 1207**, NAV4.2F **PASS 475**, NAV4.2C **PASS 111**, NAV4.2B **PASS 81**. Esta proyección es necesaria porque los validadores NAV4 congelan el hash exacto del seeder anterior a NAV5.
- Controles NAV3/NAV2 relevantes: **PASS dentro del validador NAV5** mediante byte-equivalencia exacta de las superficies de seguridad/guards/servicios de navegación respecto del baseline NAV4.2J, validación de referencias de permisos/roles existentes, parent/order/cobertura canónica y comparación del paquete completo. Los validadores standalone NAV3.5A/B son locks de snapshots intermedios previos a cambios autorizados de NAV4 y no se usan como prueba de regresión del árbol NAV4.2J.
- Angular runtime: `NOT_RUN_ENVIRONMENT` (`node_modules` ausente; no se instalaron dependencias).
- .NET runtime/tests: `NOT_RUN_ENVIRONMENT` (`dotnet` ausente).

## Gate de cierre

- 142/142 routes inventariadas y clasificadas.
- 70/70 `MENU` cubiertas.
- 0 `UNKNOWN`.
- 0 `UNREACHABLE_OR_DEAD`.
- 0 rutas de menú inválidas.
- 0 contextual/técnicas expuestas por error.
- 0 duplicados indebidos de destino.
- 3 gaps corregidos y 1 residual contextual documentado.
- Seguridad, NAV4, NAV3.5C y solution graph invariantes: **PASS**.
- `NO LOGIC DELTA` ajeno: **PASS**; el único delta productivo es el reconciliador IA descrito arriba.

## Siguiente fase

`NEXT recomendado = NAV6`, únicamente por decisión humana. **NAV6 NO fue ejecutado.**
