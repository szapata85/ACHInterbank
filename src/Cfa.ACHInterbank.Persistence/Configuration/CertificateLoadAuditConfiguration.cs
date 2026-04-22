using Cfa.ACHInterbank.Domain.Models.ACHSobreDigital;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public class CertificateLoadAuditConfiguration : IEntityTypeConfiguration<CertificateLoadAudit>
{
    public void Configure(EntityTypeBuilder<CertificateLoadAudit> builder)
    {
        builder.ToTable("CertificateLoadAudits");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.LoadSource).HasMaxLength(120).IsRequired();
        builder.Property(x => x.ValidationResult).HasMaxLength(60).IsRequired();
        builder.Property(x => x.ValidationErrorsJson).HasMaxLength(4000);
        builder.Property(x => x.LoadedBy).HasMaxLength(120).IsRequired();

        builder.HasOne(x => x.CertificateVersion)
            .WithMany()
            .HasForeignKey(x => x.CertificateVersionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.LoadedAtUtc);
    }
}
