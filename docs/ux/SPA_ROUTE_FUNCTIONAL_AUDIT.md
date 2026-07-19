# Auditoría funcional de rutas de la SPA

Fecha de corte: 2026-07-18  
Rama local auditada: `ACH-Interbank-Postgresql`  
Alcance: árbol Angular efectivo, redirecciones, guards, permisos, menú persistido y contrato HTTP usado por cada pantalla. La clasificación describe el estado del *working tree* local; no presupone que una ruta visible sea funcional de extremo a extremo.

## Resultado ejecutivo

- Se localizaron **25 archivos productivos que registran rutas**, **164 declaraciones sintácticas `path`**, **20 redirecciones**, **70 nodos con `canActivate`** y **1 nodo con `canActivateChild`**.
- Al componer los montajes lazy, eliminar los `path: ''` estructurales duplicados y conservar cada patrón parametrizado, resultan **127 patrones URL efectivos**: 107 pantallas o rutas de navegación, 19 redirecciones y 1 wildcard. Las cuatro declaraciones de `AuthModule` no suman patrones efectivos porque ese módulo no está montado.
- Clasificación de los 127 patrones efectivos: **77 `KEEP`**, **15 `FIX`**, **1 `MERGE`**, **20 `REDIRECT`** (incluye el wildcard) y **14 `BLOCKED`**. La fila adicional `REMOVE` representa el único grupo de cuatro definiciones huérfanas, no una URL efectiva ni código ya eliminado.
- El menú dejó de crear opciones hardcodeadas en Angular: `NavigationService` consume `GET api/navigation/menu`; el backend filtra rol, permiso, rutas NACHA legacy y herramientas UAT por ambiente/feature flag.
- Rutas canónicas confirmadas: ciclos `/ach-cycles`, certificados `/nacha-security/certificates`, configuración NACHA-M `/nacha-config-admin/perfiles`, logs `/audit-logs` e integración SOAP `/integraciones/soap-settings`.
- Permanecen bloqueos funcionales relevantes: resolución manual de respuestas, CRUD de homologaciones, conciliación con resolución, scheduler multiinstancia/historial, generador diferencial homologado y generalización completa de catálogos CENIT.
- Los formularios hijos de alias y clientes ahora ejecutan un guard propio de gestión; ya no dependen del permiso de lectura heredado para decidir la navegación.

## Reproducción del inventario

Ejecutar desde la raíz del repositorio:

```powershell
$routeFiles = rg -l 'RouterModule\.forChild|RouterModule\.forRoot|provideRouter' web/ach-interbank-ui/src/app --glob '*.ts' --glob '!*spec.ts'
$paths = 0; $redirects = 0; $guards = 0; $childGuards = 0
foreach ($file in $routeFiles) {
  $paths += (Select-String -Path $file -Pattern '\bpath\s*:').Count
  $redirects += (Select-String -Path $file -Pattern '\bredirectTo\s*:').Count
  $guards += (Select-String -Path $file -Pattern '\bcanActivate\s*:').Count
  $childGuards += (Select-String -Path $file -Pattern '\bcanActivateChild\s*:').Count
}
"files=$($routeFiles.Count) paths=$paths redirects=$redirects canActivate=$guards canActivateChild=$childGuards"
```

Resultado al corte:

```text
files=25 paths=164 redirects=20 canActivate=70 canActivateChild=1
```

La diferencia `164 - 127` corresponde a montajes lazy, wrappers vacíos y rutas hijas vacías que componen la misma URL. Los patrones `:id`, `:token`, `:batchId`, `:fileId`, `:methodCode` y `:mappingSetId` aparecen explícitamente abajo; no fueron absorbidos por un conteo de pantalla padre.

## Convenciones

| Marca | Significado |
|---|---|
| `KEEP` | Ruta y responsabilidad conservadas; no se encontró un defecto dirigido que obligue a cambiarla. |
| `FIX` | Ruta conservada con corrección aplicada en este working tree. |
| `MERGE` | Función integrada bajo una entrada canónica, aunque pueda conservar una URL técnica compatible. |
| `REDIRECT` | URL de compatibilidad; no mantiene implementación paralela. |
| `HIDE` | Ruta/herramienta excluida del menú o del ambiente donde no corresponde. |
| `REMOVE` | Definición sin valor funcional que debe retirarse. |
| `BLOCKED` | No satisface aún el flujo funcional, de seguridad u operación exigido. |

Los guards de Angular son defensa de navegación, no frontera de seguridad. `permissionGuard` y `roleGuard` aceptan **cualquiera** de los valores de su arreglo y redirigen denegaciones autenticadas a `/unauthorized`. La evidencia definitiva de acciones sensibles es la policy del endpoint.

## Matriz completa

### Acceso, shell y estado

