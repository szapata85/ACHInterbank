using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public class CenitNetPositionConfiguration : IEntityTypeConfiguration<CenitNetPosition>
{
    public void Configure(EntityTypeBuilder<CenitNetPosition> builder)
    {
        builder.ToTable("CenitNetPositions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.DebitAmount).HasPrecision(18, 2);
        builder.Property(x => x.CreditAmount).HasPrecision(18, 2);
        builder.Property(x => x.NetAmount).HasPrecision(18, 2);
        builder.Property(x => x.AvailableLiquidity).HasPrecision(18, 2);
        builder.HasIndex(x => new { x.CenitNettingExecutionId, x.FinancialInstitutionId }).IsUnique();

        builder.HasOne(x => x.CenitNettingExecution)
            .WithMany(x => x.NetPositions)
            .HasForeignKey(x => x.CenitNettingExecutionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.FinancialInstitution)
            .WithMany()
            .HasForeignKey(x => x.FinancialInstitutionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
