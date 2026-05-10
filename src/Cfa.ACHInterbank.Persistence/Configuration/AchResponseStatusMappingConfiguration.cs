using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public class AchResponseStatusMappingConfiguration : IEntityTypeConfiguration<AchResponseStatusMapping>
{
    public void Configure(EntityTypeBuilder<AchResponseStatusMapping> builder)
    {
        builder.ToTable("AchResponseStatusMappings");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.CodigoCamaraCompensacion).IsRequired().HasMaxLength(30);
        builder.Property(x => x.CodigoEstadoExterno).IsRequired().HasMaxLength(30);
        builder.Property(x => x.CodigoCausalExterna).HasMaxLength(50);
        builder.Property(x => x.EstadoInternoNombre).IsRequired().HasMaxLength(100);
        builder.Property(x => x.CausalNormalizada).HasMaxLength(50);
        builder.Property(x => x.DescripcionCausalNormalizada).HasMaxLength(300);
        builder.Property(x => x.TipoRespuesta).HasConversion<string>().HasMaxLength(30).IsRequired();

        builder.HasIndex(x => new { x.CodigoCamaraCompensacion, x.TipoRespuesta, x.CodigoEstadoExterno, x.Activo })
            .HasDatabaseName("IX_AchRespStatusMap_Search");
        builder.HasIndex(x => new { x.CodigoCamaraCompensacion, x.TipoRespuesta, x.CodigoEstadoExterno, x.CodigoCausalExterna, x.Activo })
            .HasDatabaseName("IX_AchRespStatusMap_Causal");
        builder.HasIndex(x => new { x.FechaInicioVigencia, x.FechaFinVigencia })
            .HasDatabaseName("IX_AchRespStatusMap_Vigency");
    }
}