| Ruta | Decisión | Canónica | Cámara/capacidad | Guard o permiso | Endpoint principal | Estado inicial | Estado final | Evidencia |
|---|---|---|---|---|---|---|---|---|
| `/` | REDIRECT | `/dashboard` | Global | `authGuard` | Cliente | Redirect de shell | Redirect explícito | `app-routing.module.ts` |
| `/**` | REDIRECT | `/not-found` | Global | `authGuard` dentro del shell | Cliente | Fallback | Fallback conservado | `app-routing.module.ts` |
| `/login` | KEEP | misma | Global | Pública | Autenticación | Funcional | Conservada | `app-routing.module.ts`, `auth.service.ts` |
| `/forgot-password` | KEEP | misma | Global | Pública | Recuperación de acceso | Funcional | Conservada | `app-routing.module.ts` |
| `/reset-password` | KEEP | misma | Global | Pública | Restablecimiento | Funcional | Conservada | `app-routing.module.ts` |
| `/reset-password/:token` | KEEP | misma | Global | Pública | Restablecimiento con token | Funcional | Conservada | `app-routing.module.ts` |
| `/dashboard` | KEEP | misma | Multi-cámara | `authGuard` | Dashboard | Sin defecto dirigido | Conservada | `dashboard.module.ts` |
| `/unauthorized` | FIX | misma | Global | Usuario autenticado | Cliente | Guards podían enviar a login | Denegación autenticada llega a 403 visual | `permission.guard.ts`, `role.guard.ts` |
| `/not-found` | KEEP | misma | Global | `authGuard` | Cliente | Funcional | Destino único de rutas inválidas | `app-routing.module.ts` |
| Definiciones huérfanas de `AuthModule`: `''`, `login`, `forgot-password`, `reset-password/:token` | REMOVE | Rutas públicas anteriores | Global | Ninguno | Cliente | Segundo árbol no montado | Sigue sin montar; retiro pendiente | `features/auth/auth.module.ts`; no hay import de `AuthModule` |

### Usuarios, alias, clientes y navegación

| Ruta | Decisión | Canónica | Cámara/capacidad | Guard o permiso | Endpoint principal | Estado inicial | Estado final | Evidencia |
|---|---|---|---|---|---|---|---|---|
| `/users` | KEEP | misma | Global | `Admin` + `CanManageUsers` | `api/users` | Funcional | Conservada | `admin-routing.module.ts`, `users-api.service.ts` |
| `/users/new` | KEEP | misma | Global | Hereda `Admin` + `CanManageUsers` | `POST api/users` | Funcional | Conservada | `admin-routing.module.ts` |
| `/users/:id/edit` | KEEP | misma | Global | Hereda `Admin` + `CanManageUsers` | `GET/PUT api/users/:id` | Funcional | Conservada | `admin-routing.module.ts` |
| `/users/:id/roles` | KEEP | misma | Global | Hereda `Admin` + `CanManageUsers` | `api/users/:id`, `api/roles` | Funcional | Conservada | `admin-routing.module.ts`, `users-api.service.ts` |
| `/users/branding` | KEEP | misma | Global | Hereda `Admin` + `CanManageUsers` | `GET/PUT api/users/branding` | Funcional | Conservada | `branding.service.ts` |
| `/users/password-rules` | KEEP | misma | Global | Hereda `Admin` + `CanManageUsers` | `GET/PUT api/users/password-rules` | Funcional | Conservada | `password-rules.service.ts` |
| `/users/login-lockout` | KEEP | misma | Global | Hereda `Admin` + `CanManageUsers` | `GET/PUT api/users/login-lockout` | Funcional | Conservada | `login-lockout-settings.service.ts` |
| `/aliases` | KEEP | misma | Global | `CanReadAliases` | `GET api/aliases` | Funcional | Conservada | `aliases-routing.module.ts`, `aliases-api.service.ts` |
| `/aliases/new` | FIX | misma | Global | `CanManageAliases` en guard local | `POST api/aliases` | Guard de lectura del padre abría el formulario | Navegación de creación exige gestión | `ALIASES_ROUTES`, prueba de acceso/compilación dirigida pendiente de consolidación final |
| `/aliases/:id/edit` | FIX | misma | Global | `CanManageAliases` en guard local | `GET/PUT api/aliases/:id` | Mismo defecto | Navegación de edición exige gestión | `aliases-routing.module.ts` |
| `/customers` | KEEP | misma | Multi-cámara | `CanReadAch` | `GET customers` | Funcional | Conservada | `customers-routing.module.ts`, `customers-api.service.ts` |
| `/customers/new` | FIX | misma | Multi-cámara | `CanManageAch` en guard local | `POST customers` | Guard de lectura del padre abría el formulario | Navegación de creación exige gestión | `CUSTOMERS_ROUTES`, prueba de acceso/compilación dirigida pendiente de consolidación final |
| `/customers/:id/edit` | FIX | misma | Multi-cámara | `CanManageAch` en guard local | `GET/PUT customers/:id` | Mismo defecto | Navegación de edición exige gestión | `customers-routing.module.ts` |
| `/customer-third-parties` | KEEP | misma | Multi-cámara/prenotificación | `authGuard`; menú `CanReadAch`/`CanManageAch` | `GET api/customer-third-parties` | Consulta paginada | Conservada | `customer-third-parties-routing.module.ts`, servicio homónimo |
| `/navigation` | REDIRECT | `/navigation/menu-items` | Global | `Admin` + `CanManageUsers` | Cliente | Wrapper | Redirect conservado | `navigation-routing.module.ts` |
| `/navigation/menu-items` | FIX | misma | Global | `Admin` + `CanManageUsers` | CRUD `api/navigation/menu-items`; runtime `GET api/navigation/menu` | Menú frontend reinyectaba opciones y nginx colisionaba con la ruta SPA | Base persistida es fuente de verdad; filtrado backend y prefijo API aislado | `navigation.service.ts`, `GetMenuForCurrentUserHandler.cs`, `nginx.conf` |

