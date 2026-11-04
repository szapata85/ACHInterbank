# ACH Interbank — Corrección de compilación .NET 10 / Quartz 4 — Ronda 3

Fecha: 2026-11-03
Archivo base: `ACHInterbank_NET10_Quartz4_RONDA2_Corregido.zip`.

## Evidencia de entrada

El usuario ejecutó `dotnet build ACHInterbank.sln -c Debug --no-restore` y el resultado fue:

- 5 errores de compilación, 59 advertencias.
- Los proyectos Domain, Application, External, Persistence y Persistence.Migrations.SqlServer finalizaron correctamente.
- Los 5 errores pertenecen a `Cfa.ACHInterbank.Api` y el proyecto Tests queda bloqueado por esta dependencia.

## Cambios limitados a las causas raíz

1. `src/Cfa.ACHInterbank.Api/Cfa.ACHInterbank.Api.csproj`
   - Cambiado `Microsoft.OpenApi` de `3.11.0` a `2.13.0`.
   - `Microsoft.AspNetCore.OpenApi` se conserva en `10.0.14`: requiere Microsoft.OpenApi >=2.12.2 y <3.0.0.
   - Resuelve la incompatibilidad NuGet NU1608, los dos CS0200 en código generado y los dos CS1503 de OpenApiExamplesTransformer sin cambiar su lógica.

2. `src/Cfa.ACHInterbank.Api/DependencyInjectionService.cs`
   - `quartzScheduler.GetMetaData(timeoutCts.Token)` se actualizó a `quartzScheduler.GetMetadata(timeoutCts.Token)` según Quartz.NET 4.
   - Se mantiene la comprobación de persistencia, cluster y estado de salud tal como estaba.

No se modificó ningún archivo generado en `obj`, ni las migraciones, la configuración financiera, CENIT ni ACH Colombia.

## Verificación local

Desde `ACHInterbank_actuaizado`:

```powershell
dotnet --version
dotnet restore .\ACHInterbank.sln --force --no-cache
dotnet build .\ACHInterbank.sln -c Debug --no-restore
dotnet test .\ACHInterbank.sln -c Debug --no-build
```

Para probar específicamente OpenAPI tras compilar:

```powershell
dotnet test .\tests\Cfa.ACHInterbank.Tests\Cfa.ACHInterbank.Tests.csproj -c Debug --no-build --filter "FullyQualifiedName~OpenApi"
```

No editar `obj\...\OpenApiXmlCommentSupport.generated.cs`; se genera durante la compilación. Si sigue mostrando versiones anteriores tras restaurar, cerrar Visual Studio y eliminar `bin` y `obj` **solo dentro de la copia de trabajo**; después ejecutar nuevamente `dotnet restore`.

## Advertencias pendientes

Las 59 advertencias del registro se dejan intactas en esta ronda. Incluyen documentación XML (CS1573), nullability (CS860x, CS87xx) y referencias NuGet redundantes (NU1510). Priorizar posteriormente las advertencias de nulabilidad en servicios de procesamiento financiero y evitar cambios masivos sin pruebas. La corrección de NU1608 sí está incluida.

## Alcance de validación

- Comprobación estática: reemplazos únicos sobre los dos archivos, manifiesto XML válido y estructura ZIP comprobada.
- Referencia externa: NuGet Microsoft.AspNetCore.OpenApi 10.0.14 y documentación oficial Quartz.NET 4.
- No se ha ejecutado `dotnet build`/`dotnet test` aquí porque el entorno no contiene el SDK .NET. La compilación completa y la ejecución de pruebas deberán verificarse con el SDK 10.0.403 del usuario.
