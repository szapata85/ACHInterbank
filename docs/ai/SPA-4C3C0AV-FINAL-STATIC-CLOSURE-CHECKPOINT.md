# SPA 4C.3C0av — 4C.3C0 Final Static Closure Checkpoint

## Tipo de checkpoint

Checkpoint **READ-ONLY** final de 4C.3C0 orientado únicamente a detectar regresiones respecto de los checkpoints anteriores y determinar si la fase puede declararse `STATIC_CLOSED`. No se implementó ningún cambio en source, SCSS/CSS, HTML/TS/specs, facade, `BrandingService`, tokens, routing/guards/permisos, backend, contratos ni configuración. El único artefacto agregado al baseline por este micro-lote es este reporte.

## Baseline e integridad

- Baseline: `ACHInterbank-v121-spa-4C3C0au-nacha-config-profiles-surface-token.zip`
- SHA-256 esperado: `7cb98abfd7c4a61aa59c9c62742516b68f002077882d9911e36c6c0cbd04a6a9`
- SHA-256 observado: `7cb98abfd7c4a61aa59c9c62742516b68f002077882d9911e36c6c0cbd04a6a9`
- Integridad: **coincide**.

Como control de continuidad, la comparación de v121 contra v120 confirma exactamente el micro-cambio 4C.3C0au ya documentado: **1 archivo source modificado** (`nacha-config-profiles-page.component.scss`) con las 3 sustituciones `background: #fff;` → `background: var(--color-surface);`, **1 reporte agregado**, 0 archivos removidos y ninguna otra modificación.

## Resumen final consolidado

```text
orphans pendientes = 0
feature -> feature = 0
core -> features = 0
shared -> features = 0
SAFE_EXACT_TOKEN pendientes = 0
FALLBACK_ONLY = 13
NO_EXACT_TOKEN = 41
branding runtime bypasses = 0
authorization unresolved = 0
regresiones nuevas encontradas = 0
```

Los criterios exigidos para cierre estático están todos en cero. Los `FALLBACK_ONLY = 13` y `NO_EXACT_TOKEN = 41` permanecen como inventario estático conocido y **no bloquean** el cierre porque no constituyen por sí mismos deuda estructural ni existe token actual exacto demostrado para convertirlos.

## 1. Ownership / estructura de `nacha-config-admin`

El feature conserva exactamente **22 archivos**: **17 productivos** y **5 specs**, sin adiciones ni eliminaciones respecto del inventario previo.

### Archivos productivos actuales

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

Los 5 specs siguen correspondiendo a las cuatro páginas y al catálogo de presentación. El spec de `NachaConfigApiService` permanece en `core/services`, como ya estaba documentado.

### Orphans / residuos

```text
orphans pendientes = 0
```

No reaparecieron los residuos previamente eliminados/auditados:

- `nacha-config-official-readonly-page.component.scss` = ausente;
- `nacha-config-official-readonly-page.component.spec.ts` = ausente;
- `nacha-config-state.service.ts` = ausente.

Las cuatro páginas siguen referenciadas por routing/módulo según su modalidad, el catálogo de presentación continúa consumido por `nacha-config-variants-fields-page.component.ts`, y los services `NachaConfigQueryService`/`NachaConfigCommandService` conservan consumidores productivos en las páginas del feature.

### Facade documentada sin call sites

La superficie pública documentada previamente sin call sites productivos permanece intacta. No se toma su falta de call site como evidencia suficiente de orphan y **no debe eliminarse** dentro de este cierre sin evidencia adicional.

En particular, no se abrió ningún frente de refactor sobre los métodos previamente inventariados de `NachaConfigQueryService` ni sobre `NachaConfigCommandService.preview`.

### Imports / dependencias

Scan estático de imports TypeScript relativos resolubles bajo `web/ach-interbank-ui/src/app`:

```text
feature -> feature = 0
core -> features = 0
shared -> features = 0
```

No se detectó regresión de ownership entre capas/features.

## 2. `nacha-config-admin` — cierre de hardcodes clasificados

La precondición usada por 4C.3C0au sigue intacta en `_tokens.scss`:

```scss
--color-surface: #ffffff;
```

En `pages/nacha-config-profiles-page.component.scss` las tres posiciones previamente clase A permanecen como:

```scss
background: var(--color-surface);
```

en las líneas 21, 66 y 72 del baseline actual. El literal exacto:

```scss
background: #fff;
```

aparece **0 veces** en ese archivo.

Por continuidad con el inventario 4C.3C0at y el diff exacto 4C.3C0au:

```text
SAFE_EXACT_TOKEN pendientes = 0
FALLBACK_ONLY = 13
NO_EXACT_TOKEN = 41
```

El scan actual de SCSS productivo de `nacha-config-admin` confirma **52 ocurrencias hex residuales**: 13 son fallbacks dentro de `var(...)` y 39 son hex directos ya clasificados `NO_EXACT_TOKEN`. Además permanecen las mismas 2 sombras RGBA compuestas sin token exacto, dando `NO_EXACT_TOKEN = 39 + 2 = 41`.

No se creó ni redefinió ningún token, no se modificó ningún `FALLBACK_ONLY`, ningún `NO_EXACT_TOKEN`, ninguna sombra RGBA ni se convirtió `DESIGN_TOKEN_STATIC` en branding configurable.

## 3. Branding runtime

