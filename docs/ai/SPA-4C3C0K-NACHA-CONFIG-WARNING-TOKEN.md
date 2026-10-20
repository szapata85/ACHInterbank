# SPA 4C.3C0k — NACHA Config Variants/Fields Warning Token

## Estado

`4C3C0K_NACHA_CONFIG_WARNING_TOKEN_IMPLEMENTED_VALIDATION_PENDING`

No se declara `VERIFIED`: Angular CLI no está disponible en el baseline y, por instrucción del lote, no se ejecutaron instalación, acceso a registry, build ni tests.

## Baseline

- ZIP: `ACHInterbank-v84-spa-4C3C0j-nacha-config-error-token.zip`
- SHA-256 esperado/obtenido: `019a43c0a99fb7e71eae5122fe833697baaca5397dbdf2293346eec6d6ea535f`

## Cambio único

Archivo:

`web/ach-interbank-ui/src/app/features/nacha-config-admin/pages/nacha-config-variants-fields-page.component.scss`

Precondición confirmada en `web/ach-interbank-ui/src/styles/_tokens.scss`:

```scss
--color-warning: #d97706;
```

El baseline contenía exactamente una declaración:

```scss
border-left-color: #d97706;
```

Se reemplazó exclusivamente por:

```scss
border-left-color: var(--color-warning);
```

La transformación binaria del SCSS de salida coincide exactamente con el baseline aplicando esa única sustitución de bytes y ninguna otra.

## Evidencia y validaciones

```text
baseline exact declaration count: 1
post #d97706 count in target SCSS: 0
post var(--color-warning) declaration delta: +1
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

Antes de agregar este checkpoint, el diff funcional contra el baseline contiene exactamente un archivo modificado: el SCSS objetivo.

Las rutas, guards/permisos, services, core/shared, backend, contratos/endpoints y configuración no fueron modificados. Los 29 archivos de routing detectados son byte-a-byte idénticos al baseline.

## Comandos/evidencia principal

```text
sha256sum ACHInterbank-v84-spa-4C3C0j-nacha-config-error-token.zip
grep -n --fixed-strings -- '--color-warning: #d97706;' web/ach-interbank-ui/src/styles/_tokens.scss
grep -n --fixed-strings 'border-left-color: #d97706;' web/ach-interbank-ui/src/app/features/nacha-config-admin/pages/nacha-config-variants-fields-page.component.scss
# comparación binaria baseline/output y scan estático de imports TypeScript
# comparación byte-a-byte de src/ backend y de los archivos *routing*.ts
```

## Restricciones de ejecución respetadas

Se comprobó únicamente:

`web/ach-interbank-ui/node_modules/.bin/ng`

Resultado exacto:

`ng: not found`

No se ejecutaron `npm ping`, `npm ci`, registry retries, build ni tests.
