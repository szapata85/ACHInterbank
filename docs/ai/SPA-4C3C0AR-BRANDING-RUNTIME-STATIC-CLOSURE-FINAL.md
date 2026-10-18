# SPA 4C.3C0ar — Branding Runtime Static Closure Final

## Tipo de checkpoint

READ-ONLY global final sobre el branding administrable existente. No se implementaron cambios en source, estilos, contratos, backend, rutas, guards, permisos ni configuración.

## Baseline

- ZIP: `ACHInterbank-v117-spa-4C3C0aq-sidebar-scrollbar-contrast-branding.zip`
- SHA-256 esperado/verificado: `a4a2f05b4de7d894e6fceee1936cacaa066e1e2895d507992aaaf5db3473a789`

## Contrato administrable vigente

`BrandingSettings` conserva exactamente los seis campos auditados:

- `publicLogo`
- `privateLogo`
- `publicBackground`
- `privateBackground`
- `sidebarBackground`
- `buttonColor`

La pantalla `/users/branding` mantiene el payload de esos seis campos y `BrandingService` mantiene `GET/PUT api/users/branding`. No se amplió el contrato.

## Mapa end-to-end

| Campo | Clasificación | Aplicación runtime/direct binding | Alias/tokens | Consumidores SPA principales |
|---|---|---|---|---|
| `publicLogo` | `LOGO/DIRECT_BINDING` | `BrandingService.branding$` → `branding.publicLogo` | No usa CSS var | `login-layout.component.html`: `[src]="branding.publicLogo"` |
| `privateLogo` | `LOGO/DIRECT_BINDING` | `BrandingService.branding$` → `branding.privateLogo` | No usa CSS var | `main-layout.component.html`: `[src]="branding.privateLogo"` |
| `publicBackground` | `BRANDING_RUNTIME` | `BrandingService` → `--brand-public-background` | runtime token directo | `login-layout.component.scss` `:host` |
| `privateBackground` | `BRANDING_RUNTIME` | `BrandingService` → `--brand-private-background` | runtime token directo | `main-layout.component.scss` `:host` y `.shell-container` |
| `sidebarBackground` | `BRANDING_RUNTIME` | `BrandingService` → `--brand-sidebar-background`; calcula contraste → `--brand-sidebar-on-background` | `--color-sidebar`, `--color-on-sidebar` | sidenav, texto/iconos/hover/expanded/child/sidebar scrollbar |
| `buttonColor` | `BRANDING_RUNTIME` | `BrandingService` normaliza primary, calcula contraste y hover | primary/on-primary/hover/focus + Material + legacy | shared buttons, layouts, features, Material primary y focos/accentos |

## `buttonColor` — cadena runtime revalidada

`BrandingService.applyBranding()` sigue derivando desde `buttonColor`:

- `--brand-primary` = color primary normalizado.
- `--brand-on-primary` = contraste runtime calculado por `getContrastColor()`.
- `--brand-primary-hover` = primary con lightness desplazada por `shiftHexLightness(primary, -0.12)`.
- `--button-color` = primary, compatibilidad legacy.
- `--mat-sys-primary` = primary.
- `--mat-sys-on-primary` = contraste runtime.

Aliases semánticos revalidados en `_tokens.scss`:

- `--color-primary: var(--brand-primary)`.
- `--color-on-primary: var(--brand-on-primary)`.
- `--color-primary-contrast: var(--color-on-primary)`.
- `--color-primary-hover: var(--brand-primary-hover)`.
- `--color-focus: var(--color-primary)`.

Conteo estático de referencias `var(...)` en `web/ach-interbank-ui/src`:

- `--color-primary`: 137 referencias / 45 archivos.
- `--color-on-primary`: 11 / 11.
- `--color-primary-contrast`: 6 / 4.
- `--color-primary-hover`: 18 / 10.
- `--color-focus`: 8 / 6.
- `--button-color`: 3 / 3.
- `--mat-sys-primary`: 11 / 6.
- `--mat-sys-on-primary`: 6 / 4.

## `sidebarBackground` — cadena runtime revalidada

`BrandingService.applyBranding()` sigue derivando:

- `--brand-sidebar-background` desde `sidebarBackground` cuando existe valor administrado; si no, aplica el default de `_tokens.scss`.
- `--brand-sidebar-on-background` mediante el contraste calculado sobre el color efectivo del sidebar.

Aliases:

- `--color-sidebar: var(--brand-sidebar-background)`.
- `--color-on-sidebar: var(--brand-sidebar-on-background)`.

`main-layout.component.scss` conserva:

- `--shell-sidenav-bg: var(--color-sidebar)`.
- `--shell-nav-text: var(--color-on-sidebar)`.
- texto secundario, iconos, hover, expanded, child y scrollbar derivados mediante `color-mix(... var(--color-on-sidebar) ... transparent)`.
- scrollbar normal/hover ya depende de `--color-on-sidebar` (corrección 4C.3C0aq).

## Logos y backgrounds

