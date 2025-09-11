using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public class FinancialInstitutionConfigurationConfiguration : IEntityTypeConfiguration<FinancialInstitution>
{
    public void Configure(EntityTypeBuilder<FinancialInstitution> builder)
    {
        builder.ToTable("FinancialInstitutions");

        builder.HasKey(f => f.Id);
        builder.Property(f => f.Name).HasMaxLength(200).IsRequired();
        builder.Property(f => f.Code).HasMaxLength(20).IsRequired();

        // Relación con ClearingHouse
        builder.HasOne(f => f.ClearingHouse)
              .WithMany(ch => ch.FinancialInstitutions)
              .HasForeignKey(f => f.ClearingHouseId)
              .OnDelete(DeleteBehavior.Restrict);



    }
}
