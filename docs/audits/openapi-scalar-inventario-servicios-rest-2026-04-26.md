# Inventario REST expuesto en OpenAPI/Scalar — ACHInterbank

**Fecha de inventario (UTC):** 2026-04-26  
**Scope:** `src/Cfa.ACHInterbank.Api` (solo análisis, sin cambios funcionales).

## 1) Hallazgos de publicación OpenAPI/Scalar

- La API registra OpenAPI con `services.AddOpenApi("v1")`.
- El documento JSON se publica en `/openapi/{documentName}.json`.
- Scalar se publica con `app.MapScalarApiReference()` y redirección raíz a `/scalar`.
- No se detectaron controladores con `[ApiExplorerSettings(IgnoreApi = true)]`; por tanto, los endpoints de controladores son **visibles o potencialmente visibles** en Scalar (sujeto a autorización/runtime).

## 2) Cobertura inventariada

- **Controladores:** 52
- **Operaciones HTTP:** 220
- **GET:** 114 | **POST:** 67 | **PUT:** 24 | **PATCH:** 2 | **DELETE:** 13

## 3) Catálogo de endpoints (por controlador)

### AchCyclesController  
Base route: `/ach-cycles`  
Operaciones: 6

| Método | Ruta final | Fuente |
|---|---|---|
| `GET` | `/ach-cycles` | `src/Cfa.ACHInterbank.Api/Controllers/AchCyclesController.cs:23` |
| `GET` | `/ach-cycles/exportable` | `src/Cfa.ACHInterbank.Api/Controllers/AchCyclesController.cs:42` |
| `GET` | `/ach-cycles/{id}` | `src/Cfa.ACHInterbank.Api/Controllers/AchCyclesController.cs:57` |
| `POST` | `/ach-cycles` | `src/Cfa.ACHInterbank.Api/Controllers/AchCyclesController.cs:68` |
| `PUT` | `/ach-cycles/{id}` | `src/Cfa.ACHInterbank.Api/Controllers/AchCyclesController.cs:79` |
| `DELETE` | `/ach-cycles/{id}` | `src/Cfa.ACHInterbank.Api/Controllers/AchCyclesController.cs:90` |

### AchReturnsController  
Base route: `/ach-returns`  
Operaciones: 2

| Método | Ruta final | Fuente |
|---|---|---|
| `GET` | `/ach-returns/cycles/{cycleId}/transactions` | `src/Cfa.ACHInterbank.Api/Controllers/AchReturnsController.cs:11` |
| `POST` | `/ach-returns/generate-file` | `src/Cfa.ACHInterbank.Api/Controllers/AchReturnsController.cs:19` |

### AchTraceabilityController  
Base route: `/api/ach-traceability`  
Operaciones: 3

| Método | Ruta final | Fuente |
|---|---|---|
| `POST` | `/api/ach-traceability/sol02/{transactionId:int}/certify` | `src/Cfa.ACHInterbank.Api/Controllers/AchTraceabilityController.cs:18` |
| `GET` | `/api/ach-traceability/transactions/{transactionId:int}` | `src/Cfa.ACHInterbank.Api/Controllers/AchTraceabilityController.cs:50` |
| `GET` | `/api/ach-traceability/report` | `src/Cfa.ACHInterbank.Api/Controllers/AchTraceabilityController.cs:62` |

### AuditLogsController  
Base route: `/api/audit-logs`  
Operaciones: 1

| Método | Ruta final | Fuente |
|---|---|---|
| `GET` | `/api/audit-logs` | `src/Cfa.ACHInterbank.Api/Controllers/AuditLogsController.cs:25` |

### AuthController  
Base route: `/Auth`  
Operaciones: 4

| Método | Ruta final | Fuente |
|---|---|---|
| `POST` | `/Auth/login` | `src/Cfa.ACHInterbank.Api/Controllers/AuthController.cs:24` |
| `POST` | `/Auth/forgot-password` | `src/Cfa.ACHInterbank.Api/Controllers/AuthController.cs:86` |
| `POST` | `/Auth/reset-password` | `src/Cfa.ACHInterbank.Api/Controllers/AuthController.cs:101` |
| `POST` | `/Auth/refresh` | `src/Cfa.ACHInterbank.Api/Controllers/AuthController.cs:116` |

### AuthLogsController  
Base route: `/api/auth-logs`  
Operaciones: 1

| Método | Ruta final | Fuente |
|---|---|---|
| `GET` | `/api/auth-logs` | `src/Cfa.ACHInterbank.Api/Controllers/AuthLogsController.cs:25` |

### BankHolidaysController  
Base route: `/bank-holidays`  
Operaciones: 4

| Método | Ruta final | Fuente |
|---|---|---|
| `GET` | `/bank-holidays` | `src/Cfa.ACHInterbank.Api/Controllers/BankHolidaysController.cs:23` |
| `POST` | `/bank-holidays` | `src/Cfa.ACHInterbank.Api/Controllers/BankHolidaysController.cs:31` |
| `PUT` | `/bank-holidays/{id}` | `src/Cfa.ACHInterbank.Api/Controllers/BankHolidaysController.cs:39` |
| `DELETE` | `/bank-holidays/{id}` | `src/Cfa.ACHInterbank.Api/Controllers/BankHolidaysController.cs:50` |

### BrandingController  
Base route: `/api/users/branding`  
Operaciones: 2

| Método | Ruta final | Fuente |
|---|---|---|
| `GET` | `/api/users/branding` | `src/Cfa.ACHInterbank.Api/Controllers/BrandingController.cs:22` |
| `PUT` | `/api/users/branding` | `src/Cfa.ACHInterbank.Api/Controllers/BrandingController.cs:33` |

