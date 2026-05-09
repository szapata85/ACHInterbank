# Análisis Fase 1 — Integración de Registro de Respuestas ACH (SOAP)

## 1) Mapa actual del proyecto
- Capas detectadas: `Api`, `Application`, `Domain`, `Persistence`, `External`, `Tests`.
- Proyectos `.csproj` detectados:
  - `src/Cfa.ACHInterbank.Api/Cfa.ACHInterbank.Api.csproj`
  - `src/Cfa.ACHInterbank.Application/Cfa.ACHInterbank.Application.csproj`
  - `src/Cfa.ACHInterbank.Domain/Cfa.ACHInterbank.Domain.csproj`
  - `src/Cfa.ACHInterbank.Persistence/Cfa.ACHInterbank.Persistence.csproj`
  - `src/Cfa.ACHInterbank.External/Cfa.ACHInterbank.External.csproj`
  - `tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj`
- Patrón observado: Clean Architecture con registro por capas en `Program.cs` (`AddApplication`, `AddPersistence`, `AddExternal`).
- Pruebas: xUnit + pruebas unitarias/integración focalizadas (parsers SOAP, mappers, jobs, idempotencia).
- Quartz: configurado en Persistence (`AddQuartz`, `AddQuartzHostedService`, `SchedulerSyncService`).

## 2) Inventario de código relacionado
### SOAP / clientes externos
- Existe contrato Application para Axon: `IWsAxonRespuestaTransaccionesSoapClient`.
- Existe implementación Infrastructure/External: `WsAxonRespuestaTransaccionesSoapClient`.
- Existe cliente para `Proc_Transacciones` / `Proc_Contrapartidas`: `WscfaachSoapClient`.
- Existe parametrización dinámica SOAP en DB: `SoapIntegrationSetting` + `SoapIntegrationSettingsService`.

### Flujo transacciones/prenotas/respuestas
- Orquestador principal de post-proceso NACHA entrante:
  - `IncomingNachaPostProcessingOrchestrator`.
  - Usa mapper `IProcTransaccionesRequestMapper`, parser `IProcTransaccionesResponseParser`, cliente `IWscfaachSoapClient`.
- Mappers/parsers existentes:
  - `ProcTransaccionesRequestMapper`
  - `ProcTransaccionesResponseParser`
  - `ProcContrapartidasResponseParser`
- Prenotificación:
  - `PrenotificationHandler`
  - `IncomingNachaPrenotificationResolver`

### Reintentos, idempotencia, auditoría
- Reintentos técnicos existentes en `IncomingNachaPostProcessingOrchestrator` (`RetryPending`, backoff, jitter por opciones).
- Idempotencia existente en cola de despacho NACHA entrante (`IdempotencyDispatchKey`) y prueba relacional de unicidad.
- Auditoría/traceabilidad existente:
  - `AuditLog`, `AuditLogsService`
  - `IncomingNachaIntegrationExecution` (request/response hash/xml, códigos, tiempos).

### DI / appsettings / jobs
- DI por reflection + atributos `[Scoped]/[Singleton]` en Persistence.
- DI External con pipeline de resiliencia HTTP (`Retry`, `CircuitBreaker`, `RateLimiter`, `Bulkhead`).
- `appsettings.json` ya incluye bloques `IncomingNacha:DispatchResilience`, `Database`, `NachaGeneration`, etc.
- Jobs Quartz ACH activos en `src/Cfa.ACHInterbank.Persistence/ACH/Quartz/Jobs/Implementation`.

## 3) Diagnóstico de reutilización (resumen)
- **Reutilizar y extender**:
  - `IWsAxonRespuestaTransaccionesSoapClient` + `WsAxonRespuestaTransaccionesSoapClient`.
  - `SoapIntegrationSetting` y `SoapIntegrationSettingsService` (ya soportan mapping para `RegistrarRespuestaTransaccion`).
  - Infraestructura de resiliencia/log y patrón de `IncomingNachaPostProcessingOrchestrator`.
- **Refactorizar**:
  - Nomenclatura Axon en Application (`IWsAxon...`) para evitar contaminación de proveedor fuera de mapper/cliente físico.
  - `ProcTransaccionesResponseParser` y/o homologación de códigos para eliminar catálogos hardcodeados.
- **Extender**:
  - Persistencia de intentos de notificación de respuesta ACH (tabla tipo `AchResponseNotificationAttempt`).
  - Homologación configurable de estados/causales por cámara/canal.
- **Reemplazar parcialmente**:
  - Estrategia de `new HttpClient()` local en `WsAxonRespuestaTransaccionesSoapClient` por `IHttpClientFactory` tipado y pipeline resiliente consistente con External.

