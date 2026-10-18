# SPA 4C.3C0ap — Branding Runtime Final Closure Checkpoint

**Tipo:** READ-ONLY checkpoint final  
**Baseline:** `ACHInterbank-v115-spa-4C3C0ao-admin-legacy-primary-buttons-on-primary-branding.zip`  
**Baseline SHA-256 esperado/verificado:** `432d03076f90cf2644a5c6e53d0ab1dbb0cb8105bd976d75381084502af9185a`

No se implementó ningún cambio de source. El único archivo agregado por este micro-lote es este reporte.

## Resultado del cierre

Los **7 bypasses tratados previamente** permanecen resueltos y su conteo pendiente es **0**:

1. CENIT Home `.link-operativo` → `color: var(--color-on-primary)`.
2. Third Parties `.detail-panel__notice` → `border-left: ... var(--color-primary)`.
3. Navigation Log `button.primary` → `color: var(--color-on-primary)`.
4. Variants/Fields focus → `color-mix(in srgb, var(--color-focus) 28%, transparent)`.
5. Admin `user-form` `button.primary` → `color: var(--color-on-primary)`.
6. Admin `password-rules-settings` `button.primary` → `color: var(--color-on-primary)`.
7. Admin `login-lockout-settings` `button.primary` → `color: var(--color-on-primary)`.

El scan global final encontró **1 bypass semántico runtime nuevo**, concentrado en el chrome del scrollbar de la navegación lateral configurable. Son **3 declaraciones CSS del mismo consumidor lógico** en `main-layout.component.scss`:

```scss
scrollbar-color: rgba(226, 232, 240, 0.28) transparent;
background: rgba(226, 232, 240, 0.28);
background-color: rgba(226, 232, 240, 0.48);
```

Estas declaraciones pertenecen al scrollbar de `.navigation`, que se renderiza dentro de `.primary-sidenav`. El fondo del sidenav sí sigue `sidebarBackground` mediante `--color-sidebar`, y el contraste runtime calculado sí está disponible mediante `--color-on-sidebar`, pero el thumb del scrollbar conserva un gris claro fijo adecuado al sidebar oscuro por defecto. Si el administrador configura un sidebar claro, ese chrome no sigue el contraste runtime y puede perder contraste. Por evidencia semántica directa, este caso se clasifica como **BYPASS_REAL**, no como coincidencia cromática estática.

Conteos exigidos:

- **bypasses conocidos previos pendientes = 0**
- **bypasses globales nuevos encontrados = 1** consumidor lógico (**3 declaraciones**) 
- **bypasses runtime totales pendientes = 1**

**Estado:** `4C3C0AP_BRANDING_RUNTIME_FINAL_CHECKPOINT_BYPASSES_REMAIN_VALIDATION_PENDING`

## 1. Campo administrable → runtime/direct binding → tokens → consumidores

| Campo | Clasificación | Aplicación actual | Bridge / token | Consumidor principal | Resultado |
|---|---|---|---|---|---|
| `publicLogo` | **LOGO/DIRECT_BINDING** | `BrandingService.branding$` conserva el campo | binding directo, sin CSS var | `login-layout.component.html` → `<img [src]="branding.publicLogo">` | Conectado; sin bypass real |
| `privateLogo` | **LOGO/DIRECT_BINDING** | `BrandingService.branding$` conserva el campo | binding directo, sin CSS var | `main-layout.component.html` → `<img [src]="branding.privateLogo">` | Conectado; sin bypass real |
| `publicBackground` | **BRANDING_RUNTIME** | `setOptionalRuntimeToken(...)` | `--brand-public-background` | `login-layout.component.scss :host` | Conectado; sin bypass real |
| `privateBackground` | **BRANDING_RUNTIME** | `setOptionalRuntimeToken(...)` | `--brand-private-background` | `main-layout.component.scss :host` + `.shell-container` | Conectado; sin bypass real |
| `sidebarBackground` | **BRANDING_RUNTIME** | fondo runtime + contraste calculado | `--brand-sidebar-background` → `--color-sidebar`; `--brand-sidebar-on-background` → `--color-on-sidebar` | `MainLayoutComponent` sidenav/navigation | Conectado, con **1 BYPASS_REAL** restante en scrollbar |
| `buttonColor` | **BRANDING_RUNTIME** | normalización, contraste, hover, bridges Material/legacy | primary/on-primary/hover/focus + Material + `--button-color` | shared buttons, layouts, features, Material, admin legacy | Cobertura runtime sin bypass real nuevo detectado |

Los textos `ACH` usados cuando no existe logo son fallback funcional y no un bypass de los campos de logo.

