using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public class ContrapartidaDispatchItemConfiguration : IEntityTypeConfiguration<ContrapartidaDispatchItem>
{
    public void Configure(EntityTypeBuilder<ContrapartidaDispatchItem> builder)
    {
        builder.ToTable("ContrapartidaDispatchItems");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.AchCycleId)
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(x => x.State)
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(x => x.DispatchLeaseToken).HasMaxLength(120).IsRequired().IsConcurrencyToken();
        builder.Property(x => x.DispatchLeasePhase).HasMaxLength(20).IsRequired();
        builder.Property(x => x.DispatchIntentCorrelationId).HasMaxLength(120).IsRequired();
        builder.Property(x => x.DispatchIntentPayloadHash).HasMaxLength(64).IsRequired();
        builder.Property(x => x.DispatchIntentPayloadXml);
        builder.Property(x => x.DispatchIntentMappingSnapshotHash).HasMaxLength(128).IsRequired();
        builder.Property(x => x.DispatchIntentSoapEndpoint).HasMaxLength(500).IsRequired();
        builder.Property(x => x.DispatchIntentExecutionMode).HasMaxLength(20).IsRequired();
        builder.Property(x => x.RequiresReconciliation).HasDefaultValue(false).IsRequired();
        builder.HasIndex(x => new { x.State, x.DispatchLeaseExpiresAtUtc });

        builder.Property(x => x.LastReassignedFromCycleId).HasMaxLength(40).IsRequired();
        builder.Property(x => x.LastResponseCode)
            .HasMaxLength(20);

        builder.Property(x => x.LastErrorCode)
            .HasMaxLength(50);

        builder.Property(x => x.LastErrorMessage)
            .HasMaxLength(2000);

        builder.Property(x => x.LastCorrelationId)
            .HasMaxLength(120);

        builder.Property(x => x.LastDispatchedBy)
            .HasMaxLength(120);

        builder.HasIndex(x => x.AchTransactionId).IsUnique();
        builder.HasIndex(x => new { x.State, x.NextAttemptAtUtc });
        builder.HasIndex(x => new { x.ClearingHouseId, x.AchCycleId, x.State });

        builder.HasOne(x => x.AchTransaction)
            .WithOne(t => t.ContrapartidaDispatchItem)
            .HasForeignKey<ContrapartidaDispatchItem>(x => x.AchTransactionId)
            .OnDelete(DeleteBehavior.Cascade);

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
            .WithOne(x => x.DispatchItem)
            .HasForeignKey(x => x.DispatchItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
