# SPA 4C.3C0d — Remove Unused NACHA Config State

## Baseline e integridad

- Baseline: `ACHInterbank-v77-spa-4C3C0c-nacha-config-api-spec-ownership.zip`
- SHA-256 esperado: `ed40becfaa61d0b065d1d6a121222af331014ceab12c12ccb432c4d88635fb83`
- SHA-256 observado: `ed40becfaa61d0b065d1d6a121222af331014ceab12c12ccb432c4d88635fb83`
- Integridad del baseline: **PASS**.

## Reference scan previo

Se repitió el scan global bajo `web/ach-interbank-ui/src/app`, excluyendo únicamente el archivo candidato:

```text
NachaConfigStateService: 0 referencias
nacha-config-state:       0 referencias
```

Comando equivalente:

```bash
cd web/ach-interbank-ui/src/app
rg -n --fixed-strings 'NachaConfigStateService' . \
  --glob '!features/nacha-config-admin/services/nacha-config-state.service.ts'
rg -n --fixed-strings 'nacha-config-state' . \
  --glob '!features/nacha-config-admin/services/nacha-config-state.service.ts'
```

Ambos scans cumplieron la precondición de **0 referencias**.

## Cambio único

Se eliminó exclusivamente:

```text
web/ach-interbank-ui/src/app/features/nacha-config-admin/services/nacha-config-state.service.ts
```

No se modificó ningún otro TS/HTML/SCSS/spec, query/command service, API core, model, página, ruta, guard, permiso, backend, contrato/endpoint ni configuración.

## Reference scan posterior

Tras la eliminación:

```text
old path absent: yes
NachaConfigStateService references: 0
nacha-config-state references: 0
```

## Boundary/imports

Scan estático global de imports/exports TypeScript bajo `web/ach-interbank-ui/src/app`:

```text
feature -> feature: 0
core -> features: 0
shared -> features: 0
```

## Rutas públicas

El manifiesto SHA-256 de **29** archivos de routing/access relacionados del SPA es idéntico antes y después del cambio.

Hashes de las rutas relevantes:

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

Antes de agregar este reporte, el diff completo contra una extracción prístina del baseline contenía exactamente **una eliminación**:

```text
Only in baseline/.../features/nacha-config-admin/services: nacha-config-state.service.ts
```

El diff de `web/ach-interbank-ui/src` también contenía exactamente esa eliminación.

Después de agregar este reporte, el diff esperado es exactamente:

```text
- web/ach-interbank-ui/src/app/features/nacha-config-admin/services/nacha-config-state.service.ts
+ docs/ai/SPA-4C3C0D-NACHA-CONFIG-STATE-CLEANUP.md
```

Validaciones adicionales:

```text
backend src diff: 0
tests diff: 0
database diff: 0
package/config diff: 0
routing hash manifest: IDENTICAL
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
sha256sum ACHInterbank-v77-spa-4C3C0c-nacha-config-api-spec-ownership.zip

cd web/ach-interbank-ui/src/app
rg -n --fixed-strings 'NachaConfigStateService' . \
  --glob '!features/nacha-config-admin/services/nacha-config-state.service.ts'
rg -n --fixed-strings 'nacha-config-state' . \
  --glob '!features/nacha-config-admin/services/nacha-config-state.service.ts'

rm features/nacha-config-admin/services/nacha-config-state.service.ts

rg -n --fixed-strings 'NachaConfigStateService' .
rg -n --fixed-strings 'nacha-config-state' .

python3 <scan de boundaries TypeScript>

diff -qr <baseline>/web/ach-interbank-ui/src <output>/web/ach-interbank-ui/src
diff -qr <baseline>/src <output>/src

sha256sum \
  app-routing.module.ts \
  features/nacha-config-admin/nacha-config-admin-routing.module.ts \
  features/nacha-operational/nacha-operational-routing.module.ts

test -e web/ach-interbank-ui/node_modules/.bin/ng && echo 'ng: found' || echo 'ng: not found'
```

## Estado

`4C3C0D_NACHA_CONFIG_STATE_ORPHAN_REMOVED_VALIDATION_PENDING`

No se avanzó al siguiente lote.
