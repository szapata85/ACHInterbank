# NAV1 — Baseline actual de navegación

Programa: `NAV-MENU-IA-001`  
Fase: `NAV1`  
Fecha de caracterización: 2026-10-13  
Fuente de verdad: código contenido en el ZIP full de entrada. No se usó Git ni información externa.

## 1. Alcance ejecutado

NAV1 sólo caracteriza el estado vigente. No reorganiza el menú, no cambia nombres visibles, no modifica permisos/roles productivos y no implementa NAV2.

Se revisaron exclusivamente:

- configuración EF y seeders del menú;
- orden de ejecución de `IDbSeeder`;
- jerarquía, rutas y órdenes actuales;
- rutas SPA relacionadas con navegación;
- roles y permisos usados por menú y rutas;
- órdenes duplicados y rutas duplicadas activas;
- pantallas SPA sin acceso canónico desde el menú;
- overrides de navegación en Angular;
- pruebas existentes de navegación/autorización.

## 2. Pipeline de seeds vigente

`DbInitializer` obtiene todos los `IDbSeeder`, aplica únicamente `OrderBy(seeder => seeder.Order)` y los ejecuta secuencialmente. No existe desempate explícito para seeders con el mismo `Order`; por tanto, el código no fija un orden relativo dentro de cada empate.

Seeders de navegación encontrados:

| `Order` | Seeder | Efecto principal de navegación |
|---:|---|---|
| 0 | `NachaSecurityMenuSeeder` | normaliza `/nacha-security`, certificados y sobre digital |
| 1 | `NachaConfigMenuSeeder` | normaliza `/nacha-config-admin` y sus tres hijos |
| 1 | `OperationalRestrictionsMenuSeeder` | agrega `/operational-restrictions` bajo Catálogos |
| 1 | `UsersMenuSeeder` | normaliza `/users` y agrega `/users/list` |
| 2 | `CyclesMenuSeeder` | normaliza `/ach-cycles`, agrega hijo catálogo y reordena exportación |
| 3 | `IncomingNachaCommandCenterMenuSeeder` | agrega seguimiento de archivos bajo Transacciones |
| 4 | `AchClaimsMenuSeeder` | agrega `/ach-claims` bajo Transacciones |
| 4 | `AchColombiaFileExchangeMenuSeeder` | agrega intercambio de archivos bajo Transacciones |
| 4 | `AchColombiaMftAdministrationMenuSeeder` | agrega `/administracion/mft-ach` bajo Transacciones |
| 4 | `OutgoingTransactionMonitoringSeeder` | agrega monitoreo de salida bajo Transacciones |
| 4 | `ReportsMenuSeeder` | agrega `/reports` como raíz |

Prerequisitos de seguridad relevantes que también participan en el pipeline:

- `ClaimsPermissionSeeder`, `Order = 0`;
- `CfaOperationalTransactionRestrictionsPermissionSeeder`, `Order = 0`.

**Hallazgo NAV1:** hay empates de `Order` tanto entre seeders como entre opciones de menú. En el pipeline no hay un criterio secundario explícito. Los seeders empatados observados son mayormente independientes, pero la reproducibilidad de su orden relativo no está expresada por contrato.

## 3. Inventario efectivo actual del menú

El siguiente inventario combina `MenuItemConfiguration` con las mutaciones actuales de los seeders. Es una fotografía del estado vigente, no una propuesta.

### 3.1 Raíces activas

| Orden | Ruta | Etiqueta actual | Observación |
|---:|---|---|---|
| 1 | `/dashboard` | Panel principal | raíz |
| 2 | `/users` | Usuarios | empate de orden |
| 2 | `/nacha-config-admin` | Configuración NACHA-M | empate de orden |
| 4 | `/ach-cycles` | Configuración de ciclos | tiene hijo con la misma ruta |
| 5 | `/integraciones` | Integraciones | empate de orden; Angular la reescribe visualmente |
| 5 | `/catalogs` | Catálogos | empate de orden |
| 6 | `/transactions` | Transacciones | grupo principal operativo actual |
| 7 | `/navigation/menu-items` | Navegación | empate de orden |
| 7 | `/reports` | Reportes | empate de orden |
| 8 | `/nacha-security` | Seguridad NACHA | grupo con hijos |
| 9 | `/scheduler` | Programador | grupo con hijo |
| 10 | `/logs` | Logs | grupo con hijos |
| 11 | `/uat` | UAT / Simuladores | visibilidad adicionalmente condicionada por ambiente/opción |

