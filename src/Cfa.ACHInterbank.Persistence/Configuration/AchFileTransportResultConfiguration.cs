using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public sealed class AchFileTransportResultConfiguration : IEntityTypeConfiguration<AchFileTransportResult>
{
    public void Configure(EntityTypeBuilder<AchFileTransportResult> builder)
    {
        builder.ToTable("AchFileTransportResults");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ExternalEventId).HasMaxLength(128).IsRequired();
        builder.Property(x => x.FunctionalIdentityHash).HasMaxLength(64).IsRequired();
        builder.Property(x => x.FileName).HasMaxLength(180).IsRequired();
        builder.Property(x => x.TransmissionReference).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Outcome).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.ResultCode).HasMaxLength(60).IsRequired();
        builder.Property(x => x.ResultSummary).HasMaxLength(500).IsRequired();
        builder.Property(x => x.CorrelationStatus).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.HasOne(x => x.AchFileExport)
            .WithMany(x => x.TransportResults)
            .HasForeignKey(x => x.AchFileExportId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.ExternalEventId).IsUnique();
        builder.HasIndex(x => x.FunctionalIdentityHash).IsUnique();
        builder.HasIndex(x => new { x.TransmissionReference, x.FileName });
    }
}
