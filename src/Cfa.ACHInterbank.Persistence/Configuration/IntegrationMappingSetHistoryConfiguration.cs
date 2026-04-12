using Cfa.ACHInterbank.Domain.Entities.Integrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public class IntegrationMappingSetHistoryConfiguration : IEntityTypeConfiguration<IntegrationMappingSetHistory>
{
    public void Configure(EntityTypeBuilder<IntegrationMappingSetHistory> builder)
    {
        builder.ToTable("IntegrationMappingSetHistory");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Status).HasConversion<int>().IsRequired();
        builder.Property(x => x.Action).HasMaxLength(80).IsRequired();
        builder.Property(x => x.PerformedBy).HasMaxLength(120).IsRequired();
        builder.Property(x => x.SnapshotJson).IsRequired();
        builder.Property(x => x.SnapshotHash).HasMaxLength(128).IsRequired();

        builder.HasIndex(x => new { x.MappingSetId, x.PerformedAtUtc });
    }
}
