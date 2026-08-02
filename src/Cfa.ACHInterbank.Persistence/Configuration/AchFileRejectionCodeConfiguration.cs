using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public class AchFileRejectionCodeConfiguration : IEntityTypeConfiguration<AchFileRejectionCode>
{
    public void Configure(EntityTypeBuilder<AchFileRejectionCode> builder)
    {
        builder.ToTable("AchFileRejectionCodes");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(10).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Severity).HasMaxLength(20).IsRequired();
        builder.Property(x => x.AppliesToStage).HasMaxLength(30).IsRequired();
        builder.Property(x => x.RegulatorySource).HasMaxLength(120)
            .HasDefaultValue("Catálogo histórico pendiente de contexto").IsRequired();
        builder.Property(x => x.EffectiveFrom).HasDefaultValue(new DateTime(2024, 1, 1)).IsRequired();
        builder.HasOne(x => x.ClearingHouse)
            .WithMany()
            .HasForeignKey(x => x.ClearingHouseId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.ClearingHouseId, x.Code, x.AppliesToStage, x.EffectiveFrom }).IsUnique();
    }
}
