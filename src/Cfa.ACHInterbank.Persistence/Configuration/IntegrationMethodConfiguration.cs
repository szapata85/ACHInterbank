using Cfa.ACHInterbank.Domain.Entities.Integrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public class IntegrationMethodConfiguration : IEntityTypeConfiguration<IntegrationMethod>
{
    public void Configure(EntityTypeBuilder<IntegrationMethod> builder)
    {
        builder.ToTable("IntegrationMethods");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code).HasMaxLength(150).IsRequired();
        builder.Property(x => x.DisplayName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.SoapClientCode).HasMaxLength(120).IsRequired();
        builder.Property(x => x.IsActive).HasDefaultValue(true);

        builder.HasIndex(x => x.Code).IsUnique();

        builder.HasMany(x => x.Parameters)
            .WithOne(x => x.Method)
            .HasForeignKey(x => x.MethodId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.SourceCatalogFields)
            .WithOne(x => x.Method)
            .HasForeignKey(x => x.MethodId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.MappingSets)
            .WithOne(x => x.Method)
            .HasForeignKey(x => x.MethodId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.ResponseCodes)
            .WithOne(x => x.Method)
            .HasForeignKey(x => x.MethodId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
