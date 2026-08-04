using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public sealed class BankHolidayConfiguration : IEntityTypeConfiguration<BankHolidayModel>
{
    public void Configure(EntityTypeBuilder<BankHolidayModel> builder)
    {
        builder.ToTable("BankHolidays");
        builder.Property(x => x.Description).IsRequired();
        builder.Property(x => x.CountryCode).IsRequired();
        builder.Property(x => x.RuleCode).HasMaxLength(80);
        builder.Property(x => x.LegalOrigin).HasMaxLength(120);
        builder.HasIndex(x => new { x.RuleCode, x.CommemorativeDate })
            .IsUnique()
            .HasDatabaseName("UX_BankHolidays_LegalRule");
        builder.HasIndex(x => x.Date)
            .HasDatabaseName("IX_BankHolidays_EffectiveDate");
    }
}
