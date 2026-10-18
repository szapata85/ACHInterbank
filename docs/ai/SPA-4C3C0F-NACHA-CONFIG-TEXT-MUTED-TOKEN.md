# SPA 4C.3C0f — NACHA Config text-muted token checkpoint

Estado: `4C3C0F_NACHA_CONFIG_TEXT_MUTED_TOKEN_IMPLEMENTED_VALIDATION_PENDING`

## Baseline

- ZIP: `ACHInterbank-v79-spa-4C3C0e-nacha-config-text-soft-token.zip`
- SHA-256 esperado/verificado: `e805bf42ab933f45bcfb336604115fda4368952eafe7c6b28cbd80fd8744d930`

## Cambio aplicado

Archivo único de código modificado:

`web/ach-interbank-ui/src/app/features/nacha-config-admin/pages/nacha-config-variants-fields-page.component.scss`

Antes del cambio se confirmó en `web/ach-interbank-ui/src/styles/_tokens.scss`:

```scss
--color-text-muted: #475569;
```

Se reemplazaron exactamente 5 ocurrencias de:

```scss
color: #475569;
```

por:

```scss
color: var(--color-text-muted);
```

Las ocurrencias estaban en las líneas baseline 217, 342, 439, 476 y 536.

## Evidencia estática

```text
baseline color: #475569; = 5
output   color: #475569; = 0
baseline color: var(--color-text-muted); = 0
output   color: var(--color-text-muted); = 5
baseline var(--color-text-muted, #334155) = 1
output   var(--color-text-muted, #334155) = 1
```

La comparación binaria del SCSS de salida contra una transformación del baseline que hace únicamente esas cinco sustituciones devuelve `true`; por tanto, el resto del archivo permanece byte-a-byte sin cambios.

Hashes del SCSS objetivo:

```text
baseline: ebfdcffd6b88e79538e8f850661cd65ba87a29acde0f5e9e460be92a84f444d4
output:   628c3db28dc11c16f0d9aa97ff2a5fd255e70a97dd9856675ad4fc2513c8a41f
```

`_tokens.scss` se mantuvo intacto:

```text
SHA-256 baseline/output: a49161a1b8984ba70b4df49fc4a1761ce457ca28e28e304ede1f2a273e291d15
```

## Dependencias

Escaneo de imports TypeScript bajo `web/ach-interbank-ui/src/app`:

```text
feature -> feature: 0
core -> features:    0
shared -> features:  0
```

## Rutas y backend

Los siguientes archivos de routing son byte-a-byte idénticos al baseline:

```text
app-routing.module.ts                                            8aebb136a0f5d1ae9f22b4af1467a3191766aa6836f3314d08e2b9c94150e11c
nacha-config-admin/nacha-config-admin-routing.module.ts          c42c257234b1e236c67123b09ffd676c6a0614da49f5c2090df6ee60c42cd254
nacha-operational/nacha-operational-routing.module.ts            49a57ebe9754d9767bde1b68fe429212c0e30c6a4478c027356b1851b30ff0bb
```

Permanecen las rutas públicas de NACHA Config Admin (`/nacha-config-admin/perfiles`, `/records`, `/variants-fields`, `/perfiles/:id`) y el redirect histórico `/ach/nacha/config-profiles` hacia `/nacha-config-admin/perfiles`.

Validaciones adicionales:

```text
backend src diff: 0
package/config files changed: 0
```

Antes de agregar este documento, el diff completo contra el baseline contenía únicamente el SCSS objetivo.

## Angular

Se comprobó únicamente:

`web/ach-interbank-ui/node_modules/.bin/ng`

Resultado exacto:

```text
ng: not found
```

No se ejecutaron `npm ping`, `npm ci`, registry retries, build ni tests. No se declara `VERIFIED`.
