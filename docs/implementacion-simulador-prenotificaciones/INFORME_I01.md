# I01 — Informe de implementación y verificación

**Fecha:** 2026-11-02  
**Origen inmutable:** `ACHInterbank_COMPLETO_SIMULADOR_PRENOTIFICACIONES_I00_2026-11-02.zip`  
**Entrega:** `ACHInterbank_COMPLETO_SIMULADOR_PRENOTIFICACIONES_I01_2026-11-02.zip`  
**Estado:** `EN_PROGRESO` (código completo para I01, pruebas C# pendientes por ambiente).

## Resultado real

- Nuevo enum explícito `IncomingPrenotificationReturn = 10` sin reasignar `IncomingPrenotificationResponse = 3`.
- Resultado tipado `Returned` frente a `NoReturnByDeadline`, contrato de solicitud con identificadores internos de prenotificación y ciclos independientes.
- Validador de forma puro sin consultas/efectos, dos endpoints de lectura sujetos a permisos existentes.
- Guardrail fail-closed en Preview (y por ende Generate) de la operación aún no implementada; no generador improvisado.
- Tests Xunit incorporados; guía de contrato y 14 casos de uso, documentación y memory state actualizados.

## Pruebas ejecutadas (no confundir alcance)

| Prueba | Comando | Resultado | Alcance |
|---|---|---|---|
| Grafo .sln | `python scripts/validate-solution-projects.py` | PASS (`SOLUTION GRAPH: OK`, 7 proyectos) | estructura de proyecto, no build |
| Verificador I01 | `python scripts/test/prenotification_simulator_i01_static_check.py` | PASS_STATIC (20/20) | invariantes textuales/estructurales, no ejecución C# |
| Compilación | `dotnet build ACHInterbank.sln -c Release` | NO_EJECUTADA_POR_AMBIENTE (`dotnet: command not found`) | verificar con SDK .NET 10 |
| Xunit I01/regresión | `dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --filter "FullyQualifiedName~PrenotificationSimulatorContractI01Tests|FullyQualifiedName~I01_NewPrenotificationReturnMustFailClosedWithoutCreatingFileOrState"` | NO_EJECUTADA_POR_AMBIENTE (sin `dotnet`) | pendiente ejecución real |
| UAT API/BD / SPA | N/A I01 | NO_APLICA a contrato en lectura; ejecución permanece inhabilitada | I02–I07 |

## Riesgos pendientes y estado de operación

1. No se ha compilado C#; antes de habilitar I02/I03 revisar build/regresión contra SDK de proyecto.
2. La validación de forma (`ShapeValid`) **no** verifica que la referencia exista, que corresponda al banco o que esté pendiente; I02 hará elegibilidad con idempotencia.
3. No se resolvieron en I01 consecutivos, NACHA-M de devoluciones, carga NachaUpload, job de aceptación tácita ni SPA.
4. No se habilitó uso real, no existe archivo de aprobación positiva ni envío/autoimportación.
5. Conservar auditoría anterior de 12 hallazgos; este incremento no afirma haberlos cerrado.

## Condiciones restantes para cerrar I01

- Ejecutar `dotnet build` y los tests Xunit I01 con éxito y conservar logs de la versión real.
- Validar serialización HTTP real del enum y permisos read-only en API con entorno UAT si aplica al cierre de release.

**Siguiente incremento:** I02 — consulta y elegibilidad de prenotificaciones efectivamente originadas en backend, sin abrir generación hasta I03.
