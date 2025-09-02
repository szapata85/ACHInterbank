using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public class NachaHeaderConfiguration : IEntityTypeConfiguration<NachaHeader>
{
    public void Configure(EntityTypeBuilder<NachaHeader> builder)
    {
        builder.ToTable("NachaHeaders");

        builder.Property(x => x.ImmediateDestination).HasMaxLength(10);
        builder.Property(x => x.ImmediateOrigin).HasMaxLength(10);

        builder.Property(n => n.CycleNumber)
            .IsRequired();

        builder.HasOne(n => n.AchCycle)
            .WithMany(c => c.NachaHeaders)
            .HasForeignKey(n => n.AchCycleId)
            .OnDelete(DeleteBehavior.Restrict);


        //builder.HasMany(x => x.Batches)
        //       .WithOne(x => x.NachaHeader)
        //       .HasForeignKey(x => x.NachaHeaderId)
        //       .OnDelete(DeleteBehavior.Cascade);
    }
}
