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

        builder.Property(n => n.AchCycleId)
            .HasMaxLength(40);

        builder.HasOne(n => n.AchCycle)
            .WithMany(c => c.NachaHeaders)
            .HasForeignKey(n => n.AchCycleId)
            .OnDelete(DeleteBehavior.Restrict);


        builder.Property(n => n.IncomingNachaFileIngestionId);

        builder.HasOne(n => n.IncomingNachaFileIngestion)
            .WithMany(i => i.ParsedHeaders)
            .HasForeignKey(n => n.IncomingNachaFileIngestionId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(n => n.IncomingNachaFileIngestionId);

        //builder.HasMany(x => x.Batches)
        //       .WithOne(x => x.NachaHeader)
        //       .HasForeignKey(x => x.NachaHeaderId)
        //       .OnDelete(DeleteBehavior.Cascade);
    }
}
