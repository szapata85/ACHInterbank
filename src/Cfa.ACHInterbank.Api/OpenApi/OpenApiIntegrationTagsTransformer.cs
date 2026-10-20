using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Cfa.ACHInterbank.Api.OpenApi;

public sealed class OpenApiIntegrationTagsTransformer : IOpenApiOperationTransformer
{
    private static readonly IReadOnlyDictionary<string, string> ControllerTags =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["AchClaims"] = "Devoluciones y respuestas",
            ["AchColombiaFileExchange"] = "ACH Colombia",
            ["AchCycles"] = "Cámaras y ciclos",
            ["AchOutboundReturnTransport"] = "Devoluciones y respuestas",
            ["AchReconciliation"] = "Trazabilidad y conciliación",
            ["AchResponseOperations"] = "Devoluciones y respuestas",
            ["AchResponses"] = "Devoluciones y respuestas",
            ["AchReturnOfReturn"] = "Devoluciones y respuestas",
            ["AchReturns"] = "Devoluciones y respuestas",
            ["AchTraceability"] = "Trazabilidad y conciliación",
            ["AuditLogs"] = "Auditoría y observabilidad",
            ["Auth"] = "Autenticación y acceso",
            ["AuthLogs"] = "Auditoría y observabilidad",
            ["BankHolidays"] = "Cámaras y ciclos",
            ["Branding"] = "Administración y catálogos",
            ["BulkIngestion"] = "Transacciones interbancarias",
            ["CatalogTypes"] = "Administración y catálogos",
            ["CenitOperations"] = "CENIT",
            ["CertificateManagement"] = "Seguridad y certificados",
            ["CfaOperationalTransactionRestrictions"] = "Políticas y reglas transaccionales",
            ["ClearingHouseCycleConfigs"] = "Cámaras y ciclos",
            ["ClearingHouseSpecialDates"] = "Cámaras y ciclos",
            ["ClearingHouseTransactionPolicies"] = "Políticas y reglas transaccionales",
            ["ClearingHouseTransactionRules"] = "Políticas y reglas transaccionales",
            ["ClearingHouses"] = "Cámaras y ciclos",
            ["CompanyEntryDescriptions"] = "Administración y catálogos",
            ["CustomerThirdParties"] = "Administración y catálogos",
            ["Customers"] = "Administración y catálogos",
            ["DigitalEnvelopeCertificates"] = "Seguridad y certificados",
            ["FinancialInstitutions"] = "Administración y catálogos",
            ["IncomingNachaCommandCenter"] = "NACHA-M",
            ["InstitutionClearingHousePreferences"] = "Cámaras y ciclos",
            ["IntegrationCatalog"] = "Integraciones",
            ["IntegrationMappingSets"] = "Integraciones",
            ["Jwks"] = "Autenticación y acceso",
            ["LoginLockoutSettings"] = "Usuarios y permisos",
            ["Maintenance"] = "Operación y programación",
            ["MenuItems"] = "Administración y catálogos",
            ["Nacha"] = "NACHA-M",
            ["NachaConfigProfiles"] = "NACHA-M",
            ["NachaConfigProfilesReadOnly"] = "NACHA-M",
            ["NachaExport"] = "NACHA-M",
            ["NachaInboundSimulator"] = "NACHA-M",
            ["NachaOperationalReadiness"] = "NACHA-M",
            ["NachaRecordDefinitions"] = "NACHA-M",
            ["NachaRecordLayouts"] = "NACHA-M",
            ["NachaSecurityOperations"] = "NACHA-M",
            ["NachaSoapUatConsole"] = "NACHA-M",
            ["NachaUpload"] = "NACHA-M",
            ["Navigation"] = "Administración y catálogos",
            ["NavigationLogs"] = "Auditoría y observabilidad",
            ["Oauths"] = "Autenticación y acceso",
            ["OutgoingTransactionMonitoring"] = "Trazabilidad y conciliación",
            ["PasswordRules"] = "Usuarios y permisos",
            ["PaymentRailCapabilityRegistry"] = "Políticas y reglas transaccionales",
            ["Permissions"] = "Usuarios y permisos",
            ["Prenotifications"] = "Transacciones interbancarias",
            ["RegulatoryCatalogs"] = "Administración y catálogos",
            ["Reports"] = "Reportes",
            ["ReturnReasons"] = "Devoluciones y respuestas",
            ["Roles"] = "Usuarios y permisos",
            ["Scheduler"] = "Operación y programación",
            ["SoapIntegrationSettings"] = "Integraciones",
            ["SobreDigital"] = "Seguridad y certificados",
            ["TaskDefinitions"] = "Operación y programación",
            ["TransactionPrerequisitePolicy"] = "Políticas y reglas transaccionales",
            ["Transactions"] = "Transacciones interbancarias",
            ["UatContrapartidas"] = "Integraciones",
            ["Users"] = "Usuarios y permisos"
        };

    public Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (context.Description.ActionDescriptor is not ControllerActionDescriptor action)
        {
            return Task.CompletedTask;
        }

        if (!ControllerTags.TryGetValue(action.ControllerName, out var tag))
        {
            operation.Tags = new HashSet<OpenApiTagReference>();
            return Task.CompletedTask;
        }

        operation.Tags = new HashSet<OpenApiTagReference>
        {
            new OpenApiTagReference(tag, context.Document)
        };

        return Task.CompletedTask;
    }
}
