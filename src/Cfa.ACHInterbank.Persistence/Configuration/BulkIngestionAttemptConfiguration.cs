using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public class BulkIngestionAttemptConfiguration : IEntityTypeConfiguration<BulkIngestionAttempt>
{
    public void Configure(EntityTypeBuilder<BulkIngestionAttempt> builder)
    {
        builder.ToTable("BulkIngestionAttempts");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TriggerType)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.Scope)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(25)
            .IsRequired();

        builder.Property(x => x.TriggeredBy)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(x => x.JobId)
            .HasMaxLength(150);

        builder.Property(x => x.ResultMessage)
            .HasMaxLength(2000);

        builder.HasIndex(x => new { x.BatchId, x.AttemptNumber })
            .IsUnique();

        builder.HasIndex(x => new { x.BatchId, x.Status });

        builder.HasOne(x => x.Batch)
            .WithMany(x => x.Attempts)
            .HasForeignKey(x => x.BatchId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
