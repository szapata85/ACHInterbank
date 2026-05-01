# Cobertura total OpenAPI/Scalar REST — ACHInterbank
**Fecha (UTC):** 2026-04-29  
**Alcance:** validación de cobertura documental para todas las operaciones REST encontradas en controladores ASP.NET Core.

## Resultado consolidado
- Controladores analizados: **52**.
- Operaciones REST detectadas: **220**.
- Operaciones con `EndpointSummary` explícito: **61**.
- Operaciones con `EndpointDescription` explícito: **61**.
- Operaciones que dependen del transformer global para completar summary/description: **159**.
- Cobertura final esperada en Scalar/OpenAPI: **100%** (atributos explícitos + fallback del transformer global).

## Matriz de cobertura por controlador
| Controlador | Operaciones | Summary explícito | Description explícito | Cobertura final Scalar |
|---|---:|---:|---:|---|
| AchCyclesController | 6 | 0 | 0 | Sí (manual + transformer) |
| AchReturnsController | 2 | 2 | 2 | Sí (manual + transformer) |
| AchTraceabilityController | 3 | 3 | 3 | Sí (manual + transformer) |
| AuditLogsController | 1 | 0 | 0 | Sí (manual + transformer) |
| AuthController | 4 | 0 | 0 | Sí (manual + transformer) |
| AuthLogsController | 1 | 0 | 0 | Sí (manual + transformer) |
| BankHolidaysController | 4 | 0 | 0 | Sí (manual + transformer) |
| BrandingController | 2 | 0 | 0 | Sí (manual + transformer) |
| BulkIngestionController | 6 | 0 | 0 | Sí (manual + transformer) |
| CatalogTypesController | 4 | 0 | 0 | Sí (manual + transformer) |
| CenitOperationsController | 4 | 4 | 4 | Sí (manual + transformer) |
| CertificateManagementController | 8 | 8 | 8 | Sí (manual + transformer) |
| ClearingHouseCycleConfigsController | 4 | 0 | 0 | Sí (manual + transformer) |
| ClearingHouseSpecialDatesController | 4 | 0 | 0 | Sí (manual + transformer) |
| ClearingHousesController | 3 | 0 | 0 | Sí (manual + transformer) |
| CompanyEntryDescriptionsController | 4 | 0 | 0 | Sí (manual + transformer) |
| CustomerThirdPartiesController | 2 | 0 | 0 | Sí (manual + transformer) |
| CustomersController | 5 | 0 | 0 | Sí (manual + transformer) |
| DigitalEnvelopeCertificatesController | 3 | 3 | 3 | Sí (manual + transformer) |
| FinancialInstitutionsController | 5 | 0 | 0 | Sí (manual + transformer) |
| IncomingNachaCommandCenterController | 9 | 9 | 9 | Sí (manual + transformer) |
| InstitutionClearingHousePreferencesController | 4 | 0 | 0 | Sí (manual + transformer) |
| IntegrationCatalogController | 4 | 0 | 0 | Sí (manual + transformer) |
| IntegrationMappingSetsController | 12 | 0 | 0 | Sí (manual + transformer) |
| JwksController | 4 | 0 | 0 | Sí (manual + transformer) |
| LoginLockoutSettingsController | 2 | 0 | 0 | Sí (manual + transformer) |
| MaintenanceController | 1 | 0 | 0 | Sí (manual + transformer) |
| MenuItemsController | 4 | 0 | 0 | Sí (manual + transformer) |
| NachaConfigProfilesController | 17 | 0 | 0 | Sí (manual + transformer) |
| NachaController | 1 | 0 | 0 | Sí (manual + transformer) |
| NachaExportController | 2 | 2 | 2 | Sí (manual + transformer) |
| NachaRecordDefinitionsController | 5 | 0 | 0 | Sí (manual + transformer) |
| NachaRecordLayoutsController | 5 | 0 | 0 | Sí (manual + transformer) |
| NachaSecurityOperationsController | 8 | 8 | 8 | Sí (manual + transformer) |
| NachaUploadController | 2 | 0 | 0 | Sí (manual + transformer) |
| NavigationController | 1 | 0 | 0 | Sí (manual + transformer) |
| NavigationLogsController | 2 | 0 | 0 | Sí (manual + transformer) |
| OauthsController | 2 | 0 | 0 | Sí (manual + transformer) |
| PasswordRulesController | 2 | 0 | 0 | Sí (manual + transformer) |
| PaymentRailCapabilityRegistryController | 3 | 3 | 3 | Sí (manual + transformer) |
| PermissionsController | 1 | 0 | 0 | Sí (manual + transformer) |
| RegulatoryCatalogsController | 6 | 0 | 0 | Sí (manual + transformer) |
| ReportsController | 19 | 19 | 19 | Sí (manual + transformer) |
| ReturnReasonsController | 1 | 0 | 0 | Sí (manual + transformer) |
| RolesController | 1 | 0 | 0 | Sí (manual + transformer) |
| ServersController | 1 | 0 | 0 | Sí (manual + transformer) |
| SoapIntegrationSettingsController | 2 | 0 | 0 | Sí (manual + transformer) |
| SobreDigitalController | 3 | 0 | 0 | Sí (manual + transformer) |
| TaskDefinitionsController | 5 | 0 | 0 | Sí (manual + transformer) |
| TestsController | 2 | 0 | 0 | Sí (manual + transformer) |
| TransactionsController | 7 | 0 | 0 | Sí (manual + transformer) |
| UsersController | 7 | 0 | 0 | Sí (manual + transformer) |

