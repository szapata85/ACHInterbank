# SPA 4C.3C0n — NACHA Config variants/fields surface-muted token checkpoint

## Estado

`4C3C0N_NACHA_CONFIG_SURFACE_MUTED_TOKEN_IMPLEMENTED_VALIDATION_PENDING`

No se declara `VERIFIED`: Angular CLI no está disponible en el baseline y, por instrucción del micro-lote, no se instalaron dependencias ni se ejecutaron build/tests.

## Baseline

- ZIP: `ACHInterbank-v87-spa-4C3C0m-nacha-config-success-text-token.zip`
- SHA-256 esperado/verificado: `d17ffe0a9e7452b5bb7739dc1589c1da033e0eb1137f702fd6d74b15b343f9eb`

## Cambio implementado

Archivo único de código modificado:

`web/ach-interbank-ui/src/app/features/nacha-config-admin/pages/nacha-config-variants-fields-page.component.scss`

Precondición verificada en `web/ach-interbank-ui/src/styles/_tokens.scss`:

```scss
--color-surface-muted: #f8fafc;
```

Se reemplazaron exactamente seis declaraciones:

```scss
background: #f8fafc;
```

por:

```scss
background: var(--color-surface-muted);
```

Ocurrencias baseline: líneas 156, 205, 243, 372, 448 y 557.

## Evidencia estática

- `background: #f8fafc;` antes: **6**
- `background: #f8fafc;` después: **0**
- nuevas declaraciones `background: var(--color-surface-muted);`: **6**
- reversión mecánica de esas seis sustituciones sobre el archivo de salida reproduce byte-a-byte el SCSS baseline: **sí**
- diff funcional previo al documento de checkpoint: **1 archivo modificado**, exclusivamente el SCSS objetivo
- feature -> feature TypeScript imports: **0**
- core -> features TypeScript imports: **0**
- shared -> features TypeScript imports: **0**
- backend `src/` diff: **0**
- routing: **29 archivos comparados, 0 modificados**
- package/config protegido: **6 archivos comparados, 0 modificados**

Rutas NACHA Config Admin verificadas como presentes e intactas por comparación byte-a-byte de routing:

- `/nacha-config-admin/perfiles`
- `/nacha-config-admin/records`
- `/nacha-config-admin/variants-fields`
- `/nacha-config-admin/perfiles/:id`
- redirect histórico hacia `/nacha-config-admin/perfiles`

## Angular

Se comprobó únicamente:

`web/ach-interbank-ui/node_modules/.bin/ng`

Resultado exacto:

```text
ng: not found
```

No se ejecutó `npm ping`, `npm ci`, registry retries, build ni tests.
