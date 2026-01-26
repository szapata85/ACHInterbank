using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

internal class PersonTypeCatalogConfiguration : IEntityTypeConfiguration<PersonTypeCatalog>
{
    public void Configure(EntityTypeBuilder<PersonTypeCatalog> builder)
    {
        builder.ToTable("PersonTypes");
        builder.HasKey(p => p.Code);

        builder.Property(p => p.Code).HasMaxLength(5).IsRequired();
        builder.Property(p => p.Name).HasMaxLength(100).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(200);

        builder.HasData(
            new PersonTypeCatalog { Code = "PN", Name = "Persona natural" },
            new PersonTypeCatalog { Code = "PJ", Name = "Persona jurídica" }
        );
    }
}
