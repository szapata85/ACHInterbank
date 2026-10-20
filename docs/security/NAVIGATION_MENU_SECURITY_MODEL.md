# Modelo de seguridad del menú — NAV-MENU-IA-001

## Regla

La autorización real está en permisos/rutas/API; ocultar un menú no reemplaza la seguridad de la ruta ni del endpoint.

`GetMenuForCurrentUserHandler.ShouldInclude()` aplica actualmente:

```text
(hasAnyRequiredRole OR noRoleRequired)
AND
(hasAnyRequiredPermission OR noPermissionRequired)
```

Por esa semántica, varios `MenuItemPermission` son alternativas OR. Para que `TestTools.Access` sea obligatorio en el dominio `Herramientas de prueba`, desde NAV3.2B ese permiso queda como la única asociación de permiso de menú de sus tres nodos canónicos y se eliminan sus asociaciones directas `MenuItemRole`.

## Roles y permiso canónico

Los únicos roles sembrados por `RoleConfiguration` continúan siendo:

- `Admin` — Id estable `RoleConfiguration.AdminRoleId`, descripción `Acceso completo al sistema`;
- `ACH.Operator` — Id estable `RoleConfiguration.OperatorRoleId`.

No existe un rol separado de `Superadministrador` ni un rol Desarrollo/Soporte técnico. La aplicación no usa checks por nombre de rol para entrar a `Herramientas de prueba`.

El permiso dedicado del dominio es:

```text
Permission: TestTools.Access
Display: Acceso a herramientas de prueba
```

La claim se declara como `FineGrainedPermissions.TestTools.Access` y se persiste mediante `TestToolsPermissionSeeder`.

El grant vigente es exclusivamente:

```text
Admin -> TestTools.Access
```

`TestToolsAdminRolePermissionSeeder` persiste esa relación idempotentemente en `RolePermissions`. `ACH.Operator` no recibe `TestTools.Access`; NAV3.3B no crea roles, permisos, grants nuevos ni permisos directos a usuarios.

## Estado efectivo de Herramientas de prueba después de NAV3.3B

### Menú

`TestToolsMenuAuthorizationSeeder` ejecuta después del reconciliador canónico NAV2 (`Order 101`, frente a `NavigationInformationArchitectureSeeder.Order 100`) y reconcilia únicamente:

- `/uat` — `Herramientas de prueba`;
- `/uat/nacha-inbound-simulator` — `Simulador de archivos recibidos`;
- `/ach/nacha/soap-uat-console` — `Pruebas de servicios`.

Para esos tres MenuItem:

- `TestTools.Access` es la única asociación `MenuItemPermission`;
- no existen asociaciones `MenuItemRole` directas;
- NAV3.3B activa `Pruebas de servicios` (`IsActive = true`) **después** de reconciliar su permiso de menú.

`NavigationInformationArchitectureSeeder` conserva deliberadamente el valor canónico inicial `IsActive = false` para `Pruebas de servicios`: el reconciliador posterior de seguridad es quien la publica una vez aplicada la frontera `TestTools.Access`. Esto evita que una ejecución incompleta del seed canónico publique la herramienta antes de la reconciliación de seguridad.

Desde NAV3.3A `GetMenuForCurrentUserHandler` no aplica gating por ambiente a `/uat` ni a `/uat/**`. Por tanto, la visibilidad final depende de `IsActive`, jerarquía y `TestTools.Access`, no de Production/UAT-like ni de `CanReadAch`/`CanManageAch` o nombres de rol.

La jerarquía y órdenes permanecen:

```text
Herramientas de prueba — 90
├── 10 Simulador de archivos recibidos
└── 20 Pruebas de servicios
```

### SPA

`/uat` y `/uat/nacha-inbound-simulator` continúan protegidos únicamente por `permissionGuard` con `TestTools.Access` como frontera del dominio.

NAV3.3B desacopla la consola de pruebas de las guardas heredadas de `/ach` sin cambiar su URL. En `app-routing.module.ts` se declara **antes** del padre compartido `/ach` una ruta exacta independiente:

```text
/ach/nacha/soap-uat-console
  -> permissionGuard
  -> TestTools.Access
```

La ruta hija equivalente se elimina de `NACHA_OPERATIONAL_ROUTES`. De esta forma la consola no atraviesa el bloque compartido:

```text
/ach
  -> roleGuard: Admin | ACH.Operator
  -> permissionGuard: CanReadAch
```

El padre `/ach` y todas las demás rutas `/ach/**` conservan sus guardas previas. La consola no exige indirectamente `Admin`, `ACH.Operator`, `CanReadAch` ni `CanManageAch`; su autorización SPA efectiva es `TestTools.Access`.

### API del Simulador de archivos recibidos

`NachaInboundSimulatorController` continúa exigiendo:

```text
[Authorize(Policy = FineGrainedPermissions.TestTools.Access)]
```

Las policies específicas `NachaSimulator.*` permanecen como requisitos adicionales donde corresponden. El modelo efectivo continúa siendo:

```text
TestTools.Access AND permiso específico adicional cuando aplique
```

NAV3.3B no modifica funcionalmente el simulador.

### API de Pruebas de servicios

`NachaSoapUatConsoleController` conserva como frontera obligatoria de clase:

```text
[Authorize(Policy = FineGrainedPermissions.TestTools.Access)]
```

NAV3.3B elimina únicamente los requisitos de disponibilidad por ambiente del controller:

- se elimina la dependencia de `IHostEnvironment`;
- se elimina la dependencia de `NachaInboundSimulatorOptions` para decidir disponibilidad;
- se elimina `!Production`;
- se elimina `IsUatLike()`/Development/Testing/UAT como requisito de entrada;
- se elimina la respuesta `UAT_FEATURE_UNAVAILABLE` originada exclusivamente por el ambiente.

Los cuatro endpoints del controller son `GET` y delegan únicamente al read model `INachaSoapUatConsoleReadModelService`. NAV3.3B retira además la policy de acción `P1Policies.NachaSimulatorRead`, porque no representa una capacidad diferenciada de esta consola y, por compatibilidad histórica, podía exigir permisos ACH genéricos no pertenecientes al dominio. La frontera API efectiva de la consola queda exclusivamente en `TestTools.Access`; no se agrega fallback por `CanReadAch`, `CanManageAch`, `Admin` o `ACH.Operator`.

## Dependencias reales de Pruebas de servicios

La pantalla `/ach/nacha/soap-uat-console` usa `NachaSoapUatConsoleService` con base path `api/ach/nacha/soap-uat-console`.

El flujo que carga actualmente la pantalla invoca sólo:

| Endpoint | Uso actual | Clasificación |
|---|---|---|
| `GET api/ach/nacha/soap-uat-console/dashboard` | resumen de preparación/estado | sólo lectura / diagnóstico |
| `GET api/ach/nacha/soap-uat-console/candidates` | candidatos y bloqueos | sólo lectura / preparación / diagnóstico |
| `GET api/ach/nacha/soap-uat-console/audit` | evidencia y eventos sanitizados | sólo lectura / diagnóstico |

El servicio SPA también expone `GET api/ach/nacha/soap-uat-console/candidates/{correlationId}`. El componente actual no lo invoca —selecciona el detalle desde la colección ya cargada—, pero el endpoint permanece igualmente read-only y protegido por el mismo `TestTools.Access` de clase.

No hay llamadas desde la pantalla o su servicio a `api/uat/contrapartidas`, a otros controllers UAT/SOAP ni a endpoints mutadores.

`NachaSoapUatConsoleReadModelService` depende únicamente de `INachaOperationalReadStore`. Su salida marca explícitamente:

```text
ProductiveStatus = NO-GO
ProductiveExecution = false
WouldInvokeRealSoap = false
```

El servicio proyecta archivos, decisiones, readiness y auditoría existentes; bloquea candidatos `ProcTransacciones`/`ProcContrapartidas` para ejecución productiva y sanitiza detalles. `NachaOperationalReadStore` consulta `AchDbContext` con operaciones `AsNoTracking()` y no ejecuta `SaveChanges`, dispatch jobs ni clientes SOAP.

Conclusión funcional: `Pruebas de servicios` es una superficie de **lectura, preparación y diagnóstico**. NAV3.3B no agrega envío SOAP, transmisión externa ni mutaciones operativas.

## UatContrapartidasController y operaciones SOAP live

`UatContrapartidasController` fue inspeccionado expresamente en NAV3.3B y **no es dependencia** de `Pruebas de servicios`.

Ese controller es una superficie distinta y potencialmente live:

- publica `POST api/uat/contrapartidas/dispatch-cycle`;
- exige actualmente `CanManageAch`;
- usa `IContrapartidaDispatchJobService.ProcessTransactionAsync`;
- puede ejecutar el flujo `Proc_Contrapartidas` mediante el cliente SOAP configurado;
- rechaza `Production`;
- limita ambiente a `Development`/`Testing`/`UAT`;
- exige `ACH_SOAP_LIVE_TESTS=true`.

NAV3.3B lo deja intacto. En particular, **no se elimina `ACH_SOAP_LIVE_TESTS`**, no se retiran sus gates de ambiente y no se le agrega `TestTools.Access`, porque la consola no lo necesita y hacerlo ampliaría una superficie live fuera del objetivo autorizado.

## Seguridad funcional vigente

### Simulador de archivos recibidos

Se mantienen las decisiones NAV3.3A:

- no hay gating de ambiente en menú, SPA, controller o `PreviewAsync`;
- `NachaInboundSimulator:Enabled` continúa como switch funcional;
- `AllowAutoImport=true` o `AllowExternalTransmission=true` continúa fallando de forma cerrada;
- persisten límites, validaciones de formato, cámara/ciclo/referencias y controles de escenarios;
- la salida conserva `GeneratedOnly=true`, `AutoImported=false`, `UploadRequired=true`, `ExternalTransmission=false`;
- no existe transmisión externa automática ni autoimportación desde el simulador.

### Pruebas de servicios

Después de NAV3.3B:

- no queda gating de ambiente conocido en su menú, ruta SPA ni `NachaSoapUatConsoleController`;
- `TestTools.Access` sigue siendo obligatorio en las tres capas;
- su controller sólo publica lecturas `GET`;
- su read model no ejecuta SOAP ni modifica estado;
- `UatContrapartidasController` y su flag `ACH_SOAP_LIVE_TESTS` continúan aislando la superficie que sí puede ejecutar una operación externa/live.

No se identifica un bypass conocido de `TestTools.Access` para las dos herramientas publicadas.

## Reglas vigentes

- `Admin` recibe `TestTools.Access` mediante `RolePermissions`.
- `ACH.Operator` NO recibe `TestTools.Access`.
- `CanReadAch` y `CanManageAch` no son fallbacks para entrar a `Herramientas de prueba`.
- `Herramientas de prueba`, `Simulador de archivos recibidos` y `Pruebas de servicios` requieren `TestTools.Access` en menú.
- La SPA del simulador y la ruta SPA independiente de la consola exigen `TestTools.Access`.
- Los controllers propios de ambas herramientas exigen `TestTools.Access` a nivel de clase.
- Las capacidades específicas del simulador pueden exigir permisos adicionales; nunca sustituyen `TestTools.Access`.
- `Pruebas de servicios` está activa en el estado final ordenado de seed, luego de la reconciliación de seguridad.
- Los gates y feature flags de `UatContrapartidasController` permanecen porque protegen una operación live separada.
- No codificar checks por nombres de rol como sustituto del permiso.

## Deuda posterior a NAV3.3B

Si las pruebas runtime pendientes confirman el comportamiento, el subdominio `Herramientas de prueba` puede considerarse cerrado **a nivel de diseño/seguridad de código**: ambas herramientas publicadas quedan protegidas por `TestTools.Access`, sin gating por nombre de ambiente y sin habilitar efectos externos nuevos.

La deuda siguiente no debe seguir ampliando estas herramientas. Debe abordar, en incrementos pequeños y separados, los gaps de autorización acumulados de otros dominios ya inventariados —por ejemplo drifts SPA/API o permisos técnicos no persistidos— sin cambiar la arquitectura NAV2.

La deuda de verificación de entorno sigue separada: .NET runtime/idempotencia no puede darse por cerrada hasta disponer del SDK, por lo que NAV2 permanece `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`.

## Capacidad técnica adicional

`/payment-rail-capability-registry` permanece clasificada como `Capacidades de la plataforma` bajo `Administración`; su normalización de autorización no forma parte de NAV3.3B.

## NAV3.4A — Revisión manual de respuestas ACH

### Clasificación funcional

`/ach-responses/manual-review` es una superficie **mixta**: conserva consultas de lectura, pero también ofrece decisiones manuales que persisten estado. La inspección del componente SPA muestra exactamente estas operaciones:

| Operación | Endpoint | Clasificación | Policy API |
|---|---|---|---|
| Buscar casos | `GET api/ach/responses` | sólo lectura / revisión | `P1.NachaRead` |
| Listar huérfanas | `GET api/ach/responses/orphans` | sólo lectura / revisión | `P1.NachaRead` |
| Ver detalle | navegación a `/ach-responses/:id` | consulta contextual | lectura |
| Seleccionar huérfana | estado local SPA | preparación local | no aplica |
| Iniciar revisión | `POST api/ach/responses/orphans/{id}/review/start` | actualización de estado | `P1.NachaGenerate` |
| Resolver/asociar o rechazar | `POST api/ach/responses/orphans/{id}/resolve` | decisión manual + actualización de estado | `P1.NachaGenerate` |

La pantalla no invoca `requestReprocess`, `process` ni `sendNotification`. No ejecuta SOAP, no transmite archivos y no despacha operaciones externas.

`BeginReviewAsync` cambia la huérfana `Pending -> InReview`, transiciona la respuesta asociada a `EnRevision`, registra auditoría y persiste. `ResolveOrphanAsync` exige revisión iniciada, valida una referencia funcional inequívoca en la misma cámara cuando se asocia, marca la huérfana `Resolved` o `Rejected`, transiciona la respuesta a `Resuelta` o `Rechazada`, registra auditoría y persiste. `AchResponseOperationsService` depende sólo de `AchDbContext`; esas dos operaciones no usan cliente SOAP, dispatch ni transporte externo.

### Autorización antes de NAV3.4A

- **MenuItem:** `/ach-responses/manual-review` tenía roles existentes `Admin OR ACH.Operator` y permiso `CanReadAch`.
- **SPA:** el padre `/ach-responses` exigía `Admin OR ACH.Operator` **AND** `CanReadAch`; la ruta hija `manual-review` volvía a exigir `CanReadAch`. La SPA mostraba controles mutables sin un gate adicional de gestión.
- **API de lectura:** `P1.NachaRead`, cuya implementación acepta `Nacha.Read OR CanReadAch`.
- **API mutable:** `P1.NachaGenerate`, cuya implementación acepta `Nacha.Generate OR CanManageAch` y **no** acepta `CanReadAch`.

Por tanto, el backend ya impedía que `CanReadAch` ejecutara las mutaciones; el gap era un drift de autorización/UX en la SPA, no una ampliación de privilegios del API.

### Autorización final NAV3.4A

NAV3.4A conserva la lectura con `CanReadAch` y separa las acciones mutables, siguiendo la capacidad ya definida en backend:

```text
Entrada/consulta de Revisión manual
  MenuItem: Admin OR ACH.Operator  AND CanReadAch
  SPA route: Admin OR ACH.Operator AND CanReadAch
  API GET: P1.NachaRead = Nacha.Read OR CanReadAch

Acciones Iniciar revisión / Resolver-Rechazar
  SPA action gate: Nacha.Generate OR CanManageAch
  API POST: P1.NachaGenerate = Nacha.Generate OR CanManageAch
```

La SPA oculta los controles mutables cuando ninguna de esas dos claims de gestión está presente y además bloquea las llamadas desde los métodos del componente. `CanReadAch` solo sigue siendo suficiente para consultar la pantalla, pero ya no para iniciar o resolver una revisión. No se agregó ningún check por nombre de rol.

No se cambió `MenuItemPermission`, el guard de entrada, los controllers, las policies, `RolePermissions`, definiciones de permisos ni grants. Tampoco se amplió acceso respecto del backend existente.

### Regresión y límites de alcance

- Las demás rutas y superficies `ach-responses` no reciben cambios de producción.
- `/ach-responses/status-mappings` (`Homologación de estados`) permanece bajo `Administración de parámetros`.
- `Herramientas de prueba`, `TestTools.Access`, el simulador y `Pruebas de servicios` permanecen intactos.
- La arquitectura NAV2 permanece intacta y en estado `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`.
- No hay efecto externo/productivo nuevo desde Revisión manual; sí existen mutaciones internas de estado/auditoría, que son precisamente las acciones protegidas por `P1.NachaGenerate`.

## Estado de NAV2

NAV2 permanece `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`. No se marca `CLOSED` hasta ejecutar las pruebas .NET runtime/idempotentes pendientes en un entorno con SDK.

## NAV3.4B — Crear transacción

### Comportamiento funcional real

`/transactions/create` no es una superficie de sólo lectura. El componente `TransactionCreateComponent` carga datos auxiliares de consulta y validación —clientes, entidades financieras, conceptos de lote, cuentas contextuales y preview de política— y, al confirmar, ejecuta `POST api/transactions` mediante `TransactionsApiService.createTransaction`.

El endpoint `TransactionsController.CreateTransaction` invoca `AchTransactionService.RegisterTransactionAsync`. El flujo valida la solicitud y persiste la transacción y estado interno relacionado. Según la clasificación/ciclo, puede encolar trabajo interno mediante `CenitCycleQueueService` y crear/asegurar un `ContrapartidaDispatchItem` en estado `PendingContrapartidaReport`. Este endpoint no invoca directamente `IContrapartidaDispatchJobService`, `ProcessTransactionAsync`, clientes SOAP ni generación inmediata de archivos. NAV3.4B no cambia este comportamiento funcional.

### Endpoints utilizados por la pantalla

| Uso | Endpoint | Autorización API vigente | Clasificación |
|---|---|---|---|
| Conceptos de lote | `GET api/transactions/company-entry-descriptions` | `P0.TransactionsRead` = `Transactions.Read OR CanReadAch` | lectura |
| Cuentas contextuales | `GET api/transactions/third-party-accounts/selector` | `P0.TransactionsRead` | lectura |
| Preview de política | `GET api/transactions/policies/preview` | `P0.TransactionsPolicyPreview` = `Transactions.PolicyPreview OR CanReadAch` | lectura/validación |
| Clientes | `GET customers` | `CanReadAch` | lectura |
| Entidades financieras | `GET financial-institutions` | `CanReadAch` | lectura |
| Crear transacción | `POST api/transactions` | `P0.TransactionsCreate` = `Transactions.Create OR CanManageAch` | mutación/persistencia |

Los GET auxiliares mantienen su autorización de lectura; NAV3.4B no los endurece por el hecho de que el submit final sea mutable.

### Autorización anterior

Antes de NAV3.4B:

```text
MenuItem Crear transacción
  roles existentes: Admin OR ACH.Operator
  permisos: CanManageAch OR CanReadAch

SPA /transactions
  parent: Admin OR ACH.Operator AND (CanManageAch OR CanReadAch)

SPA /transactions/create
  child: CanManageAch

API POST api/transactions
  P0.TransactionsCreate = Transactions.Create OR CanManageAch
```

El drift estaba en el menú: por la semántica OR de `MenuItemPermission`, un usuario con rol permitido y sólo `CanReadAch` podía ver `Crear transacción`, aunque el guard hijo no permitía entrar y la API no permitía crear.

### Autorización final NAV3.4B

La capacidad canónica ya existente en backend es `P0.TransactionsCreate`. NAV3.4B la refleja en menú y en el guard hijo:

```text
MenuItem Crear transacción
  roles existentes: sin cambio
  capacidad: Transactions.Create OR CanManageAch
  CanReadAch: NO es alternativa de creación

SPA /transactions/create
  permissionGuard: Transactions.Create OR CanManageAch
  no agrega checks por nombre de rol

API POST api/transactions
  P0.TransactionsCreate = Transactions.Create OR CanManageAch
  sin cambios
```

`Transactions.Create` ya existía en `FineGrainedPermissions`, `AllPermissions` y la policy `P0.TransactionsCreate`. Para que `MenuItemPermission` pueda referenciar esa misma capacidad, NAV3.4B la materializa idempotentemente en el catálogo `Permission` mediante `TransactionsCreatePermissionSeeder`. No se crea una semántica nueva, no se agregan `RolePermissions`, no se cambian grants de `Admin`/`ACH.Operator` y no se otorgan permisos directos a usuarios.

`TransactionsCreateMenuAuthorizationSeeder` se ejecuta después del reconciliador canónico de NAV2 y de Herramientas de prueba; modifica sólo las asociaciones de permiso de `MenuItemConfiguration.TransactionsCreateId`, elimina el fallback `CanReadAch` y conserva las alternativas `Transactions.Create` y `CanManageAch`. No cambia label, ruta, `ParentId`, `Order`, `IsActive` ni asociaciones de rol.

