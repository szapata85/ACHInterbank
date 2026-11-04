# Corrección de compatibilidad .NET 10 + Quartz.NET 4.7.1

**Archivo base:** `ACHInterbank_actuaizado.zip` recibido el 3 de noviembre de 2026. Se conserva `net10.0` y la referencia a SDK `10.0.403`.

## Correcciones aplicadas al código fuente

- Los tres `IJob` ahora implementan `ValueTask Execute(IJobExecutionContext, CancellationToken)`; se propaga la cancelación a los handlers.
- Se actualizaron los espacios de nombres y la configuración de Quartz 4: registro mediante Microsoft DI, configuración tipada del scheduler, almacenes RAM/SQL Server/Postgres, clustering y serialización.
- Se preservó la semántica `AUTO` del identificador de instancia con `GenerateInstanceId` para evitar identificadores repetidos en el clúster.
- Se adaptaron `SchedulerSyncService`, listeners de *misfire*, consultas de ejecuciones, comprobación de existencia, scheduling y opciones de reemplazo.
- Se sustituyeron las APIs obsoletas de instrucciones de misfire por enums tipados de Quartz 4.
- Se actualizaron los mocks de pruebas para la API `QueryJobs`/`QueryTriggers` y `ValueTask`.
- Se eliminaron referencias a paquetes Quartz antiguos que en 4.x ya se consolidaron en el paquete principal.
- El ZIP íntegro ha sido regenerado **sin** cachés locales `bin/`, `obj/`, `.vs/` ni `node_modules/`. Estas carpetas deben regenerarse en tu máquina.

## Validación obligatoria en Windows (PowerShell, raíz de la solución)

```powershell
# 1) Cierra Visual Studio antes de borrar caches locales.
Get-ChildItem -Path . -Directory -Recurse -Force |
  Where-Object { $_.Name -in @('obj','bin','.vs') } |
  Sort-Object FullName -Descending |
  ForEach-Object { Remove-Item -LiteralPath $_.FullName -Recurse -Force }

# 2) En una consola con .NET SDK 10.0.403 activo
 dotnet --version
 dotnet restore .\ACHInterbank.sln --force --no-cache
 dotnet build .\ACHInterbank.sln -c Debug --no-restore
 dotnet test .\ACHInterbank.sln -c Debug --no-build
```

Si `restore` reporta NU1901–NU1904, se debe revisar cada vulnerabilidad; `Directory.Build.props` trata estos avisos como errores por decisión del proyecto. **No** se recomienda deshabilitar `NuGetAudit` para ocultarlos.

## ADVERTENCIA ANTES DE INICIAR QUARTZ EN MODO PERSISTENTE

Quartz 4 necesita un **esquema de base de datos actualizado**: no basta con que el código compile. Si ya existen tablas `QRTZ_*` creadas por Quartz 3, realiza una copia de seguridad y ejecuta las migraciones oficiales de Quartz antes de arrancar los nodos del clúster. No se modificó ni borró ninguna base de datos desde esta entrega. Comprueba igualmente que el formato de serialización de los datos históricos sea compatible. Documentación:

- https://www.quartz-scheduler.net/documentation/quartz-4.x/migration-guide.html
- https://www.quartz-scheduler.net/documentation/quartz-4.x/quick-start.html
- https://www.quartz-scheduler.net/documentation/quartz-4.x/tutorial/querying-jobs-and-triggers.html

## Alcance de verificación

No hay `dotnet` instalado ni acceso a las dependencias NuGet dentro de este entorno. Se verificó estáticamente la estructura del proyecto, la sintaxis XML de los archivos `.csproj`, la ausencia de rutas caché en el ZIP, y se contrastaron las firmas de Quartz 4 con la documentación oficial. **No se ha ejecutado** `dotnet restore`, `build` ni `test`; esta entrega es una corrección de compatibilidad estática, no una certificación de compilación ni de regresión. Si aparecen errores restantes tras el build limpio, utiliza la salida completa del primer proyecto que falle (no únicamente los CS0006 cascada) para la siguiente corrección.

## Archivos modificados

- `src/Cfa.ACHInterbank.Api/DependencyInjectionService.cs`
- `src/Cfa.ACHInterbank.Persistence/ACH/Quartz/Jobs/DynamicJob.cs`
- `src/Cfa.ACHInterbank.Persistence/ACH/Quartz/Jobs/DynamicJobExecutor.cs`
- `src/Cfa.ACHInterbank.Persistence/ACH/Quartz/Jobs/Implementation/ProcessBulkIngestionBatchJob.cs`
- `src/Cfa.ACHInterbank.Persistence/ACH/Quartz/Jobs/NonConcurrentDynamicJob.cs`
- `src/Cfa.ACHInterbank.Persistence/ACH/Quartz/SchedulerMisfireListener.cs`
- `src/Cfa.ACHInterbank.Persistence/ACH/Quartz/SchedulerRuntimeService.cs`
- `src/Cfa.ACHInterbank.Persistence/ACH/Quartz/SchedulerSyncService.cs`
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchBulkJobScheduler.cs`
- `src/Cfa.ACHInterbank.Persistence/Cfa.ACHInterbank.Persistence.csproj`
- `src/Cfa.ACHInterbank.Persistence/DependencyInjectionService.cs`
- `src/Cfa.ACHInterbank.Persistence/Scheduler/SchedulerAdminService.cs`
- `tests/Cfa.ACHInterbank.Tests/AchColombiaOperationalQuartzScheduleTests.cs`
- `tests/Cfa.ACHInterbank.Tests/NachaUploadPostgresUatEndToEndTests.cs`
- `tests/Cfa.ACHInterbank.Tests/Quartz4SchedulerMockHelpers.cs`
- `tests/Cfa.ACHInterbank.Tests/QuartzConcurrencyPolicyTests.cs`
- `tests/Cfa.ACHInterbank.Tests/SchedulerAdministrationTests.cs`
- `tests/Cfa.ACHInterbank.Tests/SchedulerSyncServiceTests.cs`
