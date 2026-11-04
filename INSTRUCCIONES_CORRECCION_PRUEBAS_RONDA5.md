# ACH Interbank – Ronda 5: corrección de atributo duplicado xUnit

Fecha: 2026-11-03

## Fuente de la corrección

- Base exacta: `ACHInterbank_NET10_Quartz4_RONDA4_CORREGIDO.zip`.
- Reporte de compilación aportado: `dotnet build .\ACHInterbank.sln -c Debug --no-restore`.
- Los proyectos Domain, Application, External, Persistence, Persistence.Migrations.SqlServer y Api compilaron; el proyecto Tests falló con `CS0579 Duplicate 'Fact' attribute`.

## Cambio aplicado

Archivo: `tests/Cfa.ACHInterbank.Tests/NotificarRespuestaAchUseCaseTests.cs`.

Ubicación: líneas 238–239 del archivo original.

Antes:

```csharp
    [Fact]
    [Fact]
    public async Task RegistrarRespuestaTransaccion_ShouldFailControlled_WhenRequiredMappingMissing()
```

Después:

```csharp
    [Fact]
    public async Task RegistrarRespuestaTransaccion_ShouldFailControlled_WhenRequiredMappingMissing()
```

Se eliminó exclusivamente la segunda ocurrencia consecutiva de `[Fact]`. No se modificaron las aserciones ni la lógica de la prueba.

## Verificación estática

- Se comprobó que la ocurrencia de `[Fact]` duplicada era única en los `.cs` de `tests/` de la Ronda 4.
- Se comprobó que el proyecto íntegro permanece igual salvo este archivo y esta nota añadida.
- Se comprobó la integridad de los archivos ZIP generados.
- No se ejecutaron compilación ni pruebas en este entorno: aquí no se dispone del SDK .NET.

## Instrucciones (desde la raíz `ACHInterbank_actuaizado`)

```powershell
dotnet restore .\ACHInterbank.sln --force --no-cache
dotnet build .\ACHInterbank.sln -c Debug --no-restore
dotnet test .\ACHInterbank.sln -c Debug --no-build
```

Si se utiliza el ZIP de *solo cambios*, copiar el archivo corregido a su ruta relativa dentro del proyecto de la Ronda 4, sustituyendo el existente.

## Observaciones

Las 57 advertencias de compilación listadas por el usuario son independientes del error CS0579. Las advertencias de nulabilidad en el procesamiento financiero no se suprimieron ni corrigieron sin su revisión específica.
