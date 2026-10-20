# SPA 4C.3C0h — NACHA Config Variants/Fields Text Token

## Estado

`4C3C0H_NACHA_CONFIG_TEXT_TOKEN_IMPLEMENTED_VALIDATION_PENDING`

No se declara `VERIFIED`: Angular CLI no está disponible en el baseline y, por instrucción del lote, no se ejecutaron instalación, acceso a registry, build ni tests.

## Baseline

- ZIP: `ACHInterbank-v81-spa-4C3C0g-nacha-config-error-text-token.zip`
- SHA-256 esperado/obtenido: `0da6ae2d25953effe527dcb38d30549db61a7f1eec70bd888d8058ccaa2a1474`

## Cambio único

Archivo:

`web/ach-interbank-ui/src/app/features/nacha-config-admin/pages/nacha-config-variants-fields-page.component.scss`

Precondición confirmada en `web/ach-interbank-ui/src/styles/_tokens.scss`:

```scss
--color-text: #0f172a;
```

Declaración objetivo encontrada una sola vez en el baseline:

```scss
color: #0f172a;
```

Sustitución aplicada:

```scss
color: var(--color-text);
```

La transformación binaria del SCSS de salida coincide exactamente con el baseline aplicando una única sustitución de esos bytes; no existen otras diferencias en ese archivo.

## Evidencia y validaciones

```text
baseline target count: 1
post target count: 0
post token declaration count: 1
exact_transform=yes
feature->feature=0
core->features=0
shared->features=0
backend diff=0
routing_files=29
routing_modified=0
ng: not found
```

Diff funcional antes de agregar este checkpoint:

```diff
 .technical-details dd {
   margin: 0;
-  color: #0f172a;
+  color: var(--color-text);
   font-family: ui-monospace, SFMono-Regular, Consolas, "Liberation Mono", monospace;
   overflow-wrap: anywhere;
 }
```

Las rutas y guards/permisos no fueron modificados. Los 29 archivos de routing detectados son byte-a-byte idénticos al baseline.

## Restricciones de ejecución respetadas

Se comprobó únicamente:

`web/ach-interbank-ui/node_modules/.bin/ng`

Resultado exacto:

`ng: not found`

No se ejecutaron `npm ping`, `npm ci`, registry retries, build ni tests.