### CanReadAch y prerequisitos de lectura

`CanReadAch` continúa siendo válido para `Consultar transacciones` y para varios GET auxiliares usados por la pantalla de creación. El padre `/transactions` conserva sus guardas legacy existentes porque NAV3.4B no reestructura el módulo ni otras rutas. Esto no crea un bypass: el guard hijo `/transactions/create` exige además `Transactions.Create OR CanManageAch`, y el POST backend aplica la misma policy de creación. Por tanto, `CanReadAch` solo puede habilitar lectura/preparación, nunca el acceso efectivo a la capacidad de creación ni la ejecución del POST.

### Regresión y alcance

- `Consultar transacciones` conserva su autorización de lectura.
- Las demás opciones de `Operación` permanecen intactas.
- `Revisión manual` conserva el modelo resuelto en NAV3.4A.
- `Herramientas de prueba` y `TestTools.Access` permanecen intactos.
- No se crean roles ni grants nuevos.
- NAV2 permanece `IMPLEMENTED_UNVERIFIED_ENVIRONMENT` hasta disponer de validación runtime .NET.

Con estas condiciones, el gap de `Crear transacción` queda cerrado a nivel de diseño/código. Las pruebas runtime .NET y Angular permanecen como deuda de entorno y no se consideran ejecutadas en NAV3.4B.

## NAV3.4C — Cargar archivo NACHA-M

### Comportamiento funcional real

`/transactions/nacha-upload` no es una superficie de sólo lectura. `NachaUploadComponent` valida/clasifica el archivo en cliente, ejecuta `POST NachaUpload/upload`, permite un reproceso explícito del último resultado cuando el backend lo habilita y consulta `GET NachaUpload/records` para mostrar los encabezados ya persistidos.

`NachaUploadController.UploadNachaFile` conserva validaciones de tamaño, nombre/extensión, `ClearingHouseId` para sobres `.env` y MIME type antes de delegar en `IncomingNachaIngestionAppService.IngestAsync`. La ingesta persiste `IncomingNachaFileIngestion`, resultados y eventos de procesamiento; detecta duplicados, resuelve cámara/ciclo/perfil, parsea y persiste el grafo NACHA-M mediante `ParseAndSaveDetailedAsync`, ejecuta el post-procesador y planifica trabajo durable en `IncomingNachaDispatchQueue`. Por tanto la carga/importación sí produce mutación persistente y puede preparar procesamiento operativo posterior.

El request HTTP de upload no posee `IWscfaachSoapClient` ni invoca directamente `IContrapartidaDispatchJobService`: no hay transmisión SOAP/externa directa dentro de la acción de carga. El pipeline asíncrono separado de post-procesamiento sí conserva la capacidad existente de integración externa y el job de contrapartida, bajo sus controles operativos vigentes; NAV3.4C no modifica ese pipeline ni sus guardas.

Existe además `IncomingNachaLocalLivePreparationService`, que puede crear una `AchTransaction` local de preparación únicamente cuando se cumplen simultáneamente `IsDevelopment()`, `RUN_LOCAL_SOAP_PROC_TRANSACCIONES_E2E=true`, `ALLOW_LOCAL_MONETARY_SOAP_E2E=true` y `ProcTransacciones:Mode=Live`. Es un camino de prueba local ya existente y permanece intacto; no habilita creación live en Production por este cambio.

### Endpoints y clasificación

| Uso | Endpoint | Autorización API | Clasificación |
|---|---|---|---|
| Carga/ingesta y reproceso | `POST NachaUpload/upload` | `P1.NachaUpload = Nacha.Upload OR CanManageAch` | mutación persistente / importación |
| Consultar archivos cargados | `GET NachaUpload/records` | `P1.NachaRead = Nacha.Read OR CanReadAch` | lectura/consulta |

No se identificó un endpoint de preview separado en esta pantalla. Las validaciones preliminares son locales/controller y la operación mutable real es el POST de ingesta.

### Autorización anterior

Antes de NAV3.4C, el backend ya tenía la separación correcta: `CanReadAch` no satisfacía `P1.NachaUpload`; `Nacha.Upload` o `CanManageAch` sí. Sin embargo, el hijo SPA exigía sólo `CanManageAch` y el MenuItem `Cargar archivo NACHA-M` no tenía una autorización directa propia después de la reconciliación estructural, de modo que su publicación no representaba explícitamente la capacidad de ingesta. El padre compartido `/transactions` conserva su modelo legacy `Admin OR ACH.Operator AND (CanManageAch OR CanReadAch)`.

### Autorización final NAV3.4C

La fuente de verdad funcional es la policy backend ya existente `P1.NachaUpload`:

```text
MenuItem Cargar archivo NACHA-M
  capacidad directa: Nacha.Upload OR CanManageAch
  CanReadAch: NO es alternativa de carga

SPA /transactions/nacha-upload
  permissionGuard: Nacha.Upload OR CanManageAch
  no agrega checks por nombre de rol

API POST NachaUpload/upload
  P1.NachaUpload = Nacha.Upload OR CanManageAch
  sin cambios

API GET NachaUpload/records
  P1.NachaRead = Nacha.Read OR CanReadAch
  sin cambios
```

`Nacha.Upload` ya existía en `FineGrainedPermissions`, `AllPermissions` y `P1.NachaUpload`. NAV3.4C lo materializa idempotentemente en el catálogo `Permission` mediante `NachaUploadPermissionSeeder` únicamente para que `MenuItemPermission` pueda referenciar la misma capacidad del backend. No se crea permiso conceptual nuevo, no se agregan `RolePermissions`, no se cambian grants de `Admin`/`ACH.Operator` y no se otorgan permisos directamente a usuarios.

`NachaUploadMenuAuthorizationSeeder` se ejecuta después de los reconciliadores previos y modifica sólo las asociaciones de permiso del MenuItem `/transactions/nacha-upload`: conserva `Nacha.Upload` y `CanManageAch` y elimina cualquier otra alternativa directa. No cambia label, ruta, `ParentId`, `Order`, `IsActive` ni asociaciones de rol.

### CanReadAch y padre compartido

`CanReadAch` sigue siendo válido para `GET NachaUpload/records` y para otras superficies de lectura del módulo. El padre `/transactions` permanece sin refactor por alcance: su role/permission guard puede constituir un prerequisito legacy adicional para navegar a la pantalla, pero no puede sustituir la capacidad específica del hijo. Por tanto, `CanReadAch` solo no autoriza el MenuItem de carga, no satisface el guard hijo y no autoriza el POST. Esta asimetría puede restringir a un principal que posea únicamente `Nacha.Upload`, pero no amplía privilegios ni crea un bypass de seguridad.

### Regresión y alcance

- `Crear transacción` conserva el modelo cerrado en NAV3.4B.
- `Revisión manual` conserva el modelo NAV3.4A.
- `Herramientas de prueba`/`TestTools.Access` permanece intacta.
- No se modifica carga masiva, Seguridad de archivos, generación/exportación NACHA-M, Devoluciones ni otras opciones de Operación.
- No se modifica comportamiento funcional de ingesta, parsing, colas o integraciones posteriores.
- NAV2 permanece `IMPLEMENTED_UNVERIFIED_ENVIRONMENT` hasta disponer de validación runtime .NET.

Con estas condiciones, el gap de `Cargar archivo NACHA-M` queda cerrado a nivel de diseño/código. Las pruebas runtime .NET y Angular permanecen como deuda de entorno.


## NAV3.4D — Devoluciones: entrada de lectura y generación protegida

### Comportamiento funcional real

`/transactions/returns` es una superficie **mixta de lectura/preparación y mutación**. `AchReturnsManagementComponent` carga el catálogo de ciclos, consulta las causales válidas para devolución, obtiene las transacciones elegibles del ciclo, permite filtrar/seleccionar y ver detalle local, y sólo después ofrece la acción `Asignar causal y generar .RET`.

Los endpoints realmente consumidos por esta pantalla son:

| Uso | Endpoint | Autorización API | Clasificación |
|---|---|---|---|
| Catálogo de ciclos | `GET api/ach-cycles` | `CanReadAch` | lectura/consulta |
| Catálogo de causales | `GET return-reasons?onlyForReturn=true` | `CanReadAch` | lectura/consulta |
| Transacciones elegibles del ciclo | `GET ach-returns/cycles/{cycleId}/transactions` | `P0.ReturnsRead = Returns.Read OR CanReadAch` | lectura/consulta |
| Generar devolución | `POST ach-returns/generate-file` | `P0.ReturnsGenerateFile = Returns.GenerateFile OR CanManageAch` | mutación persistente / generación de archivo |

La pantalla no expone aprobar/rechazar, cancelar ni reprocesar devoluciones. `Devolución de una devolución` (`/transactions/returns-ror`) es una superficie distinta y permanece fuera de NAV3.4D.

### Efectos de la generación

`AchReturnsService.GenerateReturnsFileAsync` valida elegibilidad, causal y alcance; persiste filas `AchReturnGenerated`; genera el artefacto ReturnOut NACHA-M mediante `INachaFileBuilder`; persiste nombre/auditoría; y aplica la transición de la transacción original a `ReturnedByEpr` mediante el servicio de transición de estados. El payload de auditoría conserva `transmissionStatus = "GeneratedNotTransmitted"` y `productiveStatus = "TechnicalGeneratedOnly"`.

El request de `POST ach-returns/generate-file` **no** invoca `IAchOutboundReturnDispatchService`, `IContrapartidaDispatchJobService` ni `IWscfaachSoapClient`, y no crea una cola asíncrona de transmisión en el flujo inspeccionado. Existe una superficie separada de transporte de devoluciones (`AchOutboundReturnTransportController`), pero `/transactions/returns` no la consume ni la activa automáticamente. NAV3.4D no cambia ningún efecto funcional ni habilita transmisión externa.

### Autorización anterior

Antes de NAV3.4D:

```text
MenuItem Devoluciones
  sin autorización directa específica reconciliada para esta opción

SPA /transactions
  parent: Admin OR ACH.Operator AND (CanManageAch OR CanReadAch)

SPA /transactions/returns
  child: CanManageAch

API lecturas
  GET ach-cycles: CanReadAch
  GET return-reasons: CanReadAch
  GET ach-returns/cycles/{cycleId}/transactions: P0.ReturnsRead = Returns.Read OR CanReadAch

API mutación
  POST ach-returns/generate-file: P0.ReturnsGenerateFile = Returns.GenerateFile OR CanManageAch
```

El backend ya separaba correctamente lectura y escritura. El drift estaba en navegación/SPA: la ruta completa exigía gestión incluso para consultar, mientras el MenuItem no expresaba directamente la capacidad de entrada y la UI no distinguía la acción mutable de la lectura.

### Autorización final NAV3.4D

El modelo final conserva una entrada de lectura y protege la generación como capacidad adicional:

```text
MenuItem Devoluciones
  capacidad directa: CanReadAch

SPA /transactions/returns
  permissionGuard: CanReadAch

Acción SPA "Asignar causal y generar .RET"
  Returns.GenerateFile OR CanManageAch
  gating visual + validación fail-closed dentro de openReasonDialog/generateFile

API lecturas
  sin cambios

API POST ach-returns/generate-file
  P0.ReturnsGenerateFile = Returns.GenerateFile OR CanManageAch
  sin cambios
```

`CanReadAch` se conserva como capacidad de entrada efectiva porque la pantalla completa consume además `AchCyclesController` y `ReturnReasonsController`, cuyos GET siguen requiriendo `CanReadAch`. Aunque `P0.ReturnsRead` también acepta la capacidad fina `Returns.Read`, esa claim por sí sola no basta hoy para cargar todos los datos auxiliares de la pantalla. NAV3.4D no amplía esos controllers compartidos ni refactoriza el padre `/transactions`.

La capacidad mutable fina `Returns.GenerateFile` ya existía en `FineGrainedPermissions`, `AllPermissions` y `P0.ReturnsGenerateFile`. NAV3.4D sólo la materializa idempotentemente en el catálogo `Permission` para que la alternativa fina usada por la SPA sea administrable sin inventar una capacidad nueva. No se agregan `RolePermissions`, grants a `Admin`/`ACH.Operator`, permisos directos a usuarios ni checks por nombre de rol.

### Regresión y alcance

- `/transactions/returns-ror`, su componente, controller, policies y servicios permanecen intactos.
- `Crear transacción` conserva el modelo NAV3.4B.
- `Cargar archivo NACHA-M` conserva el modelo NAV3.4C.
- `Revisión manual` conserva el modelo NAV3.4A.
- `Herramientas de prueba` y `TestTools.Access` permanecen intactos.
- El padre `/transactions` conserva sus guardas legacy existentes; es un prerequisito adicional y no sustituye la capacidad mutable de generación.
- NAV2 permanece `IMPLEMENTED_UNVERIFIED_ENVIRONMENT` hasta disponer de validación runtime .NET.

Con estas condiciones, el gap de `Devoluciones` queda cerrado a nivel de diseño/código, pendiente de las pruebas runtime .NET/Angular que el entorno actual no permite ejecutar.

## NAV3.4E — Seguimiento de cargas: lectura con retry/cancel protegidos

### Comportamiento funcional real

`/transactions/bulk-ingestion/tracking` es una superficie **mixta (modelo B)**. La vista principal no obtiene un listado global del servidor: conserva IDs recientes en `localStorage`, permite buscar/agregar un `batchId`, refresca cada lote con el API, filtra estados localmente y navega al detalle contextual `/transactions/bulk-ingestion/:batchId`. El detalle consulta resumen, historial de intentos e ítems paginados/filtrados.

La superficie sí expone dos mutaciones: **reintentar** y, en el detalle, **cancelar**. No se identificaron acciones de descarga, corrección, eliminación ni una operación adicional de reproceso distinta del retry.

### Endpoints y capacidades backend

| Uso | Endpoint | Autorización API | Clasificación |
|---|---|---|---|
| Estado del lote | `GET api/transactions/bulk-ingestion/{batchId}` | `P1.BulkIngestionRead = BulkIngestion.Read OR CanReadAch` | lectura/seguimiento |
| Ítems del lote | `GET api/transactions/bulk-ingestion/{batchId}/items` | `P1.BulkIngestionRead` | lectura/seguimiento |
| Resumen e intentos | `GET api/transactions/bulk-ingestion/{batchId}/summary` | `P1.BulkIngestionRead` | lectura/seguimiento |
| Reintentar | `POST api/transactions/bulk-ingestion/{batchId}/retry` | `P1.BulkIngestionRetry = BulkIngestion.Retry OR CanManageAch` | mutación / reencolamiento |
| Cancelar | `POST api/transactions/bulk-ingestion/{batchId}/cancel` | `P1.BulkIngestionCancel = BulkIngestion.Cancel OR CanManageAch` | mutación de estado |

`POST api/transactions/bulk-ingestion/upload` comparte controller y servicio Angular, pero pertenece a `/transactions/bulk-ingestion/upload` (`Cargar transacciones desde archivo`) y queda fuera de NAV3.4E.

El backend ya estaba correctamente separado y NAV3.4E no modifica `BulkIngestionController`, las policies P1 ni los servicios de procesamiento. En particular, `CanManageAch` no sustituye la policy de lectura y `CanReadAch`/`BulkIngestion.Read` no satisfacen retry o cancel.

### Efectos operativos de las mutaciones

`AchBulkBatchRetryService.RetryAsync` reinicia a `Ready` los ítems elegibles, limpia su asociación transaccional previa cuando corresponde, crea un nuevo `BulkIngestionAttempt`, marca el lote `Retrying` y llama a `DispatchProcessingAsync`. El procesamiento asíncrono posterior ejecuta `AchBulkBatchProcessingService` y puede llegar a `AchBulkTransactionService.RegisterBulkAsync`, que crea transacciones y conserva la preparación downstream existente (por ejemplo cola de ciclo o `ContrapartidaDispatchItem` pendiente). NAV3.4E no altera ese pipeline.

El request HTTP de retry no posee `IWscfaachSoapClient` ni ejecuta directamente `IContrapartidaDispatchJobService`; por tanto, **no transmite directamente a terceros**. Sí puede iniciar procesamiento asíncrono que, por el diseño existente, prepare trabajo operativo posterior.

`BulkIngestionLifecycleService.RequestCancellationAsync` persiste el estado `Cancelled`, mensaje y fecha de finalización. El código inspeccionado no contiene una operación de unschedule/delete de job, por lo que “cancelar” representa la mutación persistente del lifecycle; no se afirma que detenga físicamente un job Quartz/backplane que ya esté ejecutándose.

### Autorización anterior

Antes de NAV3.4E:

```text
MenuItem Seguimiento de cargas
  Admin/ACH.Operator + CanManageAch (autorización histórica al crearse)

SPA /transactions
  parent: Admin OR ACH.Operator AND (CanManageAch OR CanReadAch)

SPA /transactions/bulk-ingestion/tracking
  CanManageAch

SPA /transactions/bulk-ingestion/:batchId
  CanManageAch

API
  read:   BulkIngestion.Read OR CanReadAch
  retry:  BulkIngestion.Retry OR CanManageAch
  cancel: BulkIngestion.Cancel OR CanManageAch
```

Esto producía el drift: las consultas backend aceptaban una capacidad de lectura, mientras la entrada SPA/MenuItem exigía gestión.

### Autorización final NAV3.4E

```text
MenuItem Seguimiento de cargas
  roles existentes: Admin OR ACH.Operator
  permisos directos: BulkIngestion.Read OR CanReadAch

SPA /transactions/bulk-ingestion/tracking
  BulkIngestion.Read OR CanReadAch

SPA /transactions/bulk-ingestion/:batchId
  BulkIngestion.Read OR CanReadAch

Acción SPA retry
  BulkIngestion.Retry OR CanManageAch
  gating visual + validación fail-closed dentro del método

Acción SPA cancel
  BulkIngestion.Cancel OR CanManageAch
  gating visual + validación fail-closed dentro del método

API
  sin cambios; conserva las tres policies P1 existentes
```

`BulkIngestion.Read`, `BulkIngestion.Retry` y `BulkIngestion.Cancel` ya existían en `FineGrainedPermissions`, `AllPermissions` y las policies P1. NAV3.4E sólo los materializa idempotentemente en el catálogo `Permission` para poder expresar las mismas capacidades en navegación/SPA. No crea permiso conceptual nuevo, no agrega `RolePermissions`, no cambia grants de `Admin`/`ACH.Operator` y no asigna permisos directos a usuarios. `BulkIngestion.Upload` no se materializa ni se modifica en este incremento.

### Efecto del padre `/transactions`

El padre compartido conserva deliberadamente su guard legacy `Admin OR ACH.Operator` más `CanManageAch OR CanReadAch`. Por ello una claim aislada `BulkIngestion.Read` satisface la policy backend y el guard hijo de tracking/detail, pero **no garantiza por sí sola el recorrido SPA completo** si el principal no satisface además el padre. Esta asimetría es más restrictiva y no constituye un bypass. Refactorizar `/transactions` afectaría otras rutas y queda fuera de NAV3.4E.

El MenuItem conserva igualmente sus roles históricos; el reconciliador modifica sólo sus alternativas directas de permiso. No se usa nombre de rol como sustituto de las capacidades mutables.

### Regresión y alcance

- `/transactions/bulk-ingestion/upload` queda intacto; su carga/importación no se modifica.
- `/transactions/bulk-create` permanece fuera del menú y sin cambios.
- Los modelos cerrados en NAV3.4A–NAV3.4D y Herramientas de prueba permanecen intactos.
- No se modifica el comportamiento funcional de retry, cancel, procesamiento asíncrono o integración posterior.
- NAV2 permanece `IMPLEMENTED_UNVERIFIED_ENVIRONMENT` hasta disponer de validación runtime .NET.

Con estas condiciones, el gap de `Seguimiento de cargas` queda cerrado a nivel de diseño/código, pendiente únicamente de las pruebas runtime .NET/Angular que el entorno actual no permite ejecutar.


## NAV3.4F — Certificados digitales (`/nacha-security/certificates`)

`Certificados digitales` se clasifica como **superficie mixta (modelo B)**: la entrada es legítimamente consultable con capacidad de lectura, mientras que la carga/reemplazo y la revocación/eliminación son operaciones administrativas con policies independientes.

### Contrato de autorización por capa

