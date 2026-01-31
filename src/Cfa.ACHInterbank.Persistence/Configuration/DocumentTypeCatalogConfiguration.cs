using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

internal class DocumentTypeCatalogConfiguration : IEntityTypeConfiguration<DocumentTypeCatalog>
{
    public void Configure(EntityTypeBuilder<DocumentTypeCatalog> builder)
    {
        builder.ToTable("DocumentTypes");
        builder.HasKey(d => d.Code);

        builder.Property(d => d.Code).HasMaxLength(10).IsRequired();
        builder.Property(d => d.Name).HasMaxLength(100).IsRequired();
        builder.Property(d => d.Description).HasMaxLength(200);

        builder.HasData(
            new DocumentTypeCatalog { Code = "CC", Name = "Cédula de Ciudadanía" },
            new DocumentTypeCatalog { Code = "CE", Name = "Cédula de Extranjería" },
            new DocumentTypeCatalog { Code = "NIT", Name = "Número de Identificación Tributaria" },
            new DocumentTypeCatalog { Code = "PAS", Name = "Pasaporte" },
            new DocumentTypeCatalog { Code = "TI", Name = "Tarjeta de Identidad" },
            new DocumentTypeCatalog { Code = "OTRO", Name = "Otro" }
        );
    }
}
