# Scalar-SEC-PERM-6 — Cierre completo (alcance P1)

## 1. Resumen ejecutivo
Se consolida el cierre documental de PERM-6 para el alcance P1 seleccionado, cubriendo Users, Roles, Permissions y configuración operativa crítica con políticas finas y compatibilidad legacy.

## 2. Alcance de PERM-6
Incluye los bloques PERM-6B, PERM-6C y PERM-6D seleccionados.

## 3. Bloques cerrados
- PERM-6B: UsersController.
- PERM-6C: RolesController y PermissionsController.
- PERM-6D: LoginLockoutSettingsController, SoapIntegrationSettingsController, NachaConfigProfilesController, ClearingHouseCycleConfigsController, MaintenanceController.

## 4. Controladores migrados
- UsersController.
- RolesController.
- PermissionsController.
- LoginLockoutSettingsController.
- SoapIntegrationSettingsController.
- NachaConfigProfilesController.
- ClearingHouseCycleConfigsController.
- MaintenanceController.

## 5. Policies P1 usadas
- P1Policies.UsersRead, UsersCreate, UsersUpdate, UsersAssignRoles, UsersDeactivate.
- P1Policies.RolesRead, RolesCreate, RolesUpdate, RolesDelete.
- P1Policies.PermissionsRead, PermissionsAssign.
- P1Policies.ConfigRead, ConfigManage.
- P1Policies.MaintenanceSeed, MaintenanceRunAdminTask.

## 6. Compatibilidad legacy
- ConfigRead -> FineGrainedPermissions.Config.Read OR CanReadAch.
- ConfigManage -> FineGrainedPermissions.Config.Manage OR CanManageAch.
- MaintenanceSeed -> FineGrainedPermissions.Maintenance.Seed OR CanManageAch.
- MaintenanceRunAdminTask -> FineGrainedPermissions.Maintenance.RunAdminTask OR CanManageAch.

## 7. Evidencia por bloque
Cada bloque fue validado con pruebas dirigidas de composición Authorize y compatibilidad OR en `IAuthorizationService`.

## 8. Build final
- Build Release: OK.

## 9. Evidencia OpenAPI heredada de PERM-6D
- TOTAL_OPERACIONES_OPENAPI=213.
- OPERACIONES_CON_SECURITY=207.
- OPERACIONES_SIN_SECURITY=6.
- OPERACIONES_SIN_SECURITY_NO_ESPERADAS=0.
- ESCRITURAS_SIN_SECURITY_NO_ESPERADAS=0.
- Suite completa previa PERM-6D: 440/440 OK.

## 10. Riesgos restantes
- Permanecen controladores legacy fuera de este alcance sin migración fina completa.
- No hay enforcement exclusivo de permisos finos todavía.

## 11. Qué NO se implementó en Scalar-SEC-PERM-6

- No se migraron todos los controladores del sistema.
- No se migraron todos los controladores de configuración existentes.
- No se eliminaron CanReadAch ni CanManageAch.
- No se exigieron permisos finos de forma exclusiva.
- No se cambiaron rutas.
- No se cambiaron contratos.
- No se cambiaron DTOs.
- No se cambiaron roles reales.
- No se crearon migraciones.
- No se declara producción lista.
- No se ejecutó pentest.
- No se ejecutó UAT de seguridad.

## 12. Veredicto
- Scalar-SEC-PERM-6: CERRADO para el alcance P1 seleccionado.
- Users/Roles/Permissions migrados: SÍ.
- Configuración operativa seleccionada migrada: SÍ.
- OpenAPI sin huecos inesperados de security: SÍ.
- Producción lista: NO.

## 13. Siguiente paso recomendado
Scalar-SEC-PERM-7 — Depuración técnica de permisos finos, limpieza de tests y evaluación de controladores legacy restantes.
