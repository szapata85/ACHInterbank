using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public class AchResponseConfiguration : IEntityTypeConfiguration<AchResponse>
{
    public void Configure(EntityTypeBuilder<AchResponse> builder)
    {
        builder.ToTable("AchResponses");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.TipoRespuesta).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.ClearingHouseId);
        builder.Property(x => x.CorrelationStatus).HasConversion<string>().HasMaxLength(30)
            .HasDefaultValue(AchResponseCorrelationStatus.Unknown).IsRequired();
        builder.Property(x => x.CorrelationCriterion).HasMaxLength(80);
        builder.Property(x => x.IdTransaccion).IsRequired().HasMaxLength(100);
        builder.Property(x => x.CodigoCamaraCompensacion).IsRequired().HasMaxLength(30);
        builder.Property(x => x.CodigoEntidadOrigen).HasMaxLength(30);
        builder.Property(x => x.CodigoEntidadDestino).HasMaxLength(30);
        builder.Property(x => x.CodigoEstadoExterno).IsRequired().HasMaxLength(30);
        builder.Property(x => x.CodigoCausalExterna).HasMaxLength(50);
        builder.Property(x => x.EstadoInternoNombre).HasMaxLength(100);
        builder.Property(x => x.CausalNormalizada).HasMaxLength(50);
        builder.Property(x => x.DescripcionCausal).HasMaxLength(300);
        builder.Property(x => x.HashIdempotencia).IsRequired().HasMaxLength(128);
        builder.Property(x => x.CanonicalPayloadHash).IsRequired().HasMaxLength(128);
        builder.Property(x => x.OperationalDate).IsRequired();
        builder.Property(x => x.Version).IsRequired().IsConcurrencyToken().ValueGeneratedNever();
        builder.Property(x => x.EstadoProcesamiento).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(x => x.MotivoNoHomologacion).HasMaxLength(500);
        builder.Property(x => x.CorrelationId).HasMaxLength(100);
        builder.Property(x => x.FechaRecepcion).IsRequired();
        builder.Property(x => x.FechaCreacion).IsRequired();

        builder.HasMany(x => x.NotificationAttempts)
            .WithOne(x => x.AchResponse)
            .HasForeignKey(x => x.AchResponseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ClearingHouse)
            .WithMany()
            .HasForeignKey(x => x.ClearingHouseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.AchTransaction)
            .WithMany()
            .HasForeignKey(x => x.AchTransactionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.AppliedMapping)
            .WithMany(x => x.AppliedResponses)
            .HasForeignKey(x => x.AppliedMappingId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.HashIdempotencia).IsUnique().HasDatabaseName("UX_AchResponses_HashIdempotencia");
        builder.HasIndex(x => x.IdTransaccion).HasDatabaseName("IX_AchResponses_IdTransaccion");
        builder.HasIndex(x => new { x.TipoRespuesta, x.CodigoCamaraCompensacion, x.CodigoEstadoExterno }).HasDatabaseName("IX_AchResponses_Filter");
        builder.HasIndex(x => x.EstadoProcesamiento).HasDatabaseName("IX_AchResponses_EstadoProcesamiento");
        builder.HasIndex(x => x.CorrelationId).HasDatabaseName("IX_AchResponses_CorrelationId");
        builder.HasIndex(x => new { x.AchTransactionId, x.FechaRecepcion }).HasDatabaseName("IX_AchResponses_Transaction_ReceivedAt");
        builder.HasIndex(x => new { x.ClearingHouseId, x.OperationalDate, x.EstadoProcesamiento }).HasDatabaseName("IX_AchResponses_Operational");
    }
}
