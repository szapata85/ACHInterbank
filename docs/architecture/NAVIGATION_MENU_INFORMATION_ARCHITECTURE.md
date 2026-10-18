# Arquitectura objetivo de navegación — NAV-MENU-IA-001

## Principios

- El usuario ve funciones, no tecnologías.
- Español claro; acrónimos sólo cuando son parte real del dominio (ACH, NACHA-M).
- La plataforma es multi-cámara; no se crea un silo CENIT.
- Operar, controlar, parametrizar y administrar son conceptos distintos.
- El seed determina estructura, copy, orden y autorización del menú.

## Árbol objetivo

```text
10 Inicio

20 Operación
   10 Consultar transacciones
   20 Crear transacción
   30 Cargar transacciones desde archivo
   40 Seguimiento de cargas
   50 Cargar archivo NACHA-M
   60 Devoluciones
   70 Devolución de una devolución
   80 Reclamaciones
   90 Intercambio de archivos
  100 Exportar archivo NACHA-M

30 Clientes

40 Seguimiento y control
   10 Transacciones enviadas
   20 Seguimiento de archivos
   30 Indicadores operativos
   40 Cola de procesamiento
   50 Devoluciones sin relación
   60 Respuestas de transacciones
      10 Bandeja de respuestas
      20 Resumen de respuestas
      30 Revisión manual
      40 Historial de respuestas
   70 Conciliación

50 Administración de parámetros
   10 Cámaras compensadoras
   20 Prioridades por cámara
   30 Ciclos de operación
   40 Días no hábiles
   50 Restricciones operativas
   60 Configuración de archivos NACHA-M
      10 Perfiles oficiales
   70 Catálogos
      10 Entidades financieras
      20 Conceptos de lote
      30 Tipos de documento
      40 Tipos de persona
      50 Tipos de género
      60 Tipos de teléfono
      70 Tipos de correo
      80 Tipos de dirección
      90 Códigos de transacción
   80 Conexiones
      10 Contrato del servicio
      20 Relación de información
      30 Conexión entidad participante
      40 Configuración del intercambio de archivos
   90 Homologación de estados

60 Seguridad de archivos
   10 Certificados digitales
   20 Cifrar y descifrar archivos

70 Reportes
   10 Transacciones enviadas
   20 Transacciones recibidas
   30 Devoluciones
   40 Rechazos
   50 Archivos NACHA-M
   60 Ciclos
   70 Conciliación
   80 Trazabilidad ACH
   90 Histórico
  100 Auditoría

80 Administración
   10 Usuarios y acceso
      10 Administrar usuarios
      20 Identidad visual
      30 Reglas de contraseña
      40 Bloqueo de acceso
   20 Tareas programadas
   30 Registros del sistema
      10 Registro de auditoría
      20 Registro de accesos
      30 Registro de navegación
   40 Organización del menú
   50 Capacidades de la plataforma

90 Herramientas de prueba [autorización técnica]
   10 Simulador de archivos recibidos
   20 Pruebas de servicios
```
**NAV2.8A:** `Administración` usa `/administracion` sólo como identificador estructural del menú (sin pantalla SPA propia). `Usuarios y acceso` reutiliza el `MenuItem` y la ruta SPA existentes `/users`; sus destinos funcionales publicados siguen siendo `/users/list`, `/users/branding`, `/users/password-rules` y `/users/login-lockout`.

**NAV2.10B:** `Catálogos` permanece bajo `Administración de parámetros` con Order 70 y queda compuesto únicamente por las nueve superficies administrativas SPA confirmadas en el árbol (órdenes 10..90). Se reutiliza `/catalogs/financial-institutions` y se publican las ocho rutas administrativas restantes con el permiso vigente `CanManageAch`; el redirect legacy `/catalogs/clearing-house-special-dates` conserva ruta/código/autorización pero queda inactivo como entrada de menú principal.

**NAV2.8B:** `Tareas programadas` reutiliza el `MenuItem` funcional existente `/scheduler/tasks` y queda como hijo directo de `Administración` con Order 20. El agrupador legacy `/scheduler` se conserva en datos y en el SPA como redirect a `tasks`, pero queda inactivo en el menú; no se publican rutas adicionales para detalle, historial, instancias, ejecución o edición de programación.

**NAV2.8C:** `Registros del sistema` reutiliza el `MenuItem` legacy `/logs` como agrupador estructural hijo directo de `Administración` con Order 30. Reutiliza `/audit-logs` como `Registro de auditoría` Order 10 y `/auth-logs` como `Registro de accesos` Order 20. La ruta SPA vigente `/navigation-logs` se publica como `Registro de navegación` Order 30; como no tenía `MenuItem`, el nuevo ítem reutiliza `CanReadAch`, que ya protege el GET funcional `api/navigation-logs`. No se modifica Angular ni la autorización existente de `/logs`, `/audit-logs` o `/auth-logs`. Quedan documentados, sin corregirse, los drifts actuales: audit/access sólo exigen autenticación en SPA/API pese a tener `CanManageUsers` en menú, y navegación sólo exige autenticación en SPA mientras su GET API exige `CanReadAch`; además el padre `/logs` conserva `CanManageUsers`. `/reports/audit` permanece exclusivamente bajo `Reportes`.

