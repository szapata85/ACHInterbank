using Cfa.ACHInterbank.Domain.Entities.Navigation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public class MenuItemPermissionConfiguration : IEntityTypeConfiguration<MenuItemPermission>
{
    public void Configure(EntityTypeBuilder<MenuItemPermission> builder)
    {
        builder.ToTable("MenuItemPermissions");

        builder.HasKey(x => new { x.MenuItemId, x.PermissionId });

        builder.HasOne(x => x.MenuItem)
            .WithMany(x => x.MenuItemPermissions)
            .HasForeignKey(x => x.MenuItemId);

        builder.HasOne(x => x.Permission)
            .WithMany()
            .HasForeignKey(x => x.PermissionId);

        builder.HasData(
            new MenuItemPermission
            {
                MenuItemId = MenuItemConfiguration.UsersId,
                PermissionId = PermissionConfiguration.ManageUsersPermissionId
            },
            new MenuItemPermission
            {
                MenuItemId = MenuItemConfiguration.NavigationAdminId,
                PermissionId = PermissionConfiguration.ManageUsersPermissionId
            },
            new MenuItemPermission
            {
                MenuItemId = MenuItemConfiguration.FinancialInstitutionsId,
                PermissionId = PermissionConfiguration.ManageAchPermissionId
            },
            new MenuItemPermission
            {
                MenuItemId = MenuItemConfiguration.ClearingHousePreferencesId,
                PermissionId = PermissionConfiguration.ManageAchPermissionId
            },
            new MenuItemPermission
            {
                MenuItemId = MenuItemConfiguration.BankHolidaysId,
                PermissionId = PermissionConfiguration.ManageAchPermissionId
            },
            new MenuItemPermission
            {
                MenuItemId = MenuItemConfiguration.ClearingHouseSpecialDatesId,
                PermissionId = PermissionConfiguration.ManageAchPermissionId
            },
            new MenuItemPermission
            {
                MenuItemId = MenuItemConfiguration.BrandingId,
                PermissionId = PermissionConfiguration.ManageUsersPermissionId
            },
            new MenuItemPermission
            {
                MenuItemId = MenuItemConfiguration.AchCyclesId,
                PermissionId = PermissionConfiguration.ReadAchPermissionId
            },
            new MenuItemPermission
            {
                MenuItemId = MenuItemConfiguration.NachaExportId,
                PermissionId = PermissionConfiguration.ReadAchPermissionId
            },
            new MenuItemPermission
            {
                MenuItemId = MenuItemConfiguration.NachaLayoutsId,
                PermissionId = PermissionConfiguration.ReadAchPermissionId
            },
            new MenuItemPermission
            {
                MenuItemId = MenuItemConfiguration.NachaDefinitionsId,
                PermissionId = PermissionConfiguration.ReadAchPermissionId
            },
            new MenuItemPermission
            {
                MenuItemId = MenuItemConfiguration.NachaConfigRecordsId,
                PermissionId = PermissionConfiguration.ReadAchPermissionId
            },
            new MenuItemPermission
            {
                MenuItemId = MenuItemConfiguration.NachaConfigVariantsFieldsId,
                PermissionId = PermissionConfiguration.ReadAchPermissionId
            },
            new MenuItemPermission
            {
                MenuItemId = MenuItemConfiguration.CatalogsId,
                PermissionId = PermissionConfiguration.ReadCatalogsPermissionId
            },
            new MenuItemPermission
            {
                MenuItemId = MenuItemConfiguration.TransactionsId,
                PermissionId = PermissionConfiguration.ManageAchPermissionId
            },
            new MenuItemPermission
            {
                MenuItemId = MenuItemConfiguration.TransactionsId,
                PermissionId = PermissionConfiguration.ReadAchPermissionId
            },
            new MenuItemPermission
            {
                MenuItemId = MenuItemConfiguration.TransactionsListId,
                PermissionId = PermissionConfiguration.ManageAchPermissionId
            },
            new MenuItemPermission
            {
                MenuItemId = MenuItemConfiguration.TransactionsListId,
                PermissionId = PermissionConfiguration.ReadAchPermissionId
            },
            new MenuItemPermission
            {
                MenuItemId = MenuItemConfiguration.TransactionsCreateId,
                PermissionId = PermissionConfiguration.ManageAchPermissionId
            },
            new MenuItemPermission
            {
                MenuItemId = MenuItemConfiguration.TransactionsCreateId,
                PermissionId = PermissionConfiguration.ReadAchPermissionId
            },
            new MenuItemPermission
            {
                MenuItemId = MenuItemConfiguration.CustomerThirdPartiesId,
                PermissionId = PermissionConfiguration.ManageAchPermissionId
            },
            new MenuItemPermission
            {
                MenuItemId = MenuItemConfiguration.CustomerThirdPartiesId,
                PermissionId = PermissionConfiguration.ReadAchPermissionId
            },
            new MenuItemPermission
            {
                MenuItemId = MenuItemConfiguration.ClearingHouseTransactionRulesId,
                PermissionId = PermissionConfiguration.ManageAchPermissionId
            },
            new MenuItemPermission
            {
                MenuItemId = MenuItemConfiguration.ClearingHouseTransactionRulesId,
                PermissionId = PermissionConfiguration.ReadAchPermissionId
            },
            new MenuItemPermission
            {
                MenuItemId = MenuItemConfiguration.UatSimulatorsId,
                PermissionId = PermissionConfiguration.ManageAchPermissionId
            },
            new MenuItemPermission
            {
                MenuItemId = MenuItemConfiguration.UatSimulatorsId,
                PermissionId = PermissionConfiguration.ReadAchPermissionId
            },
            new MenuItemPermission
            {
                MenuItemId = MenuItemConfiguration.NachaInboundSimulatorId,
                PermissionId = PermissionConfiguration.ManageAchPermissionId
            },
            new MenuItemPermission
            {
                MenuItemId = MenuItemConfiguration.NachaInboundSimulatorId,
                PermissionId = PermissionConfiguration.ReadAchPermissionId
            },
            new MenuItemPermission
            {
                MenuItemId = MenuItemConfiguration.SchedulerId,
                PermissionId = PermissionConfiguration.ManageAchPermissionId
            },
            new MenuItemPermission
            {
                MenuItemId = MenuItemConfiguration.TaskDefinitionsId,
                PermissionId = PermissionConfiguration.ManageAchPermissionId
            },
            new MenuItemPermission
            {
                MenuItemId = MenuItemConfiguration.AuditLogId,
                PermissionId = PermissionConfiguration.ManageUsersPermissionId
            },
            new MenuItemPermission
            {
                MenuItemId = MenuItemConfiguration.AuthLogId,
                PermissionId = PermissionConfiguration.ManageUsersPermissionId
            },
            new MenuItemPermission
            {
                MenuItemId = MenuItemConfiguration.LogsId,
                PermissionId = PermissionConfiguration.ManageUsersPermissionId
            },
            new MenuItemPermission
            {
                MenuItemId = MenuItemConfiguration.PasswordRulesId,
                PermissionId = PermissionConfiguration.ManageUsersPermissionId
            },
            new MenuItemPermission
            {
                MenuItemId = MenuItemConfiguration.LoginLockoutSettingsId,
                PermissionId = PermissionConfiguration.ManageUsersPermissionId
            },
            new MenuItemPermission
            {
                MenuItemId = MenuItemConfiguration.IntegrationsId,
                PermissionId = PermissionConfiguration.ManageUsersPermissionId
            },
            new MenuItemPermission
            {
                MenuItemId = MenuItemConfiguration.SoapIntegrationSettingsId,
                PermissionId = PermissionConfiguration.ManageUsersPermissionId
            });
    }
}
