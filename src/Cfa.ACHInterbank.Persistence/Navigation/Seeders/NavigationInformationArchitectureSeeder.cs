using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Domain.Entities.Navigation;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.Configuration;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.Navigation.Seeders;

/// <summary>
/// Final navigation information-architecture reconciler.
/// NAV2.2 owns "Administración de parámetros", NAV2.3 adds "Operación", NAV2.4 adds "Clientes",
/// NAV2.5A/NAV2.5B build "Seguimiento y control", NAV2.6 adds "Seguridad de archivos",
/// NAV2.7 canonicalizes "Reportes", NAV2.8A creates "Administración" for the
/// "Usuarios y acceso" group, NAV2.8B adds the existing scheduled-tasks surface,
/// NAV2.8C canonicalizes the existing system-records surfaces, NAV2.8D moves the
/// existing navigation-structure administration surface under Administración, NAV2.8E
/// publishes the existing read-only payment-rail capability registry as platform administration,
/// NAV2.9 canonicalizes the existing technical test-tool domain without changing its security model,
/// NAV2.10A normalizes the final backend tree (root orders and known legacy duplicates), and
/// NAV2.10B completes the canonical Catalogs group using only existing SPA administration routes.
/// Other navigation/security changes remain deferred until a later authorized increment.
/// </summary>
[Scoped]
public sealed class NavigationInformationArchitectureSeeder(AchDbContext context) : IDbSeeder
{
    public const int FinalReconciliationOrder = 100;

    public const string HomeRoute = "/dashboard";
    public const string HomeLabel = "Inicio";
    public const string OperationRoute = "/transactions";
    public const string OperationLabel = "Operación";
    public const string CustomersRoute = "/customers";
    public const string CustomersLabel = "Clientes";
    public const string MonitoringAndControlRoute = "/seguimiento-control";
    public const string MonitoringAndControlLabel = "Seguimiento y control";
    public const string TransactionResponsesGroupRoute = "/seguimiento-control/respuestas-transacciones";
    public const string TransactionResponsesGroupLabel = "Respuestas de transacciones";
    public const string ParametersAdministrationRoute = "/administracion-parametros";
    public const string ParametersAdministrationLabel = "Administración de parámetros";
    public const string FileSecurityRoute = "/nacha-security";
    public const string FileSecurityLabel = "Seguridad de archivos";
    public const string ReportsRoute = "/reports";
    public const string ReportsLabel = "Reportes";
    public const string AdministrationRoute = "/administracion";
    public const string AdministrationLabel = "Administración";
    public const string UsersAccessLabel = "Usuarios y acceso";
    public const string ScheduledTasksRoute = "/scheduler/tasks";
    public const string ScheduledTasksLabel = "Tareas programadas";
    public const string SystemRecordsRoute = "/logs";
    public const string SystemRecordsLabel = "Registros del sistema";
    public const string AuditLogRoute = "/audit-logs";
    public const string AuditLogLabel = "Registro de auditoría";
    public const string AuthenticationLogRoute = "/auth-logs";
    public const string AuthenticationLogLabel = "Registro de accesos";
    public const string NavigationLogRoute = "/navigation-logs";
    public const string NavigationLogLabel = "Registro de navegación";
    public const string MenuOrganizationRoute = "/navigation/menu-items";
    public const string MenuOrganizationLabel = "Organización del menú";
    public const string PlatformCapabilitiesRoute = "/payment-rail-capability-registry";
    public const string PlatformCapabilitiesLabel = "Capacidades de la plataforma";
    public const string TestToolsRoute = "/uat";
    public const string TestToolsLabel = "Herramientas de prueba";
    public const string InboundFileSimulatorRoute = "/uat/nacha-inbound-simulator";
    public const string InboundFileSimulatorLabel = "Simulador de archivos recibidos";
    public const string ServiceTestsRoute = "/ach/nacha/soap-uat-console";
    public const string ServiceTestsLabel = "Pruebas de servicios";

    private const string LegacySchedulerGroupRoute = "/scheduler";

    private const string LegacyCustomerThirdPartiesRoute = "/customer-third-parties";

    private const string TransactionsListRoute = "/transactions/list";
    private const string TransactionsCreateRoute = "/transactions/create";
    private const string BulkIngestionUploadRoute = "/transactions/bulk-ingestion/upload";
    private const string BulkIngestionTrackingRoute = "/transactions/bulk-ingestion/tracking";
    private const string NachaUploadRoute = "/transactions/nacha-upload";
    private const string TransactionsReturnsRoute = "/transactions/returns";
    private const string ReturnOfReturnRoute = "/transactions/returns-ror";
    private const string ClaimsRoute = "/ach-claims";
    private const string AchColombiaFileExchangeRoute = "/ach-colombia/file-exchange";
    private const string NachaExportRoute = "/ach-cycles/nacha/export";

    private const string OutgoingMonitoringRoute = "/transactions/outgoing-monitoring";
    private const string IncomingNachaCommandCenterRoute = "/incoming-nacha-command-center";
    private const string IncomingNachaObservabilityRoute = "/incoming-nacha-command-center/observability";
    private const string IncomingNachaQueueRoute = "/incoming-nacha-command-center/queue";
    private const string IncomingNachaOrphanResolutionRoute = "/incoming-nacha-command-center/orphan-resolution";
    private const string AchResponsesInboxRoute = "/ach-responses";
    private const string AchResponsesDashboardRoute = "/ach-responses/dashboard";
    private const string AchResponsesManualReviewRoute = "/ach-responses/manual-review";
    private const string AchResponsesAuditRoute = "/ach-responses/audit";
    private const string ReconciliationRoute = "/ach/reconciliation";

