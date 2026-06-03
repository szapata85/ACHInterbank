using Cfa.ACHInterbank.Domain.Entities.Navigation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public class MenuItemConfiguration : IEntityTypeConfiguration<MenuItem>
{
    public const int DashboardId = 1;
    public const int UsersId = 2;
    public const int AliasesId = 3;
    public const int AchCyclesId = 4;
    public const int CatalogsId = 5;
    public const int TransactionsId = 6;
    public const int TransactionsCreateId = 7;
    public const int TransactionsListId = 8;
    public const int NavigationAdminId = 9;
    public const int NachaSecurityId = 10;
    public const int NachaExportId = 11;
    public const int BrandingId = 12;
    public const int FinancialInstitutionsId = 13;
    public const int ClearingHousePreferencesId = 14;
    public const int BankHolidaysId = 15;
    public const int ClearingHouseSpecialDatesId = 16;
    public const int NachaUploadId = 17;
    public const int SchedulerId = 18;
    public const int TaskDefinitionsId = 19;
    public const int NachaLayoutsId = 20;
    public const int DigitalEnvelopeId = 21;
    public const int AuditLogId = 22;
    public const int AuthLogId = 26;
    public const int PasswordRulesId = 23;
    public const int LoginLockoutSettingsId = 24;
    public const int NachaDefinitionsId = 25;
    public const int LogsId = 27;
    public const int CustomerThirdPartiesId = 28;
    public const int IntegrationsId = 29;
    public const int SoapIntegrationSettingsId = 30;
    public const int TransactionsReturnsId = 31;
    public const int ClearingHouseTransactionRulesId = 32;
    public const int UatSimulatorsId = 33;
    public const int NachaInboundSimulatorId = 34;
    public const int NachaConfigRecordsId = 2802;
    public const int NachaConfigVariantsFieldsId = 2803;

    public void Configure(EntityTypeBuilder<MenuItem> builder)
    {
        builder.ToTable("MenuItems");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.Label)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Route)
            .IsRequired()
            .HasMaxLength(400);

        builder.Property(x => x.Icon)
            .HasMaxLength(100);

        builder.Property(x => x.Order)
            .HasDefaultValue(0);

        builder.Property(x => x.IsActive)
            .HasDefaultValue(true);

        builder.HasOne(x => x.Parent)
            .WithMany(x => x.Children)
            .HasForeignKey(x => x.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasData(
            new MenuItem
            {
                Id = DashboardId,
                MenuId = MenuConfiguration.MainMenuId,
                Label = "Dashboard",
                Route = "/dashboard",
                Icon = "dashboard",
                Order = 1,
                Exact = true,
                IsActive = true
            },
            new MenuItem
            {
                Id = UsersId,
                MenuId = MenuConfiguration.MainMenuId,
                Label = "Usuarios",
                Route = "/users",
                Icon = "group",
                Order = 2,
                Exact = false,
                IsActive = true
            },
            new MenuItem
            {
                Id = BrandingId,
                MenuId = MenuConfiguration.MainMenuId,
                ParentId = UsersId,
                Label = "Identidad y colores",
                Route = "/users/branding",
                Icon = "palette",
                Order = 2,
                Exact = true,
                IsActive = true
            },
            new MenuItem
            {
                Id = PasswordRulesId,
                MenuId = MenuConfiguration.MainMenuId,
                ParentId = UsersId,
                Label = "Reglas de contraseña",
                Route = "/users/password-rules",
                Icon = "policy",
                Order = 3,
                Exact = true,
                IsActive = true
            },
            new MenuItem
            {
                Id = LoginLockoutSettingsId,
                MenuId = MenuConfiguration.MainMenuId,
                ParentId = UsersId,
                Label = "Bloqueo de acceso",
                Route = "/users/login-lockout",
                Icon = "lock_clock",
                Order = 4,
                Exact = true,
                IsActive = true
            },
            new MenuItem
            {
                Id = IntegrationsId,
                MenuId = MenuConfiguration.MainMenuId,
                Label = "Integraciones",
                Route = "/integraciones",
                Icon = "hub",
                Order = 5,
                Exact = false,
                IsActive = true
            },
            new MenuItem
            {
                Id = SoapIntegrationSettingsId,
                MenuId = MenuConfiguration.MainMenuId,
                ParentId = IntegrationsId,
                Label = "Integraciones SOAP",
                Route = "/soap-integrations",
                Icon = "settings_ethernet",
                Order = 1,
                Exact = true,
                IsActive = true
            },
            new MenuItem
            {
                Id = AchCyclesId,
                MenuId = MenuConfiguration.MainMenuId,
                Label = "Ciclos ACH",
                Route = "/ach-cycles",
                Icon = "schedule",
                Order = 4,
                Exact = true,
                IsActive = true
            },
            new MenuItem
            {
                Id = NachaExportId,
                MenuId = MenuConfiguration.MainMenuId,
                ParentId = AchCyclesId,
                Label = "Exportar NACHA",
                Route = "/ach-cycles/nacha/export",
                Icon = "download", 
                Order = 1,
                Exact = true,
                IsActive = true
            },
            new MenuItem
            {
                Id = NachaLayoutsId,
                MenuId = MenuConfiguration.MainMenuId,
                Label = "NACHA-M ConfiguraciÃ³n",
                Route = "/nacha-config-admin/perfiles",
                Icon = "tune",
                Order = 2,
                Exact = true,
                IsActive = true
            },
            new MenuItem
            {
                Id = NachaDefinitionsId,
                MenuId = MenuConfiguration.MainMenuId,
                ParentId = NachaLayoutsId,
                Label = "Perfiles oficiales",
                Route = "/nacha-config-admin/perfiles",
                Icon = "fact_check",
                Order = 3,
                Exact = true,
                IsActive = true
            },
            new MenuItem
            {
                Id = NachaConfigRecordsId,
                MenuId = MenuConfiguration.MainMenuId,
                ParentId = NachaLayoutsId,
                Label = "Records oficiales",
                Route = "/nacha-config-admin/records",
                Icon = "view_list",
                Order = 4,
                Exact = true,
                IsActive = true
            },
            new MenuItem
            {
                Id = NachaConfigVariantsFieldsId,
                MenuId = MenuConfiguration.MainMenuId,
                ParentId = NachaLayoutsId,
                Label = "Variants y Fields",
                Route = "/nacha-config-admin/variants-fields",
                Icon = "schema",
                Order = 5,
                Exact = true,
                IsActive = true
            },
            new MenuItem
            {
                Id = CatalogsId,
                MenuId = MenuConfiguration.MainMenuId,
                Label = "Catálogos",
                Route = "/catalogs",
                Icon = "inventory",
                Order = 5,
                Exact = false,
                IsActive = true
            },
            new MenuItem
            {
                Id = FinancialInstitutionsId,
                MenuId = MenuConfiguration.MainMenuId,
                ParentId = CatalogsId,
                Label = "Instituciones financieras",
                Route = "/catalogs/financial-institutions",
                Icon = "account_balance",
                Order = 1,
                Exact = true,
                IsActive = true
            },
            new MenuItem
            {
                Id = ClearingHousePreferencesId,
                MenuId = MenuConfiguration.MainMenuId,
                ParentId = CatalogsId,
                Label = "Prioridades cámaras",
                Route = "/catalogs/clearing-house-preferences",
                Icon = "tune",
                Order = 2,
                Exact = true,
                IsActive = true
            },
            new MenuItem
            {
                Id = BankHolidaysId,
                MenuId = MenuConfiguration.MainMenuId,
                ParentId = CatalogsId,
                Label = "Festivos bancarios",
                Route = "/catalogs/bank-holidays",
                Icon = "event",
                Order = 3,
                Exact = true,
                IsActive = true
            },
            new MenuItem
            {
                Id = ClearingHouseSpecialDatesId,
                MenuId = MenuConfiguration.MainMenuId,
                ParentId = CatalogsId,
                Label = "Fechas especiales cámaras",
                Route = "/catalogs/clearing-house-special-dates",
                Icon = "event_busy",
                Order = 4,
                Exact = true,
                IsActive = true
            },
            new MenuItem
            {
                Id = TransactionsId,
                MenuId = MenuConfiguration.MainMenuId,
                Label = "Transacciones",
                Route = "/transactions",
                Icon = "swap_horiz",
                Order = 6,
                Exact = false,
                IsActive = true
            },
            new MenuItem
            {
                Id = NavigationAdminId,
                MenuId = MenuConfiguration.MainMenuId,
                Label = "Navegación",
                Route = "/navigation/menu-items",
                Icon = "category",
                Order = 7,
                Exact = true,
                IsActive = true
            },
            new MenuItem
            {
                Id = NachaSecurityId,
                MenuId = MenuConfiguration.MainMenuId,
                Label = "Seguridad NACHA",
                Route = "/nacha-security/certificates",
                Icon = "security",
                Order = 8,
                Exact = true,
                IsActive = true
            },
            new MenuItem
            {
                Id = SchedulerId,
                MenuId = MenuConfiguration.MainMenuId,
                Label = "Programador",
                Route = "/scheduler",
                Icon = "timer",
                Order = 9,
                Exact = false,
                IsActive = true
            },
            new MenuItem
            {
                Id = LogsId,
                MenuId = MenuConfiguration.MainMenuId,
                Label = "Logs",
                Route = "/logs",
                Icon = "summarize",
                Order = 10,
                Exact = false,
                IsActive = true
            },
            new MenuItem
            {
                Id = UatSimulatorsId,
                MenuId = MenuConfiguration.MainMenuId,
                Label = "UAT / Simuladores",
                Route = "/uat",
                Icon = "science",
                Order = 11,
                Exact = false,
                IsActive = true
            },
            new MenuItem
            {
                Id = NachaInboundSimulatorId,
                MenuId = MenuConfiguration.MainMenuId,
                ParentId = UatSimulatorsId,
                Label = "Simulador NACHA-M Entrada",
                Route = "/uat/nacha-inbound-simulator",
                Icon = "file_download",
                Order = 1,
                Exact = true,
                IsActive = true
            },
            new MenuItem
            {
                Id = AuditLogId,
                MenuId = MenuConfiguration.MainMenuId,
                ParentId = LogsId,
                Label = "Auditoría",
                Route = "/audit-logs",
                Icon = "history",
                Order = 1,
                Exact = true,
                IsActive = true
            },
            new MenuItem
            {
                Id = AuthLogId,
                MenuId = MenuConfiguration.MainMenuId,
                ParentId = LogsId,
                Label = "Autenticaciones",
                Route = "/auth-logs",
                Icon = "login",
                Order = 2,
                Exact = true,
                IsActive = true
            },
            new MenuItem
            {
                Id = TaskDefinitionsId,
                MenuId = MenuConfiguration.MainMenuId,
                ParentId = SchedulerId,
                Label = "Tareas programadas",
                Route = "/scheduler/tasks",
                Icon = "schedule",
                Order = 1,
                Exact = true,
                IsActive = true
            },
            new MenuItem
            {
                Id = TransactionsListId,
                MenuId = MenuConfiguration.MainMenuId,
                ParentId = TransactionsId,
                Label = "Consultar transacciones",
                Route = "/transactions/list",
                Icon = "list_alt",
                Order = 2,
                Exact = true,
                IsActive = true
            },
            new MenuItem
            {
                Id = TransactionsCreateId,
                MenuId = MenuConfiguration.MainMenuId,
                ParentId = TransactionsId,
                Label = "Crear transacción",
                Route = "/transactions/create",
                Icon = "note_add",
                Order = 1,
                Exact = true,
                IsActive = true
            },
            new MenuItem
            {
                Id = NachaUploadId,
                MenuId = MenuConfiguration.MainMenuId,
                ParentId = TransactionsId,
                Label = "Cargar NACHA-M",
                Route = "/transactions/nacha-upload",
                Icon = "upload_file",
                Order = 3,
                Exact = true,
                IsActive = true
            },
            new MenuItem
            {
                Id = CustomerThirdPartiesId,
                MenuId = MenuConfiguration.MainMenuId,
                ParentId = TransactionsId,
                Label = "Terceros prenotificación",
                Route = "/customer-third-parties",
                Icon = "groups",
                Order = 4,
                Exact = true,
                IsActive = true
            },
            new MenuItem
            {
                Id = TransactionsReturnsId,
                MenuId = MenuConfiguration.MainMenuId,
                ParentId = TransactionsId,
                Label = "Gestión devoluciones",
                Route = "/transactions/returns",
                Icon = "assignment_return",
                Order = 5,
                Exact = true,
                IsActive = true
            },
            new MenuItem
            {
                Id = ClearingHouseTransactionRulesId,
                MenuId = MenuConfiguration.MainMenuId,
                ParentId = TransactionsId,
                Label = "Reglas por camara",
                Route = "/transactions/clearing-house-rules",
                Icon = "rule",
                Order = 6,
                Exact = true,
                IsActive = true
            },
            new MenuItem
            {
                Id = DigitalEnvelopeId,
                MenuId = MenuConfiguration.MainMenuId,
                ParentId = NachaSecurityId,
                Label = "Sobre digital",
                Route = "/nacha-security/sobre-digital",
                Icon = "lock",
                Order = 1,
                Exact = true,
                IsActive = true
            });
    }
}
