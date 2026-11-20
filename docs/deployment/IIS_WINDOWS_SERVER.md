# ACHInterbank — despliegue Windows Server + IIS sin Docker

## Alcance

La API utiliza **un solo** `src/Cfa.ACHInterbank.Api/appsettings.json` (sin archivos `appsettings.Development.json` ni `appsettings.Production.json`). Consulte [configuracion unificada y auditoria](CONFIGURACION_UNIFICADA_AUDITORIA.md).

Se entrega una **modalidad opcional** de despliegue en un unico sitio IIS:

- ASP.NET Core 10 y Quartz.NET (hosted services) se ejecutan en el proceso de la API.
- Angular compilado se sirve desde `wwwroot`. El publicador establece `SpaHosting:Enabled=true` en el `appsettings.json` **publicado** (no utiliza variables de entorno).
- SQL Server es una instancia normal accesible por red o local; **no se requiere Docker, nginx, ARR ni URL Rewrite**.
- Las tablas `QRTZ_*` en SQL Server se crean mediante la migracion EF Core `20261118110000_QuartzSqlServer471Schema`, ya incluida en el proyecto.
- El modo Docker existente no cambia: `SpaHosting:Enabled=false` de forma predeterminada y nginx mantiene el frontend en un contenedor separado.

**No equivale a certificacion de continuidad ni de entrega financiera exactamente una vez.** Un reciclado de IIS, una interrupcion electrica o la caida de Docker pueden interrumpir un job tras una operacion externa, con respuesta incierta. Requiere conciliacion y pruebas controladas por escenario.

## Requisitos de infraestructura

1. Windows Server con IIS instalado y **ASP.NET Core Hosting Bundle .NET 10** correspondiente a la version del runtime publicada. Si se instala el Hosting Bundle antes que IIS, repararlo/reinstalarlo despues de habilitar IIS.
2. Para compilar (servidor de build, no servidor IIS): .NET SDK 10, Node.js compatible con Angular 21 y npm.
3. SQL Server accesible desde la identidad del Application Pool; base de datos, login y permisos provisionados por el DBA.
4. Certificado HTTPS, DNS, reglas de firewall, backup/restauracion de base de datos y ubicaciones persistentes (logs, archivos de intercambio, certificados) definidas por el operador.
5. IIS Application Initialization y permisos NTFS adecuados sobre la carpeta de publicacion. Dar escritura **solo** donde sea necesaria (`logs`, directorios operativos, etc.); el directorio del codigo y `wwwroot` deben ser de solo lectura para la identidad del pool.

## Generar la carpeta de publicacion

Desde la raiz del repositorio, usando CMD en **Windows de build**:

```bat
scripts\iis\publish-iis.cmd
```

El script realiza `dotnet publish` (Release, framework-dependent, `InProcess`), `npm ci`, `npm run build -- --configuration production`, copia `dist/ach-interbank-ui/*` a `wwwroot` y habilita `SpaHosting:Enabled=true` en el `appsettings.json` **de la carpeta publicada**. El `web.config` solo mantiene la integracion IIS `InProcess`, sin `environmentVariables`.

La opcion `Database:ApplyMigrations=true` viene habilitada en `src/Cfa.ACHInterbank.Api/appsettings.json`, y se conserva en el `appsettings.json` publicado. `Database:ApplySeed=false` permanece deshabilitada. ASP.NET Core usa `Production` si no se configura otro entorno. Compruebe que no haya sobrescrituras externas de estas opciones en el servidor.

Salida predeterminada: `artifacts\publish\iis`. Debe ser una carpeta vacia (si ya existe con archivos, use otra ruta `-OutputDirectory` desde PowerShell o archivela). El script **no despliega en IIS ni altera SQL Server**. No debe confundirse este ZIP de *fuentes* con un paquete de binarios ya compilados.

Ejemplo con ruta de salida propia:

```bat
powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts\iis\publish-iis.ps1 -OutputDirectory C:\Deploy\ACHInterbank\release-001
```

Copiar esa carpeta completa a una ruta versionada de despliegue en el Windows Server. No sobrescribir un sitio activo parcialmente: detener el sitio y usar sustitucion atomica/swap de carpeta cuando corresponda.

## Configurar IIS (un sitio, sin proxy)

1. Crear Application Pool `ACHInterbank` con `.NET CLR Version = No Managed Code`, canal **Integrated**, arquitectura x64 y `Maximum Worker Processes = 1`. **No** crear un segundo proceso Quartz sobre la misma BD sin una configuracion deliberada de cluster.
2. Configurar `Start Mode = AlwaysRunning`, `Idle Time-out (minutes) = 0` y establecer en la aplicacion IIS `Preload Enabled = True`. Habilitar el componente Windows **Application Initialization**. Ajustar el reciclado a una ventana operativa definida; las interrupciones controladas siguen pudiendo detener jobs.
3. Crear un sitio HTTPS con carpeta fisica igual a la carpeta publicada y asignar el pool `ACHInterbank`. No crear otra aplicacion `/api` ni un proxy ARR: el mismo proceso sirve las rutas Angular, API, `/health/*`, `/auth/*`, etc.
4. Validar reglas de acceso, identidad del pool, directorios de escritura, reloj/huso horario, salida a SQL Server e integraciones autorizadas. No conceder escritura general a la carpeta de ejecutables.
5. Configurar `ConnectionStrings:SqlConnection`, JWT, certificados, endpoints y demas opciones necesarias en el **appsettings.json publicado**. Restringir acceso al archivo mediante ACL NTFS; nunca confirmar credenciales de produccion en Git ni distribuirlas en el ZIP de fuentes. Las opciones `Quartz:JobStore` ya tienen valores predeterminados en `appsettings.json`. Esta modalidad IIS no necesita variables de entorno; el operador debe verificar que ningun valor externo sobrescriba la configuracion JSON.

