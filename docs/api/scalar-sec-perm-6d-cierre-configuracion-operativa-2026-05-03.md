# Scalar-SEC-PERM-6D — Cierre de configuración operativa

## 1. Resumen ejecutivo
Se cerró el alcance seleccionado de PERM-6D migrando controladores críticos de configuración operativa a políticas finas P1 con compatibilidad legacy y validación por pruebas y OpenAPI runtime.

## 2. Alcance de PERM-6D
Alcance cerrado:
- LoginLockoutSettingsController.
- SoapIntegrationSettingsController.
- NachaConfigProfilesController.
- ClearingHouseCycleConfigsController.
- MaintenanceController.

## 3. Controladores migrados
- LoginLockoutSettingsController.
- SoapIntegrationSettingsController.
- NachaConfigProfilesController.
- ClearingHouseCycleConfigsController.
- MaintenanceController.

## 4. Mapeo controller -> policy fina
- LoginLockoutSettingsController: GET -> `P1Policies.ConfigRead`, escritura -> `P1Policies.ConfigManage`.
- SoapIntegrationSettingsController: GET -> `P1Policies.ConfigRead`, escritura -> `P1Policies.ConfigManage`.
- NachaConfigProfilesController: GET -> `P1Policies.ConfigRead`, POST/PUT -> `P1Policies.ConfigManage`.
- ClearingHouseCycleConfigsController: GET -> `P1Policies.ConfigRead`, POST -> `P1Policies.ConfigManage`.
- MaintenanceController: seed -> `P1Policies.MaintenanceSeed`.

## 5. Compatibilidad legacy
- `ConfigRead` -> `FineGrainedPermissions.Config.Read` OR `CanReadAch`.
- `ConfigManage` -> `FineGrainedPermissions.Config.Manage` OR `CanManageAch`.
- `MaintenanceSeed` -> `FineGrainedPermissions.Maintenance.Seed` OR `CanManageAch`.
- `MaintenanceRunAdminTask` -> `FineGrainedPermissions.Maintenance.RunAdminTask` OR `CanManageAch`.

## 6. Evidencia por bloque
Cada bloque PERM-6D migrado tuvo validación incremental con build y pruebas dirigidas de composición `[Authorize]` y compatibilidad OR de políticas.

## 7. Build y suite completa
- Build Release: OK.
- Suite completa: 440/440 OK.

## 8. OpenAPI final
- TOTAL_OPERACIONES_OPENAPI=213.
- OPERACIONES_CON_SECURITY=207.
- OPERACIONES_SIN_SECURITY=6.
- OPERACIONES_SIN_SECURITY_NO_ESPERADAS=0.
- ESCRITURAS_SIN_SECURITY_NO_ESPERADAS=0.

## 9. Endpoints públicos sin security esperados
- POST /Auth/login.
- POST /Auth/forgot-password.
- POST /Auth/reset-password.
- GET /api/users/branding.
- POST /Oauths/GenerateToken.
- POST /Oauths/GenerateTokenAsync.

## 10. Riesgos restantes
- Persisten controladores fuera de este alcance que todavía usan policies legacy directas.
- La migración a permisos finos aún no es total en toda la superficie de configuración.

## 11. Qué NO se implementó en Scalar-SEC-PERM-6D

- No se migraron todos los controladores de configuración existentes.
- No se migraron controladores fuera del alcance PERM-6D seleccionado.
- No se eliminaron CanReadAch ni CanManageAch.
- No se exigieron permisos finos de forma exclusiva.
- No se cambiaron rutas.
- No se cambiaron contratos.
- No se cambiaron DTOs.
- No se cambiaron roles reales.
- No se crearon migraciones.
- No se declara producción lista.

## 12. Veredicto
- Scalar-SEC-PERM-6D: CERRADO para el alcance seleccionado.
- OpenAPI sin huecos inesperados de security: SÍ.
- Producción lista: NO.

## 13. Siguiente paso recomendado
Scalar-SEC-PERM-6E — Cierre PERM-6 completo o migración de configuración restante de menor criticidad.
