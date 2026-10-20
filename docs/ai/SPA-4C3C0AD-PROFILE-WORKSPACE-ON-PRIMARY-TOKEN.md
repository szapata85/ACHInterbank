# SPA 4C.3C0ad — Profile Workspace On-Primary Token

## Estado

`4C3C0AD_NACHA_CONFIG_PROFILE_WORKSPACE_ON_PRIMARY_TOKEN_IMPLEMENTED_VALIDATION_PENDING`

No se declara `VERIFIED`: Angular CLI no está disponible en el baseline y, por instrucción del lote, no se ejecutaron instalación, acceso a registry, build ni tests.

## Baseline

- ZIP: `ACHInterbank-v103-spa-4C3C0ac-profile-workspace-surface-token.zip`
- SHA-256 esperado: `38c59d4b0ba7aefd62d395d7fc2786166bf318e04975162613d9f922ea726117`
- SHA-256 observado: `38c59d4b0ba7aefd62d395d7fc2786166bf318e04975162613d9f922ea726117`

## Precondición de tokens

Archivo: `web/ach-interbank-ui/src/styles/_tokens.scss`

```scss
--brand-on-primary: #ffffff;
--color-on-primary: var(--brand-on-primary);
```

## Cambio implementado

Archivo único de source modificado:

`web/ach-interbank-ui/src/app/features/nacha-config-admin/pages/nacha-config-profile-workspace-page.component.scss`

Dentro de `.btn-primary`:

```diff
 .btn-primary {
   background: var(--color-primary);
-  color: #fff;
+  color: var(--color-on-primary);
   border-color: var(--color-primary);
 }
```

No se modificaron `.btn-secondary` ni `.btn-danger`.

## Validación estática

- Reemplazos exactos: **1**.
- `color: #fff;`: **3 → 2**.
- `.btn-secondary` conserva su bloque byte-a-byte.
- `.btn-danger` conserva su bloque byte-a-byte.
- El SCSS resultante coincide byte-a-byte con el baseline aplicando únicamente la sustitución solicitada.
- Terminadores CRLF preservados: **105 → 105**.
- TypeScript `feature -> feature`: **0**.
- TypeScript `core -> features`: **0**.
- TypeScript `shared -> features`: **0**.
- Backend `src/`: **1474 archivos; 0 agregados, 0 eliminados, 0 modificados**.
- Routing: **29 archivos revisados; 0 modificados**.
- Rutas públicas: intactas por identidad de los archivos de routing.

Antes de agregar este reporte, el diff completo baseline → output fue exactamente un archivo modificado: el SCSS objetivo.

Con este reporte, el diff total contiene únicamente:

1. modificación del SCSS objetivo;
2. agregado de este checkpoint.

## Angular

Se comprobó únicamente:

`web/ach-interbank-ui/node_modules/.bin/ng`

Resultado exacto:

```text
ng: not found
```

No se ejecutó `npm ping`, `npm ci`, registry retries, build ni tests.

## Comandos/evidencia principal

```text
sha256sum ACHInterbank-v103-spa-4C3C0ac-profile-workspace-surface-token.zip
grep -n -- '--brand-on-primary\|--color-on-primary' web/ach-interbank-ui/src/styles/_tokens.scss
grep -n -A8 -B2 '^\.btn-primary' web/ach-interbank-ui/src/app/features/nacha-config-admin/pages/nacha-config-profile-workspace-page.component.scss
grep -n 'color: #fff;' web/ach-interbank-ui/src/app/features/nacha-config-admin/pages/nacha-config-profile-workspace-page.component.scss
```
