# Evidencia reproducible — I02-ACH

| Evidencia | Ejecución | Resultado |
|---|---|---|
| ZIP I01 extraído íntegro | `unzip -q` | Base I01 disponible |
| Grafo `.sln` y referencias | `python scripts/validate-solution-projects.py` | PASS, 7 proyectos y 7 cargados |
| Guardas ACHCOL/Live/retorno financiero | `python scripts/test/automatic_returns_i02_ach_static_check.py` | PASS_STATIC 18/18 |
| Compilación C# .NET 10 | `dotnet build ACHInterbank.sln -c Release` | NO EJECUTADA: SDK ausente |
| Pruebas unitarias por cámara/tipo/error | `dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --filter FullyQualifiedName~AchColombiaApplicationFailureDetectorI02Tests` | AGREGADAS / NO EJECUTADAS |
| Pruebas I01, migración SQL Server, regresión | según `EVIDENCIA_I01_ACH.md` | Pendientes de ejecución externa |
| Integración real y certificación | Acuse de la contraparte/core y casos reales controlados | NO EJECUTADAS / NO-GO |

## Matriz de escenarios xUnit agregados, no ejecutados

| Caso | Esperado |
|---|---|
| Crédito / débito / prenotificación, respuesta funcional real ACHCOL, `R02` verificada | Incidencia `Detectada`, no devolución ni movimiento |
| Código del core `CORE_123` sin correspondencia Rxx | Incidencia `RevisionCausal`, causal vacía |
| Código Rxx sin catálogo de devolución válido | Incidencia `RevisionCausal`, causal vacía |
| Mismo ID de transacción repetido | Una incidencia en procesamiento normal |
| Live vs DryRun / Disabled | Solo Live permite detección |
| Identidad CENIT / origen de cola AchColFileBased | No registra incidencia de ACHCOL |
| Timeout, reintento, revisión manual, catálogo desconocido | No registra falsa falla de aplicación |

## Puerta de homologación

La validación estática comprueba presencia y estructura de guardas, no valida comportamiento C# ni integraciones reales. **La implementación está lista para compilar y probar, no homologada para producción.**
