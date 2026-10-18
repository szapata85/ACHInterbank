using Cfa.ACHInterbank.Domain.Models.ACH.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public sealed class ClaimCaseEventConfiguration : IEntityTypeConfiguration<ClaimCaseEvent>
{
    public void Configure(EntityTypeBuilder<ClaimCaseEvent> builder)
    {
        builder.ToTable("ClaimCaseEvents");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.EventType)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.OccurredAt)
            .IsRequired();

        builder.Property(x => x.Observation)
            .HasMaxLength(2000);

        builder.Property(x => x.Actor)
            .HasMaxLength(200);

        builder.Property(x => x.IdempotencyKey)
            .HasMaxLength(128);

        builder.HasOne(x => x.ClaimCase)
            .WithMany(x => x.Events)
            .HasForeignKey(x => x.ClaimCaseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.ClaimCaseId, x.OccurredAt, x.Id })
            .HasDatabaseName("IX_ClaimCaseEvents_ClaimCase_OccurredAt_Id");
    }
}
