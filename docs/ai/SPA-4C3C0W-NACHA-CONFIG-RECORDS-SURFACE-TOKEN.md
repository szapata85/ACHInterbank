# SPA 4C.3C0w — NACHA Config Records Surface Token

Estado: `4C3C0W_NACHA_CONFIG_RECORDS_SURFACE_TOKEN_IMPLEMENTED_VALIDATION_PENDING`

## Alcance

Micro-cambio mecánico limitado a:

`web/ach-interbank-ui/src/app/features/nacha-config-admin/pages/nacha-config-records-page.component.scss`

Sustitución única:

```scss
background: #fff;
```

por:

```scss
background: var(--color-surface);
```

## Precondición

En `web/ach-interbank-ui/src/styles/_tokens.scss` se confirmó:

```scss
--color-surface: #ffffff;
```

Conteo baseline en el SCSS objetivo:

- `background: #fff;`: 1
- `var(--color-surface, #fff)`: 1

## Validación del cambio

- Reemplazos realizados: **1**.
- `background: #fff;` posterior: **0**.
- `background: var(--color-surface);` posterior: **1**.
- Fallback existente `var(--color-surface, #fff)`: **1 → 1, intacto**.
- CRLF del archivo: **147 → 147**.
- La transformación binaria esperada (reemplazar una sola vez la secuencia objetivo) coincide exactamente con el archivo resultante.
- Antes de agregar este checkpoint, el diff global contra v96 contenía únicamente el SCSS objetivo.

Diff funcional:

```diff
 .record-selector select {
   min-width: 180px;
   border: 1px solid var(--color-border, #cbd5e1);
   border-radius: var(--radius-sm, 6px);
   padding: .35rem .5rem;
-  background: #fff;
+  background: var(--color-surface);
 }
```

## Arquitectura estática

Scan de imports TypeScript bajo `web/ach-interbank-ui/src/app`:

- feature → feature: **0**
- core → features: **0**
- shared → features: **0**

Backend (`src/`) diff contra baseline: **0**.

Routing: **29 archivos inspeccionados, 0 modificados**. Permanecen intactas las rutas públicas de NACHA Config Admin:

- `/nacha-config-admin/perfiles`
- `/nacha-config-admin/records`
- `/nacha-config-admin/variants-fields`
- `/nacha-config-admin/perfiles/:id`

También permanece el redirect histórico `nacha/config-profiles` → `/nacha-config-admin/perfiles` dentro del boundary `/ach` existente.

## Angular

Se comprobó únicamente:

`web/ach-interbank-ui/node_modules/.bin/ng`

Resultado exacto:

```text
ng: not found
```

No se ejecutó `npm ping`, `npm ci`, registry retries, build ni tests. No se declara `VERIFIED`.
