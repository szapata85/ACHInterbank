# SPA 4C.3C0x — NACHA Config records error-surface token

Estado: `4C3C0X_NACHA_CONFIG_RECORDS_ERROR_SURFACE_TOKEN_IMPLEMENTED_VALIDATION_PENDING`

## Alcance

Baseline: `ACHInterbank-v97-spa-4C3C0w-nacha-config-records-surface-token.zip`

SHA-256 baseline verificado:

`630316125651c8e4e14f766bed3ca00c5cf94ac6281485b472fcf65d82dabcfa`

Único cambio funcional realizado:

`web/ach-interbank-ui/src/app/features/nacha-config-admin/pages/nacha-config-records-page.component.scss`

```diff
-  background: #fef2f2;
+  background: var(--color-error-surface);
```

## Precondición

En `web/ach-interbank-ui/src/styles/_tokens.scss` se confirmó:

```scss
--color-error-surface: #fef2f2;
```

Comando de evidencia:

```bash
grep -nF -- '--color-error-surface: #fef2f2;' web/ach-interbank-ui/src/styles/_tokens.scss
```

Resultado: una definición encontrada.

## Validación del cambio

Conteos en el SCSS objetivo:

- `background: #fef2f2;`: `1 -> 0`
- `background: var(--color-error-surface);`: `0 -> 1`
- `color: #991b1b;`: `1 -> 1` (intacto)
- `#ecfdf5`: `1 -> 1` (intacto)
- `#166534`: `1 -> 1` (intacto)
- `#eff6ff`: `1 -> 1` (intacto)
- `#1d4ed8`: `1 -> 1` (intacto)

La comparación binaria confirmó que el archivo resultante es exactamente el baseline con una única sustitución de bytes de `background: #fef2f2;` por `background: var(--color-error-surface);`. Los 147 finales CRLF se preservaron.

SHA-256 del SCSS objetivo:

- baseline: `cc699a61871a7fed6d366c550ec25532f60a616b597465f290af1ac10f10728c`
- salida: `2152d869133e12ada275df2cd22a83341708a50500a031462f7c464c191dcc2c`

## Validación arquitectónica estática

Escaneo de imports TypeScript bajo `web/ach-interbank-ui/src/app`:

- feature -> feature: `0`
- core -> features: `0`
- shared -> features: `0`

El escaneo resolvió imports relativos y referencias explícitas a `features/`, `core/` y `shared/`.

Backend:

```text
backend_diff=0
```

Comparación de routing TypeScript: `29` archivos revisados, `0` modificados. Por ello las rutas públicas, guards y permisos permanecen idénticos al baseline.

Antes de agregar este reporte, el diff de `web/ach-interbank-ui/src` contra el baseline contenía únicamente `nacha-config-records-page.component.scss`.

## Angular

Se comprobó únicamente:

`web/ach-interbank-ui/node_modules/.bin/ng`

Resultado exacto:

```text
ng: not found
```

No se ejecutó `npm ping`, `npm ci`, retries de registry, build ni tests. No se declara `VERIFIED`.

## Cierre

Estado final:

`4C3C0X_NACHA_CONFIG_RECORDS_ERROR_SURFACE_TOKEN_IMPLEMENTED_VALIDATION_PENDING`

No se avanzó al siguiente lote.
