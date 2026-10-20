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
        builder.HasIndex(n => new { n.ClearingHouseId, n.FileCreationDate, n.CycleNumber });

        builder.HasMany(x => x.Batches)
            .WithOne(x => x.NachaHeader)
            .HasForeignKey(x => x.NachaID)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.EntryDetails)
            .WithOne(x => x.NachaHeader)
            .HasForeignKey(x => x.NachaID)
            // EntryDetail also references BatchHeader. Restrict the direct legacy
            // link so SQL Server does not create two cascade paths from a header.
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.AddendaRecords)
            .WithOne(x => x.NachaHeader)
            .HasForeignKey(x => x.NachaID)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.BatchControls)
            .WithOne(x => x.NachaHeader)
            .HasForeignKey(x => x.NachaID)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.FileControls)
            .WithOne(x => x.NachaHeader)
            .HasForeignKey(x => x.NachaID)
            .OnDelete(DeleteBehavior.Cascade);

        //builder.HasMany(x => x.Batches)
        //       .WithOne(x => x.NachaHeader)
        //       .HasForeignKey(x => x.NachaHeaderId)
        //       .OnDelete(DeleteBehavior.Cascade);
    }
}