### BulkIngestionController  
Base route: `/api/transactions/bulk-ingestion`  
Operaciones: 6

| Método | Ruta final | Fuente |
|---|---|---|
| `POST` | `/api/transactions/bulk-ingestion/upload` | `src/Cfa.ACHInterbank.Api/Controllers/BulkIngestionController.cs:36` |
| `GET` | `/api/transactions/bulk-ingestion/{batchId:guid}` | `src/Cfa.ACHInterbank.Api/Controllers/BulkIngestionController.cs:89` |
| `GET` | `/api/transactions/bulk-ingestion/{batchId:guid}/items` | `src/Cfa.ACHInterbank.Api/Controllers/BulkIngestionController.cs:100` |
| `GET` | `/api/transactions/bulk-ingestion/{batchId:guid}/summary` | `src/Cfa.ACHInterbank.Api/Controllers/BulkIngestionController.cs:119` |
| `POST` | `/api/transactions/bulk-ingestion/{batchId:guid}/retry` | `src/Cfa.ACHInterbank.Api/Controllers/BulkIngestionController.cs:130` |
| `POST` | `/api/transactions/bulk-ingestion/{batchId:guid}/cancel` | `src/Cfa.ACHInterbank.Api/Controllers/BulkIngestionController.cs:155` |

### CatalogTypesController  
Base route: `/catalog-types`  
Operaciones: 4

| Método | Ruta final | Fuente |
|---|---|---|
| `GET` | `/catalog-types/{catalogType}` | `src/Cfa.ACHInterbank.Api/Controllers/CatalogTypesController.cs:20` |
| `POST` | `/catalog-types/{catalogType}` | `src/Cfa.ACHInterbank.Api/Controllers/CatalogTypesController.cs:35` |
| `PUT` | `/catalog-types/{catalogType}/{code}` | `src/Cfa.ACHInterbank.Api/Controllers/CatalogTypesController.cs:54` |
| `DELETE` | `/catalog-types/{catalogType}/{code}` | `src/Cfa.ACHInterbank.Api/Controllers/CatalogTypesController.cs:73` |

### CenitOperationsController  
Base route: `/api/cenit`  
Operaciones: 4

| Método | Ruta final | Fuente |
|---|---|---|
| `GET` | `/api/cenit/queues` | `src/Cfa.ACHInterbank.Api/Controllers/CenitOperationsController.cs:21` |
| `GET` | `/api/cenit/net-positions` | `src/Cfa.ACHInterbank.Api/Controllers/CenitOperationsController.cs:78` |
| `GET` | `/api/cenit/optimization-decisions` | `src/Cfa.ACHInterbank.Api/Controllers/CenitOperationsController.cs:130` |
| `GET` | `/api/cenit/traceability` | `src/Cfa.ACHInterbank.Api/Controllers/CenitOperationsController.cs:186` |

### CertificateManagementController  
Base route: `/nacha-security/certificates/management`  
Operaciones: 8

| Método | Ruta final | Fuente |
|---|---|---|
| `POST` | `/nacha-security/certificates/management/public` | `src/Cfa.ACHInterbank.Api/Controllers/CertificateManagementController.cs:34` |
| `POST` | `/nacha-security/certificates/management/private` | `src/Cfa.ACHInterbank.Api/Controllers/CertificateManagementController.cs:57` |
| `GET` | `/nacha-security/certificates/management` | `src/Cfa.ACHInterbank.Api/Controllers/CertificateManagementController.cs:83` |
| `GET` | `/nacha-security/certificates/management/{id:int}/versions` | `src/Cfa.ACHInterbank.Api/Controllers/CertificateManagementController.cs:91` |
| `POST` | `/nacha-security/certificates/management/versions/{id:int}/activate` | `src/Cfa.ACHInterbank.Api/Controllers/CertificateManagementController.cs:99` |
| `POST` | `/nacha-security/certificates/management/versions/{id:int}/revoke` | `src/Cfa.ACHInterbank.Api/Controllers/CertificateManagementController.cs:107` |
| `POST` | `/nacha-security/certificates/management/versions/{id:int}/validate` | `src/Cfa.ACHInterbank.Api/Controllers/CertificateManagementController.cs:115` |
| `GET` | `/nacha-security/certificates/management/audit` | `src/Cfa.ACHInterbank.Api/Controllers/CertificateManagementController.cs:122` |

### ClearingHouseCycleConfigsController  
Base route: `/clearing-house-cycle-configs`  
Operaciones: 4

| Método | Ruta final | Fuente |
|---|---|---|
| `GET` | `/clearing-house-cycle-configs` | `src/Cfa.ACHInterbank.Api/Controllers/ClearingHouseCycleConfigsController.cs:20` |
| `GET` | `/clearing-house-cycle-configs/current` | `src/Cfa.ACHInterbank.Api/Controllers/ClearingHouseCycleConfigsController.cs:28` |
| `POST` | `/clearing-house-cycle-configs` | `src/Cfa.ACHInterbank.Api/Controllers/ClearingHouseCycleConfigsController.cs:36` |
| `POST` | `/clearing-house-cycle-configs/{id:int}/inactivate` | `src/Cfa.ACHInterbank.Api/Controllers/ClearingHouseCycleConfigsController.cs:41` |

### ClearingHouseSpecialDatesController  
Base route: `/clearing-house-special-dates`  
Operaciones: 4

