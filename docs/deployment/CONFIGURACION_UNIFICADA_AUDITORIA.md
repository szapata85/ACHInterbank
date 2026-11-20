# ACHInterbank - configuración unificada y auditoría de appsettings

## Qué se cambió (19 de noviembre de 2026)

- **Único archivo de configuración de la API:** `src/Cfa.ACHInterbank.Api/appsettings.json`.
- Se eliminaron los overlays `appsettings.Development.json` y `appsettings.Production.json` para evitar que IIS/Visual Studio apliquen valores diferentes de manera inesperada. Se actualizaron los consumidores directos (`AchDbContextFactory`, `docker-ach` y el helper de Visual Studio), pruebas y montaje local de Docker Compose.
- El `appsettings.json` mantiene las opciones *seguras para operación* del anterior archivo principal: `Quartz:JobStore:Mode=Persistent`, SQL Server, simulador NACHA apagado, sin habilitar homologación CENIT ni transmitir hacia cámaras externas por defecto.
- Se trasladó `SoapIntegrationBootstrap` del perfil de producción con `OverwriteExistingSettings=false` y **endpoints vacíos**. Es un contrato de inicialización de configuración persistida en BD, NO una conexión automática a una contraparte.
- Se trasladaron las secciones `appSettings:Certificates`, `SoapTransport:ControlledLocal` y `SoapEndpointSecurity:WsAxonRespuestaTransacciones` que antes solo aparecían en Development. Los overrides de transporte Docker y de WSAXON ControlledLocal **no** se activaron automáticamente: `TransportHost`/`HostHeader` vacíos y `Mode=Unconfigured`.
- No se copió la contraseña del antiguo archivo Development ni endpoints privados/locales. **Se deben configurar de forma autorizada** en la instalación. `ConnectionStrings:SqlConnection` y `appSettings:tokenManager:secretKetJwt` se entregan vacíos.

## Auditoría estática de parámetros

Se revisaron las **tres** fuentes originales y las referencias de código bajo `src/`, scripts, despliegue y pruebas. El [inventario CSV de 157 rutas](INVENTARIO_CLAVES_APPSETTINGS.csv) enumera **todos los valores y colecciones** del archivo unificado sin incluir sus datos ni secretos. El resultado estático distingue configuración leída directamente, opciones enlazadas con `Bind`/`Configure` y valores sin consumidor identificado. No se ha instrumentado una aplicación en ejecución: la reflexión, llamadas externas, componentes futuros y configuración persistida en BD pueden usar rutas adicionales.

| Configuración | Resultado | Decisión |
| --- | --- | --- |
| `Database`, `ConnectionStrings`, `Quartz` | Consumidas por inicializador EF/DbContext y scheduler | Conservar; migraciones activas por el pedido anterior (`ApplyMigrations=true`), `ApplySeed=false` |
| `Cors`, `SpaHosting`, `RateLimiting` | Consumidas por API | Conservar; sin orígenes CORS externos por defecto; IIS activa SPA **solo en la carpeta publicada** |
| `appSettings:tokenManager`, `appSettings:servers`, `appSettings:Integrations`, `appSettings:Certificates` | Enlazadas por External/seguridad y clientes | Conservar; reemplazar marcadores de ejemplo por parámetros aprobados cuando corresponda |
| `appSettings:key`, `appSettings:address`, `appSettings:prueba` | Consumidas mediante `Configure`: cifrado, URL de recuperación y timeout WCF | Conservar a pesar del nombre legado `prueba`; renombrarlo requiere cambiar código y contrato |
| `DigitalEnvelope`, `CertificateManagement`, `SoapEnvelopeLogging` | Referenciadas por módulos de seguridad, certificados y trazas | Conservar; verificar el alcance de los logs SOAP en el entorno de despliegue |
| `AchColombiaManagedMft`, `AchOutboundReturnTransport`, `AchCol`, `IncomingNacha`, `NachaGeneration` | Leídas o enlazadas por flujos de ACH y NACHA | Conservar; las puertas de operación no se habilitaron por la unificación |
| `SoapIntegrationBootstrap` | Servicio de bootstrap lee `Enabled`, `EnforceLiveOnly`, `OverwriteExistingSettings`, operaciones y timeouts | Conservar la variante de producción, endpoints sin definir |
| `SoapEndpointSecurity`, `SoapTransport` | Enlazadas por clientes SOAP; antes solo en Development | Conservar en modo neutral: no resolver automáticamente `host.docker.internal` desde IIS |
| `NachaInboundSimulator` | Enlazado por servicios y endpoint UAT | Conservar apagado en configuración única; habilitación solo en pruebas aisladas |
| `Resilience:ExternalHttp` | Enlazado y validado al inicio de External | Conservar política sin reintentos de SOAP |
| `ProcContrapartidas:Mode`, `ProcTransacciones:Mode` | Configuración leída para control de modos | Conservar valores anteriores; `Live` **no** garantiza configuración ni habilitación de las conexiones externas |
| `OpenApi:OutputCacheMinutes` | No se encontró lectura/bind en el código actual | **Eliminado** del JSON único; OpenAPI se configura con sus propios mapeos en API |
| `appSettings:headersSecurity` | Solo se detectó la definición `SecurityHeadersOptions` en modelo; no se encontró uso ni bind activo | **Eliminado** del JSON único; no modificar cabeceras HTTP solo por este archivo |

