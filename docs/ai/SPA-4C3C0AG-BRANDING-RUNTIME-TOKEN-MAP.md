# SPA 4C.3C0ag — Branding Runtime Token Map

## Estado

`4C3C0AG_BRANDING_RUNTIME_TOKEN_MAP_COMPLETE_VALIDATION_PENDING`

Checkpoint **READ-ONLY** sobre `ACHInterbank-v106-spa-4C3C0af-profile-workspace-on-error-token.zip`.
No se modificó ningún source, SCSS, token, HTML/TS/spec, backend, DTO, endpoint, ruta, permiso, contrato ni configuración. El único archivo agregado por este lote es este reporte.

## Baseline

- SHA-256 esperado/verificado: `b499f5e1afdb8ca6a4aa8ef2f1b80ca7e6c6651fe39ee898ce36116e142db75d`
- UI route: `/users/branding`
- API usada por el SPA: `GET/PUT api/users/branding`
- Modelo frontend: `web/ach-interbank-ui/src/app/core/models/branding.model.ts`
- Aplicador runtime: `web/ach-interbank-ui/src/app/core/services/branding.service.ts`
- Tokens: `web/ach-interbank-ui/src/styles/_tokens.scss`

## Ruta y permisos preservados

La navegación sigue siendo:

`/users/branding` → `MainLayoutComponent` → lazy `AdminModule` → `BrandingSettingsComponent`.

Controles heredados actuales:

- `MainLayoutComponent`: `authGuard`.
- `AdminRoutingModule` raíz: `roleGuard` + `permissionGuard`.
- Rol: `Admin`.
- Permiso: `CanManageUsers`.

El backend conserva `[Route("api/users/branding")]` en `BrandingController`; no fue modificado.

Hashes de evidencia del baseline y salida (idénticos):

- `app-routing.module.ts`: `8aebb136a0f5d1ae9f22b4af1467a3191766aa6836f3314d08e2b9c94150e11c`
- `admin-routing.module.ts`: `b5253d6b1538c61f7e648d310dbe62e5637aa0c9e4cf96900730c8fffae9382a`
- `branding.model.ts`: `0ac8758abdf7dcc3b6ee6bda1fb26e1c5a476c0499c4cbf72b1f763b92e7092a`
- `branding.service.ts`: `04228a1efc0d21b695a8c6d69465c6262935e50994ccdd70e4d998a1868866ed`
- `_tokens.scss`: `a49161a1b8984ba70b4df49fc4a1761ce457ca28e28e304ede1f2a273e291d15`
- backend `BrandingController.cs`: `0d9e1609e29c28dbb7c7b83dbac3be42c13fa7cc9bab262a057e9805694bc573`

## Mapa campo → servicio → runtime → tokens/consumidores

| Campo `BrandingSettings` | Tipo | Aplicación actual en `BrandingService` | Runtime / token resultante | Consumo principal |
|---|---|---|---|---|
| `publicLogo` | **LOGO/DIRECT_BINDING** | Se conserva en `branding$`; no genera CSS var | Ninguno | `login-layout.component.html` enlaza `branding.publicLogo` a `<img [src]>` |
| `privateLogo` | **LOGO/DIRECT_BINDING** | Se conserva en `branding$`; no genera CSS var | Ninguno | `main-layout.component.html` enlaza `branding.privateLogo` a `<img [src]>` |
| `publicBackground` | **BRANDING_RUNTIME** | `setOptionalRuntimeToken`; valor CSS no vacío se aplica directamente, ausencia elimina override inline | `--brand-public-background` (default CSS `#f4f7fb`) | `login-layout.component.scss :host` → `background` |
| `privateBackground` | **BRANDING_RUNTIME** | `setOptionalRuntimeToken`; valor CSS no vacío se aplica directamente, ausencia elimina override inline | `--brand-private-background` (default CSS `#f8fafc`) | `main-layout.component.scss :host` y `.shell-container` |
| `sidebarBackground` | **BRANDING_RUNTIME** | Fondo se aplica a `--brand-sidebar-background`; además se calcula contraste sobre hex normalizado o default | `--brand-sidebar-background` → `--color-sidebar`; contraste → `--brand-sidebar-on-background` → `--color-on-sidebar` | Sidenav y texto/iconos/hover/active del `MainLayoutComponent` |
| `buttonColor` | **BRANDING_RUNTIME** | Se normaliza a hex; fallback `#2563eb`; calcula contraste y hover (`-12%` de lightness) | `--brand-primary`, `--brand-on-primary`, `--brand-primary-hover`, `--mat-sys-primary`, `--mat-sys-on-primary`, `--button-color` | Semánticos de primary/focus, botones compartidos, numerosos features y Material primary |