| Método | Ruta final | Fuente |
|---|---|---|
| `GET` | `/clearing-house-special-dates` | `src/Cfa.ACHInterbank.Api/Controllers/ClearingHouseSpecialDatesController.cs:23` |
| `POST` | `/clearing-house-special-dates` | `src/Cfa.ACHInterbank.Api/Controllers/ClearingHouseSpecialDatesController.cs:31` |
| `PUT` | `/clearing-house-special-dates/{id}` | `src/Cfa.ACHInterbank.Api/Controllers/ClearingHouseSpecialDatesController.cs:39` |
| `DELETE` | `/clearing-house-special-dates/{id}` | `src/Cfa.ACHInterbank.Api/Controllers/ClearingHouseSpecialDatesController.cs:50` |

### ClearingHousesController  
Base route: `/clearing-houses`  
Operaciones: 3

| Método | Ruta final | Fuente |
|---|---|---|
| `GET` | `/clearing-houses` | `src/Cfa.ACHInterbank.Api/Controllers/ClearingHousesController.cs:25` |
| `GET` | `/clearing-houses/{id:int}` | `src/Cfa.ACHInterbank.Api/Controllers/ClearingHousesController.cs:36` |
| `GET` | `/clearing-houses/{id:int}/cycles` | `src/Cfa.ACHInterbank.Api/Controllers/ClearingHousesController.cs:47` |

### CompanyEntryDescriptionsController  
Base route: `/company-entry-descriptions`  
Operaciones: 4

| Método | Ruta final | Fuente |
|---|---|---|
| `GET` | `/company-entry-descriptions` | `src/Cfa.ACHInterbank.Api/Controllers/CompanyEntryDescriptionsController.cs:20` |
| `POST` | `/company-entry-descriptions` | `src/Cfa.ACHInterbank.Api/Controllers/CompanyEntryDescriptionsController.cs:28` |
| `PUT` | `/company-entry-descriptions/{id:int}` | `src/Cfa.ACHInterbank.Api/Controllers/CompanyEntryDescriptionsController.cs:47` |
| `DELETE` | `/company-entry-descriptions/{id:int}` | `src/Cfa.ACHInterbank.Api/Controllers/CompanyEntryDescriptionsController.cs:70` |

### CustomerThirdPartiesController  
Base route: `/api/customer-third-parties`  
Operaciones: 2

| Método | Ruta final | Fuente |
|---|---|---|
| `GET` | `/api/customer-third-parties` | `src/Cfa.ACHInterbank.Api/Controllers/CustomerThirdPartiesController.cs:21` |
| `PATCH` | `/api/customer-third-parties/{id:int}/status` | `src/Cfa.ACHInterbank.Api/Controllers/CustomerThirdPartiesController.cs:56` |

### CustomersController  
Base route: `/customers`  
Operaciones: 5

| Método | Ruta final | Fuente |
|---|---|---|
| `GET` | `/customers` | `src/Cfa.ACHInterbank.Api/Controllers/CustomersController.cs:23` |
| `GET` | `/customers/{id:int}` | `src/Cfa.ACHInterbank.Api/Controllers/CustomersController.cs:35` |
| `POST` | `/customers` | `src/Cfa.ACHInterbank.Api/Controllers/CustomersController.cs:54` |
| `PUT` | `/customers/{id:int}` | `src/Cfa.ACHInterbank.Api/Controllers/CustomersController.cs:79` |
| `DELETE` | `/customers/{id:int}` | `src/Cfa.ACHInterbank.Api/Controllers/CustomersController.cs:110` |

### DigitalEnvelopeCertificatesController  
Base route: `/nacha-security/certificates`  
Operaciones: 3

| Método | Ruta final | Fuente |
|---|---|---|
| `GET` | `/nacha-security/certificates` | `src/Cfa.ACHInterbank.Api/Controllers/DigitalEnvelopeCertificatesController.cs:28` |
| `POST` | `/nacha-security/certificates` | `src/Cfa.ACHInterbank.Api/Controllers/DigitalEnvelopeCertificatesController.cs:39` |
| `DELETE` | `/nacha-security/certificates/{id:int}` | `src/Cfa.ACHInterbank.Api/Controllers/DigitalEnvelopeCertificatesController.cs:85` |

### FinancialInstitutionsController  
Base route: `/financial-institutions`  
Operaciones: 5

| Método | Ruta final | Fuente |
|---|---|---|
| `GET` | `/financial-institutions` | `src/Cfa.ACHInterbank.Api/Controllers/FinancialInstitutionsController.cs:24` |
| `GET` | `/financial-institutions/{id}` | `src/Cfa.ACHInterbank.Api/Controllers/FinancialInstitutionsController.cs:32` |
| `POST` | `/financial-institutions` | `src/Cfa.ACHInterbank.Api/Controllers/FinancialInstitutionsController.cs:40` |
| `PUT` | `/financial-institutions/{id}` | `src/Cfa.ACHInterbank.Api/Controllers/FinancialInstitutionsController.cs:48` |
| `PATCH` | `/financial-institutions/{id}/status` | `src/Cfa.ACHInterbank.Api/Controllers/FinancialInstitutionsController.cs:59` |

### IncomingNachaCommandCenterController  
Base route: `/incoming-nacha-command-center`  
Operaciones: 9

