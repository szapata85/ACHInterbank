using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

internal class GenderCatalogConfiguration : IEntityTypeConfiguration<GenderCatalog>
{
    public void Configure(EntityTypeBuilder<GenderCatalog> builder)
    {
        builder.ToTable("GenderTypes");
        builder.HasKey(g => g.Code);

        builder.Property(g => g.Code).HasMaxLength(20).IsRequired();
        builder.Property(g => g.Name).HasMaxLength(100).IsRequired();
        builder.Property(g => g.Description).HasMaxLength(200);

        builder.HasData(
            new GenderCatalog { Code = "MASCULINO", Name = "Masculino" },
            new GenderCatalog { Code = "FEMENINO", Name = "Femenino" },
            new GenderCatalog { Code = "NO_BINARIO", Name = "No binario" },
            new GenderCatalog { Code = "OTRO", Name = "Otro" },
            new GenderCatalog { Code = "NO_ESPECIFICA", Name = "No especifica" }
        );
    }
}
