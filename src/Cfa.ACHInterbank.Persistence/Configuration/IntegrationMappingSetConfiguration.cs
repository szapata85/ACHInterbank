using Cfa.ACHInterbank.Domain.Entities.Integrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public class IntegrationMappingSetConfiguration : IEntityTypeConfiguration<IntegrationMappingSet>
{
    public void Configure(EntityTypeBuilder<IntegrationMappingSet> builder)
    {
        builder.ToTable("IntegrationMappingSets");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).HasMaxLength(220).IsRequired();
        builder.Property(x => x.Version).IsRequired();
        builder.Property(x => x.Revision).HasDefaultValue(1L).IsRequired().IsConcurrencyToken();
        builder.Property(x => x.Status).HasConversion<int>().IsRequired();
        builder.Property(x => x.IsActive).HasDefaultValue(true);
        builder.Property(x => x.Notes).HasMaxLength(3000).IsRequired();
        builder.Property(x => x.PublishedBy).HasMaxLength(120).IsRequired();
        builder.Property(x => x.ValidationSummaryJson).IsRequired();

        builder.HasIndex(x => new { x.MethodId, x.Status, x.Version });
        builder.HasIndex(x => new { x.MethodId, x.ContractVersionId, x.Version });

        builder.HasOne(x => x.ContractVersion)
            .WithMany(x => x.MappingSets)
            .HasForeignKey(x => x.ContractVersionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Rules)
            .WithOne(x => x.MappingSet)
            .HasForeignKey(x => x.MappingSetId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.History)
            .WithOne(x => x.MappingSet)
            .HasForeignKey(x => x.MappingSetId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
