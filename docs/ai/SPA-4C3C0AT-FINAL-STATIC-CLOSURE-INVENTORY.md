# SPA 4C.3C0at — Final Static Closure Inventory

## Tipo de checkpoint

Checkpoint **READ-ONLY** final del alcance 4C.3C0. No se implementaron cambios en source frontend/backend, SCSS/CSS, HTML/TS/spec, tokens, `BrandingService`, routing/guards/permisos, contratos, backend ni configuración. Este reporte es el único artefacto agregado al baseline.

## Baseline e integridad

- Baseline: `ACHInterbank-v119-spa-4C3C0as-branding-authorization-contract-checkpoint.zip`
- SHA-256 esperado: `34499ac785cdea314712d5a4c5c85bc61627d16c55ed30ea8f3ced933b71c9b9`
- SHA-256 observado: `34499ac785cdea314712d5a4c5c85bc61627d16c55ed30ea8f3ced933b71c9b9`
- Integridad: **coincide**.

Además, el baseline v119 contiene exactamente un archivo adicional respecto de v118 —`SPA-4C3C0AS-BRANDING-AUTHORIZATION-CONTRACT-CHECKPOINT.md`— y **0 archivos preexistentes modificados**, por lo que el source que soportó el cierre runtime 4C.3C0ar permanece byte-a-byte igual.

## Resumen ejecutivo consolidado

| Control | Resultado actual |
|---|---:|
| Archivos productivos actuales `nacha-config-admin` | **17** |
| Specs actuales dentro del feature | **5** |
| Archivos totales dentro del feature | **22** |
| Orphans de archivo pendientes | **0** |
| `feature -> feature` | **0** |
| `core -> features` | **0** |
| `shared -> features` | **0** |
| Branding runtime bypasses pendientes | **0** |
| Authorization unresolved | **0** |
| `SAFE_EXACT_TOKEN` implementables | **3 ocurrencias** |
| `FALLBACK_ONLY` | **13 ocurrencias** |
| `NO_EXACT_TOKEN` | **41 ocurrencias/declaraciones** = 39 hex directos + 2 sombras RGBA compuestas |
| ¿4C.3C0 puede cerrarse estáticamente tal como está? | **NO** |

La única razón demostrada en este inventario para **no** declarar todavía el cierre estático es que quedan **3 sustituciones cromáticas `SAFE_EXACT_TOKEN`** en un único SCSS productivo. No se crea ni redefine ningún token para resolverlas.

## 1. `nacha-config-admin` — ownership actual

### Archivos productivos actuales

Los **17 archivos productivos** actuales son:

```text
nacha-config-admin-routing.module.ts
nacha-config-admin.module.ts
pages/nacha-config-profile-workspace-page.component.html
pages/nacha-config-profile-workspace-page.component.scss
pages/nacha-config-profile-workspace-page.component.ts
pages/nacha-config-profiles-page.component.html
pages/nacha-config-profiles-page.component.scss
pages/nacha-config-profiles-page.component.ts
pages/nacha-config-records-page.component.html
pages/nacha-config-records-page.component.scss
pages/nacha-config-records-page.component.ts
pages/nacha-config-variants-fields-page.component.html
pages/nacha-config-variants-fields-page.component.scss
pages/nacha-config-variants-fields-page.component.ts
presentation/nacha-config-presentation.catalog.ts
services/nacha-config-command.service.ts
services/nacha-config-query.service.ts
```

Los **5 specs** colocados dentro del feature corresponden a las cuatro páginas y al catálogo de presentación:

```text
pages/nacha-config-profile-workspace-page.component.spec.ts
pages/nacha-config-profiles-page.component.spec.ts
pages/nacha-config-records-page.component.spec.ts
pages/nacha-config-variants-fields-page.component.spec.ts
presentation/nacha-config-presentation.catalog.spec.ts
```

El spec de `NachaConfigApiService` permanece correctamente relocalizado junto al service en `core/services/nacha-config-api.service.spec.ts`; ya no existe bajo `features/nacha-config-admin/services`.

### Orphans

`orphans pendientes = 0` a nivel de archivos del feature.

Se confirmó que:

