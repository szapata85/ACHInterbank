using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public class IncomingNachaFileIngestionConfiguration : IEntityTypeConfiguration<IncomingNachaFileIngestion>
{
    public void Configure(EntityTypeBuilder<IncomingNachaFileIngestion> builder)
    {
        builder.ToTable("IncomingNachaFileIngestions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.FileName).HasMaxLength(260).IsRequired();
        builder.Property(x => x.FileHashSha256).HasMaxLength(64).IsRequired();
        builder.Property(x => x.ContentType).HasMaxLength(120).IsRequired();
        builder.Property(x => x.UploadedBy).HasMaxLength(120).IsRequired();
        builder.Property(x => x.ReceivedBy).HasMaxLength(120);
        builder.Property(x => x.ResolvedAchCycleId).HasMaxLength(40);
        builder.Property(x => x.ResolutionMode).HasMaxLength(60);
        builder.Property(x => x.ResolutionConfidence).HasPrecision(5, 2);
        builder.Property(x => x.RawStorageReference).HasMaxLength(400);
        builder.Property(x => x.CorrelationId).HasMaxLength(80).IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(2000).IsRequired();

        builder.Property(x => x.IngestionStatus).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(x => x.CycleResolutionStatus).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(x => x.ParsingStatus).HasConversion<string>().HasMaxLength(40).IsRequired();

        builder.Property(x => x.ResolutionEvidenceJson).IsRequired();
        builder.Property(x => x.WarningsJson).IsRequired();

        builder.HasIndex(x => new { x.FileHashSha256, x.FileSize })
            .IsUnique()
            .HasDatabaseName("UX_IncomingNachaFileIngestions_FileHash_FileSize_Canonical");
        builder.HasIndex(x => new { x.FileHashSha256, x.FileSize, x.IsReprocess, x.ParentIngestionId });
        builder.HasIndex(x => new { x.UploadedAtUtc, x.FileName });
        builder.HasIndex(x => new { x.ResolvedClearingHouseId, x.OperationalDate, x.ResolvedAchCycleId });
        builder.HasIndex(x => x.CorrelationId);

        builder.HasOne(x => x.ParentIngestion)
            .WithMany(x => x.ReprocessChildren)
            .HasForeignKey(x => x.ParentIngestionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.ProcessingResults)
            .WithOne(x => x.Ingestion)
            .HasForeignKey(x => x.IncomingNachaFileIngestionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.TransactionLinks)
            .WithOne(x => x.Ingestion)
            .HasForeignKey(x => x.IncomingNachaFileIngestionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.EntryClassifications)
            .WithOne(x => x.Ingestion)
            .HasForeignKey(x => x.IncomingNachaFileIngestionId)
            .OnDelete(DeleteBehavior.Cascade);

    }
}
