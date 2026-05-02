# Scalar-SEC-PERM-3 — Migración controlada P0 a permisos finos con compatibilidad temporal (2026-05-01)

## 1. Resumen ejecutivo
Se migraron únicamente los controladores P0 (`TransactionsController`, `AchTraceabilityController`, `AchReturnsController`) a policies compuestas que aceptan permiso fino **o** permiso legacy, sin cambios de rutas/contratos/DTOs/lógica.

## 2. Contexto PERM-1/PERM-2/PERM-2A
PERM-1 diseñó el modelo fino, PERM-2 definió constantes/catálogo, y PERM-2A registró policies finas. PERM-3 aplica migración controlada en P0 con compatibilidad temporal.

## 3. Controladores P0 migrados
- `TransactionsController`
- `AchTraceabilityController`
- `AchReturnsController`

## 4. Policies compuestas creadas
- `P0.TransactionsRead`
- `P0.TransactionsCreate`
- `P0.TransactionsBulkSubmit`
- `P0.TransactionsPolicyPreview`
- `P0.TraceabilityRead`
- `P0.TraceabilityCertifySol02`
- `P0.ReturnsRead`
- `P0.ReturnsGenerateFile`

## 5. Mapeo endpoint → policy anterior → policy compuesta
- `GET /Transactions`: `CanReadAch` → `P0.TransactionsRead`
- `GET /Transactions/company-entry-descriptions`: `CanReadAch` → `P0.TransactionsRead`
- `GET /Transactions/policies/preview`: `CanReadAch` → `P0.TransactionsPolicyPreview`
- `POST /Transactions`: `CanManageAch` → `P0.TransactionsCreate`
- `POST /Transactions/bulk/submit`: `CanManageAch` → `P0.TransactionsBulkSubmit`
- `POST /Transactions/bulk`: `CanManageAch` → `P0.TransactionsBulkSubmit`
- `GET /Transactions/{id}`: `CanReadAch` → `P0.TransactionsRead`
- `POST /api/ach-traceability/sol02/{transactionId}/certify`: `CanManageAch` → `P0.TraceabilityCertifySol02`
- `GET /api/ach-traceability/transactions/{transactionId}`: `CanReadAch` → `P0.TraceabilityRead`
- `GET /api/ach-traceability/report`: `CanReadAch` → `P0.TraceabilityRead`
- `GET /ach-returns/cycles/{cycleId}/transactions`: `CanReadAch` → `P0.ReturnsRead`
- `POST /ach-returns/generate-file`: `CanManageAch` → `P0.ReturnsGenerateFile`

## 6. Compatibilidad temporal con CanReadAch/CanManageAch
Todas las policies compuestas P0 usan `RequireAssertion` con claim type `permission` y validan: permiso fino esperado **o** permiso legacy equivalente.

## 7. Validación de no tocar P1/P2
`git diff -- src/Cfa.ACHInterbank.Api/Controllers` muestra cambios únicamente en los tres controladores P0.

## 8. Resultado pruebas específicas
Filtro de seguridad/autorización/P0 ejecutado exitosamente (`31/31`).

## 9. Resultado suite completa
Suite backend completa exitosa (`423/423`).

## 10. Resultado build final
`dotnet build ACHInterbank.sln -c Release` exitoso.

## 11. Resultado OpenAPI P0
- `TOTAL_P0_OPENAPI=12`
- `P0_SIN_SECURITY=0`
- Todas las operaciones P0 reportaron `security=[{'Bearer': []}]`.

## 12. Riesgos restantes
- La compatibilidad temporal mantiene dependencia de claims legacy mientras se completa rollout de claims finos.
- P1/P2 aún pendientes de migración controlada.

## 13. Qué NO se implementó
Se limitó estrictamente el alcance a P0 con compatibilidad temporal.

## Qué NO se implementó en Scalar-SEC-PERM-3
- No se migraron controladores P1/P2.
- No se eliminaron CanReadAch ni CanManageAch.
- No se exigieron permisos finos de forma exclusiva.
- No se cambiaron rutas.
- No se cambiaron contratos.
- No se cambiaron DTOs.
- No se cambiaron roles reales.
- No se crearon migraciones.
- No se declara producción lista.

## 14. Veredicto
**Scalar-SEC-PERM-3: CERRADO** para P0 bajo compatibilidad temporal, con pruebas, build y validación OpenAPI.
