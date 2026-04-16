using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public class CenitCycleQueueConfiguration : IEntityTypeConfiguration<CenitCycleQueue>
{
    public void Configure(EntityTypeBuilder<CenitCycleQueue> builder)
    {
        builder.ToTable("CenitCycleQueues");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.QueueReason).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(30).IsRequired();
        builder.HasIndex(x => new { x.TargetAchCycleId, x.Status });

        builder.HasOne(x => x.AchTransaction)
            .WithMany()
            .HasForeignKey(x => x.AchTransactionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.TargetAchCycle)
            .WithMany()
            .HasForeignKey(x => x.TargetAchCycleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
