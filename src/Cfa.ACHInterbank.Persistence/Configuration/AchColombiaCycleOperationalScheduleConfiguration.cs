using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public sealed class AchColombiaCycleOperationalScheduleConfiguration
    : IEntityTypeConfiguration<AchColombiaCycleOperationalSchedule>
{
    public void Configure(EntityTypeBuilder<AchColombiaCycleOperationalSchedule> builder)
    {
        builder.ToTable("AchColombiaCycleOperationalSchedules", table =>
        {
            table.HasCheckConstraint(
                "CK_AchColombiaCycleOperationalSchedules_ExactTimeRequiresStart",
                "\"OutboundExecutionStrategy\" <> 1 OR \"AutomaticOutboundStartTime\" IS NOT NULL");
            table.HasCheckConstraint(
                "CK_AchColombiaCycleOperationalSchedules_OutboundTimeRange",
                "\"AutomaticOutboundStartTime\" IS NULL OR \"AutomaticOutboundLatestStartTime\" IS NULL OR \"AutomaticOutboundStartTime\" <= \"AutomaticOutboundLatestStartTime\"");
            table.HasCheckConstraint(
                "CK_AchColombiaCycleOperationalSchedules_Strategy",
                "\"OutboundExecutionStrategy\" IN (1, 2)");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.AutomaticOutboundEnabled).IsRequired();
        builder.Property(x => x.OutboundExecutionStrategy).IsRequired();
        builder.Property(x => x.EffectiveTimeZoneId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.RowVersion)
            .IsRequired()
            .IsConcurrencyToken()
            .ValueGeneratedNever();

        builder.HasOne(x => x.ClearingHouse)
            .WithMany()
            .HasForeignKey(x => x.ClearingHouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ClearingHouseCycleConfig)
            .WithMany()
            .HasForeignKey(x => x.ClearingHouseCycleConfigId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.ClearingHouseId, x.ClearingHouseCycleConfigId })
            .IsUnique()
            .HasDatabaseName("UX_AchColombiaCycleOperationalSchedules_House_Cycle");

        builder.HasIndex(x => new { x.ClearingHouseId, x.IsActive })
            .HasDatabaseName("IX_AchColombiaCycleOperationalSchedules_House_Active");
    }
}
