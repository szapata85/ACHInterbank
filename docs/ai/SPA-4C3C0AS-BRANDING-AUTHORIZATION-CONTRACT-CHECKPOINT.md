# SPA 4C.3C0as — Branding Authorization Contract Checkpoint

## Tipo de checkpoint

READ-ONLY para determinar el contrato efectivo de autorización de la administración de branding. No se implementaron cambios en frontend, backend, routing, guards, policies, roles, permissions, `BrandingController`, `BrandingService`, DTO de branding, tests, configuración ni estilos.

## Baseline

- ZIP: `ACHInterbank-v118-spa-4C3C0ar-branding-runtime-static-closure-final.zip`
- SHA-256 esperado: `c450b29ecc156cd8ba2033603aff70c43fcaf274617ac420dc6dbf9a48adc61e`
- SHA-256 observado: `c450b29ecc156cd8ba2033603aff70c43fcaf274617ac420dc6dbf9a48adc61e`
- Resultado de integridad del baseline: coincide.

## Contrato actual confirmado

| Capa | Superficie | Regla efectiva actual |
|---|---|---|
| Frontend | `/users/branding` | `roleGuard` + `permissionGuard` heredados del route padre del módulo Admin |
| Frontend | roles | `Admin` |
| Frontend | permissions | `CanManageUsers` |
| Backend | `GET api/users/branding` | `[AllowAnonymous]` |
| Backend | `PUT api/users/branding` | `[Authorize(Policy = "CanManageAch")]` |

Por tanto, los valores solicitados quedan confirmados exactamente:

- Frontend `/users/branding`: `roleGuard`, `permissionGuard`, roles `Admin`, permissions `CanManageUsers`.
- Backend `GET api/users/branding`: `AllowAnonymous`.
- Backend `PUT api/users/branding`: `Authorize Policy = CanManageAch`.

## Frontend

### Routing de `/users/branding`

`web/ach-interbank-ui/src/app/app-routing.module.ts` expone el grupo `users` mediante lazy loading del `AdminModule` bajo el `MainLayoutComponent`, cuyo route padre usa `authGuard` (líneas 55-66).

`web/ach-interbank-ui/src/app/features/admin/admin-routing.module.ts` define un route padre vacío con:

- `canActivate: [roleGuard, permissionGuard]` (línea 16);
- `roles: ['Admin']` (línea 18);
- `permissions: ['CanManageUsers']` (línea 19).

El child `branding` usa `BrandingSettingsComponent` y no redefine guards, roles ni permissions (líneas 45-47). Por estructura de routing, la autorización aplicable a `/users/branding` es la del padre Admin.

### Semántica de guards

`roleGuard` permite si el usuario autenticado tiene al menos uno de los roles declarados; de lo contrario redirige a `/unauthorized` (`core/guards/role.guard.ts`, líneas 5-19).

`permissionGuard` permite si el usuario autenticado tiene al menos uno de los permisos declarados; de lo contrario redirige a `/unauthorized` (`core/guards/permission.guard.ts`, líneas 5-19).

`AuthService.hasRole()` y `AuthService.hasPermission()` usan `some(...)`, es decir OR dentro de una lista (`core/services/auth.service.ts`, líneas 120-134). En este route cada lista contiene un único valor, por lo que se exigen efectivamente `Admin` y `CanManageUsers`.

### Visibilidad de navegación hacia branding

Existe navegación dinámica para la ruta. `MenuItemConfiguration` registra el item:

- label: `Identidad y colores`;
- route: `/users/branding`;
- parent: `Usuarios`.

Aunque la pantalla usa el título/breadcrumb `Identidad visual`, el texto sembrado del menú es `Identidad y colores` (`Persistence/Configuration/MenuItemConfiguration.cs`, líneas 99-109).

Ese item de menú está asociado a:

- rol `Admin` (`MenuItemRoleConfiguration.cs`, líneas 94-98);
- permiso `CanManageUsers` (`MenuItemPermissionConfiguration.cs`, líneas 54-58).

El backend que construye el menú exige simultáneamente el rol requerido y el permiso requerido (`GetMenuForCurrentUserHandler.ShouldInclude`, líneas 90-105). El frontend consume `api/navigation/menu` y no reintroduce rutas filtradas. Por tanto, la visibilidad del acceso de menú a branding está alineada con `Admin + CanManageUsers`.

