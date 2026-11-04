# Corrección puntual de CS0103 — 2026-11-03

## Alcance

Se parte del ZIP `ACHInterbank_COMPLETO_CORRECCION_PRENOTIFICACIONES_APN_I13_2026-11-03.zip` y se atienden los **9 errores CS0103** comunicados al compilar el proyecto `Cfa.ACHInterbank.Persistence` en Visual Studio 2026.

## Causa raíz

Los cuatro símbolos existen en los proyectos Domain/Application y son accesibles desde Persistence, pero tres archivos consumidores no importaban los espacios de nombres correspondientes. No fue necesario introducir tipos nuevos ni modificar reglas de negocio.

## Cambios aplicados

| Archivo | Cambio mínimo | Errores asociados |
| --- | --- | ---: |
| `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/IncomingNachaPostParseProcessor.cs` | Añade `using Cfa.ACHInterbank.Domain.Models.ACH.Enums;` para `NachaInboundSimulationType` | 1 |
| `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchStateTransitionService.cs` | Añade `using Cfa.ACHInterbank.Domain.Models.ACH.Enums;` para `PrenotificationValidationAttemptStatus` y `PrenotificationValidationResolution` | 6 |
| `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchColombiaAutomaticReturnFileRunner.cs` | Añade `using Cfa.ACHInterbank.Application.ACH.Models;` para `RegulatoryCycleScheduleCatalog` | 2 |

No se cambió la lógica de prenotificaciones, devoluciones, simulaciones o ciclos. No se modificaron archivos de definición de enums, catálogo, pruebas, migraciones ni `.csproj`.

## Evidencia de comprobación

- Se inspeccionaron las definiciones reales:
  - `src/Cfa.ACHInterbank.Domain/Models/ACH/Enums/NachaInboundSimulationEnums.cs`
  - `src/Cfa.ACHInterbank.Domain/Models/ACH/Enums/ThirdPartyAccountValidationEnums.cs`
  - `src/Cfa.ACHInterbank.Application/ACH/Models/RegulatoryCycleScheduleCatalog.cs`
- Comparación con ZIP original: **exactamente una línea `using` añadida en cada archivo**, sin eliminaciones de líneas de código.
- `python scripts/validate-solution-projects.py`: **SOLUTION GRAPH: OK** (7 proyectos en solución, 7 en disco y todas las referencias cargadas).
- **Limitación:** no se ejecutaron `dotnet build` ni `dotnet test` porque el SDK de .NET no está disponible en este entorno. No se afirma compilación ni pruebas automatizadas exitosas.

## Validación local recomendada (CMD en Windows)

En la carpeta `ACHInterbank_ajustado`:

```cmd
python scripts\validate-solution-projects.py
dotnet build ACHInterbank.sln -c Release
dotnet test tests\Cfa.ACHInterbank.Tests\Cfa.ACHInterbank.Tests.csproj -c Release --no-build
```

En Visual Studio 2026 también se puede ejecutar **Compilar > Recompilar solución**. Registrar cualquier mensaje adicional: la corrección está acotada a los 9 errores reportados.