## Muestreo operativo por ruta
| Controlador | Acción | Método | Ruta | Summary explícito | Description explícito | Permiso cercano (atributo) |
|---|---|---|---|---|---|---|
| AchCyclesController | Get | GET | `/ach-cycles` | No | No | N/D en bloque inmediato |
| AchCyclesController | GetExportable | GET | `/ach-cycles/exportable` | No | No | N/D en bloque inmediato |
| AchCyclesController | GetById | GET | `/ach-cycles/{id}` | No | No | N/D en bloque inmediato |
| AchCyclesController | Create | POST | `/ach-cycles` | No | No | Policy = "CanReadAch" |
| AchCyclesController | Update | PUT | `/ach-cycles/{id}` | No | No | Policy = "CanManageAch" |
| AchCyclesController | Delete | DELETE | `/ach-cycles/{id}` | No | No | Policy = "CanManageAch" |
| AchReturnsController | GetTransactionsByCycle | GET | `/ach-returns/cycles/{cycleId}/transactions` | Sí | Sí | N/D en bloque inmediato |
| AchReturnsController | GenerateFile | POST | `/ach-returns/generate-file` | Sí | Sí | N/D en bloque inmediato |
| AchTraceabilityController | CertifyWithSol02 | POST | `/api/ach-traceability/sol02/{transactionId:int}/certify` | Sí | Sí | N/D en bloque inmediato |
| AchTraceabilityController | GetTransactionTraceability | GET | `/api/ach-traceability/transactions/{transactionId:int}` | Sí | Sí | N/D en bloque inmediato |
| AchTraceabilityController | GetTraceabilityReport | GET | `/api/ach-traceability/report` | Sí | Sí | N/D en bloque inmediato |
| AuditLogsController | GetAuditLogsAsync | GET | `/api/audit-logs` | No | No | N/D en bloque inmediato |
| AuthController | Login | POST | `/[controller]/login` | No | No | N/D en bloque inmediato |
| AuthController | ForgotPassword | POST | `/[controller]/forgot-password` | No | No | N/D en bloque inmediato |
| AuthController | ResetPassword | POST | `/[controller]/reset-password` | No | No | N/D en bloque inmediato |
| AuthController | RefreshSession | POST | `/[controller]/refresh` | No | No | N/D en bloque inmediato |
| AuthLogsController | GetAuthLogsAsync | GET | `/api/auth-logs` | No | No | N/D en bloque inmediato |
| BankHolidaysController | GetAll | GET | `/bank-holidays` | No | No | N/D en bloque inmediato |
| BankHolidaysController | Create | POST | `/bank-holidays` | No | No | Policy = "CanManageAch" |
| BankHolidaysController | Update | PUT | `/bank-holidays/{id}` | No | No | Policy = "CanManageAch" |
| BankHolidaysController | Delete | DELETE | `/bank-holidays/{id}` | No | No | Policy = "CanManageAch" |
| BrandingController | GetBrandingAsync | GET | `/api/users/branding` | No | No | N/D en bloque inmediato |
| BrandingController | SaveBrandingAsync | PUT | `/api/users/branding` | No | No | N/D en bloque inmediato |
| BulkIngestionController | Upload | POST | `/api/transactions/bulk-ingestion/upload` | No | No | N/D en bloque inmediato |
| BulkIngestionController | GetBatch | GET | `/api/transactions/bulk-ingestion/{batchId:guid}` | No | No | N/D en bloque inmediato |
| BulkIngestionController | GetBatchItems | GET | `/api/transactions/bulk-ingestion/{batchId:guid}/items` | No | No | N/D en bloque inmediato |
| BulkIngestionController | GetBatchSummary | GET | `/api/transactions/bulk-ingestion/{batchId:guid}/summary` | No | No | N/D en bloque inmediato |
| BulkIngestionController | Retry | POST | `/api/transactions/bulk-ingestion/{batchId:guid}/retry` | No | No | N/D en bloque inmediato |
| BulkIngestionController | Cancel | POST | `/api/transactions/bulk-ingestion/{batchId:guid}/cancel` | No | No | N/D en bloque inmediato |
| CatalogTypesController | GetAll | GET | `/catalog-types/{catalogType}` | No | No | Authorize |
| CatalogTypesController | Create | POST | `/catalog-types/{catalogType}` | No | No | N/D en bloque inmediato |
| CatalogTypesController | Update | PUT | `/catalog-types/{catalogType}/{code}` | No | No | N/D en bloque inmediato |
| CatalogTypesController | Delete | DELETE | `/catalog-types/{catalogType}/{code}` | No | No | N/D en bloque inmediato |
| CenitOperationsController | GetQueueAsync | GET | `/api/cenit/queues` | Sí | Sí | Authorize |
| CenitOperationsController | GetNetPositionsAsync | GET | `/api/cenit/net-positions` | Sí | Sí | N/D en bloque inmediato |
| CenitOperationsController | GetOptimizationDecisionsAsync | GET | `/api/cenit/optimization-decisions` | Sí | Sí | N/D en bloque inmediato |
| CenitOperationsController | GetTraceabilityAsync | GET | `/api/cenit/traceability` | Sí | Sí | N/D en bloque inmediato |
| CertificateManagementController | UploadPublicAsync | POST | `/nacha-security/certificates/management/public` | Sí | Sí | N/D en bloque inmediato |
| CertificateManagementController | UploadPrivateAsync | POST | `/nacha-security/certificates/management/private` | Sí | Sí | N/D en bloque inmediato |
| CertificateManagementController | ListAsync | GET | `/nacha-security/certificates/management` | Sí | Sí | N/D en bloque inmediato |
| CertificateManagementController | ListVersionsAsync | GET | `/nacha-security/certificates/management/{id:int}/versions` | Sí | Sí | Policy = "CanReadAch" |
| CertificateManagementController | ActivateAsync | POST | `/nacha-security/certificates/management/versions/{id:int}/activate` | Sí | Sí | Policy = "CanReadAch" |
| CertificateManagementController | RevokeAsync | POST | `/nacha-security/certificates/management/versions/{id:int}/revoke` | Sí | Sí | Policy = FineGrainedPermissions.CanManageCertificates |
| CertificateManagementController | ValidateAsync | POST | `/nacha-security/certificates/management/versions/{id:int}/validate` | Sí | Sí | Policy = FineGrainedPermissions.CanManageCertificates |
| CertificateManagementController | AuditAsync | GET | `/nacha-security/certificates/management/audit` | Sí | Sí | Policy = FineGrainedPermissions.CanManageCertificates |
| ClearingHouseCycleConfigsController | GetByClearingHouse | GET | `/clearing-house-cycle-configs` | No | No | Authorize |
| ClearingHouseCycleConfigsController | GetCurrentByClearingHouse | GET | `/clearing-house-cycle-configs/current` | No | No | Policy = "CanManageAch" |
| ClearingHouseCycleConfigsController | CreateVersion | POST | `/clearing-house-cycle-configs` | No | No | Policy = "CanManageAch" |
| ClearingHouseCycleConfigsController | Inactivate | POST | `/clearing-house-cycle-configs/{id:int}/inactivate` | No | No | Policy = "CanManageAch" |
| ClearingHouseSpecialDatesController | GetAll | GET | `/clearing-house-special-dates` | No | No | N/D en bloque inmediato |
| ClearingHouseSpecialDatesController | Create | POST | `/clearing-house-special-dates` | No | No | Policy = "CanManageAch" |
| ClearingHouseSpecialDatesController | Update | PUT | `/clearing-house-special-dates/{id}` | No | No | Policy = "CanManageAch" |
| ClearingHouseSpecialDatesController | Delete | DELETE | `/clearing-house-special-dates/{id}` | No | No | Policy = "CanManageAch" |
| ClearingHousesController | Get | GET | `/clearing-houses` | No | No | N/D en bloque inmediato |
| ClearingHousesController | GetById | GET | `/clearing-houses/{id:int}` | No | No | Policy = "CanReadAch" |
| ClearingHousesController | GetCyclesForClearingHouse | GET | `/clearing-houses/{id:int}/cycles` | No | No | Policy = "CanReadAch" |
| CompanyEntryDescriptionsController | GetAll | GET | `/company-entry-descriptions` | No | No | Authorize |
| CompanyEntryDescriptionsController | Create | POST | `/company-entry-descriptions` | No | No | Policy = "CanManageAch" |
| CompanyEntryDescriptionsController | Update | PUT | `/company-entry-descriptions/{id:int}` | No | No | N/D en bloque inmediato |
| CompanyEntryDescriptionsController | Delete | DELETE | `/company-entry-descriptions/{id:int}` | No | No | N/D en bloque inmediato |
| CustomerThirdPartiesController | Get | GET | `/api/customer-third-parties` | No | No | Authorize |
| CustomerThirdPartiesController | UpdateStatus | PATCH | `/api/customer-third-parties/{id:int}/status` | No | No | N/D en bloque inmediato |
| CustomersController | GetAll | GET | `/customers` | No | No | N/D en bloque inmediato |
| CustomersController | GetById | GET | `/customers/{id:int}` | No | No | Policy = "CanReadAch" |
| CustomersController | Create | POST | `/customers` | No | No | N/D en bloque inmediato |
| CustomersController | Update | PUT | `/customers/{id:int}` | No | No | N/D en bloque inmediato |
| CustomersController | Delete | DELETE | `/customers/{id:int}` | No | No | N/D en bloque inmediato |
| DigitalEnvelopeCertificatesController | GetAsync | GET | `/nacha-security/certificates` | Sí | Sí | Authorize |
| DigitalEnvelopeCertificatesController | UploadAsync | POST | `/nacha-security/certificates` | Sí | Sí | N/D en bloque inmediato |
| DigitalEnvelopeCertificatesController | DeleteAsync | DELETE | `/nacha-security/certificates/{id:int}` | Sí | Sí | N/D en bloque inmediato |
| FinancialInstitutionsController | GetAll | GET | `/financial-institutions` | No | No | N/D en bloque inmediato |
| FinancialInstitutionsController | GetById | GET | `/financial-institutions/{id}` | No | No | Policy = "CanReadAch" |
| FinancialInstitutionsController | Create | POST | `/financial-institutions` | No | No | Policy = "CanReadAch" |
| FinancialInstitutionsController | Update | PUT | `/financial-institutions/{id}` | No | No | Policy = "CanManageAch" |
| FinancialInstitutionsController | SetStatus | PATCH | `/financial-institutions/{id}/status` | No | No | Policy = "CanManageAch" |
| IncomingNachaCommandCenterController | GetObservabilitySummary | GET | `/incoming-nacha-command-center/observability/summary` | Sí | Sí | N/D en bloque inmediato |
| IncomingNachaCommandCenterController | GetIngestions | GET | `/incoming-nacha-command-center/ingestions` | Sí | Sí | N/D en bloque inmediato |
| IncomingNachaCommandCenterController | GetIngestionDetail | GET | `/incoming-nacha-command-center/ingestions/{ingestionId:guid}` | Sí | Sí | N/D en bloque inmediato |
| IncomingNachaCommandCenterController | GetQueue | GET | `/incoming-nacha-command-center/queue` | Sí | Sí | N/D en bloque inmediato |
| IncomingNachaCommandCenterController | GetQueueDetail | GET | `/incoming-nacha-command-center/queue/{queueId:guid}` | Sí | Sí | N/D en bloque inmediato |
| IncomingNachaCommandCenterController | RetryManual | POST | `/incoming-nacha-command-center/queue/{queueId:guid}/retry` | Sí | Sí | N/D en bloque inmediato |
| IncomingNachaCommandCenterController | UnblockManual | POST | `/incoming-nacha-command-center/queue/{queueId:guid}/unblock` | Sí | Sí | N/D en bloque inmediato |
| IncomingNachaCommandCenterController | RequeueManual | POST | `/incoming-nacha-command-center/queue/{queueId:guid}/requeue` | Sí | Sí | N/D en bloque inmediato |
| IncomingNachaCommandCenterController | MarkFailedFinal | POST | `/incoming-nacha-command-center/queue/{queueId:guid}/mark-failed-final` | Sí | Sí | N/D en bloque inmediato |
| InstitutionClearingHousePreferencesController | GetAll | GET | `/institution-clearing-house-preferences` | No | No | N/D en bloque inmediato |
| InstitutionClearingHousePreferencesController | Create | POST | `/institution-clearing-house-preferences` | No | No | Policy = "CanManageAch" |
| InstitutionClearingHousePreferencesController | Update | PUT | `/institution-clearing-house-preferences/{id}` | No | No | Policy = "CanManageAch" |
| InstitutionClearingHousePreferencesController | Delete | DELETE | `/institution-clearing-house-preferences/{id}` | No | No | N/D en bloque inmediato |
| IntegrationCatalogController | GetMethods | GET | `/api/integrations/methods` | No | No | Authorize |
| IntegrationCatalogController | GetParametersByMethod | GET | `/api/integrations/methods/{methodId:int}/parameters` | No | No | Policy = "CanManageAch" |
| IntegrationCatalogController | GetSourceCatalog | GET | `/api/integrations/source-catalog` | No | No | Policy = "CanManageAch" |
| IntegrationCatalogController | GetTransformations | GET | `/api/integrations/transformations` | No | No | Policy = "CanManageAch" |
| IntegrationMappingSetsController | Get | GET | `/api/integrations/mappingsets` | No | No | Authorize |
| IntegrationMappingSetsController | GetPublished | GET | `/api/integrations/mappingsets/published` | No | No | Policy = "CanManageAch" |
| IntegrationMappingSetsController | GetById | GET | `/api/integrations/mappingsets/{id:guid}` | No | No | Policy = "CanManageAch" |
| IntegrationMappingSetsController | Create | POST | `/api/integrations/mappingsets` | No | No | Policy = "CanManageAch" |
| IntegrationMappingSetsController | Update | PUT | `/api/integrations/mappingsets/{id:guid}` | No | No | Policy = "CanManageAch" |
| IntegrationMappingSetsController | UpsertRules | PUT | `/api/integrations/mappingsets/{id:guid}/rules` | No | No | Policy = "CanManageAch" |
| IntegrationMappingSetsController | Validate | POST | `/api/integrations/mappingsets/{id:guid}/validate` | No | No | Policy = "CanManageAch" |
| IntegrationMappingSetsController | Preview | POST | `/api/integrations/mappingsets/{id:guid}/preview` | No | No | Policy = "CanManageAch" |
| IntegrationMappingSetsController | Publish | POST | `/api/integrations/mappingsets/{id:guid}/publish` | No | No | Policy = "CanManageAch" |
| IntegrationMappingSetsController | Clone | POST | `/api/integrations/mappingsets/{id:guid}/clone` | No | No | Policy = "CanManageAch" |
| IntegrationMappingSetsController | History | GET | `/api/integrations/mappingsets/{id:guid}/history` | No | No | Policy = "CanManageAch" |
| IntegrationMappingSetsController | Compare | POST | `/api/integrations/mappingsets/compare` | No | No | Policy = "CanManageAch" |
| JwksController | GetJwks | GET | `/oauth2/jwks` | No | No | N/D en bloque inmediato |
| JwksController | TokenClientAssertions | GET | `/oauth2/TokenClientAssertions` | No | No | N/D en bloque inmediato |
| JwksController | Authenticate | POST | `/oauth2/client-assertion` | No | No | N/D en bloque inmediato |
| JwksController | GenerateClientAssertion | POST | `/oauth2/Genearte-client-assertion` | No | No | N/D en bloque inmediato |
| LoginLockoutSettingsController | GetAsync | GET | `/api/users/login-lockout` | No | No | N/D en bloque inmediato |
| LoginLockoutSettingsController | SaveAsync | PUT | `/api/users/login-lockout` | No | No | N/D en bloque inmediato |
| MaintenanceController | RunDbInitializer | POST | `/[controller]/seed` | No | No | N/D en bloque inmediato |
| MenuItemsController | GetMenuItemsAsync | GET | `/navigation/menu-items` | No | No | N/D en bloque inmediato |
| MenuItemsController | CreateMenuItemAsync | POST | `/navigation/menu-items` | No | No | N/D en bloque inmediato |
| MenuItemsController | UpdateMenuItemAsync | PUT | `/navigation/menu-items/{id:int}` | No | No | N/D en bloque inmediato |
| MenuItemsController | DeleteMenuItemAsync | DELETE | `/navigation/menu-items/{id:int}` | No | No | N/D en bloque inmediato |
| NachaConfigProfilesController | GetProfiles | GET | `/nacha-config/perfiles` | No | No | N/D en bloque inmediato |
| NachaConfigProfilesController | GetFilterCatalogs | GET | `/nacha-config/catalogos-filtro` | No | No | Policy = "CanReadAch" |
| NachaConfigProfilesController | GetProfile | GET | `/nacha-config/perfiles/{id:int}` | No | No | Policy = "CanReadAch" |
| NachaConfigProfilesController | CreateDraft | POST | `/nacha-config/perfiles` | No | No | Policy = "CanReadAch" |
| NachaConfigProfilesController | UpdateDraft | PUT | `/nacha-config/perfiles/{id:int}` | No | No | Policy = "CanManageAch" |

> Nota: el muestreo muestra las primeras 120 operaciones ordenadas por archivo; el total consolidado se calcula sobre todas las operaciones detectadas.