| Método | Ruta final | Fuente |
|---|---|---|
| `GET` | `/incoming-nacha-command-center/observability/summary` | `src/Cfa.ACHInterbank.Api/Controllers/IncomingNachaCommandCenterController.cs:22` |
| `GET` | `/incoming-nacha-command-center/ingestions` | `src/Cfa.ACHInterbank.Api/Controllers/IncomingNachaCommandCenterController.cs:26` |
| `GET` | `/incoming-nacha-command-center/ingestions/{ingestionId:guid}` | `src/Cfa.ACHInterbank.Api/Controllers/IncomingNachaCommandCenterController.cs:30` |
| `GET` | `/incoming-nacha-command-center/queue` | `src/Cfa.ACHInterbank.Api/Controllers/IncomingNachaCommandCenterController.cs:37` |
| `GET` | `/incoming-nacha-command-center/queue/{queueId:guid}` | `src/Cfa.ACHInterbank.Api/Controllers/IncomingNachaCommandCenterController.cs:41` |
| `POST` | `/incoming-nacha-command-center/queue/{queueId:guid}/retry` | `src/Cfa.ACHInterbank.Api/Controllers/IncomingNachaCommandCenterController.cs:48` |
| `POST` | `/incoming-nacha-command-center/queue/{queueId:guid}/unblock` | `src/Cfa.ACHInterbank.Api/Controllers/IncomingNachaCommandCenterController.cs:62` |
| `POST` | `/incoming-nacha-command-center/queue/{queueId:guid}/requeue` | `src/Cfa.ACHInterbank.Api/Controllers/IncomingNachaCommandCenterController.cs:76` |
| `POST` | `/incoming-nacha-command-center/queue/{queueId:guid}/mark-failed-final` | `src/Cfa.ACHInterbank.Api/Controllers/IncomingNachaCommandCenterController.cs:90` |

### InstitutionClearingHousePreferencesController  
Base route: `/institution-clearing-house-preferences`  
Operaciones: 4

| Método | Ruta final | Fuente |
|---|---|---|
| `GET` | `/institution-clearing-house-preferences` | `src/Cfa.ACHInterbank.Api/Controllers/InstitutionClearingHousePreferencesController.cs:24` |
| `POST` | `/institution-clearing-house-preferences` | `src/Cfa.ACHInterbank.Api/Controllers/InstitutionClearingHousePreferencesController.cs:32` |
| `PUT` | `/institution-clearing-house-preferences/{id}` | `src/Cfa.ACHInterbank.Api/Controllers/InstitutionClearingHousePreferencesController.cs:40` |
| `DELETE` | `/institution-clearing-house-preferences/{id}` | `src/Cfa.ACHInterbank.Api/Controllers/InstitutionClearingHousePreferencesController.cs:70` |

### IntegrationCatalogController  
Base route: `/api/integrations`  
Operaciones: 4

| Método | Ruta final | Fuente |
|---|---|---|
| `GET` | `/api/integrations/methods` | `src/Cfa.ACHInterbank.Api/Controllers/IntegrationCatalogController.cs:19` |
| `GET` | `/api/integrations/methods/{methodId:int}/parameters` | `src/Cfa.ACHInterbank.Api/Controllers/IntegrationCatalogController.cs:24` |
| `GET` | `/api/integrations/source-catalog` | `src/Cfa.ACHInterbank.Api/Controllers/IntegrationCatalogController.cs:29` |
| `GET` | `/api/integrations/transformations` | `src/Cfa.ACHInterbank.Api/Controllers/IntegrationCatalogController.cs:34` |

### IntegrationMappingSetsController  
Base route: `/api/integrations/mappingsets`  
Operaciones: 12

| Método | Ruta final | Fuente |
|---|---|---|
| `GET` | `/api/integrations/mappingsets` | `src/Cfa.ACHInterbank.Api/Controllers/IntegrationMappingSetsController.cs:20` |
| `GET` | `/api/integrations/mappingsets/published` | `src/Cfa.ACHInterbank.Api/Controllers/IntegrationMappingSetsController.cs:25` |
| `GET` | `/api/integrations/mappingsets/{id:guid}` | `src/Cfa.ACHInterbank.Api/Controllers/IntegrationMappingSetsController.cs:33` |
| `POST` | `/api/integrations/mappingsets` | `src/Cfa.ACHInterbank.Api/Controllers/IntegrationMappingSetsController.cs:41` |
| `PUT` | `/api/integrations/mappingsets/{id:guid}` | `src/Cfa.ACHInterbank.Api/Controllers/IntegrationMappingSetsController.cs:49` |
| `PUT` | `/api/integrations/mappingsets/{id:guid}/rules` | `src/Cfa.ACHInterbank.Api/Controllers/IntegrationMappingSetsController.cs:54` |
| `POST` | `/api/integrations/mappingsets/{id:guid}/validate` | `src/Cfa.ACHInterbank.Api/Controllers/IntegrationMappingSetsController.cs:59` |
| `POST` | `/api/integrations/mappingsets/{id:guid}/preview` | `src/Cfa.ACHInterbank.Api/Controllers/IntegrationMappingSetsController.cs:64` |
| `POST` | `/api/integrations/mappingsets/{id:guid}/publish` | `src/Cfa.ACHInterbank.Api/Controllers/IntegrationMappingSetsController.cs:69` |
| `POST` | `/api/integrations/mappingsets/{id:guid}/clone` | `src/Cfa.ACHInterbank.Api/Controllers/IntegrationMappingSetsController.cs:74` |
| `GET` | `/api/integrations/mappingsets/{id:guid}/history` | `src/Cfa.ACHInterbank.Api/Controllers/IntegrationMappingSetsController.cs:79` |
| `POST` | `/api/integrations/mappingsets/compare` | `src/Cfa.ACHInterbank.Api/Controllers/IntegrationMappingSetsController.cs:84` |

### JwksController  
Base route: `/oauth2`  
Operaciones: 4

| Método | Ruta final | Fuente |
|---|---|---|
| `GET` | `/oauth2/jwks` | `src/Cfa.ACHInterbank.Api/Controllers/JwksController.cs:20` |
| `GET` | `/oauth2/TokenClientAssertions` | `src/Cfa.ACHInterbank.Api/Controllers/JwksController.cs:30` |
| `POST` | `/oauth2/client-assertion` | `src/Cfa.ACHInterbank.Api/Controllers/JwksController.cs:40` |
| `POST` | `/oauth2/Genearte-client-assertion` | `src/Cfa.ACHInterbank.Api/Controllers/JwksController.cs:54` |

