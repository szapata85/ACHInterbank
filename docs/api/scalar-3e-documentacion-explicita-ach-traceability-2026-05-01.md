# Scalar-3E — Documentación explícita de AchTraceabilityController (2026-05-01)

## 1. Resumen ejecutivo
Se completó la documentación explícita OpenAPI/Scalar de `AchTraceabilityController` sin modificar lógica de negocio, rutas, contratos ni permisos. Se verificó el módulo de trazabilidad (consultas y acción operativa de certificación SOL02) y se dejó registrado el hallazgo transversal heredado de Scalar-3D: `TransactionsController` no declara `[Authorize]` explícito y depende de seguridad global.

## 2. Contexto Scalar-3
Scalar-3E extiende Scalar-3A..3D para cerrar cobertura documental en trazabilidad ACH. Esta fase mantiene alcance atributivo/documental (resúmenes, descripciones y responses OpenAPI) y no implementa hardening de seguridad.

## 3. Controller(s) inspeccionados
- `src/Cfa.ACHInterbank.Api/Controllers/AchTraceabilityController.cs`
- `src/Cfa.ACHInterbank.Api/Controllers/TransactionsController.cs` (hallazgo transversal)
- `src/Cfa.ACHInterbank.Api/DependencyInjectionService.cs` (evidencia de endpoints anónimos y configuración de pipeline)

## 4. Endpoints documentados
1. `POST /api/ach-traceability/sol02/{transactionId:int}/certify`
2. `GET /api/ach-traceability/transactions/{transactionId:int}`
3. `GET /api/ach-traceability/report`

## 5. Cambios realizados
- Se reforzó `EndpointSummary`, `EndpointDescription` y `ProducesResponseType` en los endpoints del módulo de trazabilidad.
- Se dejó explícito por endpoint: tipo de operación, consumidores (operación/soporte/auditoría/tecnología/cumplimiento), dependencia de seguridad global, evidencia entregada, riesgos de interpretación y errores esperados.
- Se incluyó advertencia explícita de que la trazabilidad no reemplaza proceso formal de auditoría.

## 6. Permisos documentados
- `AchTraceabilityController` no declara `[Authorize]` a nivel controller ni acción.
- No declara `[AllowAnonymous]` en sus endpoints.
- En este alcance queda clasificado como dependiente de seguridad global del API.

## 7. Clasificación del módulo: read-only o acciones operativas
Clasificación mixta:
- Consultas read-only: 2 endpoints (`GET transactions/{id}`, `GET report`).
- Acción operativa: 1 endpoint (`POST sol02/.../certify`) con impacto en estado de transacción.

## 8. Uso en soporte, auditoría y continuidad operacional
El módulo soporta investigación de incidentes, reconstrucción de timeline de transacción, verificación de estado por caso y reportes por ventana temporal/ciclo para continuidad operativa y control de cumplimiento.

## 9. Riesgos de interpretación
- Confundir trazabilidad de consulta con autorización para cambio de estado.
- Interpretar reporte agregado como sustituto de evidencia formal de auditoría.
- Aplicar certificación SOL02 sobre transacción incorrecta por validación insuficiente de contexto.

## 10. Validación OpenAPI real
- Se intentó levantar API y descargar `/openapi/v1.json`.
- Resultado: el host no quedó disponible porque el proceso encontró fallo de conexión a PostgreSQL (`127.0.0.1:5432`, connection refused) durante inicialización de servicios de background.
- Con este bloqueo no fue posible generar archivo OpenAPI runtime de esta ejecución.

## 11. Resultado pruebas específicas
`dotnet test ... --filter "FullyQualifiedName~AchTraceability|...|Transaction" -v minimal`:
- **67/67 passed**.

## 12. Resultado suite completa
`dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release -v minimal`:
- **408/408 passed**.

## 13. Resultado build final
`dotnet build ACHInterbank.sln -c Release`:
- **Build exitoso**.

## 14. Hallazgo transversal heredado de Scalar-3D — Seguridad explícita en controladores críticos
Se mantiene vigente el hallazgo:
- `TransactionsController` no declara `[Authorize]` explícito.
- `AchTraceabilityController` tampoco declara `[Authorize]` explícito.
- Ambos quedan dependiendo de controles globales del API en tiempo de ejecución.

## 15. Matriz transversal de seguridad
| Controller | Tiene [Authorize] explícito | Tiene AllowAnonymous | Depende de seguridad global | Criticidad | Riesgo residual | Acción recomendada |
|---|---|---|---|---|---|---|
| TransactionsController | No | No (en este controller) | Sí | Alta | Alto | Reforzar con [Authorize] explícito en fase posterior |
| AchTraceabilityController | No | No (en este controller) | Sí | Alta | Alto | Requiere prompt de hardening |

## 16. Riesgos restantes
Riesgo transversal de seguridad API:
`TransactionsController` no declara `[Authorize]` explícito y depende de la seguridad global.
`AchTraceabilityController` queda clasificado en este mismo análisis.
Se recomienda una fase posterior Scalar-SEC-1 para revisar controladores críticos sin autorización explícita y definir si deben reforzarse con políticas por controller o acción.

## 17. Recomendación Scalar-SEC-1
**Scalar-SEC-1 — Auditoría de autorización explícita en controladores críticos**.
Objetivo: identificar controladores críticos que dependen solo de seguridad global y decidir si deben tener `[Authorize]` explícito por controller o acción.

## 18. Veredicto
Estado de esta ejecución: **PARCIAL / NO CERRADO** por bloqueo de generación OpenAPI runtime real (servicio API sin disponibilidad por conexión a PostgreSQL).

## Matriz obligatoria del módulo
| Método | Ruta | Tipo | Acción/consulta | Permiso | Evidencia entregada | Uso operativo/auditoría | Responses | Estado |
|---|---|---|---|---|---|---|---|---|
| POST | `/api/ach-traceability/sol02/{transactionId:int}/certify` | Acción operativa | Certificación SOL02 de transacción | Depende de seguridad global (sin `[Authorize]` local) | Estado actualizado, `transactionId`, `state`, `stateChangedAtUtc` | Cumplimiento, operación ACH, auditoría de cambios | 200, 400, 401, 403, 404, 500 | Documentado |
| GET | `/api/ach-traceability/transactions/{transactionId:int}` | Consulta | Trazabilidad puntual por transacción | Depende de seguridad global (sin `[Authorize]` local) | DTO de trazabilidad con timeline/estado asociado | Soporte de incidentes, auditoría técnica, continuidad operacional | 200, 401, 403, 404, 500 | Documentado |
| GET | `/api/ach-traceability/report` | Consulta | Reporte de trazabilidad por rango/estado/ciclo | Depende de seguridad global (sin `[Authorize]` local) | Consolidado de trazabilidad por filtros | Operación, control interno y seguimiento de incidentes | 200, 400, 401, 403, 500 | Documentado |

## Siguiente módulo funcional recomendado
**Scalar-3F** (siguiente módulo funcional pendiente de cobertura documental explícita OpenAPI/Scalar).

## Siguiente prompt de seguridad recomendado
**Scalar-SEC-1 — Auditoría de autorización explícita en controladores críticos**.


Nota Scalar-3E-A: la validación runtime de OpenAPI que quedó bloqueada en Scalar-3E fue revalidada en docs/api/scalar-3ea-revalidacion-openapi-ach-traceability-2026-05-01.md.
