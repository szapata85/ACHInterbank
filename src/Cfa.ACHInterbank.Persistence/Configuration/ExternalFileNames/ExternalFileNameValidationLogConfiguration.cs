using Cfa.ACHInterbank.Domain.Models.ACH.ExternalFileNames;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration.ExternalFileNames;

public class ExternalFileNameValidationLogConfiguration : IEntityTypeConfiguration<ExternalFileNameValidationLog>
{
    public void Configure(EntityTypeBuilder<ExternalFileNameValidationLog> builder)
    {
        builder.ToTable("ExternalFileNameValidationLog");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ValidationStage).HasMaxLength(30).IsRequired();
        builder.Property(x => x.RuleCode).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Severity).HasMaxLength(20).IsRequired();
        builder.Property(x => x.IssueCode).HasMaxLength(60).IsRequired();
        builder.Property(x => x.IssueMessage).HasMaxLength(500).IsRequired();

        builder.HasIndex(x => new { x.RegistryId, x.CreatedAtUtc });
    }
}
