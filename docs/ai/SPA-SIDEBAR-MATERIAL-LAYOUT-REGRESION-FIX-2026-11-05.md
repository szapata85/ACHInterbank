# Corrección de la regresión visual del menú lateral — ACH Interbank

**Fecha:** 5 de noviembre de 2026  
**Base exacta:** `ACHInterbank_SIDEBAR_UX_MATERIAL_BRANDING_2026-11-05.zip`  
**Alcance:** `web/ach-interbank-ui` (menú global)  
**Estado:** código corregido; validación estática y comprobación de geometría en Chromium aislado realizadas; compilación Angular y pruebas del SPA real pendientes.

## Reporte del operador

Tras la entrega del buscador lateral, los íconos de «Seguridad NACHA», «Operación», «Clientes» y otras opciones aparecían encima de sus textos; las filas se estiraron verticalmente y el indicador de expansión terminó convertido en un trazo por debajo de la etiqueta. El buscador sí era útil y debía conservarse.

## Causa técnica

La implementación anterior declaraba `mat-button` en cada opción, pero suponía que los íconos, etiquetas y cheurones eran hijos flex directos de `.nav-item`. Angular Material proyecta el contenido del botón dentro de `.mdc-button__label` y puede tratar `mat-icon` como contenido especial. Por ello, `display: flex` y `gap` aplicados al host del botón no distribuían esos hijos; `.nav-label` (`display: block`) y `.expand-icon` quedaban apilados. Los controles estáticos anteriores verificaban clases y tokens, **no la geometría real**.

## Corrección mínima aplicada

1. Cada uno de los tres patrones de navegación (`.nav-parent`, `.nav-link`, `.nav-destination`) contiene ahora un único `<span class="nav-item-content">` que agrupa **ícono, etiqueta y, si procede, flecha**. La fila interna usa `display:flex`, `align-items:center`, `width:100%` y `min-width:0`.
2. Se añadió una **única regla global estrictamente acotada al menú**: `.primary-sidenav .nav-item.mat-mdc-button > .mdc-button__label` ocupa todo el ancho disponible. Ningún botón Material de formularios u otras pantallas es afectado.
3. Se conservaron colores dinámicos, búsqueda con tildes, menú compacto, responsive, jerarquía, vínculos, permisos, gestión del estado activo, clasificación de padres y foco de teclado. No cambian modelos, endpoints, reglas ACH Colombia/CENIT ni configuración SQL.
4. El modo compacto centra el ícono dentro de la misma fila; etiquetas largas pueden ocupar dos líneas **sin cambiar la alineación horizontal**.

## Evidencia de validación

- `python scripts/validate-sidebar-ux-static.py`: **38/38 PASS** (siete salvaguardas nuevas para la regresión de la proyección MDC).
- TypeScript sintáctico: componente, especificación Jasmine y especificación E2E **3/3 PASS**, realizado con parser global de TypeScript, **no** compilación Angular.
- Estructura HTML: los 3 tipos de enlace tienen un único contenedor `.nav-item-content` con ícono y etiqueta.
- **Chromium aislado (sin Angular):** se cargaron los estilos reales del menú y el markup representativo del contenedor `.mdc-button__label` de Material. En 6 opciones de muestra, íconos, etiquetas y flechas mantuvieron alineación horizontal; alturas de filas normales ≈45,8 px; «Administración de parámetros» ≈59,4 px en dos líneas. Modo compacto: ícono centrado, diferencia de centros = 0 px. Esto valida el CSS con un navegador, **no afirma que el SPA de Docker haya sido ejecutado**.
- Se incorporó regresión Jasmine para la estructura material proyectada y una prueba Playwright de **geometría real** (`icon.right < label.left`, alineación vertical, ancho de `.mdc-button__label`, límites de fila, flecha en su lugar y centrado en modo compacto). **No ejecutadas en el SPA real** por falta de dependencias `node_modules`/API QA autorizada en este entorno.

## Criterios de aceptación pendientes en Docker del usuario

1. Desde esta entrega acumulada, reconstruir la imagen del SPA y la API con `docker-ach.cmd up --no-seed --no-auto-migrations` (sin `--no-build`). Abrir `/dashboard`, recarga forzada.
2. En 1366×768, 1440×900, 390×844 y zoom 200%, verificar cada fila: ícono a la izquierda, texto en la misma línea o dos si es necesario, cheurón a la derecha; ninguna barra/línea vertical bajo el título. No debe aparecer scrollbar horizontal.
3. Buscar etiquetas con y sin tilde, expandir/contraer, visitar rutas, comprobar resaltado activo, permisos y el regreso desde compacto o móvil.
4. Ejecutar `npm ci && npm run build` y pruebas focales: `npm test -- --watch=false --include=src/app/layout/main-layout.component.spec.ts`; con servidor QA y credenciales autorizadas, `npx playwright test e2e/navigation-sidebar-hover-contrast.spec.ts --project=chromium --workers=1`.
5. Revisar con el branding de al menos dos entidades y en condiciones claro/oscuro; contraste normal y en hover/focus.

## Nota distinta sobre el orden del menú

El backend sigue siendo la fuente autorizada del orden, nombre, árbol y permisos. En la captura, «Seguridad NACHA» aparece antes de «Inicio», probablemente porque la instalación conserva valores antiguos en SQL Server. Esta corrección **no reordena unilateralmente la navegación en Angular**. Revisar `/navigation/menu-items` con permiso de administración, contrastar orden y etiquetas con la arquitectura aprobada y corregir los registros por el mecanismo administrativo autorizado, sin siembra general ni SQL destructivo.

## Archivos del incremento

- `web/ach-interbank-ui/src/app/layout/main-layout.component.html`
- `web/ach-interbank-ui/src/app/layout/main-layout.component.scss`
- `web/ach-interbank-ui/src/styles.scss`
- `web/ach-interbank-ui/src/app/layout/main-layout.component.spec.ts`
- `web/ach-interbank-ui/e2e/navigation-sidebar-hover-contrast.spec.ts`
- `scripts/validate-sidebar-ux-static.py`
- `docs/ai/SPA-SIDEBAR-MATERIAL-LAYOUT-REGRESION-FIX-2026-11-05.md`
