# Incoming NACHA-M — UAT Operativo Controlado (Prompt 8)

Fecha base: 2026-04-25

## 1) Objetivo

Validar en ambiente controlado (sin datos reales) la operación end-to-end de inbound NACHA-M sobre capacidades ya implementadas, sin agregar nuevas funcionalidades.

## 2) Alcance validado

1. Cargue de archivo entrante.
2. Duplicidad e idempotencia de archivo.
3. Parseo y clasificación.
4. Prenotificaciones.
5. Devoluciones/returns.
6. Cola dispatch.
7. Retry técnico.
8. Rechazo funcional.
9. WaitingWindow.
10. Acciones manuales (retry/unblock/requeue/mark-failed-final).
11. `AllowedActions`.
12. Auditoría end-to-end.
13. Observabilidad (KPI/backlog/aging/top errors/timeline).
14. No regresión backend.
15. Build Angular.
16. Restricciones de seguridad frontend.

## 3) Fuera de alcance

- Nuevas features backend/frontend.
- Cambios de parser NACHA salvo bug bloqueante.
- Cambios criptográficos (`CryptoServiceScoped`, `OpenEnvelopeAsync`, `RsaKeyProvider`, `identifier`, IV, XML/AES/RSA/padding).
- Cambios de custodia de secretos / SecretRef.

## 4) Dataset UAT sintético

- Archivos NACHA sintéticos por cámara/ciclo (ACH y CENIT).
- Casos sintéticos: válido, duplicado, fuera de ventana, error técnico simulado, error funcional simulado.
- Sin datos de clientes ni certificados reales.

## 5) Matriz de escenarios UAT (ejecutable)

| ID | Escenario | Evidencia mínima | Criterio de aceptación |
|---|---|---|---|
| UAT-01 | Cargue de archivo válido | `IncomingNachaFileIngestion` + eventos | Ingesta creada con estado coherente |
| UAT-02 | Duplicidad por hash/tamaño | respuesta + registro de ingestión | Segunda carga detectada como duplicado |
| UAT-03 | Parseo/clasificación | clasificaciones persistidas | `FunctionalClass` y `EligibilityStatus` válidos |
| UAT-04 | Prenotificación | clasificación prenote + evento | Sin despacho indebido a integración |
| UAT-05 | Return/devolución | causal + transición | Política regulatoria respetada (incluye DEV14) |
| UAT-06 | Queue planning | `IncomingNachaDispatchQueue` | Item encolado con idempotencia |
| UAT-07 | Retry técnico | ejecución + estado | `RetryPending` + `NextAttemptAtUtc` |
| UAT-08 | Rechazo funcional | ejecución + estado | `FailedFinal` sin retry automático |
| UAT-09 | WaitingWindow | cola + release | `WaitingWindow` y liberación a `Queued` cuando vence |
| UAT-10 | Acción manual retry | Command Center API + evento | Solo permitido por `AllowedActions` |
| UAT-11 | Acción manual unblock | Command Center API + evento | Solo desde `Blocked` |
| UAT-12 | Acción manual requeue | Command Center API + evento | Respetando state machine |
| UAT-13 | Acción manual mark-failed-final | Command Center API + evento | No permitido desde estados terminales |
| UAT-14 | AllowedActions en SPA/API | payload API + UI | UI obedece backend, sin lógica duplicada |
| UAT-15 | Observabilidad | endpoint summary + dashboard | KPIs/timeline/top errors consistentes |
| UAT-16 | Seguridad frontend | búsqueda en código + build | Sin local/session storage sensible ni crypto.subtle |

## 6) Criterios Go / No-Go

### Go
- Todos los escenarios P0/P1 aprobados.
- Build backend Release exitoso.
- Suites críticas backend en verde.
- Build Angular exitoso.
- Evidencia trazable de auditoría y observabilidad.

### No-Go
- Falla en state machine/allowedActions de acciones manuales.
- Falla en no-regresión NACHA/Mapping/BatchNumber.
- Falla en seguridad (exposición de secretos o almacenamiento sensible en SPA).
- Falla en compilación Release.

## 7) Clasificación de hallazgos (severidad)

- **Sev-1 Crítico:** bloquea operación regulatoria o seguridad.
- **Sev-2 Alto:** afecta operación diaria sin workaround robusto.
- **Sev-3 Medio:** workaround temporal viable.
- **Sev-4 Bajo:** observación menor/documental.

## 8) Checklist operativo de ejecución

1. Ejecutar `scripts/uat/run-incoming-nacha-uat-controlled.sh`.
2. Consolidar salida de comandos (build/tests).
3. Registrar evidencias por escenario (ID, fecha UTC, resultado, artefacto).
4. Determinar Go/No-Go.
5. Documentar bloqueos de entorno sin maquillar resultados.

## 9) Evidencias obligatorias por escenario

- `ScenarioId`
- `ExecutedAtUtc`
- `Executor`
- `Command`
- `Result`
- `Artifacts/Logs`
- `OperationId/CorrelationId` (si aplica)

## 10) Nota de entorno

Si `npm test` falla por runner sin ChromeHeadless (`CHROME_BIN`) o runtime Karma/rimraf, el estado del escenario de pruebas browser se marca como **Bloqueado por entorno**, no como aprobado.
