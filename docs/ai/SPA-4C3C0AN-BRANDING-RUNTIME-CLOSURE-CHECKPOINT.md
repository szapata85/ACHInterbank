# SPA 4C.3C0an — Branding Runtime Closure Checkpoint

**Tipo:** READ-ONLY checkpoint  
**Baseline:** `ACHInterbank-v113-spa-4C3C0am-variants-fields-focus-branding.zip`  
**Baseline SHA-256:** `78cb1de578f3b037c528be5fd132a9b813a62049ca907c13fb2ef0ff4a469baa`

## Resultado de cierre

Los **4 bypasses conocidos pendientes** identificados en 4C.3C0ai ya están resueltos en el baseline v113:

1. CENIT `.link-operativo`: foreground → `var(--color-on-primary)`.
2. Third Parties `.detail-panel__notice`: acento → `var(--color-primary)`.
3. Navigation Log `button.primary`: foreground → `var(--color-on-primary)`.
4. Variants/Fields `:focus-visible`: outline → `color-mix(in srgb, var(--color-focus) 28%, transparent)`.

Por tanto, **bypasses conocidos pendientes de 4C.3C0ai = 0**.

Sin embargo, el scan global de cierre encontró **3 bypasses semánticos reales adicionales** del `buttonColor` existente, todos en botones legacy de administración. Cada uno usa un background gobernado por branding (`var(--button-color, var(--color-primary))`) pero conserva `color: white`, omitiendo el contraste runtime `--color-on-primary`:

- `web/ach-interbank-ui/src/app/features/admin/components/user-form.component.scss`
- `web/ach-interbank-ui/src/app/features/admin/components/password-rules-settings.component.scss`
- `web/ach-interbank-ui/src/app/features/admin/components/login-lockout-settings.component.scss`

En consecuencia, **bypasses reales restantes = 3** y no corresponde declarar `4C3C0AN_BRANDING_RUNTIME_STATIC_CLOSED_VALIDATION_PENDING`.

**Estado del checkpoint:** `4C3C0AN_BRANDING_RUNTIME_CLOSURE_CHECKPOINT_STATIC_OK_BYPASSES_REMAIN_VALIDATION_PENDING`

## 1. Contrato vigente: campo → runtime/direct binding → consumidor

| Campo | Clasificación | Flujo actual | Consumidor principal | Estado de cobertura |
|---|---|---|---|---|
| `publicLogo` | **LOGO/DIRECT_BINDING** | `BrandingService.branding$` → `branding.publicLogo` | `login-layout.component.html` → `<img [src]="branding.publicLogo">` | Conectado; sin bypass real detectado |
| `privateLogo` | **LOGO/DIRECT_BINDING** | `BrandingService.branding$` → `branding.privateLogo` | `main-layout.component.html` → `<img [src]="branding.privateLogo">` | Conectado; sin bypass real detectado |
| `publicBackground` | **BRANDING_RUNTIME** | `BrandingService` → `--brand-public-background` | `login-layout.component.scss :host` | Conectado; sin bypass real detectado |
| `privateBackground` | **BRANDING_RUNTIME** | `BrandingService` → `--brand-private-background` | `main-layout.component.scss :host` y `.shell-container` | Conectado; sin bypass real detectado |
| `sidebarBackground` | **BRANDING_RUNTIME** | `--brand-sidebar-background` + contraste calculado `--brand-sidebar-on-background` | `--color-sidebar` / `--color-on-sidebar` en `main-layout.component.scss` | Conectado; sin bypass real detectado |
| `buttonColor` | **BRANDING_RUNTIME** | normalización → primary; contraste → on-primary; derivación → hover; bridge Material/legacy | shared buttons, layouts, features, Material, admin legacy | **3 bypasses reales restantes** |

Los textos `ACH` usados cuando no existe logo son fallback funcional y no bypass de `publicLogo`/`privateLogo`.

## 2. `buttonColor` sigue gobernando primary / on-primary / hover / focus

`BrandingService.applyBranding(...)` mantiene el flujo runtime:

- `buttonColor` normalizado → `--brand-primary`.
- contraste calculado → `--brand-on-primary`.
- hover derivado → `--brand-primary-hover`.
- bridge Material → `--mat-sys-primary` / `--mat-sys-on-primary`.
- bridge legacy → `--button-color`.

`_tokens.scss` mantiene:

- `--color-primary: var(--brand-primary)`.
- `--color-on-primary: var(--brand-on-primary)`.
- `--color-primary-contrast: var(--color-on-primary)`.
- `--color-primary-hover: var(--brand-primary-hover)`.
- `--color-focus: var(--color-primary)`.

Referencias consumidoras actuales, excluyendo `_tokens.scss`, `BrandingService` y su spec:

- `--color-primary`: **136** refs / **44** archivos.
- `--color-on-primary`: **7** refs / **7** archivos.
- `--color-primary-contrast`: **6** refs / **4** archivos.
- `--color-primary-hover`: **18** refs / **10** archivos.
- `--color-focus`: **7** refs / **5** archivos.
- `--mat-sys-primary`: **13** refs / **8** archivos.
- `--mat-sys-on-primary`: **8** refs / **6** archivos.
- `--button-color`: **3** refs / **3** archivos.

## 3. Logos y backgrounds siguen conectados