### Auditoría y logs

| Ruta | Decisión | Canónica | Cámara/capacidad | Guard o permiso | Endpoint principal | Estado inicial | Estado final | Evidencia |
|---|---|---|---|---|---|---|---|---|
| `/log` | REDIRECT | `/audit-logs` | Global | `authGuard` | Cliente | Alias duplicado | Redirect | `app-routing.module.ts` |
| `/logs` | REDIRECT | `/audit-logs` | Global | `authGuard`; menú filtrado | Cliente | Alias de menú | Redirect | `app-routing.module.ts` |
| `/audit-logs` | KEEP | misma | Global | `authGuard`; permisos de menú | `GET api/audit-logs` | Paginada | Conservada | `audit-routing.module.ts`, `audit-log.service.ts` |
| `/auth-logs` | KEEP | misma | Global | `authGuard`; permisos de menú | `GET api/auth-logs` | Paginada | Conservada | `auth-logs-routing.module.ts` |
| `/navigation-logs` | KEEP | misma | Global | `authGuard`; permisos de menú | `GET/POST api/navigation-logs` | Paginada | Conservada | `navigation-logs-routing.module.ts`, `navigation-log.service.ts` |

### Catálogos

| Ruta | Decisión | Canónica | Cámara/capacidad | Guard o permiso | Endpoint principal | Estado inicial | Estado final | Evidencia |
|---|---|---|---|---|---|---|---|---|
| `/catalogs` | KEEP | misma | Global | `CanReadCatalogs` | Catálogos | Funcional | Conservada | `catalogs-routing.module.ts` |
| `/catalogs/financial-institutions` | KEEP | misma | Multi-cámara | `CanManageAch` | `financial-institutions` | Funcional | Conservada | `financial-institution-admin.service.ts` |
| `/catalogs/clearing-house-preferences` | KEEP | misma | Por cámara | `CanManageAch` | `institution-clearing-house-preferences` | Funcional | Conservada | servicio homónimo |
| `/catalogs/bank-holidays` | KEEP | misma | Global/calendario | `CanManageAch` | `bank-holidays` | Funcional | Conservada | `bank-holidays-admin.service.ts` |
| `/catalogs/clearing-house-special-dates` | KEEP | misma | Por cámara/calendario | `CanManageAch` | `clearing-house-special-dates` | Funcional | Conservada | servicio homónimo |
| `/catalogs/company-entry-descriptions` | KEEP | misma | Multi-cámara | `CanManageAch` | `company-entry-descriptions` | Funcional | Conservada | `company-entry-descriptions-api.service.ts` |
| `/catalogs/document-types` | KEEP | misma | Global | `CanManageAch` | `catalog-types/document-types` | Funcional | Conservada | `catalog-types-api.service.ts` |
| `/catalogs/gender-types` | KEEP | misma | Global | `CanManageAch` | `catalog-types/gender-types` | Funcional | Conservada | `catalog-types-api.service.ts` |
| `/catalogs/person-types` | KEEP | misma | Global | `CanManageAch` | `catalog-types/person-types` | Funcional | Conservada | `catalog-types-api.service.ts` |
| `/catalogs/phone-types` | KEEP | misma | Global | `CanManageAch` | `catalog-types/phone-types` | Funcional | Conservada | `catalog-types-api.service.ts` |
| `/catalogs/email-types` | KEEP | misma | Global | `CanManageAch` | `catalog-types/email-types` | Funcional | Conservada | `catalog-types-api.service.ts` |
| `/catalogs/address-types` | KEEP | misma | Global | `CanManageAch` | `catalog-types/address-types` | Funcional | Conservada | `catalog-types-api.service.ts` |
| `/catalogs/transaction-codes` | KEEP | misma | ACH | `CanManageAch` | `catalog-types/transaction-codes` | Funcional | Conservada | `catalog-types-api.service.ts` |

### Ciclos y compatibilidad NACHA legacy

| Ruta | Decisión | Canónica | Cámara/capacidad | Guard o permiso | Endpoint principal | Estado inicial | Estado final | Evidencia |
|---|---|---|---|---|---|---|---|---|
| `/ach-cycles` | FIX | misma | ACHCOL/CENIT; `Cycle` | `CanReadAch` | `GET api/ach-cycles`, `GET clearing-houses` | Catálogo separado, duplicado en menú y colisión SPA/API en nginx | Entrada única «Configuración de ciclos», selector de cámara, pestañas y API aislada | `ach-cycle-list.component.*`, `CyclesMenuSeeder.cs`, `nginx.conf` |
| `/ach-cycles/new` | KEEP | misma | Por cámara; `Cycle` | `CanManageAch` | `POST api/ach-cycles` | Funcional | Conservada | `ach-cycles-routing.module.ts` |
| `/ach-cycles/:id/edit` | KEEP | misma | Por cámara; `Cycle` | `CanManageAch` | `GET/PUT api/ach-cycles/:id` | Funcional | Conservada | `ach-cycles-api.service.ts` |
| `/ach-cycles/nacha/export` | KEEP | misma | Por cámara/exportación | `CanReadAch` + backend | `api/ach-cycles/exportable`, `NachaExport/:cycleId` | Funcional | Conservada | `nacha-export-api.service.ts` |
| `/ach-cycles/nacha/layouts` | REDIRECT | `/not-found` | Obsoleto | `authGuard` | Cliente | Motor legacy aún navegable | Bloqueado y retirado del menú | `ach-cycles-routing.module.ts`, `NachaConfigMenuSeeder.cs` |
| `/ach-cycles/nacha/definitions` | REDIRECT | `/not-found` | Obsoleto | `authGuard` | Cliente | Motor legacy aún navegable | Bloqueado y retirado del menú | mismas evidencias |
| `/nacha-layouts` | REDIRECT | `/not-found` | Obsoleto | `authGuard` | Cliente | URL legacy | Redirect explícito | `app-routing.module.ts` |
| `/nacha-record-definitions` | REDIRECT | `/not-found` | Obsoleto | `authGuard` | Cliente | URL legacy | Redirect explícito | `app-routing.module.ts` |

