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
                MenuItemId = MenuItemConfiguration.TransactionsCreateId,
                RoleId = RoleConfiguration.AdminRoleId
            },
            new MenuItemRole
            {
                MenuItemId = MenuItemConfiguration.TransactionsCreateId,
                RoleId = RoleConfiguration.OperatorRoleId
            });
    }
}
