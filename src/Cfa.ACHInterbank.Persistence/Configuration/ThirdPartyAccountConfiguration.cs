using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

internal sealed class ThirdPartyAccountConfiguration : IEntityTypeConfiguration<ThirdPartyAccount>
{
    public void Configure(EntityTypeBuilder<ThirdPartyAccount> builder)
    {
        builder.ToTable("ThirdPartyAccounts");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.AccountType)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.AccountNumberNormalized)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.OriginSource)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.HasOne(x => x.ThirdParty)
            .WithMany(x => x.Accounts)
            .HasForeignKey(x => x.ThirdPartyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.FinancialInstitution)
            .WithMany()
            .HasForeignKey(x => x.FinancialInstitutionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new
            {
                x.ThirdPartyId,
                x.FinancialInstitutionId,
                x.AccountType,
                x.AccountNumberNormalized
            })
            .IsUnique()
            .HasDatabaseName("UX_ThirdPartyAccounts_ThirdParty_Institution_Type_Number");

        builder.HasIndex(x => x.FinancialInstitutionId)
            .HasDatabaseName("IX_ThirdPartyAccounts_FinancialInstitutionId");
    }
}
