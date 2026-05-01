# Scalar-3B — Documentación explícita Incoming NACHA Command Center (2026-05-01)

## 1. Resumen ejecutivo
Se documentaron explícitamente las 9 operaciones REST reales del módulo `IncomingNachaCommandCenterController` en OpenAPI/Scalar, reemplazando dependencia de fallback contextual y manteniendo intacta la lógica de negocio, rutas, permisos y contratos públicos.

## 2. Contexto Scalar-3
Después de Scalar-3A (Capability Registry), Scalar-3B aborda `Incoming NACHA Command Center`, módulo crítico para consulta y operación manual de inbound NACHA-M, con foco en trazabilidad, impacto operacional y seguridad.

## 3. Endpoints documentados
- `GET /incoming-nacha-command-center/observability/summary`
- `GET /incoming-nacha-command-center/ingestions`
- `GET /incoming-nacha-command-center/ingestions/{ingestionId}`
- `GET /incoming-nacha-command-center/queue`
- `GET /incoming-nacha-command-center/queue/{queueId}`
- `POST /incoming-nacha-command-center/queue/{queueId}/retry`
- `POST /incoming-nacha-command-center/queue/{queueId}/unblock`
- `POST /incoming-nacha-command-center/queue/{queueId}/requeue`
- `POST /incoming-nacha-command-center/queue/{queueId}/mark-failed-final`

## 4. Cambios realizados
- Se actualizaron `EndpointSummary` y `EndpointDescription` para los 9 endpoints con redacción explícita y no genérica.
- Se incorporó para cada endpoint: permiso, tipo (consulta/acción manual), impacto operacional, auditoría esperada, riesgos, errores esperados y precaución operacional.
- Se añadieron atributos `ProducesResponseType` con códigos esperados (200/400/401/403/404/409/500 según aplica).

## 5. Permisos documentados
- Política de clase: `CanReadAch`.
- Acciones manuales (`retry`, `unblock`, `requeue`, `mark-failed-final`): `CanManageAch`.

## 6. Acciones manuales identificadas
Acciones manuales de escritura confirmadas en el controller:
- `POST /queue/{queueId}/retry`
- `POST /queue/{queueId}/unblock`
- `POST /queue/{queueId}/requeue`
- `POST /queue/{queueId}/mark-failed-final`

Todas mantienen autorización explícita y documentación de impacto operacional.

## 7. Impacto operacional por acción
- **retry**: reintento controlado del item; riesgo de reproceso duplicado si no se valida causa raíz.
- **unblock**: desbloqueo para continuidad; riesgo de reactivar falla si no hay remediación.
- **requeue**: reencolado para recuperación; riesgo de backlog y degradación de SLA.
- **mark-failed-final**: cierre definitivo del caso; riesgo de cierre prematuro sin agotamiento de rutas de recuperación.

## 8. Validación OpenAPI real
Validación sobre `/openapi/v1.json`:
- `TOTAL_ENDPOINTS_INCOMING_COMMAND_CENTER=9`
- `SIN_SUMMARY=0`
- `SIN_DESCRIPTION=0`
- `CON_TEXTOS_GENERICOS=0`

## 9. Resultado pruebas específicas
- Descubrimiento de pruebas relacionadas (`--list-tests`): encontrado universo IncomingNacha/StateMachine/DispatchQueue/Observability.
- Ejecución con filtro amplio: **63/63 passed**.

## 10. Resultado suite completa
- Suite backend completa: **408/408 passed**.

## 11. Resultado build final
- `dotnet build ACHInterbank.sln -c Release`: exitoso.

## 12. Riesgos restantes
- Durante `dotnet run` se observaron eventos de conexión a PostgreSQL en tareas de background/scheduler; no bloquearon la generación de OpenAPI real, pero deben validarse en ambiente con DB disponible.
- Las acciones manuales continúan siendo de alto impacto y requieren disciplina operativa de aprobación y trazabilidad.

## 13. Veredicto
**Scalar-3B CERRADO** para el módulo `Incoming NACHA Command Center` con documentación explícita completa y evidencia técnica de build, OpenAPI real y pruebas.

## Matriz de endpoints

| Método | Ruta | Tipo | Acción/consulta | Permiso | Impacto operacional | Auditoría esperada | Responses | Estado |
|---|---|---|---|---|---|---|---|---|
| GET | `/incoming-nacha-command-center/observability/summary` | Consulta | Observabilidad consolidada | `CanReadAch` | Priorización operativa sin cambio de estado | Trazas de acceso correlacionables | 200,400,401,403,500 | Validado |
| GET | `/incoming-nacha-command-center/ingestions` | Consulta | Listado de ingestas | `CanReadAch` | Selección de casos para investigación | Registro de consulta para monitoreo | 200,400,401,403,500 | Validado |
| GET | `/incoming-nacha-command-center/ingestions/{ingestionId}` | Consulta | Detalle de ingesta | `CanReadAch` | Diagnóstico de un caso puntual | Evidencia de consulta por correlación | 200,401,403,404,500 | Validado |
| GET | `/incoming-nacha-command-center/queue` | Consulta | Listado de dispatch queue | `CanReadAch` | Soporta decisión de intervención manual | Trazabilidad de revisión previa | 200,400,401,403,500 | Validado |
| GET | `/incoming-nacha-command-center/queue/{queueId}` | Consulta | Detalle de item de cola | `CanReadAch` | Define viabilidad de acciones permitidas | Evidencia de debido proceso previo | 200,401,403,404,500 | Validado |
| POST | `/incoming-nacha-command-center/queue/{queueId}/retry` | Acción manual | Reintento manual | `CanManageAch` | Modifica estado/intento de reenvío | Usuario, motivo, estado previo y resultante | 200,400,401,403,404,409,500 | Validado |
| POST | `/incoming-nacha-command-center/queue/{queueId}/unblock` | Acción manual | Desbloqueo manual | `CanManageAch` | Modifica estado de bloqueo | Usuario, motivo y aprobación de desbloqueo | 200,400,401,403,404,409,500 | Validado |
| POST | `/incoming-nacha-command-center/queue/{queueId}/requeue` | Acción manual | Reencolado manual | `CanManageAch` | Reordena flujo y carga operativa | Registro de transición y justificación | 200,400,401,403,404,409,500 | Validado |
| POST | `/incoming-nacha-command-center/queue/{queueId}/mark-failed-final` | Acción manual | Cierre de falla final | `CanManageAch` | Cierra definitivamente el caso | Evidencia de aprobación y cierre | 200,400,401,403,404,409,500 | Validado |
