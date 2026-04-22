using Cfa.ACHInterbank.Domain.Models.ACHSobreDigital;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public class DigitalEnvelopeOperationLogConfiguration : IEntityTypeConfiguration<DigitalEnvelopeOperationLog>
{
    public void Configure(EntityTypeBuilder<DigitalEnvelopeOperationLog> builder)
    {
        builder.ToTable("DigitalEnvelopeOperationLogs");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Direction).HasMaxLength(40).IsRequired();
        builder.Property(x => x.Result).HasMaxLength(60).IsRequired();
        builder.Property(x => x.ErrorCode).HasMaxLength(100);
        builder.Property(x => x.FileNameIn).HasMaxLength(300);
        builder.Property(x => x.FileNameOut).HasMaxLength(300);
        builder.Property(x => x.HashPlainSha256).HasMaxLength(128);
        builder.Property(x => x.HashEncryptedSha256).HasMaxLength(128);
        builder.Property(x => x.Actor).HasMaxLength(120).IsRequired();

        builder.HasOne(x => x.CertificateVersion)
            .WithMany()
            .HasForeignKey(x => x.CertificateVersionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.OccurredAtUtc, x.ClearingHouseId, x.Environment, x.Purpose, x.Result });
    }
}
