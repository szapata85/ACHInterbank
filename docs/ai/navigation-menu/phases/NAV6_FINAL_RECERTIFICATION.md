# NAV6 — Recertificación integral y cierre de NAV-MENU-IA-001

Fecha de ejecución: **2026-10-15**.

## Resultado ejecutivo

- Fuente exclusiva: `ACHInterbank-R34-NAV-IA-NAV5-FULL-PROJECT.zip`.
- SHA-256 de entrada verificado: `c11268bb2e5509abf6bbe39f4d2e0074714b1492674ed7d8b2b40183331ec115`.
- `NAV6 = IMPLEMENTED_UNVERIFIED_ENVIRONMENT`.
- `NAV-MENU-IA-001 = IMPLEMENTED_UNVERIFIED_ENVIRONMENT`.
- **IMPLEMENTACIÓN NAV-MENU-IA-001 COMPLETA**.
- **RECERTIFICACIÓN RUNTIME PENDIENTE POR AMBIENTE**.
- Conclusión: **COMPLETO PENDIENTE RUNTIME**.
- Cambios productivos NAV6: **0 cambios productivos**. No existe `NAV6_FINAL_DELTA.json` porque la recertificación no demostró un defecto NAV que justificara lógica nueva.
- `NAV3.5C` permanece `BLOCKED / NO EJECUTAR` y **NO fue ejecutado**. No se ejecutó trabajo posterior. No se usó Git.

El estado no se promueve a `CLOSED`/`VERIFIED`: el baseline no contiene `web/ach-interbank-ui/node_modules` y el host no dispone de `dotnet`, por lo que los gates runtime requeridos por el programa no pueden ejecutarse sin alterar el ambiente.

## Estado de fases

| Fase | Estado final NAV6 |
|---|---|
| NAV1 | `CLOSED` histórico |
| NAV2 + NAV2.1..NAV2.10B | `IMPLEMENTED_UNVERIFIED_ENVIRONMENT` |
| NAV3 + NAV3.1..NAV3.5B | `IMPLEMENTED_UNVERIFIED_ENVIRONMENT` salvo NAV3.3A/B, que conservan su `CLOSED` histórico |
| NAV3.5C | `BLOCKED / NO EJECUTAR` |
| NAV4 + NAV4.1A/B + NAV4.2A-J | `IMPLEMENTED_UNVERIFIED_ENVIRONMENT` |
| NAV5 | `IMPLEMENTED_UNVERIFIED_ENVIRONMENT` |
| NAV6 | `IMPLEMENTED_UNVERIFIED_ENVIRONMENT` |

## 1. IA / MENU / NAV2

La recertificación mantiene `NavigationInformationArchitectureSeeder` como reconciliador canónico. Su SHA-256 final es `296ffd77544e8017d0fae4dce31a5267b2799a643f8e4dd888d96f79df3dac1c`. Se verificaron de forma estática el árbol raíz canónico, `parent`, `order`, labels, routes, `IsActive`, ausencia de aliases redirect en el árbol visible, unicidad/cobertura y existencia de destinos mediante el validador NAV5 y el validador NAV6.

NAV5 deja `Perfiles oficiales` en orden 10 y mantiene inactivos como MenuItem los aliases `/nacha-config-admin/records` y `/nacha-config-admin/variants-fields`. El árbol final es coherente con la IA NAV2/NAV5 y no se modificó ningún seed productivo en NAV6.

## 2. Inventario y cobertura final

Inventario exacto recertificado:

| Clasificación | Cantidad |
|---|---:|
| Rutas totales | **142** |
| MENU | **70** |
| CONTEXTUAL | **36** |
| TECHNICAL | **12** |
| REDIRECT | **24** |
| DEAD | **0** |
| UNKNOWN | **0** |

Cobertura MENU: **70/70**. No hay CONTEXTUAL/TECHNICAL indebidamente expuestas en menú, los redirects permanecen válidos y los gaps bloqueantes son **0**.

