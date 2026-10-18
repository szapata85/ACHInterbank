using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public class AchCycleConfiguration : IEntityTypeConfiguration<AchCycle>
{
    public void Configure(EntityTypeBuilder<AchCycle> builder)
    {
        builder.Property(cycle => cycle.Id)
            .HasMaxLength(40)
            .ValueGeneratedNever();

        builder.Property(cycle => cycle.CycleName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(cycle => cycle.CutoffTime)
            .IsRequired();

        builder.Property(cycle => cycle.StartTime)
            .IsRequired();

        builder.Property(cycle => cycle.EndTime)
            .IsRequired();

        builder.Property(cycle => cycle.OutputReleaseTime)
            .IsRequired();

        builder.Property(cycle => cycle.CompensationStatementStartTime)
            .IsRequired(false);

        builder.Property(cycle => cycle.CompensationStatementEndTime)
            .IsRequired(false);

        builder.Property(cycle => cycle.CompensationPaymentStartTime)
            .IsRequired(false);

        builder.Property(cycle => cycle.CompensationPaymentEndTime)
            .IsRequired(false);

        builder.Property(cycle => cycle.OutputReleaseStartTime)
            .IsRequired(false);

        builder.Property(cycle => cycle.OutputReleaseEndTime)
            .IsRequired(false);

        builder.Property(cycle => cycle.ProcessingDate)
            .HasColumnType("date")
            .IsConcurrencyToken();

        builder.HasIndex(cycle => new { cycle.ClearingHouseId, cycle.ProcessingDate, cycle.CycleName })
            .IsUnique();

        builder.HasOne(cycle => cycle.ClearingHouse)
            .WithMany(ch => ch.AchCycles)
            .HasForeignKey(cycle => cycle.ClearingHouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(cycle => cycle.ClearingHouseCycleConfig)
            .WithMany(cfg => cfg.AchCycles)
            .HasForeignKey(cycle => cycle.ClearingHouseCycleConfigId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(cycle => cycle.ClearingHouseCycleConfigId);
    }
}
