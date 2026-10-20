# SPA 4C.3C0z — NACHA Config Records Success-Text Token

## Estado

`4C3C0Z_NACHA_CONFIG_RECORDS_SUCCESS_TEXT_TOKEN_IMPLEMENTED_VALIDATION_PENDING`

No se declara `VERIFIED`: Angular CLI no está disponible en el baseline y, conforme al alcance, no se ejecutaron instalación, acceso a registry, build ni tests.

## Baseline

- ZIP: `ACHInterbank-v99-spa-4C3C0y-nacha-config-records-error-text-token.zip`
- SHA-256 esperado: `e378de091874fe5fd91e3c0342aa093cc89f0c0037acc3c5d26b987d97ea115a`
- SHA-256 observado: `e378de091874fe5fd91e3c0342aa093cc89f0c0037acc3c5d26b987d97ea115a`

Comando:

```text
sha256sum ACHInterbank-v99-spa-4C3C0y-nacha-config-records-error-text-token.zip
```

## Precondición

En `web/ach-interbank-ui/src/styles/_tokens.scss` se confirmó:

```scss
--color-success-text: #166534;
```

En el SCSS objetivo existía exactamente una declaración directa:

```scss
color: #166534;
```

## Cambio implementado

Archivo único de source modificado:

`web/ach-interbank-ui/src/app/features/nacha-config-admin/pages/nacha-config-records-page.component.scss`

Cambio exacto:

```diff
-color: #166534;
+color: var(--color-success-text);
```

Conteos:

- `color: #166534;` antes: **1**
- `color: #166534;` después: **0**
- `color: var(--color-success-text);` después: **1**

La comparación binaria contra la transformación mecánica exacta del baseline confirma que el resto del archivo permanece byte-a-byte idéntico; también se preservaron los **147 terminadores CRLF**.

Los valores expresamente protegidos permanecen idénticos:

- `#ecfdf5`: 1 → 1
- `#eff6ff`: 1 → 1
- `#1d4ed8`: 1 → 1

Los fallbacks existentes permanecen intactos, incluyendo `var(--color-surface, #fff)`, `var(--color-border, #d9e2ec)`, `var(--color-border, #cbd5e1)`, `var(--color-border, #e2e8f0)` y `var(--color-text-soft, #64748b)`.

## Validación estática

Comparación baseline vs árbol de salida antes de agregar este reporte:

```text
UI source diff: exactamente 1 archivo modificado, el SCSS objetivo
backend diff: 0
routing diff: 0
```

Escaneo de imports TypeScript bajo `web/ach-interbank-ui/src/app`:

```text
feature -> feature: 0
core -> features: 0
shared -> features: 0
```

Los **29 archivos de routing** mantienen hash idéntico al baseline; por tanto las rutas públicas existentes permanecen intactas.

Comandos/evidencia representativos:

```text
grep -n -- '--color-success-text' web/ach-interbank-ui/src/styles/_tokens.scss
grep -nF 'color: #166534;' web/ach-interbank-ui/src/app/features/nacha-config-admin/pages/nacha-config-records-page.component.scss
diff -qr <baseline>/src <output>/src
diff -qr <baseline>/web/ach-interbank-ui/src <output>/web/ach-interbank-ui/src
```

## Angular

Se comprobó únicamente:

`web/ach-interbank-ui/node_modules/.bin/ng`

Resultado exacto:

```text
ng: not found
```

No se ejecutó `npm ping`, `npm ci`, retries de registry, build ni tests.

## Alcance final

El ZIP de salida contiene únicamente:

1. la sustitución mecánica de una línea en `nacha-config-records-page.component.scss`;
2. este documento de checkpoint.

No se modificaron HTML, TypeScript, specs, otros SCSS, rutas, services, core/shared, backend, contratos/endpoints ni archivos de configuración. No se avanzó al siguiente lote.