| Capa/operación | Autorización final |
|---|---|
| MenuItem `Certificados digitales` | `Certificates.Read OR CanManageCertificates OR CanReadAch` |
| SPA `/nacha-security/certificates` | `Certificates.Read OR CanManageCertificates OR CanReadAch` |
| SPA `/nacha-security/certificates/:id/versions` | `Certificates.Read OR CanManageCertificates OR CanReadAch` |
| API list / versions | `P1.CertificatesRead = Certificates.Read OR CanManageCertificates OR CanReadAch` |
| SPA preview de carga | `Certificates.UploadPublic OR CanManageCertificates` (más `CanReadAch` como prerequisito UI de los selectores existentes) |
| API `managed/preview` | `P1.CertificatesUploadPublic = Certificates.UploadPublic OR CanManageCertificates` |
| SPA guardar carga | `Certificates.RegisterPrivate OR CanManageCertificates` (más `CanReadAch` como prerequisito UI de los selectores existentes) |
| API `managed` | `P1.CertificatesRegisterPrivate = Certificates.RegisterPrivate OR CanManageCertificates` |
| SPA revocar/eliminar | `Certificates.Revoke OR CanManageCertificates` |
| API revoke/delete | `P1.CertificatesRevoke = Certificates.Revoke OR CanManageCertificates` |

El manager aplica doble defensa a las acciones mutables: gating visual y validación fail-closed dentro del método antes de ejecutar el servicio. No hay checks por nombre de rol.

### Lectura fina y dependencias auxiliares

Los catálogos compartidos de cámaras compensadoras y entidades financieras continúan protegidos por `CanReadAch`. Para que `Certificates.Read` sea una capacidad de consulta funcional real sin ampliar esos controllers, el manager omite esos GET auxiliares cuando falta `CanReadAch` y conserva la consulta principal de certificados. Esto permite lista/detalle/versiones con la capacidad fina de lectura sin abrir nuevos permisos en APIs compartidas.

El wizard de carga sí necesita actualmente dichos selectores; por ello la UI sólo publica el flujo completo cuando también existe `CanReadAch`. Esta condición es más restrictiva que las policies de carga individuales y no permite a una capacidad de lectura sustituir `Certificates.UploadPublic`, `Certificates.RegisterPrivate` o `CanManageCertificates`.

### Material criptográfico

La normalización de autorización no cambia el manejo criptográfico:

- el material privado se protege antes de persistirse como `EncryptedPrivateMaterial`;
- la sustitución de versiones mantiene transición `Replaced`, historial de rotación y auditoría;
- la revocación mantiene estado/fecha/motivo/actor;
- la eliminación conserva validaciones de referencias operativas y limpieza de material privado cifrado;
- la UI de certificados no ofrece descarga de material criptográfico sensible.

Los endpoints backend existentes de activación, validación y auditoría conservan sus policies específicas (`Certificates.Activate`, `Certificates.Validate`, `Certificates.Audit` OR `CanManageCertificates`) y no se exponen como nuevas acciones de la pantalla en NAV3.4F.

### Fuera de alcance preservado

`/nacha-security/sobre-digital` y las cuatro rutas técnicas/manuales (`nacha/generate`, `nacha/generate-encrypted`, `digital-envelope/manual-encrypt`, `digital-envelope/manual-decrypt`) no se modifican. La raíz `/nacha-security` conserva su configuración; cualquier normalización de autorización de esa raíz o de `Cifrar y descifrar archivos` requiere un incremento separado.

## NAV3.4G — Registros del sistema: lectura sensible, padre estructural y permiso pendiente

El dominio `Administración > Registros del sistema` contiene tres vistas administrativas que el código vigente caracteriza como **read-only pero sensibles**:

| Superficie | Datos visibles | Mutación desde la UI | Frontera vigente antes de NAV3.4G |
|---|---|---|---|
| `/audit-logs` | entidad/ID, acción, actor, fecha, campos cambiados | ninguna | MenuItem `CanManageUsers`; SPA/API sólo autenticados |
| `/auth-logs` | usuario, éxito/fallo, motivo, IP, User-Agent, fecha | ninguna | MenuItem `CanManageUsers`; SPA/API sólo autenticados |
| `/navigation-logs` | usuario, ruta, sesión, duración, IP, User-Agent, fecha | ninguna en la vista administrativa | MenuItem/GET API `CanReadAch`; SPA sólo autenticado |

La escritura `POST api/navigation-logs` es **telemetría automática**, no una acción administrativa de la pantalla. Conserva autenticación de controller y no se condiciona a `CanReadAch` para evitar que una frontera de consulta rompa el registro de navegación de sesiones autenticadas.

### Capacidad semántica

No existe actualmente una capacidad general que represente inequívocamente lectura de registros del sistema. `CanManageUsers` modela gestión de usuarios, no auditoría general; `CanReadAch` modela lectura ACH, no necesariamente actividad de navegación/autenticación. `Certificates.Audit` y `CanViewNachaSecurityAudit` son capacidades específicas de otros dominios.

NAV3.4G, por diseño, **no crea** `SystemLogs.Read`, `AuditLogs.Read`, `AuthLogs.Read` ni `NavigationLogs.Read` y no agrega roles/grants. En consecuencia:

- Auditoría y Accesos mantienen sus fronteras existentes y el drift MenuItem vs URL directa/API permanece como deuda explícita.
- Navegación alinea únicamente la SPA con el GET backend vigente `CanReadAch`, sin afirmar que esa sea la capacidad semántica definitiva.

### Padre estructural `/logs`

`GetMenuForCurrentUserHandler` filtra filas antes de reconstruir jerarquía; si un hijo visible pierde su padre filtrado, el algoritmo lo agrega como root. NAV3.4G no modifica ese comportamiento global.

`SystemRecordsMenuAuthorizationSeeder` (Order 107) reconcilia sólo el padre estructural `Registros del sistema` a:

```text
CanManageUsers OR CanReadAch
```

Esto hace al padre compatible con las fronteras actuales de sus hijos sin otorgar acceso a éstos: el filtro sigue evaluando cada hijo individualmente. Un usuario autorizado sólo a navegación recibe `Administración > Registros del sistema > Registro de navegación`, no un `Registro de navegación` flotante en raíz; un usuario sin permiso de un hijo no lo recibe por el solo hecho de ver el padre.

El agrupador conserva `Order 30` bajo Administración y sus hijos conservan 10/20/30. No se cambian rutas, labels ni ParentId.

### Estado final por superficie en NAV3.4G

```text
Registros del sistema (padre estructural)
  MenuItem: CanManageUsers OR CanReadAch

Registro de auditoría
  MenuItem: CanManageUsers
  SPA: authGuard raíz
  GET API: [Authorize]
  estado: PENDIENTE permiso semántico de lectura

Registro de accesos
  MenuItem: CanManageUsers
  SPA: authGuard raíz
  GET API: [Authorize]
  estado: PENDIENTE permiso semántico de lectura

Registro de navegación
  MenuItem: CanReadAch
  SPA GET/lista: permissionGuard + CanReadAch
  GET API: CanReadAch
  POST telemetría: [Authorize], independiente
  estado: capas alineadas con frontera vigente; semántica final del permiso PENDIENTE
```

`/reports/audit` permanece una superficie distinta bajo Reportes y conserva su autorización/contrato. NAV3.4G tampoco modifica otros hijos de Administración ni ningún modelo cerrado NAV3.4A–NAV3.4F.

Por tanto, NAV3.4G es un **cierre parcial**: corrige la estructura padre/hijo y la evasión por URL de la vista de navegación respecto de su GET API, pero el dominio no debe declararse completamente cerrado hasta diseñar y aplicar una capacidad de lectura específica para Registros del sistema.

## NAV3.4G2 — Registros del sistema: capacidades semánticas separadas y cierre del bypass directo

NAV3.4G2 reemplaza la solución transitoria de NAV3.4G por capacidades propias del dominio de logs, sin cambiar todavía la población efectiva que accede a cada superficie.

### Capacidades finales

Se crean y persisten idempotentemente en `Permission` las siguientes capacidades, siguiendo la convención `Domain.Action` ya usada por `FineGrainedPermissions`:

| Capacidad | Descripción funcional |
|---|---|
| `AuditLogs.Read` | Consultar registro de auditoría |
| `AuthLogs.Read` | Consultar registro de accesos |
| `NavigationLogs.Read` | Consultar registro de navegación |

`SystemRecordPermissionsSeeder` es el seeder de catálogo usado para las tres. Las capacidades también se agregan a `FineGrainedPermissions.AllPermissions`.

No se crean roles nuevos, `RolePermissions`, grants a `Admin` o `ACH.Operator`, ni grants directos a usuarios. Los permisos legacy permanecen temporalmente como alternativas de compatibilidad.

### Policies de migración

Se agregan tres policies P1 que reutilizan `RequireAssertion` y modelan OR explícito:

```text
P1.AuditLogsRead
  AuditLogs.Read OR CanManageUsers

P1.AuthLogsRead
  AuthLogs.Read OR CanManageUsers

P1.NavigationLogsRead
  NavigationLogs.Read OR CanReadAch
```

No hay checks por nombre de rol y las capacidades nuevas no son requisitos adicionales AND sobre las legacy.

### Autorización por capa: antes y después

| Superficie | Capa | Antes de NAV3.4G2 | Final NAV3.4G2 |
|---|---|---|---|
| Registro de auditoría | MenuItem `/audit-logs` | `CanManageUsers` | `AuditLogs.Read OR CanManageUsers` |
| Registro de auditoría | SPA | sólo autenticado | `AuditLogs.Read OR CanManageUsers` |
| Registro de auditoría | GET API | `[Authorize]` | `P1.AuditLogsRead` |
| Registro de accesos | MenuItem `/auth-logs` | `CanManageUsers` | `AuthLogs.Read OR CanManageUsers` |
| Registro de accesos | SPA | sólo autenticado | `AuthLogs.Read OR CanManageUsers` |
| Registro de accesos | GET API | `[Authorize]` | `P1.AuthLogsRead` |
| Registro de navegación | MenuItem `/navigation-logs` | `CanReadAch` | `NavigationLogs.Read OR CanReadAch` |
| Registro de navegación | SPA | `CanReadAch` desde NAV3.4G | `NavigationLogs.Read OR CanReadAch` |
| Registro de navegación | GET API | `CanReadAch` | `P1.NavigationLogsRead` |

Con esto, un principal que sólo esté autenticado pero no tenga ninguna de las alternativas ya no puede consultar `api/audit-logs` ni `api/auth-logs` ni entrar por URL directa a sus rutas SPA. La audiencia persistida actual no cambia porque todavía no existen grants a los tres permisos nuevos y se conservan los fallbacks legacy.

`/reports/audit` permanece completamente separado bajo Reportes y conserva su autorización y contrato existentes.

### POST de telemetría de navegación

La escritura automática de telemetría sigue separada de la consulta administrativa:

```text
POST api/navigation-logs
  [Authorize] de clase
  NO exige P1.NavigationLogsRead
  NO exige NavigationLogs.Read
  NO exige CanReadAch
```

El cliente `NavigationLogService` continúa enviando telemetría sólo cuando hay sesión autenticada. NAV3.4G2 no cambia su payload, persistencia ni comportamiento funcional.

### Padre estructural `Registros del sistema`

`/logs` conserva su posición y función:

```text
Administración
└── Registros del sistema   Order 30
    ├── Registro de auditoría   Order 10
    ├── Registro de accesos     Order 20
    └── Registro de navegación  Order 30
```

`SystemRecordsMenuAuthorizationSeeder` mantiene `Order 107` y reconcilia las asociaciones finales a:

```text
Padre /logs
  AuditLogs.Read
  OR AuthLogs.Read
  OR NavigationLogs.Read
  OR CanManageUsers
  OR CanReadAch

/audit-logs
  AuditLogs.Read OR CanManageUsers

/auth-logs
  AuthLogs.Read OR CanManageUsers

/navigation-logs
  NavigationLogs.Read OR CanReadAch
```

El padre sólo controla su propia visibilidad. No concede permisos a hijos y cada hijo sigue evaluándose de forma independiente.

Casos explícitos:

- sólo `NavigationLogs.Read` -> padre visible + Registro de navegación; Auditoría y Accesos no aparecen;
- `CanManageUsers` -> padre visible + Auditoría + Accesos; Navegación no aparece salvo `CanReadAch` o `NavigationLogs.Read`;
- `AuditLogs.Read` solo -> padre visible + Auditoría únicamente;
- `AuthLogs.Read` solo -> padre visible + Accesos únicamente.

`GetMenuForCurrentUserHandler` no se modifica. La unión de capacidades del padre permite representar correctamente los hijos sin introducir el refactor general de orphaning/leakage que NAV3.4G había evitado.

### Persistencia y grants

`SystemRecordPermissionsSeeder` usa el patrón idempotente vigente de otros permisos finos: busca primero por nombre, luego por ID estable, crea la fila si no existe y normaliza `Name`/`Description`.

No se modifica `RolePermissionConfiguration`, no se crean `RolePermission` desde los seeders de NAV3.4G2 y no existe una asignación directa nueva a usuarios. Por tanto, Admin y ACH.Operator no reciben automáticamente ninguna de las tres capacidades nuevas.

### Validación y estado

Validaciones estáticas ejecutadas en el entorno del incremento:

- grafo de solución: PASS;
- Herramientas de prueba NAV3.3B: PASS;
- regresiones NAV3.4B, NAV3.4C, NAV3.4D, NAV3.4E y NAV3.4F: PASS;
- NAV3.4G2 focal: PASS, 85 aserciones sobre permisos, persistencia, policies, menú, SPA, API, telemetría, padre/hijos y separación de `/reports/audit`.

Pruebas runtime diferidas:

- .NET: **NOT_RUN_ENVIRONMENT**, `dotnet` no está instalado;
- Angular: **NOT_RUN_ENVIRONMENT**, `node_modules` no está instalado.

NAV2 continúa **IMPLEMENTED_UNVERIFIED_ENVIRONMENT** y no debe marcarse CLOSED hasta disponer de la validación runtime pendiente.

### Deuda posterior

El gap técnico de `Registros del sistema` puede considerarse cerrado a nivel de diseño/código: ya no queda bypass conocido sólo-autenticado en las tres consultas administrativas y el padre representa las alternativas de sus hijos sin orphaning conocido.

La deuda restante es de **migración de grants**: decidir explícitamente qué roles o poblaciones deben recibir `AuditLogs.Read`, `AuthLogs.Read` y `NavigationLogs.Read`, migrar esos grants desde `CanManageUsers`/`CanReadAch`, validar la audiencia resultante y sólo después retirar los fallbacks legacy en un incremento separado.


## NAV3.4H — Organización del menú: frontera semántica de administración

NAV3.4H trata `/navigation/menu-items` como una superficie administrativa sensible porque el editor no sólo cambia presentación: persiste `Label`, `Route`, `Icon`, `ParentId`, `Order`, `Exact`, `IsActive` y también las asociaciones `MenuItemRole` y `MenuItemPermission`. El CRUD no modifica `RolePermission`, `UserRole` ni grants directos.

### Drift previo

Antes del incremento, las capas no expresaban una única frontera:

| Capa | Autorización previa |
|---|---|
| MenuItem | rol `Admin` AND `CanManageUsers` |
| SPA `/navigation` | rol `Admin` AND `CanManageUsers` |
| SPA `/navigation/menu-items` | sin guard hijo; dependía del prefijo |
| API GET/POST/PUT/DELETE | `[Authorize(Roles = "Admin")]` |
| controles mutables UI | `Admin` AND `CanManageUsers` |

`CanManageUsers` no describe semánticamente la administración de la estructura de navegación y el rol `Admin` tampoco es una capability funcional. La inspección de `FineGrainedPermissions` no encontró un permiso equivalente existente.

### Capability y policy

La capability canónica creada es:

```text
NavigationMenu.Manage
Administrar organización del menú
```

`NavigationMenuPermissionSeeder` la persiste idempotentemente en `Permission` sin crear `RolePermission` ni grants a usuarios. La policy de migración es:

```text
P1.NavigationMenuManage
  NavigationMenu.Manage OR CanManageUsers
```

El fallback no se eligió por semejanza semántica sino por compatibilidad de audiencia: en el estado fuente canónico sólo `Admin` recibe `CanManageUsers`; `ACH.Operator` no lo recibe. Como `NavigationMenu.Manage` nace sin grants, la población fuente que accede no aumenta. La equivalencia no se afirma sobre bases desplegadas no inspeccionadas; no hubo runtime DB disponible.

### Menú

`NavigationMenuAuthorizationSeeder` ejecuta después de la canonicalización IA y reconcilia `Organización del menú` a dos `MenuItemPermission` alternativas:

```text
NavigationMenu.Manage OR CanManageUsers
```

El `MenuItemRole` `Admin` se elimina para esta opción. Esto es necesario porque `GetMenuForCurrentUserHandler` combina requisitos de rol y permiso con AND entre categorías; mantener simultáneamente `Admin` y la nueva capability convertiría accidentalmente la frontera en `Admin AND (NavigationMenu.Manage OR CanManageUsers)`.

La entrada conserva `Administración -> Organización del menú`, ruta `/navigation/menu-items` y `Order 40`; no se cambia la arquitectura NAV2.

### SPA

`/navigation` continúa como prefijo estructural del módulo y su redirect vacío sigue apuntando a `menu-items`. Tanto el prefijo como la ruta efectiva `/navigation/menu-items` exigen:

```text
NavigationMenu.Manage OR CanManageUsers
```

La ruta hija tiene guard explícito, por lo que una navegación directa a `/navigation/menu-items` no depende exclusivamente del guard del padre. El componente usa la misma alternativa para sus acciones mutables y no hace check por nombre de rol.

### API

`MenuItemsController`, disponible por `navigation/menu-items` y `api/navigation/menu-items`, aplica `P1.NavigationMenuManage` a toda su superficie:

- `GET` lista la jerarquía;
- `POST` crea;
- `PUT {id}` actualiza estructura, estado y asociaciones rol/permiso;
- `DELETE {id}` elimina una opción sin hijos.

No existen endpoints separados de reorder/PATCH/enable-disable: reordenar, mover y activar/desactivar se realizan mediante `PUT`. El rol `Admin` ya no es la frontera hardcodeada del API; Admin conserva acceso por `CanManageUsers`.

### Lecturas auxiliares de roles y permisos

El editor carga `GET api/roles` y `GET api/permissions` para poblar sus selectores de `RoleIds`/`PermissionIds`. Para que `NavigationMenu.Manage` sea funcionalmente autocontenida cuando se migren grants, sin otorgar permisos de gestión de usuarios, las policies de lectura aceptan:

```text
P1.RolesRead       = Roles.Read OR NavigationMenu.Manage OR CanReadAch
P1.PermissionsRead = Permissions.Read OR NavigationMenu.Manage OR CanReadAch
```

Las policies de creación/edición/eliminación de roles y de asignación de permisos no se cambian. Mientras la capability nueva no tenga grants, estas alternativas no expanden la audiencia actual.

### Escalación y límite de confianza

El poseedor de `NavigationMenu.Manage` puede alterar globalmente qué links se muestran, su ruta, jerarquía, orden, estado y qué roles/permisos controlan su visibilidad. Ese poder puede confundir o exponer un link hacia otra superficie, pero **no otorga por sí mismo la autorización del destino**: guards SPA y policies API continúan siendo la frontera de negocio.

La pantalla no contiene una operación que agregue `RolePermission`, cambie `UserRole` o emita claims, por lo que no se identifica un vector directo de autoescalación desde este editor. Si una ruta/API destino ya tuviera un gap de autorización independiente, hacerlo visible podría facilitar descubrirlo; ese riesgo pertenece al gap de la superficie destino y NAV3.4H no amplía el alcance para corregirlo.

No se conoce un bypass de URL/API para `Organización del menú` después de la alineación. La deuda futura es migrar explícitamente los grants desde `CanManageUsers` hacia `NavigationMenu.Manage` y después retirar el fallback, sin hacerlo automáticamente en NAV3.4H.


## NAV3.4I — Capacidades de la plataforma: frontera semántica read-only

NAV3.4I normaliza `/payment-rail-capability-registry` sin cambiar su comportamiento funcional. La superficie continúa siendo una consulta administrativa de capacidades efectivas por riel y no una consola de configuración.

### Superficie publicada

La SPA usa `PaymentRailCapabilityRegistryPageComponent` y `PaymentRailCapabilityRegistryApiService`. Los únicos endpoints consumidos y publicados por `PaymentRailCapabilityRegistryController` son:

```text
GET api/payment-rails/capability-registry/rails
GET api/payment-rails/capability-registry/rails/{railCode}/capabilities
GET api/payment-rails/capability-registry/rails/{railCode}/capabilities/{capabilityCode}
```

El controller delega directamente en `IPaymentRailCapabilityRegistryService`/`PaymentRailCapabilityRegistryService`; no existe un handler MediatR/read-model intermedio para esta superficie. No existen en este controller acciones `POST`, `PUT`, `PATCH` o `DELETE`, y la UI no ofrece edición, enable/disable, cutover ni cambios de riel/capability. El servicio interno conserva `UpsertCapabilityAsync` para persistir overrides, pero NAV3.4I confirma que esa operación no está expuesta por este controller ni invocada por esta UI. Su eventual autorización pertenece a otra superficie y no se cubre con la policy de lectura por inferencia.

