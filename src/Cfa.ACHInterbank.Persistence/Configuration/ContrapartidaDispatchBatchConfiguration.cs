using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public class ContrapartidaDispatchBatchConfiguration : IEntityTypeConfiguration<ContrapartidaDispatchBatch>
{
    public void Configure(EntityTypeBuilder<ContrapartidaDispatchBatch> builder)
    {
        builder.ToTable("ContrapartidaDispatchBatches");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.AchCycleId)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.TriggerType)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.RequestedBy)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(x => x.JobId)
            .HasMaxLength(150);

        builder.Property(x => x.MappingSnapshotHash)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(x => x.RequestPayloadXml);
        builder.Property(x => x.ResponsePayloadXml);

        builder.Property(x => x.SummaryMessage)
            .HasMaxLength(2000);

        builder.HasIndex(x => new { x.ClearingHouseId, x.AchCycleId, x.TriggeredAtUtc });
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => new { x.MappingSetId, x.MappingVersion });

        builder.HasOne(x => x.AchCycle)
            .WithMany()
            .HasForeignKey(x => x.AchCycleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ClearingHouse)
            .WithMany()
            .HasForeignKey(x => x.ClearingHouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.AchBatch)
            .WithMany()
            .HasForeignKey(x => x.AchBatchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Attempts)
            .WithOne(x => x.DispatchBatch)
            .HasForeignKey(x => x.DispatchBatchId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
