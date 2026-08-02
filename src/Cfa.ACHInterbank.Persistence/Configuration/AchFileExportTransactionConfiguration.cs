using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public sealed class AchFileExportTransactionConfiguration : IEntityTypeConfiguration<AchFileExportTransaction>
{
    public void Configure(EntityTypeBuilder<AchFileExportTransaction> builder)
    {
        builder.ToTable("AchFileExportTransactions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.AchCycleId).HasMaxLength(40).IsRequired();
        builder.Property(x => x.TraceNumber).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.Property(x => x.IncludedAtUtc).IsRequired();

        builder.HasOne(x => x.AchFileExport)
            .WithMany(x => x.Transactions)
            .HasForeignKey(x => x.AchFileExportId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.AchTransaction)
            .WithMany(x => x.FileExportMemberships)
            .HasForeignKey(x => x.AchTransactionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.AchFileExportId, x.AchTransactionId }).IsUnique();
        builder.HasIndex(x => new { x.AchFileExportId, x.FileSequence }).IsUnique();
        builder.HasIndex(x => new { x.AchTransactionId, x.AchFileExportId });
    }
}
