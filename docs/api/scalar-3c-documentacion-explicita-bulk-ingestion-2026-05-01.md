# Scalar-3C — Documentación explícita Bulk Ingestion (2026-05-01)

## 1. Resumen ejecutivo
Se documentó explícitamente el módulo Bulk Ingestion en OpenAPI/Scalar, eliminando dependencia de fallback contextual para sus operaciones REST reales, sin cambios de lógica, rutas, contratos ni permisos.

## 2. Contexto Scalar-3
Tras Scalar-3A (Capability Registry) y Scalar-3B (Incoming NACHA Command Center), Scalar-3C aborda Bulk Ingestion por su impacto en cargas masivas, control de lotes y acciones operativas de retry/cancel.

## 3. Controller(s) inspeccionados
- `src/Cfa.ACHInterbank.Api/Controllers/BulkIngestionController.cs`

## 4. Endpoints documentados
- `POST /api/transactions/bulk-ingestion/upload`
- `GET /api/transactions/bulk-ingestion/{batchId}`
- `GET /api/transactions/bulk-ingestion/{batchId}/items`
- `GET /api/transactions/bulk-ingestion/{batchId}/summary`
- `POST /api/transactions/bulk-ingestion/{batchId}/retry`
- `POST /api/transactions/bulk-ingestion/{batchId}/cancel`

## 5. Cambios realizados
- Se agregaron `EndpointSummary` y `EndpointDescription` explícitos en los 6 endpoints del controller.
- Se incluyó descripción de permiso, tipo de operación (consulta/acción operativa), impacto operacional, auditoría/trazabilidad, riesgos, errores y precauciones.
- Se completaron `ProducesResponseType` para códigos esperados por endpoint (200/400/401/403/404/409/500 según aplica).

## 6. Permisos documentados
- Política real del controller y de todos los endpoints: `CanManageAch`.

## 7. Acciones operativas identificadas
Acciones de escritura detectadas:
- `POST /upload` (inicio de carga masiva)
- `POST /{batchId}/retry` (reintento operativo)
- `POST /{batchId}/cancel` (cancelación operativa)

## 8. Impacto operacional por acción
- **upload**: registra y dispara procesamiento de cargas masivas; error de archivo puede escalar a incidentes de alto volumen.
- **retry**: reactiva procesamiento de lote/ítems; uso sin diagnóstico puede duplicar procesamiento.
- **cancel**: detiene lote en curso; decisión incorrecta puede afectar SLA y conciliación operativa.

## 9. Validación OpenAPI real
Se ejecutó validación OpenAPI con dos lecturas:
1) Filtro amplio por keywords (`bulk|ingestion|batch|lote`) detectó rutas de otros módulos con esas palabras.
2) Filtro ajustado al módulo real (`/api/transactions/bulk-ingestion`) arrojó:
- `TOTAL_ENDPOINTS_BULK_INGESTION_MODULE=6`
- `SIN_SUMMARY=0`
- `SIN_DESCRIPTION=0`
- `CON_TEXTOS_GENERICOS=0`

## 10. Resultado pruebas específicas
- Descubrimiento de pruebas relacionadas ejecutado con `--list-tests`.
- Filtro amplio relacionado a Bulk/Ingestion/Batch/Upload/Tracking: **61/61 passed**.

## 11. Resultado suite completa
- Suite backend completa: **408/408 passed**.

## 12. Resultado build final
- `dotnet build ACHInterbank.sln -c Release`: exitoso.

## 13. Riesgos restantes
- En `dotnet run` pueden aparecer trazas de background asociadas a disponibilidad de PostgreSQL en el entorno; no bloquearon generación de OpenAPI.
- Las acciones `retry` y `cancel` mantienen riesgo operativo alto y exigen validación previa e evidencia de auditoría.

## 14. Veredicto
**Scalar-3C CERRADO** para el módulo Bulk Ingestion en alcance de documentación explícita OpenAPI/Scalar.

## Matriz de endpoints

| Método | Ruta | Tipo | Acción/consulta | Permiso | Impacto operacional | Auditoría esperada | Responses | Estado |
|---|---|---|---|---|---|---|---|---|
| POST | `/api/transactions/bulk-ingestion/upload` | Acción operativa | Inicio de carga masiva | `CanManageAch` | Ingresa lote y habilita procesamiento de alto volumen | Usuario, archivo, referencia, correlación | 200,400,401,403,500 | Validado |
| GET | `/api/transactions/bulk-ingestion/{batchId}` | Consulta | Estado general de lote | `CanManageAch` | Seguimiento de avance/falla sin mutación | Registro de consulta y correlación | 200,401,403,404,500 | Validado |
| GET | `/api/transactions/bulk-ingestion/{batchId}/items` | Consulta | Detalle paginado de ítems | `CanManageAch` | Identifica errores/pendientes para decisiones operativas | Evidencia de revisión por estado | 200,400,401,403,404,500 | Validado |
| GET | `/api/transactions/bulk-ingestion/{batchId}/summary` | Consulta | Resumen de procesamiento | `CanManageAch` | Soporta evaluación de salud del lote | Evidencia consolidada para incidentes | 200,401,403,404,500 | Validado |
| POST | `/api/transactions/bulk-ingestion/{batchId}/retry` | Acción operativa | Reintento de procesamiento | `CanManageAch` | Reactiva procesamiento de lote/ítems | Usuario, motivo, criterio, resultado | 200,400,401,403,404,409,500 | Validado |
| POST | `/api/transactions/bulk-ingestion/{batchId}/cancel` | Acción operativa | Cancelación de lote | `CanManageAch` | Detiene procesamiento pendiente del lote | Usuario, motivo, timestamp, decisión | 200,400,401,403,404,409,500 | Validado |
