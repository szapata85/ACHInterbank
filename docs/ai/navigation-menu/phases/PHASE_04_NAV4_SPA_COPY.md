# NAV4 — SPA y copy humanizado

## Objetivo
Que el SPA renderice la IA del backend y use copy funcional.

## Incrementos

### NAV4.1A — Fidelidad del árbol backend en `NavigationService`

**Estado:** `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`.

Al iniciar NAV4.1A, el pipeline era:

```text
GET api/navigation/menu
-> ApiService.get<MenuItem[]>()
-> NavigationService.getMenu()
-> MenuItem
-> MainLayoutComponent.menuItems
-> template recursivo de MainLayout
```

El backend/seed es la fuente de verdad para `label`, `route`, parent/children, `order` y jerarquía. NAV4.1A no introduce árbol, labels ni agrupaciones locales.

#### Inventario de transformaciones encontradas al inicio

| Archivo / método | Propósito aparente | Propiedad afectada | ¿Modifica IA? | Decisión NAV4.1A |
| --- | --- | --- | --- | --- |
| `NavigationService.getMenu` -> `removeRetiredMenuItems` | Retirar rutas consideradas legacy/retiradas en frontend | presencia de nodos/children | **Sí**: podía descartar nodos que el backend sí entregó | **REMOVE** |
| `NavigationService.removeRetiredMenuItems` -> `normalizeRoute` | Normalizar `route` para comparar con sets hardcodeados | route sólo para comparación | Soporta una transformación de IA; no reescribe la route emitida | **REMOVE** junto con el filtro |
| `NavigationService.removeRetiredMenuItems` | Rematerializar recursivamente cada nodo y usar `children: []` si no había hijos | children | Cambia la forma del árbol aunque no agregue items | **REMOVE** junto con el filtro |
| `NavigationService.sortMenu` | Ordenar siblings por `order` del backend, recursivamente | orden de siblings | No reconstruye IA; materializa el `Order` backend | **KEEP** |
| `NavigationService.sortMenu` | Rematerializar hojas con `children: []` | children | Cambio de forma no necesario | **REMOVE**; ahora conserva `undefined` y sólo recurre si `children` existe |
| `MainLayout` | Render recursivo; padres con children se comportan como expander | interacción, no árbol recibido | No transforma el árbol de `NavigationService` | **OUT_OF_SCOPE** |
| `MainLayout.buildBreadcrumbs` / títulos de rutas | Construir breadcrumb/page title desde route data | título/breadcrumb | No pertenece al árbol del menú | **OUT_OF_SCOPE** |

No se encontraron en `NavigationService` al inicio de NAV4.1A: reemplazos por `Label`, mapas de labels, creación manual de MenuItems, flattening, deduplicación, reparenting, mutación de `exact`, mutación de `icon`, ni sustitución de rutas. El antiguo override `Centro de integración CFA` ya había sido retirado en NAV3.4N.

#### Cambio aplicado

`NavigationService` ahora:

- consume exclusivamente `api/navigation/menu`;
- no filtra nodos por `route` ni por `label`;
- no contiene sets/listas locales de rutas canónicas o retiradas;
- no fabrica ni sustituye children;
- no reparenta ni aplana el árbol;
- no modifica `label`, `route`, `exact`, `icon` ni `order`;
- conserva la ordenación recursiva por el campo `order` entregado por backend, sin orden semántico hardcodeado por route/label;
- preserva un child futuro/desconocido sin requerir una regla Angular.

El filtro frontend que retiraba `/aliases`, rutas NACHA legacy y `/transactions/clearing-house-rules` fue eliminado. Si esas rutas no deben formar parte del menú visible, su exclusión corresponde al backend/seed/autorización, no a una IA paralela en Angular.

#### Regresiones focales

`navigation.service.spec.ts` cubre:

- parent/children y profundidad preservados;
- labels y routes preservados exactamente;
- orden de siblings basado sólo en `order` backend;
- child futuro/desconocido conservado;
- árbol ya filtrado por backend no recibe siblings/items locales;
- labels representativos de NAV2 sin reescritura;
- `Conexiones` conserva exactamente `Contrato del servicio`, `Relación de información`, `Conexión entidad participante` y `Configuración del intercambio de archivos`;
- `Centro de integración CFA` no reaparece;
- ausencia de fabricación de `children: []` para metadata omitida.

#### Fuera de alcance confirmado

NAV4.1A no cambia `MainLayout`. Un padre con children continúa funcionando como expander/no navegable desde el nodo padre. El caso `/reports` queda pendiente para NAV4.1B.

NAV4.1A tampoco cambia route titles, `document.title`, breadcrumbs, headings, copy interno, rutas faltantes, guards, policies, MenuItemRole/MenuItemPermission, grants, seeders NAV3, ni `GetMenuForCurrentUserHandler`.

### NAV4.1B — navegación de padres con children

**Estado:** `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`.

NAV4.1B corrige únicamente la interacción del menú. El árbol que llega de `NavigationService` no se reescribe y NAV4.1A permanece vigente.

#### Flujo inspeccionado y comportamiento anterior

El consumo actual es:

```text
NavigationService.getMenu()
-> MainLayoutComponent.menuItems
-> template recursivo #menuTree
-> routerLink / toggleSubmenu()
-> Router
-> syncActiveNavigation() / RouterLinkActive
```

Antes de NAV4.1B, el template aplicaba una regla única: `item.children?.length > 0` seleccionaba siempre un `<button>` que ejecutaba `toggleSubmenu(item)`. Por tanto, cualquier parent con children era sólo expander y nunca renderizaba `routerLink`, aunque su `Route` resolviera una pantalla funcional real. Ese comportamiento hacía inaccesible desde el propio parent el landing `/reports`.

El modelo `MenuItem` vigente sólo contiene `id`, `label`, `route`, `icon`, `exact`, `order` y `children`; no existe metadata `IsNavigable`/equivalente. NAV4.1B no agrega metadata al backend/schema/modelo.

#### Inventario completo de parents con children

El árbol canónico vigente contiene **13 parents con children**. La clasificación se obtuvo contrastando la ruta de cada parent con la configuración Angular real; no se infirió por label ni por nombre de dominio.

| Parent | Route | Order | Children canónicos | Evidencia de routing exacta | Clasificación |
| --- | --- | ---: | --- | --- | --- |
| Operación | `/transactions` | 20 | Consultar transacciones; Crear transacción; Cargar transacciones desde archivo; Seguimiento de cargas; Cargar archivo NACHA-M; Devoluciones; Devolución de una devolución; Reclamaciones; Intercambio de archivos; Exportar archivo NACHA-M | `app-routing` carga `TransactionsModule`; su route `''` hace redirect a `list`, sin component propio del parent | `STRUCTURAL_PARENT` |
| Seguimiento y control | `/seguimiento-control` | 40 | Transacciones enviadas; Seguimiento de archivos; Indicadores operativos; Cola de procesamiento; Devoluciones sin relación; Respuestas de transacciones; Conciliación | no existe route exacta `/seguimiento-control` en el Router | `STRUCTURAL_PARENT` |
| Respuestas de transacciones | `/seguimiento-control/respuestas-transacciones` | 60 | Bandeja de respuestas; Resumen de respuestas; Revisión manual; Historial de respuestas | no existe route exacta del agrupador; sus children funcionales viven bajo `/ach-responses/**` | `STRUCTURAL_PARENT` |
| Administración de parámetros | `/administracion-parametros` | 50 | Cámaras compensadoras; Prioridades por cámara; Ciclos de operación; Días no hábiles; Restricciones operativas; Configuración de archivos NACHA-M; Catálogos; Conexiones; Homologación de estados | no existe route exacta `/administracion-parametros` en el Router | `STRUCTURAL_PARENT` |
| Configuración de archivos NACHA-M | `/nacha-config-admin` | 60 | Perfiles oficiales; Registros oficiales; Campos y variantes | `NachaConfigAdminRoutingModule` tiene default redirect `'' -> perfiles` | `STRUCTURAL_PARENT` |
| Catálogos | `/catalogs` | 70 | Entidades financieras; Conceptos de lote; Tipos de documento; Tipos de persona; Tipos de género; Tipos de teléfono; Tipos de correo; Tipos de dirección; Códigos de transacción | `CatalogsRoutingModule` resuelve `''` con `CatalogsListComponent` | `FUNCTIONAL_PARENT` |
| Conexiones | `/integraciones` | 80 | Contrato del servicio; Relación de información; Conexión entidad participante; Configuración del intercambio de archivos | `INTEGRATIONS_ROUTES` resuelve `''` con lazy `IntegrationWorkspaceComponent` y no hace redirect | `FUNCTIONAL_PARENT` |
| Seguridad de archivos | `/nacha-security` | 60 | Certificados digitales; Cifrar y descifrar archivos | `NachaSecurityRoutingModule` termina su default route en redirect `'' -> certificates` | `STRUCTURAL_PARENT` |
| Reportes | `/reports` | 70 | Transacciones enviadas; Transacciones recibidas; Devoluciones; Rechazos; Archivos NACHA-M; Ciclos; Conciliación; Trazabilidad ACH; Histórico; Auditoría | `ReportsRoutingModule` resuelve `''` con `ReportsHomeComponent` | `FUNCTIONAL_PARENT` |
| Administración | `/administracion` | 80 | Usuarios y acceso; Tareas programadas; Registros del sistema; Organización del menú; Capacidades de la plataforma | no existe route exacta `/administracion`; `/administracion/mft-ach` es otra superficie y no convierte al parent en navegable | `STRUCTURAL_PARENT` |
| Usuarios y acceso | `/users` | 10 | Administrar usuarios; Identidad visual; Reglas de contraseña; Bloqueo de acceso | `AdminRoutingModule` tiene wrapper pathless y un child `''` con `UsersListComponent` | `FUNCTIONAL_PARENT` |
| Registros del sistema | `/logs` | 30 | Registro de auditoría; Registro de accesos; Registro de navegación | `app-routing` define `/logs` como redirect exacto a `/audit-logs` | `STRUCTURAL_PARENT` |
| Herramientas de prueba | `/uat` | 90 | Simulador de archivos recibidos; Pruebas de servicios | `UatRoutingModule` tiene default redirect `'' -> nacha-inbound-simulator` | `STRUCTURAL_PARENT` |

Resultado: **4 `FUNCTIONAL_PARENT`** (`Catálogos`, `Conexiones`, `Reportes`, `Usuarios y acceso`) y **9 `STRUCTURAL_PARENT`**.

#### Caso `/reports`

`/reports` sí es una superficie funcional independiente. `ReportsRoutingModule` carga `ReportsHomeComponent` en la ruta vacía del módulo. Ese landing muestra una central de reportes agrupada, enlaces a los diez reportes y una sección independiente de revisión contable con `AccountingReviewExportComponent`. Antes de NAV4.1B el menú sólo permitía expandir `Reportes`; no existía link al landing del parent.

NAV4.1B hace accesible `/reports` desde el label/área principal del parent y conserva un toggle separado para sus **10 children canónicos**. No agrega `Resumen de reportes` ni ningún child artificial.

#### Caso `/integraciones`

`/integraciones` también es funcional según el mismo criterio genérico: su default route carga `IntegrationWorkspaceComponent`, que monta `IntegrationCenterShellComponent` y carga el store de integración. `Conexiones` recibe exactamente el mismo patrón link + toggle sin ninguna excepción por path. Su jerarquía NAV4.1A permanece exactamente en cuatro children y `Centro de integración CFA` no reaparece como item de menú.

#### Determinación genérica de navegabilidad

Se agregó `MenuRouteSurfaceService`, aislado al comportamiento UI/routing. Para cada **parent visible que ya tiene children**:

1. parsea su URL con el `Router`;
2. recorre `router.config` por el outlet primario;
3. considera funcional una coincidencia exacta que tenga `component`/`loadComponent`, incluida una cadena de children pathless `''` que termine en component;
4. considera estructural un destino inexistente o cuyo destino exacto sea sólo `redirectTo`;
5. resuelve `loadChildren` de forma genérica, tanto arrays `Routes` como NgModules lazy; para NgModules obtiene el token `ROUTES` del injector propio del módulo;
6. cachea la configuración lazy por `Route` mediante `WeakMap`.

No existe una lista de rutas navegables ni comparaciones especiales para `/reports`, `/integraciones`, `/catalogs`, `/users` u otro parent. El servicio sólo inspecciona configuración real del Router. No ejecuta navegación ni reemplaza guards; cargar la configuración lazy para clasificar un parent visible no concede acceso a la ruta. La autorización efectiva sigue siendo la de los guards existentes cuando el usuario navega.

#### Modelo de interacción final

Para un `FUNCTIONAL_PARENT` con children:

- el área principal es un `<a [routerLink]="item.route">` semántico que navega al destino funcional;
- existe un `<button type="button">` separado para expandir/colapsar children;
- click en el toggle no navega; click en el link no funciona como toggle;
- los children siguen siendo el árbol recibido del backend.

Para un `STRUCTURAL_PARENT`:

- continúa existiendo un único `<button mat-list-item>` que expande/colapsa;
- no se emite un link a una route inexistente o redirect-only.

El modelo existente de expansión se conserva. En desktop compacto, activar el toggle primero recupera el sidebar expandido y abre el grupo; en móvil, el toggle mantiene el drawer abierto y el link de navegación usa el comportamiento vigente que cierra el drawer después de seleccionar destino.

#### Active state

`syncActiveNavigation()` conserva su recorrido recursivo y `item.exact` para detectar la rama activa. Para un parent funcional, el `routerLinkActive` del link principal usa matching **exacto**: en la URL exacta del parent el link recibe `active`/`aria-current=page`; al navegar a un child, el child queda como destino `aria-current=page` y el parent permanece indicado/expandido como rama activa sin apropiarse del estado exacto del child.

No se modifican `exact`, `Route` ni el árbol recibido.

#### Accesibilidad

El toggle independiente del parent funcional:

- es un `button` nativo con `type="button"`;
- expone `aria-expanded`;
- expone `aria-controls` hacia el submenú;
- usa `aria-label` dinámico `Expandir/Contraer <label>`;
- conserva activación nativa por teclado Enter/Space;
- recibe foco de teclado y tiene `:focus-visible`.

El destino funcional sigue siendo un link/routerLink real. Los parents estructurales siguen siendo buttons, no anchors sin destino.

#### Pruebas focales y validación

Se agregaron pruebas para:

- clasificación de component, `loadComponent`, redirect, ruta inexistente, lazy `Routes` y lazy NgModule;
- parent funcional futuro/desconocido con route registrada, sin regla por path;
- parent estructural sintético sin navegación inválida;
- separación link/toggle y preservación de children;
- `/reports`: landing navegable, toggle independiente, diez children, child activo sin `aria-current` erróneo en el parent y ausencia de child artificial;
- `/integraciones`: mismo modelo genérico y jerarquía canónica de cuatro children;
- active state de parent/child;
- semántica/foco/ARIA del toggle;
- desktop compacto y mobile drawer.

Validación disponible en este entorno:

- parser TypeScript 5.8.3 focal (4 archivos TS NAV4.1B): `PASS`;
- `scripts/validate-nav4-1b-parent-navigation.py`: `PASS — 108 aserciones` (incluye hashes de seguridad/NAV4.1A);
- `scripts/validate-solution-projects.py`: `PASS — 7/7 proyectos`;
- Angular build/test/runtime: `NOT_RUN_ENVIRONMENT` porque `web/ach-interbank-ui/node_modules` no está presente;
- .NET: `NOT_RUN_ENVIRONMENT` porque `dotnet` no está disponible.

NAV4.1B no modifica `NavigationInformationArchitectureSeeder`, `GetMenuForCurrentUserHandler`, grants, fallbacks, policies, guards de autorización, Orders 119/120 ni route configs.

## Alcance NAV4 posterior

- alinear títulos/breadcrumbs de rutas afectadas;
- usar copy funcional aprobado en superficies visibles;
- reemplazar copy visible técnico cuando el incremento correspondiente sea autorizado.

## No hacer
No renombrar rutas ni contratos técnicos si no es necesario. No cambiar reglas de negocio ni seguridad como efecto lateral de NAV4.

## Gate
Pruebas SPA focales cuando exista `node_modules`, más validación estática de `NavigationService` y regresión de superficies protegidas.

### NAV4.2A — títulos y breadcrumbs alineados con navegación/routing canónico

**Estado:** `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`.

NAV4.2A parte exclusivamente del handoff `ACHInterbank-R34-NAV-IA-NAV4.1B-FULL-PROJECT.zip`, SHA-256 `7a744875af98c6b9fcfa5c5381a9f998df87acf480428606023eae27ac8fcdc6`, sin Git. NAV2 y NAV3 permanecen `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`; NAV3.5C continúa `BLOCKED / NO EJECUTAR`; NAV4.1A y NAV4.1B permanecen `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`.

#### Infraestructura previa de naming visible

La inspección completa encontró estos mecanismos:

| Mecanismo | Archivo / método | Fuente previa del texto | Alcance | Duplicación backend | Decisión NAV4.2A |
| --- | --- | --- | --- | --- | --- |
| Título visible del layout | `layout/main-layout.component.ts` -> `buildBreadcrumbs()` y `main-layout.component.html` -> `.page-title` | último `ActivatedRoute.snapshot.data['title']` encontrado al recorrer `firstChild` | toda ruta bajo `MainLayout` | sí, para muchas rutas menu-backed | **REPLACE** como fuente efectiva para exact menu-backed; conservar route metadata como fallback/contextual |
| Breadcrumb global del layout | mismo `buildBreadcrumbs()` y `<nav class="breadcrumbs">` | `data['breadcrumb']`; URL armada desde `routeConfig.path` | toda ruta bajo `MainLayout` | sí, para rutas menu-backed; podía repetir aliases de parents | **REPLACE** para exact menu-backed y **KEEP_CONTEXTUAL** para descendientes sin MenuItem |
| Metadata de routing | `app-routing.module.ts` y módulos/routes feature | `data.title` / `data.breadcrumb` | menu-backed + contextual + técnica | mezcla de duplicado y metadata necesaria | **KEEP_CONTEXTUAL**; no se hace refactor masivo ni se borra metadata duplicada |
| Breadcrumb de página `ui-migas-pan` | `shared/components/ui/ui-migas-pan.component.ts` + arrays locales | arrays `{ etiqueta, ruta? }` en páginas CENIT, command center, NACHA config, integraciones, registry | breadcrumbs internos ya existentes | sí en elementos que apuntan a una route menu-backed | **REUSE/REPLACE** sólo para el label de crumbs con route canónica; crumbs contextuales sin MenuItem se conservan |
| Browser/document title | `src/index.html` | estático `ACH Interbank - Gestión de Transacciones` | documento completo | no era route-aware | **REUSE** de branding/separador existente; bajo `MainLayout` pasa a `ACH Interbank - <pageTitle efectivo>` |
| `AppComponent` | `app.component.ts` | propiedad local `title = 'ACH Interbank SPA'`, no consumida para naming | shell raíz | no produce title/breadcrumb | **OUT_OF_SCOPE** |
| Tracking de `NavigationEnd` | `NavigationLogService.startTracking()` | URL normalizada para auditoría | logging | no produce naming | **OUT_OF_SCOPE** |
| Titles internos de dialogs/cards/modelos | múltiples componentes | copy funcional interno | contenido de página | no es infraestructura de route naming | **OUT_OF_SCOPE** para NAV4.2A |

No se encontró un mapa global `route -> label`, un `switch/case` por URL ni un `TitleStrategy` previo. Sí existían arrays locales de `ui-migas-pan` y metadata de routing con aliases técnicos, por ejemplo `Centro de integración CFA`, `Conexión CFA`, `MFT ACH`, `Command Center Respuestas ACH`, `Capability Registry`, `UAT` y otros. NAV4.2A no inicia una campaña general de copy: sólo evita que esos valores sean la fuente efectiva cuando la route exacta tiene un MenuItem canónico.

La aplicación **sí tenía breadcrumbs visibles** antes de NAV4.2A: el toolbar de `MainLayout` renderiza `<nav class="breadcrumbs">` y varias páginas renderizan adicionalmente `ui-migas-pan`. Por tanto no se crea un componente visual nuevo ni se rediseña layout.

#### Modelo final y fuente de verdad

Se agrega `NavigationNamingService`, que reutiliza la única llamada de `NavigationService.getMenu()` mediante un stream compartido `shareReplay(1)`. `NavigationService` NAV4.1A queda byte a byte intacto y sigue haciendo únicamente fetch backend + sorting recursivo por `order`. MainLayout y `ui-migas-pan` consumen el mismo árbol compartido; no se solicita ni reconstruye un segundo árbol.

La resolución semántica vive en `navigation-naming.util.ts` y no contiene rutas ni labels de negocio hardcodeados. Recorre recursivamente el `MenuItem[]` recibido y aplica:

1. **Exact menu-backed route:** se elimina query/fragment para comparar la URL actual con `MenuItem.route`. Si existe match exacto, `pageTitle` y crumb final usan `MenuItem.label`; la jerarquía del breadcrumb es `ancestors` reales del árbol backend + item actual.
2. **Contextual/non-menu route:** si no hay match exacto, se busca el MenuItem ancestro más específico sólo con boundary de segmentos (`candidate + '/'`, nunca `startsWith()` ingenuo). Esa selección identifica el contexto; **la jerarquía no se deriva del prefijo URL**, sino de los ancestors almacenados en el árbol backend. Después se agregan únicamente los breadcrumbs contextuales de `route data` que no corresponden ya a una route canónica.
3. **Sin MenuItem relacionado:** se conserva la metadata de routing existente como antes.
4. **Rutas parametrizadas:** la traversal usa `ActivatedRouteSnapshot.url` (segmentos resueltos) y no `routeConfig.path`; por ello una URL real como `/users/42/edit` nunca fabrica `:id` como título ni como breadcrumb.
5. **Query params / fragment:** se excluyen sólo para el matching canónico; `/reports?x=1#section` resuelve igual que `/reports`.
6. **Árbol aún no cargado o error:** el sistema usa temporalmente la metadata de routing ya existente; no muestra `undefined`, no rompe routing y vuelve a calcular naming en cuanto llega el árbol. Si el árbol falla, permanece el fallback técnico previo y se mantiene el status de error de menú existente.

No se agregó ninguna lista productiva `route -> label`, allowlist de rutas, switch por URL ni copia de NAV2 en TypeScript.

#### Browser/document title

Antes de NAV4.2A no existía `Title.setTitle`, `document.title` dinámico ni `TitleStrategy`; sólo el `<title>` estático de `index.html`: `ACH Interbank - Gestión de Transacciones`.

NAV4.2A conserva el branding y el separador existentes: dentro de `MainLayout`, el `Title` de Angular se actualiza a:

```text
ACH Interbank - <pageTitle efectivo>
```

La parte de página proviene del label backend para exact menu-backed routes y de metadata contextual para non-menu routes. No se cambia favicon, header global ni nombre de producto.

#### Breadcrumb global y breadcrumbs locales existentes

El breadcrumb global de `MainLayout` ahora usa la misma resolución que el título. Para rutas exactas menu-backed, sus ancestors provienen del árbol backend, no de `route.split('/')` ni de prefijos textuales. Para ancestors conceptuales cuya route no forma parte de la cadena ActivatedRoute actual, el crumb se renderiza como texto y no se inventa un link a una route estructural inexistente.

`ui-migas-pan` se conserva visualmente sin rediseño. Cuando un item ya tiene `ruta`, su `etiqueta` se reemplaza sólo si esa route tiene match exacto en el árbol backend. Para el último crumb sin `ruta`, se intenta el match exacto de la URL actual; esto corrige páginas menu-backed cuyos arrays locales tenían alias como `Capability Registry (solo lectura)`, sin tocar crumbs contextuales. Si no existe MenuItem, el texto local permanece.

#### Inventario focal de rutas menu-backed y divergencias

La lista canónica completa de rutas MENU/MENU_GROUP continúa en `docs/ux/NAVIGATION_MENU_ROUTE_MATRIX.md`; NAV4.2A no duplica esa taxonomía en otra constante Angular. La resolución nueva aplica recursivamente a todas esas rutas cuando el Router las activa. Los casos de divergencia relevantes comprobados son:

| Route | Jerarquía backend / label canónico | Naming Angular efectivo previo | Resultado NAV4.2A |
| --- | --- | --- | --- |
| `/reports` | `Reportes` | parent `title: Reportes ACH` + child `title: Reportes`; breadcrumb podía duplicar `Reportes` por route pathless | title `Reportes`; breadcrumb `Reportes`; document title `ACH Interbank - Reportes` |
| `/reports/sent` | `Reportes -> Transacciones enviadas` | `title: Enviados`; sin breadcrumb child | title/crumb final `Transacciones enviadas` |
| `/reports/received` | `Reportes -> Transacciones recibidas` | `title: Recibidos` | title/crumb final `Transacciones recibidas` |
| `/reports/returns` | `Reportes -> Devoluciones` | `title: Devoluciones` | mismo copy pero fuente backend |
| `/reports/rejections` | `Reportes -> Rechazos` | `title: Rechazos` | mismo copy pero fuente backend |
| `/reports/files` | `Reportes -> Archivos NACHA-M` | `title: Archivos` | `Archivos NACHA-M` |
| `/reports/cycles` | `Reportes -> Ciclos` | `title: Ciclos` | mismo copy pero fuente backend |
| `/reports/reconciliation` | `Reportes -> Conciliación` | `title: Conciliación` | mismo copy pero fuente backend |
| `/reports/traceability` | `Reportes -> Trazabilidad ACH` | `title: Reporte de trazabilidad ACH` | `Trazabilidad ACH` |
| `/reports/history` | `Reportes -> Histórico` | `title: Histórico` | mismo copy pero fuente backend |
| `/reports/audit` | `Reportes -> Auditoría` | `title: Auditoría` | mismo copy pero fuente backend |
| `/integraciones` | `Administración de parámetros -> Conexiones` | `Centro de integración CFA` | title `Conexiones`; breadcrumb usa hierarchy backend |
| `/integraciones/contrato` | `Administración de parámetros -> Conexiones -> Contrato del servicio` | parent crumb `Centro de integración CFA`; child correcto | hierarchy y title canónicos backend |
| `/integraciones/relacion-informacion` | `Administración de parámetros -> Conexiones -> Relación de información` | parent alias CFA; child correcto | hierarchy y title canónicos backend |
| `/integraciones/conexion` | `Administración de parámetros -> Conexiones -> Conexión entidad participante` | `Conexión CFA` | `Conexión entidad participante` |
| `/administracion/mft-ach` | `Administración de parámetros -> Conexiones -> Configuración del intercambio de archivos` | `Administración MFT ACH` / `MFT ACH` | backend hierarchy exacta, aunque el prefijo URL no sea `/integraciones` |
| `/users` | `Administración -> Usuarios y acceso` | `Administración de usuarios` / `Usuarios` | exact route usa `Usuarios y acceso`; backend ancestor `Administración` |
| `/navigation/menu-items` | `Administración -> Organización del menú` | `Menú de navegación` / `Navegación` + `Administrar menú de navegación` / `Menús` | naming visible canónico backend |
| `/navigation-logs` | `Administración -> Registros del sistema -> Registro de navegación` | `Log de navegación` | `Registro de navegación` |
| `/auth-logs` | `Administración -> Registros del sistema -> Registro de accesos` | `Registro de autenticaciones` | `Registro de accesos` |
| `/payment-rail-capability-registry` | `Administración -> Capacidades de la plataforma` | route title `Capability Registry multi-riel (solo lectura)` y local crumb `Capability Registry (solo lectura)` | `Capacidades de la plataforma` en MainLayout y `ui-migas-pan` |
| `/operational-restrictions` | `Administración de parámetros -> Restricciones operativas` | `Restricciones operacionales CFA` | `Restricciones operativas` |

Rutas menu-backed sin drift textual visible también pasan por la misma fuente backend; no se mantienen mapas paralelos para ellas.

#### Inventario focal de rutas contextuales/non-menu

Estas rutas no reciben MenuItem nuevo y conservan metadata contextual:

| Route/patrón | Metadata contextual vigente | Tratamiento NAV4.2A |
| --- | --- | --- |
| `/users/new` | `Crear usuario` / `Nuevo usuario` | contextual; puede mostrar ancestors backend de `/users`, luego crumb contextual |
| `/users/:id/edit` | `Editar usuario` | contextual; segmentos reales, nunca `:id` literal |
| `/users/:id/roles` | `Administrar perfiles de acceso` / `Perfiles de acceso` | contextual; no se fabrica MenuItem |
| `/customers/:id/third-parties` | `Terceros del cliente` / `Terceros` | contextual sobre el ancestor menu-backed `/customers` |
| `/customers/:id/third-parties/:thirdPartyId` | `Detalle del tercero` | contextual; no API adicional para nombre de entidad |
| `/ach-responses/:id` | `Detalle respuesta ACH` / `Detalle` | contextual; no usa ID como crumb |
| `/ach-responses/:id/notification-attempts` | `Intentos de notificación ACH` / `Intentos` | contextual |
| `/transactions/bulk-ingestion/:batchId` | `Detalle de lote` | contextual; hereda sólo el ancestor menu-backed resoluble sin inventar relación sibling |
| `/transactions/outgoing-monitoring/:id` | `Detalle de transacción de salida` / `Detalle de salida` | contextual |
| `/incoming-nacha-command-center/files/:fileId` y detail equivalentes | `Trazabilidad del archivo NACHA-M` / `Detalle del archivo` | contextual |
| `/incoming-nacha-command-center/queue/:id` | `Detalle de procesamiento NACHA-M` / `Detalle de procesamiento` | contextual |
| `/integraciones/relacion-informacion/:methodCode/:mappingSetId` | `Editor de relación de información` | contextual sobre `Relación de información`; local crumb route-linked usa naming canónico del backend |

No se introducen llamadas API para resolver nombres de usuarios, clientes, lotes, archivos o entidades en breadcrumbs.

#### Pruebas focales NAV4.2A

Se agrega `navigation-naming.service.spec.ts` con pruebas para:

- hierarchy backend `Administración de parámetros -> Conexiones -> Conexión entidad participante`;
- cambio sintético de labels backend reflejado automáticamente sin editar configuración Angular;
- child backend futuro/registrado consumido sin mapping productivo;
- `/reports` con query/fragment y child `Transacciones enviadas`;
- `/administracion/mft-ach` usando ancestors backend y no el prefijo URL;
- route parametrizada `/users/42/edit` conservando metadata contextual sin `:id` visible;
- route contextual sin MenuItem;
- canonicalización de crumbs locales route-linked.

`main-layout.component.spec.ts` agrega la expectativa de title visible y browser title `ACH Interbank - Reportes` para `/reports?x=1#section`.

Además, `scripts/validate-nav4-2a-navigation-naming.py` ejecuta aserciones estáticas y un test Node sobre el resolver TypeScript puro, y hash-lock de superficies protegidas.

#### Límites / copy pendiente

NAV4.2A no humaniza headings internos, botones, tablas, mensajes, tooltips, títulos de cards/dialogs ni términos técnicos que no sean la pieza de title/breadcrumb. Persisten textos internos como CFA/CENIT/UAT/Command Center/Capability en diversas pantallas; quedan para el inventario del siguiente incremento si se aprueba. Las route definitions conservan metadata legacy como fallback/contextual y no se reescriben masivamente.

NAV4.2A no modifica navegación funcional, active state, clasificación FUNCTIONAL_PARENT/STRUCTURAL_PARENT, guards, grants, fallbacks, policies, seeders ni backend.


# NAV4.2B — encabezados visibles y metadata fallback de rutas menu-backed

## Estado

`IMPLEMENTED_UNVERIFIED_ENVIRONMENT` — implementación de diseño/código completada; runtime diferido por ambiente. NAV2, NAV3, NAV4.1A, NAV4.1B y NAV4.2A conservan exactamente `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`. NAV3.5C conserva `BLOCKED / NO EJECUTAR`.

## Objetivo y límite

NAV4.2B alinea dos superficies que todavía podían contradecir NAV2 aunque NAV4.2A ya resolvía el naming efectivo desde backend: (1) el heading principal que funciona como nombre de una página menu-backed y (2) `data.title`/`data.breadcrumb` usados como fallback antes de disponer del árbol backend. No se realizó una campaña general de copy. Botones, formularios, labels de campos, placeholders, columnas, mensajes, toasts, diálogos, validaciones, empty states, tooltips y copy contextual profundo quedan fuera de alcance.

El contrato de source of truth continúa siendo el de NAV4.2A:

- **Con menú backend cargado:** `MenuItem.label` + ancestors reales del árbol backend = fuente de verdad.
- **Antes de cargar menú:** route metadata Angular = fallback técnico.
- **Rutas contextuales/non-menu:** conservan metadata propia y no se convierten en MenuItems.
- NAV4.2B no agrega un mapa productivo `route → label`; sólo alinea el texto ya existente en componentes y definiciones de route con el label aprobado.

## Inventario completo de entrada

Se inspeccionaron **81 rutas menu-backed resolubles**. Clasificación antes de NAV4.2B:

- `ALIGNED`: **20**
- `DRIFT`: **52**
- `NO_HEADING`: **7**
- `CONTEXTUAL_ONLY`: **2**

Dentro de ese inventario, **47 rutas** tenían drift en un heading principal que actuaba como nombre de pantalla y **51 rutas** tenían drift en `data.title`/`data.breadcrumb` fallback. `NO_HEADING` significa que no existía un heading local que actuara como nombre de pantalla; el H1 global de `MainLayout` ya cubre el naming canónico. `CONTEXTUAL_ONLY` identifica redirects técnicos que no deben fabricar una nueva superficie visual.

