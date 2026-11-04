# MEMORY_STATE — ACH Interbank / simulador de respuesta a prenotificaciones

**Corte:** 2026-11-02  
**Paquete:** I04 — correlación NachaUpload incorporada; sin compilación/homologación runtime  
**Último ZIP íntegro vigente:** `ACHInterbank_COMPLETO_SIMULADOR_PRENOTIFICACIONES_I04_2026-11-02.zip` (derivado de I00; no reutilizar ZIP de F08 como base)  
**Directorio raíz del proyecto dentro del ZIP:** `ACHInterbank_ajustado/`  
**Continuidad:** usar siempre el **último ZIP incremental** como única base, no reconstruir el proyecto desde memoria de chat.

## Objetivo vigente

Implementar en la SPA `/uat/nacha-inbound-simulator` la **simulación de respuestas a prenotificaciones SALIENTES originadas por ACH Interbank**, dirigida a ACH Colombia y CENIT. La solución debe cubrir dos rutas, separadas y normativamente fieles:

1. **Devolución expresa:** seleccionar prenotificación original elegible; generar archivo oficial NACHA-M de devolución (monto cero, causal aplicable, traza original, nuevo trace, adenda y ciclo válido); descargarlo mediante JWT y **cargarlo manualmente por NachaUpload** para validar la correlación y el estado.
2. **Sin devolución dentro del plazo:** no crear supuestas confirmaciones positivas NACHA-M. Comprobar la **aceptación tácita** con tiempo controlado y flujo de dominio/job existente sin alterar reloj global ni estados a mano.

**Restricción:** UAT/local exclusivamente. No transmisión a cámaras/bancos reales, no movimientos monetarios, no autoimportación, no escrituras productivas. Mantener perfiles y reglas separados por ACH Colombia/CENIT. Compatibilidad con contratos históricos salvo migración explícita y probada.

## Estado verificado y afirmaciones permitidas

- La línea base contiene rutas de SPA, backend, builder, NachaUpload, `PrenotificationValidationAttempt.ResponseDeadlineAtUtc` y `AchTacitAcceptanceJobHandler` identificados en el análisis previo.
- Existe `IncomingPrenotificationResponse` en la selección/contrato pero el flujo diferencial no devolutivo está bloqueado por `DIFFERENTIAL_GENERATOR_NOT_HOMOLOGATED`; **no declararlo implementado**.
- La auditoría del simulador del 2026-11-02 registró **12 hallazgos abiertos** y 14 comprobaciones por patrones estáticos; **esas comprobaciones no equivalen a pruebas runtime**.
- **I00 = COMPLETADO** solo para documentación, seguimiento y entrega íntegra del proyecto; no constituye implementación de la funcionalidad.
- **I01 = EN_PROGRESO**: contrato, enums, 2 endpoints read-only, validación de forma, bloqueo del escenario y tests C# incorporados. **20/20 comprobaciones estáticas PASS** y grafo .sln PASS; no hay dotnet disponible para compilar/ejecutar tests C#. I02 código incorporado con 26/26 PASS_STATIC, runtime .NET pendiente; I03 e I04 código incorporado, I05–I07 pendientes. No hay verificación E2E/runtime de la nueva función.
- El proyecto tiene un `MEMORY.md` anterior y múltiples estados históricos: **no sobrescribirlos ni presentarlos como estados de este nuevo trabajo**. Este archivo documenta únicamente la iniciativa del simulador de respuestas a prenotificaciones.

## Referencias normativas provistas

- ACH Colombia `ACH-Colombia-V35.md` (abril 2026), numeral **2.10.3.1**: ausencia de devolución en el plazo se considera prenotificación exitosa.
- CENIT `CENIT-DSP-152-Anexo-2.md` (febrero 2025), numeral **4.8**: devolución en la misma fecha valor, hasta el último ciclo; vencimiento sin devolución habilita la operación posterior.
- Manual de Especificaciones NACHA-M CENIT (mayo 2026), registros de 106 caracteres, controles, adendas y tablas de códigos.
- Mantener manuales de ambos operadores sin asumir que los plazos y ciclos son iguales.

## Plan de incrementos

| Incremento | Estado al corte | Resultado faltante |
|---|---|---|
| I00 — Empaquetado y seguimiento | COMPLETADO (documental) | Sin cambios funcionales |
| I01 — Contrato y casos de uso | EN_PROGRESO | Código y documentos realizados; faltan `dotnet build` y `dotnet test` |
| I02 — Consulta y elegibilidad | EN_PROGRESO | Consulta paginada, policy y preview incorporados; compilar y ejecutar Xunit/API; reserva única en I03 |
| I03 — Archivo NACHA-M devolución | EN_PROGRESO | Endpoint y builder incorporados; falta compilar, fixtures y concurrency SQL |
| I04 — NachaUpload y reconciliación | EN_PROGRESO | Guarda de correlación y pruebas C# incorporadas; faltan compilación, fixtures, reupload y E2E |
| I05 — Sin devolución/aceptación tácita | PENDIENTE | Reloj inyectable + job probado + prevención de carreras |
| I06 — SPA completa | PENDIENTE | UX, descarga autenticada, estados/errores, E2E |
| I07 — Regresión y cierre UAT | PENDIENTE | Pruebas reales, matriz PRE-01..PRE-14 y evidencias |