### `BrandingSettingsComponent`

No contiene un check adicional de autorización por rol o permiso. `save()` construye el payload y llama directamente `BrandingService.updateBranding(...)` (`branding-settings.component.ts`, líneas 182-208). El botón `Guardar cambios` está presente sin condición de rol/permiso y solo se deshabilita por `isSaving` (`branding-settings.component.html`, líneas 149-154).

`BrandingService.updateBranding()` ejecuta `PUT api/users/branding`; `refreshFromServer()` ejecuta `GET api/users/branding` (`core/services/branding.service.ts`, líneas 34-40 y 53-61).

## Backend

### `BrandingController`

`src/Cfa.ACHInterbank.Api/Controllers/BrandingController.cs` mantiene:

- route base `[Route("api/users/branding")]` (línea 9);
- GET con `[AllowAnonymous]` (líneas 22-24);
- PUT con `[Authorize(Policy = "CanManageAch")]` (líneas 33-35).

No hay autorización adicional en el controller para `CanManageUsers`.

### Definición actual de `CanManageAch`

`PermissionConfiguration` define `CanManageAch` como permiso separado con descripción `Gestión completa de operaciones ACH` (`PermissionConfiguration.cs`, líneas 39-44).

`DependencyInjectionService` registra la policy `CanManageAch` como una policy de claim exacta:

`RequireClaim("permission", "CanManageAch")` (`DependencyInjectionService.cs`, líneas 123-127).

No existe en esa policy expansión a `CanManageUsers`, rol `Admin` ni otra equivalencia.

### Definición actual de `CanManageUsers`

`PermissionConfiguration` define `CanManageUsers` como permiso separado con descripción `Gestión de usuarios` (`PermissionConfiguration.cs`, líneas 57-62).

En el registro de autorización no existe una policy legacy llamada `CanManageUsers`. En el flujo revisado, `CanManageUsers` se usa como claim/permission para guards y menú frontend, no como alias backend de `CanManageAch`.

## Mapping efectivo de roles y claims

`RolePermissionConfiguration` siembra el rol `Admin` con ambos permisos:

- `Admin -> CanManageAch` (líneas 24-28);
- `Admin -> CanManageUsers` (líneas 39-43).

También siembra:

- `ACH.Operator -> CanManageAch` (líneas 49-53);
- no existe `ACH.Operator -> CanManageUsers` en esa configuración.

`AuthService.BuildAuthResult()` del backend construye los claims `permission` del JWT tomando la unión de `RolePermissions` de los roles asignados al usuario y emitiendo un claim por permiso (`Application/Services/Authentication/Implementations/AuthService.cs`, líneas 203-225).

Esto es el punto de alineación actual: no hay una implicación semántica entre permisos; hay un co-grant por mapping del rol `Admin`.

## Respuestas del checkpoint

### 1. ¿`CanManageUsers` implica `CanManageAch` en alguna capa?

**No como policy, claim o regla de autorización directa.** No existe una policy que convierta `CanManageUsers` en `CanManageAch`.

**Sí existe una implicación efectiva dentro del mapping sembrado de roles actuales:** el único rol built-in que recibe `CanManageUsers` es `Admin`, y ese mismo rol también recibe `CanManageAch`. Como el JWT deriva sus permission claims desde `RolePermissions`, un usuario producido por el mapping built-in actual que tenga `CanManageUsers` por `Admin` también tendrá `CanManageAch`.

Esta alineación depende del mapping de datos; no es una equivalencia entre los dos permisos.

### 2. ¿`CanManageAch` implica `CanManageUsers`?

**No.** El contraejemplo está en el propio mapping actual: `ACH.Operator` recibe `CanManageAch` pero no `CanManageUsers`.

### 3. ¿Existe alguna policy/claim/role mapping que haga equivalentes ambas reglas?

**No.**

- No existe policy de equivalencia.
- No existe transformación de claims entre ambos nombres.
- El mapping `Admin` co-concede ambos permisos, pero `ACH.Operator` demuestra que la relación no es bidireccional.

