using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public sealed class AchFileTransmissionAttemptConfiguration : IEntityTypeConfiguration<AchFileTransmissionAttempt>
{
    public void Configure(EntityTypeBuilder<AchFileTransmissionAttempt> builder)
    {
        builder.ToTable("AchFileTransmissionAttempts");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.IdempotencyKey).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.ResultCode).HasMaxLength(60).IsRequired();
        builder.Property(x => x.ResultSummary).HasMaxLength(500).IsRequired();
        builder.Property(x => x.ExternalReference).HasMaxLength(120);
        builder.Property(x => x.ContentSha256).HasMaxLength(64).IsRequired();
        builder.Property(x => x.ProtectedContent).IsRequired();
        builder.HasOne(x => x.AchFileExport)
            .WithMany(x => x.TransmissionAttempts)
            .HasForeignKey(x => x.AchFileExportId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.AchFileExportId, x.AttemptNumber }).IsUnique();
        builder.HasIndex(x => x.IdempotencyKey).IsUnique();
    }
}
