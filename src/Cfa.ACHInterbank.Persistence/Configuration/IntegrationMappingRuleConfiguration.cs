using Cfa.ACHInterbank.Domain.Entities.Integrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public class IntegrationMappingRuleConfiguration : IEntityTypeConfiguration<IntegrationMappingRule>
{
    public void Configure(EntityTypeBuilder<IntegrationMappingRule> builder)
    {
        builder.ToTable("IntegrationMappingRules");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.SourceKind).HasConversion<int>().IsRequired();
        builder.Property(x => x.SourceFieldPath).HasMaxLength(300).IsRequired();
        builder.Property(x => x.FixedValue).HasMaxLength(1000);
        builder.Property(x => x.DefaultValue).HasMaxLength(1000);
        builder.Property(x => x.TransformationCode).HasMaxLength(80);
        builder.Property(x => x.FormatMask).HasMaxLength(120);
        builder.Property(x => x.Priority).IsRequired();
        builder.Property(x => x.Enabled).HasDefaultValue(true);
        builder.Property(x => x.ConditionExpression).HasMaxLength(500);

        builder.HasIndex(x => new { x.MappingSetId, x.ParameterId, x.Priority });
    }
}
