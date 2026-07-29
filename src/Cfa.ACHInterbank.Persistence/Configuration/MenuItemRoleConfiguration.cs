using Cfa.ACHInterbank.Domain.Entities.Navigation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public class MenuItemRoleConfiguration : IEntityTypeConfiguration<MenuItemRole>
{
    public void Configure(EntityTypeBuilder<MenuItemRole> builder)
    {
        builder.ToTable("MenuItemRoles");

        builder.HasKey(x => new { x.MenuItemId, x.RoleId });

        builder.HasOne(x => x.MenuItem)
            .WithMany(x => x.MenuItemRoles)
            .HasForeignKey(x => x.MenuItemId);

        builder.HasOne(x => x.Role)
            .WithMany()
            .HasForeignKey(x => x.RoleId);

        builder.HasData(
            new MenuItemRole
            {
                MenuItemId = MenuItemConfiguration.UsersId,
                RoleId = RoleConfiguration.AdminRoleId
            },
            new MenuItemRole
            {
                MenuItemId = MenuItemConfiguration.TransactionsId,
                RoleId = RoleConfiguration.AdminRoleId
            },
            new MenuItemRole
            {
                MenuItemId = MenuItemConfiguration.TransactionsId,
                RoleId = RoleConfiguration.OperatorRoleId
            },
            new MenuItemRole
            {
                MenuItemId = MenuItemConfiguration.TransactionsListId,
                RoleId = RoleConfiguration.AdminRoleId
            },
            new MenuItemRole
            {
                MenuItemId = MenuItemConfiguration.TransactionsListId,
                RoleId = RoleConfiguration.OperatorRoleId
            },
            new MenuItemRole
            {
                MenuItemId = MenuItemConfiguration.TransactionsCreateId,
                RoleId = RoleConfiguration.AdminRoleId
            },
            new MenuItemRole
            {
                MenuItemId = MenuItemConfiguration.TransactionsCreateId,
                RoleId = RoleConfiguration.OperatorRoleId
            },
            new MenuItemRole
            {
                MenuItemId = MenuItemConfiguration.CustomerThirdPartiesId,
                RoleId = RoleConfiguration.AdminRoleId
            },
            new MenuItemRole
            {
                MenuItemId = MenuItemConfiguration.CustomerThirdPartiesId,
                RoleId = RoleConfiguration.OperatorRoleId
            },
            new MenuItemRole
            {
                MenuItemId = MenuItemConfiguration.UatSimulatorsId,
                RoleId = RoleConfiguration.AdminRoleId
            },
            new MenuItemRole
            {
                MenuItemId = MenuItemConfiguration.UatSimulatorsId,
                RoleId = RoleConfiguration.OperatorRoleId
            },
            new MenuItemRole
            {
                MenuItemId = MenuItemConfiguration.NachaInboundSimulatorId,
                RoleId = RoleConfiguration.AdminRoleId
            },
            new MenuItemRole
            {
                MenuItemId = MenuItemConfiguration.NachaInboundSimulatorId,
                RoleId = RoleConfiguration.OperatorRoleId
            },
            new MenuItemRole
            {
                MenuItemId = MenuItemConfiguration.NavigationAdminId,
                RoleId = RoleConfiguration.AdminRoleId
            },
            new MenuItemRole
            {
                MenuItemId = MenuItemConfiguration.BrandingId,
                RoleId = RoleConfiguration.AdminRoleId
            },
            new MenuItemRole
            {
                MenuItemId = MenuItemConfiguration.PasswordRulesId,
                RoleId = RoleConfiguration.AdminRoleId
            },
            new MenuItemRole
            {
                MenuItemId = MenuItemConfiguration.LoginLockoutSettingsId,
                RoleId = RoleConfiguration.AdminRoleId
            },
            new MenuItemRole
            {
                MenuItemId = MenuItemConfiguration.IntegrationsId,
                RoleId = RoleConfiguration.AdminRoleId
            },
            new MenuItemRole
            {
                MenuItemId = MenuItemConfiguration.SoapIntegrationSettingsId,
                RoleId = RoleConfiguration.AdminRoleId
            },
            new MenuItemRole
            {
                MenuItemId = MenuItemConfiguration.FinancialInstitutionsId,
                RoleId = RoleConfiguration.AdminRoleId
            },
            new MenuItemRole
            {
                MenuItemId = MenuItemConfiguration.FinancialInstitutionsId,
                RoleId = RoleConfiguration.OperatorRoleId
            },
            new MenuItemRole
            {
                MenuItemId = MenuItemConfiguration.ClearingHousePreferencesId,
                RoleId = RoleConfiguration.AdminRoleId
            },
            new MenuItemRole
            {
                MenuItemId = MenuItemConfiguration.ClearingHousePreferencesId,
                RoleId = RoleConfiguration.OperatorRoleId
            });
    }
}