| # | Route | Label canónico NAV2/backend | Componente/landing Angular | Heading principal previo | `data.title` previo | `data.breadcrumb` previo | Otros aliases previos usados como naming | Estado previo | Acción NAV4.2B |
|---:|---|---|---|---|---|---|---|---|---|
| 1 | `/dashboard` | Inicio | DashboardComponent | — (sin heading de nombre de pantalla) | Inicio | Inicio | — | **NO_HEADING** | Sin heading local de nombre de pantalla; se conserva el título global de MainLayout. |
| 2 | `/transactions/list` | Consultar transacciones | TransactionListComponent | Transacciones | Transacciones | Transacciones | Transacciones | **DRIFT** | heading → label canónico; metadata fallback → label canónico |
| 3 | `/transactions/create` | Crear transacción | TransactionCreateComponent | Crear transacción ACH | Crear transacción | Crear transacción | Crear transacción ACH | **DRIFT** | heading → label canónico |
| 4 | `/transactions/bulk-ingestion/upload` | Cargar transacciones desde archivo | BulkIngestionUploadComponent | Carga masiva por archivo | Carga masiva por archivo | Carga masiva por archivo | Carga masiva por archivo | **DRIFT** | heading → label canónico; metadata fallback → label canónico |
| 5 | `/transactions/bulk-ingestion/tracking` | Seguimiento de cargas | BulkIngestionTrackingComponent | Seguimiento de lotes masivos | Seguimiento de lotes | Seguimiento de lotes | Seguimiento de lotes masivos; Seguimiento de lotes | **DRIFT** | heading → label canónico; metadata fallback → label canónico |
| 6 | `/transactions/nacha-upload` | Cargar archivo NACHA-M | NachaUploadComponent | Cargar archivo NACHA-M | Cargar NACHA-M | Cargar NACHA-M | Cargar NACHA-M | **DRIFT** | metadata fallback → label canónico |
| 7 | `/transactions/returns` | Devoluciones | AchReturnsManagementComponent | Devoluciones ACH | Devoluciones ACH | Devoluciones ACH | Devoluciones ACH | **DRIFT** | heading → label canónico; metadata fallback → label canónico |
| 8 | `/transactions/returns-ror` | Devolución de una devolución | AchReturnOfReturnManagementComponent | Devolución de devolución | Devolución de devolución | Devolución de devolución | Devolución de devolución | **DRIFT** | heading → label canónico; metadata fallback → label canónico |
| 9 | `/ach-claims` | Reclamaciones | AchClaimsListComponent | Reclamaciones ACH | Reclamaciones ACH | Reclamaciones ACH | Reclamaciones ACH | **DRIFT** | heading → label canónico; metadata fallback → label canónico |
| 10 | `/ach-colombia/file-exchange` | Intercambio de archivos | AchColombiaFileExchangeComponent | Intercambio de archivos ACH Colombia | Intercambio de archivos ACH Colombia | Intercambio de archivos ACH Colombia | Intercambio de archivos ACH Colombia | **DRIFT** | heading → label canónico; metadata fallback → label canónico |
| 11 | `/ach-cycles/nacha/export` | Exportar archivo NACHA-M | NachaExportComponent | Exportación NACHA-M | Exportación NACHA-M | Exportación NACHA-M | Exportación NACHA-M | **DRIFT** | heading → label canónico; metadata fallback → label canónico |
| 12 | `/customers` | Clientes | CustomersListComponent | Clientes | Clientes | Clientes | — | **ALIGNED** | sin cambio |
| 13 | `/transactions/outgoing-monitoring` | Transacciones enviadas | OutgoingTransactionMonitoringListComponent | Monitoreo de transacciones de salida | Monitoreo de transacciones de salida | Transacciones de salida | Monitoreo de transacciones de salida; Transacciones de salida | **DRIFT** | heading → label canónico; metadata fallback → label canónico |
| 14 | `/incoming-nacha-command-center` | Seguimiento de archivos | NachaOperationalDashboardComponent | Seguimiento de archivos NACHA-M | Seguimiento de archivos NACHA-M -> Seguimiento de archivos NACHA-M | Seguimiento de archivos NACHA-M -> Seguimiento de archivos NACHA-M | Seguimiento de archivos NACHA-M | **DRIFT** | heading → label canónico; metadata fallback → label canónico |
| 15 | `/incoming-nacha-command-center/observability` | Indicadores operativos | IncomingNachaObservabilityPageComponent | Indicadores operativos NACHA-M | Indicadores operativos NACHA-M | Indicadores operativos | Indicadores operativos NACHA-M | **DRIFT** | heading → label canónico; metadata fallback → label canónico |
| 16 | `/incoming-nacha-command-center/queue` | Cola de procesamiento | IncomingNachaQueuePageComponent | Cola de procesamiento NACHA-M | Cola de procesamiento NACHA-M | Cola de procesamiento | Cola de procesamiento NACHA-M | **DRIFT** | heading → label canónico; metadata fallback → label canónico |
| 17 | `/incoming-nacha-command-center/orphan-resolution` | Devoluciones sin relación | IncomingNachaOrphansPageComponent | Devoluciones recibidas sin relación | Resolver devoluciones sin relación | Devoluciones sin relación | Devoluciones recibidas sin relación; Resolver devoluciones sin relación | **DRIFT** | heading → label canónico; metadata fallback → label canónico |
| 18 | `/ach-responses` | Bandeja de respuestas | AchResponseListPageComponent | Bandeja de respuestas ACH | Command Center Respuestas ACH -> Respuestas ACH | Respuestas ACH -> Bandeja | Bandeja de respuestas ACH; Command Center Respuestas ACH; Respuestas ACH; Bandeja | **DRIFT** | heading → label canónico; metadata fallback → label canónico |
| 19 | `/ach-responses/dashboard` | Resumen de respuestas | AchResponseDashboardPageComponent | Panel operativo ACH | Panel de respuestas ACH | Panel | Panel operativo ACH; Panel de respuestas ACH; Panel | **DRIFT** | heading → label canónico; metadata fallback → label canónico |
| 20 | `/ach-responses/manual-review` | Revisión manual | AchResponseManualReviewPageComponent | Revisión manual de respuestas ACH | Revisión manual ACH | Revisión manual | Revisión manual de respuestas ACH; Revisión manual ACH | **DRIFT** | heading → label canónico; metadata fallback → label canónico |
| 21 | `/ach-responses/audit` | Historial de respuestas | AchResponseAuditPageComponent | Auditoría de respuestas ACH | Auditoría de respuestas ACH | Auditoría | Auditoría de respuestas ACH; Auditoría | **DRIFT** | heading → label canónico; metadata fallback → label canónico |
| 22 | `/ach/reconciliation` | Conciliación | AchReconciliationConsoleComponent | Consola de conciliación ACH | Consola de conciliación ACH solo lectura | Conciliación ACH | Consola de conciliación ACH; Consola de conciliación ACH solo lectura; Conciliación ACH | **DRIFT** | heading → label canónico; metadata fallback → label canónico |
| 23 | `/clearing-houses` | Cámaras compensadoras | ClearingHousesComponent | Centro administrativo de cámaras | Cámaras compensadoras | Cámaras compensadoras | Centro administrativo de cámaras | **DRIFT** | heading → label canónico |
| 24 | `/catalogs/clearing-house-preferences` | Prioridades por cámara | ClearingHousePreferencesComponent | Prioridades por cámara compensadora | Prioridades cámaras | Prioridades cámaras | Prioridades por cámara compensadora; Prioridades cámaras | **DRIFT** | heading → label canónico; metadata fallback → label canónico |
| 25 | `/ach-cycles` | Ciclos de operación | AchCycleListComponent | Configuración de ciclos | Ciclos ACH | Ciclos ACH | Configuración de ciclos; Ciclos ACH | **DRIFT** | heading → label canónico; metadata fallback → label canónico |
| 26 | `/catalogs/bank-holidays` | Días no hábiles | BankHolidaysComponent | Festivos nacionales | Festivos bancarios | Festivos bancarios | Festivos nacionales; Festivos bancarios | **DRIFT** | heading → label canónico; metadata fallback → label canónico |
| 27 | `/operational-restrictions` | Restricciones operativas | OperationalRestrictionsComponent | RESTRICCIONES OPERACIONALES CFA | Restricciones operacionales CFA | Restricciones operacionales CFA | RESTRICCIONES OPERACIONALES CFA; Restricciones operacionales CFA | **DRIFT** | heading → label canónico; metadata fallback → label canónico |
| 28 | `/nacha-config-admin` | Configuración de archivos NACHA-M | redirect/structural | — | Administración de perfiles NACHA-M | Configuración NACHA-M | Administración de perfiles NACHA-M; Configuración NACHA-M | **DRIFT** | metadata fallback → label canónico |
| 29 | `/nacha-config-admin/perfiles` | Perfiles oficiales | NachaConfigProfilesPageComponent | Perfiles de configuración NACHA-M | Configuración NACHA-M | Configuración NACHA-M | Perfiles de configuración NACHA-M; Configuración NACHA-M | **DRIFT** | heading → label canónico; metadata fallback → label canónico |
| 30 | `/nacha-config-admin/records` | Registros oficiales | NachaConfigLegacyWorkspaceRedirectComponent | — (redirect técnico) | Configuración NACHA-M | Configuración NACHA-M | Configuración NACHA-M | **CONTEXTUAL_ONLY** | Redirect técnico conservado; no se fabrica heading. |
| 31 | `/nacha-config-admin/variants-fields` | Campos y variantes | NachaConfigLegacyWorkspaceRedirectComponent | — (redirect técnico) | Configuración NACHA-M | Configuración NACHA-M | Configuración NACHA-M | **CONTEXTUAL_ONLY** | Redirect técnico conservado; no se fabrica heading. |
| 32 | `/catalogs/financial-institutions` | Entidades financieras | FinancialInstitutionsComponent | Instituciones financieras | Instituciones financieras | Instituciones financieras | Instituciones financieras | **DRIFT** | heading → label canónico; metadata fallback → label canónico |
| 33 | `/catalogs/company-entry-descriptions` | Conceptos de lote | CompanyEntryDescriptionAdminComponent | Conceptos de lote | Conceptos de lote | Conceptos de lote | — | **ALIGNED** | sin cambio |
| 34 | `/catalogs/document-types` | Tipos de documento | CatalogTypesAdminComponent | Tipos de documento | Tipos de documento | Tipos de documento | — | **ALIGNED** | sin cambio |
| 35 | `/catalogs/person-types` | Tipos de persona | CatalogTypesAdminComponent | Tipos de persona | Tipos de persona | Tipos de persona | — | **ALIGNED** | sin cambio |
| 36 | `/catalogs/gender-types` | Tipos de género | CatalogTypesAdminComponent | Tipos de género | Tipos de género | Tipos de género | — | **ALIGNED** | sin cambio |
| 37 | `/catalogs/phone-types` | Tipos de teléfono | CatalogTypesAdminComponent | Tipos de teléfono | Tipos de teléfono | Tipos de teléfono | — | **ALIGNED** | sin cambio |
| 38 | `/catalogs/email-types` | Tipos de correo | CatalogTypesAdminComponent | Tipos de correo | Tipos de correo | Tipos de correo | — | **ALIGNED** | sin cambio |
| 39 | `/catalogs/address-types` | Tipos de dirección | CatalogTypesAdminComponent | Tipos de dirección | Tipos de dirección | Tipos de dirección | — | **ALIGNED** | sin cambio |
| 40 | `/catalogs/transaction-codes` | Códigos de transacción | CatalogTypesAdminComponent | Códigos de transacción ACH | Códigos de transacción ACH | Códigos de transacción ACH | Códigos de transacción ACH | **DRIFT** | heading → label canónico; metadata fallback → label canónico |
| 41 | `/integraciones/contrato` | Contrato del servicio | ContractVersionsPageComponent | Versiones contractuales | Contrato del servicio | Contrato del servicio | Versiones contractuales | **DRIFT** | heading → label canónico |
| 42 | `/integraciones/relacion-informacion` | Relación de información | MappingSetsPageComponent | Relación de información | Relación de información | Relación de información | — | **ALIGNED** | sin cambio |
| 43 | `/integraciones/conexion` | Conexión entidad participante | SoapIntegrationSettingsComponent | Conexión técnica avanzada | Conexión CFA | Conexión CFA | Conexión técnica avanzada; Conexión CFA | **DRIFT** | heading → label canónico; metadata fallback → label canónico |
| 44 | `/administracion/mft-ach` | Configuración del intercambio de archivos | AchColombiaMftAdministrationComponent | Administración del intercambio de archivos | Administración MFT ACH | MFT ACH | Administración del intercambio de archivos; Administración MFT ACH; MFT ACH | **DRIFT** | heading → label canónico; metadata fallback → label canónico |
| 45 | `/ach-responses/status-mappings` | Homologación de estados | AchResponseStatusMappingsPageComponent | Homologación de estados ACH | Homologaciones ACH | Homologaciones | Homologación de estados ACH; Homologaciones ACH; Homologaciones | **DRIFT** | heading → label canónico; metadata fallback → label canónico |
| 46 | `/nacha-security/certificates` | Certificados digitales | NachaCertificateManagerComponent | Administración de certificados de seguridad | Certificados de seguridad NACHA-M | Certificados de seguridad NACHA-M | Administración de certificados de seguridad; Certificados de seguridad NACHA-M | **DRIFT** | heading → label canónico; metadata fallback → label canónico |
| 47 | `/nacha-security/sobre-digital` | Cifrar y descifrar archivos | DigitalEnvelopeToolComponent | Sobre digital | Sobre digital NACHA-M | Sobre digital NACHA-M | Sobre digital; Sobre digital NACHA-M | **DRIFT** | heading → label canónico; metadata fallback → label canónico |
| 48 | `/reports` | Reportes | ReportsHomeComponent | Central de Reportes ACH | Reportes ACH -> Reportes | Reportes -> Reportes | Central de Reportes ACH; Reportes ACH | **DRIFT** | heading → label canónico; metadata fallback → label canónico |
| 49 | `/reports/sent` | Transacciones enviadas | ReportListPageComponent | Enviados | Enviados | — | Enviados | **DRIFT** | heading → label canónico; metadata fallback → label canónico |
| 50 | `/reports/received` | Transacciones recibidas | ReportListPageComponent | Recibidos | Recibidos | — | Recibidos | **DRIFT** | heading → label canónico; metadata fallback → label canónico |
| 51 | `/reports/returns` | Devoluciones | ReportListPageComponent | Devoluciones | Devoluciones | — | — | **ALIGNED** | sin cambio |
| 52 | `/reports/rejections` | Rechazos | ReportListPageComponent | Rechazos | Rechazos | — | — | **ALIGNED** | sin cambio |
| 53 | `/reports/files` | Archivos NACHA-M | ReportListPageComponent | Archivos | Archivos | — | Archivos | **DRIFT** | heading → label canónico; metadata fallback → label canónico |
| 54 | `/reports/cycles` | Ciclos | ReportListPageComponent | Ciclos | Ciclos | — | — | **ALIGNED** | sin cambio |
| 55 | `/reports/reconciliation` | Conciliación | ReconciliationReportComponent | Conciliación | Conciliación | — | — | **ALIGNED** | sin cambio |
| 56 | `/reports/traceability` | Trazabilidad ACH | TraceabilityReportComponent | Reporte de trazabilidad ACH | Reporte de trazabilidad ACH | Trazabilidad ACH | Reporte de trazabilidad ACH | **DRIFT** | heading → label canónico; metadata fallback → label canónico |
| 57 | `/reports/history` | Histórico | ReportListPageComponent | Histórico | Histórico | — | — | **ALIGNED** | sin cambio |
| 58 | `/reports/audit` | Auditoría | ReportListPageComponent | Auditoría | Auditoría | — | — | **ALIGNED** | sin cambio |
| 59 | `/administracion` | Administración | sin componente propio | — (sin heading de nombre de pantalla) | — | — | — | **NO_HEADING** | Sin heading local de nombre de pantalla; se conserva el título global de MainLayout. |
| 60 | `/users` | Usuarios y acceso | UsersListComponent | Administración de usuarios | Administración de usuarios | Usuarios | Administración de usuarios; Usuarios | **DRIFT** | heading → label canónico; metadata fallback → label canónico |
| 61 | `/users/list` | Administrar usuarios | UsersListComponent | Administración de usuarios | Administración de usuarios | Administrar usuarios | Administración de usuarios | **DRIFT** | heading → label canónico; metadata fallback → label canónico |
| 62 | `/users/branding` | Identidad visual | BrandingSettingsComponent | Identidad visual | Identidad visual | Identidad visual | — | **ALIGNED** | sin cambio |
| 63 | `/users/password-rules` | Reglas de contraseña | PasswordRulesSettingsComponent | Reglas de contraseña | Reglas de contraseña | Reglas de contraseña | — | **ALIGNED** | sin cambio |
| 64 | `/users/login-lockout` | Bloqueo de acceso | LoginLockoutSettingsComponent | Bloqueo de acceso | Bloqueo de acceso | Bloqueo de acceso | — | **ALIGNED** | sin cambio |
| 65 | `/scheduler/tasks` | Tareas programadas | TaskDefinitionsComponent | Tareas programadas | Tareas programadas | Tareas programadas | — | **ALIGNED** | sin cambio |
| 66 | `/logs` | Registros del sistema | redirect/structural | — (sin heading de nombre de pantalla) | — | — | — | **NO_HEADING** | Sin heading local de nombre de pantalla; se conserva el título global de MainLayout. |
| 67 | `/audit-logs` | Registro de auditoría | AuditLogComponent | Registro de auditoría | Registro de auditoría | Registro de auditoría | — | **ALIGNED** | sin cambio |
| 68 | `/auth-logs` | Registro de accesos | AuthLogComponent | Registro de autenticaciones | Registro de autenticaciones | Registro de autenticaciones | Registro de autenticaciones | **DRIFT** | heading → label canónico; metadata fallback → label canónico |
| 69 | `/navigation-logs` | Registro de navegación | NavigationLogComponent | Log de navegación | Log de navegación | Log de navegación | Log de navegación | **DRIFT** | heading → label canónico; metadata fallback → label canónico |
| 70 | `/navigation/menu-items` | Organización del menú | NavigationMenuComponent | Administración del menú | Administrar menú de navegación | Menús | Administración del menú; Administrar menú de navegación; Menús | **DRIFT** | heading → label canónico; metadata fallback → label canónico |
| 71 | `/payment-rail-capability-registry` | Capacidades de la plataforma | PaymentRailCapabilityRegistryPageComponent | Capability Registry por riel | Capability Registry multi-riel (solo lectura) | Capability Registry | Capability Registry por riel; Capability Registry multi-riel (solo lectura); Capability Registry | **DRIFT** | heading → label canónico; metadata fallback → label canónico |
| 72 | `/uat` | Herramientas de prueba | redirect/structural | — | Simuladores UAT | UAT | Simuladores UAT; UAT | **DRIFT** | metadata fallback → label canónico |
| 73 | `/uat/nacha-inbound-simulator` | Simulador de archivos recibidos | NachaInboundSimulatorComponent | Simular respuesta de otra entidad | Simulador NACHA-M de entrada | Simulador NACHA-M de entrada | Simular respuesta de otra entidad; Simulador NACHA-M de entrada | **DRIFT** | heading → label canónico; metadata fallback → label canónico |
| 74 | `/ach/nacha/soap-uat-console` | Pruebas de servicios | NachaSoapUatConsoleComponent | Consola SOAP/UAT solo lectura | Consola SOAP/UAT solo lectura | Consola SOAP/UAT | Consola SOAP/UAT solo lectura; Consola SOAP/UAT | **DRIFT** | heading → label canónico; metadata fallback → label canónico |
| 75 | `/transactions` | Operación | redirect/structural | — | Transacciones ACH | Transacciones | Transacciones ACH; Transacciones | **DRIFT** | metadata fallback → label canónico |
| 76 | `/seguimiento-control` | Seguimiento y control | sin componente propio | — (sin heading de nombre de pantalla) | — | — | — | **NO_HEADING** | Sin heading local de nombre de pantalla; se conserva el título global de MainLayout. |
| 77 | `/seguimiento-control/respuestas-transacciones` | Respuestas de transacciones | sin componente propio | — (sin heading de nombre de pantalla) | — | — | — | **NO_HEADING** | Sin heading local de nombre de pantalla; se conserva el título global de MainLayout. |
| 78 | `/administracion-parametros` | Administración de parámetros | sin componente propio | — (sin heading de nombre de pantalla) | — | — | — | **NO_HEADING** | Sin heading local de nombre de pantalla; se conserva el título global de MainLayout. |
| 79 | `/catalogs` | Catálogos | CatalogsListComponent | — (sin heading de nombre de pantalla) | Catálogos | Catálogos | — | **NO_HEADING** | Sin heading local de nombre de pantalla; se conserva el título global de MainLayout. |
| 80 | `/integraciones` | Conexiones | IntegrationWorkspaceComponent | Centro de integración CFA | Centro de integración CFA | Centro de integración CFA | Centro de integración CFA | **DRIFT** | heading → label canónico; metadata fallback → label canónico |
| 81 | `/nacha-security` | Seguridad de archivos | redirect/structural | — | Seguridad NACHA | Seguridad NACHA | Seguridad NACHA | **DRIFT** | metadata fallback → label canónico |

