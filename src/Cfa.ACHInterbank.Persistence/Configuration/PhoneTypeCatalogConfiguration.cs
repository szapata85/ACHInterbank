using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

internal class PhoneTypeCatalogConfiguration : IEntityTypeConfiguration<PhoneTypeCatalog>
{
    public void Configure(EntityTypeBuilder<PhoneTypeCatalog> builder)
    {
        builder.ToTable("PhoneTypes");
        builder.HasKey(p => p.Code);

        builder.Property(p => p.Code).HasMaxLength(20).IsRequired();
        builder.Property(p => p.Name).HasMaxLength(100).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(200);

        builder.HasData(
            new PhoneTypeCatalog { Code = "CELULAR", Name = "Celular" },
            new PhoneTypeCatalog { Code = "FIJO", Name = "Fijo" },
            new PhoneTypeCatalog { Code = "TRABAJO", Name = "Trabajo" },
            new PhoneTypeCatalog { Code = "MOVIL", Name = "Móvil" }
        );
    }
}
