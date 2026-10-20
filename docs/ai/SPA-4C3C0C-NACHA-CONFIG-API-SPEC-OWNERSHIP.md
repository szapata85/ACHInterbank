# SPA 4C.3C0c — NACHA Config API Spec Ownership

## Baseline e integridad

- Baseline: `ACHInterbank-v76-spa-4C3C0b-nacha-config-orphan-cleanup.zip`
- SHA-256 esperado: `c82f4474b3d0d122af26aa1b27251c5a017a3bcc1f5a13640f6447410b17e3ae`
- SHA-256 observado: `c82f4474b3d0d122af26aa1b27251c5a017a3bcc1f5a13640f6447410b17e3ae`
- Integridad del baseline: **PASS**.

## Cambio único

Se movió exclusivamente el spec que prueba `NachaConfigApiService`, desde ownership de feature hacia el mismo boundary `core/services` del service:

```text
FROM web/ach-interbank-ui/src/app/features/nacha-config-admin/services/nacha-config-api.service.spec.ts
TO   web/ach-interbank-ui/src/app/core/services/nacha-config-api.service.spec.ts
```

Solo cambiaron los tres imports relativos requeridos por el nuevo path:

```diff
-import { environment } from '../../../../environments/environment';
-import { NachaConfigApiService } from '../../../core/services/nacha-config-api.service';
-import { NACHA_CONFIG_READ_FALLBACK_DELAY_MS } from '../../../core/services/nacha-config-read-retry';
+import { environment } from '../../../environments/environment';
+import { NachaConfigApiService } from './nacha-config-api.service';
+import { NACHA_CONFIG_READ_FALLBACK_DELAY_MS } from './nacha-config-read-retry';
```

No se modificaron `NachaConfigApiService`, `nacha-config-read-retry`, `NachaConfigStateService`, query/command services, páginas, rutas, guards, permisos, backend, contratos/endpoints, package/config ni ningún otro spec.

## Preservación del cuerpo de tests

El cuerpo del spec desde `describe('NachaConfigApiService'` hasta EOF es byte-a-byte idéntico al baseline:

```text
baseline bytes: 10271
output bytes:   10271
baseline SHA-256: b269d7f0e797825176ea6004a34002f1beaee5bbcf6d30b27eb3216e0a65c558
output SHA-256:   b269d7f0e797825176ea6004a34002f1beaee5bbcf6d30b27eb3216e0a65c558
body_identical=yes
```

## Paths / unicidad

```text
old path absent: yes
new path present: yes
nacha-config-api.service.spec.ts count: 1
```

## Boundary/imports

Scan estático global de imports/exports TypeScript bajo `web/ach-interbank-ui/src/app`:

```text
feature -> feature: 0
core -> features: 0
shared -> features: 0
```

## Rutas públicas

Los siguientes archivos de routing conservan el mismo SHA-256 antes y después:

```text
app-routing.module.ts
8aebb136a0f5d1ae9f22b4af1467a3191766aa6836f3314d08e2b9c94150e11c

features/nacha-config-admin/nacha-config-admin-routing.module.ts
c42c257234b1e236c67123b09ffd676c6a0614da49f5c2090df6ee60c42cd254

features/nacha-operational/nacha-operational-routing.module.ts
49a57ebe9754d9767bde1b68fe429212c0e30c6a4478c027356b1851b30ff0bb
```

Permanecen presentes las rutas públicas de NACHA Config Admin:

- `/nacha-config-admin/perfiles`
- `/nacha-config-admin/records`
- `/nacha-config-admin/variants-fields`
- `/nacha-config-admin/perfiles/:id`
- redirect histórico `/ach/nacha/config-profiles` → `/nacha-config-admin/perfiles`

## Diff / backend / config

Antes de agregar este reporte, el diff completo contra una extracción prístina del baseline contenía únicamente la relocalización del spec:

```text
Only in output/.../core/services: nacha-config-api.service.spec.ts
Only in baseline/.../features/nacha-config-admin/services: nacha-config-api.service.spec.ts
```

El contenido desde `describe(...)` es idéntico y el header solo difiere en los tres imports relativos documentados arriba. Después de agregar este reporte, el diff esperado es exactamente **move del spec + este documento**.

Validaciones adicionales:

```text
backend diff: 0
package/config diff count: 0
```

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
sha256sum ACHInterbank-v76-spa-4C3C0b-nacha-config-orphan-cleanup.zip

find web/ach-interbank-ui/src/app -name 'nacha-config-api.service.spec.ts'

python3 <comparación byte-a-byte desde describe(...) y scan de boundaries TypeScript>

sha256sum \
  web/ach-interbank-ui/src/app/app-routing.module.ts \
  web/ach-interbank-ui/src/app/features/nacha-config-admin/nacha-config-admin-routing.module.ts \
  web/ach-interbank-ui/src/app/features/nacha-operational/nacha-operational-routing.module.ts

diff -qr <baseline>/src <output>/src

test -e web/ach-interbank-ui/node_modules/.bin/ng && echo 'ng: found' || echo 'ng: not found'
```

## Estado

`4C3C0C_NACHA_CONFIG_API_SPEC_OWNERSHIP_IMPLEMENTED_VALIDATION_PENDING`

No se avanzó al siguiente lote.
