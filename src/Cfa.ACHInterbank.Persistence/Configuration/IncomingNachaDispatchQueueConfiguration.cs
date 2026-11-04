using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public class IncomingNachaDispatchQueueConfiguration : IEntityTypeConfiguration<IncomingNachaDispatchQueue>
{
    public void Configure(EntityTypeBuilder<IncomingNachaDispatchQueue> builder)
    {
        builder.ToTable("IncomingNachaDispatchQueue", table => table.HasCheckConstraint(
            "CK_IncomingNachaDispatchQueue_SourceShape",
            "(\"SourceKind\" = 'CycleBound' AND \"AchTransactionId\" IS NOT NULL AND \"AchCycleId\" IS NOT NULL AND \"EntryDetailId\" IS NULL AND \"IncomingNachaTransactionLinkId\" IS NOT NULL) OR " +
            "(\"SourceKind\" = 'AchColFileBased' AND \"EntryDetailId\" IS NOT NULL AND \"AchTransactionId\" IS NULL AND \"AchCycleId\" IS NULL)"));
        builder.HasKey(x => x.Id);

        builder.Property(x => x.SourceKind)
            .HasMaxLength(30)
            .IsRequired()
            .HasDefaultValue(IncomingNachaDispatchSourceKinds.CycleBound);
        builder.Property(x => x.IdempotencyDispatchKey).HasMaxLength(200).IsRequired();
        builder.Property(x => x.LastErrorCode).HasMaxLength(80);
        builder.Property(x => x.LastResponseCode).HasMaxLength(80);
        builder.Property(x => x.LastErrorMessage).HasMaxLength(4000);

        builder.HasIndex(x => x.IdempotencyDispatchKey).IsUnique();
        builder.HasIndex(x => new { x.QueueStatus, x.NextAttemptAtUtc, x.Priority });
        builder.HasIndex(x => x.AchTransactionId);
        builder.HasIndex(x => x.EntryDetailId);

        builder.HasOne(x => x.Ingestion)
            .WithMany()
            .HasForeignKey(x => x.IncomingNachaFileIngestionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Classification)
            .WithMany()
            .HasForeignKey(x => x.IncomingNachaEntryClassificationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.TransactionLink)
            .WithMany()
            .HasForeignKey(x => x.IncomingNachaTransactionLinkId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.AchTransaction)
            .WithMany()
            .HasForeignKey(x => x.AchTransactionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.EntryDetail)
            .WithMany()
            .HasForeignKey(x => x.EntryDetailId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
