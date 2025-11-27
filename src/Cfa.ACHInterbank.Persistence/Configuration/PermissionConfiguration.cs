using Cfa.ACHInterbank.Domain.Entities.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public static readonly Guid ManageAchPermissionId = Guid.Parse("a6c3bd53-111a-48a3-8d4a-2d1a37c4b86a");
    public static readonly Guid ReadAchPermissionId = Guid.Parse("4f0cbde9-1b2e-4ad8-b8e6-62f0a1cd6cf7");
    public static readonly Guid ReadAliasesPermissionId = Guid.Parse("0ee0a30f-48b6-4b8c-9afd-0dfb3d4770e7");
    public static readonly Guid ReadCatalogsPermissionId = Guid.Parse("dd0e54be-b6df-4ab3-8783-0f72b6e774a2");
    public static readonly Guid ManageUsersPermissionId = Guid.Parse("b5d45f3c-8ac2-4a8b-80d1-315063e27fdf");

    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("Permissions");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(150);
        builder.Property(x => x.Description).HasMaxLength(250);

        builder.HasIndex(x => x.Name).IsUnique();

        builder.HasData(
            new Permission
            {
                Id = ManageAchPermissionId,
                Name = "CanManageAch",
                Description = "Gestión completa de operaciones ACH"
            },
            new Permission
            {
                Id = ReadAchPermissionId,
                Name = "CanReadAch",
                Description = "Consulta de operaciones ACH"
            },
            new Permission
            {
                Id = ReadAliasesPermissionId,
                Name = "CanReadAliases",
                Description = "Consulta de alias"
            },
            new Permission
            {
                Id = ReadCatalogsPermissionId,
                Name = "CanReadCatalogs",
                Description = "Consulta de catálogos"
            },
            new Permission
            {
                Id = ManageUsersPermissionId,
                Name = "CanManageUsers",
                Description = "Gestión de usuarios"
            });
    }
}
