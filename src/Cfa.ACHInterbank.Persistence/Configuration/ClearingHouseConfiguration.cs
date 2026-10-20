using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public class ClearingHouseConfiguration : IEntityTypeConfiguration<ClearingHouse>
{
    public void Configure(EntityTypeBuilder<ClearingHouse> builder)
    {
        builder.ToTable("ClearingHouses", table =>
            table.HasCheckConstraint("CK_ClearingHouses_Code_Normalized", "\"Code\" = UPPER(TRIM(\"Code\"))"));

        builder.Property(house => house.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(house => house.Code)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(house => house.OriginCode)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(house => house.IsActive)
            .HasDefaultValue(true)
            .IsRequired();

        builder.HasIndex(house => house.Code).IsUnique();
        builder.HasIndex(house => new { house.IsActive, house.Code });

        builder.HasOne(house => house.ClearingHouseConfig)
            .WithMany(config => config.ClearingHouses)
            .HasForeignKey(house => house.ClearingHouseId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
