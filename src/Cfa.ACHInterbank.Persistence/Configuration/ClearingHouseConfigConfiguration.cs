using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public class ClearingHouseConfigConfiguration : IEntityTypeConfiguration<ClearingHouseConfig>
{
    public void Configure(EntityTypeBuilder<ClearingHouseConfig> builder)
    {
        builder.Property(config => config.HolidayStrategy)
            .HasMaxLength(100);

        builder.HasIndex(config => config.ClearingHouseId)
            .IsUnique();
    }
}
