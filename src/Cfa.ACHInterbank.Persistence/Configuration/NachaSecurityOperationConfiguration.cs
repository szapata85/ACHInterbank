using Cfa.ACHInterbank.Domain.Models.ACHSobreDigital;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public class NachaSecurityOperationConfiguration : IEntityTypeConfiguration<NachaSecurityOperation>
{
    public void Configure(EntityTypeBuilder<NachaSecurityOperation> builder)
    {
        builder.ToTable("NachaSecurityOperations");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.OperationId).HasMaxLength(120).IsRequired();
        builder.Property(x => x.RequestedBy).HasMaxLength(160).IsRequired();
        builder.Property(x => x.Environment).HasMaxLength(40);
        builder.Property(x => x.ExternalFileName).HasMaxLength(300);
        builder.Property(x => x.PlainHashSha256).HasMaxLength(128);
        builder.Property(x => x.EnvelopeHashSha256).HasMaxLength(128);
        builder.Property(x => x.ErrorCode).HasMaxLength(120);
        builder.Property(x => x.ErrorMessageSanitized).HasMaxLength(500);
        builder.Property(x => x.SigningCertificateThumbprintMasked).HasMaxLength(64);
        builder.Property(x => x.EncryptionCertificateThumbprintMasked).HasMaxLength(64);
        builder.Property(x => x.ArtifactRelativePath).HasMaxLength(400);
        builder.Property(x => x.ArtifactContentType).HasMaxLength(120);
        builder.Property(x => x.RowVersion).IsRequired().IsConcurrencyToken().ValueGeneratedNever();

        builder.HasIndex(x => x.OperationId).IsUnique();
        builder.HasIndex(x => new { x.RequestedAtUtc, x.OperationType, x.Status });
    }
}
