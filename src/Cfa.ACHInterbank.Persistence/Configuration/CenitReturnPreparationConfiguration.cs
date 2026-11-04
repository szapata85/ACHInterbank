using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public sealed class CenitReturnPreparationConfiguration : IEntityTypeConfiguration<CenitReturnPreparation>
{
    public void Configure(EntityTypeBuilder<CenitReturnPreparation> builder)
    {
        builder.ToTable("CenitReturnPreparations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.OriginalCycleId).HasMaxLength(40).IsRequired();
        builder.Property(x => x.SuggestedReturnCycleId).HasMaxLength(40).IsRequired();
        builder.Property(x => x.ReasonCode).HasMaxLength(3).IsRequired();
        builder.Property(x => x.OriginalValueDate).HasColumnType("date");
        builder.Property(x => x.SuggestedValueDate).HasColumnType("date");
        builder.Property(x => x.Status).HasMaxLength(40).IsRequired().IsConcurrencyToken();
        builder.Property(x => x.ReviewedByUserId).HasMaxLength(128);
        builder.Property(x => x.DecisionJustification).HasMaxLength(500);
        builder.Property(x => x.DecisionEvidenceReference).HasMaxLength(120);
        builder.Property(x => x.ArtifactSha256).HasMaxLength(64);
        builder.Property(x => x.ArtifactFileName).HasMaxLength(120);
        builder.Property(x => x.ArtifactTraceNumber).HasMaxLength(15);
        builder.Property(x => x.ArtifactProfileCode).HasMaxLength(100);
        builder.Property(x => x.GeneratedByUserId).HasMaxLength(128);
        builder.Property(x => x.GeneratedAtUtc);
        builder.Property(x => x.ArtifactRecordCount);
        // ProtectedArtifactContent sólo almacena texto protegido con ASP.NET Data Protection.
        builder.Property(x => x.ProtectedArtifactContent);
        builder.HasIndex(x => x.OriginalTransactionId).IsUnique()
            .HasDatabaseName("UX_CenitReturnPreparation_Original");
        builder.HasIndex(x => x.SourceExecutionId).IsUnique()
            .HasDatabaseName("UX_CenitReturnPreparation_Execution");
        builder.HasOne(x => x.SourceExecution).WithMany()
            .HasForeignKey(x => x.SourceExecutionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.OriginalTransaction).WithMany()
            .HasForeignKey(x => x.OriginalTransactionId).OnDelete(DeleteBehavior.Restrict);
    }
}
