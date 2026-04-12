using Cfa.ACHInterbank.Domain.Entities.Integrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public class IntegrationSourceCatalogFieldConfiguration : IEntityTypeConfiguration<IntegrationSourceCatalogField>
{
    public void Configure(EntityTypeBuilder<IntegrationSourceCatalogField> builder)
    {
        builder.ToTable("IntegrationSourceCatalogFields");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.SourceKind).HasConversion<int>().IsRequired();
        builder.Property(x => x.EntityName).HasMaxLength(120).IsRequired();
        builder.Property(x => x.FieldPath).HasMaxLength(250).IsRequired();
        builder.Property(x => x.DisplayName).HasMaxLength(250).IsRequired();
        builder.Property(x => x.DataType).HasMaxLength(60).IsRequired();
        builder.Property(x => x.Cardinality).HasConversion<int>().IsRequired();
        builder.Property(x => x.Nullable).IsRequired();
        builder.Property(x => x.SortOrder).IsRequired();
        builder.Property(x => x.IsActive).HasDefaultValue(true);

        builder.HasIndex(x => new { x.MethodId, x.SourceKind, x.FieldPath }).IsUnique();

        builder.HasMany(x => x.MappingRules)
            .WithOne(x => x.SourceCatalogField)
            .HasForeignKey(x => x.SourceCatalogFieldId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