## Riesgos y dependencias conocidos

1. Descarga con `window.open` puede omitir JWT; usar `HttpClient` blob y permiso Download.
2. Validación diferencial actualmente inconsistente entre selección y generación; no confiar en validación de UI.
3. Reutilización de trace/ciclo y control de respuestas duplicadas requieren comprobación rigurosa.
4. Consecutivos no atómicos y escritura de archivo/DB/evidencia fragmentada; corregir antes de cierre.
5. E2E anteriores con selectores Material desactualizados; actualizar como parte de implementación.
6. Algunos archivos de evidencia históricos contienen etiquetas de prenotificación, **no prueban homologación de la nueva ruta**.

## Instrucción inequívoca para el siguiente turno (I05)

1. Abrir el **ZIP incremental más reciente**, revisar este `MEMORY_STATE.md`, el `docs/implementacion-simulador-prenotificaciones/ESTADO_INCREMENTAL.md`, el plan y la auditoría.
2. Implementar **I05** (vencimiento sin devolución y aceptación tácita) con reloj aislado; no repetir auditoría total.
3. Ejecutar lo ejecutable. Si falta .NET, Node, Postgres u otro componente: registrar `NO_EJECUTADA_POR_AMBIENTE`, hacer verificaciones estáticas o pruebas independientes cuando sea posible, **continuar el trabajo seguro del incremento**. Nunca reportar `PASS` sin ejecución; no habilitar operación sin evidencias requeridas.
4. Actualizar el plan si cambia el diseño, `ESTADO_INCREMENTAL.md`, este `MEMORY_STATE.md`, informe y changelog; incorporar diff y pruebas en la carpeta de evidencias.
5. Entregar **el ZIP completo con TODO el proyecto** (no solo patch) y resumen: implementado, probado, pendiente, bloqueado, riesgos, siguiente incremento.
6. Si una restricción del entorno impide una prueba, no ocultarla ni usarla como excusa para detener cambios de código que sí se puedan efectuar. Si la validación final exige UAT real, señalarlo pendiente, sin falsos cierres.

## Cómo continuar en ChatGPT en modo chat

Adjuntar el ZIP incremental más reciente y escribir: «Ejecuta I05 del simulador de respuestas a prenotificaciones. Usa `MEMORY_STATE.md` y el plan interno. Entrega ZIP COMPLETO, cambios y estado». Repetir con I05, I06, etc.; cada nueva entrega sustituye a la anterior como base única. Este archivo es memoria **documental portátil**, no una modificación de la memoria del producto ChatGPT.

## Estado específico de I01 (2026-11-02)

- Nuevos símbolos: `NachaInboundSimulationType.IncomingPrenotificationReturn=10`, `PrenotificationSimulationOutcome.{Returned,NoReturnByDeadline}`, `PrenotificationSimulationContractRequest`, `PrenotificationSimulationContract`.
- Endpoints I01: `GET /api/uat/nacha-inbound-simulator/prenotifications/contract` y `POST /api/uat/nacha-inbound-simulator/prenotifications/validate-contract`, ambos protegidos por TestTools.Access + NachaSimulatorRead; **no ejecutan escenarios**.
- La validación I01 comprueba solo forma: cámara, ids, originalCycle, responseCycle según resultado, businessDate, respondingBank, reason e idempotencyKey. NO afirma que la operación existe o es elegible.
- El `GenerateAsync` anterior invoca Preview; Preview devuelve `PRENOTIFICATION_RETURN_NOT_IMPLEMENTED` para escenario 10, por lo que NO construye un supuesto NACHA. El legado escenario 3 sigue como estaba; no remapearlo implícitamente.
- Evidencias: `docs/implementacion-simulador-prenotificaciones/INFORME_I01.md`, `CONTRATO_PRENOTIFICACIONES_I01.md`, `CASOS_USO_I01.md`, `EVIDENCIA_I01.txt` y `scripts/test/prenotification_simulator_i01_static_check.py`.
- Entorno: no hay SDK dotnet; `python scripts/validate-solution-projects.py` PASS, `python scripts/test/prenotification_simulator_i01_static_check.py` PASS 20/20. Las pruebas Xunit añadidas NO EJECUTADAS.
- La funcionalidad no se activa en I01; próximas tareas: I02 elegibilidad con base de datos/idempotencia y I03 archivo NACHA-M homologado; I05 tiempo de job; I06 SPA.

## Estado específico de I02 (2026-11-02)

