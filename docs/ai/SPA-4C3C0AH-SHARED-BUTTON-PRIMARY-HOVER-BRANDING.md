# SPA 4C.3C0ah — Shared Button Primary-Hover Branding

## Estado

`4C3C0AH_SHARED_BUTTON_PRIMARY_HOVER_BRANDING_IMPLEMENTED_VALIDATION_PENDING`

No se declara `VERIFIED`: no se ejecutaron build/tests Angular porque `web/ach-interbank-ui/node_modules/.bin/ng` no existe.

## Baseline

- ZIP: `ACHInterbank-v107-spa-4C3C0ag-branding-runtime-token-map.zip`
- SHA-256 esperado/verificado: `3a82a47f063e86ee6ec161122d3a5db4b60d1467bd350041e964865692810765`

## Precondiciones confirmadas

En `web/ach-interbank-ui/src/styles/_tokens.scss`:

```scss
--brand-primary-hover: #1d4ed8;
--color-primary-hover: var(--brand-primary-hover);
```

En `BrandingService`, `buttonColor` alimenta `primary`; el servicio deriva `hover` mediante `shiftHexLightness(primary, -0.12)` y lo publica en `--brand-primary-hover` mediante `root.style.setProperty(RUNTIME_TOKENS.primaryHover, hover)`. Por tanto `--color-primary-hover` queda enlazado al branding runtime actual sin ampliar contrato ni crear tokens.

## Cambio implementado

Único source modificado:

`web/ach-interbank-ui/src/styles/_components.scss`

Dos sustituciones exactas:

```scss
--btn-color: #1d4ed8;
```

por:

```scss
--btn-color: var(--color-primary-hover);
```

Ocurrencias modificadas: 2 (líneas baseline 194 y 304).

Se conservaron sin cambios:

```scss
--btn-bg: #eff6ff;
--btn-border: #93c5fd;
```

ambas con 2 ocurrencias antes y después.

## Validación estática

- `--btn-color: #1d4ed8;` antes: 2
- `--btn-color: #1d4ed8;` después: 0
- `--btn-color: var(--color-primary-hover);` después: 2
- `--btn-bg: #eff6ff;`: 2 → 2
- `--btn-border: #93c5fd;`: 2 → 2
- `_components.scss`: transformación binaria exacta del baseline sustituyendo únicamente esas 2 cadenas
- CRLF de `_components.scss`: 424 → 424
- feature → feature TypeScript imports: 0
- core → features TypeScript imports: 0
- shared → features TypeScript imports: 0
- backend `src/`: 1474 archivos; diff = 0
- `BrandingService`: SHA-256 idéntico al baseline
- `app-routing.module.ts`: SHA-256 idéntico al baseline
- `admin-routing.module.ts`: SHA-256 idéntico al baseline
- `/users/branding`: preservada como ruta `users` + child `branding`, bajo los guards/permisos existentes
- diff funcional previo al reporte: exactamente `web/ach-interbank-ui/src/styles/_components.scss`
- diff final esperado: ese SCSS + este reporte

### Evidencia/comandos principales

```bash
grep -nF -- '--brand-primary-hover: #1d4ed8;' web/ach-interbank-ui/src/styles/_tokens.scss
grep -nF -- '--color-primary-hover: var(--brand-primary-hover);' web/ach-interbank-ui/src/styles/_tokens.scss
grep -nF -- '--btn-color: #1d4ed8;' web/ach-interbank-ui/src/styles/_components.scss
grep -nF -- '--btn-color: var(--color-primary-hover);' web/ach-interbank-ui/src/styles/_components.scss
diff -qr <baseline>/src <output>/src
```

## Angular

Se comprobó únicamente:

`web/ach-interbank-ui/node_modules/.bin/ng`

Resultado exacto:

```text
ng: not found
```

No se ejecutó `npm ping`, `npm ci`, registry retries, build ni tests.
