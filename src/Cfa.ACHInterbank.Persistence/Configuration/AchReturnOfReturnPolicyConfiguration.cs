using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public class AchReturnOfReturnPolicyConfiguration : IEntityTypeConfiguration<AchReturnOfReturnPolicy>
{
    public void Configure(EntityTypeBuilder<AchReturnOfReturnPolicy> builder)
    {
        builder.ToTable("AchReturnOfReturnPolicies");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ClearingHouseId).IsRequired();
        builder.Property(x => x.OriginalReturnCode).HasMaxLength(10).IsRequired();
        builder.Property(x => x.Direction).HasMaxLength(20).IsRequired();
        builder.Property(x => x.FlowType).HasMaxLength(30).IsRequired();
        builder.Property(x => x.AllowedNewReturnCodesCsv).HasMaxLength(500).IsRequired();
        builder.Property(x => x.RequiredOriginalState).HasMaxLength(40).IsRequired();
        builder.Property(x => x.EffectiveFrom).IsRequired();
        builder.Property(x => x.EffectiveTo);
        builder.HasOne(x => x.ClearingHouse)
            .WithMany()
            .HasForeignKey(x => x.ClearingHouseId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.ClearingHouseId, x.OriginalReturnCode, x.Direction, x.FlowType, x.IsActive });
    }
}