- no reaparecieron `nacha-config-official-readonly-page.component.scss` ni su spec residual;
- no reapareció `nacha-config-state.service.ts`;
- `nacha-config-presentation.catalog.ts` es consumido por `nacha-config-variants-fields-page.component.ts`;
- las cuatro páginas están declaradas o referenciadas por routing según su modalidad NgModule/standalone;
- los dos services del feature (`Query` y `Command`) tienen consumidores productivos.

### Services: superficie sin call site

El scan de llamadas productivas dentro de `src/app` mantiene una superficie de facade sin call site actual:

- `NachaConfigQueryService`: `perfiles`, `detalleReadOnly`, `variantesReadOnly`, `fieldsReadOnly`, `historial`, `snapshots` → **0 call sites productivos** cada uno.
- `NachaConfigCommandService.preview` → **0 call sites productivos**.

Esto se documenta como **superficie pública residual/candidata**, no como orphan de archivo y no como autorización para borrarla en este checkpoint. No se recomienda aquí un cambio sobre estos métodos porque el objetivo del lote es inventario y existe primero un micro-cambio mecánico `SAFE_EXACT_TOKEN` claramente demostrable.

### Imports / dependencias

Scan estático global de imports/exports TypeScript bajo `web/ach-interbank-ui/src/app`:

```text
feature -> feature = 0
core -> features = 0
shared -> features = 0
```

`nacha-config-admin` depende de `core`, `shared` y archivos locales del propio feature; no importa TypeScript de otro feature. Se preserva el ownership del transporte/modelos NACHA en `core` y de las fachadas UI en el feature.

## 2. Estilos y tokens de `nacha-config-admin`

### Alcance de la clasificación

Para mantener el mismo contrato de los micro-lotes de estilos 4C.3C0, la clasificación siguiente cubre **hardcodes cromáticos residuales** en los SCSS productivos: hex directos, hex usados únicamente como fallback y sombras RGBA compuestas. Valores geométricos/locales (`rem`, tamaños, breakpoints, porcentajes, `transparent`, etc.) son `D. OTHER` para este cierre y no se convierten automáticamente en deuda de tokenización.

No se asume que un valor hex debe eliminarse solo por ser hex. La comparación se hace contra los tokens actuales de `web/ach-interbank-ui/src/styles/_tokens.scss` y exige equivalencia semántica + default exacto para clase A.

### Estilos/tokens ya tratados

- `nacha-config-profile-workspace-page.component.scss`: surface, on-primary, on-secondary y on-error; actualmente no conserva literales de color hex/RGBA.
- `nacha-config-variants-fields-page.component.scss`: text-soft, text-muted, error-text, text, primary, error, warning, success-surface, success-text, surface-muted, border, surface, on-primary, primary-hover, error-hover y el reemplazo exacto del border `#64748b` por `--color-text-soft`.
- `nacha-config-records-page.component.scss`: surface, error-surface, error-text, success-text y primary-hover; el checkpoint 4C.3C0ab ya dejó A=0 para ese archivo.
- `nacha-config-profiles-page.component.scss`: no tuvo un micro-lote equivalente de tokenización y es donde aparece el `SAFE_EXACT_TOKEN` residual de este inventario.

### A. `SAFE_EXACT_TOKEN`

**Cantidad: 3 ocurrencias**, todas en un único archivo y con un único valor (`#fff`).

`--color-surface` tiene default actual exacto `#ffffff`, equivalente a `#fff`, y las tres propiedades son backgrounds de superficie.

| Archivo | Línea | Hardcode actual | Token exacto existente | Clasificación |
|---|---:|---|---|---|
| `pages/nacha-config-profiles-page.component.scss` | 21 | `background: #fff;` | `var(--color-surface)` | A |
| `pages/nacha-config-profiles-page.component.scss` | 66 | `background: #fff;` | `var(--color-surface)` | A |
| `pages/nacha-config-profiles-page.component.scss` | 72 | `background: #fff;` | `var(--color-surface)` | A |

Estas son las **3 sustituciones todavía implementables mecánicamente** con tokens actuales. No se implementan en este lote.

### B. `FALLBACK_ONLY`

**Cantidad: 13 ocurrencias**, 8 valores hex distintos. Todas aparecen únicamente como fallback de un `var(...)` en la ocurrencia indicada y no se recomiendan para cambio en 4C.3C0at.

