using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public class LiquidityOptimizationDecisionConfiguration : IEntityTypeConfiguration<LiquidityOptimizationDecision>
{
    public void Configure(EntityTypeBuilder<LiquidityOptimizationDecision> builder)
    {
        builder.ToTable("LiquidityOptimizationDecisions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.DecisionType).HasMaxLength(30).IsRequired();
        builder.Property(x => x.DecisionReason).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ClearingHouseCode).HasMaxLength(16).IsRequired();
        builder.Property(x => x.SourceFileReference).HasMaxLength(120);
        builder.Property(x => x.LiquidityModelUsed).HasMaxLength(20).IsRequired();
        builder.Property(x => x.FromCycleId).HasMaxLength(40).IsRequired();
        builder.Property(x => x.ToCycleId).HasMaxLength(40);
        builder.HasIndex(x => new { x.CenitCycleExecutionId, x.DecisionType });
        builder.HasIndex(x => new { x.CenitCycleExecutionId, x.AchTransactionId }).IsUnique();

        builder.HasOne(x => x.CenitCycleExecution)
            .WithMany(x => x.OptimizationDecisions)
            .HasForeignKey(x => x.CenitCycleExecutionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.AchTransaction)
            .WithMany()
            .HasForeignKey(x => x.AchTransactionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
