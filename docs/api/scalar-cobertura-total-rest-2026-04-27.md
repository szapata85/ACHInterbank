# Cobertura total OpenAPI/Scalar REST — ACHInterbank
**Fecha (UTC):** 2026-04-27  
**Objetivo:** completar cobertura documental para todos los endpoints REST visibles o potencialmente visibles en Scalar sin modificar lógica de negocio, rutas ni contratos públicos.

## Estrategia aplicada
- Se incorporó un `IOpenApiOperationTransformer` global para completar `summary` y `description` cuando una acción no define atributos explícitos.
- Si un endpoint ya tiene `EndpointSummary`/`EndpointDescription`, se respeta la documentación existente.
- La descripción generada incluye: permiso requerido, método HTTP, naturaleza de operación (consulta/modificación), auditoría esperada, códigos de respuesta deducibles y riesgo operativo.
- Se mantiene trazabilidad ACH/CENIT/NACHA-M en cada descripción generada por fallback.

## Matriz de cobertura total por controlador
- Controladores inventariados: **52**.
- Operaciones inventariadas: **220**.
- Cobertura documental final esperada en Scalar/OpenAPI: **100%** (manual + fallback automático).

| # | Controlador | Base route | Operaciones | Cobertura summary | Cobertura description | Mecanismo principal |
|---:|---|---|---:|---|---|---|
| 1 | AchCyclesController | `/ach-cycles` | 6 | Sí | Sí | Atributos explícitos o transformer global |
| 2 | AchReturnsController | `/ach-returns` | 2 | Sí | Sí | Atributos explícitos o transformer global |
| 3 | AchTraceabilityController | `/api/ach-traceability` | 3 | Sí | Sí | Atributos explícitos o transformer global |
| 4 | AuditLogsController | `/api/audit-logs` | 1 | Sí | Sí | Atributos explícitos o transformer global |
| 5 | AuthController | `/Auth` | 4 | Sí | Sí | Atributos explícitos o transformer global |
| 6 | AuthLogsController | `/api/auth-logs` | 1 | Sí | Sí | Atributos explícitos o transformer global |
| 7 | BankHolidaysController | `/bank-holidays` | 4 | Sí | Sí | Atributos explícitos o transformer global |
| 8 | BrandingController | `/api/users/branding` | 2 | Sí | Sí | Atributos explícitos o transformer global |
| 9 | BulkIngestionController | `/api/transactions/bulk-ingestion` | 6 | Sí | Sí | Atributos explícitos o transformer global |
| 10 | CatalogTypesController | `/catalog-types` | 4 | Sí | Sí | Atributos explícitos o transformer global |
| 11 | CenitOperationsController | `/api/cenit` | 4 | Sí | Sí | Atributos explícitos o transformer global |
| 12 | CertificateManagementController | `/nacha-security/certificates/management` | 8 | Sí | Sí | Atributos explícitos o transformer global |
| 13 | ClearingHouseCycleConfigsController | `/clearing-house-cycle-configs` | 4 | Sí | Sí | Atributos explícitos o transformer global |
| 14 | ClearingHouseSpecialDatesController | `/clearing-house-special-dates` | 4 | Sí | Sí | Atributos explícitos o transformer global |
| 15 | ClearingHousesController | `/clearing-houses` | 3 | Sí | Sí | Atributos explícitos o transformer global |
| 16 | CompanyEntryDescriptionsController | `/company-entry-descriptions` | 4 | Sí | Sí | Atributos explícitos o transformer global |
| 17 | CustomerThirdPartiesController | `/api/customer-third-parties` | 2 | Sí | Sí | Atributos explícitos o transformer global |
| 18 | CustomersController | `/customers` | 5 | Sí | Sí | Atributos explícitos o transformer global |
| 19 | DigitalEnvelopeCertificatesController | `/nacha-security/certificates` | 3 | Sí | Sí | Atributos explícitos o transformer global |
| 20 | FinancialInstitutionsController | `/financial-institutions` | 5 | Sí | Sí | Atributos explícitos o transformer global |
| 21 | IncomingNachaCommandCenterController | `/incoming-nacha-command-center` | 9 | Sí | Sí | Atributos explícitos o transformer global |
| 22 | InstitutionClearingHousePreferencesController | `/institution-clearing-house-preferences` | 4 | Sí | Sí | Atributos explícitos o transformer global |
| 23 | IntegrationCatalogController | `/api/integrations` | 4 | Sí | Sí | Atributos explícitos o transformer global |
| 24 | IntegrationMappingSetsController | `/api/integrations/mappingsets` | 12 | Sí | Sí | Atributos explícitos o transformer global |
| 25 | JwksController | `/oauth2` | 4 | Sí | Sí | Atributos explícitos o transformer global |
| 26 | LoginLockoutSettingsController | `/api/users/login-lockout` | 2 | Sí | Sí | Atributos explícitos o transformer global |
| 27 | MaintenanceController | `/Maintenance` | 1 | Sí | Sí | Atributos explícitos o transformer global |
| 28 | MenuItemsController | `/navigation/menu-items` | 4 | Sí | Sí | Atributos explícitos o transformer global |
| 29 | NachaConfigProfilesController | `/nacha-config` | 17 | Sí | Sí | Atributos explícitos o transformer global |
| 30 | NachaController | `/Nacha` | 1 | Sí | Sí | Atributos explícitos o transformer global |
| 31 | NachaExportController | `/NachaExport` | 2 | Sí | Sí | Atributos explícitos o transformer global |
| 32 | NachaRecordDefinitionsController | `/nacha-record-definitions` | 5 | Sí | Sí | Atributos explícitos o transformer global |
| 33 | NachaRecordLayoutsController | `/nacha-layouts` | 5 | Sí | Sí | Atributos explícitos o transformer global |
| 34 | NachaSecurityOperationsController | `/nacha-security/operations` | 8 | Sí | Sí | Atributos explícitos o transformer global |
| 35 | NachaUploadController | `/NachaUpload` | 2 | Sí | Sí | Atributos explícitos o transformer global |
| 36 | NavigationController | `/navigation` | 1 | Sí | Sí | Atributos explícitos o transformer global |
| 37 | NavigationLogsController | `/api/navigation-logs` | 2 | Sí | Sí | Atributos explícitos o transformer global |
| 38 | OauthsController | `/Oauths` | 2 | Sí | Sí | Atributos explícitos o transformer global |
| 39 | PasswordRulesController | `/api/users/password-rules` | 2 | Sí | Sí | Atributos explícitos o transformer global |
| 40 | PaymentRailCapabilityRegistryController | `/api/payment-rails/capability-registry` | 3 | Sí | Sí | Atributos explícitos o transformer global |
| 41 | PermissionsController | `/api/Permissions` | 1 | Sí | Sí | Atributos explícitos o transformer global |
| 42 | RegulatoryCatalogsController | `/api/regulatory-catalogs` | 6 | Sí | Sí | Atributos explícitos o transformer global |
| 43 | ReportsController | `/api/reports` | 19 | Sí | Sí | Atributos explícitos o transformer global |
| 44 | ReturnReasonsController | `/return-reasons` | 1 | Sí | Sí | Atributos explícitos o transformer global |
| 45 | RolesController | `/api/Roles` | 1 | Sí | Sí | Atributos explícitos o transformer global |
| 46 | ServersController | `/Servers` | 1 | Sí | Sí | Atributos explícitos o transformer global |
| 47 | SoapIntegrationSettingsController | `/api/users/soap-integrations` | 2 | Sí | Sí | Atributos explícitos o transformer global |
| 48 | SobreDigitalController | `/SobreDigital` | 3 | Sí | Sí | Atributos explícitos o transformer global |
| 49 | TaskDefinitionsController | `/TaskDefinitions` | 5 | Sí | Sí | Atributos explícitos o transformer global |
| 50 | TestsController | `/Tests` | 2 | Sí | Sí | Atributos explícitos o transformer global |
| 51 | TransactionsController | `/Transactions` | 7 | Sí | Sí | Atributos explícitos o transformer global |
| 52 | UsersController | `/api/Users` | 7 | Sí | Sí | Atributos explícitos o transformer global |

## Criterios de verificación
- Build en `Release` sin errores para validar compilación del transformer y registro en DI.
- Revisión en documento OpenAPI (`/openapi/v1.json`) para confirmar que toda operación publica `summary` y `description`.
- La cobertura se considera completa porque el fallback opera sobre cada `ControllerActionDescriptor` expuesto por `MapControllers()`.
