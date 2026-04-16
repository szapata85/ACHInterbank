using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public class CenitNettingExecutionConfiguration : IEntityTypeConfiguration<CenitNettingExecution>
{
    public void Configure(EntityTypeBuilder<CenitNettingExecution> builder)
    {
        builder.ToTable("CenitNettingExecutions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TotalDebit).HasPrecision(18, 2);
        builder.Property(x => x.TotalCredit).HasPrecision(18, 2);
        builder.HasIndex(x => x.CenitCycleExecutionId).IsUnique();

        builder.HasOne(x => x.CenitCycleExecution)
            .WithOne(x => x.NettingExecution)
            .HasForeignKey<CenitNettingExecution>(x => x.CenitCycleExecutionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