### LoginLockoutSettingsController  
Base route: `/api/users/login-lockout`  
Operaciones: 2

| Método | Ruta final | Fuente |
|---|---|---|
| `GET` | `/api/users/login-lockout` | `src/Cfa.ACHInterbank.Api/Controllers/LoginLockoutSettingsController.cs:23` |
| `PUT` | `/api/users/login-lockout` | `src/Cfa.ACHInterbank.Api/Controllers/LoginLockoutSettingsController.cs:33` |

### MaintenanceController  
Base route: `/Maintenance`  
Operaciones: 1

| Método | Ruta final | Fuente |
|---|---|---|
| `POST` | `/Maintenance/seed` | `src/Cfa.ACHInterbank.Api/Controllers/MaintenanceController.cs:23` |

### MenuItemsController  
Base route: `/navigation/menu-items`  
Operaciones: 4

| Método | Ruta final | Fuente |
|---|---|---|
| `GET` | `/navigation/menu-items` | `src/Cfa.ACHInterbank.Api/Controllers/MenuItemsController.cs:23` |
| `POST` | `/navigation/menu-items` | `src/Cfa.ACHInterbank.Api/Controllers/MenuItemsController.cs:33` |
| `PUT` | `/navigation/menu-items/{id:int}` | `src/Cfa.ACHInterbank.Api/Controllers/MenuItemsController.cs:50` |
| `DELETE` | `/navigation/menu-items/{id:int}` | `src/Cfa.ACHInterbank.Api/Controllers/MenuItemsController.cs:72` |

### NachaConfigProfilesController  
Base route: `/nacha-config`  
Operaciones: 17

| Método | Ruta final | Fuente |
|---|---|---|
| `GET` | `/nacha-config/perfiles` | `src/Cfa.ACHInterbank.Api/Controllers/NachaConfigProfilesController.cs:36` |
| `GET` | `/nacha-config/catalogos-filtro` | `src/Cfa.ACHInterbank.Api/Controllers/NachaConfigProfilesController.cs:41` |
| `GET` | `/nacha-config/perfiles/{id:int}` | `src/Cfa.ACHInterbank.Api/Controllers/NachaConfigProfilesController.cs:46` |
| `POST` | `/nacha-config/perfiles` | `src/Cfa.ACHInterbank.Api/Controllers/NachaConfigProfilesController.cs:54` |
| `PUT` | `/nacha-config/perfiles/{id:int}` | `src/Cfa.ACHInterbank.Api/Controllers/NachaConfigProfilesController.cs:66` |
| `POST` | `/nacha-config/perfiles/{id:int}/clonar` | `src/Cfa.ACHInterbank.Api/Controllers/NachaConfigProfilesController.cs:78` |
| `POST` | `/nacha-config/perfiles/{id:int}/validar` | `src/Cfa.ACHInterbank.Api/Controllers/NachaConfigProfilesController.cs:90` |
| `POST` | `/nacha-config/perfiles/{id:int}/publicar` | `src/Cfa.ACHInterbank.Api/Controllers/NachaConfigProfilesController.cs:95` |
| `POST` | `/nacha-config/perfiles/{id:int}/inactivar` | `src/Cfa.ACHInterbank.Api/Controllers/NachaConfigProfilesController.cs:116` |
| `POST` | `/nacha-config/perfiles/{id:int}/archivar` | `src/Cfa.ACHInterbank.Api/Controllers/NachaConfigProfilesController.cs:127` |
| `GET` | `/nacha-config/perfiles/{id:int}/historial` | `src/Cfa.ACHInterbank.Api/Controllers/NachaConfigProfilesController.cs:138` |
| `GET` | `/nacha-config/perfiles/{id:int}/snapshots` | `src/Cfa.ACHInterbank.Api/Controllers/NachaConfigProfilesController.cs:143` |
| `PUT` | `/nacha-config/perfiles/{id:int}/records/secuencia` | `src/Cfa.ACHInterbank.Api/Controllers/NachaConfigProfilesController.cs:148` |
| `PUT` | `/nacha-config/perfiles/{id:int}/variantes/{variantId:int}` | `src/Cfa.ACHInterbank.Api/Controllers/NachaConfigProfilesController.cs:159` |
| `PUT` | `/nacha-config/perfiles/{id:int}/fields/{fieldId:int}` | `src/Cfa.ACHInterbank.Api/Controllers/NachaConfigProfilesController.cs:170` |
| `PUT` | `/nacha-config/perfiles/{id:int}/rules/{ruleId:int}` | `src/Cfa.ACHInterbank.Api/Controllers/NachaConfigProfilesController.cs:181` |
| `POST` | `/nacha-config/resolver-preview` | `src/Cfa.ACHInterbank.Api/Controllers/NachaConfigProfilesController.cs:192` |

### NachaController  
Base route: `/Nacha`  
Operaciones: 1

| Método | Ruta final | Fuente |
|---|---|---|
| `POST` | `/Nacha/header` | `src/Cfa.ACHInterbank.Api/Controllers/NachaController.cs:23` |

### NachaExportController  
Base route: `/NachaExport`  
Operaciones: 2

| Método | Ruta final | Fuente |
|---|---|---|
| `GET` | `/NachaExport/{cycleId}` | `src/Cfa.ACHInterbank.Api/Controllers/NachaExportController.cs:50` |
| `GET` | `/NachaExport/{cycleId}/sobre-digital` | `src/Cfa.ACHInterbank.Api/Controllers/NachaExportController.cs:99` |

### NachaRecordDefinitionsController  
Base route: `/nacha-record-definitions`  
Operaciones: 5

