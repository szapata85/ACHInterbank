# SPA 4C.3A.1 — Alias frontend retirement checkpoint

**Estado:** `ALIAS_FRONTEND_RETIREMENT_IMPLEMENTED_VALIDATION_PENDING`

## Baseline

- Baseline de trabajo: `ACHInterbank-v25-nacha-security-4C2.zip`.
- Objetivo de este lote: retirar exclusivamente la feature frontend funcional Alias (`/aliases`).
- Fuera de alcance y no modificados: backend, seeds, migrations, permisos backend, `ach-responses`, NACHA Security y engine NACHA-M.
- No se creó reemplazo para `/aliases`; la URL retirada queda cubierta por el wildcard existente `** -> not-found`.

## Archivos eliminados

Se eliminó por completo `web/ach-interbank-ui/src/app/features/aliases` (11 archivos):

- `web/ach-interbank-ui/src/app/features/aliases/aliases-routing.module.ts`
- `web/ach-interbank-ui/src/app/features/aliases/aliases.module.ts`
- `web/ach-interbank-ui/src/app/features/aliases/components/alias-form.component.html`
- `web/ach-interbank-ui/src/app/features/aliases/components/alias-form.component.scss`
- `web/ach-interbank-ui/src/app/features/aliases/components/alias-form.component.ts`
- `web/ach-interbank-ui/src/app/features/aliases/components/aliases-list.component.html`
- `web/ach-interbank-ui/src/app/features/aliases/components/aliases-list.component.scss`
- `web/ach-interbank-ui/src/app/features/aliases/components/aliases-list.component.spec.ts`
- `web/ach-interbank-ui/src/app/features/aliases/components/aliases-list.component.ts`
- `web/ach-interbank-ui/src/app/features/aliases/models/alias.model.ts`
- `web/ach-interbank-ui/src/app/features/aliases/services/aliases-api.service.ts`

## Archivos modificados / agregados

- `web/ach-interbank-ui/src/app/app-routing.module.ts`
  - retirada la lazy route `aliases -> AliasesModule`.
- `web/ach-interbank-ui/src/app/app-routing-access.spec.ts`
  - retiradas referencias a `ALIASES_ROUTES` / `CanManageAliases`;
  - agregada aserción de ausencia de la ruta y cobertura por el wildcard de `not-found`.
- `web/ach-interbank-ui/src/app/core/services/navigation.service.ts`
  - `/aliases` se trata como ruta frontend retirada y se filtra del menú runtime aunque el backend aún la entregue.
- `web/ach-interbank-ui/src/app/core/services/navigation.service.spec.ts`
  - agregado caso de regresión que verifica que un item backend `/aliases` no llega al menú visible.
- `web/ach-interbank-ui/src/app/features/navigation/components/navigation-menu.component.ts`
  - retirada la etiqueta frontend `CanReadAliases: 'Consultar alias'` que quedó huérfana al retirar la feature.
- `web/ach-interbank-ui/src/app/features/navigation/services/navigation-menu.service.ts`
  - `/aliases` también se filtra de la administración/configuración frontend del menú, sin modificar seeds backend.
- `web/ach-interbank-ui/src/app/features/navigation/services/navigation-menu.service.spec.ts` (nuevo)
  - regresión focal para el filtrado de `/aliases` en la administración del menú.
- `web/ach-interbank-ui/e2e/spa-routes-smoke.spec.ts`
  - retirada `/aliases` del conjunto de rutas funcionales;
  - incorporada `/aliases` al conjunto que debe terminar en `not-found`;
  - retirados items Alias de fixtures de navegación visibles.
- `docs/ai/SPA-4C3A1-ALIAS-FRONTEND-RETIREMENT-CHECKPOINT.md` (este documento).

## Búsquedas de residuos

Búsquedas ejecutadas sobre el frontend después del cambio:

```text
rg "path:\s*'aliases'|features/aliases/aliases\.module|AliasesModule|ALIASES_ROUTES" web/ach-interbank-ui/src/app
=> 0 archivos / 0 coincidencias

rg "AliasesApiService|api/aliases" web/ach-interbank-ui
=> 0 archivos / 0 coincidencias

rg "CanReadAliases|CanManageAliases" web/ach-interbank-ui
=> 0 archivos / 0 coincidencias

web/ach-interbank-ui/src/app/features/aliases
=> ABSENT
```

