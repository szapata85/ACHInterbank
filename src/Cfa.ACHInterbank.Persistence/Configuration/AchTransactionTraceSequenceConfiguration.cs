using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

internal sealed class AchTransactionTraceSequenceConfiguration : IEntityTypeConfiguration<AchTransactionTraceSequence>
{
    public void Configure(EntityTypeBuilder<AchTransactionTraceSequence> builder)
    {
        builder.ToTable("AchTransactionTraceSequences");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.OriginatingDfi).HasMaxLength(8).IsRequired();
        builder.Property(x => x.SequenceDate).IsRequired();
        builder.Property(x => x.LastAssignedValue).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).IsRequired();
        builder.HasIndex(x => new { x.OriginatingDfi, x.SequenceDate })
            .IsUnique()
            .HasDatabaseName("UX_AchTransactionTraceSequence_Dfi_Date");

        builder.ToTable(table => table.HasCheckConstraint(
            "CK_AchTransactionTraceSequence_LastAssignedValue",
            "\"LastAssignedValue\" >= 0 AND \"LastAssignedValue\" <= 6999999"));
    }
}
