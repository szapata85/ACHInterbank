# Scalar-3D — Documentación explícita TransactionsController (2026-05-01)

## 1. Resumen ejecutivo
Se documentó explícitamente `TransactionsController` en OpenAPI/Scalar, cubriendo endpoints transaccionales y bulk legacy para separar su alcance operativo frente a `BulkIngestionController` moderno.

## 2. Contexto Scalar-3
Tras Scalar-3A/3B/3C, se identificó que el dominio de transacciones incluía rutas `/Transactions/bulk*` fuera del módulo de ingestión moderna, por lo que se reforzó documentación en `TransactionsController`.

## 3. Controller(s) inspeccionados
- `src/Cfa.ACHInterbank.Api/Controllers/TransactionsController.cs`
- Referencia comparativa: `src/Cfa.ACHInterbank.Api/Controllers/BulkIngestionController.cs`

## 4. Endpoints documentados
- `GET /Transactions`
- `GET /Transactions/company-entry-descriptions`
- `GET /Transactions/policies/preview`
- `POST /Transactions`
- `POST /Transactions/bulk/submit`
- `POST /Transactions/bulk`
- `GET /Transactions/{id}`

## 5. Diferencia frente a BulkIngestionController
- `TransactionsController` expone gestión de transacción individual y bulk legacy por payload (`/Transactions/bulk*`).
- `BulkIngestionController` gestiona ingestión moderna por archivo y lifecycle de batch (`/api/transactions/bulk-ingestion/*`).
- La documentación explicita esta separación para evitar interpretación cruzada de responsabilidades.

## 6. Cambios realizados
- Se reemplazaron descripciones genéricas por `EndpointSummary`/`EndpointDescription` explícitos en los 7 endpoints del controller.
- Se completaron `ProducesResponseType` para 400/401/403/404/409/500 según aplica.
- No se cambió lógica, rutas ni contratos públicos.

## 7. Permisos documentados
- No existe atributo `[Authorize]` explícito en `TransactionsController`.
- Se documentó dependencia de seguridad/política global del API para respuestas 401/403.

## 8. Acciones operativas identificadas
- `POST /Transactions` (creación individual)
- `POST /Transactions/bulk/submit` (bulk legacy submit)
- `POST /Transactions/bulk` (bulk legacy inline)

## 9. Impacto operacional por acción
- **POST /Transactions**: crea entidad transacción con impacto en conciliación y cumplimiento de reglas ACH.
- **POST /Transactions/bulk/submit**: inicia flujo masivo legacy dependiente de modo y origen.
- **POST /Transactions/bulk**: registra múltiples transacciones en línea con riesgo de duplicidad masiva si no hay controles previos.

## 10. Validación OpenAPI real
- Filtro amplio `/transactions|/api/transactions` devuelve endpoints de varios módulos (reports, ach-returns, ach-traceability, bulk-ingestion).
- Filtro específico `route.startswith('/Transactions')` para TransactionsController:
  - `TOTAL_ENDPOINTS_TRANSACTIONSCONTROLLER=7`
  - `SIN_SUMMARY=0`
  - `SIN_DESCRIPTION=0`
  - `CON_TEXTOS_GENERICOS=0`

## 11. Resultado pruebas específicas
- Descubrimiento de pruebas relacionadas ejecutado con `--list-tests`.
- Filtro amplio (Transactions/Transaction/Bulk/Batch/Upload): **91/91 passed**.

## 12. Resultado suite completa
- Suite backend completa: **408/408 passed**.

## 13. Resultado build final
- `dotnet build ACHInterbank.sln -c Release`: exitoso.

## 14. Riesgos restantes
- Ausencia de `[Authorize]` explícito en controller obliga a mantener control estricto de seguridad global para no exponer rutas críticas.
- Convivencia de bulk legacy y bulk moderno requiere guías operativas claras para evitar invocación del endpoint incorrecto.

## 15. Veredicto
**Scalar-3D CERRADO** para `TransactionsController` con documentación explícita y separación funcional frente a `BulkIngestionController`.

## Matriz de endpoints

| Método | Ruta | Tipo | Acción/consulta | Permiso | Relación con BulkIngestion | Impacto operacional | Auditoría esperada | Responses | Estado |
|---|---|---|---|---|---|---|---|---|---|
| GET | `/Transactions` | Consulta | Listado filtrado de transacciones | Seguridad global (sin `[Authorize]` local) | Consulta de entidad transacción, no de batch | Visibilidad operativa para conciliación | Usuario + filtros + timestamp | 200,400,401,403,500 | Validado |
| GET | `/Transactions/company-entry-descriptions` | Consulta | Catálogo de descripciones de lote | Seguridad global (sin `[Authorize]` local) | Catálogo para originación; no lifecycle batch | Previene parametrización inválida | Registro de acceso a catálogo | 200,401,403,500 | Validado |
| GET | `/Transactions/policies/preview` | Consulta | Simulación de política de transacción | Seguridad global (sin `[Authorize]` local) | Valida transacción individual, no archivo | Reduce rechazos en registro real | Evidencia de request/resultado preview | 200,400,401,403,500 | Validado |
| POST | `/Transactions` | Acción operativa | Crear transacción individual | Seguridad global (sin `[Authorize]` local) | No usa flujo de archivo; alta granularidad | Impacta conciliación y estado transaccional | Usuario + referencias + resultado | 201,400,401,403,409,500 | Validado |
| POST | `/Transactions/bulk/submit` | Acción operativa | Submit bulk legacy | Seguridad global (sin `[Authorize]` local) | Solapa dominio bulk, pero vía interfaz legacy | Inicia carga masiva programática | Usuario + sourceType + mode + resultado | 200,400,401,403,500 | Validado |
| POST | `/Transactions/bulk` | Acción operativa | Registro bulk legacy inline | Seguridad global (sin `[Authorize]` local) | Bulk por payload; no tracking moderno de batch | Riesgo de duplicidad/errores masivos | Correlación + resultados por ítem | 200,400,401,403,409,500 | Validado |
| GET | `/Transactions/{id}` | Consulta | Consulta puntual por id | Seguridad global (sin `[Authorize]` local) | Entidad transacción puntual; no estado de ingestión | Soporte de incidentes y auditoría | Usuario + id + contexto | 200,401,403,404,500 | Validado |
