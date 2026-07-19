# Validación de regresión de íconos UI

## Identificación

- Fecha local: 2026-07-18.
- Rama: `ACH-Interbank-Postgresql`.
- Commit base y `HEAD` inicial: `d2abe3df4fceda9f9c5dc5aeef3b74c4dcec9025` (`ajuste SPA en pantallas y Menus y Simulador de respuestas diferenciales.`).
- El commit base está contenido en la rama local.
- Alcance: íconos del menú runtime, control de visibilidad de contraseña y estilos directamente relacionados.

## Síntomas iniciales

- El menú expandido mostraba espacios reservados, pero no los íconos configurados de opciones raíz ni hijas.
- El login mostraba las palabras visibles `Mostrar`/`Ocultar` en lugar del ojo y ojo tachado.
- La fuente Material Symbols dependía de Google Fonts, por lo que los íconos podían desaparecer por red, CSP o caché.
- La ejecución Playwright de línea base falló en los seis escenarios de las tres resoluciones.

## Causa raíz del menú

- Persistencia y backend: `MenuItem.Icon` se conserva, `MenuItemDto.Icon` lo expone y el handler lo mapea; no fue necesario cambiar entidad, consulta, DTO, seed ni base de datos.
- Frontend: `MenuItem.icon` y `NavigationService` conservaban la propiedad y el menú backend como única fuente de verdad.
- Regresión: el commit base retiró del template la representación de `item.icon` y `child.icon`, junto con el estilo local del ícono.
- Recurso visual: Material Symbols Outlined estaba declarado mediante CDN en `index.html`, sin activo local disponible.
- Modo compacto: no existía un estado compacto de escritorio que preservara los íconos sin los textos.

## Causa raíz del login

- El template sustituyó el símbolo Material `visibility`/`visibility_off` por texto visible.
- El botón ya era `type="button"` y la lógica preservaba el `FormControl`, pero faltaba `title` dinámico y una representación gráfica coherente.
- El selector SCSS genérico `button { width: 100% }` también alcanzaba el control del ojo y ocupaba espacio innecesario dentro del campo.

## Corrección aplicada

- Se reutilizó Material Symbols Outlined mediante `@material-symbols/font-400` versión `0.45.8`, empaquetado localmente; no se agregó CDN ni un segundo sistema de íconos.
- `UiIconComponent` centraliza la clave semántica, soporta las 36 claves configuradas detectadas y resuelve claves desconocidas al fallback visible `help`.
- El layout representa íconos raíz e hijos desde `MenuItem.icon`, separa el ícono funcional de la flecha y conserva color activo, `hover` y `focus` mediante `currentColor`.
- El modo compacto de escritorio mantiene los íconos y ofrece `title`; el modo expandido conserva ícono y texto.
- El login representa `visibility` cuando la contraseña está oculta y `visibility_off` cuando está visible.
- El botón del login conserva `type="button"` y actualiza `aria-label`, `title` y `aria-pressed` según la acción disponible.
- Los estilos del botón principal quedaron limitados al `submit` directo del formulario; el control del ojo mantiene tamaño, centrado y foco.
- No se modificaron permisos, rutas, guards, redirects, autenticación, backend, seeds ni persistencia.

## Archivos modificados

### Angular y configuración

- `web/ach-interbank-ui/angular.json`
- `web/ach-interbank-ui/package.json`
- `web/ach-interbank-ui/package-lock.json`
- `web/ach-interbank-ui/src/index.html`
- `web/ach-interbank-ui/src/app/shared/shared.module.ts`
- `web/ach-interbank-ui/src/app/shared/components/ui/ui-icon.component.ts`
- `web/ach-interbank-ui/src/app/features/auth/login.component.html`
- `web/ach-interbank-ui/src/app/features/auth/login.component.scss`
- `web/ach-interbank-ui/src/app/layout/main-layout.component.html`
- `web/ach-interbank-ui/src/app/layout/main-layout.component.scss`
- `web/ach-interbank-ui/src/app/layout/main-layout.component.ts`

### Pruebas