## Headings principales modificados

Las siguientes 47 superficies tenían un heading de nombre de pantalla distinto del label canónico. Se cambió sólo esa pieza de naming:

- `/transactions/list`: `Transacciones` → `Consultar transacciones`
- `/transactions/create`: `Crear transacción ACH` → `Crear transacción`
- `/transactions/bulk-ingestion/upload`: `Carga masiva por archivo` → `Cargar transacciones desde archivo`
- `/transactions/bulk-ingestion/tracking`: `Seguimiento de lotes masivos` → `Seguimiento de cargas`
- `/transactions/returns`: `Devoluciones ACH` → `Devoluciones`
- `/transactions/returns-ror`: `Devolución de devolución` → `Devolución de una devolución`
- `/ach-claims`: `Reclamaciones ACH` → `Reclamaciones`
- `/ach-colombia/file-exchange`: `Intercambio de archivos ACH Colombia` → `Intercambio de archivos`
- `/ach-cycles/nacha/export`: `Exportación NACHA-M` → `Exportar archivo NACHA-M`
- `/transactions/outgoing-monitoring`: `Monitoreo de transacciones de salida` → `Transacciones enviadas`
- `/incoming-nacha-command-center`: `Seguimiento de archivos NACHA-M` → `Seguimiento de archivos`
- `/incoming-nacha-command-center/observability`: `Indicadores operativos NACHA-M` → `Indicadores operativos`
- `/incoming-nacha-command-center/queue`: `Cola de procesamiento NACHA-M` → `Cola de procesamiento`
- `/incoming-nacha-command-center/orphan-resolution`: `Devoluciones recibidas sin relación` → `Devoluciones sin relación`
- `/ach-responses`: `Bandeja de respuestas ACH` → `Bandeja de respuestas`
- `/ach-responses/dashboard`: `Panel operativo ACH` → `Resumen de respuestas`
- `/ach-responses/manual-review`: `Revisión manual de respuestas ACH` → `Revisión manual`
- `/ach-responses/audit`: `Auditoría de respuestas ACH` → `Historial de respuestas`
- `/ach/reconciliation`: `Consola de conciliación ACH` → `Conciliación`
- `/clearing-houses`: `Centro administrativo de cámaras` → `Cámaras compensadoras`
- `/catalogs/clearing-house-preferences`: `Prioridades por cámara compensadora` → `Prioridades por cámara`
- `/ach-cycles`: `Configuración de ciclos` → `Ciclos de operación`
- `/catalogs/bank-holidays`: `Festivos nacionales` → `Días no hábiles`
- `/operational-restrictions`: `RESTRICCIONES OPERACIONALES CFA` → `Restricciones operativas`
- `/nacha-config-admin/perfiles`: `Perfiles de configuración NACHA-M` → `Perfiles oficiales`
- `/catalogs/financial-institutions`: `Instituciones financieras` → `Entidades financieras`
- `/catalogs/transaction-codes`: `Códigos de transacción ACH` → `Códigos de transacción`
- `/integraciones/contrato`: `Versiones contractuales` → `Contrato del servicio`
- `/integraciones/conexion`: `Conexión técnica avanzada` → `Conexión entidad participante`
- `/administracion/mft-ach`: `Administración del intercambio de archivos` → `Configuración del intercambio de archivos`
- `/ach-responses/status-mappings`: `Homologación de estados ACH` → `Homologación de estados`
- `/nacha-security/certificates`: `Administración de certificados de seguridad` → `Certificados digitales`
- `/nacha-security/sobre-digital`: `Sobre digital` → `Cifrar y descifrar archivos`
- `/reports`: `Central de Reportes ACH` → `Reportes`
- `/reports/sent`: `Enviados` → `Transacciones enviadas`
- `/reports/received`: `Recibidos` → `Transacciones recibidas`
- `/reports/files`: `Archivos` → `Archivos NACHA-M`
- `/reports/traceability`: `Reporte de trazabilidad ACH` → `Trazabilidad ACH`
- `/users`: `Administración de usuarios` → `Usuarios y acceso`
- `/users/list`: `Administración de usuarios` → `Administrar usuarios`
- `/auth-logs`: `Registro de autenticaciones` → `Registro de accesos`
- `/navigation-logs`: `Log de navegación` → `Registro de navegación`
- `/navigation/menu-items`: `Administración del menú` → `Organización del menú`
- `/payment-rail-capability-registry`: `Capability Registry por riel` → `Capacidades de la plataforma`
- `/uat/nacha-inbound-simulator`: `Simular respuesta de otra entidad` → `Simulador de archivos recibidos`
- `/ach/nacha/soap-uat-console`: `Consola SOAP/UAT solo lectura` → `Pruebas de servicios`
- `/integraciones`: `Centro de integración CFA` → `Conexiones`

Casos con tratamiento especial:

- `/integraciones`: el nombre principal provenía de `INTEGRATION_COPY.centerTitle`; se cambió `Centro de integración CFA` por `Conexiones`. Mensajes de error/ayuda que aún contienen `Centro de integración CFA` no son el heading principal y se dejaron para un incremento posterior.
- `/reports/*`: `ReportListPageComponent` comparte componente entre varios children; sólo se ajustaron las entradas `config.title` cuyo nombre de superficie divergía (`Enviados`, `Recibidos`, `Archivos`). No se tocaron columnas, filtros, subtítulos descriptivos o mensajes.
- `/users` y `/users/list`: ambas rutas comparten `UsersListComponent`. Un string estático no puede representar simultáneamente `Usuarios y acceso` y `Administrar usuarios`; por eso el heading inicia desde `route.snapshot.data.title` y se canonicaliza con `NavigationNamingService.canonicalLabelForRoute(...)` cuando llega el árbol backend. No hay `switch`, `if` por URL ni tabla route→label.
- `/dashboard`: `Bienvenido al panel ACH Interbank` se clasificó como contenido de bienvenida, no como heading de nombre de pantalla. El naming de ruta `Inicio` ya lo muestra `MainLayout`, por lo que no se reescribió ese copy.
- `/catalogs`: el `h2` local `Instituciones financieras` funciona como heading de una sección del landing, no como sustituto del nombre de página `Catálogos`; no se cambió.

## Route metadata fallback modificada

Se alinearon las 51 rutas menu-backed cuyo fallback tenía alias legacy o técnico. La metadata sigue siendo sólo fallback técnico; después de cargar navegación, backend continúa ganando.

- `/transactions/list`: `title=Transacciones` / `breadcrumb=Transacciones` → fallback canónico `Consultar transacciones` (en los campos que existen para esa route).
- `/transactions/bulk-ingestion/upload`: `title=Carga masiva por archivo` / `breadcrumb=Carga masiva por archivo` → fallback canónico `Cargar transacciones desde archivo` (en los campos que existen para esa route).
- `/transactions/bulk-ingestion/tracking`: `title=Seguimiento de lotes` / `breadcrumb=Seguimiento de lotes` → fallback canónico `Seguimiento de cargas` (en los campos que existen para esa route).
- `/transactions/nacha-upload`: `title=Cargar NACHA-M` / `breadcrumb=Cargar NACHA-M` → fallback canónico `Cargar archivo NACHA-M` (en los campos que existen para esa route).
- `/transactions/returns`: `title=Devoluciones ACH` / `breadcrumb=Devoluciones ACH` → fallback canónico `Devoluciones` (en los campos que existen para esa route).
- `/transactions/returns-ror`: `title=Devolución de devolución` / `breadcrumb=Devolución de devolución` → fallback canónico `Devolución de una devolución` (en los campos que existen para esa route).
- `/ach-claims`: `title=Reclamaciones ACH` / `breadcrumb=Reclamaciones ACH` → fallback canónico `Reclamaciones` (en los campos que existen para esa route).
- `/ach-colombia/file-exchange`: `title=Intercambio de archivos ACH Colombia` / `breadcrumb=Intercambio de archivos ACH Colombia` → fallback canónico `Intercambio de archivos` (en los campos que existen para esa route).
- `/ach-cycles/nacha/export`: `title=Exportación NACHA-M` / `breadcrumb=Exportación NACHA-M` → fallback canónico `Exportar archivo NACHA-M` (en los campos que existen para esa route).
- `/transactions/outgoing-monitoring`: `title=Monitoreo de transacciones de salida` / `breadcrumb=Transacciones de salida` → fallback canónico `Transacciones enviadas` (en los campos que existen para esa route).
- `/incoming-nacha-command-center`: `title=Seguimiento de archivos NACHA-M -> Seguimiento de archivos NACHA-M` / `breadcrumb=Seguimiento de archivos NACHA-M -> Seguimiento de archivos NACHA-M` → fallback canónico `Seguimiento de archivos` (en los campos que existen para esa route).
- `/incoming-nacha-command-center/observability`: `title=Indicadores operativos NACHA-M` / `breadcrumb=Indicadores operativos` → fallback canónico `Indicadores operativos` (en los campos que existen para esa route).
- `/incoming-nacha-command-center/queue`: `title=Cola de procesamiento NACHA-M` / `breadcrumb=Cola de procesamiento` → fallback canónico `Cola de procesamiento` (en los campos que existen para esa route).
- `/incoming-nacha-command-center/orphan-resolution`: `title=Resolver devoluciones sin relación` / `breadcrumb=Devoluciones sin relación` → fallback canónico `Devoluciones sin relación` (en los campos que existen para esa route).
- `/ach-responses`: `title=Command Center Respuestas ACH -> Respuestas ACH` / `breadcrumb=Respuestas ACH -> Bandeja` → fallback canónico `Bandeja de respuestas` (en los campos que existen para esa route).
- `/ach-responses/dashboard`: `title=Panel de respuestas ACH` / `breadcrumb=Panel` → fallback canónico `Resumen de respuestas` (en los campos que existen para esa route).
- `/ach-responses/manual-review`: `title=Revisión manual ACH` / `breadcrumb=Revisión manual` → fallback canónico `Revisión manual` (en los campos que existen para esa route).
- `/ach-responses/audit`: `title=Auditoría de respuestas ACH` / `breadcrumb=Auditoría` → fallback canónico `Historial de respuestas` (en los campos que existen para esa route).
- `/ach/reconciliation`: `title=Consola de conciliación ACH solo lectura` / `breadcrumb=Conciliación ACH` → fallback canónico `Conciliación` (en los campos que existen para esa route).
- `/catalogs/clearing-house-preferences`: `title=Prioridades cámaras` / `breadcrumb=Prioridades cámaras` → fallback canónico `Prioridades por cámara` (en los campos que existen para esa route).
- `/ach-cycles`: `title=Ciclos ACH` / `breadcrumb=Ciclos ACH` → fallback canónico `Ciclos de operación` (en los campos que existen para esa route).
- `/catalogs/bank-holidays`: `title=Festivos bancarios` / `breadcrumb=Festivos bancarios` → fallback canónico `Días no hábiles` (en los campos que existen para esa route).
- `/operational-restrictions`: `title=Restricciones operacionales CFA` / `breadcrumb=Restricciones operacionales CFA` → fallback canónico `Restricciones operativas` (en los campos que existen para esa route).
- `/nacha-config-admin`: `title=Administración de perfiles NACHA-M` / `breadcrumb=Configuración NACHA-M` → fallback canónico `Configuración de archivos NACHA-M` (en los campos que existen para esa route).
- `/nacha-config-admin/perfiles`: `title=Configuración NACHA-M` / `breadcrumb=Configuración NACHA-M` → fallback canónico `Perfiles oficiales` (en los campos que existen para esa route).
- `/nacha-config-admin/records`: `title=Configuración NACHA-M` / `breadcrumb=Configuración NACHA-M` → fallback canónico `Registros oficiales` (en los campos que existen para esa route).
- `/nacha-config-admin/variants-fields`: `title=Configuración NACHA-M` / `breadcrumb=Configuración NACHA-M` → fallback canónico `Campos y variantes` (en los campos que existen para esa route).
- `/catalogs/financial-institutions`: `title=Instituciones financieras` / `breadcrumb=Instituciones financieras` → fallback canónico `Entidades financieras` (en los campos que existen para esa route).
- `/catalogs/transaction-codes`: `title=Códigos de transacción ACH` / `breadcrumb=Códigos de transacción ACH` → fallback canónico `Códigos de transacción` (en los campos que existen para esa route).
- `/integraciones/conexion`: `title=Conexión CFA` / `breadcrumb=Conexión CFA` → fallback canónico `Conexión entidad participante` (en los campos que existen para esa route).
- `/administracion/mft-ach`: `title=Administración MFT ACH` / `breadcrumb=MFT ACH` → fallback canónico `Configuración del intercambio de archivos` (en los campos que existen para esa route).
- `/ach-responses/status-mappings`: `title=Homologaciones ACH` / `breadcrumb=Homologaciones` → fallback canónico `Homologación de estados` (en los campos que existen para esa route).
- `/nacha-security/certificates`: `title=Certificados de seguridad NACHA-M` / `breadcrumb=Certificados de seguridad NACHA-M` → fallback canónico `Certificados digitales` (en los campos que existen para esa route).
- `/nacha-security/sobre-digital`: `title=Sobre digital NACHA-M` / `breadcrumb=Sobre digital NACHA-M` → fallback canónico `Cifrar y descifrar archivos` (en los campos que existen para esa route).
- `/reports`: `title=Reportes ACH -> Reportes` / `breadcrumb=Reportes -> Reportes` → fallback canónico `Reportes` (en los campos que existen para esa route).
- `/reports/sent`: `title=Enviados` / `breadcrumb=—` → fallback canónico `Transacciones enviadas` (en los campos que existen para esa route).
- `/reports/received`: `title=Recibidos` / `breadcrumb=—` → fallback canónico `Transacciones recibidas` (en los campos que existen para esa route).
- `/reports/files`: `title=Archivos` / `breadcrumb=—` → fallback canónico `Archivos NACHA-M` (en los campos que existen para esa route).
- `/reports/traceability`: `title=Reporte de trazabilidad ACH` / `breadcrumb=Trazabilidad ACH` → fallback canónico `Trazabilidad ACH` (en los campos que existen para esa route).
- `/users`: `title=Administración de usuarios` / `breadcrumb=Usuarios` → fallback canónico `Usuarios y acceso` (en los campos que existen para esa route).
- `/users/list`: `title=Administración de usuarios` / `breadcrumb=Administrar usuarios` → fallback canónico `Administrar usuarios` (en los campos que existen para esa route).
- `/auth-logs`: `title=Registro de autenticaciones` / `breadcrumb=Registro de autenticaciones` → fallback canónico `Registro de accesos` (en los campos que existen para esa route).
- `/navigation-logs`: `title=Log de navegación` / `breadcrumb=Log de navegación` → fallback canónico `Registro de navegación` (en los campos que existen para esa route).
- `/navigation/menu-items`: `title=Administrar menú de navegación` / `breadcrumb=Menús` → fallback canónico `Organización del menú` (en los campos que existen para esa route).
- `/payment-rail-capability-registry`: `title=Capability Registry multi-riel (solo lectura)` / `breadcrumb=Capability Registry` → fallback canónico `Capacidades de la plataforma` (en los campos que existen para esa route).
- `/uat`: `title=Simuladores UAT` / `breadcrumb=UAT` → fallback canónico `Herramientas de prueba` (en los campos que existen para esa route).
- `/uat/nacha-inbound-simulator`: `title=Simulador NACHA-M de entrada` / `breadcrumb=Simulador NACHA-M de entrada` → fallback canónico `Simulador de archivos recibidos` (en los campos que existen para esa route).
- `/ach/nacha/soap-uat-console`: `title=Consola SOAP/UAT solo lectura` / `breadcrumb=Consola SOAP/UAT` → fallback canónico `Pruebas de servicios` (en los campos que existen para esa route).
- `/transactions`: `title=Transacciones ACH` / `breadcrumb=Transacciones` → fallback canónico `Operación` (en los campos que existen para esa route).
- `/integraciones`: `title=Centro de integración CFA` / `breadcrumb=Centro de integración CFA` → fallback canónico `Conexiones` (en los campos que existen para esa route).
- `/nacha-security`: `title=Seguridad NACHA` / `breadcrumb=Seguridad NACHA` → fallback canónico `Seguridad de archivos` (en los campos que existen para esa route).