### 3.2 Hijos relevantes

**Usuarios (`/users`)**

| Orden | Ruta | Etiqueta actual |
|---:|---|---|
| 1 | `/users/list` | Administrar usuarios |
| 2 | `/users/branding` | Identidad y colores |
| 3 | `/users/password-rules` | Reglas de contraseña |
| 4 | `/users/login-lockout` | Bloqueo de acceso |

**Configuración de ciclos (`/ach-cycles`)**

| Orden | Ruta | Etiqueta actual |
|---:|---|---|
| 1 | `/ach-cycles` | Catálogo de ciclos |
| 2 | `/ach-cycles/nacha/export` | Exportar NACHA |

La raíz y el primer hijo comparten la ruta activa `/ach-cycles`.

**Configuración NACHA-M (`/nacha-config-admin`)**

| Orden | Ruta | Etiqueta actual |
|---:|---|---|
| 1 | `/nacha-config-admin/perfiles` | Perfiles oficiales |
| 2 | `/nacha-config-admin/records` | Registros oficiales |
| 3 | `/nacha-config-admin/variants-fields` | Variantes y campos |

**Catálogos (`/catalogs`)**

| Orden | Ruta | Etiqueta actual |
|---:|---|---|
| 1 | `/catalogs/financial-institutions` | Instituciones financieras |
| 2 | `/catalogs/clearing-house-preferences` | Prioridades cámaras |
| 3 | `/catalogs/bank-holidays` | Festivos bancarios |
| 4 | `/catalogs/clearing-house-special-dates` | Fechas especiales cámaras |
| 5 | `/clearing-houses` | Cámaras compensadoras |
| 6 | `/operational-restrictions` | Restricciones operacionales CFA |

`NavigationService` elimina actualmente `/catalogs/clearing-house-special-dates` antes de renderizar el menú SPA, de modo que la fila existe en backend pero no llega a ser visible en el cliente.

**Transacciones (`/transactions`)**

| Orden | Ruta | Etiqueta actual |
|---:|---|---|
| 1 | `/transactions/create` | Crear transacción |
| 2 | `/transactions/list` | Consultar transacciones |
| 3 | `/transactions/nacha-upload` | Cargar NACHA-M |
| 4 | `/customer-third-parties` | Terceros prenotificación |
| 5 | `/transactions/returns` | Gestión devoluciones |
| 6 | `/transactions/outgoing-monitoring` | Transacciones de salida |
| 7 | `/incoming-nacha-command-center` | Seguimiento de archivos NACHA-M |
| 8 | `/ach-colombia/file-exchange` | Intercambio de archivos ACH Colombia |
| 9 | `/ach-claims` | Reclamaciones ACH |
| 9 | `/administracion/mft-ach` | MFT ACH |

Existe un empate de orden entre Reclamaciones ACH y MFT ACH.

**Integraciones (`/integraciones`)**

El backend mantiene un hijo `/soap-integrations` con etiqueta `Integraciones SOAP`. Sin embargo, el cliente Angular reemplaza el nodo por una entrada única `/integraciones` con etiqueta `Centro de integración CFA` y `children: []`, por lo cual el hijo sembrado no se representa.

**Seguridad NACHA (`/nacha-security`)**

| Orden | Ruta | Etiqueta actual |
|---:|---|---|
| 1 | `/nacha-security/certificates` | Certificados digitales |
| 2 | `/nacha-security/sobre-digital` | Sobre digital |

**Programador (`/scheduler`)**: `/scheduler/tasks`.  
**Logs (`/logs`)**: `/audit-logs` y `/auth-logs`.  
**UAT / Simuladores (`/uat`)**: `/uat/nacha-inbound-simulator`.

## 4. Órdenes y rutas duplicadas

### 4.1 Empates entre hermanos activos

Se caracterizaron exactamente estos empates actuales:

- raíz, orden `2`: `/users` y `/nacha-config-admin`;
- raíz, orden `5`: `/integraciones` y `/catalogs`;
- raíz, orden `7`: `/navigation/menu-items` y `/reports`;
- `/transactions`, orden `9`: `/ach-claims` y `/administracion/mft-ach`.

El handler ordena por `Order`; no define un segundo criterio de ordenamiento. Por lo tanto, el orden visible relativo de hermanos empatados no está expresado de forma determinista por la lógica de negocio.

