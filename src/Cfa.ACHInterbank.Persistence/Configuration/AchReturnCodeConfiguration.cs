using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public class AchReturnCodeConfiguration : IEntityTypeConfiguration<AchReturnCode>
{
    public void Configure(EntityTypeBuilder<AchReturnCode> builder)
    {
        builder.ToTable("AchReturnCodes");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ClearingHouseId).IsRequired();
        builder.Property(x => x.Code).HasMaxLength(10).IsRequired();
        builder.Property(x => x.FlowType).HasMaxLength(30).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(200).IsRequired();
        builder.Property(x => x.BusinessOutcome).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.RegulatorySource).HasMaxLength(20).IsRequired();
        builder.Property(x => x.EffectiveFrom).IsRequired();
        builder.Property(x => x.EffectiveTo);
        builder.HasOne(x => x.ClearingHouse)
            .WithMany()
            .HasForeignKey(x => x.ClearingHouseId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.ClearingHouseId, x.Code, x.FlowType, x.EffectiveFrom }).IsUnique();
    }
}
