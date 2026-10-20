# SPA 4C.3C0ab — Records style checkpoint

## Estado

`4C3C0AB_RECORDS_STYLE_CHECKPOINT_STATIC_OK_VALIDATION_PENDING`

Checkpoint **READ-ONLY** sobre el baseline `ACHInterbank-v101-spa-4C3C0aa-nacha-config-records-primary-hover-token.zip`.
No se modificó ningún source, token, HTML/TS/spec, SCSS, ruta, service, core/shared, backend, contrato ni configuración. El único archivo agregado por este lote es este reporte.

No se declara `VERIFIED`: Angular CLI no está disponible en el baseline y, conforme al alcance, no se ejecutaron instalación, acceso a registry, build ni tests.

## Baseline

- SHA-256 esperado: `20a4658619d1669d377b7f19c788cd9ace54199945b6f02c1ba6809465780d87`
- SHA-256 observado: `20a4658619d1669d377b7f19c788cd9ace54199945b6f02c1ba6809465780d87`
- SCSS auditado: `web/ach-interbank-ui/src/app/features/nacha-config-admin/pages/nacha-config-records-page.component.scss`
- SHA-256 del SCSS: `af97b108de9d3431318b53ddcd2f8d013104f6320a7e8c78fa9704a69eda64fd`
- Tokens comparados: `web/ach-interbank-ui/src/styles/_tokens.scss`
- SHA-256 de `_tokens.scss`: `a49161a1b8984ba70b4df49fc4a1761ce457ca28e28e304ede1f2a273e291d15`

Comando de baseline:

```bash
sha256sum ACHInterbank-v101-spa-4C3C0aa-nacha-config-records-primary-hover-token.zip
```

## Resultado del checkpoint de estilos

Quedan **7 ocurrencias hex**, correspondientes a **7 valores únicos**.

Clasificación actualizada por valor único:

- **A — hardcode directo con equivalencia exacta a token existente: 0**
- **B — valor presente únicamente como fallback existente: 5**
- **C — hardcode directo sin token exacto: 2**

Por tanto, **no quedan hardcodes directos con equivalencia exacta a tokens existentes**.
`#ecfdf5` y `#eff6ff` son los únicos hardcodes hex directos restantes y `_tokens.scss` no contiene un token con esos valores exactos. Los otros cinco valores hex aparecen únicamente dentro de fallbacks `var(..., fallback)` ya existentes.

No procede otra sustitución mecánica segura con los tokens actuales: continuar exigiría crear/redefinir tokens o alterar fallbacks preexistentes, ambos fuera de este lote.

Criterio: para comparar equivalencia cromática se normaliza `#fff` a `#ffffff`; la clasificación **B** prima cuando el valor aparece únicamente como fallback existente, aunque el fallback coincida con el valor de un token.

## Inventario hex restante

| Hex | Ocurrencias | Selector / propiedad | Comparación con `_tokens.scss` | Clase |
|---|---:|---|---|:---:|
| `#d9e2ec` | 1 | L13 `.selector-card, .summary-card, .issues-card, .records-card` → fallback de `border: 1px solid var(--color-border, #d9e2ec)` | Solo fallback; no coincide con `--color-border: #dbe3ee` | B |
| `#fff` | 1 | L15 mismo bloque de cards → fallback de `background: var(--color-surface, #fff)` | Solo fallback; normalizado `#ffffff`, coincide con `--color-surface: #ffffff` | B |
| `#cbd5e1` | 1 | L27 `.selector-grid select, .records-table input` → fallback de `border: 1px solid var(--color-border, #cbd5e1)` | Solo fallback; no coincide con `--color-border: #dbe3ee` | B |
| `#e2e8f0` | 1 | L80 `.records-table th, .records-table td` → fallback de `border-bottom: 1px solid var(--color-border, #e2e8f0)` | Solo fallback; no existe token exacto | B |
| `#64748b` | 1 | L96 `.records-table td:nth-child(2) small` → fallback de `color: var(--color-text-soft, #64748b)` | Solo fallback; coincide exactamente con `--color-text-soft: #64748b` | B |
| `#ecfdf5` | 1 | L109 `.flag-on` → `background` | Sin token exacto actual | C |
| `#eff6ff` | 1 | L122 `.estado` → `background` | Sin token exacto actual | C |

Comando de inventario:

```bash
grep -oE '#[0-9A-Fa-f]{3,8}\b' \
  web/ach-interbank-ui/src/app/features/nacha-config-admin/pages/nacha-config-records-page.component.scss \
  | sort | uniq -c | sort -nr
```

Resultado agregado:

```text
total hex: 7
valores únicos: 7
A: 0
B: 5
C: 2
```

## Validación arquitectónica

Escaneo estático de imports TypeScript relativos/alias resolubles bajo `web/ach-interbank-ui/src/app`:

```text
feature -> feature: 0
core -> features:   0
shared -> features: 0
```

Las rutas públicas existentes de NACHA Config Admin permanecen presentes e intactas:

- `/nacha-config-admin/perfiles`
- `/nacha-config-admin/records`
- `/nacha-config-admin/variants-fields`
- `/nacha-config-admin/perfiles/:id`
- redirect histórico `/ach/nacha/config-profiles` → `/nacha-config-admin/perfiles`

Los **29 archivos de routing** son idénticos al baseline. Guards y permisos no fueron modificados.

## Angular

Se comprobó únicamente:

```text
web/ach-interbank-ui/node_modules/.bin/ng
ng: not found
```

No se ejecutó `npm ping`, `npm ci`, retry de registry, build ni tests. **No se declara VERIFIED**.

## Inmutabilidad del lote

Validación contra el baseline extraído:

```text
UI source diff: 0
backend diff: 0
routing diff: 0
```

El diff global del árbol de salida contra el baseline contiene únicamente:

```text
+ docs/ai/SPA-4C3C0AB-RECORDS-STYLE-CHECKPOINT.md
```

No se avanzó al siguiente lote.
