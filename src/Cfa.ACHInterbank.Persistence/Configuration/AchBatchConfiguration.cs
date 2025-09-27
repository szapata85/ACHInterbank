using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

internal class AchBatchConfiguration : IEntityTypeConfiguration<AchBatch>
{
    public void Configure(EntityTypeBuilder<AchBatch> builder)
    {
        builder.ToTable("AchBatches");

        //builder.HasKey(a => a.Id);

        builder.HasOne(b => b.AchCycle)
            .WithMany(c => c.Batches)
            .HasForeignKey(b => b.AchCycleId)
            .OnDelete(DeleteBehavior.Restrict);


    }
}