Se mantiene el estado previo:

`4C3C0AR_BRANDING_RUNTIME_STATIC_CLOSED_VALIDATION_PENDING`

Los archivos que materializan el contrato de branding (`branding.model.ts`, `BrandingService`, `BrandingSettingsComponent`, layouts público/privado, `_tokens.scss` y `_components.scss`) son byte-a-byte iguales a los del checkpoint 4C.3C0ar.

### Seis campos administrables

`BrandingSettings` continúa exponiendo exactamente:

- `publicLogo`
- `privateLogo`
- `publicBackground`
- `privateBackground`
- `sidebarBackground`
- `buttonColor`

La pantalla `/users/branding` continúa construyendo el payload con esos seis campos. `BrandingService` mantiene `GET/PUT api/users/branding`, deriva los runtime tokens de `buttonColor` y `sidebarBackground`, y aplica `publicBackground`, `privateBackground` y `sidebarBackground`; los logos continúan con binding directo en los layouts público/privado.

### Scan de regresión de bypasses

Se revalidaron únicamente patrones de regresión ya conocidos del cierre runtime:

- hardcodes primary default `#2563eb` / `#1d4ed8` en estilos fuera de `_tokens.scss`: **0**;
- foco antiguo `rgba(37, 99, 235, ...)`: **0**;
- scrollbar sidebar fijo `rgba(226, 232, 240, .28/.48)`: **0**;
- los 3 consumidores legacy de `--button-color` siguen usando `color: var(--color-on-primary)`;
- `main-layout` conserva scrollbar/foreground derivados de `--color-on-sidebar`;
- login/main layout conservan consumo de `--brand-public-background` / `--brand-private-background` y los logos directos.

Resultado:

```text
branding runtime bypasses = 0
```

No se identificó una regresión nueva ni se amplió el contrato administrable.

## 4. Branding authorization

Se mantiene la clasificación:

`ALIGNED_BY_MAPPING`

Los archivos de routing/guards, `BrandingController`, configuración de permisos/roles, policy `CanManageAch`, emisión de claims y configuración de menú permanecen byte-a-byte iguales al checkpoint de autorización 4C.3C0as.

Contrato actual revalidado:

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

El mapping actual sigue co-concediendo `CanManageUsers` y `CanManageAch` al rol `Admin`, mientras `ACH.Operator` recibe `CanManageAch` sin `CanManageUsers`. No existe equivalencia bidireccional entre ambos permisos, pero no aparece nueva evidencia que invalide la clasificación vigente ni que justifique proponer cambios de guards/policies/permisos.

```text
authorization unresolved = 0
```

## 5. Rutas / backend / continuidad contra checkpoints anteriores

Comparación de contenido entre el checkpoint 4C.3C0ar (v118) y el baseline actual v121:

- UI source: 649 archivos en ambos; **1 archivo modificado**, exclusivamente `nacha-config-profiles-page.component.scss`, correspondiente al micro-cambio 4C.3C0au ya aprobado;
- backend `src/`: **1474 archivos / diff 0**;
- routing frontend: **29 archivos / diff 0**;
- file list de `nacha-config-admin`: **22 → 22**, sin altas/bajas; solo el SCSS de profiles difiere por los 3 reemplazos esperados.

Además, los archivos específicos de `/users/branding`, `BrandingService`, guards, permisos/roles/policies, menú y `BrandingController` mantienen hashes idénticos entre v118/v119/v121.

Por tanto:

```text
rutas públicas intactas = sí
backend intacto = sí
/users/branding intacta = sí
api/users/branding intacto = sí
regresiones nuevas encontradas = 0
```

No se abrió ningún frente nuevo de refactor ni se inventó contrato, token, permiso o comportamiento.

## 6. Criterio de cierre

| Criterio | Valor | Cumple |
|---|---:|---|
| orphans | 0 | Sí |
| feature -> feature | 0 | Sí |
| core -> features | 0 | Sí |
| shared -> features | 0 | Sí |
| SAFE_EXACT_TOKEN pendientes | 0 | Sí |
| branding runtime bypasses | 0 | Sí |
| authorization unresolved | 0 | Sí |
| regresiones nuevas | 0 | Sí |

`FALLBACK_ONLY = 13` y `NO_EXACT_TOKEN = 41` no impiden el cierre conforme al criterio explícito del lote.

### Declaración de cierre estático

`4C3C0_STATIC_CLOSED_VALIDATION_PENDING`

Este estado es un cierre **estático** con validación de ejecución pendiente; no equivale a `VERIFIED`.

## 7. Validaciones de inmutabilidad del micro-lote 4C.3C0av

El ZIP de salida se construye a partir del baseline v121 agregando únicamente este reporte. La validación de manifiesto confirma:

```text
UI source diff = 0
backend diff = 0
routing diff = 0
feature -> feature = 0
core -> features = 0
shared -> features = 0
```

No se modifica ningún archivo preexistente del baseline.

## 8. Angular

Se comprobó exclusivamente:

`web/ach-interbank-ui/node_modules/.bin/ng`

Resultado exacto:

`ng: not found`

No se ejecutó `npm ping`, `npm ci`, registry retries, build ni tests. **No se declara VERIFIED.**

## Estado final

`4C3C0_STATIC_CLOSED_VALIDATION_PENDING`

No se avanza al siguiente lote.
