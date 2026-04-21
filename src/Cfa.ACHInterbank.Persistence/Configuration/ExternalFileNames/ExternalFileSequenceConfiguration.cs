using Cfa.ACHInterbank.Domain.Models.ACH.ExternalFileNames;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration.ExternalFileNames;

public class ExternalFileSequenceConfiguration : IEntityTypeConfiguration<ExternalFileSequence>
{
    public void Configure(EntityTypeBuilder<ExternalFileSequence> builder)
    {
        builder.ToTable("ExternalFileSequences");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ScopeCode).HasMaxLength(40).IsRequired();
        builder.Property(x => x.LastValue).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).IsRequired();
        builder.Property(x => x.RowVersion).IsRequired().IsConcurrencyToken().ValueGeneratedNever();

        builder.HasIndex(x => new { x.ClearingHouseId, x.ScopeCode, x.SequenceDate }).IsUnique();
    }
}
