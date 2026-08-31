using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public sealed class CenitChamberResponseConfiguration : IEntityTypeConfiguration<CenitChamberResponse>
{
    public void Configure(EntityTypeBuilder<CenitChamberResponse> builder)
    {
        builder.ToTable("CenitChamberResponses");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.SourceResponseId).HasMaxLength(128).IsRequired();
        builder.Property(x => x.SourceFileName).HasMaxLength(180).IsRequired();
        builder.Property(x => x.ResponseType).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.ResultingState).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.ReasonCode).HasMaxLength(60);
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.CorrelationOutcome).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.RawTechnicalReference).HasMaxLength(256).IsRequired();
        builder.Property(x => x.ContentSha256).HasMaxLength(64).IsRequired();
        builder.Property(x => x.IdempotencyKey).HasMaxLength(64).IsRequired();
        builder.Property(x => x.RelatedOutboundFileName).HasMaxLength(180);
        builder.Property(x => x.RelatedReference).HasMaxLength(120);
        builder.Property(x => x.XmlNamespace).HasMaxLength(120);
        builder.Property(x => x.MessageGroupId).HasMaxLength(16);
        builder.Property(x => x.MessageStatus).HasMaxLength(35);
        builder.Property(x => x.OriginatingSender).HasMaxLength(8);
        builder.Property(x => x.TransactionTraceNumber).HasMaxLength(20);
        builder.Property(x => x.ProblemCode).HasMaxLength(60);
        builder.Property(x => x.AchCycleId).HasMaxLength(40);
        builder.HasOne(x => x.ClearingHouse).WithMany().HasForeignKey(x => x.ClearingHouseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.AchFileExport).WithMany(x => x.ChamberResponses).HasForeignKey(x => x.AchFileExportId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.AchCycle).WithMany().HasForeignKey(x => x.AchCycleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.AchTransaction).WithMany().HasForeignKey(x => x.AchTransactionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.ClearingHouseId, x.SourceResponseId, x.ItemSequence }).IsUnique();
        builder.HasIndex(x => x.IdempotencyKey).IsUnique();
        builder.HasIndex(x => new { x.AchFileExportId, x.ReceivedAtUtc });
        builder.HasIndex(x => new { x.CorrelationOutcome, x.ReceivedAtUtc });
        builder.HasIndex(x => new { x.AchCycleId, x.ReceivedAtUtc });
    }
}