### 4.2 Ruta activa duplicada

Hay dos `MenuItem` activos con ruta `/ach-cycles`:

- el padre raíz `Configuración de ciclos`;
- su hijo `Catálogo de ciclos`.

Esto coincide con el comportamiento del layout actual: un padre con hijos funciona sólo como expansor y no como link navegable.

## 5. Autorización actual: menú frente a rutas SPA

### 5.1 Modelo base

`RoleConfiguration` define únicamente dos roles base:

- `Admin`;
- `ACH.Operator`.

En `GetMenuForCurrentUserHandler`:

- las restricciones por rol se satisfacen si el usuario tiene **cualquiera** de los roles requeridos;
- las restricciones por permiso se satisfacen si tiene **cualquiera** de los permisos requeridos;
- cuando existen restricciones de ambos tipos, deben cumplirse ambas categorías;
- `/uat` y sus descendientes además pasan por un gate de ambiente/opción.

### 5.2 Diferencias relevantes encontradas

| Superficie | Menú actual | Ruta SPA actual | Gap caracterizado |
|---|---|---|---|
| Crear transacción | `CanManageAch` **o** `CanReadAch` | `CanManageAch` | un usuario sólo lectura puede ver la opción y luego ser rechazado por la ruta |
| `/administracion/mft-ach` | `Admin` + `CanReadAch` | `Admin` **o** `ACH.Operator` + `CanReadAch` | operador puede abrir por URL pero no ve la opción |
| `/integraciones` | `Admin` + `CanManageUsers` | `Admin` **o** `ACH.Operator` + `CanReadAch` **o** `CanManageAch` | la visibilidad del menú es más restrictiva y usa un permiso de usuarios no alineado con la ruta |
| `/ach-claims` | `Claims.Read` sin restricción de rol | guardas finas de claims en el módulo | `ClaimsPermissionSeeder` crea permisos pero no los asigna por defecto a roles base |
| UAT | roles/permisos + gate de ambiente/opción | guardas SPA de rol/permiso | la disponibilidad del menú depende adicionalmente de `IsProduction`/opción actual |

No se corrigió ninguna de estas diferencias en NAV1.

## 6. Rutas SPA sin acceso canónico desde el menú actual

Se comparó el código de rutas con los `MenuItem` efectivos. Se excluyeron deliberadamente detalles por `:id`, redirects, 403/404, rutas CENIT que el programa ya clasifica fuera del menú, y flujos claramente contextuales/técnicos según la documentación del programa.

### 6.1 Cobertura faltante relevante

- `/customers`;
- `/transactions/bulk-ingestion/upload`;
- `/transactions/bulk-ingestion/tracking`;
- `/transactions/returns-ror`;
- `/incoming-nacha-command-center/observability`;
- `/incoming-nacha-command-center/orphan-resolution`;
- `/incoming-nacha-command-center/queue`;
- `/ach-responses`;
- `/ach-responses/dashboard`;
- `/ach-responses/manual-review`;
- `/ach-responses/audit`;
- `/ach-responses/status-mappings`;
- `/ach/reconciliation`;
- `/navigation-logs`;
- `/catalogs/company-entry-descriptions`;
- `/catalogs/document-types`;
- `/catalogs/person-types`;
- `/catalogs/gender-types`;
- `/catalogs/phone-types`;
- `/catalogs/email-types`;
- `/catalogs/address-types`;
- `/catalogs/transaction-codes`;
- `/integraciones/contrato`;
- `/integraciones/relacion-informacion`;
- `/integraciones/conexion`;
- `/ach/nacha/soap-uat-console`;
- `/payment-rail-capability-registry`.

### 6.2 Superficies con entrada parcial o legacy

- `/customer-third-parties` sí está en el menú, pero es una superficie legacy mientras `/customers` no tiene entrada canónica.
- `/incoming-nacha-command-center` sí tiene entrada, pero sus vistas operativas observability/queue/orphan-resolution no.
- `/ach-responses` no tiene ninguna entrada sembrada pese a tener varias rutas funcionales.
- integraciones tiene entrada backend y subrutas SPA, pero el override de Angular aplana la jerarquía y elimina hijos.
- `/catalogs/clearing-house-special-dates` está sembrada pero filtrada en Angular.

## 7. Overrides de navegación en Angular

`NavigationService` no se limita hoy a consumir la jerarquía del backend. Además de ordenar recursivamente por `order`, realiza estas transformaciones relevantes:

