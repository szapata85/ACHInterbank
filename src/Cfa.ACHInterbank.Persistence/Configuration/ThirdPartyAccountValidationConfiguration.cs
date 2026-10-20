using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

internal sealed class ThirdPartyAccountValidationConfiguration : IEntityTypeConfiguration<ThirdPartyAccountValidation>
{
    public void Configure(EntityTypeBuilder<ThirdPartyAccountValidation> builder)
    {
        builder.ToTable("ThirdPartyAccountValidations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TransactionNature).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.ServiceType).HasMaxLength(20).HasDefaultValue(string.Empty).IsRequired();
        builder.Property(x => x.ValidationType).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.EffectiveDate).IsRequired();

        builder.HasOne(x => x.ThirdPartyAccount)
            .WithMany(x => x.Validations)
            .HasForeignKey(x => x.ThirdPartyAccountId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ClearingHouse)
            .WithMany()
            .HasForeignKey(x => x.ClearingHouseId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.PolicyRule)
            .WithMany()
            .HasForeignKey(x => x.PolicyRuleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.ThirdPartyAccountId, x.ClearingHouseId, x.TransactionNature, x.ServiceType, x.ValidationType })
            .IsUnique()
            .HasDatabaseName("UX_TPAV_Account_Chamber_Nature_Service_Type");
        builder.HasIndex(x => x.PolicyRuleId).HasDatabaseName("IX_TPAV_PolicyRuleId");
    }
}
