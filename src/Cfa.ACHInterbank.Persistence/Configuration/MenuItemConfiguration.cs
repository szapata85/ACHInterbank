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
                Exact = true,
                IsActive = true
            },
            new MenuItem
            {
                Id = AliasesId,
                MenuId = MenuConfiguration.MainMenuId,
                Label = "Alias",
                Route = "/aliases",
                Icon = "key",
                Order = 3,
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
                Id = CatalogsId,
                MenuId = MenuConfiguration.MainMenuId,
                Label = "Catálogos",
                Route = "/catalogs",
                Icon = "inventory",
                Order = 5,
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
            });
    }
}
