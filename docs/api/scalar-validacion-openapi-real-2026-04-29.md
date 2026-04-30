# Validación real del OpenAPI generado y matriz completa — Scalar-2B
**Fecha (UTC):** 2026-04-29  
**Fuente validada:** `http://127.0.0.1:5194/openapi/v1.json` generado por la API en ejecución con `Database__ApplyMigrations=false`.

## Resultado ejecutivo
- Operaciones REST encontradas en OpenAPI real: **213**.
- Operaciones REST esperadas en inventario previo: **220**.
- Brecha detectada frente al inventario previo: **7** operaciones.
- Operaciones con `summary` presente: **213/213**.
- Operaciones con `description` presente: **213/213**.
- Operaciones con documentación explícita (`EndpointSummary` + `EndpointDescription`): **61/213**.
- Operaciones que dependen del fallback del transformer: **152/213**.
- Operaciones con descripción potencialmente genérica a revisar: **0**.
- Operaciones sin permiso evidenciado por atributo cercano en código fuente: **24**.

## Veredicto de validación
- **No procede declarar 220/220** con evidencia del OpenAPI real actual.
- La cobertura real de OpenAPI validada es **213 operaciones** con `summary` y `description` completas, pero el universo real publicado no coincide con 220.

## Matriz completa 213/213 operaciones publicadas
| # | Controller | Método | Ruta | Summary | Description | Origen documentación | Permiso evidenciado | Riesgo de genericidad |
|---:|---|---|---|---|---|---|---|---|
| 1 | AchCyclesController | GET | `/ach-cycles` | Sí | Sí | Fallback | Authorize | No |
| 2 | AchCyclesController | POST | `/ach-cycles` | Sí | Sí | Fallback | Policy = "CanReadAch" | No |
| 3 | AchCyclesController | GET | `/ach-cycles/exportable` | Sí | Sí | Fallback | Policy = "CanManageAch" | No |
| 4 | AchCyclesController | DELETE | `/ach-cycles/{id}` | Sí | Sí | Fallback | Policy = "CanManageAch" | No |
| 5 | AchCyclesController | GET | `/ach-cycles/{id}` | Sí | Sí | Fallback | Policy = "CanReadAch" | No |
| 6 | AchCyclesController | PUT | `/ach-cycles/{id}` | Sí | Sí | Fallback | Policy = "CanManageAch" | No |
| 7 | AchReturnsController | GET | `/ach-returns/cycles/{cycleId}/transactions` | Sí | Sí | Explícita | Sin atributo [Authorize] cercano | No |
| 8 | AchReturnsController | POST | `/ach-returns/generate-file` | Sí | Sí | Explícita | Sin atributo [Authorize] cercano | No |
| 9 | AchTraceabilityController | GET | `/api/ach-traceability/report` | Sí | Sí | Explícita | Sin atributo [Authorize] cercano | No |
| 10 | AchTraceabilityController | POST | `/api/ach-traceability/sol02/{transactionId}/certify` | Sí | Sí | Explícita | Sin atributo [Authorize] cercano | No |
| 11 | AchTraceabilityController | GET | `/api/ach-traceability/transactions/{transactionId}` | Sí | Sí | Explícita | Sin atributo [Authorize] cercano | No |
| 12 | AuditLogsController | GET | `/api/audit-logs` | Sí | Sí | Fallback | Authorize | No |
| 13 | AuthController | POST | `/Auth/forgot-password` | Sí | Sí | Fallback | Authorize | No |
| 14 | AuthController | POST | `/Auth/login` | Sí | Sí | Fallback | Authorize | No |
| 15 | AuthController | POST | `/Auth/refresh` | Sí | Sí | Fallback | AllowAnonymous | No |
| 16 | AuthController | POST | `/Auth/reset-password` | Sí | Sí | Fallback | AllowAnonymous | No |
| 17 | AuthLogsController | GET | `/api/auth-logs` | Sí | Sí | Fallback | Authorize | No |
| 18 | BankHolidaysController | GET | `/bank-holidays` | Sí | Sí | Fallback | Authorize | No |
| 19 | BankHolidaysController | POST | `/bank-holidays` | Sí | Sí | Fallback | Policy = "CanManageAch" | No |
| 20 | BankHolidaysController | DELETE | `/bank-holidays/{id}` | Sí | Sí | Fallback | Policy = "CanManageAch" | No |
| 21 | BankHolidaysController | PUT | `/bank-holidays/{id}` | Sí | Sí | Fallback | Policy = "CanManageAch" | No |
| 22 | BrandingController | GET | `/api/users/branding` | Sí | Sí | Fallback | Sin atributo [Authorize] cercano | No |
| 23 | BrandingController | PUT | `/api/users/branding` | Sí | Sí | Fallback | AllowAnonymous | No |
| 24 | BulkIngestionController | POST | `/api/transactions/bulk-ingestion/upload` | Sí | Sí | Fallback | Policy = "CanManageAch" | No |
| 25 | BulkIngestionController | GET | `/api/transactions/bulk-ingestion/{batchId}` | Sí | Sí | Fallback | Policy = "CanManageAch" | No |
| 26 | BulkIngestionController | POST | `/api/transactions/bulk-ingestion/{batchId}/cancel` | Sí | Sí | Fallback | Policy = "CanManageAch" | No |
| 27 | BulkIngestionController | GET | `/api/transactions/bulk-ingestion/{batchId}/items` | Sí | Sí | Fallback | Policy = "CanManageAch" | No |
| 28 | BulkIngestionController | POST | `/api/transactions/bulk-ingestion/{batchId}/retry` | Sí | Sí | Fallback | Policy = "CanManageAch" | No |
| 29 | BulkIngestionController | GET | `/api/transactions/bulk-ingestion/{batchId}/summary` | Sí | Sí | Fallback | Policy = "CanManageAch" | No |
| 30 | CatalogTypesController | GET | `/catalog-types/{catalogType}` | Sí | Sí | Fallback | Authorize | No |
| 31 | CatalogTypesController | POST | `/catalog-types/{catalogType}` | Sí | Sí | Fallback | Policy = "CanManageAch" | No |
| 32 | CatalogTypesController | DELETE | `/catalog-types/{catalogType}/{code}` | Sí | Sí | Fallback | Policy = "CanManageAch" | No |
| 33 | CatalogTypesController | PUT | `/catalog-types/{catalogType}/{code}` | Sí | Sí | Fallback | Policy = "CanManageAch" | No |
| 34 | CenitOperationsController | GET | `/api/cenit/net-positions` | Sí | Sí | Explícita | Policy = "CanReadAch" | No |
| 35 | CenitOperationsController | GET | `/api/cenit/optimization-decisions` | Sí | Sí | Explícita | Policy = "CanReadAch" | No |
| 36 | CenitOperationsController | GET | `/api/cenit/queues` | Sí | Sí | Explícita | Authorize | No |
| 37 | CenitOperationsController | GET | `/api/cenit/traceability` | Sí | Sí | Explícita | Policy = "CanReadAch" | No |
| 38 | CertificateManagementController | GET | `/nacha-security/certificates/management` | Sí | Sí | Explícita | Policy = FineGrainedPermissions.CanManageCertificates | No |
| 39 | CertificateManagementController | GET | `/nacha-security/certificates/management/audit` | Sí | Sí | Explícita | Policy = FineGrainedPermissions.CanManageCertificates | No |
| 40 | CertificateManagementController | POST | `/nacha-security/certificates/management/private` | Sí | Sí | Explícita | Policy = FineGrainedPermissions.CanManageCertificates | No |
| 41 | CertificateManagementController | POST | `/nacha-security/certificates/management/public` | Sí | Sí | Explícita | Policy = FineGrainedPermissions.CanManageCertificates | No |
| 42 | CertificateManagementController | POST | `/nacha-security/certificates/management/versions/{id}/activate` | Sí | Sí | Explícita | Policy = "CanReadAch" | No |
| 43 | CertificateManagementController | POST | `/nacha-security/certificates/management/versions/{id}/revoke` | Sí | Sí | Explícita | Policy = FineGrainedPermissions.CanManageCertificates | No |
| 44 | CertificateManagementController | POST | `/nacha-security/certificates/management/versions/{id}/validate` | Sí | Sí | Explícita | Policy = FineGrainedPermissions.CanManageCertificates | No |
| 45 | CertificateManagementController | GET | `/nacha-security/certificates/management/{id}/versions` | Sí | Sí | Explícita | Policy = "CanReadAch" | No |
| 46 | ClearingHouseCycleConfigsController | GET | `/clearing-house-cycle-configs` | Sí | Sí | Fallback | Authorize | No |
| 47 | ClearingHouseCycleConfigsController | POST | `/clearing-house-cycle-configs` | Sí | Sí | Fallback | Policy = "CanManageAch" | No |
| 48 | ClearingHouseCycleConfigsController | GET | `/clearing-house-cycle-configs/current` | Sí | Sí | Fallback | Policy = "CanManageAch" | No |
| 49 | ClearingHouseCycleConfigsController | POST | `/clearing-house-cycle-configs/{id}/inactivate` | Sí | Sí | Fallback | Policy = "CanManageAch" | No |
| 50 | ClearingHouseSpecialDatesController | GET | `/clearing-house-special-dates` | Sí | Sí | Fallback | Authorize | No |
| 51 | ClearingHouseSpecialDatesController | POST | `/clearing-house-special-dates` | Sí | Sí | Fallback | Policy = "CanManageAch" | No |
| 52 | ClearingHouseSpecialDatesController | DELETE | `/clearing-house-special-dates/{id}` | Sí | Sí | Fallback | Policy = "CanManageAch" | No |
| 53 | ClearingHouseSpecialDatesController | PUT | `/clearing-house-special-dates/{id}` | Sí | Sí | Fallback | Policy = "CanManageAch" | No |
| 54 | ClearingHousesController | GET | `/clearing-houses` | Sí | Sí | Fallback | Policy = "CanReadAch" | No |
| 55 | ClearingHousesController | GET | `/clearing-houses/{id}` | Sí | Sí | Fallback | Policy = "CanReadAch" | No |
| 56 | ClearingHousesController | GET | `/clearing-houses/{id}/cycles` | Sí | Sí | Fallback | Policy = "CanReadAch" | No |
| 57 | CompanyEntryDescriptionsController | GET | `/company-entry-descriptions` | Sí | Sí | Fallback | Authorize | No |
| 58 | CompanyEntryDescriptionsController | POST | `/company-entry-descriptions` | Sí | Sí | Fallback | Policy = "CanManageAch" | No |
| 59 | CompanyEntryDescriptionsController | DELETE | `/company-entry-descriptions/{id}` | Sí | Sí | Fallback | Policy = "CanManageAch" | No |
| 60 | CompanyEntryDescriptionsController | PUT | `/company-entry-descriptions/{id}` | Sí | Sí | Fallback | Policy = "CanManageAch" | No |
| 61 | CustomerThirdPartiesController | GET | `/api/customer-third-parties` | Sí | Sí | Fallback | Authorize | No |
| 62 | CustomerThirdPartiesController | PATCH | `/api/customer-third-parties/{id}/status` | Sí | Sí | Fallback | Policy = "CanManageAch" | No |
| 63 | CustomersController | GET | `/customers` | Sí | Sí | Fallback | Authorize | No |
| 64 | CustomersController | POST | `/customers` | Sí | Sí | Fallback | Policy = "CanManageAch" | No |
| 65 | CustomersController | DELETE | `/customers/{id}` | Sí | Sí | Fallback | Policy = "CanManageAch" | No |
| 66 | CustomersController | GET | `/customers/{id}` | Sí | Sí | Fallback | Policy = "CanReadAch" | No |
| 67 | CustomersController | PUT | `/customers/{id}` | Sí | Sí | Fallback | Policy = "CanManageAch" | No |
| 68 | DigitalEnvelopeCertificatesController | GET | `/nacha-security/certificates` | Sí | Sí | Explícita | Authorize | No |
| 69 | DigitalEnvelopeCertificatesController | POST | `/nacha-security/certificates` | Sí | Sí | Explícita | Authorize | No |
| 70 | DigitalEnvelopeCertificatesController | DELETE | `/nacha-security/certificates/{id}` | Sí | Sí | Explícita | Authorize | No |
| 71 | FinancialInstitutionsController | GET | `/financial-institutions` | Sí | Sí | Fallback | Authorize | No |
| 72 | FinancialInstitutionsController | POST | `/financial-institutions` | Sí | Sí | Fallback | Policy = "CanReadAch" | No |
| 73 | FinancialInstitutionsController | GET | `/financial-institutions/{id}` | Sí | Sí | Fallback | Policy = "CanReadAch" | No |
| 74 | FinancialInstitutionsController | PUT | `/financial-institutions/{id}` | Sí | Sí | Fallback | Policy = "CanManageAch" | No |
| 75 | FinancialInstitutionsController | PATCH | `/financial-institutions/{id}/status` | Sí | Sí | Fallback | Policy = "CanManageAch" | No |
| 76 | IncomingNachaCommandCenterController | GET | `/incoming-nacha-command-center/ingestions` | Sí | Sí | Explícita | Policy = "CanManageAch" | No |
| 77 | IncomingNachaCommandCenterController | GET | `/incoming-nacha-command-center/ingestions/{ingestionId}` | Sí | Sí | Explícita | Policy = "CanManageAch" | No |
| 78 | IncomingNachaCommandCenterController | GET | `/incoming-nacha-command-center/observability/summary` | Sí | Sí | Explícita | Policy = "CanReadAch" | No |
| 79 | IncomingNachaCommandCenterController | GET | `/incoming-nacha-command-center/queue` | Sí | Sí | Explícita | Policy = "CanManageAch" | No |
| 80 | IncomingNachaCommandCenterController | GET | `/incoming-nacha-command-center/queue/{queueId}` | Sí | Sí | Explícita | Policy = "CanManageAch" | No |
| 81 | IncomingNachaCommandCenterController | POST | `/incoming-nacha-command-center/queue/{queueId}/mark-failed-final` | Sí | Sí | Explícita | Policy = "CanManageAch" | No |
| 82 | IncomingNachaCommandCenterController | POST | `/incoming-nacha-command-center/queue/{queueId}/requeue` | Sí | Sí | Explícita | Policy = "CanManageAch" | No |
| 83 | IncomingNachaCommandCenterController | POST | `/incoming-nacha-command-center/queue/{queueId}/retry` | Sí | Sí | Explícita | Policy = "CanManageAch" | No |
| 84 | IncomingNachaCommandCenterController | POST | `/incoming-nacha-command-center/queue/{queueId}/unblock` | Sí | Sí | Explícita | Policy = "CanManageAch" | No |
| 85 | InstitutionClearingHousePreferencesController | GET | `/institution-clearing-house-preferences` | Sí | Sí | Fallback | Authorize | No |
| 86 | InstitutionClearingHousePreferencesController | POST | `/institution-clearing-house-preferences` | Sí | Sí | Fallback | Policy = "CanManageAch" | No |
| 87 | InstitutionClearingHousePreferencesController | DELETE | `/institution-clearing-house-preferences/{id}` | Sí | Sí | Fallback | Policy = "CanManageAch" | No |
| 88 | InstitutionClearingHousePreferencesController | PUT | `/institution-clearing-house-preferences/{id}` | Sí | Sí | Fallback | Policy = "CanManageAch" | No |
| 89 | IntegrationCatalogController | GET | `/api/integrations/methods` | Sí | Sí | Fallback | Authorize | No |
| 90 | IntegrationCatalogController | GET | `/api/integrations/methods/{methodId}/parameters` | Sí | Sí | Fallback | Policy = "CanManageAch" | No |
| 91 | IntegrationCatalogController | GET | `/api/integrations/source-catalog` | Sí | Sí | Fallback | Policy = "CanManageAch" | No |
| 92 | IntegrationCatalogController | GET | `/api/integrations/transformations` | Sí | Sí | Fallback | Policy = "CanManageAch" | No |
| 93 | IntegrationMappingSetsController | GET | `/api/integrations/mappingsets` | Sí | Sí | Fallback | Authorize | No |
| 94 | IntegrationMappingSetsController | POST | `/api/integrations/mappingsets` | Sí | Sí | Fallback | Policy = "CanManageAch" | No |
| 95 | IntegrationMappingSetsController | POST | `/api/integrations/mappingsets/compare` | Sí | Sí | Fallback | Policy = "CanManageAch" | No |
| 96 | IntegrationMappingSetsController | GET | `/api/integrations/mappingsets/published` | Sí | Sí | Fallback | Policy = "CanManageAch" | No |
| 97 | IntegrationMappingSetsController | GET | `/api/integrations/mappingsets/{id}` | Sí | Sí | Fallback | Policy = "CanManageAch" | No |
| 98 | IntegrationMappingSetsController | PUT | `/api/integrations/mappingsets/{id}` | Sí | Sí | Fallback | Policy = "CanManageAch" | No |
| 99 | IntegrationMappingSetsController | POST | `/api/integrations/mappingsets/{id}/clone` | Sí | Sí | Fallback | Policy = "CanManageAch" | No |
| 100 | IntegrationMappingSetsController | GET | `/api/integrations/mappingsets/{id}/history` | Sí | Sí | Fallback | Policy = "CanManageAch" | No |
| 101 | IntegrationMappingSetsController | POST | `/api/integrations/mappingsets/{id}/preview` | Sí | Sí | Fallback | Policy = "CanManageAch" | No |
| 102 | IntegrationMappingSetsController | POST | `/api/integrations/mappingsets/{id}/publish` | Sí | Sí | Fallback | Policy = "CanManageAch" | No |
| 103 | IntegrationMappingSetsController | PUT | `/api/integrations/mappingsets/{id}/rules` | Sí | Sí | Fallback | Policy = "CanManageAch" | No |
| 104 | IntegrationMappingSetsController | POST | `/api/integrations/mappingsets/{id}/validate` | Sí | Sí | Fallback | Policy = "CanManageAch" | No |
| 105 | LoginLockoutSettingsController | GET | `/api/users/login-lockout` | Sí | Sí | Fallback | Authorize | No |
| 106 | LoginLockoutSettingsController | PUT | `/api/users/login-lockout` | Sí | Sí | Fallback | Authorize | No |
| 107 | MaintenanceController | POST | `/Maintenance/seed` | Sí | Sí | Fallback | Sin atributo [Authorize] cercano | No |
| 108 | MenuItemsController | GET | `/navigation/menu-items` | Sí | Sí | Fallback | Roles = "Admin" | No |
| 109 | MenuItemsController | POST | `/navigation/menu-items` | Sí | Sí | Fallback | Roles = "Admin" | No |
| 110 | MenuItemsController | DELETE | `/navigation/menu-items/{id}` | Sí | Sí | Fallback | Roles = "Admin" | No |
| 111 | MenuItemsController | PUT | `/navigation/menu-items/{id}` | Sí | Sí | Fallback | Roles = "Admin" | No |
| 112 | NachaConfigProfilesController | GET | `/nacha-config/catalogos-filtro` | Sí | Sí | Fallback | Policy = "CanReadAch" | No |
| 113 | NachaConfigProfilesController | GET | `/nacha-config/perfiles` | Sí | Sí | Fallback | Policy = "CanManageAch" | No |
| 114 | NachaConfigProfilesController | POST | `/nacha-config/perfiles` | Sí | Sí | Fallback | Policy = "CanReadAch" | No |
| 115 | NachaConfigProfilesController | GET | `/nacha-config/perfiles/{id}` | Sí | Sí | Fallback | Policy = "CanReadAch" | No |
| 116 | NachaConfigProfilesController | PUT | `/nacha-config/perfiles/{id}` | Sí | Sí | Fallback | Policy = "CanManageAch" | No |
| 117 | NachaConfigProfilesController | POST | `/nacha-config/perfiles/{id}/archivar` | Sí | Sí | Fallback | Policy = "CanManageAch" | No |
| 118 | NachaConfigProfilesController | POST | `/nacha-config/perfiles/{id}/clonar` | Sí | Sí | Fallback | Policy = "CanManageAch" | No |
| 119 | NachaConfigProfilesController | PUT | `/nacha-config/perfiles/{id}/fields/{fieldId}` | Sí | Sí | Fallback | Policy = "CanManageAch" | No |
| 120 | NachaConfigProfilesController | GET | `/nacha-config/perfiles/{id}/historial` | Sí | Sí | Fallback | Policy = "CanManageAch" | No |
| 121 | NachaConfigProfilesController | POST | `/nacha-config/perfiles/{id}/inactivar` | Sí | Sí | Fallback | Policy = "CanManageAch" | No |
| 122 | NachaConfigProfilesController | POST | `/nacha-config/perfiles/{id}/publicar` | Sí | Sí | Fallback | Policy = "CanManageAch" | No |
| 123 | NachaConfigProfilesController | PUT | `/nacha-config/perfiles/{id}/records/secuencia` | Sí | Sí | Fallback | Policy = "CanReadAch" | No |
| 124 | NachaConfigProfilesController | PUT | `/nacha-config/perfiles/{id}/rules/{ruleId}` | Sí | Sí | Fallback | Policy = "CanManageAch" | No |
| 125 | NachaConfigProfilesController | GET | `/nacha-config/perfiles/{id}/snapshots` | Sí | Sí | Fallback | Policy = "CanReadAch" | No |
| 126 | NachaConfigProfilesController | POST | `/nacha-config/perfiles/{id}/validar` | Sí | Sí | Fallback | Policy = "CanManageAch" | No |
| 127 | NachaConfigProfilesController | PUT | `/nacha-config/perfiles/{id}/variantes/{variantId}` | Sí | Sí | Fallback | Policy = "CanManageAch" | No |
| 128 | NachaConfigProfilesController | POST | `/nacha-config/resolver-preview` | Sí | Sí | Fallback | Policy = "CanManageAch" | No |
| 129 | NachaController | POST | `/Nacha/header` | Sí | Sí | Fallback | AllowAnonymous | No |
| 130 | NachaExportController | GET | `/NachaExport/{cycleId}` | Sí | Sí | Explícita | Policy = "CanReadAch" | No |
| 131 | NachaExportController | GET | `/NachaExport/{cycleId}/sobre-digital` | Sí | Sí | Explícita | Policy = "CanReadAch" | No |
| 132 | NachaRecordDefinitionsController | GET | `/nacha-record-definitions` | Sí | Sí | Fallback | Authorize | No |
| 133 | NachaRecordDefinitionsController | POST | `/nacha-record-definitions` | Sí | Sí | Fallback | Policy = "CanReadAch" | No |
| 134 | NachaRecordDefinitionsController | DELETE | `/nacha-record-definitions/{id}` | Sí | Sí | Fallback | Policy = "CanManageAch" | No |
| 135 | NachaRecordDefinitionsController | GET | `/nacha-record-definitions/{id}` | Sí | Sí | Fallback | Policy = "CanReadAch" | No |
| 136 | NachaRecordDefinitionsController | PUT | `/nacha-record-definitions/{id}` | Sí | Sí | Fallback | Policy = "CanManageAch" | No |
| 137 | NachaRecordLayoutsController | GET | `/nacha-layouts` | Sí | Sí | Fallback | Authorize | No |
| 138 | NachaRecordLayoutsController | POST | `/nacha-layouts` | Sí | Sí | Fallback | Policy = "CanReadAch" | No |
| 139 | NachaRecordLayoutsController | DELETE | `/nacha-layouts/{id}` | Sí | Sí | Fallback | Policy = "CanManageAch" | No |
| 140 | NachaRecordLayoutsController | GET | `/nacha-layouts/{id}` | Sí | Sí | Fallback | Policy = "CanReadAch" | No |
| 141 | NachaRecordLayoutsController | PUT | `/nacha-layouts/{id}` | Sí | Sí | Fallback | Policy = "CanManageAch" | No |
| 142 | NachaSecurityOperationsController | GET | `/nacha-security/operations/audit` | Sí | Sí | Explícita | Policy = "CanReadAch" | No |
| 143 | NachaSecurityOperationsController | POST | `/nacha-security/operations/envelope/manual-decrypt` | Sí | Sí | Explícita | Policy = FineGrainedPermissions.CanViewNachaSecurityAudit | No |
| 144 | NachaSecurityOperationsController | POST | `/nacha-security/operations/envelope/manual-encrypt` | Sí | Sí | Explícita | Policy = FineGrainedPermissions.CanGenerateEncryptedNacha | No |
| 145 | NachaSecurityOperationsController | POST | `/nacha-security/operations/nacha/generate` | Sí | Sí | Explícita | Authorize | No |
| 146 | NachaSecurityOperationsController | POST | `/nacha-security/operations/nacha/generate-encrypted` | Sí | Sí | Explícita | Policy = FineGrainedPermissions.CanGenerateNacha | No |
| 147 | NachaSecurityOperationsController | GET | `/nacha-security/operations/{operationId}` | Sí | Sí | Explícita | Policy = FineGrainedPermissions.CanViewNachaSecurityAudit | No |
| 148 | NachaSecurityOperationsController | POST | `/nacha-security/operations/{operationId}/authorize-download` | Sí | Sí | Explícita | Policy = FineGrainedPermissions.CanViewNachaSecurityAudit | No |
| 149 | NachaSecurityOperationsController | GET | `/nacha-security/operations/{operationId}/download` | Sí | Sí | Explícita | Policy = FineGrainedPermissions.CanViewNachaSecurityAudit | No |
| 150 | NachaUploadController | GET | `/NachaUpload/records` | Sí | Sí | Fallback | Policy = "CanManageAch" | No |
| 151 | NachaUploadController | POST | `/NachaUpload/upload` | Sí | Sí | Fallback | Policy = "CanManageAch" | No |
| 152 | NavigationController | GET | `/navigation/menu` | Sí | Sí | Fallback | Authorize | No |
| 153 | NavigationLogsController | GET | `/api/navigation-logs` | Sí | Sí | Fallback | Authorize | No |
| 154 | NavigationLogsController | POST | `/api/navigation-logs` | Sí | Sí | Fallback | Policy = "CanReadAch" | No |
| 155 | OauthsController | POST | `/Oauths/GenerateToken` | Sí | Sí | Fallback | AllowAnonymous | No |
| 156 | OauthsController | POST | `/Oauths/GenerateTokenAsync` | Sí | Sí | Fallback | Sin atributo [Authorize] cercano | No |
| 157 | PasswordRulesController | GET | `/api/users/password-rules` | Sí | Sí | Fallback | Authorize | No |
| 158 | PasswordRulesController | PUT | `/api/users/password-rules` | Sí | Sí | Fallback | Authorize | No |
| 159 | PaymentRailCapabilityRegistryController | GET | `/api/payment-rails/capability-registry/rails` | Sí | Sí | Explícita | Authorize | No |
| 160 | PaymentRailCapabilityRegistryController | GET | `/api/payment-rails/capability-registry/rails/{railCode}/capabilities` | Sí | Sí | Explícita | Policy = FineGrainedPermissions.CanViewPaymentRailCapabilityRegistry | No |
| 161 | PaymentRailCapabilityRegistryController | GET | `/api/payment-rails/capability-registry/rails/{railCode}/capabilities/{capabilityCode}` | Sí | Sí | Explícita | Policy = FineGrainedPermissions.CanViewPaymentRailCapabilityRegistry | No |
| 162 | PermissionsController | GET | `/api/Permissions` | Sí | Sí | Fallback | Authorize | No |
| 163 | RegulatoryCatalogsController | GET | `/api/regulatory-catalogs/file-rejection-codes` | Sí | Sí | Fallback | Sin atributo [Authorize] cercano | No |
| 164 | RegulatoryCatalogsController | GET | `/api/regulatory-catalogs/prenotification-policies` | Sí | Sí | Fallback | Sin atributo [Authorize] cercano | No |
| 165 | RegulatoryCatalogsController | GET | `/api/regulatory-catalogs/return-codes` | Sí | Sí | Fallback | Sin atributo [Authorize] cercano | No |
| 166 | RegulatoryCatalogsController | GET | `/api/regulatory-catalogs/return-of-return-policies` | Sí | Sí | Fallback | Sin atributo [Authorize] cercano | No |
| 167 | RegulatoryCatalogsController | GET | `/api/regulatory-catalogs/return-policies` | Sí | Sí | Fallback | Sin atributo [Authorize] cercano | No |
| 168 | RegulatoryCatalogsController | GET | `/api/regulatory-catalogs/transaction-type-policies` | Sí | Sí | Fallback | Sin atributo [Authorize] cercano | No |
| 169 | ReportsController | GET | `/api/reports/audit` | Sí | Sí | Explícita | Policy = "CanReadAch" | No |
| 170 | ReportsController | GET | `/api/reports/audit/pdf` | Sí | Sí | Explícita | Policy = "CanReadAch" | No |
| 171 | ReportsController | GET | `/api/reports/cycles` | Sí | Sí | Explícita | Policy = "CanReadAch" | No |
| 172 | ReportsController | GET | `/api/reports/cycles/pdf` | Sí | Sí | Explícita | Policy = "CanReadAch" | No |
| 173 | ReportsController | GET | `/api/reports/history` | Sí | Sí | Explícita | Policy = "CanReadAch" | No |
| 174 | ReportsController | GET | `/api/reports/history/pdf` | Sí | Sí | Explícita | Policy = "CanReadAch" | No |
| 175 | ReportsController | GET | `/api/reports/nacha-files` | Sí | Sí | Explícita | Policy = "CanReadAch" | No |
| 176 | ReportsController | GET | `/api/reports/nacha-files/pdf` | Sí | Sí | Explícita | Policy = "CanReadAch" | No |
| 177 | ReportsController | GET | `/api/reports/reconciliation` | Sí | Sí | Explícita | Policy = "CanReadAch" | No |
| 178 | ReportsController | GET | `/api/reports/reconciliation/pdf` | Sí | Sí | Explícita | Policy = "CanReadAch" | No |
| 179 | ReportsController | GET | `/api/reports/rejections` | Sí | Sí | Explícita | Policy = "CanReadAch" | No |
| 180 | ReportsController | GET | `/api/reports/rejections/pdf` | Sí | Sí | Explícita | Policy = "CanReadAch" | No |
| 181 | ReportsController | GET | `/api/reports/returns` | Sí | Sí | Explícita | Policy = "CanReadAch" | No |
| 182 | ReportsController | GET | `/api/reports/returns/pdf` | Sí | Sí | Explícita | Policy = "CanReadAch" | No |
| 183 | ReportsController | GET | `/api/reports/traceability/pdf` | Sí | Sí | Explícita | Policy = "CanReadAch" | No |
| 184 | ReportsController | GET | `/api/reports/transactions/received` | Sí | Sí | Explícita | Policy = "CanReadAch" | No |
| 185 | ReportsController | GET | `/api/reports/transactions/received/pdf` | Sí | Sí | Explícita | Policy = "CanReadAch" | No |
| 186 | ReportsController | GET | `/api/reports/transactions/sent` | Sí | Sí | Explícita | Policy = "CanReadAch" | No |
| 187 | ReportsController | GET | `/api/reports/transactions/sent/pdf` | Sí | Sí | Explícita | Policy = "CanReadAch" | No |
| 188 | ReturnReasonsController | GET | `/return-reasons` | Sí | Sí | Fallback | Authorize | No |
| 189 | RolesController | GET | `/api/Roles` | Sí | Sí | Fallback | Authorize | No |
| 190 | SoapIntegrationSettingsController | GET | `/api/users/soap-integrations` | Sí | Sí | Fallback | Authorize | No |
| 191 | SoapIntegrationSettingsController | PUT | `/api/users/soap-integrations` | Sí | Sí | Fallback | Authorize | No |
| 192 | SobreDigitalController | POST | `/SobreDigital/decrypt` | Sí | Sí | Fallback | Sin atributo [Authorize] cercano | No |
| 193 | SobreDigitalController | POST | `/SobreDigital/encrypt` | Sí | Sí | Fallback | Sin atributo [Authorize] cercano | No |
| 194 | SobreDigitalController | POST | `/SobreDigital/testRSA` | Sí | Sí | Fallback | Sin atributo [Authorize] cercano | No |
| 195 | TaskDefinitionsController | GET | `/TaskDefinitions` | Sí | Sí | Fallback | Authorize | No |
| 196 | TaskDefinitionsController | POST | `/TaskDefinitions` | Sí | Sí | Fallback | Policy = "CanReadAch" | No |
| 197 | TaskDefinitionsController | DELETE | `/TaskDefinitions/{id}` | Sí | Sí | Fallback | Policy = "CanManageAch" | No |
| 198 | TaskDefinitionsController | GET | `/TaskDefinitions/{id}` | Sí | Sí | Fallback | Policy = "CanReadAch" | No |
| 199 | TaskDefinitionsController | PUT | `/TaskDefinitions/{id}` | Sí | Sí | Fallback | Policy = "CanManageAch" | No |
| 200 | TransactionsController | GET | `/Transactions` | Sí | Sí | Fallback | Sin atributo [Authorize] cercano | No |
| 201 | TransactionsController | POST | `/Transactions` | Sí | Sí | Fallback | Sin atributo [Authorize] cercano | No |
| 202 | TransactionsController | POST | `/Transactions/bulk` | Sí | Sí | Fallback | Sin atributo [Authorize] cercano | No |
| 203 | TransactionsController | POST | `/Transactions/bulk/submit` | Sí | Sí | Fallback | Sin atributo [Authorize] cercano | No |
| 204 | TransactionsController | GET | `/Transactions/company-entry-descriptions` | Sí | Sí | Fallback | Sin atributo [Authorize] cercano | No |
| 205 | TransactionsController | GET | `/Transactions/policies/preview` | Sí | Sí | Fallback | Sin atributo [Authorize] cercano | No |
| 206 | TransactionsController | GET | `/Transactions/{id}` | Sí | Sí | Fallback | Sin atributo [Authorize] cercano | No |
| 207 | UsersController | GET | `/api/Users` | Sí | Sí | Fallback | Authorize | No |
| 208 | UsersController | POST | `/api/Users` | Sí | Sí | Fallback | Authorize | No |
| 209 | UsersController | GET | `/api/Users/validate-email-domain` | Sí | Sí | Fallback | Authorize | No |
| 210 | UsersController | DELETE | `/api/Users/{id}` | Sí | Sí | Fallback | Authorize | No |
| 211 | UsersController | GET | `/api/Users/{id}` | Sí | Sí | Fallback | Authorize | No |
| 212 | UsersController | PUT | `/api/Users/{id}` | Sí | Sí | Fallback | Authorize | No |
| 213 | UsersController | POST | `/api/Users/{id}/roles` | Sí | Sí | Fallback | Authorize | No |

## Priorización para documentación manual explícita (siguiente fase)
1. Rutas críticas con fallback en dominios de seguridad (`auth`, `token`, `certificate`, `security`).
2. Rutas de modificación (`POST`, `PUT`, `PATCH`, `DELETE`) con fallback en flujos NACHA/CENIT.
3. Rutas con `Permiso evidenciado` como `Sin atributo [Authorize] cercano` o `No evidenciado en mapeo` para trazabilidad de control de acceso.
4. Rutas marcadas con `Riesgo de genericidad = Revisar` para mejorar texto de negocio en documentación explícita.