### Transacciones, cargas y reglas

| Ruta | Decisión | Canónica | Cámara/capacidad | Guard o permiso | Endpoint principal | Estado inicial | Estado final | Evidencia |
|---|---|---|---|---|---|---|---|---|
| `/transactions` | REDIRECT | `/transactions/list` | Multi-cámara | `Admin/ACH.Operator` + `CanManageAch` o `CanReadAch` | Cliente | Wrapper | Redirect conservado | `transactions-routing.module.ts` |
| `/transactions/list` | KEEP | misma | Multi-cámara | `CanReadAch` | `GET api/transactions` | Funcional; nginx colisionaba con la ruta SPA | Conservada con API aislada | `transactions-api.service.ts`, `nginx.conf` |
| `/transactions/create` | KEEP | misma | Multi-cámara | `CanManageAch` | `POST api/transactions` | Funcional | Conservada | `transactions-api.service.ts` |
| `/transactions/bulk-create` | KEEP | misma | Multi-cámara | `CanManageAch` | `POST api/transactions/bulk` | Funcional | Conservada | `transactions-api.service.ts` |
| `/transactions/bulk-ingestion/upload` | KEEP | misma | Multi-cámara | `CanManageAch` | `POST api/transactions/bulk-ingestion/upload` | Funcional | Conservada | `bulk-ingestion-tracking-api.service.ts` |
| `/transactions/bulk-ingestion/tracking` | KEEP | misma | Multi-cámara | `CanManageAch` | `GET api/transactions/bulk-ingestion/:batchId` | Funcional | Conservada | `bulk-ingestion-tracking-api.service.ts` |
| `/transactions/bulk-ingestion/:batchId` | KEEP | misma | Multi-cámara | `CanManageAch` | Estado/items/resumen/retry/cancel bajo `api/transactions` | Funcional | Conservada | `bulk-ingestion-tracking-api.service.ts` |
| `/transactions/nacha-upload` | KEEP | misma | Multi-cámara/ingesta formal | `CanManageAch` | `POST NachaUpload/upload`, `GET NachaUpload/records` | Funcional | Conservada; punto formal de carga | `nacha-upload.service.ts` |
| `/transactions/cycle-configs` | MERGE | `/ach-cycles` (pestaña Reglas) | Por cámara; reglas/scheduler | `CanManageAch` | `clearing-house-cycle-configs` | Segunda opción de menú | Implementación separada conservada como pestaña técnica; menú duplicado se deshabilita | `cycle-config-management.component.*`, `CyclesMenuSeeder.cs` |
| `/transactions/clearing-house-rules` | KEEP | misma | Por cámara | `CanManageAch` | `api/clearing-house-transaction-rules` | Funcional | Conservada | servicio homónimo |
| `/transactions/returns` | KEEP | misma | Por cámara; `Return` | `CanManageAch` | `ach-returns/generate-file` | Funcional | Conservada | `ach-returns-api.service.ts` |
| `/transactions/returns-ror` | KEEP | misma | Por cámara; `Return` | `CanManageAch` | `ach-returns/return-of-return/*` | Funcional | Conservada | `ach-returns-api.service.ts` |

### Integraciones

| Ruta | Decisión | Canónica | Cámara/capacidad | Guard o permiso | Endpoint principal | Estado inicial | Estado final | Evidencia |
|---|---|---|---|---|---|---|---|---|
| `/integraciones` | REDIRECT | `/integraciones/mappings` | Global | `Admin/ACH.Operator` + `CanReadAch` o `CanManageAch` | Cliente | Wrapper | Redirect conservado | `integrations-routing.module.ts` |
| `/integraciones/soap-settings` | KEEP | misma | Integración SOAP | Hereda guard del módulo; backend | `GET/PUT api/users/soap-integrations` | Funcional | Canónica | `soap-integration-settings.service.ts` |
| `/integraciones/mappings` | KEEP | misma | Integración SOAP | Hereda guard del módulo; backend | `api/integrations/methods`, `mappingsets` | Funcional | Conservada | `integration-mapping-admin.service.ts` |
| `/integraciones/mappings/:methodCode/:mappingSetId` | KEEP | misma | Integración SOAP | Hereda guard del módulo; backend | `api/integrations/mappingsets/:id/*` | Funcional | Conservada | `integration-mapping-admin.service.ts` |
| `/soap-integrations` | REDIRECT | `/integraciones/soap-settings` | Integración SOAP | Guard del destino | Cliente | Alias histórico | Redirect compatible | `app-routing.module.ts` |

