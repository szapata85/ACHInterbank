# SPA 4C.3C0m — NACHA Config success-text token checkpoint

Estado: `4C3C0M_NACHA_CONFIG_SUCCESS_TEXT_TOKEN_IMPLEMENTED_VALIDATION_PENDING`

## Baseline

- ZIP: `ACHInterbank-v86-spa-4C3C0l-nacha-config-success-surface-token.zip`
- SHA-256 esperado/observado: `216d4fb458f9da15aec7d64576ae9f6a9609488b8554f605080472665c8750c2`

## Precondición

En `web/ach-interbank-ui/src/styles/_tokens.scss` se confirmó:

```scss
--color-success-text: #166534;
```

En el SCSS objetivo había exactamente una declaración `color: #166534;`.

## Cambio implementado

Único cambio funcional en:

`web/ach-interbank-ui/src/app/features/nacha-config-admin/pages/nacha-config-variants-fields-page.component.scss`

```diff
 .semantic-badge[data-state='configured'],
 .badge-editable {
   background: var(--color-success-surface);
-  color: #166534;
+  color: var(--color-success-text);
 }
```

No se modificó ningún otro selector, propiedad o color.

## Evidencia y validaciones

Comandos/equivalentes usados:

```bash
sha256sum ACHInterbank-v86-spa-4C3C0l-nacha-config-success-surface-token.zip
grep -n -- '--color-success-text: #166534;' web/ach-interbank-ui/src/styles/_tokens.scss
grep -nF 'color: #166534;' web/ach-interbank-ui/src/app/features/nacha-config-admin/pages/nacha-config-variants-fields-page.component.scss
diff -u <baseline-target.scss> <output-target.scss>
```

Resultados:

- Reemplazos exactos: **1**.
- `#166534` posterior en el SCSS objetivo: **0**.
- `#16a34a`: **2 antes / 2 después**, intacto.
- El output del SCSS es byte-a-byte igual al baseline después de aplicar únicamente la sustitución literal solicitada.
- TypeScript feature → feature: **0**.
- TypeScript core → features: **0**.
- TypeScript shared → features: **0**.
- Backend `src/`: **0 diferencias**.
- `database/`: **0 diferencias**.
- `tests/`: **0 diferencias**.
- Archivos de routing: sin cambios respecto del baseline; rutas públicas y redirects existentes permanecen intactos.
- El diff del paquete contiene únicamente el SCSS objetivo y este documento de checkpoint.

### Angular

Se comprobó únicamente:

`web/ach-interbank-ui/node_modules/.bin/ng`

Resultado exacto:

```text
ng: not found
```

No se ejecutaron `npm ping`, `npm ci`, registry retries, build ni tests. No se declara `VERIFIED`.