Además, el wrapper técnico `/navigation` dejó de aportar `title`/`breadcrumb` legacy (`Menú de navegación` / `Navegación`) porque esa ruta no es un `MenuItem` canónico y sólo envuelve `/navigation/menu-items`. No se eliminó metadata contextual necesaria ni se cambió autorización.

## Rutas contextuales

NAV4.2B inspeccionó rutas no-menu para evitar regresión, pero no las humanizó de forma general. En particular:

- `/users/new` conserva `breadcrumb: Nuevo usuario` y `title: Crear usuario`.
- `/users/:id/edit` conserva `breadcrumb/title: Editar usuario`.
- `/users/:id/roles` conserva `breadcrumb: Perfiles de acceso` y `title: Administrar perfiles de acceso`.

El matcher NAV4.2A continúa usando los segmentos URL resueltos del `ActivatedRouteSnapshot`; no expone `:id` como label, no requiere MenuItem contextual y no agrega llamadas API para resolver nombres de entidades. Query params y fragment se siguen ignorando para el matching canónico.

## Focales

- `/reports`: heading/fallback `Reportes`; tras carga de menú, backend sigue produciendo `Reportes`; browser title continúa `ACH Interbank - Reportes`; landing funcional y sus 10 children no cambian.
- `/integraciones`: heading/fallback `Conexiones`; continúa FUNCTIONAL_PARENT con link navegable + toggle independiente.
- `/integraciones/contrato`: heading/fallback `Contrato del servicio`.
- `/integraciones/relacion-informacion`: heading/fallback `Relación de información`.
- `/integraciones/conexion`: heading/fallback `Conexión entidad participante`.
- `/administracion/mft-ach`: heading/fallback `Configuración del intercambio de archivos`; NAV4.2A sigue obteniendo `Administración de parámetros → Conexiones → Configuración del intercambio de archivos` desde ancestors backend, no desde el prefijo URL.
- `/users`: fallback `Usuarios y acceso`; heading efectivo se inicia con esa metadata y luego backend puede reemplazarlo dinámicamente. `/users/list` se comporta igual con `Administrar usuarios`.
- `/logs`: no se inventó landing/heading para el parent redirect; el naming canónico del parent sigue siendo `Registros del sistema` cuando proviene del árbol backend. Sus children mantienen sus nombres funcionales (`Registro de auditoría`, `Registro de accesos`, `Registro de navegación`).

## Evidencia de source of truth

Se agregó una prueba focal explícita con:

- route metadata fallback = `Fallback X`;
- backend label = `Canonical Y`;
- resultado después de cargar menú = `Canonical Y`.

También se conserva la prueba NAV4.2A de cambio sintético de label backend, child futuro, `/reports?x=1#section`, hierarchy de MFT y route contextual `/users/42/edit`. El resolver productivo `navigation-naming.util.ts` y `NavigationNamingService` quedaron byte-idénticos a NAV4.2A.

## No regresión NAV4.1A / NAV4.1B / NAV4.2A / seguridad

- `NavigationService` quedó byte-idéntico: sin filtros, replacement, reparenting, fabricación/eliminación de children y con sorting únicamente por `order`.
- `MenuRouteSurfaceService` quedó byte-idéntico: FUNCTIONAL_PARENT conserva link + toggle; STRUCTURAL_PARENT conserva expander.
- `NavigationNamingService` y `navigation-naming.util.ts` quedaron byte-idénticos; no cambió matching, hierarchy, query/fragment, `:id`, resincronización ni document title.
- `GetMenuForCurrentUserHandler`, `NavigationInformationArchitectureSeeder`, `FineGrainedGrantBackfillSeeder`, `LegacyFineGrainedRoleMigrationSeeder`, `permissionGuard` y `roleGuard` conservaron sus hashes. Orders 119 y 120 permanecen intactos.
- No se modificaron permisos, grants, roles, RolePermission, MenuItemPermission, MenuItemRole, policies P0/P1, controllers, guards, component permission gating, fallbacks ni role gates.

## Copy visible pendiente fuera de alcance

El inventario encontró copy que puede revisarse después, pero se dejó intencionalmente intacto. Ejemplos representativos:

- acciones/enlaces secundarios como `Volver al Centro de integración CFA`, `Carga masiva por archivo` y accesos a `Restricciones operacionales CFA`;
- mensajes/estados que aún mencionan `Centro de integración CFA`, `Sobre digital`, `Capability Registry` o `Configuración NACHA-M`;
- aria/labels de tablas como referencias a `Festivos bancarios`;
- headings contextuales o deep-copy como el eyebrow `Reclamaciones ACH` dentro de un detalle;
- términos técnicos legítimos SOAP, NACHA-M, MFT, ACH, SHA-256, WSDL, XSD y SFTP se preservan cuando describen tecnología o dominio.

No se modificaron masivamente `Save`, `Cancel`, `Delete`, `Retry`, `Upload`, labels de inputs, placeholders, table headers, toasts, confirm dialogs, validation messages, empty states o tooltips.

## Validaciones

- SHA-256 del ZIP de entrada NAV4.2A: `0736fdae5b474c8a73d90b0c5a04486f94546930aaab7bb62fcaa80ffd8f31a2` — **PASS**.
- Parser TypeScript 5.8.3 sobre **38 TS modificados** — **PASS**, 0 parse diagnostics.
- `scripts/validate-nav4-2b-menu-backed-copy.py` — **PASS, 81 assertions**; incluye hashes protegidos, focales de heading/metadata, rutas contextuales y ejecución Node del resolver TypeScript puro.
- Escaneo estático completo de metadata canónica: **79 registros de naming asociados a 76/81 rutas, 0 mismatches**; las 5 rutas restantes no poseen metadata propia y no se les fabricó.
- Escaneo de headings de nombre de pantalla posterior al cambio: **0 mismatches** contra los 81 labels canónicos, excluyendo explícitamente los 7 `NO_HEADING` y 2 redirects `CONTEXTUAL_ONLY`.
- `scripts/validate-solution-projects.py` — **PASS**, solución 7/7 y referencias válidas.
- Angular build/test/runtime — `NOT_RUN_ENVIRONMENT`: `web/ach-interbank-ui/node_modules` no está presente.
- .NET — `NOT_RUN_ENVIRONMENT`: `dotnet` no está disponible.

## Resultado y siguiente decisión

NAV4.2B puede considerarse **implementado a nivel diseño/código** y permanece `IMPLEMENTED_UNVERIFIED_ENVIRONMENT` hasta disponer del runtime. Dentro del alcance de heading principal + metadata fallback menu-backed, no queda un alias conocido que contradiga directamente NAV2.

No ejecutar otro incremento automáticamente. A partir del inventario real, el candidato focal recomendado es **`NAV4.2C — acciones y enlaces secundarios de navegación asociados a rutas menu-backed`**, porque los aliases restantes más claros están en CTAs/enlaces auxiliares y no justifican todavía una barrida SPA completa.

# NAV4.2C — acciones y enlaces secundarios de navegación asociados a rutas menu-backed — 2026-10-15

Estado: `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`

NAV4.2C parte únicamente de `ACHInterbank-R34-NAV-IA-NAV4.2B-FULL-PROJECT.zip`, SHA-256 `fe2f56f5d6ac8297d7c9ee68ec373f9c8f58f95bb1ed01825ee4a807614500f1`, sin Git. NAV2 y NAV3 permanecen `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`; NAV3.5C permanece **BLOCKED / NO EJECUTAR**; NAV4.1A/B y NAV4.2A/B permanecen `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`.

## Alcance e inventario

Se inspeccionaron **114 definiciones de controles navegacionales visibles**: 107 detectadas en templates/inline templates mediante `routerLink`, `router.navigate`, `navigateByUrl`, handlers de navegación y back controls, más 7 controles generados por código en grids. También se inspeccionaron las colecciones dinámicas que alimentan Reportes, Catálogos, CENIT, MainLayout y `ui-migas-pan`.

Clasificación del estado de entrada NAV4.2B:

- **33 `ALIGNED`**;
- **27 `DRIFT`**;
- **48 `CONTEXTUAL_NAVIGATION`**;
- **6 `FUNCTIONAL_ACTION_OUT_OF_SCOPE`**;
- **0 `TECHNICAL_TERM_LEGITIMATE` como clasificación primaria**; los términos técnicos legítimos aparecen dentro de controles/contexto de otras clases y se preservaron.

El inventario completo control por control —archivo, línea, copy, aria/title/tooltip, destino, condición menu-backed/contextual, label canónico, clasificación y resultado— quedó en `docs/ai/navigation-menu/phases/NAV4_2C_NAVIGATION_CONTROL_INVENTORY.md`.

## DRIFT corregido — old → new

Los **27** controles con drift se corrigieron sin cambiar su destino ni su handler:

1. `Volver a cámaras` → `Volver a Cámaras compensadoras` en Restricciones operativas.
2. `Consultar configuración de intercambio vigente` → `Consultar Configuración del intercambio de archivos` para `/administracion/mft-ach`.
3. `Restricciones operacionales CFA` → `Restricciones operativas` en el acceso desde Cámaras compensadoras.
4. `Volver a cámaras` → `Volver a Cámaras compensadoras` en Políticas transaccionales.
5. `Volver a la cola` → `Volver a Cola de procesamiento`.
6. `Volver al seguimiento` → `Volver a Seguimiento de archivos` desde Cola de procesamiento.
7. `Volver al seguimiento` → `Volver a Seguimiento de archivos` desde Devoluciones sin relación.
8. `Volver al seguimiento` → `Volver a Seguimiento de archivos` desde Indicadores operativos.
9. breadcrumb local `Seguridad de archivos` que enlazaba exactamente a `/nacha-security/certificates` → `Certificados digitales`.
10. `Ir a Certificados de seguridad NACHA-M` → `Ir a Certificados digitales`.
11. `Ir a la administración de certificados` → `Ir a Certificados digitales`.
12. `Volver al monitoreo` → `Volver a Transacciones enviadas`.
13. `Ver seguimiento` → `Ver Seguimiento de cargas`.
14. `Ir a seguimiento` → `Ir a Seguimiento de cargas`.
15. `Volver a cámaras` → `Volver a Cámaras compensadoras` en configuración de ciclos.
16. `Carga masiva por archivo` → `Cargar transacciones desde archivo` para el link a la pantalla de carga.
17. `Seguimiento lotes` → `Seguimiento de cargas`.
18. `Administrar conexión` → `Ver Conexión entidad participante`.
19. `Volver al Centro de integración CFA` → `Volver a Conexiones`.
20. `Ir a carga de archivos NACHA-M` → `Ir a Cargar archivo NACHA-M`.
21. tab `Ciclos` → `Ciclos de operación` para la route exacta `/ach-cycles`.
22. `Exportar NACHA-M` → `Exportar archivo NACHA-M` para el acceso a `/ach-cycles/nacha/export`.
23. `Volver a bandeja` → `Volver a Bandeja de respuestas`.
24. `Volver a cámaras` → `Volver a Cámaras compensadoras` en Fechas especiales.
25. card de catálogo `Códigos de transacción ACH` → `Códigos de transacción`.
26. acción de corrección `Ir a Certificados de seguridad NACHA-M` → `Ir a Certificados digitales`.
27. retorno compartido `Volver a cámaras` → `Volver a Cámaras compensadoras`; en ese mismo control se alinearon `aria-label` y `matTooltip` con el visible label.

## Source of truth y estrategia de implementación

NAV4.2A/B permanece vigente sin cambios:

- backend `MenuItem.label` + ancestors = source of truth para naming menu-backed una vez cargado el árbol;
- route metadata Angular = fallback técnico pre-menu;
- metadata contextual = naming de rutas non-menu.

NAV4.2C **no agrega mapa, switch ni catálogo global route→label**. `NavigationNamingService`, `NavigationService` y `MenuRouteSurfaceService` quedaron byte-idénticos.

No se inyectó `NavigationNamingService` en nuevos componentes para estos CTAs. Los 27 drifts pertenecían a controles ya inseparables de un destino concreto o a colecciones locales de navegación preexistentes; se corrigió únicamente el string local. Esta decisión evita crear dependencias sólo para copy estático y está dentro de la excepción aprobada para controles ligados a una sola superficie. MainLayout continúa siendo el caso de reutilización dinámica: sus links visibles usan directamente `item.label` del árbol backend. `ui-migas-pan` conserva su comportamiento genérico y el naming que recibe.

## Casos focales

### Conexiones

- El retorno explícito de `/integraciones/contrato` a `/integraciones` ahora muestra `Volver a Conexiones`.
- Los accesos a `Contrato del servicio` y `Relación de información` ya estaban alineados.
- El acceso a `/integraciones/conexion` ahora nombra la superficie `Conexión entidad participante`.
- No queda copy navegacional visible `Centro de integración CFA`. Las ocurrencias remanentes están en fixtures/tests de regresión o mensajes funcionales fuera de NAV4.2C.

### Configuración del intercambio de archivos

- El enlace desde Intercambio de archivos hacia `/administracion/mft-ach` ahora contiene el nombre canónico `Configuración del intercambio de archivos`.
- `MFT`, SFTP y otros términos técnicos se preservan dentro del contenido técnico de la pantalla.

### Operación

- El control de `transaction-list` que **navega** a `/transactions/bulk-ingestion/upload` ahora dice `Cargar transacciones desde archivo`.
- Los controles que navegan a `/transactions/bulk-ingestion/tracking` usan `Seguimiento de cargas`.
- La acción funcional real `(click)="upload()"` conserva `Enviar archivo`; no fue confundida con el link a la pantalla.
- `Abrir detalle de lote` continúa contextual (`/transactions/bulk-ingestion/:batchId`) y no se convirtió en MenuItem.

### Reportes

El landing `/reports` conserva su comportamiento NAV4.1B. Su único template genérico de card continúa mostrando `Abrir reporte`; los **10** modelos de reportes hijos ya tienen titles canónicos desde NAV4.2B, por lo que no fue necesario cambiar copy, rutas, orden, cards ni children.

