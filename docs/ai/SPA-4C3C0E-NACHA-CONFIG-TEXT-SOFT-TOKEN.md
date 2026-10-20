# SPA 4C.3C0e — NACHA Config Variants/Fields text-soft token

## Estado

`4C3C0E_NACHA_CONFIG_TEXT_SOFT_TOKEN_IMPLEMENTED_VALIDATION_PENDING`

Checkpoint del micro-lote 4C.3C0e. No se avanzó a otro lote.

## Baseline

- Archivo: `ACHInterbank-v78-spa-4C3C0d-nacha-config-state-cleanup.zip`
- SHA-256 esperado/verificado: `2a5846b3da1a2cb02698906fbd7c11bd576f9ea20786cf5f82e76396217866dd`

## Precondición del token

Se inspeccionó `web/ach-interbank-ui/src/styles/_tokens.scss` antes del cambio.

Resultado:

```text
--color-text-soft: #64748b;
```

La definición aparece exactamente una vez. `_tokens.scss` no fue modificado; SHA-256 baseline/output:

```text
a49161a1b8984ba70b4df49fc4a1761ce457ca28e28e304ede1f2a273e291d15
```

## Cambio implementado

Único archivo fuente modificado:

`web/ach-interbank-ui/src/app/features/nacha-config-admin/pages/nacha-config-variants-fields-page.component.scss`

Se reemplazaron exactamente siete declaraciones:

```scss
color: #64748b;
```

por:

```scss
color: var(--color-text-soft);
```

Líneas del baseline afectadas: 110, 157, 295, 394, 432, 566 y 598.

No se cambió ningún selector, propiedad adicional, border/background, fallback, HTML, TS, spec, otro SCSS, ruta, service, core/shared, backend, contrato o configuración.

## Validación específica del SCSS

Resultados:

```text
baseline exact color declarations = 7
output exact color declarations   = 0
new token declarations            = 7
changed lines                     = 7
```

El fallback ya existente se preservó byte-a-byte:

```scss
var(--color-text-soft, #64748b)
```

Conteo baseline/output: `1 / 1`.

También permaneció intacta la declaración distinta:

```scss
border-left-color: #64748b;
```

Conteo baseline/output: `1 / 1`.

Una comparación línea a línea del SCSS confirma que las únicas diferencias son las siete sustituciones solicitadas; todas las demás líneas y sus terminadores permanecen idénticos.

SHA-256 del SCSS:

```text
baseline: 448abdd63a6c454b6781d0aa9c633a7cc5e7126a1c4e8669acca585200870aaa
output:   ebfdcffd6b88e79538e8f850661cd65ba87a29acde0f5e9e460be92a84f444d4
```

## Validación arquitectónica

Scan de imports TypeScript bajo `web/ach-interbank-ui/src/app`:

```text
feature -> feature = 0
core -> features    = 0
shared -> features  = 0
```

Backend:

```text
diff -qr baseline/src output/src
0 diferencias
```

Package/config revisado (`package*.json`, `angular.json`, `tsconfig*.json` relevantes): `0` diferencias.

Rutas: se compararon 29 archivos de routing entre baseline y output; `0` cambiaron. Permanecen, entre otras, las rutas públicas de NACHA Config Admin:

- `/nacha-config-admin/perfiles`
- `/nacha-config-admin/records`
- `/nacha-config-admin/variants-fields`
- `/nacha-config-admin/perfiles/:id`
- redirect histórico `/ach/nacha/config-profiles` → `/nacha-config-admin/perfiles`

## Angular

Se comprobó únicamente:

`web/ach-interbank-ui/node_modules/.bin/ng`

Resultado exacto:

```text
ng: not found
```

No se ejecutó `npm ping`, `npm ci`, registry retries, build ni tests. No se declara VERIFIED.

## Evidencia de comandos

Comandos/controles utilizados, de forma equivalente:

```bash
sha256sum ACHInterbank-v78-spa-4C3C0d-nacha-config-state-cleanup.zip
grep -n -- '--color-text-soft' web/ach-interbank-ui/src/styles/_tokens.scss
diff -u <baseline-scss> <output-scss>
diff -qr <baseline>/src <output>/src
# scan estático de imports TS con resolución de imports relativos
# comparación byte-a-byte de archivos de routing y package/config
[ -e web/ach-interbank-ui/node_modules/.bin/ng ] || printf 'ng: not found\n'
```
