# Scalar-3E-A — Revalidación OpenAPI real de AchTraceabilityController (2026-05-01)

## 1. Resumen ejecutivo
Se revalidó en runtime la generación de OpenAPI posterior a Scalar-3E. Se descargó exitosamente `/openapi/v1.json` y se verificó `AchTraceabilityController` con sus 3 endpoints esperados, `summary`/`description` completos, sin textos genéricos y con responses publicados.

## 2. Contexto del bloqueo de Scalar-3E
Scalar-3E quedó parcial porque en esa ejecución la API no quedó disponible al intentar descargar OpenAPI, con trazas de conexión rechazada a PostgreSQL en servicios de background.

## 3. Estrategia usada para levantar API
Se aplicó estrategia no invasiva con configuración existente:
- `ASPNETCORE_ENVIRONMENT=Development`
- `Database__ApplyMigrations=false`
- `dotnet run` de la API y descarga de OpenAPI en `http://127.0.0.1:5194/openapi/v1.json`.

No se modificó código, permisos, rutas ni contratos.

## 4. Resultado de inspección PostgreSQL/hosted services
Hallazgos relevantes en código/configuración:
- `SchedulerSyncService` está registrado como hosted service (`AddHostedService<SchedulerSyncService>()`) y puede intentar acceso a base durante arranque.
- También existen `AddQuartz` y `AddQuartzHostedService`.
- Se encontró bandera real `Database:ApplyMigrations` para evitar migraciones startup.
- `MapOpenApi` y `MapScalarApiReference` están habilitados con `AllowAnonymous`.
- `appsettings.Development.json` apunta por defecto a `PostgresConnection` en `localhost:5432`.

En esta revalidación, aun con trazas previas históricas de PostgreSQL, la API sí expuso OpenAPI runtime y el archivo se descargó correctamente.

## 5. Resultado OpenAPI real
- Archivo generado: `/tmp/openapi-scalar-3ea.json`
- Tamaño: `697553 bytes`
- Fuente: `http://127.0.0.1:5194/openapi/v1.json`

## 6. Total endpoints AchTraceability
- `TOTAL_ENDPOINTS_ACH_TRACEABILITY=3`

## 7. Summary/Description/Textos genéricos
- `SIN_SUMMARY=0`
- `SIN_DESCRIPTION=0`
- `CON_TEXTOS_GENERICOS=0`

## 8. Validación de endpoints esperados
Resultado del script de validación:
- `ENDPOINTS_ESPERADOS_PRESENTES=3`
- `ENDPOINTS_ESPERADOS_FALTANTES=0`

Endpoints detectados:
- `POST /api/ach-traceability/sol02/{transactionId}/certify` (equivalente al route template con constraint `{transactionId:int}` en código fuente).
- `GET /api/ach-traceability/transactions/{transactionId}` (equivalente al template con constraint `{transactionId:int}`).
- `GET /api/ach-traceability/report`.

Responses publicados:
- `POST certify`: `200,400,401,403,404,500`
- `GET transaction traceability`: `200,401,403,404,500`
- `GET report`: `200,400,401,403,500`

## 9. Validación transversal de seguridad en OpenAPI
Se evaluaron endpoints de `ach-traceability` y `Transactions`:
- `TOTAL_ENDPOINTS_SEGURIDAD_TRANSVERSAL=10`
- En OpenAPI, el campo `security` apareció como `None` por operación en este conjunto.

Esto es consistente con la situación documental actual: controllers críticos sin `[Authorize]` explícito local y dependencia de seguridad global/pipeline. Esta fase no implementa hardening.

## 10. Resultado pruebas relacionadas
`dotnet test ... --filter "FullyQualifiedName~AchTraceability|...|Transaction" -v minimal`:
- **67/67 passed**.

## 11. Resultado suite completa
`dotnet test ... -c Release -v minimal`:
- **408/408 passed**.

## 12. Resultado build final
`dotnet build ACHInterbank.sln -c Release`:
- **Exitoso**.

## 13. Estado final de Scalar-3E
Con esta revalidación runtime, el bloqueo pendiente de OpenAPI de Scalar-3E queda resuelto para el módulo AchTraceability.

## 14. Riesgos restantes
- No se cierra seguridad API: sigue pendiente evaluación de autorización explícita en controllers críticos (línea de trabajo Scalar-SEC-1).
- Se mantiene sensibilidad a disponibilidad de PostgreSQL para algunos servicios de background en ciertos arranques.

## 15. Veredicto
**Scalar-3E-A CERRADO**.