### Seguridad NACHA y certificados

| Ruta | Decisión | Canónica | Cámara/capacidad | Guard o permiso | Endpoint principal | Estado inicial | Estado final | Evidencia |
|---|---|---|---|---|---|---|---|---|
| `/nacha-security` | REDIRECT | `/nacha-security/certificates` | Por cámara/ambiente | `permissionGuard` + `canActivateChild` | Cliente | Entrada ambigua | Certificados como destino | `nacha-security-routing.module.ts` |
| `/nacha-security/dashboard` | REDIRECT | `/nacha-security/certificates` | Por cámara/ambiente | Guard del padre | Cliente | Dashboard decorativo duplicado | Redirect canónico | `nacha-security-routing.module.ts` |
| `/nacha-security/certificates` | FIX | misma | Por cámara/ambiente | `Certificates.Read` o fallback ACH | `api/nacha-security/certificates/management` | Cámara implícita y duplicación conceptual | Única opción visible; selector de cámara/ambiente, inventario y alertas reales | `nacha-certificate-manager.component.*`, `MenuItemConfiguration.cs` |
| `/nacha-security/certificates/:id/versions` | KEEP | misma | Por cámara/ambiente | Gestión certificados o fallback ACH | `.../management/:id/versions` | Funcional | Conservada | `nacha-security-routing.module.ts` |
| `/nacha-security/nacha/generate` | KEEP | misma | Por cámara/firma | `Nacha.Generate` o fallback ACH; backend | Generación NACHA | Funcional | Conservada | `nacha-security-routing.module.ts` |
| `/nacha-security/nacha/generate-encrypted` | KEEP | misma | Por cámara/firma+cifrado | `Nacha.GenerateEncrypted` o fallback ACH; backend | Generación cifrada | Funcional | Conservada | `nacha-security-routing.module.ts` |
| `/nacha-security/digital-envelope/manual-encrypt` | KEEP | misma | Sobre digital | `Envelope.ManualEncrypt` o fallback ACH; backend | `api/nacha-security/digital-envelope/encrypt` | Funcional | Conservada | `sobre-digital.service.ts` |
| `/nacha-security/digital-envelope/manual-decrypt` | KEEP | misma | Sobre digital | `Envelope.ManualDecrypt` o fallback ACH; backend | `api/nacha-security/digital-envelope/decrypt` | Funcional | Conservada | `sobre-digital.service.ts` |
| `/nacha-security/sobre-digital` | KEEP | misma | Sobre digital | Cifrar o descifrar o fallback ACH; backend | `api/nacha-security/digital-envelope/*` | Funcional | Conservada | `nacha-security-routing.module.ts` |

### Respuestas ACH

| Ruta | Decisión | Canónica | Cámara/capacidad | Guard o permiso | Endpoint principal | Estado inicial | Estado final | Evidencia |
|---|---|---|---|---|---|---|---|---|
| `/ach-responses` | FIX | misma | Multi-cámara/respuestas | Angular `CanReadAch`; API `P1.NachaRead`; acciones `P1.NachaGenerate` | `GET api/ach/responses`, `POST .../process`, `POST .../notifications/send` | Contratos/autorización inconsistentes | Bandeja canónica con políticas separadas | `ach-responses-api.service.ts`, `AchResponsesController.cs` |
| `/ach-responses/manual-review` | BLOCKED | misma | Multi-cámara/revisión | `CanReadAch` / `P1.NachaRead` | `GET api/ach/responses` | Solo consulta | Sin caso de uso seguro de resolución, causal, comentario, auditoría y concurrencia | componente de revisión, `AchResponsesController.cs` |
| `/ach-responses/status-mappings` | BLOCKED | misma | Por cámara/mapping | `CanReadAch` / `P1.NachaRead` | `GET api/ach/response-status-mappings` | Solo consulta | Sin CRUD administrativo, unicidad, vigencia ni auditoría de cambio | componente de mappings |
| `/ach-responses/dashboard` | FIX | misma | Multi-cámara/métricas | `CanReadAch` / `P1.NachaRead` | `GET api/ach/responses/dashboard` | Nueve consultas desde Angular | Una agregación backend y un request | `AchResponseRepository.cs`, página dashboard |
| `/ach-responses/:id/notification-attempts` | FIX | misma | Multi-cámara/trazabilidad | `CanReadAch` / `P1.NachaRead` | `GET api/ach/responses/:id/notification-attempts` | Loader podía quedar activo | `finalize` y error visible | página attempts |
| `/ach-responses/:id` | FIX | misma | Multi-cámara/trazabilidad | `CanReadAch` / `P1.NachaRead` | `GET api/ach/responses/:id` | Loader podía quedar activo | `finalize` y error visible | página detail |

### Operación NACHA, conciliación e ingestas

