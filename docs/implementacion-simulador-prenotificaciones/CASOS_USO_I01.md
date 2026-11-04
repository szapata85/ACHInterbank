# I01 — Matriz de casos de uso del contrato

| Caso | Resultado de forma I01 | Efecto I01 | Validación pendiente |
|---|---|---|---|
| UC-01 ACHCOL: devolución con IDs, ciclos, causal y clave | `ShapeValid=true` | Ninguno | I02 elegibilidad; I03 archivo; I04 ingesta |
| UC-02 CENIT: devolución con IDs, ciclos, causal y clave | `ShapeValid=true` | Ninguno | I02/I03 normas CENIT; I04 ingesta |
| UC-03 ACHCOL: sin devolución, sin causal ni ciclo respuesta | `ShapeValid=true` | Ninguno | I05 deadline ACHCOL |
| UC-04 CENIT: sin devolución, sin causal ni ciclo respuesta | `ShapeValid=true` | Ninguno | I05 fin de jornada CENIT |
| UC-05 Sin IDs / IDs no positivos | `ShapeValid=false` | Ninguno | N/A |
| UC-06 IDs repetidos | `ShapeValid=false` | Ninguno | N/A |
| UC-07 Devuelta sin causal o sin clave idempotencia | `ShapeValid=false` | Ninguno | N/A |
| UC-08 Devuelta sin ciclo respuesta | `ShapeValid=false` | Ninguno | N/A |
| UC-09 Sin devolución con causal / ciclo respuesta | `ShapeValid=false` | Ninguno | N/A |
| UC-10 Outcome desconocido o cámara ajena | `ShapeValid=false` | Ninguno | N/A |
| UC-11 Nuevo escenario `IncomingPrenotificationReturn=10` en preview/generate | `PRENOTIFICATION_RETURN_NOT_IMPLEMENTED` | No genera ni modifica estados | Desbloquear de forma controlada I02–I03 |
| UC-12 Legado `IncomingPrenotificationResponse=3` | Se conserva su comportamiento/bloqueo anterior | No se habilita ni remapea | Migración explícita I06 |
| UC-13 Referencia de otra cámara/banco, transacción no enviada, duplicado de respuesta, causales no aplicables | Fuera del alcance de validación de forma | Ninguno | I02 exige consulta y bloqueo en API |
| UC-14 Repetición/concurrencia o timeout de carga | Fuera del alcance de I01 | Ninguno | I02/I03/I04, transiciones idempotentes |

**Criterio de aceptación I01:** contratos y ejemplos versionados, valores legado conservados, API protegida de lectura sin ejecución, rama nueva fail-closed y pruebas de contrato incorporadas. La compilación/test suite C# sigue pendiente si falta SDK .NET en modo chat. La ausencia de dotnet **no impide avanzar a I02**, pero impide declarar verificado el comportamiento runtime.
