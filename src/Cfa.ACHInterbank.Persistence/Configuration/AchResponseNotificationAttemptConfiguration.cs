using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public class AchResponseNotificationAttemptConfiguration : IEntityTypeConfiguration<AchResponseNotificationAttempt>
{
    public void Configure(EntityTypeBuilder<AchResponseNotificationAttempt> builder)
    {
        builder.ToTable("AchResponseNotificationAttempts");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.EstadoNotificacion).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(x => x.NombreCanal).IsRequired().HasMaxLength(100);
        builder.Property(x => x.IdTransaccion).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Causal).HasMaxLength(50);
        builder.Property(x => x.DescripcionCausal).HasMaxLength(300);
        builder.Property(x => x.CodigoError).HasMaxLength(100);
        builder.Property(x => x.DescripcionError).HasMaxLength(500);
        builder.Property(x => x.ErrorTecnico).HasMaxLength(1000);
        builder.Property(x => x.RequestPayload).HasColumnType("text");
        builder.Property(x => x.ResponsePayload).HasColumnType("text");

        builder.HasIndex(x => new { x.AchResponseId, x.NumeroIntento }).HasDatabaseName("IX_AchRespAttempts_Response_Attempt");
        builder.HasIndex(x => x.EstadoNotificacion).HasDatabaseName("IX_AchRespAttempts_Estado");
        builder.HasIndex(x => x.FechaCreacion).HasDatabaseName("IX_AchRespAttempts_FechaCreacion");
    }
}