| Archivo | Valor | Líneas |
|---|---|---|
| `pages/nacha-config-profiles-page.component.scss` | `#d8dee8` | 70, 83 |
| `pages/nacha-config-profiles-page.component.scss` | `#155eef` | 78 |
| `pages/nacha-config-records-page.component.scss` | `#d9e2ec` | 13 |
| `pages/nacha-config-records-page.component.scss` | `#fff` | 15 |
| `pages/nacha-config-records-page.component.scss` | `#cbd5e1` | 27 |
| `pages/nacha-config-records-page.component.scss` | `#e2e8f0` | 80 |
| `pages/nacha-config-records-page.component.scss` | `#64748b` | 96 |
| `pages/nacha-config-variants-fields-page.component.scss` | `#d9e2ec` | 18 |
| `pages/nacha-config-variants-fields-page.component.scss` | `#fff` | 20 |
| `pages/nacha-config-variants-fields-page.component.scss` | `#64748b` | 53 |
| `pages/nacha-config-variants-fields-page.component.scss` | `#334155` | 79 |
| `pages/nacha-config-variants-fields-page.component.scss` | `#cbd5e1` | 91 |

> La tabla agrupa por valor/archivo; `#d8dee8` tiene dos ocurrencias, por eso el total es 13 aunque existan 12 filas agrupadas.

### C. `NO_EXACT_TOKEN`

**Cantidad: 41 ocurrencias/declaraciones**:

- **39** hardcodes hex directos;
- **2** sombras RGBA compuestas sin token actual exactamente equivalente.

No se propone crear tokens, redefinir defaults ni aproximar colores.

| Archivo | Valor / declaración | Líneas | Ocurrencias |
|---|---|---|---:|
| `pages/nacha-config-profiles-page.component.scss` | `#d8dee8` | 18 | 1 |
| `pages/nacha-config-profiles-page.component.scss` | `#5f6b7a` | 26 | 1 |
| `pages/nacha-config-records-page.component.scss` | `#ecfdf5` | 109 | 1 |
| `pages/nacha-config-records-page.component.scss` | `#eff6ff` | 122 | 1 |
| `pages/nacha-config-variants-fields-page.component.scss` | `#172033` | 42, 94, 266, 426, 587 | 5 |
| `pages/nacha-config-variants-fields-page.component.scss` | `#e2e8f0` | 154, 189, 242, 407, 469, 535, 571, 651 | 8 |
| `pages/nacha-config-variants-fields-page.component.scss` | `#bfdbfe` | 161 | 1 |
| `pages/nacha-config-variants-fields-page.component.scss` | `#eff6ff` | 162, 277, 516, 594 | 4 |
| `pages/nacha-config-variants-fields-page.component.scss` | `#1e3a8a` | 163 | 1 |
| `pages/nacha-config-variants-fields-page.component.scss` | `#93c5fd` | 272 | 1 |
| `pages/nacha-config-variants-fields-page.component.scss` | `#16a34a` | 311, 489 | 2 |
| `pages/nacha-config-variants-fields-page.component.scss` | `#94a3b8` | 315 | 1 |
| `pages/nacha-config-variants-fields-page.component.scss` | `#f1f5f9` | 331 | 1 |
| `pages/nacha-config-variants-fields-page.component.scss` | `#dbeafe` | 365, 530 | 2 |
| `pages/nacha-config-variants-fields-page.component.scss` | `#334155` | 376, 421, 453 | 3 |
| `pages/nacha-config-variants-fields-page.component.scss` | `#cbd5e1` | 446, 555 | 2 |
| `pages/nacha-config-variants-fields-page.component.scss` | `#fff7f7` | 506 | 1 |
| `pages/nacha-config-variants-fields-page.component.scss` | `#fef3c7` | 541 | 1 |
| `pages/nacha-config-variants-fields-page.component.scss` | `#92400e` | 542 | 1 |
| `pages/nacha-config-variants-fields-page.component.scss` | `#fee2e2` | 546 | 1 |
| `pages/nacha-config-variants-fields-page.component.scss` | `box-shadow: 0 1px 2px rgba(15, 23, 42, .04)` | 21 | 1 |
| `pages/nacha-config-variants-fields-page.component.scss` | `box-shadow: 0 1px 3px rgba(15, 23, 42, .12)` | 350 | 1 |

Para las sombras, los tokens `--shadow-sm/md/lg` actuales no son equivalentes exactos a esas declaraciones completas; por ello son C, no A.

