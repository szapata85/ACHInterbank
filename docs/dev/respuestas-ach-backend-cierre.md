# Cierre backend — Respuestas ACH

## 1) Resumen ejecutivo
Se implementó el backend completo para respuestas ACH: procesamiento, homologación configurable de estados/causales, auditoría de respuestas, manejo de intentos de notificación, encapsulamiento SOAP en External, endpoints API y migraciones EF Core.

## 2) Arquitectura implementada
### Domain
- `TipoRespuestaAch`
- `AchResponse`
- `AchResponseNotificationAttempt`
- `AchResponseStatusMapping`
- `AchResponseProcessingStatus`
- `AchResponseNotificationStatus`

### Application
- `ProcesarRespuestaAchUseCase`
- `NotificarRespuestaAchUseCase`
- `RespuestaAchStatusMappingService`
- `IRespuestaTransaccionesAchGateway`
- interfaces de repositorio de respuestas/intentos/mappings
- modelos de consulta paginada y detalle
- validadores de comandos
- servicio de idempotencia (`AchResponseIdempotencyHashService`)

### Persistence
- `AchResponseRepository`
- `AchResponseNotificationAttemptRepository`
- `AchResponseStatusMappingRepository`
- configuraciones EF Core
- migración y snapshot

### External
- `RespuestaTransaccionesAchGateway`
- `RegistrarRespuestaAchSoapRequestMapper` (físico SOAP)
- `RegistrarRespuestaAchSoapResponseParser` (físico SOAP)
- cliente SOAP heredado encapsulado

### API
- `AchResponsesController`
- contratos DTO request/response
- mappers API
- validadores API

## 3) Flujo funcional
### A. Procesamiento de respuesta externa
`POST /api/ach/responses/process`
1. Recibe DTO API
2. Valida request
3. Mapea a `ProcesarRespuestaAchCommand`
4. Calcula `HashIdempotencia`
5. Detecta duplicados
6. Homologa estado/causal
7. Persiste `AchResponse`
8. Crea `AchResponseNotificationAttempt` si aplica
9. Retorna estado de procesamiento

### B. Notificación de intento pendiente
`POST /api/ach/responses/notifications/send`
1. Recibe `NotificationAttemptId`
2. Busca intento pendiente
3. Mapea a `RegistrarRespuestaAchCommand`
4. Invoca `IRespuestaTransaccionesAchGateway`
5. External traduce a SOAP físico
6. Actualiza estado de intento
7. Actualiza estado de `AchResponse`
8. Retorna resultado auditado

### C. Consulta y auditoría
- `GET /api/ach/responses`
- `GET /api/ach/responses/{id}`
- `GET /api/ach/responses/{id}/notification-attempts`
- `GET /api/ach/response-status-mappings`

## 4) Endpoints disponibles
| Método | Ruta | Descripción | Request | Response | Notas |
|---|---|---|---|---|---|
| POST | `/api/ach/responses/process` | Procesar respuesta ACH | `ProcesarRespuestaAchRequest` | `ProcesarRespuestaAchResponse` | idempotencia + homologación |
| POST | `/api/ach/responses/notifications/send` | Enviar notificación de intento | `NotificarRespuestaAchRequest` | `NotificarRespuestaAchResponse` | audita error funcional/técnico |
| GET | `/api/ach/responses` | Buscar respuestas | query params | paginado | auditoría operativa |
| GET | `/api/ach/responses/{id}` | Ver detalle | id | `AchResponseDetailResponse` | incluye attempts públicos |
| GET | `/api/ach/responses/{id}/notification-attempts` | Ver intentos | id | lista pública de intentos | sin payloads técnicos |
| GET | `/api/ach/response-status-mappings` | Consultar homologaciones | filtros | lista de mappings | ruta absoluta pública |

## 5) Contratos principales
- `ProcesarRespuestaAchRequest`
- `ProcesarRespuestaAchResponse`
- `NotificarRespuestaAchRequest`
- `NotificarRespuestaAchResponse`
- `AchResponseDetailResponse`
- `AchResponseNotificationAttemptResponse`
- `AchResponseStatusMappingResponse`

Reglas de exposición:
- No se expone `idTransaccionAxon`.
- No se expone XML SOAP.
- No se exponen `RequestPayload`/`ResponsePayload` en DTO público.
- Nombre funcional interno expuesto: `IdTransaccionServicioExterno`.

