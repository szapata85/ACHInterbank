# SPA 4C.3C0ai — Branding Runtime Coverage Checkpoint

**Tipo:** READ-ONLY checkpoint  
**Baseline:** `ACHInterbank-v108-spa-4C3C0ah-shared-button-primary-hover-branding.zip`  
**Baseline SHA-256:** `3435bf400295ca0202934e483cc62bab1cecb272b502bba74d3797bd91bbbc2d`  
**Estado:** `4C3C0AI_BRANDING_RUNTIME_COVERAGE_CHECKPOINT_STATIC_OK_VALIDATION_PENDING`

## 1. Alcance y resultado

Se auditó sin modificar source el contrato de branding actual (`BrandingSettings`), su aplicación por `BrandingService`, los runtime CSS vars, los aliases semánticos de `_tokens.scss`, el bridge de Angular Material y sus consumidores.

Clasificaciones usadas:

- **BRANDING_RUNTIME:** valor del contrato que llega a CSS vars en runtime y, desde allí, a tokens/consumidores.
- **DESIGN_TOKEN_STATIC:** token/valor visual que permanece estático y no forma parte del contrato de branding.
- **LOGO/DIRECT_BINDING:** valor del contrato consumido directamente por Angular, sin CSS var.

## 2. Cobertura por campo de `BrandingSettings`

| Campo | Clasificación | Flujo runtime actual | Consumidor principal | Bypass real detectado |
|---|---|---|---|---|
| `publicLogo` | LOGO/DIRECT_BINDING | `BrandingService.branding$` → `branding.publicLogo` | `login-layout.component.html` → `<img [src]="branding.publicLogo">` | **No.** El texto `ACH` es fallback cuando no existe logo, no un bypass. |
| `privateLogo` | LOGO/DIRECT_BINDING | `BrandingService.branding$` → `branding.privateLogo` | `main-layout.component.html` → `<img [src]="branding.privateLogo">` | **No.** El texto `ACH` es fallback cuando no existe logo. |
| `publicBackground` | BRANDING_RUNTIME | `BrandingService` → `--brand-public-background` | `login-layout.component.scss` `:host { background: var(--brand-public-background); }` | **No.** `#f4f7fb` solo queda como default del contrato/token, no como fondo público consumidor hardcoded. |
| `privateBackground` | BRANDING_RUNTIME | `BrandingService` → `--brand-private-background` | `main-layout.component.scss` en `:host` y `.shell-container` | **No.** Otros `#f8fafc` son superficies locales/estáticas y no representan el fondo privado del shell. |
| `sidebarBackground` | BRANDING_RUNTIME | `BrandingService` → `--brand-sidebar-background`; contraste calculado → `--brand-sidebar-on-background`; `_tokens.scss` → `--color-sidebar` / `--color-on-sidebar` | `main-layout.component.scss` → sidenav, texto, iconos, hover/active derivados | **No** en el shell. Coincidencias `#0f172a` fuera del sidebar son texto/contraste estático y no bypass del campo. |
| `buttonColor` | BRANDING_RUNTIME | `buttonColor` normalizado → `--brand-primary`; contraste → `--brand-on-primary`; hover calculado → `--brand-primary-hover`; también `--mat-sys-primary`, `--mat-sys-on-primary`, `--button-color` | shared buttons, shell/login accents, NACHA config, transacciones, Material y 3 consumidores legacy de `--button-color` | **Sí.** Quedan consumidores semánticos que aún fijan color/foreground de la familia primaria fuera de los runtime tokens. |

## 3. Mapa de tokens realmente configurables

### BRANDING_RUNTIME

`buttonColor` controla en runtime:

- `--brand-primary` → `--color-primary`
- `--brand-on-primary` → `--color-on-primary` → `--color-primary-contrast`
- `--brand-primary-hover` → `--color-primary-hover`
- `--color-focus` → `--color-primary` y, por tanto, `--focus-ring`
- `--mat-sys-primary`
- `--mat-sys-on-primary`
- `--button-color` (bridge legacy)

`sidebarBackground` controla:

- `--brand-sidebar-background` → `--color-sidebar`
- contraste derivado `--brand-sidebar-on-background` → `--color-on-sidebar`

`publicBackground` y `privateBackground` llegan directamente a `--brand-public-background` y `--brand-private-background`.

Referencias consumidoras (excluyendo `_tokens.scss`, `BrandingService` y su spec):

- `--brand-public-background`: **1** referencia / **1** archivo.
- `--brand-private-background`: **2** referencias / **1** archivo.
- `--color-sidebar`: **1** referencia / **1** archivo.
- `--color-on-sidebar`: **7** referencias / **1** archivo.
- `--color-primary`: **135** referencias / **43** archivos.
- `--color-on-primary`: **5** referencias / **5** archivos.
- `--color-primary-contrast`: **6** referencias / **4** archivos.
- `--color-primary-hover`: **18** referencias / **10** archivos.
- `--color-focus`: **6** referencias / **4** archivos.
- `--mat-sys-primary`: **13** referencias / **8** archivos.
- `--mat-sys-on-primary`: **8** referencias / **6** archivos.
- `--button-color`: **3** referencias / **3** archivos.

