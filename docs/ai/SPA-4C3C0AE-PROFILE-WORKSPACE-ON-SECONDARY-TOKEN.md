# SPA 4C.3C0ae — Profile Workspace On-Secondary Token

## Estado

`4C3C0AE_NACHA_CONFIG_PROFILE_WORKSPACE_ON_SECONDARY_TOKEN_IMPLEMENTED_VALIDATION_PENDING`

## Baseline

- ZIP: `ACHInterbank-v104-spa-4C3C0ad-profile-workspace-on-primary-token.zip`
- SHA-256 esperado/verificado: `3ee0dc5e43ac29453718207fbb8b1a662922880bb135f9403958ab195747a119`

## Cambio implementado

Archivo único de source modificado:

`web/ach-interbank-ui/src/app/features/nacha-config-admin/pages/nacha-config-profile-workspace-page.component.scss`

Precondición confirmada en `web/ach-interbank-ui/src/styles/_tokens.scss`:

```scss
--color-on-secondary: #ffffff;
```

Cambio único dentro de `.btn-secondary`:

```diff
 .btn-secondary {
   background: var(--color-secondary);
-  color: #fff;
+  color: var(--color-on-secondary);
   border-color: var(--color-secondary);
 }
```

`.btn-primary` y `.btn-danger` no fueron modificados.

## Validación estática

- Reemplazos exactos: `1`.
- `color: #fff;`: `2 -> 1`.
- `.btn-danger` conserva `color: #fff;`.
- El SCSS resultante coincide byte-a-byte con una transformación mecánica del baseline que sustituye únicamente la línea objetivo.
- CRLF preservados: `105 -> 105`.
- TypeScript feature -> feature: `0`.
- TypeScript core -> features: `0`.
- TypeScript shared -> features: `0`.
- Backend `src/`: `1474` archivos; diff `0`.
- Routing: `29` archivos; diff `0`.
- Rutas públicas NACHA Config Admin preservadas: `/nacha-config-admin/perfiles`, `/nacha-config-admin/records`, `/nacha-config-admin/variants-fields`, `/nacha-config-admin/perfiles/:id`.
- Redirect histórico preservado: `/ach/nacha/config-profiles` -> `/nacha-config-admin/perfiles`.

Comandos/evidencia representativos:

```text
sha256sum ACHInterbank-v104-spa-4C3C0ad-profile-workspace-on-primary-token.zip
3ee0dc5e43ac29453718207fbb8b1a662922880bb135f9403958ab195747a119

grep -n -- '--color-on-secondary: #ffffff;' web/ach-interbank-ui/src/styles/_tokens.scss
36:  --color-on-secondary: #ffffff;

feature->feature 0
core->features 0
shared->features 0
backend_diff_count 0
routing_diff_count 0
```

## Angular

Se comprobó únicamente:

`web/ach-interbank-ui/node_modules/.bin/ng`

Resultado exacto:

```text
ng: not found
```

No se ejecutó `npm ping`, `npm ci`, registry retries, build ni tests. No se declara `VERIFIED`.
