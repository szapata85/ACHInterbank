# SPA 4C.3C0r — NACHA Config Variants/Fields Primary-Hover Token

## Estado

`4C3C0R_NACHA_CONFIG_PRIMARY_HOVER_TOKEN_IMPLEMENTED_VALIDATION_PENDING`

No se declara `VERIFIED`: no se ejecutaron build/tests Angular porque el binario local `web/ach-interbank-ui/node_modules/.bin/ng` no existe.

## Baseline

- ZIP: `ACHInterbank-v91-spa-4C3C0q-nacha-config-on-primary-token.zip`
- SHA-256 esperado/verificado: `b90443afe11464d4aa36d1243a8e17d83732b30e1cccb77d972207cb09fbeacd`

## Cambio único

Archivo:

`web/ach-interbank-ui/src/app/features/nacha-config-admin/pages/nacha-config-variants-fields-page.component.scss`

Precondiciones confirmadas en `web/ach-interbank-ui/src/styles/_tokens.scss`:

```scss
--brand-primary-hover: #1d4ed8;
--color-primary-hover: var(--brand-primary-hover);
```

Se reemplazaron exclusivamente las tres declaraciones exactas:

```scss
color: #1d4ed8;
```

por:

```scss
color: var(--color-primary-hover);
```

Las tres ocurrencias estaban en las líneas baseline 349, 517 y 531.

No se modificaron `#eff6ff`, `#dbeafe` ni ningún otro selector, color o propiedad.

## Evidencia estática

Conteos sobre el SCSS objetivo:

- `color: #1d4ed8;` baseline: **3**
- `color: #1d4ed8;` posterior: **0**
- `color: var(--color-primary-hover);` posterior: **3**
- `#eff6ff`: **4 -> 4**, intacto
- `#dbeafe`: **2 -> 2**, intacto
- CRLF: **700 -> 700**

La validación byte-a-byte construyó el resultado esperado mediante una sustitución binaria exacta de las tres ocurrencias sobre el baseline y lo comparó con el archivo de salida: **idéntico**.

SHA-256 del SCSS:

- baseline: `17c6bc6e38a50b4b9277048cafb53eefc6dd170ad70edab3654e721e699a34ea`
- salida: `f6f392c3db54ed5abb029b16ade3bc1d0c010ec4f5151945c039185fa95fddb5`

## Arquitectura y regresiones estáticas

Escaneo de imports TypeScript en `web/ach-interbank-ui/src/app`:

- feature -> feature: **0**
- core -> features: **0**
- shared -> features: **0**

Backend:

- diferencias baseline/salida en `src`: **0**

Rutas:

- archivos de routing comparados: **29**
- diferencias de paths en manifiesto: **0**
- diferencias SHA-256 baseline/salida: **0**
- las rutas públicas, guards/permisos y redirects existentes permanecen intactos.

Package/config protegido:

- archivos comparados: **6**
- modificados: **0**

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
sha256sum ACHInterbank-v91-spa-4C3C0q-nacha-config-on-primary-token.zip
# b90443afe11464d4aa36d1243a8e17d83732b30e1cccb77d972207cb09fbeacd

grep -n -- '--brand-primary-hover\|--color-primary-hover' web/ach-interbank-ui/src/styles/_tokens.scss
# --brand-primary-hover: #1d4ed8;
# --color-primary-hover: var(--brand-primary-hover);

# sustitución binaria exacta con precondición count == 3
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
