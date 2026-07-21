using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public class ClearingHouseSpecialDateConfiguration : IEntityTypeConfiguration<ClearingHouseSpecialDate>
{
    public void Configure(EntityTypeBuilder<ClearingHouseSpecialDate> builder)
    {
        builder.ToTable("ClearingHouseSpecialDates");

        builder.Property(d => d.Description)
            .HasMaxLength(200);

        builder.Property(d => d.IsActive)
            .HasDefaultValue(true)
            .IsRequired();

        builder.HasIndex(d => new { d.ClearingHouseId, d.Date })
            .IsUnique();

        builder.HasOne(d => d.ClearingHouse)
            .WithMany(ch => ch.SpecialDates)
            .HasForeignKey(d => d.ClearingHouseId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