| Método | Ruta final | Fuente |
|---|---|---|
| `GET` | `/nacha-record-definitions` | `src/Cfa.ACHInterbank.Api/Controllers/NachaRecordDefinitionsController.cs:23` |
| `GET` | `/nacha-record-definitions/{id:int}` | `src/Cfa.ACHInterbank.Api/Controllers/NachaRecordDefinitionsController.cs:34` |
| `POST` | `/nacha-record-definitions` | `src/Cfa.ACHInterbank.Api/Controllers/NachaRecordDefinitionsController.cs:45` |
| `PUT` | `/nacha-record-definitions/{id:int}` | `src/Cfa.ACHInterbank.Api/Controllers/NachaRecordDefinitionsController.cs:56` |
| `DELETE` | `/nacha-record-definitions/{id:int}` | `src/Cfa.ACHInterbank.Api/Controllers/NachaRecordDefinitionsController.cs:72` |

### NachaRecordLayoutsController  
Base route: `/nacha-layouts`  
Operaciones: 5

| Método | Ruta final | Fuente |
|---|---|---|
| `GET` | `/nacha-layouts` | `src/Cfa.ACHInterbank.Api/Controllers/NachaRecordLayoutsController.cs:23` |
| `GET` | `/nacha-layouts/{id:int}` | `src/Cfa.ACHInterbank.Api/Controllers/NachaRecordLayoutsController.cs:34` |
| `POST` | `/nacha-layouts` | `src/Cfa.ACHInterbank.Api/Controllers/NachaRecordLayoutsController.cs:45` |
| `PUT` | `/nacha-layouts/{id:int}` | `src/Cfa.ACHInterbank.Api/Controllers/NachaRecordLayoutsController.cs:56` |
| `DELETE` | `/nacha-layouts/{id:int}` | `src/Cfa.ACHInterbank.Api/Controllers/NachaRecordLayoutsController.cs:72` |

### NachaSecurityOperationsController  
Base route: `/nacha-security/operations`  
Operaciones: 8

| Método | Ruta final | Fuente |
|---|---|---|
| `POST` | `/nacha-security/operations/nacha/generate` | `src/Cfa.ACHInterbank.Api/Controllers/NachaSecurityOperationsController.cs:21` |
| `POST` | `/nacha-security/operations/nacha/generate-encrypted` | `src/Cfa.ACHInterbank.Api/Controllers/NachaSecurityOperationsController.cs:33` |
| `POST` | `/nacha-security/operations/envelope/manual-encrypt` | `src/Cfa.ACHInterbank.Api/Controllers/NachaSecurityOperationsController.cs:45` |
| `POST` | `/nacha-security/operations/envelope/manual-decrypt` | `src/Cfa.ACHInterbank.Api/Controllers/NachaSecurityOperationsController.cs:66` |
| `GET` | `/nacha-security/operations/{operationId}` | `src/Cfa.ACHInterbank.Api/Controllers/NachaSecurityOperationsController.cs:87` |
| `GET` | `/nacha-security/operations/audit` | `src/Cfa.ACHInterbank.Api/Controllers/NachaSecurityOperationsController.cs:100` |
| `POST` | `/nacha-security/operations/{operationId}/authorize-download` | `src/Cfa.ACHInterbank.Api/Controllers/NachaSecurityOperationsController.cs:107` |
| `GET` | `/nacha-security/operations/{operationId}/download` | `src/Cfa.ACHInterbank.Api/Controllers/NachaSecurityOperationsController.cs:138` |

### NachaUploadController  
Base route: `/NachaUpload`  
Operaciones: 2

| Método | Ruta final | Fuente |
|---|---|---|
| `POST` | `/NachaUpload/upload` | `src/Cfa.ACHInterbank.Api/Controllers/NachaUploadController.cs:43` |
| `GET` | `/NachaUpload/records` | `src/Cfa.ACHInterbank.Api/Controllers/NachaUploadController.cs:186` |

### NavigationController  
Base route: `/navigation`  
Operaciones: 1

| Método | Ruta final | Fuente |
|---|---|---|
| `GET` | `/navigation/menu` | `src/Cfa.ACHInterbank.Api/Controllers/NavigationController.cs:24` |

### NavigationLogsController  
Base route: `/api/navigation-logs`  
Operaciones: 2

| Método | Ruta final | Fuente |
|---|---|---|
| `GET` | `/api/navigation-logs` | `src/Cfa.ACHInterbank.Api/Controllers/NavigationLogsController.cs:24` |
| `POST` | `/api/navigation-logs` | `src/Cfa.ACHInterbank.Api/Controllers/NavigationLogsController.cs:48` |

### OauthsController  
Base route: `/Oauths`  
Operaciones: 2

| Método | Ruta final | Fuente |
|---|---|---|
| `POST` | `/Oauths/GenerateToken` | `src/Cfa.ACHInterbank.Api/Controllers/OauthsController.cs:21` |
| `POST` | `/Oauths/GenerateTokenAsync` | `src/Cfa.ACHInterbank.Api/Controllers/OauthsController.cs:37` |

### PasswordRulesController  
Base route: `/api/users/password-rules`  
Operaciones: 2

| Método | Ruta final | Fuente |
|---|---|---|
| `GET` | `/api/users/password-rules` | `src/Cfa.ACHInterbank.Api/Controllers/PasswordRulesController.cs:23` |
| `PUT` | `/api/users/password-rules` | `src/Cfa.ACHInterbank.Api/Controllers/PasswordRulesController.cs:33` |

### PaymentRailCapabilityRegistryController  
Base route: `/api/payment-rails/capability-registry`  
Operaciones: 3