### Capability canónica y persistencia

La capability ya existente sigue siendo:

```text
CanViewPaymentRailCapabilityRegistry
Consultar capacidades de la plataforma
```

Antes de NAV3.4I existía como constante/policy pero no como fila `Permission` persistida. `PaymentRailCapabilityRegistryPermissionSeeder` la materializa idempotentemente con ID estable `9c178737-a650-4437-a68b-9b3f21555e02`, búsqueda por nombre/ID y normalización de descripción. También se incorpora a `FineGrainedPermissions.AllPermissions`.

No se crea una capability conceptual nueva, no se crean roles y no se agregan `RolePermission` ni grants directos a usuarios.

### Policy efectiva

La policy backend existente se conserva sin cambios porque ya expresa la frontera de migración requerida:

```text
CanViewPaymentRailCapabilityRegistry
OR CanManageAch
OR CanReadAch
```

El nombre de la policy coincide con la capability dedicada, pero su evaluación **no** exige exclusivamente esa claim: `CanManageAch` y `CanReadAch` continúan como fallbacks legacy hasta una migración explícita de grants.

### Drift previo

Antes de NAV3.4I:

| Capa | Autorización |
|---|---|
| MenuItem | `(Admin OR ACH.Operator) AND CanReadAch` |
| SPA | `(Admin OR ACH.Operator) AND (CanViewPaymentRailCapabilityRegistry OR CanManageAch OR CanReadAch)` |
| API GET | `CanViewPaymentRailCapabilityRegistry OR CanManageAch OR CanReadAch` |

La restricción por rol de menú/SPA no formaba parte de la policy funcional del API.

### Audiencia fuente y decisión fail-safe

El seed canónico define únicamente `Admin` y `ACH.Operator`; ambos reciben `CanManageAch` y `CanReadAch` mediante `RolePermissionConfiguration`. Ninguno recibe `CanViewPaymentRailCapabilityRegistry` en NAV3.4I.

En ese estado fuente, remover el role gate redundante no agrega ninguna identidad sembrada: Admin y ACH.Operator ya satisfacen los fallbacks y siguen siendo la población efectiva. La normalización no otorga permisos nuevos.

No hubo base runtime desplegada disponible. Por tanto, no se afirma que instalaciones externas carezcan de roles personalizados con `CanManageAch`/`CanReadAch`; la garantía de no expansión se refiere al artefacto fuente canónico y sus grants sembrados.

### Menú final

`PaymentRailCapabilityRegistryMenuAuthorizationSeeder` (Order 109) ejecuta después de la canonicalización NAV2 y de los reconciliadores NAV3.4G2/NAV3.4H. Conserva:

```text
Administración
└── Capacidades de la plataforma   Order 50
    /payment-rail-capability-registry
```

Y deja exactamente estas alternativas `MenuItemPermission`:

```text
CanViewPaymentRailCapabilityRegistry
OR CanManageAch
OR CanReadAch
```

Los `MenuItemRole` Admin/ACH.Operator se eliminan del hijo. `GetMenuForCurrentUserHandler` combina categorías de rol/permiso con AND, por lo que mantener un role gate junto con la capability dedicada habría creado una frontera distinta de la policy permission-based.

El padre `Administración` y sus demás hijos no se modifican.

### SPA final

La ruta efectiva conserva protección directa y deja de depender de nombres de rol:

```text
/payment-rail-capability-registry
  permissionGuard
  permissions:
    CanViewPaymentRailCapabilityRegistry
    OR CanManageAch
    OR CanReadAch
```

No existe bypass por ocultamiento de menú: navegar directamente a la URL sigue evaluando el guard.

### API final

Los tres GET mantienen:

```text
[Authorize(Policy = FineGrainedPermissions.CanViewPaymentRailCapabilityRegistry)]
```

La implementación de la policy conserva las tres alternativas anteriores y no contiene `IsInRole` ni checks `Admin`/`ACH.Operator`.

### Deuda posterior

El gap puede considerarse cerrado a nivel de diseño/código. La deuda restante es deliberadamente de grants:

1. decidir la población objetivo de `CanViewPaymentRailCapabilityRegistry`;
2. agregar los grants explícitos en un incremento separado;
3. validar la audiencia runtime;
4. retirar sólo entonces `CanManageAch` y `CanReadAch` como fallbacks.

NAV3.4I no ejecuta ninguna de esas cuatro etapas.

## NAV3.4J — Sobre digital administrado: entrada de lectura y operaciones criptográficas separadas

NAV3.4J normaliza únicamente `/nacha-security/sobre-digital` (`Cifrar y descifrar archivos`). No modifica Certificados digitales, algoritmos, formato del sobre, proveedores de secretos ni las rutas técnicas/manuales.

### Superficie y efectos

La SPA prepara el contexto con dos lecturas:

```text
GET api/nacha-security/digital-envelope/certificates
  P1.CertificatesRead

GET api/clearing-houses/operational
  CanReadAch
```

Y ejecuta dos operaciones sensibles:

```text
POST api/nacha-security/digital-envelope/encrypt
  P1.DigitalEnvelopeEncrypt
  DigitalEnvelope.Encrypt OR CanManageAch

POST api/nacha-security/digital-envelope/decrypt
  P1.DigitalEnvelopeDecrypt
  DigitalEnvelope.Decrypt OR CanManageAch
```

El cifrado firma y cifra del lado servidor; el descifrado recupera el contenido y valida firma/integridad antes de responder. Ambos POST reciben el archivo por multipart y devuelven el artefacto binario en la misma respuesta. La UI guarda ese `Blob` y puede volver a guardar la respuesta cacheada en memoria; no existe un endpoint de descarga separado en esta superficie.

El flujo inspeccionado persiste únicamente `DigitalEnvelopeOperationLog` con metadatos, tamaños, hashes, certificado, resultado/error y actor. No escribe el contenido cifrado/descifrado como artefacto persistente y no inicia transmisión SOAP/MFT/SFTP ni comunicación a terceros.

### Modelo de autorización

El modelo es **mixto (B)**:

- **entrada/preparación**: `CanReadAch`, porque la pantalla depende de `GET api/clearing-houses/operational` y esa es su policy vigente;
- **cifrado**: `DigitalEnvelope.Encrypt OR CanManageAch`;
- **descifrado**: `DigitalEnvelope.Decrypt OR CanManageAch`.

`CanReadAch` nunca sustituye la autorización criptográfica. La ruta puede abrirse con lectura, pero el botón y el método del componente vuelven a comprobar la capability operativa antes de invocar el POST; el backend repite la frontera de seguridad con su policy P1.

### Capability catalog

`DigitalEnvelope.Encrypt` y `DigitalEnvelope.Decrypt` ya existían en `FineGrainedPermissions`, `AllPermissions` y policies, pero no estaban materializadas en `Permission`. `DigitalEnvelopeOperationPermissionSeeder` crea/reconcilia idempotentemente:

```text
DigitalEnvelope.Encrypt
  d951161d-06c5-4e31-a574-188e5094f59d
  Cifrar archivos mediante sobre digital

DigitalEnvelope.Decrypt
  7ccf7a90-d66f-4e63-bcb9-92275118c7e1
  Descifrar archivos mediante sobre digital
```

No se crean `RolePermission`, roles ni grants a usuarios. `Nacha.Read`, `Nacha.Generate` y `NachaSecurity.Read` no son las fronteras de estos endpoints; `NachaSecurity.ManualEncrypt`/`ManualDecrypt` y los aliases `CanManual*` pertenecen a las rutas manuales; `CanDownloadEnvelope`/`NachaSecurity.AuthorizeDownload` no aplican porque esta pantalla no tiene un download endpoint separado.

### Menú y SPA

Antes de NAV3.4J el MenuItem no tenía restricciones directas, mientras la ruta SPA aceptaba:

```text
CanManualEncryptEnvelope
OR CanManualDecryptEnvelope
OR CanManageAch
OR CanReadAch
```

Ese conjunto no reflejaba las policies de los endpoints administrados. Los aliases `CanManual*` corresponden a rutas manuales distintas y no deben representar `/sobre-digital`.

Después de NAV3.4J:

```text
MenuItem Cifrar y descifrar archivos
  CanReadAch
  sin MenuItemRole

SPA /nacha-security/sobre-digital
  CanReadAch

Acción cifrar
  DigitalEnvelope.Encrypt OR CanManageAch

Acción descifrar
  DigitalEnvelope.Decrypt OR CanManageAch
```

Usar sólo `CanReadAch` para el MenuItem no autoriza cifrado/descifrado; representa la capacidad mínima para inicializar una pantalla mixta. Incluir `DigitalEnvelope.Encrypt` o `.Decrypt` como alternativas OR del MenuItem/ruta habría sido incorrecto porque esas claims por sí solas no autorizan el GET de cámaras operativas requerido por la UI.

### Material criptográfico

Los DTO de preparación sólo exponen metadatos y thumbprints enmascarados. El material privado no cruza al navegador. `ManagedDigitalEnvelopeService` llama `ICertificateSecretResolver` para abrir la identidad privada en backend; el proveedor de base de datos usa `ICertificatePrivateMaterialProtector.Unprotect` sobre `EncryptedPrivateMaterial`, mientras el proveedor in-memory carga PKCS#12 con `EphemeralKeySet`. Las referencias de secreto se enmascaran.

NAV3.4J no cambia AES/RSA/SHA, firma, IV, formato XML, validación de firma, vigencia, rotación o selección de certificados. Tampoco crea ninguna descarga de llave privada.

### Audiencia y migración futura

`Admin` y `ACH.Operator` ya tienen `CanReadAch` y `CanManageAch` en los seeds canónicos; ambos conservan la misma capacidad funcional. No reciben `DigitalEnvelope.Encrypt` ni `.Decrypt` en NAV3.4J.

Un principal con sólo `CanReadAch` puede consultar/preparar pero no ejecutar crypto. Un principal con sólo `CanManageAch` continúa autorizado por el API crypto, pero la SPA completa requiere `CanReadAch` para cargar el contexto operativo; NAV3.4J hace fail-closed en la ruta en vez de presentar como usable ese camino incompleto. Una futura asignación de capabilities finas deberá incluir deliberadamente la dependencia de lectura necesaria mientras el GET operacional conserve su policy actual.

La deuda posterior es migrar grants hacia `DigitalEnvelope.Encrypt`/`.Decrypt`, validar audiencia runtime y sólo entonces evaluar retirar `CanManageAch` como fallback. Un eventual permiso dedicado de lectura para Sobre digital, si se considera necesario, requiere otro incremento y no se inventa aquí.

### Superficies explícitamente intactas

- `/nacha-security/certificates`: modelo NAV3.4F intacto.
- `/nacha-security/nacha/generate`: intacta y fuera del menú como opción independiente.
- `/nacha-security/nacha/generate-encrypted`: intacta y fuera del menú como opción independiente.
- `/nacha-security/digital-envelope/manual-encrypt`: intacta.
- `/nacha-security/digital-envelope/manual-decrypt`: intacta.
- `GetMenuForCurrentUserHandler`: intacto.

## NAV3.4K — Devolución de una devolución: entrada de lectura y generación mutable separada

NAV3.4K normaliza únicamente `/transactions/returns-ror` (`Devolución de una devolución`) y conserva `/transactions/returns` bajo el modelo cerrado en NAV3.4D.

### Comportamiento funcional y endpoints consumidos

La pantalla no lista devoluciones ni busca un catálogo de candidatas. El operador introduce manualmente el ID de la devolución origen, la nueva causal y, para generación, uno o más `flowId` ya existentes. La SPA consume exactamente tres operaciones:

```text
POST ach-returns/return-of-return/evaluate
  P0.ReturnsRead
  Returns.Read OR CanReadAch

POST ach-returns/return-of-return/generate-audit-file
  P0.ReturnsGenerateFile
  Returns.GenerateFile OR CanManageAch

POST ach-returns/return-of-return/generate-nacha-file
  P0.ReturnsGenerateFile
  Returns.GenerateFile OR CanManageAch
```

`evaluate` es una operación de consulta/validación: lee transacción origen, reglas, ciclos y causales con consultas sin tracking y no ejecuta `SaveChanges`. No crea un `ReturnOfReturnFlow` ni modifica estados.

Las dos operaciones de generación son mutables porque persisten evidencia de archivo generado (`AchReturnOfReturnGeneratedFileAudit` y sus vínculos de flujo). Trabajan sobre `ReturnOfReturnFlow` ya existentes; esta pantalla no registra el ROR de negocio. El modo auditoría genera un archivo interno y el modo productivo genera NACHA-M, ambos devueltos en la respuesta para descarga del navegador.

El controller contiene además `POST ach-returns/return-of-return/cenit/out`, también bajo `P0.ReturnsGenerateFile`, capaz de delegar creación CENIT. **La SPA de `/transactions/returns-ror` no consume ese endpoint** y NAV3.4K no lo incorpora ni lo modifica.

No se encontró dispatch, enqueue, cliente SOAP, MFT, `HttpClient` ni transporte externo directo en el servicio de generación usado por los dos endpoints de la pantalla. Generar el archivo prepara un artefacto operativo; la solicitud no lo transmite a terceros.

### Capability catalog

Las dos capabilities canónicas ya existían:

```text
Returns.Read
Returns.GenerateFile
```

`Returns.GenerateFile` ya estaba materializada por NAV3.4D. `Returns.Read` estaba declarada en `FineGrainedPermissions`, incluida en `AllPermissions` y usada por `P0.ReturnsRead`, pero faltaba en el catálogo `Permission`. NAV3.4K agrega `ReturnsReadPermissionSeeder`:

```text
Returns.Read
  fa144b62-95c2-4540-b2f8-110516eeb837
  Consultar devoluciones ACH
```

El seeder es idempotente y no crea roles, `RolePermission` ni permisos directos a usuarios.

### Modelo de autorización

El modelo demostrado por backend es **B — superficie mixta**:

- **entrada/evaluación**: `Returns.Read OR CanReadAch`;
- **generación de auditoría**: `Returns.GenerateFile OR CanManageAch`;
- **generación NACHA-M productiva**: `Returns.GenerateFile OR CanManageAch`.

No hay una semántica en la que lectura sustituya generación. `CanReadAch` solo no satisface `P0.ReturnsGenerateFile`, y `CanManageAch` solo no satisface `P0.ReturnsRead`.

### Drift anterior y autorización final

Antes de NAV3.4K:

| Capa | Autorización |
|---|---|
| MenuItem | `CanManageAch` |
| SPA child `/transactions/returns-ror` | `CanManageAch` |
| API evaluate | `Returns.Read OR CanReadAch` |
| API generate audit/NACHA | `Returns.GenerateFile OR CanManageAch` |

El MenuItem y la ruta trataban toda la pantalla como mutable, aunque el backend separaba la evaluación de lectura.

Después de NAV3.4K:

```text
MenuItem /transactions/returns-ror
  Returns.Read OR CanReadAch
  sin MenuItemRole directo

SPA child /transactions/returns-ror
  Returns.Read OR CanReadAch

Acciones Generate audit / Generate NACHA
  Returns.GenerateFile OR CanManageAch
  gating visual + comprobación fail-closed dentro del método

API
  sin cambios: P0.ReturnsRead / P0.ReturnsGenerateFile
```

El padre SPA `/transactions` permanece deliberadamente intacto y continúa exigiendo `Admin OR ACH.Operator` **y** `CanManageAch OR CanReadAch`. Ese gate heredado sigue siendo un prerequisito adicional del frontend; no sustituye ni debilita la autorización específica del hijo o del API y su refactor global está fuera de NAV3.4K.

### Audiencia y fallbacks

Los grants canónicos de `Admin` y `ACH.Operator` ya incluyen `CanReadAch` y `CanManageAch`. NAV3.4K no les asigna `Returns.Read` ni `Returns.GenerateFile`; por tanto, ambos conservan el comportamiento existente y la audiencia sembrada no cambia.

Para un principal aislado por claim:

- `CanReadAch` autoriza `P0.ReturnsRead`/evaluación, pero no generación;
- `CanManageAch` autoriza `P0.ReturnsGenerateFile`, pero no `P0.ReturnsRead`;
- en la SPA, ambos siguen además sujetos al gate heredado del padre `/transactions`.

La migración futura debe decidir grants de `Returns.Read`/`Returns.GenerateFile`, validar la audiencia runtime y sólo después considerar retirar `CanReadAch`/`CanManageAch` como fallbacks.

### Relación con NAV3.4D

`/transactions/returns` permanece con entrada `CanReadAch` y generación `Returns.GenerateFile OR CanManageAch`. Su MenuItem, componente, controller y seeder de autorización no cambian. Se ajustan únicamente expectativas de regresión históricas que fijaban el antiguo guard de `returns-ror`, ahora propiedad de NAV3.4K; el producto de NAV3.4D permanece intacto.

`GetMenuForCurrentUserHandler` no cambia.

## NAV3.4L — Cargar transacciones desde archivo: frontera mutable de Bulk Ingestion

NAV3.4L normaliza únicamente `/transactions/bulk-ingestion/upload`. La caracterización confirma que no es una pantalla de lectura previa con una mutación opcional: desde el punto de vista de seguridad, toda utilidad remota de la superficie culmina en el upload mutable.

### Operación real

La UI sólo hace preparación local antes del request: selección de archivo, comprobación local de extensión/tamaño (20 MB), captura opcional de referencia e ID de cliente y resumen visual. No consume un preview GET ni una validación backend separada.

La única operación backend propia de esta pantalla es:

```text
POST api/transactions/bulk-ingestion/upload
  P1.BulkIngestionUpload
  BulkIngestion.Upload OR CanManageAch
```

El controller recibe multipart, valida presencia/tamaño y pasa el stream a `AchBulkFileIngestionService.UploadAndParseAsync`.

### Persistencia y procesamiento asíncrono

El servicio calcula hash SHA-256, aplica idempotencia por `ClientRequestId + FileHash`, parsea el formato, valida estructuralmente cada fila y persiste:

- `BulkIngestionBatch` con metadatos, hash, estado, contadores y timestamps;
- `BulkIngestionItem` con payload crudo serializado, payload normalizado, estado y mensajes;
- `BulkIngestionAttempt` inicial cuando existen ítems válidos y el batch pasa a `Queued`.

El binario original no se conserva como blob/archivo persistente en el flujo inspeccionado. Lo durable es el metadata/hash y las filas parseadas.

Cuando corresponde procesamiento, el flujo es:

```text
BulkIngestionWorkDispatcher
  -> IAchBulkJobScheduler
  -> AchBulkJobScheduler / Quartz StartNow
  -> ProcessBulkIngestionBatchJob
  -> AchBulkBatchProcessingService
  -> IAchBulkTransactionService.RegisterBulkAsync
```

El procesamiento puede persistir transacciones ACH y preparar los mecanismos internos existentes de ciclo CENIT/contrapartida. No se encontraron clientes SOAP, MFT, SFTP ni HTTP externo en el POST de upload ni en el dispatcher/scheduler/job/processor inspeccionado. Por tanto, **el upload no transmite directamente a terceros**; inicia procesamiento asíncrono interno que puede preparar trabajo downstream persistente.

### Capability canónica

`BulkIngestion.Upload` ya existía como capability fina en `FineGrainedPermissions`, `AllPermissions` y `P1.BulkIngestionUpload`; NAV3.4L no inventa una capability.

La policy efectiva permanece:

```text
P1.BulkIngestionUpload
  BulkIngestion.Upload OR CanManageAch
```

`CanReadAch` y `BulkIngestion.Read` no satisfacen la policy de upload. `BulkIngestion.Read` continúa perteneciendo exclusivamente a la lectura de tracking/detalle bajo NAV3.4E.

`BulkIngestionUploadPermissionSeeder` materializa `BulkIngestion.Upload` en el catálogo:

```text
e8194742-09ca-4148-9567-75bb24923f94
BulkIngestion.Upload
Cargar transacciones desde archivo
```

No crea roles, `RolePermission` ni grants de usuario.

### Autorización antes y después

| Capa | Antes de NAV3.4L | Final NAV3.4L |
|---|---|---|
| MenuItem | `(Admin OR ACH.Operator) AND CanManageAch` | `(Admin OR ACH.Operator) AND (BulkIngestion.Upload OR CanManageAch)` |
| SPA child | `CanManageAch` | `BulkIngestion.Upload OR CanManageAch` |
| SPA parent `/transactions` | `Admin OR ACH.Operator` **AND** `CanManageAch OR CanReadAch` | sin cambio |
| POST upload | `P1.BulkIngestionUpload` = `BulkIngestion.Upload OR CanManageAch` | sin cambio |

