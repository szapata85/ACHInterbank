# SPA 4C.3C0ao — Admin Legacy Primary Buttons On-Primary Branding

**Tipo:** micro-cambio mecánico acotado  
**Baseline:** `ACHInterbank-v114-spa-4C3C0an-branding-runtime-closure-checkpoint.zip`  
**Baseline SHA-256:** `6c20f28c3ed0fa5ea31e1776b129b94b8ac1374aadc9d08b7174ec91d5e84fb0`

## Resultado

Se resolvieron exclusivamente los tres bypasses semánticos reales reportados por 4C.3C0an en botones `button.primary` legacy de administración.

En cada archivo objetivo se reemplazó exactamente una vez:

```scss
color: white;
```

por:

```scss
color: var(--color-on-primary);
```

Archivos modificados:

1. `web/ach-interbank-ui/src/app/features/admin/components/user-form.component.scss`
2. `web/ach-interbank-ui/src/app/features/admin/components/password-rules-settings.component.scss`
3. `web/ach-interbank-ui/src/app/features/admin/components/login-lockout-settings.component.scss`

**Estado:** `4C3C0AO_ADMIN_LEGACY_PRIMARY_BUTTONS_ON_PRIMARY_BRANDING_IMPLEMENTED_VALIDATION_PENDING`

## Precondiciones confirmadas

Los tres `button.primary` conservaban antes del cambio:

```scss
background: var(--button-color, var(--color-primary));
color: white;
```

`_tokens.scss` conserva:

```scss
--color-on-primary: var(--brand-on-primary);
```

`BrandingService` conserva el bridge runtime desde `buttonColor`:

- `buttonColor` normalizado → `--brand-primary`.
- contraste calculado de ese primary → `--brand-on-primary`.
- el mismo primary → `--button-color` para compatibilidad legacy.

Por tanto, `--brand-on-primary` representa el contraste runtime calculado para el `buttonColor` activo.

## Validación exacta de los tres SCSS

Resultado global:

- reemplazos ejecutados: **3**;
- reemplazos por archivo: **1 / 1 / 1**;
- `color: white;` dentro de los tres `button.primary` después: **0**;
- `color: var(--color-on-primary);` dentro de los tres `button.primary` después: **3**;
- `background: var(--button-color, var(--color-primary));` después: **3 / 3**, intactos.

Cada archivo de salida coincide byte-a-byte con su baseline después de aplicar únicamente su reemplazo esperado. Se conservaron terminadores CRLF:

- `user-form.component.scss`: **70 → 70** CRLF;
- `password-rules-settings.component.scss`: **44 → 44** CRLF;
- `login-lockout-settings.component.scss`: **44 → 44** CRLF.

No se modificó ningún cuarto archivo bajo `web/ach-interbank-ui/src`.

## Scan específico de los bypasses 4C.3C0an

Se escanearon los estilos del SPA para el patrón específico de `button.primary` que combina:

```scss
background: var(--button-color, var(--color-primary));
color: white;
```

Resultado posterior: **0 bypasses pendientes**.

Este micro-lote no realizó un nuevo cierre global de branding y no corrigió hallazgos fuera de ese patrón específico. El scan específico no produjo hallazgos adicionales.

## Inmutabilidad y arquitectura

Scan estático de imports TypeScript relativos resolubles bajo `web/ach-interbank-ui/src/app`:

```text
feature -> feature: 0
core -> features:   0
shared -> features: 0
```

- Backend `src/`: **1474 archivos / diff 0**.
- Routing: **29 archivos / diff 0**.
- `BrandingService`: **intacto**.
- `_tokens.scss`: **intacto**.
- `/users/branding`: **intacta**.
- `api/users/branding`: **intacto** en frontend y backend.
- El diff contra v114 contiene únicamente los tres SCSS objetivo y este reporte.

## Angular

Se comprobó exclusivamente:

`web/ach-interbank-ui/node_modules/.bin/ng`

Resultado exacto:

```text
ng: not found
```

No se ejecutó `npm ping`, `npm ci`, retries de registry, build ni tests. **No se declara VERIFIED**.
