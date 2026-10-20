using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public sealed class AchOperationalReconciliationSnapshotConfiguration : IEntityTypeConfiguration<AchOperationalReconciliationSnapshot>
{
    public void Configure(EntityTypeBuilder<AchOperationalReconciliationSnapshot> builder)
    {
        builder.ToTable("AchOperationalReconciliationSnapshots");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.AchCycleId).HasMaxLength(40).IsRequired();
        builder.Property(x => x.SourceFingerprint).HasMaxLength(64).IsRequired();
        builder.Property(x => x.ExternalEvidenceReference).HasMaxLength(200);
        builder.Property(x => x.CalculatedBy).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Version).IsRequired().IsConcurrencyToken().ValueGeneratedNever();
        builder.Property(x => x.SentAmount).HasPrecision(18, 2);
        builder.Property(x => x.ReceivedAmount).HasPrecision(18, 2);
        builder.Property(x => x.AppliedAmount).HasPrecision(18, 2);
        builder.Property(x => x.ParticipantReturnAmount).HasPrecision(18, 2);
        builder.Property(x => x.OperatorReturnAmount).HasPrecision(18, 2);
        builder.Property(x => x.InternalExpectedNetPosition).HasPrecision(18, 2);
        builder.Property(x => x.ExternalSentAmount).HasPrecision(18, 2);
        builder.Property(x => x.ExternalReceivedAmount).HasPrecision(18, 2);
        builder.Property(x => x.ExternalNetPosition).HasPrecision(18, 2);
        builder.HasOne(x => x.ClearingHouse).WithMany().HasForeignKey(x => x.ClearingHouseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.AchCycle).WithMany().HasForeignKey(x => x.AchCycleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.ClearingHouseId, x.OperationalDate, x.AchCycleId, x.Revision })
            .IsUnique().HasDatabaseName("UX_AchOperationalReconciliation_Identity_Revision");
        builder.HasIndex(x => new { x.ClearingHouseId, x.OperationalDate, x.AchCycleId, x.SourceFingerprint })
            .IsUnique().HasDatabaseName("UX_AchOperationalReconciliation_Identity_Fingerprint");
    }
}

public sealed class AchOperationalReconciliationDifferenceConfiguration : IEntityTypeConfiguration<AchOperationalReconciliationDifference>
{
    public void Configure(EntityTypeBuilder<AchOperationalReconciliationDifference> builder)
    {
        builder.ToTable("AchOperationalReconciliationDifferences");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.InternalValue).HasPrecision(18, 2);
        builder.Property(x => x.ExternalValue).HasPrecision(18, 2);
        builder.Property(x => x.Delta).HasPrecision(18, 2);
        builder.Property(x => x.EvidenceSource).HasMaxLength(200).IsRequired();
        builder.HasOne(x => x.Snapshot).WithMany(x => x.Differences).HasForeignKey(x => x.SnapshotId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => new { x.SnapshotId, x.Category }).IsUnique();
    }
}