El MenuItem conserva sus `MenuItemRole` existentes. Esta decisión es deliberadamente fail-closed: el padre `/transactions` todavía impone el mismo dominio de roles y NAV3.4L no puede normalizarlo globalmente. Retirar sólo el role link del hijo podría representar una audiencia que la SPA no admite y, con el algoritmo actual de reconstrucción, permitir orphaning futuro si se otorga la capability fina sin acceso al padre. Los links de rol no son grants nuevos y no se modifican.

### Defensa SPA

El child route usa sólo `permissionGuard` con:

```text
BulkIngestion.Upload OR CanManageAch
```

No hay check de `Admin`/`ACH.Operator` en el hijo o componente. El role gate únicamente llega por el padre legacy existente.

La acción mutable está protegida en dos niveles adicionales:

1. el botón `Enviar archivo` sólo se muestra cuando `AuthService.hasPermission(['BulkIngestion.Upload', 'CanManageAch'])` es verdadero;
2. `upload()` llama a `ensureCanUpload()` y retorna antes del servicio si la capacidad falta.

Ocultar el botón no es la única frontera: el método falla cerrado y el backend repite la policy P1.

### Audiencia

No se agregan grants. En los seeds canónicos, Admin y ACH.Operator ya reciben `CanManageAch` y `CanReadAch`; ambos conservan el mismo acceso funcional mediante el fallback legacy. NAV3.4L no les otorga `BulkIngestion.Upload`.

Por capability aislada:

- `CanReadAch`: no puede ejecutar upload.
- `CanManageAch`: puede ejecutar el API upload; en SPA sigue sujeto al role gate del padre.
- `BulkIngestion.Read`: sólo lectura de tracking/detalle, no upload.
- `BulkIngestion.Upload`: puede ejecutar el API y satisface el child guard, pero la SPA completa sigue requiriendo el padre legacy/rol hasta una migración posterior.

### Regresiones preservadas

- Seguimiento de cargas y detalle contextual conservan `BulkIngestion.Read OR CanReadAch`.
- Retry conserva `BulkIngestion.Retry OR CanManageAch`.
- Cancel conserva `BulkIngestion.Cancel OR CanManageAch`.
- `/transactions/nacha-upload` conserva `Nacha.Upload OR CanManageAch` bajo NAV3.4C.
- `/transactions/bulk-create` permanece fuera del menú.
- `GetMenuForCurrentUserHandler` no cambia.

Existe un drift funcional previo, no de seguridad: la UI anuncia `.xls` mientras el resolver backend enumera `.xlsx/.xlsm`. NAV3.4L lo documenta y no lo corrige.

### Deuda posterior

El gap de autorización de upload queda cerrado a nivel de diseño/código sujeto a validación runtime. La migración futura debe decidir grants de `BulkIngestion.Upload`, validar la audiencia desplegada y sólo entonces retirar `CanManageAch` como fallback. La eliminación de la restricción heredada del padre `/transactions` y la eventual retirada de `MenuItemRole` requieren una decisión separada para no ampliar audiencia ni provocar hijos huérfanos.

### Validación NAV3.4L

La validación focal NAV3.4L pasa con **124 aserciones**. También pasan solution graph, Herramientas de prueba y las regresiones NAV3.4B–NAV3.4K (usando G2 para Registros del sistema). El parseo sintáctico focal de cuatro TypeScript modificados pasa con TypeScript 5.8.3. `.NET` queda `NOT_RUN_ENVIRONMENT` porque `dotnet` no está disponible; Angular queda `NOT_RUN_ENVIRONMENT` porque `node_modules` no está instalado. NAV2 continúa `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`.


## NAV3.4M — Configuración del intercambio de archivos (`/administracion/mft-ach`)

### Conclusión de seguridad

`/administracion/mft-ach` demuestra un **modelo B (superficie mixta)**. La pantalla permite consultar configuración MFT y horarios con una capacidad de lectura, mientras edición de configuración, reemplazo de credenciales, cambios de programación y prueba de conectividad requieren capacidad administrativa. No es una pantalla de transferencia de archivos: no inicia outbound/inbound, no envía archivos de prueba y no ofrece upload/download de contenido.

La frontera final queda así:

```text
Entrada / lectura
  P1.ConfigRead
  = Config.Read OR CanReadAch

Edición de configuración
Reemplazo de credencial
Creación/actualización de horarios
Prueba de conectividad
  P1.ConfigManage
  = Config.Manage OR CanManageAch
```

No se introduce una capability MFT nueva. `Config.Read` y `Config.Manage` ya existían en `FineGrainedPermissions`, `AllPermissions` y las policies P1; NAV3.4M únicamente las materializa idempotentemente en `Permission`, sin grants.

### Superficie funcional y clasificación de operaciones

| Operación UI | Endpoint | Clase | Persistencia | Secreto | Efecto externo |
|---|---|---|---|---|---|
| Cargar perfil MFT | `GET api/ach-colombia/file-exchange/administration` | lectura | puede crear la fila default si aún no existe | sólo metadata/estado | no |
| Cargar administración de horarios | `GET api/ach-colombia/operational-schedules/administration` | lectura | no modifica programación | no | no |
| Guardar perfil/conexión | `PUT api/ach-colombia/file-exchange/administration` | edición de configuración | sí | no cambia material secreto salvo metadata | no |
| Reemplazar password | `PUT api/ach-colombia/file-exchange/administration/credential` | administración de secretos | sí | recibe secreto write-only, lo cifra | no |
| Probar conexión | `POST api/ach-colombia/file-exchange/administration/test-connection` | prueba de conectividad | no cambia transferencia | usa secreto backend | sí: filesystem o sesión/listado SFTP |
| Crear horario | `POST api/ach-colombia/operational-schedules` | edición de configuración operativa | sí | no | resincroniza Quartz; no transfiere en el request |
| Actualizar horario | `PUT api/ach-colombia/operational-schedules/{id}` | edición de configuración operativa | sí | no | resincroniza Quartz; no transfiere en el request |

El endpoint `PATCH .../{id}/deactivate` no es consumido por esta pantalla. Los endpoints de ejecución, retry, reprocess, archive, retire, download u otras operaciones de transferencia del controller compartido tampoco son consumidos por `/administracion/mft-ach` y quedan fuera de NAV3.4M.

### Configuración y datos sensibles expuestos

El modelo persistido `AchManagedFileTransferConfiguration` contiene configuración de transporte y operación, incluyendo:

- proveedor/protocolo;
- `Endpoint`/hostname o IP configurable;
- puerto;
- `Principal`/usuario técnico;
- rutas outbound, inbound y archive;
- habilitación del perfil y flags automáticos/manuales;
- reintentos/retención;
- tipo de credencial y fecha de actualización;
- `ProtectedCredential`;
- fingerprint del host SFTP;
- timeout SFTP;
- token de concurrencia.

`AchManagedMftAdministrationDto`, que sí llega al navegador, expone la configuración operativa necesaria y metadata de credencial (`CredentialConfigured`, `CredentialType`, `CredentialUpdatedAtUtc`), pero **no contiene `ProtectedCredential`, password, token, private key ni secreto equivalente**. El fingerprint de host se expone como dato de verificación de identidad del servidor, no como material secreto.

La UI permite escribir un password nuevo. El secreto se envía únicamente a `PUT .../administration/credential`; `AchColombiaManagedMftCredentialComponent` ejecuta `encryption.Encrypt(value.Secret)` antes de persistir y responde mediante `MapAdministration`, que no proyecta el secreto. NAV3.4M no modifica cifrado, storage, key management, masking, rotación ni logging de credenciales.

### Prueba de conectividad y efectos externos

`AchColombiaManagedMftConnectionTester` tiene dos ramas:

```text
ManagedFolder
  Directory.Exists(...)
  Directory.EnumerateFileSystemEntries(...).Take(1)

SftpEndpoint
  decrypt ProtectedCredential in backend
  IAchColombiaSftpSessionFactory.Connect(configuration, password)
  session.List(path) para outbound/inbound/archive
```

La operación ocurre sincrónicamente dentro del request `test-connection`. No llama upload/download/move/delete y no envía NACHA-M. El password descifrado permanece en backend y se limpia de la variable local al finalizar el flujo existente.

Los cambios de horario sí llaman `IAchColombiaOperationalScheduleSynchronizer.SynchronizeOutboundAsync`. `SchedulerSyncService` localiza la tarea `AchColombiaManagedMftOutbound` y resincroniza su programación Quartz. Esto puede alterar **cuándo ocurrirán futuras ejecuciones automáticas**, pero el request de guardar horario no abre SFTP ni transfiere archivos por sí mismo.

### Capacidades existentes y decisión de reutilización

La inspección de `FineGrainedPermissions`, `AllPermissions` y policies encontró:

```text
Config.Read
Config.Manage
Integrations.Read
Integrations.ManageMappings
Integrations.Validate
Integrations.Publish
Integrations.Compare
CanReadAch
CanManageAch
```

Para esta pantalla, `Config.Read`/`Config.Manage` son la capacidad fina semánticamente adecuada ya aprobada y reutilizada por otras superficies de configuración. Las capacidades `Integrations.*` existentes tienen semántica específica de integración/mapeo/publicación y no se fuerzan sobre MFT. No existe una capability fina separada preexistente para password o `test-connection`; ambas operaciones continúan bajo gestión de configuración.

NAV3.4M materializa únicamente capacidades ya declaradas:

```text
Config.Read
  15d0e235-eb36-4f7d-a922-d935dc1af7e2

Config.Manage
  6e9d334b-d77a-4576-814f-8e1f987a421f
```

No hay `RolePermission` nuevos, no hay grants a Admin/ACH.Operator y no hay grants directos de usuario.

### API: antes y después

Antes de NAV3.4M, los endpoints consumidos usaban directamente legacy capabilities:

```text
GET administration                    CanReadAch
GET schedules/administration          CanReadAch
PUT administration                    CanManageAch
PUT administration/credential         CanManageAch
POST administration/test-connection   CanManageAch
POST schedules                         CanManageAch
PUT schedules/{id}                    CanManageAch
```

Después:

```text
GET administration                    P1.ConfigRead
GET schedules/administration          P1.ConfigRead
PUT administration                    P1.ConfigManage
PUT administration/credential         P1.ConfigManage
POST administration/test-connection   P1.ConfigManage
POST schedules                         P1.ConfigManage
PUT schedules/{id}                    P1.ConfigManage
```

Las policies P1 mantienen los fallbacks legacy, de modo que no se debilita la frontera ni se rompe la audiencia existente:

```text
P1.ConfigRead   = Config.Read OR CanReadAch
P1.ConfigManage = Config.Manage OR CanManageAch
```

Las policies de endpoints del mismo controller que no consume esta pantalla no se modifican.

### SPA: antes y después

Antes:

```text
/administracion/mft-ach
  roleGuard + permissionGuard
  roles: Admin OR ACH.Operator
  permission: CanReadAch
```

Después:

```text
/administracion/mft-ach
  permissionGuard
  permissions: Config.Read OR CanReadAch
```

La ruta MFT es top-level y no es hija Angular de `/integraciones`; por tanto, no hereda el guard de ese parent route.

Las operaciones mutables dentro del componente calculan:

```text
canManage = Config.Manage OR CanManageAch
```

Hay dos defensas SPA para las mutaciones: gating visual/disabled y comprobación dentro de `saveConnection()`, `testConnection()` y `saveSchedules()` antes de invocar servicios. No hay checks por nombre de rol.

Separación deliberada:

- `CanReadAch` o `Config.Read` solos: lectura/entrada, sin edición ni test.
- `CanManageAch` o `Config.Manage` solos: pueden satisfacer las APIs mutables, pero no la ruta/GET de lectura; gestión no sustituye lectura.

### Menú y blocker estructural de `Conexiones`

La identidad NAV2 no cambia:

```text
Administración de parámetros
└── Conexiones
    └── Configuración del intercambio de archivos
        /administracion/mft-ach
        order 40
```

Autorización directa anterior del MenuItem:

```text
MenuItemRole       Admin
MenuItemPermission CanReadAch
=> Admin AND CanReadAch
```

Autorización directa final:

```text
MenuItemRole       Admin              (sin cambio)
MenuItemPermission Config.Read
MenuItemPermission CanReadAch
=> Admin AND (Config.Read OR CanReadAch)
```

El role link no se retira en NAV3.4M por una dependencia estructural previa:

1. el parent `/integraciones` conserva `Admin AND CanManageUsers`;
2. `GetMenuForCurrentUserHandler` filtra cada item de forma independiente con `Role AND Permission` y, si un hijo es visible pero su padre no, lo promueve a root;
3. `NavigationService` Angular todavía reemplaza `/integraciones` por `Centro de integración CFA` y fija `children: []`, descartando los hijos canónicos de `Conexiones` cuando ese parent llega al cliente.

Con ese estado, quitar el role únicamente del hijo MFT permitiría una semántica de orphaning para futuros principals con `Config.Read`, sin resolver la representación real del árbol. NAV3.4M conserva fail-closed el role existente y **no modifica** `/integraciones`, `NavigationService` ni `GetMenuForCurrentUserHandler`, conforme al límite de alcance.

Esto implica un gap residual: el permiso directo del MenuItem ya refleja la capacidad canónica de lectura, pero el menú renderizado todavía no puede considerarse alineado con la SPA/API mientras exista la adaptación legacy del parent.

### Audiencia efectiva

No se agregan grants de `Config.Read` ni `Config.Manage`. Admin y ACH.Operator que ya operaban mediante `CanReadAch`/`CanManageAch` conservan acceso por fallback a las APIs y la ruta directa. Por ello la **audiencia sembrada no cambia**.

El MenuItem continúa más restrictivo que la ruta directa/API por el `Admin` role directo y el parent legacy. Esta diferencia es intencionalmente conservada como deuda estructural, no como modelo objetivo final.

### Regresión de las otras superficies `Conexiones`

NAV3.4M no cambia autorización ni comportamiento de:

```text
/integraciones/contrato
/integraciones/relacion-informacion
/integraciones/conexion
```

Tampoco modifica la arquitectura NAV2, los labels/orders/hierarchy canónicos, override CFA, comportamiento general de parents navegables, `GetMenuForCurrentUserHandler`, grants, TestTools ni los gaps NAV3.4A–L.

### Estado de cierre y siguiente incremento

La normalización focal de backend/SPA y la caracterización de secretos/efectos externos de MFT quedan implementadas. El gap completo `/administracion/mft-ach` **no se considera cerrado** por el blocker estructural del parent/renderizado de `Conexiones`.

El siguiente incremento recomendado es:

```text
NAV3.4N — Conexiones parent/navigation prerequisite
```

Debe limitarse a normalizar `/integraciones` y su adaptación en `NavigationService` para preservar la jerarquía NAV2, impedir orphaning y permitir renderizar sus hijos canónicos. No debe cambiar todavía la autorización propia de `Contrato del servicio`, `Relación de información` ni `Conexión entidad participante`.

### Validación NAV3.4M

La validación estática focal final pasa con **192 aserciones** y se ejecutan satisfactoriamente las regresiones NAV3.4B–NAV3.4L, solution graph y Herramientas de prueba. El parseo sintáctico focal de los tres TypeScript modificados pasa con TypeScript 5.8.3.

`.NET` queda **`NOT_RUN_ENVIRONMENT`** porque `dotnet` no está instalado. Angular queda **`NOT_RUN_ENVIRONMENT`** porque no existe `web/ach-interbank-ui/node_modules`; Node/NPM sí están presentes. No se declaran pruebas runtime no ejecutadas como exitosas.

### Bypass conocido / acceso directo

No existe evidencia de un bypass de backend: una capability de lectura no satisface `P1.ConfigManage` y no puede ejecutar las mutaciones ni `test-connection`; tampoco existe transmisión directa en esta pantalla.

Permanece un mismatch de **descubrimiento/navegación**: la ruta SPA directa y el GET aceptan `Config.Read OR CanReadAch`, mientras el MenuItem sigue además condicionado por `Admin` y por el parent legacy. Por ello un usuario legítimamente autorizado para lectura podría entrar por URL directa aunque no vea el menú. Es un gap de alineamiento del árbol, no una omisión de autorización API, y debe resolverse en el incremento estructural del parent.

## NAV3.4N — `Conexiones` como contenedor estructural y cierre del blocker MFT — 2026-10-15

### Problema previo de seguridad/navegación

El parent canónico del grupo NAV2 es el `MenuItem` histórico `ID 29`, reconciliado por la IA como:

```text
Label     Conexiones
Route     /integraciones
Parent    /administracion-parametros
Order     80
IsActive  true
```

Antes de NAV3.4N conservaba autorización legacy:

```text
MenuItemRole       Admin
MenuItemPermission CanManageUsers
=> Admin AND CanManageUsers
```

Esta frontera era incompatible con el hijo MFT ya normalizado en NAV3.4M. `GetMenuForCurrentUserHandler` filtra cada item de forma independiente: hace OR entre roles, OR entre permisos y luego exige `hasRole AND hasPermission`. Después arma el árbol sólo con los items visibles. Si un child visible referencia un `ParentId` que fue filtrado, el handler agrega ese child a `roots`. Por tanto, un principal con `Config.Read`/`CanReadAch` podía ser legítimo para MFT pero perder `/integraciones` y quedar promovido como raíz si se retiraba aislada y anticipadamente el role `Admin` del hijo.

El segundo blocker estaba en Angular. `NavigationService.normalizeIntegrationNavigation` reconocía `/integraciones` y rutas legacy de integración, sustituía el nodo por:

```text
label    Centro de integración CFA
route    /integraciones
exact    false
children []
```

y luego deduplicaba sobre `/integraciones`. El resultado era que el label backend `Conexiones` y todos los children autorizados entregados por la API se perdían en el cliente.

### Modelo estructural final del parent

El handler actual no puede derivar dinámicamente la visibilidad de un parent a partir de sus children. Dejar `/integraciones` sin roles ni permisos haría que el parent pasara siempre `ShouldInclude`, incluso vacío. A la vez, el modelo actual de un solo MenuItem tampoco puede expresar exactamente:

```text
(Admin AND CanManageUsers)
OR Config.Read
OR CanReadAch
```

sin modificar el algoritmo general, cambio fuera de alcance de NAV3.4N.

Se adopta el modelo focal mínimo compatible con las capacidades **ya existentes** de los cuatro children actuales:

```text
MenuItemRole       ninguno
MenuItemPermission CanManageUsers
                   OR Config.Read
                   OR CanReadAch
```

No se crea `Connections.Read` ni ninguna capability nueva; tampoco se agregan `RolePermission`, grants a roles, permisos directos a usuarios ni nuevos roles.

Con las reglas actuales de los children:

- `Admin + CanManageUsers` conserva `Conexiones` para los tres children legacy;
- `Config.Read` conserva `Conexiones` para MFT;
- `CanReadAch` conserva `Conexiones` para MFT;
- `CanManageAch` por sí solo no vuelve visible `Conexiones` ni ninguno de esos cuatro children;
- el parent nunca autoriza un child: cada child sigue siendo filtrado por sus propias asociaciones;
- un principal con acceso sólo a MFT no recibe `Contrato del servicio`, `Relación de información` ni `Conexión entidad participante`.

Existe una limitación residual del algoritmo general: un principal sintético que tuviera `CanManageUsers` pero no role `Admin` podría ver el parent vacío, porque el parent pasa por permiso pero los tres children legacy exigen además `Admin`. Los grants canónicos actuales no generan ese estado: `CanManageUsers` está asociado al rol Admin. Evitar ese caso de forma absoluta exigiría cambiar el algoritmo general para visibilidad derivada de children, lo cual queda fuera de NAV3.4N. Esta limitación no concede acceso funcional ni hace visibles siblings no autorizados.

### `NavigationService`: backend vuelve a ser la fuente del subárbol

NAV3.4N elimina exclusivamente el tratamiento especial de Integraciones:

- desaparecen `INTEGRATION_CENTER_ROUTE`, `OBSOLETE_INTEGRATION_NAV_ROUTES` y `normalizeIntegrationNavigation`;
- `getMenu()` mantiene sólo el flujo genérico existente `removeRetiredMenuItems -> sortMenu`;
- el label `Conexiones` proviene del backend/seed;
- los children provienen del backend y se conservan;
- el orden se toma de `order` backend;
- Angular no crea hijos, no sustituye el parent ni fuerza `children: []`.

No se modifican otros overrides, filtros de rutas retiradas, títulos, breadcrumbs ni `MainLayout`. El route SPA `/integraciones` sigue existiendo y conserva su guard preexistente `(Admin OR ACH.Operator) AND (CanReadAch OR CanManageAch)`. En el menú, `MainLayout` ya representa cualquier nodo con children como botón de expansión; NAV3.4N no cambia ese comportamiento.

