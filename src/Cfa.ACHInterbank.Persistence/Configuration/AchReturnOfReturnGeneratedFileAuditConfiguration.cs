using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public class AchReturnOfReturnGeneratedFileAuditConfiguration : IEntityTypeConfiguration<AchReturnOfReturnGeneratedFileAudit>
{
    public void Configure(EntityTypeBuilder<AchReturnOfReturnGeneratedFileAudit> builder)
    {
        builder.ToTable("AchReturnOfReturnGeneratedFileAudits");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.FileName).HasMaxLength(300).IsRequired();
        builder.Property(x => x.ContentSha256).HasMaxLength(64).IsRequired();
        builder.Property(x => x.RequestedBy).HasMaxLength(120);
        builder.Property(x => x.Source).HasMaxLength(120);
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.HasIndex(x => x.FileName);

        builder.HasMany(x => x.Flows)
            .WithOne(x => x.Audit)
            .HasForeignKey(x => x.AchReturnOfReturnGeneratedFileAuditId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class AchReturnOfReturnGeneratedFileAuditFlowConfiguration : IEntityTypeConfiguration<AchReturnOfReturnGeneratedFileAuditFlow>
{
    public void Configure(EntityTypeBuilder<AchReturnOfReturnGeneratedFileAuditFlow> builder)
    {
        builder.ToTable("AchReturnOfReturnGeneratedFileAuditFlows");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.AchReturnOfReturnGeneratedFileAuditId, x.ReturnOfReturnFlowId }).IsUnique();

        builder.HasOne(x => x.ReturnOfReturnFlow)
            .WithMany()
            .HasForeignKey(x => x.ReturnOfReturnFlowId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