### Rutas contextuales y back histórico

Los 48 controles `CONTEXTUAL_NAVIGATION` conservan su copy propio. Ejemplos: `Nuevo usuario`, `Editar usuario`, `Administrar perfiles de acceso`, detalles de respuestas, lotes, cola y perfiles NACHA-M. Un control genérico con `window.history.back()` continúa diciendo simplemente `Volver`; NAV4.2C no inventó un destino no garantizado.

## Acciones funcionales fuera de alcance

Las seis definiciones detectadas que combinan efecto funcional y navegación se dejaron intactas: `Nueva reclamación`, `Actualizar información`, `Limpiar filtros`, `Guardar perfiles`, `Clonar` y `Crear borrador`. También quedaron fuera acciones como `Enviar archivo`, `Descargar`, `Reintentar`, `Reprocesar`, `Archivar`, `Retirar archivo`, `Validar`, cifrado/descifrado y demás mutaciones/operaciones de dominio.

No se hizo barrida general de botones funcionales, formularios, labels/placeholders, tablas/columnas, mensajes/empty states ni tooltips/ARIA no navegacionales.

## No regresión NAV4.1A / NAV4.1B / NAV4.2A / NAV4.2B / seguridad

- `NavigationService` conserva SHA-256 `4df488ac45465a5283a02afe795fc64c4e5995a3415556d324275cc5e1d7ea93`.
- `MenuRouteSurfaceService` conserva SHA-256 `3a32dc7921e3c805e32fa9921e371109813aae9ba3a3173bda6a5b6b7030df72`.
- `NavigationNamingService` conserva SHA-256 `de6d4a402690956914a67f26466c7ea6c1cfac16eb717dbeb4d4b6f56e59db19` y su resolver puro `17e066e755f589ee818b6fa48fbcf3963ba14886a35cdf3187e5381e80368895`.
- El validador NAV4.2B continúa **PASS**; headings menu-backed y metadata fallback permanecen con **0 mismatches** dentro de su alcance.
- `/reports` y `/integraciones` continúan navegables como FUNCTIONAL_PARENT; no cambió link+toggle, active state, expansión, route paths, redirects, lazy loading, guards ni componentes.
- `GetMenuForCurrentUserHandler`, `NavigationInformationArchitectureSeeder`, seeders NAV3, guards, Orders 119/120 y fallbacks/role gates permanecen intactos.
- NAV3.5A/B permanecen intactos; NAV3.5C continúa **BLOCKED / NO EJECUTAR**.

## Copy pendiente por categoría

El incremento deja deliberadamente pendiente copy no navegacional:

- **acciones funcionales:** acciones de persistencia, upload/download, retry, clone, validate, publish/test y otras operaciones; el scan focal encontró al menos las seis definiciones híbridas documentadas y numerosas acciones puramente funcionales adicionales;
- **formularios:** labels, placeholders, hints y validaciones no fueron auditados/cambiados de forma general;
- **tablas/columnas:** headers, filtros y aria de tablas no fueron barridos; por ejemplo permanece `aria-label="Restricciones operacionales CFA"` en una tabla porque no es control navegacional;
- **mensajes/empty states:** permanecen mensajes funcionales como referencias al `Centro de integración CFA`, `Sobre digital` u otros aliases cuando no son navegación;
- **tooltips/aria no navegacional:** permanecen fuera de alcance salvo el accessible name del control focal corregido.

## Validaciones

- ZIP de entrada NAV4.2B SHA-256 `fe2f56f5d6ac8297d7c9ee68ec373f9c8f58f95bb1ed01825ee4a807614500f1` — **PASS**.
- `scripts/validate-nav4-2c-navigation-copy.py` — **PASS, 111 assertions**; incluye hashes protegidos, 27 drifts, handlers/destinos, separación de acciones funcionales, back genérico, rutas contextuales, inventario de 114 controles y regresión NAV4.2B.
- `scripts/validate-nav4-2b-menu-backed-copy.py` — **PASS, 81 assertions** durante la regresión NAV4.2C.
- Parser TypeScript 5.8.3 sobre **6 archivos TS modificados** — **PASS**, 0 error diagnostics.
- `scripts/validate-solution-projects.py` — **PASS**, solución 7/7, proyectos en disco 7/7 y referencias válidas.
- Angular build/test/runtime — `NOT_RUN_ENVIRONMENT`: `web/ach-interbank-ui/node_modules` está ausente.
- .NET — `NOT_RUN_ENVIRONMENT`: `dotnet` no está disponible.

## Resultado y siguiente decisión

NAV4.2C queda implementado a nivel diseño/código y permanece `IMPLEMENTED_UNVERIFIED_ENVIRONMENT` hasta disponer de runtime Angular/.NET. No se ejecuta ningún incremento posterior automáticamente.

El inventario muestra que el siguiente grupo visible más cercano es **copy de acciones funcionales**; por tanto el candidato recomendado, sujeto a decisión humana, es **`NAV4.2D — copy de acciones funcionales primarias/secundarias`**, con clasificación previa por efecto de dominio para no confundir navegación con mutaciones. No ejecutar sin aprobación explícita.


# NAV4.2D — copy de acciones funcionales primarias/secundarias — 2026-10-15

## Estado y límite

NAV4.2D queda `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`. El incremento sólo cambia copy visible/accesible del mismo control funcional. No cambia handlers, eventos, endpoints, payloads, side effects, permisos/gating, loading/disabled state, confirmaciones, validación, lógica de negocio ni navegación posterior. NAV2/NAV3 y NAV4.1A/B/NAV4.2A/B/C conservan sus estados; NAV3.5C continúa `BLOCKED / NO EJECUTAR`.

## Inventario y clasificación

Se inspeccionaron **582 definiciones de control** en la SPA: 482 `<button>` externos, 24 `<ui-boton>` externos, 61 `<button>` inline en producción TypeScript, 4 `<ui-boton>` inline, 7 botones creados programáticamente y 4 labels `textoReintentar`. **461** son acciones funcionales del universo NAV4.2D y **121** quedaron fuera por navegación pura/paginación/retorno/toggle o naturaleza no funcional.

Decisiones de entrada: **289 ALIGNED**, **45 DRIFT**, **14 TECHNICAL_TERM_LEGITIMATE**, **113 CONTEXTUAL_ACTION**, **121 OUT_OF_SCOPE**. Los 45 `DRIFT` fueron corregidos. El inventario reproducible completo, con archivo, handler, efecto/servicio, gating, categoría y old→new, está en `NAV4_2D_FUNCTIONAL_ACTION_INVENTORY.md`.

Distribución funcional de las 461 acciones: CREATE 30; UPDATE 3; DELETE 10; SAVE 31; UPLOAD 4; DOWNLOAD 9; GENERATE 7; EXPORT 2; IMPORT 2; RETRY 39; CANCEL_OPERATION 1; VALIDATE 6; PREVIEW 6; PUBLISH 2; CLONE 2; ENCRYPT 4; DECRYPT 2; TEST_CONNECTION 1; REFRESH 23; FILTER 6; CLEAR_FILTERS 35; SEARCH 22; ACTIVATE 3; DEACTIVATE 18; REVOKE 2; OTHER_FUNCTIONAL 191.

## Convención resultante

- `Guardar` permanece cuando el contexto es inequívoco; se agrega objeto cuando evita ambigüedad (`Guardar perfiles de acceso`, `Guardar configuración`, `Guardar horarios`).
- Creación usa `Crear <objeto>`; Draft mantiene `Crear borrador` o `Crear versión en borrador` según el objeto persistido.
- Reloads/read-only refresh se expresan como `Actualizar`/`Actualizar configuración`; no se usa esa convención para mutaciones.
- `Eliminar`, `Desactivar`, `Revocar` y `Cancelar operación` permanecen distintos cuando sus efectos persistentes son distintos.
- Upload real describe su operación (`Enviar archivo`, `Cargar certificado…`) y no adopta el nombre de la pantalla.
- `Descargar` y `Exportar` siguen diferenciados por efecto.
- Retry de control se normaliza a `Reintentar`; mensajes explicativos no se reescriben.
- `Validar`, `Vista previa`/comparación y `Publicar` permanecen semánticamente distintos.
- Clone nombra el objeto cuando aporta precisión (`Clonar configuración`).
- Generate conserva el resultante (`Generar archivo de auditoría`, `Generar archivo NACHA-M`).
- ACH, NACHA-M, MFT, SFTP, SOAP, WSDL, XSD y SHA-256 permanecen términos técnicos legítimos.

## DRIFT corregido

El detalle exacto de los 45 cambios old→new está en la tabla `DRIFT corregido — old → new` de `NAV4_2D_FUNCTIONAL_ACTION_INVENTORY.md`. Por grupos: **12** controles retry legacy (`Intentar nuevamente`/`Volver a intentar`) pasan a `Reintentar`; **15** acciones `Limpiar` que limpian conjuntos de filtros pasan a `Limpiar filtros`; **2** acciones `Filtrar` pasan a `Aplicar filtros`; **6** reloads/read-only actions se alinean a `Actualizar`/`Actualizar configuración`; y **10** correcciones focales precisan dominio/objeto (`Guardar perfiles de acceso`, `Analizar archivo`, `Validar operación`, `Crear versión en borrador`, `metadatos`, `Clonar configuración`, certificado de firma y descifrado, `Generar archivo de auditoría`, `Cerrar sesión`). La tabla focal conserva cada archivo, línea, handler y servicio/efecto.

## Seis controles híbridos heredados de NAV4.2C

- `Nueva reclamación`: sin cambio; `openCreateDialog()` inicia creación contextual.
- `Actualizar información`: sin cambio; `refresh()` vuelve a leer el resumen y no muta dominio.
- `Limpiar filtros`: sin cambio donde ya estaba alineado; variantes equivalentes `Limpiar` se alinearon.
- `Guardar perfiles` → `Guardar perfiles de acceso`; `save()` mantiene `usersApi.assignRoles(...)` y la navegación posterior intacta.
- `Clonar` → `Clonar configuración`; `clone()` mantiene `api.clone(...)` y la navegación posterior intacta.
- `Crear borrador`: sin cambio; `crearBorrador()` persiste el Draft.

## Evidencia de no logic delta

`NAV4_2D_COPY_DELTA.json` registra exactamente **49 líneas** cambiadas en **39 archivos SPA**: 38 archivos de producción y 1 spec ajustado únicamente porque verificaba el literal de acción modificado. El validador NAV4.2D revierte sólo esas líneas sobre el workspace actual y exige que el hash agregado completo de `web/ach-interbank-ui/src/app` vuelva a `95493edb9108bf6ff328adc33bf5e98adbbb9c9ea283efda5d890c3d3721004d`, el árbol NAV4.2C. Así se demuestra estáticamente que no existe delta adicional de lógica.

Los handlers/eventos, service methods/endpoints, payloads, condiciones disabled/loading, permisos/gating, invocaciones de confirmación y navegación posterior permanecen byte-idénticos fuera de los strings documentados. El caso bulk ingestion conserva `Enviar archivo`; MFT conserva `Probar conexión`; cifrado/descifrado permanecen acciones distintas; preview-change de Conexión entidad participante no adquiere copy que sugiera ejecución SOAP real.

## No regresión NAV4

`NavigationService`, `MenuRouteSurfaceService`, `NavigationNamingService` y `navigation-naming.util.ts` permanecen byte-idénticos. `GetMenuForCurrentUserHandler`, `NavigationInformationArchitectureSeeder`, seeders NAV3, guards y Orders 119/120 permanecen intactos. NAV4.2B mantiene 0 mismatches de headings/metadata dentro de su alcance y NAV4.2C mantiene 0 drift navegacional dentro del suyo. No se agregó route→label map ni switch de URLs, y `NavigationNamingService` no se usa para verbos funcionales.

## Validaciones

- input NAV4.2C SHA-256 `36380e5fa7843611928cd35163e25b6f03efc7384ff18cfb8b3b9e82cd3dcda0` — PASS;
- `scripts/validate-nav4-2d-functional-action-copy.py` — **PASS, 210 assertions**;
- `scripts/validate-nav4-2c-navigation-copy.py` — **PASS, 111 assertions**;
- `scripts/validate-nav4-2b-menu-backed-copy.py` — **PASS, 81 assertions**;
- parser TypeScript 5.8.3 sobre los **2 TS modificados** — **PASS**, 0 diagnostics;
- `scripts/validate-solution-projects.py` — **PASS**, 7/7;
- Angular build/test/runtime — `NOT_RUN_ENVIRONMENT` por ausencia de `node_modules`;
- .NET — `NOT_RUN_ENVIRONMENT` por ausencia de `dotnet`.

## Copy restante y siguiente decisión

NAV4.2D no humaniza formularios, tablas/columnas, mensajes/empty states ni tooltips/ARIA no funcional. Como inventario estructural de superficie pendiente se detectan 394 `mat-label`, 88 `<label>`, 54 placeholders y 78 `mat-hint` en la SPA; son **614 hooks de copy de formulario** potenciales, no 614 drifts confirmados. También existen superficies de tabla y mensajes de volumen relevante, pero requieren su propio inventario semántico y no fueron modificadas.

El siguiente candidato recomendado, sujeto a decisión humana, es **`NAV4.2E — labels/placeholders/hints de formularios`** porque constituye el siguiente bloque de copy visible de alto volumen más claramente delimitable sin mezclar mensajes ni tablas. No ejecutar automáticamente.


# NAV4.2E — labels, placeholders y hints de formularios — 2026-10-15

## Estado y alcance

NAV4.2E queda `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`. Se tomó únicamente `ACHInterbank-R34-NAV-IA-NAV4.2D-FULL-PROJECT.zip` (SHA-256 `0da1a5f2bba0b00e5255d61c230708082f1a7cbe47edfa62a4a706867240d8d2`) y no se usó Git. El incremento se limita al copy que nombra/orienta fields de captura o selección: labels, placeholders, hints y nombre accesible equivalente del mismo field. No toca botones/acciones, navegación, tablas, mensajes, `mat-error`, DTOs, validators, servicios, APIs ni seguridad.

El conteo de **614 hooks potenciales** reportado por NAV4.2D era deliberadamente estructural y preliminar. El inventario exacto NAV4.2E amplió la detección a labels equivalentes de `ui-campo-*`, checkbox/radio/toggle y accessible names propios del field; el universo real inspeccionado es **675 hooks** que representan **517 fields físicos únicos**.

## Inventario exacto

Distribución por hook:

- `MAT_LABEL`: **370**
- `HTML_LABEL`: **143**
- `PLACEHOLDER`: **47**
- `HINT`: **75**
- `ACCESSIBLE_FIELD_NAME`: **40**

Clasificación del estado de entrada NAV4.2D:

- `ALIGNED`: **355**
- `DRIFT`: **36**
- `TECHNICAL_TERM_LEGITIMATE`: **24**
- `DOMAIN_TERM_LEGITIMATE`: **167**
- `CONTEXTUAL_FIELD`: **58**
- `DYNAMIC_DATA`: **35**
- `OUT_OF_SCOPE`: **0** dentro del inventario focal; las superficies excluidas se escanearon aparte y no se mezclan con estos hooks.

Los 36 `DRIFT` fueron corregidos. El detalle fila por fila está en `NAV4_2E_FORM_FIELD_INVENTORY.md`; el drift residual identificado dentro del scope de fields queda en **0**.

## Transiciones old → new

Los **36 hooks** corregidos corresponden a estas transiciones (las repeticiones indican hooks distintos):

- `PolicyVersion` → `Versión de política` (**2** labels).
- `Filtrar por PolicyVersion` → `Filtrar por versión de política`.
- `PolicyVersion del ciclo` → `Versión de política del ciclo`.
- `ClearingHouseCycleConfig` → `Ciclo versionado`.
- `El ID se toma del ciclo versionado; no se usan nombres ni IDs de cámara hardcodeados.` → `El ID se toma del ciclo versionado; no se usan nombres ni IDs de cámara definidos de forma fija.`
- `Semántica` → `Tratamiento de tipos de cuenta`.
- `Semántica de tipos de cuenta permitidos` → `Tratamiento de tipos de cuenta permitidos`.
- `EffectiveFrom` → `Vigencia inicial`.
- `EffectiveTo` → `Vigencia final`.
- `Capability (opcional)` → `Capacidad (opcional)`.
- `As Of UTC (opcional)` → `Fecha y hora de vigencia (UTC, opcional)`.
- `Tipo respuesta` → `Tipo de respuesta` (**4** fields).
- `Id transacción` → `ID de transacción`.
- `ID transacción` → `ID de transacción`.
- `Estado procesamiento` → `Estado de procesamiento`.
- `CorrelationId` → `ID de correlación`.
- `Tamaño página` → `Tamaño de página` (**2** fields).
- `Selector de cámara (ID)` → `ID de cámara`.
- `ID estado interno` → `ID del estado interno`.
- `ID estado servicio externo` → `ID del estado del servicio externo`.
- `Nombre estado interno` → `Nombre del estado interno`.
- `ID devolución origen` → `ID de la devolución de origen`.
- `FlowIds (separados por coma)` → `IDs de flujo (separados por comas)`.
- `Id de transacción real` → `ID de transacción real`.
- `Id de ciclo real` → `ID de ciclo real`.
- `Si se omite, el backend generará una referencia automática.` → `Si se omite, el sistema generará una referencia automática.`
- `ID cliente (opcional)` → `ID del cliente (opcional)`.
- `La elegibilidad y su motivo son calculados por backend para la cámara y contexto actuales.` → `La elegibilidad y su motivo son calculados por el sistema para la cámara y el contexto actuales.`
- `Backend determina y aplica la seguridad del destino.` → `El sistema determina y aplica la seguridad del destino.`
- `Transacción ACH` → `ID de transacción ACH`.
- `Tamaño` → `Tamaño de página`.

