# SPA 4C.3C0i — NACHA Config Variants/Fields Primary Token

## Estado

`4C3C0I_NACHA_CONFIG_PRIMARY_TOKEN_IMPLEMENTED_VALIDATION_PENDING`

No se declara `VERIFIED`: Angular CLI no está disponible en el baseline y, por instrucción del lote, no se ejecutaron instalación, acceso a registry, build ni tests.

## Baseline

- ZIP: `ACHInterbank-v82-spa-4C3C0h-nacha-config-text-token.zip`
- SHA-256 esperado/obtenido: `4cc1ee1397da0e416fc7027f41309f433d9af3bb910ce70efecff1c9d56d8c0f`

## Cambio único

Archivo:

`web/ach-interbank-ui/src/app/features/nacha-config-admin/pages/nacha-config-variants-fields-page.component.scss`

Precondiciones confirmadas en `web/ach-interbank-ui/src/styles/_tokens.scss`:

```scss
--brand-primary: #2563eb;
--color-primary: var(--brand-primary);
```

El baseline contenía exactamente 7 ocurrencias de:

```scss
#2563eb
```

Se reemplazaron exclusivamente por:

```scss
var(--color-primary)
```

La transformación binaria del SCSS de salida coincide exactamente con el baseline aplicando esas 7 sustituciones de bytes y ninguna otra.

Se preservaron explícitamente:

```scss
rgba(37, 99, 235, .28)
#1d4ed8
```

## Evidencia y validaciones

```text
baseline #2563eb count: 7
post #2563eb count: 0
post var(--color-primary) additions: 7
exact_transform=yes
rgba(37, 99, 235, .28): 1 -> 1
#1d4ed8: 3 -> 3
feature->feature=0
core->features=0
shared->features=0
backend diff=0
routing_files=29
routing_modified=0
ng: not found
```

El diff funcional antes de agregar este checkpoint contiene únicamente el SCSS objetivo.

Las rutas y guards/permisos no fueron modificados. Los 29 archivos de routing detectados son byte-a-byte idénticos al baseline.

## Restricciones de ejecución respetadas

Se comprobó únicamente:

`web/ach-interbank-ui/node_modules/.bin/ng`

Resultado exacto:

`ng: not found`

No se ejecutaron `npm ping`, `npm ci`, registry retries, build ni tests.
