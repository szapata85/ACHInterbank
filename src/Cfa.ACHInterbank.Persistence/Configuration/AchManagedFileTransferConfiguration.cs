using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public sealed class AchManagedFileTransferEntityConfiguration : IEntityTypeConfiguration<AchManagedFileTransfer>
{
    public void Configure(EntityTypeBuilder<AchManagedFileTransfer> builder)
    {
        builder.ToTable("AchManagedFileTransfers");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Direction).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.ExecutionOrigin).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.LogicalFileIdentity).HasMaxLength(180).IsRequired();
        builder.Property(x => x.PhysicalFileName).HasMaxLength(180).IsRequired();
        builder.Property(x => x.ContentSha256).HasMaxLength(64).IsRequired();
        builder.Property(x => x.AchCycleId).HasMaxLength(40);
        builder.Property(x => x.LastErrorCode).HasMaxLength(80);
        builder.Property(x => x.LastError).HasMaxLength(1000);
        builder.Property(x => x.ActiveStorageReference).HasMaxLength(300);
        builder.Property(x => x.ArchiveReference).HasMaxLength(300);
        builder.Property(x => x.CorrelationId).HasMaxLength(128).IsRequired();
        builder.Property(x => x.IdempotencyKey).HasMaxLength(160).IsRequired();
        builder.Property(x => x.OperatorIdentity).HasMaxLength(160);
        builder.Property(x => x.RetiredBy).HasMaxLength(160);
        builder.Property(x => x.RetirementReason).HasMaxLength(500);
        builder.Property(x => x.ConcurrencyToken).IsConcurrencyToken();
        builder.HasOne(x => x.ClearingHouse).WithMany().HasForeignKey(x => x.ClearingHouseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.AchFileExport).WithMany().HasForeignKey(x => x.AchFileExportId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.IncomingNachaFileIngestion).WithMany().HasForeignKey(x => x.IncomingNachaFileIngestionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.AchCycle).WithMany().HasForeignKey(x => x.AchCycleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.CorrectedFromTransfer).WithMany().HasForeignKey(x => x.CorrectedFromTransferId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.IdempotencyKey).IsUnique();
        builder.HasIndex(x => new { x.Direction, x.ContentSha256, x.FileSize }).IsUnique();
        builder.HasIndex(x => new { x.Direction, x.PhysicalFileName, x.OperationalDate });
        builder.HasIndex(x => new { x.Status, x.OperationalDate });
    }
}

public sealed class AchManagedFileTransferEventConfiguration : IEntityTypeConfiguration<AchManagedFileTransferEvent>
{
    public void Configure(EntityTypeBuilder<AchManagedFileTransferEvent> builder)
    {
        builder.ToTable("AchManagedFileTransferEvents");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EventType).HasMaxLength(60).IsRequired();
        builder.Property(x => x.Result).HasMaxLength(40).IsRequired();
        builder.Property(x => x.Message).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.ExecutionOrigin).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.Actor).HasMaxLength(160).IsRequired();
        builder.HasOne(x => x.Transfer).WithMany(x => x.Events).HasForeignKey(x => x.TransferId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => new { x.TransferId, x.OccurredAtUtc });
    }
}

public sealed class AchManagedFileTransferConfigurationEntityConfiguration : IEntityTypeConfiguration<AchManagedFileTransferConfiguration>
{
    public void Configure(EntityTypeBuilder<AchManagedFileTransferConfiguration> builder)
    {
        builder.ToTable("AchManagedFileTransferConfigurations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.OutboundLocation).HasMaxLength(120).IsRequired();
        builder.Property(x => x.InboundLocation).HasMaxLength(120).IsRequired();
        builder.Property(x => x.ArchiveLocation).HasMaxLength(120).IsRequired();
        builder.Property(x => x.ProfileName).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Provider).HasMaxLength(60).IsRequired();
        builder.Property(x => x.Protocol).HasMaxLength(40).IsRequired();
        builder.Property(x => x.Endpoint).HasMaxLength(300);
        builder.Property(x => x.Principal).HasMaxLength(160);
        builder.Property(x => x.CredentialType).HasMaxLength(40);
        builder.Property(x => x.ProtectedCredential).HasMaxLength(8000);
        builder.Property(x => x.ConcurrencyToken).IsConcurrencyToken();
        builder.HasOne(x => x.ClearingHouse).WithMany().HasForeignKey(x => x.ClearingHouseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.ClearingHouseId).IsUnique();
    }
}
