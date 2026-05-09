# Scalar-SEC-PERM-4A — Corrección de composición Authorize en P1 grupo 1 (2026-05-01)

## 1. Resumen ejecutivo
Se corrigió la composición de autorización en controladores P1 grupo 1 para evitar acumulación no intencional de políticas cuando coexistían políticas a nivel controlador y action. El patrón final deja `[Authorize]` sin policy a nivel clase y la policy P1 específica en cada action crítica.

## 2. Riesgo detectado en PERM-4
Durante PERM-4 se identificó riesgo de endurecimiento accidental por composición AND implícita cuando un controlador define `[Authorize(Policy = ...)]` y una action define otra policy distinta.

## 3. Explicación del problema
Cuando existe `controller-level policy` más `action-level policy`, la autorización puede acumularse y exigir simultáneamente ambas reglas, rompiendo la compatibilidad esperada de transición con permisos legacy (`CanReadAch` / `CanManageAch`) y permisos finos.

## 4. Patrón corregido
Patrón aplicado:
- Controlador: `[Authorize]` sin `Policy`.
- Action: `[Authorize(Policy = P1Policies.X)]` según operación.

Con esto, cada endpoint aplica solo su policy objetivo de migración controlada.

## 5. Controladores corregidos
- `BulkIngestionController`.
- `IncomingNachaCommandCenterController`.
- `NachaUploadController`.

## 6. Actions con policy específica

### BulkIngestionController
- `Upload` → `P1Policies.BulkIngestionUpload`.
- `GetBatch` → `P1Policies.BulkIngestionRead`.
- `GetBatchItems` → `P1Policies.BulkIngestionRead`.
- `GetBatchSummary` → `P1Policies.BulkIngestionRead`.
- `Retry` → `P1Policies.BulkIngestionRetry`.
- `Cancel` → `P1Policies.BulkIngestionCancel`.

### IncomingNachaCommandCenterController
- `GetObservabilitySummary` → `P1Policies.CommandCenterRead`.
- `GetIngestions` → `P1Policies.CommandCenterRead`.
- `GetIngestionDetail` → `P1Policies.CommandCenterRead`.
- `GetQueue` → `P1Policies.CommandCenterRead`.
- `GetQueueDetail` → `P1Policies.CommandCenterRead`.
- `RetryManual` → `P1Policies.CommandCenterRetry`.
- `UnblockManual` → `P1Policies.CommandCenterUnblock`.
- `RequeueManual` → `P1Policies.CommandCenterRequeue`.
- `MarkFailedFinal` → `P1Policies.CommandCenterMarkFailedFinal`.

### NachaUploadController
- `UploadNachaFile` → `P1Policies.NachaUpload`.
- `GetUploadedRecords` (GET equivalente) → `P1Policies.NachaRead`.

### NachaController
- `SaveHeader` → `P1Policies.NachaGenerate`.

## 7. Pruebas agregadas
Se reforzó `P1FineGrainedPolicyMigrationTests` con:
- reflexión de composición (`Authorize` de clase sin policy + policies por action esperadas),
- verificación de ausencia de `CanReadAch` / `CanManageAch` directos en actions migradas,
- verificación real con `IAuthorizationService` para escenarios P1 clave.

## 8. Resultado pruebas específicas
**35/35** exitosas.

## 9. Resultado suite completa
**427/427** exitosas.

## 10. Resultado build final
Build final exitoso en `Release`.

## 11. Resultado OpenAPI P1 grupo 1
- `TOTAL_P1_GRUPO1_OPENAPI=18`
- `P1_GRUPO1_SIN_SECURITY=0`

## 12. Validación de no tocar otros controladores
Se validó que solo se ajustaron:
- `BulkIngestionController`
- `IncomingNachaCommandCenterController`
- `NachaUploadController`

No hubo migración de otros controladores fuera del alcance de PERM-4A.

## 13. Riesgos restantes
- Persisten políticas legacy por compatibilidad (estrategia OR), por lo que aún no existe enforcement exclusivamente fino.
- P2/P3 permanecen pendientes de migración controlada.
- Riesgo operativo si futuros cambios reintroducen policy a nivel clase con valor específico en controladores migrados.

## 14. Qué NO se implementó
No se ejecutaron migraciones funcionales fuera de alcance ni cambios de contratos/rutas/DTOs; no se modificó la estrategia de compatibilidad OR en esta fase.

## Qué NO se implementó en Scalar-SEC-PERM-4A

- No se migraron nuevos controladores.
- No se migraron P2/P3.
- No se eliminaron CanReadAch ni CanManageAch.
- No se exigieron permisos finos de forma exclusiva.
- No se cambiaron rutas.
- No se cambiaron contratos.
- No se cambiaron DTOs.
- No se cambiaron roles reales.
- No se crearon migraciones.
- No se declara producción lista.

## 15. Veredicto
PERM-4A queda cerrado en alcance técnico de composición Authorize para P1 grupo 1, con evidencia de build, pruebas y OpenAPI, y con continuidad recomendada hacia PERM-5 para P1 grupo 2.
