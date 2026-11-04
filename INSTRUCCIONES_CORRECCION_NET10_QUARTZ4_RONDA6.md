# ACH Interbank — Corrección .NET 10 / Quartz 4 — Ronda 6

## Base de trabajo

Se parte del archivo `ACHInterbank_NET10_Quartz4_RONDA5_CORREGIDO.zip` entregado previamente. Esta ronda modifica **exclusivamente ocho archivos en `tests/Cfa.ACHInterbank.Tests`**. No se modifica código de producción ni referencias NuGet.

## Errores de compilación tratados

1. `SchedulerAdministrationTests.cs` (líneas ~206 y ~207) y `AchColombiaOperationalQuartzScheduleTests.cs` (línea ~299): los mocks para `IScheduler.PauseJob` y `IScheduler.ResumeJob` devuelven `ValueTask<bool>` en Quartz 4. Se reemplaza `ValueTask.CompletedTask` por `ValueTask.FromResult(true)`.
2. `SchedulerSyncServiceTests.cs` (línea ~420): las expresiones lambda convertidas a árboles de expresión no admiten la declaración de patrón `is IDailyTimeIntervalTrigger daily`; se usa una prueba de tipo y conversiones explícitas, conservando las mismas comparaciones.
3. `ProcTransaccionesRequestMapperTests.cs` (líneas ~31, ~75 y ~159) e `IncomingNachaPostProcessingOrchestratorTests.cs` (línea ~183): `IncomingNachaDispatchQueue.AchTransactionId` es `int?`, pero se requiere `int`. Se comprueba la existencia del valor mediante `?? throw`, sin asignar silenciosamente cero.
4. `PrenotificationTacitAcceptanceI09SafetyTests.cs` (línea ~11): se reemplaza `new(...)` por `new DateTimeOffset(...)` en una expresión con `.AddDays(1)`.
5. `S1TraceabilityMatrixTests.cs` (línea ~228) y `CertificateRuntimeWithoutSecretBackendCharacterizationTests.cs` (línea ~159): se usa `BeGreaterThanOrEqualTo`, nombre actual en FluentAssertions 8.

## Validación

Se comprobó que el ZIP es íntegro y que los ocho archivos indicados son los únicos modificados respecto de la Ronda 5. Esto es **validación estática**; no se ha ejecutado `dotnet build` ni `dotnet test` en este entorno porque no hay SDK .NET instalado. El usuario debe validar el proyecto con .NET 10.0.403.

Desde el directorio `ACHInterbank_actuaizado`:

```powershell
dotnet restore .\ACHInterbank.sln --force --no-cache
dotnet build .\ACHInterbank.sln -c Debug --no-restore
dotnet test .\ACHInterbank.sln -c Debug --no-build
```

No es necesario actualizar bases de datos ni esquemas por estos cambios, que afectan únicamente a pruebas. Las advertencias previas de nulabilidad, documentación y analizadores no se tratan aquí.