| Ruta | Decisión | Canónica | Cámara/capacidad | Guard o permiso | Endpoint principal | Estado inicial | Estado final | Evidencia |
|---|---|---|---|---|---|---|---|---|
| `/ach` | REDIRECT | `/ach/nacha/operational-dashboard` | Multi-cámara | `Admin/ACH.Operator` + `CanReadAch` | Cliente | Wrapper | Redirect conservado | `nacha-operational-routing.module.ts` |
| `/ach/nacha/operational-dashboard` | KEEP | misma | Multi-cámara/operación | `CanReadAch` | `GET api/ach/nacha/operational/dashboard` | Solo lectura | Conservada | `nacha-operational-readiness.service.ts` |
| `/ach/nacha/operational-dashboard/files/:fileId` | KEEP | misma | Multi-cámara/trazabilidad | `CanReadAch` | `GET api/ach/nacha/operational/files/:fileId` | Solo lectura | Conservada | servicio anterior |
| `/ach/nacha/soap-uat-console` | FIX | misma, solo UAT | UAT/integración SOAP | Angular `CanReadAch`; API `P1.NachaSimulatorRead` + no Production + ambiente UAT | `api/ach/nacha/soap-uat-console/*` | Acceso directo no gobernado por ambiente | Backend responde 404 fuera de UAT y exige permiso fino; menú UAT se oculta | `NachaSoapUatConsoleController.cs`, handler de menú |
| `/ach/reconciliation` | BLOCKED | misma | Multi-cámara/conciliación | `CanReadAch` | `api/ach/reconciliation/dashboard`, `items`, `items/:id` | Consola de consulta | Sin resolución manual, reproceso gobernado ni cierre de diferencias | `ach-reconciliation.service.ts` |
| `/ach/nacha/config-profiles` | REDIRECT | `/nacha-config-admin/perfiles` | Multi-cámara/configuración | Guard del destino | Cliente | Alias técnico | Redirect canónico | `nacha-operational-routing.module.ts` |
| `/incoming-nacha-command-center` | KEEP | misma | Multi-cámara/ingesta | `CanReadAch`; API `P1.CommandCenterRead` | `GET incoming-nacha-command-center/ingestions` | Funcional | Conservada | routing, controller y API service homónimos |
| `/incoming-nacha-command-center/ingestions/:id` | KEEP | misma | Multi-cámara/trazabilidad | `CanReadAch`; `P1.CommandCenterRead` | `GET .../ingestions/:id` | Funcional | Conservada | mismas evidencias |
| `/incoming-nacha-command-center/observability` | KEEP | misma | Multi-cámara/observabilidad | `CanReadAch`; `P1.CommandCenterRead` | `GET .../observability/summary` | Funcional | Conservada | mismas evidencias |
| `/incoming-nacha-command-center/queue` | KEEP | misma | Multi-cámara/dispatch | `CanReadAch`; `P1.CommandCenterRead` | `GET .../queue` | Funcional | Conservada | mismas evidencias |
| `/incoming-nacha-command-center/queue/:id` | KEEP | misma | Multi-cámara/dispatch | Lectura `P1.CommandCenterRead`; retry/unblock/requeue/final con policies propias | `GET .../queue/:id`, `POST .../:action` | Funcional | Frontera backend de cada acción confirmada | `IncomingNachaCommandCenterController.cs` |

### CENIT y generalización por cámara

| Ruta | Decisión | Canónica | Cámara/capacidad | Guard o permiso | Endpoint principal | Estado inicial | Estado final | Evidencia |
|---|---|---|---|---|---|---|---|---|
| `/cenit` | KEEP | misma | CENIT | `Admin/ACH.Operator` + `CanReadAch` | Resumen CENIT | Funcional | Conservada | `cenit-routing.module.ts` |
| `/cenit/regulatorio/causales-devolucion` | FIX | Futuro catálogo genérico por cámara | CENIT; catálogos regulatorios | Hereda `CanReadAch` | `GET api/regulatory-catalogs/return-codes?clearingHouseCode=CENIT` | Lista podía mezclar cámaras | Cliente transporta CENIT y backend filtra en base de datos por código/id de cámara | `CenitRegulatoryApiService`, `RegulatoryCatalogsController.cs`; prueba/compilación dirigida pendiente de consolidación final |
| `/cenit/regulatorio/causales-rechazo` | BLOCKED | Futuro catálogo genérico por cámara | CENIT; catálogos regulatorios | Hereda `CanReadAch` | `GET .../file-rejection-codes` | Vista exclusiva | Falta modelo genérico/capacidad aplicada a menú y ruta | routing y servicio regulatorios |
| `/cenit/regulatorio/politicas-transaccion` | BLOCKED | Futuras políticas genéricas | CENIT; políticas | Hereda `CanReadAch` | `GET .../transaction-type-policies` | Vista exclusiva | Falta generalización por cámara | mismas evidencias |
| `/cenit/regulatorio/politicas-devolucion` | BLOCKED | Futuras políticas genéricas | CENIT; devoluciones | Hereda `CanReadAch` | `GET .../return-policies`, `return-of-return-policies` | Vista exclusiva | Falta generalización por cámara | mismas evidencias |
| `/cenit/regulatorio/politicas-prenotificacion` | BLOCKED | Futuras políticas genéricas | CENIT; prenotificación | Hereda `CanReadAch` | `GET .../prenotification-policies` | Vista exclusiva | Falta generalización por cámara | mismas evidencias |
| `/cenit/operacion/ciclos` | REDIRECT | `/ach-cycles?clearingHouseCode=CENIT` | CENIT; `Cycle` | `cenitCyclesRedirectGuard` | Cliente | Pantalla específica duplicada | UrlTree funcional a catálogo genérico con CENIT preseleccionado | `cenit-routing.module.ts` |
| `/cenit/operacion/cola` | KEEP | misma | Solo CENIT; `Dispatch` | Hereda `CanReadAch`; backend CENIT | `GET api/cenit/queues` | Funcional | Conservada como capacidad exclusiva | `cenit-operations-api.service.ts` |
| `/cenit/operacion/neteo` | KEEP | misma | Solo CENIT; `Netting` | Hereda `CanReadAch`; backend CENIT | `GET api/cenit/net-positions` | Funcional | Conservada como capacidad exclusiva | mismo servicio |
| `/cenit/operacion/optimizacion` | KEEP | misma | Solo CENIT; `Liquidity` | Hereda `CanReadAch`; backend CENIT | `GET api/cenit/optimization-decisions` | Funcional | Conservada como capacidad exclusiva | mismo servicio |
| `/cenit/operacion/devoluciones` | BLOCKED | Futuro módulo genérico de devoluciones | CENIT; `Return` | Hereda `CanReadAch` | `GET api/reports/returns` | Vista específica | No se consolidó con devoluciones multi-cámara | componente de operación CENIT |
| `/cenit/operacion/trazabilidad` | FIX | misma mientras se generaliza | CENIT; trazabilidad | Hereda `CanReadAch` | `GET api/cenit/traceability` | Podía mezclar ACHCOL | Consulta backend filtra relación real con CENIT y causales CENIT | `CenitOperationsController.cs`, pruebas de aislamiento |

