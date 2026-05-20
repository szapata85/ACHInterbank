using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public class NachaInboundSimulationConfiguration : IEntityTypeConfiguration<NachaInboundSimulation>
{
    public void Configure(EntityTypeBuilder<NachaInboundSimulation> builder)
    {
        builder.ToTable("NachaInboundSimulations");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.SimulationId).IsRequired();
        builder.Property(x => x.ClearingHouseName).HasMaxLength(120).IsRequired();
        builder.Property(x => x.ScenarioType).HasConversion<string>().HasMaxLength(80).IsRequired();
        builder.Property(x => x.ResponseMode).HasConversion<string>().HasMaxLength(40);
        builder.Property(x => x.ReasonCode).HasMaxLength(20);
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.Property(x => x.CycleCode).HasMaxLength(80).IsRequired();
        builder.Property(x => x.FileName).HasMaxLength(260).IsRequired();
        builder.Property(x => x.FilePath).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.Sha256).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(x => x.GeneratedOnly).HasDefaultValue(true);
        builder.Property(x => x.AutoImported).HasDefaultValue(false);
        builder.Property(x => x.UploadRequired).HasDefaultValue(true);
        builder.Property(x => x.ExternalTransmission).HasDefaultValue(false);
        builder.Property(x => x.CreatedBy).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(2000);
        builder.Property(x => x.MetadataJson).HasColumnType("text");

        builder.HasOne(x => x.ClearingHouse)
            .WithMany()
            .HasForeignKey(x => x.ClearingHouseId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.OriginFinancialInstitution)
            .WithMany()
            .HasForeignKey(x => x.OriginFinancialInstitutionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.DestinationFinancialInstitution)
            .WithMany()
            .HasForeignKey(x => x.DestinationFinancialInstitutionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.SimulationId).IsUnique();
        builder.HasIndex(x => x.FileName);
        builder.HasIndex(x => x.CreatedAt);
        builder.HasIndex(x => new { x.ClearingHouseId, x.ScenarioType });
    }
}