| Método | Ruta final | Fuente |
|---|---|---|
| `GET` | `/api/payment-rails/capability-registry/rails` | `src/Cfa.ACHInterbank.Api/Controllers/PaymentRailCapabilityRegistryController.cs:21` |
| `GET` | `/api/payment-rails/capability-registry/rails/{railCode}/capabilities` | `src/Cfa.ACHInterbank.Api/Controllers/PaymentRailCapabilityRegistryController.cs:26` |
| `GET` | `/api/payment-rails/capability-registry/rails/{railCode}/capabilities/{capabilityCode}` | `src/Cfa.ACHInterbank.Api/Controllers/PaymentRailCapabilityRegistryController.cs:44` |

### PermissionsController  
Base route: `/api/Permissions`  
Operaciones: 1

| Método | Ruta final | Fuente |
|---|---|---|
| `GET` | `/api/Permissions` | `src/Cfa.ACHInterbank.Api/Controllers/PermissionsController.cs:23` |

### RegulatoryCatalogsController  
Base route: `/api/regulatory-catalogs`  
Operaciones: 6

| Método | Ruta final | Fuente |
|---|---|---|
| `GET` | `/api/regulatory-catalogs/return-codes` | `src/Cfa.ACHInterbank.Api/Controllers/RegulatoryCatalogsController.cs:17` |
| `GET` | `/api/regulatory-catalogs/file-rejection-codes` | `src/Cfa.ACHInterbank.Api/Controllers/RegulatoryCatalogsController.cs:21` |
| `GET` | `/api/regulatory-catalogs/transaction-type-policies` | `src/Cfa.ACHInterbank.Api/Controllers/RegulatoryCatalogsController.cs:25` |
| `GET` | `/api/regulatory-catalogs/return-policies` | `src/Cfa.ACHInterbank.Api/Controllers/RegulatoryCatalogsController.cs:29` |
| `GET` | `/api/regulatory-catalogs/return-of-return-policies` | `src/Cfa.ACHInterbank.Api/Controllers/RegulatoryCatalogsController.cs:33` |
| `GET` | `/api/regulatory-catalogs/prenotification-policies` | `src/Cfa.ACHInterbank.Api/Controllers/RegulatoryCatalogsController.cs:37` |

### ReportsController  
Base route: `/api/reports`  
Operaciones: 19

| Método | Ruta final | Fuente |
|---|---|---|
| `GET` | `/api/reports/transactions/sent` | `src/Cfa.ACHInterbank.Api/Controllers/ReportsController.cs:48` |
| `GET` | `/api/reports/transactions/received` | `src/Cfa.ACHInterbank.Api/Controllers/ReportsController.cs:83` |
| `GET` | `/api/reports/transactions/sent/pdf` | `src/Cfa.ACHInterbank.Api/Controllers/ReportsController.cs:118` |
| `GET` | `/api/reports/transactions/received/pdf` | `src/Cfa.ACHInterbank.Api/Controllers/ReportsController.cs:151` |
| `GET` | `/api/reports/returns` | `src/Cfa.ACHInterbank.Api/Controllers/ReportsController.cs:185` |
| `GET` | `/api/reports/rejections` | `src/Cfa.ACHInterbank.Api/Controllers/ReportsController.cs:216` |
| `GET` | `/api/reports/returns/pdf` | `src/Cfa.ACHInterbank.Api/Controllers/ReportsController.cs:247` |
| `GET` | `/api/reports/rejections/pdf` | `src/Cfa.ACHInterbank.Api/Controllers/ReportsController.cs:276` |
| `GET` | `/api/reports/nacha-files` | `src/Cfa.ACHInterbank.Api/Controllers/ReportsController.cs:306` |
| `GET` | `/api/reports/nacha-files/pdf` | `src/Cfa.ACHInterbank.Api/Controllers/ReportsController.cs:331` |
| `GET` | `/api/reports/cycles` | `src/Cfa.ACHInterbank.Api/Controllers/ReportsController.cs:354` |
| `GET` | `/api/reports/cycles/pdf` | `src/Cfa.ACHInterbank.Api/Controllers/ReportsController.cs:381` |
| `GET` | `/api/reports/reconciliation` | `src/Cfa.ACHInterbank.Api/Controllers/ReportsController.cs:407` |
| `GET` | `/api/reports/reconciliation/pdf` | `src/Cfa.ACHInterbank.Api/Controllers/ReportsController.cs:430` |
| `GET` | `/api/reports/audit` | `src/Cfa.ACHInterbank.Api/Controllers/ReportsController.cs:454` |
| `GET` | `/api/reports/audit/pdf` | `src/Cfa.ACHInterbank.Api/Controllers/ReportsController.cs:482` |
| `GET` | `/api/reports/history` | `src/Cfa.ACHInterbank.Api/Controllers/ReportsController.cs:508` |
| `GET` | `/api/reports/history/pdf` | `src/Cfa.ACHInterbank.Api/Controllers/ReportsController.cs:536` |
| `GET` | `/api/reports/traceability/pdf` | `src/Cfa.ACHInterbank.Api/Controllers/ReportsController.cs:562` |

### ReturnReasonsController  
Base route: `/return-reasons`  
Operaciones: 1

| Método | Ruta final | Fuente |
|---|---|---|
| `GET` | `/return-reasons` | `src/Cfa.ACHInterbank.Api/Controllers/ReturnReasonsController.cs:22` |

### RolesController  
Base route: `/api/Roles`  
Operaciones: 1

| Método | Ruta final | Fuente |
|---|---|---|
| `GET` | `/api/Roles` | `src/Cfa.ACHInterbank.Api/Controllers/RolesController.cs:23` |

### ServersController  
Base route: `/Servers`  
Operaciones: 1