- `publicLogo`: `login-layout.component.ts` consume `BrandingService.branding$`; HTML enlaza `branding.publicLogo` a `<img [src]>`.
- `privateLogo`: `main-layout.component.ts` consume `BrandingService.branding$`; HTML enlaza `branding.privateLogo` a `<img [src]>`.
- `publicBackground`: `--brand-public-background` tiene **1** consumidor, el layout público.
- `privateBackground`: `--brand-private-background` tiene **2** consumidores, ambos en el shell privado.
- `sidebarBackground`: `--color-sidebar` tiene **1** consumidor y `--color-on-sidebar` **7**, todos en el shell privado.

## 4. Scan global de bypasses semánticos

### Bypasses conocidos de 4C.3C0ai

Resultado: **0 pendientes**. Los cuatro sitios originales ya consumen los runtime/design tokens correspondientes.

### Bypasses reales adicionales encontrados

Los únicos usos de `--button-color` fuera del service son tres botones `button.primary` legacy:

```scss
background: var(--button-color, var(--color-primary));
color: white;
```

Como `--button-color` sigue dinámicamente a `buttonColor`, pero `white` no sigue a `--brand-on-primary`, el foreground puede perder contraste cuando el administrador elige un primary claro. Esto es un bypass semántico real de `buttonColor`, no una mera coincidencia cromática.

Conteo: **3 declaraciones / 3 archivos**.

### Literales primary/focus restantes

El scan de `#2563eb`, `#1d4ed8` y `rgba(37, 99, 235, ...)` bajo `web/ach-interbank-ui/src` deja únicamente:

- defaults de `--brand-primary` / `--brand-primary-hover` en `_tokens.scss`;
- default `buttonColor: '#2563eb'` en `branding.model.ts`.

No queda literal primary/focus directo en un consumidor visual fuera de esos defaults.

## 5. BRANDING_RUNTIME vs DESIGN_TOKEN_STATIC

### BRANDING_RUNTIME

Configurable con el contrato actual:

- `--brand-primary`, `--brand-on-primary`, `--brand-primary-hover`;
- `--color-primary`, `--color-on-primary`, `--color-primary-contrast`, `--color-primary-hover`, `--color-focus`;
- `--mat-sys-primary`, `--mat-sys-on-primary`;
- `--button-color`;
- `--brand-public-background`, `--brand-private-background`;
- `--brand-sidebar-background`, `--brand-sidebar-on-background`, `--color-sidebar`, `--color-on-sidebar`.

### DESIGN_TOKEN_STATIC

Siguen fuera del contrato de branding, entre otros:

- surface/background foundation (`--color-background`, `--color-surface`, `--color-surface-muted`);
- border/outline/text;
- secondary, error/danger, warning y success;
- Material surface/background/outline/error y primary-container no sobrescritos por `BrandingService`.

Por tanto, coincidencias como `#f8fafc`, `#0f172a`, `#eff6ff`, `#93c5fd`, `#bfdbfe`, o blancos en botones secondary/danger/success no se clasifican como bypass de branding salvo que el mismo selector esté gobernado por un campo runtime correspondiente. Los `#f8fafc` locales encontrados son superficies de diseño estáticas, no el background privado del shell; los `#0f172a` locales son texto/contraste estático, no el sidebar configurable.

## 6. `/users/branding` y API intactos

La UI conserva:

- `app-routing.module.ts`: `path: 'users'` bajo `MainLayoutComponent` protegido por `authGuard`.
- `admin-routing.module.ts`: guards `roleGuard` + `permissionGuard`, rol `Admin`, permiso `CanManageUsers`, child `path: 'branding'` → `BrandingSettingsComponent`.

El frontend conserva `GET/PUT api/users/branding` en `BrandingService` y el backend conserva `[Route("api/users/branding")]` en `BrandingController`.

El DTO backend conserva exactamente los seis campos equivalentes: `PublicLogo`, `PrivateLogo`, `PublicBackground`, `PrivateBackground`, `SidebarBackground`, `ButtonColor`.

Hashes preservados desde el baseline:

- `branding.model.ts`: `0ac8758abdf7dcc3b6ee6bda1fb26e1c5a476c0499c4cbf72b1f763b92e7092a`
- `branding.service.ts`: `04228a1efc0d21b695a8c6d69465c6262935e50994ccdd70e4d998a1868866ed`
- `_tokens.scss`: `a49161a1b8984ba70b4df49fc4a1761ce457ca28e28e304ede1f2a273e291d15`
- `styles.scss`: `f9a3cad07f925757af32226a9c4457d668d4edc052725fb82f659798a8d0664a`
- `app-routing.module.ts`: `8aebb136a0f5d1ae9f22b4af1467a3191766aa6836f3314d08e2b9c94150e11c`
- `admin-routing.module.ts`: `b5253d6b1538c61f7e648d310dbe62e5637aa0c9e4cf96900730c8fffae9382a`
- backend `BrandingController.cs`: `0d9e1609e29c28dbb7c7b83dbac3be42c13fa7cc9bab262a057e9805694bc573`

## 7. Validación estática e inmutabilidad

Scan estático de imports TypeScript relativos resolubles bajo `web/ach-interbank-ui/src/app`:

```text
feature -> feature: 0
core -> features:   0
shared -> features: 0
```

- UI source diff contra baseline: **0**.
- Backend `src/`: **1474 archivos / diff 0**.
- Routing: **29 archivos / diff 0**.
- `/users/branding`: **intacta**.
- `api/users/branding`: **intacto**.
- El ZIP de salida agrega únicamente este reporte.

## 8. Angular

Se comprobó únicamente:

`web/ach-interbank-ui/node_modules/.bin/ng`

Resultado exacto:

```text
ng: not found
```

No se ejecutó `npm ping`, `npm ci`, retries de registry, build ni tests. **No se declara VERIFIED**.
