using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public class CenitNettingDetailConfiguration : IEntityTypeConfiguration<CenitNettingDetail>
{
    public void Configure(EntityTypeBuilder<CenitNettingDetail> builder)
    {
        builder.ToTable("CenitNettingDetails");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.Property(x => x.DecisionReason).HasMaxLength(150).IsRequired();
        builder.Property(x => x.ClearingHouseCode).HasMaxLength(16).IsRequired();
        builder.Property(x => x.SourceFileReference).HasMaxLength(120);

        builder.HasOne(x => x.CenitNettingExecution)
            .WithMany(x => x.Details)
            .HasForeignKey(x => x.CenitNettingExecutionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.AchTransaction)
            .WithMany()
            .HasForeignKey(x => x.AchTransactionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
