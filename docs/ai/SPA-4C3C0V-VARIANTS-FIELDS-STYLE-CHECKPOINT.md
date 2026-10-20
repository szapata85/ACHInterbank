# SPA 4C.3C0v — Variants/Fields style checkpoint

## Estado

`4C3C0V_VARIANTS_FIELDS_STYLE_CHECKPOINT_STATIC_OK_VALIDATION_PENDING`

Checkpoint **READ-ONLY** sobre el baseline `ACHInterbank-v95-spa-4C3C0u-nacha-config-text-soft-border-token.zip`.
No se modificó ningún source, token, HTML/TS/spec, SCSS, ruta, service, core/shared, backend, contrato ni configuración. El único archivo agregado por este lote es este reporte.

## Baseline

- SHA-256 esperado y comprobado del ZIP: `6bcb74155edb850b1b58c891328a4b6e3849b50cc2f70ad97678c653172b78c6`
- SCSS auditado: `web/ach-interbank-ui/src/app/features/nacha-config-admin/pages/nacha-config-variants-fields-page.component.scss`
- SHA-256 del SCSS auditado: `93557e3a8252951ba6f36f6852afe39935346f2099045e4aa248b0f1639534f4`
- Tokens comparados: `web/ach-interbank-ui/src/styles/_tokens.scss`
- SHA-256 de `_tokens.scss`: `a49161a1b8984ba70b4df49fc4a1761ce457ca28e28e304ede1f2a273e291d15`

## Resultado del checkpoint de estilos

Tras C0e–C0u quedan **40 ocurrencias hex** en el SCSS objetivo, correspondientes a **19 valores únicos**.

Clasificación actualizada por valor único:

- **A — equivalencia exacta con token existente en una declaración directa: 0**
- **B — valor presente únicamente como fallback existente: 3**
- **C — sin token exacto: 16**

Por tanto, **no quedan hardcodes directos con equivalencia exacta a tokens existentes**. Los hex restantes son únicamente fallbacks preexistentes o valores para los que `_tokens.scss` no define una equivalencia exacta. No procede otra sustitución mecánica segura sin crear o redefinir tokens, lo cual está fuera de este lote.

Criterio de comparación: se normaliza `#fff` a `#ffffff` y se resuelven las equivalencias directas/aliases definidos en `_tokens.scss` (por ejemplo, tokens que apuntan a `var(--brand-...)`).

## Inventario hex restante

| Hex | Ocurrencias | Selector / propiedad | Comparación con `_tokens.scss` | Clase |
|---|---:|---|---|:---:|
| `#e2e8f0` | 8 | L154 `.context-step` → `border`; L189 `.step-number` → `background`; L242 `.master-panel` → `border-right`; L407 `.semantic-value` → `border`; L469 `.technical-details dl > div` → `border`; L535 `.semantic-badge[data-state='not-applicable']` → `background`; L571 `.form-grid` → `border-top`; L651 `@media <=780 .master-panel` → `border-bottom` | Sin token exacto | C |
| `#172033` | 5 | L42 headings → `color`; L94 `.field input/select/textarea` → `color`; L266 `.master-item` → `color`; L426 `.semantic-value p` → `color`; L587 `.rule-list button` → `color` | Sin token exacto | C |
| `#334155` | 4 | L79 fallback de `color: var(--color-text-muted, #334155)`; L376 `.semantic-legend summary` → `color`; L421 `.semantic-value strong` → `color`; L453 `.technical-details summary` → `color` | Sin token exacto; `--color-text-muted` = `#475569` | C |
| `#eff6ff` | 4 | L162 `.context-step.is-complete` → `background`; L277 `.master-item.is-selected` → `background`; L516 `.semantic-badge, .badge` → `background`; L594 `.rule-list button.is-selected` → `background` | Sin token exacto | C |
| `#cbd5e1` | 3 | L91 fallback de `border: ... var(--color-border, #cbd5e1)`; L446 `.technical-details` → `border`; L555 `.semantic-empty` → `border` | Sin token exacto; `--color-border` = `#dbe3ee` | C |
| `#16a34a` | 2 | L311 `.status-dot` → `background`; L489 `.semantic-value[data-state='configured']` → `border-left-color` | Sin token exacto; `--color-success` = `#15803d` | C |
| `#dbeafe` | 2 | L365 `.detail-tabs span` → `background`; L530 `.semantic-badge[data-state='calculated']` → `background` | Sin token exacto | C |
| `#1e3a8a` | 1 | L163 `.context-step.is-complete` → `color` | Sin token exacto | C |
| `#64748b` | 1 | L53 fallback de `color: var(--color-text-soft, #64748b)` | Solo fallback existente; coincide con `--color-text-soft: #64748b` | B |
| `#92400e` | 1 | L542 pending/read-only badge → `color` | Sin token exacto; `--color-warning-text` = `#9a3412` | C |
| `#93c5fd` | 1 | L272 `.master-item:hover` → `border-color` | Sin token exacto | C |
| `#94a3b8` | 1 | L315 `.status-dot.is-inactive` → `background` | Sin token exacto | C |
| `#bfdbfe` | 1 | L161 `.context-step.is-complete` → `border-color` | Sin token exacto | C |
| `#d9e2ec` | 1 | L18 fallback de `border: ... var(--color-border, #d9e2ec)` | Solo fallback existente; no coincide con `--color-border: #dbe3ee` | B |
| `#f1f5f9` | 1 | L331 `.detail-tabs` → `background` | Sin token exacto | C |
| `#fee2e2` | 1 | L546 blocking badge → `background` | Sin token exacto; `--color-error-surface` = `#fef2f2` | C |
| `#fef3c7` | 1 | L541 pending/read-only badge → `background` | Sin token exacto; `--color-warning-surface` = `#ffedd5` | C |
| `#fff` | 1 | L20 fallback de `background: var(--color-surface, #fff)` | Solo fallback existente; normalizado `#ffffff`, coincide con `--color-surface` | B |
| `#fff7f7` | 1 | L506 `.semantic-value[data-state='blocking']` → `background` | Sin token exacto | C |

Comando de conteo:

```bash
grep -oE '#[0-9A-Fa-f]{3,8}\b' "web/ach-interbank-ui/src/app/features/nacha-config-admin/pages/nacha-config-variants-fields-page.component.scss" \
  | sort | uniq -c | sort -nr
```

Resultado agregado:

```text
total hex: 40
valores únicos: 19
A: 0
B: 3
C: 16
```

## Validación arquitectónica

Scan estático de imports TypeScript relativos resolubles bajo `web/ach-interbank-ui/src/app`:

```text
feature -> feature: 0
core -> features:   0
shared -> features: 0
```

Las rutas públicas de NACHA Config Admin permanecen presentes e intactas:

- `/nacha-config-admin/perfiles`
- `/nacha-config-admin/records`
- `/nacha-config-admin/variants-fields`
- `/nacha-config-admin/perfiles/:id`
- redirect histórico `/ach/nacha/config-profiles` → `/nacha-config-admin/perfiles`

No se modificaron guards ni permisos.

## Angular

Se comprobó únicamente:

```text
web/ach-interbank-ui/node_modules/.bin/ng
ng: not found
```

No se ejecutó `npm ping`, `npm ci`, retry de registry, build ni tests. **No se declara VERIFIED**.

## Inmutabilidad del lote

Contra el baseline extraído:

```text
UI source diff: 0
backend diff: 0
routing diff: 0
```

El diff global del árbol de salida contra el baseline contiene únicamente este reporte:

```text
+ docs/ai/SPA-4C3C0V-VARIANTS-FIELDS-STYLE-CHECKPOINT.md
```