### Cierre del MenuItem MFT pendiente de NAV3.4M

Una vez que el parent puede conservarse para la audiencia MFT y Angular deja de aplanar el subárbol, se elimina el role residual `Admin` del MenuItem `/administracion/mft-ach`.

Autorización final del MenuItem:

```text
MenuItemRole       ninguno
MenuItemPermission Config.Read OR CanReadAch
```

Esto coincide con la frontera de entrada SPA y con `P1.ConfigRead` ya establecidas en NAV3.4M. No se vuelven a tocar el route guard MFT, el componente, las acciones mutables ni los controllers: edición, credenciales, horarios y `test-connection` continúan bajo `Config.Manage OR CanManageAch` / `P1.ConfigManage`.

No se agregan grants. La audiencia backend no aumenta. La visibilidad de menú sí deja de excluir artificialmente a principals ya autorizados por lectura; en particular `ACH.Operator`, que ya recibe `CanReadAch` por el seed vigente, puede descubrir MFT sin adquirir ninguna capability nueva.

### Otros tres children y ausencia de sibling leakage

NAV3.4N no modifica la autorización propia de:

```text
/integraciones/contrato
/integraciones/relacion-informacion
/integraciones/conexion
```

Sus `MenuItemRole`, `MenuItemPermission`, guards SPA y policies API permanecen byte a byte iguales al ZIP NAV3.4M. La visibilidad del parent `Conexiones` no los hace visibles: el handler continúa evaluando cada child de forma independiente.

`GetMenuForCurrentUserHandler` tampoco se modifica. Para las cuatro políticas actuales, la unión de permisos del parent evita que un child autorizado quede huérfano/promovido a raíz. El comportamiento genérico de promoción sigue existiendo para un eventual futuro child cuya autorización no esté cubierta por la unión del parent; resolverlo globalmente requiere otro diseño y no se introduce en este incremento.

### Verificación y estado

Validación estática focal NAV3.4N: **PASS — 96 aserciones**. Regresión NAV3.4M adaptada únicamente a la eliminación del blocker: **PASS — 189 aserciones**. Solution graph, Herramientas de prueba y NAV3.4B–NAV3.4L vigentes pasan; el parseo sintáctico focal de `NavigationService` y su spec pasa con TypeScript 5.8.3.

`.NET` permanece **`NOT_RUN_ENVIRONMENT`** porque `dotnet` no está instalado. Angular permanece **`NOT_RUN_ENVIRONMENT`** porque `node_modules` no está disponible. Por ello NAV2 continúa exactamente en **`IMPLEMENTED_UNVERIFIED_ENVIRONMENT`** y no se eleva a `CLOSED`/verificado por runtime.

## NAV3.4O — autorización de `Contrato del servicio` — 2026-10-15

### Superficie y threat model

`/integraciones/contrato` no es una pantalla de conexión ni un ejecutor SOAP. Es un workspace mixto de versiones WSDL/XSD y metadata contractual. La lectura consulta catálogo/métodos, versiones, detalle y mapping sets asociados. La administración target permite editar sólo metadata funcional de parámetros, analizar un artefacto local, previsualizar su diff e importarlo como un nuevo `Draft`.

No se exponen en los DTOs de esta superficie passwords, tokens, client secrets, private keys, material de certificados ni secret references. Tampoco se muestra el endpoint/URL de la entidad participante: esa responsabilidad pertenece a `/integraciones/conexion`.

El analizador WSDL/XSD opera localmente con DTD prohibido y `XmlResolver = null`; rechaza WSDL imports y XSD import/include/redefine externos. Los endpoints consumidos por la pantalla no descargan WSDL, no abren HTTP/SOAP hacia terceros y no ejecutan una operación productiva externa. `preview` no persiste; `import` sólo persiste un `Draft`.

### Fronteras previas

En NAV3.4N la autorización observable era:

```text
MenuItem /integraciones/contrato
  Role:       Admin
  Permission: CanManageUsers
  => Admin AND CanManageUsers

SPA child /integraciones/contrato
  sin guard propio
  => hereda parent /integraciones:
     (Admin OR ACH.Operator) AND (CanReadAch OR CanManageAch)

API consumida
  GET                         => CanReadAch
  PUT/POST target management => CanManageAch
```

Esto no exponía escritura a un lector, pero presentaba drift semántico: el MenuItem dependía de una capability de administración de usuarios; la ruta hija permitía que `CanManageAch` reemplazara la lectura aunque el workspace necesita GET protegidos por `CanReadAch`; y la gestión contractual no tenía capability fina propia.

### Capability de gestión contractual

Las capabilities preexistentes de integración (`Integrations.Read`, `Integrations.ManageMappings`, `Integrations.Validate`, `Integrations.Publish`, `Integrations.Compare`) fueron evaluadas. Ninguna representa por sí sola la edición de metadata contractual y la importación de un nuevo Draft. Fragmentar el flujo entre Validate/Compare/Publish sería semánticamente incorrecto y `ManageMappings` pertenece a la superficie de mapeos.

NAV3.4O introduce una única capability dedicada:

```text
Integrations.ManageContracts
```

persistida idempotentemente sin grants por `IntegrationContractPermissionSeeder`, y una policy de transición:

```text
P1.IntegrationContractsManage
= Integrations.ManageContracts OR CanManageAch
```

El fallback `CanManageAch` mantiene la audiencia administrativa existente; la nueva capability no recibe `RolePermission` ni asignaciones directas a usuarios.

`Integrations.Read` no se adopta ni materializa en NAV3.4O. La página depende de dos GET compartidos (`api/integrations/methods` y `api/integrations/mappingsets`) ya protegidos por `CanReadAch`. Cambiar esos endpoints modificaría otras superficies y podría requerir reabrir el parent `Conexiones`, prohibido por el alcance. `CanReadAch` sigue siendo por tanto la frontera de lectura efectiva de Contrato en este incremento.

### Modelo final

El modelo final es B, con lectura y administración separadas.

**MenuItem**

```text
Role:       Admin
Permission: CanReadAch
=> Admin AND CanReadAch
```

El role Admin se preserva deliberadamente: el MenuItem previo era Admin-only y quitarlo abriría descubribilidad a ACH.Operator. NAV3.4O corrige el permiso incoherente `CanManageUsers` sin ampliar silenciosamente la audiencia del menú. El parent `Conexiones` ya acepta `CanReadAch` y permanece intacto, por lo que el child conserva su jerarquía.

**SPA**

El child declara explícitamente `permissionGuard` con `CanReadAch`. Como el parent `/integraciones` no se modifica, la frontera efectiva es:

```text
(Admin OR ACH.Operator) AND CanReadAch
```

El store define gestión como:

```text
Integrations.ManageContracts OR CanManageAch
```

Las mutaciones contractuales tienen gating visual y comprobación fail-closed antes de invocar el servicio. El import stepper valida `canManage` dentro de `analyzeSelectedFile`, `validateOperation`, `previewDiff` e `importDraft`, además del estado disabled de sus controles.

**API consumida**

```text
GET methods                              CanReadAch
GET methods/{id}/contracts               CanReadAch
GET mappingsets?methodId=...             CanReadAch
GET contracts/{id}                       CanReadAch
PUT .../parameters/{id}/presentation     P1.IntegrationContractsManage
POST contracts/analyze                   P1.IntegrationContractsManage
POST .../contracts/preview/{baseId}      P1.IntegrationContractsManage
POST .../contracts/import                P1.IntegrationContractsManage
```

Create/clone/activate/rollback/compare contractuales existen en el controller/client compartido pero no son operaciones de esta pantalla; sus policies quedan sin cambios en NAV3.4O.

### Propiedades de seguridad resultantes

- `CanReadAch` no satisface `P1.IntegrationContractsManage`; un lector no puede editar/importar/analizar/preview por API.
- `CanManageAch` puede satisfacer el fallback de gestión, pero por sí solo ya no satisface el guard child ni los GET de lectura requeridos por el workspace.
- `Integrations.ManageContracts` por sí sola habilita la policy de gestión backend, pero no sustituye `CanReadAch`; se mantiene separación explícita lectura/gestión.
- La capability fina no concede acceso a `/integraciones/relacion-informacion`, `/integraciones/conexion` ni MFT.
- No se crean roles ni grants.
- No se altera cifrado, secretos, transporte SOAP, payloads, retry/timeout ni lifecycle productivo.

La audiencia sembrada de Admin/ACH.Operator para SPA/API no cambia porque ambos ya poseen `CanReadAch` y `CanManageAch`. La audiencia sembrada del MenuItem tampoco cambia: Admin ya posee `CanReadAch`; ACH.Operator continúa sin verlo. Persiste por diseño una diferencia de descubribilidad entre MenuItem Admin-only y acceso directo SPA/API de ACH.Operator. No es un bypass de autorización backend y NAV3.4O no lo resuelve ampliando audiencia sin decisión de grants.

### Invariantes de regresión

NAV3.4O conserva byte a byte respecto de NAV3.4N:

- `ConnectionsStructuralMenuAuthorizationSeeder` y permisos del parent `Conexiones`;
- `NavigationService` sin el override eliminado en NAV3.4N;
- `GetMenuForCurrentUserHandler`;
- guard padre `/integraciones` en `app-routing.module.ts`;
- MFT (MenuItem/SPA/API funcionales);
- autorización propia de `Relación de información`;
- autorización propia de `Conexión entidad participante`.

Por tanto no se reabre el diseño estructural NAV3.4N y la visibilidad del parent no concede acceso a siblings.

### Verificación

La validación estática NAV3.4O pasa con 210 aserciones, más solution graph, Herramientas de prueba y regresiones NAV3.4B–NAV3.4L vigentes. El parseo sintáctico focal TypeScript pasa con TypeScript 5.8.3.

`.NET` permanece `NOT_RUN_ENVIRONMENT` porque `dotnet` no está instalado. Angular permanece `NOT_RUN_ENVIRONMENT` porque no existe `node_modules`. Por ello la normalización queda `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`; no se declara cierre runtime ni se cambia el estado global NAV2.

## NAV3.4P — `Relación de información`: separación read / mapping / validate / publish — 2026-10-15

### Threat model de la superficie

`/integraciones/relacion-informacion` administra reglas que construyen los datos de futuras integraciones. No ejecuta una transmisión externa en el request, pero publicar un mapping altera el mapping `Published && IsActive` que el runtime usa después; por ello la mutación es administración sensible y no puede quedar disponible con una capacidad de lectura.

La superficie real es mixta:

- **lectura**: métodos, parámetros contractuales, catálogo fuente, transformaciones, mapping sets, workspace, estado de cobertura e historial;
- **gestión**: selección de origen/tratamiento, edición de reglas, defaults/constantes, máscara, prioridad, obligatoriedad, enabled y clonación;
- **validación/prueba**: validación persistente y preview sanitizado;
- **publicación**: cambio de estado/versionado del mapping que será usado por ejecuciones futuras.

No existe DELETE ni una invocación SOAP/HTTP/SFTP directa desde estos flujos.

### Fronteras anteriores

```text
MenuItem
  Admin AND CanManageUsers

SPA child
  sin guard propio
  => (Admin OR ACH.Operator) AND (CanReadAch OR CanManageAch) por parent

API
  GET        CanReadAch
  write/test/publish  CanManageAch
```

El backend ya impedía escritura con `CanReadAch`, pero no expresaba la granularidad fina declarada en `FineGrainedPermissions`.

### Capacidades canónicas reutilizadas

El diseño preexistente ya define y NAV3.4P confirma por comportamiento real:

```text
Integrations.ManageMappings  gestión de mapping sets/reglas
Integrations.Validate        validación y preview/test de transformaciones
Integrations.Publish         publicación de configuración
```

Se materializan idempotentemente en `Permission` sin ningún grant. No se crea una capability conceptual nueva.

La lectura continúa temporalmente en `CanReadAch`. `Integrations.Read` existe semánticamente, pero los GET necesarios son compartidos con otras superficies y cambiarla en este incremento violaría el aislamiento de NAV3.4O/NAV3.4N.

Policies de transición:

```text
P1.IntegrationMappingsManage   = Integrations.ManageMappings OR CanManageAch
P1.IntegrationMappingsValidate = Integrations.Validate OR CanManageAch
P1.IntegrationMappingsPublish  = Integrations.Publish OR CanManageAch
```

Ninguna de esas policies acepta `CanReadAch`.

### Menú y ruta

El MenuItem final permanece bajo `Conexiones`, order 20, con:

```text
Role       Admin
Permission CanReadAch
```

El role histórico se conserva exclusivamente para no ampliar de forma implícita la audiencia visible del menú. El permiso `CanManageUsers` deja de participar porque no representa lectura de mappings.

Las rutas SPA lista/editor declaran explícitamente `CanReadAch`. Las acciones no usan role names: gestión, validación/preview y publicación se gatean por sus capacidades P1 equivalentes (fine capability OR fallback `CanManageAch`) y vuelven a comprobarse dentro del método antes del service call. El store de la vista principal no llama automáticamente a preview para un principal sin `Integrations.Validate`/`CanManageAch`.

### API final

```text
Read:
  GET mappingsets / published / {id} / workspace / history -> CanReadAch
  GET shared method/parameter/source/transform catalog       -> CanReadAch

Manage mappings:
  POST mappingsets
  PUT mappingsets/{id}
  PUT mappingsets/{id}/rules
  PUT mappingsets/{id}/parameters/{parameterId}
  POST mappingsets/{id}/clone
  -> P1.IntegrationMappingsManage

Validate/test:
  POST mappingsets/{id}/validate
  POST mappingsets/{id}/preview
  -> P1.IntegrationMappingsValidate

Publish:
  POST mappingsets/{id}/publish
  -> P1.IntegrationMappingsPublish
```

`clone-to-contract-version`, `validate-contract-mapping` y `compare` no forman parte del flujo target y conservan `CanManageAch` en este incremento.

### Datos sensibles y efectos

`IntegrationMappingRuleDto` contiene valores de configuración como `FixedValue` y `DefaultValue`; no contiene un campo dedicado de password/token/private key/secret reference. Esos valores de configuración no están tratados como secret store y se devuelven a lectores autorizados, por lo que no deben utilizarse para credenciales. NAV3.4P no modifica ni amplía esa exposición existente.

Preview puede resolver datos desde una transacción persistida. Antes de devolverlos, backend aplica sanitización por rutas sensibles y marcadores de secretos y marca `IsMasked`; la SPA recibe preview/payload JSON sanitizados. La validación persiste únicamente el resumen de validación. Publicar archiva el mapping publicado anterior, activa el nuevo y registra actor/fecha/historia.

No se encontró transporte externo directo en mapping service, validation service ni preview service. El efecto productivo es diferido: los resolvers de ejecución posteriores seleccionan el mapping publicado/activo.

### Invariantes de aislamiento

NAV3.4P no modifica el parent `Conexiones`, `NavigationService`, `GetMenuForCurrentUserHandler`, el guard parent `/integraciones`, MFT, `Contrato del servicio` ni `Conexión entidad participante`. No crea roles ni grants.

Estado: `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`; NAV2 permanece en el mismo estado hasta disponer de runtime .NET.

### Verificación NAV3.4P

El validador focal NAV3.4P pasa 190 aserciones y NAV3.4O actualizado para el nuevo snapshot de mapping pasa 210. También pasan solution graph, Herramientas de prueba y las regresiones estáticas NAV3.4B–NAV3.4L ejecutadas. El parseo sintáctico focal de 7 archivos TypeScript pasa con TypeScript 5.8.3.

`.NET = NOT_RUN_ENVIRONMENT` por ausencia de `dotnet`. Angular = `NOT_RUN_ENVIRONMENT` por ausencia de `node_modules`. El estado de NAV2 permanece `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`.

## NAV3.4Q — `Conexión entidad participante`: lectura + administración de conexión sin invocación directa — 2026-10-15

### Threat model y clasificación

`/integraciones/conexion` administra la configuración técnica que los clientes SOAP runtime utilizarán posteriormente para comunicarse con una entidad participante. La pantalla **no ejecuta SOAP por sí misma**, pero puede cambiar endpoint, SOAP Action y el estado Live/enabled que condicionan ejecuciones productivas futuras; por ello la modificación de la conexión es una capacidad administrativa sensible y debe permanecer separada de la lectura.

Modelo confirmado: **B — superficie mixta de lectura + administración**. No se adopta modelo C porque no existe en esta UI/controller un endpoint de `test connection`, `test authentication`, invocación SOAP ni envío de payload de prueba. `preview-change` es un preflight de configuración: valida revision/estado y devuelve destino/acción sanitizados, sin abrir red ni ejecutar transporte.

### Operaciones y endpoints target

```text
GET  api/users/soap-integrations
  consulta configuración proyectada/sanitizada

POST api/users/soap-integrations/preview-change
  previsualiza un cambio de configuración antes de habilitar envío real
  no persiste
  no conecta ni invoca SOAP

PUT  api/users/soap-integrations
  persiste configuración SOAP
  no conecta ni invoca SOAP
```

La UI permite seleccionar método, consultar estado y editar endpoint, SOAP Action, `OperatingMode` y `Enabled`. No expone una operación de credential update, DELETE, test, invoke o publish. El timeout existe en backend y es preservado al guardar; no forma parte del editor target.

### Secretos y datos sensibles

Los valores raw de endpoint/SOAP Action se persisten backend-side. La representación administrativa enviada al navegador es sanitizada:

- endpoint se transforma a una representación enmascarada;
- SOAP Action se sanitiza y no expone userinfo/query/fragment;
- los valores sanitizados se reconocen como placeholders al guardar, por lo que no sobrescriben el raw persistido;
- el navegador recibe estado/mode/enabled y metadata de input mappings.

No existen campos target dedicados de password, token, API key, client secret, private key, `SecretRef`, encrypted credential ni certificado. Tampoco existe endpoint target para leer o reemplazar credenciales. `DefaultValue` de un input mapping es metadata de configuración existente y no debe usarse como secret store.

NAV3.4Q no modifica cifrado, key vault, almacenamiento de secretos, rotación, masking ni logging.

### Fronteras anteriores

```text
MenuItem
  Admin AND CanManageUsers

SPA child
  sin guard propio
  => heredaba (Admin OR ACH.Operator) AND (CanReadAch OR CanManageAch)

component
  read   Config.Read OR CanReadAch OR manage
  manage Config.Manage OR CanManageAch

API
  GET            P1.ConfigRead   = Config.Read OR CanReadAch
  preview-change P1.ConfigManage = Config.Manage OR CanManageAch
  PUT            P1.ConfigManage = Config.Manage OR CanManageAch
```

El backend ya separaba lectura de mutación; el drift era semántico: `Config.*` pertenece al dominio general/MFT y no expresa administración de conexión SOAP de participante.

### Capability canónica de gestión

No había una capability fina con semántica suficiente. Se crea y materializa idempotentemente, sin grants:

```text
Integrations.ManageConnections
289e92ea-abd4-43a9-b93a-23b06fb03362
```

Policy de transición:

```text
P1.IntegrationConnectionsManage
  = Integrations.ManageConnections OR CanManageAch
```

`CanManageAch` conserva compatibilidad con la audiencia legacy existente. `Config.Manage`, `Integrations.ManageContracts`, `Integrations.ManageMappings`, `Integrations.Validate` e `Integrations.Publish` no se reutilizan artificialmente.

No se crea capability de test/invocación porque la superficie no ejecuta una operación de ese tipo. La previsualización del cambio pertenece a la misma frontera de administración de conexión.

### Lectura canónica

La lectura target queda en:

```text
CanReadAch
```

El GET deja `P1.ConfigRead`. Esto retira el acceso sintético `Config.Read`-only en vez de ampliar audiencia y evita seguir acoplando esta superficie al dominio MFT/configuración.

`Integrations.Read` existe, pero no se adopta de forma aislada: Contrato/Relación comparten lecturas bajo `CanReadAch` y el parent `Conexiones` actual no contempla una audiencia `Integrations.Read`-only. Adoptarla únicamente aquí podría volver a introducir orphaning o exigir reabrir NAV3.4N/O/P. La decisión transversal queda diferida.

### Menú y SPA final

MenuItem final:

```text
Role       Admin
Permission CanReadAch
=> Admin AND CanReadAch
```

El role `Admin` se conserva para mantener la audiencia visible previa y evitar una ampliación silenciosa hacia ACH.Operator. La visibilidad del parent `Conexiones` no se modifica.

Child route final:

```text
permissionGuard -> CanReadAch
```

Con el parent heredado intacto:

```text
(Admin OR ACH.Operator) AND CanReadAch
```

Las acciones SPA quedan:

```text
read              CanReadAch
edit/preview/save Integrations.ManageConnections OR CanManageAch
```