    private const string ClearingHousesRoute = "/clearing-houses";
    private const string ClearingHousePrioritiesRoute = "/catalogs/clearing-house-preferences";
    private const string BankHolidaysRoute = "/catalogs/bank-holidays";
    private const string OperationalRestrictionsRoute = "/operational-restrictions";
    private const string NachaConfigurationRoute = "/nacha-config-admin";
    private const string NachaConfigProfilesRoute = "/nacha-config-admin/perfiles";
    private const string NachaConfigRecordsRedirectRoute = "/nacha-config-admin/records";
    private const string NachaConfigVariantsFieldsRedirectRoute = "/nacha-config-admin/variants-fields";
    private const string CatalogsRoute = "/catalogs";
    private const string FinancialInstitutionsRoute = "/catalogs/financial-institutions";
    private const string CompanyEntryDescriptionsRoute = "/catalogs/company-entry-descriptions";
    private const string DocumentTypesRoute = "/catalogs/document-types";
    private const string PersonTypesRoute = "/catalogs/person-types";
    private const string GenderTypesRoute = "/catalogs/gender-types";
    private const string PhoneTypesRoute = "/catalogs/phone-types";
    private const string EmailTypesRoute = "/catalogs/email-types";
    private const string AddressTypesRoute = "/catalogs/address-types";
    private const string TransactionCodesRoute = "/catalogs/transaction-codes";
    private const string LegacyClearingHouseSpecialDatesRoute = "/catalogs/clearing-house-special-dates";
    private const string ConnectionsRoute = "/integraciones";
    private const string LegacySoapIntegrationsRoute = "/soap-integrations";
    private const string ContractRoute = "/integraciones/contrato";
    private const string InformationRelationshipRoute = "/integraciones/relacion-informacion";
    private const string ParticipantConnectionRoute = "/integraciones/conexion";
    private const string FileExchangeConfigurationRoute = "/administracion/mft-ach";
    private const string StatusMappingsRoute = "/ach-responses/status-mappings";

    private const string DigitalCertificatesRoute = "/nacha-security/certificates";
    private const string DigitalEnvelopeRoute = "/nacha-security/sobre-digital";

    private const string ReportsSentRoute = "/reports/sent";
    private const string ReportsReceivedRoute = "/reports/received";
    private const string ReportsReturnsRoute = "/reports/returns";
    private const string ReportsRejectionsRoute = "/reports/rejections";
    private const string ReportsFilesRoute = "/reports/files";
    private const string ReportsCyclesRoute = "/reports/cycles";
    private const string ReportsReconciliationRoute = "/reports/reconciliation";
    private const string ReportsTraceabilityRoute = "/reports/traceability";
    private const string ReportsHistoryRoute = "/reports/history";
    private const string ReportsAuditRoute = "/reports/audit";

    private const string UsersRoute = "/users";
    private const string ManageUsersRoute = "/users/list";
    private const string BrandingRoute = "/users/branding";
    private const string PasswordRulesRoute = "/users/password-rules";
    private const string LoginLockoutRoute = "/users/login-lockout";

    public int Order => FinalReconciliationOrder;

    public async Task SeedAsync()
    {
        await ReconcileHomeAsync();
        await ReconcileOperationAsync();
        await ReconcileCustomersAsync();
        await ReconcileMonitoringAndControlAsync();
        await ReconcileParametersAdministrationAsync();
        await ReconcileFileSecurityAsync();
        await ReconcileReportsAsync();
        await ReconcileAdministrationAsync();
        await ReconcileTestToolsAsync();
        await context.SaveChangesAsync();
    }