1. filtra rutas legacy/retiradas, entre ellas `/catalogs/clearing-house-special-dates` y rutas NACHA antiguas;
2. detecta `/integraciones`, `/soap-integrations`, `/integraciones/soap-settings` y `/integraciones/mappings`;
3. las consolida en una sola opción con:
   - label `Centro de integración CFA`;
   - route `/integraciones`;
   - `exact: false`;
   - `children: []`;
4. deduplica cualquier otra representación del centro de integración.

Esto contradice la decisión documental de que el seed/backend sea la fuente de verdad de la navegación visible, pero NAV1 sólo registra el gap.

`MainLayoutComponent` también tiene un comportamiento estructural relevante: cualquier `MenuItem` con hijos se renderiza como `button` expansor, no como `<a>` con `routerLink`. Por tanto, la `route` del padre deja de ser un acceso directo desde el menú. Se agregó una prueba de caracterización específica para congelar este comportamiento antes de NAV2.

## 8. Pruebas actuales y cobertura NAV1

### 8.1 Cobertura existente encontrada

Backend:

- idempotencia/canonicalización de varios seeders (`AchClaims`, File Exchange, Incoming NACHA, Operational Restrictions, Reports, Outgoing Monitoring, NACHA config/cycles);
- visibilidad por permisos finos para claims/restricciones;
- ausencia de algunas rutas legacy;
- caso de UAT no disponible;
- jerarquía del servicio de menú.

Frontend:

- `NavigationService`: fuente backend, orden recursivo, filtros legacy, rewrite/dedupe de integraciones;
- `MainLayoutComponent`: jerarquía multinivel, estado activo, navegación desktop/mobile;
- guards y matriz parcial de acceso en `app-routing-access.spec.ts`;
- existencia/guardas de `returns-ror` en routing de transacciones.

### 8.2 Guardas de caracterización agregadas en NAV1

`NavigationMenuNav1BaselineTests.cs` congela:

- raíces y órdenes efectivos actuales tras ejecutar los seeders de navegación;
- hijos actuales de Transacciones y sus órdenes;
- los cuatro empates de orden actuales;
- la ruta duplicada activa `/ach-cycles` padre/hijo;
- idempotencia práctica del conjunto al sembrarlo dos veces en el test de inventario.

`main-layout.component.spec.ts` agrega una guarda que confirma explícitamente que un padre con `route` y `children` se renderiza como expansor y no como link.

### 8.3 Gaps de test todavía existentes

- no hay una prueba end-to-end del `DbInitializer` real que demuestre un desempate estable para seeders con el mismo `Order` (el código tampoco define ese desempate);
- no existe una prueba automática que compare exhaustivamente todas las rutas SPA funcionales contra cobertura de menú;
- el gate UAT tiene cobertura parcial, pero no una matriz completa Production/no-Production + opción habilitada/deshabilitada;
- varias diferencias de autorización menú/ruta están documentadas, no cerradas.

## 9. Diferencias frente a la documentación objetivo

La matriz objetivo y la arquitectura del programa describen una navegación futura con nombres, jerarquías y órdenes distintos. El código vigente todavía conserva, entre otros:

- etiquetas técnicas (`MFT ACH`, `UAT / Simuladores`, `Centro de integración CFA`, `Restricciones operacionales CFA`);
- empates de orden en lugar de secuencias 10/20/30;
- grupo Transacciones con opciones que la matriz objetivo reparte entre Operación y Seguimiento y control;
- falta de entradas para Clientes, Respuestas, navegación logs y múltiples subrutas funcionales;
- override frontend que impide que seed/backend sea hoy la única fuente de verdad visual;
- gate ambiental para UAT, aunque la decisión objetivo prevé autorización técnica en lugar de `IsProduction`.

Estas diferencias son hallazgos/gaps de baseline. No se resolvieron en NAV1.

## 10. Conclusión de NAV1

El baseline quedó caracterizado sin cambios de comportamiento productivo. La evidencia permite abrir NAV2 técnicamente: ya están identificados el inventario vigente, el orden de seeders, los empates, la duplicidad `/ach-cycles`, los gaps de cobertura SPA, los overrides de Angular y las principales divergencias de autorización.

Recomendación de incremento siguiente: **NAV2 es técnicamente elegible**, pero debe iniciarse sólo con una instrucción nueva del usuario. NAV1 no ejecuta ni anticipa cambios de NAV2.
