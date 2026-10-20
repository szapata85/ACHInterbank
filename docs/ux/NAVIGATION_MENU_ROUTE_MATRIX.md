# Matriz ruta → menú objetivo — NAV-MENU-IA-001

Leyenda: `MENU`, `CONTEXTUAL`, `TECNICA`, `COMPATIBILIDAD`, `REDIRECT`, `FUERA_ALCANCE`.

## Menú objetivo

| Ruta | Nombre visible | Padre | Orden | Clasificación | Permiso base actual/objetivo |
|---|---|---|---:|---|---|
| `/dashboard` | Inicio | raíz | 10 | MENU | autenticado |
| `/transactions/list` | Consultar transacciones | Operación | 10 | MENU | `CanReadAch` |
| `/transactions/create` | Crear transacción | Operación | 20 | MENU | `CanManageAch` |
| `/transactions/bulk-ingestion/upload` | Cargar transacciones desde archivo | Operación | 30 | MENU | `CanManageAch` / revisar `BulkIngestion.Upload` |
| `/transactions/bulk-ingestion/tracking` | Seguimiento de cargas | Operación | 40 | MENU | `CanManageAch` / revisar `BulkIngestion.Read` |
| `/transactions/nacha-upload` | Cargar archivo NACHA-M | Operación | 50 | MENU | `CanManageAch` |
| `/transactions/returns` | Devoluciones | Operación | 60 | MENU | `CanManageAch` |
| `/transactions/returns-ror` | Devolución de una devolución | Operación | 70 | MENU | `CanManageAch` |
| `/ach-claims` | Reclamaciones | Operación | 80 | MENU | `Claims.Read` |
| `/ach-colombia/file-exchange` | Intercambio de archivos | Operación | 90 | MENU | `CanReadAch` |
| `/ach-cycles/nacha/export` | Exportar archivo NACHA-M | Operación | 100 | MENU | `CanReadAch` |
| `/customers` | Clientes | raíz | 30 | MENU | `CanReadAch` |
| `/transactions/outgoing-monitoring` | Transacciones enviadas | Seguimiento y control | 10 | MENU | `OutgoingTransactions.Monitor.Read` |
| `/incoming-nacha-command-center` | Seguimiento de archivos | Seguimiento y control | 20 | MENU | `CanReadAch` |
| `/incoming-nacha-command-center/observability` | Indicadores operativos | Seguimiento y control | 30 | MENU | `CanReadAch` |
| `/incoming-nacha-command-center/queue` | Cola de procesamiento | Seguimiento y control | 40 | MENU | `CanReadAch` |
| `/incoming-nacha-command-center/orphan-resolution` | Devoluciones sin relación | Seguimiento y control | 50 | MENU | `CanReadAch` |
| `/ach-responses` | Bandeja de respuestas | Respuestas de transacciones | 10 | MENU | `CanReadAch` |
| `/ach-responses/dashboard` | Resumen de respuestas | Respuestas de transacciones | 20 | MENU | `CanReadAch` |
| `/ach-responses/manual-review` | Revisión manual | Respuestas de transacciones | 30 | MENU | `CanReadAch` (revisar fino en NAV3/NAV5) |
| `/ach-responses/audit` | Historial de respuestas | Respuestas de transacciones | 40 | MENU | `CanReadAch` |
| `/ach/reconciliation` | Conciliación | Seguimiento y control | 70 | MENU | `CanReadAch` |
| `/clearing-houses` | Cámaras compensadoras | Administración de parámetros | 10 | MENU | `ClearingHouses.View` |
| `/catalogs/clearing-house-preferences` | Prioridades por cámara | Administración de parámetros | 20 | MENU | `CanManageAch` |
| `/ach-cycles` | Ciclos de operación | Administración de parámetros | 30 | MENU | `CanReadAch` |
| `/catalogs/bank-holidays` | Días no hábiles | Administración de parámetros | 40 | MENU | `CanManageAch` |
| `/operational-restrictions` | Restricciones operativas | Administración de parámetros | 50 | MENU | `OperationalRestrictions.Read` |
| `/nacha-config-admin` | Configuración de archivos NACHA-M | Administración de parámetros | 60 | MENU_GROUP | `CanReadAch`/Config |
| `/nacha-config-admin/perfiles` | Perfiles oficiales | Configuración de archivos NACHA-M | 10 | MENU | existente |
| `/nacha-config-admin/records` | Registros oficiales | Configuración de archivos NACHA-M | 20 | MENU | existente |
| `/nacha-config-admin/variants-fields` | Campos y variantes | Configuración de archivos NACHA-M | 30 | MENU | existente |
| `/catalogs/financial-institutions` | Entidades financieras | Catálogos | 10 | MENU | `CanManageAch` |
| `/catalogs/company-entry-descriptions` | Conceptos de lote | Catálogos | 20 | MENU | `CanManageAch` |
| `/catalogs/document-types` | Tipos de documento | Catálogos | 30 | MENU | `CanManageAch` |
| `/catalogs/person-types` | Tipos de persona | Catálogos | 40 | MENU | `CanManageAch` |
| `/catalogs/gender-types` | Tipos de género | Catálogos | 50 | MENU | `CanManageAch` |
| `/catalogs/phone-types` | Tipos de teléfono | Catálogos | 60 | MENU | `CanManageAch` |
| `/catalogs/email-types` | Tipos de correo | Catálogos | 70 | MENU | `CanManageAch` |
| `/catalogs/address-types` | Tipos de dirección | Catálogos | 80 | MENU | `CanManageAch` |
| `/catalogs/transaction-codes` | Códigos de transacción | Catálogos | 90 | MENU | `CanManageAch` |
| `/integraciones/contrato` | Contrato del servicio | Conexiones | 10 | MENU | Integrations / revisar |
| `/integraciones/relacion-informacion` | Relación de información | Conexiones | 20 | MENU | Integrations / revisar |
| `/integraciones/conexion` | Conexión entidad participante | Conexiones | 30 | MENU | Integrations / revisar |
| `/administracion/mft-ach` | Configuración del intercambio de archivos | Conexiones | 40 | MENU | Admin/`CanReadAch` actual; endurecer si aplica |
| `/ach-responses/status-mappings` | Homologación de estados | Administración de parámetros | 90 | MENU | `CanReadAch` actual; revisar permiso de configuración |
| `/nacha-security/certificates` | Certificados digitales | Seguridad de archivos | 10 | MENU | certificates |
| `/nacha-security/sobre-digital` | Cifrar y descifrar archivos | Seguridad de archivos | 20 | MENU | NACHA security |
| `/reports` | Reportes | raíz | 70 | MENU_GROUP | `CanReadAch` |
| `/reports/sent` | Transacciones enviadas | Reportes | 10 | MENU | `CanReadAch` |
| `/reports/received` | Transacciones recibidas | Reportes | 20 | MENU | `CanReadAch` |
| `/reports/returns` | Devoluciones | Reportes | 30 | MENU | `CanReadAch` |
| `/reports/rejections` | Rechazos | Reportes | 40 | MENU | `CanReadAch` |
| `/reports/files` | Archivos NACHA-M | Reportes | 50 | MENU | `CanReadAch` |
| `/reports/cycles` | Ciclos | Reportes | 60 | MENU | `CanReadAch` |
| `/reports/reconciliation` | Conciliación | Reportes | 70 | MENU | `CanReadAch` |
| `/reports/traceability` | Trazabilidad ACH | Reportes | 80 | MENU | `CanReadAch` |
| `/reports/history` | Histórico | Reportes | 90 | MENU | `CanReadAch` |
| `/reports/audit` | Auditoría | Reportes | 100 | MENU | `CanReadAch`; reporte funcional, distinto de `/audit-logs` |
| `/administracion` | Administración | raíz | 80 | MENU_GROUP | agrupador estructural; no es destino SPA funcional |
| `/users` | Usuarios y acceso | Administración | 10 | MENU_GROUP | reutiliza ruta SPA existente y conserva Admin + `CanManageUsers` |
| `/users/list` | Administrar usuarios | Usuarios y acceso | 10 | MENU | `CanManageUsers` |
| `/users/branding` | Identidad visual | Usuarios y acceso | 20 | MENU | `CanManageUsers` |
| `/users/password-rules` | Reglas de contraseña | Usuarios y acceso | 30 | MENU | `CanManageUsers` |
| `/users/login-lockout` | Bloqueo de acceso | Usuarios y acceso | 40 | MENU | `CanManageUsers` |
| `/scheduler/tasks` | Tareas programadas | Administración | 20 | MENU | `Scheduler.View` |

