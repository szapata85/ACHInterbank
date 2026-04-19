using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

internal sealed class BatchNumberSequenceConfiguration : IEntityTypeConfiguration<BatchNumberSequence>
{
    public void Configure(EntityTypeBuilder<BatchNumberSequence> builder)
    {
        builder.ToTable("BatchNumberSequences");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ClearingHouseId)
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(x => x.OriginatingDfi)
            .IsRequired()
            .HasMaxLength(16);

        builder.Property(x => x.PolicyCode)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(x => x.LastAssignedValue)
            .IsRequired();

        builder.HasIndex(x => new { x.ClearingHouseId, x.OriginatingDfi, x.ProcessingDate, x.PolicyCode })
            .IsUnique();
    }
}
