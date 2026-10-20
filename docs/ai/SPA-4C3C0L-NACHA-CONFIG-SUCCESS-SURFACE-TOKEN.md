# SPA 4C.3C0l — NACHA Config Variants/Fields Success Surface Token

## Estado

`4C3C0L_NACHA_CONFIG_SUCCESS_SURFACE_TOKEN_IMPLEMENTED_VALIDATION_PENDING`

No se declara `VERIFIED`: Angular CLI no está disponible en el baseline y, por instrucción del lote, no se ejecutaron instalación, acceso a registry, build ni tests.

## Baseline

- ZIP: `ACHInterbank-v85-spa-4C3C0k-nacha-config-warning-token.zip`
- SHA-256 esperado/obtenido: `e95aa2ef0165b617a8d5ee7d9ed574f233ec0f647e346610ad2349027ffe48bf`

## Cambio único

Archivo:

`web/ach-interbank-ui/src/app/features/nacha-config-admin/pages/nacha-config-variants-fields-page.component.scss`

Precondición confirmada en `web/ach-interbank-ui/src/styles/_tokens.scss`:

```scss
--color-success-surface: #dcfce7;
```

El baseline contenía exactamente una declaración:

```scss
background: #dcfce7;
```

Se reemplazó exclusivamente por:

```scss
background: var(--color-success-surface);
```

La transformación binaria del SCSS de salida coincide exactamente con el baseline aplicando esa única sustitución de bytes y ninguna otra. `#166534` permanece intacto.

## Evidencia y validaciones

```text
baseline exact declaration count: 1
post #dcfce7 count in target SCSS: 0
post var(--color-success-surface) declaration delta: +1
#166534: 1 -> 1, intacto
exact_transform=yes
feature->feature=0
core->features=0
shared->features=0
backend files=1474
backend diff=0
routing_files=29
routing_modified=0
ng: not found
```

Antes de agregar este checkpoint, el diff funcional contra el baseline contenía exactamente un archivo modificado: el SCSS objetivo.

Las rutas, guards/permisos, services, core/shared, backend, contratos/endpoints y configuración no fueron modificados. Los 29 archivos de routing detectados son byte-a-byte idénticos al baseline.

## Comandos/evidencia principal

```text
sha256sum ACHInterbank-v85-spa-4C3C0k-nacha-config-warning-token.zip
grep -n --fixed-strings -- '--color-success-surface: #dcfce7;' web/ach-interbank-ui/src/styles/_tokens.scss
grep -n --fixed-strings 'background: #dcfce7;' web/ach-interbank-ui/src/app/features/nacha-config-admin/pages/nacha-config-variants-fields-page.component.scss
# comparación binaria baseline/output y scan estático de imports TypeScript
# comparación byte-a-byte de src/ backend y de los archivos *routing*.ts
```

## Restricciones de ejecución respetadas

Se comprobó únicamente:

`web/ach-interbank-ui/node_modules/.bin/ng`

Resultado exacto:

`ng: not found`

No se ejecutaron `npm ping`, `npm ci`, registry retries, build ni tests.
