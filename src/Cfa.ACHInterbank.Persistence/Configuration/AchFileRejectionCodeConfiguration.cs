using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public class AchFileRejectionCodeConfiguration : IEntityTypeConfiguration<AchFileRejectionCode>
{
    public void Configure(EntityTypeBuilder<AchFileRejectionCode> builder)
    {
        builder.ToTable("AchFileRejectionCodes");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(10).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Severity).HasMaxLength(20).IsRequired();
        builder.Property(x => x.AppliesToStage).HasMaxLength(30).IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();
    }
}
