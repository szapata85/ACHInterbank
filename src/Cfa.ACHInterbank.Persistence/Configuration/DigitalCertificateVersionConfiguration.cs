using Cfa.ACHInterbank.Domain.Models.ACHSobreDigital;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public class DigitalCertificateVersionConfiguration : IEntityTypeConfiguration<DigitalCertificateVersion>
{
    public void Configure(EntityTypeBuilder<DigitalCertificateVersion> builder)
    {
        builder.ToTable("DigitalCertificateVersions");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.FileName).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Subject).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Issuer).HasMaxLength(500).IsRequired();
        builder.Property(x => x.SerialNumber).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Thumbprint).HasMaxLength(200).IsRequired();
        builder.Property(x => x.FingerprintSha256).HasMaxLength(200).IsRequired();
        builder.Property(x => x.KeyAlgorithm).HasMaxLength(120).IsRequired();
        builder.Property(x => x.SignatureAlgorithm).HasMaxLength(120).IsRequired();
        builder.Property(x => x.SecretRef).HasMaxLength(500);
        builder.Property(x => x.FileRef).HasMaxLength(500);
        builder.Property(x => x.UploadedBy).HasMaxLength(120).IsRequired();
        builder.Property(x => x.ValidationSummaryJson).HasMaxLength(4000);
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

        builder.HasOne(x => x.DigitalCertificate)
            .WithMany(x => x.Versions)
            .HasForeignKey(x => x.DigitalCertificateId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ReplacedByVersion)
            .WithMany()
            .HasForeignKey(x => x.ReplacedByVersionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.Thumbprint);
        builder.HasIndex(x => new { x.FingerprintSha256, x.ClearingHouseId, x.Environment, x.Purpose, x.HolderType }).IsUnique();
        builder.HasIndex(x => x.SerialNumber);
        builder.HasIndex(x => x.NotAfter);
        builder.HasIndex(x => new { x.ClearingHouseId, x.Environment, x.Purpose, x.HolderType });
    }
}
