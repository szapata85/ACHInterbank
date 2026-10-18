# SPA 4C.3C0o — NACHA Config Variants/Fields Border Token

## Alcance

Micro-lote limitado a `web/ach-interbank-ui/src/app/features/nacha-config-admin/pages/nacha-config-variants-fields-page.component.scss`.

Estado: `4C3C0O_NACHA_CONFIG_BORDER_TOKEN_IMPLEMENTED_VALIDATION_PENDING`

No se declara VERIFIED porque no fue posible ejecutar build/tests Angular.

## Baseline

- ZIP: `ACHInterbank-v88-spa-4C3C0n-nacha-config-surface-muted-token.zip`
- SHA-256 verificado: `dc4f153f342ea7f9e4428523be3b49ca4c8b721dbf69d145069c4f74d23a5dcd`

## Precondición de token

En `web/ach-interbank-ui/src/styles/_tokens.scss`, línea 19:

```scss
--color-border: #dbe3ee;
```

El archivo `_tokens.scss` quedó intacto; SHA-256 baseline/output: `a49161a1b8984ba70b4df49fc4a1761ce457ca28e28e304ede1f2a273e291d15`.

## Cambio implementado

Se reemplazaron exclusivamente las 3 ocurrencias exactas de:

```scss
border: 1px solid #dbe3ee;
```

por:

```scss
border: 1px solid var(--color-border);
```

Conteos:

- baseline `border: 1px solid #dbe3ee;`: 3
- output `border: 1px solid #dbe3ee;`: 0
- output `border: 1px solid var(--color-border);`: 3
- output `#dbe3ee`: 0

La transformación inversa de las 3 sustituciones reproduce byte-a-byte el SCSS del baseline (`reverse_exact = True`).

SHA-256 del SCSS:

- baseline: `99307a41607d479feb8e6cd9463d4b363569b6c31e726cffe7115b0108685c89`
- output: `d57f13a991cea7ac6d9feddb52d02a82cfe7ffa2b20a7f26e7a7cb18a2407afc`

## Validación arquitectónica

Scan estático de imports TypeScript bajo `web/ach-interbank-ui/src/app`:

- feature -> feature: 0
- core -> features: 0
- shared -> features: 0

Backend: `diff -qr <baseline>/src <output>/src` => 0 diferencias.

Rutas: manifiesto SHA-256 de 29 archivos `*routing*.ts` / `app.routes.ts` idéntico entre baseline y output; 0 archivos de routing modificados.

Antes de agregar este checkpoint, `diff -qr` entre baseline y output reportó únicamente el SCSS objetivo.

## Validación Angular

Se comprobó únicamente:

`web/ach-interbank-ui/node_modules/.bin/ng`

Resultado:

```text
ng: not found
```

No se ejecutó `npm ping`, `npm ci`, registry retries, build ni tests.

## Comandos/evidencia resumida

```text
sha256sum ACHInterbank-v88-spa-4C3C0n-nacha-config-surface-muted-token.zip
# dc4f153f342ea7f9e4428523be3b49ca4c8b721dbf69d145069c4f74d23a5dcd

grep -n -- '--color-border: #dbe3ee;' web/ach-interbank-ui/src/styles/_tokens.scss
# 19:  --color-border: #dbe3ee;

# conteo exacto sobre bytes del SCSS
baseline_old_count = 3
output_old_count = 0
output_new_count = 3
output_hex_count = 0
reverse_exact = True

diff -qr <baseline>/src <output>/src
# sin salida

# comparación de manifiestos SHA-256 de routing
routes_intact = 29

web/ach-interbank-ui/node_modules/.bin/ng
ng: not found
```

## Diff permitido

El paquete de salida solo contiene:

1. modificación del SCSS objetivo con las 3 sustituciones indicadas;
2. este documento de checkpoint.

No se avanzó al siguiente lote.
