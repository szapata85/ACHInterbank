using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

internal sealed class PrenotificationValidationAttemptConfiguration : IEntityTypeConfiguration<PrenotificationValidationAttempt>
{
    public void Configure(EntityTypeBuilder<PrenotificationValidationAttempt> builder)
    {
        builder.ToTable("PrenotificationValidationAttempts");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.IdempotencyKey).HasMaxLength(160).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.Resolution).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.ReturnCode).HasMaxLength(20);
        builder.Property(x => x.ReturnDetail).HasMaxLength(500);
        builder.Property(x => x.EvidenceReference).HasMaxLength(300);
        builder.Property(x => x.CorrelationReference).HasMaxLength(200);
        builder.Property(x => x.SourceCycleId).HasMaxLength(100);
        builder.Property(x => x.SourceFileReference).HasMaxLength(300);

        builder.HasOne(x => x.ThirdPartyAccountValidation)
            .WithMany(x => x.Attempts)
            .HasForeignKey(x => x.ThirdPartyAccountValidationId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.AchTransaction)
            .WithMany()
            .HasForeignKey(x => x.AchTransactionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.ThirdPartyAccountValidationId, x.AttemptNumber })
            .IsUnique()
            .HasDatabaseName("UX_PVA_Validation_AttemptNumber");
        builder.HasIndex(x => x.IdempotencyKey)
            .IsUnique()
            .HasDatabaseName("UX_PVA_IdempotencyKey");
        builder.HasIndex(x => x.AchTransactionId).HasDatabaseName("IX_PVA_AchTransactionId");
    }
}