**No se hizo una depuración agresiva** de propiedades dentro de clases con *options binding* (ej. `NachaGeneration` o `DigitalEnvelope`); una búsqueda textual por nombre de campo no basta para acreditar que un parámetro de seguridad o negocio está muerto. Los parámetros de ejemplo de `appSettings:servers` son leídos como colección; requieren revisión del operador, no eliminación silenciosa.

## Cómo usar el archivo único

### Visual Studio / desarrollo local

1. Editar `src/Cfa.ACHInterbank.Api/appsettings.json`, establecer la conexión local SQL Server y los valores de autenticación requeridos. O ejecutar `scripts\configure-visualstudio-development.ps1`, que ahora escribe **en este mismo archivo**.
2. Mantener Quartz persistente si se usa la infraestructura SQL Server. Si se elige `Quartz:JobStore:Mode=RAM` para una prueba aislada, cambiarlo deliberadamente y **revertirlo** antes de desplegar: no tiene recuperación persistente.
3. La SPA Angular en `localhost:4200` necesita agregar su origen a `Cors:Origins` si se ejecuta separada de la API; en IIS con un sitio mismo-origen, el arreglo puede seguir vacío.
4. Para pruebas SOAP **solo locales y autorizadas**, configurar `SoapEndpointSecurity:WsAxonRespuestaTransacciones:Mode=ControlledLocal`, hosts/puertos permitidos y `SoapIntegrationBootstrap:*:Endpoint` según el servidor de pruebas. No habilitar `SoapTransport:ControlledLocal:TransportHost=host.docker.internal` para IIS a menos que se use un alias de red autorizado y probado.
5. Los valores anteriores de Development (`NachaInboundSimulator:Enabled=true`, `NachaGeneration:ExecutionScope=DEVELOPMENT`, `AllowNonHomologatedCenitDevelopment=true`) **no se copiaron a un entorno común**, porque habilitarían comportamientos experimentales en IIS/producción.

### IIS

El publicador `scripts\iis\publish-iis.cmd` modifica **únicamente una copia** del `appsettings.json` en la carpeta publicada para establecer `SpaHosting:Enabled=true`. La opción `Database:ApplyMigrations=true` permanece en JSON según la decisión anterior. Tras aplicar migraciones con un procedimiento controlado, establecerla en `false` **en el JSON publicado** y reiniciar el sitio.

### Docker local

`docker-ach.cmd` y `docker-compose.yml` usan **el mismo archivo único** para la configuración base; Compose sigue usando parámetros de contenedor para conexión SQL, puertos, JWT y clustering. No es una segunda copia `appsettings`. Para `docker-ach.cmd`, la cadena SQL debe ser local (`127.0.0.1,puerto`) con usuario `sa` y contraseña válida. No distribuir una imagen Docker creada con secretos en el archivo fuente: el contexto de construcción podría incluirlos.

## Restricciones y verificación

- No usar la configuración de ejemplo como una aprobación de despliegue productivo. Variables externas de Compose, servicios Windows o políticas de IIS *todavía pueden prevalecer sobre JSON* cuando se inyectan por infraestructura; el publicador IIS no las añade.
- Restringir permisos de lectura del `appsettings.json` publicado y excluir secretos del control de versiones y del ZIP de fuente.
- Se hicieron comprobaciones estáticas y del empaquetado. **Quedan pendientes** la compilación y las pruebas de integración .NET/IIS/Docker sobre un entorno con las herramientas y una base de datos autorizada.

## Segunda revisión: comentarios y compatibilidad JSONC (19 de noviembre de 2026)

Se agregó un comentario `//` inmediatamente **antes de cada clave** (incluidas las propiedades de elementos de arreglos) en el único `src/Cfa.ACHInterbank.Api/appsettings.json`. **No se cambió ningún valor del árbol de configuración**, y las secciones que habían sido retiradas en la primera auditoría siguen ausentes. Los comentarios son intencionalmente líneas independientes; evite convertirlos en comentarios al final de un valor, porque los auxiliares de Windows PowerShell 5.1 y los validadores usan un lector conservador para esa forma.

El proveedor `Microsoft.Extensions.Configuration.Json` de ASP.NET Core acepta comentarios JSON en archivos de configuración. **El archivo deja de ser JSON estricto**, por lo que parsers genéricos `JSON.parse`, `json.loads` o lectores JSON sin soporte de comentarios necesitan un lector JSONC. Se adaptaron:

- `tools/AchInterbank.DockerCli/Program.cs`: `JsonDocumentOptions.CommentHandling=Skip`.
- `scripts/iis/publish-iis.ps1` y `scripts/configure-visualstudio-development.ps1`: funciones `scripts/jsonc-settings.ps1` (reconocen los comentarios y preservan el texto anotado al actualizar propiedades).
- `scripts/jsonc_settings.py`: lector de líneas comentadas para verificaciones Python y scripts `verify_unified_appsettings.py`, `verify_iis_hosting.py`, `automatic_returns_i06_ach_static_check.py` y `validate-r3-external-resilience.py`.
- Prueba C# que lee directamente el archivo con `JsonDocument.Parse` (`CfaOperationalRestrictionProductionIntegrationTests`): comentarios admitidos explícitamente.
- Nueva guarda `scripts/test/verify_commented_appsettings.py`: exige comentario por clave, revisa configuración crítica y comprueba adaptación de consumidores.

**Hallazgos que NO justifican cambiar valores sin un despliegue y pruebas específicos:**

| Ruta | Estado | Riesgo o decisión de configuración |
| --- | --- | --- |
| `Database:ApplyMigrations=true` | Usada | Ejecuta migraciones durante el arranque; coordinar primer inicio/reinicios y acceso a SQL. Se conserva por indicación del usuario. |
| `SoapEnvelopeLogging:Enabled=true` | **Riesgo alto confirmado** | `SoapEnvelopeJournal` escribe el XML SOAP íntegro en logs a nivel Information sin anonimización. Puede contener datos personales y bancarios. Evaluar **desactivarlo (`false`) antes de producción** y proteger históricos. Se conserva el valor original por compatibilidad. |
| `appSettings:servers:servicesWCF[].url` | Colección enlazada | Incluye direcciones HTTP de red privada repetidas; confirmar vigencia y canal seguro; no modificar automáticamente un destino contractual. |
| `appSettings:prueba=""` | **Configuración incompleta** | `ConnectionAuth.ParseTimeoutMinutes()` exige un entero positivo en minutos y falla si el valor está vacío. Configurar antes de usar el cliente WCF heredado. |
| `appSettings:tokenManager:issuerJwt/audienceJwt`, `appSettings:Integrations:*` | Colección enlazada | Hay dominios de ejemplo; no sirven como endpoints productivos. |
| `DigitalEnvelope:CertificateResolver:AllowLegacyCertificateFallback=true` | Opción enlazada | Permite compatibilidad heredada; requerir revisión de política criptográfica. |
| `DigitalEnvelope:SignatureValidation:ValidateSignerCertificateChain=false` | Opción enlazada | No implica por sí sola validación completa de cadena de confianza; validar los controles compensatorios. |
| `DigitalEnvelope:CertificateSecretResolver:EnableInMemoryProvider=true` | Opción enlazada | El control `DisableInMemoryProviderInProduction=true` debe verificarse en tiempo de ejecución productivo. |
| `NachaGeneration:FailOnResolverAmbiguity=false` | Opción enlazada | Revisar manejo de reglas ambiguas antes de homologar flujos de cámara. |
| `Quartz:JobStore:Clustered=true` | Usada | Requiere coordinación de IDs, base compartida y políticas misfire/recuperación si hay múltiples instancias. |
| `Resilience:ExternalHttp:Soap:MaxRetryAttempts=0` | Usada | Mantener sin reintentos automáticos de operaciones financieras no idempotentes. |
| `SoapIntegrationBootstrap:OverwriteExistingSettings=false` | Usada | Protege parámetros SOAP existentes en BD frente a reemplazos por bootstrap. |
| `NachaInboundSimulator:Enabled=false` | Usada | Dejar deshabilitado en producción. |

**Segunda auditoría de uso:** el inventario inicial documentó 157 rutas de valores/colecciones; el JSON comentado contiene comentarios para todas las claves, incluyendo secciones y los campos de cada objeto del catálogo de endpoints. No se encontraron nuevas evidencias suficientes para retirar más parámetros sin tocar comportamientos con enlace dinámico, reflexión o configuración persistida en BD. Los comentarios no certifican que las opciones estén correctamente provisionadas ni sustituyen pruebas reales.

**Validaciones disponibles:** `python scripts/test/verify_commented_appsettings.py`, `python scripts/test/verify_unified_appsettings.py`, `python scripts/test/verify_iis_hosting.py`, `python scripts/test/automatic_returns_i06_ach_static_check.py`, `python scripts/validate-r3-external-resilience.py`. La compilación .NET, los scripts PowerShell en Windows y la integración con SQL Server/IIS/Docker requieren ejecutarse en el entorno correspondiente.