**Importante:** la aplicacion debe publicarse en la raiz del sitio (`/`); la compilacion Angular incluida tiene `<base href="/">`. Un directorio virtual o subruta requeriria configurar y probar una base distinta.

## Primera instalacion o actualizacion de SQL Server

- Realizar **backup verificado** antes de toda migracion y validar que no hay una segunda instancia migrando simultaneamente.
- Aplicar las migraciones EF Core (incluida la de Quartz) en un **paso de mantenimiento controlado** con la cadena SQL Server real configurada en el `appsettings.json` publicado. La aplicacion tiene `DatabaseInitializationHostedService` y usa `Database:ApplyMigrations=true` desde ese archivo para migrar antes de iniciar el scheduler. Una vez verificado el resultado, detener la aplicacion, poner `Database:ApplyMigrations=false` en el mismo `appsettings.json` publicado y reiniciar para las ejecuciones ordinarias. Un nuevo paquete publicado vuelve a heredar el `true` del codigo fuente: revisarlo antes de desplegarlo en produccion.
- Si se usa el arranque temporal para migrar, deshabilitar el trafico/operacion ACH durante ese paso; no dejar expuesto el sitio ni arrancar dos instancias en paralelo. Para infraestructura con multiples nodos, realizar la migracion **fuera** de los nodos de produccion mediante el procedimiento EF Core autorizado por el DBA.
- No volver a ejecutar el script de Quartz con un contenedor. La migracion SQL Server es **no destructiva** sobre tablas existentes; su `Down()` no elimina jobs y triggers. Esto **no** implica que cualquier rollback de la aplicacion o de la base sea automaticamente seguro.
- Usar `Database:ApplySeed=false` en `appsettings.json` durante el arranque normal y solo habilitar seeds bajo un procedimiento validado; no resembrar sin verificar su impacto.

## Verificaciones posteriores a la publicacion

1. `GET https://<sitio>/` y `GET /index.html`: devuelven HTML Angular, no la redireccion anterior de la API.
2. Navegar directamente a rutas Angular (ej. `/login`, `/customers`, `/transactions`, `/ach-cycles`) y refrescar la pagina: deberian devolver `index.html` (con `Accept: text/html`). Algunas de estas rutas comparten prefijo con endpoints JSON, por lo que el fallback distingue navegacion HTML de llamadas API.
3. `GET /health/live`: HTTP 200 JSON, comprueba vida del proceso **no** SQL ni Quartz.
4. `GET /health/ready`: HTTP 200 solo cuando el estado de dependencias y scheduler cumple el readiness; 503 requiere investigacion (no ocultarlo con reintentos de IIS).
5. `GET /api/ruta-inexistente` con `Accept: application/json`: 404, no HTML Angular. Asset `*.js` inexistente: 404.
6. Revisar identidad, permisos, logs protegidos, activacion del scheduler, estado de jobs, `QRTZ_*`, `SchedulerInstanceStates` y endpoints de integraciones **sin generar operaciones financieras reales**.

## Recuperacion ante caidas de IIS o servidor

- Quartz usa job store persistente SQL Server y `Clustered=true`, pero `RequestsRecovery` y la politica de *misfire* son **por tarea**. Una tarea interrumpida puede reejecutarse; no asumir *exactly once* para SOAP, SFTP ni archivos ya recibidos por la ACH.
- IIS `AlwaysRunning`/`Preload Enabled`/`Idle Time-out=0` reducen la suspencion por inactividad; **no evitan reinicios ni garantizan continuidad**. `WaitForJobsToComplete=true` aplica al apagado ordenado, pero no a una terminacion forzosa del worker process.
- Ante salida de operacion incierta **no retransmitir de inmediato**: cotejar hash, identificador de archivo, trazabilidad, acuse externo, estado de despacho y conciliacion en el sistema receptor. Registrar resolucion en el flujo de revision/conciliacion definido.
- Realizar pruebas de falla **controladas, no monetarias**, en UAT: (a) antes del envio, (b) durante la transmision, (c) despues de enviar y antes de guardar el acuse, (d) durante un reciclado IIS, (e) caida/reinicio de SQL Server. Verificar que ninguna operacion de estado incierto sea reenviada sin conciliacion, y comprobar misfires y leases.
- El codigo de ACH Colombia MFT tiene proteccion por hash cuando detecta el archivo remoto, pero **no equivale a acuse de negocio** si la contraparte ya retiro el archivo. Ese escenario requiere prueba y conciliacion para declarar un flujo seguro.

## Reversion

- Conservar el paquete y respaldos de la version anterior, ademas del backup SQL. Detener el sitio antes de cambiar el directorio fisico o volver al paquete anterior.
- Revisar compatibilidad de esquema de EF Core/Quartz con la version a restaurar. **No** eliminar `QRTZ_*` ni ejecutar `Down` de forma automatica sobre produccion.
- No iniciar jobs financieros sobre datos con estado incierto tras un rollback hasta su conciliacion.

## Limitaciones de esta entrega

No se ha compilado ni ejecutado IIS/.NET/SQL Server desde este entorno. El script de Windows, la API, el frontend y la recuperacion deben verificarse en Windows Server de UAT con un SQL Server y una contraparte controlada antes de habilitar operaciones reales. La modalidad IIS requiere los mismos servicios externos/bases de datos/configuraciones que su despliegue Docker equivalente.