`validate-nav5-screen-route-coverage.py` se ejecutó sobre el baseline actual y pasó **805 assertions**, preservando exactamente `routes=142`, `MENU=70`, `CONTEXTUAL=36`, `TECHNICAL=12`, `REDIRECT=24`, `DEAD=0`, `UNKNOWN=0`, cobertura `70/70` y `blocking_security=0`.

## 3. Navegación SPA

Se recertificaron sin delta productivo:

- `NavigationService`: obtiene `api/navigation/menu` y ordena recursivamente por `order`; no reintroduce overrides históricos de IA.
- `MenuRouteSurfaceService`: clasifica superficies/redirects y soporta segmentos dinámicos sin una lista paralela hardcodeada de menú.
- `NavigationNamingService` y `navigation-naming.util.ts`: consumen naming canónico de navegación y mantienen los fallbacks aprobados.
- parent navigation: padres funcionales conservan navegación + expansión separada; padres estructurales continúan como expanders.
- router links, active state, `aria-current` y navegación programática relevante permanecen en el baseline NAV5/NAV4.2J aprobado.

Los hashes exactos protegidos de estos servicios y el agregado de navegación permanecen byte-equivalentes al baseline certificado por NAV5.

## 4. Copy NAV4 — drift = 0

No se abrió una nueva fase de copy. Se recertificaron todos los buckets cerrados: navigation/headings, actions, fields, tablas, feedback/empty states, validaciones, confirmaciones y tooltips/ARIA.

Los validadores históricos NAV4 que fijan snapshots intermedios se ejecutaron sobre snapshots reconstruidos mediante los deltas aprobados, evitando declarar PASS sobre un baseline incompatible. Resultado de replay:

- NAV4.2J: **445 assertions** PASS.
- NAV4.2C: **111 assertions** PASS.
- NAV4.2B: **81 assertions** PASS.
- NAV4.2I: **405 assertions** PASS.
- NAV4.2H: **1034 assertions** PASS.
- NAV4.2G: **1207 assertions** PASS.
- NAV4.2F: **475 assertions** PASS.
- NAV4.2E: **264 assertions** PASS.
- NAV4.2D: **210 assertions** PASS.

El delta NAV5 se revierte en el snapshot para reconstruir exactamente el seeder NAV4.2J y luego los deltas J→I→H→G→F→E se revierten secuencialmente. Resultado final: **NAV4 drift = 0**.

## 5. Seguridad NAV3

La seguridad permanece intacta y sin cambios productivos NAV6. Se bloquearon por hash y semántica, entre otros:

- `GetMenuForCurrentUserHandler`;
- `Permission` / `RolePermission` y asociaciones `MenuItemPermission` / `MenuItemRole`;
- `P0Policies`, `P1Policies`, `FineGrainedPermissions`;
- `permissionGuard` / `roleGuard` y gates de componentes;
- grants vigentes;
- `FineGrainedGrantBackfillSeeder` (`Order=119`);
- `LegacyFineGrainedRoleMigrationSeeder` (`Order=120`);
- fallbacks legacy vigentes.

Los validadores NAV3 directamente compatibles con el baseline actual se ejecutaron, no se simularon: NAV3.3B y NAV3.4B/C/D/E/F/G2/H/J/K/L/M suman **1346 assertions** PASS. Los validadores históricos que fijan snapshots pre-NAV4 quedan documentados como `SNAPSHOT_INCOMPATIBLE`; no se falsificó su PASS y sus invariantes vigentes se recertificaron mediante hashes/agregados actuales y los replays NAV4 aplicables.

`NAV3.5C` permanece **BLOCKED / NO EJECUTAR** y no fue ejecutado.

## 6. Residual NAV5 — `/nacha-security/certificates/:id/versions`

Decisión NAV6: **A — CONTEXTUAL_INTENTIONAL**.

Evidencia recertificada:

