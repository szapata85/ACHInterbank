using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public sealed class AchResponseAuditConfiguration : IEntityTypeConfiguration<AchResponseAudit>
{
    public void Configure(EntityTypeBuilder<AchResponseAudit> builder)
    {
        builder.ToTable("AchResponseAudits");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EntityType).HasMaxLength(80).IsRequired();
        builder.Property(x => x.EntityId).HasMaxLength(80).IsRequired();
        builder.Property(x => x.Action).HasMaxLength(80).IsRequired();
        builder.Property(x => x.PreviousState).HasMaxLength(50);
        builder.Property(x => x.NewState).HasMaxLength(50);
        builder.Property(x => x.Actor).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Reason).HasMaxLength(500).IsRequired();
        builder.Property(x => x.CorrelationId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.SanitizedMetadata).HasMaxLength(2000);
        builder.HasOne(x => x.AchResponse).WithMany(x => x.AuditEntries).HasForeignKey(x => x.AchResponseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.EntityType, x.EntityId, x.OccurredAtUtc });
        builder.HasIndex(x => x.CorrelationId);
    }
}

public sealed class AchResponseOrphanConfiguration : IEntityTypeConfiguration<AchResponseOrphan>
{
    public void Configure(EntityTypeBuilder<AchResponseOrphan> builder)
    {
        builder.ToTable("AchResponseOrphans");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ResponseType).HasMaxLength(30).IsRequired();
        builder.Property(x => x.ExternalIdentifiers).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.ExternalCode).HasMaxLength(50).IsRequired();
        builder.Property(x => x.CanonicalPayloadHash).HasMaxLength(128).IsRequired();
        builder.Property(x => x.CorrelationId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.OrphanReason).HasMaxLength(500).IsRequired();
        builder.Property(x => x.CandidateReferences).HasMaxLength(2000);
        builder.Property(x => x.ResolutionStatus).HasMaxLength(30).IsRequired();
        builder.Property(x => x.ResolvedReference).HasMaxLength(100);
        builder.Property(x => x.ResolvedBy).HasMaxLength(150);
        builder.Property(x => x.ResolutionReason).HasMaxLength(500);
        builder.Property(x => x.Version).IsRequired().IsConcurrencyToken().ValueGeneratedNever();
        builder.HasOne(x => x.AchResponse).WithOne(x => x.Orphan).HasForeignKey<AchResponseOrphan>(x => x.AchResponseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ClearingHouse).WithMany().HasForeignKey(x => x.ClearingHouseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.AchResponseId).IsUnique();
        builder.HasIndex(x => new { x.ClearingHouseId, x.ResolutionStatus, x.ReceivedAtUtc });
    }
}

public sealed class AchResponseReprocessAttemptConfiguration : IEntityTypeConfiguration<AchResponseReprocessAttempt>
{
    public void Configure(EntityTypeBuilder<AchResponseReprocessAttempt> builder)
    {
        builder.ToTable("AchResponseReprocessAttempts");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Status).HasMaxLength(30).IsRequired();
        builder.Property(x => x.RequestedBy).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Reason).HasMaxLength(500).IsRequired();
        builder.Property(x => x.CorrelationId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.ClaimedBy).HasMaxLength(150);
        builder.Property(x => x.ResultCode).HasMaxLength(50);
        builder.Property(x => x.Result).HasMaxLength(1000);
        builder.Property(x => x.ErrorType).HasMaxLength(100);
        builder.Property(x => x.ErrorDetailSanitized).HasMaxLength(1000);
        builder.Property(x => x.Version).IsRequired().IsConcurrencyToken().ValueGeneratedNever();
        builder.HasOne(x => x.AchResponse).WithMany().HasForeignKey(x => x.AchResponseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.AchResponseId, x.AttemptNumber }).IsUnique();
        builder.HasIndex(x => x.CommandId).IsUnique();
        builder.HasIndex(x => new { x.AchResponseId, x.Status });
        builder.HasIndex(x => new { x.Status, x.LeaseExpiresAtUtc, x.RequestedAtUtc, x.Id });
    }
}

public sealed class AchResponseReconciliationCaseConfiguration : IEntityTypeConfiguration<AchResponseReconciliationCase>
{
    public void Configure(EntityTypeBuilder<AchResponseReconciliationCase> builder)
    {
        builder.ToTable("AchResponseReconciliationCases");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ExceptionType).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(30).IsRequired();
        builder.Property(x => x.Reference).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Details).HasMaxLength(1000);
        builder.Property(x => x.Resolution).HasMaxLength(50);
        builder.Property(x => x.ResolutionReason).HasMaxLength(500);
        builder.Property(x => x.ResolvedBy).HasMaxLength(150);
        builder.Property(x => x.CorrelationId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Version).IsRequired().IsConcurrencyToken().ValueGeneratedNever();
        builder.HasOne(x => x.ClearingHouse).WithMany().HasForeignKey(x => x.ClearingHouseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.AchResponse).WithMany().HasForeignKey(x => x.AchResponseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.ClearingHouseId, x.Status, x.ExceptionType });
        builder.HasIndex(x => x.CorrelationId);
    }
}
