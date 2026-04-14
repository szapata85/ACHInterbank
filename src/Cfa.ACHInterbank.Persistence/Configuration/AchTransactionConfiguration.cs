using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public class AchTransactionConfiguration : IEntityTypeConfiguration<AchTransaction>
{
    public void Configure(EntityTypeBuilder<AchTransaction> builder)
    {
        builder.ToTable("AchTransactions");

        builder.HasKey(t => t.Id);

        // 🔹 Guardar el enum como string en BD
        builder.Property(t => t.Type)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(t => t.DiscretionaryData)
            .HasMaxLength(2);

        builder.Property(t => t.TransactionExternalId)
            .HasMaxLength(64);

        builder.Property(t => t.CompanyEntryDescriptionId)
            .IsRequired();

        builder.Property(t => t.RecipientIdNumber)
            .HasMaxLength(20);

        builder.Property(t => t.State)
            .HasConversion<string>()
            .HasMaxLength(40)
            .HasDefaultValue(AchTransferStateEnum.Pending)
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

        builder.HasOne(t => t.AchBatch)
            .WithMany(b => b.Transactions)
            .HasForeignKey(t => t.AchBatchId)
            .OnDelete(DeleteBehavior.Restrict);

    }
}