- `web/ach-interbank-ui/src/app/shared/components/ui/ui-icon.component.spec.ts`
- `web/ach-interbank-ui/src/app/features/auth/login.component.spec.ts`
- `web/ach-interbank-ui/src/app/layout/main-layout.component.spec.ts`
- `web/ach-interbank-ui/src/app/core/services/navigation.service.spec.ts`
- `web/ach-interbank-ui/e2e/navigation-and-login-icons-regression.spec.ts`

### Evidencia

- `web/ach-interbank-ui/e2e-evidence/ui-icon-regression/baseline/`
- `web/ach-interbank-ui/e2e-evidence/ui-icon-regression/final/`

## Pruebas y resultados

| Validación | Resultado |
|---|---|
| `npm ci` | Aprobado; 1.064 paquetes instalados. El audit reporta 22 vulnerabilidades del árbol npm. |
| `npm run build -- --configuration production` | Aprobado; hash `e6fd3522f94a8dad`, bundle inicial 2,32 MB. |
| `npm test -- --watch=false --browsers=ChromeHeadless` | Aprobado; `421 SUCCESS`, 0 fallos. |
| `npx playwright test e2e/navigation-and-login-icons-regression.spec.ts --project=chromium --workers=1` | Aprobado; 6/6 en 22,7 s. |
| `npx playwright test e2e/spa-routes-smoke.spec.ts --project=chromium --workers=1` | Aprobado; 79/79 en 2,3 min. |
| Contenedor `achinterbank-spa` | `healthy`; `http://localhost:743` responde 200. |
| Fuente local Material Symbols | HTTP 200, 487.788 bytes, cero referencias externas a Google Fonts. |
| Backend | No aplica: no hubo cambios productivos .NET, DTO, handler, seed o persistencia. |

La prueba dirigida verifica DOM y comportamiento: íconos raíz e hijo, fallback controlado, rama desplegada, estado activo, menú compacto, expansión, recarga, ausencia de errores de consola, ausencia de solicitudes fallidas, alternancia por clic y teclado, preservación del valor ficticio y cero solicitudes de login durante la alternancia.

## Resoluciones y capturas

Se validaron `1920×1080`, `1366×768` y `1024×768`.

- Línea base: [evidencia inicial](../../web/ach-interbank-ui/e2e-evidence/ui-icon-regression/baseline/).
- Login oculto: [1366×768](../../web/ach-interbank-ui/e2e-evidence/ui-icon-regression/final/login-oculto-1366x768.png).
- Login visible: [1366×768](../../web/ach-interbank-ui/e2e-evidence/ui-icon-regression/final/login-visible-1366x768.png).
- Menú expandido con rama hija: [1366×768](../../web/ach-interbank-ui/e2e-evidence/ui-icon-regression/final/menu-expandido-1366x768.png).
- Menú compacto: [1366×768](../../web/ach-interbank-ui/e2e-evidence/ui-icon-regression/final/menu-colapsado-1366x768.png).
- Evidencia final de las tres resoluciones: [directorio final](../../web/ach-interbank-ui/e2e-evidence/ui-icon-regression/final/).

Las capturas usan un JWT sintético no firmado y una contraseña explícitamente ficticia; no contienen credenciales, tokens visibles ni datos personales reales.

## Riesgos residuales

- Si la administración incorpora en el futuro una clave Material Symbol nueva, deberá añadirse al catálogo central; hasta entonces aparecerá el fallback `help`, de forma visible y comprobable.
- La validación visual automatizada se ejecutó en Chromium; no se hizo matriz adicional de renderizado en Firefox o WebKit para este hotfix dirigido.

## Decisión

**HOTFIX UI APROBADO**

Los íconos se obtienen del menú runtime, se representan en padres e hijos, sobreviven al modo compacto y a la recarga, y usan una fuente local. El control de contraseña alterna el ícono y el tipo sin enviar el formulario ni perder el valor, con atributos accesibles dinámicos. Build, 421 pruebas Angular, 6 escenarios Playwright y 79 smokes de rutas aprobaron.