- `publicLogo`: permanece enlazado directamente en login mediante `[src]="branding.publicLogo"`.
- `privateLogo`: permanece enlazado directamente en el shell privado mediante `[src]="branding.privateLogo"`.
- `publicBackground`: `login-layout :host` consume `var(--brand-public-background)`.
- `privateBackground`: `main-layout :host` y `.shell-container` consumen `var(--brand-private-background)`.
- `sidebarBackground`: `.primary-sidenav` consume indirectamente `--color-sidebar` vía `--shell-sidenav-bg`; sus foregrounds relevantes consumen el contraste runtime.

## Scan global de `BYPASS_REAL`

Se revisó globalmente el source frontend (`.scss`, `.css`, `.html`, `.ts`) buscando evidencia semántica, no mera coincidencia cromática.

### Primary / buttonColor

- No hay usos de `#2563eb` ni `#1d4ed8` en estilos de feature/layout/shared fuera de las definiciones default de `_tokens.scss`.
- No quedan focos con el RGB default `rgba(37, 99, 235, ...)`.
- No se detectaron reglas con background primary/runtime y foreground fijo `white` / `#fff` / `#ffffff` en lugar de `--color-on-primary`/`--color-primary-contrast`/`--mat-sys-on-primary`.
- Los tres consumidores legacy de `--button-color` (`user-form`, `password-rules-settings`, `login-lockout-settings`) usan `color: var(--color-on-primary)`.
- Los backgrounds primary directos auditados usan foreground runtime correspondiente cuando existe relación foreground/background primaria.

### Sidebar

- No quedan los `rgba(226, 232, 240, 0.28/0.48)` del scrollbar.
- El scrollbar usa `--color-on-sidebar` a 28%/48%.
- No se detectó foreground visualmente dependiente del sidebar configurable que conserve un color fijo de contraste del sidebar default.

### Public/private backgrounds

- El layout público consume `--brand-public-background`.
- El shell privado consume `--brand-private-background`.
- No se identificó un contenedor semánticamente equivalente que bypassée esos fondos runtime.

## Revalidación de los 8 bypasses corregidos

| # | Bypass tratado | Estado final |
|---:|---|---|
| 1 | CENIT Home on-primary | Resuelto; background primary + `--color-on-primary` |
| 2 | Third Parties primary accent | Resuelto; border accent usa `--color-primary` |
| 3 | Navigation Log on-primary | Resuelto; background primary + `--color-on-primary` |
| 4 | Variants/Fields focus | Resuelto; `color-mix(... var(--color-focus) 28%, transparent)` |
| 5 | `user-form` primary legacy | Resuelto; `--button-color` + `--color-on-primary` |
| 6 | `password-rules-settings` primary legacy | Resuelto; `--button-color` + `--color-on-primary` |
| 7 | `login-lockout-settings` primary legacy | Resuelto; `--button-color` + `--color-on-primary` |
| 8 | sidebar scrollbar contrast | Resuelto; normal/hover derivados de `--color-on-sidebar` |

Patrones antiguos específicos de esos ocho casos: 0 pendientes.

## Clasificación explícita

### `BRANDING_RUNTIME`

- `publicBackground`
- `privateBackground`
- `sidebarBackground` + contraste de sidebar
- `buttonColor` + primary/on-primary/hover/focus + Material primary/on-primary + legacy `--button-color`

### `LOGO/DIRECT_BINDING`

- `publicLogo`
- `privateLogo`

### `DESIGN_TOKEN_STATIC`

Permanecen estáticos, sin considerarse bypass por coincidencia de color: foundations de background/surface/text/border, secondary, error/danger, warning, success y tokens Material que `BrandingService` no gobierna (por ejemplo primary-container/on-primary-container). También permanecen estáticas superficies/tintes/bordes locales cuando no existe evidencia semántica de que pertenezcan al contrato administrable.

### `BYPASS_REAL`

Ninguno identificado en el scan global final.

## Resultado de cierre

- bypasses conocidos previos pendientes = **0**
- bypasses globales nuevos encontrados = **0**
- bypasses runtime totales pendientes = **0**

Con el alcance estático solicitado, el branding runtime administrable actual puede declararse `STATIC_CLOSED`.

## Validaciones de inmutabilidad / arquitectura

- UI source diff vs baseline: 0.
- backend diff vs baseline: 0.
- routing diff vs baseline: 0 (29 archivos de routing auditados).
- `feature -> feature`: 0 imports cross-feature.
- `core -> features`: 0.
- `shared -> features`: 0.
- `BrandingService`: intacto.
- `_tokens.scss`: intacto.
- `_components.scss`: intacto.
- `/users/branding`: ruta/pantalla intactas.
- `api/users/branding`: endpoint intacto.
- Contratos/DTO de branding: intactos.

## Angular

Se comprobó exclusivamente `web/ach-interbank-ui/node_modules/.bin/ng`.

`ng: not found`

No se ejecutó `npm ping`, `npm ci`, registry retries, build ni tests. No se declara VERIFIED.

## Estado

`4C3C0AR_BRANDING_RUNTIME_STATIC_CLOSED_VALIDATION_PENDING`
