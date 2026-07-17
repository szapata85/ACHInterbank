# Inventario dirigido de catálogos SOAP

Fecha de corte: 2026-07-16. Alcance: interpretación de respuestas de `Proc_Contrapartidas` y `Proc_Transacciones`.

## Decisión de reutilización

Se reutilizan `IntegrationMethods`, `IntegrationCatalogBootstrapper`, `DbInitializer` y el patrón de servicios de integración. La tabla `AchResponseStatusMappings` no se usa para R96 porque representa respuestas diferenciales ACH y mezclarla violaría la separación funcional.

La brecha mínima se cerró con el subcatálogo `IntegrationResponseCodes`, relacionado con `IntegrationMethods`. No se creó otro agregado de integración ni un seeder paralelo.

## Componentes

| Responsabilidad | Componente |
| --- | --- |
| Entidad parametrizada | `IntegrationResponseCode` |
| Tabla | `IntegrationResponseCodes` |
| Configuración EF | `IntegrationResponseCodeConfiguration` |
| Bootstrap idempotente | `IntegrationCatalogBootstrapper` |
| Resolución | `IntegrationResponseCatalogResolver` |
| Historial débito | `ContrapartidaDispatchAttempts` |
| Historial crédito | `IncomingNachaIntegrationExecutions` |
| Consulta segura | `TransactionIntegrationResultService` |
| API | `GET /Transactions/{id}/integration-result` |
| SPA | `TransactionIntegrationResultComponent` |

## Índices y relaciones

La identidad única es `Source + MethodId + Code`. Ambos historiales conservan una FK opcional `ResponseCatalogId`; el código y la descripción se guardan también como snapshot histórico.