- Nuevos símbolos: `PrenotificationCandidateQuery`, `PrenotificationCandidatePage`, `PrenotificationBusinessEligibilityPreviewResponse`, `PrenotificationSimulationEligibilityPolicy`.
- Nuevos endpoints protegidos: `GET /api/uat/nacha-inbound-simulator/prenotifications/originated` y `POST /api/uat/nacha-inbound-simulator/prenotifications/eligibility-preview`; ambos solo lectura y UAT/local.
- Elegibilidad estricta exige transacción prenotificación saliente, cámara y entidad coherentes, trace y correlación, archivo con exportación transmitida/reconocida, único intento actual pendiente con deadline y sin respuesta previa. ACHCOL/CENIT separados por reglas; CENIT devolución misma fecha valor.
- **`BusinessEligible=true` NO habilita ejecución**. `ExecutionEnabled=false`; no se crea archivo ni cambia estado. No se reserva idempotency key ni se afirma garantía de concurrencia: I03 lo implementará dentro de una transacción con constraint UNIQUE.
- Resultado de pruebas: 26/26 `PASS_STATIC` y grafo proyectos 7/7; `dotnet` no está instalado: build/Xunit/API/BD `NO_EJECUTADA_POR_AMBIENTE`, no reportar falsamente PASS.
- Ruta de evidencia: `docs/implementacion-simulador-prenotificaciones/INFORME_I02.md`, `CONTRATO_ELEGIBILIDAD_I02.md`, `EVIDENCIA_I02.txt`, `scripts/test/prenotification_simulator_i02_static_check.py`.
- **Siguiente: I03**, reserva idempotente y retorno NACHA-M homologado. Usar I02 ZIP completo como base única.

## Estado específico de I03 (2026-11-02)

- Nuevo contrato de ejecución `POST /api/uat/nacha-inbound-simulator/prenotifications/generate-return` y `INachaInboundSimulationService.GeneratePrenotificationReturnAsync`, guardados por TestTools.Access + NachaSimulatorGenerateDifferential.
- Valida contrato, elegibilidad I02, causal y perfil homologado RETORNO/ENTRADA; crea NACHA-M mediante `BuildOfficialReturnFileAsync`, monto cero, correlación y trace nuevo, sin autoimportar ni alterar estados.
- Reserva `SimulationId` determinista SHA-256 con índice UNIQUE existente y huella de payload; usa transacción serializable para revalidación, bloqueo de archivos previos, consecutivos, escritura y evidencia. Intenta borrar el directorio físico ante rollback. No crear archivo positivo de aceptación tácita.
- Ruta genérica escenario 10 continúa bloqueada; endpoint dedicado I03 es el único que puede generar retornos de prenotificaciones.
- `PASS`: proyecto 7/7, verificaciones I01 20/20, I02 26/26, I03 31/31 `PASS_STATIC`. `NO_EJECUTADA_POR_AMBIENTE`: .NET SDK, tests C#, archivos reales, BD concurrency, importación NachaUpload. **No homologado**.
- Riesgos: bloqueo en concurrencia debe verificarse en SQL Server/PostgreSQL; fallos bruscos pueden dejar archivos físicos huérfanos; verificar semántica NACHA real y el impacto de la auditoría del builder en transacción.
- Evidencia: `docs/implementacion-simulador-prenotificaciones/INFORME_I03.md`, `CONTRATO_GENERACION_I03.md`, `EVIDENCIA_I03.txt`, `scripts/test/prenotification_simulator_i03_static_check.py`.
- Próximo I04: importación manual NachaUpload, correlación y resultados idempotentes.

## Estado específico de I04 (2026-11-02)

- Nueva guarda `IncomingPrenotificationReturnGuard` en la entrada NachaUpload, ANTES de la transición; no es un segundo aplicador.
- Correlación por trace original/adenda 99, monto cero, cámara, intento vigente, estado terminal y procedencia del archivo I03 por SHA-256.
- Hallazgo reparado: el hash `IngestAsync` es hexadecimal **minúsculo** y el hash I03 **mayúsculo**; se normaliza a mayúsculo antes de comparar. No confiar en nombre editable del archivo.
- El motor `AchStateTransitionService` ya sincroniza validación, tercero y estado; se reutiliza. Se ajustó catalogación/prueba CENIT con el flag `IsPrenotification` para originales cuyo `Type` es Credit o Debit.
- `PASS_STATIC`: I04 31/31; I01 20/20; I02 26/26; I03 31/31. Grafo de 7 proyectos PASS. Nuevos 12 casos Xunit de política y 2 pruebas de orquestación incorporados, **NO EJECUTADOS**.
- `NO_EJECUTADA_POR_AMBIENTE`: dotnet build/test, fixtures NACHA-M, E2E ACHCOL/CENIT, concurrency DB, comprobación de hash tras desencripción. No homologado.
- Evidencias y contrato: `docs/implementacion-simulador-prenotificaciones/{INFORME_I04.md,CONTRATO_INTEGRACION_I04.md,EVIDENCIA_I04.txt}` y `scripts/test/prenotification_simulator_i04_static_check.py`.
- **Próximo I05**: simular vencimiento SIN devolución ni archivo positivo; usar reloj de prueba aislado y `AchTacitAcceptanceJobHandler`. Continuar en chat aunque el ambiente carezca de .NET.
