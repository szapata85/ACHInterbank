# Plan de implementación por fases — NAV-MENU-IA-001

## Regla de ejecución

Una fase por chat. No avanzar con criterios rojos. El ZIP full de salida de una fase es el único insumo de la siguiente.

## NAV1 — Baseline y guardas

**Prioridad 1.** Sin cambio funcional.

- Inventariar árbol efectivo: `MenuItemConfiguration` + 11 seeders de navegación + roles/permisos + rutas SPA.
- Caracterizar `GetMenuForCurrentUserHandler`, `NavigationService` y `MainLayoutComponent`.
- Añadir pruebas que congelen comportamiento actual relevante antes del refactor.
- Registrar rutas sin entrada de menú y rutas técnicas/contextuales.

Gate: inventario completo, tests de caracterización y `MEMORY_STATE.md` actualizado.

## NAV2 — Seed canónico

**Prioridad 2.** Mayor valor funcional.

- Crear un reconciliador final de arquitectura de navegación, por ejemplo `NavigationInformationArchitectureSeeder`, con `Order` posterior a los seeders funcionales.
- Upsert por ruta canónica; no depender de IDs generados para localizar elementos.
- Reubicar/renombrar según el árbol objetivo.
- Órdenes 10/20/30 sin empates entre hermanos.
- Desactivar duplicados/entradas obsoletas sin borrar rutas SPA.
- Mantener permisos existentes salvo lo estrictamente necesario para que los padres sean visibles.

Gate: seed idempotente y árbol objetivo para Admin/Operator sin resolver aún herramientas técnicas.

**Estado tras NAV2.10A:** la estructura backend canónica queda normalizada: `/dashboard` es `Inicio` Order 10, los nueve dominios raíz activos ocupan exactamente 10..90 sin empates y `/ach-cycles` tiene una sola representación visible (`Ciclos de operación` bajo `Administración de parámetros`). Los duplicados/legacy se preservan inactivos cuando corresponde; `Pruebas de servicios` continúa inactiva hasta NAV3. La ejecución .NET/idempotente sigue diferida por ausencia de SDK en el entorno de trabajo, por lo que NAV2 permanece `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`, no `CLOSED`.

**Estado tras NAV2.10B:** la cobertura canónica prevista de `Catálogos` queda completa a nivel de fuente: nueve hijos exactos en órdenes 10..90, sin rutas nuevas fuera del conjunto aprobado. Las ocho rutas que carecían de MenuItem usan el mismo sobre de autorización existente `Admin`/`ACH.Operator` + `CanManageAch`; la fila legacy de `/catalogs/clearing-house-special-dates` queda inactiva y su redirect SPA se conserva. No se cambia Angular ni el modelo de seguridad. NAV2 puede considerarse funcionalmente implementado en fuente, pero sigue `IMPLEMENTED_UNVERIFIED_ENVIRONMENT` hasta ejecutar las pruebas .NET focales/idempotentes.

## NAV3 — Seguridad y herramientas técnicas

**Prioridad 3.** Seguridad antes de publicar superficies técnicas.

- Introducir permiso dedicado `TestTools.Access` o equivalente aprobado.
- Preferir autorización por permiso; roles sólo otorgan permisos.
- Seed técnico aditivo para `Superadministrador` y/o `Desarrollo` según decisión final.
- `Herramientas de prueba` debe sembrarse siempre.
- Retirar el filtro `environment.IsProduction()` que hoy oculta `/uat` en `GetMenuForCurrentUserHandler`.
- Proteger rutas de simulador y consola de pruebas con el mismo permiso dedicado.
- No otorgar permiso a `ACH.Operator`.

Gate: producción ya no decide visibilidad; autorización sí.

## NAV4 — SPA y copy humanizado

**Prioridad 4.** El frontend deja de contradecir al seed.

- Eliminar `NavigationService.normalizeIntegrationNavigation` o reducirlo a compatibilidad sin sobrescribir label/hijos.
- Sustituir copy visible CFA/MFT/SOAP/UAT/Command Center/Capability Registry por nombres aprobados.
- Alinear `title`, `breadcrumb`, encabezados y textos de landing.
- Mantener nombres técnicos en código, rutas y contratos cuando sea necesario.
- No cambiar reglas de negocio.

Gate: el menú y los títulos visibles no contienen spanglish/términos técnicos prohibidos salvo acrónimos de dominio autorizados.

## NAV5 — Cobertura y clasificación de pantallas

**Prioridad 5.** Evita pantallas huérfanas.

Publicar según matriz:
- `/customers`
- bulk ingestion upload/tracking
- `/transactions/returns-ror`
- `/ach-responses` + dashboard/manual-review/audit
- `/ach-responses/status-mappings` bajo parámetros
- `/navigation-logs`
- observability/queue/orphan-resolution
- `/ach/reconciliation`
- `/payment-rail-capability-registry` aprobado en NAV2.8E como `Capacidades de la plataforma` bajo Administración; la normalización de su permiso dedicado queda para seguridad.

Mantener fuera del menú detalles, redirects, legacy y CENIT específico.

Gate: toda ruta funcional queda clasificada como `MENU`, `CONTEXTUAL`, `TECNICA`, `COMPATIBILIDAD` o `REDIRECT`.

## NAV6 — Recertificación y empaquetado

**Prioridad 6.** Cierre.

- Seed desde base vacía y seed repetido.
- Validar que no haya órdenes duplicados entre hermanos.
- Verificar perfiles Admin, Operator, Superadministrador/Desarrollo.
- Verificar acceso directo por URL y menú.
- Build/test backend y SPA cuando el ambiente lo permita.
- Ejecutar E2E de navegación existentes y ampliar casos críticos.
- Crear ZIP full final y manifest SHA-256.

Gate: `NAV-MENU-IA-001=CLOSED` sólo con evidencia del artefacto exacto.


### Resultado de ejecución NAV6 — 2026-10-15

NAV6 quedó `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`: la recertificación estática integral pasó, con 142 rutas, MENU 70/70, DEAD/UNKNOWN 0, gaps bloqueantes 0, NAV4 drift = 0 y seguridad NAV3 intacta. El residual `/nacha-security/certificates/:id/versions` se resolvió como `CONTEXTUAL_INTENTIONAL` no bloqueante. No hubo cambios productivos NAV6. Angular y .NET quedaron `NOT_RUN_ENVIRONMENT` por ausencia de `node_modules` y `dotnet`, respectivamente; por el gate oficial el programa **no** se marca `CLOSED`. Conclusión: `COMPLETO PENDIENTE RUNTIME`. NAV3.5C no fue ejecutado y no se autoriza trabajo posterior sin decisión humana.
