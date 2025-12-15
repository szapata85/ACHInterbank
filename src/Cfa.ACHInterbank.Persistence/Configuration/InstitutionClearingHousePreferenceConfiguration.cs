using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public class InstitutionClearingHousePreferenceConfiguration : IEntityTypeConfiguration<InstitutionClearingHousePreference>
{
    public void Configure(EntityTypeBuilder<InstitutionClearingHousePreference> builder)
    {
        builder.ToTable("InstitutionClearingHousePreferences");
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new { x.FinancialInstitutionId, x.ClearingHouseId })
         .IsUnique();

        builder.Property(x => x.IsDefault).HasDefaultValue(false);
        builder.Property(x => x.Priority).HasDefaultValue(1);
        builder.Property(x => x.IsActive).HasDefaultValue(true);

        builder.HasOne(x => x.FinancialInstitution)
         .WithMany(fi => fi.ClearingHousePreferences)
         .HasForeignKey(x => x.FinancialInstitutionId)
         .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.ClearingHouse)
         .WithMany()
         .HasForeignKey(x => x.ClearingHouseId)
         .OnDelete(DeleteBehavior.Cascade);
    }
}
