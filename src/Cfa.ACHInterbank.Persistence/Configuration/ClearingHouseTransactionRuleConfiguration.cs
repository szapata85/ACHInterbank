using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public class ClearingHouseTransactionRuleConfiguration : IEntityTypeConfiguration<ClearingHouseTransactionRule>
{
    public void Configure(EntityTypeBuilder<ClearingHouseTransactionRule> builder)
    {
        builder.ToTable("ClearingHouseTransactionRules");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.TransactionNature)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
        builder.Property(x => x.TransactionType)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();
        builder.Property(x => x.PrenotificationMode)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();
        builder.Property(x => x.PrenotificationLeadBusinessDays);
        builder.Property(x => x.ReceiverIdentificationValidationMode)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();
        builder.Property(x => x.NormativeSource).HasMaxLength(200).IsRequired();
        builder.Property(x => x.NormativeReference).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(2000);
        builder.Property(x => x.EffectiveFrom).IsRequired();
        builder.Property(x => x.IsActive).HasDefaultValue(true);
        builder.Property(x => x.AppliesToNachaExport).HasDefaultValue(true);
        builder.Property(x => x.AppliesToMonetaryTransactions).HasDefaultValue(true);

        builder.HasOne(x => x.ClearingHouse)
            .WithMany()
            .HasForeignKey(x => x.ClearingHouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new
            {
                x.ClearingHouseId,
                x.TransactionNature,
                x.TransactionType,
                x.AppliesToNachaExport,
                x.AppliesToMonetaryTransactions,
                x.EffectiveFrom
            })
            .HasDatabaseName("IX_CHTR_RuleLookup");
    }
}
