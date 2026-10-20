# SPA 4C.3C0aj — CENIT Home On-Primary Branding

## Estado

`4C3C0AJ_CENIT_HOME_ON_PRIMARY_BRANDING_IMPLEMENTED_VALIDATION_PENDING`

## Baseline

- ZIP: `ACHInterbank-v109-spa-4C3C0ai-branding-runtime-coverage-checkpoint.zip`
- SHA-256 verificado: `78a39a615d1457f17d37b9279fb60610ad2233390d7de5ffb0a56ef4dadea96f`

## Precondiciones confirmadas

En `web/ach-interbank-ui/src/styles/_tokens.scss`:

```scss
--brand-on-primary: #ffffff;
--color-on-primary: var(--brand-on-primary);
```

En `BrandingService`, `buttonColor` determina el color primary runtime, se calcula su contraste y ese contraste se aplica a `--brand-on-primary` mediante `RUNTIME_TOKENS.onPrimary`.

## Cambio único

Archivo:

`web/ach-interbank-ui/src/app/features/cenit/components/cenit-home.component.scss`

Dentro de `.link-operativo`:

```diff
 .link-operativo {
   background: var(--color-primary);
-  color: #fff;
+  color: var(--color-on-primary);
 }
```

`.link-operativo:hover` permanece con `background: var(--color-primary-hover);`.

## Validación estática

- Reemplazos exactos: `1`.
- `color: #fff;` posterior en el SCSS objetivo: `0`.
- `color: var(--color-on-primary);` posterior: `1`.
- `background: var(--color-primary);`: intacto, `1` ocurrencia.
- `background: var(--color-primary-hover);`: intacto, `1` ocurrencia.
- Transformación binaria esperada del SCSS = salida real: `true`.
- CRLF preservados: `53 -> 53`.
- Resto del SCSS: byte-a-byte idéntico salvo la línea objetivo.
- TypeScript feature -> feature: `0`.
- TypeScript core -> features: `0`.
- TypeScript shared -> features: `0`.
- Backend `src/` diff: `0`.
- Routing: `29` archivos revisados, `0` diferencias.
- `features/admin/admin-routing.module.ts`: hash idéntico al baseline; `/users/branding` permanece intacta.
- `core/services/branding.service.ts`: hash idéntico al baseline; `api/users/branding` permanece intacto.

## Angular

Se comprobó únicamente:

`web/ach-interbank-ui/node_modules/.bin/ng`

Resultado exacto:

```text
ng: not found
```

No se ejecutó `npm ping`, `npm ci`, retries de registry, build ni tests. No se declara `VERIFIED`.
