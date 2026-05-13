using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public class AchReturnPolicyConfiguration : IEntityTypeConfiguration<AchReturnPolicy>
{
    public void Configure(EntityTypeBuilder<AchReturnPolicy> builder)
    {
        builder.ToTable("AchReturnPolicies");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TransactionType).HasMaxLength(30).IsRequired();
        builder.Property(x => x.AllowedReturnCodesCsv).HasMaxLength(500).IsRequired();
        builder.Property(x => x.RequiredOriginalTransactionState).HasMaxLength(40);
        builder.HasIndex(x => x.TransactionType);
    }
}
