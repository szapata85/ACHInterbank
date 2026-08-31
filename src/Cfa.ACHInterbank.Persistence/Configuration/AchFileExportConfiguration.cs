using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public class AchFileExportConfiguration : IEntityTypeConfiguration<AchFileExport>
{
    public void Configure(EntityTypeBuilder<AchFileExport> builder)
    {
        builder.ToTable("AchFileExports");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.AchCycleId).HasMaxLength(40).IsRequired();
        builder.Property(x => x.ExportKind).HasMaxLength(20).IsRequired();
        builder.Property(x => x.FileName).HasMaxLength(180).IsRequired();
        builder.Property(x => x.TotalRecords).IsRequired();
        builder.Property(x => x.TotalTransactions).IsRequired();
        builder.Property(x => x.IsEncrypted).IsRequired();
        builder.Property(x => x.GeneratedAtUtc).IsRequired();
        builder.Property(x => x.ContentSha256).HasMaxLength(64);
        builder.Property(x => x.LifecycleStatus).HasConversion<string>().HasMaxLength(30)
            .HasDefaultValue(AchFileExportLifecycleStatus.HistoricalUnknown).IsRequired();
        builder.Property(x => x.TransmissionReference).HasMaxLength(120);
        builder.Property(x => x.AcknowledgementCode).HasMaxLength(30);
        builder.Property(x => x.ChamberResponseState).HasConversion<string>().HasMaxLength(30).HasDefaultValue(CenitChamberResponseState.Pending).IsRequired();

        builder.HasOne(x => x.AchCycle)
            .WithMany()
            .HasForeignKey(x => x.AchCycleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ClearingHouse)
            .WithMany()
            .HasForeignKey(x => x.ClearingHouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.AchCycleId, x.ExportKind, x.GeneratedAtUtc });
        builder.HasIndex(x => new { x.AchCycleId, x.ExportKind, x.IsEncrypted, x.FileName })
            .IsUnique()
            .HasDatabaseName("UX_AchFileExports_Cycle_Kind_Encrypted_FileName");
        builder.HasIndex(x => new { x.AchCycleId, x.ExportKind, x.IsEncrypted, x.ContentSha256 })
            .HasDatabaseName("IX_AchFileExports_ContentIdentity");
    }
}
