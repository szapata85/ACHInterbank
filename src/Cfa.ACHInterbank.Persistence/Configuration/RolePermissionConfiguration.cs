using Cfa.ACHInterbank.Domain.Entities.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("RolePermissions");

        builder.HasKey(x => new { x.RoleId, x.PermissionId });

        builder.HasOne(x => x.Role)
            .WithMany(r => r.RolePermissions)
            .HasForeignKey(x => x.RoleId);

        builder.HasOne(x => x.Permission)
            .WithMany(p => p.RolePermissions)
            .HasForeignKey(x => x.PermissionId);

        builder.HasData(
            new RolePermission
            {
                RoleId = RoleConfiguration.AdminRoleId,
                PermissionId = PermissionConfiguration.ManageAchPermissionId
            },
            new RolePermission
            {
                RoleId = RoleConfiguration.AdminRoleId,
                PermissionId = PermissionConfiguration.ReadAchPermissionId
            },
            new RolePermission
            {
                RoleId = RoleConfiguration.AdminRoleId,
                PermissionId = PermissionConfiguration.ReadAliasesPermissionId
            },
            new RolePermission
            {
                RoleId = RoleConfiguration.AdminRoleId,
                PermissionId = PermissionConfiguration.ReadCatalogsPermissionId
            },
            new RolePermission
            {
                RoleId = RoleConfiguration.AdminRoleId,
                PermissionId = PermissionConfiguration.ManageUsersPermissionId
            },
            new RolePermission
            {
                RoleId = RoleConfiguration.AdminRoleId,
                PermissionId = PermissionConfiguration.ManageCertificatesPermissionId
            },
            new RolePermission
            {
                RoleId = RoleConfiguration.OperatorRoleId,
                PermissionId = PermissionConfiguration.ManageAchPermissionId
            },
            new RolePermission
            {
                RoleId = RoleConfiguration.OperatorRoleId,
                PermissionId = PermissionConfiguration.ReadAchPermissionId
            },
            new RolePermission
            {
                RoleId = RoleConfiguration.OperatorRoleId,
                PermissionId = PermissionConfiguration.ReadAliasesPermissionId
            },
            new RolePermission
            {
                RoleId = RoleConfiguration.OperatorRoleId,
                PermissionId = PermissionConfiguration.ReadCatalogsPermissionId
            });
    }
}
