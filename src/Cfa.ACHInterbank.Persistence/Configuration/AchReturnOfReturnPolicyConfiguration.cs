using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public class AchReturnOfReturnPolicyConfiguration : IEntityTypeConfiguration<AchReturnOfReturnPolicy>
{
    public void Configure(EntityTypeBuilder<AchReturnOfReturnPolicy> builder)
    {
        builder.ToTable("AchReturnOfReturnPolicies");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.OriginalReturnCode).HasMaxLength(10).IsRequired();
        builder.Property(x => x.AllowedNewReturnCodesCsv).HasMaxLength(500).IsRequired();
        builder.Property(x => x.RequiredOriginalState).HasMaxLength(40).IsRequired();
        builder.HasIndex(x => x.OriginalReturnCode);
    }
}
