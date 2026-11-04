# APN-I08 — Corrección incremental de vencimientos de prenotificaciones

**Fecha:** 2026-11-03  
**Base inmutable:** ZIP `ACHInterbank_PROYECTO_COMPLETO_APN_PRENOTIFICACIONES_FINAL_2026-11-03.zip` (APN-I07).  
**Tipo:** Primera corrección de código productivo posterior al cierre de auditoría documental APN-I01–I07.  
**Estado:** **IMPLEMENTADO_EN_FUENTES; VERIFICACIÓN_ESTÁTICA_EJECUTADA; PRUEBA_NET_PENDIENTE**.

## Objetivo y hallazgos abordados

- **APN-R02:** impedir que la falta de ciclos futuros reduzca inadvertidamente la ventana de cuatro ciclos de ACH Colombia.
- **APN-R01 (mitigación parcial):** impedir que la aceptación tácita se calcule en el *cutoff* del último ciclo sin considerar el plazo de entrega de archivos.
- **ACH Colombia: 7 ciclos confirmados** desde el 14-10-2026, política `ACHCOL-7C-20261014`; la referencia a 5 ciclos en V35 es histórica.
- **CENIT: 5 ciclos**; el último archivo de salida se dispone hasta 19:15 (Bogotá).

## Modificación de fuentes

| Archivo | Cambio |
|---|---|
| `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/ClearingHouseValidationPolicyService.cs` | `CycleCount`: requiere exactamente `window` sucesores; si faltan, devuelve `null` (nunca inventa un deadline). El vencimiento toma `OutputReleaseEndTime` o el valor legacy `OutputReleaseTime` **solo cuando es posterior a `CutoffTime`**. |
| `tests/Cfa.ACHInterbank.Tests/ThirdPartyAccountValidationLifecycleTests.cs` | Ajusta el test anterior de cinco ciclos históricos a la liberación final; añade cuatro escenarios de regresión con siete ciclos, faltantes, horario de liberación ausente y cambio de día. |
| `scripts/audit/prenotificaciones/test_apn_i08_deadline_guard.py` | 6 verificaciones estáticas y de referencia documental, separadas de las pruebas C#. |

## Comportamiento esperado

1. ACH Colombia: prenotificación en ciclo 1 → cuatro ciclos posteriores = ciclo 5 → vencimiento **15:30**, no 14:30, si el calendario está completo y su configuración declara liberación final hasta las 15:30.
2. ACH Colombia: si la ventana de cuatro ciclos no está completamente cargada → vencimiento `null`; la ruta de registro existente lo deriva a `TechnicalReview` y no a aceptación tácita automática.
3. ACH Colombia: una prenotificación cerca del cierre operacional puede continuar el cómputo en el día hábil siguiente con ciclos persistidos; nunca se adelanta por un `LastOrDefault()` incompleto.
4. CENIT: si la última sesión declara `OutputReleaseEndTime=19:15`, el deadline es **19:15**, no 18:45.
5. Cuando la metadata de liberación del ciclo de vencimiento no es válida (no supera el cutoff), el cálculo **falla cerrado** devolviendo `null`.

## Evidencia y límites

- Ejecutar: `python3 scripts/audit/prenotificaciones/test_apn_i08_deadline_guard.py` (pruebas del código fuente y referencias documentales; no validan C#).
- Ejecutar en entorno .NET: `dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj --filter FullyQualifiedName~ThirdPartyAccountValidationLifecycleTests`.
- **No se ha ejecutado .NET** en este entorno. Los cinco casos C# nuevos/actualizados están incorporados, pero **NO HOMOLOGADOS**.
- **No se ha comprobado** recepción real de archivos, integridad de bases de datos, ni carrera con el job.
- **Riesgo residual APN-R03:** el job usa vencimientos ya guardados en intentos anteriores a la corrección, que pueden contener el cutoff original; tampoco posee barrera de ingestión del último archivo. Requiere incremento específico posterior.
- **Riesgo residual APN-R04:** sincronización de estados ante devoluciones tardías. No modificado.
- No hay migraciones, cambios de schema, alters del scheduler, modificaciones de 7/5 ciclos ni cambios de UI.

## Criterio de aceptación pendiente

Demostrar con pruebas .NET y UAT que la aceptación tácita no ocurre antes de la liberación y procesamiento del último archivo permitido; reproducir escenarios de calendario incompleto y legado. Hasta entonces `R01` permanece **MITIGADO_PARCIALMENTE**, `R02` está **CORREGIDO_EN_FUENTE/PENDIENTE_RUNTIME**.
