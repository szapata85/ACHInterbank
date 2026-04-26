using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public class PaymentRailCapabilityRegistryEntryConfiguration : IEntityTypeConfiguration<PaymentRailCapabilityRegistryEntry>
{
    public void Configure(EntityTypeBuilder<PaymentRailCapabilityRegistryEntry> builder)
    {
        builder.ToTable("PaymentRailCapabilityRegistry");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.RailCode).HasMaxLength(32).IsRequired();
        builder.Property(x => x.CapabilityCode).HasMaxLength(64).IsRequired();
        builder.Property(x => x.State).HasMaxLength(32).IsRequired();
        builder.Property(x => x.ChangeSource).HasMaxLength(32).IsRequired();
        builder.Property(x => x.ChangedBy).HasMaxLength(128).IsRequired();
        builder.Property(x => x.ChangeTicket).HasMaxLength(64);
        builder.Property(x => x.Notes).HasMaxLength(500);

        builder.HasIndex(x => new { x.RailCode, x.CapabilityCode, x.IsActive, x.EffectiveFromUtc });
    }
}