### Configuración NACHA-M

| Ruta | Decisión | Canónica | Cámara/capacidad | Guard o permiso | Endpoint principal | Estado inicial | Estado final | Evidencia |
|---|---|---|---|---|---|---|---|---|
| `/nacha-config-admin` | REDIRECT | `/nacha-config-admin/perfiles` | Multi-cámara/configuración | Guard del destino | Cliente | Wrapper | Redirect conservado | `nacha-config-admin-routing.module.ts` |
| `/nacha-config-admin/perfiles` | BLOCKED | misma | Multi-cámara/perfiles | `Admin/ACH.Operator` + `CanReadAch` | `GET api/ach/nacha/config-profiles` | Solo lectura; UX conocida incompleta | Guard corregido; rediseño funcional/publicación no cerrado | routing y `nacha-config-api.service.ts` |
| `/nacha-config-admin/perfiles/:id` | BLOCKED | misma | Multi-cámara/perfil/versión | Mismo guard | `GET api/ach/nacha/config-profiles/:id` | Workspace solo lectura | Publicación/edición gobernada no disponible desde esta SPA | mismas evidencias |
| `/nacha-config-admin/records` | BLOCKED | misma | Multi-cámara/records | Mismo guard | Lectura del perfil oficial | Problemas de jerarquía/tabla reportados | No existe evidencia E2E responsive final | routing y componentes del módulo |
| `/nacha-config-admin/variants-fields` | BLOCKED | misma | Multi-cámara/variantes/campos | Mismo guard | Lectura del perfil oficial | Problemas de jerarquía/tabla reportados | No existe evidencia E2E responsive final | routing y componentes del módulo |

### Simulador UAT

| Ruta | Decisión | Canónica | Cámara/capacidad | Guard o permiso | Endpoint principal | Estado inicial | Estado final | Evidencia |
|---|---|---|---|---|---|---|---|---|
| `/uat` | REDIRECT | `/uat/nacha-inbound-simulator` | Solo UAT/local | Menú omitido en Production o feature no UAT | Cliente | UAT podía aparecer siempre | Redirect interno; backend de menú lo excluye fuera de ambiente autorizado | `GetMenuForCurrentUserHandler.cs`, `uat-routing.module.ts` |
| `/uat/nacha-inbound-simulator` | BLOCKED | misma | ACHCOL/CENIT; simulación | Angular `CanManageAch`; API permisos finos por modo y no Production | `api/uat/nacha-inbound-simulator/*` | Un modo semánticamente ambiguo | Modos explícitos, entrantes corregido y diferencial fail-closed; falta perfil/generador diferencial homologado y E2E formal | componente/servicio UAT, `NachaInboundSimulatorController.cs` |

### Scheduler

| Ruta | Decisión | Canónica | Cámara/capacidad | Guard o permiso | Endpoint principal | Estado inicial | Estado final | Evidencia |
|---|---|---|---|---|---|---|---|---|
| `/scheduler` | REDIRECT | `/scheduler/tasks` | Técnica/multi-cámara | Guard del destino | Cliente | Wrapper | Redirect conservado | `scheduler-routing.module.ts` |
| `/scheduler/tasks` | BLOCKED | misma | Técnica/multi-cámara | `CanManageAch` | CRUD `taskdefinitions` | CRUD básico | Persistencia/ejecución Quartz usa RAM y no acredita clustering, historial, reintentos ni calendario completo | `task-definitions.service.ts`, configuración Quartz |

### Reportes

