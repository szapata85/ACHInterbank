# SPA 4C.3C0t — NACHA Config remaining color debt inventory

## Estado

`4C3C0T_NACHA_CONFIG_REMAINING_COLOR_INVENTORY_COMPLETE_VALIDATION_PENDING`

Checkpoint **READ-ONLY** sobre el baseline `ACHInterbank-v93-spa-4C3C0s-nacha-config-error-hover-token.zip`.
No se modificó código fuente, rutas, servicios, contratos, configuración ni backend. El único archivo agregado por este lote es este reporte.

## Baseline

- ZIP: `ACHInterbank-v93-spa-4C3C0s-nacha-config-error-hover-token.zip`
- SHA-256 esperado y comprobado: `0c3844bacb2ec57a5f28bf53f46d7bf9a02945cbc78b6f2c75c5d9c3d4b16400`
- SCSS auditado: `web/ach-interbank-ui/src/app/features/nacha-config-admin/pages/nacha-config-variants-fields-page.component.scss`
- Tokens comparados: `web/ach-interbank-ui/src/styles/_tokens.scss`

## Resumen de deuda hex restante

Comando base de inventario:

```bash
grep -oE '#[0-9A-Fa-f]{3,8}\\b' \
  web/ach-interbank-ui/src/app/features/nacha-config-admin/pages/nacha-config-variants-fields-page.component.scss \
  | sort | uniq -c | sort -nr
```

Resultado:

- **41 ocurrencias hex** restantes.
- **19 valores hex únicos**.
- Clasificación por valor único: **A = 1**, **B = 2**, **C = 16**.

Criterio usado:

- **A — equivalencia exacta con token existente**: existe un token de `_tokens.scss` con el mismo valor normalizado y el hex tiene al menos una declaración directa restante.
- **B — solo fallback existente**: el valor aparece únicamente dentro de un `var(..., fallback)` ya existente; no se recomienda tocarlo en este lote.
- **C — sin token exacto**: no existe token con valor exacto en `_tokens.scss`.

> Para comparación, `#fff` se normaliza a `#ffffff`.

## Inventario detallado

