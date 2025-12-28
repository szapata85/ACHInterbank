using Cfa.ACHInterbank.Domain.Models.ACHSobreDigital;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public class DigitalEnvelopeCertificateConfiguration : IEntityTypeConfiguration<DigitalEnvelopeCertificate>
{
    public void Configure(EntityTypeBuilder<DigitalEnvelopeCertificate> builder)
    {
        builder.ToTable("DigitalEnvelopeCertificates");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedOnAdd();

        builder.Property(c => c.FileName)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(c => c.Type)
            .IsRequired();

        builder.Property(c => c.RawData)
            .IsRequired();

        builder.Property(c => c.Password)
            .HasMaxLength(500);

        builder.Property(c => c.Subject)
            .HasMaxLength(500);

        builder.Property(c => c.Issuer)
            .HasMaxLength(500);

        builder.Property(c => c.Thumbprint)
            .HasMaxLength(200);

        builder.Property(c => c.UploadedAt);
    }
}
