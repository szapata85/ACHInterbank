# I02 — Implementación de consulta y elegibilidad de prenotificaciones salientes

**Corte:** 2026-11-02 · **Base:** I01 acumulativo · **Estado:** EN_PROGRESO (código incorporado, SDK .NET no disponible).

## Cambios de código efectivos

1. Se añadieron dos endpoints protegidos bajo `TestTools.Access` (clase) y `P1Policies.NachaSimulatorRead` (métodos):
   - `GET /api/uat/nacha-inbound-simulator/prenotifications/originated?ClearingHouseCode=ACHCOL&Page=1&PageSize=20`
   - `POST /api/uat/nacha-inbound-simulator/prenotifications/eligibility-preview` con DTO I01.
2. DTOs `PrenotificationCandidateQuery`, `PrenotificationCandidateDto`, `PrenotificationCandidatePage`, `PrenotificationBusinessEligibilityPreviewResponse`: lista paginada, 100 por página como máximo, correlación y cuenta enmascarada.
3. Política pura `PrenotificationSimulationEligibilityPolicy` para evaluar tipo, origen, destino externo, dirección, estado pendiente, trace e identificador externo, constancia de exportación transmitida/reconocida, un único intento actual `AwaitingResponse`, estado de validación `PendingResponse`, ausencia de respuesta y deadline conocido. Resultado Returned exige no haber vencido; NoReturnByDeadline exige plazo alcanzado.
4. Consulta desde `AchTransactions`, `AchFileExportTransactions`, `PrenotificationValidationAttempts`, `ThirdPartyAccountValidation` y `AchResponses`, siempre `AsNoTracking`. Se exige coincidencia de cámara, ciclo, trace y pertenencia de la exportación; no se presupone que crear el archivo implica transmitirlo.
5. Vista previa verifica pertenencia de cada transacción a cámara/ciclo/banco, que el ciclo de respuesta esté en la cámara/fecha pedidas y que sea posterior al original; en CENIT se exige mismo día valor. No registra idempotencia ni modifica ningún estado.
6. El contrato I01 ahora acota a 100 IDs por petición. Se conserva sin cambios de valor el enum legado `IncomingPrenotificationResponse=3` y el bloqueo de `IncomingPrenotificationReturn=10` dentro de Generate/Preview anteriores.
7. Se añaden tests C# puros de política, integrados SQLite y un script estático de evidencia.

## Límite funcional que NO se oculta

- **`BusinessEligible=true` significa solamente una opinión de consulta sobre el snapshot actual.** `ExecutionEnabled=false` siempre en I02. No genera NACHA, no llama NachaUpload, no simula el reloj, no reserva idempotencyKey.
- **Idempotencia/concurrencia definitiva:** I03 debe crear una reserva durable UNIQUE ligada a `(camara, escenario, clave)` con verificación del payload y revalidar los datos dentro de la misma transacción; nunca confiar únicamente en Preview. I04 verifica reimportación.
- La selección estricta puede excluir prenotificaciones históricas sin vínculo `AchFileExportTransaction` + transmisión acreditada o sin `PrenotificationValidationAttempt` actual; se trata de un bloqueo deliberado, no de aprobación por inferencia.
- La UI no se modifica hasta I06. El deadline se lee de la política persistida existente, no se calcula ad hoc con zonas horarias.
- Se revisará al compilar si las pruebas SQLite requieren ajustes a fixtures. En este entorno no se ejecuta dotnet.

## Pruebas y evidencias

| Evidencia | Resultado | Clasificación |
|---|---|---|
| `python scripts/validate-solution-projects.py` | 7 proyectos/7, PASS | Grafo estructural, no build |
| `python scripts/test/prenotification_simulator_i02_static_check.py` | 26/26, PASS_STATIC | Texto y estructura, no C# runtime |
| `python scripts/test/prenotification_simulator_i01_static_check.py` | repetir sobre I02, ver `EVIDENCIA_I02.txt` | regresión estática |
| `dotnet build ACHInterbank.sln -c Release` | NO_EJECUTADA_POR_AMBIENTE (SDK ausente) | Compilación pendiente |
| `dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --filter FullyQualifiedName~PrenotificationSimulatorEligibilityI02` | NO_EJECUTADA_POR_AMBIENTE | Xunit pendiente |
| API/BD UAT + Playwright | NO_EJECUTADA_POR_AMBIENTE | Revisión de integración pendiente |

## Riesgos y pendientes antes de habilitar

- I01 e I02 no tienen compilación ni pruebas Xunit reales. No declarar release apta.
- Ejecutar integración para ambos operadores y verificar plan de consultas en PostgreSQL real; comprobar límites de tiempo y respuesta bajo concurrencia.
- I03 implementará reserva idempotente y archivo NACHA-M; I04 verificará correlación de NachaUpload, I05 plazo, I06 SPA, I07 regresión y cierre.
- Los 12 hallazgos de auditoría heredados no se declaran resueltos por I02.
