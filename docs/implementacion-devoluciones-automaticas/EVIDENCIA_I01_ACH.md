# Evidencia I01-ACH — estado comprobable

| Comprobación | Resultado y alcance |
|---|---|
| Inventario del ZIP I00 | `zipfile`, entrada I00 verificada antes de cambios |
| Reutilización de `ClearingHouseConfigs` | Inspección directa del modelo, `ClearingHouseSeeder`, configuración EF y snapshot |
| Protección de modos | Validación de tres literales en servicio y CHECK SQL Server; inspección estática |
| Aislamiento de CENIT | Endpoint y servicio nuevos apuntan a `ACHCOL`, no a `CENIT`; test de aislamiento añadido; pendiente ejecutar |
| Auditoría | Inspección `AchDbContext.BuildAuditEntries` y `SaveChangesAsync`, prueba añadida; runtime pendiente |
| Integridad del grafo solución | `python scripts/validate-solution-projects.py`: `SOLUTION GRAPH: OK` |
| Compilación y xUnit | `NO_EJECUTADA_POR_AMBIENTE` — no existe `dotnet` disponible en este contenedor |
| Migración EF SQL Server | Código y snapshot incluidos; **NO aplicada** a una base real |
| SPA / E2E / UAT | No modificada; no aplican pruebas nuevas de interfaz; regresión general pendiente |
| Homologación/producción | NO-GO — no hay generación, transmisión ni validación externa en I01-ACH |

## Comandos para ejecutar en Windows con SDK y BD de pruebas

```cmd
python scripts/validate-solution-projects.py
dotnet build ACHInterbank.sln -c Release
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --filter FullyQualifiedName~AchColombiaAutomaticReturnSettingsI01Tests
```

Aplicar las migraciones con el procedimiento habitual del proyecto **solo en base de pruebas autorizada** y verificar `ClearingHouseConfigs.AutomaticReturnMode` para ambas cámaras antes de avanzar.

## Criterio de no engaño

La existencia de tests `.cs` en el ZIP **no prueba que pasaron**. No declarar PASS de .NET ni de SQL Server hasta aportar salida literal verificable.
