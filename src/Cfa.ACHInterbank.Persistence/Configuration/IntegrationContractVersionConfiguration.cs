using Cfa.ACHInterbank.Domain.Entities.Integrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public sealed class IntegrationContractVersionConfiguration : IEntityTypeConfiguration<IntegrationContractVersion>
{
    public void Configure(EntityTypeBuilder<IntegrationContractVersion> builder)
    {
        builder.ToTable("IntegrationContractVersions");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.VersionLabel).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Status).HasConversion<int>().IsRequired();
        builder.Property(x => x.OperationName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.TargetNamespace).HasMaxLength(500).IsRequired();
        builder.Property(x => x.SoapAction).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.SourceType).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(x => x.SourceDocumentName).HasMaxLength(260);
        builder.Property(x => x.SourceDocumentHash).HasMaxLength(64);
        builder.Property(x => x.SourceReference).HasMaxLength(1000);
        builder.Property(x => x.Notes).HasMaxLength(2000);
        builder.Property(x => x.Revision).HasDefaultValue(1L).IsRequired().IsConcurrencyToken();
        builder.Property(x => x.ImportedBy).HasMaxLength(200);
        builder.Property(x => x.ActivatedBy).HasMaxLength(200);

        builder.HasIndex(x => new { x.MethodId, x.VersionLabel })
            .IsUnique()
            .HasDatabaseName("UX_IntegrationContractVersions_Method_VersionLabel");
        builder.HasIndex(x => new { x.MethodId, x.Status })
            .HasDatabaseName("IX_IntegrationContractVersions_Method_Status");
        builder.HasIndex(x => x.SourceDocumentHash)
            .HasDatabaseName("IX_IntegrationContractVersions_SourceDocumentHash");

        builder.HasOne(x => x.Method)
            .WithMany(x => x.ContractVersions)
            .HasForeignKey(x => x.MethodId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
