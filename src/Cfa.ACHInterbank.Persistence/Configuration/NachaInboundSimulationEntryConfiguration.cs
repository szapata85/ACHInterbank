using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public class NachaInboundSimulationEntryConfiguration : IEntityTypeConfiguration<NachaInboundSimulationEntry>
{
    public void Configure(EntityTypeBuilder<NachaInboundSimulationEntry> builder)
    {
        builder.ToTable("NachaInboundSimulationEntries");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Reference).HasMaxLength(80).IsRequired();
        builder.Property(x => x.PrenotificationReference).HasMaxLength(80);
        builder.Property(x => x.AccountNumberMasked).HasMaxLength(40).IsRequired();
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.Property(x => x.Nature).HasMaxLength(30).IsRequired();
        builder.Property(x => x.PreviousStatus).HasMaxLength(80);
        builder.Property(x => x.ExpectedStatusAfterUpload).HasMaxLength(120).IsRequired();
        builder.Property(x => x.ReasonCode).HasMaxLength(20);
        builder.Property(x => x.IsSynthetic).HasDefaultValue(true);

        builder.HasOne(x => x.Simulation)
            .WithMany(x => x.Entries)
            .HasForeignKey(x => x.NachaInboundSimulationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
