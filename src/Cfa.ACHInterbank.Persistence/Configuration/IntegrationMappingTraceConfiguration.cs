using Cfa.ACHInterbank.Domain.Entities.Integrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public sealed class IntegrationMappingTraceConfiguration : IEntityTypeConfiguration<IntegrationMappingTrace>
{
    public void Configure(EntityTypeBuilder<IntegrationMappingTrace> builder)
    {
        builder.ToTable("IntegrationMappingTraces");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.IntegrationKey).HasMaxLength(80).IsRequired();
        builder.Property(x => x.OperationKey).HasMaxLength(120).IsRequired();
        builder.Property(x => x.MappingPurpose).HasMaxLength(120).IsRequired();
        builder.Property(x => x.MappingDirection).HasMaxLength(80).IsRequired();
        builder.Property(x => x.Reference).HasMaxLength(160);
        builder.Property(x => x.CorrelationId).HasMaxLength(160);

        builder.HasIndex(x => new { x.IntegrationKey, x.OperationKey, x.CreatedAtUtc });
        builder.HasIndex(x => x.TransactionId);
        builder.HasIndex(x => x.CorrelationId);

        builder.HasMany(x => x.Entries)
            .WithOne(x => x.Trace)
            .HasForeignKey(x => x.TraceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class IntegrationMappingTraceEntryConfiguration : IEntityTypeConfiguration<IntegrationMappingTraceEntry>
{
    public void Configure(EntityTypeBuilder<IntegrationMappingTraceEntry> builder)
    {
        builder.ToTable("IntegrationMappingTraceEntries");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.SourceField).HasMaxLength(300);
        builder.Property(x => x.TargetField).HasMaxLength(300).IsRequired();
        builder.Property(x => x.SourceValueSanitized).HasMaxLength(1000);
        builder.Property(x => x.MappedValueSanitized).HasMaxLength(1000);
        builder.Property(x => x.TransformationApplied).HasMaxLength(120);
        builder.Property(x => x.ErrorCode).HasMaxLength(120);

        builder.HasIndex(x => new { x.TraceId, x.TargetField });
    }
}
