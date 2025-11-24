using Cfa.ACHInterbank.Domain.Entities.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public static readonly Guid AdminRoleId = Guid.Parse("1f8602da-6415-43f8-b61d-cb396f8577f1");
    public static readonly Guid OperatorRoleId = Guid.Parse("a51746c2-0710-4d79-97b1-5b4368326f56");

    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Description).HasMaxLength(250);

        builder.HasIndex(x => x.Name).IsUnique();

        builder.HasData(
            new Role
            {
                Id = AdminRoleId,
                Name = "Admin",
                Description = "Acceso completo al sistema"
            },
            new Role
            {
                Id = OperatorRoleId,
                Name = "Operator",
                Description = "Operaciones básicas sobre ACH"
            });
    }
}