### DESIGN_TOKEN_STATIC

Permanecen fuera del contrato actual, entre otros:

- `--color-background`, `--color-surface`, `--color-surface-muted`
- `--color-border`, `--color-border-strong`, `--color-outline`
- `--color-text`, `--color-text-muted`, `--color-text-soft`
- secondary (`--color-secondary*`, `--color-on-secondary`)
- error/danger, warning y success
- Material background/surface/outline/error y el resto de la paleta generada estáticamente

Por ello, coincidencias como `#f8fafc`, `#0f172a`, `#eff6ff`, `#93c5fd` o `#bfdbfe` no se consideran automáticamente bypass de branding: solo lo son cuando el selector tiene semántica primaria/sidebar/background del contrato actual.

## 4. Bypass reales restantes del branding actual

Se identificaron estos gaps semánticos de `buttonColor`:

1. `features/cenit/components/cenit-home.component.scss`, `.link-operativo`: usa `background: var(--color-primary)` pero mantiene `color: #fff`; el foreground ignora `--color-on-primary` calculado por `BrandingService`.
2. `features/customer-third-parties/components/customer-third-parties.component.css`, `.detail-panel__notice`: usa `border-left: 4px solid #2563eb`; el acento conserva el default en vez de `--color-primary`.
3. `features/nacha-config-admin/pages/nacha-config-variants-fields-page.component.scss`, grupo `:focus-visible`: usa `outline: 3px solid rgba(37, 99, 235, .28)` aunque el sistema ya dispone de `--color-focus` / `--focus-ring` derivados de `buttonColor`.
4. `features/navigation-logs/components/navigation-log.component.css`, `button.primary`: background runtime `--color-primary` pero foreground `--color-surface`. No es un literal hardcoded, pero sí usa un **DESIGN_TOKEN_STATIC** como sustituto de la semántica dinámica `--color-on-primary`.

No se clasificaron como bypass los azules claros estáticos (`#eff6ff`, `#93c5fd`, `#bfdbfe`) usados como superficies/bordes de información o ghost/outline, porque el contrato actual no expone un token runtime exacto equivalente.

## 5. Próximo micro-cambio mecánico seguro — NO IMPLEMENTADO

Recomendación única:

```scss
/* features/cenit/components/cenit-home.component.scss */
.link-operativo {
  background: var(--color-primary);
  color: var(--color-on-primary); // reemplazar únicamente color: #fff;
}
```

Razón: el selector ya consume `--color-primary`; `BrandingService` calcula y publica `--brand-on-primary` según contraste, y `_tokens.scss` lo expone exactamente como `--color-on-primary`. Es un cambio de una línea, sin token/contrato nuevo, y corrige cobertura real cuando `buttonColor` es claro.

## 6. Ruta y endpoint de branding

- `/users/branding` sigue formado por `app-routing.module.ts` (`users`) + `admin-routing.module.ts` (`branding`).
- La ruta conserva los guards heredados `roleGuard` + `permissionGuard`, rol `Admin` y permiso `CanManageUsers`.
- Frontend conserva `GET/PUT api/users/branding` en `BrandingService`.
- Backend conserva `[Route("api/users/branding")]` en `BrandingController`.

No se modificó ni reconcilió ningún permiso/contrato durante este checkpoint.

## 7. Validación estática

- UI source diff contra baseline: **0**.
- Backend diff contra baseline: **0**.
- TypeScript feature → feature: **0**.
- TypeScript core → features: **0**.
- TypeScript shared → features: **0**.
- Rutas: **intactas**.
- `/users/branding`: **intacta**.
- `api/users/branding`: **intacto**.
- El ZIP de salida agrega únicamente este reporte.

Comandos/evidencia principal:

```text
sha256sum ACHInterbank-v108-spa-4C3C0ah-shared-button-primary-hover-branding.zip
rg "publicLogo|privateLogo|publicBackground|privateBackground|sidebarBackground|buttonColor" web/ach-interbank-ui/src/app
rg "--brand-|--color-primary|--color-on-primary|--color-primary-hover|--color-sidebar|--color-on-sidebar|--mat-sys-primary|--mat-sys-on-primary|--button-color" web/ach-interbank-ui/src
rg "#2563eb|#1d4ed8|rgba\(37, 99, 235|color: #fff" web/ach-interbank-ui/src
```

Angular se comprobó únicamente en:

`web/ach-interbank-ui/node_modules/.bin/ng`

Resultado:

```text
ng: not found
```

No se ejecutó `npm ping`, `npm ci`, retries de registry, build ni tests. **No se declara VERIFIED.**
