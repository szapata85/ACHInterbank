# SPA 4C.3C0j — NACHA Config Variants/Fields Error Token

## Estado

`4C3C0J_NACHA_CONFIG_ERROR_TOKEN_IMPLEMENTED_VALIDATION_PENDING`

No se declara `VERIFIED`: Angular CLI no está disponible en el baseline y, por instrucción del lote, no se ejecutaron instalación, acceso a registry, build ni tests.

## Baseline

- ZIP: `ACHInterbank-v83-spa-4C3C0i-nacha-config-primary-token.zip`
- SHA-256 esperado/obtenido: `d0c2d02b0b4bbe9942c9b95751a4d35742d7b8ddf26d4522930b0750055a3104`

## Cambio único

Archivo:

`web/ach-interbank-ui/src/app/features/nacha-config-admin/pages/nacha-config-variants-fields-page.component.scss`

Precondición confirmada en `web/ach-interbank-ui/src/styles/_tokens.scss`:

```scss
--color-error: #dc2626;
```

El baseline contenía exactamente una declaración:

```scss
border-left-color: #dc2626;
```

Se reemplazó exclusivamente por:

```scss
border-left-color: var(--color-error);
```

La transformación binaria del SCSS de salida coincide exactamente con el baseline aplicando esa única sustitución de bytes y ninguna otra.

Se preservaron explícitamente `#b91c1c`, `#fee2e2` y `#fff7f7`.

## Evidencia y validaciones

```text
baseline exact declaration count: 1
post #dc2626 count in target SCSS: 0
post var(--color-error) declaration delta: +1
exact_transform=yes
#b91c1c: 1 -> 1
#fee2e2: 1 -> 1
#fff7f7: 1 -> 1
feature->feature=0
core->features=0
shared->features=0
backend files=1474
backend diff=0
routing_files=29
routing_modified=0
protected_config_files=6
protected_config_modified=0
ng: not found
```

Antes de agregar este checkpoint, el diff funcional contra el baseline contiene exactamente un archivo modificado: el SCSS objetivo.

Las rutas, guards/permisos, services, core/shared, backend, contratos/endpoints y configuración no fueron modificados. Los 29 archivos de routing detectados son byte-a-byte idénticos al baseline.

## Restricciones de ejecución respetadas

Se comprobó únicamente:

`web/ach-interbank-ui/node_modules/.bin/ng`

Resultado exacto:

`ng: not found`

No se ejecutaron `npm ping`, `npm ci`, registry retries, build ni tests.
