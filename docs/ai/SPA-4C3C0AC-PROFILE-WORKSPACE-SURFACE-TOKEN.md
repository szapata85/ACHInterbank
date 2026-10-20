# SPA 4C.3C0ac — Profile Workspace Surface Token

## Estado

`4C3C0AC_NACHA_CONFIG_PROFILE_WORKSPACE_SURFACE_TOKEN_IMPLEMENTED_VALIDATION_PENDING`

No se declara `VERIFIED`: el ejecutable Angular requerido no está disponible y, por instrucción del micro-lote, no se ejecutaron instalación, acceso a registry, build ni tests.

## Baseline

- ZIP: `ACHInterbank-v102-spa-4C3C0ab-records-style-checkpoint.zip`
- SHA-256 esperado y comprobado: `937b8da96cf6ee18ad44803aa8dba35967de09ac66569bca666eb73ae18112a6`

Comando:

```bash
sha256sum ACHInterbank-v102-spa-4C3C0ab-records-style-checkpoint.zip
```

## Precondición de token

En `web/ach-interbank-ui/src/styles/_tokens.scss` se confirmó:

```scss
--color-surface: #ffffff;
```

## Cambio aplicado

Único source modificado:

`web/ach-interbank-ui/src/app/features/nacha-config-admin/pages/nacha-config-profile-workspace-page.component.scss`

Se reemplazaron exactamente las 2 declaraciones:

```scss
background: #fff;
```

por:

```scss
background: var(--color-surface);
```

Las dos ubicaciones son `.btn-workspace` y `.btn-outline`.

Diff funcional:

```diff
 .btn-workspace {
-  background: #fff;
+  background: var(--color-surface);
   color: var(--color-text);
 }
@@
 .btn-outline {
-  background: #fff;
+  background: var(--color-surface);
   color: var(--color-primary);
 }
```

## Validación exacta del SCSS

Validación binaria sobre el baseline y el resultado:

- `background: #fff;` baseline: `2`
- `background: #fff;` resultado: `0`
- `background: var(--color-surface);` nuevo: `2`
- `color: #fff;` baseline: `3`
- `color: #fff;` resultado: `3`
- CRLF baseline: `105`
- CRLF resultado: `105`
- el resultado es byte-a-byte igual a `baseline.replace("background: #fff;", "background: var(--color-surface);")`

Por tanto, fuera de esas dos sustituciones, el SCSS permanece byte-a-byte intacto.

## Dependencias TypeScript

Scan global bajo `web/ach-interbank-ui/src/app/**/*.ts`, resolviendo imports relativos:

- feature -> feature: `0`
- core -> features: `0`
- shared -> features: `0`

## Backend y rutas

Comparación de contenido contra el baseline:

- backend `src/`: `0` archivos diferentes
- archivos de routing comprobados: `29`
- archivos de routing diferentes: `0`

Las rutas públicas y guards/permisos permanecen intactos. En particular siguen presentes las rutas NACHA Config Admin `perfiles`, `records`, `variants-fields`, `perfiles/:id`, y el redirect histórico `nacha/config-profiles -> /nacha-config-admin/perfiles`.

## Angular

Se comprobó únicamente:

`web/ach-interbank-ui/node_modules/.bin/ng`

Resultado exacto:

```text
ng: not found
```

No se ejecutó `npm ping`, `npm ci`, registry retries, build ni tests.

## Alcance del diff

Antes de agregar este reporte, el único archivo diferente frente al baseline era el SCSS objetivo. El paquete final contiene solamente:

1. la modificación mecánica de 2 líneas en `nacha-config-profile-workspace-page.component.scss`;
2. este reporte de checkpoint.

No se modificaron HTML, TS, specs, otros SCSS, tokens, rutas, services, core/shared, backend, contratos/endpoints ni configuración.
