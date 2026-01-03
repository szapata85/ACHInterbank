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
                Label = "Scheduler",
                Route = "/scheduler",
                Icon = "timer",
                Order = 9,
                Exact = false,
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
            });
    }
}