El `BrandingSettingsComponent` guarda los seis campos juntos mediante `BrandingService.updateBranding(...)`; el service hace `PUT api/users/branding` y reaplica el estado retornado. En construcción/refresh hace `GET api/users/branding` y aplica los mismos tokens sin recompilar.

## Puente BRANDING_RUNTIME → design tokens

### `buttonColor`

Runtime directo:

- `--brand-primary` = color normalizado.
- `--brand-on-primary` = contraste calculado (`#0f172a` o `#ffffff`).
- `--brand-primary-hover` = variante oscurecida calculada.
- `--mat-sys-primary` = primary runtime.
- `--mat-sys-on-primary` = contraste runtime.
- `--button-color` = alias legacy runtime.

Aliases semánticos de `_tokens.scss` que por tanto son configurables en runtime:

- `--color-primary` → `--brand-primary`.
- `--color-on-primary` → `--brand-on-primary`.
- `--color-primary-contrast` → `--color-on-primary`.
- `--color-primary-hover` → `--brand-primary-hover`.
- `--color-focus` → `--color-primary`.

### `sidebarBackground`

- `--brand-sidebar-background` → `--color-sidebar`.
- `--brand-sidebar-on-background` (contraste calculado) → `--color-on-sidebar`.

### Fondos público/privado

- `publicBackground` controla `--brand-public-background` directamente; no pasa por un token semántico foundation.
- `privateBackground` controla `--brand-private-background` directamente; no redefine `--color-background`, `--color-surface` ni Material background/surface.

## Material: cobertura runtime real

`styles.scss` genera la estructura Material desde paletas estáticas y enlaza varios system tokens a los design tokens. `BrandingService` sobrescribe inline **solo**:

- `--mat-sys-primary`.
- `--mat-sys-on-primary`.

Por ello esos dos sí son **BRANDING_RUNTIME**. En cambio `--mat-sys-background`, `--mat-sys-on-background`, `--mat-sys-surface`, `--mat-sys-surface-container`, `--mat-sys-surface-container-low`, `--mat-sys-on-surface`, `--mat-sys-on-surface-variant`, `--mat-sys-outline`, `--mat-sys-error` y `--mat-sys-on-error` siguen derivados de tokens estáticos. Tokens Material generados como `--mat-sys-primary-container` / `--mat-sys-on-primary-container` tampoco son sobrescritos por `BrandingService` y no siguen `buttonColor` actualmente.

## Tokens realmente configurables vs estáticos

### BRANDING_RUNTIME

Base runtime:

- `--brand-primary`
- `--brand-on-primary`
- `--brand-primary-hover`
- `--brand-public-background`
- `--brand-private-background`
- `--brand-sidebar-background`
- `--brand-sidebar-on-background`
- `--mat-sys-primary`
- `--mat-sys-on-primary`
- `--button-color`

Semánticos configurables por alias:

- `--color-primary`
- `--color-on-primary`
- `--color-primary-contrast`
- `--color-primary-hover`
- `--color-focus`
- `--color-sidebar`
- `--color-on-sidebar`

### LOGO/DIRECT_BINDING

- `publicLogo`: binding directo en login.
- `privateLogo`: binding directo en shell privado.

