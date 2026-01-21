using Cfa.ACHInterbank.Domain.Entities.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public class LoginLockoutSettingConfiguration : IEntityTypeConfiguration<LoginLockoutSetting>
{
    public void Configure(EntityTypeBuilder<LoginLockoutSetting> builder)
    {
        builder.ToTable("LoginLockoutSettings");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.MaxFailedAttempts);
        builder.Property(x => x.LockoutMinutes);
    }
}