| Método | Ruta final | Fuente |
|---|---|---|
| `GET` | `/Servers` | `src/Cfa.ACHInterbank.Api/Controllers/ServersController.cs:19` |

### SoapIntegrationSettingsController  
Base route: `/api/users/soap-integrations`  
Operaciones: 2

| Método | Ruta final | Fuente |
|---|---|---|
| `GET` | `/api/users/soap-integrations` | `src/Cfa.ACHInterbank.Api/Controllers/SoapIntegrationSettingsController.cs:20` |
| `PUT` | `/api/users/soap-integrations` | `src/Cfa.ACHInterbank.Api/Controllers/SoapIntegrationSettingsController.cs:27` |

### SobreDigitalController  
Base route: `/SobreDigital`  
Operaciones: 3

| Método | Ruta final | Fuente |
|---|---|---|
| `POST` | `/SobreDigital/encrypt` | `src/Cfa.ACHInterbank.Api/Controllers/SobreDigitalController.cs:33` |
| `POST` | `/SobreDigital/decrypt` | `src/Cfa.ACHInterbank.Api/Controllers/SobreDigitalController.cs:62` |
| `POST` | `/SobreDigital/testRSA` | `src/Cfa.ACHInterbank.Api/Controllers/SobreDigitalController.cs:94` |

### TaskDefinitionsController  
Base route: `/TaskDefinitions`  
Operaciones: 5

| Método | Ruta final | Fuente |
|---|---|---|
| `GET` | `/TaskDefinitions` | `src/Cfa.ACHInterbank.Api/Controllers/TaskDefinitionsController.cs:23` |
| `GET` | `/TaskDefinitions/{id}` | `src/Cfa.ACHInterbank.Api/Controllers/TaskDefinitionsController.cs:34` |
| `POST` | `/TaskDefinitions` | `src/Cfa.ACHInterbank.Api/Controllers/TaskDefinitionsController.cs:46` |
| `PUT` | `/TaskDefinitions/{id}` | `src/Cfa.ACHInterbank.Api/Controllers/TaskDefinitionsController.cs:57` |
| `DELETE` | `/TaskDefinitions/{id}` | `src/Cfa.ACHInterbank.Api/Controllers/TaskDefinitionsController.cs:69` |

### TestsController  
Base route: `/Tests`  
Operaciones: 2

| Método | Ruta final | Fuente |
|---|---|---|
| `GET` | `/Tests` | `src/Cfa.ACHInterbank.Api/Controllers/TestsController.cs:22` |
| `GET` | `/Tests/Prueba` | `src/Cfa.ACHInterbank.Api/Controllers/TestsController.cs:50` |

### TransactionsController  
Base route: `/Transactions`  
Operaciones: 7

| Método | Ruta final | Fuente |
|---|---|---|
| `GET` | `/Transactions` | `src/Cfa.ACHInterbank.Api/Controllers/TransactionsController.cs:37` |
| `GET` | `/Transactions/company-entry-descriptions` | `src/Cfa.ACHInterbank.Api/Controllers/TransactionsController.cs:54` |
| `GET` | `/Transactions/policies/preview` | `src/Cfa.ACHInterbank.Api/Controllers/TransactionsController.cs:65` |
| `POST` | `/Transactions` | `src/Cfa.ACHInterbank.Api/Controllers/TransactionsController.cs:97` |
| `POST` | `/Transactions/bulk/submit` | `src/Cfa.ACHInterbank.Api/Controllers/TransactionsController.cs:164` |
| `POST` | `/Transactions/bulk` | `src/Cfa.ACHInterbank.Api/Controllers/TransactionsController.cs:192` |
| `GET` | `/Transactions/{id:int}` | `src/Cfa.ACHInterbank.Api/Controllers/TransactionsController.cs:219` |

### UsersController  
Base route: `/api/Users`  
Operaciones: 7

| Método | Ruta final | Fuente |
|---|---|---|
| `GET` | `/api/Users` | `src/Cfa.ACHInterbank.Api/Controllers/UsersController.cs:25` |
| `GET` | `/api/Users/validate-email-domain` | `src/Cfa.ACHInterbank.Api/Controllers/UsersController.cs:47` |
| `GET` | `/api/Users/{id:guid}` | `src/Cfa.ACHInterbank.Api/Controllers/UsersController.cs:59` |
| `POST` | `/api/Users` | `src/Cfa.ACHInterbank.Api/Controllers/UsersController.cs:76` |
| `PUT` | `/api/Users/{id:guid}` | `src/Cfa.ACHInterbank.Api/Controllers/UsersController.cs:94` |
| `POST` | `/api/Users/{id:guid}/roles` | `src/Cfa.ACHInterbank.Api/Controllers/UsersController.cs:113` |
| `DELETE` | `/api/Users/{id:guid}` | `src/Cfa.ACHInterbank.Api/Controllers/UsersController.cs:131` |

## 4) Observaciones técnicas para documentación profesional (siguiente fase)

- Normalizar rutas históricas sin prefijo (`/customers`, `/ach-cycles`, `/nacha-config`, etc.) vs rutas con `/api/...` para coherencia de versionado.
- Estandarizar casing y semántica en algunos endpoints legacy (`/Oauths/GenerateToken`, typo `Genearte-client-assertion`).
- Definir tags OpenAPI por bounded context (Auth, Seguridad NACHA, CENIT, Catálogos, Reportes, Operación ACH).
- Agregar esquemas de respuesta de error homogéneos (`ProblemDetails` o contrato estándar) y ejemplos por endpoint.
- Documentar explícitamente endpoints operativos/sensibles (descarga de artefactos, certificados, mantenimiento/seed, pruebas).
- Incluir matriz de autenticación/autorización por endpoint (anonymous/authenticated/roles/policies) para traspaso de equipo.
