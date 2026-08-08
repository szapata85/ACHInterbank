using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

internal sealed class AchReturnTraceSequenceConfiguration : IEntityTypeConfiguration<AchReturnTraceSequence>
{
    public void Configure(EntityTypeBuilder<AchReturnTraceSequence> builder)
    {
        builder.ToTable("AchReturnTraceSequences");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ParticipantDfi).HasMaxLength(8).IsRequired();
        builder.Property(x => x.SequenceDate).IsRequired();
        builder.Property(x => x.LastAssignedValue).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).IsRequired();
        builder.HasIndex(x => new { x.ParticipantDfi, x.SequenceDate })
            .IsUnique()
            .HasDatabaseName("UX_AchReturnTraceSequence_Participant_Date");

        builder.ToTable(table => table.HasCheckConstraint(
            "CK_AchReturnTraceSequence_LastAssignedValue",
            "\"LastAssignedValue\" >= 0 AND \"LastAssignedValue\" <= 6999999"));
    }
}