**NAV2.8B:** `/scheduler/tasks` es la única ruta SPA funcional publicada del scheduler. Se reutiliza su `MenuItem` existente con `Scheduler.View`; `/scheduler` queda como redirect SPA y su agrupador de menú legacy se desactiva. Las superficies de historial, instancias, detalle técnico, ejecución manual y edición de programación permanecen dentro de la pantalla/API y no se convierten en opciones de menú.
| `/logs` | Registros del sistema | Administración | 30 | MENU_GROUP | reutiliza `CanManageUsers`; SPA redirect a `/audit-logs`, usado sólo como agrupador visible |
| `/audit-logs` | Registro de auditoría | Registros del sistema | 10 | MENU | conserva `CanManageUsers`; SPA/API actualmente sólo exigen autenticación (gap) |
| `/auth-logs` | Registro de accesos | Registros del sistema | 20 | MENU | conserva `CanManageUsers`; SPA/API actualmente sólo exigen autenticación (gap) |
| `/navigation-logs` | Registro de navegación | Registros del sistema | 30 | MENU | nuevo MenuItem con `CanReadAch`, reutilizado del GET API; SPA sólo exige autenticación y el padre conserva `CanManageUsers` (gap) |


**NAV2.8C:** `/logs` se reutiliza como agrupador estructural `Registros del sistema`; sus hijos funcionales quedan en `/audit-logs`, `/auth-logs` y `/navigation-logs` con órdenes 10, 20 y 30. Los dos primeros conservan sus asociaciones de menú `CanManageUsers`. `/navigation-logs` no tenía MenuItem y se publica con el permiso vigente del GET funcional `CanReadAch`. No se corrigen las diferencias de autorización SPA/API/menu descritas en la tabla. Los alias SPA `/log` y `/logs` continúan siendo redirects y no se crean entradas adicionales. `/reports/audit` sigue siendo la superficie de reporte bajo `Reportes` y no se mezcla con el registro administrativo.

