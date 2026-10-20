# SPA 4C.3C0am — Variants/Fields Focus Branding

Estado: `4C3C0AM_VARIANTS_FIELDS_FOCUS_BRANDING_IMPLEMENTED_VALIDATION_PENDING`

## Alcance

Se modificó exclusivamente `web/ach-interbank-ui/src/app/features/nacha-config-admin/pages/nacha-config-variants-fields-page.component.scss`.

Reemplazo único:

```scss
outline: 3px solid rgba(37, 99, 235, .28);
```

por:

```scss
outline: 3px solid color-mix(in srgb, var(--color-focus) 28%, transparent);
```

## Precondiciones confirmadas

- `--color-focus: var(--color-primary);`
- `--color-primary: var(--brand-primary);`
- `--focus-ring: 0 0 0 3px color-mix(in srgb, var(--color-focus) 28%, transparent);`
- `BrandingService` deriva `--brand-primary` de `buttonColor` y lo establece en runtime.

## Validaciones estáticas

- Reemplazos exactos: 1.
- `rgba(37, 99, 235, .28)` posterior en el SCSS objetivo: 0.
- El resto del SCSS es byte-a-byte idéntico salvo la línea solicitada.
- CRLF del SCSS: 700 antes / 700 después.
- feature -> feature: 0.
- core -> features: 0.
- shared -> features: 0.
- Backend `src/`: 0 cambios.
- Routing: 29 archivos revisados / 0 cambios.
- `BrandingService`: intacto.
- `/users/branding`: intacta.
- `api/users/branding`: intacto.
- `web/ach-interbank-ui/node_modules/.bin/ng`: `ng: not found`.

No se ejecutó `npm ping`, `npm ci`, registry retries, build ni tests. No se declara VERIFIED.
