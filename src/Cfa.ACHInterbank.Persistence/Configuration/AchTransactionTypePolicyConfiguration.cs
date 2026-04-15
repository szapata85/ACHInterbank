using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public class AchTransactionTypePolicyConfiguration : IEntityTypeConfiguration<AchTransactionTypePolicy>
{
    public void Configure(EntityTypeBuilder<AchTransactionTypePolicy> builder)
    {
        builder.ToTable("AchTransactionTypePolicies");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TransactionType).HasMaxLength(30).IsRequired();
        builder.HasIndex(x => x.TransactionType).IsUnique();
    }
}