### DESIGN_TOKEN_STATIC

Permanecen estáticos con el contrato actual:

- foundation: `--color-background`, `--color-bg`, `--color-surface`, `--color-surface-muted`, `--color-border`, `--color-border-strong`, `--color-outline`, `--color-text`, `--color-text-muted`, `--color-text-soft`;
- secondary: `--color-secondary`, `--color-secondary-hover`, `--color-on-secondary`;
- error/danger: `--color-error`, `--color-error-hover`, `--color-error-surface`, `--color-error-border`, `--color-error-text`, `--color-on-error`, aliases danger;
- warning y success;
- radios, sombras, spacing y escalas tipográficas;
- Material background/surface/error/outline y primary-container derivados/generados no sobrescritos por el service.

## Consumidores principales de branding configurable

Conteo estático de llamadas `var(...)` exactas en `web/ach-interbank-ui/src`:

- `--brand-public-background`: **1** uso / **1** archivo (`login-layout`).
- `--brand-private-background`: **2** usos / **1** archivo (`main-layout`).
- `--color-sidebar`: **1** uso / **1** archivo; `--color-on-sidebar`: **7** usos / **1** archivo (`main-layout`).
- `--color-primary`: **112** usos / **41** archivos. Principales concentraciones: certificate manager (12), `_components.scss` (9), variants/fields (7), UAT inbound simulator (7), `ui-boton` (7), digital-envelope (6).
- `--color-primary-hover`: **16** usos / **10** archivos.
- `--color-focus`: **7** usos / **5** archivos (incluye el alias en `_tokens.scss`).
- `--color-on-primary`: **6** usos / **6** archivos (incluye el alias en `_tokens.scss`).
- `--mat-sys-primary`: **6** usos directos / **6** archivos de UI; `--mat-sys-on-primary`: **1** uso directo / **1** archivo, además de los componentes Material que consumen esos system vars internamente.
- `--button-color`: **3** usos / **3** archivos admin legacy (`user-form`, `password-rules-settings`, `login-lockout-settings`).

Los logos tienen un consumidor de layout principal cada uno, además de sus previews en `/users/branding`.

## Próximo micro-cambio mecánico recomendado — NO implementado

El siguiente cambio de menor ambigüedad para aumentar cobertura **real** del `buttonColor` existente es sustituir únicamente las dos asignaciones globales de hover en:

`web/ach-interbank-ui/src/styles/_components.scss`

```scss
--btn-color: #1d4ed8;
```

por:

```scss
--btn-color: var(--color-primary-hover);
```

Aparecen en los estados hover de `button.ghost/.btn-outline` y de botones de acción sin clase dentro de tablas. La equivalencia default es exacta (`--brand-primary-hover: #1d4ed8`) y `--color-primary-hover` ya sigue dinámicamente a `buttonColor`; no requiere campo, token, contrato ni lógica nuevos. No se recomienda tocar en el mismo micro-lote los fondos/bordes tintados vecinos (`#eff6ff`, `#93c5fd`), porque no tienen equivalencia exacta runtime actual.

## Validación arquitectónica y de inmutabilidad

Scan estático de imports TypeScript relativos resolubles bajo `web/ach-interbank-ui/src/app`:

```text
feature -> feature: 0
core -> features:   0
shared -> features: 0
```

Resultados contra el baseline extraído:

```text
UI source diff: 0
backend diff: 0
routing diff: 0
```

- Backend `src/`: `1474` archivos; diff `0`.
- `/users/branding` permanece presente con sus guards/permisos intactos.
- `api/users/branding` permanece presente en `BrandingService` y `BrandingController` sin cambios.
- El diff global del ZIP de salida agrega únicamente este reporte.

## Angular

Se comprobó únicamente:

`web/ach-interbank-ui/node_modules/.bin/ng`

Resultado exacto:

```text
ng: not found
```

No se ejecutó `npm ping`, `npm ci`, registry retries, build ni tests. **No se declara VERIFIED**.
