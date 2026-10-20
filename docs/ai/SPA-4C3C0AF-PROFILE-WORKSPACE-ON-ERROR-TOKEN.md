# SPA 4C.3C0af — Profile Workspace On-Error Token

## Estado

`4C3C0AF_NACHA_CONFIG_PROFILE_WORKSPACE_ON_ERROR_TOKEN_IMPLEMENTED_VALIDATION_PENDING`

## Baseline

- ZIP: `ACHInterbank-v105-spa-4C3C0ae-profile-workspace-on-secondary-token.zip`
- SHA-256 esperado/verificado: `c761f3e8cb2af3288e5bcbc72cb977366eb867776d0aad7626f4049406eeb86b`

## Cambio implementado

Archivo único de source modificado:

`web/ach-interbank-ui/src/app/features/nacha-config-admin/pages/nacha-config-profile-workspace-page.component.scss`

Precondición confirmada en `web/ach-interbank-ui/src/styles/_tokens.scss`:

```scss
--color-on-error: #ffffff;
```

Cambio único dentro de `.btn-danger`:

```diff
 .btn-danger {
   background: var(--color-danger);
-  color: #fff;
+  color: var(--color-on-error);
   border-color: var(--color-danger);
 }
```

`.btn-primary` y `.btn-secondary` permanecen intactos.

## Validación estática

- Reemplazos exactos: `1`.
- `color: #fff;`: `1 -> 0`.
- Hex directos posteriores en el SCSS objetivo: `0`.
- El SCSS resultante coincide byte-a-byte con una transformación mecánica del baseline que sustituye únicamente la línea objetivo.
- CRLF preservados: `105 -> 105`.
- TypeScript feature -> feature: `0`.
- TypeScript core -> features: `0`.
- TypeScript shared -> features: `0`.
- Backend `src/`: `1474` archivos; diff `0`.
- Routing: `29` archivos; diff `0`.
- Rutas públicas NACHA Config Admin preservadas: `/nacha-config-admin/perfiles`, `/nacha-config-admin/records`, `/nacha-config-admin/variants-fields`, `/nacha-config-admin/perfiles/:id`.
- Redirect histórico preservado: `/ach/nacha/config-profiles` -> `/nacha-config-admin/perfiles`.

Evidencia representativa:

```text
sha256sum ACHInterbank-v105-spa-4C3C0ae-profile-workspace-on-secondary-token.zip
c761f3e8cb2af3288e5bcbc72cb977366eb867776d0aad7626f4049406eeb86b

grep -n -- '--color-on-error: #ffffff;' web/ach-interbank-ui/src/styles/_tokens.scss
42:  --color-on-error: #ffffff;

feature->feature 0
core->features 0
shared->features 0
backend_diff 0
routing_diff 0
```

## Angular

Se comprobó únicamente:

`web/ach-interbank-ui/node_modules/.bin/ng`

Resultado exacto:

```text
ng: not found
```

No se ejecutó `npm ping`, `npm ci`, registry retries, build ni tests. No se declara `VERIFIED`.