### D. `INTENTIONAL/OTHER`

En este inventario se clasifican como `OTHER`, sin inferir intención de producto, los valores locales de geometría/composición que no forman parte del cierre cromático/token tratado: tamaños, gaps, paddings, breakpoints, porcentajes, `transparent`, `color-mix(...)` ya parametrizado por tokens, etc.

No se convierten en branding configurable y no se propone crear/redefinir tokens para ellos.

## 3. Branding runtime

Se confirma que permanece el estado:

`4C3C0AR_BRANDING_RUNTIME_STATIC_CLOSED_VALIDATION_PENDING`

Evidencia de continuidad:

- el baseline v119 no cambió ningún archivo preexistente respecto de v118; solo agregó el reporte 4C.3C0as;
- por tanto, todo `web/ach-interbank-ui/src` usado por el cierre 4C.3C0ar permanece byte-a-byte intacto;
- no se amplió el contrato administrable ni se convirtió `DESIGN_TOKEN_STATIC` en branding runtime.

Resultado consolidado:

```text
branding runtime bypasses = 0
```

Los residuales C de `nacha-config-admin` son estilos/tintes estáticos sin token exacto actual; su existencia **no** demuestra un bypass del contrato administrable de branding.

## 4. Branding authorization

Se confirma la clasificación vigente:

`ALIGNED_BY_MAPPING`

El contrato efectivo sigue siendo:

```text
Frontend /users/branding
  roleGuard
  permissionGuard
  roles: Admin
  permissions: CanManageUsers

Backend
  GET api/users/branding: AllowAnonymous
  PUT api/users/branding: Authorize Policy = CanManageAch
```

El mapping actual sigue co-concediendo `CanManageUsers` y `CanManageAch` al rol `Admin`; `ACH.Operator` conserva `CanManageAch` sin `CanManageUsers`. No existe equivalencia bidireccional entre permisos, pero tampoco queda una pregunta sin resolver desde código para este contrato.

```text
authorization unresolved = 0
```

No aparece nueva evidencia que justifique proponer un cambio de permisos.

## 5. Próximo micro-cambio mecánico recomendado — NO implementado

Como `SAFE_EXACT_TOKEN > 0`, el único próximo micro-cambio recomendado es:

**Archivo único:**

`web/ach-interbank-ui/src/app/features/nacha-config-admin/pages/nacha-config-profiles-page.component.scss`

**Sustituir exclusivamente las tres declaraciones de líneas 21, 66 y 72:**

```scss
background: #fff;
```

por:

```scss
background: var(--color-surface);
```

Justificación: `--color-surface` existe actualmente, su default es exactamente `#ffffff`, y las tres ocurrencias son semánticamente backgrounds de superficie. No requiere crear/redefinir tokens ni tocar branding runtime.

No se recomienda ningún otro cambio en este checkpoint.

## 6. ¿Puede cerrarse estáticamente 4C.3C0 tal como está?

**NO.** Quedan **3 ocurrencias `SAFE_EXACT_TOKEN`** demostradas y mecánicamente sustituibles con un token actual exacto.

Los valores `FALLBACK_ONLY` no son objetivo de eliminación automática. Los `NO_EXACT_TOKEN` tampoco deben aproximarse ni provocar creación/redefinición de tokens dentro de este cierre.

## 7. Validaciones de inmutabilidad / arquitectura

La salida de este micro-lote agrega únicamente este reporte. Frente al baseline v119:

```text
UI source diff = 0
backend diff = 0
routing diff = 0
feature -> feature = 0
core -> features = 0
shared -> features = 0
branding runtime bypasses = 0
/users/branding intacta
api/users/branding intacto
```

Se auditó un manifiesto de **29 archivos de routing** del SPA y no existe modificación de routing en la salida.

No se tocó ningún source, SCSS/CSS, HTML/TS/spec, `BrandingService`, token, routing/guard/permiso, backend, contrato ni config.

## 8. Angular

Se comprobó exclusivamente:

`web/ach-interbank-ui/node_modules/.bin/ng`

Resultado exacto:

`ng: not found`

No se ejecutó `npm ping`, `npm ci`, registry retries, build ni tests. **No se declara VERIFIED.**

## Estado

`4C3C0AT_FINAL_STATIC_CLOSURE_INVENTORY_COMPLETE_VALIDATION_PENDING`
