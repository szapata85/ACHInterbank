using Cfa.ACHInterbank.Domain.Entities.Branding;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public class BrandingSettingConfiguration : IEntityTypeConfiguration<BrandingSetting>
{
    public void Configure(EntityTypeBuilder<BrandingSetting> builder)
    {
        builder.ToTable("BrandingSettings");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.PublicLogo);
        builder.Property(b => b.PrivateLogo);
        builder.Property(b => b.PublicBackground);
        builder.Property(b => b.PrivateBackground);
        builder.Property(b => b.SidebarBackground);
        builder.Property(b => b.ButtonColor).HasMaxLength(150);
    }
}