## 6) Estados manejados
### `AchResponseProcessingStatus`
- Recibida
- Homologada
- Notificada
- ErrorFuncional
- PendienteReintento
- RequiereRevisionManual
- NoHomologada
- Duplicada

### `AchResponseNotificationStatus`
- Pendiente
- Exitosa
- ErrorFuncional
- ErrorTecnico
- PendienteReintento
- RequiereRevisionManual

## 7) Homologación
`AchResponseStatusMapping` permite homologar por:
- cámara de compensación
- tipo de respuesta
- estado externo
- causal externa

Con soporte de:
- vigencia (`FechaInicioVigencia` / `FechaFinVigencia`)
- `Activo`
- `RequiereCausal`
- `PermiteNotificacion`
- fallback por estado cuando causal no es requerida

## 8) Idempotencia
`HashIdempotencia` incluye:
- `TipoRespuesta`
- `CodigoCamaraCompensacion`
- `IdTransaccion`
- `CodigoEstadoExterno`
- `CodigoCausalExterna`
- `IdTransaccionServicioExterno`
- `CodigoEntidadOrigen`
- `CodigoEntidadDestino`

No incluye:
- `FechaRecepcion`
- `CorrelationId`
- `DescripcionCausalExterna`

## 9) Persistencia y migraciones
Migración principal:
- `20260509224122_AddAchResponseStatusMappingsAndAuditTables`

Tablas:
- `AchResponseStatusMappings`
- `AchResponses`
- `AchResponseNotificationAttempts`

Índices clave:
- `UX_AchResponses_HashIdempotencia`
- `UX_AchRespAttempts_Response_Attempt`
- índices de consulta y vigencia

Script SQL generado:
- `artifacts/sql/AddAchResponseStatusMappingsAndAuditTables.sql`

Referencia:
- `docs/dev/migraciones-respuestas-ach.md`

## 10) Comandos operativos
```bash
dotnet tool restore
dotnet restore ACHInterbank.sln
dotnet build ACHInterbank.sln -c Release --no-restore
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --no-build
```

Migraciones:
```bash
dotnet tool run dotnet-ef migrations list --project src/Cfa.ACHInterbank.Persistence/Cfa.ACHInterbank.Persistence.csproj --startup-project src/Cfa.ACHInterbank.Api/Cfa.ACHInterbank.Api.csproj --context AchDbContext

dotnet tool run dotnet-ef database update --project src/Cfa.ACHInterbank.Persistence/Cfa.ACHInterbank.Persistence.csproj --startup-project src/Cfa.ACHInterbank.Api/Cfa.ACHInterbank.Api.csproj --context AchDbContext
```

## 11) Pruebas existentes
Familias de pruebas:
- contratos Application
- homologación
- repositorios EF
- auditoría
- procesamiento
- notificación
- gateway SOAP External
- contratos API
- controller endpoints
- pruebas arquitecturales anti-contaminación

Estado esperado actual:
- Passed: 580
- Skipped: 1
- Failed: 0

## 12) Decisiones arquitecturales
- Domain no depende de Application.
- SOAP físico encapsulado en External.
- Application usa gateway abstracto.
- API no usa DbContext directo en controller.
- `RequestPayload`/`ResponsePayload` no se exponen públicamente.
- WebApplicationFactory se descartó temporalmente por costo/beneficio en esta fase.

## 13) Pendientes controlados
- Validar migración contra PostgreSQL real en ambiente de integración (Docker/CI dedicado).
- Reintentar pruebas HTTP de integración en fase posterior con host aislado desde cero.
- Reducir warnings de nullable gradualmente.
- Definir seed funcional de homologaciones ACH/CENIT con insumo confirmado.
- Implementar CRUD administrativo de mappings si lo solicita cliente.
- Alinear SPA Angular después del cierre backend.

## 14) Checklist de cierre backend
- [x] Build verde
- [x] Test verde
- [x] Migraciones generadas
- [x] Script SQL generado
- [x] Endpoints definidos
- [x] DTOs neutrales
- [x] SOAP encapsulado
- [x] Sin `idTransaccionAxon` fuera de External/tests
- [x] Sin tocar SPA
- [x] Pendientes documentados

## 15) Próximo paso recomendado
Siguiente fase: análisis y alineación del SPA Angular como **Command Center ACH**.

No iniciar implementación SPA hasta validar endpoints y migración en ambiente controlado con el cliente.

Referencias:
- `docs/dev/respuestas-ach-backend-cierre.md`
- `docs/dev/migraciones-respuestas-ach.md`