| Ruta | Decisión | Canónica | Cámara/capacidad | Guard o permiso | Endpoint principal | Estado inicial | Estado final | Evidencia |
|---|---|---|---|---|---|---|---|---|
| `/reports` | KEEP | misma | Multi-cámara | `Admin/ACH.Operator` + `CanReadAch` | Índice de reportes | Funcional | Conservada | `reports-routing.module.ts` |
| `/reports/traceability` | KEEP | misma | Multi-cámara/trazabilidad | Hereda guard del módulo | Reporte de trazabilidad | Funcional | Conservada | `reports-api.service.ts` |
| `/reports/sent` | KEEP | misma | Multi-cámara | Hereda guard | `GET api/reports/transactions/sent` | Funcional | Conservada | `reports-api.service.ts` |
| `/reports/received` | KEEP | misma | Multi-cámara | Hereda guard | `GET api/reports/transactions/received` | Funcional | Conservada | mismo servicio |
| `/reports/returns` | KEEP | misma | Multi-cámara/retornos | Hereda guard | `GET api/reports/returns` | Funcional | Conservada | mismo servicio |
| `/reports/rejections` | KEEP | misma | Multi-cámara/rechazos | Hereda guard | `GET api/reports/rejections` | Funcional | Conservada | mismo servicio |
| `/reports/files` | KEEP | misma | Multi-cámara/archivos | Hereda guard | `GET api/reports/nacha-files` | Funcional | Conservada | mismo servicio |
| `/reports/cycles` | KEEP | misma | Multi-cámara/ciclos | Hereda guard | `GET api/reports/cycles` | Funcional | Conservada | mismo servicio |
| `/reports/audit` | KEEP | misma | Multi-cámara/auditoría | Hereda guard | `GET api/reports/audit` | Funcional | Conservada | mismo servicio |
| `/reports/history` | KEEP | misma | Multi-cámara/histórico | Hereda guard | `GET api/reports/history` | Funcional | Conservada | mismo servicio |
| `/reports/reconciliation` | KEEP | misma | Multi-cámara/conciliación de reporte | Hereda guard | `GET api/reports/reconciliation` | Reporte agregado | Conservado como reporte; no reemplaza la consola operativa bloqueada | mismo servicio |

### Registro de capacidades

| Ruta | Decisión | Canónica | Cámara/capacidad | Guard o permiso | Endpoint principal | Estado inicial | Estado final | Evidencia |
|---|---|---|---|---|---|---|---|---|
| `/payment-rail-capability-registry` | KEEP | misma | Multi-riel/capacidades | `Admin/ACH.Operator` + cualquiera de `CanViewPaymentRailCapabilityRegistry`, `CanManageAch`, `CanReadAch` | Registro de capacidades | Solo lectura | Conservada como fuente de consulta; su aplicación transversal a menú/rutas CENIT sigue incompleta | routing y servicio de capability registry |

## Menú y rutas canónicas

El menú efectivo se construye a partir de `MenuItems`, `MenuItemRoles` y `MenuItemPermissions`; Angular ya no agrega ramas faltantes. El handler backend aplica rol y permiso con semántica `ANY` dentro de cada grupo, exige simultáneamente que se cumplan los grupos de rol y permiso, y elimina rutas legacy. Las herramientas `/uat` se eliminan del resultado si el ambiente es Production o el modo del simulador no es UAT-like.

| Función | Entrada visible esperada | Compatibilidad conservada |
|---|---|---|
| Configuración de ciclos | `/ach-cycles` | `/transactions/cycle-configs` queda como pestaña técnica; `/cenit/operacion/ciclos` redirige con CENIT. |
| Certificados digitales | `/nacha-security/certificates` | `/nacha-security` y `/nacha-security/dashboard` redirigen. |
| Configuración NACHA-M | `/nacha-config-admin/perfiles` | `/nacha-config-admin` y `/ach/nacha/config-profiles` redirigen. |
| Integración SOAP | `/integraciones/soap-settings` | `/soap-integrations` redirige. |
| Auditoría | `/audit-logs` | `/log` y `/logs` redirigen. |
| Simulador NACHA-M | `/uat/nacha-inbound-simulator`, solo ambiente autorizado | `/uat` redirige únicamente cuando el módulo está disponible; el menú se oculta fuera de UAT. |

## Bloqueos que impiden clasificar la SPA como completamente funcional

1. `/ach-responses/manual-review` no dispone de resolución autorizada con causal, comentario, auditoría, idempotencia y control de concurrencia.
2. `/ach-responses/status-mappings` no implementa administración persistente/auditable; es lectura.
3. `/ach/reconciliation` no cierra diferencias ni ofrece resolución/reproceso gobernado.
4. `/uat/nacha-inbound-simulator` bloquea correctamente el modo diferencial porque no existe perfil `RETORNO/ENTRADA` publicado/homologado ni generador diferencial table-driven; por tanto no hay evidencia de carga, correlación, `RegistrarRespuestaTransaccion`, idempotencia y conciliación diferencial.
5. `/scheduler/tasks` no acredita ejecución clusterizada ni el historial operativo requerido.
6. Catálogos/políticas/devoluciones CENIT comunes no se consolidaron todavía bajo una ruta genérica por cámara y capacidades; la causal de devolución ya está aislada por `clearingHouseCode=CENIT`.
7. Las tres pantallas de administración NACHA-M no cuentan con evidencia E2E responsive ni con el flujo administrativo completo solicitado.

Esta matriz no convierte un smoke de renderizado en aprobación funcional. `KEEP` significa que no apareció un defecto dirigido dentro del alcance inspeccionado; las rutas `BLOCKED` requieren implementación y prueba antes de cualquier GO productivo.
