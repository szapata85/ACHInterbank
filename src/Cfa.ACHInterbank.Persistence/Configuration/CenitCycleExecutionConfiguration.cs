using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public class CenitCycleExecutionConfiguration : IEntityTypeConfiguration<CenitCycleExecution>
{
    public void Configure(EntityTypeBuilder<CenitCycleExecution> builder)
    {
        builder.ToTable("CenitCycleExecutions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Status).HasMaxLength(30).IsRequired();
        builder.Property(x => x.Summary).HasMaxLength(500);
        builder.HasIndex(x => x.AchCycleId).IsUnique();

        builder.HasOne(x => x.AchCycle)
            .WithMany()
            .HasForeignKey(x => x.AchCycleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
