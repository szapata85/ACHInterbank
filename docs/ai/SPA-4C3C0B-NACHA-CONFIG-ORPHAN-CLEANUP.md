# SPA 4C.3C0b — Remove Orphan NACHA Config Pair

## Baseline e integridad

- Baseline: `ACHInterbank-v75-spa-4C3C0a-nacha-config-admin-inventory.zip`
- SHA-256 esperado: `31bce1e7a6de19b9b93a6ff46f0ccedea93702d16a5f4fe703a05ce26149b2b4`
- SHA-256 observado: `31bce1e7a6de19b9b93a6ff46f0ccedea93702d16a5f4fe703a05ce26149b2b4`
- Integridad del baseline: **PASS**.

## Cambio único

Se eliminaron exclusivamente los dos archivos orphan solicitados:

- `web/ach-interbank-ui/src/app/features/nacha-config-admin/pages/nacha-config-official-readonly-page.component.spec.ts`
- `web/ach-interbank-ui/src/app/features/nacha-config-admin/pages/nacha-config-official-readonly-page.component.scss`

No se modificó ningún otro archivo fuente, ruta, guard, permiso, service, contrato, archivo core/shared, backend ni configuración. Este documento es el único artefacto agregado.

## Reference scan

Antes de borrar se repitió el scan en `web/ach-interbank-ui/src/app`, excluyendo únicamente los dos archivos target. Resultado:

```text
pre_reference_scan_src_app_stem=0
```

Después de borrar:

```text
post_reference_scan_src_app_stem=0
```

El scan usa el stem `nacha-config-official-readonly-page`; por tanto cubre referencias por nombre de archivo/path en imports, metadata de estilos y código fuente del SPA.

## Preservación del spec canónico

`nacha-config-records-page.component.spec.ts` se preservó byte-a-byte:

```text
SHA-256 antes:  9fece8b8dfd6566256024464df704d72b0f1e639ad6281238fd0d9893a77fa78
SHA-256 después: 9fece8b8dfd6566256024464df704d72b0f1e639ad6281238fd0d9893a77fa78
```

## Boundary/imports

Scan estático global de imports/exports TypeScript bajo `web/ach-interbank-ui/src/app`:

```text
feature -> feature: 0
core -> features: 0
shared -> features: 0
```

## Rutas públicas

Los archivos de routing no cambiaron. Continúan presentes:

- `/nacha-config-admin/perfiles`
- `/nacha-config-admin/records`
- `/nacha-config-admin/variants-fields`
- `/nacha-config-admin/perfiles/:id`
- redirect histórico `/ach/nacha/config-profiles` → `/nacha-config-admin/perfiles`

Los guards, roles y permisos permanecen sin cambios.

## Diff / backend / config

Antes de agregar este reporte, el diff completo contra una extracción prístina del baseline arrojó exactamente dos eliminaciones:

```text
Only in baseline/.../nacha-config-admin/pages: nacha-config-official-readonly-page.component.scss
Only in baseline/.../nacha-config-admin/pages: nacha-config-official-readonly-page.component.spec.ts
```

Validaciones adicionales:

```text
backend diff: 0
package/config diff: 0
```

Después de agregar este reporte, el diff esperado es exactamente **esas dos eliminaciones + este documento**.

## Angular CLI

Se comprobó únicamente:

```text
web/ach-interbank-ui/node_modules/.bin/ng
ng: not found
```

No se ejecutó `npm ping`, `npm ci`, registry retries, build ni tests.

Por tanto, este lote **no declara VERIFIED**.

## Evidencia / comandos

```bash
sha256sum ACHInterbank-v75-spa-4C3C0a-nacha-config-admin-inventory.zip

rg -n --glob '!nacha-config-official-readonly-page.component.spec.ts' \
      --glob '!nacha-config-official-readonly-page.component.scss' \
      'nacha-config-official-readonly-page' web/ach-interbank-ui/src/app

sha256sum web/ach-interbank-ui/src/app/features/nacha-config-admin/pages/nacha-config-records-page.component.spec.ts

python3 <static TypeScript boundary import scan>

diff -qr <baseline>/src <output>/src

test -x web/ach-interbank-ui/node_modules/.bin/ng && echo 'ng: found' || echo 'ng: not found'
```

## Estado

`4C3C0B_NACHA_CONFIG_ORPHAN_CLEANUP_IMPLEMENTED_VALIDATION_PENDING`

No se avanzó al siguiente lote.
