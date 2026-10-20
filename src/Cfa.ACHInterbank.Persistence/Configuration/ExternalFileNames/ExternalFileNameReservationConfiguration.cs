using Cfa.ACHInterbank.Domain.Models.ACH.ExternalFileNames;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration.ExternalFileNames;

public class ExternalFileNameReservationConfiguration : IEntityTypeConfiguration<ExternalFileNameReservation>
{
    public void Configure(EntityTypeBuilder<ExternalFileNameReservation> builder)
    {
        builder.ToTable("ExternalFileNameReservations");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ScopeCode).HasMaxLength(40).IsRequired();
        builder.Property(x => x.IdempotencyKeyHash).HasMaxLength(64).IsFixedLength().IsRequired();
        builder.Property(x => x.RequestFingerprintHash).HasMaxLength(64).IsFixedLength().IsRequired();
        builder.Property(x => x.FileIdModifier).HasMaxLength(1);
        builder.Property(x => x.ExternalFileName).HasMaxLength(260);
        builder.Property(x => x.Status).HasMaxLength(20).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(120).IsRequired();
        builder.Property(x => x.RowVersion).IsRequired().IsConcurrencyToken().ValueGeneratedNever();

        builder.HasIndex(x => new { x.ClearingHouseId, x.IdempotencyKeyHash })
            .IsUnique()
            .HasDatabaseName("UX_ExternalFileNameReservations_Idempotency");
        builder.HasIndex(x => new { x.ClearingHouseId, x.ScopeCode, x.OperationalDate, x.Sequence })
            .IsUnique()
            .HasDatabaseName("UX_ExternalFileNameReservations_Sequence");
        builder.HasIndex(x => new { x.OperationalDate, x.Status });
    }
}
