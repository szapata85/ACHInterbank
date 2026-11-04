using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using Cfa.ACHInterbank.Api;
using Cfa.ACHInterbank.Api.Controllers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi;
using Xunit.Abstractions;

namespace Cfa.ACHInterbank.Tests;

public sealed class OpenApiDocumentGenerationTests
{
    private static readonly HashSet<string> HttpMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        "get", "put", "post", "delete", "options", "head", "patch", "trace"
    };

    private static readonly HashSet<string> OfficialIntegrationTags = new(StringComparer.Ordinal)
    {
        "Autenticación y acceso",
        "Usuarios y permisos",
        "Cámaras y ciclos",
        "Transacciones interbancarias",
        "Devoluciones y respuestas",
        "Trazabilidad y conciliación",
        "Políticas y reglas transaccionales",
        "NACHA-M",
        "CENIT",
        "ACH Colombia",
        "Seguridad y certificados",
        "Integraciones",
        "Operación y programación",
        "Reportes",
        "Auditoría y observabilidad",
        "Administración y catálogos"
    };

    private static readonly (string Method, string Path)[] BaselineQgOperations =
    [
        ("GET", "/incoming-nacha-command-center/ingestions/{ingestionId}/validations"),
        ("GET", "/incoming-nacha-command-center/ingestions/{ingestionId}/batches"),
        ("GET", "/incoming-nacha-command-center/ingestions/{ingestionId}/transactions"),
        ("GET", "/incoming-nacha-command-center/ingestions/{ingestionId}/transactions/{entryDetailId}/addendas"),
        ("GET", "/incoming-nacha-command-center/observability/summary"),
        ("GET", "/incoming-nacha-command-center/ingestions"),
        ("GET", "/incoming-nacha-command-center/ingestions/{ingestionId}"),
        ("GET", "/incoming-nacha-command-center/orphans"),
        ("GET", "/incoming-nacha-command-center/orphans/{linkId}"),
        ("GET", "/incoming-nacha-command-center/orphans/{linkId}/candidates"),
        ("POST", "/incoming-nacha-command-center/orphans/{linkId}/resolve"),
        ("GET", "/incoming-nacha-command-center/queue"),
        ("GET", "/incoming-nacha-command-center/queue/{queueId}"),
        ("POST", "/incoming-nacha-command-center/queue/{queueId}/retry"),
        ("POST", "/incoming-nacha-command-center/queue/{queueId}/unblock"),
        ("POST", "/incoming-nacha-command-center/queue/{queueId}/requeue"),
        ("POST", "/incoming-nacha-command-center/queue/{queueId}/mark-failed-final"),
        ("POST", "/Auth/login"),
        ("POST", "/Auth/forgot-password"),
        ("POST", "/Auth/reset-password"),
        ("POST", "/Auth/refresh"),
        ("GET", "/clearing-houses"),
        ("GET", "/clearing-houses/operational"),
        ("GET", "/clearing-houses/payment-rail-options"),
        ("GET", "/clearing-houses/nacha-profiles"),
        ("GET", "/clearing-houses/{id}"),
        ("GET", "/clearing-houses/{id}/readiness"),
        ("POST", "/clearing-houses"),
        ("PUT", "/clearing-houses/{id}"),
        ("PATCH", "/clearing-houses/{id}/status"),
        ("GET", "/clearing-houses/{id}/cycles"),
        ("GET", "/ach-cycles"),
        ("GET", "/ach-cycles/exportable"),
        ("GET", "/ach-cycles/{id}"),
        ("POST", "/ach-cycles"),
        ("PUT", "/ach-cycles/{id}"),
        ("POST", "/ach-cycles/repair-configuration-links"),
        ("DELETE", "/ach-cycles/{id}"),
        ("GET", "/Transactions"),
        ("GET", "/Transactions/company-entry-descriptions"),
        ("GET", "/Transactions/policies/preview"),
        ("POST", "/Transactions"),
        ("GET", "/Transactions/{id}"),
        ("POST", "/Transactions/bulk/submit"),
        ("POST", "/Transactions/bulk"),
        ("GET", "/Transactions/{id}/integration-result"),
        ("GET", "/Transactions/{id}/integration-readiness"),
        ("POST", "/api/transactions/bulk-ingestion/upload"),
        ("GET", "/api/transactions/bulk-ingestion/{batchId}"),
        ("GET", "/api/transactions/bulk-ingestion/{batchId}/items"),
        ("GET", "/api/transactions/bulk-ingestion/{batchId}/summary"),
        ("POST", "/api/transactions/bulk-ingestion/{batchId}/retry"),
        ("POST", "/api/transactions/bulk-ingestion/{batchId}/cancel"),
        ("POST", "/api/ach-traceability/sol02/{transactionId}/certify"),
        ("GET", "/api/ach-traceability/transactions/{transactionId}"),
        ("GET", "/api/ach-traceability/report"),
        ("GET", "/ach-returns/cycles/{cycleId}/transactions"),
        ("POST", "/ach-returns/generate-file"),
        ("GET", "/return-reasons"),
        ("POST", "/ach-returns/return-of-return/cenit/out"),
        ("POST", "/ach-returns/return-of-return/evaluate"),
        ("POST", "/ach-returns/return-of-return/generate-audit-file"),
        ("POST", "/ach-returns/return-of-return/generate-nacha-file"),
        ("POST", "/api/ach/responses/process"),
        ("POST", "/api/ach/responses/notifications/send"),
        ("GET", "/api/ach/responses"),
        ("GET", "/api/ach/responses/dashboard"),
        ("GET", "/api/ach/responses/{id}"),
        ("GET", "/api/ach/responses/{id}/notification-attempts"),
        ("GET", "/api/ach/response-status-mappings"),
        ("GET", "/api/clearing-houses/{clearingHouseId}/transaction-policies"),
        ("GET", "/api/clearing-houses/{clearingHouseId}/transaction-policies/current"),
        ("GET", "/api/clearing-houses/{clearingHouseId}/transaction-policies/{id}"),
        ("POST", "/api/clearing-houses/{clearingHouseId}/transaction-policies"),
        ("PATCH", "/api/clearing-houses/{clearingHouseId}/transaction-policies/{id}/metadata"),
        ("POST", "/api/clearing-houses/{clearingHouseId}/transaction-policies/{id}/close"),
        ("POST", "/api/clearing-houses/{clearingHouseId}/transaction-policies/{id}/activate"),
        ("POST", "/api/clearing-houses/{clearingHouseId}/transaction-policies/preview"),
        ("GET", "/api/clearing-house-transaction-rules"),
        ("GET", "/api/clearing-house-transaction-rules/{id}"),
        ("POST", "/api/clearing-house-transaction-rules"),
        ("PUT", "/api/clearing-house-transaction-rules/{id}"),
        ("PATCH", "/api/clearing-house-transaction-rules/{id}/activate"),
        ("PATCH", "/api/clearing-house-transaction-rules/{id}/deactivate"),
        ("GET", "/api/cfa-operational-transaction-restrictions"),
        ("GET", "/api/cfa-operational-transaction-restrictions/{id}"),
        ("POST", "/api/cfa-operational-transaction-restrictions"),
        ("PUT", "/api/cfa-operational-transaction-restrictions/{id}"),
        ("PATCH", "/api/cfa-operational-transaction-restrictions/{id}/activate"),
        ("PATCH", "/api/cfa-operational-transaction-restrictions/{id}/deactivate"),
        ("GET", "/api/payment-rails/capability-registry/rails"),
        ("GET", "/api/payment-rails/capability-registry/rails/{railCode}/capabilities"),
        ("GET", "/api/payment-rails/capability-registry/rails/{railCode}/capabilities/{capabilityCode}"),
        ("POST", "/api/transaction-prerequisite-policy/preview"),
        ("GET", "/nacha-record-definitions"),
        ("GET", "/nacha-record-definitions/{id}"),
        ("POST", "/nacha-record-definitions"),
        ("PUT", "/nacha-record-definitions/{id}"),
        ("DELETE", "/nacha-record-definitions/{id}"),
        ("GET", "/nacha-layouts"),
        ("GET", "/nacha-layouts/{id}"),
        ("POST", "/nacha-layouts"),
        ("PUT", "/nacha-layouts/{id}"),
        ("DELETE", "/nacha-layouts/{id}"),
        ("GET", "/api/ach/response-status-mappings/{id}"),
        ("POST", "/api/ach/response-status-mappings"),
        ("PUT", "/api/ach/response-status-mappings/{id}"),
        ("POST", "/api/ach/response-status-mappings/{id}/activate"),
        ("POST", "/api/ach/response-status-mappings/{id}/deactivate"),
        ("GET", "/api/ach/response-status-mappings/{id}/audit"),
        ("GET", "/api/ach/responses/{id}/audit"),
        ("GET", "/api/ach/responses/orphans"),
        ("POST", "/api/ach/responses/{id}/orphan"),
        ("POST", "/api/ach/responses/orphans/{id}/review/start"),
        ("POST", "/api/ach/responses/orphans/{id}/resolve"),
        ("POST", "/api/ach/responses/{id}/reprocess"),
        ("GET", "/api/ach/responses/{id}/reprocess-attempts"),
        ("GET", "/api/ach/responses/{id}/reprocess-attempts/{attemptId}"),
        ("POST", "/api/cenit/chamber-responses"),
        ("GET", "/api/cenit/chamber-responses/{id}"),
        ("GET", "/api/cenit/chamber-responses"),
        ("GET", "/api/cenit/queues"),
        ("GET", "/api/cenit/net-positions"),
        ("GET", "/api/cenit/optimization-decisions"),
        ("GET", "/api/cenit/traceability"),
        ("POST", "/nacha-security/certificates/management/managed/preview"),
        ("POST", "/api/nacha-security/certificates/management/managed/preview"),
        ("POST", "/nacha-security/certificates/management/managed"),
        ("POST", "/api/nacha-security/certificates/management/managed"),
        ("POST", "/nacha-security/certificates/management/public"),
        ("POST", "/api/nacha-security/certificates/management/public"),
        ("POST", "/nacha-security/certificates/management/private"),
        ("POST", "/api/nacha-security/certificates/management/private"),
        ("GET", "/nacha-security/certificates/management"),
        ("GET", "/api/nacha-security/certificates/management"),
        ("GET", "/nacha-security/certificates/management/{id}/versions"),
        ("GET", "/api/nacha-security/certificates/management/{id}/versions"),
        ("GET", "/nacha-security/certificates"),
        ("POST", "/nacha-security/certificates"),
        ("DELETE", "/nacha-security/certificates/{id}"),
        ("GET", "/api/nacha-security/digital-envelope/certificates"),
        ("POST", "/api/nacha-security/digital-envelope/encrypt"),
        ("POST", "/api/nacha-security/digital-envelope/decrypt"),
        ("POST", "/nacha-security/operations/nacha/generate"),
        ("POST", "/nacha-security/operations/nacha/generate-encrypted"),
        ("POST", "/nacha-security/operations/envelope/manual-encrypt"),
        ("POST", "/nacha-security/operations/envelope/manual-decrypt"),
        ("GET", "/nacha-security/operations/{operationId}"),
        ("GET", "/nacha-security/operations/audit"),
        ("POST", "/nacha-security/operations/{operationId}/authorize-download"),
        ("GET", "/nacha-security/operations/{operationId}/download"),
        ("GET", "/NachaExport/{cycleId}"),
        ("GET", "/NachaExport/{cycleId}/sobre-digital"),
        ("POST", "/NachaUpload/upload"),
        ("GET", "/NachaUpload/records"),
        ("GET", "/api/ach-colombia/file-exchange/transfers"),
        ("GET", "/api/ach-colombia/file-exchange/transfers/{id}"),
        ("GET", "/api/ach-colombia/file-exchange/transfers/{id}/download"),
        ("POST", "/api/ach-colombia/file-exchange/outbound/execute"),
        ("POST", "/api/ach-colombia/file-exchange/inbound/execute"),
        ("POST", "/api/ach-colombia/file-exchange/transfers/{id}/retry"),
        ("POST", "/api/ach-colombia/file-exchange/transfers/{id}/reprocess"),
        ("POST", "/api/ach-colombia/file-exchange/transfers/{id}/archive"),
        ("POST", "/api/ach-colombia/file-exchange/transfers/{id}/retire"),
        ("GET", "/api/integrations/mappingsets"),
        ("GET", "/api/integrations/mappingsets/published"),
        ("GET", "/api/integrations/mappingsets/{id}"),
        ("POST", "/api/integrations/mappingsets"),
        ("PUT", "/api/integrations/mappingsets/{id}"),
        ("PUT", "/api/integrations/mappingsets/{id}/rules"),
        ("POST", "/api/integrations/mappingsets/{id}/validate"),
        ("POST", "/api/integrations/mappingsets/{id}/preview"),
        ("POST", "/api/integrations/mappingsets/{id}/publish"),
        ("POST", "/api/integrations/mappingsets/{id}/clone"),
        ("GET", "/api/integrations/mappingsets/{id}/history"),
        ("POST", "/api/integrations/mappingsets/compare"),
        ("GET", "/api/users/soap-integrations"),
        ("PUT", "/api/users/soap-integrations"),
        ("POST", "/api/uat/contrapartidas/dispatch-cycle"),
        ("GET", "/api/scheduler/overview"),
        ("GET", "/api/scheduler/tasks"),
        ("GET", "/api/scheduler/tasks/{taskCode}"),
        ("GET", "/api/scheduler/tasks/{taskCode}/technical"),
        ("GET", "/api/scheduler/history"),
        ("GET", "/api/scheduler/tasks/{taskCode}/history"),
        ("GET", "/api/scheduler/instances"),
        ("GET", "/api/scheduler/executions/{executionId}"),
        ("POST", "/api/scheduler/tasks/{taskCode}/execute"),
        ("POST", "/api/scheduler/tasks/{taskCode}/pause"),
        ("POST", "/api/scheduler/tasks/{taskCode}/resume"),
        ("PUT", "/api/scheduler/tasks/{taskCode}/schedule"),
        ("POST", "/api/scheduler/schedule/preview"),
        ("GET", "/TaskDefinitions"),
        ("GET", "/TaskDefinitions/{id}"),
        ("POST", "/TaskDefinitions"),
        ("PUT", "/TaskDefinitions/{id}"),
        ("DELETE", "/TaskDefinitions/{id}"),
        ("GET", "/api/reports/transactions/sent"),
        ("GET", "/api/reports/transactions/received"),
        ("GET", "/api/reports/transactions/sent/pdf"),
        ("GET", "/api/reports/transactions/received/pdf"),
        ("GET", "/api/reports/returns"),
        ("GET", "/api/reports/rejections"),
        ("GET", "/api/reports/returns/pdf"),
        ("GET", "/api/reports/rejections/pdf"),
        ("GET", "/api/reports/nacha-files"),
        ("GET", "/api/reports/nacha-files/pdf"),
        ("GET", "/api/reports/cycles"),
        ("GET", "/api/reports/cycles/pdf"),
        ("GET", "/api/reports/reconciliation"),
        ("GET", "/api/reports/reconciliation/pdf"),
        ("GET", "/api/reports/audit"),
        ("GET", "/api/reports/audit/pdf"),
        ("GET", "/api/reports/history"),
        ("GET", "/api/reports/history/pdf"),
        ("GET", "/api/reports/traceability/pdf"),
        ("POST", "/api/reports/accounting-review/export"),
        ("GET", "/api/integrations/methods"),
        ("GET", "/api/integrations/methods/{methodId}/parameters"),
        ("GET", "/api/integrations/source-catalog"),
        ("GET", "/api/integrations/transformations"),
        ("GET", "/api/ach-colombia/file-exchange/configuration"),
        ("PUT", "/api/ach-colombia/file-exchange/configuration"),
        ("GET", "/api/ach-colombia/file-exchange/administration"),
        ("PUT", "/api/ach-colombia/file-exchange/administration"),
        ("PUT", "/api/ach-colombia/file-exchange/administration/credential"),
        ("GET", "/nacha-config/perfiles"),
        ("GET", "/nacha-config/catalogos-filtro"),
        ("GET", "/nacha-config/catalogos-operativos"),
        ("GET", "/nacha-config/perfiles/{id}"),
        ("GET", "/nacha-config/perfiles/{id}/workspace"),
        ("GET", "/nacha-config/perfiles/{id}/fields/{fieldId}/editor"),
        ("GET", "/nacha-config/perfiles/{id}/cambios"),
        ("GET", "/nacha-config/perfiles/{id}/historial"),
        ("GET", "/nacha-config/perfiles/{id}/snapshots"),
        ("GET", "/api/ach/nacha/config-profiles/dashboard"),
        ("GET", "/api/ach/nacha/config-profiles"),
        ("GET", "/api/ach/nacha/config-profiles/{id}"),
        ("GET", "/api/ach/nacha/config-profiles/by-code/{profileCode}"),
        ("GET", "/api/ach/nacha/config-profiles/{id}/variants"),
        ("GET", "/api/ach/nacha/config-profiles/{id}/fields"),
        ("GET", "/api/audit-logs"),
        ("GET", "/api/navigation-logs"),
        ("POST", "/api/navigation-logs"),
        ("GET", "/health/live"),
        ("GET", "/health/ready")
    ];

    private static readonly Sc508PublicContractOperation[] Sc508PublicContractOperations =
    [
        new("GET", "/api/ach-claims", typeof(AchClaimsController), nameof(AchClaimsController.Query)),
        new("GET", "/api/ach-claims/by-transaction/{transactionId}", typeof(AchClaimsController), nameof(AchClaimsController.GetByTransaction)),
        new("POST", "/api/ach-claims/dev14", typeof(AchClaimsController), nameof(AchClaimsController.CreateDev14)),
        new("GET", "/api/ach-claims/{id}", typeof(AchClaimsController), nameof(AchClaimsController.GetById)),
        new("POST", "/api/ach-claims/{id}/expire", typeof(AchClaimsController), nameof(AchClaimsController.Expire)),
        new("POST", "/api/ach-claims/{id}/progress", typeof(AchClaimsController), nameof(AchClaimsController.RecordProgress)),
        new("POST", "/api/ach-claims/{id}/resolve", typeof(AchClaimsController), nameof(AchClaimsController.Resolve)),
        new("POST", "/ach-returns/transport/files/{fileName}/dispatch", typeof(AchOutboundReturnTransportController), nameof(AchOutboundReturnTransportController.DispatchExisting)),
        new("POST", "/ach-returns/transport/generate-and-dispatch", typeof(AchOutboundReturnTransportController), nameof(AchOutboundReturnTransportController.GenerateAndDispatch)),
        new("POST", "/ach-returns/transport/results", typeof(AchOutboundReturnTransportController), nameof(AchOutboundReturnTransportController.ProcessResult)),
        new("GET", "/api/ach/reconciliation/dashboard", typeof(AchReconciliationController), nameof(AchReconciliationController.GetDashboard)),
        new("GET", "/api/ach/reconciliation/exceptions", typeof(AchReconciliationController), nameof(AchReconciliationController.GetExceptions)),
        new("GET", "/api/ach/reconciliation/exceptions/{id}", typeof(AchReconciliationController), nameof(AchReconciliationController.GetException)),
        new("POST", "/api/ach/reconciliation/exceptions/{id}/resolve", typeof(AchReconciliationController), nameof(AchReconciliationController.ResolveException)),
        new("GET", "/api/ach/reconciliation/items", typeof(AchReconciliationController), nameof(AchReconciliationController.GetItems)),
        new("GET", "/api/ach/reconciliation/items/by-correlation/{correlationId}", typeof(AchReconciliationController), nameof(AchReconciliationController.GetItemByCorrelation)),
        new("GET", "/api/ach/reconciliation/items/{reconciliationId}", typeof(AchReconciliationController), nameof(AchReconciliationController.GetItem)),
        new("GET", "/bank-holidays", typeof(BankHolidaysController), nameof(BankHolidaysController.GetAll)),
        new("POST", "/bank-holidays", typeof(BankHolidaysController), nameof(BankHolidaysController.Create)),
        new("POST", "/bank-holidays/ensure", typeof(BankHolidaysController), nameof(BankHolidaysController.Ensure)),
        new("DELETE", "/bank-holidays/{id}", typeof(BankHolidaysController), nameof(BankHolidaysController.Delete)),
        new("PUT", "/bank-holidays/{id}", typeof(BankHolidaysController), nameof(BankHolidaysController.Update)),
        new("GET", "/api/users/branding", typeof(BrandingController), nameof(BrandingController.GetBrandingAsync)),
        new("PUT", "/api/users/branding", typeof(BrandingController), nameof(BrandingController.SaveBrandingAsync)),
        new("GET", "/catalog-types/{catalogType}", typeof(CatalogTypesController), nameof(CatalogTypesController.GetAll)),
        new("POST", "/catalog-types/{catalogType}", typeof(CatalogTypesController), nameof(CatalogTypesController.Create)),
        new("DELETE", "/catalog-types/{catalogType}/{code}", typeof(CatalogTypesController), nameof(CatalogTypesController.Delete)),
        new("PUT", "/catalog-types/{catalogType}/{code}", typeof(CatalogTypesController), nameof(CatalogTypesController.Update)),
        new("GET", "/clearing-house-cycle-configs", typeof(ClearingHouseCycleConfigsController), nameof(ClearingHouseCycleConfigsController.GetByClearingHouse)),
        new("POST", "/clearing-house-cycle-configs", typeof(ClearingHouseCycleConfigsController), nameof(ClearingHouseCycleConfigsController.CreateVersion)),
        new("GET", "/clearing-house-cycle-configs/current", typeof(ClearingHouseCycleConfigsController), nameof(ClearingHouseCycleConfigsController.GetCurrentByClearingHouse)),
        new("POST", "/clearing-house-cycle-configs/{id}/inactivate", typeof(ClearingHouseCycleConfigsController), nameof(ClearingHouseCycleConfigsController.Inactivate)),
        new("PATCH", "/clearing-house-cycle-configs/{id}/status", typeof(ClearingHouseCycleConfigsController), nameof(ClearingHouseCycleConfigsController.ChangeStatus)),
        new("GET", "/api/clearing-house-special-dates", typeof(ClearingHouseSpecialDatesController), nameof(ClearingHouseSpecialDatesController.GetAll)),
        new("POST", "/api/clearing-house-special-dates", typeof(ClearingHouseSpecialDatesController), nameof(ClearingHouseSpecialDatesController.Create)),
        new("PUT", "/api/clearing-house-special-dates/{id}", typeof(ClearingHouseSpecialDatesController), nameof(ClearingHouseSpecialDatesController.Update)),
        new("PATCH", "/api/clearing-house-special-dates/{id}/status", typeof(ClearingHouseSpecialDatesController), nameof(ClearingHouseSpecialDatesController.ChangeStatus)),
        new("GET", "/company-entry-descriptions", typeof(CompanyEntryDescriptionsController), nameof(CompanyEntryDescriptionsController.GetAll)),
        new("POST", "/company-entry-descriptions", typeof(CompanyEntryDescriptionsController), nameof(CompanyEntryDescriptionsController.Create)),
        new("DELETE", "/company-entry-descriptions/{id}", typeof(CompanyEntryDescriptionsController), nameof(CompanyEntryDescriptionsController.Delete)),
        new("PUT", "/company-entry-descriptions/{id}", typeof(CompanyEntryDescriptionsController), nameof(CompanyEntryDescriptionsController.Update)),
        new("GET", "/api/customer-third-parties", typeof(CustomerThirdPartiesController), nameof(CustomerThirdPartiesController.Get)),
        new("GET", "/customers", typeof(CustomersController), nameof(CustomersController.GetAll)),
        new("POST", "/customers", typeof(CustomersController), nameof(CustomersController.Create)),
        new("DELETE", "/customers/{id}", typeof(CustomersController), nameof(CustomersController.Delete)),
        new("GET", "/customers/{id}", typeof(CustomersController), nameof(CustomersController.GetById)),
        new("PUT", "/customers/{id}", typeof(CustomersController), nameof(CustomersController.Update)),
        new("GET", "/financial-institutions", typeof(FinancialInstitutionsController), nameof(FinancialInstitutionsController.GetAll)),
        new("POST", "/financial-institutions", typeof(FinancialInstitutionsController), nameof(FinancialInstitutionsController.Create)),
        new("GET", "/financial-institutions/{id}", typeof(FinancialInstitutionsController), nameof(FinancialInstitutionsController.GetById)),
        new("PUT", "/financial-institutions/{id}", typeof(FinancialInstitutionsController), nameof(FinancialInstitutionsController.Update)),
        new("PATCH", "/financial-institutions/{id}/status", typeof(FinancialInstitutionsController), nameof(FinancialInstitutionsController.SetStatus)),
        new("GET", "/institution-clearing-house-preferences", typeof(InstitutionClearingHousePreferencesController), nameof(InstitutionClearingHousePreferencesController.GetAll)),
        new("POST", "/institution-clearing-house-preferences", typeof(InstitutionClearingHousePreferencesController), nameof(InstitutionClearingHousePreferencesController.Create)),
        new("DELETE", "/institution-clearing-house-preferences/{id}", typeof(InstitutionClearingHousePreferencesController), nameof(InstitutionClearingHousePreferencesController.Delete)),
        new("PUT", "/institution-clearing-house-preferences/{id}", typeof(InstitutionClearingHousePreferencesController), nameof(InstitutionClearingHousePreferencesController.Update)),
        new("GET", "/api/users/login-lockout", typeof(LoginLockoutSettingsController), nameof(LoginLockoutSettingsController.GetAsync)),
        new("PUT", "/api/users/login-lockout", typeof(LoginLockoutSettingsController), nameof(LoginLockoutSettingsController.SaveAsync)),
        new("POST", "/Nacha/header", typeof(NachaController), nameof(NachaController.SaveHeader)),
        new("POST", "/Oauths/GenerateToken", typeof(OauthsController), nameof(OauthsController.GenerateToken)),
        new("POST", "/Oauths/GenerateTokenAsync", typeof(OauthsController), nameof(OauthsController.GenerateTokenAsync)),
        new("GET", "/api/users/password-rules", typeof(PasswordRulesController), nameof(PasswordRulesController.GetRulesAsync)),
        new("PUT", "/api/users/password-rules", typeof(PasswordRulesController), nameof(PasswordRulesController.SaveRulesAsync)),
        new("GET", "/api/Permissions", typeof(PermissionsController), nameof(PermissionsController.GetPermissionsAsync)),
        new("GET", "/api/prenotifications/by-reference/{reference}", typeof(PrenotificationsController), nameof(PrenotificationsController.GetByReference)),
        new("GET", "/api/prenotifications/{id}", typeof(PrenotificationsController), nameof(PrenotificationsController.GetById)),
        new("GET", "/api/regulatory-catalogs/file-rejection-codes", typeof(RegulatoryCatalogsController), nameof(RegulatoryCatalogsController.GetFileRejectionCodes)),
        new("GET", "/api/regulatory-catalogs/prenotification-policies", typeof(RegulatoryCatalogsController), nameof(RegulatoryCatalogsController.GetPrenotificationPolicies)),
        new("GET", "/api/regulatory-catalogs/return-codes", typeof(RegulatoryCatalogsController), nameof(RegulatoryCatalogsController.GetReturnCodes)),
        new("GET", "/api/regulatory-catalogs/return-of-return-policies", typeof(RegulatoryCatalogsController), nameof(RegulatoryCatalogsController.GetReturnOfReturnPolicies)),
        new("GET", "/api/regulatory-catalogs/return-policies", typeof(RegulatoryCatalogsController), nameof(RegulatoryCatalogsController.GetReturnPolicies)),
        new("GET", "/api/regulatory-catalogs/transaction-type-policies", typeof(RegulatoryCatalogsController), nameof(RegulatoryCatalogsController.GetTransactionTypePolicies)),
        new("GET", "/api/Roles", typeof(RolesController), nameof(RolesController.GetRolesAsync)),
        new("GET", "/api/Users", typeof(UsersController), nameof(UsersController.GetUsersAsync)),
        new("POST", "/api/Users", typeof(UsersController), nameof(UsersController.CreateUserAsync)),
        new("GET", "/api/Users/validate-email-domain", typeof(UsersController), nameof(UsersController.ValidateEmailDomainAsync)),
        new("DELETE", "/api/Users/{id}", typeof(UsersController), nameof(UsersController.DeactivateUserAsync)),
        new("GET", "/api/Users/{id}", typeof(UsersController), nameof(UsersController.GetUserAsync)),
        new("PUT", "/api/Users/{id}", typeof(UsersController), nameof(UsersController.UpdateUserAsync)),
        new("POST", "/api/Users/{id}/roles", typeof(UsersController), nameof(UsersController.AssignRolesAsync)),
    ];

    private static readonly Sc508PublicContractOperation[] Phase5PublicContractOperations =
    [
        new("POST", "/api/ach-colombia/file-exchange/administration/test-connection", typeof(AchColombiaFileExchangeController), nameof(AchColombiaFileExchangeController.TestConnection)),
        new("GET", "/api/ach-colombia/operational-schedules/administration", typeof(AchColombiaCycleOperationalSchedulesController), nameof(AchColombiaCycleOperationalSchedulesController.GetAdministration))
    ];

    private static readonly (string Method, string Path)[] Qg1Operations =
    [
        .. BaselineQgOperations,
        .. Sc508PublicContractOperations.Select(operation => (operation.Method, operation.Path)),
        .. Phase5PublicContractOperations.Select(operation => (operation.Method, operation.Path))
    ];

    private static readonly (string Method, string Path)[] Qg2Operations =
    [
        .. BaselineQgOperations,
        .. Sc508PublicContractOperations.Select(operation => (operation.Method, operation.Path)),
        .. Phase5PublicContractOperations.Select(operation => (operation.Method, operation.Path))
    ];

    private static readonly Sc508IntentionalExclusionOperation[] Sc508IntentionalExclusionOperations =
    [
        new("POST", "/Maintenance/incoming-nacha/post-processing", typeof(MaintenanceController), nameof(MaintenanceController.RunIncomingNachaPostProcessing), "Maintenance technical/seed/setup surface"),
        new("GET", "/Maintenance/proc-transacciones-e2e/readiness", typeof(MaintenanceController), nameof(MaintenanceController.InspectProcTransaccionesE2e), "Maintenance technical/seed/setup surface"),
        new("POST", "/Maintenance/proc-transacciones-e2e/setup", typeof(MaintenanceController), nameof(MaintenanceController.SetupProcTransaccionesE2e), "Maintenance technical/seed/setup surface"),
        new("POST", "/Maintenance/seed", typeof(MaintenanceController), nameof(MaintenanceController.RunDbInitializer), "Maintenance technical/seed/setup surface"),
        new("GET", "/api/uat/nacha-inbound-simulator", typeof(NachaInboundSimulatorController), nameof(NachaInboundSimulatorController.List), "UAT/local NACHA inbound simulator guarded by environment"),
        new("GET", "/api/uat/nacha-inbound-simulator/available-cycles", typeof(NachaInboundSimulatorController), nameof(NachaInboundSimulatorController.AvailableCycles), "UAT/local NACHA inbound simulator guarded by environment"),
        new("POST", "/api/uat/nacha-inbound-simulator/eligibility-preview", typeof(NachaInboundSimulatorController), nameof(NachaInboundSimulatorController.Preview), "UAT/local NACHA inbound simulator guarded by environment"),
        new("GET", "/api/uat/nacha-inbound-simulator/eligible-differential-transactions", typeof(NachaInboundSimulatorController), nameof(NachaInboundSimulatorController.EligibleDifferentialTransactions), "UAT/local NACHA inbound simulator guarded by environment"),
        new("POST", "/api/uat/nacha-inbound-simulator/generate", typeof(NachaInboundSimulatorController), nameof(NachaInboundSimulatorController.Generate), "UAT/local NACHA inbound simulator guarded by environment"),
        new("GET", "/api/uat/nacha-inbound-simulator/{id}", typeof(NachaInboundSimulatorController), nameof(NachaInboundSimulatorController.GetById), "UAT/local NACHA inbound simulator guarded by environment"),
        new("GET", "/api/uat/nacha-inbound-simulator/{id}/evidence", typeof(NachaInboundSimulatorController), nameof(NachaInboundSimulatorController.Evidence), "UAT/local NACHA inbound simulator guarded by environment"),
        new("GET", "/api/uat/nacha-inbound-simulator/{id}/file", typeof(NachaInboundSimulatorController), nameof(NachaInboundSimulatorController.DownloadFile), "UAT/local NACHA inbound simulator guarded by environment"),
    ];

    private static readonly (string Method, string Path)[] PendingReservedScopeOperations =
    [
        ("GET", "/api/auth-logs"),
        ("POST", "/nacha-config/perfiles"),
        ("PUT", "/nacha-config/perfiles/{id}"),
        ("POST", "/nacha-config/perfiles/{id}/clonar"),
        ("POST", "/nacha-config/perfiles/{id}/validar"),
        ("POST", "/nacha-config/perfiles/{id}/probar"),
        ("POST", "/nacha-config/perfiles/{id}/publicar"),
        ("POST", "/nacha-config/perfiles/{id}/inactivar"),
        ("POST", "/nacha-config/perfiles/{id}/archivar"),
        ("PUT", "/nacha-config/perfiles/{id}/records/secuencia"),
        ("PUT", "/nacha-config/perfiles/{id}/variantes/{variantId}"),
        ("PUT", "/nacha-config/perfiles/{id}/fields/{fieldId}"),
        ("PUT", "/nacha-config/perfiles/{id}/fields/{fieldId}/editor"),
        ("PUT", "/nacha-config/perfiles/{id}/rules/{ruleId}"),
        ("POST", "/nacha-config/resolver-preview"),
        ("GET", "/api/ach/nacha/soap-uat-console/dashboard"),
        ("GET", "/api/ach/nacha/soap-uat-console/candidates"),
        ("GET", "/api/ach/nacha/soap-uat-console/candidates/{correlationId}"),
        ("GET", "/api/ach/nacha/soap-uat-console/audit"),
        ("GET", "/api/ach/nacha/operational/dashboard"),
        ("GET", "/api/ach/nacha/operational/summary"),
        ("GET", "/api/ach/nacha/operational/files"),
        ("GET", "/api/ach/nacha/operational/files/{fileId}"),
        ("GET", "/api/ach/nacha/operational/decisions"),
        ("GET", "/api/ach/nacha/operational/soap-readiness"),
        ("GET", "/api/ach/nacha/operational/audit"),
        ("POST", "/nacha-security/certificates/management/versions/{id}/activate"),
        ("POST", "/api/nacha-security/certificates/management/versions/{id}/activate"),
        ("POST", "/nacha-security/certificates/management/versions/{id}/revoke"),
        ("POST", "/api/nacha-security/certificates/management/versions/{id}/revoke"),
        ("DELETE", "/nacha-security/certificates/management/versions/{id}"),
        ("DELETE", "/api/nacha-security/certificates/management/versions/{id}"),
        ("POST", "/nacha-security/certificates/management/versions/{id}/validate"),
        ("POST", "/api/nacha-security/certificates/management/versions/{id}/validate"),
        ("GET", "/nacha-security/certificates/management/audit"),
        ("GET", "/api/nacha-security/certificates/management/audit"),
        ("GET", "/api/transactions/outgoing-monitoring"),
        ("GET", "/api/transactions/outgoing-monitoring/{id}"),
    ];

    private static readonly (string Method, string Path)[] PendingCanonicalAliasExclusions =
    [
        ("GET", "/api/ach-cycles"),
        ("GET", "/api/ach-cycles/exportable"),
        ("GET", "/api/ach-cycles/{id}"),
        ("POST", "/api/ach-cycles"),
        ("PUT", "/api/ach-cycles/{id}"),
        ("POST", "/api/ach-cycles/repair-configuration-links"),
        ("DELETE", "/api/ach-cycles/{id}"),
        ("GET", "/api/clearing-houses"),
        ("GET", "/api/clearing-houses/operational"),
        ("GET", "/api/clearing-houses/payment-rail-options"),
        ("GET", "/api/clearing-houses/nacha-profiles"),
        ("GET", "/api/clearing-houses/{id}"),
        ("GET", "/api/clearing-houses/{id}/readiness"),
        ("POST", "/api/clearing-houses"),
        ("PUT", "/api/clearing-houses/{id}"),
        ("PATCH", "/api/clearing-houses/{id}/status"),
        ("GET", "/api/clearing-houses/{id}/cycles"),
        ("GET", "/navigation/menu-items"),
        ("GET", "/api/navigation/menu-items"),
        ("POST", "/navigation/menu-items"),
        ("POST", "/api/navigation/menu-items"),
        ("PUT", "/navigation/menu-items/{id}"),
        ("PUT", "/api/navigation/menu-items/{id}"),
        ("DELETE", "/navigation/menu-items/{id}"),
        ("DELETE", "/api/navigation/menu-items/{id}"),
        ("GET", "/navigation/menu"),
        ("GET", "/api/navigation/menu"),
        ("GET", "/api/transactions"),
        ("GET", "/api/transactions/company-entry-descriptions"),
        ("GET", "/api/transactions/policies/preview"),
        ("POST", "/api/transactions"),
        ("POST", "/api/transactions/bulk/submit"),
        ("POST", "/api/transactions/bulk"),
        ("GET", "/api/transactions/{id}"),
        ("GET", "/api/transactions/{id}/integration-result"),
        ("GET", "/api/transactions/{id}/integration-readiness"),
    ];

    private static readonly ApiExplorerIgnoredOperation[] ApiExplorerIgnoredOperations =
    [
        new("GET", "/oauth2/jwks", null, "JwksController", "GetJwks"),
        new("GET", "/oauth2/TokenClientAssertions", null, "JwksController", "TokenClientAssertions"),
        new("POST", "/oauth2/client-assertion", null, "JwksController", "Authenticate"),
        new("POST", "/oauth2/Genearte-client-assertion", null, "JwksController", "GenerateClientAssertion"),
        new("GET", "/Servers", null, "ServersController", "HandleRequest"),
        new("POST", "/api/nacha-security/digital-envelope/testRSA", typeof(SobreDigitalController), "SobreDigitalController", "testRSA"),
        new("GET", "/Tests", null, "TestsController", "Get"),
        new("GET", "/Tests/Prueba", null, "TestsController", "Get"),
    ];

    private static readonly (string Method, string Path)[] DocumentationRedirectExclusions =
    [
        ("GET", "/"),
        ("GET", "/index.html")
    ];

    private static readonly IncomingNachaQg2Operation[] IncomingNachaQg2Operations =
    [
        new(
            "GET",
            "/incoming-nacha-command-center/ingestions/{ingestionId}/validations",
            Parameters: [new("ingestionId", "path")],
            Responses:
            [
                new("200", "Validaciones de admisión registradas o derivadas para la ingesta solicitada.", Qg2ResponseShape.Collection, "IncomingNachaValidationDto"),
                new("404", "No existe una ingesta NACHA-M entrante con el identificador indicado.", Qg2ResponseShape.NoBody)
            ],
            ResponseSchemaDocumentation: new(
                "IncomingNachaValidationDto",
                ["code", "title", "message", "expectedValue", "foundValue", "suggestedAction", "errorType", "severity", "isSuccessful", "occurredAtUtc"])),
        new(
            "GET",
            "/incoming-nacha-command-center/ingestions/{ingestionId}/batches",
            Parameters:
            [
                new("ingestionId", "path"),
                new("Page", "query"),
                new("PageSize", "query"),
                new("Search", "query", Required: false),
                new("SortBy", "query"),
                new("SortDescending", "query")
            ],
            Responses:
            [
                new("200", "Página de lotes asociados a la ingesta solicitada.", Qg2ResponseShape.IncomingNachaPage, "IncomingNachaBatchDto")
            ],
            ResponseSchemaDocumentation: new(
                "IncomingNachaBatchDto",
                ["id", "batchNumber", "companyName", "serviceClassCode", "standardEntryClassCode", "effectiveEntryDate", "totalTransactions", "totalAmount", "totalDebit", "totalCredit", "companyEntryDescription"])),
        new(
            "GET",
            "/incoming-nacha-command-center/ingestions/{ingestionId}/transactions",
            Parameters:
            [
                new("ingestionId", "path"),
                new("Page", "query"),
                new("PageSize", "query"),
                new("BatchId", "query", Required: false),
                new("Search", "query", Required: false),
                new("ResultCode", "query", Required: false),
                new("BusinessOutcome", "query", Required: false),
                new("ProcessingStatus", "query", Required: false),
                new("HasAddenda", "query", Required: false),
                new("HasTechnicalError", "query", Required: false),
                new("TransactionCode", "query", Required: false),
                new("SortBy", "query"),
                new("SortDescending", "query")
            ],
            Responses:
            [
                new("200", "Página de transacciones asociadas a la ingesta y filtros solicitados.", Qg2ResponseShape.IncomingNachaPage, "IncomingNachaTransactionDto")
            ],
            ResponseSchemaDocumentation: new(
                "IncomingNachaTransactionDto",
                [
                    "id", "batchId", "batchNumber", "traceNumber", "transactionCode", "amount", "addendaCount",
                    "classificationCode", "classificationText", "dispatchStatusCode", "dispatchStatusText", "attemptCount",
                    "processingStatus", "processingStatusText", "businessOutcome", "businessOutcomeText", "resultCode",
                    "resultDescription", "processedAtUtc", "correlationId", "dispatchQueueId", "clearingHouseId",
                    "operationalDate", "achCycleId", "scheduledAtUtc", "startedAtUtc", "finishedAtUtc", "nextRetryAtUtc",
                    "maxAttempts", "soapOperation", "externalTransactionId", "achReturnCodeId", "technicalErrorCode",
                    "technicalErrorMessage", "transactionCodeDescription", "accountNumberMasked", "originInstitution",
                    "destinationInstitution", "recipientNameMasked", "effectiveEntryDate"
                ])),
        new(
            "GET",
            "/incoming-nacha-command-center/ingestions/{ingestionId}/transactions/{entryDetailId}/addendas",
            Parameters: [new("ingestionId", "path"), new("entryDetailId", "path")],
            Responses:
            [
                new("200", "Adendas asociadas a la transacción dentro de la ingesta solicitada.", Qg2ResponseShape.Collection, "IncomingNachaAddendaDto")
            ],
            ResponseSchemaDocumentation: new(
                "IncomingNachaAddendaDto",
                ["id", "typeCode", "sequence", "returnReasonCode", "originalTraceNumber", "paymentInformation"])),
        new(
            "GET",
            "/incoming-nacha-command-center/observability/summary",
            Parameters: [new("windowHours", "query")],
            Responses:
            [
                new("200", "Resumen consolidado de observabilidad para la ventana efectiva solicitada.", Qg2ResponseShape.Schema, "IncomingNachaObservabilitySummaryDto")
            ],
            ResponseSchemaDocumentation: new(
                "IncomingNachaObservabilitySummaryDto",
                ["generatedAtUtc", "windowHours", "pipelineHealth", "ingestionsByStatus", "queueByStatus", "byClearingHouseCycle", "topErrors", "timeline", "achColFileBased"])),
        new(
            "GET",
            "/incoming-nacha-command-center/ingestions",
            Parameters:
            [
                new("Page", "query"),
                new("PageSize", "query"),
                new("IngestionStatus", "query", Required: false),
                new("ParsingStatus", "query", Required: false),
                new("CorrelationId", "query", Required: false),
                new("FileName", "query", Required: false),
                new("ClearingHouseId", "query", Required: false),
                new("AchCycleId", "query", Required: false),
                new("OperationalDate", "query", Required: false),
                new("UploadedFromUtc", "query", Required: false),
                new("UploadedToUtc", "query", Required: false),
                new("ResultCode", "query", Required: false),
                new("BusinessOutcome", "query", Required: false),
                new("HasTechnicalErrors", "query", Required: false),
                new("HasIssues", "query", Required: false),
                new("SortBy", "query"),
                new("SortDescending", "query")
            ],
            Responses:
            [
                new("200", "Página de ingestas inbound que cumplen los filtros solicitados.", Qg2ResponseShape.IncomingNachaPage, "IncomingNachaIngestionListItemDto")
            ],
            ResponseSchemaDocumentation: new(
                "IncomingNachaIngestionListItemDto",
                [
                    "id", "fileName", "correlationId", "ingestionStatus", "cycleResolutionStatus", "parsingStatus",
                    "resolvedClearingHouseId", "resolvedAchCycleId", "operationalDate", "isReprocess", "uploadedAtUtc",
                    "queueItems", "processingEvents", "ingestionStatusText", "stageCode", "stageText", "clearingHouseName",
                    "uploadedBy", "totalBatches", "totalTransactions", "totalDebit", "totalCredit", "processingStatusText",
                    "overallResultText", "scheduledAtUtc", "hasTechnicalErrors", "hasIssues"
                ])),
        new(
            "GET",
            "/incoming-nacha-command-center/ingestions/{ingestionId}",
            Parameters: [new("ingestionId", "path")],
            Responses:
            [
                new("200", "Detalle operativo de la ingesta NACHA-M solicitada.", Qg2ResponseShape.Schema, "IncomingNachaIngestionDetailDto"),
                new("404", "No existe una ingesta NACHA-M entrante con el identificador indicado.", Qg2ResponseShape.NoBody)
            ],
            ResponseSchemaDocumentation: new(
                "IncomingNachaIngestionDetailDto",
                [
                    "id", "fileName", "correlationId", "ingestionStatus", "cycleResolutionStatus", "parsingStatus",
                    "detectedClearingHouseId", "resolvedClearingHouseId", "resolvedAchCycleId", "operationalDate", "notes",
                    "isReprocess", "parentIngestionId", "queue", "events", "summary", "admissionIssue", "ingestionStatusText",
                    "stageCode", "stageText", "clearingHouseName", "uploadedBy", "uploadedAtUtc", "receivedAtUtc",
                    "overallResultText", "pendingTransactions"
                ])),
        new(
            "GET",
            "/incoming-nacha-command-center/orphans",
            Parameters:
            [
                new("Page", "query"),
                new("PageSize", "query"),
                new("Search", "query", Required: false)
            ],
            Responses:
            [
                new("200", "Página de devoluciones entrantes pendientes de relación final.", Qg2ResponseShape.IncomingNachaPage, "IncomingNachaOrphanDto")
            ],
            ResponseSchemaDocumentation: new(
                "IncomingNachaOrphanDto",
                [
                    "id", "ingestionId", "fileName", "receivedAtUtc", "clearingHouseId", "achCycleId", "operationalDate",
                    "entryDetailId", "addendaRecordId", "amount", "traceNumber", "originalTraceNumber", "returnReasonCode",
                    "returnReasonDescription", "accountNumberMasked", "recipientNameMasked", "originInstitution",
                    "destinationInstitution", "linkType", "candidateTransactionIds", "resolvedAchTransactionId",
                    "resolutionStatus", "resolvedBy", "resolvedAtUtc"
                ])),
        new(
            "GET",
            "/incoming-nacha-command-center/orphans/{linkId}",
            Parameters: [new("linkId", "path")],
            Responses:
            [
                new("200", "Detalle de la devolución entrante solicitada.", Qg2ResponseShape.Schema, "IncomingNachaOrphanDto"),
                new("404", "No existe un vínculo de devolución entrante con el identificador indicado.", Qg2ResponseShape.NoBody)
            ],
            ResponseSchemaDocumentation: new(
                "IncomingNachaOrphanDto",
                [
                    "id", "ingestionId", "fileName", "receivedAtUtc", "clearingHouseId", "achCycleId", "operationalDate",
                    "entryDetailId", "addendaRecordId", "amount", "traceNumber", "originalTraceNumber", "returnReasonCode",
                    "returnReasonDescription", "accountNumberMasked", "recipientNameMasked", "originInstitution",
                    "destinationInstitution", "linkType", "candidateTransactionIds", "resolvedAchTransactionId",
                    "resolutionStatus", "resolvedBy", "resolvedAtUtc"
                ])),
        new(
            "GET",
            "/incoming-nacha-command-center/orphans/{linkId}/candidates",
            Parameters: [new("linkId", "path"), new("search", "query", Required: false)],
            Responses:
            [
                new("200", "Transacciones candidatas evaluadas para la devolución entrante.", Qg2ResponseShape.Collection, "IncomingNachaOrphanCandidateDto"),
                new("404", "No existe la devolución entrante indicada.", Qg2ResponseShape.NoBody)
            ],
            ResponseSchemaDocumentation: new(
                "IncomingNachaOrphanCandidateDto",
                ["achTransactionId", "traceNumber", "amount", "effectiveEntryDate", "state", "accountNumberMasked", "originInstitution", "destinationInstitution", "isCompatible", "incompatibilityReasons"])),
        new(
            "POST",
            "/incoming-nacha-command-center/orphans/{linkId}/resolve",
            Parameters: [new("linkId", "path")],
            Responses:
            [
                new("200", "La relación manual fue aplicada o ya se había aplicado al mismo destino.", Qg2ResponseShape.Schema, "IncomingNachaOrphanManualResolutionResult"),
                new("400", "La solicitud no cumple las condiciones requeridas para aplicar la relación manual.", Qg2ResponseShape.Untyped),
                new("404", "No existe la devolución entrante o la transacción seleccionada.", Qg2ResponseShape.Untyped),
                new("409", "La devolución no puede resolverse con la decisión solicitada en su estado actual.", Qg2ResponseShape.Untyped)
            ],
            ResponseSchemaDocumentation: new(
                "IncomingNachaOrphanManualResolutionResult",
                ["isResolved", "status", "processingEventId", "achTransactionStateEventId", "message", "isIdempotentReplay"]),
            RequestSchema: "IncomingNachaOrphanResolveRequestDto",
            RequestProperties: ["achTransactionId", "justification", "comment", "correlationId"]),
        new(
            "GET",
            "/incoming-nacha-command-center/queue",
            Parameters:
            [
                new("Page", "query"),
                new("PageSize", "query"),
                new("IngestionId", "query", Required: false),
                new("QueueStatus", "query", Required: false),
                new("ClearingHouseId", "query", Required: false),
                new("AchCycleId", "query", Required: false)
            ],
            Responses:
            [
                new("200", "Página de ítems de la cola de despacho que cumplen los filtros solicitados.", Qg2ResponseShape.IncomingNachaPage, "IncomingNachaQueueListItemDto")
            ],
            ResponseSchemaDocumentation: new(
                "IncomingNachaQueueListItemDto",
                [
                    "id", "ingestionId", "achTransactionId", "achCycleId", "clearingHouseId", "queueStatus", "priority",
                    "attemptCount", "nextAttemptAtUtc", "lastAttemptAtUtc", "lastErrorCode", "lastErrorMessage",
                    "lastResponseCode", "confirmedAtUtc", "createdAtUtc", "allowedActions", "entryDetailId", "queueStatusText",
                    "scheduledAtUtc", "maxAttempts", "soapOperation"
                ])),
        new(
            "GET",
            "/incoming-nacha-command-center/queue/{queueId}",
            Parameters: [new("queueId", "path")],
            Responses:
            [
                new("200", "Detalle operativo del ítem de cola solicitado.", Qg2ResponseShape.Schema, "IncomingNachaQueueDetailDto"),
                new("404", "No existe un ítem de cola con el identificador indicado.", Qg2ResponseShape.NoBody)
            ],
            ResponseSchemaDocumentation: new(
                "IncomingNachaQueueDetailDto",
                ["queue", "ingestion", "classification", "executions", "events"])),
        new(
            "POST",
            "/incoming-nacha-command-center/queue/{queueId}/retry",
            Parameters: [new("queueId", "path")],
            Responses:
            [
                new("200", "Reintento manual aplicado o solicitud idempotente ya procesada.", Qg2ResponseShape.Schema, "IncomingNachaManualActionResultDto"),
                new("409", "La acción no puede aplicarse con los datos o el estado actual del ítem de cola.", Qg2ResponseShape.Untyped)
            ],
            ResponseSchemaDocumentation: new(
                "IncomingNachaManualActionResultDto",
                ["queueId", "action", "previousStatus", "currentStatus", "isIdempotentReplay", "message", "actionText"]),
            RequestSchema: "IncomingNachaManualActionRequest",
            RequestProperties: ["justification", "idempotencyKey", "priority"]),
        new(
            "POST",
            "/incoming-nacha-command-center/queue/{queueId}/unblock",
            Parameters: [new("queueId", "path")],
            Responses:
            [
                new("200", "Desbloqueo manual aplicado o solicitud idempotente ya procesada.", Qg2ResponseShape.Schema, "IncomingNachaManualActionResultDto"),
                new("409", "La acción no puede aplicarse con los datos o el estado actual del ítem de cola.", Qg2ResponseShape.Untyped)
            ],
            ResponseSchemaDocumentation: new(
                "IncomingNachaManualActionResultDto",
                ["queueId", "action", "previousStatus", "currentStatus", "isIdempotentReplay", "message", "actionText"]),
            RequestSchema: "IncomingNachaManualActionRequest",
            RequestProperties: ["justification", "idempotencyKey", "priority"]),
        new(
            "POST",
            "/incoming-nacha-command-center/queue/{queueId}/requeue",
            Parameters: [new("queueId", "path")],
            Responses:
            [
                new("200", "Reencolado manual aplicado o solicitud idempotente ya procesada.", Qg2ResponseShape.Schema, "IncomingNachaManualActionResultDto"),
                new("409", "La acción no puede aplicarse con los datos o el estado actual del ítem de cola.", Qg2ResponseShape.Untyped)
            ],
            ResponseSchemaDocumentation: new(
                "IncomingNachaManualActionResultDto",
                ["queueId", "action", "previousStatus", "currentStatus", "isIdempotentReplay", "message", "actionText"]),
            RequestSchema: "IncomingNachaManualActionRequest",
            RequestProperties: ["justification", "idempotencyKey", "priority"]),
        new(
            "POST",
            "/incoming-nacha-command-center/queue/{queueId}/mark-failed-final",
            Parameters: [new("queueId", "path")],
            Responses:
            [
                new("200", "El ítem fue marcado como falla final o la misma solicitud ya había sido procesada.", Qg2ResponseShape.Schema, "IncomingNachaManualActionResultDto"),
                new("409", "La acción no puede aplicarse con los datos o el estado actual del ítem de cola.", Qg2ResponseShape.Untyped)
            ],
            ResponseSchemaDocumentation: new(
                "IncomingNachaManualActionResultDto",
                ["queueId", "action", "previousStatus", "currentStatus", "isIdempotentReplay", "message", "actionText"]),
            RequestSchema: "IncomingNachaManualActionRequest",
            RequestProperties: ["justification", "idempotencyKey", "priority"])
    ];

    private static readonly NachaLegacyQg2Operation[] NachaLegacyQg2Operations =
    [
        new(
            "GET",
            "/nacha-record-definitions",
            Parameters: [],
            Responses:
            [
                new("200", "Definiciones NACHA-M legacy disponibles para diagnóstico.", Qg2ResponseShape.Collection, "NachaRecordDefinitionDto")
            ],
            ResponseSchemaDocumentation: new(
                "NachaRecordDefinitionDto",
                ["id", "recordCode", "sequence", "sourceType", "sourceName", "filterKey", "isEnabled"])),
        new(
            "GET",
            "/nacha-record-definitions/{id}",
            Parameters: [new("id", "path")],
            Responses:
            [
                new("200", "Definición NACHA-M legacy encontrada.", Qg2ResponseShape.Schema, "NachaRecordDefinitionDto"),
                new("404", "No existe una definición NACHA-M legacy con el identificador indicado.", Qg2ResponseShape.NoBody)
            ],
            ResponseSchemaDocumentation: new(
                "NachaRecordDefinitionDto",
                ["id", "recordCode", "sequence", "sourceType", "sourceName", "filterKey", "isEnabled"])),
        new(
            "POST",
            "/nacha-record-definitions",
            Parameters: [],
            Responses:
            [
                new("410", "La creación legacy está retirada; la configuración oficial se administra mediante nacha-config profiles.", Qg2ResponseShape.Untyped)
            ],
            RequestSchema: "NachaRecordDefinitionDto",
            RequestProperties: ["id", "recordCode", "sequence", "sourceType", "sourceName", "filterKey", "isEnabled"]),
        new(
            "PUT",
            "/nacha-record-definitions/{id}",
            Parameters: [new("id", "path")],
            Responses:
            [
                new("410", "La actualización legacy está retirada; la configuración oficial se administra mediante nacha-config profiles.", Qg2ResponseShape.Untyped)
            ],
            RequestSchema: "NachaRecordDefinitionDto",
            RequestProperties: ["id", "recordCode", "sequence", "sourceType", "sourceName", "filterKey", "isEnabled"]),
        new(
            "DELETE",
            "/nacha-record-definitions/{id}",
            Parameters: [new("id", "path")],
            Responses:
            [
                new("410", "La eliminación legacy está retirada; la configuración oficial se administra mediante nacha-config profiles.", Qg2ResponseShape.Untyped)
            ]),
        new(
            "GET",
            "/nacha-layouts",
            Parameters: [],
            Responses:
            [
                new("200", "Layouts NACHA-M legacy disponibles para diagnóstico.", Qg2ResponseShape.Collection, "NachaRecordLayoutDto")
            ],
            ResponseSchemaDocumentation: new(
                "NachaRecordLayoutDto",
                ["id", "recordType", "recordCode", "totalLength", "description", "fields"])),
        new(
            "GET",
            "/nacha-layouts/{id}",
            Parameters: [new("id", "path")],
            Responses:
            [
                new("200", "Layout NACHA-M legacy encontrado.", Qg2ResponseShape.Schema, "NachaRecordLayoutDto"),
                new("404", "No existe un layout NACHA-M legacy con el identificador indicado.", Qg2ResponseShape.NoBody)
            ],
            ResponseSchemaDocumentation: new(
                "NachaRecordLayoutDto",
                ["id", "recordType", "recordCode", "totalLength", "description", "fields"])),
        new(
            "POST",
            "/nacha-layouts",
            Parameters: [],
            Responses:
            [
                new("410", "La creación legacy está retirada; la configuración oficial se administra mediante nacha-config profiles.", Qg2ResponseShape.Untyped)
            ],
            RequestSchema: "NachaRecordLayoutDto",
            RequestProperties: ["id", "recordType", "recordCode", "totalLength", "description", "fields"]),
        new(
            "PUT",
            "/nacha-layouts/{id}",
            Parameters: [new("id", "path")],
            Responses:
            [
                new("410", "La actualización legacy está retirada; la configuración oficial se administra mediante nacha-config profiles.", Qg2ResponseShape.Untyped)
            ],
            RequestSchema: "NachaRecordLayoutDto",
            RequestProperties: ["id", "recordType", "recordCode", "totalLength", "description", "fields"]),
        new(
            "DELETE",
            "/nacha-layouts/{id}",
            Parameters: [new("id", "path")],
            Responses:
            [
                new("410", "La eliminación legacy está retirada; la configuración oficial se administra mediante nacha-config profiles.", Qg2ResponseShape.Untyped)
            ])
    ];

    private static readonly AuthQg2Operation[] AuthQg2Operations =
    [
        new(
            "POST",
            "/Auth/login",
            RequiresBearer: false,
            RequestSchema: "LoginRequest",
            RequestProperties: ["username", "password"],
            Responses:
            [
                ("200", "Autenticación correcta. Data contiene el token JWT y la información de autorización del usuario."),
                ("401", "Las credenciales o el estado de la cuenta no permiten iniciar sesión."),
                ("500", "No fue posible registrar el intento de autenticación.")
            ]),
        new(
            "POST",
            "/Auth/forgot-password",
            RequiresBearer: false,
            RequestSchema: "ForgotPasswordRequest",
            RequestProperties: ["email"],
            Responses:
            [
                ("200", "Solicitud procesada con mensaje genérico sin revelar si el correo está registrado."),
                ("400", "El correo requerido no fue proporcionado.")
            ]),
        new(
            "POST",
            "/Auth/reset-password",
            RequiresBearer: false,
            RequestSchema: "ResetPasswordRequest",
            RequestProperties: ["token", "newPassword", "confirmPassword"],
            Responses:
            [
                ("200", "La contraseña fue actualizada correctamente."),
                ("400", "La solicitud de restablecimiento no es válida o el token no puede utilizarse.")
            ]),
        new(
            "POST",
            "/Auth/refresh",
            RequiresBearer: true,
            RequestSchema: null,
            RequestProperties: [],
            Responses:
            [
                ("200", "La sesión fue renovada y Data contiene un nuevo resultado de autenticación."),
                ("401", "La identidad de la sesión actual no es válida o el usuario no puede renovar la sesión.")
            ])
    ];

    private static readonly ChambersCyclesQg2Operation[] ChambersCyclesQg2Operations =
    [
        new(
            "GET",
            "/clearing-houses",
            PrimaryStatusCode: "200",
            Parameters:
            [
                new("Search", "query"),
                new("IsActive", "query"),
                new("Page", "query"),
                new("PageSize", "query")
            ],
            RequestSchema: null,
            RequestProperties: [],
            Responses:
            [
                new("200", "Listado paginado de cámaras compensadoras que cumplen los filtros solicitados.", Qg2ResponseShape.Paginated, "ClearingHouseDto")
            ]),
        new(
            "GET",
            "/clearing-houses/operational",
            PrimaryStatusCode: "200",
            Parameters: [],
            RequestSchema: null,
            RequestProperties: [],
            Responses:
            [
                new("200", "Colección de cámaras activas y listas para operar según su configuración actual.", Qg2ResponseShape.Collection, "ClearingHouseDto")
            ]),
        new(
            "GET",
            "/clearing-houses/payment-rail-options",
            PrimaryStatusCode: "200",
            Parameters: [],
            RequestSchema: null,
            RequestProperties: [],
            Responses:
            [
                new("200", "Opciones de riel de pago seleccionables para la configuración administrativa de una cámara.", Qg2ResponseShape.Collection, "ClearingHousePaymentRailOptionDto")
            ],
            ResponseSchemaDocumentation: new(
                "ClearingHousePaymentRailOptionDto",
                ["code", "name"])),
        new(
            "GET",
            "/clearing-houses/nacha-profiles",
            PrimaryStatusCode: "200",
            Parameters: [new("clearingHouseCode", "query", Required: false)],
            RequestSchema: null,
            RequestProperties: [],
            Responses:
            [
                new("200", "Perfiles NACHA-M publicados disponibles según el filtro opcional de cámara.", Qg2ResponseShape.Collection, "ClearingHouseNachaProfileOptionDto")
            ],
            ResponseSchemaDocumentation: new(
                "ClearingHouseNachaProfileOptionDto",
                ["id", "code", "name", "clearingHouseCode", "isPublished", "isCurrent"])),
        new(
            "GET",
            "/clearing-houses/{id}",
            PrimaryStatusCode: "200",
            Parameters: [new("id", "path")],
            RequestSchema: null,
            RequestProperties: [],
            Responses:
            [
                new("200", "Cámara compensadora encontrada.", Qg2ResponseShape.Schema, "ClearingHouseDto"),
                new("404", "No existe una cámara compensadora con el identificador indicado.", Qg2ResponseShape.ProblemDetails, "ProblemDetails")
            ]),
        new(
            "GET",
            "/clearing-houses/{id}/readiness",
            PrimaryStatusCode: "200",
            Parameters: [new("id", "path")],
            RequestSchema: null,
            RequestProperties: [],
            Responses:
            [
                new("200", "Resultado actual de readiness de la cámara, incluyendo los requisitos faltantes cuando existan.", Qg2ResponseShape.Schema, "ClearingHouseReadinessDto"),
                new("404", "No existe una cámara compensadora con el identificador indicado.", Qg2ResponseShape.ProblemDetails, "ProblemDetails")
            ]),
        new(
            "POST",
            "/clearing-houses",
            PrimaryStatusCode: "201",
            Parameters: [],
            RequestSchema: "CreateClearingHouseRequest",
            RequestProperties:
            [
                "code", "name", "originCode", "timeZoneId", "holidayStrategy",
                "paymentRailCode", "requiresNachaProfile", "nachaProfileId"
            ],
            Responses:
            [
                new("201", "Cámara compensadora creada en estado inactivo.", Qg2ResponseShape.Schema, "ClearingHouseDto"),
                new("400", "La configuración de la cámara es incompleta o inválida.", Qg2ResponseShape.ProblemDetails, "ProblemDetails"),
                new("409", "La creación entra en conflicto con una cámara o configuración existente.", Qg2ResponseShape.ProblemDetails, "ProblemDetails")
            ]),
        new(
            "PUT",
            "/clearing-houses/{id}",
            PrimaryStatusCode: "200",
            Parameters: [new("id", "path")],
            RequestSchema: "UpdateClearingHouseRequest",
            RequestProperties:
            [
                "code", "name", "originCode", "timeZoneId", "holidayStrategy",
                "paymentRailCode", "requiresNachaProfile", "nachaProfileId", "expectedUpdatedAt"
            ],
            Responses:
            [
                new("200", "Cámara compensadora actualizada.", Qg2ResponseShape.Schema, "ClearingHouseDto"),
                new("400", "La configuración solicitada es incompleta o inválida.", Qg2ResponseShape.ProblemDetails, "ProblemDetails"),
                new("404", "No existe una cámara compensadora con el identificador indicado.", Qg2ResponseShape.ProblemDetails, "ProblemDetails"),
                new("409", "La cámara cambió desde la versión esperada o el cambio solicitado entra en conflicto con su configuración o relaciones actuales.", Qg2ResponseShape.ProblemDetails, "ProblemDetails")
            ]),
        new(
            "PATCH",
            "/clearing-houses/{id}/status",
            PrimaryStatusCode: "200",
            Parameters: [new("id", "path")],
            RequestSchema: "ChangeClearingHouseStatusRequest",
            RequestProperties: ["isActive"],
            Responses:
            [
                new("200", "Estado activo de la cámara actualizado o ya coincidente con el solicitado.", Qg2ResponseShape.Schema, "ClearingHouseDto"),
                new("400", "La cámara no puede activarse porque no cumple los requisitos de readiness.", Qg2ResponseShape.ProblemDetails, "ProblemDetails"),
                new("404", "No existe una cámara compensadora con el identificador indicado.", Qg2ResponseShape.ProblemDetails, "ProblemDetails")
            ]),
        new(
            "GET",
            "/clearing-houses/{id}/cycles",
            PrimaryStatusCode: "200",
            Parameters: [new("id", "path"), new("processingDate", "query")],
            RequestSchema: null,
            RequestProperties: [],
            Responses:
            [
                new("200", "Colección de ciclos de la cámara que cumplen el filtro opcional de ProcessingDate.", Qg2ResponseShape.Collection, "AchCycleDto")
            ]),
        new(
            "GET",
            "/ach-cycles",
            PrimaryStatusCode: "200",
            Parameters:
            [
                new("clearingHouseId", "query"),
                new("startDate", "query"),
                new("endDate", "query"),
                new("processingDate", "query")
            ],
            RequestSchema: null,
            RequestProperties: [],
            Responses:
            [
                new("200", "Colección de ciclos que cumplen los filtros de cámara y ProcessingDate.", Qg2ResponseShape.Collection, "AchCycleDto")
            ]),
        new(
            "GET",
            "/ach-cycles/exportable",
            PrimaryStatusCode: "200",
            Parameters:
            [
                new("clearingHouseId", "query"),
                new("startDate", "query"),
                new("endDate", "query")
            ],
            RequestSchema: null,
            RequestProperties: [],
            Responses:
            [
                new("200", "Colección de ciclos con información de disponibilidad y artefactos de exportación.", Qg2ResponseShape.Collection, "AchCycleExportDto")
            ]),
        new(
            "GET",
            "/ach-cycles/{id}",
            PrimaryStatusCode: "200",
            Parameters: [new("id", "path")],
            RequestSchema: null,
            RequestProperties: [],
            Responses:
            [
                new("200", "Ciclo operativo encontrado.", Qg2ResponseShape.Schema, "AchCycleDto"),
                new("404", "No existe un ciclo con el identificador indicado.", Qg2ResponseShape.NoBody)
            ]),
        new(
            "POST",
            "/ach-cycles",
            PrimaryStatusCode: "201",
            Parameters: [],
            RequestSchema: "AchCycleRequest",
            RequestProperties:
            [
                "cycleName", "processingDate", "startTime", "endTime", "cutoffTime",
                "rescheduleOnHoliday", "clearingHouseId", "clearingHouseCycleConfigId"
            ],
            Responses:
            [
                new("201", "Ciclo creado con los valores finales de la configuración canónica resuelta.", Qg2ResponseShape.Schema, "AchCycleDto"),
                new("400", "No fue posible resolver una configuración canónica válida y no ambigua para el ciclo solicitado.", Qg2ResponseShape.ProblemDetails, "ProblemDetails")
            ]),
        new(
            "PUT",
            "/ach-cycles/{id}",
            PrimaryStatusCode: "200",
            Parameters: [new("id", "path")],
            RequestSchema: "AchCycleRequest",
            RequestProperties:
            [
                "cycleName", "processingDate", "startTime", "endTime", "cutoffTime",
                "rescheduleOnHoliday", "clearingHouseId", "clearingHouseCycleConfigId"
            ],
            Responses:
            [
                new("200", "Ciclo actualizado con los valores de la configuración canónica resuelta.", Qg2ResponseShape.Schema, "AchCycleDto"),
                new("400", "La configuración solicitada es inválida, ambigua o no puede cambiarse para el ciclo actual.", Qg2ResponseShape.ProblemDetails, "ProblemDetails"),
                new("404", "No existe un ciclo con el identificador indicado.", Qg2ResponseShape.ProblemDetails, "ProblemDetails")
            ]),
        new(
            "POST",
            "/ach-cycles/repair-configuration-links",
            PrimaryStatusCode: "200",
            Parameters: [],
            RequestSchema: null,
            RequestProperties: [],
            Responses:
            [
                new("200", "Reparación completada; el resultado informa ciclos inspeccionados, reparados y no emparejados.", Qg2ResponseShape.Schema, "AchCycleConfigurationLinkRepairResult"),
                new("409", "La reparación se bloqueó por asociaciones ambiguas y no se modificó ningún ciclo.", Qg2ResponseShape.ProblemDetails, "ProblemDetails")
            ]),
        new(
            "DELETE",
            "/ach-cycles/{id}",
            PrimaryStatusCode: "204",
            Parameters: [new("id", "path")],
            RequestSchema: null,
            RequestProperties: [],
            Responses:
            [
                new("204", "Ciclo eliminado correctamente; la respuesta no contiene cuerpo.", Qg2ResponseShape.NoBody)
            ])
    ];

    private static readonly TransactionsQg2Operation[] TransactionsQg2Operations =
    [
        new(
            "GET",
            "/Transactions",
            PrimaryStatusCode: "200",
            Parameters:
            [
                new("achCycleId", "query"),
                new("achCycleName", "query"),
                new("effectiveDate", "query"),
                new("clearingHouseId", "query")
            ],
            RequestSchema: null,
            RequestProperties: [],
            Responses:
            [
                new("200", "Colección de transacciones que cumplen los filtros solicitados.", Qg2ResponseShape.Collection, "AchTransactionListDto")
            ],
            ResponseSchemaDocumentation: new(
                "AchTransactionListDto",
                ["id", "amount", "transactionExternalId", "reference", "type", "traceNumber", "effectiveEntryDate", "createdAt", "sourceAccountNumber", "destinationAccountNumber", "achCycleId", "achCycleName", "clearingHouseName"])),
        new(
            "GET",
            "/Transactions/company-entry-descriptions",
            PrimaryStatusCode: "200",
            Parameters: [],
            RequestSchema: null,
            RequestProperties: [],
            Responses:
            [
                new("200", "Catálogo activo de Company Entry Descriptions configurado en ACHInterbank.", Qg2ResponseShape.Collection, "CompanyEntryDescriptionDto")
            ],
            ResponseSchemaDocumentation: new(
                "CompanyEntryDescriptionDto",
                ["id", "term", "description", "standardEntryClassCode"])),
        new(
            "GET",
            "/Transactions/policies/preview",
            PrimaryStatusCode: "200",
            Parameters:
            [
                new("amount", "query"),
                new("transactionExternalId", "query"),
                new("reference", "query"),
                new("type", "query"),
                new("accountType", "query"),
                new("isPrenotification", "query"),
                new("destinationInstitutionId", "query"),
                new("sourceAccountNumber", "query"),
                new("destinationAccountNumber", "query"),
                new("companyIdentification", "query"),
                new("recipientIdNumber", "query")
            ],
            RequestSchema: null,
            RequestProperties: [],
            Responses:
            [
                new("200", "Resultado de la evaluación previa de política para los datos suministrados.", Qg2ResponseShape.Schema, "TransactionPolicyPreview")
            ]),
        new(
            "POST",
            "/Transactions",
            PrimaryStatusCode: "201",
            Parameters: [],
            RequestSchema: "AchTransactionRequest",
            RequestProperties:
            [
                "amount", "transactionExternalId", "reference", "type", "accountType", "isPrenotification",
                "destinationInstitutionId", "sourceAccountNumber", "destinationAccountNumber", "recipientIdNumber",
                "recipientName", "requiresIdentityValidation", "companyName", "companyIdentification",
                "companyEntryDescriptionId", "sourcePersonType", "recipientPersonType", "addendas"
            ],
            Responses:
            [
                new("201", "Transacción registrada correctamente.", Qg2ResponseShape.Schema, "AchTransaction"),
                new("400", "La solicitud incumple una validación de entrada o una regla de negocio capturada por la acción.", Qg2ResponseShape.Untyped),
                new("500", "La acción capturó un error interno inesperado durante el registro de la transacción.", Qg2ResponseShape.Untyped)
            ],
            ResponseSchemaDocumentation: new(
                "AchTransaction",
                ["id", "amount", "transactionExternalId", "reference", "type", "transactionCode", "effectiveEntryDate", "isPrenotification", "state", "sourceAccountNumber", "destinationAccountNumber", "destinationInstitutionId", "achCycleId", "achBatchId", "addendas"])),
        new(
            "GET",
            "/Transactions/{id}",
            PrimaryStatusCode: "200",
            Parameters: [new("id", "path")],
            RequestSchema: null,
            RequestProperties: [],
            Responses:
            [
                new("200", "Transacción encontrada.", Qg2ResponseShape.Schema, "AchTransaction"),
                new("404", "No existe una transacción con el identificador indicado.", Qg2ResponseShape.Untyped)
            ]),
        new(
            "POST",
            "/Transactions/bulk/submit",
            PrimaryStatusCode: "200",
            Parameters: [],
            RequestSchema: "BulkIngestionRequest",
            RequestProperties:
            [
                "batchReference", "sourceType", "processingMode", "chunkSize", "transactions",
                "fileName", "contentType", "contentBase64", "clientRequestId", "retryCount"
            ],
            Responses:
            [
                new("200", "Resultado de la modalidad de ingestión solicitada; en modo síncrono puede incluir el resultado inmediato del registro masivo.", Qg2ResponseShape.Schema, "BulkIngestionResponse"),
                new("400", "La solicitud incumple una validación global o usa un origen no soportado por el modo síncrono actual.", Qg2ResponseShape.Untyped),
                new("500", "La acción capturó un error interno inesperado durante la ingestión masiva.", Qg2ResponseShape.Untyped)
            ],
            ResponseSchemaDocumentation: new(
                "BulkIngestionResponse",
                ["processingMode", "jobId", "status", "immediateResult"])),
        new(
            "POST",
            "/Transactions/bulk",
            PrimaryStatusCode: "200",
            Parameters: [],
            RequestSchema: "BulkAchTransactionRequest",
            RequestProperties: ["batchReference", "transactions", "chunkSize"],
            Responses:
            [
                new("200", "Resultado agregado del lote con totales, transacciones creadas y resultado individual de cada ítem procesado.", Qg2ResponseShape.Schema, "BulkAchTransactionResponse"),
                new("400", "La solicitud incumple una validación global del lote, como referencia o colección de transacciones inválida.", Qg2ResponseShape.Untyped),
                new("500", "La acción capturó un error interno inesperado durante el registro masivo.", Qg2ResponseShape.Untyped)
            ],
            ResponseSchemaDocumentation: new(
                "BulkAchTransactionResponse",
                ["batchReference", "totalReceived", "totalProcessed", "totalSucceeded", "totalFailed", "createdTransactionIds", "itemResults"])),
        new(
            "GET",
            "/Transactions/{id}/integration-result",
            PrimaryStatusCode: "200",
            Parameters: [new("id", "path")],
            RequestSchema: null,
            RequestProperties: [],
            Responses:
            [
                new("200", "Resultado de integración de la transacción, con el último intento y su historial cuando existan registros.", Qg2ResponseShape.Schema, "TransactionIntegrationResultDto"),
                new("404", "No existe una transacción con el identificador indicado.", Qg2ResponseShape.Untyped),
                new("503", "El servicio de consulta de resultados de integración no está disponible.", Qg2ResponseShape.NoBody)
            ]),
        new(
            "GET",
            "/Transactions/{id}/integration-readiness",
            PrimaryStatusCode: "200",
            Parameters: [new("id", "path")],
            RequestSchema: null,
            RequestProperties: [],
            Responses:
            [
                new("200", "Evaluación de readiness técnico de la integración correspondiente a la transacción.", Qg2ResponseShape.Schema, "TransactionIntegrationReadinessResult"),
                new("404", "No existe una transacción con el identificador indicado.", Qg2ResponseShape.Untyped),
                new("503", "El servicio de readiness de integración no está disponible.", Qg2ResponseShape.Untyped)
            ])
    ];

    private static readonly BulkIngestionQg2Operation[] BulkIngestionQg2Operations =
    [
        new(
            "POST",
            "/api/transactions/bulk-ingestion/upload",
            PrimaryStatusCode: "200",
            Parameters: [],
            RequestSchema: "BulkFileUploadForm",
            RequestContentType: "multipart/form-data",
            RequestProperties: ["file", "batchReference", "clientRequestId"],
            RequiredRequestProperties: ["file"],
            Responses:
            [
                new("200", "Lote registrado o lote existente devuelto por coincidencia de ClientRequestId y hash del archivo.", Qg2ResponseShape.Schema, "BulkFileUploadResponse"),
                new("400", "El archivo falta, está vacío, supera el tamaño admitido, usa un formato no soportado o produce una validación estructural capturada.", Qg2ResponseShape.Untyped),
                new("500", "La acción capturó un error interno inesperado al registrar el lote.", Qg2ResponseShape.Untyped)
            ],
            ResponseSchemaDocumentation: new(
                "BulkFileUploadResponse",
                ["batchId", "jobId", "batchReference", "status", "fileType", "totalRecordsDetected", "totalStructuralErrors", "totalReadyForProcessing", "errorSummary"])),
        new(
            "GET",
            "/api/transactions/bulk-ingestion/{batchId}",
            PrimaryStatusCode: "200",
            Parameters: [new("batchId", "path")],
            RequestSchema: null,
            RequestContentType: null,
            RequestProperties: [],
            RequiredRequestProperties: [],
            Responses:
            [
                new("200", "Estado consolidado actual del lote.", Qg2ResponseShape.Schema, "BulkBatchStatusDto"),
                new("404", "No existe un lote con el identificador indicado.", Qg2ResponseShape.Untyped)
            ],
            ResponseSchemaDocumentation: new(
                "BulkBatchStatusDto",
                ["batchId", "batchReference", "status", "totalRecords", "totalValid", "totalInvalid", "totalProcessed", "totalSucceeded", "totalFailed", "progressPercent", "uploadedAtUtc", "processingStartedAtUtc", "processingFinishedAtUtc", "retryCount", "lastJobId", "lastJobMessage", "errorSummary"])),
        new(
            "GET",
            "/api/transactions/bulk-ingestion/{batchId}/items",
            PrimaryStatusCode: "200",
            Parameters: [new("batchId", "path"), new("page", "query"), new("pageSize", "query"), new("status", "query")],
            RequestSchema: null,
            RequestContentType: null,
            RequestProperties: [],
            RequiredRequestProperties: [],
            Responses:
            [
                new("200", "Página de ítems del lote después de aplicar la normalización de paginación y el filtro opcional por estado.", Qg2ResponseShape.Schema, "BulkBatchItemsPageDto"),
                new("404", "No existe un lote con el identificador indicado.", Qg2ResponseShape.Untyped)
            ],
            ResponseSchemaDocumentation: new(
                "BulkBatchItemsPageDto",
                ["page", "pageSize", "total", "items"])),
        new(
            "GET",
            "/api/transactions/bulk-ingestion/{batchId}/summary",
            PrimaryStatusCode: "200",
            Parameters: [new("batchId", "path")],
            RequestSchema: null,
            RequestContentType: null,
            RequestProperties: [],
            RequiredRequestProperties: [],
            Responses:
            [
                new("200", "Resumen del lote con estado consolidado e historial de intentos de procesamiento.", Qg2ResponseShape.Schema, "BulkBatchProcessingSummaryDto"),
                new("404", "No existe un lote con el identificador indicado.", Qg2ResponseShape.Untyped)
            ],
            ResponseSchemaDocumentation: new(
                "BulkBatchProcessingSummaryDto",
                ["batchId", "status", "attempts"])),
        new(
            "POST",
            "/api/transactions/bulk-ingestion/{batchId}/retry",
            PrimaryStatusCode: "200",
            Parameters: [new("batchId", "path")],
            RequestSchema: "RetryBatchRequest",
            RequestContentType: "application/json",
            RequestProperties: ["scope"],
            RequiredRequestProperties: [],
            Responses:
            [
                new("200", "Reintento creado y procesamiento despachado para los ítems elegibles.", Qg2ResponseShape.Schema, "RetryBatchResponse"),
                new("400", "El lote no es reintentable en su estado actual, alcanzó el máximo de 3 reintentos o no tiene ítems elegibles para el alcance seleccionado.", Qg2ResponseShape.Untyped),
                new("404", "No existe un lote con el identificador indicado.", Qg2ResponseShape.Untyped)
            ],
            ResponseSchemaDocumentation: new(
                "RetryBatchResponse",
                ["batchId", "attemptId", "attemptNumber", "jobId", "status"])),
        new(
            "POST",
            "/api/transactions/bulk-ingestion/{batchId}/cancel",
            PrimaryStatusCode: "200",
            Parameters: [new("batchId", "path")],
            RequestSchema: null,
            RequestContentType: null,
            RequestProperties: [],
            RequiredRequestProperties: [],
            Responses:
            [
                new("200", "La cancelación fue aceptada y el servicio marcó el lote como Cancelled.", Qg2ResponseShape.Untyped),
                new("400", "El lote existe, pero su estado actual no permite cancelarlo.", Qg2ResponseShape.Untyped),
                new("404", "No existe un lote con el identificador indicado.", Qg2ResponseShape.Untyped)
            ])
    ];

    private static readonly TraceabilityQg2Operation[] TraceabilityQg2Operations =
    [
        new(
            "POST",
            "/api/ach-traceability/sol02/{transactionId}/certify",
            PrimaryStatusCode: "200",
            Parameters: [new("transactionId", "path")],
            RequestSchema: "Sol02CertificationRequest",
            RequestProperties: ["certificationReference", "notes"],
            Responses:
            [
                new("200", "Certificación SOL02 aplicada; la respuesta informa el identificador, estado y fecha del cambio de estado.", Qg2ResponseShape.Untyped),
                new("400", "La transición de la transacción al estado Certified no está permitida desde su estado actual.", Qg2ResponseShape.Untyped),
                new("404", "No existe una transacción con el identificador indicado.", Qg2ResponseShape.Untyped)
            ]),
        new(
            "GET",
            "/api/ach-traceability/transactions/{transactionId}",
            PrimaryStatusCode: "200",
            Parameters: [new("transactionId", "path")],
            RequestSchema: null,
            RequestProperties: [],
            Responses:
            [
                new("200", "Detalle de trazabilidad de la transacción y su historial de eventos de estado.", Qg2ResponseShape.Schema, "AchTraceabilityDetailDto"),
                new("404", "No existe una transacción con el identificador indicado.", Qg2ResponseShape.Untyped)
            ],
            ResponseSchemaDocumentation: new(
                "AchTraceabilityDetailDto",
                [
                    "transactionId", "transactionExternalId", "reference", "traceNumber", "originalTraceRef",
                    "transactionCode", "amount", "effectiveEntryDate", "achCycleId", "achCycleName",
                    "clearingHouseName", "clearingHouseCode", "currentNachaFileName", "currentNachaGeneratedAtUtc",
                    "returnFileName", "returnCycleId", "returnOriginalTransactionId", "returnGeneratedAtUtc",
                    "sourceInstitutionName", "destinationInstitutionName", "state", "stateChangedAtUtc",
                    "slaDeadlineAtUtc", "returnReasonCode", "events"
                ])),
        new(
            "GET",
            "/api/ach-traceability/report",
            PrimaryStatusCode: "200",
            Parameters:
            [
                new("fromUtc", "query"),
                new("toUtc", "query"),
                new("state", "query"),
                new("achCycleId", "query")
            ],
            RequestSchema: null,
            RequestProperties: [],
            Responses:
            [
                new("200", "Filas de trazabilidad que cumplen los filtros solicitados, ordenadas por el cambio de estado más reciente.", Qg2ResponseShape.Collection, "AchTraceabilityReportRowDto")
            ],
            ResponseSchemaDocumentation: new(
                "AchTraceabilityReportRowDto",
                [
                    "transactionId", "transactionExternalId", "reference", "traceNumber", "transactionCode",
                    "amount", "achCycleId", "achCycleName", "clearingHouseName", "clearingHouseCode",
                    "currentNachaFileName", "effectiveEntryDate", "state", "stateChangedAtUtc", "returnReasonCode",
                    "eventsCount", "sourceInstitutionName", "destinationInstitutionName"
                ]))
    ];

    private static readonly ReturnsQg2Operation[] ReturnsQg2Operations =
    [
        new(
            "GET",
            "/ach-returns/cycles/{cycleId}/transactions",
            PrimaryStatusCode: "200",
            Parameters: [new("cycleId", "path")],
            RequestSchema: null,
            RequestProperties: [],
            Responses:
            [
                new("200", "Transacciones del ciclo con el indicador de elegibilidad usado por la consulta de devoluciones.", Qg2ResponseShape.Collection, "ReturnEligibleTransactionDto")
            ],
            ResponseSchemaDocumentation: new(
                "ReturnEligibleTransactionDto",
                [
                    "id", "traceNumber", "amount", "transactionCode", "reference", "sourceAccountNumber",
                    "destinationAccountNumber", "originatingDfi", "receivingDfi", "achCycleId",
                    "effectiveEntryDate", "isPrenotification", "isEligible", "validationMessage"
                ])),
        new(
            "POST",
            "/ach-returns/generate-file",
            PrimaryStatusCode: "200",
            Parameters: [],
            RequestSchema: "GenerateReturnsFileRequest",
            RequestProperties: ["cycleId", "items", "returnCycleId"],
            Responses:
            [
                new("200", "Archivo de devoluciones generado y devuelto como descarga.", Qg2ResponseShape.Untyped),
                new("409", "Una o más transacciones seleccionadas ya tienen una devolución generada.", Qg2ResponseShape.ProblemDetails, "ProblemDetails")
            ]),
        new(
            "GET",
            "/return-reasons",
            PrimaryStatusCode: "200",
            Parameters: [new("onlyForReturn", "query", Required: false)],
            RequestSchema: null,
            RequestProperties: [],
            Responses:
            [
                new("200", "Causales de devolución que cumplen el filtro solicitado, ordenadas por código.", Qg2ResponseShape.Collection, "ReturnReasonDto")
            ],
            ResponseSchemaDocumentation: new(
                "ReturnReasonDto",
                ["id", "code", "description", "category", "isForReturn"])),
        new(
            "POST",
            "/ach-returns/return-of-return/cenit/out",
            PrimaryStatusCode: "200",
            Parameters: [],
            RequestSchema: "CreateCenitReturnOfReturnOutRequest",
            RequestProperties: ["parentIncomingReturnStateEventId", "reasonCode", "returnCycleId", "requestedBy", "source"],
            Responses:
            [
                new("200", "Devolución de devolución saliente creada con resultado exitoso.", Qg2ResponseShape.Schema, "CenitReturnOfReturnResult"),
                new("400", "Faltan datos obligatorios para solicitar la devolución de devolución.", Qg2ResponseShape.Untyped),
                new("409", "El servicio CENIT de devolución de devolución no está disponible o la creación no terminó exitosamente.", Qg2ResponseShape.Untyped)
            ],
            ResponseSchemaDocumentation: new(
                "CenitReturnOfReturnResult",
                ["isSuccessful", "wasDuplicate", "flowId", "returnOfReturnTransactionId", "code", "message"])),
        new(
            "POST",
            "/ach-returns/return-of-return/evaluate",
            PrimaryStatusCode: "200",
            Parameters: [],
            RequestSchema: "EvaluateReturnOfReturnRequest",
            RequestProperties: ["sourceReturnTransactionId", "newReturnReasonCode", "requestedBy", "source"],
            Responses:
            [
                new("200", "Resultado de elegibilidad de la devolución de devolución, incluyendo fallas funcionales cuando no es elegible.", Qg2ResponseShape.Schema, "AchReturnOfReturnEligibilityResult"),
                new("400", "La solicitud no contiene una transacción origen válida o una nueva causal.", Qg2ResponseShape.Untyped)
            ],
            ResponseSchemaDocumentation: new(
                "AchReturnOfReturnEligibilityResult",
                ["isEligible", "clearingHouseId", "sourceReturnTransactionId", "originalReturnReasonCode", "newReturnReasonCode", "isUniquePerTransaction", "failures"])),
        new(
            "POST",
            "/ach-returns/return-of-return/generate-audit-file",
            PrimaryStatusCode: "200",
            Parameters: [],
            RequestSchema: "GenerateReturnOfReturnAuditFileRequest",
            RequestProperties: ["flowIds", "requestedBy", "source"],
            Responses:
            [
                new("200", "Archivo interno de auditoría generado y devuelto como descarga text/plain.", Qg2ResponseShape.Untyped),
                new("400", "La solicitud no contiene flowIds para generar el archivo.", Qg2ResponseShape.Untyped),
                new("409", "El servicio no pudo generar un archivo de auditoría utilizable para los flujos solicitados.", Qg2ResponseShape.Untyped)
            ]),
        new(
            "POST",
            "/ach-returns/return-of-return/generate-nacha-file",
            PrimaryStatusCode: "200",
            Parameters: [],
            RequestSchema: "GenerateReturnOfReturnAuditFileRequest",
            RequestProperties: ["flowIds", "requestedBy", "source"],
            Responses:
            [
                new("200", "Archivo NACHA de devolución de devolución generado y devuelto como descarga text/plain.", Qg2ResponseShape.Untyped),
                new("400", "La solicitud no contiene flowIds para generar el archivo.", Qg2ResponseShape.Untyped),
                new("409", "El servicio no pudo generar un archivo NACHA utilizable para los flujos solicitados.", Qg2ResponseShape.Untyped)
            ]),
        new(
            "POST",
            "/api/ach/responses/process",
            PrimaryStatusCode: "200",
            Parameters: [],
            RequestSchema: "ProcesarRespuestaAchRequest",
            RequestProperties: ["tipoRespuesta", "idTransaccion", "codigoCamaraCompensacion", "codigoEntidadOrigen", "codigoEntidadDestino", "codigoEstadoExterno", "codigoCausalExterna", "descripcionCausalExterna", "idCanal", "nombreCanal", "idTransaccionServicioExterno", "fechaRecepcion", "correlationId"],
            Responses:
            [
                new("200", "Respuesta ACH procesada; el cuerpo informa el estado y los indicadores obtenidos por el caso de uso.", Qg2ResponseShape.Schema, "ProcesarRespuestaAchResponse"),
                new("400", "La solicitud no cumple las validaciones estructurales del controller.", Qg2ResponseShape.ValidationProblemDetails),
                new("422", "El caso de uso devolvió la respuesta como no procesada y expone el resultado funcional en el mismo contrato de respuesta.", Qg2ResponseShape.Schema, "ProcesarRespuestaAchResponse")
            ],
            ResponseSchemaDocumentation: new(
                "ProcesarRespuestaAchResponse",
                ["achResponseId", "procesada", "duplicada", "existeHomologacion", "permiteNotificacion", "intentoPendienteCreado", "estadoProcesamiento", "motivo", "hashIdempotencia"])),
        new(
            "POST",
            "/api/ach/responses/notifications/send",
            PrimaryStatusCode: "200",
            Parameters: [],
            RequestSchema: "NotificarRespuestaAchRequest",
            RequestProperties: ["notificationAttemptId", "correlationId"],
            Responses:
            [
                new("200", "Intento de notificación encontrado y procesado; el cuerpo refleja su resultado funcional o técnico.", Qg2ResponseShape.Schema, "NotificarRespuestaAchResponse"),
                new("400", "El identificador del intento de notificación no es válido.", Qg2ResponseShape.ValidationProblemDetails),
                new("404", "No existe el intento de notificación indicado; la respuesta no contiene cuerpo.", Qg2ResponseShape.NoBody)
            ],
            ResponseSchemaDocumentation: new(
                "NotificarRespuestaAchResponse",
                ["procesada", "encontrada", "yaProcesada", "existeError", "errorTecnico", "estadoNotificacion", "estadoProcesamiento", "codigoError", "descripcionError", "errorTecnicoDetalle", "motivo"])),
        new(
            "GET",
            "/api/ach/responses",
            PrimaryStatusCode: "200",
            Parameters:
            [
                new("FechaDesde", "query", Required: false), new("FechaHasta", "query", Required: false),
                new("TipoRespuesta", "query", Required: false), new("IdTransaccion", "query", Required: false),
                new("CodigoCamaraCompensacion", "query", Required: false), new("CodigoEntidadOrigen", "query", Required: false),
                new("CodigoEntidadDestino", "query", Required: false), new("CodigoEstadoExterno", "query", Required: false),
                new("EstadoProcesamiento", "query", Required: false), new("CorrelationId", "query", Required: false),
                new("PageNumber", "query"), new("PageSize", "query")
            ],
            RequestSchema: null,
            RequestProperties: [],
            Responses:
            [
                new("200", "Página de respuestas ACH que cumplen los filtros solicitados.", Qg2ResponseShape.AchResponsePaged, "AchResponseListItemResponse")
            ],
            ResponseSchemaDocumentation: new(
                "AchResponseListItemResponse",
                ["id", "tipoRespuesta", "idTransaccion", "codigoCamaraCompensacion", "codigoEntidadOrigen", "codigoEntidadDestino", "codigoEstadoExterno", "codigoCausalExterna", "estadoInternoNombre", "estadoProcesamiento", "permiteNotificacion", "correlationId", "fechaRecepcion", "fechaCreacion"])),
        new(
            "GET",
            "/api/ach/responses/dashboard",
            PrimaryStatusCode: "200",
            Parameters: [new("FechaDesde", "query", Required: false), new("FechaHasta", "query", Required: false), new("TipoRespuesta", "query", Required: false)],
            RequestSchema: null,
            RequestProperties: [],
            Responses:
            [
                new("200", "Contadores agregados de respuestas ACH para los filtros solicitados.", Qg2ResponseShape.Schema, "AchResponseDashboardResponse"),
                new("400", "TipoRespuesta tiene un valor distinto de Prenota o Transaccion.", Qg2ResponseShape.ProblemDetails, "ProblemDetails")
            ],
            ResponseSchemaDocumentation: new(
                "AchResponseDashboardResponse",
                ["totalRespuestas", "recibidas", "homologadas", "notificadas", "noHomologadas", "revisionManual", "pendientesReintento", "erroresFuncionales", "duplicadas"])),
        new(
            "GET",
            "/api/ach/responses/{id}",
            PrimaryStatusCode: "200",
            Parameters: [new("id", "path")],
            RequestSchema: null,
            RequestProperties: [],
            Responses:
            [
                new("200", "Detalle de la respuesta ACH encontrada, incluidos sus intentos de notificación.", Qg2ResponseShape.Schema, "AchResponseDetailResponse"),
                new("404", "No existe una respuesta ACH con el identificador indicado; la respuesta no contiene cuerpo.", Qg2ResponseShape.NoBody)
            ],
            ResponseSchemaDocumentation: new(
                "AchResponseDetailResponse",
                ["id", "tipoRespuesta", "idTransaccion", "codigoCamaraCompensacion", "codigoEntidadOrigen", "codigoEntidadDestino", "codigoEstadoExterno", "codigoCausalExterna", "idEstadoInterno", "idEstadoServicioExterno", "estadoInternoNombre", "causalNormalizada", "descripcionCausal", "idTransaccionServicioExterno", "hashIdempotencia", "estadoProcesamiento", "motivoNoHomologacion", "permiteNotificacion", "correlationId", "fechaRecepcion", "fechaCreacion", "fechaActualizacion", "notificationAttempts", "clearingHouseId", "appliedMappingId", "duplicateReceiptCount", "version"])),
        new(
            "GET",
            "/api/ach/responses/{id}/notification-attempts",
            PrimaryStatusCode: "200",
            Parameters: [new("id", "path")],
            RequestSchema: null,
            RequestProperties: [],
            Responses:
            [
                new("200", "Intentos de notificación asociados a la respuesta, ordenados por número de intento.", Qg2ResponseShape.Collection, "AchResponseNotificationAttemptResponse")
            ],
            ResponseSchemaDocumentation: new(
                "AchResponseNotificationAttemptResponse",
                ["id", "achResponseId", "numeroIntento", "estadoNotificacion", "idCanal", "nombreCanal", "idTransaccion", "idEstado", "causal", "idTransaccionServicioExterno", "descripcionCausal", "existeError", "codigoError", "descripcionError", "errorTecnico", "fechaCreacion", "fechaEnvio"])),
        new(
            "GET",
            "/api/ach/response-status-mappings",
            PrimaryStatusCode: "200",
            Parameters: [new("codigoCamaraCompensacion", "query", Required: false), new("tipoRespuesta", "query", Required: false), new("activo", "query", Required: false)],
            RequestSchema: null,
            RequestProperties: [],
            Responses:
            [
                new("200", "Mappings de estado que cumplen los filtros solicitados.", Qg2ResponseShape.Collection, "AchResponseStatusMappingResponse"),
                new("400", "TipoRespuesta tiene un valor distinto de Prenota o Transaccion.", Qg2ResponseShape.ProblemDetails, "ProblemDetails")
            ],
            ResponseSchemaDocumentation: new(
                "AchResponseStatusMappingResponse",
                ["id", "codigoCamaraCompensacion", "tipoRespuesta", "codigoEstadoExterno", "codigoCausalExterna", "idEstadoInterno", "idEstadoServicioExterno", "estadoInternoNombre", "causalNormalizada", "descripcionCausalNormalizada", "requiereCausal", "permiteNotificacion", "activo", "fechaInicioVigencia", "fechaFinVigencia", "clearingHouseId", "priority", "version"]))
    ];

    private static readonly ResponseOperationsQg2Operation[] ResponseOperationsQg2Operations =
    [
        new(
            "GET",
            "/api/ach/response-status-mappings/{id}",
            PrimaryStatusCode: "200",
            Parameters: [new("id", "path")],
            RequestSchema: null,
            RequestProperties: [],
            HasCorrelationHeader: false,
            Responses:
            [
                new("200", "Mapping de estado ACH encontrado para el identificador solicitado.", Qg2ResponseShape.Schema, "AchResponseMappingModel"),
                new("404", "No existe un mapping de estado ACH con el identificador indicado.", Qg2ResponseShape.ProblemDetails, "ProblemDetails")
            ],
            ResponseSchemaDocumentation: new(
                "AchResponseMappingModel",
                ["id", "clearingHouseId", "clearingHouseCode", "responseType", "externalCode", "externalCause", "internalStatusId", "externalServiceStatusId", "internalStatusName", "normalizedCause", "normalizedDescription", "requiresCause", "allowsNotification", "priority", "effectiveFrom", "effectiveTo", "isActive", "version"])),
        new(
            "POST",
            "/api/ach/response-status-mappings",
            PrimaryStatusCode: "201",
            Parameters: [],
            RequestSchema: "AchResponseMappingWriteRequest",
            RequestProperties: ["clearingHouseId", "responseType", "externalCode", "externalCause", "internalStatusId", "externalServiceStatusId", "internalStatusName", "normalizedCause", "normalizedDescription", "requiresCause", "allowsNotification", "priority", "effectiveFrom", "effectiveTo", "isActive", "expectedVersion", "reason"],
            HasCorrelationHeader: true,
            Responses:
            [
                new("201", "Mapping de estado ACH creado y ubicación canónica devuelta mediante CreatedAtAction.", Qg2ResponseShape.Schema, "AchResponseMappingModel"),
                new("400", "El mapping, la cámara, el tipo de respuesta, la vigencia o el contexto de auditoría no cumplen las validaciones del servicio.", Qg2ResponseShape.ProblemDetails, "ProblemDetails"),
                new("409", "La creación entra en conflicto con un mapping activo superpuesto o con una operación concurrente persistida.", Qg2ResponseShape.ProblemDetails, "ProblemDetails")
            ],
            ResponseSchemaDocumentation: new(
                "AchResponseMappingModel",
                ["id", "clearingHouseId", "clearingHouseCode", "responseType", "externalCode", "externalCause", "internalStatusId", "externalServiceStatusId", "internalStatusName", "normalizedCause", "normalizedDescription", "requiresCause", "allowsNotification", "priority", "effectiveFrom", "effectiveTo", "isActive", "version"])),
        new(
            "PUT",
            "/api/ach/response-status-mappings/{id}",
            PrimaryStatusCode: "200",
            Parameters: [new("id", "path")],
            RequestSchema: "AchResponseMappingWriteRequest",
            RequestProperties: ["clearingHouseId", "responseType", "externalCode", "externalCause", "internalStatusId", "externalServiceStatusId", "internalStatusName", "normalizedCause", "normalizedDescription", "requiresCause", "allowsNotification", "priority", "effectiveFrom", "effectiveTo", "isActive", "expectedVersion", "reason"],
            HasCorrelationHeader: true,
            Responses:
            [
                new("200", "Mapping de estado ACH actualizado con la configuración solicitada.", Qg2ResponseShape.Schema, "AchResponseMappingModel"),
                new("400", "El mapping, la cámara, el tipo de respuesta, la vigencia o el contexto de auditoría no cumplen las validaciones del servicio.", Qg2ResponseShape.ProblemDetails, "ProblemDetails"),
                new("404", "No existe el mapping de estado ACH que se intenta actualizar.", Qg2ResponseShape.ProblemDetails, "ProblemDetails"),
                new("409", "ExpectedVersion no coincide, existe un solapamiento activo o la persistencia detecta concurrencia.", Qg2ResponseShape.ProblemDetails, "ProblemDetails")
            ],
            ResponseSchemaDocumentation: new(
                "AchResponseMappingModel",
                ["id", "clearingHouseId", "clearingHouseCode", "responseType", "externalCode", "externalCause", "internalStatusId", "externalServiceStatusId", "internalStatusName", "normalizedCause", "normalizedDescription", "requiresCause", "allowsNotification", "priority", "effectiveFrom", "effectiveTo", "isActive", "version"])),
        new(
            "POST",
            "/api/ach/response-status-mappings/{id}/activate",
            PrimaryStatusCode: "200",
            Parameters: [new("id", "path")],
            RequestSchema: "VersionedReasonRequest",
            RequestProperties: ["expectedVersion", "reason", "correlationId"],
            HasCorrelationHeader: true,
            Responses:
            [
                new("200", "Mapping de estado ACH activo; incluye el estado persistido y su versión vigente.", Qg2ResponseShape.Schema, "AchResponseMappingModel"),
                new("400", "El motivo o el contexto requerido para auditar la transición no es válido.", Qg2ResponseShape.ProblemDetails, "ProblemDetails"),
                new("404", "No existe el mapping de estado ACH que se intenta activar.", Qg2ResponseShape.ProblemDetails, "ProblemDetails"),
                new("409", "ExpectedVersion no coincide, la activación produciría un solapamiento activo o existe concurrencia de persistencia.", Qg2ResponseShape.ProblemDetails, "ProblemDetails")
            ],
            ResponseSchemaDocumentation: new(
                "AchResponseMappingModel",
                ["id", "clearingHouseId", "clearingHouseCode", "responseType", "externalCode", "externalCause", "internalStatusId", "externalServiceStatusId", "internalStatusName", "normalizedCause", "normalizedDescription", "requiresCause", "allowsNotification", "priority", "effectiveFrom", "effectiveTo", "isActive", "version"])),
        new(
            "POST",
            "/api/ach/response-status-mappings/{id}/deactivate",
            PrimaryStatusCode: "200",
            Parameters: [new("id", "path")],
            RequestSchema: "VersionedReasonRequest",
            RequestProperties: ["expectedVersion", "reason", "correlationId"],
            HasCorrelationHeader: true,
            Responses:
            [
                new("200", "Mapping de estado ACH inactivo; incluye el estado persistido y su versión vigente.", Qg2ResponseShape.Schema, "AchResponseMappingModel"),
                new("400", "El motivo o el contexto requerido para auditar la transición no es válido.", Qg2ResponseShape.ProblemDetails, "ProblemDetails"),
                new("404", "No existe el mapping de estado ACH que se intenta desactivar.", Qg2ResponseShape.ProblemDetails, "ProblemDetails"),
                new("409", "ExpectedVersion no coincide o la persistencia detecta una modificación concurrente.", Qg2ResponseShape.ProblemDetails, "ProblemDetails")
            ],
            ResponseSchemaDocumentation: new(
                "AchResponseMappingModel",
                ["id", "clearingHouseId", "clearingHouseCode", "responseType", "externalCode", "externalCause", "internalStatusId", "externalServiceStatusId", "internalStatusName", "normalizedCause", "normalizedDescription", "requiresCause", "allowsNotification", "priority", "effectiveFrom", "effectiveTo", "isActive", "version"])),
        new(
            "GET",
            "/api/ach/response-status-mappings/{id}/audit",
            PrimaryStatusCode: "200",
            Parameters: [new("id", "path")],
            RequestSchema: null,
            RequestProperties: [],
            HasCorrelationHeader: false,
            Responses:
            [
                new("200", "Eventos de auditoría registrados para el mapping solicitado, posiblemente una colección vacía.", Qg2ResponseShape.Collection, "AchResponseAuditModel")
            ],
            ResponseSchemaDocumentation: new(
                "AchResponseAuditModel",
                ["id", "entityType", "entityId", "action", "previousState", "newState", "actor", "reason", "correlationId", "occurredAtUtc", "sanitizedMetadata"])),
        new(
            "GET",
            "/api/ach/responses/{id}/audit",
            PrimaryStatusCode: "200",
            Parameters: [new("id", "path")],
            RequestSchema: null,
            RequestProperties: [],
            HasCorrelationHeader: false,
            Responses:
            [
                new("200", "Eventos de auditoría registrados para la respuesta ACH solicitada, posiblemente una colección vacía.", Qg2ResponseShape.Collection, "AchResponseAuditModel")
            ],
            ResponseSchemaDocumentation: new(
                "AchResponseAuditModel",
                ["id", "entityType", "entityId", "action", "previousState", "newState", "actor", "reason", "correlationId", "occurredAtUtc", "sanitizedMetadata"])),
        new(
            "GET",
            "/api/ach/responses/orphans",
            PrimaryStatusCode: "200",
            Parameters: [new("clearingHouseId", "query", Required: false), new("status", "query", Required: false)],
            RequestSchema: null,
            RequestProperties: [],
            HasCorrelationHeader: false,
            Responses:
            [
                new("200", "Respuestas ACH huérfanas que cumplen los filtros solicitados, con un máximo de 500 elementos.", Qg2ResponseShape.Collection, "AchResponseOrphanModel")
            ],
            ResponseSchemaDocumentation: new(
                "AchResponseOrphanModel",
                ["id", "achResponseId", "clearingHouseId", "responseType", "externalIdentifiers", "externalCode", "receivedAtUtc", "operationalDate", "correlationId", "orphanReason", "candidateReferences", "resolutionStatus", "resolvedReference", "resolvedAtUtc", "version"])),
        new(
            "POST",
            "/api/ach/responses/{id}/orphan",
            PrimaryStatusCode: "200",
            Parameters: [new("id", "path")],
            RequestSchema: "CreateOrphanRequest",
            RequestProperties: ["reason", "candidateReferences", "correlationId"],
            HasCorrelationHeader: true,
            Responses:
            [
                new("200", "Registro huérfano existente o recién creado para la respuesta ACH indicada.", Qg2ResponseShape.Schema, "AchResponseOrphanModel"),
                new("400", "El motivo o el contexto requerido por la operación no es válido.", Qg2ResponseShape.ProblemDetails, "ProblemDetails"),
                new("404", "No existe la respuesta ACH que se intenta registrar como huérfana.", Qg2ResponseShape.ProblemDetails, "ProblemDetails"),
                new("409", "La respuesta carece de cámara canónica, su transición de estado no es válida o existe concurrencia de persistencia.", Qg2ResponseShape.ProblemDetails, "ProblemDetails")
            ],
            ResponseSchemaDocumentation: new(
                "AchResponseOrphanModel",
                ["id", "achResponseId", "clearingHouseId", "responseType", "externalIdentifiers", "externalCode", "receivedAtUtc", "operationalDate", "correlationId", "orphanReason", "candidateReferences", "resolutionStatus", "resolvedReference", "resolvedAtUtc", "version"])),
        new(
            "POST",
            "/api/ach/responses/orphans/{id}/review/start",
            PrimaryStatusCode: "200",
            Parameters: [new("id", "path")],
            RequestSchema: "VersionedReasonRequest",
            RequestProperties: ["expectedVersion", "reason", "correlationId"],
            HasCorrelationHeader: true,
            Responses:
            [
                new("200", "Registro huérfano en revisión o que ya se encontraba en revisión.", Qg2ResponseShape.Schema, "AchResponseOrphanModel"),
                new("400", "El motivo o el contexto requerido por la operación no es válido.", Qg2ResponseShape.ProblemDetails, "ProblemDetails"),
                new("404", "No existe la respuesta huérfana indicada.", Qg2ResponseShape.ProblemDetails, "ProblemDetails"),
                new("409", "ExpectedVersion no coincide, la huérfana ya fue resuelta, la transición asociada no es válida o existe concurrencia.", Qg2ResponseShape.ProblemDetails, "ProblemDetails")
            ],
            ResponseSchemaDocumentation: new(
                "AchResponseOrphanModel",
                ["id", "achResponseId", "clearingHouseId", "responseType", "externalIdentifiers", "externalCode", "receivedAtUtc", "operationalDate", "correlationId", "orphanReason", "candidateReferences", "resolutionStatus", "resolvedReference", "resolvedAtUtc", "version"])),
        new(
            "POST",
            "/api/ach/responses/orphans/{id}/resolve",
            PrimaryStatusCode: "200",
            Parameters: [new("id", "path")],
            RequestSchema: "ResolveOrphanRequest",
            RequestProperties: ["expectedVersion", "reason", "functionalReference", "reject", "correlationId"],
            HasCorrelationHeader: true,
            Responses:
            [
                new("200", "Respuesta huérfana resuelta, rechazada o ya finalizada previamente.", Qg2ResponseShape.Schema, "AchResponseOrphanModel"),
                new("400", "Falta el motivo/contexto requerido o FunctionalReference cuando se solicita asociar en lugar de rechazar.", Qg2ResponseShape.ProblemDetails, "ProblemDetails"),
                new("404", "No existe la respuesta huérfana indicada.", Qg2ResponseShape.ProblemDetails, "ProblemDetails"),
                new("409", "ExpectedVersion no coincide, la revisión no fue iniciada, la referencia no identifica una única transacción compatible o existe concurrencia.", Qg2ResponseShape.ProblemDetails, "ProblemDetails")
            ],
            ResponseSchemaDocumentation: new(
                "AchResponseOrphanModel",
                ["id", "achResponseId", "clearingHouseId", "responseType", "externalIdentifiers", "externalCode", "receivedAtUtc", "operationalDate", "correlationId", "orphanReason", "candidateReferences", "resolutionStatus", "resolvedReference", "resolvedAtUtc", "version"])),
        new(
            "POST",
            "/api/ach/responses/{id}/reprocess",
            PrimaryStatusCode: "202",
            Parameters: [new("id", "path")],
            RequestSchema: "ReprocessResponseRequest",
            RequestProperties: ["commandId", "expectedVersion", "reason", "correlationId"],
            HasCorrelationHeader: true,
            Responses:
            [
                new("202", "Intento de reproceso solicitado o intento existente recuperado por CommandId.", Qg2ResponseShape.Schema, "AchResponseReprocessModel"),
                new("400", "El motivo o el contexto requerido por la solicitud de reproceso no es válido.", Qg2ResponseShape.ProblemDetails, "ProblemDetails"),
                new("404", "No existe la respuesta ACH que se intenta reprocesar.", Qg2ResponseShape.ProblemDetails, "ProblemDetails"),
                new("409", "ExpectedVersion no coincide, ya existe un reproceso activo, la transición no es válida o existe concurrencia.", Qg2ResponseShape.ProblemDetails, "ProblemDetails")
            ],
            ResponseSchemaDocumentation: new(
                "AchResponseReprocessModel",
                ["id", "achResponseId", "attemptNumber", "status", "requestedBy", "reason", "correlationId", "requestedAtUtc", "completedAtUtc", "result", "commandId", "claimedBy", "startedAtUtc", "resultCode", "errorDetailSanitized"])),
        new(
            "GET",
            "/api/ach/responses/{id}/reprocess-attempts",
            PrimaryStatusCode: "200",
            Parameters: [new("id", "path")],
            RequestSchema: null,
            RequestProperties: [],
            HasCorrelationHeader: false,
            Responses:
            [
                new("200", "Intentos de reproceso asociados a la respuesta ACH, ordenados del intento más reciente al más antiguo.", Qg2ResponseShape.Collection, "AchResponseReprocessModel")
            ],
            ResponseSchemaDocumentation: new(
                "AchResponseReprocessModel",
                ["id", "achResponseId", "attemptNumber", "status", "requestedBy", "reason", "correlationId", "requestedAtUtc", "completedAtUtc", "result", "commandId", "claimedBy", "startedAtUtc", "resultCode", "errorDetailSanitized"])),
        new(
            "GET",
            "/api/ach/responses/{id}/reprocess-attempts/{attemptId}",
            PrimaryStatusCode: "200",
            Parameters: [new("id", "path"), new("attemptId", "path")],
            RequestSchema: null,
            RequestProperties: [],
            HasCorrelationHeader: false,
            Responses:
            [
                new("200", "Intento de reproceso encontrado para la respuesta ACH indicada.", Qg2ResponseShape.Schema, "AchResponseReprocessModel"),
                new("404", "No existe el intento de reproceso indicado para esa respuesta ACH.", Qg2ResponseShape.ProblemDetails, "ProblemDetails")
            ],
            ResponseSchemaDocumentation: new(
                "AchResponseReprocessModel",
                ["id", "achResponseId", "attemptNumber", "status", "requestedBy", "reason", "correlationId", "requestedAtUtc", "completedAtUtc", "result", "commandId", "claimedBy", "startedAtUtc", "resultCode", "errorDetailSanitized"]))
    ];

    private static readonly CenitOperationsQg2Operation[] CenitOperationsQg2Operations =
    [
        new(
            "POST",
            "/api/cenit/chamber-responses",
            ActionName: "ImportChamberResponseAsync",
            Policy: "CanManageAch",
            Summary: "Importar respuesta de cámara CENIT",
            Description: "Registra y clasifica la respuesta recibida mediante el servicio CENIT. Devuelve 201 cuando crea un registro sin problema operativo y 200 cuando recupera idempotentemente una respuesta ya registrada. Las validaciones de entrada traducidas por el controller devuelven 400; los resultados persistidos con conflicto, correlación ambigua o transición inválida devuelven 409; los demás resultados operativos controlados devuelven 422. Si el servicio no está disponible en el controller, devuelve 503.",
            PrimaryStatusCode: "201",
            Parameters: [],
            RequestSchema: "CenitChamberResponseImportCommand",
            RequestProperties: ["sourceResponseId", "sourceFileName", "messageType", "content", "receivedAtUtc", "relatedOutboundFileName", "relatedReference", "transactionTraceNumber", "achCycleId"],
            Responses:
            [
                new("201", "Respuesta de cámara CENIT registrada y ubicación canónica devuelta mediante CreatedAtAction.", Qg2ResponseShape.Schema, "CenitChamberResponseResult"),
                new("200", "Respuesta de cámara CENIT ya registrada recuperada idempotentemente sin crear un nuevo registro.", Qg2ResponseShape.Schema, "CenitChamberResponseResult"),
                new("400", "La solicitud incumple una validación explícita del servicio, como identificadores obligatorios, fecha de recepción o nombre de archivo válido.", Qg2ResponseShape.ProblemDetails, "ProblemDetails"),
                new("409", "La respuesta quedó registrada con conflicto, correlación ambigua o transición de estado no válida.", Qg2ResponseShape.ProblemDetails, "ProblemDetails"),
                new("422", "La respuesta quedó registrada con un resultado operativo controlado distinto de conflicto, ambigüedad o transición inválida.", Qg2ResponseShape.ProblemDetails, "ProblemDetails"),
                new("503", "El servicio de importación de respuestas de cámara CENIT no está disponible en el controller.", Qg2ResponseShape.ProblemDetails, "ProblemDetails")
            ],
            ResponseSchemaDocumentation: new(
                "CenitChamberResponseResult",
                ["id", "isDuplicate", "sourceResponseId", "sourceFileName", "rawTechnicalReference", "responseType", "state", "correlationOutcome", "relatedFileId", "relatedFileName", "achCycleId", "xmlNamespace", "messageGroupId", "messageStatus", "messageCreatedAtUtc", "originatingSender", "relatedReference", "relatedTransactionId", "transactionTraceNumber", "reasonCode", "description", "receivedAtUtc", "processedAtUtc", "isApplied", "problemCode", "itemSequence", "itemCount"])),
        new(
            "GET",
            "/api/cenit/chamber-responses/{id}",
            ActionName: "GetChamberResponseAsync",
            Policy: "CanReadAch",
            Summary: "Detalle de respuesta de cámara CENIT",
            Description: "Busca la respuesta persistida por id mediante el servicio de respuestas de cámara. Devuelve 404 sin cuerpo cuando el servicio no está disponible o cuando no existe el identificador solicitado.",
            PrimaryStatusCode: "200",
            Parameters: [new("id", "path", Required: true)],
            RequestSchema: null,
            RequestProperties: [],
            Responses:
            [
                new("200", "Respuesta de cámara CENIT encontrada para el identificador solicitado.", Qg2ResponseShape.Schema, "CenitChamberResponseResult"),
                new("404", "No existe una respuesta de cámara CENIT con el identificador indicado o el servicio no está disponible; la respuesta no contiene cuerpo.", Qg2ResponseShape.NoBody)
            ],
            ResponseSchemaDocumentation: new(
                "CenitChamberResponseResult",
                ["id", "isDuplicate", "sourceResponseId", "sourceFileName", "rawTechnicalReference", "responseType", "state", "correlationOutcome", "relatedFileId", "relatedFileName", "achCycleId", "xmlNamespace", "messageGroupId", "messageStatus", "messageCreatedAtUtc", "originatingSender", "relatedReference", "relatedTransactionId", "transactionTraceNumber", "reasonCode", "description", "receivedAtUtc", "processedAtUtc", "isApplied", "problemCode", "itemSequence", "itemCount"])),
        new(
            "GET",
            "/api/cenit/chamber-responses",
            ActionName: "ListChamberResponsesAsync",
            Policy: "CanReadAch",
            Summary: "Consultar respuestas de cámara CENIT",
            Description: "Devuelve una página que combina respuestas persistidas con archivos NACHA CENIT todavía pendientes de respuesta. page se normaliza a un mínimo de 1 y pageSize se limita entre 1 y 200; si el servicio no está disponible, devuelve una página vacía con esos valores normalizados.",
            PrimaryStatusCode: "200",
            Parameters: [new("page", "query", Required: false), new("pageSize", "query", Required: false)],
            RequestSchema: null,
            RequestProperties: [],
            Responses:
            [
                new("200", "Página de respuestas de cámara CENIT y archivos pendientes de respuesta.", Qg2ResponseShape.Schema, "CenitChamberResponsePage")
            ],
            ResponseSchemaDocumentation: new(
                "CenitChamberResponsePage",
                ["items", "total", "page", "pageSize"])),
        new(
            "GET",
            "/api/cenit/queues",
            ActionName: "GetQueueAsync",
            Policy: "CanReadAch",
            Summary: "Cola de ejecución de ciclos CENIT",
            Description: "Consulta CenitCycleQueue sin seguimiento. Aplica igualdad exacta cuando se informan status o targetAchCycleId, normaliza la paginación, ordena por EnqueuedAtUtc descendente y devuelve un cuerpo anónimo con items, total, page y pageSize; cada item incluye datos de cola, ciclo destino y contexto de la transacción relacionada.",
            PrimaryStatusCode: "200",
            Parameters: [new("status", "query", Required: false), new("targetAchCycleId", "query", Required: false), new("page", "query", Required: false), new("pageSize", "query", Required: false)],
            RequestSchema: null,
            RequestProperties: [],
            Responses:
            [
                new("200", "Página de elementos de la cola CENIT que cumplen los filtros solicitados.", Qg2ResponseShape.Untyped)
            ]),
        new(
            "GET",
            "/api/cenit/net-positions",
            ActionName: "GetNetPositionsAsync",
            Policy: "CanReadAch",
            Summary: "Posiciones netas de liquidez por ejecución",
            Description: "Consulta posiciones netas de la ejecución indicada; cuando cenitCycleExecutionId no se informa, selecciona la ejecución más reciente por StartedAtUtc. Si no existe ninguna ejecución, devuelve items vacío, total 0 y cenitCycleExecutionId nulo. En caso contrario ordena las posiciones por NetAmount descendente y devuelve un cuerpo anónimo paginado con los importes y datos de liquidez persistidos.",
            PrimaryStatusCode: "200",
            Parameters: [new("cenitCycleExecutionId", "query", Required: false), new("page", "query", Required: false), new("pageSize", "query", Required: false)],
            RequestSchema: null,
            RequestProperties: [],
            Responses:
            [
                new("200", "Página de posiciones netas de la ejecución CENIT seleccionada, o resultado vacío cuando no existe una ejecución.", Qg2ResponseShape.Untyped)
            ]),
        new(
            "GET",
            "/api/cenit/optimization-decisions",
            ActionName: "GetOptimizationDecisionsAsync",
            Policy: "CanReadAch",
            Summary: "Decisiones de optimización de liquidez",
            Description: "Consulta LiquidityOptimizationDecision sin seguimiento. Aplica filtros exactos por cenitCycleExecutionId y decisionType cuando se informan, normaliza la paginación, ordena por DecidedAtUtc descendente y devuelve un cuerpo anónimo con items, total, page y pageSize, incluyendo el contexto transaccional persistido para cada decisión.",
            PrimaryStatusCode: "200",
            Parameters: [new("cenitCycleExecutionId", "query", Required: false), new("decisionType", "query", Required: false), new("page", "query", Required: false), new("pageSize", "query", Required: false)],
            RequestSchema: null,
            RequestProperties: [],
            Responses:
            [
                new("200", "Página de decisiones de optimización que cumplen los filtros solicitados.", Qg2ResponseShape.Untyped)
            ]),
        new(
            "GET",
            "/api/cenit/traceability",
            ActionName: "GetTraceabilityAsync",
            Policy: "CanReadAch",
            Summary: "Trazabilidad CENIT de transacciones",
            Description: "Resuelve la cámara cuyo código es CENIT y consulta únicamente sus transacciones. Aplica filtros exactos por state y achCycleId cuando se informan, ordena por StateChangedAtUtc descendente y devuelve un cuerpo anónimo paginado. La respuesta deriva CausalCode, CausalKind y CausalDescription de ReturnReasonCode o, en su ausencia, ContrapartidasResponseCode y de los catálogos activos consultados por el controller. Si la cámara CENIT no está configurada, devuelve una página vacía.",
            PrimaryStatusCode: "200",
            Parameters: [new("state", "query", Required: false), new("achCycleId", "query", Required: false), new("page", "query", Required: false), new("pageSize", "query", Required: false)],
            RequestSchema: null,
            RequestProperties: [],
            Responses:
            [
                new("200", "Página de trazabilidad de transacciones pertenecientes a la cámara CENIT que cumplen los filtros solicitados.", Qg2ResponseShape.Untyped)
            ])
    ];

    private static readonly Qg2SchemaDocumentation CertificateVersionSchemaDocumentation = new(
        "CertificateVersionApiDto",
        [
            "id", "code", "displayName", "fileName", "financialInstitutionId", "financialInstitutionName",
            "clearingHouseId", "clearingHouseName", "environment", "purpose", "holderType", "status",
            "functionalStatus", "daysRemaining", "versionNumber", "subject", "issuer", "serialNumber",
            "thumbprint", "fingerprintSha256", "notBefore", "notAfter", "hasPrivateKey", "keyAlgorithm",
            "keySize", "signatureAlgorithm", "secretRefMasked", "uploadedAtUtc", "uploadedBy", "activatedAtUtc",
            "revokedAtUtc", "revocationReason", "revokedBy", "canDelete"
        ]);

    private static readonly CertificateQg2Operation[] CertificateQg2Operations =
    [
        new(
            "POST",
            "/nacha-security/certificates/management/managed/preview",
            typeof(CertificateManagementController),
            ActionName: "PreviewManagedAsync",
            Policy: "P1.CertificatesUploadPublic",
            Summary: "Previsualizar certificado administrado",
            Description: "Valida la solicitud y el archivo antes de invocar la previsualización del servicio. El uso debe ser CFA signing/decryption o validación de cámara; para validación de cámara se exige clearingHouseId. Requiere archivo no vacío de hasta 10 MB y una extensión compatible con el uso; para el certificado CFA exige contraseña. Devuelve 400 para validaciones funcionales y 413 cuando el archivo supera el máximo permitido.",
            Parameters: [],
            RequestSchema: "ManagedCertificateApiRequest",
            RequestProperties: ["purpose", "clearingHouseId", "password", "file"],
            RequestContentType: "multipart/form-data",
            Responses:
            [
                new("200", "Previsualización del certificado validado sin persistirlo.", Qg2ResponseShape.Schema, "CertificatePreviewDto"),
                new("400", "La solicitud, el formato del archivo, la contraseña o la validación del certificado no cumplen las reglas ejecutadas por el controller o el servicio.", Qg2ResponseShape.ProblemDetails, "ProblemDetails"),
                new("413", "El archivo informado supera el máximo de 10 MB aceptado por la operación.", Qg2ResponseShape.ProblemDetails, "ProblemDetails")
            ],
            ResponseSchemaDocumentation: new(
                "CertificatePreviewDto",
                ["purpose", "financialInstitutionId", "financialInstitutionName", "clearingHouseId", "clearingHouseName", "subject", "issuer", "serialNumber", "thumbprint", "notBefore", "notAfter", "hasPrivateKey", "keyAlgorithm", "keySize", "signatureAlgorithm", "functionalStatus", "daysRemaining", "canSignAndDecrypt", "isValid", "warnings"])),
        new(
            "POST",
            "/api/nacha-security/certificates/management/managed/preview",
            typeof(CertificateManagementController),
            ActionName: "PreviewManagedAsync",
            Policy: "P1.CertificatesUploadPublic",
            Summary: "Previsualizar certificado administrado",
            Description: "Valida la solicitud y el archivo antes de invocar la previsualización del servicio. El uso debe ser CFA signing/decryption o validación de cámara; para validación de cámara se exige clearingHouseId. Requiere archivo no vacío de hasta 10 MB y una extensión compatible con el uso; para el certificado CFA exige contraseña. Devuelve 400 para validaciones funcionales y 413 cuando el archivo supera el máximo permitido.",
            Parameters: [],
            RequestSchema: "ManagedCertificateApiRequest",
            RequestProperties: ["purpose", "clearingHouseId", "password", "file"],
            RequestContentType: "multipart/form-data",
            Responses:
            [
                new("200", "Previsualización del certificado validado sin persistirlo.", Qg2ResponseShape.Schema, "CertificatePreviewDto"),
                new("400", "La solicitud, el formato del archivo, la contraseña o la validación del certificado no cumplen las reglas ejecutadas por el controller o el servicio.", Qg2ResponseShape.ProblemDetails, "ProblemDetails"),
                new("413", "El archivo informado supera el máximo de 10 MB aceptado por la operación.", Qg2ResponseShape.ProblemDetails, "ProblemDetails")
            ],
            ResponseSchemaDocumentation: new(
                "CertificatePreviewDto",
                ["purpose", "financialInstitutionId", "financialInstitutionName", "clearingHouseId", "clearingHouseName", "subject", "issuer", "serialNumber", "thumbprint", "notBefore", "notAfter", "hasPrivateKey", "keyAlgorithm", "keySize", "signatureAlgorithm", "functionalStatus", "daysRemaining", "canSignAndDecrypt", "isValid", "warnings"])),
        new(
            "POST",
            "/nacha-security/certificates/management/managed",
            typeof(CertificateManagementController),
            ActionName: "SaveManagedAsync",
            Policy: "P1.CertificatesRegisterPrivate",
            Summary: "Guardar certificado administrado",
            Description: "Valida la misma solicitud aceptada por la previsualización y persiste el certificado mediante el servicio de carga. Devuelve la versión registrada; una validación funcional produce 400, un certificado duplicado produce 409 y un archivo mayor de 10 MB produce 413.",
            Parameters: [],
            RequestSchema: "ManagedCertificateApiRequest",
            RequestProperties: ["purpose", "clearingHouseId", "password", "file"],
            RequestContentType: "multipart/form-data",
            Responses:
            [
                new("200", "Versión del certificado administrado registrada por el servicio.", Qg2ResponseShape.Schema, "CertificateVersionApiDto"),
                new("400", "La solicitud o el certificado no superan las validaciones explícitas del controller o del servicio.", Qg2ResponseShape.ProblemDetails, "ProblemDetails"),
                new("409", "El servicio detectó un conflicto por certificado duplicado.", Qg2ResponseShape.ProblemDetails, "ProblemDetails"),
                new("413", "El archivo informado supera el máximo de 10 MB aceptado por la operación.", Qg2ResponseShape.ProblemDetails, "ProblemDetails")
            ],
            ResponseSchemaDocumentation: CertificateVersionSchemaDocumentation),
        new(
            "POST",
            "/api/nacha-security/certificates/management/managed",
            typeof(CertificateManagementController),
            ActionName: "SaveManagedAsync",
            Policy: "P1.CertificatesRegisterPrivate",
            Summary: "Guardar certificado administrado",
            Description: "Valida la misma solicitud aceptada por la previsualización y persiste el certificado mediante el servicio de carga. Devuelve la versión registrada; una validación funcional produce 400, un certificado duplicado produce 409 y un archivo mayor de 10 MB produce 413.",
            Parameters: [],
            RequestSchema: "ManagedCertificateApiRequest",
            RequestProperties: ["purpose", "clearingHouseId", "password", "file"],
            RequestContentType: "multipart/form-data",
            Responses:
            [
                new("200", "Versión del certificado administrado registrada por el servicio.", Qg2ResponseShape.Schema, "CertificateVersionApiDto"),
                new("400", "La solicitud o el certificado no superan las validaciones explícitas del controller o del servicio.", Qg2ResponseShape.ProblemDetails, "ProblemDetails"),
                new("409", "El servicio detectó un conflicto por certificado duplicado.", Qg2ResponseShape.ProblemDetails, "ProblemDetails"),
                new("413", "El archivo informado supera el máximo de 10 MB aceptado por la operación.", Qg2ResponseShape.ProblemDetails, "ProblemDetails")
            ],
            ResponseSchemaDocumentation: CertificateVersionSchemaDocumentation),
        new(
            "POST",
            "/nacha-security/certificates/management/public",
            typeof(CertificateManagementController),
            ActionName: "UploadPublicAsync",
            Policy: "P1.CertificatesUploadPublic",
            Summary: "Cargar certificado público",
            Description: "Acepta archivos públicos CER, CRT o PEM de hasta 10 MB. El controller exige archivo, código y nombre válidos antes de invocar el servicio. Devuelve la versión registrada; las validaciones funcionales producen 400, un duplicado produce 409 y un archivo demasiado grande produce 413.",
            Parameters: [],
            RequestSchema: "UploadPublicCertificateApiRequest",
            RequestProperties: ["code", "displayName", "clearingHouseId", "environment", "purpose", "holderType", "file"],
            RequestContentType: "multipart/form-data",
            Responses:
            [
                new("200", "Versión del certificado público registrada por el servicio.", Qg2ResponseShape.Schema, "CertificateVersionApiDto"),
                new("400", "El formulario, la extensión o el contenido del certificado no superan las validaciones ejecutadas por el controller o el servicio.", Qg2ResponseShape.ProblemDetails, "ProblemDetails"),
                new("409", "El servicio detectó un conflicto por certificado duplicado.", Qg2ResponseShape.ProblemDetails, "ProblemDetails"),
                new("413", "El archivo informado supera el máximo de 10 MB aceptado por la operación.", Qg2ResponseShape.ProblemDetails, "ProblemDetails")
            ],
            ResponseSchemaDocumentation: CertificateVersionSchemaDocumentation),
        new(
            "POST",
            "/api/nacha-security/certificates/management/public",
            typeof(CertificateManagementController),
            ActionName: "UploadPublicAsync",
            Policy: "P1.CertificatesUploadPublic",
            Summary: "Cargar certificado público",
            Description: "Acepta archivos públicos CER, CRT o PEM de hasta 10 MB. El controller exige archivo, código y nombre válidos antes de invocar el servicio. Devuelve la versión registrada; las validaciones funcionales producen 400, un duplicado produce 409 y un archivo demasiado grande produce 413.",
            Parameters: [],
            RequestSchema: "UploadPublicCertificateApiRequest",
            RequestProperties: ["code", "displayName", "clearingHouseId", "environment", "purpose", "holderType", "file"],
            RequestContentType: "multipart/form-data",
            Responses:
            [
                new("200", "Versión del certificado público registrada por el servicio.", Qg2ResponseShape.Schema, "CertificateVersionApiDto"),
                new("400", "El formulario, la extensión o el contenido del certificado no superan las validaciones ejecutadas por el controller o el servicio.", Qg2ResponseShape.ProblemDetails, "ProblemDetails"),
                new("409", "El servicio detectó un conflicto por certificado duplicado.", Qg2ResponseShape.ProblemDetails, "ProblemDetails"),
                new("413", "El archivo informado supera el máximo de 10 MB aceptado por la operación.", Qg2ResponseShape.ProblemDetails, "ProblemDetails")
            ],
            ResponseSchemaDocumentation: CertificateVersionSchemaDocumentation),
        new(
            "POST",
            "/nacha-security/certificates/management/private",
            typeof(CertificateManagementController),
            ActionName: "UploadPrivateAsync",
            Policy: "P1.CertificatesRegisterPrivate",
            Summary: "Registrar certificado privado",
            Description: "Acepta archivos PFX o P12 de hasta 10 MB. Además de las validaciones comunes del formulario, exige contraseña y StorageMode DatabaseEncrypted antes de invocar el servicio. Devuelve la versión registrada; las validaciones funcionales producen 400, un duplicado produce 409 y un archivo demasiado grande produce 413.",
            Parameters: [],
            RequestSchema: "UploadPrivateCertificateApiRequest",
            RequestProperties: ["code", "displayName", "clearingHouseId", "environment", "purpose", "holderType", "file", "password", "storageMode", "secretRef"],
            RequestContentType: "multipart/form-data",
            Responses:
            [
                new("200", "Versión del certificado privado registrada por el servicio.", Qg2ResponseShape.Schema, "CertificateVersionApiDto"),
                new("400", "El formulario, la contraseña, el modo de almacenamiento o el certificado no superan las validaciones ejecutadas por el controller o el servicio.", Qg2ResponseShape.ProblemDetails, "ProblemDetails"),
                new("409", "El servicio detectó un conflicto por certificado duplicado.", Qg2ResponseShape.ProblemDetails, "ProblemDetails"),
                new("413", "El archivo informado supera el máximo de 10 MB aceptado por la operación.", Qg2ResponseShape.ProblemDetails, "ProblemDetails")
            ],
            ResponseSchemaDocumentation: CertificateVersionSchemaDocumentation),
        new(
            "POST",
            "/api/nacha-security/certificates/management/private",
            typeof(CertificateManagementController),
            ActionName: "UploadPrivateAsync",
            Policy: "P1.CertificatesRegisterPrivate",
            Summary: "Registrar certificado privado",
            Description: "Acepta archivos PFX o P12 de hasta 10 MB. Además de las validaciones comunes del formulario, exige contraseña y StorageMode DatabaseEncrypted antes de invocar el servicio. Devuelve la versión registrada; las validaciones funcionales producen 400, un duplicado produce 409 y un archivo demasiado grande produce 413.",
            Parameters: [],
            RequestSchema: "UploadPrivateCertificateApiRequest",
            RequestProperties: ["code", "displayName", "clearingHouseId", "environment", "purpose", "holderType", "file", "password", "storageMode", "secretRef"],
            RequestContentType: "multipart/form-data",
            Responses:
            [
                new("200", "Versión del certificado privado registrada por el servicio.", Qg2ResponseShape.Schema, "CertificateVersionApiDto"),
                new("400", "El formulario, la contraseña, el modo de almacenamiento o el certificado no superan las validaciones ejecutadas por el controller o el servicio.", Qg2ResponseShape.ProblemDetails, "ProblemDetails"),
                new("409", "El servicio detectó un conflicto por certificado duplicado.", Qg2ResponseShape.ProblemDetails, "ProblemDetails"),
                new("413", "El archivo informado supera el máximo de 10 MB aceptado por la operación.", Qg2ResponseShape.ProblemDetails, "ProblemDetails")
            ],
            ResponseSchemaDocumentation: CertificateVersionSchemaDocumentation),
        new(
            "GET",
            "/nacha-security/certificates/management",
            typeof(CertificateManagementController),
            ActionName: "ListAsync",
            Policy: "P1.CertificatesRead",
            Summary: "Consultar catálogo de certificados",
            Description: "Consulta el servicio de catálogo con filtros opcionales por cámara, ambiente, uso, titular y estado. Devuelve las versiones que cumplen el filtro transformadas al contrato de API; la operación no modifica certificados.",
            Parameters: [new("clearingHouseId", "query", Required: false), new("environment", "query", Required: false), new("purpose", "query", Required: false), new("holderType", "query", Required: false), new("status", "query", Required: false)],
            RequestSchema: null,
            RequestProperties: [],
            RequestContentType: null,
            Responses:
            [
                new("200", "Colección de versiones de certificado que cumplen los filtros solicitados.", Qg2ResponseShape.Collection, "CertificateVersionApiDto")
            ],
            ResponseSchemaDocumentation: CertificateVersionSchemaDocumentation),
        new(
            "GET",
            "/api/nacha-security/certificates/management",
            typeof(CertificateManagementController),
            ActionName: "ListAsync",
            Policy: "P1.CertificatesRead",
            Summary: "Consultar catálogo de certificados",
            Description: "Consulta el servicio de catálogo con filtros opcionales por cámara, ambiente, uso, titular y estado. Devuelve las versiones que cumplen el filtro transformadas al contrato de API; la operación no modifica certificados.",
            Parameters: [new("clearingHouseId", "query", Required: false), new("environment", "query", Required: false), new("purpose", "query", Required: false), new("holderType", "query", Required: false), new("status", "query", Required: false)],
            RequestSchema: null,
            RequestProperties: [],
            RequestContentType: null,
            Responses:
            [
                new("200", "Colección de versiones de certificado que cumplen los filtros solicitados.", Qg2ResponseShape.Collection, "CertificateVersionApiDto")
            ],
            ResponseSchemaDocumentation: CertificateVersionSchemaDocumentation),
        new(
            "GET",
            "/nacha-security/certificates/management/{id}/versions",
            typeof(CertificateManagementController),
            ActionName: "ListVersionsAsync",
            Policy: "P1.CertificatesRead",
            Summary: "Consultar versiones de certificado",
            Description: "Solicita al catálogo las versiones asociadas al identificador indicado y devuelve la colección transformada al contrato de API. El controller no introduce una respuesta 404 cuando la colección está vacía.",
            Parameters: [new("id", "path", Required: true)],
            RequestSchema: null,
            RequestProperties: [],
            RequestContentType: null,
            Responses:
            [
                new("200", "Colección de versiones asociadas al identificador de certificado solicitado.", Qg2ResponseShape.Collection, "CertificateVersionApiDto")
            ],
            ResponseSchemaDocumentation: CertificateVersionSchemaDocumentation),
        new(
            "GET",
            "/api/nacha-security/certificates/management/{id}/versions",
            typeof(CertificateManagementController),
            ActionName: "ListVersionsAsync",
            Policy: "P1.CertificatesRead",
            Summary: "Consultar versiones de certificado",
            Description: "Solicita al catálogo las versiones asociadas al identificador indicado y devuelve la colección transformada al contrato de API. El controller no introduce una respuesta 404 cuando la colección está vacía.",
            Parameters: [new("id", "path", Required: true)],
            RequestSchema: null,
            RequestProperties: [],
            RequestContentType: null,
            Responses:
            [
                new("200", "Colección de versiones asociadas al identificador de certificado solicitado.", Qg2ResponseShape.Collection, "CertificateVersionApiDto")
            ],
            ResponseSchemaDocumentation: CertificateVersionSchemaDocumentation),
        new(
            "GET",
            "/nacha-security/certificates",
            typeof(DigitalEnvelopeCertificatesController),
            ActionName: "GetAsync",
            Policy: "P1.CertificatesRead",
            Summary: "Consultar certificados de sobre digital",
            Description: "Consulta el inventario del servicio y devuelve por cada registro su identificador, archivo, tipo, disponibilidad de llave privada y metadatos del certificado almacenado. La operación es de solo lectura y el controller devuelve 200 con la colección obtenida.",
            Parameters: [],
            RequestSchema: null,
            RequestProperties: [],
            RequestContentType: null,
            Responses:
            [
                new("200", "Colección de certificados registrados por el servicio de sobre digital.", Qg2ResponseShape.Collection, "DigitalEnvelopeCertificateResponse")
            ],
            ResponseSchemaDocumentation: new(
                "DigitalEnvelopeCertificateResponse",
                ["id", "fileName", "type", "hasPrivateKey", "subject", "issuer", "thumbprint", "notBefore", "notAfter", "uploadedAt"])),
        new(
            "POST",
            "/nacha-security/certificates",
            typeof(DigitalEnvelopeCertificatesController),
            ActionName: "UploadAsync",
            Policy: "P1.CertificatesUploadPublic",
            Summary: "Carga simplificada de certificados retirada",
            Description: "La operación no procesa un archivo ni crea un certificado. Siempre devuelve 410 con ProblemDetails e indica usar las operaciones de gestión public o private.",
            Parameters: [],
            RequestSchema: null,
            RequestProperties: [],
            RequestContentType: null,
            Responses:
            [
                new("410", "La carga simplificada está retirada y el cuerpo ProblemDetails remite a las operaciones de gestión public o private.", Qg2ResponseShape.ProblemDetails, "ProblemDetails")
            ]),
        new(
            "DELETE",
            "/nacha-security/certificates/{id}",
            typeof(DigitalEnvelopeCertificatesController),
            ActionName: "DeleteAsync",
            Policy: "P1.CertificatesRevoke",
            Summary: "Eliminar certificado de sobre digital",
            Description: "Invoca la eliminación del servicio y, al completar la llamada, devuelve 204 sin cuerpo. La implementación actual del repositorio no produce 404 cuando el identificador no existe: simplemente no elimina ninguna entidad.",
            Parameters: [new("id", "path", Required: true)],
            RequestSchema: null,
            RequestProperties: [],
            RequestContentType: null,
            Responses:
            [
                new("204", "La solicitud de eliminación finalizó sin cuerpo; la implementación actual también usa este resultado cuando no existe una entidad con el id indicado.", Qg2ResponseShape.NoBody)
            ])
    ];

    private static readonly CertificateQg2Operation[] DigitalEnvelopeQg2Operations =
    [
        new(
            "GET",
            "/api/nacha-security/digital-envelope/certificates",
            typeof(SobreDigitalController),
            ActionName: "Certificates",
            Policy: "P1.CertificatesRead",
            Summary: "Certificados utilizables para sobre digital",
            Description: "Lista versiones activas y vigentes registradas en DigitalCertificates/DigitalCertificateVersions que pueden participar en cifrado o descifrado.",
            Parameters: [],
            RequestSchema: null,
            RequestProperties: [],
            RequestContentType: null,
            Responses:
            [
                new("200", "Colección de versiones de certificado activas y vigentes que el servicio considera utilizables para operaciones de sobre digital.", Qg2ResponseShape.Collection, "ManagedDigitalEnvelopeCertificateDto")
            ],
            ResponseSchemaDocumentation: new(
                "ManagedDigitalEnvelopeCertificateDto",
                ["id", "code", "displayName", "fileName", "clearingHouseId", "financialInstitutionId", "environment", "purpose", "versionNumber", "hasPrivateKey", "thumbprintMasked", "notBefore", "notAfter", "canEncrypt", "canDecrypt"])),
        new(
            "POST",
            "/api/nacha-security/digital-envelope/encrypt",
            typeof(SobreDigitalController),
            ActionName: "Encrypt",
            Policy: "P1.DigitalEnvelopeEncrypt",
            Summary: "Cifrar archivo mediante sobre digital ACH V32",
            Description: "Recibe multipart/form-data con certificateVersionId, operationMode, file y clearingHouseId opcional. Tras las validaciones del controller, invoca el servicio de cifrado y devuelve el archivo binario con .ENV agregado al nombre original; en éxito publica X-Cryptographic-Profile. Los errores controlados se devuelven como ProblemDetails.",
            Parameters: [],
            RequestSchema: "DigitalEnvelopeFileRequest",
            RequestProperties: ["certificateVersionId", "clearingHouseId", "operationMode", "file"],
            RequestContentType: "multipart/form-data",
            Responses:
            [
                new("200", "Archivo cifrado devuelto como application/octet-stream con nombre de descarga y header X-Cryptographic-Profile informados por el servicio.", Qg2ResponseShape.Untyped),
                new("400", "La solicitud o la operación criptográfica no cumplen una validación controlada que el controller traduce a solicitud inválida.", Qg2ResponseShape.ProblemDetails, "ProblemDetails"),
                new("404", "No se encontró el certificado requerido para completar la operación de cifrado.", Qg2ResponseShape.ProblemDetails, "ProblemDetails"),
                new("409", "La versión seleccionada no puede utilizarse por estado, vigencia o propósito, o no está disponible el certificado privado de firma requerido para completar el cifrado.", Qg2ResponseShape.ProblemDetails, "ProblemDetails"),
                new("413", "El archivo supera el máximo de 50 MB aceptado por la operación.", Qg2ResponseShape.ProblemDetails, "ProblemDetails")
            ]),
        new(
            "POST",
            "/api/nacha-security/digital-envelope/decrypt",
            typeof(SobreDigitalController),
            ActionName: "Decrypt",
            Policy: "P1.DigitalEnvelopeDecrypt",
            Summary: "Descifrar archivo de sobre digital ACH V32",
            Description: "Recibe multipart/form-data con file .ENV, operationMode y certificateVersionId o clearingHouseId según la selección disponible. Tras las validaciones del controller, invoca el servicio de descifrado y devuelve el archivo recuperado eliminando la última extensión .ENV; en éxito publica X-Cryptographic-Profile. Los errores controlados se devuelven como ProblemDetails.",
            Parameters: [],
            RequestSchema: "DigitalEnvelopeFileRequest",
            RequestProperties: ["certificateVersionId", "clearingHouseId", "operationMode", "file"],
            RequestContentType: "multipart/form-data",
            Responses:
            [
                new("200", "Archivo descifrado devuelto como application/octet-stream con nombre de descarga y header X-Cryptographic-Profile informados por el servicio.", Qg2ResponseShape.Untyped),
                new("400", "La solicitud o la operación criptográfica no cumplen una validación controlada que el controller traduce a solicitud inválida.", Qg2ResponseShape.ProblemDetails, "ProblemDetails"),
                new("404", "No se encontró el certificado requerido para completar la operación de descifrado.", Qg2ResponseShape.ProblemDetails, "ProblemDetails"),
                new("409", "La versión requerida no puede utilizarse por estado, vigencia, propósito, disponibilidad de llave privada o correspondencia con el sobre recibido.", Qg2ResponseShape.ProblemDetails, "ProblemDetails"),
                new("413", "El archivo supera el máximo de 50 MB aceptado por la operación.", Qg2ResponseShape.ProblemDetails, "ProblemDetails"),
                new("422", "El sobre digital, su contenido firmado o la validación criptográfica produce un error controlado que el controller clasifica como no procesable.", Qg2ResponseShape.ProblemDetails, "ProblemDetails")
            ])
    ];


    private static readonly NachaSecurityQg2Operation[] NachaSecurityQg2Operations =
    [
        new(
            "POST",
            "/nacha-security/operations/nacha/generate",
            ActionName: "GeneratePlainAsync",
            Policy: "P1.NachaSecurityGenerateEncrypted",
            Summary: "Generar artefacto NACHA-M sin sobre digital",
            Description: "Recibe el identificador del ciclo y solicita al servicio la generación sin encapsulado de sobre digital. La acción devuelve 200 con DigitalEnvelopeOperationDto tanto cuando la operación termina correctamente como cuando el servicio registra un fallo funcional dentro del propio resultado; el controller no traduce esos fallos a códigos HTTP adicionales.",
            Parameters: [],
            RequestShape: NachaSecurityRequestShape.JsonSchema,
            RequestSchema: "NachaGenerateApiRequest",
            RequestProperties: ["cycleId"],
            Responses:
            [
                new("200", "Resultado persistido de la operación de generación NACHA-M, incluido su estado, artefacto y error cuando el servicio registró un fallo.", Qg2ResponseShape.Schema, "DigitalEnvelopeOperationDto")
            ]),
        new(
            "POST",
            "/nacha-security/operations/nacha/generate-encrypted",
            ActionName: "GenerateEncryptedAsync",
            Policy: "P1.NachaSecurityGenerateEncrypted",
            Summary: "Generar artefacto NACHA-M con sobre digital",
            Description: "Recibe el identificador del ciclo y solicita al servicio la generación con encapsulado de sobre digital. La acción devuelve 200 con DigitalEnvelopeOperationDto tanto cuando la operación termina correctamente como cuando el servicio registra un fallo funcional dentro del propio resultado; el controller no traduce esos fallos a códigos HTTP adicionales.",
            Parameters: [],
            RequestShape: NachaSecurityRequestShape.JsonSchema,
            RequestSchema: "NachaGenerateApiRequest",
            RequestProperties: ["cycleId"],
            Responses:
            [
                new("200", "Resultado persistido de la operación de generación NACHA-M con sobre digital, incluido su estado, artefacto y error cuando el servicio registró un fallo.", Qg2ResponseShape.Schema, "DigitalEnvelopeOperationDto")
            ]),
        new(
            "POST",
            "/nacha-security/operations/envelope/manual-encrypt",
            ActionName: "ManualEncryptAsync",
            Policy: "P1.NachaSecurityManualEncrypt",
            Summary: "Cifrar manualmente un archivo",
            Description: "Requiere un archivo no vacío, copia sus bytes y los entrega al servicio de cifrado manual. Si el archivo falta o está vacío responde 400 con el body anónimo code/message; RequestSizeLimit limita la solicitud a 50 MB. Una vez invocado el servicio, devuelve 200 con DigitalEnvelopeOperationDto incluso cuando el servicio registra la operación como fallida dentro del resultado.",
            Parameters: [],
            RequestShape: NachaSecurityRequestShape.MultipartFile,
            RequestSchema: null,
            RequestProperties: ["file"],
            Responses:
            [
                new("200", "Resultado persistido del cifrado manual, incluido su estado y metadata de artefacto.", Qg2ResponseShape.Schema, "DigitalEnvelopeOperationDto"),
                new("400", "No se informó un archivo o el archivo recibido está vacío; el body actual contiene code y message.", Qg2ResponseShape.Untyped),
                new("413", "La solicitud supera el límite de 50 MB aplicado por RequestSizeLimit.", Qg2ResponseShape.Untyped)
            ]),
        new(
            "POST",
            "/nacha-security/operations/envelope/manual-decrypt",
            ActionName: "ManualDecryptAsync",
            Policy: "P1.NachaSecurityManualDecrypt",
            Summary: "Descifrar manualmente un archivo",
            Description: "Requiere un archivo no vacío, copia sus bytes y los entrega al servicio de descifrado manual. Si el archivo falta o está vacío responde 400 con el body anónimo code/message; RequestSizeLimit limita la solicitud a 50 MB. Una vez invocado el servicio, devuelve 200 con DigitalEnvelopeOperationDto incluso cuando el servicio registra la operación como fallida dentro del resultado.",
            Parameters: [],
            RequestShape: NachaSecurityRequestShape.MultipartFile,
            RequestSchema: null,
            RequestProperties: ["file"],
            Responses:
            [
                new("200", "Resultado persistido del descifrado manual, incluido su estado y metadata de artefacto.", Qg2ResponseShape.Schema, "DigitalEnvelopeOperationDto"),
                new("400", "No se informó un archivo o el archivo recibido está vacío; el body actual contiene code y message.", Qg2ResponseShape.Untyped),
                new("413", "La solicitud supera el límite de 50 MB aplicado por RequestSizeLimit.", Qg2ResponseShape.Untyped)
            ]),
        new(
            "GET",
            "/nacha-security/operations/{operationId}",
            ActionName: "GetByOperationIdAsync",
            Policy: "P1.NachaSecurityRead",
            Summary: "Consultar operación de seguridad NACHA",
            Description: "Busca la operación por operationId. Devuelve 404 con el body anónimo code/message/operationId cuando no existe; cuando existe devuelve el DTO persistido. Al consultar una operación exitosa cuyo artefacto ya venció, el servicio proyecta el estado Expired en la respuesta.",
            Parameters: [new("operationId", "path", Required: true)],
            RequestShape: NachaSecurityRequestShape.None,
            RequestSchema: null,
            RequestProperties: [],
            Responses:
            [
                new("200", "Operación de seguridad NACHA encontrada.", Qg2ResponseShape.Schema, "DigitalEnvelopeOperationDto"),
                new("404", "No existe una operación con el operationId indicado; el body actual contiene code, message y operationId.", Qg2ResponseShape.Untyped)
            ]),
        new(
            "GET",
            "/nacha-security/operations/audit",
            ActionName: "AuditAsync",
            Policy: "P1.NachaSecurityRead",
            Summary: "Consultar auditoría de operaciones de seguridad NACHA",
            Description: "Devuelve las operaciones almacenadas ordenadas por RequestedAtUtc descendente. El parámetro take tiene valor predeterminado 100 y el servicio lo normaliza al rango 1..200; valores fuera de ese rango no producen un status HTTP de error.",
            Parameters: [new("take", "query", Required: false)],
            RequestShape: NachaSecurityRequestShape.None,
            RequestSchema: null,
            RequestProperties: [],
            Responses:
            [
                new("200", "Colección de operaciones de seguridad NACHA ordenadas desde la más reciente, limitada por el valor normalizado de take.", Qg2ResponseShape.Collection, "DigitalEnvelopeOperationDto")
            ]),
        new(
            "POST",
            "/nacha-security/operations/{operationId}/authorize-download",
            ActionName: "AuthorizeDownloadAsync",
            Policy: "P1.NachaSecurityAuthorizeDownload",
            Summary: "Autorizar descarga de artefacto de seguridad",
            Description: "Busca la operación, exige el permiso de descarga derivado del tipo de artefacto y solicita al servicio una autorización temporal. Devuelve 404 si la operación no existe, 403 cuando el usuario no posee el permiso de descarga requerido y 400 cuando la descarga de plano está bloqueada por SIGNATURE_VALIDATION_FAILED o cuando el servicio no autoriza la descarga. El éxito devuelve un body anónimo con operationId, authorized=true y expiresAtUtc.",
            Parameters: [new("operationId", "path", Required: true)],
            RequestShape: NachaSecurityRequestShape.None,
            RequestSchema: null,
            RequestProperties: [],
            Responses:
            [
                new("200", "Autorización concedida; el body anónimo contiene operationId, authorized y expiresAtUtc.", Qg2ResponseShape.Untyped),
                new("400", "La descarga no puede autorizarse; el body anónimo contiene code, message y operationId.", Qg2ResponseShape.Untyped),
                new("403", "El usuario autenticado no posee el permiso de descarga requerido para el artefacto de la operación.", Qg2ResponseShape.NoBody),
                new("404", "No existe la operación indicada; el body anónimo contiene code, message y operationId.", Qg2ResponseShape.Untyped)
            ]),
        new(
            "GET",
            "/nacha-security/operations/{operationId}/download",
            ActionName: "DownloadAsync",
            Policy: "P1.NachaSecurityRead",
            Summary: "Descargar artefacto de seguridad autorizado",
            Description: "Busca la operación y exige el permiso de descarga derivado del tipo de artefacto. Devuelve 404 si la operación no existe, 403 cuando falta ese permiso y 400 cuando la descarga de plano está bloqueada por SIGNATURE_VALIDATION_FAILED o cuando no existe una autorización/artefacto disponible. En éxito devuelve el stream, Content-Type y nombre de archivo entregados por el servicio; esos valores dependen del artefacto persistido y no se fuerzan a un media type único en esta operación.",
            Parameters: [new("operationId", "path", Required: true)],
            RequestShape: NachaSecurityRequestShape.None,
            RequestSchema: null,
            RequestProperties: [],
            Responses:
            [
                new("200", "Stream del artefacto con Content-Type y nombre de descarga informados por el descriptor persistido.", Qg2ResponseShape.Untyped),
                new("400", "La descarga no está autorizada o no está disponible, o la descarga de plano está bloqueada; el body anónimo contiene code, message y operationId.", Qg2ResponseShape.Untyped),
                new("403", "El usuario autenticado no posee el permiso de descarga requerido para el artefacto de la operación.", Qg2ResponseShape.NoBody),
                new("404", "No existe la operación indicada; el body anónimo contiene code, message y operationId.", Qg2ResponseShape.Untyped)
            ])
    ];

    private static readonly Sc309Qg2Operation[] Sc309Qg2Operations =
    [
        new(
            "GET",
            "/NachaExport/{cycleId}",
            typeof(NachaExportController),
            ActionName: "Export",
            Policy: "CanReadAch",
            Summary: "Descargar archivo NACHA-M generado para un ciclo",
            Description: "Busca el ciclo y su cámara, genera los artefactos NACHA-M y aplica la política de nombre externo antes de devolverlos. Con un único artefacto responde text/plain y usa el ExternalFileName resuelto como nombre de descarga; con varios artefactos responde application/zip y usa el primer nombre externo seguido de .zip. Si no existe el ciclo o su cámara responde 404 sin body. Si no hay contenido exportable o una validación/generación controlada falla responde 422 con ProblemDetails.",
            Parameters: [new("cycleId", "path")],
            RequestShape: Sc309RequestShape.None,
            RequestSchema: null,
            RequestProperties: [],
            Responses:
            [
                new("200", "Archivo de descarga: text/plain para un único artefacto con Content-Disposition basado en el ExternalFileName resuelto; application/zip para múltiples artefactos con nombre <primer ExternalFileName>.zip.", Qg2ResponseShape.Untyped),
                new("404", "No existe el ciclo solicitado o no existe la cámara asociada al ciclo; la respuesta no tiene body.", Qg2ResponseShape.NoBody),
                new("422", "No existe contenido exportable o una validación/precondición controlada impide generar el archivo; el body es ProblemDetails.", Qg2ResponseShape.ProblemDetails, "ProblemDetails")
            ],
            SuccessContentTypes: ["text/plain", "application/zip"],
            SuccessDescriptionFragments: ["Content-Disposition", "ExternalFileName", ".zip"]),
        new(
            "GET",
            "/NachaExport/{cycleId}/sobre-digital",
            typeof(NachaExportController),
            ActionName: "ExportEncrypted",
            Policy: "CanReadAch",
            Summary: "Descargar exportación NACHA-M con política de sobre digital",
            Description: "Genera los artefactos NACHA-M del ciclo y aplica cifrado cuando forceEncryption es true o la política de sobre digital lo requiere. Un único artefacto sin cifrar se devuelve como text/plain con el nombre externo resuelto; un único artefacto cifrado usa el ContentType y FileName entregados por el servicio de sobre digital (la implementación vigente devuelve application/octet-stream y agrega .ENV al nombre de entrada). Si se generan varios artefactos, la respuesta es application/zip con nombre basado en el primer archivo devuelto. Para un único archivo cifrado también se emiten X-Cryptographic-Profile y X-Plaintext-SHA256. Los errores controlados de sobre digital se traducen a 400, 413 o 422; ciclo/cámara inexistente produce 404 sin body.",
            Parameters: [new("cycleId", "path"), new("forceEncryption", "query", Required: false)],
            RequestShape: Sc309RequestShape.None,
            RequestSchema: null,
            RequestProperties: [],
            Responses:
            [
                new("200", "Archivo de descarga con Content-Disposition dinámico: text/plain cuando no se cifra, application/octet-stream para el único sobre cifrado de la implementación vigente, o application/zip cuando hay múltiples artefactos. En el caso cifrado de un solo archivo se emiten X-Cryptographic-Profile y X-Plaintext-SHA256.", Qg2ResponseShape.Untyped),
                new("400", "La operación de sobre digital produjo un error controlado que no corresponde a tamaño ni a contenido no procesable; el body es ProblemDetails.", Qg2ResponseShape.ProblemDetails, "ProblemDetails"),
                new("404", "No existe el ciclo solicitado o no existe la cámara asociada al ciclo; la respuesta no tiene body.", Qg2ResponseShape.NoBody),
                new("413", "El servicio de sobre digital rechazó el contenido por exceder el tamaño permitido; el body es ProblemDetails.", Qg2ResponseShape.ProblemDetails, "ProblemDetails"),
                new("422", "No existe contenido exportable, falla una precondición de generación o el sobre digital/certificado produce un error controlado no procesable; el body es ProblemDetails.", Qg2ResponseShape.ProblemDetails, "ProblemDetails")
            ],
            SuccessContentTypes: ["text/plain", "application/octet-stream", "application/zip"],
            SuccessDescriptionFragments: ["Content-Disposition", "X-Cryptographic-Profile", "X-Plaintext-SHA256"]),
        new(
            "POST",
            "/NachaUpload/upload",
            typeof(NachaUploadController),
            ActionName: "UploadNachaFile",
            Policy: "P1.NachaUpload",
            Summary: "Cargar archivo NACHA-M para ingestión",
            Description: "Recibe multipart/form-data con File, ClearingHouseId, ForceReprocess y ParentIngestionId. El archivo debe ser no vacío y de hasta 10 MB; se aceptan nombres CENIT con el patrón implementado o extensiones .ach, .nacha, .txt, .OUT, .RET y .env. Los archivos .env requieren ClearingHouseId positivo. Cuando el archivo informa MIME type, solo se aceptan text/plain o application/octet-stream; si viene vacío se remite al servicio como application/octet-stream. Devuelve 200 al completar/aceptar la ingesta, 422 cuando el resultado queda Bloqueado o Fallido, 400 para validaciones controladas, 408 para cancelación, 500 para errores capturados y el límite de request puede producir 413 antes de completar la acción.",
            Parameters: [],
            RequestShape: Sc309RequestShape.MultipartSchema,
            RequestSchema: "NachaUploadRequest",
            RequestProperties: ["file", "clearingHouseId", "forceReprocess", "parentIngestionId"],
            Responses:
            [
                new("200", "Resultado de la ingesta NACHA-M cuando el servicio no deja el archivo en estado Bloqueado ni Fallido.", Qg2ResponseShape.Schema, "NachaUploadResponseDto"),
                new("400", "El archivo/formulario no supera una validación explícita del controller o el servicio lanza ArgumentException; el body es NachaUploadResponseDto.", Qg2ResponseShape.Schema, "NachaUploadResponseDto"),
                new("408", "La operación fue cancelada o agotó el tiempo de espera; el body es NachaUploadResponseDto.", Qg2ResponseShape.Schema, "NachaUploadResponseDto"),
                new("413", "La solicitud multipart supera el límite de 10 MB aplicado por RequestSizeLimit/RequestFormLimits antes de completar el procesamiento de la acción.", Qg2ResponseShape.NoBody),
                new("422", "El servicio de ingestión devolvió estado Bloqueado o Fallido; el body es NachaUploadResponseDto.", Qg2ResponseShape.Schema, "NachaUploadResponseDto"),
                new("500", "Una excepción no controlada por las validaciones específicas fue capturada por la acción; el body es NachaUploadResponseDto.", Qg2ResponseShape.Schema, "NachaUploadResponseDto")
            ],
            SuccessContentTypes: ["application/json"],
            SuccessDescriptionFragments: [],
            ResponseSchemaDocumentation: new(
                "NachaUploadResponseDto",
                ["success", "partial", "message", "errors", "operatorReturns", "traceId", "ingestionId", "originalFileName", "fileHash", "correlationId", "ingestionStatus", "cycleResolutionStatus", "parsingStatus", "detectedClearingHouseId", "resolvedClearingHouseId", "resolvedAchCycleId", "operationalDate", "profileSelectionStatus", "selectedProfileCode", "selectedProfileVersion", "totalBatches", "totalEntries", "totalAddendas", "error"])),
        new(
            "GET",
            "/NachaUpload/records",
            typeof(NachaUploadController),
            ActionName: "GetUploadedRecords",
            Policy: "P1.NachaRead",
            Summary: "Consultar archivos NACHA-M cargados",
            Description: "Consulta los encabezados NACHA persistidos y permite filtrar por immediateOrigin, immediateDestination, referenceCode, achCycleId y fileCreationDate. La respuesta agrega por archivo los conteos de lotes, entradas y adendas, además de totales monetarios persistidos; la acción no expone descarga de archivo ni recibe body.",
            Parameters:
            [
                new("immediateOrigin", "query", Required: false),
                new("immediateDestination", "query", Required: false),
                new("referenceCode", "query", Required: false),
                new("achCycleId", "query", Required: false),
                new("fileCreationDate", "query", Required: false)
            ],
            RequestShape: Sc309RequestShape.None,
            RequestSchema: null,
            RequestProperties: [],
            Responses:
            [
                new("200", "Colección de archivos NACHA-M persistidos que cumplen los filtros solicitados, con conteos y totales agregados.", Qg2ResponseShape.Collection, "NachaUploadRecordResponse")
            ],
            SuccessContentTypes: ["application/json"],
            SuccessDescriptionFragments: [],
            ResponseSchemaDocumentation: new(
                "NachaUploadRecordResponse",
                ["nachaId", "immediateOrigin", "immediateDestination", "immediateOriginName", "immediateDestinationName", "referenceCode", "fileCreationDate", "fileCreationTime", "achCycleId", "achCycleName", "clearingHouseName", "totalEntries", "totalAddendas", "totalBatches", "totalAmount", "totalDebitAmount", "totalCreditAmount"]))
    ];

    private static readonly Sc401Qg2Operation[] Sc401Qg2Operations =
    [
        new(
            "GET",
            "/api/ach-colombia/file-exchange/transfers",
            ActionName: "Query",
            Policy: "CanReadAch",
            Summary: "Consultar transferencias administradas de ACH Colombia",
            Description: "Consulta transferencias administradas de archivos de ACH Colombia usando filtros opcionales por fechas, dirección, estado, ciclo, origen, nombre, identificador y archivado. El servicio ordena desde la transferencia más reciente y normaliza pageNumber a un mínimo de 1 y pageSize al rango 1..500. Para outbound, Transferred significa publicación confirmada en la frontera CFA y no recepción ni aceptación por ACH Colombia.",
            Parameters:
            [
                new("from", "query", Required: false),
                new("to", "query", Required: false),
                new("direction", "query", Required: false),
                new("status", "query", Required: false),
                new("cycleId", "query", Required: false),
                new("executionOrigin", "query", Required: false),
                new("fileName", "query", Required: false),
                new("transferId", "query", Required: false),
                new("archived", "query", Required: false),
                new("pageNumber", "query", Required: false),
                new("pageSize", "query", Required: false)
            ],
            HasIdempotencyKey: false,
            RequestShape: Sc401RequestShape.None,
            RequestProperties: [],
            Responses:
            [
                new("200", "Colección de transferencias administradas con etiqueta operativa segura y evidencia externa explícitamente no confirmada cuando corresponde.", Qg2ResponseShape.Collection, "AchManagedFileTransferSummary")
            ],
            SuccessContentTypes: [],
            SuccessDescriptionFragments: [],
            ResponseSchemaDocumentation: new(
                "AchManagedFileTransferSummary",
                ["id", "fileName", "direction", "operationalDate", "cycleId", "status", "executionOrigin", "attemptCount", "updatedAtUtc", "archived", "retired", "statusLabel", "externalConfirmationStatus", "externalConfirmationMessage"])),
        new(
            "GET",
            "/api/ach-colombia/file-exchange/transfers/{id}",
            ActionName: "Get",
            Policy: "CanReadAch",
            Summary: "Obtener transferencia administrada de ACH Colombia",
            Description: "Obtiene el detalle persistido de la transferencia identificada por id, incluida su historia ordenada, referencias sanitizadas y capacidades calculadas. Para outbound, la publicación en la frontera CFA se distingue de cualquier recepción o aceptación externa, que permanece no confirmada sin evidencia verificable. Si no existe una transferencia ACH Colombia con ese identificador, responde 404 sin body.",
            Parameters: [new("id", "path", Required: true)],
            HasIdempotencyKey: false,
            RequestShape: Sc401RequestShape.None,
            RequestProperties: [],
            Responses:
            [
                new("200", "Detalle de la transferencia administrada con trazabilidad, semántica de publicación CFA y estado explícito de confirmación externa.", Qg2ResponseShape.Schema, "AchManagedFileTransferDetail"),
                new("404", "No existe una transferencia ACH Colombia con el identificador indicado; la respuesta no tiene body.", Qg2ResponseShape.NoBody)
            ],
            SuccessContentTypes: [],
            SuccessDescriptionFragments: [],
            ResponseSchemaDocumentation: new(
                "AchManagedFileTransferDetail",
                ["id", "fileName", "direction", "operationalDate", "cycleId", "status", "executionOrigin", "fileSize", "contentSha256", "attemptCount", "createdAtUtc", "transferredAtUtc", "processedAtUtc", "lastError", "archived", "archivedAtUtc", "retired", "retiredAtUtc", "retirementReason", "correctedFromTransferId", "history", "lastAttemptAtUtc", "lastErrorCode", "correlationId", "achFileExportId", "incomingNachaFileIngestionId", "transactionIds", "contentAvailable", "canRetry", "canReprocess", "canArchive", "canRetire", "clearingHouseCode", "clearingHouseName", "statusLabel", "externalConfirmationStatus", "externalConfirmationMessage", "sanitizedActiveReference", "sanitizedArchiveReference"])),
        new(
            "GET",
            "/api/ach-colombia/file-exchange/transfers/{id}/download",
            ActionName: "Download",
            Policy: "CanReadAch",
            Summary: "Descargar contenido de transferencia ACH Colombia",
            Description: "Solicita al servicio el contenido retenido de la transferencia. En éxito devuelve File(result.Content, result.ContentType, result.FileName): la implementación vigente entrega application/octet-stream y usa el nombre físico de la transferencia como filename de Content-Disposition. Si el servicio devuelve null por ausencia de contenido retenido, responde 404 sin body.",
            Parameters: [new("id", "path", Required: true)],
            HasIdempotencyKey: false,
            RequestShape: Sc401RequestShape.None,
            RequestProperties: [],
            Responses:
            [
                new("200", "Contenido retenido devuelto como application/octet-stream; Content-Disposition usa el FileName suministrado por AchManagedFileDownload, que el servicio vigente obtiene del nombre físico de la transferencia.", Qg2ResponseShape.Untyped),
                new("404", "El servicio no dispone de contenido retenido para devolver y la respuesta no tiene body.", Qg2ResponseShape.NoBody)
            ],
            SuccessContentTypes: ["application/octet-stream"],
            SuccessDescriptionFragments: ["Content-Disposition", "FileName", "application/octet-stream"]),
        new(
            "POST",
            "/api/ach-colombia/file-exchange/outbound/execute",
            ActionName: "ExecuteOutbound",
            Policy: "CanManageAch",
            Summary: "Ejecutar intercambio saliente ACH Colombia",
            Description: "Solicita al servicio una ejecución manual de intercambio saliente para el CycleId indicado. CorrectedFromTransferId puede identificar una transferencia saliente previa del mismo ciclo que se corrige. La operación consume Idempotency-Key opcional; si no se envía, el controller deriva una clave manual desde HttpContext.TraceIdentifier.",
            Parameters: [],
            HasIdempotencyKey: true,
            RequestShape: Sc401RequestShape.JsonSchema,
            RequestProperties: ["cycleId", "correctedFromTransferId"],
            Responses:
            [
                new("200", "Resultado de la ejecución saliente con cantidades procesadas, exitosas, fallidas e identificadores de transferencia involucrados.", Qg2ResponseShape.Schema, "AchManagedFileExecutionResult")
            ],
            SuccessContentTypes: [],
            SuccessDescriptionFragments: [],
            ResponseSchemaDocumentation: new("AchManagedFileExecutionResult", ["processed", "succeeded", "failed", "transferIds"])),
        new(
            "POST",
            "/api/ach-colombia/file-exchange/inbound/execute",
            ActionName: "ExecuteInbound",
            Policy: "CanManageAch",
            Summary: "Ejecutar intercambio entrante ACH Colombia",
            Description: "Solicita al servicio una ejecución manual de recepción de archivos mediante el adaptador configurado. La operación consume Idempotency-Key opcional; si no se envía, el controller deriva una clave manual desde HttpContext.TraceIdentifier.",
            Parameters: [],
            HasIdempotencyKey: true,
            RequestShape: Sc401RequestShape.None,
            RequestProperties: [],
            Responses:
            [
                new("200", "Resultado de la ejecución entrante con cantidades procesadas, exitosas, fallidas e identificadores de transferencia involucrados.", Qg2ResponseShape.Schema, "AchManagedFileExecutionResult")
            ],
            SuccessContentTypes: [],
            SuccessDescriptionFragments: [],
            ResponseSchemaDocumentation: new("AchManagedFileExecutionResult", ["processed", "succeeded", "failed", "transferIds"])),
        new(
            "POST",
            "/api/ach-colombia/file-exchange/transfers/{id}/retry",
            ActionName: "Retry",
            Policy: "CanManageAch",
            Summary: "Reintentar transferencia saliente ACH Colombia",
            Description: "Solicita al servicio un reintento manual para la transferencia identificada. La implementación del servicio admite transferencias salientes en RetryPending o Uncertain, con contenido retenido, perfil habilitado y reintentos disponibles. La operación consume Idempotency-Key opcional con el mismo fallback del controller.",
            Parameters: [new("id", "path", Required: true)],
            HasIdempotencyKey: true,
            RequestShape: Sc401RequestShape.None,
            RequestProperties: [],
            Responses:
            [
                new("200", "Detalle actualizado de la transferencia después del reintento completado por el servicio.", Qg2ResponseShape.Schema, "AchManagedFileTransferDetail")
            ],
            SuccessContentTypes: [],
            SuccessDescriptionFragments: [],
            ResponseSchemaDocumentation: new(
                "AchManagedFileTransferDetail",
                ["id", "fileName", "direction", "operationalDate", "cycleId", "status", "executionOrigin", "fileSize", "contentSha256", "attemptCount", "createdAtUtc", "transferredAtUtc", "processedAtUtc", "lastError", "archived", "archivedAtUtc", "retired", "retiredAtUtc", "retirementReason", "correctedFromTransferId", "history", "lastAttemptAtUtc", "lastErrorCode", "correlationId", "achFileExportId", "incomingNachaFileIngestionId", "transactionIds", "contentAvailable", "canRetry", "canReprocess", "canArchive", "canRetire", "clearingHouseCode", "clearingHouseName", "statusLabel", "externalConfirmationStatus", "externalConfirmationMessage", "sanitizedActiveReference", "sanitizedArchiveReference"])),
        new(
            "POST",
            "/api/ach-colombia/file-exchange/transfers/{id}/reprocess",
            ActionName: "Reprocess",
            Policy: "CanManageAch",
            Summary: "Reprocesar transferencia entrante ACH Colombia",
            Description: "Solicita al servicio reprocesar la transferencia identificada. La implementación admite transferencias entrantes rechazadas o fallidas que conservan contenido y referencia a la ingestión previa; en éxito devuelve su detalle actualizado.",
            Parameters: [new("id", "path", Required: true)],
            HasIdempotencyKey: false,
            RequestShape: Sc401RequestShape.None,
            RequestProperties: [],
            Responses:
            [
                new("200", "Detalle actualizado de la transferencia después del reproceso completado por el servicio.", Qg2ResponseShape.Schema, "AchManagedFileTransferDetail")
            ],
            SuccessContentTypes: [],
            SuccessDescriptionFragments: [],
            ResponseSchemaDocumentation: new(
                "AchManagedFileTransferDetail",
                ["id", "fileName", "direction", "operationalDate", "cycleId", "status", "executionOrigin", "fileSize", "contentSha256", "attemptCount", "createdAtUtc", "transferredAtUtc", "processedAtUtc", "lastError", "archived", "archivedAtUtc", "retired", "retiredAtUtc", "retirementReason", "correctedFromTransferId", "history", "lastAttemptAtUtc", "lastErrorCode", "correlationId", "achFileExportId", "incomingNachaFileIngestionId", "transactionIds", "contentAvailable", "canRetry", "canReprocess", "canArchive", "canRetire", "clearingHouseCode", "clearingHouseName", "statusLabel", "externalConfirmationStatus", "externalConfirmationMessage", "sanitizedActiveReference", "sanitizedArchiveReference"])),
        new(
            "POST",
            "/api/ach-colombia/file-exchange/transfers/{id}/archive",
            ActionName: "Archive",
            Policy: "CanManageAch",
            Summary: "Archivar transferencia ACH Colombia",
            Description: "Solicita al servicio registrar el archivado operativo de la transferencia identificada. La implementación conserva el contenido retenido, establece la referencia de archivo cuando falta y rechaza internamente transferencias ya retiradas; en éxito devuelve el detalle actualizado.",
            Parameters: [new("id", "path", Required: true)],
            HasIdempotencyKey: false,
            RequestShape: Sc401RequestShape.None,
            RequestProperties: [],
            Responses:
            [
                new("200", "Detalle actualizado de la transferencia después del archivado completado por el servicio.", Qg2ResponseShape.Schema, "AchManagedFileTransferDetail")
            ],
            SuccessContentTypes: [],
            SuccessDescriptionFragments: [],
            ResponseSchemaDocumentation: new(
                "AchManagedFileTransferDetail",
                ["id", "fileName", "direction", "operationalDate", "cycleId", "status", "executionOrigin", "fileSize", "contentSha256", "attemptCount", "createdAtUtc", "transferredAtUtc", "processedAtUtc", "lastError", "archived", "archivedAtUtc", "retired", "retiredAtUtc", "retirementReason", "correctedFromTransferId", "history", "lastAttemptAtUtc", "lastErrorCode", "correlationId", "achFileExportId", "incomingNachaFileIngestionId", "transactionIds", "contentAvailable", "canRetry", "canReprocess", "canArchive", "canRetire", "clearingHouseCode", "clearingHouseName", "statusLabel", "externalConfirmationStatus", "externalConfirmationMessage", "sanitizedActiveReference", "sanitizedArchiveReference"])),
        new(
            "POST",
            "/api/ach-colombia/file-exchange/transfers/{id}/retire",
            ActionName: "Retire",
            Policy: "CanManageAch",
            Summary: "Retirar transferencia ACH Colombia",
            Description: "Solicita al servicio retirar la transferencia identificada del almacenamiento activo y registrar el motivo informado. El servicio exige una razón no vacía y no permite retirar una transferencia InProgress; en éxito devuelve el detalle actualizado.",
            Parameters: [new("id", "path", Required: true)],
            HasIdempotencyKey: false,
            RequestShape: Sc401RequestShape.JsonSchema,
            RequestProperties: ["reason"],
            Responses:
            [
                new("200", "Detalle actualizado de la transferencia después del retiro completado por el servicio.", Qg2ResponseShape.Schema, "AchManagedFileTransferDetail")
            ],
            SuccessContentTypes: [],
            SuccessDescriptionFragments: [],
            ResponseSchemaDocumentation: new(
                "AchManagedFileTransferDetail",
                ["id", "fileName", "direction", "operationalDate", "cycleId", "status", "executionOrigin", "fileSize", "contentSha256", "attemptCount", "createdAtUtc", "transferredAtUtc", "processedAtUtc", "lastError", "archived", "archivedAtUtc", "retired", "retiredAtUtc", "retirementReason", "correctedFromTransferId", "history", "lastAttemptAtUtc", "lastErrorCode", "correlationId", "achFileExportId", "incomingNachaFileIngestionId", "transactionIds", "contentAvailable", "canRetry", "canReprocess", "canArchive", "canRetire", "clearingHouseCode", "clearingHouseName", "statusLabel", "externalConfirmationStatus", "externalConfirmationMessage", "sanitizedActiveReference", "sanitizedArchiveReference"]))
    ];

    private static readonly Qg2SchemaDocumentation[] Sc401AdditionalSchemaDocumentation =
    [
        new("AchManagedFileTransferEventDto", ["id", "occurredAtUtc", "eventType", "result", "message", "executionOrigin", "actor", "displayName", "displayResult", "displayMessage", "actorDisplay"])
    ];

    private static readonly Sc402Qg2Operation[] Sc402Qg2Operations =
    [
        new(
            "GET",
            "/api/integrations/mappingsets",
            ActionName: "Get",
            Policy: "CanReadAch",
            Summary: "Consultar mapping sets de integración",
            Description: "Consulta los mapping sets persistidos y, cuando methodId se informa, limita el resultado a ese método de integración. El servicio devuelve los elementos ordenados desde la actualización más reciente e incluye sus reglas.",
            Parameters: [new("methodId", "query", Required: false)],
            RequestSchema: null,
            RequestProperties: [],
            Responses:
            [
                new("200", "Colección de mapping sets que cumplen el filtro opcional por método de integración.", Qg2ResponseShape.Collection, "IntegrationMappingSetDto")
            ],
            ResponseSchemaDocumentation: new(
                "IntegrationMappingSetDto",
                ["id", "methodId", "methodCode", "name", "version", "status", "isActive", "notes", "publishedAtUtc", "publishedBy", "rules"])),
        new(
            "GET",
            "/api/integrations/mappingsets/published",
            ActionName: "GetPublished",
            Policy: "CanReadAch",
            Summary: "Obtener mapping set publicado por método",
            Description: "Busca mapping sets en estado Published para el methodId indicado y devuelve el de mayor versión. Si no existe uno publicado para ese método, responde 404 sin body.",
            Parameters: [new("methodId", "query")],
            RequestSchema: null,
            RequestProperties: [],
            Responses:
            [
                new("200", "Mapping set publicado de mayor versión para el método solicitado.", Qg2ResponseShape.Schema, "IntegrationMappingSetDto"),
                new("404", "No existe un mapping set publicado para el método indicado; la respuesta no tiene body.", Qg2ResponseShape.NoBody)
            ],
            ResponseSchemaDocumentation: new(
                "IntegrationMappingSetDto",
                ["id", "methodId", "methodCode", "name", "version", "status", "isActive", "notes", "publishedAtUtc", "publishedBy", "rules"])),
        new(
            "GET",
            "/api/integrations/mappingsets/{id}",
            ActionName: "GetById",
            Policy: "CanReadAch",
            Summary: "Obtener mapping set de integración",
            Description: "Obtiene el mapping set identificado por id junto con sus reglas. Si no existe, responde 404 sin body.",
            Parameters: [new("id", "path", Required: true)],
            RequestSchema: null,
            RequestProperties: [],
            Responses:
            [
                new("200", "Mapping set encontrado con sus reglas asociadas.", Qg2ResponseShape.Schema, "IntegrationMappingSetDto"),
                new("404", "No existe un mapping set con el identificador indicado; la respuesta no tiene body.", Qg2ResponseShape.NoBody)
            ],
            ResponseSchemaDocumentation: new(
                "IntegrationMappingSetDto",
                ["id", "methodId", "methodCode", "name", "version", "status", "isActive", "notes", "publishedAtUtc", "publishedBy", "rules"])),
        new(
            "POST",
            "/api/integrations/mappingsets",
            ActionName: "Create",
            Policy: "CanManageAch",
            Summary: "Crear borrador de mapping set",
            Description: "Crea un mapping set para un método de integración activo. El servicio inicializa el registro en estado Draft, activo y versión 0, registra el evento de creación y devuelve el recurso creado mediante CreatedAtAction.",
            Parameters: [],
            RequestSchema: "CreateIntegrationMappingSetRequest",
            RequestProperties: ["methodId", "name", "notes", "createdBy"],
            Responses:
            [
                new("201", "Mapping set borrador creado y devuelto con su contrato persistido.", Qg2ResponseShape.Schema, "IntegrationMappingSetDto")
            ],
            ResponseSchemaDocumentation: new(
                "IntegrationMappingSetDto",
                ["id", "methodId", "methodCode", "name", "version", "status", "isActive", "notes", "publishedAtUtc", "publishedBy", "rules"])),
        new(
            "PUT",
            "/api/integrations/mappingsets/{id}",
            ActionName: "Update",
            Policy: "CanManageAch",
            Summary: "Actualizar borrador de mapping set",
            Description: "Actualiza nombre, notas e indicador de actividad de un mapping set que el servicio exige esté en estado Draft, y registra el evento de actualización en su historia.",
            Parameters: [new("id", "path", Required: true)],
            RequestSchema: "UpdateIntegrationMappingSetRequest",
            RequestProperties: ["name", "notes", "isActive", "updatedBy"],
            Responses:
            [
                new("200", "Mapping set borrador actualizado con su estado persistido actual.", Qg2ResponseShape.Schema, "IntegrationMappingSetDto")
            ],
            ResponseSchemaDocumentation: new(
                "IntegrationMappingSetDto",
                ["id", "methodId", "methodCode", "name", "version", "status", "isActive", "notes", "publishedAtUtc", "publishedBy", "rules"])),
        new(
            "PUT",
            "/api/integrations/mappingsets/{id}/rules",
            ActionName: "UpsertRules",
            Policy: "CanManageAch",
            Summary: "Actualizar reglas de mapping set",
            Description: "Crea o actualiza reglas de un mapping set Draft. El servicio exige ParameterId activos del mismo método, no admite SourceKind Expression ni ConditionExpression, normaliza Priority a un mínimo de 1 y registra el cambio en la historia del mapping set.",
            Parameters: [new("id", "path", Required: true)],
            RequestSchema: "UpsertIntegrationMappingRulesRequest",
            RequestProperties: ["updatedBy", "rules"],
            Responses:
            [
                new("200", "Mapping set borrador devuelto con las reglas persistidas después del upsert.", Qg2ResponseShape.Schema, "IntegrationMappingSetDto")
            ],
            ResponseSchemaDocumentation: new(
                "IntegrationMappingSetDto",
                ["id", "methodId", "methodCode", "name", "version", "status", "isActive", "notes", "publishedAtUtc", "publishedBy", "rules"])),
        new(
            "POST",
            "/api/integrations/mappingsets/{id}/validate",
            ActionName: "Validate",
            Policy: "CanManageAch",
            Summary: "Validar mapping set de integración",
            Description: "Evalúa las reglas del mapping set contra los parámetros activos y el catálogo fuente del mismo método. Devuelve issues, cobertura y estado por parámetro, y el servicio persiste el resumen de validación calculado.",
            Parameters: [new("id", "path", Required: true)],
            RequestSchema: "ValidateIntegrationMappingSetRequest",
            RequestProperties: ["includeWarnings"],
            Responses:
            [
                new("200", "Resultado de validación con issues, resumen de cobertura y estado de los parámetros evaluados.", Qg2ResponseShape.Schema, "IntegrationMappingValidationResultDto")
            ],
            ResponseSchemaDocumentation: new(
                "IntegrationMappingValidationResultDto",
                ["mappingSetId", "isValid", "issues", "coverage", "parameters"])),
        new(
            "POST",
            "/api/integrations/mappingsets/{id}/preview",
            ActionName: "Preview",
            Policy: "CanManageAch",
            Summary: "Previsualizar resolución de mapping set",
            Description: "Resuelve las reglas habilitadas del mapping set sobre la muestra solicitada. Puede usar una muestra controlada, una transacción por id o la transacción más reciente opcionalmente filtrada por ciclo; si no encuentra una muestra reciente usa la muestra controlada. Devuelve los valores resueltos y representaciones JSON de la previsualización.",
            Parameters: [new("id", "path", Required: true)],
            RequestSchema: "PreviewIntegrationMappingSetRequest",
            RequestProperties: ["sampleTransactionId", "sampleCycleId", "maxItems", "useControlledSample"],
            Responses:
            [
                new("200", "Previsualización de los valores resueltos, su origen y las representaciones JSON generadas por el servicio.", Qg2ResponseShape.Schema, "IntegrationMappingPreviewResultDto")
            ],
            ResponseSchemaDocumentation: new(
                "IntegrationMappingPreviewResultDto",
                ["mappingSetId", "methodId", "methodCode", "contextMode", "items", "payloadPreviewJson", "rawPreviewJson"])),
        new(
            "POST",
            "/api/integrations/mappingsets/{id}/publish",
            ActionName: "Publish",
            Policy: "CanManageAch",
            Summary: "Publicar mapping set de integración",
            Description: "Valida un mapping set Draft antes de publicarlo. Cuando la validación es satisfactoria, archiva los mapping sets Published actuales del mismo método, asigna la siguiente versión, marca el mapping set como Published y activo, registra actor/fecha e historia de publicación.",
            Parameters: [new("id", "path", Required: true)],
            RequestSchema: "PublishIntegrationMappingSetRequest",
            RequestProperties: ["publishedBy", "publishNote"],
            Responses:
            [
                new("200", "Mapping set publicado con la versión y metadata de publicación persistidas.", Qg2ResponseShape.Schema, "IntegrationMappingSetDto")
            ],
            ResponseSchemaDocumentation: new(
                "IntegrationMappingSetDto",
                ["id", "methodId", "methodCode", "name", "version", "status", "isActive", "notes", "publishedAtUtc", "publishedBy", "rules"])),
        new(
            "POST",
            "/api/integrations/mappingsets/{id}/clone",
            ActionName: "Clone",
            Policy: "CanManageAch",
            Summary: "Clonar mapping set de integración",
            Description: "Copia el método, notas y reglas del mapping set de origen a un nuevo mapping set Draft, activo y versión 0. Si NewName está vacío, el servicio deriva el nombre agregando '(clon)' al nombre de origen y registra el evento de clonación.",
            Parameters: [new("id", "path", Required: true)],
            RequestSchema: "CloneIntegrationMappingSetRequest",
            RequestProperties: ["newName", "clonedBy"],
            Responses:
            [
                new("200", "Nuevo mapping set borrador creado como clon del mapping set de origen.", Qg2ResponseShape.Schema, "IntegrationMappingSetDto")
            ],
            ResponseSchemaDocumentation: new(
                "IntegrationMappingSetDto",
                ["id", "methodId", "methodCode", "name", "version", "status", "isActive", "notes", "publishedAtUtc", "publishedBy", "rules"])),
        new(
            "GET",
            "/api/integrations/mappingsets/{id}/history",
            ActionName: "History",
            Policy: "CanReadAch",
            Summary: "Consultar historia de mapping set",
            Description: "Consulta los eventos de historia persistidos para el mapping set y los devuelve desde el más reciente por PerformedAtUtc. Si no existen eventos, devuelve una colección vacía.",
            Parameters: [new("id", "path", Required: true)],
            RequestSchema: null,
            RequestProperties: [],
            Responses:
            [
                new("200", "Eventos de historia del mapping set, posiblemente una colección vacía, ordenados del más reciente al más antiguo.", Qg2ResponseShape.Collection, "IntegrationMappingSetHistoryDto")
            ],
            ResponseSchemaDocumentation: new(
                "IntegrationMappingSetHistoryDto",
                ["id", "mappingSetId", "methodId", "version", "status", "action", "performedBy", "performedAtUtc", "snapshotHash"])),
        new(
            "POST",
            "/api/integrations/mappingsets/compare",
            ActionName: "Compare",
            Policy: "CanManageAch",
            Summary: "Comparar mapping sets de integración",
            Description: "Compara dos mapping sets del mismo método. El servicio selecciona por parámetro la regla de menor prioridad numérica de cada lado y devuelve metadata de ambos conjuntos y diferencias Added, Removed, Modified o Equal con los campos que cambiaron.",
            Parameters: [],
            RequestSchema: "CompareIntegrationMappingSetsRequest",
            RequestProperties: ["leftMappingSetId", "rightMappingSetId"],
            Responses:
            [
                new("200", "Comparación de metadata y reglas entre los dos mapping sets solicitados.", Qg2ResponseShape.Schema, "IntegrationMappingSetComparisonResultDto")
            ],
            ResponseSchemaDocumentation: new(
                "IntegrationMappingSetComparisonResultDto",
                ["left", "right", "rules"]))
    ];

    private static readonly Qg2SchemaDocumentation[] Sc402AdditionalSchemaDocumentation =
    [
        new("UpsertIntegrationMappingRuleRequest", ["id", "methodId", "parameterId", "sourceKind", "sourceCatalogFieldId", "sourceFieldPath", "fixedValue", "defaultValue", "transformationCode", "formatMask", "priority", "requiredOverride", "enabled", "conditionExpression"]),
        new("IntegrationMappingRuleDto", ["id", "mappingSetId", "methodId", "parameterId", "sourceKind", "sourceCatalogFieldId", "sourceFieldPath", "fixedValue", "defaultValue", "transformationCode", "formatMask", "priority", "requiredOverride", "enabled", "conditionExpression"]),
        new("IntegrationMappingValidationIssueDto", ["severity", "code", "message", "path", "category"]),
        new("IntegrationMappingParameterValidationDto", ["parameterId", "parameterPath", "required", "status", "resolutionKind", "hints"]),
        new("IntegrationMappingCoverageSummaryDto", ["totalParameters", "validParameters", "incompleteParameters", "invalidParameters", "inactiveParameters", "coveredByDefaultOrFixed", "coveredBySourceField"]),
        new("IntegrationMappingPreviewItemDto", ["parameterId", "parameterPath", "resolvedFrom", "previewValue", "sourceSection", "resolutionKind", "appliedTransformation", "priority", "enabled"]),
        new("IntegrationMappingSetComparisonMetadataDto", ["mappingSetId", "name", "version", "status", "publishedAtUtc", "publishedBy", "notes"]),
        new("IntegrationMappingSetRuleComparisonDto", ["leftRuleId", "rightRuleId", "parameterId", "parameterPath", "parameterGroup", "changeType", "changedFields", "potentialImpact", "left", "right"])
    ];

    private static readonly Sc403Qg2Operation[] Sc403Qg2Operations =
    [
        new(
            "GET",
            "/api/users/soap-integrations",
            ControllerType: typeof(SoapIntegrationSettingsController),
            ActionName: "GetAsync",
            Policy: "P1.ConfigRead",
            Summary: "Consultar configuración SOAP de integraciones",
            Description: "Obtiene las listas de mappings SOAP persistidas. Si no existe una fila de configuración, el servicio devuelve listas vacías. La respuesta incorpora ProcTransaccionesEffectiveSettings calculado a partir del mapping Proc_Transacciones y de la evaluación de readiness disponible.",
            Parameters: [],
            RequestSchema: null,
            RequestProperties: [],
            Responses:
            [
                new("200", "Configuración SOAP persistida con la vista efectiva calculada para Proc_Transacciones.", Qg2ResponseShape.Schema, "SoapIntegrationSettingsDto")
            ],
            ResponseSchemaDocumentation: new(
                "SoapIntegrationSettingsDto",
                ["wscfaachMappings", "wsAxonRespuestaTransaccionesMappings", "procTransaccionesEffectiveSettings"])),
        new(
            "PUT",
            "/api/users/soap-integrations",
            ControllerType: typeof(SoapIntegrationSettingsController),
            ActionName: "SaveAsync",
            Policy: "P1.ConfigManage",
            Summary: "Guardar configuración SOAP de integraciones",
            Description: "Normaliza y persiste WscfaachMappings y WsAxonRespuestaTransaccionesMappings. El servicio descarta mappings sin MethodName, recorta los textos, normaliza OperatingMode a Live, Disabled o DryRun y conserva el timeout persistido cuando el request envía un valor no positivo para un método ya existente. ProcTransaccionesEffectiveSettings se recalcula para la respuesta y no se persiste desde el request.",
            Parameters: [],
            RequestSchema: "SoapIntegrationSettingsDto",
            RequestProperties: ["wscfaachMappings", "wsAxonRespuestaTransaccionesMappings", "procTransaccionesEffectiveSettings"],
            Responses:
            [
                new("200", "Configuración SOAP normalizada y persistida, con la vista efectiva recalculada para Proc_Transacciones.", Qg2ResponseShape.Schema, "SoapIntegrationSettingsDto")
            ],
            ResponseSchemaDocumentation: new(
                "SoapIntegrationSettingsDto",
                ["wscfaachMappings", "wsAxonRespuestaTransaccionesMappings", "procTransaccionesEffectiveSettings"])),
        new(
            "POST",
            "/api/uat/contrapartidas/dispatch-cycle",
            ControllerType: typeof(UatContrapartidasController),
            ActionName: "DispatchCycle",
            Policy: "CanManageAch",
            Summary: "Despachar transacción UAT a Proc_Contrapartidas",
            Description: "La operación solo está habilitada fuera de Production, en Development, Testing o UAT, y cuando ACH_SOAP_LIVE_TESTS es true. Valida que TransactionId sea positivo, obtiene el actor autenticado y solicita a IContrapartidaDispatchJobService procesar esa transacción; la implementación del servicio resuelve su ciclo y cámara desde la cola y ejecuta el flujo Proc_Contrapartidas mediante el cliente SOAP configurado.",
            Parameters: [],
            RequestSchema: "UatContrapartidasDispatchCycleRequest",
            RequestProperties: ["transactionId"],
            Responses:
            [
                new("200", "Resultado del procesamiento dirigido de la transacción, con ciclo, cámara y conteos de ejecución reportados por el servicio.", Qg2ResponseShape.Schema, "ContrapartidaCycleDispatchResult"),
                new("400", "TransactionId no es positivo; el controller devuelve un body anónimo con errorCode y message.", Qg2ResponseShape.Untyped),
                new("404", "La operación UAT no está habilitada y responde sin body, o no existe la transacción objetivo en la cola y se devuelve un body anónimo con errorCode.", Qg2ResponseShape.Untyped),
                new("409", "La transacción ya tiene un resultado funcional exitoso; el controller devuelve un body anónimo con errorCode y message.", Qg2ResponseShape.Untyped)
            ])
    ];

    private static readonly Qg2SchemaDocumentation[] Sc403AdditionalSchemaDocumentation =
    [
        new("SoapEndpointMethodMappingDto", ["methodName", "endpoint", "soapAction", "operatingMode", "timeoutSeconds", "enabled", "inputParameterMappings"]),
        new("SoapInputParameterMappingDto", ["inputName", "soapParameterName", "defaultValue", "required"]),
        new("ProcTransaccionesEffectiveSettingsDto", ["operation", "effectiveMode", "endpoint", "timeoutSeconds", "enabled", "mappingReady", "mappingIssueCode", "blockingParameters"])
    ];

    private static readonly Sc404Qg2Operation[] Sc404Qg2Operations =
    [
        new(
            "GET",
            "/api/scheduler/overview",
            ActionName: "GetOverview",
            Policy: "P1.SchedulerView",
            Summary: "Consultar resumen operativo del scheduler",
            Description: "Consolida las instancias registradas para el scheduler configurado, ejecuciones de las últimas 24 horas, tareas con próxima ejecución, metadata del scheduler Quartz y definiciones pendientes de sincronización. Los conteos se calculan con el estado persistido y la instancia Quartz activa al momento de la consulta.",
            Parameters: [],
            HasCorrelationId: false,
            RequestSchema: null,
            RequestProperties: [],
            Responses:
            [
                new("200", "Resumen operativo calculado para el scheduler configurado.", Qg2ResponseShape.Schema, "SchedulerOverviewDto")
            ],
            ResponseSchemaDocumentation: new(
                "SchedulerOverviewDto",
                ["totalInstances", "activeInstances", "offlineInstances", "runningJobs", "upcomingExecutions", "recentFailures", "recentMisfires", "schedulerName", "persistentStore", "clustered", "pendingSynchronizations"])),
        new(
            "GET",
            "/api/scheduler/tasks",
            ActionName: "GetTasks",
            Policy: "P1.SchedulerView",
            Summary: "Listar tareas del scheduler",
            Description: "Consulta las definiciones de tarea reconocidas por el catálogo del scheduler y las enriquece con su última ejecución, estado de ejecución actual, próxima ejecución y, cuando aplica, contextos operativos derivados de ciclos de compensación. Las definiciones sin entrada en el catálogo SchedulerTaskCatalog no se incluyen.",
            Parameters: [],
            HasCorrelationId: false,
            RequestSchema: null,
            RequestProperties: [],
            Responses:
            [
                new("200", "Tareas reconocidas por el catálogo del scheduler con su estado operativo actual.", Qg2ResponseShape.Collection, "SchedulerTaskDto")
            ],
            ResponseSchemaDocumentation: new(
                "SchedulerTaskDto",
                ["taskCode", "name", "description", "status", "clearingHouse", "scheduleDescription", "cronExpression", "timeZoneId", "misfirePolicy", "misfireDescription", "lastExecutionUtc", "nextExecutionUtc", "lastResult", "lastDurationMilliseconds", "lastSchedulerInstance", "currentState", "manualExecutionEnabled", "requestsRecovery", "allowsConcurrentExecution", "periodicityType", "n", "minute", "timeOfDay", "weeklyDay", "monthDay", "onlyBusinessDays", "startAt", "endAt", "synchronizationStatus", "lastSynchronizationError", "category", "processType", "soapService", "usesCycleSchedule", "canEditSchedule", "operationalContexts"])),
        new(
            "GET",
            "/api/scheduler/tasks/{taskCode}",
            ActionName: "GetTask",
            Policy: "P1.SchedulerView",
            Summary: "Consultar tarea del scheduler",
            Description: "Resuelve taskCode mediante SchedulerTaskCatalog y consulta la definición persistida asociada. La respuesta incluye programación, última y próxima ejecución, estado actual, sincronización y contextos operativos cuando la tarea depende de ciclos. Devuelve 404 cuando el código no está autorizado por el catálogo o no existe su definición persistida.",
            Parameters: [new("taskCode", "path", Required: true)],
            HasCorrelationId: false,
            RequestSchema: null,
            RequestProperties: [],
            Responses:
            [
                new("200", "Detalle operativo de la tarea solicitada.", Qg2ResponseShape.Schema, "SchedulerTaskDto"),
                new("404", "El código de tarea no está autorizado por el catálogo o no existe su definición persistida.", Qg2ResponseShape.NoBody)
            ]),
        new(
            "GET",
            "/api/scheduler/tasks/{taskCode}/technical",
            ActionName: "GetTechnicalInfo",
            Policy: "P1.SchedulerViewTechnical",
            Summary: "Consultar información técnica de tarea",
            Description: "Resuelve la tarea por su código público y devuelve el handler interno, claves Quartz, programación persistida, política de misfire, flags de recuperación/concurrencia, parámetros persistidos y triggers asociados al job. Devuelve 404 cuando el código no está autorizado por el catálogo o no existe su definición persistida.",
            Parameters: [new("taskCode", "path", Required: true)],
            HasCorrelationId: false,
            RequestSchema: null,
            RequestProperties: [],
            Responses:
            [
                new("200", "Información técnica de la tarea y sus triggers Quartz.", Qg2ResponseShape.Schema, "SchedulerTechnicalInfoDto"),
                new("404", "El código de tarea no está autorizado por el catálogo o no existe su definición persistida.", Qg2ResponseShape.NoBody)
            ],
            ResponseSchemaDocumentation: new(
                "SchedulerTechnicalInfoDto",
                ["taskCode", "handlerCode", "soapService", "jobName", "jobGroup", "cronExpression", "timeZoneId", "misfirePolicy", "requestsRecovery", "allowsConcurrentExecution", "parameters", "triggerKeys"])),
        new(
            "GET",
            "/api/scheduler/history",
            ActionName: "GetHistory",
            Policy: "P1.SchedulerHistoryView",
            Summary: "Consultar historial global del scheduler",
            Description: "Consulta ejecuciones persistidas del scheduler aplicando los filtros informados por estado, rango de inicio, tipo de trigger, instancia, usuario y correlación. Page se normaliza a mínimo 1, PageSize se limita al rango 1..100 y los resultados se ordenan desde la ejecución más reciente por StartedAt.",
            Parameters:
            [
                new("Page", "query"), new("PageSize", "query"), new("Status", "query"),
                new("FromUtc", "query"), new("ToUtc", "query"), new("TriggerType", "query"),
                new("InstanceId", "query"), new("UserName", "query"), new("CorrelationId", "query")
            ],
            HasCorrelationId: false,
            RequestSchema: null,
            RequestProperties: [],
            Responses:
            [
                new("200", "Página de ejecuciones del scheduler que cumplen los filtros solicitados.", Qg2ResponseShape.SchedulerPaged, "SchedulerExecutionDto")
            ]),
        new(
            "GET",
            "/api/scheduler/tasks/{taskCode}/history",
            ActionName: "GetTaskHistory",
            Policy: "P1.SchedulerHistoryView",
            Summary: "Consultar historial de una tarea",
            Description: "Consulta ejecuciones persistidas para la tarea identificada por taskCode y aplica los filtros informados. Si el código no existe en SchedulerTaskCatalog, devuelve una página vacía en lugar de 404. Page se normaliza a mínimo 1, PageSize se limita al rango 1..100 y los resultados se ordenan desde la ejecución más reciente por StartedAt.",
            Parameters:
            [
                new("taskCode", "path", Required: true), new("Page", "query"), new("PageSize", "query"),
                new("Status", "query"), new("FromUtc", "query"), new("ToUtc", "query"),
                new("TriggerType", "query"), new("InstanceId", "query"), new("UserName", "query"), new("CorrelationId", "query")
            ],
            HasCorrelationId: false,
            RequestSchema: null,
            RequestProperties: [],
            Responses:
            [
                new("200", "Página de ejecuciones de la tarea; puede ser vacía cuando el código no existe o no tiene ejecuciones coincidentes.", Qg2ResponseShape.SchedulerPaged, "SchedulerExecutionDto")
            ]),
        new(
            "GET",
            "/api/scheduler/instances",
            ActionName: "GetInstances",
            Policy: "P1.SchedulerViewInstances",
            Summary: "Listar instancias del scheduler",
            Description: "Lista las instancias persistidas cuyo SchedulerName coincide con el scheduler Quartz activo. El servicio calcula el estado reportado con los umbrales configurados de heartbeat y desconexión, identifica la instancia actual y expone el número persistido de trabajos en ejecución.",
            Parameters: [],
            HasCorrelationId: false,
            RequestSchema: null,
            RequestProperties: [],
            Responses:
            [
                new("200", "Instancias registradas para el scheduler Quartz activo.", Qg2ResponseShape.Collection, "SchedulerInstanceDto")
            ],
            ResponseSchemaDocumentation: new(
                "SchedulerInstanceDto",
                ["instanceId", "instanceName", "hostName", "startedAtUtc", "lastHeartbeatUtc", "status", "isCurrentInstance", "currentlyExecutingJobs", "version"])),
        new(
            "GET",
            "/api/scheduler/executions/{executionId}",
            ActionName: "GetExecution",
            Policy: "P1.SchedulerHistoryView",
            Summary: "Consultar ejecución del scheduler",
            Description: "Busca una ejecución persistida por ExecutionId y devuelve su información de disparo, instancia, solicitante, tiempos, estado, resultado, error y recuperación. Devuelve 404 cuando no existe una ejecución con el identificador indicado.",
            Parameters: [new("executionId", "path", Required: true)],
            HasCorrelationId: false,
            RequestSchema: null,
            RequestProperties: [],
            Responses:
            [
                new("200", "Detalle de la ejecución solicitada.", Qg2ResponseShape.Schema, "SchedulerExecutionDto"),
                new("404", "No existe una ejecución con el identificador indicado.", Qg2ResponseShape.NoBody)
            ],
            ResponseSchemaDocumentation: new(
                "SchedulerExecutionDto",
                ["executionId", "taskCode", "jobName", "jobGroup", "triggerName", "triggerType", "fireInstanceId", "schedulerInstanceId", "schedulerInstanceName", "requestedByUserId", "requestedByUserName", "requestReason", "requestId", "correlationId", "scheduledFireTimeUtc", "actualFireTimeUtc", "startedAtUtc", "finishedAtUtc", "durationMilliseconds", "status", "isRecovery", "refireCount", "misfireDetected", "resultSummary", "errorCode", "errorSummary", "originalFireInstanceId", "recoveredByInstanceId", "recoveryStartedAtUtc", "recoveryResult"])),
        new(
            "POST",
            "/api/scheduler/tasks/{taskCode}/execute",
            ActionName: "Execute",
            Policy: "P1.SchedulerExecute",
            Summary: "Ejecutar manualmente una tarea",
            Description: "Valida que RequestId sea distinto de Guid.Empty y que Reason, después de recortar espacios, tenga entre 10 y 500 caracteres. El servicio usa RequestId para detectar solicitudes ya registradas: una nueva ejecución aceptada devuelve 202; una solicitud duplicada devuelve 200; una tarea en conflicto o una solicitud rechazada por reglas operativas devuelve 409; y un taskCode no autorizado por el catálogo o sin definición persistida devuelve 404. El body ManualExecutionResult conserva el outcome y los identificadores disponibles para cada resultado.",
            Parameters: [new("taskCode", "path", Required: true)],
            HasCorrelationId: false,
            RequestSchema: "ExecuteSchedulerTaskRequest",
            RequestProperties: ["reason", "requestId"],
            Responses:
            [
                new("202", "La solicitud manual fue registrada y disparada hacia Quartz; el resultado incluye ExecutionId.", Qg2ResponseShape.Schema, "ManualExecutionResult"),
                new("200", "RequestId ya estaba registrado; se devuelve la ejecución asociada sin volver a disparar la tarea.", Qg2ResponseShape.Schema, "ManualExecutionResult"),
                new("400", "RequestId es Guid.Empty o Reason no cumple la longitud de 10 a 500 caracteres; se devuelve ValidationProblemDetails.", Qg2ResponseShape.ValidationProblemDetails, "ValidationProblemDetails"),
                new("404", "El código de tarea no está autorizado por el catálogo o no existe su definición persistida; se devuelve ManualExecutionResult con outcome NotFound.", Qg2ResponseShape.Schema, "ManualExecutionResult"),
                new("409", "La ejecución entra en conflicto con una ejecución activa no paralela o es rechazada porque la ejecución manual no está habilitada, la tarea está pausada/deshabilitada o no está disponible en Quartz.", Qg2ResponseShape.Schema, "ManualExecutionResult")
            ]),
        new(
            "POST",
            "/api/scheduler/tasks/{taskCode}/pause",
            ActionName: "Pause",
            Policy: "P1.SchedulerPauseResume",
            Summary: "Pausar tarea del scheduler",
            Description: "Marca la definición como pausada, deja la sincronización pendiente, solicita a Quartz pausar el job y registra la sincronización resultante. Si X-Correlation-ID está presente se usa para la auditoría de la modificación; en caso contrario se usa HttpContext.TraceIdentifier. Devuelve 404 cuando taskCode no corresponde a una tarea disponible.",
            Parameters: [new("taskCode", "path", Required: true)],
            HasCorrelationId: true,
            RequestSchema: null,
            RequestProperties: [],
            Responses:
            [
                new("204", "La tarea fue encontrada, marcada como pausada y la operación Quartz finalizó correctamente.", Qg2ResponseShape.NoBody),
                new("404", "El código de tarea no corresponde a una definición disponible para el scheduler.", Qg2ResponseShape.NoBody)
            ]),
        new(
            "POST",
            "/api/scheduler/tasks/{taskCode}/resume",
            ActionName: "Resume",
            Policy: "P1.SchedulerPauseResume",
            Summary: "Reanudar tarea del scheduler",
            Description: "Marca la definición como no pausada, sincroniza su configuración con Quartz, solicita reanudar el job y registra la sincronización resultante. Si X-Correlation-ID está presente se usa para la auditoría de la modificación; en caso contrario se usa HttpContext.TraceIdentifier. Devuelve 404 cuando taskCode no corresponde a una tarea disponible.",
            Parameters: [new("taskCode", "path", Required: true)],
            HasCorrelationId: true,
            RequestSchema: null,
            RequestProperties: [],
            Responses:
            [
                new("204", "La tarea fue encontrada, sincronizada y reanudada correctamente en Quartz.", Qg2ResponseShape.NoBody),
                new("404", "El código de tarea no corresponde a una definición disponible para el scheduler.", Qg2ResponseShape.NoBody)
            ]),
        new(
            "PUT",
            "/api/scheduler/tasks/{taskCode}/schedule",
            ActionName: "UpdateSchedule",
            Policy: "P1.SchedulerManageSchedule",
            Summary: "Actualizar programación de tarea",
            Description: "Rechaza con 400 las tareas cuya programación depende del ciclo de compensación y las programaciones inválidas: PeriodicityType o MisfirePolicy no definidos, StartAt no anterior a EndAt, TimeZoneId no reconocido por TimeZoneInfo, cron inválido para Quartz, intervalo EveryNMinutes fuera de 1..1440, minuto horario fuera de 0..59, hora no interpretable para periodicidades diarias/semanales/mensuales, día semanal ausente, día mensual fuera de 1..31 o StartAt ausente para Once/Yearly. También aplica el intervalo mínimo definido por SchedulerTaskCatalog cuando corresponda. Con una solicitud válida actualiza y sincroniza la tarea; devuelve 404 si taskCode no corresponde a una definición disponible. X-Correlation-ID se usa opcionalmente para la auditoría.",
            Parameters: [new("taskCode", "path", Required: true)],
            HasCorrelationId: true,
            RequestSchema: "SchedulerScheduleUpdateRequest",
            RequestProperties: ["periodicityType", "n", "minute", "timeOfDay", "weeklyDay", "monthDay", "cronExpression", "timeZoneId", "misfirePolicy", "onlyBusinessDays", "startAt", "endAt"],
            Responses:
            [
                new("200", "Programación persistida y sincronizada; se devuelve el estado operativo actualizado de la tarea.", Qg2ResponseShape.Schema, "SchedulerTaskDto"),
                new("400", "La programación no cumple una regla validada por el servicio; el controller conserva el body anónimo con la propiedad message.", Qg2ResponseShape.Untyped),
                new("404", "Con una programación válida, el código de tarea no corresponde a una definición disponible.", Qg2ResponseShape.NoBody)
            ]),
        new(
            "POST",
            "/api/scheduler/schedule/preview",
            ActionName: "PreviewSchedule",
            Policy: "P1.SchedulerManageSchedule",
            Summary: "Previsualizar programación del scheduler",
            Description: "Valida la programación con las mismas reglas generales usadas por la actualización —PeriodicityType, MisfirePolicy, fechas, TimeZoneId y campos requeridos por periodicidad— sin asociarla a una tarea ni persistir cambios. Para una programación válida devuelve una descripción y hasta cinco próximas ejecuciones calculadas por el servicio. Los errores ArgumentException se traducen a 400 conservando el body anónimo con message.",
            Parameters: [],
            HasCorrelationId: false,
            RequestSchema: "SchedulerScheduleUpdateRequest",
            RequestProperties: ["periodicityType", "n", "minute", "timeOfDay", "weeklyDay", "monthDay", "cronExpression", "timeZoneId", "misfirePolicy", "onlyBusinessDays", "startAt", "endAt"],
            Responses:
            [
                new("200", "Descripción de la programación y próximas ejecuciones calculadas sin persistencia.", Qg2ResponseShape.Schema, "SchedulerSchedulePreviewDto"),
                new("400", "La programación no cumple una regla validada por el servicio; el controller conserva el body anónimo con la propiedad message.", Qg2ResponseShape.Untyped)
            ],
            ResponseSchemaDocumentation: new(
                "SchedulerSchedulePreviewDto",
                ["description", "nextExecutionsUtc"]))
    ];

    private static readonly Qg2SchemaDocumentation[] Sc404AdditionalSchemaDocumentation =
    [
        new("SchedulerOperationalContextDto", ["cycleConfigId", "clearingHouseCode", "clearingHouseName", "cycleName", "windowDescription", "cutoffDescription", "nextValidWindowUtc", "nextValidWindowEndUtc", "status"]),
        new("ExecuteSchedulerTaskRequest", ["reason", "requestId"]),
        new("ManualExecutionResult", ["outcome", "executionId", "message", "activeExecutionId"]),
        new("SchedulerScheduleUpdateRequest", ["periodicityType", "n", "minute", "timeOfDay", "weeklyDay", "monthDay", "cronExpression", "timeZoneId", "misfirePolicy", "onlyBusinessDays", "startAt", "endAt"])
    ];

    private static readonly Sc405Qg2Operation[] Sc405Qg2Operations =
    [
        new(
            "GET",
            "/TaskDefinitions",
            ActionName: "Get",
            ActionRouteTemplate: null,
            Policy: "CanReadAch",
            Summary: "Listar definiciones de tarea",
            Description: "Consulta todas las definiciones de tarea persistidas, incluyendo sus parámetros, ordenadas por Id ascendente.",
            Parameters: [],
            RequestSchema: null,
            RequestProperties: [],
            Responses:
            [
                new("200", "Definiciones de tarea persistidas con sus parámetros, ordenadas por Id ascendente.", Qg2ResponseShape.Collection, "TaskDefinitionDto")
            ],
            ResponseSchemaDocumentation: new(
                "TaskDefinitionDto",
                ["id", "code", "name", "description", "status", "calendarPolicy", "timeZoneId", "concurrencyPolicy", "retryOnFailure", "maxRetries", "retryBackoffSeconds", "misfirePolicy", "requestsRecovery", "manualExecutionEnabled", "paused", "periodicityType", "n", "minute", "timeOfDay", "weeklyDay", "monthDay", "cronExpression", "startAt", "endAt", "parameters"])),
        new(
            "GET",
            "/TaskDefinitions/{id}",
            ActionName: "Get",
            ActionRouteTemplate: "{id}",
            Policy: "CanReadAch",
            Summary: "Consultar definición de tarea",
            Description: "Consulta la definición de tarea persistida identificada por id e incluye sus parámetros. Devuelve 404 cuando no existe una definición con ese identificador.",
            Parameters: [new("id", "path", Required: true)],
            RequestSchema: null,
            RequestProperties: [],
            Responses:
            [
                new("200", "Definición de tarea encontrada con sus parámetros persistidos.", Qg2ResponseShape.Schema, "TaskDefinitionDto"),
                new("404", "No existe una definición de tarea con el identificador indicado; la respuesta no tiene body.", Qg2ResponseShape.NoBody)
            ]),
        new(
            "POST",
            "/TaskDefinitions",
            ActionName: "Post",
            ActionRouteTemplate: null,
            Policy: "CanManageAch",
            Summary: "Crear definición de tarea",
            Description: "Crea una definición nueva a partir del body. El servicio recorta Code y Name; convierte Description, TimeZoneId y CronExpression en null cuando contienen solo espacios; intenta convertir TimeOfDay a TimeOnly y persiste null si no puede interpretarlo; descarta parámetros con Key vacío y recorta Key y Value. La respuesta se emite con CreatedAtAction y la definición persistida.",
            Parameters: [],
            RequestSchema: "TaskDefinitionDto",
            RequestProperties: ["id", "code", "name", "description", "status", "calendarPolicy", "timeZoneId", "concurrencyPolicy", "retryOnFailure", "maxRetries", "retryBackoffSeconds", "misfirePolicy", "requestsRecovery", "manualExecutionEnabled", "paused", "periodicityType", "n", "minute", "timeOfDay", "weeklyDay", "monthDay", "cronExpression", "startAt", "endAt", "parameters"],
            Responses:
            [
                new("201", "Definición de tarea creada y devuelta con la ubicación de consulta por identificador.", Qg2ResponseShape.Schema, "TaskDefinitionDto")
            ]),
        new(
            "PUT",
            "/TaskDefinitions/{id}",
            ActionName: "Put",
            ActionRouteTemplate: "{id}",
            Policy: "CanManageAch",
            Summary: "Actualizar definición de tarea",
            Description: "Exige que el id de la ruta coincida con TaskDefinitionDto.Id; si difiere responde 400 sin body. Si la definición no existe responde 404. Para una definición existente aplica la misma normalización de textos y TimeOfDay usada en creación. En Parameters, los elementos con Key vacío se descartan; los Id existentes se actualizan, Id 0 se agrega como parámetro nuevo y los parámetros persistidos que no aparecen con su Id se eliminan.",
            Parameters: [new("id", "path", Required: true)],
            RequestSchema: "TaskDefinitionDto",
            RequestProperties: ["id", "code", "name", "description", "status", "calendarPolicy", "timeZoneId", "concurrencyPolicy", "retryOnFailure", "maxRetries", "retryBackoffSeconds", "misfirePolicy", "requestsRecovery", "manualExecutionEnabled", "paused", "periodicityType", "n", "minute", "timeOfDay", "weeklyDay", "monthDay", "cronExpression", "startAt", "endAt", "parameters"],
            Responses:
            [
                new("200", "Definición de tarea actualizada con sus parámetros persistidos.", Qg2ResponseShape.Schema, "TaskDefinitionDto"),
                new("400", "El id de la ruta no coincide con el Id del body; la respuesta no tiene body.", Qg2ResponseShape.NoBody),
                new("404", "No existe una definición de tarea con el identificador indicado; la respuesta no tiene body.", Qg2ResponseShape.NoBody)
            ]),
        new(
            "DELETE",
            "/TaskDefinitions/{id}",
            ActionName: "Delete",
            ActionRouteTemplate: "{id}",
            Policy: "CanManageAch",
            Summary: "Eliminar definición de tarea",
            Description: "Elimina la definición persistida identificada por id. Devuelve 204 cuando la entidad fue eliminada y 404 cuando no existe una definición con ese identificador.",
            Parameters: [new("id", "path", Required: true)],
            RequestSchema: null,
            RequestProperties: [],
            Responses:
            [
                new("204", "La definición de tarea fue eliminada; la respuesta no tiene body.", Qg2ResponseShape.NoBody),
                new("404", "No existe una definición de tarea con el identificador indicado; la respuesta no tiene body.", Qg2ResponseShape.NoBody)
            ])
    ];

    private static readonly Qg2SchemaDocumentation[] Sc405AdditionalSchemaDocumentation =
    [
        new("TaskParameterDto", ["id", "key", "value"])
    ];

    private static readonly Sc406Qg2Operation[] Sc406Qg2Operations =
    [
        new(
            "GET",
            "/api/reports/transactions/sent",
            ActionName: "GetSentTransactions",
            ActionRouteTemplate: "transactions/sent",
            Summary: "Reporte de transacciones enviadas",
            Description: "Consulta transacciones enviadas aplicando los filtros opcionales publicados. El servicio excluye las transacciones en estado ReturnedByOperator o ReturnedByEpr y aquellas con ReturnReasonCode; bankId filtra SourceInstitutionId. date compara la fecha de EffectiveEntryDate, achCycleId se recorta y compara por igualdad, reference busca por contenido en Reference o TransactionExternalId y page/pageSize se normalizan a 1/25 cuando son menores o iguales a cero, con pageSize máximo de 200. clearingHouseId, cuando se informa, debe ser mayor que cero y corresponder a una cámara existente; en caso contrario la acción responde 400 con un body anónimo que contiene message.",
            Parameters: [new("date", "query"), new("clearingHouseId", "query"), new("achCycleId", "query"), new("state", "query"), new("reference", "query"), new("bankId", "query"), new("transactionType", "query"), new("page", "query"), new("pageSize", "query")],
            Responses:
            [
                new("200", "Reporte paginado de transacciones enviadas con items, totales y paginación efectiva.", Qg2ResponseShape.Schema, "AchTransactionReportResponseDto"),
                new("400", "clearingHouseId fue informado con valor menor o igual a cero o no corresponde a una cámara existente; el body anónimo contiene message.", Qg2ResponseShape.Untyped)
            ]),
        new(
            "GET",
            "/api/reports/transactions/received",
            ActionName: "GetReceivedTransactions",
            ActionRouteTemplate: "transactions/received",
            Summary: "Reporte de transacciones recibidas",
            Description: "Consulta transacciones recibidas aplicando los filtros opcionales publicados. bankId filtra DestinationInstitutionId; date compara la fecha de EffectiveEntryDate, achCycleId se recorta y compara por igualdad, reference busca por contenido en Reference o TransactionExternalId y page/pageSize se normalizan a 1/25 cuando son menores o iguales a cero, con pageSize máximo de 200. clearingHouseId, cuando se informa, debe ser mayor que cero y corresponder a una cámara existente; en caso contrario la acción responde 400 con un body anónimo que contiene message.",
            Parameters: [new("date", "query"), new("clearingHouseId", "query"), new("achCycleId", "query"), new("state", "query"), new("reference", "query"), new("bankId", "query"), new("transactionType", "query"), new("page", "query"), new("pageSize", "query")],
            Responses:
            [
                new("200", "Reporte paginado de transacciones recibidas con items, totales y paginación efectiva.", Qg2ResponseShape.Schema, "AchTransactionReportResponseDto"),
                new("400", "clearingHouseId fue informado con valor menor o igual a cero o no corresponde a una cámara existente; el body anónimo contiene message.", Qg2ResponseShape.Untyped)
            ]),
        new(
            "GET",
            "/api/reports/transactions/sent/pdf",
            ActionName: "GetSentTransactionsPdf",
            ActionRouteTemplate: "transactions/sent/pdf",
            Summary: "Descargar PDF de transacciones enviadas",
            Description: "Genera un PDF de transacciones enviadas con los mismos filtros de la consulta JSON. El generador solicita la primera página con PageSize=5000, pero el servicio normaliza pageSize a un máximo de 200; los totales se calculan sobre todo el conjunto filtrado. La respuesta exitosa usa application/pdf y el nombre ACH_Sent_<timestamp UTC yyyyMMdd_HHmmss>.pdf; File(...) emite Content-Disposition usando ese nombre. clearingHouseId inválido o inexistente produce 400 con body anónimo { message }.",
            Parameters: [new("date", "query"), new("clearingHouseId", "query"), new("achCycleId", "query"), new("state", "query"), new("reference", "query"), new("bankId", "query"), new("transactionType", "query")],
            Responses:
            [
                new("200", "Archivo PDF application/pdf; Content-Disposition usa el nombre ACH_Sent_<timestamp UTC yyyyMMdd_HHmmss>.pdf generado por el servicio.", Qg2ResponseShape.Untyped),
                new("400", "clearingHouseId fue informado con valor menor o igual a cero o no corresponde a una cámara existente; el body anónimo contiene message.", Qg2ResponseShape.Untyped)
            ],
            PdfFileNameFragment: "ACH_Sent_<timestamp UTC yyyyMMdd_HHmmss>.pdf"),
        new(
            "GET",
            "/api/reports/transactions/received/pdf",
            ActionName: "GetReceivedTransactionsPdf",
            ActionRouteTemplate: "transactions/received/pdf",
            Summary: "Descargar PDF de transacciones recibidas",
            Description: "Genera un PDF de transacciones recibidas con los mismos filtros de la consulta JSON. El generador solicita la primera página con PageSize=5000, pero el servicio normaliza pageSize a un máximo de 200; los totales se calculan sobre todo el conjunto filtrado. La respuesta exitosa usa application/pdf y el nombre ACH_Received_<timestamp UTC yyyyMMdd_HHmmss>.pdf; File(...) emite Content-Disposition usando ese nombre. clearingHouseId inválido o inexistente produce 400 con body anónimo { message }.",
            Parameters: [new("date", "query"), new("clearingHouseId", "query"), new("achCycleId", "query"), new("state", "query"), new("reference", "query"), new("bankId", "query"), new("transactionType", "query")],
            Responses:
            [
                new("200", "Archivo PDF application/pdf; Content-Disposition usa el nombre ACH_Received_<timestamp UTC yyyyMMdd_HHmmss>.pdf generado por el servicio.", Qg2ResponseShape.Untyped),
                new("400", "clearingHouseId fue informado con valor menor o igual a cero o no corresponde a una cámara existente; el body anónimo contiene message.", Qg2ResponseShape.Untyped)
            ],
            PdfFileNameFragment: "ACH_Received_<timestamp UTC yyyyMMdd_HHmmss>.pdf"),
        new(
            "GET",
            "/api/reports/returns",
            ActionName: "GetReturns",
            ActionRouteTemplate: "returns",
            Summary: "Reporte de devoluciones",
            Description: "Consulta transacciones cuyo ReturnReasonCode coincide con R__ o DEV__ según los patrones SQL implementados. date compara StateChangedAtUtc.Date; causal se recorta, se convierte a mayúsculas y se compara por igualdad; reference busca por contenido en Reference o TransactionExternalId. page/pageSize se normalizan a 1/25 cuando son menores o iguales a cero y pageSize se limita a 200. clearingHouseId, cuando se informa, debe ser mayor que cero y existir; en caso contrario responde 400 con body anónimo { message }.",
            Parameters: [new("date", "query"), new("causal", "query"), new("clearingHouseId", "query"), new("state", "query"), new("reference", "query"), new("page", "query"), new("pageSize", "query")],
            Responses:
            [
                new("200", "Reporte paginado de devoluciones con items, totales y paginación efectiva.", Qg2ResponseShape.Schema, "AchReturnRejectionReportResponseDto"),
                new("400", "clearingHouseId fue informado con valor menor o igual a cero o no corresponde a una cámara existente; el body anónimo contiene message.", Qg2ResponseShape.Untyped)
            ]),
        new(
            "GET",
            "/api/reports/rejections",
            ActionName: "GetRejections",
            ActionRouteTemplate: "rejections",
            Summary: "Reporte de rechazos",
            Description: "Consulta transacciones cuyo ReturnReasonCode coincide con el patrón SQL D__ implementado. date compara StateChangedAtUtc.Date; causal se recorta, se convierte a mayúsculas y se compara por igualdad; reference busca por contenido en Reference o TransactionExternalId. page/pageSize se normalizan a 1/25 cuando son menores o iguales a cero y pageSize se limita a 200. clearingHouseId, cuando se informa, debe ser mayor que cero y existir; en caso contrario responde 400 con body anónimo { message }.",
            Parameters: [new("date", "query"), new("causal", "query"), new("clearingHouseId", "query"), new("state", "query"), new("reference", "query"), new("page", "query"), new("pageSize", "query")],
            Responses:
            [
                new("200", "Reporte paginado de rechazos con items, totales y paginación efectiva.", Qg2ResponseShape.Schema, "AchReturnRejectionReportResponseDto"),
                new("400", "clearingHouseId fue informado con valor menor o igual a cero o no corresponde a una cámara existente; el body anónimo contiene message.", Qg2ResponseShape.Untyped)
            ]),
        new(
            "GET",
            "/api/reports/returns/pdf",
            ActionName: "GetReturnsPdf",
            ActionRouteTemplate: "returns/pdf",
            Summary: "Descargar PDF de devoluciones",
            Description: "Genera un PDF de devoluciones usando los filtros de la consulta correspondiente. El generador solicita Page=1 y PageSize=5000, pero el servicio limita pageSize a 200; los totales representan todo el conjunto filtrado. Responde application/pdf con nombre ACH_Returns_<timestamp UTC yyyyMMdd_HHmmss>.pdf y File(...) emite Content-Disposition usando ese nombre. clearingHouseId inválido o inexistente produce 400 con body anónimo { message }.",
            Parameters: [new("date", "query"), new("causal", "query"), new("clearingHouseId", "query"), new("state", "query"), new("reference", "query")],
            Responses:
            [
                new("200", "Archivo PDF application/pdf; Content-Disposition usa el nombre ACH_Returns_<timestamp UTC yyyyMMdd_HHmmss>.pdf generado por el servicio.", Qg2ResponseShape.Untyped),
                new("400", "clearingHouseId fue informado con valor menor o igual a cero o no corresponde a una cámara existente; el body anónimo contiene message.", Qg2ResponseShape.Untyped)
            ],
            PdfFileNameFragment: "ACH_Returns_<timestamp UTC yyyyMMdd_HHmmss>.pdf"),
        new(
            "GET",
            "/api/reports/rejections/pdf",
            ActionName: "GetRejectionsPdf",
            ActionRouteTemplate: "rejections/pdf",
            Summary: "Descargar PDF de rechazos",
            Description: "Genera un PDF de rechazos usando los filtros de la consulta correspondiente. El generador solicita Page=1 y PageSize=5000, pero el servicio limita pageSize a 200; los totales representan todo el conjunto filtrado. Responde application/pdf con nombre ACH_Rejections_<timestamp UTC yyyyMMdd_HHmmss>.pdf y File(...) emite Content-Disposition usando ese nombre. clearingHouseId inválido o inexistente produce 400 con body anónimo { message }.",
            Parameters: [new("date", "query"), new("causal", "query"), new("clearingHouseId", "query"), new("state", "query"), new("reference", "query")],
            Responses:
            [
                new("200", "Archivo PDF application/pdf; Content-Disposition usa el nombre ACH_Rejections_<timestamp UTC yyyyMMdd_HHmmss>.pdf generado por el servicio.", Qg2ResponseShape.Untyped),
                new("400", "clearingHouseId fue informado con valor menor o igual a cero o no corresponde a una cámara existente; el body anónimo contiene message.", Qg2ResponseShape.Untyped)
            ],
            PdfFileNameFragment: "ACH_Rejections_<timestamp UTC yyyyMMdd_HHmmss>.pdf"),
        new(
            "GET",
            "/api/reports/nacha-files",
            ActionName: "GetNachaFiles",
            ActionRouteTemplate: "nacha-files",
            Summary: "Reporte de archivos NACHA",
            Description: "Consulta registros de exportación de archivos NACHA. date compara GeneratedAtUtc.Date y clearingHouseId filtra la cámara asociada. page/pageSize se normalizan a 1/25 cuando son menores o iguales a cero y pageSize se limita a 200. Los totales se calculan sobre todo el conjunto filtrado antes de paginar. clearingHouseId, cuando se informa, debe ser mayor que cero y existir; en caso contrario responde 400 con body anónimo { message }.",
            Parameters: [new("date", "query"), new("clearingHouseId", "query"), new("page", "query"), new("pageSize", "query")],
            Responses:
            [
                new("200", "Reporte paginado de archivos NACHA con items, totales y paginación efectiva.", Qg2ResponseShape.Schema, "AchNachaFileReportResponseDto"),
                new("400", "clearingHouseId fue informado con valor menor o igual a cero o no corresponde a una cámara existente; el body anónimo contiene message.", Qg2ResponseShape.Untyped)
            ]),
        new(
            "GET",
            "/api/reports/nacha-files/pdf",
            ActionName: "GetNachaFilesPdf",
            ActionRouteTemplate: "nacha-files/pdf",
            Summary: "Descargar PDF de archivos NACHA",
            Description: "Genera un PDF de archivos NACHA con filtros opcionales por fecha y cámara. El generador solicita Page=1 y PageSize=5000, pero el servicio limita pageSize a 200; los totales representan todo el conjunto filtrado. Responde application/pdf con nombre ACH_NachaFiles_<timestamp UTC yyyyMMdd_HHmmss>.pdf y File(...) emite Content-Disposition usando ese nombre. clearingHouseId inválido o inexistente produce 400 con body anónimo { message }.",
            Parameters: [new("date", "query"), new("clearingHouseId", "query")],
            Responses:
            [
                new("200", "Archivo PDF application/pdf; Content-Disposition usa el nombre ACH_NachaFiles_<timestamp UTC yyyyMMdd_HHmmss>.pdf generado por el servicio.", Qg2ResponseShape.Untyped),
                new("400", "clearingHouseId fue informado con valor menor o igual a cero o no corresponde a una cámara existente; el body anónimo contiene message.", Qg2ResponseShape.Untyped)
            ],
            PdfFileNameFragment: "ACH_NachaFiles_<timestamp UTC yyyyMMdd_HHmmss>.pdf"),
        new(
            "GET",
            "/api/reports/cycles",
            ActionName: "GetCycles",
            ActionRouteTemplate: "cycles",
            Summary: "Reporte de ciclos ACH",
            Description: "Consulta ciclos ACH. date compara ProcessingDate.Date, clearingHouseId filtra la cámara y name se recorta antes de buscarlo por contenido en CycleName. page/pageSize se normalizan a 1/25 cuando son menores o iguales a cero y pageSize se limita a 200. Los totales de transacciones y monto se calculan sobre todos los ciclos filtrados antes de paginar; cada item deriva Status como Programado, En ejecución o Cerrado comparando DateTime.UtcNow con StartTime/EndTime del ciclo, considerando fin al día siguiente cuando EndTime es menor que StartTime. clearingHouseId inválido o inexistente produce 400 con body anónimo { message }.",
            Parameters: [new("date", "query"), new("clearingHouseId", "query"), new("name", "query"), new("page", "query"), new("pageSize", "query")],
            Responses:
            [
                new("200", "Reporte paginado de ciclos ACH con items, totales y paginación efectiva.", Qg2ResponseShape.Schema, "AchCycleReportResponseDto"),
                new("400", "clearingHouseId fue informado con valor menor o igual a cero o no corresponde a una cámara existente; el body anónimo contiene message.", Qg2ResponseShape.Untyped)
            ]),
        new(
            "GET",
            "/api/reports/cycles/pdf",
            ActionName: "GetCyclesPdf",
            ActionRouteTemplate: "cycles/pdf",
            Summary: "Descargar PDF de ciclos ACH",
            Description: "Genera un PDF de ciclos ACH con filtros opcionales por fecha, cámara y nombre. El generador solicita Page=1 y PageSize=5000, pero el servicio limita pageSize a 200; los totales representan todos los ciclos filtrados. Responde application/pdf con nombre ACH_Cycles_<timestamp UTC yyyyMMdd_HHmmss>.pdf y File(...) emite Content-Disposition usando ese nombre. clearingHouseId inválido o inexistente produce 400 con body anónimo { message }.",
            Parameters: [new("date", "query"), new("clearingHouseId", "query"), new("name", "query")],
            Responses:
            [
                new("200", "Archivo PDF application/pdf; Content-Disposition usa el nombre ACH_Cycles_<timestamp UTC yyyyMMdd_HHmmss>.pdf generado por el servicio.", Qg2ResponseShape.Untyped),
                new("400", "clearingHouseId fue informado con valor menor o igual a cero o no corresponde a una cámara existente; el body anónimo contiene message.", Qg2ResponseShape.Untyped)
            ],
            PdfFileNameFragment: "ACH_Cycles_<timestamp UTC yyyyMMdd_HHmmss>.pdf")
    ];

    private static readonly Qg2SchemaDocumentation[] Sc406SchemaDocumentation =
    [
        new("AchTransactionReportResponseDto", ["items", "totals", "page", "pageSize", "total"]),
        new("AchTransactionReportRowDto", ["transactionId", "effectiveEntryDate", "transactionExternalId", "reference", "amount", "transactionType", "state", "clearingHouseName", "achCycleId", "achCycleName", "batchId", "batchSequenceNumber", "sourceBankName", "destinationBankName", "nachaFileName"]),
        new("AchTransactionReportTotalsDto", ["totalRecords", "totalCreditAmount", "totalDebitAmount"]),
        new("AchReturnRejectionReportResponseDto", ["items", "totals", "page", "pageSize", "total"]),
        new("AchReturnRejectionReportRowDto", ["transactionId", "effectiveEntryDate", "transactionExternalId", "reference", "amount", "state", "causalCode", "causalDescription", "clearingHouseName", "achCycleId", "achCycleName", "originalTraceRef", "originalTransactionId", "originalTransactionReference"]),
        new("AchReturnRejectionReportTotalsDto", ["totalRecords", "totalAmount"]),
        new("AchNachaFileReportResponseDto", ["items", "totals", "page", "pageSize", "total"]),
        new("AchNachaFileReportRowDto", ["fileName", "generatedAtUtc", "clearingHouseName", "exportKind", "totalRecords", "totalTransactions"]),
        new("AchNachaFileReportTotalsDto", ["totalFiles", "totalRecords", "totalTransactions"]),
        new("AchCycleReportResponseDto", ["items", "totals", "page", "pageSize", "total"]),
        new("AchCycleReportRowDto", ["cycleId", "cycleName", "processingDate", "startTime", "endTime", "cutoffTime", "schedule", "status", "clearingHouseName", "totalTransactions", "totalAmount"]),
        new("AchCycleReportTotalsDto", ["totalCycles", "totalTransactions", "totalAmount"])
    ];

    private static readonly Sc407Qg2Operation[] Sc407Qg2Operations =
    [
        new(
            "GET",
            "/api/reports/reconciliation",
            ActionName: "GetReconciliation",
            ActionRouteTemplate: "reconciliation",
            Summary: "Reporte de conciliación ACH",
            Description: "Consulta totales, diferencias e inconsistencias de conciliación sobre transacciones ACH. date compara EffectiveEntryDate.Date; clearingHouseId filtra AchCycle.ClearingHouseId y, cuando se informa, debe ser mayor que cero y corresponder a una cámara existente; achCycleId se recorta y compara por igualdad. Los totales enviados excluyen Type=Return, los recibidos incluyen estados Certified o AppliedTacitly y los devueltos incluyen ReturnedByOperator o ReturnedByEpr. Las inconsistencias actuales detectan devoluciones sin causal, causal registrada en transacciones no devueltas y montos negativos. clearingHouseId inválido o inexistente produce 400 con body anónimo { message }.",
            Parameters: [new("date", "query"), new("clearingHouseId", "query"), new("achCycleId", "query")],
            RequestSchema: null,
            RequestProperties: [],
            Responses:
            [
                new("200", "Totales enviados, recibidos y devueltos, diferencias entre esos totales e inconsistencias detectadas para los filtros solicitados.", Qg2ResponseShape.Schema, "AchReconciliationReportResponseDto"),
                new("400", "clearingHouseId fue informado con valor menor o igual a cero o no corresponde a una cámara existente; el body anónimo contiene message.", Qg2ResponseShape.Untyped)
            ]),
        new(
            "GET",
            "/api/reports/reconciliation/pdf",
            ActionName: "GetReconciliationPdf",
            ActionRouteTemplate: "reconciliation/pdf",
            Summary: "Descargar PDF de conciliación ACH",
            Description: "Genera el PDF de conciliación con los mismos filtros del reporte JSON. Responde application/pdf con nombre ACH_Reconciliation_<timestamp UTC yyyyMMdd_HHmmss>.pdf y File(...) emite Content-Disposition usando ese nombre. clearingHouseId inválido o inexistente produce 400 con body anónimo { message }.",
            Parameters: [new("date", "query"), new("clearingHouseId", "query"), new("achCycleId", "query")],
            RequestSchema: null,
            RequestProperties: [],
            Responses:
            [
                new("200", "Archivo PDF application/pdf; Content-Disposition usa el nombre ACH_Reconciliation_<timestamp UTC yyyyMMdd_HHmmss>.pdf generado por el servicio.", Qg2ResponseShape.Untyped),
                new("400", "clearingHouseId fue informado con valor menor o igual a cero o no corresponde a una cámara existente; el body anónimo contiene message.", Qg2ResponseShape.Untyped)
            ],
            SuccessMediaTypes: ["application/pdf"],
            FileNameFragment: "ACH_Reconciliation_<timestamp UTC yyyyMMdd_HHmmss>.pdf"),
        new(
            "GET",
            "/api/reports/audit",
            ActionName: "GetAudit",
            ActionRouteTemplate: "audit",
            Summary: "Reporte de auditoría ACH",
            Description: "Consulta registros de auditoría. user, action y entity se recortan y buscan por contenido; fromUtc y toUtc delimitan ChangedAt de forma inclusiva. page/pageSize se normalizan a 1/25 cuando son menores o iguales a cero y pageSize se limita a 200. Los resultados se ordenan por ChangedAt descendente.",
            Parameters: [new("user", "query"), new("action", "query"), new("entity", "query"), new("fromUtc", "query"), new("toUtc", "query"), new("page", "query"), new("pageSize", "query")],
            RequestSchema: null,
            RequestProperties: [],
            Responses:
            [
                new("200", "Página de registros de auditoría con paginación efectiva y total de coincidencias.", Qg2ResponseShape.Schema, "AchAuditReportResponseDto")
            ]),
        new(
            "GET",
            "/api/reports/audit/pdf",
            ActionName: "GetAuditPdf",
            ActionRouteTemplate: "audit/pdf",
            Summary: "Descargar PDF de auditoría ACH",
            Description: "Genera un PDF de auditoría con filtros opcionales por usuario, acción, entidad e intervalo UTC. El generador solicita Page=1 y PageSize=5000, pero el servicio limita pageSize a 200. Responde application/pdf con nombre ACH_Audit_<timestamp UTC yyyyMMdd_HHmmss>.pdf y File(...) emite Content-Disposition usando ese nombre.",
            Parameters: [new("user", "query"), new("action", "query"), new("entity", "query"), new("fromUtc", "query"), new("toUtc", "query")],
            RequestSchema: null,
            RequestProperties: [],
            Responses:
            [
                new("200", "Archivo PDF application/pdf; Content-Disposition usa el nombre ACH_Audit_<timestamp UTC yyyyMMdd_HHmmss>.pdf generado por el servicio.", Qg2ResponseShape.Untyped)
            ],
            SuccessMediaTypes: ["application/pdf"],
            FileNameFragment: "ACH_Audit_<timestamp UTC yyyyMMdd_HHmmss>.pdf"),
        new(
            "GET",
            "/api/reports/history",
            ActionName: "GetHistory",
            ActionRouteTemplate: "history",
            Summary: "Histórico de cambios de estado ACH",
            Description: "Consulta eventos de cambio de estado ACH filtrables por intervalo UTC, transactionId, estado destino y origen. fromUtc/toUtc se aplican inclusivamente sobre CreatedAt; page/pageSize se normalizan a 1/25 cuando son menores o iguales a cero y pageSize se limita a 200. Los resultados se ordenan por CreatedAt descendente.",
            Parameters: [new("fromUtc", "query"), new("toUtc", "query"), new("transactionId", "query"), new("toState", "query"), new("source", "query"), new("page", "query"), new("pageSize", "query")],
            RequestSchema: null,
            RequestProperties: [],
            Responses:
            [
                new("200", "Página de eventos de cambio de estado ACH con paginación efectiva y total de coincidencias.", Qg2ResponseShape.Schema, "AchHistoryReportResponseDto")
            ]),
        new(
            "GET",
            "/api/reports/history/pdf",
            ActionName: "GetHistoryPdf",
            ActionRouteTemplate: "history/pdf",
            Summary: "Descargar PDF de histórico ACH",
            Description: "Genera un PDF del histórico de cambios de estado con filtros opcionales por intervalo UTC, transactionId, estado destino y origen. El generador solicita Page=1 y PageSize=5000, pero el servicio limita pageSize a 200. Responde application/pdf con nombre ACH_History_<timestamp UTC yyyyMMdd_HHmmss>.pdf y File(...) emite Content-Disposition usando ese nombre.",
            Parameters: [new("fromUtc", "query"), new("toUtc", "query"), new("transactionId", "query"), new("toState", "query"), new("source", "query")],
            RequestSchema: null,
            RequestProperties: [],
            Responses:
            [
                new("200", "Archivo PDF application/pdf; Content-Disposition usa el nombre ACH_History_<timestamp UTC yyyyMMdd_HHmmss>.pdf generado por el servicio.", Qg2ResponseShape.Untyped)
            ],
            SuccessMediaTypes: ["application/pdf"],
            FileNameFragment: "ACH_History_<timestamp UTC yyyyMMdd_HHmmss>.pdf"),
        new(
            "GET",
            "/api/reports/traceability/pdf",
            ActionName: "GetTraceabilityPdf",
            ActionRouteTemplate: "traceability/pdf",
            Summary: "Descargar PDF de trazabilidad ACH",
            Description: "Genera un PDF de trazabilidad con rango UTC, estado y ciclo opcionales. Sin fechas usa un rango de 7 días terminado en DateTime.UtcNow; si falta solo un extremo completa 31 días desde el extremo informado. Rechaza con 400 cuando fromUtc es posterior a toUtc o cuando el rango supera 31 días. La generación tiene timeout interno de 30 segundos: si vence sin cancelación del cliente responde 408; una excepción no controlada por el generador se traduce a 500. El éxito usa application/pdf con nombre ACH_Traceability_<timestamp UTC yyyyMMdd_HHmmss>.pdf y File(...) emite Content-Disposition usando ese nombre.",
            Parameters: [new("fromUtc", "query"), new("toUtc", "query"), new("state", "query"), new("achCycleId", "query")],
            RequestSchema: null,
            RequestProperties: [],
            Responses:
            [
                new("200", "Archivo PDF application/pdf; Content-Disposition usa el nombre ACH_Traceability_<timestamp UTC yyyyMMdd_HHmmss>.pdf generado por el servicio.", Qg2ResponseShape.Untyped),
                new("400", "El rango es inválido porque fromUtc es posterior a toUtc o porque supera el máximo de 31 días; el body anónimo contiene message.", Qg2ResponseShape.Untyped),
                new("408", "La generación excedió el timeout interno de 30 segundos sin cancelación del cliente; el body anónimo contiene message.", Qg2ResponseShape.Untyped),
                new("500", "La generación lanzó una excepción capturada por la acción; el body anónimo contiene message.", Qg2ResponseShape.Untyped)
            ],
            SuccessMediaTypes: ["application/pdf"],
            FileNameFragment: "ACH_Traceability_<timestamp UTC yyyyMMdd_HHmmss>.pdf"),
        new(
            "POST",
            "/api/reports/accounting-review/export",
            ActionName: "ExportAccountingReview",
            ActionRouteTemplate: "accounting-review/export",
            Summary: "Exportar revisión contable operativa",
            Description: "Genera en memoria una exportación de revisión operativa contra terceros; no contabiliza ni crea asientos. El body es AccountingReviewExportApiRequest. Format se recorta y compara sin distinguir mayúsculas: pdf produce application/pdf, csv produce text/csv y excel/xlsx producen application/vnd.openxmlformats-officedocument.spreadsheetml.sheet. El nombre de descarga es accounting-review-reconciliation-<timestamp UTC yyyyMMddHHmmss> con extensión .pdf, .csv o .xlsx. Body nulo, formato no soportado, ArgumentException del servicio o contenido generado vacío producen 400 con body anónimo { message }.",
            Parameters: [],
            RequestSchema: "AccountingReviewExportApiRequest",
            RequestProperties: ["format", "dateFrom", "dateTo", "clearingHouseId", "clearingHouseCode", "cycleId", "cycleName", "fileId", "fileName", "fileHash", "transactionId", "status", "causeCode", "includeOutbound", "includeIncoming", "includeReturns", "includeReturnOfReturn", "includeOrphans", "includeManualAuditOnly", "includeNetting", "includeLiquidity", "includeCudEvidence", "includeRows", "includeDifferences", "includeEvidence", "includeBoundaryFlags", "includeWarnings", "includeSummary", "includeScope", "requestedBy", "correlationId", "csvDelimiter"],
            Responses:
            [
                new("200", "Archivo generado por File(...); Content-Type depende de Format y Content-Disposition usa accounting-review-reconciliation-<timestamp UTC yyyyMMddHHmmss> con extensión .pdf, .csv o .xlsx.", Qg2ResponseShape.Untyped),
                new("400", "El body es nulo, el formato no es pdf/csv/excel/xlsx, el servicio produjo ArgumentException o no generó contenido; el body anónimo contiene message.", Qg2ResponseShape.Untyped)
            ],
            SuccessMediaTypes: ["application/pdf", "text/csv", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"],
            FileNameFragment: "accounting-review-reconciliation-<timestamp UTC yyyyMMddHHmmss>")
    ];

    private static readonly Qg2SchemaDocumentation[] Sc407SchemaDocumentation =
    [
        new("AchReconciliationReportResponseDto", ["totals", "differences", "inconsistencies"]),
        new("AchReconciliationTotalsDto", ["sentCount", "sentAmount", "receivedCount", "receivedAmount", "returnedCount", "returnedAmount"]),
        new("AchReconciliationDifferencesDto", ["sentVsReceivedCountDiff", "sentVsReceivedAmountDiff", "sentVsReturnedCountDiff", "sentVsReturnedAmountDiff", "receivedVsReturnedCountDiff", "receivedVsReturnedAmountDiff"]),
        new("AchReconciliationInconsistencyDto", ["code", "description", "affectedCount"]),
        new("AchAuditReportResponseDto", ["items", "page", "pageSize", "total"]),
        new("AchAuditReportRowDto", ["user", "action", "entity", "entityId", "dateUtc"]),
        new("AchHistoryReportResponseDto", ["items", "page", "pageSize", "total"]),
        new("AchHistoryReportRowDto", ["transactionId", "fromState", "toState", "source", "reasonCode", "dateUtc", "changedBy"]),
        new("AccountingReviewExportApiRequest", ["format", "dateFrom", "dateTo", "clearingHouseId", "clearingHouseCode", "cycleId", "cycleName", "fileId", "fileName", "fileHash", "transactionId", "status", "causeCode", "includeOutbound", "includeIncoming", "includeReturns", "includeReturnOfReturn", "includeOrphans", "includeManualAuditOnly", "includeNetting", "includeLiquidity", "includeCudEvidence", "includeRows", "includeDifferences", "includeEvidence", "includeBoundaryFlags", "includeWarnings", "includeSummary", "includeScope", "requestedBy", "correlationId", "csvDelimiter"])
    ];

    private static readonly Sc408Qg2Operation[] Sc408Qg2Operations =
    [
        new(
            "GET",
            "/api/integrations/methods",
            ControllerType: typeof(IntegrationCatalogController),
            ActionName: "GetMethods",
            ActionRouteTemplate: "methods",
            Policy: "CanReadAch",
            Tag: "Integraciones",
            Summary: "Consultar métodos de integración",
            Description: "Devuelve los métodos del catálogo de integración después de que el servicio asegura su bootstrap persistido. Los métodos se ordenan por Id e incluyen las claves y la clasificación funcional calculadas por el servicio a partir del código del método.",
            Parameters: [],
            RequestSchema: null,
            RequestProperties: [],
            Responses:
            [
                new("200", "Colección de métodos de integración expuestos por el catálogo, ordenada por identificador.", Qg2ResponseShape.Collection, "IntegrationMethodDto")
            ],
            ResponseSchemaDocumentation: new(
                "IntegrationMethodDto",
                ["id", "code", "displayName", "soapClientCode", "isActive", "integrationKey", "operationKey", "mappingDirection", "mappingPurpose", "functionalNature", "functionalOriginator", "movesMoney"])),
        new(
            "GET",
            "/api/integrations/methods/{methodId}/parameters",
            ControllerType: typeof(IntegrationCatalogController),
            ActionName: "GetParametersByMethod",
            ActionRouteTemplate: "methods/{methodId:int}/parameters",
            Policy: "CanReadAch",
            Tag: "Integraciones",
            Summary: "Consultar parámetros de un método de integración",
            Description: "Devuelve los parámetros activos cuyo MethodId coincide con methodId, ordenados por SortOrder y ParameterPath. Si el identificador no tiene parámetros activos, el servicio devuelve una colección vacía; la acción no traduce ese caso a 404.",
            Parameters: [new("methodId", "path", Required: true)],
            RequestSchema: null,
            RequestProperties: [],
            Responses:
            [
                new("200", "Colección, posiblemente vacía, de parámetros activos del método solicitado.", Qg2ResponseShape.Collection, "IntegrationMethodParameterDto")
            ],
            ResponseSchemaDocumentation: new(
                "IntegrationMethodParameterDto",
                ["id", "methodId", "parameterPath", "displayName", "descriptionEs", "category", "exampleValue", "uiHelpText", "dataType", "direction", "cardinality", "required", "sortOrder", "isActive"])),
        new(
            "GET",
            "/api/integrations/source-catalog",
            ControllerType: typeof(IntegrationCatalogController),
            ActionName: "GetSourceCatalog",
            ActionRouteTemplate: "source-catalog",
            Policy: "CanReadAch",
            Tag: "Integraciones",
            Summary: "Consultar catálogo de campos fuente de integración",
            Description: "Devuelve campos fuente activos. Sin methodId incluye todo el catálogo activo; con methodId incluye los campos globales cuyo MethodId es null y los asociados al método solicitado. El servicio ordena por SourceKind, SortOrder y FieldPath.",
            Parameters: [new("methodId", "query", Required: false)],
            RequestSchema: null,
            RequestProperties: [],
            Responses:
            [
                new("200", "Colección de campos fuente activos que cumplen el filtro opcional por método.", Qg2ResponseShape.Collection, "IntegrationSourceCatalogFieldDto")
            ],
            ResponseSchemaDocumentation: new(
                "IntegrationSourceCatalogFieldDto",
                ["id", "methodId", "sourceKind", "entityName", "fieldPath", "displayName", "dataType", "cardinality", "nullable", "sortOrder", "isActive"])),
        new(
            "GET",
            "/api/integrations/transformations",
            ControllerType: typeof(IntegrationCatalogController),
            ActionName: "GetTransformations",
            ActionRouteTemplate: "transformations",
            Policy: "CanReadAch",
            Tag: "Integraciones",
            Summary: "Consultar transformaciones de integración",
            Description: "Devuelve el catálogo en memoria de transformaciones que el servicio publica para mappings, junto con los indicadores que señalan si cada transformación admite máscara de formato o múltiples fuentes.",
            Parameters: [],
            RequestSchema: null,
            RequestProperties: [],
            Responses:
            [
                new("200", "Colección de transformaciones disponibles para configuración de mappings de integración.", Qg2ResponseShape.Collection, "IntegrationTransformationCatalogDto")
            ],
            ResponseSchemaDocumentation: new(
                "IntegrationTransformationCatalogDto",
                ["code", "displayName", "description", "supportsFormatMask", "supportsMultipleSources"])),
        new(
            "GET",
            "/api/ach-colombia/file-exchange/configuration",
            ControllerType: typeof(AchColombiaFileExchangeController),
            ActionName: "GetConfiguration",
            ActionRouteTemplate: "configuration",
            Policy: "CanReadAch",
            Tag: "ACH Colombia",
            Summary: "Consultar configuración de intercambio ACH Colombia",
            Description: "Obtiene la configuración operativa vigente del intercambio administrado de archivos ACH Colombia, incluidos indicadores de ejecución automática/manual, reintentos, retención, ubicaciones operativas y el token de concurrencia actual.",
            Parameters: [],
            RequestSchema: null,
            RequestProperties: [],
            Responses:
            [
                new("200", "Configuración operativa vigente del intercambio administrado ACH Colombia.", Qg2ResponseShape.Schema, "AchManagedFileTransferConfigurationDto")
            ],
            ResponseSchemaDocumentation: new(
                "AchManagedFileTransferConfigurationDto",
                ["automaticOutboundEnabled", "automaticInboundEnabled", "manualOutboundAllowed", "manualInboundAllowed", "maximumRetries", "retentionDays", "outboundLocation", "inboundLocation", "archiveLocation", "concurrencyToken"])),
        new(
            "PUT",
            "/api/ach-colombia/file-exchange/configuration",
            ControllerType: typeof(AchColombiaFileExchangeController),
            ActionName: "UpdateConfiguration",
            ActionRouteTemplate: "configuration",
            Policy: "CanManageAch",
            Tag: "ACH Colombia",
            Summary: "Actualizar configuración de intercambio ACH Colombia",
            Description: "Actualiza los indicadores operativos, límites de reintentos/retención y ubicaciones del intercambio administrado. El servicio compara ConcurrencyToken con la versión persistida y genera un token nuevo al guardar. El controller traduce únicamente el conflicto de concurrencia a 409 ProblemDetails.",
            Parameters: [],
            RequestSchema: "AchManagedFileTransferConfigurationDto",
            RequestProperties: ["automaticOutboundEnabled", "automaticInboundEnabled", "manualOutboundAllowed", "manualInboundAllowed", "maximumRetries", "retentionDays", "outboundLocation", "inboundLocation", "archiveLocation", "concurrencyToken"],
            Responses:
            [
                new("200", "Configuración operativa persistida con un nuevo ConcurrencyToken.", Qg2ResponseShape.Schema, "AchManagedFileTransferConfigurationDto"),
                new("409", "ConcurrencyToken no coincide con la configuración persistida; se devuelve ProblemDetails indicando recargar y reintentar.", Qg2ResponseShape.ProblemDetails, "ProblemDetails")
            ]),
        new(
            "GET",
            "/api/ach-colombia/file-exchange/administration",
            ControllerType: typeof(AchColombiaFileExchangeController),
            ActionName: "GetAdministration",
            ActionRouteTemplate: "administration",
            Policy: "CanReadAch",
            Tag: "ACH Colombia",
            Summary: "Consultar administración de intercambio ACH Colombia",
            Description: "Obtiene el perfil administrativo vigente: proveedor, protocolo, endpoint, principal, controles operativos, ubicaciones, estado de credencial y token de concurrencia. El contrato no devuelve el valor secreto de la credencial.",
            Parameters: [],
            RequestSchema: null,
            RequestProperties: [],
            Responses:
            [
                new("200", "Perfil administrativo vigente; expone metadata de credencial pero nunca su valor secreto.", Qg2ResponseShape.Schema, "AchManagedMftAdministrationDto")
            ],
            ResponseSchemaDocumentation: new(
                "AchManagedMftAdministrationDto",
                ["profileName", "provider", "protocol", "profileEnabled", "endpoint", "port", "principal", "automaticOutboundEnabled", "automaticInboundEnabled", "manualOutboundAllowed", "manualInboundAllowed", "maximumRetries", "retryDelaySeconds", "retentionDays", "outboundLocation", "inboundLocation", "archiveLocation", "credentialConfigured", "credentialType", "credentialUpdatedAtUtc", "concurrencyToken", "sftpHostKeyFingerprint", "sftpTimeoutSeconds"])),
        new(
            "PUT",
            "/api/ach-colombia/file-exchange/administration",
            ControllerType: typeof(AchColombiaFileExchangeController),
            ActionName: "UpdateAdministration",
            ActionRouteTemplate: "administration",
            Policy: "CanManageAch",
            Tag: "ACH Colombia",
            Summary: "Actualizar administración de intercambio ACH Colombia",
            Description: "Actualiza perfil, proveedor, protocolo, endpoint/principal, controles operativos, reintentos, retención y ubicaciones. El servicio valida rangos operativos y campos requeridos; ArgumentException se traduce a 400 ProblemDetails. Si ConcurrencyToken no coincide con la versión persistida, devuelve 409 ProblemDetails. En éxito genera un token de concurrencia nuevo.",
            Parameters: [],
            RequestSchema: "UpdateAchManagedMftAdministrationRequest",
            RequestProperties: ["profileName", "provider", "protocol", "profileEnabled", "endpoint", "port", "principal", "automaticOutboundEnabled", "automaticInboundEnabled", "manualOutboundAllowed", "manualInboundAllowed", "maximumRetries", "retryDelaySeconds", "retentionDays", "outboundLocation", "inboundLocation", "archiveLocation", "concurrencyToken", "sftpHostKeyFingerprint", "sftpTimeoutSeconds"],
            Responses:
            [
                new("200", "Perfil administrativo persistido con metadata de credencial y un nuevo ConcurrencyToken.", Qg2ResponseShape.Schema, "AchManagedMftAdministrationDto"),
                new("400", "El servicio rechazó la configuración por campos requeridos o rangos operativos inválidos; se devuelve ProblemDetails.", Qg2ResponseShape.ProblemDetails, "ProblemDetails"),
                new("409", "ConcurrencyToken no coincide con la configuración persistida; se devuelve ProblemDetails indicando recargar y reintentar.", Qg2ResponseShape.ProblemDetails, "ProblemDetails")
            ],
            ResponseSchemaDocumentation: new(
                "UpdateAchManagedMftAdministrationRequest",
                ["profileName", "provider", "protocol", "profileEnabled", "endpoint", "port", "principal", "automaticOutboundEnabled", "automaticInboundEnabled", "manualOutboundAllowed", "manualInboundAllowed", "maximumRetries", "retryDelaySeconds", "retentionDays", "outboundLocation", "inboundLocation", "archiveLocation", "concurrencyToken", "sftpHostKeyFingerprint", "sftpTimeoutSeconds"])),
        new(
            "PUT",
            "/api/ach-colombia/file-exchange/administration/credential",
            ControllerType: typeof(AchColombiaFileExchangeController),
            ActionName: "SetCredential",
            ActionRouteTemplate: "administration/credential",
            Policy: "CanManageAch",
            Tag: "ACH Colombia",
            Summary: "Actualizar credencial de intercambio ACH Colombia",
            Description: "Establece la credencial del perfil administrativo. CredentialType y Secret deben contener un valor no vacío y ConcurrencyToken debe corresponder a la versión vigente; ArgumentException se traduce a 400 ProblemDetails y un token obsoleto a 409 ProblemDetails. El servicio protege el secreto antes de persistirlo y la respuesta devuelve únicamente metadata de credencial, nunca el secreto.",
            Parameters: [],
            RequestSchema: "SetAchManagedMftCredentialRequest",
            RequestProperties: ["credentialType", "secret", "concurrencyToken"],
            Responses:
            [
                new("200", "Perfil administrativo actualizado; confirma metadata de credencial sin devolver el secreto.", Qg2ResponseShape.Schema, "AchManagedMftAdministrationDto"),
                new("400", "CredentialType o Secret no contienen un valor válido; se devuelve ProblemDetails sin eco del secreto.", Qg2ResponseShape.ProblemDetails, "ProblemDetails"),
                new("409", "ConcurrencyToken no coincide con la configuración persistida; se devuelve ProblemDetails indicando recargar y reintentar.", Qg2ResponseShape.ProblemDetails, "ProblemDetails")
            ],
            ResponseSchemaDocumentation: new(
                "SetAchManagedMftCredentialRequest",
                ["credentialType", "secret", "concurrencyToken"]))
    ];

    private static readonly Qg2SchemaDocumentation[] Sc408AdditionalSchemaDocumentation =
    [
        new("AchManagedMftAdministrationDto", ["profileName", "provider", "protocol", "profileEnabled", "endpoint", "port", "principal", "automaticOutboundEnabled", "automaticInboundEnabled", "manualOutboundAllowed", "manualInboundAllowed", "maximumRetries", "retryDelaySeconds", "retentionDays", "outboundLocation", "inboundLocation", "archiveLocation", "credentialConfigured", "credentialType", "credentialUpdatedAtUtc", "concurrencyToken", "sftpHostKeyFingerprint", "sftpTimeoutSeconds"])
    ];

    private static readonly Sc409Qg2Operation[] Sc409Qg2Operations =
    [
        new(
            "GET",
            "/nacha-config/perfiles",
            ControllerType: typeof(NachaConfigProfilesController),
            ActionName: "GetProfiles",
            ActionRouteTemplate: "perfiles",
            Policy: "P1.ConfigRead",
            Tag: "NACHA-M",
            Summary: "Listar perfiles de configuración NACHA",
            Description: "Devuelve todos los perfiles de configuración NACHA persistidos, ordenados por UpdatedAt descendente. Cada elemento expone identificación, códigos de estado/cámara/flujo/dirección/servicio, versión, vigencia y RowVersion codificado en Base64; la acción no aplica filtros adicionales.",
            Parameters: [],
            Responses:
            [
                new("200", "Colección de perfiles de configuración NACHA ordenada por la última actualización persistida.", Qg2ResponseShape.Collection, "NachaConfigProfileListItemDto")
            ],
            ResponseSchemaDocumentation: new(
                "NachaConfigProfileListItemDto",
                ["id", "profileCode", "nombreEs", "estado", "camara", "flujo", "direccion", "servicio", "versionMajor", "versionMinor", "effectiveFrom", "effectiveTo", "rowVersion"])),
        new(
            "GET",
            "/nacha-config/catalogos-filtro",
            ControllerType: typeof(NachaConfigProfilesController),
            ActionName: "GetFilterCatalogs",
            ActionRouteTemplate: "catalogos-filtro",
            Policy: "P1.ConfigRead",
            Tag: "NACHA-M",
            Summary: "Consultar catálogos de filtro NACHA",
            Description: "Devuelve estados de configuración y catálogos de cámaras, flujos, direcciones, clases de servicio, registros, tipos de origen y tipos de regla. Las opciones se obtienen desde la configuración persistida; las cámaras, flujos, direcciones y servicios solo incluyen registros activos.",
            Parameters: [],
            Responses:
            [
                new("200", "Catálogos persistidos disponibles para filtros y compatibilidad de la configuración NACHA.", Qg2ResponseShape.Schema, "NachaConfigFilterCatalogsDto")
            ],
            ResponseSchemaDocumentation: new(
                "NachaConfigFilterCatalogsDto",
                ["estados", "camaras", "flujos", "direcciones", "servicios", "registros", "tiposOrigen", "tiposRegla"])),
        new(
            "GET",
            "/nacha-config/catalogos-operativos",
            ControllerType: typeof(NachaConfigProfilesController),
            ActionName: "GetOperationalCatalogs",
            ActionRouteTemplate: "catalogos-operativos",
            Policy: "P1.ConfigRead",
            Tag: "NACHA-M",
            Summary: "Consultar catálogos operativos NACHA",
            Description: "Devuelve catálogos humanizados desde las tablas de configuración. Cuando se informa camaraCode, las clases de servicio se limitan a las específicas de esa cámara y a las globales; cuando se informa flujoCode con dirección predeterminada, solo se devuelve esa dirección activa.",
            Parameters: [new("camaraCode", "query", Required: false), new("flujoCode", "query", Required: false)],
            Responses:
            [
                new("200", "Catálogos operativos NACHA humanizados y filtrados por el contexto informado.", Qg2ResponseShape.Schema, "NachaConfigFilterCatalogsDto")
            ],
            ResponseSchemaDocumentation: new(
                "NachaConfigFilterCatalogsDto",
                ["estados", "camaras", "flujos", "direcciones", "servicios", "registros", "tiposOrigen", "tiposRegla"])),
        new(
            "GET",
            "/nacha-config/perfiles/{id}",
            ControllerType: typeof(NachaConfigProfilesController),
            ActionName: "GetProfile",
            ActionRouteTemplate: "perfiles/{id:int}",
            Policy: "P1.ConfigRead",
            Tag: "NACHA-M",
            Summary: "Consultar perfil de configuración NACHA",
            Description: "Obtiene el perfil identificado por id con sus registros, variantes de layout, campos y reglas. Los registros se ordenan por secuencia; las variantes por código de registro y prioridad; los campos por posición inicial y las reglas por identificador. Devuelve 404 sin body cuando el perfil no existe.",
            Parameters: [new("id", "path", Required: true)],
            Responses:
            [
                new("200", "Detalle del perfil de configuración NACHA solicitado, incluyendo registros, variantes, campos y reglas persistidos.", Qg2ResponseShape.Schema, "NachaConfigProfileDetailDto"),
                new("404", "No existe un perfil de configuración NACHA con el identificador indicado.", Qg2ResponseShape.NoBody)
            ],
            ResponseSchemaDocumentation: new(
                "NachaConfigProfileDetailDto",
                ["id", "profileCode", "nombreEs", "descripcion", "estado", "estadoNombre", "isEditable", "camara", "camaraNombre", "flujo", "flujoNombre", "direccion", "direccionNombre", "servicio", "servicioNombre", "versionMajor", "versionMinor", "contextPriority", "effectiveFrom", "effectiveTo", "rowVersion", "records", "variantes"])),
        new(
            "GET",
            "/nacha-config/perfiles/{id}/workspace",
            ControllerType: typeof(NachaConfigProfilesController),
            ActionName: "GetWorkspace",
            ActionRouteTemplate: "perfiles/{id:int}/workspace",
            Policy: "P1.ConfigRead",
            Tag: "NACHA-M",
            Summary: "Consultar workspace de configuración NACHA",
            Description: "Devuelve el perfil con nombres funcionales persistidos para estado, cámara, flujo, dirección, servicio, registros, variantes, campos, tipos de origen y tipos de regla. Conserva los códigos técnicos para trazabilidad y expone si el estado permite edición.",
            Parameters: [new("id", "path", Required: true)],
            Responses:
            [
                new("200", "Workspace humanizado del perfil solicitado.", Qg2ResponseShape.Schema, "NachaConfigWorkspaceDto"),
                new("404", "No existe un perfil de configuración NACHA con el identificador indicado.", Qg2ResponseShape.NoBody)
            ],
            ResponseSchemaDocumentation: new(
                "NachaConfigWorkspaceDto",
                ["id", "profileCode", "nombreEs", "descripcion", "estado", "estadoNombre", "isEditable", "camara", "camaraNombre", "flujo", "flujoNombre", "direccion", "direccionNombre", "servicio", "servicioNombre", "versionMajor", "versionMinor", "contextPriority", "effectiveFrom", "effectiveTo", "rowVersion", "records", "variantes"])),
        new(
            "GET",
            "/nacha-config/perfiles/{id}/fields/{fieldId}/editor",
            ControllerType: typeof(NachaConfigProfilesController),
            ActionName: "GetFieldEditor",
            ActionRouteTemplate: "perfiles/{id:int}/fields/{fieldId:int}/editor",
            Policy: "P1.ConfigRead",
            Tag: "NACHA-M",
            Summary: "Consultar editor operativo de campo NACHA",
            Description: "Devuelve el campo solicitado, sus reglas existentes y opciones de origen/formato/alineación disponibles para la misma cámara. Las rutas internas, expresiones y configuraciones técnicas se conservan solo como información avanzada de lectura.",
            Parameters: [new("id", "path", Required: true), new("fieldId", "path", Required: true)],
            Responses:
            [
                new("200", "Editor operativo contextual del campo solicitado.", Qg2ResponseShape.Schema, "NachaConfigFieldEditorDto"),
                new("404", "No existe el perfil o el campo no pertenece al perfil indicado.", Qg2ResponseShape.NoBody)
            ],
            ResponseSchemaDocumentation: new(
                "NachaConfigFieldEditorDto",
                ["profileId", "fieldId", "isEditable", "rowVersion", "field", "sourceTypes", "sourceOptions", "alignments", "formats", "severities", "sourceReferenceCount"])),
        new(
            "GET",
            "/nacha-config/perfiles/{id}/cambios",
            ControllerType: typeof(NachaConfigProfilesController),
            ActionName: "Changes",
            ActionRouteTemplate: "perfiles/{id:int}/cambios",
            Policy: "P1.ConfigRead",
            Tag: "NACHA-M",
            Summary: "Revisar cambios pendientes de configuración NACHA",
            Description: "Compara el perfil con la configuración que reemplaza usando los datos persistidos y devuelve únicamente diferencias reales humanizadas. No modifica ni publica la configuración.",
            Parameters: [new("id", "path", Required: true)],
            Responses:
            [
                new("200", "Comparación humanizada del borrador frente a su configuración base.", Qg2ResponseShape.Schema, "NachaConfigChangeReviewDto"),
                new("404", "No existe el perfil de configuración NACHA indicado.", Qg2ResponseShape.NoBody)
            ],
            ResponseSchemaDocumentation: new(
                "NachaConfigChangeReviewDto",
                ["profileId", "baseProfileId", "hasComparisonBase", "isEditable", "rowVersion", "currentVersion", "nextVersion", "camaraNombreEs", "flujoNombreEs", "direccionNombreEs", "servicioNombreEs", "effectiveFrom", "effectiveTo", "changedFieldsCount", "changeCount", "summaryEs", "changes"])),
        new(
            "GET",
            "/nacha-config/perfiles/{id}/historial",
            ControllerType: typeof(NachaConfigProfilesController),
            ActionName: "History",
            ActionRouteTemplate: "perfiles/{id:int}/historial",
            Policy: "P1.ConfigRead",
            Tag: "NACHA-M",
            Summary: "Consultar historial de configuración NACHA",
            Description: "Devuelve los cambios históricos ordenados del más reciente al más antiguo, con acción, elemento, versión, resumen y detalle humanizados desde la auditoría persistida. La ausencia de cambios se representa como una colección vacía.",
            Parameters: [new("id", "path", Required: true)],
            Responses:
            [
                new("200", "Colección humanizada, posiblemente vacía, de eventos históricos del perfil ordenados del más reciente al más antiguo.", Qg2ResponseShape.Collection, "NachaConfigHistoryItemDto")
            ],
            ResponseSchemaDocumentation: new(
                "NachaConfigHistoryItemDto",
                ["id", "changedAtUtc", "changedBy", "changeType", "entityName", "correlationId", "actionNameEs", "elementNameEs", "version", "summaryEs", "detailEs"])),
        new(
            "GET",
            "/nacha-config/perfiles/{id}/snapshots",
            ControllerType: typeof(NachaConfigProfilesController),
            ActionName: "Snapshots",
            ActionRouteTemplate: "perfiles/{id:int}/snapshots",
            Policy: "P1.ConfigRead",
            Tag: "NACHA-M",
            Summary: "Consultar snapshots de configuración NACHA",
            Description: "Devuelve los snapshots históricos cuyo ProfileId coincide con id, ordenados por CreatedAtUtc descendente. La ausencia de snapshots se representa como una colección vacía; la acción no traduce ese caso a 404.",
            Parameters: [new("id", "path", Required: true)],
            Responses:
            [
                new("200", "Colección, posiblemente vacía, de snapshots históricos del perfil ordenados del más reciente al más antiguo.", Qg2ResponseShape.Collection, "NachaConfigSnapshotItemDto")
            ],
            ResponseSchemaDocumentation: new(
                "NachaConfigSnapshotItemDto",
                ["id", "createdAtUtc", "createdBy", "snapshotType", "versionMajor", "versionMinor"])),
        new(
            "GET",
            "/api/ach/nacha/config-profiles/dashboard",
            ControllerType: typeof(NachaConfigProfilesReadOnlyController),
            ActionName: "GetDashboard",
            ActionRouteTemplate: "dashboard",
            Policy: "P1.NachaRead",
            Tag: "NACHA-M",
            Summary: "Consultar dashboard de configuración NACHA",
            Description: "Resume los perfiles oficiales persistidos: total de perfiles, publicados y vigentes según DateTime.UtcNow, totales de variantes/campos y catálogos distintos de cámaras y tipos de registro. Si no existen perfiles, Warnings contiene el aviso definido por el servicio.",
            Parameters: [],
            Responses:
            [
                new("200", "Resumen de los perfiles de configuración NACHA persistidos y sus conteos derivados.", Qg2ResponseShape.Schema, "NachaConfigProfilesDashboardReadModel")
            ],
            ResponseSchemaDocumentation: new(
                "NachaConfigProfilesDashboardReadModel",
                ["productiveStatus", "isOfficialModel", "legacyDeprecated", "profileCount", "publishedProfileCount", "currentProfileCount", "layoutVariantCount", "fieldCount", "clearingHouses", "recordTypes", "warnings"])),
        new(
            "GET",
            "/api/ach/nacha/config-profiles",
            ControllerType: typeof(NachaConfigProfilesReadOnlyController),
            ActionName: "GetProfiles",
            ActionRouteTemplate: null,
            Policy: "P1.NachaRead",
            Tag: "NACHA-M",
            Summary: "Listar perfiles NACHA de solo lectura",
            Description: "Devuelve los perfiles ordenados por código de cámara y ProfileCode. IsPublished refleja Status=PUBLICADO y IsCurrent se calcula comparando EffectiveFrom/EffectiveTo con DateTime.UtcNow; la colección puede estar vacía.",
            Parameters: [],
            Responses:
            [
                new("200", "Colección, posiblemente vacía, de perfiles NACHA proyectados por el read model oficial.", Qg2ResponseShape.Collection, "NachaConfigProfileReadModel")
            ],
            ResponseSchemaDocumentation: new(
                "NachaConfigProfileReadModel",
                ["profileId", "profileCode", "profileName", "clearingHouseCode", "flowType", "status", "version", "isPublished", "isCurrent", "effectiveFrom", "effectiveTo", "layoutVariantCount", "fieldCount", "recordTypes", "isOfficialModel", "legacyDeprecated"])),
        new(
            "GET",
            "/api/ach/nacha/config-profiles/{id}",
            ControllerType: typeof(NachaConfigProfilesReadOnlyController),
            ActionName: "GetProfile",
            ActionRouteTemplate: "{id:int}",
            Policy: "P1.NachaRead",
            Tag: "NACHA-M",
            Summary: "Consultar perfil NACHA de solo lectura por id",
            Description: "Obtiene el perfil identificado por id junto con sus variantes y campos proyectados por el read model. Devuelve 404 sin body cuando el perfil no existe.",
            Parameters: [new("id", "path", Required: true)],
            Responses:
            [
                new("200", "Detalle de solo lectura del perfil NACHA solicitado, incluidas variantes y campos.", Qg2ResponseShape.Schema, "NachaConfigProfileDetailReadModel"),
                new("404", "No existe un perfil NACHA con el identificador indicado.", Qg2ResponseShape.NoBody)
            ],
            ResponseSchemaDocumentation: new(
                "NachaConfigProfileDetailReadModel",
                ["profileId", "profileCode", "profileName", "clearingHouseCode", "flowType", "status", "version", "isPublished", "isCurrent", "effectiveFrom", "effectiveTo", "layoutVariantCount", "fieldCount", "recordTypes", "isOfficialModel", "legacyDeprecated", "variants", "fields"])),
        new(
            "GET",
            "/api/ach/nacha/config-profiles/by-code/{profileCode}",
            ControllerType: typeof(NachaConfigProfilesReadOnlyController),
            ActionName: "GetProfileByCode",
            ActionRouteTemplate: "by-code/{profileCode}",
            Policy: "P1.NachaRead",
            Tag: "NACHA-M",
            Summary: "Consultar perfil NACHA de solo lectura por código",
            Description: "Recorta profileCode y busca una coincidencia exacta con ProfileCode. Devuelve el mismo detalle de solo lectura que la consulta por id o 404 sin body cuando no existe una coincidencia.",
            Parameters: [new("profileCode", "path", Required: true)],
            Responses:
            [
                new("200", "Detalle de solo lectura del perfil NACHA cuyo código coincide con el valor normalizado solicitado.", Qg2ResponseShape.Schema, "NachaConfigProfileDetailReadModel"),
                new("404", "No existe un perfil NACHA con el código indicado después de eliminar espacios exteriores.", Qg2ResponseShape.NoBody)
            ]),
        new(
            "GET",
            "/api/ach/nacha/config-profiles/{id}/variants",
            ControllerType: typeof(NachaConfigProfilesReadOnlyController),
            ActionName: "GetVariants",
            ActionRouteTemplate: "{id:int}/variants",
            Policy: "P1.NachaRead",
            Tag: "NACHA-M",
            Summary: "Listar variantes de layout de un perfil NACHA",
            Description: "Devuelve las variantes cuyo ProfileId coincide con id, ordenadas por tipo de registro y prioridad. IsActive solo es true cuando la variante es la predeterminada del registro y su estado es PUBLICADO; BlockingFactor se obtiene del campo BLOCKINGFACTOR cuando su valor constante es entero y, en otro caso, el servicio usa 10. La ausencia de variantes se representa como colección vacía.",
            Parameters: [new("id", "path", Required: true)],
            Responses:
            [
                new("200", "Colección, posiblemente vacía, de variantes de layout del perfil solicitado.", Qg2ResponseShape.Collection, "NachaConfigProfileVariantReadModel")
            ],
            ResponseSchemaDocumentation: new(
                "NachaConfigProfileVariantReadModel",
                ["variantId", "variantCode", "recordType", "recordLength", "blockingFactor", "isActive", "fieldCount"])),
        new(
            "GET",
            "/api/ach/nacha/config-profiles/{id}/fields",
            ControllerType: typeof(NachaConfigProfilesReadOnlyController),
            ActionName: "GetFields",
            ActionRouteTemplate: "{id:int}/fields",
            Policy: "P1.NachaRead",
            Tag: "NACHA-M",
            Summary: "Listar campos de layout de un perfil NACHA",
            Description: "Devuelve los campos pertenecientes a variantes del perfil indicado, ordenados por tipo de registro y posición inicial. El read model deriva EndPosition, dirección/carácter de padding, marca de campo computado y marca de total de control a partir de la configuración persistida. La ausencia de campos se representa como colección vacía.",
            Parameters: [new("id", "path", Required: true)],
            Responses:
            [
                new("200", "Colección, posiblemente vacía, de campos de layout proyectados para el perfil solicitado.", Qg2ResponseShape.Collection, "NachaConfigProfileFieldReadModel")
            ],
            ResponseSchemaDocumentation: new(
                "NachaConfigProfileFieldReadModel",
                ["fieldId", "recordType", "fieldName", "startPosition", "length", "endPosition", "dataType", "isRequired", "defaultValue", "sourceFieldPath", "paddingDirection", "paddingChar", "format", "isComputed", "isControlTotalField"]))
    ];

    private static readonly Qg2SchemaDocumentation[] Sc409AdditionalSchemaDocumentation =
    [
        new("NachaConfigFilterCatalogOptionDto", ["code", "labelEs"]),
        new("NachaConfigProfileRecordDto", ["id", "recordCode", "recordNameEs", "sequence", "isEnabled", "minOccurs", "maxOccurs", "sourceStrategy", "sourceStrategyName"]),
        new("NachaConfigLayoutVariantDto", ["id", "recordCode", "variantCode", "nombreEs", "descripcion", "priority", "isDefaultForRecord", "totalLength", "effectiveFrom", "effectiveTo", "fields"]),
        new("NachaConfigLayoutFieldDto", ["id", "fieldCode", "fieldNameEs", "startPosition", "length", "padChar", "justification", "formatMask", "sortOrder", "isVisibleInBackoffice", "transformationPipelineJson", "sourceDefinitionId", "propertyPath", "sourceType", "sourceTypeName", "constantValue", "entityName", "sqlObjectName", "expressionDsl", "externalCatalogCode", "fallbackPolicyJson", "isEnabled", "reglas"]),
        new("NachaConfigFieldRuleDto", ["id", "ruleType", "ruleTypeName", "errorCode", "errorMessageEs", "severity", "severityName", "ruleCode", "conditionDsl", "ruleConfigJson", "order", "isEnabled"]),
        new("NachaConfigEditorOptionDto", ["code", "labelEs"]),
        new("NachaConfigEditorSourceTypeOptionDto", ["code", "labelEs", "catalogLabelEs", "usesFixedValue", "requiresDefinitionSelection"]),
        new("NachaConfigEditorSourceOptionDto", ["sourceDefinitionId", "sourceTypeCode", "labelEs", "descriptionEs"])
    ];

    private static readonly Sc410ControllerQg2Operation[] Sc410ControllerQg2Operations =
    [
        new(
            "GET",
            "/api/audit-logs",
            ControllerType: typeof(AuditLogsController),
            ActionName: "GetAuditLogsAsync",
            ActionRouteTemplate: null,
            NominalPolicy: null,
            Summary: "Consultar registros de auditoría",
            Description: "Consulta los registros de auditoría aplicando los filtros opcionales startDate, endDate, changedBy y action. El servicio normaliza page a 1 y pageSize a 50 cuando reciben valores menores o iguales a cero, ordena por ChangedAt descendente y devuelve un BaseResponseModel cuyo data contiene PagedResponse<AuditLogDto>.",
            Parameters:
            [
                new("startDate", "query", Required: false),
                new("endDate", "query", Required: false),
                new("changedBy", "query", Required: false),
                new("action", "query", Required: false),
                new("page", "query", Required: false),
                new("pageSize", "query", Required: false)
            ],
            RequestSchema: null,
            RequestProperties: [],
            Responses:
            [
                new("200", "Página de registros de auditoría que cumplen los filtros solicitados, envuelta en BaseResponseModel.", Qg2ResponseShape.Schema, "BaseResponseModel")
            ]),
        new(
            "GET",
            "/api/navigation-logs",
            ControllerType: typeof(NavigationLogsController),
            ActionName: "GetNavigationLogsAsync",
            ActionRouteTemplate: null,
            NominalPolicy: "CanReadAch",
            Summary: "Consultar registros de navegación",
            Description: "Consulta los registros de navegación aplicando los filtros opcionales startDate, endDate, userId y route. userId también busca por Username o FullName del usuario relacionado. El servicio normaliza page a 1 y pageSize a 50 cuando reciben valores menores o iguales a cero, ordena por VisitedAt descendente y devuelve un BaseResponseModel cuyo data contiene PagedResponse<NavigationLogDto>.",
            Parameters:
            [
                new("startDate", "query", Required: false),
                new("endDate", "query", Required: false),
                new("userId", "query", Required: false),
                new("route", "query", Required: false),
                new("page", "query", Required: false),
                new("pageSize", "query", Required: false)
            ],
            RequestSchema: null,
            RequestProperties: [],
            Responses:
            [
                new("200", "Página de registros de navegación que cumplen los filtros solicitados, envuelta en BaseResponseModel.", Qg2ResponseShape.Schema, "BaseResponseModel")
            ]),
        new(
            "POST",
            "/api/navigation-logs",
            ControllerType: typeof(NavigationLogsController),
            ActionName: "AddNavigationLogAsync",
            ActionRouteTemplate: null,
            NominalPolicy: null,
            Summary: "Registrar navegación",
            Description: "Registra una navegación asociada al usuario autenticado. El servicio elimina query string y fragmento de Route, asegura un slash inicial y limita la ruta a 300 caracteres; SessionId se recorta a 100 caracteres; DurationMs solo se conserva entre 1 y 86400000; VisitedAt se convierte a UTC o usa la hora UTC actual. El controller obtiene el usuario de los claims y captura IP y User-Agent de la solicitud. Una ruta vacía produce 400; el éxito produce 200.",
            Parameters: [],
            RequestSchema: "NavigationLogCreate",
            RequestProperties: ["route", "visitedAt", "sessionId", "durationMs"],
            Responses:
            [
                new("200", "Registro de navegación persistido; la respuesta usa BaseResponseModel sin data de negocio adicional.", Qg2ResponseShape.Schema, "BaseResponseModel"),
                new("400", "La ruta de navegación está vacía después de normalizarla; la respuesta usa BaseResponseModel con el error NAV_ROUTE_REQUIRED.", Qg2ResponseShape.Schema, "BaseResponseModel")
            ])
    ];

    private static readonly Sc410HealthQg2Operation[] Sc410HealthQg2Operations =
    [
        new(
            "GET",
            "/health/live",
            Summary: "Consultar liveness del servicio",
            Description: "Expone la señal pública de liveness del proceso. Responde 200 con un body JSON anónimo que contiene status=Healthy, check=live, service=ACHInterbank y timestampUtc; no ejecuta comprobaciones de dependencias.",
            StatusCodes: ["200"]),
        new(
            "GET",
            "/health/ready",
            Summary: "Consultar readiness del servicio",
            Description: "Expone la señal pública de readiness. Conserva las comprobaciones existentes de conectividad de base de datos, presencia de claves de Data Protection y estado del scheduler cuando esos servicios están registrados, con timeout enlazado de 5 segundos. Responde 200 o 503 con el mismo body JSON anónimo que contiene status, check=ready, database, dataProtectionKeyRing, scheduler, persistentStore, clusterInstance y timestampUtc.",
            StatusCodes: ["200", "503"])
    ];

    private static readonly TransactionPoliciesQg2Operation[] TransactionPoliciesQg2Operations =
    [
        new(
            "GET",
            "/api/clearing-houses/{clearingHouseId}/transaction-policies",
            PrimaryStatusCode: "200",
            Parameters:
            [
                new("clearingHouseId", "path"),
                new("transactionType", "query", Required: false)
            ],
            RequestSchema: null,
            RequestProperties: [],
            Responses:
            [
                new("200", "Versiones de política transaccional registradas para la cámara y el filtro solicitado.", Qg2ResponseShape.Collection, "ClearingHouseTransactionRuleDto"),
                new("422", "La cámara no existe o el tipo de transacción informado no admite una política por cámara.", Qg2ResponseShape.Untyped)
            ],
            ResponseSchemaDocumentation: new(
                "ClearingHouseTransactionRuleDto",
                [
                    "id", "clearingHouseId", "clearingHouseName", "transactionNature", "transactionType",
                    "requiresPrenotification", "prenotificationMode", "prenotificationLeadBusinessDays",
                    "requiresReceiverIdentificationValidation", "receiverIdentificationValidationMode",
                    "appliesToNachaExport", "appliesToMonetaryTransactions", "effectiveFrom", "effectiveTo",
                    "isActive", "normativeSource", "normativeReference", "notes", "createdAt", "updatedAt"
                ])),
        new(
            "GET",
            "/api/clearing-houses/{clearingHouseId}/transaction-policies/current",
            PrimaryStatusCode: "200",
            Parameters:
            [
                new("clearingHouseId", "path"),
                new("transactionType", "query"),
                new("effectiveAt", "query", Required: false)
            ],
            RequestSchema: null,
            RequestProperties: [],
            Responses:
            [
                new("200", "Política transaccional activa que corresponde a la cámara, tipo y fecha solicitados.", Qg2ResponseShape.Schema, "ClearingHouseTransactionRuleDto"),
                new("404", "No existe una política activa vigente para la cámara, tipo y fecha solicitados.", Qg2ResponseShape.NoBody),
                new("422", "La cámara o el tipo no son válidos, o existe más de una política activa para la misma fecha.", Qg2ResponseShape.Untyped)
            ]),
        new(
            "GET",
            "/api/clearing-houses/{clearingHouseId}/transaction-policies/{id}",
            PrimaryStatusCode: "200",
            Parameters: [new("clearingHouseId", "path"), new("id", "path")],
            RequestSchema: null,
            RequestProperties: [],
            Responses:
            [
                new("200", "Versión de política transaccional encontrada para la cámara indicada.", Qg2ResponseShape.Schema, "ClearingHouseTransactionRuleDto"),
                new("404", "La versión no existe o no pertenece a la cámara indicada.", Qg2ResponseShape.NoBody),
                new("422", "La cámara indicada no existe.", Qg2ResponseShape.Untyped)
            ]),
        new(
            "POST",
            "/api/clearing-houses/{clearingHouseId}/transaction-policies",
            PrimaryStatusCode: "201",
            Parameters: [new("clearingHouseId", "path")],
            RequestSchema: "CreateClearingHouseTransactionPolicyVersionRequest",
            RequestProperties:
            [
                "transactionType", "prenotificationMode", "prenotificationLeadBusinessDays", "effectiveFrom",
                "effectiveTo", "isActive", "normativeSource", "normativeReference", "notes"
            ],
            Responses:
            [
                new("201", "Nueva versión de política transaccional creada para la cámara.", Qg2ResponseShape.Schema, "ClearingHouseTransactionRuleDto"),
                new("422", "La cámara, el tipo, la vigencia o la metadata de la versión no cumplen las validaciones del servicio.", Qg2ResponseShape.Untyped)
            ]),
        new(
            "PATCH",
            "/api/clearing-houses/{clearingHouseId}/transaction-policies/{id}/metadata",
            PrimaryStatusCode: "200",
            Parameters: [new("clearingHouseId", "path"), new("id", "path")],
            RequestSchema: "UpdateClearingHouseTransactionPolicyMetadataRequest",
            RequestProperties: ["normativeSource", "normativeReference", "notes"],
            Responses:
            [
                new("200", "Metadata de la versión actualizada sin cambiar su decisión funcional.", Qg2ResponseShape.Schema, "ClearingHouseTransactionRuleDto"),
                new("422", "La metadata es inválida o la versión no existe para la cámara indicada.", Qg2ResponseShape.Untyped)
            ]),
        new(
            "POST",
            "/api/clearing-houses/{clearingHouseId}/transaction-policies/{id}/close",
            PrimaryStatusCode: "200",
            Parameters: [new("clearingHouseId", "path"), new("id", "path")],
            RequestSchema: "CloseClearingHouseTransactionPolicyVersionRequest",
            RequestProperties: ["effectiveTo"],
            Responses:
            [
                new("200", "Versión cerrada con la fecha final de vigencia solicitada.", Qg2ResponseShape.Schema, "ClearingHouseTransactionRuleDto"),
                new("422", "La versión no existe para la cámara o la fecha de cierre entra en conflicto con su vigencia.", Qg2ResponseShape.Untyped)
            ]),
        new(
            "POST",
            "/api/clearing-houses/{clearingHouseId}/transaction-policies/{id}/activate",
            PrimaryStatusCode: "200",
            Parameters: [new("clearingHouseId", "path"), new("id", "path")],
            RequestSchema: null,
            RequestProperties: [],
            Responses:
            [
                new("200", "Versión de política transaccional activada.", Qg2ResponseShape.Schema, "ClearingHouseTransactionRuleDto"),
                new("422", "La versión no existe para la cámara o su activación produciría un solapamiento activo.", Qg2ResponseShape.Untyped)
            ]),
        new(
            "POST",
            "/api/clearing-houses/{clearingHouseId}/transaction-policies/preview",
            PrimaryStatusCode: "200",
            Parameters: [new("clearingHouseId", "path")],
            RequestSchema: "TransactionPrerequisitePreviewRequest",
            RequestProperties: ["clearingHouseId", "transactionType", "effectiveEntryDate", "appliesToNachaExport"],
            Responses:
            [
                new("200", "Resultado de la evaluación previa de prerequisitos para la cámara indicada.", Qg2ResponseShape.Schema, "TransactionPrerequisitePreviewResponse"),
                new("422", "Existe más de una política vigente aplicable a la misma cámara, tipo y fecha.", Qg2ResponseShape.Untyped)
            ],
            ResponseSchemaDocumentation: new(
                "TransactionPrerequisitePreviewResponse",
                [
                    "ruleConfigured", "requiresPrenotification", "prenotificationMode",
                    "requiresReceiverIdentificationValidation", "receiverIdentificationValidationMode",
                    "normativeSource", "normativeReference", "decision", "message"
                ])),
        new(
            "GET",
            "/api/clearing-house-transaction-rules",
            PrimaryStatusCode: "200",
            Parameters:
            [
                new("clearingHouseId", "query", Required: false),
                new("transactionNature", "query", Required: false),
                new("includeInactive", "query", Required: false)
            ],
            RequestSchema: null,
            RequestProperties: [],
            Responses:
            [
                new("200", "Reglas transaccionales que cumplen los filtros solicitados.", Qg2ResponseShape.Collection, "ClearingHouseTransactionRuleDto")
            ]),
        new(
            "GET",
            "/api/clearing-house-transaction-rules/{id}",
            PrimaryStatusCode: "200",
            Parameters: [new("id", "path")],
            RequestSchema: null,
            RequestProperties: [],
            Responses:
            [
                new("200", "Regla transaccional encontrada.", Qg2ResponseShape.Schema, "ClearingHouseTransactionRuleDto"),
                new("404", "No existe una regla transaccional con el identificador indicado.", Qg2ResponseShape.NoBody)
            ]),
        new(
            "POST",
            "/api/clearing-house-transaction-rules",
            PrimaryStatusCode: "201",
            Parameters: [],
            RequestSchema: "CreateClearingHouseTransactionRuleRequest",
            RequestProperties:
            [
                "clearingHouseId", "transactionNature", "transactionType", "requiresPrenotification",
                "prenotificationMode", "prenotificationLeadBusinessDays", "requiresReceiverIdentificationValidation",
                "receiverIdentificationValidationMode", "appliesToNachaExport", "appliesToMonetaryTransactions",
                "effectiveFrom", "effectiveTo", "normativeSource", "normativeReference", "notes"
            ],
            Responses:
            [
                new("201", "Regla transaccional creada y ubicación canónica devuelta mediante CreatedAtAction.", Qg2ResponseShape.Schema, "ClearingHouseTransactionRuleDto"),
                new("422", "La regla no cumple las validaciones de coherencia, vigencia, metadata o solapamiento del servicio.", Qg2ResponseShape.Untyped)
            ]),
        new(
            "PUT",
            "/api/clearing-house-transaction-rules/{id}",
            PrimaryStatusCode: "200",
            Parameters: [new("id", "path")],
            RequestSchema: "UpdateClearingHouseTransactionRuleRequest",
            RequestProperties:
            [
                "clearingHouseId", "transactionNature", "transactionType", "requiresPrenotification",
                "prenotificationMode", "prenotificationLeadBusinessDays", "requiresReceiverIdentificationValidation",
                "receiverIdentificationValidationMode", "appliesToNachaExport", "appliesToMonetaryTransactions",
                "effectiveFrom", "effectiveTo", "normativeSource", "normativeReference", "notes"
            ],
            Responses:
            [
                new("200", "Regla actualizada o nueva versión funcional creada según los cambios solicitados.", Qg2ResponseShape.Schema, "ClearingHouseTransactionRuleDto"),
                new("422", "La regla no existe o la actualización incumple las validaciones del servicio.", Qg2ResponseShape.Untyped)
            ]),
        new(
            "PATCH",
            "/api/clearing-house-transaction-rules/{id}/activate",
            PrimaryStatusCode: "200",
            Parameters: [new("id", "path")],
            RequestSchema: null,
            RequestProperties: [],
            Responses:
            [
                new("200", "Regla transaccional activada.", Qg2ResponseShape.Schema, "ClearingHouseTransactionRuleDto"),
                new("422", "La regla no existe o su activación produciría un solapamiento activo.", Qg2ResponseShape.Untyped)
            ]),
        new(
            "PATCH",
            "/api/clearing-house-transaction-rules/{id}/deactivate",
            PrimaryStatusCode: "200",
            Parameters: [new("id", "path")],
            RequestSchema: null,
            RequestProperties: [],
            Responses:
            [
                new("200", "Regla transaccional desactivada.", Qg2ResponseShape.Schema, "ClearingHouseTransactionRuleDto"),
                new("422", "No existe una regla transaccional con el identificador indicado.", Qg2ResponseShape.Untyped)
            ]),
        new(
            "GET",
            "/api/cfa-operational-transaction-restrictions",
            PrimaryStatusCode: "200",
            Parameters:
            [
                new("clearingHouseCode", "query", Required: false),
                new("policyVersion", "query", Required: false),
                new("clearingHouseCycleConfigId", "query", Required: false),
                new("transactionType", "query", Required: false),
                new("isPrenotification", "query", Required: false),
                new("isActive", "query", Required: false),
                new("effectiveAt", "query", Required: false)
            ],
            RequestSchema: null,
            RequestProperties: [],
            Responses:
            [
                new("200", "Restricciones operacionales CFA que cumplen los filtros solicitados.", Qg2ResponseShape.Collection, "CfaOperationalTransactionRestrictionDto")
            ],
            ResponseSchemaDocumentation: new(
                "CfaOperationalTransactionRestrictionDto",
                [
                    "id", "clearingHouseId", "clearingHouseCode", "clearingHouseName", "clearingHouseCycleConfigId",
                    "policyVersion", "cycleName", "transactionType", "isPrenotification", "maxAmountPerTransaction",
                    "maxAmountPerCycle", "maxTransactionsPerCycle", "allowedAccountTypes", "effectiveFrom", "effectiveTo",
                    "isActive", "createdAt", "updatedAt", "rowVersion"
                ])),
        new(
            "GET",
            "/api/cfa-operational-transaction-restrictions/{id}",
            PrimaryStatusCode: "200",
            Parameters: [new("id", "path")],
            RequestSchema: null,
            RequestProperties: [],
            Responses:
            [
                new("200", "Restricción operacional CFA encontrada.", Qg2ResponseShape.Schema, "CfaOperationalTransactionRestrictionDto"),
                new("404", "No existe una restricción operacional CFA con el identificador indicado.", Qg2ResponseShape.NoBody)
            ]),
        new(
            "POST",
            "/api/cfa-operational-transaction-restrictions",
            PrimaryStatusCode: "201",
            Parameters: [],
            RequestSchema: "CreateCfaOperationalTransactionRestrictionRequest",
            RequestProperties:
            [
                "clearingHouseCode", "clearingHouseCycleConfigId", "transactionType", "isPrenotification",
                "maxAmountPerTransaction", "maxAmountPerCycle", "maxTransactionsPerCycle", "allowedAccountTypes",
                "effectiveFrom", "effectiveTo", "isActive"
            ],
            Responses:
            [
                new("201", "Restricción operacional CFA creada.", Qg2ResponseShape.Schema, "CfaOperationalTransactionRestrictionDto"),
                new("422", "El alcance, los límites, los valores de enum o la vigencia de la restricción no son válidos.", Qg2ResponseShape.Schema, "CfaOperationalTransactionRestrictionApiError")
            ],
            ResponseSchemaDocumentation: new(
                "CfaOperationalTransactionRestrictionApiError",
                ["code", "message", "currentRowVersion"])),
        new(
            "PUT",
            "/api/cfa-operational-transaction-restrictions/{id}",
            PrimaryStatusCode: "200",
            Parameters: [new("id", "path")],
            RequestSchema: "UpdateCfaOperationalTransactionRestrictionRequest",
            RequestProperties:
            [
                "clearingHouseCode", "clearingHouseCycleConfigId", "transactionType", "isPrenotification",
                "maxAmountPerTransaction", "maxAmountPerCycle", "maxTransactionsPerCycle", "allowedAccountTypes",
                "effectiveFrom", "effectiveTo", "isActive", "expectedRowVersion"
            ],
            Responses:
            [
                new("200", "Restricción operacional CFA actualizada.", Qg2ResponseShape.Schema, "CfaOperationalTransactionRestrictionDto"),
                new("404", "No existe una restricción operacional CFA con el identificador indicado.", Qg2ResponseShape.NoBody),
                new("409", "La restricción cambió respecto de ExpectedRowVersion; la respuesta puede incluir la RowVersion vigente.", Qg2ResponseShape.Schema, "CfaOperationalTransactionRestrictionApiError"),
                new("422", "La versión esperada, el alcance, los límites, los valores de enum o la vigencia no son válidos.", Qg2ResponseShape.Schema, "CfaOperationalTransactionRestrictionApiError")
            ]),
        new(
            "PATCH",
            "/api/cfa-operational-transaction-restrictions/{id}/activate",
            PrimaryStatusCode: "200",
            Parameters: [new("id", "path")],
            RequestSchema: "ChangeCfaOperationalTransactionRestrictionStatusRequest",
            RequestProperties: ["expectedRowVersion", "effectiveTo"],
            Responses:
            [
                new("200", "Restricción operacional CFA activada.", Qg2ResponseShape.Schema, "CfaOperationalTransactionRestrictionDto"),
                new("404", "No existe una restricción operacional CFA con el identificador indicado.", Qg2ResponseShape.NoBody),
                new("409", "La restricción cambió respecto de ExpectedRowVersion; la respuesta puede incluir la RowVersion vigente.", Qg2ResponseShape.Schema, "CfaOperationalTransactionRestrictionApiError"),
                new("422", "ExpectedRowVersion o la fecha final de vigencia no son válidos.", Qg2ResponseShape.Schema, "CfaOperationalTransactionRestrictionApiError")
            ]),
        new(
            "PATCH",
            "/api/cfa-operational-transaction-restrictions/{id}/deactivate",
            PrimaryStatusCode: "200",
            Parameters: [new("id", "path")],
            RequestSchema: "ChangeCfaOperationalTransactionRestrictionStatusRequest",
            RequestProperties: ["expectedRowVersion", "effectiveTo"],
            Responses:
            [
                new("200", "Restricción operacional CFA desactivada.", Qg2ResponseShape.Schema, "CfaOperationalTransactionRestrictionDto"),
                new("404", "No existe una restricción operacional CFA con el identificador indicado.", Qg2ResponseShape.NoBody),
                new("409", "La restricción cambió respecto de ExpectedRowVersion; la respuesta puede incluir la RowVersion vigente.", Qg2ResponseShape.Schema, "CfaOperationalTransactionRestrictionApiError"),
                new("422", "ExpectedRowVersion o la fecha final de vigencia no son válidos.", Qg2ResponseShape.Schema, "CfaOperationalTransactionRestrictionApiError")
            ]),
        new(
            "GET",
            "/api/payment-rails/capability-registry/rails",
            PrimaryStatusCode: "200",
            Parameters: [],
            RequestSchema: null,
            RequestProperties: [],
            Responses:
            [
                new("200", "Rieles publicados por el registro de capacidades.", Qg2ResponseShape.Collection, "PaymentRailRegistryRailItem")
            ],
            ResponseSchemaDocumentation: new(
                "PaymentRailRegistryRailItem",
                ["railCode", "displayName", "isKnownRail", "isOperational", "source", "version"])),
        new(
            "GET",
            "/api/payment-rails/capability-registry/rails/{railCode}/capabilities",
            PrimaryStatusCode: "200",
            Parameters: [new("railCode", "path"), new("asOfUtc", "query", Required: false)],
            RequestSchema: null,
            RequestProperties: [],
            Responses:
            [
                new("200", "Capacidades efectivas del riel para el instante solicitado.", Qg2ResponseShape.Collection, "PaymentRailCapabilityRegistryItem"),
                new("400", "El railCode está vacío o no corresponde a un riel soportado por el servicio.", Qg2ResponseShape.Untyped)
            ],
            ResponseSchemaDocumentation: new(
                "PaymentRailCapabilityRegistryItem",
                [
                    "railCode", "capabilityCode", "state", "source", "notes", "evaluatedAtUtc",
                    "effectiveFromUtc", "effectiveToUtc", "version", "changeSource", "changeTicket",
                    "changedBy", "changedAtUtc"
                ])),
        new(
            "GET",
            "/api/payment-rails/capability-registry/rails/{railCode}/capabilities/{capabilityCode}",
            PrimaryStatusCode: "200",
            Parameters:
            [
                new("railCode", "path"),
                new("capabilityCode", "path"),
                new("asOfUtc", "query", Required: false)
            ],
            RequestSchema: null,
            RequestProperties: [],
            Responses:
            [
                new("200", "Capacidad efectiva encontrada para el riel y el instante solicitados.", Qg2ResponseShape.Schema, "PaymentRailCapabilityRegistryItem"),
                new("400", "El railCode está vacío o no corresponde a un riel soportado por el servicio.", Qg2ResponseShape.Untyped),
                new("404", "El capabilityCode no pertenece al catálogo o la capacidad no está disponible para el riel solicitado.", Qg2ResponseShape.Untyped)
            ]),
        new(
            "POST",
            "/api/transaction-prerequisite-policy/preview",
            PrimaryStatusCode: "200",
            Parameters: [],
            RequestSchema: "TransactionPrerequisitePreviewRequest",
            RequestProperties: ["clearingHouseId", "transactionType", "effectiveEntryDate", "appliesToNachaExport"],
            Responses:
            [
                new("200", "Resultado de la evaluación previa de prerequisitos para los datos suministrados.", Qg2ResponseShape.Schema, "TransactionPrerequisitePreviewResponse")
            ],
            ResponseSchemaDocumentation: new(
                "TransactionPrerequisitePreviewResponse",
                [
                    "ruleConfigured", "requiresPrenotification", "prenotificationMode",
                    "requiresReceiverIdentificationValidation", "receiverIdentificationValidationMode",
                    "normativeSource", "normativeReference", "decision", "message"
                ]))
    ];

    private readonly ITestOutputHelper _output;

    public OpenApiDocumentGenerationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact(Timeout = 20_000)]
    public async Task GenerateAndSerialize_ShouldCompleteWithBearerAndWithoutHeuristicDocumentation()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ApplicationName = typeof(AchCyclesController).Assembly.FullName,
            EnvironmentName = Environments.Production
        });
        builder.WebHost.UseTestServer();
        builder.Services.AddWebApi(builder.Configuration);

        await using var app = builder.Build();
        app.MapControllers();
        app.MapPublicHealthEndpoints();

        var provider = app.Services.GetRequiredKeyedService<IOpenApiDocumentProvider>("v1");
        var generationTimer = Stopwatch.StartNew();
        var document = await Task.Run(
                () => provider.GetOpenApiDocumentAsync(timeout.Token),
                CancellationToken.None)
            .WaitAsync(timeout.Token);
        generationTimer.Stop();

        await using var jsonStream = new MemoryStream();
        var serializationTimer = Stopwatch.StartNew();
        await document.SerializeAsJsonAsync(jsonStream, OpenApiSpecVersion.OpenApi3_1, timeout.Token)
            .WaitAsync(timeout.Token);
        serializationTimer.Stop();

        var jsonBytes = jsonStream.ToArray();
        using var json = JsonDocument.Parse(jsonBytes);
        var root = json.RootElement;
        var paths = root.GetProperty("paths");
        var components = root.GetProperty("components");
        var schemas = components.GetProperty("schemas");
        var operations = CountOperations(paths);

        _output.WriteLine(
            "OpenAPI generation={0:F3}ms serialization={1:F3}ms paths={2} operations={3} schemas={4} size={5}bytes",
            generationTimer.Elapsed.TotalMilliseconds,
            serializationTimer.Elapsed.TotalMilliseconds,
            paths.EnumerateObject().Count(),
            operations,
            schemas.EnumerateObject().Count(),
            jsonBytes.Length);

        root.GetProperty("openapi").GetString().Should().StartWith("3.");
        paths.EnumerateObject().Should().NotBeEmpty();
        operations.Should().BeGreaterThan(0);
        schemas.EnumerateObject().Should().NotBeEmpty();

        var info = root.GetProperty("info");
        info.GetProperty("title").GetString().Should().Be("ACHInterbank API");
        info.GetProperty("version").GetString().Should().Be("v1");

        var description = info.GetProperty("description").GetString();
        description.Should().NotBeNullOrWhiteSpace();
        description.Should().Contain("ACHInterbank");
        description.Should().Contain("contrato");
        description.Should().Contain("ACH Colombia");
        description.Should().Contain("CENIT");
        description.Should().Contain("NACHA-M");
        description.Should().Contain("cámaras independientes");
        description.Should().Contain("Las reglas de una cámara no deben interpretarse automáticamente como aplicables a la otra");
        description.Should().Contain("Bearer JWT");
        description.Should().Contain("tags funcionales");
        description.Should().Contain("operationId");
        description.Should().Contain("Idempotency-Key");
        description.Should().Contain("X-Correlation-ID");
        description.Should().Contain("no son headers obligatorios globales");
        description.Should().Contain("### Quick Start");
        description.Should().Contain("POST /Auth/login");
        description.Should().Contain("data.token");
        description.Should().Contain("Authorization: Bearer <token>");
        description.Should().Contain("GET /clearing-houses");
        description.Should().Contain("payment-rail-options");
        description.Should().Contain("nacha-profiles");
        description.Should().Contain("PaymentRailCode");
        description.Should().Contain("NachaProfileId");
        description.Should().Contain("clearingHouseCode");
        description.Should().Contain("GET /clearing-houses/{id}/cycles");
        description.Should().Contain("GET /ach-cycles?clearingHouseId={id}");
        description.Should().NotContain("Idempotency-Key es obligatorio");
        description.Should().NotContain("X-Correlation-ID es obligatorio");
        description.Should().NotContain("eyJ", "el Quick Start no debe incrustar un JWT utilizable");
        description.Should().NotContain("http://", "el Quick Start usa rutas relativas y no inventa hosts");
        description.Should().NotContain("https://", "el Quick Start usa rutas relativas y no inventa hosts");
        root.TryGetProperty("servers", out _).Should().BeFalse("SC-107 no debe inventar Servers en OpenAPI");
        description.Should().NotContain("Endpoint de la API ACH Interbank.");
        description.Should().NotContain("Operación del módulo");
        description.Should().NotContain("Consulta información del sistema.");
        description.Should().NotContain("DOCUMENTATION INCOMPLETE");

        var httpOperations = EnumerateHttpOperations(paths).ToArray();
        var apiDescriptions = app.Services
            .GetRequiredService<IApiDescriptionGroupCollectionProvider>()
            .ApiDescriptionGroups.Items
            .SelectMany(group => group.Items)
            .Where(description => !string.IsNullOrWhiteSpace(description.HttpMethod)
                                  && !string.IsNullOrWhiteSpace(description.RelativePath))
            .ToArray();

        AssertSc508FinalSurfaceGate(httpOperations, apiDescriptions);

        var operationsWithoutOperationId = httpOperations
            .Where(item => !item.Operation.TryGetProperty("operationId", out var operationId)
                || operationId.ValueKind != JsonValueKind.String
                || string.IsNullOrWhiteSpace(operationId.GetString()))
            .Select(item => $"{item.Method.ToUpperInvariant()} {item.Path}")
            .ToArray();
        operationsWithoutOperationId.Should().BeEmpty("todas las operaciones públicas deben tener operationId");

        var operationIds = httpOperations
            .Select(item => item.Operation.GetProperty("operationId").GetString()!)
            .ToArray();
        var duplicatedOperationIds = operationIds
            .GroupBy(operationId => operationId, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();
        duplicatedOperationIds.Should().BeEmpty("los operationId deben ser únicos usando comparación ordinal");

        var taskDefinitionsCollectionOperation = paths.GetProperty("/TaskDefinitions").GetProperty("get");
        var taskDefinitionsByIdOperation = paths.GetProperty("/TaskDefinitions/{id}").GetProperty("get");
        taskDefinitionsCollectionOperation.GetProperty("operationId").GetString()
            .Should().Be("TaskDefinitions_Get_GET_TaskDefinitions");
        taskDefinitionsByIdOperation.GetProperty("operationId").GetString()
            .Should().Be("TaskDefinitions_Get_GET_TaskDefinitions_id");
        taskDefinitionsCollectionOperation.GetProperty("operationId").GetString()
            .Should().NotBe(taskDefinitionsByIdOperation.GetProperty("operationId").GetString(),
                "la ruta debe distinguir acciones con el mismo controller, ActionName y método HTTP");

        operationIds.Should().Contain(UsersController.GetUserByIdRouteName,
            "el operationId explícito derivado de la ruta nombrada debe conservarse");

        foreach (var item in httpOperations)
        {
            item.Operation.TryGetProperty("tags", out var tags).Should().BeTrue(
                $"{item.Method.ToUpperInvariant()} {item.Path} debe tener un tag funcional explícito");
            tags.ValueKind.Should().Be(JsonValueKind.Array);

            var operationTags = tags.EnumerateArray().ToArray();
            operationTags.Should().ContainSingle(
                $"{item.Method.ToUpperInvariant()} {item.Path} debe usar un único tag funcional primario");
            operationTags[0].ValueKind.Should().Be(JsonValueKind.String);

            var tag = operationTags[0].GetString();
            tag.Should().NotBeNullOrWhiteSpace();
            OfficialIntegrationTags.Should().Contain(tag!,
                $"{item.Method.ToUpperInvariant()} {item.Path} solo puede usar la taxonomía oficial");
        }

        GetOperationTags(paths.GetProperty("/api/ach-colombia/file-exchange/transfers").GetProperty("get"))
            .Should().Equal("ACH Colombia");
        GetOperationTags(paths.GetProperty("/api/cenit/chamber-responses").GetProperty("get"))
            .Should().Equal("CENIT");
        GetOperationTags(paths.GetProperty("/Nacha/header").GetProperty("post"))
            .Should().Equal("NACHA-M");

        var generalTransactionTags = GetOperationTags(paths.GetProperty("/Transactions").GetProperty("post"));
        generalTransactionTags.Should().Equal("Transacciones interbancarias");
        generalTransactionTags.Should().NotContain("ACH Colombia");
        generalTransactionTags.Should().NotContain("CENIT");

        foreach (var (method, path) in Qg1Operations)
        {
            AssertQG1(paths, method, path);
        }

        foreach (var operation in Sc508PublicContractOperations)
        {
            AssertSc508PublicContractQG2(paths, apiDescriptions, operation);
        }

        foreach (var operation in Phase5PublicContractOperations)
        {
            AssertSc508PublicContractQG2(paths, apiDescriptions, operation);
        }

        foreach (var operation in IncomingNachaQg2Operations)
        {
            AssertIncomingNachaQG2(paths, schemas, operation);
        }

        foreach (var operation in NachaLegacyQg2Operations)
        {
            AssertNachaLegacyQG2(paths, schemas, operation);
        }

        foreach (var authOperation in AuthQg2Operations)
        {
            AssertAuthQG2(paths, schemas, authOperation);
        }

        foreach (var operation in ChambersCyclesQg2Operations)
        {
            AssertChambersCyclesQG2(paths, schemas, operation);
        }

        foreach (var operation in TransactionsQg2Operations)
        {
            AssertTransactionsQG2(paths, schemas, operation);
        }

        foreach (var operation in BulkIngestionQg2Operations)
        {
            AssertBulkIngestionQG2(paths, schemas, operation);
        }

        foreach (var operation in TraceabilityQg2Operations)
        {
            AssertTraceabilityQG2(paths, schemas, operation);
        }

        foreach (var operation in ReturnsQg2Operations)
        {
            AssertReturnsQG2(paths, schemas, operation);
        }

        foreach (var operation in ResponseOperationsQg2Operations)
        {
            AssertResponseOperationsQG2(paths, schemas, operation);
        }

        foreach (var operation in CenitOperationsQg2Operations)
        {
            AssertCenitOperationsQG2(paths, schemas, operation);
        }

        foreach (var operation in CertificateQg2Operations)
        {
            AssertCertificateQG2(paths, schemas, operation);
        }

        foreach (var operation in DigitalEnvelopeQg2Operations)
        {
            AssertDigitalEnvelopeQG2(paths, schemas, operation);
        }

        foreach (var operation in NachaSecurityQg2Operations)
        {
            AssertNachaSecurityQG2(paths, schemas, operation);
        }

        foreach (var operation in Sc309Qg2Operations)
        {
            AssertSc309QG2(paths, schemas, operation);
        }

        foreach (var operation in Sc401Qg2Operations)
        {
            AssertSc401QG2(paths, schemas, operation);
        }

        foreach (var schemaDocumentation in Sc401AdditionalSchemaDocumentation)
        {
            AssertSchemaPropertiesDocumented(schemas, schemaDocumentation, "SC-401");
        }

        foreach (var operation in Sc402Qg2Operations)
        {
            AssertSc402QG2(paths, schemas, operation);
        }

        foreach (var schemaDocumentation in Sc402AdditionalSchemaDocumentation)
        {
            AssertSchemaPropertiesDocumented(schemas, schemaDocumentation, "SC-402");
        }

        foreach (var operation in Sc403Qg2Operations)
        {
            AssertSc403QG2(paths, schemas, operation);
        }

        foreach (var schemaDocumentation in Sc403AdditionalSchemaDocumentation)
        {
            AssertSchemaPropertiesDocumented(schemas, schemaDocumentation, "SC-403");
        }

        foreach (var operation in Sc404Qg2Operations)
        {
            AssertSc404QG2(paths, schemas, operation);
        }

        foreach (var schemaDocumentation in Sc404AdditionalSchemaDocumentation)
        {
            AssertSchemaPropertiesDocumented(schemas, schemaDocumentation, "SC-404");
        }

        foreach (var operation in Sc405Qg2Operations)
        {
            AssertSc405QG2(paths, schemas, operation);
        }

        foreach (var schemaDocumentation in Sc405AdditionalSchemaDocumentation)
        {
            AssertSchemaPropertiesDocumented(schemas, schemaDocumentation, "SC-405");
        }

        foreach (var operation in Sc406Qg2Operations)
        {
            AssertSc406QG2(paths, schemas, operation);
        }

        foreach (var schemaDocumentation in Sc406SchemaDocumentation)
        {
            AssertSchemaPropertiesDocumented(schemas, schemaDocumentation, "SC-406");
        }

        foreach (var operation in Sc407Qg2Operations)
        {
            AssertSc407QG2(paths, schemas, operation);
        }

        foreach (var schemaDocumentation in Sc407SchemaDocumentation)
        {
            AssertSchemaPropertiesDocumented(schemas, schemaDocumentation, "SC-407");
        }

        foreach (var operation in Sc408Qg2Operations)
        {
            AssertSc408QG2(paths, schemas, operation);
        }

        foreach (var schemaDocumentation in Sc408AdditionalSchemaDocumentation)
        {
            AssertSchemaPropertiesDocumented(schemas, schemaDocumentation, "SC-408");
        }

        foreach (var operation in Sc409Qg2Operations)
        {
            AssertSc409QG2(paths, schemas, operation);
        }

        foreach (var schemaDocumentation in Sc409AdditionalSchemaDocumentation)
        {
            AssertSchemaPropertiesDocumented(schemas, schemaDocumentation, "SC-409");
        }

        foreach (var operation in Sc410ControllerQg2Operations)
        {
            AssertSc410ControllerQG2(paths, schemas, operation);
        }

        foreach (var operation in Sc410HealthQg2Operations)
        {
            AssertSc410HealthQG2(paths, operation);
        }

        AssertSchemaPropertiesDocumented(
            schemas,
            new Qg2SchemaDocumentation("BaseResponseModel", ["statusCode", "sucess", "message", "data"]),
            "SC-410");

        BaselineQgOperations.Should().HaveCount(245, "UX3 incorpora el GET contextual del editor de campo al baseline contractual existente");
        BaselineQgOperations.Distinct().Should().HaveCount(245, "el baseline QG no debe contener METHOD + PATH duplicados");
        Qg1Operations.Should().HaveCount(327, "QG-1 = baseline 245 + SC-508 80 + FASE 5 2 operaciones administrativas ACH Colombia");
        Qg1Operations.Distinct().Should().HaveCount(327, "QG-1 final no debe contener METHOD + PATH duplicados");
        Qg2Operations.Should().HaveCount(327, "QG-2 final debe contener las mismas 327 operaciones que QG-1");
        Qg2Operations.Should().BeEquivalentTo(Qg1Operations, "QG-1 y QG-2 finales deben compartir exactamente el mismo METHOD + PATH set");
        AssertQg2ParameterDocumentation(paths);
        AssertQg2ResponseStatusAudit(paths);
        AssertSc506SchemaDocumentationAudit(schemas);
        Qg1Operations.Count(operation => operation.Path.StartsWith("/api/reports/", StringComparison.Ordinal))
            .Should().Be(20, "SC-409 no debe reabrir Reports SC-406/SC-407");
        Sc408Qg2Operations.Should().HaveCount(9, "SC-408 debe cubrir exclusivamente sus nueve operaciones autorizadas");
        Sc408Qg2Operations.Select(operation => operation.ControllerType).Distinct().Should().HaveCount(2,
            "SC-408 debe limitarse a IntegrationCatalogController y AchColombiaFileExchangeController");
        Sc409Qg2Operations.Should().HaveCount(14, "UX3 añade el GET contextual del editor de campo a la cobertura contractual");
        Sc409Qg2Operations.Select(operation => operation.ControllerType).Distinct().Should().HaveCount(2,
            "SC-409 debe limitarse a NachaConfigProfilesController y NachaConfigProfilesReadOnlyController");
        Sc409Qg2Operations.Should().OnlyContain(operation => string.Equals(operation.Method, "GET", StringComparison.Ordinal),
            "SC-409 no debe absorber ninguna mutación NACHA");
        Qg1Operations.Count(operation => operation.Path.StartsWith("/nacha-config/", StringComparison.Ordinal))
            .Should().Be(8, "UX3 incorpora ocho GET de NachaConfigProfilesController");
        Qg1Operations.Count(operation => operation.Path.StartsWith("/api/ach/nacha/config-profiles", StringComparison.Ordinal))
            .Should().Be(6, "SC-409 debe incorporar las seis operaciones GET de NachaConfigProfilesReadOnlyController");
        Sc410ControllerQg2Operations.Should().HaveCount(3, "SC-410 debe cubrir exclusivamente tres acciones de controllers autorizadas");
        Sc410ControllerQg2Operations.Select(operation => operation.ControllerType).Distinct().Should().HaveCount(2,
            "SC-410 debe limitarse a AuditLogsController y NavigationLogsController");
        Sc410HealthQg2Operations.Should().HaveCount(2, "SC-410 debe cubrir exclusivamente las dos rutas health autorizadas");
        var sc410Qg2Keys = Sc410ControllerQg2Operations
            .Select(operation => (operation.Method, operation.Path))
            .Concat(Sc410HealthQg2Operations.Select(operation => (operation.Method, operation.Path)))
            .ToArray();
        sc410Qg2Keys.Should().HaveCount(5, "SC-410 debe agregar exactamente cinco METHOD + PATH a QG-2");
        sc410Qg2Keys.Distinct().Should().HaveCount(5, "SC-410 no debe duplicar METHOD + PATH en QG-2");
        var sc410Qg1Keys = Qg1Operations
            .Where(operation => sc410Qg2Keys.Contains((operation.Method, operation.Path)))
            .ToArray();
        sc410Qg1Keys.Should().BeEquivalentTo(sc410Qg2Keys,
            "QG-1 y QG-2 deben contener exactamente el mismo conjunto de cinco operaciones SC-410");
        Qg1Operations.Count(operation => operation.Path == "/api/auth-logs").Should().Be(0,
            "AuthLogsController permanece expresamente fuera de SC-410 y de QG");
        Qg1Operations.Count(operation => operation.Path.StartsWith("/api/transactions/outgoing-monitoring", StringComparison.Ordinal)).Should().Be(0,
            "OutgoingTransactionMonitoringController no se reasigna a SC-410");

        paths.TryGetProperty("/api/nacha-security/digital-envelope/testRSA", out _).Should().BeFalse(
            "SC-307 debe conservar testRSA fuera del documento OpenAPI");
        var testRsaMethod = typeof(SobreDigitalController).GetMethod("testRSA");
        testRsaMethod.Should().NotBeNull();
        testRsaMethod!.GetCustomAttributes(inherit: true)
            .OfType<Microsoft.AspNetCore.Mvc.ApiExplorerSettingsAttribute>()
            .Should().ContainSingle(attribute => attribute.IgnoreApi,
                "testRSA debe seguir excluida mediante ApiExplorerSettings(IgnoreApi = true)");

        foreach (var operation in TransactionPoliciesQg2Operations)
        {
            AssertTransactionPoliciesQG2(paths, schemas, operation);
        }

        AssertSchemaPropertiesDocumented(
            schemas,
            new Qg2SchemaDocumentation(
                "NachaRecordFieldDto",
                ["id", "fieldName", "startPosition", "length", "padChar", "justification", "dbColumn", "format"]),
            "SC-303 layouts NACHA-M legacy");

        AssertSchemaPropertiesDocumented(
            schemas,
            new Qg2SchemaDocumentation(
                "AddendaDto",
                ["addendaType", "businessType", "information", "purpose", "reference", "collectorId", "receiverCustomerCode", "serviceDescription", "returnReasonCode", "originalTraceNumber", "newTraceNumber"]),
            "POST /Transactions");

        AssertSchemaPropertiesDocumented(
            schemas,
            new Qg2SchemaDocumentation(
                "BulkAchTransactionItemRequest",
                ["amount", "transactionExternalId", "reference", "type", "accountType", "isPrenotification", "destinationInstitutionId", "sourceAccountNumber", "destinationAccountNumber", "recipientIdNumber", "recipientName", "requiresIdentityValidation", "companyName", "companyIdentification", "companyEntryDescriptionId", "sourcePersonType", "recipientPersonType", "addendas"]),
            "POST /Transactions/bulk");
        AssertSchemaPropertiesDocumented(
            schemas,
            new Qg2SchemaDocumentation(
                "BulkAchTransactionItemResult",
                ["index", "transactionExternalId", "reference", "succeeded", "transactionId", "errorCode", "errorMessage"]),
            "POST /Transactions/bulk");

        AssertSchemaPropertiesDocumented(
            schemas,
            new Qg2SchemaDocumentation(
                "BulkBatchItemDto",
                ["itemId", "itemIndex", "reference", "status", "message", "transactionId"]),
            "GET /api/transactions/bulk-ingestion/{batchId}/items");
        AssertSchemaPropertiesDocumented(
            schemas,
            new Qg2SchemaDocumentation(
                "BulkBatchAttemptDto",
                ["attemptId", "attemptNumber", "triggerType", "scope", "triggeredBy", "triggeredAtUtc", "status", "jobId", "startedAtUtc", "finishedAtUtc", "totalProcessed", "totalSucceeded", "totalFailed", "resultMessage"]),
            "GET /api/transactions/bulk-ingestion/{batchId}/summary");

        AssertSchemaPropertiesDocumented(
            schemas,
            new Qg2SchemaDocumentation(
                "AchTraceabilityEventDto",
                ["id", "createdAt", "fromState", "toState", "source", "reasonCode", "payloadJson"]),
            "GET /api/ach-traceability/transactions/{transactionId}");

        AssertSchemaPropertiesDocumented(
            schemas,
            new Qg2SchemaDocumentation(
                "ReturnSelectionItemDto",
                ["transactionId", "returnReasonCode", "cenitOperationalEvidence", "achColombiaR10Basis"]),
            "POST /ach-returns/generate-file");
        AssertSchemaPropertiesDocumented(
            schemas,
            new Qg2SchemaDocumentation(
                "CenitIncomingReturnOperationalEvidence",
                [
                    "prenotificationDirection", "returnRequestDate", "immediateReturnCycleConfirmed",
                    "fundsAvailabilityRequired", "fundsAvailabilityConfirmed",
                    "confirmationToOriginatorRecorded", "receiverRejectionDeadlineDate"
                ]),
            "POST /ach-returns/generate-file");

        AssertSchemaPropertiesDocumented(
            schemas,
            new Qg2SchemaDocumentation(
                "AchReturnOfReturnEligibilityFailure",
                ["code", "message", "field"]),
            "POST /ach-returns/return-of-return/evaluate");

        AssertResponseStatusesAbsent(paths, "GET", "/incoming-nacha-command-center/ingestions/{ingestionId}/validations", ["400", "401", "403", "409", "422", "500"]);
        AssertResponseStatusesAbsent(paths, "GET", "/incoming-nacha-command-center/ingestions/{ingestionId}/batches", ["400", "401", "403", "404", "409", "422", "500"]);
        AssertResponseStatusesAbsent(paths, "GET", "/incoming-nacha-command-center/ingestions/{ingestionId}/transactions", ["400", "401", "403", "404", "409", "422", "500"]);
        AssertResponseStatusesAbsent(paths, "GET", "/incoming-nacha-command-center/ingestions/{ingestionId}/transactions/{entryDetailId}/addendas", ["400", "401", "403", "404", "409", "422", "500"]);
        AssertResponseStatusesAbsent(paths, "GET", "/incoming-nacha-command-center/observability/summary", ["400", "401", "403", "404", "409", "422", "500"]);
        AssertResponseStatusesAbsent(paths, "GET", "/incoming-nacha-command-center/ingestions", ["400", "401", "403", "404", "409", "422", "500"]);
        AssertResponseStatusesAbsent(paths, "GET", "/incoming-nacha-command-center/ingestions/{ingestionId}", ["400", "401", "403", "409", "422", "500"]);
        AssertResponseStatusesAbsent(paths, "GET", "/incoming-nacha-command-center/orphans", ["400", "401", "403", "404", "409", "422", "500"]);
        AssertResponseStatusesAbsent(paths, "GET", "/incoming-nacha-command-center/orphans/{linkId}", ["400", "401", "403", "409", "422", "500"]);
        AssertResponseStatusesAbsent(paths, "GET", "/incoming-nacha-command-center/orphans/{linkId}/candidates", ["400", "401", "403", "409", "422", "500"]);
        AssertResponseStatusesAbsent(paths, "POST", "/incoming-nacha-command-center/orphans/{linkId}/resolve", ["401", "403", "422", "500"]);
        AssertResponseStatusesAbsent(paths, "GET", "/incoming-nacha-command-center/queue", ["400", "401", "403", "404", "409", "422", "500"]);
        AssertResponseStatusesAbsent(paths, "GET", "/incoming-nacha-command-center/queue/{queueId}", ["400", "401", "403", "409", "422", "500"]);
        AssertResponseStatusesAbsent(paths, "POST", "/incoming-nacha-command-center/queue/{queueId}/retry", ["400", "401", "403", "404", "422", "500"]);
        AssertResponseStatusesAbsent(paths, "POST", "/incoming-nacha-command-center/queue/{queueId}/unblock", ["400", "401", "403", "404", "422", "500"]);
        AssertResponseStatusesAbsent(paths, "POST", "/incoming-nacha-command-center/queue/{queueId}/requeue", ["400", "401", "403", "404", "422", "500"]);
        AssertResponseStatusesAbsent(paths, "POST", "/incoming-nacha-command-center/queue/{queueId}/mark-failed-final", ["400", "401", "403", "404", "422", "500"]);

        AssertResponseStatusesAbsent(paths, "POST", "/Transactions/bulk/submit", ["401", "403", "409"]);
        AssertResponseStatusesAbsent(paths, "POST", "/Transactions/bulk", ["401", "403", "409"]);
        AssertResponseStatusesAbsent(paths, "GET", "/Transactions/{id}/integration-result", ["401", "403", "500"]);
        AssertResponseStatusesAbsent(paths, "GET", "/Transactions/{id}/integration-readiness", ["401", "403", "500"]);

        AssertResponseStatusesAbsent(paths, "POST", "/api/transactions/bulk-ingestion/upload", ["401", "403", "409"]);
        AssertResponseStatusesAbsent(paths, "GET", "/api/transactions/bulk-ingestion/{batchId}", ["401", "403", "500"]);
        AssertResponseStatusesAbsent(paths, "GET", "/api/transactions/bulk-ingestion/{batchId}/items", ["400", "401", "403", "500"]);
        AssertResponseStatusesAbsent(paths, "GET", "/api/transactions/bulk-ingestion/{batchId}/summary", ["401", "403", "500"]);
        AssertResponseStatusesAbsent(paths, "POST", "/api/transactions/bulk-ingestion/{batchId}/retry", ["401", "403", "409", "500"]);
        AssertResponseStatusesAbsent(paths, "POST", "/api/transactions/bulk-ingestion/{batchId}/cancel", ["401", "403", "409", "500"]);

        AssertResponseStatusesAbsent(paths, "POST", "/api/ach-traceability/sol02/{transactionId}/certify", ["401", "403", "500"]);
        AssertResponseStatusesAbsent(paths, "GET", "/api/ach-traceability/transactions/{transactionId}", ["401", "403", "500"]);
        AssertResponseStatusesAbsent(paths, "GET", "/api/ach-traceability/report", ["400", "401", "403", "500"]);

        AssertResponseStatusesAbsent(paths, "GET", "/ach-returns/cycles/{cycleId}/transactions", ["400", "401", "403", "404", "500"]);
        AssertResponseStatusesAbsent(paths, "POST", "/ach-returns/generate-file", ["400", "401", "403", "500"]);
        AssertResponseStatusesAbsent(paths, "GET", "/return-reasons", ["400", "401", "403", "404", "500"]);

        AssertResponseStatusesAbsent(paths, "POST", "/ach-returns/return-of-return/cenit/out", ["401", "403", "404", "422", "500"]);
        AssertResponseStatusesAbsent(paths, "POST", "/ach-returns/return-of-return/evaluate", ["401", "403", "404", "409", "422", "500"]);
        AssertResponseStatusesAbsent(paths, "POST", "/ach-returns/return-of-return/generate-audit-file", ["401", "403", "404", "422", "500"]);
        AssertResponseStatusesAbsent(paths, "POST", "/ach-returns/return-of-return/generate-nacha-file", ["401", "403", "404", "422", "500"]);

        AssertResponseStatusesAbsent(paths, "POST", "/api/ach/responses/process", ["401", "403", "404", "409", "500"]);
        AssertResponseStatusesAbsent(paths, "POST", "/api/ach/responses/notifications/send", ["401", "403", "409", "422", "500"]);
        AssertResponseStatusesAbsent(paths, "GET", "/api/ach/responses", ["400", "401", "403", "404", "409", "422", "500"]);
        AssertResponseStatusesAbsent(paths, "GET", "/api/ach/responses/dashboard", ["401", "403", "404", "409", "422", "500"]);
        AssertResponseStatusesAbsent(paths, "GET", "/api/ach/responses/{id}", ["400", "401", "403", "409", "422", "500"]);
        AssertResponseStatusesAbsent(paths, "GET", "/api/ach/responses/{id}/notification-attempts", ["400", "401", "403", "404", "409", "422", "500"]);
        AssertResponseStatusesAbsent(paths, "GET", "/api/ach/response-status-mappings", ["401", "403", "404", "409", "422", "500"]);

        AssertResponseStatusesAbsent(paths, "GET", "/api/clearing-houses/{clearingHouseId}/transaction-policies", ["400", "401", "403", "404", "409", "500"]);
        AssertResponseStatusesAbsent(paths, "GET", "/api/clearing-houses/{clearingHouseId}/transaction-policies/current", ["400", "401", "403", "409", "500"]);
        AssertResponseStatusesAbsent(paths, "GET", "/api/clearing-houses/{clearingHouseId}/transaction-policies/{id}", ["400", "401", "403", "409", "500"]);
        AssertResponseStatusesAbsent(paths, "POST", "/api/clearing-houses/{clearingHouseId}/transaction-policies", ["400", "401", "403", "404", "409", "500"]);
        AssertResponseStatusesAbsent(paths, "PATCH", "/api/clearing-houses/{clearingHouseId}/transaction-policies/{id}/metadata", ["400", "401", "403", "404", "409", "500"]);
        AssertResponseStatusesAbsent(paths, "POST", "/api/clearing-houses/{clearingHouseId}/transaction-policies/{id}/close", ["400", "401", "403", "404", "409", "500"]);
        AssertResponseStatusesAbsent(paths, "POST", "/api/clearing-houses/{clearingHouseId}/transaction-policies/{id}/activate", ["400", "401", "403", "404", "409", "500"]);
        AssertResponseStatusesAbsent(paths, "POST", "/api/clearing-houses/{clearingHouseId}/transaction-policies/preview", ["400", "401", "403", "404", "409", "500"]);

        AssertResponseStatusesAbsent(paths, "GET", "/api/clearing-house-transaction-rules", ["400", "401", "403", "404", "409", "422", "500"]);
        AssertResponseStatusesAbsent(paths, "GET", "/api/clearing-house-transaction-rules/{id}", ["400", "401", "403", "409", "422", "500"]);
        AssertResponseStatusesAbsent(paths, "POST", "/api/clearing-house-transaction-rules", ["400", "401", "403", "404", "409", "500"]);
        AssertResponseStatusesAbsent(paths, "PUT", "/api/clearing-house-transaction-rules/{id}", ["400", "401", "403", "404", "409", "500"]);
        AssertResponseStatusesAbsent(paths, "PATCH", "/api/clearing-house-transaction-rules/{id}/activate", ["400", "401", "403", "404", "409", "500"]);
        AssertResponseStatusesAbsent(paths, "PATCH", "/api/clearing-house-transaction-rules/{id}/deactivate", ["400", "401", "403", "404", "409", "500"]);

        AssertResponseStatusesAbsent(paths, "GET", "/api/cfa-operational-transaction-restrictions", ["400", "401", "403", "404", "409", "422", "500"]);
        AssertResponseStatusesAbsent(paths, "GET", "/api/cfa-operational-transaction-restrictions/{id}", ["400", "401", "403", "409", "422", "500"]);
        AssertResponseStatusesAbsent(paths, "POST", "/api/cfa-operational-transaction-restrictions", ["400", "401", "403", "404", "409", "500"]);
        AssertResponseStatusesAbsent(paths, "PUT", "/api/cfa-operational-transaction-restrictions/{id}", ["400", "401", "403", "500"]);
        AssertResponseStatusesAbsent(paths, "PATCH", "/api/cfa-operational-transaction-restrictions/{id}/activate", ["400", "401", "403", "500"]);
        AssertResponseStatusesAbsent(paths, "PATCH", "/api/cfa-operational-transaction-restrictions/{id}/deactivate", ["400", "401", "403", "500"]);

        AssertResponseStatusesAbsent(paths, "GET", "/api/payment-rails/capability-registry/rails", ["400", "401", "403", "404", "409", "422", "500"]);
        AssertResponseStatusesAbsent(paths, "GET", "/api/payment-rails/capability-registry/rails/{railCode}/capabilities", ["401", "403", "404", "409", "422", "500"]);
        AssertResponseStatusesAbsent(paths, "GET", "/api/payment-rails/capability-registry/rails/{railCode}/capabilities/{capabilityCode}", ["401", "403", "409", "422", "500"]);

        AssertResponseStatusesAbsent(paths, "POST", "/api/transaction-prerequisite-policy/preview", ["400", "401", "403", "404", "409", "422", "500"]);

        AssertResponseStatusesAbsent(paths, "GET", "/nacha-record-definitions", ["400", "401", "403", "404", "409", "410", "422", "500"]);
        AssertResponseStatusesAbsent(paths, "GET", "/nacha-record-definitions/{id}", ["400", "401", "403", "409", "410", "422", "500"]);
        AssertResponseStatusesAbsent(paths, "POST", "/nacha-record-definitions", ["200", "201", "204", "400", "401", "403", "404", "409", "422", "500"]);
        AssertResponseStatusesAbsent(paths, "PUT", "/nacha-record-definitions/{id}", ["200", "201", "204", "400", "401", "403", "404", "409", "422", "500"]);
        AssertResponseStatusesAbsent(paths, "DELETE", "/nacha-record-definitions/{id}", ["200", "201", "204", "400", "401", "403", "404", "409", "422", "500"]);
        AssertResponseStatusesAbsent(paths, "GET", "/nacha-layouts", ["400", "401", "403", "404", "409", "410", "422", "500"]);
        AssertResponseStatusesAbsent(paths, "GET", "/nacha-layouts/{id}", ["400", "401", "403", "409", "410", "422", "500"]);
        AssertResponseStatusesAbsent(paths, "POST", "/nacha-layouts", ["200", "201", "204", "400", "401", "403", "404", "409", "422", "500"]);
        AssertResponseStatusesAbsent(paths, "PUT", "/nacha-layouts/{id}", ["200", "201", "204", "400", "401", "403", "404", "409", "422", "500"]);
        AssertResponseStatusesAbsent(paths, "DELETE", "/nacha-layouts/{id}", ["200", "201", "204", "400", "401", "403", "404", "409", "422", "500"]);

        var loginOperation = paths.GetProperty("/Auth/login").GetProperty("post");
        GetRequestExample(loginOperation).GetProperty("username").GetString().Should().Be("usuario.demo");
        var loginSuccessExample = GetResponseExample(loginOperation, "200");
        loginSuccessExample.GetProperty("statusCode").GetInt32().Should().Be(200);
        loginSuccessExample.GetProperty("sucess").GetBoolean().Should().BeTrue();
        loginSuccessExample.GetProperty("data").GetProperty("token").GetString().Should().Be("jwt-ficticio-no-utilizable");

        var forgotOperation = paths.GetProperty("/Auth/forgot-password").GetProperty("post");
        GetRequestExample(forgotOperation).GetProperty("email").GetString().Should().Be("persona@example.test");
        GetResponseExample(forgotOperation, "200").GetProperty("data").GetString()
            .Should().Be("Si el correo existe, se ha enviado un enlace de recuperación");

        var resetOperation = paths.GetProperty("/Auth/reset-password").GetProperty("post");
        GetRequestExample(resetOperation).GetProperty("token").GetString().Should().Be("token-recuperacion-ficticio");
        GetResponseExample(resetOperation, "200").GetProperty("data").GetString()
            .Should().Be("Contraseña actualizada correctamente");

        var refreshOperation = paths.GetProperty("/Auth/refresh").GetProperty("post");
        refreshOperation.TryGetProperty("requestBody", out _).Should().BeFalse(
            "POST /Auth/refresh obtiene la identidad del JWT actual y no recibe request body");
        GetResponseExample(refreshOperation, "200")
            .GetProperty("data")
            .GetProperty("username")
            .GetString()
            .Should().Be("usuario.demo");

        AssertSc505CriticalFlowExamples(paths);

        var bearer = components.GetProperty("securitySchemes").GetProperty("Bearer");
        bearer.GetProperty("type").GetString().Should().Be("http");
        bearer.GetProperty("scheme").GetString().Should().Be("bearer");
        bearer.GetProperty("bearerFormat").GetString().Should().Be("JWT");

        var protectedOperation = paths.GetProperty("/ach-cycles").GetProperty("get");
        var protectedSecurity = protectedOperation.GetProperty("security");
        protectedSecurity.GetArrayLength().Should().BeGreaterThan(0);
        protectedSecurity[0].TryGetProperty("Bearer", out _).Should().BeTrue();

        var anonymousOperation = paths.GetProperty("/api/users/branding").GetProperty("get");
        anonymousOperation.TryGetProperty("security", out _).Should().BeFalse();

        foreach (var item in httpOperations)
        {
            var headerParameters = GetHeaderParameters(item.Operation);
            headerParameters
                .Where(parameter => string.Equals(GetParameterName(parameter), "Authorization", StringComparison.OrdinalIgnoreCase))
                .Should().BeEmpty($"{item.Method.ToUpperInvariant()} {item.Path} no debe duplicar Authorization como parámetro header");

            var duplicatedHeaders = headerParameters
                .Select(GetParameterName)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .GroupBy(name => name!, StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ToArray();
            duplicatedHeaders.Should().BeEmpty(
                $"{item.Method.ToUpperInvariant()} {item.Path} no debe contener parámetros header duplicados");
        }

        AssertHeaderParameter(
            paths.GetProperty("/api/ach-colombia/file-exchange/outbound/execute").GetProperty("post"),
            "Idempotency-Key");
        AssertHeaderParameter(
            paths.GetProperty("/api/ach-claims/dev14").GetProperty("post"),
            "Idempotency-Key");
        AssertHeaderAbsent(
            paths.GetProperty("/api/ach-colombia/file-exchange/transfers/{id}/reprocess").GetProperty("post"),
            "Idempotency-Key");

        AssertHeaderParameter(
            paths.GetProperty("/api/ach/response-status-mappings").GetProperty("post"),
            "X-Correlation-ID");
        AssertHeaderParameter(
            paths.GetProperty("/api/transactions/outgoing-monitoring").GetProperty("get"),
            "X-Correlation-ID");
        AssertHeaderParameter(
            paths.GetProperty("/api/scheduler/tasks/{taskCode}/pause").GetProperty("post"),
            "X-Correlation-ID");
        AssertHeaderAbsent(
            paths.GetProperty("/api/scheduler/tasks/{taskCode}/execute").GetProperty("post"),
            "X-Correlation-ID");

        paths.GetProperty("/Nacha/header").GetProperty("post").ValueKind.Should().Be(JsonValueKind.Object);
        paths.GetProperty("/Transactions").GetProperty("post").ValueKind.Should().Be(JsonValueKind.Object);

        AssertDoesNotContainHeuristicDocumentation(protectedOperation);

        var nachaHeaderProperties = schemas.GetProperty("NachaHeader").GetProperty("properties");
        nachaHeaderProperties.TryGetProperty("clearingHouse", out _).Should().BeFalse();
        nachaHeaderProperties.TryGetProperty("batches", out _).Should().BeFalse();

        var transactionProperties = schemas.GetProperty("AchTransaction").GetProperty("properties");
        transactionProperties.TryGetProperty("sourceInstitution", out _).Should().BeFalse();
        transactionProperties.TryGetProperty("achCycle", out _).Should().BeFalse();
        transactionProperties.TryGetProperty("addendas", out _).Should().BeFalse();
        transactionProperties.TryGetProperty("fileExportMemberships", out _).Should().BeFalse();

        AssertAllReferencesAreLocalAndResolvable(root);
    }

    [Fact]
    public void QG1Assertion_ShouldDetectBasicDocumentationRegressions()
    {
        var validOperation = new OpenApiOperation
        {
            Summary = "Validaciones de admisión del archivo NACHA-M entrante",
            Description = "Expone el resultado funcional de admisión con código estable y detalle verificable.",
            OperationId = "IncomingNachaCommandCenter_GetIngestionValidations_GET_example"
        };

        Action valid = () => AssertQG1Operation(
            validOperation,
            ["NACHA-M"],
            "GET",
            "/quality-gate/example");
        valid.Should().NotThrow();

        var placeholderSummary = new OpenApiOperation
        {
            Summary = "Endpoint de la API ACH Interbank.",
            Description = validOperation.Description,
            OperationId = validOperation.OperationId
        };
        Action invalidSummary = () => AssertQG1Operation(
            placeholderSummary,
            ["NACHA-M"],
            "GET",
            "/quality-gate/example");
        invalidSummary.Should().Throw<Exception>().WithMessage("*Summary*");

        var emptyDescription = new OpenApiOperation
        {
            Summary = validOperation.Summary,
            Description = "   ",
            OperationId = validOperation.OperationId
        };
        Action invalidDescription = () => AssertQG1Operation(
            emptyDescription,
            ["NACHA-M"],
            "GET",
            "/quality-gate/example");
        invalidDescription.Should().Throw<Exception>().WithMessage("*Description*");
    }

    [Fact(Timeout = 20_000)]
    public async Task OpenApiEndpoint_ShouldServeDirectDocumentExpectedByScalar()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ApplicationName = typeof(AchCyclesController).Assembly.FullName,
            EnvironmentName = Environments.Production
        });
        builder.WebHost.UseTestServer();
        builder.Services.AddWebApi(builder.Configuration);

        await using var app = builder.Build();
        app.MapOpenApi("/openapi/{documentName}.json")
            .AllowAnonymous();
        app.MapControllers();

        await app.StartAsync(timeout.Token);
        using var client = app.GetTestClient();
        using var response = await client.GetAsync("/openapi/v1.json", timeout.Token);
        response.EnsureSuccessStatusCode();

        response.Content.Headers.ContentType?.MediaType
            .Should().Be("application/json");

        var payload = await response.Content.ReadAsByteArrayAsync(timeout.Token);
        using var json = JsonDocument.Parse(payload);
        json.RootElement.GetProperty("openapi").GetString().Should().StartWith("3.");
        json.RootElement.GetProperty("paths").EnumerateObject().Should().NotBeEmpty();
    }

    private sealed record Sc508PublicContractOperation(
        string Method,
        string Path,
        Type ControllerType,
        string ActionName);

    private sealed record Sc508IntentionalExclusionOperation(
        string Method,
        string Path,
        Type ControllerType,
        string ActionName,
        string Reason);

    private sealed record ApiExplorerIgnoredOperation(
        string Method,
        string Path,
        Type? ControllerType,
        string ControllerTypeName,
        string ActionName);

    private sealed record IncomingNachaQg2Operation(
        string Method,
        string Path,
        Qg2Parameter[] Parameters,
        Qg2Response[] Responses,
        Qg2SchemaDocumentation? ResponseSchemaDocumentation = null,
        string? RequestSchema = null,
        string[]? RequestProperties = null);

    private static void AssertIncomingNachaQG2(
        JsonElement paths,
        JsonElement schemas,
        IncomingNachaQg2Operation expected)
    {
        var operationLabel = $"{expected.Method} {expected.Path}";
        paths.TryGetProperty(expected.Path, out var pathItem).Should().BeTrue(
            $"{operationLabel}: la operación incluida en QG-2 debe existir en OpenAPI");
        pathItem.TryGetProperty(expected.Method.ToLowerInvariant(), out var operation).Should().BeTrue(
            $"{operationLabel}: el método incluido en QG-2 debe existir en OpenAPI");

        GetOperationTags(operation).Should().Equal("NACHA-M",
            $"{operationLabel}: debe conservar el tag funcional aprobado para NACHA-M");
        HasBearerSecurity(operation).Should().BeTrue(
            $"{operationLabel}: las operaciones de Incoming NACHA incluidas en QG-2 deben conservar el requisito Bearer publicado");

        AssertHeaderAbsent(operation, "Authorization");
        AssertHeaderAbsent(operation, "Idempotency-Key");
        AssertHeaderAbsent(operation, "X-Correlation-ID");

        foreach (var parameter in expected.Parameters)
        {
            AssertDocumentedOperationParameter(operation, parameter, operationLabel);
        }

        if (expected.RequestSchema is null)
        {
            operation.TryGetProperty("requestBody", out _).Should().BeFalse(
                $"{operationLabel}: la operación sin body no debe publicar requestBody");
        }
        else
        {
            operation.TryGetProperty("requestBody", out var requestBody).Should().BeTrue(
                $"{operationLabel}: debe publicar el request body contractual");
            var requestMediaType = GetJsonMediaType(requestBody.GetProperty("content"), operationLabel, "request");
            SchemaResolvesTo(requestMediaType.GetProperty("schema"), schemas, expected.RequestSchema).Should().BeTrue(
                $"{operationLabel}: el request debe usar el schema {expected.RequestSchema}");

            var requestDocumentation = new Qg2SchemaDocumentation(
                expected.RequestSchema,
                expected.RequestProperties ?? []);
            AssertSchemaPropertiesDocumented(schemas, requestDocumentation, operationLabel);
        }

        operation.TryGetProperty("responses", out var responses).Should().BeTrue(
            $"{operationLabel}: debe publicar responses");
        foreach (var responseExpectation in expected.Responses)
        {
            responses.TryGetProperty(responseExpectation.StatusCode, out var response).Should().BeTrue(
                $"{operationLabel}: debe publicar el status {responseExpectation.StatusCode}");
            response.GetProperty("description").GetString().Should().Be(responseExpectation.Description,
                $"{operationLabel}: el status {responseExpectation.StatusCode} debe conservar su descripción explícita");

            AssertResponseContract(response, schemas, responseExpectation, operationLabel);
        }

        if (expected.ResponseSchemaDocumentation is not null)
        {
            AssertSchemaPropertiesDocumented(schemas, expected.ResponseSchemaDocumentation, operationLabel);
        }
    }

    private sealed record NachaLegacyQg2Operation(
        string Method,
        string Path,
        Qg2Parameter[] Parameters,
        Qg2Response[] Responses,
        Qg2SchemaDocumentation? ResponseSchemaDocumentation = null,
        string? RequestSchema = null,
        string[]? RequestProperties = null);

    private static void AssertNachaLegacyQG2(
        JsonElement paths,
        JsonElement schemas,
        NachaLegacyQg2Operation expected)
    {
        var operationLabel = $"{expected.Method} {expected.Path}";
        paths.TryGetProperty(expected.Path, out var pathItem).Should().BeTrue(
            $"{operationLabel}: la operación SC-303 incluida en QG-2 debe existir en OpenAPI");
        pathItem.TryGetProperty(expected.Method.ToLowerInvariant(), out var operation).Should().BeTrue(
            $"{operationLabel}: el método SC-303 incluido en QG-2 debe existir en OpenAPI");

        GetOperationTags(operation).Should().Equal("NACHA-M",
            $"{operationLabel}: debe conservar el tag funcional aprobado para NACHA-M");
        HasBearerSecurity(operation).Should().BeTrue(
            $"{operationLabel}: las operaciones legacy de definiciones/layouts incluidas en QG-2 deben conservar el requisito Bearer publicado");

        AssertHeaderAbsent(operation, "Authorization");
        AssertHeaderAbsent(operation, "Idempotency-Key");
        AssertHeaderAbsent(operation, "X-Correlation-ID");

        foreach (var parameter in expected.Parameters)
        {
            AssertDocumentedOperationParameter(operation, parameter, operationLabel);
        }

        if (expected.RequestSchema is null)
        {
            operation.TryGetProperty("requestBody", out _).Should().BeFalse(
                $"{operationLabel}: la operación sin body no debe publicar requestBody");
        }
        else
        {
            operation.TryGetProperty("requestBody", out var requestBody).Should().BeTrue(
                $"{operationLabel}: debe publicar el request body contractual");
            var requestMediaType = GetJsonMediaType(requestBody.GetProperty("content"), operationLabel, "request");
            SchemaResolvesTo(requestMediaType.GetProperty("schema"), schemas, expected.RequestSchema).Should().BeTrue(
                $"{operationLabel}: el request debe usar el schema {expected.RequestSchema}");

            AssertSchemaPropertiesDocumented(
                schemas,
                new Qg2SchemaDocumentation(expected.RequestSchema, expected.RequestProperties ?? []),
                operationLabel);
        }

        operation.TryGetProperty("responses", out var responses).Should().BeTrue(
            $"{operationLabel}: debe publicar responses");
        foreach (var responseExpectation in expected.Responses)
        {
            responses.TryGetProperty(responseExpectation.StatusCode, out var response).Should().BeTrue(
                $"{operationLabel}: debe publicar el status {responseExpectation.StatusCode}");
            response.GetProperty("description").GetString().Should().Be(responseExpectation.Description,
                $"{operationLabel}: el status {responseExpectation.StatusCode} debe conservar su descripción explícita");

            AssertResponseContract(response, schemas, responseExpectation, operationLabel);
        }

        if (expected.ResponseSchemaDocumentation is not null)
        {
            AssertSchemaPropertiesDocumented(schemas, expected.ResponseSchemaDocumentation, operationLabel);
        }
    }

    private sealed record AuthQg2Operation(
        string Method,
        string Path,
        bool RequiresBearer,
        string? RequestSchema,
        string[] RequestProperties,
        (string StatusCode, string Description)[] Responses);

    private static void AssertAuthQG2(JsonElement paths, JsonElement schemas, AuthQg2Operation expected)
    {
        var operationLabel = $"{expected.Method} {expected.Path}";
        paths.TryGetProperty(expected.Path, out var pathItem).Should().BeTrue(
            $"{operationLabel}: la operación incluida en QG-2 debe existir en OpenAPI");
        pathItem.TryGetProperty(expected.Method.ToLowerInvariant(), out var operation).Should().BeTrue(
            $"{operationLabel}: el método incluido en QG-2 debe existir en OpenAPI");

        GetOperationTags(operation).Should().Equal("Autenticación y acceso",
            $"{operationLabel}: Auth debe conservar el tag funcional aprobado");

        HasBearerSecurity(operation).Should().Be(expected.RequiresBearer,
            expected.RequiresBearer
                ? $"{operationLabel}: debe requerir Bearer según la metadata de autorización"
                : $"{operationLabel}: debe permanecer anónima sin requisito Bearer");

        AssertHeaderAbsent(operation, "Authorization");
        AssertHeaderAbsent(operation, "Idempotency-Key");
        AssertHeaderAbsent(operation, "X-Correlation-ID");

        if (expected.RequestSchema is null)
        {
            operation.TryGetProperty("requestBody", out _).Should().BeFalse(
                $"{operationLabel}: no debe publicar request body");
        }
        else
        {
            operation.TryGetProperty("requestBody", out var requestBody).Should().BeTrue(
                $"{operationLabel}: debe publicar request body");
            var requestMediaType = GetJsonMediaType(requestBody.GetProperty("content"), operationLabel, "request");
            GetSchemaReferenceName(requestMediaType.GetProperty("schema")).Should().Be(expected.RequestSchema,
                $"{operationLabel}: el request debe usar el contrato {expected.RequestSchema}");

            var schemaProperties = schemas.GetProperty(expected.RequestSchema).GetProperty("properties");
            foreach (var propertyName in expected.RequestProperties)
            {
                schemaProperties.TryGetProperty(propertyName, out var property).Should().BeTrue(
                    $"{operationLabel}: el schema {expected.RequestSchema} debe publicar la propiedad {propertyName}");
                property.TryGetProperty("description", out var propertyDescription).Should().BeTrue(
                    $"{operationLabel}: {expected.RequestSchema}.{propertyName} debe exponer documentación XML");
                propertyDescription.GetString().Should().NotBeNullOrWhiteSpace(
                    $"{operationLabel}: {expected.RequestSchema}.{propertyName} debe tener descripción XML");
            }
        }

        operation.TryGetProperty("responses", out var responses).Should().BeTrue(
            $"{operationLabel}: debe publicar responses");
        responses.TryGetProperty("200", out _).Should().BeTrue(
            $"{operationLabel}: debe publicar su response principal 200");

        foreach (var (statusCode, description) in expected.Responses)
        {
            responses.TryGetProperty(statusCode, out var response).Should().BeTrue(
                $"{operationLabel}: debe publicar el status {statusCode}");
            response.GetProperty("description").GetString().Should().Be(description,
                $"{operationLabel}: el status {statusCode} debe conservar su descripción explícita");

            var responseMediaType = GetJsonMediaType(
                response.GetProperty("content"),
                operationLabel,
                $"response {statusCode}");
            GetSchemaReferenceName(responseMediaType.GetProperty("schema")).Should().Be("BaseResponseModel",
                $"{operationLabel}: el status {statusCode} usa el envelope real BaseResponseModel");
        }
    }

    private enum Qg2ResponseShape
    {
        Schema,
        Collection,
        Paginated,
        ProblemDetails,
        ValidationProblemDetails,
        AchResponsePaged,
        IncomingNachaPage,
        SchedulerPaged,
        NoBody,
        Untyped
    }

    private sealed record Qg2Parameter(string Name, string Location, bool? Required = null);

    private sealed record Qg2Response(
        string StatusCode,
        string Description,
        Qg2ResponseShape Shape,
        string? SchemaName = null);

    private sealed record Qg2SchemaDocumentation(string SchemaName, string[] Properties);

    private sealed record ChambersCyclesQg2Operation(
        string Method,
        string Path,
        string PrimaryStatusCode,
        Qg2Parameter[] Parameters,
        string? RequestSchema,
        string[] RequestProperties,
        Qg2Response[] Responses,
        Qg2SchemaDocumentation? ResponseSchemaDocumentation = null);

    private static void AssertChambersCyclesQG2(
        JsonElement paths,
        JsonElement schemas,
        ChambersCyclesQg2Operation expected)
    {
        var operationLabel = $"{expected.Method} {expected.Path}";
        paths.TryGetProperty(expected.Path, out var pathItem).Should().BeTrue(
            $"{operationLabel}: la operación incluida en QG-2 debe existir en OpenAPI");
        pathItem.TryGetProperty(expected.Method.ToLowerInvariant(), out var operation).Should().BeTrue(
            $"{operationLabel}: el método incluido en QG-2 debe existir en OpenAPI");

        GetOperationTags(operation).Should().Equal("Cámaras y ciclos",
            $"{operationLabel}: debe conservar el tag funcional aprobado para cámaras y ciclos");
        HasBearerSecurity(operation).Should().BeTrue(
            $"{operationLabel}: las operaciones de cámaras y ciclos incluidas en QG-2 deben conservar el requisito Bearer publicado");

        AssertHeaderAbsent(operation, "Authorization");
        AssertHeaderAbsent(operation, "Idempotency-Key");
        AssertHeaderAbsent(operation, "X-Correlation-ID");

        foreach (var parameter in expected.Parameters)
        {
            AssertDocumentedOperationParameter(operation, parameter, operationLabel);
        }

        if (expected.RequestSchema is null)
        {
            operation.TryGetProperty("requestBody", out _).Should().BeFalse(
                $"{operationLabel}: no debe publicar request body");
        }
        else
        {
            operation.TryGetProperty("requestBody", out var requestBody).Should().BeTrue(
                $"{operationLabel}: debe publicar request body");
            var requestMediaType = GetJsonMediaType(requestBody.GetProperty("content"), operationLabel, "request");
            GetSchemaReferenceName(requestMediaType.GetProperty("schema")).Should().Be(expected.RequestSchema,
                $"{operationLabel}: el request debe usar el contrato {expected.RequestSchema}");

            var requestSchema = schemas.GetProperty(expected.RequestSchema);
            foreach (var propertyName in expected.RequestProperties)
            {
                TryGetSchemaProperty(requestSchema, schemas, propertyName, out var property).Should().BeTrue(
                    $"{operationLabel}: el schema {expected.RequestSchema} debe publicar la propiedad {propertyName}");
                property.TryGetProperty("description", out var propertyDescription).Should().BeTrue(
                    $"{operationLabel}: {expected.RequestSchema}.{propertyName} debe exponer documentación XML");
                propertyDescription.GetString().Should().NotBeNullOrWhiteSpace(
                    $"{operationLabel}: {expected.RequestSchema}.{propertyName} debe tener descripción XML");
            }
        }

        operation.TryGetProperty("responses", out var responses).Should().BeTrue(
            $"{operationLabel}: debe publicar responses");
        responses.TryGetProperty(expected.PrimaryStatusCode, out _).Should().BeTrue(
            $"{operationLabel}: debe publicar su response principal {expected.PrimaryStatusCode}");

        foreach (var responseExpectation in expected.Responses)
        {
            responses.TryGetProperty(responseExpectation.StatusCode, out var response).Should().BeTrue(
                $"{operationLabel}: debe publicar el status {responseExpectation.StatusCode}");
            response.GetProperty("description").GetString().Should().Be(responseExpectation.Description,
                $"{operationLabel}: el status {responseExpectation.StatusCode} debe conservar su descripción explícita");

            AssertResponseContract(response, schemas, responseExpectation, operationLabel);
        }

        if (expected.ResponseSchemaDocumentation is not null)
        {
            AssertSchemaPropertiesDocumented(
                schemas,
                expected.ResponseSchemaDocumentation,
                operationLabel);
        }
    }

    private sealed record TransactionsQg2Operation(
        string Method,
        string Path,
        string PrimaryStatusCode,
        Qg2Parameter[] Parameters,
        string? RequestSchema,
        string[] RequestProperties,
        Qg2Response[] Responses,
        Qg2SchemaDocumentation? ResponseSchemaDocumentation = null);

    private static void AssertTransactionsQG2(
        JsonElement paths,
        JsonElement schemas,
        TransactionsQg2Operation expected)
    {
        var operationLabel = $"{expected.Method} {expected.Path}";
        paths.TryGetProperty(expected.Path, out var pathItem).Should().BeTrue(
            $"{operationLabel}: la operación incluida en QG-2 debe existir en OpenAPI");
        pathItem.TryGetProperty(expected.Method.ToLowerInvariant(), out var operation).Should().BeTrue(
            $"{operationLabel}: el método incluido en QG-2 debe existir en OpenAPI");

        GetOperationTags(operation).Should().Equal("Transacciones interbancarias",
            $"{operationLabel}: debe conservar el tag funcional aprobado para transacciones");
        HasBearerSecurity(operation).Should().BeTrue(
            $"{operationLabel}: las operaciones de Transactions incluidas en QG-2 deben conservar el requisito Bearer publicado");

        AssertHeaderAbsent(operation, "Authorization");
        AssertHeaderAbsent(operation, "Idempotency-Key");
        AssertHeaderAbsent(operation, "X-Correlation-ID");

        foreach (var parameter in expected.Parameters)
        {
            AssertDocumentedOperationParameter(operation, parameter, operationLabel);
        }

        if (expected.RequestSchema is null)
        {
            operation.TryGetProperty("requestBody", out _).Should().BeFalse(
                $"{operationLabel}: no debe publicar request body");
        }
        else
        {
            operation.TryGetProperty("requestBody", out var requestBody).Should().BeTrue(
                $"{operationLabel}: debe publicar request body");
            var requestMediaType = GetJsonMediaType(requestBody.GetProperty("content"), operationLabel, "request");
            GetSchemaReferenceName(requestMediaType.GetProperty("schema")).Should().Be(expected.RequestSchema,
                $"{operationLabel}: el request debe usar el contrato {expected.RequestSchema}");

            var requestSchema = schemas.GetProperty(expected.RequestSchema);
            foreach (var propertyName in expected.RequestProperties)
            {
                TryGetSchemaProperty(requestSchema, schemas, propertyName, out var property).Should().BeTrue(
                    $"{operationLabel}: el schema {expected.RequestSchema} debe publicar la propiedad {propertyName}");
                property.TryGetProperty("description", out var propertyDescription).Should().BeTrue(
                    $"{operationLabel}: {expected.RequestSchema}.{propertyName} debe exponer documentación XML");
                propertyDescription.GetString().Should().NotBeNullOrWhiteSpace(
                    $"{operationLabel}: {expected.RequestSchema}.{propertyName} debe tener descripción XML");
            }
        }

        operation.TryGetProperty("responses", out var responses).Should().BeTrue(
            $"{operationLabel}: debe publicar responses");
        responses.TryGetProperty(expected.PrimaryStatusCode, out _).Should().BeTrue(
            $"{operationLabel}: debe publicar su response principal {expected.PrimaryStatusCode}");

        foreach (var responseExpectation in expected.Responses)
        {
            responses.TryGetProperty(responseExpectation.StatusCode, out var response).Should().BeTrue(
                $"{operationLabel}: debe publicar el status {responseExpectation.StatusCode}");
            response.GetProperty("description").GetString().Should().Be(responseExpectation.Description,
                $"{operationLabel}: el status {responseExpectation.StatusCode} debe conservar su descripción explícita");

            AssertResponseContract(response, schemas, responseExpectation, operationLabel);
        }

        if (expected.ResponseSchemaDocumentation is not null)
        {
            AssertSchemaPropertiesDocumented(schemas, expected.ResponseSchemaDocumentation, operationLabel);
        }
    }

    private sealed record BulkIngestionQg2Operation(
        string Method,
        string Path,
        string PrimaryStatusCode,
        Qg2Parameter[] Parameters,
        string? RequestSchema,
        string? RequestContentType,
        string[] RequestProperties,
        string[] RequiredRequestProperties,
        Qg2Response[] Responses,
        Qg2SchemaDocumentation? ResponseSchemaDocumentation = null);

    private static void AssertBulkIngestionQG2(
        JsonElement paths,
        JsonElement schemas,
        BulkIngestionQg2Operation expected)
    {
        var operationLabel = $"{expected.Method} {expected.Path}";
        paths.TryGetProperty(expected.Path, out var pathItem).Should().BeTrue(
            $"{operationLabel}: la operación incluida en QG-2 debe existir en OpenAPI");
        pathItem.TryGetProperty(expected.Method.ToLowerInvariant(), out var operation).Should().BeTrue(
            $"{operationLabel}: el método incluido en QG-2 debe existir en OpenAPI");

        GetOperationTags(operation).Should().Equal("Transacciones interbancarias",
            $"{operationLabel}: debe conservar el tag funcional aprobado para ingestión masiva");
        HasBearerSecurity(operation).Should().BeTrue(
            $"{operationLabel}: las operaciones de BulkIngestion incluidas en QG-2 deben conservar el requisito Bearer publicado");

        AssertHeaderAbsent(operation, "Authorization");
        AssertHeaderAbsent(operation, "Idempotency-Key");
        AssertHeaderAbsent(operation, "X-Correlation-ID");

        foreach (var parameter in expected.Parameters)
        {
            AssertDocumentedOperationParameter(operation, parameter, operationLabel);
        }

        if (expected.RequestSchema is null)
        {
            operation.TryGetProperty("requestBody", out _).Should().BeFalse(
                $"{operationLabel}: no debe publicar request body");
        }
        else
        {
            operation.TryGetProperty("requestBody", out var requestBody).Should().BeTrue(
                $"{operationLabel}: debe publicar request body");
            requestBody.TryGetProperty("content", out var requestContent).Should().BeTrue(
                $"{operationLabel}: el request debe publicar content types");
            expected.RequestContentType.Should().NotBeNullOrWhiteSpace();
            requestContent.TryGetProperty(expected.RequestContentType!, out var requestMediaType).Should().BeTrue(
                $"{operationLabel}: el request debe publicar {expected.RequestContentType}");
            requestMediaType.TryGetProperty("schema", out var requestSchema).Should().BeTrue(
                $"{operationLabel}: el request debe publicar un schema");

            var referenceName = GetSchemaReferenceName(requestSchema);
            if (!string.IsNullOrWhiteSpace(referenceName))
            {
                referenceName.Should().Be(expected.RequestSchema,
                    $"{operationLabel}: el request debe usar el contrato {expected.RequestSchema}");
            }

            foreach (var propertyName in expected.RequestProperties)
            {
                TryGetSchemaPropertyIgnoreCase(requestSchema, schemas, propertyName, out var property).Should().BeTrue(
                    $"{operationLabel}: el schema del request debe publicar la propiedad {propertyName}");
                property.TryGetProperty("description", out var propertyDescription).Should().BeTrue(
                    $"{operationLabel}: {expected.RequestSchema}.{propertyName} debe exponer documentación XML");
                propertyDescription.GetString().Should().NotBeNullOrWhiteSpace(
                    $"{operationLabel}: {expected.RequestSchema}.{propertyName} debe tener descripción XML");
            }

            if (expected.RequiredRequestProperties.Length > 0)
            {
                var resolvedRequestSchema = ResolveSchema(requestSchema, schemas);
                resolvedRequestSchema.TryGetProperty("required", out var required).Should().BeTrue(
                    $"{operationLabel}: el schema debe identificar sus propiedades obligatorias");
                required.ValueKind.Should().Be(JsonValueKind.Array);
                var requiredNames = required.EnumerateArray()
                    .Where(item => item.ValueKind == JsonValueKind.String)
                    .Select(item => item.GetString())
                    .Where(item => !string.IsNullOrWhiteSpace(item))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                foreach (var propertyName in expected.RequiredRequestProperties)
                {
                    requiredNames.Should().Contain(propertyName,
                        $"{operationLabel}: {propertyName} debe conservarse como propiedad obligatoria del request");
                }
            }
        }

        operation.TryGetProperty("responses", out var responses).Should().BeTrue(
            $"{operationLabel}: debe publicar responses");
        responses.TryGetProperty(expected.PrimaryStatusCode, out _).Should().BeTrue(
            $"{operationLabel}: debe publicar su response principal {expected.PrimaryStatusCode}");

        foreach (var responseExpectation in expected.Responses)
        {
            responses.TryGetProperty(responseExpectation.StatusCode, out var response).Should().BeTrue(
                $"{operationLabel}: debe publicar el status {responseExpectation.StatusCode}");
            response.GetProperty("description").GetString().Should().Be(responseExpectation.Description,
                $"{operationLabel}: el status {responseExpectation.StatusCode} debe conservar su descripción explícita");

            AssertResponseContract(response, schemas, responseExpectation, operationLabel);
        }

        if (expected.ResponseSchemaDocumentation is not null)
        {
            AssertSchemaPropertiesDocumented(schemas, expected.ResponseSchemaDocumentation, operationLabel);
        }
    }

    private sealed record TraceabilityQg2Operation(
        string Method,
        string Path,
        string PrimaryStatusCode,
        Qg2Parameter[] Parameters,
        string? RequestSchema,
        string[] RequestProperties,
        Qg2Response[] Responses,
        Qg2SchemaDocumentation? ResponseSchemaDocumentation = null);

    private static void AssertTraceabilityQG2(
        JsonElement paths,
        JsonElement schemas,
        TraceabilityQg2Operation expected)
    {
        var operationLabel = $"{expected.Method} {expected.Path}";
        paths.TryGetProperty(expected.Path, out var pathItem).Should().BeTrue(
            $"{operationLabel}: la operación incluida en QG-2 debe existir en OpenAPI");
        pathItem.TryGetProperty(expected.Method.ToLowerInvariant(), out var operation).Should().BeTrue(
            $"{operationLabel}: el método incluido en QG-2 debe existir en OpenAPI");

        GetOperationTags(operation).Should().Equal("Trazabilidad y conciliación",
            $"{operationLabel}: debe conservar el tag funcional aprobado para trazabilidad");
        HasBearerSecurity(operation).Should().BeTrue(
            $"{operationLabel}: las operaciones de trazabilidad incluidas en QG-2 deben conservar el requisito Bearer publicado");

        AssertHeaderAbsent(operation, "Authorization");
        AssertHeaderAbsent(operation, "Idempotency-Key");
        AssertHeaderAbsent(operation, "X-Correlation-ID");

        foreach (var parameter in expected.Parameters)
        {
            AssertDocumentedOperationParameter(operation, parameter, operationLabel);
        }

        if (expected.RequestSchema is null)
        {
            operation.TryGetProperty("requestBody", out _).Should().BeFalse(
                $"{operationLabel}: no debe publicar request body");
        }
        else
        {
            operation.TryGetProperty("requestBody", out var requestBody).Should().BeTrue(
                $"{operationLabel}: debe publicar request body");
            var requestMediaType = GetJsonMediaType(requestBody.GetProperty("content"), operationLabel, "request");
            GetSchemaReferenceName(requestMediaType.GetProperty("schema")).Should().Be(expected.RequestSchema,
                $"{operationLabel}: el request debe usar el contrato {expected.RequestSchema}");

            var requestSchema = schemas.GetProperty(expected.RequestSchema);
            foreach (var propertyName in expected.RequestProperties)
            {
                TryGetSchemaProperty(requestSchema, schemas, propertyName, out var property).Should().BeTrue(
                    $"{operationLabel}: el schema {expected.RequestSchema} debe publicar la propiedad {propertyName}");
                property.TryGetProperty("description", out var propertyDescription).Should().BeTrue(
                    $"{operationLabel}: {expected.RequestSchema}.{propertyName} debe exponer documentación XML");
                propertyDescription.GetString().Should().NotBeNullOrWhiteSpace(
                    $"{operationLabel}: {expected.RequestSchema}.{propertyName} debe tener descripción XML");
            }
        }

        operation.TryGetProperty("responses", out var responses).Should().BeTrue(
            $"{operationLabel}: debe publicar responses");
        responses.TryGetProperty(expected.PrimaryStatusCode, out _).Should().BeTrue(
            $"{operationLabel}: debe publicar su response principal {expected.PrimaryStatusCode}");

        foreach (var responseExpectation in expected.Responses)
        {
            responses.TryGetProperty(responseExpectation.StatusCode, out var response).Should().BeTrue(
                $"{operationLabel}: debe publicar el status {responseExpectation.StatusCode}");
            response.GetProperty("description").GetString().Should().Be(responseExpectation.Description,
                $"{operationLabel}: el status {responseExpectation.StatusCode} debe conservar su descripción explícita");

            AssertResponseContract(response, schemas, responseExpectation, operationLabel);
        }

        if (expected.ResponseSchemaDocumentation is not null)
        {
            AssertSchemaPropertiesDocumented(schemas, expected.ResponseSchemaDocumentation, operationLabel);
        }
    }

    private sealed record ReturnsQg2Operation(
        string Method,
        string Path,
        string PrimaryStatusCode,
        Qg2Parameter[] Parameters,
        string? RequestSchema,
        string[] RequestProperties,
        Qg2Response[] Responses,
        Qg2SchemaDocumentation? ResponseSchemaDocumentation = null);

    private static void AssertReturnsQG2(
        JsonElement paths,
        JsonElement schemas,
        ReturnsQg2Operation expected)
    {
        var operationLabel = $"{expected.Method} {expected.Path}";
        paths.TryGetProperty(expected.Path, out var pathItem).Should().BeTrue(
            $"{operationLabel}: la operación incluida en QG-2 debe existir en OpenAPI");
        pathItem.TryGetProperty(expected.Method.ToLowerInvariant(), out var operation).Should().BeTrue(
            $"{operationLabel}: el método incluido en QG-2 debe existir en OpenAPI");

        GetOperationTags(operation).Should().Equal("Devoluciones y respuestas",
            $"{operationLabel}: debe conservar el tag funcional aprobado para devoluciones");
        HasBearerSecurity(operation).Should().BeTrue(
            $"{operationLabel}: las operaciones de devoluciones y respuestas incluidas en QG-2 deben conservar el requisito Bearer publicado");

        AssertHeaderAbsent(operation, "Authorization");
        AssertHeaderAbsent(operation, "Idempotency-Key");
        AssertHeaderAbsent(operation, "X-Correlation-ID");

        foreach (var parameter in expected.Parameters)
        {
            AssertDocumentedOperationParameter(operation, parameter, operationLabel);
        }

        if (expected.RequestSchema is null)
        {
            operation.TryGetProperty("requestBody", out _).Should().BeFalse(
                $"{operationLabel}: no debe publicar request body");
        }
        else
        {
            operation.TryGetProperty("requestBody", out var requestBody).Should().BeTrue(
                $"{operationLabel}: debe publicar request body");
            var requestMediaType = GetJsonMediaType(requestBody.GetProperty("content"), operationLabel, "request");
            GetSchemaReferenceName(requestMediaType.GetProperty("schema")).Should().Be(expected.RequestSchema,
                $"{operationLabel}: el request debe usar el contrato {expected.RequestSchema}");

            var requestSchema = schemas.GetProperty(expected.RequestSchema);
            foreach (var propertyName in expected.RequestProperties)
            {
                TryGetSchemaProperty(requestSchema, schemas, propertyName, out var property).Should().BeTrue(
                    $"{operationLabel}: el schema {expected.RequestSchema} debe publicar la propiedad {propertyName}");
                property.TryGetProperty("description", out var propertyDescription).Should().BeTrue(
                    $"{operationLabel}: {expected.RequestSchema}.{propertyName} debe exponer documentación XML");
                propertyDescription.GetString().Should().NotBeNullOrWhiteSpace(
                    $"{operationLabel}: {expected.RequestSchema}.{propertyName} debe tener descripción XML");
            }
        }

        operation.TryGetProperty("responses", out var responses).Should().BeTrue(
            $"{operationLabel}: debe publicar responses");
        responses.TryGetProperty(expected.PrimaryStatusCode, out _).Should().BeTrue(
            $"{operationLabel}: debe publicar su response principal {expected.PrimaryStatusCode}");

        foreach (var responseExpectation in expected.Responses)
        {
            responses.TryGetProperty(responseExpectation.StatusCode, out var response).Should().BeTrue(
                $"{operationLabel}: debe publicar el status {responseExpectation.StatusCode}");
            response.GetProperty("description").GetString().Should().Be(responseExpectation.Description,
                $"{operationLabel}: el status {responseExpectation.StatusCode} debe conservar su descripción explícita");

            AssertResponseContract(response, schemas, responseExpectation, operationLabel);
        }

        if (expected.ResponseSchemaDocumentation is not null)
        {
            AssertSchemaPropertiesDocumented(schemas, expected.ResponseSchemaDocumentation, operationLabel);
        }
    }

    private sealed record ResponseOperationsQg2Operation(
        string Method,
        string Path,
        string PrimaryStatusCode,
        Qg2Parameter[] Parameters,
        string? RequestSchema,
        string[] RequestProperties,
        bool HasCorrelationHeader,
        Qg2Response[] Responses,
        Qg2SchemaDocumentation? ResponseSchemaDocumentation = null);

    private static void AssertResponseOperationsQG2(
        JsonElement paths,
        JsonElement schemas,
        ResponseOperationsQg2Operation expected)
    {
        var operationLabel = $"{expected.Method} {expected.Path}";
        paths.TryGetProperty(expected.Path, out var pathItem).Should().BeTrue(
            $"{operationLabel}: la operación SC-304 incluida en QG-2 debe existir en OpenAPI");
        pathItem.TryGetProperty(expected.Method.ToLowerInvariant(), out var operation).Should().BeTrue(
            $"{operationLabel}: el método SC-304 incluido en QG-2 debe existir en OpenAPI");

        GetOperationTags(operation).Should().Equal("Devoluciones y respuestas",
            $"{operationLabel}: debe conservar el tag funcional aprobado para operaciones de respuesta ACH");
        HasBearerSecurity(operation).Should().BeTrue(
            $"{operationLabel}: las operaciones SC-304 deben conservar el requisito Bearer publicado");

        AssertHeaderAbsent(operation, "Authorization");
        AssertHeaderAbsent(operation, "Idempotency-Key");
        if (expected.HasCorrelationHeader)
        {
            AssertHeaderParameter(operation, "X-Correlation-ID");
        }
        else
        {
            AssertHeaderAbsent(operation, "X-Correlation-ID");
        }

        foreach (var parameter in expected.Parameters)
        {
            AssertDocumentedOperationParameter(operation, parameter, operationLabel);
        }

        if (expected.RequestSchema is null)
        {
            operation.TryGetProperty("requestBody", out _).Should().BeFalse(
                $"{operationLabel}: no debe publicar request body");
        }
        else
        {
            operation.TryGetProperty("requestBody", out var requestBody).Should().BeTrue(
                $"{operationLabel}: debe publicar request body");
            var requestMediaType = GetJsonMediaType(requestBody.GetProperty("content"), operationLabel, "request");
            GetSchemaReferenceName(requestMediaType.GetProperty("schema")).Should().Be(expected.RequestSchema,
                $"{operationLabel}: el request debe usar el contrato {expected.RequestSchema}");

            AssertSchemaPropertiesDocumented(
                schemas,
                new Qg2SchemaDocumentation(expected.RequestSchema, expected.RequestProperties),
                operationLabel);
        }

        operation.TryGetProperty("responses", out var responses).Should().BeTrue(
            $"{operationLabel}: debe publicar responses");
        responses.TryGetProperty(expected.PrimaryStatusCode, out _).Should().BeTrue(
            $"{operationLabel}: debe publicar su response principal {expected.PrimaryStatusCode}");

        var expectedStatusCodes = expected.Responses
            .Select(response => response.StatusCode)
            .ToHashSet(StringComparer.Ordinal);
        var actualStatusCodes = responses.EnumerateObject()
            .Select(response => response.Name)
            .ToHashSet(StringComparer.Ordinal);
        actualStatusCodes.Should().BeEquivalentTo(expectedStatusCodes,
            $"{operationLabel}: SC-304 no debe publicar status codes especulativos ni omitir status alcanzables documentados");

        foreach (var responseExpectation in expected.Responses)
        {
            responses.TryGetProperty(responseExpectation.StatusCode, out var response).Should().BeTrue(
                $"{operationLabel}: debe publicar el status {responseExpectation.StatusCode}");
            response.GetProperty("description").GetString().Should().Be(responseExpectation.Description,
                $"{operationLabel}: el status {responseExpectation.StatusCode} debe conservar su descripción explícita");

            AssertResponseContract(response, schemas, responseExpectation, operationLabel);
        }

        if (expected.ResponseSchemaDocumentation is not null)
        {
            AssertSchemaPropertiesDocumented(schemas, expected.ResponseSchemaDocumentation, operationLabel);
        }
    }

    private sealed record CenitOperationsQg2Operation(
        string Method,
        string Path,
        string ActionName,
        string Policy,
        string Summary,
        string Description,
        string PrimaryStatusCode,
        Qg2Parameter[] Parameters,
        string? RequestSchema,
        string[] RequestProperties,
        Qg2Response[] Responses,
        Qg2SchemaDocumentation? ResponseSchemaDocumentation = null);

    private static void AssertCenitOperationsQG2(
        JsonElement paths,
        JsonElement schemas,
        CenitOperationsQg2Operation expected)
    {
        var operationLabel = $"{expected.Method} {expected.Path}";
        paths.TryGetProperty(expected.Path, out var pathItem).Should().BeTrue(
            $"{operationLabel}: la operación SC-305 incluida en QG-2 debe existir en OpenAPI");
        pathItem.TryGetProperty(expected.Method.ToLowerInvariant(), out var operation).Should().BeTrue(
            $"{operationLabel}: el método SC-305 incluido en QG-2 debe existir en OpenAPI");

        operation.GetProperty("summary").GetString().Should().Be(expected.Summary,
            $"{operationLabel}: debe conservar el Summary contractual de SC-305");
        operation.GetProperty("description").GetString().Should().Be(expected.Description,
            $"{operationLabel}: debe conservar la Description contractual de SC-305");
        GetOperationTags(operation).Should().Equal("CENIT",
            $"{operationLabel}: debe conservar el tag funcional CENIT");
        HasBearerSecurity(operation).Should().BeTrue(
            $"{operationLabel}: las operaciones SC-305 deben conservar el requisito Bearer publicado");

        var controllerMethod = typeof(CenitOperationsController).GetMethod(expected.ActionName);
        controllerMethod.Should().NotBeNull($"{operationLabel}: debe corresponder a la acción {expected.ActionName}");
        var actionPolicy = controllerMethod!.GetCustomAttributes(inherit: true)
            .OfType<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>()
            .Select(attribute => attribute.Policy)
            .SingleOrDefault(policy => !string.IsNullOrWhiteSpace(policy));
        actionPolicy.Should().Be(expected.Policy,
            $"{operationLabel}: debe conservar la policy autorizada del controller");

        AssertHeaderAbsent(operation, "Authorization");
        AssertHeaderAbsent(operation, "Idempotency-Key");
        AssertHeaderAbsent(operation, "X-Correlation-ID");

        foreach (var parameter in expected.Parameters)
        {
            AssertDocumentedOperationParameter(operation, parameter, operationLabel);
        }

        var expectedContractParameters = expected.Parameters
            .Select(parameter => $"{parameter.Location}:{parameter.Name}")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var actualContractParameters = operation.TryGetProperty("parameters", out var parameters)
            ? parameters.EnumerateArray()
                .Where(parameter => parameter.ValueKind == JsonValueKind.Object
                    && parameter.TryGetProperty("name", out var name)
                    && name.ValueKind == JsonValueKind.String
                    && parameter.TryGetProperty("in", out var location)
                    && location.ValueKind == JsonValueKind.String
                    && (string.Equals(location.GetString(), "path", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(location.GetString(), "query", StringComparison.OrdinalIgnoreCase)))
                .Select(parameter =>
                {
                    var location = parameter.GetProperty("in").GetString();
                    var name = parameter.GetProperty("name").GetString();
                    return $"{location}:{name}";
                })
                .ToHashSet(StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        actualContractParameters.Should().BeEquivalentTo(expectedContractParameters,
            $"{operationLabel}: SC-305 debe publicar exactamente sus parámetros path/query reales");

        if (expected.RequestSchema is null)
        {
            operation.TryGetProperty("requestBody", out _).Should().BeFalse(
                $"{operationLabel}: no debe publicar request body");
        }
        else
        {
            operation.TryGetProperty("requestBody", out var requestBody).Should().BeTrue(
                $"{operationLabel}: debe publicar request body");
            var requestMediaType = GetJsonMediaType(requestBody.GetProperty("content"), operationLabel, "request");
            GetSchemaReferenceName(requestMediaType.GetProperty("schema")).Should().Be(expected.RequestSchema,
                $"{operationLabel}: el request debe usar el contrato {expected.RequestSchema}");

            AssertSchemaPropertiesDocumented(
                schemas,
                new Qg2SchemaDocumentation(expected.RequestSchema, expected.RequestProperties),
                operationLabel);
        }

        operation.TryGetProperty("responses", out var responses).Should().BeTrue(
            $"{operationLabel}: debe publicar responses");
        responses.TryGetProperty(expected.PrimaryStatusCode, out _).Should().BeTrue(
            $"{operationLabel}: debe publicar su response principal {expected.PrimaryStatusCode}");

        var expectedStatusCodes = expected.Responses
            .Select(response => response.StatusCode)
            .ToHashSet(StringComparer.Ordinal);
        var actualStatusCodes = responses.EnumerateObject()
            .Select(response => response.Name)
            .ToHashSet(StringComparer.Ordinal);
        actualStatusCodes.Should().BeEquivalentTo(expectedStatusCodes,
            $"{operationLabel}: SC-305 no debe publicar status codes especulativos ni omitir status alcanzables documentados");

        foreach (var responseExpectation in expected.Responses)
        {
            responses.TryGetProperty(responseExpectation.StatusCode, out var response).Should().BeTrue(
                $"{operationLabel}: debe publicar el status {responseExpectation.StatusCode}");
            response.GetProperty("description").GetString().Should().Be(responseExpectation.Description,
                $"{operationLabel}: el status {responseExpectation.StatusCode} debe conservar su descripción explícita");

            AssertResponseContract(response, schemas, responseExpectation, operationLabel);
        }

        if (expected.ResponseSchemaDocumentation is not null)
        {
            AssertSchemaPropertiesDocumented(schemas, expected.ResponseSchemaDocumentation, operationLabel);
        }
    }

    private sealed record CertificateQg2Operation(
        string Method,
        string Path,
        Type ControllerType,
        string ActionName,
        string Policy,
        string Summary,
        string Description,
        Qg2Parameter[] Parameters,
        string? RequestSchema,
        string[] RequestProperties,
        string? RequestContentType,
        Qg2Response[] Responses,
        Qg2SchemaDocumentation? ResponseSchemaDocumentation = null);

    private enum NachaSecurityRequestShape
    {
        None,
        JsonSchema,
        MultipartFile
    }

    private sealed record NachaSecurityQg2Operation(
        string Method,
        string Path,
        string ActionName,
        string Policy,
        string Summary,
        string Description,
        Qg2Parameter[] Parameters,
        NachaSecurityRequestShape RequestShape,
        string? RequestSchema,
        string[] RequestProperties,
        Qg2Response[] Responses);

    private enum Sc309RequestShape
    {
        None,
        MultipartSchema
    }

    private sealed record Sc309Qg2Operation(
        string Method,
        string Path,
        Type ControllerType,
        string ActionName,
        string Policy,
        string Summary,
        string Description,
        Qg2Parameter[] Parameters,
        Sc309RequestShape RequestShape,
        string? RequestSchema,
        string[] RequestProperties,
        Qg2Response[] Responses,
        string[] SuccessContentTypes,
        string[] SuccessDescriptionFragments,
        Qg2SchemaDocumentation? ResponseSchemaDocumentation = null);

    private enum Sc401RequestShape
    {
        None,
        JsonSchema
    }

    private sealed record Sc401Qg2Operation(
        string Method,
        string Path,
        string ActionName,
        string Policy,
        string Summary,
        string Description,
        Qg2Parameter[] Parameters,
        bool HasIdempotencyKey,
        Sc401RequestShape RequestShape,
        string[] RequestProperties,
        Qg2Response[] Responses,
        string[] SuccessContentTypes,
        string[] SuccessDescriptionFragments,
        Qg2SchemaDocumentation? ResponseSchemaDocumentation = null);

    private sealed record Sc402Qg2Operation(
        string Method,
        string Path,
        string ActionName,
        string Policy,
        string Summary,
        string Description,
        Qg2Parameter[] Parameters,
        string? RequestSchema,
        string[] RequestProperties,
        Qg2Response[] Responses,
        Qg2SchemaDocumentation? ResponseSchemaDocumentation = null);

    private sealed record Sc403Qg2Operation(
        string Method,
        string Path,
        Type ControllerType,
        string ActionName,
        string Policy,
        string Summary,
        string Description,
        Qg2Parameter[] Parameters,
        string? RequestSchema,
        string[] RequestProperties,
        Qg2Response[] Responses,
        Qg2SchemaDocumentation? ResponseSchemaDocumentation = null);

    private sealed record Sc404Qg2Operation(
        string Method,
        string Path,
        string ActionName,
        string Policy,
        string Summary,
        string Description,
        Qg2Parameter[] Parameters,
        bool HasCorrelationId,
        string? RequestSchema,
        string[] RequestProperties,
        Qg2Response[] Responses,
        Qg2SchemaDocumentation? ResponseSchemaDocumentation = null);

    private sealed record Sc405Qg2Operation(
        string Method,
        string Path,
        string ActionName,
        string? ActionRouteTemplate,
        string Policy,
        string Summary,
        string Description,
        Qg2Parameter[] Parameters,
        string? RequestSchema,
        string[] RequestProperties,
        Qg2Response[] Responses,
        Qg2SchemaDocumentation? ResponseSchemaDocumentation = null);

    private sealed record Sc406Qg2Operation(
        string Method,
        string Path,
        string ActionName,
        string ActionRouteTemplate,
        string Summary,
        string Description,
        Qg2Parameter[] Parameters,
        Qg2Response[] Responses,
        string? PdfFileNameFragment = null);

    private sealed record Sc407Qg2Operation(
        string Method,
        string Path,
        string ActionName,
        string ActionRouteTemplate,
        string Summary,
        string Description,
        Qg2Parameter[] Parameters,
        string? RequestSchema,
        string[] RequestProperties,
        Qg2Response[] Responses,
        string[]? SuccessMediaTypes = null,
        string? FileNameFragment = null);

    private sealed record Sc408Qg2Operation(
        string Method,
        string Path,
        Type ControllerType,
        string ActionName,
        string ActionRouteTemplate,
        string Policy,
        string Tag,
        string Summary,
        string Description,
        Qg2Parameter[] Parameters,
        string? RequestSchema,
        string[] RequestProperties,
        Qg2Response[] Responses,
        Qg2SchemaDocumentation? ResponseSchemaDocumentation = null);

    private sealed record Sc409Qg2Operation(
        string Method,
        string Path,
        Type ControllerType,
        string ActionName,
        string? ActionRouteTemplate,
        string Policy,
        string Tag,
        string Summary,
        string Description,
        Qg2Parameter[] Parameters,
        Qg2Response[] Responses,
        Qg2SchemaDocumentation? ResponseSchemaDocumentation = null);

    private sealed record Sc410ControllerQg2Operation(
        string Method,
        string Path,
        Type ControllerType,
        string ActionName,
        string? ActionRouteTemplate,
        string? NominalPolicy,
        string Summary,
        string Description,
        Qg2Parameter[] Parameters,
        string? RequestSchema,
        string[] RequestProperties,
        Qg2Response[] Responses);

    private sealed record Sc410HealthQg2Operation(
        string Method,
        string Path,
        string Summary,
        string Description,
        string[] StatusCodes);

    private static void AssertCertificateQG2(
        JsonElement paths,
        JsonElement schemas,
        CertificateQg2Operation expected)
    {
        var operationLabel = $"{expected.Method} {expected.Path}";
        paths.TryGetProperty(expected.Path, out var pathItem).Should().BeTrue(
            $"{operationLabel}: la operación SC-306 incluida en QG-2 debe existir en OpenAPI");
        pathItem.TryGetProperty(expected.Method.ToLowerInvariant(), out var operation).Should().BeTrue(
            $"{operationLabel}: el método SC-306 incluido en QG-2 debe existir en OpenAPI");

        operation.GetProperty("summary").GetString().Should().Be(expected.Summary,
            $"{operationLabel}: debe conservar el Summary contractual de SC-306");
        operation.GetProperty("description").GetString().Should().Be(expected.Description,
            $"{operationLabel}: debe conservar la Description contractual de SC-306");
        GetOperationTags(operation).Should().Equal("Seguridad y certificados",
            $"{operationLabel}: debe conservar el tag funcional de seguridad y certificados");
        HasBearerSecurity(operation).Should().BeTrue(
            $"{operationLabel}: las operaciones SC-306 deben conservar el requisito Bearer publicado");

        var controllerMethod = expected.ControllerType.GetMethod(expected.ActionName);
        controllerMethod.Should().NotBeNull($"{operationLabel}: debe corresponder a la acción {expected.ActionName}");
        var actionPolicy = controllerMethod!.GetCustomAttributes(inherit: true)
            .OfType<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>()
            .Select(attribute => attribute.Policy)
            .SingleOrDefault(policy => !string.IsNullOrWhiteSpace(policy));
        actionPolicy.Should().Be(expected.Policy,
            $"{operationLabel}: debe conservar la policy real de la acción");

        AssertHeaderAbsent(operation, "Authorization");
        AssertHeaderAbsent(operation, "Idempotency-Key");
        AssertHeaderAbsent(operation, "X-Correlation-ID");

        foreach (var parameter in expected.Parameters)
        {
            AssertDocumentedOperationParameter(operation, parameter, operationLabel);
        }

        var expectedContractParameters = expected.Parameters
            .Select(parameter => $"{parameter.Location}:{parameter.Name}")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var actualContractParameters = operation.TryGetProperty("parameters", out var parameters)
            ? parameters.EnumerateArray()
                .Where(parameter => parameter.ValueKind == JsonValueKind.Object
                    && parameter.TryGetProperty("name", out var name)
                    && name.ValueKind == JsonValueKind.String
                    && parameter.TryGetProperty("in", out var location)
                    && location.ValueKind == JsonValueKind.String
                    && (string.Equals(location.GetString(), "path", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(location.GetString(), "query", StringComparison.OrdinalIgnoreCase)))
                .Select(parameter => $"{parameter.GetProperty("in").GetString()}:{parameter.GetProperty("name").GetString()}")
                .ToHashSet(StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        actualContractParameters.Should().BeEquivalentTo(expectedContractParameters,
            $"{operationLabel}: SC-306 debe publicar exactamente sus parámetros path/query reales");

        if (expected.RequestSchema is null)
        {
            operation.TryGetProperty("requestBody", out _).Should().BeFalse(
                $"{operationLabel}: la operación sin body no debe publicar requestBody");
        }
        else
        {
            operation.TryGetProperty("requestBody", out var requestBody).Should().BeTrue(
                $"{operationLabel}: debe publicar el requestBody real");
            expected.RequestContentType.Should().NotBeNullOrWhiteSpace();
            var content = requestBody.GetProperty("content");
            content.TryGetProperty(expected.RequestContentType!, out var requestMediaType).Should().BeTrue(
                $"{operationLabel}: el request debe publicar {expected.RequestContentType}");
            var requestSchema = requestMediaType.GetProperty("schema");
            var requestUsesExpectedSchema = SchemaResolvesTo(requestSchema, schemas, expected.RequestSchema)
                || expected.RequestProperties.All(propertyName =>
                    TryGetSchemaProperty(requestSchema, schemas, propertyName, out _));
            requestUsesExpectedSchema.Should().BeTrue(
                $"{operationLabel}: el request debe conservar la forma contractual de {expected.RequestSchema}");
            AssertSchemaPropertiesDocumented(
                schemas,
                new Qg2SchemaDocumentation(expected.RequestSchema, expected.RequestProperties),
                operationLabel);
        }

        operation.TryGetProperty("responses", out var responses).Should().BeTrue(
            $"{operationLabel}: debe publicar responses");
        var expectedStatusCodes = expected.Responses
            .Select(response => response.StatusCode)
            .ToHashSet(StringComparer.Ordinal);
        var actualStatusCodes = responses.EnumerateObject()
            .Select(response => response.Name)
            .ToHashSet(StringComparer.Ordinal);
        actualStatusCodes.Should().BeEquivalentTo(expectedStatusCodes,
            $"{operationLabel}: SC-306 no debe publicar status codes especulativos ni omitir los documentados");

        foreach (var responseExpectation in expected.Responses)
        {
            responses.TryGetProperty(responseExpectation.StatusCode, out var response).Should().BeTrue(
                $"{operationLabel}: debe publicar el status {responseExpectation.StatusCode}");
            response.GetProperty("description").GetString().Should().Be(responseExpectation.Description,
                $"{operationLabel}: el status {responseExpectation.StatusCode} debe conservar su descripción explícita");
            AssertResponseContract(response, schemas, responseExpectation, operationLabel);
        }

        if (expected.ResponseSchemaDocumentation is not null)
        {
            AssertSchemaPropertiesDocumented(schemas, expected.ResponseSchemaDocumentation, operationLabel);
        }
    }

    private static void AssertDigitalEnvelopeQG2(
        JsonElement paths,
        JsonElement schemas,
        CertificateQg2Operation expected)
    {
        var operationLabel = $"{expected.Method} {expected.Path}";
        paths.TryGetProperty(expected.Path, out var pathItem).Should().BeTrue(
            $"{operationLabel}: la operación SC-307 incluida en QG-2 debe existir en OpenAPI");
        pathItem.TryGetProperty(expected.Method.ToLowerInvariant(), out var operation).Should().BeTrue(
            $"{operationLabel}: el método SC-307 incluido en QG-2 debe existir en OpenAPI");

        operation.GetProperty("summary").GetString().Should().Be(expected.Summary,
            $"{operationLabel}: debe conservar el Summary contractual de SC-307");
        operation.GetProperty("description").GetString().Should().Be(expected.Description,
            $"{operationLabel}: debe conservar la Description contractual de SC-307");
        GetOperationTags(operation).Should().Equal("Seguridad y certificados",
            $"{operationLabel}: debe conservar el tag funcional de seguridad y certificados");
        HasBearerSecurity(operation).Should().BeTrue(
            $"{operationLabel}: las operaciones SC-307 deben conservar el requisito Bearer publicado");

        var controllerMethod = expected.ControllerType.GetMethod(expected.ActionName);
        controllerMethod.Should().NotBeNull($"{operationLabel}: debe corresponder a la acción {expected.ActionName}");
        var actionPolicy = controllerMethod!.GetCustomAttributes(inherit: true)
            .OfType<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>()
            .Select(attribute => attribute.Policy)
            .SingleOrDefault(policy => !string.IsNullOrWhiteSpace(policy));
        actionPolicy.Should().Be(expected.Policy,
            $"{operationLabel}: debe conservar la policy real de la acción");

        AssertHeaderAbsent(operation, "Authorization");
        AssertHeaderAbsent(operation, "Idempotency-Key");
        AssertHeaderAbsent(operation, "X-Correlation-ID");

        foreach (var parameter in expected.Parameters)
        {
            AssertDocumentedOperationParameter(operation, parameter, operationLabel);
        }

        var expectedContractParameters = expected.Parameters
            .Select(parameter => $"{parameter.Location}:{parameter.Name}")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var actualContractParameters = operation.TryGetProperty("parameters", out var parameters)
            ? parameters.EnumerateArray()
                .Where(parameter => parameter.ValueKind == JsonValueKind.Object
                    && parameter.TryGetProperty("name", out var name)
                    && name.ValueKind == JsonValueKind.String
                    && parameter.TryGetProperty("in", out var location)
                    && location.ValueKind == JsonValueKind.String
                    && (string.Equals(location.GetString(), "path", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(location.GetString(), "query", StringComparison.OrdinalIgnoreCase)))
                .Select(parameter => $"{parameter.GetProperty("in").GetString()}:{parameter.GetProperty("name").GetString()}")
                .ToHashSet(StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        actualContractParameters.Should().BeEquivalentTo(expectedContractParameters,
            $"{operationLabel}: SC-307 debe publicar exactamente sus parámetros path/query reales");

        if (expected.RequestSchema is null)
        {
            operation.TryGetProperty("requestBody", out _).Should().BeFalse(
                $"{operationLabel}: la operación sin body no debe publicar requestBody");
        }
        else
        {
            operation.TryGetProperty("requestBody", out var requestBody).Should().BeTrue(
                $"{operationLabel}: debe publicar el requestBody real");
            expected.RequestContentType.Should().NotBeNullOrWhiteSpace();
            var content = requestBody.GetProperty("content");
            content.TryGetProperty(expected.RequestContentType!, out var requestMediaType).Should().BeTrue(
                $"{operationLabel}: el request debe publicar {expected.RequestContentType}");
            var requestSchema = requestMediaType.GetProperty("schema");
            var requestUsesExpectedSchema = SchemaResolvesTo(requestSchema, schemas, expected.RequestSchema)
                || expected.RequestProperties.All(propertyName =>
                    TryGetSchemaProperty(requestSchema, schemas, propertyName, out _));
            requestUsesExpectedSchema.Should().BeTrue(
                $"{operationLabel}: el request debe conservar la forma contractual de {expected.RequestSchema}");
            AssertSchemaPropertiesDocumented(
                schemas,
                new Qg2SchemaDocumentation(expected.RequestSchema, expected.RequestProperties),
                operationLabel);
        }

        operation.TryGetProperty("responses", out var responses).Should().BeTrue(
            $"{operationLabel}: debe publicar responses");
        var expectedStatusCodes = expected.Responses
            .Select(response => response.StatusCode)
            .ToHashSet(StringComparer.Ordinal);
        var actualStatusCodes = responses.EnumerateObject()
            .Select(response => response.Name)
            .ToHashSet(StringComparer.Ordinal);
        actualStatusCodes.Should().BeEquivalentTo(expectedStatusCodes,
            $"{operationLabel}: SC-307 no debe publicar status codes especulativos ni omitir los documentados");

        foreach (var responseExpectation in expected.Responses)
        {
            responses.TryGetProperty(responseExpectation.StatusCode, out var response).Should().BeTrue(
                $"{operationLabel}: debe publicar el status {responseExpectation.StatusCode}");
            response.GetProperty("description").GetString().Should().Be(responseExpectation.Description,
                $"{operationLabel}: el status {responseExpectation.StatusCode} debe conservar su descripción explícita");
            AssertResponseContract(response, schemas, responseExpectation, operationLabel);
        }

        if (string.Equals(expected.ActionName, "Encrypt", StringComparison.Ordinal)
            || string.Equals(expected.ActionName, "Decrypt", StringComparison.Ordinal))
        {
            var success = responses.GetProperty("200");
            success.TryGetProperty("content", out var successContent).Should().BeTrue(
                $"{operationLabel}: el archivo exitoso debe publicar content");
            successContent.TryGetProperty("application/octet-stream", out _).Should().BeTrue(
                $"{operationLabel}: el archivo exitoso debe declarar application/octet-stream");
            success.GetProperty("description").GetString().Should().Contain("X-Cryptographic-Profile",
                $"{operationLabel}: la documentación debe declarar el header de respuesta emitido por el controller");
        }

        if (expected.ResponseSchemaDocumentation is not null)
        {
            AssertSchemaPropertiesDocumented(schemas, expected.ResponseSchemaDocumentation, operationLabel);
        }
    }

    private static readonly Qg2SchemaDocumentation[] NachaSecurityOperationSchemaDocumentation =
    [
        new(
            "DigitalEnvelopeOperationDto",
            ["operationId", "operationType", "status", "clearingHouseId", "requestedBy", "requestedAtUtc", "finishedAtUtc", "failCloseApplied", "legacyFallbackUsed", "artifact", "error", "certificateSummary"]),
        new(
            "DigitalEnvelopeOperationArtifactDto",
            ["externalFileName", "contentType", "plainHashSha256", "envelopeHashSha256", "downloadAvailable", "downloadExpiresAtUtc", "sizeBytes"]),
        new(
            "DigitalEnvelopeOperationErrorDto",
            ["code", "message", "retryable"]),
        new(
            "DigitalEnvelopeCertificateSummaryDto",
            ["signingCertificateThumbprintMasked", "encryptionCertificateThumbprintMasked", "secretRefMasked"])
    ];

    private static void AssertNachaSecurityQG2(
        JsonElement paths,
        JsonElement schemas,
        NachaSecurityQg2Operation expected)
    {
        var operationLabel = $"{expected.Method} {expected.Path}";
        paths.TryGetProperty(expected.Path, out var pathItem).Should().BeTrue(
            $"{operationLabel}: la operación SC-308 incluida en QG-2 debe existir en OpenAPI");
        pathItem.TryGetProperty(expected.Method.ToLowerInvariant(), out var operation).Should().BeTrue(
            $"{operationLabel}: el método SC-308 incluido en QG-2 debe existir en OpenAPI");

        operation.GetProperty("summary").GetString().Should().Be(expected.Summary,
            $"{operationLabel}: debe conservar el Summary contractual de SC-308");
        operation.GetProperty("description").GetString().Should().Be(expected.Description,
            $"{operationLabel}: debe conservar la Description contractual de SC-308");
        GetOperationTags(operation).Should().Equal("NACHA-M",
            $"{operationLabel}: debe conservar el tag funcional NACHA-M");
        HasBearerSecurity(operation).Should().BeTrue(
            $"{operationLabel}: las operaciones SC-308 deben conservar el requisito Bearer publicado");

        var controllerMethod = typeof(NachaSecurityOperationsController).GetMethod(expected.ActionName);
        controllerMethod.Should().NotBeNull($"{operationLabel}: debe corresponder a la acción {expected.ActionName}");
        var actionPolicy = controllerMethod!.GetCustomAttributes(inherit: true)
            .OfType<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>()
            .Select(attribute => attribute.Policy)
            .SingleOrDefault(policy => !string.IsNullOrWhiteSpace(policy));
        actionPolicy.Should().Be(expected.Policy,
            $"{operationLabel}: debe conservar la policy real de la acción");

        AssertHeaderAbsent(operation, "Authorization");
        AssertHeaderAbsent(operation, "Idempotency-Key");
        AssertHeaderAbsent(operation, "X-Correlation-ID");

        foreach (var parameter in expected.Parameters)
        {
            AssertDocumentedOperationParameter(operation, parameter, operationLabel);
        }

        var expectedContractParameters = expected.Parameters
            .Select(parameter => $"{parameter.Location}:{parameter.Name}")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var actualContractParameters = operation.TryGetProperty("parameters", out var parameters)
            ? parameters.EnumerateArray()
                .Where(parameter => parameter.ValueKind == JsonValueKind.Object
                    && parameter.TryGetProperty("name", out var name)
                    && name.ValueKind == JsonValueKind.String
                    && parameter.TryGetProperty("in", out var location)
                    && location.ValueKind == JsonValueKind.String
                    && (string.Equals(location.GetString(), "path", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(location.GetString(), "query", StringComparison.OrdinalIgnoreCase)))
                .Select(parameter => $"{parameter.GetProperty("in").GetString()}:{parameter.GetProperty("name").GetString()}")
                .ToHashSet(StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        actualContractParameters.Should().BeEquivalentTo(expectedContractParameters,
            $"{operationLabel}: SC-308 debe publicar exactamente sus parámetros path/query reales");

        switch (expected.RequestShape)
        {
            case NachaSecurityRequestShape.None:
                operation.TryGetProperty("requestBody", out _).Should().BeFalse(
                    $"{operationLabel}: la operación sin body no debe publicar requestBody");
                break;
            case NachaSecurityRequestShape.JsonSchema:
            {
                expected.RequestSchema.Should().NotBeNullOrWhiteSpace();
                operation.TryGetProperty("requestBody", out var requestBody).Should().BeTrue(
                    $"{operationLabel}: debe publicar el requestBody JSON real");
                var content = requestBody.GetProperty("content");
                var jsonMediaType = GetJsonMediaType(content, operationLabel, "request");
                SchemaResolvesTo(jsonMediaType.GetProperty("schema"), schemas, expected.RequestSchema!).Should().BeTrue(
                    $"{operationLabel}: el request debe usar {expected.RequestSchema}");
                AssertSchemaPropertiesDocumented(
                    schemas,
                    new Qg2SchemaDocumentation(expected.RequestSchema!, expected.RequestProperties),
                    operationLabel);
                break;
            }
            case NachaSecurityRequestShape.MultipartFile:
            {
                operation.TryGetProperty("requestBody", out var requestBody).Should().BeTrue(
                    $"{operationLabel}: debe publicar el archivo multipart como requestBody");
                var content = requestBody.GetProperty("content");
                content.TryGetProperty("multipart/form-data", out var multipartMediaType).Should().BeTrue(
                    $"{operationLabel}: el request de archivo debe publicar multipart/form-data");
                var requestSchema = multipartMediaType.GetProperty("schema");
                TryGetSchemaPropertyIgnoreCase(requestSchema, schemas, "file", out var fileProperty).Should().BeTrue(
                    $"{operationLabel}: el request multipart debe contener la propiedad file");
                if (fileProperty.TryGetProperty("format", out var format) && format.ValueKind == JsonValueKind.String)
                {
                    format.GetString().Should().Be("binary",
                        $"{operationLabel}: el archivo multipart debe conservar formato binario");
                }
                break;
            }
            default:
                throw new InvalidOperationException($"Request shape SC-308 no soportado: {expected.RequestShape}");
        }

        operation.TryGetProperty("responses", out var responses).Should().BeTrue(
            $"{operationLabel}: debe publicar responses");
        var expectedStatusCodes = expected.Responses
            .Select(response => response.StatusCode)
            .ToHashSet(StringComparer.Ordinal);
        var actualStatusCodes = responses.EnumerateObject()
            .Select(response => response.Name)
            .ToHashSet(StringComparer.Ordinal);
        actualStatusCodes.Should().BeEquivalentTo(expectedStatusCodes,
            $"{operationLabel}: SC-308 no debe publicar status codes especulativos ni omitir los documentados");

        foreach (var responseExpectation in expected.Responses)
        {
            responses.TryGetProperty(responseExpectation.StatusCode, out var response).Should().BeTrue(
                $"{operationLabel}: debe publicar el status {responseExpectation.StatusCode}");
            response.GetProperty("description").GetString().Should().Be(responseExpectation.Description,
                $"{operationLabel}: el status {responseExpectation.StatusCode} debe conservar su descripción explícita");
            AssertResponseContract(response, schemas, responseExpectation, operationLabel);
        }

        if (expected.Responses.Any(response => string.Equals(response.SchemaName, "DigitalEnvelopeOperationDto", StringComparison.Ordinal)))
        {
            foreach (var schemaDocumentation in NachaSecurityOperationSchemaDocumentation)
            {
                AssertSchemaPropertiesDocumented(schemas, schemaDocumentation, operationLabel);
            }
        }
    }

    private static void AssertSc309QG2(
        JsonElement paths,
        JsonElement schemas,
        Sc309Qg2Operation expected)
    {
        var operationLabel = $"{expected.Method} {expected.Path}";
        paths.TryGetProperty(expected.Path, out var pathItem).Should().BeTrue(
            $"{operationLabel}: la operación SC-309 incluida en QG-2 debe existir en OpenAPI");
        pathItem.TryGetProperty(expected.Method.ToLowerInvariant(), out var operation).Should().BeTrue(
            $"{operationLabel}: el método SC-309 incluido en QG-2 debe existir en OpenAPI");

        operation.GetProperty("summary").GetString().Should().Be(expected.Summary,
            $"{operationLabel}: debe conservar el Summary contractual de SC-309");
        operation.GetProperty("description").GetString().Should().Be(expected.Description,
            $"{operationLabel}: debe conservar la Description contractual de SC-309");
        GetOperationTags(operation).Should().Equal("NACHA-M",
            $"{operationLabel}: debe conservar el tag funcional NACHA-M");
        HasBearerSecurity(operation).Should().BeTrue(
            $"{operationLabel}: las operaciones SC-309 deben conservar el requisito Bearer publicado");

        var controllerMethod = expected.ControllerType.GetMethod(expected.ActionName);
        controllerMethod.Should().NotBeNull($"{operationLabel}: debe corresponder a la acción {expected.ActionName}");
        var actionPolicy = controllerMethod!.GetCustomAttributes(inherit: true)
            .OfType<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>()
            .Select(attribute => attribute.Policy)
            .SingleOrDefault(policy => !string.IsNullOrWhiteSpace(policy));
        actionPolicy.Should().Be(expected.Policy,
            $"{operationLabel}: debe conservar la policy real de la acción");

        AssertHeaderAbsent(operation, "Authorization");
        AssertHeaderAbsent(operation, "Idempotency-Key");
        AssertHeaderAbsent(operation, "X-Correlation-ID");

        foreach (var parameter in expected.Parameters)
        {
            AssertDocumentedOperationParameter(operation, parameter, operationLabel);
        }

        var expectedContractParameters = expected.Parameters
            .Select(parameter => $"{parameter.Location}:{parameter.Name}")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var actualContractParameters = operation.TryGetProperty("parameters", out var parameters)
            ? parameters.EnumerateArray()
                .Where(parameter => parameter.ValueKind == JsonValueKind.Object
                    && parameter.TryGetProperty("name", out var name)
                    && name.ValueKind == JsonValueKind.String
                    && parameter.TryGetProperty("in", out var location)
                    && location.ValueKind == JsonValueKind.String
                    && (string.Equals(location.GetString(), "path", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(location.GetString(), "query", StringComparison.OrdinalIgnoreCase)))
                .Select(parameter => $"{parameter.GetProperty("in").GetString()}:{parameter.GetProperty("name").GetString()}")
                .ToHashSet(StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        actualContractParameters.Should().BeEquivalentTo(expectedContractParameters,
            $"{operationLabel}: SC-309 debe publicar exactamente sus parámetros path/query reales");

        switch (expected.RequestShape)
        {
            case Sc309RequestShape.None:
                operation.TryGetProperty("requestBody", out _).Should().BeFalse(
                    $"{operationLabel}: la operación sin body no debe publicar requestBody");
                break;
            case Sc309RequestShape.MultipartSchema:
            {
                expected.RequestSchema.Should().NotBeNullOrWhiteSpace();
                operation.TryGetProperty("requestBody", out var requestBody).Should().BeTrue(
                    $"{operationLabel}: debe publicar el requestBody multipart real");
                var content = requestBody.GetProperty("content");
                content.TryGetProperty("multipart/form-data", out var multipartMediaType).Should().BeTrue(
                    $"{operationLabel}: el upload debe publicar multipart/form-data");
                var requestSchema = multipartMediaType.GetProperty("schema");
                foreach (var propertyName in expected.RequestProperties)
                {
                    TryGetSchemaPropertyIgnoreCase(requestSchema, schemas, propertyName, out var property).Should().BeTrue(
                        $"{operationLabel}: el multipart debe publicar el campo {propertyName}");
                    property.TryGetProperty("description", out var propertyDescription).Should().BeTrue(
                        $"{operationLabel}: {expected.RequestSchema}.{propertyName} debe exponer documentación XML");
                    propertyDescription.GetString().Should().NotBeNullOrWhiteSpace(
                        $"{operationLabel}: {expected.RequestSchema}.{propertyName} debe tener descripción XML");
                    if (string.Equals(propertyName, "file", StringComparison.OrdinalIgnoreCase)
                        && property.TryGetProperty("format", out var format)
                        && format.ValueKind == JsonValueKind.String)
                    {
                        format.GetString().Should().Be("binary",
                            $"{operationLabel}: el campo file debe conservar formato binario");
                    }
                }
                break;
            }
            default:
                throw new InvalidOperationException($"Request shape SC-309 no soportado: {expected.RequestShape}");
        }

        operation.TryGetProperty("responses", out var responses).Should().BeTrue(
            $"{operationLabel}: debe publicar responses");
        var expectedStatusCodes = expected.Responses
            .Select(response => response.StatusCode)
            .ToHashSet(StringComparer.Ordinal);
        var actualStatusCodes = responses.EnumerateObject()
            .Select(response => response.Name)
            .ToHashSet(StringComparer.Ordinal);
        actualStatusCodes.Should().BeEquivalentTo(expectedStatusCodes,
            $"{operationLabel}: SC-309 no debe publicar status codes especulativos ni omitir los documentados");

        foreach (var responseExpectation in expected.Responses)
        {
            responses.TryGetProperty(responseExpectation.StatusCode, out var response).Should().BeTrue(
                $"{operationLabel}: debe publicar el status {responseExpectation.StatusCode}");
            response.GetProperty("description").GetString().Should().Be(responseExpectation.Description,
                $"{operationLabel}: el status {responseExpectation.StatusCode} debe conservar su descripción explícita");
            AssertResponseContract(response, schemas, responseExpectation, operationLabel);
        }

        var success = responses.GetProperty("200");
        success.TryGetProperty("content", out var successContent).Should().BeTrue(
            $"{operationLabel}: el response exitoso debe publicar content");
        foreach (var contentType in expected.SuccessContentTypes)
        {
            successContent.TryGetProperty(contentType, out _).Should().BeTrue(
                $"{operationLabel}: el response exitoso debe declarar {contentType}");
        }

        var successDescription = success.GetProperty("description").GetString() ?? string.Empty;
        foreach (var fragment in expected.SuccessDescriptionFragments)
        {
            successDescription.Should().Contain(fragment,
                $"{operationLabel}: la descripción del response debe documentar {fragment}");
        }

        if (expected.ResponseSchemaDocumentation is not null)
        {
            AssertSchemaPropertiesDocumented(schemas, expected.ResponseSchemaDocumentation, operationLabel);
        }
    }

    private static void AssertSc401QG2(
        JsonElement paths,
        JsonElement schemas,
        Sc401Qg2Operation expected)
    {
        var operationLabel = $"{expected.Method} {expected.Path}";
        paths.TryGetProperty(expected.Path, out var pathItem).Should().BeTrue(
            $"{operationLabel}: la operación SC-401 incluida en QG-2 debe existir en OpenAPI");
        pathItem.TryGetProperty(expected.Method.ToLowerInvariant(), out var operation).Should().BeTrue(
            $"{operationLabel}: el método SC-401 incluido en QG-2 debe existir en OpenAPI");

        operation.GetProperty("summary").GetString().Should().Be(expected.Summary,
            $"{operationLabel}: debe conservar el Summary contractual de SC-401");
        operation.GetProperty("description").GetString().Should().Be(expected.Description,
            $"{operationLabel}: debe conservar la Description contractual de SC-401");
        GetOperationTags(operation).Should().Equal("ACH Colombia",
            $"{operationLabel}: debe conservar el tag funcional ACH Colombia");
        HasBearerSecurity(operation).Should().BeTrue(
            $"{operationLabel}: las operaciones SC-401 deben conservar el requisito Bearer publicado");

        var controllerMethod = typeof(AchColombiaFileExchangeController).GetMethod(expected.ActionName);
        controllerMethod.Should().NotBeNull($"{operationLabel}: debe corresponder a la acción {expected.ActionName}");
        var actionPolicy = controllerMethod!.GetCustomAttributes(inherit: true)
            .OfType<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>()
            .Select(attribute => attribute.Policy)
            .SingleOrDefault(policy => !string.IsNullOrWhiteSpace(policy));
        actionPolicy.Should().Be(expected.Policy,
            $"{operationLabel}: debe conservar la policy real de la acción");

        AssertHeaderAbsent(operation, "Authorization");
        AssertHeaderAbsent(operation, "X-Correlation-ID");
        if (expected.HasIdempotencyKey)
        {
            AssertHeaderParameter(operation, "Idempotency-Key");
        }
        else
        {
            AssertHeaderAbsent(operation, "Idempotency-Key");
        }

        foreach (var parameter in expected.Parameters)
        {
            AssertDocumentedOperationParameter(operation, parameter, operationLabel);
        }

        var expectedContractParameters = expected.Parameters
            .Select(parameter => $"{parameter.Location}:{parameter.Name}")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var actualContractParameters = operation.TryGetProperty("parameters", out var parameters)
            ? parameters.EnumerateArray()
                .Where(parameter => parameter.ValueKind == JsonValueKind.Object
                    && parameter.TryGetProperty("name", out var name)
                    && name.ValueKind == JsonValueKind.String
                    && parameter.TryGetProperty("in", out var location)
                    && location.ValueKind == JsonValueKind.String
                    && (string.Equals(location.GetString(), "path", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(location.GetString(), "query", StringComparison.OrdinalIgnoreCase)))
                .Select(parameter => $"{parameter.GetProperty("in").GetString()}:{parameter.GetProperty("name").GetString()}")
                .ToHashSet(StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        actualContractParameters.Should().BeEquivalentTo(expectedContractParameters,
            $"{operationLabel}: SC-401 debe publicar exactamente sus parámetros path/query reales");

        switch (expected.RequestShape)
        {
            case Sc401RequestShape.None:
                operation.TryGetProperty("requestBody", out _).Should().BeFalse(
                    $"{operationLabel}: la operación sin body no debe publicar requestBody");
                break;
            case Sc401RequestShape.JsonSchema:
            {
                operation.TryGetProperty("requestBody", out var requestBody).Should().BeTrue(
                    $"{operationLabel}: debe publicar el requestBody JSON real");
                var requestMediaType = GetJsonMediaType(requestBody.GetProperty("content"), operationLabel, "request");
                var requestSchema = requestMediaType.GetProperty("schema");
                foreach (var propertyName in expected.RequestProperties)
                {
                    TryGetSchemaPropertyIgnoreCase(requestSchema, schemas, propertyName, out var property).Should().BeTrue(
                        $"{operationLabel}: el request debe publicar la propiedad {propertyName}");
                    property.TryGetProperty("description", out var propertyDescription).Should().BeTrue(
                        $"{operationLabel}: {propertyName} debe exponer documentación XML");
                    propertyDescription.GetString().Should().NotBeNullOrWhiteSpace(
                        $"{operationLabel}: {propertyName} debe tener descripción XML");
                }
                break;
            }
            default:
                throw new InvalidOperationException($"Request shape SC-401 no soportado: {expected.RequestShape}");
        }

        operation.TryGetProperty("responses", out var responses).Should().BeTrue(
            $"{operationLabel}: debe publicar responses");
        var expectedStatusCodes = expected.Responses
            .Select(response => response.StatusCode)
            .ToHashSet(StringComparer.Ordinal);
        var actualStatusCodes = responses.EnumerateObject()
            .Select(response => response.Name)
            .ToHashSet(StringComparer.Ordinal);
        actualStatusCodes.Should().BeEquivalentTo(expectedStatusCodes,
            $"{operationLabel}: SC-401 no debe publicar status codes especulativos ni omitir los documentados");

        foreach (var responseExpectation in expected.Responses)
        {
            responses.TryGetProperty(responseExpectation.StatusCode, out var response).Should().BeTrue(
                $"{operationLabel}: debe publicar el status {responseExpectation.StatusCode}");
            response.GetProperty("description").GetString().Should().Be(responseExpectation.Description,
                $"{operationLabel}: el status {responseExpectation.StatusCode} debe conservar su descripción explícita");
            AssertResponseContract(response, schemas, responseExpectation, operationLabel);
        }

        if (expected.SuccessContentTypes.Length > 0)
        {
            var success = responses.GetProperty("200");
            success.TryGetProperty("content", out var successContent).Should().BeTrue(
                $"{operationLabel}: el response exitoso debe publicar content");
            foreach (var contentType in expected.SuccessContentTypes)
            {
                successContent.TryGetProperty(contentType, out _).Should().BeTrue(
                    $"{operationLabel}: el response exitoso debe declarar {contentType}");
            }
        }

        if (expected.SuccessDescriptionFragments.Length > 0)
        {
            var successDescription = responses.GetProperty("200").GetProperty("description").GetString() ?? string.Empty;
            foreach (var fragment in expected.SuccessDescriptionFragments)
            {
                successDescription.Should().Contain(fragment,
                    $"{operationLabel}: la descripción del response debe documentar {fragment}");
            }
        }

        if (expected.ResponseSchemaDocumentation is not null)
        {
            AssertSchemaPropertiesDocumented(schemas, expected.ResponseSchemaDocumentation, operationLabel);
        }
    }

    private static void AssertSc402QG2(
        JsonElement paths,
        JsonElement schemas,
        Sc402Qg2Operation expected)
    {
        var operationLabel = $"{expected.Method} {expected.Path}";
        paths.TryGetProperty(expected.Path, out var pathItem).Should().BeTrue(
            $"{operationLabel}: la operación SC-402 incluida en QG-2 debe existir en OpenAPI");
        pathItem.TryGetProperty(expected.Method.ToLowerInvariant(), out var operation).Should().BeTrue(
            $"{operationLabel}: el método SC-402 incluido en QG-2 debe existir en OpenAPI");

        operation.GetProperty("summary").GetString().Should().Be(expected.Summary,
            $"{operationLabel}: debe conservar el Summary contractual de SC-402");
        operation.GetProperty("description").GetString().Should().Be(expected.Description,
            $"{operationLabel}: debe conservar la Description contractual de SC-402");
        GetOperationTags(operation).Should().Equal("Integraciones",
            $"{operationLabel}: debe conservar el tag funcional Integraciones");
        HasBearerSecurity(operation).Should().BeTrue(
            $"{operationLabel}: las operaciones SC-402 deben conservar el requisito Bearer publicado");

        var controllerMethod = typeof(IntegrationMappingSetsController).GetMethod(expected.ActionName);
        controllerMethod.Should().NotBeNull($"{operationLabel}: debe corresponder a la acción {expected.ActionName}");
        var actionPolicy = controllerMethod!.GetCustomAttributes(inherit: true)
            .OfType<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>()
            .Select(attribute => attribute.Policy)
            .SingleOrDefault(policy => !string.IsNullOrWhiteSpace(policy));
        actionPolicy.Should().Be(expected.Policy,
            $"{operationLabel}: debe conservar la policy real de la acción");

        AssertHeaderAbsent(operation, "Authorization");
        AssertHeaderAbsent(operation, "Idempotency-Key");
        AssertHeaderAbsent(operation, "X-Correlation-ID");

        foreach (var parameter in expected.Parameters)
        {
            AssertDocumentedOperationParameter(operation, parameter, operationLabel);
        }

        var expectedContractParameters = expected.Parameters
            .Select(parameter => $"{parameter.Location}:{parameter.Name}")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var actualContractParameters = operation.TryGetProperty("parameters", out var parameters)
            ? parameters.EnumerateArray()
                .Where(parameter => parameter.ValueKind == JsonValueKind.Object
                    && parameter.TryGetProperty("name", out var name)
                    && name.ValueKind == JsonValueKind.String
                    && parameter.TryGetProperty("in", out var location)
                    && location.ValueKind == JsonValueKind.String
                    && (string.Equals(location.GetString(), "path", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(location.GetString(), "query", StringComparison.OrdinalIgnoreCase)))
                .Select(parameter => $"{parameter.GetProperty("in").GetString()}:{parameter.GetProperty("name").GetString()}")
                .ToHashSet(StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        actualContractParameters.Should().BeEquivalentTo(expectedContractParameters,
            $"{operationLabel}: SC-402 debe publicar exactamente sus parámetros path/query reales");

        if (expected.RequestSchema is null)
        {
            operation.TryGetProperty("requestBody", out _).Should().BeFalse(
                $"{operationLabel}: la operación sin body no debe publicar requestBody");
        }
        else
        {
            operation.TryGetProperty("requestBody", out var requestBody).Should().BeTrue(
                $"{operationLabel}: debe publicar el requestBody JSON real");
            var requestMediaType = GetJsonMediaType(requestBody.GetProperty("content"), operationLabel, "request");
            var requestSchema = requestMediaType.GetProperty("schema");
            SchemaResolvesTo(requestSchema, schemas, expected.RequestSchema).Should().BeTrue(
                $"{operationLabel}: el request debe usar el schema {expected.RequestSchema}");
            AssertSchemaPropertiesDocumented(
                schemas,
                new Qg2SchemaDocumentation(expected.RequestSchema, expected.RequestProperties),
                operationLabel);
        }

        operation.TryGetProperty("responses", out var responses).Should().BeTrue(
            $"{operationLabel}: debe publicar responses");
        var expectedStatusCodes = expected.Responses
            .Select(response => response.StatusCode)
            .ToHashSet(StringComparer.Ordinal);
        var actualStatusCodes = responses.EnumerateObject()
            .Select(response => response.Name)
            .ToHashSet(StringComparer.Ordinal);
        actualStatusCodes.Should().BeEquivalentTo(expectedStatusCodes,
            $"{operationLabel}: SC-402 no debe publicar status codes especulativos ni omitir los documentados");

        foreach (var responseExpectation in expected.Responses)
        {
            responses.TryGetProperty(responseExpectation.StatusCode, out var response).Should().BeTrue(
                $"{operationLabel}: debe publicar el status {responseExpectation.StatusCode}");
            response.GetProperty("description").GetString().Should().Be(responseExpectation.Description,
                $"{operationLabel}: el status {responseExpectation.StatusCode} debe conservar su descripción explícita");
            AssertResponseContract(response, schemas, responseExpectation, operationLabel);
        }

        if (expected.ResponseSchemaDocumentation is not null)
        {
            AssertSchemaPropertiesDocumented(schemas, expected.ResponseSchemaDocumentation, operationLabel);
        }
    }

    private static void AssertSc403QG2(
        JsonElement paths,
        JsonElement schemas,
        Sc403Qg2Operation expected)
    {
        var operationLabel = $"{expected.Method} {expected.Path}";
        paths.TryGetProperty(expected.Path, out var pathItem).Should().BeTrue(
            $"{operationLabel}: la operación SC-403 incluida en QG-2 debe existir en OpenAPI");
        pathItem.TryGetProperty(expected.Method.ToLowerInvariant(), out var operation).Should().BeTrue(
            $"{operationLabel}: el método SC-403 incluido en QG-2 debe existir en OpenAPI");

        operation.GetProperty("summary").GetString().Should().Be(expected.Summary,
            $"{operationLabel}: debe conservar el Summary contractual de SC-403");
        operation.GetProperty("description").GetString().Should().Be(expected.Description,
            $"{operationLabel}: debe conservar la Description contractual de SC-403");
        GetOperationTags(operation).Should().Equal("Integraciones",
            $"{operationLabel}: debe conservar el tag funcional Integraciones");
        HasBearerSecurity(operation).Should().BeTrue(
            $"{operationLabel}: las operaciones SC-403 deben conservar el requisito Bearer publicado");

        var controllerMethod = expected.ControllerType.GetMethod(expected.ActionName);
        controllerMethod.Should().NotBeNull($"{operationLabel}: debe corresponder a la acción {expected.ActionName}");
        var declaredPolicies = controllerMethod!.GetCustomAttributes(inherit: true)
            .OfType<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>()
            .Concat(expected.ControllerType.GetCustomAttributes(inherit: true)
                .OfType<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>())
            .Select(attribute => attribute.Policy)
            .Where(policy => !string.IsNullOrWhiteSpace(policy))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        declaredPolicies.Should().ContainSingle(
            $"{operationLabel}: debe existir una única policy contractual no vacía entre acción y controller");
        declaredPolicies.Single().Should().Be(expected.Policy,
            $"{operationLabel}: debe conservar la policy real de la operación");

        AssertHeaderAbsent(operation, "Authorization");
        AssertHeaderAbsent(operation, "Idempotency-Key");
        AssertHeaderAbsent(operation, "X-Correlation-ID");

        foreach (var parameter in expected.Parameters)
        {
            AssertDocumentedOperationParameter(operation, parameter, operationLabel);
        }

        var expectedContractParameters = expected.Parameters
            .Select(parameter => $"{parameter.Location}:{parameter.Name}")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var actualContractParameters = operation.TryGetProperty("parameters", out var parameters)
            ? parameters.EnumerateArray()
                .Where(parameter => parameter.ValueKind == JsonValueKind.Object
                    && parameter.TryGetProperty("name", out var name)
                    && name.ValueKind == JsonValueKind.String
                    && parameter.TryGetProperty("in", out var location)
                    && location.ValueKind == JsonValueKind.String
                    && (string.Equals(location.GetString(), "path", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(location.GetString(), "query", StringComparison.OrdinalIgnoreCase)))
                .Select(parameter => $"{parameter.GetProperty("in").GetString()}:{parameter.GetProperty("name").GetString()}")
                .ToHashSet(StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        actualContractParameters.Should().BeEquivalentTo(expectedContractParameters,
            $"{operationLabel}: SC-403 debe publicar exactamente sus parámetros path/query reales");

        if (expected.RequestSchema is null)
        {
            operation.TryGetProperty("requestBody", out _).Should().BeFalse(
                $"{operationLabel}: la operación sin body no debe publicar requestBody");
        }
        else
        {
            operation.TryGetProperty("requestBody", out var requestBody).Should().BeTrue(
                $"{operationLabel}: debe publicar el requestBody JSON real");
            var requestMediaType = GetJsonMediaType(requestBody.GetProperty("content"), operationLabel, "request");
            SchemaResolvesTo(requestMediaType.GetProperty("schema"), schemas, expected.RequestSchema).Should().BeTrue(
                $"{operationLabel}: el request debe usar el schema {expected.RequestSchema}");
            AssertSchemaPropertiesDocumented(
                schemas,
                new Qg2SchemaDocumentation(expected.RequestSchema, expected.RequestProperties),
                operationLabel);
        }

        operation.TryGetProperty("responses", out var responses).Should().BeTrue(
            $"{operationLabel}: debe publicar responses");
        var expectedStatusCodes = expected.Responses
            .Select(response => response.StatusCode)
            .ToHashSet(StringComparer.Ordinal);
        var actualStatusCodes = responses.EnumerateObject()
            .Select(response => response.Name)
            .ToHashSet(StringComparer.Ordinal);
        actualStatusCodes.Should().BeEquivalentTo(expectedStatusCodes,
            $"{operationLabel}: SC-403 no debe publicar status codes especulativos ni omitir los documentados");

        foreach (var responseExpectation in expected.Responses)
        {
            responses.TryGetProperty(responseExpectation.StatusCode, out var response).Should().BeTrue(
                $"{operationLabel}: debe publicar el status {responseExpectation.StatusCode}");
            response.GetProperty("description").GetString().Should().Be(responseExpectation.Description,
                $"{operationLabel}: el status {responseExpectation.StatusCode} debe conservar su descripción explícita");
            AssertResponseContract(response, schemas, responseExpectation, operationLabel);
        }

        if (expected.ResponseSchemaDocumentation is not null)
        {
            AssertSchemaPropertiesDocumented(schemas, expected.ResponseSchemaDocumentation, operationLabel);
        }
    }

    private static void AssertSc404QG2(
        JsonElement paths,
        JsonElement schemas,
        Sc404Qg2Operation expected)
    {
        var operationLabel = $"{expected.Method} {expected.Path}";
        paths.TryGetProperty(expected.Path, out var pathItem).Should().BeTrue(
            $"{operationLabel}: la operación SC-404 incluida en QG-2 debe existir en OpenAPI");
        pathItem.TryGetProperty(expected.Method.ToLowerInvariant(), out var operation).Should().BeTrue(
            $"{operationLabel}: el método SC-404 incluido en QG-2 debe existir en OpenAPI");

        operation.GetProperty("summary").GetString().Should().Be(expected.Summary,
            $"{operationLabel}: debe conservar el Summary contractual de SC-404");
        operation.GetProperty("description").GetString().Should().Be(expected.Description,
            $"{operationLabel}: debe conservar la Description contractual de SC-404");
        GetOperationTags(operation).Should().Equal("Operación y programación",
            $"{operationLabel}: debe conservar el tag funcional Operación y programación");
        HasBearerSecurity(operation).Should().BeTrue(
            $"{operationLabel}: las operaciones SC-404 deben conservar el requisito Bearer publicado");

        var controllerMethod = typeof(SchedulerController).GetMethod(expected.ActionName);
        controllerMethod.Should().NotBeNull($"{operationLabel}: debe corresponder a la acción {expected.ActionName}");
        var declaredPolicies = controllerMethod!.GetCustomAttributes(inherit: true)
            .OfType<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>()
            .Concat(typeof(SchedulerController).GetCustomAttributes(inherit: true)
                .OfType<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>())
            .Select(attribute => attribute.Policy)
            .Where(policy => !string.IsNullOrWhiteSpace(policy))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        declaredPolicies.Should().ContainSingle(
            $"{operationLabel}: debe existir una única policy contractual no vacía entre acción y controller");
        declaredPolicies.Single().Should().Be(expected.Policy,
            $"{operationLabel}: debe conservar la policy real de la operación");

        AssertHeaderAbsent(operation, "Authorization");
        AssertHeaderAbsent(operation, "Idempotency-Key");
        if (expected.HasCorrelationId)
        {
            AssertHeaderParameter(operation, "X-Correlation-ID");
        }
        else
        {
            AssertHeaderAbsent(operation, "X-Correlation-ID");
        }

        foreach (var parameter in expected.Parameters)
        {
            AssertDocumentedOperationParameter(operation, parameter, operationLabel);
        }

        var expectedContractParameters = expected.Parameters
            .Select(parameter => $"{parameter.Location}:{parameter.Name}")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var actualContractParameters = operation.TryGetProperty("parameters", out var parameters)
            ? parameters.EnumerateArray()
                .Where(parameter => parameter.ValueKind == JsonValueKind.Object
                    && parameter.TryGetProperty("name", out var name)
                    && name.ValueKind == JsonValueKind.String
                    && parameter.TryGetProperty("in", out var location)
                    && location.ValueKind == JsonValueKind.String
                    && (string.Equals(location.GetString(), "path", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(location.GetString(), "query", StringComparison.OrdinalIgnoreCase)))
                .Select(parameter => $"{parameter.GetProperty("in").GetString()}:{parameter.GetProperty("name").GetString()}")
                .ToHashSet(StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        actualContractParameters.Should().BeEquivalentTo(expectedContractParameters,
            $"{operationLabel}: SC-404 debe publicar exactamente sus parámetros path/query reales");

        if (expected.RequestSchema is null)
        {
            operation.TryGetProperty("requestBody", out _).Should().BeFalse(
                $"{operationLabel}: la operación sin body no debe publicar requestBody");
        }
        else
        {
            operation.TryGetProperty("requestBody", out var requestBody).Should().BeTrue(
                $"{operationLabel}: debe publicar el requestBody JSON real");
            var requestMediaType = GetJsonMediaType(requestBody.GetProperty("content"), operationLabel, "request");
            SchemaResolvesTo(requestMediaType.GetProperty("schema"), schemas, expected.RequestSchema).Should().BeTrue(
                $"{operationLabel}: el request debe usar el schema {expected.RequestSchema}");
            AssertSchemaPropertiesDocumented(
                schemas,
                new Qg2SchemaDocumentation(expected.RequestSchema, expected.RequestProperties),
                operationLabel);
        }

        operation.TryGetProperty("responses", out var responses).Should().BeTrue(
            $"{operationLabel}: debe publicar responses");
        var expectedStatusCodes = expected.Responses
            .Select(response => response.StatusCode)
            .ToHashSet(StringComparer.Ordinal);
        var actualStatusCodes = responses.EnumerateObject()
            .Select(response => response.Name)
            .ToHashSet(StringComparer.Ordinal);
        actualStatusCodes.Should().BeEquivalentTo(expectedStatusCodes,
            $"{operationLabel}: SC-404 no debe publicar status codes especulativos ni omitir los documentados");

        foreach (var responseExpectation in expected.Responses)
        {
            responses.TryGetProperty(responseExpectation.StatusCode, out var response).Should().BeTrue(
                $"{operationLabel}: debe publicar el status {responseExpectation.StatusCode}");
            response.GetProperty("description").GetString().Should().Be(responseExpectation.Description,
                $"{operationLabel}: el status {responseExpectation.StatusCode} debe conservar su descripción explícita");
            AssertResponseContract(response, schemas, responseExpectation, operationLabel);
        }

        if (expected.ResponseSchemaDocumentation is not null)
        {
            AssertSchemaPropertiesDocumented(schemas, expected.ResponseSchemaDocumentation, operationLabel);
        }
    }

    private static void AssertSc405QG2(
        JsonElement paths,
        JsonElement schemas,
        Sc405Qg2Operation expected)
    {
        var operationLabel = $"{expected.Method} {expected.Path}";
        paths.TryGetProperty(expected.Path, out var pathItem).Should().BeTrue(
            $"{operationLabel}: la operación SC-405 incluida en QG-2 debe existir en OpenAPI");
        pathItem.TryGetProperty(expected.Method.ToLowerInvariant(), out var operation).Should().BeTrue(
            $"{operationLabel}: el método SC-405 incluido en QG-2 debe existir en OpenAPI");

        operation.GetProperty("summary").GetString().Should().Be(expected.Summary,
            $"{operationLabel}: debe conservar el Summary contractual de SC-405");
        operation.GetProperty("description").GetString().Should().Be(expected.Description,
            $"{operationLabel}: debe conservar la Description contractual de SC-405");
        GetOperationTags(operation).Should().Equal("Operación y programación",
            $"{operationLabel}: debe conservar el tag funcional Operación y programación");
        HasBearerSecurity(operation).Should().BeTrue(
            $"{operationLabel}: las operaciones SC-405 deben conservar el requisito Bearer publicado");

        var controllerMethod = typeof(TaskDefinitionsController).GetMethods()
            .Where(method => string.Equals(method.Name, expected.ActionName, StringComparison.Ordinal))
            .Where(method => method.GetCustomAttributes(inherit: true)
                .OfType<Microsoft.AspNetCore.Mvc.Routing.HttpMethodAttribute>()
                .Any(attribute => attribute.HttpMethods.Contains(expected.Method, StringComparer.OrdinalIgnoreCase)
                    && string.Equals(attribute.Template, expected.ActionRouteTemplate, StringComparison.Ordinal)))
            .SingleOrDefault();
        controllerMethod.Should().NotBeNull($"{operationLabel}: debe corresponder a una única acción {expected.ActionName}");
        var declaredPolicies = controllerMethod!.GetCustomAttributes(inherit: true)
            .OfType<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>()
            .Concat(typeof(TaskDefinitionsController).GetCustomAttributes(inherit: true)
                .OfType<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>())
            .Select(attribute => attribute.Policy)
            .Where(policy => !string.IsNullOrWhiteSpace(policy))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        declaredPolicies.Should().ContainSingle(
            $"{operationLabel}: debe existir una única policy contractual no vacía entre acción y controller");
        declaredPolicies.Single().Should().Be(expected.Policy,
            $"{operationLabel}: debe conservar la policy real de la operación");

        AssertHeaderAbsent(operation, "Authorization");
        AssertHeaderAbsent(operation, "Idempotency-Key");
        AssertHeaderAbsent(operation, "X-Correlation-ID");

        foreach (var parameter in expected.Parameters)
        {
            AssertDocumentedOperationParameter(operation, parameter, operationLabel);
        }

        var expectedContractParameters = expected.Parameters
            .Select(parameter => $"{parameter.Location}:{parameter.Name}")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var actualContractParameters = operation.TryGetProperty("parameters", out var parameters)
            ? parameters.EnumerateArray()
                .Where(parameter => parameter.ValueKind == JsonValueKind.Object
                    && parameter.TryGetProperty("name", out var name)
                    && name.ValueKind == JsonValueKind.String
                    && parameter.TryGetProperty("in", out var location)
                    && location.ValueKind == JsonValueKind.String
                    && (string.Equals(location.GetString(), "path", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(location.GetString(), "query", StringComparison.OrdinalIgnoreCase)))
                .Select(parameter => $"{parameter.GetProperty("in").GetString()}:{parameter.GetProperty("name").GetString()}")
                .ToHashSet(StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        actualContractParameters.Should().BeEquivalentTo(expectedContractParameters,
            $"{operationLabel}: SC-405 debe publicar exactamente sus parámetros path/query reales");

        if (expected.RequestSchema is null)
        {
            operation.TryGetProperty("requestBody", out _).Should().BeFalse(
                $"{operationLabel}: la operación sin body no debe publicar requestBody");
        }
        else
        {
            operation.TryGetProperty("requestBody", out var requestBody).Should().BeTrue(
                $"{operationLabel}: debe publicar el requestBody JSON real");
            var requestMediaType = GetJsonMediaType(requestBody.GetProperty("content"), operationLabel, "request");
            SchemaResolvesTo(requestMediaType.GetProperty("schema"), schemas, expected.RequestSchema).Should().BeTrue(
                $"{operationLabel}: el request debe usar el schema {expected.RequestSchema}");
            AssertSchemaPropertiesDocumented(
                schemas,
                new Qg2SchemaDocumentation(expected.RequestSchema, expected.RequestProperties),
                operationLabel);
        }

        operation.TryGetProperty("responses", out var responses).Should().BeTrue(
            $"{operationLabel}: debe publicar responses");
        var expectedStatusCodes = expected.Responses
            .Select(response => response.StatusCode)
            .ToHashSet(StringComparer.Ordinal);
        var actualStatusCodes = responses.EnumerateObject()
            .Select(response => response.Name)
            .ToHashSet(StringComparer.Ordinal);
        actualStatusCodes.Should().BeEquivalentTo(expectedStatusCodes,
            $"{operationLabel}: SC-405 no debe publicar status codes especulativos ni omitir los documentados");

        foreach (var responseExpectation in expected.Responses)
        {
            responses.TryGetProperty(responseExpectation.StatusCode, out var response).Should().BeTrue(
                $"{operationLabel}: debe publicar el status {responseExpectation.StatusCode}");
            response.GetProperty("description").GetString().Should().Be(responseExpectation.Description,
                $"{operationLabel}: el status {responseExpectation.StatusCode} debe conservar su descripción explícita");
            AssertResponseContract(response, schemas, responseExpectation, operationLabel);
        }

        if (expected.ResponseSchemaDocumentation is not null)
        {
            AssertSchemaPropertiesDocumented(schemas, expected.ResponseSchemaDocumentation, operationLabel);
        }
    }

    private sealed record TransactionPoliciesQg2Operation(
        string Method,
        string Path,
        string PrimaryStatusCode,
        Qg2Parameter[] Parameters,
        string? RequestSchema,
        string[] RequestProperties,
        Qg2Response[] Responses,
        Qg2SchemaDocumentation? ResponseSchemaDocumentation = null);

    private static MethodInfo? FindReportControllerMethod(string actionName)
    {
        return typeof(AuthController).Assembly.GetTypes()
            .Where(type => !type.IsAbstract && typeof(Microsoft.AspNetCore.Mvc.ControllerBase).IsAssignableFrom(type))
            .Where(type => type.GetCustomAttributes(inherit: true)
                .OfType<Microsoft.AspNetCore.Mvc.RouteAttribute>()
                .Any(route => string.Equals(route.Template, "api/reports", StringComparison.Ordinal)))
            .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            .SingleOrDefault(method => string.Equals(method.Name, actionName, StringComparison.Ordinal));
    }

    private static void AssertSc406QG2(
        JsonElement paths,
        JsonElement schemas,
        Sc406Qg2Operation expected)
    {
        var operationLabel = $"{expected.Method} {expected.Path}";
        paths.TryGetProperty(expected.Path, out var pathItem).Should().BeTrue(
            $"{operationLabel}: la operación SC-406 incluida en QG-2 debe existir en OpenAPI");
        pathItem.TryGetProperty(expected.Method.ToLowerInvariant(), out var operation).Should().BeTrue(
            $"{operationLabel}: el método SC-406 incluido en QG-2 debe existir en OpenAPI");

        operation.GetProperty("summary").GetString().Should().Be(expected.Summary,
            $"{operationLabel}: debe conservar el Summary contractual de SC-406");
        operation.GetProperty("description").GetString().Should().Be(expected.Description,
            $"{operationLabel}: debe conservar la Description contractual de SC-406");
        GetOperationTags(operation).Should().Equal("Reportes",
            $"{operationLabel}: debe conservar el tag funcional Reportes");
        HasBearerSecurity(operation).Should().BeTrue(
            $"{operationLabel}: las operaciones SC-406 deben conservar el requisito Bearer publicado");

        var controllerMethod = FindReportControllerMethod(expected.ActionName);
        controllerMethod.Should().NotBeNull($"{operationLabel}: debe corresponder a la acción {expected.ActionName}");
        var httpAttributes = controllerMethod!.GetCustomAttributes(inherit: true)
            .OfType<Microsoft.AspNetCore.Mvc.Routing.HttpMethodAttribute>()
            .ToArray();
        httpAttributes.Should().ContainSingle(
            $"{operationLabel}: la acción SC-406 no debe publicar aliases ni verbs adicionales");
        httpAttributes.Single().HttpMethods.Should().ContainSingle().Which.Should().Be(expected.Method,
            $"{operationLabel}: debe conservar el verbo real");
        httpAttributes.Single().Template.Should().Be(expected.ActionRouteTemplate,
            $"{operationLabel}: debe conservar la ruta real");

        var declaredPolicies = controllerMethod.GetCustomAttributes(inherit: true)
            .OfType<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>()
            .Concat(controllerMethod.DeclaringType!.GetCustomAttributes(inherit: true)
                .OfType<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>())
            .Select(attribute => attribute.Policy)
            .Where(policy => !string.IsNullOrWhiteSpace(policy))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        declaredPolicies.Should().ContainSingle(
            $"{operationLabel}: debe existir una única policy contractual no vacía entre acción y controller");
        declaredPolicies.Single().Should().Be("CanReadAch",
            $"{operationLabel}: debe conservar la policy CanReadAch");

        AssertHeaderAbsent(operation, "Authorization");
        AssertHeaderAbsent(operation, "Idempotency-Key");
        AssertHeaderAbsent(operation, "X-Correlation-ID");

        foreach (var parameter in expected.Parameters)
        {
            AssertDocumentedOperationParameter(operation, parameter, operationLabel);
        }

        var expectedContractParameters = expected.Parameters
            .Select(parameter => $"{parameter.Location}:{parameter.Name}")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var actualContractParameters = operation.TryGetProperty("parameters", out var parameters)
            ? parameters.EnumerateArray()
                .Where(parameter => parameter.ValueKind == JsonValueKind.Object
                    && parameter.TryGetProperty("name", out var name)
                    && name.ValueKind == JsonValueKind.String
                    && parameter.TryGetProperty("in", out var location)
                    && location.ValueKind == JsonValueKind.String
                    && (string.Equals(location.GetString(), "path", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(location.GetString(), "query", StringComparison.OrdinalIgnoreCase)))
                .Select(parameter => $"{parameter.GetProperty("in").GetString()}:{parameter.GetProperty("name").GetString()}")
                .ToHashSet(StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        actualContractParameters.Should().BeEquivalentTo(expectedContractParameters,
            $"{operationLabel}: SC-406 debe publicar exactamente sus parámetros path/query reales");

        operation.TryGetProperty("requestBody", out _).Should().BeFalse(
            $"{operationLabel}: las operaciones autorizadas de SC-406 no reciben body");

        operation.TryGetProperty("responses", out var responses).Should().BeTrue(
            $"{operationLabel}: debe publicar responses");
        var expectedStatusCodes = expected.Responses
            .Select(response => response.StatusCode)
            .ToHashSet(StringComparer.Ordinal);
        var actualStatusCodes = responses.EnumerateObject()
            .Select(response => response.Name)
            .ToHashSet(StringComparer.Ordinal);
        actualStatusCodes.Should().BeEquivalentTo(expectedStatusCodes,
            $"{operationLabel}: SC-406 no debe publicar status codes especulativos ni omitir los documentados");

        foreach (var responseExpectation in expected.Responses)
        {
            responses.TryGetProperty(responseExpectation.StatusCode, out var response).Should().BeTrue(
                $"{operationLabel}: debe publicar el status {responseExpectation.StatusCode}");
            response.GetProperty("description").GetString().Should().Be(responseExpectation.Description,
                $"{operationLabel}: el status {responseExpectation.StatusCode} debe conservar su descripción explícita");
            AssertResponseContract(response, schemas, responseExpectation, operationLabel);
        }

        if (expected.PdfFileNameFragment is not null)
        {
            var success = responses.GetProperty("200");
            success.TryGetProperty("content", out var successContent).Should().BeTrue(
                $"{operationLabel}: el PDF exitoso debe publicar content");
            successContent.TryGetProperty("application/pdf", out _).Should().BeTrue(
                $"{operationLabel}: el PDF exitoso debe declarar application/pdf");
            var description = success.GetProperty("description").GetString() ?? string.Empty;
            description.Should().Contain("Content-Disposition",
                $"{operationLabel}: la respuesta PDF debe documentar Content-Disposition");
            description.Should().Contain(expected.PdfFileNameFragment,
                $"{operationLabel}: la respuesta PDF debe documentar el patrón real del filename");
        }
    }

    private static void AssertSc407QG2(
        JsonElement paths,
        JsonElement schemas,
        Sc407Qg2Operation expected)
    {
        var operationLabel = $"{expected.Method} {expected.Path}";
        paths.TryGetProperty(expected.Path, out var pathItem).Should().BeTrue(
            $"{operationLabel}: la operación SC-407 incluida en QG-2 debe existir en OpenAPI");
        pathItem.TryGetProperty(expected.Method.ToLowerInvariant(), out var operation).Should().BeTrue(
            $"{operationLabel}: el método SC-407 incluido en QG-2 debe existir en OpenAPI");

        operation.GetProperty("summary").GetString().Should().Be(expected.Summary,
            $"{operationLabel}: debe conservar el Summary contractual de SC-407");
        operation.GetProperty("description").GetString().Should().Be(expected.Description,
            $"{operationLabel}: debe conservar la Description contractual de SC-407");
        GetOperationTags(operation).Should().Equal("Reportes",
            $"{operationLabel}: debe conservar el tag funcional Reportes");
        HasBearerSecurity(operation).Should().BeTrue(
            $"{operationLabel}: las operaciones SC-407 deben conservar el requisito Bearer publicado");

        var controllerMethod = FindReportControllerMethod(expected.ActionName);
        controllerMethod.Should().NotBeNull($"{operationLabel}: debe corresponder a la acción {expected.ActionName}");
        var httpAttributes = controllerMethod!.GetCustomAttributes(inherit: true)
            .OfType<Microsoft.AspNetCore.Mvc.Routing.HttpMethodAttribute>()
            .ToArray();
        httpAttributes.Should().ContainSingle(
            $"{operationLabel}: la acción SC-407 no debe publicar aliases ni verbs adicionales");
        httpAttributes.Single().HttpMethods.Should().ContainSingle().Which.Should().Be(expected.Method,
            $"{operationLabel}: debe conservar el verbo real");
        httpAttributes.Single().Template.Should().Be(expected.ActionRouteTemplate,
            $"{operationLabel}: debe conservar la ruta real");

        var declaredPolicies = controllerMethod.GetCustomAttributes(inherit: true)
            .OfType<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>()
            .Concat(controllerMethod.DeclaringType!.GetCustomAttributes(inherit: true)
                .OfType<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>())
            .Select(attribute => attribute.Policy)
            .Where(policy => !string.IsNullOrWhiteSpace(policy))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        declaredPolicies.Should().ContainSingle(
            $"{operationLabel}: debe existir una única policy contractual no vacía entre acción y controller");
        declaredPolicies.Single().Should().Be("CanReadAch",
            $"{operationLabel}: debe conservar la policy CanReadAch");

        AssertHeaderAbsent(operation, "Authorization");
        AssertHeaderAbsent(operation, "Idempotency-Key");
        AssertHeaderAbsent(operation, "X-Correlation-ID");

        foreach (var parameter in expected.Parameters)
        {
            AssertDocumentedOperationParameter(operation, parameter, operationLabel);
        }

        var expectedContractParameters = expected.Parameters
            .Select(parameter => $"{parameter.Location}:{parameter.Name}")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var actualContractParameters = operation.TryGetProperty("parameters", out var parameters)
            ? parameters.EnumerateArray()
                .Where(parameter => parameter.ValueKind == JsonValueKind.Object
                    && parameter.TryGetProperty("name", out var name)
                    && name.ValueKind == JsonValueKind.String
                    && parameter.TryGetProperty("in", out var location)
                    && location.ValueKind == JsonValueKind.String
                    && (string.Equals(location.GetString(), "path", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(location.GetString(), "query", StringComparison.OrdinalIgnoreCase)))
                .Select(parameter => $"{parameter.GetProperty("in").GetString()}:{parameter.GetProperty("name").GetString()}")
                .ToHashSet(StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        actualContractParameters.Should().BeEquivalentTo(expectedContractParameters,
            $"{operationLabel}: SC-407 debe publicar exactamente sus parámetros path/query reales");

        if (expected.RequestSchema is null)
        {
            operation.TryGetProperty("requestBody", out _).Should().BeFalse(
                $"{operationLabel}: no debe publicar request body");
        }
        else
        {
            operation.TryGetProperty("requestBody", out var requestBody).Should().BeTrue(
                $"{operationLabel}: debe publicar request body");
            var requestMediaType = GetJsonMediaType(requestBody.GetProperty("content"), operationLabel, "request");
            SchemaResolvesTo(requestMediaType.GetProperty("schema"), schemas, expected.RequestSchema).Should().BeTrue(
                $"{operationLabel}: el request debe usar {expected.RequestSchema}");
            var requestSchema = schemas.GetProperty(expected.RequestSchema);
            foreach (var propertyName in expected.RequestProperties)
            {
                TryGetSchemaProperty(requestSchema, schemas, propertyName, out var property).Should().BeTrue(
                    $"{operationLabel}: {expected.RequestSchema}.{propertyName} debe existir");
                property.TryGetProperty("description", out var propertyDescription).Should().BeTrue(
                    $"{operationLabel}: {expected.RequestSchema}.{propertyName} debe exponer documentación XML");
                propertyDescription.GetString().Should().NotBeNullOrWhiteSpace(
                    $"{operationLabel}: {expected.RequestSchema}.{propertyName} debe tener descripción XML");
            }
        }

        operation.TryGetProperty("responses", out var responses).Should().BeTrue(
            $"{operationLabel}: debe publicar responses");
        var expectedStatusCodes = expected.Responses.Select(response => response.StatusCode).ToHashSet(StringComparer.Ordinal);
        var actualStatusCodes = responses.EnumerateObject().Select(response => response.Name).ToHashSet(StringComparer.Ordinal);
        actualStatusCodes.Should().BeEquivalentTo(expectedStatusCodes,
            $"{operationLabel}: SC-407 no debe publicar status codes especulativos ni omitir los documentados");

        foreach (var responseExpectation in expected.Responses)
        {
            responses.TryGetProperty(responseExpectation.StatusCode, out var response).Should().BeTrue(
                $"{operationLabel}: debe publicar el status {responseExpectation.StatusCode}");
            response.GetProperty("description").GetString().Should().Be(responseExpectation.Description,
                $"{operationLabel}: el status {responseExpectation.StatusCode} debe conservar su descripción explícita");
            AssertResponseContract(response, schemas, responseExpectation, operationLabel);
        }

        if (expected.SuccessMediaTypes is not null)
        {
            var success = responses.GetProperty("200");
            success.TryGetProperty("content", out var successContent).Should().BeTrue(
                $"{operationLabel}: la respuesta de archivo debe publicar content");
            var actualMediaTypes = successContent.EnumerateObject().Select(mediaType => mediaType.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
            actualMediaTypes.Should().BeEquivalentTo(expected.SuccessMediaTypes,
                $"{operationLabel}: debe publicar exclusivamente los media types reales del archivo");
            var successDescription = success.GetProperty("description").GetString() ?? string.Empty;
            successDescription.Should().Contain("Content-Disposition",
                $"{operationLabel}: la respuesta de archivo debe documentar Content-Disposition");
            if (expected.FileNameFragment is not null)
            {
                successDescription.Should().Contain(expected.FileNameFragment,
                    $"{operationLabel}: debe documentar el patrón real del filename");
            }
        }
    }

    private static void AssertSc408QG2(
        JsonElement paths,
        JsonElement schemas,
        Sc408Qg2Operation expected)
    {
        var operationLabel = $"{expected.Method} {expected.Path}";
        paths.TryGetProperty(expected.Path, out var pathItem).Should().BeTrue(
            $"{operationLabel}: la operación SC-408 incluida en QG-2 debe existir en OpenAPI");
        pathItem.TryGetProperty(expected.Method.ToLowerInvariant(), out var operation).Should().BeTrue(
            $"{operationLabel}: el método SC-408 incluido en QG-2 debe existir en OpenAPI");

        operation.GetProperty("summary").GetString().Should().Be(expected.Summary,
            $"{operationLabel}: debe conservar el Summary contractual de SC-408");
        operation.GetProperty("description").GetString().Should().Be(expected.Description,
            $"{operationLabel}: debe conservar la Description contractual de SC-408");
        GetOperationTags(operation).Should().Equal(expected.Tag,
            $"{operationLabel}: debe conservar el tag funcional real del controller");
        HasBearerSecurity(operation).Should().BeTrue(
            $"{operationLabel}: las operaciones SC-408 deben conservar el requisito Bearer publicado");

        var controllerRoutes = expected.ControllerType.GetCustomAttributes(inherit: true)
            .OfType<Microsoft.AspNetCore.Mvc.RouteAttribute>()
            .ToArray();
        controllerRoutes.Should().ContainSingle(
            $"{operationLabel}: el controller no debe adquirir aliases de ruta durante SC-408");

        var controllerMethod = expected.ControllerType.GetMethod(expected.ActionName);
        controllerMethod.Should().NotBeNull($"{operationLabel}: debe corresponder a la acción {expected.ActionName}");
        var httpAttributes = controllerMethod!.GetCustomAttributes(inherit: true)
            .OfType<Microsoft.AspNetCore.Mvc.Routing.HttpMethodAttribute>()
            .ToArray();
        httpAttributes.Should().ContainSingle(
            $"{operationLabel}: la acción SC-408 no debe publicar aliases ni verbs adicionales");
        httpAttributes.Single().HttpMethods.Should().ContainSingle().Which.Should().Be(expected.Method,
            $"{operationLabel}: debe conservar el verbo real");
        httpAttributes.Single().Template.Should().Be(expected.ActionRouteTemplate,
            $"{operationLabel}: debe conservar la plantilla de ruta real, incluidas sus constraints");

        var declaredPolicies = controllerMethod.GetCustomAttributes(inherit: true)
            .OfType<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>()
            .Concat(expected.ControllerType.GetCustomAttributes(inherit: true)
                .OfType<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>())
            .Select(attribute => attribute.Policy)
            .Where(policy => !string.IsNullOrWhiteSpace(policy))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        declaredPolicies.Should().ContainSingle(
            $"{operationLabel}: debe existir una única policy contractual no vacía entre acción y controller");
        declaredPolicies.Single().Should().Be(expected.Policy,
            $"{operationLabel}: debe conservar la policy real");

        AssertHeaderAbsent(operation, "Authorization");
        AssertHeaderAbsent(operation, "Idempotency-Key");
        AssertHeaderAbsent(operation, "X-Correlation-ID");

        foreach (var parameter in expected.Parameters)
        {
            AssertDocumentedOperationParameter(operation, parameter, operationLabel);
        }

        var expectedContractParameters = expected.Parameters
            .Select(parameter => $"{parameter.Location}:{parameter.Name}")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var actualContractParameters = operation.TryGetProperty("parameters", out var parameters)
            ? parameters.EnumerateArray()
                .Where(parameter => parameter.ValueKind == JsonValueKind.Object
                    && parameter.TryGetProperty("name", out var name)
                    && name.ValueKind == JsonValueKind.String
                    && parameter.TryGetProperty("in", out var location)
                    && location.ValueKind == JsonValueKind.String
                    && (string.Equals(location.GetString(), "path", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(location.GetString(), "query", StringComparison.OrdinalIgnoreCase)))
                .Select(parameter => $"{parameter.GetProperty("in").GetString()}:{parameter.GetProperty("name").GetString()}")
                .ToHashSet(StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        actualContractParameters.Should().BeEquivalentTo(expectedContractParameters,
            $"{operationLabel}: SC-408 debe publicar exactamente sus parámetros path/query reales");

        if (expected.RequestSchema is null)
        {
            operation.TryGetProperty("requestBody", out _).Should().BeFalse(
                $"{operationLabel}: la operación sin body no debe publicar requestBody");
        }
        else
        {
            operation.TryGetProperty("requestBody", out var requestBody).Should().BeTrue(
                $"{operationLabel}: debe publicar el requestBody JSON real");
            var requestMediaType = GetJsonMediaType(requestBody.GetProperty("content"), operationLabel, "request");
            SchemaResolvesTo(requestMediaType.GetProperty("schema"), schemas, expected.RequestSchema).Should().BeTrue(
                $"{operationLabel}: el request debe usar {expected.RequestSchema}");
            var requestSchema = schemas.GetProperty(expected.RequestSchema);
            foreach (var propertyName in expected.RequestProperties)
            {
                TryGetSchemaProperty(requestSchema, schemas, propertyName, out var property).Should().BeTrue(
                    $"{operationLabel}: {expected.RequestSchema}.{propertyName} debe existir");
                property.TryGetProperty("description", out var propertyDescription).Should().BeTrue(
                    $"{operationLabel}: {expected.RequestSchema}.{propertyName} debe exponer documentación XML");
                propertyDescription.GetString().Should().NotBeNullOrWhiteSpace(
                    $"{operationLabel}: {expected.RequestSchema}.{propertyName} debe tener descripción XML");
            }

            if (string.Equals(expected.RequestSchema, "SetAchManagedMftCredentialRequest", StringComparison.Ordinal))
            {
                requestMediaType.TryGetProperty("example", out _).Should().BeFalse(
                    $"{operationLabel}: SC-408 no debe publicar example con credenciales");
                requestMediaType.TryGetProperty("examples", out _).Should().BeFalse(
                    $"{operationLabel}: SC-408 no debe publicar examples con credenciales");
            }
        }

        operation.TryGetProperty("responses", out var responses).Should().BeTrue(
            $"{operationLabel}: debe publicar responses");
        var expectedStatusCodes = expected.Responses.Select(response => response.StatusCode).ToHashSet(StringComparer.Ordinal);
        var actualStatusCodes = responses.EnumerateObject().Select(response => response.Name).ToHashSet(StringComparer.Ordinal);
        actualStatusCodes.Should().BeEquivalentTo(expectedStatusCodes,
            $"{operationLabel}: SC-408 no debe publicar status codes especulativos ni omitir los documentados");

        foreach (var responseExpectation in expected.Responses)
        {
            responses.TryGetProperty(responseExpectation.StatusCode, out var response).Should().BeTrue(
                $"{operationLabel}: debe publicar el status {responseExpectation.StatusCode}");
            response.GetProperty("description").GetString().Should().Be(responseExpectation.Description,
                $"{operationLabel}: el status {responseExpectation.StatusCode} debe conservar su descripción explícita");
            AssertResponseContract(response, schemas, responseExpectation, operationLabel);
        }

        if (expected.ResponseSchemaDocumentation is not null)
        {
            AssertSchemaPropertiesDocumented(schemas, expected.ResponseSchemaDocumentation, operationLabel);
        }

        if (string.Equals(expected.RequestSchema, "SetAchManagedMftCredentialRequest", StringComparison.Ordinal))
        {
            var adminSchema = schemas.GetProperty("AchManagedMftAdministrationDto");
            TryGetSchemaProperty(adminSchema, schemas, "secret", out _).Should().BeFalse(
                $"{operationLabel}: la respuesta administrativa no debe exponer el secreto de la credencial");
        }
    }

    private static void AssertSc409QG2(
        JsonElement paths,
        JsonElement schemas,
        Sc409Qg2Operation expected)
    {
        var operationLabel = $"{expected.Method} {expected.Path}";
        paths.TryGetProperty(expected.Path, out var pathItem).Should().BeTrue(
            $"{operationLabel}: la operación SC-409 incluida en QG-2 debe existir en OpenAPI");
        pathItem.TryGetProperty(expected.Method.ToLowerInvariant(), out var operation).Should().BeTrue(
            $"{operationLabel}: el método SC-409 incluido en QG-2 debe existir en OpenAPI");

        operation.GetProperty("summary").GetString().Should().Be(expected.Summary,
            $"{operationLabel}: debe conservar el Summary contractual de SC-409");
        operation.GetProperty("description").GetString().Should().Be(expected.Description,
            $"{operationLabel}: debe conservar la Description contractual de SC-409");
        GetOperationTags(operation).Should().Equal(expected.Tag,
            $"{operationLabel}: debe conservar el tag funcional real del controller");
        HasBearerSecurity(operation).Should().BeTrue(
            $"{operationLabel}: las operaciones SC-409 deben conservar el requisito Bearer publicado");

        var controllerRoutes = expected.ControllerType.GetCustomAttributes(inherit: true)
            .OfType<Microsoft.AspNetCore.Mvc.RouteAttribute>()
            .ToArray();
        controllerRoutes.Should().ContainSingle(
            $"{operationLabel}: el controller no debe adquirir aliases de ruta durante SC-409");

        var controllerMethod = expected.ControllerType.GetMethod(expected.ActionName);
        controllerMethod.Should().NotBeNull($"{operationLabel}: debe corresponder a la acción {expected.ActionName}");
        var httpAttributes = controllerMethod!.GetCustomAttributes(inherit: true)
            .OfType<Microsoft.AspNetCore.Mvc.Routing.HttpMethodAttribute>()
            .ToArray();
        httpAttributes.Should().ContainSingle(
            $"{operationLabel}: la acción SC-409 no debe publicar aliases ni verbs adicionales");
        httpAttributes.Single().HttpMethods.Should().ContainSingle().Which.Should().Be(expected.Method,
            $"{operationLabel}: debe conservar el verbo real");
        httpAttributes.Single().Template.Should().Be(expected.ActionRouteTemplate,
            $"{operationLabel}: debe conservar la plantilla de ruta real, incluidas sus constraints");

        var declaredPolicies = controllerMethod.GetCustomAttributes(inherit: true)
            .OfType<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>()
            .Concat(expected.ControllerType.GetCustomAttributes(inherit: true)
                .OfType<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>())
            .Select(attribute => attribute.Policy)
            .Where(policy => !string.IsNullOrWhiteSpace(policy))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        declaredPolicies.Should().ContainSingle(
            $"{operationLabel}: debe existir una única policy contractual no vacía entre acción y controller");
        declaredPolicies.Single().Should().Be(expected.Policy,
            $"{operationLabel}: debe conservar la policy real");

        AssertHeaderAbsent(operation, "Authorization");
        AssertHeaderAbsent(operation, "Idempotency-Key");
        AssertHeaderAbsent(operation, "X-Correlation-ID");

        foreach (var parameter in expected.Parameters)
        {
            AssertDocumentedOperationParameter(operation, parameter, operationLabel);
        }

        var expectedContractParameters = expected.Parameters
            .Select(parameter => $"{parameter.Location}:{parameter.Name}")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var actualContractParameters = operation.TryGetProperty("parameters", out var parameters)
            ? parameters.EnumerateArray()
                .Where(parameter => parameter.ValueKind == JsonValueKind.Object
                    && parameter.TryGetProperty("name", out var name)
                    && name.ValueKind == JsonValueKind.String
                    && parameter.TryGetProperty("in", out var location)
                    && location.ValueKind == JsonValueKind.String
                    && (string.Equals(location.GetString(), "path", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(location.GetString(), "query", StringComparison.OrdinalIgnoreCase)))
                .Select(parameter => $"{parameter.GetProperty("in").GetString()}:{parameter.GetProperty("name").GetString()}")
                .ToHashSet(StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        actualContractParameters.Should().BeEquivalentTo(expectedContractParameters,
            $"{operationLabel}: SC-409 debe publicar exactamente sus parámetros path/query reales");

        operation.TryGetProperty("requestBody", out _).Should().BeFalse(
            $"{operationLabel}: las operaciones GET SC-409 no deben publicar requestBody");

        operation.TryGetProperty("responses", out var responses).Should().BeTrue(
            $"{operationLabel}: debe publicar responses");
        var expectedStatusCodes = expected.Responses.Select(response => response.StatusCode).ToHashSet(StringComparer.Ordinal);
        var actualStatusCodes = responses.EnumerateObject().Select(response => response.Name).ToHashSet(StringComparer.Ordinal);
        actualStatusCodes.Should().BeEquivalentTo(expectedStatusCodes,
            $"{operationLabel}: SC-409 no debe publicar status codes especulativos ni omitir los documentados");

        foreach (var responseExpectation in expected.Responses)
        {
            responses.TryGetProperty(responseExpectation.StatusCode, out var response).Should().BeTrue(
                $"{operationLabel}: debe publicar el status {responseExpectation.StatusCode}");
            response.GetProperty("description").GetString().Should().Be(responseExpectation.Description,
                $"{operationLabel}: el status {responseExpectation.StatusCode} debe conservar su descripción explícita");
            AssertResponseContract(response, schemas, responseExpectation, operationLabel);
        }

        if (expected.ResponseSchemaDocumentation is not null)
        {
            AssertSchemaPropertiesDocumented(schemas, expected.ResponseSchemaDocumentation, operationLabel);
        }
    }

    private static void AssertSc410ControllerQG2(
        JsonElement paths,
        JsonElement schemas,
        Sc410ControllerQg2Operation expected)
    {
        var operationLabel = $"{expected.Method} {expected.Path}";
        paths.TryGetProperty(expected.Path, out var pathItem).Should().BeTrue(
            $"{operationLabel}: la operación SC-410 incluida en QG-2 debe existir en OpenAPI");
        pathItem.TryGetProperty(expected.Method.ToLowerInvariant(), out var operation).Should().BeTrue(
            $"{operationLabel}: el método SC-410 incluido en QG-2 debe existir en OpenAPI");

        operation.GetProperty("summary").GetString().Should().Be(expected.Summary,
            $"{operationLabel}: debe conservar el Summary contractual de SC-410");
        operation.GetProperty("description").GetString().Should().Be(expected.Description,
            $"{operationLabel}: debe conservar la Description contractual de SC-410");
        GetOperationTags(operation).Should().Equal("Auditoría y observabilidad",
            $"{operationLabel}: debe conservar el tag funcional aprobado para auditoría y observabilidad");
        HasBearerSecurity(operation).Should().BeTrue(
            $"{operationLabel}: las acciones controller SC-410 están protegidas por Authorize y deben publicar Bearer");

        var controllerRoutes = expected.ControllerType.GetCustomAttributes(inherit: true)
            .OfType<Microsoft.AspNetCore.Mvc.RouteAttribute>()
            .ToArray();
        controllerRoutes.Should().ContainSingle(
            $"{operationLabel}: el controller no debe adquirir aliases de ruta durante SC-410");

        var controllerMethod = expected.ControllerType.GetMethod(expected.ActionName);
        controllerMethod.Should().NotBeNull($"{operationLabel}: debe corresponder a la acción {expected.ActionName}");
        var httpAttributes = controllerMethod!.GetCustomAttributes(inherit: true)
            .OfType<Microsoft.AspNetCore.Mvc.Routing.HttpMethodAttribute>()
            .ToArray();
        httpAttributes.Should().ContainSingle(
            $"{operationLabel}: la acción SC-410 no debe publicar aliases ni verbs adicionales");
        httpAttributes.Single().HttpMethods.Should().ContainSingle().Which.Should().Be(expected.Method,
            $"{operationLabel}: debe conservar el verbo real");
        httpAttributes.Single().Template.Should().Be(expected.ActionRouteTemplate,
            $"{operationLabel}: debe conservar la plantilla de ruta real");

        var authorizeAttributes = controllerMethod.GetCustomAttributes(inherit: true)
            .OfType<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>()
            .Concat(expected.ControllerType.GetCustomAttributes(inherit: true)
                .OfType<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>())
            .ToArray();
        authorizeAttributes.Should().NotBeEmpty(
            $"{operationLabel}: debe conservar autorización en acción o controller");
        var declaredPolicies = authorizeAttributes
            .Select(attribute => attribute.Policy)
            .Where(policy => !string.IsNullOrWhiteSpace(policy))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (expected.NominalPolicy is null)
        {
            declaredPolicies.Should().BeEmpty(
                $"{operationLabel}: no debe inventar una policy nominal cuando solo existe Authorize");
        }
        else
        {
            declaredPolicies.Should().ContainSingle().Which.Should().Be(expected.NominalPolicy,
                $"{operationLabel}: debe conservar la policy nominal real");
        }

        AssertHeaderAbsent(operation, "Authorization");
        AssertHeaderAbsent(operation, "Idempotency-Key");
        AssertHeaderAbsent(operation, "X-Correlation-ID");

        foreach (var parameter in expected.Parameters)
        {
            AssertDocumentedOperationParameter(operation, parameter, operationLabel);
        }

        var expectedContractParameters = expected.Parameters
            .Select(parameter => $"{parameter.Location}:{parameter.Name}")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var actualContractParameters = operation.TryGetProperty("parameters", out var parameters)
            ? parameters.EnumerateArray()
                .Where(parameter => parameter.ValueKind == JsonValueKind.Object
                    && parameter.TryGetProperty("name", out var name)
                    && name.ValueKind == JsonValueKind.String
                    && parameter.TryGetProperty("in", out var location)
                    && location.ValueKind == JsonValueKind.String
                    && (string.Equals(location.GetString(), "path", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(location.GetString(), "query", StringComparison.OrdinalIgnoreCase)))
                .Select(parameter => $"{parameter.GetProperty("in").GetString()}:{parameter.GetProperty("name").GetString()}")
                .ToHashSet(StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        actualContractParameters.Should().BeEquivalentTo(expectedContractParameters,
            $"{operationLabel}: SC-410 debe publicar exactamente sus parámetros path/query reales");

        if (expected.RequestSchema is null)
        {
            operation.TryGetProperty("requestBody", out _).Should().BeFalse(
                $"{operationLabel}: no debe publicar request body");
        }
        else
        {
            operation.TryGetProperty("requestBody", out var requestBody).Should().BeTrue(
                $"{operationLabel}: debe publicar request body");
            var requestMediaType = GetJsonMediaType(requestBody.GetProperty("content"), operationLabel, "request");
            SchemaResolvesTo(requestMediaType.GetProperty("schema"), schemas, expected.RequestSchema).Should().BeTrue(
                $"{operationLabel}: el request debe usar {expected.RequestSchema}");

            var requestSchema = schemas.GetProperty(expected.RequestSchema);
            foreach (var propertyName in expected.RequestProperties)
            {
                TryGetSchemaProperty(requestSchema, schemas, propertyName, out var property).Should().BeTrue(
                    $"{operationLabel}: {expected.RequestSchema}.{propertyName} debe existir");
                property.TryGetProperty("description", out var propertyDescription).Should().BeTrue(
                    $"{operationLabel}: {expected.RequestSchema}.{propertyName} debe exponer documentación XML");
                propertyDescription.GetString().Should().NotBeNullOrWhiteSpace(
                    $"{operationLabel}: {expected.RequestSchema}.{propertyName} debe tener descripción XML");
            }
        }

        operation.TryGetProperty("responses", out var responses).Should().BeTrue(
            $"{operationLabel}: debe publicar responses");
        var expectedStatusCodes = expected.Responses.Select(response => response.StatusCode).ToHashSet(StringComparer.Ordinal);
        var actualStatusCodes = responses.EnumerateObject().Select(response => response.Name).ToHashSet(StringComparer.Ordinal);
        actualStatusCodes.Should().BeEquivalentTo(expectedStatusCodes,
            $"{operationLabel}: SC-410 no debe publicar status codes especulativos ni omitir los documentados");

        foreach (var responseExpectation in expected.Responses)
        {
            responses.TryGetProperty(responseExpectation.StatusCode, out var response).Should().BeTrue(
                $"{operationLabel}: debe publicar el status {responseExpectation.StatusCode}");
            response.GetProperty("description").GetString().Should().Be(responseExpectation.Description,
                $"{operationLabel}: el status {responseExpectation.StatusCode} debe conservar su descripción explícita");
            AssertResponseContract(response, schemas, responseExpectation, operationLabel);
        }
    }

    private static void AssertSc410HealthQG2(
        JsonElement paths,
        Sc410HealthQg2Operation expected)
    {
        var operationLabel = $"{expected.Method} {expected.Path}";
        paths.TryGetProperty(expected.Path, out var pathItem).Should().BeTrue(
            $"{operationLabel}: la ruta health autorizada debe existir en OpenAPI");
        pathItem.TryGetProperty(expected.Method.ToLowerInvariant(), out var operation).Should().BeTrue(
            $"{operationLabel}: el método health autorizado debe existir en OpenAPI");

        operation.GetProperty("summary").GetString().Should().Be(expected.Summary,
            $"{operationLabel}: debe conservar el Summary documental de health");
        operation.GetProperty("description").GetString().Should().Be(expected.Description,
            $"{operationLabel}: debe conservar la Description documental de health");
        GetOperationTags(operation).Should().Equal("Auditoría y observabilidad",
            $"{operationLabel}: health debe usar el tag de auditoría y observabilidad");
        HasBearerSecurity(operation).Should().BeFalse(
            $"{operationLabel}: health permanece anónimo");
        AssertHeaderAbsent(operation, "Authorization");
        AssertHeaderAbsent(operation, "Idempotency-Key");
        AssertHeaderAbsent(operation, "X-Correlation-ID");

        if (operation.TryGetProperty("parameters", out var parameters))
        {
            parameters.EnumerateArray().Should().BeEmpty(
                $"{operationLabel}: health no expone parámetros contractuales");
        }
        operation.TryGetProperty("requestBody", out _).Should().BeFalse(
            $"{operationLabel}: health no recibe request body");

        operation.TryGetProperty("responses", out var responses).Should().BeTrue(
            $"{operationLabel}: health debe publicar responses");
        var actualStatusCodes = responses.EnumerateObject().Select(response => response.Name).ToHashSet(StringComparer.Ordinal);
        actualStatusCodes.Should().BeEquivalentTo(expected.StatusCodes,
            $"{operationLabel}: health debe publicar exclusivamente sus status codes reales");

        foreach (var statusCode in expected.StatusCodes)
        {
            var response = responses.GetProperty(statusCode);
            response.GetProperty("description").GetString().Should().NotBeNullOrWhiteSpace(
                $"{operationLabel}: response {statusCode} debe tener descripción OpenAPI");
            response.TryGetProperty("content", out var content).Should().BeTrue(
                $"{operationLabel}: response {statusCode} debe publicar el media type JSON real");
            var mediaTypes = content.EnumerateObject().Select(mediaType => mediaType.Name).ToArray();
            mediaTypes.Should().Equal("application/json",
                $"{operationLabel}: response {statusCode} no debe inventar media types adicionales");
            var mediaType = content.GetProperty("application/json");
            if (mediaType.TryGetProperty("schema", out var schema)
                && schema.ValueKind == JsonValueKind.Object)
            {
                schema.TryGetProperty("$ref", out _).Should().BeFalse(
                    $"{operationLabel}: el body anónimo de health no debe convertirse en un DTO/schema nombrado artificial");
            }
        }
    }

    private static void AssertTransactionPoliciesQG2(
        JsonElement paths,
        JsonElement schemas,
        TransactionPoliciesQg2Operation expected)
    {
        var operationLabel = $"{expected.Method} {expected.Path}";
        paths.TryGetProperty(expected.Path, out var pathItem).Should().BeTrue(
            $"{operationLabel}: la operación incluida en QG-2 debe existir en OpenAPI");
        pathItem.TryGetProperty(expected.Method.ToLowerInvariant(), out var operation).Should().BeTrue(
            $"{operationLabel}: el método incluido en QG-2 debe existir en OpenAPI");

        GetOperationTags(operation).Should().Equal("Políticas y reglas transaccionales",
            $"{operationLabel}: debe conservar el tag funcional aprobado para políticas y reglas transaccionales");
        HasBearerSecurity(operation).Should().BeTrue(
            $"{operationLabel}: las operaciones de políticas y reglas transaccionales incluidas en QG-2 deben conservar el requisito Bearer publicado");

        AssertHeaderAbsent(operation, "Authorization");
        AssertHeaderAbsent(operation, "Idempotency-Key");
        AssertHeaderAbsent(operation, "X-Correlation-ID");

        foreach (var parameter in expected.Parameters)
        {
            AssertDocumentedOperationParameter(operation, parameter, operationLabel);
        }

        if (expected.RequestSchema is null)
        {
            operation.TryGetProperty("requestBody", out _).Should().BeFalse(
                $"{operationLabel}: no debe publicar request body");
        }
        else
        {
            operation.TryGetProperty("requestBody", out var requestBody).Should().BeTrue(
                $"{operationLabel}: debe publicar request body");
            var requestMediaType = GetJsonMediaType(requestBody.GetProperty("content"), operationLabel, "request");
            GetSchemaReferenceName(requestMediaType.GetProperty("schema")).Should().Be(expected.RequestSchema,
                $"{operationLabel}: el request debe usar el contrato {expected.RequestSchema}");

            var requestSchema = schemas.GetProperty(expected.RequestSchema);
            foreach (var propertyName in expected.RequestProperties)
            {
                TryGetSchemaProperty(requestSchema, schemas, propertyName, out var property).Should().BeTrue(
                    $"{operationLabel}: el schema {expected.RequestSchema} debe publicar la propiedad {propertyName}");
                property.TryGetProperty("description", out var propertyDescription).Should().BeTrue(
                    $"{operationLabel}: {expected.RequestSchema}.{propertyName} debe exponer documentación XML");
                propertyDescription.GetString().Should().NotBeNullOrWhiteSpace(
                    $"{operationLabel}: {expected.RequestSchema}.{propertyName} debe tener descripción XML");
            }
        }

        operation.TryGetProperty("responses", out var responses).Should().BeTrue(
            $"{operationLabel}: debe publicar responses");
        responses.TryGetProperty(expected.PrimaryStatusCode, out _).Should().BeTrue(
            $"{operationLabel}: debe publicar su response principal {expected.PrimaryStatusCode}");

        foreach (var responseExpectation in expected.Responses)
        {
            responses.TryGetProperty(responseExpectation.StatusCode, out var response).Should().BeTrue(
                $"{operationLabel}: debe publicar el status {responseExpectation.StatusCode}");
            response.GetProperty("description").GetString().Should().Be(responseExpectation.Description,
                $"{operationLabel}: el status {responseExpectation.StatusCode} debe conservar su descripción explícita");

            AssertResponseContract(response, schemas, responseExpectation, operationLabel);
        }

        if (expected.ResponseSchemaDocumentation is not null)
        {
            AssertSchemaPropertiesDocumented(schemas, expected.ResponseSchemaDocumentation, operationLabel);
        }
    }

    private static void AssertResponseStatusesAbsent(
        JsonElement paths,
        string method,
        string path,
        IReadOnlyCollection<string> statusCodes)
    {
        var operationLabel = $"{method.ToUpperInvariant()} {path}";
        var operation = paths.GetProperty(path).GetProperty(method.ToLowerInvariant());
        operation.TryGetProperty("responses", out var responses).Should().BeTrue(
            $"{operationLabel}: debe publicar responses");

        foreach (var statusCode in statusCodes)
        {
            responses.TryGetProperty(statusCode, out _).Should().BeFalse(
                $"{operationLabel}: no debe conservar el status especulativo {statusCode}");
        }
    }

    private static void AssertSchemaPropertiesDocumented(
        JsonElement schemas,
        Qg2SchemaDocumentation expected,
        string operationLabel)
    {
        schemas.TryGetProperty(expected.SchemaName, out var schema).Should().BeTrue(
            $"{operationLabel}: debe publicar el schema {expected.SchemaName}");

        foreach (var propertyName in expected.Properties)
        {
            TryGetSchemaProperty(schema, schemas, propertyName, out var property).Should().BeTrue(
                $"{operationLabel}: el schema {expected.SchemaName} debe publicar la propiedad {propertyName}");
            property.TryGetProperty("description", out var propertyDescription).Should().BeTrue(
                $"{operationLabel}: {expected.SchemaName}.{propertyName} debe exponer documentación XML");
            propertyDescription.GetString().Should().NotBeNullOrWhiteSpace(
                $"{operationLabel}: {expected.SchemaName}.{propertyName} debe tener descripción XML");
        }
    }

    private static void AssertSc506SchemaDocumentationAudit(JsonElement schemas)
    {
        AssertSchemaPropertiesDocumented(
            schemas,
            new Qg2SchemaDocumentation(
                "AchTransaction",
                [
                    "createdAt", "updatedAt", "serviceClassCode", "originatingDFI", "receivingDFI",
                    "traceSequenceNumber", "addendaRecordIndicator", "origin", "monetaryIntegrationRoute",
                    "classificationStatus", "sourceInstitutionWasDefaultAtCreation", "classifiedAtUtc",
                    "classificationVersion", "stateChangedAtUtc", "slaDeadlineAtUtc",
                    "contrapartidasResponseCode", "returnReasonCode", "originalTraceRef", "discretionaryData"
                ]),
            "SC-506 AchTransaction");

        AssertSchemaPropertiesDocumented(
            schemas,
            new Qg2SchemaDocumentation(
                "NachaValidationFailure",
                ["recordType", "batchNumber", "entrySequence", "transactionCode", "reason"]),
            "SC-506 NachaValidationFailure");
    }

    private static readonly HashSet<(string Method, string Path)> Qg2IdempotencyHeaderOperations =
    [
        ("POST", "/api/ach-colombia/file-exchange/outbound/execute"),
        ("POST", "/api/ach-colombia/file-exchange/inbound/execute"),
        ("POST", "/api/ach-colombia/file-exchange/transfers/{id}/retry")
    ];

    private static readonly HashSet<(string Method, string Path)> Qg2CorrelationHeaderOperations =
    [
        ("POST", "/api/ach/response-status-mappings"),
        ("PUT", "/api/ach/response-status-mappings/{id}"),
        ("POST", "/api/ach/response-status-mappings/{id}/activate"),
        ("POST", "/api/ach/response-status-mappings/{id}/deactivate"),
        ("POST", "/api/ach/responses/{id}/orphan"),
        ("POST", "/api/ach/responses/orphans/{id}/review/start"),
        ("POST", "/api/ach/responses/orphans/{id}/resolve"),
        ("POST", "/api/ach/responses/{id}/reprocess"),
        ("POST", "/api/scheduler/tasks/{taskCode}/pause"),
        ("POST", "/api/scheduler/tasks/{taskCode}/resume"),
        ("PUT", "/api/scheduler/tasks/{taskCode}/schedule")
    ];

    private static void AssertQg2ResponseStatusAudit(JsonElement paths)
    {
        var expectedResponses = new Dictionary<(string Method, string Path), HashSet<string>>();

        void Add(string method, string path, IEnumerable<string> statusCodes)
        {
            expectedResponses.Add(
                (method, path),
                statusCodes.ToHashSet(StringComparer.Ordinal));
        }

        foreach (var operation in IncomingNachaQg2Operations)
            Add(operation.Method, operation.Path, operation.Responses.Select(response => response.StatusCode));
        foreach (var operation in NachaLegacyQg2Operations)
            Add(operation.Method, operation.Path, operation.Responses.Select(response => response.StatusCode));
        foreach (var operation in AuthQg2Operations)
            Add(operation.Method, operation.Path, operation.Responses.Select(response => response.StatusCode));
        foreach (var operation in ChambersCyclesQg2Operations)
            Add(operation.Method, operation.Path, operation.Responses.Select(response => response.StatusCode));
        foreach (var operation in TransactionsQg2Operations)
            Add(operation.Method, operation.Path, operation.Responses.Select(response => response.StatusCode));
        foreach (var operation in BulkIngestionQg2Operations)
            Add(operation.Method, operation.Path, operation.Responses.Select(response => response.StatusCode));
        foreach (var operation in TraceabilityQg2Operations)
            Add(operation.Method, operation.Path, operation.Responses.Select(response => response.StatusCode));
        foreach (var operation in ReturnsQg2Operations)
            Add(operation.Method, operation.Path, operation.Responses.Select(response => response.StatusCode));
        foreach (var operation in ResponseOperationsQg2Operations)
            Add(operation.Method, operation.Path, operation.Responses.Select(response => response.StatusCode));
        foreach (var operation in CenitOperationsQg2Operations)
            Add(operation.Method, operation.Path, operation.Responses.Select(response => response.StatusCode));
        foreach (var operation in CertificateQg2Operations)
            Add(operation.Method, operation.Path, operation.Responses.Select(response => response.StatusCode));
        foreach (var operation in DigitalEnvelopeQg2Operations)
            Add(operation.Method, operation.Path, operation.Responses.Select(response => response.StatusCode));
        foreach (var operation in NachaSecurityQg2Operations)
            Add(operation.Method, operation.Path, operation.Responses.Select(response => response.StatusCode));
        foreach (var operation in Sc309Qg2Operations)
            Add(operation.Method, operation.Path, operation.Responses.Select(response => response.StatusCode));
        foreach (var operation in Sc401Qg2Operations)
            Add(operation.Method, operation.Path, operation.Responses.Select(response => response.StatusCode));
        foreach (var operation in Sc402Qg2Operations)
            Add(operation.Method, operation.Path, operation.Responses.Select(response => response.StatusCode));
        foreach (var operation in Sc403Qg2Operations)
            Add(operation.Method, operation.Path, operation.Responses.Select(response => response.StatusCode));
        foreach (var operation in Sc404Qg2Operations)
            Add(operation.Method, operation.Path, operation.Responses.Select(response => response.StatusCode));
        foreach (var operation in Sc405Qg2Operations)
            Add(operation.Method, operation.Path, operation.Responses.Select(response => response.StatusCode));
        foreach (var operation in Sc406Qg2Operations)
            Add(operation.Method, operation.Path, operation.Responses.Select(response => response.StatusCode));
        foreach (var operation in Sc407Qg2Operations)
            Add(operation.Method, operation.Path, operation.Responses.Select(response => response.StatusCode));
        foreach (var operation in Sc408Qg2Operations)
            Add(operation.Method, operation.Path, operation.Responses.Select(response => response.StatusCode));
        foreach (var operation in Sc409Qg2Operations)
            Add(operation.Method, operation.Path, operation.Responses.Select(response => response.StatusCode));
        foreach (var operation in Sc410ControllerQg2Operations)
            Add(operation.Method, operation.Path, operation.Responses.Select(response => response.StatusCode));
        foreach (var operation in Sc410HealthQg2Operations)
            Add(operation.Method, operation.Path, operation.StatusCodes);
        foreach (var operation in TransactionPoliciesQg2Operations)
            Add(operation.Method, operation.Path, operation.Responses.Select(response => response.StatusCode));

        expectedResponses.Should().HaveCount(245,
            "UX3 debe auditar responses de las 245 operaciones contractuales QG");
        expectedResponses.Keys.Should().BeEquivalentTo(BaselineQgOperations,
            "UX3 conserva el baseline ampliado a 245 operaciones");

        var auditedOperationStatusPairs = 0;
        foreach (var expectation in expectedResponses)
        {
            var operationLabel = $"{expectation.Key.Method} {expectation.Key.Path}";
            paths.TryGetProperty(expectation.Key.Path, out var pathItem).Should().BeTrue(
                $"{operationLabel}: la operación auditada por SC-504 debe existir en OpenAPI");
            pathItem.TryGetProperty(expectation.Key.Method.ToLowerInvariant(), out var operation).Should().BeTrue(
                $"{operationLabel}: el método auditado por SC-504 debe existir en OpenAPI");
            operation.TryGetProperty("responses", out var responses).Should().BeTrue(
                $"{operationLabel}: debe publicar responses");

            var actualStatusCodes = responses.EnumerateObject()
                .Select(response => response.Name)
                .ToHashSet(StringComparer.Ordinal);
            actualStatusCodes.Should().BeEquivalentTo(expectation.Value,
                $"{operationLabel}: SC-504 debe impedir status faltantes o metadata especulativa");

            foreach (var statusCode in expectation.Value)
            {
                var response = responses.GetProperty(statusCode);
                response.TryGetProperty("description", out var description).Should().BeTrue(
                    $"{operationLabel}: response {statusCode} debe publicar descripción");
                description.GetString().Should().NotBeNullOrWhiteSpace(
                    $"{operationLabel}: response {statusCode} debe conservar una descripción contractual útil");
                auditedOperationStatusPairs++;
            }
        }

        auditedOperationStatusPairs.Should().Be(481,
            "UX1 añade un 200 de catálogos operativos y 200/404 de workspace al contrato vigente");
    }

    private static void AssertQg2ParameterDocumentation(JsonElement paths)
    {
        var totals = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["path"] = 0,
            ["query"] = 0,
            ["header"] = 0
        };

        foreach (var expectedOperation in BaselineQgOperations)
        {
            var operationLabel = $"{expectedOperation.Method} {expectedOperation.Path}";
            paths.TryGetProperty(expectedOperation.Path, out var pathItem).Should().BeTrue(
                $"{operationLabel}: SC-503 debe continuar auditando el baseline contractual ampliado por UX1");
            pathItem.TryGetProperty(expectedOperation.Method.ToLowerInvariant(), out var operation).Should().BeTrue(
                $"{operationLabel}: la operación contractual debe existir para auditar sus parámetros");

            if (!operation.TryGetProperty("parameters", out var parameters) || parameters.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var parameter in parameters.EnumerateArray())
            {
                parameter.ValueKind.Should().Be(JsonValueKind.Object,
                    $"{operationLabel}: cada parámetro debe serializarse como objeto OpenAPI");

                var name = GetParameterName(parameter);
                name.Should().NotBeNullOrWhiteSpace($"{operationLabel}: cada parámetro debe tener nombre");
                parameter.TryGetProperty("in", out var locationElement).Should().BeTrue(
                    $"{operationLabel}: {name} debe indicar su ubicación HTTP");
                var location = locationElement.GetString();
                new[] { "path", "query", "header" }.Should().Contain(location,
                    $"{operationLabel}: SC-503 audita únicamente parámetros path/query/header reales");

                seen.Add($"{location}:{name}").Should().BeTrue(
                    $"{operationLabel}: {location}:{name} no debe publicarse duplicado");
                totals[location!]++;

                parameter.TryGetProperty("description", out var descriptionElement).Should().BeTrue(
                    $"{operationLabel}: {location}:{name} debe tener descripción contractual");
                var description = descriptionElement.GetString();
                description.Should().NotBeNullOrWhiteSpace(
                    $"{operationLabel}: {location}:{name} no puede tener descripción vacía");
                IsObviouslyTautologicalParameterDescription(name!, description!).Should().BeFalse(
                    $"{operationLabel}: {location}:{name} debe aportar semántica adicional al nombre");

                parameter.TryGetProperty("schema", out var schema).Should().BeTrue(
                    $"{operationLabel}: {location}:{name} debe conservar su schema OpenAPI");
                schema.ValueKind.Should().Be(JsonValueKind.Object,
                    $"{operationLabel}: {location}:{name} debe serializar un schema válido");

                if (string.Equals(location, "path", StringComparison.OrdinalIgnoreCase))
                {
                    parameter.TryGetProperty("required", out var required).Should().BeTrue(
                        $"{operationLabel}: el parámetro path {name} debe declarar required");
                    required.GetBoolean().Should().BeTrue(
                        $"{operationLabel}: el parámetro path {name} es obligatorio por contrato de routing");
                }

                if (!string.Equals(location, "header", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string.Equals(name, "Authorization", StringComparison.OrdinalIgnoreCase).Should().BeFalse(
                    $"{operationLabel}: Bearer debe modelarse como OpenAPI Security y no como parámetro Authorization");

                if (string.Equals(name, "Idempotency-Key", StringComparison.OrdinalIgnoreCase))
                {
                    Qg2IdempotencyHeaderOperations.Should().Contain(expectedOperation,
                        $"{operationLabel}: Idempotency-Key solo puede publicarse donde el código consume ese header");
                }
                else if (string.Equals(name, "X-Correlation-ID", StringComparison.OrdinalIgnoreCase))
                {
                    Qg2CorrelationHeaderOperations.Should().Contain(expectedOperation,
                        $"{operationLabel}: X-Correlation-ID solo puede publicarse donde el código consume ese header");
                }
                else
                {
                    false.Should().BeTrue(
                        $"{operationLabel}: el header {name} no pertenece al contrato explícito auditado por SC-503");
                }
            }
        }

        totals["path"].Should().Be(122, "UX3 añade id y fieldId del editor contextual");
        totals["query"].Should().Be(300, "UX1 añade camaraCode y flujoCode a catálogos operativos");
        totals["header"].Should().Be(14, "SC-503 inventarió 14 headers explícitos realmente consumidos por operaciones QG");
        totals.Values.Sum().Should().Be(436, "UX3 eleva el inventario contractual a 436 parámetros OpenAPI");
    }

    private static bool IsObviouslyTautologicalParameterDescription(string name, string description)
    {
        static string Normalize(string value)
            => string.Concat(value.Where(char.IsLetterOrDigit)).ToLowerInvariant();

        var normalizedName = Normalize(name);
        var normalizedDescription = Normalize(description);
        if (normalizedDescription == normalizedName)
        {
            return true;
        }

        if (normalizedDescription is "id" or "identificador" or "identifier"
            or "filtro" or "filter" or "valor" or "value"
            or "parametro" or "parámetro" or "parameter")
        {
            return true;
        }

        return normalizedDescription == $"parameter{normalizedName}"
            || normalizedDescription == $"parametro{normalizedName}"
            || normalizedDescription == $"filtro{normalizedName}"
            || normalizedDescription == $"filter{normalizedName}"
            || normalizedDescription == $"valor{normalizedName}"
            || normalizedDescription == $"value{normalizedName}";
    }

    private static void AssertDocumentedOperationParameter(
        JsonElement operation,
        Qg2Parameter expected,
        string operationLabel)
    {
        operation.TryGetProperty("parameters", out var parameters).Should().BeTrue(
            $"{operationLabel}: debe publicar el parámetro {expected.Name}");

        var matches = parameters.EnumerateArray()
            .Where(parameter => parameter.ValueKind == JsonValueKind.Object
                && parameter.TryGetProperty("name", out var name)
                && name.ValueKind == JsonValueKind.String
                && string.Equals(name.GetString(), expected.Name, StringComparison.OrdinalIgnoreCase)
                && parameter.TryGetProperty("in", out var location)
                && location.ValueKind == JsonValueKind.String
                && string.Equals(location.GetString(), expected.Location, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        matches.Should().ContainSingle(
            $"{operationLabel}: {expected.Name} debe publicarse una sola vez como parámetro {expected.Location}");
        matches[0].TryGetProperty("description", out var description).Should().BeTrue(
            $"{operationLabel}: {expected.Name} debe tener descripción XML");
        description.GetString().Should().NotBeNullOrWhiteSpace(
            $"{operationLabel}: {expected.Name} debe tener una descripción útil");

        if (expected.Required.HasValue)
        {
            if (matches[0].TryGetProperty("required", out var required))
            {
                required.GetBoolean().Should().Be(expected.Required.Value,
                    $"{operationLabel}: {expected.Name} debe conservar su obligatoriedad contractual");
            }
            else
            {
                expected.Required.Value.Should().BeFalse(
                    $"{operationLabel}: omitir required solo es válido para un parámetro opcional");
            }
        }
    }

    private static void AssertResponseContract(
        JsonElement response,
        JsonElement schemas,
        Qg2Response expected,
        string operationLabel)
    {
        var responseLabel = $"{operationLabel} response {expected.StatusCode}";
        if (expected.Shape == Qg2ResponseShape.NoBody)
        {
            if (response.TryGetProperty("content", out var noBodyContent))
            {
                noBodyContent.ValueKind.Should().Be(JsonValueKind.Object);
                noBodyContent.EnumerateObject().Should().BeEmpty(
                    $"{responseLabel}: una respuesta sin cuerpo no debe inventar content types ni schemas");
            }

            return;
        }

        if (expected.Shape == Qg2ResponseShape.Untyped)
        {
            if (response.TryGetProperty("content", out var untypedContent) && untypedContent.ValueKind == JsonValueKind.Object)
            {
                foreach (var mediaType in untypedContent.EnumerateObject())
                {
                    if (mediaType.Value.ValueKind == JsonValueKind.Object
                        && mediaType.Value.TryGetProperty("schema", out var untypedSchema))
                    {
                        SchemaResolvesTo(untypedSchema, schemas, "ProblemDetails").Should().BeFalse(
                            $"{responseLabel}: QG-2 no debe convertir el body heterogéneo actual en ProblemDetails");
                    }
                }
            }

            return;
        }

        var schema = GetFirstResponseSchema(response, responseLabel);
        expected.SchemaName.Should().NotBeNullOrWhiteSpace();

        switch (expected.Shape)
        {
            case Qg2ResponseShape.Schema:
                SchemaResolvesTo(schema, schemas, expected.SchemaName!).Should().BeTrue(
                    $"{responseLabel}: debe usar el schema {expected.SchemaName}");
                break;
            case Qg2ResponseShape.Collection:
                AssertCollectionSchema(schema, schemas, expected.SchemaName!, responseLabel);
                break;
            case Qg2ResponseShape.Paginated:
                AssertPaginatedSchema(schema, schemas, expected.SchemaName!, responseLabel);
                break;
            case Qg2ResponseShape.ProblemDetails:
                SchemaResolvesTo(schema, schemas, "ProblemDetails").Should().BeTrue(
                    $"{responseLabel}: solo este status documentado debe usar ProblemDetails");
                break;
            case Qg2ResponseShape.ValidationProblemDetails:
                AssertValidationProblemDetailsSchema(schema, schemas, responseLabel);
                break;
            case Qg2ResponseShape.AchResponsePaged:
                AssertAchResponsePagedSchema(schema, schemas, expected.SchemaName!, responseLabel);
                break;
            case Qg2ResponseShape.IncomingNachaPage:
                AssertIncomingNachaPageSchema(schema, schemas, expected.SchemaName!, responseLabel);
                break;
            case Qg2ResponseShape.SchedulerPaged:
                AssertSchedulerPagedSchema(schema, schemas, expected.SchemaName!, responseLabel);
                break;
            default:
                throw new InvalidOperationException($"Shape QG-2 no soportado: {expected.Shape}");
        }
    }

    private static JsonElement GetFirstResponseSchema(JsonElement response, string responseLabel)
    {
        response.TryGetProperty("content", out var content).Should().BeTrue(
            $"{responseLabel}: debe publicar un cuerpo tipado");
        content.ValueKind.Should().Be(JsonValueKind.Object);

        foreach (var mediaType in content.EnumerateObject())
        {
            if (mediaType.Value.ValueKind == JsonValueKind.Object
                && mediaType.Value.TryGetProperty("schema", out var schema))
            {
                return schema;
            }
        }

        throw new InvalidOperationException($"{responseLabel}: no publicó ningún schema en sus content types existentes.");
    }

    private static void AssertCollectionSchema(
        JsonElement schema,
        JsonElement schemas,
        string itemSchemaName,
        string responseLabel)
    {
        var resolved = ResolveSchema(schema, schemas);
        resolved.TryGetProperty("type", out var type).Should().BeTrue(
            $"{responseLabel}: la colección debe publicar type=array");
        type.GetString().Should().Be("array", $"{responseLabel}: la response debe ser una colección");
        resolved.TryGetProperty("items", out var items).Should().BeTrue(
            $"{responseLabel}: la colección debe declarar items");
        SchemaResolvesTo(items, schemas, itemSchemaName).Should().BeTrue(
            $"{responseLabel}: los items deben usar {itemSchemaName}");
    }

    private static void AssertPaginatedSchema(
        JsonElement schema,
        JsonElement schemas,
        string itemSchemaName,
        string responseLabel)
    {
        var resolved = ResolveSchema(schema, schemas);
        resolved.TryGetProperty("properties", out var properties).Should().BeTrue(
            $"{responseLabel}: el contrato paginado debe exponer sus propiedades");

        foreach (var propertyName in new[] { "items", "page", "pageSize", "totalCount", "totalPages", "hasNext", "hasPrevious" })
        {
            properties.TryGetProperty(propertyName, out var property).Should().BeTrue(
                $"{responseLabel}: el contrato paginado debe publicar {propertyName}");
            property.TryGetProperty("description", out var propertyDescription).Should().BeTrue(
                $"{responseLabel}: {propertyName} debe exponer documentación XML");
            propertyDescription.GetString().Should().NotBeNullOrWhiteSpace(
                $"{responseLabel}: {propertyName} debe tener descripción XML");
        }

        var itemsSchema = ResolveSchema(properties.GetProperty("items"), schemas);
        itemsSchema.TryGetProperty("type", out var itemsType).Should().BeTrue(
            $"{responseLabel}: items debe ser una colección");
        itemsType.GetString().Should().Be("array");
        itemsSchema.TryGetProperty("items", out var itemSchema).Should().BeTrue();
        SchemaResolvesTo(itemSchema, schemas, itemSchemaName).Should().BeTrue(
            $"{responseLabel}: items debe contener {itemSchemaName}");
    }

    private static void AssertSchedulerPagedSchema(
        JsonElement schema,
        JsonElement schemas,
        string itemSchemaName,
        string responseLabel)
    {
        var resolved = ResolveSchema(schema, schemas);
        resolved.TryGetProperty("properties", out var properties).Should().BeTrue(
            $"{responseLabel}: el resultado paginado del scheduler debe exponer sus propiedades");

        foreach (var propertyName in new[] { "items", "page", "pageSize", "total" })
        {
            properties.TryGetProperty(propertyName, out var property).Should().BeTrue(
                $"{responseLabel}: el resultado paginado del scheduler debe publicar {propertyName}");
            property.TryGetProperty("description", out var propertyDescription).Should().BeTrue(
                $"{responseLabel}: {propertyName} debe exponer documentación XML");
            propertyDescription.GetString().Should().NotBeNullOrWhiteSpace(
                $"{responseLabel}: {propertyName} debe tener descripción XML");
        }

        var itemsSchema = ResolveSchema(properties.GetProperty("items"), schemas);
        itemsSchema.TryGetProperty("type", out var itemsType).Should().BeTrue(
            $"{responseLabel}: items debe ser una colección");
        itemsType.GetString().Should().Be("array");
        itemsSchema.TryGetProperty("items", out var itemSchema).Should().BeTrue();
        SchemaResolvesTo(itemSchema, schemas, itemSchemaName).Should().BeTrue(
            $"{responseLabel}: items debe contener {itemSchemaName}");
    }

    private static void AssertIncomingNachaPageSchema(
        JsonElement schema,
        JsonElement schemas,
        string itemSchemaName,
        string responseLabel)
    {
        var resolved = ResolveSchema(schema, schemas);
        resolved.TryGetProperty("properties", out var properties).Should().BeTrue(
            $"{responseLabel}: el resultado paginado de Incoming NACHA debe exponer sus propiedades");

        foreach (var propertyName in new[] { "items", "page", "pageSize", "totalItems" })
        {
            properties.TryGetProperty(propertyName, out var property).Should().BeTrue(
                $"{responseLabel}: el resultado paginado debe publicar {propertyName}");
            property.TryGetProperty("description", out var propertyDescription).Should().BeTrue(
                $"{responseLabel}: {propertyName} debe exponer documentación XML");
            propertyDescription.GetString().Should().NotBeNullOrWhiteSpace(
                $"{responseLabel}: {propertyName} debe tener descripción XML");
        }

        var itemsSchema = ResolveSchema(properties.GetProperty("items"), schemas);
        itemsSchema.TryGetProperty("type", out var itemsType).Should().BeTrue(
            $"{responseLabel}: items debe ser una colección");
        itemsType.GetString().Should().Be("array");
        itemsSchema.TryGetProperty("items", out var itemSchema).Should().BeTrue();
        SchemaResolvesTo(itemSchema, schemas, itemSchemaName).Should().BeTrue(
            $"{responseLabel}: items debe contener {itemSchemaName}");
    }

    private static void AssertValidationProblemDetailsSchema(
        JsonElement schema,
        JsonElement schemas,
        string responseLabel)
    {
        var resolved = ResolveSchema(schema, schemas);
        TryGetSchemaProperty(resolved, schemas, "errors", out _).Should().BeTrue(
            $"{responseLabel}: ValidationProblemDetails debe exponer errors");
        TryGetSchemaProperty(resolved, schemas, "title", out _).Should().BeTrue(
            $"{responseLabel}: ValidationProblemDetails debe conservar los campos de ProblemDetails");
    }

    private static void AssertAchResponsePagedSchema(
        JsonElement schema,
        JsonElement schemas,
        string itemSchemaName,
        string responseLabel)
    {
        var resolved = ResolveSchema(schema, schemas);
        resolved.TryGetProperty("properties", out var properties).Should().BeTrue(
            $"{responseLabel}: el contrato paginado de respuestas debe exponer sus propiedades");

        foreach (var propertyName in new[] { "items", "pageNumber", "pageSize", "totalCount", "totalPages" })
        {
            properties.TryGetProperty(propertyName, out var property).Should().BeTrue(
                $"{responseLabel}: el contrato paginado debe publicar {propertyName}");
            property.TryGetProperty("description", out var description).Should().BeTrue(
                $"{responseLabel}: {propertyName} debe conservar documentación XML");
            description.GetString().Should().NotBeNullOrWhiteSpace();
        }

        var itemsSchema = ResolveSchema(properties.GetProperty("items"), schemas);
        itemsSchema.TryGetProperty("type", out var itemsType).Should().BeTrue(
            $"{responseLabel}: items debe ser una colección");
        itemsType.GetString().Should().Be("array");
        itemsSchema.TryGetProperty("items", out var itemSchema).Should().BeTrue();
        SchemaResolvesTo(itemSchema, schemas, itemSchemaName).Should().BeTrue(
            $"{responseLabel}: items debe contener {itemSchemaName}");
    }

    private static bool TryGetSchemaProperty(
        JsonElement schema,
        JsonElement schemas,
        string propertyName,
        out JsonElement property)
    {
        var resolved = ResolveSchema(schema, schemas);
        if (resolved.ValueKind == JsonValueKind.Object
            && resolved.TryGetProperty("properties", out var properties)
            && properties.TryGetProperty(propertyName, out property))
        {
            return true;
        }

        if (resolved.ValueKind == JsonValueKind.Object
            && resolved.TryGetProperty("allOf", out var allOf)
            && allOf.ValueKind == JsonValueKind.Array)
        {
            foreach (var part in allOf.EnumerateArray())
            {
                if (TryGetSchemaProperty(part, schemas, propertyName, out property))
                {
                    return true;
                }
            }
        }

        property = default;
        return false;
    }

    private static bool TryGetSchemaPropertyIgnoreCase(
        JsonElement schema,
        JsonElement schemas,
        string propertyName,
        out JsonElement property)
    {
        var resolved = ResolveSchema(schema, schemas);
        if (resolved.ValueKind == JsonValueKind.Object
            && resolved.TryGetProperty("properties", out var properties)
            && properties.ValueKind == JsonValueKind.Object)
        {
            foreach (var candidate in properties.EnumerateObject())
            {
                if (string.Equals(candidate.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    property = candidate.Value;
                    return true;
                }
            }
        }

        if (resolved.ValueKind == JsonValueKind.Object
            && resolved.TryGetProperty("allOf", out var allOf)
            && allOf.ValueKind == JsonValueKind.Array)
        {
            foreach (var part in allOf.EnumerateArray())
            {
                if (TryGetSchemaPropertyIgnoreCase(part, schemas, propertyName, out property))
                {
                    return true;
                }
            }
        }

        property = default;
        return false;
    }

    private static bool SchemaResolvesTo(JsonElement schema, JsonElement schemas, string schemaName)
    {
        if (schema.TryGetProperty("$ref", out var reference)
            && reference.ValueKind == JsonValueKind.String)
        {
            var referenceName = reference.GetString()?.Split('/').LastOrDefault();
            if (string.Equals(referenceName, schemaName, StringComparison.Ordinal))
            {
                return true;
            }

            if (!string.IsNullOrWhiteSpace(referenceName)
                && schemas.TryGetProperty(referenceName, out var referencedSchema))
            {
                return SchemaResolvesTo(referencedSchema, schemas, schemaName);
            }
        }

        return false;
    }

    private static JsonElement ResolveSchema(JsonElement schema, JsonElement schemas)
    {
        var current = schema;
        var visited = new HashSet<string>(StringComparer.Ordinal);
        while (current.ValueKind == JsonValueKind.Object
               && current.TryGetProperty("$ref", out var reference)
               && reference.ValueKind == JsonValueKind.String)
        {
            var referenceName = reference.GetString()?.Split('/').LastOrDefault();
            if (string.IsNullOrWhiteSpace(referenceName)
                || !visited.Add(referenceName)
                || !schemas.TryGetProperty(referenceName, out var referencedSchema))
            {
                break;
            }

            current = referencedSchema;
        }

        return current;
    }

    private static bool HasBearerSecurity(JsonElement operation)
    {
        if (!operation.TryGetProperty("security", out var security) || security.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        return security.EnumerateArray().Any(requirement =>
            requirement.ValueKind == JsonValueKind.Object && requirement.TryGetProperty("Bearer", out _));
    }

    private static void AssertSc505CriticalFlowExamples(JsonElement paths)
    {
        var transactionCreatePaths = new[] { "/Transactions", "/api/transactions" };
        foreach (var path in transactionCreatePaths)
        {
            var request = GetRequestExample(paths.GetProperty(path).GetProperty("post"));
            request.GetProperty("transactionExternalId").GetString().Should().Be("TX-DEMO-001");
            request.GetProperty("sourceAccountNumber").GetString().Should().Be("000000000001");
            request.GetProperty("destinationAccountNumber").GetString().Should().Be("000000000002");
            request.GetProperty("companyName").GetString().Should().Be("EMPRESA DEMO");
        }

        var bulkSubmitPaths = new[] { "/Transactions/bulk/submit", "/api/transactions/bulk/submit" };
        foreach (var path in bulkSubmitPaths)
        {
            var operation = paths.GetProperty(path).GetProperty("post");
            var request = GetRequestExample(operation);
            request.GetProperty("batchReference").GetString().Should().Be("BATCH-DEMO-001");
            request.GetProperty("sourceType").GetInt32().Should().Be(1);
            request.GetProperty("processingMode").GetInt32().Should().Be(1);
            request.GetProperty("transactions").GetArrayLength().Should().Be(1);

            var response = GetResponseExample(operation, "200");
            response.GetProperty("status").GetString().Should().Be("COMPLETED");
            response.GetProperty("immediateResult").GetProperty("totalSucceeded").GetInt32().Should().Be(1);
        }

        var bulkPaths = new[] { "/Transactions/bulk", "/api/transactions/bulk" };
        foreach (var path in bulkPaths)
        {
            var operation = paths.GetProperty(path).GetProperty("post");
            var request = GetRequestExample(operation);
            request.GetProperty("batchReference").GetString().Should().Be("BATCH-DEMO-001");
            request.GetProperty("transactions").GetArrayLength().Should().Be(1);

            var response = GetResponseExample(operation, "200");
            response.GetProperty("totalReceived").GetInt32().Should().Be(1);
            response.GetProperty("totalSucceeded").GetInt32().Should().Be(1);
            response.GetProperty("itemResults").GetArrayLength().Should().Be(1);
        }

        var outboundOperation = paths.GetProperty("/api/ach-colombia/file-exchange/outbound/execute").GetProperty("post");
        var outboundRequest = GetRequestExample(outboundOperation);
        outboundRequest.GetProperty("cycleId").GetString().Should().Be("CYCLE-DEMO-001");
        outboundRequest.TryGetProperty("correctedFromTransferId", out _).Should().BeFalse(
            "el example no debe inventar una transferencia corregida");
        var outboundResponse = GetResponseExample(outboundOperation, "200");
        outboundResponse.GetProperty("processed").GetInt32().Should().Be(1);
        outboundResponse.GetProperty("succeeded").GetInt32().Should().Be(1);
        outboundResponse.GetProperty("failed").GetInt32().Should().Be(0);

        var inboundOperation = paths.GetProperty("/api/ach-colombia/file-exchange/inbound/execute").GetProperty("post");
        inboundOperation.TryGetProperty("requestBody", out _).Should().BeFalse(
            "la ejecución entrante no recibe body y SC-505 no debe inventarlo");
        var inboundResponse = GetResponseExample(inboundOperation, "200");
        inboundResponse.GetProperty("processed").GetInt32().Should().Be(1);
        inboundResponse.GetProperty("succeeded").GetInt32().Should().Be(1);

        foreach (var path in new[]
                 {
                     "/nacha-security/operations/nacha/generate",
                     "/nacha-security/operations/nacha/generate-encrypted"
                 })
        {
            var operation = paths.GetProperty(path).GetProperty("post");
            GetRequestExample(operation).GetProperty("cycleId").GetString().Should().Be("CYCLE-DEMO-001");
        }
    }

    private static JsonElement GetRequestExample(JsonElement operation)
    {
        var mediaType = GetJsonMediaType(
            operation.GetProperty("requestBody").GetProperty("content"),
            "operación",
            "request");
        mediaType.TryGetProperty("example", out var example).Should().BeTrue(
            "la operación debe publicar el ejemplo de request explícito");
        return example;
    }

    private static JsonElement GetResponseExample(JsonElement operation, string statusCode)
    {
        var response = operation.GetProperty("responses").GetProperty(statusCode);
        var mediaType = GetJsonMediaType(response.GetProperty("content"), "operación", $"response {statusCode}");
        mediaType.TryGetProperty("example", out var example).Should().BeTrue(
            $"el response {statusCode} debe publicar el ejemplo explícito");
        return example;
    }

    private static JsonElement GetJsonMediaType(JsonElement content, string operationLabel, string contractPart)
    {
        content.TryGetProperty("application/json", out var mediaType).Should().BeTrue(
            $"{operationLabel}: {contractPart} debe conservar application/json publicado por ASP.NET Core");
        return mediaType;
    }

    private static string? GetSchemaReferenceName(JsonElement schema)
    {
        if (!schema.TryGetProperty("$ref", out var reference) || reference.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        return reference.GetString()?.Split('/').LastOrDefault();
    }

    private static void AssertSc508FinalSurfaceGate(
        IReadOnlyCollection<(string Path, string Method, JsonElement Operation)> httpOperations,
        IReadOnlyCollection<ApiDescription> apiDescriptions)
    {
        var contractual = Qg1Operations.ToHashSet();
        var qg2 = Qg2Operations.ToHashSet();
        var pending = PendingReservedScopeOperations.ToHashSet();
        var aliasExclusions = PendingCanonicalAliasExclusions.ToHashSet();
        var sc508Exclusions = Sc508IntentionalExclusionOperations
            .Select(operation => (operation.Method, operation.Path))
            .ToHashSet();
        var visibleIntentionalExclusions = aliasExclusions.Concat(sc508Exclusions).ToHashSet();
        var ignoredKeys = ApiExplorerIgnoredOperations
            .Select(operation => (operation.Method, operation.Path))
            .ToHashSet();
        var redirectKeys = DocumentationRedirectExclusions.ToHashSet();

        BaselineQgOperations.Should().HaveCount(245,
            "UX3 amplía el baseline contractual con el GET contextual del editor");
        BaselineQgOperations.Distinct().Should().HaveCount(245,
            "el baseline QG previo a SC-508 no debe contener METHOD + PATH duplicados");
        Sc508PublicContractOperations.Should().HaveCount(80,
            "SC-508 clasifica 80 de las 92 operaciones autorizadas como PUBLIC_CONTRACT");
        Sc508IntentionalExclusionOperations.Should().HaveCount(12,
            "SC-508 excluye únicamente Maintenance y el simulador NACHA UAT/local dentro del lote de 92");
        PendingReservedScopeOperations.Should().HaveCount(38,
            "UX4 agrega la prueba operativa de solo lectura a las operaciones reservadas fuera del baseline GET");
        PendingCanonicalAliasExclusions.Should().HaveCount(36,
            "los aliases CANÓNICA PENDIENTE visibles fuera de QG deben permanecer sin resolver");
        ApiExplorerIgnoredOperations.Should().HaveCount(8,
            "las ocho operaciones históricamente excluidas deben permanecer controladas (siete fuera de compilación y una IgnoreApi)");
        DocumentationRedirectExclusions.Should().HaveCount(2,
            "los redirects documentales raíz deben permanecer fuera de OpenAPI/QG");

        contractual.Should().HaveCount(327,
            "QG final = 245 baseline + 80 PUBLIC_CONTRACT SC-508 + 2 operaciones administrativas FASE 5");
        qg2.Should().HaveCount(327,
            "QG-2 final debe contener las mismas 327 METHOD + PATH que QG-1");
        qg2.Should().BeEquivalentTo(contractual,
            "QG-1 y QG-2 finales deben ser exactamente el mismo conjunto METHOD + PATH");

        contractual.Intersect(pending).Should().BeEmpty();
        contractual.Intersect(visibleIntentionalExclusions).Should().BeEmpty();
        contractual.Intersect(ignoredKeys).Should().BeEmpty();
        contractual.Intersect(redirectKeys).Should().BeEmpty();
        pending.Intersect(visibleIntentionalExclusions).Should().BeEmpty();
        pending.Intersect(ignoredKeys).Should().BeEmpty();
        pending.Intersect(redirectKeys).Should().BeEmpty();
        visibleIntentionalExclusions.Intersect(ignoredKeys).Should().BeEmpty();
        visibleIntentionalExclusions.Intersect(redirectKeys).Should().BeEmpty();
        ignoredKeys.Intersect(redirectKeys).Should().BeEmpty();

        var expectedVisible = contractual
            .Concat(pending)
            .Concat(visibleIntentionalExclusions)
            .ToHashSet();
        expectedVisible.Should().HaveCount(408,
            "la superficie visible por ApiExplorer/OpenAPI debe incluir las dos operaciones administrativas agregadas en FASE 5");

        var openApiKeys = httpOperations
            .Select(operation => (operation.Method.ToUpperInvariant(), operation.Path))
            .ToHashSet();
        var apiExplorerKeys = apiDescriptions
            .Select(description => (description.HttpMethod!.ToUpperInvariant(), NormalizeApiExplorerPath(description.RelativePath!)))
            .ToHashSet();

        openApiKeys.Should().HaveCount(408,
            "OpenAPI debe incorporar las dos operaciones administrativas ACH Colombia agregadas en FASE 5");
        apiExplorerKeys.Should().HaveCount(408,
            "ApiExplorer debe incorporar las dos operaciones administrativas ACH Colombia agregadas en FASE 5");
        openApiKeys.Should().BeEquivalentTo(apiExplorerKeys,
            "OpenAPI debe reflejar exactamente la superficie visible de ApiExplorer");
        openApiKeys.Should().BeEquivalentTo(expectedVisible,
            "toda operación visible debe quedar como CONTRACTUAL_QG, PENDING_RESERVED_SCOPE o INTENTIONAL_EXCLUSION");

        foreach (var ignored in ApiExplorerIgnoredOperations)
        {
            var key = (ignored.Method, ignored.Path);
            openApiKeys.Should().NotContain(key,
                $"{ignored.Method} {ignored.Path} está clasificada APIEXPLORER_IGNORED y no debe publicarse en OpenAPI");
            apiExplorerKeys.Should().NotContain(key,
                $"{ignored.Method} {ignored.Path} está clasificada APIEXPLORER_IGNORED");
            AssertApiExplorerIgnoredMetadata(ignored);
        }

        foreach (var redirect in DocumentationRedirectExclusions)
        {
            openApiKeys.Should().NotContain(redirect,
                $"{redirect.Method} {redirect.Path} conserva su exclusión documental explícita");
            apiExplorerKeys.Should().NotContain(redirect,
                $"{redirect.Method} {redirect.Path} conserva su exclusión de ApiExplorer");
        }

        var totalClassified = contractual.Count
                              + pending.Count
                              + visibleIntentionalExclusions.Count
                              + ignoredKeys.Count
                              + redirectKeys.Count;
        totalClassified.Should().Be(416,
            "el gate final debe clasificar las 416 operaciones HTTP públicas inventariadas en r508");

        openApiKeys.Except(expectedVisible).Should().BeEmpty(
            "UNEXPECTED_UNCOVERED_OPERATION debe terminar en cero");
    }

    private static void AssertSc508PublicContractQG2(
        JsonElement paths,
        IReadOnlyCollection<ApiDescription> apiDescriptions,
        Sc508PublicContractOperation expected)
    {
        var operationLabel = $"{expected.Method} {expected.Path}";
        var apiDescription = apiDescriptions.Single(description =>
            string.Equals(description.HttpMethod, expected.Method, StringComparison.OrdinalIgnoreCase)
            && string.Equals(NormalizeApiExplorerPath(description.RelativePath!), expected.Path, StringComparison.Ordinal));

        var action = apiDescription.ActionDescriptor.Should().BeOfType<ControllerActionDescriptor>().Subject;
        action.ControllerTypeInfo.AsType().Should().Be(expected.ControllerType,
            $"{operationLabel}: SC-508 debe gobernar la acción del controller inventariado en r508");
        action.ActionName.Should().Be(expected.ActionName,
            $"{operationLabel}: SC-508 debe gobernar la acción inventariada en r508");

        var openApiOperation = paths.GetProperty(expected.Path).GetProperty(expected.Method.ToLowerInvariant());

        var openApiParameters = openApiOperation.TryGetProperty("parameters", out var parameters)
                                && parameters.ValueKind == JsonValueKind.Array
            ? parameters.EnumerateArray().ToArray()
            : [];

        foreach (var parameter in openApiParameters)
        {
            var name = GetParameterName(parameter);
            name.Should().NotBeNullOrWhiteSpace($"{operationLabel}: cada parámetro OpenAPI debe tener nombre");
            parameter.TryGetProperty("in", out var location).Should().BeTrue(
                $"{operationLabel}: el parámetro {name} debe declarar su ubicación HTTP");
            parameter.TryGetProperty("schema", out var parameterSchema).Should().BeTrue(
                $"{operationLabel}: el parámetro {name} debe publicar schema");
            parameterSchema.ValueKind.Should().Be(JsonValueKind.Object,
                $"{operationLabel}: el schema del parámetro {name} debe ser un objeto OpenAPI");

            if (string.Equals(location.GetString(), "path", StringComparison.OrdinalIgnoreCase))
            {
                parameter.TryGetProperty("required", out var required).Should().BeTrue(
                    $"{operationLabel}: el parámetro path {name} debe declarar required");
                required.GetBoolean().Should().BeTrue(
                    $"{operationLabel}: el parámetro path {name} debe ser obligatorio");
            }
        }

        var publicApiParameters = apiDescription.ParameterDescriptions
            .Where(parameter => parameter.Source?.Id is "Path" or "Query" or "Header")
            .Select(parameter => (
                Name: parameter.Name,
                Location: parameter.Source!.Id.ToLowerInvariant()))
            .ToArray();
        foreach (var parameter in publicApiParameters)
        {
            var hasMatchingOpenApiParameter = openApiParameters.Any(openApiParameter =>
                string.Equals(GetParameterName(openApiParameter), parameter.Name, StringComparison.OrdinalIgnoreCase)
                && openApiParameter.TryGetProperty("in", out var location)
                && string.Equals(location.GetString(), parameter.Location, StringComparison.OrdinalIgnoreCase));

            hasMatchingOpenApiParameter.Should().BeTrue(
                $"{operationLabel}: ApiExplorer publica {parameter.Location}:{parameter.Name} y OpenAPI debe conservarlo");
        }

        var hasRequestBody = apiDescription.ParameterDescriptions.Any(parameter =>
            parameter.Source?.Id is "Body" or "Form" or "FormFile");
        if (hasRequestBody)
        {
            openApiOperation.TryGetProperty("requestBody", out var requestBody).Should().BeTrue(
                $"{operationLabel}: ApiExplorer declara body/form y OpenAPI debe publicarlo");
            requestBody.TryGetProperty("content", out var requestContent).Should().BeTrue(
                $"{operationLabel}: el request body debe publicar media types");
            AssertContentSchemas(requestContent, $"{operationLabel}: request body");
        }

        var expectedStatuses = apiDescription.SupportedResponseTypes
            .Select(response => response.StatusCode)
            .Where(statusCode => statusCode > 0)
            .Select(statusCode => statusCode.ToString())
            .ToHashSet(StringComparer.Ordinal);
        openApiOperation.TryGetProperty("responses", out var responses).Should().BeTrue(
            $"{operationLabel}: toda operación contractual debe publicar responses");
        var actualStatuses = responses.EnumerateObject()
            .Select(response => response.Name)
            .ToHashSet(StringComparer.Ordinal);
        if (expectedStatuses.Count > 0)
        {
            actualStatuses.Should().BeEquivalentTo(expectedStatuses,
                $"{operationLabel}: OpenAPI debe conservar los status codes que ApiExplorer obtiene de la metadata actual sin inventar otros");
        }

        foreach (var response in responses.EnumerateObject())
        {
            response.Value.TryGetProperty("description", out var description).Should().BeTrue(
                $"{operationLabel}: response {response.Name} debe publicar descripción");
            description.GetString().Should().NotBeNullOrWhiteSpace(
                $"{operationLabel}: response {response.Name} debe conservar una descripción OpenAPI");
            if (response.Value.TryGetProperty("content", out var responseContent))
            {
                AssertContentSchemas(responseContent, $"{operationLabel}: response {response.Name}");
            }
        }

        var allowAnonymous = action.EndpointMetadata.OfType<IAllowAnonymous>().Any();
        var requiresAuthorization = action.EndpointMetadata.OfType<IAuthorizeData>().Any();
        if (allowAnonymous)
        {
            HasBearerSecurity(openApiOperation).Should().BeFalse(
                $"{operationLabel}: la metadata actual AllowAnonymous no debe convertirse en Bearer obligatorio");
        }
        else if (requiresAuthorization)
        {
            HasBearerSecurity(openApiOperation).Should().BeTrue(
                $"{operationLabel}: la metadata Authorize/Policy actual debe publicarse como Bearer");
        }
    }

    private static void AssertContentSchemas(JsonElement content, string operationPart)
    {
        content.ValueKind.Should().Be(JsonValueKind.Object, $"{operationPart}: content debe ser un objeto OpenAPI");
        foreach (var mediaType in content.EnumerateObject())
        {
            mediaType.Value.TryGetProperty("schema", out var schema).Should().BeTrue(
                $"{operationPart}: {mediaType.Name} debe publicar schema");
            schema.ValueKind.Should().Be(JsonValueKind.Object,
                $"{operationPart}: el schema de {mediaType.Name} debe ser un objeto OpenAPI");
        }
    }

    private static void AssertApiExplorerIgnoredMetadata(ApiExplorerIgnoredOperation expected)
    {
        if (expected.ControllerType is null)
        {
            // JwksController, ServersController and TestsController remain in source for audit/history,
            // but the production API project intentionally excludes them from compilation. Their routes
            // therefore remain absent from ApiExplorer/OpenAPI without re-enabling internal/sensitive endpoints.
            var apiAssembly = typeof(SobreDigitalController).Assembly;
            apiAssembly.GetType($"Cfa.ACHInterbank.Api.Controllers.{expected.ControllerTypeName}")
                .Should().BeNull(
                    $"{expected.ControllerTypeName} is intentionally excluded from the API compilation");
            return;
        }

        var controllerIgnored = expected.ControllerType
            .GetCustomAttributes(inherit: true)
            .OfType<Microsoft.AspNetCore.Mvc.ApiExplorerSettingsAttribute>()
            .Any(attribute => attribute.IgnoreApi);
        var actionIgnored = expected.ControllerType
            .GetMethods()
            .Where(method => string.Equals(method.Name, expected.ActionName, StringComparison.Ordinal))
            .SelectMany(method => method.GetCustomAttributes(inherit: true))
            .OfType<Microsoft.AspNetCore.Mvc.ApiExplorerSettingsAttribute>()
            .Any(attribute => attribute.IgnoreApi);

        (controllerIgnored || actionIgnored).Should().BeTrue(
            $"{expected.Method} {expected.Path} debe conservar ApiExplorerSettings(IgnoreApi = true)");
    }

    private static string NormalizeApiExplorerPath(string relativePath)
    {
        var path = relativePath.Split('?', 2)[0].Trim();
        if (!path.StartsWith('/'))
        {
            path = "/" + path;
        }

        return System.Text.RegularExpressions.Regex.Replace(
            path,
            @"\{([^}:]+):[^}]+\}",
            "{$1}");
    }

    private static void AssertQG1(JsonElement paths, string method, string path)
    {
        paths.TryGetProperty(path, out var pathItem).Should().BeTrue(
            $"{method} {path}: la operación declarada en el allowlist QG-1 debe existir en OpenAPI");

        var normalizedMethod = method.ToLowerInvariant();
        pathItem.TryGetProperty(normalizedMethod, out var jsonOperation).Should().BeTrue(
            $"{method} {path}: el método declarado en el allowlist QG-1 debe existir en OpenAPI");

        var operation = new OpenApiOperation
        {
            Summary = GetOptionalString(jsonOperation, "summary"),
            Description = GetOptionalString(jsonOperation, "description"),
            OperationId = GetOptionalString(jsonOperation, "operationId")
        };

        AssertQG1Operation(operation, GetOperationTags(jsonOperation), method, path);
    }

    private static void AssertQG1Operation(
        OpenApiOperation operation,
        IReadOnlyCollection<string> tags,
        string method,
        string path)
    {
        var operationLabel = $"{method.ToUpperInvariant()} {path}";
        var issues = OpenApiDocumentationCompletenessDetector.Detect(operation);

        issues.HasFlag(OpenApiDocumentationIssue.SummaryIncomplete).Should().BeFalse(
            $"{operationLabel}: Summary debe existir y no ser placeholder según SC-003");
        issues.HasFlag(OpenApiDocumentationIssue.DescriptionIncomplete).Should().BeFalse(
            $"{operationLabel}: Description debe existir y no ser placeholder según SC-003");
        operation.OperationId.Should().NotBeNullOrWhiteSpace(
            $"{operationLabel}: operationId debe existir y no estar vacío");
        tags.Should().ContainSingle(
            $"{operationLabel}: debe existir exactamente un tag funcional primario");
        tags.Single().Should().NotBeNullOrWhiteSpace(
            $"{operationLabel}: el tag funcional no puede estar vacío");
        OfficialIntegrationTags.Should().Contain(tags.Single(),
            $"{operationLabel}: el tag debe pertenecer a la taxonomía oficial");
    }

    private static string? GetOptionalString(JsonElement element, string propertyName)
        => element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;

    private static void AssertDoesNotContainHeuristicDocumentation(JsonElement operation)
    {
        var textualDocumentation = string.Join(
            "\n",
            operation.TryGetProperty("summary", out var summary) && summary.ValueKind == JsonValueKind.String
                ? summary.GetString()
                : null,
            operation.TryGetProperty("description", out var description) && description.ValueKind == JsonValueKind.String
                ? description.GetString()
                : null);

        textualDocumentation.Should().NotContain("Qué hace:");
        textualDocumentation.Should().NotContain("Cuándo se usa:");
        textualDocumentation.Should().NotContain("Riesgo operativo:");
        textualDocumentation.Should().NotContain("Precauciones para desarrollo u operación:");
    }

    private static void AssertHeaderParameter(JsonElement operation, string headerName)
    {
        var matches = GetHeaderParameters(operation)
            .Where(parameter => string.Equals(GetParameterName(parameter), headerName, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        matches.Should().ContainSingle($"{headerName} debe aparecer una sola vez");
        var parameter = matches[0];
        parameter.GetProperty("in").GetString().Should().Be("header");

        var isRequired = parameter.TryGetProperty("required", out var required) && required.GetBoolean();
        isRequired.Should().BeFalse();

        parameter.GetProperty("description").GetString().Should().NotBeNullOrWhiteSpace();
        parameter.GetProperty("schema").GetProperty("type").GetString().Should().Be("string");
    }

    private static void AssertHeaderAbsent(JsonElement operation, string headerName)
    {
        GetHeaderParameters(operation)
            .Where(parameter => string.Equals(GetParameterName(parameter), headerName, StringComparison.OrdinalIgnoreCase))
            .Should().BeEmpty($"{headerName} no debe documentarse cuando la operación no lo consume");
    }

    private static JsonElement[] GetHeaderParameters(JsonElement operation)
    {
        if (!operation.TryGetProperty("parameters", out var parameters) || parameters.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return parameters.EnumerateArray()
            .Where(parameter => parameter.ValueKind == JsonValueKind.Object
                && parameter.TryGetProperty("in", out var location)
                && location.ValueKind == JsonValueKind.String
                && string.Equals(location.GetString(), "header", StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }

    private static string? GetParameterName(JsonElement parameter)
        => parameter.TryGetProperty("name", out var name) && name.ValueKind == JsonValueKind.String
            ? name.GetString()
            : null;

    private static string[] GetOperationTags(JsonElement operation)
    {
        if (!operation.TryGetProperty("tags", out var tags) || tags.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return tags.EnumerateArray()
            .Where(tag => tag.ValueKind == JsonValueKind.String)
            .Select(tag => tag.GetString())
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Select(tag => tag!)
            .ToArray();
    }

    private static int CountOperations(JsonElement paths)
    {
        return EnumerateHttpOperations(paths).Count();
    }

    private static IEnumerable<(string Path, string Method, JsonElement Operation)> EnumerateHttpOperations(JsonElement paths)
    {
        foreach (var path in paths.EnumerateObject())
        {
            foreach (var property in path.Value.EnumerateObject())
            {
                if (HttpMethods.Contains(property.Name))
                {
                    yield return (path.Name, property.Name, property.Value);
                }
            }
        }
    }

    private static void AssertAllReferencesAreLocalAndResolvable(JsonElement root)
    {
        foreach (var reference in EnumerateReferences(root))
        {
            reference.Should().StartWith("#/components/");
            ResolveJsonPointer(root, reference).Should().BeTrue($"reference '{reference}' must resolve");
        }
    }

    private static IEnumerable<string> EnumerateReferences(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    if (property.NameEquals("$ref") && property.Value.ValueKind == JsonValueKind.String)
                    {
                        yield return property.Value.GetString()!;
                    }

                    foreach (var nested in EnumerateReferences(property.Value))
                    {
                        yield return nested;
                    }
                }

                break;
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    foreach (var nested in EnumerateReferences(item))
                    {
                        yield return nested;
                    }
                }

                break;
        }
    }

    private static bool ResolveJsonPointer(JsonElement root, string reference)
    {
        if (!reference.StartsWith("#/", StringComparison.Ordinal))
        {
            return false;
        }

        var current = root;
        foreach (var rawSegment in reference[2..].Split('/'))
        {
            var segment = rawSegment.Replace("~1", "/", StringComparison.Ordinal)
                .Replace("~0", "~", StringComparison.Ordinal);
            if (current.ValueKind != JsonValueKind.Object || !current.TryGetProperty(segment, out current))
            {
                return false;
            }
        }

        return true;
    }
}
