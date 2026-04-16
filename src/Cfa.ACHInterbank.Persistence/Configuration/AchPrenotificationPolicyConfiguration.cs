using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public class AchPrenotificationPolicyConfiguration : IEntityTypeConfiguration<AchPrenotificationPolicy>
{
    public void Configure(EntityTypeBuilder<AchPrenotificationPolicy> builder)
    {
        builder.ToTable("AchPrenotificationPolicies");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TransactionType).HasMaxLength(30).IsRequired();
        builder.HasIndex(x => x.TransactionType).IsUnique();
    }
}
