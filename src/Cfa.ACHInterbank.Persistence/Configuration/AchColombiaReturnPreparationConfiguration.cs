using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public sealed class AchColombiaReturnPreparationConfiguration : IEntityTypeConfiguration<AchColombiaReturnPreparation>
{
    public void Configure(EntityTypeBuilder<AchColombiaReturnPreparation> builder)
    {
        builder.ToTable("AchColombiaReturnPreparations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.OriginalCycleId).HasMaxLength(40).IsRequired();
        builder.Property(x => x.SuggestedReturnCycleId).HasMaxLength(40);
        builder.Property(x => x.ReasonCode).HasMaxLength(5).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(40).IsRequired().IsConcurrencyToken();
        builder.HasIndex(x => x.OriginalTransactionId)
            .IsUnique().HasDatabaseName("UX_AchColReturnPreparation_Original");
        builder.HasIndex(x => x.SourceEventId)
            .IsUnique().HasDatabaseName("UX_AchColReturnPreparation_Event");
        builder.HasOne(x => x.OriginalTransaction).WithMany()
            .HasForeignKey(x => x.OriginalTransactionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.SourceEvent).WithMany()
            .HasForeignKey(x => x.SourceEventId).OnDelete(DeleteBehavior.Restrict);
    }
}
