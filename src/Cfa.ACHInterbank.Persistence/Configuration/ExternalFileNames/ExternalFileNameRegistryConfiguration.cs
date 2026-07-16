using Cfa.ACHInterbank.Domain.Models.ACH.ExternalFileNames;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration.ExternalFileNames;

public class ExternalFileNameRegistryConfiguration : IEntityTypeConfiguration<ExternalFileNameRegistry>
{
    public void Configure(EntityTypeBuilder<ExternalFileNameRegistry> builder)
    {
        builder.ToTable("ExternalFileNameRegistry");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.FlowCode).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Direction).HasMaxLength(20).IsRequired();
        builder.Property(x => x.ExternalFileName).HasMaxLength(260).IsRequired();
        builder.Property(x => x.InternalFileName).HasMaxLength(260);
        builder.Property(x => x.ExternalFileType).HasMaxLength(30).IsRequired();
        builder.Property(x => x.FileIdModifier).HasMaxLength(1);
        builder.Property(x => x.FileHash).HasMaxLength(128);
        builder.Property(x => x.CycleId).HasMaxLength(64);
        builder.Property(x => x.ValidationDisposition).HasMaxLength(20).IsRequired();
        builder.Property(x => x.ValidationResult).HasMaxLength(30).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(120).IsRequired();
        builder.Property(x => x.RowVersion).IsRequired().IsConcurrencyToken().ValueGeneratedNever();

        builder.HasMany(x => x.ValidationLogs)
            .WithOne(x => x.Registry)
            .HasForeignKey(x => x.RegistryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<ExternalFileNameReservation>()
            .WithMany()
            .HasForeignKey(x => x.GenerationReservationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.ClearingHouseId, x.ExternalFileName, x.ProcessingDate });
        builder.HasIndex(x => new { x.ClearingHouseId, x.CycleId, x.ExternalFileType, x.CreatedAtUtc });
        builder.HasIndex(x => x.GenerationReservationId)
            .IsUnique()
            .HasDatabaseName("UX_ExternalFileNameRegistry_GenerationReservation");
    }
}
