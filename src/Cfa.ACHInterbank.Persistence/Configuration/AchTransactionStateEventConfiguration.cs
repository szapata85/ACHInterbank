using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public class AchTransactionStateEventConfiguration : IEntityTypeConfiguration<AchTransactionStateEvent>
{
    public void Configure(EntityTypeBuilder<AchTransactionStateEvent> builder)
    {
        builder.ToTable("AchTransactionStateEvents");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.FromState)
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(x => x.ToState)
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(x => x.Source)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.ReasonCode)
            .HasMaxLength(20);

        builder.Property(x => x.PayloadJson);

        builder.Property(x => x.IdempotencyKey).HasMaxLength(128);
        builder.Property(x => x.ResolvedReasonDescription).HasMaxLength(300);
        builder.Property(x => x.OccurredAtUtc).IsRequired();

        builder.HasIndex(x => x.AchTransactionId);
        builder.HasIndex(x => new { x.AchTransactionId, x.OccurredAtUtc });

        builder.HasOne(x => x.AchTransaction)
            .WithMany(x => x.StateEvents)
            .HasForeignKey(x => x.AchTransactionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.ClearingHouse)
            .WithMany()
            .HasForeignKey(x => x.ClearingHouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.AchReturnCode)
            .WithMany()
            .HasForeignKey(x => x.AchReturnCodeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
