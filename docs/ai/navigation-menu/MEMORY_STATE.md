# State memory — NAV-MENU-IA-001

```yaml
program: NAV-MENU-IA-001
program_status: IMPLEMENTED_UNVERIFIED_ENVIRONMENT
source_control_mode: NO_GIT_FULL_ZIP
current_source_package: ACHInterbank-R34-NAV-IA-NAV5-FULL-PROJECT.zip
current_source_sha256: c11268bb2e5509abf6bbe39f4d2e0074714b1492674ed7d8b2b40183331ec115
baseline_git_sha: UNAVAILABLE_SOURCE_ARCHIVE_NO_GIT_METADATA

active_phase: NAV6
active_phase_status: IMPLEMENTED_UNVERIFIED_ENVIRONMENT
next_allowed_phase: USER_DECISION_REQUIRED
last_completed_step: NAV6_FINAL_RECERTIFICATION_STATIC_COMPLETE
last_output_package: ACHInterbank-R34-NAV-IA-NAV6-FULL-PROJECT.zip
output_sha256: EXTERNAL_HANDOFF_ONLY_SELF_HASH_CIRCULAR

approved_decisions:
  - id: DEC-NAV-001
    text: El seed/backend es la fuente de verdad de la navegación visible.
  - id: DEC-NAV-002
    text: El copy visible será español claro y funcional; no se expondrán CFA, MFT, SOAP, UAT, Dashboard, Logs, Scheduler o Status Mapping salvo necesidad técnica explícita.
  - id: DEC-NAV-003
    text: CENIT no será un menú raíz ni un micrositio promovido; neteo y optimización permanecen fuera del menú por ahora.
  - id: DEC-NAV-004
    text: La conexión se mostrará como 'Conexión entidad participante'.
  - id: DEC-NAV-005
    text: Herramientas de prueba se siembra siempre y se protege por permiso/rol técnico, no por IsProduction.
  - id: DEC-NAV-006
    text: No se usa Git; cada fase recibe y entrega ZIP full con proyecto y documentación.
  - id: DEC-NAV-007
    text: Una fase por chat; prompts cortos; cada fase actualiza este state memory.
  - id: DEC-NAV-008
    text: Órdenes entre hermanos usan saltos 10,20,30 y no se permiten empates.
  - id: DEC-NAV-009
    text: La ejecución global de IDbSeeder se ordena por Order y, en empate, por nombre completo del tipo con comparación ordinal para un desempate estable y determinístico.
  - id: DEC-NAV-010
    text: NavigationInformationArchitectureSeeder es el reconciliador canónico final de navegación; usa Order 100 y en NAV2.1 es intencionalmente no-op para no modificar el menú vigente.
  - id: DEC-NAV-011
    text: NAV2.2 crea el agrupador canónico 'Administración de parámetros' y reubica sólo ese dominio con órdenes 10..90, conservando las rutas funcionales existentes.
  - id: DEC-NAV-012
    text: El copy aprobado para /catalogs/clearing-house-preferences es 'Prioridades por cámara'; Conexiones publica las subrutas SPA existentes y usa 'Conexión entidad participante' para /integraciones/conexion.
  - id: DEC-NAV-013
    text: NAV2.3 reutiliza /transactions como raíz canónica 'Operación' y publica sólo las diez rutas funcionales objetivo con órdenes 10..100; las nuevas entradas de carga por archivo, seguimiento de cargas y devolución de una devolución usan la autorización SPA vigente Admin/ACH.Operator + CanManageAch sin crear permisos, roles ni grants.
  - id: DEC-NAV-014
    text: NAV2.4 publica /customers como raíz canónica 'Clientes' con Order 30 y reutiliza CanReadAch sólo para la nueva asociación de menú; /customer-third-parties queda inactivo en navegación pero su ruta, código y autorización existentes se conservan para compatibilidad.
  - id: DEC-NAV-017
    text: NAV2.5A crea el agrupador estructural /seguimiento-control como raíz 'Seguimiento y control' con Order 40 y mueve allí únicamente las cinco superficies autorizadas en este incremento, con órdenes 10..50.
  - id: DEC-NAV-018
    text: Las rutas nuevas de indicadores, cola y devoluciones sin relación reutilizan por espejo la autorización existente de /incoming-nacha-command-center sólo al crear su MenuItem; no se modifican definiciones de permisos, roles, RolePermissions ni asociaciones existentes.
  - id: DEC-NAV-019
    text: NAV2.5B completa Seguimiento y control con el agrupador estructural /seguimiento-control/respuestas-transacciones = 'Respuestas de transacciones' Order 60; /ach-responses queda como hijo navegable 'Bandeja de respuestas' y no se duplica la ruta padre/hijo.
  - id: DEC-NAV-020
    text: NAV2.5B publica cuatro rutas de respuestas con órdenes 10..40 y /ach/reconciliation como 'Conciliación' Order 70; las nuevas entradas funcionales reutilizan Admin/ACH.Operator + CanReadAch sólo al crearse y no alteran autorizaciones existentes.
  - id: DEC-NAV-021
    text: NAV2.6 reutiliza /nacha-security como raíz 'Seguridad de archivos' Order 60 y conserva /nacha-security/certificates y /nacha-security/sobre-digital como hijos 10 y 20 sin cambiar rutas ni autorización existente.
  - id: DEC-NAV-022
    text: Las rutas técnicas/manuales de generación NACHA-M, generación cifrada, cifrado manual y descifrado manual permanecen funcionales pero fuera del menú principal.
  - id: DEC-NAV-023
    text: NAV2.7 canonicaliza /reports como raíz 'Reportes' Order 70 y publica sus diez rutas SPA funcionales con órdenes 10..100 en el orden de presentación vigente del módulo de reportes.
  - id: DEC-NAV-024
    text: Los diez hijos de Reportes no tenían MenuItem previo; al crearse reutilizan la autorización existente de /reports (Admin/ACH.Operator + CanReadAch) sin modificar definiciones de permisos, roles, RolePermissions, grants ni autorización de MenuItem preexistente.
  - id: DEC-NAV-025
    text: NAV2.8A crea únicamente el agrupador estructural /administracion = Administración Order 80 y reutiliza /users como Usuarios y acceso Order 10; /administracion no es una pantalla SPA funcional.
  - id: DEC-NAV-026
    text: NAV2.8A reutiliza los MenuItem existentes /users/list, /users/branding, /users/password-rules y /users/login-lockout con órdenes 10..40 y conserva sus rutas, IDs y autorizaciones existentes Admin + CanManageUsers sin modificar permisos, roles, RolePermissions ni grants.
  - id: DEC-NAV-027
    text: NAV2.8B reutiliza /scheduler/tasks como Tareas programadas Order 20 directamente bajo Administración; el agrupador legacy /scheduler queda inactivo en menú, conservando ruta SPA redirect, IDs y autorización Scheduler.View sin modificar permisos, roles, RolePermissions ni grants.
  - id: DEC-NAV-028
    text: NAV2.8C reutiliza /logs como Registros del sistema Order 30 bajo Administración; /audit-logs y /auth-logs conservan sus MenuItem e identificación/autorización preexistentes y se canonicalizan como Registro de auditoría 10 y Registro de accesos 20.
  - id: DEC-NAV-029
    text: NAV2.8C publica la ruta SPA existente /navigation-logs como Registro de navegación Order 30; al no existir MenuItem previo, el nuevo ítem reutiliza CanReadAch del GET api/navigation-logs sin crear ni modificar permisos, roles, RolePermissions o grants.
  - id: DEC-NAV-030
    text: NAV2.8D reutiliza MenuItemConfiguration.NavigationAdminId (/navigation/menu-items) como Organización del menú Order 40 directamente bajo Administración y conserva su ruta, ID y asociaciones preexistentes Admin + CanManageUsers.
  - id: DEC-NAV-031
    text: NAV2.8E clasifica /payment-rail-capability-registry como Capacidades de la plataforma, consulta administrativa read-only de gobernanza multi-riel, y la publica como hijo directo de Administración Order 50.
  - id: DEC-NAV-032
    text: El nuevo MenuItem de Capacidades de la plataforma reutiliza CanReadAch + roles Admin/ACH.Operator, ya aceptados por el acceso efectivo SPA/API; no crea ni modifica permisos, roles, RolePermissions, grants, policies ni autorizaciones de MenuItem existentes.
  - id: DEC-NAV-033
    text: NAV2.9 canonicaliza /uat como raíz Herramientas de prueba Order 90 y reutiliza /uat/nacha-inbound-simulator como Simulador de archivos recibidos Order 10, conservando sus asociaciones existentes Admin/ACH.Operator + CanManageAch/CanReadAch y sin condicionar el seed al ambiente.
  - id: DEC-NAV-034
    text: /ach/nacha/soap-uat-console corresponde a Pruebas de servicios Order 20 bajo Herramientas de prueba, pero su nuevo MenuItem queda IsActive=false hasta NAV3 porque el modelo actual de menú no representa la conjunción efectiva de guardas SPA y el filtrado de /uat por ambiente podría dejar flotar un hijo con prefijo /ach cuando el padre se oculta.
  - id: DEC-NAV-035
    text: NAV2.10A canonicaliza /dashboard como Inicio Order 10 y deja exactamente los nueve dominios raíz aprobados en órdenes 10..90 sin cambiar rutas ni autorizaciones.
  - id: DEC-NAV-036
    text: NAV2.10A conserva MenuItemConfiguration.AchCyclesId como única representación visible de /ach-cycles bajo Administración de parámetros y deja inactivos los MenuItem duplicados de esa misma ruta sin eliminar datos ni autorización histórica.
  - id: DEC-NAV-037
    text: NAV2.10B completa Catálogos bajo Administración de parámetros con nueve hijos canónicos exactos en órdenes 10..90, reutilizando /catalogs/financial-institutions y publicando sólo las ocho rutas SPA administrativas aprobadas que faltaban.
  - id: DEC-NAV-038
    text: Los ocho MenuItem nuevos de Catálogos reutilizan Admin/ACH.Operator + CanManageAch, consistente con sus guards SPA y APIs CRUD; /catalogs/clearing-house-special-dates conserva ruta/código/autorización pero queda inactivo como redirect contextual legacy.
  - id: DEC-NAV-039
    text: /catalogs/clearing-house-special-dates queda inactivo como MenuItem principal porque es un redirect SPA contextual hacia la administración por cámara; su ruta funcional y autorización histórica se conservan.
  - id: DEC-NAV-040
    text: NAV3.1 crea el permiso técnico canónico TestTools.Access (Acceso a herramientas de prueba) como FineGrainedPermissions y lo persiste idempotentemente con TestToolsPermissionSeeder; NachaSimulator.Read permanece como permiso específico del simulador y no sustituye la semántica general del dominio.
  - id: DEC-NAV-041
    text: NAV3.1 no asigna TestTools.Access a ningún rol porque los únicos roles sembrados son Admin y ACH.Operator; ACH.Operator está excluido y Admin no se equipara implícitamente a Superadministrador. No se crea ningún rol Desarrollo/Soporte/Superadministrador ni se cambia la autorización vigente de las herramientas.
  - id: DEC-NAV-042
    text: NAV3.2A confirma Admin como el rol administrativo existente de máximo privilegio y siembra únicamente Admin -> TestTools.Access mediante RolePermissions; ACH.Operator permanece sin el permiso y no se crean roles ni grants directos a usuarios.
  - id: DEC-NAV-043
    text: NAV3.2A persiste Admin -> TestTools.Access idempotentemente con TestToolsAdminRolePermissionSeeder; no crea Superadministrador ni Desarrollo/Soporte.
  - id: DEC-NAV-044
    text: NAV3.2B hace TestTools.Access obligatorio en el menú de /uat y sus dos herramientas mediante un reconciliador posterior a NAV2 que elimina fallbacks genéricos y MenuItemRole directos, manteniendo Pruebas de servicios inactiva.
  - id: DEC-NAV-045
    text: NAV3.2B hace TestTools.Access obligatorio en las rutas SPA propias de ambas herramientas; /uat deja de usar roles/CanReadAch/CanManageAch, mientras la consola SOAP conserva como restricción adicional heredada el padre compartido /ach hasta un incremento posterior.
  - id: DEC-NAV-046
    text: NAV3.2B exige TestTools.Access a nivel de controller en ambas APIs; las policies NachaSimulator específicas permanecen como requisitos adicionales y ya no pueden funcionar como bypass del permiso de dominio.
  - id: DEC-NAV-047
    text: NAV3.3A elimina del handler de menú todo filtrado de /uat y /uat/** por Production/UAT-like; la visibilidad queda determinada por IsActive, jerarquía y TestTools.Access.
  - id: DEC-NAV-048
    text: NAV3.3A elimina del NachaInboundSimulatorController los checks Production/IsUatLike y conserva TestTools.Access más las policies NachaSimulator específicas como autorización efectiva acumulativa.
  - id: DEC-NAV-049
    text: NAV3.3A elimina el requisito Mode=UAT/Local del servicio del simulador, conserva Enabled como switch funcional y mantiene fail-closed AllowAutoImport=false y AllowExternalTransmission=false; el simulador sigue generate-only y sin envío externo automático.
  - id: DEC-NAV-050
    text: NAV3.3B conserva exactamente /ach/nacha/soap-uat-console pero la declara como ruta SPA independiente antes del padre compartido /ach, protegida sólo por permissionGuard + TestTools.Access; elimina la ruta hija duplicada y deja intactas las guardas Admin/ACH.Operator + CanReadAch de las demás rutas /ach/**.
  - id: DEC-NAV-051
    text: NAV3.3B elimina de NachaSoapUatConsoleController los gates Production/IsUatLike y la policy adicional NachaSimulatorRead; la consola es íntegramente GET/read-only y su frontera API efectiva queda exclusivamente en TestTools.Access, sin fallbacks ACH o por nombre de rol.
  - id: DEC-NAV-052
    text: La consola usa realmente GET dashboard, candidates y audit de su propio controller; el detail GET permanece read-only aunque el componente actual no lo invoca. UatContrapartidasController no es dependencia, puede ejecutar Proc_Contrapartidas vía SOAP y queda intacto con sus gates de ambiente y ACH_SOAP_LIVE_TESTS=true.
  - id: DEC-NAV-053
    text: Pruebas de servicios se activa sólo en TestToolsMenuAuthorizationSeeder después de reconciliar TestTools.Access como única autorización de menú; NavigationInformationArchitectureSeeder conserva el estado inicial IsActive=false para impedir publicación previa a la reconciliación de seguridad.
  - id: DEC-NAV-054
    text: 'NAV3.4A clasifica /ach-responses/manual-review como superficie mixta: búsqueda/listado/detalle son lectura, mientras BeginReview y ResolveOrphan son decisiones operativas que persisten cambios de estado y auditoría.'
  - id: DEC-NAV-055
    text: La entrada MenuItem y la ruta SPA de Revisión manual conservan CanReadAch para lectura; las acciones mutables de la SPA exigen Nacha.Generate OR CanManageAch, exactamente las alternativas aceptadas por P1.NachaGenerate en backend. CanReadAch solo no habilita mutaciones y no se agregan checks por rol.
  - id: DEC-NAV-056
    text: NAV3.4B confirma que /transactions/create persiste una transacción y estado interno relacionado; puede encolar trabajo interno y preparar dispatch pendiente, pero el endpoint de creación no ejecuta directamente SOAP, transmisión externa, jobs de contrapartida ni generación inmediata de archivos.
  - id: DEC-NAV-057
    text: La capacidad canónica de creación es P0.TransactionsCreate = Transactions.Create OR CanManageAch; CanReadAch no satisface la policy. El MenuItem y el guard hijo /transactions/create se alinean a esas alternativas sin cambiar el backend.
  - id: DEC-NAV-058
    text: Transactions.Create ya existía como permiso fino y policy backend antes de NAV3.4B. Se persiste idempotentemente en Permission sólo para permitir su referencia desde MenuItemPermission, sin RolePermissions, grants directos ni cambios de roles.
  - id: DEC-NAV-059
    text: NAV3.4C confirma que /transactions/nacha-upload es una capacidad mutable de ingesta: persiste el archivo/auditoría y el grafo NACHA-M parseado, ejecuta post-procesamiento y puede dejar trabajo durable para procesamiento asíncrono posterior.
  - id: DEC-NAV-060
    text: La capacidad canónica de carga es P1.NachaUpload = Nacha.Upload OR CanManageAch; CanReadAch no satisface la policy. El MenuItem y el guard hijo /transactions/nacha-upload se alinean a esas alternativas; GET NachaUpload/records conserva P1.NachaRead.
  - id: DEC-NAV-061
    text: Nacha.Upload ya existía como permiso fino y policy backend; NAV3.4C sólo lo persiste idempotentemente para MenuItemPermission, sin roles, RolePermissions ni grants nuevos. El padre /transactions permanece intacto y no sustituye la capacidad específica del hijo.
  - id: DEC-NAV-062
    text: NAV3.4D clasifica /transactions/returns como superficie mixta de lectura/preparación y generación; CanReadAch permite cargar la pantalla completa y la generación .RET exige Returns.GenerateFile OR CanManageAch con control SPA fail-closed.
  - id: DEC-NAV-063
    text: P0.ReturnsRead y P0.ReturnsGenerateFile ya separaban correctamente lectura y mutación; NAV3.4D no modifica backend y sólo materializa Returns.GenerateFile sin grants nuevos.
  - id: DEC-NAV-064
    text: /transactions/returns-ror permanece fuera del alcance y sin cambios en NAV3.4D.
  - id: DEC-NAV-065
    text: NAV3.4E clasifica /transactions/bulk-ingestion/tracking y su detalle contextual como superficie mixta modelo B: lectura/seguimiento en la entrada y retry/cancel como mutaciones protegidas individualmente.
  - id: DEC-NAV-066
    text: La capacidad canónica de lectura es P1.BulkIngestionRead = BulkIngestion.Read OR CanReadAch; retry usa P1.BulkIngestionRetry = BulkIngestion.Retry OR CanManageAch y cancel usa P1.BulkIngestionCancel = BulkIngestion.Cancel OR CanManageAch. El backend ya estaba correctamente separado y no se modifica.
  - id: DEC-NAV-067
    text: NAV3.4E alinea el MenuItem y los hijos SPA tracking/detail a BulkIngestion.Read OR CanReadAch, y aplica gating visual más validación fail-closed a retry/cancel. El padre /transactions conserva sus restricciones legacy como prerequisito adicional y no sustituye el permiso hijo.
  - id: DEC-NAV-068
    text: BulkIngestion.Read, BulkIngestion.Retry y BulkIngestion.Cancel eran capacidades preexistentes y NAV3.4E sólo las persiste idempotentemente en Permission, sin RolePermissions ni grants nuevos; BulkIngestion.Upload permanece fuera del incremento.
  - id: DEC-NAV-069
    text: NAV3.4G clasifica Registro de auditoría, Registro de accesos y la vista administrativa de Registro de navegación como superficies read-only sensibles. La escritura automática de telemetría de navegación es una responsabilidad distinta y conserva autenticación sin exigir el permiso administrativo de lectura.
  - id: DEC-NAV-070
    text: No existe una capacidad existente inequívoca que represente lectura general de registros del sistema. CanManageUsers no modela auditoría/accesos en general y CanReadAch no modela semánticamente telemetría de navegación; NAV3.4G no inventa SystemLogs.Read/AuditLogs.Read/AuthLogs.Read/NavigationLogs.Read ni cambia grants.
  - id: DEC-NAV-071
    text: Para evitar que un hijo autorizado quede reconstruido como raíz, /logs conserva su función estructural y su autorización directa se reconcilia a CanManageUsers OR CanReadAch, compatible con las capacidades actuales de sus tres hijos. GetMenuForCurrentUserHandler no se modifica.
  - id: DEC-NAV-072
    text: /navigation-logs alinea su guard SPA con el GET API vigente CanReadAch. El POST automático api/navigation-logs permanece autenticado e independiente. Auditoría y accesos no reciben una policy especulativa y su drift queda pendiente de un incremento de diseño de permiso.
  - id: DEC-NAV-073
    text: NAV3.4G2 adopta capacidades separadas de lectura siguiendo la convención Domain.Action: AuditLogs.Read, AuthLogs.Read y NavigationLogs.Read; no se usa un SystemLogs.Read común porque las audiencias legacy de Auditoría/Accesos y Navegación son distintas.
  - id: DEC-NAV-074
    text: Las policies de migración son alternativas OR, no requisitos AND: P1.AuditLogsRead = AuditLogs.Read OR CanManageUsers; P1.AuthLogsRead = AuthLogs.Read OR CanManageUsers; P1.NavigationLogsRead = NavigationLogs.Read OR CanReadAch.
  - id: DEC-NAV-075
    text: SystemRecordPermissionsSeeder persiste idempotentemente las tres capacidades con descripciones funcionales en español y no crea roles, RolePermissions, grants a Admin/ACH.Operator ni grants directos a usuarios.
  - id: DEC-NAV-076
    text: MenuItem, SPA y GET API quedan alineados por superficie con la capacidad dedicada OR su fallback legacy; un usuario sólo autenticado ya no puede consultar Auditoría ni Accesos por URL/API directa. /reports/audit permanece fuera del cambio.
  - id: DEC-NAV-077
    text: El padre estructural /logs acepta la unión AuditLogs.Read OR AuthLogs.Read OR NavigationLogs.Read OR CanManageUsers OR CanReadAch y no concede acceso a hijos; GetMenuForCurrentUserHandler permanece intacto.
  - id: DEC-NAV-078
    text: POST api/navigation-logs continúa separado de la consulta administrativa y conserva sólo la autenticación de controller; no exige NavigationLogs.Read ni CanReadAch. La población efectiva existente no cambia porque no se migran grants y se conservan los fallbacks legacy.
  - id: DEC-NAV-079
    text: NAV3.4H clasifica Organización del menú como una superficie administrativa sensible que modifica estructura/presentación y también asociaciones MenuItemRole/MenuItemPermission; no modifica RolePermissions, UserRoles ni grants de usuario.
  - id: DEC-NAV-080
    text: No existe una capacidad existente inequívoca para administrar la organización del menú; se crea NavigationMenu.Manage con descripción Administrar organización del menú y persistencia idempotente, sin grants a roles o usuarios.
  - id: DEC-NAV-081
    text: La policy final P1.NavigationMenuManage usa NavigationMenu.Manage OR CanManageUsers. El fallback CanManageUsers se conserva porque en el estado fuente canónico sólo Admin lo recibe y por tanto preserva la audiencia previa común de MenuItem/SPA/API sin ampliar ACH.Operator.
  - id: DEC-NAV-082
    text: Organización del menú elimina el gate MenuItemRole Admin y usa como alternativas MenuItemPermission NavigationMenu.Manage OR CanManageUsers; SPA /navigation y /navigation/menu-items usan las mismas alternativas y el API CRUD deja de depender del nombre de rol Admin para usar P1.NavigationMenuManage.
  - id: DEC-NAV-083
    text: P1.RolesRead y P1.PermissionsRead aceptan además NavigationMenu.Manage porque la pantalla necesita GET api/roles y GET api/permissions para editar asociaciones del menú; no se amplía la audiencia actual mientras NavigationMenu.Manage no tenga grants.
  - id: DEC-NAV-084
    text: No se encontró una vía directa para autoasignar permisos o roles desde Organización del menú: la pantalla sólo cambia visibilidad/asociaciones del MenuItem. Puede alterar qué enlaces se muestran globalmente, pero la autorización real de las rutas y APIs permanece independiente; cualquier gap downstream preexistente queda fuera de NAV3.4H.
  - id: DEC-NAV-085
    text: NAV3.5B agrega LegacyFineGrainedRoleMigrationSeeder Order 120 como migrador genérico data-driven sobre todos los RolePermission existentes; contiene 29 edges legacy→fine, 27 capabilities finas únicas, no depende del nombre del rol y excluye TestTools.Access.
  - id: DEC-NAV-086
    text: El modelo persistido de autorización no tiene UserPermission ni equivalente directo; los permission claims emitidos por AuthService se derivan de UserRole→RolePermission→Permission. Los JWT ya emitidos son snapshots y deben renovarse/expirar después del backfill antes de cualquier retiro de fallbacks.
  - id: DEC-NAV-087
    text: Los permisos legacy CanReadAch, CanManageAch, CanManageUsers y CanManageCertificates permanecen en el catálogo Permission. Las APIs first-party de Roles/Permissions inspeccionadas son de lectura para RolePermission, pero una base customizada o escritura out-of-band puede conservar/crear roles legacy-only; NAV3.5B migra el estado existente al ejecutarse y no elimina ni deshabilita el catálogo legacy.
  - id: DEC-NAV-088
    text: Al cierre de NAV3.5B CanReadAch, CanManageAch, CanManageUsers y CanManageCertificates quedan BLOCKED para retiro: además de runtime y renovación de sesión existen dependencias SPA/parent y usos legacy fuera del mapping NAV3.5B. No ejecutar NAV3.5C automáticamente.
  - id: DEC-NAV-4.2G-001
    text: El copy SPA-authored de feedback/estado puede humanizarse; backend/external passthrough no se altera y en HYBRID sólo puede cambiar el wrapper SPA preservando íntegro el fragmento dinámico externo.
  - id: DEC-NAV-4.2G-002
    text: Los empty states representan estados distintos y no se reemplazan por un único mensaje genérico; búsqueda, filtros, carga, configuración, historial, ausencia legítima, error y no aplicable conservan semánticas separadas.
  - id: DEC-NAV-4.2G-003
    text: NAV4.2G excluye mat-error y mensajes de validación; NAV4.2H queda reservado exclusivamente para mensajes de validación y no se ejecuta automáticamente.
  - id: DEC-NAV-4.2H-001
    text: Sólo los mensajes de validación SPA pueden humanizarse; backend/external validation passthrough es invariante y no se traduce, resume, corrige ni sustituye.
  - id: DEC-NAV-4.2H-002
    text: Humanizar copy de validación no modifica validators, error keys, condiciones, precedencia, touched/dirty/submitted behavior, bindings, regex ni límites.
  - id: DEC-NAV-4.2H-003
    text: Todo mensaje de validación describe únicamente restricciones demostrables por el validator real; no se inventan formatos, rangos, límites ni semántica.
  - id: DEC-NAV-5-001
    text: No toda ruta SPA pertenece al menú; cada ruta se clasifica MENU, CONTEXTUAL, TECHNICAL o REDIRECT según su función real, y UNREACHABLE_OR_DEAD/UNKNOWN sólo se usan cuando la evidencia lo exige.
  - id: DEC-NAV-5-002
    text: Una ruta MENU debe tener cobertura única y coherente en la IA; rutas contextuales, técnicas y redirects no se promueven artificialmente al menú.
  - id: DEC-NAV-5-003
    text: NAV5 no introduce permisos nuevos ni modifica el modelo de seguridad; cualquier gap que requiera rediseño de seguridad se bloquea y documenta.

known_current_facts:
  - DbInitializer ejecuta IDbSeeder por Order y luego por nombre completo del tipo con StringComparer.Ordinal.
  - NavigationInformationArchitectureSeeder está registrado por convención como IDbSeeder con Order 100; en NAV2.10A reconcilia Inicio y los ocho dominios funcionales restantes, y aplica la normalización estructural final de legacy/duplicados aprobada.
  - Todos los seeders funcionales de navegación existentes tienen Order entre 0 y 4 y por tanto preceden al reconciliador canónico.
  - GetMenuForCurrentUserHandler filtra por roles y permisos; desde NAV3.3A no contiene lógica de ambiente para /uat ni /uat/**.
  - NavigationService hoy sobrescribe integraciones con 'Centro de integración CFA', elimina sus hijos y filtra rutas legacy/retiradas.
  - MainLayout trata cualquier nodo con children sólo como expansor; su route no es navegable desde el padre.
  - Sólo existen roles base Admin y ACH.Operator en RoleConfiguration.
  - AchResponses list/dashboard/manual-review/audit y /ach/reconciliation ya tienen cobertura canónica en NAV2.5B; /navigation-logs queda publicado en NAV2.8C y otras rutas funcionales de dominios aún no implementados siguen pendientes.
  - La colisión raíz order 7 entre /navigation/menu-items y /reports queda eliminada en NAV2.7 al mover /reports a Order 70; las colisiones de orden raíz y /transactions caracterizadas en NAV1 ya no permanecen activas por la canonicalización NAV2.2-NAV2.7.
  - NAV2.10A fija /dashboard como Inicio Order 10; los nueve dominios raíz activos quedan exactamente en 10..90 y el árbol canonicalizado no conserva empates de Order entre hermanos activos.
  - /ach-cycles tiene una sola representación activa: MenuItemConfiguration.AchCyclesId como Ciclos de operación Order 30 bajo Administración de parámetros. Las filas duplicadas de esa ruta se conservan pero quedan inactivas desde NAV2.10A.
  - Catálogos permanece bajo Administración de parámetros Order 70 y desde NAV2.10B contiene exactamente Entidades financieras, Conceptos de lote, Tipos de documento, Tipos de persona, Tipos de género, Tipos de teléfono, Tipos de correo, Tipos de dirección y Códigos de transacción en órdenes 10..90.
  - Las ocho rutas de catálogo que carecían de MenuItem tienen guard SPA CanManageAch y API CRUD CanManageAch; sus nuevos MenuItem usan los roles existentes Admin/ACH.Operator + CanManageAch. Entidades financieras reutiliza su MenuItem y autorización preexistentes.
  - /catalogs/clearing-house-special-dates es un redirect SPA legacy hacia la configuración contextual por cámara; su MenuItem se conserva con autorización histórica pero queda IsActive=false desde NAV2.10B.
  - Operación reutiliza el MenuItem existente /transactions; las rutas /transactions/bulk-ingestion/upload, /transactions/bulk-ingestion/tracking y /transactions/returns-ror no tenían MenuItem en NAV2.2 y NAV2.3 las publica con la autorización SPA actual.
  - NAV2.4 crea /customers como raíz Clientes porque no existía MenuItem; la ruta SPA exige CanReadAch y la nueva entrada reutiliza ese permiso sin crear definiciones ni grants.
  - /customer-third-parties conserva su ruta, código y asociaciones de autorización legacy pero queda IsActive=false; /customers/:id/third-parties y /customers/:id/third-parties/:thirdPartyId siguen siendo rutas contextuales sin MenuItem.
  - /transactions/bulk-create permanece fuera del menú por decisión explícita; no se crea MenuItem.
  - Seguimiento y control usa /seguimiento-control como ruta estructural raíz Order 40; /transactions/outgoing-monitoring y /incoming-nacha-command-center ya no son hijos de Operación.
  - /incoming-nacha-command-center/observability, /queue y /orphan-resolution quedan publicados como MenuItem canónicos con la autorización vigente espejada desde /incoming-nacha-command-center; sus rutas SPA no cambian.
  - Respuestas de transacciones usa /seguimiento-control/respuestas-transacciones como agrupador estructural Order 60; /ach-responses es Bandeja de respuestas y sus hermanos dashboard, manual-review y audit usan órdenes 10..40.
  - /ach/reconciliation se publica como Conciliación Order 70 bajo Seguimiento y control; las cinco nuevas entradas funcionales NAV2.5B reutilizan Admin/ACH.Operator + CanReadAch al crearse.
  - /ach-responses/status-mappings permanece exclusivamente bajo Administración de parámetros; /ach-responses/:id y /ach-responses/:id/notification-attempts permanecen contextuales y sin MenuItem.
  - Seguridad de archivos reutiliza /nacha-security como raíz Order 60; Certificados digitales y Cifrar y descifrar archivos conservan /nacha-security/certificates y /nacha-security/sobre-digital con órdenes 10 y 20.
  - NachaSecurityMenuSeeder conserva la autorización existente: Certificados digitales usa CanManageCertificates y no tiene restricción de rol; Cifrar y descifrar archivos y la raíz no tienen enlaces MenuItemPermission/MenuItemRole directos. NAV2.6 no cambia esos enlaces.
  - /nacha-security/nacha/generate, /nacha-security/nacha/generate-encrypted, /nacha-security/digital-envelope/manual-encrypt y /nacha-security/digital-envelope/manual-decrypt permanecen como rutas SPA técnicas/contextuales sin MenuItem independiente.
  - ReportsMenuSeeder crea antes del reconciliador únicamente la raíz /reports con Admin/ACH.Operator + CanReadAch; NAV2.7 publica como hijos /reports/sent, /received, /returns, /rejections, /files, /cycles, /reconciliation, /traceability, /history y /audit con órdenes 10..100.
  - /reports/audit es una superficie de reporte con endpoint api/reports/audit y exportación PDF; comparte el origen AuditLogs con /audit-logs, pero la superficie administrativa /audit-logs mantiene un contrato UI/API distinto y NAV2.7 no la mueve.
  - El SPA declara /users/list, /users/branding, /users/password-rules y /users/login-lockout bajo el guard Admin + CanManageUsers; /users/new, /users/:id/edit y /users/:id/roles son rutas contextuales del flujo y no tienen MenuItem; no existe una ruta independiente de detalle de usuario.
  - NAV2.8A reutiliza el MenuItem /users como agrupador Usuarios y acceso bajo el nuevo agrupador estructural /administracion; no crea MenuItem funcional nuevo para las cuatro opciones soportadas.
  - El SPA sólo declara `scheduler/tasks` como pantalla funcional del programador; `/scheduler` es un redirect a `tasks`. Las capacidades de historial, instancias, detalle técnico, ejecución manual, pausa/reanudación y edición de programación viven en la misma pantalla/API y no son rutas SPA de menú independientes.
  - NAV2.8B reutiliza MenuItemConfiguration.TaskDefinitionsId (`/scheduler/tasks`) como hijo directo de Administración Order 20 y desactiva sólo el agrupador de menú legacy MenuItemConfiguration.SchedulerId (`/scheduler`); ambos conservan sus enlaces Scheduler.View preexistentes.
  - NAV2.8C reutiliza MenuItemConfiguration.LogsId (`/logs`) como Registros del sistema Order 30 y conserva Auditoría/Accesos/Navegación en órdenes 10/20/30. NAV3.4G2 reconcilia los hijos a sus capacidades dedicadas con fallback legacy y el padre estructural a la unión de las cinco alternativas, sin cambiar jerarquía ni algoritmo de reconstrucción.
  - /navigation-logs usa NavigationLogs.Read OR CanReadAch en MenuItem/SPA/GET API; el POST automático de telemetría permanece autenticado e independiente de ambas capacidades de consulta.
  - /audit-logs usa AuditLogs.Read OR CanManageUsers y /auth-logs usa AuthLogs.Read OR CanManageUsers de forma coherente en MenuItem/SPA/GET API. Un principal sólo autenticado ya no satisface esas consultas.
  - /reports/audit permanece bajo Reportes como superficie de reporte distinta de /audit-logs; /log y /logs siguen siendo redirects SPA y no se publican como opciones adicionales.
  - NAV2.8D reutiliza NavigationAdminId (`/navigation/menu-items`) como Organización del menú Order 40 bajo Administración. NAV3.4H conserva Label/Route/ParentId/Order/IsActive y reconcilia su autorización a NavigationMenu.Manage OR CanManageUsers, sin MenuItemRole directo.
  - El SPA monta `/navigation` como prefijo estructural y conserva el redirect raíz a `menu-items`; desde NAV3.4H tanto `/navigation` como `/navigation/menu-items` exigen NavigationMenu.Manage OR CanManageUsers. El CRUD API `navigation/menu-items` y `api/navigation/menu-items` usa P1.NavigationMenuManage con las mismas alternativas.
  - Organización del menú puede listar jerarquía, crear/eliminar opciones y editar Label, Route, Icon, ParentId, Order, Exact, IsActive, RoleIds y PermissionIds. No existe endpoint reorder/PATCH separado; reordenar/activar/desactivar se persiste mediante PUT. No modifica RolePermissions ni UserRoles.
  - En el seed fuente vigente sólo Admin tiene CanManageUsers y ACH.Operator no lo tiene. NavigationMenu.Manage se crea sin grants, por lo que NAV3.4H conserva la audiencia canónica previa y no entrega acceso nuevo a ACH.Operator.
  - `/payment-rail-capability-registry` es una superficie administrativa read-only de gobernanza multi-riel. El SPA exige Admin/ACH.Operator + ANY de CanViewPaymentRailCapabilityRegistry/CanManageAch/CanReadAch; el controller nombra la policy CanViewPaymentRailCapabilityRegistry, pero la implementación efectiva de esa policy acepta las mismas tres claims. NAV2.8E la publica con CanReadAch + Admin/ACH.Operator sin ampliar acceso.
  - CanViewPaymentRailCapabilityRegistry no existe como Permission persistida en PermissionConfiguration ni en los grants seed actuales; la decisión de sembrar/endurecer un permiso dedicado queda para la fase de seguridad.
  - Las dos superficies SPA publicadas de Herramientas de prueba son /uat/nacha-inbound-simulator y /ach/nacha/soap-uat-console; ambas exigen TestTools.Access y ya no dependen del nombre de ambiente.
  - /uat/nacha-inbound-simulator continúa generate-only, sin autoimportación/transmisión automática y con sus permisos específicos como requisitos adicionales cuando corresponden.
  - /ach/nacha/soap-uat-console es una ruta SPA exacta e independiente declarada antes del padre /ach: exige sólo TestTools.Access y no hereda Admin/ACH.Operator ni CanReadAch/CanManageAch. Las demás rutas /ach/** conservan sus guardas previas.
  - NachaSoapUatConsoleController es GET-only, exige TestTools.Access a nivel de clase y ya no contiene gates Production/IsUatLike ni la policy NachaSimulatorRead; su servicio/read store sólo consulta estado persistido y no ejecuta SOAP o mutaciones.
  - El componente de Pruebas de servicios carga dashboard, candidates y audit desde api/ach/nacha/soap-uat-console; el endpoint candidate detail existe pero el componente actual selecciona localmente y no lo invoca.
  - UatContrapartidasController no es dependencia de Pruebas de servicios; permanece intacto como superficie potencialmente live con POST dispatch-cycle, CanManageAch, bloqueo Production, allowlist Development/Testing/UAT y ACH_SOAP_LIVE_TESTS=true.
  - TestToolsMenuAuthorizationSeeder deja TestTools.Access como único permiso de menú y activa Pruebas de servicios sólo después de esa reconciliación; el seeder canónico NAV2 mantiene su valor inicial IsActive=false por seguridad.
  - Admin conserva el único grant actual TestTools.Access; ACH.Operator continúa sin el permiso y no se crearon roles/grants nuevos en NAV3.3B.
  - /ach-responses/manual-review usa GET api/ach/responses y GET api/ach/responses/orphans para consulta, y POST review/start + POST resolve para decisiones manuales; no invoca reproceso, process, notificación, SOAP ni transmisión externa.
  - El backend ya separa lectura y mutación: P1.NachaRead acepta Nacha.Read/CanReadAch y P1.NachaGenerate acepta Nacha.Generate/CanManageAch sin CanReadAch. NAV3.4A conserva CanReadAch en menú/ruta y agrega el gate de gestión sólo a los controles/métodos mutables de la SPA.
  - /transactions/create usa consultas auxiliares de lectura y finalmente POST api/transactions; el controller ya exige P0.TransactionsCreate = Transactions.Create OR CanManageAch y rechaza CanReadAch como capacidad de creación.
  - NAV3.4B elimina CanReadAch de la autorización final del MenuItem Crear transacción mediante reconciliación Order 102 y mantiene únicamente Transactions.Create OR CanManageAch; Consultar transacciones conserva su autorización de lectura.
  - La ruta SPA hija /transactions/create exige Transactions.Create OR CanManageAch. El padre /transactions conserva sus guardas legacy existentes; CanReadAch puede habilitar las lecturas auxiliares, pero por sí solo no satisface el guard hijo de creación.
  - Transactions.Create se persiste en el catálogo Permission sin RolePermission asociado. Admin y ACH.Operator no reciben grants nuevos en NAV3.4B y continúan accediendo a creación por su CanManageAch existente.
  - NAV3.4E confirma que Seguimiento de cargas usa GET batch/items/summary bajo P1.BulkIngestionRead y expone POST retry/cancel bajo policies mutables separadas; no existe una operación de descarga en la UI inspeccionada.
  - El retry de carga masiva reencola procesamiento asíncrono que puede registrar transacciones y preparar trabajo downstream; el POST retry no transmite directamente por SOAP. Cancel persiste el estado Cancelled pero no demuestra unscheduling físico de un job ya ejecutándose.
  - El MenuItem de Seguimiento de cargas y las rutas hijas tracking/detail aceptan BulkIngestion.Read OR CanReadAch; los controles retry/cancel requieren BulkIngestion.Retry/Cancel OR CanManageAch respectivamente. El padre /transactions sigue siendo un prerequisito legacy adicional.

phase_status:
  NAV1: CLOSED
  NAV2: IMPLEMENTED_UNVERIFIED_ENVIRONMENT
  NAV2.1: IMPLEMENTED_UNVERIFIED_ENVIRONMENT
  NAV2.2: IMPLEMENTED_UNVERIFIED_ENVIRONMENT
  NAV2.3: IMPLEMENTED_UNVERIFIED_ENVIRONMENT
  NAV2.4: IMPLEMENTED_UNVERIFIED_ENVIRONMENT
  NAV2.5: IMPLEMENTED_UNVERIFIED_ENVIRONMENT
  NAV2.5A: IMPLEMENTED_UNVERIFIED_ENVIRONMENT
  NAV2.5B: IMPLEMENTED_UNVERIFIED_ENVIRONMENT
  NAV2.6: IMPLEMENTED_UNVERIFIED_ENVIRONMENT
  NAV2.7: IMPLEMENTED_UNVERIFIED_ENVIRONMENT
  NAV2.8: IMPLEMENTED_UNVERIFIED_ENVIRONMENT
  NAV2.8A: IMPLEMENTED_UNVERIFIED_ENVIRONMENT
  NAV2.8B: IMPLEMENTED_UNVERIFIED_ENVIRONMENT
  NAV2.8C: IMPLEMENTED_UNVERIFIED_ENVIRONMENT
  NAV2.8D: IMPLEMENTED_UNVERIFIED_ENVIRONMENT
  NAV2.8E: IMPLEMENTED_UNVERIFIED_ENVIRONMENT
  NAV2.9: IMPLEMENTED_UNVERIFIED_ENVIRONMENT
  NAV2.10A: IMPLEMENTED_UNVERIFIED_ENVIRONMENT
  NAV2.10B: IMPLEMENTED_UNVERIFIED_ENVIRONMENT
  NAV3: IMPLEMENTED_UNVERIFIED_ENVIRONMENT
  NAV3.1: IMPLEMENTED_UNVERIFIED_ENVIRONMENT
  NAV3.2A: IMPLEMENTED_UNVERIFIED_ENVIRONMENT
  NAV3.2B: IMPLEMENTED_UNVERIFIED_ENVIRONMENT
  NAV3.3A: CLOSED
  NAV3.3B: CLOSED
  NAV3.4A: IMPLEMENTED_UNVERIFIED_ENVIRONMENT
  NAV3.4B: IMPLEMENTED_UNVERIFIED_ENVIRONMENT
  NAV3.4C: IMPLEMENTED_UNVERIFIED_ENVIRONMENT
  NAV3.4D: IMPLEMENTED_UNVERIFIED_ENVIRONMENT
  NAV3.4E: IMPLEMENTED_UNVERIFIED_ENVIRONMENT
  NAV3.4F: IMPLEMENTED_UNVERIFIED_ENVIRONMENT
  NAV3.4G: IMPLEMENTED_UNVERIFIED_ENVIRONMENT
  NAV3.4G2: IMPLEMENTED_UNVERIFIED_ENVIRONMENT
  NAV3.4H: IMPLEMENTED_UNVERIFIED_ENVIRONMENT
  NAV3.4I: IMPLEMENTED_UNVERIFIED_ENVIRONMENT
  NAV3.4J: IMPLEMENTED_UNVERIFIED_ENVIRONMENT
  NAV3.4K: IMPLEMENTED_UNVERIFIED_ENVIRONMENT
  NAV3.4L: IMPLEMENTED_UNVERIFIED_ENVIRONMENT
  NAV3.4M: IMPLEMENTED_UNVERIFIED_ENVIRONMENT
  NAV3.4N: IMPLEMENTED_UNVERIFIED_ENVIRONMENT
  NAV3.4O: IMPLEMENTED_UNVERIFIED_ENVIRONMENT
  NAV3.4P: IMPLEMENTED_UNVERIFIED_ENVIRONMENT
  NAV3.4Q: IMPLEMENTED_UNVERIFIED_ENVIRONMENT
  NAV3.4R: IMPLEMENTED_UNVERIFIED_ENVIRONMENT
  NAV3.5A: IMPLEMENTED_UNVERIFIED_ENVIRONMENT
  NAV3.5B: IMPLEMENTED_UNVERIFIED_ENVIRONMENT
  NAV3.5C: "BLOCKED / NO EJECUTAR"
  NAV4: IMPLEMENTED_UNVERIFIED_ENVIRONMENT
  NAV4.1A: IMPLEMENTED_UNVERIFIED_ENVIRONMENT
  NAV4.1B: IMPLEMENTED_UNVERIFIED_ENVIRONMENT
  NAV4.2A: IMPLEMENTED_UNVERIFIED_ENVIRONMENT
  NAV4.2B: IMPLEMENTED_UNVERIFIED_ENVIRONMENT
  NAV4.2C: IMPLEMENTED_UNVERIFIED_ENVIRONMENT
  NAV4.2D: IMPLEMENTED_UNVERIFIED_ENVIRONMENT
  NAV4.2E: IMPLEMENTED_UNVERIFIED_ENVIRONMENT
  NAV4.2F: IMPLEMENTED_UNVERIFIED_ENVIRONMENT
  NAV4.2G: IMPLEMENTED_UNVERIFIED_ENVIRONMENT
  NAV4.2H: IMPLEMENTED_UNVERIFIED_ENVIRONMENT
  NAV4.2I: IMPLEMENTED_UNVERIFIED_ENVIRONMENT
  NAV4.2J: IMPLEMENTED_UNVERIFIED_ENVIRONMENT
  NAV5: IMPLEMENTED_UNVERIFIED_ENVIRONMENT
  NAV6: IMPLEMENTED_UNVERIFIED_ENVIRONMENT

evidence:
  - docs/ai/navigation-menu/phases/NAV4_2H_VALIDATION_MESSAGES_INVENTORY.md
  - docs/ai/navigation-menu/phases/NAV4_2H_COPY_DELTA.json
  - scripts/validate-nav4-2h-validation-copy.py
  - docs/ai/navigation-menu/phases/PHASE_04_NAV4_SPA_COPY.md
  - docs/ai/navigation-menu/NAV1_BASELINE_CURRENT_STATE.md
  - docs/architecture/NAVIGATION_MENU_INFORMATION_ARCHITECTURE.md
  - docs/architecture/NAVIGATION_MENU_IMPLEMENTATION_PLAN.md
  - docs/architecture/NAVIGATION_MENU_SEED_STRATEGY.md
  - docs/ux/NAVIGATION_MENU_ROUTE_MATRIX.md
  - docs/security/NAVIGATION_MENU_SECURITY_MODEL.md
  - docs/testing/NAVIGATION_MENU_TEST_STRATEGY.md
  - src/Cfa.ACHInterbank.Persistence/DataBase/DbSeederExecutionOrder.cs
  - src/Cfa.ACHInterbank.Persistence/Navigation/Seeders/NavigationInformationArchitectureSeeder.cs
  - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav1BaselineTests.cs
  - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav21SeedInfrastructureTests.cs
  - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav22ParametersInformationArchitectureTests.cs
  - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav23OperationInformationArchitectureTests.cs
  - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav24CustomersInformationArchitectureTests.cs
  - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav25AMonitoringInformationArchitectureTests.cs
  - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav25BResponsesReconciliationInformationArchitectureTests.cs
  - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav26FileSecurityInformationArchitectureTests.cs
  - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav27ReportsInformationArchitectureTests.cs
  - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav28AAdministrationUsersAccessInformationArchitectureTests.cs
  - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav28BAdministrationScheduledTasksInformationArchitectureTests.cs
  - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav28CAdministrationSystemRecordsInformationArchitectureTests.cs
  - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav28DAdministrationMenuOrganizationInformationArchitectureTests.cs
  - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav34HMenuOrganizationAuthorizationTests.cs
  - src/Cfa.ACHInterbank.Persistence/Security/Seeders/NavigationMenuPermissionSeeder.cs
  - src/Cfa.ACHInterbank.Persistence/Security/Seeders/NavigationMenuAuthorizationSeeder.cs
  - src/Cfa.ACHInterbank.Api/Controllers/MenuItemsController.cs
  - web/ach-interbank-ui/src/app/features/navigation/components/navigation-menu.component.ts
  - web/ach-interbank-ui/src/app/features/navigation/navigation-routing.module.ts
  - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav28EAdministrationPlatformCapabilitiesInformationArchitectureTests.cs
  - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav29TestToolsInformationArchitectureTests.cs
  - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav210AStructuralNormalizationTests.cs
  - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav210BCatalogsCoverageTests.cs
  - src/Cfa.ACHInterbank.Application/Security/FineGrainedPermissions.cs
  - src/Cfa.ACHInterbank.Persistence/Security/Seeders/TestToolsPermissionSeeder.cs
  - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav31TestToolsPermissionFoundationTests.cs
  - src/Cfa.ACHInterbank.Persistence/Security/Seeders/TestToolsAdminRolePermissionSeeder.cs
  - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav32ATestToolsAdminRoleGrantTests.cs
  - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav32BTestToolsPermissionEnforcementTests.cs
  - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav33AInboundSimulatorProductionAuthorizationTests.cs
  - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav33BServiceTestsProductionAuthorizationTests.cs
  - web/ach-interbank-ui/src/app/app-routing-access.spec.ts
  - web/ach-interbank-ui/src/app/layout/main-layout.component.spec.ts

nav1_execution:
  phase_executed: NAV1
  phase_status: CLOSED
  files_modified:
    - MEMORY.md
    - docs/ai/navigation-menu/MEMORY_STATE.md
    - docs/ai/navigation-menu/NAV1_BASELINE_CURRENT_STATE.md
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav1BaselineTests.cs
    - web/ach-interbank-ui/src/app/layout/main-layout.component.spec.ts
  tests_executed:
    - command: python scripts/validate-solution-projects.py
      result: PASS
    - command: Comparación SHA-256 por archivo entre el ZIP full de entrada y el workspace NAV1
      result: PASS; 2 archivos agregados, 3 modificados, 0 eliminados; todos dentro del alcance NAV1.
  tests_passed:
    - python scripts/validate-solution-projects.py
    - comparación de contenido ZIP/workspace sin cambios fuera de los cinco archivos declarados
  tests_failed: []
  tests_deferred:
    - command: dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj --filter FullyQualifiedName~NavigationMenuNav1BaselineTests
      reason: NOT_RUN_ENVIRONMENT; dotnet no está instalado en el host.
    - command: Angular focal specs de NavigationService, MainLayout, app-routing-access y transactions-routing
      reason: NOT_RUN_ENVIRONMENT; node_modules no venía en el ZIP y npm ci no completó dentro del timeout del entorno, por lo que no se ejecutó ng test.
  decisions_taken:
    - NAV1 se limitó a documentación y guardas de caracterización; no se cambió comportamiento productivo.
    - El inventario efectivo se derivó del código real combinando MenuItemConfiguration con los seeders runtime; la documentación objetivo se usó sólo para registrar diferencias.
    - Los empates de orden y la ruta duplicada /ach-cycles se congelaron como baseline conocido, no se corrigieron.
    - NAV2 quedó técnicamente elegible, pero no se ejecutó sin una nueva instrucción del usuario.
  relevant_findings:
    - DbInitializer sólo usaba OrderBy(Order); seeders con igual Order no tenían desempate explícito.
    - Empates actuales de hermanos: raíz order 2 (/users, /nacha-config-admin), raíz order 5 (/integraciones, /catalogs), raíz order 7 (/navigation/menu-items, /reports), y /transactions order 9 (/ach-claims, /administracion/mft-ach).
    - /ach-cycles está activo tanto como raíz como hijo 'Catálogo de ciclos' con la misma ruta.
    - Angular reescribe Integraciones como 'Centro de integración CFA', aplana sus hijos y filtra al menos la ruta sembrada /catalogs/clearing-house-special-dates.
    - MainLayout no permite navegar directamente a la route de un padre que tiene children.
    - Hay diferencias entre autorización de menú y guardas SPA en Crear transacción, MFT e Integraciones.
    - Hay rutas funcionales sin acceso canónico desde menú, detalladas en NAV1_BASELINE_CURRENT_STATE.md.
  gaps_pending:
    - Normalizar órdenes únicos del menú y resolver jerarquía/labels en un incremento posterior autorizado.
    - Resolver la duplicidad /ach-cycles y el patrón de padres con route + children sin perder acceso funcional.
    - Eliminar o acotar overrides de NavigationService para cumplir seed/backend como fuente de verdad.
    - Alinear visibilidad de menú con guardas SPA y permisos finos.
    - Cubrir rutas funcionales faltantes desde la arquitectura de menú aprobada.
    - Completar la matriz de pruebas del gate UAT y ejecutar las pruebas focales .NET/Angular en un entorno con toolchain disponible.
  risks_or_blockers:
    - No hay bloqueo técnico de diseño para abrir NAV2.
    - La ejecución runtime de las nuevas guardas backend quedó diferida por ausencia de dotnet.
    - La ejecución focal Angular quedó diferida por imposibilidad de completar npm ci dentro del entorno NAV1.
  next_increment_recommendation: NAV2 era técnicamente elegible y requería nueva instrucción del usuario.

nav2_1_execution:
  phase_executed: NAV2.1
  phase_status: IMPLEMENTED_UNVERIFIED_ENVIRONMENT
  files_modified:
    - MEMORY.md
    - docs/ai/navigation-menu/MEMORY_STATE.md
    - src/Cfa.ACHInterbank.Persistence/DataBase/DbInitializer.cs
    - src/Cfa.ACHInterbank.Persistence/DataBase/DbSeederExecutionOrder.cs
    - src/Cfa.ACHInterbank.Persistence/Navigation/Seeders/NavigationInformationArchitectureSeeder.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav1BaselineTests.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav21SeedInfrastructureTests.cs
  tests_executed:
    - command: python scripts/validate-solution-projects.py
      result: PASS
    - command: NAV2.1 static focal assertions sobre DbInitializer, desempate ordinal, seeder canónico Order 100, registro convencional y ausencia de cambios en los 11 seeders funcionales de navegación
      result: PASS
    - command: Auditoría estática de todos los IDbSeeder productivos
      result: PASS; 32 seeders, máximo previo al reconciliador 11, único seeder con Order 100 es NavigationInformationArchitectureSeeder.
    - command: Comparación SHA-256 por archivo entre el ZIP full NAV1 y el workspace NAV2.1
      result: PASS; 3 archivos agregados, 4 modificados, 0 eliminados; todos dentro del alcance NAV2.1.
  tests_passed:
    - python scripts/validate-solution-projects.py
    - NAV2.1 static focal assertions
    - auditoría estática de órdenes de IDbSeeder
    - comparación de contenido ZIP/workspace sin cambios fuera de los siete archivos declarados
  tests_failed: []
  tests_deferred:
    - command: dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj --filter FullyQualifiedName~NavigationMenuNav21SeedInfrastructureTests|FullyQualifiedName~NavigationMenuNav1BaselineTests
      reason: NOT_RUN_ENVIRONMENT; dotnet no está instalado en el host.
  decisions_taken:
    - El desempate global de seeders usa el nombre completo del tipo y StringComparer.Ordinal después de Order; no depende del orden de registro de DI.
    - El reconciliador canónico se introduce ahora como hook final Order 100, pero SeedAsync devuelve Task.CompletedTask en NAV2.1.
    - Los tests NAV1 de caracterización ahora ejecutan el conjunto de seeders mediante el mismo orden determinístico y añaden el reconciliador no-op; sus expectativas funcionales permanecen sin cambio.
    - No se tocó ningún seeder funcional de navegación existente ni código Angular.
  relevant_findings:
    - Los 11 seeders funcionales de navegación actuales usan Order 0..4; Order 100 deja una separación explícita para el reconciliador final.
    - El máximo Order observado entre todos los IDbSeeder productivos actuales antes de NAV2.1 es 11; el reconciliador canónico queda también después del pipeline productivo vigente completo.
    - El cambio de orden sólo formaliza un criterio secundario determinístico; los gaps del árbol de menú caracterizados en NAV1 permanecen intactos.
  gaps_pending:
    - GAP-NAV-001 queda parcialmente cerrado: el orden relativo de seeders empatados ya es determinístico; los empates de MenuItem.Order siguen abiertos y no se tocaron.
    - La duplicidad /ach-cycles permanece abierta por alcance explícito.
    - El override CFA de Angular permanece abierto por alcance explícito.
    - Permisos/roles y cobertura de rutas permanecen abiertos por alcance explícito.
    - El reconciliador canónico todavía no aplica árbol, labels, ParentId, visibilidad ni órdenes de menú; eso corresponde al siguiente incremento autorizado.
  risks_or_blockers:
    - No se pudo ejecutar la prueba .NET focal ni la caracterización backend porque el host no tiene dotnet; por ello NAV2.1 queda IMPLEMENTED_UNVERIFIED_ENVIRONMENT y no CLOSED.
    - No hay cambio frontend en NAV2.1; no se ejecutaron pruebas Angular por no ser parte del alcance modificado.
    - Un futuro IDbSeeder con Order mayor o igual a 100 requerirá revisar la reserva del reconciliador final; hoy ningún seeder productivo existente supera Order 11.
  next_increment_recommendation: NAV2.2 es técnicamente elegible para implementar la reconciliación funcional del árbol dentro de NavigationInformationArchitectureSeeder, pero no debe ejecutarse sin nueva instrucción del usuario.

nav2_2_execution:
  phase_executed: NAV2.2
  phase_status: IMPLEMENTED_UNVERIFIED_ENVIRONMENT
  files_modified:
    - MEMORY.md
    - docs/ai/navigation-menu/MEMORY_STATE.md
    - docs/architecture/NAVIGATION_MENU_INFORMATION_ARCHITECTURE.md
    - docs/ux/NAVIGATION_MENU_ROUTE_MATRIX.md
    - src/Cfa.ACHInterbank.Persistence/Navigation/Seeders/NavigationInformationArchitectureSeeder.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav1BaselineTests.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav21SeedInfrastructureTests.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav22ParametersInformationArchitectureTests.cs
  tests_executed:
    - command: python scripts/validate-solution-projects.py
      result: PASS
    - command: NAV2.2 static focal assertions sobre rutas, labels, órdenes 10..90 y no mutación de rutas existentes en ReconcileExistingAsync
      result: PASS
    - command: C# delimiter sanity sobre archivos focales modificados
      result: PASS
    - command: Comparación SHA-256 por archivo entre el ZIP full NAV2.1 y el workspace NAV2.2
      result: PASS; 1 archivo agregado, 7 modificados, 0 eliminados; todos dentro del alcance NAV2.2.
  tests_passed:
    - python scripts/validate-solution-projects.py
    - NAV2.2 static focal assertions
    - C# delimiter sanity
    - auditoría estática de cobertura de rutas MENU* de la matriz objetivo
    - comparación de contenido ZIP/workspace dentro del alcance declarado
    - integridad del ZIP FULL contra el workspace final
  tests_failed: []
  tests_deferred:
    - command: dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj --filter FullyQualifiedName~NavigationMenuNav22ParametersInformationArchitectureTests|FullyQualifiedName~NavigationMenuNav21SeedInfrastructureTests|FullyQualifiedName~NavigationMenuNav1BaselineTests
      reason: NOT_RUN_ENVIRONMENT; dotnet no está instalado en el host.
  decisions_taken:
    - El reconciliador canónico usa /administracion-parametros como ruta estructural del nuevo agrupador raíz y Order 50 para no reordenar todavía los demás dominios raíz.
    - Los nueve hijos inmediatos de Administración de parámetros quedan en órdenes 10..90 y conservan sus rutas SPA existentes.
    - Conexiones reutiliza /integraciones como agrupador y publica /integraciones/contrato, /integraciones/relacion-informacion y /integraciones/conexion; /administracion/mft-ach se reubica como cuarto hijo sin cambiar su ruta ni sus enlaces de autorización existentes.
    - El ítem técnico legacy /soap-integrations se conserva con la misma ruta y autorización pero queda inactivo porque las rutas funcionales de Conexiones ya son canónicas.
    - Homologación de estados publica la ruta SPA existente /ach-responses/status-mappings reutilizando los identificadores de autorización ya exigidos por esa ruta (Admin/ACH.Operator + CanReadAch); no se crean permisos, roles ni grants nuevos.
    - No se modificó Angular, no se corrigió la duplicidad /ach-cycles y no se reorganizó ningún dominio distinto de Administración de parámetros.
  relevant_findings:
    - El código SPA confirma de forma inequívoca las rutas /integraciones/contrato, /integraciones/relacion-informacion, /integraciones/conexion y /ach-responses/status-mappings usadas por la matriz de navegación.
    - La documentación previa decía 'Prioridades de procesamiento'; la instrucción NAV2.2 aprueba 'Prioridades por cámara' y la arquitectura/matriz se alinearon con ese copy.
    - NavigationService seguirá sobrescribiendo /integraciones como 'Centro de integración CFA' y descartando sus hijos hasta el incremento frontend autorizado; el seed canónico ya queda preparado pero la representación SPA todavía no reflejará Conexiones correctamente.
    - MainLayout seguirá tratando cualquier padre con hijos como expansor; la ruta estructural /administracion-parametros no tiene pantalla SPA propia y no se usa como destino mientras existan hijos visibles.
  gaps_pending:
    - Resolver en un incremento posterior autorizado la duplicidad activa /ach-cycles; NAV2.2 la conserva por instrucción explícita.
    - Retirar/acotar el override CFA de NavigationService sólo cuando corresponda; NAV2.2 no modifica Angular.
    - Revisar permisos/roles y drift de visibilidad en fases de seguridad posteriores; NAV2.2 no altera asociaciones existentes de MenuItem y sólo reutiliza autorización actual para nuevos ítems publicados.
    - Mantener diferida la cobertura general de rutas: no se agregaron los catálogos aún ausentes del menú ni otros dominios fuera de Administración de parámetros.
    - Validar runtime/idempotencia con .NET cuando el SDK esté disponible.
  risks_or_blockers:
    - La prueba .NET focal no pudo ejecutarse por ausencia de dotnet; el estado queda IMPLEMENTED_UNVERIFIED_ENVIRONMENT.
    - El override actual de Angular impide observar en UI la jerarquía canónica de Conexiones aunque el backend quede reconciliado.
    - El agrupador /administracion-parametros es estructural y no tiene ruta SPA propia; con el layout actual un padre con hijos no navega, pero si todos sus hijos fueran filtrados por autorización podría quedar como enlace sin destino funcional.
  next_increment_recommendation: NAV2.3 es técnicamente elegible para continuar el programa sólo con una nueva instrucción explícita; NAV2.2 no anticipa qué dominio o gap deberá atender.

nav2_3_execution:
  phase_executed: NAV2.3
  phase_status: IMPLEMENTED_UNVERIFIED_ENVIRONMENT
  files_modified:
    - MEMORY.md
    - docs/ai/navigation-menu/MEMORY_STATE.md
    - src/Cfa.ACHInterbank.Persistence/Navigation/Seeders/NavigationInformationArchitectureSeeder.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav22ParametersInformationArchitectureTests.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav23OperationInformationArchitectureTests.cs
  tests_executed:
    - command: python scripts/validate-solution-projects.py
      result: PASS
    - command: NAV2.3 static focal assertions sobre raíz Operación, diez rutas objetivo, órdenes 10..100, exclusión de /transactions/bulk-create y autorización de las tres entradas nuevas
      result: PASS
    - command: C# delimiter sanity sobre seeder y pruebas focales modificadas
      result: PASS
    - command: Comparación SHA-256 por archivo entre el ZIP full NAV2.2 y el workspace NAV2.3
      result: PASS; 1 archivo agregado, 4 modificados, 0 eliminados; todos dentro del alcance NAV2.3.
  tests_passed:
    - python scripts/validate-solution-projects.py
    - NAV2.3 static focal assertions
    - C# delimiter sanity
    - auditoría estática de cobertura de rutas MENU* de la matriz objetivo
    - comparación de contenido ZIP/workspace dentro del alcance declarado
  tests_failed: []
  tests_deferred:
    - command: dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj --filter FullyQualifiedName~NavigationMenuNav23OperationInformationArchitectureTests|FullyQualifiedName~NavigationMenuNav22ParametersInformationArchitectureTests|FullyQualifiedName~NavigationMenuNav21SeedInfrastructureTests
      reason: NOT_RUN_ENVIRONMENT; dotnet no está instalado en el host.
  decisions_taken:
    - /transactions se reutiliza como raíz canónica Operación, conservando su ruta y sus asociaciones de autorización existentes.
    - Los diez hijos objetivo de Operación se ordenan 10..100; se conservan todas sus rutas funcionales actuales.
    - Se crean MenuItem sólo para /transactions/bulk-ingestion/upload, /transactions/bulk-ingestion/tracking y /transactions/returns-ror porque las rutas SPA y sus guardas actuales son inequívocas; cada entrada nueva recibe Admin/ACH.Operator + CanManageAch, sin crear ni modificar definiciones de permisos, roles o grants.
    - /transactions/bulk-create no se publica.
    - No se modifica Angular, no se corrige el override CFA, no se resuelve la duplicidad /ach-cycles y no se reorganizan Clientes, Seguimiento y control, Seguridad, Reportes, Administración ni Herramientas de prueba.
    - Los hijos legacy actualmente bajo /transactions que pertenecen a incrementos futuros se dejan en su estado actual; no se reubican ni desactivan en NAV2.3.
  relevant_findings:
    - La SPA protege carga por archivo, seguimiento de cargas y devolución de una devolución con CanManageAch bajo el padre /transactions, que exige rol Admin o ACH.Operator.
    - El API de carga masiva usa políticas P1: upload acepta BulkIngestion.Upload o CanManageAch y lectura acepta BulkIngestion.Read o CanReadAch; la SPA de seguimiento sigue siendo más restrictiva porque exige CanManageAch. NAV2.3 refleja la guarda SPA vigente y deja la alineación fina para una fase de seguridad/cobertura posterior.
    - /ach-claims ya existe con Claims.Read; /ach-colombia/file-exchange ya existe con Admin/ACH.Operator + CanReadAch; /ach-cycles/nacha/export ya existe con CanReadAch. Sus asociaciones no se modifican.
    - /transactions/nacha-upload y /transactions/returns continúan sin autorización directa de MenuItem aunque sus rutas SPA exigen CanManageAch; NAV2.3 no corrige ese drift por alcance explícito.
    - Los hijos /customer-third-parties, /transactions/outgoing-monitoring e /incoming-nacha-command-center permanecen temporalmente bajo el MenuItem /transactions ahora rotulado Operación, porque moverlos correspondería a Clientes/Seguimiento y control y está fuera de NAV2.3.
  gaps_pending:
    - Reubicar en incrementos autorizados posteriores los hijos legacy de /transactions que conceptualmente pertenecen a Clientes o Seguimiento y control.
    - Resolver la diferencia de autorización entre menú y SPA para Crear transacción, Cargar archivo NACHA-M y Devoluciones, y revisar la asimetría SPA/API de seguimiento de cargas, sin hacerlo en NAV2.3.
    - Resolver posteriormente la duplicidad activa /ach-cycles; NAV2.3 la conserva.
    - Retirar/acotar el override CFA de Angular sólo en el incremento frontend autorizado.
    - Mantener /transactions/bulk-create fuera del menú hasta una decisión posterior explícita.
    - Validar runtime/idempotencia con .NET cuando el SDK esté disponible.
  risks_or_blockers:
    - La prueba .NET focal no puede ejecutarse por ausencia de dotnet; NAV2.3 queda IMPLEMENTED_UNVERIFIED_ENVIRONMENT.
    - El árbol visible de Operación es transicional: además de los diez hijos canónicos conserva tres hijos legacy fuera del alcance de este incremento para no reorganizar dominios futuros.
    - Los gaps de autorización existentes pueden seguir exponiendo entradas visibles a usuarios que luego sean rechazados por la guarda SPA; se preservan deliberadamente.
  next_increment_recommendation: NAV2.4 es técnicamente elegible sólo con nueva instrucción explícita. El siguiente incremento debería continuar un único dominio o gap del seed canónico, sin mezclar cambios de Angular o seguridad salvo autorización expresa.

nav2_4_execution:
  phase_executed: NAV2.4
  phase_status: IMPLEMENTED_UNVERIFIED_ENVIRONMENT
  files_modified:
    - MEMORY.md
    - docs/ai/navigation-menu/MEMORY_STATE.md
    - src/Cfa.ACHInterbank.Persistence/Navigation/Seeders/NavigationInformationArchitectureSeeder.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav22ParametersInformationArchitectureTests.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav23OperationInformationArchitectureTests.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav24CustomersInformationArchitectureTests.cs
  tests_executed:
    - command: python scripts/validate-solution-projects.py
      result: PASS
    - command: NAV2.4 static focal assertions sobre /customers, CanReadAch vigente, desactivación de /customer-third-parties y conservación de rutas contextuales Angular
      result: PASS
    - command: C# delimiter sanity sobre seeder y pruebas focales NAV2.2-NAV2.4 modificadas
      result: PASS
    - command: Comparación SHA-256 por archivo entre el ZIP full NAV2.3 y el workspace NAV2.4
      result: PASS; sólo archivos declarados cambiaron y no hubo cambios Angular.
  tests_passed:
    - python scripts/validate-solution-projects.py
    - NAV2.4 static focal assertions
    - C# delimiter sanity
    - auditoría estática de cobertura de rutas MENU* de la matriz objetivo
    - comparación de contenido ZIP/workspace dentro del alcance declarado
  tests_failed: []
  tests_deferred:
    - command: dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj --filter FullyQualifiedName~NavigationMenuNav24CustomersInformationArchitectureTests|FullyQualifiedName~NavigationMenuNav23OperationInformationArchitectureTests|FullyQualifiedName~NavigationMenuNav22ParametersInformationArchitectureTests|FullyQualifiedName~NavigationMenuNav21SeedInfrastructureTests
      reason: NOT_RUN_ENVIRONMENT; dotnet no está instalado en el host.
  decisions_taken:
    - /customers se publica como raíz canónica Clientes con Order 30; no existía un MenuItem previo para esa ruta.
    - La ruta SPA /customers está protegida inequívocamente por CanReadAch; la nueva entrada reutiliza únicamente ese PermissionId existente y no agrega roles, definiciones de permisos ni grants de usuario.
    - /customer-third-parties queda IsActive=false para retirarlo de la navegación visible, conservando su Route, Label, ParentId, Order, Exact, Icon y asociaciones de autorización existentes.
    - /customers/:id/third-parties y /customers/:id/third-parties/:thirdPartyId permanecen en Angular como rutas contextuales y no se crean MenuItem para ellas.
    - No se modifica Angular ni se reorganizan Seguimiento y control, Seguridad, Reportes, Administración o Herramientas de prueba.
  relevant_findings:
    - La SPA ya contiene una superficie canónica de clientes en /customers con CanReadAch y rutas contextuales de terceros bajo el cliente; el backend no tenía MenuItem /customers antes de NAV2.4.
    - La superficie legacy /customer-third-parties sigue existiendo funcionalmente como módulo/ruta independiente y mantiene Admin/ACH.Operator + CanReadAch/CanManageAch en su MenuItem histórico, pero deja de ser visible porque el reconciliador canónico la desactiva.
    - No fue necesario fusionar ni eliminar superficies de terceros: la administración contextual canónica y la compatibilidad legacy permanecen separadas.
  gaps_pending:
    - Operación todavía conserva /transactions/outgoing-monitoring e /incoming-nacha-command-center hasta el incremento autorizado de Seguimiento y control.
    - Mantener abiertos los gaps de autorización documentados en NAV2.3; NAV2.4 no los corrige.
    - Resolver posteriormente la duplicidad activa /ach-cycles y el override CFA de Angular, ambos fuera de NAV2.4.
    - Validar runtime/idempotencia con .NET cuando el SDK esté disponible.
  risks_or_blockers:
    - La prueba .NET focal no puede ejecutarse por ausencia de dotnet; NAV2.4 queda IMPLEMENTED_UNVERIFIED_ENVIRONMENT.
    - /customer-third-parties sigue siendo accesible por URL directa como compatibilidad, aunque ya no se publique en menú; su retiro funcional requeriría una decisión posterior explícita.
  next_increment_recommendation: NAV2.5 es técnicamente elegible sólo con nueva instrucción explícita; el siguiente incremento recomendado es Seguimiento y control para retirar de Operación las dos opciones transicionales restantes, sin mezclar cambios Angular ni seguridad salvo autorización expresa.

nav2_5a_execution:
  phase_executed: NAV2.5A
  phase_status: IMPLEMENTED_UNVERIFIED_ENVIRONMENT
  files_modified:
    - MEMORY.md
    - docs/ai/navigation-menu/MEMORY_STATE.md
    - docs/architecture/NAVIGATION_MENU_INFORMATION_ARCHITECTURE.md
    - docs/ux/NAVIGATION_MENU_ROUTE_MATRIX.md
    - src/Cfa.ACHInterbank.Persistence/Navigation/Seeders/NavigationInformationArchitectureSeeder.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav22ParametersInformationArchitectureTests.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav23OperationInformationArchitectureTests.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav24CustomersInformationArchitectureTests.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav25AMonitoringInformationArchitectureTests.cs
  tests_executed:
    - command: python scripts/validate-solution-projects.py
      result: PASS
    - command: NAV2.5A static focal assertions sobre raíz, jerarquía 10..50, rutas SPA/autorización vigente, exclusión de AchResponses/conciliación y alineación documental
      result: PASS
    - command: C# delimiter sanity sobre seeder y pruebas focales NAV2.2-NAV2.5A modificadas
      result: PASS
    - command: Comparación SHA-256 por archivo entre el ZIP full NAV2.4 y el workspace NAV2.5A
      result: PASS; 1 archivo agregado, 8 modificados, 0 eliminados; ningún archivo bajo web/ cambió.
    - command: Verificación ZIP FULL NAV2.5A contra el workspace final
      result: PASS; 4186 archivos y coincidencia SHA-256 por archivo.
  tests_passed:
    - python scripts/validate-solution-projects.py
    - NAV2.5A static focal assertions
    - C# delimiter sanity
    - auditoría estática de cobertura de rutas MENU* de la matriz objetivo
    - comparación de contenido ZIP/workspace dentro del alcance declarado
    - integridad del ZIP FULL contra el workspace final
  tests_failed: []
  tests_deferred:
    - command: dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj --filter FullyQualifiedName~NavigationMenuNav25AMonitoringInformationArchitectureTests|FullyQualifiedName~NavigationMenuNav24CustomersInformationArchitectureTests|FullyQualifiedName~NavigationMenuNav23OperationInformationArchitectureTests|FullyQualifiedName~NavigationMenuNav22ParametersInformationArchitectureTests|FullyQualifiedName~NavigationMenuNav21SeedInfrastructureTests
      reason: NOT_RUN_ENVIRONMENT; dotnet no está instalado en el host.
  decisions_taken:
    - Se crea /seguimiento-control como agrupador estructural raíz 'Seguimiento y control' con Order 40; no se agrega una ruta SPA nueva porque el padre funciona como expansor.
    - /transactions/outgoing-monitoring se conserva como ruta funcional y se reubica como 'Transacciones enviadas' Order 10, preservando sus enlaces de autorización existentes.
    - /incoming-nacha-command-center se conserva como ruta funcional y se reubica como 'Seguimiento de archivos' Order 20, preservando sus enlaces de autorización existentes.
    - /incoming-nacha-command-center/observability, /queue y /orphan-resolution no tenían MenuItem; se crean como hijos Orders 30, 40 y 50 porque sus rutas SPA y CanReadAch bajo Admin/ACH.Operator son inequívocos. Sólo al crear esos MenuItem se espejan los enlaces de autorización del ítem /incoming-nacha-command-center.
    - No se cambian rutas funcionales, definiciones de permisos, roles, RolePermissions, grants de usuario ni asociaciones de autorización de MenuItem preexistentes.
    - No se modifica Angular ni se publican Respuestas de transacciones, Conciliación, Seguridad, Reportes, Administración o Herramientas de prueba.
  relevant_findings:
    - La SPA confirma /incoming-nacha-command-center como superficie Admin/ACH.Operator + CanReadAch y sus rutas observability, queue y orphan-resolution como CanReadAch; por ello la relación es inequívoca sin inventar autorización.
    - /transactions/outgoing-monitoring ya cuenta con MenuItem y permiso fino OutgoingTransactions.Monitor.Read; NAV2.5A sólo cambia label, ParentId, Order y estado activo.
    - Operación queda sin los dos hijos transitorios /transactions/outgoing-monitoring e /incoming-nacha-command-center; sus diez hijos canónicos NAV2.3 permanecen sin cambios de ruta.
    - La documentación objetivo previa ordenaba Seguimiento de archivos antes de Transacciones enviadas; la instrucción NAV2.5A vigente establece Transacciones enviadas 10 y desplaza las cuatro superficies inbound a 20..50, por lo que arquitectura y matriz se alinearon con esa decisión.
  gaps_pending:
    - Respuestas de transacciones y Conciliación permanecen fuera de NAV2.5A y no se crean MenuItem para sus rutas.
    - Mantener abiertos los gaps de autorización documentados en NAV2.3; NAV2.5A no los corrige.
    - Resolver posteriormente la duplicidad activa /ach-cycles y el override CFA de Angular; ambos siguen fuera de alcance.
    - Mantener diferida la cobertura general de rutas y los dominios Seguridad, Reportes, Administración y Herramientas de prueba.
    - Validar runtime/idempotencia con .NET cuando el SDK esté disponible.
  risks_or_blockers:
    - La prueba .NET focal no puede ejecutarse por ausencia de dotnet; NAV2.5A queda IMPLEMENTED_UNVERIFIED_ENVIRONMENT.
    - /seguimiento-control es un agrupador estructural sin pantalla SPA propia; hereda el mismo comportamiento de padres expansores ya caracterizado para otros agrupadores canónicos.
  next_increment_recommendation: NAV2.5B queda técnicamente elegible sólo con nueva instrucción explícita. Debe continuar un alcance separado y no mezclar automáticamente Respuestas de transacciones, Conciliación, Angular ni correcciones de seguridad salvo autorización expresa.

nav2_5b_execution:
  phase_executed: NAV2.5B
  phase_status: IMPLEMENTED_UNVERIFIED_ENVIRONMENT
  files_modified:
    - MEMORY.md
    - docs/ai/navigation-menu/MEMORY_STATE.md
    - src/Cfa.ACHInterbank.Persistence/Navigation/Seeders/NavigationInformationArchitectureSeeder.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav25AMonitoringInformationArchitectureTests.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav25BResponsesReconciliationInformationArchitectureTests.cs
  tests_executed:
    - command: python scripts/validate-solution-projects.py
      result: PASS
    - command: NAV2.5B static focal assertions sobre jerarquía final, grupo estructural, órdenes 10..70, separación de status-mappings, rutas SPA y autorización vigente
      result: PASS
    - command: C# delimiter sanity sobre seeder y pruebas focales NAV2.5A/NAV2.5B
      result: PASS
    - command: Comparación SHA-256 por archivo entre el ZIP full NAV2.5A y el workspace NAV2.5B
      result: PASS; 1 archivo agregado, 4 modificados, 0 eliminados; ningún archivo bajo web/ cambió.
    - command: Verificación ZIP FULL NAV2.5B contra el workspace final
      result: PASS; archivo por archivo, sin diferencias.
  tests_passed:
    - python scripts/validate-solution-projects.py
    - NAV2.5B static focal assertions
    - C# delimiter sanity
    - auditoría estática de cobertura de rutas MENU* de la matriz objetivo
    - comparación de contenido ZIP/workspace dentro del alcance declarado
    - integridad del ZIP FULL contra el workspace final
  tests_failed: []
  tests_deferred:
    - command: dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj --filter FullyQualifiedName~NavigationMenuNav25BResponsesReconciliationInformationArchitectureTests|FullyQualifiedName~NavigationMenuNav25AMonitoringInformationArchitectureTests|FullyQualifiedName~NavigationMenuNav24CustomersInformationArchitectureTests|FullyQualifiedName~NavigationMenuNav23OperationInformationArchitectureTests|FullyQualifiedName~NavigationMenuNav22ParametersInformationArchitectureTests|FullyQualifiedName~NavigationMenuNav21SeedInfrastructureTests
      reason: NOT_RUN_ENVIRONMENT; dotnet no está instalado en el host.
  decisions_taken:
    - Se crea /seguimiento-control/respuestas-transacciones como agrupador estructural 'Respuestas de transacciones' Order 60; no es destino SPA y evita compartir /ach-responses con su hijo Bandeja de respuestas.
    - /ach-responses, /dashboard, /manual-review y /audit se publican como hijos 10, 20, 30 y 40; /ach-responses/status-mappings permanece en Administración de parámetros.
    - /ach/reconciliation se publica como Conciliación Order 70 bajo Seguimiento y control.
    - Las cinco nuevas entradas funcionales reutilizan únicamente Admin/ACH.Operator + CanReadAch al crearse; no se cambian definiciones de permisos, roles, RolePermissions, grants ni enlaces de autorización preexistentes.
    - Las rutas /ach-responses/:id y /ach-responses/:id/notification-attempts permanecen contextuales y fuera del menú.
    - No se modifica Angular ni se implementan Seguridad, Reportes, Administración o Herramientas de prueba.
  relevant_findings:
    - La SPA confirma de forma inequívoca Admin/ACH.Operator + CanReadAch para el módulo /ach-responses y CanReadAch para sus cuatro rutas objetivo; /ach/reconciliation está bajo /ach con la misma autorización y su ruta hija también exige CanReadAch.
    - Ninguna de las cinco rutas funcionales NAV2.5B tenía MenuItem antes del reconciliador canónico; por ello se crean sin inventar permisos.
    - Revisión manual comparte hoy CanReadAch con las superficies de lectura; la matriz ya marcaba que su granularidad fina debe revisarse en una fase posterior, pero NAV2.5B no la endurece.
  gaps_pending:
    - Mantener abiertos los gaps de autorización previos, incluida la revisión fina de /ach-responses/manual-review; NAV2.5B sólo conserva la autorización SPA vigente.
    - Resolver posteriormente la duplicidad activa /ach-cycles y el override CFA de Angular; ambos siguen fuera de alcance.
    - Mantener diferida la cobertura general restante y los dominios Seguridad, Reportes, Administración y Herramientas de prueba.
    - Validar runtime/idempotencia con .NET cuando el SDK esté disponible.
  risks_or_blockers:
    - La prueba .NET focal no puede ejecutarse por ausencia de dotnet; NAV2.5B queda IMPLEMENTED_UNVERIFIED_ENVIRONMENT.
    - Los agrupadores /seguimiento-control y /seguimiento-control/respuestas-transacciones son estructurales sin pantalla SPA propia y dependen del comportamiento expansor ya caracterizado de MainLayout.
  next_increment_recommendation: NAV2.6 queda técnicamente elegible sólo con nueva instrucción explícita; se recomienda abordar un único dominio canónico restante, preferiblemente Seguridad de archivos, sin mezclar Angular ni correcciones de autorización salvo alcance expreso.

nav2_6_execution:
  phase_executed: NAV2.6
  phase_status: IMPLEMENTED_UNVERIFIED_ENVIRONMENT
  files_modified:
    - MEMORY.md
    - docs/ai/navigation-menu/MEMORY_STATE.md
    - src/Cfa.ACHInterbank.Persistence/Navigation/Seeders/NavigationInformationArchitectureSeeder.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav22ParametersInformationArchitectureTests.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav23OperationInformationArchitectureTests.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav24CustomersInformationArchitectureTests.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav25AMonitoringInformationArchitectureTests.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav25BResponsesReconciliationInformationArchitectureTests.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav26FileSecurityInformationArchitectureTests.cs
  tests_executed:
    - command: python scripts/validate-solution-projects.py
      result: PASS
    - command: NAV2.6 static focal assertions sobre raíz Seguridad de archivos, órdenes 60/10/20, conservación de rutas y exclusión de cuatro superficies técnicas/manuales
      result: PASS
    - command: C# delimiter sanity sobre seeder y pruebas focales NAV2.2-NAV2.6 modificadas
      result: PASS
    - command: Comparación SHA-256 por archivo entre el ZIP full NAV2.5B y el workspace NAV2.6
      result: PASS; 1 archivo agregado, 8 modificados, 0 eliminados; ningún archivo bajo web/ cambió.
    - command: Verificación ZIP FULL NAV2.6 contra el workspace final
      result: PASS; archivo por archivo, sin diferencias.
  tests_passed:
    - python scripts/validate-solution-projects.py
    - NAV2.6 static focal assertions
    - C# delimiter sanity
    - auditoría estática de cobertura de rutas MENU* de la matriz objetivo
    - comparación de contenido ZIP/workspace dentro del alcance declarado
    - integridad del ZIP FULL contra el workspace final
  tests_failed: []
  tests_deferred:
    - command: dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj --filter FullyQualifiedName~NavigationMenuNav26FileSecurityInformationArchitectureTests|FullyQualifiedName~NavigationMenuNav25BResponsesReconciliationInformationArchitectureTests|FullyQualifiedName~NavigationMenuNav25AMonitoringInformationArchitectureTests|FullyQualifiedName~NavigationMenuNav24CustomersInformationArchitectureTests|FullyQualifiedName~NavigationMenuNav23OperationInformationArchitectureTests|FullyQualifiedName~NavigationMenuNav22ParametersInformationArchitectureTests|FullyQualifiedName~NavigationMenuNav21SeedInfrastructureTests
      reason: NOT_RUN_ENVIRONMENT; dotnet no está instalado en el host.
  decisions_taken:
    - /nacha-security se reutiliza como raíz canónica Seguridad de archivos con Order 60; su ruta funcional existente no cambia.
    - /nacha-security/certificates se conserva como Certificados digitales Order 10 y /nacha-security/sobre-digital se conserva como Cifrar y descifrar archivos Order 20.
    - NAV2.6 sólo ajusta Label, ParentId, Order e IsActive de los tres MenuItem existentes; no crea MenuItem nuevo para este dominio ni modifica definiciones de permisos, roles, RolePermissions, grants o enlaces de autorización existentes.
    - Las rutas técnicas/manuales /nacha-security/nacha/generate, /nacha-security/nacha/generate-encrypted, /nacha-security/digital-envelope/manual-encrypt y /nacha-security/digital-envelope/manual-decrypt permanecen funcionales en Angular y fuera del menú.
    - No se modifica Angular, no se corrigen gaps de autorización previos, no se resuelve /ach-cycles, no se toca el override CFA y no se implementan Reportes, Administración o Herramientas de prueba.
  relevant_findings:
    - NachaSecurityMenuSeeder ya crea/normaliza las tres entradas requeridas antes del reconciliador canónico, por lo que NAV2.6 no necesita inventar rutas ni permisos.
    - Certificados digitales conserva su MenuItemPermission CanManageCertificates y no tiene restricción MenuItemRole; la raíz y Cifrar y descifrar archivos no tienen enlaces directos MenuItemPermission/MenuItemRole después de NachaSecurityMenuSeeder.
    - La SPA de certificados acepta Certificates.Read, CanManageCertificates o CanReadAch; la SPA de sobre digital acepta CanManualEncryptEnvelope, CanManualDecryptEnvelope, CanManageAch o CanReadAch. La asimetría frente a la visibilidad del menú es preexistente y no se corrige en NAV2.6.
  gaps_pending:
    - Mantener abiertos los gaps de autorización previos y GAP-NAV-008 de Seguridad de archivos; NAV2.6 conserva autorización existente por instrucción explícita.
    - Resolver posteriormente la duplicidad activa /ach-cycles y el override CFA de Angular; ambos siguen fuera de alcance.
    - Mantener diferida la cobertura general restante y los dominios Reportes, Administración y Herramientas de prueba.
    - Validar runtime/idempotencia con .NET cuando el SDK esté disponible.
  risks_or_blockers:
    - La prueba .NET focal no puede ejecutarse por ausencia de dotnet; NAV2.6 queda IMPLEMENTED_UNVERIFIED_ENVIRONMENT.
    - La autorización de menú de Seguridad de archivos no coincide exactamente con las guardas SPA; el drift es conocido, se preserva y debe revisarse sólo en una fase explícita de autorización.
  next_increment_recommendation: NAV2.7 queda técnicamente elegible sólo con nueva instrucción explícita; se recomienda abordar únicamente Reportes y mantener separados Angular, seguridad y dominios administrativos.

nav2_7_execution:
  phase_executed: NAV2.7
  phase_status: IMPLEMENTED_UNVERIFIED_ENVIRONMENT
  files_modified:
    - MEMORY.md
    - docs/ai/navigation-menu/MEMORY_STATE.md
    - docs/architecture/NAVIGATION_MENU_INFORMATION_ARCHITECTURE.md
    - docs/ux/NAVIGATION_MENU_ROUTE_MATRIX.md
    - src/Cfa.ACHInterbank.Persistence/Navigation/Seeders/NavigationInformationArchitectureSeeder.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav23OperationInformationArchitectureTests.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav24CustomersInformationArchitectureTests.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav25AMonitoringInformationArchitectureTests.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav25BResponsesReconciliationInformationArchitectureTests.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav26FileSecurityInformationArchitectureTests.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav27ReportsInformationArchitectureTests.cs
  tests_executed:
    - command: python scripts/validate-solution-projects.py
      result: PASS
    - command: NAV2.7 static focal assertions sobre raíz Reportes, diez rutas SPA, órdenes 10..100, autorización heredada y separación /reports/audit vs /audit-logs
      result: PASS
    - command: C# delimiter sanity sobre seeder y prueba focal NAV2.7
      result: PASS
    - command: Comparación SHA-256 por archivo entre el ZIP full NAV2.6 y el workspace NAV2.7
      result: PASS; 1 archivo agregado, 10 modificados, 0 eliminados; ningún archivo bajo web/ cambió.
    - command: Verificación ZIP FULL NAV2.7 contra el workspace final
      result: PASS; 4189 archivos, sin faltantes, extras ni diferencias de contenido.
  tests_passed:
    - python scripts/validate-solution-projects.py
    - NAV2.7 static focal assertions
    - C# delimiter sanity
    - auditoría estática de cobertura de rutas MENU* de la matriz objetivo
    - comparación de contenido ZIP/workspace dentro del alcance declarado
    - integridad del ZIP FULL contra el workspace final
  tests_failed: []
  tests_deferred:
    - command: dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj --filter FullyQualifiedName~NavigationMenuNav27ReportsInformationArchitectureTests|FullyQualifiedName~NavigationMenuNav26FileSecurityInformationArchitectureTests|FullyQualifiedName~NavigationMenuNav25BResponsesReconciliationInformationArchitectureTests|FullyQualifiedName~NavigationMenuNav25AMonitoringInformationArchitectureTests|FullyQualifiedName~NavigationMenuNav24CustomersInformationArchitectureTests|FullyQualifiedName~NavigationMenuNav23OperationInformationArchitectureTests|FullyQualifiedName~NavigationMenuNav22ParametersInformationArchitectureTests|FullyQualifiedName~NavigationMenuNav21SeedInfrastructureTests
      reason: NOT_RUN_ENVIRONMENT; dotnet no está instalado en el host.
  decisions_taken:
    - /reports se conserva como ruta funcional existente y queda canonicalizado como raíz Reportes Order 70.
    - Se publican únicamente las diez rutas de reporte realmente declaradas en el SPA: sent, received, returns, rejections, files, cycles, reconciliation, traceability, history y audit; no se inventan reportes ni rutas.
    - Los hijos usan órdenes 10..100 en el mismo orden funcional mostrado por ReportsHomeComponent; los labels son Transacciones enviadas, Transacciones recibidas, Devoluciones, Rechazos, Archivos NACHA-M, Ciclos, Conciliación, Trazabilidad ACH, Histórico y Auditoría.
    - Ninguna de las diez rutas tenía MenuItem antes de NAV2.7; sólo al crearlas se espeja la autorización existente de /reports. No se reescribe autorización preexistente ni se modifican permisos, roles, RolePermissions o grants.
    - /reports/audit se mantiene dentro de Reportes porque es una superficie funcional de reporte con API/PDF propia; /audit-logs permanece sin cambios para Administración.
    - No se modifica Angular ni se implementan Administración o Herramientas de prueba; tampoco se corrigen gaps de autorización, /ach-cycles, override CFA o cobertura restante.
  relevant_findings:
    - El SPA declara diez rutas hijas reales bajo /reports. ReportsMenuSeeder actual sólo crea la raíz /reports; no existían MenuItem para esos diez reportes antes del reconciliador canónico.
    - El guard del módulo /reports exige Admin o ACH.Operator y CanReadAch; todas las rutas hijas pertenecen a ese módulo y las declaraciones actuales usan CanReadAch, por lo que la autorización es inequívoca para publicar las entradas nuevas.
    - Auditoría de Reportes y Registro de auditoría administrativo comparten la tabla AuditLogs, pero exponen contratos distintos: api/reports/audit permite la vista de reporte y PDF; api/audit-logs alimenta la pantalla administrativa detallada. No son el mismo destino de navegación.
  gaps_pending:
    - Mantener abiertos los gaps de autorización registrados; NAV2.7 no cambia políticas ni asociaciones preexistentes.
    - Resolver posteriormente la duplicidad activa /ach-cycles y el override CFA de Angular; ambos siguen fuera de alcance.
    - Mantener diferidos Administración, Herramientas de prueba y la cobertura general restante.
    - Validar runtime/idempotencia con .NET cuando el SDK esté disponible.
  risks_or_blockers:
    - La prueba .NET focal no puede ejecutarse por ausencia de dotnet; NAV2.7 queda IMPLEMENTED_UNVERIFIED_ENVIRONMENT.
    - /reports sigue siendo una ruta SPA funcional con pantalla índice, pero al tener hijos el MainLayout actual trata la raíz como expansor; la ruta directa /reports se conserva aunque la entrada raíz del menú no navega mientras tenga hijos.
  next_increment_recommendation: NAV2.8 queda técnicamente elegible sólo con nueva instrucción explícita; se recomienda abordar únicamente Administración y mantener Herramientas de prueba para su incremento separado.

nav2_8a_execution:
  phase_executed: NAV2.8A
  phase_status: IMPLEMENTED_UNVERIFIED_ENVIRONMENT
  files_modified:
    - MEMORY.md
    - docs/ai/navigation-menu/MEMORY_STATE.md
    - docs/architecture/NAVIGATION_MENU_INFORMATION_ARCHITECTURE.md
    - docs/ux/NAVIGATION_MENU_ROUTE_MATRIX.md
    - src/Cfa.ACHInterbank.Persistence/Navigation/Seeders/NavigationInformationArchitectureSeeder.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav23OperationInformationArchitectureTests.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav24CustomersInformationArchitectureTests.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav25AMonitoringInformationArchitectureTests.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav25BResponsesReconciliationInformationArchitectureTests.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav26FileSecurityInformationArchitectureTests.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav27ReportsInformationArchitectureTests.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav28AAdministrationUsersAccessInformationArchitectureTests.cs
  tests_executed:
    - command: python scripts/validate-solution-projects.py
      result: PASS
    - command: NAV2.8A static focal assertions sobre rutas SPA reales, jerarquía/órdenes, autorización existente, exclusión contextual y alcance
      result: PASS
    - command: C# delimiter sanity sobre seeder y prueba focal NAV2.8A
      result: PASS
    - command: Comparación SHA-256 por archivo entre el ZIP full NAV2.7 y el workspace NAV2.8A
      result: PASS; 1 archivo agregado, 11 modificados, 0 eliminados; ningún archivo bajo web/ cambió.
    - command: Verificación ZIP FULL NAV2.8A contra el workspace final
      result: PASS; 4190 archivos, sin faltantes, extras ni diferencias de contenido.
  tests_passed:
    - python scripts/validate-solution-projects.py
    - NAV2.8A static focal assertions
    - C# delimiter sanity
    - auditoría estática de cobertura de rutas MENU* de la matriz objetivo
    - comparación de contenido ZIP/workspace dentro del alcance declarado
    - integridad del ZIP FULL contra el workspace final
  tests_failed: []
  tests_deferred:
    - command: dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj --filter FullyQualifiedName~NavigationMenuNav28AAdministrationUsersAccessInformationArchitectureTests|FullyQualifiedName~NavigationMenuNav27ReportsInformationArchitectureTests|FullyQualifiedName~NavigationMenuNav26FileSecurityInformationArchitectureTests|FullyQualifiedName~NavigationMenuNav25BResponsesReconciliationInformationArchitectureTests|FullyQualifiedName~NavigationMenuNav25AMonitoringInformationArchitectureTests|FullyQualifiedName~NavigationMenuNav24CustomersInformationArchitectureTests|FullyQualifiedName~NavigationMenuNav23OperationInformationArchitectureTests|FullyQualifiedName~NavigationMenuNav22ParametersInformationArchitectureTests|FullyQualifiedName~NavigationMenuNav21SeedInfrastructureTests
      reason: NOT_RUN_ENVIRONMENT; dotnet no está instalado en el host.
  decisions_taken:
    - /administracion se crea sólo como agrupador estructural raíz Administración Order 80; no se introduce una nueva pantalla SPA.
    - /users se reutiliza como agrupador Usuarios y acceso Order 10, conservando su ruta SPA y su autorización preexistente.
    - Se reutilizan /users/list, /users/branding, /users/password-rules y /users/login-lockout con labels/órdenes 10..40; no se crea ningún MenuItem funcional nuevo ni se inventa una ruta funcional.
    - La autorización existente Admin + CanManageUsers de /users y las cuatro opciones se conserva sin cambios; no se modifican permisos, roles, RolePermissions, grants ni políticas de autorización.
    - /users/new, /users/:id/edit y /users/:id/roles permanecen contextuales y fuera del menú; no existe ruta SPA independiente de detalle de usuario.
    - No se modifica Angular y no se implementan Tareas programadas, Registros del sistema, Organización del menú, Capacidades de la plataforma ni Herramientas de prueba.
    - /reports/audit permanece bajo Reportes; /audit-logs no se toca en NAV2.8A.
  relevant_findings:
    - UsersMenuSeeder ya materializa /users y /users/list antes del reconciliador; Branding, Reglas de contraseña y Bloqueo de acceso ya existen por seed de modelo.
    - Las cuatro rutas objetivo existen de forma inequívoca en AdminRoutingModule y heredan el guard de módulo Admin + CanManageUsers.
    - Los cinco MenuItem existentes del dominio de usuarios conservan sus IDs y enlaces MenuItemRole/MenuItemPermission; NAV2.8A sólo ajusta Label, ParentId, Order e IsActive en ellos.
  gaps_pending:
    - Mantener abiertos los gaps de autorización previos; NAV2.8A no los corrige.
    - /administracion es estructural y no tiene pantalla SPA propia; el comportamiento general de padres navegables/estructurales en Angular sigue fuera de alcance.
    - Resolver posteriormente la duplicidad activa /ach-cycles y el override CFA de Angular; ambos siguen fuera de alcance.
    - Mantener diferidos NAV2.8B, Herramientas de prueba y la cobertura general restante hasta instrucción explícita.
    - Validar runtime/idempotencia con .NET cuando el SDK esté disponible.
  risks_or_blockers:
    - La prueba .NET focal no puede ejecutarse por ausencia de dotnet; NAV2.8A queda IMPLEMENTED_UNVERIFIED_ENVIRONMENT.
    - El agrupador estructural /administracion no es destino SPA funcional y depende del comportamiento actual del MainLayout para presentarse como expansor cuando tenga hijos visibles.
  next_increment_recommendation: NAV2.8B queda técnicamente elegible sólo con nueva instrucción explícita; se recomienda abordar únicamente Tareas programadas, Registros del sistema y Organización del menú, manteniendo Capacidades de la plataforma/Herramientas de prueba para sus fases explícitas.

nav2_8b_execution:
  phase_executed: NAV2.8B
  phase_status: IMPLEMENTED_UNVERIFIED_ENVIRONMENT
  files_modified:
    - MEMORY.md
    - docs/ai/navigation-menu/MEMORY_STATE.md
    - docs/architecture/NAVIGATION_MENU_INFORMATION_ARCHITECTURE.md
    - docs/ux/NAVIGATION_MENU_ROUTE_MATRIX.md
    - src/Cfa.ACHInterbank.Persistence/Navigation/Seeders/NavigationInformationArchitectureSeeder.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav23OperationInformationArchitectureTests.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav24CustomersInformationArchitectureTests.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav25AMonitoringInformationArchitectureTests.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav25BResponsesReconciliationInformationArchitectureTests.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav26FileSecurityInformationArchitectureTests.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav27ReportsInformationArchitectureTests.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav28AAdministrationUsersAccessInformationArchitectureTests.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav28BAdministrationScheduledTasksInformationArchitectureTests.cs
  - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav28CAdministrationSystemRecordsInformationArchitectureTests.cs
  tests_executed:
    - command: python scripts/validate-solution-projects.py
      result: PASS
    - command: NAV2.8B static focal assertions sobre ruta SPA, jerarquía/órdenes, autorización vigente, exclusión de superficies contextuales/técnicas y alcance
      result: PASS
    - command: C# delimiter sanity sobre seeder y pruebas de navegación modificadas/agregadas
      result: PASS
    - command: Comparación SHA-256 por archivo entre el ZIP FULL NAV2.8A y el workspace NAV2.8B
      result: PASS; 1 archivo agregado, 12 modificados, 0 eliminados; ningún archivo bajo web/ cambió.
    - command: Verificación ZIP FULL NAV2.8B contra el workspace final
      result: PASS; 4191 archivos, sin faltantes, extras ni diferencias de contenido.
  tests_passed:
    - python scripts/validate-solution-projects.py
    - NAV2.8B static focal assertions
    - C# delimiter sanity
    - auditoría estática de cobertura de rutas MENU* de la matriz objetivo
    - comparación de contenido ZIP/workspace dentro del alcance declarado
    - integridad del ZIP FULL contra el workspace final
  tests_failed: []
  tests_deferred:
    - command: dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj --filter FullyQualifiedName~NavigationMenuNav28BAdministrationScheduledTasksInformationArchitectureTests|FullyQualifiedName~NavigationMenuNav28AAdministrationUsersAccessInformationArchitectureTests|FullyQualifiedName~NavigationMenuNav27ReportsInformationArchitectureTests|FullyQualifiedName~NavigationMenuNav26FileSecurityInformationArchitectureTests|FullyQualifiedName~NavigationMenuNav25BResponsesReconciliationInformationArchitectureTests|FullyQualifiedName~NavigationMenuNav25AMonitoringInformationArchitectureTests|FullyQualifiedName~NavigationMenuNav24CustomersInformationArchitectureTests|FullyQualifiedName~NavigationMenuNav23OperationInformationArchitectureTests|FullyQualifiedName~NavigationMenuNav22ParametersInformationArchitectureTests|FullyQualifiedName~NavigationMenuNav21SeedInfrastructureTests
      reason: NOT_RUN_ENVIRONMENT; dotnet no está instalado en el host.
  decisions_taken:
    - /scheduler/tasks es la ruta SPA funcional inequívoca de administración del scheduler y se reutiliza como Tareas programadas, hijo directo de /administracion con Order 20.
    - Se reutiliza el MenuItem existente TaskDefinitionsId; no se crea una nueva ruta ni un nuevo MenuItem funcional.
    - El MenuItem legacy /scheduler se conserva en datos pero queda IsActive=false para evitar un segundo acceso visible; la ruta SPA /scheduler se conserva como redirect a /scheduler/tasks.
    - La autorización preexistente Scheduler.View de /scheduler y /scheduler/tasks se conserva sin cambios; no se modifican permisos, roles, RolePermissions, grants ni asociaciones de autorización.
    - Historial, instancias, detalle técnico, ejecución manual, pausa/reanudación y edición de programación permanecen dentro de la pantalla/API de Tareas programadas y no se publican como opciones de menú independientes.
    - No se modifica Angular ni se reorganizan Registros del sistema, Organización del menú, Capacidades de la plataforma o Herramientas de prueba.
  relevant_findings:
    - SchedulerRoutingModule declara sólo path `tasks` con permissionGuard + Scheduler.View y un redirect vacío a `tasks`; AppRouting monta el módulo bajo /scheduler.
    - MenuItemConfiguration ya siembra /scheduler y /scheduler/tasks; ambos tienen MenuItemPermission Scheduler.View y no tienen MenuItemRole directo.
    - Admin y ACH.Operator ya reciben Scheduler.View mediante RolePermission; NAV2.8B no toca esos grants ni los permisos más finos usados dentro de la pantalla.
  gaps_pending:
    - Mantener abiertos los gaps de autorización previos; NAV2.8B no los corrige.
    - /administracion sigue siendo estructural y sin pantalla SPA propia; el comportamiento general de padres navegables/estructurales en Angular sigue fuera de alcance.
    - Resolver posteriormente la duplicidad activa /ach-cycles y el override CFA de Angular; ambos siguen fuera de alcance.
    - Mantener diferidos Registros del sistema, Organización del menú, Capacidades de la plataforma y Herramientas de prueba hasta sus incrementos explícitos.
    - Validar runtime/idempotencia con .NET cuando el SDK esté disponible.
  risks_or_blockers:
    - La prueba .NET focal no puede ejecutarse por ausencia de dotnet; NAV2.8B queda IMPLEMENTED_UNVERIFIED_ENVIRONMENT.
    - /scheduler permanece como ruta SPA de compatibilidad/redirect aunque su MenuItem legacy queda inactivo; no debe interpretarse como una segunda opción funcional publicada.
  next_increment_recommendation: NAV2.8C queda técnicamente elegible sólo con nueva instrucción explícita; se recomienda limitarlo a Registros del sistema y mantener Organización del menú, Capacidades de la plataforma y Herramientas de prueba para incrementos posteriores.

nav2_8c_execution:
  phase_executed: NAV2.8C
  phase_status: IMPLEMENTED_UNVERIFIED_ENVIRONMENT
  files_modified:
    - MEMORY.md
    - docs/ai/navigation-menu/MEMORY_STATE.md
    - docs/architecture/NAVIGATION_MENU_INFORMATION_ARCHITECTURE.md
    - docs/ux/NAVIGATION_MENU_ROUTE_MATRIX.md
    - src/Cfa.ACHInterbank.Persistence/Navigation/Seeders/NavigationInformationArchitectureSeeder.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav23OperationInformationArchitectureTests.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav24CustomersInformationArchitectureTests.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav25AMonitoringInformationArchitectureTests.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav25BResponsesReconciliationInformationArchitectureTests.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav26FileSecurityInformationArchitectureTests.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav27ReportsInformationArchitectureTests.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav28AAdministrationUsersAccessInformationArchitectureTests.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav28BAdministrationScheduledTasksInformationArchitectureTests.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav28CAdministrationSystemRecordsInformationArchitectureTests.cs
  tests_executed:
    - command: python scripts/validate-solution-projects.py
      result: PASS
    - command: NAV2.8C static focal assertions sobre rutas SPA/API, jerarquía/órdenes, preservación/creación de autorización, separación de /reports/audit y exclusión de superficies técnicas
      result: PASS
    - command: C# delimiter sanity sobre seeder y pruebas de navegación modificadas/agregadas
      result: PASS
    - command: Comparación SHA-256 por archivo entre el ZIP FULL NAV2.8B y el workspace NAV2.8C
      result: PASS; 1 archivo agregado, 13 modificados, 0 eliminados; ningún archivo bajo web/ cambió.
    - command: Verificación ZIP FULL NAV2.8C contra el workspace final
      result: PASS; 4192 archivos, sin faltantes, extras ni diferencias de contenido.
  tests_passed:
    - python scripts/validate-solution-projects.py
    - NAV2.8C static focal assertions
    - C# delimiter sanity
    - auditoría estática de cobertura de rutas MENU* de la matriz objetivo
    - comparación SHA-256 ZIP NAV2.8B/workspace NAV2.8C dentro del alcance declarado
    - integridad del ZIP FULL NAV2.8C contra el workspace final
  tests_failed: []
  tests_deferred:
    - command: dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj --filter FullyQualifiedName~NavigationMenuNav28CAdministrationSystemRecordsInformationArchitectureTests|FullyQualifiedName~NavigationMenuNav28BAdministrationScheduledTasksInformationArchitectureTests|FullyQualifiedName~NavigationMenuNav28AAdministrationUsersAccessInformationArchitectureTests|FullyQualifiedName~NavigationMenuNav27ReportsInformationArchitectureTests|FullyQualifiedName~NavigationMenuNav26FileSecurityInformationArchitectureTests|FullyQualifiedName~NavigationMenuNav25BResponsesReconciliationInformationArchitectureTests|FullyQualifiedName~NavigationMenuNav25AMonitoringInformationArchitectureTests|FullyQualifiedName~NavigationMenuNav24CustomersInformationArchitectureTests|FullyQualifiedName~NavigationMenuNav23OperationInformationArchitectureTests|FullyQualifiedName~NavigationMenuNav22ParametersInformationArchitectureTests|FullyQualifiedName~NavigationMenuNav21SeedInfrastructureTests
      reason: NOT_RUN_ENVIRONMENT; dotnet no está instalado en el host.
  decisions_taken:
    - /logs se reutiliza como agrupador estructural Registros del sistema, hijo directo de /administracion con Order 30; su autorización CanManageUsers preexistente se conserva.
    - /audit-logs se reutiliza como Registro de auditoría Order 10 y /auth-logs como Registro de accesos Order 20; ambos conservan IDs, rutas y MenuItemPermission CanManageUsers.
    - /navigation-logs es la ruta SPA vigente de consulta de navegación; como no tenía MenuItem, se crea sólo ese MenuItem como Registro de navegación Order 30 y se reutiliza CanReadAch del GET API vigente.
    - /reports/audit permanece exclusivamente bajo Reportes y no se mezcla con el registro administrativo /audit-logs.
    - /log y /logs permanecen redirects SPA; no se publican aliases ni rutas de detalle/técnicas adicionales para los registros.
    - No se modifica Angular ni Organización del menú, Capacidades de la plataforma, Herramientas de prueba o dominios ya canonicalizados salvo los ajustes acumulativos mínimos de pruebas.
  relevant_findings:
    - AppRouting declara /audit-logs, /auth-logs y /navigation-logs como superficies SPA reales bajo autenticación; los tres feature routing modules sólo declaran su ruta vacía y no exponen detalle independiente.
    - AuditLogsController y AuthLogsController requieren [Authorize] sin política fina, mientras sus MenuItem existentes exigen CanManageUsers.
    - NavigationLogsController GET exige CanReadAch, mientras la ruta SPA no declara permissionGuard; el POST de telemetría sigue disponible para cualquier usuario autenticado y no se convierte en entrada de menú.
    - El padre /logs conserva CanManageUsers. Como GetMenuForCurrentUserHandler filtra cada fila antes de reconstruir padres/hijos, existe drift de visibilidad potencial para usuarios con CanReadAch sin CanManageUsers; NAV2.8C lo registra y no altera autorización preexistente.
  gaps_pending:
    - Mantener abierto el drift de autorización de Registros del sistema: audit/access menú CanManageUsers vs SPA/API autenticados; navegación SPA autenticada vs GET API CanReadAch; padre /logs CanManageUsers.
    - Mantener abiertos los gaps de autorización anteriores; NAV2.8C no los corrige.
    - /administracion sigue siendo estructural y sin pantalla SPA propia; el comportamiento general de padres navegables/estructurales en Angular sigue fuera de alcance.
    - Resolver posteriormente la duplicidad activa /ach-cycles y el override CFA de Angular; ambos siguen fuera de alcance.
    - Mantener diferidos Organización del menú, Capacidades de la plataforma y Herramientas de prueba hasta sus incrementos explícitos.
    - Validar runtime/idempotencia con .NET cuando el SDK esté disponible.
  risks_or_blockers:
    - La prueba .NET focal no puede ejecutarse por ausencia de dotnet; NAV2.8C queda IMPLEMENTED_UNVERIFIED_ENVIRONMENT.
    - La combinación de CanManageUsers en el padre /logs y CanReadAch en el nuevo hijo /navigation-logs puede producir una presentación raíz del hijo para un principal con CanReadAch pero sin CanManageUsers debido al algoritmo actual de reconstrucción del menú; corregirlo sería un cambio de autorización/comportamiento fuera de NAV2.8C.
  next_increment_recommendation: NAV2.8D queda técnicamente elegible sólo con nueva instrucción explícita; se recomienda limitarlo a Organización del menú y mantener Capacidades de la plataforma y Herramientas de prueba para sus incrementos posteriores.

nav2_8d_execution:
  phase_executed: NAV2.8D
  phase_status: IMPLEMENTED_UNVERIFIED_ENVIRONMENT
  files_modified:
    - MEMORY.md
    - docs/ai/navigation-menu/MEMORY_STATE.md
    - docs/architecture/NAVIGATION_MENU_INFORMATION_ARCHITECTURE.md
    - docs/ux/NAVIGATION_MENU_ROUTE_MATRIX.md
    - src/Cfa.ACHInterbank.Persistence/Navigation/Seeders/NavigationInformationArchitectureSeeder.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav23OperationInformationArchitectureTests.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav24CustomersInformationArchitectureTests.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav25AMonitoringInformationArchitectureTests.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav25BResponsesReconciliationInformationArchitectureTests.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav26FileSecurityInformationArchitectureTests.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav27ReportsInformationArchitectureTests.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav28AAdministrationUsersAccessInformationArchitectureTests.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav28BAdministrationScheduledTasksInformationArchitectureTests.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav28CAdministrationSystemRecordsInformationArchitectureTests.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav28DAdministrationMenuOrganizationInformationArchitectureTests.cs
  tests_executed:
    - command: python scripts/validate-solution-projects.py
      result: PASS
    - command: NAV2.8D static focal assertions sobre ruta SPA/API, reutilización del MenuItem, jerarquía/órdenes, autorización vigente, separación de navigation logs y exclusión de capacidades
      result: PASS
    - command: C# delimiter sanity sobre seeder y pruebas de navegación modificadas/agregadas
      result: PASS
    - command: Comparación SHA-256 por archivo entre el ZIP FULL NAV2.8C y el workspace NAV2.8D
      result: PASS; 1 archivo agregado, 14 modificados, 0 eliminados; ningún archivo bajo web/ cambió.
    - command: Verificación ZIP FULL NAV2.8D contra el workspace final
      result: PASS; 4193 archivos, sin faltantes, extras ni diferencias de contenido.
  tests_passed:
    - python scripts/validate-solution-projects.py
    - NAV2.8D static focal assertions
    - C# delimiter sanity
    - auditoría estática de cobertura de rutas MENU* de la matriz objetivo
    - comparación SHA-256 ZIP NAV2.8C/workspace NAV2.8D dentro del alcance declarado
    - integridad del ZIP FULL NAV2.8D contra el workspace final
  tests_failed: []
  tests_deferred:
    - command: dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj --filter FullyQualifiedName~NavigationMenuNav28DAdministrationMenuOrganizationInformationArchitectureTests|FullyQualifiedName~NavigationMenuNav28CAdministrationSystemRecordsInformationArchitectureTests|FullyQualifiedName~NavigationMenuNav28BAdministrationScheduledTasksInformationArchitectureTests|FullyQualifiedName~NavigationMenuNav28AAdministrationUsersAccessInformationArchitectureTests|FullyQualifiedName~NavigationMenuNav27ReportsInformationArchitectureTests|FullyQualifiedName~NavigationMenuNav26FileSecurityInformationArchitectureTests|FullyQualifiedName~NavigationMenuNav25BResponsesReconciliationInformationArchitectureTests|FullyQualifiedName~NavigationMenuNav25AMonitoringInformationArchitectureTests|FullyQualifiedName~NavigationMenuNav24CustomersInformationArchitectureTests|FullyQualifiedName~NavigationMenuNav23OperationInformationArchitectureTests|FullyQualifiedName~NavigationMenuNav22ParametersInformationArchitectureTests|FullyQualifiedName~NavigationMenuNav21SeedInfrastructureTests
      reason: NOT_RUN_ENVIRONMENT; dotnet no está instalado en el host.
  decisions_taken:
    - /navigation/menu-items es la ruta SPA funcional inequívoca para administrar la estructura del menú y se reutiliza como Organización del menú, hijo directo de /administracion con Order 40.
    - Se reutiliza MenuItemConfiguration.NavigationAdminId; no se crea una ruta ni un MenuItem nuevo.
    - La autorización preexistente del MenuItem (rol Admin + CanManageUsers) se conserva sin cambios; no se modifican permisos, roles, RolePermissions, grants ni asociaciones existentes.
    - /navigation permanece como prefijo/redirect SPA hacia menu-items y no se publica como entrada adicional.
    - /navigation-logs permanece bajo Registros del sistema; no se mezcla con Organización del menú.
    - Capacidades de la plataforma y Herramientas de prueba permanecen fuera de alcance y sin publicación adicional.
  relevant_findings:
    - AppRouting protege /navigation con roleGuard + permissionGuard para Admin + CanManageUsers; NavigationRoutingModule declara únicamente el redirect vacío y path menu-items hacia NavigationMenuComponent.
    - MenuItemsController acepta navigation/menu-items y api/navigation/menu-items, pero sólo exige [Authorize(Roles = "Admin")].
    - El MenuItem legacy NavigationAdminId ya existe en MenuItemConfiguration con ruta /navigation/menu-items, permiso CanManageUsers y rol Admin.
    - /payment-rail-capability-registry existe en el SPA y no tenía MenuItem en NAV2.8D. NAV2.8E confirmó que el controller referencia CanViewPaymentRailCapabilityRegistry pero la policy efectiva usa fallback ANY entre CanViewPaymentRailCapabilityRegistry, CanManageAch y CanReadAch.
  gaps_pending:
    - Mantener abierto el drift de Organización del menú: menú/SPA exigen Admin + CanManageUsers y el CRUD API sólo Admin.
    - Mantener abierto el drift de Registros del sistema documentado en NAV2.8C, incluido el comportamiento padre/hijo de /logs.
    - Mantener abiertos los gaps de autorización anteriores; NAV2.8D no los corrige.
    - /administracion sigue siendo estructural y sin pantalla SPA propia; el comportamiento general de padres navegables/estructurales en Angular sigue fuera de alcance.
    - Resolver posteriormente la duplicidad activa /ach-cycles y el override CFA de Angular; ambos siguen fuera de alcance.
    - Capacidades de la plataforma quedó resuelta en NAV2.8E; Herramientas de prueba permanece diferida hasta una decisión explícita posterior.
    - La caracterización SPA ANY vs API estricta queda supersedida por NAV2.8E: la policy API efectiva también acepta las tres claims; queda pendiente sólo la normalización del permiso dedicado/persistido en seguridad.
    - Validar runtime/idempotencia con .NET cuando el SDK esté disponible.
  risks_or_blockers:
    - La prueba .NET focal no puede ejecutarse por ausencia de dotnet; NAV2.8D queda IMPLEMENTED_UNVERIFIED_ENVIRONMENT.
    - La reorganización depende del reconciliador canónico Order 100; la verificación runtime/idempotente con EF real queda pendiente hasta disponer de .NET.
  next_increment_recommendation: No avanzar automáticamente a NAV2.9. Se recomienda una decisión explícita separada sobre Capacidades de la plataforma antes de continuar; Herramientas de prueba permanece diferida por su modelo de autorización técnico NAV3/NAV5.

nav2_8e_execution:
  phase_executed: NAV2.8E
  phase_status: IMPLEMENTED_UNVERIFIED_ENVIRONMENT
  files_modified:
    - MEMORY.md
    - docs/ai/navigation-menu/MEMORY_STATE.md
    - docs/architecture/NAVIGATION_MENU_IMPLEMENTATION_PLAN.md
    - docs/architecture/NAVIGATION_MENU_INFORMATION_ARCHITECTURE.md
    - docs/ux/NAVIGATION_MENU_ROUTE_MATRIX.md
    - src/Cfa.ACHInterbank.Persistence/Navigation/Seeders/NavigationInformationArchitectureSeeder.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav28BAdministrationScheduledTasksInformationArchitectureTests.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav28CAdministrationSystemRecordsInformationArchitectureTests.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav28DAdministrationMenuOrganizationInformationArchitectureTests.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav28EAdministrationPlatformCapabilitiesInformationArchitectureTests.cs
  tests_executed:
    - command: python scripts/validate-solution-projects.py
      result: PASS
    - command: NAV2.8E static focal assertions sobre clasificación funcional, ruta SPA, comportamiento read-only, policy efectiva, autorización del nuevo MenuItem y documentación
      result: PASS
    - command: C# delimiter sanity sobre seeder y pruebas NAV2.8B-E modificadas/agregadas
      result: PASS
    - command: Comparación SHA-256 por archivo entre el ZIP FULL NAV2.8D y el workspace NAV2.8E
      result: PASS; 1 archivo agregado, 9 modificados, 0 eliminados; ningún archivo bajo web/ cambió.
    - command: Verificación ZIP FULL NAV2.8E contra el workspace final
      result: PASS; 4194 archivos, sin faltantes, extras ni diferencias de contenido.
  tests_passed:
    - python scripts/validate-solution-projects.py
    - NAV2.8E static focal assertions
    - C# delimiter sanity
    - auditoría estática de cobertura de rutas MENU* de la matriz objetivo
    - comparación SHA-256 ZIP NAV2.8D/workspace NAV2.8E dentro del alcance declarado
    - integridad del ZIP FULL NAV2.8E contra el workspace final
  tests_failed: []
  tests_deferred:
    - command: dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj --filter FullyQualifiedName~NavigationMenuNav28EAdministrationPlatformCapabilitiesInformationArchitectureTests|FullyQualifiedName~NavigationMenuNav28DAdministrationMenuOrganizationInformationArchitectureTests|FullyQualifiedName~NavigationMenuNav28CAdministrationSystemRecordsInformationArchitectureTests|FullyQualifiedName~NavigationMenuNav28BAdministrationScheduledTasksInformationArchitectureTests|FullyQualifiedName~NavigationMenuNav28AAdministrationUsersAccessInformationArchitectureTests|FullyQualifiedName~NavigationMenuNav27ReportsInformationArchitectureTests|FullyQualifiedName~NavigationMenuNav26FileSecurityInformationArchitectureTests|FullyQualifiedName~NavigationMenuNav25BResponsesReconciliationInformationArchitectureTests|FullyQualifiedName~NavigationMenuNav25AMonitoringInformationArchitectureTests|FullyQualifiedName~NavigationMenuNav24CustomersInformationArchitectureTests|FullyQualifiedName~NavigationMenuNav23OperationInformationArchitectureTests|FullyQualifiedName~NavigationMenuNav22ParametersInformationArchitectureTests|FullyQualifiedName~NavigationMenuNav21SeedInfrastructureTests
      reason: NOT_RUN_ENVIRONMENT; dotnet no está instalado en el host.
  decisions_taken:
    - /payment-rail-capability-registry corresponde funcionalmente a Capacidades de la plataforma y pertenece a Administración, no a Administración de parámetros ni a Herramientas de prueba.
    - Se crea un MenuItem nuevo con Label Capacidades de la plataforma, Parent Administración y Order 50; la ruta funcional existente no cambia.
    - El nuevo MenuItem reutiliza CanReadAch + roles Admin/ACH.Operator porque esa autorización ya es aceptada por el guard SPA y por la policy efectiva del API; no se amplía acceso backend.
    - No se crea CanViewPaymentRailCapabilityRegistry como Permission persistida y no se modifican permissions, roles, RolePermissions, grants, policies SPA/API ni autorizaciones de MenuItem existentes.
    - Herramientas de prueba permanece fuera de alcance y sin cambios.
  relevant_findings:
    - La pantalla se describe a sí misma como consulta administrativa en modo solo lectura para gobernanza multi-riel, muestra estado efectivo por riel y declara explícitamente que no permite edición.
    - El servicio Angular del feature usa sólo GET y el controller expone sólo HttpGet.
    - El controller referencia la policy CanViewPaymentRailCapabilityRegistry, pero DependencyInjectionService la define con fallback ANY sobre CanViewPaymentRailCapabilityRegistry, CanManageAch y CanReadAch; por tanto el acceso efectivo del API coincide en permisos con el SPA.
    - CanViewPaymentRailCapabilityRegistry no está sembrado en PermissionConfiguration ni en RolePermissions actuales; crear ese permiso habría violado el alcance NAV2.8E.
  gaps_pending:
    - La nomenclatura de policy dedicada CanViewPaymentRailCapabilityRegistry no coincide con su fallback efectivo y esa claim no existe como Permission persistida; resolver en la futura fase de seguridad si se endurece/siembra el permiso o se formaliza el fallback.
    - Mantener abiertos los gaps de autorización anteriores, incluido el drift de Organización del menú y el comportamiento padre/hijo de /logs.
    - /administracion sigue siendo estructural y sin pantalla SPA propia; el comportamiento general de padres navegables/estructurales en Angular sigue fuera de alcance.
    - Resolver posteriormente la duplicidad activa /ach-cycles y el override CFA de Angular; ambos siguen fuera de alcance.
    - Herramientas de prueba permanece diferida; no se publica ni se modifica su modelo de autorización en NAV2.8E.
    - Validar runtime/idempotencia con .NET cuando el SDK esté disponible.
  risks_or_blockers:
    - La prueba .NET focal no puede ejecutarse por ausencia de dotnet; NAV2.8E queda IMPLEMENTED_UNVERIFIED_ENVIRONMENT.
    - El nuevo MenuItem usa el fallback persistido CanReadAch y por diseño no representa a un principal hipotético con sólo la claim no persistida CanViewPaymentRailCapabilityRegistry; normalizar ese modelo requiere una decisión de seguridad posterior.
  next_increment_recommendation: No avanzar automáticamente a NAV2.9. Si se autoriza NAV2.9, limitarlo a la decisión/arquitectura de Herramientas de prueba; si su autorización técnica no puede expresarse con permisos existentes sin ampliar acceso, detener publicación y diferir la creación/endurecimiento del permiso a NAV3.

nav2_9_execution:
  phase_executed: NAV2.9
  phase_status: IMPLEMENTED_UNVERIFIED_ENVIRONMENT
  files_modified:
    - MEMORY.md
    - docs/ai/navigation-menu/MEMORY_STATE.md
    - docs/architecture/NAVIGATION_MENU_IMPLEMENTATION_PLAN.md
    - docs/architecture/NAVIGATION_MENU_INFORMATION_ARCHITECTURE.md
    - docs/ux/NAVIGATION_MENU_ROUTE_MATRIX.md
    - src/Cfa.ACHInterbank.Persistence/Navigation/Seeders/NavigationInformationArchitectureSeeder.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav23OperationInformationArchitectureTests.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav24CustomersInformationArchitectureTests.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav25AMonitoringInformationArchitectureTests.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav25BResponsesReconciliationInformationArchitectureTests.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav26FileSecurityInformationArchitectureTests.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav27ReportsInformationArchitectureTests.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav28AAdministrationUsersAccessInformationArchitectureTests.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav28BAdministrationScheduledTasksInformationArchitectureTests.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav28CAdministrationSystemRecordsInformationArchitectureTests.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav28DAdministrationMenuOrganizationInformationArchitectureTests.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav28EAdministrationPlatformCapabilitiesInformationArchitectureTests.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav29TestToolsInformationArchitectureTests.cs
  tests_executed:
    - command: python scripts/validate-solution-projects.py
      result: PASS
    - command: NAV2.9 static focal assertions sobre clasificación de superficies, rutas, jerarquía/órdenes, autorización sin ampliación, gating por ambiente preexistente y alcance
      result: PASS; 30 checks focales.
    - command: C# delimiter sanity sobre seeder y pruebas de navegación modificadas/agregadas
      result: PASS; 13 archivos C# modificados/agregados.
    - command: Comparación SHA-256 por archivo entre el ZIP FULL NAV2.8E y el workspace NAV2.9
      result: PASS; 1 archivo agregado, 17 modificados, 0 eliminados; 0 cambios bajo web/.
    - command: Verificación ZIP FULL NAV2.9 contra el workspace final
      result: PASS; 4195 archivos, sin faltantes, extras ni diferencias de contenido.
  tests_passed:
    - python scripts/validate-solution-projects.py
    - NAV2.9 static focal assertions
    - C# delimiter sanity
    - auditoría estática de cobertura de rutas MENU* de la matriz objetivo
    - comparación SHA-256 ZIP NAV2.8E/workspace NAV2.9 dentro del alcance declarado
    - integridad del ZIP FULL NAV2.9 contra el workspace final
  tests_failed: []
  tests_deferred:
    - command: dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj --filter FullyQualifiedName~NavigationMenuNav29TestToolsInformationArchitectureTests|FullyQualifiedName~NavigationMenuNav28EAdministrationPlatformCapabilitiesInformationArchitectureTests|FullyQualifiedName~NavigationMenuNav28DAdministrationMenuOrganizationInformationArchitectureTests|FullyQualifiedName~NavigationMenuNav28CAdministrationSystemRecordsInformationArchitectureTests|FullyQualifiedName~NavigationMenuNav28BAdministrationScheduledTasksInformationArchitectureTests|FullyQualifiedName~NavigationMenuNav28AAdministrationUsersAccessInformationArchitectureTests|FullyQualifiedName~NavigationMenuNav27ReportsInformationArchitectureTests|FullyQualifiedName~NavigationMenuNav26FileSecurityInformationArchitectureTests|FullyQualifiedName~NavigationMenuNav25BResponsesReconciliationInformationArchitectureTests|FullyQualifiedName~NavigationMenuNav25AMonitoringInformationArchitectureTests|FullyQualifiedName~NavigationMenuNav24CustomersInformationArchitectureTests|FullyQualifiedName~NavigationMenuNav23OperationInformationArchitectureTests|FullyQualifiedName~NavigationMenuNav22ParametersInformationArchitectureTests|FullyQualifiedName~NavigationMenuNav21SeedInfrastructureTests
      reason: NOT_RUN_ENVIRONMENT; dotnet no está instalado en el host.
  decisions_taken:
    - /uat se canonicaliza como Herramientas de prueba Order 90 y se reutiliza el MenuItem existente UatSimulatorsId; el seed canónico no depende de ambiente.
    - /uat/nacha-inbound-simulator se reutiliza como Simulador de archivos recibidos Order 10 y permanece activo con sus asociaciones MenuItem existentes Admin/ACH.Operator + CanManageAch/CanReadAch intactas.
    - /ach/nacha/soap-uat-console se clasifica como Pruebas de servicios Order 20; se crea el MenuItem canónico pero queda inactivo hasta NAV3.
    - El MenuItem inactivo de Pruebas de servicios reutiliza de forma provisional Admin/ACH.Operator + CanManageAch y no crea permisos, roles, RolePermissions, grants, policies, guards ni autorización API.
    - /api/uat/contrapartidas y /Tests no se publican porque no tienen superficie SPA navegable independiente; el panel nacha-config-test-panel permanece embebido y no se trata como destino de menú.
    - NAV2.9 no modifica Angular ni elimina/refactoriza gates por ambiente preexistentes; sólo evita introducir gating por ambiente en el seed canónico.
  relevant_findings:
    - La simulación de archivos recibidos genera fixtures NACHA-M de recepción sin transmisión automática y es una herramienta de prueba real, no una función operacional ordinaria.
    - La consola SOAP/UAT es read-only y orientada a candidatos SOAP, preparación UAT, bloqueos, simulación, resiliencia e idempotencia; no ejecuta SOAP.
    - La consola SOAP tiene guardas SPA efectivas que combinan CanReadAch heredado del padre /ach con CanManageAch de la ruta hija, pero la autorización de menú sólo puede expresar OR entre permisos asociados.
    - En producción el GetMenuForCurrentUserHandler actual elimina /uat y /uat/**; un hijo activo con ruta /ach/nacha/soap-uat-console podría sobrevivir al filtro y reconstruirse como raíz al faltar el padre. Mantenerlo inactivo evita exposición adicional hasta NAV3.
    - Los controllers del simulador y consola SOAP/UAT devuelven 404 fuera de entorno autorizado/UAT-like; UatContrapartidas además depende de ACH_SOAP_LIVE_TESTS=true. Estos gates son deuda explícita para NAV3 y no fueron alterados.
  gaps_pending:
    - NAV3 debe crear o seleccionar un permiso técnico explícito de Herramientas de prueba, asignarlo mediante permiso → rol → usuario, alinear menú/SPA/API y activar Pruebas de servicios sin checks hardcodeados por nombre de rol.
    - NAV3 debe resolver el gating por ambiente preexistente de /uat en GetMenuForCurrentUserHandler y de los endpoints técnicos si la decisión aprobada exige disponibilidad también en producción basada sólo en autorización.
    - La ejecución focal/runtime/idempotente del reconciliador canónico sigue pendiente porque el host no dispone de dotnet; por ello NAV2 no se declara CLOSED aunque el diseño canónico principal queda resuelto a nivel de código/documentación.
    - Permanecen como deuda diferida ya conocida la duplicidad /ach-cycles, el override CFA, el comportamiento padre/hijo de /logs, padres navegables en Angular y gaps de autorización acumulados que el usuario excluyó de NAV2.9.
  risks_or_blockers:
    - La prueba .NET focal no puede ejecutarse por ausencia de dotnet; NAV2.9 y NAV2 quedan IMPLEMENTED_UNVERIFIED_ENVIRONMENT.
    - Pruebas de servicios está deliberadamente inactiva: activarla antes de NAV3 podría modelar mal la autorización y, con el filtro actual por prefijo /uat, provocar una entrada huérfana/raíz en producción.
  next_increment_recommendation: El diseño canónico principal del menú puede considerarse resuelto a nivel de fuente tras NAV2.9, pero NAV2 no debe declararse CLOSED hasta ejecutar validación .NET focal/idempotente en un entorno con SDK. Con decisión explícita del usuario, el siguiente incremento recomendado es NAV3 para el modelo de autorización técnico de Herramientas de prueba y normalización de gates por ambiente, sin avanzar automáticamente.

nav2_10a_execution:
  phase_executed: NAV2.10A
  phase_status: IMPLEMENTED_UNVERIFIED_ENVIRONMENT
  files_modified:
    - MEMORY.md
    - docs/ai/navigation-menu/MEMORY_STATE.md
    - docs/architecture/NAVIGATION_MENU_IMPLEMENTATION_PLAN.md
    - docs/architecture/NAVIGATION_MENU_INFORMATION_ARCHITECTURE.md
    - docs/ux/NAVIGATION_MENU_ROUTE_MATRIX.md
    - src/Cfa.ACHInterbank.Persistence/Navigation/Seeders/NavigationInformationArchitectureSeeder.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav22ParametersInformationArchitectureTests.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav23OperationInformationArchitectureTests.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav24CustomersInformationArchitectureTests.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav25AMonitoringInformationArchitectureTests.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav25BResponsesReconciliationInformationArchitectureTests.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav26FileSecurityInformationArchitectureTests.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav27ReportsInformationArchitectureTests.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav28AAdministrationUsersAccessInformationArchitectureTests.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav28BAdministrationScheduledTasksInformationArchitectureTests.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav28CAdministrationSystemRecordsInformationArchitectureTests.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav28DAdministrationMenuOrganizationInformationArchitectureTests.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav210AStructuralNormalizationTests.cs
  - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav210BCatalogsCoverageTests.cs
  tests_executed:
    - command: python scripts/validate-solution-projects.py
      result: PASS
    - command: NAV2.10A static focal assertions sobre Inicio, órdenes raíz 10..90, normalización /ach-cycles, legacy aprobados, Pruebas de servicios inactiva y alcance sin Angular/seguridad
      result: PASS; 19 checks focales.
    - command: C# delimiter sanity sobre seeder y pruebas de navegación modificadas/agregadas
      result: PASS; 13 archivos C# modificados/agregados.
    - command: Comparación SHA-256 por archivo entre el ZIP FULL NAV2.9 y el workspace NAV2.10A
      result: PASS; 1 archivo agregado, 17 modificados, 0 eliminados; 0 cambios bajo web/ y 0 archivos de seguridad/autorización modificados.
    - command: Verificación ZIP FULL NAV2.10A contra el workspace final
      result: PASS; 4196 archivos, sin faltantes, extras ni diferencias de contenido.
  tests_passed:
    - python scripts/validate-solution-projects.py
    - NAV2.10A static focal assertions
    - C# delimiter sanity
    - auditoría estática de cobertura de rutas MENU* de la matriz objetivo
    - comparación SHA-256 ZIP NAV2.9/workspace NAV2.10A dentro del alcance declarado
    - integridad del ZIP FULL NAV2.10A contra el workspace final
  tests_failed: []
  tests_deferred:
    - command: dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj --filter FullyQualifiedName~NavigationMenuNav210AStructuralNormalizationTests|FullyQualifiedName~NavigationMenuNav29TestToolsInformationArchitectureTests|FullyQualifiedName~NavigationMenuNav28EAdministrationPlatformCapabilitiesInformationArchitectureTests|FullyQualifiedName~NavigationMenuNav28DAdministrationMenuOrganizationInformationArchitectureTests|FullyQualifiedName~NavigationMenuNav28CAdministrationSystemRecordsInformationArchitectureTests|FullyQualifiedName~NavigationMenuNav28BAdministrationScheduledTasksInformationArchitectureTests|FullyQualifiedName~NavigationMenuNav28AAdministrationUsersAccessInformationArchitectureTests|FullyQualifiedName~NavigationMenuNav27ReportsInformationArchitectureTests|FullyQualifiedName~NavigationMenuNav26FileSecurityInformationArchitectureTests|FullyQualifiedName~NavigationMenuNav25BResponsesReconciliationInformationArchitectureTests|FullyQualifiedName~NavigationMenuNav25AMonitoringInformationArchitectureTests|FullyQualifiedName~NavigationMenuNav24CustomersInformationArchitectureTests|FullyQualifiedName~NavigationMenuNav23OperationInformationArchitectureTests|FullyQualifiedName~NavigationMenuNav22ParametersInformationArchitectureTests|FullyQualifiedName~NavigationMenuNav21SeedInfrastructureTests
      reason: NOT_RUN_ENVIRONMENT; dotnet no está instalado en el host.
  decisions_taken:
    - /dashboard se canonicaliza como Inicio Order 10 sin cambiar su ruta ni autorización, completando los nueve dominios raíz aprobados en órdenes 10..90.
    - MenuItemConfiguration.AchCyclesId se conserva como Ciclos de operación Order 30 bajo Administración de parámetros y es la única representación activa de /ach-cycles; cualquier fila duplicada de esa misma ruta se conserva pero queda inactiva.
    - Los legacy ya decididos /customer-third-parties, /soap-integrations y /scheduler permanecen inactivos; /transactions/bulk-create y las rutas manuales de seguridad continúan fuera del menú.
    - Pruebas de servicios permanece inactiva pendiente de NAV3; NAV2.10A no toca Angular, permisos, roles, RolePermissions, grants, policies, guards ni gating por ambiente.
  relevant_findings:
    - Antes de NAV2.10A /dashboard seguía con el copy/Order del seeder legacy y /ach-cycles conservaba dos MenuItem activos sobre la misma ruta; ambas inconsistencias estructurales quedan normalizadas en el reconciliador final.
    - La prueba focal nueva valida los nueve dominios raíz exactos, ausencia de empates de Order entre hermanos activos, única entrada visible /ach-cycles, conservación de autorización/rutas, legacy aprobados e idempotencia del estado estructural.
    - Permanecen rutas funcionales del catálogo extendido sin clasificación canónica: /catalogs/company-entry-descriptions, /catalogs/document-types, /catalogs/person-types, /catalogs/gender-types, /catalogs/phone-types, /catalogs/email-types, /catalogs/address-types y /catalogs/transaction-codes; NAV2.10A no las publica.
  gaps_pending:
    - Ejecutar las pruebas .NET focales/acumulativas e idempotentes del reconciliador en un entorno con SDK antes de declarar NAV2 CLOSED.
    - Los gaps de autorización acumulados, el gating por ambiente de Herramientas de prueba y el modelo técnico de permisos se mantienen para NAV3.
    - Override CFA, padres navegables y comportamiento padre/hijo de /logs permanecen fuera de NAV2.10A; la cobertura de rutas de catálogo extendido sigue diferida a una decisión posterior.
  risks_or_blockers:
    - dotnet no está disponible en el host; el estado estructural está implementado y validado estáticamente, pero NAV2 permanece IMPLEMENTED_UNVERIFIED_ENVIRONMENT.
  next_increment_recommendation: No se identifica una necesidad estructural de NAV2.10B a nivel de fuente. Primero ejecutar la validación .NET focal/idempotente en un entorno con SDK para poder cerrar NAV2; después, y sólo con autorización explícita del usuario, preparar NAV3. No avanzar automáticamente.


nav2_10b_execution:
  phase_executed: NAV2.10B
  phase_status: IMPLEMENTED_UNVERIFIED_ENVIRONMENT
  files_modified:
    - MEMORY.md
    - docs/ai/navigation-menu/MEMORY_STATE.md
    - docs/architecture/NAVIGATION_MENU_IMPLEMENTATION_PLAN.md
    - docs/architecture/NAVIGATION_MENU_INFORMATION_ARCHITECTURE.md
    - docs/ux/NAVIGATION_MENU_ROUTE_MATRIX.md
    - src/Cfa.ACHInterbank.Persistence/Navigation/Seeders/NavigationInformationArchitectureSeeder.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav22ParametersInformationArchitectureTests.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav23OperationInformationArchitectureTests.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav24CustomersInformationArchitectureTests.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav25AMonitoringInformationArchitectureTests.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav25BResponsesReconciliationInformationArchitectureTests.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav26FileSecurityInformationArchitectureTests.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav27ReportsInformationArchitectureTests.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav28AAdministrationUsersAccessInformationArchitectureTests.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav28BAdministrationScheduledTasksInformationArchitectureTests.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav28CAdministrationSystemRecordsInformationArchitectureTests.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav28DAdministrationMenuOrganizationInformationArchitectureTests.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav210BCatalogsCoverageTests.cs
  tests_executed:
    - command: python scripts/validate-solution-projects.py
      result: PASS
    - command: NAV2.10B static focal assertions sobre las nueve rutas SPA, seed canónico, autorización CanManageAch y legacy de fechas especiales
      result: PASS; 30 checks focales.
    - command: C# delimiter sanity sobre seeder y pruebas de navegación modificadas/agregadas
      result: PASS.
    - command: Auditoría estática de cobertura de rutas MENU* de docs/ux/NAVIGATION_MENU_ROUTE_MATRIX.md contra seed/config de navegación
      result: PASS; 74 rutas no-paramétricas previstas por la matriz tienen representación en fuente de navegación.
    - command: Comparación SHA-256 por archivo entre el ZIP FULL NAV2.10A y el workspace NAV2.10B
      result: PASS; 1 archivo agregado, 17 modificados, 0 eliminados; sólo seed, documentación y pruebas NAV2.10B; 0 cambios bajo web/ y 0 cambios en definiciones de permisos/roles/grants/policies/guards.
    - command: Verificación ZIP FULL NAV2.10B contra el workspace final
      result: PASS; sin faltantes, extras ni diferencias de contenido.
  tests_passed:
    - python scripts/validate-solution-projects.py
    - NAV2.10B static focal assertions
    - C# delimiter sanity
    - auditoría estática de cobertura de rutas MENU* de la matriz objetivo
    - comparación SHA-256 ZIP NAV2.10A/workspace NAV2.10B dentro del alcance declarado
    - integridad del ZIP FULL NAV2.10B contra el workspace final
  tests_failed: []
  tests_deferred:
    - command: dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj --filter FullyQualifiedName~NavigationMenuNav210BCatalogsCoverageTests|FullyQualifiedName~NavigationMenuNav210AStructuralNormalizationTests|FullyQualifiedName~NavigationMenuNav22ParametersInformationArchitectureTests
      reason: NOT_RUN_ENVIRONMENT; dotnet no está instalado en el host.
  decisions_taken:
    - Catálogos permanece bajo Administración de parámetros Order 70 con exactamente nueve hijos activos en órdenes 10..90 según la IA aprobada.
    - /catalogs/financial-institutions reutiliza MenuItemConfiguration.FinancialInstitutionsId, se humaniza a Entidades financieras Order 10 y conserva íntegramente sus asociaciones de autorización existentes.
    - Las ocho rutas pendientes existen en el SPA y sus pantallas/APIs CRUD usan CanManageAch; al no existir MenuItem previo se crean con Admin/ACH.Operator + CanManageAch usando identificadores existentes, sin modificar permisos, roles ni grants.
    - /catalogs/clearing-house-special-dates conserva su ruta y autorización legacy, pero su MenuItem queda inactivo porque es un redirect contextual por cámara y no forma parte del agrupador canónico principal.
    - Pruebas de servicios permanece inactiva y los nueve dominios raíz y la representación única de /ach-cycles permanecen sin cambios.
  relevant_findings:
    - Las nueve rutas objetivo están declaradas en CatalogsRoutingModule. CompanyEntryDescriptionsController y CatalogTypesController exigen CanManageAch para sus operaciones; FinancialInstitutions conserva guard SPA CanManageAch y escritura API CanManageAch, mientras sus lecturas API también admiten CanReadAch.
    - El único drift de autorización relevante dentro del grupo es preexistente en Entidades financieras: el GET API admite CanReadAch mientras la pantalla administrativa y su MenuItem requieren CanManageAch (además de los roles de menú existentes). NAV2.10B no corrige ese drift.
    - El padre /catalogs conserva CanReadCatalogs; con los grants seed actuales Admin y ACH.Operator poseen tanto CanReadCatalogs como CanManageAch, por lo que la publicación de los ocho hijos no amplía acceso efectivo.
    - La auditoría estática de la matriz objetivo confirma cobertura en fuente para las 74 rutas MENU* no-paramétricas; no queda otra ruta principal prevista por NAV-MENU-IA-001 sin clasificación canónica de fuente. Pruebas de servicios sigue clasificada pero inactiva hasta NAV3.
  gaps_pending:
    - Ejecutar las pruebas .NET focales/acumulativas e idempotentes del reconciliador en un entorno con SDK antes de declarar NAV2 CLOSED.
    - El drift de lectura de Entidades financieras (GET API CanReadAch frente a pantalla/menu CanManageAch) queda documentado para una futura fase de seguridad; no bloquea la IA canónica.
    - Los gaps de autorización acumulados, gating por ambiente de Herramientas de prueba, override CFA, padre/hijo de /logs y padres navegables Angular siguen diferidos según decisiones previas.
  risks_or_blockers:
    - dotnet no está disponible en el host; NAV2.10B y NAV2 permanecen IMPLEMENTED_UNVERIFIED_ENVIRONMENT pese a que la cobertura funcional canónica prevista queda completa a nivel de fuente.
  next_increment_recommendation: NAV2 puede considerarse funcionalmente implementado en fuente después de NAV2.10B. Recomendar cierre administrativo como IMPLEMENTED_UNVERIFIED_ENVIRONMENT y preparación de NAV3 sólo tras decisión explícita del usuario; el único bloqueo para CLOSED es la validación .NET focal/idempotente en un entorno con SDK.

nav3_1_execution:
  phase_executed: NAV3.1
  phase_status: IMPLEMENTED_UNVERIFIED_ENVIRONMENT
  files_modified:
    - MEMORY.md
    - docs/ai/navigation-menu/MEMORY_STATE.md
    - docs/security/NAVIGATION_MENU_SECURITY_MODEL.md
    - src/Cfa.ACHInterbank.Application/Security/FineGrainedPermissions.cs
    - src/Cfa.ACHInterbank.Persistence/Security/Seeders/TestToolsPermissionSeeder.cs
    - tests/Cfa.ACHInterbank.Tests/Authorization/FineGrainedPermissionsRegistrationTests.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav31TestToolsPermissionFoundationTests.cs
  tests_executed:
    - command: python scripts/validate-solution-projects.py
      result: PASS
    - command: NAV3.1 static focal assertions sobre permiso dedicado, seeder idempotente, roles existentes, ausencia de grants, autorizaciones vigentes y gating por ambiente
      result: PASS; 39 checks focales.
    - command: C# delimiter sanity sobre los cuatro archivos C# modificados/agregados
      result: PASS.
    - command: Comparación SHA-256 por archivo entre el ZIP FULL NAV2.10B y el workspace NAV3.1
      result: PASS; 2 archivos agregados, 5 modificados, 0 eliminados; 0 cambios bajo web/ y sin cambios en RoleConfiguration, RolePermissionConfiguration, MenuItemPermission/MenuItemRole, controllers, guards ni NavigationInformationArchitectureSeeder.
    - command: Verificación ZIP FULL NAV3.1 contra el workspace final
      result: PASS; 4199 archivos, sin faltantes, extras ni diferencias de contenido.
  tests_passed:
    - python scripts/validate-solution-projects.py
    - NAV3.1 static focal assertions
    - C# delimiter sanity
    - comparación SHA-256 ZIP NAV2.10B/workspace NAV3.1 dentro del alcance declarado
    - integridad del ZIP FULL NAV3.1 contra el workspace final
  tests_failed: []
  tests_deferred:
    - command: dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj --filter FullyQualifiedName~NavigationMenuNav31TestToolsPermissionFoundationTests|FullyQualifiedName~FineGrainedPermissionsRegistrationTests|FullyQualifiedName~NavigationMenuNav29TestToolsInformationArchitectureTests|FullyQualifiedName~NavigationMenuNav210BCatalogsCoverageTests|FullyQualifiedName~NavigationMenuNav210AStructuralNormalizationTests
      reason: NOT_RUN_ENVIRONMENT; dotnet no está instalado en el host. La deuda runtime heredada de NAV2 permanece abierta y NAV2 conserva IMPLEMENTED_UNVERIFIED_ENVIRONMENT.
  decisions_taken:
    - Se crea TestTools.Access porque ningún permiso existente tiene la semántica general exacta de Acceso a herramientas de prueba; NachaSimulator.Read y sus permisos hermanos son específicos de acciones del simulador.
    - El permiso se persiste de forma idempotente mediante TestToolsPermissionSeeder y se incorpora a FineGrainedPermissions.AllPermissions para usar el mecanismo estándar de claims/policies, sin aplicarlo todavía a rutas existentes.
    - Los únicos roles existentes son Admin y ACH.Operator. Ninguno recibe TestTools.Access en NAV3.1: ACH.Operator está excluido por diseño y Admin no se equipara implícitamente a Superadministrador.
    - No se crea ningún rol de Superadministración, Desarrollo o Soporte y no se agregan RolePermissions/grants por defecto.
    - La autorización vigente de ambas herramientas, el gating por ambiente y el estado inactivo de Pruebas de servicios permanecen intactos.
  relevant_findings:
    - RoleConfiguration sólo siembra Admin (descripción Acceso completo al sistema) y ACH.Operator; no hay un rol separado de Superadministrador ni un rol técnico de Desarrollo/Soporte.
    - FineGrainedPermissions ya contiene NachaSimulator.Read/GenerateIncoming/GenerateDifferential/Download/Live, pero su ámbito es específico del simulador y no expresa acceso general al dominio Herramientas de prueba.
    - AuthService emite claims permission desde RolePermissions, por lo que TestTools.Access queda técnicamente delegable pero no será emitido a usuarios actuales hasta que exista una asignación explícita de rol-permiso.
  gaps_pending:
    - NAV3.2 debe decidir y sembrar el/los roles técnicos o superadministrativos apropiados antes de otorgar TestTools.Access; no existe hoy un rol inequívoco que cumpla esa función.
    - Sólo después de esa decisión NAV3.2 debe alinear MenuItemPermission, guards SPA y policies/controllers API de /uat/nacha-inbound-simulator y /ach/nacha/soap-uat-console con TestTools.Access, retirar el gating por ambiente y activar Pruebas de servicios cuando la autorización efectiva esté cerrada.
    - La deuda runtime .NET de NAV2 y NAV3.1 permanece pendiente; NAV2 no se marca CLOSED.
    - Los demás gaps de seguridad acumulados permanecen fuera de NAV3.1.
  risks_or_blockers:
    - TestTools.Access no otorga acceso a ningún usuario actual por diseño; intentar exigirlo antes de crear/asignar un rol técnico dejaría las herramientas sin principals autorizados.
    - dotnet no está disponible en el host, por lo que la idempotencia/runtime del nuevo seeder y la deuda NAV2 no pudieron ejecutarse.
  next_increment_recommendation: No ejecutar NAV3.2 automáticamente. Con autorización explícita, NAV3.2 debe resolver primero el modelo de roles técnicos y luego migrar coherentemente menú/SPA/API/gating al permiso TestTools.Access sin checks hardcodeados de nombres de rol.

nav3_2a_execution:
  phase_executed: NAV3.2A
  phase_status: IMPLEMENTED_UNVERIFIED_ENVIRONMENT
  files_modified:
    - MEMORY.md
    - docs/ai/navigation-menu/MEMORY_STATE.md
    - docs/security/NAVIGATION_MENU_SECURITY_MODEL.md
    - src/Cfa.ACHInterbank.Persistence/Security/Seeders/TestToolsAdminRolePermissionSeeder.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav32ATestToolsAdminRoleGrantTests.cs
  tests_executed:
    - command: python scripts/validate-solution-projects.py
      result: PASS
    - command: NAV3.2A static focal assertions sobre identidad estable de Admin, grant TestTools.Access, exclusión de ACH.Operator, ausencia de roles/grants de usuario nuevos, autorizaciones vigentes y gating por ambiente
      result: PASS; 25 checks focales.
    - command: C# delimiter sanity sobre los dos archivos C# agregados
      result: PASS.
    - command: Comparación SHA-256 por archivo entre el ZIP FULL NAV3.1 y el workspace NAV3.2A
      result: PASS; 2 archivos agregados, 3 modificados, 0 eliminados; 0 cambios bajo web/ y sin cambios en MenuItemPermission/MenuItemRole, guards, controllers, policies, RoleConfiguration, RolePermissionConfiguration ni NavigationInformationArchitectureSeeder.
    - command: Verificación ZIP FULL NAV3.2A contra el workspace final
      result: PASS; 4201 archivos, sin faltantes, extras ni diferencias de contenido.
  tests_passed:
    - python scripts/validate-solution-projects.py
    - NAV3.2A static focal assertions
    - C# delimiter sanity
    - comparación SHA-256 ZIP NAV3.1/workspace NAV3.2A dentro del alcance declarado
    - integridad del ZIP FULL NAV3.2A contra el workspace final
  tests_failed: []
  tests_deferred:
    - command: dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj --filter FullyQualifiedName~NavigationMenuNav32ATestToolsAdminRoleGrantTests|FullyQualifiedName~NavigationMenuNav31TestToolsPermissionFoundationTests|FullyQualifiedName~FineGrainedPermissionsRegistrationTests|FullyQualifiedName~NavigationMenuNav210BCatalogsCoverageTests|FullyQualifiedName~NavigationMenuNav210AStructuralNormalizationTests
      reason: NOT_RUN_ENVIRONMENT; dotnet no está instalado en el host. La deuda runtime heredada de NAV2 y NAV3.1 permanece abierta; NAV2 conserva IMPLEMENTED_UNVERIFIED_ENVIRONMENT.
  decisions_taken:
    - RoleConfiguration confirma Admin con Id estable 1f8602da-6415-43f8-b61d-cb396f8577f1 y descripción Acceso completo al sistema; por decisión funcional explícita NAV3.2A lo usa como rol administrativo existente de máximo privilegio.
    - TestToolsAdminRolePermissionSeeder siembra de forma idempotente únicamente Admin -> TestTools.Access en RolePermissions, después de TestToolsPermissionSeeder.
    - ACH.Operator no recibe TestTools.Access; no se crea Superadministrador ni Desarrollo/Soporte y no se introduce ningún grant directo a usuarios.
    - El modelo RolePermission actual usa sólo RoleId + PermissionId como clave compuesta y no distingue flags de grant de sistema/protegido/editable.
    - Las autorizaciones actuales de ambas herramientas, su gating por ambiente y el estado inactivo de Pruebas de servicios permanecen intactos.
  relevant_findings:
    - Los únicos roles sembrados siguen siendo Admin y ACH.Operator. No existe un rol Desarrollo/Soporte técnico ni una distinción de rol separada entre Admin y Superadministrador.
    - TestTools.Access queda ahora delegable y emitible a usuarios que posean Admin mediante el mecanismo estándar RolePermissions -> claims, pero las herramientas todavía no lo exigen.
    - Los permisos específicos NachaSimulator.*, incluido NachaSimulator.Read, permanecen sin cambios.
  - Desde NAV3.2B los MenuItem /uat, /uat/nacha-inbound-simulator y /ach/nacha/soap-uat-console tienen únicamente TestTools.Access y ninguna asociación MenuItemRole directa; Pruebas de servicios sigue inactiva.
  - Las rutas SPA de ambas herramientas declaran TestTools.Access; el padre /uat elimina fallbacks genéricos, pero la consola SOAP aún hereda las guardas del padre compartido /ach como restricción adicional.
  - Los controllers NachaInboundSimulatorController y NachaSoapUatConsoleController exigen TestTools.Access a nivel de clase; policies específicas preexistentes siguen siendo adicionales.
  gaps_pending:
    - NAV3.2B debe alinear MenuItemPermission, guards SPA y policies/controllers API de las dos herramientas con TestTools.Access sin checks por nombre de rol ni ampliación de acceso.
    - Pruebas de servicios debe permanecer inactiva hasta que el permiso dedicado proteja efectivamente menú, SPA y API.
    - El gating por Production/UAT-like/environment permanece y sólo debe retirarse en un incremento explícitamente autorizado después de cerrar la autorización por permiso.
    - No existe un rol Desarrollo/Soporte; su creación futura requiere decisión funcional sobre propósito, población, permisos, administración y elegibilidad en producción.
    - La deuda runtime .NET de NAV2, NAV3.1 y NAV3.2A permanece pendiente; NAV2 no se marca CLOSED.
  risks_or_blockers:
    - TestTools.Access ya llega a Admin por RolePermissions, pero aún no protege las rutas; no debe asumirse que NAV3.2A cambia acceso efectivo de las herramientas.
    - dotnet no está disponible en el host, por lo que la idempotencia/runtime del nuevo grant seeder no pudo ejecutarse.
  next_increment_recommendation: No ejecutar NAV3.2B automáticamente. Con autorización explícita, NAV3.2B debe sustituir de forma coherente la autorización de menú/SPA/API de ambas herramientas por TestTools.Access, mantener ACH.Operator sin acceso salvo decisión expresa y activar Pruebas de servicios sólo después de cerrar esa protección; el retiro de gating por ambiente puede quedar para un incremento separado explícitamente autorizado.

nav3_2b_execution:
  phase_executed: NAV3.2B
  phase_status: IMPLEMENTED_UNVERIFIED_ENVIRONMENT
  files_modified:
    - MEMORY.md
    - docs/ai/navigation-menu/MEMORY_STATE.md
    - docs/security/NAVIGATION_MENU_SECURITY_MODEL.md
    - src/Cfa.ACHInterbank.Persistence/Security/Seeders/TestToolsMenuAuthorizationSeeder.cs
    - src/Cfa.ACHInterbank.Api/Controllers/NachaInboundSimulatorController.cs
    - src/Cfa.ACHInterbank.Api/Controllers/NachaSoapUatConsoleController.cs
    - web/ach-interbank-ui/src/app/app-routing.module.ts
    - web/ach-interbank-ui/src/app/features/uat/uat-routing.module.ts
    - web/ach-interbank-ui/src/app/features/nacha-operational/nacha-operational-routing.module.ts
    - web/ach-interbank-ui/src/app/app-routing-access.spec.ts
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav29TestToolsInformationArchitectureTests.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav31TestToolsPermissionFoundationTests.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav32ATestToolsAdminRoleGrantTests.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav32BTestToolsPermissionEnforcementTests.cs
  tests_executed:
    - command: python scripts/validate-solution-projects.py
      result: PASS; grafo de solución consistente (7 proyectos).
    - command: NAV3.2B static focal assertions sobre menú, SPA, API, grant Admin, exclusión de ACH.Operator, estado inactivo de Pruebas de servicios y gating existente
      result: PASS; 24 checks focales.
    - command: C# delimiter sanity sobre archivos C# modificados/agregados
      result: PASS; 7 archivos C#.
    - command: Verificación de archivos sensibles que deben permanecer intactos contra baseline NAV3.2A
      result: PASS; 11 archivos sin diferencias, incluyendo NavigationInformationArchitectureSeeder, GetMenuForCurrentUserHandler, grants/roles/policies P1 y gating backend no modificado.
    - command: Comparación SHA-256 por archivo entre ZIP FULL NAV3.2A y workspace NAV3.2B
      result: PASS; 2 archivos agregados, 12 modificados, 0 eliminados; sólo seguridad de Herramientas de prueba, documentación y pruebas focales/acumulativas.
    - command: Verificación ZIP FULL NAV3.2B contra workspace final
      result: PASS; 4203 archivos, sin faltantes, extras ni diferencias de contenido.
  tests_passed:
    - python scripts/validate-solution-projects.py
    - NAV3.2B static focal assertions
    - C# delimiter sanity
    - comparación de archivos sensibles inmutables contra NAV3.2A
    - comparación SHA-256 ZIP NAV3.2A/workspace NAV3.2B dentro del alcance declarado
    - integridad del ZIP FULL NAV3.2B contra el workspace final
  tests_failed: []
  tests_deferred:
    - command: dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj --filter FullyQualifiedName~NavigationMenuNav32BTestToolsPermissionEnforcementTests|FullyQualifiedName~NavigationMenuNav32ATestToolsAdminRoleGrantTests|FullyQualifiedName~NavigationMenuNav31TestToolsPermissionFoundationTests|FullyQualifiedName~NavigationMenuNav29TestToolsInformationArchitectureTests
      reason: NOT_RUN_ENVIRONMENT; dotnet no está instalado en el host. La deuda runtime heredada de NAV2/NAV3 permanece abierta y NAV2 conserva IMPLEMENTED_UNVERIFIED_ENVIRONMENT.
    - command: Angular focal tests for app-routing-access.spec.ts
      reason: NOT_RUN_ENVIRONMENT; node/npm están disponibles pero web/ach-interbank-ui/node_modules no existe en el ZIP/workspace, por lo que no se inventa ejecución de Karma/Angular.
  decisions_taken:
    - TestTools.Access es la única autorización de menú de Herramientas de prueba, Simulador de archivos recibidos y Pruebas de servicios; sus MenuItemRole directos se eliminan mediante reconciliación idempotente posterior a NAV2.
    - El padre SPA /uat y las dos rutas de herramienta declaran TestTools.Access sin fallbacks CanReadAch/CanManageAch propios; no se introducen checks por nombre de rol.
    - Los controllers propios de ambas herramientas exigen TestTools.Access a nivel de clase. Las policies NachaSimulator existentes permanecen como requisitos adicionales y no pueden evadir el permiso de dominio.
    - Pruebas de servicios permanece IsActive=false y el gating Production/UAT-like/feature-flag existente no se elimina ni se amplía.
    - Admin conserva el grant TestTools.Access de NAV3.2A; ACH.Operator permanece sin el permiso. No se crean roles ni grants directos a usuarios.
    - NAV2 permanece IMPLEMENTED_UNVERIFIED_ENVIRONMENT y su estructura no se modifica.
  relevant_findings:
    - El permissionGuard interpreta la lista de permissions como alternativas; por eso los fallbacks genéricos se eliminan de los bloques propios de las herramientas en lugar de combinarse con TestTools.Access.
    - El controller-level Authorize(TestTools.Access) se combina con las policies específicas de acción, logrando TestTools.Access AND policy específica cuando aplica aunque la policy específica mantenga compatibilidad histórica con permisos ACH genéricos.
    - /ach/nacha/soap-uat-console todavía hereda del padre compartido /ach guardas Admin/ACH.Operator + CanReadAch. Es una restricción adicional, no un bypass; debe desacoplarse antes de permitir un futuro rol técnico no administrativo y antes de activar la opción.
    - Los endpoints compartidos usados como datos de apoyo por el simulador no se convierten en APIs exclusivas de Herramientas de prueba; sólo los controllers propios reciben el gate de dominio.
  gaps_pending:
    - Retirar en un incremento separado el gating de ambiente de GetMenuForCurrentUserHandler y de los componentes backend específicos de las herramientas para permitir producción basada en autorización, sin retirar TestTools.Access.
    - Preservar las guardas funcionales del simulador que impiden autoimportación/transmisión externa al separar seguridad de ambiente de seguridad funcional.
    - Revisar explícitamente UatContrapartidasController y ACH_SOAP_LIVE_TESTS antes de habilitación en producción; no es una superficie SPA publicada pero conserva gating UAT técnico.
    - Desacoplar la consola SOAP de las guardas heredadas del padre /ach si un futuro rol Desarrollo/Soporte debe acceder sin pertenecer a Admin/ACH.Operator.
    - Activar Pruebas de servicios únicamente después de retirar el filtro de ambiente que puede ocultar /uat y dejar flotar un hijo /ach.
    - La deuda runtime .NET heredada de NAV2/NAV3 permanece; NAV2 no se marca CLOSED.
  risks_or_blockers:
    - El gating de ambiente sigue impidiendo uso en Production aun con TestTools.Access; es deliberado en NAV3.2B.
    - La consola SOAP requiere TestTools.Access pero además hereda el padre /ach; esto limitará un eventual rol técnico hasta resolver ese acoplamiento.
  next_increment_recommendation: No avanzar automáticamente. El siguiente incremento autorizado debe retirar de forma controlada el gating de ambiente manteniendo TestTools.Access, revisar el endpoint UatContrapartidas/feature flag, desacoplar la consola SOAP del padre /ach si corresponde y sólo entonces activar Pruebas de servicios.

nav3_3a_execution:
  phase_executed: NAV3.3A
  phase_status: IMPLEMENTED_UNVERIFIED_ENVIRONMENT
  source_package: ACHInterbank-R34-NAV-IA-NAV3.2B-FULL-PROJECT.zip
  files_modified:
    - MEMORY.md
    - docs/ai/navigation-menu/MEMORY_STATE.md
    - docs/security/NAVIGATION_MENU_SECURITY_MODEL.md
    - src/Cfa.ACHInterbank.Application/Navigation/Queries/GetMenuForCurrentUserHandler.cs
    - src/Cfa.ACHInterbank.Api/Controllers/NachaInboundSimulatorController.cs
    - src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaInboundSimulationService.cs
    - web/ach-interbank-ui/src/app/app-routing-access.spec.ts
    - tests/Cfa.ACHInterbank.Tests/NachaInboundSimulatorTests.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav29TestToolsInformationArchitectureTests.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav31TestToolsPermissionFoundationTests.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav32ATestToolsAdminRoleGrantTests.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav32BTestToolsPermissionEnforcementTests.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav33AInboundSimulatorProductionAuthorizationTests.cs
  menu_environment_gates_removed:
    - GetMenuForCurrentUserHandler deja de inyectar IHostEnvironment/NachaInboundSimulatorOptions y deja de filtrar /uat y /uat/** por Production/IsUatLike.
  spa_changes:
    - No hubo cambio runtime de rutas/guards SPA porque NAV3.2B ya dejó /uat y /uat/nacha-inbound-simulator protegidos sólo por permissionGuard + TestTools.Access; se reforzó la prueba focal para documentar ausencia de fallback/gate adicional.
  api_environment_gates_removed:
    - NachaInboundSimulatorController deja de inyectar IHostEnvironment/NachaInboundSimulatorOptions y elimina UnavailableOutsideAuthorizedEnvironment/UAT_FEATURE_UNAVAILABLE.
  service_environment_gates_removed:
    - NachaInboundSimulationService.PreviewAsync deja de exigir IsUatLike/Mode UAT-Local; Enabled permanece como switch funcional.
  functional_gates_preserved:
    - TestTools.Access obligatorio a nivel de controller.
    - Policies específicas NachaSimulator.Read/Download/Generate* permanecen adicionales cuando corresponden.
    - AllowAutoImport=true o AllowExternalTransmission=true continúa bloqueando con SIMULATOR_GUARDRAIL_FAILED.
    - Límites, cámaras permitidas, ciclos, causales, referencias, perfiles y validaciones del simulador permanecen.
    - La salida sigue GeneratedOnly=true, AutoImported=false, UploadRequired=true, ExternalTransmission=false y requiere carga manual por NachaUpload.
  regression_state:
    - Pruebas de servicios permanece IsActive=false.
    - NachaSoapUatConsoleController mantiene !Production && IsUatLike y no fue modificado.
    - UatContrapartidasController no es dependencia del simulador y permanece intacto; su revisión corresponde a NAV3.3B.
    - Admin conserva TestTools.Access; ACH.Operator continúa sin el permiso; no se crean roles/grants/permiso nuevos.
    - NAV2 permanece IMPLEMENTED_UNVERIFIED_ENVIRONMENT y su arquitectura no se modifica.
  tests_executed:
    - command: python scripts/validate-solution-projects.py
      result: PASS; grafo de solución consistente (7 proyectos).
    - command: NAV3.3A static focal assertions sobre menú, SPA, API, servicio, grants, Pruebas de servicios y UatContrapartidas
      result: PASS; 33 checks focales.
    - command: C# delimiter sanity sobre archivos C# modificados/agregados
      result: PASS; 9 archivos C#.
    - command: comparación SHA-256 de archivos sensibles que NAV3.3A debía dejar intactos contra el ZIP NAV3.2B
      result: PASS; 11 archivos sin cambios, incluyendo NachaSoapUatConsoleController, UatContrapartidasController, seeders de permisos/grants/menú, RoleConfiguration y rutas SPA runtime.
    - command: comparación SHA-256 por archivo entre ZIP FULL NAV3.2B y workspace NAV3.3A
      result: PASS; 1 archivo agregado, 12 modificados, 0 eliminados antes del empaquetado final.
    - command: verificación ZIP FULL NAV3.3A contra workspace de handoff
      result: PASS; 4204 archivos, 0 faltantes, 0 extras y 0 diferencias de contenido.
  tests_passed:
    - python scripts/validate-solution-projects.py
    - NAV3.3A static focal assertions
    - C# delimiter sanity
    - verificación de archivos sensibles inmutables contra NAV3.2B
    - comparación SHA-256 de alcance ZIP NAV3.2B/workspace NAV3.3A
    - verificación de integridad ZIP FULL NAV3.3A contra workspace de handoff
  tests_failed: []
  tests_deferred:
    - command: dotnet focal/build tests NAV3.3A
      reason: NOT_RUN_ENVIRONMENT; dotnet no está instalado en el host.
    - command: Angular focal test app-routing-access.spec.ts
      reason: NOT_RUN_ENVIRONMENT; node/npm están disponibles pero web/ach-interbank-ui/node_modules no existe en el ZIP/workspace.
  next_increment_recommendation: No ejecutar NAV3.3B automáticamente. Revisar sólo Pruebas de servicios, su herencia SPA /ach, NachaSoapUatConsoleController y el endpoint ejecutor UatContrapartidasController antes de cualquier activación en Production.

nav3_3b_execution:
  phase_executed: NAV3.3B
  phase_status: IMPLEMENTED_UNVERIFIED_ENVIRONMENT
  source_package: ACHInterbank-R34-NAV-IA-NAV3.3A-FULL-PROJECT.zip
  source_sha256: 5c96c5d90370bd4776ef1a7feef1dd3d869088dcf66e3e10182db9d979ab11bf
  files_modified:
    - MEMORY.md
    - docs/ai/navigation-menu/MEMORY_STATE.md
    - docs/security/NAVIGATION_MENU_SECURITY_MODEL.md
    - src/Cfa.ACHInterbank.Api/Controllers/NachaSoapUatConsoleController.cs
    - src/Cfa.ACHInterbank.Persistence/Navigation/Seeders/NavigationInformationArchitectureSeeder.cs
    - src/Cfa.ACHInterbank.Persistence/Security/Seeders/TestToolsMenuAuthorizationSeeder.cs
    - web/ach-interbank-ui/src/app/app-routing.module.ts
    - web/ach-interbank-ui/src/app/features/nacha-operational/nacha-operational-routing.module.ts
    - web/ach-interbank-ui/src/app/app-routing-access.spec.ts
    - tests/Cfa.ACHInterbank.Tests/NachaSoapUatConsoleReadModelTests.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav29TestToolsInformationArchitectureTests.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav31TestToolsPermissionFoundationTests.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav32ATestToolsAdminRoleGrantTests.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav32BTestToolsPermissionEnforcementTests.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav33AInboundSimulatorProductionAuthorizationTests.cs
  files_added:
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav33BServiceTestsProductionAuthorizationTests.cs
    - scripts/validate-nav3-3b-service-tests.py
  dependency_inspection:
    - La SPA usa su propio NachaSoapUatConsoleController mediante GET dashboard, candidates y audit; candidate detail también es GET/read-only pero el componente actual no lo invoca.
    - NachaSoapUatConsoleReadModelService depende únicamente de INachaOperationalReadStore y proyecta lecturas/diagnóstico; no despacha jobs, no guarda cambios y no invoca clientes SOAP.
    - UatContrapartidasController no es dependencia de la consola; es una superficie potencialmente live y permanece intacta.
  spa_decoupling:
    - /ach/nacha/soap-uat-console se declara como ruta exacta independiente antes de /ach con permissionGuard + TestTools.Access y loadComponent standalone.
    - Se elimina la ruta hija correspondiente de NACHA_OPERATIONAL_ROUTES; el padre /ach conserva Admin/ACH.Operator + CanReadAch para todas las demás rutas.
  api_environment_gates_removed:
    - NachaSoapUatConsoleController deja de depender de IHostEnvironment/NachaInboundSimulatorOptions y elimina !Production, IsUatLike y UAT_FEATURE_UNAVAILABLE.
    - Se elimina P1Policies.NachaSimulatorRead de sus acciones GET porque no representa una capacidad diferenciada de esta consola read-only y agregaba un requisito ACH genérico ajeno al dominio.
  permissions_and_guards_preserved:
    - TestTools.Access permanece obligatorio en menú, SPA y controller API.
    - Admin conserva TestTools.Access; ACH.Operator continúa sin el permiso; no se crean roles, permisos o grants nuevos ni permisos directos a usuarios.
    - Los permisos específicos del Simulador de archivos recibidos permanecen intactos.
  live_controls_preserved:
    - UatContrapartidasController permanece byte a byte intacto con CanManageAch, bloqueo Production, allowlist Development/Testing/UAT y ACH_SOAP_LIVE_TESTS=true.
    - No se habilita envío SOAP, transmisión externa ni mutación operativa desde Pruebas de servicios; ProductiveExecution=false y WouldInvokeRealSoap=false permanecen en el read model.
  final_menu_state:
    - TestToolsMenuAuthorizationSeeder activa Pruebas de servicios IsActive=true sólo después de aplicar TestTools.Access como única autorización de menú.
    - NavigationInformationArchitectureSeeder mantiene IsActive=false como estado canónico inicial previo al reconciliador de seguridad.
  tests_executed:
    - command: python scripts/validate-solution-projects.py
      result: PASS; grafo de solución consistente (7 proyectos).
    - command: python scripts/validate-nav3-3b-service-tests.py
      result: PASS; 39 aserciones estáticas focales.
    - command: C# delimiter sanity sobre archivos C# modificados/agregados
      result: PASS; 10 archivos C#.
    - command: TypeScript delimiter sanity sobre rutas/prueba SPA modificadas
      result: PASS; 3 archivos TypeScript.
    - command: comparación SHA-256 de archivos sensibles que NAV3.3B debía dejar intactos contra NAV3.3A
      result: PASS; UatContrapartidasController, NachaInboundSimulatorController, NachaInboundSimulationService, TestToolsAdminRolePermissionSeeder y TestToolsPermissionSeeder permanecen byte a byte intactos.
    - command: comparación SHA-256 por archivo entre ZIP FULL NAV3.3A y workspace NAV3.3B
      result: PASS; 2 archivos agregados, 15 modificados, 0 eliminados antes del empaquetado final.
  tests_deferred:
    - command: dotnet focal/build tests NAV3.3B
      reason: NOT_RUN_ENVIRONMENT; dotnet no está instalado en el host.
    - command: Angular focal tests
      reason: NOT_RUN_ENVIRONMENT; node/npm están disponibles pero web/ach-interbank-ui/node_modules no está instalado en el ZIP/workspace.
  next_increment_recommendation: No avanzar automáticamente. Si el usuario acepta NAV3.3B, el subdominio Herramientas de prueba puede considerarse cerrado a nivel de diseño/seguridad de código; el siguiente incremento debe revisar un gap de autorización del resto del menú en alcance pequeño, sin volver a ampliar estas herramientas. NAV2 permanece IMPLEMENTED_UNVERIFIED_ENVIRONMENT.

nav3_4a_execution:
  phase_executed: NAV3.4A
  phase_status: IMPLEMENTED_UNVERIFIED_ENVIRONMENT
  source_package: ACHInterbank-R34-NAV-IA-NAV3.3B-FULL-PROJECT.zip
  source_sha256: e30ee2d88385ca7ad09a7e9f2f72bb12c7be478f6d1cf3f6ddabfe82587ab460
  files_modified:
    - MEMORY.md
    - docs/ai/navigation-menu/MEMORY_STATE.md
    - docs/security/NAVIGATION_MENU_SECURITY_MODEL.md
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav25BResponsesReconciliationInformationArchitectureTests.cs
    - web/ach-interbank-ui/src/app/features/ach-responses/ach-responses-routing.module.spec.ts
    - web/ach-interbank-ui/src/app/features/ach-responses/pages/ach-response-manual-review-page.component.html
    - web/ach-interbank-ui/src/app/features/ach-responses/pages/ach-response-manual-review-page.component.spec.ts
    - web/ach-interbank-ui/src/app/features/ach-responses/pages/ach-response-manual-review-page.component.ts
  files_added:
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav34AManualReviewAuthorizationTests.cs
  operations_found:
    - GET api/ach/responses: búsqueda/consulta de casos; P1.NachaRead.
    - GET api/ach/responses/orphans: consulta de huérfanas; P1.NachaRead.
    - POST api/ach/responses/orphans/{id}/review/start: decisión mutable Pending -> InReview / EnRevision con auditoría; P1.NachaGenerate.
    - POST api/ach/responses/orphans/{id}/resolve: decisión mutable Resolve/Reject y Resuelta/Rechazada con validación, auditoría y persistencia; P1.NachaGenerate.
    - La SPA no usa requestReprocess, process ni sendNotification desde Revisión manual.
  authorization_before:
    - MenuItem: Admin OR ACH.Operator + CanReadAch.
    - SPA: padre /ach-responses Admin OR ACH.Operator + CanReadAch; hijo manual-review CanReadAch.
    - API reads: P1.NachaRead = Nacha.Read OR CanReadAch.
    - API mutations: P1.NachaGenerate = Nacha.Generate OR CanManageAch; CanReadAch ya era rechazado para escribir.
  authorization_final:
    - MenuItem y ruta de entrada conservan CanReadAch y sus roles existentes para permitir consulta sin endurecimiento innecesario.
    - Los controles y métodos SPA begin/resolve requieren Nacha.Generate OR CanManageAch.
    - Controllers/policies API permanecen sin cambio porque ya distinguían lectura de mutación correctamente.
    - No se agregan checks por rol, permisos, roles, RolePermissions o grants.
  functional_effects:
    - BeginReview/ResolveOrphan realizan sólo mutaciones persistentes internas en AchDbContext y auditoría.
    - No se agrega envío SOAP, transmisión externa, dispatch, reproceso ni notificación desde esta pantalla.
  regression_state:
    - Otras superficies productivas ach-responses permanecen intactas.
    - Homologación de estados permanece bajo Administración de parámetros.
    - Herramientas de prueba/TestTools.Access permanece intacta.
    - NAV2 permanece IMPLEMENTED_UNVERIFIED_ENVIRONMENT.
  tests_executed:
    - command: python scripts/validate-solution-projects.py
      result: PASS; grafo de solución consistente (7 proyectos).
    - command: NAV3.4A focal static assertions sobre operaciones SPA, rutas, endpoints, policies y ausencia de efectos externos
      result: PASS; 38 checks focales.
    - command: delimiter sanity C#/TypeScript sobre archivos de código/prueba modificados
      result: PASS; 5 archivos.
    - command: comparación SHA-256 de alcance y archivos sensibles contra ZIP FULL NAV3.3B
      result: PASS; 1 archivo agregado, 8 modificados, 0 eliminados; sólo el componente/template de Revisión manual cambian en producción, 18 archivos sensibles permanecen byte a byte intactos y las demás superficies productivas ach-responses permanecen intactas.
    - command: verificación de integridad ZIP FULL NAV3.4A contra workspace
      result: PASS; 4207 archivos de proyecto, 0 faltantes, 0 extras y 0 diferencias de contenido en el handoff final.
  tests_deferred:
    - command: dotnet focal/build tests NAV3.4A
      reason: NOT_RUN_ENVIRONMENT; dotnet no está instalado en el host.
    - command: Angular focal tests
      reason: NOT_RUN_ENVIRONMENT; Node/npm están disponibles pero web/ach-interbank-ui/node_modules no está instalado en el ZIP/workspace.
  residual_gap: Sólo queda verificación runtime de los tests focales cuando existan SDK .NET y dependencias Angular; no se identifica bypass de CanReadAch hacia las mutaciones en el diseño/código final.
  next_increment_recommendation: No avanzar automáticamente. Si el usuario acepta NAV3.4A, revisar Crear transacción como siguiente gap aislado porque NAV1 ya caracterizó visibilidad de menú CanReadAch frente a ruta mutable CanManageAch.

nav3_4b_execution:
  phase_executed: NAV3.4B
  phase_status: IMPLEMENTED_UNVERIFIED_ENVIRONMENT
  source_package: ACHInterbank-R34-NAV-IA-NAV3.4A-FULL-PROJECT.zip
  source_sha256: f773ea7d06a697c9d55b2277394d4721d617c143c8200efbceb8455a45f81f53
  files_modified:
    - MEMORY.md
    - docs/ai/navigation-menu/MEMORY_STATE.md
    - docs/security/NAVIGATION_MENU_SECURITY_MODEL.md
    - web/ach-interbank-ui/src/app/features/transactions/transactions-routing.module.ts
  files_added:
    - src/Cfa.ACHInterbank.Persistence/Security/Seeders/TransactionsCreatePermissionSeeder.cs
    - src/Cfa.ACHInterbank.Persistence/Security/Seeders/TransactionsCreateMenuAuthorizationSeeder.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav34BTransactionCreateAuthorizationTests.cs
    - scripts/validate-nav3-4b-transaction-create.py
  behavior_found:
    - La pantalla consulta clientes, entidades financieras, conceptos de lote, cuentas contextuales y preview de política; el submit invoca POST api/transactions.
    - RegisterTransactionAsync valida y persiste la transacción/estado interno, puede encolar ciclo y dejar dispatch de contrapartida en PendingContrapartidaReport; no invoca directamente el job live, SOAP ni generación de archivo.
  authorization_before:
    - MenuItem final efectivo heredado: Admin OR ACH.Operator AND (CanManageAch OR CanReadAch), por semántica OR de MenuItemPermission.
    - SPA: padre /transactions Admin OR ACH.Operator AND (CanManageAch OR CanReadAch); hijo /transactions/create requería CanManageAch.
    - API POST api/transactions: P0.TransactionsCreate = Transactions.Create OR CanManageAch; CanReadAch ya era rechazado.
  authorization_final:
    - MenuItem: roles existentes sin cambio AND (Transactions.Create OR CanManageAch); CanReadAch se elimina como alternativa de creación.
    - SPA hijo /transactions/create: Transactions.Create OR CanManageAch; no contiene CanReadAch ni checks nuevos por rol. El padre /transactions permanece intacto y las lecturas auxiliares siguen requiriendo sus permisos de lectura existentes.
    - API: sin cambio; POST api/transactions conserva P0.TransactionsCreate. Los GET auxiliares conservan P0.TransactionsRead, P0.TransactionsPolicyPreview o CanReadAch según el controller.
  permission_catalog:
    - Transactions.Create ya era una capacidad existente en FineGrainedPermissions/AllPermissions/P0.TransactionsCreate; NAV3.4B sólo la persiste idempotentemente para MenuItemPermission.
    - No se crean RolePermissions ni grants directos; Admin/ACH.Operator no reciben permisos nuevos.
  regression_state:
    - Consultar transacciones conserva CanReadAch.
    - Otras opciones de Operación permanecen intactas.
    - Revisión manual conserva el modelo NAV3.4A.
    - Herramientas de prueba/TestTools.Access permanece intacta.
    - NAV2 permanece IMPLEMENTED_UNVERIFIED_ENVIRONMENT.
  tests_executed:
    - command: python scripts/validate-solution-projects.py
      result: PASS; grafo de solución consistente (7 proyectos).
    - command: python scripts/validate-nav3-4b-transaction-create.py
      result: PASS; validación estática focal de menú, SPA, API, efectos funcionales y regresiones.
    - command: delimiter sanity C#/TypeScript/Python sobre archivos agregados/modificados de NAV3.4B
      result: PASS; 4 archivos C#/TypeScript balanceados y el validador Python compila con ast.parse.
    - command: comparación SHA-256 de alcance y archivos sensibles contra ZIP FULL NAV3.4A
      result: PASS; 4 archivos agregados, 4 modificados y 0 eliminados; 22 archivos sensibles y los otros 69 archivos del feature transactions permanecen byte a byte intactos.
    - command: verificación de integridad ZIP FULL NAV3.4B contra workspace
      result: PASS; 4211 archivos de proyecto, 0 faltantes, 0 extras y 0 diferencias de contenido.
  tests_deferred:
    - command: dotnet focal/build tests NAV3.4B
      reason: NOT_RUN_ENVIRONMENT; dotnet no está instalado en el host.
    - command: Angular focal tests
      reason: NOT_RUN_ENVIRONMENT; Node/npm están disponibles pero web/ach-interbank-ui/node_modules no está instalado en el ZIP/workspace.
  residual_gap: Sólo queda verificación runtime de los tests focales. No se identifica bypass de CanReadAch hacia la creación; una claim de rol por sí sola tampoco sustituye la capacidad de creación.
  next_increment_recommendation: No avanzar automáticamente. Si el usuario acepta NAV3.4B, revisar Cargar archivo NACHA-M como siguiente gap aislado, inspeccionando primero su operación real y policy backend antes de tocar menú/SPA.

nav3_4c_execution:
  phase_executed: NAV3.4C
  phase_status: IMPLEMENTED_UNVERIFIED_ENVIRONMENT
  source_package: ACHInterbank-R34-NAV-IA-NAV3.4B-FULL-PROJECT.zip
  source_sha256: bcd4fd3962703a85508d33b5cf58554cfdd8e2f374c481facaafd40b28128723
  files_modified:
    - MEMORY.md
    - docs/ai/navigation-menu/MEMORY_STATE.md
    - docs/security/NAVIGATION_MENU_SECURITY_MODEL.md
    - web/ach-interbank-ui/src/app/features/transactions/transactions-routing.module.ts
    - web/ach-interbank-ui/src/app/features/transactions/transactions-routing.module.spec.ts
  files_added:
    - src/Cfa.ACHInterbank.Persistence/Security/Seeders/NachaUploadPermissionSeeder.cs
    - src/Cfa.ACHInterbank.Persistence/Security/Seeders/NachaUploadMenuAuthorizationSeeder.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav34CNachaUploadAuthorizationTests.cs
    - scripts/validate-nav3-4c-nacha-upload.py
  behavior_found:
    - La SPA clasifica/valida el archivo, ejecuta POST NachaUpload/upload, permite reproceso explícito y consulta GET NachaUpload/records.
    - La ingesta persiste IncomingNachaFileIngestion, resultados/eventos de procesamiento y el grafo NACHA-M parseado; luego ejecuta post-procesamiento y planifica IncomingNachaDispatchQueue para procesamiento asíncrono posterior.
    - El upload HTTP no llama directamente clientes SOAP ni IContrapartidaDispatchJobService. Un pipeline asíncrono separado sí conserva integración externa bajo sus controles existentes.
    - IncomingNachaLocalLivePreparationService sólo puede crear una AchTransaction de preparación local cuando IsDevelopment y los tres controles live explícitos están habilitados; NAV3.4C no cambia esa protección.
  endpoints:
    - POST NachaUpload/upload: P1.NachaUpload = Nacha.Upload OR CanManageAch; mutación/ingesta.
    - GET NachaUpload/records: P1.NachaRead = Nacha.Read OR CanReadAch; consulta read-only.
  authorization_before:
    - MenuItem Cargar archivo NACHA-M: sin autorización directa propia; bajo el árbol de Operación podía resultar visible a un usuario que superara el acceso del padre con CanReadAch.
    - SPA padre /transactions: Admin OR ACH.Operator AND (CanManageAch OR CanReadAch); hijo /transactions/nacha-upload requería CanManageAch.
    - API POST NachaUpload/upload: P1.NachaUpload = Nacha.Upload OR CanManageAch; CanReadAch ya era rechazado. GET records usaba P1.NachaRead.
  authorization_final:
    - MenuItem: Nacha.Upload OR CanManageAch como permisos directos; no CanReadAch, sin cambios de label, route, ParentId, Order, IsActive o roles.
    - SPA hijo /transactions/nacha-upload: Nacha.Upload OR CanManageAch; sin CanReadAch ni checks por nombre de rol. El padre /transactions permanece intacto como prerequisito legacy adicional.
    - API: sin cambios; POST conserva P1.NachaUpload y GET records conserva P1.NachaRead.
  permission_catalog:
    - Nacha.Upload ya existía en FineGrainedPermissions/AllPermissions/P1.NachaUpload; NAV3.4C sólo lo persiste idempotentemente para MenuItemPermission.
    - No se crean RolePermissions ni grants directos; Admin/ACH.Operator no reciben permisos nuevos.
  regression_state:
    - Crear transacción conserva el modelo NAV3.4B.
    - Revisión manual conserva el modelo NAV3.4A.
    - Herramientas de prueba/TestTools.Access permanece intacta.
    - Las demás opciones de Operación permanecen intactas.
    - NAV2 permanece IMPLEMENTED_UNVERIFIED_ENVIRONMENT.
  tests_executed:
    - command: python scripts/validate-solution-projects.py
      result: PASS; grafo de solución consistente (7 proyectos).
    - command: python scripts/validate-nav3-4c-nacha-upload.py
      result: PASS; 87 aserciones estáticas focales sobre menú, SPA, API, efectos funcionales y regresión.
    - command: delimiter/syntax sanity C#/TypeScript/Python sobre archivos agregados/modificados de NAV3.4C
      result: PASS; validador Python compila con ast.parse y 5 archivos C#/TypeScript focales presentan delimitadores balanceados tras excluir literales/comentarios.
    - command: comparación SHA-256 de alcance y archivos sensibles contra ZIP FULL NAV3.4B
      result: PASS; 4 archivos agregados, 5 modificados y 0 eliminados; 68 archivos restantes del feature transactions y 18 archivos sensibles backend/seguridad permanecen byte a byte intactos.
    - command: verificación de integridad ZIP FULL NAV3.4C contra workspace
      result: PASS; 4215 archivos de proyecto, 0 faltantes, 0 extras y 0 diferencias de contenido.
  tests_deferred:
    - command: dotnet focal/build tests NAV3.4C
      reason: NOT_RUN_ENVIRONMENT; dotnet no está instalado en el host.
    - command: Angular focal tests
      reason: NOT_RUN_ENVIRONMENT; Node v22.16.0 y npm 10.9.2 están disponibles pero web/ach-interbank-ui/node_modules no está instalado en el ZIP/workspace.
  residual_gap: Sólo queda verificación runtime de tests focales. El padre /transactions conserva requisitos legacy adicionales; no permiten que CanReadAch sustituya Nacha.Upload/CanManageAch en el hijo y por tanto no constituyen bypass de carga.
  next_increment_recommendation: No avanzar automáticamente. Si el usuario acepta NAV3.4C, revisar Devoluciones como siguiente gap NAV3 aislado, caracterizando primero operaciones y policy backend.

nav5_execution:
  phase_executed: NAV5
  phase_status: IMPLEMENTED_UNVERIFIED_ENVIRONMENT
  source_package: ACHInterbank-R34-NAV-IA-NAV4.2J-FULL-PROJECT.zip
  source_sha256: 3f3c297f2df4ee11e085ca32172a3f7b6667b8fc00ebf7a0b0dbfae5a1e61515
  route_universe: 142
  classification:
    MENU: 70
    CONTEXTUAL: 36
    TECHNICAL: 12
    REDIRECT: 24
    UNREACHABLE_OR_DEAD: 0
    UNKNOWN: 0
  menu_coverage:
    navigable_menu_items: 70
    menu_covered: 70
    menu_missing: 0
    invalid_menu_routes: 0
    contextual_or_technical_exposed: 0
    duplicate_destinations: 0
  gaps:
    found: 4
    corrected: 3
    blocked_security: 0
    residual: 1
    residual_detail: /nacha-security/certificates/:id/versions permanece ORPHAN_CONTEXTUAL_ROUTE; no se agrega acción UI ni entrada de menú en NAV5.
  productive_delta:
    - /nacha-config-admin/perfiles Order 1 -> 10 en el estado final del reconciliador.
    - /nacha-config-admin/records IsActive true -> false en el estado final; route y autorización histórica intactas.
    - /nacha-config-admin/variants-fields IsActive true -> false en el estado final; route y autorización histórica intactas.
  invariants:
    - Árbol web/ach-interbank-ui/src/app byte-equivalente a NAV4.2J; NAV4 copy intacto.
    - Security seeders/model, GetMenuForCurrentUserHandler y NavigationService/MenuRouteSurfaceService/NavigationNamingService byte-equivalentes al baseline.
    - NAV3.5C permanece BLOCKED / NO EJECUTAR.
    - Sin cambios de lógica de negocio, APIs, DTOs, forms, validators, payloads o integración.
  next_increment_recommendation: NAV6 sólo por decisión humana; NAV6 NO fue ejecutado.

open_questions:
  - Decidir en una fase posterior si se necesita un rol dedicado Desarrollo/Soporte para acceso técnico no administrativo, definiendo propósito, población, permisos y elegibilidad en producción antes de crearlo.
  - Confirmar en una fase de seguridad posterior si CanViewPaymentRailCapabilityRegistry debe persistirse como permiso dedicado y endurecerse, o si se formaliza el fallback actual CanManageAch/CanReadAch.
```

## Regla de actualización

Al cerrar una fase: cambiar `active_phase_status`, registrar `tests_executed`, `tests_deferred`, `files_touched`, `last_output_package`, fijar `next_allowed_phase` y agregar sólo decisiones/gaps durables. No pegar logs extensos.

El SHA-256 del ZIP de salida se entrega externamente en el handoff porque incrustar el hash del propio ZIP dentro de un archivo contenido en ese mismo ZIP produciría una referencia circular y alteraría el hash.

nav3_4d_execution:
  phase_executed: NAV3.4D
  phase_status: IMPLEMENTED_UNVERIFIED_ENVIRONMENT
  source_package: ACHInterbank-R34-NAV-IA-NAV3.4C-FULL-PROJECT.zip
  source_sha256: 457f5e13cd747202e8358a7a8c447c6c3c1a27687843b447dec82cc0817d5333
  files_modified:
    - MEMORY.md
    - docs/ai/navigation-menu/MEMORY_STATE.md
    - docs/security/NAVIGATION_MENU_SECURITY_MODEL.md
    - web/ach-interbank-ui/src/app/features/transactions/transactions-routing.module.ts
    - web/ach-interbank-ui/src/app/features/transactions/transactions-routing.module.spec.ts
    - web/ach-interbank-ui/src/app/features/transactions/components/ach-returns-management/ach-returns-management.component.ts
    - web/ach-interbank-ui/src/app/features/transactions/components/ach-returns-management/ach-returns-management.component.html
    - web/ach-interbank-ui/src/app/features/transactions/components/ach-returns-management/ach-returns-management.component.spec.ts
  files_added:
    - src/Cfa.ACHInterbank.Persistence/Security/Seeders/ReturnsPermissionSeeder.cs
    - src/Cfa.ACHInterbank.Persistence/Security/Seeders/ReturnsMenuAuthorizationSeeder.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav34DReturnsAuthorizationTests.cs
    - scripts/validate-nav3-4d-returns.py
  behavior_found:
    - La pantalla es mixta: consulta ciclos, causales y transacciones elegibles; filtra/selecciona y muestra detalle; la única mutación expuesta es asignar causal y generar el archivo .RET.
    - POST ach-returns/generate-file persiste AchReturnGenerated, genera ReturnOut NACHA-M, persiste auditoría/nombre y mueve las transacciones originales a ReturnedByEpr.
    - El request de generación no invoca dispatch externo, job de contrapartida, SOAP ni una cola automática de transmisión; existe una superficie de transporte separada que la pantalla no consume.
  endpoints_used:
    - GET api/ach-cycles: lectura auxiliar; CanReadAch.
    - GET return-reasons?onlyForReturn=true: lectura auxiliar; CanReadAch.
    - GET ach-returns/cycles/{cycleId}/transactions: lectura de elegibilidad; P0.ReturnsRead = Returns.Read OR CanReadAch.
    - POST ach-returns/generate-file: mutación/generación; P0.ReturnsGenerateFile = Returns.GenerateFile OR CanManageAch.
  authorization_before:
    - MenuItem: sin autorización directa específica reconciliada para Devoluciones.
    - SPA padre /transactions: Admin OR ACH.Operator AND (CanManageAch OR CanReadAch); hijo /transactions/returns requería CanManageAch.
    - API: lecturas y generación ya estaban separadas por P0.ReturnsRead y P0.ReturnsGenerateFile.
  authorization_final:
    - MenuItem Devoluciones: CanReadAch como capacidad directa de entrada de la pantalla completa.
    - SPA /transactions/returns: CanReadAch; no se exige gestión para consultar.
    - Acción mutable SPA: Returns.GenerateFile OR CanManageAch, con gating visual y validación fail-closed en los métodos antes de invocar el servicio.
    - API: sin cambios; GET conserva lectura y POST conserva P0.ReturnsGenerateFile. CanReadAch no satisface la mutación.
  permission_catalog:
    - Returns.GenerateFile ya existía en FineGrainedPermissions/AllPermissions/P0.ReturnsGenerateFile y se materializa idempotentemente en Permission.
    - No se crean roles, RolePermissions, grants a Admin/ACH.Operator ni permisos directos a usuarios.
    - Returns.Read no se materializa en este incremento porque la pantalla completa aún consume GET auxiliares que requieren CanReadAch; ampliar esos controllers compartidos queda fuera del alcance.
  regression_state:
    - /transactions/returns-ror permanece fuera del alcance y byte a byte intacto.
    - Crear transacción conserva NAV3.4B.
    - Cargar archivo NACHA-M conserva NAV3.4C.
    - Revisión manual conserva NAV3.4A.
    - Herramientas de prueba/TestTools.Access permanece intacta.
    - NAV2 permanece IMPLEMENTED_UNVERIFIED_ENVIRONMENT.
  tests_executed:
    - command: python scripts/validate-solution-projects.py
      result: PASS; grafo de solución consistente (7 proyectos).
    - command: python scripts/validate-nav3-4d-returns.py
      result: PASS; 100 aserciones estáticas focales sobre menú, SPA, API, efectos funcionales y regresiones.
    - command: TypeScript parser syntax + C# delimiter sanity + Python py_compile sobre archivos agregados/modificados focales
      result: PASS; 4 archivos TypeScript sin parse diagnostics, 3 archivos C# balanceados y validador Python válido.
    - command: comparación SHA-256 de archivos sensibles contra ZIP FULL NAV3.4C
      result: PASS; 29 archivos sensibles permanecen byte a byte intactos y 27/27 archivos del conjunto ReturnOfReturn inspeccionado permanecen byte a byte idénticos.
    - command: comparación SHA-256 de alcance contra ZIP FULL NAV3.4C
      result: PASS; 4 archivos agregados, 8 modificados y 0 eliminados; no hay cambios productivos fuera del MenuItem/SPA focal y los seeders de autorización/catálogo de Devoluciones.
    - command: verificación de integridad ZIP FULL NAV3.4D contra workspace
      result: PASS; 4219 archivos de proyecto, 0 faltantes, 0 extras y 0 diferencias de contenido en el handoff final.
  tests_deferred:
    - command: dotnet focal/build tests NAV3.4D
      reason: NOT_RUN_ENVIRONMENT; dotnet no está instalado en el host.
    - command: Angular focal tests
      reason: NOT_RUN_ENVIRONMENT; Node v22.16.0, npm 10.9.2 y tsc 5.8.3 están disponibles, pero web/ach-interbank-ui/node_modules no está instalado.
  residual_gap: Sólo queda verificación runtime de los tests focales. La claim fina Returns.Read no basta por sí sola para cargar la pantalla completa porque los catálogos auxiliares siguen exigiendo CanReadAch; esto restringe, no amplía, y no constituye bypass de la generación.
  next_increment_recommendation: No avanzar automáticamente. Seleccionar explícitamente el siguiente gap NAV3 en un incremento aislado; Devolución de una devolución permanece fuera de alcance hasta instrucción separada.

nav3_4e_execution:
  phase_executed: NAV3.4E
  status: IMPLEMENTED_UNVERIFIED_ENVIRONMENT
  source_package: ACHInterbank-R34-NAV-IA-NAV3.4D-FULL-PROJECT.zip
  source_sha256: 4cc986455d9fa78801c4ecee499a3e55c38bf4172940a18874610666424ea630
  scope: Sólo /transactions/bulk-ingestion/tracking, su detalle contextual /transactions/bulk-ingestion/:batchId y las acciones retry/cancel realmente consumidas; upload, bulk-create y otros gaps quedan fuera.
  functional_characterization:
    - Modelo B: la pantalla de Seguimiento de cargas es mixta. Buscar/agregar por batchId, refrescar estado, filtrar localmente y abrir detalle son lectura/seguimiento.
    - El detalle consulta estado/resumen, historial de intentos e ítems paginados/filtrados.
    - Retry es mutable: reinicia ítems elegibles, crea un intento, marca el lote Retrying y despacha procesamiento asíncrono; el procesamiento posterior puede crear transacciones y preparar colas/dispatch pendientes existentes.
    - Cancel es mutable: persiste el lote como Cancelled y fecha/mensaje final. El código actual no demuestra que cancele físicamente un job Quartz/backplane ya ejecutándose.
    - No se encontró descarga, corrección, eliminación ni inicio manual distinto de retry/cancel en la superficie inspeccionada.
    - Los requests retry/cancel no invocan directamente IWscfaachSoapClient ni IContrapartidaDispatchJobService; no hay transmisión externa directa desde esos requests.
  endpoints_consumed:
    - GET api/transactions/bulk-ingestion/{batchId} -> P1.BulkIngestionRead.
    - GET api/transactions/bulk-ingestion/{batchId}/items -> P1.BulkIngestionRead.
    - GET api/transactions/bulk-ingestion/{batchId}/summary -> P1.BulkIngestionRead.
    - POST api/transactions/bulk-ingestion/{batchId}/retry -> P1.BulkIngestionRetry.
    - POST api/transactions/bulk-ingestion/{batchId}/cancel -> P1.BulkIngestionCancel.
    - POST api/transactions/bulk-ingestion/upload comparte controller/servicio pero pertenece a Cargar transacciones desde archivo y permanece fuera de NAV3.4E.
  authorization_before:
    - MenuItem Seguimiento de cargas: creado históricamente con Admin/ACH.Operator + CanManageAch y sin capacidad fina de lectura persistida/reconciliada.
    - SPA /transactions/bulk-ingestion/tracking y detalle /transactions/bulk-ingestion/:batchId: CanManageAch.
    - SPA padre /transactions: Admin OR ACH.Operator AND (CanManageAch OR CanReadAch).
    - API: ya separaba read (BulkIngestion.Read OR CanReadAch), retry (BulkIngestion.Retry OR CanManageAch) y cancel (BulkIngestion.Cancel OR CanManageAch).
  backend_capabilities:
    read: P1.BulkIngestionRead = BulkIngestion.Read OR CanReadAch
    retry: P1.BulkIngestionRetry = BulkIngestion.Retry OR CanManageAch
    cancel: P1.BulkIngestionCancel = BulkIngestion.Cancel OR CanManageAch
  authorization_final:
    - MenuItem Seguimiento de cargas conserva sus roles existentes y sus alternativas directas de permiso quedan exactamente BulkIngestion.Read OR CanReadAch.
    - SPA tracking y detalle contextual exigen permissionGuard con BulkIngestion.Read OR CanReadAch.
    - Retry SPA exige BulkIngestion.Retry OR CanManageAch, con gating visual y comprobación fail-closed dentro del método antes de invocar el API.
    - Cancel SPA exige BulkIngestion.Cancel OR CanManageAch, con gating visual y comprobación fail-closed dentro del método antes de invocar el API.
    - API/controller/policies no cambian. CanReadAch/BulkIngestion.Read no autorizan retry/cancel.
  permission_catalog:
    - BulkIngestion.Read, BulkIngestion.Retry y BulkIngestion.Cancel ya existían en FineGrainedPermissions/AllPermissions/P1 policies y se materializan idempotentemente en Permission.
    - No se materializa BulkIngestion.Upload en NAV3.4E y no se crean roles, RolePermissions, grants ni permisos directos a usuarios.
  inherited_parent_effect:
    - /transactions conserva Admin/ACH.Operator + (CanManageAch OR CanReadAch) como prerequisito SPA legacy adicional.
    - Por ello BulkIngestion.Read solo satisface backend y guard hijo, pero no garantiza navegación SPA end-to-end sin los requisitos del padre. Es una restricción adicional, no un bypass; el padre no se refactoriza en NAV3.4E.
  files_added:
    - src/Cfa.ACHInterbank.Persistence/Security/Seeders/BulkIngestionTrackingPermissionSeeder.cs
    - src/Cfa.ACHInterbank.Persistence/Security/Seeders/BulkIngestionTrackingMenuAuthorizationSeeder.cs
    - tests/Cfa.ACHInterbank.Tests/NavigationMenuNav34EBulkIngestionTrackingAuthorizationTests.cs
    - scripts/validate-nav3-4e-bulk-ingestion-tracking.py
  files_modified_productive:
    - web/ach-interbank-ui/src/app/features/transactions/transactions-routing.module.ts
    - web/ach-interbank-ui/src/app/features/transactions/components/bulk-ingestion-tracking/bulk-ingestion-tracking.component.ts
    - web/ach-interbank-ui/src/app/features/transactions/components/bulk-ingestion-detail/bulk-ingestion-detail.component.ts
    - web/ach-interbank-ui/src/app/features/transactions/components/bulk-ingestion-detail/bulk-ingestion-detail.component.html
  files_modified_tests:
    - web/ach-interbank-ui/src/app/features/transactions/transactions-routing.module.spec.ts
    - web/ach-interbank-ui/src/app/features/transactions/components/bulk-ingestion-tracking/bulk-ingestion-tracking.component.spec.ts
    - web/ach-interbank-ui/src/app/features/transactions/components/bulk-ingestion-detail/bulk-ingestion-detail.component.spec.ts
  documentation_updated:
    - MEMORY.md
    - docs/ai/navigation-menu/MEMORY_STATE.md
    - docs/security/NAVIGATION_MENU_SECURITY_MODEL.md
  regression_state:
    - /transactions/bulk-ingestion/upload permanece intacto funcional/autorizativamente.
    - /transactions/bulk-create permanece fuera del menú y sin cambios.
    - NAV3.4A Revisión manual, NAV3.4B Crear transacción, NAV3.4C Cargar archivo NACHA-M, NAV3.4D Devoluciones y Herramientas de prueba permanecen intactos.
    - Backend BulkIngestionController, policies P1 y servicios funcionales de procesamiento permanecen sin cambios productivos.
    - NAV2 permanece IMPLEMENTED_UNVERIFIED_ENVIRONMENT.
  tests_executed:
    - command: python scripts/validate-solution-projects.py
      result: PASS; grafo de solución consistente (7 proyectos).
    - command: python scripts/validate-nav3-4e-bulk-ingestion-tracking.py
      result: PASS; 153 aserciones estáticas focales sobre caracterización, menú, SPA, API, efectos funcionales y regresiones.
    - command: TypeScript parser + C# delimiter sanity + Python py_compile sobre archivos focales
      result: PASS; 6 archivos TypeScript sin parse diagnostics, 3 archivos C# balanceados y validador Python válido.
    - command: comparación SHA-256 de archivos sensibles contra ZIP FULL NAV3.4D
      result: PASS; 43 archivos sensibles permanecen byte a byte intactos, incluyendo Cargar transacciones desde archivo, bulk-create, backend BulkIngestion, modelos NAV3.4A-D, TestTools y arquitectura NAV2.
    - command: comparación SHA-256 de alcance contra ZIP FULL NAV3.4D
      result: PASS; 4 archivos agregados, 10 modificados y 0 eliminados.
    - command: verificación de integridad ZIP FULL NAV3.4E contra workspace
      result: PASS; 4223 archivos de proyecto, 0 faltantes, 0 extras y 0 diferencias de contenido en el handoff final.
  tests_deferred:
    - command: dotnet focal/build tests NAV3.4E
      reason: NOT_RUN_ENVIRONMENT; dotnet no está instalado en el host.
    - command: Angular focal tests
      reason: NOT_RUN_ENVIRONMENT; Node v22.16.0, npm 10.9.2 y tsc 5.8.3 están disponibles, pero web/ach-interbank-ui/node_modules no está instalado.
  residual_gap: BulkIngestion.Read solo es suficiente para la policy backend y el guard hijo de lectura, pero el padre /transactions conserva roles y permiso legacy como prerequisito adicional. Esto restringe navegación, no amplía acceso ni permite mutaciones; corregir el padre globalmente queda fuera de NAV3.4E.
  next_increment_recommendation: No avanzar automáticamente. Si el usuario acepta NAV3.4E, seleccionar otro gap aislado; Seguridad de archivos es un candidato acotado antes de Organización del menú/Capacidades de la plataforma/MFT-Integraciones.


## NAV3.4F — Certificados digitales: lectura + administración protegida — 2026-10-14

### Estado

- Incremento ejecutado: **NAV3.4F solamente**.
- Fuente vigente: ZIP FULL de NAV3.4E.
- Git: **no utilizado**.
- NAV2: **IMPLEMENTED_UNVERIFIED_ENVIRONMENT**; no se marca CLOSED porque las pruebas runtime .NET continúan pendientes.
- Modelo final de `/nacha-security/certificates`: **B — superficie mixta de consulta + acciones administrativas**.

### Comportamiento real de la superficie

La pantalla `Certificados digitales` consulta el inventario de certificados y permite abrir detalle. La ruta contextual `/nacha-security/certificates/:id/versions` consulta versiones. La pantalla principal también expone el flujo guiado de carga/reemplazo y las acciones de revocación/eliminación.

Servicios/endpoints efectivamente utilizados por la UI vigente:

| Operación SPA | Endpoint | Policy backend | Clasificación |
|---|---|---|---|
| Listar certificados | `GET api/nacha-security/certificates/management` | `P1.CertificatesRead` | lectura |
| Consultar versiones | `GET api/nacha-security/certificates/management/{id}/versions` | `P1.CertificatesRead` | lectura contextual |
| Contexto de cámaras | `GET api/clearing-houses/operational` | `CanReadAch` | lectura auxiliar |
| Contexto de entidad origen | `GET financial-institutions` | `CanReadAch` | lectura auxiliar |
| Previsualizar/validar carga guiada | `POST api/nacha-security/certificates/management/managed/preview` | `P1.CertificatesUploadPublic` | preparación sensible |
| Guardar carga guiada | `POST api/nacha-security/certificates/management/managed` | `P1.CertificatesRegisterPrivate` | mutación/material criptográfico |
| Revocar versión | `POST api/nacha-security/certificates/management/versions/{id}/revoke` | `P1.CertificatesRevoke` | mutación |
| Eliminar versión | `DELETE api/nacha-security/certificates/management/versions/{id}` | `P1.CertificatesRevoke` | mutación |

El controller conserva además endpoints con policies existentes para activar (`P1.CertificatesActivate`), validar (`P1.CertificatesValidate`) y consultar auditoría (`P1.CertificatesAudit`), pero la pantalla vigente inspeccionada no los invoca. Tampoco existe una descarga de material de certificado expuesta por `CertificateManagementApiService`/manager actual.

### Capacidades backend canónicas

```text
P1.CertificatesRead
  Certificates.Read OR CanManageCertificates OR CanReadAch

P1.CertificatesUploadPublic
  Certificates.UploadPublic OR CanManageCertificates

P1.CertificatesRegisterPrivate
  Certificates.RegisterPrivate OR CanManageCertificates

P1.CertificatesRevoke
  Certificates.Revoke OR CanManageCertificates
```

Las policies `CertificatesActivate`, `CertificatesValidate` y `CertificatesAudit` conservan igualmente su capacidad fina correspondiente OR `CanManageCertificates`. `CanReadAch` no satisface las policies mutables.

### Autorización anterior

Antes de NAV3.4F:

```text
MenuItem Certificados digitales
  CanManageCertificates

SPA /nacha-security/certificates
  Certificates.Read OR CanManageCertificates OR CanReadAch

SPA /nacha-security/certificates/:id/versions
  CanManageCertificates OR CanManageAch OR CanReadAch

API
  ya separada por P1.CertificatesRead / UploadPublic / RegisterPrivate /
  Activate / Revoke / Validate / Audit
```

Además, el manager cargaba siempre los catálogos de cámaras y entidades financieras. Como esos GET exigen `CanReadAch`, un principal con sólo `Certificates.Read` superaba el guard pero podía fallar al cargar la pantalla completa.

### Autorización final NAV3.4F

```text
MenuItem Certificados digitales
  Certificates.Read OR CanManageCertificates OR CanReadAch

SPA /nacha-security/certificates
  Certificates.Read OR CanManageCertificates OR CanReadAch

SPA /nacha-security/certificates/:id/versions
  Certificates.Read OR CanManageCertificates OR CanReadAch

Carga guiada SPA
  CanReadAch (contexto de selectores compartidos)
  AND (Certificates.UploadPublic OR CanManageCertificates)
  AND (Certificates.RegisterPrivate OR CanManageCertificates)

Revocar / eliminar SPA
  Certificates.Revoke OR CanManageCertificates

API
  sin cambios; conserva las policies P1 existentes
```

La pantalla carga siempre el inventario principal, pero sólo invoca los dos GET auxiliares cuando el usuario posee `CanReadAch`. Así, `Certificates.Read` por sí solo habilita una experiencia de consulta real del inventario/detalle/versiones sin producir 403 auxiliares. `CanReadAch` por sí solo también permite consulta, pero no mutaciones. Las acciones de carga, revocación y eliminación tienen gating visual y validación fail-closed dentro del método antes de invocar el servicio.

El flujo de carga completo exige además `CanReadAch` porque el wizard vigente necesita los selectores compartidos de cámaras/entidad origen, cuyos controllers no fueron ampliados. Esto es una restricción adicional de SPA, no un bypass; el backend continúa aceptando sus capacidades finas o `CanManageCertificates` según cada operación.

### Material criptográfico y efectos

`SaveManagedCertificateAsync` conserva el comportamiento existente:

- para el certificado de CFA, exige material con llave privada y protege el contenido mediante `ICertificatePrivateMaterialProtector` antes de persistir `EncryptedPrivateMaterial`;
- para cámaras compensadoras, persiste el certificado público correspondiente;
- una nueva versión queda activa y las versiones activas anteriores equivalentes pasan a `Replaced`, con historial de rotación y auditoría;
- la contraseña del PFX/P12 se usa para abrir/validar el certificado y la UI no la conserva como dato funcional.

La revocación persiste `Revoked`, fecha/motivo/actor y auditoría. La eliminación conserva las guardas de referencias operativas, impide retirar material activo en uso sin la transición correspondiente y limpia el material privado cifrado en memoria al eliminar. NAV3.4F no cambia almacenamiento, cifrado, validación, expiración, selección, rotación ni uso criptográfico.

No se identificó descarga de llave privada ni de otro material sensible desde esta pantalla. El detalle sólo expone metadatos técnicos y el hecho de que existe una llave privada protegida.

### Capacidades finas materializadas

Se materializan idempotentemente en `Permission`, porque ya existían en `FineGrainedPermissions`, `AllPermissions` y policies:

- `Certificates.Read`;
- `Certificates.UploadPublic`;
- `Certificates.RegisterPrivate`;
- `Certificates.Revoke`.

No se materializan `Certificates.Activate`, `Certificates.Validate` ni `Certificates.Audit` en NAV3.4F porque no son acciones consumidas por la pantalla vigente. No se agregan roles, `RolePermissions`, grants a Admin/ACH.Operator ni permisos directos a usuarios.

### Alcance y regresión

- `NachaSecurityMenuSeeder` conserva su estado histórico inicial; un reconciliador posterior y focal fija las alternativas de entrada finales del MenuItem sin cambiar Label, Route, ParentId, Order ni IsActive.
- La raíz `Seguridad de archivos` no se modifica. Se observó que no tiene un permiso directo de entrada; el child guard de certificados sigue siendo obligatorio, por lo que no constituye un bypass de la ruta, pero el posible detalle UX/estructural queda fuera de NAV3.4F.
- `/nacha-security/sobre-digital` permanece intacto.
- `/nacha-security/nacha/generate`, `/nacha-security/nacha/generate-encrypted`, `/nacha-security/digital-envelope/manual-encrypt` y `/nacha-security/digital-envelope/manual-decrypt` permanecen intactos y fuera del menú según la decisión previa.
- NAV3.4A–NAV3.4E y Herramientas de prueba permanecen fuera del alcance y deben mantenerse sin regresiones.

Con estas condiciones, el gap específico de `Certificados digitales` queda cerrado a nivel de diseño/código. La verificación runtime .NET/Angular sigue dependiendo del entorno.

### Validaciones ejecutadas y diferidas — NAV3.4F

Validaciones razonables disponibles en el entorno:

- `python scripts/validate-solution-projects.py`: **PASS**, grafo de solución 7/7.
- `python scripts/validate-nav3-4f-certificates.py`: **PASS**, 171 aserciones estáticas focales sobre policies, endpoints, menú, SPA, material criptográfico y regresión de alcance.
- parser TypeScript 5.8.3 sobre 7 archivos focales de `nacha-security`: **PASS**, sin diagnostics de parseo.
- sanity léxico de delimitadores C# sobre los 2 seeders y la prueba focal NAV3.4F: **PASS**.
- comparación SHA-256 de 28 archivos sensibles fuera de los cambios autorizados —controller/policies/servicios criptográficos, Sobre digital/rutas manuales y modelos NAV3.4A–NAV3.4E/TestTools— contra NAV3.4E: **PASS**, byte a byte sin cambios.
- alcance respecto de NAV3.4E: **4 archivos agregados, 9 modificados, 0 eliminados**.

Pruebas runtime diferidas:

- .NET: **NOT_RUN_ENVIRONMENT**; `dotnet` no está instalado en el entorno. No se infieren resultados de compilación o test runtime.
- Angular: **NOT_RUN_ENVIRONMENT**; Node `v22.16.0`, npm `10.9.2` y TypeScript `5.8.3` están disponibles, pero `web/ach-interbank-ui/node_modules` no está instalado. No se ejecutan `ng test`/build y no se inventan resultados.

## NAV3.4G — Registros del sistema: estructura normalizada y diseño de permiso pendiente — 2026-10-14

### Estado

- Incremento ejecutado: **NAV3.4G solamente**.
- Fuente vigente: ZIP FULL de NAV3.4F.
- Git: **no utilizado**.
- NAV2: **IMPLEMENTED_UNVERIFIED_ENVIRONMENT**; no se marca CLOSED porque las pruebas runtime .NET continúan pendientes.
- Cierre del dominio: **parcial**. Se cierra el problema estructural padre/hijo y el drift SPA/API de navegación con la capacidad vigente, pero Auditoría/Accesos y la semántica final de lectura de navegación requieren un permiso de registros del sistema aún inexistente.

### Caracterización funcional

Las tres pantallas administrativas inspeccionadas son de consulta:

- `/audit-logs`: lista auditoría con entidad/ID afectados, acción, actor (`ChangedBy`), fecha y `ChangedFields`. La UI sólo consulta y reintenta la consulta ante error; no borra, modifica, exporta ni ejecuta acciones administrativas.
- `/auth-logs`: lista autenticaciones con usuario, éxito/fallo, motivo, IP, User-Agent y fecha. La UI sólo consulta; la escritura de eventos de autenticación se realiza internamente desde el flujo de login y no mediante esta pantalla.
- `/navigation-logs`: lista usuario, ruta, sesión, duración, IP, User-Agent y fecha. La vista administrativa sólo consulta. Separadamente, `NavigationLogService` envía `POST api/navigation-logs` automáticamente para telemetría del usuario autenticado.

No se identificaron operaciones UI de borrar/modificar logs, cambiar estados, exportar, reprocesar ni ejecutar administración desde estas tres superficies. `NavigationLogsService.PurgeOlderThanAsync` existe como mantenimiento interno y no está publicado por el controller/listado SPA.

### Autorización anterior por capa

```text
MenuItem /logs                    CanManageUsers
MenuItem /audit-logs              CanManageUsers
MenuItem /auth-logs               CanManageUsers
MenuItem /navigation-logs         CanReadAch

SPA /audit-logs                   sólo authGuard raíz
SPA /auth-logs                    sólo authGuard raíz
SPA /navigation-logs              sólo authGuard raíz

GET api/audit-logs                [Authorize]
GET api/auth-logs                 [Authorize]
GET api/navigation-logs           CanReadAch
POST api/navigation-logs          [Authorize] (telemetría automática)
```

### Permisos existentes evaluados

- `CanManageUsers`: representa gestión de usuarios y no es una capacidad inequívoca para leer auditoría general del sistema o autenticaciones.
- `CanReadAch`: representa consulta de operaciones ACH y no es semánticamente una capacidad de lectura general de navegación/telemetría.
- permisos de auditoría existentes como `Certificates.Audit` o `CanViewNachaSecurityAudit` son específicos de otros dominios y no aplican a estos logs generales.
- no existen `SystemLogs.Read`, `AuditLogs.Read`, `AuthLogs.Read` ni `NavigationLogs.Read` en `FineGrainedPermissions`, policies o `Permission` seed.

Por la regla de NAV3.4G, no se inventa una capacidad nueva ni se agregan grants. Por tanto, Auditoría y Accesos permanecen sin normalización end-to-end y se documentan como sub-gap pendiente, en vez de elegir `CanManageUsers`/`CanReadAch` por inercia.

### Resolución del agrupador padre

`GetMenuForCurrentUserHandler` filtra cada fila antes de reconstruir el árbol y, cuando el padre no está en `dtoLookup`, agrega el hijo visible directamente a `roots`. No se modifica este algoritmo general.

Para el caso actual se introduce `SystemRecordsMenuAuthorizationSeeder` con Order 107. El seeder reconcilia **sólo** `/logs` a:

```text
CanManageUsers OR CanReadAch
```

Así el padre estructural `Registros del sistema` permanece visible cuando existe hoy un hijo autorizado por cualquiera de esas dos familias, mientras cada hijo sigue evaluando su propia autorización. Un usuario `CanReadAch` puede ver el padre con `Registro de navegación` sin que ese hijo flote como raíz; un usuario `CanManageUsers` mantiene el padre con Auditoría/Accesos; hacer visible el padre no hace visible un hijo no autorizado.

El padre conserva `Order 30` bajo `Administración`; hijos Auditoría/Accesos/Navegación conservan órdenes 10/20/30. No cambian labels, rutas, ParentId ni jerarquía.

### Navegación: normalización realizable con capacidad existente

El GET administrativo de `api/navigation-logs` ya exige `CanReadAch`; NAV3.4G alinea `/navigation-logs` con `permissionGuard` + `CanReadAch`. No se agregan roles hardcodeados.

El POST automático de telemetría conserva únicamente la autenticación heredada del controller. El cliente de telemetría continúa ejecutándose para cualquier sesión autenticada y no comprueba `CanReadAch`, de modo que proteger la consulta administrativa no rompe el registro automático de navegación.

`CanReadAch` se conserva aquí sólo como frontera vigente ya existente en el backend; NAV3.4G **no concluye que sea la capacidad semántica final correcta**. Debe reemplazarse o confirmarse cuando se diseñe la capacidad específica de lectura de Registros del sistema.

### Auditoría y Accesos: bloqueo de diseño

Ambas superficies son read-only pero sensibles. MenuItem conserva `CanManageUsers`, mientras ruta SPA y GET API continúan sólo autenticados. Endurecer API/SPA con `CanManageUsers` consolidaría una semántica de gestión de usuarios que no representa inequívocamente los datos auditados; usar `CanReadAch` tampoco corresponde a auditoría/autenticación general.

En ausencia de una capacidad existente adecuada, NAV3.4G no modifica esas capas. Esto significa que el sub-gap de acceso por URL directa para Auditoría/Accesos **permanece abierto** y no debe declararse cerrado. El siguiente incremento debe diseñar primero la capacidad de lectura de Registros del sistema y recién después alinear MenuItem, SPA y API.

### Regresión y alcance

- `/reports/audit` permanece bajo `Reportes`, con su servicio `api/reports/audit`; no se confunde con `/audit-logs`.
- `/logs` conserva sus redirects SPA existentes y su función estructural en menú.
- `GetMenuForCurrentUserHandler` no se modifica.
- No se modifica backend de AuditLogs/AuthLogs/NavigationLogs.
- No se crean permisos, policies, roles, `RolePermissions` ni grants.
- NAV3.4A–NAV3.4F y Herramientas de prueba permanecen fuera de alcance.

### Validaciones — NAV3.4G

- `python scripts/validate-solution-projects.py`: **PASS**, grafo de solución 7/7.
- `python scripts/validate-nav3-4g-system-records.py`: **PASS**, 138 aserciones estáticas focales sobre estructura, datos sensibles, endpoints, separación GET/telemetría, guard SPA y ausencia de permiso especulativo.
- parser TypeScript 5.8.3 sobre `app-routing.module.ts` y `app-routing-access.spec.ts`: **PASS**, sin diagnostics de parseo.
- sanity léxico de delimitadores C# sobre el reconciliador, la prueba focal y el seeder IA comentado: **PASS**.
- comparación SHA-256 contra NAV3.4F: **3 archivos agregados, 6 modificados, 0 eliminados**; los otros **4221 archivos preexistentes** permanecen byte a byte intactos.
- .NET: **NOT_RUN_ENVIRONMENT**; `dotnet` no está instalado en el host. No se infieren resultados de compilación o tests runtime.
- Angular: **NOT_RUN_ENVIRONMENT**; Node `v22.16.0`, npm `10.9.2` y TypeScript `5.8.3` están disponibles, pero `web/ach-interbank-ui/node_modules` no está instalado. No se ejecutan `ng test`/build ni se inventan resultados.

### Gap residual y siguiente incremento

`Registros del sistema` **no queda completamente cerrado** en NAV3.4G. Quedan pendientes la capacidad semántica y la alineación end-to-end de `/audit-logs` y `/auth-logs`, más la sustitución/confirmación del `CanReadAch` temporal de lectura de navegación.

Siguiente incremento recomendado, sin avanzar automáticamente: **NAV3.4G2 — Diseño y aplicación de la capacidad de lectura de Registros del sistema**, limitado a decidir si corresponde una capacidad común o capacidades separadas para auditoría/accesos/navegación y luego alinear las tres capas y el padre estructural. No tocar otros dominios en ese incremento.


## NAV3.4G2 — Capacidades semánticas separadas para Registros del sistema — 2026-10-14

### Estado

- Incremento ejecutado: **NAV3.4G2 solamente**.
- Fuente única: `ACHInterbank-R34-NAV-IA-NAV3.4G-FULL-PROJECT.zip`, SHA-256 `6c3c37168c131f54b36a036fa4307485a8ae7aa04920eff3bb109c584ac75eeb`.
- Git: **no utilizado**.
- NAV2: **IMPLEMENTED_UNVERIFIED_ENVIRONMENT**; no se marca CLOSED porque el runtime .NET sigue pendiente.
- Gap técnico `Registros del sistema`: **cerrado a nivel de diseño/código**, con verificación runtime diferida por entorno.

### Capacidades finales y persistencia

Se crean exactamente las capacidades internas:

- `AuditLogs.Read` — `Consultar registro de auditoría`.
- `AuthLogs.Read` — `Consultar registro de accesos`.
- `NavigationLogs.Read` — `Consultar registro de navegación`.

`SystemRecordPermissionsSeeder` las persiste idempotentemente en `Permission` con IDs estables y `Order 0`, siguiendo el patrón existente `name -> id -> upsert`. Las tres se agregan a `FineGrainedPermissions.AllPermissions`.

No se crean roles, `RolePermissions`, grants a `Admin` o `ACH.Operator`, ni grants directos a usuarios. La audiencia efectiva existente se conserva mediante fallbacks legacy; la migración de grants queda explícitamente diferida.

### Policies finales

```text
P1.AuditLogsRead
  AuditLogs.Read OR CanManageUsers

P1.AuthLogsRead
  AuthLogs.Read OR CanManageUsers

P1.NavigationLogsRead
  NavigationLogs.Read OR CanReadAch
```

Las policies reutilizan la infraestructura `AddAuthorization`/`RequireAssertion`; no hacen checks por nombre de rol.

### Autorización por superficie: anterior -> final

```text
Registro de auditoría /audit-logs
  anterior MenuItem: CanManageUsers
  anterior SPA: sólo autenticado
  anterior GET API: sólo [Authorize]
  final MenuItem: AuditLogs.Read OR CanManageUsers
  final SPA: AuditLogs.Read OR CanManageUsers
  final GET API: P1.AuditLogsRead

Registro de accesos /auth-logs
  anterior MenuItem: CanManageUsers
  anterior SPA: sólo autenticado
  anterior GET API: sólo [Authorize]
  final MenuItem: AuthLogs.Read OR CanManageUsers
  final SPA: AuthLogs.Read OR CanManageUsers
  final GET API: P1.AuthLogsRead

Registro de navegación /navigation-logs
  anterior MenuItem: CanReadAch
  anterior SPA: CanReadAch (desde NAV3.4G)
  anterior GET API: CanReadAch
  final MenuItem: NavigationLogs.Read OR CanReadAch
  final SPA: NavigationLogs.Read OR CanReadAch
  final GET API: P1.NavigationLogsRead
```

Un usuario solamente autenticado ya no satisface los GET administrativos de Auditoría o Accesos. `/reports/audit` permanece intacto bajo Reportes y conserva su contrato/autorización anterior.

### POST de telemetría

`POST api/navigation-logs` sigue heredando únicamente `[Authorize]` del controller. No usa `P1.NavigationLogsRead`, `NavigationLogs.Read` ni `CanReadAch`. El cliente automático continúa condicionado sólo a una sesión autenticada. La escritura automática de telemetría permanece separada de la consulta administrativa.

### Padre `Registros del sistema`

`/logs` conserva `Administración > Registros del sistema`, `Order 30`, y continúa como contenedor estructural. Sus alternativas directas quedan exactamente:

```text
AuditLogs.Read
OR AuthLogs.Read
OR NavigationLogs.Read
OR CanManageUsers
OR CanReadAch
```

Los hijos conservan autorización propia. Por tanto:

- usuario sólo `NavigationLogs.Read` -> recibe padre + Registro de navegación; no recibe Auditoría ni Accesos;
- usuario `CanManageUsers` -> recibe padre + Auditoría + Accesos; no recibe Navegación salvo `CanReadAch` o `NavigationLogs.Read`;
- hacer visible el padre no concede acceso a ningún hijo.

`GetMenuForCurrentUserHandler` permanece intacto. La representación por asociaciones de permisos evita el orphaning conocido sin refactor general.

### Validaciones ejecutadas y diferidas

Ejecutadas:

- `python scripts/validate-solution-projects.py`: **PASS**, 7/7 proyectos y referencias válidas.
- `python scripts/validate-nav3-3b-service-tests.py`: **PASS**, 39 aserciones; Herramientas de prueba intacta.
- `python scripts/validate-nav3-4b-transaction-create.py`: **PASS**, 72 aserciones.
- `python scripts/validate-nav3-4c-nacha-upload.py`: **PASS**, 87 aserciones.
- `python scripts/validate-nav3-4d-returns.py`: **PASS**, 100 aserciones.
- `python scripts/validate-nav3-4e-bulk-ingestion-tracking.py`: **PASS**, 153 aserciones.
- `python scripts/validate-nav3-4f-certificates.py`: **PASS**, 171 aserciones.
- `python scripts/validate-nav3-4g2-system-records.py`: **PASS**, 85 aserciones focales.

Diferidas:

- .NET: **NOT_RUN_ENVIRONMENT**; `dotnet` no está instalado, por lo que no se ejecutaron build/tests runtime ni idempotencia contra una base real.
- Angular: **NOT_RUN_ENVIRONMENT**; Node `v22.16.0` y npm `10.9.2` están disponibles, pero `web/ach-interbank-ui/node_modules` no existe. No se ejecutaron `ng test`/build.

### Deuda residual y siguiente incremento

No queda bypass conocido sólo-autenticado en las tres consultas administrativas de Registros del sistema y no queda riesgo conocido de hijo huérfano con las asociaciones finales. La deuda deliberada es **migrar grants** desde `CanManageUsers`/`CanReadAch` hacia las capacidades dedicadas y, sólo después de completar esa migración, retirar los fallbacks legacy en un incremento separado con población objetivo explícita.

No avanzar automáticamente. Recomendación exacta del siguiente incremento NAV3: **NAV3.4H — Organización del menú: normalizar MenuItem/SPA/API de `/navigation/menu-items` sin tocar Registros del sistema, Capacidades de la plataforma, Cifrar y descifrar archivos ni MFT/Integraciones**. Primero caracterizar las operaciones reales y el drift actual `Admin + CanManageUsers` vs API `AdminOnly`; no crear una capacidad nueva hasta verificar las convenciones existentes.


## NAV3.4H — Organización del menú: capacidad semántica y alineación MenuItem/SPA/API — 2026-10-14

### Estado

- Incremento ejecutado: **NAV3.4H únicamente**.
- Fuente exclusiva: `ACHInterbank-R34-NAV-IA-NAV3.4G2-FULL-PROJECT.zip`, SHA-256 `29162e9b26708fb0a14cce05570bb4196f54d51b7b97c297bbc32cf080629a74`.
- Git: **no usado**.
- NAV2: **IMPLEMENTED_UNVERIFIED_ENVIRONMENT**; no se marca `CLOSED` porque sigue pendiente la validación runtime .NET.
- `GetMenuForCurrentUserHandler`: **sin cambios**.
- NAV3.4A–NAV3.4G2, Herramientas de prueba, `/navigation-logs` y `/reports/audit`: sin cambios funcionales.

### Caracterización real de `Organización del menú`

La pantalla efectiva es `/navigation/menu-items`, montada bajo el prefijo SPA `/navigation`. `/navigation` no representa una segunda pantalla: el módulo conserva `path: '' -> redirectTo: 'menu-items'`.

La UI usa `NavigationMenuComponent` y `NavigationMenuService`. El CRUD principal consume `api/navigation/menu-items`; adicionalmente carga `GET api/roles` y `GET api/permissions` para los selectores de asociaciones de autorización.

Operaciones reales disponibles:

- listar el árbol completo de `MenuItem`;
- crear opciones;
- editar `Label`, `Route`, `Icon`, `ParentId`, `Order`, `Exact` e `IsActive`;
- activar/desactivar mediante el mismo `PUT` de edición;
- cambiar padre y orden, lo que permite reorganizar la jerarquía;
- eliminar una opción sin hijos;
- modificar `RoleIds`, persistidos como `MenuItemRole`;
- modificar `PermissionIds`, persistidos como `MenuItemPermission`.

No existe endpoint PATCH/reorder/enable-disable separado. El controller expone únicamente `GET`, `POST`, `PUT {id}` y `DELETE {id}` en los aliases `navigation/menu-items` y `api/navigation/menu-items`. `MenuItemsService` persiste la estructura del `MenuItem` y reconcilia `MenuItemRole`/`MenuItemPermission`; no crea ni modifica `RolePermission`, `UserRole` o grants directos a usuarios.

Conclusión: la superficie administra **estructura/presentación y metadatos de autorización del menú**; no administra por sí misma la capacidad real de backend/SPA que cada destino protege. Por ello se trata como superficie administrativa sensible.

### Autorización previa

Antes de NAV3.4H:

| Capa | Autorización efectiva en fuente |
|---|---|
| MenuItem `Organización del menú` | `Admin` **AND** `CanManageUsers` por `MenuItemRole` + `MenuItemPermission` |
| SPA `/navigation` | `roleGuard(Admin)` **AND** `permissionGuard(CanManageUsers)` |
| SPA `/navigation/menu-items` | heredaba el gate del prefijo; no tenía guard propio |
| API `navigation/menu-items` / `api/navigation/menu-items` | `[Authorize(Roles = "Admin")]` |
| Componente mutable | `hasRole('Admin') && hasPermission('CanManageUsers')` |

El modelo fuente canónico contiene sólo los roles base `Admin` y `ACH.Operator`; `RolePermissionConfiguration` concede `CanManageUsers` a `Admin` y no a `ACH.Operator`. Por tanto, para los grants sembrados vigentes, las poblaciones `Admin` y `CanManageUsers` son equivalentes para esta superficie. No existe evidencia runtime de una base desplegada adicional en este entorno; la afirmación de audiencia se limita al estado fuente canónico del ZIP.

### Capacidades existentes evaluadas

Se inspeccionaron `FineGrainedPermissions`, `AllPermissions`, policies y el catálogo `Permission`. Las capacidades de usuarios (`Users.*`, `CanManageUsers`), seguridad (`Permissions.Assign`), configuración (`Config.Manage`), logs (`NavigationLogs.Read`) y las restantes capacidades de navegación/administración no representan inequívocamente **Administrar organización del menú**.

`CanManageUsers` se conserva sólo como compatibilidad temporal de audiencia; no se adopta como semántica definitiva.

### Capacidad y persistencia finales

Se crea la capacidad dedicada siguiendo la convención `Domain.Action`:

```text
NavigationMenu.Manage
Administrar organización del menú
```

`NavigationMenuPermissionSeeder` la persiste idempotentemente en `Permission` con ID estable `5f847759-419c-4cf5-b2bb-2a849b8fa33c`, búsqueda por nombre/ID y normalización de nombre/descripción. Se agrega a `FineGrainedPermissions.AllPermissions`.

No se crean roles, `RolePermission`, grants a `Admin`, grants a `ACH.Operator` ni grants directos a usuarios. `RolePermissionConfiguration` permanece intacto.

### Policy y autorización final

La policy semántica final es:

```text
P1.NavigationMenuManage
  NavigationMenu.Manage OR CanManageUsers
```

`NavigationMenuAuthorizationSeeder` (Order 108, después de la canonicalización IA y de NAV3.4G2) reconcilia sólo el MenuItem `NavigationAdminId` a:

```text
MenuItem /navigation/menu-items
  NavigationMenu.Manage OR CanManageUsers
  sin MenuItemRole directo
```

La eliminación del `MenuItemRole Admin` evita convertir accidentalmente la capacidad dedicada en `Admin AND permiso`; el padre/menú no concede capacidades y `GetMenuForCurrentUserHandler` no se modifica. Label, Route, ParentId, Order 40 e IsActive se conservan.

SPA final:

```text
/navigation
  permissionGuard: NavigationMenu.Manage OR CanManageUsers
  redirect interno sin cambios -> menu-items

/navigation/menu-items
  permissionGuard: NavigationMenu.Manage OR CanManageUsers
```

El componente usa la misma alternativa para habilitar acciones mutables y ya no contiene un check funcional por nombre de rol. El guard explícito del hijo impide usar la URL directa como bypass aunque cambie la estructura del módulo padre en el futuro.

API final:

```text
GET    navigation/menu-items / api/navigation/menu-items
POST   navigation/menu-items / api/navigation/menu-items
PUT    navigation/menu-items/{id} / api/navigation/menu-items/{id}
DELETE navigation/menu-items/{id} / api/navigation/menu-items/{id}

[Authorize(Policy = P1Policies.NavigationMenuManage)]
```

El rol `Admin` deja de ser la frontera hardcodeada del controller. Admin conserva acceso por su grant legado `CanManageUsers`; `ACH.Operator` no obtiene acceso nuevo.

### Dependencias de catálogos de roles/permisos

La misma pantalla necesita leer roles y permisos para editar `RoleIds`/`PermissionIds`. Para que una futura población con sólo `NavigationMenu.Manage` pueda usar la funcionalidad completa sin recibir permisos de gestión de usuarios, las policies de lectura quedan:

```text
P1.RolesRead       = Roles.Read OR NavigationMenu.Manage OR CanReadAch
P1.PermissionsRead = Permissions.Read OR NavigationMenu.Manage OR CanReadAch
```

Esto no cambia la audiencia actual porque `NavigationMenu.Manage` se crea sin grants. Las policies de crear/editar/eliminar roles y asignar permisos permanecen intactas.

### Audiencia antes/después y escalación

En el estado fuente canónico:

- antes: MenuItem/SPA requerían Admin + `CanManageUsers`; API requería Admin; el único principal sembrado que satisface la combinación es Admin;
- después: todas las capas aceptan `NavigationMenu.Manage OR CanManageUsers`; como la capacidad nueva no tiene grants y `CanManageUsers` continúa sembrado sólo para Admin, la audiencia fuente efectiva **no cambia**;
- `ACH.Operator` no obtiene acceso nuevo.

Riesgo sensible caracterizado: quien administra esta pantalla puede cambiar qué roles/permisos hacen visible cualquier opción de menú, moverla, cambiar su ruta o desactivarla. Sin embargo, la pantalla no puede autoasignarse `RolePermission`, `UserRole` ni claims, por lo que no se encontró una vía directa de escalación de privilegios desde `NavigationMenu.Manage` hacia capacidades de negocio. Mostrar un enlace no sustituye los guards SPA ni las policies API del destino. Un posible gap de autorización downstream preexistente seguiría siendo deuda de esa superficie y no se corrige en NAV3.4H.

No queda bypass conocido de `/navigation/menu-items` por URL directa o por CRUD API bajo el modelo inspeccionado.

### Validaciones ejecutadas y diferidas

Ejecutadas:

- `python scripts/validate-solution-projects.py`: **PASS**, 7/7 proyectos y referencias válidas.
- `python scripts/validate-nav3-3b-service-tests.py`: **PASS**, 39 aserciones.
- `python scripts/validate-nav3-4b-transaction-create.py`: **PASS**, 72 aserciones.
- `python scripts/validate-nav3-4c-nacha-upload.py`: **PASS**, 87 aserciones.
- `python scripts/validate-nav3-4d-returns.py`: **PASS**, 100 aserciones.
- `python scripts/validate-nav3-4e-bulk-ingestion-tracking.py`: **PASS**, 153 aserciones.
- `python scripts/validate-nav3-4f-certificates.py`: **PASS**, 171 aserciones.
- `python scripts/validate-nav3-4g2-system-records.py`: **PASS**, 85 aserciones.
- `python scripts/validate-nav3-4h-navigation-menu.py`: **PASS**, 94 aserciones focales sobre caracterización, capability/persistencia, audiencia seed, MenuItem, SPA, API, dependencias de roles/permisos y regresión.

Diferidas:

- .NET: **NOT_RUN_ENVIRONMENT**; `dotnet` no está instalado. No se inventan build/tests runtime ni idempotencia contra base real.
- Angular: **NOT_RUN_ENVIRONMENT**; Node/npm están disponibles pero `web/ach-interbank-ui/node_modules` no existe. No se ejecutan `ng test`/build.

### Cierre técnico y deuda residual

El gap técnico de autorización de `Organización del menú` puede considerarse cerrado a nivel de diseño/código: MenuItem, SPA y CRUD API usan una frontera semántica común, no hay gate final por nombre de rol y la URL directa no evita el guard hijo. NAV2 continúa no cerrado por su verificación runtime pendiente.

Deuda deliberada posterior: decidir explícitamente qué población debe recibir `NavigationMenu.Manage`, migrar el grant desde `CanManageUsers` hacia esa capacidad y, sólo después de validar la audiencia, retirar el fallback `CanManageUsers`. NAV3.4H no realiza esa migración.

No avanzar automáticamente. Recomendación exacta del siguiente incremento NAV3: **NAV3.4I — Capacidades de la plataforma: normalizar la autorización de `/payment-rail-capability-registry`, materializar o reutilizar su capacidad semántica y alinear MenuItem/SPA/API sin tocar Cifrar y descifrar archivos, devoluciones de una devolución, carga desde archivo, MFT/Integraciones ni migraciones de grants previas**.


## NAV3.4I — Capacidades de la plataforma: materialización de capability y alineación MenuItem/SPA/API — 2026-10-14

### Estado

- Incremento ejecutado: **NAV3.4I únicamente**.
- Fuente exclusiva: `ACHInterbank-R34-NAV-IA-NAV3.4H-FULL-PROJECT.zip`, SHA-256 `1f798437fd4c2d4bc71becf33eed5ea8392335cc9e6ad596bdd05c5fe2ee0255`.
- Git: **no usado**.
- NAV2: **IMPLEMENTED_UNVERIFIED_ENVIRONMENT**; no se marca `CLOSED` porque la validación runtime .NET continúa pendiente.
- `GetMenuForCurrentUserHandler`: **sin cambios**.
- NAV3.4A–NAV3.4H, Herramientas de prueba, Organización del menú y Registros del sistema: sin cambios funcionales.

### Propósito funcional confirmado y superficie real

`/payment-rail-capability-registry` continúa siendo **Capacidades de la plataforma**, una consulta administrativa read-only del estado efectivo de capacidades por riel. Permanece en:

```text
Administración
└── 50 Capacidades de la plataforma
    /payment-rail-capability-registry
```

La SPA usa `PaymentRailCapabilityRegistryPageComponent` y `PaymentRailCapabilityRegistryApiService`. El servicio Angular consume exclusivamente:

```text
GET api/payment-rails/capability-registry/rails
GET api/payment-rails/capability-registry/rails/{railCode}/capabilities
GET api/payment-rails/capability-registry/rails/{railCode}/capabilities/{capabilityCode}
```

La UI permite seleccionar riel, filtrar por capability e instante `asOfUtc`, consultar y limpiar filtros. No expone edición, activación/desactivación, cutover, modificación de rieles, modificación de capacidades ni escritura de configuración. El HTML sigue declarando explícitamente `Solo lectura`, `Esta pantalla no permite edición` y `sin cutover`.

`PaymentRailCapabilityRegistryController` publica exactamente los mismos tres endpoints `GET` y delega directamente en `IPaymentRailCapabilityRegistryService`/`PaymentRailCapabilityRegistryService`; esta superficie no usa un handler MediatR/read-model separado. No publica `POST`, `PUT`, `PATCH` ni `DELETE`.

Existe en el servicio interno `IPaymentRailCapabilityRegistryService`/`PaymentRailCapabilityRegistryService` una operación `UpsertCapabilityAsync` que persiste overrides. NAV3.4I confirma que **esa mutación no está expuesta por esta UI ni por este controller**: no tiene endpoint HTTP aquí, no es llamada por el servicio Angular y no se incorpora al alcance del incremento. Por tanto, la policy de lectura no se reutiliza para una mutación.

### Autorización anterior

Antes de NAV3.4I:

| Capa | Autorización efectiva |
|---|---|
| MenuItem `Capacidades de la plataforma` | `(Admin OR ACH.Operator) AND CanReadAch` |
| SPA `/payment-rail-capability-registry` | `(Admin OR ACH.Operator) AND (CanViewPaymentRailCapabilityRegistry OR CanManageAch OR CanReadAch)` |
| API GET | policy nombrada `CanViewPaymentRailCapabilityRegistry` |

La implementación efectiva de esa policy ya era y continúa siendo:

```text
CanViewPaymentRailCapabilityRegistry
OR CanManageAch
OR CanReadAch
```

No contiene checks por nombre de rol.

### Estado previo de la capability canónica

`CanViewPaymentRailCapabilityRegistry` ya existía como constante de `FineGrainedPermissions` y como nombre de policy backend. No existía otra capability equivalente que debiera sustituirla.

Antes de NAV3.4I:

- no estaba incluida en `FineGrainedPermissions.AllPermissions`;
- no existía una fila `Permission` sembrada para ese nombre;
- no tenía `RolePermission`;
- no tenía grants directos a usuarios.

`PermissionConfiguration` continúa sin duplicarla como `HasData`: NAV3.4I usa el patrón de seeders idempotentes introducido por los incrementos de seguridad recientes.

### Persistencia final

`PaymentRailCapabilityRegistryPermissionSeeder` materializa idempotentemente la capability existente con ID estable:

```text
CanViewPaymentRailCapabilityRegistry
Consultar capacidades de la plataforma
9c178737-a650-4437-a68b-9b3f21555e02
```

El seeder busca primero por nombre, luego por ID, crea sólo si falta y normaliza `Name`/`Description`. `CanViewPaymentRailCapabilityRegistry` se incorpora a `FineGrainedPermissions.AllPermissions`.

No se crean roles, `RolePermission`, grants a `Admin`, grants a `ACH.Operator` ni grants directos a usuarios.

### Audiencia fuente antes de la normalización

El estado fuente canónico define únicamente los roles base `Admin` y `ACH.Operator`. `RolePermissionConfiguration` concede a **ambos**:

```text
CanManageAch
CanReadAch
```

Ninguno recibe `CanViewPaymentRailCapabilityRegistry` en NAV3.4I.

Por tanto, con los grants sembrados del ZIP, tanto el MenuItem/SPA previo como el API ya tenían como población efectiva a los dos roles base. La restricción de rol en menú/SPA era redundante respecto de los fallbacks sembrados y más estricta que la policy funcional del API.

No hubo runtime DB disponible. La afirmación de equivalencia se limita al estado fuente canónico; no se afirma que una base desplegada externa carezca de roles personalizados con `CanManageAch` o `CanReadAch`.

### Autorización final

La policy backend **no cambia** porque ya representaba correctamente la semántica de migración:

```text
CanViewPaymentRailCapabilityRegistry
OR CanManageAch
OR CanReadAch
```

Los fallbacks `CanManageAch` y `CanReadAch` se conservan temporalmente. La capability dedicada nace sin grants.

`PaymentRailCapabilityRegistryMenuAuthorizationSeeder` (Order 109, posterior a la canonicalización IA y a los reconciliadores NAV3.4G2/NAV3.4H) reconcilia únicamente `Capacidades de la plataforma` a tres alternativas `MenuItemPermission`:

```text
CanViewPaymentRailCapabilityRegistry
OR CanManageAch
OR CanReadAch
```

Los `MenuItemRole` `Admin`/`ACH.Operator` de ese hijo se eliminan. Esto evita convertir la frontera funcional en `rol AND permiso` cuando la policy API es exclusivamente permission-based. Label, Route, ParentId, `Order 50`, `IsActive` y la arquitectura NAV2 no cambian.

SPA final:

```text
/payment-rail-capability-registry
  permissionGuard:
    CanViewPaymentRailCapabilityRegistry
    OR CanManageAch
    OR CanReadAch
  sin roleGuard
  sin roles hardcodeados
```

El acceso directo por URL continúa protegido porque el guard permanece en la ruta efectiva.

API final — **sin cambio de comportamiento**:

```text
GET api/payment-rails/capability-registry/rails
GET api/payment-rails/capability-registry/rails/{railCode}/capabilities
GET api/payment-rails/capability-registry/rails/{railCode}/capabilities/{capabilityCode}

[Authorize(Policy = FineGrainedPermissions.CanViewPaymentRailCapabilityRegistry)]
```

La evaluación efectiva de la policy continúa aceptando la capability dedicada o cualquiera de los dos fallbacks legacy.

### MenuItemRole y semántica del padre

Se eliminan del hijo `Capacidades de la plataforma` los `MenuItemRole` heredados `Admin` y `ACH.Operator`, porque no forman parte de la frontera funcional del API y, con los grants fuente vigentes, no reducían la población efectiva frente a `CanManageAch`/`CanReadAch`.

`Administración` no se modifica. No se cambia la autorización de ningún hermano ni se modifica `GetMenuForCurrentUserHandler`. El hijo sigue bajo el mismo padre y no se introduce orphaning por esta normalización.

### Comparación de audiencia antes/después

En el **estado fuente canónico sembrado**:

- Admin conserva acceso mediante `CanManageAch` y `CanReadAch`;
- ACH.Operator conserva acceso mediante `CanManageAch` y `CanReadAch`;
- ninguno recibe la capability dedicada;
- no aparece ningún rol nuevo;
- no aparece ningún grant nuevo.

Por tanto, NAV3.4I **no amplía la audiencia sembrada**. La capa menú/SPA pasa a expresar la misma frontera por permisos que el API ya aceptaba. Una base desplegada con roles personalizados no fue inspeccionada en este entorno; esa población runtime no se inventa ni se declara verificada.

### Validaciones ejecutadas y diferidas

Ejecutadas:

- `python scripts/validate-solution-projects.py`: **PASS**, 7/7 proyectos y referencias válidas.
- `python scripts/validate-nav3-3b-service-tests.py`: **PASS**, 39 aserciones.
- `python scripts/validate-nav3-4b-transaction-create.py`: **PASS**, 72 aserciones.
- `python scripts/validate-nav3-4c-nacha-upload.py`: **PASS**, 87 aserciones.
- `python scripts/validate-nav3-4d-returns.py`: **PASS**, 100 aserciones.
- `python scripts/validate-nav3-4e-bulk-ingestion-tracking.py`: **PASS**, 153 aserciones.
- `python scripts/validate-nav3-4f-certificates.py`: **PASS**, 171 aserciones.
- `python scripts/validate-nav3-4g2-system-records.py`: **PASS**, 85 aserciones.
- `python scripts/validate-nav3-4h-navigation-menu.py`: **PASS**, 94 aserciones.
- `python scripts/validate-nav3-4i-platform-capabilities.py`: **PASS**, 73 aserciones focales.
- parseo TypeScript focal con TypeScript 5.8.3: **PASS** para `app-routing.module.ts`, el componente y el servicio de Capability Registry.

El validador histórico `scripts/validate-nav3-4g-system-records.py` continúa fallando antes de ejecutar aserciones porque referencia `NavigationMenuNav34GSystemRecordsAuthorizationTests.cs`, archivo ya ausente/sustituido por NAV3.4G2. El mismo fallo se reproduce sobre el ZIP fuente NAV3.4H, por lo que no es una regresión de NAV3.4I. No se corrige por estar fuera del alcance.

Diferidas:

- .NET: **NOT_RUN_ENVIRONMENT**; `dotnet` no está instalado, por lo que no se ejecutan build, tests .NET ni idempotencia runtime contra una base real.
- Angular: **NOT_RUN_ENVIRONMENT**; `web/ach-interbank-ui/node_modules` no existe, por lo que no se ejecutan `ng test` ni build Angular. El parseo TypeScript focal no sustituye esas pruebas.

NAV2 permanece **IMPLEMENTED_UNVERIFIED_ENVIRONMENT**.

### Gap residual y siguiente incremento

No queda bypass conocido de URL/API para `Capacidades de la plataforma`: la SPA exige una de las tres alternativas y todos los endpoints públicos de esta superficie son GET protegidos por la policy canónica. La operación interna `UpsertCapabilityAsync` sigue fuera de esta UI/controller y no recibe exposición nueva.

El gap de autorización de esta superficie puede considerarse **cerrado a nivel de diseño/código**, con la verificación runtime pendiente por entorno.

La deuda deliberada es de migración de grants: decidir qué población debe recibir `CanViewPaymentRailCapabilityRegistry`, otorgar explícitamente esa capability y, sólo después de validar la audiencia, retirar de forma separada los fallbacks `CanManageAch`/`CanReadAch`. NAV3.4I no realiza esa migración.

No avanzar automáticamente. Recomendación exacta del siguiente incremento NAV3: **NAV3.4J — Cifrar y descifrar archivos: caracterizar y normalizar la autorización de `/nacha-security/sobre-digital` y únicamente las operaciones realmente consumidas por esa UI, sin tocar Devolución de una devolución, Cargar transacciones desde archivo, MFT/Integraciones ni migraciones de grants previas**.

## NAV3.4J — Cifrar y descifrar archivos: modelo mixto de entrada y operaciones criptográficas — 2026-10-14

### Estado

- Incremento ejecutado: **NAV3.4J únicamente**.
- Fuente exclusiva: `ACHInterbank-R34-NAV-IA-NAV3.4I-FULL-PROJECT.zip`, SHA-256 `990c744b0549c76f4f6dcfd8efcba427f42f0f794c5dcb43d84c0a24ae7f3615`.
- Git: **no usado**.
- NAV2: **IMPLEMENTED_UNVERIFIED_ENVIRONMENT**; no se marca `CLOSED` porque la validación runtime .NET continúa pendiente.
- `GetMenuForCurrentUserHandler`: **sin cambios**.
- NAV3.4A–NAV3.4I y Herramientas de prueba: sin cambios funcionales fuera de la normalización focal de `/nacha-security/sobre-digital`.
- `/nacha-security/certificates` y las cuatro rutas manuales/técnicas permanecen intactas.

### Comportamiento funcional real de Sobre digital

La superficie canónica permanece:

```text
Seguridad de archivos
└── 20 Cifrar y descifrar archivos
    /nacha-security/sobre-digital
```

La SPA usa `DigitalEnvelopeToolComponent`, `SobreDigitalService`, `ClearingHousesApiService` y `BlobDownloadService`.

Operaciones reales:

1. **Consulta/preparación**:
   - carga metadatos de versiones de certificados utilizables;
   - carga cámaras compensadoras operativas;
   - permite elegir cámara, ambiente `Test`/`Production`, modo `LIVE` y archivo local;
   - resuelve en la UI la versión de certificado aplicable a partir de los metadatos recibidos.
2. **Cifrado**:
   - recibe un archivo local de hasta 50 MB;
   - el backend cifra el contenido y además firma el contenido comprimido con un certificado privado de firma resuelto del lado servidor;
   - devuelve el artefacto resultante como `application/octet-stream` con sufijo `.ENV`.
3. **Descifrado**:
   - recibe un archivo `.ENV` de hasta 50 MB;
   - el backend resuelve la identidad privada CFA del lado servidor, descifra, descomprime y valida firma/integridad antes de devolver el contenido recuperado.
4. **Descarga**:
   - no existe endpoint de descarga independiente para esta pantalla;
   - la primera descarga es el `Blob` devuelto por el mismo `POST encrypt/decrypt`;
   - `downloadAgain()` vuelve a guardar en el navegador la respuesta ya almacenada en memoria y no ejecuta otra operación backend.

La pantalla **no** genera NACHA-M, no importa el resultado a otra función, no encola procesamiento y no transmite por SOAP/MFT/SFTP ni a terceros. El flujo inspeccionado no persiste el contenido del archivo cifrado o descifrado; persiste únicamente auditoría operativa en `DigitalEnvelopeOperationLogs` con nombres, tamaños, hashes SHA-256, certificado, resultado/error y actor.

### Endpoints realmente consumidos

```text
GET  api/nacha-security/digital-envelope/certificates
  P1.CertificatesRead
  = Certificates.Read OR CanManageCertificates OR CanReadAch

GET  api/clearing-houses/operational
  CanReadAch

POST api/nacha-security/digital-envelope/encrypt
  P1.DigitalEnvelopeEncrypt
  = DigitalEnvelope.Encrypt OR CanManageAch

POST api/nacha-security/digital-envelope/decrypt
  P1.DigitalEnvelopeDecrypt
  = DigitalEnvelope.Decrypt OR CanManageAch
```

Los dos POST reciben `multipart/form-data` y devuelven directamente el artefacto binario. El endpoint oculto `POST api/nacha-security/digital-envelope/testRSA` conserva `P1.DigitalEnvelopeTest`, está marcado `IgnoreApi` y **no es consumido por esta UI**; NAV3.4J no lo modifica.

### Autorización anterior

| Capa | Estado antes de NAV3.4J |
|---|---|
| MenuItem `Cifrar y descifrar archivos` | sin `MenuItemPermission` ni `MenuItemRole` directos después de los seeders vigentes |
| SPA `/nacha-security/sobre-digital` | `CanManualEncryptEnvelope OR CanManualDecryptEnvelope OR CanManageAch OR CanReadAch` |
| acción SPA cifrar | no tenía gate de capability propio antes de invocar el servicio |
| acción SPA descifrar | no tenía gate de capability propio antes de invocar el servicio |
| API certificados | `P1.CertificatesRead` |
| API cifrar | `P1.DigitalEnvelopeEncrypt = DigitalEnvelope.Encrypt OR CanManageAch` |
| API descifrar | `P1.DigitalEnvelopeDecrypt = DigitalEnvelope.Decrypt OR CanManageAch` |

El drift principal era doble: el MenuItem no tenía frontera directa, y la ruta SPA usaba aliases de las rutas manuales más capacidades ACH genéricas, mientras los endpoints administrados de cifrado/descifrado ya tenían capabilities finas distintas. `CanReadAch` podía abrir la pantalla y llegar a intentar los POST, pero el backend correctamente rechazaba las operaciones criptográficas.

### Capacidades backend encontradas y materialización

Las capacidades canónicas ya estaban definidas en `FineGrainedPermissions`, incluidas en `AllPermissions` y usadas por las policies backend:

```text
DigitalEnvelope.Encrypt
DigitalEnvelope.Decrypt
```

No existían filas `Permission` persistidas para esas dos capabilities. NAV3.4J agrega `DigitalEnvelopeOperationPermissionSeeder`, idempotente, con IDs estables:

```text
DigitalEnvelope.Encrypt
  d951161d-06c5-4e31-a574-188e5094f59d
  Cifrar archivos mediante sobre digital

DigitalEnvelope.Decrypt
  7ccf7a90-d66f-4e63-bcb9-92275118c7e1
  Descifrar archivos mediante sobre digital
```

No se crea ninguna capability conceptual nueva. No se agregan `RolePermission`, roles, grants a `Admin`, grants a `ACH.Operator` ni grants directos a usuarios.

Capacidades/policies relacionadas evaluadas y descartadas para esta superficie:

- `Nacha.Read` / `Nacha.Generate`: pertenecen al dominio NACHA general y no protegen los endpoints consumidos por Sobre digital;
- `NachaSecurity.Read`: protege lecturas de `NachaSecurityOperationsController`, no `SobreDigitalController`;
- `NachaSecurity.ManualEncrypt`, `NachaSecurity.ManualDecrypt`, `CanManualEncryptEnvelope`, `CanManualDecryptEnvelope`: corresponden a los flujos manuales/técnicos separados;
- `CanDownloadEnvelope` / `NachaSecurity.AuthorizeDownload`: no aplican porque Sobre digital no usa un endpoint de descarga/autoriza-descarga separado;
- `CanReadAch`: sí es requisito efectivo de entrada/preparación por el GET de cámaras operativas;
- `CanManageAch`: permanece sólo como fallback legacy de las policies criptográficas existentes.

### Modelo final de autorización

Modelo final: **B — pantalla mixta**, con operaciones criptográficas separadas mediante capabilities distintas.

La entrada/preparación efectiva requiere `CanReadAch` porque la pantalla necesita `GET api/clearing-houses/operational`, cuya policy vigente es exactamente `CanReadAch`. No se reutiliza `CanManageAch` como sustituto de esa dependencia y no se inventa una nueva capability de lectura dentro de NAV3.4J.

Las operaciones sensibles permanecen separadas:

```text
Cifrar:
  DigitalEnvelope.Encrypt OR CanManageAch

Descifrar:
  DigitalEnvelope.Decrypt OR CanManageAch
```

`CanReadAch` **no** satisface ninguna de las dos policies criptográficas.

### MenuItem final

`DigitalEnvelopeMenuAuthorizationSeeder` ejecuta con `Order 110`, después de la canonicalización NAV2 y de NAV3.4I. Conserva exactamente Label, Route, ParentId, `Order 20` e `IsActive` y reconcilia el hijo a:

```text
MenuItem /nacha-security/sobre-digital
  CanReadAch
  sin MenuItemRole directo
```

El MenuItem representa la capacidad real de **entrada/preparación** de esta pantalla mixta. No incorpora `DigitalEnvelope.Encrypt OR DigitalEnvelope.Decrypt` como OR del MenuItem, porque esas capabilities por sí solas no satisfacen el GET de cámaras operativas; hacerlo produciría una opción visible que no puede inicializarse. Tampoco se usa un OR donde `CanReadAch` sustituya la autorización criptográfica: cifrar y descifrar se protegen en sus acciones y en el backend.

### SPA final

La ruta efectiva queda:

```text
/nacha-security/sobre-digital
  permissionGuard
  CanReadAch
```

El padre `/nacha-security` continúa como prefijo estructural autenticado y no sustituye los requisitos específicos del hijo.

El componente agrega gating visual y fail-closed dentro del método antes de invocar el servicio:

```text
Cifrar:
  DigitalEnvelope.Encrypt OR CanManageAch

Descifrar:
  DigitalEnvelope.Decrypt OR CanManageAch
```

Los botones quedan deshabilitados cuando falta la capability operativa y `submit()` vuelve a comprobarla antes de `SobreDigitalService.encrypt/decrypt`. Un principal con sólo `CanReadAch` puede consultar/preparar el contexto y seleccionar archivo, pero no ejecutar cifrado ni descifrado.

Los aliases `CanManualEncryptEnvelope` y `CanManualDecryptEnvelope` permanecen exclusivamente en sus rutas manuales existentes y dejan de autorizar `/sobre-digital`.

### API final

**No necesitó cambios.** Ya estaba correctamente protegida:

- `GET certificates`: `P1.CertificatesRead`;
- `POST encrypt`: `P1.DigitalEnvelopeEncrypt`;
- `POST decrypt`: `P1.DigitalEnvelopeDecrypt`.

No se debilita ninguna policy. `CanReadAch` sigue rechazado por las policies de cifrado/descifrado. No se agregan endpoints ni mutaciones.

### Certificados y llaves privadas

El navegador recibe sólo metadatos de certificados (`id`, display name, propósito, vigencia, flags operativos y thumbprint enmascarado). No recibe `SecretRef`, `EncryptedPrivateMaterial`, PKCS#12 ni llaves privadas.

Las llaves privadas necesarias para firma/descifrado se resuelven en backend mediante `ICertificateSecretResolver`. Entre los proveedores existentes, `DatabaseEncryptedCertificateSecretProvider` abre `EncryptedPrivateMaterial` mediante `ICertificatePrivateMaterialProtector`; el provider in-memory carga PKCS#12 con `X509KeyStorageFlags.EphemeralKeySet`; las referencias de secretos se enmascaran en resultados/errores. NAV3.4J no cambia algoritmos, proveedores, selección, vigencia, rotación, formato del sobre ni protección de secretos.

`/nacha-security/certificates` permanece íntegramente bajo el modelo cerrado en NAV3.4F. Sobre digital consume los certificados administrados por esa superficie pero no cambia su autorización ni comportamiento.

### Audiencia antes/después

En los grants sembrados canónicos, tanto `Admin` como `ACH.Operator` tienen `CanReadAch` y `CanManageAch`. Ninguno recibe `DigitalEnvelope.Encrypt` o `DigitalEnvelope.Decrypt` en NAV3.4J. Por tanto, ambos conservan acceso funcional y la audiencia sembrada efectiva **no se amplía ni cambia**.

Casos no sembrados relevantes:

- sólo `CanReadAch`: antes podía abrir la SPA y enviar una solicitud que el backend rechazaba; ahora puede abrir/preparar, pero la SPA falla cerrada antes del POST. No gana capacidad criptográfica;
- sólo `CanManageAch`: el backend sigue autorizando encrypt/decrypt, pero esa claim por sí sola nunca satisfizo `GET api/clearing-houses/operational`; la ruta final ya no presenta como usable una pantalla cuya preparación fallaría. Esto es una restricción fail-closed, no una ampliación;
- sólo `DigitalEnvelope.Encrypt` o sólo `DigitalEnvelope.Decrypt`: el API correspondiente sigue aceptando la capability fina; la SPA completa necesita además `CanReadAch` para su preparación actual. La futura migración de grants debe considerar explícitamente esa composición.

No queda bypass conocido por URL directa: la ruta requiere `CanReadAch`. Tampoco hay bypass de operación sensible: los métodos SPA fallan cerrados y los POST conservan sus policies backend independientes.

### Rutas manuales/técnicas

Permanecen intactas y fuera del menú como opciones independientes:

```text
/nacha-security/nacha/generate
/nacha-security/nacha/generate-encrypted
/nacha-security/digital-envelope/manual-encrypt
/nacha-security/digital-envelope/manual-decrypt
```

Sus componentes, visibilidad y autorización no se migran en NAV3.4J.

### Validaciones ejecutadas y diferidas

Ejecutadas:

- `python scripts/validate-solution-projects.py`: **PASS**, 7/7 proyectos y referencias válidas.
- `python scripts/validate-nav3-3b-service-tests.py`: **PASS**, 39 aserciones.
- `python scripts/validate-nav3-4b-transaction-create.py`: **PASS**, 72 aserciones.
- `python scripts/validate-nav3-4c-nacha-upload.py`: **PASS**, 87 aserciones.
- `python scripts/validate-nav3-4d-returns.py`: **PASS**, 100 aserciones.
- `python scripts/validate-nav3-4e-bulk-ingestion-tracking.py`: **PASS**, 153 aserciones.
- `python scripts/validate-nav3-4f-certificates.py`: **PASS**, 171 aserciones.
- `python scripts/validate-nav3-4g2-system-records.py`: **PASS**, 85 aserciones.
- `python scripts/validate-nav3-4h-navigation-menu.py`: **PASS**, 94 aserciones.
- `python scripts/validate-nav3-4i-platform-capabilities.py`: **PASS**, 73 aserciones.
- `python scripts/validate-nav3-4j-digital-envelope.py`: **PASS**, 145 aserciones focales.
- Parseo sintáctico TypeScript focal con TypeScript 5.8.3: **PASS**, 4 archivos modificados sin diagnósticos de parseo.
- Comparación de alcance contra el ZIP fuente NAV3.4I: **PASS**, 8 archivos existentes modificados + 4 archivos nuevos, sin cambios fuera del conjunto permitido de NAV3.4J/documentación/pruebas.

Diferidas:

- .NET: **NOT_RUN_ENVIRONMENT**; `dotnet` no está instalado. No se inventan build/tests runtime ni idempotencia contra base real.
- Angular: **NOT_RUN_ENVIRONMENT**; Node 22.16.0/npm 10.9.2 están disponibles pero `web/ach-interbank-ui/node_modules` no existe. No se ejecutan `ng test`/build.

### Cierre técnico y deuda residual

El gap de `/nacha-security/sobre-digital` puede considerarse cerrado a nivel de diseño/código: la entrada representa su dependencia real de lectura, los controles SPA distinguen y fallan cerrados por operación, y el backend ya tenía las policies criptográficas correctas.

Deuda deliberada futura:

1. decidir qué poblaciones deben recibir `DigitalEnvelope.Encrypt` y `DigitalEnvelope.Decrypt`;
2. otorgar esos grants explícitamente en un incremento separado;
3. conservar durante la migración la composición de lectura necesaria para usar la SPA (`CanReadAch` mientras `GET operational` la requiera);
4. validar la audiencia runtime;
5. sólo entonces evaluar retirar `CanManageAch` como fallback de las policies criptográficas;
6. si se desea independizar la lectura de Sobre digital del permiso genérico ACH, diseñar esa capability/read dependency en un incremento separado, no dentro de NAV3.4J.

No avanzar automáticamente. Recomendación exacta del siguiente incremento NAV3: **NAV3.4K — Devolución de una devolución: caracterizar y normalizar exclusivamente `/transactions/returns-ror` y sus operaciones MenuItem/SPA/API, sin tocar Cargar transacciones desde archivo, MFT/Integraciones ni migraciones de grants previas**.

## NAV-MENU-IA-001 — NAV3.4K — Devolución de una devolución — 2026-10-14

### Estado de ejecución

- Incremento ejecutado: **NAV3.4K únicamente**.
- Fuente exclusiva: `ACHInterbank-R34-NAV-IA-NAV3.4J-FULL-PROJECT.zip`.
- SHA-256 de la fuente: `c2d373884aa280fd5c7c01fc19f70be3627e0b462a2e3cd5f970dc8f298bcdfe`.
- Git: **NO USADO**.
- `NAV2`: **IMPLEMENTED_UNVERIFIED_ENVIRONMENT**. No se marca `CLOSED`; continúa pendiente la validación runtime .NET heredada.

### Propósito funcional confirmado

`/transactions/returns-ror` (`Devolución de una devolución`) es una superficie **mixta** y no replica exactamente el modelo de `/transactions/returns`.

La UI actual permite:

1. ingresar manualmente `sourceReturnTransactionId` y una nueva causal;
2. evaluar elegibilidad de una devolución de devolución;
3. ingresar manualmente uno o más `flowId` ya existentes;
4. generar un archivo interno de auditoría ROR;
5. generar un archivo NACHA-M productivo, sólo después de una evaluación elegible y confirmación explícita;
6. descargar en navegador el `Blob` devuelto por el backend.

No ofrece listado/búsqueda de devoluciones elegibles, detalle navegable, catálogo de causales, cancelación, reproceso, cambio directo de estados, creación desde UI del `ReturnOfReturnFlow`, cola de procesamiento ni transmisión externa.

### Endpoints realmente consumidos por la SPA

| Método | Endpoint | Propósito | Policy | Efecto |
|---|---|---|---|---|
| POST | `ach-returns/return-of-return/evaluate` | evaluar elegibilidad | `P0.ReturnsRead` | lectura/validación; sin persistencia |
| POST | `ach-returns/return-of-return/generate-audit-file` | generar archivo interno de auditoría | `P0.ReturnsGenerateFile` | genera artefacto y persiste auditoría de archivo |
| POST | `ach-returns/return-of-return/generate-nacha-file` | generar NACHA-M productivo | `P0.ReturnsGenerateFile` | genera artefacto y persiste auditoría de archivo |

El controller también expone:

```text
POST ach-returns/return-of-return/cenit/out
P0.ReturnsGenerateFile
```

pero el `AchReturnsApiService` usado por esta pantalla **no lo consume**. Ese flujo separado no se incorpora a NAV3.4K y queda intacto.

### Efectos persistentes y posteriores

`AchReturnOfReturnEligibilityService` usa lecturas `AsNoTracking`, consulta la devolución origen y reglas/ciclos aplicables y no ejecuta `SaveChanges`; por tanto, `evaluate` no crea flujo ROR ni altera estados.

`AchReturnOfReturnFileGenerationService` trabaja sobre `ReturnOfReturnFlow` previamente existentes. Tanto la generación de auditoría como la productiva persisten evidencia en `AchReturnOfReturnGeneratedFileAudit` y vínculos relacionados con los flows. La generación NACHA-M conserva el builder vigente; no se cambian reglas, formato ni contenido funcional.

En el servicio de generación no se encontraron `IContrapartidaDispatchJobService`, `IWscfaachSoapClient`, `IAchOutboundReturnDispatchService` ni `HttpClient`. La respuesta devuelve el archivo al navegador; **no existe transmisión externa directa, SOAP/MFT, dispatch ni enqueue desde estas dos operaciones de la UI**.

### Capacidades backend encontradas

Las capacidades ya existentes son:

```text
Returns.Read
Returns.GenerateFile
```

Policies efectivas:

```text
P0.ReturnsRead
  Returns.Read OR CanReadAch

P0.ReturnsGenerateFile
  Returns.GenerateFile OR CanManageAch
```

`CanManageAch` no satisface la policy de lectura y `CanReadAch` no satisface la policy mutable.

### Estado previo de Permission

- `Returns.GenerateFile`: ya estaba persistida mediante `ReturnsPermissionSeeder` desde NAV3.4D. NAV3.4K no la modifica.
- `Returns.Read`: existía en `FineGrainedPermissions`, `AllPermissions` y `P0.ReturnsRead`, pero no existía como fila `Permission`.

NAV3.4K materializa únicamente la capability existente `Returns.Read` mediante `ReturnsReadPermissionSeeder`:

```text
Id:          fa144b62-95c2-4540-b2f8-110516eeb837
Name:        Returns.Read
Description: Consultar devoluciones ACH
```

El seeder es idempotente. No crea `RolePermission`, roles ni grants directos a usuarios.

### Modelo final

**Modelo B — superficie mixta**.

- Entrada/evaluación: `Returns.Read OR CanReadAch`.
- Generación de archivo de auditoría: `Returns.GenerateFile OR CanManageAch`.
- Generación de archivo NACHA-M: `Returns.GenerateFile OR CanManageAch`.

La capacidad de lectura no sustituye ninguna operación mutable.

### Autorización anterior

#### MenuItem

En el estado fuente NAV3.4J, `NavigationInformationArchitectureSeeder` publicaba el hijo nuevo con:

```text
CanManageAch
```

sin `MenuItemRole` directo propio.

La estructura era y sigue siendo:

```text
Operación
└── Devolución de una devolución   Order 70
    /transactions/returns-ror
```

#### SPA

El child route exigía:

```text
CanManageAch
```

Además heredaba del padre `/transactions`:

```text
Admin OR ACH.Operator
AND
CanManageAch OR CanReadAch
```

#### API

Ya estaba correctamente separada:

```text
evaluate
  Returns.Read OR CanReadAch

generate-audit-file
  Returns.GenerateFile OR CanManageAch

generate-nacha-file
  Returns.GenerateFile OR CanManageAch
```

### Autorización final

#### MenuItem

`ReturnOfReturnMenuAuthorizationSeeder` (Order 111) conserva Label, Route, ParentId, Order, `Exact` e `IsActive`, elimina cualquier role link directo y deja exactamente:

```text
Returns.Read
OR CanReadAch
```

El padre `Operación` y los demás hijos no se modifican.

#### SPA

El child route `/transactions/returns-ror` queda con `permissionGuard` y:

```text
Returns.Read
OR CanReadAch
```

No usa nombres de rol en el hijo. El padre `/transactions` se mantiene sin refactor y continúa siendo un prerequisito adicional con su gate legacy existente.

Las acciones de generación usan:

```text
Returns.GenerateFile
OR CanManageAch
```

`AchReturnOfReturnManagementComponent` aplica dos capas SPA para cada mutación:

1. gating visual de los controles;
2. comprobación `ensureCanGenerateFiles()` dentro del método antes de llamar a `AchReturnsApiService`.

Un usuario de lectura ve un aviso de que puede evaluar elegibilidad pero no generar archivos.

#### API

**Sin cambios**. El backend ya estaba alineado con el modelo mixto mediante `P0.ReturnsRead` y `P0.ReturnsGenerateFile`.

### Audiencia antes/después

`RolePermissionConfiguration` mantiene, tanto para `Admin` como para `ACH.Operator`:

```text
CanReadAch
CanManageAch
```

NAV3.4K no asigna `Returns.Read` ni `Returns.GenerateFile` a esos roles y no crea ningún grant. Por ello la audiencia funcional **sembrada** de la pantalla no cambia: Admin y ACH.Operator conservan evaluación y generación mediante los fallbacks legacy existentes.

No se afirma equivalencia respecto de bases runtime personalizadas no disponibles en este entorno.

Capa por capa para claims aisladas:

- `CanReadAch` solo: autoriza `P0.ReturnsRead`/evaluación, pero no los endpoints de generación. En SPA sigue además sujeto al role gate heredado de `/transactions`.
- `CanManageAch` solo: autoriza los endpoints de generación, pero no `P0.ReturnsRead`; además no satisface por sí solo el nuevo child guard de entrada. En SPA sigue sujeto al padre `/transactions`.
- `Returns.Read` solo: satisface el child/read API, pero la SPA aún hereda los requisitos del padre `/transactions`.
- `Returns.GenerateFile` solo: satisface backend mutable, pero no el child/read entry ni el padre SPA.

### Relación con `/transactions/returns` / NAV3.4D

`/transactions/returns` permanece intacto funcionalmente:

- entrada SPA: `CanReadAch`;
- acción de generación: `Returns.GenerateFile OR CanManageAch`;
- API: `P0.ReturnsRead` / `P0.ReturnsGenerateFile`;
- MenuItem de devoluciones: sin cambio por NAV3.4K.

La única actualización sobre artefactos históricos NAV3.4D fue retirar una expectativa de regresión que fijaba el antiguo guard `CanManageAch` de `returns-ror`, porque ese guard quedó legítimamente supersedido por NAV3.4K. No se cambió código de producto de `/transactions/returns`.

### Bypass y separación de responsabilidades

No queda bypass conocido dentro del alcance inspeccionado:

- URL directa: evalúa el child `permissionGuard` y también conserva los requisitos heredados del padre;
- botones de generación: no se muestran sin capacidad mutable;
- métodos `generateAudit()`/`generateNacha()`: fallan cerrados antes de invocar servicio;
- API: repite la frontera con `P0.ReturnsGenerateFile` y no acepta `CanReadAch` para generación.

El role gate legacy del padre `/transactions` es una restricción adicional, no un bypass. Su normalización global está fuera de NAV3.4K.

### Archivos de seguridad/producto cambiados

- `src/Cfa.ACHInterbank.Persistence/Security/Seeders/ReturnsReadPermissionSeeder.cs` — nuevo.
- `src/Cfa.ACHInterbank.Persistence/Security/Seeders/ReturnOfReturnMenuAuthorizationSeeder.cs` — nuevo.
- `web/ach-interbank-ui/src/app/features/transactions/transactions-routing.module.ts` — child read gate.
- `web/ach-interbank-ui/src/app/features/transactions/components/ach-return-of-return-management/ach-return-of-return-management.component.ts` — gating/fail-closed mutable.
- `...component.html` — gating visual/aviso read-only.

Las specs/validadores focales se ajustan para el nuevo contrato. `GetMenuForCurrentUserHandler` permanece intacto.

### Validación

Ejecutado en este entorno:

- `scripts/validate-nav3-4k-return-of-return.py`: **PASS — 86 aserciones**.
- `scripts/validate-nav3-4d-returns.py`: **PASS — 100 aserciones** después de retirar únicamente la expectativa histórica supersedida de ROR.
- `scripts/validate-solution-projects.py`: **PASS**.
- `scripts/validate-nav3-3b-service-tests.py`: **PASS — 39**.
- NAV3.4B: **PASS — 72**.
- NAV3.4C: **PASS — 87**.
- NAV3.4E: **PASS — 153**.
- NAV3.4F: **PASS — 171**.
- NAV3.4G2: **PASS — 85**.
- NAV3.4H: **PASS — 94**.
- NAV3.4I: **PASS — 73**.
- NAV3.4J: **PASS — 145**.
- Parseo sintáctico TypeScript focal de rutas/componente/spec: **PASS con TypeScript 5.8.3**.

Pruebas runtime:

- `.NET`: **NOT_RUN_ENVIRONMENT** — `dotnet` no está instalado.
- Angular: **NOT_RUN_ENVIRONMENT** — `node_modules` no existe en `web/ach-interbank-ui`.

No se inventan resultados runtime.

### Gap residual y siguiente incremento

El gap de `/transactions/returns-ror` puede considerarse cerrado a nivel de diseño/código, sujeto a la deuda runtime general del proyecto.

Deuda explícita posterior:

1. decidir quién debe recibir `Returns.Read`;
2. decidir quién debe recibir `Returns.GenerateFile` como capability fina;
3. migrar grants explícitamente;
4. validar audiencia runtime;
5. sólo entonces evaluar retirar `CanReadAch`/`CanManageAch` de las policies;
6. tratar en otro incremento, si se decide, la restricción heredada global del padre `/transactions`.

No avanzar automáticamente. Recomendación exacta del siguiente incremento:

**NAV3.4L — Cargar transacciones desde archivo**, limitado a `/transactions/bulk-ingestion/upload` y a sus operaciones reales MenuItem/SPA/API. Mantener MFT/Integraciones y las migraciones de grants previas fuera de ese incremento.

## NAV-MENU-IA-001 — NAV3.4L — Cargar transacciones desde archivo — 2026-10-14

### Fuente y alcance

NAV3.4L parte exclusivamente de `ACHInterbank-R34-NAV-IA-NAV3.4K-FULL-PROJECT.zip` (SHA-256 `7e0581bac1e0b263b522d93a69efdcddd396548aaeb4a8bdb2fa95b281f536d7`) y no usa Git. El incremento se limita a `/transactions/bulk-ingestion/upload`; `/transactions/bulk-ingestion/tracking`, `/transactions/bulk-ingestion/:batchId`, `/transactions/nacha-upload`, `/transactions/bulk-create`, MFT/Integraciones y las migraciones de grants anteriores permanecen fuera de alcance.

`NAV2` permanece `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`. No se marca `CLOSED` porque las pruebas runtime .NET heredadas siguen pendientes.

### Comportamiento funcional confirmado

`Cargar transacciones desde archivo` es una superficie **completamente mutable (modelo A)** desde el punto de vista de autorización. Antes de enviar, el navegador sólo realiza preparación local:

- selección de un archivo;
- filtro de extensiones anunciado por la UI;
- validación local de extensión/tipo y tamaño máximo de 20 MB;
- captura opcional de `batchReference` y `clientRequestId`;
- resumen visual del archivo seleccionado.

La pantalla **no consume un endpoint de lectura/preview/validación previo**. Su única llamada backend de negocio es:

```text
POST api/transactions/bulk-ingestion/upload
  P1.BulkIngestionUpload
  BulkIngestion.Upload OR CanManageAch
```

No ejecuta desde esta pantalla `GET batch`, `GET items`, `GET summary`, retry, cancel ni descarga de errores; esas operaciones pertenecen a Seguimiento de cargas/detalle y conservan NAV3.4E.

### Efectos del upload y procesamiento posterior

El POST recibe `multipart/form-data`, limita el request a 20 MB y delega en `AchBulkFileIngestionService.UploadAndParseAsync`. El flujo vigente:

1. calcula SHA-256 del archivo y usa `ClientRequestId + hash` para idempotencia cuando el client request está informado;
2. selecciona parser y parsea JSON/CSV/Excel;
3. valida estructuralmente cada fila;
4. persiste un `BulkIngestionBatch` con metadatos del archivo (`FileName`, `ContentType`, `FileHash`, referencia, actor y timestamps) y contadores/estado;
5. persiste `BulkIngestionItem` por fila con payload crudo serializado, payload normalizado, referencia, estado y mensaje de validación;
6. **no persiste el binario original del archivo** como artefacto; conserva metadatos/hash y la representación parseada por ítem;
7. si existe al menos un ítem válido, marca el batch `Queued`, crea `BulkIngestionAttempt` inicial y llama `IBulkIngestionWorkDispatcher.DispatchProcessingAsync`;
8. el dispatcher actual usa `IAchBulkJobScheduler`, implementado con Quartz; `AchBulkJobScheduler` agenda `ProcessBulkIngestionBatchJob` con `StartNow()`;
9. el job llama `AchBulkBatchProcessingService.ProcessBatchAsync`, que procesa los ítems `Ready` mediante `IAchBulkTransactionService.RegisterBulkAsync` y persiste sus resultados/estados;
10. el servicio masivo persiste `AchTransaction` mediante `_transactionPersister.PersistAsync`; según la clasificación vigente puede preparar `CenitCycleQueue` y asegurar un registro pendiente de contrapartida mediante `EnsurePendingDispatchAsync`.

No se encontró transporte directo SOAP, MFT, SFTP ni `HttpClient` en el request de upload ni en el dispatcher/scheduler/job/processor inspeccionado. El POST inicia **procesamiento asíncrono interno** y puede preparar colas/dispatch persistentes posteriores; no transmite directamente a terceros.

### Gap funcional observado, fuera de autorización

La UI anuncia `.json,.csv,.xlsx,.xls`, mientras `AchBulkFileIngestionService.ResolveFileType` reconoce por extensión `.json`, `.csv`, `.xlsx` y `.xlsm` (además de heurísticas por content type). Este drift de formatos es previo/funcional y **no se corrige en NAV3.4L** porque el incremento está limitado a autorización.

### Capability y policy canónicas

La capability fina ya existía antes de NAV3.4L:

```text
BulkIngestion.Upload
```

Está declarada en `FineGrainedPermissions.BulkIngestion.Upload`, incluida en `FineGrainedPermissions.AllPermissions` y es consumida por:

```text
P1.BulkIngestionUpload
  BulkIngestion.Upload OR CanManageAch
```

`CanReadAch` y `BulkIngestion.Read` **no** satisfacen esa policy. No existe una fase legítima de lectura backend propia de esta pantalla que justifique usar `BulkIngestion.Read` como frontera de entrada.

En NAV3.4L `BulkIngestionUploadPermissionSeeder` materializa idempotentemente la capability existente en `Permission`:

```text
BulkIngestion.Upload
  e8194742-09ca-4148-9567-75bb24923f94
  Cargar transacciones desde archivo
```

No se crean `RolePermission`, roles ni grants directos a usuarios. Admin y ACH.Operator no reciben la capability fina.

### Autorización anterior

En el estado fuente NAV3.4K:

```text
MenuItem /transactions/bulk-ingestion/upload
  MenuItemRole: Admin OR ACH.Operator
  MenuItemPermission: CanManageAch
  evaluación efectiva directa: (Admin OR ACH.Operator) AND CanManageAch

SPA child /transactions/bulk-ingestion/upload
  CanManageAch

SPA parent /transactions (heredado)
  Admin OR ACH.Operator
  AND CanManageAch OR CanReadAch

API POST upload
  P1.BulkIngestionUpload
  BulkIngestion.Upload OR CanManageAch
```

El backend ya expresaba la capability semántica fina, pero menú y child route sólo representaban el fallback legacy `CanManageAch`.

### Autorización final

#### MenuItem

`BulkIngestionUploadMenuAuthorizationSeeder` (Order 112) conserva Label, Route, ParentId, Order 30, `Exact`, `IsActive` e icono y deja exactamente estas alternativas de permiso:

```text
BulkIngestion.Upload
OR CanManageAch
```

Los `MenuItemRole` existentes `Admin`/`ACH.Operator` **se conservan deliberadamente**. Motivo: el padre SPA `/transactions` continúa bajo el mismo prerequisito legacy de roles y NAV3.4L tiene prohibido refactorizarlo globalmente. Retirar sólo el role link del hijo haría que el menú representara una audiencia que la SPA completa aún no puede atravesar y, con el algoritmo de reconstrucción vigente, podría crear una ruta huérfana para futuros grants finos. Por tanto, la frontera efectiva de menú en esta fase es fail-closed:

```text
(Admin OR ACH.Operator)
AND
(BulkIngestion.Upload OR CanManageAch)
```

No se crean nuevos roles ni asociaciones de rol; se preservan las existentes.

#### SPA

El child route queda con `permissionGuard` y:

```text
BulkIngestion.Upload OR CanManageAch
```

No contiene checks por nombre de rol. El componente aplica además defensa fail-closed en la acción:

- gating visual: el botón `Enviar archivo` sólo se muestra con `BulkIngestion.Upload OR CanManageAch`;
- gating en método: `upload()` llama `ensureCanUpload()` antes de invocar el servicio y aborta con notificación si no hay capacidad.

El padre `/transactions` permanece intacto con su requisito heredado:

```text
Admin OR ACH.Operator
AND
CanManageAch OR CanReadAch
```

Ese padre es una restricción adicional del frontend; no sustituye la capability específica del hijo.

#### API

**Sin cambios**. `BulkIngestionController.Upload` ya estaba correctamente protegido por `P1.BulkIngestionUpload = BulkIngestion.Upload OR CanManageAch`. No se debilitan las policies `BulkIngestion.Read`, `Retry` ni `Cancel`.

### Audiencia y grants

`RolePermissionConfiguration` mantiene en los seeds canónicos:

```text
Admin        -> CanManageAch + CanReadAch + ...
ACH.Operator -> CanManageAch + CanReadAch + ...
```

No existe grant sembrado de `BulkIngestion.Upload` y NAV3.4L no agrega ninguno. Por ello la audiencia **sembrada** antes/después no cambia: Admin y ACH.Operator siguen entrando y subiendo mediante `CanManageAch`.

Para claims aisladas:

- `CanReadAch` solo: no satisface el child route ni `P1.BulkIngestionUpload`; no puede subir.
- `CanManageAch` solo: satisface el child y el API de upload, pero la SPA completa conserva además el role gate heredado del padre `/transactions`.
- `BulkIngestion.Read` solo: sirve para tracking/read según NAV3.4E; no permite upload.
- `BulkIngestion.Upload` solo: satisface la policy backend y el child guard, pero en la SPA completa todavía necesita satisfacer el parent legacy y, en el menú, los role links conservados. Esto es deuda de migración/parent authorization, no un bypass.

### Regresión NAV3.4E / NAV3.4C

- `/transactions/bulk-ingestion/tracking` y `/transactions/bulk-ingestion/:batchId` conservan `BulkIngestion.Read OR CanReadAch`.
- Retry conserva `BulkIngestion.Retry OR CanManageAch`.
- Cancel conserva `BulkIngestion.Cancel OR CanManageAch`.
- `/transactions/nacha-upload` conserva `Nacha.Upload OR CanManageAch` bajo NAV3.4C.
- `/transactions/bulk-create` permanece fuera del menú y no se modifica.
- `GetMenuForCurrentUserHandler` permanece intacto.

Las únicas expectativas históricas NAV3.4E ajustadas son las que fijaban literalmente el antiguo child guard de upload; se mantiene su garantía real de que upload no usa `BulkIngestion.Read` ni `CanReadAch`.

### Validación

Ejecutado en este entorno:

- `scripts/validate-nav3-4l-bulk-ingestion-upload.py`: **PASS — 124 aserciones**.
- `scripts/validate-solution-projects.py`: **PASS**.
- `scripts/validate-nav3-3b-service-tests.py`: **PASS — 39**.
- NAV3.4B: **PASS — 72**.
- NAV3.4C: **PASS — 87**.
- NAV3.4D: **PASS — 100**.
- NAV3.4E: **PASS — 154** después de actualizar únicamente la expectativa histórica que fijaba el child guard pre-L de upload.
- NAV3.4F: **PASS — 171**.
- NAV3.4G2: **PASS — 85**.
- NAV3.4H: **PASS — 94**.
- NAV3.4I: **PASS — 73**.
- NAV3.4J: **PASS — 145**.
- NAV3.4K: **PASS — 86**.
- Parseo sintáctico focal de 4 archivos TypeScript modificados: **PASS con TypeScript 5.8.3**.

Pruebas runtime:

- `.NET`: **NOT_RUN_ENVIRONMENT** — `dotnet` no está instalado/disponible.
- Angular: **NOT_RUN_ENVIRONMENT** — Node 22.16.0, npm 10.9.2 y TypeScript 5.8.3 están disponibles, pero `web/ach-interbank-ui/node_modules` no existe.

No se inventan resultados runtime.

### Gap residual y siguiente incremento

El gap de autorización de `/transactions/bulk-ingestion/upload` puede considerarse cerrado a nivel de diseño/código cuando pasen las validaciones estáticas focales. Queda deuda deliberada:

1. decidir qué población debe recibir `BulkIngestion.Upload`;
2. migrar grants explícitos sin ampliar audiencia;
3. validar audiencia runtime;
4. sólo entonces evaluar retirar `CanManageAch` como fallback de `P1.BulkIngestionUpload`;
5. tratar en incremento separado la restricción global heredada del padre `/transactions` y la estrategia de `MenuItemRole` cuando existan grants finos;
6. corregir, en un gap funcional separado, el drift `.xls`/`.xlsm` si se prioriza.

No avanzar automáticamente. Recomendación exacta del siguiente incremento NAV3: **NAV3.4M — MFT/Integraciones**, limitado a caracterizar y normalizar exclusivamente las superficies de Conexiones/MFT que aún presentan drift de MenuItem/SPA/API, sin mezclar migraciones de grants, el padre global `/transactions` ni gaps ya cerrados.


## NAV-MENU-IA-001 — NAV3.4M — Configuración del intercambio de archivos — 2026-10-14

### Fuente, alcance y estado

NAV3.4M parte **exclusivamente** de `ACHInterbank-R34-NAV-IA-NAV3.4L-FULL-PROJECT.zip` (SHA-256 `41a820b3ede669d5de39ac57df42df8aec201e657de353468aabf36a398704d3`) y no usa Git. El incremento se limita a `/administracion/mft-ach` → `Configuración del intercambio de archivos`.

Permanecen fuera de alcance y sin cambios de autorización:

- `/integraciones/contrato`;
- `/integraciones/relacion-informacion`;
- `/integraciones/conexion`.

`NAV2` permanece **`IMPLEMENTED_UNVERIFIED_ENVIRONMENT`**. No se marca `CLOSED`: las pruebas runtime .NET continúan pendientes.

### Comportamiento funcional real de `/administracion/mft-ach`

La superficie es una **administración mixta de configuración MFT y horarios operativos**, no una pantalla de transmisión de archivos. Al cargar ejecuta dos lecturas en paralelo:

```text
GET api/ach-colombia/file-exchange/administration
GET api/ach-colombia/operational-schedules/administration
```

La primera consulta expone el perfil MFT configurable y la segunda combina la política de ciclos vigente con la programación operativa persistida.

La UI permite modificar:

- proveedor/tipo de conexión (`ManagedFolder` o `SftpEndpoint`) y protocolo derivado;
- habilitación del perfil de integración;
- endpoint/hostname, puerto y usuario/principal técnico para SFTP;
- fingerprint de host SFTP y timeout;
- rutas de salida, entrada y archivo;
- flags operativos de envío/recepción automático y manual, reintentos y retención presentes en el modelo;
- credencial tipo password mediante una operación write-only;
- habilitación maestra de outbound automático;
- por ciclo: hora automática, hora límite y activación de la programación.

No ofrece desde este componente:

- eliminación de la configuración MFT;
- envío de archivo de prueba;
- ejecución manual outbound/inbound;
- upload/download de archivos;
- retry/reprocess/archive/retire de transferencias;
- ejecución directa de una transferencia productiva.

La pantalla sí ofrece `Probar conexión`. Para `ManagedFolder`, el backend comprueba existencia y enumera como máximo una entrada de cada directorio configurado. Para SFTP, descifra la credencial **sólo en backend**, abre una sesión autenticada y hace `List` sobre las rutas configuradas. La prueba ocurre dentro del request, no sube ni descarga archivos y no dispara un job de transferencia.

Guardar horarios persiste la programación y llama `IAchColombiaOperationalScheduleSynchronizer.SynchronizeOutboundAsync`; `SchedulerSyncService` resincroniza la tarea Quartz outbound. Ese request no transfiere archivos directamente, aunque la programación modificada puede gobernar ejecuciones automáticas futuras.

Un matiz de lectura: `GetAdministrationAsync` usa `GetOrCreateConfigurationEntityAsync`. Si todavía no existe la fila de configuración MFT, el GET crea y persiste los valores por defecto. No abre conexiones externas.

### Datos y secretos

La configuración persistida `AchManagedFileTransferConfiguration` incluye, entre otros: `ProfileEnabled`, `Provider`, `Protocol`, `Endpoint`, `Port`, `Principal`, `OutboundLocation`, `InboundLocation`, `ArchiveLocation`, `CredentialType`, `ProtectedCredential`, `CredentialUpdatedAtUtc`, `SftpHostKeyFingerprint`, `SftpTimeoutSeconds` y flags de automatización.

El DTO de administración entregado al navegador expone hostname/endpoint, puerto, protocolo/proveedor, rutas, principal/username, fingerprint, timeout, flags, `CredentialConfigured`, `CredentialType`, `CredentialUpdatedAtUtc` y token de concurrencia. **No expone `ProtectedCredential`, password, token, private key ni otro material secreto.**

La credencial nueva entra por `PUT .../administration/credential`; `AchColombiaManagedMftCredentialComponent` la cifra mediante `encryption.Encrypt(value.Secret)` antes de persistir. Las respuestas vuelven a `MapAdministration`, que sólo proyecta el booleano `CredentialConfigured` y metadata de credencial. La prueba SFTP descifra `ProtectedCredential` únicamente dentro del backend y no la retorna. NAV3.4M no cambia cifrado, secret store, masking, rotación ni redacción de logs.

No se encontró manejo de certificados cliente, private keys ni tokens dentro de esta pantalla. El fingerprint de host SFTP se trata como configuración verificable, no como secreto.

### Endpoints realmente consumidos y autorización anterior/final

| Endpoint | Propósito | Efecto | Policy anterior | Policy final NAV3.4M |
|---|---|---|---|---|
| `GET api/ach-colombia/file-exchange/administration` | consultar perfil MFT | lectura; puede bootstrapear fila default | `CanReadAch` | `P1.ConfigRead` = `Config.Read OR CanReadAch` |
| `PUT api/ach-colombia/file-exchange/administration` | editar perfil/configuración | persistencia | `CanManageAch` | `P1.ConfigManage` = `Config.Manage OR CanManageAch` |
| `PUT api/ach-colombia/file-exchange/administration/credential` | reemplazar password protegido | cifra y persiste secreto | `CanManageAch` | `P1.ConfigManage` |
| `POST api/ach-colombia/file-exchange/administration/test-connection` | probar acceso configurado | I/O de filesystem o conexión/listado SFTP dentro del request; sin transferencia | `CanManageAch` | `P1.ConfigManage` |
| `GET api/ach-colombia/operational-schedules/administration` | consultar ciclos y programación | lectura | `CanReadAch` | `P1.ConfigRead` |
| `POST api/ach-colombia/operational-schedules` | crear programación | persistencia + resincronización Quartz | `CanManageAch` | `P1.ConfigManage` |
| `PUT api/ach-colombia/operational-schedules/{id}` | actualizar programación | persistencia + resincronización Quartz | `CanManageAch` | `P1.ConfigManage` |

`PATCH api/ach-colombia/operational-schedules/{id}/deactivate` no es consumido por esta pantalla y permanece fuera del incremento con su policy previa. Del mismo modo, los endpoints de ejecución/transferencia del controller de file exchange no son consumidos por `/administracion/mft-ach` y permanecen intactos.

### Capacidades canónicas y materialización

Ya existían en código y policies las capacidades finas semánticamente adecuadas:

```text
Config.Read
Config.Manage

P1.ConfigRead   = Config.Read   OR CanReadAch
P1.ConfigManage = Config.Manage OR CanManageAch
```

No existe una capability MFT/FileExchange/FileTransfer específica adecuada para esta administración ni una capability separada preexistente para credential/test-connection. Las capabilities `Integrations.ManageMappings`, `Integrations.Validate`, `Integrations.Publish` y `Integrations.Compare` pertenecen a otros ciclos funcionales y no se reutilizan artificialmente.

`Config.Read` y `Config.Manage` estaban declaradas en `FineGrainedPermissions`, incluidas en `AllPermissions` y usadas por policies, pero no estaban materializadas en el catálogo `Permission`. NAV3.4M agrega `ConfigurationPermissionSeeder` idempotente:

```text
Config.Read
  15d0e235-eb36-4f7d-a922-d935dc1af7e2
  Consultar configuración de la plataforma

Config.Manage
  6e9d334b-d77a-4576-814f-8e1f987a421f
  Administrar configuración de la plataforma
```

No se crean roles, `RolePermission`, grants a Admin/ACH.Operator ni permisos directos a usuarios.

### Modelo de autorización final

Aplica **modelo B — superficie mixta**:

```text
Entrada / consulta
  Config.Read OR CanReadAch

Edición de configuración
Administración write-only de credencial
Creación/edición de horarios
Prueba de conectividad
  Config.Manage OR CanManageAch
```

No aplica un tercer nivel de transmisión porque esta pantalla no ejecuta transferencias ni envía archivos de prueba. `test-connection` sí produce un efecto externo sensible, pero sólo autentica/lista para verificar conectividad y queda bajo la misma capacidad administrativa que el backend ya usa para configuración mutable.

Por claims aisladas:

- `CanReadAch` solo: satisface lectura/API y ruta directa SPA; no puede guardar configuración, cambiar credencial, modificar horarios ni probar conexión.
- `CanManageAch` solo: satisface las APIs mutables/test, pero **no** `P1.ConfigRead` ni la ruta SPA de entrada; requiere además una capacidad de lectura para operar la pantalla completa.
- `Config.Read` solo: equivalente fino de lectura; no habilita mutaciones/test.
- `Config.Manage` solo: equivalente fino de gestión API; no sustituye la capacidad de lectura para entrar/cargar la SPA.

### MenuItem: autorización anterior, final y blocker estructural

La identidad canónica permanece sin cambios:

```text
Administración de parámetros
└── Conexiones
    └── Configuración del intercambio de archivos
        /administracion/mft-ach
        order 40
```

Antes de NAV3.4M el MenuItem MFT conservaba del seeder histórico:

```text
MenuItemRole       Admin
MenuItemPermission CanReadAch
```

Por la semántica de `GetMenuForCurrentUserHandler`, eso equivale directamente a `Admin AND CanReadAch`.

NAV3.4M agrega `MftAdministrationMenuAuthorizationSeeder` (Order 113) y deja las alternativas de permiso directas en:

```text
Config.Read OR CanReadAch
```

**No cambia `MenuItemRole`: continúa `Admin`.** Esto es deliberado y fail-closed por un blocker de padre ya existente que NAV3.4M no puede ampliar:

- el padre canónico `/integraciones` (`Conexiones`) conserva `MenuItemRole Admin` y `MenuItemPermission CanManageUsers`;
- `GetMenuForCurrentUserHandler` evalúa Role **AND** Permission por item y promueve como root un hijo visible cuyo padre fue filtrado;
- `NavigationService` Angular todavía intercepta `/integraciones`, lo renombra `Centro de integración CFA` y fuerza `children: []`, por lo que descarta los hijos canónicos al renderizar ese padre.

Retirar sólo el role del hijo en NAV3.4M habría permitido orphaning/promoción a root para futuros principals con `Config.Read` que no pasen el padre, sin corregir la navegación efectiva. El incremento documenta el blocker y no modifica el padre, `NavigationService` ni `GetMenuForCurrentUserHandler`.

Consecuencia: el MenuItem directo queda más semántico en permisos, pero la **navegación renderizada bajo `Conexiones` aún no representa correctamente la superficie**. Por ello el gap completo Menu/SPA/API no puede considerarse cerrado todavía.

### SPA anterior y final

Antes:

```text
/administracion/mft-ach
  roleGuard + permissionGuard
  (Admin OR ACH.Operator) AND CanReadAch
```

Final:

```text
/administracion/mft-ach
  permissionGuard
  Config.Read OR CanReadAch
```

No hay check de nombre de rol en el componente. Las acciones mutables usan:

```text
Config.Manage OR CanManageAch
```

El template aplica gating visual/disabled y `saveConnection()`, `testConnection()` y `saveSchedules()` vuelven a verificar `canManage` dentro del método antes de invocar servicios. La spec focal prueba el estado read-only y confirma que llamadas directas a esos métodos no alcanzan las APIs mutables sin capacidad.

La ruta MFT es top-level en `app-routing.module.ts`; no hereda el guard Angular de `/integraciones`. El gate del padre sólo afecta el menú backend/renderizado, no el acceso directo a esta SPA.

### Backend y cambios requeridos

El backend **sí requirió un cambio focal de policy**, porque los endpoints consumidos todavía usaban directamente `CanReadAch`/`CanManageAch` aunque las policies finas `P1.ConfigRead`/`P1.ConfigManage` ya existían. Se cambiaron únicamente los siete endpoints consumidos por esta superficie. No se debilitaron policies y se conservaron los fallbacks legacy, por lo que la audiencia desplegada con claims legacy no pierde acceso.

Los endpoints de transferencia del mismo controller permanecen fuera de alcance y no se tocaron.

### Audiencia efectiva antes/después

NAV3.4M no agrega grants. Los roles sembrados que ya disponen de `CanReadAch`/`CanManageAch` conservan su acceso API/SPA por fallback; por tanto, **la audiencia sembrada no cambia**.

Las nuevas capabilities materializadas no se asignan a nadie. En una futura asignación explícita, `Config.Read` puede habilitar lectura directa de la SPA/API sin otorgar gestión; `Config.Manage` habilita gestión API pero no sustituye lectura. La audiencia efectiva del menú sigue limitada además por el `MenuItemRole Admin` y por el problema estructural del padre/renderizador descrito arriba.

### Relación con las otras superficies de `Conexiones`

`/integraciones/contrato`, `/integraciones/relacion-informacion` y `/integraciones/conexion` sólo se inspeccionaron lo mínimo para confirmar la dependencia del padre. Sus rutas, guards, MenuItems, controllers/policies y comportamiento no se modificaron. `NavigationInformationArchitectureSeeder` conserva los tres hijos canónicos y NAV3.4M no cambia su autorización.

### Pruebas y validación

NAV3.4M agrega:

- `NavigationMenuNav34MMftAdministrationAuthorizationTests.cs`, con caracterización de capabilities, menú, SPA, APIs, secretos, efectos externos y regresión;
- `scripts/validate-nav3-4m-mft-administration.py`, validación estática focal.

La validación focal pasa con **192 aserciones** antes del empaquetado. Las validaciones de regresión del proyecto se ejecutan nuevamente al cierre del incremento y sus resultados se registran en el handoff final.

Pruebas runtime permanecen condicionadas por el entorno:

- `.NET`: registrar **`NOT_RUN_ENVIRONMENT`** si `dotnet` no está instalado;
- Angular: registrar **`NOT_RUN_ENVIRONMENT`** si `web/ach-interbank-ui/node_modules` no existe.

No se inventan resultados runtime.

### Gap residual y recomendación

El alineamiento de policies API, ruta SPA y acciones mutables de `/administracion/mft-ach` queda implementado. La capability fina existente de configuración queda materializada sin grants y el modelo de secretos se conserva.

Sin embargo, el gap **no puede considerarse completamente cerrado** porque el MenuItem continúa condicionado por `MenuItemRole Admin` y, más importante, el padre `/integraciones`/`NavigationService` impide renderizar correctamente los hijos canónicos de `Conexiones`. Corregirlo dentro de NAV3.4M violaría el límite explícito sobre el padre y las otras tres superficies.

Recomendación exacta del siguiente incremento, sin ejecutarlo automáticamente:

**NAV3.4N — prerequisito estructural `Conexiones`: normalizar únicamente el padre `/integraciones` y su adaptación en `NavigationService` para preservar la jerarquía NAV2, evitar orphaning y permitir que los hijos canónicos se rendericen; no cambiar todavía la autorización propia de Contrato del servicio, Relación de información ni Conexión entidad participante.**

### Validación final NAV3.4M

Ejecutado sobre el estado final del incremento:

- `scripts/validate-nav3-4m-mft-administration.py`: **PASS — 192 aserciones**.
- `scripts/validate-solution-projects.py`: **PASS** — 7/7 proyectos y referencias válidas.
- `scripts/validate-nav3-3b-service-tests.py`: **PASS — 39 aserciones** (Herramientas de prueba/regresión de servicio).
- NAV3.4B: **PASS — 72**.
- NAV3.4C: **PASS — 87**.
- NAV3.4D: **PASS — 100**.
- NAV3.4E: **PASS — 154**.
- NAV3.4F: **PASS — 171**.
- NAV3.4G2: **PASS — 85**.
- NAV3.4H: **PASS — 94**.
- NAV3.4I: **PASS — 73**.
- NAV3.4J: **PASS — 145**.
- NAV3.4K: **PASS — 86**.
- NAV3.4L: **PASS — 124**.
- `python -m py_compile scripts/validate-nav3-4m-mft-administration.py`: **PASS**.
- Parseo sintáctico focal de `app-routing.module.ts`, `ach-colombia-mft-administration.component.ts` y su spec: **PASS — 3 archivos con TypeScript 5.8.3**.

Runtime:

- `.NET`: **`NOT_RUN_ENVIRONMENT`** — `dotnet` no está instalado en el entorno.
- Angular: **`NOT_RUN_ENVIRONMENT`** — Node/NPM existen, pero `web/ach-interbank-ui/node_modules` no está presente; no se inventan resultados de `ng test`/build.

El conjunto de archivos modificado se verificó por comparación directa contra el ZIP NAV3.4L, sin Git. Sólo cambian los archivos focales NAV3.4M y la documentación requerida; las tres rutas excluidas de `Conexiones` no tienen cambios propios.

### Bypass conocido

No se encontró un bypass que permita a una capability de lectura ejecutar escritura, prueba de conexión o transferencia. El backend repite `P1.ConfigManage` en todas las mutaciones consumidas y en `test-connection`.

Sí permanece una **inconsistencia de navegación conocida**: un principal con `Config.Read`/`CanReadAch` puede autorizar la ruta SPA directa y el GET de administración aunque el MenuItem no se le muestre por el `MenuItemRole Admin`/parent legacy. Esto no evade la autorización backend; es el mismo blocker estructural de `Conexiones` que impide cerrar el gap de menú en NAV3.4M.

## NAV-MENU-IA-001 — NAV3.4N — prerequisito estructural Conexiones — 2026-10-15

### Fuente, alcance y estado

NAV3.4N parte **exclusivamente** de `ACHInterbank-R34-NAV-IA-NAV3.4M-FULL-PROJECT.zip` (SHA-256 `9f39f693c21b24ca863dc07d920e3db8b3f7af9e35767a7f65a5b9f6c38ebddc`) y no usa Git. El incremento corrige únicamente el parent estructural `Conexiones`, el tratamiento especial de `/integraciones` en `NavigationService` y el `MenuItemRole Admin` residual de `/administracion/mft-ach` que NAV3.4M había conservado por ese blocker.

`NAV2` permanece **`IMPLEMENTED_UNVERIFIED_ENVIRONMENT`**. No se marca `CLOSED`: `dotnet` continúa ausente y las pruebas runtime .NET siguen pendientes.

Quedan explícitamente fuera de normalización propia:

```text
/integraciones/contrato
/integraciones/relacion-informacion
/integraciones/conexion
```

No se modifican sus MenuItemRole/MenuItemPermission, guards SPA, componentes, servicios ni policies API.

### Estado anterior exacto del padre `/integraciones`

El MenuItem canónico es el row histórico `MenuItemConfiguration.IntegrationsId = 29`. `NavigationInformationArchitectureSeeder` lo reconcilia sin cambiar su ID a:

```text
ID        29
Route     /integraciones
Label     Conexiones
Parent    /administracion-parametros  (Administración de parámetros)
Order     80
IsActive  true
Exact     false
```

Antes de NAV3.4N conservaba la autorización legacy sembrada originalmente:

```text
MenuItemRole       Admin
MenuItemPermission CanManageUsers
=> Admin AND CanManageUsers
```

El route `/integraciones` **sí tiene una pantalla SPA funcional**. `app-routing.module.ts` carga `INTEGRATIONS_ROUTES` y su child `path: ''` carga `IntegrationWorkspaceComponent`. El route parent conserva, antes y después de NAV3.4N:

```text
(Admin OR ACH.Operator)
AND
(CanReadAch OR CanManageAch)
```

NAV3.4N no cambia ese guard, ni títulos/breadcrumbs. Además, `MainLayout` ya trata cualquier MenuItem con children como un botón de expansión (`toggleSubmenu`) y no como un enlace; por ello el MenuItem `Conexiones` puede actuar como contenedor estructural aun cuando `/integraciones` continúe existiendo como pantalla funcional accesible por URL.

### Cuatro relaciones padre/hijo y autorización propia observada

La jerarquía canónica permanece exactamente:

```text
Administración de parámetros
└── Conexiones                       /integraciones                order 80
    ├── Contrato del servicio        /integraciones/contrato       order 10
    ├── Relación de información      /integraciones/relacion-informacion order 20
    ├── Conexión entidad participante /integraciones/conexion      order 30
    └── Configuración del intercambio de archivos /administracion/mft-ach order 40
```

Los tres primeros hijos fueron creados por NAV2.2 sin autorización propia previa y `MirrorAuthorizationIfEmptyAsync` copió del parent legacy `Admin + CanManageUsers`. NAV3.4N **no cambia esas asociaciones**.

| Hijo | MenuItem antes/final NAV3.4N | SPA actual | API relevante actual | Cambio NAV3.4N |
|---|---|---|---|---|
| `/integraciones/contrato` | `Admin AND CanManageUsers` | hereda parent Angular: `(Admin OR ACH.Operator) AND (CanReadAch OR CanManageAch)` | consultas de métodos/contratos/compare: `CanReadAch`; import/clone/activate/rollback/presentation/analyze/preview: `CanManageAch` | ninguno |
| `/integraciones/relacion-informacion` | `Admin AND CanManageUsers` | mismo parent Angular; no agrega child guard | catálogo/mapping/workspace/history: `CanReadAch`; create/update/rules/validate/preview/publish/clone/compare: `CanManageAch` | ninguno |
| `/integraciones/conexion` | `Admin AND CanManageUsers` | mismo parent Angular; no agrega child guard | `GET api/users/soap-integrations`: `P1.ConfigRead`; preview-change/PUT: `P1.ConfigManage` | ninguno |
| `/administracion/mft-ach` | antes NAV3.4N: `Admin AND (Config.Read OR CanReadAch)` | route top-level `Config.Read OR CanReadAch`; mutaciones `Config.Manage OR CanManageAch` | `P1.ConfigRead`/`P1.ConfigManage` según NAV3.4M | se elimina sólo el role `Admin` residual |

Esta caracterización no normaliza la discrepancia legacy de los tres primeros hijos; se registra únicamente para calcular la audiencia mínima del parent.

### Causa exacta del orphaning en backend

`GetMenuForCurrentUserHandler` permanece sin cambios. Su algoritmo:

1. carga todos los MenuItem activos;
2. ejecuta `ShouldInclude` para **cada item independientemente**;
3. `ShouldInclude` evalúa `Role OR` dentro de roles, `Permission OR` dentro de permisos y finalmente `Role AND Permission` entre ambos grupos;
4. construye `dtoLookup` sólo con los items que pasaron el filtro;
5. si un item visible tiene `ParentId` pero ese parent no existe en `dtoLookup`, ejecuta `roots.Add(dto)`.

Por ello un hijo con autorización válida puede ser promovido a raíz cuando su parent es filtrado. Ésa era la razón por la cual NAV3.4M no podía retirar de forma aislada el `Admin` de MFT: `Config.Read` podía hacer visible el hijo mientras `/integraciones` seguía fallando `Admin AND CanManageUsers`.

NAV3.4N **no modifica `GetMenuForCurrentUserHandler`**. El cambio general del algoritmo queda fuera de alcance.

### Modelo estructural final del parent `Conexiones`

El modelo ideal “parent sin autorización propia y visibilidad derivada de children” no es representable por el handler actual: un parent sin roles ni permisos pasaría siempre `ShouldInclude` y podría aparecer vacío. Tampoco el esquema actual puede expresar en un solo MenuItem la fórmula exacta:

```text
(Admin AND CanManageUsers)
OR Config.Read
OR CanReadAch
```

sin cambiar globalmente el algoritmo.

Se adopta por tanto la alternativa focal permitida: **parent sin role gate y con la unión de capacidades ya existentes en los cuatro hijos actuales**:

```text
MenuItemRole       ninguno
MenuItemPermission CanManageUsers
                   OR Config.Read
                   OR CanReadAch
```

No se crea `Connections.Read` ni ninguna capability nueva. No se agregan grants.

Esta unión garantiza para las reglas actuales:

- `Admin + CanManageUsers` conserva el parent para cualquiera de los tres hijos legacy;
- `Config.Read` conserva el parent para MFT;
- `CanReadAch` conserva el parent para MFT;
- la visibilidad del parent **no concede** ningún child: cada hijo continúa pasando su propio `ShouldInclude` de forma independiente;
- un principal `Config.Read`-only o `CanReadAch`-only recibe `Administración de parámetros > Conexiones > Configuración del intercambio de archivos` y no recibe los tres siblings legacy;
- un principal sin capability de ninguno de los cuatro hijos (por ejemplo `CanManageAch`-only) no recibe `Conexiones` ni sus superficies.

Limitación residual del algoritmo existente: un principal sintético con `CanManageUsers` pero sin role `Admin` puede hacer visible un parent vacío porque los tres hijos legacy exigen además `Admin`. Los grants canónicos actuales no crean ese estado (`CanManageUsers` se siembra para `Admin`), y resolver la fórmula disyuntiva exacta requeriría el cambio general de handler prohibido por NAV3.4N. No hay sibling leakage ni acceso funcional derivado del parent.

### NavigationService anterior y cambio focal

Antes, `NavigationService.normalizeIntegrationNavigation` recorría el árbol y, para `/integraciones` o rutas técnicas de integración reconocidas, sustituía el nodo por:

```text
label    Centro de integración CFA
route    /integraciones
exact    false
children []
```

Después deduplicaba todo nodo resultante con route `/integraciones`. En consecuencia, aunque el backend entregara `Conexiones` con children autorizados y ordenados, Angular reemplazaba el label y descartaba todo el subárbol.

NAV3.4N elimina **únicamente** esa adaptación especial:

- se eliminan `INTEGRATION_CENTER_ROUTE`, `OBSOLETE_INTEGRATION_NAV_ROUTES` y `normalizeIntegrationNavigation`;
- `getMenu()` conserva el flujo genérico existente `removeRetiredMenuItems -> sortMenu`;
- `Conexiones`, su label, su order y sus children provienen ahora del backend;
- los children se ordenan por `order` del backend;
- Angular no fabrica, reemplaza ni aplana una jerarquía paralela de Integraciones.

No se modifican otros overrides/filtros, `MainLayout`, títulos ni breadcrumbs.

### Cierre pendiente de NAV3.4M

`ConnectionsStructuralMenuAuthorizationSeeder` (Order 114, posterior al reconciliador MFT Order 113):

1. reconcilia únicamente el parent `/integraciones` con la unión `CanManageUsers OR Config.Read OR CanReadAch` y sin `MenuItemRole`;
2. elimina todos los `MenuItemRole` residuales de `/administracion/mft-ach`;
3. no modifica sus permisos directos `Config.Read OR CanReadAch`;
4. no toca los otros tres hijos;
5. no crea `Permission`, roles, `RolePermission` ni grants.

Autorización final del MenuItem MFT:

```text
MenuItemRole       ninguno
MenuItemPermission Config.Read OR CanReadAch
```

Así el menú queda alineado con la entrada SPA y `P1.ConfigRead` de NAV3.4M. Las piezas funcionales de MFT permanecen byte-identical al ZIP NAV3.4M: route/guard SPA, componente, endpoints y controllers `P1.ConfigRead/P1.ConfigManage` no se modifican.

La audiencia de **grants** no cambia. La audiencia efectiva visible del menú MFT sí se alinea con la audiencia ya autorizada por SPA/API: `ACH.Operator`, que ya tenía `CanReadAch`, deja de quedar oculto por el role `Admin`; no recibe nuevas capabilities ni nuevos permisos backend.

### GetMenuForCurrentUserHandler y otras superficies

`GetMenuForCurrentUserHandler` permanece byte a byte igual al ZIP NAV3.4M. También permanecen byte a byte iguales:

- `app-routing.module.ts` (incluido el guard SPA `/integraciones` y el route MFT);
- `integrations.routes.ts`;
- `IntegrationCatalogController`;
- `IntegrationContractsController`;
- `IntegrationMappingSetsController`;
- `SoapIntegrationSettingsController`;
- `AchColombiaFileExchangeController`;
- `AchColombiaCycleOperationalSchedulesController`;
- componente SPA MFT;
- `NavigationInformationArchitectureSeeder`;
- `TestToolsMenuAuthorizationSeeder`.

Por ello `Contrato del servicio`, `Relación de información` y `Conexión entidad participante` no cambian en autorización ni comportamiento. NAV3.4N tampoco modifica NAV2 hierarchy, labels, routes, IDs, ParentIds, orders ni `IsActive`.

### Validación NAV3.4N

Ejecutado en este entorno:

- `scripts/validate-nav3-4n-connections-navigation.py`: **PASS — 96 aserciones**.
- `scripts/validate-nav3-4m-mft-administration.py`: **PASS — 189 aserciones**, actualizando sólo la expectativa histórica del blocker que NAV3.4N resuelve.
- `scripts/validate-solution-projects.py`: **PASS**.
- `scripts/validate-nav3-3b-service-tests.py`: **PASS — 39**.
- NAV3.4B: **PASS — 72**.
- NAV3.4C: **PASS — 87**.
- NAV3.4D: **PASS — 100**.
- NAV3.4E: **PASS — 154**.
- NAV3.4F: **PASS — 171**.
- NAV3.4G2: **PASS — 85**.
- NAV3.4H: **PASS — 94**.
- NAV3.4I: **PASS — 73**.
- NAV3.4J: **PASS — 145**.
- NAV3.4K: **PASS — 86**.
- NAV3.4L: **PASS — 124**.
- Parseo sintáctico focal de `navigation.service.ts` y su spec: **PASS con TypeScript 5.8.3**.
- Guardas byte-level contra el ZIP NAV3.4M para handler, tres superficies excluidas, MFT funcional/API/SPA, IA y TestTools: **PASS**.

El script histórico `validate-nav3-4g-system-records.py` sigue siendo obsoleto en el propio ZIP de entrada: referencia `NavigationMenuNav34GSystemRecordsAuthorizationTests.cs`, archivo ya sustituido por la variante G2. La regresión vigente `NAV3.4G2` sí pasa; NAV3.4N no introduce este drift de herramienta.

Pruebas runtime:

- `.NET`: **`NOT_RUN_ENVIRONMENT`** — `dotnet` no está instalado/disponible.
- Angular: **`NOT_RUN_ENVIRONMENT`** — Node 22.16.0, npm 10.9.2 y TypeScript 5.8.3 están disponibles, pero `web/ach-interbank-ui/node_modules` no existe.

No se inventan resultados runtime.

### Estado de cierre y siguiente incremento

El blocker estructural que impedía completar NAV3.4M queda resuelto en código: para las cuatro autorizaciones actuales un hijo visible conserva `Administración de parámetros > Conexiones`, Angular preserva el subárbol backend y MFT ya no tiene el `MenuItemRole Admin` residual. Por tanto, el **gap de diseño/código de NAV3.4M puede considerarse completamente resuelto**, aunque la fase no se eleva a `VERIFIED/CLOSED` formal por la validación runtime .NET pendiente.

NAV3.4N también queda implementado en su alcance focal, con estado **`IMPLEMENTED_UNVERIFIED_ENVIRONMENT`** por la misma limitación de runtime. El handler general conserva su promoción de huérfanos y la fórmula exacta de parent visibilidad no puede derivarse de children sin refactor global; para las cuatro reglas actuales no existe orphaning.

No avanzar automáticamente. Recomendación exacta del siguiente incremento NAV3:

```text
NAV3.4O — Contrato del servicio (/integraciones/contrato)
```

Debe caracterizar y normalizar exclusivamente la autorización propia de `Contrato del servicio` (MenuItem/SPA/API/capabilities) sin modificar `Relación de información`, `Conexión entidad participante`, MFT ni el parent `Conexiones` ya normalizado.

## NAV3.4O — `Contrato del servicio` (`/integraciones/contrato`) — 2026-10-15

### Fuente y alcance

NAV3.4O parte **únicamente** de `ACHInterbank-R34-NAV-IA-NAV3.4N-FULL-PROJECT.zip`. No se usa Git. NAV2 permanece exactamente en `IMPLEMENTED_UNVERIFIED_ENVIRONMENT` porque la validación runtime .NET continúa pendiente.

El incremento se limita a `/integraciones/contrato`. Permanecen fuera de alcance y byte-identical respecto del ZIP NAV3.4N sus superficies funcionales/autorizativas propias: `/integraciones/relacion-informacion`, `/integraciones/conexion`, `/administracion/mft-ach`, el reconciliador del parent `Conexiones`, `NavigationService`, `GetMenuForCurrentUserHandler` y el guard SPA padre `/integraciones`.

### Identidad canónica del MenuItem

El child se reconcilia por ruta dentro de `NavigationInformationArchitectureSeeder.ReconcileConnectionsAsync` con:

```text
Label     Contrato del servicio
Route     /integraciones/contrato
ParentId  29 (`Conexiones`, una vez reconciliada la IA)
Order     10
Exact     true
IsActive  true
```

A diferencia del parent histórico `Conexiones` (`MenuItemConfiguration.IntegrationsId = 29`), **el child Contrato no tiene un ID entero fijo declarado en código**: `UpsertAsync` lo crea con `MenuItem.Id` generado por base de datos cuando no existe y luego lo identifica por la ruta canónica. Sin una base runtime no es correcto inventar el valor numérico concreto de un despliegue; NAV3.4O conserva el ID existente si el item ya existe y no modifica ID/ParentId/Order/Label/Route/IsActive.

Autorización de menú de entrada NAV3.4N:

```text
MenuItemRole       Admin
MenuItemPermission CanManageUsers
=> Admin AND CanManageUsers
```

### Qué representa realmente la superficie

`Contrato del servicio` es el workspace de **versiones del contrato técnico de una operación de integración**, especialmente WSDL/XSD y su metadata técnica/funcional. La pantalla agrupa versiones `Draft`, `Active` y `Retired`, permite consultar detalle/parametrización contractual y relaciona cada versión con el estado de su `Relación de información`.

El modelo demostrado por código es **B — superficie mixta**:

- lectura/consulta de catálogo, versiones, detalle y relación de información;
- administración focal de metadata funcional de parámetros de una versión editable;
- análisis local de un WSDL/XSD cargado por el usuario;
- preview local de diferencias contra una versión base;
- importación de un WSDL/XSD como **nuevo Draft**;
- no activa, publica, elimina ni revierte versiones desde esta UI;
- no prueba una operación SOAP ni invoca un endpoint externo desde esta UI.

El cliente `IntegrationContractApi` contiene además métodos para create/clone/compare/activate/rollback, y el controller expone esos endpoints, pero **el store/página `/integraciones/contrato` no los consume**. NAV3.4O no altera su autorización ni los incorpora a la UI.

### Operaciones y endpoints realmente consumidos

| Operación UI | Endpoint | Tipo | Persistencia | Efecto externo | Autorización previa | Autorización final |
|---|---|---|---|---|---|---|
| cargar métodos/operaciones | `GET api/integrations/methods` | lectura compartida | no | no | `CanReadAch` | `CanReadAch` |
| listar versiones por método | `GET api/integrations/methods/{methodId}/contracts` | lectura | no | no | `CanReadAch` | `CanReadAch` |
| cargar relaciones de información | `GET api/integrations/mappingsets?methodId=...` | lectura compartida | no | no | `CanReadAch` | `CanReadAch` |
| ver detalle contractual | `GET api/integrations/contracts/{id}` | lectura | no | no | `CanReadAch` | `CanReadAch` |
| editar metadata funcional de parámetro | `PUT api/integrations/contracts/{contractVersionId}/parameters/{parameterId}/presentation` | mutación | sí | no | `CanManageAch` | `P1.IntegrationContractsManage` |
| analizar WSDL/XSD local | `POST api/integrations/contracts/analyze` | administración/validación local | no | no | `CanManageAch` | `P1.IntegrationContractsManage` |
| preview de artefacto vs versión base | `POST api/integrations/methods/{methodId}/contracts/preview/{baseContractVersionId}` | administración/preview local | no | no | `CanManageAch` | `P1.IntegrationContractsManage` |
| importar artefacto como Draft | `POST api/integrations/methods/{methodId}/contracts/import` | mutación | sí | no | `CanManageAch` | `P1.IntegrationContractsManage` |

`IntegrationCatalogController` y `IntegrationMappingSetsController` sirven los dos GET compartidos. `IntegrationContractsController` atiende la lista/detalle y las cuatro operaciones contractuales. La persistencia se realiza por `IntegrationContractVersionService` / `IntegrationContractArtifactImportService` sobre `IntegrationContractVersion` y `IntegrationMethodParameter`.

La edición funcional sólo cambia `DisplayName`, `DescriptionEs`, `UiHelpText`, `Category` y `ExampleValue` y avanza la revisión contractual; no modifica nombre técnico, XPath, tipos, cardinalidad ni datos runtime del parámetro. La importación crea una nueva `IntegrationContractVersion` con `Status = Draft`, guarda sus parámetros y conserva `SourceReference = "local-upload"`; no activa ni publica automáticamente el contrato.

### Datos expuestos y sensibilidad

La UI/API muestra metadata técnica/funcional, entre ella:

- operación/método y propósito de mapeo;
- versión, estado, revisión, actor/fecha de importación;
- nombre del documento fuente o referencia de origen;
- `TargetNamespace`;
- nombre de operación;
- SOAP Action;
- hash SHA-256 del artefacto;
- parámetros con nombre técnico/XML path, XSD/data type, dirección, cardinalidad, obligatoriedad/min/max y soporte;
- nombre/descrición/ayuda/categoría/ejemplo funcionales;
- estado de relación de información/mapping asociado.

No se encontró en los DTOs realmente enviados a esta superficie ningún campo de password, token, client secret, private key, credencial, material de certificado ni secret reference. La pantalla tampoco consulta el endpoint/URL del participante: esa configuración pertenece a `/integraciones/conexion` y queda fuera de NAV3.4O.

`sourceReference` es metadata de procedencia, no una credencial; para la importación local el backend la fija a `local-upload`. NAV3.4O no introduce ninguna exposición nueva de secretos.

### WSDL/XSD y efectos externos

El análisis/importación usa `IntegrationContractArtifactAnalyzer` con `XmlReaderSettings` fail-closed (`DtdProcessing.Prohibit`, `XmlResolver = null` y límites de documento/entidades). El analizador rechaza referencias externas WSDL import y XSD import/include/redefine con `EXTERNAL_REFERENCE_NOT_ALLOWED`.

No existe descarga de WSDL, resolución HTTP, cliente SOAP, prueba de conectividad ni invocación productiva dentro de los requests consumidos por esta pantalla. `preview` repite análisis y comparación sin `SaveChanges`; `import` sólo persiste el Draft local. Por tanto **NAV3.4O no habilita ni modifica efectos externos**.

### Capacidades evaluadas

Se inspeccionan `FineGrainedPermissions`, `AllPermissions`, policies P1 y persistencia de `Permission`. Ya existían declaradas:

```text
Integrations.Read
Integrations.ManageMappings
Integrations.Validate
Integrations.Publish
Integrations.Compare
```

`Integrations.Read` describe semánticamente lectura de Integraciones, pero no puede sustituir de forma focal a `CanReadAch` en toda esta pantalla sin modificar dos GET compartidos (`methods` y `mappingsets`), la autorización propia de siblings y/o el parent `/integraciones`, todo ello fuera de NAV3.4O. Tampoco estaba materializada como `Permission` por un seeder específico. En consecuencia NAV3.4O **no la materializa ni amplía su audiencia** y mantiene `CanReadAch` como la capacidad de lectura efectiva/canónica de esta superficie durante este incremento.

`Integrations.ManageMappings`, `Integrations.Validate`, `Integrations.Publish` e `Integrations.Compare` no representan conjuntamente la administración de metadata contractual y la creación de un Draft. Usarlas habría mezclado dominios o dividido artificialmente un único flujo de administración contractual.

Se introduce una sola capability fina inequívoca:

```text
Integrations.ManageContracts
```

con policy de migración:

```text
P1.IntegrationContractsManage
= Integrations.ManageContracts OR CanManageAch
```

`IntegrationContractPermissionSeeder` la materializa idempotentemente en `Permission` (`40776ed1-238c-4a59-a890-894a1f7d9c8d`, descripción `Administrar contratos de integración`) y **no crea roles, RolePermissions, UserPermissions ni grants**.

Análisis, preview e importación usan la misma capacidad de gestión contractual porque son pasos del mismo flujo de incorporación de contrato y ninguno ejecuta una llamada externa. No se crea una capability separada de test/invocación porque esa operación no existe en la superficie objetivo.

### Autorización final de MenuItem

El MenuItem tenía `Admin AND CanManageUsers`, semánticamente ligado a administración de usuarios. Para corregir el permiso sin ampliar silenciosamente la audiencia de menú, `IntegrationContractMenuAuthorizationSeeder` reconcilia sólo sus permisos directos a:

```text
MenuItemRole       Admin   (preservado)
MenuItemPermission CanReadAch
=> Admin AND CanReadAch
```

No se elimina el `Admin` role en NAV3.4O porque la audiencia previa del MenuItem era Admin-only; retirarlo habría hecho visible el child también para `ACH.Operator` y sería una ampliación de audiencia no autorizada. El parent `Conexiones` de NAV3.4N permanece byte-identical y ya incluye `CanReadAch`, por lo que este hijo no queda huérfano.

En los grants seed canónicos, Admin ya posee tanto `CanManageUsers` como `CanReadAch`, de modo que la población sembrada del MenuItem no cambia. No se agrega ningún grant nuevo.

### Autorización SPA anterior y final

Antes de NAV3.4O el child `contrato` no tenía guard propio; heredaba únicamente el guard del parent SPA `/integraciones`:

```text
(Admin OR ACH.Operator)
AND
(CanReadAch OR CanManageAch)
```

NAV3.4O añade al child un `permissionGuard` explícito de `CanReadAch`, sin tocar el guard del parent. La frontera efectiva final es:

```text
(Admin OR ACH.Operator)
AND
CanReadAch
```

Esto evita que `CanManageAch` solo sustituya lectura. Admin y ACH.Operator del seed canónico ya poseen `CanReadAch`, por lo que su audiencia sembrada no cambia. Un principal sintético con sólo `CanManageAch` deja de entrar a la ruta hija: es un endurecimiento fail-closed deliberado y coherente con los GET realmente necesarios.

El `ContractWorkspaceStore` conserva `CanReadAch` como `canRead` y cambia `canManage` a `Integrations.ManageContracts OR CanManageAch`. Las operaciones de edición/importación ya tenían gating en la página/store; NAV3.4O añade además comprobación fail-closed dentro de los métodos sensibles del stepper (`analyzeSelectedFile`, `validateOperation`, `previewDiff`, `importDraft`) antes de invocar servicios y gating visual en sus botones.

### Autorización API anterior y final

La separación read/write del backend ya era fail-closed antes del incremento:

- todos los GET consumidos por la pantalla: `CanReadAch`;
- las cuatro mutaciones/operaciones administrativas consumidas: `CanManageAch`.

No existía un bypass lectura→escritura. NAV3.4O refina únicamente las cuatro operaciones target a la policy semántica `P1.IntegrationContractsManage`; el fallback `CanManageAch` preserva la audiencia de gestión existente mientras permite una migración futura explícita a la capability dedicada.

Los endpoints contractuales **no consumidos** por esta UI no se normalizan en este incremento: create/clone/activate/rollback continúan en `CanManageAch` y compare continúa en `CanReadAch`.

### Audiencia efectiva y bypasses

En los roles seed canónicos, Admin y ACH.Operator ya poseen `CanReadAch` y `CanManageAch`; no reciben `Integrations.ManageContracts`. Por eso las poblaciones canónicas que podían leer/administrar vía SPA/API continúan pudiendo hacerlo por los fallbacks vigentes.

El MenuItem continúa deliberadamente Admin-only para no ampliar su audiencia previa. En consecuencia existe una diferencia de **descubribilidad**, no un bypass: un `ACH.Operator` con `CanReadAch` puede satisfacer la ruta SPA y los GET por URL directa aunque el MenuItem no se le muestre. Esa divergencia existía antes y NAV3.4O no la resuelve ampliando el menú; requiere una decisión futura de audiencia/grants si se desea unificar descubribilidad.

`CanReadAch` solo puede satisfacer los GET backend y la lectura del store; para entrar a la SPA completa además se necesita un role permitido por el parent (`Admin` o `ACH.Operator`). No habilita edición, análisis, preview ni importación.

`CanManageAch` solo satisface el fallback de `P1.IntegrationContractsManage` y también algunos endpoints lifecycle no usados por la página, pero no satisface el nuevo guard child `CanReadAch` ni los GET requeridos por el workspace. Gestión no sustituye lectura.

### Regresión de superficies excluidas

Comparación byte-level contra el ZIP NAV3.4N confirma sin cambios:

- `ConnectionsStructuralMenuAuthorizationSeeder`;
- `NavigationService`;
- `GetMenuForCurrentUserHandler`;
- `MftAdministrationMenuAuthorizationSeeder`, componente MFT y `AchColombiaFileExchangeController`;
- página `Relación de información` y `IntegrationMappingSetsController`;
- página `Conexión entidad participante` y `SoapIntegrationSettingsController`;
- `app-routing.module.ts` (guard del parent `/integraciones`).

Por tanto `Conexiones`, MFT, Relación de información y Conexión entidad participante permanecen intactos en NAV3.4O. Sólo se toca el child route `contrato` dentro de `integrations.routes.ts`.

### Validación NAV3.4O

Validaciones disponibles ejecutadas en el árbol de trabajo:

- solution/project graph: **PASS**;
- Herramientas de prueba / service regression: **PASS — 39**;
- NAV3.4B: **PASS — 72**;
- NAV3.4C: **PASS — 87**;
- NAV3.4D: **PASS — 100**;
- NAV3.4E: **PASS — 154**;
- NAV3.4F: **PASS — 171**;
- NAV3.4G2: **PASS — 85**;
- NAV3.4H: **PASS — 94**;
- NAV3.4I: **PASS — 73**;
- NAV3.4J: **PASS — 145**;
- NAV3.4K: **PASS — 86**;
- NAV3.4L: **PASS — 124**;
- NAV3.4O focal: **PASS — 210 aserciones**;
- parseo sintáctico focal TypeScript (6 archivos) con TypeScript 5.8.3: **PASS**.

Los validadores históricos NAV3.4M/N contienen snapshots de archivos target que NAV3.4O modifica legítimamente (`IntegrationContractsController`/`integrations.routes.ts`); no se usan para afirmar regresión de esos snapshots. El validador NAV3.4O reemplaza esa comprobación con hashes exactos de todos los artefactos MFT/N/parent/siblings que deben permanecer fuera de alcance.

`.NET`: **`NOT_RUN_ENVIRONMENT`** — `dotnet` no está instalado. Angular runtime/test/build: **`NOT_RUN_ENVIRONMENT`** — Node/NPM están presentes pero `web/ach-interbank-ui/node_modules` no existe. No se inventan resultados runtime.

### Estado y siguiente incremento

El gap de autorización de `Contrato del servicio` queda implementado a nivel diseño/código como **`IMPLEMENTED_UNVERIFIED_ENVIRONMENT`**. No se declara formalmente `CLOSED` mientras continúe pendiente la validación runtime .NET. NAV2 sigue `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`.

Gap residual deliberado: el MenuItem sigue Admin-only mientras la ruta/API también admiten ACH.Operator con `CanReadAch`; cambiar esa audiencia requeriría una decisión explícita de población/grants, no una ampliación implícita dentro de NAV3.4O. También queda para incrementos futuros la eventual migración desde `CanManageAch` hacia `Integrations.ManageContracts` y, si se decide, la adopción consistente de `Integrations.Read` en las lecturas compartidas.

No avanzar automáticamente. Recomendación exacta del siguiente incremento:

```text
NAV3.4P — Relación de información (/integraciones/relacion-informacion)
```

limitado exclusivamente a caracterizar y normalizar su propia autorización MenuItem/SPA/API/capacidades, manteniendo intactos `/integraciones/conexion`, MFT y el parent `Conexiones`.

## NAV3.4P — autorización de `Relación de información` — 2026-10-15

### Alcance y estructura canónica

NAV3.4P parte exclusivamente del ZIP FULL NAV3.4O. No usa Git y no reabre NAV3.4N/O. El item objetivo continúa bajo el parent canónico `Conexiones`:

```text
Label     Relación de información
Route     /integraciones/relacion-informacion
ParentId  Conexiones (/integraciones)
Order     20
Exact     true
IsActive  true
```

El child no tiene un ID fijo declarado en el seeder de IA: se reconcilia por ruta y conserva el ID existente en base. NAV3.4P no cambia label, route, ParentId, order, IsActive ni el parent `Conexiones`.

Autorización previa del MenuItem (heredada del modelo legacy anterior a este incremento):

```text
MenuItemRole       Admin
MenuItemPermission CanManageUsers
=> Admin AND CanManageUsers
```

### Qué administra realmente la superficie

La superficie consta de dos rutas: la vista principal `/integraciones/relacion-informacion` (`MappingSetsPageComponent`) y el editor contextual `/integraciones/relacion-informacion/:methodCode/:mappingSetId` (`MappingEditorPageComponent`). Ambas trabajan sobre `IntegrationMappingSet` y sus reglas.

Operaciones realmente consumidas por estas pantallas:

- leer métodos activos de integración;
- listar mapping sets por método;
- cargar el workspace del mapping set seleccionado, incluida versión contractual asociada, parámetros, catálogo de orígenes, transformaciones compatibles, cobertura y elegibilidad de publicación;
- leer detalle del mapping set, parámetros del método, catálogo fuente, transformaciones e historial;
- actualizar el mapping guiado de un parámetro (`sourceCatalogFieldId`, transformación y máscara), con concurrencia por `Revision`;
- hacer upsert de reglas en el editor técnico, incluyendo origen, valor fijo/default, transformación, máscara, prioridad, override de obligatoriedad y enabled; el backend rechaza `SourceKind Expression` y `ConditionExpression` no vacío;
- validar el mapping set;
- generar una vista previa sanitizada usando muestra controlada o una transacción/ciclo persistido;
- publicar el Draft cuando backend indica que es publicable;
- clonar el mapping set desde el editor técnico.

No existe DELETE de mapping sets en el controller. Los clientes compartidos exponen create/update generales y operaciones cross-contract/compare, pero las rutas objetivo no consumen create/update generales, delete, import/export, `clone-to-contract-version`, `validate-contract-mapping` ni compare. El botón `Revisar publicación` del editor técnico sólo vuelve a la vista principal; la publicación real ocurre allí.

### Relación con Contrato del servicio

Relación de información consume metadata contractual de forma de lectura: parámetros del método, `ContractVersionId`, versión/namespace del contrato dentro del workspace y datos de cobertura contra esos parámetros. Esos GET son compartidos con el workspace de integración y permanecen en `CanReadAch`.

NAV3.4P no requiere ni usa `Integrations.ManageContracts`/`P1.IntegrationContractsManage`; no modifica metadata contractual ni crea/importa contratos. `Contrato del servicio` y su autorización NAV3.4O permanecen byte-identical en sus artefactos focales.

### Datos modificados y sensibilidad

Las reglas persistidas pueden contener:

```text
SourceKind
SourceCatalogFieldId / SourceFieldPath
FixedValue
DefaultValue
TransformationCode
FormatMask
Priority
RequiredOverride
Enabled
ConditionExpression
```

Además se persisten revision, historia/auditoría y metadata del mapping set. La validación persiste `ValidationSummaryJson`. Publicar cambia estado/versionado: archiva el Published anterior del mismo scope, marca el objetivo `Published`, `IsActive=true`, registra `PublishedAtUtc`/`PublishedBy`, aumenta la revisión y genera historia.

No existen campos dedicados de password, token, private key, certificate o secret reference en los DTO de mapping. Sí se devuelven `FixedValue` y `DefaultValue` como valores de configuración; el código no demuestra que sean un secret store ni que se enmascaren al leer el mapping, por lo que no deben utilizarse para credenciales. NAV3.4P conserva ese comportamiento y no introduce nueva exposición.

La vista previa puede basarse en una transacción persistida. `IntegrationMappingPreviewService` sanitiza antes de responder: mantiene catálogos de fragmentos sensibles y marcadores de secretos, aplica `SanitizePreviewValue` y devuelve `PreviewValue`, `PayloadPreviewJson` y `RawPreviewJson` ya sanitizados junto con `IsMasked`. NAV3.4P no debilita ese mecanismo.

### Efectos persistentes y externos

`ValidateAsync` calcula la validación y persiste el resumen; `PreviewAsync` calcula una muestra/validación sin ejecutar transporte externo; `PublishAsync` modifica estado/versionado del mapping. No se encontró `HttpClient`, cliente SOAP CFA/SFTP ni invocación externa dentro de los servicios de mapping/validación/preview usados por estas acciones.

La publicación sí tiene efecto productivo **posterior**: `IntegrationExecutionContractResolver` y los mapeadores productivos seleccionan mapping sets `Published && IsActive`. Por tanto cambiar/publicar mappings altera cómo futuras ejecuciones construyen datos hacia integraciones, y se trata como administración sensible. El request de publicación no transmite por sí mismo, no dispara SOAP y no inicia un job externo.

### Capacidades preexistentes evaluadas

En `FineGrainedPermissions`/`AllPermissions` ya existían:

```text
Integrations.Read
Integrations.ManageContracts
Integrations.ManageMappings
Integrations.Validate
Integrations.Publish
Integrations.Compare
CanReadAch
CanManageAch
```

El diseño de permisos finos existente define explícitamente `Integrations.ManageMappings` como gestión de mapping sets, `Integrations.Validate` como validación de integración y `Integrations.Publish` como publicación de configuración. La implementación real confirma esa separación; por tanto NAV3.4P **reutiliza** esas capabilities y no crea ninguna capability conceptual nueva.

`Integrations.Read` no se adopta en NAV3.4P. Los GET necesarios (`methods`, parámetros, catálogo fuente, transformaciones, mapping sets, workspace, history) están compartidos y actualmente protegidos con `CanReadAch`. Cambiarlos de forma focal alteraría siblings/NAV3.4O y podría requerir revisar el parent; se conserva `CanReadAch` y se registra la deuda semántica.

### Materialización y policies P1

NAV3.4P materializa idempotentemente, sin grants:

```text
Integrations.ManageMappings  a78e21ac-846c-470d-a9a5-5d4bd019bf08
Integrations.Validate        67d58589-d51e-44b2-8177-2650d93bc46d
Integrations.Publish         911b72f4-ef59-48e4-86de-7d559dbafb1d
```

No se crean roles, `RolePermissions` ni asignaciones directas a usuarios.

Policies de transición:

```text
P1.IntegrationMappingsManage
= Integrations.ManageMappings OR CanManageAch

P1.IntegrationMappingsValidate
= Integrations.Validate OR CanManageAch

P1.IntegrationMappingsPublish
= Integrations.Publish OR CanManageAch
```

El fallback `CanManageAch` conserva la audiencia administrativa existente mientras permite una migración futura a capacidades específicas sin grants implícitos.

### Autorización final de MenuItem

`InformationRelationshipMenuAuthorizationSeeder` reconcilia únicamente este child y sustituye el permiso incoherente `CanManageUsers` por la frontera de lectura realmente necesaria:

```text
MenuItemRole       Admin   (preservado)
MenuItemPermission CanReadAch
=> Admin AND CanReadAch
```

Se preserva `Admin` porque quitarlo ampliaría silenciosamente la descubribilidad del menú a ACH.Operator. En los grants seed canónicos Admin ya posee `CanReadAch`, así que la población sembrada del MenuItem no cambia. El parent `Conexiones` permanece intacto y ya admite `CanReadAch`, por lo que no hay orphaning para esta audiencia.

### SPA anterior y final

Antes de NAV3.4P las rutas `relacion-informacion` y su editor no tenían guard hijo; dependían sólo del parent `/integraciones`:

```text
(Admin OR ACH.Operator) AND (CanReadAch OR CanManageAch)
```

NAV3.4P añade `permissionGuard` explícito `CanReadAch` a ambas rutas, sin tocar el parent. La frontera efectiva final queda:

```text
(Admin OR ACH.Operator) AND CanReadAch
```

Las acciones SPA quedan separadas:

```text
editar/guardar/clonar mappings  Integrations.ManageMappings OR CanManageAch
validar / preview               Integrations.Validate OR CanManageAch
publicar                        Integrations.Publish OR CanManageAch
```

Hay gating visual y comprobación dentro de los métodos antes de llamar al servicio. `MappingWorkspaceStore` recibe explícitamente si preview está autorizado y, por defecto, falla cerrado sin invocar el endpoint; así `CanReadAch` solo no dispara automáticamente `POST preview`.

No hay checks por nombre de rol dentro de las acciones de mapping.

### API anterior y final

Antes, los GET usaban `CanReadAch` y todas las operaciones mutables/operativas del controller usaban `CanManageAch`.

NAV3.4P mantiene todos los GET compartidos en `CanReadAch` y normaliza las operaciones de mapping del flujo target:

```text
GET mappingsets                         CanReadAch
GET mappingsets/published               CanReadAch
GET mappingsets/{id}                    CanReadAch
GET mappingsets/{id}/workspace          CanReadAch
GET mappingsets/{id}/history            CanReadAch

POST mappingsets                        P1.IntegrationMappingsManage
PUT mappingsets/{id}                    P1.IntegrationMappingsManage
PUT mappingsets/{id}/rules              P1.IntegrationMappingsManage
PUT mappingsets/{id}/parameters/{pid}   P1.IntegrationMappingsManage
POST mappingsets/{id}/clone             P1.IntegrationMappingsManage

POST mappingsets/{id}/validate          P1.IntegrationMappingsValidate
POST mappingsets/{id}/preview           P1.IntegrationMappingsValidate
POST mappingsets/{id}/publish           P1.IntegrationMappingsPublish
```

No hay DELETE.

Los endpoints cross-contract no consumidos por Relación de información (`clone-to-contract-version`, `validate-contract-mapping`) y `compare` permanecen en `CanManageAch` para no reabrir NAV3.4O/otros flujos.

### Audiencia y bypasses

La audiencia canónica sembrada no cambia: Admin/ACH.Operator ya poseen `CanReadAch` y `CanManageAch`, y no reciben grants de las capabilities finas nuevas-materializadas. El MenuItem continúa Admin-only; por tanto persiste, igual que en Contrato, una diferencia de descubribilidad: ACH.Operator con `CanReadAch` puede entrar por URL directa aunque no vea el item. Esto no evade el backend.

`CanReadAch` solo permite lectura/entrada semántica; no satisface Manage/Validate/Publish. `CanManageAch` solo satisface los tres fallbacks backend, pero no sustituye `CanReadAch` en el guard child ni en los GET necesarios. `Integrations.ManageMappings` solo satisface la policy backend de gestión de mappings; no satisface lectura, validate ni publish. `Integrations.Validate` y `Integrations.Publish` permanecen separadas y sí forman parte del flujo real.

No se encontró bypass de lectura a mutación. Los endpoints mutables no aceptan una capability de lectura. El editor no permite asociar libremente reglas a parámetros de otro método: el backend valida parámetros activos del mismo método y el flujo guiado usa `SourceCatalogFieldId`/compatibilidad backend. Las operaciones cross-contract quedan fuera del flujo objetivo.

### Regresión fuera de alcance

Hashes byte-level respecto del ZIP NAV3.4O confirman intactos:

- `ConnectionsStructuralMenuAuthorizationSeeder`;
- `NavigationService`;
- `GetMenuForCurrentUserHandler`;
- `app-routing.module.ts` y por tanto el guard parent `/integraciones`;
- seeders/menu/página/controller focales de `Contrato del servicio` NAV3.4O;
- `Conexión entidad participante` y `SoapIntegrationSettingsController`;
- MFT menu/component/API.

El único ajuste a una prueba NAV3.4O es su snapshot estático de los archivos de mapping que NAV3.4P cambia legítimamente; las aserciones funcionales de Contrato permanecen y siguen pasando en validación estática.

### Estado

NAV3.4P queda implementado a nivel diseño/código como `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`. NAV2 permanece exactamente `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`; no se declara `CLOSED` porque la validación runtime .NET sigue pendiente.

No avanzar automáticamente. Siguiente incremento recomendado, sujeto a decisión del usuario:

```text
NAV3.4Q — Conexión entidad participante (/integraciones/conexion)
```

limitado exclusivamente a caracterizar y normalizar esa superficie sin reabrir Contrato, Relación de información, MFT ni el parent `Conexiones`.

### Validaciones NAV3.4P

Validaciones razonables disponibles ejecutadas sobre el árbol NAV3.4P:

- solution/project graph: **PASS** — 7/7 proyectos y referencias válidas;
- Herramientas de prueba / NAV3.3B: **PASS — 39 aserciones**;
- NAV3.4B: **PASS — 72**;
- NAV3.4C: **PASS — 87**;
- NAV3.4D: **PASS — 100**;
- NAV3.4E: **PASS — 154**;
- NAV3.4F: **PASS — 171**;
- NAV3.4G2: **PASS — 85**;
- NAV3.4H: **PASS — 94**;
- NAV3.4I: **PASS — 73**;
- NAV3.4J: **PASS — 145**;
- NAV3.4K: **PASS — 86**;
- NAV3.4L: **PASS — 124**;
- NAV3.4O regression: **PASS — 210**;
- NAV3.4P focal: **PASS — 190**;
- parseo sintáctico focal TypeScript con TypeScript 5.8.3: **PASS — 7 archivos**.

Los validadores NAV3.4M/N mantienen snapshots de superficies que fueron modificadas legítimamente por NAV3.4O/P y no se usan como prueba de regresión de esos snapshots. NAV3.4P usa hashes byte-level de parent/NavigationService/GetMenu/MFT/Contrato/Conexión para cubrir las invariantes fuera de alcance.

`.NET`: **NOT_RUN_ENVIRONMENT** — `dotnet` no está instalado. Angular build/test/runtime: **NOT_RUN_ENVIRONMENT** — Node/NPM/TypeScript están disponibles pero `web/ach-interbank-ui/node_modules` no existe. No se inventan resultados runtime.

## NAV3.4Q — autorización de `Conexión entidad participante` — 2026-10-15

### Fuente, alcance y estado heredado

NAV3.4Q parte **únicamente** de `ACHInterbank-R34-NAV-IA-NAV3.4P-FULL-PROJECT.zip`. No usa Git y no reabre las decisiones cerradas de NAV3.4N/O/P. `NAV2` permanece exactamente `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`; no se declara `CLOSED` porque la validación runtime .NET sigue pendiente.

La superficie objetivo conserva su identidad NAV2 canónica:

```text
Administración de parámetros
└── Conexiones                /integraciones
    └── Conexión entidad participante
        /integraciones/conexion
        order 30
```

El child se reconcilia por ruta en el seed canónico; NAV3.4Q no cambia su label, route, ParentId, order, `IsActive`, icono ni el parent `Conexiones`.

### Comportamiento funcional real

`/integraciones/conexion` es una superficie **mixta de lectura + administración de configuración de conexión (modelo B)**. No existe en la UI ni en el controller target una tercera operación de prueba/invocación externa.

La página usa `IntegrationConnectionApi` y consume exactamente:

```text
GET  api/users/soap-integrations
POST api/users/soap-integrations/preview-change
PUT  api/users/soap-integrations
```

Operaciones reales:

- cargar la configuración SOAP persistida para los clientes `WscfaachSoapClient` y `WsAxonRespuestaTransaccionesSoapClient`;
- seleccionar un método/servicio;
- consultar destino y SOAP Action **sanitizados**, modo operativo, enabled, estado calculado y metadata de mappings de entrada;
- editar únicamente endpoint, SOAP Action, `OperatingMode` (`Live`/`DryRun`/`Disabled`) y `Enabled` para el método seleccionado;
- antes de un cambio que habilita o mantiene envío real en condiciones sensibles, pedir `preview-change`, que valida revision/estado y devuelve sólo destino/acción sanitizados para confirmación contextual;
- guardar la configuración mediante PUT con control de revision y recargar la representación sanitizada.

La UI **no** ofrece: cambio de participante por un selector independiente, edición de timeout/retries, headers de autenticación, username/password/token/API key/client secret, certificados/private keys, referencia de secreto, test de conexión, test de autenticación, ejecución SOAP manual, envío de XML de prueba, DELETE ni activación/publicación separada. El timeout forma parte del mapping backend y se preserva al guardar, pero el editor target no lo modifica.

### Datos sensibles y modelo de secretos

La entidad/configuración persistida contiene mappings JSON con datos técnicos de conexión: `MethodName`, `Endpoint`, `SoapAction`, `OperatingMode`, `TimeoutSeconds`, `Enabled` e `InputParameterMappings`. Los DTO de esta superficie no definen campos dedicados de password, access token, API key, client secret, private key, `SecretRef`, credencial cifrada ni material de certificado.

Los valores técnicos que sí llegan al navegador se protegen de esta forma:

- `SoapIntegrationSettingsService.GetAdministrationAsync` devuelve `Endpoint` y `SoapAction` reemplazados por sus variantes sanitizadas;
- endpoint se reduce a una representación del tipo `scheme://masked-host/***`, sin exponer ruta/puerto completos;
- SOAP Action se sanitiza eliminando userinfo/query/fragment cuando corresponde;
- al guardar, `PreservePersistedSensitiveValues` reconoce las variantes sanitizadas como placeholders y conserva el valor raw persistido en backend;
- `PreservePersistedTimeouts` conserva el timeout persistido cuando el cliente no lo administra;
- `DefaultValue` de input mappings es metadata de configuración existente, no un secret store demostrado por el código.

Por tanto NAV3.4Q **no introduce lectura ni escritura de secretos**, no crea un endpoint de credenciales y no modifica cifrado, key vault, secret store, masking, rotación ni redacción de logs. El modelo target es de metadata técnica sanitizada en browser y valores completos conservados exclusivamente en backend/persistencia.

### Efectos externos y productivos

Los tres requests de esta UI no ejecutan transporte externo:

- GET sólo lee y proyecta configuración;
- `preview-change` sólo valida revision/estado/cambio y sanitiza metadata; no persiste y no invoca SOAP;
- PUT persiste configuración y devuelve la proyección sanitizada; no invoca SOAP.

Sin embargo, la configuración guardada tiene **efecto productivo posterior** porque los clientes runtime consultan `ISoapIntegrationSettingsService` antes de enviar:

- `WscfaachSoapClient` puede ejecutar HTTP POST SOAP para `Proc_Contrapartidas` y `Proc_Transacciones` sólo cuando el mapping de base está `Enabled`, en `OperatingMode=Live`, con timeout válido y endpoint absoluto seguro sin userinfo/fragment. Usa `ControlledLocalSoapTransportResolver` cuando corresponde y mantiene `retry=disabled`;
- `WsAxonRespuestaTransaccionesSoapClient` puede ejecutar HTTP POST SOAP para `RegistrarRespuestaTransaccion` bajo las mismas condiciones Live/enabled/timeout y además exige una policy de endpoint explícita: `Unconfigured` falla cerrado; `ControlledLocal` restringe el destino local; `ConfiguredAllowlist` exige esquemas/hosts/paths explícitos y puede exigir HTTPS.

`Proc_Transacciones` y `Proc_Contrapartidas` pertenecen al procesamiento ACH y pueden tener efecto productivo/monetario downstream cuando otro flujo runtime los ejecuta. `RegistrarRespuestaTransaccion` transmite respuestas/estados. **NAV3.4Q no ejecuta esos clientes ni altera sus payloads, timeouts, retries, feature/environment gates o comportamiento de transporte.**

Los defaults generales conservan `ProcContrapartidas.Mode=DryRun` y `ProcTransacciones.Mode=DryRun`; Development conserva su bootstrap local controlado y configuración `SoapTransport`/`SoapEndpointSecurity`. El runtime efectivo de los clientes sigue dependiendo además del mapping persistido Live/enabled. No se elimina ni modifica ningún gate.

Gap de hardening preexistente observado y fuera de alcance: `WsAxonRespuestaTransaccionesSoapClient` dispone de `ConfiguredAllowlist`; `WscfaachSoapClient` valida URI y puede usar el transporte local controlado, pero no presenta una policy `ConfiguredAllowlist` equivalente en el flujo inspeccionado. NAV3.4Q no rediseña esa seguridad de transporte.

### Capabilities evaluadas y modelo final

Antes de NAV3.4Q existían, entre otras:

```text
Integrations.Read
Integrations.ManageContracts
Integrations.ManageMappings
Integrations.Validate
Integrations.Publish
Integrations.Compare
Config.Read
Config.Manage
CanReadAch
CanManageAch
```

Ninguna capability existente representa semánticamente la administración de endpoint/SOAP Action/modo/enabled de una conexión participante:

- `Config.Manage` corresponde al dominio de configuración/MFT y no debe reutilizarse sólo por amplitud;
- `Integrations.ManageContracts` administra contratos, no conexiones;
- `Integrations.ManageMappings` administra relaciones de información;
- `Integrations.Validate` protege validate/preview de mappings, no el `preview-change` de estado de conexión que forma parte del flujo de guardar;
- `Integrations.Publish` no describe persistencia de configuración de conexión.

NAV3.4Q introduce una única capability fina:

```text
Integrations.ManageConnections
```

`IntegrationConnectionPermissionSeeder` la materializa idempotentemente como:

```text
289e92ea-abd4-43a9-b93a-23b06fb03362
Integrations.ManageConnections
Administrar conexiones de integración
```

No crea roles, `RolePermission`, grants a Admin/ACH.Operator ni permisos directos a usuarios.

La policy de transición es:

```text
P1.IntegrationConnectionsManage
= Integrations.ManageConnections OR CanManageAch
```

`CanManageAch` se conserva como fallback legacy para no romper la audiencia administrativa existente. No se crea una capability separada de test/invocación porque la superficie no contiene tal operación; `preview-change` es una precondición local/backend de una modificación de configuración y queda bajo la misma capability de administración.

### Lectura

La capability de lectura final de esta superficie es `CanReadAch`.

Antes el GET estaba bajo:

```text
P1.ConfigRead = Config.Read OR CanReadAch
```

pero `Config.Read` pertenece al modelo de configuración MFT y no representa semánticamente esta superficie SOAP. NAV3.4Q restringe el GET target a `CanReadAch`. No adopta `Integrations.Read`: el parent `Conexiones` y las lecturas compartidas de Contrato/Relación permanecen deliberadamente sobre `CanReadAch`, y adoptar `Integrations.Read` sólo aquí introduciría una audiencia huérfana o exigiría reabrir NAV3.4N/O/P. La migración transversal a `Integrations.Read` queda como deuda explícita posterior.

Este cambio no amplía audiencia. Un principal sintético con sólo `Config.Read` deja de poder leer este endpoint; la población canónica sembrada no depende de ese permiso para esta superficie.

### Autorización anterior

Antes de NAV3.4Q:

```text
MenuItem /integraciones/conexion
  MenuItemRole       Admin
  MenuItemPermission CanManageUsers
  => Admin AND CanManageUsers

SPA child /integraciones/conexion
  sin guard hijo
  heredaba del parent /integraciones:
  (Admin OR ACH.Operator) AND (CanReadAch OR CanManageAch)

Componente
  canRead   = canManage OR (Config.Read OR CanReadAch)
  canManage = Config.Manage OR CanManageAch

API
  GET            P1.ConfigRead
                 Config.Read OR CanReadAch
  POST preview   P1.ConfigManage
                 Config.Manage OR CanManageAch
  PUT save       P1.ConfigManage
                 Config.Manage OR CanManageAch
```

La separación lectura/escritura ya era fail-closed en backend de forma genérica, pero usaba capabilities del dominio `Config.*` que no expresaban la semántica de conexión participante.

### Autorización final

#### MenuItem

`ParticipantConnectionMenuAuthorizationSeeder` cambia únicamente el permiso directo del child y conserva el role histórico `Admin`:

```text
MenuItemRole       Admin
MenuItemPermission CanReadAch
=> Admin AND CanReadAch
```

El role `Admin` se mantiene deliberadamente para no ampliar silenciosamente la descubribilidad del menú a ACH.Operator. No se tocan label, route, ParentId, order, `IsActive` ni el parent `Conexiones`.

#### SPA

La ruta hija declara explícitamente:

```text
permissionGuard
CanReadAch
```

Combinada con el parent `/integraciones` intacto, la frontera efectiva es:

```text
(Admin OR ACH.Operator) AND CanReadAch
```

El componente queda:

```text
canRead   = CanReadAch
canManage = Integrations.ManageConnections OR CanManageAch
```

La edición permanece protegida visualmente y dentro del método: `beginEdit()` depende de `canEditSelected`; `save()` vuelve a exigir `canManage`, revision/estado conocido y otras precondiciones antes de invocar el adapter. `preview-change` sólo se ejecuta desde el flujo de guardado autorizado. No hay checks por nombre de rol dentro de las acciones.

Una capability de gestión por sí sola no sustituye la lectura/entrada SPA.

#### API

Autorización final:

```text
GET  api/users/soap-integrations
  CanReadAch

POST api/users/soap-integrations/preview-change
  P1.IntegrationConnectionsManage
  Integrations.ManageConnections OR CanManageAch

PUT  api/users/soap-integrations
  P1.IntegrationConnectionsManage
  Integrations.ManageConnections OR CanManageAch
```

No existe endpoint target de test connection, test authentication, invoke, credential update o DELETE.

### Audiencia, fallbacks y bypasses

La población canónica sembrada conserva `CanReadAch` y `CanManageAch` para Admin/ACH.Operator. NAV3.4Q no agrega grants de `Integrations.ManageConnections`; por tanto la audiencia sembrada de lectura/gestión no se amplía.

Cambios fail-closed de audiencias sintéticas:

- `Config.Read`-only ya no satisface el GET target;
- `Config.Manage`-only ya no satisface preview/PUT;
- `CanManageAch`-only conserva el fallback backend de gestión, pero no satisface la lectura/guard hijo necesaria para operar la SPA;
- `Integrations.ManageConnections`-only satisface los endpoints de gestión si el claim se asigna en el futuro, pero no satisface por sí sola GET/SPA read.

Persiste la diferencia deliberada de descubribilidad de los tres children administrativos de Integraciones: ACH.Operator con `CanReadAch` puede atravesar la ruta SPA directa y los GET, pero el MenuItem sigue Admin-only. No constituye bypass de backend. No se encontró bypass `CanReadAch -> escritura`, ni una ruta de lectura que ejecute SOAP.

### Regresión fuera de alcance

Las validaciones NAV3.4Q comprueban byte-level contra el ZIP NAV3.4P que permanecen intactos:

- `ConnectionsStructuralMenuAuthorizationSeeder` y por tanto el parent `Conexiones`;
- `NavigationService`;
- `GetMenuForCurrentUserHandler`;
- `app-routing.module.ts` y el guard parent `/integraciones`;
- seeders/component/controller focales de `Contrato del servicio` NAV3.4O;
- seeders/component/controller focales de `Relación de información` NAV3.4P;
- MFT Menu/SPA/API;
- `SoapIntegrationSettingsService`, entidades DTO runtime y clientes SOAP, salvo los artefactos autorizativos target explícitamente modificados.

Los cambios a validadores anteriores N/O/P son sólo actualizaciones mínimas de snapshots/expectativas para reconocer las modificaciones Q legítimas en la superficie target; no debilitan sus invariantes funcionales.

### Estado, deuda residual y siguiente incremento

NAV3.4Q queda implementado a nivel diseño/código como `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`. NAV2 permanece exactamente `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`; no se declara `CLOSED` mientras `.NET` runtime continúe no disponible.

La familia `Conexiones` queda caracterizada y normalizada focalmente en M/N/O/P/Q. Deudas transversales deliberadamente no migradas:

1. decidir si la lectura compartida de las tres superficies de integración debe migrar de `CanReadAch` a `Integrations.Read`;
2. decidir explícitamente la audiencia de menú Admin-only frente a acceso directo de ACH.Operator para Contrato/Relación/Conexión;
3. decidir población/grants de `Integrations.ManageContracts`, `Integrations.ManageMappings`, `Integrations.Validate`, `Integrations.Publish` e `Integrations.ManageConnections` antes de retirar fallbacks `CanManageAch`;
4. si se adopta `Integrations.Read`, revisar conjuntamente el parent `Conexiones` para evitar orphaning en lugar de ampliarlo silenciosamente;
5. evaluar en incremento separado el hardening/allowlist de WSCFAACH, sin mezclarlo con autorización de menú.

No avanzar automáticamente. Recomendación exacta del siguiente incremento NAV3, sujeto a aprobación explícita:

```text
NAV3.4R — modelo de lectura y audiencia del subárbol Conexiones
```

limitado a decidir/migrar de forma conjunta `Integrations.Read` y la audiencia de descubribilidad de `/integraciones/contrato`, `/integraciones/relacion-informacion` y `/integraciones/conexion`, preservando las capabilities finas de gestión ya introducidas y **sin** otorgar grants ni retirar fallbacks hasta que la población objetivo sea decidida explícitamente.

### Validaciones NAV3.4Q

Validaciones razonables disponibles ejecutadas sobre el árbol NAV3.4Q antes del empaquetado:

- solution/project graph: **PASS — 7/7** proyectos y todas las referencias válidas;
- Herramientas de prueba / NAV3.3B: **PASS — 39 aserciones**;
- NAV3.4B: **PASS — 72**;
- NAV3.4C: **PASS — 87**;
- NAV3.4D: **PASS — 100**;
- NAV3.4E: **PASS — 154**;
- NAV3.4F: **PASS — 171**;
- NAV3.4G2: **PASS — 85**;
- NAV3.4H: **PASS — 94**;
- NAV3.4I: **PASS — 73**;
- NAV3.4J: **PASS — 145**;
- NAV3.4K: **PASS — 86**;
- NAV3.4L: **PASS — 124**;
- NAV3.4M: **PASS — 189**;
- NAV3.4N: **PASS — 96**;
- NAV3.4O: **PASS — 210**;
- NAV3.4P: **PASS — 190**;
- NAV3.4Q focal: **PASS — 177**;
- parseo sintáctico focal TypeScript: **PASS — 4 archivos con TypeScript 5.8.3**.

El validador histórico `validate-nav3-4g-system-records.py` no se usa porque en el estado de entrada ya referencia una prueba G reemplazada; la regresión vigente es NAV3.4G2 y pasa.

Pruebas runtime:

- `.NET`: **NOT_RUN_ENVIRONMENT** — `dotnet` no está instalado/disponible;
- Angular build/test/runtime: **NOT_RUN_ENVIRONMENT** — Node 22.16.0, npm 10.9.2 y TypeScript 5.8.3 están disponibles, pero `web/ach-interbank-ui/node_modules` no existe.

No se inventan resultados runtime. El ZIP FULL final fue extraído en limpio y la misma batería vigente volvió a pasar antes del handoff.

## NAV3.4R — modelo de lectura y audiencia del subárbol `Conexiones` — 2026-10-15

NAV3.4R parte **únicamente** de `ACHInterbank-R34-NAV-IA-NAV3.4Q-FULL-PROJECT.zip`. No usa Git. `NAV2` permanece exactamente **`IMPLEMENTED_UNVERIFIED_ENVIRONMENT`** y no se declara `CLOSED` mientras la validación runtime .NET continúe pendiente.

### Estado previo de `Integrations.Read`

`FineGrainedPermissions.Integrations.Read = "Integrations.Read"` ya existía conceptualmente y **ya estaba incluida en `FineGrainedPermissions.AllPermissions`** antes de NAV3.4R. No existía una policy P1 de lectura de Integraciones, no había un `Permission` materializado por seeder para esa capability y no existían grants de role/usuario para ella.

NAV3.4R no crea una capability conceptual nueva. `IntegrationReadPermissionSeeder` materializa idempotentemente la existente como:

```text
Id          e6cfca32-17a9-45c0-a6f9-2a023317fbce
Name        Integrations.Read
Description Consultar configuración de integraciones
```

No crea `RolePermission`, roles ni grants directos a usuarios. `Integrations.Read` continúa **sin grants** en este incremento.

### Policy de lectura final

Se introduce:

```text
P1.IntegrationsRead
= Integrations.Read OR CanReadAch
```

`CanReadAch` permanece como fallback legacy. `CanManageAch`, `Config.Read`, `Config.Manage` y las capabilities mutables de Integraciones no satisfacen esta policy.

Las policies mutables cerradas en NAV3.4O/P/Q quedan intactas:

```text
P1.IntegrationContractsManage
= Integrations.ManageContracts OR CanManageAch

P1.IntegrationMappingsManage
= Integrations.ManageMappings OR CanManageAch

P1.IntegrationMappingsValidate
= Integrations.Validate OR CanManageAch

P1.IntegrationMappingsPublish
= Integrations.Publish OR CanManageAch

P1.IntegrationConnectionsManage
= Integrations.ManageConnections OR CanManageAch
```

No se retira ningún fallback `CanManageAch` y ninguna capability fina recibe grants.

### GET migrados

Todos los GET del dominio de Integraciones inspeccionados son exclusivos o compartidos únicamente entre las tres superficies `/integraciones/*`; no se encontró un consumidor MFT ni una superficie fuera del subárbol que obligara a conservar autorización directa diferente. La policy transicional mantiene la audiencia legacy por `CanReadAch`.

Se migran **13 GET** desde `CanReadAch` directo a `P1.IntegrationsRead`:

- `IntegrationCatalogController`: métodos, parámetros por método, catálogo fuente y transformaciones — 4 GET;
- `IntegrationContractsController`: versiones por método, detalle de versión y compare — 3 GET;
- `IntegrationMappingSetsController`: listado, published, detalle, workspace e history — 5 GET;
- `SoapIntegrationSettingsController`: lectura de configuración de conexión — 1 GET.

No queda un GET de estos cuatro controllers con `[Authorize(Policy = "CanReadAch")]`. No hay GET de Integraciones inspeccionado que haya quedado sin migrar por dependencia externa; las excepciones fuera de este conjunto no se tocaron porque no pertenecen al dominio target.

### Menú final de los tres hijos de Integraciones

Antes de NAV3.4R, NAV3.4O/P/Q habían dejado respectivamente:

```text
Contrato del servicio
  Role       Admin
  Permission CanReadAch

Relación de información
  Role       Admin
  Permission CanReadAch

Conexión entidad participante
  Role       Admin
  Permission CanReadAch
```

La SPA ya admitía `Admin OR ACH.Operator` en el parent y ambos roles sembrados ya poseen `CanReadAch`; por tanto ACH.Operator podía acceder por URL directa aunque no descubriera los tres MenuItems.

NAV3.4R aplica la decisión de audiencia aprobada y deja **los tres hijos** como:

```text
MenuItemRole
  Admin OR ACH.Operator

MenuItemPermission
  Integrations.Read OR CanReadAch

Efectivo
  (Admin OR ACH.Operator)
  AND
  (Integrations.Read OR CanReadAch)
```

No cambian Label, Route, ParentId, Order ni `IsActive`. La ampliación visual hacia ACH.Operator **no es un nuevo privilegio funcional en el estado sembrado**: ACH.Operator ya tenía el role admitido por el parent, `CanReadAch` y acceso directo a SPA/API antes de NAV3.4R. Se alinea descubribilidad con una audiencia funcional ya existente.

### Child guards SPA

Antes:

```text
/contrato                   CanReadAch
/relacion-informacion       CanReadAch
/relacion-informacion/:...  CanReadAch
/conexion                    CanReadAch
```

Final:

```text
Integrations.Read OR CanReadAch
```

para las cuatro rutas anteriores. `contract-workspace.store`, `integration-center.store` y `soap-integration-settings.component` reconocen la misma alternativa de lectura. Los checks mutables de Contrato, Mapping y Conexión permanecen exactamente bajo sus capabilities O/P/Q + `CanManageAch`.

### Parent SPA `/integraciones`

El role guard se conserva:

```text
Admin OR ACH.Operator
```

La permission del parent pasa de:

```text
CanReadAch OR CanManageAch
```

a:

```text
Integrations.Read OR CanReadAch OR CanManageAch
```

`CanReadAch` y `CanManageAch` permanecen temporalmente por compatibilidad. El parent **no** se convierte en capability-only en NAV3.4R.

Consecuencia transicional deliberada: un principal arbitrario con sólo `Integrations.Read` puede satisfacer los GET API bajo `P1.IntegrationsRead`, pero no atraviesa la SPA `/integraciones` porque no satisface `Admin OR ACH.Operator`. No hay grants de `Integrations.Read`, por lo que esto no amplía la población sembrada actual.

### Parent MenuItem `Conexiones`

Estado NAV3.4N previo:

```text
MenuItemRole       ninguno
MenuItemPermission CanManageUsers OR Config.Read OR CanReadAch
```

Después de alinear los tres hijos, `CanManageUsers` ya no conserva ningún hijo real del subárbol. NAV3.4R lo retira y añade la nueva lectura semántica:

```text
MenuItemRole       ninguno
MenuItemPermission Integrations.Read OR Config.Read OR CanReadAch
```

Esto elimina el edge documentado en NAV3.4N `CanManageUsers-only -> Conexiones vacío`. `Config.Read` conserva el parent para MFT; `CanReadAch` conserva compatibilidad legacy; `Integrations.Read` conserva el parent para los tres hijos de Integraciones. La visibilidad del parent no concede ningún child: cada child sigue pasando su propio `Role AND Permission` en `GetMenuForCurrentUserHandler`, que permanece byte a byte intacto.

Existe un edge transicional distinto y explícito: un principal **sin** Admin/ACH.Operator pero con `Integrations.Read` puede hacer visible el parent role-free `Conexiones` pero no sus tres children role-scoped. Esto no concede acceso y no afecta la población sembrada porque `Integrations.Read` no tiene grants. Eliminar por completo padres vacíos requeriría derivar visibilidad del parent desde children o retirar role gates después de una migración de grants; ambas decisiones quedan fuera de NAV3.4R.

### MFT intacto

`/administracion/mft-ach` no adopta `Integrations.Read` y conserva exactamente:

```text
MenuItem       Config.Read OR CanReadAch
SPA lectura    Config.Read OR CanReadAch
Mutaciones     Config.Manage OR CanManageAch
API            P1.ConfigRead / P1.ConfigManage
```

`IntegrationReadNavigationAudienceSeeder` no modifica el child MFT; sólo ajusta el parent estructural y los tres hijos `/integraciones/*`.

### Audiencia y semántica final

- Admin + `CanReadAch`: ve `Conexiones`, Contrato, Relación y Conexión y puede leer como antes.
- ACH.Operator + `CanReadAch`: ahora **también ve en menú** los tres hijos; ya podía entrar directamente antes.
- Admin/ACH.Operator + `Integrations.Read`: puede descubrir/entrar/leer las tres superficies si esa capability se otorga en el futuro.
- Admin/ACH.Operator sin `Integrations.Read` ni `CanReadAch`: el role por sí solo no habilita children.
- `Integrations.Read` sin Admin/ACH.Operator: satisface los GET API de lectura, pero no los child MenuItems ni el parent SPA role guard; no existen grants de ese estado.
- `CanReadAch` conserva la misma lectura legacy de Integraciones por fallback.
- `CanManageAch` conserva sólo los fallbacks mutables O/P/Q; no sustituye el guard hijo de lectura. El parent SPA conserva `CanManageAch` temporalmente, pero el child read guard impide operar una superficie completa sin capacidad de lectura.

No se encontró bypass de lectura hacia mutación: `Integrations.Read`/`CanReadAch` no satisfacen ManageContracts, ManageMappings, Validate, Publish ni ManageConnections.

### Regresión y aislamiento

NAV3.4R no modifica:

- `GetMenuForCurrentUserHandler`;
- tratamiento de `Conexiones` en `NavigationService` ni reintroduce `Centro de integración CFA`;
- capabilities/policies mutables de NAV3.4O/P/Q;
- MFT NAV3.4M/N;
- `TestTools.Access`/Herramientas de prueba;
- arquitectura NAV2, labels, routes, IDs, ParentIds, orders o `IsActive`.

Los validadores históricos M/N/O/P/Q se ajustan únicamente para reconocer la lectura transicional posterior NAV3.4R; conservan sus invariantes funcionales y de mutación.

### Estado de NAV3 y deuda residual

Con NAV3.4R queda resuelto a nivel diseño/código el drift coordinado de lectura y descubribilidad del subárbol `Conexiones`. La familia MFT/Integraciones queda semánticamente separada en lectura y mutación y la población canónica Admin/ACH.Operator tiene navegación coherente con el acceso funcional previo.

El trabajo funcional de normalización de autorización inventariado hasta NAV3.4R puede considerarse **completo a nivel diseño/código**, pero NAV3 no está listo para retirar compatibilidad ni declararse verificado. Permanecen como deuda explícita de migración/validación:

1. definir y otorgar grants de `Integrations.Read` a la población objetivo;
2. decidir grants de `Integrations.ManageContracts`, `Integrations.ManageMappings`, `Integrations.Validate`, `Integrations.Publish` e `Integrations.ManageConnections`;
3. validar runtime y, sólo después, retirar gradualmente fallbacks `CanReadAch`/`CanManageAch` y role gates transicionales;
4. deuda de grants ya registrada en otros gaps NAV3: Registros del sistema, Capacidades de la plataforma, `Returns.Read/Returns.GenerateFile` y `BulkIngestion.Upload`;
5. parent legacy `/transactions` para capabilities finas de ROR/Bulk, diferido explícitamente;
6. validaciones runtime .NET/Angular de NAV2/NAV3 aún pendientes por entorno.

El hardening/allowlist WSCFAACH documentado en NAV3.4Q es un gap de seguridad de transporte separado y no forma parte de la autorización NAV-MENU-IA de NAV3.4R.

**Recomendación siguiente:** opción **A — migración explícita de grants/fallbacks**, en un incremento separado y aprobado, antes de retirar role gates o avanzar a un refactor NAV4. No ejecutar automáticamente.

### Validaciones NAV3.4R

Validaciones disponibles ejecutadas sobre el workspace:

- solution/project graph: **PASS — 7/7**;
- Herramientas de prueba / NAV3.3B: **PASS — 39**;
- NAV3.4B: **PASS — 72**;
- NAV3.4C: **PASS — 87**;
- NAV3.4D: **PASS — 100**;
- NAV3.4E: **PASS — 154**;
- NAV3.4F: **PASS — 171**;
- NAV3.4G2: **PASS — 85**;
- NAV3.4H: **PASS — 94**;
- NAV3.4I: **PASS — 73**;
- NAV3.4J: **PASS — 145**;
- NAV3.4K: **PASS — 86**;
- NAV3.4L: **PASS — 124**;
- NAV3.4M: **PASS — 189**;
- NAV3.4N: **PASS — 96**;
- NAV3.4O: **PASS — 210**;
- NAV3.4P: **PASS — 190**;
- NAV3.4Q: **PASS — 177**;
- NAV3.4R focal: **PASS — 130**;
- parseo sintáctico focal TypeScript: **PASS — 10 archivos con TypeScript 5.8.3**.

Runtime:

- `.NET`: **NOT_RUN_ENVIRONMENT** — `dotnet` no está instalado/disponible;
- Angular build/test/runtime: **NOT_RUN_ENVIRONMENT** — Node 22.16.0, npm 10.9.2 y TypeScript 5.8.3 están disponibles, pero `web/ach-interbank-ui/node_modules` no existe.

No se inventan resultados runtime. `NAV2` continúa `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`.

## NAV3.5A — Fine-grained grant backfill — 2026-10-15

NAV3.5A parte **únicamente** de `ACHInterbank-R34-NAV-IA-NAV3.4R-FULL-PROJECT.zip` (`SHA-256 6c95ee4682835c028e7e06283fac5a6b8a054fc89b12e0044916baa55e722b2c`). No usa Git, no reabre gaps funcionales NAV3.4 y no ejecuta NAV3.5B.

`NAV2` permanece exactamente **`IMPLEMENTED_UNVERIFIED_ENVIRONMENT`**. No se marca `CLOSED` porque las pruebas runtime .NET continúan pendientes.

### Fuente de verdad de roles y fallbacks

El código canónico confirma exactamente dos roles sembrados:

```text
Admin
  1f8602da-6415-43f8-b61d-cb396f8577f1
  CanManageAch
  CanReadAch
  CanManageUsers
  CanManageCertificates

ACH.Operator
  a51746c2-0710-4d79-97b1-5b4368326f56
  CanManageAch
  CanReadAch
  NO CanManageUsers
  NO CanManageCertificates
```

No se encontraron otros roles canónicamente sembrados. No se infieren roles personalizados ni grants de bases desplegadas. La equivalencia NAV3.5A se garantiza exclusivamente para el estado sembrado del ZIP.

### Inventario cerrado del backfill

Se evaluaron exactamente **28 capabilities NAV3**. Todas existen en `FineGrainedPermissions`, están en `AllPermissions` y tienen materialización persistente con IDs estables. El inventario se limita a las capabilities creadas/materializadas o adoptadas por una policy de transición durante NAV3; no se agregan automáticamente otros permisos existentes del sistema.

`Nacha.Generate` y `Certificates.Activate/Validate/Audit` quedan fuera del target NAV3.5A: no fueron materializados/adoptados como capabilities target de esta migración NAV3.

### Resultado de equivalencia y grants

| Capability | Rol(es) con fine grant después de NAV3.5A | Fuente de equivalencia anterior | Decisión |
|---|---|---|---|
| `TestTools.Access` | Admin | decisión explícita NAV3.2A; grant ya existente | GRANT existente, 0 filas nuevas |
| `Transactions.Create` | Admin, ACH.Operator | `CanManageAch` | GRANT |
| `Nacha.Upload` | Admin, ACH.Operator | `CanManageAch` | GRANT |
| `Returns.Read` | Admin, ACH.Operator | `CanReadAch` | GRANT |
| `Returns.GenerateFile` | Admin, ACH.Operator | `CanManageAch` | GRANT |
| `BulkIngestion.Read` | Admin, ACH.Operator | `CanReadAch` | GRANT |
| `BulkIngestion.Upload` | Admin, ACH.Operator | `CanManageAch` | GRANT |
| `BulkIngestion.Retry` | Admin, ACH.Operator | `CanManageAch` | GRANT |
| `BulkIngestion.Cancel` | Admin, ACH.Operator | `CanManageAch` | GRANT |
| `Certificates.Read` | Admin, ACH.Operator | `CanManageCertificates OR CanReadAch` | GRANT |
| `Certificates.UploadPublic` | Admin | `CanManageCertificates` | GRANT |
| `Certificates.RegisterPrivate` | Admin | `CanManageCertificates` | GRANT |
| `Certificates.Revoke` | Admin | `CanManageCertificates` | GRANT |
| `DigitalEnvelope.Encrypt` | Admin, ACH.Operator | `CanManageAch` | GRANT |
| `DigitalEnvelope.Decrypt` | Admin, ACH.Operator | `CanManageAch` | GRANT |
| `AuditLogs.Read` | Admin | `CanManageUsers` | GRANT |
| `AuthLogs.Read` | Admin | `CanManageUsers` | GRANT |
| `NavigationLogs.Read` | Admin, ACH.Operator | `CanReadAch` | GRANT |
| `NavigationMenu.Manage` | Admin | `CanManageUsers` | GRANT |
| `CanViewPaymentRailCapabilityRegistry` | Admin, ACH.Operator | `CanManageAch OR CanReadAch` | GRANT |
| `Config.Read` | Admin, ACH.Operator | `CanReadAch` | GRANT |
| `Config.Manage` | Admin, ACH.Operator | `CanManageAch` | GRANT |
| `Integrations.Read` | Admin, ACH.Operator | `CanReadAch` | GRANT |
| `Integrations.ManageContracts` | Admin, ACH.Operator | `CanManageAch` | GRANT |
| `Integrations.ManageMappings` | Admin, ACH.Operator | `CanManageAch` | GRANT |
| `Integrations.Validate` | Admin, ACH.Operator | `CanManageAch` | GRANT |
| `Integrations.Publish` | Admin, ACH.Operator | `CanManageAch` | GRANT |
| `Integrations.ManageConnections` | Admin, ACH.Operator | `CanManageAch` | GRANT |

No queda una capability completa en SKIP para la población canónica. Hay **7 exclusiones role-capability deliberadas de ACH.Operator**: `TestTools.Access`, `Certificates.UploadPublic`, `Certificates.RegisterPrivate`, `Certificates.Revoke`, `AuditLogs.Read`, `AuthLogs.Read` y `NavigationMenu.Manage`. Son exclusiones demostradas, no casos ambiguos: TestTools es Admin-only por decisión explícita y las otras seis carecen del fallback legacy requerido en ACH.Operator.

### Seeder NAV3.5A

Se agrega:

```text
FineGrainedGrantBackfillSeeder
Order = 119
```

Se ejecuta después de los materializadores de capabilities target (`Order 0`), del grant ya existente `Admin -> TestTools.Access` (`Order 1`) y de los reconciliadores de navegación NAV3 (`101..118`). `Order 119` es único entre los seeders de seguridad.

Primer run esperado:

```text
Admin         27 RolePermission nuevos
ACH.Operator  21 RolePermission nuevos
TOTAL         48 RolePermission nuevos
```

`Admin -> TestTools.Access` ya existía y no se vuelve a crear. El seeder localiza `Role` por ID estable y valida nombre, localiza `Permission` por `Name`, falla cerrado si falta una capability target, agrega sólo pares ausentes y no elimina filas. No modifica `Role`, `Permission`, `UserRole`, usuarios ni existe un `UserPermission` directo en `AchDbContext`.

### Audiencia before/after

Para cada uno de los 48 grants nuevos, el rol ya satisfacía antes la policy correspondiente mediante el fallback legacy. Después del backfill sigue satisfaciendo la **misma** policy, ahora por fine capability y por el fallback todavía presente.

Por tanto, para los dos roles canónicos:

- no se amplía autorización backend efectiva;
- no se reduce autorización backend efectiva;
- no se crean nuevas superficies de menú/SPA;
- no se retira ningún role gate;
- no se modifica ningún parent route guard;
- no se cambia ninguna policy P0/P1;
- no se retira ningún fallback.

El parent `/transactions` continúa siendo un prerequisito legacy que puede impedir el recorrido SPA de un principal futuro que tenga una capability fina aislada pero no satisfaga el role/permission gate del parent. Los role gates de `/integraciones` continúan siendo transicionales: los grants a `Integrations.*` no los eliminan.

### Archivos de implementación NAV3.5A

- `src/Cfa.ACHInterbank.Persistence/Security/Seeders/FineGrainedGrantBackfillSeeder.cs` — nuevo seeder focal, `Order 119`.
- `tests/Cfa.ACHInterbank.Tests/NavigationMenuNav35AFineGrainedGrantBackfillTests.cs` — pruebas runtime .NET focales para inventario, equivalencia before/after, idempotencia, roles y exclusiones.
- `scripts/validate-nav3-5a-fine-grained-grant-backfill.py` — validador estático focal, incluyendo hash-lock de policies, navegación, guards, roles y componentes de filtrado no modificados.
- `docs/security/NAVIGATION_MENU_SECURITY_MODEL.md` — matriz completa de equivalencia y estrategia de migración.
- `docs/ai/navigation-menu/MEMORY_STATE.md` y `MEMORY.md` — estado/hallazgos durables.

### Validaciones NAV3.5A

Validaciones estáticas ejecutadas sobre el workspace NAV3.5A:

- grafo de solución/proyectos: **PASS — 7/7**;
- Herramientas de prueba / NAV3.3B: **PASS — 39**;
- NAV3.4B: **PASS — 72**;
- NAV3.4C: **PASS — 87**;
- NAV3.4D: **PASS — 100**;
- NAV3.4E: **PASS — 154**;
- NAV3.4F: **PASS — 171**;
- NAV3.4G2: **PASS — 85**;
- NAV3.4H: **PASS — 94**;
- NAV3.4I: **PASS — 73**;
- NAV3.4J: **PASS — 145**;
- NAV3.4K: **PASS — 86**;
- NAV3.4L: **PASS — 124**;
- NAV3.4M: **PASS — 189**;
- NAV3.4N: **PASS — 96**;
- NAV3.4O: **PASS — 210**;
- NAV3.4P: **PASS — 190**;
- NAV3.4Q: **PASS — 177**;
- NAV3.4R: **PASS — 130**;
- NAV3.5A focal: **PASS — 259**.

El validador NAV3.5A comprueba inventario/IDs, `AllPermissions`, materialización, roles/fallbacks canónicos, 48 grants declarados, 7 exclusiones de ACH.Operator, idempotencia estructural, TestTools Admin-only, ausencia de mutaciones a usuarios/roles/permisos, preservación de fallbacks y hash-lock de policies, menú/guards SPA, `NavigationService`, `MainLayout` y `GetMenuForCurrentUserHandler` contra NAV3.4R.

### Estado de runtime

- `.NET`: **NOT_RUN_ENVIRONMENT** — `dotnet` no está instalado/disponible.
- Angular build/test/runtime: **NOT_RUN_ENVIRONMENT** — Node `22.16.0`, npm `10.9.2` y TypeScript `5.8.3` están disponibles, pero `web/ach-interbank-ui/node_modules` no existe.

No se inventan resultados runtime. La prueba .NET focal queda incorporada para ejecutarse cuando el entorno lo permita.

## DECISIÓN REQUERIDA

- **Capabilities evaluadas:** 28 exactas.
- **Grants finos nuevos agregados por NAV3.5A:** 48 `RolePermission` exactos.
- **Roles que reciben grants nuevos:** `Admin` (27) y `ACH.Operator` (21).
- **Grant previo preservado:** `Admin -> TestTools.Access`; no se duplica.
- **Matriz resumida capability -> roles:** la tabla anterior es la fuente de decisión; Admin queda con las 28 capabilities target y ACH.Operator con 21.
- **Capabilities SKIP a nivel capability:** 0.
- **SKIP role-capability de ACH.Operator:** 7 — `TestTools.Access` por decisión Admin-only; `Certificates.UploadPublic`, `Certificates.RegisterPrivate`, `Certificates.Revoke` porque ACH.Operator no posee `CanManageCertificates`; `AuditLogs.Read`, `AuthLogs.Read`, `NavigationMenu.Manage` porque ACH.Operator no posee `CanManageUsers`.
- **TestTools.Access:** continúa **Admin-only**.
- **Permisos directos a usuarios:** ninguno; no se crea `UserPermission`.
- **Nuevos roles:** ninguno.
- **Role gates retirados:** ninguno.
- **Fallbacks retirados:** ninguno.
- **Policies P0/P1 cambiadas:** ninguna; el hash de `DependencyInjectionService.cs`, `P0Policies.cs` y `P1Policies.cs` permanece igual al ZIP NAV3.4R.
- **Audiencia efectiva sembrada:** no cambia; los 48 grants sólo duplican capacidades ya ejercibles por fallback.
- **Divergencia potencial en bases desplegadas/customizadas:** sí. Roles/grants no representados por los seeds canónicos pueden depender exclusivamente de fallbacks legacy y deben inventariarse antes de NAV3.5B. NAV3.5A no pretende cubrirlos.
- **NAV3.5A diseño/código:** puede considerarse completo, sujeto a la validación runtime .NET/Angular diferida por entorno.
- **Cobertura para iniciar NAV3.5B:** suficiente **para diseñar/evaluar** un retiro controlado sobre la población canónica, pero no para retirar automáticamente compatibilidad. NAV3.5B debe volver a considerar bases desplegadas/customizadas y ejecutar runtime cuando esté disponible.
- **Fallbacks candidatos concretos a evaluación en NAV3.5B:** `CanManageUsers` en `P1.AuditLogsRead`, `P1.AuthLogsRead`, `P1.NavigationMenuManage`; `CanManageCertificates` en `P1.CertificatesUploadPublic`, `P1.CertificatesRegisterPrivate`, `P1.CertificatesRevoke`; `CanManageAch` en las policies mutables target (`Transactions.Create`, `Nacha.Upload`, `Returns.GenerateFile`, Bulk upload/retry/cancel, DigitalEnvelope encrypt/decrypt, `Config.Manage` e Integrations manage/validate/publish); y, con mayor cautela, los `CanReadAch` de las policies read-only target.
- **NO retirar todavía:** parent/guards legacy de `/transactions`; role gates de `/integraciones`; dependencias `CanReadAch` de selectores/lecturas compartidas de Certificados y entrada de Sobre digital; cualquier fallback requerido por poblaciones desplegadas no inventariadas; ningún permiso legacy a nivel de rol de forma global.
- **Parent `/transactions`:** sí, sigue pudiendo bloquear el uso SPA de una fine capability aislada aunque el backend la acepte; ambos roles canónicos siguen atravesándolo por sus grants legacy actuales.
- **Role gates `/integraciones`:** sí, siguen siendo transicionales y permanecen intactos.
- **.NET:** `NOT_RUN_ENVIRONMENT`.
- **Angular:** `NOT_RUN_ENVIRONMENT`.

No ejecutar NAV3.5B, no retirar fallbacks/role gates y no avanzar a NAV4 sin decisión explícita del usuario.

## NAV3.5B — Deployment-safe legacy permission migration — 2026-10-15

NAV3.5B parte **únicamente** de `ACHInterbank-R34-NAV-IA-NAV3.5A-FULL-PROJECT.zip` (`SHA-256 11d9a65d4fd5633f4f202fed51c9c3449db8854a9dd2a8bbd1faf0df8558b9ce`). No usa Git, no modifica el alcance funcional de NAV3.4/NAV3.5A y **NO ejecuta NAV3.5C ni NAV4**.

`NAV2` permanece exactamente **`IMPLEMENTED_UNVERIFIED_ENVIRONMENT`**. NAV3.4 permanece funcionalmente implementado a nivel diseño/código hasta NAV3.4R. NAV3.5A permanece como backfill de capabilities finas para los roles canónicos sembrados.

### Modelo efectivo de permisos

La autorización persistida vigente es role-based:

```text
User
  -> UserRole
  -> Role
  -> RolePermission
  -> Permission
  -> AuthService materializa claim "permission" en JWT
```

`AchDbContext` no expone `UserPermission`, no existe `UserPermissionConfiguration` ni se encontró otra entidad persistida equivalente para otorgar permisos directamente a un usuario. `UserAuthRepository` hidrata `UserRole -> Role -> RolePermission -> Permission`; `AuthService.BuildAuthResult` construye los permission claims únicamente desde esos RolePermissions. Por tanto, la clasificación vigente es **A: FineGrainedPermission sólo mediante RolePermission en persistencia**.

El JWT sí contiene permission claims, pero son una instantánea emitida al login/refresh. Un token emitido **antes** del backfill no adquiere automáticamente los nuevos fine grants. Antes de retirar cualquier fallback en un entorno desplegado será necesario que NAV3.5B haya corrido contra la DB objetivo y que los tokens activos se renueven/expiren.

### Seeder NAV3.5B

Se agrega:

```text
LegacyFineGrainedRoleMigrationSeeder
Order = 120
```

Coexiste después de `FineGrainedGrantBackfillSeeder` de NAV3.5A (`Order 119`) sin reescribirlo. El Order 120 es lógico, determinista y único dentro de los `IDbSeeder` inspeccionados.

El seeder es data-driven y opera sobre **todos los RolePermission legacy existentes en DB**. No depende de nombres ni IDs de roles, no crea roles/usuarios/UserRole/Permission, no elimina filas y sólo agrega pares `(RoleId, FinePermissionId)` ausentes. Si dos legacy fallbacks conducen a la misma fine capability, la unión se realiza sin duplicados. Un segundo run agrega 0 filas.

`TestTools.Access` está expresamente fuera del mapping genérico. La decisión existente `Admin -> TestTools.Access` continúa en el seeder dedicado previo; `ACH.Operator` y roles custom con `CanManageAch`/`CanManageUsers` no reciben TestTools automáticamente.

### Mapping final legacy → fine derivado de policies vigentes

El mapping contiene **29 edges** y **27 fine capabilities únicas**:

| Legacy permission | Fine capabilities derivadas | Edges |
|---|---|---:|
| `CanReadAch` | `Returns.Read`; `BulkIngestion.Read`; `Certificates.Read`; `NavigationLogs.Read`; `CanViewPaymentRailCapabilityRegistry`; `Config.Read`; `Integrations.Read` | 7 |
| `CanManageAch` | `Transactions.Create`; `Nacha.Upload`; `Returns.GenerateFile`; `BulkIngestion.Upload`; `BulkIngestion.Retry`; `BulkIngestion.Cancel`; `DigitalEnvelope.Encrypt`; `DigitalEnvelope.Decrypt`; `CanViewPaymentRailCapabilityRegistry`; `Config.Manage`; `Integrations.ManageContracts`; `Integrations.ManageMappings`; `Integrations.Validate`; `Integrations.Publish`; `Integrations.ManageConnections` | 15 |
| `CanManageUsers` | `AuditLogs.Read`; `AuthLogs.Read`; `NavigationMenu.Manage` | 3 |
| `CanManageCertificates` | `Certificates.Read`; `Certificates.UploadPublic`; `Certificates.RegisterPrivate`; `Certificates.Revoke` | 4 |

`Certificates.Read` tiene correctamente dos edges (`CanReadAch`, `CanManageCertificates`) y `CanViewPaymentRailCapabilityRegistry` tiene dos (`CanReadAch`, `CanManageAch`). No se agrega ninguna relación que no esté demostrada por una policy transicional vigente.

### Resultado esperado por población

Sobre la DB canónica inmediatamente posterior a NAV3.5A, el primer run de NAV3.5B debe insertar **0 RolePermission**: NAV3.5A ya dejó a Admin y ACH.Operator con exactamente las fine capabilities derivables de sus fallbacks.

En roles sintéticos/custom existentes:

```text
CustomReadRole + CanReadAch
  -> 7 fine grants exactos

CustomManageRole + CanManageAch
  -> 15 fine grants exactos

CustomUserAdminRole + CanManageUsers
  -> AuditLogs.Read + AuthLogs.Read + NavigationMenu.Manage

CustomCertificateRole + CanManageCertificates
  -> Certificates.Read + Certificates.UploadPublic
     + Certificates.RegisterPrivate + Certificates.Revoke

Role con los cuatro fallbacks target
  -> unión de 27 fine capabilities únicas

Role sin fallback target
  -> 0 fine grants

Segundo run
  -> 0 inserts adicionales
```

La audiencia backend efectiva no cambia: cada fine grant agregado al rol custom es consecuencia directa de un fallback que el mismo rol ya poseía y que continúa en la policy.

### Permisos directos y riesgo de bases desplegadas

No existe un mecanismo persistido de direct-user permissions que deba migrarse y NAV3.5B no crea uno. Por ello no hay un blocker específico por filas `UserPermission` de esta aplicación.

Sí permanece riesgo operacional para bases customizadas:

- NAV3.5B migra el estado real presente **cuando el seeder se ejecuta**;
- los cuatro legacy permissions permanecen en el catálogo `Permission` y no se desactivan;
- `RolesController` y `PermissionsController` inspeccionados no exponen una mutación first-party de RolePermission, pero una DB desplegada puede tener datos históricos/custom o escritura out-of-band no representada por el ZIP;
- si después del backfill y de un eventual retiro se crea por fuera de este flujo un rol nuevo con sólo un legacy permission, ese rol no tendrá automáticamente su fine grant salvo que el migrador vuelva a ejecutarse o se gobierne la asignación legacy en un incremento posterior.

NAV3.5B no elimina el catálogo legacy ni toma esa decisión de gobierno.

### Readiness de fallbacks

| Legacy fallback | Fine capabilities dependientes NAV3.5B | Migración dinámica de roles | Direct-user risk | Parent/SPA / dependencia adicional | Runtime | Estado |
|---|---:|---|---|---|---|---|
| `CanManageUsers` | 3 | Sí | No persistido | guards legacy del módulo administrativo/logs fuera del mapping | DB real + refresh/expiry JWT + runtime pendientes | **BLOCKED** |
| `CanManageCertificates` | 4 | Sí | No persistido | `Certificates.Activate/Validate/Audit` aún usan este fallback y no están en NAV3.5A/B | DB real + refresh/expiry JWT + runtime pendientes | **BLOCKED** |
| `CanReadAch` | 7 | Sí | No persistido | `/transactions`, `/integraciones`, selectores de certificados, cámaras operativas, entrada de Sobre digital y muchos usos directos fuera del mapping | DB real + refresh/expiry JWT + runtime pendientes | **BLOCKED** |
| `CanManageAch` | 15 | Sí | No persistido | `/transactions`, `/integraciones` y numerosos usos directos fuera del mapping | DB real + refresh/expiry JWT + runtime pendientes | **BLOCKED** |

Ningún fallback se considera `READY_FOR_NAV3_5C` en este estado.

### Dependencia `/transactions`

El parent SPA continúa sin cambios:

```text
roles: Admin OR ACH.Operator
permissions: CanManageAch OR CanReadAch
```

Por tanto, las fine capabilities `Transactions.Create`, `Nacha.Upload`, `BulkIngestion.Read`, `BulkIngestion.Upload`, `BulkIngestion.Retry`, `BulkIngestion.Cancel`, `Returns.Read` y `Returns.GenerateFile` siguen restringidas en SPA por el parent aunque sus grants finos existan. Además, `/transactions/returns` sigue exigiendo directamente `CanReadAch`.

Retirar fallbacks backend sin normalizar el parent no quitaría acceso inmediato a Admin/ACH.Operator mientras conserven sus legacy grants, pero produciría una inconsistencia arquitectónica para principals fine-only: backend capability-ready y SPA todavía legacy/role-gated. NAV3.5B no corrige el parent.

### Dependencia `/integraciones`

El parent sigue:

```text
(Admin OR ACH.Operator)
AND
(Integrations.Read OR CanReadAch OR CanManageAch)
```

Los children siguen con `Admin OR ACH.Operator` más `Integrations.Read OR CanReadAch`, y las mutaciones conservan sus capabilities `Integrations.Manage*`/`Validate`/`Publish` con fallback `CanManageAch`.

Antes de retirar `CanReadAch`/`CanManageAch` aquí se requiere: backfill ejecutado en DB objetivo, renovación/expiración de tokens, runtime y resolución coordinada de los guards legacy. Antes de retirar los role gates hace falta una decisión humana explícita sobre audiencia capability-only. NAV3.5B no retira ninguno.

### Certificados / Sobre digital / cámaras

`CanReadAch` continúa siendo una dependencia real fuera de los RolePermissions migrados:

- `NachaCertificateManagerComponent` condiciona carga de cámaras/instituciones auxiliares por `CanReadAch`;
- `GET api/clearing-houses/operational` permanece bajo `CanReadAch`;
- `GET financial-institutions` permanece bajo `CanReadAch`;
- la entrada SPA `/nacha-security/sobre-digital` permanece bajo `CanReadAch`;
- el upload de certificados mantiene gating transicional adicional.

Esto bloquea retirar `CanReadAch` globalmente aun después del backfill de roles.

### Archivos NAV3.5B

- `src/Cfa.ACHInterbank.Persistence/Security/Seeders/LegacyFineGrainedRoleMigrationSeeder.cs` — migrador genérico, `Order 120`.
- `tests/Cfa.ACHInterbank.Tests/NavigationMenuNav35BDeploymentSafeLegacyPermissionMigrationTests.cs` — pruebas focales de roles custom, unión, no-target, canonical zero-insert, audiencia e idempotencia.
- `scripts/validate-nav3-5b-deployment-safe-legacy-permission-migration.py` — validador estático de mapping, policies, modelo de autorización, ordering y regresión hash-lock contra NAV3.5A.
- `docs/security/NAVIGATION_MENU_SECURITY_MODEL.md` — modelo, mapping y matriz de readiness.
- `docs/ai/navigation-menu/MEMORY_STATE.md` y `MEMORY.md` — estado/hallazgos durables.

### Validación y estado runtime

Validaciones estáticas ejecutadas sobre el workspace NAV3.5B:

- grafo de solución/proyectos: **PASS — 7/7**;
- Herramientas de prueba / NAV3.3B: **PASS — 39**;
- NAV3.4B: **PASS — 72**; NAV3.4C: **87**; NAV3.4D: **100**; NAV3.4E: **154**; NAV3.4F: **171**;
- NAV3.4G2: **PASS — 85**; NAV3.4H: **94**; NAV3.4I: **73**; NAV3.4J: **145**; NAV3.4K: **86**; NAV3.4L: **124**;
- NAV3.4M: **PASS — 189**; NAV3.4N: **96**; NAV3.4O: **210**; NAV3.4P: **190**; NAV3.4Q: **177**; NAV3.4R: **130**;
- NAV3.5A focal: **PASS — 259**;
- NAV3.5B focal: **PASS — 3371**.

Las pruebas .NET focales quedan incorporadas para ejecución en un ambiente con SDK.

- `.NET`: **NOT_RUN_ENVIRONMENT** — `dotnet` no está instalado/disponible.
- Angular build/test/runtime: **NOT_RUN_ENVIRONMENT** — Node/npm existen, pero `web/ach-interbank-ui/node_modules` no existe.

No se inventan resultados runtime. No se modificó SPA, por lo que la regresión SPA del incremento se valida mediante hash-lock contra el ZIP NAV3.5A.

## DECISIÓN REQUERIDA — NAV3.5B

- **Mecanismos que pueden otorgar permissions a un usuario:** persistencia únicamente `UserRole -> Role -> RolePermission -> Permission`; `AuthService` materializa esos permisos como claims `permission` en el JWT.
- **Permisos directos a usuario persistidos:** no existe `UserPermission`/configuration/equivalente efectivo en el modelo inspeccionado.
- **¿Direct-user bloquea retirar fallbacks?:** no por persistencia direct-user; **sí existe** un blocker de sesión porque JWT ya emitidos no incorporan los fine grants hasta refresh/login/expiración.
- **Seeder NAV3.5B:** `LegacyFineGrainedRoleMigrationSeeder`, **Order 120**.
- **Mappings legacy→fine:** 29 edges exactos.
- **Fine capabilities únicas cubiertas:** 27.
- **`TestTools.Access`:** excluido del migrador genérico; continúa Admin-only.
- **Inserts esperados en base canónica post-NAV3.5A:** 0.
- **Custom `CanReadAch`:** 7 fine grants exactos.
- **Custom `CanManageAch`:** 15 fine grants exactos.
- **Custom `CanManageUsers`:** 3 fine grants exactos (`AuditLogs.Read`, `AuthLogs.Read`, `NavigationMenu.Manage`).
- **Custom `CanManageCertificates`:** 4 fine grants exactos (`Certificates.Read`, `Certificates.UploadPublic`, `Certificates.RegisterPrivate`, `Certificates.Revoke`).
- **Idempotencia:** sí; segundo run 0 inserts adicionales y los edges múltiples se unen sin duplicados.
- **Policies cambiadas:** ninguna.
- **Fallbacks retirados:** ninguno.
- **Role gates retirados:** ninguno.
- **`CanManageUsers`:** **BLOCKED** por runtime/sesión y usages SPA legacy fuera del mapping focal.
- **`CanManageCertificates`:** **BLOCKED** porque `Activate/Validate/Audit` aún dependen del legacy fallback y por runtime/sesión pendiente.
- **`CanReadAch`:** **BLOCKED** por parent `/transactions`, `/integraciones`, Certificados/Sobre digital/cámaras, usos directos transversales y runtime/sesión.
- **`CanManageAch`:** **BLOCKED** por parent `/transactions`, `/integraciones`, usos directos transversales y runtime/sesión.
- **Impacto `/transactions`:** sigue impidiendo el recorrido SPA capability-only de varias fine capabilities aunque el backend ya tenga policy de transición.
- **Impacto `/integraciones`:** los role gates `Admin/ACH.Operator` y fallbacks read/manage siguen siendo transicionales; no se retiraron.
- **Impacto Certificados/Sobre digital:** `CanReadAch` continúa requerido por selectores/endpoints auxiliares y entrada SPA; bloquea retiro global.
- **Riesgo residual custom DB:** el seeder cubre roles existentes al ejecutarse, pero legacy Permission permanece catalogado y escrituras fuera de la aplicación o roles legacy-only creados después requieren gobierno/rerun antes de retirar compatibilidad.
- **NAV3.5B diseño/código:** puede considerarse completo sujeto a runtime diferido; no autoriza retiro de fallbacks.
- **Recomendación exacta:** **C — detener retiro de fallbacks hasta runtime**. Después de runtime todavía deben resolverse los blockers estructurales anteriores antes de un retiro global.
- **.NET:** `NOT_RUN_ENVIRONMENT`.
- **Angular:** `NOT_RUN_ENVIRONMENT`.

**NO ejecutar NAV3.5C. NO avanzar a NAV4. Esperar decisión explícita del usuario.**


## NAV4.1A — Fidelidad del árbol de navegación backend en NavigationService — 2026-10-15

NAV4.1A parte únicamente de `ACHInterbank-R34-NAV-IA-NAV3.5B-FULL-PROJECT.zip`, SHA-256 `9f7ae179a7f6f46607e6a90a76bd8039bc614b577df562a1591cb1b49227edbd`, sin Git. NAV2 y NAV3 permanecen exactamente `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`; NAV3.5A/B quedan intactos; NAV3.5C permanece **BLOCKED / NO EJECUTAR**.

### Flujo inspeccionado

```text
GET api/navigation/menu
-> ApiService.get<MenuItem[]>()
-> NavigationService.getMenu()
-> MenuItem
-> MainLayoutComponent.menuItems
-> template recursivo de MainLayout
```

`NavigationController` entrega `MenuItemDto` producido por `GetMenuForCurrentUserHandler`; NAV4.1A no modifica ese handler ni su promoción backend cuando un parent no está visible. `MainLayout` sólo fue inspeccionado para caracterizar el consumo.

### Inventario de transforms de NavigationService al iniciar

1. `removeRetiredMenuItems`: filtraba recursivamente nodos por `route` usando `LEGACY_NACHA_ROUTES` y `RETIRED_FRONTEND_ROUTES`. **Modificaba IA** porque podía eliminar nodos/children entregados por backend. **REMOVE**.
2. `normalizeRoute`: hacía `trim`, lowercase y removía slash final para soportar el filtro anterior. No reescribía la route emitida, pero habilitaba la comparación hardcodeada. **REMOVE** junto con el filtro.
3. `removeRetiredMenuItems` rematerializaba hojas como `children: []`. Era una mutación de forma innecesaria. **REMOVE** junto con el filtro.
4. `sortMenu`: ordenaba recursivamente siblings por `order` backend. No usa route/label ni cambia el valor de `order`. **KEEP**.
5. `sortMenu` también rematerializaba hojas como `children: []`. **REMOVE**; ahora conserva `children` omitido y sólo recurre cuando existe.

No existían al inicio de NAV4.1A otros maps de labels, reemplazo de nodos, creación manual de items, flattening, deduplicación, reparenting, mutación de `exact`, mutación de `icon` ni sustitución de route. El override antiguo `Centro de integración CFA` ya estaba ausente desde NAV3.4N.

### Resultado de código

`NavigationService` queda limitado a:

- fetch de `api/navigation/menu`;
- ordenación recursiva por `order` del backend;
- copia recursiva que preserva `label`, `route`, `exact`, `icon`, `order`, parent/children y profundidad.

Se eliminaron los sets de rutas y el filtro frontend. No existe ya comparación por `Route` o `Label` dentro de `NavigationService`. Un child nuevo/desconocido entregado por backend se preserva sin cambio Angular.

Regresión `Conexiones`: el servicio preserva exactamente `Contrato del servicio`, `Relación de información`, `Conexión entidad participante` y `Configuración del intercambio de archivos`, ordenados por sus `order` backend. `Centro de integración CFA` no reaparece y no existe adaptación especial que produzca `children: []`.

### MainLayout / titles / breadcrumbs

`MainLayout` permanece byte a byte intacto. Un nodo con children sigue renderizándose como botón expander y no como link de navegación del parent; `/reports` no se corrige en NAV4.1A. `buildBreadcrumbs`, route titles, `document.title`, headings y copy de componentes permanecen fuera de alcance.

### Seguridad y seed

Verificación hash contra el ZIP NAV3.5B confirma sin cambios:

- `GetMenuForCurrentUserHandler`;
- todos los seeders de navegación, incluido `NavigationInformationArchitectureSeeder`;
- todos los seeders de seguridad, incluidos `TestToolsMenuAuthorizationSeeder`, `FineGrainedGrantBackfillSeeder` Order 119 y `LegacyFineGrainedRoleMigrationSeeder` Order 120;
- `P0Policies.cs` y `P1Policies.cs`;
- guards Angular bajo `core/guards`;
- `MainLayout` y `MenuItem` model;
- `ApiService`.

No se retiró ningún fallback, role gate, grant, MenuItemRole/MenuItemPermission, policy o component permission gate.

### Pruebas / validaciones

Ejecutadas:

- SHA-256 del input: **PASS**, coincide exactamente con `9f7ae179a7f6f46607e6a90a76bd8039bc614b577df562a1591cb1b49227edbd`.
- TypeScript 5.8.3 parser/transpile sintáctico de `navigation.service.ts` y `navigation.service.spec.ts`: **PASS**.
- `python scripts/validate-nav4-1a-navigation-service.py`: **PASS — 41 aserciones**.
- `python scripts/validate-solution-projects.py`: **PASS — 7/7 proyectos**.
- NAV3.3B static validator: **PASS — 39**.
- NAV3.4B/C/D/E/F: **PASS — 72/87/100/154/171**.
- NAV3.4G2/H/I/J/K/L/M: **PASS — 85/94/73/145/86/124/189**.
- El validador histórico NAV3.4G no es ejecutable en el estado vigente porque referencia un test renombrado/no presente (`NavigationMenuNav34GSystemRecordsAuthorizationTests.cs`); se usa NAV3.4G2 como reemplazo vigente.
- Los validadores históricos NAV3.4N–R y NAV3.5A/B contienen hash-lock explícito del `NavigationService` pre-NAV4 y por diseño dejan de ser aplicables después del cambio autorizado de NAV4.1A. Su código y superficies protegidas permanecen byte a byte intactos según comparación de alcance/hash y el nuevo validador NAV4.1A.

Diferidas:

- `.NET`: **NOT_RUN_ENVIRONMENT** — `dotnet` no está instalado/disponible.
- Angular build/test/runtime: **NOT_RUN_ENVIRONMENT** — Node/npm están presentes, pero `web/ach-interbank-ui/node_modules` está ausente.

### Estado y siguiente incremento

NAV4.1A puede considerarse **implementado a nivel diseño/código**, con runtime Angular/.NET diferido por ambiente. Los gaps SPA restantes identificados son, como mínimo, el comportamiento de parents con children, títulos/breadcrumbs/copy y cobertura de rutas posteriores de NAV4/NAV5.

El siguiente incremento natural para revisión humana es **NAV4.1B — comportamiento de navegación de padres con children**, incluyendo decisión explícita para parents con route funcional, especialmente `/reports`. **NO ejecutar NAV4.1B automáticamente.**

## DECISIÓN REQUERIDA — NAV4.1A

- **Transformaciones especiales al iniciar:** 3 mecanismos explícitos (`removeRetiredMenuItems`, `normalizeRoute`, `sortMenu`), con dos efectos auxiliares de rematerialización `children: []` dentro de los transforms recursivos.
- **Transformaciones que modificaban IA:** el filtro recursivo por route eliminaba nodos/children backend; la rematerialización `children: []` alteraba forma de hojas.
- **Eliminadas:** filtro por rutas, sets hardcodeados, normalización de route asociada al filtro y fabricación de `children: []`.
- **Permanece:** ordenación recursiva por `order` backend únicamente; se conserva porque materializa el Order ya canónico sin inventar orden semántico.
- **Label hardcodeado que reemplace backend en NavigationService:** no.
- **Route hardcodeada que reconstruya el árbol:** no.
- **Transformación que elimine children:** no.
- **Reparenting Angular:** no.
- **Order backend:** sí, se conserva y siblings se ordenan recursivamente sólo por ese campo.
- **Child backend nuevo/desconocido:** sí, se preserva sin regla Angular.
- **Conexiones:** preserva exactamente los cuatro hijos canónicos recibidos del backend.
- **Centro de integración CFA:** no reapareció.
- **Seguridad NAV3:** intacta; no se tocaron fallbacks, roles, grants, policies, guards ni handler backend.
- **NAV3.5A/B:** intactos; Order 119/120 permanecen.
- **NAV4.1A diseño/código:** implementado.
- **Gaps SPA restantes:** navegación de padres con children; `/reports`; titles/breadcrumbs; copy visible; cobertura de rutas posterior.
- **Siguiente incremento recomendado:** `NAV4.1B — comportamiento de navegación de padres con children`, no ejecutar sin decisión humana.
- **.NET:** `NOT_RUN_ENVIRONMENT`.
- **Angular:** `NOT_RUN_ENVIRONMENT`.

## NAV4.1B — Comportamiento de navegación de padres con children — 2026-10-15

NAV4.1B parte únicamente de `ACHInterbank-R34-NAV-IA-NAV4.1A-FULL-PROJECT.zip`, SHA-256 `7d0d1c242d4e03e5d9e04af7a6ee8c97af4a10087d3a1b450dd170a0c06cc5ab`, sin Git. NAV2 y NAV3 permanecen exactamente `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`; NAV4.1A permanece `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`; NAV3.5C continúa **BLOCKED / NO EJECUTAR**.

### Hallazgo de modelo y clasificación

`MenuItem` no contiene metadata que distinga parent estructural de parent navegable. NAV4.1B no agrega `IsNavigable` ni modifica backend/schema/seed. La distinción se obtiene de la configuración Angular real del Router mediante un helper UI genérico.

El árbol canónico vigente tiene **13 parents con children**:

- `FUNCTIONAL_PARENT` (4): `Catálogos` (`/catalogs` -> `CatalogsListComponent`), `Conexiones` (`/integraciones` -> lazy `IntegrationWorkspaceComponent`), `Reportes` (`/reports` -> `ReportsHomeComponent`) y `Usuarios y acceso` (`/users` -> pathless default `UsersListComponent`).
- `STRUCTURAL_PARENT` (9): `Operación` (`/transactions` -> default redirect a `list`), `Seguimiento y control` (sin route Angular exacta), `Respuestas de transacciones` (sin route Angular exacta), `Administración de parámetros` (sin route Angular exacta), `Configuración de archivos NACHA-M` (`/nacha-config-admin` -> default redirect a `perfiles`), `Seguridad de archivos` (`/nacha-security` -> default redirect a `certificates`), `Administración` (sin route Angular exacta), `Registros del sistema` (`/logs` -> redirect a `/audit-logs`) y `Herramientas de prueba` (`/uat` -> default redirect al simulador).

La evidencia completa de route config y children de los 13 parents está en `docs/ai/navigation-menu/phases/PHASE_04_NAV4_SPA_COPY.md`.

### Mecanismo genérico

`MenuRouteSurfaceService` inspecciona `router.config` y clasifica la URL exacta de cada parent visible con children:

- `component` o `loadComponent` en la coincidencia exacta => funcional;
- children pathless `''` que terminan en component/loadComponent => funcional;
- `redirectTo` exacto o route inexistente => estructural;
- `loadChildren` se resuelve para arrays `Routes` y NgModules lazy; los NgModules se inspeccionan mediante el token `ROUTES` del injector propio;
- la configuración lazy se cachea por `Route`.

No existe lista hardcodeada de rutas funcionales/estructurales y no hay una excepción `/reports`. Un parent futuro desconocido para el menú adopta automáticamente link + toggle si su route registrada resuelve una superficie funcional propia.

### Interacción final

Para `FUNCTIONAL_PARENT`, `MainLayout` separa capacidades:

- label/icon/área principal = link/routerLink al parent;
- botón nativo separado = expand/collapse de children.

Para `STRUCTURAL_PARENT`, el nodo permanece como button expander y no intenta navegar a rutas inexistentes o redirect-only.

El toggle funcional usa `type=button`, `aria-expanded`, `aria-controls`, `aria-label` dinámico y foco visible; al ser button nativo conserva activación Enter/Space. En desktop compacto sigue disponible y recupera el sidebar antes de abrir el grupo. En móvil el toggle no cierra el drawer; el link usa el cierre de navegación existente.

### Active state

El link de un parent funcional usa `RouterLinkActive` exacto: la route exacta del parent recibe `aria-current=page`; un child conserva su propio estado activo y `aria-current`, mientras el parent permanece indicado/expandido como rama mediante la lógica recursiva existente. `syncActiveNavigation()` y la semántica backend de `exact` no se reescriben.

### Casos principales

- `/reports`: confirmado landing funcional y útil. `ReportsHomeComponent` contiene grupos/enlaces de reportes y revisión contable. `Reportes` ahora navega al landing desde el parent y conserva toggle independiente para exactamente 10 children. No se fabrica `Resumen de reportes`.
- `/integraciones`: confirmado landing funcional `IntegrationWorkspaceComponent`. `Conexiones` usa el mismo mecanismo genérico y conserva exactamente sus cuatro children NAV4.1A. `Centro de integración CFA` no reaparece como item.

### Límites y seguridad

`NavigationService` NAV4.1A permanece byte a byte intacto: fetch backend + sort recursivo sólo por `order`; sin filtros por route/label, sin reparenting y sin fabricación/eliminación de children.

También permanecen byte a byte intactos `NavigationInformationArchitectureSeeder`, `GetMenuForCurrentUserHandler`, seeders NAV3, `FineGrainedGrantBackfillSeeder` Order 119, `LegacyFineGrainedRoleMigrationSeeder` Order 120, policies y guards. No se cambian grants, fallbacks, role gates ni autorización de routing. El link del parent continúa sometido a sus guards existentes.

### Validación

- SHA-256 input NAV4.1A: **PASS** — `7d0d1c242d4e03e5d9e04af7a6ee8c97af4a10087d3a1b450dd170a0c06cc5ab`.
- parser TypeScript 5.8.3 focal (4 archivos TS NAV4.1B): **PASS**.
- `scripts/validate-nav4-1b-parent-navigation.py`: **PASS — 108 aserciones**, incluyendo hash-lock de `NavigationService` NAV4.1A y superficies protegidas.
- `.NET`: **NOT_RUN_ENVIRONMENT** — `dotnet` no disponible.
- Angular build/test/runtime: **NOT_RUN_ENVIRONMENT** — `web/ach-interbank-ui/node_modules` ausente.

NAV4.1B puede considerarse implementado a nivel diseño/código, con runtime diferido por ambiente.

## DECISIÓN REQUERIDA — NAV4.1B

- **Parents con children:** 13.
- **Funcionales:** 4 — Catálogos, Conexiones, Reportes, Usuarios y acceso.
- **Estructurales:** 9 — Operación, Seguimiento y control, Respuestas de transacciones, Administración de parámetros, Configuración de archivos NACHA-M, Seguridad de archivos, Administración, Registros del sistema, Herramientas de prueba.
- **Evidencia de clasificación:** configuración real del Angular Router, distinguiendo component/loadComponent, default component pathless, redirects y ausencia de route.
- **Router usado genéricamente:** sí; se inspecciona `router.config` y lazy route configuration.
- **Lista hardcodeada de rutas:** no.
- **Parent funcional:** link semántico navegable + toggle button independiente para children.
- **Parent estructural:** button expander, sin navegación inválida.
- **`/reports`:** landing funcional accesible desde el menú; toggle separado conserva 10 children.
- **`/integraciones`:** funcional por la misma regla genérica; link al workspace + toggle; cuatro children intactos.
- **Separación navegación/expansión:** dos controles distintos en parents funcionales.
- **Active state:** parent exacto activo en su route; child activo en route hija; parent queda indicado/expandido sin sustituir al child como `aria-current`.
- **Accesibilidad/teclado:** toggle nativo con ARIA/foco visible y semántica Enter/Space; link sigue siendo anchor/routerLink.
- **NavigationService NAV4.1A:** intacto.
- **Seguridad NAV3:** intacta; sin cambios de fallbacks, role gates, grants, policies, guards o handler.
- **NAV3.5A/B:** intactos; Orders 119/120 permanecen.
- **NAV4.1B diseño/código:** implementado, runtime diferido.
- **Gaps SPA restantes:** titles/breadcrumbs y copy visible todavía pueden divergir de la navegación canónica; no se abordaron en NAV4.1B.
- **Siguiente incremento recomendado:** `NAV4.2A — títulos y breadcrumbs derivados de la navegación/routing canónico`, sujeto a decisión humana; no ejecutar automáticamente.
- **.NET:** `NOT_RUN_ENVIRONMENT`.
- **Angular:** `NOT_RUN_ENVIRONMENT`.


## NAV4.2A — Títulos y breadcrumbs derivados de navegación/routing canónico — 2026-10-15

NAV4.2A parte únicamente de `ACHInterbank-R34-NAV-IA-NAV4.1B-FULL-PROJECT.zip`, SHA-256 `7a744875af98c6b9fcfa5c5381a9f998df87acf480428606023eae27ac8fcdc6`, sin Git. NAV2 y NAV3 permanecen exactamente `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`; NAV3.5C continúa `BLOCKED / NO EJECUTAR`; NAV4.1A y NAV4.1B permanecen `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`.

### Infraestructura previa

- `MainLayout.buildBreadcrumbs()` era la fuente del H1 de layout (`pageTitle`) y del breadcrumb global. Recorría `ActivatedRoute.firstChild`, tomaba `data.title`/`data.breadcrumb` y armaba URLs desde `routeConfig.path`.
- La aplicación ya tenía breadcrumb UI visible en el toolbar; no se agrega un componente visual nuevo.
- `ui-migas-pan` constituía una segunda infraestructura de breadcrumbs internos con arrays locales `{ etiqueta, ruta? }`.
- `document.title` era únicamente el `<title>` estático `ACH Interbank - Gestión de Transacciones`; no existían `Title.setTitle`, `document.title` dinámico ni `TitleStrategy`.
- `AppComponent` no producía naming visible. `NavigationLogService` observaba `NavigationEnd` sólo para logging.
- No se encontró mapa/switch global route→label. Sí existían metadata/arrays locales con aliases técnicos.

### Modelo final

`NavigationNamingService` comparte una única ejecución de `NavigationService.getMenu()` con `shareReplay(1)`. El resolver recursivo puro (`navigation-naming.util.ts`) no contiene rutas ni labels de negocio. Para una URL exacta presente en el árbol backend, el `MenuItem.label` es la fuente efectiva de page title y breadcrumb; los ancestors del breadcrumb son los ancestors reales del árbol backend. `NavigationService` no se modifica.

Para una route contextual/non-menu, se conserva la metadata `data.title`/`data.breadcrumb`. Si existe un MenuItem ancestor por boundary de segmentos, se usa sólo para elegir el contexto más específico; su jerarquía sigue viniendo del árbol backend y no de `route.split('/')`. No se crea MenuItem contextual.

Las URLs parametrizadas se obtienen de `ActivatedRouteSnapshot.url`, de modo que IDs reales pueden existir en el href técnico pero `:id` nunca se vuelve label. Query params y fragments no intervienen en el matching exacto. No hay llamadas API adicionales para nombres de entidades.

`ui-migas-pan` conserva el componente y copy contextual, pero cuando un crumb apunta a una route exacta menu-backed —o el último crumb coincide con la route actual exacta— muestra automáticamente el label backend. Esto elimina aliases de breadcrumb sin editar cada página ni crear mapping Angular.

El browser title conserva el branding/separador existente y pasa a `ACH Interbank - <pageTitle efectivo>` dentro de MainLayout. Para menu-backed `<pageTitle>` es el label backend; para contextual es la metadata vigente.

### Casos focales

- `/reports` -> `Reportes`; `/reports?x=1#section` produce el mismo match. Los children usan labels backend, por ejemplo `/reports/sent` -> `Transacciones enviadas` y `/reports/files` -> `Archivos NACHA-M`.
- `/integraciones` -> `Conexiones` y breadcrumb backend `Administración de parámetros -> Conexiones`.
- `/integraciones/contrato` -> `Administración de parámetros -> Conexiones -> Contrato del servicio`.
- `/integraciones/relacion-informacion` -> `Administración de parámetros -> Conexiones -> Relación de información`.
- `/integraciones/conexion` -> `Administración de parámetros -> Conexiones -> Conexión entidad participante`; `Conexión CFA` deja de ser fuente efectiva.
- `/administracion/mft-ach` -> `Administración de parámetros -> Conexiones -> Configuración del intercambio de archivos`, sin inferir parent por prefijo URL.
- `/users/new`, `/users/:id/edit`, `/users/:id/roles`, `/customers/:id/third-parties`, detalles de respuestas/intentos y `/transactions/bulk-ingestion/:batchId` permanecen contextuales y usan route metadata.

### Fallback / carga inicial

Antes de que el árbol backend llegue, MainLayout conserva el fallback técnico de route metadata y `ui-migas-pan` conserva sus etiquetas locales. En cuanto el stream compartido recibe el árbol, ambos recalculan. Un error de menú conserva el status/error handling previo y no produce `undefined` ni una excepción de routing.

### Cambios realizados

- nuevo `navigation-naming.util.ts` recursivo y sin taxonomía productiva hardcodeada;
- nuevo `NavigationNamingService` compartiendo el árbol de `NavigationService`;
- `MainLayout` usa naming canónico para H1, breadcrumb y browser title;
- `ui-migas-pan` canonicaliza sólo crumbs que puedan resolverse exactamente al árbol backend;
- pruebas focales de source-of-truth, labels sintéticos, child futuro, Reports, Conexiones, MFT conceptual, contextual/parametrizada y query/fragment;
- nuevo validador estático NAV4.2A.

No se modifican route definitions, NavigationInformationArchitectureSeeder, GetMenuForCurrentUserHandler, guards, policies, grants, fallbacks, NAV3.5A/B, NavigationService ni MenuRouteSurfaceService.

### Gaps de copy pendientes

Permanece fuera de alcance el copy interno general de headings, botones, formularios, columnas, mensajes, tooltips, cards y textos técnicos. Hay CFA/CENIT/UAT/Command Center/Capability y otros términos todavía visibles dentro de páginas que no son la pieza efectiva de route title/breadcrumb. Deben inventariarse y decidirse en un incremento posterior.

### Validación

- SHA-256 input NAV4.1B: `7a744875af98c6b9fcfa5c5381a9f998df87acf480428606023eae27ac8fcdc6` — **PASS**.
- parser TypeScript 5.8.3 focal — **PASS**.
- test Node independiente sobre el resolver TypeScript puro — **PASS**.
- `scripts/validate-nav4-1b-parent-navigation.py` — **PASS — 108 aserciones** sobre el código NAV4.2A antes de avanzar el marcador documental de fase; sus invariantes de código/seguridad quedan además revalidadas por el validador NAV4.2A final.
- `scripts/validate-solution-projects.py` — **PASS — 7/7 proyectos**.
- Angular build/test/runtime — `NOT_RUN_ENVIRONMENT` porque `web/ach-interbank-ui/node_modules` está ausente.
- .NET — `NOT_RUN_ENVIRONMENT` porque `dotnet` no está disponible.

NAV4.2A puede considerarse implementado a nivel diseño/código, con runtime diferido por ambiente. El siguiente candidato natural, sujeto a decisión humana, es `NAV4.2B — humanización y consistencia del copy visible de navegación/páginas`. No avanzar automáticamente.

## NAV4.2B — encabezados menu-backed y metadata fallback — 2026-10-15

- Estado: `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`. NAV2, NAV3, NAV4.1A, NAV4.1B y NAV4.2A conservan `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`; NAV3.5C conserva `BLOCKED / NO EJECUTAR`.
- Inventario de entrada: 81 rutas menu-backed resolubles inspeccionadas. Clasificación previa: 20 `ALIGNED`, 52 `DRIFT`, 7 `NO_HEADING`, 2 `CONTEXTUAL_ONLY`. Se detectaron 47 rutas con drift de heading principal de nombre de pantalla y 51 con drift en `data.title`/`data.breadcrumb` fallback.
- Modelo durable: NAV4.2A no cambia. Con árbol disponible, `MenuItem.label` + ancestors backend siguen siendo la fuente de verdad; route metadata queda únicamente como fallback técnico previo a la carga del árbol. NAV4.2B alinea ese fallback y los headings principales, sin convertir metadata Angular en taxonomía canónica.
- Headings: se alinearon únicamente nombres principales de superficies menu-backed. No se hizo barrida general de botones, formularios, columnas, mensajes, tooltips, validaciones, modales ni ayuda.
- `/users` y `/users/list` comparten `UsersListComponent`; para evitar un condicional URL→label, el heading inicia desde `route.snapshot.data.title` y, cuando llega el menú, se canonicaliza con `NavigationNamingService.canonicalLabelForRoute`.
- Metadata: las rutas menu-backed con alias legacy se alinearon al label NAV2 como fallback. Las rutas contextuales `/users/new`, `/users/:id/edit` y `/users/:id/roles` conservan su metadata contextual; no se fabricaron MenuItems.
- Focales: `/reports` = `Reportes`; `/integraciones` = `Conexiones`; `/administracion/mft-ach` = `Configuración del intercambio de archivos`; `/users` = `Usuarios y acceso`. El browser title continúa bajo NAV4.2A como `ACH Interbank - <Página>`.
- Invariantes: `NavigationService`, `MenuRouteSurfaceService`, `NavigationNamingService` y `navigation-naming.util.ts` permanecen byte-idénticos respecto de NAV4.2A. `GetMenuForCurrentUserHandler`, `NavigationInformationArchitectureSeeder`, seeders NAV3, guards y Orders 119/120 permanecen intactos.
- Validación disponible: parser TypeScript sobre 38 TS modificados = PASS; `validate-nav4-2b-menu-backed-copy.py` = PASS (81 assertions); solution graph = PASS (7/7); escaneo de metadata canónica = 79 registros / 76 rutas con metadata / 0 mismatches; escaneo de headings principales = 0 mismatches. Angular build/test/runtime = `NOT_RUN_ENVIRONMENT` por ausencia de `node_modules`. .NET = `NOT_RUN_ENVIRONMENT` por ausencia de `dotnet`.
- Copy pendiente observado y fuera de alcance: aliases técnicos o históricos en acciones/enlaces secundarios, mensajes, aria/labels de tablas, copy contextual profundo y descripciones; ejemplos incluyen `Volver al Centro de integración CFA`, acciones `Carga masiva por archivo`, referencias `Restricciones operacionales CFA`, mensajes de `Sobre digital`/`Capability Registry` y otras etiquetas internas.
- Siguiente paso: `USER_DECISION_REQUIRED`. Candidato focal recomendado a partir del inventario: `NAV4.2C — acciones y enlaces secundarios de navegación asociados a rutas menu-backed`; no ejecutar automáticamente.


## NAV4.2C — acciones y enlaces secundarios de navegación — 2026-10-15

- Source of truth package: `ACHInterbank-R34-NAV-IA-NAV4.2B-FULL-PROJECT.zip`, SHA-256 `fe2f56f5d6ac8297d7c9ee68ec373f9c8f58f95bb1ed01825ee4a807614500f1`; no Git.
- Estado: NAV2=`IMPLEMENTED_UNVERIFIED_ENVIRONMENT`, NAV3=`IMPLEMENTED_UNVERIFIED_ENVIRONMENT`, NAV3.5C=`BLOCKED / NO EJECUTAR`, NAV4.1A/B=`IMPLEMENTED_UNVERIFIED_ENVIRONMENT`, NAV4.2A/B/C=`IMPLEMENTED_UNVERIFIED_ENVIRONMENT`.
- Inventario focal: **114** definiciones de control navegacional; entrada NAV4.2B = 33 `ALIGNED`, 27 `DRIFT`, 48 `CONTEXTUAL_NAVIGATION`, 6 `FUNCTIONAL_ACTION_OUT_OF_SCOPE`. Los 27 drifts de naming de destino menu-backed fueron corregidos sin cambiar rutas/handlers. Inventario completo: `phases/NAV4_2C_NAVIGATION_CONTROL_INVENTORY.md`.
- `NavigationNamingService` no se modificó ni se inyectó en nuevos componentes. Los controles corregidos son links/CTAs ya ligados a un destino fijo o colecciones locales existentes; se cambió sólo el string local. No existe nuevo mapa/switch route→label ni nueva carga HTTP del menú.
- Conexiones: retorno explícito a `/integraciones` = `Volver a Conexiones`; acceso a `/integraciones/conexion` contiene `Conexión entidad participante`. No queda `Centro de integración CFA` como copy visible navegacional.
- MFT: enlaces a `/administracion/mft-ach` usan `Configuración del intercambio de archivos`; `MFT`/SFTP siguen válidos como términos técnicos internos.
- Operación: link a `/transactions/bulk-ingestion/upload` = `Cargar transacciones desde archivo`; links a tracking = `Seguimiento de cargas`; acción funcional real `Enviar archivo` queda fuera de alcance.
- Reportes: landing y 10 cards/children permanecen funcionalmente intactos; titles de cards ya estaban canonicalizados.
- Rutas contextuales conservan metadata/copy propio; el back histórico genérico permanece `Volver`.
- Copy accesible sólo se cambió cuando pertenecía al mismo control focal: retorno compartido a Cámaras compensadoras alinea visible, `aria-label` y `matTooltip`. No hubo auditoría ARIA general.
- No regresión: `NavigationService`, `MenuRouteSurfaceService`, `NavigationNamingService`, resolver NAV4.2A, `GetMenuForCurrentUserHandler`, seed NAV2, seeders NAV3, guards y Orders 119/120 permanecen byte-idénticos. NAV4.2B validator continúa PASS y su scope de headings/metadata conserva 0 mismatches.
- Fuera de alcance pendiente: acciones funcionales, formularios, tablas/columnas, mensajes/empty states y tooltips/ARIA no navegacional.
- NEXT: esperar decisión humana. Candidato basado en inventario: `NAV4.2D — copy de acciones funcionales primarias/secundarias`; no ejecutar automáticamente.


## NAV4.2D — copy de acciones funcionales primarias/secundarias — 2026-10-15

- Source of truth package: `ACHInterbank-R34-NAV-IA-NAV4.2C-FULL-PROJECT.zip`, SHA-256 `36380e5fa7843611928cd35163e25b6f03efc7384ff18cfb8b3b9e82cd3dcda0`; no Git.
- Estado: NAV2=`IMPLEMENTED_UNVERIFIED_ENVIRONMENT`, NAV3=`IMPLEMENTED_UNVERIFIED_ENVIRONMENT`, NAV3.5C=`BLOCKED / NO EJECUTAR`, NAV4.1A/B=`IMPLEMENTED_UNVERIFIED_ENVIRONMENT`, NAV4.2A/B/C/D=`IMPLEMENTED_UNVERIFIED_ENVIRONMENT`.
- Inventario focal: **582** definiciones de control inspeccionadas; **461** acciones funcionales y **121** `OUT_OF_SCOPE`. Decisiones de entrada NAV4.2C: 289 `ALIGNED`, 45 `DRIFT`, 14 `TECHNICAL_TERM_LEGITIMATE`, 113 `CONTEXTUAL_ACTION`, 121 `OUT_OF_SCOPE`. Los **45 drifts** fueron corregidos únicamente mediante copy visible/accesible del mismo control. Inventario completo: `phases/NAV4_2D_FUNCTIONAL_ACTION_INVENTORY.md`.
- Taxonomía funcional de las 461 acciones: CREATE 30, UPDATE 3, DELETE 10, SAVE 31, UPLOAD 4, DOWNLOAD 9, GENERATE 7, EXPORT 2, IMPORT 2, RETRY 39, CANCEL_OPERATION 1, VALIDATE 6, PREVIEW 6, PUBLISH 2, CLONE 2, ENCRYPT 4, DECRYPT 2, TEST_CONNECTION 1, REFRESH 23, FILTER 6, CLEAR_FILTERS 35, SEARCH 22, ACTIVATE 3, DEACTIVATE 18, REVOKE 2 y OTHER_FUNCTIONAL 191.
- Convención durable: el verbo se decide desde el efecto real del handler, no desde el nombre del método/icono. Se preservan diferencias destructivas (`Eliminar`, `Desactivar`, `Revocar`, `Cancelar operación`), operación de archivo (`Enviar`/`Cargar`, `Descargar`/`Exportar`), y estados de workflow (`Validar`, `Vista previa`, `Publicar`, `Clonar`, `Generar`). Términos ACH/NACHA-M/MFT/SFTP/SOAP/WSDL/XSD/SHA-256 permanecen técnicos cuando corresponde.
- Seis controles híbridos NAV4.2C: `Nueva reclamación`, `Actualizar información`, `Limpiar filtros` y `Crear borrador` permanecen; `Guardar perfiles` pasa a `Guardar perfiles de acceso`; `Clonar` pasa a `Clonar configuración`. La mutación principal nombra el control aunque después exista navegación.
- No logic delta: `NAV4_2D_COPY_DELTA.json` registra exactamente **49 líneas** modificadas en **39 archivos SPA** (38 producción + 1 spec). El validador revierte únicamente esas líneas y reproduce el hash agregado exacto del árbol `src/app` NAV4.2C, demostrando estáticamente que handlers, eventos, service methods, endpoints, payloads, `[disabled]`, permisos/gating, confirmaciones, loading y navegación posterior no cambiaron.
- No regresión: `NavigationService`, `MenuRouteSurfaceService`, `NavigationNamingService`, resolver NAV4.2A, handler backend, seed NAV2, seeders NAV3 y guards conservan sus hashes; Orders 119/120 siguen intactos. Validadores NAV4.2B/C continúan PASS, por lo que headings/metadata menu-backed y navigation-copy mantienen 0 drift dentro de sus scopes.
- Validación disponible: `validate-nav4-2d-functional-action-copy.py` = PASS (210 assertions); `validate-nav4-2c-navigation-copy.py` = PASS (111); `validate-nav4-2b-menu-backed-copy.py` = PASS (81); parser TypeScript 5.8.3 sobre los 2 TS modificados = PASS, 0 diagnostics; solution graph = PASS 7/7. Angular build/test/runtime = `NOT_RUN_ENVIRONMENT` por ausencia de `node_modules`; .NET = `NOT_RUN_ENVIRONMENT` por ausencia de `dotnet`.
- Copy restante no tratado: formularios (394 `mat-label`, 88 `<label>`, 54 placeholders, 78 hints detectados como universo estructural), tablas/columnas, mensajes/empty states y tooltips/ARIA no funcional. Estas cifras son superficie potencial, no conteo de drift.
- NEXT: esperar decisión humana. Candidato focal recomendado por ser un bloque estructurado, de alto volumen y semánticamente acotado: `NAV4.2E — labels/placeholders/hints de formularios`; no ejecutar automáticamente.


## NAV4.2E — labels, placeholders y hints de formularios — 2026-10-15

- Source of truth package: `ACHInterbank-R34-NAV-IA-NAV4.2D-FULL-PROJECT.zip`, SHA-256 `0da1a5f2bba0b00e5255d61c230708082f1a7cbe47edfa62a4a706867240d8d2`; no Git.
- Estado: NAV2=`IMPLEMENTED_UNVERIFIED_ENVIRONMENT`, NAV3=`IMPLEMENTED_UNVERIFIED_ENVIRONMENT`, NAV3.5C=`BLOCKED / NO EJECUTAR`, NAV4.1A/B=`IMPLEMENTED_UNVERIFIED_ENVIRONMENT`, NAV4.2A/B/C/D/E=`IMPLEMENTED_UNVERIFIED_ENVIRONMENT`.
- Inventario exacto: **675 hooks** de copy de campo y **517 fields físicos únicos**. Tipos: 370 `MAT_LABEL`, 143 `HTML_LABEL`, 47 `PLACEHOLDER`, 75 `HINT`, 40 `ACCESSIBLE_FIELD_NAME`. El universo de 614 reportado en NAV4.2D era un scan estructural preliminar; NAV4.2E incorpora además labels equivalentes de `ui-campo-*`, checkbox/radio/toggle y nombres accesibles del propio field.
- Clasificación de entrada: 355 `ALIGNED`, 36 `DRIFT`, 24 `TECHNICAL_TERM_LEGITIMATE`, 167 `DOMAIN_TERM_LEGITIMATE`, 58 `CONTEXTUAL_FIELD`, 35 `DYNAMIC_DATA`, 0 `OUT_OF_SCOPE` dentro del inventario focal. Los **36 DRIFT** fueron corregidos sólo en copy y el drift residual identificado dentro del scope queda en 0. Inventario: `phases/NAV4_2E_FORM_FIELD_INVENTORY.md`.
- Convención durable: label = dato/dimensión funcional; placeholder = ejemplo/formato/orientación cuando aporta valor; hint = contexto/formato/restricción útil sin exponer implementación innecesaria. `ID de …`/`IDs de …` se usa para identificadores cuando el tipo real lo demuestra. No se usa placeholder como sustituto del label.
- Se preservan términos técnicos/de dominio cuando son precisos: ACH, NACHA-M, MFT, SFTP, SOAP, WSDL, XSD, SHA-256/SHA256 cuando es formato real, URL, IP, puerto, namespace, GUID, JSON, CSV y UTC. Opciones/catalog data/roles/fuentes contractuales permanecen dinámicos y no se hardcodean.
- Copy-only delta: `NAV4_2E_COPY_DELTA.json` registra **36 líneas/hooks** modificados en **12 HTML de producción**. El validador revierte exclusivamente esas líneas y reproduce el hash agregado exacto del árbol `src/app` NAV4.2D `4d83eac4561e9a8b4a57ce91e20980bce2f8e1a5f76c6b8cc8b6a0c8f8e1022f`.
- Integridad funcional de fields: la firma estática de **853 registros de controles/options**, excluyendo sólo atributos de copy, es idéntica before/after: `299fe031e20fa402d9a3aa941859c0d682a98771f872c593a30e2fa8c7adf015`. Por tanto formControlName/[formControl]/ngModel/name/value/option value/validators declarativos/required/disabled/readonly/type/autocomplete/min/max/pattern y eventos permanecen intactos.
- Secretos: los cuatro campos focales password/credential conservan exactamente `type`, `autocomplete`, binding y masking/disabled; no aparece ningún secreto nuevo en UI.
- No regresión: NAV4.2D action-copy continúa sin drift; NAV4.2C navigation-copy continúa sin drift; NAV4.2B mantiene headings y metadata fallback en 0 mismatches. `NavigationService`, `MenuRouteSurfaceService`, `NavigationNamingService`, resolver NAV4.2A, handler backend, seed NAV2, seeders NAV3 y guards conservan hashes; Orders 119/120 permanecen.
- Validación disponible: `validate-nav4-2e-form-copy.py` = PASS (264 assertions; incluye regresión exacta de strings NAV4.2D y validadores NAV4.2C/B); NAV4.2C = PASS (111); NAV4.2B = PASS (81); parser TypeScript 5.8.3 sobre 12 TS asociados = PASS, 0 diagnostics; firma independiente de 853 controls/options = PASS; solution graph = PASS 7/7. Angular build/test/runtime = `NOT_RUN_ENVIRONMENT` por ausencia de `node_modules`; .NET = `NOT_RUN_ENVIRONMENT` por ausencia de `dotnet`.
- Deliberadamente diferido: `mat-error`/validaciones, tablas/columnas, mensajes/empty states, tooltips/ARIA no asociados a fields y option labels/enums. El gap conocido UI `.xls` vs backend `.xlsx/.xlsm` sigue separado.
- NEXT: esperar decisión humana. El mayor bloque estructural restante observado es tabular (aprox. 255 `<th>` + 250 `headerName:` frente a 224 `mat-error`), por lo que el candidato recomendado es `NAV4.2F — tablas, columnas y filtros/tabular accessibility`; requiere inventario semántico antes de cambiar copy y no debe ejecutarse automáticamente.

## NAV4.2F — tablas, columnas y accesibilidad tabular — 2026-10-15

- Source of truth package: `ACHInterbank-R34-NAV-IA-NAV4.2E-FULL-PROJECT.zip`, SHA-256 `48e215acdd645c64c16ace36f662deaabf47bbe9eb0ca3d4c6bcf060d703e9fd`; no Git.
- Estado: `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`. NAV2/NAV3 y NAV4.1A/B/NAV4.2A/B/C/D/E mantienen `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`; NAV3.5C continúa `BLOCKED / NO EJECUTAR`.
- Inventario exacto: **630 hooks tabulares**, **573 columnas estáticas/lógicas únicas**. Tipos: 44 `HTML_TH`, 206 `MATERIAL_HEADER`, 250 `GRID_HEADER_NAME`, 86 `COLUMN_DISPLAY_LABEL`, 0 `SORT_ACCESSIBLE_NAME`, 16 `TABLE_ACCESSIBLE_NAME`, 25 `PAGINATION_ACCESSIBLE_COPY`, 3 `OTHER_TABULAR_COPY`.
- Clasificación de entrada: 243 `ALIGNED`, 112 `DRIFT`, 8 `TECHNICAL_TERM_LEGITIMATE`, 54 `DOMAIN_TERM_LEGITIMATE`, 210 `CONTEXTUAL_COLUMN`, 3 `DYNAMIC_DATA`, 0 `OUT_OF_SCOPE`. Los 112 drifts fueron corregidos; residual identificado = 0. Inventario: `phases/NAV4_2F_TABULAR_COPY_INVENTORY.md`.
- Copy-only delta: `NAV4_2F_COPY_DELTA.json` registra **108 líneas / 26 archivos de producción / 112 hooks**. Revirtiendo sólo esas líneas se reconstruye el árbol NAV4.2E `src/app` hash `62230bdde11661f6d55559e6077972a65646578c9ae9c015002d7f563291e3cb`.
- Export: ningún header cambiado está acoplado a export AG Grid CSV/Excel; los PDF de Reportes usan APIs backend dedicadas y no reciben `config.columns`. Contratos externos de export = sin cambio.
- Invariantes: column keys/IDs, fields/bindings, sort/filter, renderers/value getters, displayed order, pagination, selection, endpoints/query params, row actions, forms y mensajes no cambiaron.
- No regresión: NavigationService/MenuRouteSurfaceService/NavigationNamingService/resolver, handler backend, seed NAV2, seeders NAV3 y guards conservan hashes; Orders 119/120 permanecen. NAV4.2D/E strings finales se verifican exactos.
- Inventario residual estructural: 202 `mat-error`; 24 `ui-estado-vacio` + 9 empty-state attrs de grids; 29 `ui-alerta`; 360 llamadas de notificación/snackbar candidatas; 49 `matTooltip`. Son hooks potenciales, no drifts.
- NEXT: esperar decisión humana. Por volumen residual, candidato exacto: `NAV4.2G — mensajes y estados vacíos`; reservar validaciones para NAV4.2H si el inventario semántico confirma el bloque separado. No ejecutar automáticamente.


## NAV4.2G — mensajes y estados vacíos — 2026-10-15

- Estado: `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`. Baseline único: `ACHInterbank-R34-NAV-IA-NAV4.2F-FULL-PROJECT.zip`, SHA-256 `6563abb7802f7b7d0e60ed48b3f62c4a432ded7742539ffd711b38a883afb79f`. No se usó Git.
- NAV2, NAV3, NAV4.1A/B y NAV4.2A-F conservan `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`; NAV3.5C conserva `BLOCKED / NO EJECUTAR`.
- Alcance productivo: exclusivamente copy/display de feedback y estado SPA. Inventario: **643 hooks**. Mecanismos: 25 `EMPTY_STATE`, 23 `GRID_EMPTY_STATE`, 29 `UI_ALERT`, 11 `SNACKBAR`, 279 `NOTIFICATION`, 106 `INLINE_STATUS`, 45 `LOADING_MESSAGE`, 45 `ERROR_MESSAGE`, 79 `WARNING_MESSAGE`, 1 `SUCCESS_MESSAGE`. Origen: 408 `SPA_AUTHORED`, 72 `SPA_COMPOSED`, 25 `HYBRID`, 36 `BACKEND_PASSTHROUGH`, 102 `DYNAMIC_DATA`; no se detectó un hook directo adicional clasificable únicamente como `EXTERNAL_PASSTHROUGH`, y los fragmentos externos presentes en hooks híbridos se preservaron.
- Clasificación: 292 `ALIGNED`, 48 `DRIFT`, 50 `TECHNICAL_TERM_LEGITIMATE`, 48 `DOMAIN_TERM_LEGITIMATE`, 67 `CONTEXTUAL_MESSAGE`, 102 `DYNAMIC_DATA`, 36 `BACKEND_PASSTHROUGH`. Los 48 hooks DRIFT se corrigieron mediante **50 líneas de copy en 27 archivos**; drift residual identificado dentro del alcance: 0.
- `NAV4_2G_COPY_DELTA.json` es el delta reversible; al revertir sus 50 líneas se reconstruye byte-a-byte el árbol `src/app` NAV4.2F con hash agregado `e260fe85e533bf326538f201343bf9b11a74236d62aa007628450121a86daa16`.
- Semántica async preservada: envío/recepción ACH Colombia indica fin de la ejecución local y remite al estado, sin afirmar recepción/aceptación externa; MFT sólo afirma conectividad en `test-connection`; Bulk Ingestion conserva `lote registrado`, `reintento solicitado` y `cancelación registrada`; NACHA-M conserva mensajes backend cuando existen; ROR conserva generación local sin afirmar transmisión; cifrado/descifrado sólo se afirma al completar esas operaciones.
- Backend/external passthrough: no se tradujeron, resumieron, corrigieron ni ocultaron `message/detail/title/errors`, cuerpos de error, códigos o causales Rxx/Dxx. En HYBRID los fragmentos dinámicos permanecen intactos.
- No logic delta: triggers, condiciones, severity, duration, snackbar action, handlers, retry/loading, service/API calls, endpoints, payloads, response parsing, navegación, permisos, validators, sort/filter/pagination permanecen intactos. NAV4.2D/E/F no se reabren.
- `scripts/validate-nav4-2g-messages-empty-states.py` — PASS, **1207 assertions**; parser TypeScript 5.8.3 sobre **12 TS productivos modificados** — PASS, 0 diagnostics; `scripts/validate-solution-projects.py` — PASS, 7/7. Angular = `NOT_RUN_ENVIRONMENT` porque `node_modules` está ausente; .NET = `NOT_RUN_ENVIRONMENT` porque `dotnet` está ausente.
- Siguiente candidato, sólo por decisión humana: `NAV4.2H — mensajes de validación`. **NAV4.2H NO fue ejecutado.** Confirmaciones y tooltips/ARIA generales siguen fuera de NAV4.2G.

## NAV4.2H — mensajes de validación — 2026-10-15

- Estado: `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`. Baseline exclusivo: `ACHInterbank-R34-NAV-IA-NAV4.2G-FULL-PROJECT.zip`, SHA-256 `7f6f4e74222c964e1ab7f6e65d060a6f5bcac6495c73272f254cf8e20907119a`; no se usó Git.
- Inventario exacto: **321 hooks** de validación, incluyendo **224 `mat-error`** (202 en HTML + 22 en templates inline TS), 14 `app-error-message`, resolvers/fábricas SPA y renders inline/resultados de validación. Origen: 288 `SPA_VALIDATION`, 16 `SPA_VALIDATION_COMPOSED`, 15 `BACKEND_VALIDATION_PASSTHROUGH`, 2 `DYNAMIC_DATA`, 0 direct `EXTERNAL_VALIDATION_PASSTHROUGH` observados.
- Clasificación: 188 `ALIGNED`, **92 `DRIFT`**, 11 `TECHNICAL_TERM_LEGITIMATE`, 13 `DOMAIN_TERM_LEGITIMATE`, 15 `BACKEND_VALIDATION_PASSTHROUGH`, 2 `OUT_OF_SCOPE`. Los 92 DRIFT se corrigen mediante **92 cambios de copy en 16 archivos productivos**; drift residual identificado del alcance = 0.
- `NAV4_2H_COPY_DELTA.json` contiene únicamente `archivo`, `old`, `new`, `campo/contexto`, `validator/error key`, `origen`. La reversión exacta reconstruye `src/app` NAV4.2G con hash agregado `ba52a7a83c980ba302754ea3b776989cbf075a654b6e451230decd85751d4737`.
- `scripts/validate-nav4-2h-validation-copy.py` — PASS, 1034 assertions: sólo copy permitido; validators, regex, min/max/length, error keys, condiciones, precedencia, bindings, touched/dirty/submitted, backend/external passthrough, NAV4.2G feedback, NAV4.2D/E/F, navegación/security/seed y reversión de delta quedan invariantes.
- Revalidación aislada de baselines previos: NAV4.2G PASS 1207 assertions; NAV4.2F PASS 475; NAV4.2C PASS 111; NAV4.2B PASS 81. Parser TypeScript 5.8.3 PASS en 5 TS modificados; scanner templates PASS (`mat-error=224`, `app-error-message=14`); solution graph PASS 7/7.
- Angular build/tests/runtime: `NOT_RUN_ENVIRONMENT` por ausencia de `web/ach-interbank-ui/node_modules`; existen specs focales de forms/validators pero no se instalaron dependencias. .NET: `NOT_RUN_ENVIRONMENT` porque `dotnet` no está disponible.
- NAV4.2G feedback/empty states/alerts/notifications permanece intacto; confirmaciones y tooltips/ARIA generales siguen fuera de NAV4.2H.
- NEXT: esperar decisión humana. El siguiente incremento recomendado es un bloque separado de **confirmaciones** (y después tooltips/ARIA generales), sin ejecutarlo automáticamente.

## NAV4.2I — confirmaciones — 2026-10-15

- Source of truth: `ACHInterbank-R34-NAV-IA-NAV4.2H-FULL-PROJECT.zip`, SHA-256 `6c63174893be602f52ad1e915f8641ce58d9f2b6e17445cce2dc90bd6a449039`; no Git.
- Estado: NAV2, NAV3, NAV4.1A/B y NAV4.2A-I = `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`; NAV3.5C = `BLOCKED / NO EJECUTAR`.
- Inventario exacto: **44 hooks** de confirmación previa a acción. Origen: 21 `SPA_CONFIRMATION`, 22 `SPA_CONFIRMATION_COMPOSED`, 1 `EXTERNAL_PASSTHROUGH` (`beforeunload` con copy controlado por navegador), 0 backend passthrough directos. Clasificación: 32 `ALIGNED`, 11 `DRIFT`, 1 `OUT_OF_SCOPE`.
- Los **11 DRIFT** se corrigieron mediante **13 cambios de copy en 8 archivos productivos**. Se clarifican inicio/intento frente a completitud, objeto de borrado, pérdida de cambios al descartar, configuración de envío real frente a ejecución SOAP, solicitud de ejecución scheduler frente a ejecución inmediata y generación ROR antes de que exista el archivo.
- `NAV4_2I_COPY_DELTA.json` revierte exactamente a NAV4.2H `src/app` hash `6e291cbba272a76baaec6979f3c163b62e51cb8c7e24af41ff8fbbe8544d1f83`. Botones/action labels, handlers/callbacks, condiciones, confirm/cancel result, APIs/payloads, validators, navegación/security/seed y superficies NAV4.2D/E/F/G/H permanecen invariantes.
- Decisiones: DEC-NAV-4.2I-001 acción/consecuencia sólo demostrables; DEC-NAV-4.2I-002 sólo título/cuerpo cambian y botones/handlers/resultados son invariantes; DEC-NAV-4.2I-003 backend/external passthrough no se reescribe.
- Restante fuera de fase: tooltips/ARIA generales. NEXT sólo por decisión humana; no avanzar automáticamente.


## NAV4.2J — tooltips y copy accesible ARIA — 2026-10-15

- Source of truth package: `ACHInterbank-R34-NAV-IA-NAV4.2I-FULL-PROJECT.zip`, SHA-256 `e9f86390a0d400d6219bc9060ad4ac817a50c9881d48949251b645848dd2ea90`; no Git.
- Estado: NAV2, NAV3, NAV4.1A/B y NAV4.2A-I permanecen `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`; NAV4.2J queda `IMPLEMENTED_UNVERIFIED_ENVIRONMENT`; NAV3.5C permanece `BLOCKED / NO EJECUTAR`.
- Inventario focal exacto: **392 hooks**. Mecanismos: 272 `ARIA_LABEL`, 45 `MAT_TOOLTIP`, 62 `TITLE`, 8 `SR_ONLY`, 3 `ARIA_LABEL_RUNTIME`, 2 `ARIA_LABEL_CONFIG`; `aria-description` = 0. Se auditaron 42 controles icon-only / `mat-icon-button` y no se detectó ninguno sin nombre accesible útil.
- Clasificación: 111 `ALIGNED`, 22 `DRIFT`, 36 `TECHNICAL_TERM_LEGITIMATE`, 62 `DOMAIN_TERM_LEGITIMATE`, 45 `CONTEXTUAL_COPY`, 9 `DYNAMIC_DATA`, 6 `BACKEND_PASSTHROUGH`, 0 `EXTERNAL_PASSTHROUGH`, 101 `OUT_OF_SCOPE`. Los 22 `DRIFT` quedaron corregidos con **22 cambios productivos de copy en 11 archivos**.
- `TITLE`: 4 hooks son ayuda/nombre de controles icon-only y 58 corresponden a títulos visibles de página/diálogo/componente; los 58 quedan `OUT_OF_SCOPE` y sin cambios. Navegación y confirmaciones accesibles inventariadas se preservan por las invariantes NAV4.2C/I.
- Correcciones focales: aliases accesibles históricos (`autenticaciones`) se alinean a `Registro de accesos`; `diff`/`grilla`/`badges` se reemplazan por copy orientado al usuario; menús icon-only especifican el objeto real; `Consultar trazabilidad` deja de anunciar `Ver detalle`; el tooltip de correlación identifica el dato exacto. Datos dinámicos, términos ACH/NACHA-M/CFA y passthrough se conservan.
- `NAV4_2J_COPY_DELTA.json` contiene sólo `file`, `old`, `new`, `mechanism`, `control_context`. Revertir sus 22 cambios reconstruye exactamente NAV4.2I `src/app`, hash agregado `5885f1a7c83c3c979d1a5909555ab372aa29d7b01dba1472547e5d933273b77f`. NO LOGIC DELTA.
- Invariantes comprobadas: handlers/callbacks, conditions/visibility, disabled/readOnly, roles, tabindex, focus/keyboard behavior, estados y relaciones ARIA, IDs, tooltip position/delay/disabled config, APIs/service calls, navegación, validators/error bindings, passthrough y códigos Rxx/Dxx permanecen sin cambios.
- Validación: `validate-nav4-2j-tooltip-aria-copy.py` PASS **445 assertions**; replays aislados I/H/G/F/C/B PASS (405 / 1034 / 1207 / 475 / 111 / 81 assertions); parser TypeScript 5.8.3 PASS sobre 374 TS productivos, 0 syntax diagnostics; scanner accessibility/templates PASS sobre 392 hooks e icon-only 42/0 missing-name; solution graph PASS 7/7.
- Angular build/tests/runtime = `NOT_RUN_ENVIRONMENT`: `web/ach-interbank-ui/node_modules` ausente; no se instalaron dependencias. .NET = `NOT_RUN_ENVIRONMENT`: `dotnet` ausente.
- Decisiones: `DEC-NAV-4.2J-001/002/003` registradas. Problemas que exijan estructura/comportamiento quedan fuera de alcance para fase posterior.
- Restante NAV4.2: no queda otro bucket de copy solicitado en esta secuencia. **NEXT recomendado = NAV5**, únicamente con aprobación humana. **NAV5 NO fue ejecutado**.


## NAV6 — Recertificación integral y cierre — 2026-10-15

- Source of truth package: `ACHInterbank-R34-NAV-IA-NAV5-FULL-PROJECT.zip`, SHA-256 `c11268bb2e5509abf6bbe39f4d2e0074714b1492674ed7d8b2b40183331ec115`; no Git.
- Estado: `NAV6=IMPLEMENTED_UNVERIFIED_ENVIRONMENT` y `NAV-MENU-IA-001=IMPLEMENTED_UNVERIFIED_ENVIRONMENT`. **IMPLEMENTACIÓN NAV-MENU-IA-001 COMPLETA**; **RECERTIFICACIÓN RUNTIME PENDIENTE POR AMBIENTE**. Conclusión: **COMPLETO PENDIENTE RUNTIME**.
- Cobertura final: 142 rutas = MENU 70, CONTEXTUAL 36, TECHNICAL 12, REDIRECT 24; MENU 70/70; DEAD 0; UNKNOWN 0; gaps bloqueantes 0.
- NAV4: drift = 0. Los validadores J/C/B y los replays I/H/G/F/E/D pasaron sobre snapshots reconstruidos con los deltas aprobados; los snapshot validators incompatibles con el baseline final no se declararon falsamente PASS.
- NAV3: seguridad intacta por hash/semántica y replays compatibles; `GetMenuForCurrentUserHandler`, policies, guards, grants, `FineGrainedGrantBackfillSeeder` Order 119, `LegacyFineGrainedRoleMigrationSeeder` Order 120 y fallbacks permanecen sin delta. `NAV3.5C` sigue `BLOCKED / NO EJECUTAR` y no fue ejecutado.
- Residual NAV5 `/nacha-security/certificates/:id/versions`: decisión **A = CONTEXTUAL_INTENTIONAL**. Es una ruta parametrizada, protegida y testeada de historial de versiones, válida como deep-link/contextual; no requiere MenuItem ni corrección NAV6.
- NAV6 productivo: 0 cambios. `src/`, `web/`, `tests/`, `database/` y `docker/` permanecen byte-equivalentes al input NAV5.
- Angular=`NOT_RUN_ENVIRONMENT` porque `node_modules` no existe. .NET=`NOT_RUN_ENVIRONMENT` porque `dotnet` no está disponible. No se instalaron dependencias ni herramientas.
- Handoff: `ACHInterbank-R34-NAV-IA-NAV6-FULL-PROJECT.zip`; el SHA-256 final se registra externamente después de empaquetar y se exige reextracción + recertificación del ZIP. No ejecutar trabajo posterior sin decisión humana.
