using Cfa.ACHInterbank.Domain.Entities.Integrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public class IntegrationMethodParameterConfiguration : IEntityTypeConfiguration<IntegrationMethodParameter>
{
    public void Configure(EntityTypeBuilder<IntegrationMethodParameter> builder)
    {
        builder.ToTable("IntegrationMethodParameters");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ParameterPath).HasMaxLength(250).IsRequired();
        builder.Property(x => x.XmlElementName).HasMaxLength(250).IsRequired();
        builder.Property(x => x.DisplayName).HasMaxLength(250).IsRequired();
        builder.Property(x => x.DescriptionEs).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.Category).HasMaxLength(120).IsRequired();
        builder.Property(x => x.ExampleValue).HasMaxLength(250).IsRequired();
        builder.Property(x => x.UiHelpText).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.DataType).HasMaxLength(60).IsRequired();
        builder.Property(x => x.XsdType).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Nillable).IsRequired();
        builder.Property(x => x.MinOccurs).IsRequired();
        builder.Property(x => x.SupportStatus).HasMaxLength(80).IsRequired();
        builder.Property(x => x.Direction).HasConversion<int>().IsRequired();
        builder.Property(x => x.Cardinality).HasConversion<int>().IsRequired();
        builder.Property(x => x.Required).IsRequired();
        builder.Property(x => x.SortOrder).IsRequired();
        builder.Property(x => x.IsActive).HasDefaultValue(true);

        // Final P1.3 uniqueness is configured in AchDbContext because filtered-index SQL differs by provider.
        // Nullable ContractVersionId remains as the legacy compatibility bridge; versioned rows are unique per contract.

        builder.HasOne(x => x.ContractVersion)
            .WithMany(x => x.Parameters)
            .HasForeignKey(x => x.ContractVersionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.MappingRules)
            .WithOne(x => x.Parameter)
            .HasForeignKey(x => x.ParameterId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
