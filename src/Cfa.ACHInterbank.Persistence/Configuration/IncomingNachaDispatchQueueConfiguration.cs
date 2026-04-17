using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public class IncomingNachaDispatchQueueConfiguration : IEntityTypeConfiguration<IncomingNachaDispatchQueue>
{
    public void Configure(EntityTypeBuilder<IncomingNachaDispatchQueue> builder)
    {
        builder.ToTable("IncomingNachaDispatchQueue");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.IdempotencyDispatchKey).HasMaxLength(200).IsRequired();
        builder.Property(x => x.LastErrorCode).HasMaxLength(80);
        builder.Property(x => x.LastResponseCode).HasMaxLength(80);
        builder.Property(x => x.LastErrorMessage).HasMaxLength(4000);

        builder.HasIndex(x => x.IdempotencyDispatchKey).IsUnique();
        builder.HasIndex(x => new { x.QueueStatus, x.NextAttemptAtUtc, x.Priority });
        builder.HasIndex(x => x.AchTransactionId);

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
    }
}
