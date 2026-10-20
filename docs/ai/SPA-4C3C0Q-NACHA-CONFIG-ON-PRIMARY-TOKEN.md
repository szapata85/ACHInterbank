# SPA 4C.3C0q — NACHA Config Variants/Fields On-Primary Token

## Estado

`4C3C0Q_NACHA_CONFIG_ON_PRIMARY_TOKEN_IMPLEMENTED_VALIDATION_PENDING`

No se declara `VERIFIED`: no se ejecutaron build/tests Angular porque el binario local `web/ach-interbank-ui/node_modules/.bin/ng` no existe.

## Baseline

- ZIP: `ACHInterbank-v90-spa-4C3C0p-nacha-config-surface-token.zip`
- SHA-256 esperado/verificado: `9dfc5c8273e1e729aec46997a148ab929de45debfe18691897bfee02d56ee6cb`

## Cambio único

Archivo:

`web/ach-interbank-ui/src/app/features/nacha-config-admin/pages/nacha-config-variants-fields-page.component.scss`

Precondiciones confirmadas en `web/ach-interbank-ui/src/styles/_tokens.scss`:

```scss
--brand-on-primary: #ffffff;
--color-on-primary: var(--brand-on-primary);
```

Se reemplazó exclusivamente la única declaración:

```scss
color: #fff;
```

por:

```scss
color: var(--color-on-primary);
```

No se modificó el fallback existente `var(--color-surface, #fff)`, `#fff7f7` ni ningún otro selector, color o propiedad.

## Evidencia estática

Conteos sobre el SCSS objetivo:

- `color: #fff;` baseline: **1**
- `color: #fff;` posterior: **0**
- `color: var(--color-on-primary);` posterior: **1**
- `var(--color-surface, #fff)`: **1 -> 1**, intacto
- `#fff7f7`: **1 -> 1**, intacto
- CRLF: **700 -> 700** líneas con terminador CRLF

La validación byte-a-byte se realizó construyendo el resultado esperado mediante una sustitución binaria exacta de `color: #fff;` por `color: var(--color-on-primary);` sobre el baseline y comparándolo con el archivo de salida; el resultado fue idéntico.

SHA-256 del SCSS:

- baseline: `899455651377278e55d44bd63c1fb456a63f20b4d6c1e505931f3f61a04ba1ba`
- salida: `17c6bc6e38a50b4b9277048cafb53eefc6dd170ad70edab3654e721e699a34ea`

## Arquitectura y regresiones estáticas

Escaneo de imports TypeScript en `web/ach-interbank-ui/src/app`:

- feature -> feature: **0**
- core -> features: **0**
- shared -> features: **0**

Backend:

- diferencias baseline/salida en `src`: **0**

Rutas:

- archivos de routing comparados: **29**
- diferencias de manifiesto SHA-256 baseline/salida: **0**
- las rutas públicas, guards/permisos y redirects existentes permanecen intactos.

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
sha256sum ACHInterbank-v90-spa-4C3C0p-nacha-config-surface-token.zip
# 9dfc5c8273e1e729aec46997a148ab929de45debfe18691897bfee02d56ee6cb

grep -n -- '--brand-on-primary\|--color-on-primary' web/ach-interbank-ui/src/styles/_tokens.scss
# --brand-on-primary: #ffffff;
# --color-on-primary: var(--brand-on-primary);

# sustitución binaria exacta con precondición count == 1
# comparación expected_bytes == output_bytes -> True

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
