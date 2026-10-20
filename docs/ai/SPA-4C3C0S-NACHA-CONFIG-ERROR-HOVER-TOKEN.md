# SPA 4C.3C0s — NACHA Config Variants/Fields Error-Hover Token

## Estado

`4C3C0S_NACHA_CONFIG_ERROR_HOVER_TOKEN_IMPLEMENTED_VALIDATION_PENDING`

No se declara `VERIFIED`: no se ejecutaron build/tests Angular porque el binario local `web/ach-interbank-ui/node_modules/.bin/ng` no existe.

## Baseline

- ZIP: `ACHInterbank-v92-spa-4C3C0r-nacha-config-primary-hover-token.zip`
- SHA-256 esperado/verificado: `f541d47184ebedc0b7a3467d5140c7cef62bfdb072adedd3f820bd44f75efa5a`

## Cambio único

Archivo:

`web/ach-interbank-ui/src/app/features/nacha-config-admin/pages/nacha-config-variants-fields-page.component.scss`

Precondición confirmada en `web/ach-interbank-ui/src/styles/_tokens.scss`:

```scss
--color-error-hover: #b91c1c;
```

Se reemplazó exclusivamente la única declaración:

```scss
color: #b91c1c !important;
```

por:

```scss
color: var(--color-error-hover) !important;
```

La ocurrencia estaba en la línea baseline 135.

No se modificó ningún otro color, selector, propiedad ni `!important`.

## Evidencia estática

Conteos sobre el SCSS objetivo:

- `color: #b91c1c !important;` baseline: **1**
- `color: #b91c1c !important;` posterior: **0**
- `color: var(--color-error-hover) !important;` posterior: **1**
- `#b91c1c` posterior: **0**
- CRLF: **700 -> 700**

La validación byte-a-byte construyó el resultado esperado mediante una sustitución binaria exacta de la única ocurrencia sobre el baseline y lo comparó con el archivo de salida: **idéntico**.

SHA-256 del SCSS:

- baseline: `f6f392c3db54ed5abb029b16ade3bc1d0c010ec4f5151945c039185fa95fddb5`
- salida: `a30d6541fb7af2dc83bbae5a5e988bd704249626e93d9f7fdf9d692a1bed832e`

## Arquitectura y regresiones estáticas

Escaneo de imports TypeScript en `web/ach-interbank-ui/src/app`:

- feature -> feature: **0**
- core -> features: **0**
- shared -> features: **0**

Backend:

- diferencias baseline/salida en `src`: **0**

Rutas:

- archivos de routing comparados: **29**
- diferencias SHA-256 baseline/salida: **0**
- `app-routing.module.ts`, `nacha-config-admin-routing.module.ts` y `nacha-operational-routing.module.ts`: hashes idénticos al baseline.
- las rutas públicas NACHA Config Admin, guards/permisos y redirects existentes permanecen intactos.

Package/config protegido:

- `package.json`, `package-lock.json`, `angular.json`, `tsconfig.json`, `tsconfig.app.json`, `tsconfig.spec.json`: **0 cambios**.

Antes de agregar este documento de checkpoint, `diff -qr` entre baseline y salida reportó exactamente un archivo modificado: el SCSS objetivo.

## Angular

Se comprobó únicamente:

`web/ach-interbank-ui/node_modules/.bin/ng`

Resultado exacto:

```text
ng: not found
```

No se ejecutaron `npm ping`, `npm ci`, retries de registry, build ni tests.

## Comandos/evidencia resumida

```text
sha256sum ACHInterbank-v92-spa-4C3C0r-nacha-config-primary-hover-token.zip
# f541d47184ebedc0b7a3467d5140c7cef62bfdb072adedd3f820bd44f75efa5a

grep -nF -- '--color-error-hover: #b91c1c;' web/ach-interbank-ui/src/styles/_tokens.scss
# --color-error-hover: #b91c1c;

# sustitución binaria exacta con precondición count == 1
# expected_bytes == output_bytes -> True

# import scan
# feature->feature = 0
# core->features = 0
# shared->features = 0

# backend diff = 0
# routing manifest diff = 0

web/ach-interbank-ui/node_modules/.bin/ng
# ng: not found
```

No se avanzó al siguiente lote.
