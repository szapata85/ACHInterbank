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

        builder.Property(b => b.PublicLogo).HasColumnType("nvarchar(max)");
        builder.Property(b => b.PrivateLogo).HasColumnType("nvarchar(max)");
        builder.Property(b => b.PublicBackground).HasColumnType("nvarchar(max)");
        builder.Property(b => b.PrivateBackground).HasColumnType("nvarchar(max)");
        builder.Property(b => b.SidebarBackground).HasColumnType("nvarchar(max)");
        builder.Property(b => b.ButtonColor).HasMaxLength(150);
    }
}