Las referencias textuales que permanecen a `/aliases` son únicamente controles de retiro y pruebas negativas: filtros de navegación, aserciones de no exposición y verificación de `not-found`. No existe una ruta funcional, módulo, componente ni cliente API Alias asociado a esas referencias.

## Alias técnico NACHA-M: intacto

Se verificó explícitamente que **Alias funcional** no se confundió con los aliases técnicos NACHA-M. Se mantienen presentes, entre otros:

- `INachaType7AliasMap`
- `NachaType7AliasMap`
- `NachaType7FieldValueResolver`
- `NachaCanonicalMapper`
- canonical mappings y lógica de resolución del engine NACHA-M

Además, la comparación recursiva contra el baseline no reportó diferencias en `src`, `database`, `tests` ni `web/ach-interbank-ui/src/app/features/nacha-security`.

Hashes SHA-256 de archivos técnicos clave, comparados contra el baseline, permanecen idénticos:

```text
INachaType7AliasMap.cs                                      73bc9cf0c8beb982b20108c4c9789f24b9d98ceb0124f0b24efbb0ac84e6726b  MATCH
INachaType7FieldValueResolver.cs                            8e0eef82be7c7a197bf1a188ed95f09e5171447efdc4f129de2f631187ffb569  MATCH
NachaType7AliasMap.cs                                       89c75e2001f949491096e058f29a993429ee31403334af0a8b11f470a720e68e  MATCH
NachaType7FieldValueResolver.cs                             63599ee61a2f6bfd0272526b1ff468e710a6597b17799342619f5895e8fbc17d  MATCH
NachaCanonicalMapper.cs                                     e05317977c2fb6752ea6be8ca8ec0705ffd1f7711e5ae3917b527bbd85e26f91  MATCH
```

No se ejecutaron suites backend ni migraciones.

## CanReadAliases / CanManageAliases — pendiente para 4C.3A.2

- En frontend ya no quedan referencias funcionales a `CanReadAliases` ni `CanManageAliases`.
- `CanReadAliases` **permanece en backend** (por ejemplo en `PermissionConfiguration` y snapshots/migrations) y fue dejado intacto deliberadamente en este lote.
- `CanManageAliases` no aparece como permiso backend definido en el código de configuración inspeccionado; sí existe en documentación histórica del frontend. Su reconciliación/eliminación, junto con el tratamiento definitivo de `CanReadAliases`, queda expresamente pendiente para **4C.3A.2**.
- No se modificaron seeds, snapshots, migrations ni asignaciones de permisos backend.

## Validación focal y tests realmente ejecutados

Validaciones estáticas/focales completadas:

- ausencia de lazy route `/aliases`: **OK**;
- ausencia de `AliasesApiService`: **OK**;
- ausencia de referencias frontend a `api/aliases`: **OK**;
- ausencia de imports/referencias a `AliasesModule` / `ALIASES_ROUTES`: **OK**;
- ausencia del directorio de feature Alias: **OK**;
- aliases técnicos NACHA-M presentes e idénticos al baseline: **OK**;
- backend / migrations / NACHA Security sin cambios: **OK**.

Tests frontend ejecutados con runner: **ninguno pudo iniciar** por bloqueo real del entorno.

Intento de preparación de dependencias:

```text
npm ci --prefer-offline --no-audit --no-fund
=> bloqueado por resolución de red del registry npm; log con EAI_AGAIN para registry.npmjs.org.
```

Intento del conjunto focal:

```text
npm test -- --watch=false --browsers=ChromeHeadless \
  --include=src/app/app-routing-access.spec.ts \
  --include=src/app/core/services/navigation.service.spec.ts \
  --include=src/app/features/navigation/services/navigation-menu.service.spec.ts

=> sh: 1: ng: not found
=> exit 127
```

El `node_modules` parcial generado por el intento fallido de `npm ci` fue eliminado y no forma parte del entregable. No se simularon resultados de test ni build.

## Alcance retenido

- No se migró `ach-responses`.
- No se tocó backend Alias ni se creó backend nuevo.
- No se tocaron seeds, migrations ni permisos backend.
- No se tocó NACHA Security.
- No se tocaron aliases técnicos NACHA-M ni canonical mappings.
- No se avanzó a 4C.3A.2.

## Estado final

`ALIAS_FRONTEND_RETIREMENT_IMPLEMENTED_VALIDATION_PENDING`
