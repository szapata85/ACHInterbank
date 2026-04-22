using Cfa.ACHInterbank.Domain.Models.ACHSobreDigital;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public class CertificateRotationHistoryConfiguration : IEntityTypeConfiguration<CertificateRotationHistory>
{
    public void Configure(EntityTypeBuilder<CertificateRotationHistory> builder)
    {
        builder.ToTable("CertificateRotationHistories");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Reason).HasMaxLength(500).IsRequired();
        builder.Property(x => x.RotatedBy).HasMaxLength(120).IsRequired();
        builder.Property(x => x.TicketRef).HasMaxLength(150);

        builder.HasOne(x => x.PreviousVersion)
            .WithMany()
            .HasForeignKey(x => x.PreviousVersionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.NewVersion)
            .WithMany()
            .HasForeignKey(x => x.NewVersionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.PreviousVersionId, x.NewVersionId }).IsUnique();
    }
}
