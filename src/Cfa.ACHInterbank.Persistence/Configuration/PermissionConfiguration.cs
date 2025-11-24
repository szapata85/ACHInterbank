using Cfa.ACHInterbank.Domain.Entities.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public static readonly Guid ManageAchPermissionId = Guid.Parse("a6c3bd53-111a-48a3-8d4a-2d1a37c4b86a");
    public static readonly Guid ReadAchPermissionId = Guid.Parse("4f0cbde9-1b2e-4ad8-b8e6-62f0a1cd6cf7");

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
                Name = "ach.manage",
                Description = "Gestión completa de operaciones ACH"
            },
            new Permission
            {
                Id = ReadAchPermissionId,
                Name = "ach.read",
                Description = "Consulta de operaciones ACH"
            });
    }
}
