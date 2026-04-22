using Cfa.ACHInterbank.Domain.Models.ACHSobreDigital;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public class CertificateUsageLogConfiguration : IEntityTypeConfiguration<CertificateUsageLog>
{
    public void Configure(EntityTypeBuilder<CertificateUsageLog> builder)
    {
        builder.ToTable("CertificateUsageLogs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.OperationType).HasMaxLength(120).IsRequired();
        builder.Property(x => x.OperationId).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Result).HasMaxLength(60).IsRequired();
        builder.Property(x => x.ErrorCode).HasMaxLength(100);
        builder.Property(x => x.CreatedByProcess).HasMaxLength(120).IsRequired();
        builder.Property(x => x.ContextJson).HasMaxLength(4000);

        builder.HasOne(x => x.CertificateVersion)
            .WithMany()
            .HasForeignKey(x => x.CertificateVersionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.OccurredAtUtc);
        builder.HasIndex(x => x.Result);
    }
}
