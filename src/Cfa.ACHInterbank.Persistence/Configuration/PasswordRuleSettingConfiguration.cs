using Cfa.ACHInterbank.Domain.Entities.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public class PasswordRuleSettingConfiguration : IEntityTypeConfiguration<PasswordRuleSetting>
{
    public void Configure(EntityTypeBuilder<PasswordRuleSetting> builder)
    {
        builder.ToTable("PasswordRuleSettings");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.MinLength);
        builder.Property(p => p.MinUppercase);
        builder.Property(p => p.MinNumbers);
        builder.Property(p => p.MinSpecial);
        builder.Property(p => p.MaxSpecial);
    }
}
