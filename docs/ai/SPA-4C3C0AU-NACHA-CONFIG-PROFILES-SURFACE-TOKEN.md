# SPA 4C.3C0au — NACHA Config Profiles Surface Token

## Tipo de micro-lote

Micro-cambio mecánico limitado a las **3 ocurrencias `SAFE_EXACT_TOKEN`** identificadas por 4C.3C0at en `nacha-config-profiles-page.component.scss`. No se implementó ninguna otra recomendación y no se realizó el checkpoint de cierre de 4C.3C0.

## Baseline e integridad

- Baseline: `ACHInterbank-v120-spa-4C3C0at-final-static-closure-inventory.zip`
- SHA-256 esperado: `2b911bbd0a53f54210362b5a87a6db1a79e4ec020968589541ee3cdb4f11f71a`
- SHA-256 observado: `2b911bbd0a53f54210362b5a87a6db1a79e4ec020968589541ee3cdb4f11f71a`
- Integridad: **coincide**.

## Precondición de token

Se confirmó en:

`web/ach-interbank-ui/src/styles/_tokens.scss`

la definición actual:

```scss
--color-surface: #ffffff;
```

ubicada en la línea 17 del baseline. `_tokens.scss` no fue modificado.

## Cambio implementado

Archivo único de source modificado:

`web/ach-interbank-ui/src/app/features/nacha-config-admin/pages/nacha-config-profiles-page.component.scss`

Se reemplazaron exclusivamente las tres declaraciones de las líneas 21, 66 y 72:

```scss
background: #fff;
```

por:

```scss
background: var(--color-surface);
```

Resultado: **exactamente 3 reemplazos**.

Validación binaria del archivo:

- antes: `background: #fff;` = **3**;
- después: `background: #fff;` = **0**;
- antes: `background: var(--color-surface);` = **0**;
- después: `background: var(--color-surface);` = **3**;
- al revertir únicamente esas 3 secuencias en memoria, el archivo resultante es byte-a-byte idéntico al baseline;
- terminadores CRLF preservados: **131 → 131**;
- SHA-256 SCSS baseline: `a7b43e4bf315274410d209e537907ffad2c92eba9cef17362f43536200d36c52`;
- SHA-256 SCSS resultado: `a1c8dfcea21fa387b0a209437ca7aec66f2530d0f7e1785e5eb77b14137adbcb`.

No se modificó ningún otro selector, propiedad, literal, fallback, sombra ni archivo SCSS/CSS.

## Residuales de estilos preservados

La modificación corresponde exactamente a las 3 ocurrencias clase A inventariadas por 4C.3C0at. Por inmutabilidad del resto del source:

```text
SAFE_EXACT_TOKEN pendientes en nacha-config-admin = 0
FALLBACK_ONLY = 13
NO_EXACT_TOKEN = 41
```

No se tocó ningún `FALLBACK_ONLY`, ningún `NO_EXACT_TOKEN`, ningún RGBA, ni se creó/redefinió token alguno. `DESIGN_TOKEN_STATIC` no se convirtió en branding configurable.

## Arquitectura / ownership

El feature `nacha-config-admin` conserva **22 archivos**: **17 productivos** y **5 specs**. El único archivo del feature modificado es el SCSS objetivo, por lo que las relaciones de ownership/imports y la condición de orphans del baseline no cambian.

Scan estático de imports TypeScript bajo `web/ach-interbank-ui/src/app`:

```text
orphans pendientes = 0
feature -> feature = 0
core -> features = 0
shared -> features = 0
```

No se modificaron HTML, TS, specs, facade ni services.

## Branding runtime y autorización

La modificación es exclusivamente una sustitución estática de surface token en el SCSS de `nacha-config-admin`; no toca `BrandingService`, branding runtime, routing, guards, permisos ni backend.

Se conserva el estado del checkpoint runtime:

`4C3C0AR_BRANDING_RUNTIME_STATIC_CLOSED_VALIDATION_PENDING`

con:

```text
branding runtime bypasses = 0
```

También se conserva la clasificación de autorización:

`ALIGNED_BY_MAPPING`

sin nueva evidencia y sin cambio de permisos:

```text
authorization unresolved = 0
/users/branding intacta
api/users/branding intacto
```

## Diff permitido contra v120

El paquete v121 contiene únicamente dos diferencias de contenido respecto del baseline v120:

1. modificación de `web/ach-interbank-ui/src/app/features/nacha-config-admin/pages/nacha-config-profiles-page.component.scss` con las 3 sustituciones descritas;
2. adición de `docs/ai/SPA-4C3C0AU-NACHA-CONFIG-PROFILES-SURFACE-TOKEN.md`.

Todos los demás archivos preexistentes permanecen byte-a-byte idénticos. En particular:

```text
ningún otro archivo source modificado
backend diff = 0
routing diff = 0
/users/branding intacta
api/users/branding intacto
```

## Angular

Se comprobó exclusivamente:

`web/ach-interbank-ui/node_modules/.bin/ng`

Resultado exacto:

`ng: not found`

No se ejecutó `npm ping`, `npm ci`, registry retries, build ni tests. **No se declara VERIFIED.**

## Alcance posterior

No se realiza todavía el checkpoint de cierre de 4C.3C0 y no se implementa ninguna otra recomendación.

## Estado

`4C3C0AU_NACHA_CONFIG_PROFILES_SURFACE_TOKEN_IMPLEMENTED_VALIDATION_PENDING`