**NAV2.8D:** `Organización del menú` reutiliza el `MenuItem` existente `/navigation/menu-items` (`NavigationAdminId`) y queda como hijo directo de `Administración` con Order 40. La ruta funcional SPA se conserva: `/navigation` carga el módulo protegido por rol `Admin` y `CanManageUsers`, y su ruta interna `menu-items` muestra la administración de la estructura de navegación. El `MenuItem` conserva su asociación `Admin` + `CanManageUsers`. El API CRUD `navigation/menu-items` / `api/navigation/menu-items` exige actualmente sólo rol `Admin`; esa diferencia respecto del permiso SPA/menú queda registrada como gap y no se corrige. `Registro de navegación` continúa bajo `Registros del sistema`.

**NAV2.8E:** `/payment-rail-capability-registry` se clasifica como una consulta administrativa de gobernanza multi-riel y se publica como `Capacidades de la plataforma`, hijo directo de `Administración` con Order 50. La pantalla y su servicio Angular son estrictamente de solo lectura: consultan rieles y estados efectivos de capacidades, no editan parámetros, no simulan operaciones y no forman parte de `Herramientas de prueba`. El controller usa la policy nominal `CanViewPaymentRailCapabilityRegistry`, pero la implementación vigente de esa policy acepta `CanViewPaymentRailCapabilityRegistry`, `CanManageAch` o `CanReadAch`; por tanto, el acceso efectivo del API no es más estricto que el guard ANY del SPA. Como `CanViewPaymentRailCapabilityRegistry` no existe como fila `Permission` sembrada y NAV2.8E no puede crear permisos ni grants, el nuevo `MenuItem` reutiliza `CanReadAch` y los roles `Admin`/`ACH.Operator`, ya aceptados por SPA y API. Esto no amplía acceso respecto del backend. La discrepancia entre el nombre de la policy dedicada y su fallback efectivo, junto con la ausencia de esa policy como permiso persistido, queda para la fase futura de seguridad.

**NAV2.9:** `Herramientas de prueba` reutiliza el `MenuItem` legacy `/uat` (`UatSimulatorsId`) como raíz Order 90 y conserva sus asociaciones existentes `Admin`/`ACH.Operator` + `CanManageAch`/`CanReadAch`. `/uat/nacha-inbound-simulator` reutiliza `NachaInboundSimulatorId`, se humaniza como `Simulador de archivos recibidos` Order 10 y permanece activo sin cambiar ruta ni autorización. La segunda superficie SPA real es `/ach/nacha/soap-uat-console`: se clasifica como `Pruebas de servicios` Order 20 porque es una consola técnica read-only de preparación, candidatos, simulación/resiliencia e idempotencia para pruebas de servicios. Como no tenía `MenuItem`, NAV2.9 siembra su fila bajo la raíz técnica pero la deja `IsActive=false`; se asocia de forma conservadora a `Admin`/`ACH.Operator` + `CanManageAch` sin cambiar permisos ni grants. La inactividad evita publicar una entrada cuyo acceso SPA efectivo combina el `CanReadAch` heredado de `/ach` con `CanManageAch` de la ruta hija, conjunción que el modelo actual de autorización de menú no representa porque sus permisos se evalúan con OR. También evita que, en Production, el filtro actual que oculta sólo `/uat/**` elimine el padre pero deje flotando `/ach/nacha/soap-uat-console` como raíz. NAV3 debe introducir el permiso técnico dedicado, retirar el gating por ambiente y activar ambas superficies por autorización. Los endpoints backend-only `/api/uat/contrapartidas` y `/Tests` no tienen ruta SPA independiente y no se convierten en opciones de navegación.

**NAV2.10A:** el reconciliador final normaliza la estructura backend ya aprobada sin cambiar rutas ni seguridad. `/dashboard` se canonicaliza como `Inicio` Order 10 y los nueve dominios raíz activos quedan exactamente en 10..90. La duplicidad histórica de `/ach-cycles` se resuelve conservando `MenuItemConfiguration.AchCyclesId` como `Ciclos de operación` Order 30 bajo `Administración de parámetros`; cualquier otra fila con esa misma ruta se conserva pero queda inactiva. No se activan nuevas superficies ni se modifica Angular, permisos, roles, `RolePermissions`, grants, policies o guards.

**NAV5:** la cobertura final de rutas demuestra que `/nacha-config-admin/records` y `/nacha-config-admin/variants-fields` no son pantallas-destino: ambas cargan `NachaConfigLegacyWorkspaceRedirectComponent` y redirigen al workspace de perfiles. Por tanto, sus `MenuItem` se conservan por compatibilidad/autorización histórica pero quedan inactivos. `/nacha-config-admin/perfiles` permanece como la única pantalla `MENU` del agrupador y se normaliza a Order 10. No se cambian permisos, roles, policies, guards ni grants.


## Exclusiones explícitas del menú

- `/cenit/**` como silo de navegación.
- CENIT neteo y optimización, por decisión de alcance actual.
- `/transactions/bulk-create` (entrada técnica JSON) mientras exista la carga por archivo más amigable.
- Detalles/ediciones: usuarios, clientes, terceros, archivos, lotes, respuestas, certificados, colas.
- Rutas legacy/redirect/compatibilidad.
- Operaciones manuales individuales de cifrado/generación cuando ya estén cubiertas por una pantalla funcional agregadora.

## Diferencia semántica

- **Operación:** el usuario ejecuta una acción de negocio.
- **Seguimiento y control:** el usuario verifica resultados, colas, respuestas o conciliación.
- **Administración de parámetros:** el usuario cambia reglas/configuración funcional del negocio.
- **Reportes:** el usuario consulta y exporta vistas consolidadas; `Auditoría` es un reporte funcional distinto del registro administrativo `/audit-logs`, aunque ambos consultan datos de auditoría.
- **Administración:** el usuario administra la plataforma, accesos y registros técnicos.
- **Herramientas de prueba:** sólo personal técnico autorizado.
