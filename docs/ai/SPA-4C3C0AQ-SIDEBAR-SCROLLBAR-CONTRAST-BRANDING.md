# SPA 4C.3C0aq — Sidebar Scrollbar Contrast Branding

## Estado

`4C3C0AQ_SIDEBAR_SCROLLBAR_CONTRAST_BRANDING_IMPLEMENTED_VALIDATION_PENDING`

No se declara `VERIFIED` porque `web/ach-interbank-ui/node_modules/.bin/ng` no existe y, por instrucción del micro-lote, no se ejecutaron npm ping, npm ci, retries de registry, build ni tests.

## Baseline

- ZIP: `ACHInterbank-v116-spa-4C3C0ap-branding-runtime-final-closure-checkpoint.zip`
- SHA-256 esperado/verificado: `6342c13b9903fd4a028cbe00b50e5a56c0b47565c855ce00400abd888b127411`

## Precondiciones confirmadas

- `_tokens.scss` mantiene `--color-on-sidebar: var(--brand-sidebar-on-background);`.
- `BrandingService` normaliza `sidebarBackground`, calcula `sidebarContrast = getContrastColor(sidebar)` y asigna el resultado a `--brand-sidebar-on-background` mediante `RUNTIME_TOKENS.sidebarOnBackground`.
- `main-layout.component.scss` ya utilizaba `color-mix(in srgb, var(--color-on-sidebar) ..., transparent)` en otros elementos del mismo sidebar antes de este cambio.

## Cambio implementado

Archivo único source modificado:

`web/ach-interbank-ui/src/app/layout/main-layout.component.scss`

Se realizaron exactamente tres sustituciones:

1. `scrollbar-color: rgba(226, 232, 240, 0.28) transparent;`
   → `scrollbar-color: color-mix(in srgb, var(--color-on-sidebar) 28%, transparent) transparent;`
2. `background: rgba(226, 232, 240, 0.28);`
   → `background: color-mix(in srgb, var(--color-on-sidebar) 28%, transparent);`
3. `background-color: rgba(226, 232, 240, 0.48);`
   → `background-color: color-mix(in srgb, var(--color-on-sidebar) 48%, transparent);`

## Validación estática

- Reemplazos exactos: `3`.
- `rgba(226, 232, 240, 0.28)` en las declaraciones objetivo después: `0`.
- `rgba(226, 232, 240, 0.48)` en la declaración objetivo después: `0`.
- `color-mix(in srgb, var(--color-on-sidebar) 28%, transparent)` después: `2` usos nuevos en el scrollbar.
- `color-mix(in srgb, var(--color-on-sidebar) 48%, transparent)` después: `1` uso nuevo en el scrollbar.
- Scrollbar track: `background: transparent;` intacto.
- CRLF del SCSS: `522 → 522`.
- El SCSS resultante fue comparado contra una reconstrucción byte-a-byte del baseline aplicando solo las tres sustituciones solicitadas: coincide exactamente.
- Ningún cuarto archivo source fue modificado.
- `feature -> feature = 0`.
- `core -> features = 0`.
- `shared -> features = 0`.
- Backend `src/`: `1474` archivos, diff `0`.
- Routing: `29` archivos, diff `0`.
- `BrandingService`: intacto.
- `_tokens.scss`: intacto.
- `/users/branding`: ruta intacta.
- `api/users/branding`: controlador intacto.

## Scan específico del bypass 4C.3C0ap

`sidebar scrollbar bypass pendiente = 0`

No se realizó un nuevo cierre global y no se corrigió ningún otro posible hallazgo fuera del bypass lógico solicitado.

## Angular

Se comprobó exclusivamente:

`web/ach-interbank-ui/node_modules/.bin/ng`

Resultado exacto:

`ng: not found`
