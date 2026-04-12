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
        builder.Property(x => x.DisplayName).HasMaxLength(250).IsRequired();
        builder.Property(x => x.DataType).HasMaxLength(60).IsRequired();
        builder.Property(x => x.Cardinality).HasConversion<int>().IsRequired();
        builder.Property(x => x.Required).IsRequired();
        builder.Property(x => x.SortOrder).IsRequired();
        builder.Property(x => x.IsActive).HasDefaultValue(true);

        builder.HasIndex(x => new { x.MethodId, x.ParameterPath }).IsUnique();

        builder.HasMany(x => x.MappingRules)
            .WithOne(x => x.Parameter)
            .HasForeignKey(x => x.ParameterId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
