using Cfa.ACHInterbank.Domain.Models.ACH.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public sealed class ClaimCaseConfiguration : IEntityTypeConfiguration<ClaimCase>
{
    public void Configure(EntityTypeBuilder<ClaimCase> builder)
    {
        builder.ToTable("ClaimCases");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.CauseCode)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.FiledAt)
            .IsRequired();

        builder.Property(x => x.DeadlineAt);
        builder.Property(x => x.ClosedAt);

        builder.Property(x => x.ResolutionNotes)
            .HasMaxLength(2000);

        builder.Property(x => x.ExternalReference)
            .HasMaxLength(128);

        builder.Property(x => x.CreationIdempotencyKey)
            .HasMaxLength(128);

        builder.HasIndex(x => x.ClearingHouseId);
        builder.HasIndex(x => x.RequestingParticipantId);
        builder.HasIndex(x => x.RespondingParticipantId);
        builder.HasIndex(x => x.Status)
            .HasDatabaseName("IX_ClaimCases_Status");
        builder.HasIndex(x => x.DeadlineAt)
            .HasDatabaseName("IX_ClaimCases_DeadlineAt");
        builder.HasIndex(x => new { x.ClearingHouseId, x.CauseCode, x.FiledAt })
            .HasDatabaseName("IX_ClaimCases_ClearingHouse_Cause_FiledAt");

        builder.HasOne(x => x.ClearingHouse)
            .WithMany()
            .HasForeignKey(x => x.ClearingHouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.RequestingParticipant)
            .WithMany()
            .HasForeignKey(x => x.RequestingParticipantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.RespondingParticipant)
            .WithMany()
            .HasForeignKey(x => x.RespondingParticipantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