| `/navigation/menu-items` | Organización del menú | Administración | 40 | MENU | conserva `Admin` + `CanManageUsers`; SPA exige ambos, API CRUD actualmente sólo `Admin` (gap) |
| `/payment-rail-capability-registry` | Capacidades de la plataforma | Administración | 50 | MENU | nuevo MenuItem NAV2.8E: `Admin`/`ACH.Operator` + `CanReadAch`; SPA/API efectivos aceptan también `CanManageAch` y la policy nominal `CanViewPaymentRailCapabilityRegistry` |

**NAV2.8D:** `/navigation/menu-items` es la superficie funcional real para administrar la estructura de navegación. Se reutiliza su `MenuItem` existente y se mueve bajo `Administración` con Order 40, sin cambiar ruta ni asociaciones de autorización. `/navigation` sigue siendo sólo el prefijo/redirect SPA del módulo y no se crea como entrada adicional. `/navigation-logs` permanece bajo `Registros del sistema`.

**NAV2.8E:** `/payment-rail-capability-registry` deja de clasificarse como superficie técnica huérfana y se publica como `Capacidades de la plataforma` bajo `Administración` con Order 50. La evidencia funcional la identifica como consulta administrativa read-only de gobernanza multi-riel: no cambia parámetros, no ejecuta simulaciones y no expone acciones de escritura. El SPA exige `Admin` o `ACH.Operator` y ANY de `CanViewPaymentRailCapabilityRegistry`/`CanManageAch`/`CanReadAch`. Aunque el controller referencia la policy `CanViewPaymentRailCapabilityRegistry`, su definición vigente usa el mismo fallback ANY. Como el permiso dedicado no existe en el catálogo persistido de `Permission`, el MenuItem nuevo usa `CanReadAch` + `Admin`/`ACH.Operator`, autorización existente que no amplía el acceso efectivo del API. La normalización de esa policy dedicada queda pendiente de la fase de seguridad.
| `/uat` | Herramientas de prueba | raíz | 90 | MENU_GROUP_TECNICO | reutiliza `Admin`/`ACH.Operator` + `CanManageAch`/`CanReadAch`; GetMenu aún aplica gate por ambiente (NAV3) |
| `/uat/nacha-inbound-simulator` | Simulador de archivos recibidos | Herramientas de prueba | 10 | MENU_TECNICA | activo; conserva `Admin`/`ACH.Operator` + `CanManageAch`/`CanReadAch`; SPA hija exige `CanManageAch`; API aún gatea por ambiente |
| `/ach/nacha/soap-uat-console` | Pruebas de servicios | Herramientas de prueba | 20 | MENU_TECNICA_INACTIVA | nuevo MenuItem inactivo con `Admin`/`ACH.Operator` + `CanManageAch`; NAV3 debe reemplazar por permiso técnico y eliminar gates de ambiente |

