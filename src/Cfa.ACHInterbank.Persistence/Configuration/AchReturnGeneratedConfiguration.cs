using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public class AchReturnGeneratedConfiguration : IEntityTypeConfiguration<AchReturnGenerated>
{
    public void Configure(EntityTypeBuilder<AchReturnGenerated> builder)
    {
        builder.ToTable("AchReturnsGenerated");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ReturnCycleId).HasMaxLength(40).IsRequired();
        builder.Property(x => x.ReturnReasonCode).HasMaxLength(5).IsRequired();
        builder.Property(x => x.NewSequenceNumber).HasMaxLength(15).IsRequired();
        builder.Property(x => x.OriginalSequenceNumber).HasMaxLength(15).IsRequired();
        builder.Property(x => x.ReceiverEntityCode).HasMaxLength(8).IsRequired();
        builder.Property(x => x.OriginatorEntityCode).HasMaxLength(8).IsRequired();
        builder.Property(x => x.FileName).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Amount).HasPrecision(18, 2);

        builder.HasOne(x => x.OriginalTransaction)
            .WithMany()
            .HasForeignKey(x => x.OriginalTransactionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ReturnCycle)
            .WithMany()
            .HasForeignKey(x => x.ReturnCycleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.OriginalTransactionId)
            .IsUnique()
            .HasDatabaseName("UX_AchReturnGenerated_OriginalTransaction");

        builder.HasIndex(x => new { x.SequenceDate, x.NewSequenceNumber })
            .IsUnique()
            .HasDatabaseName("UX_AchReturnGenerated_SequenceDate_Trace");
    }
}