Los mensajes de validación cercanos que todavía contienen términos como `PolicyVersion`, `EffectiveFrom`, `EffectiveTo`, `flowIds` o `backend` permanecen deliberadamente fuera de NAV4.2E porque no son labels/placeholders/hints de field.

## Convenciones durables

**Labels.** Nombran el dato o dimensión funcional, no la propiedad/DTO/implementación. Se usa `ID de …` o `IDs de …` cuando el binding representa un identificador. No se traduce ni diluye terminología técnica necesaria.

**Placeholders.** Se conservan cuando ejemplifican formato o aportan orientación real. No se eliminan masivamente ni se usan como sustituto del label. Ejemplos técnicos legítimos como `SHA256:…`, GUIDs o formatos de fecha permanecen cuando describen el input esperado.

**Hints.** Añaden formato, restricción o contexto útil. Se evita exponer detalles de implementación sin valor para el usuario: en los tres hints focales `backend` se expresa como `el sistema` sin alterar la semántica funcional. Los hints no absorben mensajes de validación.

**Términos preservados.** ACH, NACHA-M, MFT, SFTP, SOAP, SOAP Action, WSDL, XSD, SHA-256/SHA256 cuando corresponde al formato, URL, IP, puerto, namespace, GUID, JSON, CSV y UTC. También se preserva vocabulario de dominio aprobado como cámara, ciclo, causal, devolución/ROR y conceptos contractuales.

**Datos dinámicos y selects.** Labels/textos derivados de catálogo, backend, enum, roles, metadata contractual o interpolación se clasifican `DYNAMIC_DATA` y no se reemplazan con hardcodes. NAV4.2E no modifica option labels ni `option value`; no abre una campaña de enums.

## Evidencia copy-only / integridad del formulario

`NAV4_2E_COPY_DELTA.json` documenta exactamente **36 líneas/hooks** modificados en **12 archivos HTML de producción**. No hay TS productivo modificado por NAV4.2E. El validador revierte sólo esas líneas en memoria y exige reconstruir exactamente el hash agregado NAV4.2D de `web/ach-interbank-ui/src/app`: `4d83eac4561e9a8b4a57ce91e20980bce2f8e1a5f76c6b8cc8b6a0c8f8e1022f`.

Una firma independiente sobre **853 registros de controls/options**, excluyendo únicamente atributos de copy, es idéntica before/after: `299fe031e20fa402d9a3aa941859c0d682a98771f872c593a30e2fa8c7adf015`. Se preservan `formControlName`, `[formControl]`, `[(ngModel)]`, `name`, `value`, option values, attributes/event bindings, `required`, `disabled`, `readonly`, `type`, `autocomplete`, min/max/length/pattern y demás configuración del control. DTOs, services, APIs y persistencia no cambian.

Los cuatro campos focales password/credential conservan exactamente type/autocomplete/binding/masking/disabled: reset de password y confirmación, password de usuario administrativo y credencial MFT write-only (`type=password`, `autocomplete=new-password`). NAV4.2E no expone ningún secreto.

## No regresión NAV4 / seguridad

- NAV4.2D: los 45 drifts de action-copy permanecen corregidos; handlers/event/service methods siguen intactos.
- NAV4.2C: los 27 drifts navegacionales permanecen corregidos; no se reabre navegación.
- NAV4.2B: headings menu-backed y metadata fallback permanecen con **0 mismatches**.
- NAV4.2A: `NavigationNamingService`/resolver mantienen el modelo backend-source-of-truth.
- NAV4.1A/B: `NavigationService`, `MenuRouteSurfaceService`, routing, parent classification y active-state quedan intactos.
- NAV3/NAV3.5A/B: handler, seed NAV2, seeders, policies/guards y Orders 119/120 quedan intactos; NAV3.5C continúa `BLOCKED / NO EJECUTAR`.

## Validaciones ejecutadas / diferidas

- input NAV4.2D SHA-256 `0da1a5f2bba0b00e5255d61c230708082f1a7cbe47edfa62a4a706867240d8d2` — **PASS**;
- `scripts/validate-nav4-2e-form-copy.py` — **PASS, 264 assertions**; además verifica cada línea de copy NAV4.2D contra `NAV4_2D_COPY_DELTA.json` y ejecuta los validadores composicionales NAV4.2C/B;
- `scripts/validate-nav4-2c-navigation-copy.py` — **PASS, 111 assertions**;
- `scripts/validate-nav4-2b-menu-backed-copy.py` — **PASS, 81 assertions**;
- parser TypeScript 5.8.3 sobre los **12 componentes TS asociados a los templates modificados** — **PASS, 0 diagnostics**;
- firma independiente de controls/options before/after — **PASS**, 853 registros idénticos en atributos funcionales;
- `scripts/validate-solution-projects.py` — **PASS, 7/7**;
- Angular build/test/runtime — `NOT_RUN_ENVIRONMENT`: `web/ach-interbank-ui/node_modules` ausente;
- .NET — `NOT_RUN_ENVIRONMENT`: `dotnet` no está disponible.

El validador histórico aislado de NAV4.2D no se usa como gate composicional después de introducir NAV4.2E porque su prueba fuerte espera que el árbol completo revierta directamente a NAV4.2C. NAV4.2E preserva su intención de regresión verificando exactamente todos los `new` strings de `NAV4_2D_COPY_DELTA.json` antes de validar NAV4.2C/B.

## Superficie deliberadamente diferida y siguiente candidato

NAV4.2E no modifica `mat-error`, mensajes de validación/server errors, headers/columnas, sort/paginator, mensajes/empty states, confirmaciones, tooltips generales ni ARIA no asociado a fields. Un scan estructural actual (superficie, **no drift certificado**) encuentra aproximadamente **255 `<th>`**, **250 `headerName:`**, **224 `<mat-error>`**, **20 `matTooltip`**, **218 atributos `aria-label` totales** (40 field-specific ya incluidos en este inventario) y **29 `ui-alerta`**.

Por volumen y delimitación, el siguiente candidato recomendado —sujeto a decisión humana y a inventario semántico previo— es **`NAV4.2F — tablas, columnas y filtros/tabular accessibility`**. No ejecutar automáticamente.

# NAV4.2F — tablas, columnas y accesibilidad tabular — 2026-10-15

## Estado y límite

NAV4.2F queda `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`. El incremento modifica exclusivamente copy visible/accesible que identifica datos en tablas/grillas: encabezados HTML/Material, `headerName`, display labels de columnas, nombres accesibles de tabla y paginación/tabular copy local. No cambia column keys/IDs, bindings, fields, sort/filter, renderers/value getters, datasource, selección, paginación, endpoints, query params, export mapping, row actions, formularios, mensajes ni empty states.

NAV2/NAV3 y NAV4.1A/B/NAV4.2A/B/C/D/E conservan exactamente `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`. NAV3.5C continúa `BLOCKED / NO EJECUTAR`.

## Inventario semántico exacto

Se inspeccionaron **630 hooks tabulares**, representando **573 identidades estáticas de columna** después de colapsar display labels duplicados del selector de columnas y el renderer genérico de `app-table`. Tipos: 44 `HTML_TH`, 206 `MATERIAL_HEADER`, 250 `GRID_HEADER_NAME`, 86 `COLUMN_DISPLAY_LABEL`, 0 `SORT_ACCESSIBLE_NAME`, 16 `TABLE_ACCESSIBLE_NAME`, 25 `PAGINATION_ACCESSIBLE_COPY` y 3 `OTHER_TABULAR_COPY`.

Clasificación de entrada: 243 `ALIGNED`, 112 `DRIFT`, 8 `TECHNICAL_TERM_LEGITIMATE`, 54 `DOMAIN_TERM_LEGITIMATE`, 210 `CONTEXTUAL_COLUMN`, 3 `DYNAMIC_DATA`, 0 `OUT_OF_SCOPE` dentro del inventario focal. Los **112 DRIFT** fueron corregidos y el drift residual identificado del alcance queda en **0**. El inventario completo, incluyendo archivo/línea, key, dato representado, sort/filter/export y old→new, está en `NAV4_2F_TABULAR_COPY_INVENTORY.md`.

## Convenciones resultantes

- El header nombra el dato, no la propiedad interna: `CorrelationId` → `ID de correlación`, `IdEstado` → `ID del estado`, `Código error` → `Código de error`.
- `ID de …` se usa sólo cuando el binding demuestra el identificador concreto; IDs técnicos relevantes no se ocultan.
- Se reutilizan las convenciones NAV4.2E para conceptos equivalentes: `Vigencia inicial/final`, `Estado de procesamiento`, `ID de transacción`, `ID de operación` y formulaciones de origen/destino.
- Términos técnicos legítimos permanecen precisos: ACH, NACHA-M, SOAP, DFI, Routing, Transit, User-Agent, IP, SHA-256 y UTC.
- Los valores de estados, chips/badges, contenido de celdas, row actions y datos dinámicos no fueron humanizados.
- Los builders CENIT `headerName: header` y el renderer `headerName: column.label` permanecen dinámicos; NAV4.2F no crea un catálogo hardcodeado de columnas.

## Export y contratos

El scan no encontró `exportDataAsCsv`, `exportDataAsExcel`, `getDataAsCsv` ni `getDataAsExcel` en la SPA. Los `TableColumn.label` de Reportes alimentan exclusivamente la tabla; la descarga PDF usa métodos dedicados de `ReportsApiService` y no recibe `config.columns`. Por ello **0 headers modificados** cambian un contrato externo/máquina de export. No se modificaron columnas exportadas, keys, orden, formato, nombres de archivo ni endpoints.

## Evidencia de copy-only / no logic delta

`NAV4_2F_COPY_DELTA.json` registra **108 líneas** modificadas en **26 archivos de producción** y **112 hooks tabulares** corregidos. El validador revierte únicamente esas líneas y exige que el árbol completo `web/ach-interbank-ui/src/app` reproduzca exactamente el hash agregado NAV4.2E `62230bdde11661f6d55559e6077972a65646578c9ae9c015002d7f563291e3cb`.

Además, para cada línea se normaliza exclusivamente `headerName`, `label`, texto de `<th>` o el accessible copy tabular permitido y se exige que el resto de tokens permanezca idéntico. Así quedan protegidos `matColumnDef`, `field`, `colId`/keys, sort/filter, renderers, value getters, row actions, bindings y configuración funcional.

## No regresión NAV4 / seguridad

- `NavigationService`, `MenuRouteSurfaceService`, `NavigationNamingService` y `navigation-naming.util.ts` permanecen byte-idénticos.
- NAV4.2B mantiene headings y metadata fallback con **0 mismatches** dentro de su alcance.
- Los strings finales documentados por `NAV4_2D_COPY_DELTA.json` y `NAV4_2E_COPY_DELTA.json` permanecen exactos; NAV4.2C no cambia porque no se tocó routing ni copy navegacional.
- `GetMenuForCurrentUserHandler`, `NavigationInformationArchitectureSeeder`, seeders NAV3, guards, Orders 119/120, policies, grants, fallbacks y role gates permanecen intactos.

## Validaciones

- input NAV4.2E SHA-256 `48e215acdd645c64c16ace36f662deaabf47bbe9eb0ca3d4c6bcf060d703e9fd` — **PASS**;
- `scripts/validate-nav4-2f-tabular-copy.py` — **PASS, 475 assertions**;
- `scripts/validate-nav4-2c-navigation-copy.py` — **PASS, 111 assertions**;
- `scripts/validate-nav4-2b-menu-backed-copy.py` — **PASS, 81 assertions**;
- parser TypeScript 5.8.3 sobre los **19 TS modificados** — **PASS, 0 diagnostics**;
- `scripts/validate-solution-projects.py` — **PASS, solución 7/7**;
- Angular build/test/runtime — `NOT_RUN_ENVIRONMENT` mientras `node_modules` continúe ausente;
- .NET — `NOT_RUN_ENVIRONMENT` mientras `dotnet` continúe ausente.

## Copy restante y siguiente decisión

NAV4.2F no cambia empty states, mensajes, validaciones (`mat-error`), tooltips/ARIA no tabular ni cuerpos de confirmación. El scan estructural residual observa **202 `mat-error`**, **24 `ui-estado-vacio` + 9 atributos de empty-state de grids**, **29 `ui-alerta`**, **360 llamadas de notificación/snackbar candidatas** y **49 `matTooltip`**; estos conteos son hooks potenciales, no drifts. Por volumen y separación semántica, el candidato recomendado es **`NAV4.2G — mensajes y estados vacíos`**, dejando **mensajes de validación** para NAV4.2H si el inventario confirma el volumen. No ejecutar automáticamente.

# NAV4.2G — mensajes y estados vacíos — 2026-10-15

## Estado y alcance

NAV4.2G queda `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`. Se ejecutó exclusivamente sobre el baseline NAV4.2F y sólo modifica copy visible de feedback/estado generado por la SPA. No cambia `mat-error`, validation messages/validators, confirmaciones, tooltips/ARIA generales, labels/placeholders/hints, botones/actions, headings, headers/columnas, rutas/menú, permisos/guards/security, seed, DTOs/endpoints/payloads, lógica funcional ni backend.

NAV2, NAV3, NAV4.1A/B y NAV4.2A-F permanecen `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`; NAV3.5C continúa `BLOCKED / NO EJECUTAR`.

## Inventario y origen

`NAV4_2G_MESSAGES_EMPTY_STATES_INVENTORY.md` registra **643 hooks** con archivo/línea, mecanismo, texto, origen, trigger, severity, contexto, clasificación, old/new y decisión. Distribución por mecanismo: 25 `EMPTY_STATE`, 23 `GRID_EMPTY_STATE`, 29 `UI_ALERT`, 11 `SNACKBAR`, 279 `NOTIFICATION`, 106 `INLINE_STATUS`, 45 `LOADING_MESSAGE`, 45 `ERROR_MESSAGE`, 79 `WARNING_MESSAGE` y 1 `SUCCESS_MESSAGE`.

Origen: 408 `SPA_AUTHORED`, 72 `SPA_COMPOSED`, 25 `HYBRID`, 36 `BACKEND_PASSTHROUGH` y 102 `DYNAMIC_DATA`. Los hooks híbridos conservan sus fragmentos externos/dinámicos; no se alteró ningún passthrough backend/external. Clasificación: 292 `ALIGNED`, 48 `DRIFT`, 50 `TECHNICAL_TERM_LEGITIMATE`, 48 `DOMAIN_TERM_LEGITIMATE`, 67 `CONTEXTUAL_MESSAGE`, 102 `DYNAMIC_DATA` y 36 `BACKEND_PASSTHROUGH`.

Los **48 DRIFT** se resolvieron con **50 líneas de copy** en **27 archivos**. El drift residual identificado dentro del alcance queda en 0.

## Decisiones

- `DEC-NAV-4.2G-001`: SPA-authored puede humanizarse; backend/external passthrough no se altera. En HYBRID sólo puede cambiar el wrapper SPA, preservando el fragmento dinámico.
- `DEC-NAV-4.2G-002`: los empty states representan estados distintos y no se unifican en “No hay registros”. Búsqueda/filtro, no cargado, ausencia de configuración, historial vacío, ausencia legítima, error y no aplicable conservan semántica propia.
- `DEC-NAV-4.2G-003`: NAV4.2G excluye `mat-error`/validaciones; NAV4.2H queda reservado para mensajes de validación.

## Correcciones semánticas focales

- Integraciones: copy de estado deja de exponer “backend”/“conexión técnica avanzada”; `preview-change` no afirma SOAP ejecutado ni conexión probada.
- ACH Colombia file exchange: los success messages indican que la **ejecución** de envío/recepción terminó y remiten al estado; no afirman recepción o aceptación por ACH Colombia. La advertencia existente “Publicado en la frontera CFA. La recepción por ACH Colombia no está confirmada.” permanece.
- MFT: `Conexión exitosa.` permanece sólo en el `test-connection` real; guardar configuración conserva `Configuración guardada.` y no se presenta como test.
- Bulk ingestion: se preserva la diferencia entre lote registrado, reintento solicitado, cancelación registrada y procesamiento finalizado.
- NACHA-M: se mantiene `result.message` cuando el backend entrega mensaje útil; el fallback SPA sólo opera en ausencia de ese mensaje. `.xls/.xlsm` no se toca.
- Returns/ROR: generación local no se presenta como transmisión externa.
- Cifrado/descifrado: las operaciones exitosas muestran `Archivo cifrado`/`Archivo descifrado`; no se afirma una firma verificada fuera del camino que realmente la valida.
- Empty states: se reemplazan aliases internos (`payload`, `maestro canónico`, `persistidos`) y genéricos `No hay registros` sólo donde el contexto permite una formulación precisa; no se agregan CTAs ni se cambian condiciones/handlers.

