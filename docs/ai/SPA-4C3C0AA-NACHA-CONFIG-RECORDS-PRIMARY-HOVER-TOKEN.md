# SPA 4C.3C0aa — NACHA Config Records Primary-Hover Token

## Estado

`4C3C0AA_NACHA_CONFIG_RECORDS_PRIMARY_HOVER_TOKEN_IMPLEMENTED_VALIDATION_PENDING`

No se declara `VERIFIED`: Angular CLI no está disponible en el baseline y, conforme al alcance, no se ejecutaron instalación, acceso a registry, build ni tests.

## Baseline

- ZIP: `ACHInterbank-v100-spa-4C3C0z-nacha-config-records-success-text-token.zip`
- SHA-256 esperado: `872c5413af9419727613fbe2de1520091106bf3e2102281497ec9d2713fd8c59`
- SHA-256 observado: `872c5413af9419727613fbe2de1520091106bf3e2102281497ec9d2713fd8c59`

Comando:

```text
sha256sum ACHInterbank-v100-spa-4C3C0z-nacha-config-records-success-text-token.zip
```

## Precondición

En `web/ach-interbank-ui/src/styles/_tokens.scss` se confirmó:

```scss
--brand-primary-hover: #1d4ed8;
--color-primary-hover: var(--brand-primary-hover);
```

En el SCSS objetivo existía exactamente una declaración directa:

```scss
color: #1d4ed8;
```

## Cambio implementado

Archivo único de source modificado:

`web/ach-interbank-ui/src/app/features/nacha-config-admin/pages/nacha-config-records-page.component.scss`

Cambio exacto:

```diff
-color: #1d4ed8;
+color: var(--color-primary-hover);
```

Conteos:

- `color: #1d4ed8;` antes: **1**
- `color: #1d4ed8;` después: **0**
- `color: var(--color-primary-hover);` después: **1**
- `#eff6ff`: **1 → 1, intacto**

La comparación binaria contra la transformación mecánica exacta del baseline confirma que el resto del archivo permanece byte-a-byte idéntico; se preservaron los **147 terminadores CRLF**.

Los fallbacks existentes permanecen intactos, incluyendo `var(--color-surface, #fff)`, `var(--color-border, #d9e2ec)`, `var(--color-border, #cbd5e1)`, `var(--color-border, #e2e8f0)` y `var(--color-text-soft, #64748b)`.

## Validación estática

Antes de agregar este reporte, la comparación completa baseline vs árbol de salida mostró exactamente **1 archivo con diferencia**, el SCSS objetivo.

```text
UI source diff: exactamente 1 archivo modificado, el SCSS objetivo
backend diff: 0
routing diff: 0 / 29 archivos
```

Escaneo de imports TypeScript bajo `web/ach-interbank-ui/src/app`:

```text
feature -> feature: 0
core -> features: 0
shared -> features: 0
```

Los **29 archivos de routing** mantienen hash/contenido idéntico al baseline; por tanto permanecen intactas las rutas públicas existentes de NACHA Config Admin (`/nacha-config-admin/perfiles`, `/nacha-config-admin/records`, `/nacha-config-admin/variants-fields`, `/nacha-config-admin/perfiles/:id`) y el redirect histórico `/ach/nacha/config-profiles`.

Comandos/evidencia representativos:

```text
grep -n -- '--brand-primary-hover\|--color-primary-hover' web/ach-interbank-ui/src/styles/_tokens.scss
grep -nF 'color: #1d4ed8;' web/ach-interbank-ui/src/app/features/nacha-config-admin/pages/nacha-config-records-page.component.scss
grep -nF '#eff6ff' web/ach-interbank-ui/src/app/features/nacha-config-admin/pages/nacha-config-records-page.component.scss
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