El HTML oculta/deshabilita edición según `canManage` y los métodos vuelven a fallar cerrados antes de llamar al servicio. No se usan nombres de rol dentro de las acciones.

### API final

```text
GET  api/users/soap-integrations
  CanReadAch

POST api/users/soap-integrations/preview-change
  P1.IntegrationConnectionsManage

PUT  api/users/soap-integrations
  P1.IntegrationConnectionsManage
```

`CanReadAch` no satisface la policy de gestión. `Integrations.ManageConnections` no satisface el GET ni el guard de entrada; lectura y administración permanecen independientes.

### Efecto productivo diferido y gates runtime

Los endpoints de administración no usan `HttpClient`, no crean `HttpRequestMessage` y no invocan SOAP. El riesgo productivo es diferido: el valor guardado se consume después por clientes runtime.

`WscfaachSoapClient` protege el envío real mediante:

- mapping existente;
- `Enabled=true`;
- `OperatingMode=Live`;
- endpoint y SOAP Action presentes;
- timeout válido;
- endpoint absoluto sin userinfo/fragment;
- resolución de transporte local controlado cuando está configurada;
- retry deshabilitado.

`WsAxonRespuestaTransaccionesSoapClient` añade una frontera de destino explícita:

```text
Unconfigured       -> fail closed
ControlledLocal    -> sólo destino local autorizado
ConfiguredAllowlist -> esquemas/hosts/paths/puertos explícitos; HTTPS opcionalmente obligatorio
```

Los defaults generales `ProcContrapartidas.Mode`/`ProcTransacciones.Mode` permanecen DryRun y Development conserva sus valores locales controlados. NAV3.4Q no elimina, relaja ni reconfigura esos gates.

La configuración `Live + Enabled` puede permitir que **otros flujos** ejecuten posteriormente `Proc_Transacciones`, `Proc_Contrapartidas` o `RegistrarRespuestaTransaccion`. Los dos primeros participan en procesamiento ACH y pueden producir efecto productivo/monetario downstream. Guardar o previsualizar desde esta UI no realiza esa operación.

Riesgo residual fuera de alcance: el cliente WSCFAACH no tiene una policy `ConfiguredAllowlist` equivalente a la de WSAXON en el código inspeccionado. No se corrige en NAV3.4Q porque sería un rediseño de seguridad de transporte, no una normalización de autorización de menú/SPA/API.

### Audiencia y grants

No se crean roles ni `RolePermission`; Admin/ACH.Operator no reciben `Integrations.ManageConnections` y no hay grants directos a usuarios.

La audiencia sembrada permanece estable porque los roles existentes ya tienen `CanReadAch`/`CanManageAch`. La nueva capability permite una migración futura, pero no otorga acceso por sí sola hasta que exista una decisión explícita de grants.

Diferencias fail-closed deliberadas:

- `Config.Read`-only pierde el GET target;
- `Config.Manage`-only pierde preview/PUT;
- `CanManageAch`-only conserva la gestión backend por fallback, pero no sustituye la lectura SPA;
- `Integrations.ManageConnections`-only puede satisfacer gestión backend si se asigna en el futuro, pero no lectura/entrada.

ACH.Operator con `CanReadAch` puede continuar usando la ruta directa aunque el MenuItem sea Admin-only. Es una diferencia de descubribilidad, no un bypass de API.

### Invariantes de aislamiento

NAV3.4Q no modifica:

- `Conexiones` ni sus permisos/roles;
- `NavigationService`;
- `GetMenuForCurrentUserHandler`;
- guard parent `/integraciones`;
- Contrato del servicio / NAV3.4O;
- Relación de información / NAV3.4P;
- MFT / NAV3.4M/N;
- clientes SOAP/runtime o la persistencia funcional de `SoapIntegrationSettingsService`.

Estado: `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`. NAV2 permanece `IMPLEMENTED_UNVERIFIED_ENVIRONMENT` hasta que puedan ejecutarse las pruebas runtime .NET pendientes.

### Verificación NAV3.4Q

El validador focal NAV3.4Q pasa **177 aserciones** y comprueba estructura, policy, materialización sin grants, MenuItem, guard SPA, separación API, sanitización/no-secret-fields, ausencia de transporte directo y gates de los clientes downstream. También pasan solution graph, Herramientas de prueba y las regresiones vigentes NAV3.4B–F, G2–P. El parseo sintáctico focal pasa en 4 archivos con TypeScript 5.8.3.

`.NET = NOT_RUN_ENVIRONMENT` porque `dotnet` no está instalado. Angular build/test/runtime = `NOT_RUN_ENVIRONMENT` porque `web/ach-interbank-ui/node_modules` no existe; Node/NPM/TypeScript sí están disponibles. El parseo sintáctico no sustituye runtime Angular.

El ZIP FULL final fue revalidado desde una extracción limpia; documentación, código y validadores empaquetados corresponden al mismo estado NAV3.4Q.

## NAV3.4R — lectura semántica y audiencia transicional de `Conexiones`

NAV3.4R normaliza de forma coordinada la lectura de las tres superficies `/integraciones/*` sin alterar sus capacidades mutables. `Integrations.Read` ya existía en el catálogo de capabilities; se materializa en `Permission` sin grants y se incorpora mediante una policy de transición:

```text
P1.IntegrationsRead
  = Integrations.Read OR CanReadAch
```

`CanReadAch` sigue siendo fallback. La policy no acepta `CanManageAch`, `Config.Read`, `Config.Manage` ni ninguna capability mutable de Integraciones.

Los 13 GET de `IntegrationCatalogController`, `IntegrationContractsController`, `IntegrationMappingSetsController` y `SoapIntegrationSettingsController` usan ahora `P1.IntegrationsRead`. La compatibilidad de audiencia se conserva porque `CanReadAch` sigue satisfaciendo la policy. MFT no usa esta policy y continúa en `P1.ConfigRead/P1.ConfigManage`.

Las fronteras mutables permanecen:

```text
Contrato
  Integrations.ManageContracts OR CanManageAch

Mappings
  Integrations.ManageMappings OR CanManageAch
  Integrations.Validate OR CanManageAch
  Integrations.Publish OR CanManageAch

Conexión
  Integrations.ManageConnections OR CanManageAch
```

Lectura no implica gestión y gestión no sustituye la lectura necesaria para cargar las pantallas.

### Audiencia de MenuItem

Los tres children quedan en:

```text
Roles       Admin OR ACH.Operator
Permissions Integrations.Read OR CanReadAch

=> (Admin OR ACH.Operator)
   AND
   (Integrations.Read OR CanReadAch)
```

La adición de `ACH.Operator` al menú no concede una capacidad funcional nueva en el estado sembrado: el role ya atravesaba el parent SPA `/integraciones`, ya tenía `CanReadAch` y ya podía usar ruta/API directa. NAV3.4R elimina una diferencia de descubribilidad.

El parent `Conexiones` sigue siendo un contenedor sin role gate y queda:

```text
Integrations.Read OR Config.Read OR CanReadAch
```

`CanManageUsers` se retira por quedar obsoleto para todos sus children. `Config.Read` preserva MFT y `Integrations.Read` preserva las tres superficies de Integraciones. La visibilidad del parent no concede ningún child; el handler continúa evaluando cada MenuItem independientemente.

### SPA transicional

Los child guards de Contrato, Relación (lista/editor) y Conexión usan:

```text
Integrations.Read OR CanReadAch
```

El parent SPA conserva role gate y compatibilidad legacy:

```text
(Admin OR ACH.Operator)
AND
(Integrations.Read OR CanReadAch OR CanManageAch)
```

NAV3.4R no convierte todavía el parent en capability-only. Un claim `Integrations.Read` sin Admin/ACH.Operator puede autorizar GETs API, pero no atraviesa el parent SPA ni ve los children role-scoped en menú. Como no existen grants de `Integrations.Read`, la población sembrada no se amplía.

Por la combinación deliberada de parent MenuItem role-free + children role-scoped, un principal sintético sin role y con `Integrations.Read` puede ver `Conexiones` vacío. Esto no concede siblings ni APIs mutables. Su eliminación correcta pertenece a la migración posterior: definir grants/población, validar runtime y decidir si se retiran role gates o se cambia genéricamente la derivación de visibilidad del parent. `GetMenuForCurrentUserHandler` no se modifica en NAV3.4R.

### Grants y retiro de fallbacks

NAV3.4R no otorga `Integrations.Read` ni ninguna capability fina a Admin, ACH.Operator o usuarios. Tampoco retira `CanReadAch`/`CanManageAch`. El orden de migración seguro queda explícito:

1. aprobar población objetivo de cada capability;
2. agregar grants explícitos en incremento separado;
3. validar menú, SPA y API en runtime;
4. retirar fallbacks legacy sólo cuando la audiencia equivalente esté demostrada;
5. retirar role gates transicionales únicamente después de que la capability sea suficiente para representar la población aprobada.

Hasta entonces NAV2 y NAV3 permanecen `IMPLEMENTED_UNVERIFIED_ENVIRONMENT` respecto de la verificación runtime pendiente.

## NAV3.5A — Fine-grained grant backfill

NAV3.5A parte únicamente de `ACHInterbank-R34-NAV-IA-NAV3.4R-FULL-PROJECT.zip` y es un **backfill de grants**, no una retirada de compatibilidad. Las policies P0/P1, los `MenuItemRole`, `MenuItemPermission`, guards SPA, `NavigationService`, `MainLayout` y `GetMenuForCurrentUserHandler` permanecen intactos. La forma de autorización sigue siendo, cuando aplica:

```text
FinePermission OR LegacyPermission
```

La única diferencia es que los roles canónicamente sembrados que ya tenían la misma capacidad funcional por el fallback legacy reciben ahora la capability fina equivalente.

### Población canónica y equivalencia legacy comprobada

El seed canónico define exactamente dos roles:

| Rol | ID estable | Fallbacks legacy relevantes |
|---|---|---|
| `Admin` | `1f8602da-6415-43f8-b61d-cb396f8577f1` | `CanManageAch`, `CanReadAch`, `CanManageUsers`, `CanManageCertificates` |
| `ACH.Operator` | `a51746c2-0710-4d79-97b1-5b4368326f56` | `CanManageAch`, `CanReadAch` |

`ACH.Operator` **no** tiene `CanManageUsers` ni `CanManageCertificates`. No se encontraron otros roles canónicamente sembrados ni seeders adicionales que creen roles. Los seeders adicionales de `RolePermission` existentes corresponden a permisos fuera del inventario target y al grant ya aprobado `Admin -> TestTools.Access`.

Esta equivalencia sólo se garantiza para el **estado sembrado del proyecto**. Bases desplegadas con roles personalizados, cambios manuales, grants runtime o poblaciones externas no representadas en el ZIP pueden divergir y deberán inventariarse antes de retirar fallbacks.

### Inventario y matriz de equivalencia

Se evaluaron exactamente **28 capabilities**: las capabilities finas creadas/materializadas o adoptadas por policies de transición dentro de NAV3. No se incorporan automáticamente permisos existentes ajenos a esa migración. En particular, `Nacha.Generate` y `Certificates.Activate/Validate/Audit` no forman parte del backfill NAV3.5A porque no fueron materializados/adoptados como targets de esta normalización NAV3.

En la columna “Gates adicionales” se documenta discoverability/SPA por separado de la capacidad backend. Un gate de navegación más restrictivo no invalida la equivalencia de un grant backend si el rol ya ejecutaba legítimamente la operación por fallback.

| Capability | Policy | Legacy fallback | Roles legacy equivalentes | Gates adicionales de menú/SPA relevantes | Audiencia efectiva antes | Fine grant | Equiv. | Decisión | Motivo |
|---|---|---|---|---|---|---|---|---|---|
| `TestTools.Access` | `TestTools.Access` exacta | ninguno | `Admin` por decisión NAV3.2A | capability-only en menú/SPA/API; sin role gate Admin/Operator | Admin | `Admin` ya existente | SÍ | GRANT existente | Decisión explícita previa; `ACH.Operator` permanece excluido. |
| `Transactions.Create` | `P0.TransactionsCreate` | `CanManageAch` | Admin, ACH.Operator | parent `/transactions`: Admin/Operator + legacy; child exige fine OR fallback | Admin, ACH.Operator | Admin, ACH.Operator | SÍ | GRANT | Ambos ya creaban por `CanManageAch`. |
| `Nacha.Upload` | `P1.NachaUpload` | `CanManageAch` | Admin, ACH.Operator | parent `/transactions` legacy; child fine OR fallback | Admin, ACH.Operator | Admin, ACH.Operator | SÍ | GRANT | Ambos ya podían ejecutar upload por `CanManageAch`. |
| `Returns.Read` | `P0.ReturnsRead` | `CanReadAch` | Admin, ACH.Operator | parent `/transactions` legacy; pantalla conserva lecturas auxiliares legacy | Admin, ACH.Operator | Admin, ACH.Operator | SÍ | GRANT | Ambos ya leían devoluciones por `CanReadAch`. |
| `Returns.GenerateFile` | `P0.ReturnsGenerateFile` | `CanManageAch` | Admin, ACH.Operator | parent `/transactions` legacy; acción mutable fine OR fallback | Admin, ACH.Operator | Admin, ACH.Operator | SÍ | GRANT | Ambos ya generaban por `CanManageAch`. |
| `BulkIngestion.Read` | `P1.BulkIngestionRead` | `CanReadAch` | Admin, ACH.Operator | parent `/transactions` legacy; tracking/detail fine OR fallback | Admin, ACH.Operator | Admin, ACH.Operator | SÍ | GRANT | Ambos ya consultaban lotes por `CanReadAch`. |
| `BulkIngestion.Upload` | `P1.BulkIngestionUpload` | `CanManageAch` | Admin, ACH.Operator | parent `/transactions` legacy; upload child fine OR fallback | Admin, ACH.Operator | Admin, ACH.Operator | SÍ | GRANT | Ambos ya cargaban por `CanManageAch`. |
| `BulkIngestion.Retry` | `P1.BulkIngestionRetry` | `CanManageAch` | Admin, ACH.Operator | superficie tracking requiere lectura; acción retry fine OR fallback | Admin, ACH.Operator | Admin, ACH.Operator | SÍ | GRANT | Ambos ya reintentaban por `CanManageAch`. |
| `BulkIngestion.Cancel` | `P1.BulkIngestionCancel` | `CanManageAch` | Admin, ACH.Operator | superficie tracking requiere lectura; acción cancel fine OR fallback | Admin, ACH.Operator | Admin, ACH.Operator | SÍ | GRANT | Ambos ya cancelaban por `CanManageAch`. |
| `Certificates.Read` | `P1.CertificatesRead` | `CanManageCertificates OR CanReadAch` | Admin, ACH.Operator | menú/SPA aceptan la misma unión; lecturas auxiliares pueden seguir usando `CanReadAch` | Admin, ACH.Operator | Admin, ACH.Operator | SÍ | GRANT | Admin entra por ambos fallbacks; Operator ya entra por `CanReadAch`. |
| `Certificates.UploadPublic` | `P1.CertificatesUploadPublic` | `CanManageCertificates` | Admin | wizard SPA además necesita `CanReadAch` para selectores compartidos | Admin | Admin | SÍ | GRANT | Sólo Admin posee el fallback de certificados. |
| `Certificates.RegisterPrivate` | `P1.CertificatesRegisterPrivate` | `CanManageCertificates` | Admin | wizard SPA además necesita `CanReadAch` para selectores compartidos | Admin | Admin | SÍ | GRANT | Sólo Admin posee el fallback de certificados. |
| `Certificates.Revoke` | `P1.CertificatesRevoke` | `CanManageCertificates` | Admin | acción SPA fine OR fallback | Admin | Admin | SÍ | GRANT | Sólo Admin posee el fallback de certificados. |
| `DigitalEnvelope.Encrypt` | `P1.DigitalEnvelopeEncrypt` | `CanManageAch` | Admin, ACH.Operator | entrada SPA sigue requiriendo `CanReadAch`; acción crypto fine OR fallback | Admin, ACH.Operator | Admin, ACH.Operator | SÍ | GRANT | Ambos ya cifraban por `CanManageAch` y poseen la lectura de entrada. |
| `DigitalEnvelope.Decrypt` | `P1.DigitalEnvelopeDecrypt` | `CanManageAch` | Admin, ACH.Operator | entrada SPA sigue requiriendo `CanReadAch`; acción crypto fine OR fallback | Admin, ACH.Operator | Admin, ACH.Operator | SÍ | GRANT | Ambos ya descifraban por `CanManageAch` y poseen la lectura de entrada. |
| `AuditLogs.Read` | `P1.AuditLogsRead` | `CanManageUsers` | Admin | padre `/logs` es unión de permisos; child fine OR fallback | Admin | Admin | SÍ | GRANT | Sólo Admin posee `CanManageUsers`. |
| `AuthLogs.Read` | `P1.AuthLogsRead` | `CanManageUsers` | Admin | padre `/logs` es unión de permisos; child fine OR fallback | Admin | Admin | SÍ | GRANT | Sólo Admin posee `CanManageUsers`. |
| `NavigationLogs.Read` | `P1.NavigationLogsRead` | `CanReadAch` | Admin, ACH.Operator | padre `/logs` es unión de permisos; child fine OR fallback | Admin, ACH.Operator | Admin, ACH.Operator | SÍ | GRANT | Ambos ya consultaban por `CanReadAch`. |
| `NavigationMenu.Manage` | `P1.NavigationMenuManage` | `CanManageUsers` | Admin | MenuItem/SPA/API ya fine OR fallback; sin role gate Admin obligatorio | Admin | Admin | SÍ | GRANT | Sólo Admin posee `CanManageUsers`; editor no concede roles/permisos. |
| `CanViewPaymentRailCapabilityRegistry` | policy del mismo nombre | `CanManageAch OR CanReadAch` | Admin, ACH.Operator | SPA mantiene role gate Admin/Operator + la misma unión | Admin, ACH.Operator | Admin, ACH.Operator | SÍ | GRANT | Ambos ya podían consultar por ambos fallbacks. |
| `Config.Read` | `P1.ConfigRead` | `CanReadAch` | Admin, ACH.Operator | MFT bajo `Conexiones`; MenuItem/SPA conservan gates transicionales | Admin, ACH.Operator | Admin, ACH.Operator | SÍ | GRANT | Ambos ya podían leer configuración por `CanReadAch`. |
| `Config.Manage` | `P1.ConfigManage` | `CanManageAch` | Admin, ACH.Operator | MFT conserva entrada/lectura separada; mutaciones fine OR fallback | Admin, ACH.Operator | Admin, ACH.Operator | SÍ | GRANT | Ambos ya gestionaban por `CanManageAch`. |
| `Integrations.Read` | `P1.IntegrationsRead` | `CanReadAch` | Admin, ACH.Operator | children y parent SPA conservan role gates Admin/Operator transicionales | Admin, ACH.Operator | Admin, ACH.Operator | SÍ | GRANT | Ambos ya leían Integraciones por `CanReadAch`. |
| `Integrations.ManageContracts` | `P1.IntegrationContractsManage` | `CanManageAch` | Admin, ACH.Operator | superficie también requiere lectura `Integrations.Read OR CanReadAch` | Admin, ACH.Operator | Admin, ACH.Operator | SÍ | GRANT | Ambos ya gestionaban contratos por `CanManageAch`. |
| `Integrations.ManageMappings` | `P1.IntegrationMappingsManage` | `CanManageAch` | Admin, ACH.Operator | parent/child role gates transicionales; lectura separada | Admin, ACH.Operator | Admin, ACH.Operator | SÍ | GRANT | Ambos ya gestionaban mappings por `CanManageAch`. |
| `Integrations.Validate` | `P1.IntegrationMappingsValidate` | `CanManageAch` | Admin, ACH.Operator | parent/child role gates transicionales; lectura separada | Admin, ACH.Operator | Admin, ACH.Operator | SÍ | GRANT | Ambos ya validaban mappings por `CanManageAch`. |
| `Integrations.Publish` | `P1.IntegrationMappingsPublish` | `CanManageAch` | Admin, ACH.Operator | parent/child role gates transicionales; lectura separada | Admin, ACH.Operator | Admin, ACH.Operator | SÍ | GRANT | Ambos ya publicaban mappings por `CanManageAch`. |
| `Integrations.ManageConnections` | `P1.IntegrationConnectionsManage` | `CanManageAch` | Admin, ACH.Operator | parent/child role gates transicionales; lectura separada | Admin, ACH.Operator | Admin, ACH.Operator | SÍ | GRANT | Ambos ya gestionaban conexión por `CanManageAch`. |

