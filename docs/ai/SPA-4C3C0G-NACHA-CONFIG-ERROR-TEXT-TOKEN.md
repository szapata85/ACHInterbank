# SPA 4C.3C0g — NACHA Config error-text token checkpoint

Estado: `4C3C0G_NACHA_CONFIG_ERROR_TEXT_TOKEN_IMPLEMENTED_VALIDATION_PENDING`

## Baseline

- ZIP: `ACHInterbank-v80-spa-4C3C0f-nacha-config-text-muted-token.zip`
- SHA-256 esperado/verificado: `6e3b1b163ae55efdb79b71782320c7cf00e54f987840fff3bb4d60e4101f0758`

## Cambio aplicado

Archivo único de código modificado:

`web/ach-interbank-ui/src/app/features/nacha-config-admin/pages/nacha-config-variants-fields-page.component.scss`

Antes del cambio se confirmó en `web/ach-interbank-ui/src/styles/_tokens.scss`:

```scss
--color-error-text: #991b1b;
```

Se reemplazaron exactamente 2 ocurrencias de:

```scss
color: #991b1b;
```

por:

```scss
color: var(--color-error-text);
```

Las ocurrencias estaban en las líneas baseline 547 y 604.

## Evidencia estática

```text
baseline color: #991b1b; = 2
output   color: #991b1b; = 0
baseline color: var(--color-error-text); = 0
output   color: var(--color-error-text); = 2
```

La comparación binaria del SCSS de salida contra una transformación del baseline que realiza únicamente esas dos sustituciones devuelve `true`; por tanto, el resto del archivo permanece byte-a-byte sin cambios.

Los otros rojos indicados permanecieron intactos:

```text
#b91c1c baseline/output = 1 / 1
#dc2626 baseline/output = 1 / 1
#fee2e2 baseline/output = 1 / 1
```

Hashes del SCSS objetivo:

```text
baseline: 628c3db28dc11c16f0d9aa97ff2a5fd255e70a97dd9856675ad4fc2513c8a41f
output:   e66629ea9d971be3c9539f7cfd888568bda3e1d0b37e06a80de3e3f71c16903f
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
database diff:    0
tests diff:       0
```

Antes de agregar este documento, el diff completo contra el baseline contenía únicamente el SCSS objetivo. Con el checkpoint agregado, el diff total es exactamente el SCSS objetivo más este reporte.

## Angular

Se comprobó únicamente:

`web/ach-interbank-ui/node_modules/.bin/ng`

Resultado exacto:

```text
ng: not found
```

No se ejecutaron `npm ping`, `npm ci`, registry retries, build ni tests. No se declara `VERIFIED`.
