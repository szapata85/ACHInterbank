# ACTUALIZACIÓN VIGENTE — I07-R1 (2026-11-02)

**Paquete acumulativo vigente:** `ACHInterbank_COMPLETO_SIMULADOR_PRENOTIFICACIONES_I07R1_2026-11-02.zip`. Este encabezado prevalece sobre menciones históricas a I07 en el resto del archivo.

**Avance concreto:** la SPA invalida respuestas de previsualización tardías al cambiar opción, cámara, prenotificación o causal; la descarga backend valida tamaño y SHA-256 del contenido antes de registrar `Downloaded` y retorna 422 controlado si el archivo difiere. Añadidas regresiones Angular y xUnit, junto a verificador I07-R1.

**Verificaciones ejecutadas:** grafo 7/7; estáticas I01–I07 200/200; estáticas I07-R1 20/20; TypeScript transpile sintaxis 2/2. **NO ejecutadas**: .NET (sin SDK), npm install/build (caché offline carece de zone.js@0.16.3), Karma/Playwright, UAT/BD/NachaUpload. **PRE-01..PRE-14 = 0/14 homologadas**. No habilitar producción ni afirmar certificación.

**Regla de continuidad:** usar solo este ZIP íntegro para I07-R2; no rehacer ni perder I00–I07. Continuar verificaciones/correcciones seguras aunque falte ambiente; entregar siempre un ZIP completo, memoria, estado, hashes, pruebas con su resultado real y pendientes. Sin sobreingeniería, sin otros componentes UI.

**Documentación vigente:** `docs/implementacion-simulador-prenotificaciones/INFORME_I07R1.md`, `ESTADO_I07R1.md`, `MATRIZ_ACEPTACION.md`, `EVIDENCIA_I07R1.json`.

---

# MEMORY_STATE — ACH Interbank / simulador de respuesta a prenotificaciones

**Corte:** 2026-11-02  
**Paquete:** I07 — correcciones de regresión incorporadas; sin compilación Angular/.NET ni homologación runtime  
**Último ZIP íntegro vigente:** `ACHInterbank_COMPLETO_SIMULADOR_PRENOTIFICACIONES_I07_2026-11-02.zip` (derivado de I00; no reutilizar ZIP de F08 como base)  
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
- **I01 = EN_PROGRESO**: contrato, enums, 2 endpoints read-only, validación de forma, bloqueo del escenario y tests C# incorporados. **20/20 comprobaciones estáticas PASS** y grafo .sln PASS; no hay dotnet disponible para compilar/ejecutar tests C#. I02 código incorporado con 26/26 PASS_STATIC, runtime .NET pendiente; I03 e I04 código incorporado, I05 código incorporado (36/36 PASS_STATIC), I06 código UI incorporado (32/32 PASS_STATIC, pruebas Angular no ejecutadas); I07 con correcciones puntuales, 24/24 PASS_STATIC; sigue pendiente la homologación I07-R. No hay verificación E2E/runtime de la nueva función.
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
| I05 — Sin devolución/aceptación tácita | EN_PROGRESO | Código de reloj virtual/real incorporado; runtime pendiente |
| I06 — SPA completa | EN_PROGRESO | Código UI + 5 tests añadidos; build Angular, Karma, Playwright y JWT runtime pendientes |
| I07 — Regresión y cierre UAT | EN_PROGRESO | Código corregido + estáticas PASS; pruebas reales PRE-01..PRE-14 aún no ejecutadas; fase I07-R |

## Riesgos y dependencias conocidos

1. Descarga con `window.open` puede omitir JWT; usar `HttpClient` blob y permiso Download.
2. Validación diferencial actualmente inconsistente entre selección y generación; no confiar en validación de UI.
3. Reutilización de trace/ciclo y control de respuestas duplicadas requieren comprobación rigurosa.
4. Consecutivos no atómicos y escritura de archivo/DB/evidencia fragmentada; corregir antes de cierre.
5. E2E anteriores con selectores Material desactualizados; actualizar como parte de implementación.
6. Algunos archivos de evidencia históricos contienen etiquetas de prenotificación, **no prueban homologación de la nueva ruta**.

## Instrucción inequívoca para el siguiente turno (I07-R)

1. Abrir el **ZIP incremental más reciente**, revisar este `MEMORY_STATE.md`, el `docs/implementacion-simulador-prenotificaciones/ESTADO_INCREMENTAL.md`, el plan y la auditoría.
2. Continuar **I07-R** (homologación runtime de I07) sin repetir auditoría total; priorizar Angular y pruebas integradas NACHA-M/aceptación por cámara.
3. Ejecutar lo ejecutable. Si falta .NET, Node, Postgres u otro componente: registrar `NO_EJECUTADA_POR_AMBIENTE`, hacer verificaciones estáticas o pruebas independientes cuando sea posible, **continuar el trabajo seguro del incremento**. Nunca reportar `PASS` sin ejecución; no habilitar operación sin evidencias requeridas.
4. Actualizar el plan si cambia el diseño, `ESTADO_INCREMENTAL.md`, este `MEMORY_STATE.md`, informe y changelog; incorporar diff y pruebas en la carpeta de evidencias.
5. Entregar **el ZIP completo con TODO el proyecto** (no solo patch) y resumen: implementado, probado, pendiente, bloqueado, riesgos, siguiente incremento.
6. Si una restricción del entorno impide una prueba, no ocultarla ni usarla como excusa para detener cambios de código que sí se puedan efectuar. Si la validación final exige UAT real, señalarlo pendiente, sin falsos cierres.