## Delta reversible / no logic delta

`NAV4_2G_COPY_DELTA.json` contiene sólo cambios productivos reales. Revertir sus 50 líneas reconstruye exactamente el árbol `web/ach-interbank-ui/src/app` NAV4.2F, hash agregado `e260fe85e533bf326538f201343bf9b11a74236d62aa007628450121a86daa16`.

El validador exige que cada línea cambie sólo el fragmento de copy documentado y compara semánticamente contra el baseline reconstruido: `mat-error`, validators, confirmaciones, fields/forms NAV4.2E, actions NAV4.2D, tabular copy NAV4.2F, handlers/clicks, backend body/ProblemDetails, códigos Rxx/Dxx, navegación, seguridad y seed. `GetMenuForCurrentUserHandler`, `NavigationInformationArchitectureSeeder`, `FineGrainedGrantBackfillSeeder`, `LegacyFineGrainedRoleMigrationSeeder`, guards y NavigationNaming/NavigationService permanecen congelados.

## Validación y siguiente paso

- input NAV4.2F SHA-256 `6563abb7802f7b7d0e60ed48b3f62c4a432ded7742539ffd711b38a883afb79f` — **PASS**;
- `scripts/validate-nav4-2g-messages-empty-states.py` — **PASS, 1207 assertions**;
- parser TypeScript 5.8.3 sobre **12 TS productivos modificados** — **PASS, 0 diagnostics**;
- `scripts/validate-solution-projects.py` — **PASS, solución 7/7**;
- Angular build/test/runtime — `NOT_RUN_ENVIRONMENT`: `web/ach-interbank-ui/node_modules` ausente; no se instalaron dependencias;
- .NET — `NOT_RUN_ENVIRONMENT`: `dotnet` ausente.

Inventario restante fuera de esta fase: mensajes de validación (`NAV4.2H`), confirmaciones y tooltips/ARIA generales. **NEXT = NAV4.2H — mensajes de validación**, únicamente por decisión humana. **NAV4.2H NO fue ejecutado.**

# NAV4.2H — mensajes de validación — 2026-10-15

## Estado y alcance

NAV4.2H queda `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`. Se ejecutó exclusivamente sobre NAV4.2G y es **COPY ONLY**: humaniza mensajes de validación SPA (`mat-error`, `app-error-message`, required/min/max/length/pattern/email, validaciones cruzadas/custom y copy de validación de archivos) sin modificar validators, estructuras FormControl/FormGroup, regex, límites, condiciones, precedencia, touched/dirty/submitted, bindings, submit logic, APIs, DTOs, routing, permisos, security o seed.

NAV2, NAV3, NAV4.1A/B y NAV4.2A-G permanecen `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`; NAV3.5C continúa `BLOCKED / NO EJECUTAR`.

## Inventario y decisiones

`NAV4_2H_VALIDATION_MESSAGES_INVENTORY.md` registra **321 hooks exactos**. Incluye **224 `mat-error`** (202 HTML + 22 templates inline TS), 14 `app-error-message`, fábricas/resolvers SPA y validaciones inline/resultados. Distribución por origen: 288 `SPA_VALIDATION`, 16 `SPA_VALIDATION_COMPOSED`, 15 `BACKEND_VALIDATION_PASSTHROUGH`, 2 `DYNAMIC_DATA`; no se observó un hook directo de `EXTERNAL_VALIDATION_PASSTHROUGH` dentro del inventario focal, pero la invariancia externa se valida estáticamente.

Clasificación: 188 `ALIGNED`, 92 `DRIFT`, 11 `TECHNICAL_TERM_LEGITIMATE`, 13 `DOMAIN_TERM_LEGITIMATE`, 15 `BACKEND_VALIDATION_PASSTHROUGH`, 2 `OUT_OF_SCOPE`. Los **92 DRIFT** quedaron corregidos en **92 líneas / 16 archivos productivos**.

- `DEC-NAV-4.2H-001`: sólo mensajes SPA pueden humanizarse; backend/external validation passthrough es invariante.
- `DEC-NAV-4.2H-002`: copy no cambia validator, error key, condición ni precedencia.
- `DEC-NAV-4.2H-003`: el mensaje sólo describe restricciones demostrables por el validator real.

## Correcciones focales

- Los resolvers compartidos pasan de copy genérico/formal (`Campo obligatorio`, `Ingrese`, `Debe tener`, `Formato inválido`) a instrucciones naturales, conservando los mismos error keys y valores dinámicos de `requiredLength`, `min` y `max`.
- Validaciones con contexto demostrable se vuelven precisas: código de cámara 2–20 con regex de mayúsculas/números/guion/guion bajo; descripción/país con carácter visible donde el `pattern /\S/` lo exige; `amountOverflow` deja de hablar de “longitud” y expresa máximo permitido sin inventar un valor distinto al validator.
- Aliases internos de validación visibles se humanizan donde el contexto lo demuestra: `PolicyVersion` → “versión de política”, `EffectiveTo/EffectiveFrom` → “vigencia final/inicial”, `cutoff` → “corte de entrada”, `addenda` visible → “adenda”. Las propiedades/paths internos (`addendas`, error keys) permanecen intactos.
- SOAP, XSD, HH:mm, Quartz, ACH, NACHA-M, CFA, R10 y otros términos técnicos/de dominio necesarios se conservan cuando la restricción real los requiere.
- `ProblemDetails`, `ValidationProblemDetails`, `form.getError('backend')`, `result.errors`, mensajes técnicos de mapping/integraciones, resultados NACHA y preview backend se preservan sin humanización.

## Delta reversible / NO LOGIC DELTA

`NAV4_2H_COPY_DELTA.json` contiene únicamente los seis campos solicitados por cambio productivo (`file`, `old`, `new`, `field_context`, `validator_error_key`, `origin`). Revertir sus 92 cambios reconstruye exactamente el árbol NAV4.2G con hash agregado `ba52a7a83c980ba302754ea3b776989cbf075a654b6e451230decd85751d4737`.

El validador H congela validators, regex, límites min/max/length, keys, condiciones, precedencia, bindings, touched/dirty/submitted, códigos Rxx/Dxx, passthrough backend/external, todo el feedback NAV4.2G, copy D/E/F y navegación/security/seed. Por tanto el incremento tiene **NO LOGIC DELTA**.

## Validaciones

- SHA-256 input NAV4.2G `7f6f4e74222c964e1ab7f6e65d060a6f5bcac6495c73272f254cf8e20907119a` — PASS;
- `scripts/validate-nav4-2h-validation-copy.py` — PASS, 1034 assertions;
- baseline aislado NAV4.2G — PASS, 1207 assertions;
- baseline aislado NAV4.2F — PASS, 475 assertions;
- baseline aislado NAV4.2C — PASS, 111 assertions;
- baseline aislado NAV4.2B — PASS, 81 assertions;
- parser TypeScript 5.8.3 — PASS, 5 TS modificados, 0 syntax diagnostics;
- scanner templates — PASS, 224 `mat-error`, 14 `app-error-message`, bindings/condition-token counts invariantes;
- `scripts/validate-solution-projects.py` — PASS, 7/7;
- Angular build/tests/runtime — `NOT_RUN_ENVIRONMENT`: `node_modules` ausente; no se instalaron dependencias;
- .NET — `NOT_RUN_ENVIRONMENT`: `dotnet` ausente.

## Restante y siguiente decisión

NAV4.2H no toca confirmaciones ni tooltips/ARIA generales. El siguiente incremento recomendado es un bloque **separado de confirmaciones**; tooltips/ARIA generales deben permanecer en otro inventario. No ejecutar automáticamente.

# NAV4.2I — confirmaciones — 2026-10-15

## Estado y alcance

NAV4.2I queda `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`. Se ejecuta exclusivamente sobre NAV4.2H y es **COPY ONLY** para títulos, cuerpos, advertencias y texto explicativo de confirmaciones **previas** a una acción. No se modifican botones/action labels, handlers/callbacks, resultado confirm/cancel, condiciones, configuración funcional de diálogos, APIs/payloads, navegación, permisos/security/seed, validators, feedback NAV4.2G, fields NAV4.2E, tablas NAV4.2F ni validaciones NAV4.2H.

## Inventario y decisiones

`NAV4_2I_CONFIRMATION_COPY_INVENTORY.md` registra **44 hooks exactos**. Origen: 21 `SPA_CONFIRMATION`, 22 `SPA_CONFIRMATION_COMPOSED`, 1 `EXTERNAL_PASSTHROUGH` y 0 passthrough backend directo. Clasificación: 32 `ALIGNED`, 11 `DRIFT`, 1 `OUT_OF_SCOPE`. Los 11 drift se corrigen con **13 cambios de copy en 8 archivos productivos**.

- `DEC-NAV-4.2I-001`: la confirmación expresa sólo la acción que se solicita y consecuencias demostrables por el flujo real.
- `DEC-NAV-4.2I-002`: sólo título/cuerpo pueden cambiar; botones, handlers y resultado confirm/cancel son invariantes.
- `DEC-NAV-4.2I-003`: backend/external passthrough no se reescribe; datos dinámicos se preservan.

## Correcciones focales

- ACH Colombia file exchange: envío, recepción y retry se formulan como **inicio/intento**, no como éxito; archivar aclara que conserva contenido operativo y no equivale a retiro/eliminación.
- Catálogos: delete de código/término identifica explícitamente el catálogo sin alterar el dato interpolado.
- Transaction policies: descartar cambios explicita la pérdida de datos no guardados.
- Mapping CFA: wrapper SPA deja de exponer `backend`; datos dinámicos/decisión del servicio permanecen intactos.
- SOAP: `Confirmar comunicación externa` pasa a `Confirmar configuración de envío real`; confirmar sólo persiste configuración y no afirma que se ejecute una llamada SOAP.
- Scheduler: la confirmación habla de **solicitar una ejecución extraordinaria**, coherente con aceptación/seguimiento posterior, no de ejecución completada.
- ROR: ya no presenta como existente el archivo antes de generarlo; se habla de solicitar generación y revalidación previa, sin afirmar transmisión externa.

## Delta reversible / NO LOGIC DELTA

`NAV4_2I_COPY_DELTA.json` contiene sólo `file`, `old`, `new`, `action_context` y `origin`. Revertir sus 13 cambios reconstruye exactamente NAV4.2H `src/app`, hash agregado `6e291cbba272a76baaec6979f3c163b62e51cb8c7e24af41ff8fbbe8544d1f83`. El validador congela botones/action labels, condiciones, resultados, handlers/callbacks, APIs/service calls, validators, passthrough backend/external, NAV4.2D/E/F/G/H, navegación/security/seed.

## Restante y siguiente decisión

Fuera de NAV4.2I permanecen tooltips/ARIA generales. El siguiente incremento recomendado, sólo si se aprueba explícitamente, es un inventario separado de tooltips/ARIA; NAV4.2I no ejecuta ningún incremento adicional.


# NAV4.2J — tooltips y copy accesible ARIA — 2026-10-15

## Estado y alcance

NAV4.2J queda `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`. Se ejecutó exclusivamente sobre NAV4.2I y es **ACCESSIBILITY/COPY ONLY**: inventaría y corrige `matTooltip`, `title` usado como ayuda, `aria-label` SPA estático/compuesto, copy `sr-only`/equivalente, nombres accesibles de controles icon-only y ayudas breves asociadas a controles. No modifica comportamiento ni estructura para “mejorar accesibilidad”.

Roles, `tabindex`, focus management, keyboard handlers, estados ARIA (`expanded/selected/checked/disabled/live/...`), relaciones `aria-labelledby`/`aria-describedby`, IDs, event handlers, botones/actions visibles, labels/placeholders/hints, validaciones, alerts/notifications, confirmaciones, tablas, navegación, APIs, permisos/security/seed quedan fuera de alcance e invariantes.

## Inventario y clasificación

`NAV4_2J_TOOLTIP_ARIA_COPY_INVENTORY.md` registra **392 hooks exactos**:

- `ARIA_LABEL`: 272
- `MAT_TOOLTIP`: 45
- `TITLE`: 62
- `SR_ONLY`: 8
- `ARIA_LABEL_RUNTIME`: 3
- `ARIA_LABEL_CONFIG`: 2
- `aria-description`: 0

Se inspeccionaron **42** controles icon-only / `mat-icon-button`; faltantes de nombre accesible útil: **0**. De 62 `title`, 4 son ayuda/nombre de controles icon-only y 58 son títulos visibles de página/diálogo/componente, por lo que estos últimos se inventariaron como `OUT_OF_SCOPE` y permanecen sin cambios.

Clasificación: 111 `ALIGNED`, 22 `DRIFT`, 36 `TECHNICAL_TERM_LEGITIMATE`, 62 `DOMAIN_TERM_LEGITIMATE`, 45 `CONTEXTUAL_COPY`, 9 `DYNAMIC_DATA`, 6 `BACKEND_PASSTHROUGH`, 0 `EXTERNAL_PASSTHROUGH`, 101 `OUT_OF_SCOPE`. Los **22 DRIFT** quedaron corregidos mediante **22 cambios productivos en 11 archivos**.

## Decisiones y correcciones focales

- **DEC-NAV-4.2J-001:** tooltip y nombre accesible describen la acción real y no inventan comportamiento.
- **DEC-NAV-4.2J-002:** NAV4.2J modifica sólo copy accesible; roles, estados ARIA, focus y comportamiento son invariantes.
- **DEC-NAV-4.2J-003:** problemas de accesibilidad que requieran cambios estructurales/lógicos se registran `OUT_OF_SCOPE` para una fase posterior.

Correcciones representativas: `Centro administrativo de cámaras` → `Cámaras compensadoras`; menús de acción identifican la versión/restricción/fecha/configuración concreta; `Ver detalle del archivo …` → `Consultar trazabilidad del archivo …` cuando ésa es la acción visible; `Copiar identificador` → `Copiar identificador de correlación`; `Resumen del diff` → `Resumen de cambios`; `Selector de columnas para la grilla` → `Selector de columnas visibles`; `Badges conciliación` → `Estados de conciliación`; y los nombres accesibles de la superficie de autenticación se alinean con el nombre visible `Registro de accesos`.

Los datos interpolados (`row.fileName`, IDs, nombres, labels), términos técnicos/de dominio necesarios y passthrough backend permanecen intactos. No se añade tooltip decorativo ni `aria-label` estructural porque la auditoría no encontró icon-only sin nombre útil.

## Delta reversible / NO LOGIC DELTA

`NAV4_2J_COPY_DELTA.json` contiene sólo `file`, `old`, `new`, `mechanism` y `control_context`. Revertir exclusivamente sus 22 entradas reconstruye exactamente NAV4.2I `src/app`, hash agregado `5885f1a7c83c3c979d1a5909555ab372aa29d7b01dba1472547e5d933273b77f`.

`scripts/validate-nav4-2j-tooltip-aria-copy.py` congela handlers/callbacks, conditions, visibility, disabled/readOnly, roles, `tabindex`, estados/relaciones ARIA, IDs, configuración funcional de tooltip, navegación, API/service calls, validators/bindings, botones visibles, passthrough y códigos de dominio. También congela explícitamente las superficies de navegación accesible fuera de alcance. Resultado: **NO LOGIC DELTA**.

## Validación y cierre de NAV4.2

- SHA-256 input NAV4.2I `e9f86390a0d400d6219bc9060ad4ac817a50c9881d48949251b645848dd2ea90` — PASS;
- `scripts/validate-nav4-2j-tooltip-aria-copy.py` — **PASS, 445 assertions**;
- replay aislado NAV4.2I — **PASS, 405 assertions**;
- replay aislado NAV4.2H — **PASS, 1034 assertions**;
- replay aislado NAV4.2G — **PASS, 1207 assertions**;
- replay aislado NAV4.2F — **PASS, 475 assertions**;
- replay NAV4.2C — **PASS, 111 assertions**;
- replay NAV4.2B — **PASS, 81 assertions**;
- parser TypeScript 5.8.3 — **PASS, 374 TS productivos, 0 syntax diagnostics**;
- scanner templates/accessibility — **PASS, 392 hooks; 42 icon-only / 0 missing-name**;
- `scripts/validate-solution-projects.py` — **PASS, 7/7**;
- Angular build/tests/component runtime — `NOT_RUN_ENVIRONMENT`: `web/ach-interbank-ui/node_modules` ausente; no se instalaron dependencias;
- .NET — `NOT_RUN_ENVIRONMENT`: `dotnet` ausente.

Inventario de copy restante para NAV4.2: ninguno dentro de los buckets ejecutados D–J. Los problemas de accesibilidad que exijan cambios estructurales/lógicos permanecen `OUT_OF_SCOPE` para una fase posterior. **NEXT recomendado = NAV5**, sujeto a decisión humana explícita. **NAV5 NO fue ejecutado.**