## 2. `buttonColor` — cadena runtime revalidada

`BrandingService.applyBranding(...)` conserva la cadena:

- `buttonColor` normalizado → `--brand-primary`.
- contraste calculado para ese primary → `--brand-on-primary`.
- hover derivado del mismo primary → `--brand-primary-hover`.
- primary runtime → `--button-color` para compatibilidad legacy.
- primary runtime → `--mat-sys-primary`.
- contraste runtime → `--mat-sys-on-primary`.

`_tokens.scss` conserva los aliases semánticos:

```scss
--color-primary: var(--brand-primary);
--color-on-primary: var(--brand-on-primary);
--color-primary-contrast: var(--color-on-primary);
--color-primary-hover: var(--brand-primary-hover);
--color-focus: var(--color-primary);
```

Referencias consumidoras actuales bajo `web/ach-interbank-ui/src`, excluyendo `_tokens.scss`, `BrandingService` y su spec:

- `--color-primary`: **136** refs / **44** archivos.
- `--color-on-primary`: **10** refs / **10** archivos.
- `--color-primary-contrast`: **6** refs / **4** archivos.
- `--color-primary-hover`: **18** refs / **10** archivos.
- `--color-focus`: **7** refs / **5** archivos.
- `--mat-sys-primary`: **13** refs / **8** archivos.
- `--mat-sys-on-primary`: **8** refs / **6** archivos.
- `--button-color`: **3** refs / **3** archivos; los tres foregrounds legacy ya usan `--color-on-primary`.

## 3. Logos y backgrounds

- `publicLogo`: `LoginLayoutComponent` expone `branding$`; el HTML consume `branding.publicLogo` con binding directo.
- `privateLogo`: `MainLayoutComponent` expone `branding$`; el HTML consume `branding.privateLogo` con binding directo.
- `publicBackground`: `--brand-public-background` tiene **1** consumidor de layout público.
- `privateBackground`: `--brand-private-background` tiene **2** consumidores en el shell privado.
- `sidebarBackground`: `--color-sidebar` tiene **1** consumidor y `--color-on-sidebar` **7** referencias, todas en el shell privado.

No se detectó un segundo layout público/privado que reemplace conceptualmente esos fondos con el valor default hardcoded. Los `#f8fafc` locales restantes aparecen en superficies internas de componentes y se clasifican como **DESIGN_TOKEN_STATIC**, no como bypass del background privado del shell.

## 4. Scan global de BYPASS_REAL

### 4.1 Foreground fijo sobre primary runtime

Scan de reglas con background gobernado por `--color-primary`, `--brand-primary`, `--button-color` o `--mat-sys-primary` y foreground `white`, `#fff`, `#ffffff` o `--color-surface`:

- **0** consumidores con bypass pendiente.

Los tres usos legacy de `--button-color` están en los tres botones admin tratados por 4C.3C0ao y sus foregrounds son ahora `var(--color-on-primary)`.

### 4.2 Literales del primary/hover default

Scan de `#2563eb`, `#1d4ed8` y formas RGB equivalentes a `37, 99, 235`:

- no quedan en consumidores visuales de implementación;
- permanecen únicamente defaults/fixtures esperados (`branding.model.ts`, `_tokens.scss` y `branding.service.spec.ts`).

Por tanto, no se detectó foreground/background/acento conceptualmente primary que conserve directamente el default exacto en vez de seguir `buttonColor`.

### 4.3 Focus primary

Los focus explícitamente vinculados al primary usan `--color-focus`, `--color-primary` o sus `color-mix(...)`. El bypass exacto de Variants/Fields permanece resuelto. No queda `rgba(37, 99, 235, ...)` en un consumidor visual.

Otros azules de features —por ejemplo paletas propias en Navigation Admin, Scheduler y NACHA Operational— no coinciden con el default runtime y forman parte de paletas locales estáticas; sin evidencia de que representen el `buttonColor` administrable, se clasifican **DESIGN_TOKEN_STATIC**, no **BYPASS_REAL**.

### 4.4 Sidebar configurable

Texto, iconos, hover, expanded, child backgrounds y bordes derivados usan `--color-on-sidebar` o aliases basados en él. El único bypass semántico detectado es el scrollbar descrito arriba: su thumb sigue un gris fijo en vez del contraste runtime del sidebar.

El `box-shadow` divisorio del sidenav y colores de estados warning/error son chrome/bordes o estados semánticos estáticos y permanecen **DESIGN_TOKEN_STATIC**.

### 4.5 Public/private backgrounds

