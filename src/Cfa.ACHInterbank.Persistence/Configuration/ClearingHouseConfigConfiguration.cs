using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public class ClearingHouseConfigConfiguration : IEntityTypeConfiguration<ClearingHouseConfig>
{
    public void Configure(EntityTypeBuilder<ClearingHouseConfig> builder)
    {
        builder.ToTable(table => table.HasCheckConstraint(
            "CK_ClearingHouseConfigs_AutomaticReturnMode",
            "\"AutomaticReturnMode\" IN ('DESACTIVADO', 'CON_APROBACION', 'AUTOMATICO')"));

        builder.Property(config => config.AutomaticReturnMode)
            .HasMaxLength(20)
            .IsRequired()
            .HasDefaultValue("DESACTIVADO")
            .IsConcurrencyToken();

        builder.Property(config => config.HolidayStrategy)
            .HasMaxLength(100);

        builder.Property(config => config.TimeZoneId)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(config => config.PaymentRailCode)
            .HasMaxLength(50);

        builder.HasIndex(config => config.ClearingHouseId)
            .IsUnique();

        builder.HasIndex(config => config.NachaProfileId);

        builder.HasOne(config => config.NachaProfile)
            .WithMany()
            .HasForeignKey(config => config.NachaProfileId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
