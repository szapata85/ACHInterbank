using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public class AchTransactionConfiguration : IEntityTypeConfiguration<AchTransaction>
{
    public void Configure(EntityTypeBuilder<AchTransaction> builder)
    {
        builder.ToTable("AchTransactions", table =>
            table.HasTrigger("TR_AchTransactions_SyncTraceSequence"));

        builder.HasKey(t => t.Id);

        // 🔹 Guardar el enum como string en BD
        builder.Property(t => t.Type)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(t => t.DiscretionaryData)
            .HasMaxLength(2);

        builder.Property(t => t.Amount).HasPrecision(18, 2);

        builder.Property(t => t.Direction)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(AchTransactionDirection.Unknown)
            .IsRequired();
        builder.Property(t => t.Origin)
            .HasConversion<string>()
            .HasMaxLength(30)
            .HasDefaultValue(AchTransactionOrigin.Unknown)
            .IsRequired();
        builder.Property(t => t.MonetaryIntegrationRoute)
            .HasConversion<string>()
            .HasMaxLength(30)
            .HasDefaultValue(AchMonetaryIntegrationRoute.ManualReview)
            .IsRequired();
        builder.Property(t => t.ClassificationStatus)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(AchTransactionClassificationStatus.Unknown)
            .IsRequired();
        builder.Property(t => t.ClassificationVersion)
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(t => t.TransactionExternalId)
            .HasMaxLength(64);
        builder.Property(t => t.TraceNumber).HasMaxLength(20).IsRequired();

        builder.Property(t => t.CompanyEntryDescriptionId)
            .IsRequired();

        builder.Property(t => t.RecipientIdNumber)
            .HasMaxLength(20);

        builder.Property(t => t.State)
            .HasConversion<string>()
            .HasMaxLength(40)
            .HasDefaultValue(AchTransferStateEnum.Pending)
            .IsConcurrencyToken()
            .IsRequired();

        builder.Property(t => t.StateChangedAtUtc)
            .IsRequired();

        builder.Property(t => t.ReturnReasonCode)
            .HasMaxLength(20);

        builder.Property(t => t.ContrapartidasResponseCode)
            .HasMaxLength(10);

        builder.Property(t => t.OriginalTraceRef)
            .HasMaxLength(20);

        builder.HasIndex(t => t.CompanyEntryDescriptionId);
        builder.HasIndex(t => new { t.EffectiveEntryDate, t.TraceNumber })
            .IsUnique()
            .HasDatabaseName("UX_AchTransactions_EffectiveEntryDate_TraceNumber");
        builder.HasIndex(t => t.TransactionExternalId).HasDatabaseName("IX_AchTransactions_TransactionExternalId");
        builder.HasIndex(t => new { t.Direction, t.ClassificationStatus, t.CreatedAt })
            .HasDatabaseName("IX_AchTransactions_Direction_Classification_CreatedAt");
        builder.HasIndex(t => new { t.MonetaryIntegrationRoute, t.State })
            .HasDatabaseName("IX_AchTransactions_MonetaryRoute_State");

        builder.Property(t => t.AchCycleId).IsConcurrencyToken();
        builder.Property(t => t.AchBatchId).IsConcurrencyToken();

        builder.HasOne(t => t.AchBatch)
            .WithMany(b => b.Transactions)
            .HasForeignKey(t => t.AchBatchId)
            .OnDelete(DeleteBehavior.Restrict);

    }
}