`login-layout` usa `--brand-public-background`; `main-layout` usa `--brand-private-background`. No se detectó bypass de layout para esos dos campos.

## 5. Clasificación obligatoria

### BRANDING_RUNTIME

- `--brand-primary`
- `--brand-on-primary`
- `--brand-primary-hover`
- `--button-color`
- `--mat-sys-primary`
- `--mat-sys-on-primary`
- `--color-primary`
- `--color-on-primary`
- `--color-primary-contrast`
- `--color-primary-hover`
- `--color-focus`
- `--brand-public-background`
- `--brand-private-background`
- `--brand-sidebar-background`
- `--brand-sidebar-on-background`
- `--color-sidebar`
- `--color-on-sidebar`

### LOGO/DIRECT_BINDING

- `publicLogo`
- `privateLogo`

### DESIGN_TOKEN_STATIC

Permanecen fuera del contrato de branding actual, salvo evidencia semántica concreta:

- foundation surface/background/text/border/outline;
- secondary;
- error/danger;
- warning;
- success;
- Material background/surface/outline/error y `primary-container`/`on-primary-container` no sobrescritos por `BrandingService`;
- paletas locales de features que no representan los campos administrables.

Coincidencias de hex como `#f8fafc` o blancos en botones secondary/danger/success no son automáticamente bypasses.

### BYPASS_REAL

**1 consumidor lógico pendiente:** scrollbar de `.navigation` dentro del sidebar configurable, con **3 declaraciones fijas** que no siguen `--color-on-sidebar`.

## 6. Revalidación de los 7 bypasses tratados

Resultado: **7/7 resueltos; 0 pendientes**.

Los cuatro de 4C.3C0ai y los tres de 4C.3C0an continúan consumiendo los tokens runtime esperados; ninguno regresó a un literal o token estático equivalente.

## 7. `/users/branding`, API y contrato intactos

La UI conserva:

- `app-routing.module.ts`: `path: 'users'` bajo `MainLayoutComponent` protegido por `authGuard`.
- `admin-routing.module.ts`: `roleGuard` + `permissionGuard`, rol `Admin`, permiso `CanManageUsers`, child `path: 'branding'` → `BrandingSettingsComponent`.

`BrandingService` conserva `GET/PUT api/users/branding` y el backend conserva `[Route("api/users/branding")]` en `BrandingController`.

El DTO backend conserva los seis campos equivalentes: `PublicLogo`, `PrivateLogo`, `PublicBackground`, `PrivateBackground`, `SidebarBackground`, `ButtonColor`.

Hashes de evidencia del baseline, preservados en salida:

- `branding.model.ts`: `0ac8758abdf7dcc3b6ee6bda1fb26e1c5a476c0499c4cbf72b1f763b92e7092a`
- `branding.service.ts`: `04228a1efc0d21b695a8c6d69465c6262935e50994ccdd70e4d998a1868866ed`
- `_tokens.scss`: `a49161a1b8984ba70b4df49fc4a1761ce457ca28e28e304ede1f2a273e291d15`
- `styles.scss`: `f9a3cad07f925757af32226a9c4457d668d4edc052725fb82f659798a8d0664a`
- `app-routing.module.ts`: `8aebb136a0f5d1ae9f22b4af1467a3191766aa6836f3314d08e2b9c94150e11c`
- `admin-routing.module.ts`: `b5253d6b1538c61f7e648d310dbe62e5637aa0c9e4cf96900730c8fffae9382a`
- backend `BrandingController.cs`: `0d9e1609e29c28dbb7c7b83dbac3be42c13fa7cc9bab262a057e9805694bc573`
- backend `BrandingSettingsDto.cs`: `ed01e95972a8e3001111e4c3a793382eefe96edf81603e98f021113b829f5a39`

## 8. Validación estática e inmutabilidad

Scan estático de imports TypeScript relativos resolubles bajo `web/ach-interbank-ui/src/app`:

```text
feature -> feature: 0
core -> features:   0
shared -> features: 0
```

Validaciones contra el baseline extraído:

- UI source diff: **0**.
- backend `src/`: **1474 archivos / diff 0**.
- routing: **29 archivos / diff 0**.
- `/users/branding`: **intacta**.
- `api/users/branding`: **intacto**.
- El ZIP de salida agrega únicamente este reporte.

## 9. Angular

Se comprobó exclusivamente:

`web/ach-interbank-ui/node_modules/.bin/ng`

Resultado exacto:

```text
ng: not found
```

No se ejecutó `npm ping`, `npm ci`, retries de registry, build ni tests. **No se declara VERIFIED**.