## Cómo continuar en ChatGPT en modo chat

Adjuntar el ZIP incremental más reciente y escribir: «Continúa I07-R del simulador de respuestas a prenotificaciones. Usa `MEMORY_STATE.md` y el plan interno. Entrega ZIP COMPLETO, cambios y estado». Repetir con cada incremento posterior; cada nueva entrega sustituye a la anterior como base única. Este archivo es memoria **documental portátil**, no una modificación de la memoria del producto ChatGPT.

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

## Estado específico de I06 (2026-11-02)

- Código SPA implementado en la pantalla original, sin dependencias, rutas ni submódulos nuevos. Tres pasos: consulta por cámara/referencia, selección y tipo de respuesta, preflight/confirmación.
- Devolución: `GET originated`, `POST eligibility-preview`, `POST generate-return` con causal y ciclo desde API; se descarga blob JWT y sigue NachaUpload manual. Sin importación automática.
- Sin devolución: `POST tacit-acceptance/preview` con hora virtual opcional y `apply-due` solo si reloj REAL + API cumplen elegibilidad. Nunca NACHA positivo ficticio.
- Selección individual intencional para evitar errores de banca/ciclos y no sobreingeniería. Se oculta opción legado del selector, pero permanecen los enums/contratos y etiqueta en historial.
- Archivos modificados: service TS, component TS/HTML/SCSS/spec; ver `MANIFIESTO_I06.json`. Nuevo verificador `scripts/test/prenotification_simulator_i06_static_check.py`.
- **PASS_STATIC 32/32 I06 + 144/144 I01–I05**, **PASS** grafo de 7 proyectos. Cinco pruebas Karma incorporadas, no ejecutadas; TypeScript solo comprobó sintaxis.
- `npm ci --offline` falló ENOTCACHED `zone.js@0.16.3`; compilación Angular/Karma/Playwright NO EJECUTADA. No se certifica UX visual, seguridad JWT ni homologación NACHA end-to-end.
- **Próximo I07:** regresión, ejecución de pruebas, cumplimiento de ACHCOL/CENIT, revisión de ciclos reales de devolución, correcciones mínimas y certificación UAT con trazabilidad de pendientes.

## Estado específico I07 (2026-11-02) — ENTREGA ACTUAL

- ZIP completo I07 derivado estrictamente del ZIP I06; mantiene los archivos heredados y añade correcciones/tests/documentos I07. **Usar I07, no I06, como nueva base.**
- Corregido `ListAvailableCyclesAsync` para `IncomingPrenotificationReturn`: los ciclos configurados no requieren retornos previos; no expone ciclos cancelados; marca cantidad de retornos existentes como cero. Otros escenarios sin cambios. Preflight I02 sigue verificando ciclo/fecha.
- SPA: versiones de consulta y de catálogo para descartar respuestas tardías al cambiar cámara/búsqueda/cierre/selección; limpieza de resultados obsoletos, una prenotificación a la vez.
- Preview de aceptación tácita: condiciones `ExecutionEnabled` alineadas con `apply-due`. Reloj virtual sigue siendo solo consulta y no se crea archivo positivo.
- Tests fuente añadidos C# (ACHCOL/CENIT/cancelado) y Angular (cámara, cierre, catálogos). Verificador I07 24/24 `PASS_STATIC`; regresión I01–I06 176/176, acumulado **200/200**; grafo .sln 7/7 PASS. Ninguno es runtime.
- `NO_EJECUTADA_POR_AMBIENTE`: SDK .NET ausente; `npm ci --offline` falla por `zone.js@0.16.3` no cacheado; Angular/Karma/Playwright y UAT/BD no ejecutados. **0/14 PRE homologadas**; no habilitar producción.
- Archivos nuevos: `docs/implementacion-simulador-prenotificaciones/{INFORME_I07.md,EVIDENCIA_I07.txt,PLAN_EJECUCION_UAT_I07.md,I07_CAMBIOS_CODIGO.patch}`, `scripts/test/prenotification_simulator_i07_static_check.py`, `tests/Cfa.ACHInterbank.Tests/NachaInboundSimulatorTests.I07.cs`.
- **Próxima actividad I07-R:** ejecutar pruebas reales y corregir errores demostrados, actualizar `MATRIZ_ACEPTACION.md`, estados y MEMORY_STATE, entregar ZIP completo nuevamente. Evitar sobreingeniería y nuevas funcionalidades.
