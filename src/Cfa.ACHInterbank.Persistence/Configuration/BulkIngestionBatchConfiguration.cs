using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public class BulkIngestionBatchConfiguration : IEntityTypeConfiguration<BulkIngestionBatch>
{
    public void Configure(EntityTypeBuilder<BulkIngestionBatch> builder)
    {
        builder.ToTable("BulkIngestionBatches");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.BatchReference)
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(x => x.FileName)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.ContentType)
            .HasMaxLength(120);

        builder.Property(x => x.FileHash)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(x => x.UploadedBy)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(x => x.ClientRequestId)
            .HasMaxLength(100);
        
        builder.Property(x => x.LastJobId)
            .HasMaxLength(150);

        builder.Property(x => x.LastJobMessage)
            .HasMaxLength(2000);

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.FileType)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.SummaryErrorsJson);

        builder.HasIndex(x => x.BatchReference);
        builder.HasIndex(x => x.UploadedAtUtc);
        builder.HasIndex(x => new { x.ClientRequestId, x.FileHash });

        builder.HasMany(x => x.Items)
            .WithOne(x => x.Batch)
            .HasForeignKey(x => x.BatchId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