| Hex | Ocurrencias | Selector / propiedad | Comparación con `_tokens.scss` | Clase |
|---|---:|---|---|:---:|
| `#e2e8f0` | 8 | L154 `.context-step` → `border`; L189 `.step-number` → `background`; L242 `.master-panel` → `border-right`; L407 `.semantic-value` → `border`; L469 `.technical-details dl > div` → `border`; L535 `.semantic-badge[data-state='not-applicable']` → `background`; L571 `.form-grid` → `border-top`; L651 `@media <=780 .master-panel` → `border-bottom` | Sin token exacto | C |
| `#172033` | 5 | L42 headings (`.section-head h2`, `.detail-heading h2`, `.master-head h3`) → `color`; L94 `.field input/select/textarea` → `color`; L266 `.master-item` → `color`; L426 `.semantic-value p` → `color`; L587 `.rule-list button` → `color` | Sin token exacto | C |
| `#eff6ff` | 4 | L162 `.context-step.is-complete` → `background`; L277 `.master-item.is-selected` → `background`; L516 `.semantic-badge, .badge` → `background`; L594 `.rule-list button.is-selected` → `background` | Sin token exacto | C |
| `#334155` | 4 | L79 `.field > span` → fallback de `color: var(--color-text-muted, #334155)`; L376 `.semantic-legend summary` → `color`; L421 `.semantic-value strong` → `color`; L453 `.technical-details summary` → `color` | Sin token exacto. `--color-text-muted` es `#475569`, no equivalente | C |
| `#cbd5e1` | 3 | L91 `.field input/select/textarea` → fallback de `border: ... var(--color-border, #cbd5e1)`; L446 `.technical-details` → `border`; L555 `.semantic-empty` → `border` | Sin token exacto. `--color-border` es `#dbe3ee`, no equivalente | C |
| `#dbeafe` | 2 | L365 `.detail-tabs span` → `background`; L530 `.semantic-badge[data-state='calculated']` → `background` | Sin token exacto | C |
| `#64748b` | 2 | L53 section/detail descriptive text → fallback de `color: var(--color-text-soft, #64748b)`; L497 `.semantic-value[data-state='not-applicable']` → `border-left-color` | Exacto: `--color-text-soft: #64748b` | A |
| `#16a34a` | 2 | L311 `.status-dot` → `background`; L489 `.semantic-value[data-state='configured']` → `border-left-color` | Sin token exacto. `--color-success` es `#15803d` | C |
| `#fff7f7` | 1 | L506 `.semantic-value[data-state='blocking']` → `background` | Sin token exacto | C |
| `#fff` | 1 | L20 `.card` → fallback de `background: var(--color-surface, #fff)` | Solo fallback. Normalizado `#ffffff`; coincide con `--color-surface` y otros tokens blancos, pero no hay declaración directa `#fff` | B |
| `#fef3c7` | 1 | L541 pending/read-only badge → `background` | Sin token exacto. `--color-warning-surface` es `#ffedd5` | C |
| `#fee2e2` | 1 | L546 blocking badge → `background` | Sin token exacto. `--color-error-surface` es `#fef2f2` | C |
| `#f1f5f9` | 1 | L331 `.detail-tabs` → `background` | Sin token exacto | C |
| `#d9e2ec` | 1 | L18 `.card` → fallback de `border: ... var(--color-border, #d9e2ec)` | Solo fallback; sin token exacto. `--color-border` es `#dbe3ee` | B |
| `#bfdbfe` | 1 | L161 `.context-step.is-complete` → `border-color` | Sin token exacto | C |
| `#94a3b8` | 1 | L315 `.status-dot.is-inactive` → `background` | Sin token exacto | C |
| `#93c5fd` | 1 | L272 `.master-item:hover` → `border-color` | Sin token exacto | C |
| `#92400e` | 1 | L542 pending/read-only badge → `color` | Sin token exacto. `--color-warning-text` es `#9a3412` | C |
| `#1e3a8a` | 1 | L163 `.context-step.is-complete` → `color` | Sin token exacto | C |

## Próximo micro-cambio mecánico recomendado — no implementado

El siguiente cambio mínimo y de equivalencia exacta sería **una sola sustitución** en `.semantic-value[data-state='not-applicable']`:

```scss
border-left-color: #64748b;
```

por:

```scss
border-left-color: var(--color-text-soft);
```

Motivo: `#64748b` es el único valor restante con una declaración directa y equivalencia exacta ya definida (`--color-text-soft: #64748b`). El fallback existente `var(--color-text-soft, #64748b)` de otra regla debe permanecer intacto. **Este checkpoint no implementa la recomendación.**

## Validación arquitectónica

Scan estático de imports TypeScript relativos/alias resolubles bajo `web/ach-interbank-ui/src/app`:

```text
feature -> feature: 0
core -> features:   0
shared -> features: 0
```

Rutas públicas NACHA Config Admin confirmadas en el baseline y preservadas sin cambios:

- `/nacha-config-admin/perfiles`
- `/nacha-config-admin/records`
- `/nacha-config-admin/variants-fields`
- `/nacha-config-admin/perfiles/:id`
- redirect histórico `/ach/nacha/config-profiles` → `/nacha-config-admin/perfiles`

Guards/permisos no fueron modificados.

## Angular

Se comprobó únicamente:

```text
web/ach-interbank-ui/node_modules/.bin/ng
ng: not found
```

No se ejecutó `npm ping`, `npm ci`, retry de registry, build ni tests.
Por tanto, **no se declara VERIFIED**.

## Inmutabilidad del lote

Validaciones contra el baseline extraído:

```text
UI source diff: 0
backend diff: 0
routing diff: 0
```

El diff global del árbol de salida contra el baseline debe contener únicamente:

```text
+ docs/ai/SPA-4C3C0T-NACHA-CONFIG-COLOR-INVENTORY.md
```