Por tanto, las reglas no son equivalentes. Solo hay alineación unidireccional para el conjunto de usuarios que el frontend deja entrar a branding bajo los mappings built-in actuales.

### 4. Escenarios de usuario

| Escenario | Abrir `/users/branding` | Ver item de menú | Guardar (`PUT`) | Evidencia/conclusión |
|---|---:|---:|---:|---|
| `Admin` con mapping built-in actual | Sí | Sí | Sí | `Admin` obtiene `CanManageUsers` y `CanManageAch` |
| `ACH.Operator` built-in | No | No | Sí a nivel de policy backend si invoca directamente el endpoint autenticado | Tiene `CanManageAch`, pero no rol `Admin` ni `CanManageUsers` |
| Principal hipotético `Admin + CanManageUsers` sin `CanManageAch` | Sí por contrato frontend | Sí por contrato de menú | No, 403 | La combinación satisface frontend pero no backend; **no es generada por el mapping built-in actual** |
| Principal con `CanManageAch` pero sin `Admin/CanManageUsers` | No | No | Sí a nivel backend | El caso built-in concreto es `ACH.Operator` |

#### Puede ver/abrir la pantalla pero recibir 403 al guardar

Con el mapping built-in actual, **no se identifica un escenario normal generado por código**: el usuario que satisface `Admin + CanManageUsers` recibe también `CanManageAch`.

La discrepancia de predicados sí permitiría ese 403 si existiera un principal `Admin + CanManageUsers` sin `CanManageAch`, pero el código revisado no genera esa combinación bajo los mappings built-in actuales. No se asume drift manual de base de datos ni configuración externa no demostrada por código.

#### Puede tener `CanManageAch` pero no poder acceder a la pantalla

**Sí.** `ACH.Operator` es el escenario explícito: recibe `CanManageAch` por mapping, pero no cumple el rol `Admin` ni `CanManageUsers`, por lo que el menú se filtra y el route frontend redirige a no autorizado.

### 5. Clasificación

`ALIGNED_BY_MAPPING`

Justificación:

- El frontend y el PUT backend usan permisos distintos, por lo que no están alineados por una misma regla textual.
- No hay equivalencia entre `CanManageUsers` y `CanManageAch`.
- Sin embargo, el mapping built-in actual de `Admin` co-concede ambos y el JWT materializa ambos claims; por ello, el conjunto de usuarios admitido por el frontend para branding queda cubierto por la policy del PUT.
- El sentido inverso no aplica: `CanManageAch` es más amplio porque también lo tiene `ACH.Operator`.
- No se clasifica `INTENTIONAL_DIFFERENCE` porque el código revisado no demuestra intención explícita para mantener reglas distintas.
- No se clasifica `AUTHORIZATION_MISMATCH` porque, con el mapping built-in actual, no se obtiene un usuario normal que pase el frontend de branding y falle el PUT por falta de `CanManageAch`.
- No se clasifica `UNRESOLVED_FROM_CODE` porque el mapping, la emisión de claims y la policy están definidos en código.

## Validaciones de inmutabilidad / arquitectura

La salida agrega únicamente este reporte. Todos los entries preexistentes del baseline permanecen byte-for-byte iguales en contenido.

- UI source diff = 0.
- backend diff = 0.
- routing diff = 0 (29 archivos de routing auditados).
- branding runtime closure permanece intacto: el checkpoint previo `SPA-4C3C0AR-BRANDING-RUNTIME-STATIC-CLOSURE-FINAL.md` y los sources preexistentes de branding no fueron modificados.
- `feature -> feature` = 0 imports cross-feature.
- `core -> features` = 0.
- `shared -> features` = 0.

No se modificaron `BrandingController`, `BrandingService`, DTOs, routing, guards, policies, roles, permissions, tests, config ni SCSS/CSS.

## Angular

Se comprobó exclusivamente:

`web/ach-interbank-ui/node_modules/.bin/ng`

`ng: not found`

No se ejecutó `npm ping`, `npm ci`, registry retries, build ni tests. No se declara VERIFIED.

## Estado

`4C3C0AS_BRANDING_AUTHORIZATION_CONTRACT_ANALYZED_VALIDATION_PENDING`
