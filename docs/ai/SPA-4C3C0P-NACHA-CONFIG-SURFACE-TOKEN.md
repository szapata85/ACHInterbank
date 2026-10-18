# SPA 4C.3C0p — NACHA Config Variants/Fields Surface Token

## Estado

`4C3C0P_NACHA_CONFIG_SURFACE_TOKEN_IMPLEMENTED_VALIDATION_PENDING`

No se declara `VERIFIED`: no se ejecutaron build/tests Angular porque el binario local `web/ach-interbank-ui/node_modules/.bin/ng` no existe.

## Baseline

- ZIP: `ACHInterbank-v89-spa-4C3C0o-nacha-config-border-token.zip`
- SHA-256 esperado/verificado: `2c8ada1e6c776c656da36228f29b72bf4f32a7dc63a922f6f5a027297746a256`

## Cambio único

Archivo:

`web/ach-interbank-ui/src/app/features/nacha-config-admin/pages/nacha-config-variants-fields-page.component.scss`

Precondición confirmada en `web/ach-interbank-ui/src/styles/_tokens.scss`:

```scss
--color-surface: #ffffff;
```

Se reemplazaron exclusivamente las 6 declaraciones exactas:

```scss
background: #fff;
```

por:

```scss
background: var(--color-surface);
```

No se modificó `color: #fff;` ni el fallback existente `background: var(--color-surface, #fff);`.

## Evidencia estática

Conteos sobre el SCSS objetivo:

- `background: #fff;` baseline: **6**
- `background: #fff;` posterior: **0**
- `background: var(--color-surface);` posterior: **6**
- `color: #fff;`: **1 -> 1**, intacto
- `var(--color-surface, #fff)`: **1 -> 1**, intacto
- CRLF: **700 -> 700** líneas con terminador CRLF

La validación byte-a-byte se realizó construyendo el resultado esperado mediante una sustitución binaria exacta de `background: #fff;` por `background: var(--color-surface);` sobre el baseline y comparándolo con el archivo de salida; el resultado fue idéntico.

SHA-256 del SCSS:

- baseline: `d57f13a991cea7ac6d9feddb52d02a82cfe7ffa2b20a7f26e7a7cb18a2407afc`
- salida: `899455651377278e55d44bd63c1fb456a63f20b4d6c1e505931f3f61a04ba1ba`

## Arquitectura y regresiones estáticas

Escaneo de imports TypeScript en `web/ach-interbank-ui/src/app`:

- feature -> feature: **0**
- core -> features: **0**
- shared -> features: **0**

Backend:

- archivos en `src`: **1474**
- diferencias baseline/salida en `src`: **0**

Rutas:

- archivos de routing comparados: **29**
- diferencias de manifiesto SHA-256 baseline/salida: **0**
- por tanto, las rutas públicas y redirects existentes permanecen intactos.

Package/config protegido: sin modificaciones.

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
sha256sum ACHInterbank-v89-spa-4C3C0o-nacha-config-border-token.zip
# 2c8ada1e6c776c656da36228f29b72bf4f32a7dc63a922f6f5a027297746a256

grep -- '--color-surface:' web/ach-interbank-ui/src/styles/_tokens.scss
# --color-surface: #ffffff;

# sustitución binaria exacta con precondición count == 6
# comparación expected_bytes == output_bytes -> True

# import scan
# feature->feature = 0
# core->features = 0
# shared->features = 0

# backend diff
# 0

# routing manifest diff
# 0

web/ach-interbank-ui/node_modules/.bin/ng
# ng: not found
```

No se avanzó al siguiente lote.
