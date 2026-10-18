# SPA 4C.3C0ak — Third Parties Primary Accent Branding

## Estado

`4C3C0AK_THIRD_PARTIES_PRIMARY_ACCENT_BRANDING_IMPLEMENTED_VALIDATION_PENDING`

No se declara `VERIFIED`: el ejecutable Angular solicitado no existe y no se ejecutaron build/tests.

## Baseline

- ZIP: `ACHInterbank-v110-spa-4C3C0aj-cenit-home-on-primary-branding.zip`
- SHA-256 esperado/confirmado: `6fa59e86f3cd4eeed9232b8355223fac5edebeb69d7e65c14a5f420a6d65c6f2`

## Precondiciones verificadas

En `web/ach-interbank-ui/src/styles/_tokens.scss`:

```scss
--brand-primary: #2563eb;
--color-primary: var(--brand-primary);
```

En `BrandingService`, `buttonColor` se normaliza como `primary` y se aplica en runtime mediante `root.style.setProperty('--brand-primary', primary)`.

## Cambio único de source

Archivo:

`web/ach-interbank-ui/src/app/features/customer-third-parties/components/customer-third-parties.component.css`

Dentro de `.detail-panel__notice`:

```diff
-  border-left: 4px solid #2563eb;
+  border-left: 4px solid var(--color-primary);
   background: #eff6ff;
```

Se realizó exactamente 1 sustitución.

## Validación estática

- `border-left: 4px solid #2563eb;`: 1 → 0
- `border-left: 4px solid var(--color-primary);`: 0 → 1
- `#2563eb` restante en el CSS objetivo: 0
- `background: #eff6ff;` en `.detail-panel__notice`: intacto
- La transformación binaria esperada del baseline coincide exactamente con el CSS resultante.
- CRLF del CSS objetivo: 211 → 211
- Resto del CSS: byte-a-byte intacto salvo la sustitución indicada.
- TypeScript feature → feature: 0
- TypeScript core → features: 0
- TypeScript shared → features: 0
- Backend `src/`: diff 0
- Routing: diff 0
- `BrandingService`: intacto
- `/users/branding`: intacta por diff de UI source/routing; no se modificó su pantalla, guards, permisos ni routing.
- `api/users/branding`: intacto; `BrandingService` no fue modificado.

Antes de agregar este reporte, el manifiesto de `web/ach-interbank-ui/src` mostró un único archivo cambiado: el CSS objetivo.

## Angular

Se comprobó únicamente:

`web/ach-interbank-ui/node_modules/.bin/ng`

Resultado exacto:

```text
ng: not found
```

No se ejecutó `npm ping`, `npm ci`, retries de registry, build ni tests.

## Comandos/evidencia principal

```bash
sha256sum ACHInterbank-v110-spa-4C3C0aj-cenit-home-on-primary-branding.zip
grep -nF -- '--brand-primary: #2563eb;' web/ach-interbank-ui/src/styles/_tokens.scss
grep -nF -- '--color-primary: var(--brand-primary);' web/ach-interbank-ui/src/styles/_tokens.scss
grep -nE 'buttonColor|brand-primary' web/ach-interbank-ui/src/app/core/services/branding.service.ts
grep -nF 'border-left: 4px solid #2563eb;' web/ach-interbank-ui/src/app/features/customer-third-parties/components/customer-third-parties.component.css
```

No se avanzó al siguiente lote.