- existe la ruta `certificates/:id/versions` en `nacha-security-routing.module.ts`;
- hereda `permissionGuard` y exige permisos de lectura/gestión aprobados;
- el routing spec prueba explícitamente esa ruta;
- `CertificateVersionsComponent` toma `:id` de `ActivatedRoute` y consulta el historial de versiones;
- el cliente llama `api/nacha-security/certificates/management/${id}/versions`;
- el controller expone `GET {id:int}/versions` bajo `P1Policies.CertificatesRead`;
- la matriz de rutas la clasifica `CONTEXTUAL | detalle de certificado`;
- no existe seed MENU para esa URL ni NAV6 agregó link/acción para forzarlo.

Por tanto es una superficie contextual/deep-link válida, explícita, protegida y testeada. **No requiere entrada MENU y no es un defecto NAV.** El residual histórico de NAV5 queda resuelto semánticamente como no bloqueante; no se agregó acción, handler, permiso ni rediseño UX.

## 7. Replay / no regresión

Además de los replays NAV4, se ejecutaron en el árbol actual:

- `scripts/validate-solution-projects.py` — PASS (7/7 proyectos de solución).
- `scripts/validate-nav5-screen-route-coverage.py` — PASS, 805 assertions.
- validadores NAV3 actuales compatibles — PASS, 1346 assertions acumuladas.
- `scripts/validate-nav6-final-recertification.py` — orquestador final; valida hashes/byte-equivalence, IA/seed/menu, seguridad, navegación, residual, alcance y reconstrucción/replay NAV4.

Resultado del orquestador sobre el árbol NAV6: **813 checks propios** + **6383 assertions** reportadas por validadores hijos = **7196 verificaciones/assertions contabilizadas**, todas PASS en los mecanismos ejecutables estáticos/replay aplicables.

Los validadores de snapshot intermedio incompatibles con el baseline final se registran explícitamente como tales. Sus checks no se presentan como ejecutados sobre un árbol al que no aplican.

## 8. Runtime

- **Angular = `NOT_RUN_ENVIRONMENT`**: `web/ach-interbank-ui/node_modules` no existe. No se instalaron dependencias ni se alteraron manifests.
- **.NET = `NOT_RUN_ENVIRONMENT`**: `dotnet` no está disponible en el host. No se descargaron herramientas.

Estos son los únicos gates de cierre aún pendientes por ambiente.

## 9. Alcance / no logic delta

NAV6 introdujo **0 cambios productivos**. Permanecen byte-equivalentes al ZIP NAV5 todos los archivos bajo `src/`, `web/`, `tests/`, `database/` y `docker/`. No se modificaron negocio, APIs, DTOs, payloads, procesamiento ACH, integraciones, validators, permisos, guards ni security model.

`NAV6_INPUT_TREE_MANIFEST.json` conserva el mapa SHA-256 completo de los **4316** archivos del ZIP de entrada y el validador NAV6 rechaza cualquier cambio fuera de la documentación/artefactos NAV6 permitidos.

## 10. Pendientes reales y criterio de cierre

Pendiente real: ejecutar en un ambiente compatible los gates runtime Angular y .NET sobre **este mismo artefacto** o una extracción byte-equivalente. Hasta entonces no corresponde usar `VERIFIED` ni `CLOSED`.

`NAV3.5C` continúa como bloqueo separado deliberado y no forma parte del trabajo autorizado para NAV6. No se ejecutó trabajo posterior a NAV6.

Criterio observado:

- implementación/recertificación estática integral: PASS;
- gaps bloqueantes: 0;
- residual NAV5: `CONTEXTUAL_INTENTIONAL`, no bloqueante;
- runtime Angular/.NET: pendiente por ambiente;
- estado terminal aplicable: `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`;
- **Conclusión exacta: COMPLETO PENDIENTE RUNTIME**.

## 11. Paquete final

El handoff es `ACHInterbank-R34-NAV-IA-NAV6-FULL-PROJECT.zip`. Su SHA-256 se calcula externamente después de comprimir, porque incluir el hash del propio ZIP dentro del ZIP produciría autorreferencia. El ZIP final debe reextraerse y ejecutar nuevamente `scripts/validate-nav6-final-recertification.py`; esa validación de la extracción final es obligatoria y sustituye cualquier supuesto de equivalencia basado sólo en el árbol previo al empaquetado.