## 4) Brechas identificadas
- Falta caso de uso Application explícito para registrar respuesta ACH de prenota/transacción con contrato funcional estable.
- Falta gateway funcional neutro (`IRespuestaTransaccionesAchGateway`) desacoplado de nombre físico proveedor.
- Falta modelo de homologación configurable de estados/causales específico para este flujo (no hardcode).
- Falta persistencia explícita de intento/resultado de notificación para trazabilidad e idempotencia de envío al servicio externo.
- Falta política formal de idempotencia de notificación (clave natural + constraint + dedupe).
- Falta suite TDD dedicada al caso de uso de respuesta ACH (funcional/tecnológico/arquitectural).

## 5) Diseño de adaptación (sin duplicación)
- Adaptar flujo existente de post-procesamiento para desacoplar “resultado de procesamiento” vs “notificación de respuesta a externo”.
- Mantener SOAP solo en Infrastructure/External.
- Application debe introducir contratos funcionales neutros:
  - `RegistrarRespuestaAchCommand`
  - `ResultadoRegistroRespuestaAch`
  - `IRespuestaTransaccionesAchGateway`
- Infrastructure mapeará a request físico SOAP (`idTransaccionAxon`) exclusivamente en mapper/cliente.
- Persistencia:
  - nueva entidad funcional de respuesta/attempts (nombres neutrales), con `IdTransaccionServicioExterno` (no `idTransaccionAxon`).
  - tabla de homologación configurable (`AchResponseStatusMapping`).

## 6) Estrategia TDD (primero pruebas)
1. Tests de caracterización del comportamiento actual de `WsAxonRespuestaTransaccionesSoapClient`, `ProcTransaccionesResponseParser`, `IncomingNachaPostProcessingOrchestrator`.
2. Tests de contratos Application (sin SOAP/XML).
3. Tests de homologación configurable (estados/causales por cámara/canal).
4. Tests de idempotencia (no duplicar envío ante misma clave).
5. Tests de reintentos técnicos (timeout/5xx/soap fault servidor).
6. Tests de errores funcionales (rechazo no retryable).
7. Tests de mapeo Infrastructure (propiedad física `idTransaccionAxon` <-> `IdTransaccionServicioExterno`).
8. Test arquitectural: Domain/Application no referencian `System.Xml*`, namespaces SOAP, ni nombres físicos del proveedor.

## 7) Plan de commits pequeños
1. Caracterización pruebas existentes flujo actual.
2. Contratos Application neutrales.
3. Homologación configurable + pruebas.
4. Persistencia/auditoría intentos + pruebas.
5. Adaptación cliente SOAP Infrastructure + pruebas de mapeo y resiliencia.
6. Integración caso prenotas.
7. Integración caso transacciones.
8. Reintentos/idempotencia end-to-end.
9. Limpieza y eliminación de código obsoleto.
10. Pruebas finales + documentación técnica.

## 8) Riesgos técnicos
- Romper `IncomingNachaPostProcessingOrchestrator` y jobs Quartz dependientes.
- Duplicar lógica entre `Proc_Transacciones` y nuevo registro de respuestas.
- Contaminar Application con nombres de proveedor.
- Homologación insuficiente que mezcle estados/causales entre cámaras.
- Reprocesamiento por falta de constraint/idempotency key.
- Pérdida de respuesta si falla externo sin outbox/attempt log.
- Errores de namespace SOAPAction o envelope.
- Fallas DI por renombres/refactors.
- Riesgo EF en migraciones por tablas nuevas y constraints únicos.

## 9) Archivos candidatos a modificar en fase de implementación
- Application:
  - `src/Cfa.ACHInterbank.Application/External/Connections/IWsAxonRespuestaTransaccionesSoapClient.cs` (evaluar renombre o fachada).
  - `src/Cfa.ACHInterbank.Application/ACH/...` (nuevos contratos funcionales).
- External:
  - `src/Cfa.ACHInterbank.External/Connections/WsAxonRespuestaTransaccionesSoapClient.cs`.
  - `src/Cfa.ACHInterbank.External/DependencyInjectionService.cs`.
- Persistence:
  - `src/Cfa.ACHInterbank.Persistence/Security/Services/SoapIntegrationSettingsService.cs`.
  - `src/Cfa.ACHInterbank.Persistence/DataBase/AchDbContext.cs`.
  - `src/Cfa.ACHInterbank.Persistence/Configuration/*` (nuevas entidades/configs).
  - `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/IncomingNachaPostProcessingOrchestrator.cs` (adaptación controlada).
- Api:
  - `src/Cfa.ACHInterbank.Api/appsettings*.json` (config endpoint/acciones/políticas).

## 10) Siguiente paso recomendado
- Ejecutar Commit 1 (caracterización) sin tocar código productivo funcional, validando baseline de comportamiento actual para SOAP/parsers/orquestador antes de cualquier refactor.
