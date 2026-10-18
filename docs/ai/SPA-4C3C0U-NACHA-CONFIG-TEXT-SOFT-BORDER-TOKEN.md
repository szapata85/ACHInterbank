# SPA 4C.3C0u — NACHA Config text-soft border token

## Estado

`4C3C0U_NACHA_CONFIG_TEXT_SOFT_BORDER_TOKEN_IMPLEMENTED_VALIDATION_PENDING`

Micro-lote limitado a una sustitución mecánica en:

`web/ach-interbank-ui/src/app/features/nacha-config-admin/pages/nacha-config-variants-fields-page.component.scss`

No se modificaron HTML, TypeScript, specs, otros SCSS, rutas, services, core/shared, backend, contratos, endpoints ni configuración.

## Baseline

- ZIP: `ACHInterbank-v94-spa-4C3C0t-nacha-config-color-inventory.zip`
- SHA-256 esperado y comprobado: `62af7207e4cf638ece03752844c70ba5e1a4fcd228a79cc50c3477796e8a927d`

## Precondición de token

Comando:

```bash
grep -nF -- '--color-text-soft: #64748b;' web/ach-interbank-ui/src/styles/_tokens.scss
```

Resultado:

```text
24:  --color-text-soft: #64748b;
```

## Cambio aplicado

Antes:

```scss
.semantic-value[data-state='not-applicable'] {
  border-left-color: #64748b;
}
```

Después:

```scss
.semantic-value[data-state='not-applicable'] {
  border-left-color: var(--color-text-soft);
}
```

Se realizó exactamente **1 reemplazo**.

## Validación de precisión

Comandos/evidencia:

```bash
grep -nF 'border-left-color: #64748b;' \
  web/ach-interbank-ui/src/app/features/nacha-config-admin/pages/nacha-config-variants-fields-page.component.scss
# 0 resultados

grep -nF 'border-left-color: var(--color-text-soft);' \
  web/ach-interbank-ui/src/app/features/nacha-config-admin/pages/nacha-config-variants-fields-page.component.scss
# 497:  border-left-color: var(--color-text-soft);

grep -nF 'var(--color-text-soft, #64748b)' \
  web/ach-interbank-ui/src/app/features/nacha-config-admin/pages/nacha-config-variants-fields-page.component.scss
# 53:  color: var(--color-text-soft, #64748b);
```

Conteos:

```text
baseline exact target count: 1
output exact target count:   0
output replacement count:    1
fallback baseline/output:    1 / 1
#64748b total posterior en SCSS objetivo: 1 (solo el fallback existente)
```

La reversión mecánica de `border-left-color: var(--color-text-soft);` a la declaración original reproduce byte-a-byte el SCSS baseline. El número de finales CRLF también permanece idéntico (`700 -> 700`). Por tanto, el resto del archivo quedó intacto salvo la línea solicitada.

Diff funcional:

```diff
 .semantic-value[data-state='not-applicable'] {
-  border-left-color: #64748b;
+  border-left-color: var(--color-text-soft);
 }
```

## Validación arquitectónica

Scan estático de imports TypeScript bajo `web/ach-interbank-ui/src/app`:

```text
feature -> feature: 0
core -> features:   0
shared -> features: 0
```

Validación de inmutabilidad:

```text
UI source diff: exactamente 1 archivo modificado (SCSS objetivo)
backend diff: 0
routing files: 29 / 29 idénticos
package/config protegidos: 6 / 6 idénticos
```

Las rutas públicas de NACHA Config Admin permanecen presentes e intactas:

- `/nacha-config-admin/perfiles`
- `/nacha-config-admin/records`
- `/nacha-config-admin/variants-fields`
- `/nacha-config-admin/perfiles/:id`
- redirect histórico `/ach/nacha/config-profiles` → `/nacha-config-admin/perfiles`

## Angular

Se comprobó únicamente:

```text
web/ach-interbank-ui/node_modules/.bin/ng
ng: not found
```

No se ejecutó `npm ping`, `npm ci`, retry de registry, build ni tests.
Por tanto, **no se declara VERIFIED**.

## Diff esperado del lote

Contra el baseline, el árbol de salida contiene únicamente:

```text
~ web/ach-interbank-ui/src/app/features/nacha-config-admin/pages/nacha-config-variants-fields-page.component.scss
+ docs/ai/SPA-4C3C0U-NACHA-CONFIG-TEXT-SOFT-BORDER-TOKEN.md
```