    private async Task ReconcileHomeAsync()
    {
        var home = await context.MenuItems.SingleAsync(item => item.Id == MenuItemConfiguration.DashboardId);
        if (!string.Equals(home.Route, HomeRoute, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Expected existing home route '{HomeRoute}' before NAV2.10A reconciliation.");
        }

        // NAV2.10A only makes the already-existing dashboard entry canonical as the first
        // root domain. Route and authorization links remain untouched.
        Reconcile(home, HomeLabel, parentId: null, order: 10);
    }

    private async Task ReconcileOperationAsync()
    {
        var operation = await context.MenuItems.SingleAsync(item => item.Id == MenuItemConfiguration.TransactionsId);
        Reconcile(operation, OperationLabel, parentId: null, order: 20);

        await ReconcileExistingAsync(
            TransactionsListRoute,
            "Consultar transacciones",
            operation.Id,
            order: 10);

        await ReconcileExistingAsync(
            TransactionsCreateRoute,
            "Crear transacción",
            operation.Id,
            order: 20);

        var (bulkUpload, bulkUploadCreated) = await UpsertWithCreationStateAsync(
            BulkIngestionUploadRoute,
            "Cargar transacciones desde archivo",
            operation.Id,
            order: 30,
            icon: "upload_file",
            exact: true);

        var (bulkTracking, bulkTrackingCreated) = await UpsertWithCreationStateAsync(
            BulkIngestionTrackingRoute,
            "Seguimiento de cargas",
            operation.Id,
            order: 40,
            icon: "manage_search",
            exact: true);

        await ReconcileExistingAsync(
            NachaUploadRoute,
            "Cargar archivo NACHA-M",
            operation.Id,
            order: 50);

        await ReconcileExistingAsync(
            TransactionsReturnsRoute,
            "Devoluciones",
            operation.Id,
            order: 60);

        var (returnOfReturn, returnOfReturnCreated) = await UpsertWithCreationStateAsync(
            ReturnOfReturnRoute,
            "Devolución de una devolución",
            operation.Id,
            order: 70,
            icon: "assignment_returned",
            exact: true);

        await ReconcileExistingAsync(
            ClaimsRoute,
            "Reclamaciones",
            operation.Id,
            order: 80);

        await ReconcileExistingAsync(
            AchColombiaFileExchangeRoute,
            "Intercambio de archivos",
            operation.Id,
            order: 90);

        await ReconcileExistingAsync(
            NachaExportRoute,
            "Exportar archivo NACHA-M",
            operation.Id,
            order: 100);

        await context.SaveChangesAsync();

        // These three SPA routes had no MenuItem in the NAV2.2 baseline. Their current
        // Angular route guards require CanManageAch and inherit Admin/ACH.Operator at
        // /transactions. Add only those current authorization links when the row is new;
        // never rewrite authorization on a pre-existing MenuItem.
        if (bulkUploadCreated)
            AddAuthorization(bulkUpload.Id, PermissionConfiguration.ManageAchPermissionId);
        if (bulkTrackingCreated)
            AddAuthorization(bulkTracking.Id, PermissionConfiguration.ManageAchPermissionId);
        if (returnOfReturnCreated)
            AddAuthorization(returnOfReturn.Id, PermissionConfiguration.ManageAchPermissionId);
    }

    private async Task ReconcileCustomersAsync()
    {
        var (customers, customersCreated) = await UpsertWithCreationStateAsync(
            CustomersRoute,
            CustomersLabel,
            parentId: null,
            order: 30,
            icon: "groups",
            exact: false);

        var legacyThirdParties = await context.MenuItems
            .SingleOrDefaultAsync(item => item.Route == LegacyCustomerThirdPartiesRoute);
        if (legacyThirdParties is not null)
        {
            // The legacy route remains functional for compatibility but must no longer
            // appear as an independent menu option once Clientes is canonical.
            legacyThirdParties.IsActive = false;
        }

        await context.SaveChangesAsync();

        // /customers is guarded by CanReadAch in the current SPA. For a newly published
        // menu row, reuse that existing permission identifier without creating or changing
        // any permission/role definition or user grant. Existing authorization is never rewritten.
        if (customersCreated && !await context.MenuItemPermissions
                .AnyAsync(link => link.MenuItemId == customers.Id))
        {
            context.MenuItemPermissions.Add(new MenuItemPermission
            {
                MenuItemId = customers.Id,
                PermissionId = PermissionConfiguration.ReadAchPermissionId
            });
        }
    }

    private async Task ReconcileMonitoringAndControlAsync()
    {
        var monitoringRoot = await UpsertAsync(
            MonitoringAndControlRoute,
            MonitoringAndControlLabel,
            parentId: null,
            order: 40,
            icon: "monitoring",
            exact: false);
        await context.SaveChangesAsync();

        // Both entries already exist before the canonical reconciler. NAV2.5A only moves,
        // relabels and orders them; their current authorization links remain untouched.
        await ReconcileExistingAsync(
            OutgoingMonitoringRoute,
            "Transacciones enviadas",
            monitoringRoot.Id,
            order: 10);

        var incomingCommandCenter = await ReconcileExistingAsync(
            IncomingNachaCommandCenterRoute,
            "Seguimiento de archivos",
            monitoringRoot.Id,
            order: 20);

        var (observability, observabilityCreated) = await UpsertMonitoringChildAsync(
            IncomingNachaObservabilityRoute,
            "Indicadores operativos",
            monitoringRoot.Id,
            order: 30,
            icon: "insights");

        var (queue, queueCreated) = await UpsertMonitoringChildAsync(
            IncomingNachaQueueRoute,
            "Cola de procesamiento",
            monitoringRoot.Id,
            order: 40,
            icon: "format_list_numbered");

        var (orphanResolution, orphanResolutionCreated) = await UpsertMonitoringChildAsync(
            IncomingNachaOrphanResolutionRoute,
            "Devoluciones sin relación",
            monitoringRoot.Id,
            order: 50,
            icon: "rule");

        await context.SaveChangesAsync();

        // These three SPA routes are protected by the same current parent authorization as
        // /incoming-nacha-command-center. Reuse that existing menu authorization only when
        // NAV2.5A creates the row; never rewrite authorization on a pre-existing MenuItem.
        if (observabilityCreated)
            await MirrorAuthorizationIfEmptyAsync(incomingCommandCenter.Id, observability.Id);
        if (queueCreated)
            await MirrorAuthorizationIfEmptyAsync(incomingCommandCenter.Id, queue.Id);
        if (orphanResolutionCreated)
            await MirrorAuthorizationIfEmptyAsync(incomingCommandCenter.Id, orphanResolution.Id);

        var responsesGroup = await UpsertAsync(
            TransactionResponsesGroupRoute,
            TransactionResponsesGroupLabel,
            monitoringRoot.Id,
            order: 60,
            icon: "forum",
            exact: false);
        await context.SaveChangesAsync();

        var (responsesInbox, responsesInboxCreated) = await UpsertMonitoringChildAsync(
            AchResponsesInboxRoute,
            "Bandeja de respuestas",
            responsesGroup.Id,
            order: 10,
            icon: "inbox");

        var (responsesDashboard, responsesDashboardCreated) = await UpsertMonitoringChildAsync(
            AchResponsesDashboardRoute,
            "Resumen de respuestas",
            responsesGroup.Id,
            order: 20,
            icon: "dashboard");

        var (responsesManualReview, responsesManualReviewCreated) = await UpsertMonitoringChildAsync(
            AchResponsesManualReviewRoute,
            "Revisión manual",
            responsesGroup.Id,
            order: 30,
            icon: "fact_check");

        var (responsesAudit, responsesAuditCreated) = await UpsertMonitoringChildAsync(
            AchResponsesAuditRoute,
            "Historial de respuestas",
            responsesGroup.Id,
            order: 40,
            icon: "history");

        var (reconciliation, reconciliationCreated) = await UpsertMonitoringChildAsync(
            ReconciliationRoute,
            "Conciliación",
            monitoringRoot.Id,
            order: 70,
            icon: "balance");

        await context.SaveChangesAsync();

        // All five NAV2.5B functional routes are explicitly guarded in the current SPA by
        // Admin/ACH.Operator + CanReadAch. Add only those existing authorization identifiers
        // when NAV2.5B creates a menu row; never rewrite authorization on pre-existing rows.
        if (responsesInboxCreated)
            AddAuthorization(responsesInbox.Id, PermissionConfiguration.ReadAchPermissionId);
        if (responsesDashboardCreated)
            AddAuthorization(responsesDashboard.Id, PermissionConfiguration.ReadAchPermissionId);
        if (responsesManualReviewCreated)
            AddAuthorization(responsesManualReview.Id, PermissionConfiguration.ReadAchPermissionId);
        if (responsesAuditCreated)
            AddAuthorization(responsesAudit.Id, PermissionConfiguration.ReadAchPermissionId);
        if (reconciliationCreated)
            AddAuthorization(reconciliation.Id, PermissionConfiguration.ReadAchPermissionId);
    }

    private async Task<(MenuItem Item, bool Created)> UpsertMonitoringChildAsync(
        string route,
        string label,
        int parentId,
        int order,
        string icon)
    {
        var item = await context.MenuItems
            .OrderByDescending(candidate => candidate.IsActive)
            .ThenBy(candidate => candidate.Id)
            .FirstOrDefaultAsync(candidate => candidate.Route == route);
        var created = item is null;

        if (item is null)
        {
            item = new MenuItem
            {
                MenuId = MenuConfiguration.MainMenuId,
                Route = route,
                Icon = icon,
                Exact = true
            };
            context.MenuItems.Add(item);
        }

        Reconcile(item, label, parentId, order);
        return (item, created);
    }

    private async Task ReconcileFileSecurityAsync()
    {
        // NachaSecurityMenuSeeder owns these existing rows and their authorization. NAV2.6
        // only makes their information architecture canonical: label, hierarchy, order and
        // active state. Routes and authorization links are deliberately left untouched.
        var fileSecurity = await context.MenuItems
            .SingleAsync(item => item.Id == MenuItemConfiguration.NachaSecurityId);
        Reconcile(fileSecurity, FileSecurityLabel, parentId: null, order: 60);

        await ReconcileExistingAsync(
            DigitalCertificatesRoute,
            "Certificados digitales",
            fileSecurity.Id,
            order: 10);

        await ReconcileExistingAsync(
            DigitalEnvelopeRoute,
            "Cifrar y descifrar archivos",
            fileSecurity.Id,
            order: 20);
    }

    private async Task ReconcileReportsAsync()
    {
        // ReportsMenuSeeder owns the existing root and its current authorization. NAV2.7
        // makes the domain canonical and publishes only SPA routes that already exist under
        // the same Admin/ACH.Operator + CanReadAch guard. Existing authorization is never rewritten.
        var reports = await context.MenuItems
            .OrderByDescending(item => item.IsActive)
            .ThenBy(item => item.Id)
            .FirstOrDefaultAsync(item => item.Route == ReportsRoute)
            ?? throw new InvalidOperationException(
                $"Navigation route '{ReportsRoute}' must exist before the canonical information-architecture reconciler runs.");

        Reconcile(reports, ReportsLabel, parentId: null, order: 70);
        await context.SaveChangesAsync();

        (string Route, string Label, int Order, string Icon)[] children =
        [
            (ReportsSentRoute, "Transacciones enviadas", 10, "north_east"),
            (ReportsReceivedRoute, "Transacciones recibidas", 20, "south_west"),
            (ReportsReturnsRoute, "Devoluciones", 30, "assignment_return"),
            (ReportsRejectionsRoute, "Rechazos", 40, "block"),
            (ReportsFilesRoute, "Archivos NACHA-M", 50, "description"),
            (ReportsCyclesRoute, "Ciclos", 60, "schedule"),
            (ReportsReconciliationRoute, "Conciliación", 70, "balance"),
            (ReportsTraceabilityRoute, "Trazabilidad ACH", 80, "route"),
            (ReportsHistoryRoute, "Histórico", 90, "history"),
            (ReportsAuditRoute, "Auditoría", 100, "fact_check")
        ];

        foreach (var (route, label, order, icon) in children)
        {
            var (item, created) = await UpsertWithCreationStateAsync(
                route,
                label,
                reports.Id,
                order,
                icon,
                exact: true);

            if (created)
            {
                await context.SaveChangesAsync();
                await MirrorAuthorizationIfEmptyAsync(reports.Id, item.Id);
            }
        }
    }

    private async Task ReconcileAdministrationAsync()
    {
        // NAV2.8A introduces the structural Administración root and canonicalizes the
        // existing Usuarios y acceso branch. NAV2.8B additionally moves the already-seeded
        // /scheduler/tasks MenuItem directly under Administración. NAV2.8C reuses the legacy
        // /logs group plus its audit/authentication children and publishes the existing
        // /navigation-logs SPA surface. NAV2.8D additionally reuses the existing
        // /navigation/menu-items administration entry. NAV2.8E publishes the existing
        // read-only /payment-rail-capability-registry surface. Existing identities, routes
        // and authorization links are preserved; the legacy /scheduler menu group remains
        // hidden because the SPA already redirects it to /scheduler/tasks.
        var administration = await UpsertAsync(
            AdministrationRoute,
            AdministrationLabel,
            parentId: null,
            order: 80,
            icon: "admin_panel_settings",
            exact: false);
        await context.SaveChangesAsync();

        var usersAccess = await context.MenuItems
            .SingleAsync(item => item.Id == MenuItemConfiguration.UsersId);
        if (!string.Equals(usersAccess.Route, UsersRoute, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Expected existing users route '{UsersRoute}' before NAV2.8A reconciliation.");
        }

        Reconcile(usersAccess, UsersAccessLabel, administration.Id, order: 10);

        await ReconcileExistingAsync(
            ManageUsersRoute,
            "Administrar usuarios",
            usersAccess.Id,
            order: 10);

        await ReconcileExistingAsync(
            BrandingRoute,
            "Identidad visual",
            usersAccess.Id,
            order: 20);

        await ReconcileExistingAsync(
            PasswordRulesRoute,
            "Reglas de contraseña",
            usersAccess.Id,
            order: 30);

        await ReconcileExistingAsync(
            LoginLockoutRoute,
            "Bloqueo de acceso",
            usersAccess.Id,
            order: 40);

        var legacySchedulerGroup = await context.MenuItems
            .SingleAsync(item => item.Id == MenuItemConfiguration.SchedulerId);
        if (!string.Equals(legacySchedulerGroup.Route, LegacySchedulerGroupRoute, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Expected existing scheduler group route '{LegacySchedulerGroupRoute}' before NAV2.8B reconciliation.");
        }
        legacySchedulerGroup.IsActive = false;

        var scheduledTasks = await context.MenuItems
            .SingleAsync(item => item.Id == MenuItemConfiguration.TaskDefinitionsId);
        if (!string.Equals(scheduledTasks.Route, ScheduledTasksRoute, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Expected existing scheduled-tasks route '{ScheduledTasksRoute}' before NAV2.8B reconciliation.");
        }

        Reconcile(scheduledTasks, ScheduledTasksLabel, administration.Id, order: 20);

        var systemRecords = await context.MenuItems
            .SingleAsync(item => item.Id == MenuItemConfiguration.LogsId);
        if (!string.Equals(systemRecords.Route, SystemRecordsRoute, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Expected existing system-records group route '{SystemRecordsRoute}' before NAV2.8C reconciliation.");
        }
        Reconcile(systemRecords, SystemRecordsLabel, administration.Id, order: 30);

        var auditLog = await context.MenuItems
            .SingleAsync(item => item.Id == MenuItemConfiguration.AuditLogId);
        if (!string.Equals(auditLog.Route, AuditLogRoute, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Expected existing audit-log route '{AuditLogRoute}' before NAV2.8C reconciliation.");
        }
        Reconcile(auditLog, AuditLogLabel, systemRecords.Id, order: 10);

        var authenticationLog = await context.MenuItems
            .SingleAsync(item => item.Id == MenuItemConfiguration.AuthLogId);
        if (!string.Equals(authenticationLog.Route, AuthenticationLogRoute, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Expected existing authentication-log route '{AuthenticationLogRoute}' before NAV2.8C reconciliation.");
        }
        Reconcile(authenticationLog, AuthenticationLogLabel, systemRecords.Id, order: 20);

        var (navigationLog, navigationLogCreated) = await UpsertWithCreationStateAsync(
            NavigationLogRoute,
            NavigationLogLabel,
            systemRecords.Id,
            order: 30,
            icon: "route",
            exact: true);

        await context.SaveChangesAsync();

        // NAV2.8C originally published the existing navigation-log surface with the same
        // CanReadAch boundary already required by its GET API. Later security increments may
        // align the SPA route or replace this legacy ACH-read capability if a dedicated
        // system-records read permission is approved; this IA seeder does not redesign it.
        if (navigationLogCreated)
        {
            context.MenuItemPermissions.Add(new MenuItemPermission
            {
                MenuItemId = navigationLog.Id,
                PermissionId = PermissionConfiguration.ReadAchPermissionId
            });
        }

        var menuOrganization = await context.MenuItems
            .SingleAsync(item => item.Id == MenuItemConfiguration.NavigationAdminId);
        if (!string.Equals(menuOrganization.Route, MenuOrganizationRoute, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Expected existing menu-organization route '{MenuOrganizationRoute}' before NAV2.8D reconciliation.");
        }

        // The existing MenuItem is already Admin + CanManageUsers. NAV2.8D only changes
        // its label, parent and order; its route and authorization associations remain intact.
        Reconcile(menuOrganization, MenuOrganizationLabel, administration.Id, order: 40);

        var (platformCapabilities, platformCapabilitiesCreated) = await UpsertWithCreationStateAsync(
            PlatformCapabilitiesRoute,
            PlatformCapabilitiesLabel,
            administration.Id,
            order: 50,
            icon: "hub",
            exact: true);

        await context.SaveChangesAsync();

        // NAV2.8E does not create or alter permission definitions. The SPA requires
        // Admin/ACH.Operator plus ANY of CanViewPaymentRailCapabilityRegistry,
        // CanManageAch or CanReadAch. The API action names the dedicated CanView policy,
        // but that policy currently accepts the same three permission claims. The dedicated
        // permission is not a persisted Permission row in the current seed, so the new menu
        // row reuses the existing read permission already accepted by both SPA and API and
        // keeps the SPA's Admin/ACH.Operator role boundary. This is a strict subset of the
        // effective API access and therefore does not expand authorization.
        if (platformCapabilitiesCreated)
        {
            AddAuthorization(platformCapabilities.Id, PermissionConfiguration.ReadAchPermissionId);
        }
    }

    private async Task ReconcileTestToolsAsync()
    {
        // NAV2.9 canonicalizes only the technical surfaces that already exist as SPA routes.
        // The legacy /uat root and inbound simulator already have menu authorization links;
        // keep those links untouched and change only information architecture/copy/order.
        var testTools = await context.MenuItems
            .SingleAsync(item => item.Id == MenuItemConfiguration.UatSimulatorsId);
        if (!string.Equals(testTools.Route, TestToolsRoute, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Expected existing test-tools route '{TestToolsRoute}' before NAV2.9 reconciliation.");
        }
        Reconcile(testTools, TestToolsLabel, parentId: null, order: 90);

        var inboundSimulator = await context.MenuItems
            .SingleAsync(item => item.Id == MenuItemConfiguration.NachaInboundSimulatorId);
        if (!string.Equals(inboundSimulator.Route, InboundFileSimulatorRoute, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Expected existing inbound-simulator route '{InboundFileSimulatorRoute}' before NAV2.9 reconciliation.");
        }
        Reconcile(inboundSimulator, InboundFileSimulatorLabel, testTools.Id, order: 10);

        // /ach/nacha/soap-uat-console is the read-only service-test surface. Keep the canonical
        // row inactive here so it can never be published before the post-NAV2 security reconciler
        // has enforced TestTools.Access as its sole menu authorization. NAV3.3B activates it there.
        var (serviceTests, serviceTestsCreated) = await UpsertWithCreationStateAsync(
            ServiceTestsRoute,
            ServiceTestsLabel,
            testTools.Id,
            order: 20,
            icon: "science",
            exact: true);
        serviceTests.IsActive = false;

        await context.SaveChangesAsync();

        if (serviceTestsCreated)
        {
            AddAuthorization(serviceTests.Id, PermissionConfiguration.ManageAchPermissionId);
        }
    }

    private async Task ReconcileParametersAdministrationAsync()
    {
        var parametersRoot = await UpsertAsync(
            ParametersAdministrationRoute,
            ParametersAdministrationLabel,
            parentId: null,
            order: 50,
            icon: "tune",
            exact: false);
        await context.SaveChangesAsync();

        await ReconcileExistingAsync(
            ClearingHousesRoute,
            "Cámaras compensadoras",
            parametersRoot.Id,
            order: 10);

        await ReconcileExistingAsync(
            ClearingHousePrioritiesRoute,
            "Prioridades por cámara",
            parametersRoot.Id,
            order: 20);

        var cycles = await context.MenuItems.SingleAsync(item => item.Id == MenuItemConfiguration.AchCyclesId);
        Reconcile(cycles, "Ciclos de operación", parametersRoot.Id, order: 30);

        // CyclesMenuSeeder historically creates a second active child using the same
        // /ach-cycles route. Preserve all rows and authorization history, but keep only the
        // canonical AchCyclesId entry visible so the route is represented exactly once.
        var duplicateCycleEntries = await context.MenuItems
            .Where(item => item.Id != cycles.Id && item.Route == cycles.Route)
            .ToListAsync();
        foreach (var duplicate in duplicateCycleEntries)
        {
            duplicate.IsActive = false;
        }

        await ReconcileExistingAsync(
            BankHolidaysRoute,
            "Días no hábiles",
            parametersRoot.Id,
            order: 40);

        await ReconcileExistingAsync(
            OperationalRestrictionsRoute,
            "Restricciones operativas",
            parametersRoot.Id,
            order: 50);

        var nachaConfiguration = await ReconcileExistingAsync(
            NachaConfigurationRoute,
            "Configuración de archivos NACHA-M",
            parametersRoot.Id,
            order: 60);

        await ReconcileExistingAsync(
            NachaConfigProfilesRoute,
            "Perfiles oficiales",
            nachaConfiguration.Id,
            order: 10);

        // NAV5 classifies these two legacy workspace aliases as redirects, not standalone
        // menu destinations. Keep the routes and their authorization history for compatibility,
        // but prevent the final IA reconciler from exposing redirect-only surfaces as menu items.
        await DeactivateMenuRouteAsync(NachaConfigRecordsRedirectRoute);
        await DeactivateMenuRouteAsync(NachaConfigVariantsFieldsRedirectRoute);

        var catalogs = await ReconcileExistingAsync(
            CatalogsRoute,
            "Catálogos",
            parametersRoot.Id,
            order: 70);
        await ReconcileCatalogsAsync(catalogs);

        var connections = await ReconcileExistingAsync(
            ConnectionsRoute,
            "Conexiones",
            parametersRoot.Id,
            order: 80);

        await ReconcileConnectionsAsync(connections);
        await ReconcileStatusMappingsAsync(parametersRoot.Id);
    }


    private async Task ReconcileCatalogsAsync(MenuItem catalogs)
    {
        var financialInstitutions = await context.MenuItems
            .SingleAsync(item => item.Id == MenuItemConfiguration.FinancialInstitutionsId);
        if (!string.Equals(financialInstitutions.Route, FinancialInstitutionsRoute, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Expected existing financial-institutions route '{FinancialInstitutionsRoute}' before NAV2.10B reconciliation.");
        }

        // Reuse the existing financial-institutions MenuItem and authorization links; NAV2.10B
        // changes only its canonical copy/order inside Catalogs.
        Reconcile(financialInstitutions, "Entidades financieras", catalogs.Id, order: 10);

        (string Route, string Label, int Order, string Icon)[] definitions =
        [
            (CompanyEntryDescriptionsRoute, "Conceptos de lote", 20, "list"),
            (DocumentTypesRoute, "Tipos de documento", 30, "badge"),
            (PersonTypesRoute, "Tipos de persona", 40, "apartment"),
            (GenderTypesRoute, "Tipos de género", 50, "diversity_3"),
            (PhoneTypesRoute, "Tipos de teléfono", 60, "call"),
            (EmailTypesRoute, "Tipos de correo", 70, "mail"),
            (AddressTypesRoute, "Tipos de dirección", 80, "location_on"),
            (TransactionCodesRoute, "Códigos de transacción", 90, "swap_horiz")
        ];

        var createdItems = new List<MenuItem>();
        foreach (var definition in definitions)
        {
            var (item, created) = await UpsertWithCreationStateAsync(
                definition.Route,
                definition.Label,
                catalogs.Id,
                definition.Order,
                definition.Icon,
                exact: true);

            if (created)
            {
                createdItems.Add(item);
            }
        }

        // The legacy catalog route is an Angular redirect into the clearing-house context and
        // has already been filtered from the visible client menu. Preserve its route/code/data,
        // but keep it inactive so Catalogs has one canonical set of primary entries from seed.
        var legacySpecialDates = await context.MenuItems
            .SingleOrDefaultAsync(item => item.Route == LegacyClearingHouseSpecialDatesRoute);
        if (legacySpecialDates is not null)
        {
            legacySpecialDates.IsActive = false;
        }

        // Save new MenuItems first so database-generated IDs are available for their
        // authorization links. No existing authorization association is rewritten.
        await context.SaveChangesAsync();

        foreach (var item in createdItems)
        {
            // These administration screens all require CanManageAch in the SPA and their
            // functional CRUD APIs. Reuse the same Admin/ACH.Operator + CanManageAch menu
            // envelope already used by Entidades financieras.
            AddAuthorization(item.Id, PermissionConfiguration.ManageAchPermissionId);
        }

        await context.SaveChangesAsync();
    }

    private async Task ReconcileConnectionsAsync(MenuItem connections)
    {
        var contract = await UpsertAsync(
            ContractRoute,
            "Contrato del servicio",
            connections.Id,
            order: 10,
            icon: "description",
            exact: true);

        var informationRelationship = await UpsertAsync(
            InformationRelationshipRoute,
            "Relación de información",
            connections.Id,
            order: 20,
            icon: "schema",
            exact: true);

        var participantConnection = await UpsertAsync(
            ParticipantConnectionRoute,
            "Conexión entidad participante",
            connections.Id,
            order: 30,
            icon: "link",
            exact: true);

        // Preserve the existing MFT route and its existing authorization links; only
        // relocate and humanize the menu item inside the canonical connections group.
        await ReconcileExistingAsync(
            FileExchangeConfigurationRoute,
            "Configuración del intercambio de archivos",
            connections.Id,
            order: 40);

        var legacySoapItem = await context.MenuItems
            .SingleOrDefaultAsync(item => item.Route == LegacySoapIntegrationsRoute);
        if (legacySoapItem is not null)
        {
            legacySoapItem.IsActive = false;
        }

        await context.SaveChangesAsync();

        // These routes already exist in the SPA but had no menu row. Reuse the current
        // integration menu authorization rather than introducing a new permission model.
        await MirrorAuthorizationIfEmptyAsync(connections.Id, contract.Id);
        await MirrorAuthorizationIfEmptyAsync(connections.Id, informationRelationship.Id);
        await MirrorAuthorizationIfEmptyAsync(connections.Id, participantConnection.Id);
    }

    private async Task ReconcileStatusMappingsAsync(int parametersRootId)
    {
        var statusMappings = await UpsertAsync(
            StatusMappingsRoute,
            "Homologación de estados",
            parametersRootId,
            order: 90,
            icon: "sync_alt",
            exact: true);

        await context.SaveChangesAsync();

        // The existing SPA route is already protected by Admin/Operator + CanReadAch.
        // Reuse those existing role/permission identifiers for the newly published menu entry;
        // NAV2.2 does not create role/permission definitions or change user grants.
        if (!await context.MenuItemPermissions.AnyAsync(link => link.MenuItemId == statusMappings.Id))
        {
            context.MenuItemPermissions.Add(new MenuItemPermission
            {
                MenuItemId = statusMappings.Id,
                PermissionId = PermissionConfiguration.ReadAchPermissionId
            });
        }

        if (!await context.MenuItemRoles.AnyAsync(link => link.MenuItemId == statusMappings.Id))
        {
            context.MenuItemRoles.AddRange(
                new MenuItemRole
                {
                    MenuItemId = statusMappings.Id,
                    RoleId = RoleConfiguration.AdminRoleId
                },
                new MenuItemRole
                {
                    MenuItemId = statusMappings.Id,
                    RoleId = RoleConfiguration.OperatorRoleId
                });
        }
    }


    private async Task DeactivateMenuRouteAsync(string route)
    {
        var items = await context.MenuItems
            .Where(item => item.Route == route)
            .ToListAsync();

        foreach (var item in items)
        {
            item.IsActive = false;
        }
    }

    private async Task<MenuItem> ReconcileExistingAsync(
        string route,
        string label,
        int parentId,
        int order)
    {
        var item = await context.MenuItems
            .OrderByDescending(candidate => candidate.IsActive)
            .ThenBy(candidate => candidate.Id)
            .FirstOrDefaultAsync(candidate => candidate.Route == route)
            ?? throw new InvalidOperationException(
                $"Navigation route '{route}' must exist before the canonical information-architecture reconciler runs.");

        Reconcile(item, label, parentId, order);
        return item;
    }

    private static void Reconcile(MenuItem item, string label, int? parentId, int order)
    {
        item.MenuId = MenuConfiguration.MainMenuId;
        item.ParentId = parentId;
        item.Label = label;
        item.Order = order;
        item.IsActive = true;
    }

    private async Task<MenuItem> UpsertAsync(
        string route,
        string label,
        int? parentId,
        int order,
        string icon,
        bool exact)
    {
        var (item, _) = await UpsertWithCreationStateAsync(route, label, parentId, order, icon, exact);
        return item;
    }

    private async Task<(MenuItem Item, bool Created)> UpsertWithCreationStateAsync(
        string route,
        string label,
        int? parentId,
        int order,
        string icon,
        bool exact)
    {
        var item = await context.MenuItems
            .OrderByDescending(candidate => candidate.IsActive)
            .ThenBy(candidate => candidate.Id)
            .FirstOrDefaultAsync(candidate => candidate.Route == route);
        var created = item is null;

        if (item is null)
        {
            item = new MenuItem
            {
                Route = route
            };
            context.MenuItems.Add(item);
        }

        item.MenuId = MenuConfiguration.MainMenuId;
        item.ParentId = parentId;
        item.Label = label;
        item.Icon ??= icon;
        item.Order = order;
        item.Exact = exact;
        item.IsActive = true;

        return (item, created);
    }

    private void AddAuthorization(int menuItemId, Guid permissionId)
    {
        context.MenuItemPermissions.Add(new MenuItemPermission
        {
            MenuItemId = menuItemId,
            PermissionId = permissionId
        });

        context.MenuItemRoles.AddRange(
            new MenuItemRole
            {
                MenuItemId = menuItemId,
                RoleId = RoleConfiguration.AdminRoleId
            },
            new MenuItemRole
            {
                MenuItemId = menuItemId,
                RoleId = RoleConfiguration.OperatorRoleId
            });
    }

    private async Task MirrorAuthorizationIfEmptyAsync(int sourceMenuItemId, int targetMenuItemId)
    {
        if (!await context.MenuItemPermissions.AnyAsync(link => link.MenuItemId == targetMenuItemId))
        {
            var permissionIds = await context.MenuItemPermissions
                .Where(link => link.MenuItemId == sourceMenuItemId)
                .Select(link => link.PermissionId)
                .ToListAsync();

            context.MenuItemPermissions.AddRange(permissionIds.Select(permissionId => new MenuItemPermission
            {
                MenuItemId = targetMenuItemId,
                PermissionId = permissionId
            }));
        }

        if (!await context.MenuItemRoles.AnyAsync(link => link.MenuItemId == targetMenuItemId))
        {
            var roleIds = await context.MenuItemRoles
                .Where(link => link.MenuItemId == sourceMenuItemId)
                .Select(link => link.RoleId)
                .ToListAsync();

            context.MenuItemRoles.AddRange(roleIds.Select(roleId => new MenuItemRole
            {
                MenuItemId = targetMenuItemId,
                RoleId = roleId
            }));
        }
    }
}