**NAV2.9:** la búsqueda de rutas SPA con propósito explícito de simulación/prueba técnica confirma dos destinos navegables: `/uat/nacha-inbound-simulator` y `/ach/nacha/soap-uat-console`. `UatContrapartidasController` (`api/uat/contrapartidas`) y `TestsController` (`/Tests`) son endpoints backend-only sin pantalla SPA independiente, por lo que quedan fuera del menú. El simulador existente conserva su estado activo y su autorización de menú legacy. `Pruebas de servicios` se siembra inactiva hasta NAV3 porque la autorización SPA efectiva requiere `CanReadAch` en el padre `/ach` y `CanManageAch` en la hija, mientras el filtro de menú interpreta múltiples permisos como OR; además el gating actual de Production sólo filtra rutas `/uat/**`. No se cambian guards, policies, roles, permisos ni grants en NAV2.9.

**NAV2.10A:** la matriz queda respaldada por una única representación activa de `/ach-cycles`: `Ciclos de operación` bajo `Administración de parámetros` Order 30. La fila legacy duplicada se conserva inactiva. `/dashboard` queda canonicalizado como `Inicio` Order 10 y no se agregan rutas nuevas.

**NAV2.10B:** las nueve rutas del agrupador `Catálogos` quedan cubiertas en seed con órdenes 10..90. `/catalogs/financial-institutions` reutiliza su MenuItem y conserva autorización; las ocho rutas administrativas restantes se crean con `Admin`/`ACH.Operator` + `CanManageAch`, consistente con sus guards SPA y APIs CRUD vigentes. `/catalogs/clearing-house-special-dates` conserva su ruta redirect contextual, pero su fila legacy de menú queda inactiva para no competir con la navegación por cámara.

## Rutas que no deben entrar al menú principal

| Ruta/patrón | Clasificación | Motivo |
|---|---|---|
| `/customers/new`, `/customers/:id/edit` | CONTEXTUAL | alta/edición desde Clientes |
| `/customers/:id/third-parties/**` | CONTEXTUAL | navegación desde cliente |
| `/customer-third-parties` | COMPATIBILIDAD | superficie legacy |
| `/transactions/bulk-ingestion/:batchId` | CONTEXTUAL | detalle de lote |
| `/transactions/outgoing-monitoring/:id` | CONTEXTUAL | detalle |
| `/ach-responses/:id/**` | CONTEXTUAL | detalle/intentos |
| `/incoming-nacha-command-center/files/:fileId` | CONTEXTUAL | detalle |
| `/incoming-nacha-command-center/queue/:id` | CONTEXTUAL | detalle |
| `/clearing-houses/:id/**` | CONTEXTUAL | configuración por cámara |
| `/catalogs/clearing-house-special-dates` | REDIRECT/CONTEXTUAL | redirect legacy hacia fechas especiales dentro del contexto de cámara; MenuItem inactivo desde NAV2.10B |
| `/nacha-security/certificates/:id/versions` | CONTEXTUAL | detalle de certificado |
| `/nacha-security/nacha/generate*` | TECNICA/CONTEXTUAL | cubierto por flujos funcionales |
| `/nacha-security/digital-envelope/manual-*` | TECNICA/CONTEXTUAL | cubierto por Cifrar y descifrar |
| `/transactions/bulk-create` | TECNICA | entrada JSON, no prioritaria para usuario funcional |
| `/cenit/**` | FUERA_ALCANCE_MENU | no promover silo CENIT; conservar compatibilidad |
| `/cenit/operacion/neteo` | FUERA_ALCANCE | no requerido ahora |
| `/cenit/operacion/optimizacion` | FUERA_ALCANCE | no requerido ahora |
| legacy NACHA redirects y 403/404 | REDIRECT | navegación técnica |
