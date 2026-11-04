# ACH Interbank - compatibilidad Quartz.NET 4 / .NET 10 - Ronda 4

## Origen

Se parte íntegramente de `ACHInterbank_NET10_Quartz4_RONDA3_CORREGIDO.zip`.

## Único cambio de código

Archivo: `src/Cfa.ACHInterbank.Api/DependencyInjectionService.cs`

En el endpoint `/health/ready`, sustituir:

```csharp
persistentStore = metadata.JobStoreSupportsPersistence ? "Healthy" : "Unhealthy";
```

por:

```csharp
persistentStore = metadata.JobStorePersistent ? "Healthy" : "Unhealthy";
```

El nombre de propiedad `JobStorePersistent` es el disponible en
`Quartz.SchedulerMetadata` de Quartz.NET 4.x:
https://www.quartz-scheduler.net/apidoc/4.x/api/Quartz.SchedulerMetadata.html

El cambio conserva la comprobación existente del almacén persistente.

## Validación realizada

- Se verificó que la referencia obsoleta aparece una sola vez en el proyecto Ronda 3.
- Se realizó sustitución exacta en ese único archivo de código.
- Se comprobaron los ZIP completos mediante verificación CRC (`testzip`).
- El resto de archivos de la Ronda 3 se conserva sin modificaciones.
- No se ejecutó `dotnet build` ni `dotnet test` en este entorno: no hay SDK .NET instalado.

## Ejecutar en Windows (.NET SDK 10.0.403)

En la raíz `ACHInterbank_actuaizado` del proyecto descomprimido:

```powershell
dotnet restore .\ACHInterbank.sln --force --no-cache
dotnet build .\ACHInterbank.sln -c Debug --no-restore
dotnet test .\ACHInterbank.sln -c Debug --no-build
```

La salida de compilación compartida para la Ronda 3 mostró 1 error en API y 57 warnings.
Los warnings no se modifican en esta ronda. Si la API compila, el proyecto Tests
podrá compilar y podrían aparecer otros errores de pruebas no visibles anteriormente.

No se altera código de transacciones, conciliaciones, reglas bancarias, programación
ni migraciones de ACH Colombia y CENIT.