Resultado: **0 capabilities quedan SKIP a nivel capability** para el estado canónico sembrado. Sí existen **7 exclusiones role-capability deliberadas para `ACH.Operator`**: `TestTools.Access`, `Certificates.UploadPublic`, `Certificates.RegisterPrivate`, `Certificates.Revoke`, `AuditLogs.Read`, `AuthLogs.Read` y `NavigationMenu.Manage`. Ninguna es ambigua: la primera está decidida explícitamente Admin-only y las seis restantes carecen del fallback legacy correspondiente en `ACH.Operator`.

### Implementación del backfill

Se agrega un único `FineGrainedGrantBackfillSeeder` con `Order = 119`, posterior a todos los materializadores de permisos target (`Order 0`), al grant preexistente de TestTools (`Order 1`) y a los reconciliadores NAV3 de navegación (`101..118`). No existe otro seeder de seguridad con `Order 119`.

El seeder:

- valida los IDs/nombres estables de `Admin` y `ACH.Operator`;
- localiza cada `Permission` target por `Name` y falla cerrado si alguna no fue persistida previamente;
- crea únicamente los pares `RolePermission` faltantes;
- usa el conjunto de pares existentes para ser idempotente en reruns;
- no elimina grants;
- no modifica `Role`, `Permission`, `UserRole` ni usuarios;
- no existe un modelo `UserPermission` directo en `AchDbContext`.

Nuevas filas esperadas en el primer run:

```text
Admin         27 nuevas
ACH.Operator  21 nuevas
TOTAL         48 nuevas RolePermission
```

`Admin -> TestTools.Access` ya existía antes de NAV3.5A y no se cuenta entre las 48 nuevas filas. Después del backfill, dentro del inventario NAV3.5A, Admin posee las 28 capabilities target y ACH.Operator posee 21.

### Invariantes before/after

Para cada grant agregado se demostró:

```text
ROL + estado anterior
  -> policy satisfecha por el fallback legacy vigente

ROL + estado posterior
  -> la misma policy sigue satisfecha
     por FinePermission y por el fallback legacy, que no se retiró
```

No cambia la audiencia efectiva sembrada: ningún rol gana una policy que antes fallara y ningún rol pierde una policy que antes pasara. No se crean roles, grants directos de usuario ni cambios de `UserRole`.

Los siguientes elementos permanecen deliberadamente intactos y deben seguir considerándose transicionales:

- `CanReadAch`, `CanManageAch`, `CanManageUsers`, `CanManageCertificates` y demás fallbacks;
- role gates `Admin` / `ACH.Operator`;
- parent `/transactions`, que todavía puede bloquear el recorrido SPA de una capability fina aislada si faltan sus roles/permisos legacy del parent;
- parent/children `/integraciones`, cuyos role gates Admin/ACH.Operator siguen siendo transicionales aun después de otorgar `Integrations.*` a los dos roles canónicos;
- dependencias auxiliares como `CanReadAch` en selectores/lecturas compartidas de Certificados y en la entrada de Sobre digital.

### Cobertura para un NAV3.5B posterior

NAV3.5A **no ejecuta NAV3.5B**. A nivel de seed canónico, el backfill deja cobertura suficiente para evaluar de forma separada y con runtime la retirada de fallbacks de las 27 policies target con compatibilidad legacy. Los candidatos más directos son los fallbacks `CanManageUsers` de `AuditLogs.Read`, `AuthLogs.Read` y `NavigationMenu.Manage`; `CanManageCertificates` de los tres mutadores de certificados; y `CanManageAch` de las operaciones mutables target ya backfilled.

No deberían retirarse todavía, sin validación adicional de navegación/runtime y de bases desplegadas/customizadas, los gates/alternativas compartidos que sostienen prerequisitos de lectura o parent routing: el parent `/transactions`, los role gates de `/integraciones`, `CanReadAch` usado por lecturas auxiliares de Certificados/Sobre digital y cualquier fallback cuya población desplegada no esté representada por el seed canónico. Retirar un fallback de policy y retirar el permiso legacy del rol son decisiones distintas; NAV3.5A no realiza ninguna de las dos.


### Validación NAV3.5A

Pasan el grafo de solución 7/7, Herramientas de prueba 39, todas las regresiones vigentes NAV3.4B–F/G2–R y el validador focal NAV3.5A con **259 aserciones**. La validación focal hash-lockea las policies P0/P1, configuración de roles, asociaciones de menú, guards SPA, `NavigationService`, `MainLayout` y `GetMenuForCurrentUserHandler` contra el ZIP NAV3.4R para demostrar que el incremento es grants-only.

`.NET = NOT_RUN_ENVIRONMENT` porque `dotnet` no está disponible. Angular build/test/runtime = `NOT_RUN_ENVIRONMENT` porque `web/ach-interbank-ui/node_modules` no existe; Node 22.16.0, npm 10.9.2 y TypeScript 5.8.3 sí están instalados. No se inventan resultados runtime.

## NAV3.5B — Deployment-safe legacy permission migration

NAV3.5B parte únicamente de `ACHInterbank-R34-NAV-IA-NAV3.5A-FULL-PROJECT.zip` y mantiene NAV3.5A intacto. Este incremento **no retira ningún fallback**, **no retira role gates**, **no cambia MenuItem/SPA/API policies** y **no modifica la audiencia efectiva**. Su función es preparar el estado de datos de bases desplegadas para que los roles personalizados ya existentes no dependan exclusivamente de permisos legacy cuando se evalúe un retiro posterior.

La autorización continúa evaluándose con la misma forma transicional:

```text
FinePermission OR LegacyPermission
```

### Modelo efectivo de permisos

El modelo persistido de autorización de usuario es role-based:

```text
User
  -> UserRole
     -> Role
        -> RolePermission
           -> Permission
```

`AchDbContext` expone `Users`, `Roles`, `Permissions`, `UserRoles` y `RolePermissions`. No existe entidad, `DbSet`, configuración EF ni tabla de dominio `UserPermission`/`UserPermissionConfiguration`, ni un catálogo persistido equivalente de claims directos por usuario.

`UserAuthRepository` carga `UserRoles -> Role -> RolePermissions -> Permission`. `AuthService.BuildAuthResult` deriva la colección `permissions` exclusivamente de esa cadena y emite un claim JWT `permission` por nombre de `Permission`. `RefreshSessionAsync` vuelve a cargar al usuario por el mismo repositorio, por lo que una renovación de sesión posterior al backfill obtiene los fine grants recién agregados.

La evaluación runtime es claim-based: las policies consultan claims `permission`. Sin embargo, en el modelo persistido de esta aplicación los claims de permisos que genera el login/refresh provienen únicamente de `RolePermission`. Por tanto, la clasificación actual es **A: FineGrainedPermission mediante RolePermission**, no existe un mecanismo persistido de permisos directos a usuario y NAV3.5B no crea uno.

Existe una dependencia operativa importante para un retiro futuro: un JWT ya emitido antes del backfill conserva el snapshot de claims con el que fue creado. Aunque la DB ya tenga el fine grant, ese token no lo incorpora hasta login/refresh o expiración. Antes de retirar cualquier fallback en despliegue debe garantizarse renovación/expiración de sesiones activas después de ejecutar NAV3.5B.

### Catálogo y asignabilidad de permisos legacy

`CanReadAch`, `CanManageAch`, `CanManageUsers` y `CanManageCertificates` siguen persistidos en `PermissionConfiguration` y `PermissionsService` continúa devolviendo el catálogo completo de `Permission`. NAV3.5B no los elimina, desactiva ni oculta.

En el estado vigente no existe un endpoint first-party para crear/editar roles o asignar `RolePermission`: `RolesController` y `PermissionsController` son read-only (`GET`). La administración de usuarios puede asignar `UserRole`, y el editor de navegación modifica `MenuItemRole`/`MenuItemPermission`, no `RolePermission`. Aun así, una base desplegada puede contener roles personalizados y `RolePermission` creados históricamente, por migraciones u operación fuera de estas APIs; NAV3.5B los toma como fuente real al ejecutarse.

Después de un eventual retiro de fallbacks debe resolverse también el caso de **nuevos** roles creados fuera de las APIs actuales: mientras los permisos legacy sigan en `Permission`, un proceso externo podría asignar únicamente un legacy permission. NAV3.5B no desactiva esa posibilidad; esa gobernanza pertenece a NAV3.5C+ y debe preceder cualquier retiro global del permiso legacy.

### Seeder NAV3.5B

Se agrega `LegacyFineGrainedRoleMigrationSeeder` con `Order = 120`, inmediatamente después de `FineGrainedGrantBackfillSeeder` (`Order = 119`). El seeder genérico coexiste con NAV3.5A y no reescribe su decisión canónica.

Características:

- opera sobre **todos los `RolePermission` existentes** que referencian uno de los cuatro fallbacks target;
- no consulta ni ramifica por nombre de rol;
- resuelve legacy y fine permissions por `Permission.Name` y falla cerrado si falta cualquiera de las permissions requeridas;
- construye la unión `(RoleId, FinePermissionId)` a partir de cada legacy grant real;
- elimina duplicados cuando una capability tiene más de un fallback;
- crea únicamente pares `RolePermission` faltantes;
- rerun idempotente: segundo run agrega 0 filas;
- no crea roles, usuarios, `UserRole`, `Permission` ni grants directos de usuario;
- no elimina grants legacy ni fine;
- `TestTools.Access` está expresamente fuera del mapping genérico.

Sobre una DB canónica post-NAV3.5A, el insert esperado de NAV3.5B es **0**: Admin y ACH.Operator ya contienen todos los fine grants derivados de sus fallbacks. En una DB con roles personalizados, el resultado depende exclusivamente de los `RolePermission` legacy presentes al ejecutar el seeder.

### Mapping legacy → fine validado contra policies vigentes

La matriz final contiene **29 edges legacy→fine** y **27 fine capabilities únicas**. Los dos duplicados intencionales son `Certificates.Read` y `CanViewPaymentRailCapabilityRegistry`, cada uno alcanzable por dos fallbacks distintos.

| Legacy permission | Fine capabilities derivadas | Edges |
| --- | --- | ---: |
| `CanReadAch` | `Returns.Read`; `BulkIngestion.Read`; `Certificates.Read`; `NavigationLogs.Read`; `CanViewPaymentRailCapabilityRegistry`; `Config.Read`; `Integrations.Read` | 7 |
| `CanManageAch` | `Transactions.Create`; `Nacha.Upload`; `Returns.GenerateFile`; `BulkIngestion.Upload`; `BulkIngestion.Retry`; `BulkIngestion.Cancel`; `DigitalEnvelope.Encrypt`; `DigitalEnvelope.Decrypt`; `CanViewPaymentRailCapabilityRegistry`; `Config.Manage`; `Integrations.ManageContracts`; `Integrations.ManageMappings`; `Integrations.Validate`; `Integrations.Publish`; `Integrations.ManageConnections` | 15 |
| `CanManageUsers` | `AuditLogs.Read`; `AuthLogs.Read`; `NavigationMenu.Manage` | 3 |
| `CanManageCertificates` | `Certificates.Read`; `Certificates.UploadPublic`; `Certificates.RegisterPrivate`; `Certificates.Revoke` | 4 |

Cada edge anterior coincide con una policy real cuyo fallback continúa vigente. No se agregan edges para permisos existentes fuera del trabajo NAV3. En particular, `TestTools.Access` no tiene ningún edge desde `CanManageAch`/`CanManageUsers`, y `Certificates.Activate`, `Certificates.Validate` y `Certificates.Audit` siguen fuera del backfill NAV3.5A/3.5B.

### Comportamiento con roles personalizados

Los escenarios focales esperados son:

```text
CustomReadRole + CanReadAch
  -> 7 fine grants

CustomManageRole + CanManageAch
  -> 15 fine grants

CustomUserAdminRole + CanManageUsers
  -> AuditLogs.Read + AuthLogs.Read + NavigationMenu.Manage

CustomCertificateRole + CanManageCertificates
  -> Certificates.Read + Certificates.UploadPublic
     + Certificates.RegisterPrivate + Certificates.Revoke

Role con los cuatro legacy permissions
  -> unión de 27 fine capabilities únicas

Role sin legacy permission target
  -> 0 fine grants

segundo run
  -> 0 inserts adicionales
```

Un rol custom con `CanManageAch` no recibe `TestTools.Access`. La decisión `Admin -> TestTools.Access` permanece en `TestToolsAdminRolePermissionSeeder`; ACH.Operator continúa sin ese permiso.

### Auditoría de permisos directos a usuario

No existe un mecanismo persistido de permisos directos a usuario en el código vigente. Por tanto, NAV3.5B no encuentra una población `UserPermission` que deba migrarse y no existe un blocker de retiro derivado de filas direct-user almacenadas por esta aplicación.

Esto no elimina el riesgo de **sesiones JWT antiguas**: los claims están materializados en el token hasta refresh/login/expiración. El retiro de fallbacks debe esperar a que el backfill haya corrido en el entorno y las sesiones activas hayan sido renovadas o expirado.

### Dependencia específica de `/transactions`

El parent SPA `/transactions` permanece exactamente:

```text
roles: Admin OR ACH.Operator
permissions: CanManageAch OR CanReadAch
```

Por ello, aun con fine grants ya presentes, una identidad capability-only no puede usar el subárbol si no satisface además los role gates y legacy permissions del parent. Las capabilities NAV3.5B afectadas dentro de ese subárbol incluyen al menos:

- `Transactions.Create`;
- `Nacha.Upload`;
- `BulkIngestion.Upload`;
- `BulkIngestion.Read`;
- `BulkIngestion.Retry`;
- `BulkIngestion.Cancel`;
- `Returns.Read`;
- `Returns.GenerateFile`.

Además, `/transactions/returns` sigue guardado directamente por `CanReadAch`, mientras `/transactions/returns-ror` acepta `Returns.Read OR CanReadAch`. Los mutadores de bulk/returns mantienen sus gating transicionales propios.

Retirar fallbacks backend antes de normalizar el parent dejaría una arquitectura inconsistente para principals fine-only: el API podría aceptar la capability fina mientras la SPA seguiría exigiendo roles/legacy permissions. Para Admin/ACH.Operator sembrados no se produciría pérdida inmediata mientras los legacy grants sigan presentes, pero el objetivo capability-only seguiría incompleto. Por eso `/transactions` bloquea un retiro coordinado global de `CanReadAch`/`CanManageAch`.

### Dependencia específica de `/integraciones`

El parent SPA permanece:

```text
(Admin OR ACH.Operator)
AND
(Integrations.Read OR CanReadAch OR CanManageAch)
```

Los children `contrato`, `relacion-informacion`, editor de mapping y `conexion` permanecen con:

```text
Integrations.Read OR CanReadAch
```

Las acciones de gestión continúan usando sus capabilities `Integrations.Manage*`/`Validate`/`Publish` con fallback `CanManageAch`.

Antes de retirar `CanReadAch` o `CanManageAch` de las policies de Integraciones se requieren, además del backfill ya implementado: ejecución real de NAV3.5B en la DB desplegada, renovación/expiración de tokens y validación runtime. Antes de retirar los role gates `Admin/ACH.Operator` debe existir una decisión explícita de audiencia capability-only y un cambio coordinado de parent/MenuItem/SPA; NAV3.5B no toma esa decisión.

### Certificados / Sobre digital / cámaras operativas

`Certificates.Read` ya puede derivarse de `CanReadAch` o `CanManageCertificates`, y NAV3.5B migra ambos caminos. Sin embargo, la carga de certificados tiene prerequisitos SPA auxiliares que siguen dependiendo de `CanReadAch`:

- `NachaCertificateManagerComponent` sólo carga cámaras operativas e instituciones financieras cuando posee `CanReadAch`;
- `GET api/clearing-houses/operational` sigue protegido por `CanReadAch`;
- `GET financial-institutions` sigue protegido por `CanReadAch`;
- la habilitación de upload en el componente sigue exigiendo `CanReadAch` además de los fine permissions de preview/save.

La entrada SPA `/nacha-security/sobre-digital` continúa exigiendo `CanReadAch`, aunque las operaciones backend de encrypt/decrypt ya acepten `DigitalEnvelope.Encrypt/Decrypt OR CanManageAch`.

Por tanto, el backfill de roles no basta para retirar `CanReadAch` globalmente. Ese permiso aún es una dependencia directa de selectores auxiliares, cámaras operativas y entrada de Sobre digital, además de muchas otras superficies fuera de NAV3.5B.

### Uso legacy fuera del mapping NAV3.5B

La revisión de código confirma que los cuatro permisos legacy no pueden interpretarse como exclusivos de los 29 edges anteriores:

- `CanReadAch` aparece en numerosas policies adicionales y en decenas de endpoints que todavía lo usan directamente; también sostiene varios guards SPA fuera del inventario NAV3.5B.
- `CanManageAch` aparece en numerosas policies adicionales y endpoints directos, incluyendo administración de catálogos/operación y otras capacidades no normalizadas por NAV3.5A.
- `CanManageUsers` sigue siendo un guard SPA directo del módulo administrativo de usuarios, además de los tres fallbacks NAV3.5B.
- `CanManageCertificates` sigue siendo fallback de `Certificates.Activate`, `Certificates.Validate` y `Certificates.Audit`, capabilities que no forman parte del backfill NAV3.5A/3.5B.

NAV3.5B no elimina ni intenta normalizar esos usos.

### Readiness para retiro de fallbacks

La clasificación considera migración de roles, direct-user risk, dependencias SPA/parent, usos fuera del mapping, sesiones JWT y runtime. **Ningún fallback se retira en este incremento.**

| Legacy fallback | Fine capabilities NAV3.5B dependientes | Role grants migrados dinámicamente | Direct-user risk | Parent/SPA / otras dependencias | Runtime dependency | Candidate for removal | Blocker |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `CanManageUsers` | `AuditLogs.Read`, `AuthLogs.Read`, `NavigationMenu.Manage` | Sí, para todo rol existente con el fallback | No existe persistencia direct-user | `/navigation`, logs y el módulo admin aún contienen gating legacy; el permiso no puede retirarse globalmente sólo por este backfill | Seeder debe correr en DB real; JWT activos deben renovarse; .NET/Angular no ejecutados | `BLOCKED` | runtime + sesión + usos SPA legacy fuera del retiro focal |
| `CanManageCertificates` | `Certificates.Read`, `UploadPublic`, `RegisterPrivate`, `Revoke` | Sí | No existe persistencia direct-user | `Certificates.Activate/Validate/Audit` todavía dependen de `CanManageCertificates`; rutas/componentes conservan compatibilidad | Seeder real + renovación de JWT + runtime pendiente | `BLOCKED` | capabilities de certificados fuera del backfill + runtime |
| `CanReadAch` | 7 capabilities read | Sí | No existe persistencia direct-user | parent `/transactions`; parent/children `/integraciones`; selectores de certificados; cámaras operativas; entrada de Sobre digital; múltiples policies/endpoints directos fuera de NAV3.5B | Seeder real + renovación de JWT + runtime pendiente | `BLOCKED` | dependencia transversal no normalizada + runtime |
| `CanManageAch` | 15 capabilities de gestión | Sí | No existe persistencia direct-user | parent `/transactions`; parent `/integraciones`; múltiples policies/endpoints/guards directos fuera de NAV3.5B | Seeder real + renovación de JWT + runtime pendiente | `BLOCKED` | dependencia transversal no normalizada + runtime |

La ausencia de direct-user permissions elimina uno de los blockers anticipados, pero no resuelve los restantes. El estado correcto al cierre de NAV3.5B es que **los cuatro fallbacks están BLOCKED para retiro**.

### Validación NAV3.5B

Se agrega:

- `tests/Cfa.ACHInterbank.Tests/NavigationMenuNav35BDeploymentSafeLegacyPermissionMigrationTests.cs`, con escenarios de custom roles, unión, rol sin fallback, idempotencia, canonical zero-insert y audiencia before/after;
- `scripts/validate-nav3-5b-deployment-safe-legacy-permission-migration.py`, que valida mapping vs policies reales, exclusión de TestTools, modelo role-based, ausencia de `UserPermission`, Order 120, coexistencia con NAV3.5A y hash-lock de policies/menu/SPA/auth contra el ZIP NAV3.5A.

Validación estática disponible ejecutada: solution graph **PASS 7/7**, Herramientas de prueba/NAV3.3B **PASS 39**, NAV3.4B–F/G2–R **PASS**, NAV3.5A **PASS 259** y NAV3.5B focal **PASS 3371**. `.NET` permanece `NOT_RUN_ENVIRONMENT` por ausencia de `dotnet`; Angular permanece `NOT_RUN_ENVIRONMENT` por ausencia de `node_modules`.

NAV3.5B no ejecuta NAV3.5C. La recomendación de seguridad al cierre de diseño/código es **no retirar fallbacks hasta disponer de runtime**, ejecutar el backfill en la DB objetivo y forzar/esperar renovación de sesiones. Después de esa evidencia, todavía deberán resolverse los blockers estructurales descritos para cada legacy permission antes de un retiro global.
